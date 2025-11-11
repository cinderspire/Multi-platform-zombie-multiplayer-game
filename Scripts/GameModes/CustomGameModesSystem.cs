using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.GameModes
{
    /// <summary>
    /// Custom game modes system allowing players to create and share custom
    /// game configurations with modifiable rules, settings, and voting.
    /// </summary>
    public class CustomGameModesSystem : NetworkBehaviour
    {
        public static CustomGameModesSystem Instance { get; private set; }

        [Header("Custom Game Configuration")]
        [SerializeField] private bool enableCustomGames = true;
        [SerializeField] private int maxCustomGames = 100;
        [SerializeField] private float voteDuration = 30f;

        private Dictionary<string, CustomGameMode> customGameModes = new Dictionary<string, CustomGameMode>();
        private Dictionary<string, MapVote> activeVotes = new Dictionary<string, MapVote>();

        public event Action<string, CustomGameMode> OnCustomGameCreated;
        public event Action<string> OnCustomGameStarted;
        public event Action<string, MapVoteResult> OnMapVoteCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        /// <summary>
        /// Create custom game mode
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void CreateCustomGameServerRpc(ulong creatorId, string gameModeName, GameRules rules, ServerRpcParams rpcParams = default)
        {
            if (!enableCustomGames) return;
            if (customGameModes.Count >= maxCustomGames) return;

            string gameModeId = Guid.NewGuid().ToString();

            CustomGameMode gameMode = new CustomGameMode
            {
                gameModeId = gameModeId,
                gameModeName = gameModeName,
                creatorId = creatorId,
                rules = rules,
                createdAt = DateTime.UtcNow,
                timesPlayed = 0,
                rating = 0f
            };

            customGameModes[gameModeId] = gameMode;
            OnCustomGameCreated?.Invoke(gameModeId, gameMode);

            NotifyGameModeCreatedClientRpc(creatorId, gameModeId, gameModeName);
        }

        /// <summary>
        /// Start custom game
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void StartCustomGameServerRpc(string gameModeId, ServerRpcParams rpcParams = default)
        {
            if (!customGameModes.TryGetValue(gameModeId, out var gameMode)) return;

            // Apply custom rules
            ApplyGameRules(gameMode.rules);

            gameMode.timesPlayed++;
            OnCustomGameStarted?.Invoke(gameModeId);

            NotifyGameStartedClientRpc(gameModeId, gameMode.gameModeName);
        }

        private void ApplyGameRules(GameRules rules)
        {
            // Apply health modifiers
            // Apply damage modifiers
            // Apply speed modifiers
            // Apply zombie spawn rules
            // Apply time limits
            // Apply score multipliers
            // Apply weapon restrictions
            // Enable/disable abilities
        }

        /// <summary>
        /// Start map vote
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void StartMapVoteServerRpc(string lobbyId, string[] mapOptions, ServerRpcParams rpcParams = default)
        {
            MapVote vote = new MapVote
            {
                voteId = lobbyId,
                mapOptions = mapOptions.ToList(),
                votes = new Dictionary<string, List<ulong>>(),
                startTime = Time.time,
                duration = voteDuration
            };

            // Initialize vote counts
            foreach (string map in mapOptions)
            {
                vote.votes[map] = new List<ulong>();
            }

            activeVotes[lobbyId] = vote;

            StartMapVoteClientRpc(lobbyId, mapOptions);
        }

        /// <summary>
        /// Cast vote for map
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void CastMapVoteServerRpc(ulong playerId, string lobbyId, string mapChoice, ServerRpcParams rpcParams = default)
        {
            if (!activeVotes.TryGetValue(lobbyId, out var vote)) return;

            // Remove previous vote
            foreach (var kvp in vote.votes)
            {
                kvp.Value.Remove(playerId);
            }

            // Add new vote
            if (vote.votes.ContainsKey(mapChoice))
            {
                vote.votes[mapChoice].Add(playerId);
            }

            UpdateMapVoteClientRpc(lobbyId, GetVoteResults(vote));
        }

        /// <summary>
        /// End map vote and select winner
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void EndMapVoteServerRpc(string lobbyId, ServerRpcParams rpcParams = default)
        {
            if (!activeVotes.TryGetValue(lobbyId, out var vote)) return;

            // Count votes
            var results = GetVoteResults(vote);
            string winningMap = results.OrderByDescending(r => r.voteCount).First().mapName;

            MapVoteResult result = new MapVoteResult
            {
                winningMap = winningMap,
                voteResults = results
            };

            OnMapVoteCompleted?.Invoke(lobbyId, result);
            activeVotes.Remove(lobbyId);

            AnnounceMapVoteResultClientRpc(lobbyId, winningMap);
        }

        private List<MapVoteCount> GetVoteResults(MapVote vote)
        {
            List<MapVoteCount> results = new List<MapVoteCount>();

            foreach (var kvp in vote.votes)
            {
                results.Add(new MapVoteCount
                {
                    mapName = kvp.Key,
                    voteCount = kvp.Value.Count
                });
            }

            return results;
        }

        /// <summary>
        /// Rate custom game mode
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void RateGameModeServerRpc(ulong playerId, string gameModeId, float rating, ServerRpcParams rpcParams = default)
        {
            if (!customGameModes.TryGetValue(gameModeId, out var gameMode)) return;

            rating = Mathf.Clamp(rating, 1f, 5f);

            // Simple average rating (could be improved with weighted average)
            float totalRatings = gameMode.rating * gameMode.ratingCount;
            gameMode.ratingCount++;
            gameMode.rating = (totalRatings + rating) / gameMode.ratingCount;
        }

        /// <summary>
        /// Get popular custom game modes
        /// </summary>
        public List<CustomGameMode> GetPopularGameModes(int count = 10)
        {
            return customGameModes.Values
                .OrderByDescending(g => g.timesPlayed)
                .Take(count)
                .ToList();
        }

        /// <summary>
        /// Get top rated custom game modes
        /// </summary>
        public List<CustomGameMode> GetTopRatedGameModes(int count = 10)
        {
            return customGameModes.Values
                .Where(g => g.ratingCount >= 5) // Minimum ratings required
                .OrderByDescending(g => g.rating)
                .Take(count)
                .ToList();
        }

        [ClientRpc]
        private void NotifyGameModeCreatedClientRpc(ulong creatorId, string gameModeId, string name)
        {
            if (NetworkManager.Singleton.LocalClientId != creatorId) return;
            Debug.Log($"<color=lime>Custom Game Mode Created: {name}</color>");
        }

        [ClientRpc]
        private void NotifyGameStartedClientRpc(string gameModeId, string name)
        {
            Debug.Log($"<color=yellow>Starting Custom Game: {name}</color>");
        }

        [ClientRpc]
        private void StartMapVoteClientRpc(string lobbyId, string[] mapOptions)
        {
            Debug.Log($"<color=cyan>MAP VOTE STARTED</color>");
            for (int i = 0; i < mapOptions.Length; i++)
            {
                Debug.Log($"{i + 1}. {mapOptions[i]}");
            }
            Debug.Log($"Vote with /vote <number>");
        }

        [ClientRpc]
        private void UpdateMapVoteClientRpc(string lobbyId, List<MapVoteCount> results)
        {
            Debug.Log($"<color=cyan>Current Votes:</color>");
            foreach (var result in results)
            {
                Debug.Log($"{result.mapName}: {result.voteCount} votes");
            }
        }

        [ClientRpc]
        private void AnnounceMapVoteResultClientRpc(string lobbyId, string winningMap)
        {
            Debug.Log($"<color=gold>WINNING MAP: {winningMap}</color>");
        }

        [Serializable]
        public class CustomGameMode
        {
            public string gameModeId;
            public string gameModeName;
            public ulong creatorId;
            public GameRules rules;
            public DateTime createdAt;
            public int timesPlayed;
            public float rating;
            public int ratingCount;
        }

        [Serializable]
        public class GameRules
        {
            // Health & Damage
            public float playerHealthMultiplier = 1f;
            public float zombieHealthMultiplier = 1f;
            public float damageMultiplier = 1f;

            // Movement
            public float playerSpeedMultiplier = 1f;
            public float zombieSpeedMultiplier = 1f;

            // Spawning
            public int zombieSpawnRate = 100; // percentage
            public int maxZombiesAtOnce = 50;
            public bool enableBossZombies = true;

            // Time & Scoring
            public int timeLimit = 0; // 0 = unlimited
            public int scoreToWin = 0; // 0 = survive all rounds
            public float scoreMultiplier = 1f;

            // Weapons & Abilities
            public bool enableAllWeapons = true;
            public List<string> allowedWeapons = new List<string>();
            public bool enableAbilities = true;
            public bool infiniteAmmo = false;

            // Special Rules
            public bool friendlyFire = false;
            public bool oneHitKills = false;
            public bool zombiesOneHitKill = false;
            public bool noHealthRegen = false;
            public bool randomWeapons = false;

            // Environmental
            public string weather = "Clear";
            public string timeOfDay = "Day";
            public bool enableHazards = true;
        }

        [Serializable]
        private class MapVote
        {
            public string voteId;
            public List<string> mapOptions;
            public Dictionary<string, List<ulong>> votes;
            public float startTime;
            public float duration;
        }

        [Serializable]
        public class MapVoteResult
        {
            public string winningMap;
            public List<MapVoteCount> voteResults;
        }

        [Serializable]
        public class MapVoteCount
        {
            public string mapName;
            public int voteCount;
        }
    }
}
