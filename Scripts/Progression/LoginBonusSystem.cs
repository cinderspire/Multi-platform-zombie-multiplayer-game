using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Progression
{
    /// <summary>
    /// Login bonus system rewarding players for daily logins with escalating rewards
    /// and special bonuses for consecutive login streaks.
    /// </summary>
    public class LoginBonusSystem : NetworkBehaviour
    {
        public static LoginBonusSystem Instance { get; private set; }

        [Header("Login Bonus Configuration")]
        [SerializeField] private int maxConsecutiveDays = 30;
        [SerializeField] private bool resetStreakOnMissedDay = false;
        [SerializeField] private int gracePeriodHours = 24;

        private Dictionary<ulong, PlayerLoginData> playerLoginData = new Dictionary<ulong, PlayerLoginData>();

        public event Action<ulong, int, LoginReward> OnLoginBonusClaimed;
        public event Action<ulong, int> OnLoginStreakUpdated;
        public event Action<ulong, int> OnMilestoneReached;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        /// <summary>
        /// Check and award login bonus
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void CheckLoginBonusServerRpc(ulong playerId, ServerRpcParams rpcParams = default)
        {
            if (!playerLoginData.ContainsKey(playerId))
            {
                InitializePlayerLoginData(playerId);
            }

            var loginData = playerLoginData[playerId];
            DateTime now = DateTime.UtcNow;

            // Check if already claimed today
            if (loginData.lastClaimDate.Date == now.Date)
            {
                // Already claimed today
                NotifyAlreadyClaimedClientRpc(playerId);
                return;
            }

            // Check streak
            TimeSpan timeSinceLastLogin = now - loginData.lastClaimDate;

            if (timeSinceLastLogin.TotalHours > gracePeriodHours && timeSinceLastLogin.TotalHours <= 48)
            {
                // Within grace period - maintain streak
                loginData.consecutiveDays++;
            }
            else if (timeSinceLastLogin.TotalHours > 48)
            {
                // Streak broken
                if (resetStreakOnMissedDay)
                {
                    loginData.consecutiveDays = 1;
                }
                else
                {
                    loginData.consecutiveDays++;
                }
            }
            else
            {
                // First login or continuing streak
                loginData.consecutiveDays++;
            }

            // Cap at max days
            if (loginData.consecutiveDays > maxConsecutiveDays)
            {
                loginData.consecutiveDays = 1; // Reset to start of cycle
            }

            loginData.lastClaimDate = now;
            loginData.totalLogins++;

            // Award bonus
            LoginReward reward = GetRewardForDay(loginData.consecutiveDays);
            AwardLoginBonus(playerId, reward);

            OnLoginBonusClaimed?.Invoke(playerId, loginData.consecutiveDays, reward);
            OnLoginStreakUpdated?.Invoke(playerId, loginData.consecutiveDays);

            NotifyLoginBonusClientRpc(playerId, loginData.consecutiveDays, reward);

            // Check milestones
            CheckMilestones(playerId, loginData.totalLogins);
        }

        private void InitializePlayerLoginData(ulong playerId)
        {
            playerLoginData[playerId] = new PlayerLoginData
            {
                playerId = playerId,
                consecutiveDays = 0,
                totalLogins = 0,
                lastClaimDate = DateTime.MinValue,
                claimedMilestones = new List<int>()
            };
        }

        private LoginReward GetRewardForDay(int day)
        {
            LoginReward reward = new LoginReward();

            // Base rewards
            reward.softCurrency = 100 * day;
            reward.xp = 50 * day;

            // Special rewards for certain days
            if (day % 7 == 0) // Weekly bonus
            {
                reward.hardCurrency = 50;
                reward.lootBoxes.Add(new LootBox { rarity = "Rare", count = 1 });
            }

            if (day % 30 == 0) // Monthly bonus
            {
                reward.hardCurrency = 200;
                reward.lootBoxes.Add(new LootBox { rarity = "Epic", count = 1 });
                reward.exclusiveItems.Add("monthly_skin_" + DateTime.UtcNow.Month);
            }

            // Milestone rewards
            switch (day)
            {
                case 3:
                    reward.lootBoxes.Add(new LootBox { rarity = "Common", count = 1 });
                    break;
                case 7:
                    reward.lootBoxes.Add(new LootBox { rarity = "Uncommon", count = 1 });
                    break;
                case 14:
                    reward.lootBoxes.Add(new LootBox { rarity = "Rare", count = 1 });
                    break;
                case 21:
                    reward.hardCurrency += 100;
                    break;
                case 30:
                    reward.lootBoxes.Add(new LootBox { rarity = "Legendary", count = 1 });
                    reward.exclusiveItems.Add("30_day_title");
                    break;
            }

            return reward;
        }

        private void AwardLoginBonus(ulong playerId, LoginReward reward)
        {
            // Award soft currency
            if (reward.softCurrency > 0 && Economy.EconomyManager.Instance != null)
            {
                // Would call economy system
            }

            // Award hard currency
            if (reward.hardCurrency > 0 && Economy.EconomyManager.Instance != null)
            {
                // Would call economy system
            }

            // Award XP
            if (reward.xp > 0 && ProgressionSystem.Instance != null)
            {
                ProgressionSystem.Instance.AddExperienceServerRpc(playerId, reward.xp);
            }

            // Award loot boxes
            foreach (var lootBox in reward.lootBoxes)
            {
                // Would integrate with loot box system
            }

            // Award exclusive items
            foreach (var itemId in reward.exclusiveItems)
            {
                if (Inventory.InventorySystem.Instance != null)
                {
                    Inventory.InventorySystem.Instance.AddItemServerRpc(playerId, itemId, 1);
                }
            }
        }

        private void CheckMilestones(ulong playerId, int totalLogins)
        {
            var loginData = playerLoginData[playerId];
            int[] milestones = { 10, 25, 50, 100, 250, 500, 1000 };

            foreach (int milestone in milestones)
            {
                if (totalLogins >= milestone && !loginData.claimedMilestones.Contains(milestone))
                {
                    loginData.claimedMilestones.Add(milestone);
                    AwardMilestoneBonus(playerId, milestone);
                    OnMilestoneReached?.Invoke(playerId, milestone);
                }
            }
        }

        private void AwardMilestoneBonus(ulong playerId, int milestone)
        {
            // Award special milestone rewards
            int bonusCurrency = milestone * 10;
            string exclusiveTitle = $"login_milestone_{milestone}";

            // Award currency
            // Award exclusive title
            if (TitleSystem.Instance != null)
            {
                TitleSystem.Instance.UnlockTitleServerRpc(playerId, exclusiveTitle);
            }

            NotifyMilestoneClientRpc(playerId, milestone);
        }

        [ClientRpc]
        private void NotifyLoginBonusClientRpc(ulong playerId, int day, LoginReward reward)
        {
            if (NetworkManager.Singleton.LocalClientId != playerId) return;

            Debug.Log($"<color=gold>DAY {day} LOGIN BONUS!</color>");
            Debug.Log($"Rewards: {reward.softCurrency} coins, {reward.xp} XP");

            if (reward.hardCurrency > 0)
                Debug.Log($"+ {reward.hardCurrency} premium currency!");

            foreach (var box in reward.lootBoxes)
                Debug.Log($"+ {box.count}x {box.rarity} Loot Box");
        }

        [ClientRpc]
        private void NotifyAlreadyClaimedClientRpc(ulong playerId)
        {
            if (NetworkManager.Singleton.LocalClientId != playerId) return;
            Debug.Log("You've already claimed your login bonus today!");
        }

        [ClientRpc]
        private void NotifyMilestoneClientRpc(ulong playerId, int milestone)
        {
            if (NetworkManager.Singleton.LocalClientId != playerId) return;
            Debug.Log($"<color=orange>MILESTONE REACHED: {milestone} Total Logins!</color>");
        }

        public int GetCurrentStreak(ulong playerId)
        {
            return playerLoginData.TryGetValue(playerId, out var data) ? data.consecutiveDays : 0;
        }

        public int GetTotalLogins(ulong playerId)
        {
            return playerLoginData.TryGetValue(playerId, out var data) ? data.totalLogins : 0;
        }

        [Serializable]
        private class PlayerLoginData
        {
            public ulong playerId;
            public int consecutiveDays;
            public int totalLogins;
            public DateTime lastClaimDate;
            public List<int> claimedMilestones;
        }

        [Serializable]
        public class LoginReward
        {
            public int softCurrency;
            public int hardCurrency;
            public int xp;
            public List<LootBox> lootBoxes = new List<LootBox>();
            public List<string> exclusiveItems = new List<string>();
        }

        [Serializable]
        public class LootBox
        {
            public string rarity;
            public int count;
        }
    }
}
