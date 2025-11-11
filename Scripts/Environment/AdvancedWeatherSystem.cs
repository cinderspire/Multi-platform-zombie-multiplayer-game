using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

namespace ZombieGame
{
    /// <summary>
    /// Advanced Weather System - Dynamic weather with gameplay effects
    /// Rain reduces visibility/accuracy, snow slows movement, fog hides zombies
    /// Supports 8 weather types with smooth transitions and regional variation
    /// </summary>
    public class AdvancedWeatherSystem : NetworkBehaviour
    {
        public static AdvancedWeatherSystem Instance { get; private set; }

        [Header("Weather Settings")]
        [SerializeField] private bool enableDynamicWeather = true;
        [SerializeField] private float weatherChangeInterval = 300f; // 5 minutes
        [SerializeField] private float transitionDuration = 60f; // 1 minute transition

        [Header("Visual Effects")]
        [SerializeField] private ParticleSystem rainParticles;
        [SerializeField] private ParticleSystem snowParticles;
        [SerializeField] private ParticleSystem sandstormParticles;
        [SerializeField] private Light sunLight;
        [SerializeField] private GameObject fogObject;

        [Header("Gameplay Impact")]
        [SerializeField] private bool enableGameplayEffects = true;
        [SerializeField] private float maxVisibilityReduction = 0.7f;
        [SerializeField] private float maxMovementPenalty = 0.3f;
        [SerializeField] private float maxAccuracyPenalty = 0.4f;

        // Network Variables
        private NetworkVariable<WeatherType> currentWeather = new NetworkVariable<WeatherType>(WeatherType.Clear);
        private NetworkVariable<float> weatherIntensity = new NetworkVariable<float>(0f);
        private NetworkVariable<float> transitionProgress = new NetworkVariable<float>(1f);

        private WeatherType targetWeather;
        private float weatherTimer = 0f;
        private float transitionTimer = 0f;
        private bool isTransitioning = false;

        // Weather configurations
        private Dictionary<WeatherType, WeatherConfig> weatherConfigs = new Dictionary<WeatherType, WeatherConfig>();

        // Events
        public event System.Action<WeatherType, float> OnWeatherChanged;
        public event System.Action<WeatherType> OnWeatherTransitionStart;

        public enum WeatherType
        {
            Clear,
            Cloudy,
            Rain,
            HeavyRain,
            Snow,
            Blizzard,
            Fog,
            Sandstorm
        }

        [System.Serializable]
        public class WeatherConfig
        {
            public WeatherType weatherType;
            public float visibilityMultiplier = 1.0f; // 1 = normal, 0.3 = heavy fog
            public float movementSpeedMultiplier = 1.0f;
            public float accuracyMultiplier = 1.0f;
            public float audioDistanceMultiplier = 1.0f;
            public float zombieDetectionMultiplier = 1.0f;
            public Color skyColor = Color.white;
            public float lightIntensity = 1.0f;
            public bool hasParticles = false;
            public float windStrength = 0f;
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeWeatherConfigs();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                currentWeather.Value = WeatherType.Clear;
                weatherIntensity.Value = 0.5f;
                transitionProgress.Value = 1.0f;
                weatherTimer = weatherChangeInterval;
            }

            currentWeather.OnValueChanged += OnWeatherChangedCallback;
        }

        private void InitializeWeatherConfigs()
        {
            // Clear Weather
            weatherConfigs[WeatherType.Clear] = new WeatherConfig
            {
                weatherType = WeatherType.Clear,
                visibilityMultiplier = 1.0f,
                movementSpeedMultiplier = 1.0f,
                accuracyMultiplier = 1.0f,
                audioDistanceMultiplier = 1.0f,
                zombieDetectionMultiplier = 1.0f,
                skyColor = new Color(0.5f, 0.7f, 1.0f),
                lightIntensity = 1.0f,
                windStrength = 0.1f
            };

            // Cloudy
            weatherConfigs[WeatherType.Cloudy] = new WeatherConfig
            {
                weatherType = WeatherType.Cloudy,
                visibilityMultiplier = 0.95f,
                movementSpeedMultiplier = 1.0f,
                accuracyMultiplier = 0.98f,
                audioDistanceMultiplier = 0.95f,
                zombieDetectionMultiplier = 0.95f,
                skyColor = new Color(0.6f, 0.6f, 0.7f),
                lightIntensity = 0.7f,
                windStrength = 0.3f
            };

            // Rain
            weatherConfigs[WeatherType.Rain] = new WeatherConfig
            {
                weatherType = WeatherType.Rain,
                visibilityMultiplier = 0.7f,
                movementSpeedMultiplier = 0.95f,
                accuracyMultiplier = 0.85f,
                audioDistanceMultiplier = 0.6f,
                zombieDetectionMultiplier = 0.7f,
                skyColor = new Color(0.4f, 0.4f, 0.5f),
                lightIntensity = 0.5f,
                hasParticles = true,
                windStrength = 0.5f
            };

            // Heavy Rain
            weatherConfigs[WeatherType.HeavyRain] = new WeatherConfig
            {
                weatherType = WeatherType.HeavyRain,
                visibilityMultiplier = 0.5f,
                movementSpeedMultiplier = 0.9f,
                accuracyMultiplier = 0.7f,
                audioDistanceMultiplier = 0.4f,
                zombieDetectionMultiplier = 0.5f,
                skyColor = new Color(0.3f, 0.3f, 0.4f),
                lightIntensity = 0.3f,
                hasParticles = true,
                windStrength = 0.8f
            };

            // Snow
            weatherConfigs[WeatherType.Snow] = new WeatherConfig
            {
                weatherType = WeatherType.Snow,
                visibilityMultiplier = 0.75f,
                movementSpeedMultiplier = 0.85f, // Slow in snow
                accuracyMultiplier = 0.9f,
                audioDistanceMultiplier = 0.7f,
                zombieDetectionMultiplier = 0.8f,
                skyColor = new Color(0.8f, 0.8f, 0.9f),
                lightIntensity = 0.8f,
                hasParticles = true,
                windStrength = 0.4f
            };

            // Blizzard
            weatherConfigs[WeatherType.Blizzard] = new WeatherConfig
            {
                weatherType = WeatherType.Blizzard,
                visibilityMultiplier = 0.3f, // Severe visibility loss
                movementSpeedMultiplier = 0.7f, // Very slow
                accuracyMultiplier = 0.6f,
                audioDistanceMultiplier = 0.3f,
                zombieDetectionMultiplier = 0.4f,
                skyColor = new Color(0.7f, 0.7f, 0.8f),
                lightIntensity = 0.4f,
                hasParticles = true,
                windStrength = 1.0f
            };

            // Fog
            weatherConfigs[WeatherType.Fog] = new WeatherConfig
            {
                weatherType = WeatherType.Fog,
                visibilityMultiplier = 0.4f, // Heavy fog
                movementSpeedMultiplier = 1.0f,
                accuracyMultiplier = 0.75f,
                audioDistanceMultiplier = 0.8f,
                zombieDetectionMultiplier = 0.3f, // Zombies hidden in fog!
                skyColor = new Color(0.7f, 0.7f, 0.7f),
                lightIntensity = 0.5f,
                windStrength = 0.2f
            };

            // Sandstorm
            weatherConfigs[WeatherType.Sandstorm] = new WeatherConfig
            {
                weatherType = WeatherType.Sandstorm,
                visibilityMultiplier = 0.4f,
                movementSpeedMultiplier = 0.8f,
                accuracyMultiplier = 0.6f,
                audioDistanceMultiplier = 0.5f,
                zombieDetectionMultiplier = 0.5f,
                skyColor = new Color(0.8f, 0.7f, 0.5f),
                lightIntensity = 0.6f,
                hasParticles = true,
                windStrength = 0.9f
            };
        }

        private void Update()
        {
            if (IsServer && enableDynamicWeather)
            {
                UpdateWeatherTimer();
            }

            if (isTransitioning)
            {
                UpdateWeatherTransition();
            }

            ApplyWeatherEffects();
        }

        private void UpdateWeatherTimer()
        {
            weatherTimer -= Time.deltaTime;

            if (weatherTimer <= 0f)
            {
                ChangeWeather();
                weatherTimer = weatherChangeInterval;
            }
        }

        private void ChangeWeather()
        {
            // Select random weather
            WeatherType newWeather = (WeatherType)Random.Range(0, System.Enum.GetValues(typeof(WeatherType)).Length);

            // Don't repeat same weather
            if (newWeather == currentWeather.Value)
            {
                newWeather = (WeatherType)(((int)newWeather + 1) % System.Enum.GetValues(typeof(WeatherType)).Length);
            }

            StartWeatherTransition(newWeather);
        }

        private void StartWeatherTransition(WeatherType newWeather)
        {
            targetWeather = newWeather;
            isTransitioning = true;
            transitionTimer = 0f;
            transitionProgress.Value = 0f;

            OnWeatherTransitionStart?.Invoke(newWeather);
            NotifyWeatherChangeClientRpc(newWeather);
        }

        private void UpdateWeatherTransition()
        {
            transitionTimer += Time.deltaTime;
            transitionProgress.Value = Mathf.Clamp01(transitionTimer / transitionDuration);

            if (transitionProgress.Value >= 1.0f)
            {
                // Transition complete
                currentWeather.Value = targetWeather;
                isTransitioning = false;
                transitionProgress.Value = 1.0f;
            }
        }

        [ClientRpc]
        private void NotifyWeatherChangeClientRpc(WeatherType newWeather)
        {
            Debug.Log($"[Weather] Transitioning to: {newWeather}");
        }

        private void OnWeatherChangedCallback(WeatherType oldWeather, WeatherType newWeather)
        {
            OnWeatherChanged?.Invoke(newWeather, weatherIntensity.Value);
            UpdateVisualEffects(newWeather);
        }

        private void UpdateVisualEffects(WeatherType weather)
        {
            if (!weatherConfigs.ContainsKey(weather)) return;

            WeatherConfig config = weatherConfigs[weather];

            // Update particles
            if (rainParticles != null)
                rainParticles.gameObject.SetActive(weather == WeatherType.Rain || weather == WeatherType.HeavyRain);

            if (snowParticles != null)
                snowParticles.gameObject.SetActive(weather == WeatherType.Snow || weather == WeatherType.Blizzard);

            if (sandstormParticles != null)
                sandstormParticles.gameObject.SetActive(weather == WeatherType.Sandstorm);

            // Update fog
            if (fogObject != null)
            {
                fogObject.SetActive(weather == WeatherType.Fog);
            }

            // Update lighting
            if (sunLight != null)
            {
                sunLight.intensity = config.lightIntensity;
                sunLight.color = config.skyColor;
            }

            // Update render settings
            RenderSettings.fogDensity = Mathf.Lerp(0.001f, 0.05f, 1f - config.visibilityMultiplier);
            RenderSettings.ambientLight = config.skyColor;
        }

        private void ApplyWeatherEffects()
        {
            if (!enableGameplayEffects) return;

            WeatherConfig config = GetCurrentWeatherConfig();
            if (config == null) return;

            // Effects will be queried by other systems via GetWeatherEffects()
        }

        // Public API

        public WeatherConfig GetCurrentWeatherConfig()
        {
            return weatherConfigs.ContainsKey(currentWeather.Value) ? weatherConfigs[currentWeather.Value] : null;
        }

        public float GetVisibilityMultiplier()
        {
            var config = GetCurrentWeatherConfig();
            return config != null ? config.visibilityMultiplier : 1.0f;
        }

        public float GetMovementSpeedMultiplier()
        {
            var config = GetCurrentWeatherConfig();
            return config != null ? config.movementSpeedMultiplier : 1.0f;
        }

        public float GetAccuracyMultiplier()
        {
            var config = GetCurrentWeatherConfig();
            return config != null ? config.accuracyMultiplier : 1.0f;
        }

        public float GetAudioDistanceMultiplier()
        {
            var config = GetCurrentWeatherConfig();
            return config != null ? config.audioDistanceMultiplier : 1.0f;
        }

        public float GetZombieDetectionMultiplier()
        {
            var config = GetCurrentWeatherConfig();
            return config != null ? config.zombieDetectionMultiplier : 1.0f;
        }

        public WeatherType GetCurrentWeather()
        {
            return currentWeather.Value;
        }

        public float GetWeatherIntensity()
        {
            return weatherIntensity.Value;
        }

        [ServerRpc(RequireOwnership = false)]
        public void ForceWeatherChangeServerRpc(WeatherType newWeather, ServerRpcParams rpcParams = default)
        {
            StartWeatherTransition(newWeather);
        }

        public void SetWeatherChangeInterval(float interval)
        {
            weatherChangeInterval = interval;
        }

        public void SetGameplayEffectsEnabled(bool enabled)
        {
            enableGameplayEffects = enabled;
        }

        public bool AreGameplayEffectsEnabled()
        {
            return enableGameplayEffects;
        }
    }
}
