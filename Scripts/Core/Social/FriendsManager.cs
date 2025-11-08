using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace DeadFrontier.Core.Social
{
    /// <summary>
    /// Manages friend system with add, remove, block, and online status
    /// Integrates with matchmaking for squad formation
    /// </summary>
    public class FriendsManager : Singleton<FriendsManager>
    {
        [Header("Settings")]
        [SerializeField] private int maxFriends = 100;
        [SerializeField] private int maxPendingRequests = 50;
        [SerializeField] private float onlineStatusUpdateInterval = 30f;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;
        [SerializeField] private bool useLocalStorage = true; // For testing without backend

        // Friends data
        private List<FriendData> friends = new List<FriendData>();
        private List<FriendRequest> pendingRequests = new List<FriendRequest>();
        private List<string> blockedPlayers = new List<string>();

        // Update timer
        private float updateTimer = 0f;

        // Events
        public event System.Action<FriendData> OnFriendAdded;
        public event System.Action<string> OnFriendRemoved;
        public event System.Action<FriendRequest> OnFriendRequestReceived;
        public event System.Action<FriendData> OnFriendOnline;
        public event System.Action<FriendData> OnFriendOffline;

        protected override void Awake()
        {
            base.Awake();
            LoadFriendsData();
        }

        private void Update()
        {
            updateTimer += Time.deltaTime;
            if (updateTimer >= onlineStatusUpdateInterval)
            {
                updateTimer = 0f;
                UpdateOnlineStatus();
            }
        }

        #region Friend Management

        /// <summary>
        /// Sends a friend request to a player
        /// </summary>
        public void SendFriendRequest(string playerId, string playerName)
        {
            if (string.IsNullOrEmpty(playerId))
            {
                if (showDebugLogs)
                    Debug.LogWarning("[FriendsManager] Invalid player ID");
                return;
            }

            // Check if already friends
            if (IsFriend(playerId))
            {
                if (showDebugLogs)
                    Debug.LogWarning($"[FriendsManager] Already friends with {playerName}");
                return;
            }

            // Check if blocked
            if (IsBlocked(playerId))
            {
                if (showDebugLogs)
                    Debug.LogWarning($"[FriendsManager] Cannot send request to blocked player");
                return;
            }

            // Check friend limit
            if (friends.Count >= maxFriends)
            {
                if (showDebugLogs)
                    Debug.LogWarning($"[FriendsManager] Friend limit reached ({maxFriends})");
                return;
            }

            if (useLocalStorage)
            {
                // In real implementation, this would send to backend
                if (showDebugLogs)
                    Debug.Log($"[FriendsManager] Friend request sent to {playerName}");

                // For testing, auto-accept
                AcceptFriendRequest(playerId, playerName);
            }
            else
            {
                // TODO: Send to backend
                SendFriendRequestToBackend(playerId);
            }

            // Track analytics
            Analytics.AnalyticsManager.Instance?.TrackEvent("friend_request_sent", new Dictionary<string, object>
            {
                { "target_player_id", playerId }
            });
        }

        /// <summary>
        /// Accepts a friend request
        /// </summary>
        public void AcceptFriendRequest(string playerId, string playerName)
        {
            var request = pendingRequests.Find(r => r.senderId == playerId);
            if (request != null)
            {
                pendingRequests.Remove(request);
            }

            // Add as friend
            var friend = new FriendData
            {
                playerId = playerId,
                playerName = playerName,
                friendsSince = System.DateTime.Now,
                isOnline = false,
                lastOnline = System.DateTime.Now
            };

            friends.Add(friend);
            SaveFriendsData();

            if (showDebugLogs)
                Debug.Log($"[FriendsManager] Added friend: {playerName}");

            OnFriendAdded?.Invoke(friend);

            // Show notification
            if (UI.NotificationManager.Instance != null)
            {
                UI.NotificationManager.Instance.ShowNotification(
                    "New Friend!",
                    $"{playerName} is now your friend",
                    new Color(0.3f, 1f, 0.3f),
                    UI.NotificationType.Info
                );
            }

            // Track analytics
            Analytics.AnalyticsManager.Instance?.TrackEvent("friend_added", new Dictionary<string, object>
            {
                { "friend_player_id", playerId },
                { "total_friends", friends.Count }
            });
        }

        /// <summary>
        /// Declines a friend request
        /// </summary>
        public void DeclineFriendRequest(string playerId)
        {
            var request = pendingRequests.Find(r => r.senderId == playerId);
            if (request != null)
            {
                pendingRequests.Remove(request);
                SaveFriendsData();

                if (showDebugLogs)
                    Debug.Log($"[FriendsManager] Declined friend request from {request.senderName}");
            }
        }

        /// <summary>
        /// Removes a friend
        /// </summary>
        public void RemoveFriend(string playerId)
        {
            var friend = friends.Find(f => f.playerId == playerId);
            if (friend != null)
            {
                friends.Remove(friend);
                SaveFriendsData();

                if (showDebugLogs)
                    Debug.Log($"[FriendsManager] Removed friend: {friend.playerName}");

                OnFriendRemoved?.Invoke(playerId);

                // Track analytics
                Analytics.AnalyticsManager.Instance?.TrackEvent("friend_removed", new Dictionary<string, object>
                {
                    { "friend_player_id", playerId },
                    { "total_friends", friends.Count }
                });
            }
        }

        #endregion

        #region Block Management

        /// <summary>
        /// Blocks a player
        /// </summary>
        public void BlockPlayer(string playerId)
        {
            if (!blockedPlayers.Contains(playerId))
            {
                blockedPlayers.Add(playerId);

                // Remove from friends if present
                RemoveFriend(playerId);

                SaveFriendsData();

                if (showDebugLogs)
                    Debug.Log($"[FriendsManager] Blocked player: {playerId}");

                // Track analytics
                Analytics.AnalyticsManager.Instance?.TrackEvent("player_blocked", new Dictionary<string, object>
                {
                    { "blocked_player_id", playerId }
                });
            }
        }

        /// <summary>
        /// Unblocks a player
        /// </summary>
        public void UnblockPlayer(string playerId)
        {
            if (blockedPlayers.Remove(playerId))
            {
                SaveFriendsData();

                if (showDebugLogs)
                    Debug.Log($"[FriendsManager] Unblocked player: {playerId}");
            }
        }

        /// <summary>
        /// Checks if a player is blocked
        /// </summary>
        public bool IsBlocked(string playerId)
        {
            return blockedPlayers.Contains(playerId);
        }

        #endregion

        #region Online Status

        private void UpdateOnlineStatus()
        {
            if (useLocalStorage)
            {
                // Simulate some friends coming online/offline
                foreach (var friend in friends)
                {
                    bool wasOnline = friend.isOnline;
                    friend.isOnline = UnityEngine.Random.value > 0.5f;

                    if (friend.isOnline && !wasOnline)
                    {
                        OnFriendOnline?.Invoke(friend);
                    }
                    else if (!friend.isOnline && wasOnline)
                    {
                        friend.lastOnline = System.DateTime.Now;
                        OnFriendOffline?.Invoke(friend);
                    }
                }
            }
            else
            {
                // TODO: Fetch from backend
                FetchOnlineStatusFromBackend();
            }
        }

        #endregion

        #region Queries

        /// <summary>
        /// Gets all friends
        /// </summary>
        public List<FriendData> GetAllFriends()
        {
            return new List<FriendData>(friends);
        }

        /// <summary>
        /// Gets online friends
        /// </summary>
        public List<FriendData> GetOnlineFriends()
        {
            return friends.Where(f => f.isOnline).ToList();
        }

        /// <summary>
        /// Gets pending friend requests
        /// </summary>
        public List<FriendRequest> GetPendingRequests()
        {
            return new List<FriendRequest>(pendingRequests);
        }

        /// <summary>
        /// Checks if a player is a friend
        /// </summary>
        public bool IsFriend(string playerId)
        {
            return friends.Exists(f => f.playerId == playerId);
        }

        /// <summary>
        /// Gets friend data by player ID
        /// </summary>
        public FriendData GetFriend(string playerId)
        {
            return friends.Find(f => f.playerId == playerId);
        }

        /// <summary>
        /// Searches friends by name
        /// </summary>
        public List<FriendData> SearchFriends(string searchQuery)
        {
            return friends.Where(f => f.playerName.ToLower().Contains(searchQuery.ToLower())).ToList();
        }

        #endregion

        #region Squad Formation

        /// <summary>
        /// Invites a friend to join squad
        /// </summary>
        public void InviteToSquad(string friendId)
        {
            var friend = GetFriend(friendId);
            if (friend == null)
            {
                if (showDebugLogs)
                    Debug.LogWarning($"[FriendsManager] Friend not found: {friendId}");
                return;
            }

            if (!friend.isOnline)
            {
                if (showDebugLogs)
                    Debug.LogWarning($"[FriendsManager] Friend is offline: {friend.playerName}");
                return;
            }

            // TODO: Send squad invite through networking
            if (showDebugLogs)
                Debug.Log($"[FriendsManager] Squad invite sent to {friend.playerName}");

            // Track analytics
            Analytics.AnalyticsManager.Instance?.TrackEvent("squad_invite_sent", new Dictionary<string, object>
            {
                { "friend_player_id", friendId }
            });
        }

        #endregion

        #region Save/Load

        private void LoadFriendsData()
        {
            if (useLocalStorage)
            {
                // Load from PlayerPrefs
                string friendsJson = PlayerPrefs.GetString("FriendsData", "");
                if (!string.IsNullOrEmpty(friendsJson))
                {
                    try
                    {
                        var data = JsonUtility.FromJson<FriendsDataContainer>(friendsJson);
                        friends = data.friends ?? new List<FriendData>();
                        pendingRequests = data.pendingRequests ?? new List<FriendRequest>();
                        blockedPlayers = data.blockedPlayers ?? new List<string>();
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[FriendsManager] Failed to load friends data: {e.Message}");
                        friends = new List<FriendData>();
                        pendingRequests = new List<FriendRequest>();
                        blockedPlayers = new List<string>();
                    }
                }

                if (showDebugLogs)
                    Debug.Log($"[FriendsManager] Loaded {friends.Count} friends, {pendingRequests.Count} pending requests");
            }
            else
            {
                // TODO: Load from backend
                LoadFriendsFromBackend();
            }
        }

        private void SaveFriendsData()
        {
            if (useLocalStorage)
            {
                var data = new FriendsDataContainer
                {
                    friends = friends,
                    pendingRequests = pendingRequests,
                    blockedPlayers = blockedPlayers
                };

                string json = JsonUtility.ToJson(data);
                PlayerPrefs.SetString("FriendsData", json);
                PlayerPrefs.Save();
            }
            else
            {
                // TODO: Save to backend
                SaveFriendsToBackend();
            }
        }

        #endregion

        #region Backend Integration (Placeholder)

        private void SendFriendRequestToBackend(string playerId)
        {
            // TODO: Implement backend call
            if (showDebugLogs)
                Debug.Log($"[FriendsManager] Sending friend request to backend for player: {playerId}");
        }

        private void LoadFriendsFromBackend()
        {
            // TODO: Implement backend call
            if (showDebugLogs)
                Debug.Log("[FriendsManager] Loading friends from backend...");
        }

        private void SaveFriendsToBackend()
        {
            // TODO: Implement backend call
            if (showDebugLogs)
                Debug.Log("[FriendsManager] Saving friends to backend...");
        }

        private void FetchOnlineStatusFromBackend()
        {
            // TODO: Implement backend call
        }

        #endregion

        #region Properties

        public int FriendCount => friends.Count;
        public int OnlineFriendCount => friends.Count(f => f.isOnline);
        public int PendingRequestCount => pendingRequests.Count;
        public int BlockedPlayerCount => blockedPlayers.Count;

        #endregion
    }

    #region Data Structures

    [System.Serializable]
    public class FriendData
    {
        public string playerId;
        public string playerName;
        public int level;
        public int prestigeLevel;
        public string avatarUrl;
        public bool isOnline;
        public System.DateTime lastOnline;
        public System.DateTime friendsSince;
        public PlayerStatus status;
    }

    [System.Serializable]
    public class FriendRequest
    {
        public string senderId;
        public string senderName;
        public int senderLevel;
        public System.DateTime requestDate;
    }

    [System.Serializable]
    public class FriendsDataContainer
    {
        public List<FriendData> friends;
        public List<FriendRequest> pendingRequests;
        public List<string> blockedPlayers;
    }

    public enum PlayerStatus
    {
        Online,
        InMatch,
        InLobby,
        Away,
        Offline
    }

    #endregion
}
