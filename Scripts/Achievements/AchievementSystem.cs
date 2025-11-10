using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Achievements
{
    /// <summary>
    /// Comprehensive achievement and trophy system.
    /// Tracks player accomplishments, awards rewards, and manages achievement progression.
    /// </summary>
    public class AchievementSystem : NetworkBehaviour
    {
        public static AchievementSystem Instance { get; private set; }

        [Header("Achievement Configuration")]
        [SerializeField] private int maxFeaturedAchievements = 3;
        [SerializeField] private bool enableHiddenAchievements = true;
        [SerializeField] private int achievementPointsPerTier = 10;
        [SerializeField] private bool enablePlatformTrophies = true;

        [Header("Rewards")]
        [SerializeField] private int baseRewardCurrency = 100;
        [SerializeField] private int tierMultiplier = 5;
        [SerializeField] private bool grantCosmeticRewards = true;

        [Header("Showcase")]
        [SerializeField] private int maxShowcaseSlots = 5;
        [SerializeField] private bool enableAchievementRarity = true;

        // Data structures
        private Dictionary<string, Achievement> achievementDatabase = new Dictionary<string, Achievement>();
        private Dictionary<ulong, PlayerAchievementData> playerAchievements = new Dictionary<ulong, PlayerAchievementData>();
        private Dictionary<string, List<string>> achievementChains = new Dictionary<string, List<string>>();
        private Dictionary<AchievementCategory, List<string>> categorizedAchievements = new Dictionary<AchievementCategory, List<string>>();

        // Events
        public event Action<ulong, string> OnAchievementUnlocked;
        public event Action<ulong, string, float> OnAchievementProgress;
        public event Action<ulong, int> OnAchievementPointsChanged;
        public event Action<ulong, string> OnAchievementShowcased;

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
                InitializeAchievements();
                InitializeAchievementChains();
                InitializeCategories();
            }
        }

        private void InitializeAchievements()
        {
            // Combat Achievements
            RegisterAchievement(new Achievement
            {
                achievementId = "first_blood",
                title = "First Blood",
                description = "Get your first kill",
                category = AchievementCategory.Combat,
                tier = AchievementTier.Bronze,
                isHidden = false,
                requirementType = RequirementType.KillCount,
                requiredProgress = 1,
                rewardCurrency = 100,
                rewardPoints = 10
            });

            RegisterAchievement(new Achievement
            {
                achievementId = "slayer",
                title = "Slayer",
                description = "Kill 100 zombies",
                category = AchievementCategory.Combat,
                tier = AchievementTier.Silver,
                isHidden = false,
                requirementType = RequirementType.KillCount,
                requiredProgress = 100,
                rewardCurrency = 500,
                rewardPoints = 25
            });

            RegisterAchievement(new Achievement
            {
                achievementId = "genocide",
                title = "Genocide",
                description = "Kill 1000 zombies",
                category = AchievementCategory.Combat,
                tier = AchievementTier.Gold,
                isHidden = false,
                requirementType = RequirementType.KillCount,
                requiredProgress = 1000,
                rewardCurrency = 2500,
                rewardPoints = 50,
                rewardCosmetic = "title_slayer"
            });

            RegisterAchievement(new Achievement
            {
                achievementId = "headhunter",
                title = "Headhunter",
                description = "Get 50 headshot kills",
                category = AchievementCategory.Combat,
                tier = AchievementTier.Silver,
                isHidden = false,
                requirementType = RequirementType.HeadshotKills,
                requiredProgress = 50,
                rewardCurrency = 750,
                rewardPoints = 30
            });

            RegisterAchievement(new Achievement
            {
                achievementId = "sharpshooter",
                title = "Sharpshooter",
                description = "Get 100 headshot kills",
                category = AchievementCategory.Combat,
                tier = AchievementTier.Gold,
                isHidden = false,
                requirementType = RequirementType.HeadshotKills,
                requiredProgress = 100,
                rewardCurrency = 1500,
                rewardPoints = 50
            });

            // Survival Achievements
            RegisterAchievement(new Achievement
            {
                achievementId = "survivor",
                title = "Survivor",
                description = "Survive for 30 minutes in a single session",
                category = AchievementCategory.Survival,
                tier = AchievementTier.Bronze,
                isHidden = false,
                requirementType = RequirementType.SurvivalTime,
                requiredProgress = 1800,
                rewardCurrency = 200,
                rewardPoints = 15
            });

            RegisterAchievement(new Achievement
            {
                achievementId = "lone_wolf",
                title = "Lone Wolf",
                description = "Survive for 1 hour solo",
                category = AchievementCategory.Survival,
                tier = AchievementTier.Gold,
                isHidden = false,
                requirementType = RequirementType.SoloSurvivalTime,
                requiredProgress = 3600,
                rewardCurrency = 1000,
                rewardPoints = 40
            });

            // Exploration Achievements
            RegisterAchievement(new Achievement
            {
                achievementId = "explorer",
                title = "Explorer",
                description = "Discover 10 locations",
                category = AchievementCategory.Exploration,
                tier = AchievementTier.Bronze,
                isHidden = false,
                requirementType = RequirementType.LocationsDiscovered,
                requiredProgress = 10,
                rewardCurrency = 300,
                rewardPoints = 15
            });

            RegisterAchievement(new Achievement
            {
                achievementId = "cartographer",
                title = "Cartographer",
                description = "Discover all locations",
                category = AchievementCategory.Exploration,
                tier = AchievementTier.Platinum,
                isHidden = false,
                requirementType = RequirementType.LocationsDiscovered,
                requiredProgress = 50,
                rewardCurrency = 5000,
                rewardPoints = 100,
                rewardCosmetic = "title_cartographer"
            });

            // Economy Achievements
            RegisterAchievement(new Achievement
            {
                achievementId = "entrepreneur",
                title = "Entrepreneur",
                description = "Earn 10,000 currency",
                category = AchievementCategory.Economy,
                tier = AchievementTier.Silver,
                isHidden = false,
                requirementType = RequirementType.CurrencyEarned,
                requiredProgress = 10000,
                rewardCurrency = 500,
                rewardPoints = 20
            });

            RegisterAchievement(new Achievement
            {
                achievementId = "tycoon",
                title = "Tycoon",
                description = "Earn 100,000 currency",
                category = AchievementCategory.Economy,
                tier = AchievementTier.Gold,
                isHidden = false,
                requirementType = RequirementType.CurrencyEarned,
                requiredProgress = 100000,
                rewardCurrency = 2000,
                rewardPoints = 50
            });

            // Crafting Achievements
            RegisterAchievement(new Achievement
            {
                achievementId = "craftsman",
                title = "Craftsman",
                description = "Craft 50 items",
                category = AchievementCategory.Crafting,
                tier = AchievementTier.Silver,
                isHidden = false,
                requirementType = RequirementType.ItemsCrafted,
                requiredProgress = 50,
                rewardCurrency = 400,
                rewardPoints = 20
            });

            RegisterAchievement(new Achievement
            {
                achievementId = "master_crafter",
                title = "Master Crafter",
                description = "Craft 500 items",
                category = AchievementCategory.Crafting,
                tier = AchievementTier.Platinum,
                isHidden = false,
                requirementType = RequirementType.ItemsCrafted,
                requiredProgress = 500,
                rewardCurrency = 3000,
                rewardPoints = 75,
                rewardCosmetic = "title_master_crafter"
            });

            // Social Achievements
            RegisterAchievement(new Achievement
            {
                achievementId = "friendly",
                title = "Friendly",
                description = "Add 10 friends",
                category = AchievementCategory.Social,
                tier = AchievementTier.Bronze,
                isHidden = false,
                requirementType = RequirementType.FriendsAdded,
                requiredProgress = 10,
                rewardCurrency = 200,
                rewardPoints = 10
            });

            RegisterAchievement(new Achievement
            {
                achievementId = "team_player",
                title = "Team Player",
                description = "Complete 25 missions in a party",
                category = AchievementCategory.Social,
                tier = AchievementTier.Silver,
                isHidden = false,
                requirementType = RequirementType.PartyMissionsCompleted,
                requiredProgress = 25,
                rewardCurrency = 600,
                rewardPoints = 25
            });

            // Progression Achievements
            RegisterAchievement(new Achievement
            {
                achievementId = "level_10",
                title = "Apprentice",
                description = "Reach level 10",
                category = AchievementCategory.Progression,
                tier = AchievementTier.Bronze,
                isHidden = false,
                requirementType = RequirementType.LevelReached,
                requiredProgress = 10,
                rewardCurrency = 250,
                rewardPoints = 15
            });

            RegisterAchievement(new Achievement
            {
                achievementId = "level_50",
                title = "Veteran",
                description = "Reach level 50",
                category = AchievementCategory.Progression,
                tier = AchievementTier.Gold,
                isHidden = false,
                requirementType = RequirementType.LevelReached,
                requiredProgress = 50,
                rewardCurrency = 1500,
                rewardPoints = 50
            });

            RegisterAchievement(new Achievement
            {
                achievementId = "level_100",
                title = "Legend",
                description = "Reach level 100",
                category = AchievementCategory.Progression,
                tier = AchievementTier.Platinum,
                isHidden = false,
                requirementType = RequirementType.LevelReached,
                requiredProgress = 100,
                rewardCurrency = 5000,
                rewardPoints = 100,
                rewardCosmetic = "title_legend"
            });

            // Hidden/Secret Achievements
            RegisterAchievement(new Achievement
            {
                achievementId = "secret_room",
                title = "???",
                description = "???",
                category = AchievementCategory.Secret,
                tier = AchievementTier.Gold,
                isHidden = true,
                requirementType = RequirementType.SpecialCondition,
                requiredProgress = 1,
                rewardCurrency = 2000,
                rewardPoints = 60,
                revealedTitle = "Secret Room",
                revealedDescription = "Find the secret room"
            });

            RegisterAchievement(new Achievement
            {
                achievementId = "easter_egg",
                title = "???",
                description = "???",
                category = AchievementCategory.Secret,
                tier = AchievementTier.Platinum,
                isHidden = true,
                requirementType = RequirementType.SpecialCondition,
                requiredProgress = 1,
                rewardCurrency = 5000,
                rewardPoints = 100,
                revealedTitle = "Easter Egg Hunter",
                revealedDescription = "Find the hidden easter egg"
            });

            // Boss Achievements
            RegisterAchievement(new Achievement
            {
                achievementId = "boss_slayer",
                title = "Boss Slayer",
                description = "Defeat your first boss",
                category = AchievementCategory.Bosses,
                tier = AchievementTier.Silver,
                isHidden = false,
                requirementType = RequirementType.BossesDefeated,
                requiredProgress = 1,
                rewardCurrency = 1000,
                rewardPoints = 30
            });

            RegisterAchievement(new Achievement
            {
                achievementId = "raid_champion",
                title = "Raid Champion",
                description = "Defeat 10 raid bosses",
                category = AchievementCategory.Bosses,
                tier = AchievementTier.Gold,
                isHidden = false,
                requirementType = RequirementType.RaidBossesDefeated,
                requiredProgress = 10,
                rewardCurrency = 3000,
                rewardPoints = 75
            });

            // Collection Achievements
            RegisterAchievement(new Achievement
            {
                achievementId = "collector",
                title = "Collector",
                description = "Collect 100 unique items",
                category = AchievementCategory.Collection,
                tier = AchievementTier.Silver,
                isHidden = false,
                requirementType = RequirementType.UniqueItemsCollected,
                requiredProgress = 100,
                rewardCurrency = 800,
                rewardPoints = 30
            });

            RegisterAchievement(new Achievement
            {
                achievementId = "completionist",
                title = "Completionist",
                description = "Collect all items in the game",
                category = AchievementCategory.Collection,
                tier = AchievementTier.Platinum,
                isHidden = false,
                requirementType = RequirementType.UniqueItemsCollected,
                requiredProgress = 500,
                rewardCurrency = 10000,
                rewardPoints = 150,
                rewardCosmetic = "title_completionist"
            });
        }

        private void RegisterAchievement(Achievement achievement)
        {
            achievementDatabase[achievement.achievementId] = achievement;
        }

        private void InitializeAchievementChains()
        {
            // Define achievement chains (prerequisites)
            achievementChains["kill_chain"] = new List<string> { "first_blood", "slayer", "genocide" };
            achievementChains["headshot_chain"] = new List<string> { "headhunter", "sharpshooter" };
            achievementChains["level_chain"] = new List<string> { "level_10", "level_50", "level_100" };
            achievementChains["economy_chain"] = new List<string> { "entrepreneur", "tycoon" };
            achievementChains["crafting_chain"] = new List<string> { "craftsman", "master_crafter" };
        }

        private void InitializeCategories()
        {
            foreach (AchievementCategory category in Enum.GetValues(typeof(AchievementCategory)))
            {
                categorizedAchievements[category] = new List<string>();
            }

            foreach (var achievement in achievementDatabase.Values)
            {
                categorizedAchievements[achievement.category].Add(achievement.achievementId);
            }
        }

        #region Server RPCs

        [ServerRpc(RequireOwnership = false)]
        public void InitializePlayerAchievementsServerRpc(ulong playerId, ServerRpcParams rpcParams = default)
        {
            if (!playerAchievements.ContainsKey(playerId))
            {
                playerAchievements[playerId] = new PlayerAchievementData
                {
                    playerId = playerId,
                    unlockedAchievements = new List<string>(),
                    achievementProgress = new Dictionary<string, float>(),
                    showcasedAchievements = new List<string>(),
                    featuredAchievements = new List<string>(),
                    totalPoints = 0,
                    unlockTimestamps = new Dictionary<string, DateTime>()
                };
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void UpdateAchievementProgressServerRpc(ulong playerId, RequirementType requirementType, float progress, ServerRpcParams rpcParams = default)
        {
            if (!playerAchievements.ContainsKey(playerId))
            {
                InitializePlayerAchievementsServerRpc(playerId);
            }

            var playerData = playerAchievements[playerId];

            // Find all achievements matching this requirement type
            var relevantAchievements = achievementDatabase.Values
                .Where(a => a.requirementType == requirementType && !playerData.unlockedAchievements.Contains(a.achievementId))
                .ToList();

            foreach (var achievement in relevantAchievements)
            {
                // Update progress
                if (!playerData.achievementProgress.ContainsKey(achievement.achievementId))
                {
                    playerData.achievementProgress[achievement.achievementId] = 0;
                }

                playerData.achievementProgress[achievement.achievementId] = progress;

                OnAchievementProgress?.Invoke(playerId, achievement.achievementId, progress);

                // Check if achievement is complete
                if (progress >= achievement.requiredProgress)
                {
                    UnlockAchievement(playerId, achievement.achievementId);
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void UnlockAchievementServerRpc(ulong playerId, string achievementId, ServerRpcParams rpcParams = default)
        {
            UnlockAchievement(playerId, achievementId);
        }

        private void UnlockAchievement(ulong playerId, string achievementId)
        {
            if (!achievementDatabase.TryGetValue(achievementId, out var achievement))
            {
                Debug.LogWarning($"Achievement not found: {achievementId}");
                return;
            }

            if (!playerAchievements.TryGetValue(playerId, out var playerData))
            {
                InitializePlayerAchievementsServerRpc(playerId);
                playerData = playerAchievements[playerId];
            }

            if (playerData.unlockedAchievements.Contains(achievementId))
            {
                return;
            }

            // Unlock achievement
            playerData.unlockedAchievements.Add(achievementId);
            playerData.unlockTimestamps[achievementId] = DateTime.UtcNow;
            playerData.totalPoints += achievement.rewardPoints;

            // Award rewards
            if (achievement.rewardCurrency > 0)
            {
                Economy.EconomyManager.Instance?.AddSoftCurrency(playerId, achievement.rewardCurrency);
            }

            if (!string.IsNullOrEmpty(achievement.rewardCosmetic))
            {
                // Unlock cosmetic reward
                Customization.AppearanceCustomizationSystem.Instance?.UnlockCosmeticServerRpc(playerId, achievement.rewardCosmetic);
            }

            // Check for chain progression
            CheckAchievementChains(playerId, achievementId);

            // Trigger platform trophy if enabled
            if (enablePlatformTrophies)
            {
                TriggerPlatformTrophy(playerId, achievement);
            }

            OnAchievementUnlocked?.Invoke(playerId, achievementId);
            OnAchievementPointsChanged?.Invoke(playerId, playerData.totalPoints);
            NotifyAchievementUnlockedClientRpc(playerId, achievementId);

            Debug.Log($"Achievement unlocked for player {playerId}: {achievement.title} (+{achievement.rewardPoints} points)");
        }

        private void CheckAchievementChains(ulong playerId, string achievementId)
        {
            foreach (var chain in achievementChains.Values)
            {
                if (!chain.Contains(achievementId))
                    continue;

                int currentIndex = chain.IndexOf(achievementId);
                if (currentIndex < chain.Count - 1)
                {
                    string nextAchievement = chain[currentIndex + 1];
                    // Notify player of next achievement in chain
                    NotifyNextInChainClientRpc(playerId, nextAchievement);
                }
            }
        }

        private void TriggerPlatformTrophy(ulong playerId, Achievement achievement)
        {
            // This would integrate with platform-specific trophy systems
            // Steam Achievements, PlayStation Trophies, Xbox Achievements, etc.
            Debug.Log($"Triggering platform trophy for {achievement.title}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void ShowcaseAchievementServerRpc(ulong playerId, string achievementId, ServerRpcParams rpcParams = default)
        {
            if (!playerAchievements.TryGetValue(playerId, out var playerData))
            {
                return;
            }

            if (!playerData.unlockedAchievements.Contains(achievementId))
            {
                Debug.LogWarning($"Player {playerId} has not unlocked achievement {achievementId}");
                return;
            }

            if (playerData.showcasedAchievements.Count >= maxShowcaseSlots)
            {
                Debug.LogWarning($"Player {playerId} has maximum showcased achievements");
                return;
            }

            if (!playerData.showcasedAchievements.Contains(achievementId))
            {
                playerData.showcasedAchievements.Add(achievementId);
                OnAchievementShowcased?.Invoke(playerId, achievementId);
                NotifyAchievementShowcasedClientRpc(playerId, achievementId);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void RemoveShowcaseAchievementServerRpc(ulong playerId, string achievementId, ServerRpcParams rpcParams = default)
        {
            if (!playerAchievements.TryGetValue(playerId, out var playerData))
            {
                return;
            }

            playerData.showcasedAchievements.Remove(achievementId);
            NotifyShowcaseRemovedClientRpc(playerId, achievementId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SetFeaturedAchievementsServerRpc(ulong playerId, List<string> achievementIds, ServerRpcParams rpcParams = default)
        {
            if (!playerAchievements.TryGetValue(playerId, out var playerData))
            {
                return;
            }

            if (achievementIds.Count > maxFeaturedAchievements)
            {
                achievementIds = achievementIds.Take(maxFeaturedAchievements).ToList();
            }

            playerData.featuredAchievements = achievementIds;
            NotifyFeaturedAchievementsUpdatedClientRpc(playerId, achievementIds.ToArray());
        }

        #endregion

        #region Client RPCs

        [ClientRpc]
        private void NotifyAchievementUnlockedClientRpc(ulong playerId, string achievementId)
        {
            OnAchievementUnlocked?.Invoke(playerId, achievementId);
        }

        [ClientRpc]
        private void NotifyNextInChainClientRpc(ulong playerId, string nextAchievementId)
        {
            // Client-side notification for next achievement in chain
        }

        [ClientRpc]
        private void NotifyAchievementShowcasedClientRpc(ulong playerId, string achievementId)
        {
            OnAchievementShowcased?.Invoke(playerId, achievementId);
        }

        [ClientRpc]
        private void NotifyShowcaseRemovedClientRpc(ulong playerId, string achievementId)
        {
            // Client-side notification
        }

        [ClientRpc]
        private void NotifyFeaturedAchievementsUpdatedClientRpc(ulong playerId, string[] achievementIds)
        {
            // Client-side notification
        }

        #endregion

        #region Public API

        public PlayerAchievementData GetPlayerAchievements(ulong playerId)
        {
            return playerAchievements.GetValueOrDefault(playerId);
        }

        public List<Achievement> GetAllAchievements(bool includeHidden = false)
        {
            var achievements = achievementDatabase.Values.AsEnumerable();

            if (!includeHidden)
            {
                achievements = achievements.Where(a => !a.isHidden);
            }

            return achievements.ToList();
        }

        public List<Achievement> GetAchievementsByCategory(AchievementCategory category, bool includeHidden = false)
        {
            if (!categorizedAchievements.ContainsKey(category))
                return new List<Achievement>();

            var achievementIds = categorizedAchievements[category];
            var achievements = achievementIds
                .Select(id => achievementDatabase.GetValueOrDefault(id))
                .Where(a => a != null);

            if (!includeHidden)
            {
                achievements = achievements.Where(a => !a.isHidden);
            }

            return achievements.ToList();
        }

        public Achievement GetAchievement(string achievementId)
        {
            return achievementDatabase.GetValueOrDefault(achievementId);
        }

        public float GetCompletionPercentage(ulong playerId)
        {
            if (!playerAchievements.TryGetValue(playerId, out var playerData))
                return 0f;

            int totalAchievements = achievementDatabase.Count;
            int unlockedAchievements = playerData.unlockedAchievements.Count;

            return (float)unlockedAchievements / totalAchievements * 100f;
        }

        public Dictionary<AchievementCategory, float> GetCategoryCompletion(ulong playerId)
        {
            var completion = new Dictionary<AchievementCategory, float>();

            foreach (var category in categorizedAchievements.Keys)
            {
                int totalInCategory = categorizedAchievements[category].Count;
                int unlockedInCategory = 0;

                if (playerAchievements.TryGetValue(playerId, out var playerData))
                {
                    unlockedInCategory = categorizedAchievements[category]
                        .Count(id => playerData.unlockedAchievements.Contains(id));
                }

                completion[category] = totalInCategory > 0 ? (float)unlockedInCategory / totalInCategory * 100f : 0f;
            }

            return completion;
        }

        public List<Achievement> GetRecentlyUnlocked(ulong playerId, int count = 5)
        {
            if (!playerAchievements.TryGetValue(playerId, out var playerData))
                return new List<Achievement>();

            return playerData.unlockTimestamps
                .OrderByDescending(kvp => kvp.Value)
                .Take(count)
                .Select(kvp => achievementDatabase.GetValueOrDefault(kvp.Key))
                .Where(a => a != null)
                .ToList();
        }

        public List<Achievement> GetRarestAchievements(int count = 10)
        {
            // Calculate rarity based on unlock percentage
            var achievementUnlockCounts = new Dictionary<string, int>();

            foreach (var playerData in playerAchievements.Values)
            {
                foreach (var achievementId in playerData.unlockedAchievements)
                {
                    achievementUnlockCounts[achievementId] = achievementUnlockCounts.GetValueOrDefault(achievementId) + 1;
                }
            }

            int totalPlayers = playerAchievements.Count;

            return achievementUnlockCounts
                .OrderBy(kvp => kvp.Value)
                .Take(count)
                .Select(kvp => achievementDatabase.GetValueOrDefault(kvp.Key))
                .Where(a => a != null)
                .ToList();
        }

        #endregion
    }

    #region Data Structures

    [Serializable]
    public class Achievement
    {
        public string achievementId;
        public string title;
        public string description;
        public AchievementCategory category;
        public AchievementTier tier;
        public bool isHidden;
        public RequirementType requirementType;
        public float requiredProgress;
        public int rewardCurrency;
        public int rewardPoints;
        public string rewardCosmetic;
        public string revealedTitle;
        public string revealedDescription;
    }

    [Serializable]
    public class PlayerAchievementData
    {
        public ulong playerId;
        public List<string> unlockedAchievements;
        public Dictionary<string, float> achievementProgress;
        public List<string> showcasedAchievements;
        public List<string> featuredAchievements;
        public int totalPoints;
        public Dictionary<string, DateTime> unlockTimestamps;
    }

    public enum AchievementCategory
    {
        Combat,
        Survival,
        Exploration,
        Economy,
        Crafting,
        Social,
        Progression,
        Bosses,
        Collection,
        Secret
    }

    public enum AchievementTier
    {
        Bronze,
        Silver,
        Gold,
        Platinum,
        Diamond
    }

    public enum RequirementType
    {
        KillCount,
        HeadshotKills,
        SurvivalTime,
        SoloSurvivalTime,
        LocationsDiscovered,
        CurrencyEarned,
        ItemsCrafted,
        FriendsAdded,
        PartyMissionsCompleted,
        LevelReached,
        BossesDefeated,
        RaidBossesDefeated,
        UniqueItemsCollected,
        SpecialCondition
    }

    #endregion
}
