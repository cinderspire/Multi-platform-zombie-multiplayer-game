namespace DeadFrontier.Core
{
    /// <summary>
    /// Game-wide constants to avoid magic numbers
    /// </summary>
    public static class Constants
    {
        // Physics Layers
        public const int LAYER_PLAYER = 8;
        public const int LAYER_ZOMBIE = 9;
        public const int LAYER_ENVIRONMENT = 10;
        public const int LAYER_PROJECTILE = 11;
        public const int LAYER_LOOT = 12;
        public const int LAYER_INTERACTABLE = 13;
        public const int LAYER_EXTRACTION_ZONE = 14;

        // Tags
        public const string TAG_PLAYER = "Player";
        public const string TAG_ZOMBIE = "Zombie";
        public const string TAG_WEAPON = "Weapon";
        public const string TAG_LOOT = "Loot";
        public const string TAG_EXTRACTION_POINT = "ExtractionPoint";

        // Game Settings
        public const int MAX_PLAYERS = 8;
        public const float MATCH_DURATION = 600f; // 10 minutes in seconds
        public const float EXTRACTION_DURATION = 30f; // 30 seconds to extract
        public const int EXTRACTION_POINT_COUNT = 3;

        // Player Stats
        public const float PLAYER_MAX_HEALTH = 100f;
        public const float PLAYER_WALK_SPEED = 5f;
        public const float PLAYER_SPRINT_SPEED = 8f;
        public const float PLAYER_CROUCH_SPEED = 2.5f;
        public const float PLAYER_JUMP_HEIGHT = 1.5f;
        public const float PLAYER_GRAVITY = -9.81f;
        public const float PLAYER_MAX_STAMINA = 100f;
        public const float PLAYER_STAMINA_REGEN_RATE = 10f; // per second
        public const float PLAYER_STAMINA_DRAIN_RATE = 15f; // per second when sprinting

        // Weapon Settings
        public const float WEAPON_SWITCH_DELAY = 0.5f;
        public const float HEADSHOT_MULTIPLIER = 2.5f;

        // Zombie Settings
        public const float ZOMBIE_VISION_RANGE = 30f;
        public const float ZOMBIE_HEARING_RANGE = 50f;
        public const float ZOMBIE_ATTACK_RANGE = 2f;
        public const float ZOMBIE_MEMORY_DURATION = 10f; // seconds to remember last known position
        public const int ZOMBIE_HORDE_MIN_SIZE = 20;
        public const int ZOMBIE_HORDE_MAX_SIZE = 50;

        // Networking
        public const int NETWORK_TICK_RATE = 60;
        public const float NETWORK_UPDATE_INTERVAL = 0.05f; // 20Hz position updates
        public const int MAX_SNAPSHOT_HISTORY = 60; // 1 second of snapshots at 60 FPS
        public const float INTERPOLATION_DELAY = 0.1f; // 100ms
        public const int MAX_PACKET_SIZE = 1200; // bytes

        // Performance
        public const int OBJECT_POOL_DEFAULT_CAPACITY = 20;
        public const int OBJECT_POOL_MAX_SIZE = 100;
        public const float SPATIAL_GRID_CELL_SIZE = 10f; // meters
        public const int MAX_ZOMBIES_PER_MATCH = 100;
        public const float LOD_DISTANCE_TIER1 = 10f; // meters
        public const float LOD_DISTANCE_TIER2 = 30f;
        public const float LOD_DISTANCE_TIER3 = 60f;

        // UI
        public const float UI_FADE_DURATION = 0.3f;
        public const float DAMAGE_FLASH_DURATION = 0.5f;
        public const float CROSSHAIR_HITMARKER_DURATION = 0.2f;

        // Audio
        public const float AUDIO_MAX_DISTANCE = 100f;
        public const float AUDIO_ZOMBIE_AGGRO_DISTANCE = 200f; // Extreme noise (screamer)
        public const int AUDIO_SOURCE_POOL_SIZE = 20;

        // Economy
        public const int STARTING_CURRENCY = 1000;
        public const int BATTLE_PASS_TIER_COUNT = 50;
        public const int BATTLE_PASS_PRICE = 999; // cents ($9.99)
        public const int INVENTORY_DEFAULT_SLOTS = 12;
        public const int STASH_DEFAULT_SLOTS = 50;

        // Match Settings
        public const int MIN_PLAYERS_TO_START = 2; // For testing, increase to 6-8 for production
        public const float LOBBY_COUNTDOWN_DURATION = 30f;
        public const float PREGAME_COUNTDOWN = 10f;

        // File Paths (Resources)
        public const string PATH_WEAPON_DATA = "Data/Weapons/";
        public const string PATH_ZOMBIE_CONFIG = "Data/Zombies/";
        public const string PATH_ITEM_DATA = "Data/Items/";
        public const string PATH_LOOT_TABLES = "Data/LootTables/";

        // Scene Names
        public const string SCENE_BOOTSTRAP = "Bootstrap";
        public const string SCENE_MAIN_MENU = "MainMenu";
        public const string SCENE_LOBBY = "Lobby";
        public const string SCENE_LOADING = "Loading";
        public const string SCENE_DOWNTOWN_RUINS = "DowntownRuins";
        public const string SCENE_MILITARY_BASE = "MilitaryBase";
        public const string SCENE_FOREST_OUTPOST = "ForestOutpost";
    }
}
