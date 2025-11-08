using UnityEngine;
using System.Collections.Generic;

namespace DeadFrontier.Zombies
{
    /// <summary>
    /// Extension methods and additional functionality for ZombieManager
    /// </summary>
    public partial class ZombieManager
    {
        // Active zombie tracking
        private List<ZombieAI> activeZombieList = new List<ZombieAI>();

        /// <summary>
        /// Gets the count of active zombies
        /// </summary>
        public int GetActiveZombieCount()
        {
            // Clean up null references
            activeZombieList.RemoveAll(z => z == null);
            return activeZombieList.Count;
        }

        /// <summary>
        /// Gets all active zombies
        /// </summary>
        public List<ZombieAI> GetActiveZombies()
        {
            activeZombieList.RemoveAll(z => z == null);
            return new List<ZombieAI>(activeZombieList);
        }

        /// <summary>
        /// Registers a zombie when spawned
        /// </summary>
        public void RegisterZombie(ZombieAI zombie, GameObject zombieObj)
        {
            if (zombie == null || zombieObj == null)
                return;

            if (!activeZombieList.Contains(zombie))
            {
                activeZombieList.Add(zombie);
            }

            // Insert into spatial grid
            spatialGrid?.Insert(zombie);

            // Subscribe to death event
            var health = zombieObj.GetComponent<ZombieHealth>();
            if (health != null)
            {
                health.OnDeath += () => OnZombieDied(zombie);
            }
        }

        /// <summary>
        /// Called when a zombie dies
        /// </summary>
        private void OnZombieDied(ZombieAI zombie)
        {
            if (zombie != null)
            {
                activeZombieList.Remove(zombie);
                spatialGrid?.Remove(zombie);
            }
        }

        /// <summary>
        /// Gets zombies of a specific type
        /// </summary>
        public List<ZombieAI> GetZombiesByType(ZombieType type)
        {
            var result = new List<ZombieAI>();

            foreach (var zombie in activeZombieList)
            {
                if (zombie != null && zombie.Config.zombieType == type)
                {
                    result.Add(zombie);
                }
            }

            return result;
        }

        /// <summary>
        /// Gets closest zombie to a position
        /// </summary>
        public ZombieAI GetClosestZombie(Vector3 position, float maxDistance = float.MaxValue)
        {
            ZombieAI closest = null;
            float closestDist = maxDistance;

            // Use spatial grid if available
            if (spatialGrid != null)
            {
                var nearby = spatialGrid.Query(position, maxDistance);

                foreach (var zombie in nearby)
                {
                    if (zombie == null)
                        continue;

                    float dist = Vector3.Distance(position, zombie.transform.position);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closest = zombie;
                    }
                }
            }
            else
            {
                // Fallback: check all zombies
                foreach (var zombie in activeZombieList)
                {
                    if (zombie == null)
                        continue;

                    float dist = Vector3.Distance(position, zombie.transform.position);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closest = zombie;
                    }
                }
            }

            return closest;
        }

        /// <summary>
        /// Spawns a random zombie type
        /// </summary>
        public ZombieAI SpawnRandomZombie(Vector3 position)
        {
            // Weighted random selection
            ZombieType type = GetRandomZombieType();
            return SpawnZombie(position, type);
        }

        private ZombieType GetRandomZombieType()
        {
            float random = Random.value;

            // Weighted distribution
            if (random < 0.5f)
                return ZombieType.Walker; // 50%
            else if (random < 0.75f)
                return ZombieType.Runner; // 25%
            else if (random < 0.9f)
                return ZombieType.Tank; // 15%
            else if (random < 0.97f)
                return ZombieType.Exploder; // 7%
            else
                return ZombieType.Screamer; // 3%
        }

        /// <summary>
        /// Despawns all zombies
        /// </summary>
        public void DespawnAllZombies()
        {
            var zombiesToDespawn = new List<ZombieAI>(activeZombieList);

            foreach (var zombie in zombiesToDespawn)
            {
                if (zombie != null)
                {
                    Destroy(zombie.gameObject);
                }
            }

            activeZombieList.Clear();

            Debug.Log("[ZombieManager] Despawned all zombies");
        }

        /// <summary>
        /// Gets zombie density at a position
        /// </summary>
        public float GetZombieDensity(Vector3 position, float radius)
        {
            if (spatialGrid == null)
                return 0f;

            var zombiesInRadius = spatialGrid.Query(position, radius);
            float area = Mathf.PI * radius * radius;

            return zombiesInRadius.Count / area;
        }

        /// <summary>
        /// Finds a safe spawn position (away from players)
        /// </summary>
        public Vector3 GetSafeSpawnPosition(float minDistanceFromPlayers = 20f, float maxAttempts = 10)
        {
            var players = FindObjectsOfType<Player.PlayerController>();

            for (int i = 0; i < maxAttempts; i++)
            {
                // Random position in a circle
                Vector2 randomCircle = Random.insideUnitCircle * 100f;
                Vector3 testPos = new Vector3(randomCircle.x, 0f, randomCircle.y);

                // Check distance from all players
                bool isSafe = true;

                foreach (var player in players)
                {
                    if (player == null)
                        continue;

                    float dist = Vector3.Distance(testPos, player.transform.position);
                    if (dist < minDistanceFromPlayers)
                    {
                        isSafe = false;
                        break;
                    }
                }

                if (isSafe)
                {
                    // Sample NavMesh
                    if (UnityEngine.AI.NavMesh.SamplePosition(testPos, out UnityEngine.AI.NavMeshHit hit, 10f, UnityEngine.AI.NavMesh.AllAreas))
                    {
                        return hit.position;
                    }
                }
            }

            // Fallback: return random position
            return Vector3.zero;
        }

        /// <summary>
        /// Gets performance statistics
        /// </summary>
        public ZombiePerformanceStats GetPerformanceStats()
        {
            return new ZombiePerformanceStats
            {
                activeZombies = GetActiveZombieCount(),
                maxZombies = Core.Constants.MAX_ZOMBIES_PER_MATCH,
                spatialGridCells = spatialGrid?.GetCellCount() ?? 0,
                averageDensity = CalculateAverageDensity()
            };
        }

        private float CalculateAverageDensity()
        {
            if (activeZombieList.Count == 0)
                return 0f;

            float totalDensity = 0f;
            int sampleCount = Mathf.Min(10, activeZombieList.Count);

            for (int i = 0; i < sampleCount; i++)
            {
                var zombie = activeZombieList[Random.Range(0, activeZombieList.Count)];
                if (zombie != null)
                {
                    totalDensity += GetZombieDensity(zombie.transform.position, 20f);
                }
            }

            return totalDensity / sampleCount;
        }
    }

    [System.Serializable]
    public struct ZombiePerformanceStats
    {
        public int activeZombies;
        public int maxZombies;
        public int spatialGridCells;
        public float averageDensity;

        public float GetUtilizationPercentage()
        {
            return maxZombies > 0 ? (float)activeZombies / maxZombies * 100f : 0f;
        }
    }
}
