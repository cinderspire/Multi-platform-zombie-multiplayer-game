using UnityEngine;

namespace DeadFrontier.BattlePass
{
    /// <summary>
    /// ScriptableObject defining battle pass season data and rewards.
    /// </summary>
    [CreateAssetMenu(fileName = "New Battle Pass", menuName = "Dead Frontier/BattlePass/Season Data")]
    public class BattlePassDataSO : ScriptableObject
    {
        [Header("Season Info")]
        public string seasonId;
        public string seasonName;
        [TextArea(3, 5)]
        public string seasonDescription;
        public int seasonNumber;
        public string themeId;

        [Header("Visuals")]
        public Sprite seasonBanner;
        public Sprite seasonIcon;
        public Color seasonColor;
        public Texture2D seasonBackground;

        [Header("Timing")]
        public System.DateTime startDate;
        public System.DateTime endDate;
        public int totalDays = 90;

        [Header("Tiers")]
        public int totalTiers = 100;
        public int xpPerTier = 1000;
        public float xpScaling = 1.05f; // Each tier requires 5% more XP
        public int maxBonusTiers = 50;

        [Header("Rewards")]
        public BattlePassTier[] tiers;
        public BattlePassReward[] bonusTierRewards;

        [Header("Premium")]
        public int premiumCost = 950; // In premium currency
        public BattlePassReward[] premiumExclusiveRewards;
        public float premiumXPBoost = 1.1f;

        [Header("Challenges")]
        public BattlePassChallenge[] dailyChallenges;
        public BattlePassChallenge[] weeklyChallenges;
        public BattlePassChallenge[] seasonChallenges;

        [Header("Milestones")]
        public SeasonMilestone[] milestones;

        [Header("Bundle")]
        public BattlePassBundle[] bundles;
    }

    [System.Serializable]
    public class BattlePassTier
    {
        public int tierNumber;
        public int requiredXP;
        public BattlePassReward freeReward;
        public BattlePassReward premiumReward;
        public bool isMilestone;
    }

    [System.Serializable]
    public class BattlePassReward
    {
        public string rewardId;
        public string rewardName;
        public BattlePassRewardType rewardType;
        public Sprite rewardIcon;
        public Gameplay.ItemRarity rarity;
        public string itemId;
        public int quantity = 1;
        public string cosmeticId;
        public int currencyAmount;
        public bool isExclusive;
        public bool isAnimated;
        public GameObject rewardPreviewPrefab;
    }

    [System.Serializable]
    public class BattlePassChallenge
    {
        public string challengeId;
        public string challengeName;
        [TextArea(2, 3)]
        public string description;
        public ChallengeType challengeType;
        public string targetId;
        public int targetAmount;
        public int xpReward;
        public bool isHidden;
        public int weekNumber; // For weekly challenges
    }

    [System.Serializable]
    public class SeasonMilestone
    {
        public string milestoneId;
        public string milestoneName;
        public int requiredTier;
        public BattlePassReward[] rewards;
        public bool unlocksBadge;
        public string badgeId;
    }

    [System.Serializable]
    public class BattlePassBundle
    {
        public string bundleId;
        public string bundleName;
        public BundleType bundleType;
        public int tiersIncluded;
        public int premiumCurrencyCost;
        public float discount;
        public BattlePassReward[] bonusRewards;
    }

    public enum BattlePassRewardType
    {
        Currency,
        Item,
        Weapon,
        WeaponSkin,
        CharacterSkin,
        Emote,
        Spray,
        Banner,
        Title,
        Avatar,
        LoadingScreen,
        Music,
        XPBoost,
        LootBox,
        Recipe
    }

    public enum ChallengeType
    {
        KillZombies,
        KillSpecificZombie,
        HeadshotKills,
        ExtractSuccessfully,
        CollectItems,
        CollectRareItems,
        DealDamage,
        HealDamage,
        ReviveTeammates,
        CompleteQuests,
        CraftItems,
        TravelDistance,
        SurviveTime,
        WinMatches,
        PlayWithFriends,
        UseAbility,
        DiscoverLocations
    }

    public enum BundleType
    {
        Standard,
        Premium,
        Ultimate,
        Starter,
        Catchup
    }
}
