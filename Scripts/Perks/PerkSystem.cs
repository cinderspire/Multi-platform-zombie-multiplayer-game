using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Perks
{
    /// <summary>
    /// Comprehensive perk and passive ability system with active abilities, skill trees, and synergies.
    /// Handles perk unlocking, equipping, upgrading, and effect application.
    /// </summary>
    public class PerkSystem : NetworkBehaviour
    {
        public static PerkSystem Instance { get; private set; }

        [Header("Perk Configuration")]
        [SerializeField] private int maxEquippedPerks = 6;
        [SerializeField] private int maxPerkLevel = 5;
        [SerializeField] private bool enablePerkSynergies = true;

        // Perk data
        private Dictionary<string, PerkDefinition> perkDatabase = new Dictionary<string, PerkDefinition>();
        private Dictionary<ulong, PlayerPerks> playerPerks = new Dictionary<ulong, PlayerPerks>();
        private Dictionary<string, List<string>> perkTrees = new Dictionary<string, List<string>>();
        private Dictionary<string, PerkSynergy> perkSynergies = new Dictionary<string, PerkSynergy>();

        // Events
        public event Action<ulong, string> OnPerkUnlocked;
        public event Action<ulong, string> OnPerkEquipped;
        public event Action<ulong, string> OnPerkActivated;
        public event Action<ulong, string, int> OnPerkUpgraded;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer)
            {
                InitializePerkDatabase();
                InitializePerkTrees();
                InitializePerkSynergies();
            }
        }

        private void InitializePerkDatabase()
        {
            // ===== COMBAT PERKS =====
            
            // Damage perks
            perkDatabase["combat_damage_basic"] = new PerkDefinition
            {
                perkId = "combat_damage_basic",
                perkName = "Damage Boost",
                description = "Increase all damage dealt",
                perkType = PerkType.Passive,
                category = PerkCategory.Combat,
                rarity = PerkRarity.Common,
                maxLevel = 5,
                effects = new List<PerkEffect>
                {
                    new PerkEffect { effectType = EffectType.DamageBonus, value = 0.05f, valuePerLevel = 0.03f }
                },
                requiredLevel = 1,
                unlockCost = 0
            };

            perkDatabase["combat_headshot"] = new PerkDefinition
            {
                perkId = "combat_headshot",
                perkName = "Headhunter",
                description = "Increased headshot damage and chance",
                perkType = PerkType.Passive,
                category = PerkCategory.Combat,
                rarity = PerkRarity.Rare,
                maxLevel = 5,
                effects = new List<PerkEffect>
                {
                    new PerkEffect { effectType = EffectType.HeadshotDamage, value = 0.15f, valuePerLevel = 0.10f },
                    new PerkEffect { effectType = EffectType.Accuracy, value = 0.10f, valuePerLevel = 0.05f }
                },
                requiredLevel = 10,
                unlockCost = 500
            };

            perkDatabase["combat_crit"] = new PerkDefinition
            {
                perkId = "combat_crit",
                perkName = "Critical Strike",
                description = "Increase critical hit chance and damage",
                perkType = PerkType.Passive,
                category = PerkCategory.Combat,
                rarity = PerkRarity.Epic,
                maxLevel = 5,
                effects = new List<PerkEffect>
                {
                    new PerkEffect { effectType = EffectType.CritChance, value = 0.05f, valuePerLevel = 0.03f },
                    new PerkEffect { effectType = EffectType.CritDamage, value = 0.25f, valuePerLevel = 0.15f }
                },
                requiredLevel = 20,
                unlockCost = 2000
            };

            perkDatabase["combat_reload"] = new PerkDefinition
            {
                perkId = "combat_reload",
                perkName = "Quick Reload",
                description = "Faster reload and weapon swap",
                perkType = PerkType.Passive,
                category = PerkCategory.Combat,
                rarity = PerkRarity.Uncommon,
                maxLevel = 5,
                effects = new List<PerkEffect>
                {
                    new PerkEffect { effectType = EffectType.ReloadSpeed, value = 0.10f, valuePerLevel = 0.08f },
                    new PerkEffect { effectType = EffectType.WeaponSwapSpeed, value = 0.15f, valuePerLevel = 0.10f }
                },
                requiredLevel = 5,
                unlockCost = 300
            };

            perkDatabase["combat_berserker"] = new PerkDefinition
            {
                perkId = "combat_berserker",
                perkName = "Berserker Rage",
                description = "Activate to gain massive damage boost for short duration",
                perkType = PerkType.Active,
                category = PerkCategory.Combat,
                rarity = PerkRarity.Legendary,
                maxLevel = 3,
                effects = new List<PerkEffect>
                {
                    new PerkEffect { effectType = EffectType.DamageBonus, value = 0.50f, valuePerLevel = 0.25f, duration = 10f }
                },
                cooldown = 60f,
                requiredLevel = 30,
                unlockCost = 5000
            };

            // ===== SURVIVAL PERKS =====

            perkDatabase["survival_health"] = new PerkDefinition
            {
                perkId = "survival_health",
                perkName = "Vitality",
                description = "Increase maximum health",
                perkType = PerkType.Passive,
                category = PerkCategory.Survival,
                rarity = PerkRarity.Common,
                maxLevel = 5,
                effects = new List<PerkEffect>
                {
                    new PerkEffect { effectType = EffectType.MaxHealth, value = 0.10f, valuePerLevel = 0.08f }
                },
                requiredLevel = 1,
                unlockCost = 0
            };

            perkDatabase["survival_regen"] = new PerkDefinition
            {
                perkId = "survival_regen",
                perkName = "Regeneration",
                description = "Slowly regenerate health over time",
                perkType = PerkType.Passive,
                category = PerkCategory.Survival,
                rarity = PerkRarity.Rare,
                maxLevel = 5,
                effects = new List<PerkEffect>
                {
                    new PerkEffect { effectType = EffectType.HealthRegen, value = 1f, valuePerLevel = 0.5f }
                },
                requiredLevel = 15,
                unlockCost = 1000
            };

            perkDatabase["survival_armor"] = new PerkDefinition
            {
                perkId = "survival_armor",
                perkName = "Iron Skin",
                description = "Reduce all damage taken",
                perkType = PerkType.Passive,
                category = PerkCategory.Survival,
                rarity = PerkRarity.Epic,
                maxLevel = 5,
                effects = new List<PerkEffect>
                {
                    new PerkEffect { effectType = EffectType.DamageReduction, value = 0.05f, valuePerLevel = 0.04f }
                },
                requiredLevel = 20,
                unlockCost = 2000
            };

            perkDatabase["survival_medic"] = new PerkDefinition
            {
                perkId = "survival_medic",
                perkName = "Combat Medic",
                description = "Healing items are more effective",
                perkType = PerkType.Passive,
                category = PerkCategory.Survival,
                rarity = PerkRarity.Uncommon,
                maxLevel = 5,
                effects = new List<PerkEffect>
                {
                    new PerkEffect { effectType = EffectType.HealingBonus, value = 0.15f, valuePerLevel = 0.10f },
                    new PerkEffect { effectType = EffectType.HealingSpeed, value = 0.20f, valuePerLevel = 0.15f }
                },
                requiredLevel = 8,
                unlockCost = 400
            };

            perkDatabase["survival_laststand"] = new PerkDefinition
            {
                perkId = "survival_laststand",
                perkName = "Last Stand",
                description = "Survive a lethal hit with 1 HP and gain temporary invulnerability",
                perkType = PerkType.Active,
                category = PerkCategory.Survival,
                rarity = PerkRarity.Legendary,
                maxLevel = 3,
                effects = new List<PerkEffect>
                {
                    new PerkEffect { effectType = EffectType.Invulnerability, value = 1f, duration = 3f, durationPerLevel = 1f }
                },
                cooldown = 300f,
                requiredLevel = 40,
                unlockCost = 8000
            };

            // ===== MOBILITY PERKS =====

            perkDatabase["mobility_speed"] = new PerkDefinition
            {
                perkId = "mobility_speed",
                perkName = "Swift Runner",
                description = "Increase movement speed",
                perkType = PerkType.Passive,
                category = PerkCategory.Mobility,
                rarity = PerkRarity.Common,
                maxLevel = 5,
                effects = new List<PerkEffect>
                {
                    new PerkEffect { effectType = EffectType.MovementSpeed, value = 0.05f, valuePerLevel = 0.04f }
                },
                requiredLevel = 1,
                unlockCost = 0
            };

            perkDatabase["mobility_sprint"] = new PerkDefinition
            {
                perkId = "mobility_sprint",
                perkName = "Marathoner",
                description = "Reduced stamina consumption while sprinting",
                perkType = PerkType.Passive,
                category = PerkCategory.Mobility,
                rarity = PerkRarity.Uncommon,
                maxLevel = 5,
                effects = new List<PerkEffect>
                {
                    new PerkEffect { effectType = EffectType.StaminaCost, value = -0.10f, valuePerLevel = -0.08f },
                    new PerkEffect { effectType = EffectType.StaminaRegen, value = 0.15f, valuePerLevel = 0.10f }
                },
                requiredLevel = 5,
                unlockCost = 300
            };

            perkDatabase["mobility_dodge"] = new PerkDefinition
            {
                perkId = "mobility_dodge",
                perkName = "Evasion",
                description = "Chance to dodge incoming attacks",
                perkType = PerkType.Passive,
                category = PerkCategory.Mobility,
                rarity = PerkRarity.Epic,
                maxLevel = 5,
                effects = new List<PerkEffect>
                {
                    new PerkEffect { effectType = EffectType.DodgeChance, value = 0.05f, valuePerLevel = 0.03f }
                },
                requiredLevel = 25,
                unlockCost = 3000
            };

            perkDatabase["mobility_dash"] = new PerkDefinition
            {
                perkId = "mobility_dash",
                perkName = "Quick Dash",
                description = "Activate to perform a quick dash in any direction",
                perkType = PerkType.Active,
                category = PerkCategory.Mobility,
                rarity = PerkRarity.Rare,
                maxLevel = 3,
                effects = new List<PerkEffect>
                {
                    new PerkEffect { effectType = EffectType.MovementSpeed, value = 3f, duration = 0.5f }
                },
                cooldown = 15f,
                requiredLevel = 18,
                unlockCost = 1500
            };

            // ===== UTILITY PERKS =====

            perkDatabase["utility_ammo"] = new PerkDefinition
            {
                perkId = "utility_ammo",
                perkName = "Ammo Conservation",
                description = "Chance to not consume ammo when firing",
                perkType = PerkType.Passive,
                category = PerkCategory.Utility,
                rarity = PerkRarity.Rare,
                maxLevel = 5,
                effects = new List<PerkEffect>
                {
                    new PerkEffect { effectType = EffectType.AmmoConservation, value = 0.05f, valuePerLevel = 0.04f }
                },
                requiredLevel = 12,
                unlockCost = 800
            };

            perkDatabase["utility_loot"] = new PerkDefinition
            {
                perkId = "utility_loot",
                perkName = "Fortune Finder",
                description = "Increased loot quality and quantity",
                perkType = PerkType.Passive,
                category = PerkCategory.Utility,
                rarity = PerkRarity.Epic,
                maxLevel = 5,
                effects = new List<PerkEffect>
                {
                    new PerkEffect { effectType = EffectType.LootQuality, value = 0.10f, valuePerLevel = 0.08f },
                    new PerkEffect { effectType = EffectType.LootQuantity, value = 0.15f, valuePerLevel = 0.10f }
                },
                requiredLevel = 22,
                unlockCost = 2500
            };

            perkDatabase["utility_repair"] = new PerkDefinition
            {
                perkId = "utility_repair",
                perkName = "Engineer",
                description = "Reduced repair costs and faster building",
                perkType = PerkType.Passive,
                category = PerkCategory.Utility,
                rarity = PerkRarity.Uncommon,
                maxLevel = 5,
                effects = new List<PerkEffect>
                {
                    new PerkEffect { effectType = EffectType.RepairCost, value = -0.10f, valuePerLevel = -0.08f },
                    new PerkEffect { effectType = EffectType.BuildSpeed, value = 0.15f, valuePerLevel = 0.10f }
                },
                requiredLevel = 10,
                unlockCost = 600
            };

            perkDatabase["utility_xp"] = new PerkDefinition
            {
                perkId = "utility_xp",
                perkName = "Fast Learner",
                description = "Gain additional XP from all sources",
                perkType = PerkType.Passive,
                category = PerkCategory.Utility,
                rarity = PerkRarity.Rare,
                maxLevel = 5,
                effects = new List<PerkEffect>
                {
                    new PerkEffect { effectType = EffectType.XpBonus, value = 0.10f, valuePerLevel = 0.08f }
                },
                requiredLevel = 15,
                unlockCost = 1200
            };

            perkDatabase["utility_scan"] = new PerkDefinition
            {
                perkId = "utility_scan",
                perkName = "Threat Detection",
                description = "Reveal nearby enemies and loot for short duration",
                perkType = PerkType.Active,
                category = PerkCategory.Utility,
                rarity = PerkRarity.Epic,
                maxLevel = 3,
                effects = new List<PerkEffect>
                {
                    new PerkEffect { effectType = EffectType.Detection, value = 50f, duration = 10f, durationPerLevel = 5f }
                },
                cooldown = 45f,
                requiredLevel = 28,
                unlockCost = 4000
            };

            // ===== STEALTH PERKS =====

            perkDatabase["stealth_basic"] = new PerkDefinition
            {
                perkId = "stealth_basic",
                perkName = "Shadow Walker",
                description = "Reduced detection range by enemies",
                perkType = PerkType.Passive,
                category = PerkCategory.Stealth,
                rarity = PerkRarity.Uncommon,
                maxLevel = 5,
                effects = new List<PerkEffect>
                {
                    new PerkEffect { effectType = EffectType.Stealth, value = 0.10f, valuePerLevel = 0.08f }
                },
                requiredLevel = 8,
                unlockCost = 400
            };

            perkDatabase["stealth_backstab"] = new PerkDefinition
            {
                perkId = "stealth_backstab",
                perkName = "Assassin",
                description = "Massive damage bonus when attacking from behind",
                perkType = PerkType.Passive,
                category = PerkCategory.Stealth,
                rarity = PerkRarity.Epic,
                maxLevel = 5,
                effects = new List<PerkEffect>
                {
                    new PerkEffect { effectType = EffectType.BackstabDamage, value = 0.50f, valuePerLevel = 0.30f }
                },
                requiredLevel = 25,
                unlockCost = 3000
            };

            perkDatabase["stealth_invisible"] = new PerkDefinition
            {
                perkId = "stealth_invisible",
                perkName = "Ghost",
                description = "Become invisible for a short duration",
                perkType = PerkType.Active,
                category = PerkCategory.Stealth,
                rarity = PerkRarity.Legendary,
                maxLevel = 3,
                effects = new List<PerkEffect>
                {
                    new PerkEffect { effectType = EffectType.Invisibility, value = 1f, duration = 5f, durationPerLevel = 2f }
                },
                cooldown = 90f,
                requiredLevel = 35,
                unlockCost = 6000
            };

            // ===== TEAM PERKS =====

            perkDatabase["team_aura_damage"] = new PerkDefinition
            {
                perkId = "team_aura_damage",
                perkName = "Inspiring Presence",
                description = "Nearby allies gain damage bonus",
                perkType = PerkType.Passive,
                category = PerkCategory.Team,
                rarity = PerkRarity.Rare,
                maxLevel = 5,
                effects = new List<PerkEffect>
                {
                    new PerkEffect { effectType = EffectType.AllyDamageBonus, value = 0.08f, valuePerLevel = 0.05f, radius = 20f }
                },
                requiredLevel = 18,
                unlockCost = 1500
            };

            perkDatabase["team_aura_defense"] = new PerkDefinition
            {
                perkId = "team_aura_defense",
                perkName = "Protective Aura",
                description = "Nearby allies take reduced damage",
                perkType = PerkType.Passive,
                category = PerkCategory.Team,
                rarity = PerkRarity.Epic,
                maxLevel = 5,
                effects = new List<PerkEffect>
                {
                    new PerkEffect { effectType = EffectType.AllyDamageReduction, value = 0.08f, valuePerLevel = 0.05f, radius = 20f }
                },
                requiredLevel = 24,
                unlockCost = 2800
            };

            perkDatabase["team_revive"] = new PerkDefinition
            {
                perkId = "team_revive",
                perkName = "Field Medic",
                description = "Faster revive speed and revived allies have bonus health",
                perkType = PerkType.Passive,
                category = PerkCategory.Team,
                rarity = PerkRarity.Rare,
                maxLevel = 5,
                effects = new List<PerkEffect>
                {
                    new PerkEffect { effectType = EffectType.ReviveSpeed, value = 0.25f, valuePerLevel = 0.15f },
                    new PerkEffect { effectType = EffectType.ReviveHealth, value = 0.20f, valuePerLevel = 0.15f }
                },
                requiredLevel = 16,
                unlockCost = 1200
            };

            perkDatabase["team_buff"] = new PerkDefinition
            {
                perkId = "team_buff",
                perkName = "Battle Cry",
                description = "Activate to give all nearby allies temporary combat bonuses",
                perkType = PerkType.Active,
                category = PerkCategory.Team,
                rarity = PerkRarity.Legendary,
                maxLevel = 3,
                effects = new List<PerkEffect>
                {
                    new PerkEffect { effectType = EffectType.AllyDamageBonus, value = 0.30f, duration = 15f, radius = 30f },
                    new PerkEffect { effectType = EffectType.AllyMovementSpeed, value = 0.20f, duration = 15f, radius = 30f }
                },
                cooldown = 120f,
                requiredLevel = 38,
                unlockCost = 7000
            };

            // ===== SPECIAL PERKS =====

            perkDatabase["special_vampire"] = new PerkDefinition
            {
                perkId = "special_vampire",
                perkName = "Vampirism",
                description = "Heal for a percentage of damage dealt",
                perkType = PerkType.Passive,
                category = PerkCategory.Special,
                rarity = PerkRarity.Legendary,
                maxLevel = 5,
                effects = new List<PerkEffect>
                {
                    new PerkEffect { effectType = EffectType.Lifesteal, value = 0.05f, valuePerLevel = 0.03f }
                },
                requiredLevel = 32,
                unlockCost = 5000
            };

            perkDatabase["special_phoenix"] = new PerkDefinition
            {
                perkId = "special_phoenix",
                perkName = "Phoenix",
                description = "Automatically revive on death",
                perkType = PerkType.Active,
                category = PerkCategory.Special,
                rarity = PerkRarity.Legendary,
                maxLevel = 1,
                effects = new List<PerkEffect>
                {
                    new PerkEffect { effectType = EffectType.AutoRevive, value = 0.5f }
                },
                cooldown = 600f,
                requiredLevel = 50,
                unlockCost = 10000
            };

            perkDatabase["special_scavenger"] = new PerkDefinition
            {
                perkId = "special_scavenger",
                perkName = "Master Scavenger",
                description = "Killed enemies have a chance to drop extra loot",
                perkType = PerkType.Passive,
                category = PerkCategory.Special,
                rarity = PerkRarity.Epic,
                maxLevel = 5,
                effects = new List<PerkEffect>
                {
                    new PerkEffect { effectType = EffectType.ExtraLootChance, value = 0.10f, valuePerLevel = 0.08f }
                },
                requiredLevel = 28,
                unlockCost = 4000
            };

            perkDatabase["special_tank"] = new PerkDefinition
            {
                perkId = "special_tank",
                perkName = "Juggernaut",
                description = "Massively increased health and defense, reduced movement speed",
                perkType = PerkType.Passive,
                category = PerkCategory.Special,
                rarity = PerkRarity.Legendary,
                maxLevel = 3,
                effects = new List<PerkEffect>
                {
                    new PerkEffect { effectType = EffectType.MaxHealth, value = 0.50f, valuePerLevel = 0.30f },
                    new PerkEffect { effectType = EffectType.DamageReduction, value = 0.20f, valuePerLevel = 0.10f },
                    new PerkEffect { effectType = EffectType.MovementSpeed, value = -0.15f }
                },
                requiredLevel = 42,
                unlockCost = 8000
            };

            perkDatabase["special_glass_cannon"] = new PerkDefinition
            {
                perkId = "special_glass_cannon",
                perkName = "Glass Cannon",
                description = "Massive damage increase, significantly reduced health",
                perkType = PerkType.Passive,
                category = PerkCategory.Special,
                rarity = PerkRarity.Epic,
                maxLevel = 3,
                effects = new List<PerkEffect>
                {
                    new PerkEffect { effectType = EffectType.DamageBonus, value = 0.40f, valuePerLevel = 0.20f },
                    new PerkEffect { effectType = EffectType.MaxHealth, value = -0.30f }
                },
                requiredLevel = 30,
                unlockCost = 4500
            };

            Debug.Log($"Initialized {perkDatabase.Count} perk definitions");
        }

        private void InitializePerkTrees()
        {
            perkTrees["combat_tree"] = new List<string>
            {
                "combat_damage_basic",
                "combat_reload",
                "combat_headshot",
                "combat_crit",
                "combat_berserker"
            };

            perkTrees["survival_tree"] = new List<string>
            {
                "survival_health",
                "survival_medic",
                "survival_regen",
                "survival_armor",
                "survival_laststand"
            };

            perkTrees["mobility_tree"] = new List<string>
            {
                "mobility_speed",
                "mobility_sprint",
                "mobility_dash",
                "mobility_dodge"
            };

            perkTrees["utility_tree"] = new List<string>
            {
                "utility_xp",
                "utility_repair",
                "utility_ammo",
                "utility_loot",
                "utility_scan"
            };

            perkTrees["stealth_tree"] = new List<string>
            {
                "stealth_basic",
                "stealth_backstab",
                "stealth_invisible"
            };

            perkTrees["team_tree"] = new List<string>
            {
                "team_revive",
                "team_aura_damage",
                "team_aura_defense",
                "team_buff"
            };

            perkTrees["special_tree"] = new List<string>
            {
                "special_scavenger",
                "special_vampire",
                "special_glass_cannon",
                "special_tank",
                "special_phoenix"
            };

            Debug.Log($"Initialized {perkTrees.Count} perk trees");
        }

        private void InitializePerkSynergies()
        {
            // Combat synergies
            perkSynergies["combat_synergy_1"] = new PerkSynergy
            {
                synergyId = "combat_synergy_1",
                name = "Marksman",
                requiredPerks = new List<string> { "combat_headshot", "combat_crit" },
                bonusEffect = new PerkEffect { effectType = EffectType.HeadshotDamage, value = 0.20f }
            };

            perkSynergies["combat_synergy_2"] = new PerkSynergy
            {
                synergyId = "combat_synergy_2",
                name = "Warrior",
                requiredPerks = new List<string> { "combat_damage_basic", "combat_berserker" },
                bonusEffect = new PerkEffect { effectType = EffectType.DamageBonus, value = 0.15f }
            };

            // Survival synergies
            perkSynergies["survival_synergy_1"] = new PerkSynergy
            {
                synergyId = "survival_synergy_1",
                name = "Unbreakable",
                requiredPerks = new List<string> { "survival_health", "survival_armor" },
                bonusEffect = new PerkEffect { effectType = EffectType.MaxHealth, value = 0.15f }
            };

            perkSynergies["survival_synergy_2"] = new PerkSynergy
            {
                synergyId = "survival_synergy_2",
                name = "Immortal",
                requiredPerks = new List<string> { "survival_regen", "survival_laststand" },
                bonusEffect = new PerkEffect { effectType = EffectType.HealthRegen, value = 2f }
            };

            // Mobility synergies
            perkSynergies["mobility_synergy_1"] = new PerkSynergy
            {
                synergyId = "mobility_synergy_1",
                name = "Speed Demon",
                requiredPerks = new List<string> { "mobility_speed", "mobility_sprint" },
                bonusEffect = new PerkEffect { effectType = EffectType.MovementSpeed, value = 0.10f }
            };

            // Team synergies
            perkSynergies["team_synergy_1"] = new PerkSynergy
            {
                synergyId = "team_synergy_1",
                name = "Commander",
                requiredPerks = new List<string> { "team_aura_damage", "team_buff" },
                bonusEffect = new PerkEffect { effectType = EffectType.AllyDamageBonus, value = 0.10f, radius = 25f }
            };

            Debug.Log($"Initialized {perkSynergies.Count} perk synergies");
        }

        // Main perk operations
        [ServerRpc(RequireOwnership = false)]
        public void UnlockPerkServerRpc(ulong playerId, string perkId, ServerRpcParams rpcParams = default)
        {
            if (!perkDatabase.TryGetValue(perkId, out var perk)) return;

            if (!playerPerks.ContainsKey(playerId))
            {
                playerPerks[playerId] = new PlayerPerks
                {
                    playerId = playerId,
                    unlockedPerks = new List<string>(),
                    equippedPerks = new List<string>(),
                    perkLevels = new Dictionary<string, int>(),
                    activePerkCooldowns = new Dictionary<string, float>()
                };
            }

            var playerPerkData = playerPerks[playerId];

            // Check requirements
            if (playerPerkData.unlockedPerks.Contains(perkId))
            {
                Debug.LogWarning($"Player {playerId} already has perk {perkId}");
                return;
            }

            // Would check player level and currency here
            // For now, just unlock it
            playerPerkData.unlockedPerks.Add(perkId);
            playerPerkData.perkLevels[perkId] = 1;

            OnPerkUnlocked?.Invoke(playerId, perkId);
            NotifyPerkUnlockedClientRpc(playerId, perkId);

            Debug.Log($"Player {playerId} unlocked perk {perk.perkName}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void EquipPerkServerRpc(ulong playerId, string perkId, ServerRpcParams rpcParams = default)
        {
            if (!playerPerks.TryGetValue(playerId, out var playerPerkData)) return;
            if (!playerPerkData.unlockedPerks.Contains(perkId)) return;

            if (playerPerkData.equippedPerks.Contains(perkId))
            {
                // Unequip
                playerPerkData.equippedPerks.Remove(perkId);
                Debug.Log($"Player {playerId} unequipped perk {perkId}");
            }
            else
            {
                // Equip
                if (playerPerkData.equippedPerks.Count >= maxEquippedPerks)
                {
                    Debug.LogWarning($"Player {playerId} has max equipped perks");
                    return;
                }

                playerPerkData.equippedPerks.Add(perkId);
                OnPerkEquipped?.Invoke(playerId, perkId);
                Debug.Log($"Player {playerId} equipped perk {perkId}");
            }

            NotifyPerksChangedClientRpc(playerId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void UpgradePerkServerRpc(ulong playerId, string perkId, ServerRpcParams rpcParams = default)
        {
            if (!playerPerks.TryGetValue(playerId, out var playerPerkData)) return;
            if (!playerPerkData.perkLevels.ContainsKey(perkId)) return;
            if (!perkDatabase.TryGetValue(perkId, out var perk)) return;

            int currentLevel = playerPerkData.perkLevels[perkId];
            if (currentLevel >= perk.maxLevel)
            {
                Debug.LogWarning($"Perk {perkId} is already max level");
                return;
            }

            // Would check currency here
            int upgradeCost = perk.unlockCost * (currentLevel + 1);

            playerPerkData.perkLevels[perkId]++;

            OnPerkUpgraded?.Invoke(playerId, perkId, playerPerkData.perkLevels[perkId]);
            NotifyPerkUpgradedClientRpc(playerId, perkId, playerPerkData.perkLevels[perkId]);

            Debug.Log($"Player {playerId} upgraded {perk.perkName} to level {playerPerkData.perkLevels[perkId]}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void ActivatePerkServerRpc(ulong playerId, string perkId, ServerRpcParams rpcParams = default)
        {
            if (!playerPerks.TryGetValue(playerId, out var playerPerkData)) return;
            if (!playerPerkData.equippedPerks.Contains(perkId)) return;
            if (!perkDatabase.TryGetValue(perkId, out var perk)) return;

            if (perk.perkType != PerkType.Active)
            {
                Debug.LogWarning($"Perk {perkId} is not an active perk");
                return;
            }

            // Check cooldown
            if (playerPerkData.activePerkCooldowns.ContainsKey(perkId))
            {
                float remainingCooldown = playerPerkData.activePerkCooldowns[perkId] - Time.time;
                if (remainingCooldown > 0)
                {
                    Debug.LogWarning($"Perk {perkId} is on cooldown for {remainingCooldown}s");
                    return;
                }
            }

            // Activate perk
            ApplyActivePerkEffects(playerId, perk, playerPerkData.perkLevels[perkId]);
            playerPerkData.activePerkCooldowns[perkId] = Time.time + perk.cooldown;

            OnPerkActivated?.Invoke(playerId, perkId);
            NotifyPerkActivatedClientRpc(playerId, perkId);

            Debug.Log($"Player {playerId} activated {perk.perkName}");
        }

        private void ApplyActivePerkEffects(ulong playerId, PerkDefinition perk, int level)
        {
            // Apply temporary effects based on perk definition
            // This would integrate with a buff/debuff system
            foreach (var effect in perk.effects)
            {
                float totalValue = effect.value + (effect.valuePerLevel * (level - 1));
                float totalDuration = effect.duration + (effect.durationPerLevel * (level - 1));

                // Apply effect to player
                Debug.Log($"Applying {effect.effectType}: {totalValue} for {totalDuration}s");
            }
        }

        public float GetPerkBonus(ulong playerId, EffectType effectType)
        {
            if (!playerPerks.TryGetValue(playerId, out var playerPerkData)) return 0f;

            float totalBonus = 0f;

            foreach (var perkId in playerPerkData.equippedPerks)
            {
                if (!perkDatabase.TryGetValue(perkId, out var perk)) continue;
                if (perk.perkType != PerkType.Passive) continue;

                int level = playerPerkData.perkLevels.GetValueOrDefault(perkId, 1);

                foreach (var effect in perk.effects)
                {
                    if (effect.effectType == effectType)
                    {
                        totalBonus += effect.value + (effect.valuePerLevel * (level - 1));
                    }
                }
            }

            // Add synergy bonuses
            if (enablePerkSynergies)
            {
                totalBonus += GetSynergyBonus(playerId, effectType);
            }

            return totalBonus;
        }

        private float GetSynergyBonus(ulong playerId, EffectType effectType)
        {
            if (!playerPerks.TryGetValue(playerId, out var playerPerkData)) return 0f;

            float synergyBonus = 0f;

            foreach (var synergy in perkSynergies.Values)
            {
                bool hasAll = synergy.requiredPerks.All(p => playerPerkData.equippedPerks.Contains(p));
                if (hasAll && synergy.bonusEffect.effectType == effectType)
                {
                    synergyBonus += synergy.bonusEffect.value;
                }
            }

            return synergyBonus;
        }

        // Client RPCs
        [ClientRpc]
        private void NotifyPerkUnlockedClientRpc(ulong playerId, string perkId) { }

        [ClientRpc]
        private void NotifyPerksChangedClientRpc(ulong playerId) { }

        [ClientRpc]
        private void NotifyPerkUpgradedClientRpc(ulong playerId, string perkId, int newLevel) { }

        [ClientRpc]
        private void NotifyPerkActivatedClientRpc(ulong playerId, string perkId) { }

        // Public getters
        public PerkDefinition GetPerkDefinition(string perkId) => perkDatabase.GetValueOrDefault(perkId);
        public PlayerPerks GetPlayerPerks(ulong playerId) => playerPerks.GetValueOrDefault(playerId);
        public List<string> GetPerkTree(string treeName) => perkTrees.GetValueOrDefault(treeName);
        public List<PerkDefinition> GetAvailablePerks(ulong playerId, int playerLevel)
        {
            return perkDatabase.Values.Where(p => p.requiredLevel <= playerLevel).ToList();
        }
    }

    // Data structures
    [Serializable]
    public class PerkDefinition
    {
        public string perkId;
        public string perkName;
        public string description;
        public PerkType perkType;
        public PerkCategory category;
        public PerkRarity rarity;
        public int maxLevel;
        public List<PerkEffect> effects;
        public float cooldown;
        public int requiredLevel;
        public int unlockCost;
    }

    [Serializable]
    public class PerkEffect
    {
        public EffectType effectType;
        public float value;
        public float valuePerLevel;
        public float duration;
        public float durationPerLevel;
        public float radius;
    }

    [Serializable]
    public class PlayerPerks
    {
        public ulong playerId;
        public List<string> unlockedPerks;
        public List<string> equippedPerks;
        public Dictionary<string, int> perkLevels;
        public Dictionary<string, float> activePerkCooldowns;
    }

    [Serializable]
    public class PerkSynergy
    {
        public string synergyId;
        public string name;
        public List<string> requiredPerks;
        public PerkEffect bonusEffect;
    }

    public enum PerkType { Passive, Active }
    public enum PerkCategory { Combat, Survival, Mobility, Utility, Stealth, Team, Special }
    public enum PerkRarity { Common, Uncommon, Rare, Epic, Legendary }
    public enum EffectType
    {
        // Combat
        DamageBonus, HeadshotDamage, CritChance, CritDamage, ReloadSpeed, WeaponSwapSpeed, Accuracy,
        // Survival
        MaxHealth, HealthRegen, DamageReduction, HealingBonus, HealingSpeed, Invulnerability,
        // Mobility
        MovementSpeed, StaminaCost, StaminaRegen, DodgeChance,
        // Utility
        AmmoConservation, LootQuality, LootQuantity, RepairCost, BuildSpeed, XpBonus, Detection,
        // Stealth
        Stealth, BackstabDamage, Invisibility,
        // Team
        AllyDamageBonus, AllyDamageReduction, AllyMovementSpeed, ReviveSpeed, ReviveHealth,
        // Special
        Lifesteal, AutoRevive, ExtraLootChance
    }
}
