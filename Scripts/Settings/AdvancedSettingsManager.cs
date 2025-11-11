using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace ZombieGame
{
    /// <summary>
    /// Advanced Settings Manager - Centralized settings control
    /// Features: Graphics presets, audio mixing, gameplay tweaks, profile management
    /// Essential for user customization and comfort
    /// </summary>
    public class AdvancedSettingsManager : MonoBehaviour
    {
        public static AdvancedSettingsManager Instance { get; private set; }

        [Header("Graphics Settings")]
        [SerializeField] private GraphicsPreset currentPreset = GraphicsPreset.High;
        [SerializeField] private int targetFrameRate = 60;
        [SerializeField] private bool vSyncEnabled = true;
        [SerializeField] private bool fullscreen = true;
        [SerializeField] private Resolution currentResolution;

        [Header("Audio Settings")]
        [SerializeField] private float masterVolume = 1.0f;
        [SerializeField] private float musicVolume = 0.8f;
        [SerializeField] private float sfxVolume = 1.0f;
        [SerializeField] private float voiceVolume = 1.0f;
        [SerializeField] private float uiVolume = 0.9f;

        [Header("Gameplay Settings")]
        [SerializeField] private float mouseSensitivity = 1.0f;
        [SerializeField] private bool invertY = false;
        [SerializeField] private float fovValue = 90f;
        [SerializeField] private bool headBobEnabled = true;
        [SerializeField] private float headBobIntensity = 1.0f;

        private Dictionary<string, SettingsProfile> profiles = new Dictionary<string, SettingsProfile>();
        private string currentProfileId = "default";

        // Events
        public event System.Action OnSettingsChanged;
        public event System.Action<GraphicsPreset> OnGraphicsPresetChanged;

        [System.Serializable]
        public class SettingsProfile
        {
            public string profileId;
            public string profileName;
            public GraphicsSettings graphics;
            public AudioSettings audio;
            public GameplaySettings gameplay;
        }

        [System.Serializable]
        public class GraphicsSettings
        {
            public GraphicsPreset preset;
            public int targetFPS;
            public bool vsync;
            public bool fullscreen;
            public int resolutionWidth;
            public int resolutionHeight;
            public int textureQuality;
            public int shadowQuality;
            public int antiAliasing;
            public bool postProcessing;
            public bool motionBlur;
            public bool bloom;
            public bool ambientOcclusion;
        }

        [System.Serializable]
        public class AudioSettings
        {
            public float master;
            public float music;
            public float sfx;
            public float voice;
            public float ui;
        }

        [System.Serializable]
        public class GameplaySettings
        {
            public float mouseSensitivity;
            public bool invertY;
            public float fov;
            public bool headBob;
            public float headBobIntensity;
            public bool showFPS;
            public bool showPing;
            public bool showDamageNumbers;
        }

        public enum GraphicsPreset
        {
            Low,
            Medium,
            High,
            Ultra,
            Custom
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                LoadSettings();
                ApplySettings();
            }
            else { Destroy(gameObject); }
        }

        // Graphics Settings

        public void SetGraphicsPreset(GraphicsPreset preset)
        {
            currentPreset = preset;
            ApplyGraphicsPreset(preset);
            OnGraphicsPresetChanged?.Invoke(preset);
            SaveSettings();

            Debug.Log($"[Settings] Graphics preset set to: {preset}");
        }

        private void ApplyGraphicsPreset(GraphicsPreset preset)
        {
            switch (preset)
            {
                case GraphicsPreset.Low:
                    QualitySettings.SetQualityLevel(0);
                    targetFrameRate = 30;
                    QualitySettings.shadows = ShadowQuality.Disable;
                    QualitySettings.antiAliasing = 0;
                    break;

                case GraphicsPreset.Medium:
                    QualitySettings.SetQualityLevel(2);
                    targetFrameRate = 60;
                    QualitySettings.shadows = ShadowQuality.HardOnly;
                    QualitySettings.antiAliasing = 2;
                    break;

                case GraphicsPreset.High:
                    QualitySettings.SetQualityLevel(4);
                    targetFrameRate = 60;
                    QualitySettings.shadows = ShadowQuality.All;
                    QualitySettings.antiAliasing = 4;
                    break;

                case GraphicsPreset.Ultra:
                    QualitySettings.SetQualityLevel(5);
                    targetFrameRate = 120;
                    QualitySettings.shadows = ShadowQuality.All;
                    QualitySettings.antiAliasing = 8;
                    break;
            }

            Application.targetFrameRate = targetFrameRate;
        }

        public void SetTargetFrameRate(int fps)
        {
            targetFrameRate = Mathf.Clamp(fps, 30, 240);
            Application.targetFrameRate = targetFrameRate;
            SaveSettings();
        }

        public void SetVSync(bool enabled)
        {
            vSyncEnabled = enabled;
            QualitySettings.vSyncCount = enabled ? 1 : 0;
            SaveSettings();
        }

        public void SetFullscreen(bool enabled)
        {
            fullscreen = enabled;
            Screen.fullScreen = enabled;
            SaveSettings();
        }

        public void SetResolution(int width, int height)
        {
            currentResolution = new Resolution { width = width, height = height };
            Screen.SetResolution(width, height, fullscreen);
            SaveSettings();
        }

        public void SetTextureQuality(int quality)
        {
            QualitySettings.masterTextureLimit = 3 - Mathf.Clamp(quality, 0, 3);
            SaveSettings();
        }

        public void SetShadowQuality(int quality)
        {
            QualitySettings.shadows = (ShadowQuality)Mathf.Clamp(quality, 0, 2);
            SaveSettings();
        }

        public void SetAntiAliasing(int level)
        {
            QualitySettings.antiAliasing = level;
            SaveSettings();
        }

        // Audio Settings

        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            AudioListener.volume = masterVolume;
            OnSettingsChanged?.Invoke();
            SaveSettings();
        }

        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            // Would set music audio mixer group volume
            OnSettingsChanged?.Invoke();
            SaveSettings();
        }

        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            // Would set SFX audio mixer group volume
            OnSettingsChanged?.Invoke();
            SaveSettings();
        }

        public void SetVoiceVolume(float volume)
        {
            voiceVolume = Mathf.Clamp01(volume);
            OnSettingsChanged?.Invoke();
            SaveSettings();
        }

        public void SetUIVolume(float volume)
        {
            uiVolume = Mathf.Clamp01(volume);
            OnSettingsChanged?.Invoke();
            SaveSettings();
        }

        // Gameplay Settings

        public void SetMouseSensitivity(float sensitivity)
        {
            mouseSensitivity = Mathf.Clamp(sensitivity, 0.1f, 5.0f);
            OnSettingsChanged?.Invoke();
            SaveSettings();
        }

        public void SetInvertY(bool invert)
        {
            invertY = invert;
            OnSettingsChanged?.Invoke();
            SaveSettings();
        }

        public void SetFOV(float fov)
        {
            fovValue = Mathf.Clamp(fov, 60f, 120f);
            // Would update camera FOV
            OnSettingsChanged?.Invoke();
            SaveSettings();
        }

        public void SetHeadBob(bool enabled, float intensity = 1.0f)
        {
            headBobEnabled = enabled;
            headBobIntensity = Mathf.Clamp01(intensity);
            OnSettingsChanged?.Invoke();
            SaveSettings();
        }

        // Profile Management

        public void SaveProfile(string profileId, string profileName)
        {
            var profile = new SettingsProfile
            {
                profileId = profileId,
                profileName = profileName,
                graphics = new GraphicsSettings
                {
                    preset = currentPreset,
                    targetFPS = targetFrameRate,
                    vsync = vSyncEnabled,
                    fullscreen = fullscreen
                },
                audio = new AudioSettings
                {
                    master = masterVolume,
                    music = musicVolume,
                    sfx = sfxVolume,
                    voice = voiceVolume,
                    ui = uiVolume
                },
                gameplay = new GameplaySettings
                {
                    mouseSensitivity = mouseSensitivity,
                    invertY = invertY,
                    fov = fovValue,
                    headBob = headBobEnabled,
                    headBobIntensity = headBobIntensity
                }
            };

            profiles[profileId] = profile;
            string json = JsonUtility.ToJson(profile, true);
            PlayerPrefs.SetString($"SettingsProfile_{profileId}", json);
            PlayerPrefs.Save();

            Debug.Log($"[Settings] Profile saved: {profileName}");
        }

        public void LoadProfile(string profileId)
        {
            string json = PlayerPrefs.GetString($"SettingsProfile_{profileId}", "");
            if (string.IsNullOrEmpty(json)) return;

            var profile = JsonUtility.FromJson<SettingsProfile>(json);
            ApplyProfile(profile);
            currentProfileId = profileId;

            Debug.Log($"[Settings] Profile loaded: {profile.profileName}");
        }

        private void ApplyProfile(SettingsProfile profile)
        {
            // Apply graphics
            SetGraphicsPreset(profile.graphics.preset);
            SetTargetFrameRate(profile.graphics.targetFPS);
            SetVSync(profile.graphics.vsync);
            SetFullscreen(profile.graphics.fullscreen);

            // Apply audio
            SetMasterVolume(profile.audio.master);
            SetMusicVolume(profile.audio.music);
            SetSFXVolume(profile.audio.sfx);
            SetVoiceVolume(profile.audio.voice);
            SetUIVolume(profile.audio.ui);

            // Apply gameplay
            SetMouseSensitivity(profile.gameplay.mouseSensitivity);
            SetInvertY(profile.gameplay.invertY);
            SetFOV(profile.gameplay.fov);
            SetHeadBob(profile.gameplay.headBob, profile.gameplay.headBobIntensity);
        }

        public void ResetToDefaults()
        {
            SetGraphicsPreset(GraphicsPreset.High);
            SetTargetFrameRate(60);
            SetVSync(true);
            SetMasterVolume(1.0f);
            SetMusicVolume(0.8f);
            SetSFXVolume(1.0f);
            SetVoiceVolume(1.0f);
            SetUIVolume(0.9f);
            SetMouseSensitivity(1.0f);
            SetInvertY(false);
            SetFOV(90f);
            SetHeadBob(true, 1.0f);

            Debug.Log("[Settings] Reset to defaults");
        }

        private void ApplySettings()
        {
            ApplyGraphicsPreset(currentPreset);
            AudioListener.volume = masterVolume;
            Application.targetFrameRate = targetFrameRate;
            QualitySettings.vSyncCount = vSyncEnabled ? 1 : 0;
        }

        private void SaveSettings()
        {
            PlayerPrefs.SetInt("Settings_GraphicsPreset", (int)currentPreset);
            PlayerPrefs.SetInt("Settings_TargetFPS", targetFrameRate);
            PlayerPrefs.SetInt("Settings_VSync", vSyncEnabled ? 1 : 0);
            PlayerPrefs.SetInt("Settings_Fullscreen", fullscreen ? 1 : 0);
            PlayerPrefs.SetFloat("Settings_MasterVolume", masterVolume);
            PlayerPrefs.SetFloat("Settings_MusicVolume", musicVolume);
            PlayerPrefs.SetFloat("Settings_SFXVolume", sfxVolume);
            PlayerPrefs.SetFloat("Settings_VoiceVolume", voiceVolume);
            PlayerPrefs.SetFloat("Settings_UIVolume", uiVolume);
            PlayerPrefs.SetFloat("Settings_MouseSensitivity", mouseSensitivity);
            PlayerPrefs.SetInt("Settings_InvertY", invertY ? 1 : 0);
            PlayerPrefs.SetFloat("Settings_FOV", fovValue);
            PlayerPrefs.SetInt("Settings_HeadBob", headBobEnabled ? 1 : 0);
            PlayerPrefs.SetFloat("Settings_HeadBobIntensity", headBobIntensity);
            PlayerPrefs.Save();
        }

        private void LoadSettings()
        {
            currentPreset = (GraphicsPreset)PlayerPrefs.GetInt("Settings_GraphicsPreset", 2);
            targetFrameRate = PlayerPrefs.GetInt("Settings_TargetFPS", 60);
            vSyncEnabled = PlayerPrefs.GetInt("Settings_VSync", 1) == 1;
            fullscreen = PlayerPrefs.GetInt("Settings_Fullscreen", 1) == 1;
            masterVolume = PlayerPrefs.GetFloat("Settings_MasterVolume", 1.0f);
            musicVolume = PlayerPrefs.GetFloat("Settings_MusicVolume", 0.8f);
            sfxVolume = PlayerPrefs.GetFloat("Settings_SFXVolume", 1.0f);
            voiceVolume = PlayerPrefs.GetFloat("Settings_VoiceVolume", 1.0f);
            uiVolume = PlayerPrefs.GetFloat("Settings_UIVolume", 0.9f);
            mouseSensitivity = PlayerPrefs.GetFloat("Settings_MouseSensitivity", 1.0f);
            invertY = PlayerPrefs.GetInt("Settings_InvertY", 0) == 1;
            fovValue = PlayerPrefs.GetFloat("Settings_FOV", 90f);
            headBobEnabled = PlayerPrefs.GetInt("Settings_HeadBob", 1) == 1;
            headBobIntensity = PlayerPrefs.GetFloat("Settings_HeadBobIntensity", 1.0f);

            Debug.Log("[Settings] Settings loaded");
        }

        // Getters
        public GraphicsPreset GetCurrentPreset() => currentPreset;
        public float GetMasterVolume() => masterVolume;
        public float GetMusicVolume() => musicVolume;
        public float GetSFXVolume() => sfxVolume;
        public float GetMouseSensitivity() => mouseSensitivity;
        public float GetFOV() => fovValue;
        public bool IsInvertYEnabled() => invertY;
    }
}
