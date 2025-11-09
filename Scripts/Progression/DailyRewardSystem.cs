using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;
using System;

namespace DeadFrontier.Progression
{
    /// <summary>
    /// Comprehensive daily login reward system.
    /// Tracks login streaks, provides daily/weekly/monthly rewards, milestone bonuses.
    /// Encourages player retention through progressive rewards.
    /// </summary>
    public class DailyRewardSystem : NetworkBehaviour
    {
        public static DailyRewardSystem Instance { get; private set; }

        [Header("Reward Settings")]
        [SerializeField] private int maxDailyRewardTier = 7;
        [SerializeField] private int maxMonthlyRewardTier = 30;
        [SerializeField] private float streakResetHours = 48f; // Grace period before streak resets
        [SerializeField] private int streakBonusThreshold = 7; // Days for bonus

        [Header("Reward Multipliers")]
        [SerializeField] private float weekendMultiplier = 1.5f;
        [SerializeField] private float streakBonusMultiplier = 1.2f;
        [SerializeField] private float milestoneMultiplier = 2.0f;

        [Header("Special Events")]
        [SerializeField] private bool doubleRewardEventActive = false;
        [SerializeField] private DateTime doubleRewardEventEnd;

        // Player login data
        private Dictionary<ulong, DailyRewardData> playerRewardData = new Dictionary<ulong, DailyRewardData>();

        // Reward templates
        private Dictionary<int, DailyRewardTier> dailyRewardTiers = new Dictionary<int, DailyRewardTier>();
        private Dictionary<int, MonthlyRewardTier> monthlyRewardTiers = new Dictionary<int, MonthlyRewardTier>();
        private Dictionary<int, MilestoneReward> milestoneRewards = new Dictionary<int, MilestoneReward>();

        // Events
        public event Action<ulong, RewardClaim> OnRewardClaimed;
        public event Action<ulong, int> OnStreakIncreased;
        public event Action<ulong, int> OnMilestoneReached;
        public event Action<ulong> OnStreakBroken;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeRewardTiers();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        #region Initialization

        private void InitializeRewardTiers()
        {
            // Daily reward tiers (day 1-7 cycle)
            dailyRewardTiers[1] = new DailyRewardTier
            {
                day = 1,
                softCurrency = 100,
                hardCurrency = 0,
                xpBonus = 50,
                items = new List<RewardItem>()
            };

            dailyRewardTiers[2] = new DailyRewardTier
            {
                day = 2,
                softCurrency = 150,
                hardCurrency = 0,
                xpBonus = 75,
                items = new List<RewardItem> { new RewardItem { itemId = "consumable_medkit", quantity = 1 } }
            };

            dailyRewardTiers[3] = new DailyRewardTier
            {
                day = 3,
                softCurrency = 200,
                hardCurrency = 5,
                xpBonus = 100,
                items = new List<RewardItem> { new RewardItem { itemId = "consumable_ammo", quantity = 2 } }
            };

            dailyRewardTiers[4] = new DailyRewardTier
            {
                day = 4,
                softCurrency = 250,
                hardCurrency = 0,
                xpBonus = 125,
                items = new List<RewardItem> { new RewardItem { itemId = "consumable_grenade", quantity = 1 } }
            };

            dailyRewardTiers[5] = new DailyRewardTier
            {
                day = 5,
                softCurrency = 300,
                hardCurrency = 10,
                xpBonus = 150,
                items = new List<RewardItem> { new RewardItem { itemId = "weapon_attachment", quantity = 1 } }
            };

            dailyRewardTiers[6] = new DailyRewardTier
            {
                day = 6,
                softCurrency = 400,
                hardCurrency = 0,
                xpBonus = 200,
                items = new List<RewardItem> { new RewardItem { itemId = "loot_crate_common", quantity = 1 } }
            };

            dailyRewardTiers[7] = new DailyRewardTier
            {
                day = 7,
                softCurrency = 500,
                hardCurrency = 25,
                xpBonus = 300,
                items = new List<RewardItem>
                {
                    new RewardItem { itemId = "loot_crate_rare", quantity = 1 },
                    new RewardItem { itemId = "cosmetic_random", quantity = 1 }
                }
            };

            // Monthly reward tiers (day 1-30)
            for (int i = 1; i <= maxMonthlyRewardTier; i++)
            {
                monthlyRewardTiers[i] = new MonthlyRewardTier
                {
                    day = i,
                    softCurrency = 50 + (i * 10),
                    hardCurrency = i % 5 == 0 ? 5 : 0, // Every 5 days
                    xpBonus = 25 + (i * 5),
                    items = GenerateMonthlyRewardItems(i)
                };
            }

            // Milestone rewards (total logins)
            milestoneRewards[7] = new MilestoneReward
            {
                totalDays = 7,
                name = "One Week Warrior",
                softCurrency = 1000,
                hardCurrency = 50,
                items = new List<RewardItem> { new RewardItem { itemId = "cosmetic_title_week", quantity = 1 } }
            };

            milestoneRewards[30] = new MilestoneReward
            {
                totalDays = 30,
                name = "Monthly Veteran",
                softCurrency = 5000,
                hardCurrency = 200,
                items = new List<RewardItem>
                {
                    new RewardItem { itemId = "cosmetic_title_month", quantity = 1 },
                    new RewardItem { itemId = "weapon_skin_epic", quantity = 1 }
                }
            };

            milestoneRewards[100] = new MilestoneReward
            {
                totalDays = 100,
                name = "Hundred Days Hero",
                softCurrency = 10000,
                hardCurrency = 500,
                items = new List<RewardItem>
                {
                    new RewardItem { itemId = "cosmetic_title_hundred", quantity = 1 },
                    new RewardItem { itemId = "character_skin_legendary", quantity = 1 }
                }
            };

            milestoneRewards[365] = new MilestoneReward
            {
                totalDays = 365,
                name = "Year Long Legend",
                softCurrency = 50000,
                hardCurrency = 2000,
                items = new List<RewardItem>
                {
                    new RewardItem { itemId = "cosmetic_title_year", quantity = 1 },
                    new RewardItem { itemId = "character_skin_mythic", quantity = 1 },
                    new RewardItem { itemId = "weapon_skin_mythic", quantity = 1 }
                }
            };
        }

        private List<RewardItem> GenerateMonthlyRewardItems(int day)
        {
            var items = new List<RewardItem>();

            // Every 7 days, add loot crate
            if (day % 7 == 0)
            {
                items.Add(new RewardItem { itemId = "loot_crate_rare", quantity = 1 });
            }
            // Every 10 days, add cosmetic
            else if (day % 10 == 0)
            {
                items.Add(new RewardItem { itemId = "cosmetic_random", quantity = 1 });
            }
            // Every 15 days, add weapon skin
            else if (day % 15 == 0)
            {
                items.Add(new RewardItem { itemId = "weapon_skin_epic", quantity = 1 });
            }
            // Day 30: Big reward
            else if (day == 30)
            {
                items.Add(new RewardItem { itemId = "loot_crate_epic", quantity = 1 });
                items.Add(new RewardItem { itemId = "character_skin_legendary", quantity = 1 });
            }

            return items;
        }

        #endregion

        #region Daily Login

        public void ProcessDailyLogin(ulong playerId)
        {
            if (!playerRewardData.ContainsKey(playerId))
            {
                playerRewardData[playerId] = new DailyRewardData
                {
                    playerId = playerId,
                    firstLoginDate = DateTime.UtcNow,
                    lastLoginDate = DateTime.UtcNow,
                    totalLoginDays = 0,
                    currentStreak = 0,
                    longestStreak = 0,
                    lastClaimDate = DateTime.MinValue,
                    monthlyCalendar = new bool[31]
                };
            }

            var data = playerRewardData[playerId];
            DateTime now = DateTime.UtcNow;

            // Check if already logged in today
            if (data.lastLoginDate.Date == now.Date)
            {
                Debug.Log($"[DailyRewardSystem] Player {playerId} already logged in today");
                return;
            }

            // Update login data
            DateTime lastLogin = data.lastLoginDate;
            data.lastLoginDate = now;
            data.totalLoginDays++;

            // Check streak
            TimeSpan timeSinceLastLogin = now - lastLogin;

            if (timeSinceLastLogin.TotalHours <= streakResetHours)
            {
                // Continue streak
                data.currentStreak++;

                if (data.currentStreak > data.longestStreak)
                {
                    data.longestStreak = data.currentStreak;
                }

                OnStreakIncreased?.Invoke(playerId, data.currentStreak);
            }
            else
            {
                // Streak broken
                if (data.currentStreak > 0)
                {
                    OnStreakBroken?.Invoke(playerId);
                }
                data.currentStreak = 1;
            }

            // Update monthly calendar
            int dayOfMonth = now.Day - 1; // 0-indexed
            if (dayOfMonth >= 0 && dayOfMonth < data.monthlyCalendar.Length)
            {
                data.monthlyCalendar[dayOfMonth] = true;
            }

            // Check for milestone rewards
            CheckMilestoneRewards(playerId, data.totalLoginDays);

            // Notify client
            ProcessDailyLoginClientRpc(playerId, data.currentStreak, data.totalLoginDays);

            SavePlayerData(playerId);

            Debug.Log($"[DailyRewardSystem] Player {playerId} logged in. Streak: {data.currentStreak}, Total: {data.totalLoginDays}");
        }

        [ClientRpc]
        private void ProcessDailyLoginClientRpc(ulong playerId, int currentStreak, int totalDays)
        {
            // Show login UI notification
            Debug.Log($"[DailyRewardSystem] Welcome back! Current streak: {currentStreak} days, Total logins: {totalDays}");
        }

        #endregion

        #region Reward Claiming

        public bool CanClaimDailyReward(ulong playerId)
        {
            if (!playerRewardData.ContainsKey(playerId)) return false;

            var data = playerRewardData[playerId];
            DateTime now = DateTime.UtcNow;

            // Check if already claimed today
            if (data.lastClaimDate.Date == now.Date)
            {
                return false;
            }

            // Check if logged in today
            if (data.lastLoginDate.Date != now.Date)
            {
                return false;
            }

            return true;
        }

        public RewardClaim ClaimDailyReward(ulong playerId)
        {
            if (!CanClaimDailyReward(playerId))
            {
                Debug.LogWarning($"[DailyRewardSystem] Player {playerId} cannot claim daily reward");
                return null;
            }

            var data = playerRewardData[playerId];
            DateTime now = DateTime.UtcNow;

            // Get current tier (cycles 1-7)
            int tierDay = ((data.currentStreak - 1) % maxDailyRewardTier) + 1;
            var tier = dailyRewardTiers[tierDay];

            // Calculate reward
            var claim = new RewardClaim
            {
                playerId = playerId,
                claimDate = now,
                rewardType = RewardType.Daily,
                tier = tierDay,
                streak = data.currentStreak
            };

            // Base rewards
            claim.softCurrency = tier.softCurrency;
            claim.hardCurrency = tier.hardCurrency;
            claim.xpBonus = tier.xpBonus;
            claim.items = new List<RewardItem>(tier.items);

            // Apply multipliers
            ApplyRewardMultipliers(claim, data);

            // Grant rewards
            GrantRewards(playerId, claim);

            // Update claim date
            data.lastClaimDate = now;
            data.rewardHistory.Add(claim);

            SavePlayerData(playerId);

            OnRewardClaimed?.Invoke(playerId, claim);

            Debug.Log($"[DailyRewardSystem] Player {playerId} claimed daily reward: Day {tierDay}, Streak {data.currentStreak}");

            // Notify client
            ClaimDailyRewardClientRpc(playerId, claim.softCurrency, claim.hardCurrency, claim.xpBonus);

            return claim;
        }

        [ClientRpc]
        private void ClaimDailyRewardClientRpc(ulong playerId, int softCurrency, int hardCurrency, int xpBonus)
        {
            // Show reward claim UI
            Debug.Log($"[DailyRewardSystem] Claimed rewards: {softCurrency} coins, {hardCurrency} gems, {xpBonus} XP");
        }

        public bool CanClaimMonthlyReward(ulong playerId, int day)
        {
            if (!playerRewardData.ContainsKey(playerId)) return false;
            if (day < 1 || day > maxMonthlyRewardTier) return false;

            var data = playerRewardData[playerId];
            int dayIndex = day - 1;

            // Check if logged in that day
            if (!data.monthlyCalendar[dayIndex]) return false;

            // Check if already claimed
            if (data.claimedMonthlyRewards.Contains(day)) return false;

            return true;
        }

        public RewardClaim ClaimMonthlyReward(ulong playerId, int day)
        {
            if (!CanClaimMonthlyReward(playerId, day))
            {
                Debug.LogWarning($"[DailyRewardSystem] Player {playerId} cannot claim monthly reward for day {day}");
                return null;
            }

            var data = playerRewardData[playerId];
            var tier = monthlyRewardTiers[day];

            var claim = new RewardClaim
            {
                playerId = playerId,
                claimDate = DateTime.UtcNow,
                rewardType = RewardType.Monthly,
                tier = day,
                streak = data.currentStreak
            };

            claim.softCurrency = tier.softCurrency;
            claim.hardCurrency = tier.hardCurrency;
            claim.xpBonus = tier.xpBonus;
            claim.items = new List<RewardItem>(tier.items);

            // Apply multipliers
            ApplyRewardMultipliers(claim, data);

            // Grant rewards
            GrantRewards(playerId, claim);

            // Mark as claimed
            data.claimedMonthlyRewards.Add(day);

            SavePlayerData(playerId);

            OnRewardClaimed?.Invoke(playerId, claim);

            Debug.Log($"[DailyRewardSystem] Player {playerId} claimed monthly reward for day {day}");

            return claim;
        }

        #endregion

        #region Multipliers & Bonuses

        private void ApplyRewardMultipliers(RewardClaim claim, DailyRewardData data)
        {
            float totalMultiplier = 1f;

            // Weekend bonus
            if (IsWeekend(claim.claimDate))
            {
                totalMultiplier *= weekendMultiplier;
                claim.bonuses.Add("Weekend Bonus");
            }

            // Streak bonus (every 7 days)
            if (data.currentStreak >= streakBonusThreshold && data.currentStreak % streakBonusThreshold == 0)
            {
                totalMultiplier *= streakBonusMultiplier;
                claim.bonuses.Add($"{data.currentStreak} Day Streak Bonus");
            }

            // Double reward event
            if (doubleRewardEventActive && DateTime.UtcNow < doubleRewardEventEnd)
            {
                totalMultiplier *= 2f;
                claim.bonuses.Add("Double Reward Event");
            }

            // Apply multiplier
            if (totalMultiplier > 1f)
            {
                claim.softCurrency = Mathf.RoundToInt(claim.softCurrency * totalMultiplier);
                claim.hardCurrency = Mathf.RoundToInt(claim.hardCurrency * totalMultiplier);
                claim.xpBonus = Mathf.RoundToInt(claim.xpBonus * totalMultiplier);
            }
        }

        private bool IsWeekend(DateTime date)
        {
            return date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;
        }

        #endregion

        #region Milestone Rewards

        private void CheckMilestoneRewards(ulong playerId, int totalDays)
        {
            foreach (var kvp in milestoneRewards)
            {
                int milestone = kvp.Key;
                var reward = kvp.Value;

                if (totalDays == milestone)
                {
                    GrantMilestoneReward(playerId, reward);
                }
            }
        }

        private void GrantMilestoneReward(ulong playerId, MilestoneReward reward)
        {
            var claim = new RewardClaim
            {
                playerId = playerId,
                claimDate = DateTime.UtcNow,
                rewardType = RewardType.Milestone,
                tier = reward.totalDays,
                softCurrency = Mathf.RoundToInt(reward.softCurrency * milestoneMultiplier),
                hardCurrency = Mathf.RoundToInt(reward.hardCurrency * milestoneMultiplier),
                items = new List<RewardItem>(reward.items)
            };

            GrantRewards(playerId, claim);

            OnMilestoneReached?.Invoke(playerId, reward.totalDays);

            Debug.Log($"[DailyRewardSystem] Player {playerId} reached milestone: {reward.name} ({reward.totalDays} days)");

            // Notify client
            GrantMilestoneRewardClientRpc(playerId, reward.name, reward.totalDays);
        }

        [ClientRpc]
        private void GrantMilestoneRewardClientRpc(ulong playerId, string milestoneName, int days)
        {
            // Show big milestone UI
            Debug.Log($"[DailyRewardSystem] MILESTONE REACHED: {milestoneName} - {days} days!");
        }

        #endregion

        #region Reward Granting

        private void GrantRewards(ulong playerId, RewardClaim claim)
        {
            // Grant currency
            if (claim.softCurrency > 0)
            {
                Economy.EconomyManager.Instance?.AddSoftCurrency(playerId, claim.softCurrency);
            }

            if (claim.hardCurrency > 0)
            {
                Economy.EconomyManager.Instance?.AddHardCurrency(playerId, claim.hardCurrency);
            }

            // Grant XP
            if (claim.xpBonus > 0)
            {
                Progression.ProgressionManager.Instance?.AddExperience(playerId, claim.xpBonus);
            }

            // Grant items
            foreach (var item in claim.items)
            {
                Inventory.InventoryManager.Instance?.AddItem(playerId, item.itemId, item.quantity);
            }
        }

        #endregion

        #region Calendar Management

        public bool[] GetMonthlyCalendar(ulong playerId)
        {
            if (!playerRewardData.ContainsKey(playerId)) return new bool[31];

            return playerRewardData[playerId].monthlyCalendar;
        }

        public void ResetMonthlyCalendar(ulong playerId)
        {
            if (!playerRewardData.ContainsKey(playerId)) return;

            var data = playerRewardData[playerId];
            data.monthlyCalendar = new bool[31];
            data.claimedMonthlyRewards.Clear();

            SavePlayerData(playerId);

            Debug.Log($"[DailyRewardSystem] Reset monthly calendar for player {playerId}");
        }

        public void CheckMonthlyCalendarReset()
        {
            // Called daily to check if it's a new month
            foreach (var kvp in playerRewardData)
            {
                var data = kvp.Value;

                // If last login was in a different month, reset calendar
                if (data.lastLoginDate.Month != DateTime.UtcNow.Month ||
                    data.lastLoginDate.Year != DateTime.UtcNow.Year)
                {
                    ResetMonthlyCalendar(kvp.Key);
                }
            }
        }

        #endregion

        #region Special Events

        public void StartDoubleRewardEvent(float durationHours)
        {
            doubleRewardEventActive = true;
            doubleRewardEventEnd = DateTime.UtcNow.AddHours(durationHours);

            Debug.Log($"[DailyRewardSystem] Double Reward Event started! Ends: {doubleRewardEventEnd}");

            // Notify all clients
            StartDoubleRewardEventClientRpc(durationHours);
        }

        [ClientRpc]
        private void StartDoubleRewardEventClientRpc(float durationHours)
        {
            Debug.Log($"[DailyRewardSystem] DOUBLE REWARDS ACTIVE for {durationHours} hours!");
        }

        public void EndDoubleRewardEvent()
        {
            doubleRewardEventActive = false;
            Debug.Log("[DailyRewardSystem] Double Reward Event ended");
        }

        #endregion

        #region Data Management

        private void SavePlayerData(ulong playerId)
        {
            if (!playerRewardData.ContainsKey(playerId)) return;

            var data = playerRewardData[playerId];
            string json = JsonUtility.ToJson(data);

            // Integrate with save system
            SaveSystem.SaveManager.Instance?.SaveData($"daily_rewards_{playerId}", json);
        }

        public void LoadPlayerData(ulong playerId)
        {
            string json = SaveSystem.SaveManager.Instance?.LoadData($"daily_rewards_{playerId}");

            if (!string.IsNullOrEmpty(json))
            {
                var data = JsonUtility.FromJson<DailyRewardData>(json);
                playerRewardData[playerId] = data;

                Debug.Log($"[DailyRewardSystem] Loaded data for player {playerId}");
            }
        }

        #endregion

        #region Public Getters

        public DailyRewardData GetPlayerRewardData(ulong playerId)
        {
            return playerRewardData.ContainsKey(playerId) ? playerRewardData[playerId] : null;
        }

        public int GetCurrentStreak(ulong playerId)
        {
            return playerRewardData.ContainsKey(playerId) ? playerRewardData[playerId].currentStreak : 0;
        }

        public int GetTotalLoginDays(ulong playerId)
        {
            return playerRewardData.ContainsKey(playerId) ? playerRewardData[playerId].totalLoginDays : 0;
        }

        public List<RewardClaim> GetRewardHistory(ulong playerId, int maxCount = 10)
        {
            if (!playerRewardData.ContainsKey(playerId)) return new List<RewardClaim>();

            return playerRewardData[playerId].rewardHistory
                .OrderByDescending(r => r.claimDate)
                .Take(maxCount)
                .ToList();
        }

        public DailyRewardTier GetNextDailyReward(ulong playerId)
        {
            if (!playerRewardData.ContainsKey(playerId)) return dailyRewardTiers[1];

            var data = playerRewardData[playerId];
            int nextTierDay = ((data.currentStreak) % maxDailyRewardTier) + 1;

            return dailyRewardTiers[nextTierDay];
        }

        public List<int> GetAvailableMilestones()
        {
            return milestoneRewards.Keys.OrderBy(k => k).ToList();
        }

        #endregion
    }

    #region Data Classes

    [Serializable]
    public class DailyRewardData
    {
        public ulong playerId;
        public DateTime firstLoginDate;
        public DateTime lastLoginDate;
        public DateTime lastClaimDate;
        public int totalLoginDays;
        public int currentStreak;
        public int longestStreak;
        public bool[] monthlyCalendar = new bool[31]; // Days of month
        public List<int> claimedMonthlyRewards = new List<int>();
        public List<RewardClaim> rewardHistory = new List<RewardClaim>();
    }

    [Serializable]
    public class DailyRewardTier
    {
        public int day;
        public int softCurrency;
        public int hardCurrency;
        public int xpBonus;
        public List<RewardItem> items = new List<RewardItem>();
    }

    [Serializable]
    public class MonthlyRewardTier
    {
        public int day;
        public int softCurrency;
        public int hardCurrency;
        public int xpBonus;
        public List<RewardItem> items = new List<RewardItem>();
    }

    [Serializable]
    public class MilestoneReward
    {
        public int totalDays;
        public string name;
        public int softCurrency;
        public int hardCurrency;
        public List<RewardItem> items = new List<RewardItem>();
    }

    [Serializable]
    public class RewardClaim
    {
        public ulong playerId;
        public DateTime claimDate;
        public RewardType rewardType;
        public int tier;
        public int streak;
        public int softCurrency;
        public int hardCurrency;
        public int xpBonus;
        public List<RewardItem> items = new List<RewardItem>();
        public List<string> bonuses = new List<string>(); // Multiplier descriptions
    }

    [Serializable]
    public class RewardItem
    {
        public string itemId;
        public int quantity;
    }

    public enum RewardType
    {
        Daily,
        Monthly,
        Milestone,
        Special
    }

    #endregion
}
