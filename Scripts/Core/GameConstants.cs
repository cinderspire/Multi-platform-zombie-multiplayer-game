using UnityEngine;

namespace DeadFrontier.Core
{
    /// <summary>
    /// Central repository for all game constants and configuration values.
    /// </summary>
    public static class GameConstants
    {
        #region Layer Masks

        public static class Layers
        {
            public const string Default = "Default";
            public const string Player = "Player";
            public const string Enemy = "Enemy";
            public const string Zombie = "Zombie";
            public const string Weapon = "Weapon";
            public const string Item = "Item";
            public const string Projectile = "Projectile";
            public const string Interactable = "Interactable";
            public const string Ground = "Ground";
            public const string Wall = "Wall";
            public const string Water = "Water";
            public const string Trigger = "Trigger";
            public const string Ragdoll = "Ragdoll";
            public const string Vehicle = "Vehicle";
            public const string Building = "Building";
            public const string IgnoreRaycast = "Ignore Raycast";

            public static int DefaultMask => LayerMask.GetMask(Default);
            public static int PlayerMask => LayerMask.GetMask(Player);
            public static int EnemyMask => LayerMask.GetMask(Enemy, Zombie);
            public static int ShootableMask => LayerMask.GetMask(Default, Player, Enemy, Zombie, Vehicle, Building, Ground, Wall);
            public static int InteractableMask => LayerMask.GetMask(Item, Interactable);
            public static int GroundMask => LayerMask.GetMask(Ground, Default);
            public static int OcclusionMask => LayerMask.GetMask(Ground, Wall, Building);
        }

        #endregion

        #region Tags

        public static class Tags
        {
            public const string Player = "Player";
            public const string Enemy = "Enemy";
            public const string Zombie = "Zombie";
            public const string Weapon = "Weapon";
            public const string Item = "Item";
            public const string Interactable = "Interactable";
            public const string ExtractionZone = "ExtractionZone";
            public const string SpawnPoint = "SpawnPoint";
            public const string SafeZone = "SafeZone";
            public const string Vehicle = "Vehicle";
            public const string Building = "Building";
            public const string Destructible = "Destructible";
            public const string Water = "Water";
            public const string Loot = "Loot";
            public const string NPC = "NPC";
            public const string Boss = "Boss";
            public const string Objective = "Objective";
        }

        #endregion

        #region Player Constants

        public static class Player
        {
            // Health
            public const float DefaultMaxHealth = 100f;
            public const float DefaultHealthRegen = 0f;
            public const float DownedHealth = 50f;
            public const float DownedBleedRate = 1f;
            public const float ReviveTime = 5f;
            public const int MaxRevivesPerMatch = 3;

            // Stamina
            public const float DefaultMaxStamina = 100f;
            public const float StaminaRegenRate = 15f;
            public const float SprintStaminaDrain = 20f;
            public const float JumpStaminaCost = 15f;
            public const float StaminaRegenDelay = 1f;

            // Movement
            public const float WalkSpeed = 4f;
            public const float RunSpeed = 6f;
            public const float SprintSpeed = 8f;
            public const float CrouchSpeed = 2.5f;
            public const float ProneSpeed = 1f;
            public const float AimSpeedMultiplier = 0.7f;
            public const float JumpHeight = 1.2f;
            public const float Gravity = -20f;
            public const float AirControl = 0.3f;

            // Camera
            public const float DefaultSensitivity = 2f;
            public const float DefaultFOV = 75f;
            public const float AimFOV = 55f;
            public const float SprintFOV = 85f;
            public const float MinPitch = -85f;
            public const float MaxPitch = 85f;

            // Inventory
            public const int DefaultInventorySlots = 20;
            public const int MaxInventorySlots = 50;
            public const float DefaultCarryWeight = 50f;
            public const float MaxCarryWeight = 100f;

            // Progression
            public const int MaxLevel = 100;
            public const int MaxPrestige = 10;
            public const int BaseXPPerLevel = 1000;
            public const float XPScalingFactor = 1.1f;
        }

        #endregion

        #region Weapon Constants

        public static class Weapon
        {
            // Fire Rates (seconds between shots)
            public const float MinFireRate = 0.04f; // 1500 RPM max
            public const float MaxFireRate = 3f;

            // Damage
            public const float HeadshotMultiplier = 2f;
            public const float LimbshotMultiplier = 0.75f;
            public const float CriticalMultiplier = 1.5f;
            public const float ArmorPenetrationBase = 0f;

            // Spread
            public const float MinSpread = 0f;
            public const float MaxSpread = 20f;
            public const float SpreadRecoveryRate = 5f;
            public const float MovementSpreadMultiplier = 1.5f;
            public const float JumpSpreadMultiplier = 3f;
            public const float CrouchSpreadMultiplier = 0.7f;
            public const float AimSpreadMultiplier = 0.3f;

            // Recoil
            public const float RecoilRecoveryRate = 10f;
            public const float MaxRecoilX = 15f;
            public const float MaxRecoilY = 30f;

            // ADS
            public const float DefaultADSTime = 0.25f;
            public const float MinADSTime = 0.1f;
            public const float MaxADSTime = 0.5f;

            // Audio
            public const float SuppressedNoiseMultiplier = 0.3f;
            public const float MaxNoiseDistance = 100f;
        }

        #endregion

        #region Zombie Constants

        public static class Zombie
        {
            // AI
            public const float MinDetectionRange = 5f;
            public const float MaxDetectionRange = 100f;
            public const float DefaultVisionAngle = 180f;
            public const float AlertStateDuration = 10f;
            public const float SearchStateDuration = 30f;
            public const float MemoryDuration = 60f;

            // Movement
            public const float MinMoveSpeed = 1f;
            public const float MaxMoveSpeed = 10f;
            public const float DefaultWanderRadius = 20f;
            public const float PatrolPointWaitTime = 3f;

            // Combat
            public const float MinAttackRange = 1f;
            public const float MaxAttackRange = 10f;
            public const float MinAttackCooldown = 0.5f;
            public const float MaxAttackCooldown = 5f;

            // Spawning
            public const int MaxActiveZombies = 100;
            public const float MinSpawnDistance = 20f;
            public const float MaxSpawnDistance = 50f;
            public const float SpawnCooldown = 5f;
        }

        #endregion

        #region Match Constants

        public static class Match
        {
            // Timing
            public const float DefaultMatchDuration = 1200f; // 20 minutes
            public const float WarmupDuration = 60f;
            public const float ExtractionCountdown = 60f;
            public const float PostMatchDuration = 30f;

            // Players
            public const int MinPlayers = 1;
            public const int MaxPlayers = 8;
            public const int DefaultMaxPlayers = 4;

            // Extraction
            public const float ExtractionTime = 60f;
            public const float MinExtractionDelay = 300f; // 5 minutes
            public const int MaxExtractionsPerMatch = 3;

            // Loot
            public const float LootRespawnTime = 300f;
            public const float ItemDespawnTime = 300f;
        }

        #endregion

        #region Economy Constants

        public static class Economy
        {
            // Currencies
            public const string SoftCurrency = "Credits";
            public const string HardCurrency = "Platinum";
            public const string EventCurrency = "EventTokens";

            // Limits
            public const int MaxSoftCurrency = 999999999;
            public const int MaxHardCurrency = 999999;

            // Trading
            public const float TradingTax = 0.05f; // 5%
            public const float AuctionHouseTax = 0.10f; // 10%
            public const int MaxTradesPerDay = 10;

            // Daily Rewards
            public const int MaxDailyStreak = 30;
            public const int StreakResetHours = 48;
        }

        #endregion

        #region Network Constants

        public static class Network
        {
            // Tick Rates
            public const int ServerTickRate = 64;
            public const int ClientSendRate = 30;
            public const int SnapshotRate = 20;

            // Interpolation
            public const float InterpolationDelay = 0.1f;
            public const float ExtrapolationLimit = 0.5f;

            // Timeouts
            public const float ConnectionTimeout = 10f;
            public const float DisconnectTimeout = 30f;
            public const float KeepAliveInterval = 5f;

            // Limits
            public const int MaxPacketSize = 1400;
            public const int MaxPendingMessages = 100;
        }

        #endregion

        #region Audio Constants

        public static class Audio
        {
            // Volume Ranges
            public const float MinVolume = 0f;
            public const float MaxVolume = 1f;
            public const float DefaultMasterVolume = 0.8f;
            public const float DefaultMusicVolume = 0.5f;
            public const float DefaultSFXVolume = 0.8f;
            public const float DefaultVoiceVolume = 1f;

            // 3D Audio
            public const float DefaultMinDistance = 1f;
            public const float DefaultMaxDistance = 50f;
            public const float FootstepMinDistance = 1f;
            public const float FootstepMaxDistance = 20f;
            public const float GunfireMinDistance = 5f;
            public const float GunfireMaxDistance = 100f;
        }

        #endregion

        #region UI Constants

        public static class UI
        {
            // Crosshair
            public const float MinCrosshairSize = 10f;
            public const float MaxCrosshairSize = 50f;
            public const float DefaultCrosshairSize = 25f;

            // HUD
            public const float DamageIndicatorDuration = 1f;
            public const float KillfeedEntryDuration = 5f;
            public const int MaxKillfeedEntries = 5;
            public const float NotificationDuration = 3f;

            // Minimap
            public const float MinimapDefaultZoom = 50f;
            public const float MinimapMinZoom = 25f;
            public const float MinimapMaxZoom = 200f;

            // Interaction
            public const float InteractionRange = 3f;
            public const float InteractionPromptDelay = 0.2f;
        }

        #endregion

        #region Scene Names

        public static class Scenes
        {
            public const string MainMenu = "MainMenu";
            public const string Lobby = "Lobby";
            public const string Loading = "Loading";
            public const string Tutorial = "Tutorial";
            public const string Urban = "Map_Urban";
            public const string Industrial = "Map_Industrial";
            public const string Hospital = "Map_Hospital";
            public const string Mall = "Map_Mall";
            public const string Military = "Map_Military";
        }

        #endregion

        #region Input Actions

        public static class InputActions
        {
            // Movement
            public const string Move = "Move";
            public const string Look = "Look";
            public const string Jump = "Jump";
            public const string Sprint = "Sprint";
            public const string Crouch = "Crouch";
            public const string Prone = "Prone";

            // Combat
            public const string Fire = "Fire";
            public const string Aim = "Aim";
            public const string Reload = "Reload";
            public const string Melee = "Melee";
            public const string Grenade = "Grenade";

            // Weapons
            public const string WeaponPrimary = "WeaponPrimary";
            public const string WeaponSecondary = "WeaponSecondary";
            public const string WeaponMelee = "WeaponMelee";
            public const string WeaponCycle = "WeaponCycle";

            // Interaction
            public const string Interact = "Interact";
            public const string Use = "Use";

            // Communication
            public const string Ping = "Ping";
            public const string VoiceChat = "VoiceChat";
            public const string TextChat = "TextChat";
            public const string QuickMessage = "QuickMessage";

            // UI
            public const string Inventory = "Inventory";
            public const string Map = "Map";
            public const string Scoreboard = "Scoreboard";
            public const string Pause = "Pause";
        }

        #endregion

        #region Rarity Colors

        public static class RarityColors
        {
            public static readonly Color Common = new Color(0.7f, 0.7f, 0.7f);
            public static readonly Color Uncommon = new Color(0.2f, 0.8f, 0.2f);
            public static readonly Color Rare = new Color(0.2f, 0.4f, 1f);
            public static readonly Color Epic = new Color(0.6f, 0.2f, 0.8f);
            public static readonly Color Legendary = new Color(1f, 0.6f, 0f);
            public static readonly Color Mythic = new Color(1f, 0.2f, 0.2f);

            public static Color GetRarityColor(string rarity)
            {
                return rarity?.ToLower() switch
                {
                    "common" => Common,
                    "uncommon" => Uncommon,
                    "rare" => Rare,
                    "epic" => Epic,
                    "legendary" => Legendary,
                    "mythic" => Mythic,
                    _ => Common
                };
            }
        }

        #endregion

        #region Time Constants

        public static class Time
        {
            public const float DayLengthMinutes = 20f;
            public const float NightLengthMinutes = 10f;
            public const float DawnDuration = 2f;
            public const float DuskDuration = 2f;

            public static float GetTotalDayCycleMinutes() => DayLengthMinutes + NightLengthMinutes;
        }

        #endregion
    }
}
