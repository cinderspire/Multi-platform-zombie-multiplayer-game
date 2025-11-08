using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace DeadFrontier.UI
{
    /// <summary>
    /// Comprehensive in-game HUD managing health, ammo, objectives, mini-map, and all UI elements.
    /// </summary>
    public class GameHUD : MonoBehaviour
    {
        public static GameHUD Instance { get; private set; }

        [Header("Player Status")]
        [SerializeField] private Slider healthBar;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private Slider staminaBar;
        [SerializeField] private Image healthVignette;
        [SerializeField] private float vignetteIntensity = 0.5f;

        [Header("Weapon Info")]
        [SerializeField] private TextMeshProUGUI ammoText;
        [SerializeField] private TextMeshProUGUI weaponNameText;
        [SerializeField] private Image weaponIcon;
        [SerializeField] private Image reloadBar;
        [SerializeField] private GameObject reloadIndicator;
        [SerializeField] private TextMeshProUGUI fireModeText;

        [Header("Inventory")]
        [SerializeField] private Slider weightBar;
        [SerializeField] private TextMeshProUGUI weightText;
        [SerializeField] private TextMeshProUGUI currencyText;

        [Header("Objectives")]
        [SerializeField] private GameObject objectivesPanel;
        [SerializeField] private Transform objectivesContainer;
        [SerializeField] private GameObject objectiveItemPrefab;

        [Header("Mini-map")]
        [SerializeField] private RawImage minimapImage;
        [SerializeField] private RectTransform minimapPlayerMarker;
        [SerializeField] private GameObject minimapZombieMarkerPrefab;
        [SerializeField] private GameObject minimapPlayerMarkerPrefab;
        [SerializeField] private GameObject minimapExtractionMarkerPrefab;
        [SerializeField] private float minimapScale = 100f;

        [Header("Team Info")]
        [SerializeField] private GameObject teamPanel;
        [SerializeField] private Transform teamContainer;
        [SerializeField] private GameObject teamMemberPrefab;

        [Header("Extraction")]
        [SerializeField] private GameObject extractionPanel;
        [SerializeField] private Slider extractionProgressBar;
        [SerializeField] private TextMeshProUGUI extractionText;
        [SerializeField] private TextMeshProUGUI extractionTimerText;

        [Header("Danger Indicator")]
        [SerializeField] private GameObject dangerIndicatorPanel;
        [SerializeField] private Transform dangerIndicatorContainer;
        [SerializeField] private GameObject dangerArrowPrefab;

        [Header("Interaction")]
        [SerializeField] private GameObject interactionPrompt;
        [SerializeField] private TextMeshProUGUI interactionText;

        [Header("Match Info")]
        [SerializeField] private TextMeshProUGUI matchTimerText;
        [SerializeField] private TextMeshProUGUI playersAliveText;
        [SerializeField] private TextMeshProUGUI zombiesKilledText;

        [Header("Damage Indicators")]
        [SerializeField] private GameObject damageIndicatorPrefab;
        [SerializeField] private Transform damageIndicatorContainer;
        [SerializeField] private float damageIndicatorDuration = 2f;

        [Header("Hit Markers")]
        [SerializeField] private GameObject hitMarker;
        [SerializeField] private GameObject criticalHitMarker;
        [SerializeField] private float hitMarkerDuration = 0.2f;

        [Header("Status Effects")]
        [SerializeField] private Transform statusEffectsContainer;
        [SerializeField] private GameObject statusEffectPrefab;

        // Internal tracking
        private Player.PlayerHealth playerHealth;
        private Player.PlayerMovement playerMovement;
        private Dictionary<string, GameObject> activeObjectiveItems = new Dictionary<string, GameObject>();
        private Dictionary<ulong, GameObject> teamMemberWidgets = new Dictionary<ulong, GameObject>();
        private List<GameObject> activeDamageIndicators = new List<GameObject>();
        private Dictionary<string, GameObject> activeStatusEffects = new Dictionary<string, GameObject>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            InitializeHUD();
            SubscribeToEvents();
        }

        private void Update()
        {
            UpdatePlayerStatus();
            UpdateMatchInfo();
            UpdateMinimap();
            UpdateDangerIndicators();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        #region Initialization

        private void InitializeHUD()
        {
            // Hide conditional panels
            if (extractionPanel != null)
                extractionPanel.SetActive(false);

            if (reloadIndicator != null)
                reloadIndicator.SetActive(false);

            if (interactionPrompt != null)
                interactionPrompt.SetActive(false);

            // Find player components
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerHealth = player.GetComponent<Player.PlayerHealth>();
                playerMovement = player.GetComponent<Player.PlayerMovement>();
            }
        }

        private void SubscribeToEvents()
        {
            if (Gameplay.ExtractionZoneManager.Instance != null)
            {
                Gameplay.ExtractionZoneManager.Instance.OnExtractionStarted += HandleExtractionStarted;
                Gameplay.ExtractionZoneManager.Instance.OnExtractionProgress += HandleExtractionProgress;
                Gameplay.ExtractionZoneManager.Instance.OnExtractionComplete += HandleExtractionComplete;
            }

            if (Gameplay.ObjectivesManager.Instance != null)
            {
                Gameplay.ObjectivesManager.Instance.OnObjectiveStarted += AddObjectiveToUI;
                Gameplay.ObjectivesManager.Instance.OnObjectiveProgress += UpdateObjectiveProgress;
                Gameplay.ObjectivesManager.Instance.OnObjectiveCompleted += RemoveObjectiveFromUI;
            }

            if (Gameplay.InventoryManager.Instance != null)
            {
                Gameplay.InventoryManager.Instance.OnInventoryWeightChanged += UpdateInventoryWeight;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (Gameplay.ExtractionZoneManager.Instance != null)
            {
                Gameplay.ExtractionZoneManager.Instance.OnExtractionStarted -= HandleExtractionStarted;
                Gameplay.ExtractionZoneManager.Instance.OnExtractionProgress -= HandleExtractionProgress;
                Gameplay.ExtractionZoneManager.Instance.OnExtractionComplete -= HandleExtractionComplete;
            }

            if (Gameplay.ObjectivesManager.Instance != null)
            {
                Gameplay.ObjectivesManager.Instance.OnObjectiveStarted -= AddObjectiveToUI;
                Gameplay.ObjectivesManager.Instance.OnObjectiveProgress -= UpdateObjectiveProgress;
                Gameplay.ObjectivesManager.Instance.OnObjectiveCompleted -= RemoveObjectiveFromUI;
            }

            if (Gameplay.InventoryManager.Instance != null)
            {
                Gameplay.InventoryManager.Instance.OnInventoryWeightChanged -= UpdateInventoryWeight;
            }
        }

        #endregion

        #region Player Status

        private void UpdatePlayerStatus()
        {
            if (playerHealth != null)
            {
                // Update health
                float healthPercent = playerHealth.GetHealthPercentage();

                if (healthBar != null)
                    healthBar.value = healthPercent;

                if (healthText != null)
                    healthText.text = $"{Mathf.RoundToInt(playerHealth.GetCurrentHealth())}/{Mathf.RoundToInt(playerHealth.GetMaxHealth())}";

                // Update health vignette
                if (healthVignette != null)
                {
                    Color vignetteColor = healthVignette.color;
                    vignetteColor.a = (1f - healthPercent) * vignetteIntensity;
                    healthVignette.color = vignetteColor;
                }
            }

            if (playerMovement != null && staminaBar != null)
            {
                staminaBar.value = playerMovement.GetStaminaPercentage();
            }
        }

        #endregion

        #region Weapon Info

        public void UpdateAmmo(int current, int reserve)
        {
            if (ammoText != null)
                ammoText.text = $"{current} / {reserve}";
        }

        public void UpdateWeaponInfo(string weaponName, Sprite icon)
        {
            if (weaponNameText != null)
                weaponNameText.text = weaponName;

            if (weaponIcon != null && icon != null)
                weaponIcon.sprite = icon;
        }

        public void ShowReloadIndicator(bool show, float progress = 0f)
        {
            if (reloadIndicator != null)
                reloadIndicator.SetActive(show);

            if (reloadBar != null && show)
                reloadBar.fillAmount = progress;
        }

        public void UpdateFireMode(string mode)
        {
            if (fireModeText != null)
                fireModeText.text = mode;
        }

        #endregion

        #region Inventory

        private void UpdateInventoryWeight(ulong playerId, float weight)
        {
            if (Gameplay.InventoryManager.Instance == null) return;

            float maxWeight = Gameplay.InventoryManager.Instance.GetMaxWeight(playerId);
            float weightPercent = weight / maxWeight;

            if (weightBar != null)
                weightBar.value = weightPercent;

            if (weightText != null)
            {
                weightText.text = $"{weight:F1} / {maxWeight:F1} kg";

                // Change color when overweight
                if (weightPercent > 0.9f)
                    weightText.color = Color.red;
                else if (weightPercent > 0.7f)
                    weightText.color = Color.yellow;
                else
                    weightText.color = Color.white;
            }
        }

        public void UpdateCurrency(int softCurrency)
        {
            if (currencyText != null)
                currencyText.text = $"${softCurrency}";
        }

        #endregion

        #region Objectives

        private void AddObjectiveToUI(Core.MapObjective objective)
        {
            if (objectiveItemPrefab == null || objectivesContainer == null) return;

            GameObject objectiveItem = Instantiate(objectiveItemPrefab, objectivesContainer);
            var textComponent = objectiveItem.GetComponentInChildren<TextMeshProUGUI>();

            if (textComponent != null)
                textComponent.text = objective.objectiveName;

            activeObjectiveItems[objective.objectiveId] = objectiveItem;

            if (objectivesPanel != null)
                objectivesPanel.SetActive(true);
        }

        private void UpdateObjectiveProgress(Core.MapObjective objective, float progress)
        {
            if (!activeObjectiveItems.ContainsKey(objective.objectiveId)) return;

            var objectiveItem = activeObjectiveItems[objective.objectiveId];
            var slider = objectiveItem.GetComponentInChildren<Slider>();

            if (slider != null)
                slider.value = progress;
        }

        private void RemoveObjectiveFromUI(Core.MapObjective objective)
        {
            if (!activeObjectiveItems.ContainsKey(objective.objectiveId)) return;

            Destroy(activeObjectiveItems[objective.objectiveId]);
            activeObjectiveItems.Remove(objective.objectiveId);

            if (activeObjectiveItems.Count == 0 && objectivesPanel != null)
                objectivesPanel.SetActive(false);
        }

        #endregion

        #region Extraction

        private void HandleExtractionStarted(ulong playerId, Gameplay.ExtractionZone zone)
        {
            if (extractionPanel != null)
                extractionPanel.SetActive(true);

            if (extractionText != null)
                extractionText.text = $"Extracting at {zone.ZoneName}";
        }

        private void HandleExtractionProgress(ulong playerId, float progress)
        {
            if (extractionProgressBar != null)
                extractionProgressBar.value = progress;

            if (extractionTimerText != null)
            {
                float remaining = (1f - progress) * 10f; // Assuming 10s extraction
                extractionTimerText.text = $"{remaining:F1}s";
            }
        }

        private void HandleExtractionComplete(ulong playerId, bool success)
        {
            if (extractionPanel != null)
                extractionPanel.SetActive(false);
        }

        #endregion

        #region Mini-map

        private void UpdateMinimap()
        {
            // Update player marker rotation
            if (minimapPlayerMarker != null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    minimapPlayerMarker.rotation = Quaternion.Euler(0f, 0f, -player.transform.eulerAngles.y);
                }
            }

            // TODO: Update zombie markers, player markers, extraction markers
        }

        #endregion

        #region Danger Indicators

        private void UpdateDangerIndicators()
        {
            // Show arrows pointing to nearby zombies
            // TODO: Implement danger arrow system
        }

        public void ShowDamageIndicator(Vector3 damageSource)
        {
            if (damageIndicatorPrefab == null || damageIndicatorContainer == null) return;

            GameObject indicator = Instantiate(damageIndicatorPrefab, damageIndicatorContainer);

            // Calculate direction to damage source
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                Vector3 direction = (damageSource - player.transform.position).normalized;
                float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

                indicator.transform.rotation = Quaternion.Euler(0f, 0f, -angle);
            }

            activeDamageIndicators.Add(indicator);
            Destroy(indicator, damageIndicatorDuration);
        }

        #endregion

        #region Hit Markers

        public void ShowHitMarker(bool isCritical = false)
        {
            GameObject marker = isCritical ? criticalHitMarker : hitMarker;
            if (marker == null) return;

            marker.SetActive(true);
            CancelInvoke(nameof(HideHitMarker));
            Invoke(nameof(HideHitMarker), hitMarkerDuration);
        }

        private void HideHitMarker()
        {
            if (hitMarker != null)
                hitMarker.SetActive(false);

            if (criticalHitMarker != null)
                criticalHitMarker.SetActive(false);
        }

        #endregion

        #region Status Effects

        public void AddStatusEffect(string effectId, string effectName, Sprite icon, float duration)
        {
            if (statusEffectPrefab == null || statusEffectsContainer == null) return;

            if (activeStatusEffects.ContainsKey(effectId))
            {
                // Update existing effect
                return;
            }

            GameObject effectWidget = Instantiate(statusEffectPrefab, statusEffectsContainer);
            var iconImage = effectWidget.GetComponent<Image>();
            if (iconImage != null && icon != null)
                iconImage.sprite = icon;

            activeStatusEffects[effectId] = effectWidget;

            if (duration > 0f)
            {
                Destroy(effectWidget, duration);
                Invoke(() => activeStatusEffects.Remove(effectId), duration);
            }
        }

        public void RemoveStatusEffect(string effectId)
        {
            if (!activeStatusEffects.ContainsKey(effectId)) return;

            Destroy(activeStatusEffects[effectId]);
            activeStatusEffects.Remove(effectId);
        }

        #endregion

        #region Interaction

        public void ShowInteractionPrompt(string text)
        {
            if (interactionPrompt != null)
                interactionPrompt.SetActive(true);

            if (interactionText != null)
                interactionText.text = text;
        }

        public void HideInteractionPrompt()
        {
            if (interactionPrompt != null)
                interactionPrompt.SetActive(false);
        }

        #endregion

        #region Match Info

        private void UpdateMatchInfo()
        {
            // Update match timer
            // Update players alive count
            // Update zombie kill count
            // TODO: Get this data from GameManager
        }

        #endregion
    }
}
