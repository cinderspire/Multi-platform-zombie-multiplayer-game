using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace DeadFrontier.Statistics
{
    /// <summary>
    /// Comprehensive statistics tracking system.
    /// Records all player performance metrics, lifetime stats, session data, and leaderboards.
    /// Provides detailed analytics for player progression and competitive rankings.
    /// </summary>
    public class StatisticsTracker : MonoBehaviour
    {
        public static StatisticsTracker Instance { get; private set; }

        [Header("Tracking Settings")]
        [SerializeField] private bool enableDetailedTracking = true;
        [SerializeField] private float statSaveInterval = 60f; // Save every minute
        [SerializeField] private int maxSessionHistory = 100;

        [Header("Leaderboard Settings")]
        [SerializeField] private int leaderboardSize = 100;
        [SerializeField] private float leaderboardUpdateInterval = 300f; // 5 minutes

        // Player statistics
        private Dictionary<ulong, PlayerStatistics> playerStats = new Dictionary<ulong, PlayerStatistics>();
        private Dictionary<ulong, SessionStatistics> currentSessions = new Dictionary<ulong, SessionStatistics>();

        // Leaderboards
        private Dictionary<LeaderboardType, List<LeaderboardEntry>> leaderboards = new Dictionary<LeaderboardType, List<LeaderboardEntry>>();
        private float lastLeaderboardUpdate;
        private float lastStatSave;

        // Events
        public event Action<ulong, StatType, float> OnStatChanged;
        public event Action<ulong, SessionStatistics> OnSessionCompleted;
        public event Action<LeaderboardType> OnLeaderboardUpdated;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            InitializeLeaderboards();
            LoadAllPlayerStats();
        }

        private void Update()
        {
            UpdateLeaderboards();
            AutoSaveStats();
        }

        #region Initialization

        private void InitializeLeaderboards()
        {
            foreach (LeaderboardType type in Enum.GetValues(typeof(LeaderboardType)))
            {
                leaderboards[type] = new List<LeaderboardEntry>();
            }

            Debug.Log("[StatisticsTracker] Initialized leaderboards");
        }

        public void InitializePlayerStats(ulong playerId)
        {
            if (playerStats.ContainsKey(playerId)) return;

            playerStats[playerId] = new PlayerStatistics
            {
                playerId = playerId,
                lifetime = new LifetimeStats(),
                combat = new CombatStats(),
                survival = new SurvivalStats(),
                economy = new EconomyStats(),
                social = new SocialStats(),
                sessionHistory = new List<SessionStatistics>()
            };

            Debug.Log($"[StatisticsTracker] Initialized stats for player {playerId}");
        }

        #endregion

        #region Session Tracking

        public void StartSession(ulong playerId)
        {
            var session = new SessionStatistics
            {
                playerId = playerId,
                sessionStart = DateTime.UtcNow,
                kills = 0,
                deaths = 0,
                assists = 0,
                damageDealt = 0,
                damageTaken = 0,
                distanceTraveled = 0,
                itemsLooted = 0,
                currencyEarned = 0,
                missionsCompleted = 0
            };

            currentSessions[playerId] = session;

            Debug.Log($"[StatisticsTracker] Session started for player {playerId}");
        }

        public void EndSession(ulong playerId)
        {
            if (!currentSessions.ContainsKey(playerId)) return;

            var session = currentSessions[playerId];
            session.sessionEnd = DateTime.UtcNow;
            session.duration = (float)(session.sessionEnd - session.sessionStart).TotalSeconds;

            // Add to history
            if (!playerStats.ContainsKey(playerId))
            {
                InitializePlayerStats(playerId);
            }

            playerStats[playerId].sessionHistory.Add(session);

            // Limit history size
            if (playerStats[playerId].sessionHistory.Count > maxSessionHistory)
            {
                playerStats[playerId].sessionHistory.RemoveAt(0);
            }

            currentSessions.Remove(playerId);

            OnSessionCompleted?.Invoke(playerId, session);

            SavePlayerStats(playerId);

            Debug.Log($"[StatisticsTracker] Session ended for player {playerId}. Duration: {session.duration:F0}s");
        }

        #endregion

        #region Stat Recording

        public void RecordKill(ulong killerId, ulong victimId, string weaponId = null)
        {
            // Killer stats
            if (currentSessions.ContainsKey(killerId))
            {
                currentSessions[killerId].kills++;
            }

            IncrementStat(killerId, StatType.Kills, 1);
            IncrementCombatStat(killerId, s => s.totalKills++);

            if (!string.IsNullOrEmpty(weaponId))
            {
                IncrementWeaponKills(killerId, weaponId);
            }

            // Victim stats
            if (currentSessions.ContainsKey(victimId))
            {
                currentSessions[victimId].deaths++;
            }

            IncrementStat(victimId, StatType.Deaths, 1);
            IncrementCombatStat(victimId, s => s.totalDeaths++);

            // Update K/D ratio
            UpdateKDRatio(killerId);
            UpdateKDRatio(victimId);
        }

        public void RecordAssist(ulong playerId)
        {
            if (currentSessions.ContainsKey(playerId))
            {
                currentSessions[playerId].assists++;
            }

            IncrementStat(playerId, StatType.Assists, 1);
            IncrementCombatStat(playerId, s => s.totalAssists++);
        }

        public void RecordDamage(ulong dealerId, float damage)
        {
            if (currentSessions.ContainsKey(dealerId))
            {
                currentSessions[dealerId].damageDealt += damage;
            }

            IncrementStat(dealerId, StatType.DamageDealt, damage);
            IncrementCombatStat(dealerId, s => s.totalDamageDealt += damage);
        }

        public void RecordDamageTaken(ulong playerId, float damage)
        {
            if (currentSessions.ContainsKey(playerId))
            {
                currentSessions[playerId].damageTaken += damage;
            }

            IncrementStat(playerId, StatType.DamageTaken, damage);
            IncrementCombatStat(playerId, s => s.totalDamageTaken += damage);
        }

        public void RecordHeadshot(ulong playerId)
        {
            IncrementStat(playerId, StatType.Headshots, 1);
            IncrementCombatStat(playerId, s => s.totalHeadshots++);
        }

        public void RecordDistance(ulong playerId, float distance)
        {
            if (currentSessions.ContainsKey(playerId))
            {
                currentSessions[playerId].distanceTraveled += distance;
            }

            IncrementStat(playerId, StatType.DistanceTraveled, distance);
            IncrementSurvivalStat(playerId, s => s.totalDistanceTraveled += distance);
        }

        public void RecordExtraction(ulong playerId, bool successful)
        {
            IncrementSurvivalStat(playerId, s => s.totalExtractions++);

            if (successful)
            {
                IncrementStat(playerId, StatType.SuccessfulExtractions, 1);
                IncrementSurvivalStat(playerId, s => s.successfulExtractions++);
            }
            else
            {
                IncrementSurvivalStat(playerId, s => s.failedExtractions++);
            }

            // Update extraction rate
            UpdateExtractionRate(playerId);
        }

        public void RecordItemLooted(ulong playerId)
        {
            if (currentSessions.ContainsKey(playerId))
            {
                currentSessions[playerId].itemsLooted++;
            }

            IncrementStat(playerId, StatType.ItemsLooted, 1);
            IncrementEconomyStat(playerId, s => s.totalItemsLooted++);
        }

        public void RecordCurrencyEarned(ulong playerId, int amount)
        {
            if (currentSessions.ContainsKey(playerId))
            {
                currentSessions[playerId].currencyEarned += amount;
            }

            IncrementStat(playerId, StatType.CurrencyEarned, amount);
            IncrementEconomyStat(playerId, s => s.totalCurrencyEarned += amount);
        }

        public void RecordCurrencySpent(ulong playerId, int amount)
        {
            IncrementStat(playerId, StatType.CurrencySpent, amount);
            IncrementEconomyStat(playerId, s => s.totalCurrencySpent += amount);
        }

        public void RecordMissionCompleted(ulong playerId)
        {
            if (currentSessions.ContainsKey(playerId))
            {
                currentSessions[playerId].missionsCompleted++;
            }

            IncrementStat(playerId, StatType.MissionsCompleted, 1);
            IncrementLifetimeStat(playerId, s => s.totalMissionsCompleted++);
        }

        public void RecordPlayTime(ulong playerId, float seconds)
        {
            IncrementStat(playerId, StatType.PlayTime, seconds);
            IncrementLifetimeStat(playerId, s => s.totalPlayTimeSeconds += seconds);
        }

        #endregion

        #region Stat Calculations

        private void UpdateKDRatio(ulong playerId)
        {
            if (!playerStats.ContainsKey(playerId)) return;

            var combat = playerStats[playerId].combat;
            combat.kdRatio = combat.totalDeaths > 0
                ? (float)combat.totalKills / combat.totalDeaths
                : combat.totalKills;
        }

        private void UpdateExtractionRate(ulong playerId)
        {
            if (!playerStats.ContainsKey(playerId)) return;

            var survival = playerStats[playerId].survival;
            survival.extractionRate = survival.totalExtractions > 0
                ? (float)survival.successfulExtractions / survival.totalExtractions
                : 0f;
        }

        private void IncrementWeaponKills(ulong playerId, string weaponId)
        {
            if (!playerStats.ContainsKey(playerId))
            {
                InitializePlayerStats(playerId);
            }

            var combat = playerStats[playerId].combat;

            if (!combat.weaponKills.ContainsKey(weaponId))
            {
                combat.weaponKills[weaponId] = 0;
            }

            combat.weaponKills[weaponId]++;
        }

        #endregion

        #region Helper Methods

        private void IncrementStat(ulong playerId, StatType statType, float amount)
        {
            OnStatChanged?.Invoke(playerId, statType, amount);
        }

        private void IncrementLifetimeStat(ulong playerId, Action<LifetimeStats> action)
        {
            if (!playerStats.ContainsKey(playerId))
            {
                InitializePlayerStats(playerId);
            }

            action(playerStats[playerId].lifetime);
        }

        private void IncrementCombatStat(ulong playerId, Action<CombatStats> action)
        {
            if (!playerStats.ContainsKey(playerId))
            {
                InitializePlayerStats(playerId);
            }

            action(playerStats[playerId].combat);
        }

        private void IncrementSurvivalStat(ulong playerId, Action<SurvivalStats> action)
        {
            if (!playerStats.ContainsKey(playerId))
            {
                InitializePlayerStats(playerId);
            }

            action(playerStats[playerId].survival);
        }

        private void IncrementEconomyStat(ulong playerId, Action<EconomyStats> action)
        {
            if (!playerStats.ContainsKey(playerId))
            {
                InitializePlayerStats(playerId);
            }

            action(playerStats[playerId].economy);
        }

        #endregion

        #region Leaderboards

        private void UpdateLeaderboards()
        {
            if (Time.time - lastLeaderboardUpdate < leaderboardUpdateInterval) return;

            UpdateLeaderboard(LeaderboardType.Kills, stats => stats.combat.totalKills);
            UpdateLeaderboard(LeaderboardType.KDRatio, stats => stats.combat.kdRatio);
            UpdateLeaderboard(LeaderboardType.DamageDealt, stats => stats.combat.totalDamageDealt);
            UpdateLeaderboard(LeaderboardType.Headshots, stats => stats.combat.totalHeadshots);
            UpdateLeaderboard(LeaderboardType.ExtractionRate, stats => stats.survival.extractionRate);
            UpdateLeaderboard(LeaderboardType.CurrencyEarned, stats => stats.economy.totalCurrencyEarned);
            UpdateLeaderboard(LeaderboardType.PlayTime, stats => stats.lifetime.totalPlayTimeSeconds);
            UpdateLeaderboard(LeaderboardType.MissionsCompleted, stats => stats.lifetime.totalMissionsCompleted);

            lastLeaderboardUpdate = Time.time;
        }

        private void UpdateLeaderboard(LeaderboardType type, Func<PlayerStatistics, float> statSelector)
        {
            leaderboards[type] = playerStats.Values
                .OrderByDescending(statSelector)
                .Take(leaderboardSize)
                .Select((stats, index) => new LeaderboardEntry
                {
                    rank = index + 1,
                    playerId = stats.playerId,
                    score = statSelector(stats)
                })
                .ToList();

            OnLeaderboardUpdated?.Invoke(type);
        }

        #endregion

        #region Persistence

        private void LoadAllPlayerStats()
        {
            // In production, load from database
            Debug.Log("[StatisticsTracker] Statistics tracker initialized");
        }

        private void AutoSaveStats()
        {
            if (Time.time - lastStatSave < statSaveInterval) return;

            foreach (var playerId in playerStats.Keys)
            {
                SavePlayerStats(playerId);
            }

            lastStatSave = Time.time;
        }

        private void SavePlayerStats(ulong playerId)
        {
            // In production, save to database
        }

        #endregion

        #region Public Getters

        public PlayerStatistics GetPlayerStats(ulong playerId)
        {
            if (!playerStats.ContainsKey(playerId))
            {
                InitializePlayerStats(playerId);
            }

            return playerStats[playerId];
        }

        public SessionStatistics GetCurrentSession(ulong playerId)
        {
            return currentSessions.ContainsKey(playerId) ? currentSessions[playerId] : null;
        }

        public List<SessionStatistics> GetSessionHistory(ulong playerId, int count = 10)
        {
            if (!playerStats.ContainsKey(playerId)) return new List<SessionStatistics>();

            return playerStats[playerId].sessionHistory
                .OrderByDescending(s => s.sessionStart)
                .Take(count)
                .ToList();
        }

        public List<LeaderboardEntry> GetLeaderboard(LeaderboardType type)
        {
            return leaderboards.ContainsKey(type)
                ? new List<LeaderboardEntry>(leaderboards[type])
                : new List<LeaderboardEntry>();
        }

        public int GetLeaderboardRank(ulong playerId, LeaderboardType type)
        {
            if (!leaderboards.ContainsKey(type)) return -1;

            var entry = leaderboards[type].FirstOrDefault(e => e.playerId == playerId);
            return entry != null ? entry.rank : -1;
        }

        public float GetKDRatio(ulong playerId)
        {
            return GetPlayerStats(playerId).combat.kdRatio;
        }

        public float GetExtractionRate(ulong playerId)
        {
            return GetPlayerStats(playerId).survival.extractionRate;
        }

        public string GetFavoriteWeapon(ulong playerId)
        {
            var stats = GetPlayerStats(playerId);

            if (stats.combat.weaponKills.Count == 0) return "None";

            return stats.combat.weaponKills
                .OrderByDescending(kvp => kvp.Value)
                .First()
                .Key;
        }

        public Dictionary<string, int> GetWeaponKills(ulong playerId)
        {
            return new Dictionary<string, int>(GetPlayerStats(playerId).combat.weaponKills);
        }

        #endregion
    }

    #region Data Classes

    public class PlayerStatistics
    {
        public ulong playerId;
        public LifetimeStats lifetime;
        public CombatStats combat;
        public SurvivalStats survival;
        public EconomyStats economy;
        public SocialStats social;
        public List<SessionStatistics> sessionHistory;
    }

    public class LifetimeStats
    {
        public float totalPlayTimeSeconds;
        public int totalMissionsCompleted;
        public int totalAchievementsUnlocked;
        public DateTime accountCreated;
        public DateTime lastPlayed;
    }

    public class CombatStats
    {
        public int totalKills;
        public int totalDeaths;
        public int totalAssists;
        public float kdRatio;
        public float totalDamageDealt;
        public float totalDamageTaken;
        public int totalHeadshots;
        public int longestKillStreak;
        public Dictionary<string, int> weaponKills = new Dictionary<string, int>();
    }

    public class SurvivalStats
    {
        public int totalExtractions;
        public int successfulExtractions;
        public int failedExtractions;
        public float extractionRate;
        public float totalDistanceTraveled;
        public int timesRevived;
        public int timesIncapacitated;
        public float longestSurvivalTime;
    }

    public class EconomyStats
    {
        public int totalCurrencyEarned;
        public int totalCurrencySpent;
        public int totalItemsLooted;
        public int totalItemsCrafted;
        public int totalTradesMade;
        public int mostValuableItemFound;
    }

    public class SocialStats
    {
        public int totalFriends;
        public int totalClanMembers;
        public int partiesJoined;
        public int matchesWithFriends;
        public int messagesSeint;
    }

    public class SessionStatistics
    {
        public ulong playerId;
        public DateTime sessionStart;
        public DateTime sessionEnd;
        public float duration;
        public int kills;
        public int deaths;
        public int assists;
        public float damageDealt;
        public float damageTaken;
        public float distanceTraveled;
        public int itemsLooted;
        public int currencyEarned;
        public int missionsCompleted;
    }

    public class LeaderboardEntry
    {
        public int rank;
        public ulong playerId;
        public float score;
    }

    public enum StatType
    {
        Kills,
        Deaths,
        Assists,
        DamageDealt,
        DamageTaken,
        Headshots,
        DistanceTraveled,
        SuccessfulExtractions,
        ItemsLooted,
        CurrencyEarned,
        CurrencySpent,
        MissionsCompleted,
        PlayTime
    }

    public enum LeaderboardType
    {
        Kills,
        KDRatio,
        DamageDealt,
        Headshots,
        ExtractionRate,
        CurrencyEarned,
        PlayTime,
        MissionsCompleted
    }

    #endregion
}
