using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace DeadFrontier.Core.Economy
{
    /// <summary>
    /// Manages in-game economy with currency, purchases, and rewards
    /// Handles both soft currency (earned) and hard currency (premium)
    /// </summary>
    public class EconomyManager : Singleton<EconomyManager>
    {
        [Header("Currency Settings")]
        [SerializeField] private int startingSoftCurrency = 1000;
        [SerializeField] private int startingHardCurrency = 0;

        [Header("Shop Items")]
        [SerializeField] private List<ShopItemData> shopItems = new List<ShopItemData>();

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        // Currency
        private int softCurrency; // Coins/Credits earned through gameplay
        private int hardCurrency; // Premium currency (real money)

        // Owned items
        private HashSet<string> ownedItems = new HashSet<string>();

        // Events
        public event System.Action<int, int> OnCurrencyChanged; // (soft, hard)
        public event System.Action<ShopItemData> OnItemPurchased;
        public event System.Action<string> OnPurchaseFailed;

        protected override void Awake()
        {
            base.Awake();
            LoadEconomy();
        }

        #region Currency Management

        /// <summary>
        /// Adds soft currency (coins/credits)
        /// </summary>
        public void AddSoftCurrency(int amount, string reason = "")
        {
            if (amount <= 0)
                return;

            softCurrency += amount;

            if (showDebugLogs)
                Debug.Log($"[EconomyManager] Added {amount} soft currency. Reason: {reason}. Total: {softCurrency}");

            OnCurrencyChanged?.Invoke(softCurrency, hardCurrency);

            // Track analytics
            Analytics.AnalyticsManager.Instance?.TrackEvent("currency_earned", new Dictionary<string, object>
            {
                { "currency_type", "soft" },
                { "amount", amount },
                { "reason", reason },
                { "balance", softCurrency }
            });

            SaveEconomy();
        }

        /// <summary>
        /// Adds hard currency (premium)
        /// </summary>
        public void AddHardCurrency(int amount, string reason = "")
        {
            if (amount <= 0)
                return;

            hardCurrency += amount;

            if (showDebugLogs)
                Debug.Log($"[EconomyManager] Added {amount} hard currency. Reason: {reason}. Total: {hardCurrency}");

            OnCurrencyChanged?.Invoke(softCurrency, hardCurrency);

            // Track analytics
            Analytics.AnalyticsManager.Instance?.TrackEvent("currency_earned", new Dictionary<string, object>
            {
                { "currency_type", "hard" },
                { "amount", amount },
                { "reason", reason },
                { "balance", hardCurrency }
            });

            SaveEconomy();
        }

        /// <summary>
        /// Removes soft currency
        /// </summary>
        public bool SpendSoftCurrency(int amount, string reason = "")
        {
            if (amount <= 0 || softCurrency < amount)
                return false;

            softCurrency -= amount;

            if (showDebugLogs)
                Debug.Log($"[EconomyManager] Spent {amount} soft currency. Reason: {reason}. Remaining: {softCurrency}");

            OnCurrencyChanged?.Invoke(softCurrency, hardCurrency);

            // Track analytics
            Analytics.AnalyticsManager.Instance?.TrackEvent("currency_spent", new Dictionary<string, object>
            {
                { "currency_type", "soft" },
                { "amount", amount },
                { "reason", reason },
                { "balance", softCurrency }
            });

            SaveEconomy();
            return true;
        }

        /// <summary>
        /// Removes hard currency
        /// </summary>
        public bool SpendHardCurrency(int amount, string reason = "")
        {
            if (amount <= 0 || hardCurrency < amount)
                return false;

            hardCurrency -= amount;

            if (showDebugLogs)
                Debug.Log($"[EconomyManager] Spent {amount} hard currency. Reason: {reason}. Remaining: {hardCurrency}");

            OnCurrencyChanged?.Invoke(softCurrency, hardCurrency);

            // Track analytics
            Analytics.AnalyticsManager.Instance?.TrackEvent("currency_spent", new Dictionary<string, object>
            {
                { "currency_type", "hard" },
                { "amount", amount },
                { "reason", reason },
                { "balance", hardCurrency }
            });

            SaveEconomy();
            return true;
        }

        /// <summary>
        /// Checks if player can afford an item
        /// </summary>
        public bool CanAfford(CurrencyType currencyType, int price)
        {
            if (currencyType == CurrencyType.Soft)
                return softCurrency >= price;
            else
                return hardCurrency >= price;
        }

        #endregion

        #region Shop System

        /// <summary>
        /// Attempts to purchase an item
        /// </summary>
        public bool PurchaseItem(string itemId)
        {
            var item = shopItems.FirstOrDefault(i => i.itemId == itemId);
            if (item == null)
            {
                if (showDebugLogs)
                    Debug.LogError($"[EconomyManager] Item not found: {itemId}");
                OnPurchaseFailed?.Invoke("Item not found");
                return false;
            }

            return PurchaseItem(item);
        }

        /// <summary>
        /// Attempts to purchase an item
        /// </summary>
        public bool PurchaseItem(ShopItemData item)
        {
            // Check if already owned
            if (item.isPermanent && ownedItems.Contains(item.itemId))
            {
                if (showDebugLogs)
                    Debug.LogWarning($"[EconomyManager] Already own item: {item.itemName}");
                OnPurchaseFailed?.Invoke("Already owned");
                return false;
            }

            // Check level requirement
            var progression = GameObject.FindObjectOfType<Player.PlayerProgression>();
            if (progression != null && progression.Level < item.requiredLevel)
            {
                if (showDebugLogs)
                    Debug.LogWarning($"[EconomyManager] Level requirement not met for {item.itemName}. Required: {item.requiredLevel}");
                OnPurchaseFailed?.Invoke($"Level {item.requiredLevel} required");
                return false;
            }

            // Check if can afford
            if (!CanAfford(item.currencyType, item.price))
            {
                if (showDebugLogs)
                    Debug.LogWarning($"[EconomyManager] Cannot afford {item.itemName}");
                OnPurchaseFailed?.Invoke("Insufficient funds");
                return false;
            }

            // Process purchase
            if (item.currencyType == CurrencyType.Soft)
            {
                SpendSoftCurrency(item.price, $"Purchased {item.itemName}");
            }
            else
            {
                SpendHardCurrency(item.price, $"Purchased {item.itemName}");
            }

            // Grant item
            if (item.isPermanent)
            {
                ownedItems.Add(item.itemId);
            }

            ApplyItemEffects(item);

            if (showDebugLogs)
                Debug.Log($"[EconomyManager] Purchased {item.itemName}");

            OnItemPurchased?.Invoke(item);

            // Track analytics
            Analytics.AnalyticsManager.Instance?.TrackEvent("item_purchased", new Dictionary<string, object>
            {
                { "item_id", item.itemId },
                { "item_name", item.itemName },
                { "item_category", item.category.ToString() },
                { "price", item.price },
                { "currency_type", item.currencyType.ToString() }
            });

            SaveEconomy();
            return true;
        }

        private void ApplyItemEffects(ShopItemData item)
        {
            switch (item.category)
            {
                case ShopCategory.Weapon:
                    // Unlock weapon in loadout system
                    break;

                case ShopCategory.Perk:
                    // Unlock perk
                    break;

                case ShopCategory.Cosmetic:
                    // Unlock cosmetic
                    break;

                case ShopCategory.Consumable:
                    // Add to inventory
                    if (item.itemId == "health_pack")
                    {
                        // Add health pack
                    }
                    else if (item.itemId == "ammo_pack")
                    {
                        // Add ammo
                    }
                    break;

                case ShopCategory.Currency:
                    // Already handled in purchase
                    break;

                case ShopCategory.BattlePass:
                    // Unlock premium battle pass
                    if (Progression.BattlePass.BattlePassManager.Instance != null)
                    {
                        Progression.BattlePass.BattlePassManager.Instance.UnlockPremiumPass();
                    }
                    break;
            }
        }

        /// <summary>
        /// Checks if player owns an item
        /// </summary>
        public bool OwnsItem(string itemId)
        {
            return ownedItems.Contains(itemId);
        }

        /// <summary>
        /// Gets all shop items
        /// </summary>
        public List<ShopItemData> GetShopItems()
        {
            return new List<ShopItemData>(shopItems);
        }

        /// <summary>
        /// Gets shop items by category
        /// </summary>
        public List<ShopItemData> GetShopItems(ShopCategory category)
        {
            return shopItems.Where(i => i.category == category).ToList();
        }

        #endregion

        #region Rewards

        /// <summary>
        /// Grants match rewards based on performance
        /// </summary>
        public void GrantMatchRewards(MatchResult result)
        {
            int baseReward = 100;
            int bonus = 0;

            // Win bonus
            if (result.won)
                bonus += 50;

            // Kill bonus
            bonus += result.kills * 10;

            // Extraction bonus
            if (result.extracted)
                bonus += 100;

            // Survival time bonus
            bonus += (int)(result.survivalTime / 60f) * 5; // 5 credits per minute

            int totalReward = baseReward + bonus;

            AddSoftCurrency(totalReward, "Match reward");

            if (showDebugLogs)
                Debug.Log($"[EconomyManager] Match rewards: {totalReward} (Base: {baseReward}, Bonus: {bonus})");
        }

        #endregion

        #region Save/Load

        private void LoadEconomy()
        {
            if (Save.SaveSystem.Instance != null && Save.SaveSystem.Instance.CurrentSave != null)
            {
                var save = Save.SaveSystem.Instance.CurrentSave;

                softCurrency = save.inventory.currency;
                hardCurrency = save.inventory.premiumCurrency;

                // Load owned items
                ownedItems.Clear();
                foreach (var item in save.inventory.items)
                {
                    ownedItems.Add(item.itemID);
                }

                if (showDebugLogs)
                    Debug.Log($"[EconomyManager] Loaded economy. Soft: {softCurrency}, Hard: {hardCurrency}, Items: {ownedItems.Count}");
            }
            else
            {
                // First time - give starting currency
                softCurrency = startingSoftCurrency;
                hardCurrency = startingHardCurrency;

                if (showDebugLogs)
                    Debug.Log($"[EconomyManager] New economy. Soft: {softCurrency}, Hard: {hardCurrency}");
            }

            OnCurrencyChanged?.Invoke(softCurrency, hardCurrency);
        }

        private void SaveEconomy()
        {
            if (Save.SaveSystem.Instance != null && Save.SaveSystem.Instance.CurrentSave != null)
            {
                var save = Save.SaveSystem.Instance.CurrentSave;

                save.inventory.currency = softCurrency;
                save.inventory.premiumCurrency = hardCurrency;

                // Save owned items
                save.inventory.items.Clear();
                foreach (var itemId in ownedItems)
                {
                    save.inventory.items.Add(new Save.InventoryItemData
                    {
                        itemID = itemId,
                        quantity = 1
                    });
                }

                Save.SaveSystem.Instance.SaveGame(Save.SaveSystem.Instance.CurrentSlot);
            }
        }

        #endregion

        #region Properties

        public int SoftCurrency => softCurrency;
        public int HardCurrency => hardCurrency;
        public int TotalItemsOwned => ownedItems.Count;

        #endregion
    }

    #region Data Structures

    public enum CurrencyType
    {
        Soft,   // Earned through gameplay
        Hard    // Premium (real money)
    }

    public enum ShopCategory
    {
        Weapon,
        Perk,
        Cosmetic,
        Consumable,
        Currency,
        BattlePass
    }

    [System.Serializable]
    public class ShopItemData
    {
        public string itemId;
        public string itemName;
        public string description;
        public ShopCategory category;
        public CurrencyType currencyType;
        public int price;
        public int requiredLevel = 1;
        public bool isPermanent = true;
        public Sprite icon;

        // Display
        public bool isFeatured;
        public bool isOnSale;
        public int salePrice;
        public string saleEndDate;
    }

    public struct MatchResult
    {
        public bool won;
        public bool extracted;
        public int kills;
        public int deaths;
        public float survivalTime;
        public int zombiesKilled;
    }

    #endregion
}
