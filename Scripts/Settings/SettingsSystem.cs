using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Settings
{
    /// <summary>
    /// Comprehensive settings and preferences system managing all game configuration
    /// including graphics, audio, gameplay, controls, accessibility, and network preferences.
    /// </summary>
    public class SettingsSystem : NetworkBehaviour
    {
        public static SettingsSystem Instance { get; private set; }

        [Header("Settings Configuration")]
        [SerializeField] private string settingsFileName = "GameSettings.json";
        [SerializeField] private bool autoSaveEnabled = true;
        [SerializeField] private float autoSaveInterval = 30f;

        private Dictionary<ulong, PlayerSettings> playerSettings = new Dictionary<ulong, PlayerSettings>();
        private float lastAutoSaveTime;

        public event Action<ulong, SettingsCategory> OnSettingsChanged;
        public event Action<ulong> OnSettingsSaved;
        public event Action<ulong> OnSettingsReset;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Update()
        {
            if (autoSaveEnabled && Time.time - lastAutoSaveTime >= autoSaveInterval)
            {
                AutoSaveAllSettings();
                lastAutoSaveTime = Time.time;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void InitializePlayerSettingsServerRpc(ulong playerId, ServerRpcParams rpcParams = default)
        {
            if (!playerSettings.ContainsKey(playerId))
            {
                playerSettings[playerId] = CreateDefaultSettings();
                LoadPlayerSettingsFromDisk(playerId);
            }
        }

        // Graphics Settings
        [ServerRpc(RequireOwnership = false)]
        public void UpdateGraphicsSettingsServerRpc(ulong playerId, GraphicsSettings graphics, ServerRpcParams rpcParams = default)
        {
            if (!playerSettings.ContainsKey(playerId)) return;

            playerSettings[playerId].graphics = graphics;
            OnSettingsChanged?.Invoke(playerId, SettingsCategory.Graphics);
            UpdateGraphicsClientRpc(playerId, graphics);
        }

        [ClientRpc]
        private void UpdateGraphicsClientRpc(ulong playerId, GraphicsSettings graphics)
        {
            ApplyGraphicsSettings(graphics);
        }

        private void ApplyGraphicsSettings(GraphicsSettings graphics)
        {
            QualitySettings.SetQualityLevel((int)graphics.qualityPreset);
            Screen.SetResolution(graphics.resolutionWidth, graphics.resolutionHeight, graphics.fullscreenMode);
            QualitySettings.vSyncCount = graphics.vsyncEnabled ? 1 : 0;
            Application.targetFrameRate = graphics.targetFrameRate;
            QualitySettings.shadows = graphics.shadowQuality;
            QualitySettings.antiAliasing = graphics.antiAliasingLevel;
            QualitySettings.anisotropicFiltering = graphics.anisotropicFiltering;
        }

        // Audio Settings
        [ServerRpc(RequireOwnership = false)]
        public void UpdateAudioSettingsServerRpc(ulong playerId, AudioSettings audio, ServerRpcParams rpcParams = default)
        {
            if (!playerSettings.ContainsKey(playerId)) return;

            playerSettings[playerId].audio = audio;
            OnSettingsChanged?.Invoke(playerId, SettingsCategory.Audio);
            UpdateAudioClientRpc(playerId, audio);
        }

        [ClientRpc]
        private void UpdateAudioClientRpc(ulong playerId, AudioSettings audio)
        {
            ApplyAudioSettings(audio);
        }

        private void ApplyAudioSettings(AudioSettings audio)
        {
            AudioListener.volume = audio.masterVolume;
            // Apply to audio mixer groups
            // AudioMixer.SetFloat("MusicVolume", audio.musicVolume);
            // AudioMixer.SetFloat("SFXVolume", audio.sfxVolume);
            // AudioMixer.SetFloat("VoiceVolume", audio.voiceVolume);
            // AudioMixer.SetFloat("AmbienceVolume", audio.ambienceVolume);
        }

        // Gameplay Settings
        [ServerRpc(RequireOwnership = false)]
        public void UpdateGameplaySettingsServerRpc(ulong playerId, GameplaySettings gameplay, ServerRpcParams rpcParams = default)
        {
            if (!playerSettings.ContainsKey(playerId)) return;

            playerSettings[playerId].gameplay = gameplay;
            OnSettingsChanged?.Invoke(playerId, SettingsCategory.Gameplay);
            UpdateGameplayClientRpc(playerId, gameplay);
        }

        [ClientRpc]
        private void UpdateGameplayClientRpc(ulong playerId, GameplaySettings gameplay)
        {
            // Apply gameplay settings
        }

        // Control Settings
        [ServerRpc(RequireOwnership = false)]
        public void UpdateControlSettingsServerRpc(ulong playerId, ControlSettings controls, ServerRpcParams rpcParams = default)
        {
            if (!playerSettings.ContainsKey(playerId)) return;

            playerSettings[playerId].controls = controls;
            OnSettingsChanged?.Invoke(playerId, SettingsCategory.Controls);
            UpdateControlsClientRpc(playerId, controls);
        }

        [ClientRpc]
        private void UpdateControlsClientRpc(ulong playerId, ControlSettings controls)
        {
            // Apply control settings
        }

        // Accessibility Settings
        [ServerRpc(RequireOwnership = false)]
        public void UpdateAccessibilitySettingsServerRpc(ulong playerId, AccessibilitySettings accessibility, ServerRpcParams rpcParams = default)
        {
            if (!playerSettings.ContainsKey(playerId)) return;

            playerSettings[playerId].accessibility = accessibility;
            OnSettingsChanged?.Invoke(playerId, SettingsCategory.Accessibility);
            UpdateAccessibilityClientRpc(playerId, accessibility);
        }

        [ClientRpc]
        private void UpdateAccessibilityClientRpc(ulong playerId, AccessibilitySettings accessibility)
        {
            // Apply accessibility settings
        }

        // Network Settings
        [ServerRpc(RequireOwnership = false)]
        public void UpdateNetworkSettingsServerRpc(ulong playerId, NetworkSettings network, ServerRpcParams rpcParams = default)
        {
            if (!playerSettings.ContainsKey(playerId)) return;

            playerSettings[playerId].network = network;
            OnSettingsChanged?.Invoke(playerId, SettingsCategory.Network);
        }

        // Privacy Settings
        [ServerRpc(RequireOwnership = false)]
        public void UpdatePrivacySettingsServerRpc(ulong playerId, PrivacySettings privacy, ServerRpcParams rpcParams = default)
        {
            if (!playerSettings.ContainsKey(playerId)) return;

            playerSettings[playerId].privacy = privacy;
            OnSettingsChanged?.Invoke(playerId, SettingsCategory.Privacy);
        }

        // Save/Load/Reset
        [ServerRpc(RequireOwnership = false)]
        public void SavePlayerSettingsServerRpc(ulong playerId, ServerRpcParams rpcParams = default)
        {
            if (!playerSettings.ContainsKey(playerId)) return;

            SavePlayerSettingsToDisk(playerId);
            OnSettingsSaved?.Invoke(playerId);
            Debug.Log($"Settings saved for player {playerId}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void ResetToDefaultsServerRpc(ulong playerId, SettingsCategory category, ServerRpcParams rpcParams = default)
        {
            if (!playerSettings.ContainsKey(playerId)) return;

            var defaults = CreateDefaultSettings();

            switch (category)
            {
                case SettingsCategory.Graphics:
                    playerSettings[playerId].graphics = defaults.graphics;
                    UpdateGraphicsClientRpc(playerId, defaults.graphics);
                    break;
                case SettingsCategory.Audio:
                    playerSettings[playerId].audio = defaults.audio;
                    UpdateAudioClientRpc(playerId, defaults.audio);
                    break;
                case SettingsCategory.Gameplay:
                    playerSettings[playerId].gameplay = defaults.gameplay;
                    UpdateGameplayClientRpc(playerId, defaults.gameplay);
                    break;
                case SettingsCategory.Controls:
                    playerSettings[playerId].controls = defaults.controls;
                    UpdateControlsClientRpc(playerId, defaults.controls);
                    break;
                case SettingsCategory.Accessibility:
                    playerSettings[playerId].accessibility = defaults.accessibility;
                    UpdateAccessibilityClientRpc(playerId, defaults.accessibility);
                    break;
                case SettingsCategory.Network:
                    playerSettings[playerId].network = defaults.network;
                    break;
                case SettingsCategory.Privacy:
                    playerSettings[playerId].privacy = defaults.privacy;
                    break;
                case SettingsCategory.All:
                    playerSettings[playerId] = defaults;
                    UpdateGraphicsClientRpc(playerId, defaults.graphics);
                    UpdateAudioClientRpc(playerId, defaults.audio);
                    UpdateGameplayClientRpc(playerId, defaults.gameplay);
                    UpdateControlsClientRpc(playerId, defaults.controls);
                    UpdateAccessibilityClientRpc(playerId, defaults.accessibility);
                    break;
            }

            OnSettingsReset?.Invoke(playerId);
            Debug.Log($"Settings reset for player {playerId} - Category: {category}");
        }

        private PlayerSettings CreateDefaultSettings()
        {
            return new PlayerSettings
            {
                graphics = new GraphicsSettings
                {
                    qualityPreset = QualityPreset.High,
                    resolutionWidth = 1920,
                    resolutionHeight = 1080,
                    fullscreenMode = FullScreenMode.ExclusiveFullScreen,
                    vsyncEnabled = true,
                    targetFrameRate = 60,
                    shadowQuality = ShadowQuality.All,
                    antiAliasingLevel = 4,
                    anisotropicFiltering = AnisotropicFiltering.Enable,
                    textureQuality = TextureQuality.High,
                    viewDistance = 1000f,
                    fovValue = 90f,
                    motionBlurEnabled = true,
                    bloomEnabled = true,
                    ambientOcclusionEnabled = true
                },
                audio = new AudioSettings
                {
                    masterVolume = 1.0f,
                    musicVolume = 0.7f,
                    sfxVolume = 0.8f,
                    voiceVolume = 1.0f,
                    ambienceVolume = 0.5f,
                    uiVolume = 0.6f,
                    mutedWhenUnfocused = true,
                    dynamicRangeCompression = false,
                    subtitlesEnabled = false,
                    voiceChatEnabled = true
                },
                gameplay = new GameplaySettings
                {
                    difficulty = DifficultyLevel.Normal,
                    autoSaveEnabled = true,
                    tutorialEnabled = true,
                    damageNumbers = true,
                    hitmarkers = true,
                    crosshairEnabled = true,
                    crosshairStyle = CrosshairStyle.Cross,
                    aimAssistEnabled = false,
                    friendlyFire = false,
                    killFeedEnabled = true,
                    minimapEnabled = true,
                    minimapRotation = true
                },
                controls = new ControlSettings
                {
                    mouseSensitivity = 1.0f,
                    aimSensitivity = 0.8f,
                    invertY = false,
                    invertX = false,
                    toggleADS = false,
                    toggleCrouch = false,
                    toggleSprint = false,
                    holdToInteract = true,
                    controlScheme = ControlScheme.Default,
                    vibrationEnabled = true,
                    vibrationStrength = 1.0f
                },
                accessibility = new AccessibilitySettings
                {
                    colorblindMode = ColorblindMode.None,
                    subtitlesEnabled = false,
                    subtitleSize = SubtitleSize.Medium,
                    highContrastUI = false,
                    largeText = false,
                    screenReaderEnabled = false,
                    buttonHoldDuration = 0.5f,
                    photosensitivityMode = false,
                    reduceMotion = false,
                    screenShakeIntensity = 1.0f
                },
                network = new NetworkSettings
                {
                    preferredRegion = NetworkRegion.Auto,
                    maxPing = 100,
                    crossplayEnabled = true,
                    voiceChatEnabled = true,
                    voiceChatMode = VoiceChatMode.PushToTalk,
                    textChatEnabled = true,
                    showPlayerNames = true,
                    showPlayerLevels = true
                },
                privacy = new PrivacySettings
                {
                    profileVisibility = ProfileVisibility.FriendsOnly,
                    allowPartyInvites = true,
                    allowFriendRequests = true,
                    allowTradeRequests = true,
                    showOnlineStatus = true,
                    allowSpectators = false,
                    dataSharingEnabled = false,
                    analyticsEnabled = true
                }
            };
        }

        private void SavePlayerSettingsToDisk(ulong playerId)
        {
            if (!playerSettings.TryGetValue(playerId, out var settings)) return;

            string json = JsonUtility.ToJson(settings, true);
            string path = System.IO.Path.Combine(Application.persistentDataPath, $"{playerId}_{settingsFileName}");
            System.IO.File.WriteAllText(path, json);
        }

        private void LoadPlayerSettingsFromDisk(ulong playerId)
        {
            string path = System.IO.Path.Combine(Application.persistentDataPath, $"{playerId}_{settingsFileName}");
            
            if (System.IO.File.Exists(path))
            {
                string json = System.IO.File.ReadAllText(path);
                var settings = JsonUtility.FromJson<PlayerSettings>(json);
                playerSettings[playerId] = settings;
                Debug.Log($"Loaded settings for player {playerId}");
            }
        }

        private void AutoSaveAllSettings()
        {
            foreach (var playerId in playerSettings.Keys)
            {
                SavePlayerSettingsToDisk(playerId);
            }
        }

        public PlayerSettings GetPlayerSettings(ulong playerId) => playerSettings.GetValueOrDefault(playerId, CreateDefaultSettings());
        public GraphicsSettings GetGraphicsSettings(ulong playerId) => playerSettings.GetValueOrDefault(playerId)?.graphics ?? CreateDefaultSettings().graphics;
        public AudioSettings GetAudioSettings(ulong playerId) => playerSettings.GetValueOrDefault(playerId)?.audio ?? CreateDefaultSettings().audio;
        public GameplaySettings GetGameplaySettings(ulong playerId) => playerSettings.GetValueOrDefault(playerId)?.gameplay ?? CreateDefaultSettings().gameplay;
    }

    [Serializable]
    public class PlayerSettings
    {
        public GraphicsSettings graphics;
        public AudioSettings audio;
        public GameplaySettings gameplay;
        public ControlSettings controls;
        public AccessibilitySettings accessibility;
        public NetworkSettings network;
        public PrivacySettings privacy;
    }

    [Serializable]
    public class GraphicsSettings
    {
        public QualityPreset qualityPreset;
        public int resolutionWidth;
        public int resolutionHeight;
        public FullScreenMode fullscreenMode;
        public bool vsyncEnabled;
        public int targetFrameRate;
        public ShadowQuality shadowQuality;
        public int antiAliasingLevel;
        public AnisotropicFiltering anisotropicFiltering;
        public TextureQuality textureQuality;
        public float viewDistance;
        public float fovValue;
        public bool motionBlurEnabled;
        public bool bloomEnabled;
        public bool ambientOcclusionEnabled;
    }

    [Serializable]
    public class AudioSettings
    {
        public float masterVolume;
        public float musicVolume;
        public float sfxVolume;
        public float voiceVolume;
        public float ambienceVolume;
        public float uiVolume;
        public bool mutedWhenUnfocused;
        public bool dynamicRangeCompression;
        public bool subtitlesEnabled;
        public bool voiceChatEnabled;
    }

    [Serializable]
    public class GameplaySettings
    {
        public DifficultyLevel difficulty;
        public bool autoSaveEnabled;
        public bool tutorialEnabled;
        public bool damageNumbers;
        public bool hitmarkers;
        public bool crosshairEnabled;
        public CrosshairStyle crosshairStyle;
        public bool aimAssistEnabled;
        public bool friendlyFire;
        public bool killFeedEnabled;
        public bool minimapEnabled;
        public bool minimapRotation;
    }

    [Serializable]
    public class ControlSettings
    {
        public float mouseSensitivity;
        public float aimSensitivity;
        public bool invertY;
        public bool invertX;
        public bool toggleADS;
        public bool toggleCrouch;
        public bool toggleSprint;
        public bool holdToInteract;
        public ControlScheme controlScheme;
        public bool vibrationEnabled;
        public float vibrationStrength;
        public Dictionary<string, KeyCode> keyBindings;
    }

    [Serializable]
    public class AccessibilitySettings
    {
        public ColorblindMode colorblindMode;
        public bool subtitlesEnabled;
        public SubtitleSize subtitleSize;
        public bool highContrastUI;
        public bool largeText;
        public bool screenReaderEnabled;
        public float buttonHoldDuration;
        public bool photosensitivityMode;
        public bool reduceMotion;
        public float screenShakeIntensity;
    }

    [Serializable]
    public class NetworkSettings
    {
        public NetworkRegion preferredRegion;
        public int maxPing;
        public bool crossplayEnabled;
        public bool voiceChatEnabled;
        public VoiceChatMode voiceChatMode;
        public bool textChatEnabled;
        public bool showPlayerNames;
        public bool showPlayerLevels;
    }

    [Serializable]
    public class PrivacySettings
    {
        public ProfileVisibility profileVisibility;
        public bool allowPartyInvites;
        public bool allowFriendRequests;
        public bool allowTradeRequests;
        public bool showOnlineStatus;
        public bool allowSpectators;
        public bool dataSharingEnabled;
        public bool analyticsEnabled;
    }

    public enum SettingsCategory { Graphics, Audio, Gameplay, Controls, Accessibility, Network, Privacy, All }
    public enum QualityPreset { Low, Medium, High, Ultra, Custom }
    public enum TextureQuality { Low, Medium, High, VeryHigh }
    public enum DifficultyLevel { Easy, Normal, Hard, Expert }
    public enum CrosshairStyle { Dot, Cross, Circle, Custom }
    public enum ControlScheme { Default, Alternative, Custom }
    public enum ColorblindMode { None, Protanopia, Deuteranopia, Tritanopia }
    public enum SubtitleSize { Small, Medium, Large, ExtraLarge }
    public enum NetworkRegion { Auto, NorthAmerica, Europe, Asia, SouthAmerica, Oceania }
    public enum VoiceChatMode { PushToTalk, AlwaysOn, VoiceActivated }
    public enum ProfileVisibility { Public, FriendsOnly, Private }
}
