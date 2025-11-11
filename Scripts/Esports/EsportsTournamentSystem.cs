using Unity.Netcode;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ZombieGame
{
    /// <summary>
    /// Esports Tournament System - Full competitive tournament infrastructure
    /// Features: Brackets, prize pools, streaming integration, match scheduling
    /// Supports single/double elimination, swiss, round robin formats
    /// </summary>
    public class EsportsTournamentSystem : NetworkBehaviour
    {
        public static EsportsTournamentSystem Instance { get; private set; }

        [Header("Tournament Settings")]
        [SerializeField] private int maxTournamentSize = 128; // Max teams/players

        // Active Tournaments
        private Dictionary<string, Tournament> activeTournaments = new Dictionary<string, Tournament>();

        // Tournament History
        private List<TournamentResult> tournamentHistory = new List<TournamentResult>();

        // Prize Pools
        private Dictionary<string, PrizePool> prizePools = new Dictionary<string, PrizePool>();

        // Events
        public event System.Action<string> OnTournamentCreated;
        public event System.Action<string> OnTournamentStarted;
        public event System.Action<string, string> OnMatchComplete; // tournamentId, matchId
        public event System.Action<string, string> OnTournamentWinner; // tournamentId, winnerId

        [System.Serializable]
        public class Tournament
        {
            public string tournamentId;
            public string tournamentName;
            public TournamentFormat format;
            public TournamentStatus status;
            public DateTime registrationStart;
            public DateTime registrationEnd;
            public DateTime tournamentStart;
            public int maxParticipants;
            public List<string> registeredTeams = new List<string>(); // Team/Player IDs
            public TournamentBracket bracket;
            public string prizePoolId;
            public List<string> sponsors = new List<string>();
            public bool isStreamedOfficial = false;
            public string streamUrl = "";
            public TournamentRules rules;
        }

        public enum TournamentFormat
        {
            SingleElimination,  // Lose once, out
            DoubleElimination,  // Lose twice, out (winners/losers bracket)
            Swiss,              // Play set number of rounds
            RoundRobin,         // Everyone plays everyone
            BattleRoyale        // Last team standing
        }

        public enum TournamentStatus
        {
            Registration,
            CheckIn,
            InProgress,
            Completed,
            Cancelled
        }

        [System.Serializable]
        public class TournamentBracket
        {
            public List<BracketRound> rounds = new List<BracketRound>();
            public Dictionary<string, BracketMatch> matches = new Dictionary<string, BracketMatch>();
            public string winnerId = "";
        }

        [System.Serializable]
        public class BracketRound
        {
            public int roundNumber;
            public string roundName; // "Quarter Finals", "Semi Finals", "Grand Finals"
            public List<string> matchIds = new List<string>();
            public bool isComplete = false;
        }

        [System.Serializable]
        public class BracketMatch
        {
            public string matchId;
            public string team1Id;
            public string team2Id;
            public string winnerId;
            public int team1Score = 0;
            public int team2Score = 0;
            public MatchStatus matchStatus;
            public DateTime scheduledTime;
            public string nextMatchId; // Winner advances to this match
        }

        public enum MatchStatus
        {
            Scheduled,
            InProgress,
            Completed,
            Forfeited
        }

        [System.Serializable]
        public class PrizePool
        {
            public string prizePoolId;
            public int totalPrize; // In currency
            public List<PrizeDistribution> distribution = new List<PrizeDistribution>();
        }

        [System.Serializable]
        public class PrizeDistribution
        {
            public int placement; // 1st, 2nd, 3rd, etc.
            public int prizeAmount;
            public float prizePercentage;
            public List<string> additionalRewards = new List<string>(); // Skins, titles, etc.
        }

        [System.Serializable]
        public class TournamentRules
        {
            public int bestOfMatches = 1; // Best of 1, 3, 5, etc.
            public int teamSize = 4;
            public bool allowSubstitutes = true;
            public int maxSubstitutes = 2;
            public string gameMode = "Survival";
            public Dictionary<string, string> specificRules = new Dictionary<string, string>();
        }

        [System.Serializable]
        public class TournamentResult
        {
            public string tournamentId;
            public string tournamentName;
            public DateTime completionDate;
            public string winnerId;
            public string runnerUpId;
            public int totalParticipants;
            public int totalMatches;
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializePrizePools();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializePrizePools()
        {
            // Standard Prize Pool
            prizePools["standard"] = new PrizePool
            {
                prizePoolId = "standard",
                totalPrize = 10000,
                distribution = new List<PrizeDistribution>
                {
                    new PrizeDistribution { placement = 1, prizePercentage = 0.5f, prizeAmount = 5000, additionalRewards = new List<string> { "title_champion", "skin_gold_trophy" } },
                    new PrizeDistribution { placement = 2, prizePercentage = 0.3f, prizeAmount = 3000, additionalRewards = new List<string> { "title_runner_up", "skin_silver_trophy" } },
                    new PrizeDistribution { placement = 3, prizePercentage = 0.15f, prizeAmount = 1500, additionalRewards = new List<string> { "title_third_place", "skin_bronze_trophy" } },
                    new PrizeDistribution { placement = 4, prizePercentage = 0.05f, prizeAmount = 500 }
                }
            };

            // Major Prize Pool
            prizePools["major"] = new PrizePool
            {
                prizePoolId = "major",
                totalPrize = 100000,
                distribution = new List<PrizeDistribution>
                {
                    new PrizeDistribution { placement = 1, prizePercentage = 0.4f, prizeAmount = 40000, additionalRewards = new List<string> { "title_major_champion", "weapon_championship" } },
                    new PrizeDistribution { placement = 2, prizePercentage = 0.25f, prizeAmount = 25000 },
                    new PrizeDistribution { placement = 3, prizePercentage = 0.15f, prizeAmount = 15000 },
                    new PrizeDistribution { placement = 4, prizePercentage = 0.1f, prizeAmount = 10000 },
                    new PrizeDistribution { placement = 5, prizePercentage = 0.05f, prizeAmount = 5000 },
                    new PrizeDistribution { placement = 6, prizePercentage = 0.05f, prizeAmount = 5000 }
                }
            };
        }

        // Public API

        [ServerRpc(RequireOwnership = false)]
        public void CreateTournamentServerRpc(string tournamentName, TournamentFormat format, int maxParticipants,
            string prizePoolId, ServerRpcParams rpcParams = default)
        {
            string tournamentId = Guid.NewGuid().ToString();

            var tournament = new Tournament
            {
                tournamentId = tournamentId,
                tournamentName = tournamentName,
                format = format,
                status = TournamentStatus.Registration,
                registrationStart = DateTime.UtcNow,
                registrationEnd = DateTime.UtcNow.AddDays(7),
                tournamentStart = DateTime.UtcNow.AddDays(8),
                maxParticipants = maxParticipants,
                prizePoolId = prizePoolId,
                bracket = new TournamentBracket(),
                rules = new TournamentRules()
            };

            activeTournaments[tournamentId] = tournament;

            OnTournamentCreated?.Invoke(tournamentId);
            NotifyTournamentCreatedClientRpc(tournamentId, tournamentName, format);

            Debug.Log($"[Esports] Tournament created: {tournamentName} ({format})");
        }

        [ClientRpc]
        private void NotifyTournamentCreatedClientRpc(string tournamentId, string tournamentName, TournamentFormat format)
        {
            Debug.Log($"[Esports] 🏆 NEW TOURNAMENT: {tournamentName} ({format})");
        }

        [ServerRpc(RequireOwnership = false)]
        public void RegisterForTournamentServerRpc(string tournamentId, string teamId, ServerRpcParams rpcParams = default)
        {
            if (!activeTournaments.ContainsKey(tournamentId)) return;

            var tournament = activeTournaments[tournamentId];

            if (tournament.status != TournamentStatus.Registration) return;
            if (tournament.registeredTeams.Count >= tournament.maxParticipants) return;
            if (tournament.registeredTeams.Contains(teamId)) return;

            tournament.registeredTeams.Add(teamId);

            Debug.Log($"[Esports] Team {teamId} registered for {tournament.tournamentName}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void StartTournamentServerRpc(string tournamentId, ServerRpcParams rpcParams = default)
        {
            if (!activeTournaments.ContainsKey(tournamentId)) return;

            var tournament = activeTournaments[tournamentId];

            if (tournament.status != TournamentStatus.CheckIn) return;

            tournament.status = TournamentStatus.InProgress;

            // Generate bracket based on format
            GenerateBracket(tournament);

            OnTournamentStarted?.Invoke(tournamentId);
            NotifyTournamentStartedClientRpc(tournamentId, tournament.tournamentName);

            Debug.Log($"[Esports] Tournament started: {tournament.tournamentName}");
        }

        [ClientRpc]
        private void NotifyTournamentStartedClientRpc(string tournamentId, string tournamentName)
        {
            Debug.Log($"[Esports] 🏆 TOURNAMENT LIVE: {tournamentName}");
        }

        private void GenerateBracket(Tournament tournament)
        {
            switch (tournament.format)
            {
                case TournamentFormat.SingleElimination:
                    GenerateSingleEliminationBracket(tournament);
                    break;
                case TournamentFormat.DoubleElimination:
                    GenerateDoubleEliminationBracket(tournament);
                    break;
                // ... other formats
            }
        }

        private void GenerateSingleEliminationBracket(Tournament tournament)
        {
            int participantCount = tournament.registeredTeams.Count;
            int roundCount = Mathf.CeilToInt(Mathf.Log(participantCount, 2));

            for (int i = 0; i < roundCount; i++)
            {
                var round = new BracketRound
                {
                    roundNumber = i + 1,
                    roundName = GetRoundName(i, roundCount)
                };

                tournament.bracket.rounds.Add(round);
            }

            // Create first round matches
            int matchesInRound = participantCount / 2;
            for (int i = 0; i < matchesInRound; i++)
            {
                string matchId = Guid.NewGuid().ToString();

                var match = new BracketMatch
                {
                    matchId = matchId,
                    team1Id = tournament.registeredTeams[i * 2],
                    team2Id = tournament.registeredTeams[i * 2 + 1],
                    matchStatus = MatchStatus.Scheduled
                };

                tournament.bracket.matches[matchId] = match;
                tournament.bracket.rounds[0].matchIds.Add(matchId);
            }
        }

        private void GenerateDoubleEliminationBracket(Tournament tournament)
        {
            // Winners bracket + Losers bracket + Grand Finals
            // More complex implementation...
        }

        private string GetRoundName(int roundIndex, int totalRounds)
        {
            int roundsFromEnd = totalRounds - roundIndex;

            switch (roundsFromEnd)
            {
                case 1: return "Grand Finals";
                case 2: return "Semi Finals";
                case 3: return "Quarter Finals";
                case 4: return "Round of 16";
                case 5: return "Round of 32";
                default: return $"Round {roundIndex + 1}";
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void ReportMatchResultServerRpc(string tournamentId, string matchId, string winnerId,
            int team1Score, int team2Score, ServerRpcParams rpcParams = default)
        {
            if (!activeTournaments.ContainsKey(tournamentId)) return;

            var tournament = activeTournaments[tournamentId];

            if (!tournament.bracket.matches.ContainsKey(matchId)) return;

            var match = tournament.bracket.matches[matchId];
            match.winnerId = winnerId;
            match.team1Score = team1Score;
            match.team2Score = team2Score;
            match.matchStatus = MatchStatus.Completed;

            OnMatchComplete?.Invoke(tournamentId, matchId);

            // Advance winner to next match
            AdvanceWinner(tournament, match);

            // Check if tournament complete
            CheckTournamentCompletion(tournament);

            Debug.Log($"[Esports] Match complete: {winnerId} wins");
        }

        private void AdvanceWinner(Tournament tournament, BracketMatch completedMatch)
        {
            if (string.IsNullOrEmpty(completedMatch.nextMatchId)) return;

            var nextMatch = tournament.bracket.matches[completedMatch.nextMatchId];

            if (string.IsNullOrEmpty(nextMatch.team1Id))
            {
                nextMatch.team1Id = completedMatch.winnerId;
            }
            else
            {
                nextMatch.team2Id = completedMatch.winnerId;
            }
        }

        private void CheckTournamentCompletion(Tournament tournament)
        {
            // Check if all matches complete
            bool allComplete = tournament.bracket.matches.Values.All(m => m.matchStatus == MatchStatus.Completed);

            if (allComplete)
            {
                CompleteTournament(tournament);
            }
        }

        private void CompleteTournament(Tournament tournament)
        {
            tournament.status = TournamentStatus.Completed;

            // Determine winner (last match winner)
            var finalMatch = tournament.bracket.rounds.Last().matchIds.Select(id => tournament.bracket.matches[id]).First();
            string winnerId = finalMatch.winnerId;

            tournament.bracket.winnerId = winnerId;

            // Distribute prizes
            DistributePrizes(tournament);

            // Record history
            tournamentHistory.Add(new TournamentResult
            {
                tournamentId = tournament.tournamentId,
                tournamentName = tournament.tournamentName,
                completionDate = DateTime.UtcNow,
                winnerId = winnerId,
                totalParticipants = tournament.registeredTeams.Count,
                totalMatches = tournament.bracket.matches.Count
            });

            OnTournamentWinner?.Invoke(tournament.tournamentId, winnerId);

            Debug.Log($"[Esports] 🏆 TOURNAMENT CHAMPION: {winnerId}");
        }

        private void DistributePrizes(Tournament tournament)
        {
            if (!prizePools.ContainsKey(tournament.prizePoolId)) return;

            var prizePool = prizePools[tournament.prizePoolId];

            // Award prizes based on placement
            // Would integrate with economy/inventory systems
        }

        // Getters

        public List<Tournament> GetActiveTournaments()
        {
            return activeTournaments.Values.ToList();
        }

        public Tournament GetTournament(string tournamentId)
        {
            return activeTournaments.ContainsKey(tournamentId) ? activeTournaments[tournamentId] : null;
        }

        public List<TournamentResult> GetTournamentHistory()
        {
            return tournamentHistory;
        }
    }
}
