using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace DeadFrontier.Core.Communication
{
    /// <summary>
    /// Manages player communication including ping system and voice chat integration
    /// Enables team coordination without requiring voice chat
    /// </summary>
    public class CommunicationSystem : NetworkBehaviour
    {
        [Header("Ping System")]
        [SerializeField] private GameObject pingMarkerPrefab;
        [SerializeField] private float pingDuration = 5f;
        [SerializeField] private float pingCooldown = 0.5f;
        [SerializeField] private float pingMaxDistance = 100f;

        [Header("Quick Messages")]
        [SerializeField] private List<QuickMessage> quickMessages = new List<QuickMessage>();

        [Header("Voice Chat")]
        [SerializeField] private bool enableVoiceChat = true;
        [SerializeField] private KeyCode pushToTalkKey = KeyCode.V;
        [SerializeField] private bool voiceActivation = false;
        [SerializeField] private float voiceActivationThreshold = 0.05f;

        [Header("Audio")]
        [SerializeField] private AudioClip pingSound;
        [SerializeField] private AudioClip messageSound;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        // Ping state
        private float lastPingTime = 0f;
        private List<PingMarker> activePings = new List<PingMarker>();

        // Voice chat state
        private bool isTalking = false;
        private AudioSource voiceSource;

        // Events
        public event System.Action<PingData> OnPingCreated;
        public event System.Action<ulong, string> OnQuickMessageSent;
        public event System.Action<ulong, bool> OnVoiceChatStatusChanged;

        private void Awake()
        {
            InitializeQuickMessages();
        }

        private void Update()
        {
            if (!IsOwner) return;

            HandlePingInput();
            HandleVoiceChatInput();
            UpdateActivePings();
        }

        #region Initialization

        private void InitializeQuickMessages()
        {
            if (quickMessages.Count == 0)
            {
                // Add default quick messages
                quickMessages.Add(new QuickMessage { id = "enemy_here", text = "Enemy here!", icon = null, color = Color.red });
                quickMessages.Add(new QuickMessage { id = "follow_me", text = "Follow me!", icon = null, color = Color.green });
                quickMessages.Add(new QuickMessage { id = "defend", text = "Defend this area!", icon = null, color = Color.yellow });
                quickMessages.Add(new QuickMessage { id = "loot", text = "Loot here!", icon = null, color = Color.cyan });
                quickMessages.Add(new QuickMessage { id = "extract", text = "Head to extraction!", icon = null, color = Color.magenta });
                quickMessages.Add(new QuickMessage { id = "help", text = "I need help!", icon = null, color = Color.red });
                quickMessages.Add(new QuickMessage { id = "thanks", text = "Thanks!", icon = null, color = Color.green });
                quickMessages.Add(new QuickMessage { id = "sorry", text = "Sorry!", icon = null, color = Color.yellow });
            }
        }

        #endregion

        #region Ping System

        private void HandlePingInput()
        {
            // Middle mouse or context menu key
            if (Input.GetKeyDown(KeyCode.Mouse2) || Input.GetKeyDown(KeyCode.Z))
            {
                CreatePing();
            }
        }

        /// <summary>
        /// Creates a ping at the crosshair position
        /// </summary>
        public void CreatePing()
        {
            if (Time.time - lastPingTime < pingCooldown)
            {
                if (showDebugLogs)
                    Debug.LogWarning("[CommunicationSystem] Ping on cooldown");
                return;
            }

            Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
            if (Physics.Raycast(ray, out RaycastHit hit, pingMaxDistance))
            {
                Vector3 pingPosition = hit.point;
                CreatePingAtPosition(pingPosition, PingType.Generic);
                lastPingTime = Time.time;
            }
        }

        /// <summary>
        /// Creates a ping at a specific position
        /// </summary>
        public void CreatePingAtPosition(Vector3 position, PingType type)
        {
            if (!IsOwner) return;

            PingData pingData = new PingData
            {
                senderId = NetworkManager.Singleton.LocalClientId,
                senderName = GetPlayerName(),
                position = position,
                type = type,
                timestamp = Time.time
            };

            CreatePingServerRpc(pingData);

            if (showDebugLogs)
                Debug.Log($"[CommunicationSystem] Created {type} ping at {position}");
        }

        [ServerRpc(RequireOwnership = false)]
        private void CreatePingServerRpc(PingData pingData, ServerRpcParams rpcParams = default)
        {
            // Broadcast to all clients
            DisplayPingClientRpc(pingData);
        }

        [ClientRpc]
        private void DisplayPingClientRpc(PingData pingData)
        {
            // Create visual marker
            if (pingMarkerPrefab != null)
            {
                GameObject markerObj = Instantiate(pingMarkerPrefab, pingData.position, Quaternion.identity);
                PingMarker marker = markerObj.GetComponent<PingMarker>();

                if (marker == null)
                {
                    marker = markerObj.AddComponent<PingMarker>();
                }

                marker.Initialize(pingData, pingDuration);
                activePings.Add(marker);
            }

            // Play sound
            if (pingSound != null && Core.AudioManager.Instance != null)
            {
                Core.AudioManager.Instance.PlaySFX(pingSound);
            }

            // Show notification
            if (UI.NotificationManager.Instance != null && pingData.senderId != NetworkManager.Singleton.LocalClientId)
            {
                string message = $"{pingData.senderName} pinged {pingData.type}";
                UI.NotificationManager.Instance.ShowNotification("Ping", message, GetPingColor(pingData.type), UI.NotificationType.Info);
            }

            OnPingCreated?.Invoke(pingData);

            if (showDebugLogs)
                Debug.Log($"[CommunicationSystem] Displayed ping from {pingData.senderName}");
        }

        private void UpdateActivePings()
        {
            // Remove expired pings
            activePings.RemoveAll(p => p == null);
        }

        private Color GetPingColor(PingType type)
        {
            switch (type)
            {
                case PingType.Enemy:
                    return Color.red;
                case PingType.Loot:
                    return Color.cyan;
                case PingType.Defend:
                    return Color.yellow;
                case PingType.Attack:
                    return new Color(1f, 0.5f, 0f); // Orange
                case PingType.Extraction:
                    return Color.magenta;
                default:
                    return Color.white;
            }
        }

        #endregion

        #region Quick Messages

        /// <summary>
        /// Sends a quick message to team
        /// </summary>
        public void SendQuickMessage(string messageId)
        {
            var message = quickMessages.Find(m => m.id == messageId);
            if (message == null)
            {
                if (showDebugLogs)
                    Debug.LogWarning($"[CommunicationSystem] Quick message not found: {messageId}");
                return;
            }

            SendQuickMessageServerRpc(messageId, GetPlayerName());
        }

        [ServerRpc(RequireOwnership = false)]
        private void SendQuickMessageServerRpc(string messageId, string senderName, ServerRpcParams rpcParams = default)
        {
            ulong senderId = rpcParams.Receive.SenderClientId;
            DisplayQuickMessageClientRpc(senderId, messageId, senderName);
        }

        [ClientRpc]
        private void DisplayQuickMessageClientRpc(ulong senderId, string messageId, string senderName)
        {
            var message = quickMessages.Find(m => m.id == messageId);
            if (message == null) return;

            // Play sound
            if (messageSound != null && Core.AudioManager.Instance != null)
            {
                Core.AudioManager.Instance.PlaySFX(messageSound);
            }

            // Show notification
            if (UI.NotificationManager.Instance != null && senderId != NetworkManager.Singleton.LocalClientId)
            {
                UI.NotificationManager.Instance.ShowNotification(senderName, message.text, message.color, UI.NotificationType.Info);
            }

            OnQuickMessageSent?.Invoke(senderId, message.text);

            if (showDebugLogs)
                Debug.Log($"[CommunicationSystem] {senderName}: {message.text}");
        }

        public List<QuickMessage> GetQuickMessages()
        {
            return new List<QuickMessage>(quickMessages);
        }

        #endregion

        #region Voice Chat

        private void HandleVoiceChatInput()
        {
            if (!enableVoiceChat) return;

            bool shouldTalk = false;

            if (voiceActivation)
            {
                // Voice activation based on microphone volume
                shouldTalk = GetMicrophoneVolume() > voiceActivationThreshold;
            }
            else
            {
                // Push to talk
                shouldTalk = Input.GetKey(pushToTalkKey);
            }

            if (shouldTalk != isTalking)
            {
                isTalking = shouldTalk;
                SetVoiceChatActiveServerRpc(isTalking);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void SetVoiceChatActiveServerRpc(bool active, ServerRpcParams rpcParams = default)
        {
            ulong senderId = rpcParams.Receive.SenderClientId;
            BroadcastVoiceChatStatusClientRpc(senderId, active);
        }

        [ClientRpc]
        private void BroadcastVoiceChatStatusClientRpc(ulong playerId, bool active)
        {
            OnVoiceChatStatusChanged?.Invoke(playerId, active);

            if (showDebugLogs && playerId != NetworkManager.Singleton.LocalClientId)
            {
                Debug.Log($"[CommunicationSystem] Player {playerId} voice chat: {(active ? "ON" : "OFF")}");
            }
        }

        private float GetMicrophoneVolume()
        {
            // TODO: Implement actual microphone volume detection
            // This would use Unity's Microphone class
            // For now, return 0
            return 0f;
        }

        /// <summary>
        /// Initializes voice chat (call when joining match)
        /// </summary>
        public void InitializeVoiceChat()
        {
            if (!enableVoiceChat) return;

            // TODO: Initialize voice chat system
            // This would integrate with:
            // - Unity's built-in Microphone
            // - Unity Gaming Services Voice
            // - Third-party solutions like Vivox, Photon Voice, etc.

            if (showDebugLogs)
                Debug.Log("[CommunicationSystem] Voice chat initialized");
        }

        /// <summary>
        /// Cleans up voice chat (call when leaving match)
        /// </summary>
        public void CleanupVoiceChat()
        {
            isTalking = false;

            // TODO: Cleanup voice chat resources

            if (showDebugLogs)
                Debug.Log("[CommunicationSystem] Voice chat cleaned up");
        }

        public void SetVoiceActivation(bool enabled)
        {
            voiceActivation = enabled;

            if (showDebugLogs)
                Debug.Log($"[CommunicationSystem] Voice activation: {enabled}");
        }

        public void SetVoiceActivationThreshold(float threshold)
        {
            voiceActivationThreshold = Mathf.Clamp01(threshold);
        }

        #endregion

        #region Helpers

        private string GetPlayerName()
        {
            // Get player name from various sources
            if (Social.LeaderboardManager.Instance != null)
            {
                return Social.LeaderboardManager.Instance.LocalPlayerName;
            }

            return $"Player{NetworkManager.Singleton.LocalClientId}";
        }

        #endregion

        #region Properties

        public bool IsTalking => isTalking;
        public bool VoiceActivation => voiceActivation;
        public int ActivePingsCount => activePings.Count;

        #endregion
    }

    #region Data Structures

    [System.Serializable]
    public struct PingData : INetworkSerializable
    {
        public ulong senderId;
        public string senderName;
        public Vector3 position;
        public PingType type;
        public float timestamp;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref senderId);
            serializer.SerializeValue(ref position);
            serializer.SerializeValue(ref type);
            serializer.SerializeValue(ref timestamp);

            // Note: senderName would need custom serialization for strings
            // or use FixedString64Bytes from Unity.Collections
        }
    }

    public enum PingType
    {
        Generic,
        Enemy,
        Loot,
        Defend,
        Attack,
        Extraction,
        Danger,
        Help
    }

    [System.Serializable]
    public class QuickMessage
    {
        public string id;
        public string text;
        public Sprite icon;
        public Color color = Color.white;
        public KeyCode hotkey;
    }

    #endregion

    #region Ping Marker Component

    /// <summary>
    /// Visual marker for pings in the world
    /// </summary>
    public class PingMarker : MonoBehaviour
    {
        private PingData data;
        private float duration;
        private float spawnTime;

        public void Initialize(PingData pingData, float displayDuration)
        {
            data = pingData;
            duration = displayDuration;
            spawnTime = Time.time;
        }

        private void Update()
        {
            // Auto-destroy after duration
            if (Time.time - spawnTime >= duration)
            {
                Destroy(gameObject);
                return;
            }

            // Optional: Fade out over time
            float lifePercent = (Time.time - spawnTime) / duration;
            // Apply fade effect to visuals here
        }

        private void OnDrawGizmos()
        {
            // Draw sphere in editor
            Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
            Gizmos.DrawSphere(transform.position, 0.5f);
        }
    }

    #endregion
}
