using UnityEngine;

namespace DeadFrontier.Events
{
    /// <summary>
    /// ScriptableObject defining seasonal and special event configurations.
    /// </summary>
    [CreateAssetMenu(fileName = "New Event", menuName = "Dead Frontier/Events/Event Data")]
    public class EventDataSO : ScriptableObject
    {
        [Header("Identification")]
        public string eventId;
        public string eventName;
        [TextArea(3, 5)]
        public string description;
        public EventType eventType;
        public EventTheme theme;

        [Header("Visuals")]
        public Sprite eventBanner;
        public Sprite eventIcon;
        public Color eventColor;
        public Texture2D eventBackground;
        public Material eventSkybox;

        [Header("Timing")]
        public System.DateTime startDate;
        public System.DateTime endDate;
        public bool isRecurring;
        public RecurrenceType recurrenceType;

        [Header("Content")]
        public EventMap[] eventMaps;
        public EventZombie[] eventZombies;
        public EventItem[] eventItems;
        public EventQuest[] eventQuests;
        public EventChallenge[] eventChallenges;

        [Header("Rewards")]
        public EventReward[] milestoneRewards;
        public EventReward[] rankRewards;
        public EventReward[] completionRewards;
        public string eventCurrencyId;
        public Sprite eventCurrencyIcon;

        [Header("Shop")]
        public EventShopItem[] eventShopItems;
        public bool hasEventShop = true;

        [Header("Modifiers")]
        public EventModifier[] activeModifiers;
        public float xpMultiplier = 1f;
        public float lootMultiplier = 1f;
        public float currencyMultiplier = 1f;

        [Header("Leaderboard")]
        public bool hasLeaderboard;
        public LeaderboardConfig leaderboardConfig;

        [Header("Audio")]
        public AudioClip eventMusic;
        public AudioClip[] eventAmbience;
        public AudioClip eventStartJingle;
        public AudioClip eventEndJingle;

        [Header("UI")]
        public GameObject eventUIOverlayPrefab;
        public string announcementText;
        public bool showCountdown;
    }

    [System.Serializable]
    public class EventMap
    {
        public string mapId;
        public string eventMapVariant;
        public MapModification[] modifications;
        public bool isExclusive;
    }

    [System.Serializable]
    public class MapModification
    {
        public MapModificationType modificationType;
        public string targetId;
        public float modificationValue;
    }

    [System.Serializable]
    public class EventZombie
    {
        public string zombieId;
        public string eventVariantId;
        public Material eventMaterial;
        public float spawnMultiplier = 1f;
        public bool isEventExclusive;
        public LootModification[] lootModifications;
    }

    [System.Serializable]
    public class LootModification
    {
        public string itemId;
        public float dropChanceMultiplier;
        public bool isEventExclusive;
    }

    [System.Serializable]
    public class EventItem
    {
        public string itemId;
        public string eventVariantId;
        public bool isEventCurrency;
        public float spawnRate;
        public bool disappearsAfterEvent;
    }

    [System.Serializable]
    public class EventQuest
    {
        public string questId;
        public int eventPointsReward;
        public bool isRequired;
        public int order;
    }

    [System.Serializable]
    public class EventChallenge
    {
        public string challengeId;
        public string challengeName;
        [TextArea(2, 3)]
        public string description;
        public EventChallengeType challengeType;
        public int targetAmount;
        public int eventPointsReward;
        public EventReward[] completionRewards;
    }

    [System.Serializable]
    public class EventReward
    {
        public string rewardId;
        public string rewardName;
        public EventRewardType rewardType;
        public Sprite rewardIcon;
        public int amount;
        public string itemId;
        public int requiredPoints;
        public int requiredRank;
        public Gameplay.ItemRarity rarity;
        public bool isExclusive;
    }

    [System.Serializable]
    public class EventShopItem
    {
        public string itemId;
        public int eventCurrencyCost;
        public int purchaseLimit;
        public bool isLimited;
        public int stockAmount;
    }

    [System.Serializable]
    public class EventModifier
    {
        public EventModifierType modifierType;
        public float value;
        public bool isGlobal;
        public string targetId;
    }

    [System.Serializable]
    public class LeaderboardConfig
    {
        public string leaderboardId;
        public LeaderboardScoreType scoreType;
        public int rewardTiers;
        public int[] tierThresholds;
    }

    public enum EventType
    {
        Seasonal,
        Holiday,
        Anniversary,
        Collaboration,
        LimitedTime,
        Community,
        Competitive,
        Story
    }

    public enum EventTheme
    {
        Halloween,
        Christmas,
        Summer,
        Valentine,
        Easter,
        StPatricks,
        LunarNewYear,
        Apocalypse,
        Military,
        Sci_Fi,
        Horror,
        Custom
    }

    public enum RecurrenceType
    {
        None,
        Weekly,
        Monthly,
        Yearly
    }

    public enum MapModificationType
    {
        Lighting,
        Weather,
        Decoration,
        SpawnRate,
        LootTable,
        Objectives
    }

    public enum EventChallengeType
    {
        KillEventZombies,
        CollectEventItems,
        CompleteEventQuests,
        EarnEventPoints,
        ExtractWithEventLoot,
        ParticipateInMatches,
        WinMatches,
        Custom
    }

    public enum EventRewardType
    {
        Currency,
        EventCurrency,
        Item,
        Cosmetic,
        Title,
        Badge,
        Emote,
        XPBoost,
        LootBox
    }

    public enum EventModifierType
    {
        XPBoost,
        LootBoost,
        CurrencyBoost,
        ZombieHealth,
        ZombieDamage,
        ZombieSpeed,
        SpawnRate,
        ExtractTime
    }

    public enum LeaderboardScoreType
    {
        EventPoints,
        Kills,
        Extractions,
        ItemsCollected,
        QuestsCompleted
    }
}
