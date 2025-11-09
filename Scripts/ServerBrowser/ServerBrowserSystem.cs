using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace ServerBrowser
{
    /// <summary>
    /// Comprehensive server browser and custom matchmaking system for multi-platform zombie multiplayer game.
    /// Provides server listing, filtering, favorites, and direct connection capabilities.
    /// </summary>
    public class ServerBrowserSystem : NetworkBehaviour
    {
        public static ServerBrowserSystem Instance { get; private set; }

        [Header("Browser Settings")]
        [SerializeField] private bool enableServerBrowser = true;
        [SerializeField] private float refreshInterval = 5f;
        [SerializeField] private int maxServersListed = 500;
        [SerializeField] private bool enablePasswordProtection = true;

        [Header("Filter Settings")]
        [SerializeField] private bool enableRegionFilter = true;
        [SerializeField] private bool enableGameModeFilter = true;
        [SerializeField] private bool enablePlayerCountFilter = true;
        [SerializeField] private int maxPingFilter = 200;

        [Header("Server Settings")]
        [SerializeField] private int defaultMaxPlayers = 100;
        [SerializeField] private bool allowPrivateServers = true;
        [SerializeField] private int serverListTTL = 300; // 5 minutes

        // Enums
        public enum ServerRegion
        {
            NorthAmerica,
            SouthAmerica,
            Europe,
            Asia,
            Oceania,
            Africa,
            MiddleEast
        }

        public enum ServerStatus
        {
            Online,
            Starting,
            Stopping,
            Full,
            Offline,
            Maintenance
        }

        public enum GameMode
        {
            Extraction,
            Survival,
            TeamDeathmatch,
            FreeForAll,
            Horde,
            Custom
        }

        // Data structures
        [Serializable]
        public class ServerInfo
        {
            public string serverId;
            public string serverName;
            public string serverDescription;
            public string hostName;
            public string ipAddress;
            public int port;
            public ServerRegion region;
            public GameMode gameMode;
            public string mapName;
            public int currentPlayers;
            public int maxPlayers;
            public int ping;
            public ServerStatus status;
            public bool hasPassword;
            public bool isOfficial;
            public bool isFavorite;
            public bool allowMods;
            public DateTime creationTime;
            public DateTime lastUpdate;
            public Dictionary<string, string> serverTags = new Dictionary<string, string>();
            public Dictionary<string, object> customData = new Dictionary<string, object>();
        }

        [Serializable]
        public class ServerFilter
        {
            public string searchText;
            public ServerRegion? region;
            public GameMode? gameMode;
            public int? minPlayers;
            public int? maxPlayers;
            public int maxPing = 200;
            public bool hideFullServers = true;
            public bool hidePasswordProtected = false;
            public bool officialOnly = false;
            public bool showFavoritesOnly = false;
            public List<string> requiredTags = new List<string>();
            public List<string> excludedTags = new List<string>();
        }

        [Serializable]
        public class QuickJoinPreferences
        {
            public ServerRegion preferredRegion;
            public GameMode preferredMode;
            public int maxPing = 100;
            public bool avoidFullServers = true;
        }

        [Serializable]
        public class ServerHistory
        {
            public string serverId;
            public DateTime lastJoined;
            public int timesJoined;
            public float averagePing;
            public float averagePlayerCount;
        }

        // State
        private Dictionary<string, ServerInfo> availableServers = new Dictionary<string, ServerInfo>();
        private Dictionary<ulong, HashSet<string>> playerFavorites = new Dictionary<ulong, HashSet<string>>();
        private Dictionary<ulong, List<ServerHistory>> playerHistory = new Dictionary<ulong, List<ServerHistory>>();
        private ServerInfo currentServerInfo;
        private float lastRefreshTime;

        // Events
        public event Action<List<ServerInfo>> OnServerListUpdated;
        public event Action<ServerInfo> OnServerJoined;
        public event Action<ServerInfo> OnServerLeft;
        public event Action<string> OnServerFavorited;
        public event Action<string> OnServerUnfavorited;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                InitializeServerBrowser();
            }
        }

        private void Update()
        {
            if (!IsServer || !enableServerBrowser) return;

            if (Time.time - lastRefreshTime >= refreshInterval)
            {
                RefreshServerList();
                lastRefreshTime = Time.time;
            }

            CleanupStaleServers();
        }

        #region Initialization

        private void InitializeServerBrowser()
        {
            lastRefreshTime = Time.time;

            // Register this server if hosting
            if (IsServer)
            {
                RegisterCurrentServer();
            }
        }

        #endregion

        #region Server Registration

        [ServerRpc(RequireOwnership = false)]
        public void RegisterServerServerRpc(ServerInfo serverInfo)
        {
            if (availableServers.Count >= maxServersListed)
            {
                Debug.LogWarning("Maximum server limit reached");
                return;
            }

            serverInfo.serverId = string.IsNullOrEmpty(serverInfo.serverId)
                ? $"server_{Guid.NewGuid()}"
                : serverInfo.serverId;

            serverInfo.lastUpdate = DateTime.UtcNow;
            serverInfo.status = ServerStatus.Online;

            availableServers[serverInfo.serverId] = serverInfo;

            Debug.Log($"Server registered: {serverInfo.serverName} ({serverInfo.serverId})");

            BroadcastServerListUpdate();
        }

        private void RegisterCurrentServer()
        {
            currentServerInfo = new ServerInfo
            {
                serverId = $"server_{NetworkManager.Singleton.LocalClientId}",
                serverName = "Official Server",
                serverDescription = "Official zombie survival server",
                hostName = "Server Host",
                region = ServerRegion.NorthAmerica,
                gameMode = GameMode.Extraction,
                mapName = "Map_01",
                currentPlayers = 0,
                maxPlayers = defaultMaxPlayers,
                status = ServerStatus.Online,
                isOfficial = true,
                creationTime = DateTime.UtcNow,
                lastUpdate = DateTime.UtcNow
            };

            availableServers[currentServerInfo.serverId] = currentServerInfo;
        }

        [ServerRpc(RequireOwnership = false)]
        public void UnregisterServerServerRpc(string serverId)
        {
            if (availableServers.ContainsKey(serverId))
            {
                availableServers[serverId].status = ServerStatus.Offline;

                // Remove after delay
                Invoke(() => availableServers.Remove(serverId), 60f);

                BroadcastServerListUpdate();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void UpdateServerInfoServerRpc(string serverId, int currentPlayers, ServerStatus status)
        {
            if (!availableServers.ContainsKey(serverId)) return;

            var server = availableServers[serverId];
            server.currentPlayers = currentPlayers;
            server.status = status;
            server.lastUpdate = DateTime.UtcNow;

            BroadcastServerListUpdate();
        }

        #endregion

        #region Server Browsing

        [ServerRpc(RequireOwnership = false)]
        public void RequestServerListServerRpc(ulong requesterId)
        {
            var serverList = GetFilteredServers(new ServerFilter());
            SendServerListClientRpc(requesterId, serverList.ToArray());
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestFilteredServerListServerRpc(ulong requesterId, ServerFilter filter)
        {
            var serverList = GetFilteredServers(filter);
            SendServerListClientRpc(requesterId, serverList.ToArray());
        }

        private List<ServerInfo> GetFilteredServers(ServerFilter filter)
        {
            var filtered = availableServers.Values.AsEnumerable();

            // Text search
            if (!string.IsNullOrEmpty(filter.searchText))
            {
                filtered = filtered.Where(s =>
                    s.serverName.Contains(filter.searchText, StringComparison.OrdinalIgnoreCase) ||
                    s.serverDescription.Contains(filter.searchText, StringComparison.OrdinalIgnoreCase));
            }

            // Region filter
            if (filter.region.HasValue)
            {
                filtered = filtered.Where(s => s.region == filter.region.Value);
            }

            // Game mode filter
            if (filter.gameMode.HasValue)
            {
                filtered = filtered.Where(s => s.gameMode == filter.gameMode.Value);
            }

            // Player count filters
            if (filter.minPlayers.HasValue)
            {
                filtered = filtered.Where(s => s.currentPlayers >= filter.minPlayers.Value);
            }

            if (filter.maxPlayers.HasValue)
            {
                filtered = filtered.Where(s => s.currentPlayers <= filter.maxPlayers.Value);
            }

            // Ping filter
            if (filter.maxPing > 0)
            {
                filtered = filtered.Where(s => s.ping <= filter.maxPing);
            }

            // Hide full servers
            if (filter.hideFullServers)
            {
                filtered = filtered.Where(s => s.currentPlayers < s.maxPlayers);
            }

            // Hide password protected
            if (filter.hidePasswordProtected)
            {
                filtered = filtered.Where(s => !s.hasPassword);
            }

            // Official only
            if (filter.officialOnly)
            {
                filtered = filtered.Where(s => s.isOfficial);
            }

            // Status filter
            filtered = filtered.Where(s => s.status == ServerStatus.Online);

            // Tag filters
            if (filter.requiredTags.Any())
            {
                filtered = filtered.Where(s => filter.requiredTags.All(tag => s.serverTags.ContainsKey(tag)));
            }

            if (filter.excludedTags.Any())
            {
                filtered = filtered.Where(s => !filter.excludedTags.Any(tag => s.serverTags.ContainsKey(tag)));
            }

            return filtered.OrderByDescending(s => s.isOfficial)
                          .ThenBy(s => s.ping)
                          .ThenByDescending(s => s.currentPlayers)
                          .ToList();
        }

        [ClientRpc]
        private void SendServerListClientRpc(ulong targetClientId, ServerInfo[] servers)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            OnServerListUpdated?.Invoke(servers.ToList());
        }

        private void RefreshServerList()
        {
            // Update current server player count
            if (currentServerInfo != null)
            {
                currentServerInfo.currentPlayers = NetworkManager.Singleton.ConnectedClients.Count;
                currentServerInfo.lastUpdate = DateTime.UtcNow;
            }
        }

        private void BroadcastServerListUpdate()
        {
            var serverList = availableServers.Values
                .Where(s => s.status == ServerStatus.Online)
                .ToArray();

            BroadcastServerListClientRpc(serverList);
        }

        [ClientRpc]
        private void BroadcastServerListClientRpc(ServerInfo[] servers)
        {
            OnServerListUpdated?.Invoke(servers.ToList());
        }

        #endregion

        #region Server Connection

        [ServerRpc(RequireOwnership = false)]
        public void JoinServerServerRpc(ulong playerId, string serverId, string password = "")
        {
            if (!availableServers.ContainsKey(serverId))
            {
                NotifyJoinFailedClientRpc(playerId, "Server not found");
                return;
            }

            var server = availableServers[serverId];

            // Check if server is full
            if (server.currentPlayers >= server.maxPlayers)
            {
                NotifyJoinFailedClientRpc(playerId, "Server is full");
                return;
            }

            // Check password
            if (server.hasPassword && !ValidatePassword(serverId, password))
            {
                NotifyJoinFailedClientRpc(playerId, "Incorrect password");
                return;
            }

            // Record join in history
            RecordServerJoin(playerId, serverId);

            OnServerJoined?.Invoke(server);
            NotifyJoinSuccessClientRpc(playerId, serverId);

            Debug.Log($"Player {playerId} joined server {serverId}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void QuickJoinServerRpc(ulong playerId, QuickJoinPreferences preferences)
        {
            var filter = new ServerFilter
            {
                region = preferences.preferredRegion,
                gameMode = preferences.preferredMode,
                maxPing = preferences.maxPing,
                hideFullServers = preferences.avoidFullServers,
                hidePasswordProtected = true,
                officialOnly = false
            };

            var servers = GetFilteredServers(filter);

            if (servers.Count == 0)
            {
                NotifyJoinFailedClientRpc(playerId, "No suitable servers found");
                return;
            }

            // Find best match
            var bestServer = servers
                .OrderBy(s => s.ping)
                .ThenByDescending(s => s.currentPlayers)
                .First();

            JoinServerServerRpc(playerId, bestServer.serverId);
        }

        private bool ValidatePassword(string serverId, string password)
        {
            // Implement password validation
            // For now, return true
            return true;
        }

        [ClientRpc]
        private void NotifyJoinSuccessClientRpc(ulong playerId, string serverId)
        {
            if (NetworkManager.Singleton.LocalClientId != playerId) return;

            if (availableServers.ContainsKey(serverId))
            {
                OnServerJoined?.Invoke(availableServers[serverId]);
            }
        }

        [ClientRpc]
        private void NotifyJoinFailedClientRpc(ulong playerId, string reason)
        {
            if (NetworkManager.Singleton.LocalClientId != playerId) return;

            Debug.LogWarning($"Failed to join server: {reason}");
        }

        #endregion

        #region Favorites

        [ServerRpc(RequireOwnership = false)]
        public void AddFavoriteServerRpc(ulong playerId, string serverId)
        {
            if (!playerFavorites.ContainsKey(playerId))
            {
                playerFavorites[playerId] = new HashSet<string>();
            }

            playerFavorites[playerId].Add(serverId);

            if (availableServers.ContainsKey(serverId))
            {
                availableServers[serverId].isFavorite = true;
            }

            OnServerFavorited?.Invoke(serverId);
            NotifyFavoriteAddedClientRpc(playerId, serverId);

            Debug.Log($"Player {playerId} favorited server {serverId}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void RemoveFavoriteServerRpc(ulong playerId, string serverId)
        {
            if (!playerFavorites.ContainsKey(playerId)) return;

            playerFavorites[playerId].Remove(serverId);

            if (availableServers.ContainsKey(serverId))
            {
                availableServers[serverId].isFavorite = false;
            }

            OnServerUnfavorited?.Invoke(serverId);
            NotifyFavoriteRemovedClientRpc(playerId, serverId);
        }

        [ClientRpc]
        private void NotifyFavoriteAddedClientRpc(ulong playerId, string serverId)
        {
            if (NetworkManager.Singleton.LocalClientId != playerId) return;
            OnServerFavorited?.Invoke(serverId);
        }

        [ClientRpc]
        private void NotifyFavoriteRemovedClientRpc(ulong playerId, string serverId)
        {
            if (NetworkManager.Singleton.LocalClientId != playerId) return;
            OnServerUnfavorited?.Invoke(serverId);
        }

        public List<string> GetPlayerFavorites(ulong playerId)
        {
            return playerFavorites.ContainsKey(playerId)
                ? playerFavorites[playerId].ToList()
                : new List<string>();
        }

        #endregion

        #region Server History

        private void RecordServerJoin(ulong playerId, string serverId)
        {
            if (!playerHistory.ContainsKey(playerId))
            {
                playerHistory[playerId] = new List<ServerHistory>();
            }

            var history = playerHistory[playerId];
            var existing = history.FirstOrDefault(h => h.serverId == serverId);

            if (existing != null)
            {
                existing.lastJoined = DateTime.UtcNow;
                existing.timesJoined++;

                if (availableServers.ContainsKey(serverId))
                {
                    var server = availableServers[serverId];
                    existing.averagePing = (existing.averagePing * (existing.timesJoined - 1) + server.ping) / existing.timesJoined;
                    existing.averagePlayerCount = (existing.averagePlayerCount * (existing.timesJoined - 1) + server.currentPlayers) / existing.timesJoined;
                }
            }
            else
            {
                var newHistory = new ServerHistory
                {
                    serverId = serverId,
                    lastJoined = DateTime.UtcNow,
                    timesJoined = 1
                };

                if (availableServers.ContainsKey(serverId))
                {
                    var server = availableServers[serverId];
                    newHistory.averagePing = server.ping;
                    newHistory.averagePlayerCount = server.currentPlayers;
                }

                history.Add(newHistory);
            }

            // Keep last 50 history entries
            if (history.Count > 50)
            {
                history.RemoveAt(0);
            }
        }

        public List<ServerHistory> GetPlayerHistory(ulong playerId)
        {
            return playerHistory.ContainsKey(playerId)
                ? playerHistory[playerId].OrderByDescending(h => h.lastJoined).ToList()
                : new List<ServerHistory>();
        }

        #endregion

        #region Ping Calculation

        public void UpdateServerPing(string serverId, int ping)
        {
            if (availableServers.ContainsKey(serverId))
            {
                availableServers[serverId].ping = ping;
            }
        }

        #endregion

        #region Cleanup

        private void CleanupStaleServers()
        {
            var staleServers = availableServers.Values
                .Where(s => (DateTime.UtcNow - s.lastUpdate).TotalSeconds > serverListTTL)
                .Select(s => s.serverId)
                .ToList();

            foreach (var serverId in staleServers)
            {
                Debug.Log($"Removing stale server: {serverId}");
                availableServers.Remove(serverId);
            }

            if (staleServers.Any())
            {
                BroadcastServerListUpdate();
            }
        }

        #endregion

        #region Public API

        public List<ServerInfo> GetAllServers()
        {
            return availableServers.Values
                .Where(s => s.status == ServerStatus.Online)
                .ToList();
        }

        public ServerInfo GetServer(string serverId)
        {
            return availableServers.ContainsKey(serverId) ? availableServers[serverId] : null;
        }

        public int GetTotalServerCount()
        {
            return availableServers.Count(s => s.Value.status == ServerStatus.Online);
        }

        public int GetTotalPlayerCount()
        {
            return availableServers.Values
                .Where(s => s.status == ServerStatus.Online)
                .Sum(s => s.currentPlayers);
        }

        public List<ServerInfo> GetOfficialServers()
        {
            return availableServers.Values
                .Where(s => s.isOfficial && s.status == ServerStatus.Online)
                .ToList();
        }

        public List<ServerInfo> GetRegionServers(ServerRegion region)
        {
            return availableServers.Values
                .Where(s => s.region == region && s.status == ServerStatus.Online)
                .ToList();
        }

        #endregion
    }
}
