using UnityEngine;

namespace DeadFrontier.Achievements
{
    /// <summary>
    /// ScriptableObject defining an achievement
    /// </summary>
    [CreateAssetMenu(fileName = "New Achievement", menuName = "DeadFrontier/Achievement Data")]
    public class AchievementData : ScriptableObject
    {
        [Header("Basic Info")]
        public string achievementName = "New Achievement";
        public string achievementID;

        [TextArea(3, 5)]
        public string description = "Achievement description";

        public Sprite icon;
        public AchievementCategory category = AchievementCategory.Combat;
        public AchievementRarity rarity = AchievementRarity.Common;

        [Header("Hidden Achievement")]
        [Tooltip("Hidden until unlocked")]
        public bool isHidden = false;

        [Tooltip("Shown when achievement is hidden")]
        public string hiddenDescription = "???";

        [Header("Progress")]
        public AchievementType achievementType = AchievementType.SingleEvent;

        [Tooltip("Required progress to unlock (for incremental achievements)")]
        public int targetProgress = 1;

        [Header("Requirements")]
        public string[] prerequisiteAchievements; // Must unlock these first

        [Header("Rewards")]
        public int xpReward = 50;
        public int currencyReward = 25;
        public string[] itemRewards; // Unlocked items/cosmetics
        public string titleReward; // Player title
        public Sprite bannerReward; // Profile banner

        [Header("Rarity Bonus")]
        [Tooltip("Bonus multiplier based on rarity")]
        public float rarityMultiplier = 1f;

        /// <summary>
        /// Gets display name (hidden or real)
        /// </summary>
        public string GetDisplayName(bool isUnlocked)
        {
            if (isHidden && !isUnlocked)
                return "Hidden Achievement";

            return achievementName;
        }

        /// <summary>
        /// Gets display description (hidden or real)
        /// </summary>
        public string GetDisplayDescription(bool isUnlocked)
        {
            if (isHidden && !isUnlocked)
                return hiddenDescription;

            return description;
        }

        /// <summary>
        /// Gets total XP reward with rarity bonus
        /// </summary>
        public int GetTotalXPReward()
        {
            return Mathf.RoundToInt(xpReward * rarityMultiplier);
        }

        /// <summary>
        /// Gets total currency reward with rarity bonus
        /// </summary>
        public int GetTotalCurrencyReward()
        {
            return Mathf.RoundToInt(currencyReward * rarityMultiplier);
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(achievementID))
            {
                achievementID = System.Guid.NewGuid().ToString();
            }

            // Set rarity multiplier based on rarity
            rarityMultiplier = rarity switch
            {
                AchievementRarity.Common => 1f,
                AchievementRarity.Uncommon => 1.5f,
                AchievementRarity.Rare => 2f,
                AchievementRarity.Epic => 3f,
                AchievementRarity.Legendary => 5f,
                _ => 1f
            };
        }
    }

    #region Enums

    public enum AchievementCategory
    {
        Combat,         // Kill-related achievements
        Survival,       // Extraction/survival achievements
        Exploration,    // Map/loot achievements
        Mastery,        // Weapon/perk mastery
        Social,         // Team/multiplayer achievements
        Collection,     // Collectibles/unlocks
        Challenge,      // Difficulty-based achievements
        Secret          // Hidden/easter egg achievements
    }

    public enum AchievementRarity
    {
        Common,         // Easy to get
        Uncommon,       // Moderate difficulty
        Rare,           // Challenging
        Epic,           // Very challenging
        Legendary       // Extremely rare/difficult
    }

    public enum AchievementType
    {
        SingleEvent,    // Unlock instantly (e.g., "Extract once")
        Incremental,    // Track progress (e.g., "Kill 1000 zombies")
        Conditional     // Specific conditions (e.g., "Extract without taking damage")
    }

    #endregion
}
