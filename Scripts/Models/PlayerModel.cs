using UnityEngine;
using System;
using System.Collections.Generic;

namespace DeadFrontier.Models
{
    /// <summary>
    /// Complete player data model containing all player information.
    /// </summary>
    [Serializable]
    public class PlayerModel
    {
        #region Identity
        public string playerId;
        public string displayName;
        public string avatarId;
        public string bannerId;
        public string titleId;
        public DateTime createdAt;
        public DateTime lastLoginAt;
        public string platformId;
        public string platform; // Steam, PlayStation, Xbox, Mobile
        #endregion

        #region Progression
        public int level;
        public int currentXP;
        public int totalXP;
        public int prestigeLevel;
        public int prestigeTokens;
        public int skillPoints;
        public int perkPoints;
        #endregion

        #region Currency
        public int softCurrency;
        public int hardCurrency;
        public int eventCurrency;
        public int seasonalTokens;
        #endregion

        #region Stats
        public PlayerStats stats;
        public PlayerLifetimeStats lifetimeStats;
        public SeasonStats currentSeasonStats;
        #endregion

        #region Inventory
        public List<InventoryItem> inventory;
        public List<InventoryItem> stash;
        public int maxInventorySlots;
        public int maxStashSlots;
        public float currentWeight;
        public float maxWeight;
        #endregion

        #region Loadout
        public string characterId;
        public LoadoutData[] loadouts;
        public int activeLoadoutIndex;
        public List<string> unlockedWeapons;
        public List<string> unlockedCharacters;
        public List<string> unlockedPerks;
        public List<string> equippedPerks;
        #endregion

        #region Social
        public string clanId;
        public string clanRank;
        public List<string> friendIds;
        public List<string> blockedIds;
        public List<string> pendingFriendRequests;
        public SocialStatus socialStatus;
        #endregion

        #region Battle Pass
        public int battlePassTier;
        public int battlePassXP;
        public bool hasPremiumPass;
        public List<int> claimedFreeRewards;
        public List<int> claimedPremiumRewards;
        #endregion

        #region Achievements
        public List<AchievementProgress> achievementProgress;
        public List<string> completedAchievements;
        public int totalAchievementPoints;
        #endregion

        #region Daily/Weekly
        public DateTime lastDailyReset;
        public DateTime lastWeeklyReset;
        public int dailyLoginStreak;
        public int currentLoginStreak;
        public List<string> claimedDailyRewards;
        public List<ChallengeProgress> dailyChallenges;
        public List<ChallengeProgress> weeklyChallenges;
        #endregion

        #region Quests
        public List<QuestProgress> activeQuests;
        public List<string> completedQuests;
        public Dictionary<string, int> reputationLevels;
        #endregion

        #region Cosmetics
        public List<string> ownedCosmetics;
        public EquippedCosmetics equippedCosmetics;
        #endregion

        #region Companions
        public List<CompanionProgress> companions;
        public string activeCompanionId;
        #endregion

        #region Skills
        public Dictionary<string, int> skillLevels;
        public Dictionary<string, List<string>> unlockedSkillNodes;
        #endregion

        #region Settings
        public PlayerSettings settings;
        #endregion

        public PlayerModel()
        {
            inventory = new List<InventoryItem>();
            stash = new List<InventoryItem>();
            loadouts = new LoadoutData[5];
            unlockedWeapons = new List<string>();
            unlockedCharacters = new List<string>();
            unlockedPerks = new List<string>();
            equippedPerks = new List<string>();
            friendIds = new List<string>();
            blockedIds = new List<string>();
            pendingFriendRequests = new List<string>();
            claimedFreeRewards = new List<int>();
            claimedPremiumRewards = new List<int>();
            achievementProgress = new List<AchievementProgress>();
            completedAchievements = new List<string>();
            claimedDailyRewards = new List<string>();
            dailyChallenges = new List<ChallengeProgress>();
            weeklyChallenges = new List<ChallengeProgress>();
            activeQuests = new List<QuestProgress>();
            completedQuests = new List<string>();
            reputationLevels = new Dictionary<string, int>();
            ownedCosmetics = new List<string>();
            companions = new List<CompanionProgress>();
            skillLevels = new Dictionary<string, int>();
            unlockedSkillNodes = new Dictionary<string, List<string>>();
            stats = new PlayerStats();
            lifetimeStats = new PlayerLifetimeStats();
            currentSeasonStats = new SeasonStats();
            equippedCosmetics = new EquippedCosmetics();
            settings = new PlayerSettings();
        }
    }

    [Serializable]
    public class PlayerStats
    {
        public float maxHealth;
        public float currentHealth;
        public float maxStamina;
        public float currentStamina;
        public float maxArmor;
        public float currentArmor;
        public float moveSpeed;
        public float sprintSpeed;
        public float damageMultiplier;
        public float healingMultiplier;
        public float xpMultiplier;
        public float lootMultiplier;
    }

    [Serializable]
    public class PlayerLifetimeStats
    {
        public int totalKills;
        public int zombieKills;
        public int playerKills;
        public int bossKills;
        public int headshots;
        public int deaths;
        public int revives;
        public int revivesReceived;
        public int matchesPlayed;
        public int matchesWon;
        public int successfulExtractions;
        public int failedExtractions;
        public float totalDamageDealt;
        public float totalDamageTaken;
        public float totalHealing;
        public float totalDistanceTraveled;
        public int itemsCollected;
        public int itemsCrafted;
        public int questsCompleted;
        public float totalPlayTime;
        public int longestKillStreak;
        public float longestSurvivalTime;
        public int highestLevelReached;
    }

    [Serializable]
    public class SeasonStats
    {
        public int seasonNumber;
        public int kills;
        public int deaths;
        public int wins;
        public int extractions;
        public int rank;
        public int rankPoints;
        public float kdRatio;
        public float winRate;
    }

    [Serializable]
    public class InventoryItem
    {
        public string instanceId;
        public string itemId;
        public int quantity;
        public float durability;
        public float maxDurability;
        public int slotIndex;
        public ItemModification[] modifications;
        public bool isEquipped;
        public bool isFavorite;
        public DateTime acquiredAt;
    }

    [Serializable]
    public class ItemModification
    {
        public string modId;
        public string modType;
        public float value;
    }

    [Serializable]
    public class LoadoutData
    {
        public string loadoutName;
        public string characterId;
        public string primaryWeaponId;
        public string[] primaryAttachments;
        public string secondaryWeaponId;
        public string[] secondaryAttachments;
        public string meleeWeaponId;
        public string[] grenadeIds;
        public string[] perkIds;
        public string companionId;
    }

    [Serializable]
    public class AchievementProgress
    {
        public string achievementId;
        public int currentProgress;
        public int targetProgress;
        public bool isCompleted;
        public DateTime completedAt;
    }

    [Serializable]
    public class ChallengeProgress
    {
        public string challengeId;
        public int currentProgress;
        public int targetProgress;
        public bool isCompleted;
        public bool rewardClaimed;
        public DateTime expiresAt;
    }

    [Serializable]
    public class QuestProgress
    {
        public string questId;
        public QuestObjectiveProgress[] objectives;
        public bool isCompleted;
        public DateTime startedAt;
        public DateTime expiresAt;
    }

    [Serializable]
    public class QuestObjectiveProgress
    {
        public string objectiveId;
        public int currentProgress;
        public int targetProgress;
        public bool isCompleted;
    }

    [Serializable]
    public class CompanionProgress
    {
        public string companionId;
        public int bondLevel;
        public int bondXP;
        public float hunger;
        public float happiness;
        public string equippedSkinId;
        public List<string> equippedAccessories;
        public DateTime lastFed;
    }

    [Serializable]
    public class EquippedCosmetics
    {
        public string characterSkinId;
        public string headwearId;
        public string eyewearId;
        public string facewearId;
        public string glovesId;
        public string backpackId;
        public string primaryWeaponSkinId;
        public string secondaryWeaponSkinId;
        public string meleeWeaponSkinId;
        public List<string> emoteIds;
        public List<string> sprayIds;
        public string finisherId;
        public string parachuteId;
    }

    [Serializable]
    public class PlayerSettings
    {
        public float masterVolume;
        public float musicVolume;
        public float sfxVolume;
        public float voiceVolume;
        public float sensitivity;
        public float aimSensitivity;
        public bool invertY;
        public bool toggleAim;
        public bool toggleSprint;
        public bool autoreload;
        public bool showDamageNumbers;
        public bool showMinimap;
        public bool showPings;
        public bool showHitMarkers;
        public string crosshairStyle;
        public Color crosshairColor;
        public bool pushToTalk;
        public string voiceChatKey;
        public string language;
        public bool colorblindMode;
        public int colorblindType;
        public bool subtitles;
        public float subtitleSize;
    }

    public enum SocialStatus
    {
        Online,
        Away,
        Busy,
        InMatch,
        Offline,
        Invisible
    }
}
