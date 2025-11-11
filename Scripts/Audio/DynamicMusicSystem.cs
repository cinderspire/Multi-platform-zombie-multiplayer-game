using UnityEngine;
using System.Collections.Generic;

namespace ZombieGame
{
    public class DynamicMusicSystem : MonoBehaviour
    {
        public static DynamicMusicSystem Instance { get; private set; }
        
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private float transitionTime = 2f;
        
        private Dictionary<MusicLayer, AudioClip> musicLayers = new Dictionary<MusicLayer, AudioClip>();
        private MusicIntensity currentIntensity = MusicIntensity.Calm;
        
        public enum MusicLayer
        {
            Ambient, Combat, Boss, Victory
        }
        
        public enum MusicIntensity
        {
            Calm, Tense, Combat, Intense, Boss
        }
        
        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }
        
        public void SetIntensity(MusicIntensity intensity)
        {
            if (currentIntensity == intensity) return;
            
            currentIntensity = intensity;
            TransitionMusic();
        }
        
        private void TransitionMusic()
        {
            // Fade out current, fade in new
            Debug.Log($"[DynamicMusic] Transitioning to {currentIntensity}");
        }
        
        public void PlayLayer(MusicLayer layer, bool additive = false)
        {
            if (musicLayers.ContainsKey(layer))
            {
                // Play music layer
                Debug.Log($"[DynamicMusic] Playing layer: {layer}");
            }
        }
    }
}
