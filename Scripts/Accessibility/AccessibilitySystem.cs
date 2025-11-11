using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;

namespace ZombieGame.Accessibility
{
    /// <summary>
    /// Comprehensive accessibility system providing options for colorblind modes,
    /// subtitles, UI scaling, high contrast, and other accessibility features.
    /// </summary>
    public class AccessibilitySystem : MonoBehaviour
    {
        public static AccessibilitySystem Instance { get; private set; }

        [Header("Accessibility Configuration")]
        [SerializeField] private bool enableAccessibilityByDefault = true;

        [Header("Colorblind Settings")]
        [SerializeField] private Material colorblindMaterial;

        [Header("UI Settings")]
        [SerializeField] private float minUIScale = 0.8f;
        [SerializeField] private float maxUIScale = 1.5f;

        private AccessibilitySettings currentSettings;

        public event Action<ColorblindMode> OnColorblindModeChanged;
        public event Action<float> OnUIScaleChanged;
        public event Action<bool> OnHighContrastChanged;
        public event Action<bool> OnSubtitlesChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            InitializeSettings();
            LoadSettings();
            ApplyAllSettings();
        }

        private void InitializeSettings()
        {
            currentSettings = new AccessibilitySettings
            {
                colorblindMode = ColorblindMode.None,
                enableSubtitles = true,
                subtitleSize = SubtitleSize.Medium,
                uiScale = 1f,
                enableHighContrast = false,
                enableScreenShake = true,
                enableMotionBlur = true,
                enableScreenFlash = true,
                enableCameraShake = true,
                aimAssistStrength = 0.5f,
                autoAimEnabled = false,
                holdToADS = false,
                holdToCrouch = false,
                toggleSprint = false,
                enablePingSound = true,
                enableDamageFeedback = true,
                enableHitmarkerSound = true,
                showDamageNumbers = true,
                chatBackgroundOpacity = 0.7f,
                hudOpacity = 1f,
                crosshairSize = 1f,
                crosshairColor = Color.white
            };
        }

        /// <summary>
        /// Set colorblind mode
        /// </summary>
        public void SetColorblindMode(ColorblindMode mode)
        {
            currentSettings.colorblindMode = mode;
            ApplyColorblindMode(mode);
            OnColorblindModeChanged?.Invoke(mode);
            SaveSettings();
        }

        private void ApplyColorblindMode(ColorblindMode mode)
        {
            // Apply colorblind shader/filter
            if (colorblindMaterial != null)
            {
                switch (mode)
                {
                    case ColorblindMode.Protanopia:
                        colorblindMaterial.SetFloat("_ColorblindMode", 1);
                        break;
                    case ColorblindMode.Deuteranopia:
                        colorblindMaterial.SetFloat("_ColorblindMode", 2);
                        break;
                    case ColorblindMode.Tritanopia:
                        colorblindMaterial.SetFloat("_ColorblindMode", 3);
                        break;
                    default:
                        colorblindMaterial.SetFloat("_ColorblindMode", 0);
                        break;
                }
            }

            // Update UI colors
            UpdateUIColors(mode);
        }

        private void UpdateUIColors(ColorblindMode mode)
        {
            // Apply colorblind-friendly colors to UI elements
            switch (mode)
            {
                case ColorblindMode.Protanopia:
                case ColorblindMode.Deuteranopia:
                    // Use blue/yellow instead of red/green
                    break;
                case ColorblindMode.Tritanopia:
                    // Use red/green instead of blue/yellow
                    break;
            }
        }

        /// <summary>
        /// Set UI scale
        /// </summary>
        public void SetUIScale(float scale)
        {
            currentSettings.uiScale = Mathf.Clamp(scale, minUIScale, maxUIScale);
            ApplyUIScale();
            OnUIScaleChanged?.Invoke(currentSettings.uiScale);
            SaveSettings();
        }

        private void ApplyUIScale()
        {
            // Scale all UI canvases
            Canvas[] canvases = FindObjectsOfType<Canvas>();
            foreach (Canvas canvas in canvases)
            {
                CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler != null)
                {
                    scaler.scaleFactor = currentSettings.uiScale;
                }
            }
        }

        /// <summary>
        /// Enable/disable subtitles
        /// </summary>
        public void SetSubtitlesEnabled(bool enabled)
        {
            currentSettings.enableSubtitles = enabled;
            OnSubtitlesChanged?.Invoke(enabled);
            SaveSettings();
        }

        /// <summary>
        /// Set subtitle size
        /// </summary>
        public void SetSubtitleSize(SubtitleSize size)
        {
            currentSettings.subtitleSize = size;
            ApplySubtitleSettings();
            SaveSettings();
        }

        private void ApplySubtitleSettings()
        {
            // Would apply to subtitle UI component
            float fontSize = currentSettings.subtitleSize switch
            {
                SubtitleSize.Small => 16f,
                SubtitleSize.Medium => 20f,
                SubtitleSize.Large => 24f,
                SubtitleSize.ExtraLarge => 28f,
                _ => 20f
            };
        }

        /// <summary>
        /// Enable/disable high contrast mode
        /// </summary>
        public void SetHighContrast(bool enabled)
        {
            currentSettings.enableHighContrast = enabled;
            ApplyHighContrast();
            OnHighContrastChanged?.Invoke(enabled);
            SaveSettings();
        }

        private void ApplyHighContrast()
        {
            if (currentSettings.enableHighContrast)
            {
                // Increase contrast in visuals
                // Make UI elements more distinct
                // Adjust colors for better visibility
            }
        }

        /// <summary>
        /// Set motion effects
        /// </summary>
        public void SetScreenShakeEnabled(bool enabled)
        {
            currentSettings.enableScreenShake = enabled;
            SaveSettings();
        }

        public void SetMotionBlurEnabled(bool enabled)
        {
            currentSettings.enableMotionBlur = enabled;
            // Disable post-processing motion blur
            SaveSettings();
        }

        public void SetCameraShakeEnabled(bool enabled)
        {
            currentSettings.enableCameraShake = enabled;
            SaveSettings();
        }

        public void SetScreenFlashEnabled(bool enabled)
        {
            currentSettings.enableScreenFlash = enabled;
            SaveSettings();
        }

        /// <summary>
        /// Set aim assist
        /// </summary>
        public void SetAimAssistStrength(float strength)
        {
            currentSettings.aimAssistStrength = Mathf.Clamp01(strength);
            SaveSettings();
        }

        public void SetAutoAimEnabled(bool enabled)
        {
            currentSettings.autoAimEnabled = enabled;
            SaveSettings();
        }

        /// <summary>
        /// Set toggle vs hold controls
        /// </summary>
        public void SetHoldToADS(bool hold)
        {
            currentSettings.holdToADS = hold;
            SaveSettings();
        }

        public void SetHoldToCrouch(bool hold)
        {
            currentSettings.holdToCrouch = hold;
            SaveSettings();
        }

        public void SetToggleSprint(bool toggle)
        {
            currentSettings.toggleSprint = toggle;
            SaveSettings();
        }

        /// <summary>
        /// Set audio cues
        /// </summary>
        public void SetPingSoundEnabled(bool enabled)
        {
            currentSettings.enablePingSound = enabled;
            SaveSettings();
        }

        public void SetHitmarkerSoundEnabled(bool enabled)
        {
            currentSettings.enableHitmarkerSound = enabled;
            SaveSettings();
        }

        /// <summary>
        /// Set visual feedback
        /// </summary>
        public void SetDamageNumbersEnabled(bool enabled)
        {
            currentSettings.showDamageNumbers = enabled;
            SaveSettings();
        }

        public void SetDamageFeedbackEnabled(bool enabled)
        {
            currentSettings.enableDamageFeedback = enabled;
            SaveSettings();
        }

        /// <summary>
        /// Set UI opacity
        /// </summary>
        public void SetHUDOpacity(float opacity)
        {
            currentSettings.hudOpacity = Mathf.Clamp01(opacity);
            ApplyHUDOpacity();
            SaveSettings();
        }

        private void ApplyHUDOpacity()
        {
            // Apply to HUD elements
        }

        public void SetChatBackgroundOpacity(float opacity)
        {
            currentSettings.chatBackgroundOpacity = Mathf.Clamp01(opacity);
            SaveSettings();
        }

        /// <summary>
        /// Set crosshair customization
        /// </summary>
        public void SetCrosshairSize(float size)
        {
            currentSettings.crosshairSize = Mathf.Clamp(size, 0.5f, 2f);
            ApplyCrosshairSettings();
            SaveSettings();
        }

        public void SetCrosshairColor(Color color)
        {
            currentSettings.crosshairColor = color;
            ApplyCrosshairSettings();
            SaveSettings();
        }

        private void ApplyCrosshairSettings()
        {
            // Apply to crosshair UI
        }

        private void ApplyAllSettings()
        {
            ApplyColorblindMode(currentSettings.colorblindMode);
            ApplyUIScale();
            ApplyHighContrast();
            ApplySubtitleSettings();
            ApplyHUDOpacity();
            ApplyCrosshairSettings();
        }

        public AccessibilitySettings GetSettings()
        {
            return currentSettings;
        }

        private void SaveSettings()
        {
            string json = JsonUtility.ToJson(currentSettings);
            PlayerPrefs.SetString("AccessibilitySettings", json);
            PlayerPrefs.Save();
        }

        private void LoadSettings()
        {
            if (PlayerPrefs.HasKey("AccessibilitySettings"))
            {
                string json = PlayerPrefs.GetString("AccessibilitySettings");
                currentSettings = JsonUtility.FromJson<AccessibilitySettings>(json);
            }
        }

        [Serializable]
        public class AccessibilitySettings
        {
            // Visual
            public ColorblindMode colorblindMode;
            public bool enableHighContrast;
            public float uiScale;

            // Text
            public bool enableSubtitles;
            public SubtitleSize subtitleSize;

            // Motion
            public bool enableScreenShake;
            public bool enableMotionBlur;
            public bool enableCameraShake;
            public bool enableScreenFlash;

            // Gameplay
            public float aimAssistStrength;
            public bool autoAimEnabled;
            public bool holdToADS;
            public bool holdToCrouch;
            public bool toggleSprint;

            // Audio Cues
            public bool enablePingSound;
            public bool enableHitmarkerSound;

            // Visual Feedback
            public bool enableDamageFeedback;
            public bool showDamageNumbers;

            // UI
            public float chatBackgroundOpacity;
            public float hudOpacity;
            public float crosshairSize;
            public Color crosshairColor;
        }

        public enum ColorblindMode
        {
            None,
            Protanopia,      // Red-blind
            Deuteranopia,    // Green-blind
            Tritanopia       // Blue-blind
        }

        public enum SubtitleSize
        {
            Small,
            Medium,
            Large,
            ExtraLarge
        }
    }
}
