using UnityEngine;
using System;

namespace DeadFrontier.Zombies
{
    /// <summary>
    /// Handles zombie health and death
    /// </summary>
    public class ZombieHealth : MonoBehaviour, Core.IDamageable
    {
        [Header("Configuration")]
        [SerializeField] private ZombieConfig config;

        [Header("State")]
        [SerializeField] private float currentHealth;
        private bool isDead = false;

        // Events
        public event Action<float> OnHealthChanged;
        public event Action<float> OnDamaged;
        public event Action OnDeath;

        // Properties
        public float CurrentHealth => currentHealth;
        public float MaxHealth => config.maxHealth;
        public float HealthPercentage => currentHealth / config.maxHealth;
        public bool IsDead => isDead;
        public ZombieConfig Config => config;

        private void Awake()
        {
            if (config != null)
            {
                currentHealth = config.maxHealth;
            }
        }

        /// <summary>
        /// Sets the zombie configuration
        /// </summary>
        public void SetConfig(ZombieConfig newConfig)
        {
            config = newConfig;
            currentHealth = config.maxHealth;
        }

        /// <summary>
        /// Applies damage to the zombie
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (isDead || damage <= 0f)
                return;

            currentHealth -= damage;
            currentHealth = Mathf.Max(0f, currentHealth);

            Debug.Log($"[ZombieHealth] {config.zombieName} took {damage} damage. Health: {currentHealth}/{config.maxHealth}");

            OnHealthChanged?.Invoke(currentHealth);
            OnDamaged?.Invoke(damage);

            // Check if zombie should flee (low health behavior)
            if (currentHealth < config.maxHealth * 0.1f)
            {
                // TODO: Trigger flee state
            }

            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        /// <summary>
        /// Heals the zombie
        /// </summary>
        public void Heal(float amount)
        {
            if (isDead || amount <= 0f)
                return;

            currentHealth += amount;
            currentHealth = Mathf.Min(config.maxHealth, currentHealth);

            OnHealthChanged?.Invoke(currentHealth);
        }

        /// <summary>
        /// Kills the zombie
        /// </summary>
        private void Die()
        {
            if (isDead)
                return;

            isDead = true;

            Debug.Log($"[ZombieHealth] {config.zombieName} died");

            OnDeath?.Invoke();

            // Play death sound
            if (config.deathSounds != null && config.deathSounds.Length > 0)
            {
                AudioClip deathSound = config.deathSounds[UnityEngine.Random.Range(0, config.deathSounds.Length)];
                Core.AudioManager.Instance.Play(deathSound, transform.position);
            }

            // Check for explosion
            if (config.explodesOnDeath)
            {
                Explode();
            }

            // Drop loot
            DropLoot();

            // Disable AI
            var ai = GetComponent<ZombieAI>();
            if (ai != null)
            {
                ai.enabled = false;
            }

            // Disable NavMesh
            var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null)
            {
                agent.enabled = false;
            }

            // TODO: Play death animation
            // TODO: Enable ragdoll

            // Destroy after delay
            Destroy(gameObject, 5f);
        }

        /// <summary>
        /// Explodes, damaging nearby entities
        /// </summary>
        private void Explode()
        {
            Debug.Log($"[ZombieHealth] {config.zombieName} exploded!");

            // Find all colliders in explosion radius
            Collider[] colliders = Physics.OverlapSphere(transform.position, config.explosionRadius);

            foreach (var col in colliders)
            {
                // Apply damage to damageable objects
                var damageable = col.GetComponent<Core.IDamageable>();
                if (damageable != null)
                {
                    // Calculate damage based on distance
                    float distance = Vector3.Distance(transform.position, col.transform.position);
                    float damageFalloff = 1f - (distance / config.explosionRadius);
                    float damage = config.explosionDamage * damageFalloff;

                    damageable.TakeDamage(damage);
                }

                // Apply physics force
                var rb = col.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Vector3 direction = (col.transform.position - transform.position).normalized;
                    rb.AddForce(direction * 10f, ForceMode.Impulse);
                }
            }

            // TODO: Spawn explosion VFX
            // TODO: Play explosion sound
        }

        /// <summary>
        /// Drops loot on death
        /// </summary>
        private void DropLoot()
        {
            if (config.lootTable == null)
                return;

            var drops = config.lootTable.Roll();

            foreach (var item in drops)
            {
                // TODO: Spawn loot item at zombie position
                Debug.Log($"[ZombieHealth] Dropped {item.itemName}");
            }
        }

        /// <summary>
        /// Resets zombie to full health
        /// </summary>
        public void Reset()
        {
            currentHealth = config.maxHealth;
            isDead = false;
            OnHealthChanged?.Invoke(currentHealth);
        }

        private void OnDrawGizmosSelected()
        {
            if (config != null && config.explodesOnDeath)
            {
                // Draw explosion radius
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, config.explosionRadius);
            }
        }
    }
}
