using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Loot
{
    public class LootDropSystem : NetworkBehaviour
    {
        public static LootDropSystem Instance { get; private set; }

        [SerializeField] private List<LootTable> lootTables = new List<LootTable>();
        [SerializeField] private GameObject lootDropPrefab;

        public event Action<ulong, LootDrop> OnLootDropped;
        public event Action<ulong, string, int> OnLootCollected;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        [ServerRpc(RequireOwnership = false)]
        public void DropLootFromEntityServerRpc(ulong entityId, Vector3 position, string lootTableId, ServerRpcParams rpcParams = default)
        {
            var table = lootTables.FirstOrDefault(t => t.tableId == lootTableId);
            if (table == null) return;

            List<LootItem> drops = RollLootTable(table);
            if (drops.Count == 0) return;

            SpawnLootDrop(position, drops);
        }

        private List<LootItem> RollLootTable(LootTable table)
        {
            List<LootItem> result = new List<LootItem>();

            foreach (var entry in table.entries)
            {
                float roll = UnityEngine.Random.Range(0f, 100f);
                if (roll <= entry.dropChance)
                {
                    int quantity = UnityEngine.Random.Range(entry.minQuantity, entry.maxQuantity + 1);
                    result.Add(new LootItem 
                    { 
                        itemId = entry.itemId, 
                        quantity = quantity, 
                        rarity = entry.rarity 
                    });
                }
            }

            return result;
        }

        private void SpawnLootDrop(Vector3 position, List<LootItem> items)
        {
            if (lootDropPrefab == null) return;

            GameObject drop = Instantiate(lootDropPrefab, position, Quaternion.identity);
            NetworkObject netObj = drop.GetComponent<NetworkObject>();
            
            if (netObj != null)
            {
                netObj.Spawn();
                LootDrop lootComponent = drop.GetComponent<LootDrop>();
                if (lootComponent != null)
                {
                    lootComponent.Initialize(items);
                }
            }
        }

        public LootTable GetLootTable(string tableId) => lootTables.FirstOrDefault(t => t.tableId == tableId);
    }

    [Serializable]
    public class LootTable
    {
        public string tableId;
        public string tableName;
        public List<LootTableEntry> entries = new List<LootTableEntry>();
    }

    [Serializable]
    public class LootTableEntry
    {
        public string itemId;
        public float dropChance;
        public int minQuantity;
        public int maxQuantity;
        public LootRarity rarity;
    }

    [Serializable]
    public class LootItem
    {
        public string itemId;
        public int quantity;
        public LootRarity rarity;
    }

    public class LootDrop : NetworkBehaviour
    {
        public List<LootItem> items = new List<LootItem>();
        
        public void Initialize(List<LootItem> dropItems)
        {
            items = dropItems;
        }
    }

    public enum LootRarity { Common, Uncommon, Rare, Epic, Legendary, Mythic }
}
