using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Companions
{
    /// <summary>
    /// Comprehensive pet and companion system.
    /// AI companions that fight alongside players, with progression, abilities, and customization.
    /// </summary>
    public class CompanionSystem : NetworkBehaviour
    {
        public static CompanionSystem Instance { get; private set; }

        [Header("Companion Configuration")]
        [SerializeField] private int maxActiveCompanions = 2;
        [SerializeField] private int maxOwnedCompanions = 10;
        [SerializeField] private float companionReviveCost = 500f;
        [SerializeField] private float companionReviveTime = 30f;

        [Header("Progression")]
        [SerializeField] private int maxCompanionLevel = 50;
        [SerializeField] private float baseXPRequirement = 100f;
        [SerializeField] private float xpScaling = 1.5f;
        [SerializeField] private bool sharePlayerXP = true;
        [SerializeField] private float xpSharePercentage = 0.25f; // 25% of player XP goes to companion

        [Header("Combat")]
        [SerializeField] private float companionDamageMultiplier = 1f;
        [SerializeField] private float companionHealthMultiplier = 1f;
        [SerializeField] private bool enableCompanionPermadeath = false;

        [Header("Bonding")]
        [SerializeField] private bool enableBondingSystem = true;
        [SerializeField] private int maxBondLevel = 10;
        [SerializeField] private float bondDecayRate = 0.1f; // Per day without interaction

        // Data structures
        private Dictionary<ulong, PlayerCompanionData> playerCompanions = new Dictionary<ulong, PlayerCompanionData>();
        private Dictionary<string, Companion> activeCompanions = new Dictionary<string, Companion>();
        private Dictionary<string, CompanionDefinition> companionDefinitions = new Dictionary<string, CompanionDefinition>();
        private Dictionary<string, CompanionAbility> abilityDefinitions = new Dictionary<string, CompanionAbility>();

        // Events
        public event Action<ulong, string> OnCompanionAcquired;
        public event Action<ulong, string> OnCompanionSummoned;
        public event Action<ulong, string> OnCompanionDismissed;
        public event Action<string, int> OnCompanionLevelUp;
        public event Action<string, int> OnCompanionBondChanged;
        public event Action<string> OnCompanionDied;
        public event Action<string, string> OnCompanionAbilityUsed;

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
                InitializeCompanionDefinitions();
                InitializeAbilityDefinitions();
                InvokeRepeating(nameof(UpdateActiveCompanions), 1f, 0.5f);
                InvokeRepeating(nameof(UpdateBondDecay), 3600f, 3600f); // Every hour
            }
        }

        private void InitializeCompanionDefinitions()
        {
            // Dog Companions
            companionDefinitions["dog_german_shepherd"] = new CompanionDefinition
            {
                companionId = "dog_german_shepherd",
                companionName = "German Shepherd",
                species = CompanionSpecies.Dog,
                rarity = CompanionRarity.Common,
                baseHealth = 200,
                baseDamage = 25,
                baseArmor = 10,
                baseSpeed = 7f,
                specialization = CompanionRole.Assault,
                abilities = new List<string> { "bite", "bark", "guard" },
                unlockRequirement = new UnlockRequirement { requirementType = UnlockType.Default }
            };

            companionDefinitions["dog_rottweiler"] = new CompanionDefinition
            {
                companionId = "dog_rottweiler",
                companionName = "Rottweiler",
                species = CompanionSpecies.Dog,
                rarity = CompanionRarity.Uncommon,
                baseHealth = 300,
                baseDamage = 35,
                baseArmor = 15,
                baseSpeed = 6f,
                specialization = CompanionRole.Tank,
                abilities = new List<string> { "bite", "intimidate", "protect" },
                unlockRequirement = new UnlockRequirement { requirementType = UnlockType.Level, requiredLevel = 10 }
            };

            companionDefinitions["dog_wolf"] = new CompanionDefinition
            {
                companionId = "dog_wolf",
                companionName = "Wolf",
                species = CompanionSpecies.Dog,
                rarity = CompanionRarity.Rare,
                baseHealth = 250,
                baseDamage = 45,
                baseArmor = 12,
                baseSpeed = 8f,
                specialization = CompanionRole.Assault,
                abilities = new List<string> { "bite", "howl", "pack_hunter" },
                unlockRequirement = new UnlockRequirement { requirementType = UnlockType.Achievement, requiredAchievement = "wolf_tamer" }
            };

            // Cat Companions
            companionDefinitions["cat_panther"] = new CompanionDefinition
            {
                companionId = "cat_panther",
                companionName = "Panther",
                species = CompanionSpecies.Cat,
                rarity = CompanionRarity.Epic,
                baseHealth = 180,
                baseDamage = 50,
                baseArmor = 8,
                baseSpeed = 10f,
                specialization = CompanionRole.Assassin,
                abilities = new List<string> { "pounce", "stealth", "bleed" },
                unlockRequirement = new UnlockRequirement { requirementType = UnlockType.Level, requiredLevel = 25 }
            };

            // Bird Companions
            companionDefinitions["bird_hawk"] = new CompanionDefinition
            {
                companionId = "bird_hawk",
                companionName = "Hawk",
                species = CompanionSpecies.Bird,
                rarity = CompanionRarity.Uncommon,
                baseHealth = 100,
                baseDamage = 20,
                baseArmor = 5,
                baseSpeed = 12f,
                specialization = CompanionRole.Scout,
                abilities = new List<string> { "dive_bomb", "scout", "distract" },
                unlockRequirement = new UnlockRequirement { requirementType = UnlockType.Level, requiredLevel = 15 }
            };

            // Robot Companions
            companionDefinitions["robot_drone"] = new CompanionDefinition
            {
                companionId = "robot_drone",
                companionName = "Combat Drone",
                species = CompanionSpecies.Robot,
                rarity = CompanionRarity.Legendary,
                baseHealth = 400,
                baseDamage = 40,
                baseArmor = 25,
                baseSpeed = 9f,
                specialization = CompanionRole.Support,
                abilities = new List<string> { "laser_shot", "shield_generator", "repair" },
                unlockRequirement = new UnlockRequirement { requirementType = UnlockType.Currency, requiredCurrency = 50000 }
            };

            // Exotic Companions
            companionDefinitions["bear_grizzly"] = new CompanionDefinition
            {
                companionId = "bear_grizzly",
                companionName = "Grizzly Bear",
                species = CompanionSpecies.Bear,
                rarity = CompanionRarity.Legendary,
                baseHealth = 600,
                baseDamage = 60,
                baseArmor = 30,
                baseSpeed = 5f,
                specialization = CompanionRole.Tank,
                abilities = new List<string> { "maul", "roar", "regenerate" },
                unlockRequirement = new UnlockRequirement { requirementType = UnlockType.Event, requiredEvent = "beast_master_event" }
            };
        }

        private void InitializeAbilityDefinitions()
        {
            // Dog Abilities
            abilityDefinitions["bite"] = new CompanionAbility
            {
                abilityId = "bite",
                abilityName = "Bite",
                description = "Bites the target dealing damage",
                cooldown = 5f,
                damage = 30f,
                range = 2f,
                abilityType = AbilityType.Attack
            };

            abilityDefinitions["bark"] = new CompanionAbility
            {
                abilityId = "bark",
                abilityName = "Bark",
                description = "Alerts enemies and draws aggro",
                cooldown = 15f,
                damage = 0f,
                range = 10f,
                abilityType = AbilityType.Taunt
            };

            abilityDefinitions["guard"] = new CompanionAbility
            {
                abilityId = "guard",
                abilityName = "Guard",
                description = "Protects owner from incoming damage",
                cooldown = 30f,
                damage = 0f,
                range = 3f,
                abilityType = AbilityType.Buff,
                duration = 10f
            };

            abilityDefinitions["intimidate"] = new CompanionAbility
            {
                abilityId = "intimidate",
                abilityName = "Intimidate",
                description = "Reduces enemy damage output",
                cooldown = 20f,
                damage = 0f,
                range = 5f,
                abilityType = AbilityType.Debuff,
                duration = 8f
            };

            abilityDefinitions["protect"] = new CompanionAbility
            {
                abilityId = "protect",
                abilityName = "Protect",
                description = "Intercepts attacks aimed at owner",
                cooldown = 25f,
                damage = 0f,
                range = 3f,
                abilityType = AbilityType.Buff,
                duration = 12f
            };

            abilityDefinitions["howl"] = new CompanionAbility
            {
                abilityId = "howl",
                abilityName = "Howl",
                description = "Buffs nearby allies' damage",
                cooldown = 45f,
                damage = 0f,
                range = 15f,
                abilityType = AbilityType.Buff,
                duration = 20f
            };

            abilityDefinitions["pack_hunter"] = new CompanionAbility
            {
                abilityId = "pack_hunter",
                abilityName = "Pack Hunter",
                description = "Increased damage when near allies",
                cooldown = 0f,
                damage = 0f,
                range = 10f,
                abilityType = AbilityType.Passive
            };

            // Cat Abilities
            abilityDefinitions["pounce"] = new CompanionAbility
            {
                abilityId = "pounce",
                abilityName = "Pounce",
                description = "Leaps at target dealing heavy damage",
                cooldown = 12f,
                damage = 60f,
                range = 8f,
                abilityType = AbilityType.Attack
            };

            abilityDefinitions["stealth"] = new CompanionAbility
            {
                abilityId = "stealth",
                abilityName = "Stealth",
                description = "Becomes invisible for a short time",
                cooldown = 40f,
                damage = 0f,
                range = 0f,
                abilityType = AbilityType.Buff,
                duration = 8f
            };

            abilityDefinitions["bleed"] = new CompanionAbility
            {
                abilityId = "bleed",
                abilityName = "Bleed",
                description = "Inflicts bleeding damage over time",
                cooldown = 18f,
                damage = 10f,
                range = 2f,
                abilityType = AbilityType.DoT,
                duration = 10f
            };

            // Bird Abilities
            abilityDefinitions["dive_bomb"] = new CompanionAbility
            {
                abilityId = "dive_bomb",
                abilityName = "Dive Bomb",
                description = "Dives from above dealing damage",
                cooldown = 10f,
                damage = 25f,
                range = 15f,
                abilityType = AbilityType.Attack
            };

            abilityDefinitions["scout"] = new CompanionAbility
            {
                abilityId = "scout",
                abilityName = "Scout",
                description = "Reveals nearby enemies",
                cooldown = 30f,
                damage = 0f,
                range = 30f,
                abilityType = AbilityType.Utility,
                duration = 15f
            };

            abilityDefinitions["distract"] = new CompanionAbility
            {
                abilityId = "distract",
                abilityName = "Distract",
                description = "Distracts enemies reducing accuracy",
                cooldown = 25f,
                damage = 0f,
                range = 10f,
                abilityType = AbilityType.Debuff,
                duration = 10f
            };

            // Robot Abilities
            abilityDefinitions["laser_shot"] = new CompanionAbility
            {
                abilityId = "laser_shot",
                abilityName = "Laser Shot",
                description = "Fires a precise laser beam",
                cooldown = 8f,
                damage = 45f,
                range = 20f,
                abilityType = AbilityType.Attack
            };

            abilityDefinitions["shield_generator"] = new CompanionAbility
            {
                abilityId = "shield_generator",
                abilityName = "Shield Generator",
                description = "Generates shields for nearby allies",
                cooldown = 50f,
                damage = 0f,
                range = 12f,
                abilityType = AbilityType.Buff,
                duration = 15f
            };

            abilityDefinitions["repair"] = new CompanionAbility
            {
                abilityId = "repair",
                abilityName = "Repair",
                description = "Heals the owner over time",
                cooldown = 35f,
                damage = 0f,
                range = 5f,
                abilityType = AbilityType.Heal,
                duration = 10f
            };

            // Bear Abilities
            abilityDefinitions["maul"] = new CompanionAbility
            {
                abilityId = "maul",
                abilityName = "Maul",
                description = "Powerful melee attack",
                cooldown = 10f,
                damage = 80f,
                range = 3f,
                abilityType = AbilityType.Attack
            };

            abilityDefinitions["roar"] = new CompanionAbility
            {
                abilityId = "roar",
                abilityName = "Roar",
                description = "Fears nearby enemies",
                cooldown = 40f,
                damage = 0f,
                range = 8f,
                abilityType = AbilityType.Debuff,
                duration = 5f
            };

            abilityDefinitions["regenerate"] = new CompanionAbility
            {
                abilityId = "regenerate",
                abilityName = "Regenerate",
                description = "Slowly regenerates health",
                cooldown = 60f,
                damage = 0f,
                range = 0f,
                abilityType = AbilityType.Heal,
                duration = 20f
            };
        }

        private void UpdateActiveCompanions()
        {
            foreach (var companion in activeCompanions.Values)
            {
                if (companion.state == CompanionState.Active)
                {
                    UpdateCompanionAI(companion);
                    UpdateCompanionAbilities(companion);
                }
            }
        }

        private void UpdateCompanionAI(Companion companion)
        {
            // Basic AI behavior
            // This would integrate with your AI/pathfinding system
            // For now, just update position to follow owner

            // Check for enemies in range
            // Prioritize targets based on role
            // Use abilities when appropriate
        }

        private void UpdateCompanionAbilities(Companion companion)
        {
            foreach (var abilityId in companion.definition.abilities)
            {
                if (!companion.abilityCooldowns.ContainsKey(abilityId))
                {
                    companion.abilityCooldowns[abilityId] = 0f;
                }

                companion.abilityCooldowns[abilityId] -= Time.deltaTime;

                if (companion.abilityCooldowns[abilityId] <= 0f && abilityDefinitions.TryGetValue(abilityId, out var ability))
                {
                    if (ShouldUseAbility(companion, ability))
                    {
                        UseCompanionAbility(companion, ability);
                        companion.abilityCooldowns[abilityId] = ability.cooldown;
                    }
                }
            }
        }

        private bool ShouldUseAbility(Companion companion, CompanionAbility ability)
        {
            // Decide if companion should use this ability based on situation
            // This is a placeholder - would contain complex AI logic
            return UnityEngine.Random.value > 0.8f;
        }

        private void UseCompanionAbility(Companion companion, CompanionAbility ability)
        {
            OnCompanionAbilityUsed?.Invoke(companion.instanceId, ability.abilityId);
            BroadcastCompanionAbilityClientRpc(companion.instanceId, ability.abilityId);

            // Apply ability effects
            switch (ability.abilityType)
            {
                case AbilityType.Attack:
                    DealAbilityDamage(companion, ability);
                    break;
                case AbilityType.Buff:
                    ApplyBuff(companion, ability);
                    break;
                case AbilityType.Debuff:
                    ApplyDebuff(companion, ability);
                    break;
                case AbilityType.Heal:
                    ApplyHeal(companion, ability);
                    break;
            }

            // Increase bond through ability usage
            if (enableBondingSystem)
            {
                ModifyBond(companion, 1);
            }
        }

        private void DealAbilityDamage(Companion companion, CompanionAbility ability)
        {
            // Deal damage to enemies in range
            // This would integrate with your combat system
        }

        private void ApplyBuff(Companion companion, CompanionAbility ability)
        {
            // Apply buff to companion or owner
        }

        private void ApplyDebuff(Companion companion, CompanionAbility ability)
        {
            // Apply debuff to enemies
        }

        private void ApplyHeal(Companion companion, CompanionAbility ability)
        {
            // Heal companion or owner
            companion.currentHealth = Mathf.Min(companion.currentHealth + ability.damage, companion.maxHealth);
        }

        private void UpdateBondDecay()
        {
            foreach (var playerData in playerCompanions.Values)
            {
                foreach (var companionData in playerData.ownedCompanions.Values)
                {
                    if (companionData.bondLevel > 0)
                    {
                        DateTime lastInteraction = companionData.lastInteractionDate;
                        float daysSinceInteraction = (float)(DateTime.UtcNow - lastInteraction).TotalDays;

                        if (daysSinceInteraction > 1f)
                        {
                            companionData.bondLevel = Mathf.Max(0, companionData.bondLevel - Mathf.FloorToInt(daysSinceInteraction * bondDecayRate));
                        }
                    }
                }
            }
        }

        #region Server RPCs

        [ServerRpc(RequireOwnership = false)]
        public void InitializePlayerCompanionsServerRpc(ulong playerId, ServerRpcParams rpcParams = default)
        {
            if (!playerCompanions.ContainsKey(playerId))
            {
                playerCompanions[playerId] = new PlayerCompanionData
                {
                    playerId = playerId,
                    ownedCompanions = new Dictionary<string, CompanionData>(),
                    activeCompanionIds = new List<string>()
                };
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void AcquireCompanionServerRpc(ulong playerId, string companionDefId, ServerRpcParams rpcParams = default)
        {
            if (!companionDefinitions.TryGetValue(companionDefId, out var definition))
            {
                Debug.LogWarning($"Companion definition not found: {companionDefId}");
                return;
            }

            if (!playerCompanions.TryGetValue(playerId, out var playerData))
            {
                InitializePlayerCompanionsServerRpc(playerId);
                playerData = playerCompanions[playerId];
            }

            if (playerData.ownedCompanions.Count >= maxOwnedCompanions)
            {
                Debug.LogWarning($"Player {playerId} has maximum companions");
                return;
            }

            // Check unlock requirements
            if (!CheckUnlockRequirement(playerId, definition.unlockRequirement))
            {
                Debug.LogWarning($"Player {playerId} does not meet unlock requirements for {companionDefId}");
                return;
            }

            var companionData = new CompanionData
            {
                companionId = $"companion_{Guid.NewGuid()}",
                definitionId = companionDefId,
                name = definition.companionName,
                level = 1,
                currentXP = 0,
                bondLevel = 0,
                acquiredDate = DateTime.UtcNow,
                lastInteractionDate = DateTime.UtcNow,
                totalKills = 0,
                totalDamageDealt = 0
            };

            playerData.ownedCompanions[companionData.companionId] = companionData;

            OnCompanionAcquired?.Invoke(playerId, companionData.companionId);
            NotifyCompanionAcquiredClientRpc(playerId, companionData.companionId);

            Debug.Log($"Player {playerId} acquired companion: {definition.companionName}");
        }

        private bool CheckUnlockRequirement(ulong playerId, UnlockRequirement requirement)
        {
            switch (requirement.requirementType)
            {
                case UnlockType.Default:
                    return true;

                case UnlockType.Level:
                    // Would check player level
                    return true;

                case UnlockType.Achievement:
                    // Would check if player has achievement
                    return true;

                case UnlockType.Currency:
                    // Would check if player has enough currency
                    return Economy.EconomyManager.Instance?.SpendSoftCurrency(playerId, requirement.requiredCurrency) ?? false;

                case UnlockType.Event:
                    // Would check if event is active
                    return false;

                default:
                    return false;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void SummonCompanionServerRpc(ulong playerId, string companionId, ServerRpcParams rpcParams = default)
        {
            if (!playerCompanions.TryGetValue(playerId, out var playerData))
            {
                return;
            }

            if (!playerData.ownedCompanions.TryGetValue(companionId, out var companionData))
            {
                Debug.LogWarning($"Companion not found: {companionId}");
                return;
            }

            if (playerData.activeCompanionIds.Count >= maxActiveCompanions)
            {
                Debug.LogWarning($"Player {playerId} has maximum active companions");
                return;
            }

            if (!companionDefinitions.TryGetValue(companionData.definitionId, out var definition))
            {
                return;
            }

            // Create active companion instance
            var companion = new Companion
            {
                instanceId = $"instance_{Guid.NewGuid()}",
                companionDataId = companionId,
                ownerId = playerId,
                definition = definition,
                level = companionData.level,
                bondLevel = companionData.bondLevel,
                maxHealth = CalculateCompanionHealth(definition, companionData.level),
                currentHealth = 0,
                damage = CalculateCompanionDamage(definition, companionData.level),
                armor = CalculateCompanionArmor(definition, companionData.level),
                speed = definition.baseSpeed,
                state = CompanionState.Active,
                position = Vector3.zero, // Would be set to player position
                abilityCooldowns = new Dictionary<string, float>()
            };

            companion.currentHealth = companion.maxHealth;

            activeCompanions[companion.instanceId] = companion;
            playerData.activeCompanionIds.Add(companion.instanceId);
            companionData.lastInteractionDate = DateTime.UtcNow;

            OnCompanionSummoned?.Invoke(playerId, companionId);
            NotifyCompanionSummonedClientRpc(playerId, companion.instanceId, companionId);

            Debug.Log($"Player {playerId} summoned companion: {definition.companionName}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void DismissCompanionServerRpc(ulong playerId, string instanceId, ServerRpcParams rpcParams = default)
        {
            if (!activeCompanions.TryGetValue(instanceId, out var companion))
            {
                return;
            }

            if (companion.ownerId != playerId)
            {
                return;
            }

            if (!playerCompanions.TryGetValue(playerId, out var playerData))
            {
                return;
            }

            playerData.activeCompanionIds.Remove(instanceId);
            activeCompanions.Remove(instanceId);

            OnCompanionDismissed?.Invoke(playerId, instanceId);
            NotifyCompanionDismissedClientRpc(playerId, instanceId);

            Debug.Log($"Player {playerId} dismissed companion");
        }

        [ServerRpc(RequireOwnership = false)]
        public void DamageCompanionServerRpc(string instanceId, float damage, ServerRpcParams rpcParams = default)
        {
            if (!activeCompanions.TryGetValue(instanceId, out var companion))
            {
                return;
            }

            float actualDamage = Mathf.Max(0, damage - companion.armor);
            companion.currentHealth -= actualDamage;

            if (companion.currentHealth <= 0)
            {
                CompanionDied(companion);
            }
        }

        private void CompanionDied(Companion companion)
        {
            companion.state = CompanionState.Dead;
            companion.deathTime = DateTime.UtcNow;

            OnCompanionDied?.Invoke(companion.instanceId);
            NotifyCompanionDiedClientRpc(companion.instanceId);

            if (enableCompanionPermadeath)
            {
                // Remove companion permanently
                if (playerCompanions.TryGetValue(companion.ownerId, out var playerData))
                {
                    playerData.ownedCompanions.Remove(companion.companionDataId);
                }
            }

            Debug.Log($"Companion died: {companion.definition.companionName}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void ReviveCompanionServerRpc(ulong playerId, string instanceId, ServerRpcParams rpcParams = default)
        {
            if (!activeCompanions.TryGetValue(instanceId, out var companion))
            {
                return;
            }

            if (companion.state != CompanionState.Dead)
            {
                return;
            }

            // Check revive cooldown
            float timeSinceDeath = (float)(DateTime.UtcNow - companion.deathTime.Value).TotalSeconds;
            if (timeSinceDeath < companionReviveTime)
            {
                Debug.LogWarning($"Companion cannot be revived yet");
                return;
            }

            // Charge revive cost
            if (!Economy.EconomyManager.Instance.SpendSoftCurrency(playerId, (int)companionReviveCost))
            {
                Debug.LogWarning($"Player {playerId} cannot afford revive");
                return;
            }

            companion.state = CompanionState.Active;
            companion.currentHealth = companion.maxHealth * 0.5f; // Revive at 50% health
            companion.deathTime = null;

            NotifyCompanionRevivedClientRpc(instanceId);

            Debug.Log($"Companion revived: {companion.definition.companionName}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void AddCompanionXPServerRpc(string instanceId, int xp, ServerRpcParams rpcParams = default)
        {
            if (!activeCompanions.TryGetValue(instanceId, out var companion))
            {
                return;
            }

            if (!playerCompanions.TryGetValue(companion.ownerId, out var playerData))
            {
                return;
            }

            if (!playerData.ownedCompanions.TryGetValue(companion.companionDataId, out var companionData))
            {
                return;
            }

            companionData.currentXP += xp;

            // Check for level up
            int xpRequired = CalculateXPRequirement(companionData.level);
            while (companionData.currentXP >= xpRequired && companionData.level < maxCompanionLevel)
            {
                companionData.currentXP -= xpRequired;
                companionData.level++;
                companion.level = companionData.level;

                // Update stats
                companion.maxHealth = CalculateCompanionHealth(companion.definition, companionData.level);
                companion.currentHealth = companion.maxHealth;
                companion.damage = CalculateCompanionDamage(companion.definition, companionData.level);
                companion.armor = CalculateCompanionArmor(companion.definition, companionData.level);

                OnCompanionLevelUp?.Invoke(instanceId, companionData.level);
                NotifyCompanionLevelUpClientRpc(instanceId, companionData.level);

                xpRequired = CalculateXPRequirement(companionData.level);
            }
        }

        private int CalculateXPRequirement(int level)
        {
            return Mathf.RoundToInt(baseXPRequirement * Mathf.Pow(xpScaling, level - 1));
        }

        private float CalculateCompanionHealth(CompanionDefinition definition, int level)
        {
            return definition.baseHealth * (1 + (level - 1) * 0.1f) * companionHealthMultiplier;
        }

        private float CalculateCompanionDamage(CompanionDefinition definition, int level)
        {
            return definition.baseDamage * (1 + (level - 1) * 0.08f) * companionDamageMultiplier;
        }

        private float CalculateCompanionArmor(CompanionDefinition definition, int level)
        {
            return definition.baseArmor * (1 + (level - 1) * 0.05f);
        }

        [ServerRpc(RequireOwnership = false)]
        public void RenameCompanionServerRpc(ulong playerId, string companionId, string newName, ServerRpcParams rpcParams = default)
        {
            if (!playerCompanions.TryGetValue(playerId, out var playerData))
            {
                return;
            }

            if (!playerData.ownedCompanions.TryGetValue(companionId, out var companionData))
            {
                return;
            }

            companionData.name = newName;
            NotifyCompanionRenamedClientRpc(companionId, newName);
        }

        private void ModifyBond(Companion companion, int amount)
        {
            if (!enableBondingSystem)
                return;

            if (!playerCompanions.TryGetValue(companion.ownerId, out var playerData))
                return;

            if (!playerData.ownedCompanions.TryGetValue(companion.companionDataId, out var companionData))
                return;

            int oldBond = companionData.bondLevel;
            companionData.bondLevel = Mathf.Clamp(companionData.bondLevel + amount, 0, maxBondLevel);
            companion.bondLevel = companionData.bondLevel;
            companionData.lastInteractionDate = DateTime.UtcNow;

            if (oldBond != companionData.bondLevel)
            {
                OnCompanionBondChanged?.Invoke(companion.instanceId, companionData.bondLevel);
                NotifyBondChangedClientRpc(companion.instanceId, companionData.bondLevel);
            }
        }

        #endregion

        #region Client RPCs

        [ClientRpc]
        private void NotifyCompanionAcquiredClientRpc(ulong playerId, string companionId)
        {
            OnCompanionAcquired?.Invoke(playerId, companionId);
        }

        [ClientRpc]
        private void NotifyCompanionSummonedClientRpc(ulong playerId, string instanceId, string companionId)
        {
            OnCompanionSummoned?.Invoke(playerId, companionId);
        }

        [ClientRpc]
        private void NotifyCompanionDismissedClientRpc(ulong playerId, string instanceId)
        {
            OnCompanionDismissed?.Invoke(playerId, instanceId);
        }

        [ClientRpc]
        private void NotifyCompanionDiedClientRpc(string instanceId)
        {
            OnCompanionDied?.Invoke(instanceId);
        }

        [ClientRpc]
        private void NotifyCompanionRevivedClientRpc(string instanceId)
        {
            // Client-side notification
        }

        [ClientRpc]
        private void NotifyCompanionLevelUpClientRpc(string instanceId, int newLevel)
        {
            OnCompanionLevelUp?.Invoke(instanceId, newLevel);
        }

        [ClientRpc]
        private void NotifyCompanionRenamedClientRpc(string companionId, string newName)
        {
            // Client-side notification
        }

        [ClientRpc]
        private void NotifyBondChangedClientRpc(string instanceId, int newBond)
        {
            OnCompanionBondChanged?.Invoke(instanceId, newBond);
        }

        [ClientRpc]
        private void BroadcastCompanionAbilityClientRpc(string instanceId, string abilityId)
        {
            OnCompanionAbilityUsed?.Invoke(instanceId, abilityId);
        }

        #endregion

        #region Public API

        public PlayerCompanionData GetPlayerCompanions(ulong playerId)
        {
            return playerCompanions.GetValueOrDefault(playerId);
        }

        public Companion GetActiveCompanion(string instanceId)
        {
            return activeCompanions.GetValueOrDefault(instanceId);
        }

        public List<CompanionDefinition> GetAvailableCompanions()
        {
            return companionDefinitions.Values.ToList();
        }

        public CompanionDefinition GetCompanionDefinition(string companionId)
        {
            return companionDefinitions.GetValueOrDefault(companionId);
        }

        #endregion
    }

    #region Data Structures

    [Serializable]
    public class PlayerCompanionData
    {
        public ulong playerId;
        public Dictionary<string, CompanionData> ownedCompanions;
        public List<string> activeCompanionIds;
    }

    [Serializable]
    public class CompanionData
    {
        public string companionId;
        public string definitionId;
        public string name;
        public int level;
        public int currentXP;
        public int bondLevel;
        public DateTime acquiredDate;
        public DateTime lastInteractionDate;
        public int totalKills;
        public float totalDamageDealt;
    }

    [Serializable]
    public class Companion
    {
        public string instanceId;
        public string companionDataId;
        public ulong ownerId;
        public CompanionDefinition definition;
        public int level;
        public int bondLevel;
        public float maxHealth;
        public float currentHealth;
        public float damage;
        public float armor;
        public float speed;
        public CompanionState state;
        public Vector3 position;
        public DateTime? deathTime;
        public Dictionary<string, float> abilityCooldowns;
    }

    [Serializable]
    public class CompanionDefinition
    {
        public string companionId;
        public string companionName;
        public CompanionSpecies species;
        public CompanionRarity rarity;
        public float baseHealth;
        public float baseDamage;
        public float baseArmor;
        public float baseSpeed;
        public CompanionRole specialization;
        public List<string> abilities;
        public UnlockRequirement unlockRequirement;
    }

    [Serializable]
    public class CompanionAbility
    {
        public string abilityId;
        public string abilityName;
        public string description;
        public float cooldown;
        public float damage;
        public float range;
        public AbilityType abilityType;
        public float duration;
    }

    [Serializable]
    public class UnlockRequirement
    {
        public UnlockType requirementType;
        public int requiredLevel;
        public string requiredAchievement;
        public int requiredCurrency;
        public string requiredEvent;
    }

    public enum CompanionSpecies
    {
        Dog,
        Cat,
        Bird,
        Robot,
        Bear,
        Wolf
    }

    public enum CompanionRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    public enum CompanionRole
    {
        Assault,
        Tank,
        Assassin,
        Scout,
        Support
    }

    public enum CompanionState
    {
        Inactive,
        Active,
        Dead,
        Reviving
    }

    public enum AbilityType
    {
        Attack,
        Buff,
        Debuff,
        Heal,
        Taunt,
        DoT,
        Utility,
        Passive
    }

    public enum UnlockType
    {
        Default,
        Level,
        Achievement,
        Currency,
        Event
    }

    #endregion
}
