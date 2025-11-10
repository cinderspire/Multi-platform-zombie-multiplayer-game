using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

namespace ZombieGame.UI
{
    public class HUDSystem : MonoBehaviour
    {
        public static HUDSystem Instance { get; private set; }

        [Header("Health/Stamina")]
        [SerializeField] private Slider healthBar;
        [SerializeField] private Slider armorBar;
        [SerializeField] private Slider staminaBar;
        [SerializeField] private TextMeshProUGUI healthText;

        [Header("Ammo")]
        [SerializeField] private TextMeshProUGUI ammoText;
        [SerializeField] private TextMeshProUGUI reserveAmmoText;

        [Header("Crosshair")]
        [SerializeField] private Image crosshairImage;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color enemyColor = Color.red;

        [Header("Damage Indicators")]
        [SerializeField] private Image damageVignette;

        [Header("Wave Info")]
        [SerializeField] private TextMeshProUGUI waveText;
        [SerializeField] private TextMeshProUGUI zombieCountText;

        [Header("Kill Feed")]
        [SerializeField] private Transform killFeedParent;
        [SerializeField] private GameObject killFeedEntryPrefab;

        private ulong localPlayerId;
        private float damageVignetteAlpha = 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            localPlayerId = NetworkManager.Singleton.LocalClientId;
            SubscribeToEvents();
        }

        private void Update()
        {
            UpdateHealthStamina();
            UpdateDamageVignette();
        }

        private void SubscribeToEvents()
        {
            if (Health.HealthSystem.Instance != null)
            {
                Health.HealthSystem.Instance.OnHealthChanged += OnHealthChanged;
                Health.HealthSystem.Instance.OnDamageTaken += OnDamageTaken;
            }

            if (AI.SpawnerSystem.Instance != null)
            {
                AI.SpawnerSystem.Instance.OnWaveStarted += OnWaveStarted;
            }
        }

        private void UpdateHealthStamina()
        {
            var healthSys = Health.HealthSystem.Instance;
            if (healthSys != null)
            {
                var health = healthSys.GetEntityHealth(localPlayerId);
                if (health != null)
                {
                    UpdateHealthBar(health.currentHealth, health.maxHealth);
                    UpdateArmorBar(health.currentArmor, health.maxArmor);
                }
            }

            var staminaSys = Player.StaminaSystem.Instance;
            if (staminaSys != null && staminaBar != null)
            {
                // Would update stamina bar here
            }
        }

        private void UpdateHealthBar(float current, float max)
        {
            if (healthBar != null)
            {
                healthBar.value = current / max;
            }

            if (healthText != null)
            {
                healthText.text = $"{Mathf.RoundToInt(current)}/{Mathf.RoundToInt(max)}";
            }
        }

        private void UpdateArmorBar(float current, float max)
        {
            if (armorBar != null && max > 0)
            {
                armorBar.gameObject.SetActive(true);
                armorBar.value = current / max;
            }
            else if (armorBar != null)
            {
                armorBar.gameObject.SetActive(false);
            }
        }

        private void UpdateDamageVignette()
        {
            if (damageVignette != null)
            {
                damageVignetteAlpha = Mathf.Lerp(damageVignetteAlpha, 0f, Time.deltaTime * 2f);
                Color col = damageVignette.color;
                col.a = damageVignetteAlpha;
                damageVignette.color = col;
            }
        }

        private void OnHealthChanged(ulong entityId, float current, float max)
        {
            if (entityId == localPlayerId)
            {
                UpdateHealthBar(current, max);
            }
        }

        private void OnDamageTaken(ulong entityId, Health.DamageInfo damageInfo)
        {
            if (entityId == localPlayerId)
            {
                damageVignetteAlpha = 0.5f;
            }
        }

        private void OnWaveStarted(int wave)
        {
            if (waveText != null)
            {
                waveText.text = $"Wave {wave}";
            }
        }

        public void UpdateAmmoDisplay(int current, int reserve)
        {
            if (ammoText != null) ammoText.text = current.ToString();
            if (reserveAmmoText != null) reserveAmmoText.text = reserve.ToString();
        }

        public void UpdateCrosshairColor(bool onEnemy)
        {
            if (crosshairImage != null)
            {
                crosshairImage.color = onEnemy ? enemyColor : normalColor;
            }
        }

        public void ShowKillFeed(string killerName, string victimName, string weaponName)
        {
            if (killFeedParent == null || killFeedEntryPrefab == null) return;

            GameObject entry = Instantiate(killFeedEntryPrefab, killFeedParent);
            TextMeshProUGUI text = entry.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = $"{killerName} [{weaponName}] {victimName}";
            }

            Destroy(entry, 5f);
        }
    }
}
