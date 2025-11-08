using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;
using System;

namespace DeadFrontier.Stealth
{
    /// <summary>
    /// Comprehensive sound propagation and stealth system.
    /// Handles realistic sound emission, propagation, occlusion, and AI detection.
    /// Supports material-based footsteps, silencers, and environmental sound modifiers.
    /// </summary>
    public class SoundPropagationSystem : NetworkBehaviour
    {
        public static SoundPropagationSystem Instance { get; private set; }

        [Header("Sound Settings")]
        [SerializeField] private float maxSoundDistance = 100f;
        [SerializeField] private int maxActiveSounds = 50;
        [SerializeField] private float soundUpdateInterval = 0.1f; // 10 times per second
        [SerializeField] private AnimationCurve soundFalloffCurve;

        [Header("Sound Types")]
        [SerializeField] private SoundTypeData[] soundTypes;

        [Header("Material Sounds")]
        [SerializeField] private MaterialSoundData[] materialSounds;

        [Header("Occlusion")]
        [SerializeField] private bool enableSoundOcclusion = true;
        [SerializeField] private LayerMask occlusionMask;
        [SerializeField] private float wallOcclusionMultiplier = 0.3f; // Sound reduced to 30% through walls
        [SerializeField] private float doorOcclusionMultiplier = 0.5f;

        [Header("Weather Effects")]
        [SerializeField] private float rainSoundMultiplier = 0.7f; // Rain reduces sound distance
        [SerializeField] private float fogSoundMultiplier = 0.8f;

        [Header("Debug")]
        [SerializeField] private bool showSoundRadii = false;
        [SerializeField] private Color soundDebugColor = Color.yellow;

        // Active sounds
        private List<ActiveSound> activeSounds = new List<ActiveSound>();
        private Dictionary<ulong, float> lastSoundEmissionTime = new Dictionary<ulong, float>();
        private float lastUpdateTime;

        // AI listeners
        private List<ISoundListener> soundListeners = new List<ISoundListener>();

        // Events
        public event Action<Vector3, SoundCategory, float> OnSoundEmitted;
        public event Action<ActiveSound, ISoundListener> OnSoundDetected;

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

        private void Update()
        {
            if (IsServer)
            {
                UpdateSoundPropagation();
            }

            if (showSoundRadii)
            {
                DebugDrawSounds();
            }
        }

        #region Sound Emission

        public void EmitSound(Vector3 position, SoundCategory category, float volumeMultiplier = 1f, ulong? emitterId = null)
        {
            if (!IsServer) return;

            // Get sound data
            var soundData = GetSoundData(category);
            if (soundData == null)
            {
                Debug.LogWarning($"[SoundPropagationSystem] Sound category {category} not found");
                return;
            }

            // Check rate limiting
            if (emitterId.HasValue && lastSoundEmissionTime.ContainsKey(emitterId.Value))
            {
                if (Time.time - lastSoundEmissionTime[emitterId.Value] < soundData.minTimeBetweenEmissions)
                {
                    return; // Too soon since last emission
                }
            }

            // Apply environmental modifiers
            float environmentalMultiplier = GetEnvironmentalMultiplier();

            // Calculate effective radius
            float effectiveRadius = soundData.baseRadius * volumeMultiplier * environmentalMultiplier;

            // Create active sound
            var activeSound = new ActiveSound
            {
                soundId = Guid.NewGuid().ToString(),
                position = position,
                category = category,
                radius = effectiveRadius,
                intensity = soundData.baseIntensity * volumeMultiplier,
                threatLevel = soundData.threatLevel,
                emissionTime = Time.time,
                lifetime = soundData.lifetime,
                emitterId = emitterId
            };

            activeSounds.Add(activeSound);

            // Limit active sounds
            if (activeSounds.Count > maxActiveSounds)
            {
                activeSounds.RemoveAt(0); // Remove oldest
            }

            // Update emission time
            if (emitterId.HasValue)
            {
                lastSoundEmissionTime[emitterId.Value] = Time.time;
            }

            OnSoundEmitted?.Invoke(position, category, effectiveRadius);

            // Notify clients for audio playback
            EmitSoundClientRpc(position, category, volumeMultiplier);

            Debug.Log($"[SoundPropagationSystem] Emitted {category} sound at {position} with radius {effectiveRadius:F1}");
        }

        [ClientRpc]
        private void EmitSoundClientRpc(Vector3 position, SoundCategory category, float volume)
        {
            // Play audio at position
            PlaySoundEffect(position, category, volume);
        }

        private void PlaySoundEffect(Vector3 position, SoundCategory category, float volume)
        {
            // This would integrate with audio system to play the actual sound effect
            // For now, just a placeholder
        }

        #endregion

        #region Sound Propagation

        private void UpdateSoundPropagation()
        {
            if (Time.time - lastUpdateTime < soundUpdateInterval) return;

            lastUpdateTime = Time.time;

            // Remove expired sounds
            activeSounds.RemoveAll(s => Time.time - s.emissionTime > s.lifetime);

            // Check sound detection for each listener
            foreach (var listener in soundListeners.ToList())
            {
                if (listener == null) continue;

                CheckSoundDetection(listener);
            }
        }

        private void CheckSoundDetection(ISoundListener listener)
        {
            Vector3 listenerPosition = listener.GetPosition();

            foreach (var sound in activeSounds)
            {
                float distance = Vector3.Distance(listenerPosition, sound.position);

                // Check if within range
                if (distance > sound.radius) continue;

                // Calculate intensity at listener position
                float normalizedDistance = distance / sound.radius;
                float intensityAtListener = sound.intensity * soundFalloffCurve.Evaluate(normalizedDistance);

                // Apply occlusion
                if (enableSoundOcclusion)
                {
                    float occlusionMultiplier = CalculateOcclusion(sound.position, listenerPosition);
                    intensityAtListener *= occlusionMultiplier;
                }

                // Check if loud enough to detect
                if (intensityAtListener >= listener.GetDetectionThreshold())
                {
                    // Notify listener
                    listener.OnSoundDetected(sound, intensityAtListener);

                    OnSoundDetected?.Invoke(sound, listener);
                }
            }
        }

        private float CalculateOcclusion(Vector3 soundPosition, Vector3 listenerPosition)
        {
            Vector3 direction = listenerPosition - soundPosition;
            float distance = direction.magnitude;

            if (distance < 0.1f) return 1f; // No occlusion if very close

            RaycastHit[] hits = Physics.RaycastAll(soundPosition, direction.normalized, distance, occlusionMask);

            if (hits.Length == 0) return 1f; // No occlusion

            float occlusionMultiplier = 1f;

            foreach (var hit in hits)
            {
                // Check material type
                if (hit.collider.CompareTag("Wall"))
                {
                    occlusionMultiplier *= wallOcclusionMultiplier;
                }
                else if (hit.collider.CompareTag("Door"))
                {
                    occlusionMultiplier *= doorOcclusionMultiplier;
                }
                else
                {
                    occlusionMultiplier *= wallOcclusionMultiplier; // Default occlusion
                }
            }

            return Mathf.Max(0.1f, occlusionMultiplier); // Minimum 10% of sound gets through
        }

        private float GetEnvironmentalMultiplier()
        {
            float multiplier = 1f;

            // Check weather
            if (Environment.WeatherSystem.Instance != null)
            {
                if (Environment.WeatherSystem.Instance.IsRaining())
                {
                    multiplier *= rainSoundMultiplier;
                }

                var weather = Environment.WeatherSystem.Instance.GetCurrentWeather();
                if (weather == Environment.WeatherType.Fog)
                {
                    multiplier *= fogSoundMultiplier;
                }
            }

            return multiplier;
        }

        #endregion

        #region Footstep System

        public void EmitFootstep(Vector3 position, SurfaceMaterial material, MovementType movementType, ulong playerId)
        {
            // Get material sound data
            var materialData = GetMaterialSoundData(material);
            if (materialData == null)
            {
                materialData = GetMaterialSoundData(SurfaceMaterial.Concrete); // Default
            }

            // Calculate volume based on movement type
            float volumeMultiplier = GetMovementVolumeMultiplier(movementType);

            // Apply material modifier
            volumeMultiplier *= materialData.volumeModifier;

            // Emit sound
            EmitSound(position, SoundCategory.Footstep, volumeMultiplier, playerId);

            // Play local footstep audio
            if (!IsServer)
            {
                PlayFootstepAudio(position, material, volumeMultiplier);
            }
        }

        private float GetMovementVolumeMultiplier(MovementType movementType)
        {
            switch (movementType)
            {
                case MovementType.Crouch: return 0.3f;
                case MovementType.Walk: return 0.6f;
                case MovementType.Run: return 1f;
                case MovementType.Sprint: return 1.3f;
                case MovementType.Jump: return 1.5f;
                case MovementType.Land: return 2f;
                default: return 1f;
            }
        }

        private void PlayFootstepAudio(Vector3 position, SurfaceMaterial material, float volume)
        {
            var materialData = GetMaterialSoundData(material);
            if (materialData == null || materialData.footstepClips == null || materialData.footstepClips.Length == 0)
                return;

            // Play random footstep clip
            AudioClip clip = materialData.footstepClips[UnityEngine.Random.Range(0, materialData.footstepClips.Length)];

            // This would use Unity AudioSource.PlayClipAtPoint or similar
            // AudioSource.PlayClipAtPoint(clip, position, volume);
        }

        #endregion

        #region Weapon Sounds

        public void EmitGunshot(Vector3 position, bool isSilenced, float weaponLoudness, ulong shooterId)
        {
            SoundCategory category = isSilenced ? SoundCategory.SilencedGunshot : SoundCategory.Gunshot;
            float volumeMultiplier = isSilenced ? 0.2f : weaponLoudness;

            EmitSound(position, category, volumeMultiplier, shooterId);
        }

        public void EmitReload(Vector3 position, ulong playerId)
        {
            EmitSound(position, SoundCategory.Reload, 1f, playerId);
        }

        public void EmitWeaponSwitch(Vector3 position, ulong playerId)
        {
            EmitSound(position, SoundCategory.WeaponSwitch, 1f, playerId);
        }

        #endregion

        #region Listener Registration

        public void RegisterListener(ISoundListener listener)
        {
            if (!soundListeners.Contains(listener))
            {
                soundListeners.Add(listener);
                Debug.Log($"[SoundPropagationSystem] Registered sound listener: {listener.GetListenerName()}");
            }
        }

        public void UnregisterListener(ISoundListener listener)
        {
            soundListeners.Remove(listener);
            Debug.Log($"[SoundPropagationSystem] Unregistered sound listener: {listener.GetListenerName()}");
        }

        #endregion

        #region Stealth Utility

        public float GetStealthRating(Vector3 position, MovementType movementType, bool isInShadow, bool isCrouching)
        {
            float stealthRating = 100f; // Start at 100% stealth

            // Movement penalty
            switch (movementType)
            {
                case MovementType.Crouch:
                    stealthRating -= 10f;
                    break;
                case MovementType.Walk:
                    stealthRating -= 30f;
                    break;
                case MovementType.Run:
                    stealthRating -= 60f;
                    break;
                case MovementType.Sprint:
                    stealthRating -= 80f;
                    break;
            }

            // Shadow bonus
            if (isInShadow)
            {
                stealthRating += 20f;
            }

            // Crouch bonus
            if (isCrouching)
            {
                stealthRating += 15f;
            }

            // Weather bonus
            if (Environment.WeatherSystem.Instance != null)
            {
                if (Environment.WeatherSystem.Instance.IsRaining())
                {
                    stealthRating += 10f; // Rain masks sound
                }

                var weather = Environment.WeatherSystem.Instance.GetCurrentWeather();
                if (weather == Environment.WeatherType.Fog)
                {
                    stealthRating += 15f; // Fog masks visibility
                }
            }

            return Mathf.Clamp(stealthRating, 0f, 100f);
        }

        public bool IsInShadow(Vector3 position)
        {
            // Check if position is in shadow (not in direct light)
            Light[] lights = FindObjectsOfType<Light>();

            foreach (var light in lights)
            {
                if (!light.enabled) continue;

                Vector3 toLight = light.transform.position - position;
                float distance = toLight.magnitude;

                // Check if within light range
                if (distance > light.range) continue;

                // Check if light is visible (not occluded)
                if (Physics.Raycast(position, toLight.normalized, distance, occlusionMask))
                {
                    continue; // Occluded
                }

                // In light
                return false;
            }

            // Not in any light = in shadow
            return true;
        }

        public float GetNoiseLevelAtPosition(Vector3 position)
        {
            float totalNoise = 0f;

            foreach (var sound in activeSounds)
            {
                float distance = Vector3.Distance(position, sound.position);

                if (distance > sound.radius) continue;

                float normalizedDistance = distance / sound.radius;
                float intensity = sound.intensity * soundFalloffCurve.Evaluate(normalizedDistance);

                totalNoise += intensity;
            }

            return totalNoise;
        }

        #endregion

        #region Data Access

        private SoundTypeData GetSoundData(SoundCategory category)
        {
            return soundTypes.FirstOrDefault(s => s.category == category);
        }

        private MaterialSoundData GetMaterialSoundData(SurfaceMaterial material)
        {
            return materialSounds.FirstOrDefault(m => m.material == material);
        }

        public List<ActiveSound> GetActiveSounds() => new List<ActiveSound>(activeSounds);

        public List<ActiveSound> GetSoundsInRadius(Vector3 position, float radius)
        {
            return activeSounds.Where(s => Vector3.Distance(position, s.position) <= radius).ToList();
        }

        #endregion

        #region Debug Visualization

        private void DebugDrawSounds()
        {
            foreach (var sound in activeSounds)
            {
                // Draw sound radius
                Debug.DrawLine(sound.position, sound.position + Vector3.up * 2f, soundDebugColor);

                // Draw sphere outline
                int segments = 16;
                for (int i = 0; i < segments; i++)
                {
                    float angle1 = (float)i / segments * Mathf.PI * 2f;
                    float angle2 = (float)(i + 1) / segments * Mathf.PI * 2f;

                    Vector3 point1 = sound.position + new Vector3(Mathf.Cos(angle1), 0, Mathf.Sin(angle1)) * sound.radius;
                    Vector3 point2 = sound.position + new Vector3(Mathf.Cos(angle2), 0, Mathf.Sin(angle2)) * sound.radius;

                    Debug.DrawLine(point1, point2, soundDebugColor);
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (!showSoundRadii || activeSounds == null) return;

            Gizmos.color = soundDebugColor;

            foreach (var sound in activeSounds)
            {
                Gizmos.DrawWireSphere(sound.position, sound.radius);
            }
        }

        #endregion
    }

    #region Data Classes

    public class ActiveSound
    {
        public string soundId;
        public Vector3 position;
        public SoundCategory category;
        public float radius;
        public float intensity;
        public SoundThreatLevel threatLevel;
        public float emissionTime;
        public float lifetime;
        public ulong? emitterId;
    }

    [System.Serializable]
    public class SoundTypeData
    {
        public SoundCategory category;
        public string soundName;
        public float baseRadius = 10f;
        public float baseIntensity = 1f;
        public SoundThreatLevel threatLevel = SoundThreatLevel.Medium;
        public float lifetime = 2f; // How long sound persists
        public float minTimeBetweenEmissions = 0.1f; // Rate limiting
        public AudioClip[] soundClips;
    }

    [System.Serializable]
    public class MaterialSoundData
    {
        public SurfaceMaterial material;
        public float volumeModifier = 1f;
        public AudioClip[] footstepClips;
        public AudioClip[] landingClips;
    }

    public interface ISoundListener
    {
        string GetListenerName();
        Vector3 GetPosition();
        float GetDetectionThreshold(); // Minimum intensity to detect
        void OnSoundDetected(ActiveSound sound, float intensityAtListener);
    }

    public enum SoundCategory
    {
        Footstep,
        Gunshot,
        SilencedGunshot,
        Reload,
        WeaponSwitch,
        Melee,
        Grenade,
        Explosion,
        DoorOpen,
        DoorClose,
        ItemPickup,
        ItemDrop,
        Heal,
        Damage,
        Death,
        Voice,
        Vehicle,
        Environmental
    }

    public enum SoundThreatLevel
    {
        None,       // No threat (item pickup)
        Low,        // Minor threat (footstep)
        Medium,     // Moderate threat (reload)
        High,       // High threat (gunshot)
        Critical    // Critical threat (explosion)
    }

    public enum SurfaceMaterial
    {
        Concrete,
        Metal,
        Wood,
        Grass,
        Dirt,
        Sand,
        Water,
        Snow,
        Gravel,
        Carpet,
        Tile
    }

    public enum MovementType
    {
        Idle,
        Crouch,
        Walk,
        Run,
        Sprint,
        Jump,
        Land
    }

    #endregion

    #region Stealth Visual System

    /// <summary>
    /// Handles visual stealth detection (separate from sound).
    /// Integrates with lighting, cover, and line-of-sight.
    /// </summary>
    public class StealthVisibilitySystem : MonoBehaviour
    {
        public static StealthVisibilitySystem Instance { get; private set; }

        [Header("Visibility Settings")]
        [SerializeField] private float maxDetectionDistance = 50f;
        [SerializeField] private LayerMask visionBlockingMask;
        [SerializeField] private float crouchVisibilityMultiplier = 0.6f;
        [SerializeField] private float shadowVisibilityMultiplier = 0.4f;

        [Header("Cover System")]
        [SerializeField] private float coverCheckDistance = 1f;
        [SerializeField] private LayerMask coverMask;

        private Dictionary<ulong, VisibilityState> playerVisibility = new Dictionary<ulong, VisibilityState>();

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

        public float CalculateVisibility(Vector3 observerPosition, Vector3 targetPosition, bool targetIsCrouching, bool targetInShadow)
        {
            float distance = Vector3.Distance(observerPosition, targetPosition);

            if (distance > maxDetectionDistance) return 0f;

            // Base visibility (decreases with distance)
            float visibility = 1f - (distance / maxDetectionDistance);

            // Line of sight check
            Vector3 direction = targetPosition - observerPosition;
            if (Physics.Raycast(observerPosition, direction.normalized, distance, visionBlockingMask))
            {
                return 0f; // Blocked
            }

            // Crouch modifier
            if (targetIsCrouching)
            {
                visibility *= crouchVisibilityMultiplier;
            }

            // Shadow modifier
            if (targetInShadow)
            {
                visibility *= shadowVisibilityMultiplier;
            }

            // Weather modifiers
            if (Environment.WeatherSystem.Instance != null)
            {
                if (Environment.WeatherSystem.Instance.IsRaining())
                {
                    visibility *= 0.8f; // Rain reduces visibility
                }

                var weather = Environment.WeatherSystem.Instance.GetCurrentWeather();
                if (weather == Environment.WeatherType.Fog)
                {
                    visibility *= 0.5f; // Heavy visibility reduction in fog
                }
                else if (weather == Environment.WeatherType.HeavyRain || weather == Environment.WeatherType.Storm)
                {
                    visibility *= 0.6f;
                }

                // Night modifier
                if (Environment.WeatherSystem.Instance.IsNight())
                {
                    visibility *= 0.7f;
                }
            }

            return Mathf.Clamp01(visibility);
        }

        public bool IsInCover(Vector3 position, Vector3 threatDirection)
        {
            // Check if there's cover between position and threat
            RaycastHit hit;
            if (Physics.Raycast(position, threatDirection, out hit, coverCheckDistance, coverMask))
            {
                return true;
            }

            return false;
        }

        public Vector3 GetNearestCoverPosition(Vector3 fromPosition, float searchRadius)
        {
            Collider[] coverObjects = Physics.OverlapSphere(fromPosition, searchRadius, coverMask);

            if (coverObjects.Length == 0) return fromPosition;

            // Find nearest cover
            Vector3 nearestCover = fromPosition;
            float nearestDistance = float.MaxValue;

            foreach (var cover in coverObjects)
            {
                float distance = Vector3.Distance(fromPosition, cover.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestCover = cover.ClosestPoint(fromPosition);
                }
            }

            return nearestCover;
        }
    }

    public class VisibilityState
    {
        public ulong playerId;
        public float currentVisibility;
        public bool isInCover;
        public bool isInShadow;
        public Vector3 lastKnownPosition;
    }

    #endregion
}
