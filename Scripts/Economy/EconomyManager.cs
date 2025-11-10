using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Economy
{
    /// <summary>
    /// Comprehensive economy system with multiple currencies, shops, marketplace, auctions,
    /// daily deals, bundles, and complete transaction management.
    /// </summary>
    public class EconomyManager : NetworkBehaviour
    {
        public static EconomyManager Instance { get; private set; }

        [Header("Economy Configuration")]
        [SerializeField] private int startingSoftCurrency = 1000;
        [SerializeField] private int startingHardCurrency = 100;
        [SerializeField] private float marketplaceTaxRate = 0.05f;
        [SerializeField] private int dailyDealCount = 6;

        // Player wallets
        private Dictionary<ulong, PlayerWallet> playerWallets = new Dictionary<ulong, PlayerWallet>();
        
        // Shop data
        private Dictionary<string, Shop> shopDatabase = new Dictionary<string, Shop>();
        private Dictionary<string, Bundle> bundleDatabase = new Dictionary<string, Bundle>();
        private Dictionary<ulong, List<string>> playerPurchaseHistory = new Dictionary<ulong, List<string>>();
        private Dictionary<string, DailyDeal> dailyDeals = new Dictionary<string, DailyDeal>();
        
        // Marketplace
        private Dictionary<string, MarketplaceListing> activeListings = new Dictionary<string, MarketplaceListing>();

        // Events
        public event Action<ulong, CurrencyType, int> OnCurrencyChanged;
        public event Action<ulong, string, int> OnPurchaseCompleted;
        public event Action<ulong, string> OnBundlePurchased;
        public event Action<string> OnDailyDealsRefreshed;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer)
            {
                InitializeShops();
                InitializeBundles();
                RefreshDailyDeals();
            }
        }

        private void InitializeShops()
        {
            // ===== GENERAL SHOP =====
            shopDatabase["general"] = new Shop
            {
                shopId = "general",
                shopName = "General Store",
                description = "Basic supplies and equipment",
                shopType = ShopType.General,
                categories = new List<ShopCategory>
                {
                    new ShopCategory
                    {
                        categoryId = "consumables",
                        categoryName = "Consumables",
                        items = new List<ShopItem>
                        {
                            new ShopItem
                            {
                                itemId = "consumable_bandage",
                                price = 25,
                                currencyType = CurrencyType.Soft,
                                stockLimit = 100,
                                restockTime = 3600
                            },
                            new ShopItem
                            {
                                itemId = "consumable_medkit",
                                price = 100,
                                currencyType = CurrencyType.Soft,
                                stockLimit = 50,
                                restockTime = 3600
                            },
                            new ShopItem
                            {
                                itemId = "consumable_energy_drink",
                                price = 50,
                                currencyType = CurrencyType.Soft,
                                stockLimit = 50,
                                restockTime = 3600
                            }
                        }
                    },
                    new ShopCategory
                    {
                        categoryId = "ammo",
                        categoryName = "Ammunition",
                        items = new List<ShopItem>
                        {
                            new ShopItem
                            {
                                itemId = "ammo_9mm",
                                price = 1,
                                currencyType = CurrencyType.Soft,
                                stockLimit = 1000,
                                restockTime = 1800
                            },
                            new ShopItem
                            {
                                itemId = "ammo_556",
                                price = 2,
                                currencyType = CurrencyType.Soft,
                                stockLimit = 500,
                                restockTime = 1800
                            },
                            new ShopItem
                            {
                                itemId = "ammo_762",
                                price = 3,
                                currencyType = CurrencyType.Soft,
                                stockLimit = 300,
                                restockTime = 1800
                            },
                            new ShopItem
                            {
                                itemId = "ammo_shotgun",
                                price = 5,
                                currencyType = CurrencyType.Soft,
                                stockLimit = 200,
                                restockTime = 1800
                            }
                        }
                    }
                }
            };

            // ===== WEAPON SHOP =====
            shopDatabase["weapons"] = new Shop
            {
                shopId = "weapons",
                shopName = "Armory",
                description = "Premium weapons and modifications",
                shopType = ShopType.Weapons,
                categories = new List<ShopCategory>
                {
                    new ShopCategory
                    {
                        categoryId = "pistols",
                        categoryName = "Pistols",
                        items = new List<ShopItem>
                        {
                            new ShopItem
                            {
                                itemId = "weapon_pistol_glock",
                                price = 500,
                                currencyType = CurrencyType.Soft,
                                requiredLevel = 1
                            },
                            new ShopItem
                            {
                                itemId = "weapon_pistol_desert_eagle",
                                price = 2000,
                                currencyType = CurrencyType.Soft,
                                requiredLevel = 10
                            }
                        }
                    },
                    new ShopCategory
                    {
                        categoryId = "rifles",
                        categoryName = "Rifles",
                        items = new List<ShopItem>
                        {
                            new ShopItem
                            {
                                itemId = "weapon_rifle_m4",
                                price = 5000,
                                currencyType = CurrencyType.Soft,
                                requiredLevel = 15
                            },
                            new ShopItem
                            {
                                itemId = "weapon_rifle_ak47",
                                price = 6000,
                                currencyType = CurrencyType.Soft,
                                requiredLevel = 20
                            },
                            new ShopItem
                            {
                                itemId = "weapon_rifle_scar",
                                price = 200,
                                currencyType = CurrencyType.Hard,
                                requiredLevel = 25
                            }
                        }
                    },
                    new ShopCategory
                    {
                        categoryId = "sniper",
                        categoryName = "Sniper Rifles",
                        items = new List<ShopItem>
                        {
                            new ShopItem
                            {
                                itemId = "weapon_sniper_awp",
                                price = 8000,
                                currencyType = CurrencyType.Soft,
                                requiredLevel = 25
                            },
                            new ShopItem
                            {
                                itemId = "weapon_sniper_barrett",
                                price = 300,
                                currencyType = CurrencyType.Hard,
                                requiredLevel = 35
                            }
                        }
                    }
                }
            };

            // ===== ARMOR SHOP =====
            shopDatabase["armor"] = new Shop
            {
                shopId = "armor",
                shopName = "Outfitter",
                description = "Protective gear and equipment",
                shopType = ShopType.Armor,
                categories = new List<ShopCategory>
                {
                    new ShopCategory
                    {
                        categoryId = "helmets",
                        categoryName = "Helmets",
                        items = new List<ShopItem>
                        {
                            new ShopItem { itemId = "armor_helmet_basic", price = 300, currencyType = CurrencyType.Soft, requiredLevel = 1 },
                            new ShopItem { itemId = "armor_helmet_tactical", price = 2000, currencyType = CurrencyType.Soft, requiredLevel = 20 }
                        }
                    },
                    new ShopCategory
                    {
                        categoryId = "vests",
                        categoryName = "Body Armor",
                        items = new List<ShopItem>
                        {
                            new ShopItem { itemId = "armor_vest_light", price = 500, currencyType = CurrencyType.Soft, requiredLevel = 1 },
                            new ShopItem { itemId = "armor_vest_heavy", price = 6000, currencyType = CurrencyType.Soft, requiredLevel = 30 }
                        }
                    },
                    new ShopCategory
                    {
                        categoryId = "backpacks",
                        categoryName = "Backpacks",
                        items = new List<ShopItem>
                        {
                            new ShopItem { itemId = "armor_backpack_medium", price = 1000, currencyType = CurrencyType.Soft, requiredLevel = 5 },
                            new ShopItem { itemId = "armor_backpack_large", price = 3000, currencyType = CurrencyType.Soft, requiredLevel = 15 }
                        }
                    }
                }
            };

            // ===== PREMIUM SHOP =====
            shopDatabase["premium"] = new Shop
            {
                shopId = "premium",
                shopName = "Premium Store",
                description = "Exclusive items for premium currency",
                shopType = ShopType.Premium,
                categories = new List<ShopCategory>
                {
                    new ShopCategory
                    {
                        categoryId = "boosters",
                        categoryName = "Boosters",
                        items = new List<ShopItem>
                        {
                            new ShopItem
                            {
                                itemId = "boost_xp_1h",
                                price = 50,
                                currencyType = CurrencyType.Hard,
                                description = "+50% XP for 1 hour"
                            },
                            new ShopItem
                            {
                                itemId = "boost_xp_24h",
                                price = 200,
                                currencyType = CurrencyType.Hard,
                                description = "+50% XP for 24 hours"
                            },
                            new ShopItem
                            {
                                itemId = "boost_currency_1h",
                                price = 50,
                                currencyType = CurrencyType.Hard,
                                description = "+100% currency for 1 hour"
                            }
                        }
                    },
                    new ShopCategory
                    {
                        categoryId = "convenience",
                        categoryName = "Convenience",
                        items = new List<ShopItem>
                        {
                            new ShopItem { itemId = "instant_teleport", price = 20, currencyType = CurrencyType.Hard },
                            new ShopItem { itemId = "inventory_expansion", price = 100, currencyType = CurrencyType.Hard },
                            new ShopItem { itemId = "storage_expansion", price = 150, currencyType = CurrencyType.Hard }
                        }
                    },
                    new ShopCategory
                    {
                        categoryId = "crates",
                        categoryName = "Loot Crates",
                        items = new List<ShopItem>
                        {
                            new ShopItem { itemId = "crate_common", price = 50, currencyType = CurrencyType.Hard },
                            new ShopItem { itemId = "crate_rare", price = 100, currencyType = CurrencyType.Hard },
                            new ShopItem { itemId = "crate_epic", price = 200, currencyType = CurrencyType.Hard },
                            new ShopItem { itemId = "crate_legendary", price = 500, currencyType = CurrencyType.Hard }
                        }
                    }
                }
            };

            // ===== MATERIALS SHOP =====
            shopDatabase["materials"] = new Shop
            {
                shopId = "materials",
                shopName = "Resource Trader",
                description = "Crafting materials and resources",
                shopType = ShopType.Materials,
                categories = new List<ShopCategory>
                {
                    new ShopCategory
                    {
                        categoryId = "basic",
                        categoryName = "Basic Materials",
                        items = new List<ShopItem>
                        {
                            new ShopItem { itemId = "material_wood", price = 1, currencyType = CurrencyType.Soft },
                            new ShopItem { itemId = "material_stone", price = 2, currencyType = CurrencyType.Soft },
                            new ShopItem { itemId = "material_cloth", price = 1, currencyType = CurrencyType.Soft }
                        }
                    },
                    new ShopCategory
                    {
                        categoryId = "advanced",
                        categoryName = "Advanced Materials",
                        items = new List<ShopItem>
                        {
                            new ShopItem { itemId = "material_metal", price = 5, currencyType = CurrencyType.Soft },
                            new ShopItem { itemId = "material_electronics", price = 10, currencyType = CurrencyType.Soft },
                            new ShopItem { itemId = "material_steel", price = 15, currencyType = CurrencyType.Soft }
                        }
                    }
                }
            };

            Debug.Log($"Initialized {shopDatabase.Count} shops");
        }

        private void InitializeBundles()
        {
            // Starter Bundle
            bundleDatabase["starter"] = new Bundle
            {
                bundleId = "starter",
                bundleName = "Starter Pack",
                description = "Everything you need to get started",
                price = 500,
                currencyType = CurrencyType.Hard,
                originalValue = 1000,
                discountPercent = 50,
                items = new List<BundleItem>
                {
                    new BundleItem { itemId = "weapon_rifle_m4", quantity = 1 },
                    new BundleItem { itemId = "ammo_556", quantity = 200 },
                    new BundleItem { itemId = "armor_vest_light", quantity = 1 },
                    new BundleItem { itemId = "consumable_medkit", quantity = 10 },
                    new BundleItem { itemId = "currency_soft", quantity = 5000 }
                },
                oneTimePurchase = true,
                requiredLevel = 1
            };

            // Weekly Deal Bundle
            bundleDatabase["weekly"] = new Bundle
            {
                bundleId = "weekly",
                bundleName = "Weekly Survival Pack",
                description = "Limited time offer!",
                price = 1000,
                currencyType = CurrencyType.Hard,
                originalValue = 2000,
                discountPercent = 50,
                items = new List<BundleItem>
                {
                    new BundleItem { itemId = "weapon_sniper_awp", quantity = 1 },
                    new BundleItem { itemId = "armor_backpack_large", quantity = 1 },
                    new BundleItem { itemId = "boost_xp_24h", quantity = 3 },
                    new BundleItem { itemId = "crate_epic", quantity = 5 },
                    new BundleItem { itemId = "currency_soft", quantity = 20000 }
                },
                oneTimePurchase = false,
                requiredLevel = 10,
                expirationDate = DateTime.UtcNow.AddDays(7)
            };

            // Ultimate Bundle
            bundleDatabase["ultimate"] = new Bundle
            {
                bundleId = "ultimate",
                bundleName = "Ultimate Survivor Pack",
                description = "The best value pack available",
                price = 5000,
                currencyType = CurrencyType.Hard,
                originalValue = 15000,
                discountPercent = 67,
                items = new List<BundleItem>
                {
                    new BundleItem { itemId = "weapon_sniper_barrett", quantity = 1 },
                    new BundleItem { itemId = "armor_vest_heavy", quantity = 1 },
                    new BundleItem { itemId = "armor_helmet_tactical", quantity = 1 },
                    new BundleItem { itemId = "battlepass_premium", quantity = 1 },
                    new BundleItem { itemId = "boost_xp_24h", quantity = 10 },
                    new BundleItem { itemId = "crate_legendary", quantity = 10 },
                    new BundleItem { itemId = "currency_soft", quantity = 100000 },
                    new BundleItem { itemId = "inventory_expansion", quantity = 3 }
                },
                oneTimePurchase = false,
                requiredLevel = 1,
                featured = true
            };

            // Currency Bundles
            bundleDatabase["currency_small"] = new Bundle
            {
                bundleId = "currency_small",
                bundleName = "Small Coin Pack",
                description = "500 premium currency",
                price = 499, // USD cents
                currencyType = CurrencyType.RealMoney,
                items = new List<BundleItem>
                {
                    new BundleItem { itemId = "currency_hard", quantity = 500 }
                }
            };

            bundleDatabase["currency_medium"] = new Bundle
            {
                bundleId = "currency_medium",
                bundleName = "Medium Coin Pack",
                description = "1200 premium currency (+20% bonus)",
                price = 999,
                currencyType = CurrencyType.RealMoney,
                originalValue = 1000,
                bonusPercent = 20,
                items = new List<BundleItem>
                {
                    new BundleItem { itemId = "currency_hard", quantity = 1200 }
                }
            };

            bundleDatabase["currency_large"] = new Bundle
            {
                bundleId = "currency_large",
                bundleName = "Large Coin Pack",
                description = "2600 premium currency (+30% bonus)",
                price = 1999,
                currencyType = CurrencyType.RealMoney,
                originalValue = 2000,
                bonusPercent = 30,
                items = new List<BundleItem>
                {
                    new BundleItem { itemId = "currency_hard", quantity = 2600 }
                },
                popular = true
            };

            bundleDatabase["currency_huge"] = new Bundle
            {
                bundleId = "currency_huge",
                bundleName = "Mega Coin Pack",
                description = "5500 premium currency (+37.5% bonus)",
                price = 4999,
                currencyType = CurrencyType.RealMoney,
                originalValue = 4000,
                bonusPercent = 38,
                items = new List<BundleItem>
                {
                    new BundleItem { itemId = "currency_hard", quantity = 5500 }
                },
                bestValue = true
            };

            Debug.Log($"Initialized {bundleDatabase.Count} bundles");
        }

        private void RefreshDailyDeals()
        {
            dailyDeals.Clear();

            // Generate random daily deals
            var allItems = new List<string>
            {
                "weapon_rifle_ak47", "weapon_sniper_awp", "armor_vest_tactical",
                "armor_helmet_tactical", "consumable_stim_pack", "boost_xp_1h"
            };

            for (int i = 0; i < dailyDealCount && i < allItems.Count; i++)
            {
                string itemId = allItems[i];
                int discount = UnityEngine.Random.Range(20, 70);

                dailyDeals[$"daily_{i}"] = new DailyDeal
                {
                    dealId = $"daily_{i}",
                    itemId = itemId,
                    originalPrice = 1000 + (i * 500),
                    discountedPrice = Mathf.RoundToInt((1000 + (i * 500)) * (1f - discount / 100f)),
                    currencyType = CurrencyType.Soft,
                    discountPercent = discount,
                    expirationTime = DateTime.UtcNow.AddHours(24)
                };
            }

            OnDailyDealsRefreshed?.Invoke(DateTime.UtcNow.ToString());
            Debug.Log($"Refreshed {dailyDeals.Count} daily deals");
        }

        // Wallet operations
        [ServerRpc(RequireOwnership = false)]
        public void InitializePlayerWalletServerRpc(ulong playerId, ServerRpcParams rpcParams = default)
        {
            if (playerWallets.ContainsKey(playerId)) return;

            playerWallets[playerId] = new PlayerWallet
            {
                playerId = playerId,
                softCurrency = startingSoftCurrency,
                hardCurrency = startingHardCurrency,
                premiumCurrency = 0,
                seasonCurrency = 0,
                totalSpent = 0,
                totalEarned = startingSoftCurrency + startingHardCurrency
            };

            playerPurchaseHistory[playerId] = new List<string>();

            Debug.Log($"Initialized wallet for player {playerId}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void AddCurrencyServerRpc(ulong playerId, CurrencyType currencyType, int amount, ServerRpcParams rpcParams = default)
        {
            if (!playerWallets.TryGetValue(playerId, out var wallet)) return;

            switch (currencyType)
            {
                case CurrencyType.Soft:
                    wallet.softCurrency += amount;
                    break;
                case CurrencyType.Hard:
                    wallet.hardCurrency += amount;
                    break;
                case CurrencyType.Premium:
                    wallet.premiumCurrency += amount;
                    break;
                case CurrencyType.Season:
                    wallet.seasonCurrency += amount;
                    break;
            }

            wallet.totalEarned += amount;

            OnCurrencyChanged?.Invoke(playerId, currencyType, amount);
            NotifyCurrencyChangedClientRpc(playerId, currencyType, GetCurrencyAmount(playerId, currencyType));

            Debug.Log($"Player {playerId} gained {amount} {currencyType} currency");
        }

        [ServerRpc(RequireOwnership = false)]
        public void RemoveCurrencyServerRpc(ulong playerId, CurrencyType currencyType, int amount, ServerRpcParams rpcParams = default)
        {
            if (!playerWallets.TryGetValue(playerId, out var wallet)) return;

            bool success = false;

            switch (currencyType)
            {
                case CurrencyType.Soft:
                    if (wallet.softCurrency >= amount)
                    {
                        wallet.softCurrency -= amount;
                        success = true;
                    }
                    break;
                case CurrencyType.Hard:
                    if (wallet.hardCurrency >= amount)
                    {
                        wallet.hardCurrency -= amount;
                        success = true;
                    }
                    break;
                case CurrencyType.Premium:
                    if (wallet.premiumCurrency >= amount)
                    {
                        wallet.premiumCurrency -= amount;
                        success = true;
                    }
                    break;
                case CurrencyType.Season:
                    if (wallet.seasonCurrency >= amount)
                    {
                        wallet.seasonCurrency -= amount;
                        success = true;
                    }
                    break;
            }

            if (success)
            {
                wallet.totalSpent += amount;
                OnCurrencyChanged?.Invoke(playerId, currencyType, -amount);
                NotifyCurrencyChangedClientRpc(playerId, currencyType, GetCurrencyAmount(playerId, currencyType));
            }
        }

        // Purchase operations
        [ServerRpc(RequireOwnership = false)]
        public void PurchaseItemServerRpc(ulong playerId, string shopId, string itemId, int quantity, ServerRpcParams rpcParams = default)
        {
            if (!shopDatabase.TryGetValue(shopId, out var shop)) return;

            ShopItem shopItem = null;
            foreach (var category in shop.categories)
            {
                shopItem = category.items.FirstOrDefault(i => i.itemId == itemId);
                if (shopItem != null) break;
            }

            if (shopItem == null) return;

            int totalPrice = shopItem.price * quantity;

            // Check currency
            if (!HasCurrency(playerId, shopItem.currencyType, totalPrice))
            {
                Debug.LogWarning($"Player {playerId} insufficient funds");
                return;
            }

            // Check level requirement
            // Would integrate with progression system

            // Deduct currency
            RemoveCurrencyServerRpc(playerId, shopItem.currencyType, totalPrice);

            // Grant item
            Inventory.InventorySystem.Instance?.AddItemServerRpc(playerId, itemId, quantity, Inventory.ContainerType.Backpack);

            // Track purchase
            if (playerPurchaseHistory.TryGetValue(playerId, out var history))
            {
                history.Add(itemId);
            }

            OnPurchaseCompleted?.Invoke(playerId, itemId, quantity);

            Debug.Log($"Player {playerId} purchased {quantity}x {itemId}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void PurchaseBundleServerRpc(ulong playerId, string bundleId, ServerRpcParams rpcParams = default)
        {
            if (!bundleDatabase.TryGetValue(bundleId, out var bundle)) return;

            // Check if already purchased (one-time bundles)
            if (bundle.oneTimePurchase && playerPurchaseHistory.TryGetValue(playerId, out var history))
            {
                if (history.Contains(bundleId))
                {
                    Debug.LogWarning($"Player {playerId} already purchased one-time bundle {bundleId}");
                    return;
                }
            }

            // Check currency
            if (!HasCurrency(playerId, bundle.currencyType, bundle.price))
            {
                Debug.LogWarning($"Player {playerId} insufficient funds for bundle");
                return;
            }

            // Deduct currency
            if (bundle.currencyType != CurrencyType.RealMoney)
            {
                RemoveCurrencyServerRpc(playerId, bundle.currencyType, bundle.price);
            }

            // Grant all items
            foreach (var bundleItem in bundle.items)
            {
                if (bundleItem.itemId.StartsWith("currency_"))
                {
                    CurrencyType currType = bundleItem.itemId == "currency_soft" ? CurrencyType.Soft : CurrencyType.Hard;
                    AddCurrencyServerRpc(playerId, currType, bundleItem.quantity);
                }
                else
                {
                    Inventory.InventorySystem.Instance?.AddItemServerRpc(playerId, bundleItem.itemId, bundleItem.quantity, Inventory.ContainerType.Backpack);
                }
            }

            // Track purchase
            if (playerPurchaseHistory.TryGetValue(playerId, out var purchaseHistory))
            {
                purchaseHistory.Add(bundleId);
            }

            OnBundlePurchased?.Invoke(playerId, bundleId);

            Debug.Log($"Player {playerId} purchased bundle {bundle.bundleName}");
        }

        // Marketplace operations
        [ServerRpc(RequireOwnership = false)]
        public void ListItemOnMarketplaceServerRpc(ulong sellerId, string itemId, int quantity, int price, ServerRpcParams rpcParams = default)
        {
            // Check if player has item
            if (!Inventory.InventorySystem.Instance.HasItem(sellerId, itemId, quantity, Inventory.ContainerType.Backpack))
            {
                Debug.LogWarning($"Player {sellerId} doesn't have item to list");
                return;
            }

            string listingId = Guid.NewGuid().ToString();

            var listing = new MarketplaceListing
            {
                listingId = listingId,
                sellerId = sellerId,
                itemId = itemId,
                quantity = quantity,
                price = price,
                listingTime = DateTime.UtcNow,
                status = ListingStatus.Active
            };

            activeListings[listingId] = listing;

            // Remove item from inventory
            Inventory.InventorySystem.Instance?.RemoveItemServerRpc(sellerId, itemId, quantity, Inventory.ContainerType.Backpack);

            Debug.Log($"Player {sellerId} listed {quantity}x {itemId} for {price}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void PurchaseMarketplaceListingServerRpc(ulong buyerId, string listingId, ServerRpcParams rpcParams = default)
        {
            if (!activeListings.TryGetValue(listingId, out var listing)) return;
            if (listing.status != ListingStatus.Active) return;

            int totalPrice = listing.price;
            int tax = Mathf.RoundToInt(totalPrice * marketplaceTaxRate);
            int sellerProfit = totalPrice - tax;

            // Check buyer funds
            if (!HasCurrency(buyerId, CurrencyType.Soft, totalPrice))
            {
                Debug.LogWarning($"Buyer {buyerId} insufficient funds");
                return;
            }

            // Transfer currency
            RemoveCurrencyServerRpc(buyerId, CurrencyType.Soft, totalPrice);
            AddCurrencyServerRpc(listing.sellerId, CurrencyType.Soft, sellerProfit);

            // Transfer item
            Inventory.InventorySystem.Instance?.AddItemServerRpc(buyerId, listing.itemId, listing.quantity, Inventory.ContainerType.Backpack);

            // Mark listing as sold
            listing.status = ListingStatus.Sold;
            activeListings.Remove(listingId);

            Debug.Log($"Player {buyerId} bought listing {listingId} from {listing.sellerId}");
        }

        // Helper methods
        public bool HasCurrency(ulong playerId, CurrencyType currencyType, int amount)
        {
            if (!playerWallets.TryGetValue(playerId, out var wallet)) return false;

            return currencyType switch
            {
                CurrencyType.Soft => wallet.softCurrency >= amount,
                CurrencyType.Hard => wallet.hardCurrency >= amount,
                CurrencyType.Premium => wallet.premiumCurrency >= amount,
                CurrencyType.Season => wallet.seasonCurrency >= amount,
                _ => false
            };
        }

        public int GetCurrencyAmount(ulong playerId, CurrencyType currencyType)
        {
            if (!playerWallets.TryGetValue(playerId, out var wallet)) return 0;

            return currencyType switch
            {
                CurrencyType.Soft => wallet.softCurrency,
                CurrencyType.Hard => wallet.hardCurrency,
                CurrencyType.Premium => wallet.premiumCurrency,
                CurrencyType.Season => wallet.seasonCurrency,
                _ => 0
            };
        }

        // Client RPCs
        [ClientRpc]
        private void NotifyCurrencyChangedClientRpc(ulong playerId, CurrencyType currencyType, int newAmount) { }

        // Public getters
        public PlayerWallet GetPlayerWallet(ulong playerId) => playerWallets.GetValueOrDefault(playerId);
        public Shop GetShop(string shopId) => shopDatabase.GetValueOrDefault(shopId);
        public Bundle GetBundle(string bundleId) => bundleDatabase.GetValueOrDefault(bundleId);
        public List<DailyDeal> GetDailyDeals() => dailyDeals.Values.ToList();
    }

    // Data structures
    [Serializable]
    public class PlayerWallet
    {
        public ulong playerId;
        public int softCurrency;
        public int hardCurrency;
        public int premiumCurrency;
        public int seasonCurrency;
        public int totalSpent;
        public int totalEarned;
    }

    [Serializable]
    public class Shop
    {
        public string shopId;
        public string shopName;
        public string description;
        public ShopType shopType;
        public List<ShopCategory> categories;
    }

    [Serializable]
    public class ShopCategory
    {
        public string categoryId;
        public string categoryName;
        public List<ShopItem> items;
    }

    [Serializable]
    public class ShopItem
    {
        public string itemId;
        public int price;
        public CurrencyType currencyType;
        public int stockLimit;
        public float restockTime;
        public int requiredLevel;
        public string description;
    }

    [Serializable]
    public class Bundle
    {
        public string bundleId;
        public string bundleName;
        public string description;
        public int price;
        public CurrencyType currencyType;
        public int originalValue;
        public int discountPercent;
        public int bonusPercent;
        public List<BundleItem> items;
        public bool oneTimePurchase;
        public int requiredLevel;
        public DateTime expirationDate;
        public bool featured;
        public bool popular;
        public bool bestValue;
    }

    [Serializable]
    public class BundleItem
    {
        public string itemId;
        public int quantity;
    }

    [Serializable]
    public class DailyDeal
    {
        public string dealId;
        public string itemId;
        public int originalPrice;
        public int discountedPrice;
        public CurrencyType currencyType;
        public int discountPercent;
        public DateTime expirationTime;
    }

    [Serializable]
    public class MarketplaceListing
    {
        public string listingId;
        public ulong sellerId;
        public string itemId;
        public int quantity;
        public int price;
        public DateTime listingTime;
        public ListingStatus status;
    }

    public enum CurrencyType { Soft, Hard, Premium, Season, RealMoney }
    public enum ShopType { General, Weapons, Armor, Premium, Materials }
    public enum ListingStatus { Active, Sold, Cancelled, Expired }
}
