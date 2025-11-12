using UnityEngine;
using System.Collections.Generic;

namespace ZombieGame
{
    /// <summary>
    /// Advanced Audio Manager - Professional audio mixing and control
    /// Features: Audio mixer groups, 3D spatial audio, occlusion, dynamic mixing
    /// Essential for immersive audio experience
    /// </summary>
    public class AdvancedAudioManager : MonoBehaviour
    {
        public static AdvancedAudioManager Instance { get; private set; }

        [Header("Audio Settings")]
        [SerializeField] private bool enable3DAudio = true;
        [SerializeField] private bool enableOcclusion = true;
        [SerializeField] private bool enableReverb = true;
        [SerializeField] private int maxAudioSources = 32;

        [Header("Volume Settings")]
        [SerializeField] private float masterVolume = 1.0f;
        [SerializeField] private float musicVolume = 0.8f;
        [SerializeField] private float sfxVolume = 1.0f;
        [SerializeField] private float ambienceVolume = 0.6f;
        [SerializeField] private float voiceVolume = 1.0f;

        private Dictionary<string, AudioSource> audioSources = new Dictionary<string, AudioSource>();
        private Dictionary<AudioCategory, List<AudioSource>> categorizedSources = new Dictionary<AudioCategory, List<AudioSource>>();
        private Queue<AudioSource> availableAudioSources = new Queue<AudioSource>();

        // Events
        public event System.Action<AudioCategory, float> OnVolumeChanged;

        public enum AudioCategory
        {
            Master,
            Music,
            SFX,
            Ambience,
            Voice,
            UI
        }

        public enum AudioPriority
        {
            Low = 0,
            Normal = 128,
            High = 200,
            Critical = 255
        }

        [System.Serializable]
        public class AudioClipData
        {
            public string clipName;
            public AudioClip clip;
            public AudioCategory category;
            public float volume = 1.0f;
            public float pitch = 1.0f;
            public bool loop = false;
            public AudioPriority priority = AudioPriority.Normal;
            public float spatialBlend = 0f; // 0 = 2D, 1 = 3D
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeAudioManager();
                LoadSettings();
            }
            else { Destroy(gameObject); }
        }

        private void InitializeAudioManager()
        {
            // Initialize category lists
            foreach (AudioCategory category in System.Enum.GetValues(typeof(AudioCategory)))
            {
                categorizedSources[category] = new List<AudioSource>();
            }

            // Create audio source pool
            for (int i = 0; i < maxAudioSources; i++)
            {
                GameObject sourceObj = new GameObject($"AudioSource_{i}");
                sourceObj.transform.SetParent(transform);
                AudioSource source = sourceObj.AddComponent<AudioSource>();
                availableAudioSources.Enqueue(source);
            }

            Debug.Log($"[Audio] Audio manager initialized with {maxAudioSources} sources");
        }

        // Playback

        public void PlaySound(AudioClip clip, AudioCategory category, Vector3 position = default, 
            float volume = 1.0f, float pitch = 1.0f, bool loop = false)
        {
            if (clip == null) return;

            AudioSource source = GetAvailableAudioSource();
            if (source == null)
            {
                Debug.LogWarning("[Audio] No available audio sources");
                return;
            }

            source.clip = clip;
            source.volume = volume * GetCategoryVolume(category) * masterVolume;
            source.pitch = pitch;
            source.loop = loop;

            if (position != default)
            {
                source.transform.position = position;
                source.spatialBlend = enable3DAudio ? 1f : 0f;
            }
            else
            {
                source.spatialBlend = 0f;
            }

            source.Play();

            if (!loop)
            {
                StartCoroutine(ReturnSourceAfterPlay(source, clip.length / pitch));
            }
        }

        public void PlaySoundAtPoint(AudioClip clip, Vector3 position, float volume = 1.0f)
        {
            AudioSource.PlayClipAtPoint(clip, position, volume * masterVolume);
        }

        public void PlayMusic(AudioClip music, bool loop = true, float fadeInTime = 1f)
        {
            // Would integrate with music system
            Debug.Log($"[Audio] Playing music: {music.name}");
        }

        public void StopSound(string soundName)
        {
            if (audioSources.ContainsKey(soundName))
            {
                audioSources[soundName].Stop();
            }
        }

        public void StopAllSounds(AudioCategory category = AudioCategory.Master)
        {
            if (category == AudioCategory.Master)
            {
                foreach (var source in audioSources.Values)
                {
                    source.Stop();
                }
            }
            else if (categorizedSources.ContainsKey(category))
            {
                foreach (var source in categorizedSources[category])
                {
                    source.Stop();
                }
            }

            Debug.Log($"[Audio] Stopped all sounds in category: {category}");
        }

        private AudioSource GetAvailableAudioSource()
        {
            if (availableAudioSources.Count > 0)
            {
                return availableAudioSources.Dequeue();
            }

            // Find any stopped source
            foreach (var source in audioSources.Values)
            {
                if (!source.isPlaying)
                {
                    return source;
                }
            }

            return null;
        }

        private System.Collections.IEnumerator ReturnSourceAfterPlay(AudioSource source, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (!source.loop && !source.isPlaying)
            {
                availableAudioSources.Enqueue(source);
            }
        }

        // Volume Control

        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            UpdateAllVolumes();
            OnVolumeChanged?.Invoke(AudioCategory.Master, masterVolume);
            SaveSettings();
        }

        public void SetCategoryVolume(AudioCategory category, float volume)
        {
            volume = Mathf.Clamp01(volume);

            switch (category)
            {
                case AudioCategory.Music:
                    musicVolume = volume;
                    break;
                case AudioCategory.SFX:
                    sfxVolume = volume;
                    break;
                case AudioCategory.Ambience:
                    ambienceVolume = volume;
                    break;
                case AudioCategory.Voice:
                    voiceVolume = volume;
                    break;
            }

            UpdateCategoryVolumes(category);
            OnVolumeChanged?.Invoke(category, volume);
            SaveSettings();
        }

        private float GetCategoryVolume(AudioCategory category)
        {
            switch (category)
            {
                case AudioCategory.Music: return musicVolume;
                case AudioCategory.SFX: return sfxVolume;
                case AudioCategory.Ambience: return ambienceVolume;
                case AudioCategory.Voice: return voiceVolume;
                default: return 1.0f;
            }
        }

        private void UpdateAllVolumes()
        {
            foreach (var source in audioSources.Values)
            {
                // Would update based on category
            }
        }

        private void UpdateCategoryVolumes(AudioCategory category)
        {
            if (categorizedSources.ContainsKey(category))
            {
                float categoryVol = GetCategoryVolume(category);
                foreach (var source in categorizedSources[category])
                {
                    float baseVolume = source.volume / masterVolume / GetCategoryVolume(category);
                    source.volume = baseVolume * categoryVol * masterVolume;
                }
            }
        }

        // 3D Audio

        public void Set3DAudioEnabled(bool enabled)
        {
            enable3DAudio = enabled;
            Debug.Log($"[Audio] 3D audio: {enabled}");
        }

        public void SetOcclusionEnabled(bool enabled)
        {
            enableOcclusion = enabled;
            Debug.Log($"[Audio] Occlusion: {enabled}");
        }

        public void SetReverbEnabled(bool enabled)
        {
            enableReverb = enabled;
            Debug.Log($"[Audio] Reverb: {enabled}");
        }

        // Audio Occlusion (simplified)

        public float CalculateOcclusion(Vector3 listenerPos, Vector3 sourcePos)
        {
            if (!enableOcclusion) return 1.0f;

            RaycastHit hit;
            Vector3 direction = sourcePos - listenerPos;
            
            if (Physics.Raycast(listenerPos, direction.normalized, out hit, direction.magnitude))
            {
                // Simple occlusion based on material
                return 0.5f; // 50% volume when occluded
            }

            return 1.0f; // Full volume when not occluded
        }

        // Reverb Zones (simplified)

        public void ApplyReverbZone(AudioSource source, string zoneName)
        {
            if (!enableReverb) return;

            // Would apply reverb settings based on zone
            Debug.Log($"[Audio] Applied reverb zone: {zoneName}");
        }

        // Mute Control

        public void MuteCategory(AudioCategory category, bool mute)
        {
            if (categorizedSources.ContainsKey(category))
            {
                foreach (var source in categorizedSources[category])
                {
                    source.mute = mute;
                }
            }

            Debug.Log($"[Audio] Category {category} muted: {mute}");
        }

        public void MuteAll(bool mute)
        {
            foreach (var source in audioSources.Values)
            {
                source.mute = mute;
            }

            Debug.Log($"[Audio] All audio muted: {mute}");
        }

        // Settings

        private void SaveSettings()
        {
            PlayerPrefs.SetFloat("Audio_MasterVolume", masterVolume);
            PlayerPrefs.SetFloat("Audio_MusicVolume", musicVolume);
            PlayerPrefs.SetFloat("Audio_SFXVolume", sfxVolume);
            PlayerPrefs.SetFloat("Audio_AmbienceVolume", ambienceVolume);
            PlayerPrefs.SetFloat("Audio_VoiceVolume", voiceVolume);
            PlayerPrefs.SetInt("Audio_3DAudio", enable3DAudio ? 1 : 0);
            PlayerPrefs.SetInt("Audio_Occlusion", enableOcclusion ? 1 : 0);
            PlayerPrefs.SetInt("Audio_Reverb", enableReverb ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void LoadSettings()
        {
            masterVolume = PlayerPrefs.GetFloat("Audio_MasterVolume", 1.0f);
            musicVolume = PlayerPrefs.GetFloat("Audio_MusicVolume", 0.8f);
            sfxVolume = PlayerPrefs.GetFloat("Audio_SFXVolume", 1.0f);
            ambienceVolume = PlayerPrefs.GetFloat("Audio_AmbienceVolume", 0.6f);
            voiceVolume = PlayerPrefs.GetFloat("Audio_VoiceVolume", 1.0f);
            enable3DAudio = PlayerPrefs.GetInt("Audio_3DAudio", 1) == 1;
            enableOcclusion = PlayerPrefs.GetInt("Audio_Occlusion", 1) == 1;
            enableReverb = PlayerPrefs.GetInt("Audio_Reverb", 1) == 1;

            Debug.Log("[Audio] Settings loaded");
        }

        // Getters

        public float GetMasterVolume() => masterVolume;
        public float GetMusicVolume() => musicVolume;
        public float GetSFXVolume() => sfxVolume;
        public bool Is3DAudioEnabled() => enable3DAudio;
        public bool IsOcclusionEnabled() => enableOcclusion;
    }
}
