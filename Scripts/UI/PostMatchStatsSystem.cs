using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.UI
{
    /// <summary>
    /// Post-match statistics screen showing detailed player performance,
    /// awards, progression, and match summary with visual medals and badges.
    /// </summary>
    public class PostMatchStatsSystem : NetworkBehaviour
    {
        public static PostMatchStatsSystem Instance { get; private set; }

        [Header("Post Match Configuration")]
        [SerializeField] private float displayDuration = 30f;
        [SerializeField] private bool enableSkipOption = true;

        private PostMatchReport currentReport;

        public event Action<PostMatchReport> OnPostMatchReportGenerated;
        public event Action OnPostMatchScreenClosed;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        /// <summary>
        /// Generate and show post-match report
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void GeneratePostMatchReportServerRpc(string matchId, ServerRpcParams rpcParams = default)
        {
            PostMatchReport report = new PostMatchReport
            {
                matchId = matchId,
                matchEndTime = DateTime.UtcNow,
                playerStats = new List<PlayerMatchPerformance>()
            };

            // Collect stats from match history
            if (Statistics.MatchHistorySystem.Instance != null)
            {
                var matchRecord = Statistics.MatchHistorySystem.Instance.GetCurrentMatch();
                if (matchRecord != null)
                {
                    // Convert match stats to post-match performance
                    foreach (var kvp in matchRecord.playerStats)
                    {
                        var stats = kvp.Value;
                        var performance = new PlayerMatchPerformance
                        {
                            playerId = kvp.Key,
                            kills = stats.kills,
                            deaths = stats.deaths,
                            assists = stats.assists,
                            kda = stats.kda,
                            damageDealt = stats.damageDealt,
                            damageTaken = stats.damageTaken,
                            headshotKills = stats.headshotKills,
                            headshotAccuracy = stats.headshotAccuracy,
                            roundsSurvived = stats.roundsSurvived,
                            longestKillStreak = stats.longestKillStreak,
                            xpEarned = stats.xpEarned,
                            currencyEarned = stats.scoreEarned,
                            awards = new List<MatchAward>()
                        };

                        // Calculate awards
                        performance.awards = CalculatePlayerAwards(performance);

                        report.playerStats.Add(performance);
                    }
                }
            }

            // Determine team winner
            report.winningTeam = DetermineWinningTeam(report.playerStats);

            // Calculate team stats
            report.teamStats = CalculateTeamStats(report.playerStats);

            // Determine MVP
            report.mvpPlayerId = DetermineMVP(report.playerStats);

            currentReport = report;
            OnPostMatchReportGenerated?.Invoke(report);

            ShowPostMatchScreenClientRpc(report);
        }

        private List<MatchAward> CalculatePlayerAwards(PlayerMatchPerformance performance)
        {
            List<MatchAward> awards = new List<MatchAward>();

            // Kill-based awards
            if (performance.kills >= 50)
            {
                awards.Add(new MatchAward { awardType = AwardType.KillingMachine, description = "50+ Kills" });
            }
            if (performance.kills >= 100)
            {
                awards.Add(new MatchAward { awardType = AwardType.Massacre, description = "100+ Kills" });
            }

            // Headshot awards
            if (performance.headshotAccuracy >= 50f)
            {
                awards.Add(new MatchAward { awardType = AwardType.Sharpshooter, description = "50%+ Headshot Accuracy" });
            }
            if (performance.headshotKills >= 30)
            {
                awards.Add(new MatchAward { awardType = AwardType.Deadeye, description = "30+ Headshots" });
            }

            // KDA awards
            if (performance.kda >= 5f)
            {
                awards.Add(new MatchAward { awardType = AwardType.Dominator, description = "5.0+ KDA" });
            }
            if (performance.kda >= 10f)
            {
                awards.Add(new MatchAward { awardType = AwardType.Untouchable, description = "10.0+ KDA" });
            }

            // Survival awards
            if (performance.roundsSurvived >= 30)
            {
                awards.Add(new MatchAward { awardType = AwardType.Survivor, description = "30+ Rounds Survived" });
            }

            // Kill streak awards
            if (performance.longestKillStreak >= 10)
            {
                awards.Add(new MatchAward { awardType = AwardType.Unstoppable, description = "10+ Kill Streak" });
            }

            // Damage awards
            if (performance.damageDealt >= 50000f)
            {
                awards.Add(new MatchAward { awardType = AwardType.HighDamage, description = "50k+ Damage Dealt" });
            }

            // Support awards
            if (performance.assists >= 20)
            {
                awards.Add(new MatchAward { awardType = AwardType.Teamplayer, description = "20+ Assists" });
            }

            // Perfect game
            if (performance.deaths == 0 && performance.kills >= 10)
            {
                awards.Add(new MatchAward { awardType = AwardType.Flawless, description = "No Deaths" });
            }

            return awards;
        }

        private int DetermineWinningTeam(List<PlayerMatchPerformance> playerStats)
        {
            // Would integrate with actual team system
            return 1; // Placeholder
        }

        private Dictionary<int, TeamStats> CalculateTeamStats(List<PlayerMatchPerformance> playerStats)
        {
            Dictionary<int, TeamStats> teamStats = new Dictionary<int, TeamStats>();

            // Aggregate team stats
            foreach (var player in playerStats)
            {
                int teamId = 1; // Would get actual team

                if (!teamStats.ContainsKey(teamId))
                {
                    teamStats[teamId] = new TeamStats { teamId = teamId };
                }

                var team = teamStats[teamId];
                team.totalKills += player.kills;
                team.totalDeaths += player.deaths;
                team.totalDamage += player.damageDealt;
                team.playerCount++;
            }

            return teamStats;
        }

        private ulong DetermineMVP(List<PlayerMatchPerformance> playerStats)
        {
            if (playerStats.Count == 0) return 0;

            // Calculate MVP score
            var mvp = playerStats.OrderByDescending(p =>
            {
                float score = 0f;
                score += p.kills * 10f;
                score += p.assists * 3f;
                score -= p.deaths * 5f;
                score += p.damageDealt / 100f;
                score += p.headshotKills * 5f;
                score += p.roundsSurvived * 2f;
                return score;
            }).First();

            return mvp.playerId;
        }

        [ClientRpc]
        private void ShowPostMatchScreenClientRpc(PostMatchReport report)
        {
            DisplayPostMatchScreen(report);
        }

        private void DisplayPostMatchScreen(PostMatchReport report)
        {
            Debug.Log("<color=cyan>═══════════════════════════════════════</color>");
            Debug.Log($"<color=gold>        POST-MATCH STATISTICS       </color>");
            Debug.Log("<color=cyan>═══════════════════════════════════════</color>");

            // MVP
            Debug.Log($"<color=yellow>★ MVP: Player {report.mvpPlayerId} ★</color>");
            Debug.Log("");

            // Player stats
            foreach (var player in report.playerStats.OrderByDescending(p => p.kills))
            {
                Debug.Log($"<color=white>Player {player.playerId}:</color>");
                Debug.Log($"  K/D/A: {player.kills}/{player.deaths}/{player.assists} (KDA: {player.kda:F2})");
                Debug.Log($"  Damage: {player.damageDealt:F0} dealt, {player.damageTaken:F0} taken");
                Debug.Log($"  Headshots: {player.headshotKills} ({player.headshotAccuracy:F1}%)");
                Debug.Log($"  Best Streak: {player.longestKillStreak}");
                Debug.Log($"  Rewards: +{player.xpEarned} XP, +{player.currencyEarned} Currency");

                if (player.awards.Count > 0)
                {
                    Debug.Log($"  <color=gold>Awards: {string.Join(", ", player.awards.Select(a => a.awardType))}</color>");
                }

                Debug.Log("");
            }

            Debug.Log("<color=cyan>═══════════════════════════════════════</color>");

            if (enableSkipOption)
            {
                Debug.Log("<color=gray>Press SPACE to continue...</color>");
            }
        }

        public PostMatchReport GetCurrentReport()
        {
            return currentReport;
        }

        [Serializable]
        public class PostMatchReport
        {
            public string matchId;
            public DateTime matchEndTime;
            public int winningTeam;
            public ulong mvpPlayerId;
            public List<PlayerMatchPerformance> playerStats;
            public Dictionary<int, TeamStats> teamStats;
        }

        [Serializable]
        public class PlayerMatchPerformance
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
            public int xpEarned;
            public int currencyEarned;
            public List<MatchAward> awards;
        }

        [Serializable]
        public class TeamStats
        {
            public int teamId;
            public int totalKills;
            public int totalDeaths;
            public float totalDamage;
            public int playerCount;
        }

        [Serializable]
        public class MatchAward
        {
            public AwardType awardType;
            public string description;
        }

        public enum AwardType
        {
            MVP,
            KillingMachine,
            Massacre,
            Sharpshooter,
            Deadeye,
            Dominator,
            Untouchable,
            Survivor,
            Unstoppable,
            HighDamage,
            Teamplayer,
            Flawless,
            FirstBlood,
            Comeback
        }
    }
}
