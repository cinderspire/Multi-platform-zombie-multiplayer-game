using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

namespace ZombieGame
{
    /// <summary>
    /// Procedural Generation System - Infinite random map generation
    /// Features: Perlin noise terrain, random buildings, dynamic spawns
    /// Infinite replayability with seed-based generation
    /// </summary>
    public class ProceduralGenerationSystem : NetworkBehaviour
    {
        public static ProceduralGenerationSystem Instance { get; private set; }

        [Header("Generation Settings")]
        [SerializeField] private bool enableProcGen = true;
        [SerializeField] private int mapSizeX = 1000;
        [SerializeField] private int mapSizeZ = 1000;
        [SerializeField] private float terrainScale = 50f;
        [SerializeField] private int seed = 0;

        [Header("Building Settings")]
        [SerializeField] private int minBuildings = 20;
        [SerializeField] private int maxBuildings = 50;
        [SerializeField] private float buildingSpacing = 25f;

        [Header("Loot Settings")]
        [SerializeField] private int minLootSpawns = 30;
        [SerializeField] private int maxLootSpawns = 100;

        private GeneratedMap currentMap;
        private System.Random random;

        [System.Serializable]
        public class GeneratedMap
        {
            public int seed;
            public float[,] heightMap;
            public List<Building> buildings = new List<Building>();
            public List<Vector3> lootSpawns = new List<Vector3>();
            public List<Vector3> zombieSpawns = new List<Vector3>();
            public List<Vector3> playerSpawns = new List<Vector3>();
            public BiomeType biome;
        }

        [System.Serializable]
        public class Building
        {
            public Vector3 position;
            public Quaternion rotation;
            public BuildingType type;
            public Vector3 size;
        }

        public enum BuildingType
        {
            House,
            Warehouse,
            Office,
            Store,
            Hospital,
            PoliceStation,
            FireStation,
            School
        }

        public enum BiomeType
        {
            Urban,
            Suburban,
            Rural,
            Industrial,
            Military
        }

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

        public GeneratedMap GenerateMap(int? customSeed = null)
        {
            if (!enableProcGen) return null;

            // Set seed
            int useSeed = customSeed ?? UnityEngine.Random.Range(0, int.MaxValue);
            random = new System.Random(useSeed);
            seed = useSeed;

            var map = new GeneratedMap
            {
                seed = useSeed,
                biome = (BiomeType)random.Next(0, System.Enum.GetValues(typeof(BiomeType)).Length)
            };

            // Generate terrain
            map.heightMap = GenerateTerrain(mapSizeX, mapSizeZ);

            // Generate buildings
            int buildingCount = random.Next(minBuildings, maxBuildings);
            for (int i = 0; i < buildingCount; i++)
            {
                map.buildings.Add(GenerateBuilding());
            }

            // Generate loot spawns
            int lootCount = random.Next(minLootSpawns, maxLootSpawns);
            for (int i = 0; i < lootCount; i++)
            {
                map.lootSpawns.Add(GetRandomPosition());
            }

            // Generate zombie spawns
            int zombieSpawnCount = random.Next(10, 30);
            for (int i = 0; i < zombieSpawnCount; i++)
            {
                map.zombieSpawns.Add(GetRandomPosition());
            }

            // Generate player spawns
            for (int i = 0; i < 16; i++) // Support up to 16 players
            {
                map.playerSpawns.Add(GetRandomPosition());
            }

            currentMap = map;

            Debug.Log($"[ProcGen] Generated map with seed {useSeed} | Biome: {map.biome} | Buildings: {buildingCount}");

            return map;
        }

        private float[,] GenerateTerrain(int width, int height)
        {
            float[,] heightMap = new float[width, height];

            // Use Perlin noise for terrain generation
            float offsetX = random.Next(0, 10000);
            float offsetZ = random.Next(0, 10000);

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < height; z++)
                {
                    float xCoord = offsetX + (float)x / width * terrainScale;
                    float zCoord = offsetZ + (float)z / height * terrainScale;

                    heightMap[x, z] = Mathf.PerlinNoise(xCoord, zCoord);
                }
            }

            return heightMap;
        }

        private Building GenerateBuilding()
        {
            var building = new Building
            {
                position = GetRandomPosition(),
                rotation = Quaternion.Euler(0, random.Next(0, 360), 0),
                type = (BuildingType)random.Next(0, System.Enum.GetValues(typeof(BuildingType)).Length)
            };

            // Random building size based on type
            switch (building.type)
            {
                case BuildingType.Warehouse:
                case BuildingType.Hospital:
                    building.size = new Vector3(30, 10, 40);
                    break;
                case BuildingType.Office:
                case BuildingType.School:
                    building.size = new Vector3(25, 15, 25);
                    break;
                default:
                    building.size = new Vector3(10, 8, 12);
                    break;
            }

            return building;
        }

        private Vector3 GetRandomPosition()
        {
            float x = (float)random.NextDouble() * mapSizeX - mapSizeX / 2;
            float z = (float)random.NextDouble() * mapSizeZ - mapSizeZ / 2;

            // Get height from terrain if available
            float y = 0f;
            if (currentMap != null && currentMap.heightMap != null)
            {
                int heightX = Mathf.Clamp((int)(x + mapSizeX / 2), 0, mapSizeX - 1);
                int heightZ = Mathf.Clamp((int)(z + mapSizeZ / 2), 0, mapSizeZ - 1);
                y = currentMap.heightMap[heightX, heightZ] * 50f; // Scale height
            }

            return new Vector3(x, y, z);
        }

        public void InstantiateMap(GeneratedMap map)
        {
            if (!IsServer) return;

            // Instantiate buildings
            foreach (var building in map.buildings)
            {
                // In real implementation, would spawn actual building prefabs
                Debug.Log($"[ProcGen] Spawning {building.type} at {building.position}");
            }

            // Setup loot spawns
            foreach (var lootPos in map.lootSpawns)
            {
                // In real implementation, would spawn loot containers
                Debug.Log($"[ProcGen] Loot spawn at {lootPos}");
            }

            Debug.Log($"[ProcGen] Map instantiated with {map.buildings.Count} buildings and {map.lootSpawns.Count} loot spawns");
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestMapGenerationServerRpc(int requestedSeed, ServerRpcParams rpcParams = default)
        {
            var map = GenerateMap(requestedSeed);
            InstantiateMap(map);
        }

        public GeneratedMap GetCurrentMap()
        {
            return currentMap;
        }

        public int GetCurrentSeed()
        {
            return seed;
        }

        // Save/Load map seeds for favorite maps
        public void SaveMapSeed(int mapSeed, string mapName)
        {
            PlayerPrefs.SetInt($"SavedMap_{mapName}", mapSeed);
            PlayerPrefs.Save();
            Debug.Log($"[ProcGen] Saved map seed {mapSeed} as '{mapName}'");
        }

        public int LoadMapSeed(string mapName)
        {
            return PlayerPrefs.GetInt($"SavedMap_{mapName}", 0);
        }
    }
}
