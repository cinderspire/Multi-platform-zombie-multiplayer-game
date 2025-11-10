using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Events
{
    public class EventSystem : NetworkBehaviour
    {
        public static EventSystem Instance { get; private set; }

        private Dictionary<string, GameEvent> eventDatabase = new Dictionary<string, GameEvent>();
        private Dictionary<string, GameEvent> activeEvents = new Dictionary<string, GameEvent>();
        private Dictionary<ulong, PlayerEventData> playerEventData = new Dictionary<ulong, PlayerEventData>();

        public event Action<string> OnEventStarted;
        public event Action<string> OnEventEnded;
        public event Action<ulong, string> OnEventCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer) InitializeEvents();
        }

        private void InitializeEvents()
        {
            // WEEKEND EVENTS
            eventDatabase["weekend_double_xp"] = new GameEvent
            {
                eventId = "weekend_double_xp",
                eventName = "Double XP Weekend",
                description = "Earn 2x XP from all sources",
                eventType = EventType.Bonus,
                bonuses = new EventBonuses { xpMultiplier = 2f },
                recurrence = EventRecurrence.Weekly,
                duration = 172800 // 48 hours
            };

            eventDatabase["weekend_currency"] = new GameEvent
            {
                eventId = "weekend_currency",
                eventName = "Currency Bonanza",
                description = "Earn 2x currency from all sources",
                eventType = EventType.Bonus,
                bonuses = new EventBonuses { currencyMultiplier = 2f },
                recurrence = EventRecurrence.Weekly,
                duration = 172800
            };

            // SEASONAL EVENTS
            eventDatabase["halloween"] = new GameEvent
            {
                eventId = "halloween",
                eventName = "Halloween Horror",
                description = "Special Halloween event with exclusive rewards",
                eventType = EventType.Seasonal,
                objectives = new List<EventObjective>
                {
                    new EventObjective { objectiveId = "halloween_kills", description = "Kill 1000 zombies", type = EventObjectiveType.Kill, requiredAmount = 1000, rewardXP = 5000, rewardCurrency = 2000 },
                    new EventObjective { objectiveId = "halloween_boss", description = "Defeat Halloween Boss", type = EventObjectiveType.BossKill, requiredAmount = 1, rewardXP = 10000, rewardItems = new List<string> { "skin_halloween_exclusive" } }
                },
                startDate = new DateTime(2024, 10, 25),
                endDate = new DateTime(2024, 11, 2),
                rewards = new EventRewards { cosmetics = new List<string> { "skin_halloween_2024", "emote_spooky" }, currency = 5000 }
            };

            eventDatabase["christmas"] = new GameEvent
            {
                eventId = "christmas",
                eventName = "Winter Survival",
                description = "Survive the winter apocalypse",
                eventType = EventType.Seasonal,
                objectives = new List<EventObjective>
                {
                    new EventObjective { objectiveId = "winter_survive", description = "Survive 10 winter nights", type = EventObjectiveType.Survive, requiredAmount = 10, rewardXP = 8000, rewardCurrency = 3000 },
                    new EventObjective { objectiveId = "winter_gifts", description = "Collect 50 supply drops", type = EventObjectiveType.Collect, requiredAmount = 50, rewardItems = new List<string> { "crate_winter_2024" } }
                },
                startDate = new DateTime(2024, 12, 15),
                endDate = new DateTime(2025, 1, 5),
                rewards = new EventRewards { cosmetics = new List<string> { "skin_santa_survivor", "weapon_skin_candy_cane" }, currency = 10000 }
            };

            // LIMITED TIME EVENTS
            eventDatabase["blood_moon"] = new GameEvent
            {
                eventId = "blood_moon",
                eventName = "Blood Moon Rising",
                description = "Extra difficult zombies spawn during blood moon",
                eventType = EventType.Challenge,
                bonuses = new EventBonuses { enemyDamageMultiplier = 1.5f, enemyHealthMultiplier = 2f, lootMultiplier = 3f },
                objectives = new List<EventObjective>
                {
                    new EventObjective { objectiveId = "blood_moon_survive", description = "Survive the blood moon", type = EventObjectiveType.Survive, requiredAmount = 1, rewardXP = 5000, rewardCurrency = 2000, rewardItems = new List<string> { "title_blood_moon_survivor" } }
                },
                duration = 3600, // 1 hour
                recurrence = EventRecurrence.Daily
            };

            eventDatabase["horde_invasion"] = new GameEvent
            {
                eventId = "horde_invasion",
                eventName = "Horde Invasion",
                description = "Massive zombie hordes attack",
                eventType = EventType.Challenge,
                bonuses = new EventBonuses { spawnRateMultiplier = 3f, lootMultiplier = 2f },
                objectives = new List<EventObjective>
                {
                    new EventObjective { objectiveId = "horde_kills", description = "Kill 500 zombies", type = EventObjectiveType.Kill, requiredAmount = 500, rewardXP = 10000, rewardCurrency = 5000 }
                },
                duration = 7200, // 2 hours
                recurrence = EventRecurrence.Weekly
            };

            eventDatabase["supply_drop"] = new GameEvent
            {
                eventId = "supply_drop",
                eventName = "Supply Drop Event",
                description = "High-quality loot drops across the map",
                eventType = EventType.Bonus,
                bonuses = new EventBonuses { lootMultiplier = 5f },
                objectives = new List<EventObjective>
                {
                    new EventObjective { objectiveId = "supply_collect", description = "Collect 10 supply drops", type = EventObjectiveType.Collect, requiredAmount = 10, rewardXP = 3000, rewardCurrency = 1500 }
                },
                duration = 1800, // 30 minutes
                recurrence = EventRecurrence.Daily
            };

            Debug.Log($"Initialized {eventDatabase.Count} events");
        }

        [ServerRpc(RequireOwnership = false)]
        public void StartEventServerRpc(string eventId, ServerRpcParams rpcParams = default)
        {
            if (!eventDatabase.TryGetValue(eventId, out var gameEvent)) return;
            if (activeEvents.ContainsKey(eventId)) return;

            gameEvent.currentlyActive = true;
            gameEvent.activeStartTime = DateTime.UtcNow;
            activeEvents[eventId] = gameEvent;

            OnEventStarted?.Invoke(eventId);
            NotifyEventStartedClientRpc(eventId, gameEvent.eventName);

            Debug.Log($"Event started: {gameEvent.eventName}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void CompleteEventObjectiveServerRpc(ulong playerId, string eventId, string objectiveId, int progress, ServerRpcParams rpcParams = default)
        {
            if (!activeEvents.TryGetValue(eventId, out var gameEvent)) return;

            if (!playerEventData.ContainsKey(playerId))
            {
                playerEventData[playerId] = new PlayerEventData { playerId = playerId, completedEvents = new List<string>(), objectiveProgress = new Dictionary<string, int>() };
            }

            var data = playerEventData[playerId];
            string progressKey = $"{eventId}_{objectiveId}";

            if (!data.objectiveProgress.ContainsKey(progressKey))
            {
                data.objectiveProgress[progressKey] = 0;
            }

            data.objectiveProgress[progressKey] += progress;

            // Check completion
            var objective = gameEvent.objectives.FirstOrDefault(o => o.objectiveId == objectiveId);
            if (objective != null && data.objectiveProgress[progressKey] >= objective.requiredAmount)
            {
                GrantEventRewards(playerId, objective);
            }

            // Check if all objectives completed
            if (gameEvent.objectives.All(o => data.objectiveProgress.GetValueOrDefault($"{eventId}_{o.objectiveId}", 0) >= o.requiredAmount))
            {
                if (!data.completedEvents.Contains(eventId))
                {
                    data.completedEvents.Add(eventId);
                    GrantEventRewards(playerId, gameEvent.rewards);
                    OnEventCompleted?.Invoke(playerId, eventId);
                }
            }
        }

        private void GrantEventRewards(ulong playerId, EventObjective objective)
        {
            if (objective.rewardXP > 0)
            {
                Progression.ProgressionSystem.Instance?.AddExperienceServerRpc(playerId, objective.rewardXP, "event");
            }

            if (objective.rewardCurrency > 0)
            {
                Economy.EconomyManager.Instance?.AddCurrencyServerRpc(playerId, Economy.CurrencyType.Soft, objective.rewardCurrency);
            }

            foreach (var itemId in objective.rewardItems)
            {
                Inventory.InventorySystem.Instance?.AddItemServerRpc(playerId, itemId, 1, Inventory.ContainerType.Backpack);
            }
        }

        private void GrantEventRewards(ulong playerId, EventRewards rewards)
        {
            if (rewards == null) return;

            if (rewards.currency > 0)
            {
                Economy.EconomyManager.Instance?.AddCurrencyServerRpc(playerId, Economy.CurrencyType.Soft, rewards.currency);
            }

            foreach (var cosmeticId in rewards.cosmetics)
            {
                Cosmetics.CosmeticSystem.Instance?.UnlockCosmeticServerRpc(playerId, cosmeticId);
            }
        }

        [ClientRpc]
        private void NotifyEventStartedClientRpc(string eventId, string eventName) { }

        public GameEvent GetEvent(string eventId) => eventDatabase.GetValueOrDefault(eventId);
        public List<GameEvent> GetActiveEvents() => activeEvents.Values.ToList();
        public PlayerEventData GetPlayerEventData(ulong playerId) => playerEventData.GetValueOrDefault(playerId);
    }

    [Serializable]
    public class GameEvent
    {
        public string eventId;
        public string eventName;
        public string description;
        public EventType eventType;
        public EventBonuses bonuses;
        public List<EventObjective> objectives;
        public EventRewards rewards;
        public DateTime startDate;
        public DateTime endDate;
        public int duration;
        public EventRecurrence recurrence;
        public bool currentlyActive;
        public DateTime activeStartTime;
    }

    [Serializable]
    public class EventBonuses
    {
        public float xpMultiplier = 1f;
        public float currencyMultiplier = 1f;
        public float lootMultiplier = 1f;
        public float spawnRateMultiplier = 1f;
        public float enemyDamageMultiplier = 1f;
        public float enemyHealthMultiplier = 1f;
    }

    [Serializable]
    public class EventObjective
    {
        public string objectiveId;
        public string description;
        public EventObjectiveType type;
        public int requiredAmount;
        public int rewardXP;
        public int rewardCurrency;
        public List<string> rewardItems;
    }

    [Serializable]
    public class EventRewards
    {
        public List<string> cosmetics;
        public int currency;
    }

    [Serializable]
    public class PlayerEventData
    {
        public ulong playerId;
        public List<string> completedEvents;
        public Dictionary<string, int> objectiveProgress;
    }

    public enum EventType { Bonus, Challenge, Seasonal, Limited }
    public enum EventRecurrence { None, Daily, Weekly, Monthly }
    public enum EventObjectiveType { Kill, BossKill, Survive, Collect, Craft, PartyMission }
}
