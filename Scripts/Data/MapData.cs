using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeadFrontier.Core
{
    /// <summary>
    /// ScriptableObject defining map/level properties, objectives, and configuration.
    /// </summary>
    [CreateAssetMenu(fileName = "New Map", menuName = "Dead Frontier/Core/Map Data")]
    public class MapData : ScriptableObject
    {
        [Header("Basic Info")]
        public string mapId;
        public string mapName;
        [TextArea(3, 5)]
        public string description;
        public string sceneName;

        [Header("Classification")]
        public MapSize mapSize = MapSize.Medium;
        public MapEnvironment environment = MapEnvironment.Urban;
        public MapDifficulty difficulty = MapDifficulty.Medium;

        [Header("Player Capacity")]
        public int minPlayers = 1;
        public int maxPlayers = 16;
        public int recommendedPlayers = 8;

        [Header("Match Settings")]
        public float defaultMatchDuration = 1200f; // 20 minutes
        public bool hasTimeLimit = true;
        public bool allowsLateJoin;

        [Header("Spawn Configuration")]
        public int playerSpawnPoints = 16;
        public int zombieSpawnPoints = 50;
        public int extractionZones = 4;
        public int lootSpawnPoints = 100;

        [Header("Objectives")]
        public MapObjective[] primaryObjectives;
        public MapObjective[] secondaryObjectives;
        public bool objectivesRequired; // Must complete to extract

        [Header("Environmental Hazards")]
        public EnvironmentalHazard[] hazards;
        public bool hasDayNightCycle;
        public bool hasWeatherSystem;
        public float ambientDangerLevel = 0.5f;

        [Header("Loot Distribution")]
        public LootZone[] lootZones;
        public float overallLootQuality = 1f; // Multiplier for loot rarity

        [Header("AI Configuration")]
        public int baseZombieCount = 50;
        public int maxZombieCount = 100;
        public float zombieDensityMultiplier = 1f;
        public bool allowsBossSpawns = true;
        public bool allowsHordes = true;

        [Header("Visuals")]
        public Sprite mapThumbnail;
        public Sprite mapLoadingScreen;
        public Color mapThemeColor = Color.white;

        [Header("Audio")]
        public AudioClip ambientMusic;
        public AudioClip combatMusic;
        public AudioClip extractionMusic;

        [Header("Unlock Requirements")]
        public int requiredLevel = 1;
        public bool isUnlockedByDefault = true;
        public string[] prerequisiteMaps;

        [Header("Voting")]
        public int votingWeight = 1; // Weight in map voting
        public bool availableInRotation = true;
    }

    [System.Serializable]
    public class MapObjective
    {
        public string objectiveId;
        public string objectiveName;
        [TextArea(2, 3)]
        public string objectiveDescription;
        public ObjectiveType objectiveType;
        public Vector3 objectiveLocation;
        public float objectiveRadius = 10f;
        public int rewardSoftCurrency = 500;
        public int rewardXP = 100;
    }

    [System.Serializable]
    public class EnvironmentalHazard
    {
        public string hazardName;
        public HazardType hazardType;
        public Vector3 location;
        public float radius = 10f;
        public float damagePerSecond = 10f;
        public bool isActive = true;
        public float activationInterval; // For periodic hazards
    }

    [System.Serializable]
    public class LootZone
    {
        public string zoneName;
        public Vector3 centerPoint;
        public float radius = 20f;
        public LootZoneType zoneType;
        public float lootQualityMultiplier = 1f;
        public int lootDensity = 10; // Number of loot spawns
    }

    public enum MapSize
    {
        Small,      // < 500m²
        Medium,     // 500-1000m²
        Large,      // 1000-2000m²
        XLarge      // > 2000m²
    }

    public enum MapEnvironment
    {
        Urban,          // City streets
        Industrial,     // Factories, warehouses
        Rural,          // Countryside, farms
        Military,       // Bases, bunkers
        Hospital,       // Medical facilities
        Underground,    // Sewers, subways
        Forest,         // Woods, wilderness
        Residential     // Suburbs, apartments
    }

    public enum MapDifficulty
    {
        Easy,       // Low zombie density, lots of loot
        Medium,     // Balanced
        Hard,       // High zombie density, less loot
        Extreme     // Maximum difficulty
    }

    public enum ObjectiveType
    {
        Eliminate,      // Kill specific zombies
        Collect,        // Gather items
        Secure,         // Hold area
        Activate,       // Turn on device
        Escort,         // Protect NPC
        Survive,        // Last X minutes
        Extract         // Reach extraction
    }

    public enum HazardType
    {
        Fire,           // Burning areas
        Radiation,      // Radioactive zones
        Toxic,          // Poison gas
        Electric,       // Electrical hazards
        Explosive,      // Explosive barrels
        Collapsing      // Falling debris
    }

    public enum LootZoneType
    {
        Standard,       // Normal loot
        Military,       // Weapons and ammo
        Medical,        // Health items
        Industrial,     // Crafting materials
        Residential,    // Survival items
        HighValue       // Rare/valuable items
    }
}
