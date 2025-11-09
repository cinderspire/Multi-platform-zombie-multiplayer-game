using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace DeadFrontier.Social
{
    /// <summary>
    /// Comprehensive friends and social features system.
    /// Supports friend requests, online status, parties, messaging, and social presence.
    /// Provides robust social connectivity and cooperative gameplay features.
    /// </summary>
    public class FriendSystem : MonoBehaviour
    {
        public static FriendSystem Instance { get; private set; }

        [Header("Friend Settings")]
        [SerializeField] private int maxFriends = 200;
        [SerializeField] private int maxRecentPlayers = 50;
        [SerializeField] private float friendRequestExpirationDays = 30f;

        [Header("Party Settings")]
        [SerializeField] private int maxPartySize = 4;
        [SerializeField] private float partyInviteExpirationSeconds = 60f;

        [Header("Messaging")]
        [SerializeField] private int maxMessageLength = 500;
        [SerializeField] private int maxMessageHistory = 100;

        // Friends data
        private Dictionary<ulong, List<Friend>> playerFriends = new Dictionary<ulong, List<Friend>>();
        private Dictionary<ulong, List<FriendRequest>> pendingRequests = new Dictionary<ulong, List<FriendRequest>>();
        private Dictionary<ulong, List<ulong>> blockedPlayers = new Dictionary<ulong, List<ulong>>();
        private Dictionary<ulong, List<ulong>> recentPlayers = new Dictionary<ulong, List<ulong>>();

        // Online presence
        private Dictionary<ulong, PlayerPresence> playerPresence = new Dictionary<ulong, PlayerPresence>();

        // Parties
        private Dictionary<string, Party> activeParties = new Dictionary<string, Party>();
        private Dictionary<ulong, string> playerPartyMembership = new Dictionary<ulong, string>();
        private Dictionary<ulong, List<PartyInvite>> pendingPartyInvites = new Dictionary<ulong, List<PartyInvite>>();

        // Messaging
        private Dictionary<string, List<DirectMessage>> directMessages = new Dictionary<string, List<DirectMessage>>();

        // Events
        public event Action<ulong, ulong> OnFriendRequestSent;
        public event Action<ulong, ulong> OnFriendRequestAccepted;
        public event Action<ulong, ulong> OnFriendRequestDeclined;
        public event Action<ulong, ulong> OnFriendAdded;
        public event Action<ulong, ulong> OnFriendRemoved;
        public event Action<ulong, PlayerPresence> OnPresenceChanged;
        public event Action<Party> OnPartyCreated;
        public event Action<Party> OnPartyDisbanded;
        public event Action<string, ulong> OnPlayerJoinedParty;
        public event Action<string, ulong> OnPlayerLeftParty;
        public event Action<ulong, ulong, string> OnDirectMessageReceived;

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
            LoadAllSocialData();
        }

        private void Update()
        {
            UpdatePresence();
            CleanupExpiredRequests();
        }

        #region Friend Requests

        public bool CanSendFriendRequest(ulong senderId, ulong receiverId)
        {
            // Can't friend yourself
            if (senderId == receiverId) return false;

            // Check if already friends
            if (AreFriends(senderId, receiverId)) return false;

            // Check if blocked
            if (IsBlocked(receiverId, senderId)) return false;

            // Check if request already pending
            if (HasPendingRequest(senderId, receiverId)) return false;

            // Check friend limit
            if (GetFriendCount(senderId) >= maxFriends) return false;

            return true;
        }

        public bool SendFriendRequest(ulong senderId, ulong receiverId)
        {
            if (!CanSendFriendRequest(senderId, receiverId)) return false;

            var request = new FriendRequest
            {
                senderId = senderId,
                receiverId = receiverId,
                sentTime = DateTime.UtcNow,
                expirationTime = DateTime.UtcNow.AddDays(friendRequestExpirationDays)
            };

            if (!pendingRequests.ContainsKey(receiverId))
            {
                pendingRequests[receiverId] = new List<FriendRequest>();
            }

            pendingRequests[receiverId].Add(request);

            SavePlayerSocialData(receiverId);

            OnFriendRequestSent?.Invoke(senderId, receiverId);

            Debug.Log($"[FriendSystem] Friend request sent from {senderId} to {receiverId}");

            return true;
        }

        public bool AcceptFriendRequest(ulong accepterId, ulong requesterId)
        {
            if (!pendingRequests.ContainsKey(accepterId)) return false;

            var request = pendingRequests[accepterId].FirstOrDefault(r => r.senderId == requesterId);
            if (request == null) return false;

            // Check expiration
            if (DateTime.UtcNow > request.expirationTime)
            {
                pendingRequests[accepterId].Remove(request);
                return false;
            }

            // Add as friends (both directions)
            AddFriend(accepterId, requesterId);
            AddFriend(requesterId, accepterId);

            // Remove request
            pendingRequests[accepterId].Remove(request);

            SavePlayerSocialData(accepterId);
            SavePlayerSocialData(requesterId);

            OnFriendRequestAccepted?.Invoke(accepterId, requesterId);
            OnFriendAdded?.Invoke(accepterId, requesterId);

            Debug.Log($"[FriendSystem] Friend request accepted: {requesterId} and {accepterId} are now friends");

            return true;
        }

        public bool DeclineFriendRequest(ulong declinerId, ulong requesterId)
        {
            if (!pendingRequests.ContainsKey(declinerId)) return false;

            var request = pendingRequests[declinerId].FirstOrDefault(r => r.senderId == requesterId);
            if (request == null) return false;

            pendingRequests[declinerId].Remove(request);

            SavePlayerSocialData(declinerId);

            OnFriendRequestDeclined?.Invoke(declinerId, requesterId);

            Debug.Log($"[FriendSystem] Friend request declined by {declinerId}");

            return true;
        }

        private bool HasPendingRequest(ulong senderId, ulong receiverId)
        {
            if (!pendingRequests.ContainsKey(receiverId)) return false;

            return pendingRequests[receiverId].Any(r => r.senderId == senderId);
        }

        private void CleanupExpiredRequests()
        {
            foreach (var kvp in pendingRequests.ToList())
            {
                var playerId = kvp.Key;
                var requests = kvp.Value;

                requests.RemoveAll(r => DateTime.UtcNow > r.expirationTime);
            }
        }

        #endregion

        #region Friend Management

        private void AddFriend(ulong playerId, ulong friendId)
        {
            if (!playerFriends.ContainsKey(playerId))
            {
                playerFriends[playerId] = new List<Friend>();
            }

            var friend = new Friend
            {
                playerId = friendId,
                friendSince = DateTime.UtcNow,
                nickname = null,
                isFavorite = false
            };

            playerFriends[playerId].Add(friend);
        }

        public bool RemoveFriend(ulong playerId, ulong friendId)
        {
            if (!playerFriends.ContainsKey(playerId)) return false;

            var friend = playerFriends[playerId].FirstOrDefault(f => f.playerId == friendId);
            if (friend == null) return false;

            playerFriends[playerId].Remove(friend);

            // Remove from other side too
            if (playerFriends.ContainsKey(friendId))
            {
                var otherFriend = playerFriends[friendId].FirstOrDefault(f => f.playerId == playerId);
                if (otherFriend != null)
                {
                    playerFriends[friendId].Remove(otherFriend);
                }
            }

            SavePlayerSocialData(playerId);
            SavePlayerSocialData(friendId);

            OnFriendRemoved?.Invoke(playerId, friendId);

            Debug.Log($"[FriendSystem] {playerId} removed {friendId} from friends");

            return true;
        }

        public bool SetFriendNickname(ulong playerId, ulong friendId, string nickname)
        {
            if (!playerFriends.ContainsKey(playerId)) return false;

            var friend = playerFriends[playerId].FirstOrDefault(f => f.playerId == friendId);
            if (friend == null) return false;

            friend.nickname = nickname;

            SavePlayerSocialData(playerId);

            return true;
        }

        public bool SetFriendFavorite(ulong playerId, ulong friendId, bool isFavorite)
        {
            if (!playerFriends.ContainsKey(playerId)) return false;

            var friend = playerFriends[playerId].FirstOrDefault(f => f.playerId == friendId);
            if (friend == null) return false;

            friend.isFavorite = isFavorite;

            SavePlayerSocialData(playerId);

            return true;
        }

        public bool AreFriends(ulong player1, ulong player2)
        {
            if (!playerFriends.ContainsKey(player1)) return false;

            return playerFriends[player1].Any(f => f.playerId == player2);
        }

        public int GetFriendCount(ulong playerId)
        {
            return playerFriends.ContainsKey(playerId) ? playerFriends[playerId].Count : 0;
        }

        #endregion

        #region Block/Mute

        public bool BlockPlayer(ulong blockerId, ulong targetId)
        {
            if (blockerId == targetId) return false;

            if (!blockedPlayers.ContainsKey(blockerId))
            {
                blockedPlayers[blockerId] = new List<ulong>();
            }

            if (blockedPlayers[blockerId].Contains(targetId)) return false;

            blockedPlayers[blockerId].Add(targetId);

            // Remove as friend if they were friends
            if (AreFriends(blockerId, targetId))
            {
                RemoveFriend(blockerId, targetId);
            }

            SavePlayerSocialData(blockerId);

            Debug.Log($"[FriendSystem] {blockerId} blocked {targetId}");

            return true;
        }

        public bool UnblockPlayer(ulong blockerId, ulong targetId)
        {
            if (!blockedPlayers.ContainsKey(blockerId)) return false;

            bool removed = blockedPlayers[blockerId].Remove(targetId);

            if (removed)
            {
                SavePlayerSocialData(blockerId);
            }

            return removed;
        }

        public bool IsBlocked(ulong playerId, ulong targetId)
        {
            if (!blockedPlayers.ContainsKey(playerId)) return false;

            return blockedPlayers[playerId].Contains(targetId);
        }

        #endregion

        #region Presence System

        public void UpdatePresence(ulong playerId, PlayerStatus status, string details = "")
        {
            if (!playerPresence.ContainsKey(playerId))
            {
                playerPresence[playerId] = new PlayerPresence();
            }

            var presence = playerPresence[playerId];
            presence.playerId = playerId;
            presence.status = status;
            presence.statusDetails = details;
            presence.lastUpdate = DateTime.UtcNow;

            OnPresenceChanged?.Invoke(playerId, presence);

            // Notify friends
            NotifyFriendsOfPresenceChange(playerId, presence);
        }

        private void NotifyFriendsOfPresenceChange(ulong playerId, PlayerPresence presence)
        {
            // Find all players who have this player as a friend
            foreach (var kvp in playerFriends)
            {
                if (kvp.Value.Any(f => f.playerId == playerId))
                {
                    // This player has playerId as a friend, notify them
                    // In production, this would send network message
                }
            }
        }

        public PlayerPresence GetPresence(ulong playerId)
        {
            return playerPresence.ContainsKey(playerId) ? playerPresence[playerId] : null;
        }

        #endregion

        #region Recent Players

        public void AddRecentPlayer(ulong playerId, ulong recentPlayerId)
        {
            if (playerId == recentPlayerId) return;

            if (!recentPlayers.ContainsKey(playerId))
            {
                recentPlayers[playerId] = new List<ulong>();
            }

            var recent = recentPlayers[playerId];

            // Remove if already exists (will re-add at front)
            recent.Remove(recentPlayerId);

            // Add to front
            recent.Insert(0, recentPlayerId);

            // Limit size
            if (recent.Count > maxRecentPlayers)
            {
                recent.RemoveAt(recent.Count - 1);
            }

            SavePlayerSocialData(playerId);
        }

        public List<ulong> GetRecentPlayers(ulong playerId)
        {
            return recentPlayers.ContainsKey(playerId)
                ? new List<ulong>(recentPlayers[playerId])
                : new List<ulong>();
        }

        #endregion

        #region Party System

        public Party CreateParty(ulong leaderId)
        {
            // Check if already in a party
            if (playerPartyMembership.ContainsKey(leaderId)) return null;

            string partyId = $"party_{DateTime.UtcNow.Ticks}";

            var party = new Party
            {
                partyId = partyId,
                leaderId = leaderId,
                members = new List<ulong> { leaderId },
                creationTime = DateTime.UtcNow
            };

            activeParties[partyId] = party;
            playerPartyMembership[leaderId] = partyId;

            OnPartyCreated?.Invoke(party);

            Debug.Log($"[FriendSystem] Party created by {leaderId}");

            return party;
        }

        public bool InviteToParty(string partyId, ulong inviterId, ulong inviteeId)
        {
            if (!activeParties.ContainsKey(partyId)) return false;

            var party = activeParties[partyId];

            // Only leader or members can invite (if enabled)
            if (!party.members.Contains(inviterId)) return false;

            // Check if invitee is already in a party
            if (playerPartyMembership.ContainsKey(inviteeId)) return false;

            // Check party size
            if (party.members.Count >= maxPartySize) return false;

            // Check if blocked
            if (IsBlocked(inviteeId, inviterId)) return false;

            var invite = new PartyInvite
            {
                partyId = partyId,
                inviterId = inviterId,
                inviteeId = inviteeId,
                inviteTime = DateTime.UtcNow,
                expirationTime = DateTime.UtcNow.AddSeconds(partyInviteExpirationSeconds)
            };

            if (!pendingPartyInvites.ContainsKey(inviteeId))
            {
                pendingPartyInvites[inviteeId] = new List<PartyInvite>();
            }

            pendingPartyInvites[inviteeId].Add(invite);

            Debug.Log($"[FriendSystem] Party invite sent to {inviteeId}");

            return true;
        }

        public bool AcceptPartyInvite(ulong playerId, string partyId)
        {
            if (!pendingPartyInvites.ContainsKey(playerId)) return false;

            var invite = pendingPartyInvites[playerId].FirstOrDefault(i => i.partyId == partyId);
            if (invite == null) return false;

            // Check expiration
            if (DateTime.UtcNow > invite.expirationTime)
            {
                pendingPartyInvites[playerId].Remove(invite);
                return false;
            }

            // Join party
            if (JoinParty(playerId, partyId))
            {
                pendingPartyInvites[playerId].Remove(invite);
                return true;
            }

            return false;
        }

        private bool JoinParty(ulong playerId, string partyId)
        {
            if (!activeParties.ContainsKey(partyId)) return false;
            if (playerPartyMembership.ContainsKey(playerId)) return false;

            var party = activeParties[partyId];

            if (party.members.Count >= maxPartySize) return false;

            party.members.Add(playerId);
            playerPartyMembership[playerId] = partyId;

            OnPlayerJoinedParty?.Invoke(partyId, playerId);

            Debug.Log($"[FriendSystem] Player {playerId} joined party {partyId}");

            return true;
        }

        public bool LeaveParty(ulong playerId)
        {
            if (!playerPartyMembership.ContainsKey(playerId)) return false;

            string partyId = playerPartyMembership[playerId];
            var party = activeParties[partyId];

            party.members.Remove(playerId);
            playerPartyMembership.Remove(playerId);

            OnPlayerLeftParty?.Invoke(partyId, playerId);

            // If leader left, transfer or disband
            if (party.leaderId == playerId)
            {
                if (party.members.Count > 0)
                {
                    // Transfer to next member
                    party.leaderId = party.members[0];
                }
                else
                {
                    // Disband party
                    DisbandParty(partyId);
                }
            }

            Debug.Log($"[FriendSystem] Player {playerId} left party {partyId}");

            return true;
        }

        public bool KickFromParty(string partyId, ulong kickerId, ulong targetId)
        {
            if (!activeParties.ContainsKey(partyId)) return false;

            var party = activeParties[partyId];

            // Only leader can kick
            if (party.leaderId != kickerId) return false;

            // Can't kick self
            if (targetId == kickerId) return false;

            if (!party.members.Contains(targetId)) return false;

            party.members.Remove(targetId);
            playerPartyMembership.Remove(targetId);

            OnPlayerLeftParty?.Invoke(partyId, targetId);

            Debug.Log($"[FriendSystem] Player {targetId} kicked from party {partyId}");

            return true;
        }

        private void DisbandParty(string partyId)
        {
            if (!activeParties.ContainsKey(partyId)) return;

            var party = activeParties[partyId];

            // Remove all members
            foreach (var memberId in party.members.ToList())
            {
                playerPartyMembership.Remove(memberId);
            }

            activeParties.Remove(partyId);

            OnPartyDisbanded?.Invoke(party);

            Debug.Log($"[FriendSystem] Party {partyId} disbanded");
        }

        #endregion

        #region Direct Messaging

        public bool SendDirectMessage(ulong senderId, ulong receiverId, string message)
        {
            // Check if blocked
            if (IsBlocked(receiverId, senderId)) return false;

            // Validate message
            if (string.IsNullOrEmpty(message) || message.Length > maxMessageLength) return false;

            // Create conversation ID (consistent regardless of sender/receiver order)
            string conversationId = GetConversationId(senderId, receiverId);

            if (!directMessages.ContainsKey(conversationId))
            {
                directMessages[conversationId] = new List<DirectMessage>();
            }

            var dm = new DirectMessage
            {
                senderId = senderId,
                receiverId = receiverId,
                message = message,
                timestamp = DateTime.UtcNow,
                isRead = false
            };

            directMessages[conversationId].Add(dm);

            // Limit history
            if (directMessages[conversationId].Count > maxMessageHistory)
            {
                directMessages[conversationId].RemoveAt(0);
            }

            OnDirectMessageReceived?.Invoke(senderId, receiverId, message);

            Debug.Log($"[FriendSystem] Message sent from {senderId} to {receiverId}");

            return true;
        }

        public List<DirectMessage> GetConversation(ulong player1, ulong player2)
        {
            string conversationId = GetConversationId(player1, player2);

            return directMessages.ContainsKey(conversationId)
                ? new List<DirectMessage>(directMessages[conversationId])
                : new List<DirectMessage>();
        }

        public void MarkMessagesAsRead(ulong readerId, ulong otherId)
        {
            string conversationId = GetConversationId(readerId, otherId);

            if (!directMessages.ContainsKey(conversationId)) return;

            foreach (var msg in directMessages[conversationId])
            {
                if (msg.receiverId == readerId)
                {
                    msg.isRead = true;
                }
            }
        }

        private string GetConversationId(ulong player1, ulong player2)
        {
            // Sort to ensure consistent ID
            ulong low = Math.Min(player1, player2);
            ulong high = Math.Max(player1, player2);

            return $"{low}_{high}";
        }

        #endregion

        #region Persistence

        private void LoadAllSocialData()
        {
            // In production, load from database
            Debug.Log("[FriendSystem] Friend system initialized");
        }

        private void SavePlayerSocialData(ulong playerId)
        {
            // In production, save to database
        }

        #endregion

        #region Public Getters

        public List<Friend> GetFriends(ulong playerId)
        {
            return playerFriends.ContainsKey(playerId)
                ? new List<Friend>(playerFriends[playerId])
                : new List<Friend>();
        }

        public List<Friend> GetOnlineFriends(ulong playerId)
        {
            var friends = GetFriends(playerId);

            return friends.Where(f =>
            {
                var presence = GetPresence(f.playerId);
                return presence != null && presence.status != PlayerStatus.Offline;
            }).ToList();
        }

        public List<FriendRequest> GetPendingFriendRequests(ulong playerId)
        {
            return pendingRequests.ContainsKey(playerId)
                ? new List<FriendRequest>(pendingRequests[playerId])
                : new List<FriendRequest>();
        }

        public List<ulong> GetBlockedPlayers(ulong playerId)
        {
            return blockedPlayers.ContainsKey(playerId)
                ? new List<ulong>(blockedPlayers[playerId])
                : new List<ulong>();
        }

        public Party GetPlayerParty(ulong playerId)
        {
            if (!playerPartyMembership.ContainsKey(playerId)) return null;

            string partyId = playerPartyMembership[playerId];
            return activeParties.ContainsKey(partyId) ? activeParties[partyId] : null;
        }

        public List<PartyInvite> GetPendingPartyInvites(ulong playerId)
        {
            return pendingPartyInvites.ContainsKey(playerId)
                ? new List<PartyInvite>(pendingPartyInvites[playerId])
                : new List<PartyInvite>();
        }

        public bool IsInParty(ulong playerId) => playerPartyMembership.ContainsKey(playerId);

        #endregion
    }

    #region Data Classes

    public class Friend
    {
        public ulong playerId;
        public DateTime friendSince;
        public string nickname;
        public bool isFavorite;
    }

    public class FriendRequest
    {
        public ulong senderId;
        public ulong receiverId;
        public DateTime sentTime;
        public DateTime expirationTime;
    }

    public class PlayerPresence
    {
        public ulong playerId;
        public PlayerStatus status;
        public string statusDetails;
        public DateTime lastUpdate;
    }

    public class Party
    {
        public string partyId;
        public ulong leaderId;
        public List<ulong> members;
        public DateTime creationTime;
    }

    public class PartyInvite
    {
        public string partyId;
        public ulong inviterId;
        public ulong inviteeId;
        public DateTime inviteTime;
        public DateTime expirationTime;
    }

    public class DirectMessage
    {
        public ulong senderId;
        public ulong receiverId;
        public string message;
        public DateTime timestamp;
        public bool isRead;
    }

    public enum PlayerStatus
    {
        Offline,
        Online,
        InMenu,
        InLobby,
        InMatch,
        Away
    }

    #endregion
}
