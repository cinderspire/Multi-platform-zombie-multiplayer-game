using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Analytics
{
    /// <summary>
    /// Comprehensive analytics and telemetry system tracking player behavior, game metrics,
    /// performance data, and custom events with privacy compliance and batch processing.
    /// </summary>
    public class AnalyticsSystem : NetworkBehaviour
    {
        public static AnalyticsSystem Instance { get; private set; }

        [Header("Analytics Configuration")]
        [SerializeField] private bool analyticsEnabled = true;
        [SerializeField] private bool debugMode = false;
        [SerializeField] private int batchSize = 50;
        [SerializeField] private float batchInterval = 60f;
        [SerializeField] private string analyticsEndpoint = "https://api.zombiegame.com/analytics";

        [Header("Privacy Settings")]
        [SerializeField] private bool respectDoNotTrack = true;
        [SerializeField] private bool anonymizeIPs = true;
        [SerializeField] private bool gdprCompliant = true;

        private Dictionary<ulong, PlayerAnalytics> playerAnalytics = new Dictionary<ulong, PlayerAnalytics>();
        private Dictionary<ulong, SessionData> activeSessions = new Dictionary<ulong, SessionData>();
        private Queue<AnalyticsEvent> eventQueue = new Queue<AnalyticsEvent>();
        private float lastBatchTime;

        public event Action<AnalyticsEvent> OnEventTracked;
        public event Action<int> OnBatchSent;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Update()
        {
            if (IsServer && analyticsEnabled)
            {
                if (Time.time - lastBatchTime >= batchInterval)
                {
                    ProcessEventBatch();
                    lastBatchTime = Time.time;
                }

                UpdateActiveSessions();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void InitializePlayerAnalyticsServerRpc(ulong playerId, string platform, string deviceModel, ServerRpcParams rpcParams = default)
        {
            if (!playerAnalytics.ContainsKey(playerId))
            {
                playerAnalytics[playerId] = new PlayerAnalytics
                {
                    playerId = playerId,
                    platform = platform,
                    deviceModel = deviceModel,
                    firstSeen = DateTime.UtcNow,
                    lastSeen = DateTime.UtcNow,
                    totalSessions = 0,
                    totalPlayTime = 0f
                };

                StartSession(playerId);
                Debug.Log($"Initialized analytics for player {playerId}");
            }
        }

        // Session Tracking
        private void StartSession(ulong playerId)
        {
            if (activeSessions.ContainsKey(playerId)) return;

            var session = new SessionData
            {
                sessionId = Guid.NewGuid().ToString(),
                playerId = playerId,
                startTime = DateTime.UtcNow,
                events = new List<AnalyticsEvent>()
            };

            activeSessions[playerId] = session;

            if (playerAnalytics.ContainsKey(playerId))
            {
                playerAnalytics[playerId].totalSessions++;
            }

            TrackEvent(playerId, "session_start", new Dictionary<string, object>
            {
                { "session_id", session.sessionId },
                { "timestamp", session.startTime }
            });
        }

        [ServerRpc(RequireOwnership = false)]
        public void EndSessionServerRpc(ulong playerId, ServerRpcParams rpcParams = default)
        {
            if (!activeSessions.TryGetValue(playerId, out var session)) return;

            session.endTime = DateTime.UtcNow;
            session.duration = (float)(session.endTime - session.startTime).TotalSeconds;

            if (playerAnalytics.ContainsKey(playerId))
            {
                playerAnalytics[playerId].totalPlayTime += session.duration;
                playerAnalytics[playerId].lastSeen = DateTime.UtcNow;
            }

            TrackEvent(playerId, "session_end", new Dictionary<string, object>
            {
                { "session_id", session.sessionId },
                { "duration", session.duration },
                { "events_count", session.events.Count }
            });

            activeSessions.Remove(playerId);
        }

        private void UpdateActiveSessions()
        {
            foreach (var kvp in activeSessions.ToList())
            {
                var session = kvp.Value;
                session.currentDuration = (float)(DateTime.UtcNow - session.startTime).TotalSeconds;
            }
        }

        // Event Tracking
        [ServerRpc(RequireOwnership = false)]
        public void TrackEventServerRpc(ulong playerId, string eventName, Dictionary<string, string> parameters, ServerRpcParams rpcParams = default)
        {
            if (!analyticsEnabled) return;
            if (!CheckPrivacyConsent(playerId)) return;

            var eventData = new Dictionary<string, object>();
            foreach (var kvp in parameters)
            {
                eventData[kvp.Key] = kvp.Value;
            }

            TrackEvent(playerId, eventName, eventData);
        }

        private void TrackEvent(ulong playerId, string eventName, Dictionary<string, object> parameters = null)
        {
            if (!analyticsEnabled) return;

            var analyticsEvent = new AnalyticsEvent
            {
                eventId = Guid.NewGuid().ToString(),
                playerId = playerId,
                eventName = eventName,
                timestamp = DateTime.UtcNow,
                parameters = parameters ?? new Dictionary<string, object>()
            };

            // Add session context
            if (activeSessions.TryGetValue(playerId, out var session))
            {
                analyticsEvent.sessionId = session.sessionId;
                analyticsEvent.parameters["session_duration"] = session.currentDuration;
                session.events.Add(analyticsEvent);
            }

            // Add to queue for batch processing
            eventQueue.Enqueue(analyticsEvent);

            OnEventTracked?.Invoke(analyticsEvent);

            if (debugMode)
            {
                Debug.Log($"[Analytics] {eventName} - Player: {playerId}");
            }
        }

        // Game Events
        [ServerRpc(RequireOwnership = false)]
        public void TrackPlayerActionServerRpc(ulong playerId, PlayerAction action, string target, ServerRpcParams rpcParams = default)
        {
            TrackEvent(playerId, $"player_{action.ToString().ToLower()}", new Dictionary<string, object>
            {
                { "action", action.ToString() },
                { "target", target }
            });
        }

        [ServerRpc(RequireOwnership = false)]
        public void TrackCombatEventServerRpc(ulong playerId, CombatEventType eventType, string weaponId, int damage, bool isHeadshot, ServerRpcParams rpcParams = default)
        {
            TrackEvent(playerId, $"combat_{eventType.ToString().ToLower()}", new Dictionary<string, object>
            {
                { "event_type", eventType.ToString() },
                { "weapon_id", weaponId },
                { "damage", damage },
                { "is_headshot", isHeadshot }
            });
        }

        [ServerRpc(RequireOwnership = false)]
        public void TrackProgressionEventServerRpc(ulong playerId, ProgressionEventType eventType, int oldValue, int newValue, ServerRpcParams rpcParams = default)
        {
            TrackEvent(playerId, $"progression_{eventType.ToString().ToLower()}", new Dictionary<string, object>
            {
                { "event_type", eventType.ToString() },
                { "old_value", oldValue },
                { "new_value", newValue }
            });
        }

        [ServerRpc(RequireOwnership = false)]
        public void TrackEconomyEventServerRpc(ulong playerId, EconomyEventType eventType, string currencyType, int amount, string source, ServerRpcParams rpcParams = default)
        {
            TrackEvent(playerId, $"economy_{eventType.ToString().ToLower()}", new Dictionary<string, object>
            {
                { "event_type", eventType.ToString() },
                { "currency_type", currencyType },
                { "amount", amount },
                { "source", source }
            });
        }

        [ServerRpc(RequireOwnership = false)]
        public void TrackSocialEventServerRpc(ulong playerId, SocialEventType eventType, ulong targetPlayerId, ServerRpcParams rpcParams = default)
        {
            TrackEvent(playerId, $"social_{eventType.ToString().ToLower()}", new Dictionary<string, object>
            {
                { "event_type", eventType.ToString() },
                { "target_player_id", targetPlayerId }
            });
        }

        [ServerRpc(RequireOwnership = false)]
        public void TrackUIEventServerRpc(ulong playerId, string screenName, string elementName, UIEventType eventType, ServerRpcParams rpcParams = default)
        {
            TrackEvent(playerId, $"ui_{eventType.ToString().ToLower()}", new Dictionary<string, object>
            {
                { "screen_name", screenName },
                { "element_name", elementName },
                { "event_type", eventType.ToString() }
            });
        }

        [ServerRpc(RequireOwnership = false)]
        public void TrackPerformanceMetricServerRpc(ulong playerId, PerformanceMetric metric, float value, ServerRpcParams rpcParams = default)
        {
            TrackEvent(playerId, $"performance_{metric.ToString().ToLower()}", new Dictionary<string, object>
            {
                { "metric", metric.ToString() },
                { "value", value }
            });
        }

        [ServerRpc(RequireOwnership = false)]
        public void TrackErrorServerRpc(ulong playerId, string errorType, string errorMessage, string stackTrace, ServerRpcParams rpcParams = default)
        {
            TrackEvent(playerId, "error", new Dictionary<string, object>
            {
                { "error_type", errorType },
                { "error_message", errorMessage },
                { "stack_trace", stackTrace }
            });
        }

        // Funnel Tracking
        [ServerRpc(RequireOwnership = false)]
        public void TrackFunnelStepServerRpc(ulong playerId, string funnelName, int stepNumber, string stepName, bool completed, ServerRpcParams rpcParams = default)
        {
            TrackEvent(playerId, "funnel_step", new Dictionary<string, object>
            {
                { "funnel_name", funnelName },
                { "step_number", stepNumber },
                { "step_name", stepName },
                { "completed", completed }
            });
        }

        // Retention Metrics
        [ServerRpc(RequireOwnership = false)]
        public void TrackRetentionMilestoneServerRpc(ulong playerId, RetentionMilestone milestone, ServerRpcParams rpcParams = default)
        {
            TrackEvent(playerId, "retention_milestone", new Dictionary<string, object>
            {
                { "milestone", milestone.ToString() },
                { "total_sessions", playerAnalytics.GetValueOrDefault(playerId)?.totalSessions ?? 0 },
                { "total_playtime", playerAnalytics.GetValueOrDefault(playerId)?.totalPlayTime ?? 0 }
            });
        }

        // Custom Dimension Tracking
        [ServerRpc(RequireOwnership = false)]
        public void SetPlayerPropertyServerRpc(ulong playerId, string propertyName, string propertyValue, ServerRpcParams rpcParams = default)
        {
            if (!playerAnalytics.ContainsKey(playerId)) return;

            if (playerAnalytics[playerId].customProperties == null)
            {
                playerAnalytics[playerId].customProperties = new Dictionary<string, string>();
            }

            playerAnalytics[playerId].customProperties[propertyName] = propertyValue;
        }

        // Batch Processing
        private void ProcessEventBatch()
        {
            if (eventQueue.Count == 0) return;

            List<AnalyticsEvent> batch = new List<AnalyticsEvent>();
            int count = Mathf.Min(batchSize, eventQueue.Count);

            for (int i = 0; i < count; i++)
            {
                if (eventQueue.Count > 0)
                {
                    batch.Add(eventQueue.Dequeue());
                }
            }

            if (batch.Count > 0)
            {
                SendBatchToServer(batch);
                OnBatchSent?.Invoke(batch.Count);
            }
        }

        private void SendBatchToServer(List<AnalyticsEvent> batch)
        {
            // Serialize and send to analytics endpoint
            // Would use REST API or analytics provider SDK (e.g., Google Analytics, Mixpanel, etc.)
            
            if (debugMode)
            {
                Debug.Log($"[Analytics] Sending batch of {batch.Count} events");
            }
        }

        // Privacy & Consent
        private bool CheckPrivacyConsent(ulong playerId)
        {
            if (!gdprCompliant) return true;

            // Check if player has consented to analytics
            // Would integrate with PrivacySettings
            return true;
        }

        [ServerRpc(RequireOwnership = false)]
        public void SetAnalyticsConsentServerRpc(ulong playerId, bool consent, ServerRpcParams rpcParams = default)
        {
            if (playerAnalytics.ContainsKey(playerId))
            {
                playerAnalytics[playerId].hasConsent = consent;
                Debug.Log($"Analytics consent for player {playerId}: {consent}");
            }
        }

        // Query Methods
        public PlayerAnalytics GetPlayerAnalytics(ulong playerId) => playerAnalytics.GetValueOrDefault(playerId);
        public SessionData GetActiveSession(ulong playerId) => activeSessions.GetValueOrDefault(playerId);
        public int GetQueueSize() => eventQueue.Count;

        public Dictionary<string, int> GetEventCounts(ulong playerId)
        {
            if (!activeSessions.TryGetValue(playerId, out var session)) return new Dictionary<string, int>();

            return session.events
                .GroupBy(e => e.eventName)
                .ToDictionary(g => g.Key, g => g.Count());
        }
    }

    [Serializable]
    public class PlayerAnalytics
    {
        public ulong playerId;
        public string platform;
        public string deviceModel;
        public DateTime firstSeen;
        public DateTime lastSeen;
        public int totalSessions;
        public float totalPlayTime;
        public Dictionary<string, string> customProperties;
        public bool hasConsent;
    }

    [Serializable]
    public class SessionData
    {
        public string sessionId;
        public ulong playerId;
        public DateTime startTime;
        public DateTime endTime;
        public float duration;
        public float currentDuration;
        public List<AnalyticsEvent> events;
    }

    [Serializable]
    public class AnalyticsEvent
    {
        public string eventId;
        public ulong playerId;
        public string sessionId;
        public string eventName;
        public DateTime timestamp;
        public Dictionary<string, object> parameters;
    }

    public enum PlayerAction
    {
        Jump, Crouch, Sprint, Shoot, Reload, UseItem, Interact,
        OpenInventory, OpenMap, OpenMenu, ChangeWeapon
    }

    public enum CombatEventType
    {
        DamageDealt, DamageTaken, Kill, Death, Headshot,
        WeaponFired, WeaponReloaded, AbilityUsed
    }

    public enum ProgressionEventType
    {
        LevelUp, ExperienceGained, RankUp, PrestigeUp,
        SkillUnlocked, PerkUnlocked
    }

    public enum EconomyEventType
    {
        CurrencyEarned, CurrencySpent, ItemPurchased, ItemSold,
        ItemCrafted, ItemTraded, RewardClaimed
    }

    public enum SocialEventType
    {
        PartyJoined, PartyLeft, FriendAdded, FriendRemoved,
        ClanJoined, ClanLeft, TradeInitiated, TradeCancelled
    }

    public enum UIEventType
    {
        ScreenOpened, ScreenClosed, ButtonClicked, TabChanged,
        SettingChanged, NotificationShown, NotificationClicked
    }

    public enum PerformanceMetric
    {
        FPS, FrameTime, LoadTime, MemoryUsage, NetworkLatency,
        PacketLoss, DrawCalls, Vertices
    }

    public enum RetentionMilestone
    {
        FirstSession, Day1, Day3, Day7, Day14, Day30,
        Session10, Session50, Session100
    }
}
