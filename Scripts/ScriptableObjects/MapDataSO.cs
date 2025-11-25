using UnityEngine;

namespace DeadFrontier.Maps
{
    /// <summary>
    /// ScriptableObject defining map/level data for the game.
    /// </summary>
    [CreateAssetMenu(fileName = "New Map", menuName = "Dead Frontier/Maps/Map Data")]
    public class MapDataSO : ScriptableObject
    {
        [Header("Identification")]
        public string mapId;
        public string mapName;
        [TextArea(3, 5)]
        public string description;
        public MapType mapType;
        public MapEnvironment environment;

        [Header("Visuals")]
        public Sprite mapPreviewImage;
        public Sprite minimapImage;
        public Texture2D loadingScreenImage;
        public string sceneName;

        [Header("Dimensions")]
        public Vector2 mapSize = new Vector2(500f, 500f);
        public Bounds playableArea;
        public float outOfBoundsTimer = 10f;

        [Header("Player Settings")]
        public int minPlayers = 1;
        public int maxPlayers = 8;
        public int recommendedPlayers = 4;
        public SpawnPoint[] playerSpawnPoints;
        public ExtractionPoint[] extractionPoints;

        [Header("Zombie Settings")]
        public ZombieSpawnConfig zombieConfig;
        public int maxActiveZombies = 50;
        public float zombieDensity = 1f;

        [Header("Loot Settings")]
        public LootSpawnConfig lootConfig;
        public LootZone[] lootZones;
        public float globalLootMultiplier = 1f;

        [Header("Points of Interest")]
        public PointOfInterest[] pointsOfInterest;
        public SafeZone[] safeZones;

        [Header("Environmental")]
        public WeatherConfig[] possibleWeather;
        public TimeOfDayConfig timeOfDayConfig;
        public EnvironmentalHazard[] hazards;

        [Header("Audio")]
        public AudioClip[] ambientSounds;
        public AudioClip backgroundMusic;
        public float ambientSoundVolume = 0.5f;

        [Header("Objectives")]
        public MapObjective[] objectives;
        public float matchDuration = 1200f; // 20 minutes default

        [Header("Difficulty")]
        public MapDifficulty difficulty;
        public int recommendedLevel = 1;
        public float difficultyMultiplier = 1f;

        [Header("Unlock Requirements")]
        public int unlockLevel = 1;
        public string[] prerequisiteMaps;
        public bool isEventMap;
    }

    [System.Serializable]
    public class SpawnPoint
    {
        public Vector3 position;
        public Quaternion rotation;
        public int teamId;
        public bool isActive = true;
    }

    [System.Serializable]
    public class ExtractionPoint
    {
        public string extractionId;
        public string extractionName;
        public Vector3 position;
        public float radius = 10f;
        public float extractionTime = 60f;
        public bool requiresKey;
        public string requiredKeyId;
        public bool isTimeLimited;
        public float openTime;
        public float closeTime;
        public int maxExtractions = -1;
        public bool alertsZombies = true;
    }

    [System.Serializable]
    public class ZombieSpawnConfig
    {
        public ZombieSpawnPoint[] spawnPoints;
        public float baseSpawnRate = 0.5f;
        public float spawnRateIncrease = 0.1f;
        public ZombieTypeWeight[] zombieWeights;
        public int initialZombieCount = 20;
        public bool enableHordes = true;
        public float hordeChance = 0.1f;
        public int hordeSize = 15;
    }

    [System.Serializable]
    public class ZombieSpawnPoint
    {
        public Vector3 position;
        public float spawnRadius = 5f;
        public int maxZombies = 5;
        public float respawnTime = 30f;
        public bool activeAtStart = true;
    }

    [System.Serializable]
    public class ZombieTypeWeight
    {
        public string zombieTypeId;
        [Range(0f, 1f)]
        public float weight = 0.5f;
        public int minDifficultyToSpawn = 1;
    }

    [System.Serializable]
    public class LootSpawnConfig
    {
        public LootSpawnPoint[] spawnPoints;
        public float respawnTime = 300f;
        public bool dynamicRespawn = true;
    }

    [System.Serializable]
    public class LootSpawnPoint
    {
        public Vector3 position;
        public LootTier lootTier;
        public string[] possibleItemIds;
        [Range(0f, 1f)]
        public float spawnChance = 0.7f;
        public bool isContainer;
        public string containerType;
    }

    [System.Serializable]
    public class LootZone
    {
        public string zoneName;
        public Bounds zoneBounds;
        public LootTier baseLootTier;
        public float lootDensityMultiplier = 1f;
        public bool isHighRiskArea;
    }

    [System.Serializable]
    public class PointOfInterest
    {
        public string poiId;
        public string poiName;
        public PoiType poiType;
        public Vector3 position;
        public float radius = 20f;
        public bool showOnMap = true;
        public Sprite mapIcon;
        public string description;
    }

    [System.Serializable]
    public class SafeZone
    {
        public string zoneId;
        public string zoneName;
        public Vector3 position;
        public float radius = 15f;
        public bool blocksCombat = true;
        public bool healsPlayers;
        public float healRate = 5f;
        public bool hasVendor;
        public bool hasStash;
    }

    [System.Serializable]
    public class WeatherConfig
    {
        public Weather.WeatherType weatherType;
        [Range(0f, 1f)]
        public float probability = 0.2f;
        public float minDuration = 60f;
        public float maxDuration = 300f;
    }

    [System.Serializable]
    public class TimeOfDayConfig
    {
        public bool enableDayNightCycle = true;
        public float dayLengthMinutes = 20f;
        public float nightLengthMinutes = 10f;
        public TimeOfDay startingTime = TimeOfDay.Day;
    }

    [System.Serializable]
    public class EnvironmentalHazard
    {
        public string hazardId;
        public HazardType hazardType;
        public Vector3 position;
        public float radius = 5f;
        public float damage = 10f;
        public float damageInterval = 1f;
        public bool isActive = true;
    }

    [System.Serializable]
    public class MapObjective
    {
        public string objectiveId;
        public string objectiveName;
        public ObjectiveType objectiveType;
        public Vector3 location;
        public bool isRequired;
        public int pointValue;
    }

    public enum MapType
    {
        Extraction,
        Survival,
        Horde,
        PvP,
        PvPvE,
        Tutorial,
        Hub,
        Raid,
        Event
    }

    public enum MapEnvironment
    {
        Urban,
        Rural,
        Industrial,
        Military,
        Forest,
        Desert,
        Snow,
        Coastal,
        Underground,
        Hospital,
        Mall,
        Airport
    }

    public enum MapDifficulty
    {
        Easy,
        Normal,
        Hard,
        Nightmare,
        Hardcore
    }

    public enum LootTier
    {
        Low,
        Medium,
        High,
        Premium,
        Boss
    }

    public enum PoiType
    {
        LootCache,
        SafeHouse,
        MedicalStation,
        AmmoDump,
        ControlPoint,
        BossSpawn,
        SecretArea,
        QuestLocation,
        Vendor,
        Workbench
    }

    public enum TimeOfDay
    {
        Dawn,
        Day,
        Dusk,
        Night
    }

    public enum HazardType
    {
        Radiation,
        Fire,
        Toxic,
        Electric,
        Explosive,
        Collapse,
        Alarm
    }

    public enum ObjectiveType
    {
        Extraction,
        Elimination,
        Capture,
        Defend,
        Collect,
        Survive
    }
}
