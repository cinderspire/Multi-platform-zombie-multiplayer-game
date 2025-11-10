using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Social
{
    public class FriendSystem : NetworkBehaviour
    {
        public static FriendSystem Instance { get; private set; }

        private Dictionary<ulong, List<ulong>> friendLists = new Dictionary<ulong, List<ulong>>();
        private Dictionary<ulong, List<FriendRequest>> pendingRequests = new Dictionary<ulong, List<FriendRequest>>();

        public event Action<ulong, ulong> OnFriendAdded;
        public event Action<ulong, ulong> OnFriendRemoved;
        public event Action<ulong, FriendRequest> OnFriendRequestReceived;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        [ServerRpc(RequireOwnership = false)]
        public void SendFriendRequestServerRpc(ulong senderId, ulong recipientId, ServerRpcParams rpcParams = default)
        {
            if (senderId == recipientId) return;
            if (AreFriends(senderId, recipientId)) return;

            if (!pendingRequests.ContainsKey(recipientId))
            {
                pendingRequests[recipientId] = new List<FriendRequest>();
            }

            // Check if already has pending request
            if (pendingRequests[recipientId].Any(r => r.senderId == senderId)) return;

            FriendRequest request = new FriendRequest
            {
                senderId = senderId,
                recipientId = recipientId,
                timestamp = DateTime.UtcNow
            };

            pendingRequests[recipientId].Add(request);
            OnFriendRequestReceived?.Invoke(recipientId, request);
            NotifyFriendRequestClientRpc(recipientId, senderId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void AcceptFriendRequestServerRpc(ulong recipientId, ulong senderId, ServerRpcParams rpcParams = default)
        {
            if (!pendingRequests.ContainsKey(recipientId)) return;

            var request = pendingRequests[recipientId].FirstOrDefault(r => r.senderId == senderId);
            if (request == null) return;

            // Add to friend lists
            if (!friendLists.ContainsKey(recipientId))
            {
                friendLists[recipientId] = new List<ulong>();
            }
            if (!friendLists.ContainsKey(senderId))
            {
                friendLists[senderId] = new List<ulong>();
            }

            friendLists[recipientId].Add(senderId);
            friendLists[senderId].Add(recipientId);

            // Remove request
            pendingRequests[recipientId].Remove(request);

            OnFriendAdded?.Invoke(recipientId, senderId);
            OnFriendAdded?.Invoke(senderId, recipientId);

            NotifyFriendAddedClientRpc(recipientId, senderId);
            NotifyFriendAddedClientRpc(senderId, recipientId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void DeclineFriendRequestServerRpc(ulong recipientId, ulong senderId, ServerRpcParams rpcParams = default)
        {
            if (!pendingRequests.ContainsKey(recipientId)) return;

            var request = pendingRequests[recipientId].FirstOrDefault(r => r.senderId == senderId);
            if (request != null)
            {
                pendingRequests[recipientId].Remove(request);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void RemoveFriendServerRpc(ulong playerId, ulong friendId, ServerRpcParams rpcParams = default)
        {
            if (friendLists.TryGetValue(playerId, out var list1))
            {
                list1.Remove(friendId);
            }

            if (friendLists.TryGetValue(friendId, out var list2))
            {
                list2.Remove(playerId);
            }

            OnFriendRemoved?.Invoke(playerId, friendId);
            OnFriendRemoved?.Invoke(friendId, playerId);

            NotifyFriendRemovedClientRpc(playerId, friendId);
            NotifyFriendRemovedClientRpc(friendId, playerId);
        }

        [ClientRpc]
        private void NotifyFriendRequestClientRpc(ulong recipientId, ulong senderId) { }

        [ClientRpc]
        private void NotifyFriendAddedClientRpc(ulong playerId, ulong friendId) { }

        [ClientRpc]
        private void NotifyFriendRemovedClientRpc(ulong playerId, ulong friendId) { }

        public bool AreFriends(ulong player1, ulong player2)
        {
            return friendLists.TryGetValue(player1, out var list) && list.Contains(player2);
        }

        public List<ulong> GetFriendList(ulong playerId)
        {
            return friendLists.GetValueOrDefault(playerId, new List<ulong>());
        }

        public List<FriendRequest> GetPendingRequests(ulong playerId)
        {
            return pendingRequests.GetValueOrDefault(playerId, new List<FriendRequest>());
        }
    }

    [Serializable]
    public class FriendRequest
    {
        public ulong senderId;
        public ulong recipientId;
        public DateTime timestamp;
    }
}
