using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Leaderboards
{
    /// <summary>
    /// Comprehensive leaderboard system with global, regional, clan, and friend leaderboards
    /// across multiple stat categories with seasonal resets.
    /// </summary>
    public class LeaderboardSystem : NetworkBehaviour
    {
        public static LeaderboardSystem Instance { get; private set; }

        [Header("Leaderboard Configuration")]
        [SerializeField] private int entriesPerPage = 50;
        [SerializeField] private int topEntriesCache = 100;

        private Dictionary<LeaderboardType, Dictionary<ulong, LeaderboardEntry>> leaderboards = new Dictionary<LeaderboardType, Dictionary<ulong, LeaderboardEntry>>();
        private Dictionary<LeaderboardType, List<LeaderboardEntry>> cachedRankings = new Dictionary<LeaderboardType, List<LeaderboardEntry>>();
        private int currentSeason = 1;

        public event Action<LeaderboardType> OnLeaderboardUpdated;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer)
            {
                InitializeLeaderboards();
            }
        }

        private void InitializeLeaderboards()
        {
            foreach (LeaderboardType type in Enum.GetValues(typeof(LeaderboardType)))
            {
                leaderboards[type] = new Dictionary<ulong, LeaderboardEntry>();
                cachedRankings[type] = new List<LeaderboardEntry>();
            }

            Debug.Log($"Initialized {leaderboards.Count} leaderboard types");
        }

        [ServerRpc(RequireOwnership = false)]
        public void UpdateLeaderboardScoreServerRpc(ulong playerId, LeaderboardType type, long score, string playerName, ServerRpcParams rpcParams = default)
        {
            if (!leaderboards.TryGetValue(type, out var leaderboard)) return;

            if (!leaderboard.TryGetValue(playerId, out var entry))
            {
                entry = new LeaderboardEntry
                {
                    playerId = playerId,
                    playerName = playerName,
                    score = score,
                    season = currentSeason,
                    lastUpdated = DateTime.UtcNow,
                    rank = 0
                };
                leaderboard[playerId] = entry;
            }
            else
            {
                // Only update if score is better
                bool shouldUpdate = false;

                switch (type)
                {
                    case LeaderboardType.TotalKills:
                    case LeaderboardType.ZombieKills:
                    case LeaderboardType.BossKills:
                    case LeaderboardType.PlayerKills:
                    case LeaderboardType.Level:
                    case LeaderboardType.ArenaRank:
                    case LeaderboardType.Wealth:
                    case LeaderboardType.PlayTime:
                    case LeaderboardType.ClanLevel:
                        shouldUpdate = score > entry.score; // Higher is better
                        break;
                    case LeaderboardType.SpeedrunTime:
                        shouldUpdate = score < entry.score || entry.score == 0; // Lower is better
                        break;
                }

                if (shouldUpdate)
                {
                    entry.score = score;
                    entry.lastUpdated = DateTime.UtcNow;
                }
            }

            // Mark for recalculation
            cachedRankings[type].Clear();
        }

        [ServerRpc(RequireOwnership = false)]
        public void RecalculateLeaderboardServerRpc(LeaderboardType type, ServerRpcParams rpcParams = default)
        {
            if (!leaderboards.TryGetValue(type, out var leaderboard)) return;

            // Sort entries by score
            var sorted = leaderboard.Values.OrderByDescending(e => e.score).ToList();

            // Assign ranks
            for (int i = 0; i < sorted.Count; i++)
            {
                sorted[i].rank = i + 1;
            }

            // Cache top entries
            cachedRankings[type] = sorted.Take(topEntriesCache).ToList();

            OnLeaderboardUpdated?.Invoke(type);
            Debug.Log($"Recalculated {type} leaderboard with {sorted.Count} entries");
        }

        public List<LeaderboardEntry> GetTopPlayers(LeaderboardType type, int count)
        {
            if (!cachedRankings.TryGetValue(type, out var rankings) || rankings.Count == 0)
            {
                RecalculateLeaderboardServerRpc(type);
                rankings = cachedRankings.GetValueOrDefault(type, new List<LeaderboardEntry>());
            }

            return rankings.Take(count).ToList();
        }

        public LeaderboardEntry GetPlayerEntry(ulong playerId, LeaderboardType type)
        {
            if (!leaderboards.TryGetValue(type, out var leaderboard)) return null;
            return leaderboard.GetValueOrDefault(playerId);
        }

        public int GetPlayerRank(ulong playerId, LeaderboardType type)
        {
            var entry = GetPlayerEntry(playerId, type);
            return entry?.rank ?? 0;
        }

        public List<LeaderboardEntry> GetPlayersNearRank(ulong playerId, LeaderboardType type, int range)
        {
            var entry = GetPlayerEntry(playerId, type);
            if (entry == null) return new List<LeaderboardEntry>();

            if (!cachedRankings.TryGetValue(type, out var rankings) || rankings.Count == 0)
            {
                RecalculateLeaderboardServerRpc(type);
                rankings = cachedRankings.GetValueOrDefault(type, new List<LeaderboardEntry>());
            }

            int startRank = Math.Max(1, entry.rank - range);
            int endRank = Math.Min(rankings.Count, entry.rank + range);

            return rankings.Where(e => e.rank >= startRank && e.rank <= endRank).ToList();
        }

        [ServerRpc(RequireOwnership = false)]
        public void ResetSeasonalLeaderboardsServerRpc(ServerRpcParams rpcParams = default)
        {
            currentSeason++;

            foreach (var type in new[] { LeaderboardType.ArenaRank, LeaderboardType.PlayerKills })
            {
                if (leaderboards.TryGetValue(type, out var leaderboard))
                {
                    leaderboard.Clear();
                    cachedRankings[type].Clear();
                }
            }

            Debug.Log($"Reset seasonal leaderboards for season {currentSeason}");
        }
    }

    [Serializable]
    public class LeaderboardEntry
    {
        public ulong playerId;
        public string playerName;
        public long score;
        public int rank;
        public int season;
        public DateTime lastUpdated;
        public string clanTag;
        public int level;
    }

    public enum LeaderboardType
    {
        // Combat
        TotalKills,
        ZombieKills,
        BossKills,
        PlayerKills,
        
        // Progression
        Level,
        ArenaRank,
        Wealth,
        
        // Activity
        PlayTime,
        
        // Special
        SpeedrunTime,
        ClanLevel
    }
}
