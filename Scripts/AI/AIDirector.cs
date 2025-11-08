using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;
using System;

namespace DeadFrontier.AI
{
    /// <summary>
    /// AI Director dynamically controls zombie spawning, difficulty, and pacing based on player performance.
    /// Inspired by Left 4 Dead's Director system for optimal tension and flow.
    /// </summary>
    public class AIDirector : NetworkBehaviour
    {
        public static AIDirector Instance { get; private set; }

        [Header("Difficulty Settings")]
        [SerializeField] private float baseDifficulty = 1f;
        [SerializeField] private float maxDifficulty = 3f;
        [SerializeField] private float difficultyIncreaseRate = 0.1f;
        [SerializeField] private float difficultyDecreaseRate = 0.05f;

        [Header("Intensity Management")]
        [SerializeField] private float minIntensity = 0.2f;
        [SerializeField] private float maxIntensity = 1f;
        [SerializeField] private float intensityBuildRate = 0.15f;
        [SerializeField] private float intensityDecayRate = 0.3f;
        [SerializeField] private float peakIntensityDuration = 45f;
        [SerializeField] private float relaxDuration = 30f;

        [Header("Spawn Control")]
        [SerializeField] private int baseZombiesPerWave = 5;
        [SerializeField] private int maxZombiesPerWave = 20;
        [SerializeField] private float baseSpawnInterval = 10f;
        [SerializeField] private float minSpawnInterval = 3f;
        [SerializeField] private int maxConcurrentZombies = 50;

        [Header("Special Events")]
        [SerializeField] private float hordeChance = 0.15f;
        [SerializeField] private float hordeMinInterval = 120f;
        [SerializeField] private int hordeSize = 30;
        [SerializeField] private float bossSpawnChance = 0.05f;
        [SerializeField] private float bossMinInterval = 180f;

        [Header("Player Performance Tracking")]
        [SerializeField] private float performanceWeight = 0.3f;
        [SerializeField] private float healthWeight = 0.4f;
        [SerializeField] private float progressWeight = 0.3f;

        // Current state
        private NetworkVariable<float> currentDifficulty = new NetworkVariable<float>(1f);
        private NetworkVariable<float> currentIntensity = new NetworkVariable<float>(0.5f);
        private float lastHordeTime;
        private float lastBossTime;
        private float intensityTimer;
        private DirectorState currentState = DirectorState.Building;

        // Performance tracking
        private Dictionary<ulong, PlayerPerformance> playerPerformance = new Dictionary<ulong, PlayerPerformance>();
        private float averagePlayerHealth = 1f;
        private float averagePlayerPerformance = 0.5f;

        // Spawn management
        private float nextSpawnTime;
        private List<SpawnPoint> spawnPoints = new List<SpawnPoint>();
        private int activeZombieCount;

        // Events
        public event Action<float> OnDifficultyChanged;
        public event Action<float> OnIntensityChanged;
        public event Action OnHordeTriggered;
        public event Action OnBossSpawned;

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
                InitializeDirector();
                FindSpawnPoints();
            }

            currentDifficulty.OnValueChanged += OnDifficultyValueChanged;
            currentIntensity.OnValueChanged += OnIntensityValueChanged;
        }

        public override void OnNetworkDespawn()
        {
            currentDifficulty.OnValueChanged -= OnDifficultyValueChanged;
            currentIntensity.OnValueChanged -= OnIntensityValueChanged;
            base.OnNetworkDespawn();
        }

        private void Update()
        {
            if (!IsServer) return;

            UpdatePlayerPerformance();
            UpdateDifficulty();
            UpdateIntensity();
            ManageSpawning();
            CheckSpecialEvents();
        }

        #region Initialization

        private void InitializeDirector()
        {
            currentDifficulty.Value = baseDifficulty;
            currentIntensity.Value = minIntensity;
            nextSpawnTime = Time.time + baseSpawnInterval;
            lastHordeTime = Time.time;
            lastBossTime = Time.time;

            Debug.Log("[AIDirector] Initialized with base difficulty: " + baseDifficulty);
        }

        private void FindSpawnPoints()
        {
            spawnPoints.Clear();
            var points = FindObjectsOfType<SpawnPoint>();
            spawnPoints.AddRange(points);

            Debug.Log($"[AIDirector] Found {spawnPoints.Count} spawn points");
        }

        #endregion

        #region Difficulty Management

        private void UpdateDifficulty()
        {
            float targetDifficulty = CalculateTargetDifficulty();
            float difficultyChange = 0f;

            if (currentDifficulty.Value < targetDifficulty)
            {
                difficultyChange = difficultyIncreaseRate * Time.deltaTime;
            }
            else if (currentDifficulty.Value > targetDifficulty)
            {
                difficultyChange = -difficultyDecreaseRate * Time.deltaTime;
            }

            if (difficultyChange != 0f)
            {
                currentDifficulty.Value = Mathf.Clamp(
                    currentDifficulty.Value + difficultyChange,
                    baseDifficulty,
                    maxDifficulty
                );
            }
        }

        private float CalculateTargetDifficulty()
        {
            // Base difficulty increases over time
            float timeFactor = Mathf.Min(Time.time / 600f, 1f); // Max at 10 minutes

            // Adjust based on player performance
            float performanceFactor = (1f - averagePlayerPerformance) * performanceWeight;

            // Adjust based on player health
            float healthFactor = (1f - averagePlayerHealth) * healthWeight;

            // Combine factors
            float targetDifficulty = baseDifficulty + (timeFactor * 0.5f) + performanceFactor + healthFactor;

            return Mathf.Clamp(targetDifficulty, baseDifficulty, maxDifficulty);
        }

        private void OnDifficultyValueChanged(float previousValue, float newValue)
        {
            OnDifficultyChanged?.Invoke(newValue);
            Debug.Log($"[AIDirector] Difficulty changed: {previousValue:F2} -> {newValue:F2}");
        }

        #endregion

        #region Intensity Management

        private void UpdateIntensity()
        {
            intensityTimer += Time.deltaTime;

            switch (currentState)
            {
                case DirectorState.Building:
                    // Gradually increase intensity
                    currentIntensity.Value = Mathf.Min(
                        currentIntensity.Value + intensityBuildRate * Time.deltaTime,
                        maxIntensity
                    );

                    if (intensityTimer >= peakIntensityDuration || currentIntensity.Value >= maxIntensity)
                    {
                        currentState = DirectorState.Peak;
                        intensityTimer = 0f;
                    }
                    break;

                case DirectorState.Peak:
                    // Maintain high intensity
                    currentIntensity.Value = maxIntensity;

                    if (intensityTimer >= peakIntensityDuration)
                    {
                        currentState = DirectorState.Relax;
                        intensityTimer = 0f;
                    }
                    break;

                case DirectorState.Relax:
                    // Decay intensity
                    currentIntensity.Value = Mathf.Max(
                        currentIntensity.Value - intensityDecayRate * Time.deltaTime,
                        minIntensity
                    );

                    if (intensityTimer >= relaxDuration || currentIntensity.Value <= minIntensity)
                    {
                        currentState = DirectorState.Building;
                        intensityTimer = 0f;
                    }
                    break;
            }

            // Adjust for player health - if players are low, reduce intensity
            if (averagePlayerHealth < 0.3f)
            {
                currentIntensity.Value *= 0.7f;
            }
        }

        private void OnIntensityValueChanged(float previousValue, float newValue)
        {
            OnIntensityChanged?.Invoke(newValue);
        }

        #endregion

        #region Spawn Management

        private void ManageSpawning()
        {
            if (Time.time < nextSpawnTime) return;
            if (activeZombieCount >= maxConcurrentZombies) return;

            int zombiesToSpawn = CalculateSpawnCount();
            SpawnZombieWave(zombiesToSpawn);

            // Calculate next spawn time
            float spawnInterval = Mathf.Lerp(
                baseSpawnInterval,
                minSpawnInterval,
                currentIntensity.Value
            );
            nextSpawnTime = Time.time + spawnInterval;
        }

        private int CalculateSpawnCount()
        {
            // Base spawn count scaled by difficulty and intensity
            int spawnCount = Mathf.RoundToInt(
                baseZombiesPerWave * currentDifficulty.Value * currentIntensity.Value
            );

            // Clamp to max
            spawnCount = Mathf.Clamp(spawnCount, 1, maxZombiesPerWave);

            // Reduce if near max concurrent zombies
            int remaining = maxConcurrentZombies - activeZombieCount;
            spawnCount = Mathf.Min(spawnCount, remaining);

            return spawnCount;
        }

        private void SpawnZombieWave(int count)
        {
            if (spawnPoints.Count == 0 || ZombieManager.Instance == null) return;

            for (int i = 0; i < count; i++)
            {
                // Select spawn point far from players
                SpawnPoint spawnPoint = SelectSpawnPoint();
                if (spawnPoint == null) continue;

                // Select zombie type based on difficulty
                string zombieType = SelectZombieType();

                // Spawn zombie
                ZombieManager.Instance.SpawnZombie(zombieType, spawnPoint.transform.position);
                activeZombieCount++;
            }

            Debug.Log($"[AIDirector] Spawned wave of {count} zombies. Total active: {activeZombieCount}");
        }

        private SpawnPoint SelectSpawnPoint()
        {
            if (spawnPoints.Count == 0) return null;

            // Get all players
            var players = GetAllPlayerPositions();
            if (players.Count == 0) return spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Count)];

            // Find spawn points far from all players
            var validPoints = spawnPoints.Where(sp =>
            {
                float minDistance = players.Min(p => Vector3.Distance(sp.transform.position, p));
                return minDistance > 30f; // At least 30 meters from nearest player
            }).ToList();

            if (validPoints.Count == 0)
                validPoints = spawnPoints; // Fallback to any point

            return validPoints[UnityEngine.Random.Range(0, validPoints.Count)];
        }

        private string SelectZombieType()
        {
            // Weight zombie types by difficulty
            float random = UnityEngine.Random.value;

            if (currentDifficulty.Value < 1.5f)
            {
                // Low difficulty - mostly common zombies
                if (random < 0.8f) return "Common";
                if (random < 0.95f) return "Fast";
                return "Tank";
            }
            else if (currentDifficulty.Value < 2.5f)
            {
                // Medium difficulty - mixed
                if (random < 0.5f) return "Common";
                if (random < 0.75f) return "Fast";
                if (random < 0.9f) return "Tank";
                return "Spitter";
            }
            else
            {
                // High difficulty - more specials
                if (random < 0.3f) return "Common";
                if (random < 0.5f) return "Fast";
                if (random < 0.7f) return "Tank";
                if (random < 0.85f) return "Spitter";
                return "Exploder";
            }
        }

        public void OnZombieDeath()
        {
            activeZombieCount = Mathf.Max(0, activeZombieCount - 1);
        }

        #endregion

        #region Special Events

        private void CheckSpecialEvents()
        {
            // Check for horde spawn
            if (Time.time - lastHordeTime > hordeMinInterval)
            {
                if (currentIntensity.Value > 0.8f && UnityEngine.Random.value < hordeChance * Time.deltaTime)
                {
                    TriggerHorde();
                }
            }

            // Check for boss spawn
            if (Time.time - lastBossTime > bossMinInterval)
            {
                if (currentDifficulty.Value > 2f && UnityEngine.Random.value < bossSpawnChance * Time.deltaTime)
                {
                    SpawnBoss();
                }
            }
        }

        private void TriggerHorde()
        {
            lastHordeTime = Time.time;
            OnHordeTriggered?.Invoke();

            // Notify players
            NotifyHordeIncomingClientRpc();

            // Spawn horde after delay
            Invoke(nameof(SpawnHordeWave), 5f);

            Debug.Log("[AIDirector] Horde triggered!");
        }

        private void SpawnHordeWave()
        {
            int zombiesToSpawn = Mathf.RoundToInt(hordeSize * currentDifficulty.Value);

            for (int i = 0; i < zombiesToSpawn; i++)
            {
                SpawnPoint spawnPoint = SelectSpawnPoint();
                if (spawnPoint != null && ZombieManager.Instance != null)
                {
                    ZombieManager.Instance.SpawnZombie("Common", spawnPoint.transform.position);
                    activeZombieCount++;
                }
            }

            Debug.Log($"[AIDirector] Spawned horde of {zombiesToSpawn} zombies");
        }

        private void SpawnBoss()
        {
            lastBossTime = Time.time;
            OnBossSpawned?.Invoke();

            SpawnPoint spawnPoint = SelectSpawnPoint();
            if (spawnPoint != null && ZombieManager.Instance != null)
            {
                // Spawn boss zombie type
                ZombieManager.Instance.SpawnZombie("Boss", spawnPoint.transform.position);
                activeZombieCount++;
            }

            // Notify players
            NotifyBossSpawnedClientRpc();

            Debug.Log("[AIDirector] Boss spawned!");
        }

        #endregion

        #region Performance Tracking

        private void UpdatePlayerPerformance()
        {
            var players = NetworkManager.Singleton.ConnectedClientsList;
            if (players.Count == 0) return;

            float totalHealth = 0f;
            float totalPerformance = 0f;
            int validPlayers = 0;

            foreach (var client in players)
            {
                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
                    client.ClientId, out NetworkObject netObj))
                {
                    var health = netObj.GetComponent<Player.PlayerHealth>();
                    if (health != null && health.IsAlive)
                    {
                        totalHealth += health.GetHealthPercentage();
                        totalPerformance += GetPlayerPerformanceScore(client.ClientId);
                        validPlayers++;
                    }
                }
            }

            if (validPlayers > 0)
            {
                averagePlayerHealth = totalHealth / validPlayers;
                averagePlayerPerformance = totalPerformance / validPlayers;
            }
        }

        private float GetPlayerPerformanceScore(ulong playerId)
        {
            if (!playerPerformance.ContainsKey(playerId))
            {
                playerPerformance[playerId] = new PlayerPerformance();
            }

            var perf = playerPerformance[playerId];

            // Calculate score based on kills, deaths, damage taken
            float score = 0f;
            score += perf.zombieKills * 0.01f;
            score -= perf.deaths * 0.2f;
            score -= perf.damageTaken * 0.001f;

            return Mathf.Clamp01(score);
        }

        public void RecordPlayerKill(ulong playerId)
        {
            if (!playerPerformance.ContainsKey(playerId))
            {
                playerPerformance[playerId] = new PlayerPerformance();
            }
            playerPerformance[playerId].zombieKills++;
        }

        public void RecordPlayerDeath(ulong playerId)
        {
            if (!playerPerformance.ContainsKey(playerId))
            {
                playerPerformance[playerId] = new PlayerPerformance();
            }
            playerPerformance[playerId].deaths++;
        }

        public void RecordPlayerDamage(ulong playerId, float damage)
        {
            if (!playerPerformance.ContainsKey(playerId))
            {
                playerPerformance[playerId] = new PlayerPerformance();
            }
            playerPerformance[playerId].damageTaken += damage;
        }

        #endregion

        #region Client RPCs

        [ClientRpc]
        private void NotifyHordeIncomingClientRpc()
        {
            Core.NotificationManager.Instance?.ShowNotification(
                "HORDE INCOMING!",
                NotificationType.Warning,
                5f
            );

            Core.AudioManager.Instance?.PlaySFX("HordeWarning");
        }

        [ClientRpc]
        private void NotifyBossSpawnedClientRpc()
        {
            Core.NotificationManager.Instance?.ShowNotification(
                "BOSS ZOMBIE SPAWNED!",
                NotificationType.Danger,
                5f
            );

            Core.AudioManager.Instance?.PlaySFX("BossWarning");
        }

        #endregion

        #region Helpers

        private List<Vector3> GetAllPlayerPositions()
        {
            var positions = new List<Vector3>();

            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
                    client.ClientId, out NetworkObject netObj))
                {
                    positions.Add(netObj.transform.position);
                }
            }

            return positions;
        }

        #endregion

        #region Public Getters

        public float GetCurrentDifficulty() => currentDifficulty.Value;
        public float GetCurrentIntensity() => currentIntensity.Value;
        public DirectorState GetCurrentState() => currentState;
        public int GetActiveZombieCount() => activeZombieCount;

        #endregion
    }

    #region Supporting Classes

    public class PlayerPerformance
    {
        public int zombieKills;
        public int deaths;
        public float damageTaken;
    }

    public class SpawnPoint : MonoBehaviour
    {
        public bool isActive = true;
    }

    public enum DirectorState
    {
        Building,   // Intensity is increasing
        Peak,       // Maximum intensity
        Relax       // Intensity decreasing
    }

    #endregion
}
