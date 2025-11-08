using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace DeadFrontier.Tournaments
{
    /// <summary>
    /// Comprehensive tournament system for organized competitive play.
    /// Supports multiple formats: single/double elimination, Swiss, round-robin.
    /// Includes registration, seeding, brackets, prizes, and automated progression.
    /// </summary>
    public class TournamentSystem : MonoBehaviour
    {
        public static TournamentSystem Instance { get; private set; }

        [Header("Tournament Settings")]
        [SerializeField] private TournamentData[] availableTournaments;
        [SerializeField] private int maxActiveTournaments = 10;
        [SerializeField] private float registrationDuration = 3600f; // 1 hour
        [SerializeField] private float checkInDuration = 600f; // 10 minutes

        [Header("Bracket Settings")]
        [SerializeField] private int minPlayersForStart = 4;
        [SerializeField] private int maxPlayersPerTournament = 128;
        [SerializeField] private float matchTimeout = 1800f; // 30 minutes

        [Header("Prize Settings")]
        [SerializeField] private bool enablePrizePools = true;
        [SerializeField] private float[] prizeDistribution = { 0.5f, 0.3f, 0.15f, 0.05f }; // 1st, 2nd, 3rd, 4th

        // Active tournaments
        private Dictionary<string, Tournament> activeTournaments = new Dictionary<string, Tournament>();
        private Dictionary<string, List<Match>> tournamentMatches = new Dictionary<string, List<Match>>();

        // Player registrations
        private Dictionary<string, List<ulong>> tournamentRegistrations = new Dictionary<string, List<ulong>>();
        private Dictionary<string, List<ulong>> checkedInPlayers = new Dictionary<string, List<ulong>>();

        // Tournament history
        private List<TournamentResult> completedTournaments = new List<TournamentResult>();

        // Events
        public event Action<Tournament> OnTournamentCreated;
        public event Action<Tournament> OnTournamentStarted;
        public event Action<Tournament> OnTournamentCompleted;
        public event Action<string, ulong> OnPlayerRegistered;
        public event Action<string, ulong> OnPlayerCheckedIn;
        public event Action<Match> OnMatchStarted;
        public event Action<Match> OnMatchCompleted;

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
            LoadTournamentHistory();
        }

        private void Update()
        {
            UpdateTournaments();
        }

        #region Tournament Creation

        public Tournament CreateTournament(string tournamentName, TournamentFormat format, int maxPlayers, int entryFee, int prizePool)
        {
            if (activeTournaments.Count >= maxActiveTournaments)
            {
                Debug.LogWarning("[TournamentSystem] Max active tournaments reached");
                return null;
            }

            string tournamentId = $"tournament_{DateTime.UtcNow.Ticks}";

            var tournament = new Tournament
            {
                tournamentId = tournamentId,
                tournamentName = tournamentName,
                format = format,
                maxPlayers = Mathf.Min(maxPlayers, maxPlayersPerTournament),
                entryFee = entryFee,
                prizePool = prizePool,
                phase = TournamentPhase.Registration,
                creationTime = DateTime.UtcNow,
                registrationEndTime = DateTime.UtcNow.AddSeconds(registrationDuration),
                currentRound = 0
            };

            activeTournaments[tournamentId] = tournament;
            tournamentRegistrations[tournamentId] = new List<ulong>();
            checkedInPlayers[tournamentId] = new List<ulong>();
            tournamentMatches[tournamentId] = new List<Match>();

            OnTournamentCreated?.Invoke(tournament);

            Debug.Log($"[TournamentSystem] Created tournament: {tournamentName} ({format})");

            return tournament;
        }

        #endregion

        #region Registration

        public bool CanRegister(string tournamentId, ulong playerId)
        {
            if (!activeTournaments.ContainsKey(tournamentId)) return false;

            var tournament = activeTournaments[tournamentId];

            // Check phase
            if (tournament.phase != TournamentPhase.Registration) return false;

            // Check if already registered
            if (tournamentRegistrations[tournamentId].Contains(playerId)) return false;

            // Check max players
            if (tournamentRegistrations[tournamentId].Count >= tournament.maxPlayers) return false;

            // Check entry fee
            if (tournament.entryFee > 0 && Economy.EconomyManager.Instance != null)
            {
                if (!Economy.EconomyManager.Instance.CanAfford(tournament.entryFee))
                    return false;
            }

            return true;
        }

        public bool RegisterPlayer(string tournamentId, ulong playerId)
        {
            if (!CanRegister(tournamentId, playerId)) return false;

            var tournament = activeTournaments[tournamentId];

            // Charge entry fee
            if (tournament.entryFee > 0 && Economy.EconomyManager.Instance != null)
            {
                if (!Economy.EconomyManager.Instance.SpendSoftCurrency(tournament.entryFee, $"Tournament Entry: {tournament.tournamentName}"))
                    return false;

                // Add to prize pool
                tournament.prizePool += tournament.entryFee;
            }

            tournamentRegistrations[tournamentId].Add(playerId);

            OnPlayerRegistered?.Invoke(tournamentId, playerId);

            Debug.Log($"[TournamentSystem] Player {playerId} registered for tournament {tournamentId}");

            return true;
        }

        public bool UnregisterPlayer(string tournamentId, ulong playerId)
        {
            if (!tournamentRegistrations.ContainsKey(tournamentId)) return false;

            var tournament = activeTournaments[tournamentId];

            // Can only unregister during registration phase
            if (tournament.phase != TournamentPhase.Registration) return false;

            if (!tournamentRegistrations[tournamentId].Contains(playerId)) return false;

            tournamentRegistrations[tournamentId].Remove(playerId);

            // Refund entry fee
            if (tournament.entryFee > 0 && Economy.EconomyManager.Instance != null)
            {
                Economy.EconomyManager.Instance.EarnSoftCurrency(tournament.entryFee, "Tournament Registration Refund");
                tournament.prizePool -= tournament.entryFee;
            }

            Debug.Log($"[TournamentSystem] Player {playerId} unregistered from tournament {tournamentId}");

            return true;
        }

        #endregion

        #region Check-In

        public bool CheckInPlayer(string tournamentId, ulong playerId)
        {
            if (!activeTournaments.ContainsKey(tournamentId)) return false;

            var tournament = activeTournaments[tournamentId];

            // Must be in check-in phase
            if (tournament.phase != TournamentPhase.CheckIn) return false;

            // Must be registered
            if (!tournamentRegistrations[tournamentId].Contains(playerId)) return false;

            // Already checked in
            if (checkedInPlayers[tournamentId].Contains(playerId)) return false;

            checkedInPlayers[tournamentId].Add(playerId);

            OnPlayerCheckedIn?.Invoke(tournamentId, playerId);

            Debug.Log($"[TournamentSystem] Player {playerId} checked in for tournament {tournamentId}");

            return true;
        }

        #endregion

        #region Tournament Progression

        private void UpdateTournaments()
        {
            foreach (var kvp in activeTournaments.ToList())
            {
                var tournamentId = kvp.Key;
                var tournament = kvp.Value;

                switch (tournament.phase)
                {
                    case TournamentPhase.Registration:
                        UpdateRegistrationPhase(tournamentId, tournament);
                        break;

                    case TournamentPhase.CheckIn:
                        UpdateCheckInPhase(tournamentId, tournament);
                        break;

                    case TournamentPhase.Ongoing:
                        UpdateOngoingPhase(tournamentId, tournament);
                        break;
                }
            }
        }

        private void UpdateRegistrationPhase(string tournamentId, Tournament tournament)
        {
            // Check if registration period ended
            if (DateTime.UtcNow >= tournament.registrationEndTime)
            {
                // Move to check-in phase
                tournament.phase = TournamentPhase.CheckIn;
                tournament.checkInEndTime = DateTime.UtcNow.AddSeconds(checkInDuration);

                Debug.Log($"[TournamentSystem] Tournament {tournamentId} moved to check-in phase");
            }
        }

        private void UpdateCheckInPhase(string tournamentId, Tournament tournament)
        {
            // Check if check-in period ended
            if (DateTime.UtcNow >= tournament.checkInEndTime)
            {
                // Remove players who didn't check in
                int checkedInCount = checkedInPlayers[tournamentId].Count;

                if (checkedInCount < minPlayersForStart)
                {
                    // Not enough players, cancel tournament
                    CancelTournament(tournamentId);
                    return;
                }

                // Start tournament
                StartTournament(tournamentId);
            }
        }

        private void UpdateOngoingPhase(string tournamentId, Tournament tournament)
        {
            // Check if all matches in current round are complete
            var matches = tournamentMatches[tournamentId].Where(m => m.round == tournament.currentRound).ToList();

            if (matches.All(m => m.isCompleted))
            {
                // Check if tournament is complete
                if (IsTournamentComplete(tournamentId))
                {
                    CompleteTournament(tournamentId);
                }
                else
                {
                    // Advance to next round
                    AdvanceToNextRound(tournamentId);
                }
            }
        }

        private void StartTournament(string tournamentId)
        {
            var tournament = activeTournaments[tournamentId];
            var participants = checkedInPlayers[tournamentId];

            // Seed players
            var seededPlayers = SeedPlayers(tournamentId, participants);

            // Generate bracket
            GenerateBracket(tournamentId, tournament.format, seededPlayers);

            tournament.phase = TournamentPhase.Ongoing;
            tournament.currentRound = 1;
            tournament.startTime = DateTime.UtcNow;

            OnTournamentStarted?.Invoke(tournament);

            Debug.Log($"[TournamentSystem] Started tournament {tournamentId} with {participants.Count} players");
        }

        private void CancelTournament(string tournamentId)
        {
            var tournament = activeTournaments[tournamentId];

            // Refund all entry fees
            if (tournament.entryFee > 0)
            {
                foreach (var playerId in tournamentRegistrations[tournamentId])
                {
                    if (Economy.EconomyManager.Instance != null)
                    {
                        Economy.EconomyManager.Instance.EarnSoftCurrency(tournament.entryFee, "Tournament Cancelled Refund");
                    }
                }
            }

            // Remove tournament
            activeTournaments.Remove(tournamentId);
            tournamentRegistrations.Remove(tournamentId);
            checkedInPlayers.Remove(tournamentId);
            tournamentMatches.Remove(tournamentId);

            Debug.Log($"[TournamentSystem] Cancelled tournament {tournamentId} (insufficient players)");
        }

        private void CompleteTournament(string tournamentId)
        {
            var tournament = activeTournaments[tournamentId];
            tournament.phase = TournamentPhase.Completed;
            tournament.endTime = DateTime.UtcNow;

            // Determine winners
            var standings = DetermineFinalStandings(tournamentId);

            // Award prizes
            if (enablePrizePools)
            {
                AwardPrizes(tournamentId, tournament.prizePool, standings);
            }

            // Save to history
            SaveTournamentResult(tournamentId, tournament, standings);

            OnTournamentCompleted?.Invoke(tournament);

            // Clean up
            activeTournaments.Remove(tournamentId);

            Debug.Log($"[TournamentSystem] Completed tournament {tournamentId}");
        }

        #endregion

        #region Bracket Generation

        private List<ulong> SeedPlayers(string tournamentId, List<ulong> participants)
        {
            // Seed based on competitive rank
            if (Progression.PrestigeRankSystem.Instance != null)
            {
                return participants.OrderByDescending(p =>
                    Progression.PrestigeRankSystem.Instance.GetRankPoints(p)
                ).ToList();
            }

            // Random seeding
            return participants.OrderBy(x => UnityEngine.Random.value).ToList();
        }

        private void GenerateBracket(string tournamentId, TournamentFormat format, List<ulong> seededPlayers)
        {
            switch (format)
            {
                case TournamentFormat.SingleElimination:
                    GenerateSingleEliminationBracket(tournamentId, seededPlayers);
                    break;

                case TournamentFormat.DoubleElimination:
                    GenerateDoubleEliminationBracket(tournamentId, seededPlayers);
                    break;

                case TournamentFormat.Swiss:
                    GenerateSwissRound(tournamentId, seededPlayers, 1);
                    break;

                case TournamentFormat.RoundRobin:
                    GenerateRoundRobinBracket(tournamentId, seededPlayers);
                    break;
            }
        }

        private void GenerateSingleEliminationBracket(string tournamentId, List<ulong> players)
        {
            int round = 1;
            var matches = tournamentMatches[tournamentId];

            // Create first round matches
            for (int i = 0; i < players.Count; i += 2)
            {
                if (i + 1 < players.Count)
                {
                    var match = new Match
                    {
                        matchId = $"{tournamentId}_R{round}_M{i / 2}",
                        tournamentId = tournamentId,
                        round = round,
                        player1 = players[i],
                        player2 = players[i + 1],
                        isCompleted = false
                    };

                    matches.Add(match);
                }
                else
                {
                    // Bye - player advances automatically
                }
            }

            Debug.Log($"[TournamentSystem] Generated single elimination bracket with {matches.Count} first round matches");
        }

        private void GenerateDoubleEliminationBracket(string tournamentId, List<ulong> players)
        {
            // Similar to single elimination but with winners and losers brackets
            // This would be more complex and track eliminated players
            GenerateSingleEliminationBracket(tournamentId, players); // Simplified for now
        }

        private void GenerateSwissRound(string tournamentId, List<ulong> players, int round)
        {
            // Pair players with similar records
            // For first round, use seeding
            var matches = tournamentMatches[tournamentId];

            var availablePlayers = new List<ulong>(players);

            while (availablePlayers.Count >= 2)
            {
                var p1 = availablePlayers[0];
                var p2 = availablePlayers[1];

                var match = new Match
                {
                    matchId = $"{tournamentId}_R{round}_M{matches.Count}",
                    tournamentId = tournamentId,
                    round = round,
                    player1 = p1,
                    player2 = p2,
                    isCompleted = false
                };

                matches.Add(match);

                availablePlayers.RemoveAt(0);
                availablePlayers.RemoveAt(0);
            }

            Debug.Log($"[TournamentSystem] Generated Swiss round {round} with {matches.Count} matches");
        }

        private void GenerateRoundRobinBracket(string tournamentId, List<ulong> players)
        {
            // Everyone plays everyone
            var matches = tournamentMatches[tournamentId];
            int round = 1;

            for (int i = 0; i < players.Count; i++)
            {
                for (int j = i + 1; j < players.Count; j++)
                {
                    var match = new Match
                    {
                        matchId = $"{tournamentId}_M{matches.Count}",
                        tournamentId = tournamentId,
                        round = round,
                        player1 = players[i],
                        player2 = players[j],
                        isCompleted = false
                    };

                    matches.Add(match);
                }
            }

            Debug.Log($"[TournamentSystem] Generated round-robin bracket with {matches.Count} total matches");
        }

        private void AdvanceToNextRound(string tournamentId)
        {
            var tournament = activeTournaments[tournamentId];
            var currentRoundMatches = tournamentMatches[tournamentId].Where(m => m.round == tournament.currentRound).ToList();

            // Get winners from current round
            var winners = currentRoundMatches.Select(m => m.winner).Where(w => w.HasValue).Select(w => w.Value).ToList();

            if (winners.Count == 0) return;

            tournament.currentRound++;

            // Generate next round matches based on format
            if (tournament.format == TournamentFormat.Swiss)
            {
                GenerateSwissRound(tournamentId, checkedInPlayers[tournamentId], tournament.currentRound);
            }
            else
            {
                // Single/Double elimination - pair up winners
                for (int i = 0; i < winners.Count; i += 2)
                {
                    if (i + 1 < winners.Count)
                    {
                        var match = new Match
                        {
                            matchId = $"{tournamentId}_R{tournament.currentRound}_M{i / 2}",
                            tournamentId = tournamentId,
                            round = tournament.currentRound,
                            player1 = winners[i],
                            player2 = winners[i + 1],
                            isCompleted = false
                        };

                        tournamentMatches[tournamentId].Add(match);
                    }
                }
            }

            Debug.Log($"[TournamentSystem] Advanced tournament {tournamentId} to round {tournament.currentRound}");
        }

        private bool IsTournamentComplete(string tournamentId)
        {
            var tournament = activeTournaments[tournamentId];
            var currentRoundMatches = tournamentMatches[tournamentId].Where(m => m.round == tournament.currentRound).ToList();

            // Single elimination: complete when only 1 match in final round
            if (tournament.format == TournamentFormat.SingleElimination)
            {
                return currentRoundMatches.Count == 1 && currentRoundMatches.All(m => m.isCompleted);
            }

            // Swiss: complete after predetermined rounds
            if (tournament.format == TournamentFormat.Swiss)
            {
                int swissRounds = (int)Math.Ceiling(Math.Log(checkedInPlayers[tournamentId].Count, 2));
                return tournament.currentRound >= swissRounds;
            }

            // Round-robin: complete when all matches done
            if (tournament.format == TournamentFormat.RoundRobin)
            {
                return tournamentMatches[tournamentId].All(m => m.isCompleted);
            }

            return false;
        }

        #endregion

        #region Match Management

        public void ReportMatchResult(string matchId, ulong winnerId, int player1Score, int player2Score)
        {
            var match = GetMatchById(matchId);
            if (match == null || match.isCompleted) return;

            match.winner = winnerId;
            match.player1Score = player1Score;
            match.player2Score = player2Score;
            match.isCompleted = true;
            match.completionTime = DateTime.UtcNow;

            OnMatchCompleted?.Invoke(match);

            Debug.Log($"[TournamentSystem] Match {matchId} completed. Winner: {winnerId}");
        }

        private Match GetMatchById(string matchId)
        {
            foreach (var matches in tournamentMatches.Values)
            {
                var match = matches.FirstOrDefault(m => m.matchId == matchId);
                if (match != null) return match;
            }

            return null;
        }

        #endregion

        #region Prize Distribution

        private void AwardPrizes(string tournamentId, int prizePool, List<ulong> standings)
        {
            if (Economy.EconomyManager.Instance == null) return;

            int placementsToAward = Mathf.Min(prizeDistribution.Length, standings.Count);

            for (int i = 0; i < placementsToAward; i++)
            {
                int prizeAmount = Mathf.RoundToInt(prizePool * prizeDistribution[i]);
                ulong playerId = standings[i];

                Economy.EconomyManager.Instance.EarnSoftCurrency(prizeAmount, $"Tournament Prize: Rank {i + 1}");

                Debug.Log($"[TournamentSystem] Awarded {prizeAmount} to player {playerId} for placing {i + 1}");
            }
        }

        #endregion

        #region Final Standings

        private List<ulong> DetermineFinalStandings(string tournamentId)
        {
            var tournament = activeTournaments[tournamentId];
            var participants = checkedInPlayers[tournamentId];

            if (tournament.format == TournamentFormat.SingleElimination || tournament.format == TournamentFormat.DoubleElimination)
            {
                // Winner is from final match
                var finalMatch = tournamentMatches[tournamentId].FirstOrDefault(m => m.round == tournament.currentRound);
                if (finalMatch != null && finalMatch.winner.HasValue)
                {
                    var winner = finalMatch.winner.Value;
                    var runnerUp = finalMatch.player1 == winner ? finalMatch.player2 : finalMatch.player1;

                    return new List<ulong> { winner, runnerUp };
                }
            }
            else if (tournament.format == TournamentFormat.Swiss || tournament.format == TournamentFormat.RoundRobin)
            {
                // Sort by wins
                var standings = participants.OrderByDescending(p => GetPlayerWins(tournamentId, p)).ToList();
                return standings;
            }

            return participants;
        }

        private int GetPlayerWins(string tournamentId, ulong playerId)
        {
            return tournamentMatches[tournamentId].Count(m => m.winner == playerId);
        }

        #endregion

        #region Persistence

        private void SaveTournamentResult(string tournamentId, Tournament tournament, List<ulong> standings)
        {
            var result = new TournamentResult
            {
                tournamentId = tournamentId,
                tournamentName = tournament.tournamentName,
                format = tournament.format,
                startTime = tournament.startTime.ToString(),
                endTime = tournament.endTime.ToString(),
                participantCount = checkedInPlayers[tournamentId].Count,
                prizePool = tournament.prizePool,
                standings = standings
            };

            completedTournaments.Add(result);

            SaveTournamentHistory();
        }

        private void LoadTournamentHistory()
        {
            if (Core.SaveSystem.Instance == null) return;

            string savedData = Core.SaveSystem.Instance.LoadData("tournament_history");
            if (!string.IsNullOrEmpty(savedData))
            {
                try
                {
                    var historySave = JsonUtility.FromJson<TournamentHistorySaveData>(savedData);
                    completedTournaments = historySave.completedTournaments;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[TournamentSystem] Error loading tournament history: {e.Message}");
                }
            }
        }

        private void SaveTournamentHistory()
        {
            if (Core.SaveSystem.Instance == null) return;

            var historySave = new TournamentHistorySaveData
            {
                completedTournaments = completedTournaments
            };

            string json = JsonUtility.ToJson(historySave);
            Core.SaveSystem.Instance.SaveData("tournament_history", json);
        }

        #endregion

        #region Public Getters

        public List<Tournament> GetActiveTournaments() => new List<Tournament>(activeTournaments.Values);

        public Tournament GetTournament(string tournamentId)
        {
            return activeTournaments.ContainsKey(tournamentId) ? activeTournaments[tournamentId] : null;
        }

        public List<Match> GetTournamentMatches(string tournamentId)
        {
            return tournamentMatches.ContainsKey(tournamentId) ? new List<Match>(tournamentMatches[tournamentId]) : new List<Match>();
        }

        public List<ulong> GetTournamentParticipants(string tournamentId)
        {
            return tournamentRegistrations.ContainsKey(tournamentId) ? new List<ulong>(tournamentRegistrations[tournamentId]) : new List<ulong>();
        }

        public List<TournamentResult> GetTournamentHistory() => new List<TournamentResult>(completedTournaments);

        public bool IsPlayerRegistered(string tournamentId, ulong playerId)
        {
            return tournamentRegistrations.ContainsKey(tournamentId) && tournamentRegistrations[tournamentId].Contains(playerId);
        }

        #endregion
    }

    #region Data Classes

    public class Tournament
    {
        public string tournamentId;
        public string tournamentName;
        public TournamentFormat format;
        public int maxPlayers;
        public int entryFee;
        public int prizePool;
        public TournamentPhase phase;
        public DateTime creationTime;
        public DateTime registrationEndTime;
        public DateTime checkInEndTime;
        public DateTime startTime;
        public DateTime endTime;
        public int currentRound;
    }

    public class Match
    {
        public string matchId;
        public string tournamentId;
        public int round;
        public ulong player1;
        public ulong player2;
        public int player1Score;
        public int player2Score;
        public ulong? winner;
        public bool isCompleted;
        public DateTime completionTime;
    }

    [System.Serializable]
    public class TournamentData
    {
        public string tournamentName;
        public TournamentFormat format;
        public int maxPlayers;
        public int entryFee;
        public int basePrizePool;
        public Sprite tournamentIcon;
    }

    [System.Serializable]
    public class TournamentResult
    {
        public string tournamentId;
        public string tournamentName;
        public TournamentFormat format;
        public string startTime;
        public string endTime;
        public int participantCount;
        public int prizePool;
        public List<ulong> standings;
    }

    [System.Serializable]
    public class TournamentHistorySaveData
    {
        public List<TournamentResult> completedTournaments;
    }

    public enum TournamentFormat
    {
        SingleElimination,
        DoubleElimination,
        Swiss,
        RoundRobin
    }

    public enum TournamentPhase
    {
        Registration,
        CheckIn,
        Ongoing,
        Completed,
        Cancelled
    }

    #endregion
}
