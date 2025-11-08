using UnityEngine;
using System;

namespace DeadFrontier.Player
{
    /// <summary>
    /// Handles player health, damage, and death
    /// </summary>
    public class PlayerHealth : MonoBehaviour, Core.IDamageable
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = Core.Constants.PLAYER_MAX_HEALTH;
        [SerializeField] private float currentHealth;

        [Header("Damage Feedback")]
        [SerializeField] private float damageFlashDuration = 0.5f;
        [SerializeField] private float lowHealthThreshold = 30f;

        [Header("Regeneration (Optional)")]
        [SerializeField] private bool enableHealthRegen = false;
        [SerializeField] private float regenRate = 5f; // HP per second
        [SerializeField] private float regenDelay = 5f; // Delay after taking damage

        // State
        private bool isDead = false;
        private float timeSinceLastDamage;

        // Events
        public event Action<float> OnHealthChanged;
        public event Action<float, float> OnDamaged; // damage amount, current health
        public event Action OnDeath;
        public event Action OnRevived;

        // Properties
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public float HealthPercentage => currentHealth / maxHealth;
        public bool IsDead => isDead;
        public bool IsLowHealth => currentHealth <= lowHealthThreshold;

        private void Awake()
        {
            currentHealth = maxHealth;
        }

        private void Update()
        {
            if (enableHealthRegen && !isDead)
            {
                UpdateHealthRegeneration();
            }
        }

        /// <summary>
        /// Applies damage to the player
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (isDead || damage <= 0f)
                return;

            currentHealth -= damage;
            currentHealth = Mathf.Max(0f, currentHealth);
            timeSinceLastDamage = 0f;

            Debug.Log($"[PlayerHealth] Took {damage} damage. Health: {currentHealth}/{maxHealth}");

            OnHealthChanged?.Invoke(currentHealth);
            OnDamaged?.Invoke(damage, currentHealth);

            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        /// <summary>
        /// Heals the player
        /// </summary>
        public void Heal(float amount)
        {
            if (isDead || amount <= 0f)
                return;

            currentHealth += amount;
            currentHealth = Mathf.Min(maxHealth, currentHealth);

            Debug.Log($"[PlayerHealth] Healed {amount}. Health: {currentHealth}/{maxHealth}");

            OnHealthChanged?.Invoke(currentHealth);
        }

        /// <summary>
        /// Kills the player instantly
        /// </summary>
        public void Die()
        {
            if (isDead)
                return;

            isDead = true;
            currentHealth = 0f;

            Debug.Log("[PlayerHealth] Player died");

            OnDeath?.Invoke();

            // Notify GameManager
            Core.GameManager.Instance?.PlayerDied(GetPlayerId());

            // Disable player controls
            DisablePlayer();
        }

        /// <summary>
        /// Revives the player
        /// </summary>
        public void Revive(float healthPercentage = 1f)
        {
            if (!isDead)
                return;

            isDead = false;
            currentHealth = maxHealth * Mathf.Clamp01(healthPercentage);

            Debug.Log($"[PlayerHealth] Player revived with {currentHealth} health");

            OnRevived?.Invoke();
            OnHealthChanged?.Invoke(currentHealth);

            // Re-enable player controls
            EnablePlayer();
        }

        /// <summary>
        /// Resets health to maximum
        /// </summary>
        public void ResetHealth()
        {
            currentHealth = maxHealth;
            isDead = false;
            timeSinceLastDamage = 0f;

            OnHealthChanged?.Invoke(currentHealth);
        }

        /// <summary>
        /// Sets maximum health
        /// </summary>
        public void SetMaxHealth(float newMaxHealth)
        {
            float healthPercentage = HealthPercentage;
            maxHealth = Mathf.Max(1f, newMaxHealth);
            currentHealth = maxHealth * healthPercentage;

            OnHealthChanged?.Invoke(currentHealth);
        }

        private void UpdateHealthRegeneration()
        {
            timeSinceLastDamage += Time.deltaTime;

            if (timeSinceLastDamage >= regenDelay && currentHealth < maxHealth)
            {
                Heal(regenRate * Time.deltaTime);
            }
        }

        private void DisablePlayer()
        {
            // Disable movement and input
            var movement = GetComponent<PlayerMovement>();
            if (movement != null)
                movement.enabled = false;

            var controller = GetComponent<PlayerController>();
            if (controller != null)
                controller.enabled = false;

            // TODO: Play death animation
            // TODO: Show death UI
        }

        private void EnablePlayer()
        {
            // Re-enable movement and input
            var movement = GetComponent<PlayerMovement>();
            if (movement != null)
                movement.enabled = true;

            var controller = GetComponent<PlayerController>();
            if (controller != null)
                controller.enabled = true;

            // TODO: Play revive animation
        }

        private ulong GetPlayerId()
        {
            // TODO: Get actual network player ID
            // For now, return 0
            return 0;
        }

        private void OnValidate()
        {
            // Ensure health is within bounds in editor
            if (currentHealth > maxHealth)
                currentHealth = maxHealth;
            if (currentHealth < 0f)
                currentHealth = 0f;
        }
    }
}

namespace DeadFrontier.Core
{
    /// <summary>
    /// Interface for objects that can take damage
    /// </summary>
    public interface IDamageable
    {
        void TakeDamage(float damage);
    }
}
