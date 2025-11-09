using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace DeadFrontier.Progression
{
    /// <summary>
    /// Comprehensive battle pass and seasonal content system.
    /// Supports free and premium tracks, challenges, tier skips, and rewards.
    /// Provides ongoing engagement and monetization through seasonal content.
    /// </summary>
    public class BattlePassSystem : MonoBehaviour
    {
        public static BattlePassSystem Instance { get; private set; }

        [Header("Battle Pass Settings")]
        [SerializeField] private int maxTier = 100;
        [SerializeField] private int xpPerTier = 1000;
        [SerializeField] private int premiumPassCost = 950; // Hard currency
        [SerializeField] private int tierSkipCost = 150; // Hard currency per tier

        [Header("Season Settings")]
        [SerializeField] private float seasonDurationDays = 90f;
        [SerializeField] private BattlePassSeasonData currentSeasonData;

        [Header("Rewards")]
        [SerializeField] private BattlePassReward[] freeRewards;
        [SerializeField] private BattlePassReward[] premiumRewards;

        [Header("Challenges")]
        [SerializeField] private int dailyChallengesCount = 3;
        [SerializeField] private int weeklyChallengesCount = 7;
        [SerializeField] private BattlePassChallenge[] availableChallenges;

        // Player battle pass data
        private Dictionary<ulong, PlayerBattlePassData> playerBattlePassData = new Dictionary<ulong, PlayerBattlePassData>();

        // Active season
        private BattlePassSeason currentSeason;

        // Challenge rotation
        private List<BattlePassChallenge> activeDailyChallenges = new List<BattlePassChallenge>();
        private List<BattlePassChallenge> activeWeeklyChallenges = new List<BattlePassChallenge>();
        private DateTime lastDailyReset;
        private DateTime lastWeeklyReset;

        // Events
        public event Action<ulong, int> OnTierIncreased;
        public event Action<ulong, BattlePassReward> OnRewardClaimed;
        public event Action<ulong> OnPremiumPassPurchased;
        public event Action<BattlePassSeason> OnSeasonStarted;
        public event Action<BattlePassSeason> OnSeasonEnded;
        public event Action<ulong, BattlePassChallenge> OnChallengeCompleted;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            InitializeBattlePass();
            LoadAllPlayerData();
        }

        private void Update()
        {
            CheckSeasonExpiration();
            CheckChallengeRotation();
        }

        #region Initialization

        private void InitializeBattlePass()
        {
            if (currentSeason == null)
            {
                StartNewSeason();
            }

            RotateDailyChallenges();
            RotateWeeklyChallenges();

            Debug.Log($"[BattlePassSystem] Initialized. Season: {currentSeason.seasonNumber}");
        }

        private void StartNewSeason()
        {
            int seasonNumber = currentSeason != null ? currentSeason.seasonNumber + 1 : 1;

            currentSeason = new BattlePassSeason
            {
                seasonId = $"season_{seasonNumber}",
                seasonNumber = seasonNumber,
                seasonName = currentSeasonData != null ? currentSeasonData.seasonName : $"Season {seasonNumber}",
                startTime = DateTime.UtcNow,
                endTime = DateTime.UtcNow.AddDays(seasonDurationDays)
            };

            OnSeasonStarted?.Invoke(currentSeason);

            Debug.Log($"[BattlePassSystem] Started {currentSeason.seasonName}");
        }

        #endregion

        #region Player Progress

        public void InitializePlayerBattlePass(ulong playerId)
        {
            if (playerBattlePassData.ContainsKey(playerId)) return;

            var data = new PlayerBattlePassData
            {
                playerId = playerId,
                seasonId = currentSeason.seasonId,
                currentTier = 1,
                currentXP = 0,
                hasPremiumPass = false,
                claimedFreeRewards = new List<int>(),
                claimedPremiumRewards = new List<int>(),
                completedChallenges = new List<string>()
            };

            playerBattlePassData[playerId] = data;
        }

        public void AddBattlePassXP(ulong playerId, int xp, string source = "Unknown")
        {
            if (!playerBattlePassData.ContainsKey(playerId))
            {
                InitializePlayerBattlePass(playerId);
            }

            var data = playerBattlePassData[playerId];

            // Check if correct season
            if (data.seasonId != currentSeason.seasonId)
            {
                // Reset for new season
                ResetPlayerForNewSeason(playerId);
                data = playerBattlePassData[playerId];
            }

            data.currentXP += xp;

            // Check for tier ups
            while (data.currentXP >= xpPerTier && data.currentTier < maxTier)
            {
                data.currentXP -= xpPerTier;
                data.currentTier++;

                OnTierIncreased?.Invoke(playerId, data.currentTier);

                Debug.Log($"[BattlePassSystem] Player {playerId} reached tier {data.currentTier}");
            }

            SavePlayerBattlePassData(playerId);
        }

        private void ResetPlayerForNewSeason(ulong playerId)
        {
            var data = new PlayerBattlePassData
            {
                playerId = playerId,
                seasonId = currentSeason.seasonId,
                currentTier = 1,
                currentXP = 0,
                hasPremiumPass = false,
                claimedFreeRewards = new List<int>(),
                claimedPremiumRewards = new List<int>(),
                completedChallenges = new List<string>()
            };

            playerBattlePassData[playerId] = data;

            SavePlayerBattlePassData(playerId);
        }

        #endregion

        #region Premium Pass

        public bool PurchasePremiumPass(ulong playerId)
        {
            if (!playerBattlePassData.ContainsKey(playerId))
            {
                InitializePlayerBattlePass(playerId);
            }

            var data = playerBattlePassData[playerId];

            // Check if already owned
            if (data.hasPremiumPass) return false;

            // Check currency
            if (Economy.EconomyManager.Instance != null)
            {
                if (!Economy.EconomyManager.Instance.SpendHardCurrency(premiumPassCost, "Premium Battle Pass"))
                    return false;
            }

            data.hasPremiumPass = true;

            SavePlayerBattlePassData(playerId);

            OnPremiumPassPurchased?.Invoke(playerId);

            Debug.Log($"[BattlePassSystem] Player {playerId} purchased premium pass");

            return true;
        }

        public bool HasPremiumPass(ulong playerId)
        {
            if (!playerBattlePassData.ContainsKey(playerId)) return false;

            return playerBattlePassData[playerId].hasPremiumPass;
        }

        #endregion

        #region Tier Skip

        public bool PurchaseTierSkip(ulong playerId, int tiers = 1)
        {
            if (!playerBattlePassData.ContainsKey(playerId))
            {
                InitializePlayerBattlePass(playerId);
            }

            var data = playerBattlePassData[playerId];

            // Check if at max tier
            if (data.currentTier >= maxTier) return false;

            // Calculate cost
            int actualTiers = Mathf.Min(tiers, maxTier - data.currentTier);
            int cost = actualTiers * tierSkipCost;

            // Check currency
            if (Economy.EconomyManager.Instance != null)
            {
                if (!Economy.EconomyManager.Instance.SpendHardCurrency(cost, $"Battle Pass Tier Skip x{actualTiers}"))
                    return false;
            }

            // Skip tiers
            data.currentTier += actualTiers;
            data.currentXP = 0; // Reset XP when skipping

            SavePlayerBattlePassData(playerId);

            OnTierIncreased?.Invoke(playerId, data.currentTier);

            Debug.Log($"[BattlePassSystem] Player {playerId} skipped {actualTiers} tiers");

            return true;
        }

        #endregion

        #region Rewards

        public bool CanClaimReward(ulong playerId, int tier, bool isPremium)
        {
            if (!playerBattlePassData.ContainsKey(playerId)) return false;

            var data = playerBattlePassData[playerId];

            // Check tier requirement
            if (data.currentTier < tier) return false;

            // Check if already claimed
            if (isPremium)
            {
                if (!data.hasPremiumPass) return false;
                if (data.claimedPremiumRewards.Contains(tier)) return false;
            }
            else
            {
                if (data.claimedFreeRewards.Contains(tier)) return false;
            }

            return true;
        }

        public bool ClaimReward(ulong playerId, int tier, bool isPremium)
        {
            if (!CanClaimReward(playerId, tier, isPremium)) return false;

            var data = playerBattlePassData[playerId];

            // Get reward
            var reward = isPremium
                ? premiumRewards.FirstOrDefault(r => r.tier == tier)
                : freeRewards.FirstOrDefault(r => r.tier == tier);

            if (reward == null) return false;

            // Grant reward
            GrantReward(playerId, reward);

            // Mark as claimed
            if (isPremium)
            {
                data.claimedPremiumRewards.Add(tier);
            }
            else
            {
                data.claimedFreeRewards.Add(tier);
            }

            SavePlayerBattlePassData(playerId);

            OnRewardClaimed?.Invoke(playerId, reward);

            Debug.Log($"[BattlePassSystem] Player {playerId} claimed {(isPremium ? "premium" : "free")} reward at tier {tier}");

            return true;
        }

        public void ClaimAllAvailableRewards(ulong playerId)
        {
            if (!playerBattlePassData.ContainsKey(playerId)) return;

            var data = playerBattlePassData[playerId];

            // Claim all free rewards
            for (int tier = 1; tier <= data.currentTier; tier++)
            {
                if (!data.claimedFreeRewards.Contains(tier))
                {
                    ClaimReward(playerId, tier, false);
                }
            }

            // Claim all premium rewards if owned
            if (data.hasPremiumPass)
            {
                for (int tier = 1; tier <= data.currentTier; tier++)
                {
                    if (!data.claimedPremiumRewards.Contains(tier))
                    {
                        ClaimReward(playerId, tier, true);
                    }
                }
            }
        }

        private void GrantReward(ulong playerId, BattlePassReward reward)
        {
            switch (reward.rewardType)
            {
                case BattlePassRewardType.SoftCurrency:
                    if (Economy.EconomyManager.Instance != null)
                    {
                        Economy.EconomyManager.Instance.EarnSoftCurrency(reward.amount, "Battle Pass Reward");
                    }
                    break;

                case BattlePassRewardType.HardCurrency:
                    if (Economy.EconomyManager.Instance != null)
                    {
                        Economy.EconomyManager.Instance.AddHardCurrency(reward.amount, "Battle Pass Reward");
                    }
                    break;

                case BattlePassRewardType.Item:
                    if (Gameplay.InventoryManager.Instance != null)
                    {
                        var itemData = new Gameplay.LootItemData
                        {
                            itemId = reward.itemId,
                            itemName = reward.rewardName
                        };
                        Gameplay.InventoryManager.Instance.AddItem(playerId, itemData, reward.amount);
                    }
                    break;

                case BattlePassRewardType.Cosmetic:
                    // Unlock cosmetic
                    // This would integrate with cosmetic system
                    break;

                case BattlePassRewardType.XPBoost:
                    // Apply XP boost
                    // This would apply a temporary multiplier
                    break;
            }
        }

        #endregion

        #region Challenges

        private void RotateDailyChallenges()
        {
            activeDailyChallenges.Clear();

            var dailyChallenges = availableChallenges.Where(c => c.challengeType == ChallengeType.Daily).ToList();

            for (int i = 0; i < dailyChallengesCount && dailyChallenges.Count > 0; i++)
            {
                int randomIndex = UnityEngine.Random.Range(0, dailyChallenges.Count);
                activeDailyChallenges.Add(dailyChallenges[randomIndex]);
                dailyChallenges.RemoveAt(randomIndex);
            }

            lastDailyReset = DateTime.UtcNow;

            Debug.Log($"[BattlePassSystem] Rotated {activeDailyChallenges.Count} daily challenges");
        }

        private void RotateWeeklyChallenges()
        {
            activeWeeklyChallenges.Clear();

            var weeklyChallenges = availableChallenges.Where(c => c.challengeType == ChallengeType.Weekly).ToList();

            for (int i = 0; i < weeklyChallengesCount && weeklyChallenges.Count > 0; i++)
            {
                int randomIndex = UnityEngine.Random.Range(0, weeklyChallenges.Count);
                activeWeeklyChallenges.Add(weeklyChallenges[randomIndex]);
                weeklyChallenges.RemoveAt(randomIndex);
            }

            lastWeeklyReset = DateTime.UtcNow;

            Debug.Log($"[BattlePassSystem] Rotated {activeWeeklyChallenges.Count} weekly challenges");
        }

        private void CheckChallengeRotation()
        {
            // Daily reset (24 hours)
            if ((DateTime.UtcNow - lastDailyReset).TotalHours >= 24)
            {
                RotateDailyChallenges();
            }

            // Weekly reset (7 days)
            if ((DateTime.UtcNow - lastWeeklyReset).TotalDays >= 7)
            {
                RotateWeeklyChallenges();
            }
        }

        public void UpdateChallengeProgress(ulong playerId, string challengeId, float progress)
        {
            if (!playerBattlePassData.ContainsKey(playerId))
            {
                InitializePlayerBattlePass(playerId);
            }

            var data = playerBattlePassData[playerId];

            // Check if already completed
            if (data.completedChallenges.Contains(challengeId)) return;

            // Find challenge
            var challenge = availableChallenges.FirstOrDefault(c => c.challengeId == challengeId);
            if (challenge == null) return;

            // Check completion
            if (progress >= challenge.requiredProgress)
            {
                CompleteChallenge(playerId, challenge);
            }
        }

        private void CompleteChallenge(ulong playerId, BattlePassChallenge challenge)
        {
            var data = playerBattlePassData[playerId];

            data.completedChallenges.Add(challenge.challengeId);

            // Grant XP reward
            AddBattlePassXP(playerId, challenge.xpReward, $"Challenge: {challenge.challengeName}");

            SavePlayerBattlePassData(playerId);

            OnChallengeCompleted?.Invoke(playerId, challenge);

            Debug.Log($"[BattlePassSystem] Player {playerId} completed challenge: {challenge.challengeName}");
        }

        #endregion

        #region Season Management

        private void CheckSeasonExpiration()
        {
            if (currentSeason == null) return;

            if (DateTime.UtcNow >= currentSeason.endTime)
            {
                EndSeason();
            }
        }

        private void EndSeason()
        {
            OnSeasonEnded?.Invoke(currentSeason);

            Debug.Log($"[BattlePassSystem] {currentSeason.seasonName} ended");

            // Start new season
            StartNewSeason();
        }

        #endregion

        #region Persistence

        private void LoadAllPlayerData()
        {
            // In production, load from database
            Debug.Log("[BattlePassSystem] Battle pass system ready");
        }

        private void SavePlayerBattlePassData(ulong playerId)
        {
            // In production, save to database
        }

        #endregion

        #region Public Getters

        public PlayerBattlePassData GetPlayerData(ulong playerId)
        {
            if (!playerBattlePassData.ContainsKey(playerId))
            {
                InitializePlayerBattlePass(playerId);
            }

            return playerBattlePassData[playerId];
        }

        public int GetCurrentTier(ulong playerId)
        {
            return GetPlayerData(playerId).currentTier;
        }

        public int GetCurrentXP(ulong playerId)
        {
            return GetPlayerData(playerId).currentXP;
        }

        public float GetTierProgress(ulong playerId)
        {
            var data = GetPlayerData(playerId);
            return (float)data.currentXP / xpPerTier;
        }

        public List<BattlePassReward> GetFreeRewards() => new List<BattlePassReward>(freeRewards);

        public List<BattlePassReward> GetPremiumRewards() => new List<BattlePassReward>(premiumRewards);

        public List<BattlePassChallenge> GetActiveDailyChallenges() => new List<BattlePassChallenge>(activeDailyChallenges);

        public List<BattlePassChallenge> GetActiveWeeklyChallenges() => new List<BattlePassChallenge>(activeWeeklyChallenges);

        public BattlePassSeason GetCurrentSeason() => currentSeason;

        public TimeSpan GetSeasonTimeRemaining()
        {
            if (currentSeason == null) return TimeSpan.Zero;

            TimeSpan remaining = currentSeason.endTime - DateTime.UtcNow;
            return remaining.TotalSeconds > 0 ? remaining : TimeSpan.Zero;
        }

        #endregion
    }

    #region Data Classes

    public class PlayerBattlePassData
    {
        public ulong playerId;
        public string seasonId;
        public int currentTier;
        public int currentXP;
        public bool hasPremiumPass;
        public List<int> claimedFreeRewards;
        public List<int> claimedPremiumRewards;
        public List<string> completedChallenges;
    }

    public class BattlePassSeason
    {
        public string seasonId;
        public int seasonNumber;
        public string seasonName;
        public DateTime startTime;
        public DateTime endTime;
    }

    [System.Serializable]
    public class BattlePassSeasonData
    {
        public string seasonName;
        public Sprite seasonIcon;
        public string themeDescription;
    }

    [System.Serializable]
    public class BattlePassReward
    {
        public int tier;
        public string rewardName;
        public BattlePassRewardType rewardType;
        public int amount;
        public string itemId;
        public Sprite rewardIcon;
    }

    [System.Serializable]
    public class BattlePassChallenge
    {
        public string challengeId;
        public string challengeName;
        [TextArea(2, 3)]
        public string description;
        public ChallengeType challengeType;
        public float requiredProgress;
        public int xpReward;
    }

    public enum BattlePassRewardType
    {
        SoftCurrency,
        HardCurrency,
        Item,
        Cosmetic,
        XPBoost
    }

    public enum ChallengeType
    {
        Daily,
        Weekly,
        Seasonal
    }

    #endregion
}
