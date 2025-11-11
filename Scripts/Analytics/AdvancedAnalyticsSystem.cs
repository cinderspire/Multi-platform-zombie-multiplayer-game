using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Analytics
{
    /// <summary>
    /// Advanced analytics system tracking player behavior, generating heatmaps,
    /// kill zones, movement patterns, and performance metrics for game balance.
    /// </summary>
    public class AdvancedAnalyticsSystem : NetworkBehaviour
    {
        public static AdvancedAnalyticsSystem Instance { get; private set; }

        [Header("Analytics Configuration")]
        [SerializeField] private bool enableHeatmaps = true;
        [SerializeField] private float heatmapResolution = 2f; // meters per cell
        [SerializeField] private int maxHeatmapSize = 500; // 500x500 grid
        [SerializeField] private float dataCollectionInterval = 1f;

        private Dictionary<string, HeatmapData> heatmaps = new Dictionary<string, HeatmapData>();
        private Dictionary<ulong, PlayerBehaviorData> playerBehaviorData = new Dictionary<ulong, PlayerBehaviorData>();
        private List<KillEvent> killEvents = new List<KillEvent>();
        private float lastCollectionTime;

        public event Action<HeatmapData> OnHeatmapUpdated;
        public event Action<AnalyticsReport> OnReportGenerated;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            InitializeHeatmaps();
        }

        private void InitializeHeatmaps()
        {
            // Create heatmaps for different event types
            heatmaps["player_deaths"] = new HeatmapData { heatmapId = "player_deaths", eventType = "Deaths" };
            heatmaps["zombie_deaths"] = new HeatmapData { heatmapId = "zombie_deaths", eventType = "Zombie Kills" };
            heatmaps["player_movement"] = new HeatmapData { heatmapId = "player_movement", eventType = "Movement" };
            heatmaps["weapon_usage"] = new HeatmapData { heatmapId = "weapon_usage", eventType = "Combat" };
            heatmaps["ability_usage"] = new HeatmapData { heatmapId = "ability_usage", eventType = "Abilities" };
        }

        private void Update()
        {
            if (IsServer && Time.time - lastCollectionTime > dataCollectionInterval)
            {
                CollectAnalyticsData();
                lastCollectionTime = Time.time;
            }
        }

        private void CollectAnalyticsData()
        {
            foreach (var client in NetworkManager.Singleton.ConnectedClients)
            {
                ulong playerId = client.Key;
                CollectPlayerData(playerId);
            }
        }

        private void CollectPlayerData(ulong playerId)
        {
            if (!playerBehaviorData.ContainsKey(playerId))
            {
                playerBehaviorData[playerId] = new PlayerBehaviorData { playerId = playerId };
            }

            var data = playerBehaviorData[playerId];

            // Get player position (would need actual player reference)
            Vector3 playerPosition = GetPlayerPosition(playerId);

            // Record movement
            if (enableHeatmaps)
            {
                RecordHeatmapPoint("player_movement", playerPosition, 1f);
            }

            data.totalPlayTime += dataCollectionInterval;
        }

        /// <summary>
        /// Record a kill event for analytics
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void RecordKillEventServerRpc(ulong killerId, ulong victimId, Vector3 killerPosition,
            Vector3 victimPosition, string weaponUsed, bool wasHeadshot, float distance, ServerRpcParams rpcParams = default)
        {
            KillEvent killEvent = new KillEvent
            {
                timestamp = DateTime.UtcNow,
                killerId = killerId,
                victimId = victimId,
                killerPosition = killerPosition,
                victimPosition = victimPosition,
                weaponUsed = weaponUsed,
                wasHeadshot = wasHeadshot,
                distance = distance
            };

            killEvents.Add(killEvent);

            // Update heatmaps
            RecordHeatmapPoint("player_deaths", victimPosition, 1f);

            // Update player behavior data
            if (playerBehaviorData.ContainsKey(killerId))
            {
                playerBehaviorData[killerId].totalKills++;
                if (wasHeadshot) playerBehaviorData[killerId].headshotCount++;
            }
        }

        /// <summary>
        /// Record weapon usage
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void RecordWeaponUsageServerRpc(ulong playerId, string weaponId, Vector3 position,
            int shotsFired, int shotsHit, ServerRpcParams rpcParams = default)
        {
            if (!playerBehaviorData.ContainsKey(playerId)) return;

            var data = playerBehaviorData[playerId];

            if (!data.weaponUsage.ContainsKey(weaponId))
            {
                data.weaponUsage[weaponId] = new WeaponUsageStats();
            }

            var weaponStats = data.weaponUsage[weaponId];
            weaponStats.shotsFired += shotsFired;
            weaponStats.shotsHit += shotsHit;
            weaponStats.timesUsed++;

            // Record on heatmap
            RecordHeatmapPoint("weapon_usage", position, shotsFired);
        }

        /// <summary>
        /// Record ability usage
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void RecordAbilityUsageServerRpc(ulong playerId, string abilityId, Vector3 position, ServerRpcParams rpcParams = default)
        {
            if (!playerBehaviorData.ContainsKey(playerId)) return;

            var data = playerBehaviorData[playerId];

            if (!data.abilityUsage.ContainsKey(abilityId))
            {
                data.abilityUsage[abilityId] = 0;
            }

            data.abilityUsage[abilityId]++;

            // Record on heatmap
            RecordHeatmapPoint("ability_usage", position, 1f);
        }

        /// <summary>
        /// Record damage event
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void RecordDamageEventServerRpc(ulong attackerId, ulong victimId, float damage,
            string damageSource, ServerRpcParams rpcParams = default)
        {
            if (playerBehaviorData.ContainsKey(attackerId))
            {
                playerBehaviorData[attackerId].totalDamageDealt += damage;
            }

            if (playerBehaviorData.ContainsKey(victimId))
            {
                playerBehaviorData[victimId].totalDamageTaken += damage;
            }
        }

        private void RecordHeatmapPoint(string heatmapId, Vector3 worldPosition, float intensity)
        {
            if (!heatmaps.TryGetValue(heatmapId, out var heatmap)) return;

            // Convert world position to grid coordinates
            int gridX = Mathf.FloorToInt(worldPosition.x / heatmapResolution);
            int gridZ = Mathf.FloorToInt(worldPosition.z / heatmapResolution);

            // Clamp to grid size
            gridX = Mathf.Clamp(gridX, 0, maxHeatmapSize - 1);
            gridZ = Mathf.Clamp(gridZ, 0, maxHeatmapSize - 1);

            string cellKey = $"{gridX},{gridZ}";

            if (!heatmap.cells.ContainsKey(cellKey))
            {
                heatmap.cells[cellKey] = new HeatmapCell
                {
                    gridX = gridX,
                    gridZ = gridZ,
                    intensity = 0f
                };
            }

            heatmap.cells[cellKey].intensity += intensity;
            heatmap.totalEvents++;
        }

        /// <summary>
        /// Generate comprehensive analytics report
        /// </summary>
        public AnalyticsReport GenerateReport()
        {
            AnalyticsReport report = new AnalyticsReport
            {
                generatedAt = DateTime.UtcNow,
                totalPlayers = playerBehaviorData.Count,
                totalKills = killEvents.Count
            };

            // Calculate averages
            if (playerBehaviorData.Count > 0)
            {
                report.averagePlayTime = playerBehaviorData.Values.Average(p => p.totalPlayTime);
                report.averageKills = playerBehaviorData.Values.Average(p => p.totalKills);
                report.averageDeaths = playerBehaviorData.Values.Average(p => p.totalDeaths);
            }

            // Weapon popularity
            Dictionary<string, int> weaponUsage = new Dictionary<string, int>();
            foreach (var playerData in playerBehaviorData.Values)
            {
                foreach (var kvp in playerData.weaponUsage)
                {
                    if (!weaponUsage.ContainsKey(kvp.Key))
                    {
                        weaponUsage[kvp.Key] = 0;
                    }
                    weaponUsage[kvp.Key] += kvp.Value.timesUsed;
                }
            }

            report.mostUsedWeapon = weaponUsage.OrderByDescending(w => w.Value).FirstOrDefault().Key;

            // Kill zones (areas with most kills)
            report.topKillZones = GetTopKillZones(5);

            // Average engagement distance
            if (killEvents.Count > 0)
            {
                report.averageEngagementDistance = killEvents.Average(k => k.distance);
            }

            // Headshot rate
            int totalKillsWithHeadshot = killEvents.Count(k => k.wasHeadshot);
            report.globalHeadshotRate = killEvents.Count > 0 ?
                (totalKillsWithHeadshot / (float)killEvents.Count) * 100f : 0f;

            OnReportGenerated?.Invoke(report);

            return report;
        }

        private List<KillZone> GetTopKillZones(int count)
        {
            if (!heatmaps.TryGetValue("player_deaths", out var deathHeatmap)) return new List<KillZone>();

            var topCells = deathHeatmap.cells.Values
                .OrderByDescending(c => c.intensity)
                .Take(count);

            List<KillZone> zones = new List<KillZone>();
            foreach (var cell in topCells)
            {
                zones.Add(new KillZone
                {
                    position = new Vector3(cell.gridX * heatmapResolution, 0, cell.gridZ * heatmapResolution),
                    killCount = (int)cell.intensity
                });
            }

            return zones;
        }

        /// <summary>
        /// Get player behavior summary
        /// </summary>
        public PlayerBehaviorData GetPlayerBehavior(ulong playerId)
        {
            return playerBehaviorData.TryGetValue(playerId, out var data) ? data : null;
        }

        /// <summary>
        /// Get heatmap for specific event type
        /// </summary>
        public HeatmapData GetHeatmap(string heatmapId)
        {
            return heatmaps.TryGetValue(heatmapId, out var heatmap) ? heatmap : null;
        }

        /// <summary>
        /// Export analytics data for external analysis
        /// </summary>
        public string ExportAnalyticsJSON()
        {
            var exportData = new
            {
                timestamp = DateTime.UtcNow,
                players = playerBehaviorData.Values,
                kills = killEvents,
                heatmaps = heatmaps.Values
            };

            return JsonUtility.ToJson(exportData, true);
        }

        private Vector3 GetPlayerPosition(ulong playerId)
        {
            // Would get actual player position from player manager
            return Vector3.zero;
        }

        [Serializable]
        public class HeatmapData
        {
            public string heatmapId;
            public string eventType;
            public Dictionary<string, HeatmapCell> cells = new Dictionary<string, HeatmapCell>();
            public int totalEvents;
        }

        [Serializable]
        public class HeatmapCell
        {
            public int gridX;
            public int gridZ;
            public float intensity;
        }

        [Serializable]
        public class PlayerBehaviorData
        {
            public ulong playerId;
            public float totalPlayTime;
            public int totalKills;
            public int totalDeaths;
            public int headshotCount;
            public float totalDamageDealt;
            public float totalDamageTaken;
            public Dictionary<string, WeaponUsageStats> weaponUsage = new Dictionary<string, WeaponUsageStats>();
            public Dictionary<string, int> abilityUsage = new Dictionary<string, int>();
            public List<string> favoriteWeapons = new List<string>();
        }

        [Serializable]
        public class WeaponUsageStats
        {
            public int shotsFired;
            public int shotsHit;
            public int timesUsed;
            public float accuracy => shotsFired > 0 ? (shotsHit / (float)shotsFired) * 100f : 0f;
        }

        [Serializable]
        public class KillEvent
        {
            public DateTime timestamp;
            public ulong killerId;
            public ulong victimId;
            public Vector3 killerPosition;
            public Vector3 victimPosition;
            public string weaponUsed;
            public bool wasHeadshot;
            public float distance;
        }

        [Serializable]
        public class AnalyticsReport
        {
            public DateTime generatedAt;
            public int totalPlayers;
            public int totalKills;
            public float averagePlayTime;
            public float averageKills;
            public float averageDeaths;
            public string mostUsedWeapon;
            public List<KillZone> topKillZones;
            public float averageEngagementDistance;
            public float globalHeadshotRate;
        }

        [Serializable]
        public class KillZone
        {
            public Vector3 position;
            public int killCount;
        }
    }
}
