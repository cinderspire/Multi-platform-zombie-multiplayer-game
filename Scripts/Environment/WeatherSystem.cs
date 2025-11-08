using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System;

namespace DeadFrontier.Environment
{
    /// <summary>
    /// Dynamic weather and time of day system affecting gameplay, visibility, and difficulty.
    /// Provides atmospheric immersion and tactical considerations for players.
    /// </summary>
    public class WeatherSystem : NetworkBehaviour
    {
        public static WeatherSystem Instance { get; private set; }

        [Header("Time Settings")]
        [SerializeField] private bool enableTimeProgression = true;
        [SerializeField] private float dayDurationMinutes = 20f;
        [SerializeField] private float startTimeOfDay = 12f; // Start at noon (0-24)
        [SerializeField] private AnimationCurve sunIntensityCurve;
        [SerializeField] private Gradient skyColorGradient;

        [Header("Weather Settings")]
        [SerializeField] private bool enableDynamicWeather = true;
        [SerializeField] private float weatherTransitionTime = 30f;
        [SerializeField] private float minWeatherDuration = 120f;
        [SerializeField] private float maxWeatherDuration = 300f;

        [Header("Weather Patterns")]
        [SerializeField] private WeatherPattern[] weatherPatterns;

        [Header("Lighting")]
        [SerializeField] private Light sunLight;
        [SerializeField] private Light moonLight;
        [SerializeField] private Material skyboxMaterial;
        [SerializeField] private float nightLightIntensity = 0.3f;
        [SerializeField] private float dayLightIntensity = 1f;

        [Header("Effects")]
        [SerializeField] private ParticleSystem rainParticles;
        [SerializeField] private ParticleSystem snowParticles;
        [SerializeField] private ParticleSystem fogEffect;
        [SerializeField] private AudioSource weatherAmbience;

        [Header("Gameplay Impact")]
        [SerializeField] private float rainVisibilityReduction = 0.3f;
        [SerializeField] private float fogVisibilityReduction = 0.5f;
        [SerializeField] private float nightVisibilityReduction = 0.4f;
        [SerializeField] private float rainZombieSpeedBonus = 0.1f; // Zombies move faster in rain
        [SerializeField] private float nightZombieSpeedBonus = 0.2f; // Zombies faster at night

        // Network state
        private NetworkVariable<float> networkTimeOfDay = new NetworkVariable<float>(12f);
        private NetworkVariable<int> networkCurrentWeather = new NetworkVariable<int>(0);
        private NetworkVariable<float> networkWeatherIntensity = new NetworkVariable<float>(0f);

        // Current state
        private float currentTimeOfDay;
        private WeatherType currentWeather = WeatherType.Clear;
        private WeatherType targetWeather = WeatherType.Clear;
        private float weatherIntensity;
        private float weatherTransitionProgress;
        private float weatherDuration;
        private float weatherTimer;

        // Cached values
        private float visibilityModifier = 1f;
        private float zombieSpeedModifier = 1f;

        // Events
        public event Action<float> OnTimeChanged;
        public event Action<WeatherType, float> OnWeatherChanged;
        public event Action<TimeOfDay> OnDayNightCycle; // Dawn, Day, Dusk, Night

        private TimeOfDay currentDayPeriod = TimeOfDay.Day;

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

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                InitializeWeatherSystem();
            }

            networkTimeOfDay.OnValueChanged += OnTimeOfDayChanged;
            networkCurrentWeather.OnValueChanged += OnWeatherTypeChanged;
            networkWeatherIntensity.OnValueChanged += OnWeatherIntensityChanged;
        }

        public override void OnNetworkDespawn()
        {
            networkTimeOfDay.OnValueChanged -= OnTimeOfDayChanged;
            networkCurrentWeather.OnValueChanged -= OnWeatherTypeChanged;
            networkWeatherIntensity.OnValueChanged -= OnWeatherIntensityChanged;
            base.OnNetworkDespawn();
        }

        private void Update()
        {
            if (IsServer)
            {
                UpdateTimeOfDay();
                UpdateWeather();
            }

            UpdateLighting();
            UpdateWeatherEffects();
        }

        #region Initialization

        private void InitializeWeatherSystem()
        {
            currentTimeOfDay = startTimeOfDay;
            networkTimeOfDay.Value = currentTimeOfDay;

            // Start with clear weather
            currentWeather = WeatherType.Clear;
            targetWeather = WeatherType.Clear;
            networkCurrentWeather.Value = (int)currentWeather;

            // Schedule first weather change
            ScheduleNextWeather();

            Debug.Log("[WeatherSystem] Initialized");
        }

        #endregion

        #region Time of Day

        private void UpdateTimeOfDay()
        {
            if (!enableTimeProgression) return;

            // Progress time
            float timeIncrement = (24f / (dayDurationMinutes * 60f)) * Time.deltaTime;
            currentTimeOfDay += timeIncrement;

            // Wrap around 24 hours
            if (currentTimeOfDay >= 24f)
            {
                currentTimeOfDay -= 24f;
            }

            networkTimeOfDay.Value = currentTimeOfDay;

            // Check for day/night transitions
            CheckDayNightTransition();
        }

        private void CheckDayNightTransition()
        {
            TimeOfDay newPeriod = GetCurrentDayPeriod();

            if (newPeriod != currentDayPeriod)
            {
                currentDayPeriod = newPeriod;
                OnDayNightCycle?.Invoke(newPeriod);

                Debug.Log($"[WeatherSystem] Time period changed to: {newPeriod}");
            }
        }

        private TimeOfDay GetCurrentDayPeriod()
        {
            if (currentTimeOfDay >= 5f && currentTimeOfDay < 7f)
                return TimeOfDay.Dawn;
            else if (currentTimeOfDay >= 7f && currentTimeOfDay < 18f)
                return TimeOfDay.Day;
            else if (currentTimeOfDay >= 18f && currentTimeOfDay < 20f)
                return TimeOfDay.Dusk;
            else
                return TimeOfDay.Night;
        }

        #endregion

        #region Weather

        private void UpdateWeather()
        {
            if (!enableDynamicWeather) return;

            weatherTimer += Time.deltaTime;

            // Check if should transition to new weather
            if (weatherTimer >= weatherDuration)
            {
                ScheduleNextWeather();
            }

            // Handle weather transition
            if (currentWeather != targetWeather)
            {
                weatherTransitionProgress += Time.deltaTime / weatherTransitionTime;

                if (weatherTransitionProgress >= 1f)
                {
                    currentWeather = targetWeather;
                    weatherTransitionProgress = 0f;
                    networkCurrentWeather.Value = (int)currentWeather;
                }
            }

            // Update weather intensity
            UpdateWeatherIntensity();
        }

        private void ScheduleNextWeather()
        {
            // Select random weather
            targetWeather = SelectRandomWeather();
            weatherDuration = UnityEngine.Random.Range(minWeatherDuration, maxWeatherDuration);
            weatherTimer = 0f;

            Debug.Log($"[WeatherSystem] Scheduled weather: {targetWeather} for {weatherDuration:F0}s");
        }

        private WeatherType SelectRandomWeather()
        {
            // Weight weather based on time of day and current conditions
            float roll = UnityEngine.Random.value;

            if (currentDayPeriod == TimeOfDay.Night)
            {
                // More fog at night
                if (roll < 0.4f) return WeatherType.Clear;
                if (roll < 0.6f) return WeatherType.Fog;
                if (roll < 0.8f) return WeatherType.LightRain;
                return WeatherType.HeavyRain;
            }
            else
            {
                // Varied weather during day
                if (roll < 0.5f) return WeatherType.Clear;
                if (roll < 0.7f) return WeatherType.Cloudy;
                if (roll < 0.85f) return WeatherType.LightRain;
                if (roll < 0.95f) return WeatherType.HeavyRain;
                return WeatherType.Storm;
            }
        }

        private void UpdateWeatherIntensity()
        {
            float targetIntensity = GetTargetWeatherIntensity();

            weatherIntensity = Mathf.Lerp(weatherIntensity, targetIntensity, Time.deltaTime * 2f);
            networkWeatherIntensity.Value = weatherIntensity;

            // Calculate gameplay modifiers
            UpdateGameplayModifiers();
        }

        private float GetTargetWeatherIntensity()
        {
            switch (currentWeather)
            {
                case WeatherType.Clear: return 0f;
                case WeatherType.Cloudy: return 0.3f;
                case WeatherType.Fog: return 0.6f;
                case WeatherType.LightRain: return 0.5f;
                case WeatherType.HeavyRain: return 0.8f;
                case WeatherType.Storm: return 1f;
                case WeatherType.Snow: return 0.7f;
                default: return 0f;
            }
        }

        #endregion

        #region Visual Updates

        private void UpdateLighting()
        {
            float normalizedTime = currentTimeOfDay / 24f;

            // Update sun
            if (sunLight != null)
            {
                // Sun rotation (rises at 6am, sets at 6pm)
                float sunAngle = (currentTimeOfDay - 6f) * 15f; // 360 degrees / 24 hours
                sunLight.transform.rotation = Quaternion.Euler(sunAngle, 0f, 0f);

                // Sun intensity
                float sunIntensity = sunIntensityCurve.Evaluate(normalizedTime) * dayLightIntensity;

                // Reduce intensity during bad weather
                sunIntensity *= (1f - weatherIntensity * 0.5f);

                sunLight.intensity = sunIntensity;
            }

            // Update moon
            if (moonLight != null)
            {
                bool isNight = currentDayPeriod == TimeOfDay.Night;
                moonLight.enabled = isNight;

                if (isNight)
                {
                    moonLight.intensity = nightLightIntensity * (1f - weatherIntensity * 0.3f);
                }
            }

            // Update skybox
            if (skyboxMaterial != null)
            {
                Color skyColor = skyColorGradient.Evaluate(normalizedTime);
                skyboxMaterial.SetColor("_Tint", skyColor);
            }

            // Update ambient light
            RenderSettings.ambientIntensity = Mathf.Lerp(0.3f, 1f, normalizedTime);
        }

        private void UpdateWeatherEffects()
        {
            // Rain
            if (rainParticles != null)
            {
                bool shouldRain = currentWeather == WeatherType.LightRain || currentWeather == WeatherType.HeavyRain || currentWeather == WeatherType.Storm;
                rainParticles.gameObject.SetActive(shouldRain);

                if (shouldRain)
                {
                    var emission = rainParticles.emission;
                    float rainRate = currentWeather == WeatherType.HeavyRain || currentWeather == WeatherType.Storm ? 1000f : 500f;
                    emission.rateOverTime = rainRate * weatherIntensity;
                }
            }

            // Snow
            if (snowParticles != null)
            {
                bool shouldSnow = currentWeather == WeatherType.Snow;
                snowParticles.gameObject.SetActive(shouldSnow);

                if (shouldSnow)
                {
                    var emission = snowParticles.emission;
                    emission.rateOverTime = 300f * weatherIntensity;
                }
            }

            // Fog
            if (fogEffect != null)
            {
                bool shouldFog = currentWeather == WeatherType.Fog;
                fogEffect.gameObject.SetActive(shouldFog);
            }

            // Update Unity fog
            RenderSettings.fog = currentWeather == WeatherType.Fog || weatherIntensity > 0.5f;
            if (RenderSettings.fog)
            {
                RenderSettings.fogDensity = 0.01f * weatherIntensity;
            }

            // Weather audio
            UpdateWeatherAudio();
        }

        private void UpdateWeatherAudio()
        {
            if (weatherAmbience == null) return;

            switch (currentWeather)
            {
                case WeatherType.LightRain:
                case WeatherType.HeavyRain:
                case WeatherType.Storm:
                    if (!weatherAmbience.isPlaying)
                    {
                        // Play rain sound
                        // weatherAmbience.clip = rainSound;
                        weatherAmbience.Play();
                    }
                    weatherAmbience.volume = weatherIntensity;
                    break;

                default:
                    if (weatherAmbience.isPlaying)
                    {
                        weatherAmbience.Stop();
                    }
                    break;
            }
        }

        #endregion

        #region Gameplay Modifiers

        private void UpdateGameplayModifiers()
        {
            // Calculate visibility modifier
            visibilityModifier = 1f;

            // Night reduces visibility
            if (currentDayPeriod == TimeOfDay.Night)
            {
                visibilityModifier -= nightVisibilityReduction;
            }

            // Weather reduces visibility
            if (currentWeather == WeatherType.Fog)
            {
                visibilityModifier -= fogVisibilityReduction * weatherIntensity;
            }
            else if (currentWeather == WeatherType.HeavyRain || currentWeather == WeatherType.Storm)
            {
                visibilityModifier -= rainVisibilityReduction * weatherIntensity;
            }

            visibilityModifier = Mathf.Clamp01(visibilityModifier);

            // Calculate zombie speed modifier
            zombieSpeedModifier = 1f;

            if (currentDayPeriod == TimeOfDay.Night)
            {
                zombieSpeedModifier += nightZombieSpeedBonus;
            }

            if (currentWeather == WeatherType.HeavyRain || currentWeather == WeatherType.Storm)
            {
                zombieSpeedModifier += rainZombieSpeedBonus * weatherIntensity;
            }
        }

        #endregion

        #region Network Callbacks

        private void OnTimeOfDayChanged(float previousValue, float newValue)
        {
            currentTimeOfDay = newValue;
            OnTimeChanged?.Invoke(newValue);
        }

        private void OnWeatherTypeChanged(int previousValue, int newValue)
        {
            currentWeather = (WeatherType)newValue;
            OnWeatherChanged?.Invoke(currentWeather, weatherIntensity);
        }

        private void OnWeatherIntensityChanged(float previousValue, float newValue)
        {
            weatherIntensity = newValue;
        }

        #endregion

        #region Public Methods

        public void SetTimeOfDay(float time)
        {
            if (!IsServer) return;

            currentTimeOfDay = Mathf.Clamp(time, 0f, 24f);
            networkTimeOfDay.Value = currentTimeOfDay;
        }

        public void SetWeather(WeatherType weather, float intensity = 1f)
        {
            if (!IsServer) return;

            currentWeather = weather;
            targetWeather = weather;
            weatherIntensity = intensity;

            networkCurrentWeather.Value = (int)weather;
            networkWeatherIntensity.Value = intensity;
        }

        public void ForceWeatherChange(WeatherType weather)
        {
            if (!IsServer) return;

            targetWeather = weather;
            weatherTransitionProgress = 0f;
            weatherTimer = 0f;
        }

        #endregion

        #region Public Getters

        public float GetCurrentTimeOfDay() => currentTimeOfDay;

        public WeatherType GetCurrentWeather() => currentWeather;

        public float GetWeatherIntensity() => weatherIntensity;

        public TimeOfDay GetCurrentDayPeriod() => currentDayPeriod;

        public float GetVisibilityModifier() => visibilityModifier;

        public float GetZombieSpeedModifier() => zombieSpeedModifier;

        public bool IsNight() => currentDayPeriod == TimeOfDay.Night;

        public bool IsRaining() => currentWeather == WeatherType.LightRain || currentWeather == WeatherType.HeavyRain || currentWeather == WeatherType.Storm;

        #endregion
    }

    #region Data Classes

    [System.Serializable]
    public class WeatherPattern
    {
        public WeatherType weatherType;
        public float probability = 0.2f;
        public float minDuration = 60f;
        public float maxDuration = 300f;
        public TimeOfDay[] preferredTimes;
    }

    public enum WeatherType
    {
        Clear,
        Cloudy,
        Fog,
        LightRain,
        HeavyRain,
        Storm,
        Snow
    }

    public enum TimeOfDay
    {
        Dawn,       // 5-7am
        Day,        // 7am-6pm
        Dusk,       // 6-8pm
        Night       // 8pm-5am
    }

    #endregion
}
