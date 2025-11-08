using UnityEngine;

namespace DeadFrontier.Zombies
{
    /// <summary>
    /// ScriptableObject that defines zombie statistics and behavior
    /// </summary>
    [CreateAssetMenu(fileName = "New Zombie Config", menuName = "DeadFrontier/Zombie Config")]
    public class ZombieConfig : ScriptableObject
    {
        [Header("Basic Info")]
        public string zombieName = "Walker";
        public ZombieType zombieType = ZombieType.Walker;

        [Header("Stats")]
        [Tooltip("Maximum health points")]
        public int maxHealth = 100;

        [Tooltip("Base movement speed (m/s)")]
        public float moveSpeed = 3f;

        [Tooltip("Speed when chasing player (m/s)")]
        public float chaseSpeed = 5f;

        [Tooltip("Damage per attack")]
        public int damage = 15;

        [Tooltip("Range at which zombie can attack (meters)")]
        public float attackRange = 2f;

        [Tooltip("Time between attacks (seconds)")]
        public float attackCooldown = 1.5f;

        [Header("Senses")]
        [Tooltip("How far zombie can see (meters)")]
        public float visionRange = Core.Constants.ZOMBIE_VISION_RANGE;

        [Tooltip("Field of view angle (degrees)")]
        public float visionAngle = 180f;

        [Tooltip("How far zombie can hear sounds (meters)")]
        public float hearingRange = Core.Constants.ZOMBIE_HEARING_RANGE;

        [Tooltip("How long to remember last known position (seconds)")]
        public float memoryDuration = Core.Constants.ZOMBIE_MEMORY_DURATION;

        [Header("Behavior")]
        [Tooltip("Minimum idle time before wandering")]
        public float minIdleTime = 2f;

        [Tooltip("Maximum idle time before wandering")]
        public float maxIdleTime = 5f;

        [Tooltip("Distance to wander when patrolling (meters)")]
        public float wanderRadius = 10f;

        [Tooltip("Should this zombie call for help?")]
        public bool canCallHorde = false;

        [Tooltip("Radius to call nearby zombies (meters)")]
        public float callHordeRadius = 20f;

        [Header("Loot")]
        [Tooltip("Experience points awarded for killing this zombie")]
        public int xpReward = 10;

        [Tooltip("Loot table for this zombie type")]
        public Items.LootTable lootTable;

        [Header("Visuals")]
        [Tooltip("Prefab for this zombie")]
        public GameObject modelPrefab;

        [Tooltip("Animator controller for animations")]
        public RuntimeAnimatorController animatorController;

        [Header("Audio")]
        public AudioClip[] idleSounds;
        public AudioClip[] chaseSounds;
        public AudioClip[] attackSounds;
        public AudioClip[] deathSounds;

        [Header("Special Abilities")]
        [Tooltip("Does this zombie explode on death?")]
        public bool explodesOnDeath = false;

        [Tooltip("Explosion damage")]
        public int explosionDamage = 50;

        [Tooltip("Explosion radius (meters)")]
        public float explosionRadius = 5f;

        [Tooltip("Can this zombie leap at players?")]
        public bool canLeap = false;

        [Tooltip("Leap distance (meters)")]
        public float leapDistance = 5f;

        [Tooltip("Leap cooldown (seconds)")]
        public float leapCooldown = 10f;

        /// <summary>
        /// Validates zombie configuration
        /// </summary>
        private void OnValidate()
        {
            // Ensure all values are within reasonable ranges
            maxHealth = Mathf.Max(1, maxHealth);
            moveSpeed = Mathf.Max(0.1f, moveSpeed);
            chaseSpeed = Mathf.Max(moveSpeed, chaseSpeed);
            damage = Mathf.Max(1, damage);
            attackRange = Mathf.Max(0.5f, attackRange);
            attackCooldown = Mathf.Max(0.1f, attackCooldown);
            visionRange = Mathf.Max(1f, visionRange);
            visionAngle = Mathf.Clamp(visionAngle, 0f, 360f);
            hearingRange = Mathf.Max(1f, hearingRange);
            memoryDuration = Mathf.Max(0f, memoryDuration);
            wanderRadius = Mathf.Max(1f, wanderRadius);
            callHordeRadius = Mathf.Max(1f, callHordeRadius);
            xpReward = Mathf.Max(0, xpReward);
        }
    }

    /// <summary>
    /// Types of zombies in the game
    /// </summary>
    public enum ZombieType
    {
        Walker,     // Standard zombie
        Runner,     // Fast, low HP
        Tank,       // Slow, high HP
        Exploder,   // Explodes on death
        Screamer    // Calls hordes
    }
}

namespace DeadFrontier.Items
{
    /// <summary>
    /// Defines loot drop tables
    /// </summary>
    [CreateAssetMenu(fileName = "New Loot Table", menuName = "DeadFrontier/Loot Table")]
    public class LootTable : ScriptableObject
    {
        [System.Serializable]
        public class LootEntry
        {
            public ItemData item;

            [Range(0f, 100f)]
            public float dropChance = 50f;

            public int minQuantity = 1;
            public int maxQuantity = 1;
        }

        public LootEntry[] loot;

        /// <summary>
        /// Rolls for loot drops
        /// </summary>
        public System.Collections.Generic.List<ItemData> Roll()
        {
            var drops = new System.Collections.Generic.List<ItemData>();

            foreach (var entry in loot)
            {
                if (Random.Range(0f, 100f) <= entry.dropChance)
                {
                    int quantity = Random.Range(entry.minQuantity, entry.maxQuantity + 1);
                    for (int i = 0; i < quantity; i++)
                    {
                        drops.Add(entry.item);
                    }
                }
            }

            return drops;
        }
    }

    /// <summary>
    /// Base item data (placeholder for now)
    /// </summary>
    [CreateAssetMenu(fileName = "New Item", menuName = "DeadFrontier/Item")]
    public class ItemData : ScriptableObject
    {
        public string itemName;
        public Sprite icon;
        public GameObject prefab;
    }
}
