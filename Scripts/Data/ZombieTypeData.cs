using UnityEngine;

namespace DeadFrontier.AI
{
    /// <summary>
    /// ScriptableObject defining zombie type characteristics, behavior, and stats.
    /// </summary>
    [CreateAssetMenu(fileName = "New Zombie Type", menuName = "Dead Frontier/AI/Zombie Type")]
    public class ZombieTypeData : ScriptableObject
    {
        [Header("Basic Info")]
        public string zombieId;
        public string zombieName;
        [TextArea(3, 5)]
        public string description;
        public ZombieCategory category = ZombieCategory.Common;

        [Header("Prefab")]
        public GameObject zombiePrefab;

        [Header("Stats")]
        [Range(10f, 10000f)]
        public float health = 100f;
        [Range(1f, 20f)]
        public float moveSpeed = 3f;
        [Range(5f, 100f)]
        public float damage = 20f;
        [Range(1f, 50f)]
        public float detectionRange = 15f;
        [Range(1f, 10f)]
        public float attackRange = 2f;
        [Range(0.5f, 5f)]
        public float attackCooldown = 1.5f;

        [Header("Behavior")]
        public ZombieBehaviorType behaviorType = ZombieBehaviorType.Aggressive;
        public bool canSprint;
        public bool canClimb;
        public bool canJump;
        public bool avoidsLight;
        public bool groupBehavior;
        public float hearingRange = 30f;

        [Header("Special Abilities")]
        public ZombieAbility[] specialAbilities;

        [Header("Loot")]
        public LootDropTable lootTable;
        public int baseXPReward = 10;
        public int softCurrencyReward = 5;

        [Header("Spawn Settings")]
        [Range(0f, 1f)]
        public float spawnWeight = 0.5f; // Weight in spawn selection
        public int minDifficultyLevel = 1;
        public bool canSpawnInHordes = true;
        public bool isBossZombie;

        [Header("Audio")]
        public AudioClip[] idleSounds;
        public AudioClip[] chaseSounds;
        public AudioClip[] attackSounds;
        public AudioClip[] hurtSounds;
        public AudioClip[] deathSounds;

        [Header("Visuals")]
        public Material zombieMaterial;
        public ParticleSystem deathVFX;
        public float ragdollForce = 5f;
    }

    [System.Serializable]
    public class ZombieAbility
    {
        public string abilityName;
        public ZombieAbilityType abilityType;
        public float cooldown = 10f;
        public float range = 5f;
        public float damage;
        public float duration;
        [TextArea(2, 3)]
        public string description;
    }

    [System.Serializable]
    public class LootDropTable
    {
        public LootDrop[] drops;
    }

    [System.Serializable]
    public class LootDrop
    {
        public string itemId;
        [Range(0f, 1f)]
        public float dropChance = 0.1f;
        public int minQuantity = 1;
        public int maxQuantity = 1;
    }

    public enum ZombieCategory
    {
        Common,     // Basic zombies
        Special,    // Special infected (fast, tank, spitter, etc.)
        Elite,      // Tougher variants
        Boss        // Boss zombies
    }

    public enum ZombieBehaviorType
    {
        Aggressive,  // Always attack
        Defensive,   // Only attack when provoked
        Ambush,      // Wait and ambush
        Patrol,      // Patrol area
        Horde        // Swarm behavior
    }

    public enum ZombieAbilityType
    {
        Leap,           // Jump at target
        Spit,           // Ranged acid/poison
        Explode,        // Explode on death/proximity
        Scream,         // Call reinforcements
        Regenerate,     // Heal over time
        Armor,          // Damage reduction
        Frenzy,         // Increased speed/damage when low health
        Grab            // Immobilize player
    }
}
