using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeadFrontier.Models
{
    /// <summary>
    /// Match data model containing all information about a game session.
    /// </summary>
    [Serializable]
    public class MatchModel
    {
        #region Match Identity
        public string matchId;
        public string serverId;
        public string mapId;
        public string gameMode;
        public MatchState state;
        public DateTime startTime;
        public DateTime endTime;
        public float matchDuration;
        #endregion

        #region Configuration
        public MatchConfig config;
        #endregion

        #region Players
        public List<MatchPlayer> players;
        public List<MatchTeam> teams;
        public int maxPlayers;
        public int currentPlayers;
        #endregion

        #region Game State
        public int currentRound;
        public int maxRounds;
        public float timeRemaining;
        public bool isPaused;
        public MatchPhase currentPhase;
        public List<ExtractionZoneState> extractionZones;
        #endregion

        #region Stats
        public MatchStats stats;
        public List<MatchEvent> events;
        #endregion

        #region AI Director
        public AIDirectorState aiDirectorState;
        #endregion

        public MatchModel()
        {
            players = new List<MatchPlayer>();
            teams = new List<MatchTeam>();
            extractionZones = new List<ExtractionZoneState>();
            events = new List<MatchEvent>();
            config = new MatchConfig();
            stats = new MatchStats();
            aiDirectorState = new AIDirectorState();
        }
    }

    [Serializable]
    public class MatchConfig
    {
        public bool friendlyFire;
        public bool allowRespawn;
        public int respawnTime;
        public int maxRespawns;
        public float difficultyMultiplier;
        public float lootMultiplier;
        public float xpMultiplier;
        public bool enableVoiceChat;
        public bool enableTextChat;
        public bool isRanked;
        public bool isPrivate;
        public string password;
        public int minLevel;
        public int maxLevel;
        public bool allowCrossPlatform;
    }

    [Serializable]
    public class MatchPlayer
    {
        public string playerId;
        public string displayName;
        public int teamId;
        public string characterId;
        public int level;
        public int prestigeLevel;
        public string platform;

        public bool isHost;
        public bool isReady;
        public bool isConnected;
        public bool isAlive;
        public bool isSpectating;
        public bool hasExtracted;

        public Vector3 position;
        public Quaternion rotation;
        public float health;
        public float maxHealth;
        public float stamina;
        public float armor;

        public MatchPlayerStats matchStats;
        public LoadoutData loadout;
        public List<InventoryItem> carriedItems;
        public float extractionProgress;

        public MatchPlayer()
        {
            matchStats = new MatchPlayerStats();
            carriedItems = new List<InventoryItem>();
        }
    }

    [Serializable]
    public class MatchPlayerStats
    {
        public int kills;
        public int deaths;
        public int assists;
        public int zombieKills;
        public int playerKills;
        public int headshots;
        public int revives;
        public float damageDealt;
        public float damageTaken;
        public float healingDone;
        public int itemsCollected;
        public int lootValue;
        public float survivalTime;
        public int killStreak;
        public int highestKillStreak;
        public int score;
        public int xpEarned;
        public int currencyEarned;
    }

    [Serializable]
    public class MatchTeam
    {
        public int teamId;
        public string teamName;
        public Color teamColor;
        public List<string> memberIds;
        public int score;
        public int kills;
        public int deaths;
        public int extractions;
        public bool isWinner;
    }

    [Serializable]
    public class ExtractionZoneState
    {
        public string zoneId;
        public Vector3 position;
        public float radius;
        public bool isActive;
        public bool isOpen;
        public float timeUntilOpen;
        public float timeUntilClose;
        public int playersInZone;
        public int extractionsRemaining;
        public List<string> extractingPlayerIds;
    }

    [Serializable]
    public class MatchStats
    {
        public int totalZombiesSpawned;
        public int totalZombiesKilled;
        public int totalBossesSpawned;
        public int totalBossesKilled;
        public int totalItemsSpawned;
        public int totalItemsCollected;
        public int totalExtractions;
        public int totalDeaths;
        public int totalRevives;
        public float totalDamageDealt;
    }

    [Serializable]
    public class MatchEvent
    {
        public string eventId;
        public MatchEventType eventType;
        public string playerId;
        public string targetId;
        public string data;
        public Vector3 position;
        public float timestamp;
    }

    [Serializable]
    public class AIDirectorState
    {
        public float intensityLevel;
        public float peakIntensity;
        public float restPeriodRemaining;
        public DirectorPhase currentPhase;
        public int hordeWaveCount;
        public float timeSinceLastHorde;
        public int activeZombieCount;
        public int maxActiveZombies;
        public List<ActiveBoss> activeBosses;

        public AIDirectorState()
        {
            activeBosses = new List<ActiveBoss>();
        }
    }

    [Serializable]
    public class ActiveBoss
    {
        public string bossId;
        public string bossType;
        public Vector3 position;
        public float health;
        public float maxHealth;
        public bool isEnraged;
    }

    public enum MatchState
    {
        Lobby,
        Starting,
        InProgress,
        Ending,
        Complete,
        Cancelled
    }

    public enum MatchPhase
    {
        Warmup,
        Deployment,
        Main,
        Extraction,
        Overtime,
        PostMatch
    }

    public enum DirectorPhase
    {
        BuildUp,
        Peak,
        Relax,
        Recovery
    }

    public enum MatchEventType
    {
        PlayerJoined,
        PlayerLeft,
        PlayerKill,
        PlayerDeath,
        PlayerRevive,
        PlayerExtracted,
        ZombieKill,
        BossSpawned,
        BossKilled,
        ItemPickup,
        QuestComplete,
        PhaseChange,
        HordeTriggered,
        ExtractionOpened,
        ExtractionClosed
    }
}
