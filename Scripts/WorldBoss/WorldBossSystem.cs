using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.WorldBoss
{
    /// <summary>
    /// Comprehensive world boss and raid encounter system.
    /// Manages boss spawning, phases, mechanics, loot distribution, and raid coordination.
    /// </summary>
    public class WorldBossSystem : NetworkBehaviour
    {
        public static WorldBossSystem Instance { get; private set; }

        [Header("Boss Configuration")]
        [SerializeField] private int maxActiveBosses = 5;
        [SerializeField] private float bossSpawnCheckInterval = 300f; // 5 minutes
        [SerializeField] private int maxRaidSize = 40;
        [SerializeField] private int minRaidSize = 5;

        [Header("Difficulty Scaling")]
        [SerializeField] private bool enableDynamicScaling = true;
        [SerializeField] private float healthPerPlayer = 100000f;
        [SerializeField] private float damagePerPlayer = 100f;

        [Header("Loot Distribution")]
        [SerializeField] private LootDistributionMode lootMode = LootDistributionMode.PersonalLoot;
        [SerializeField] private int minLootDrops = 3;
        [SerializeField] private int maxLootDrops = 10;
        [SerializeField] private float rareDropChance = 0.1f;
        [SerializeField] private float legendaryDropChance = 0.01f;

        [Header("Enrage Mechanics")]
        [SerializeField] private bool enableEnrage = true;
        [SerializeField] private float defaultEnrageTimer = 600f; // 10 minutes
        [SerializeField] private float enrageDamageMultiplier = 3f;

        // Data structures
        private Dictionary<string, WorldBoss> activeBosses = new Dictionary<string, WorldBoss>();
        private Dictionary<string, RaidEncounter> activeRaids = new Dictionary<string, RaidEncounter>();
        private Dictionary<string, BossDefinition> bossDefinitions = new Dictionary<string, BossDefinition>();
        private Dictionary<ulong, RaidParticipation> playerParticipation = new Dictionary<ulong, RaidParticipation>();
        private Dictionary<string, BossSchedule> bossSchedules = new Dictionary<string, BossSchedule>();

        // Events
        public event Action<WorldBoss> OnBossSpawned;
        public event Action<string, ulong> OnBossDamaged;
        public event Action<string> OnBossPhaseChanged;
        public event Action<string, List<ulong>> OnBossDefeated;
        public event Action<string> OnBossEnraged;
        public event Action<string, string> OnBossAbilityUsed;
        public event Action<ulong, LootDrop> OnLootAwarded;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                InitializeBossDefinitions();
                InitializeBossSchedules();
                InvokeRepeating(nameof(CheckBossSpawns), 0f, bossSpawnCheckInterval);
                InvokeRepeating(nameof(UpdateActiveBosses), 1f, 1f);
            }
        }

        private void InitializeBossDefinitions()
        {
            // Define world bosses
            bossDefinitions["zombie_titan"] = new BossDefinition
            {
                bossId = "zombie_titan",
                bossName = "Zombie Titan",
                bossType = BossType.WorldBoss,
                tier = BossTier.Legendary,
                baseHealth = 1000000,
                baseDamage = 500,
                baseArmor = 100,
                recommendedPlayers = 20,
                enrageTimer = 600f,
                phases = new List<BossPhase>
                {
                    new BossPhase { phaseNumber = 1, healthThreshold = 100f, abilities = new List<string> { "slam", "roar" } },
                    new BossPhase { phaseNumber = 2, healthThreshold = 66f, abilities = new List<string> { "slam", "roar", "summon_adds" } },
                    new BossPhase { phaseNumber = 3, healthThreshold = 33f, abilities = new List<string> { "slam", "roar", "summon_adds", "ground_pound" } }
                },
                abilities = new List<BossAbility>
                {
                    new BossAbility { abilityId = "slam", cooldown = 10f, damage = 1000, range = 5f },
                    new BossAbility { abilityId = "roar", cooldown = 30f, damage = 0, range = 20f, effectType = AbilityEffect.Fear },
                    new BossAbility { abilityId = "summon_adds", cooldown = 45f, damage = 0, range = 0f, effectType = AbilityEffect.SummonCreatures },
                    new BossAbility { abilityId = "ground_pound", cooldown = 60f, damage = 2000, range = 15f, effectType = AbilityEffect.AreaDamage }
                },
                lootTable = new List<LootTableEntry>
                {
                    new LootTableEntry { itemId = "legendary_weapon", dropChance = 0.05f, quantity = 1 },
                    new LootTableEntry { itemId = "rare_armor", dropChance = 0.2f, quantity = 1 },
                    new LootTableEntry { itemId = "boss_currency", dropChance = 1f, quantity = 100 }
                }
            };

            bossDefinitions["plague_lord"] = new BossDefinition
            {
                bossId = "plague_lord",
                bossName = "Plague Lord",
                bossType = BossType.RaidBoss,
                tier = BossTier.Mythic,
                baseHealth = 5000000,
                baseDamage = 800,
                baseArmor = 200,
                recommendedPlayers = 40,
                enrageTimer = 900f,
                phases = new List<BossPhase>
                {
                    new BossPhase { phaseNumber = 1, healthThreshold = 100f, abilities = new List<string> { "plague_nova", "toxic_cloud" } },
                    new BossPhase { phaseNumber = 2, healthThreshold = 75f, abilities = new List<string> { "plague_nova", "toxic_cloud", "death_grip" } },
                    new BossPhase { phaseNumber = 3, healthThreshold = 50f, abilities = new List<string> { "plague_nova", "toxic_cloud", "death_grip", "disease_outbreak" } },
                    new BossPhase { phaseNumber = 4, healthThreshold = 25f, abilities = new List<string> { "plague_nova", "toxic_cloud", "death_grip", "disease_outbreak", "apocalypse" } }
                },
                abilities = new List<BossAbility>
                {
                    new BossAbility { abilityId = "plague_nova", cooldown = 15f, damage = 1500, range = 10f, effectType = AbilityEffect.DoT },
                    new BossAbility { abilityId = "toxic_cloud", cooldown = 20f, damage = 500, range = 15f, effectType = AbilityEffect.AreaDoT },
                    new BossAbility { abilityId = "death_grip", cooldown = 30f, damage = 2000, range = 30f, effectType = AbilityEffect.Pull },
                    new BossAbility { abilityId = "disease_outbreak", cooldown = 60f, damage = 3000, range = 20f, effectType = AbilityEffect.Spread },
                    new BossAbility { abilityId = "apocalypse", cooldown = 120f, damage = 5000, range = 50f, effectType = AbilityEffect.Wipe }
                },
                lootTable = new List<LootTableEntry>
                {
                    new LootTableEntry { itemId = "mythic_weapon", dropChance = 0.02f, quantity = 1 },
                    new LootTableEntry { itemId = "legendary_armor_set", dropChance = 0.1f, quantity = 1 },
                    new LootTableEntry { itemId = "raid_token", dropChance = 1f, quantity = 50 }
                }
            };

            bossDefinitions["necromancer_king"] = new BossDefinition
            {
                bossId = "necromancer_king",
                bossName = "Necromancer King",
                bossType = BossType.WorldBoss,
                tier = BossTier.Epic,
                baseHealth = 2000000,
                baseDamage = 600,
                baseArmor = 150,
                recommendedPlayers = 25,
                enrageTimer = 720f,
                phases = new List<BossPhase>
                {
                    new BossPhase { phaseNumber = 1, healthThreshold = 100f, abilities = new List<string> { "dark_bolt", "raise_dead" } },
                    new BossPhase { phaseNumber = 2, healthThreshold = 50f, abilities = new List<string> { "dark_bolt", "raise_dead", "soul_drain" } }
                },
                abilities = new List<BossAbility>
                {
                    new BossAbility { abilityId = "dark_bolt", cooldown = 5f, damage = 800, range = 50f },
                    new BossAbility { abilityId = "raise_dead", cooldown = 30f, damage = 0, range = 0f, effectType = AbilityEffect.SummonCreatures },
                    new BossAbility { abilityId = "soul_drain", cooldown = 45f, damage = 1500, range = 30f, effectType = AbilityEffect.LifeSteal }
                },
                lootTable = new List<LootTableEntry>
                {
                    new LootTableEntry { itemId = "epic_staff", dropChance = 0.15f, quantity = 1 },
                    new LootTableEntry { itemId = "necromancer_robe", dropChance = 0.25f, quantity = 1 }
                }
            };
        }

        private void InitializeBossSchedules()
        {
            // Schedule world bosses to spawn at specific times
            bossSchedules["zombie_titan_schedule"] = new BossSchedule
            {
                scheduleId = "zombie_titan_schedule",
                bossId = "zombie_titan",
                spawnLocations = new List<Vector3>
                {
                    new Vector3(100, 0, 100),
                    new Vector3(-100, 0, -100),
                    new Vector3(200, 0, -200)
                },
                spawnIntervalMinutes = 180, // 3 hours
                lastSpawnTime = DateTime.MinValue,
                isActive = true
            };

            bossSchedules["plague_lord_schedule"] = new BossSchedule
            {
                scheduleId = "plague_lord_schedule",
                bossId = "plague_lord",
                spawnLocations = new List<Vector3>
                {
                    new Vector3(500, 0, 500)
                },
                spawnIntervalMinutes = 720, // 12 hours
                lastSpawnTime = DateTime.MinValue,
                isActive = true
            };
        }

        private void CheckBossSpawns()
        {
            if (activeBosses.Count >= maxActiveBosses)
                return;

            foreach (var schedule in bossSchedules.Values)
            {
                if (!schedule.isActive)
                    continue;

                var timeSinceLastSpawn = DateTime.UtcNow - schedule.lastSpawnTime;
                if (timeSinceLastSpawn.TotalMinutes >= schedule.spawnIntervalMinutes)
                {
                    SpawnScheduledBoss(schedule);
                    schedule.lastSpawnTime = DateTime.UtcNow;
                }
            }
        }

        private void SpawnScheduledBoss(BossSchedule schedule)
        {
            if (!bossDefinitions.TryGetValue(schedule.bossId, out var definition))
                return;

            var spawnLocation = schedule.spawnLocations[UnityEngine.Random.Range(0, schedule.spawnLocations.Count)];
            SpawnBoss(definition, spawnLocation);
        }

        private void UpdateActiveBosses()
        {
            var bossesToRemove = new List<string>();

            foreach (var boss in activeBosses.Values)
            {
                if (boss.state == BossState.Dead)
                {
                    bossesToRemove.Add(boss.instanceId);
                    continue;
                }

                if (boss.state != BossState.InCombat)
                    continue;

                // Update enrage timer
                if (enableEnrage && !boss.isEnraged)
                {
                    boss.enrageTimer -= Time.deltaTime;
                    if (boss.enrageTimer <= 0)
                    {
                        EnrageBoss(boss);
                    }
                }

                // Update abilities
                UpdateBossAbilities(boss);

                // Check phase transitions
                CheckPhaseTransition(boss);
            }

            foreach (var bossId in bossesToRemove)
            {
                CleanupBoss(bossId);
            }
        }

        private void UpdateBossAbilities(WorldBoss boss)
        {
            var currentPhase = GetCurrentPhase(boss);
            if (currentPhase == null)
                return;

            foreach (var abilityId in currentPhase.abilities)
            {
                var ability = boss.definition.abilities.FirstOrDefault(a => a.abilityId == abilityId);
                if (ability == null)
                    continue;

                if (!boss.abilityCooldowns.ContainsKey(abilityId))
                {
                    boss.abilityCooldowns[abilityId] = 0f;
                }

                boss.abilityCooldowns[abilityId] -= Time.deltaTime;

                if (boss.abilityCooldowns[abilityId] <= 0f)
                {
                    UseBossAbility(boss, ability);
                    boss.abilityCooldowns[abilityId] = ability.cooldown;
                }
            }
        }

        private void UseBossAbility(WorldBoss boss, BossAbility ability)
        {
            OnBossAbilityUsed?.Invoke(boss.instanceId, ability.abilityId);
            BroadcastBossAbilityClientRpc(boss.instanceId, ability.abilityId);

            // Apply ability effects to nearby players
            var playersInRange = GetPlayersInRange(boss.position, ability.range);
            foreach (var playerId in playersInRange)
            {
                ApplyAbilityEffect(playerId, boss, ability);
            }

            Debug.Log($"Boss {boss.definition.bossName} used ability {ability.abilityId}");
        }

        private List<ulong> GetPlayersInRange(Vector3 position, float range)
        {
            // Placeholder - integrate with your player management system
            return new List<ulong>();
        }

        private void ApplyAbilityEffect(ulong playerId, WorldBoss boss, BossAbility ability)
        {
            float damage = ability.damage;

            if (boss.isEnraged)
            {
                damage *= enrageDamageMultiplier;
            }

            // This would integrate with your player health/combat system
            // PlayerHealthSystem.Instance?.DamagePlayer(playerId, damage);

            switch (ability.effectType)
            {
                case AbilityEffect.Fear:
                    // Apply fear effect
                    break;
                case AbilityEffect.DoT:
                    // Apply damage over time
                    break;
                case AbilityEffect.SummonCreatures:
                    SummonBossAdds(boss);
                    break;
                case AbilityEffect.Pull:
                    // Pull player towards boss
                    break;
                case AbilityEffect.Wipe:
                    // Potential raid wipe mechanic
                    break;
            }
        }

        private void SummonBossAdds(WorldBoss boss)
        {
            int addCount = UnityEngine.Random.Range(3, 8);
            for (int i = 0; i < addCount; i++)
            {
                Vector3 spawnPos = boss.position + UnityEngine.Random.insideUnitSphere * 10f;
                spawnPos.y = boss.position.y;
                // Spawn add creature
            }
        }

        private void CheckPhaseTransition(WorldBoss boss)
        {
            float healthPercent = (boss.currentHealth / boss.maxHealth) * 100f;
            int currentPhaseNumber = boss.currentPhase;

            foreach (var phase in boss.definition.phases)
            {
                if (phase.phaseNumber > currentPhaseNumber && healthPercent <= phase.healthThreshold)
                {
                    TransitionToPhase(boss, phase.phaseNumber);
                    break;
                }
            }
        }

        private void TransitionToPhase(WorldBoss boss, int phaseNumber)
        {
            boss.currentPhase = phaseNumber;
            boss.abilityCooldowns.Clear(); // Reset ability cooldowns on phase change

            OnBossPhaseChanged?.Invoke(boss.instanceId);
            BroadcastBossPhaseChangedClientRpc(boss.instanceId, phaseNumber);

            Debug.Log($"Boss {boss.definition.bossName} transitioned to phase {phaseNumber}");
        }

        private BossPhase GetCurrentPhase(WorldBoss boss)
        {
            return boss.definition.phases.FirstOrDefault(p => p.phaseNumber == boss.currentPhase);
        }

        private void EnrageBoss(WorldBoss boss)
        {
            boss.isEnraged = true;
            OnBossEnraged?.Invoke(boss.instanceId);
            BroadcastBossEnragedClientRpc(boss.instanceId);

            Debug.Log($"Boss {boss.definition.bossName} is now enraged!");
        }

        #region Server RPCs

        [ServerRpc(RequireOwnership = false)]
        public void SpawnBossServerRpc(string bossId, Vector3 position, ServerRpcParams rpcParams = default)
        {
            if (!bossDefinitions.TryGetValue(bossId, out var definition))
            {
                Debug.LogWarning($"Boss definition not found: {bossId}");
                return;
            }

            SpawnBoss(definition, position);
        }

        private void SpawnBoss(BossDefinition definition, Vector3 position)
        {
            var boss = new WorldBoss
            {
                instanceId = $"boss_{Guid.NewGuid()}",
                definition = definition,
                spawnTime = DateTime.UtcNow,
                position = position,
                state = BossState.Idle,
                currentPhase = 1,
                maxHealth = CalculateBossHealth(definition),
                currentHealth = 0,
                isEnraged = false,
                enrageTimer = definition.enrageTimer,
                participants = new List<ulong>(),
                damageDealt = new Dictionary<ulong, float>(),
                abilityCooldowns = new Dictionary<string, float>()
            };

            boss.currentHealth = boss.maxHealth;
            activeBosses[boss.instanceId] = boss;

            OnBossSpawned?.Invoke(boss);
            BroadcastBossSpawnedClientRpc(boss.instanceId, definition.bossId, position);

            Debug.Log($"Spawned boss: {definition.bossName} at {position}");
        }

        private float CalculateBossHealth(BossDefinition definition)
        {
            if (!enableDynamicScaling)
                return definition.baseHealth;

            // Scale based on number of nearby players or expected raid size
            int playerCount = Mathf.Max(definition.recommendedPlayers, minRaidSize);
            return definition.baseHealth + (healthPerPlayer * playerCount);
        }

        [ServerRpc(RequireOwnership = false)]
        public void DamageBossServerRpc(string bossInstanceId, ulong attackerId, float damage, ServerRpcParams rpcParams = default)
        {
            if (!activeBosses.TryGetValue(bossInstanceId, out var boss))
            {
                Debug.LogWarning($"Boss not found: {bossInstanceId}");
                return;
            }

            // Start combat if not already in combat
            if (boss.state == BossState.Idle)
            {
                boss.state = BossState.InCombat;
            }

            // Add participant
            if (!boss.participants.Contains(attackerId))
            {
                boss.participants.Add(attackerId);
                InitializePlayerParticipation(attackerId, bossInstanceId);
            }

            // Track damage
            boss.damageDealt[attackerId] = boss.damageDealt.GetValueOrDefault(attackerId) + damage;
            boss.currentHealth -= damage;

            // Update participation
            if (playerParticipation.ContainsKey(attackerId))
            {
                playerParticipation[attackerId].damageDealt += damage;
            }

            OnBossDamaged?.Invoke(bossInstanceId, attackerId);

            // Check if boss is defeated
            if (boss.currentHealth <= 0)
            {
                DefeatBoss(boss);
            }
        }

        private void DefeatBoss(WorldBoss boss)
        {
            boss.state = BossState.Dead;
            boss.defeatTime = DateTime.UtcNow;

            // Calculate loot
            DistributeLoot(boss);

            OnBossDefeated?.Invoke(boss.instanceId, boss.participants);
            BroadcastBossDefeatedClientRpc(boss.instanceId);

            Debug.Log($"Boss {boss.definition.bossName} defeated by {boss.participants.Count} players");
        }

        private void InitializePlayerParticipation(ulong playerId, string bossInstanceId)
        {
            if (!playerParticipation.ContainsKey(playerId))
            {
                playerParticipation[playerId] = new RaidParticipation
                {
                    playerId = playerId,
                    bossInstanceId = bossInstanceId,
                    joinTime = DateTime.UtcNow,
                    damageDealt = 0,
                    healingDone = 0,
                    damageTaken = 0,
                    deathCount = 0,
                    lootReceived = new List<string>()
                };
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void JoinRaidServerRpc(string bossInstanceId, ulong playerId, ServerRpcParams rpcParams = default)
        {
            if (!activeBosses.TryGetValue(bossInstanceId, out var boss))
            {
                return;
            }

            if (boss.participants.Count >= maxRaidSize)
            {
                Debug.LogWarning($"Raid is full");
                return;
            }

            if (!boss.participants.Contains(playerId))
            {
                boss.participants.Add(playerId);
                InitializePlayerParticipation(playerId, bossInstanceId);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void LeaveRaidServerRpc(string bossInstanceId, ulong playerId, ServerRpcParams rpcParams = default)
        {
            if (!activeBosses.TryGetValue(bossInstanceId, out var boss))
            {
                return;
            }

            boss.participants.Remove(playerId);
            playerParticipation.Remove(playerId);
        }

        #endregion

        #region Loot Distribution

        private void DistributeLoot(WorldBoss boss)
        {
            switch (lootMode)
            {
                case LootDistributionMode.PersonalLoot:
                    DistributePersonalLoot(boss);
                    break;
                case LootDistributionMode.GroupLoot:
                    DistributeGroupLoot(boss);
                    break;
                case LootDistributionMode.MasterLoot:
                    DistributeMasterLoot(boss);
                    break;
            }
        }

        private void DistributePersonalLoot(WorldBoss boss)
        {
            foreach (var playerId in boss.participants)
            {
                var lootDrops = RollLoot(boss.definition.lootTable);
                foreach (var loot in lootDrops)
                {
                    AwardLoot(playerId, loot);
                }
            }
        }

        private void DistributeGroupLoot(WorldBoss boss)
        {
            var lootDrops = RollLoot(boss.definition.lootTable);
            int dropsPerPlayer = Mathf.Max(1, lootDrops.Count / boss.participants.Count);

            // Sort participants by damage contribution
            var sortedParticipants = boss.participants
                .OrderByDescending(p => boss.damageDealt.GetValueOrDefault(p))
                .ToList();

            for (int i = 0; i < lootDrops.Count && i < sortedParticipants.Count; i++)
            {
                AwardLoot(sortedParticipants[i % sortedParticipants.Count], lootDrops[i]);
            }
        }

        private void DistributeMasterLoot(WorldBoss boss)
        {
            // Master looter would distribute manually
            var lootDrops = RollLoot(boss.definition.lootTable);
            // Store loot for master looter to distribute
        }

        private List<LootDrop> RollLoot(List<LootTableEntry> lootTable)
        {
            var drops = new List<LootDrop>();

            foreach (var entry in lootTable)
            {
                if (UnityEngine.Random.value <= entry.dropChance)
                {
                    drops.Add(new LootDrop
                    {
                        itemId = entry.itemId,
                        quantity = entry.quantity,
                        rarity = DetermineRarity()
                    });
                }
            }

            return drops;
        }

        private ItemRarity DetermineRarity()
        {
            float roll = UnityEngine.Random.value;

            if (roll <= legendaryDropChance)
                return ItemRarity.Legendary;
            if (roll <= rareDropChance)
                return ItemRarity.Rare;

            return ItemRarity.Common;
        }

        private void AwardLoot(ulong playerId, LootDrop loot)
        {
            // This would integrate with your inventory system
            // Inventory.InventoryManager.Instance?.AddItem(playerId, loot.itemId, loot.quantity);

            if (playerParticipation.ContainsKey(playerId))
            {
                playerParticipation[playerId].lootReceived.Add(loot.itemId);
            }

            OnLootAwarded?.Invoke(playerId, loot);
            NotifyLootAwardedClientRpc(playerId, loot.itemId, loot.quantity);

            Debug.Log($"Awarded loot to player {playerId}: {loot.itemId} x{loot.quantity}");
        }

        #endregion

        #region Cleanup

        private void CleanupBoss(string bossInstanceId)
        {
            if (!activeBosses.ContainsKey(bossInstanceId))
                return;

            var boss = activeBosses[bossInstanceId];

            // Clean up player participation
            foreach (var playerId in boss.participants)
            {
                playerParticipation.Remove(playerId);
            }

            activeBosses.Remove(bossInstanceId);
            Debug.Log($"Cleaned up boss: {bossInstanceId}");
        }

        #endregion

        #region Client RPCs

        [ClientRpc]
        private void BroadcastBossSpawnedClientRpc(string instanceId, string bossId, Vector3 position)
        {
            // Client-side boss spawn notification
        }

        [ClientRpc]
        private void BroadcastBossPhaseChangedClientRpc(string instanceId, int phaseNumber)
        {
            OnBossPhaseChanged?.Invoke(instanceId);
        }

        [ClientRpc]
        private void BroadcastBossEnragedClientRpc(string instanceId)
        {
            OnBossEnraged?.Invoke(instanceId);
        }

        [ClientRpc]
        private void BroadcastBossAbilityClientRpc(string instanceId, string abilityId)
        {
            OnBossAbilityUsed?.Invoke(instanceId, abilityId);
        }

        [ClientRpc]
        private void BroadcastBossDefeatedClientRpc(string instanceId)
        {
            // Client-side boss defeat notification
        }

        [ClientRpc]
        private void NotifyLootAwardedClientRpc(ulong playerId, string itemId, int quantity)
        {
            // Client-side loot notification
        }

        #endregion

        #region Public API

        public List<WorldBoss> GetActiveBosses()
        {
            return activeBosses.Values.Where(b => b.state != BossState.Dead).ToList();
        }

        public WorldBoss GetBoss(string instanceId)
        {
            return activeBosses.GetValueOrDefault(instanceId);
        }

        public RaidParticipation GetPlayerParticipation(ulong playerId)
        {
            return playerParticipation.GetValueOrDefault(playerId);
        }

        public List<BossDefinition> GetAvailableBosses()
        {
            return bossDefinitions.Values.ToList();
        }

        public BossDefinition GetBossDefinition(string bossId)
        {
            return bossDefinitions.GetValueOrDefault(bossId);
        }

        #endregion
    }

    #region Data Structures

    [Serializable]
    public class WorldBoss
    {
        public string instanceId;
        public BossDefinition definition;
        public DateTime spawnTime;
        public DateTime? defeatTime;
        public Vector3 position;
        public BossState state;
        public int currentPhase;
        public float maxHealth;
        public float currentHealth;
        public bool isEnraged;
        public float enrageTimer;
        public List<ulong> participants;
        public Dictionary<ulong, float> damageDealt;
        public Dictionary<string, float> abilityCooldowns;
    }

    [Serializable]
    public class BossDefinition
    {
        public string bossId;
        public string bossName;
        public BossType bossType;
        public BossTier tier;
        public float baseHealth;
        public float baseDamage;
        public float baseArmor;
        public int recommendedPlayers;
        public float enrageTimer;
        public List<BossPhase> phases;
        public List<BossAbility> abilities;
        public List<LootTableEntry> lootTable;
    }

    [Serializable]
    public class BossPhase
    {
        public int phaseNumber;
        public float healthThreshold;
        public List<string> abilities;
    }

    [Serializable]
    public class BossAbility
    {
        public string abilityId;
        public float cooldown;
        public float damage;
        public float range;
        public AbilityEffect effectType;
    }

    [Serializable]
    public class LootTableEntry
    {
        public string itemId;
        public float dropChance;
        public int quantity;
    }

    [Serializable]
    public class LootDrop
    {
        public string itemId;
        public int quantity;
        public ItemRarity rarity;
    }

    [Serializable]
    public class RaidEncounter
    {
        public string encounterId;
        public string bossInstanceId;
        public List<ulong> raidMembers;
        public DateTime startTime;
        public DateTime? endTime;
        public bool isSuccessful;
    }

    [Serializable]
    public class RaidParticipation
    {
        public ulong playerId;
        public string bossInstanceId;
        public DateTime joinTime;
        public float damageDealt;
        public float healingDone;
        public float damageTaken;
        public int deathCount;
        public List<string> lootReceived;
    }

    [Serializable]
    public class BossSchedule
    {
        public string scheduleId;
        public string bossId;
        public List<Vector3> spawnLocations;
        public int spawnIntervalMinutes;
        public DateTime lastSpawnTime;
        public bool isActive;
    }

    public enum BossType
    {
        WorldBoss,
        RaidBoss,
        FieldBoss,
        DungeonBoss
    }

    public enum BossTier
    {
        Common,
        Rare,
        Epic,
        Legendary,
        Mythic
    }

    public enum BossState
    {
        Idle,
        InCombat,
        Retreating,
        Dead
    }

    public enum AbilityEffect
    {
        None,
        Fear,
        DoT,
        AreaDamage,
        SummonCreatures,
        Pull,
        LifeSteal,
        Spread,
        Wipe,
        AreaDoT
    }

    public enum LootDistributionMode
    {
        PersonalLoot,
        GroupLoot,
        MasterLoot
    }

    public enum ItemRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary,
        Mythic
    }

    #endregion
}
