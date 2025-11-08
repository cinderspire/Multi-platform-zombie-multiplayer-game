using UnityEngine;
using System;
using System.Collections.Generic;

namespace DeadFrontier.Core.LiveOps
{
    /// <summary>
    /// Manages seasonal events and limited-time content for live operations
    /// Drives player engagement with rotating events, challenges, and exclusive rewards
    /// </summary>
    public class SeasonalEventsManager : Singleton<SeasonalEventsManager>
    {
        [Header("Event Configuration")]
        [SerializeField] private List<SeasonalEvent> seasonalEvents = new List<SeasonalEvent>();
        [SerializeField] private float checkInterval = 300f; // Check every 5 minutes
        [SerializeField] private bool useServerTime = true;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;
        [SerializeField] private bool forceEventActive = false;
        [SerializeField] private int forcedEventIndex = 0;

        // Active events
        private List<SeasonalEvent> activeEvents = new List<SeasonalEvent>();
        private float checkTimer = 0f;

        // Current season
        private Season currentSeason;

        // Events
        public event System.Action<SeasonalEvent> OnEventStarted;
        public event System.Action<SeasonalEvent> OnEventEnded;
        public event System.Action<Season> OnSeasonChanged;

        protected override void Awake()
        {
            base.Awake();
            InitializeDefaultEvents();
            DetermineSeason();
        }

        private void Start()
        {
            CheckActiveEvents();
        }

        private void Update()
        {
            checkTimer += Time.deltaTime;
            if (checkTimer >= checkInterval)
            {
                checkTimer = 0f;
                CheckActiveEvents();
            }
        }

        #region Initialization

        private void InitializeDefaultEvents()
        {
            if (seasonalEvents.Count == 0)
            {
                // Halloween Event
                seasonalEvents.Add(new SeasonalEvent
                {
                    eventId = "halloween_2025",
                    eventName = "Nightmare Festival",
                    description = "The zombies grow stronger as darkness falls. Special Halloween zombies and exclusive rewards!",
                    eventType = EventType.Seasonal,
                    startDate = new DateTime(2025, 10, 25),
                    endDate = new DateTime(2025, 11, 2),
                    season = Season.Fall,
                    rewardMultiplier = 1.5f,
                    specialZombieTypes = new List<string> { "PumpkinHead", "GhostZombie" },
                    exclusiveRewards = new List<string> { "halloween_skin", "pumpkin_weapon" }
                });

                // Christmas Event
                seasonalEvents.Add(new SeasonalEvent
                {
                    eventId = "christmas_2025",
                    eventName = "Winter Survival",
                    description = "Survive the frozen wasteland. Snow zombies and festive rewards await!",
                    eventType = EventType.Seasonal,
                    startDate = new DateTime(2025, 12, 20),
                    endDate = new DateTime(2026, 1, 2),
                    season = Season.Winter,
                    rewardMultiplier = 2.0f,
                    specialZombieTypes = new List<string> { "FrozenZombie", "SnowmanZombie" },
                    exclusiveRewards = new List<string> { "santa_outfit", "candy_cane_melee" }
                });

                // Summer Event
                seasonalEvents.Add(new SeasonalEvent
                {
                    eventId = "summer_2025",
                    eventName = "Beach Assault",
                    description = "The undead have invaded the beaches! Water-themed zombies and summer cosmetics!",
                    eventType = EventType.Seasonal,
                    startDate = new DateTime(2025, 7, 1),
                    endDate = new DateTime(2025, 8, 1),
                    season = Season.Summer,
                    rewardMultiplier = 1.3f,
                    specialZombieTypes = new List<string> { "BeachZombie", "SurferZombie" },
                    exclusiveRewards = new List<string> { "swimsuit_outfit", "water_gun" }
                });

                // Weekend Event
                seasonalEvents.Add(new SeasonalEvent
                {
                    eventId = "double_xp_weekend",
                    eventName = "Double XP Weekend",
                    description = "Earn double XP on all activities!",
                    eventType = EventType.WeekendBonus,
                    rewardMultiplier = 2.0f,
                    isRecurring = true,
                    recurringDays = new List<DayOfWeek> { DayOfWeek.Saturday, DayOfWeek.Sunday }
                });
            }
        }

        private void DetermineSeason()
        {
            DateTime now = GetCurrentTime();
            int month = now.Month;

            if (month >= 3 && month <= 5)
                currentSeason = Season.Spring;
            else if (month >= 6 && month <= 8)
                currentSeason = Season.Summer;
            else if (month >= 9 && month <= 11)
                currentSeason = Season.Fall;
            else
                currentSeason = Season.Winter;

            if (showDebugLogs)
                Debug.Log($"[SeasonalEventsManager] Current season: {currentSeason}");

            OnSeasonChanged?.Invoke(currentSeason);
        }

        #endregion

        #region Event Management

        private void CheckActiveEvents()
        {
            DateTime now = GetCurrentTime();
            List<SeasonalEvent> newActiveEvents = new List<SeasonalEvent>();

            foreach (var evt in seasonalEvents)
            {
                bool shouldBeActive = IsEventActive(evt, now);

                if (shouldBeActive)
                {
                    newActiveEvents.Add(evt);

                    // Check if this is a newly activated event
                    if (!activeEvents.Contains(evt))
                    {
                        StartEvent(evt);
                    }
                }
                else if (activeEvents.Contains(evt))
                {
                    EndEvent(evt);
                }
            }

            activeEvents = newActiveEvents;

            if (showDebugLogs)
                Debug.Log($"[SeasonalEventsManager] Active events: {activeEvents.Count}");
        }

        private bool IsEventActive(SeasonalEvent evt, DateTime now)
        {
#if UNITY_EDITOR
            if (forceEventActive && seasonalEvents.IndexOf(evt) == forcedEventIndex)
                return true;
#endif

            // Check recurring events
            if (evt.isRecurring)
            {
                if (evt.recurringDays != null && evt.recurringDays.Count > 0)
                {
                    return evt.recurringDays.Contains(now.DayOfWeek);
                }
            }

            // Check date range
            if (evt.startDate != default && evt.endDate != default)
            {
                return now >= evt.startDate && now <= evt.endDate;
            }

            return false;
        }

        private void StartEvent(SeasonalEvent evt)
        {
            if (showDebugLogs)
                Debug.Log($"[SeasonalEventsManager] Event started: {evt.eventName}");

            evt.isActive = true;

            // Apply event modifiers
            ApplyEventModifiers(evt);

            // Show notification
            if (UI.NotificationManager.Instance != null)
            {
                UI.NotificationManager.Instance.ShowNotification(
                    "Event Started!",
                    evt.eventName,
                    GetSeasonColor(evt.season),
                    UI.NotificationType.Info
                );
            }

            OnEventStarted?.Invoke(evt);

            // Track analytics
            Analytics.AnalyticsManager.Instance?.TrackEvent("seasonal_event_started", new Dictionary<string, object>
            {
                { "event_id", evt.eventId },
                { "event_name", evt.eventName },
                { "event_type", evt.eventType.ToString() }
            });
        }

        private void EndEvent(SeasonalEvent evt)
        {
            if (showDebugLogs)
                Debug.Log($"[SeasonalEventsManager] Event ended: {evt.eventName}");

            evt.isActive = false;

            // Remove event modifiers
            RemoveEventModifiers(evt);

            OnEventEnded?.Invoke(evt);

            // Track analytics
            Analytics.AnalyticsManager.Instance?.TrackEvent("seasonal_event_ended", new Dictionary<string, object>
            {
                { "event_id", evt.eventId },
                { "event_name", evt.eventName }
            });
        }

        private void ApplyEventModifiers(SeasonalEvent evt)
        {
            // Apply reward multiplier
            if (evt.rewardMultiplier > 1.0f)
            {
                // This would be checked in EconomyManager.GrantMatchRewards
                if (showDebugLogs)
                    Debug.Log($"[SeasonalEventsManager] Reward multiplier active: {evt.rewardMultiplier}x");
            }

            // Enable special zombie types
            if (evt.specialZombieTypes != null && evt.specialZombieTypes.Count > 0)
            {
                if (Zombies.ZombieManager.Instance != null)
                {
                    // Would enable special spawning logic
                    if (showDebugLogs)
                        Debug.Log($"[SeasonalEventsManager] Special zombies enabled: {string.Join(", ", evt.specialZombieTypes)}");
                }
            }

            // Add event-specific challenges
            if (evt.eventChallenges != null && evt.eventChallenges.Count > 0 &&
                Progression.Challenges.ChallengeManager.Instance != null)
            {
                // Would add temporary challenges
                if (showDebugLogs)
                    Debug.Log($"[SeasonalEventsManager] Event challenges added: {evt.eventChallenges.Count}");
            }
        }

        private void RemoveEventModifiers(SeasonalEvent evt)
        {
            // Remove event-specific content
            if (showDebugLogs)
                Debug.Log($"[SeasonalEventsManager] Removed modifiers for: {evt.eventName}");
        }

        #endregion

        #region Queries

        /// <summary>
        /// Gets all currently active events
        /// </summary>
        public List<SeasonalEvent> GetActiveEvents()
        {
            return new List<SeasonalEvent>(activeEvents);
        }

        /// <summary>
        /// Gets upcoming events
        /// </summary>
        public List<SeasonalEvent> GetUpcomingEvents(int daysAhead = 30)
        {
            DateTime now = GetCurrentTime();
            DateTime future = now.AddDays(daysAhead);
            List<SeasonalEvent> upcoming = new List<SeasonalEvent>();

            foreach (var evt in seasonalEvents)
            {
                if (!evt.isActive && evt.startDate > now && evt.startDate <= future)
                {
                    upcoming.Add(evt);
                }
            }

            return upcoming;
        }

        /// <summary>
        /// Checks if a specific event is active
        /// </summary>
        public bool IsEventActive(string eventId)
        {
            return activeEvents.Exists(e => e.eventId == eventId);
        }

        /// <summary>
        /// Gets the current reward multiplier from active events
        /// </summary>
        public float GetCurrentRewardMultiplier()
        {
            float multiplier = 1.0f;

            foreach (var evt in activeEvents)
            {
                if (evt.rewardMultiplier > multiplier)
                {
                    multiplier = evt.rewardMultiplier;
                }
            }

            return multiplier;
        }

        /// <summary>
        /// Gets event by ID
        /// </summary>
        public SeasonalEvent GetEvent(string eventId)
        {
            return seasonalEvents.Find(e => e.eventId == eventId);
        }

        #endregion

        #region Helpers

        private DateTime GetCurrentTime()
        {
            if (useServerTime)
            {
                // TODO: Fetch from server for anti-cheat
                // For now, use system time
                return DateTime.Now;
            }
            return DateTime.Now;
        }

        private Color GetSeasonColor(Season season)
        {
            switch (season)
            {
                case Season.Spring:
                    return new Color(0.5f, 1f, 0.5f); // Green
                case Season.Summer:
                    return new Color(1f, 0.9f, 0.3f); // Yellow
                case Season.Fall:
                    return new Color(1f, 0.5f, 0.1f); // Orange
                case Season.Winter:
                    return new Color(0.7f, 0.9f, 1f); // Light blue
                default:
                    return Color.white;
            }
        }

        #endregion

        #region Properties

        public Season CurrentSeason => currentSeason;
        public int ActiveEventCount => activeEvents.Count;

        #endregion
    }

    #region Data Structures

    public enum Season
    {
        Spring,
        Summer,
        Fall,
        Winter
    }

    public enum EventType
    {
        Seasonal,       // Major seasonal events (Halloween, Christmas, etc.)
        WeekendBonus,   // Weekend XP/Currency bonuses
        LimitedTime,    // Limited time events
        Community,      // Community-driven events
        Special         // Special occasions
    }

    [System.Serializable]
    public class SeasonalEvent
    {
        [Header("Event Info")]
        public string eventId;
        public string eventName;
        [TextArea(2, 4)]
        public string description;
        public EventType eventType;
        public Sprite eventIcon;

        [Header("Timing")]
        public DateTime startDate;
        public DateTime endDate;
        public Season season;

        [Header("Recurring")]
        public bool isRecurring;
        public List<DayOfWeek> recurringDays;

        [Header("Rewards")]
        public float rewardMultiplier = 1.0f;
        public List<string> exclusiveRewards;

        [Header("Content")]
        public List<string> specialZombieTypes;
        public List<string> eventChallenges;
        public List<string> eventAchievements;

        [Header("Shop")]
        public List<string> limitedTimeItems;

        // Runtime
        [System.NonSerialized]
        public bool isActive;
    }

    #endregion
}
