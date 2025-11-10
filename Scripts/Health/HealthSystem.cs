using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Health
{
    /// <summary>
    /// Comprehensive health system managing health, armor, shields, damage, healing,
    /// regeneration, death, and respawn mechanics for all entities.
    /// </summary>
    public class HealthSystem : NetworkBehaviour
    {
        public static HealthSystem Instance { get; private set; }

        private Dictionary<ulong, EntityHealth> entityHealth = new Dictionary<ulong, EntityHealth>();
        private Dictionary<ulong, float> lastDamageTime = new Dictionary<ulong, float>();
        private Dictionary<ulong, float> lastRegenTime = new Dictionary<ulong, float>();

        public event Action<ulong, float, float> OnHealthChanged;
        public event Action<ulong, float, float> OnArmorChanged;
        public event Action<ulong, DamageInfo> OnDamageTaken;
        public event Action<ulong, float> OnHealed;
        public event Action<ulong, ulong> OnEntityDied; // victim, killer
        public event Action<ulong> OnEntityRespawned;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Update()
        {
            if (IsServer)
            {
                ProcessRegeneration();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void InitializeEntityHealthServerRpc(ulong entityId, float maxHealth, float maxArmor, float maxShield, bool hasRegeneration, ServerRpcParams rpcParams = default)
        {
            if (!entityHealth.ContainsKey(entityId))
            {
                entityHealth[entityId] = new EntityHealth
                {
                    entityId = entityId,
                    maxHealth = maxHealth,
                    currentHealth = maxHealth,
                    maxArmor = maxArmor,
                    currentArmor = maxArmor,
                    maxShield = maxShield,
                    currentShield = maxShield,
                    hasRegeneration = hasRegeneration,
                    isAlive = true,
                    lastDamageTime = Time.time
                };

                Debug.Log($"Initialized health for entity {entityId}: {maxHealth} HP, {maxArmor} Armor");
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void ApplyDamageServerRpc(ulong targetEntityId, DamageInfo damageInfo, ServerRpcParams rpcParams = default)
        {
            if (!entityHealth.TryGetValue(targetEntityId, out var health)) return;
            if (!health.isAlive) return;

            // Calculate actual damage after resistances and modifiers
            float finalDamage = CalculateDamage(health, damageInfo);

            // Apply damage to shields first
            if (health.currentShield > 0f)
            {
                float shieldDamage = Mathf.Min(finalDamage, health.currentShield);
                health.currentShield -= shieldDamage;
                finalDamage -= shieldDamage;
            }

            // Then armor
            if (finalDamage > 0f && health.currentArmor > 0f)
            {
                float armorAbsorption = damageInfo.armorPenetration >= 1f ? 0f : (1f - damageInfo.armorPenetration);
                float armorDamage = finalDamage * armorAbsorption;
                float healthDamage = finalDamage * (1f - armorAbsorption);

                health.currentArmor -= armorDamage;
                if (health.currentArmor < 0f)
                {
                    healthDamage += Mathf.Abs(health.currentArmor);
                    health.currentArmor = 0f;
                }

                finalDamage = healthDamage;
                OnArmorChanged?.Invoke(targetEntityId, health.currentArmor, health.maxArmor);
            }

            // Finally health
            if (finalDamage > 0f)
            {
                health.currentHealth -= finalDamage;
                health.lastDamageTime = Time.time;
                lastDamageTime[targetEntityId] = Time.time;

                OnHealthChanged?.Invoke(targetEntityId, health.currentHealth, health.maxHealth);
                OnDamageTaken?.Invoke(targetEntityId, damageInfo);

                // Check for death
                if (health.currentHealth <= 0f)
                {
                    HandleEntityDeath(targetEntityId, damageInfo.attackerId);
                }

                // Notify clients
                NotifyDamageClientRpc(targetEntityId, finalDamage, damageInfo.damageType, damageInfo.isHeadshot);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void HealEntityServerRpc(ulong entityId, float healAmount, HealType healType, ServerRpcParams rpcParams = default)
        {
            if (!entityHealth.TryGetValue(entityId, out var health)) return;
            if (!health.isAlive) return;

            float actualHeal = Mathf.Min(healAmount, health.maxHealth - health.currentHealth);
            
            if (actualHeal > 0f)
            {
                health.currentHealth += actualHeal;
                OnHealthChanged?.Invoke(entityId, health.currentHealth, health.maxHealth);
                OnHealed?.Invoke(entityId, actualHeal);

                NotifyHealClientRpc(entityId, actualHeal, healType);
                Debug.Log($"Entity {entityId} healed for {actualHeal} HP ({healType})");
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void RestoreArmorServerRpc(ulong entityId, float armorAmount, ServerRpcParams rpcParams = default)
        {
            if (!entityHealth.TryGetValue(entityId, out var health)) return;

            float actualRestore = Mathf.Min(armorAmount, health.maxArmor - health.currentArmor);
            
            if (actualRestore > 0f)
            {
                health.currentArmor += actualRestore;
                OnArmorChanged?.Invoke(entityId, health.currentArmor, health.maxArmor);
                Debug.Log($"Entity {entityId} restored {actualRestore} armor");
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void RestoreShieldServerRpc(ulong entityId, float shieldAmount, ServerRpcParams rpcParams = default)
        {
            if (!entityHealth.TryGetValue(entityId, out var health)) return;

            float actualRestore = Mathf.Min(shieldAmount, health.maxShield - health.currentShield);
            
            if (actualRestore > 0f)
            {
                health.currentShield += actualRestore;
                Debug.Log($"Entity {entityId} restored {actualRestore} shield");
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void RespawnEntityServerRpc(ulong entityId, Vector3 spawnPosition, ServerRpcParams rpcParams = default)
        {
            if (!entityHealth.TryGetValue(entityId, out var health)) return;

            // Reset health values
            health.currentHealth = health.maxHealth;
            health.currentArmor = health.maxArmor;
            health.currentShield = health.maxShield;
            health.isAlive = true;
            health.lastDamageTime = Time.time;

            OnEntityRespawned?.Invoke(entityId);
            NotifyRespawnClientRpc(entityId, spawnPosition);
            
            Debug.Log($"Entity {entityId} respawned at {spawnPosition}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void SetInvulnerableServerRpc(ulong entityId, bool invulnerable, float duration, ServerRpcParams rpcParams = default)
        {
            if (!entityHealth.TryGetValue(entityId, out var health)) return;

            health.isInvulnerable = invulnerable;

            if (invulnerable && duration > 0f)
            {
                StartCoroutine(RemoveInvulnerabilityAfterDelay(entityId, duration));
            }
        }

        private System.Collections.IEnumerator RemoveInvulnerabilityAfterDelay(ulong entityId, float delay)
        {
            yield return new WaitForSeconds(delay);

            if (entityHealth.TryGetValue(entityId, out var health))
            {
                health.isInvulnerable = false;
            }
        }

        private float CalculateDamage(EntityHealth health, DamageInfo damageInfo)
        {
            if (health.isInvulnerable) return 0f;

            float damage = damageInfo.baseDamage;

            // Apply damage multipliers
            damage *= damageInfo.damageMultiplier;

            // Apply headshot multiplier
            if (damageInfo.isHeadshot)
            {
                damage *= damageInfo.headshotMultiplier;
            }

            // Apply resistances
            if (health.damageResistances != null && health.damageResistances.TryGetValue(damageInfo.damageType, out float resistance))
            {
                damage *= (1f - resistance);
            }

            // Apply vulnerabilities
            if (health.damageVulnerabilities != null && health.damageVulnerabilities.TryGetValue(damageInfo.damageType, out float vulnerability))
            {
                damage *= (1f + vulnerability);
            }

            return Mathf.Max(0f, damage);
        }

        private void ProcessRegeneration()
        {
            foreach (var kvp in entityHealth)
            {
                var health = kvp.Value;
                if (!health.isAlive || !health.hasRegeneration) continue;

                // Check if enough time has passed since last damage
                if (Time.time - health.lastDamageTime < health.regenDelay) continue;

                // Check regen interval
                if (!lastRegenTime.TryGetValue(health.entityId, out float lastRegen))
                {
                    lastRegenTime[health.entityId] = Time.time;
                    continue;
                }

                if (Time.time - lastRegen < health.regenInterval) continue;

                // Apply regeneration
                if (health.currentHealth < health.maxHealth)
                {
                    float regenAmount = health.regenRate * health.regenInterval;
                    HealEntityServerRpc(health.entityId, regenAmount, HealType.Regeneration);
                    lastRegenTime[health.entityId] = Time.time;
                }
            }
        }

        private void HandleEntityDeath(ulong victimId, ulong killerId)
        {
            if (!entityHealth.TryGetValue(victimId, out var health)) return;

            health.isAlive = false;
            health.currentHealth = 0f;
            health.deathCount++;

            OnEntityDied?.Invoke(victimId, killerId);
            NotifyDeathClientRpc(victimId, killerId);

            Debug.Log($"Entity {victimId} killed by {killerId}");
        }

        // Client notifications
        [ClientRpc]
        private void NotifyDamageClientRpc(ulong entityId, float damage, DamageType damageType, bool isHeadshot)
        {
            // Visual feedback for damage
        }

        [ClientRpc]
        private void NotifyHealClientRpc(ulong entityId, float healAmount, HealType healType)
        {
            // Visual feedback for healing
        }

        [ClientRpc]
        private void NotifyDeathClientRpc(ulong victimId, ulong killerId)
        {
            // Handle death effects, ragdoll, etc.
        }

        [ClientRpc]
        private void NotifyRespawnClientRpc(ulong entityId, Vector3 spawnPosition)
        {
            // Handle respawn effects
        }

        // Query methods
        public EntityHealth GetEntityHealth(ulong entityId) => entityHealth.GetValueOrDefault(entityId);
        public bool IsEntityAlive(ulong entityId) => entityHealth.TryGetValue(entityId, out var health) && health.isAlive;
        public float GetHealthPercentage(ulong entityId)
        {
            if (entityHealth.TryGetValue(entityId, out var health))
            {
                return health.currentHealth / health.maxHealth;
            }
            return 0f;
        }
    }

    [Serializable]
    public class EntityHealth
    {
        public ulong entityId;
        
        // Health
        public float maxHealth = 100f;
        public float currentHealth = 100f;
        
        // Armor
        public float maxArmor = 0f;
        public float currentArmor = 0f;
        
        // Shield
        public float maxShield = 0f;
        public float currentShield = 0f;
        
        // Status
        public bool isAlive = true;
        public bool isInvulnerable = false;
        
        // Regeneration
        public bool hasRegeneration = false;
        public float regenRate = 5f; // HP per second
        public float regenDelay = 5f; // Seconds after taking damage
        public float regenInterval = 1f; // Seconds between regen ticks
        public float lastDamageTime;
        
        // Resistances and Vulnerabilities
        public Dictionary<DamageType, float> damageResistances;
        public Dictionary<DamageType, float> damageVulnerabilities;
        
        // Stats
        public int killCount;
        public int deathCount;
    }

    [Serializable]
    public class DamageInfo
    {
        public ulong attackerId;
        public float baseDamage;
        public DamageType damageType;
        public float damageMultiplier = 1f;
        public bool isHeadshot = false;
        public float headshotMultiplier = 2f;
        public float armorPenetration = 0f; // 0 = no penetration, 1 = full penetration
        public Vector3 hitPoint;
        public Vector3 hitNormal;
        public string weaponId;
    }

    public enum DamageType
    {
        Physical, Fire, Poison, Explosive, Electric,
        Bleed, Freeze, Radiation, Psychic, True
    }

    public enum HealType
    {
        Instant, OverTime, Regeneration, Medkit, Ability
    }
}
