using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DeadFrontier.UI
{
    /// <summary>
    /// Displays and animates player health bar
    /// </summary>
    public class HealthBar : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image healthFillImage;
        [SerializeField] private Image healthBackgroundImage;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private CanvasGroup damageFlashGroup;

        [Header("Colors")]
        [SerializeField] private Color healthyColor = Color.green;
        [SerializeField] private Color warnColor = Color.yellow;
        [SerializeField] private Color criticalColor = Color.red;
        [SerializeField] private Color damageFlashColor = new Color(1f, 0f, 0f, 0.3f);

        [Header("Settings")]
        [SerializeField] private float smoothSpeed = 5f;
        [SerializeField] private float damageFlashDuration = 0.2f;
        [SerializeField] private float lowHealthThreshold = 30f;

        private float currentHealth;
        private float maxHealth;
        private float targetFillAmount;
        private Player.PlayerHealth playerHealth;

        private void Start()
        {
            // Find player health component
            FindPlayerHealth();

            // Initialize damage flash
            if (damageFlashGroup != null)
            {
                damageFlashGroup.alpha = 0f;
            }
        }

        private void Update()
        {
            // Smoothly animate health bar
            if (healthFillImage != null)
            {
                healthFillImage.fillAmount = Mathf.Lerp(
                    healthFillImage.fillAmount,
                    targetFillAmount,
                    Time.deltaTime * smoothSpeed
                );
            }
        }

        private void FindPlayerHealth()
        {
            // Find local player
            var player = FindObjectOfType<Player.PlayerController>();
            if (player != null)
            {
                playerHealth = player.Health;
                if (playerHealth != null)
                {
                    playerHealth.OnHealthChanged += UpdateHealth;
                    playerHealth.OnDamaged += OnPlayerDamaged;

                    // Initialize values
                    maxHealth = playerHealth.MaxHealth;
                    currentHealth = playerHealth.CurrentHealth;
                    UpdateHealth(currentHealth);
                }
            }
        }

        private void UpdateHealth(float newHealth)
        {
            currentHealth = newHealth;
            targetFillAmount = currentHealth / maxHealth;

            // Update text
            if (healthText != null)
            {
                healthText.text = $"{Mathf.CeilToInt(currentHealth)} / {Mathf.CeilToInt(maxHealth)}";
            }

            // Update color based on health percentage
            if (healthFillImage != null)
            {
                float healthPercentage = currentHealth / maxHealth * 100f;

                if (healthPercentage > lowHealthThreshold)
                {
                    healthFillImage.color = healthyColor;
                }
                else if (healthPercentage > lowHealthThreshold / 2f)
                {
                    healthFillImage.color = warnColor;
                }
                else
                {
                    healthFillImage.color = criticalColor;
                }
            }
        }

        private void OnPlayerDamaged(float damageAmount, float remainingHealth)
        {
            // Flash red when damaged
            if (damageFlashGroup != null)
            {
                StopAllCoroutines();
                StartCoroutine(DamageFlashCoroutine());
            }
        }

        private System.Collections.IEnumerator DamageFlashCoroutine()
        {
            // Flash to full opacity
            damageFlashGroup.alpha = 1f;

            // Fade out
            float elapsed = 0f;
            while (elapsed < damageFlashDuration)
            {
                elapsed += Time.deltaTime;
                damageFlashGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / damageFlashDuration);
                yield return null;
            }

            damageFlashGroup.alpha = 0f;
        }

        /// <summary>
        /// Manually set health values (for testing)
        /// </summary>
        public void SetHealth(float current, float max)
        {
            currentHealth = current;
            maxHealth = max;
            UpdateHealth(current);
        }

        private void OnDestroy()
        {
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged -= UpdateHealth;
                playerHealth.OnDamaged -= OnPlayerDamaged;
            }
        }
    }
}
