using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace DeadFrontier.Gameplay.Challenges
{
    /// <summary>
    /// Manages active challenges and tracks player progress
    /// </summary>
    public class ChallengeManager : Core.Singleton<ChallengeManager>
    {
        [Header("Challenge Settings")]
        [SerializeField] private int maxDailyChallenges = 3;
        [SerializeField] private int maxWeeklyChallenges = 5;
        [SerializeField] private ChallengeData[] allChallenges; // All available challenges

        [Header("Current Challenges")]
        [SerializeField] private List<ChallengeProgress> activeChallenges = new List<ChallengeProgress>();
        [SerializeField] private List<string> completedChallengeIDs = new List<string>();

        // Timers
        private float dailyResetTime;
        private float weeklyResetTime;

        // Events
        public event System.Action<ChallengeProgress> OnChallengeProgressUpdated;
        public event System.Action<ChallengeProgress> OnChallengeCompleted;
        public event System.Action OnChallengesRefreshed;

        // Properties
        public List<ChallengeProgress> ActiveChallenges => activeChallenges;

        protected override void Awake()
        {
            base.Awake();

            LoadChallenges();
            CheckResetTimers();
        }

        private void Start()
        {
            // Subscribe to game events
            SubscribeToEvents();

            // Refresh daily challenges if needed
            if (ShouldRefreshDailyChallenges())
            {
                RefreshDailyChallenges();
            }

            if (ShouldRefreshWeeklyChallenges())
            {
                RefreshWeeklyChallenges();
            }
        }

        private void Update()
        {
            // Check for challenge resets
            CheckResetTimers();
        }

        #region Challenge Management

        /// <summary>
        /// Activates a challenge for the player
        /// </summary>
        public bool ActivateChallenge(ChallengeData challenge)
        {
            if (challenge == null)
                return false;

            // Check if already active
            if (activeChallenges.Any(c => c.challengeID == challenge.challengeID))
            {
                Debug.LogWarning($"[ChallengeManager] Challenge {challenge.challengeName} already active");
                return false;
            }

            // Check if already completed
            if (completedChallengeIDs.Contains(challenge.challengeID))
            {
                Debug.LogWarning($"[ChallengeManager] Challenge {challenge.challengeName} already completed");
                return false;
            }

            // Create progress tracker
            ChallengeProgress progress = new ChallengeProgress
            {
                challengeID = challenge.challengeID,
                challengeData = challenge,
                objectiveProgress = new float[challenge.objectives.Length],
                isActive = true,
                startTime = System.DateTime.Now.Ticks
            };

            activeChallenges.Add(progress);
            SaveChallenges();

            Debug.Log($"[ChallengeManager] Activated challenge: {challenge.challengeName}");
            return true;
        }

        /// <summary>
        /// Refreshes daily challenges
        /// </summary>
        public void RefreshDailyChallenges()
        {
            // Remove old daily challenges
            activeChallenges.RemoveAll(c => c.challengeData.challengeType == ChallengeType.Daily);

            // Get available daily challenges
            var availableDailies = allChallenges
                .Where(c => c.challengeType == ChallengeType.Daily)
                .Where(c => !completedChallengeIDs.Contains(c.challengeID))
                .OrderBy(_ => Random.value)
                .Take(maxDailyChallenges);

            foreach (var challenge in availableDailies)
            {
                ActivateChallenge(challenge);
            }

            dailyResetTime = GetNextDailyResetTime();
            SaveChallenges();

            OnChallengesRefreshed?.Invoke();
            Debug.Log($"[ChallengeManager] Refreshed daily challenges ({availableDailies.Count()})");
        }

        /// <summary>
        /// Refreshes weekly challenges
        /// </summary>
        public void RefreshWeeklyChallenges()
        {
            // Remove old weekly challenges
            activeChallenges.RemoveAll(c => c.challengeData.challengeType == ChallengeType.Weekly);

            // Get available weekly challenges
            var availableWeeklies = allChallenges
                .Where(c => c.challengeType == ChallengeType.Weekly)
                .Where(c => !completedChallengeIDs.Contains(c.challengeID))
                .OrderBy(_ => Random.value)
                .Take(maxWeeklyChallenges);

            foreach (var challenge in availableWeeklies)
            {
                ActivateChallenge(challenge);
            }

            weeklyResetTime = GetNextWeeklyResetTime();
            SaveChallenges();

            OnChallengesRefreshed?.Invoke();
            Debug.Log($"[ChallengeManager] Refreshed weekly challenges ({availableWeeklies.Count()})");
        }

        #endregion

        #region Progress Tracking

        /// <summary>
        /// Updates progress for a specific objective type
        /// </summary>
        public void UpdateProgress(ChallengeObjectiveType objectiveType, float amount, string specificID = "")
        {
            foreach (var progress in activeChallenges)
            {
                if (!progress.isActive)
                    continue;

                UpdateChallengeProgress(progress, objectiveType, amount, specificID);
            }
        }

        private void UpdateChallengeProgress(ChallengeProgress progress, ChallengeObjectiveType objectiveType, float amount, string specificID)
        {
            bool progressUpdated = false;

            for (int i = 0; i < progress.challengeData.objectives.Length; i++)
            {
                var objective = progress.challengeData.objectives[i];

                // Check if objective matches
                if (objective.objectiveType != objectiveType)
                    continue;

                // Check specific requirements
                if (!string.IsNullOrEmpty(objective.weaponID) && objective.weaponID != specificID)
                    continue;

                // Update progress
                progress.objectiveProgress[i] += amount;
                progress.objectiveProgress[i] = Mathf.Min(progress.objectiveProgress[i], objective.targetValue);
                progressUpdated = true;
            }

            if (progressUpdated)
            {
                OnChallengeProgressUpdated?.Invoke(progress);

                // Check if challenge is now complete
                if (progress.challengeData.IsComplete(progress) && !progress.isCompleted)
                {
                    CompleteChallenge(progress);
                }

                SaveChallenges();
            }
        }

        /// <summary>
        /// Completes a challenge and awards rewards
        /// </summary>
        private void CompleteChallenge(ChallengeProgress progress)
        {
            progress.isCompleted = true;
            progress.completionTime = System.DateTime.Now.Ticks;

            Debug.Log($"[ChallengeManager] Challenge completed: {progress.challengeData.challengeName}");

            // Award rewards
            AwardChallengeRewards(progress.challengeData);

            // Mark as completed
            if (!completedChallengeIDs.Contains(progress.challengeID))
            {
                completedChallengeIDs.Add(progress.challengeID);
            }

            OnChallengeCompleted?.Invoke(progress);
            SaveChallenges();

            // Show completion notification
            ShowChallengeCompletionNotification(progress);
        }

        private void AwardChallengeRewards(ChallengeData challenge)
        {
            var progression = FindObjectOfType<Player.PlayerProgression>();

            // Award XP
            if (progression != null && challenge.xpReward > 0)
            {
                progression.AwardXP(challenge.xpReward, $"Challenge: {challenge.challengeName}");
            }

            // Award currency
            if (challenge.currencyReward > 0)
            {
                // TODO: Add currency to player
                Debug.Log($"[ChallengeManager] Awarded {challenge.currencyReward} currency");
            }

            // Award items
            if (challenge.itemRewards != null && challenge.itemRewards.Length > 0)
            {
                foreach (var itemID in challenge.itemRewards)
                {
                    // TODO: Add item to player inventory
                    Debug.Log($"[ChallengeManager] Awarded item: {itemID}");
                }
            }

            // Award battle pass XP
            if (challenge.battlePassXPReward > 0)
            {
                // TODO: Add to battle pass progression
                Debug.Log($"[ChallengeManager] Awarded {challenge.battlePassXPReward} battle pass XP");
            }
        }

        private void ShowChallengeCompletionNotification(ChallengeProgress progress)
        {
            // TODO: Show UI notification
            Debug.Log($"[ChallengeManager] CHALLENGE COMPLETE: {progress.challengeData.challengeName}");
        }

        #endregion

        #region Event Subscriptions

        private void SubscribeToEvents()
        {
            // Subscribe to game events to track challenge progress

            // Example: Track zombie kills
            // ZombieHealth.OnZombieDied += (zombie, isHeadshot) =>
            // {
            //     UpdateProgress(ChallengeObjectiveType.KillZombies, 1);
            //     if (isHeadshot)
            //         UpdateProgress(ChallengeObjectiveType.GetHeadshots, 1);
            // };
        }

        #endregion

        #region Reset Timers

        private void CheckResetTimers()
        {
            float currentTime = (float)System.DateTime.Now.TimeOfDay.TotalSeconds;

            // Check daily reset
            if (currentTime >= dailyResetTime && ShouldRefreshDailyChallenges())
            {
                RefreshDailyChallenges();
            }

            // Check weekly reset
            if (currentTime >= weeklyResetTime && ShouldRefreshWeeklyChallenges())
            {
                RefreshWeeklyChallenges();
            }
        }

        private bool ShouldRefreshDailyChallenges()
        {
            if (dailyResetTime == 0)
                return true;

            float currentTime = (float)System.DateTime.Now.TimeOfDay.TotalSeconds;
            return currentTime >= dailyResetTime;
        }

        private bool ShouldRefreshWeeklyChallenges()
        {
            if (weeklyResetTime == 0)
                return true;

            float currentTime = (float)System.DateTime.Now.TimeOfDay.TotalSeconds;
            return currentTime >= weeklyResetTime;
        }

        private float GetNextDailyResetTime()
        {
            // Reset at midnight
            var tomorrow = System.DateTime.Now.AddDays(1).Date;
            return (float)tomorrow.TimeOfDay.TotalSeconds;
        }

        private float GetNextWeeklyResetTime()
        {
            // Reset on Monday at midnight
            var now = System.DateTime.Now;
            int daysUntilMonday = ((int)System.DayOfWeek.Monday - (int)now.DayOfWeek + 7) % 7;
            if (daysUntilMonday == 0) daysUntilMonday = 7; // If today is Monday, next reset is in 7 days

            var nextMonday = now.AddDays(daysUntilMonday).Date;
            return (float)nextMonday.TimeOfDay.TotalSeconds;
        }

        #endregion

        #region Save/Load

        private void SaveChallenges()
        {
            // TODO: Implement proper save system
            Debug.Log($"[ChallengeManager] Saved {activeChallenges.Count} active challenges");
        }

        private void LoadChallenges()
        {
            // TODO: Load from save system
            Debug.Log("[ChallengeManager] Loaded challenges");
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets all active challenges of a specific type
        /// </summary>
        public List<ChallengeProgress> GetChallengesByType(ChallengeType type)
        {
            return activeChallenges.Where(c => c.challengeData.challengeType == type).ToList();
        }

        /// <summary>
        /// Gets challenge progress by ID
        /// </summary>
        public ChallengeProgress GetChallengeProgress(string challengeID)
        {
            return activeChallenges.FirstOrDefault(c => c.challengeID == challengeID);
        }

        /// <summary>
        /// Gets total challenges completed
        /// </summary>
        public int GetTotalChallengesCompleted()
        {
            return completedChallengeIDs.Count;
        }

        #endregion
    }

    /// <summary>
    /// Tracks progress for an individual challenge
    /// </summary>
    [System.Serializable]
    public class ChallengeProgress
    {
        public string challengeID;
        public ChallengeData challengeData;
        public float[] objectiveProgress;
        public bool isActive;
        public bool isCompleted;
        public long startTime; // DateTime.Ticks
        public long completionTime; // DateTime.Ticks

        /// <summary>
        /// Gets time elapsed since start
        /// </summary>
        public System.TimeSpan GetElapsedTime()
        {
            long currentTime = System.DateTime.Now.Ticks;
            return new System.TimeSpan(currentTime - startTime);
        }

        /// <summary>
        /// Gets remaining time if challenge has time limit
        /// </summary>
        public System.TimeSpan GetRemainingTime()
        {
            if (!challengeData.hasTimeLimit)
                return System.TimeSpan.MaxValue;

            long deadlineTime = startTime + (long)(challengeData.timeLimitHours * System.TimeSpan.TicksPerHour);
            long currentTime = System.DateTime.Now.Ticks;

            if (currentTime >= deadlineTime)
                return System.TimeSpan.Zero;

            return new System.TimeSpan(deadlineTime - currentTime);
        }

        /// <summary>
        /// Checks if challenge has expired
        /// </summary>
        public bool IsExpired()
        {
            if (!challengeData.hasTimeLimit)
                return false;

            return GetRemainingTime() <= System.TimeSpan.Zero;
        }
    }
}
