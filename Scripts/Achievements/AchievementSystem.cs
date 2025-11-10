using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Achievements
{
    /// <summary>
    /// Comprehensive achievement system with categories, tiers, hidden achievements,
    /// achievement chains, tracking, rewards, and showcase features.
    /// </summary>
    public class AchievementSystem : NetworkBehaviour
    {
        public static AchievementSystem Instance { get; private set; }

        [Header("Achievement Configuration")]
        [SerializeField] private int maxShowcaseSlots = 5;

        // Achievement data
        private Dictionary<string, Achievement> achievementDatabase = new Dictionary<string, Achievement>();
        private Dictionary<ulong, PlayerAchievementData> playerAchievementData = new Dictionary<ulong, PlayerAchievementData>();

        // Events
        public event Action<ulong, string> OnAchievementUnlocked;
        public event Action<ulong, string, int> OnAchievementProgress;

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
                InitializeAchievements();
            }
        }

        private void InitializeAchievements()
        {
            // ===== COMBAT ACHIEVEMENTS =====
            
            // Zombie Kills
            achievementDatabase["combat_kills_100"] = new Achievement
            {
                achievementId = "combat_kills_100",
                achievementName = "Zombie Hunter",
                description = "Kill 100 zombies",
                category = AchievementCategory.Combat,
                tier = AchievementTier.Bronze,
                objectiveType = AchievementObjectiveType.Kill,
                targetId = "zombie_any",
                requiredAmount = 100,
                rewards = new AchievementRewards { xp = 500, softCurrency = 100, title = "Zombie Hunter" }
            };

            achievementDatabase["combat_kills_1000"] = new Achievement
            {
                achievementId = "combat_kills_1000",
                achievementName = "Zombie Slayer",
                description = "Kill 1,000 zombies",
                category = AchievementCategory.Combat,
                tier = AchievementTier.Silver,
                objectiveType = AchievementObjectiveType.Kill,
                targetId = "zombie_any",
                requiredAmount = 1000,
                prerequisiteAchievements = new List<string> { "combat_kills_100" },
                rewards = new AchievementRewards { xp = 2000, softCurrency = 500, title = "Zombie Slayer" }
            };

            achievementDatabase["combat_kills_10000"] = new Achievement
            {
                achievementId = "combat_kills_10000",
                achievementName = "Zombie Annihilator",
                description = "Kill 10,000 zombies",
                category = AchievementCategory.Combat,
                tier = AchievementTier.Gold,
                objectiveType = AchievementObjectiveType.Kill,
                targetId = "zombie_any",
                requiredAmount = 10000,
                prerequisiteAchievements = new List<string> { "combat_kills_1000" },
                rewards = new AchievementRewards { xp = 10000, softCurrency = 5000, hardCurrency = 50, title = "Zombie Annihilator", cosmetic = "effect_blood_aura" }
            };

            achievementDatabase["combat_kills_100000"] = new Achievement
            {
                achievementId = "combat_kills_100000",
                achievementName = "Legend of the Apocalypse",
                description = "Kill 100,000 zombies",
                category = AchievementCategory.Combat,
                tier = AchievementTier.Platinum,
                objectiveType = AchievementObjectiveType.Kill,
                targetId = "zombie_any",
                requiredAmount = 100000,
                prerequisiteAchievements = new List<string> { "combat_kills_10000" },
                rewards = new AchievementRewards { xp = 50000, softCurrency = 50000, hardCurrency = 500, title = "Legend of the Apocalypse", cosmetic = "skin_legendary_survivor" }
            };

            // Headshots
            achievementDatabase["combat_headshots_100"] = new Achievement
            {
                achievementId = "combat_headshots_100",
                achievementName = "Marksman",
                description = "Get 100 headshot kills",
                category = AchievementCategory.Combat,
                tier = AchievementTier.Bronze,
                objectiveType = AchievementObjectiveType.HeadshotKill,
                requiredAmount = 100,
                rewards = new AchievementRewards { xp = 750, softCurrency = 200 }
            };

            achievementDatabase["combat_headshots_1000"] = new Achievement
            {
                achievementId = "combat_headshots_1000",
                achievementName = "Sharpshooter",
                description = "Get 1,000 headshot kills",
                category = AchievementCategory.Combat,
                tier = AchievementTier.Gold,
                objectiveType = AchievementObjectiveType.HeadshotKill,
                requiredAmount = 1000,
                prerequisiteAchievements = new List<string> { "combat_headshots_100" },
                rewards = new AchievementRewards { xp = 5000, softCurrency = 2000, hardCurrency = 25, cosmetic = "charm_sniper_bullet" }
            };

            // Boss Kills
            achievementDatabase["combat_boss_first"] = new Achievement
            {
                achievementId = "combat_boss_first",
                achievementName = "Boss Hunter",
                description = "Defeat your first world boss",
                category = AchievementCategory.Combat,
                tier = AchievementTier.Silver,
                objectiveType = AchievementObjectiveType.Kill,
                targetId = "boss_any",
                requiredAmount = 1,
                rewards = new AchievementRewards { xp = 1000, softCurrency = 500 }
            };

            achievementDatabase["combat_boss_all"] = new Achievement
            {
                achievementId = "combat_boss_all",
                achievementName = "Boss Master",
                description = "Defeat all unique world bosses",
                category = AchievementCategory.Combat,
                tier = AchievementTier.Platinum,
                objectiveType = AchievementObjectiveType.KillUnique,
                targetIds = new List<string> { "boss_tank", "boss_screamer", "boss_alpha", "boss_colossus", "boss_abomination" },
                rewards = new AchievementRewards { xp = 25000, softCurrency = 10000, hardCurrency = 100, title = "Boss Master", cosmetic = "skin_boss_slayer" }
            };

            // ===== SURVIVAL ACHIEVEMENTS =====

            achievementDatabase["survival_days_1"] = new Achievement
            {
                achievementId = "survival_days_1",
                achievementName = "First Night",
                description = "Survive your first night",
                category = AchievementCategory.Survival,
                tier = AchievementTier.Bronze,
                objectiveType = AchievementObjectiveType.Survive,
                requiredAmount = 1,
                rewards = new AchievementRewards { xp = 100, softCurrency = 50 }
            };

            achievementDatabase["survival_days_7"] = new Achievement
            {
                achievementId = "survival_days_7",
                achievementName = "Week Survivor",
                description = "Survive 7 consecutive nights",
                category = AchievementCategory.Survival,
                tier = AchievementTier.Silver,
                objectiveType = AchievementObjectiveType.Survive,
                requiredAmount = 7,
                rewards = new AchievementRewards { xp = 1000, softCurrency = 500 }
            };

            achievementDatabase["survival_days_30"] = new Achievement
            {
                achievementId = "survival_days_30",
                achievementName = "Month Survivor",
                description = "Survive 30 consecutive nights",
                category = AchievementCategory.Survival,
                tier = AchievementTier.Gold,
                objectiveType = AchievementObjectiveType.Survive,
                requiredAmount = 30,
                rewards = new AchievementRewards { xp = 5000, softCurrency = 5000, hardCurrency = 50, title = "Long-term Survivor" }
            };

            achievementDatabase["survival_no_damage"] = new Achievement
            {
                achievementId = "survival_no_damage",
                achievementName = "Untouchable",
                description = "Complete a mission without taking damage",
                category = AchievementCategory.Survival,
                tier = AchievementTier.Gold,
                objectiveType = AchievementObjectiveType.NoDamage,
                requiredAmount = 1,
                rewards = new AchievementRewards { xp = 2000, softCurrency = 1000, hardCurrency = 25 }
            };

            // ===== EXPLORATION ACHIEVEMENTS =====

            achievementDatabase["exploration_distance_10k"] = new Achievement
            {
                achievementId = "exploration_distance_10k",
                achievementName = "Wanderer",
                description = "Travel 10,000 meters",
                category = AchievementCategory.Exploration,
                tier = AchievementTier.Bronze,
                objectiveType = AchievementObjectiveType.Distance,
                requiredAmount = 10000,
                rewards = new AchievementRewards { xp = 500, softCurrency = 200 }
            };

            achievementDatabase["exploration_distance_100k"] = new Achievement
            {
                achievementId = "exploration_distance_100k",
                achievementName = "Explorer",
                description = "Travel 100,000 meters",
                category = AchievementCategory.Exploration,
                tier = AchievementTier.Silver,
                objectiveType = AchievementObjectiveType.Distance,
                requiredAmount = 100000,
                rewards = new AchievementRewards { xp = 2000, softCurrency = 1000 }
            };

            achievementDatabase["exploration_locations_all"] = new Achievement
            {
                achievementId = "exploration_locations_all",
                achievementName = "Cartographer",
                description = "Discover all locations on the map",
                category = AchievementCategory.Exploration,
                tier = AchievementTier.Platinum,
                objectiveType = AchievementObjectiveType.DiscoverLocations,
                requiredAmount = 50,
                rewards = new AchievementRewards { xp = 10000, softCurrency = 5000, hardCurrency = 100, title = "Master Explorer", cosmetic = "charm_compass" }
            };

            // ===== CRAFTING ACHIEVEMENTS =====

            achievementDatabase["crafting_items_100"] = new Achievement
            {
                achievementId = "crafting_items_100",
                achievementName = "Apprentice Crafter",
                description = "Craft 100 items",
                category = AchievementCategory.Crafting,
                tier = AchievementTier.Bronze,
                objectiveType = AchievementObjectiveType.Craft,
                requiredAmount = 100,
                rewards = new AchievementRewards { xp = 500, softCurrency = 200 }
            };

            achievementDatabase["crafting_items_1000"] = new Achievement
            {
                achievementId = "crafting_items_1000",
                achievementName = "Master Crafter",
                description = "Craft 1,000 items",
                category = AchievementCategory.Crafting,
                tier = AchievementTier.Gold,
                objectiveType = AchievementObjectiveType.Craft,
                requiredAmount = 1000,
                rewards = new AchievementRewards { xp = 5000, softCurrency = 2000, hardCurrency = 50, title = "Master Crafter" }
            };

            achievementDatabase["crafting_legendary"] = new Achievement
            {
                achievementId = "crafting_legendary",
                achievementName = "Legendary Artisan",
                description = "Craft a legendary quality item",
                category = AchievementCategory.Crafting,
                tier = AchievementTier.Platinum,
                objectiveType = AchievementObjectiveType.CraftQuality,
                targetId = "legendary",
                requiredAmount = 1,
                rewards = new AchievementRewards { xp = 10000, softCurrency = 5000, hardCurrency = 100, cosmetic = "effect_crafting_aura" }
            };

            // ===== SOCIAL ACHIEVEMENTS =====

            achievementDatabase["social_party_missions_10"] = new Achievement
            {
                achievementId = "social_party_missions_10",
                achievementName = "Team Player",
                description = "Complete 10 missions in a party",
                category = AchievementCategory.Social,
                tier = AchievementTier.Bronze,
                objectiveType = AchievementObjectiveType.PartyMission,
                requiredAmount = 10,
                rewards = new AchievementRewards { xp = 500, softCurrency = 200 }
            };

            achievementDatabase["social_clan_create"] = new Achievement
            {
                achievementId = "social_clan_create",
                achievementName = "Clan Founder",
                description = "Create a clan",
                category = AchievementCategory.Social,
                tier = AchievementTier.Silver,
                objectiveType = AchievementObjectiveType.ClanCreate,
                requiredAmount = 1,
                rewards = new AchievementRewards { xp = 1000, softCurrency = 500 }
            };

            achievementDatabase["social_clan_max_level"] = new Achievement
            {
                achievementId = "social_clan_max_level",
                achievementName = "Clan Legend",
                description = "Reach max clan level",
                category = AchievementCategory.Social,
                tier = AchievementTier.Platinum,
                objectiveType = AchievementObjectiveType.ClanLevel,
                requiredAmount = 20,
                rewards = new AchievementRewards { xp = 20000, softCurrency = 10000, hardCurrency = 200, title = "Clan Legend" }
            };

            achievementDatabase["social_trades_100"] = new Achievement
            {
                achievementId = "social_trades_100",
                achievementName = "Merchant",
                description = "Complete 100 player trades",
                category = AchievementCategory.Social,
                tier = AchievementTier.Gold,
                objectiveType = AchievementObjectiveType.Trade,
                requiredAmount = 100,
                rewards = new AchievementRewards { xp = 5000, softCurrency = 2000, hardCurrency = 50, title = "Merchant" }
            };

            // ===== COLLECTION ACHIEVEMENTS =====

            achievementDatabase["collection_weapons_10"] = new Achievement
            {
                achievementId = "collection_weapons_10",
                achievementName = "Arms Collector",
                description = "Collect 10 unique weapons",
                category = AchievementCategory.Collection,
                tier = AchievementTier.Silver,
                objectiveType = AchievementObjectiveType.CollectUnique,
                targetId = "weapon",
                requiredAmount = 10,
                rewards = new AchievementRewards { xp = 1000, softCurrency = 500 }
            };

            achievementDatabase["collection_weapons_all"] = new Achievement
            {
                achievementId = "collection_weapons_all",
                achievementName = "Arsenal Master",
                description = "Collect all weapons in the game",
                category = AchievementCategory.Collection,
                tier = AchievementTier.Platinum,
                objectiveType = AchievementObjectiveType.CollectUnique,
                targetId = "weapon",
                requiredAmount = 50,
                rewards = new AchievementRewards { xp = 25000, softCurrency = 20000, hardCurrency = 250, title = "Arsenal Master", cosmetic = "skin_weapon_collector" }
            };

            achievementDatabase["collection_cosmetics_25"] = new Achievement
            {
                achievementId = "collection_cosmetics_25",
                achievementName = "Fashion Icon",
                description = "Collect 25 cosmetic items",
                category = AchievementCategory.Collection,
                tier = AchievementTier.Gold,
                objectiveType = AchievementObjectiveType.CollectUnique,
                targetId = "cosmetic",
                requiredAmount = 25,
                rewards = new AchievementRewards { xp = 5000, softCurrency = 2000, hardCurrency = 50, title = "Fashion Icon" }
            };

            // ===== PROGRESSION ACHIEVEMENTS =====

            achievementDatabase["progression_level_25"] = new Achievement
            {
                achievementId = "progression_level_25",
                achievementName = "Veteran",
                description = "Reach level 25",
                category = AchievementCategory.Progression,
                tier = AchievementTier.Silver,
                objectiveType = AchievementObjectiveType.Level,
                requiredAmount = 25,
                rewards = new AchievementRewards { xp = 2000, softCurrency = 1000, title = "Veteran" }
            };

            achievementDatabase["progression_level_50"] = new Achievement
            {
                achievementId = "progression_level_50",
                achievementName = "Elite",
                description = "Reach level 50",
                category = AchievementCategory.Progression,
                tier = AchievementTier.Gold,
                objectiveType = AchievementObjectiveType.Level,
                requiredAmount = 50,
                rewards = new AchievementRewards { xp = 5000, softCurrency = 5000, hardCurrency = 50, title = "Elite Survivor" }
            };

            achievementDatabase["progression_level_100"] = new Achievement
            {
                achievementId = "progression_level_100",
                achievementName = "Maxed Out",
                description = "Reach max level",
                category = AchievementCategory.Progression,
                tier = AchievementTier.Platinum,
                objectiveType = AchievementObjectiveType.Level,
                requiredAmount = 100,
                rewards = new AchievementRewards { xp = 0, softCurrency = 50000, hardCurrency = 500, title = "Living Legend", cosmetic = "effect_max_level_aura" }
            };

            achievementDatabase["progression_prestige"] = new Achievement
            {
                achievementId = "progression_prestige",
                achievementName = "Prestige",
                description = "Prestige for the first time",
                category = AchievementCategory.Progression,
                tier = AchievementTier.Platinum,
                objectiveType = AchievementObjectiveType.Prestige,
                requiredAmount = 1,
                rewards = new AchievementRewards { xp = 10000, softCurrency = 10000, hardCurrency = 100, title = "Prestige I" }
            };

            // ===== SPECIAL/HIDDEN ACHIEVEMENTS =====

            achievementDatabase["special_death_fall"] = new Achievement
            {
                achievementId = "special_death_fall",
                achievementName = "Gravity Check",
                description = "Die from fall damage",
                category = AchievementCategory.Special,
                tier = AchievementTier.Bronze,
                objectiveType = AchievementObjectiveType.DeathType,
                targetId = "fall",
                requiredAmount = 1,
                hidden = true,
                rewards = new AchievementRewards { xp = 50, softCurrency = 10 }
            };

            achievementDatabase["special_death_explosion"] = new Achievement
            {
                achievementId = "special_death_explosion",
                achievementName = "Explosive Personality",
                description = "Kill yourself with explosives",
                category = AchievementCategory.Special,
                tier = AchievementTier.Bronze,
                objectiveType = AchievementObjectiveType.DeathType,
                targetId = "explosion_self",
                requiredAmount = 1,
                hidden = true,
                rewards = new AchievementRewards { xp = 50, softCurrency = 10 }
            };

            achievementDatabase["special_melee_only"] = new Achievement
            {
                achievementId = "special_melee_only",
                achievementName = "Melee Master",
                description = "Complete a mission using only melee weapons",
                category = AchievementCategory.Special,
                tier = AchievementTier.Gold,
                objectiveType = AchievementObjectiveType.MissionConstraint,
                targetId = "melee_only",
                requiredAmount = 1,
                hidden = true,
                rewards = new AchievementRewards { xp = 5000, softCurrency = 2000, hardCurrency = 50, cosmetic = "charm_melee_master" }
            };

            achievementDatabase["special_no_death_100"] = new Achievement
            {
                achievementId = "special_no_death_100",
                achievementName = "Immortal",
                description = "Kill 100 zombies without dying",
                category = AchievementCategory.Special,
                tier = AchievementTier.Platinum,
                objectiveType = AchievementObjectiveType.KillStreak,
                requiredAmount = 100,
                hidden = true,
                rewards = new AchievementRewards { xp = 10000, softCurrency = 5000, hardCurrency = 100, title = "Immortal", cosmetic = "effect_immortal_glow" }
            };

            achievementDatabase["special_millionaire"] = new Achievement
            {
                achievementId = "special_millionaire",
                achievementName = "Millionaire",
                description = "Accumulate 1,000,000 soft currency",
                category = AchievementCategory.Special,
                tier = AchievementTier.Platinum,
                objectiveType = AchievementObjectiveType.Currency,
                requiredAmount = 1000000,
                hidden = true,
                rewards = new AchievementRewards { xp = 20000, hardCurrency = 200, title = "Apocalypse Millionaire" }
            };

            Debug.Log($"Initialized {achievementDatabase.Count} achievements");
        }

        // Main achievement operations
        [ServerRpc(RequireOwnership = false)]
        public void InitializePlayerAchievementsServerRpc(ulong playerId, ServerRpcParams rpcParams = default)
        {
            if (playerAchievementData.ContainsKey(playerId)) return;

            playerAchievementData[playerId] = new PlayerAchievementData
            {
                playerId = playerId,
                unlockedAchievements = new List<string>(),
                achievementProgress = new Dictionary<string, int>(),
                showcaseAchievements = new List<string>(),
                totalPoints = 0
            };

            // Initialize progress for all achievements
            foreach (var achievementId in achievementDatabase.Keys)
            {
                playerAchievementData[playerId].achievementProgress[achievementId] = 0;
            }

            Debug.Log($"Initialized achievements for player {playerId}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void UpdateAchievementProgressServerRpc(ulong playerId, AchievementObjectiveType objectiveType, string targetId, int amount, ServerRpcParams rpcParams = default)
        {
            if (!playerAchievementData.TryGetValue(playerId, out var data)) return;

            foreach (var achievement in achievementDatabase.Values)
            {
                // Skip if already unlocked
                if (data.unlockedAchievements.Contains(achievement.achievementId)) continue;

                // Check if objective type matches
                if (achievement.objectiveType != objectiveType) continue;

                // Check target ID if specified
                if (!string.IsNullOrEmpty(achievement.targetId) && achievement.targetId != targetId && targetId != "any") continue;

                // Check prerequisites
                if (achievement.prerequisiteAchievements != null && achievement.prerequisiteAchievements.Count > 0)
                {
                    bool hasAllPrereqs = achievement.prerequisiteAchievements.All(prereq => data.unlockedAchievements.Contains(prereq));
                    if (!hasAllPrereqs) continue;
                }

                // Update progress
                int currentProgress = data.achievementProgress[achievement.achievementId];
                data.achievementProgress[achievement.achievementId] = currentProgress + amount;

                OnAchievementProgress?.Invoke(playerId, achievement.achievementId, data.achievementProgress[achievement.achievementId]);

                // Check if unlocked
                if (data.achievementProgress[achievement.achievementId] >= achievement.requiredAmount)
                {
                    UnlockAchievement(playerId, achievement);
                }
            }
        }

        private void UnlockAchievement(ulong playerId, Achievement achievement)
        {
            if (!playerAchievementData.TryGetValue(playerId, out var data)) return;

            data.unlockedAchievements.Add(achievement.achievementId);
            data.totalPoints += GetAchievementPoints(achievement.tier);

            // Grant rewards
            if (achievement.rewards.xp > 0)
            {
                Progression.ProgressionSystem.Instance?.AddExperienceServerRpc(playerId, achievement.rewards.xp, "achievement");
            }

            if (achievement.rewards.softCurrency > 0)
            {
                Economy.EconomyManager.Instance?.AddCurrencyServerRpc(playerId, Economy.CurrencyType.Soft, achievement.rewards.softCurrency);
            }

            if (achievement.rewards.hardCurrency > 0)
            {
                Economy.EconomyManager.Instance?.AddCurrencyServerRpc(playerId, Economy.CurrencyType.Hard, achievement.rewards.hardCurrency);
            }

            OnAchievementUnlocked?.Invoke(playerId, achievement.achievementId);
            NotifyAchievementUnlockedClientRpc(playerId, achievement.achievementId, achievement.achievementName);

            Debug.Log($"Player {playerId} unlocked achievement: {achievement.achievementName}");
        }

        private int GetAchievementPoints(AchievementTier tier)
        {
            return tier switch
            {
                AchievementTier.Bronze => 10,
                AchievementTier.Silver => 25,
                AchievementTier.Gold => 50,
                AchievementTier.Platinum => 100,
                _ => 10
            };
        }

        [ServerRpc(RequireOwnership = false)]
        public void SetShowcaseAchievementsServerRpc(ulong playerId, List<string> achievementIds, ServerRpcParams rpcParams = default)
        {
            if (!playerAchievementData.TryGetValue(playerId, out var data)) return;

            // Validate achievements are unlocked
            var validAchievements = achievementIds.Where(id => data.unlockedAchievements.Contains(id)).Take(maxShowcaseSlots).ToList();
            data.showcaseAchievements = validAchievements;

            Debug.Log($"Player {playerId} updated achievement showcase");
        }

        // Client RPCs
        [ClientRpc]
        private void NotifyAchievementUnlockedClientRpc(ulong playerId, string achievementId, string achievementName) { }

        // Public getters
        public Achievement GetAchievement(string achievementId) => achievementDatabase.GetValueOrDefault(achievementId);
        public PlayerAchievementData GetPlayerAchievementData(ulong playerId) => playerAchievementData.GetValueOrDefault(playerId);
        public List<Achievement> GetAchievementsByCategory(AchievementCategory category) => 
            achievementDatabase.Values.Where(a => a.category == category).ToList();
        public int GetPlayerAchievementCompletion(ulong playerId)
        {
            if (!playerAchievementData.TryGetValue(playerId, out var data)) return 0;
            return Mathf.RoundToInt((float)data.unlockedAchievements.Count / achievementDatabase.Count * 100);
        }
    }

    // Data structures
    [Serializable]
    public class Achievement
    {
        public string achievementId;
        public string achievementName;
        public string description;
        public AchievementCategory category;
        public AchievementTier tier;
        public AchievementObjectiveType objectiveType;
        public string targetId;
        public List<string> targetIds;
        public int requiredAmount;
        public List<string> prerequisiteAchievements;
        public AchievementRewards rewards;
        public bool hidden;
    }

    [Serializable]
    public class AchievementRewards
    {
        public int xp;
        public int softCurrency;
        public int hardCurrency;
        public string title;
        public string cosmetic;
    }

    [Serializable]
    public class PlayerAchievementData
    {
        public ulong playerId;
        public List<string> unlockedAchievements;
        public Dictionary<string, int> achievementProgress;
        public List<string> showcaseAchievements;
        public int totalPoints;
    }

    public enum AchievementCategory { Combat, Survival, Exploration, Crafting, Social, Collection, Progression, Special }
    public enum AchievementTier { Bronze, Silver, Gold, Platinum }
    public enum AchievementObjectiveType 
    { 
        Kill, HeadshotKill, KillUnique, KillStreak,
        Survive, NoDamage,
        Distance, DiscoverLocations,
        Craft, CraftQuality,
        PartyMission, ClanCreate, ClanLevel, Trade,
        CollectUnique,
        Level, Prestige,
        DeathType, MissionConstraint, Currency
    }
}
