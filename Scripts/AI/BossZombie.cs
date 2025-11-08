using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

namespace DeadFrontier.AI
{
    /// <summary>
    /// Boss zombie with multiple phases, special abilities, and high threat level.
    /// Provides challenging end-game content and high-value loot rewards.
    /// </summary>
    public class BossZombie : ZombieAI
    {
        [Header("Boss Settings")]
        [SerializeField] private string bossName = "Behemoth";
        [SerializeField] private float bossHealth = 5000f;
        [SerializeField] private float bossSpeed = 3f;
        [SerializeField] private float bossDamage = 50f;
        [SerializeField] private float detectionRange = 50f;

        [Header("Phase System")]
        [SerializeField] private BossPhase[] phases;
        [SerializeField] private float phaseTransitionDuration = 2f;

        [Header("Abilities")]
        [SerializeField] private BossAbility[] abilities;
        [SerializeField] private float abilityCooldown = 10f;

        [Header("Minion Spawning")]
        [SerializeField] private bool canSpawnMinions = true;
        [SerializeField] private int minionsPerSpawn = 5;
        [SerializeField] private float minionSpawnInterval = 30f;
        [SerializeField] private float minionSpawnRange = 10f;

        [Header("Special Mechanics")]
        [SerializeField] private bool hasArmor = true;
        [SerializeField] private float armorReduction = 0.5f;
        [SerializeField] private bool hasWeakPoints = true;
        [SerializeField] private Transform[] weakPoints;
        [SerializeField] private float weakPointMultiplier = 2f;

        [Header("Loot Rewards")]
        [SerializeField] private int guaranteedLootDrops = 5;
        [SerializeField] private float legendaryDropChance = 0.3f;
        [SerializeField] private float epicDropChance = 0.5f;
        [SerializeField] private int softCurrencyReward = 5000;

        [Header("Visual Effects")]
        [SerializeField] private ParticleSystem phaseChangeVFX;
        [SerializeField] private ParticleSystem abilityChargeVFX;
        [SerializeField] private GameObject armorVisuals;
        [SerializeField] private Light bossLight;

        [Header("Audio")]
        [SerializeField] private AudioClip roarSound;
        [SerializeField] private AudioClip phaseChangeSound;
        [SerializeField] private AudioClip abilitySound;
        [SerializeField] private AudioClip deathSound;

        // Boss state
        private NetworkVariable<int> currentPhaseIndex = new NetworkVariable<int>(0);
        private NetworkVariable<bool> isInPhaseTransition = new NetworkVariable<bool>(false);
        private NetworkVariable<bool> isUsingAbility = new NetworkVariable<bool>(false);

        private float lastAbilityTime;
        private float lastMinionSpawnTime;
        private float armorHealth;
        private bool armorBroken;

        // Target tracking
        private Transform currentTarget;
        private List<Transform> nearbyPlayers = new List<Transform>();
        private float targetUpdateInterval = 1f;
        private float lastTargetUpdate;

        // Enrage state
        private bool isEnraged;
        private float enrageMultiplier = 1.5f;

        protected override void Start()
        {
            base.Start();

            if (IsServer)
            {
                InitializeBoss();
            }
        }

        protected override void Update()
        {
            if (!IsServer) return;

            base.Update();

            UpdateTargeting();
            UpdatePhase();
            UpdateAbilities();
            UpdateMinions();
            UpdateVisuals();
        }

        #region Initialization

        private void InitializeBoss()
        {
            // Set boss stats
            health = bossHealth;
            maxHealth = bossHealth;
            moveSpeed = bossSpeed;
            damage = bossDamage;
            detectionRadius = detectionRange;

            armorHealth = bossHealth * 0.3f; // Armor is 30% of total health

            // Start in first phase
            currentPhaseIndex.Value = 0;
            ApplyPhaseModifiers(phases[0]);

            // Play spawn effects
            if (roarSound != null)
                Core.AudioManager.Instance?.PlaySFX(roarSound, transform.position);

            // Notify players
            NotifyBossSpawnClientRpc(bossName);

            Debug.Log($"[BossZombie] {bossName} spawned with {bossHealth} health");
        }

        #endregion

        #region Targeting

        private void UpdateTargeting()
        {
            if (Time.time - lastTargetUpdate < targetUpdateInterval) return;
            lastTargetUpdate = Time.time;

            // Find all players in range
            nearbyPlayers.Clear();
            Collider[] hits = Physics.OverlapSphere(transform.position, detectionRange, LayerMask.GetMask("Player"));

            foreach (var hit in hits)
            {
                var health = hit.GetComponent<Player.PlayerHealth>();
                if (health != null && health.IsAlive)
                {
                    nearbyPlayers.Add(hit.transform);
                }
            }

            // Select target based on threat
            if (nearbyPlayers.Count > 0)
            {
                currentTarget = SelectHighestThreatTarget();
            }
            else
            {
                currentTarget = null;
            }
        }

        private Transform SelectHighestThreatTarget()
        {
            if (nearbyPlayers.Count == 0) return null;

            // Simple threat: closest player
            // In production, would track damage dealt, healing, etc.
            return nearbyPlayers.OrderBy(p => Vector3.Distance(transform.position, p.position)).First();
        }

        #endregion

        #region Phase System

        private void UpdatePhase()
        {
            if (phases == null || phases.Length == 0) return;
            if (isInPhaseTransition.Value) return;

            float healthPercent = health / maxHealth;
            int newPhaseIndex = currentPhaseIndex.Value;

            // Check if should transition to next phase
            for (int i = phases.Length - 1; i > currentPhaseIndex.Value; i--)
            {
                if (healthPercent <= phases[i].healthThreshold)
                {
                    newPhaseIndex = i;
                    break;
                }
            }

            if (newPhaseIndex != currentPhaseIndex.Value)
            {
                TransitionToPhase(newPhaseIndex);
            }
        }

        private void TransitionToPhase(int phaseIndex)
        {
            if (phaseIndex < 0 || phaseIndex >= phases.Length) return;

            isInPhaseTransition.Value = true;
            currentPhaseIndex.Value = phaseIndex;

            // Play transition effects
            if (phaseChangeVFX != null)
                phaseChangeVFX.Play();

            if (phaseChangeSound != null)
                Core.AudioManager.Instance?.PlaySFX(phaseChangeSound, transform.position);

            // Apply phase modifiers
            var phase = phases[phaseIndex];
            ApplyPhaseModifiers(phase);

            // Notify players
            NotifyPhaseChangeClientRpc(phaseIndex, phase.phaseName);

            // End transition
            Invoke(nameof(EndPhaseTransition), phaseTransitionDuration);

            Debug.Log($"[BossZombie] {bossName} transitioned to phase {phaseIndex}: {phase.phaseName}");
        }

        private void ApplyPhaseModifiers(BossPhase phase)
        {
            moveSpeed = bossSpeed * phase.speedMultiplier;
            damage = bossDamage * phase.damageMultiplier;

            if (phase.enablesEnrage)
            {
                isEnraged = true;
                moveSpeed *= enrageMultiplier;
                damage *= enrageMultiplier;
            }
        }

        private void EndPhaseTransition()
        {
            isInPhaseTransition.Value = false;
        }

        #endregion

        #region Abilities

        private void UpdateAbilities()
        {
            if (isInPhaseTransition.Value || isUsingAbility.Value) return;
            if (Time.time - lastAbilityTime < abilityCooldown) return;
            if (currentTarget == null) return;

            // Select and use random ability from current phase
            var phase = phases[currentPhaseIndex.Value];
            if (phase.availableAbilities == null || phase.availableAbilities.Length == 0) return;

            var ability = phase.availableAbilities[Random.Range(0, phase.availableAbilities.Length)];
            UseAbility(ability);
        }

        private void UseAbility(BossAbility ability)
        {
            isUsingAbility.Value = true;
            lastAbilityTime = Time.time;

            // Play charge effects
            if (abilityChargeVFX != null)
                abilityChargeVFX.Play();

            // Execute ability after charge time
            Invoke(nameof(ExecuteCurrentAbility), ability.chargeTime);

            // Store current ability for execution
            currentAbility = ability;
        }

        private BossAbility currentAbility;

        private void ExecuteCurrentAbility()
        {
            if (currentAbility == null) return;

            switch (currentAbility.abilityType)
            {
                case BossAbilityType.GroundSlam:
                    ExecuteGroundSlam();
                    break;

                case BossAbilityType.ChargeAttack:
                    ExecuteChargeAttack();
                    break;

                case BossAbilityType.RoarStun:
                    ExecuteRoarStun();
                    break;

                case BossAbilityType.AcidSpit:
                    ExecuteAcidSpit();
                    break;

                case BossAbilityType.SummonMinions:
                    ExecuteSummonMinions();
                    break;

                case BossAbilityType.Regenerate:
                    ExecuteRegenerate();
                    break;
            }

            // Play ability sound
            if (abilitySound != null)
                Core.AudioManager.Instance?.PlaySFX(abilitySound, transform.position);

            isUsingAbility.Value = false;
            currentAbility = null;
        }

        private void ExecuteGroundSlam()
        {
            // AOE damage around boss
            Collider[] hits = Physics.OverlapSphere(transform.position, 15f, LayerMask.GetMask("Player"));
            foreach (var hit in hits)
            {
                var health = hit.GetComponent<Player.PlayerHealth>();
                if (health != null && health.IsAlive)
                {
                    float distance = Vector3.Distance(transform.position, hit.transform.position);
                    float damageFalloff = 1f - (distance / 15f);
                    health.TakeDamage(bossDamage * 2f * damageFalloff);

                    // Apply knockback
                    Vector3 knockbackDir = (hit.transform.position - transform.position).normalized;
                    // TODO: Apply knockback force
                }
            }

            Debug.Log($"[BossZombie] {bossName} used Ground Slam");
        }

        private void ExecuteChargeAttack()
        {
            if (currentTarget == null) return;

            // Charge at target
            Vector3 chargeDir = (currentTarget.position - transform.position).normalized;
            float chargeDistance = 20f;

            // TODO: Implement charge movement with collision damage

            Debug.Log($"[BossZombie] {bossName} used Charge Attack");
        }

        private void ExecuteRoarStun()
        {
            // Stun all nearby players
            Collider[] hits = Physics.OverlapSphere(transform.position, 25f, LayerMask.GetMask("Player"));
            foreach (var hit in hits)
            {
                var movement = hit.GetComponent<Player.PlayerMovement>();
                if (movement != null)
                {
                    // TODO: Apply stun effect
                    movement.SetSpeedMultiplier(0f);
                    StartCoroutine(RemoveStunAfterDelay(movement, 3f));
                }
            }

            Debug.Log($"[BossZombie] {bossName} used Roar Stun");
        }

        private System.Collections.IEnumerator RemoveStunAfterDelay(Player.PlayerMovement movement, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (movement != null)
                movement.SetSpeedMultiplier(1f);
        }

        private void ExecuteAcidSpit()
        {
            if (currentTarget == null) return;

            // Spawn acid projectile
            // TODO: Create acid projectile with DOT damage

            Debug.Log($"[BossZombie] {bossName} used Acid Spit");
        }

        private void ExecuteSummonMinions()
        {
            SpawnMinions();
        }

        private void ExecuteRegenerate()
        {
            // Heal 10% of max health
            float healAmount = maxHealth * 0.1f;
            health = Mathf.Min(health + healAmount, maxHealth);

            Debug.Log($"[BossZombie] {bossName} regenerated {healAmount} health");
        }

        #endregion

        #region Minion Management

        private void UpdateMinions()
        {
            if (!canSpawnMinions) return;
            if (Time.time - lastMinionSpawnTime < minionSpawnInterval) return;

            if (nearbyPlayers.Count > 0)
            {
                SpawnMinions();
                lastMinionSpawnTime = Time.time;
            }
        }

        private void SpawnMinions()
        {
            if (ZombieManager.Instance == null) return;

            for (int i = 0; i < minionsPerSpawn; i++)
            {
                Vector3 spawnPos = transform.position + Random.insideUnitSphere * minionSpawnRange;
                spawnPos.y = transform.position.y;

                ZombieManager.Instance.SpawnZombie("Common", spawnPos);
            }

            Debug.Log($"[BossZombie] {bossName} spawned {minionsPerSpawn} minions");
        }

        #endregion

        #region Damage System

        public override void TakeDamage(float damageAmount, Vector3 hitPoint = default, Transform attacker = null)
        {
            if (!IsServer) return;

            // Check if hit weak point
            bool isWeakPoint = false;
            if (hasWeakPoints && weakPoints != null)
            {
                foreach (var weakPoint in weakPoints)
                {
                    if (weakPoint != null && Vector3.Distance(hitPoint, weakPoint.position) < 1f)
                    {
                        isWeakPoint = true;
                        damageAmount *= weakPointMultiplier;
                        break;
                    }
                }
            }

            // Apply armor reduction
            if (hasArmor && !armorBroken)
            {
                float armorDamage = damageAmount * armorReduction;
                armorHealth -= armorDamage;

                if (armorHealth <= 0)
                {
                    armorBroken = true;
                    if (armorVisuals != null)
                        armorVisuals.SetActive(false);

                    NotifyArmorBreakClientRpc();
                    Debug.Log($"[BossZombie] {bossName}'s armor broken!");
                }

                damageAmount -= armorDamage;
            }

            base.TakeDamage(damageAmount, hitPoint, attacker);
        }

        protected override void Die()
        {
            if (deathSound != null)
                Core.AudioManager.Instance?.PlaySFX(deathSound, transform.position);

            // Spawn loot
            SpawnBossLoot();

            // Notify AI Director
            if (AIDirector.Instance != null)
            {
                AIDirector.Instance.OnZombieDeath();
            }

            // Notify players of boss death
            NotifyBossDefeatedClientRpc(bossName);

            base.Die();
        }

        private void SpawnBossLoot()
        {
            if (Gameplay.InventoryManager.Instance == null) return;

            Vector3 lootCenter = transform.position;

            for (int i = 0; i < guaranteedLootDrops; i++)
            {
                // Determine rarity
                Gameplay.ItemRarity rarity = RollLootRarity();

                // Spawn loot (placeholder - would use loot table in production)
                Vector3 lootPos = lootCenter + Random.insideUnitSphere * 3f;
                lootPos.y = lootCenter.y + 1f;

                // TODO: Spawn actual loot items based on rarity
            }

            Debug.Log($"[BossZombie] Spawned {guaranteedLootDrops} loot items from {bossName}");
        }

        private Gameplay.ItemRarity RollLootRarity()
        {
            float roll = Random.value;

            if (roll < legendaryDropChance)
                return Gameplay.ItemRarity.Legendary;
            else if (roll < legendaryDropChance + epicDropChance)
                return Gameplay.ItemRarity.Epic;
            else
                return Gameplay.ItemRarity.Rare;
        }

        #endregion

        #region Visuals

        private void UpdateVisuals()
        {
            if (bossLight != null)
            {
                // Pulse light based on health
                float intensity = Mathf.Lerp(1f, 3f, 1f - (health / maxHealth));
                bossLight.intensity = intensity;

                // Change color based on phase
                if (isEnraged)
                    bossLight.color = Color.red;
                else
                    bossLight.color = Color.yellow;
            }
        }

        #endregion

        #region Client RPCs

        [ClientRpc]
        private void NotifyBossSpawnClientRpc(string name)
        {
            Core.NotificationManager.Instance?.ShowNotification(
                $"BOSS SPAWNED: {name}",
                NotificationType.Danger,
                5f
            );
        }

        [ClientRpc]
        private void NotifyPhaseChangeClientRpc(int phaseIndex, string phaseName)
        {
            Core.NotificationManager.Instance?.ShowNotification(
                $"{bossName} entered {phaseName}!",
                NotificationType.Warning,
                4f
            );
        }

        [ClientRpc]
        private void NotifyArmorBreakClientRpc()
        {
            Core.NotificationManager.Instance?.ShowNotification(
                $"{bossName}'s armor broken!",
                NotificationType.Success,
                3f
            );
        }

        [ClientRpc]
        private void NotifyBossDefeatedClientRpc(string name)
        {
            Core.NotificationManager.Instance?.ShowNotification(
                $"BOSS DEFEATED: {name}",
                NotificationType.Success,
                5f
            );
        }

        #endregion

        private void OnDrawGizmosSelected()
        {
            // Draw detection range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            // Draw weak points
            if (hasWeakPoints && weakPoints != null)
            {
                Gizmos.color = Color.yellow;
                foreach (var weakPoint in weakPoints)
                {
                    if (weakPoint != null)
                        Gizmos.DrawWireSphere(weakPoint.position, 1f);
                }
            }
        }
    }

    #region Supporting Classes

    [System.Serializable]
    public class BossPhase
    {
        public string phaseName;
        public float healthThreshold; // Trigger when health drops below this (0-1)
        public float speedMultiplier = 1f;
        public float damageMultiplier = 1f;
        public bool enablesEnrage;
        public BossAbility[] availableAbilities;
    }

    [System.Serializable]
    public class BossAbility
    {
        public string abilityName;
        public BossAbilityType abilityType;
        public float chargeTime = 1f;
        public float cooldown = 10f;
        public float damage;
        public float range;
    }

    public enum BossAbilityType
    {
        GroundSlam,     // AOE damage + knockback
        ChargeAttack,   // Linear charge with collision damage
        RoarStun,       // AOE stun
        AcidSpit,       // Ranged projectile with DOT
        SummonMinions,  // Spawn zombie adds
        Regenerate      // Heal self
    }

    #endregion
}
