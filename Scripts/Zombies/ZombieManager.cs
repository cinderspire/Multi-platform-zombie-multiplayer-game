using UnityEngine;
using System.Collections.Generic;

namespace DeadFrontier.Zombies
{
    /// <summary>
    /// Manages all zombies in the game with spatial partitioning for performance
    /// </summary>
    public class ZombieManager : Core.Singleton<ZombieManager>
    {
        [Header("Spawning")]
        [SerializeField] private ZombieConfig[] zombieTypes;
        [SerializeField] private Transform zombieContainer;
        [SerializeField] private int maxZombies = Core.Constants.MAX_ZOMBIES_PER_MATCH;

        [Header("Performance")]
        [SerializeField] private float spatialGridCellSize = Core.Constants.SPATIAL_GRID_CELL_SIZE;
        [SerializeField] private float updateInterval = 0.5f; // Update grid every 0.5s

        // Spatial partitioning for fast queries
        private Core.SpatialGrid<ZombieAI> spatialGrid;

        // Active zombies
        private List<ZombieAI> activeZombies = new List<ZombieAI>();
        private Dictionary<ZombieAI, GameObject> zombieObjects = new Dictionary<ZombieAI, GameObject>();

        // Update timer
        private float updateTimer;

        // Stats
        public int ActiveZombieCount => activeZombies.Count;
        public int MaxZombies => maxZombies;
        public bool CanSpawnMore => ActiveZombieCount < maxZombies;

        protected override void Awake()
        {
            base.Awake();

            // Initialize spatial grid
            spatialGrid = new Core.SpatialGrid<ZombieAI>(spatialGridCellSize);

            // Create container for zombies
            if (zombieContainer == null)
            {
                GameObject container = new GameObject("Zombies");
                zombieContainer = container.transform;
                zombieContainer.SetParent(transform);
            }

            Debug.Log("[ZombieManager] Initialized with spatial grid");
        }

        private void Update()
        {
            // Update spatial grid periodically
            updateTimer += Time.deltaTime;
            if (updateTimer >= updateInterval)
            {
                UpdateSpatialGrid();
                updateTimer = 0f;
            }
        }

        /// <summary>
        /// Spawns a zombie at a position
        /// </summary>
        public ZombieAI SpawnZombie(Vector3 position, ZombieType type = ZombieType.Walker)
        {
            if (!CanSpawnMore)
            {
                Debug.LogWarning("[ZombieManager] Cannot spawn more zombies, limit reached!");
                return null;
            }

            // Get config for zombie type
            ZombieConfig config = GetConfigForType(type);
            if (config == null || config.modelPrefab == null)
            {
                Debug.LogError($"[ZombieManager] No config or prefab for zombie type: {type}");
                return null;
            }

            // Instantiate zombie
            GameObject zombieObj = Instantiate(config.modelPrefab, position, Quaternion.identity, zombieContainer);
            ZombieAI zombie = zombieObj.GetComponent<ZombieAI>();

            if (zombie == null)
            {
                zombie = zombieObj.AddComponent<ZombieAI>();
            }

            // Ensure all required components exist
            if (zombieObj.GetComponent<ZombieHealth>() == null)
                zombieObj.AddComponent<ZombieHealth>();
            if (zombieObj.GetComponent<ZombieSensors>() == null)
                zombieObj.AddComponent<ZombieSensors>();
            if (zombieObj.GetComponent<UnityEngine.AI.NavMeshAgent>() == null)
                zombieObj.AddComponent<UnityEngine.AI.NavMeshAgent>();

            // Set configuration
            zombie.SetConfig(config);

            // Add to tracking
            RegisterZombie(zombie, zombieObj);

            Debug.Log($"[ZombieManager] Spawned {type} zombie at {position}");

            return zombie;
        }

        /// <summary>
        /// Spawns a random zombie type
        /// </summary>
        public ZombieAI SpawnRandomZombie(Vector3 position)
        {
            if (zombieTypes == null || zombieTypes.Length == 0)
            {
                return SpawnZombie(position, ZombieType.Walker);
            }

            ZombieConfig randomConfig = zombieTypes[Random.Range(0, zombieTypes.Length)];
            return SpawnZombie(position, randomConfig.zombieType);
        }

        /// <summary>
        /// Registers a zombie with the manager
        /// </summary>
        public void RegisterZombie(ZombieAI zombie, GameObject zombieObject)
        {
            if (zombie == null)
                return;

            if (!activeZombies.Contains(zombie))
            {
                activeZombies.Add(zombie);
                zombieObjects[zombie] = zombieObject;
                spatialGrid.Insert(zombie);

                // Subscribe to death event
                zombie.Health.OnDeath += () => UnregisterZombie(zombie);
            }
        }

        /// <summary>
        /// Unregisters a zombie (when it dies)
        /// </summary>
        public void UnregisterZombie(ZombieAI zombie)
        {
            if (zombie == null)
                return;

            activeZombies.Remove(zombie);
            zombieObjects.Remove(zombie);
            spatialGrid.Remove(zombie);

            Debug.Log("[ZombieManager] Zombie unregistered");
        }

        /// <summary>
        /// Gets all zombies within radius of a position
        /// Uses spatial grid for O(1) performance instead of O(n)
        /// </summary>
        public List<ZombieAI> GetZombiesInRange(Vector3 position, float radius)
        {
            return spatialGrid.Query(position, radius);
        }

        /// <summary>
        /// Gets nearest zombie to a position
        /// </summary>
        public ZombieAI GetNearestZombie(Vector3 position, float maxRadius = 100f)
        {
            return spatialGrid.QueryNearest(position, maxRadius);
        }

        /// <summary>
        /// Gets all active zombies
        /// </summary>
        public List<ZombieAI> GetAllZombies()
        {
            return new List<ZombieAI>(activeZombies);
        }

        /// <summary>
        /// Spawns a horde of zombies around a position
        /// </summary>
        public void SpawnHorde(Vector3 centerPosition, int count, float spawnRadius = 20f)
        {
            int spawnedCount = 0;
            int maxAttempts = count * 3; // Prevent infinite loops
            int attempts = 0;

            while (spawnedCount < count && attempts < maxAttempts)
            {
                attempts++;

                // Random position in circle
                Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
                Vector3 spawnPos = centerPosition + new Vector3(randomCircle.x, 0f, randomCircle.y);

                // Sample NavMesh to find valid position
                if (UnityEngine.AI.NavMesh.SamplePosition(spawnPos, out UnityEngine.AI.NavMeshHit hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    SpawnRandomZombie(hit.position);
                    spawnedCount++;
                }
            }

            Debug.Log($"[ZombieManager] Spawned horde of {spawnedCount} zombies at {centerPosition}");
        }

        /// <summary>
        /// Clears all zombies
        /// </summary>
        public void ClearAllZombies()
        {
            foreach (var zombie in activeZombies.ToArray())
            {
                if (zombie != null && zombieObjects.TryGetValue(zombie, out GameObject obj))
                {
                    Destroy(obj);
                }
            }

            activeZombies.Clear();
            zombieObjects.Clear();
            spatialGrid.Clear();

            Debug.Log("[ZombieManager] All zombies cleared");
        }

        /// <summary>
        /// Updates spatial grid with current zombie positions
        /// </summary>
        private void UpdateSpatialGrid()
        {
            // Remove dead zombies
            activeZombies.RemoveAll(z => z == null || z.Health.IsDead);

            // Update positions
            spatialGrid.UpdateAll();
        }

        /// <summary>
        /// Gets config for a zombie type
        /// </summary>
        private ZombieConfig GetConfigForType(ZombieType type)
        {
            if (zombieTypes == null)
                return null;

            foreach (var config in zombieTypes)
            {
                if (config != null && config.zombieType == type)
                {
                    return config;
                }
            }

            return zombieTypes.Length > 0 ? zombieTypes[0] : null;
        }

        /// <summary>
        /// Gets zombie statistics
        /// </summary>
        public string GetStats()
        {
            return $"Active Zombies: {ActiveZombieCount}/{maxZombies}\n" +
                   $"Spatial Grid Cells: {spatialGrid.CellCount}\n" +
                   $"Grid Cell Size: {spatialGridCellSize}m";
        }

        private void OnDrawGizmos()
        {
            // Draw spatial grid in editor
            if (spatialGrid != null && Application.isPlaying)
            {
                spatialGrid.DrawGizmos(Vector3.zero, 20, 20);
            }
        }
    }
}
