using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Statistics
{
    /// <summary>
    /// Match history system recording detailed statistics and results from each
    /// game session for player review and analysis.
    /// </summary>
    public class MatchHistorySystem : NetworkBehaviour
    {
        public static MatchHistorySystem Instance { get; private set; }

        [Header("Match History Configuration")]
        [SerializeField] private int maxMatchesPerPlayer = 100;
        [SerializeField] private bool enableDetailedStats = true;

        private Dictionary<ulong, List<MatchRecord>> playerMatchHistory = new Dictionary<ulong, List<MatchRecord>>();
        private MatchRecord currentMatch;

        public event Action<string> OnMatchStarted;
        public event Action<string, MatchResult> OnMatchEnded;
        public event Action<ulong, MatchRecord> OnMatchRecordSaved;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        /// <summary>
        /// Start recording a new match
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void StartMatchRecordingServerRpc(string matchId, string gameMode, string mapName, ServerRpcParams rpcParams = default)
        {
            currentMatch = new MatchRecord
            {
                matchId = matchId,
                gameMode = gameMode,
                mapName = mapName,
                startTime = DateTime.UtcNow,
                playerStats = new Dictionary<ulong, PlayerMatchStats>()
            };

            // Initialize stats for all players
            foreach (var client in NetworkManager.Singleton.ConnectedClients)
            {
                ulong playerId = client.Key;
                currentMatch.playerStats[playerId] = new PlayerMatchStats
                {
                    playerId = playerId,
                    kills = 0,
                    deaths = 0,
                    assists = 0,
                    damageDealt = 0,
                    damageTaken = 0,
                    headshotKills = 0,
                    roundsSurvived = 0,
                    xpEarned = 0,
                    scoreEarned = 0
                };
            }

            OnMatchStarted?.Invoke(matchId);
        }

        /// <summary>
        /// Record player stats during match
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void RecordPlayerKillServerRpc(ulong playerId, bool wasHeadshot, ServerRpcParams rpcParams = default)
        {
            if (currentMatch == null) return;
            if (!currentMatch.playerStats.ContainsKey(playerId)) return;

            var stats = currentMatch.playerStats[playerId];
            stats.kills++;
            if (wasHeadshot) stats.headshotKills++;
            stats.longestKillStreak = Mathf.Max(stats.longestKillStreak, stats.currentKillStreak + 1);
            stats.currentKillStreak++;
        }

        [ServerRpc(RequireOwnership = false)]
        public void RecordPlayerDeathServerRpc(ulong playerId, ServerRpcParams rpcParams = default)
        {
            if (currentMatch == null) return;
            if (!currentMatch.playerStats.ContainsKey(playerId)) return;

            var stats = currentMatch.playerStats[playerId];
            stats.deaths++;
            stats.currentKillStreak = 0;
        }

        [ServerRpc(RequireOwnership = false)]
        public void RecordPlayerAssistServerRpc(ulong playerId, ServerRpcParams rpcParams = default)
        {
            if (currentMatch == null) return;
            if (!currentMatch.playerStats.ContainsKey(playerId)) return;

            currentMatch.playerStats[playerId].assists++;
        }

        [ServerRpc(RequireOwnership = false)]
        public void RecordPlayerDamageServerRpc(ulong playerId, float damageDealt, float damageTaken, ServerRpcParams rpcParams = default)
        {
            if (currentMatch == null) return;
            if (!currentMatch.playerStats.ContainsKey(playerId)) return;

            var stats = currentMatch.playerStats[playerId];
            stats.damageDealt += damageDealt;
            stats.damageTaken += damageTaken;
        }

        [ServerRpc(RequireOwnership = false)]
        public void RecordRoundSurvivedServerRpc(ulong playerId, ServerRpcParams rpcParams = default)
        {
            if (currentMatch == null) return;
            if (!currentMatch.playerStats.ContainsKey(playerId)) return;

            currentMatch.playerStats[playerId].roundsSurvived++;
        }

        [ServerRpc(RequireOwnership = false)]
        public void RecordPlayerRewardsServerRpc(ulong playerId, int xp, int score, ServerRpcParams rpcParams = default)
        {
            if (currentMatch == null) return;
            if (!currentMatch.playerStats.ContainsKey(playerId)) return;

            var stats = currentMatch.playerStats[playerId];
            stats.xpEarned += xp;
            stats.scoreEarned += score;
        }

        /// <summary>
        /// End match and save to history
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void EndMatchRecordingServerRpc(MatchResult result, ulong? mvpPlayerId, ServerRpcParams rpcParams = default)
        {
            if (currentMatch == null) return;

            currentMatch.endTime = DateTime.UtcNow;
            currentMatch.duration = (currentMatch.endTime - currentMatch.startTime).TotalSeconds;
            currentMatch.result = result;
            currentMatch.mvpPlayerId = mvpPlayerId ?? 0;

            // Calculate additional stats
            CalculateMatchStats();

            // Save to each player's history
            foreach (var kvp in currentMatch.playerStats)
            {
                ulong playerId = kvp.Key;
                SaveMatchToPlayerHistory(playerId, currentMatch);
            }

            OnMatchEnded?.Invoke(currentMatch.matchId, result);

            // Clear current match
            currentMatch = null;
        }

        private void CalculateMatchStats()
        {
            // Calculate accuracy, KDA, etc.
            foreach (var kvp in currentMatch.playerStats)
            {
                var stats = kvp.Value;

                // KDA = (Kills + Assists) / Deaths
                stats.kda = stats.deaths > 0 ?
                    (stats.kills + stats.assists) / (float)stats.deaths :
                    stats.kills + stats.assists;

                // Headshot accuracy
                stats.headshotAccuracy = stats.kills > 0 ?
                    (stats.headshotKills / (float)stats.kills) * 100f : 0f;
            }
        }

        private void SaveMatchToPlayerHistory(ulong playerId, MatchRecord match)
        {
            if (!playerMatchHistory.ContainsKey(playerId))
            {
                playerMatchHistory[playerId] = new List<MatchRecord>();
            }

            var history = playerMatchHistory[playerId];

            // Create a copy for this player
            MatchRecord playerRecord = new MatchRecord
            {
                matchId = match.matchId,
                gameMode = match.gameMode,
                mapName = match.mapName,
                startTime = match.startTime,
                endTime = match.endTime,
                duration = match.duration,
                result = match.result,
                mvpPlayerId = match.mvpPlayerId,
                playerStats = new Dictionary<ulong, PlayerMatchStats>()
            };

            // Only include this player's stats in their record
            if (match.playerStats.ContainsKey(playerId))
            {
                playerRecord.playerStats[playerId] = match.playerStats[playerId];
            }

            history.Add(playerRecord);

            // Limit history size
            if (history.Count > maxMatchesPerPlayer)
            {
                history.RemoveAt(0);
            }

            OnMatchRecordSaved?.Invoke(playerId, playerRecord);
        }

        /// <summary>
        /// Get player's match history
        /// </summary>
        public List<MatchRecord> GetPlayerMatchHistory(ulong playerId, int limit = 10)
        {
            if (!playerMatchHistory.ContainsKey(playerId)) return new List<MatchRecord>();

            return playerMatchHistory[playerId]
                .OrderByDescending(m => m.startTime)
                .Take(limit)
                .ToList();
        }

        /// <summary>
        /// Get player's win/loss record
        /// </summary>
        public (int wins, int losses, int draws) GetPlayerRecord(ulong playerId)
        {
            if (!playerMatchHistory.ContainsKey(playerId)) return (0, 0, 0);

            int wins = 0, losses = 0, draws = 0;

            foreach (var match in playerMatchHistory[playerId])
            {
                switch (match.result)
                {
                    case MatchResult.Victory: wins++; break;
                    case MatchResult.Defeat: losses++; break;
                    case MatchResult.Draw: draws++; break;
                }
            }

            return (wins, losses, draws);
        }

        /// <summary>
        /// Get player's average stats across all matches
        /// </summary>
        public PlayerMatchStats GetAverageStats(ulong playerId)
        {
            if (!playerMatchHistory.ContainsKey(playerId)) return new PlayerMatchStats();

            var history = playerMatchHistory[playerId];
            if (history.Count == 0) return new PlayerMatchStats();

            PlayerMatchStats avgStats = new PlayerMatchStats { playerId = playerId };
            int count = 0;

            foreach (var match in history)
            {
                if (match.playerStats.ContainsKey(playerId))
                {
                    var stats = match.playerStats[playerId];
                    avgStats.kills += stats.kills;
                    avgStats.deaths += stats.deaths;
                    avgStats.assists += stats.assists;
                    avgStats.damageDealt += stats.damageDealt;
                    avgStats.damageTaken += stats.damageTaken;
                    avgStats.headshotKills += stats.headshotKills;
                    avgStats.roundsSurvived += stats.roundsSurvived;
                    count++;
                }
            }

            if (count > 0)
            {
                avgStats.kills = Mathf.RoundToInt(avgStats.kills / (float)count);
                avgStats.deaths = Mathf.RoundToInt(avgStats.deaths / (float)count);
                avgStats.assists = Mathf.RoundToInt(avgStats.assists / (float)count);
                avgStats.damageDealt /= count;
                avgStats.damageTaken /= count;
                avgStats.headshotKills = Mathf.RoundToInt(avgStats.headshotKills / (float)count);
                avgStats.roundsSurvived = Mathf.RoundToInt(avgStats.roundsSurvived / (float)count);
            }

            return avgStats;
        }

        public MatchRecord GetCurrentMatch()
        {
            return currentMatch;
        }

        [Serializable]
        public class MatchRecord
        {
            public string matchId;
            public string gameMode;
            public string mapName;
            public DateTime startTime;
            public DateTime endTime;
            public double duration;
            public MatchResult result;
            public ulong mvpPlayerId;
            public Dictionary<ulong, PlayerMatchStats> playerStats;
        }

        [Serializable]
        public class PlayerMatchStats
        {
            public ulong playerId;
            public int kills;
            public int deaths;
            public int assists;
            public float kda;
            public float damageDealt;
            public float damageTaken;
            public int headshotKills;
            public float headshotAccuracy;
            public int roundsSurvived;
            public int longestKillStreak;
            public int currentKillStreak;
            public int xpEarned;
            public int scoreEarned;
        }

        public enum MatchResult
        {
            Victory,
            Defeat,
            Draw,
            Abandoned
        }
    }
}
