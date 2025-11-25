using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeadFrontier.Models
{
    /// <summary>
    /// Network data models for multiplayer synchronization.
    /// </summary>

    #region Player Network State
    [Serializable]
    public struct PlayerNetworkState
    {
        public ulong clientId;
        public string playerId;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 velocity;
        public PlayerActionState actionState;
        public float health;
        public float stamina;
        public float armor;
        public byte weaponIndex;
        public bool isAiming;
        public bool isSprinting;
        public bool isCrouching;
        public bool isProne;
        public bool isReloading;
        public bool isDead;
        public uint serverTick;
        public float timestamp;
    }

    [Serializable]
    public struct PlayerInputState
    {
        public Vector2 moveInput;
        public Vector2 lookInput;
        public bool firePressed;
        public bool aimPressed;
        public bool reloadPressed;
        public bool sprintPressed;
        public bool crouchPressed;
        public bool jumpPressed;
        public bool interactPressed;
        public byte abilityIndex;
        public bool abilityPressed;
        public byte weaponSwitchIndex;
        public uint clientTick;
        public float timestamp;
    }

    public enum PlayerActionState : byte
    {
        Idle,
        Walking,
        Running,
        Sprinting,
        Jumping,
        Falling,
        Crouching,
        Prone,
        Climbing,
        Swimming,
        Aiming,
        Firing,
        Reloading,
        Interacting,
        UsingAbility,
        Downed,
        BeingRevived,
        Dead
    }
    #endregion

    #region Weapon Network State
    [Serializable]
    public struct WeaponNetworkState
    {
        public string weaponId;
        public int currentAmmo;
        public int reserveAmmo;
        public float spread;
        public bool isFiring;
        public bool isReloading;
        public byte fireMode;
        public AttachmentNetworkState[] attachments;
    }

    [Serializable]
    public struct AttachmentNetworkState
    {
        public string attachmentId;
        public byte slotIndex;
        public bool isActive;
    }

    [Serializable]
    public struct ProjectileNetworkState
    {
        public uint projectileId;
        public string projectileType;
        public Vector3 position;
        public Vector3 velocity;
        public ulong ownerClientId;
        public float damage;
        public float lifetime;
        public float timestamp;
    }
    #endregion

    #region Zombie Network State
    [Serializable]
    public struct ZombieNetworkState
    {
        public uint zombieNetId;
        public string zombieTypeId;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 velocity;
        public ZombieAIState aiState;
        public float health;
        public float maxHealth;
        public ulong targetClientId;
        public bool isAlerted;
        public bool isAttacking;
        public bool isDead;
        public uint serverTick;
    }

    public enum ZombieAIState : byte
    {
        Idle,
        Wandering,
        Investigating,
        Chasing,
        Attacking,
        Stunned,
        Fleeing,
        Dead
    }

    [Serializable]
    public struct ZombieSpawnRequest
    {
        public string zombieTypeId;
        public Vector3 position;
        public Quaternion rotation;
        public float healthMultiplier;
        public float damageMultiplier;
        public bool isBoss;
        public string spawnReason;
    }
    #endregion

    #region Item Network State
    [Serializable]
    public struct ItemNetworkState
    {
        public uint itemNetId;
        public string itemId;
        public Vector3 position;
        public Quaternion rotation;
        public int quantity;
        public float durability;
        public bool isInteractable;
        public ulong ownerClientId;
        public float despawnTime;
    }

    [Serializable]
    public struct LootContainerNetworkState
    {
        public uint containerNetId;
        public string containerType;
        public Vector3 position;
        public bool isOpen;
        public bool isLooted;
        public ItemNetworkState[] contents;
        public ulong interactingClientId;
    }
    #endregion

    #region Match Network State
    [Serializable]
    public struct MatchNetworkState
    {
        public string matchId;
        public MatchPhase phase;
        public float timeRemaining;
        public int currentRound;
        public bool isPaused;
        public float intensityLevel;
        public ExtractionZoneNetworkState[] extractionZones;
        public uint serverTick;
    }

    [Serializable]
    public struct ExtractionZoneNetworkState
    {
        public string zoneId;
        public bool isActive;
        public bool isOpen;
        public float progress;
        public byte[] playerIds;
    }

    [Serializable]
    public struct LeaderboardEntry
    {
        public ulong clientId;
        public string playerName;
        public int kills;
        public int deaths;
        public int score;
        public bool hasExtracted;
    }
    #endregion

    #region RPC Messages
    [Serializable]
    public struct DamageMessage
    {
        public ulong targetClientId;
        public uint targetNetId;
        public float damage;
        public DamageType damageType;
        public Vector3 hitPoint;
        public Vector3 hitNormal;
        public ulong attackerClientId;
        public string weaponId;
        public bool isHeadshot;
        public bool isCritical;
    }

    [Serializable]
    public struct HealMessage
    {
        public ulong targetClientId;
        public float healAmount;
        public ulong healerClientId;
        public string healSourceId;
    }

    [Serializable]
    public struct ReviveMessage
    {
        public ulong targetClientId;
        public ulong reviverClientId;
        public float reviveProgress;
        public bool isComplete;
    }

    [Serializable]
    public struct ChatMessage
    {
        public ulong senderClientId;
        public string senderName;
        public string message;
        public ChatChannel channel;
        public float timestamp;
    }

    public enum ChatChannel : byte
    {
        All,
        Team,
        Squad,
        Whisper,
        System
    }

    [Serializable]
    public struct PingMessage
    {
        public ulong senderClientId;
        public PingType pingType;
        public Vector3 worldPosition;
        public uint targetNetId;
        public string customText;
        public float timestamp;
    }

    public enum PingType : byte
    {
        Generic,
        Enemy,
        Item,
        Danger,
        GoHere,
        Help,
        Affirmative,
        Negative,
        Extraction
    }

    public enum DamageType : byte
    {
        Ballistic,
        Explosive,
        Fire,
        Electric,
        Toxic,
        Melee,
        Fall,
        Environmental,
        Bleed
    }
    #endregion

    #region Server Messages
    [Serializable]
    public struct ServerInfoMessage
    {
        public string serverId;
        public string serverName;
        public string mapId;
        public string gameMode;
        public int currentPlayers;
        public int maxPlayers;
        public int ping;
        public string region;
        public bool isRanked;
        public bool hasPassword;
        public int minLevel;
        public int maxLevel;
    }

    [Serializable]
    public struct MatchResultMessage
    {
        public string matchId;
        public MatchResultType result;
        public int xpEarned;
        public int currencyEarned;
        public int battlePassXP;
        public MatchPlayerStats playerStats;
        public string[] unlockedRewards;
        public ChallengeProgress[] challengeUpdates;
        public QuestProgress[] questUpdates;
    }

    public enum MatchResultType : byte
    {
        Victory,
        Defeat,
        Extracted,
        Died,
        Abandoned,
        Disconnected
    }
    #endregion
}
