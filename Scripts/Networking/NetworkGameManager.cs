using UnityEngine;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DeadFrontier.Networking
{
    /// <summary>
    /// Manages networked game sessions and match flow
    /// </summary>
    public class NetworkGameManager : Core.Singleton<NetworkGameManager>
    {
        [Header("Network Settings")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private Transform[] spawnPoints;

        [Header("Match Settings")]
        [SerializeField] private float matchDuration = 900f; // 15 minutes
        [SerializeField] private float extractionTime = 30f; // 30 seconds to extract

        // Match state
        private NetworkVariable<MatchState> currentMatchState = new NetworkVariable<MatchState>(MatchState.Lobby);
        private NetworkVariable<float> matchTimer = new NetworkVariable<float>();
        private NetworkVariable<int> playersAlive = new NetworkVariable<int>();

        // Player tracking
        private Dictionary<ulong, NetworkedPlayer> connectedPlayers = new Dictionary<ulong, NetworkedPlayer>();
        private List<ulong> extractedPlayers = new List<ulong>();

        // Events
        public event System.Action<MatchState> OnMatchStateChanged;
        public event System.Action<float> OnMatchTimerUpdated;

        // Properties
        public MatchState CurrentMatchState => currentMatchState.Value;
        public float MatchTimeRemaining => matchTimer.Value;
        public bool IsHost => NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;

        protected override void Awake()
        {
            base.Awake();

            // Subscribe to network events
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            }
        }

        private void Update()
        {
            if (!NetworkManager.Singleton.IsServer)
                return;

            // Update match timer on server
            if (currentMatchState.Value == MatchState.InProgress)
            {
                matchTimer.Value -= Time.deltaTime;

                if (matchTimer.Value <= 0f)
                {
                    EndMatch();
                }
            }
        }

        #region Network Callbacks

        private void OnClientConnected(ulong clientId)
        {
            Debug.Log($"[NetworkGameManager] Client {clientId} connected");

            if (NetworkManager.Singleton.IsServer)
            {
                // Spawn player for the client
                SpawnPlayerForClient(clientId);
            }
        }

        private void OnClientDisconnected(ulong clientId)
        {
            Debug.Log($"[NetworkGameManager] Client {clientId} disconnected");

            if (connectedPlayers.ContainsKey(clientId))
            {
                connectedPlayers.Remove(clientId);
            }

            if (NetworkManager.Singleton.IsServer)
            {
                UpdatePlayersAlive();
            }
        }

        #endregion

        #region Match Flow

        /// <summary>
        /// Starts the match (server-only)
        /// </summary>
        public void StartMatch()
        {
            if (!NetworkManager.Singleton.IsServer)
            {
                Debug.LogWarning("[NetworkGameManager] Only server can start match");
                return;
            }

            Debug.Log("[NetworkGameManager] Starting match...");

            // Set match state
            currentMatchState.Value = MatchState.InProgress;
            matchTimer.Value = matchDuration;

            // Lock lobby to prevent new players
            _ = MatchmakingManager.Instance.LockLobbyAsync();

            // Update lobby data
            _ = MatchmakingManager.Instance.UpdateLobbyDataAsync("MatchStarted", "true");

            // Notify all clients
            NotifyMatchStartClientRpc();

            // Spawn zombies
            SpawnInitialZombies();

            // Update GameManager
            Core.GameManager.Instance.StartMatch();

            UpdatePlayersAlive();
        }

        /// <summary>
        /// Ends the match (server-only)
        /// </summary>
        public void EndMatch()
        {
            if (!NetworkManager.Singleton.IsServer)
                return;

            Debug.Log("[NetworkGameManager] Match ended");

            currentMatchState.Value = MatchState.MatchEnd;

            // Notify all clients
            NotifyMatchEndClientRpc();

            // Show results after delay
            Invoke(nameof(ShowMatchResults), 5f);

            // Update GameManager
            Core.GameManager.Instance.EndMatch();
        }

        /// <summary>
        /// Shows match results and returns to lobby
        /// </summary>
        private void ShowMatchResults()
        {
            if (!NetworkManager.Singleton.IsServer)
                return;

            // Calculate stats, rewards, etc.
            // ...

            // Return to lobby
            currentMatchState.Value = MatchState.Lobby;
        }

        [ClientRpc]
        private void NotifyMatchStartClientRpc()
        {
            Debug.Log("[NetworkGameManager] Match started!");

            OnMatchStateChanged?.Invoke(MatchState.InProgress);

            // Show match start UI
            // Hide lobby UI
        }

        [ClientRpc]
        private void NotifyMatchEndClientRpc()
        {
            Debug.Log("[NetworkGameManager] Match ended!");

            OnMatchStateChanged?.Invoke(MatchState.MatchEnd);

            // Show end screen
        }

        #endregion

        #region Player Spawning

        /// <summary>
        /// Spawns a player for a specific client
        /// </summary>
        private void SpawnPlayerForClient(ulong clientId)
        {
            if (!NetworkManager.Singleton.IsServer)
                return;

            // Get spawn point
            Vector3 spawnPosition = GetSpawnPosition();
            Quaternion spawnRotation = Quaternion.identity;

            // Spawn player object
            GameObject playerObj = Instantiate(playerPrefab, spawnPosition, spawnRotation);
            NetworkObject networkObj = playerObj.GetComponent<NetworkObject>();

            if (networkObj != null)
            {
                networkObj.SpawnAsPlayerObject(clientId);

                NetworkedPlayer networkedPlayer = playerObj.GetComponent<NetworkedPlayer>();
                if (networkedPlayer != null)
                {
                    connectedPlayers[clientId] = networkedPlayer;
                    networkedPlayer.SpawnAtPosition(spawnPosition, spawnRotation);
                }

                Debug.Log($"[NetworkGameManager] Spawned player for client {clientId} at {spawnPosition}");
            }
            else
            {
                Debug.LogError("[NetworkGameManager] Player prefab missing NetworkObject component!");
                Destroy(playerObj);
            }
        }

        /// <summary>
        /// Gets a valid spawn position
        /// </summary>
        private Vector3 GetSpawnPosition()
        {
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                // Random spawn point
                int index = Random.Range(0, spawnPoints.Length);
                return spawnPoints[index].position;
            }

            // Default spawn
            return Vector3.up * 2f;
        }

        /// <summary>
        /// Respawns a player (for testing, not used in extraction mode)
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void RespawnPlayerServerRpc(ulong clientId)
        {
            if (!NetworkManager.Singleton.IsServer)
                return;

            if (connectedPlayers.TryGetValue(clientId, out NetworkedPlayer player))
            {
                Vector3 spawnPos = GetSpawnPosition();
                player.SpawnAtPosition(spawnPos, Quaternion.identity);

                Debug.Log($"[NetworkGameManager] Respawned player {clientId}");
            }
        }

        #endregion

        #region Extraction

        /// <summary>
        /// Player requests extraction
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void RequestExtractionServerRpc(ulong clientId)
        {
            if (!NetworkManager.Singleton.IsServer)
                return;

            if (currentMatchState.Value != MatchState.InProgress)
                return;

            Debug.Log($"[NetworkGameManager] Player {clientId} requested extraction");

            // Start extraction timer for this player
            StartExtractionForPlayerClientRpc(clientId);
        }

        [ClientRpc]
        private void StartExtractionForPlayerClientRpc(ulong clientId)
        {
            if (NetworkManager.Singleton.LocalClientId == clientId)
            {
                // Show extraction UI
                Debug.Log("[NetworkGameManager] Extraction started!");

                // Start extraction countdown
                Core.GameManager.Instance.StartExtraction();
            }
        }

        /// <summary>
        /// Completes extraction for a player
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void CompleteExtractionServerRpc(ulong clientId)
        {
            if (!NetworkManager.Singleton.IsServer)
                return;

            if (!extractedPlayers.Contains(clientId))
            {
                extractedPlayers.Add(clientId);

                Debug.Log($"[NetworkGameManager] Player {clientId} successfully extracted!");

                // Notify the player
                NotifyExtractionSuccessClientRpc(clientId);

                // Update alive count
                UpdatePlayersAlive();

                // End match if all players extracted
                if (extractedPlayers.Count >= connectedPlayers.Count)
                {
                    EndMatch();
                }
            }
        }

        [ClientRpc]
        private void NotifyExtractionSuccessClientRpc(ulong clientId)
        {
            if (NetworkManager.Singleton.LocalClientId == clientId)
            {
                Debug.Log("[NetworkGameManager] Extraction successful!");

                // Show success screen
                // Save loot
                // Update stats
            }
        }

        #endregion

        #region Zombie Spawning

        /// <summary>
        /// Spawns initial zombies at match start
        /// </summary>
        private void SpawnInitialZombies()
        {
            if (!NetworkManager.Singleton.IsServer)
                return;

            int zombieCount = 20; // Start with 20 zombies
            float spawnRadius = 100f;

            for (int i = 0; i < zombieCount; i++)
            {
                // Random position around map center
                Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
                Vector3 spawnPos = new Vector3(randomCircle.x, 0f, randomCircle.y);

                // Sample NavMesh
                if (UnityEngine.AI.NavMesh.SamplePosition(spawnPos, out UnityEngine.AI.NavMeshHit hit, 10f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    Zombies.ZombieManager.Instance.SpawnRandomZombie(hit.position);
                }
            }

            Debug.Log($"[NetworkGameManager] Spawned {zombieCount} initial zombies");
        }

        /// <summary>
        /// Spawns a horde at a position (called by special events)
        /// </summary>
        public void SpawnHordeAt(Vector3 position, int count)
        {
            if (!NetworkManager.Singleton.IsServer)
                return;

            Zombies.ZombieManager.Instance.SpawnHorde(position, count, 30f);

            // Notify clients for audio/visual feedback
            NotifyHordeSpawnClientRpc(position);
        }

        [ClientRpc]
        private void NotifyHordeSpawnClientRpc(Vector3 position)
        {
            Debug.Log($"[NetworkGameManager] Horde spawned at {position}!");

            // Play ominous sound
            // Show notification
        }

        #endregion

        #region Stats

        private void UpdatePlayersAlive()
        {
            if (!NetworkManager.Singleton.IsServer)
                return;

            int alive = connectedPlayers.Count - extractedPlayers.Count;
            playersAlive.Value = alive;
        }

        /// <summary>
        /// Gets player statistics
        /// </summary>
        public PlayerMatchStats GetPlayerStats(ulong clientId)
        {
            // TODO: Track kills, damage, loot collected, etc.
            return new PlayerMatchStats
            {
                clientId = clientId,
                kills = 0,
                deaths = 0,
                damageDealt = 0,
                lootCollected = 0,
                extracted = extractedPlayers.Contains(clientId)
            };
        }

        #endregion

        #region Host Migration (Future)

        // TODO: Implement host migration for when host disconnects
        // Unity Netcode has built-in support for this

        #endregion

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            }
        }
    }

    public enum MatchState
    {
        Lobby,
        Loading,
        InProgress,
        Extraction,
        MatchEnd
    }

    [System.Serializable]
    public struct PlayerMatchStats
    {
        public ulong clientId;
        public int kills;
        public int deaths;
        public float damageDealt;
        public int lootCollected;
        public bool extracted;
    }
}
