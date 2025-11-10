using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Loot
{
    public class LootSystem : NetworkBehaviour
    {
        public static LootSystem Instance { get; private set; }

        [Header("Loot Configuration")]
        [SerializeField] private float lootDespawnTime = 300f;
        [SerializeField] private int maxActiveLootBoxes = 500;
        [SerializeField] private float rarityBonusMultiplier = 0.1f;

        private Dictionary<string, LootBox> activeLootBoxes = new Dictionary<string, LootBox>();
        private Dictionary<string, LootTable> lootTables = new Dictionary<string, LootTable>();
        private Dictionary<string, ItemDefinition> itemDatabase = new Dictionary<string, ItemDefinition>();

        public event Action<string, Vector3> OnLootSpawned;
        public event Action<ulong, string, List<LootDrop>> OnLootCollected;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer) { InitializeLootTables(); InitializeItemDatabase(); }
        }

        private void InitializeItemDatabase()
        {
            // Weapons
            itemDatabase["weapon_pistol"] = new ItemDefinition { itemId = "weapon_pistol", itemName = "Pistol", itemType = ItemType.Weapon, rarity = ItemRarity.Common, value = 500 };
            itemDatabase["weapon_rifle"] = new ItemDefinition { itemId = "weapon_rifle", itemName = "Rifle", itemType = ItemType.Weapon, rarity = ItemRarity.Rare, value = 5000 };
            itemDatabase["weapon_shotgun"] = new ItemDefinition { itemId = "weapon_shotgun", itemName = "Shotgun", itemType = ItemType.Weapon, rarity = ItemRarity.Uncommon, value = 2000 };

            // Ammo
            itemDatabase["ammo_9mm"] = new ItemDefinition { itemId = "ammo_9mm", itemName = "9mm Ammo", itemType = ItemType.Ammo, rarity = ItemRarity.Common, value = 10, stackSize = 100 };
            itemDatabase["ammo_556"] = new ItemDefinition { itemId = "ammo_556", itemName = "5.56 Ammo", itemType = ItemType.Ammo, rarity = ItemRarity.Uncommon, value = 20, stackSize = 100 };

            // Medical
            itemDatabase["medkit"] = new ItemDefinition { itemId = "medkit", itemName = "Medkit", itemType = ItemType.Medical, rarity = ItemRarity.Uncommon, value = 200, healAmount = 100f };
            itemDatabase["bandage"] = new ItemDefinition { itemId = "bandage", itemName = "Bandage", itemType = ItemType.Medical, rarity = ItemRarity.Common, value = 50, healAmount = 25f };

            // Resources
            itemDatabase["wood"] = new ItemDefinition { itemId = "wood", itemName = "Wood", itemType = ItemType.Resource, rarity = ItemRarity.Common, value = 5, stackSize = 250 };
            itemDatabase["metal"] = new ItemDefinition { itemId = "metal", itemName = "Metal", itemType = ItemType.Resource, rarity = ItemRarity.Uncommon, value = 15, stackSize = 100 };
            itemDatabase["electronics"] = new ItemDefinition { itemId = "electronics", itemName = "Electronics", itemType = ItemType.Resource, rarity = ItemRarity.Rare, value = 50, stackSize = 50 };

            // Food
            itemDatabase["canned_food"] = new ItemDefinition { itemId = "canned_food", itemName = "Canned Food", itemType = ItemType.Food, rarity = ItemRarity.Common, value = 20, foodValue = 50f };
            itemDatabase["water_bottle"] = new ItemDefinition { itemId = "water_bottle", itemName = "Water Bottle", itemType = ItemType.Food, rarity = ItemRarity.Common, value = 15, foodValue = 30f };

            // Armor
            itemDatabase["armor_vest"] = new ItemDefinition { itemId = "armor_vest", itemName = "Kevlar Vest", itemType = ItemType.Armor, rarity = ItemRarity.Rare, value = 3000, armorValue = 50f };
            itemDatabase["armor_helmet"] = new ItemDefinition { itemId = "armor_helmet", itemName = "Tactical Helmet", itemType = ItemType.Armor, rarity = ItemRarity.Uncommon, value = 1500, armorValue = 25f };
        }

        private void InitializeLootTables()
        {
            // Zombie loot table
            lootTables["zombie_common"] = new LootTable
            {
                tableId = "zombie_common",
                tableName = "Common Zombie Loot",
                entries = new List<LootEntry>
                {
                    new LootEntry { itemId = "ammo_9mm", dropChance = 0.3f, minQuantity = 5, maxQuantity = 15 },
                    new LootEntry { itemId = "bandage", dropChance = 0.2f, minQuantity = 1, maxQuantity = 3 },
                    new LootEntry { itemId = "wood", dropChance = 0.15f, minQuantity = 1, maxQuantity = 5 },
                    new LootEntry { itemId = "canned_food", dropChance = 0.1f, minQuantity = 1, maxQuantity = 2 }
                }
            };

            lootTables["zombie_special"] = new LootTable
            {
                tableId = "zombie_special",
                tableName = "Special Zombie Loot",
                entries = new List<LootEntry>
                {
                    new LootEntry { itemId = "weapon_pistol", dropChance = 0.15f, minQuantity = 1, maxQuantity = 1 },
                    new LootEntry { itemId = "ammo_556", dropChance = 0.25f, minQuantity = 10, maxQuantity = 30 },
                    new LootEntry { itemId = "medkit", dropChance = 0.2f, minQuantity = 1, maxQuantity = 2 },
                    new LootEntry { itemId = "metal", dropChance = 0.3f, minQuantity = 3, maxQuantity = 10 }
                }
            };

            // Container loot tables
            lootTables["container_military"] = new LootTable
            {
                tableId = "container_military",
                tableName = "Military Container",
                entries = new List<LootEntry>
                {
                    new LootEntry { itemId = "weapon_rifle", dropChance = 0.4f, minQuantity = 1, maxQuantity = 1 },
                    new LootEntry { itemId = "ammo_556", dropChance = 0.8f, minQuantity = 30, maxQuantity = 90 },
                    new LootEntry { itemId = "armor_vest", dropChance = 0.3f, minQuantity = 1, maxQuantity = 1 },
                    new LootEntry { itemId = "medkit", dropChance = 0.5f, minQuantity = 2, maxQuantity = 5 }
                }
            };

            lootTables["container_residential"] = new LootTable
            {
                tableId = "container_residential",
                tableName = "Residential Container",
                entries = new List<LootEntry>
                {
                    new LootEntry { itemId = "canned_food", dropChance = 0.6f, minQuantity = 2, maxQuantity = 8 },
                    new LootEntry { itemId = "water_bottle", dropChance = 0.5f, minQuantity = 1, maxQuantity = 5 },
                    new LootEntry { itemId = "bandage", dropChance = 0.4f, minQuantity = 1, maxQuantity = 4 },
                    new LootEntry { itemId = "wood", dropChance = 0.3f, minQuantity = 5, maxQuantity = 20 }
                }
            };

            lootTables["container_industrial"] = new LootTable
            {
                tableId = "container_industrial",
                tableName = "Industrial Container",
                entries = new List<LootEntry>
                {
                    new LootEntry { itemId = "metal", dropChance = 0.7f, minQuantity = 10, maxQuantity = 40 },
                    new LootEntry { itemId = "electronics", dropChance = 0.4f, minQuantity = 2, maxQuantity = 10 },
                    new LootEntry { itemId = "wood", dropChance = 0.5f, minQuantity = 10, maxQuantity = 30 }
                }
            };

            // Supply drop loot
            lootTables["supply_drop"] = new LootTable
            {
                tableId = "supply_drop",
                tableName = "Supply Drop",
                entries = new List<LootEntry>
                {
                    new LootEntry { itemId = "weapon_rifle", dropChance = 0.6f, minQuantity = 1, maxQuantity = 1 },
                    new LootEntry { itemId = "armor_vest", dropChance = 0.5f, minQuantity = 1, maxQuantity = 1 },
                    new LootEntry { itemId = "medkit", dropChance = 0.8f, minQuantity = 3, maxQuantity = 8 },
                    new LootEntry { itemId = "ammo_556", dropChance = 0.9f, minQuantity = 60, maxQuantity = 150 }
                }
            };
        }

        [ServerRpc(RequireOwnership = false)]
        public void SpawnLootBoxServerRpc(string lootTableId, Vector3 position, ServerRpcParams rpcParams = default)
        {
            if (activeLootBoxes.Count >= maxActiveLootBoxes) return;
            if (!lootTables.TryGetValue(lootTableId, out var table)) return;

            var lootBox = new LootBox
            {
                lootBoxId = $"loot_{Guid.NewGuid()}",
                lootTable = table,
                position = position,
                spawnTime = DateTime.UtcNow,
                contents = GenerateLoot(table)
            };

            activeLootBoxes[lootBox.lootBoxId] = lootBox;
            OnLootSpawned?.Invoke(lootBox.lootBoxId, position);
        }

        private List<LootDrop> GenerateLoot(LootTable table)
        {
            var drops = new List<LootDrop>();

            foreach (var entry in table.entries)
            {
                if (UnityEngine.Random.value <= entry.dropChance)
                {
                    if (itemDatabase.TryGetValue(entry.itemId, out var item))
                    {
                        int quantity = UnityEngine.Random.Range(entry.minQuantity, entry.maxQuantity + 1);
                        drops.Add(new LootDrop
                        {
                            itemDefinition = item,
                            quantity = quantity
                        });
                    }
                }
            }

            return drops;
        }

        [ServerRpc(RequireOwnership = false)]
        public void CollectLootServerRpc(ulong playerId, string lootBoxId, ServerRpcParams rpcParams = default)
        {
            if (!activeLootBoxes.TryGetValue(lootBoxId, out var lootBox)) return;

            OnLootCollected?.Invoke(playerId, lootBoxId, lootBox.contents);
            activeLootBoxes.Remove(lootBoxId);
        }

        public LootBox GetLootBox(string lootBoxId) => activeLootBoxes.GetValueOrDefault(lootBoxId);
    }

    [Serializable]
    public class LootBox
    {
        public string lootBoxId;
        public LootTable lootTable;
        public Vector3 position;
        public DateTime spawnTime;
        public List<LootDrop> contents;
    }

    [Serializable]
    public class LootTable
    {
        public string tableId;
        public string tableName;
        public List<LootEntry> entries;
    }

    [Serializable]
    public class LootEntry
    {
        public string itemId;
        public float dropChance;
        public int minQuantity;
        public int maxQuantity;
    }

    [Serializable]
    public class LootDrop
    {
        public ItemDefinition itemDefinition;
        public int quantity;
    }

    [Serializable]
    public class ItemDefinition
    {
        public string itemId;
        public string itemName;
        public ItemType itemType;
        public ItemRarity rarity;
        public int value;
        public int stackSize = 1;
        public float healAmount;
        public float foodValue;
        public float armorValue;
    }

    public enum ItemType { Weapon, Ammo, Medical, Resource, Food, Armor, Utility, Quest }
    public enum ItemRarity { Common, Uncommon, Rare, Epic, Legendary, Mythic }
}
