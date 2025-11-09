using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;
using System;

namespace DeadFrontier.Events
{
    /// <summary>
    /// Comprehensive seasonal events system.
    /// Manages limited-time events, special modes, event rewards, and progression.
    /// Includes holiday events, community challenges, and event-exclusive content.
    /// </summary>
    public class SeasonalEventSystem : NetworkBehaviour
    {
        public static SeasonalEventSystem Instance { get; private set; }

        [Header("Event Settings")]
        [SerializeField] private bool enableEvents = true;
        [SerializeField] private float eventCheckInterval = 300f; // 5 minutes

        [Header("Event Store")]
        [SerializeField] private int maxEventStoreItems = 20;
        [SerializeField] private float eventCurrencyMultiplier = 1.5f;

        // Active events
        private Dictionary<string, SeasonalEvent> activeEvents = new Dictionary<string, SeasonalEvent>();
        private Dictionary<string, EventDefinition> eventDefinitions = new Dictionary<string, EventDefinition>();

        // Player event data
        private Dictionary<ulong, PlayerEventData> playerEventData = new Dictionary<ulong, PlayerEventData>();

        // Event challenges
        private Dictionary<string, List<EventChallenge>> eventChallenges = new Dictionary<string, List<EventChallenge>>();

        // Events
        public event Action<string, SeasonalEvent> OnEventStarted;
        public event Action<string> OnEventEnded;
        public event Action<ulong, string, EventChallenge> OnEventChallengeCompleted;

        private float eventCheckTimer = 0f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeEvents();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (!IsServer) return;

            eventCheckTimer += Time.deltaTime;

            if (eventCheckTimer >= eventCheckInterval)
            {
                eventCheckTimer = 0f;
                CheckEventSchedule();
            }

            UpdateActiveEvents();
        }

        #region Initialization

        private void InitializeEvents()
        {
            // Halloween Event
            RegisterEventDefinition(new EventDefinition
            {
                eventId = "event_halloween_2024",
                eventName = "Zombie Apocalypse: Halloween Terror",
                description = "Face the horrors of Halloween with special zombie variants and spooky rewards!",
                eventType = EventType.Seasonal,
                startDate = new DateTime(2024, 10, 20),
                endDate = new DateTime(2024, 11, 3),
                rewardMultiplier = 2f,
                eventCurrency = "halloween_tokens",
                specialModes = new List<string> { "mode_haunted_house", "mode_zombie_horde" },
                eventStoreItems = new List<string> { "cosmetic_witch_hat", "cosmetic_vampire_cape", "weapon_pumpkin_launcher" }
            });

            // Winter Event
            RegisterEventDefinition(new EventDefinition
            {
                eventId = "event_winter_2024",
                eventName = "Winter Warfare",
                description = "Battle through the frozen wasteland in this winter-themed event!",
                eventType = EventType.Seasonal,
                startDate = new DateTime(2024, 12, 15),
                endDate = new DateTime(2025, 1, 5),
                rewardMultiplier = 1.5f,
                eventCurrency = "winter_tokens",
                specialModes = new List<string> { "mode_snowstorm", "mode_frozen_city" },
                eventStoreItems = new List<string> { "cosmetic_santa_outfit", "weapon_ice_rifle", "cosmetic_snowman_charm" }
            });

            // Spring Event
            RegisterEventDefinition(new EventDefinition
            {
                eventId = "event_spring_2025",
                eventName = "Rebirth Protocol",
                description = "New life emerges from the apocalypse. Discover hope in the ruins.",
                eventType = EventType.Seasonal,
                startDate = new DateTime(2025, 3, 20),
                endDate = new DateTime(2025, 4, 10),
                rewardMultiplier = 1.5f,
                eventCurrency = "spring_tokens",
                specialModes = new List<string> { "mode_reclamation" },
                eventStoreItems = new List<string> { "cosmetic_flower_crown", "weapon_nature_skin" }
            });

            // Community Event
            RegisterEventDefinition(new EventDefinition
            {
                eventId = "event_community_challenge_001",
                eventName = "Global Zombie Extermination",
                description = "Work together to eliminate 1 million zombies globally!",
                eventType = EventType.Community,
                startDate = DateTime.UtcNow,
                endDate = DateTime.UtcNow.AddDays(14),
                communityGoal = 1000000,
                rewardMultiplier = 2f,
                eventCurrency = "community_tokens"
            });

            // Limited Time Mode
            RegisterEventDefinition(new EventDefinition
            {
                eventId = "event_weekend_blitz",
                eventName = "Weekend Blitz",
                description = "Double XP and rewards all weekend!",
                eventType = EventType.LimitedTime,
                startDate = DateTime.UtcNow,
                endDate = DateTime.UtcNow.AddDays(2),
                rewardMultiplier = 2f,
                xpMultiplier = 2f
            });

            Debug.Log($"[SeasonalEventSystem] Initialized {eventDefinitions.Count} event definitions");

            // Check which events should be active
            CheckEventSchedule();
        }

        private void RegisterEventDefinition(EventDefinition eventDef)
        {
            eventDefinitions[eventDef.eventId] = eventDef;

            // Initialize event challenges
            eventChallenges[eventDef.eventId] = GenerateEventChallenges(eventDef);
        }

        private List<EventChallenge> GenerateEventChallenges(EventDefinition eventDef)
        {
            var challenges = new List<EventChallenge>();

            // Generate challenges based on event type
            if (eventDef.eventType == EventType.Seasonal)
            {
                challenges.Add(new EventChallenge
                {
                    challengeId = $"{eventDef.eventId}_challenge_1",
                    title = "Event Participant",
                    description = "Complete 10 matches during the event",
                    requirement = new ChallengeRequirement { type = RequirementType.PlayMatches, targetValue = 10 },
                    rewards = new EventReward { eventCurrency = 500, xp = 1000 }
                });

                challenges.Add(new EventChallenge
                {
                    challengeId = $"{eventDef.eventId}_challenge_2",
                    title = "Event Master",
                    description = "Complete 50 matches during the event",
                    requirement = new ChallengeRequirement { type = RequirementType.PlayMatches, targetValue = 50 },
                    rewards = new EventReward { eventCurrency = 2500, xp = 5000, items = new List<string> { "cosmetic_event_badge" } }
                });

                challenges.Add(new EventChallenge
                {
                    challengeId = $"{eventDef.eventId}_challenge_3",
                    title = "Zombie Hunter",
                    description = "Kill 1000 zombies during the event",
                    requirement = new ChallengeRequirement { type = RequirementType.KillEnemies, targetValue = 1000 },
                    rewards = new EventReward { eventCurrency = 1000, xp = 2000 }
                });
            }
            else if (eventDef.eventType == EventType.Community)
            {
                challenges.Add(new EventChallenge
                {
                    challengeId = $"{eventDef.eventId}_challenge_community",
                    title = "Community Contributor",
                    description = "Contribute to the community goal",
                    requirement = new ChallengeRequirement { type = RequirementType.CommunityContribution, targetValue = 100 },
                    rewards = new EventReward { eventCurrency = 1000, xp = 2000 }
                });
            }

            return challenges;
        }

        #endregion

        #region Event Lifecycle

        private void CheckEventSchedule()
        {
            DateTime now = DateTime.UtcNow;

            foreach (var eventDef in eventDefinitions.Values)
            {
                bool shouldBeActive = now >= eventDef.startDate && now < eventDef.endDate;

                if (shouldBeActive && !activeEvents.ContainsKey(eventDef.eventId))
                {
                    StartEvent(eventDef);
                }
                else if (!shouldBeActive && activeEvents.ContainsKey(eventDef.eventId))
                {
                    EndEvent(eventDef.eventId);
                }
            }
        }

        private void StartEvent(EventDefinition eventDef)
        {
            var seasonalEvent = new SeasonalEvent
            {
                eventId = eventDef.eventId,
                definition = eventDef,
                startTime = DateTime.UtcNow,
                isActive = true,
                participantCount = 0,
                communityProgress = 0
            };

            activeEvents[eventDef.eventId] = seasonalEvent;

            OnEventStarted?.Invoke(eventDef.eventId, seasonalEvent);

            Debug.Log($"[SeasonalEventSystem] Event started: {eventDef.eventName}");

            // Notify all clients
            StartEventClientRpc(eventDef.eventId, eventDef.eventName, eventDef.description);
        }

        [ClientRpc]
        private void StartEventClientRpc(string eventId, string eventName, string description)
        {
            Debug.Log($"[SeasonalEventSystem] NEW EVENT: {eventName}\n{description}");
        }

        private void EndEvent(string eventId)
        {
            if (!activeEvents.ContainsKey(eventId)) return;

            var seasonalEvent = activeEvents[eventId];
            seasonalEvent.isActive = false;
            seasonalEvent.endTime = DateTime.UtcNow;

            // Award final rewards
            AwardEventCompletionRewards(eventId);

            activeEvents.Remove(eventId);

            OnEventEnded?.Invoke(eventId);

            Debug.Log($"[SeasonalEventSystem] Event ended: {eventId}");

            // Notify all clients
            EndEventClientRpc(eventId);
        }

        [ClientRpc]
        private void EndEventClientRpc(string eventId)
        {
            Debug.Log($"[SeasonalEventSystem] Event ended: {eventId}");
        }

        private void UpdateActiveEvents()
        {
            foreach (var seasonalEvent in activeEvents.Values)
            {
                // Update community events
                if (seasonalEvent.definition.eventType == EventType.Community)
                {
                    UpdateCommunityEventProgress(seasonalEvent);
                }
            }
        }

        #endregion

        #region Player Participation

        public void InitializePlayerData(ulong playerId)
        {
            if (playerEventData.ContainsKey(playerId)) return;

            playerEventData[playerId] = new PlayerEventData
            {
                playerId = playerId,
                activeEventProgress = new Dictionary<string, EventProgress>(),
                eventCurrencies = new Dictionary<string, int>(),
                completedChallenges = new List<string>()
            };

            // Add player to active events
            foreach (var eventId in activeEvents.Keys)
            {
                JoinEvent(playerId, eventId);
            }
        }

        public void JoinEvent(ulong playerId, string eventId)
        {
            if (!activeEvents.ContainsKey(eventId)) return;

            if (!playerEventData.ContainsKey(playerId))
            {
                InitializePlayerData(playerId);
            }

            var data = playerEventData[playerId];

            if (!data.activeEventProgress.ContainsKey(eventId))
            {
                data.activeEventProgress[eventId] = new EventProgress
                {
                    eventId = eventId,
                    joinedTime = DateTime.UtcNow,
                    challengeProgress = new Dictionary<string, float>()
                };

                activeEvents[eventId].participantCount++;

                Debug.Log($"[SeasonalEventSystem] Player {playerId} joined event {eventId}");
            }
        }

        #endregion

        #region Event Challenges

        public void UpdateEventProgress(ulong playerId, RequirementType type, float value)
        {
            if (!playerEventData.ContainsKey(playerId)) return;

            var data = playerEventData[playerId];

            // Update progress for all active event challenges
            foreach (var eventId in activeEvents.Keys)
            {
                if (!data.activeEventProgress.ContainsKey(eventId)) continue;

                var progress = data.activeEventProgress[eventId];
                var challenges = eventChallenges[eventId];

                foreach (var challenge in challenges)
                {
                    if (challenge.requirement.type != type) continue;

                    if (data.completedChallenges.Contains(challenge.challengeId)) continue;

                    // Update progress
                    if (!progress.challengeProgress.ContainsKey(challenge.challengeId))
                    {
                        progress.challengeProgress[challenge.challengeId] = 0f;
                    }

                    float currentProgress = progress.challengeProgress[challenge.challengeId];
                    float newProgress = Mathf.Min(currentProgress + value, challenge.requirement.targetValue);

                    progress.challengeProgress[challenge.challengeId] = newProgress;

                    // Check completion
                    if (newProgress >= challenge.requirement.targetValue)
                    {
                        CompleteEventChallenge(playerId, eventId, challenge);
                    }
                }

                // Update community progress
                if (activeEvents[eventId].definition.eventType == EventType.Community)
                {
                    activeEvents[eventId].communityProgress += (long)value;
                }
            }
        }

        private void CompleteEventChallenge(ulong playerId, string eventId, EventChallenge challenge)
        {
            var data = playerEventData[playerId];

            data.completedChallenges.Add(challenge.challengeId);

            // Award rewards
            AwardEventRewards(playerId, eventId, challenge.rewards);

            OnEventChallengeCompleted?.Invoke(playerId, eventId, challenge);

            Debug.Log($"[SeasonalEventSystem] Player {playerId} completed challenge: {challenge.title}");

            // Notify client
            CompleteEventChallengeClientRpc(playerId, challenge.title);
        }

        [ClientRpc]
        private void CompleteEventChallengeClientRpc(ulong playerId, string challengeTitle)
        {
            Debug.Log($"[SeasonalEventSystem] EVENT CHALLENGE COMPLETED: {challengeTitle}");
        }

        #endregion

        #region Community Events

        private void UpdateCommunityEventProgress(SeasonalEvent seasonalEvent)
        {
            var eventDef = seasonalEvent.definition;

            if (eventDef.communityGoal <= 0) return;

            float progressPercent = (float)seasonalEvent.communityProgress / eventDef.communityGoal;

            // Check milestones
            if (progressPercent >= 1f && !seasonalEvent.communityGoalReached)
            {
                seasonalEvent.communityGoalReached = true;
                OnCommunityGoalReached(seasonalEvent);
            }
        }

        private void OnCommunityGoalReached(SeasonalEvent seasonalEvent)
        {
            Debug.Log($"[SeasonalEventSystem] Community goal reached for {seasonalEvent.eventId}!");

            // Award all participants
            foreach (var data in playerEventData.Values)
            {
                if (data.activeEventProgress.ContainsKey(seasonalEvent.eventId))
                {
                    var rewards = new EventReward
                    {
                        eventCurrency = 5000,
                        xp = 10000,
                        items = new List<string> { "cosmetic_community_hero" }
                    };

                    AwardEventRewards(data.playerId, seasonalEvent.eventId, rewards);
                }
            }

            // Notify all clients
            CommunityGoalReachedClientRpc(seasonalEvent.eventId);
        }

        [ClientRpc]
        private void CommunityGoalReachedClientRpc(string eventId)
        {
            Debug.Log($"[SeasonalEventSystem] COMMUNITY GOAL REACHED!");
        }

        #endregion

        #region Rewards

        private void AwardEventRewards(ulong playerId, string eventId, EventReward rewards)
        {
            var data = playerEventData[playerId];
            var eventDef = eventDefinitions[eventId];

            // Event currency
            if (rewards.eventCurrency > 0)
            {
                if (!data.eventCurrencies.ContainsKey(eventDef.eventCurrency))
                {
                    data.eventCurrencies[eventDef.eventCurrency] = 0;
                }

                data.eventCurrencies[eventDef.eventCurrency] += rewards.eventCurrency;
            }

            // XP (with multiplier)
            if (rewards.xp > 0)
            {
                int xp = Mathf.RoundToInt(rewards.xp * eventDef.xpMultiplier);
                Progression.ProgressionManager.Instance?.AddExperience(playerId, xp);
            }

            // Soft currency (with multiplier)
            if (rewards.softCurrency > 0)
            {
                int currency = Mathf.RoundToInt(rewards.softCurrency * eventDef.rewardMultiplier);
                Economy.EconomyManager.Instance?.AddSoftCurrency(playerId, currency);
            }

            // Hard currency
            if (rewards.hardCurrency > 0)
            {
                Economy.EconomyManager.Instance?.AddHardCurrency(playerId, rewards.hardCurrency);
            }

            // Items
            foreach (var itemId in rewards.items)
            {
                Inventory.InventoryManager.Instance?.AddItem(playerId, itemId, 1);
            }
        }

        private void AwardEventCompletionRewards(string eventId)
        {
            var seasonalEvent = activeEvents[eventId];

            // Award participation rewards to all participants
            foreach (var data in playerEventData.Values)
            {
                if (data.activeEventProgress.ContainsKey(eventId))
                {
                    var rewards = new EventReward
                    {
                        eventCurrency = 1000,
                        xp = 2000,
                        softCurrency = 500
                    };

                    AwardEventRewards(data.playerId, eventId, rewards);
                }
            }
        }

        #endregion

        #region Event Store

        public bool PurchaseEventItem(ulong playerId, string eventId, string itemId, int cost)
        {
            if (!activeEvents.ContainsKey(eventId)) return false;

            if (!playerEventData.ContainsKey(playerId)) return false;

            var data = playerEventData[playerId];
            var eventDef = eventDefinitions[eventId];

            // Check if player has enough event currency
            if (!data.eventCurrencies.ContainsKey(eventDef.eventCurrency)) return false;

            if (data.eventCurrencies[eventDef.eventCurrency] < cost) return false;

            // Deduct currency
            data.eventCurrencies[eventDef.eventCurrency] -= cost;

            // Grant item
            Inventory.InventoryManager.Instance?.AddItem(playerId, itemId, 1);

            Debug.Log($"[SeasonalEventSystem] Player {playerId} purchased event item {itemId}");

            return true;
        }

        public int GetEventCurrency(ulong playerId, string currencyType)
        {
            if (!playerEventData.ContainsKey(playerId)) return 0;

            var data = playerEventData[playerId];

            return data.eventCurrencies.ContainsKey(currencyType) ? data.eventCurrencies[currencyType] : 0;
        }

        #endregion

        #region Public Getters

        public List<SeasonalEvent> GetActiveEvents()
        {
            return activeEvents.Values.ToList();
        }

        public List<EventChallenge> GetEventChallenges(string eventId)
        {
            return eventChallenges.ContainsKey(eventId) ? eventChallenges[eventId] : new List<EventChallenge>();
        }

        public EventProgress GetPlayerEventProgress(ulong playerId, string eventId)
        {
            if (!playerEventData.ContainsKey(playerId)) return null;

            var data = playerEventData[playerId];

            return data.activeEventProgress.ContainsKey(eventId) ? data.activeEventProgress[eventId] : null;
        }

        public bool IsEventActive(string eventId)
        {
            return activeEvents.ContainsKey(eventId);
        }

        #endregion
    }

    #region Data Classes

    [Serializable]
    public class EventDefinition
    {
        public string eventId;
        public string eventName;
        public string description;
        public EventType eventType;
        public DateTime startDate;
        public DateTime endDate;
        public float rewardMultiplier = 1f;
        public float xpMultiplier = 1f;
        public string eventCurrency;
        public List<string> specialModes = new List<string>();
        public List<string> eventStoreItems = new List<string>();
        public long communityGoal; // For community events
    }

    [Serializable]
    public class SeasonalEvent
    {
        public string eventId;
        public EventDefinition definition;
        public DateTime startTime;
        public DateTime endTime;
        public bool isActive;
        public int participantCount;
        public long communityProgress;
        public bool communityGoalReached;
    }

    [Serializable]
    public class EventChallenge
    {
        public string challengeId;
        public string title;
        public string description;
        public ChallengeRequirement requirement;
        public EventReward rewards;
    }

    [Serializable]
    public class ChallengeRequirement
    {
        public RequirementType type;
        public float targetValue;
    }

    [Serializable]
    public class EventReward
    {
        public int eventCurrency;
        public int xp;
        public int softCurrency;
        public int hardCurrency;
        public List<string> items = new List<string>();
    }

    [Serializable]
    public class PlayerEventData
    {
        public ulong playerId;
        public Dictionary<string, EventProgress> activeEventProgress = new Dictionary<string, EventProgress>();
        public Dictionary<string, int> eventCurrencies = new Dictionary<string, int>();
        public List<string> completedChallenges = new List<string>();
    }

    [Serializable]
    public class EventProgress
    {
        public string eventId;
        public DateTime joinedTime;
        public Dictionary<string, float> challengeProgress = new Dictionary<string, float>();
    }

    public enum EventType
    {
        Seasonal,       // Halloween, Christmas, etc.
        Community,      // Global community goals
        LimitedTime,    // Weekend events, double XP
        Competitive,    // Tournaments
        Special         // One-off special events
    }

    public enum RequirementType
    {
        PlayMatches,
        KillEnemies,
        CompleteExtractions,
        EarnCurrency,
        CommunityContribution
    }

    #endregion
}
