using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace DeadFrontier.Core.Social
{
    /// <summary>
    /// Manages global and friends leaderboards for competitive features
    /// Supports multiple leaderboard types (kills, wins, level, etc.)
    /// </summary>
    public class LeaderboardManager : Singleton<LeaderboardManager>
    {
        [Header("Leaderboard Settings")]
        [SerializeField] private int maxEntriesPerLeaderboard = 100;
        [SerializeField] private float updateInterval = 60f; // Update every minute
        [SerializeField] private bool enableLocalLeaderboards = true; // For testing without backend

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        // Leaderboards
        private Dictionary<LeaderboardType, List<LeaderboardEntry>> leaderboards = new Dictionary<LeaderboardType, List<LeaderboardEntry>>();
        private float updateTimer = 0f;

        // Player data
        private string localPlayerId;
        private string localPlayerName;

        // Events
        public event System.Action<LeaderboardType> OnLeaderboardUpdated;
        public event System.Action<LeaderboardType, int> OnRankChanged; // type, new rank

        protected override void Awake()
        {
            base.Awake();
            InitializeLeaderboards();
            LoadLocalPlayerId();
        }

        private void Update()
        {
            updateTimer += Time.deltaTime;
            if (updateTimer >= updateInterval)
            {
                updateTimer = 0f;
                UpdateAllLeaderboards();
            }
        }

        #region Initialization

        private void InitializeLeaderboards()
        {
            foreach (LeaderboardType type in System.Enum.GetValues(typeof(LeaderboardType)))
            {
                leaderboards[type] = new List<LeaderboardEntry>();
            }

            if (showDebugLogs)
                Debug.Log("[LeaderboardManager] Initialized leaderboards");
        }

        private void LoadLocalPlayerId()
        {
            // Get player ID from authentication or generate one
            localPlayerId = PlayerPrefs.GetString("PlayerId", System.Guid.NewGuid().ToString());
            PlayerPrefs.SetString("PlayerId", localPlayerId);

            localPlayerName = PlayerPrefs.GetString("PlayerName", $"Player{UnityEngine.Random.Range(1000, 9999)}");
            PlayerPrefs.Save();

            if (showDebugLogs)
                Debug.Log($"[LeaderboardManager] Player ID: {localPlayerId}, Name: {localPlayerName}");
        }

        #endregion

        #region Leaderboard Updates

        /// <summary>
        /// Updates all leaderboards from backend or local data
        /// </summary>
        public void UpdateAllLeaderboards()
        {
            foreach (LeaderboardType type in System.Enum.GetValues(typeof(LeaderboardType)))
            {
                UpdateLeaderboard(type);
            }
        }

        /// <summary>
        /// Updates a specific leaderboard
        /// </summary>
        public void UpdateLeaderboard(LeaderboardType type)
        {
            if (enableLocalLeaderboards)
            {
                UpdateLocalLeaderboard(type);
            }
            else
            {
                // TODO: Fetch from backend
                FetchLeaderboardFromBackend(type);
            }
        }

        private void UpdateLocalLeaderboard(LeaderboardType type)
        {
            // For local testing, generate or update from player stats
            var entries = new List<LeaderboardEntry>();

            // Get local player's stats
            var stats = GetLocalPlayerStats();
            int score = GetScoreForType(type, stats);

            var localEntry = new LeaderboardEntry
            {
                playerId = localPlayerId,
                playerName = localPlayerName,
                score = score,
                rank = 1 // Will be calculated
            };

            entries.Add(localEntry);

            // Add some dummy entries for testing
            for (int i = 0; i < 20; i++)
            {
                entries.Add(new LeaderboardEntry
                {
                    playerId = $"player_{i}",
                    playerName = $"Player{i}",
                    score = UnityEngine.Random.Range(0, 10000),
                    rank = i + 1
                });
            }

            // Sort by score
            entries = entries.OrderByDescending(e => e.score).ToList();

            // Assign ranks
            for (int i = 0; i < entries.Count; i++)
            {
                entries[i].rank = i + 1;
            }

            // Limit entries
            if (entries.Count > maxEntriesPerLeaderboard)
            {
                entries = entries.Take(maxEntriesPerLeaderboard).ToList();
            }

            leaderboards[type] = entries;
            OnLeaderboardUpdated?.Invoke(type);

            if (showDebugLogs)
                Debug.Log($"[LeaderboardManager] Updated {type} leaderboard. Entries: {entries.Count}");
        }

        private void FetchLeaderboardFromBackend(LeaderboardType type)
        {
            // TODO: Implement backend integration
            // This would use Unity Gaming Services Leaderboards or custom backend

            if (showDebugLogs)
                Debug.Log($"[LeaderboardManager] Fetching {type} leaderboard from backend...");

            // Placeholder - in production this would be an async call
            UpdateLocalLeaderboard(type); // Fallback to local for now
        }

        #endregion

        #region Score Submission

        /// <summary>
        /// Submits a score to leaderboards
        /// </summary>
        public void SubmitScore(LeaderboardType type, int score)
        {
            if (enableLocalLeaderboards)
            {
                SubmitLocalScore(type, score);
            }
            else
            {
                SubmitScoreToBackend(type, score);
            }
        }

        private void SubmitLocalScore(LeaderboardType type, int score)
        {
            if (!leaderboards.ContainsKey(type))
                return;

            // Find or create player entry
            var entry = leaderboards[type].FirstOrDefault(e => e.playerId == localPlayerId);
            int oldRank = entry != null ? entry.rank : 0;

            if (entry != null)
            {
                // Update existing entry
                entry.score = score;
            }
            else
            {
                // Create new entry
                entry = new LeaderboardEntry
                {
                    playerId = localPlayerId,
                    playerName = localPlayerName,
                    score = score
                };
                leaderboards[type].Add(entry);
            }

            // Re-sort and update ranks
            leaderboards[type] = leaderboards[type].OrderByDescending(e => e.score).ToList();
            for (int i = 0; i < leaderboards[type].Count; i++)
            {
                leaderboards[type][i].rank = i + 1;
            }

            // Check if rank changed
            int newRank = leaderboards[type].FindIndex(e => e.playerId == localPlayerId) + 1;
            if (newRank != oldRank && oldRank > 0)
            {
                OnRankChanged?.Invoke(type, newRank);

                if (showDebugLogs)
                    Debug.Log($"[LeaderboardManager] Rank changed: {oldRank} → {newRank}");
            }

            // Track analytics
            Analytics.AnalyticsManager.Instance?.TrackEvent("leaderboard_score_submit", new Dictionary<string, object>
            {
                { "leaderboard_type", type.ToString() },
                { "score", score },
                { "rank", newRank }
            });
        }

        private void SubmitScoreToBackend(LeaderboardType type, int score)
        {
            // TODO: Implement backend submission
            if (showDebugLogs)
                Debug.Log($"[LeaderboardManager] Submitting {score} to {type} leaderboard...");

            SubmitLocalScore(type, score); // Fallback
        }

        #endregion

        #region Queries

        /// <summary>
        /// Gets leaderboard entries
        /// </summary>
        public List<LeaderboardEntry> GetLeaderboard(LeaderboardType type, int maxEntries = 100)
        {
            if (!leaderboards.ContainsKey(type))
                return new List<LeaderboardEntry>();

            return leaderboards[type].Take(maxEntries).ToList();
        }

        /// <summary>
        /// Gets player's rank on a leaderboard
        /// </summary>
        public int GetPlayerRank(LeaderboardType type, string playerId = null)
        {
            if (string.IsNullOrEmpty(playerId))
                playerId = localPlayerId;

            if (!leaderboards.ContainsKey(type))
                return -1;

            var entry = leaderboards[type].FirstOrDefault(e => e.playerId == playerId);
            return entry != null ? entry.rank : -1;
        }

        /// <summary>
        /// Gets player's score on a leaderboard
        /// </summary>
        public int GetPlayerScore(LeaderboardType type, string playerId = null)
        {
            if (string.IsNullOrEmpty(playerId))
                playerId = localPlayerId;

            if (!leaderboards.ContainsKey(type))
                return 0;

            var entry = leaderboards[type].FirstOrDefault(e => e.playerId == playerId);
            return entry != null ? entry.score : 0;
        }

        /// <summary>
        /// Gets entries around player's rank
        /// </summary>
        public List<LeaderboardEntry> GetEntriesAroundPlayer(LeaderboardType type, int range = 5)
        {
            if (!leaderboards.ContainsKey(type))
                return new List<LeaderboardEntry>();

            int playerRank = GetPlayerRank(type);
            if (playerRank < 0)
                return new List<LeaderboardEntry>();

            int startIndex = Mathf.Max(0, playerRank - range - 1);
            int count = range * 2 + 1;

            return leaderboards[type].Skip(startIndex).Take(count).ToList();
        }

        /// <summary>
        /// Gets top N entries
        /// </summary>
        public List<LeaderboardEntry> GetTopEntries(LeaderboardType type, int count = 10)
        {
            if (!leaderboards.ContainsKey(type))
                return new List<LeaderboardEntry>();

            return leaderboards[type].Take(count).ToList();
        }

        #endregion

        #region Helper Methods

        private Save.PlayerStatsData GetLocalPlayerStats()
        {
            if (Save.SaveSystem.Instance != null && Save.SaveSystem.Instance.CurrentSave != null)
            {
                return Save.SaveSystem.Instance.CurrentSave.stats;
            }

            return new Save.PlayerStatsData();
        }

        private int GetScoreForType(LeaderboardType type, Save.PlayerStatsData stats)
        {
            switch (type)
            {
                case LeaderboardType.TotalKills:
                    return stats.totalKills;

                case LeaderboardType.ZombieKills:
                    return stats.zombiesKilled;

                case LeaderboardType.Wins:
                    return stats.matchesWon;

                case LeaderboardType.ExtractionRate:
                    int total = stats.successfulExtractions + stats.failedExtractions;
                    return total > 0 ? (int)((float)stats.successfulExtractions / total * 100) : 0;

                case LeaderboardType.Level:
                    var progression = GameObject.FindObjectOfType<Player.PlayerProgression>();
                    return progression != null ? progression.Level : 1;

                case LeaderboardType.DamageDealt:
                    return (int)stats.totalDamageDealt;

                case LeaderboardType.Headshots:
                    return stats.headshotKills;

                case LeaderboardType.WinStreak:
                    // Would need to track this separately
                    return 0;

                default:
                    return 0;
            }
        }

        #endregion

        #region Public API

        public string LocalPlayerId => localPlayerId;
        public string LocalPlayerName
        {
            get => localPlayerName;
            set
            {
                localPlayerName = value;
                PlayerPrefs.SetString("PlayerName", value);
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// Forces an immediate update of all leaderboards
        /// </summary>
        public void ForceUpdateAllLeaderboards()
        {
            updateTimer = updateInterval; // Trigger update on next frame
        }

        #endregion
    }

    #region Data Structures

    public enum LeaderboardType
    {
        TotalKills,
        ZombieKills,
        Wins,
        ExtractionRate,
        Level,
        DamageDealt,
        Headshots,
        WinStreak
    }

    [System.Serializable]
    public class LeaderboardEntry
    {
        public string playerId;
        public string playerName;
        public int score;
        public int rank;
        public string avatarUrl;

        // Additional stats
        public int level;
        public int prestigeLevel;
    }

    #endregion
}
