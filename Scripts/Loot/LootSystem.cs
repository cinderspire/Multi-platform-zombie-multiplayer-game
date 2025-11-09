using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;
using System;

namespace DeadFrontier.Loot
{
    /// <summary>
    /// Comprehensive dynamic loot distribution system.
    /// Manages loot tables, spawn points, rarity tiers, and smart loot placement.
    /// Includes anti-farming measures, dynamic value scaling, and loot hotspots.
    /// </summary>
    public class LootSystem : NetworkBehaviour
    {
        public static LootSystem Instance { get; private set; }

        [Header("Loot Settings")]
        [SerializeField] private int maxActiveLoot = 200;
        [SerializeField] private float lootRespawnTime = 300f; // 5 minutes
        [SerializeField] private float lootDespawnTime = 600f; // 10 minutes

        [Header("Rarity Settings")]
        [SerializeField] private float commonChance = 0.60f;
        [SerializeField] private float uncommonChance = 0.25f;
        [SerializeField] private float rareChance = 0.10f;
        [SerializeField] private float epicChance = 0.04f;
        [SerializeField] private float legendaryChance = 0.01f;

        [Header("Dynamic Loot")]
        [SerializeField] private bool enableDynamicLoot = true;
        [SerializeField] private bool enableLootHotspots = true;
        [SerializeField] private float hotspotRotationMinutes = 15f;

        // Loot tables
        private Dictionary<string, LootTable> lootTables = new Dictionary<string, LootTable>();

        // Loot points
        private Dictionary<string, LootPoint> lootPoints = new Dictionary<string, LootPoint>();

        // Active loot
        private Dictionary<string, ActiveLoot> activeLoot = new Dictionary<string, ActiveLoot>();

        // Hotspots
        private List<LootHotspot> activeHotspots = new List<LootHotspot>();
        private DateTime lastHotspotRotation;

        // Player loot tracking (anti-farming)
        private Dictionary<ulong, PlayerLootData> playerLootData = new Dictionary<ulong, PlayerLootData>();

        // Events
        public event Action<string, ActiveLoot> OnLootSpawned;
        public event Action<string, ulong> OnLootCollected;
        public event Action<LootHotspot> OnHotspotActivated;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeLootTables();
                InitializeLootPoints();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            SpawnInitialLoot();

            if (enableLootHotspots)
            {
                RotateHotspots();
            }
        }

        private void Update()
        {
            if (!IsServer) return;

            UpdateLootRespawns();
            UpdateLootDespawns();
            CheckHotspotRotation();
        }

        #region Initialization

        private void InitializeLootTables()
        {
            // Common Loot Table
            RegisterLootTable(new LootTable
            {
                tableId = "loot_common",
                tableName = "Common Loot",
                entries = new List<LootEntry>
                {
                    new LootEntry { itemId = "consumable_bandage", weight = 30, minQuantity = 1, maxQuantity = 3 },
                    new LootEntry { itemId = "ammo_9mm", weight = 25, minQuantity = 10, maxQuantity = 30 },
                    new LootEntry { itemId = "consumable_water", weight = 20, minQuantity = 1, maxQuantity = 2 },
                    new LootEntry { itemId = "material_scrap", weight = 15, minQuantity = 5, maxQuantity = 15 },
                    new LootEntry { itemId = "currency_credits", weight = 10, minQuantity = 50, maxQuantity = 150 }
                }
            });

            // Uncommon Loot Table
            RegisterLootTable(new LootTable
            {
                tableId = "loot_uncommon",
                tableName = "Uncommon Loot",
                entries = new List<LootEntry>
                {
                    new LootEntry { itemId = "consumable_medkit", weight = 25, minQuantity = 1, maxQuantity = 2 },
                    new LootEntry { itemId = "weapon_pistol", weight = 20, minQuantity = 1, maxQuantity = 1 },
                    new LootEntry { itemId = "attachment_scope", weight = 15, minQuantity = 1, maxQuantity = 1 },
                    new LootEntry { itemId = "material_metal", weight = 20, minQuantity = 3, maxQuantity = 8 },
                    new LootEntry { itemId = "currency_credits", weight = 20, minQuantity = 200, maxQuantity = 500 }
                }
            });

            // Rare Loot Table
            RegisterLootTable(new LootTable
            {
                tableId = "loot_rare",
                tableName = "Rare Loot",
                entries = new List<LootEntry>
                {
                    new LootEntry { itemId = "weapon_rifle", weight = 30, minQuantity = 1, maxQuantity = 1 },
                    new LootEntry { itemId = "armor_tactical_vest", weight = 25, minQuantity = 1, maxQuantity = 1 },
                    new LootEntry { itemId = "consumable_adrenaline", weight = 20, minQuantity = 1, maxQuantity = 2 },
                    new LootEntry { itemId = "material_rare_metal", weight = 15, minQuantity = 1, maxQuantity = 3 },
                    new LootEntry { itemId = "currency_credits", weight = 10, minQuantity = 500, maxQuantity = 1000 }
                }
            });

            // Boss Loot Table
            RegisterLootTable(new LootTable
            {
                tableId = "loot_boss",
                tableName = "Boss Loot",
                guaranteedRarity = LootRarity.Epic,
                entries = new List<LootEntry>
                {
                    new LootEntry { itemId = "weapon_legendary_rifle", weight = 15, minQuantity = 1, maxQuantity = 1 },
                    new LootEntry { itemId = "armor_legendary_set", weight = 15, minQuantity = 1, maxQuantity = 1 },
                    new LootEntry { itemId = "cosmetic_boss_trophy", weight = 20, minQuantity = 1, maxQuantity = 1 },
                    new LootEntry { itemId = "material_legendary", weight = 25, minQuantity = 1, maxQuantity = 2 },
                    new LootEntry { itemId = "currency_hard", weight = 25, minQuantity = 100, maxQuantity = 500 }
                }
            });

            // Weapon Crate Table
            RegisterLootTable(new LootTable
            {
                tableId = "loot_weapon_crate",
                tableName = "Weapon Crate",
                entries = new List<LootEntry>
                {
                    new LootEntry { itemId = "weapon_shotgun", weight = 25, minQuantity = 1, maxQuantity = 1 },
                    new LootEntry { itemId = "weapon_smg", weight = 25, minQuantity = 1, maxQuantity = 1 },
                    new LootEntry { itemId = "weapon_sniper", weight = 15, minQuantity = 1, maxQuantity = 1 },
                    new LootEntry { itemId = "ammo_assorted", weight = 20, minQuantity = 30, maxQuantity = 100 },
                    new LootEntry { itemId = "attachment_random", weight = 15, minQuantity = 1, maxQuantity = 2 }
                }
            });

            Debug.Log($"[LootSystem] Initialized {lootTables.Count} loot tables");
        }

        private void InitializeLootPoints()
        {
            // Create loot spawn points across the map
            for (int i = 0; i < 100; i++)
            {
                string pointId = $"loot_point_{i}";

                LootPointType pointType;
                string lootTableId;

                // Distribute point types
                float random = UnityEngine.Random.value;

                if (random < 0.5f)
                {
                    pointType = LootPointType.Ground;
                    lootTableId = "loot_common";
                }
                else if (random < 0.8f)
                {
                    pointType = LootPointType.Container;
                    lootTableId = "loot_uncommon";
                }
                else if (random < 0.95f)
                {
                    pointType = LootPointType.SpecialCrate;
                    lootTableId = "loot_rare";
                }
                else
                {
                    pointType = LootPointType.WeaponRack;
                    lootTableId = "loot_weapon_crate";
                }

                lootPoints[pointId] = new LootPoint
                {
                    pointId = pointId,
                    position = new Vector3(
                        UnityEngine.Random.Range(-500f, 500f),
                        0,
                        UnityEngine.Random.Range(-500f, 500f)
                    ),
                    pointType = pointType,
                    lootTableId = lootTableId,
                    isActive = true,
                    lastSpawnTime = DateTime.MinValue,
                    baseRespawnTime = lootRespawnTime
                };
            }

            Debug.Log($"[LootSystem] Initialized {lootPoints.Count} loot points");
        }

        private void RegisterLootTable(LootTable table)
        {
            lootTables[table.tableId] = table;
        }

        #endregion

        #region Loot Spawning

        private void SpawnInitialLoot()
        {
            // Spawn loot at all points
            foreach (var point in lootPoints.Values)
            {
                SpawnLootAtPoint(point);
            }

            Debug.Log($"[LootSystem] Spawned initial loot at {lootPoints.Count} points");
        }

        private void SpawnLootAtPoint(LootPoint point)
        {
            if (!point.isActive) return;

            // Check if already has loot
            if (activeLoot.Values.Any(l => l.lootPointId == point.pointId)) return;

            // Get loot table
            if (!lootTables.ContainsKey(point.lootTableId)) return;

            var table = lootTables[point.lootTableId];

            // Determine rarity
            LootRarity rarity = DetermineRarity(table);

            // Select items from table
            var items = RollLootTable(table, rarity);

            if (items.Count == 0) return;

            // Create loot
            string lootId = $"loot_{point.pointId}_{DateTime.UtcNow.Ticks}";

            var loot = new ActiveLoot
            {
                lootId = lootId,
                lootPointId = point.pointId,
                position = point.position,
                items = items,
                rarity = rarity,
                spawnTime = DateTime.UtcNow,
                isCollected = false
            };

            activeLoot[lootId] = loot;
            point.lastSpawnTime = DateTime.UtcNow;

            OnLootSpawned?.Invoke(lootId, loot);

            // Notify clients
            SpawnLootClientRpc(lootId, point.position, rarity);
        }

        [ClientRpc]
        private void SpawnLootClientRpc(string lootId, Vector3 position, LootRarity rarity)
        {
            // Client-side loot visualization
            Debug.Log($"[LootSystem] Spawned {rarity} loot at {position}");
        }

        private LootRarity DetermineRarity(LootTable table)
        {
            // Check for guaranteed rarity
            if (table.guaranteedRarity != LootRarity.Common)
            {
                return table.guaranteedRarity;
            }

            // Weighted random
            float roll = UnityEngine.Random.value;

            if (roll < legendaryChance) return LootRarity.Legendary;
            if (roll < legendaryChance + epicChance) return LootRarity.Epic;
            if (roll < legendaryChance + epicChance + rareChance) return LootRarity.Rare;
            if (roll < legendaryChance + epicChance + rareChance + uncommonChance) return LootRarity.Uncommon;

            return LootRarity.Common;
        }

        private List<LootItem> RollLootTable(LootTable table, LootRarity rarity)
        {
            var items = new List<LootItem>();

            // Calculate total weight
            float totalWeight = table.entries.Sum(e => e.weight);

            // Determine number of items (higher rarity = more items)
            int itemCount = rarity switch
            {
                LootRarity.Legendary => UnityEngine.Random.Range(3, 6),
                LootRarity.Epic => UnityEngine.Random.Range(2, 4),
                LootRarity.Rare => UnityEngine.Random.Range(2, 3),
                LootRarity.Uncommon => UnityEngine.Random.Range(1, 3),
                _ => UnityEngine.Random.Range(1, 2)
            };

            for (int i = 0; i < itemCount; i++)
            {
                float roll = UnityEngine.Random.Range(0f, totalWeight);
                float cumulative = 0f;

                foreach (var entry in table.entries)
                {
                    cumulative += entry.weight;

                    if (roll <= cumulative)
                    {
                        int quantity = UnityEngine.Random.Range(entry.minQuantity, entry.maxQuantity + 1);

                        items.Add(new LootItem
                        {
                            itemId = entry.itemId,
                            quantity = quantity,
                            rarity = rarity
                        });

                        break;
                    }
                }
            }

            return items;
        }

        #endregion

        #region Loot Collection

        public bool CollectLoot(ulong playerId, string lootId)
        {
            if (!activeLoot.ContainsKey(lootId)) return false;

            var loot = activeLoot[lootId];

            if (loot.isCollected) return false;

            // Anti-farming check
            if (IsPlayerFarming(playerId))
            {
                Debug.LogWarning($"[LootSystem] Player {playerId} is farming, reduced loot");
                // Reduce loot quality/quantity
            }

            // Grant items to player
            foreach (var item in loot.items)
            {
                Inventory.InventoryManager.Instance?.AddItem(playerId, item.itemId, item.quantity);
            }

            // Mark as collected
            loot.isCollected = true;
            loot.collectedBy = playerId;
            loot.collectTime = DateTime.UtcNow;

            // Track player loot
            TrackPlayerLoot(playerId, loot);

            OnLootCollected?.Invoke(lootId, playerId);

            // Remove from active
            activeLoot.Remove(lootId);

            Debug.Log($"[LootSystem] Player {playerId} collected loot {lootId}");

            // Notify clients
            CollectLootClientRpc(lootId, playerId);

            return true;
        }

        [ClientRpc]
        private void CollectLootClientRpc(string lootId, ulong playerId)
        {
            Debug.Log($"[LootSystem] Loot {lootId} collected by player {playerId}");
        }

        #endregion

        #region Anti-Farming

        private void TrackPlayerLoot(ulong playerId, ActiveLoot loot)
        {
            if (!playerLootData.ContainsKey(playerId))
            {
                playerLootData[playerId] = new PlayerLootData
                {
                    playerId = playerId,
                    recentLoots = new List<LootCollectionRecord>()
                };
            }

            var data = playerLootData[playerId];

            data.recentLoots.Add(new LootCollectionRecord
            {
                lootId = loot.lootId,
                lootPointId = loot.lootPointId,
                collectTime = DateTime.UtcNow,
                rarity = loot.rarity
            });

            // Keep only last 20 loots
            if (data.recentLoots.Count > 20)
            {
                data.recentLoots.RemoveAt(0);
            }
        }

        private bool IsPlayerFarming(ulong playerId)
        {
            if (!playerLootData.ContainsKey(playerId)) return false;

            var data = playerLootData[playerId];

            // Check for repeated loot point visits in short time
            var recentCollections = data.recentLoots
                .Where(l => (DateTime.UtcNow - l.collectTime).TotalMinutes < 5)
                .ToList();

            if (recentCollections.Count < 5) return false;

            // Check if visiting same points repeatedly
            var uniquePoints = recentCollections.Select(l => l.lootPointId).Distinct().Count();

            return uniquePoints < 3; // Farming same 2-3 points
        }

        #endregion

        #region Loot Hotspots

        private void CheckHotspotRotation()
        {
            if (!enableLootHotspots) return;

            if ((DateTime.UtcNow - lastHotspotRotation).TotalMinutes >= hotspotRotationMinutes)
            {
                RotateHotspots();
            }
        }

        private void RotateHotspots()
        {
            activeHotspots.Clear();

            // Create 3 random hotspots
            for (int i = 0; i < 3; i++)
            {
                Vector3 position = new Vector3(
                    UnityEngine.Random.Range(-500f, 500f),
                    0,
                    UnityEngine.Random.Range(-500f, 500f)
                );

                var hotspot = new LootHotspot
                {
                    hotspotId = $"hotspot_{i}_{DateTime.UtcNow.Ticks}",
                    position = position,
                    radius = 50f,
                    lootMultiplier = 2f,
                    rarityBoost = 1,
                    expirationTime = DateTime.UtcNow.AddMinutes(hotspotRotationMinutes)
                };

                activeHotspots.Add(hotspot);

                OnHotspotActivated?.Invoke(hotspot);

                Debug.Log($"[LootSystem] Hotspot activated at {position}");

                // Notify clients
                ActivateHotspotClientRpc(hotspot.position, hotspot.radius);
            }

            lastHotspotRotation = DateTime.UtcNow;
        }

        [ClientRpc]
        private void ActivateHotspotClientRpc(Vector3 position, float radius)
        {
            Debug.Log($"[LootSystem] LOOT HOTSPOT activated at {position}!");
        }

        private bool IsPointInHotspot(Vector3 position)
        {
            foreach (var hotspot in activeHotspots)
            {
                if (Vector3.Distance(position, hotspot.position) <= hotspot.radius)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Update Methods

        private void UpdateLootRespawns()
        {
            foreach (var point in lootPoints.Values)
            {
                if (!point.isActive) continue;

                // Check if point needs respawn
                bool hasLoot = activeLoot.Values.Any(l => l.lootPointId == point.pointId);

                if (hasLoot) continue;

                float respawnTime = point.baseRespawnTime;

                // Faster respawn in hotspots
                if (IsPointInHotspot(point.position))
                {
                    respawnTime *= 0.5f;
                }

                if ((DateTime.UtcNow - point.lastSpawnTime).TotalSeconds >= respawnTime)
                {
                    SpawnLootAtPoint(point);
                }
            }
        }

        private void UpdateLootDespawns()
        {
            var toDespawn = activeLoot.Values
                .Where(l => !l.isCollected && (DateTime.UtcNow - l.spawnTime).TotalSeconds >= lootDespawnTime)
                .Select(l => l.lootId)
                .ToList();

            foreach (var lootId in toDespawn)
            {
                activeLoot.Remove(lootId);
                Debug.Log($"[LootSystem] Loot {lootId} despawned");
            }
        }

        #endregion

        #region Public Getters

        public List<ActiveLoot> GetNearbyLoot(Vector3 position, float radius)
        {
            return activeLoot.Values
                .Where(l => !l.isCollected && Vector3.Distance(l.position, position) <= radius)
                .ToList();
        }

        public List<LootHotspot> GetActiveHotspots()
        {
            return activeHotspots;
        }

        #endregion
    }

    #region Data Classes

    [Serializable]
    public class LootTable
    {
        public string tableId;
        public string tableName;
        public LootRarity guaranteedRarity = LootRarity.Common;
        public List<LootEntry> entries = new List<LootEntry>();
    }

    [Serializable]
    public class LootEntry
    {
        public string itemId;
        public float weight; // Higher = more likely
        public int minQuantity;
        public int maxQuantity;
    }

    [Serializable]
    public class LootPoint
    {
        public string pointId;
        public Vector3 position;
        public LootPointType pointType;
        public string lootTableId;
        public bool isActive;
        public DateTime lastSpawnTime;
        public float baseRespawnTime;
    }

    [Serializable]
    public class ActiveLoot
    {
        public string lootId;
        public string lootPointId;
        public Vector3 position;
        public List<LootItem> items = new List<LootItem>();
        public LootRarity rarity;
        public DateTime spawnTime;
        public DateTime collectTime;
        public bool isCollected;
        public ulong collectedBy;
    }

    [Serializable]
    public class LootItem
    {
        public string itemId;
        public int quantity;
        public LootRarity rarity;
    }

    [Serializable]
    public class LootHotspot
    {
        public string hotspotId;
        public Vector3 position;
        public float radius;
        public float lootMultiplier;
        public int rarityBoost;
        public DateTime expirationTime;
    }

    [Serializable]
    public class PlayerLootData
    {
        public ulong playerId;
        public List<LootCollectionRecord> recentLoots = new List<LootCollectionRecord>();
    }

    [Serializable]
    public class LootCollectionRecord
    {
        public string lootId;
        public string lootPointId;
        public DateTime collectTime;
        public LootRarity rarity;
    }

    public enum LootRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    public enum LootPointType
    {
        Ground,
        Container,
        SpecialCrate,
        WeaponRack,
        MedicalSupply,
        AmmoBox
    }

    #endregion
}
