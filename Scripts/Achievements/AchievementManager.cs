using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace DeadFrontier.Achievements
{
    /// <summary>
    /// Manages achievement tracking and unlocking
    /// </summary>
    public class AchievementManager : Core.Singleton<AchievementManager>
    {
        [Header("Achievement Database")]
        [SerializeField] private AchievementData[] allAchievements;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        // Progress tracking
        private Dictionary<string, AchievementProgress> achievementProgress = new Dictionary<string, AchievementProgress>();
        private HashSet<string> unlockedAchievements = new HashSet<string>();

        // Events
        public event System.Action<AchievementData, int> OnAchievementProgress;
        public event System.Action<AchievementData> OnAchievementUnlocked;

        // Properties
        public int TotalAchievements => allAchievements?.Length ?? 0;
        public int UnlockedCount => unlockedAchievements.Count;
        public float CompletionPercentage => TotalAchievements > 0 ? (float)UnlockedCount / TotalAchievements : 0f;

        protected override void Awake()
        {
            base.Awake();

            InitializeAchievements();
            LoadProgress();
        }

        #region Initialization

        private void InitializeAchievements()
        {
            if (allAchievements == null || allAchievements.Length == 0)
            {
                Debug.LogWarning("[AchievementManager] No achievements configured!");
                return;
            }

            // Initialize progress for all achievements
            foreach (var achievement in allAchievements)
            {
                if (achievement == null)
                    continue;

                if (!achievementProgress.ContainsKey(achievement.achievementID))
                {
                    achievementProgress[achievement.achievementID] = new AchievementProgress
                    {
                        achievementID = achievement.achievementID,
                        currentProgress = 0,
                        isUnlocked = false,
                        unlockTime = 0
                    };
                }
            }

            if (showDebugLogs)
                Debug.Log($"[AchievementManager] Initialized {allAchievements.Length} achievements");
        }

        #endregion

        #region Progress Tracking

        /// <summary>
        /// Updates progress for an achievement
        /// </summary>
        public void UpdateProgress(string achievementID, int amount = 1)
        {
            AchievementData achievement = GetAchievementByID(achievementID);

            if (achievement == null)
            {
                Debug.LogWarning($"[AchievementManager] Achievement not found: {achievementID}");
                return;
            }

            // Check if already unlocked
            if (IsUnlocked(achievementID))
                return;

            // Get or create progress
            if (!achievementProgress.ContainsKey(achievementID))
            {
                achievementProgress[achievementID] = new AchievementProgress
                {
                    achievementID = achievementID,
                    currentProgress = 0,
                    isUnlocked = false
                };
            }

            AchievementProgress progress = achievementProgress[achievementID];

            // Update progress
            progress.currentProgress += amount;
            progress.currentProgress = Mathf.Clamp(progress.currentProgress, 0, achievement.targetProgress);

            if (showDebugLogs)
                Debug.Log($"[AchievementManager] {achievement.achievementName} progress: {progress.currentProgress}/{achievement.targetProgress}");

            OnAchievementProgress?.Invoke(achievement, progress.currentProgress);

            // Check for unlock
            if (progress.currentProgress >= achievement.targetProgress)
            {
                UnlockAchievement(achievementID);
            }

            SaveProgress();
        }

        /// <summary>
        /// Sets progress for an achievement (absolute value)
        /// </summary>
        public void SetProgress(string achievementID, int progress)
        {
            AchievementData achievement = GetAchievementByID(achievementID);

            if (achievement == null)
                return;

            if (!achievementProgress.ContainsKey(achievementID))
            {
                achievementProgress[achievementID] = new AchievementProgress
                {
                    achievementID = achievementID,
                    currentProgress = 0,
                    isUnlocked = false
                };
            }

            achievementProgress[achievementID].currentProgress = Mathf.Clamp(progress, 0, achievement.targetProgress);

            if (achievementProgress[achievementID].currentProgress >= achievement.targetProgress)
            {
                UnlockAchievement(achievementID);
            }

            SaveProgress();
        }

        /// <summary>
        /// Unlocks an achievement
        /// </summary>
        public void UnlockAchievement(string achievementID)
        {
            AchievementData achievement = GetAchievementByID(achievementID);

            if (achievement == null)
                return;

            // Check if already unlocked
            if (IsUnlocked(achievementID))
                return;

            // Check prerequisites
            if (achievement.prerequisiteAchievements != null && achievement.prerequisiteAchievements.Length > 0)
            {
                foreach (var prereqID in achievement.prerequisiteAchievements)
                {
                    if (!IsUnlocked(prereqID))
                    {
                        if (showDebugLogs)
                            Debug.LogWarning($"[AchievementManager] Prerequisites not met for {achievement.achievementName}");
                        return;
                    }
                }
            }

            // Mark as unlocked
            unlockedAchievements.Add(achievementID);

            if (achievementProgress.ContainsKey(achievementID))
            {
                achievementProgress[achievementID].isUnlocked = true;
                achievementProgress[achievementID].unlockTime = System.DateTime.Now.Ticks;
            }

            if (showDebugLogs)
                Debug.Log($"[AchievementManager] ACHIEVEMENT UNLOCKED: {achievement.achievementName}");

            // Award rewards
            AwardAchievementRewards(achievement);

            // Notify
            OnAchievementUnlocked?.Invoke(achievement);

            // Show notification
            ShowAchievementUnlockedNotification(achievement);

            SaveProgress();
        }

        #endregion

        #region Rewards

        private void AwardAchievementRewards(AchievementData achievement)
        {
            // Award XP
            if (achievement.GetTotalXPReward() > 0)
            {
                var progression = FindObjectOfType<Player.PlayerProgression>();
                if (progression != null)
                {
                    progression.AwardXP(achievement.GetTotalXPReward(), $"Achievement: {achievement.achievementName}");
                }
            }

            // Award currency
            if (achievement.GetTotalCurrencyReward() > 0)
            {
                // TODO: Award currency to player
                if (showDebugLogs)
                    Debug.Log($"[AchievementManager] Awarded {achievement.GetTotalCurrencyReward()} currency");
            }

            // Award items
            if (achievement.itemRewards != null && achievement.itemRewards.Length > 0)
            {
                var progression = FindObjectOfType<Player.PlayerProgression>();
                if (progression != null)
                {
                    foreach (var itemID in achievement.itemRewards)
                    {
                        progression.UnlockItem(itemID);
                    }
                }
            }

            // Award title
            if (!string.IsNullOrEmpty(achievement.titleReward))
            {
                // TODO: Unlock player title
                if (showDebugLogs)
                    Debug.Log($"[AchievementManager] Unlocked title: {achievement.titleReward}");
            }

            // Award banner
            if (achievement.bannerReward != null)
            {
                // TODO: Unlock profile banner
                if (showDebugLogs)
                    Debug.Log($"[AchievementManager] Unlocked banner");
            }
        }

        private void ShowAchievementUnlockedNotification(AchievementData achievement)
        {
            // TODO: Show UI notification
            // Could use a toast/popup system
            if (showDebugLogs)
                Debug.Log($"[AchievementManager] 🏆 {achievement.achievementName} - {achievement.description}");
        }

        #endregion

        #region Queries

        /// <summary>
        /// Checks if an achievement is unlocked
        /// </summary>
        public bool IsUnlocked(string achievementID)
        {
            return unlockedAchievements.Contains(achievementID);
        }

        /// <summary>
        /// Gets achievement by ID
        /// </summary>
        public AchievementData GetAchievementByID(string achievementID)
        {
            return allAchievements?.FirstOrDefault(a => a.achievementID == achievementID);
        }

        /// <summary>
        /// Gets achievement progress
        /// </summary>
        public AchievementProgress GetProgress(string achievementID)
        {
            return achievementProgress.ContainsKey(achievementID) ? achievementProgress[achievementID] : null;
        }

        /// <summary>
        /// Gets all achievements in a category
        /// </summary>
        public List<AchievementData> GetAchievementsByCategory(AchievementCategory category)
        {
            return allAchievements?.Where(a => a.category == category).ToList();
        }

        /// <summary>
        /// Gets all unlocked achievements
        /// </summary>
        public List<AchievementData> GetUnlockedAchievements()
        {
            return allAchievements?.Where(a => IsUnlocked(a.achievementID)).ToList();
        }

        /// <summary>
        /// Gets all locked achievements (excluding hidden)
        /// </summary>
        public List<AchievementData> GetLockedAchievements(bool includeHidden = false)
        {
            return allAchievements?.Where(a => !IsUnlocked(a.achievementID) && (includeHidden || !a.isHidden)).ToList();
        }

        /// <summary>
        /// Gets achievement completion percentage for a category
        /// </summary>
        public float GetCategoryCompletion(AchievementCategory category)
        {
            var categoryAchievements = GetAchievementsByCategory(category);
            if (categoryAchievements == null || categoryAchievements.Count == 0)
                return 0f;

            int unlocked = categoryAchievements.Count(a => IsUnlocked(a.achievementID));
            return (float)unlocked / categoryAchievements.Count;
        }

        /// <summary>
        /// Gets total achievement points (based on rarity)
        /// </summary>
        public int GetTotalAchievementPoints()
        {
            int points = 0;

            foreach (var achievement in allAchievements)
            {
                if (IsUnlocked(achievement.achievementID))
                {
                    points += achievement.rarity switch
                    {
                        AchievementRarity.Common => 10,
                        AchievementRarity.Uncommon => 25,
                        AchievementRarity.Rare => 50,
                        AchievementRarity.Epic => 100,
                        AchievementRarity.Legendary => 250,
                        _ => 0
                    };
                }
            }

            return points;
        }

        #endregion

        #region Event Tracking

        /// <summary>
        /// Tracks game events and updates relevant achievements
        /// </summary>
        public void TrackEvent(AchievementEventType eventType, int count = 1, string specificID = "")
        {
            // Find achievements that track this event
            foreach (var achievement in allAchievements)
            {
                if (achievement == null || IsUnlocked(achievement.achievementID))
                    continue;

                // Match event type to achievement (simplified - expand as needed)
                bool matches = false;

                switch (eventType)
                {
                    case AchievementEventType.ZombieKilled:
                        if (achievement.achievementID.Contains("zombie_kills"))
                            matches = true;
                        break;

                    case AchievementEventType.HeadshotKill:
                        if (achievement.achievementID.Contains("headshot"))
                            matches = true;
                        break;

                    case AchievementEventType.PlayerKilled:
                        if (achievement.achievementID.Contains("player_kills"))
                            matches = true;
                        break;

                    case AchievementEventType.SuccessfulExtraction:
                        if (achievement.achievementID.Contains("extraction"))
                            matches = true;
                        break;

                    case AchievementEventType.ItemLooted:
                        if (achievement.achievementID.Contains("loot"))
                            matches = true;
                        break;

                    case AchievementEventType.MatchWon:
                        if (achievement.achievementID.Contains("win"))
                            matches = true;
                        break;

                    // ... add more event types
                }

                if (matches)
                {
                    UpdateProgress(achievement.achievementID, count);
                }
            }
        }

        #endregion

        #region Save/Load

        private void SaveProgress()
        {
            // Save unlocked achievements
            PlayerPrefs.SetString("Achievements_Unlocked", string.Join(",", unlockedAchievements));

            // Save progress for each achievement
            foreach (var kvp in achievementProgress)
            {
                PlayerPrefs.SetInt($"Achievement_{kvp.Key}_Progress", kvp.Value.currentProgress);
                PlayerPrefs.SetInt($"Achievement_{kvp.Key}_Unlocked", kvp.Value.isUnlocked ? 1 : 0);
                PlayerPrefs.SetString($"Achievement_{kvp.Key}_UnlockTime", kvp.Value.unlockTime.ToString());
            }

            PlayerPrefs.Save();

            if (showDebugLogs)
                Debug.Log("[AchievementManager] Progress saved");
        }

        private void LoadProgress()
        {
            // Load unlocked achievements
            string unlockedStr = PlayerPrefs.GetString("Achievements_Unlocked", "");
            if (!string.IsNullOrEmpty(unlockedStr))
            {
                var ids = unlockedStr.Split(',');
                foreach (var id in ids)
                {
                    if (!string.IsNullOrEmpty(id))
                        unlockedAchievements.Add(id);
                }
            }

            // Load progress for each achievement
            foreach (var achievement in allAchievements)
            {
                if (achievement == null)
                    continue;

                string id = achievement.achievementID;

                if (achievementProgress.ContainsKey(id))
                {
                    achievementProgress[id].currentProgress = PlayerPrefs.GetInt($"Achievement_{id}_Progress", 0);
                    achievementProgress[id].isUnlocked = PlayerPrefs.GetInt($"Achievement_{id}_Unlocked", 0) == 1;

                    string unlockTimeStr = PlayerPrefs.GetString($"Achievement_{id}_UnlockTime", "0");
                    if (long.TryParse(unlockTimeStr, out long ticks))
                    {
                        achievementProgress[id].unlockTime = ticks;
                    }
                }
            }

            if (showDebugLogs)
                Debug.Log($"[AchievementManager] Loaded progress - {UnlockedCount}/{TotalAchievements} unlocked");
        }

        /// <summary>
        /// Resets all achievement progress (for testing)
        /// </summary>
        public void ResetAllAchievements()
        {
            unlockedAchievements.Clear();
            achievementProgress.Clear();
            InitializeAchievements();
            SaveProgress();

            Debug.Log("[AchievementManager] All achievements reset");
        }

        #endregion
    }

    /// <summary>
    /// Tracks progress for a single achievement
    /// </summary>
    [System.Serializable]
    public class AchievementProgress
    {
        public string achievementID;
        public int currentProgress;
        public bool isUnlocked;
        public long unlockTime; // DateTime.Ticks

        public System.DateTime GetUnlockDate()
        {
            return new System.DateTime(unlockTime);
        }
    }

    /// <summary>
    /// Event types for achievement tracking
    /// </summary>
    public enum AchievementEventType
    {
        ZombieKilled,
        HeadshotKill,
        PlayerKilled,
        SuccessfulExtraction,
        FailedExtraction,
        ItemLooted,
        RareItemLooted,
        WeaponFired,
        ReloadCompleted,
        MatchStarted,
        MatchWon,
        MatchLost,
        DamageTaken,
        DamageDealt,
        HealthRestored,
        PlayerRevived,
        DistanceTraveled,
        ChallengeCompleted,
        LevelUp,
        PrestigeUp,
        PerkEquipped,
        LoadoutSaved
    }
}
