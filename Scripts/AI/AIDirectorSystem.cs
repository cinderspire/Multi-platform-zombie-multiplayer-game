using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;
using System;

namespace DeadFrontier.AI
{
    /// <summary>
    /// Comprehensive AI Director and spawn management system.
    /// Dynamically controls enemy spawns, difficulty scaling, and encounter pacing.
    /// Ensures balanced gameplay through adaptive spawn logic and intensity management.
    /// </summary>
    public class AIDirectorSystem : NetworkBehaviour
    {
        public static AIDirectorSystem Instance { get; private set; }

        [Header("Spawn Settings")]
        [SerializeField] private float spawnCheckInterval = 5f;
        [SerializeField] private int maxActiveEnemies = 50;
        [SerializeField] private int minActiveEnemies = 10;
        [SerializeField] private float spawnRadius = 100f;

        [Header("Difficulty Settings")]
        [SerializeField] private bool enableDynamicDifficulty = true;
        [SerializeField] private float difficultyIncreaseRate = 0.1f; // Per minute
        [SerializeField] private float difficultyDecreaseRate = 0.05f;
        [SerializeField] private float maxDifficulty = 10f;

        [Header("Intensity Settings")]
        [SerializeField] private float targetIntensity = 0.5f; // 0-1 scale
        [SerializeField] private float intensityChangeRate = 0.1f;
        [SerializeField] private float relaxPhaseDuration = 30f; // Seconds
        [SerializeField] private float peakPhaseDuration = 45f;

        [Header("Wave Settings")]
        [SerializeField] private bool enableWaveSpawns = true;
        [SerializeField] private float waveCooldown = 120f; // 2 minutes
        [SerializeField] private int minWaveSize = 5;
        [SerializeField] private int maxWaveSize = 15;

        // Enemy type definitions
        private Dictionary<string, EnemyTypeDefinition> enemyTypes = new Dictionary<string, EnemyTypeDefinition>();

        // Spawn zones
        private Dictionary<string, SpawnZone> spawnZones = new Dictionary<string, SpawnZone>();

        // Active spawns
        private Dictionary<string, ActiveEnemy> activeEnemies = new Dictionary<string, ActiveEnemy>();

        // Director state
        private DirectorState currentState;
        private float currentDifficulty = 1f;
        private float currentIntensity = 0.3f;
        private DateTime lastWaveTime;
        private DateTime lastPhaseChange;

        // Player tracking
        private Dictionary<ulong, PlayerThreatData> playerThreats = new Dictionary<ulong, PlayerThreatData>();

        // Events
        public event Action<float> OnDifficultyChanged;
        public event Action<float> OnIntensityChanged;
        public event Action OnWaveSpawned;

        private float spawnTimer = 0f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeEnemyTypes();
                InitializeSpawnZones();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            currentState = new DirectorState
            {
                currentPhase = DirectorPhase.Building,
                phaseStartTime = DateTime.UtcNow
            };

            lastWaveTime = DateTime.UtcNow;
            lastPhaseChange = DateTime.UtcNow;
        }

        private void Update()
        {
            if (!IsServer) return;

            spawnTimer += Time.deltaTime;

            if (spawnTimer >= spawnCheckInterval)
            {
                spawnTimer = 0f;
                ProcessSpawning();
            }

            UpdateDifficulty();
            UpdateIntensity();
            UpdatePhases();
            CleanupDeadEnemies();
        }

        #region Initialization

        private void InitializeEnemyTypes()
        {
            // Common Zombies
            RegisterEnemyType(new EnemyTypeDefinition
            {
                typeId = "zombie_walker",
                typeName = "Walker",
                difficultyRating = 1,
                threatLevel = ThreatLevel.Low,
                spawnWeight = 50,
                maxSimultaneous = 20,
                health = 100,
                damage = 10,
                speed = 2f,
                lootTableId = "loot_common"
            });

            RegisterEnemyType(new EnemyTypeDefinition
            {
                typeId = "zombie_runner",
                typeName = "Runner",
                difficultyRating = 2,
                threatLevel = ThreatLevel.Medium,
                spawnWeight = 30,
                maxSimultaneous = 10,
                health = 80,
                damage = 15,
                speed = 5f,
                lootTableId = "loot_common"
            });

            // Elite Enemies
            RegisterEnemyType(new EnemyTypeDefinition
            {
                typeId = "zombie_brute",
                typeName = "Brute",
                difficultyRating = 5,
                threatLevel = ThreatLevel.High,
                spawnWeight = 5,
                maxSimultaneous = 2,
                health = 500,
                damage = 40,
                speed = 3f,
                lootTableId = "loot_rare",
                requiresMinDifficulty = 3f
            });

            RegisterEnemyType(new EnemyTypeDefinition
            {
                typeId = "zombie_screamer",
                typeName = "Screamer",
                difficultyRating = 3,
                threatLevel = ThreatLevel.Medium,
                spawnWeight = 15,
                maxSimultaneous = 5,
                health = 150,
                damage = 5,
                speed = 4f,
                lootTableId = "loot_uncommon",
                specialAbility = "SummonHorde"
            });

            // Boss
            RegisterEnemyType(new EnemyTypeDefinition
            {
                typeId = "zombie_boss_tank",
                typeName = "Tank",
                difficultyRating = 10,
                threatLevel = ThreatLevel.Boss,
                spawnWeight = 1,
                maxSimultaneous = 1,
                health = 2000,
                damage = 80,
                speed = 2.5f,
                lootTableId = "loot_boss",
                requiresMinDifficulty = 7f,
                isBoss = true
            });

            Debug.Log($"[AIDirector] Initialized {enemyTypes.Count} enemy types");
        }

        private void InitializeSpawnZones()
        {
            // Auto-create spawn zones (in real implementation, these would be placed in the map)
            for (int i = 0; i < 10; i++)
            {
                string zoneId = $"zone_{i}";
                spawnZones[zoneId] = new SpawnZone
                {
                    zoneId = zoneId,
                    position = new Vector3(UnityEngine.Random.Range(-500f, 500f), 0, UnityEngine.Random.Range(-500f, 500f)),
                    radius = 20f,
                    isActive = true,
                    maxEnemies = 10,
                    spawnCooldown = 30f,
                    lastSpawnTime = DateTime.MinValue
                };
            }

            Debug.Log($"[AIDirector] Initialized {spawnZones.Count} spawn zones");
        }

        private void RegisterEnemyType(EnemyTypeDefinition enemyType)
        {
            enemyTypes[enemyType.typeId] = enemyType;
        }

        #endregion

        #region Spawn Management

        private void ProcessSpawning()
        {
            int activeCount = activeEnemies.Count;

            // Check if we need to spawn more
            if (activeCount < minActiveEnemies || (activeCount < maxActiveEnemies && ShouldSpawnMore()))
            {
                SpawnEnemies();
            }

            // Check for wave spawns
            if (enableWaveSpawns && ShouldSpawnWave())
            {
                SpawnWave();
            }
        }

        private bool ShouldSpawnMore()
        {
            // Spawn based on intensity and phase
            float spawnChance = currentIntensity;

            if (currentState.currentPhase == DirectorPhase.Peak)
            {
                spawnChance *= 1.5f;
            }
            else if (currentState.currentPhase == DirectorPhase.Relax)
            {
                spawnChance *= 0.5f;
            }

            return UnityEngine.Random.value < spawnChance * 0.3f; // 30% base chance scaled by intensity
        }

        private void SpawnEnemies()
        {
            // Determine how many to spawn
            int spawnCount = CalculateSpawnCount();

            for (int i = 0; i < spawnCount; i++)
            {
                // Select enemy type
                string enemyTypeId = SelectEnemyType();

                if (string.IsNullOrEmpty(enemyTypeId)) continue;

                // Select spawn zone
                var zone = SelectSpawnZone();

                if (zone == null) continue;

                // Spawn enemy
                SpawnEnemy(enemyTypeId, zone);
            }
        }

        private int CalculateSpawnCount()
        {
            int baseCount = Mathf.RoundToInt(currentIntensity * 3f) + 1;

            // Adjust for difficulty
            baseCount = Mathf.RoundToInt(baseCount * (1f + currentDifficulty * 0.1f));

            // Cap based on max active
            int available = maxActiveEnemies - activeEnemies.Count;

            return Mathf.Min(baseCount, available);
        }

        private string SelectEnemyType()
        {
            // Filter by difficulty and max simultaneous
            var availableTypes = enemyTypes.Values
                .Where(t => t.requiresMinDifficulty <= currentDifficulty &&
                           CountEnemyType(t.typeId) < t.maxSimultaneous)
                .ToList();

            if (availableTypes.Count == 0) return null;

            // Weighted random selection
            float totalWeight = availableTypes.Sum(t => t.spawnWeight);
            float randomValue = UnityEngine.Random.Range(0f, totalWeight);

            float cumulativeWeight = 0f;

            foreach (var type in availableTypes)
            {
                cumulativeWeight += type.spawnWeight;

                if (randomValue <= cumulativeWeight)
                {
                    return type.typeId;
                }
            }

            return availableTypes.First().typeId;
        }

        private SpawnZone SelectSpawnZone()
        {
            // Find zones that are ready to spawn
            var availableZones = spawnZones.Values
                .Where(z => z.isActive &&
                           CountEnemiesInZone(z.zoneId) < z.maxEnemies &&
                           (DateTime.UtcNow - z.lastSpawnTime).TotalSeconds >= z.spawnCooldown)
                .ToList();

            if (availableZones.Count == 0) return null;

            // Prefer zones farther from players
            // In real implementation, would check player positions
            return availableZones[UnityEngine.Random.Range(0, availableZones.Count)];
        }

        private void SpawnEnemy(string typeId, SpawnZone zone)
        {
            var enemyType = enemyTypes[typeId];

            string enemyId = $"enemy_{typeId}_{DateTime.UtcNow.Ticks}_{UnityEngine.Random.Range(0, 10000)}";

            Vector3 spawnPosition = zone.position + UnityEngine.Random.insideUnitSphere * zone.radius;
            spawnPosition.y = 0; // Ground level

            var enemy = new ActiveEnemy
            {
                enemyId = enemyId,
                typeId = typeId,
                spawnZoneId = zone.zoneId,
                spawnPosition = spawnPosition,
                spawnTime = DateTime.UtcNow,
                currentHealth = enemyType.health,
                maxHealth = enemyType.health,
                isAlive = true
            };

            activeEnemies[enemyId] = enemy;
            zone.lastSpawnTime = DateTime.UtcNow;

            Debug.Log($"[AIDirector] Spawned {enemyType.typeName} at zone {zone.zoneId}");

            // Notify clients to spawn enemy
            SpawnEnemyClientRpc(enemyId, typeId, spawnPosition);
        }

        [ClientRpc]
        private void SpawnEnemyClientRpc(string enemyId, string typeId, Vector3 position)
        {
            // Client-side enemy instantiation
            Debug.Log($"[AIDirector] Client: Spawn {typeId} at {position}");
        }

        #endregion

        #region Wave Spawns

        private bool ShouldSpawnWave()
        {
            TimeSpan timeSinceWave = DateTime.UtcNow - lastWaveTime;

            if (timeSinceWave.TotalSeconds < waveCooldown) return false;

            // Higher intensity = more likely to spawn wave
            return UnityEngine.Random.value < (currentIntensity * 0.5f);
        }

        private void SpawnWave()
        {
            int waveSize = UnityEngine.Random.Range(minWaveSize, maxWaveSize + 1);

            // Scale with difficulty
            waveSize = Mathf.RoundToInt(waveSize * (1f + currentDifficulty * 0.2f));

            Debug.Log($"[AIDirector] Spawning wave of {waveSize} enemies");

            // Select primary enemy type for wave
            string primaryType = SelectEnemyType();

            // Spawn wave
            for (int i = 0; i < waveSize; i++)
            {
                var zone = SelectSpawnZone();

                if (zone == null) break;

                // 70% chance for primary type, 30% for random
                string typeId = UnityEngine.Random.value < 0.7f ? primaryType : SelectEnemyType();

                if (!string.IsNullOrEmpty(typeId))
                {
                    SpawnEnemy(typeId, zone);
                }
            }

            lastWaveTime = DateTime.UtcNow;

            OnWaveSpawned?.Invoke();

            // Notify clients
            SpawnWaveClientRpc(waveSize);
        }

        [ClientRpc]
        private void SpawnWaveClientRpc(int waveSize)
        {
            Debug.Log($"[AIDirector] INCOMING WAVE: {waveSize} enemies!");
        }

        #endregion

        #region Difficulty Management

        private void UpdateDifficulty()
        {
            if (!enableDynamicDifficulty) return;

            float oldDifficulty = currentDifficulty;

            // Increase difficulty over time
            currentDifficulty += (difficultyIncreaseRate / 60f) * Time.deltaTime;

            // Decrease if players are struggling
            if (IsPlayersStruggling())
            {
                currentDifficulty -= (difficultyDecreaseRate / 60f) * Time.deltaTime;
            }

            // Clamp
            currentDifficulty = Mathf.Clamp(currentDifficulty, 1f, maxDifficulty);

            if (Mathf.Abs(currentDifficulty - oldDifficulty) > 0.1f)
            {
                OnDifficultyChanged?.Invoke(currentDifficulty);
            }
        }

        private bool IsPlayersStruggling()
        {
            // Check player threat levels
            int highThreatPlayers = playerThreats.Values.Count(p => p.threatLevel >= 0.8f);
            int totalPlayers = playerThreats.Count;

            if (totalPlayers == 0) return false;

            return (float)highThreatPlayers / totalPlayers > 0.5f; // More than half struggling
        }

        public void UpdatePlayerThreat(ulong playerId, float healthPercent, float ammoPercent, int recentDeaths)
        {
            if (!playerThreats.ContainsKey(playerId))
            {
                playerThreats[playerId] = new PlayerThreatData { playerId = playerId };
            }

            var threat = playerThreats[playerId];

            // Calculate threat level (higher = more danger)
            float threatLevel = 0f;

            threatLevel += (1f - healthPercent) * 0.4f; // Low health = high threat
            threatLevel += (1f - ammoPercent) * 0.3f; // Low ammo = high threat
            threatLevel += (recentDeaths / 5f) * 0.3f; // Recent deaths = high threat

            threat.threatLevel = Mathf.Clamp01(threatLevel);
            threat.lastUpdateTime = DateTime.UtcNow;
        }

        #endregion

        #region Intensity Management

        private void UpdateIntensity()
        {
            float targetIntensityValue = targetIntensity;

            // Adjust based on phase
            if (currentState.currentPhase == DirectorPhase.Peak)
            {
                targetIntensityValue = 0.8f;
            }
            else if (currentState.currentPhase == DirectorPhase.Relax)
            {
                targetIntensityValue = 0.2f;
            }

            // Smooth transition
            currentIntensity = Mathf.Lerp(currentIntensity, targetIntensityValue, intensityChangeRate * Time.deltaTime);

            OnIntensityChanged?.Invoke(currentIntensity);
        }

        private void UpdatePhases()
        {
            TimeSpan phaseTime = DateTime.UtcNow - lastPhaseChange;

            switch (currentState.currentPhase)
            {
                case DirectorPhase.Building:
                    if (currentIntensity >= 0.6f || phaseTime.TotalSeconds >= 60f)
                    {
                        ChangePhase(DirectorPhase.Peak);
                    }
                    break;

                case DirectorPhase.Peak:
                    if (phaseTime.TotalSeconds >= peakPhaseDuration)
                    {
                        ChangePhase(DirectorPhase.Relax);
                    }
                    break;

                case DirectorPhase.Relax:
                    if (phaseTime.TotalSeconds >= relaxPhaseDuration)
                    {
                        ChangePhase(DirectorPhase.Building);
                    }
                    break;
            }
        }

        private void ChangePhase(DirectorPhase newPhase)
        {
            currentState.currentPhase = newPhase;
            currentState.phaseStartTime = DateTime.UtcNow;
            lastPhaseChange = DateTime.UtcNow;

            Debug.Log($"[AIDirector] Phase changed to: {newPhase}");
        }

        #endregion

        #region Enemy Management

        public void RegisterEnemyDeath(string enemyId, ulong killerId)
        {
            if (!activeEnemies.ContainsKey(enemyId)) return;

            var enemy = activeEnemies[enemyId];
            enemy.isAlive = false;
            enemy.deathTime = DateTime.UtcNow;

            activeEnemies.Remove(enemyId);

            Debug.Log($"[AIDirector] Enemy {enemyId} killed by player {killerId}");
        }

        public void UpdateEnemyHealth(string enemyId, float newHealth)
        {
            if (!activeEnemies.ContainsKey(enemyId)) return;

            activeEnemies[enemyId].currentHealth = newHealth;

            if (newHealth <= 0)
            {
                RegisterEnemyDeath(enemyId, 0);
            }
        }

        private void CleanupDeadEnemies()
        {
            // Remove enemies that have been dead for a while
            var toRemove = activeEnemies.Values
                .Where(e => !e.isAlive && (DateTime.UtcNow - e.deathTime).TotalSeconds > 60f)
                .Select(e => e.enemyId)
                .ToList();

            foreach (var enemyId in toRemove)
            {
                activeEnemies.Remove(enemyId);
            }
        }

        #endregion

        #region Helper Methods

        private int CountEnemyType(string typeId)
        {
            return activeEnemies.Values.Count(e => e.typeId == typeId && e.isAlive);
        }

        private int CountEnemiesInZone(string zoneId)
        {
            return activeEnemies.Values.Count(e => e.spawnZoneId == zoneId && e.isAlive);
        }

        #endregion

        #region Public Getters

        public float GetCurrentDifficulty() => currentDifficulty;
        public float GetCurrentIntensity() => currentIntensity;
        public DirectorPhase GetCurrentPhase() => currentState.currentPhase;
        public int GetActiveEnemyCount() => activeEnemies.Count(e => e.Value.isAlive);

        public List<ActiveEnemy> GetActiveEnemies()
        {
            return activeEnemies.Values.Where(e => e.isAlive).ToList();
        }

        #endregion
    }

    #region Data Classes

    [Serializable]
    public class EnemyTypeDefinition
    {
        public string typeId;
        public string typeName;
        public int difficultyRating; // 1-10
        public ThreatLevel threatLevel;
        public int spawnWeight; // Higher = more likely to spawn
        public int maxSimultaneous;
        public float health;
        public float damage;
        public float speed;
        public string lootTableId;
        public float requiresMinDifficulty;
        public string specialAbility;
        public bool isBoss;
    }

    [Serializable]
    public class SpawnZone
    {
        public string zoneId;
        public Vector3 position;
        public float radius;
        public bool isActive;
        public int maxEnemies;
        public float spawnCooldown;
        public DateTime lastSpawnTime;
    }

    [Serializable]
    public class ActiveEnemy
    {
        public string enemyId;
        public string typeId;
        public string spawnZoneId;
        public Vector3 spawnPosition;
        public DateTime spawnTime;
        public DateTime deathTime;
        public float currentHealth;
        public float maxHealth;
        public bool isAlive;
    }

    [Serializable]
    public class DirectorState
    {
        public DirectorPhase currentPhase;
        public DateTime phaseStartTime;
    }

    [Serializable]
    public class PlayerThreatData
    {
        public ulong playerId;
        public float threatLevel; // 0-1, higher = more danger
        public DateTime lastUpdateTime;
    }

    public enum DirectorPhase
    {
        Building,   // Intensity building up
        Peak,       // High intensity combat
        Relax       // Cool down period
    }

    public enum ThreatLevel
    {
        Low,
        Medium,
        High,
        Boss
    }

    #endregion
}
