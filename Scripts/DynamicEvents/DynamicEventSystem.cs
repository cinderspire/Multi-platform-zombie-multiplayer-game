using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.DynamicEvents
{
    /// <summary>
    /// Dynamic world event system for zombie multiplayer game.
    /// Spawns random events throughout the world to keep gameplay fresh.
    /// </summary>
    public class DynamicEventSystem : NetworkBehaviour
    {
        public static DynamicEventSystem Instance { get; private set; }

        [Header("Event Configuration")]
        [SerializeField] private int maxActiveEvents = 10;
        [SerializeField] private float eventSpawnCheckInterval = 300f;
        [SerializeField] private float eventDuration = 600f;

        // Data structures
        private Dictionary<string, DynamicEvent> activeEvents = new Dictionary<string, DynamicEvent>();
        private Dictionary<string, EventDefinition> eventDefinitions = new Dictionary<string, EventDefinition>();

        // Events
        public event Action<DynamicEvent> OnEventStarted;
        public event Action<string> OnEventCompleted;
        public event Action<string> OnEventFailed;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer)
            {
                InitializeEventDefinitions();
                InvokeRepeating(nameof(CheckEventSpawns), 0f, eventSpawnCheckInterval);
                InvokeRepeating(nameof(UpdateActiveEvents), 1f, 1f);
            }
        }

        private void InitializeEventDefinitions()
        {
            // Horde Events
            eventDefinitions["horde_small"] = new EventDefinition
            {
                eventId = "horde_small",
                eventName = "Zombie Horde",
                eventType = EventType.Horde,
                rarity = EventRarity.Common,
                duration = 300f,
                participants = new List<ulong>(),
                rewards = 1000
            };

            // Supply Drop Events
            eventDefinitions["supply_drop"] = new EventDefinition
            {
                eventId = "supply_drop",
                eventName = "Supply Drop",
                eventType = EventType.SupplyDrop,
                rarity = EventRarity.Uncommon,
                duration = 180f,
                participants = new List<ulong>(),
                rewards = 2500
            };

            // Boss Spawn Events
            eventDefinitions["field_boss"] = new EventDefinition
            {
                eventId = "field_boss",
                eventName = "Field Boss",
                eventType = EventType.BossSpawn,
                rarity = EventRarity.Rare,
                duration = 900f,
                participants = new List<ulong>(),
                rewards = 5000
            };

            // Weather Events
            eventDefinitions["acid_rain"] = new EventDefinition
            {
                eventId = "acid_rain",
                eventName = "Acid Rain",
                eventType = EventType.Weather,
                rarity = EventRarity.Uncommon,
                duration = 600f,
                participants = new List<ulong>(),
                rewards = 0
            };

            // PvP Events
            eventDefinitions["king_of_hill"] = new EventDefinition
            {
                eventId = "king_of_hill",
                eventName = "King of the Hill",
                eventType = EventType.PvP,
                rarity = EventRarity.Epic,
                duration = 600f,
                participants = new List<ulong>(),
                rewards = 10000
            };
        }

        private void CheckEventSpawns()
        {
            if (activeEvents.Count >= maxActiveEvents) return;

            // Random chance to spawn event
            if (UnityEngine.Random.value < 0.3f)
            {
                SpawnRandomEvent();
            }
        }

        private void SpawnRandomEvent()
        {
            var availableEvents = eventDefinitions.Values.ToList();
            var selectedEvent = availableEvents[UnityEngine.Random.Range(0, availableEvents.Count)];

            var dynamicEvent = new DynamicEvent
            {
                eventId = $"{selectedEvent.eventId}_{Guid.NewGuid()}",
                definition = selectedEvent,
                startTime = DateTime.UtcNow,
                endTime = DateTime.UtcNow.AddSeconds(selectedEvent.duration),
                status = EventStatus.Active,
                location = GetRandomLocation(),
                participants = new List<ulong>()
            };

            activeEvents[dynamicEvent.eventId] = dynamicEvent;
            OnEventStarted?.Invoke(dynamicEvent);
            BroadcastEventStartedClientRpc(dynamicEvent.eventId, selectedEvent.eventId);

            Debug.Log($"Dynamic event started: {selectedEvent.eventName}");
        }

        private Vector3 GetRandomLocation()
        {
            return new Vector3(
                UnityEngine.Random.Range(-1000f, 1000f),
                0f,
                UnityEngine.Random.Range(-1000f, 1000f)
            );
        }

        private void UpdateActiveEvents()
        {
            var toRemove = new List<string>();

            foreach (var ev in activeEvents.Values)
            {
                if (DateTime.UtcNow >= ev.endTime)
                {
                    CompleteEvent(ev);
                    toRemove.Add(ev.eventId);
                }
            }

            foreach (var eventId in toRemove)
            {
                activeEvents.Remove(eventId);
            }
        }

        private void CompleteEvent(DynamicEvent ev)
        {
            // Award rewards to participants
            foreach (var playerId in ev.participants)
            {
                Economy.EconomyManager.Instance?.AddSoftCurrency(playerId, ev.definition.rewards);
            }

            OnEventCompleted?.Invoke(ev.eventId);
            BroadcastEventCompletedClientRpc(ev.eventId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void JoinEventServerRpc(ulong playerId, string eventId, ServerRpcParams rpcParams = default)
        {
            if (!activeEvents.TryGetValue(eventId, out var ev)) return;
            if (!ev.participants.Contains(playerId))
            {
                ev.participants.Add(playerId);
            }
        }

        [ClientRpc]
        private void BroadcastEventStartedClientRpc(string eventId, string defId)
        {
            // Client notification
        }

        [ClientRpc]
        private void BroadcastEventCompletedClientRpc(string eventId)
        {
            // Client notification
        }

        public List<DynamicEvent> GetActiveEvents()
        {
            return activeEvents.Values.ToList();
        }
    }

    [Serializable]
    public class DynamicEvent
    {
        public string eventId;
        public EventDefinition definition;
        public DateTime startTime;
        public DateTime endTime;
        public EventStatus status;
        public Vector3 location;
        public List<ulong> participants;
    }

    [Serializable]
    public class EventDefinition
    {
        public string eventId;
        public string eventName;
        public EventType eventType;
        public EventRarity rarity;
        public float duration;
        public List<ulong> participants;
        public int rewards;
    }

    public enum EventType { Horde, SupplyDrop, BossSpawn, Weather, PvP, Treasure, Rescue }
    public enum EventRarity { Common, Uncommon, Rare, Epic, Legendary }
    public enum EventStatus { Pending, Active, Completed, Failed }
}
