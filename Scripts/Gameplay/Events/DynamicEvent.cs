using UnityEngine;
using Unity.Netcode;

namespace DeadFrontier.Gameplay.Events
{
    /// <summary>
    /// ScriptableObject defining a dynamic in-match event
    /// </summary>
    [CreateAssetMenu(fileName = "New Dynamic Event", menuName = "DeadFrontier/Dynamic Event")]
    public class DynamicEvent : ScriptableObject
    {
        [Header("Event Info")]
        public string eventName = "New Event";
        public string eventID;

        [TextArea(3, 5)]
        public string description = "Event description";

        public EventType eventType = EventType.ZombieHorde;
        public EventDifficulty difficulty = EventDifficulty.Medium;

        [Header("Timing")]
        [Tooltip("Earliest time (seconds) this event can trigger")]
        public float minTriggerTime = 60f;

        [Tooltip("Latest time (seconds) this event can trigger")]
        public float maxTriggerTime = 300f;

        [Tooltip("How long the event lasts")]
        public float duration = 30f;

        [Header("Trigger Conditions")]
        public bool requiresPlayers = true;
        public int minPlayersAlive = 1;
        public bool canRepeat = false;

        [Header("Event Parameters")]
        [Tooltip("Spawn location relative to players (for horde events)")]
        public float spawnDistanceMin = 30f;
        public float spawnDistanceMax = 50f;

        [Header("Zombie Horde Settings")]
        public int zombieCount = 20;
        public Zombies.ZombieType[] zombieTypes;
        public bool isEndlessHorde = false; // Spawns until event ends

        [Header("Supply Drop Settings")]
        public GameObject supplyDropPrefab;
        public Items.LootTable supplyDropLoot;

        [Header("Environmental Settings")]
        public bool affectsLighting = false;
        public Color fogColor = Color.gray;
        public float fogDensity = 0.05f;

        [Header("Audio")]
        public AudioClip eventStartSound;
        public AudioClip eventActiveSound; // Looping ambient sound
        public AudioClip eventEndSound;

        [Header("Rewards")]
        public int bonusXP = 50;
        public int bonusCurrency = 100;
        public float xpMultiplier = 1.0f; // XP multiplier during event

        /// <summary>
        /// Checks if event can trigger at the given time
        /// </summary>
        public bool CanTrigger(float matchTime)
        {
            return matchTime >= minTriggerTime && matchTime <= maxTriggerTime;
        }

        /// <summary>
        /// Gets a random spawn position around players
        /// </summary>
        public Vector3 GetRandomSpawnPosition(Vector3 referencePosition)
        {
            float distance = Random.Range(spawnDistanceMin, spawnDistanceMax);
            Vector2 randomCircle = Random.insideUnitCircle.normalized * distance;

            Vector3 spawnPos = referencePosition + new Vector3(randomCircle.x, 0f, randomCircle.y);

            // Try to find valid NavMesh position
            if (UnityEngine.AI.NavMesh.SamplePosition(spawnPos, out UnityEngine.AI.NavMeshHit hit, 20f, UnityEngine.AI.NavMesh.AllAreas))
            {
                return hit.position;
            }

            return spawnPos;
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(eventID))
            {
                eventID = System.Guid.NewGuid().ToString();
            }
        }
    }

    public enum EventType
    {
        ZombieHorde,        // Massive zombie spawn
        SupplyDrop,         // Airdrop with loot
        BloodMoon,          // Enhanced zombie difficulty
        FogRoll,            // Heavy fog reduces visibility
        PowerOutage,        // Disables extraction points temporarily
        DoubleXP,           // 2x XP for duration
        LootBonanza,        // High-tier loot spawns
        HunterZombies,      // Special fast zombies spawn
        BossZombie,         // Single powerful zombie
        Evacuation,         // Early extraction opportunity
        RadiationZone,      // Area becomes dangerous
        AmmoDrop            // Ammo crates spawn around map
    }

    public enum EventDifficulty
    {
        Easy,       // Low risk, low reward
        Medium,     // Balanced risk/reward
        Hard,       // High risk, high reward
        Extreme     // Very high risk, very high reward
    }
}
