using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ZombieGame
{
    /// <summary>
    /// User-Generated Content System - Full map editor and custom mode creator
    /// Features: In-game map editor, custom game modes, Steam Workshop integration
    /// Share creations with community, rating system, curated content
    /// </summary>
    public class UserGeneratedContentSystem : NetworkBehaviour
    {
        public static UserGeneratedContentSystem Instance { get; private set; }

        [Header("UGC Settings")]
        [SerializeField] private bool enableUGC = true;
        [SerializeField] private int maxCustomMapsPerUser = 50;
        [SerializeField] private int maxMapSize = 2000; // meters

        // Published Content
        private Dictionary<string, UGCMap> publishedMaps = new Dictionary<string, UGCMap>();
        private Dictionary<string, UGCGameMode> publishedModes = new Dictionary<string, UGCGameMode>();

        // Player Content
        private Dictionary<ulong, List<string>> playerContent = new Dictionary<ulong, List<string>>();

        // Workshop Integration
        private Dictionary<string, WorkshopItem> workshopItems = new Dictionary<string, WorkshopItem>();

        // Events
        public event System.Action<string> OnContentPublished;
        public event System.Action<string, int> OnContentRated;
        public event System.Action<string> OnContentFeatured;

        [System.Serializable]
        public class UGCMap
        {
            public string mapId;
            public string mapName;
            public string description;
            public ulong creatorId;
            public string creatorName;
            public System.DateTime publishDate;
            public MapData mapData;
            public MapMetadata metadata;
            public int downloads = 0;
            public float averageRating = 0f;
            public int totalRatings = 0;
            public List<string> tags = new List<string>();
            public bool isFeatured = false;
            public bool isCurated = false;
            public string thumbnailPath = "";
        }

        [System.Serializable]
        public class MapData
        {
            public Vector3 mapSize;
            public List<PlacedObject> objects = new List<PlacedObject>();
            public List<SpawnPoint> playerSpawns = new List<SpawnPoint>();
            public List<SpawnPoint> zombieSpawns = new List<SpawnPoint>();
            public TerrainData terrainData;
            public LightingSettings lighting;
            public WeatherSettings weather;
        }

        [System.Serializable]
        public class PlacedObject
        {
            public string objectId;
            public string prefabName;
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 scale;
            public Dictionary<string, string> properties = new Dictionary<string, string>();
        }

        [System.Serializable]
        public class SpawnPoint
        {
            public Vector3 position;
            public Quaternion rotation;
            public SpawnPointType type;
        }

        public enum SpawnPointType
        {
            Player,
            Zombie,
            Boss,
            Vehicle,
            Loot
        }

        [System.Serializable]
        public class TerrainData
        {
            public float[,] heightMap;
            public string[] textureNames;
        }

        [System.Serializable]
        public class LightingSettings
        {
            public Color ambientColor;
            public float lightIntensity;
            public Vector3 sunDirection;
        }

        [System.Serializable]
        public class WeatherSettings
        {
            public string weatherType;
            public float intensity;
        }

        [System.Serializable]
        public class MapMetadata
        {
            public int recommendedPlayers;
            public string gameMode;
            public string difficulty;
            public int estimatedPlayTime; // minutes
            public List<string> supportedModes = new List<string>();
        }

        [System.Serializable]
        public class UGCGameMode
        {
            public string modeId;
            public string modeName;
            public string description;
            public ulong creatorId;
            public GameModeRules rules;
            public int timesPlayed = 0;
            public float averageRating = 0f;
        }

        [System.Serializable]
        public class GameModeRules
        {
            public int maxPlayers;
            public int teamSize;
            public float matchDuration;
            public Dictionary<string, float> gameplayModifiers = new Dictionary<string, float>();
            public List<string> objectiveTypes = new List<string>();
            public WinCondition winCondition;
        }

        public enum WinCondition
        {
            LastTeamStanding,
            MostKills,
            ObjectiveComplete,
            TimeLimit,
            Custom
        }

        [System.Serializable]
        public class WorkshopItem
        {
            public string workshopId;
            public string contentId; // mapId or modeId
            public ContentType contentType;
            public long steamWorkshopId;
            public int subscriberCount = 0;
        }

        public enum ContentType
        {
            Map,
            GameMode,
            Skin,
            Mod
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                LoadPublishedContent();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void LoadPublishedContent()
        {
            // Load from local storage or server
            string contentPath = Path.Combine(Application.persistentDataPath, "UGC");
            if (!Directory.Exists(contentPath))
            {
                Directory.CreateDirectory(contentPath);
            }

            Debug.Log("[UGC] User-Generated Content system initialized");
        }

        // Map Editor API

        public MapData CreateNewMap(string mapName, Vector3 size)
        {
            return new MapData
            {
                mapSize = size,
                objects = new List<PlacedObject>(),
                playerSpawns = new List<SpawnPoint>(),
                zombieSpawns = new List<SpawnPoint>(),
                lighting = new LightingSettings
                {
                    ambientColor = Color.white,
                    lightIntensity = 1.0f,
                    sunDirection = new Vector3(-45f, 50f, 0f)
                },
                weather = new WeatherSettings
                {
                    weatherType = "Clear",
                    intensity = 0.5f
                }
            };
        }

        public void AddObject(MapData map, string prefabName, Vector3 position, Quaternion rotation)
        {
            string objectId = System.Guid.NewGuid().ToString();

            map.objects.Add(new PlacedObject
            {
                objectId = objectId,
                prefabName = prefabName,
                position = position,
                rotation = rotation,
                scale = Vector3.one
            });
        }

        public void AddSpawnPoint(MapData map, SpawnPointType type, Vector3 position)
        {
            map.playerSpawns.Add(new SpawnPoint
            {
                position = position,
                rotation = Quaternion.identity,
                type = type
            });
        }

        public bool ValidateMap(MapData map)
        {
            // Validation rules
            if (map.playerSpawns.Count < 4)
            {
                Debug.LogWarning("[UGC] Map needs at least 4 player spawn points");
                return false;
            }

            if (map.zombieSpawns.Count < 1)
            {
                Debug.LogWarning("[UGC] Map needs at least 1 zombie spawn point");
                return false;
            }

            if (map.objects.Count > 10000)
            {
                Debug.LogWarning("[UGC] Map has too many objects (max 10,000)");
                return false;
            }

            return true;
        }

        [ServerRpc(RequireOwnership = false)]
        public void PublishMapServerRpc(ulong creatorId, string mapName, string description,
            MapData mapData, MapMetadata metadata, ServerRpcParams rpcParams = default)
        {
            if (!enableUGC) return;

            // Validate
            if (!ValidateMap(mapData))
            {
                return;
            }

            // Check user limit
            if (playerContent.ContainsKey(creatorId) &&
                playerContent[creatorId].Count >= maxCustomMapsPerUser)
            {
                Debug.LogWarning($"[UGC] User {creatorId} has reached map limit");
                return;
            }

            string mapId = System.Guid.NewGuid().ToString();

            var ugcMap = new UGCMap
            {
                mapId = mapId,
                mapName = mapName,
                description = description,
                creatorId = creatorId,
                publishDate = System.DateTime.UtcNow,
                mapData = mapData,
                metadata = metadata
            };

            publishedMaps[mapId] = ugcMap;

            // Track player content
            if (!playerContent.ContainsKey(creatorId))
            {
                playerContent[creatorId] = new List<string>();
            }
            playerContent[creatorId].Add(mapId);

            // Save to disk
            SaveMap(ugcMap);

            OnContentPublished?.Invoke(mapId);

            Debug.Log($"[UGC] Map published: {mapName} by user {creatorId}");
        }

        private void SaveMap(UGCMap map)
        {
            string mapPath = Path.Combine(Application.persistentDataPath, "UGC", "Maps", $"{map.mapId}.json");
            Directory.CreateDirectory(Path.GetDirectoryName(mapPath));

            string json = JsonUtility.ToJson(map, true);
            File.WriteAllText(mapPath, json);
        }

        [ServerRpc(RequireOwnership = false)]
        public void RateContentServerRpc(string contentId, ulong userId, int rating, ServerRpcParams rpcParams = default)
        {
            if (!publishedMaps.ContainsKey(contentId)) return;

            var map = publishedMaps[contentId];

            // Update rating
            float totalScore = map.averageRating * map.totalRatings;
            map.totalRatings++;
            map.averageRating = (totalScore + rating) / map.totalRatings;

            OnContentRated?.Invoke(contentId, rating);

            Debug.Log($"[UGC] Content {contentId} rated: {rating}/5 (avg: {map.averageRating:F2})");
        }

        [ServerRpc(RequireOwnership = false)]
        public void DownloadMapServerRpc(string mapId, ulong userId, ServerRpcParams rpcParams = default)
        {
            if (!publishedMaps.ContainsKey(mapId)) return;

            var map = publishedMaps[mapId];
            map.downloads++;

            Debug.Log($"[UGC] Map {mapId} downloaded by user {userId}");
        }

        public void FeatureContent(string contentId)
        {
            if (!publishedMaps.ContainsKey(contentId)) return;

            var map = publishedMaps[contentId];
            map.isFeatured = true;

            OnContentFeatured?.Invoke(contentId);

            Debug.Log($"[UGC] Content featured: {map.mapName}");
        }

        // Steam Workshop Integration

        public void PublishToWorkshop(string contentId)
        {
            if (!publishedMaps.ContainsKey(contentId)) return;

            var map = publishedMaps[contentId];

            // Integrate with Steam Workshop API
            // This would use Steamworks.NET in real implementation

            var workshopItem = new WorkshopItem
            {
                workshopId = System.Guid.NewGuid().ToString(),
                contentId = contentId,
                contentType = ContentType.Map,
                steamWorkshopId = 0 // Would be real Steam ID
            };

            workshopItems[workshopItem.workshopId] = workshopItem;

            Debug.Log($"[UGC] Published to Steam Workshop: {map.mapName}");
        }

        // Getters

        public List<UGCMap> GetPublishedMaps()
        {
            return publishedMaps.Values.ToList();
        }

        public List<UGCMap> GetTopRatedMaps(int count)
        {
            return publishedMaps.Values
                .OrderByDescending(m => m.averageRating)
                .Take(count)
                .ToList();
        }

        public List<UGCMap> GetMostDownloadedMaps(int count)
        {
            return publishedMaps.Values
                .OrderByDescending(m => m.downloads)
                .Take(count)
                .ToList();
        }

        public List<UGCMap> GetFeaturedMaps()
        {
            return publishedMaps.Values.Where(m => m.isFeatured).ToList();
        }

        public List<UGCMap> GetUserMaps(ulong userId)
        {
            if (!playerContent.ContainsKey(userId)) return new List<UGCMap>();

            return playerContent[userId]
                .Where(id => publishedMaps.ContainsKey(id))
                .Select(id => publishedMaps[id])
                .ToList();
        }

        public UGCMap GetMap(string mapId)
        {
            return publishedMaps.ContainsKey(mapId) ? publishedMaps[mapId] : null;
        }
    }
}
