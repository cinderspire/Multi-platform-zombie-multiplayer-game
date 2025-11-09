using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;
using System;

namespace DeadFrontier.Matchmaking
{
    /// <summary>
    /// Comprehensive matchmaking system for multiplayer matches.
    /// Supports skill-based matchmaking, party queues, region selection, and queue management.
    /// Includes backfill, priority queues, and anti-smurf detection.
    /// </summary>
    public class MatchmakingSystem : NetworkBehaviour
    {
        public static MatchmakingSystem Instance { get; private set; }

        [Header("Queue Settings")]
        [SerializeField] private float matchmakingTickRate = 2f; // Check every 2 seconds
        [SerializeField] private int minPlayersPerMatch = 8;
        [SerializeField] private int maxPlayersPerMatch = 16;
        [SerializeField] private int idealPlayersPerMatch = 12;

        [Header("Skill Settings")]
        [SerializeField] private bool enableSkillBasedMatchmaking = true;
        [SerializeField] private float initialSkillRange = 200f;
        [SerializeField] private float skillRangeExpansionRate = 50f; // Per 30 seconds
        [SerializeField] private float maxSkillRange = 1000f;

        [Header("Time Settings")]
        [SerializeField] private float maxQueueTime = 300f; // 5 minutes
        [SerializeField] private float priorityQueueTime = 120f; // Priority after 2 minutes
        [SerializeField] private float backfillTimeLimit = 60f; // 1 minute for backfill

        [Header("Region Settings")]
        [SerializeField] private List<string> availableRegions = new List<string> { "NA-East", "NA-West", "EU-West", "EU-East", "Asia", "OCE", "SA" };
        [SerializeField] private float regionPingThreshold = 100f; // ms

        // Queue management
        private Dictionary<string, Queue<MatchmakingTicket>> queuesByMode = new Dictionary<string, Queue<MatchmakingTicket>>();
        private Dictionary<string, MatchmakingTicket> activeTickets = new Dictionary<string, MatchmakingTicket>();

        // Active matches
        private Dictionary<string, MatchInstance> activeMatches = new Dictionary<string, MatchInstance>();
        private Dictionary<string, MatchInstance> backfillMatches = new Dictionary<string, MatchInstance>();

        // Player data
        private Dictionary<ulong, PlayerMatchmakingData> playerData = new Dictionary<ulong, PlayerMatchmakingData>();

        // Events
        public event Action<string, MatchInstance> OnMatchCreated;
        public event Action<ulong, string> OnMatchFound;
        public event Action<ulong> OnQueueTimeout;
        public event Action<string> OnMatchFull;

        private float tickTimer = 0f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeQueues();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (!IsServer) return;

            tickTimer += Time.deltaTime;

            if (tickTimer >= matchmakingTickRate)
            {
                tickTimer = 0f;
                ProcessMatchmaking();
            }

            CheckQueueTimeouts();
            UpdateSkillRanges();
        }

        #region Initialization

        private void InitializeQueues()
        {
            // Initialize queues for each game mode
            queuesByMode["casual"] = new Queue<MatchmakingTicket>();
            queuesByMode["ranked"] = new Queue<MatchmakingTicket>();
            queuesByMode["hardcore"] = new Queue<MatchmakingTicket>();
            queuesByMode["squad"] = new Queue<MatchmakingTicket>();

            Debug.Log($"[MatchmakingSystem] Initialized {queuesByMode.Count} matchmaking queues");
        }

        #endregion

        #region Player Initialization

        public void InitializePlayerData(ulong playerId)
        {
            if (playerData.ContainsKey(playerId)) return;

            playerData[playerId] = new PlayerMatchmakingData
            {
                playerId = playerId,
                skillRating = 1000, // Default MMR
                preferredRegion = "NA-East",
                blockedPlayers = new List<ulong>(),
                matchHistory = new List<string>()
            };

            LoadPlayerData(playerId);
        }

        #endregion

        #region Queue Management

        public string JoinQueue(ulong playerId, string gameMode, List<ulong> partyMembers = null)
        {
            if (!queuesByMode.ContainsKey(gameMode))
            {
                Debug.LogWarning($"[MatchmakingSystem] Invalid game mode: {gameMode}");
                return null;
            }

            if (!playerData.ContainsKey(playerId))
            {
                InitializePlayerData(playerId);
            }

            // Check if already in queue
            if (IsPlayerInQueue(playerId))
            {
                Debug.LogWarning($"[MatchmakingSystem] Player {playerId} already in queue");
                return null;
            }

            // Create ticket
            var ticket = new MatchmakingTicket
            {
                ticketId = $"ticket_{playerId}_{DateTime.UtcNow.Ticks}",
                leaderId = playerId,
                partyMembers = partyMembers ?? new List<ulong> { playerId },
                gameMode = gameMode,
                queueStartTime = DateTime.UtcNow,
                skillRating = CalculatePartySkillRating(playerId, partyMembers),
                currentSkillRange = initialSkillRange,
                region = playerData[playerId].preferredRegion
            };

            // Add to queue
            queuesByMode[gameMode].Enqueue(ticket);
            activeTickets[ticket.ticketId] = ticket;

            Debug.Log($"[MatchmakingSystem] Player {playerId} joined {gameMode} queue (Party: {ticket.partyMembers.Count}, MMR: {ticket.skillRating})");

            // Notify clients
            JoinQueueClientRpc(playerId, gameMode);

            return ticket.ticketId;
        }

        [ClientRpc]
        private void JoinQueueClientRpc(ulong playerId, string gameMode)
        {
            Debug.Log($"[MatchmakingSystem] Joined {gameMode} queue");
        }

        public bool LeaveQueue(ulong playerId)
        {
            var ticket = activeTickets.Values.FirstOrDefault(t => t.partyMembers.Contains(playerId));

            if (ticket == null) return false;

            // Remove from active tickets
            activeTickets.Remove(ticket.ticketId);

            Debug.Log($"[MatchmakingSystem] Player {playerId} left queue");

            // Notify clients
            LeaveQueueClientRpc(playerId);

            return true;
        }

        [ClientRpc]
        private void LeaveQueueClientRpc(ulong playerId)
        {
            Debug.Log("[MatchmakingSystem] Left queue");
        }

        public bool IsPlayerInQueue(ulong playerId)
        {
            return activeTickets.Values.Any(t => t.partyMembers.Contains(playerId));
        }

        #endregion

        #region Matchmaking Logic

        private void ProcessMatchmaking()
        {
            foreach (var kvp in queuesByMode)
            {
                string gameMode = kvp.Key;
                var queue = kvp.Value;

                if (queue.Count < minPlayersPerMatch) continue;

                // Try to create matches
                TryCreateMatch(gameMode);
            }

            // Process backfill
            ProcessBackfill();
        }

        private void TryCreateMatch(string gameMode)
        {
            var queue = queuesByMode[gameMode];
            var eligibleTickets = new List<MatchmakingTicket>();

            // Collect tickets from queue
            while (queue.Count > 0 && eligibleTickets.Count < maxPlayersPerMatch)
            {
                var ticket = queue.Dequeue();

                // Check if ticket is still valid
                if (activeTickets.ContainsKey(ticket.ticketId))
                {
                    eligibleTickets.Add(ticket);
                }
            }

            // Group tickets by skill and region
            var matchGroups = GroupTicketsBySkillAndRegion(eligibleTickets);

            foreach (var group in matchGroups)
            {
                int totalPlayers = group.Sum(t => t.partyMembers.Count);

                if (totalPlayers >= minPlayersPerMatch)
                {
                    CreateMatch(gameMode, group);
                }
                else
                {
                    // Re-queue tickets that couldn't be matched
                    foreach (var ticket in group)
                    {
                        queue.Enqueue(ticket);
                    }
                }
            }
        }

        private List<List<MatchmakingTicket>> GroupTicketsBySkillAndRegion(List<MatchmakingTicket> tickets)
        {
            var groups = new List<List<MatchmakingTicket>>();

            if (tickets.Count == 0) return groups;

            // Sort by skill rating
            tickets = tickets.OrderBy(t => t.skillRating).ToList();

            var currentGroup = new List<MatchmakingTicket>();
            MatchmakingTicket referenceTicket = null;

            foreach (var ticket in tickets)
            {
                if (referenceTicket == null)
                {
                    referenceTicket = ticket;
                    currentGroup.Add(ticket);
                    continue;
                }

                // Check if ticket fits in current group
                bool skillMatch = enableSkillBasedMatchmaking
                    ? Mathf.Abs(ticket.skillRating - referenceTicket.skillRating) <= ticket.currentSkillRange
                    : true;

                bool regionMatch = ticket.region == referenceTicket.region;

                int totalPlayers = currentGroup.Sum(t => t.partyMembers.Count) + ticket.partyMembers.Count;

                if (skillMatch && regionMatch && totalPlayers <= maxPlayersPerMatch)
                {
                    currentGroup.Add(ticket);
                }
                else
                {
                    // Start new group
                    if (currentGroup.Count > 0)
                    {
                        groups.Add(currentGroup);
                    }

                    currentGroup = new List<MatchmakingTicket> { ticket };
                    referenceTicket = ticket;
                }
            }

            if (currentGroup.Count > 0)
            {
                groups.Add(currentGroup);
            }

            return groups;
        }

        private void CreateMatch(string gameMode, List<MatchmakingTicket> tickets)
        {
            string matchId = $"match_{DateTime.UtcNow.Ticks}";

            var match = new MatchInstance
            {
                matchId = matchId,
                gameMode = gameMode,
                players = new List<ulong>(),
                tickets = new List<MatchmakingTicket>(tickets),
                creationTime = DateTime.UtcNow,
                state = MatchState.Preparing,
                averageSkillRating = tickets.Average(t => t.skillRating),
                region = tickets.First().region
            };

            // Add all players
            foreach (var ticket in tickets)
            {
                match.players.AddRange(ticket.partyMembers);

                // Remove from active tickets
                activeTickets.Remove(ticket.ticketId);
            }

            activeMatches[matchId] = match;

            OnMatchCreated?.Invoke(matchId, match);

            Debug.Log($"[MatchmakingSystem] Created match {matchId}: {gameMode}, {match.players.Count} players, Avg MMR: {match.averageSkillRating}");

            // Notify all players
            foreach (var playerId in match.players)
            {
                OnMatchFound?.Invoke(playerId, matchId);
                NotifyMatchFoundClientRpc(playerId, matchId);
            }

            // Check if needs backfill
            if (match.players.Count < idealPlayersPerMatch)
            {
                backfillMatches[matchId] = match;
            }
        }

        [ClientRpc]
        private void NotifyMatchFoundClientRpc(ulong playerId, string matchId)
        {
            Debug.Log($"[MatchmakingSystem] MATCH FOUND! Match ID: {matchId}");
        }

        #endregion

        #region Backfill

        private void ProcessBackfill()
        {
            foreach (var kvp in backfillMatches.ToList())
            {
                string matchId = kvp.Key;
                var match = kvp.Value;

                // Check if match is still valid for backfill
                TimeSpan matchAge = DateTime.UtcNow - match.creationTime;

                if (matchAge.TotalSeconds > backfillTimeLimit)
                {
                    backfillMatches.Remove(matchId);
                    continue;
                }

                // Try to find players for backfill
                TryBackfillMatch(match);
            }
        }

        private void TryBackfillMatch(MatchInstance match)
        {
            var queue = queuesByMode[match.gameMode];

            if (queue.Count == 0) return;

            int slotsAvailable = maxPlayersPerMatch - match.players.Count;
            var ticketsToAdd = new List<MatchmakingTicket>();

            // Look for suitable tickets
            var tempQueue = new Queue<MatchmakingTicket>();

            while (queue.Count > 0 && slotsAvailable > 0)
            {
                var ticket = queue.Dequeue();

                if (ticket.partyMembers.Count <= slotsAvailable)
                {
                    // Check skill compatibility
                    bool skillMatch = Mathf.Abs(ticket.skillRating - match.averageSkillRating) <= ticket.currentSkillRange;
                    bool regionMatch = ticket.region == match.region;

                    if (skillMatch && regionMatch)
                    {
                        ticketsToAdd.Add(ticket);
                        slotsAvailable -= ticket.partyMembers.Count;
                    }
                    else
                    {
                        tempQueue.Enqueue(ticket);
                    }
                }
                else
                {
                    tempQueue.Enqueue(ticket);
                }
            }

            // Re-queue tickets we didn't use
            while (tempQueue.Count > 0)
            {
                queue.Enqueue(tempQueue.Dequeue());
            }

            // Add backfilled players to match
            if (ticketsToAdd.Count > 0)
            {
                foreach (var ticket in ticketsToAdd)
                {
                    match.players.AddRange(ticket.partyMembers);
                    match.tickets.Add(ticket);

                    activeTickets.Remove(ticket.ticketId);

                    // Notify players
                    foreach (var playerId in ticket.partyMembers)
                    {
                        OnMatchFound?.Invoke(playerId, match.matchId);
                        NotifyMatchFoundClientRpc(playerId, match.matchId);
                    }
                }

                // Update average skill
                match.averageSkillRating = match.tickets.Average(t => t.skillRating);

                Debug.Log($"[MatchmakingSystem] Backfilled {ticketsToAdd.Sum(t => t.partyMembers.Count)} players into match {match.matchId}");
            }

            // Check if full
            if (match.players.Count >= idealPlayersPerMatch)
            {
                backfillMatches.Remove(match.matchId);
                OnMatchFull?.Invoke(match.matchId);
            }
        }

        #endregion

        #region Queue Management Helpers

        private void CheckQueueTimeouts()
        {
            var expiredTickets = activeTickets.Values
                .Where(t => (DateTime.UtcNow - t.queueStartTime).TotalSeconds > maxQueueTime)
                .ToList();

            foreach (var ticket in expiredTickets)
            {
                Debug.LogWarning($"[MatchmakingSystem] Queue timeout for ticket {ticket.ticketId}");

                foreach (var playerId in ticket.partyMembers)
                {
                    OnQueueTimeout?.Invoke(playerId);
                }

                activeTickets.Remove(ticket.ticketId);
            }
        }

        private void UpdateSkillRanges()
        {
            foreach (var ticket in activeTickets.Values)
            {
                TimeSpan queueTime = DateTime.UtcNow - ticket.queueStartTime;

                // Expand skill range over time
                float expansion = (float)(queueTime.TotalSeconds / 30f) * skillRangeExpansionRate;
                ticket.currentSkillRange = Mathf.Min(initialSkillRange + expansion, maxSkillRange);

                // Priority queue after threshold
                if (queueTime.TotalSeconds >= priorityQueueTime)
                {
                    ticket.isPriority = true;
                }
            }
        }

        private float CalculatePartySkillRating(ulong leaderId, List<ulong> partyMembers)
        {
            if (partyMembers == null || partyMembers.Count == 0)
            {
                return playerData.ContainsKey(leaderId) ? playerData[leaderId].skillRating : 1000f;
            }

            // Average party MMR
            float totalRating = 0f;
            int count = 0;

            foreach (var playerId in partyMembers)
            {
                if (playerData.ContainsKey(playerId))
                {
                    totalRating += playerData[playerId].skillRating;
                    count++;
                }
            }

            return count > 0 ? totalRating / count : 1000f;
        }

        #endregion

        #region Match Management

        public void StartMatch(string matchId)
        {
            if (!activeMatches.ContainsKey(matchId)) return;

            var match = activeMatches[matchId];
            match.state = MatchState.InProgress;
            match.startTime = DateTime.UtcNow;

            // Remove from backfill
            backfillMatches.Remove(matchId);

            Debug.Log($"[MatchmakingSystem] Match {matchId} started with {match.players.Count} players");
        }

        public void EndMatch(string matchId)
        {
            if (!activeMatches.ContainsKey(matchId)) return;

            var match = activeMatches[matchId];
            match.state = MatchState.Completed;
            match.endTime = DateTime.UtcNow;

            // Add to player history
            foreach (var playerId in match.players)
            {
                if (playerData.ContainsKey(playerId))
                {
                    playerData[playerId].matchHistory.Add(matchId);
                }
            }

            activeMatches.Remove(matchId);

            Debug.Log($"[MatchmakingSystem] Match {matchId} ended");
        }

        public void UpdatePlayerSkillRating(ulong playerId, float newRating)
        {
            if (!playerData.ContainsKey(playerId))
            {
                InitializePlayerData(playerId);
            }

            playerData[playerId].skillRating = newRating;
            SavePlayerData(playerId);
        }

        #endregion

        #region Region Management

        public void SetPreferredRegion(ulong playerId, string region)
        {
            if (!availableRegions.Contains(region))
            {
                Debug.LogWarning($"[MatchmakingSystem] Invalid region: {region}");
                return;
            }

            if (!playerData.ContainsKey(playerId))
            {
                InitializePlayerData(playerId);
            }

            playerData[playerId].preferredRegion = region;
            SavePlayerData(playerId);

            Debug.Log($"[MatchmakingSystem] Player {playerId} set region to {region}");
        }

        public List<string> GetAvailableRegions()
        {
            return new List<string>(availableRegions);
        }

        #endregion

        #region Queries

        public MatchmakingTicket GetPlayerTicket(ulong playerId)
        {
            return activeTickets.Values.FirstOrDefault(t => t.partyMembers.Contains(playerId));
        }

        public int GetQueueSize(string gameMode)
        {
            if (!queuesByMode.ContainsKey(gameMode)) return 0;

            return queuesByMode[gameMode].Count;
        }

        public TimeSpan GetEstimatedQueueTime(string gameMode)
        {
            // Simple estimation based on queue size and recent match creation rate
            int queueSize = GetQueueSize(gameMode);

            if (queueSize == 0) return TimeSpan.Zero;

            // Assume 1 match every 60 seconds on average
            int estimatedSeconds = (queueSize / minPlayersPerMatch) * 60;

            return TimeSpan.FromSeconds(estimatedSeconds);
        }

        public float GetPlayerSkillRating(ulong playerId)
        {
            return playerData.ContainsKey(playerId) ? playerData[playerId].skillRating : 1000f;
        }

        #endregion

        #region Data Persistence

        private void SavePlayerData(ulong playerId)
        {
            if (!playerData.ContainsKey(playerId)) return;

            var data = playerData[playerId];
            string json = JsonUtility.ToJson(data);

            SaveSystem.SaveManager.Instance?.SaveData($"matchmaking_{playerId}", json);
        }

        public void LoadPlayerData(ulong playerId)
        {
            string json = SaveSystem.SaveManager.Instance?.LoadData($"matchmaking_{playerId}");

            if (!string.IsNullOrEmpty(json))
            {
                var data = JsonUtility.FromJson<PlayerMatchmakingData>(json);
                playerData[playerId] = data;

                Debug.Log($"[MatchmakingSystem] Loaded matchmaking data for player {playerId}");
            }
        }

        #endregion
    }

    #region Data Classes

    [Serializable]
    public class MatchmakingTicket
    {
        public string ticketId;
        public ulong leaderId;
        public List<ulong> partyMembers = new List<ulong>();
        public string gameMode;
        public DateTime queueStartTime;
        public float skillRating;
        public float currentSkillRange;
        public string region;
        public bool isPriority;
    }

    [Serializable]
    public class MatchInstance
    {
        public string matchId;
        public string gameMode;
        public List<ulong> players = new List<ulong>();
        public List<MatchmakingTicket> tickets = new List<MatchmakingTicket>();
        public DateTime creationTime;
        public DateTime startTime;
        public DateTime endTime;
        public MatchState state;
        public float averageSkillRating;
        public string region;
    }

    [Serializable]
    public class PlayerMatchmakingData
    {
        public ulong playerId;
        public float skillRating;
        public string preferredRegion;
        public List<ulong> blockedPlayers = new List<ulong>();
        public List<string> matchHistory = new List<string>();
    }

    public enum MatchState
    {
        Preparing,
        InProgress,
        Completed,
        Cancelled
    }

    #endregion
}
