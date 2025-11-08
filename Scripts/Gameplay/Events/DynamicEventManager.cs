using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

namespace DeadFrontier.Gameplay.Events
{
    /// <summary>
    /// Manages dynamic events during matches
    /// </summary>
    public class DynamicEventManager : NetworkBehaviour
    {
        [Header("Event Pool")]
        [SerializeField] private DynamicEvent[] availableEvents;

        [Header("Event Settings")]
        [SerializeField] private bool enableEvents = true;
        [SerializeField] private float eventCheckInterval = 30f; // Check for events every 30s
        [SerializeField] private int maxSimultaneousEvents = 2;
        [SerializeField, Range(0f, 1f)] private float eventTriggerChance = 0.3f; // 30% chance per check

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        // State
        private List<ActiveEvent> activeEvents = new List<ActiveEvent>();
        private List<string> triggeredEventIDs = new List<string>();
        private float matchTime = 0f;
        private float nextEventCheckTime = 0f;

        // Events
        public event System.Action<DynamicEvent> OnEventStarted;
        public event System.Action<DynamicEvent> OnEventEnded;

        // Singleton access
        private static DynamicEventManager instance;
        public static DynamicEventManager Instance => instance;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (!IsServer || !enableEvents)
                return;

            matchTime += Time.deltaTime;

            // Check for new events
            if (matchTime >= nextEventCheckTime)
            {
                CheckForNewEvent();
                nextEventCheckTime = matchTime + eventCheckInterval;
            }

            // Update active events
            UpdateActiveEvents();
        }

        #region Event Triggering

        private void CheckForNewEvent()
        {
            if (activeEvents.Count >= maxSimultaneousEvents)
                return;

            // Roll for event trigger
            if (Random.value > eventTriggerChance)
                return;

            // Get eligible events
            var eligibleEvents = GetEligibleEvents();

            if (eligibleEvents.Count == 0)
                return;

            // Select random event (weighted by difficulty)
            DynamicEvent selectedEvent = SelectRandomEvent(eligibleEvents);

            if (selectedEvent != null)
            {
                TriggerEvent(selectedEvent);
            }
        }

        private List<DynamicEvent> GetEligibleEvents()
        {
            List<DynamicEvent> eligible = new List<DynamicEvent>();

            foreach (var evt in availableEvents)
            {
                if (evt == null)
                    continue;

                // Check if already triggered (and can't repeat)
                if (!evt.canRepeat && triggeredEventIDs.Contains(evt.eventID))
                    continue;

                // Check timing
                if (!evt.CanTrigger(matchTime))
                    continue;

                // Check player requirements
                if (evt.requiresPlayers)
                {
                    int playersAlive = GetPlayersAlive();
                    if (playersAlive < evt.minPlayersAlive)
                        continue;
                }

                eligible.Add(evt);
            }

            return eligible;
        }

        private DynamicEvent SelectRandomEvent(List<DynamicEvent> events)
        {
            if (events.Count == 0)
                return null;

            // Weight by difficulty (easier events more common)
            float totalWeight = 0f;
            Dictionary<DynamicEvent, float> weights = new Dictionary<DynamicEvent, float>();

            foreach (var evt in events)
            {
                float weight = evt.difficulty switch
                {
                    EventDifficulty.Easy => 4f,
                    EventDifficulty.Medium => 3f,
                    EventDifficulty.Hard => 2f,
                    EventDifficulty.Extreme => 1f,
                    _ => 1f
                };

                weights[evt] = weight;
                totalWeight += weight;
            }

            // Random selection
            float randomValue = Random.Range(0f, totalWeight);
            float currentWeight = 0f;

            foreach (var kvp in weights)
            {
                currentWeight += kvp.Value;
                if (randomValue <= currentWeight)
                {
                    return kvp.Key;
                }
            }

            return events[0]; // Fallback
        }

        /// <summary>
        /// Manually triggers an event (can be called from external systems)
        /// </summary>
        public void TriggerEvent(DynamicEvent evt)
        {
            if (!IsServer || evt == null)
                return;

            if (showDebugLogs)
                Debug.Log($"[DynamicEventManager] Triggering event: {evt.eventName}");

            // Create active event
            ActiveEvent activeEvent = new ActiveEvent
            {
                eventData = evt,
                startTime = matchTime,
                endTime = matchTime + evt.duration,
                isActive = true
            };

            activeEvents.Add(activeEvent);

            // Mark as triggered
            if (!triggeredEventIDs.Contains(evt.eventID))
            {
                triggeredEventIDs.Add(evt.eventID);
            }

            // Execute event logic
            ExecuteEventStart(activeEvent);

            // Notify all clients
            NotifyEventStartClientRpc(evt.eventID);

            OnEventStarted?.Invoke(evt);
        }

        #endregion

        #region Event Execution

        private void ExecuteEventStart(ActiveEvent activeEvent)
        {
            DynamicEvent evt = activeEvent.eventData;

            switch (evt.eventType)
            {
                case EventType.ZombieHorde:
                    SpawnZombieHorde(activeEvent);
                    break;

                case EventType.SupplyDrop:
                    SpawnSupplyDrop(activeEvent);
                    break;

                case EventType.BloodMoon:
                    ActivateBloodMoon(activeEvent);
                    break;

                case EventType.FogRoll:
                    ActivateFog(activeEvent);
                    break;

                case EventType.PowerOutage:
                    DisableExtractionPoints(activeEvent);
                    break;

                case EventType.DoubleXP:
                    // XP multiplier handled by progression system
                    break;

                case EventType.LootBonanza:
                    SpawnHighTierLoot(activeEvent);
                    break;

                case EventType.BossZombie:
                    SpawnBossZombie(activeEvent);
                    break;

                case EventType.AmmoDrop:
                    SpawnAmmoCrates(activeEvent);
                    break;

                // ... more event types
            }

            // Play event start sound
            if (evt.eventStartSound != null)
            {
                PlaySoundClientRpc(evt.eventStartSound.name);
            }
        }

        private void UpdateActiveEvents()
        {
            for (int i = activeEvents.Count - 1; i >= 0; i--)
            {
                ActiveEvent evt = activeEvents[i];

                // Check if event should end
                if (matchTime >= evt.endTime)
                {
                    EndEvent(evt);
                    activeEvents.RemoveAt(i);
                }
                else
                {
                    // Update ongoing event
                    UpdateOngoingEvent(evt);
                }
            }
        }

        private void UpdateOngoingEvent(ActiveEvent activeEvent)
        {
            DynamicEvent evt = activeEvent.eventData;

            switch (evt.eventType)
            {
                case EventType.ZombieHorde:
                    if (evt.isEndlessHorde)
                    {
                        // Spawn more zombies periodically
                        if (Time.frameCount % 300 == 0) // Every 5 seconds
                        {
                            SpawnHordeWave(activeEvent, evt.zombieCount / 5);
                        }
                    }
                    break;

                // ... other ongoing event updates
            }
        }

        private void EndEvent(ActiveEvent activeEvent)
        {
            DynamicEvent evt = activeEvent.eventData;

            if (showDebugLogs)
                Debug.Log($"[DynamicEventManager] Event ended: {evt.eventName}");

            // Execute end logic
            switch (evt.eventType)
            {
                case EventType.BloodMoon:
                    DeactivateBloodMoon(activeEvent);
                    break;

                case EventType.FogRoll:
                    DeactivateFog(activeEvent);
                    break;

                case EventType.PowerOutage:
                    EnableExtractionPoints(activeEvent);
                    break;

                // ... more end logic
            }

            // Award completion rewards
            AwardEventRewards(evt);

            // Play end sound
            if (evt.eventEndSound != null)
            {
                PlaySoundClientRpc(evt.eventEndSound.name);
            }

            // Notify clients
            NotifyEventEndClientRpc(evt.eventID);

            OnEventEnded?.Invoke(evt);
        }

        #endregion

        #region Specific Event Implementations

        private void SpawnZombieHorde(ActiveEvent activeEvent)
        {
            DynamicEvent evt = activeEvent.eventData;

            // Get average player position
            Vector3 spawnCenter = GetAveragePlayerPosition();

            // Get spawn location
            Vector3 spawnPos = evt.GetRandomSpawnPosition(spawnCenter);

            if (showDebugLogs)
                Debug.Log($"[DynamicEventManager] Spawning horde of {evt.zombieCount} zombies at {spawnPos}");

            // Spawn zombies
            if (evt.zombieTypes != null && evt.zombieTypes.Length > 0)
            {
                // Spawn specific zombie types
                foreach (var zombieType in evt.zombieTypes)
                {
                    int count = evt.zombieCount / evt.zombieTypes.Length;
                    SpawnHordeWave(activeEvent, count, zombieType);
                }
            }
            else
            {
                // Spawn random zombies
                Zombies.ZombieManager.Instance?.SpawnHorde(spawnPos, evt.zombieCount, 30f);
            }

            // Show horde notification
            UI.HUD.KillFeed.Instance?.ShowHordeSpawn(spawnPos);
        }

        private void SpawnHordeWave(ActiveEvent activeEvent, int count, Zombies.ZombieType? specificType = null)
        {
            Vector3 spawnCenter = GetAveragePlayerPosition();
            Vector3 spawnPos = activeEvent.eventData.GetRandomSpawnPosition(spawnCenter);

            for (int i = 0; i < count; i++)
            {
                Zombies.ZombieType type = specificType ?? (Zombies.ZombieType)Random.Range(0, 5);
                Zombies.ZombieManager.Instance?.SpawnZombie(spawnPos, type);
            }
        }

        private void SpawnSupplyDrop(ActiveEvent activeEvent)
        {
            // Get random location on map
            Vector3 dropPos = GetRandomMapPosition();

            if (showDebugLogs)
                Debug.Log($"[DynamicEventManager] Spawning supply drop at {dropPos}");

            // Spawn supply drop prefab
            if (activeEvent.eventData.supplyDropPrefab != null)
            {
                GameObject drop = Instantiate(activeEvent.eventData.supplyDropPrefab, dropPos + Vector3.up * 50f, Quaternion.identity);

                // TODO: Add parachute/fall animation
                // TODO: Generate loot from loot table
            }

            // Notify players
            NotifySupplyDropClientRpc(dropPos);
        }

        private void SpawnBossZombie(ActiveEvent activeEvent)
        {
            Vector3 spawnCenter = GetAveragePlayerPosition();
            Vector3 spawnPos = activeEvent.eventData.GetRandomSpawnPosition(spawnCenter);

            if (showDebugLogs)
                Debug.Log($"[DynamicEventManager] Spawning boss zombie at {spawnPos}");

            // Spawn tank zombie as "boss"
            Zombies.ZombieManager.Instance?.SpawnZombie(spawnPos, Zombies.ZombieType.Tank);

            // TODO: Buff the zombie (more health, damage)
        }

        private void SpawnHighTierLoot(ActiveEvent activeEvent)
        {
            // Spawn high-tier loot items around the map
            for (int i = 0; i < 5; i++)
            {
                Vector3 lootPos = GetRandomMapPosition();

                // TODO: Spawn rare/epic items
                if (showDebugLogs)
                    Debug.Log($"[DynamicEventManager] Spawning high-tier loot at {lootPos}");
            }
        }

        private void SpawnAmmoCrates(ActiveEvent activeEvent)
        {
            // Spawn ammo crates near players
            for (int i = 0; i < 3; i++)
            {
                Vector3 cratePos = GetRandomMapPosition();

                // TODO: Spawn ammo crate
                if (showDebugLogs)
                    Debug.Log($"[DynamicEventManager] Spawning ammo crate at {cratePos}");
            }
        }

        private void ActivateBloodMoon(ActiveEvent activeEvent)
        {
            if (showDebugLogs)
                Debug.Log("[DynamicEventManager] Blood moon rising...");

            // TODO: Change lighting to red
            // TODO: Increase zombie aggression
            // TODO: Buff zombie stats
        }

        private void DeactivateBloodMoon(ActiveEvent activeEvent)
        {
            // Restore normal lighting and zombie behavior
        }

        private void ActivateFog(ActiveEvent activeEvent)
        {
            DynamicEvent evt = activeEvent.eventData;

            if (showDebugLogs)
                Debug.Log("[DynamicEventManager] Heavy fog rolling in...");

            // TODO: Enable fog with custom settings
            // RenderSettings.fog = true;
            // RenderSettings.fogColor = evt.fogColor;
            // RenderSettings.fogDensity = evt.fogDensity;
        }

        private void DeactivateFog(ActiveEvent activeEvent)
        {
            // Restore normal fog settings
            // RenderSettings.fog = false;
        }

        private void DisableExtractionPoints(ActiveEvent activeEvent)
        {
            var extractions = FindObjectsOfType<ExtractionPoint>();

            foreach (var extraction in extractions)
            {
                extraction.DeactivateExtractionPoint();
            }

            if (showDebugLogs)
                Debug.Log("[DynamicEventManager] Extraction points disabled!");
        }

        private void EnableExtractionPoints(ActiveEvent activeEvent)
        {
            var extractions = FindObjectsOfType<ExtractionPoint>();

            foreach (var extraction in extractions)
            {
                extraction.ActivateExtractionPoint();
            }

            if (showDebugLogs)
                Debug.Log("[DynamicEventManager] Extraction points reactivated!");
        }

        #endregion

        #region Rewards

        private void AwardEventRewards(DynamicEvent evt)
        {
            // Award rewards to all surviving players
            var players = FindObjectsOfType<Player.PlayerProgression>();

            foreach (var player in players)
            {
                if (evt.bonusXP > 0)
                {
                    player.AwardXP(evt.bonusXP, $"Event: {evt.eventName}");
                }

                // TODO: Award currency
                // TODO: Award items
            }

            if (showDebugLogs)
                Debug.Log($"[DynamicEventManager] Awarded event rewards: {evt.bonusXP} XP");
        }

        #endregion

        #region RPCs

        [ClientRpc]
        private void NotifyEventStartClientRpc(string eventID)
        {
            DynamicEvent evt = availableEvents.FirstOrDefault(e => e.eventID == eventID);

            if (evt != null)
            {
                Debug.Log($"[DynamicEventManager] Event started: {evt.eventName}");

                // Show UI notification
                // TODO: Show event banner/notification
            }
        }

        [ClientRpc]
        private void NotifyEventEndClientRpc(string eventID)
        {
            DynamicEvent evt = availableEvents.FirstOrDefault(e => e.eventID == eventID);

            if (evt != null)
            {
                Debug.Log($"[DynamicEventManager] Event ended: {evt.eventName}");
            }
        }

        [ClientRpc]
        private void NotifySupplyDropClientRpc(Vector3 location)
        {
            Debug.Log($"[DynamicEventManager] Supply drop incoming at {location}!");

            // Show supply drop marker on minimap
            // Play incoming aircraft sound
        }

        [ClientRpc]
        private void PlaySoundClientRpc(string soundName)
        {
            // Play sound by name
            // TODO: Implement
        }

        #endregion

        #region Helper Methods

        private Vector3 GetAveragePlayerPosition()
        {
            var players = FindObjectsOfType<Networking.NetworkedPlayer>();

            if (players.Length == 0)
                return Vector3.zero;

            Vector3 sum = Vector3.zero;
            foreach (var player in players)
            {
                sum += player.transform.position;
            }

            return sum / players.Length;
        }

        private Vector3 GetRandomMapPosition()
        {
            // TODO: Get random position within map bounds
            // For now, random position around origin
            Vector2 random = Random.insideUnitCircle * 100f;
            return new Vector3(random.x, 0f, random.y);
        }

        private int GetPlayersAlive()
        {
            var players = FindObjectsOfType<Networking.NetworkedPlayer>();
            return players.Length;
        }

        /// <summary>
        /// Gets current XP multiplier from active events
        /// </summary>
        public float GetCurrentXPMultiplier()
        {
            float multiplier = 1f;

            foreach (var evt in activeEvents)
            {
                if (evt.eventData.eventType == EventType.DoubleXP || evt.eventData.xpMultiplier > 1f)
                {
                    multiplier *= evt.eventData.xpMultiplier;
                }
            }

            return multiplier;
        }

        #endregion
    }

    /// <summary>
    /// Represents an active event instance
    /// </summary>
    [System.Serializable]
    public class ActiveEvent
    {
        public DynamicEvent eventData;
        public float startTime;
        public float endTime;
        public bool isActive;

        public float GetProgress(float currentTime)
        {
            float duration = endTime - startTime;
            float elapsed = currentTime - startTime;
            return Mathf.Clamp01(elapsed / duration);
        }

        public float GetRemainingTime(float currentTime)
        {
            return Mathf.Max(0f, endTime - currentTime);
        }
    }
}
