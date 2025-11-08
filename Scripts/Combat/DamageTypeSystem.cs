using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace DeadFrontier.Combat
{
    /// <summary>
    /// Comprehensive damage type and armor system with resistances, weaknesses, and status effects.
    /// Provides tactical depth through damage type matching and armor penetration mechanics.
    /// </summary>
    public class DamageTypeSystem : MonoBehaviour
    {
        public static DamageTypeSystem Instance { get; private set; }

        [Header("Damage Multipliers")]
        [SerializeField] private DamageTypeMatchup[] damageMatchups;

        [Header("Status Effects")]
        [SerializeField] private StatusEffectData[] statusEffects;

        [Header("Debug")]
        [SerializeField] private bool showDamageNumbers = true;
        [SerializeField] private GameObject damageNumberPrefab;

        // Active status effects on entities
        private Dictionary<GameObject, List<ActiveStatusEffect>> activeStatusEffects = new Dictionary<GameObject, List<ActiveStatusEffect>>();

        // Events
        public event Action<GameObject, DamageInfo> OnDamageDealt;
        public event Action<GameObject, StatusEffectType, float> OnStatusEffectApplied;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            UpdateStatusEffects();
        }

        #region Damage Calculation

        public float CalculateDamage(DamageInfo damageInfo, ArmorData targetArmor)
        {
            float finalDamage = damageInfo.baseDamage;

            // Apply damage type multipliers
            if (targetArmor != null)
            {
                float typeMultiplier = GetDamageTypeMultiplier(damageInfo.damageType, targetArmor.armorType);
                finalDamage *= typeMultiplier;
            }

            // Apply armor reduction
            if (targetArmor != null && !damageInfo.ignoresArmor)
            {
                float armorReduction = CalculateArmorReduction(targetArmor, damageInfo);
                finalDamage *= (1f - armorReduction);
            }

            // Apply penetration
            if (damageInfo.armorPenetration > 0 && targetArmor != null)
            {
                float penetrationBonus = damageInfo.armorPenetration / targetArmor.armorRating;
                penetrationBonus = Mathf.Clamp01(penetrationBonus);
                finalDamage *= (1f + penetrationBonus * 0.5f); // Up to 50% bonus damage from penetration
            }

            // Apply critical hit
            if (damageInfo.isCritical)
            {
                finalDamage *= damageInfo.criticalMultiplier;
            }

            // Apply damage reduction from buffs/debuffs
            if (damageInfo.target != null)
            {
                float damageReductionModifier = GetDamageReductionModifier(damageInfo.target);
                finalDamage *= (1f - damageReductionModifier);
            }

            // Minimum damage
            finalDamage = Mathf.Max(finalDamage, 1f);

            return finalDamage;
        }

        public void ApplyDamage(DamageInfo damageInfo)
        {
            if (damageInfo.target == null) return;

            // Get target armor
            ArmorData targetArmor = GetTargetArmor(damageInfo.target);

            // Calculate final damage
            float finalDamage = CalculateDamage(damageInfo, targetArmor);

            // Apply damage to target
            var damageable = damageInfo.target.GetComponent<Core.IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(finalDamage);
            }

            // Apply status effects
            if (damageInfo.statusEffect != StatusEffectType.None)
            {
                float procChance = damageInfo.statusEffectChance;

                if (UnityEngine.Random.value < procChance)
                {
                    ApplyStatusEffect(damageInfo.target, damageInfo.statusEffect, damageInfo.statusEffectDuration);
                }
            }

            // Damage armor
            if (targetArmor != null && !damageInfo.ignoresArmor)
            {
                DamageArmor(damageInfo.target, damageInfo.baseDamage * 0.1f); // Armor takes 10% of damage
            }

            // Show damage numbers
            if (showDamageNumbers && damageInfo.hitPoint != Vector3.zero)
            {
                ShowDamageNumber(finalDamage, damageInfo.hitPoint, damageInfo.isCritical);
            }

            OnDamageDealt?.Invoke(damageInfo.target, damageInfo);

            Debug.Log($"[DamageTypeSystem] Applied {finalDamage:F1} damage to {damageInfo.target.name} ({damageInfo.damageType})");
        }

        private float GetDamageTypeMultiplier(DamageType attackType, ArmorType defenseType)
        {
            var matchup = damageMatchups.FirstOrDefault(m =>
                m.attackDamageType == attackType && m.targetArmorType == defenseType);

            return matchup != null ? matchup.damageMultiplier : 1f;
        }

        private float CalculateArmorReduction(ArmorData armor, DamageInfo damageInfo)
        {
            if (armor == null) return 0f;

            float armorValue = armor.currentArmor;
            float reduction = armorValue / (armorValue + 100f); // Diminishing returns formula

            // Maximum 75% reduction from armor
            reduction = Mathf.Clamp01(reduction) * 0.75f;

            return reduction;
        }

        private float GetDamageReductionModifier(GameObject target)
        {
            float totalReduction = 0f;

            if (activeStatusEffects.ContainsKey(target))
            {
                foreach (var effect in activeStatusEffects[target])
                {
                    if (effect.effectType == StatusEffectType.DamageReduction)
                    {
                        totalReduction += effect.magnitude;
                    }
                }
            }

            // Check for skill bonuses
            if (Progression.SkillTreeSystem.Instance != null)
            {
                totalReduction += Progression.SkillTreeSystem.Instance.GetBonus(Progression.SkillBonusType.DamageResistance);
            }

            return Mathf.Clamp01(totalReduction);
        }

        #endregion

        #region Armor Management

        private ArmorData GetTargetArmor(GameObject target)
        {
            var armorComponent = target.GetComponent<ArmorComponent>();
            return armorComponent != null ? armorComponent.ArmorData : null;
        }

        private void DamageArmor(GameObject target, float damage)
        {
            var armorComponent = target.GetComponent<ArmorComponent>();
            if (armorComponent != null)
            {
                armorComponent.DamageArmor(damage);
            }
        }

        public void RepairArmor(GameObject target, float amount)
        {
            var armorComponent = target.GetComponent<ArmorComponent>();
            if (armorComponent != null)
            {
                armorComponent.RepairArmor(amount);
            }
        }

        #endregion

        #region Status Effects

        public void ApplyStatusEffect(GameObject target, StatusEffectType effectType, float duration)
        {
            if (target == null) return;

            var effectData = statusEffects.FirstOrDefault(e => e.effectType == effectType);
            if (effectData == null)
            {
                Debug.LogWarning($"[DamageTypeSystem] Unknown status effect type: {effectType}");
                return;
            }

            if (!activeStatusEffects.ContainsKey(target))
            {
                activeStatusEffects[target] = new List<ActiveStatusEffect>();
            }

            // Check if effect already exists
            var existing = activeStatusEffects[target].FirstOrDefault(e => e.effectType == effectType);
            if (existing != null)
            {
                // Refresh duration
                existing.remainingDuration = Mathf.Max(existing.remainingDuration, duration);
                return;
            }

            // Create new effect
            var activeEffect = new ActiveStatusEffect
            {
                effectType = effectType,
                magnitude = effectData.magnitude,
                remainingDuration = duration,
                tickInterval = effectData.tickInterval,
                nextTickTime = Time.time + effectData.tickInterval
            };

            activeStatusEffects[target].Add(activeEffect);

            OnStatusEffectApplied?.Invoke(target, effectType, duration);

            // Apply immediate effects
            ApplyStatusEffectImmediate(target, activeEffect);

            Debug.Log($"[DamageTypeSystem] Applied {effectType} to {target.name} for {duration}s");
        }

        public void RemoveStatusEffect(GameObject target, StatusEffectType effectType)
        {
            if (!activeStatusEffects.ContainsKey(target)) return;

            var effect = activeStatusEffects[target].FirstOrDefault(e => e.effectType == effectType);
            if (effect != null)
            {
                activeStatusEffects[target].Remove(effect);

                // Remove effect modifiers
                RemoveStatusEffectModifiers(target, effect);
            }
        }

        private void UpdateStatusEffects()
        {
            var targetsToRemove = new List<GameObject>();

            foreach (var kvp in activeStatusEffects)
            {
                var target = kvp.Key;
                if (target == null)
                {
                    targetsToRemove.Add(target);
                    continue;
                }

                var effectsToRemove = new List<ActiveStatusEffect>();

                foreach (var effect in kvp.Value)
                {
                    // Update duration
                    effect.remainingDuration -= Time.deltaTime;

                    if (effect.remainingDuration <= 0f)
                    {
                        effectsToRemove.Add(effect);
                        RemoveStatusEffectModifiers(target, effect);
                        continue;
                    }

                    // Process ticking effects
                    if (Time.time >= effect.nextTickTime)
                    {
                        ProcessStatusEffectTick(target, effect);
                        effect.nextTickTime = Time.time + effect.tickInterval;
                    }
                }

                // Remove expired effects
                foreach (var effect in effectsToRemove)
                {
                    kvp.Value.Remove(effect);
                }

                if (kvp.Value.Count == 0)
                {
                    targetsToRemove.Add(target);
                }
            }

            // Clean up empty target lists
            foreach (var target in targetsToRemove)
            {
                activeStatusEffects.Remove(target);
            }
        }

        private void ApplyStatusEffectImmediate(GameObject target, ActiveStatusEffect effect)
        {
            switch (effect.effectType)
            {
                case StatusEffectType.Slow:
                    var movement = target.GetComponent<Player.PlayerMovement>();
                    if (movement != null)
                    {
                        movement.SetSpeedMultiplier(1f - effect.magnitude);
                    }
                    break;

                case StatusEffectType.Stun:
                    var movement2 = target.GetComponent<Player.PlayerMovement>();
                    if (movement2 != null)
                    {
                        movement2.SetSpeedMultiplier(0f);
                    }
                    break;
            }
        }

        private void ProcessStatusEffectTick(GameObject target, ActiveStatusEffect effect)
        {
            switch (effect.effectType)
            {
                case StatusEffectType.Bleeding:
                case StatusEffectType.Poison:
                case StatusEffectType.Burning:
                    var damageable = target.GetComponent<Core.IDamageable>();
                    if (damageable != null)
                    {
                        damageable.TakeDamage(effect.magnitude);
                    }
                    break;

                case StatusEffectType.Regeneration:
                    var health = target.GetComponent<Player.PlayerHealth>();
                    if (health != null)
                    {
                        health.Heal(effect.magnitude);
                    }
                    break;
            }
        }

        private void RemoveStatusEffectModifiers(GameObject target, ActiveStatusEffect effect)
        {
            switch (effect.effectType)
            {
                case StatusEffectType.Slow:
                case StatusEffectType.Stun:
                    var movement = target.GetComponent<Player.PlayerMovement>();
                    if (movement != null)
                    {
                        movement.SetSpeedMultiplier(1f);
                    }
                    break;
            }
        }

        #endregion

        #region Visual Feedback

        private void ShowDamageNumber(float damage, Vector3 position, bool isCritical)
        {
            if (damageNumberPrefab == null) return;

            GameObject damageNumber = Instantiate(damageNumberPrefab, position, Quaternion.identity);

            var textMesh = damageNumber.GetComponentInChildren<TMPro.TextMeshPro>();
            if (textMesh != null)
            {
                textMesh.text = Mathf.RoundToInt(damage).ToString();
                textMesh.color = isCritical ? Color.yellow : Color.white;
                textMesh.fontSize = isCritical ? 8 : 6;
            }

            Destroy(damageNumber, 1f);
        }

        #endregion

        #region Public Getters

        public List<ActiveStatusEffect> GetActiveStatusEffects(GameObject target)
        {
            if (activeStatusEffects.ContainsKey(target))
                return new List<ActiveStatusEffect>(activeStatusEffects[target]);

            return new List<ActiveStatusEffect>();
        }

        public bool HasStatusEffect(GameObject target, StatusEffectType effectType)
        {
            if (!activeStatusEffects.ContainsKey(target)) return false;

            return activeStatusEffects[target].Any(e => e.effectType == effectType);
        }

        #endregion
    }

    #region Data Classes

    [System.Serializable]
    public class DamageInfo
    {
        public GameObject target;
        public GameObject source;
        public float baseDamage;
        public DamageType damageType = DamageType.Physical;
        public Vector3 hitPoint;
        public bool isCritical;
        public float criticalMultiplier = 2f;
        public float armorPenetration;
        public bool ignoresArmor;
        public StatusEffectType statusEffect = StatusEffectType.None;
        public float statusEffectChance = 0.1f;
        public float statusEffectDuration = 5f;
    }

    [System.Serializable]
    public class DamageTypeMatchup
    {
        public DamageType attackDamageType;
        public ArmorType targetArmorType;
        [Range(0f, 3f)]
        public float damageMultiplier = 1f;
    }

    [System.Serializable]
    public class ArmorData
    {
        public string armorId;
        public string armorName;
        public ArmorType armorType;
        public float maxArmor = 100f;
        public float currentArmor = 100f;
        public float armorRating = 50f;
        public float durabilityLossRate = 1f;
    }

    [System.Serializable]
    public class StatusEffectData
    {
        public StatusEffectType effectType;
        public float magnitude;
        public float tickInterval = 1f;
        public Sprite icon;
    }

    public class ActiveStatusEffect
    {
        public StatusEffectType effectType;
        public float magnitude;
        public float remainingDuration;
        public float tickInterval;
        public float nextTickTime;
    }

    public enum DamageType
    {
        Physical,       // Standard bullet/melee damage
        Explosive,      // Grenades, rockets
        Fire,           // Incendiary weapons
        Poison,         // Toxic damage
        Electric,       // Electrical damage
        Piercing,       // Armor-piercing rounds
        Blunt           // Blunt force trauma
    }

    public enum ArmorType
    {
        None,           // No armor
        Light,          // Lightweight, mobility-focused
        Medium,         // Balanced protection
        Heavy,          // Maximum protection
        Ballistic,      // Bullet-resistant
        Blast,          // Explosion-resistant
        Hazmat          // Chemical/biological protection
    }

    public enum StatusEffectType
    {
        None,
        Bleeding,           // Damage over time
        Poison,             // Damage over time + reduced healing
        Burning,            // Damage over time + panic
        Slow,               // Reduced movement speed
        Stun,               // Cannot move
        Blind,              // Reduced vision
        Regeneration,       // Healing over time
        DamageReduction,    // Take less damage
        DamageBoost         // Deal more damage
    }

    #endregion

    #region Components

    public class ArmorComponent : MonoBehaviour
    {
        [SerializeField] private ArmorData armorData;

        public ArmorData ArmorData => armorData;

        public void DamageArmor(float damage)
        {
            if (armorData == null) return;

            armorData.currentArmor -= damage * armorData.durabilityLossRate;
            armorData.currentArmor = Mathf.Max(armorData.currentArmor, 0f);

            if (armorData.currentArmor <= 0f)
            {
                Debug.Log($"[ArmorComponent] Armor {armorData.armorName} destroyed!");
            }
        }

        public void RepairArmor(float amount)
        {
            if (armorData == null) return;

            armorData.currentArmor = Mathf.Min(armorData.currentArmor + amount, armorData.maxArmor);
        }

        public float GetArmorPercentage()
        {
            if (armorData == null) return 0f;
            return armorData.currentArmor / armorData.maxArmor;
        }
    }

    #endregion
}
