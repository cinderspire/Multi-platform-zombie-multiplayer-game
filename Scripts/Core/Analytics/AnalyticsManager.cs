using UnityEngine;
using System.Collections.Generic;

namespace DeadFrontier.Core.Analytics
{
    /// <summary>
    /// Tracks player analytics and telemetry for game improvement
    /// </summary>
    public class AnalyticsManager : Singleton<AnalyticsManager>
    {
        [Header("Analytics Settings")]
        [SerializeField] private bool enableAnalytics = true;
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private float sessionTimeout = 1800f; // 30 minutes

        [Header("Privacy")]
        [SerializeField] private bool anonymizeData = true;

        // Session tracking
        private string sessionID;
        private float sessionStartTime;
        private float lastActivityTime;
        private bool sessionActive = false;

        // Event buffer
        private List<AnalyticsEvent> eventBuffer = new List<AnalyticsEvent>();
        private const int MAX_BUFFER_SIZE = 100;

        // Properties
        public string SessionID => sessionID;
        public bool IsSessionActive => sessionActive;

        protected override void Awake()
        {
            base.Awake();

            // Start session
            StartSession();
        }

        private void Update()
        {
            if (!sessionActive)
                return;

            // Check for session timeout
            if (Time.time - lastActivityTime > sessionTimeout)
            {
                EndSession();
                StartSession();
            }
        }

        #region Session Management

        /// <summary>
        /// Starts a new analytics session
        /// </summary>
        private void StartSession()
        {
            sessionID = System.Guid.NewGuid().ToString();
            sessionStartTime = Time.time;
            lastActivityTime = Time.time;
            sessionActive = true;

            if (enableDebugLogs)
                Debug.Log($"[Analytics] Session started: {sessionID}");

            TrackEvent("session_start", new Dictionary<string, object>
            {
                { "platform", Application.platform.ToString() },
                { "version", Application.version },
                { "unity_version", Application.unityVersion }
            });
        }

        /// <summary>
        /// Ends the current session
        /// </summary>
        private void EndSession()
        {
            if (!sessionActive)
                return;

            float sessionDuration = Time.time - sessionStartTime;

            TrackEvent("session_end", new Dictionary<string, object>
            {
                { "duration", sessionDuration }
            });

            // Flush remaining events
            FlushEvents();

            sessionActive = false;

            if (enableDebugLogs)
                Debug.Log($"[Analytics] Session ended: {sessionDuration:F1}s");
        }

        /// <summary>
        /// Updates last activity time
        /// </summary>
        private void UpdateActivity()
        {
            lastActivityTime = Time.time;
        }

        #endregion

        #region Event Tracking

        /// <summary>
        /// Tracks a generic event
        /// </summary>
        public void TrackEvent(string eventName, Dictionary<string, object> parameters = null)
        {
            if (!enableAnalytics)
                return;

            UpdateActivity();

            AnalyticsEvent evt = new AnalyticsEvent
            {
                eventName = eventName,
                timestamp = System.DateTime.UtcNow,
                sessionID = sessionID,
                parameters = parameters ?? new Dictionary<string, object>()
            };

            // Add to buffer
            eventBuffer.Add(evt);

            if (enableDebugLogs)
                Debug.Log($"[Analytics] Event: {eventName}");

            // Flush if buffer is full
            if (eventBuffer.Count >= MAX_BUFFER_SIZE)
            {
                FlushEvents();
            }
        }

        /// <summary>
        /// Tracks match start
        /// </summary>
        public void TrackMatchStart(string gameMode, string map, int playerCount)
        {
            TrackEvent("match_start", new Dictionary<string, object>
            {
                { "game_mode", gameMode },
                { "map", map },
                { "player_count", playerCount }
            });
        }

        /// <summary>
        /// Tracks match end
        /// </summary>
        public void TrackMatchEnd(string result, float duration, int kills, int deaths, bool extracted)
        {
            TrackEvent("match_end", new Dictionary<string, object>
            {
                { "result", result }, // "win", "loss", "extracted"
                { "duration", duration },
                { "kills", kills },
                { "deaths", deaths },
                { "extracted", extracted }
            });
        }

        /// <summary>
        /// Tracks player death
        /// </summary>
        public void TrackPlayerDeath(string causeOfDeath, Vector3 location)
        {
            TrackEvent("player_death", new Dictionary<string, object>
            {
                { "cause", causeOfDeath },
                { "location_x", location.x },
                { "location_y", location.y },
                { "location_z", location.z }
            });
        }

        /// <summary>
        /// Tracks zombie kill
        /// </summary>
        public void TrackZombieKill(string zombieType, string weaponUsed, bool isHeadshot)
        {
            TrackEvent("zombie_kill", new Dictionary<string, object>
            {
                { "zombie_type", zombieType },
                { "weapon", weaponUsed },
                { "headshot", isHeadshot }
            });
        }

        /// <summary>
        /// Tracks extraction
        /// </summary>
        public void TrackExtraction(bool successful, int itemsExtracted, int valueExtracted)
        {
            TrackEvent("extraction", new Dictionary<string, object>
            {
                { "successful", successful },
                { "items_extracted", itemsExtracted },
                { "value", valueExtracted }
            });
        }

        /// <summary>
        /// Tracks weapon usage
        /// </summary>
        public void TrackWeaponFired(string weaponID, bool hit, float distance)
        {
            TrackEvent("weapon_fired", new Dictionary<string, object>
            {
                { "weapon", weaponID },
                { "hit", hit },
                { "distance", distance }
            });
        }

        /// <summary>
        /// Tracks level up
        /// </summary>
        public void TrackLevelUp(int newLevel, int prestigeLevel)
        {
            TrackEvent("level_up", new Dictionary<string, object>
            {
                { "level", newLevel },
                { "prestige", prestigeLevel }
            });
        }

        /// <summary>
        /// Tracks achievement unlocked
        /// </summary>
        public void TrackAchievementUnlocked(string achievementID, string achievementName)
        {
            TrackEvent("achievement_unlocked", new Dictionary<string, object>
            {
                { "achievement_id", achievementID },
                { "achievement_name", achievementName }
            });
        }

        /// <summary>
        /// Tracks challenge completed
        /// </summary>
        public void TrackChallengeCompleted(string challengeID, string challengeType, float completionTime)
        {
            TrackEvent("challenge_completed", new Dictionary<string, object>
            {
                { "challenge_id", challengeID },
                { "challenge_type", challengeType },
                { "completion_time", completionTime }
            });
        }

        /// <summary>
        /// Tracks monetization event
        /// </summary>
        public void TrackPurchase(string itemID, string itemType, int price, string currency)
        {
            TrackEvent("purchase", new Dictionary<string, object>
            {
                { "item_id", itemID },
                { "item_type", itemType },
                { "price", price },
                { "currency", currency }
            });
        }

        /// <summary>
        /// Tracks battle pass progression
        /// </summary>
        public void TrackBattlePassTier(int tier, bool premium)
        {
            TrackEvent("battlepass_tier", new Dictionary<string, object>
            {
                { "tier", tier },
                { "premium", premium }
            });
        }

        /// <summary>
        /// Tracks loadout change
        /// </summary>
        public void TrackLoadoutChanged(List<string> weapons, List<string> perks)
        {
            TrackEvent("loadout_changed", new Dictionary<string, object>
            {
                { "weapons", string.Join(",", weapons) },
                { "perks", string.Join(",", perks) }
            });
        }

        /// <summary>
        /// Tracks settings changed
        /// </summary>
        public void TrackSettingsChanged(string settingName, object value)
        {
            TrackEvent("settings_changed", new Dictionary<string, object>
            {
                { "setting", settingName },
                { "value", value.ToString() }
            });
        }

        /// <summary>
        /// Tracks error/crash
        /// </summary>
        public void TrackError(string errorMessage, string stackTrace)
        {
            TrackEvent("error", new Dictionary<string, object>
            {
                { "message", errorMessage },
                { "stack_trace", anonymizeData ? "REDACTED" : stackTrace }
            });
        }

        #endregion

        #region Performance Tracking

        /// <summary>
        /// Tracks performance metrics
        /// </summary>
        public void TrackPerformance(float fps, float ping, int zombieCount, int playerCount)
        {
            TrackEvent("performance", new Dictionary<string, object>
            {
                { "fps", fps },
                { "ping", ping },
                { "zombies", zombieCount },
                { "players", playerCount }
            });
        }

        /// <summary>
        /// Tracks load time
        /// </summary>
        public void TrackLoadTime(string sceneName, float loadTime)
        {
            TrackEvent("load_time", new Dictionary<string, object>
            {
                { "scene", sceneName },
                { "time", loadTime }
            });
        }

        #endregion

        #region Data Transmission

        /// <summary>
        /// Flushes event buffer to backend
        /// </summary>
        private void FlushEvents()
        {
            if (eventBuffer.Count == 0)
                return;

            // TODO: Send events to analytics backend
            // Options:
            // 1. Unity Analytics
            // 2. Google Analytics
            // 3. Custom backend API
            // 4. Firebase Analytics

            if (enableDebugLogs)
                Debug.Log($"[Analytics] Flushing {eventBuffer.Count} events");

            // For now, just log them
            foreach (var evt in eventBuffer)
            {
                if (enableDebugLogs)
                {
                    string paramStr = evt.parameters.Count > 0
                        ? string.Join(", ", evt.parameters)
                        : "none";
                    Debug.Log($"  - {evt.eventName} ({paramStr})");
                }
            }

            // Clear buffer
            eventBuffer.Clear();
        }

        #endregion

        #region Unity Analytics Integration (Placeholder)

        /// <summary>
        /// Initializes Unity Analytics
        /// </summary>
        private void InitializeUnityAnalytics()
        {
            // TODO: Initialize Unity Analytics
            // Analytics.initializeOnStartup = true;
            // Analytics.Analytics.enabled = enableAnalytics;
        }

        #endregion

        #region Lifecycle Events

        private void OnApplicationQuit()
        {
            EndSession();
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                TrackEvent("app_paused");
                FlushEvents();
            }
            else
            {
                TrackEvent("app_resumed");
            }
        }

        private void OnApplicationFocus(bool focus)
        {
            if (focus)
            {
                UpdateActivity();
            }
        }

        #endregion
    }

    /// <summary>
    /// Represents a single analytics event
    /// </summary>
    [System.Serializable]
    public class AnalyticsEvent
    {
        public string eventName;
        public System.DateTime timestamp;
        public string sessionID;
        public Dictionary<string, object> parameters;
    }
}
