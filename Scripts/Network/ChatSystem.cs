using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Network
{
    public class ChatSystem : NetworkBehaviour
    {
        public static ChatSystem Instance { get; private set; }

        [SerializeField] private int maxMessageHistory = 100;

        private List<ChatMessage> messageHistory = new List<ChatMessage>();

        public event Action<ChatMessage> OnMessageReceived;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        [ServerRpc(RequireOwnership = false)]
        public void SendMessageServerRpc(ulong senderId, string message, ChatChannel channel, ServerRpcParams rpcParams = default)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            if (Admin.AdminSystem.Instance?.IsPlayerMuted(senderId) == true) return;

            ChatMessage chatMessage = new ChatMessage
            {
                senderId = senderId,
                senderName = $"Player{senderId}",
                message = message,
                channel = channel,
                timestamp = DateTime.UtcNow
            };

            messageHistory.Add(chatMessage);
            if (messageHistory.Count > maxMessageHistory)
            {
                messageHistory.RemoveAt(0);
            }

            BroadcastMessageClientRpc(chatMessage);
        }

        [ClientRpc]
        private void BroadcastMessageClientRpc(ChatMessage message)
        {
            OnMessageReceived?.Invoke(message);
        }

        public List<ChatMessage> GetMessageHistory() => new List<ChatMessage>(messageHistory);
    }

    [Serializable]
    public struct ChatMessage : INetworkSerializable
    {
        public ulong senderId;
        public string senderName;
        public string message;
        public ChatChannel channel;
        public DateTime timestamp;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref senderId);
            serializer.SerializeValue(ref senderName);
            serializer.SerializeValue(ref message);
            serializer.SerializeValue(ref channel);
        }
    }

    public enum ChatChannel { Global, Team, Party, Whisper, System }
}
