using UnityEngine;

namespace DeadFrontier.Achievements
{
    /// <summary>
    /// ScriptableObject defining achievement data and tracking requirements.
    /// </summary>
    [CreateAssetMenu(fileName = "New Achievement", menuName = "Dead Frontier/Achievements/Achievement Data")]
    public class AchievementDataSO : ScriptableObject
    {
        [Header("Identification")]
        public string achievementId;
        public string achievementName;
        [TextArea(3, 5)]
        public string description;
        [TextArea(2, 3)]
        public string unlockHint;
        public AchievementCategory category;
        public AchievementTier tier;

        [Header("Visuals")]
        public Sprite achievementIcon;
        public Sprite lockedIcon;
        public Color tierColor;
        public GameObject unlockEffectPrefab;

        [Header("Requirements")]
        public AchievementRequirement[] requirements;
        public bool requiresAllConditions = true;

        [Header("Progress")]
        public bool isProgressBased;
        public int targetProgress = 1;
        public string progressFormat = "{0}/{1}";
        public bool showProgressBar = true;

        [Header("Rewards")]
        public AchievementReward[] rewards;
        public int xpReward = 0;
        public int gamerscore = 10; // For platform integration

        [Header("Visibility")]
        public bool isHidden;
        public bool revealOnProgress;
        public float revealAtPercent = 0.5f;

        [Header("Platform")]
        public bool syncToPlatform = true;
        public string steamAchievementId;
        public string playstationTrophyId;
        public string xboxAchievementId;

        [Header("Meta")]
        public int sortOrder = 0;
        public bool isLegacy;
        public string replacedByAchievementId;
    }

    [System.Serializable]
    public class AchievementRequirement
    {
        public AchievementRequirementType requirementType;
        public string targetId;
        public int targetAmount = 1;
        public ComparisonType comparison = ComparisonType.GreaterOrEqual;
        public bool mustBeInSingleMatch;
        public bool mustBeConsecutive;
        public float timeLimit = 0f;
        public MapCondition mapCondition;
        public DifficultyCondition difficultyCondition;
    }

    [System.Serializable]
    public class AchievementReward
    {
        public RewardType rewardType;
        public string rewardId;
        public int amount = 1;
        public Sprite rewardIcon;
        public string rewardName;
    }

    [System.Serializable]
    public class MapCondition
    {
        public bool anyMap = true;
        public string[] specificMapIds;
    }

    [System.Serializable]
    public class DifficultyCondition
    {
        public bool anyDifficulty = true;
        public Maps.MapDifficulty minimumDifficulty;
    }

    public enum AchievementCategory
    {
        Combat,
        Survival,
        Extraction,
        Progression,
        Social,
        Exploration,
        Collection,
        Challenge,
        Secret,
        Seasonal
    }

    public enum AchievementTier
    {
        Bronze,
        Silver,
        Gold,
        Platinum,
        Diamond,
        Legendary
    }

    public enum AchievementRequirementType
    {
        // Combat
        KillZombies,
        KillZombieType,
        HeadshotKills,
        MeleeKills,
        ExplosiveKills,
        KillStreak,
        KillsWithWeapon,
        KillsWithWeaponType,
        KillBoss,
        DealDamage,
        CriticalHits,

        // Survival
        SurviveMinutes,
        SurviveWithoutDamage,
        HealDamage,
        UseHealingItems,
        ReviveTeammates,
        SurviveNearDeath,
        BlockDamage,

        // Extraction
        ExtractSuccessfully,
        ExtractWithLoot,
        ExtractWithMinValue,
        ExtractLastSecond,
        ExtractFullSquad,

        // Progression
        ReachLevel,
        ReachPrestige,
        UnlockPerks,
        MasterWeapon,
        CompleteBattlePass,
        CompleteAllChallenges,

        // Social
        PlayWithFriends,
        JoinClan,
        WinClanWar,
        TradeItems,
        GiftItems,
        HelpNewPlayers,

        // Exploration
        DiscoverLocations,
        VisitAllMaps,
        FindSecrets,
        OpenContainers,
        TravelDistance,

        // Collection
        CollectItems,
        CollectRarity,
        OwnWeapons,
        OwnCosmetics,
        CompleteCollection,

        // Challenge
        CompleteQuest,
        CompleteQuestType,
        CompleteWithCondition,
        SpeedRun,
        PerfectRun
    }

    public enum RewardType
    {
        Currency,
        Item,
        Cosmetic,
        Title,
        Badge,
        Avatar,
        Banner,
        XPBoost,
        UnlockFeature
    }

    public enum ComparisonType
    {
        Equal,
        GreaterOrEqual,
        LessOrEqual,
        GreaterThan,
        LessThan
    }
}
