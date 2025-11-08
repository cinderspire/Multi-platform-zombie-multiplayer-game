using UnityEngine;

namespace DeadFrontier.Core.Settings
{
    /// <summary>
    /// Manages game settings and applies them
    /// </summary>
    public class SettingsManager : Singleton<SettingsManager>
    {
        [Header("Default Settings")]
        [SerializeField] private Save.SettingsData defaultSettings;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        // Current settings
        private Save.SettingsData currentSettings;

        // Events
        public event System.Action<Save.SettingsData> OnSettingsChanged;
        public event System.Action<GraphicsSettingsChange> OnGraphicsChanged;
        public event System.Action<AudioSettingsChange> OnAudioChanged;
        public event System.Action<ControlSettingsChange> OnControlsChanged;

        // Properties
        public Save.SettingsData CurrentSettings => currentSettings;

        protected override void Awake()
        {
            base.Awake();

            // Initialize with default settings
            if (currentSettings == null)
            {
                currentSettings = defaultSettings ?? new Save.SettingsData();
            }

            // Load settings from save system
            LoadSettings();

            // Apply settings
            ApplyAllSettings();
        }

        #region Graphics Settings

        /// <summary>
        /// Sets quality level preset
        /// </summary>
        public void SetQualityLevel(int level)
        {
            level = Mathf.Clamp(level, 0, 3);
            currentSettings.qualityLevel = level;

            QualitySettings.SetQualityLevel(level);

            if (showDebugLogs)
                Debug.Log($"[SettingsManager] Quality level set to {level}");

            NotifyGraphicsChanged();
        }

        /// <summary>
        /// Sets screen resolution
        /// </summary>
        public void SetResolution(int width, int height, bool fullscreen)
        {
            currentSettings.resolutionWidth = width;
            currentSettings.resolutionHeight = height;
            currentSettings.fullscreen = fullscreen;

            Screen.SetResolution(width, height, fullscreen);

            if (showDebugLogs)
                Debug.Log($"[SettingsManager] Resolution set to {width}x{height} (Fullscreen: {fullscreen})");

            NotifyGraphicsChanged();
        }

        /// <summary>
        /// Sets target frame rate
        /// </summary>
        public void SetTargetFrameRate(int fps)
        {
            currentSettings.targetFrameRate = fps;
            Application.targetFrameRate = fps;

            if (showDebugLogs)
                Debug.Log($"[SettingsManager] Target FPS set to {fps}");

            NotifyGraphicsChanged();
        }

        /// <summary>
        /// Sets VSync
        /// </summary>
        public void SetVSync(bool enabled)
        {
            currentSettings.vsyncEnabled = enabled;
            QualitySettings.vSyncCount = enabled ? 1 : 0;

            if (showDebugLogs)
                Debug.Log($"[SettingsManager] VSync {(enabled ? "enabled" : "disabled")}");

            NotifyGraphicsChanged();
        }

        private void NotifyGraphicsChanged()
        {
            OnGraphicsChanged?.Invoke(new GraphicsSettingsChange
            {
                qualityLevel = currentSettings.qualityLevel,
                resolutionWidth = currentSettings.resolutionWidth,
                resolutionHeight = currentSettings.resolutionHeight,
                fullscreen = currentSettings.fullscreen,
                targetFrameRate = currentSettings.targetFrameRate,
                vsyncEnabled = currentSettings.vsyncEnabled
            });

            OnSettingsChanged?.Invoke(currentSettings);
        }

        #endregion

        #region Audio Settings

        /// <summary>
        /// Sets master volume
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);
            currentSettings.masterVolume = volume;
            AudioListener.volume = volume;

            if (showDebugLogs)
                Debug.Log($"[SettingsManager] Master volume set to {volume}");

            NotifyAudioChanged();
        }

        /// <summary>
        /// Sets music volume
        /// </summary>
        public void SetMusicVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);
            currentSettings.musicVolume = volume;

            // Apply to AudioManager if available
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMusicVolume(volume);
            }

            if (showDebugLogs)
                Debug.Log($"[SettingsManager] Music volume set to {volume}");

            NotifyAudioChanged();
        }

        /// <summary>
        /// Sets SFX volume
        /// </summary>
        public void SetSFXVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);
            currentSettings.sfxVolume = volume;

            // Apply to AudioManager if available
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetSFXVolume(volume);
            }

            if (showDebugLogs)
                Debug.Log($"[SettingsManager] SFX volume set to {volume}");

            NotifyAudioChanged();
        }

        /// <summary>
        /// Sets voice chat volume
        /// </summary>
        public void SetVoiceVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);
            currentSettings.voiceVolume = volume;

            if (showDebugLogs)
                Debug.Log($"[SettingsManager] Voice volume set to {volume}");

            NotifyAudioChanged();
        }

        private void NotifyAudioChanged()
        {
            OnAudioChanged?.Invoke(new AudioSettingsChange
            {
                masterVolume = currentSettings.masterVolume,
                musicVolume = currentSettings.musicVolume,
                sfxVolume = currentSettings.sfxVolume,
                voiceVolume = currentSettings.voiceVolume
            });

            OnSettingsChanged?.Invoke(currentSettings);
        }

        #endregion

        #region Control Settings

        /// <summary>
        /// Sets mouse sensitivity
        /// </summary>
        public void SetMouseSensitivity(float sensitivity)
        {
            sensitivity = Mathf.Clamp(sensitivity, 0.1f, 5f);
            currentSettings.mouseSensitivity = sensitivity;

            if (showDebugLogs)
                Debug.Log($"[SettingsManager] Mouse sensitivity set to {sensitivity}");

            NotifyControlsChanged();
        }

        /// <summary>
        /// Sets aim sensitivity
        /// </summary>
        public void SetAimSensitivity(float sensitivity)
        {
            sensitivity = Mathf.Clamp(sensitivity, 0.1f, 5f);
            currentSettings.aimSensitivity = sensitivity;

            if (showDebugLogs)
                Debug.Log($"[SettingsManager] Aim sensitivity set to {sensitivity}");

            NotifyControlsChanged();
        }

        /// <summary>
        /// Sets Y-axis inversion
        /// </summary>
        public void SetInvertY(bool invert)
        {
            currentSettings.invertY = invert;

            if (showDebugLogs)
                Debug.Log($"[SettingsManager] Invert Y {(invert ? "enabled" : "disabled")}");

            NotifyControlsChanged();
        }

        /// <summary>
        /// Binds a key to an action
        /// </summary>
        public void SetKeyBinding(string action, string key)
        {
            if (currentSettings.keyBindings == null)
            {
                currentSettings.keyBindings = new System.Collections.Generic.Dictionary<string, string>();
            }

            currentSettings.keyBindings[action] = key;

            if (showDebugLogs)
                Debug.Log($"[SettingsManager] Bound {action} to {key}");

            NotifyControlsChanged();
        }

        /// <summary>
        /// Gets key binding for an action
        /// </summary>
        public string GetKeyBinding(string action)
        {
            if (currentSettings.keyBindings != null && currentSettings.keyBindings.ContainsKey(action))
            {
                return currentSettings.keyBindings[action];
            }

            return null;
        }

        private void NotifyControlsChanged()
        {
            OnControlsChanged?.Invoke(new ControlSettingsChange
            {
                mouseSensitivity = currentSettings.mouseSensitivity,
                aimSensitivity = currentSettings.aimSensitivity,
                invertY = currentSettings.invertY
            });

            OnSettingsChanged?.Invoke(currentSettings);
        }

        #endregion

        #region Gameplay Settings

        /// <summary>
        /// Sets FPS counter visibility
        /// </summary>
        public void SetShowFPS(bool show)
        {
            currentSettings.showFPS = show;

            if (showDebugLogs)
                Debug.Log($"[SettingsManager] Show FPS {(show ? "enabled" : "disabled")}");

            OnSettingsChanged?.Invoke(currentSettings);
        }

        /// <summary>
        /// Sets ping display visibility
        /// </summary>
        public void SetShowPing(bool show)
        {
            currentSettings.showPing = show;

            if (showDebugLogs)
                Debug.Log($"[SettingsManager] Show ping {(show ? "enabled" : "disabled")}");

            OnSettingsChanged?.Invoke(currentSettings);
        }

        /// <summary>
        /// Sets kill feed visibility
        /// </summary>
        public void SetShowKillFeed(bool show)
        {
            currentSettings.showKillFeed = show;

            if (showDebugLogs)
                Debug.Log($"[SettingsManager] Show kill feed {(show ? "enabled" : "disabled")}");

            OnSettingsChanged?.Invoke(currentSettings);
        }

        /// <summary>
        /// Sets crosshair visibility
        /// </summary>
        public void SetShowCrosshair(bool show)
        {
            currentSettings.showCrosshair = show;

            if (showDebugLogs)
                Debug.Log($"[SettingsManager] Show crosshair {(show ? "enabled" : "disabled")}");

            OnSettingsChanged?.Invoke(currentSettings);
        }

        /// <summary>
        /// Sets crosshair style
        /// </summary>
        public void SetCrosshairStyle(int style)
        {
            currentSettings.crosshairStyle = style;

            if (showDebugLogs)
                Debug.Log($"[SettingsManager] Crosshair style set to {style}");

            OnSettingsChanged?.Invoke(currentSettings);
        }

        /// <summary>
        /// Sets crosshair scale
        /// </summary>
        public void SetCrosshairScale(float scale)
        {
            scale = Mathf.Clamp(scale, 0.5f, 2f);
            currentSettings.crosshairScale = scale;

            if (showDebugLogs)
                Debug.Log($"[SettingsManager] Crosshair scale set to {scale}");

            OnSettingsChanged?.Invoke(currentSettings);
        }

        /// <summary>
        /// Sets field of view
        /// </summary>
        public void SetFOV(int fov)
        {
            fov = Mathf.Clamp(fov, 60, 120);
            currentSettings.fovValue = fov;

            // Apply to cameras
            var cameras = FindObjectsOfType<Camera>();
            foreach (var cam in cameras)
            {
                cam.fieldOfView = fov;
            }

            if (showDebugLogs)
                Debug.Log($"[SettingsManager] FOV set to {fov}");

            OnSettingsChanged?.Invoke(currentSettings);
        }

        #endregion

        #region Accessibility Settings

        /// <summary>
        /// Sets colorblind mode
        /// </summary>
        public void SetColorBlindMode(bool enabled)
        {
            currentSettings.colorBlindMode = enabled;

            if (showDebugLogs)
                Debug.Log($"[SettingsManager] Colorblind mode {(enabled ? "enabled" : "disabled")}");

            OnSettingsChanged?.Invoke(currentSettings);
        }

        /// <summary>
        /// Sets colorblind type
        /// </summary>
        public void SetColorBlindType(int type)
        {
            currentSettings.colorBlindType = Mathf.Clamp(type, 0, 2);

            if (showDebugLogs)
                Debug.Log($"[SettingsManager] Colorblind type set to {type}");

            OnSettingsChanged?.Invoke(currentSettings);
        }

        /// <summary>
        /// Sets subtitles
        /// </summary>
        public void SetSubtitles(bool enabled)
        {
            currentSettings.subtitles = enabled;

            if (showDebugLogs)
                Debug.Log($"[SettingsManager] Subtitles {(enabled ? "enabled" : "disabled")}");

            OnSettingsChanged?.Invoke(currentSettings);
        }

        /// <summary>
        /// Sets subtitle size
        /// </summary>
        public void SetSubtitleSize(float size)
        {
            size = Mathf.Clamp(size, 0.5f, 2f);
            currentSettings.subtitleSize = size;

            if (showDebugLogs)
                Debug.Log($"[SettingsManager] Subtitle size set to {size}");

            OnSettingsChanged?.Invoke(currentSettings);
        }

        /// <summary>
        /// Sets screen shake
        /// </summary>
        public void SetScreenShake(bool enabled)
        {
            currentSettings.screenShake = enabled;

            if (showDebugLogs)
                Debug.Log($"[SettingsManager] Screen shake {(enabled ? "enabled" : "disabled")}");

            OnSettingsChanged?.Invoke(currentSettings);
        }

        /// <summary>
        /// Sets screen shake intensity
        /// </summary>
        public void SetScreenShakeIntensity(float intensity)
        {
            intensity = Mathf.Clamp01(intensity);
            currentSettings.screenShakeIntensity = intensity;

            if (showDebugLogs)
                Debug.Log($"[SettingsManager] Screen shake intensity set to {intensity}");

            OnSettingsChanged?.Invoke(currentSettings);
        }

        #endregion

        #region Network Settings

        /// <summary>
        /// Sets maximum acceptable ping
        /// </summary>
        public void SetMaxPing(int ping)
        {
            currentSettings.maxPing = Mathf.Clamp(ping, 50, 300);

            if (showDebugLogs)
                Debug.Log($"[SettingsManager] Max ping set to {ping}");

            OnSettingsChanged?.Invoke(currentSettings);
        }

        /// <summary>
        /// Sets preferred region
        /// </summary>
        public void SetPreferredRegion(string region)
        {
            currentSettings.preferredRegion = region;

            if (showDebugLogs)
                Debug.Log($"[SettingsManager] Preferred region set to {region}");

            OnSettingsChanged?.Invoke(currentSettings);
        }

        #endregion

        #region Save/Load

        /// <summary>
        /// Saves current settings
        /// </summary>
        public void SaveSettings()
        {
            if (Save.SaveSystem.Instance != null && Save.SaveSystem.Instance.CurrentSave != null)
            {
                Save.SaveSystem.Instance.CurrentSave.settings = currentSettings;
                Save.SaveSystem.Instance.SaveGame(Save.SaveSystem.Instance.CurrentSlot);

                if (showDebugLogs)
                    Debug.Log("[SettingsManager] Settings saved");
            }
        }

        /// <summary>
        /// Loads settings
        /// </summary>
        public void LoadSettings()
        {
            if (Save.SaveSystem.Instance != null && Save.SaveSystem.Instance.CurrentSave != null)
            {
                currentSettings = Save.SaveSystem.Instance.CurrentSave.settings;

                if (showDebugLogs)
                    Debug.Log("[SettingsManager] Settings loaded");

                ApplyAllSettings();
            }
        }

        /// <summary>
        /// Resets settings to defaults
        /// </summary>
        public void ResetToDefaults()
        {
            currentSettings = defaultSettings ?? new Save.SettingsData();
            ApplyAllSettings();

            if (showDebugLogs)
                Debug.Log("[SettingsManager] Settings reset to defaults");

            OnSettingsChanged?.Invoke(currentSettings);
        }

        private void ApplyAllSettings()
        {
            // Apply graphics
            SetQualityLevel(currentSettings.qualityLevel);
            SetResolution(currentSettings.resolutionWidth, currentSettings.resolutionHeight, currentSettings.fullscreen);
            SetTargetFrameRate(currentSettings.targetFrameRate);
            SetVSync(currentSettings.vsyncEnabled);

            // Apply audio
            SetMasterVolume(currentSettings.masterVolume);
            SetMusicVolume(currentSettings.musicVolume);
            SetSFXVolume(currentSettings.sfxVolume);
            SetVoiceVolume(currentSettings.voiceVolume);

            // Apply controls
            // Mouse sensitivity will be read by PlayerCamera

            // Apply gameplay
            SetFOV(currentSettings.fovValue);

            if (showDebugLogs)
                Debug.Log("[SettingsManager] All settings applied");
        }

        #endregion

        private void OnApplicationQuit()
        {
            SaveSettings();
        }
    }

    #region Event Data Structures

    public struct GraphicsSettingsChange
    {
        public int qualityLevel;
        public int resolutionWidth;
        public int resolutionHeight;
        public bool fullscreen;
        public int targetFrameRate;
        public bool vsyncEnabled;
    }

    public struct AudioSettingsChange
    {
        public float masterVolume;
        public float musicVolume;
        public float sfxVolume;
        public float voiceVolume;
    }

    public struct ControlSettingsChange
    {
        public float mouseSensitivity;
        public float aimSensitivity;
        public bool invertY;
    }

    #endregion
}
