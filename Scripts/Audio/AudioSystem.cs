using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Audio
{
    public class AudioSystem : NetworkBehaviour
    {
        public static AudioSystem Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource ambienceSource;
        [SerializeField] private AudioSource voiceSource;

        [Header("Audio Clips")]
        [SerializeField] private List<AudioClipData> audioClips = new List<AudioClipData>();

        private Dictionary<string, AudioClip> clipDictionary = new Dictionary<string, AudioClip>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeClipDictionary();
        }

        private void InitializeClipDictionary()
        {
            foreach (var data in audioClips)
            {
                if (data.clip != null)
                {
                    clipDictionary[data.clipId] = data.clip;
                }
            }
        }

        public void PlaySFX(string clipId, float volume = 1f)
        {
            if (clipDictionary.TryGetValue(clipId, out var clip))
            {
                sfxSource.PlayOneShot(clip, volume);
            }
        }

        public void PlaySFX3D(string clipId, Vector3 position, float volume = 1f)
        {
            if (clipDictionary.TryGetValue(clipId, out var clip))
            {
                AudioSource.PlayClipAtPoint(clip, position, volume);
            }
        }

        public void PlayMusic(string clipId, bool loop = true)
        {
            if (clipDictionary.TryGetValue(clipId, out var clip))
            {
                musicSource.clip = clip;
                musicSource.loop = loop;
                musicSource.Play();
            }
        }

        public void StopMusic()
        {
            musicSource.Stop();
        }

        public void PlayAmbience(string clipId, bool loop = true)
        {
            if (clipDictionary.TryGetValue(clipId, out var clip))
            {
                ambienceSource.clip = clip;
                ambienceSource.loop = loop;
                ambienceSource.Play();
            }
        }

        public void SetMasterVolume(float volume)
        {
            AudioListener.volume = volume;
        }

        public void SetMusicVolume(float volume)
        {
            musicSource.volume = volume;
        }

        public void SetSFXVolume(float volume)
        {
            sfxSource.volume = volume;
        }

        public void SetAmbienceVolume(float volume)
        {
            ambienceSource.volume = volume;
        }
    }

    [Serializable]
    public class AudioClipData
    {
        public string clipId;
        public AudioClip clip;
        public AudioType audioType;
    }

    public enum AudioType { Music, SFX, Ambience, Voice, UI }
}
