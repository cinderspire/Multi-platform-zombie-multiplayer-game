using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace DeadFrontier.NPCs
{
    /// <summary>
    /// NPC trader and vendor system for buying/selling items, weapons, and upgrades.
    /// Supports dynamic pricing, reputation, and special vendor inventories.
    /// </summary>
    public class NPCTraderSystem : MonoBehaviour
    {
        public static NPCTraderSystem Instance { get; private set; }

        [Header("Trader Settings")]
        [SerializeField] private float priceMarkupMultiplier = 1.5f; // Buy from trader costs 150%
        [SerializeField] private float sellPriceMultiplier = 0.6f; // Sell to trader gets 60%
        [SerializeField] private int defaultTraderInventorySize = 20;
        [SerializeField] private float inventoryRefreshInterval = 3600f; // 1 hour

        [Header("Trader Database")]
        [SerializeField] private TraderData[] traders;

        // Active traders
        private Dictionary<string, TraderState> traderStates = new Dictionary<string, TraderState>();

        // Player reputation
        private Dictionary<string, int> playerReputation = new Dictionary<string, int>();

        // Events
        public event Action<string, Gameplay.LootItemData, int> OnItemPurchased;
        public event Action<string, Gameplay.LootItemData, int> OnItemSold;
        public event Action<string, int> OnReputationChanged;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            InitializeTraders();
            LoadPlayerReputation();
        }

        private void Update()
        {
            UpdateTraderInventories();
        }

        #region Initialization

        private void InitializeTraders()
        {
            foreach (var trader in traders)
            {
                var state = new TraderState
                {
                    traderId = trader.traderId,
                    inventory = new List<TraderItem>(),
                    lastInventoryRefresh = Time.time
                };

                // Generate initial inventory
                RefreshTraderInventory(trader, state);

                traderStates[trader.traderId] = state;

                // Initialize reputation
                if (!playerReputation.ContainsKey(trader.traderId))
                {
                    playerReputation[trader.traderId] = 0;
                }
            }

            Debug.Log($"[NPCTraderSystem] Initialized {traders.Length} traders");
        }

        #endregion

        #region Trading

        public bool CanPurchaseItem(string traderId, string itemId, int quantity, ulong playerId)
        {
            if (!traderStates.ContainsKey(traderId)) return false;

            var state = traderStates[traderId];
            var item = state.inventory.FirstOrDefault(i => i.itemData.itemId == itemId);

            if (item == null || item.stock < quantity) return false;

            // Check if player has enough currency
            int totalCost = CalculatePurchaseCost(traderId, item.itemData, quantity);

            if (Economy.EconomyManager.Instance != null)
            {
                return Economy.EconomyManager.Instance.CanAfford(totalCost);
            }

            return false;
        }

        public bool PurchaseItem(string traderId, string itemId, int quantity, ulong playerId)
        {
            if (!CanPurchaseItem(traderId, itemId, quantity, playerId)) return false;

            var state = traderStates[traderId];
            var item = state.inventory.FirstOrDefault(i => i.itemData.itemId == itemId);

            if (item == null) return false;

            // Calculate cost
            int totalCost = CalculatePurchaseCost(traderId, item.itemData, quantity);

            // Charge player
            if (Economy.EconomyManager.Instance != null)
            {
                if (!Economy.EconomyManager.Instance.SpendSoftCurrency(totalCost, $"Purchase from trader: {traderId}"))
                    return false;
            }

            // Add item to player inventory
            if (Gameplay.InventoryManager.Instance != null)
            {
                Gameplay.InventoryManager.Instance.AddItem(playerId, item.itemData, quantity);
            }

            // Reduce trader stock
            item.stock -= quantity;
            if (item.stock <= 0)
            {
                state.inventory.Remove(item);
            }

            // Increase reputation
            ModifyReputation(traderId, quantity);

            OnItemPurchased?.Invoke(traderId, item.itemData, quantity);

            Debug.Log($"[NPCTraderSystem] Player purchased {quantity}x {item.itemData.itemName} from {traderId} for {totalCost}");

            return true;
        }

        public bool SellItem(string traderId, string itemId, int quantity, ulong playerId)
        {
            if (!traderStates.ContainsKey(traderId)) return false;

            // Check if player has the item
            if (Gameplay.InventoryManager.Instance == null) return false;

            var playerInventory = Gameplay.InventoryManager.Instance.GetInventoryItems(playerId);
            var playerItem = playerInventory.FirstOrDefault(i => i.itemData.itemId == itemId);

            if (playerItem == null || playerItem.quantity < quantity) return false;

            // Calculate sell price
            int sellPrice = CalculateSellPrice(traderId, playerItem.itemData, quantity);

            // Remove item from player inventory
            if (!Gameplay.InventoryManager.Instance.RemoveItem(playerId, itemId, quantity))
                return false;

            // Give player currency
            if (Economy.EconomyManager.Instance != null)
            {
                Economy.EconomyManager.Instance.EarnSoftCurrency(sellPrice, $"Sold to trader: {traderId}");
            }

            // Add item to trader inventory (if they buy it)
            var trader = GetTraderData(traderId);
            if (trader != null && trader.buysItems)
            {
                var state = traderStates[traderId];
                var existingItem = state.inventory.FirstOrDefault(i => i.itemData.itemId == itemId);

                if (existingItem != null)
                {
                    existingItem.stock += quantity;
                }
                else
                {
                    state.inventory.Add(new TraderItem
                    {
                        itemData = playerItem.itemData,
                        stock = quantity,
                        basePrice = playerItem.itemData.baseValue
                    });
                }
            }

            // Increase reputation
            ModifyReputation(traderId, quantity / 2); // Less reputation for selling

            OnItemSold?.Invoke(traderId, playerItem.itemData, quantity);

            Debug.Log($"[NPCTraderSystem] Player sold {quantity}x {playerItem.itemData.itemName} to {traderId} for {sellPrice}");

            return true;
        }

        private int CalculatePurchaseCost(string traderId, Gameplay.LootItemData itemData, int quantity)
        {
            float basePrice = itemData.baseValue;

            // Apply markup
            basePrice *= priceMarkupMultiplier;

            // Apply reputation discount
            int reputation = GetReputation(traderId);
            float reputationDiscount = Mathf.Clamp01(reputation / 1000f) * 0.2f; // Up to 20% discount at max rep
            basePrice *= (1f - reputationDiscount);

            return Mathf.RoundToInt(basePrice * quantity);
        }

        private int CalculateSellPrice(string traderId, Gameplay.LootItemData itemData, int quantity)
        {
            float basePrice = itemData.baseValue;

            // Apply sell multiplier
            basePrice *= sellPriceMultiplier;

            // Apply reputation bonus
            int reputation = GetReputation(traderId);
            float reputationBonus = Mathf.Clamp01(reputation / 1000f) * 0.1f; // Up to 10% bonus at max rep
            basePrice *= (1f + reputationBonus);

            return Mathf.RoundToInt(basePrice * quantity);
        }

        #endregion

        #region Inventory Management

        private void UpdateTraderInventories()
        {
            foreach (var kvp in traderStates)
            {
                var state = kvp.Value;
                float timeSinceRefresh = Time.time - state.lastInventoryRefresh;

                if (timeSinceRefresh >= inventoryRefreshInterval)
                {
                    var trader = GetTraderData(kvp.Key);
                    if (trader != null)
                    {
                        RefreshTraderInventory(trader, state);
                        state.lastInventoryRefresh = Time.time;
                    }
                }
            }
        }

        private void RefreshTraderInventory(TraderData trader, TraderState state)
        {
            state.inventory.Clear();

            if (trader.sellsItems == null || trader.sellsItems.Length == 0) return;

            // Generate random inventory based on trader's selling pool
            int inventorySize = defaultTraderInventorySize;

            for (int i = 0; i < inventorySize; i++)
            {
                if (trader.sellsItems.Length == 0) break;

                // Select random item type
                var itemType = trader.sellsItems[UnityEngine.Random.Range(0, trader.sellsItems.Length)];

                // Create trader item (would normally pull from item database)
                var itemData = CreateItemDataFromType(itemType);
                if (itemData != null)
                {
                    var traderItem = new TraderItem
                    {
                        itemData = itemData,
                        stock = UnityEngine.Random.Range(1, 10),
                        basePrice = itemData.baseValue
                    };

                    state.inventory.Add(traderItem);
                }
            }

            Debug.Log($"[NPCTraderSystem] Refreshed inventory for trader {trader.traderId}: {state.inventory.Count} items");
        }

        private Gameplay.LootItemData CreateItemDataFromType(Gameplay.ItemCategory category)
        {
            // In production, would pull from item database based on category
            // For now, create placeholder
            return new Gameplay.LootItemData
            {
                itemId = $"item_{category}_{UnityEngine.Random.Range(0, 1000)}",
                itemName = $"{category} Item",
                category = category,
                baseValue = UnityEngine.Random.Range(50, 500),
                weight = UnityEngine.Random.Range(0.5f, 5f)
            };
        }

        #endregion

        #region Reputation

        public int GetReputation(string traderId)
        {
            return playerReputation.ContainsKey(traderId) ? playerReputation[traderId] : 0;
        }

        public void ModifyReputation(string traderId, int amount)
        {
            if (!playerReputation.ContainsKey(traderId))
            {
                playerReputation[traderId] = 0;
            }

            int oldRep = playerReputation[traderId];
            playerReputation[traderId] = Mathf.Clamp(playerReputation[traderId] + amount, -1000, 1000);

            if (oldRep != playerReputation[traderId])
            {
                OnReputationChanged?.Invoke(traderId, playerReputation[traderId]);
                SavePlayerReputation();
            }
        }

        public ReputationTier GetReputationTier(string traderId)
        {
            int rep = GetReputation(traderId);

            if (rep >= 500) return ReputationTier.Honored;
            if (rep >= 250) return ReputationTier.Friendly;
            if (rep >= 0) return ReputationTier.Neutral;
            if (rep >= -250) return ReputationTier.Unfriendly;
            return ReputationTier.Hostile;
        }

        #endregion

        #region Persistence

        private void LoadPlayerReputation()
        {
            if (Core.SaveSystem.Instance == null) return;

            string savedData = Core.SaveSystem.Instance.LoadData("trader_reputation");
            if (!string.IsNullOrEmpty(savedData))
            {
                var repData = JsonUtility.FromJson<ReputationSaveData>(savedData);

                foreach (var entry in repData.reputation)
                {
                    playerReputation[entry.traderId] = entry.reputation;
                }
            }
        }

        private void SavePlayerReputation()
        {
            if (Core.SaveSystem.Instance == null) return;

            var repData = new ReputationSaveData
            {
                reputation = playerReputation.Select(kvp => new ReputationEntry
                {
                    traderId = kvp.Key,
                    reputation = kvp.Value
                }).ToList()
            };

            string json = JsonUtility.ToJson(repData);
            Core.SaveSystem.Instance.SaveData("trader_reputation", json);
        }

        #endregion

        #region Public Getters

        public List<TraderItem> GetTraderInventory(string traderId)
        {
            if (!traderStates.ContainsKey(traderId)) return new List<TraderItem>();

            return new List<TraderItem>(traderStates[traderId].inventory);
        }

        public TraderData GetTraderData(string traderId)
        {
            return traders.FirstOrDefault(t => t.traderId == traderId);
        }

        public List<TraderData> GetAllTraders() => new List<TraderData>(traders);

        #endregion
    }

    #region Data Classes

    [System.Serializable]
    public class TraderData
    {
        public string traderId;
        public string traderName;
        [TextArea(2, 3)]
        public string description;

        public TraderType traderType;
        public Gameplay.ItemCategory[] sellsItems; // Categories of items they sell
        public bool buysItems = true;

        public int requiredReputation = 0; // Minimum reputation to trade

        public Sprite traderIcon;
        public GameObject traderPrefab;
    }

    public class TraderState
    {
        public string traderId;
        public List<TraderItem> inventory;
        public float lastInventoryRefresh;
    }

    [System.Serializable]
    public class TraderItem
    {
        public Gameplay.LootItemData itemData;
        public int stock;
        public int basePrice;
    }

    [System.Serializable]
    public class ReputationSaveData
    {
        public List<ReputationEntry> reputation;
    }

    [System.Serializable]
    public class ReputationEntry
    {
        public string traderId;
        public int reputation;
    }

    public enum TraderType
    {
        GeneralMerchant,
        Weaponsmith,
        Armorsmith,
        Medic,
        Blackmarket,
        QuestGiver
    }

    public enum ReputationTier
    {
        Hostile,
        Unfriendly,
        Neutral,
        Friendly,
        Honored
    }

    #endregion
}
