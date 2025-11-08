using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace DeadFrontier.Progression.BattlePass
{
    /// <summary>
    /// Manages battle pass progression and rewards
    /// </summary>
    public class BattlePassManager : Core.Singleton<BattlePassManager>
    {
        [Header("Current Season")]
        [SerializeField] private BattlePassData currentSeason;

        [Header("Player Progress")]
        [SerializeField] private int currentTier = 0;
        [SerializeField] private int currentXP = 0;
        [SerializeField] private bool hasPremiumPass = false;

        [Header("Claimed Rewards")]
        [SerializeField] private List<int> claimedFreeTiers = new List<int>();
        [SerializeField] private List<int> claimedPremiumTiers = new List<int>();

        // Events
        public event System.Action<int> OnXPGained;
        public event System.Action<int> OnTierUp;
        public event System.Action<BattlePassReward> OnRewardClaimed;
        public event System.Action OnPremiumPassPurchased;

        // Properties
        public int CurrentTier => currentTier;
        public int CurrentXP => currentXP;
        public bool HasPremiumPass => hasPremiumPass;
        public BattlePassData CurrentSeason => currentSeason;

        protected override void Awake()
        {
            base.Awake();

            LoadProgress();
        }

        private void Start()
        {
            // Check if season has ended
            if (currentSeason != null && !currentSeason.IsActive())
            {
                Debug.LogWarning("[BattlePassManager] Current season has ended!");
                // TODO: Handle season end (show summary, reset progress, etc.)
            }
        }

        #region XP and Progression

        /// <summary>
        /// Awards battle pass XP
        /// </summary>
        public void AwardXP(int amount, string reason = "")
        {
            if (currentSeason == null)
            {
                Debug.LogWarning("[BattlePassManager] No active season");
                return;
            }

            if (currentTier >= currentSeason.maxTier)
            {
                Debug.Log("[BattlePassManager] Max tier reached");
                return;
            }

            currentXP += amount;
            OnXPGained?.Invoke(amount);

            Debug.Log($"[BattlePassManager] +{amount} BP XP {(string.IsNullOrEmpty(reason) ? "" : $"({reason})")}");

            // Check for tier ups
            CheckTierUp();

            SaveProgress();
        }

        private void CheckTierUp()
        {
            if (currentSeason == null)
                return;

            int xpNeeded = currentSeason.GetXPForTier(currentTier + 1);

            while (currentXP >= xpNeeded && currentTier < currentSeason.maxTier)
            {
                currentXP -= xpNeeded;
                currentTier++;

                Debug.Log($"[BattlePassManager] TIER UP! Now tier {currentTier}");
                OnTierUp?.Invoke(currentTier);

                // Auto-claim free rewards
                if (!claimedFreeTiers.Contains(currentTier))
                {
                    ClaimTierRewards(currentTier, false);
                }

                // Get XP needed for next tier
                xpNeeded = currentSeason.GetXPForTier(currentTier + 1);
            }

            SaveProgress();
        }

        /// <summary>
        /// Gets progress to next tier (0-1)
        /// </summary>
        public float GetTierProgress()
        {
            if (currentSeason == null || currentTier >= currentSeason.maxTier)
                return 1f;

            int xpNeeded = currentSeason.GetXPForTier(currentTier + 1);
            return Mathf.Clamp01((float)currentXP / xpNeeded);
        }

        /// <summary>
        /// Skips tiers (from purchasing tier skips or bundle)
        /// </summary>
        public void SkipTiers(int amount)
        {
            if (currentSeason == null)
                return;

            int newTier = Mathf.Min(currentTier + amount, currentSeason.maxTier);
            int tiersSkipped = newTier - currentTier;

            if (tiersSkipped > 0)
            {
                currentTier = newTier;
                currentXP = 0; // Reset XP when skipping

                Debug.Log($"[BattlePassManager] Skipped {tiersSkipped} tier(s). Now tier {currentTier}");

                // Auto-claim all skipped tiers
                for (int i = currentTier - tiersSkipped + 1; i <= currentTier; i++)
                {
                    if (!claimedFreeTiers.Contains(i))
                    {
                        ClaimTierRewards(i, false);
                    }

                    if (hasPremiumPass && !claimedPremiumTiers.Contains(i))
                    {
                        ClaimTierRewards(i, true);
                    }
                }

                OnTierUp?.Invoke(currentTier);
                SaveProgress();
            }
        }

        #endregion

        #region Rewards

        /// <summary>
        /// Claims rewards for a tier
        /// </summary>
        public bool ClaimTierRewards(int tier, bool isPremium)
        {
            if (currentSeason == null)
                return false;

            if (tier > currentTier)
            {
                Debug.LogWarning("[BattlePassManager] Cannot claim rewards for uncompleted tier");
                return false;
            }

            if (isPremium && !hasPremiumPass)
            {
                Debug.LogWarning("[BattlePassManager] Premium pass required");
                return false;
            }

            // Check if already claimed
            var claimedList = isPremium ? claimedPremiumTiers : claimedFreeTiers;
            if (claimedList.Contains(tier))
            {
                Debug.LogWarning($"[BattlePassManager] Tier {tier} rewards already claimed");
                return false;
            }

            // Get tier rewards
            BattlePassTier tierData = currentSeason.GetTierRewards(tier, isPremium);
            if (tierData == null || tierData.rewards == null || tierData.rewards.Length == 0)
            {
                Debug.LogWarning($"[BattlePassManager] No rewards for tier {tier}");
                return false;
            }

            // Award all rewards
            foreach (var reward in tierData.rewards)
            {
                AwardReward(reward);
            }

            // Mark as claimed
            claimedList.Add(tier);
            SaveProgress();

            Debug.Log($"[BattlePassManager] Claimed tier {tier} {(isPremium ? "premium" : "free")} rewards");
            return true;
        }

        private void AwardReward(BattlePassReward reward)
        {
            switch (reward.rewardType)
            {
                case RewardType.Currency:
                    // TODO: Add currency
                    Debug.Log($"[BattlePassManager] Awarded {reward.quantity} currency");
                    break;

                case RewardType.PremiumCurrency:
                    // TODO: Add premium currency
                    Debug.Log($"[BattlePassManager] Awarded {reward.quantity} premium currency");
                    break;

                case RewardType.XPBoost:
                    // TODO: Apply XP boost
                    Debug.Log($"[BattlePassManager] Awarded {reward.quantity}% XP boost");
                    break;

                case RewardType.WeaponSkin:
                case RewardType.CharacterSkin:
                case RewardType.Emote:
                case RewardType.Banner:
                case RewardType.Title:
                case RewardType.Spray:
                case RewardType.Charm:
                case RewardType.Finisher:
                    // TODO: Unlock cosmetic
                    Debug.Log($"[BattlePassManager] Unlocked {reward.rewardType}: {reward.rewardName}");
                    break;

                case RewardType.Weapon:
                case RewardType.Perk:
                    // TODO: Unlock weapon/perk
                    var progression = FindObjectOfType<Player.PlayerProgression>();
                    if (progression != null)
                    {
                        progression.UnlockItem(reward.itemID);
                    }
                    break;

                case RewardType.TierSkip:
                    SkipTiers(reward.quantity);
                    break;

                case RewardType.LootBox:
                    // TODO: Add loot box to inventory
                    Debug.Log($"[BattlePassManager] Awarded {reward.quantity} loot box(es)");
                    break;
            }

            OnRewardClaimed?.Invoke(reward);
        }

        /// <summary>
        /// Claims all available rewards up to current tier
        /// </summary>
        public void ClaimAllRewards()
        {
            for (int i = 1; i <= currentTier; i++)
            {
                if (!claimedFreeTiers.Contains(i))
                {
                    ClaimTierRewards(i, false);
                }

                if (hasPremiumPass && !claimedPremiumTiers.Contains(i))
                {
                    ClaimTierRewards(i, true);
                }
            }
        }

        /// <summary>
        /// Gets all unclaimed rewards up to current tier
        /// </summary>
        public List<BattlePassReward> GetUnclaimedRewards()
        {
            List<BattlePassReward> unclaimed = new List<BattlePassReward>();

            if (currentSeason == null)
                return unclaimed;

            for (int i = 1; i <= currentTier; i++)
            {
                // Free tier
                if (!claimedFreeTiers.Contains(i))
                {
                    var tierData = currentSeason.GetTierRewards(i, false);
                    if (tierData != null && tierData.rewards != null)
                    {
                        unclaimed.AddRange(tierData.rewards);
                    }
                }

                // Premium tier
                if (hasPremiumPass && !claimedPremiumTiers.Contains(i))
                {
                    var tierData = currentSeason.GetTierRewards(i, true);
                    if (tierData != null && tierData.rewards != null)
                    {
                        unclaimed.AddRange(tierData.rewards);
                    }
                }
            }

            return unclaimed;
        }

        #endregion

        #region Premium Pass

        /// <summary>
        /// Purchases the premium battle pass
        /// </summary>
        public bool PurchasePremiumPass()
        {
            if (currentSeason == null)
            {
                Debug.LogWarning("[BattlePassManager] No active season");
                return false;
            }

            if (hasPremiumPass)
            {
                Debug.LogWarning("[BattlePassManager] Premium pass already owned");
                return false;
            }

            // TODO: Check if player has enough premium currency
            // TODO: Deduct premium currency

            hasPremiumPass = true;
            OnPremiumPassPurchased?.Invoke();
            SaveProgress();

            Debug.Log("[BattlePassManager] Premium pass purchased!");

            // Auto-claim all previously completed premium rewards
            for (int i = 1; i <= currentTier; i++)
            {
                if (!claimedPremiumTiers.Contains(i))
                {
                    ClaimTierRewards(i, true);
                }
            }

            return true;
        }

        /// <summary>
        /// Purchases the premium pass bundle (includes tier skips)
        /// </summary>
        public bool PurchasePremiumBundle()
        {
            if (currentSeason == null || !currentSeason.hasBundleOption)
                return false;

            // TODO: Check if player has enough premium currency
            // TODO: Deduct bundle price

            if (!hasPremiumPass)
            {
                hasPremiumPass = true;
                OnPremiumPassPurchased?.Invoke();
            }

            // Skip tiers
            SkipTiers(currentSeason.bundleTierSkips);

            SaveProgress();

            Debug.Log($"[BattlePassManager] Premium bundle purchased! (+{currentSeason.bundleTierSkips} tiers)");
            return true;
        }

        #endregion

        #region Season Management

        /// <summary>
        /// Starts a new season
        /// </summary>
        public void StartNewSeason(BattlePassData newSeason)
        {
            if (newSeason == null)
                return;

            // Save previous season rewards before resetting
            // TODO: Archive previous season data

            // Reset progression
            currentSeason = newSeason;
            currentTier = 0;
            currentXP = 0;
            hasPremiumPass = false;
            claimedFreeTiers.Clear();
            claimedPremiumTiers.Clear();

            SaveProgress();

            Debug.Log($"[BattlePassManager] Started new season: {newSeason.seasonName}");
        }

        /// <summary>
        /// Gets days remaining in current season
        /// </summary>
        public int GetDaysRemaining()
        {
            return currentSeason != null ? currentSeason.GetDaysRemaining() : 0;
        }

        #endregion

        #region Save/Load

        private void SaveProgress()
        {
            if (currentSeason == null)
                return;

            PlayerPrefs.SetString("BattlePass_Season", currentSeason.seasonName);
            PlayerPrefs.SetInt("BattlePass_Tier", currentTier);
            PlayerPrefs.SetInt("BattlePass_XP", currentXP);
            PlayerPrefs.SetInt("BattlePass_Premium", hasPremiumPass ? 1 : 0);

            // Save claimed tiers
            PlayerPrefs.SetString("BattlePass_ClaimedFree", string.Join(",", claimedFreeTiers));
            PlayerPrefs.SetString("BattlePass_ClaimedPremium", string.Join(",", claimedPremiumTiers));

            PlayerPrefs.Save();

            Debug.Log("[BattlePassManager] Progress saved");
        }

        private void LoadProgress()
        {
            if (currentSeason == null)
                return;

            string savedSeason = PlayerPrefs.GetString("BattlePass_Season", "");

            // Only load if same season
            if (savedSeason == currentSeason.seasonName)
            {
                currentTier = PlayerPrefs.GetInt("BattlePass_Tier", 0);
                currentXP = PlayerPrefs.GetInt("BattlePass_XP", 0);
                hasPremiumPass = PlayerPrefs.GetInt("BattlePass_Premium", 0) == 1;

                // Load claimed tiers
                string claimedFreeStr = PlayerPrefs.GetString("BattlePass_ClaimedFree", "");
                if (!string.IsNullOrEmpty(claimedFreeStr))
                {
                    claimedFreeTiers = claimedFreeStr.Split(',').Select(int.Parse).ToList();
                }

                string claimedPremiumStr = PlayerPrefs.GetString("BattlePass_ClaimedPremium", "");
                if (!string.IsNullOrEmpty(claimedPremiumStr))
                {
                    claimedPremiumTiers = claimedPremiumStr.Split(',').Select(int.Parse).ToList();
                }

                Debug.Log($"[BattlePassManager] Loaded progress - Tier {currentTier}, {(hasPremiumPass ? "Premium" : "Free")}");
            }
            else
            {
                Debug.Log("[BattlePassManager] New season or first time - starting fresh");
            }
        }

        #endregion
    }
}
