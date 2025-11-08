using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace DeadFrontier.Core
{
    /// <summary>
    /// Manages all audio in the game including music, SFX, and spatial audio
    /// </summary>
    public class AudioManager : Singleton<AudioManager>
    {
        [Header("Audio Mixers")]
        [SerializeField] private AudioMixerGroup masterMixer;
        [SerializeField] private AudioMixerGroup musicMixer;
        [SerializeField] private AudioMixerGroup sfxMixer;

        [Header("Settings")]
        [SerializeField] private int audioSourcePoolSize = Constants.AUDIO_SOURCE_POOL_SIZE;
        [SerializeField] private float maxAudioDistance = Constants.AUDIO_MAX_DISTANCE;

        // Audio source pool
        private Queue<AudioSource> audioSourcePool = new Queue<AudioSource>();
        private List<AudioSource> activeAudioSources = new List<AudioSource>();

        // Music
        private AudioSource musicSource;
        private float musicVolume = 1f;
        private float sfxVolume = 1f;

        // Noise events for zombie AI
        private List<NoiseEvent> recentNoises = new List<NoiseEvent>();
        private const float NOISE_LIFETIME = 0.5f; // How long noises persist

        protected override void Awake()
        {
            base.Awake();

            // Create music source
            GameObject musicObj = new GameObject("MusicSource");
            musicObj.transform.SetParent(transform);
            musicSource = musicObj.AddComponent<AudioSource>();
            musicSource.outputAudioMixerGroup = musicMixer;
            musicSource.loop = true;
            musicSource.playOnAwake = false;

            // Initialize audio source pool
            InitializePool();

            Debug.Log("[AudioManager] Initialized");
        }

        private void Update()
        {
            CleanupNoises();
        }

        #region SFX

        /// <summary>
        /// Plays a one-shot sound effect at a position
        /// </summary>
        public void Play(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f, float noiseLevel = 0f)
        {
            if (clip == null)
            {
                Debug.LogWarning("[AudioManager] Attempted to play null AudioClip");
                return;
            }

            AudioSource source = GetAudioSource();
            source.transform.position = position;
            source.clip = clip;
            source.volume = volume * sfxVolume;
            source.pitch = pitch;
            source.spatialBlend = 1f; // 3D sound
            source.maxDistance = maxAudioDistance;
            source.Play();

            // Register noise event for zombie AI
            if (noiseLevel > 0f)
            {
                RegisterNoise(position, noiseLevel);
            }

            // Return to pool when done
            StartCoroutine(ReturnToPoolWhenDone(source, clip.length / pitch));
        }

        /// <summary>
        /// Plays a one-shot sound effect at default listener position (2D)
        /// </summary>
        public void Play(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (clip == null)
            {
                Debug.LogWarning("[AudioManager] Attempted to play null AudioClip");
                return;
            }

            AudioSource source = GetAudioSource();
            source.clip = clip;
            source.volume = volume * sfxVolume;
            source.pitch = pitch;
            source.spatialBlend = 0f; // 2D sound
            source.Play();

            StartCoroutine(ReturnToPoolWhenDone(source, clip.length / pitch));
        }

        /// <summary>
        /// Plays a random clip from an array
        /// </summary>
        public void PlayRandom(AudioClip[] clips, Vector3 position, float volume = 1f, float pitch = 1f, float noiseLevel = 0f)
        {
            if (clips == null || clips.Length == 0)
            {
                Debug.LogWarning("[AudioManager] Attempted to play from empty AudioClip array");
                return;
            }

            AudioClip randomClip = clips[Random.Range(0, clips.Length)];
            Play(randomClip, position, volume, pitch, noiseLevel);
        }

        /// <summary>
        /// Plays a looping sound at a position, returns the AudioSource for control
        /// </summary>
        public AudioSource PlayLooping(AudioClip clip, Vector3 position, float volume = 1f)
        {
            if (clip == null)
            {
                Debug.LogWarning("[AudioManager] Attempted to play null AudioClip");
                return null;
            }

            AudioSource source = GetAudioSource();
            source.transform.position = position;
            source.clip = clip;
            source.volume = volume * sfxVolume;
            source.loop = true;
            source.spatialBlend = 1f;
            source.maxDistance = maxAudioDistance;
            source.Play();

            return source;
        }

        /// <summary>
        /// Stops a looping sound
        /// </summary>
        public void StopLooping(AudioSource source)
        {
            if (source != null)
            {
                source.Stop();
                source.loop = false;
                ReturnAudioSource(source);
            }
        }

        #endregion

        #region Music

        /// <summary>
        /// Plays background music
        /// </summary>
        public void PlayMusic(AudioClip music, float volume = 1f, bool loop = true)
        {
            if (music == null)
            {
                Debug.LogWarning("[AudioManager] Attempted to play null music clip");
                return;
            }

            musicSource.clip = music;
            musicSource.volume = volume * musicVolume;
            musicSource.loop = loop;
            musicSource.Play();
        }

        /// <summary>
        /// Stops background music
        /// </summary>
        public void StopMusic()
        {
            musicSource.Stop();
        }

        /// <summary>
        /// Fades music volume to a target over time
        /// </summary>
        public void FadeMusicVolume(float targetVolume, float duration)
        {
            StartCoroutine(FadeMusicCoroutine(targetVolume, duration));
        }

        private System.Collections.IEnumerator FadeMusicCoroutine(float targetVolume, float duration)
        {
            float startVolume = musicSource.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(startVolume, targetVolume * musicVolume, elapsed / duration);
                yield return null;
            }

            musicSource.volume = targetVolume * musicVolume;
        }

        #endregion

        #region Volume Control

        /// <summary>
        /// Sets the master volume
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);
            // Convert to decibels: -80 dB (silent) to 0 dB (full volume)
            float db = volume > 0 ? Mathf.Log10(volume) * 20f : -80f;
            masterMixer?.audioMixer.SetFloat("MasterVolume", db);
        }

        /// <summary>
        /// Sets the music volume
        /// </summary>
        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            float db = musicVolume > 0 ? Mathf.Log10(musicVolume) * 20f : -80f;
            musicMixer?.audioMixer.SetFloat("MusicVolume", db);
            musicSource.volume = musicVolume;
        }

        /// <summary>
        /// Sets the SFX volume
        /// </summary>
        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            float db = sfxVolume > 0 ? Mathf.Log10(sfxVolume) * 20f : -80f;
            sfxMixer?.audioMixer.SetFloat("SFXVolume", db);
        }

        #endregion

        #region Noise Events (for Zombie AI)

        /// <summary>
        /// Registers a noise event at a position (attracts zombies)
        /// </summary>
        public void RegisterNoise(Vector3 position, float range)
        {
            recentNoises.Add(new NoiseEvent
            {
                position = position,
                range = range,
                timestamp = Time.time
            });
        }

        /// <summary>
        /// Gets all noise events within range of a position
        /// </summary>
        public List<NoiseEvent> GetNoisesInRange(Vector3 position, float hearingRange)
        {
            List<NoiseEvent> noisesInRange = new List<NoiseEvent>();

            foreach (var noise in recentNoises)
            {
                float distance = Vector3.Distance(position, noise.position);
                if (distance <= noise.range && distance <= hearingRange)
                {
                    noisesInRange.Add(noise);
                }
            }

            return noisesInRange;
        }

        private void CleanupNoises()
        {
            // Remove old noise events
            recentNoises.RemoveAll(noise => Time.time - noise.timestamp > NOISE_LIFETIME);
        }

        #endregion

        #region Audio Source Pool

        private void InitializePool()
        {
            GameObject poolContainer = new GameObject("AudioSourcePool");
            poolContainer.transform.SetParent(transform);

            for (int i = 0; i < audioSourcePoolSize; i++)
            {
                AudioSource source = CreateAudioSource(poolContainer.transform);
                source.gameObject.SetActive(false);
                audioSourcePool.Enqueue(source);
            }
        }

        private AudioSource GetAudioSource()
        {
            AudioSource source;

            if (audioSourcePool.Count > 0)
            {
                source = audioSourcePool.Dequeue();
                source.gameObject.SetActive(true);
            }
            else
            {
                // Pool exhausted, create new source
                source = CreateAudioSource(transform);
                Debug.LogWarning("[AudioManager] Audio source pool exhausted, creating new source");
            }

            activeAudioSources.Add(source);
            return source;
        }

        private void ReturnAudioSource(AudioSource source)
        {
            if (source == null)
                return;

            source.Stop();
            source.clip = null;
            source.loop = false;
            source.gameObject.SetActive(false);

            activeAudioSources.Remove(source);
            audioSourcePool.Enqueue(source);
        }

        private AudioSource CreateAudioSource(Transform parent)
        {
            GameObject obj = new GameObject("AudioSource");
            obj.transform.SetParent(parent);
            AudioSource source = obj.AddComponent<AudioSource>();
            source.outputAudioMixerGroup = sfxMixer;
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            return source;
        }

        private System.Collections.IEnumerator ReturnToPoolWhenDone(AudioSource source, float duration)
        {
            yield return new WaitForSeconds(duration);
            ReturnAudioSource(source);
        }

        #endregion

        /// <summary>
        /// Stops all active sounds
        /// </summary>
        public void StopAllSounds()
        {
            foreach (var source in activeAudioSources.ToArray())
            {
                if (source != null)
                {
                    source.Stop();
                    ReturnAudioSource(source);
                }
            }
            activeAudioSources.Clear();
        }
    }

    /// <summary>
    /// Represents a noise event in the game world
    /// </summary>
    public struct NoiseEvent
    {
        public Vector3 position;
        public float range;
        public float timestamp;
    }
}
