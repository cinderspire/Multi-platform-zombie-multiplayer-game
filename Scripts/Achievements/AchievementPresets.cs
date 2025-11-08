using UnityEngine;

namespace DeadFrontier.Achievements
{
    /// <summary>
    /// Pre-configured achievement templates
    /// </summary>
    public static class AchievementPresets
    {
        #region Combat Achievements

        public static readonly AchievementPreset FirstBlood = new AchievementPreset
        {
            achievementName = "First Blood",
            achievementID = "achievement_first_blood",
            description = "Kill your first zombie",
            category = AchievementCategory.Combat,
            rarity = AchievementRarity.Common,
            achievementType = AchievementType.SingleEvent,
            targetProgress = 1,
            xpReward = 25,
            currencyReward = 10
        };

        public static readonly AchievementPreset ZombieSlayer = new AchievementPreset
        {
            achievementName = "Zombie Slayer",
            achievementID = "achievement_zombie_slayer",
            description = "Kill 100 zombies",
            category = AchievementCategory.Combat,
            rarity = AchievementRarity.Common,
            achievementType = AchievementType.Incremental,
            targetProgress = 100,
            xpReward = 100,
            currencyReward = 50
        };

        public static readonly AchievementPreset ZombieMassacre = new AchievementPreset
        {
            achievementName = "Zombie Massacre",
            achievementID = "achievement_zombie_massacre",
            description = "Kill 1,000 zombies",
            category = AchievementCategory.Combat,
            rarity = AchievementRarity.Rare,
            achievementType = AchievementType.Incremental,
            targetProgress = 1000,
            xpReward = 500,
            currencyReward = 250
        };

        public static readonly AchievementPreset ZombieExterminator = new AchievementPreset
        {
            achievementName = "Zombie Exterminator",
            achievementID = "achievement_zombie_exterminator",
            description = "Kill 10,000 zombies",
            category = AchievementCategory.Combat,
            rarity = AchievementRarity.Legendary,
            achievementType = AchievementType.Incremental,
            targetProgress = 10000,
            xpReward = 5000,
            currencyReward = 2500,
            titleReward = "The Exterminator"
        };

        public static readonly AchievementPreset HeadshotMaster = new AchievementPreset
        {
            achievementName = "Headshot Master",
            achievementID = "achievement_headshot_master",
            description = "Get 500 headshot kills",
            category = AchievementCategory.Combat,
            rarity = AchievementRarity.Epic,
            achievementType = AchievementType.Incremental,
            targetProgress = 500,
            xpReward = 1000,
            currencyReward = 500
        };

        public static readonly AchievementPreset OneShotOneKill = new AchievementPreset
        {
            achievementName = "One Shot, One Kill",
            achievementID = "achievement_oneshot_onekill",
            description = "Get 10 consecutive headshot kills without missing",
            category = AchievementCategory.Combat,
            rarity = AchievementRarity.Epic,
            achievementType = AchievementType.Conditional,
            targetProgress = 1,
            xpReward = 500,
            currencyReward = 250
        };

        #endregion

        #region Survival Achievements

        public static readonly AchievementPreset FirstExtraction = new AchievementPreset
        {
            achievementName = "First Extraction",
            achievementID = "achievement_first_extraction",
            description = "Successfully extract for the first time",
            category = AchievementCategory.Survival,
            rarity = AchievementRarity.Common,
            achievementType = AchievementType.SingleEvent,
            targetProgress = 1,
            xpReward = 50,
            currencyReward = 25
        };

        public static readonly AchievementPreset Survivor = new AchievementPreset
        {
            achievementName = "Survivor",
            achievementID = "achievement_survivor",
            description = "Successfully extract 50 times",
            category = AchievementCategory.Survival,
            rarity = AchievementRarity.Rare,
            achievementType = AchievementType.Incremental,
            targetProgress = 50,
            xpReward = 500,
            currencyReward = 250
        };

        public static readonly AchievementPreset Untouchable = new AchievementPreset
        {
            achievementName = "Untouchable",
            achievementID = "achievement_untouchable",
            description = "Extract without taking any damage",
            category = AchievementCategory.Survival,
            rarity = AchievementRarity.Epic,
            achievementType = AchievementType.Conditional,
            targetProgress = 1,
            xpReward = 250,
            currencyReward = 125
        };

        public static readonly AchievementPreset CloseCall = new AchievementPreset
        {
            achievementName = "Close Call",
            achievementID = "achievement_close_call",
            description = "Extract with less than 10 HP remaining",
            category = AchievementCategory.Survival,
            rarity = AchievementRarity.Uncommon,
            achievementType = AchievementType.SingleEvent,
            targetProgress = 1,
            xpReward = 75,
            currencyReward = 35
        };

        public static readonly AchievementPreset SoloSurvivor = new AchievementPreset
        {
            achievementName = "Solo Survivor",
            achievementID = "achievement_solo_survivor",
            description = "Extract alone (last player alive)",
            category = AchievementCategory.Survival,
            rarity = AchievementRarity.Rare,
            achievementType = AchievementType.SingleEvent,
            targetProgress = 1,
            xpReward = 300,
            currencyReward = 150
        };

        #endregion

        #region Mastery Achievements

        public static readonly AchievementPreset WeaponMaster = new AchievementPreset
        {
            achievementName = "Weapon Master",
            achievementID = "achievement_weapon_master",
            description = "Get 100 kills with every weapon type",
            category = AchievementCategory.Mastery,
            rarity = AchievementRarity.Legendary,
            achievementType = AchievementType.Incremental,
            targetProgress = 1,
            xpReward = 2000,
            currencyReward = 1000,
            titleReward = "Weapon Master"
        };

        public static readonly AchievementPreset SniperElite = new AchievementPreset
        {
            achievementName = "Sniper Elite",
            achievementID = "achievement_sniper_elite",
            description = "Get 500 kills with sniper rifles",
            category = AchievementCategory.Mastery,
            rarity = AchievementRarity.Rare,
            achievementType = AchievementType.Incremental,
            targetProgress = 500,
            xpReward = 400,
            currencyReward = 200
        };

        public static readonly AchievementPreset ShotgunSpecialist = new AchievementPreset
        {
            achievementName = "Shotgun Specialist",
            achievementID = "achievement_shotgun_specialist",
            description = "Get 500 kills with shotguns",
            category = AchievementCategory.Mastery,
            rarity = AchievementRarity.Rare,
            achievementType = AchievementType.Incremental,
            targetProgress = 500,
            xpReward = 400,
            currencyReward = 200
        };

        #endregion

        #region Collection Achievements

        public static readonly AchievementPreset TreasureHunter = new AchievementPreset
        {
            achievementName = "Treasure Hunter",
            achievementID = "achievement_treasure_hunter",
            description = "Loot 1,000 items",
            category = AchievementCategory.Collection,
            rarity = AchievementRarity.Uncommon,
            achievementType = AchievementType.Incremental,
            targetProgress = 1000,
            xpReward = 300,
            currencyReward = 150
        };

        public static readonly AchievementPreset RareCollector = new AchievementPreset
        {
            achievementName = "Rare Collector",
            achievementID = "achievement_rare_collector",
            description = "Loot 100 Rare or higher rarity items",
            category = AchievementCategory.Collection,
            rarity = AchievementRarity.Rare,
            achievementType = AchievementType.Incremental,
            targetProgress = 100,
            xpReward = 500,
            currencyReward = 250
        };

        public static readonly AchievementPreset Millionaire = new AchievementPreset
        {
            achievementName = "Millionaire",
            achievementID = "achievement_millionaire",
            description = "Earn 1,000,000 currency",
            category = AchievementCategory.Collection,
            rarity = AchievementRarity.Epic,
            achievementType = AchievementType.Incremental,
            targetProgress = 1000000,
            xpReward = 1000,
            currencyReward = 500,
            titleReward = "Millionaire"
        };

        #endregion

        #region Challenge Achievements

        public static readonly AchievementPreset Challenger = new AchievementPreset
        {
            achievementName = "Challenger",
            achievementID = "achievement_challenger",
            description = "Complete 50 daily challenges",
            category = AchievementCategory.Challenge,
            rarity = AchievementRarity.Uncommon,
            achievementType = AchievementType.Incremental,
            targetProgress = 50,
            xpReward = 300,
            currencyReward = 150
        };

        public static readonly AchievementPreset WeeklyWarrior = new AchievementPreset
        {
            achievementName = "Weekly Warrior",
            achievementID = "achievement_weekly_warrior",
            description = "Complete 20 weekly challenges",
            category = AchievementCategory.Challenge,
            rarity = AchievementRarity.Rare,
            achievementType = AchievementType.Incremental,
            targetProgress = 20,
            xpReward = 500,
            currencyReward = 250
        };

        #endregion

        #region Social Achievements

        public static readonly AchievementPreset Teamplayer = new AchievementPreset
        {
            achievementName = "Teamplayer",
            achievementID = "achievement_teamplayer",
            description = "Extract with a full squad 10 times",
            category = AchievementCategory.Social,
            rarity = AchievementRarity.Uncommon,
            achievementType = AchievementType.Incremental,
            targetProgress = 10,
            xpReward = 200,
            currencyReward = 100
        };

        public static readonly AchievementPreset Medic = new AchievementPreset
        {
            achievementName = "Medic",
            achievementID = "achievement_medic",
            description = "Revive 100 teammates",
            category = AchievementCategory.Social,
            rarity = AchievementRarity.Rare,
            achievementType = AchievementType.Incremental,
            targetProgress = 100,
            xpReward = 400,
            currencyReward = 200
        };

        #endregion

        #region Secret Achievements

        public static readonly AchievementPreset TheChosen = new AchievementPreset
        {
            achievementName = "The Chosen One",
            achievementID = "achievement_the_chosen",
            description = "Survive a horde event solo",
            category = AchievementCategory.Secret,
            rarity = AchievementRarity.Legendary,
            achievementType = AchievementType.Conditional,
            targetProgress = 1,
            isHidden = true,
            hiddenDescription = "Complete a secret challenge...",
            xpReward = 1000,
            currencyReward = 500,
            titleReward = "The Chosen One"
        };

        public static readonly AchievementPreset BountyHunter = new AchievementPreset
        {
            achievementName = "Bounty Hunter",
            achievementID = "achievement_bounty_hunter",
            description = "Kill 10 players in a single match",
            category = AchievementCategory.Secret,
            rarity = AchievementRarity.Epic,
            achievementType = AchievementType.Conditional,
            targetProgress = 1,
            isHidden = true,
            hiddenDescription = "Achieve a combat feat...",
            xpReward = 750,
            currencyReward = 375
        };

        public static readonly AchievementPreset GhostOperator = new AchievementPreset
        {
            achievementName = "Ghost Operator",
            achievementID = "achievement_ghost_operator",
            description = "Extract without alerting any zombies",
            category = AchievementCategory.Secret,
            rarity = AchievementRarity.Epic,
            achievementType = AchievementType.Conditional,
            targetProgress = 1,
            isHidden = true,
            hiddenDescription = "Complete a stealth challenge...",
            xpReward = 500,
            currencyReward = 250
        };

        #endregion

        #region Data Structure

        [System.Serializable]
        public struct AchievementPreset
        {
            public string achievementName;
            public string achievementID;
            public string description;
            public AchievementCategory category;
            public AchievementRarity rarity;
            public AchievementType achievementType;
            public int targetProgress;
            public bool isHidden;
            public string hiddenDescription;
            public int xpReward;
            public int currencyReward;
            public string titleReward;
        }

        #endregion
    }
}
