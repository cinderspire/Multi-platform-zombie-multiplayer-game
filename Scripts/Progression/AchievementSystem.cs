using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;
using System;

namespace DeadFrontier.Progression
{
    /// <summary>
    /// Comprehensive achievement and trophy system.
    /// Tracks player accomplishments, awards rewards, supports platform achievements.
    /// Includes categories, tiers, secret achievements, and progression tracking.
    /// </summary>
    public class AchievementSystem : NetworkBehaviour
    {
        public static AchievementSystem Instance { get; private set; }

        [Header("Achievement Settings")]
        [SerializeField] private bool enablePlatformAchievements = true;
        [SerializeField] private bool showSecretAchievements = false;
        [SerializeField] private float notificationDuration = 5f;

        [Header("Showcase Settings")]
        [SerializeField] private int maxShowcaseSlots = 5;

        // Achievement registry
        private Dictionary<string, AchievementDefinition> achievements = new Dictionary<string, AchievementDefinition>();

        // Player achievement data
        private Dictionary<ulong, PlayerAchievementData> playerData = new Dictionary<ulong, PlayerAchievementData>();

        // Achievement tracking
        private Dictionary<string, List<ulong>> achievementUnlockers = new Dictionary<string, List<ulong>>();

        // Events
        public event Action<ulong, string> OnAchievementUnlocked;
        public event Action<ulong, string, float> OnAchievementProgress;
        public event Action<ulong, AchievementTier> OnTierCompleted;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeAchievements();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        #region Initialization

        private void InitializeAchievements()
        {
            // Combat Achievements
            RegisterAchievement(new AchievementDefinition
            {
                id = "first_blood",
                title = "First Blood",
                description = "Get your first kill",
                category = AchievementCategory.Combat,
                tier = AchievementTier.Bronze,
                isSecret = false,
                requirement = new AchievementRequirement { type = RequirementType.KillCount, targetValue = 1 },
                rewards = new AchievementReward { softCurrency = 100, xp = 50 }
            });

            RegisterAchievement(new AchievementDefinition
            {
                id = "zombie_slayer",
                title = "Zombie Slayer",
                description = "Kill 100 zombies",
                category = AchievementCategory.Combat,
                tier = AchievementTier.Silver,
                requirement = new AchievementRequirement { type = RequirementType.ZombieKills, targetValue = 100 },
                rewards = new AchievementReward { softCurrency = 500, xp = 250, itemRewards = new List<string> { "weapon_skin_zombie_hunter" } }
            });

            RegisterAchievement(new AchievementDefinition
            {
                id = "headshot_master",
                title = "Headshot Master",
                description = "Get 50 headshot kills",
                category = AchievementCategory.Combat,
                tier = AchievementTier.Gold,
                requirement = new AchievementRequirement { type = RequirementType.HeadshotKills, targetValue = 50 },
                rewards = new AchievementReward { softCurrency = 1000, hardCurrency = 50, xp = 500 }
            });

            RegisterAchievement(new AchievementDefinition
            {
                id = "genocide",
                title = "Genocide",
                description = "Kill 1000 enemies",
                category = AchievementCategory.Combat,
                tier = AchievementTier.Platinum,
                requirement = new AchievementRequirement { type = RequirementType.KillCount, targetValue = 1000 },
                rewards = new AchievementReward { softCurrency = 5000, hardCurrency = 200, xp = 2000, itemRewards = new List<string> { "title_genocide", "weapon_skin_legendary" } }
            });

            // Survival Achievements
            RegisterAchievement(new AchievementDefinition
            {
                id = "survivor",
                title = "Survivor",
                description = "Survive your first raid",
                category = AchievementCategory.Survival,
                tier = AchievementTier.Bronze,
                requirement = new AchievementRequirement { type = RequirementType.RaidsSurvived, targetValue = 1 },
                rewards = new AchievementReward { softCurrency = 200, xp = 100 }
            });

            RegisterAchievement(new AchievementDefinition
            {
                id = "escape_artist",
                title = "Escape Artist",
                description = "Successfully extract 25 times",
                category = AchievementCategory.Survival,
                tier = AchievementTier.Gold,
                requirement = new AchievementRequirement { type = RequirementType.Extractions, targetValue = 25 },
                rewards = new AchievementReward { softCurrency = 1500, hardCurrency = 75, xp = 750 }
            });

            RegisterAchievement(new AchievementDefinition
            {
                id = "near_death",
                title = "Near Death Experience",
                description = "Survive with less than 5% health",
                category = AchievementCategory.Survival,
                tier = AchievementTier.Silver,
                isSecret = true,
                requirement = new AchievementRequirement { type = RequirementType.SurviveLowHealth, targetValue = 1 },
                rewards = new AchievementReward { softCurrency = 750, xp = 300 }
            });

            // Wealth Achievements
            RegisterAchievement(new AchievementDefinition
            {
                id = "first_fortune",
                title = "First Fortune",
                description = "Earn 10,000 credits",
                category = AchievementCategory.Wealth,
                tier = AchievementTier.Bronze,
                requirement = new AchievementRequirement { type = RequirementType.CurrencyEarned, targetValue = 10000 },
                rewards = new AchievementReward { softCurrency = 500, xp = 200 }
            });

            RegisterAchievement(new AchievementDefinition
            {
                id = "millionaire",
                title = "Millionaire",
                description = "Accumulate 1,000,000 credits",
                category = AchievementCategory.Wealth,
                tier = AchievementTier.Platinum,
                requirement = new AchievementRequirement { type = RequirementType.CurrencyTotal, targetValue = 1000000 },
                rewards = new AchievementReward { softCurrency = 10000, hardCurrency = 500, xp = 5000, itemRewards = new List<string> { "title_millionaire", "cosmetic_gold_skin" } }
            });

            // Social Achievements
            RegisterAchievement(new AchievementDefinition
            {
                id = "social_butterfly",
                title = "Social Butterfly",
                description = "Add 10 friends",
                category = AchievementCategory.Social,
                tier = AchievementTier.Bronze,
                requirement = new AchievementRequirement { type = RequirementType.FriendCount, targetValue = 10 },
                rewards = new AchievementReward { softCurrency = 300, xp = 150 }
            });

            RegisterAchievement(new AchievementDefinition
            {
                id = "clan_founder",
                title = "Clan Founder",
                description = "Create a clan",
                category = AchievementCategory.Social,
                tier = AchievementTier.Silver,
                requirement = new AchievementRequirement { type = RequirementType.CreateClan, targetValue = 1 },
                rewards = new AchievementReward { softCurrency = 1000, xp = 500 }
            });

            RegisterAchievement(new AchievementDefinition
            {
                id = "team_player",
                title = "Team Player",
                description = "Win 50 matches with a party",
                category = AchievementCategory.Social,
                tier = AchievementTier.Gold,
                requirement = new AchievementRequirement { type = RequirementType.PartyWins, targetValue = 50 },
                rewards = new AchievementReward { softCurrency = 2000, hardCurrency = 100, xp = 1000 }
            });

            // Exploration Achievements
            RegisterAchievement(new AchievementDefinition
            {
                id = "explorer",
                title = "Explorer",
                description = "Discover all map locations",
                category = AchievementCategory.Exploration,
                tier = AchievementTier.Gold,
                requirement = new AchievementRequirement { type = RequirementType.LocationsDiscovered, targetValue = 100 },
                rewards = new AchievementReward { softCurrency = 1500, hardCurrency = 75, xp = 750 }
            });

            RegisterAchievement(new AchievementDefinition
            {
                id = "treasure_hunter",
                title = "Treasure Hunter",
                description = "Find 100 rare loot items",
                category = AchievementCategory.Exploration,
                tier = AchievementTier.Silver,
                requirement = new AchievementRequirement { type = RequirementType.RareLootFound, targetValue = 100 },
                rewards = new AchievementReward { softCurrency = 1000, xp = 500 }
            });

            // Skill Achievements
            RegisterAchievement(new AchievementDefinition
            {
                id = "marksman",
                title = "Marksman",
                description = "Achieve 80% accuracy in a match",
                category = AchievementCategory.Skill,
                tier = AchievementTier.Gold,
                requirement = new AchievementRequirement { type = RequirementType.Accuracy, targetValue = 80 },
                rewards = new AchievementReward { softCurrency = 1500, hardCurrency = 75, xp = 750 }
            });

            RegisterAchievement(new AchievementDefinition
            {
                id = "unstoppable",
                title = "Unstoppable",
                description = "Get a 10 kill streak",
                category = AchievementCategory.Skill,
                tier = AchievementTier.Platinum,
                isSecret = true,
                requirement = new AchievementRequirement { type = RequirementType.KillStreak, targetValue = 10 },
                rewards = new AchievementReward { softCurrency = 3000, hardCurrency = 150, xp = 1500, itemRewards = new List<string> { "title_unstoppable" } }
            });

            // Collection Achievements
            RegisterAchievement(new AchievementDefinition
            {
                id = "weapon_collector",
                title = "Weapon Collector",
                description = "Unlock 25 different weapons",
                category = AchievementCategory.Collection,
                tier = AchievementTier.Silver,
                requirement = new AchievementRequirement { type = RequirementType.WeaponsUnlocked, targetValue = 25 },
                rewards = new AchievementReward { softCurrency = 1000, xp = 500 }
            });

            RegisterAchievement(new AchievementDefinition
            {
                id = "fashionista",
                title = "Fashionista",
                description = "Unlock 50 cosmetic items",
                category = AchievementCategory.Collection,
                tier = AchievementTier.Gold,
                requirement = new AchievementRequirement { type = RequirementType.CosmeticsUnlocked, targetValue = 50 },
                rewards = new AchievementReward { softCurrency = 2000, hardCurrency = 100, xp = 1000 }
            });

            // Competitive Achievements
            RegisterAchievement(new AchievementDefinition
            {
                id = "ranked_warrior",
                title = "Ranked Warrior",
                description = "Reach Gold rank",
                category = AchievementCategory.Competitive,
                tier = AchievementTier.Silver,
                requirement = new AchievementRequirement { type = RequirementType.RankTier, targetValue = 3 }, // Gold = 3
                rewards = new AchievementReward { softCurrency = 1500, hardCurrency = 75, xp = 750 }
            });

            RegisterAchievement(new AchievementDefinition
            {
                id = "legend",
                title = "Legend",
                description = "Reach Legendary rank",
                category = AchievementCategory.Competitive,
                tier = AchievementTier.Platinum,
                requirement = new AchievementRequirement { type = RequirementType.RankTier, targetValue = 6 }, // Legendary = 6
                rewards = new AchievementReward { softCurrency = 10000, hardCurrency = 500, xp = 5000, itemRewards = new List<string> { "title_legend", "banner_legendary" } }
            });

            RegisterAchievement(new AchievementDefinition
            {
                id = "tournament_champion",
                title = "Tournament Champion",
                description = "Win a tournament",
                category = AchievementCategory.Competitive,
                tier = AchievementTier.Platinum,
                requirement = new AchievementRequirement { type = RequirementType.TournamentWins, targetValue = 1 },
                rewards = new AchievementReward { softCurrency = 5000, hardCurrency = 250, xp = 2500, itemRewards = new List<string> { "title_champion", "trophy_gold" } }
            });

            // Special/Secret Achievements
            RegisterAchievement(new AchievementDefinition
            {
                id = "betrayal",
                title = "Betrayal",
                description = "Kill a teammate",
                category = AchievementCategory.Special,
                tier = AchievementTier.Bronze,
                isSecret = true,
                requirement = new AchievementRequirement { type = RequirementType.TeamKills, targetValue = 1 },
                rewards = new AchievementReward { softCurrency = 0, xp = 0, itemRewards = new List<string> { "title_traitor" } }
            });

            RegisterAchievement(new AchievementDefinition
            {
                id = "pacifist",
                title = "Pacifist",
                description = "Complete a raid without killing anyone",
                category = AchievementCategory.Special,
                tier = AchievementTier.Gold,
                isSecret = true,
                requirement = new AchievementRequirement { type = RequirementType.PacifistRaid, targetValue = 1 },
                rewards = new AchievementReward { softCurrency = 2000, hardCurrency = 100, xp = 1000, itemRewards = new List<string> { "title_pacifist" } }
            });

            RegisterAchievement(new AchievementDefinition
            {
                id = "completionist",
                title = "Completionist",
                description = "Unlock all other achievements",
                category = AchievementCategory.Special,
                tier = AchievementTier.Platinum,
                requirement = new AchievementRequirement { type = RequirementType.AchievementCount, targetValue = achievements.Count - 1 },
                rewards = new AchievementReward { softCurrency = 50000, hardCurrency = 2000, xp = 10000, itemRewards = new List<string> { "title_completionist", "cosmetic_platinum_trophy" } }
            });

            Debug.Log($"[AchievementSystem] Initialized {achievements.Count} achievements");
        }

        private void RegisterAchievement(AchievementDefinition achievement)
        {
            achievements[achievement.id] = achievement;
            achievementUnlockers[achievement.id] = new List<ulong>();
        }

        #endregion

        #region Player Data Management

        public void InitializePlayerData(ulong playerId)
        {
            if (playerData.ContainsKey(playerId)) return;

            playerData[playerId] = new PlayerAchievementData
            {
                playerId = playerId,
                unlockedAchievements = new List<string>(),
                achievementProgress = new Dictionary<string, float>(),
                showcasedAchievements = new List<string>(),
                totalScore = 0
            };

            LoadPlayerData(playerId);
        }

        #endregion

        #region Achievement Tracking

        public void TrackProgress(ulong playerId, RequirementType type, float value)
        {
            if (!playerData.ContainsKey(playerId))
            {
                InitializePlayerData(playerId);
            }

            // Find all achievements that match this requirement type
            var relevantAchievements = achievements.Values.Where(a => a.requirement.type == type && !IsAchievementUnlocked(playerId, a.id));

            foreach (var achievement in relevantAchievements)
            {
                UpdateAchievementProgress(playerId, achievement.id, value);
            }
        }

        private void UpdateAchievementProgress(ulong playerId, string achievementId, float currentValue)
        {
            var data = playerData[playerId];
            var achievement = achievements[achievementId];

            if (IsAchievementUnlocked(playerId, achievementId)) return;

            // Update progress
            data.achievementProgress[achievementId] = currentValue;

            // Calculate progress percentage
            float progress = Mathf.Clamp01(currentValue / achievement.requirement.targetValue);

            OnAchievementProgress?.Invoke(playerId, achievementId, progress);

            // Check if unlocked
            if (currentValue >= achievement.requirement.targetValue)
            {
                UnlockAchievement(playerId, achievementId);
            }
        }

        public void IncrementProgress(ulong playerId, RequirementType type, float incrementValue = 1f)
        {
            if (!playerData.ContainsKey(playerId))
            {
                InitializePlayerData(playerId);
            }

            var data = playerData[playerId];

            // Find relevant achievements
            var relevantAchievements = achievements.Values.Where(a => a.requirement.type == type && !IsAchievementUnlocked(playerId, a.id));

            foreach (var achievement in relevantAchievements)
            {
                float currentProgress = data.achievementProgress.ContainsKey(achievement.id) ? data.achievementProgress[achievement.id] : 0f;
                UpdateAchievementProgress(playerId, achievement.id, currentProgress + incrementValue);
            }
        }

        #endregion

        #region Achievement Unlocking

        public void UnlockAchievement(ulong playerId, string achievementId)
        {
            if (!achievements.ContainsKey(achievementId))
            {
                Debug.LogWarning($"[AchievementSystem] Achievement {achievementId} not found");
                return;
            }

            if (IsAchievementUnlocked(playerId, achievementId))
            {
                Debug.LogWarning($"[AchievementSystem] Achievement {achievementId} already unlocked for player {playerId}");
                return;
            }

            var data = playerData[playerId];
            var achievement = achievements[achievementId];

            // Add to unlocked list
            data.unlockedAchievements.Add(achievementId);
            achievementUnlockers[achievementId].Add(playerId);

            // Award rewards
            GrantAchievementRewards(playerId, achievement);

            // Update total score
            data.totalScore += GetAchievementScore(achievement.tier);

            // Check tier completion
            CheckTierCompletion(playerId, achievement.tier);

            // Platform achievement unlock
            if (enablePlatformAchievements)
            {
                UnlockPlatformAchievement(achievementId);
            }

            // Save data
            SavePlayerData(playerId);

            OnAchievementUnlocked?.Invoke(playerId, achievementId);

            Debug.Log($"[AchievementSystem] Player {playerId} unlocked achievement: {achievement.title}");

            // Notify client
            UnlockAchievementClientRpc(playerId, achievementId, achievement.title, achievement.tier);
        }

        [ClientRpc]
        private void UnlockAchievementClientRpc(ulong playerId, string achievementId, string title, AchievementTier tier)
        {
            // Show achievement notification
            Debug.Log($"[AchievementSystem] ACHIEVEMENT UNLOCKED: {title} ({tier})");
        }

        private void GrantAchievementRewards(ulong playerId, AchievementDefinition achievement)
        {
            var reward = achievement.rewards;

            // Currency rewards
            if (reward.softCurrency > 0)
            {
                Economy.EconomyManager.Instance?.AddSoftCurrency(playerId, reward.softCurrency);
            }

            if (reward.hardCurrency > 0)
            {
                Economy.EconomyManager.Instance?.AddHardCurrency(playerId, reward.hardCurrency);
            }

            // XP reward
            if (reward.xp > 0)
            {
                Progression.ProgressionManager.Instance?.AddExperience(playerId, reward.xp);
            }

            // Item rewards
            foreach (var itemId in reward.itemRewards)
            {
                Inventory.InventoryManager.Instance?.AddItem(playerId, itemId, 1);
            }
        }

        #endregion

        #region Achievement Queries

        public bool IsAchievementUnlocked(ulong playerId, string achievementId)
        {
            if (!playerData.ContainsKey(playerId)) return false;
            return playerData[playerId].unlockedAchievements.Contains(achievementId);
        }

        public float GetAchievementProgress(ulong playerId, string achievementId)
        {
            if (!playerData.ContainsKey(playerId)) return 0f;

            var data = playerData[playerId];
            if (!data.achievementProgress.ContainsKey(achievementId)) return 0f;

            var achievement = achievements[achievementId];
            return Mathf.Clamp01(data.achievementProgress[achievementId] / achievement.requirement.targetValue);
        }

        public List<AchievementDefinition> GetAllAchievements(bool includeSecret = false)
        {
            if (includeSecret || showSecretAchievements)
            {
                return achievements.Values.ToList();
            }
            else
            {
                return achievements.Values.Where(a => !a.isSecret).ToList();
            }
        }

        public List<AchievementDefinition> GetAchievementsByCategory(AchievementCategory category, bool includeSecret = false)
        {
            var filtered = achievements.Values.Where(a => a.category == category);

            if (!includeSecret && !showSecretAchievements)
            {
                filtered = filtered.Where(a => !a.isSecret);
            }

            return filtered.ToList();
        }

        public List<AchievementDefinition> GetUnlockedAchievements(ulong playerId)
        {
            if (!playerData.ContainsKey(playerId)) return new List<AchievementDefinition>();

            return playerData[playerId].unlockedAchievements
                .Select(id => achievements[id])
                .ToList();
        }

        public List<AchievementDefinition> GetLockedAchievements(ulong playerId, bool includeSecret = false)
        {
            if (!playerData.ContainsKey(playerId)) return GetAllAchievements(includeSecret);

            var unlocked = playerData[playerId].unlockedAchievements;

            var locked = achievements.Values.Where(a => !unlocked.Contains(a.id));

            if (!includeSecret && !showSecretAchievements)
            {
                locked = locked.Where(a => !a.isSecret);
            }

            return locked.ToList();
        }

        public int GetTotalAchievementScore(ulong playerId)
        {
            if (!playerData.ContainsKey(playerId)) return 0;
            return playerData[playerId].totalScore;
        }

        private int GetAchievementScore(AchievementTier tier)
        {
            switch (tier)
            {
                case AchievementTier.Bronze: return 10;
                case AchievementTier.Silver: return 25;
                case AchievementTier.Gold: return 50;
                case AchievementTier.Platinum: return 100;
                default: return 0;
            }
        }

        #endregion

        #region Showcase System

        public bool AddToShowcase(ulong playerId, string achievementId)
        {
            if (!IsAchievementUnlocked(playerId, achievementId))
            {
                Debug.LogWarning($"[AchievementSystem] Cannot showcase locked achievement {achievementId}");
                return false;
            }

            var data = playerData[playerId];

            if (data.showcasedAchievements.Count >= maxShowcaseSlots)
            {
                Debug.LogWarning($"[AchievementSystem] Showcase full for player {playerId}");
                return false;
            }

            if (data.showcasedAchievements.Contains(achievementId))
            {
                Debug.LogWarning($"[AchievementSystem] Achievement {achievementId} already in showcase");
                return false;
            }

            data.showcasedAchievements.Add(achievementId);
            SavePlayerData(playerId);

            return true;
        }

        public bool RemoveFromShowcase(ulong playerId, string achievementId)
        {
            if (!playerData.ContainsKey(playerId)) return false;

            var data = playerData[playerId];
            bool removed = data.showcasedAchievements.Remove(achievementId);

            if (removed)
            {
                SavePlayerData(playerId);
            }

            return removed;
        }

        public List<AchievementDefinition> GetShowcasedAchievements(ulong playerId)
        {
            if (!playerData.ContainsKey(playerId)) return new List<AchievementDefinition>();

            return playerData[playerId].showcasedAchievements
                .Select(id => achievements[id])
                .ToList();
        }

        #endregion

        #region Tier Completion

        private void CheckTierCompletion(ulong playerId, AchievementTier tier)
        {
            var tierAchievements = achievements.Values.Where(a => a.tier == tier);
            var unlockedTierAchievements = tierAchievements.Where(a => IsAchievementUnlocked(playerId, a.id));

            if (tierAchievements.Count() == unlockedTierAchievements.Count())
            {
                OnTierCompleted?.Invoke(playerId, tier);
                Debug.Log($"[AchievementSystem] Player {playerId} completed {tier} tier!");
            }
        }

        public bool IsTierCompleted(ulong playerId, AchievementTier tier)
        {
            var tierAchievements = achievements.Values.Where(a => a.tier == tier);
            return tierAchievements.All(a => IsAchievementUnlocked(playerId, a.id));
        }

        #endregion

        #region Platform Integration

        private void UnlockPlatformAchievement(string achievementId)
        {
            // Platform-specific achievement unlocking
            // Steam, PlayStation, Xbox integration would go here

#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX
            // Steam achievement unlock
            Debug.Log($"[AchievementSystem] Unlocking Steam achievement: {achievementId}");
#elif UNITY_PS4 || UNITY_PS5
            // PlayStation trophy unlock
            Debug.Log($"[AchievementSystem] Unlocking PlayStation trophy: {achievementId}");
#elif UNITY_XBOXONE || UNITY_GAMECORE
            // Xbox achievement unlock
            Debug.Log($"[AchievementSystem] Unlocking Xbox achievement: {achievementId}");
#endif
        }

        #endregion

        #region Statistics

        public AchievementStatistics GetPlayerStatistics(ulong playerId)
        {
            if (!playerData.ContainsKey(playerId))
            {
                return new AchievementStatistics();
            }

            var data = playerData[playerId];

            return new AchievementStatistics
            {
                totalAchievements = achievements.Count,
                unlockedAchievements = data.unlockedAchievements.Count,
                completionPercentage = (float)data.unlockedAchievements.Count / achievements.Count * 100f,
                totalScore = data.totalScore,
                bronzeUnlocked = data.unlockedAchievements.Count(id => achievements[id].tier == AchievementTier.Bronze),
                silverUnlocked = data.unlockedAchievements.Count(id => achievements[id].tier == AchievementTier.Silver),
                goldUnlocked = data.unlockedAchievements.Count(id => achievements[id].tier == AchievementTier.Gold),
                platinumUnlocked = data.unlockedAchievements.Count(id => achievements[id].tier == AchievementTier.Platinum)
            };
        }

        public List<ulong> GetRarestAchievementUnlockers(string achievementId)
        {
            if (!achievementUnlockers.ContainsKey(achievementId)) return new List<ulong>();
            return new List<ulong>(achievementUnlockers[achievementId]);
        }

        public float GetAchievementRarity(string achievementId)
        {
            if (!achievementUnlockers.ContainsKey(achievementId)) return 0f;

            int totalPlayers = playerData.Count;
            if (totalPlayers == 0) return 0f;

            int unlockers = achievementUnlockers[achievementId].Count;
            return (float)unlockers / totalPlayers * 100f;
        }

        #endregion

        #region Data Persistence

        private void SavePlayerData(ulong playerId)
        {
            if (!playerData.ContainsKey(playerId)) return;

            var data = playerData[playerId];
            string json = JsonUtility.ToJson(data);

            SaveSystem.SaveManager.Instance?.SaveData($"achievements_{playerId}", json);
        }

        public void LoadPlayerData(ulong playerId)
        {
            string json = SaveSystem.SaveManager.Instance?.LoadData($"achievements_{playerId}");

            if (!string.IsNullOrEmpty(json))
            {
                var data = JsonUtility.FromJson<PlayerAchievementData>(json);
                playerData[playerId] = data;

                Debug.Log($"[AchievementSystem] Loaded achievement data for player {playerId}");
            }
        }

        #endregion
    }

    #region Data Classes

    [Serializable]
    public class AchievementDefinition
    {
        public string id;
        public string title;
        public string description;
        public AchievementCategory category;
        public AchievementTier tier;
        public bool isSecret;
        public AchievementRequirement requirement;
        public AchievementReward rewards;
        public string iconPath; // For UI
    }

    [Serializable]
    public class AchievementRequirement
    {
        public RequirementType type;
        public float targetValue;
    }

    [Serializable]
    public class AchievementReward
    {
        public int softCurrency;
        public int hardCurrency;
        public int xp;
        public List<string> itemRewards = new List<string>();
    }

    [Serializable]
    public class PlayerAchievementData
    {
        public ulong playerId;
        public List<string> unlockedAchievements = new List<string>();
        public Dictionary<string, float> achievementProgress = new Dictionary<string, float>();
        public List<string> showcasedAchievements = new List<string>();
        public int totalScore;
    }

    [Serializable]
    public class AchievementStatistics
    {
        public int totalAchievements;
        public int unlockedAchievements;
        public float completionPercentage;
        public int totalScore;
        public int bronzeUnlocked;
        public int silverUnlocked;
        public int goldUnlocked;
        public int platinumUnlocked;
    }

    public enum AchievementCategory
    {
        Combat,
        Survival,
        Wealth,
        Social,
        Exploration,
        Skill,
        Collection,
        Competitive,
        Special
    }

    public enum AchievementTier
    {
        Bronze,
        Silver,
        Gold,
        Platinum
    }

    public enum RequirementType
    {
        KillCount,
        ZombieKills,
        HeadshotKills,
        TeamKills,
        RaidsSurvived,
        Extractions,
        SurviveLowHealth,
        PacifistRaid,
        CurrencyEarned,
        CurrencyTotal,
        FriendCount,
        CreateClan,
        PartyWins,
        LocationsDiscovered,
        RareLootFound,
        Accuracy,
        KillStreak,
        WeaponsUnlocked,
        CosmeticsUnlocked,
        RankTier,
        TournamentWins,
        AchievementCount
    }

    #endregion
}
