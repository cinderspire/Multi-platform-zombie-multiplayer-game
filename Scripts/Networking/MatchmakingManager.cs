using UnityEngine;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace DeadFrontier.Networking
{
    /// <summary>
    /// Handles matchmaking, lobby creation, and joining using Unity Lobby Service
    /// </summary>
    public class MatchmakingManager : Core.Singleton<MatchmakingManager>
    {
        [Header("Lobby Settings")]
        [SerializeField] private string lobbyName = "Dead Frontier Match";
        [SerializeField] private int maxPlayers = Core.Constants.MAX_PLAYERS;
        [SerializeField] private bool isPrivate = false;

        [Header("Heartbeat")]
        [SerializeField] private float heartbeatInterval = 15f; // Send heartbeat every 15s

        private Lobby currentLobby;
        private float heartbeatTimer;
        private bool isInLobby = false;

        // Events
        public event Action<Lobby> OnLobbyCreated;
        public event Action<Lobby> OnLobbyJoined;
        public event Action OnLobbyLeft;
        public event Action<List<Lobby>> OnLobbiesFound;

        // Properties
        public Lobby CurrentLobby => currentLobby;
        public bool IsInLobby => isInLobby;
        public bool IsHost => currentLobby != null && currentLobby.HostId == GetPlayerId();

        private void Update()
        {
            if (isInLobby && IsHost)
            {
                UpdateLobbyHeartbeat();
            }
        }

        #region Lobby Creation

        /// <summary>
        /// Creates a new lobby
        /// </summary>
        public async Task<Lobby> CreateLobbyAsync(string customLobbyName = null, int customMaxPlayers = 0)
        {
            try
            {
                string finalLobbyName = string.IsNullOrEmpty(customLobbyName) ? lobbyName : customLobbyName;
                int finalMaxPlayers = customMaxPlayers > 0 ? customMaxPlayers : maxPlayers;

                CreateLobbyOptions options = new CreateLobbyOptions
                {
                    IsPrivate = isPrivate,
                    Player = GetPlayer(),
                    Data = new Dictionary<string, DataObject>
                    {
                        { "GameMode", new DataObject(DataObject.VisibilityOptions.Public, "Extraction") },
                        { "Map", new DataObject(DataObject.VisibilityOptions.Public, "DowntownRuins") },
                        { "MatchStarted", new DataObject(DataObject.VisibilityOptions.Public, "false") }
                    }
                };

                currentLobby = await LobbyService.Instance.CreateLobbyAsync(finalLobbyName, finalMaxPlayers, options);
                isInLobby = true;

                Debug.Log($"[MatchmakingManager] Created lobby: {currentLobby.Name} (ID: {currentLobby.Id})");

                OnLobbyCreated?.Invoke(currentLobby);
                return currentLobby;
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"[MatchmakingManager] Failed to create lobby: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Creates a private lobby with join code
        /// </summary>
        public async Task<Lobby> CreatePrivateLobbyAsync(string customLobbyName = null)
        {
            isPrivate = true;
            Lobby lobby = await CreateLobbyAsync(customLobbyName);
            isPrivate = false; // Reset

            if (lobby != null)
            {
                Debug.Log($"[MatchmakingManager] Private lobby code: {lobby.LobbyCode}");
            }

            return lobby;
        }

        #endregion

        #region Lobby Joining

        /// <summary>
        /// Quick join - finds and joins any available lobby
        /// </summary>
        public async Task<Lobby> QuickJoinLobbyAsync()
        {
            try
            {
                QuickJoinLobbyOptions options = new QuickJoinLobbyOptions
                {
                    Player = GetPlayer()
                };

                currentLobby = await LobbyService.Instance.QuickJoinLobbyAsync(options);
                isInLobby = true;

                Debug.Log($"[MatchmakingManager] Quick joined lobby: {currentLobby.Name}");

                OnLobbyJoined?.Invoke(currentLobby);
                return currentLobby;
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"[MatchmakingManager] Quick join failed: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Joins a lobby by ID
        /// </summary>
        public async Task<Lobby> JoinLobbyByIdAsync(string lobbyId)
        {
            try
            {
                JoinLobbyByIdOptions options = new JoinLobbyByIdOptions
                {
                    Player = GetPlayer()
                };

                currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId, options);
                isInLobby = true;

                Debug.Log($"[MatchmakingManager] Joined lobby by ID: {currentLobby.Name}");

                OnLobbyJoined?.Invoke(currentLobby);
                return currentLobby;
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"[MatchmakingManager] Join by ID failed: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Joins a lobby by code (private lobbies)
        /// </summary>
        public async Task<Lobby> JoinLobbyByCodeAsync(string lobbyCode)
        {
            try
            {
                JoinLobbyByCodeOptions options = new JoinLobbyByCodeOptions
                {
                    Player = GetPlayer()
                };

                currentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, options);
                isInLobby = true;

                Debug.Log($"[MatchmakingManager] Joined lobby by code: {currentLobby.Name}");

                OnLobbyJoined?.Invoke(currentLobby);
                return currentLobby;
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"[MatchmakingManager] Join by code failed: {e.Message}");
                return null;
            }
        }

        #endregion

        #region Lobby Queries

        /// <summary>
        /// Queries for available lobbies
        /// </summary>
        public async Task<List<Lobby>> QueryLobbiesAsync()
        {
            try
            {
                QueryLobbiesOptions options = new QueryLobbiesOptions
                {
                    Count = 25,
                    Filters = new List<QueryFilter>
                    {
                        new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT),
                        new QueryFilter(QueryFilter.FieldOptions.IsLocked, "0", QueryFilter.OpOptions.EQ)
                    },
                    Order = new List<QueryOrder>
                    {
                        new QueryOrder(false, QueryOrder.FieldOptions.Created)
                    }
                };

                QueryResponse response = await Lobbies.Instance.QueryLobbiesAsync(options);

                Debug.Log($"[MatchmakingManager] Found {response.Results.Count} lobbies");

                OnLobbiesFound?.Invoke(response.Results);
                return response.Results;
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"[MatchmakingManager] Query lobbies failed: {e.Message}");
                return new List<Lobby>();
            }
        }

        #endregion

        #region Lobby Management

        /// <summary>
        /// Leaves the current lobby
        /// </summary>
        public async Task LeaveLobbyAsync()
        {
            if (currentLobby == null)
                return;

            try
            {
                string playerId = GetPlayerId();
                await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, playerId);

                Debug.Log($"[MatchmakingManager] Left lobby: {currentLobby.Name}");

                currentLobby = null;
                isInLobby = false;

                OnLobbyLeft?.Invoke();
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"[MatchmakingManager] Leave lobby failed: {e.Message}");
            }
        }

        /// <summary>
        /// Deletes the lobby (host only)
        /// </summary>
        public async Task DeleteLobbyAsync()
        {
            if (currentLobby == null || !IsHost)
                return;

            try
            {
                await LobbyService.Instance.DeleteLobbyAsync(currentLobby.Id);

                Debug.Log($"[MatchmakingManager] Deleted lobby: {currentLobby.Name}");

                currentLobby = null;
                isInLobby = false;

                OnLobbyLeft?.Invoke();
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"[MatchmakingManager] Delete lobby failed: {e.Message}");
            }
        }

        /// <summary>
        /// Updates lobby data
        /// </summary>
        public async Task UpdateLobbyDataAsync(string key, string value)
        {
            if (currentLobby == null || !IsHost)
                return;

            try
            {
                UpdateLobbyOptions options = new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>
                    {
                        { key, new DataObject(DataObject.VisibilityOptions.Public, value) }
                    }
                };

                currentLobby = await LobbyService.Instance.UpdateLobbyAsync(currentLobby.Id, options);

                Debug.Log($"[MatchmakingManager] Updated lobby data: {key} = {value}");
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"[MatchmakingManager] Update lobby data failed: {e.Message}");
            }
        }

        /// <summary>
        /// Locks the lobby (prevents new players from joining)
        /// </summary>
        public async Task LockLobbyAsync()
        {
            if (currentLobby == null || !IsHost)
                return;

            try
            {
                UpdateLobbyOptions options = new UpdateLobbyOptions
                {
                    IsLocked = true
                };

                currentLobby = await LobbyService.Instance.UpdateLobbyAsync(currentLobby.Id, options);

                Debug.Log("[MatchmakingManager] Lobby locked");
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"[MatchmakingManager] Lock lobby failed: {e.Message}");
            }
        }

        #endregion

        #region Heartbeat

        private void UpdateLobbyHeartbeat()
        {
            heartbeatTimer += Time.deltaTime;

            if (heartbeatTimer >= heartbeatInterval)
            {
                SendHeartbeat();
                heartbeatTimer = 0f;
            }
        }

        private async void SendHeartbeat()
        {
            if (currentLobby == null)
                return;

            try
            {
                await LobbyService.Instance.SendHeartbeatPingAsync(currentLobby.Id);
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"[MatchmakingManager] Heartbeat failed: {e.Message}");
            }
        }

        #endregion

        #region Helpers

        private Player GetPlayer()
        {
            return new Player
            {
                Id = GetPlayerId(),
                Data = new Dictionary<string, PlayerDataObject>
                {
                    { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, "Player") }
                }
            };
        }

        private string GetPlayerId()
        {
            return Unity.Services.Authentication.AuthenticationService.Instance.PlayerId;
        }

        #endregion

        protected override void OnDestroy()
        {
            base.OnDestroy();

            // Leave lobby when destroyed
            if (isInLobby)
            {
                _ = LeaveLobbyAsync();
            }
        }
    }
}
