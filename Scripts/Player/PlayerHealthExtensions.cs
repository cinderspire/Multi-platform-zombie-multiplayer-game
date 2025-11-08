using UnityEngine;

namespace DeadFrontier.Player
{
    /// <summary>
    /// Extension methods for PlayerHealth to add missing functionality
    /// </summary>
    public partial class PlayerHealth
    {
        /// <summary>
        /// Heals the player
        /// </summary>
        public void Heal(float amount)
        {
            if (currentHealth <= 0f)
                return;

            float oldHealth = currentHealth;
            currentHealth = Mathf.Min(currentHealth + amount, maxHealth);

            float actualHeal = currentHealth - oldHealth;

            if (actualHeal > 0f)
            {
                OnHealthChanged?.Invoke(currentHealth);
                Debug.Log($"[PlayerHealth] Healed {actualHeal} HP (now {currentHealth}/{maxHealth})");
            }
        }

        /// <summary>
        /// Sets health directly (for loading saves)
        /// </summary>
        public void SetHealth(float health)
        {
            currentHealth = Mathf.Clamp(health, 0f, maxHealth);
            OnHealthChanged?.Invoke(currentHealth);
        }

        /// <summary>
        /// Resets health to maximum
        /// </summary>
        public void ResetHealth()
        {
            currentHealth = maxHealth;
            OnHealthChanged?.Invoke(currentHealth);
        }

        /// <summary>
        /// Modifies maximum health (for perks)
        /// </summary>
        public void ModifyMaxHealth(float amount, bool isMultiplicative)
        {
            float oldMax = maxHealth;

            if (isMultiplicative)
            {
                maxHealth *= amount;
            }
            else
            {
                maxHealth += amount;
            }

            maxHealth = Mathf.Max(maxHealth, 10f); // Minimum 10 HP

            // Adjust current health proportionally
            float ratio = currentHealth / oldMax;
            currentHealth = maxHealth * ratio;

            OnHealthChanged?.Invoke(currentHealth);

            Debug.Log($"[PlayerHealth] Max health modified: {oldMax} → {maxHealth}");
        }

        /// <summary>
        /// Gets current health percentage
        /// </summary>
        public float GetHealthPercentage()
        {
            return maxHealth > 0f ? currentHealth / maxHealth : 0f;
        }

        /// <summary>
        /// Checks if player is at low health
        /// </summary>
        public bool IsLowHealth(float threshold = 0.3f)
        {
            return GetHealthPercentage() <= threshold;
        }

        /// <summary>
        /// Checks if player is near death
        /// </summary>
        public bool IsNearDeath(float threshold = 0.1f)
        {
            return GetHealthPercentage() <= threshold;
        }
    }
}
