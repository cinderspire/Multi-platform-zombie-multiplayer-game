using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Weather
{
    public class WeatherSystem : NetworkBehaviour
    {
        public static WeatherSystem Instance { get; private set; }

        [Header("Weather Configuration")]
        [SerializeField] private float weatherChangeDuration = 300f; // 5 minutes
        [SerializeField] private bool enableDynamicWeather = true;

        private Dictionary<string, WeatherType> weatherTypes = new Dictionary<string, WeatherType>();
        private NetworkVariable<int> currentWeatherId = new NetworkVariable<int>(0);
        private NetworkVariable<float> timeOfDay = new NetworkVariable<float>(12f); // 0-24 hours
        private float weatherTimer = 0f;

        public event Action<string> OnWeatherChanged;
        public event Action<float> OnTimeChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer) InitializeWeatherTypes();
        }

        private void InitializeWeatherTypes()
        {
            weatherTypes["clear"] = new WeatherType
            {
                weatherId = "clear",
                weatherName = "Clear",
                description = "Clear skies",
                visibility = 1f,
                effects = new WeatherEffects { movementSpeed = 1f, zombieSpawnRate = 1f, lootBonus = 1f }
            };

            weatherTypes["rain"] = new WeatherType
            {
                weatherId = "rain",
                weatherName = "Rain",
                description = "Heavy rainfall",
                visibility = 0.7f,
                effects = new WeatherEffects { movementSpeed = 0.9f, zombieSpawnRate = 1.2f, lootBonus = 1.1f, soundReduction = 0.6f }
            };

            weatherTypes["fog"] = new WeatherType
            {
                weatherId = "fog",
                weatherName = "Fog",
                description = "Dense fog",
                visibility = 0.4f,
                effects = new WeatherEffects { zombieSpawnRate = 1.5f, zombieDamage = 1.2f, soundReduction = 0.4f }
            };

            weatherTypes["storm"] = new WeatherType
            {
                weatherId = "storm",
                weatherName = "Storm",
                description = "Severe thunderstorm",
                visibility = 0.5f,
                effects = new WeatherEffects { movementSpeed = 0.8f, zombieSpawnRate = 2f, zombieDamage = 1.3f, lootBonus = 1.5f, soundReduction = 0.3f },
                dangerous = true
            };

            weatherTypes["blood_moon"] = new WeatherType
            {
                weatherId = "blood_moon",
                weatherName = "Blood Moon",
                description = "Ominous red moon",
                visibility = 0.6f,
                effects = new WeatherEffects { zombieSpawnRate = 3f, zombieDamage = 1.5f, zombieHealth = 1.5f, lootBonus = 2f },
                dangerous = true,
                eventWeather = true
            };

            Debug.Log($"Initialized {weatherTypes.Count} weather types");
        }

        private void Update()
        {
            if (!IsServer) return;

            // Update time of day
            timeOfDay.Value += Time.deltaTime / 60f; // 1 hour per minute
            if (timeOfDay.Value >= 24f) timeOfDay.Value = 0f;

            // Dynamic weather changes
            if (enableDynamicWeather)
            {
                weatherTimer += Time.deltaTime;
                if (weatherTimer >= weatherChangeDuration)
                {
                    weatherTimer = 0f;
                    ChangeWeatherRandomly();
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void SetWeatherServerRpc(string weatherId, ServerRpcParams rpcParams = default)
        {
            if (!weatherTypes.ContainsKey(weatherId)) return;

            var weather = weatherTypes[weatherId];
            currentWeatherId.Value = GetWeatherIndex(weatherId);

            OnWeatherChanged?.Invoke(weatherId);
            NotifyWeatherChangedClientRpc(weatherId);

            Debug.Log($"Weather changed to: {weather.weatherName}");
        }

        private void ChangeWeatherRandomly()
        {
            var weatherIds = new List<string>(weatherTypes.Keys);
            weatherIds.Remove("blood_moon"); // Blood moon is event-only

            string newWeather = weatherIds[UnityEngine.Random.Range(0, weatherIds.Count)];
            SetWeatherServerRpc(newWeather);
        }

        private int GetWeatherIndex(string weatherId)
        {
            int index = 0;
            foreach (var id in weatherTypes.Keys)
            {
                if (id == weatherId) return index;
                index++;
            }
            return 0;
        }

        public WeatherType GetCurrentWeather()
        {
            int index = 0;
            foreach (var weather in weatherTypes.Values)
            {
                if (index == currentWeatherId.Value) return weather;
                index++;
            }
            return weatherTypes["clear"];
        }

        public bool IsNight() => timeOfDay.Value < 6f || timeOfDay.Value > 20f;
        public float GetVisibility() => GetCurrentWeather().visibility * (IsNight() ? 0.5f : 1f);

        [ClientRpc]
        private void NotifyWeatherChangedClientRpc(string weatherId) { }
    }

    [Serializable]
    public class WeatherType
    {
        public string weatherId;
        public string weatherName;
        public string description;
        public float visibility;
        public WeatherEffects effects;
        public bool dangerous;
        public bool eventWeather;
    }

    [Serializable]
    public class WeatherEffects
    {
        public float movementSpeed = 1f;
        public float zombieSpawnRate = 1f;
        public float zombieDamage = 1f;
        public float zombieHealth = 1f;
        public float lootBonus = 1f;
        public float soundReduction = 1f;
    }
}
