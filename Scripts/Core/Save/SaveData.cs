using System;
using System.Collections.Generic;

namespace DeadFrontier.Core.Save
{
    /// <summary>
    /// Main save data container
    /// </summary>
    [Serializable]
    public class SaveData
    {
        // Meta
        public string saveVersion = "1.0.0";
        public long lastSaveTime; // DateTime.Ticks
        public int saveFileIndex = 0; // For multiple save slots

        // Player Progression
        public PlayerProgressionData progression;

        // Loadouts & Perks
        public LoadoutData loadout;

        // Achievements
        public AchievementSaveData achievements;

        // Battle Pass
        public BattlePassSaveData battlePass;

        // Challenges
        public ChallengeSaveData challenges;

        // Settings
        public SettingsData settings;

        // Stats
        public PlayerStatsData stats;

        // Inventory (if persistent across sessions)
        public InventoryData inventory;

        public SaveData()
        {
            lastSaveTime = DateTime.Now.Ticks;
            progression = new PlayerProgressionData();
            loadout = new LoadoutData();
            achievements = new AchievementSaveData();
            battlePass = new BattlePassSaveData();
            challenges = new ChallengeSaveData();
            settings = new SettingsData();
            stats = new PlayerStatsData();
            inventory = new InventoryData();
        }
    }

    #region Player Progression Data

    [Serializable]
    public class PlayerProgressionData
    {
        public int level = 1;
        public int xp = 0;
        public int prestigeLevel = 0;
        public List<string> unlockedItems = new List<string>();
        public Dictionary<string, int> stats = new Dictionary<string, int>();
    }

    #endregion

    #region Loadout Data

    [Serializable]
    public class LoadoutData
    {
        public List<string> primaryWeapons = new List<string>();
        public List<string> secondaryWeapons = new List<string>();
        public List<string> equippedPerks = new List<string>();
        public List<string> equipment = new List<string>();
        public List<SavedLoadoutData> savedLoadouts = new List<SavedLoadoutData>();
    }

    [Serializable]
    public class SavedLoadoutData
    {
        public string name;
        public List<string> primaryWeapons;
        public List<string> secondaryWeapons;
        public List<string> perks;
        public List<string> equipment;
    }

    #endregion

    #region Achievement Data

    [Serializable]
    public class AchievementSaveData
    {
        public List<string> unlockedAchievements = new List<string>();
        public Dictionary<string, int> achievementProgress = new Dictionary<string, int>();
        public Dictionary<string, long> achievementUnlockTimes = new Dictionary<string, long>();
    }

    #endregion

    #region Battle Pass Data

    [Serializable]
    public class BattlePassSaveData
    {
        public string currentSeasonID;
        public int currentTier = 0;
        public int currentXP = 0;
        public bool hasPremiumPass = false;
        public List<int> claimedFreeTiers = new List<int>();
        public List<int> claimedPremiumTiers = new List<int>();
    }

    #endregion

    #region Challenge Data

    [Serializable]
    public class ChallengeSaveData
    {
        public List<ActiveChallengeData> activeChallenges = new List<ActiveChallengeData>();
        public List<string> completedChallengeIDs = new List<string>();
        public long lastDailyReset;
        public long lastWeeklyReset;
    }

    [Serializable]
    public class ActiveChallengeData
    {
        public string challengeID;
        public List<float> objectiveProgress;
        public bool isCompleted;
        public long startTime;
        public long completionTime;
    }

    #endregion

    #region Settings Data

    [Serializable]
    public class SettingsData
    {
        // Graphics
        public int qualityLevel = 2; // 0=Low, 1=Medium, 2=High, 3=Ultra
        public int resolutionWidth = 1920;
        public int resolutionHeight = 1080;
        public bool fullscreen = true;
        public int targetFrameRate = 60;
        public bool vsyncEnabled = true;

        // Audio
        public float masterVolume = 1.0f;
        public float musicVolume = 0.7f;
        public float sfxVolume = 1.0f;
        public float voiceVolume = 1.0f;

        // Controls
        public float mouseSensitivity = 1.0f;
        public float aimSensitivity = 0.7f;
        public bool invertY = false;
        public Dictionary<string, string> keyBindings = new Dictionary<string, string>();

        // Gameplay
        public bool showFPS = false;
        public bool showPing = true;
        public bool showKillFeed = true;
        public bool showCrosshair = true;
        public int crosshairStyle = 0;
        public float crosshairScale = 1.0f;
        public int fovValue = 90;

        // Accessibility
        public bool colorBlindMode = false;
        public int colorBlindType = 0; // Protanopia, Deuteranopia, Tritanopia
        public bool subtitles = true;
        public float subtitleSize = 1.0f;
        public bool screenShake = true;
        public float screenShakeIntensity = 1.0f;

        // Network
        public int maxPing = 100;
        public string preferredRegion = "auto";
    }

    #endregion

    #region Player Stats Data

    [Serializable]
    public class PlayerStatsData
    {
        // Lifetime stats
        public int totalMatches = 0;
        public int matchesWon = 0;
        public int matchesLost = 0;
        public int totalKills = 0;
        public int totalDeaths = 0;
        public int headshotKills = 0;
        public int zombiesKilled = 0;
        public int playersKilled = 0;
        public int successfulExtractions = 0;
        public int failedExtractions = 0;
        public float totalDamageDealt = 0;
        public float totalDamageTaken = 0;
        public float totalDistanceTraveled = 0;
        public int itemsLooted = 0;
        public int currencyEarned = 0;
        public int totalPlayTime = 0; // Seconds

        // Per-weapon stats
        public Dictionary<string, WeaponStatsData> weaponStats = new Dictionary<string, WeaponStatsData>();
    }

    [Serializable]
    public class WeaponStatsData
    {
        public string weaponID;
        public int kills = 0;
        public int headshots = 0;
        public int shotsFired = 0;
        public int shotsHit = 0;
        public float damageDealt = 0;
    }

    #endregion

    #region Inventory Data

    [Serializable]
    public class InventoryData
    {
        public int currency = 0;
        public int premiumCurrency = 0;
        public List<InventoryItemData> items = new List<InventoryItemData>();
    }

    [Serializable]
    public class InventoryItemData
    {
        public string itemID;
        public int quantity;
    }

    #endregion
}
