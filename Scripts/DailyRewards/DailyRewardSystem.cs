using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.DailyRewards
{
    /// <summary>
    /// Comprehensive daily rewards system with login streaks, monthly calendars, comeback bonuses,
    /// and milestone rewards to drive player retention.
    /// </summary>
    public class DailyRewardSystem : NetworkBehaviour
    {
        public static DailyRewardSystem Instance { get; private set; }

        [Header("Daily Reward Configuration")]
        [SerializeField] private int maxStreakDays = 30;
        [SerializeField] private int comebackBonusDays = 7;

        private Dictionary<int, DailyReward> dailyRewardCalendar = new Dictionary<int, DailyReward>();
        private Dictionary<int, StreakMilestone> streakMilestones = new Dictionary<int, StreakMilestone>();
        private Dictionary<ulong, PlayerDailyData> playerDailyData = new Dictionary<ulong, PlayerDailyData>();

        public event Action<ulong, int> OnDailyRewardClaimed;
        public event Action<ulong, int> OnStreakMilestone;
        public event Action<ulong> OnComebackBonus;

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
                InitializeDailyRewards();
                InitializeStreakMilestones();
            }
        }

        private void InitializeDailyRewards()
        {
            // 30-day calendar with escalating rewards
            for (int day = 1; day <= 30; day++)
            {
                var reward = new DailyReward { day = day, rewards = new List<RewardItem>() };

                // Base rewards
                reward.rewards.Add(new RewardItem { itemType = RewardItemType.SoftCurrency, itemId = "currency_soft", quantity = 100 * day });

                // Every 5 days - bonus rewards
                if (day % 5 == 0)
                {
                    reward.rewards.Add(new RewardItem { itemType = RewardItemType.HardCurrency, itemId = "currency_hard", quantity = 10 * (day / 5) });
                    reward.rewards.Add(new RewardItem { itemType = RewardItemType.Item, itemId = "crate_common", quantity = 1 });
                }

                // Every 7 days - weekly milestone
                if (day % 7 == 0)
                {
                    reward.rewards.Add(new RewardItem { itemType = RewardItemType.Item, itemId = "crate_rare", quantity = 1 });
                    reward.rewards.Add(new RewardItem { itemType = RewardItemType.XPBoost, itemId = "boost_xp_1h", quantity = 1 });
                }

                // Day 14 - mid-month bonus
                if (day == 14)
                {
                    reward.rewards.Add(new RewardItem { itemType = RewardItemType.HardCurrency, itemId = "currency_hard", quantity = 50 });
                    reward.rewards.Add(new RewardItem { itemType = RewardItemType.Item, itemId = "crate_epic", quantity = 1 });
                }

                // Day 30 - monthly grand prize
                if (day == 30)
                {
                    reward.rewards.Add(new RewardItem { itemType = RewardItemType.HardCurrency, itemId = "currency_hard", quantity = 200 });
                    reward.rewards.Add(new RewardItem { itemType = RewardItemType.Item, itemId = "crate_legendary", quantity = 1 });
                    reward.rewards.Add(new RewardItem { itemType = RewardItemType.Cosmetic, itemId = "skin_monthly_exclusive", quantity = 1 });
                    reward.featured = true;
                }

                dailyRewardCalendar[day] = reward;
            }

            Debug.Log($"Initialized {dailyRewardCalendar.Count} daily rewards");
        }

        private void InitializeStreakMilestones()
        {
            streakMilestones[7] = new StreakMilestone
            {
                streakDays = 7,
                milestoneName = "Week Warrior",
                description = "Login 7 days in a row",
                rewards = new List<RewardItem>
                {
                    new RewardItem { itemType = RewardItemType.SoftCurrency, itemId = "currency_soft", quantity = 1000 },
                    new RewardItem { itemType = RewardItemType.Title, itemId = "title_week_warrior", quantity = 1 }
                }
            };

            streakMilestones[14] = new StreakMilestone
            {
                streakDays = 14,
                milestoneName = "Fortnight Fighter",
                description = "Login 14 days in a row",
                rewards = new List<RewardItem>
                {
                    new RewardItem { itemType = RewardItemType.SoftCurrency, itemId = "currency_soft", quantity = 2500 },
                    new RewardItem { itemType = RewardItemType.HardCurrency, itemId = "currency_hard", quantity = 50 },
                    new RewardItem { itemType = RewardItemType.Item, itemId = "crate_epic", quantity = 2 }
                }
            };

            streakMilestones[30] = new StreakMilestone
            {
                streakDays = 30,
                milestoneName = "Monthly Master",
                description = "Login 30 days in a row",
                rewards = new List<RewardItem>
                {
                    new RewardItem { itemType = RewardItemType.SoftCurrency, itemId = "currency_soft", quantity = 10000 },
                    new RewardItem { itemType = RewardItemType.HardCurrency, itemId = "currency_hard", quantity = 200 },
                    new RewardItem { itemType = RewardItemType.Item, itemId = "crate_legendary", quantity = 3 },
                    new RewardItem { itemType = RewardItemType.Cosmetic, itemId = "skin_streak_master", quantity = 1 },
                    new RewardItem { itemType = RewardItemType.Title, itemId = "title_dedication", quantity = 1 }
                }
            };

            streakMilestones[100] = new StreakMilestone
            {
                streakDays = 100,
                milestoneName = "Century Survivor",
                description = "Login 100 days in a row",
                rewards = new List<RewardItem>
                {
                    new RewardItem { itemType = RewardItemType.SoftCurrency, itemId = "currency_soft", quantity = 50000 },
                    new RewardItem { itemType = RewardItemType.HardCurrency, itemId = "currency_hard", quantity = 1000 },
                    new RewardItem { itemType = RewardItemType.Cosmetic, itemId = "skin_century_legend", quantity = 1 },
                    new RewardItem { itemType = RewardItemType.Cosmetic, itemId = "effect_dedication_aura", quantity = 1 },
                    new RewardItem { itemType = RewardItemType.Title, itemId = "title_century_survivor", quantity = 1 }
                }
            };

            Debug.Log($"Initialized {streakMilestones.Count} streak milestones");
        }

        [ServerRpc(RequireOwnership = false)]
        public void InitializePlayerDailyDataServerRpc(ulong playerId, ServerRpcParams rpcParams = default)
        {
            if (playerDailyData.ContainsKey(playerId)) return;

            playerDailyData[playerId] = new PlayerDailyData
            {
                playerId = playerId,
                currentStreak = 0,
                longestStreak = 0,
                lastLoginDate = DateTime.MinValue,
                totalLoginDays = 0,
                claimedDays = new List<int>(),
                claimedMilestones = new List<int>(),
                monthlyCalendarCycle = 0
            };
        }

        [ServerRpc(RequireOwnership = false)]
        public void ClaimDailyRewardServerRpc(ulong playerId, ServerRpcParams rpcParams = default)
        {
            if (!playerDailyData.TryGetValue(playerId, out var data)) return;

            DateTime today = DateTime.UtcNow.Date;
            DateTime lastLogin = data.lastLoginDate.Date;

            // Check if already claimed today
            if (lastLogin == today)
            {
                Debug.LogWarning($"Player {playerId} already claimed today's reward");
                return;
            }

            // Calculate streak
            if (lastLogin == today.AddDays(-1))
            {
                // Consecutive day
                data.currentStreak++;
            }
            else if (lastLogin < today.AddDays(-1))
            {
                // Streak broken
                if (data.currentStreak > 0 && (today - lastLogin).TotalDays >= comebackBonusDays)
                {
                    // Comeback bonus
                    GrantComebackBonus(playerId);
                }
                data.currentStreak = 1;
            }
            else
            {
                // First login
                data.currentStreak = 1;
            }

            // Update longest streak
            if (data.currentStreak > data.longestStreak)
            {
                data.longestStreak = data.currentStreak;
            }

            // Calculate calendar day (cycles through 1-30)
            int calendarDay = (data.totalLoginDays % maxStreakDays) + 1;

            // Grant daily reward
            if (dailyRewardCalendar.TryGetValue(calendarDay, out var reward))
            {
                GrantRewards(playerId, reward.rewards);
                data.claimedDays.Add(calendarDay);
            }

            // Check streak milestones
            CheckStreakMilestones(playerId, data);

            // Update data
            data.lastLoginDate = today;
            data.totalLoginDays++;

            // Reset calendar cycle if completed
            if (calendarDay == maxStreakDays)
            {
                data.monthlyCalendarCycle++;
                data.claimedDays.Clear();
            }

            OnDailyRewardClaimed?.Invoke(playerId, calendarDay);
            NotifyDailyRewardClaimedClientRpc(playerId, calendarDay, data.currentStreak);

            Debug.Log($"Player {playerId} claimed day {calendarDay} reward. Streak: {data.currentStreak}");
        }

        private void CheckStreakMilestones(ulong playerId, PlayerDailyData data)
        {
            foreach (var milestone in streakMilestones.Values)
            {
                if (data.currentStreak >= milestone.streakDays && !data.claimedMilestones.Contains(milestone.streakDays))
                {
                    GrantRewards(playerId, milestone.rewards);
                    data.claimedMilestones.Add(milestone.streakDays);

                    OnStreakMilestone?.Invoke(playerId, milestone.streakDays);
                    NotifyStreakMilestoneClientRpc(playerId, milestone.streakDays, milestone.milestoneName);

                    Debug.Log($"Player {playerId} reached {milestone.streakDays}-day streak milestone");
                }
            }
        }

        private void GrantComebackBonus(ulong playerId)
        {
            var comebackRewards = new List<RewardItem>
            {
                new RewardItem { itemType = RewardItemType.SoftCurrency, itemId = "currency_soft", quantity = 2000 },
                new RewardItem { itemType = RewardItemType.HardCurrency, itemId = "currency_hard", quantity = 50 },
                new RewardItem { itemType = RewardItemType.Item, itemId = "crate_rare", quantity = 1 },
                new RewardItem { itemType = RewardItemType.XPBoost, itemId = "boost_xp_1h", quantity = 1 }
            };

            GrantRewards(playerId, comebackRewards);
            OnComebackBonus?.Invoke(playerId);
            NotifyComebackBonusClientRpc(playerId);

            Debug.Log($"Player {playerId} received comeback bonus");
        }

        private void GrantRewards(ulong playerId, List<RewardItem> rewards)
        {
            foreach (var reward in rewards)
            {
                switch (reward.itemType)
                {
                    case RewardItemType.SoftCurrency:
                        Economy.EconomyManager.Instance?.AddCurrencyServerRpc(playerId, Economy.CurrencyType.Soft, reward.quantity);
                        break;
                    case RewardItemType.HardCurrency:
                        Economy.EconomyManager.Instance?.AddCurrencyServerRpc(playerId, Economy.CurrencyType.Hard, reward.quantity);
                        break;
                    case RewardItemType.Item:
                        Inventory.InventorySystem.Instance?.AddItemServerRpc(playerId, reward.itemId, reward.quantity, Inventory.ContainerType.Backpack);
                        break;
                    case RewardItemType.XPBoost:
                        // Would integrate with buff system
                        break;
                    case RewardItemType.Cosmetic:
                        Cosmetics.CosmeticSystem.Instance?.UnlockCosmeticServerRpc(playerId, reward.itemId);
                        break;
                    case RewardItemType.Title:
                        // Would integrate with title system
                        break;
                }
            }
        }

        [ClientRpc]
        private void NotifyDailyRewardClaimedClientRpc(ulong playerId, int day, int streak) { }

        [ClientRpc]
        private void NotifyStreakMilestoneClientRpc(ulong playerId, int streakDays, string milestoneName) { }

        [ClientRpc]
        private void NotifyComebackBonusClientRpc(ulong playerId) { }

        public PlayerDailyData GetPlayerDailyData(ulong playerId) => playerDailyData.GetValueOrDefault(playerId);
        public DailyReward GetDailyReward(int day) => dailyRewardCalendar.GetValueOrDefault(day);
        public List<DailyReward> GetMonthlyCalendar() => dailyRewardCalendar.Values.OrderBy(r => r.day).ToList();
        public int GetNextRewardDay(ulong playerId)
        {
            if (!playerDailyData.TryGetValue(playerId, out var data)) return 1;
            return (data.totalLoginDays % maxStreakDays) + 1;
        }
    }

    [Serializable]
    public class DailyReward
    {
        public int day;
        public List<RewardItem> rewards;
        public bool featured;
    }

    [Serializable]
    public class RewardItem
    {
        public RewardItemType itemType;
        public string itemId;
        public int quantity;
    }

    [Serializable]
    public class StreakMilestone
    {
        public int streakDays;
        public string milestoneName;
        public string description;
        public List<RewardItem> rewards;
    }

    [Serializable]
    public class PlayerDailyData
    {
        public ulong playerId;
        public int currentStreak;
        public int longestStreak;
        public DateTime lastLoginDate;
        public int totalLoginDays;
        public List<int> claimedDays;
        public List<int> claimedMilestones;
        public int monthlyCalendarCycle;
    }

    public enum RewardItemType { SoftCurrency, HardCurrency, Item, XPBoost, Cosmetic, Title }
}
