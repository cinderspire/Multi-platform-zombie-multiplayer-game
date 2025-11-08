using UnityEngine;
using System.Collections.Generic;

namespace DeadFrontier.Progression.BattlePass
{
    /// <summary>
    /// ScriptableObject defining a seasonal battle pass
    /// </summary>
    [CreateAssetMenu(fileName = "New Battle Pass", menuName = "DeadFrontier/Battle Pass Data")]
    public class BattlePassData : ScriptableObject
    {
        [Header("Season Info")]
        public string seasonName = "Season 1";
        public int seasonNumber = 1;

        [TextArea(3, 5)]
        public string description = "Season description";

        public Sprite seasonIcon;

        [Header("Timeline")]
        public string startDate; // Format: "YYYY-MM-DD"
        public string endDate;   // Format: "YYYY-MM-DD"

        [Header("Progression")]
        public int maxTier = 100;
        public int xpPerTier = 1000; // Base XP needed per tier
        public AnimationCurve xpCurve; // Optional non-linear progression

        [Header("Tiers")]
        public BattlePassTier[] freeTiers; // Free track rewards
        public BattlePassTier[] premiumTiers; // Premium track rewards

        [Header("Premium Pass")]
        public int premiumPassPrice = 1000; // In premium currency
        public bool hasBundleOption = true;
        public int bundlePrice = 2500; // Includes +25 tier skips
        public int bundleTierSkips = 25;

        /// <summary>
        /// Gets XP needed for a specific tier
        /// </summary>
        public int GetXPForTier(int tier)
        {
            if (tier <= 0 || tier > maxTier)
                return 0;

            if (xpCurve != null && xpCurve.length > 0)
            {
                float t = (float)tier / maxTier;
                return Mathf.RoundToInt(xpCurve.Evaluate(t) * xpPerTier);
            }

            return xpPerTier;
        }

        /// <summary>
        /// Gets total XP needed to reach a tier
        /// </summary>
        public int GetTotalXPForTier(int tier)
        {
            int totalXP = 0;

            for (int i = 1; i <= tier; i++)
            {
                totalXP += GetXPForTier(i);
            }

            return totalXP;
        }

        /// <summary>
        /// Gets rewards for a specific tier
        /// </summary>
        public BattlePassTier GetTierRewards(int tier, bool isPremium)
        {
            var tierArray = isPremium ? premiumTiers : freeTiers;

            if (tier < 0 || tier >= tierArray.Length)
                return null;

            return tierArray[tier];
        }

        /// <summary>
        /// Checks if season is currently active
        /// </summary>
        public bool IsActive()
        {
            if (!System.DateTime.TryParse(startDate, out System.DateTime start))
                return false;

            if (!System.DateTime.TryParse(endDate, out System.DateTime end))
                return false;

            System.DateTime now = System.DateTime.Now;
            return now >= start && now <= end;
        }

        /// <summary>
        /// Gets days remaining in season
        /// </summary>
        public int GetDaysRemaining()
        {
            if (!System.DateTime.TryParse(endDate, out System.DateTime end))
                return 0;

            System.DateTime now = System.DateTime.Now;
            System.TimeSpan remaining = end - now;

            return Mathf.Max(0, (int)remaining.TotalDays);
        }

        private void OnValidate()
        {
            // Ensure tier arrays match max tier
            if (freeTiers == null || freeTiers.Length != maxTier)
            {
                System.Array.Resize(ref freeTiers, maxTier);
            }

            if (premiumTiers == null || premiumTiers.Length != maxTier)
            {
                System.Array.Resize(ref premiumTiers, maxTier);
            }
        }
    }

    /// <summary>
    /// Represents a single battle pass tier
    /// </summary>
    [System.Serializable]
    public class BattlePassTier
    {
        public int tierNumber;
        public BattlePassReward[] rewards;

        /// <summary>
        /// Gets display text for all rewards in this tier
        /// </summary>
        public string GetRewardsSummary()
        {
            if (rewards == null || rewards.Length == 0)
                return "No rewards";

            List<string> rewardStrings = new List<string>();

            foreach (var reward in rewards)
            {
                rewardStrings.Add(reward.GetDisplayText());
            }

            return string.Join(", ", rewardStrings);
        }
    }

    /// <summary>
    /// Represents a single reward
    /// </summary>
    [System.Serializable]
    public class BattlePassReward
    {
        public RewardType rewardType;
        public int quantity = 1;

        [Header("Specific Reward Data")]
        public string itemID; // For weapons, skins, etc.
        public Sprite rewardIcon;

        [TextArea(2, 3)]
        public string rewardName;

        public Items.ItemRarity rarity = Items.ItemRarity.Common;

        /// <summary>
        /// Gets formatted display text
        /// </summary>
        public string GetDisplayText()
        {
            string quantityText = quantity > 1 ? $"{quantity}x " : "";

            return rewardType switch
            {
                RewardType.Currency => $"{quantityText}Coins",
                RewardType.PremiumCurrency => $"{quantityText}Premium Currency",
                RewardType.XPBoost => $"{quantity}% XP Boost",
                RewardType.WeaponSkin => $"Weapon Skin: {rewardName}",
                RewardType.CharacterSkin => $"Character Skin: {rewardName}",
                RewardType.Emote => $"Emote: {rewardName}",
                RewardType.Banner => $"Banner: {rewardName}",
                RewardType.Title => $"Title: {rewardName}",
                RewardType.Weapon => $"Weapon: {rewardName}",
                RewardType.TierSkip => $"{quantity} Tier Skip(s)",
                _ => rewardName
            };
        }

        /// <summary>
        /// Gets rarity color
        /// </summary>
        public Color GetRarityColor()
        {
            return rarity switch
            {
                Items.ItemRarity.Common => new Color(0.7f, 0.7f, 0.7f),
                Items.ItemRarity.Uncommon => new Color(0.2f, 0.8f, 0.2f),
                Items.ItemRarity.Rare => new Color(0.3f, 0.5f, 1f),
                Items.ItemRarity.Epic => new Color(0.7f, 0.3f, 1f),
                Items.ItemRarity.Legendary => new Color(1f, 0.6f, 0f),
                _ => Color.white
            };
        }
    }

    public enum RewardType
    {
        Currency,           // Soft currency
        PremiumCurrency,    // Hard currency
        XPBoost,            // XP boost %
        WeaponSkin,         // Weapon cosmetic
        CharacterSkin,      // Character cosmetic
        Emote,              // Player emote/gesture
        Banner,             // Profile banner
        Title,              // Player title
        Weapon,             // Actual weapon unlock
        Perk,               // Perk unlock
        TierSkip,           // Battle pass tier skip
        LootBox,            // Cosmetic loot box
        Spray,              // Spray paint
        Charm,              // Weapon charm
        Finisher            // Kill finisher animation
    }
}
