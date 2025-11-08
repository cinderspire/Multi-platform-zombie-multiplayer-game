using UnityEngine;
using System;
using System.Collections.Generic;

namespace DeadFrontier.Core.Rewards
{
    /// <summary>
    /// Manages daily login rewards and streak bonuses
    /// Encourages daily engagement with escalating rewards
    /// </summary>
    public class DailyRewardsManager : Singleton<DailyRewardsManager>
    {
        [Header("Reward Configuration")]
        [SerializeField] private List<DailyReward> dailyRewards = new List<DailyReward>();
        [SerializeField] private int maxStreakDays = 7;
        [SerializeField] private bool resetStreakOnMiss = true;

        [Header("Grace Period")]
        [SerializeField] private int gracePeriodHours = 48; // Allow missing one day

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        // State
        private int currentStreak = 0;
        private DateTime lastClaimDate;
        private bool hasClaimedToday = false;
        private int totalLoginDays = 0;

        // Events
        public event System.Action<DailyReward, int> OnRewardClaimed; // reward, day
        public event System.Action<int> OnStreakBroken; // old streak
        public event System.Action<int> OnStreakMilestone; // streak days

        protected override void Awake()
        {
            base.Awake();
            LoadProgress();
            CheckDailyStatus();
        }

        private void Start()
        {
            // Show rewards UI if available today
            if (CanClaimReward())
            {
                // Could auto-show UI here
                if (showDebugLogs)
                    Debug.Log("[DailyRewardsManager] Daily reward available!");
            }
        }

        #region Daily Rewards

        /// <summary>
        /// Checks if player can claim today's reward
        /// </summary>
        public bool CanClaimReward()
        {
            return !hasClaimedToday;
        }

        /// <summary>
        /// Claims today's daily reward
        /// </summary>
        public bool ClaimDailyReward()
        {
            if (!CanClaimReward())
            {
                if (showDebugLogs)
                    Debug.LogWarning("[DailyRewardsManager] Already claimed today's reward");
                return false;
            }

            // Get reward for current day
            int rewardIndex = (currentStreak % maxStreakDays);
            if (rewardIndex >= dailyRewards.Count)
                rewardIndex = dailyRewards.Count - 1;

            DailyReward reward = dailyRewards[rewardIndex];

            // Grant reward
            GrantReward(reward);

            // Update state
            hasClaimedToday = true;
            lastClaimDate = DateTime.Now;
            currentStreak++;
            totalLoginDays++;

            if (showDebugLogs)
                Debug.Log($"[DailyRewardsManager] Claimed day {currentStreak} reward: {reward.rewardName}");

            OnRewardClaimed?.Invoke(reward, currentStreak);

            // Check for streak milestones
            CheckStreakMilestones();

            // Track analytics
            Analytics.AnalyticsManager.Instance?.TrackEvent("daily_reward_claimed", new Dictionary<string, object>
            {
                { "day", currentStreak },
                { "streak", currentStreak },
                { "reward_type", reward.rewardType.ToString() },
                { "total_logins", totalLoginDays }
            });

            SaveProgress();
            return true;
        }

        private void GrantReward(DailyReward reward)
        {
            switch (reward.rewardType)
            {
                case RewardType.SoftCurrency:
                    if (Economy.EconomyManager.Instance != null)
                    {
                        Economy.EconomyManager.Instance.AddSoftCurrency(reward.amount, "Daily reward");
                    }
                    break;

                case RewardType.HardCurrency:
                    if (Economy.EconomyManager.Instance != null)
                    {
                        Economy.EconomyManager.Instance.AddHardCurrency(reward.amount, "Daily reward");
                    }
                    break;

                case RewardType.XP:
                    var progression = GameObject.FindObjectOfType<Player.PlayerProgression>();
                    if (progression != null)
                    {
                        progression.AwardXP(reward.amount);
                    }
                    break;

                case RewardType.BattlePassXP:
                    if (Progression.BattlePass.BattlePassManager.Instance != null)
                    {
                        Progression.BattlePass.BattlePassManager.Instance.AwardXP(reward.amount, "Daily reward");
                    }
                    break;

                case RewardType.Item:
                    // Grant specific item
                    // Implementation depends on inventory system
                    break;
            }

            // Show notification
            if (UI.NotificationManager.Instance != null)
            {
                UI.NotificationManager.Instance.ShowNotification(
                    "Daily Reward!",
                    $"Day {currentStreak}: {reward.rewardName}",
                    new Color(1f, 0.84f, 0f),
                    UI.NotificationType.Reward
                );
            }
        }

        #endregion

        #region Streak Management

        private void CheckDailyStatus()
        {
            DateTime now = DateTime.Now;
            TimeSpan timeSinceLastClaim = now - lastClaimDate;

            // Check if it's a new day
            if (lastClaimDate.Date < now.Date)
            {
                hasClaimedToday = false;

                // Check if streak should be broken
                if (timeSinceLastClaim.TotalHours > gracePeriodHours)
                {
                    if (currentStreak > 0)
                    {
                        if (showDebugLogs)
                            Debug.Log($"[DailyRewardsManager] Streak broken! Was {currentStreak} days");

                        int oldStreak = currentStreak;
                        OnStreakBroken?.Invoke(oldStreak);

                        // Track analytics
                        Analytics.AnalyticsManager.Instance?.TrackEvent("daily_streak_broken", new Dictionary<string, object>
                        {
                            { "streak_length", oldStreak },
                            { "hours_since_last", timeSinceLastClaim.TotalHours }
                        });
                    }

                    if (resetStreakOnMiss)
                    {
                        currentStreak = 0;
                    }
                }
            }

            if (showDebugLogs)
                Debug.Log($"[DailyRewardsManager] Current streak: {currentStreak} days. Can claim: {CanClaimReward()}");
        }

        private void CheckStreakMilestones()
        {
            // Check for milestone streaks
            if (currentStreak == 7)
            {
                OnStreakMilestone?.Invoke(7);
                GrantStreakMilestoneBonus(7);
            }
            else if (currentStreak == 30)
            {
                OnStreakMilestone?.Invoke(30);
                GrantStreakMilestoneBonus(30);
            }
            else if (currentStreak == 100)
            {
                OnStreakMilestone?.Invoke(100);
                GrantStreakMilestoneBonus(100);
            }
        }

        private void GrantStreakMilestoneBonus(int days)
        {
            if (showDebugLogs)
                Debug.Log($"[DailyRewardsManager] Streak milestone reached: {days} days!");

            // Grant bonus based on milestone
            int bonusCurrency = days * 10;

            if (Economy.EconomyManager.Instance != null)
            {
                Economy.EconomyManager.Instance.AddHardCurrency(bonusCurrency, $"{days} day streak bonus");
            }

            // Show notification
            if (UI.NotificationManager.Instance != null)
            {
                UI.NotificationManager.Instance.ShowNotification(
                    $"{days} Day Streak!",
                    $"Bonus: {bonusCurrency} premium currency",
                    new Color(1f, 0.5f, 0f),
                    UI.NotificationType.Reward
                );
            }

            // Track analytics
            Analytics.AnalyticsManager.Instance?.TrackEvent("streak_milestone", new Dictionary<string, object>
            {
                { "milestone_days", days },
                { "bonus_amount", bonusCurrency }
            });
        }

        #endregion

        #region Save/Load

        private void LoadProgress()
        {
            currentStreak = PlayerPrefs.GetInt("DailyReward_Streak", 0);
            totalLoginDays = PlayerPrefs.GetInt("DailyReward_TotalDays", 0);
            hasClaimedToday = PlayerPrefs.GetInt("DailyReward_ClaimedToday", 0) == 1;

            long lastClaimTicks = long.Parse(PlayerPrefs.GetString("DailyReward_LastClaim", "0"));
            if (lastClaimTicks > 0)
            {
                lastClaimDate = new DateTime(lastClaimTicks);
            }
            else
            {
                lastClaimDate = DateTime.MinValue;
            }

            if (showDebugLogs)
                Debug.Log($"[DailyRewardsManager] Loaded progress. Streak: {currentStreak}, Total days: {totalLoginDays}");
        }

        private void SaveProgress()
        {
            PlayerPrefs.SetInt("DailyReward_Streak", currentStreak);
            PlayerPrefs.SetInt("DailyReward_TotalDays", totalLoginDays);
            PlayerPrefs.SetInt("DailyReward_ClaimedToday", hasClaimedToday ? 1 : 0);
            PlayerPrefs.SetString("DailyReward_LastClaim", lastClaimDate.Ticks.ToString());
            PlayerPrefs.Save();
        }

        #endregion

        #region Public API

        public int CurrentStreak => currentStreak;
        public int TotalLoginDays => totalLoginDays;
        public bool HasClaimedToday => hasClaimedToday;
        public DateTime LastClaimDate => lastClaimDate;
        public TimeSpan TimeUntilNextReward
        {
            get
            {
                if (!hasClaimedToday)
                    return TimeSpan.Zero;

                DateTime tomorrow = lastClaimDate.Date.AddDays(1);
                return tomorrow - DateTime.Now;
            }
        }

        /// <summary>
        /// Gets the reward for a specific day
        /// </summary>
        public DailyReward GetRewardForDay(int day)
        {
            int index = (day - 1) % maxStreakDays;
            if (index >= 0 && index < dailyRewards.Count)
                return dailyRewards[index];
            return null;
        }

        /// <summary>
        /// Gets today's reward (without claiming it)
        /// </summary>
        public DailyReward GetTodaysReward()
        {
            int nextDay = currentStreak + 1;
            return GetRewardForDay(nextDay);
        }

        /// <summary>
        /// Gets all configured daily rewards
        /// </summary>
        public List<DailyReward> GetAllRewards()
        {
            return new List<DailyReward>(dailyRewards);
        }

        /// <summary>
        /// Resets daily rewards progress (for testing)
        /// </summary>
        [ContextMenu("Reset Daily Rewards")]
        public void ResetProgress()
        {
            currentStreak = 0;
            totalLoginDays = 0;
            hasClaimedToday = false;
            lastClaimDate = DateTime.MinValue;
            SaveProgress();

            if (showDebugLogs)
                Debug.Log("[DailyRewardsManager] Progress reset");
        }

        /// <summary>
        /// Simulates claiming reward (for testing)
        /// </summary>
        [ContextMenu("Test Claim Reward")]
        public void TestClaimReward()
        {
            hasClaimedToday = false;
            ClaimDailyReward();
        }

        #endregion
    }

    #region Data Structures

    public enum RewardType
    {
        SoftCurrency,
        HardCurrency,
        XP,
        BattlePassXP,
        Item,
        Cosmetic
    }

    [System.Serializable]
    public class DailyReward
    {
        public string rewardName;
        public RewardType rewardType;
        public int amount;
        public Sprite icon;
        [TextArea(2, 3)]
        public string description;
    }

    #endregion
}
