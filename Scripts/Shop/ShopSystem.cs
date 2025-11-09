using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;
using System;

namespace DeadFrontier.Shop
{
    /// <summary>
    /// Comprehensive in-game shop and store system.
    /// Supports multiple stores, featured items, daily deals, bundles, and sales.
    /// Includes purchase history, wishlists, and gift system.
    /// </summary>
    public class ShopSystem : NetworkBehaviour
    {
        public static ShopSystem Instance { get; private set; }

        [Header("Shop Settings")]
        [SerializeField] private bool enableShop = true;
        [SerializeField] private int featuredItemCount = 6;
        [SerializeField] private int dailyDealCount = 3;

        [Header("Rotation Settings")]
        [SerializeField] private float featuredRotationHours = 24f;
        [SerializeField] private float dailyDealRotationHours = 24f;

        // Shop catalogs
        private Dictionary<string, ShopItem> shopItems = new Dictionary<string, ShopItem>();
        private Dictionary<ShopCategory, List<string>> categorizedItems = new Dictionary<ShopCategory, List<string>>();

        // Player data
        private Dictionary<ulong, PlayerShopData> playerData = new Dictionary<ulong, PlayerShopData>();

        // Featured/deals
        private List<string> featuredItems = new List<string>();
        private List<DailyDeal> dailyDeals = new List<DailyDeal>();
        private DateTime lastFeaturedRotation;
        private DateTime lastDailyDealRotation;

        // Events
        public event Action<ulong, string> OnItemPurchased;
        public event Action OnFeaturedRotation;
        public event Action OnDailyDealsRotation;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeShop();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (!IsServer) return;

            CheckRotations();
        }

        #region Initialization

        private void InitializeShop()
        {
            // Initialize categories
            foreach (ShopCategory category in Enum.GetValues(typeof(ShopCategory)))
            {
                categorizedItems[category] = new List<string>();
            }

            // Weapons
            RegisterItem(new ShopItem
            {
                itemId = "weapon_assault_rifle",
                itemName = "Assault Rifle",
                description = "Standard military assault rifle",
                category = ShopCategory.Weapons,
                rarity = ItemRarity.Common,
                softCurrencyPrice = 5000,
                hardCurrencyPrice = 0,
                levelRequirement = 5,
                stock = -1 // Unlimited
            });

            RegisterItem(new ShopItem
            {
                itemId = "weapon_sniper_elite",
                itemName = "Elite Sniper Rifle",
                description = "High-precision sniper rifle",
                category = ShopCategory.Weapons,
                rarity = ItemRarity.Epic,
                softCurrencyPrice = 0,
                hardCurrencyPrice = 500,
                levelRequirement = 15,
                stock = -1
            });

            // Armor
            RegisterItem(new ShopItem
            {
                itemId = "armor_tactical_vest",
                itemName = "Tactical Vest",
                description = "Military-grade body armor",
                category = ShopCategory.Armor,
                rarity = ItemRarity.Uncommon,
                softCurrencyPrice = 3000,
                hardCurrencyPrice = 0,
                levelRequirement = 3,
                stock = -1
            });

            // Consumables
            RegisterItem(new ShopItem
            {
                itemId = "consumable_health_pack",
                itemName = "Health Pack",
                description = "Restore 50% health",
                category = ShopCategory.Consumables,
                rarity = ItemRarity.Common,
                softCurrencyPrice = 200,
                hardCurrencyPrice = 0,
                levelRequirement = 1,
                stock = -1
            });

            RegisterItem(new ShopItem
            {
                itemId = "consumable_xp_boost",
                itemName = "XP Boost (1 Hour)",
                description = "+50% XP for 1 hour",
                category = ShopCategory.Consumables,
                rarity = ItemRarity.Rare,
                softCurrencyPrice = 0,
                hardCurrencyPrice = 100,
                levelRequirement = 1,
                stock = -1
            });

            // Cosmetics
            RegisterItem(new ShopItem
            {
                itemId = "cosmetic_urban_camo",
                itemName = "Urban Camo Outfit",
                description = "Tactical urban camouflage",
                category = ShopCategory.Cosmetics,
                rarity = ItemRarity.Epic,
                softCurrencyPrice = 0,
                hardCurrencyPrice = 800,
                levelRequirement = 10,
                stock = -1
            });

            RegisterItem(new ShopItem
            {
                itemId = "cosmetic_elite_helmet",
                itemName = "Elite Helmet",
                description = "Limited edition helmet",
                category = ShopCategory.Cosmetics,
                rarity = ItemRarity.Legendary,
                softCurrencyPrice = 0,
                hardCurrencyPrice = 1500,
                levelRequirement = 20,
                stock = 100, // Limited stock
                isLimited = true
            });

            // Bundles
            RegisterItem(new ShopItem
            {
                itemId = "bundle_starter_pack",
                itemName = "Starter Bundle",
                description = "Everything you need to get started",
                category = ShopCategory.Bundles,
                rarity = ItemRarity.Common,
                softCurrencyPrice = 0,
                hardCurrencyPrice = 1000,
                levelRequirement = 1,
                stock = -1,
                bundleContents = new List<BundleContent>
                {
                    new BundleContent { itemId = "weapon_assault_rifle", quantity = 1 },
                    new BundleContent { itemId = "armor_tactical_vest", quantity = 1 },
                    new BundleContent { itemId = "consumable_health_pack", quantity = 5 }
                }
            });

            // Perks
            RegisterItem(new ShopItem
            {
                itemId = "perk_unlock_marathon",
                itemName = "Unlock: Marathon Perk",
                description = "Permanently unlock the Marathon perk",
                category = ShopCategory.Perks,
                rarity = ItemRarity.Uncommon,
                softCurrencyPrice = 2000,
                hardCurrencyPrice = 0,
                levelRequirement = 5,
                stock = -1
            });

            // Currency Packs
            RegisterItem(new ShopItem
            {
                itemId = "currency_small_pack",
                itemName = "Small Gem Pack",
                description = "100 premium gems",
                category = ShopCategory.Currency,
                rarity = ItemRarity.Common,
                softCurrencyPrice = 0,
                hardCurrencyPrice = 0,
                realMoneyPrice = 4.99f,
                levelRequirement = 1,
                stock = -1,
                currencyReward = new CurrencyReward { hardCurrency = 100 }
            });

            RegisterItem(new ShopItem
            {
                itemId = "currency_mega_pack",
                itemName = "Mega Gem Pack",
                description = "1200 premium gems + 300 bonus!",
                category = ShopCategory.Currency,
                rarity = ItemRarity.Legendary,
                softCurrencyPrice = 0,
                hardCurrencyPrice = 0,
                realMoneyPrice = 49.99f,
                levelRequirement = 1,
                stock = -1,
                currencyReward = new CurrencyReward { hardCurrency = 1500 }
            });

            Debug.Log($"[ShopSystem] Initialized {shopItems.Count} shop items");

            // Initialize rotations
            RotateFeaturedItems();
            RotateDailyDeals();
        }

        private void RegisterItem(ShopItem item)
        {
            shopItems[item.itemId] = item;
            categorizedItems[item.category].Add(item.itemId);
        }

        #endregion

        #region Player Initialization

        public void InitializePlayerData(ulong playerId)
        {
            if (playerData.ContainsKey(playerId)) return;

            playerData[playerId] = new PlayerShopData
            {
                playerId = playerId,
                purchaseHistory = new List<PurchaseRecord>(),
                wishlist = new List<string>(),
                ownedItems = new List<string>(),
                totalSpent = 0
            };

            LoadPlayerData(playerId);
        }

        #endregion

        #region Purchase System

        public bool CanPurchase(ulong playerId, string itemId, int quantity = 1)
        {
            if (!shopItems.ContainsKey(itemId)) return false;

            if (!playerData.ContainsKey(playerId))
            {
                InitializePlayerData(playerId);
            }

            var item = shopItems[itemId];
            var data = playerData[playerId];

            // Check level requirement
            int playerLevel = Progression.ProgressionManager.Instance?.GetPlayerLevel(playerId) ?? 1;
            if (playerLevel < item.levelRequirement) return false;

            // Check if already owned (for one-time purchases)
            if (item.isOneTimePurchase && data.ownedItems.Contains(itemId)) return false;

            // Check stock
            if (item.stock == 0) return false;

            // Check currency
            if (item.softCurrencyPrice > 0)
            {
                int playerCurrency = Economy.EconomyManager.Instance?.GetSoftCurrency(playerId) ?? 0;
                if (playerCurrency < item.softCurrencyPrice * quantity) return false;
            }

            if (item.hardCurrencyPrice > 0)
            {
                int playerCurrency = Economy.EconomyManager.Instance?.GetHardCurrency(playerId) ?? 0;
                if (playerCurrency < item.hardCurrencyPrice * quantity) return false;
            }

            return true;
        }

        public bool PurchaseItem(ulong playerId, string itemId, int quantity = 1)
        {
            if (!CanPurchase(playerId, itemId, quantity))
            {
                Debug.LogWarning($"[ShopSystem] Player {playerId} cannot purchase {itemId}");
                return false;
            }

            var item = shopItems[itemId];
            var data = playerData[playerId];

            // Process payment
            if (item.softCurrencyPrice > 0)
            {
                Economy.EconomyManager.Instance?.SpendSoftCurrency(playerId, item.softCurrencyPrice * quantity);
            }

            if (item.hardCurrencyPrice > 0)
            {
                Economy.EconomyManager.Instance?.SpendHardCurrency(playerId, item.hardCurrencyPrice * quantity);
            }

            // Grant item
            GrantPurchasedItem(playerId, item, quantity);

            // Update stock
            if (item.stock > 0)
            {
                item.stock -= quantity;
            }

            // Record purchase
            var purchase = new PurchaseRecord
            {
                itemId = itemId,
                quantity = quantity,
                softCurrencySpent = item.softCurrencyPrice * quantity,
                hardCurrencySpent = item.hardCurrencyPrice * quantity,
                purchaseTime = DateTime.UtcNow
            };

            data.purchaseHistory.Add(purchase);
            data.totalSpent += (item.softCurrencyPrice + item.hardCurrencyPrice) * quantity;

            // Add to owned
            if (!data.ownedItems.Contains(itemId))
            {
                data.ownedItems.Add(itemId);
            }

            OnItemPurchased?.Invoke(playerId, itemId);

            SavePlayerData(playerId);

            Debug.Log($"[ShopSystem] Player {playerId} purchased {itemId} x{quantity}");

            // Notify client
            PurchaseItemClientRpc(playerId, item.itemName, quantity);

            return true;
        }

        [ClientRpc]
        private void PurchaseItemClientRpc(ulong playerId, string itemName, int quantity)
        {
            Debug.Log($"[ShopSystem] Purchased: {itemName} x{quantity}");
        }

        private void GrantPurchasedItem(ulong playerId, ShopItem item, int quantity)
        {
            // Handle different item types
            switch (item.category)
            {
                case ShopCategory.Weapons:
                case ShopCategory.Armor:
                case ShopCategory.Consumables:
                    // Add to inventory
                    Inventory.InventoryManager.Instance?.AddItem(playerId, item.itemId, quantity);
                    break;

                case ShopCategory.Cosmetics:
                    // Unlock cosmetic
                    Customization.CosmeticSystem.Instance?.UnlockCosmetic(playerId, item.itemId, true);
                    break;

                case ShopCategory.Perks:
                    // Unlock perk
                    string perkId = item.itemId.Replace("perk_unlock_", "");
                    Loadout.LoadoutSystem.Instance?.UnlockPerk(playerId, perkId);
                    break;

                case ShopCategory.Bundles:
                    // Grant bundle contents
                    foreach (var content in item.bundleContents)
                    {
                        Inventory.InventoryManager.Instance?.AddItem(playerId, content.itemId, content.quantity * quantity);
                    }
                    break;

                case ShopCategory.Currency:
                    // Grant currency
                    if (item.currencyReward != null)
                    {
                        if (item.currencyReward.softCurrency > 0)
                        {
                            Economy.EconomyManager.Instance?.AddSoftCurrency(playerId, item.currencyReward.softCurrency * quantity);
                        }

                        if (item.currencyReward.hardCurrency > 0)
                        {
                            Economy.EconomyManager.Instance?.AddHardCurrency(playerId, item.currencyReward.hardCurrency * quantity);
                        }
                    }
                    break;
            }

            // Notification
            UI.NotificationSystem.Instance?.SendNotification(
                playerId,
                UI.NotificationType.Reward,
                "Purchase Complete!",
                $"You purchased: {item.itemName}",
                UI.NotificationPriority.Normal
            );
        }

        #endregion

        #region Featured & Daily Deals

        private void CheckRotations()
        {
            // Check featured rotation
            if ((DateTime.UtcNow - lastFeaturedRotation).TotalHours >= featuredRotationHours)
            {
                RotateFeaturedItems();
            }

            // Check daily deals rotation
            if ((DateTime.UtcNow - lastDailyDealRotation).TotalHours >= dailyDealRotationHours)
            {
                RotateDailyDeals();
            }
        }

        private void RotateFeaturedItems()
        {
            featuredItems.Clear();

            // Select random featured items
            var allItems = shopItems.Values.Where(i => !i.isLimited).ToList();

            for (int i = 0; i < featuredItemCount && allItems.Count > 0; i++)
            {
                int randomIndex = UnityEngine.Random.Range(0, allItems.Count);
                featuredItems.Add(allItems[randomIndex].itemId);
                allItems.RemoveAt(randomIndex);
            }

            lastFeaturedRotation = DateTime.UtcNow;

            OnFeaturedRotation?.Invoke();

            Debug.Log($"[ShopSystem] Rotated {featuredItems.Count} featured items");
        }

        private void RotateDailyDeals()
        {
            dailyDeals.Clear();

            // Select random items for deals
            var allItems = shopItems.Values.Where(i => i.softCurrencyPrice > 0 || i.hardCurrencyPrice > 0).ToList();

            for (int i = 0; i < dailyDealCount && allItems.Count > 0; i++)
            {
                int randomIndex = UnityEngine.Random.Range(0, allItems.Count);
                var item = allItems[randomIndex];

                // Random discount 10-50%
                float discount = UnityEngine.Random.Range(0.1f, 0.5f);

                dailyDeals.Add(new DailyDeal
                {
                    itemId = item.itemId,
                    discountPercent = discount,
                    expirationTime = DateTime.UtcNow.AddHours(dailyDealRotationHours)
                });

                allItems.RemoveAt(randomIndex);
            }

            lastDailyDealRotation = DateTime.UtcNow;

            OnDailyDealsRotation?.Invoke();

            Debug.Log($"[ShopSystem] Rotated {dailyDeals.Count} daily deals");
        }

        public List<ShopItem> GetFeaturedItems()
        {
            return featuredItems.Select(id => shopItems[id]).ToList();
        }

        public List<DailyDeal> GetDailyDeals()
        {
            return dailyDeals;
        }

        public int GetDealPrice(string itemId, bool isHardCurrency)
        {
            var deal = dailyDeals.FirstOrDefault(d => d.itemId == itemId);

            if (deal == null || DateTime.UtcNow >= deal.expirationTime)
            {
                var item = shopItems[itemId];
                return isHardCurrency ? item.hardCurrencyPrice : item.softCurrencyPrice;
            }

            var dealItem = shopItems[deal.itemId];
            int originalPrice = isHardCurrency ? dealItem.hardCurrencyPrice : dealItem.softCurrencyPrice;

            return Mathf.RoundToInt(originalPrice * (1f - deal.discountPercent));
        }

        #endregion

        #region Wishlist

        public bool AddToWishlist(ulong playerId, string itemId)
        {
            if (!shopItems.ContainsKey(itemId)) return false;

            if (!playerData.ContainsKey(playerId))
            {
                InitializePlayerData(playerId);
            }

            var data = playerData[playerId];

            if (data.wishlist.Contains(itemId)) return false;

            data.wishlist.Add(itemId);
            SavePlayerData(playerId);

            Debug.Log($"[ShopSystem] Player {playerId} added {itemId} to wishlist");

            return true;
        }

        public bool RemoveFromWishlist(ulong playerId, string itemId)
        {
            if (!playerData.ContainsKey(playerId)) return false;

            bool removed = playerData[playerId].wishlist.Remove(itemId);

            if (removed)
            {
                SavePlayerData(playerId);
            }

            return removed;
        }

        public List<ShopItem> GetWishlist(ulong playerId)
        {
            if (!playerData.ContainsKey(playerId)) return new List<ShopItem>();

            return playerData[playerId].wishlist
                .Select(id => shopItems[id])
                .ToList();
        }

        #endregion

        #region Queries

        public List<ShopItem> GetItemsByCategory(ShopCategory category)
        {
            if (!categorizedItems.ContainsKey(category)) return new List<ShopItem>();

            return categorizedItems[category].Select(id => shopItems[id]).ToList();
        }

        public List<ShopItem> SearchItems(string searchTerm)
        {
            return shopItems.Values
                .Where(i => i.itemName.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0 ||
                           i.description.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
        }

        public ShopItem GetItem(string itemId)
        {
            return shopItems.ContainsKey(itemId) ? shopItems[itemId] : null;
        }

        public List<PurchaseRecord> GetPurchaseHistory(ulong playerId, int maxCount = 20)
        {
            if (!playerData.ContainsKey(playerId)) return new List<PurchaseRecord>();

            return playerData[playerId].purchaseHistory
                .OrderByDescending(p => p.purchaseTime)
                .Take(maxCount)
                .ToList();
        }

        #endregion

        #region Data Persistence

        private void SavePlayerData(ulong playerId)
        {
            if (!playerData.ContainsKey(playerId)) return;

            var data = playerData[playerId];
            string json = JsonUtility.ToJson(data);

            SaveSystem.SaveManager.Instance?.SaveData($"shop_{playerId}", json);
        }

        public void LoadPlayerData(ulong playerId)
        {
            string json = SaveSystem.SaveManager.Instance?.LoadData($"shop_{playerId}");

            if (!string.IsNullOrEmpty(json))
            {
                var data = JsonUtility.FromJson<PlayerShopData>(json);
                playerData[playerId] = data;

                Debug.Log($"[ShopSystem] Loaded shop data for player {playerId}");
            }
        }

        #endregion
    }

    #region Data Classes

    [Serializable]
    public class ShopItem
    {
        public string itemId;
        public string itemName;
        public string description;
        public ShopCategory category;
        public ItemRarity rarity;
        public int softCurrencyPrice;
        public int hardCurrencyPrice;
        public float realMoneyPrice; // For real money purchases
        public int levelRequirement;
        public int stock; // -1 = unlimited
        public bool isLimited;
        public bool isOneTimePurchase;
        public List<BundleContent> bundleContents; // For bundles
        public CurrencyReward currencyReward; // For currency packs
    }

    [Serializable]
    public class BundleContent
    {
        public string itemId;
        public int quantity;
    }

    [Serializable]
    public class CurrencyReward
    {
        public int softCurrency;
        public int hardCurrency;
    }

    [Serializable]
    public class DailyDeal
    {
        public string itemId;
        public float discountPercent; // 0.1 = 10% off
        public DateTime expirationTime;
    }

    [Serializable]
    public class PlayerShopData
    {
        public ulong playerId;
        public List<PurchaseRecord> purchaseHistory = new List<PurchaseRecord>();
        public List<string> wishlist = new List<string>();
        public List<string> ownedItems = new List<string>();
        public int totalSpent;
    }

    [Serializable]
    public class PurchaseRecord
    {
        public string itemId;
        public int quantity;
        public int softCurrencySpent;
        public int hardCurrencySpent;
        public DateTime purchaseTime;
    }

    public enum ShopCategory
    {
        Weapons,
        Armor,
        Consumables,
        Cosmetics,
        Perks,
        Bundles,
        Currency,
        Special
    }

    public enum ItemRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    #endregion
}
