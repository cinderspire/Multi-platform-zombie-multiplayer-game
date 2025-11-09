using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;
using System;

namespace DeadFrontier.Trading
{
    /// <summary>
    /// Comprehensive player trading and marketplace system.
    /// Supports direct trades, auction house, buy orders, trade history.
    /// Includes anti-scam protection, trade limits, and market analytics.
    /// </summary>
    public class TradingSystem : NetworkBehaviour
    {
        public static TradingSystem Instance { get; private set; }

        [Header("Trading Settings")]
        [SerializeField] private bool enableDirectTrading = true;
        [SerializeField] private bool enableMarketplace = true;
        [SerializeField] private float tradeTimeoutSeconds = 300f; // 5 minutes
        [SerializeField] private int maxActiveListings = 10;

        [Header("Market Settings")]
        [SerializeField] private float marketTaxRate = 0.05f; // 5% tax
        [SerializeField] private int listingDurationDays = 7;
        [SerializeField] private int maxBuyOrders = 5;

        [Header("Security Settings")]
        [SerializeField] private int minPlayerLevel = 5; // Minimum level to trade
        [SerializeField] private float tradeCooldownSeconds = 60f;
        [SerializeField] private bool requireConfirmation = true;

        // Active trades
        private Dictionary<string, TradeOffer> activeOffers = new Dictionary<string, TradeOffer>();

        // Marketplace
        private Dictionary<string, MarketListing> activeListings = new Dictionary<string, MarketListing>();
        private Dictionary<string, BuyOrder> activeBuyOrders = new Dictionary<string, BuyOrder>();

        // Player data
        private Dictionary<ulong, PlayerTradingData> playerData = new Dictionary<ulong, PlayerTradingData>();

        // Market analytics
        private Dictionary<string, MarketData> marketPrices = new Dictionary<string, MarketData>();

        // Events
        public event Action<string, TradeOffer> OnTradeProposed;
        public event Action<string> OnTradeCompleted;
        public event Action<string> OnTradeCancelled;
        public event Action<string, MarketListing> OnItemListed;
        public event Action<string, MarketListing> OnItemSold;

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

        private void Update()
        {
            if (!IsServer) return;

            CheckTradeTimeouts();
            CheckListingExpirations();
            ProcessBuyOrders();
        }

        #region Player Initialization

        public void InitializePlayerData(ulong playerId)
        {
            if (playerData.ContainsKey(playerId)) return;

            playerData[playerId] = new PlayerTradingData
            {
                playerId = playerId,
                tradeHistory = new List<TradeRecord>(),
                activeListings = new List<string>(),
                activeBuyOrders = new List<string>(),
                lastTradeTime = DateTime.MinValue,
                totalTradesCompleted = 0,
                reputationScore = 100 // Default reputation
            };

            LoadPlayerData(playerId);
        }

        #endregion

        #region Direct Trading

        public string ProposeDirectTrade(ulong senderId, ulong receiverId, List<TradeItem> offeredItems, List<TradeItem> requestedItems, int offeredCurrency = 0, int requestedCurrency = 0)
        {
            if (!enableDirectTrading)
            {
                Debug.LogWarning("[TradingSystem] Direct trading is disabled");
                return null;
            }

            if (!CanTrade(senderId) || !CanTrade(receiverId))
            {
                Debug.LogWarning($"[TradingSystem] One or both players cannot trade");
                return null;
            }

            // Check cooldown
            if (IsOnCooldown(senderId))
            {
                Debug.LogWarning($"[TradingSystem] Player {senderId} is on trade cooldown");
                return null;
            }

            // Verify sender has the items
            if (!VerifyPlayerHasItems(senderId, offeredItems, offeredCurrency))
            {
                Debug.LogWarning($"[TradingSystem] Player {senderId} doesn't have offered items/currency");
                return null;
            }

            string tradeId = $"trade_{senderId}_{receiverId}_{DateTime.UtcNow.Ticks}";

            var trade = new TradeOffer
            {
                tradeId = tradeId,
                senderId = senderId,
                receiverId = receiverId,
                offeredItems = offeredItems,
                requestedItems = requestedItems,
                offeredCurrency = offeredCurrency,
                requestedCurrency = requestedCurrency,
                proposedTime = DateTime.UtcNow,
                senderAccepted = false,
                receiverAccepted = false,
                status = TradeStatus.Pending
            };

            activeOffers[tradeId] = trade;

            OnTradeProposed?.Invoke(tradeId, trade);

            Debug.Log($"[TradingSystem] Trade proposed: {tradeId}");

            // Notify receiver
            ProposeTradeClientRpc(receiverId, tradeId, senderId);

            return tradeId;
        }

        [ClientRpc]
        private void ProposeTradeClientRpc(ulong receiverId, string tradeId, ulong senderId)
        {
            Debug.Log($"[TradingSystem] Trade offer received from player {senderId}");
        }

        public bool AcceptTrade(ulong playerId, string tradeId)
        {
            if (!activeOffers.ContainsKey(tradeId)) return false;

            var trade = activeOffers[tradeId];

            if (trade.status != TradeStatus.Pending) return false;

            // Determine which side is accepting
            if (playerId == trade.senderId)
            {
                trade.senderAccepted = true;
            }
            else if (playerId == trade.receiverId)
            {
                trade.receiverAccepted = true;
            }
            else
            {
                return false;
            }

            // Check if both accepted
            if (trade.senderAccepted && trade.receiverAccepted)
            {
                ExecuteTrade(tradeId);
            }

            return true;
        }

        private void ExecuteTrade(string tradeId)
        {
            if (!activeOffers.ContainsKey(tradeId)) return;

            var trade = activeOffers[tradeId];

            // Verify both players still have the items
            if (!VerifyPlayerHasItems(trade.senderId, trade.offeredItems, trade.offeredCurrency) ||
                !VerifyPlayerHasItems(trade.receiverId, trade.requestedItems, trade.requestedCurrency))
            {
                Debug.LogWarning($"[TradingSystem] Trade verification failed for {tradeId}");
                CancelTrade(tradeId);
                return;
            }

            // Execute the exchange
            // Sender gives items/currency to receiver
            foreach (var item in trade.offeredItems)
            {
                Inventory.InventoryManager.Instance?.RemoveItem(trade.senderId, item.itemId, item.quantity);
                Inventory.InventoryManager.Instance?.AddItem(trade.receiverId, item.itemId, item.quantity);
            }

            if (trade.offeredCurrency > 0)
            {
                Economy.EconomyManager.Instance?.SpendSoftCurrency(trade.senderId, trade.offeredCurrency);
                Economy.EconomyManager.Instance?.AddSoftCurrency(trade.receiverId, trade.offeredCurrency);
            }

            // Receiver gives items/currency to sender
            foreach (var item in trade.requestedItems)
            {
                Inventory.InventoryManager.Instance?.RemoveItem(trade.receiverId, item.itemId, item.quantity);
                Inventory.InventoryManager.Instance?.AddItem(trade.senderId, item.itemId, item.quantity);
            }

            if (trade.requestedCurrency > 0)
            {
                Economy.EconomyManager.Instance?.SpendSoftCurrency(trade.receiverId, trade.requestedCurrency);
                Economy.EconomyManager.Instance?.AddSoftCurrency(trade.senderId, trade.requestedCurrency);
            }

            trade.status = TradeStatus.Completed;
            trade.completedTime = DateTime.UtcNow;

            // Update player data
            RecordTradeCompletion(trade.senderId, tradeId);
            RecordTradeCompletion(trade.receiverId, tradeId);

            // Update cooldowns
            playerData[trade.senderId].lastTradeTime = DateTime.UtcNow;
            playerData[trade.receiverId].lastTradeTime = DateTime.UtcNow;

            OnTradeCompleted?.Invoke(tradeId);

            Debug.Log($"[TradingSystem] Trade completed: {tradeId}");

            // Remove from active
            activeOffers.Remove(tradeId);

            // Notify clients
            TradeCompletedClientRpc(trade.senderId, tradeId);
            TradeCompletedClientRpc(trade.receiverId, tradeId);
        }

        [ClientRpc]
        private void TradeCompletedClientRpc(ulong playerId, string tradeId)
        {
            Debug.Log($"[TradingSystem] Trade completed: {tradeId}");
        }

        public void CancelTrade(string tradeId)
        {
            if (!activeOffers.ContainsKey(tradeId)) return;

            var trade = activeOffers[tradeId];
            trade.status = TradeStatus.Cancelled;

            activeOffers.Remove(tradeId);

            OnTradeCancelled?.Invoke(tradeId);

            Debug.Log($"[TradingSystem] Trade cancelled: {tradeId}");
        }

        #endregion

        #region Marketplace

        public string ListItemOnMarket(ulong sellerId, string itemId, int quantity, int price)
        {
            if (!enableMarketplace)
            {
                Debug.LogWarning("[TradingSystem] Marketplace is disabled");
                return null;
            }

            if (!CanTrade(sellerId))
            {
                Debug.LogWarning($"[TradingSystem] Player {sellerId} cannot trade");
                return null;
            }

            if (!playerData.ContainsKey(sellerId))
            {
                InitializePlayerData(sellerId);
            }

            // Check listing limit
            if (playerData[sellerId].activeListings.Count >= maxActiveListings)
            {
                Debug.LogWarning($"[TradingSystem] Player {sellerId} has reached max active listings");
                return null;
            }

            // Verify player has item
            if (Inventory.InventoryManager.Instance?.GetItemCount(sellerId, itemId) < quantity)
            {
                Debug.LogWarning($"[TradingSystem] Player {sellerId} doesn't have {itemId} x{quantity}");
                return null;
            }

            // Remove item from player inventory (held in escrow)
            Inventory.InventoryManager.Instance?.RemoveItem(sellerId, itemId, quantity);

            string listingId = $"listing_{sellerId}_{DateTime.UtcNow.Ticks}";

            var listing = new MarketListing
            {
                listingId = listingId,
                sellerId = sellerId,
                itemId = itemId,
                quantity = quantity,
                price = price,
                listedTime = DateTime.UtcNow,
                expirationTime = DateTime.UtcNow.AddDays(listingDurationDays),
                status = ListingStatus.Active
            };

            activeListings[listingId] = listing;
            playerData[sellerId].activeListings.Add(listingId);

            OnItemListed?.Invoke(listingId, listing);

            // Update market data
            UpdateMarketData(itemId, price);

            Debug.Log($"[TradingSystem] Item listed: {itemId} x{quantity} for {price} credits");

            SavePlayerData(sellerId);

            return listingId;
        }

        public bool BuyFromMarket(ulong buyerId, string listingId)
        {
            if (!activeListings.ContainsKey(listingId))
            {
                Debug.LogWarning($"[TradingSystem] Listing {listingId} not found");
                return false;
            }

            var listing = activeListings[listingId];

            if (listing.status != ListingStatus.Active)
            {
                Debug.LogWarning($"[TradingSystem] Listing {listingId} is not active");
                return false;
            }

            // Calculate total cost with tax
            int totalCost = Mathf.RoundToInt(listing.price * (1f + marketTaxRate));

            // Check if buyer has enough currency
            if (Economy.EconomyManager.Instance?.GetSoftCurrency(buyerId) < totalCost)
            {
                Debug.LogWarning($"[TradingSystem] Buyer {buyerId} doesn't have enough currency");
                return false;
            }

            // Execute purchase
            Economy.EconomyManager.Instance?.SpendSoftCurrency(buyerId, totalCost);
            Economy.EconomyManager.Instance?.AddSoftCurrency(listing.sellerId, listing.price);

            // Give item to buyer
            Inventory.InventoryManager.Instance?.AddItem(buyerId, listing.itemId, listing.quantity);

            // Update listing
            listing.status = ListingStatus.Sold;
            listing.buyerId = buyerId;
            listing.soldTime = DateTime.UtcNow;

            // Remove from seller's active listings
            playerData[listing.sellerId].activeListings.Remove(listingId);

            // Record transaction
            RecordMarketTransaction(listing.sellerId, buyerId, listing);

            OnItemSold?.Invoke(listingId, listing);

            // Update market data
            UpdateMarketData(listing.itemId, listing.price);

            Debug.Log($"[TradingSystem] Item sold: {listing.itemId} x{listing.quantity} for {listing.price} credits");

            // Remove from active listings
            activeListings.Remove(listingId);

            // Notify seller
            UI.NotificationSystem.Instance?.SendNotification(
                listing.sellerId,
                UI.NotificationType.Trade,
                "Item Sold!",
                $"Your {listing.itemId} sold for {listing.price} credits",
                UI.NotificationPriority.Normal
            );

            SavePlayerData(listing.sellerId);
            SavePlayerData(buyerId);

            return true;
        }

        public void CancelListing(ulong sellerId, string listingId)
        {
            if (!activeListings.ContainsKey(listingId)) return;

            var listing = activeListings[listingId];

            if (listing.sellerId != sellerId) return;

            // Return item to seller
            Inventory.InventoryManager.Instance?.AddItem(sellerId, listing.itemId, listing.quantity);

            // Update status
            listing.status = ListingStatus.Cancelled;

            // Remove from active
            activeListings.Remove(listingId);
            playerData[sellerId].activeListings.Remove(listingId);

            SavePlayerData(sellerId);

            Debug.Log($"[TradingSystem] Listing cancelled: {listingId}");
        }

        #endregion

        #region Buy Orders

        public string CreateBuyOrder(ulong buyerId, string itemId, int quantity, int maxPrice)
        {
            if (!playerData.ContainsKey(buyerId))
            {
                InitializePlayerData(buyerId);
            }

            // Check buy order limit
            if (playerData[buyerId].activeBuyOrders.Count >= maxBuyOrders)
            {
                Debug.LogWarning($"[TradingSystem] Player {buyerId} has reached max buy orders");
                return null;
            }

            // Check if buyer has enough currency (reserve it)
            int totalCost = quantity * maxPrice;

            if (Economy.EconomyManager.Instance?.GetSoftCurrency(buyerId) < totalCost)
            {
                Debug.LogWarning($"[TradingSystem] Buyer {buyerId} doesn't have enough currency");
                return null;
            }

            // Reserve the currency
            Economy.EconomyManager.Instance?.SpendSoftCurrency(buyerId, totalCost);

            string orderId = $"buyorder_{buyerId}_{DateTime.UtcNow.Ticks}";

            var order = new BuyOrder
            {
                orderId = orderId,
                buyerId = buyerId,
                itemId = itemId,
                quantity = quantity,
                maxPrice = maxPrice,
                reservedCurrency = totalCost,
                createdTime = DateTime.UtcNow,
                status = BuyOrderStatus.Active
            };

            activeBuyOrders[orderId] = order;
            playerData[buyerId].activeBuyOrders.Add(orderId);

            SavePlayerData(buyerId);

            Debug.Log($"[TradingSystem] Buy order created: {itemId} x{quantity} at max {maxPrice} each");

            return orderId;
        }

        private void ProcessBuyOrders()
        {
            // Match buy orders with market listings
            foreach (var order in activeBuyOrders.Values.ToList())
            {
                if (order.status != BuyOrderStatus.Active) continue;

                // Find matching listings
                var matchingListings = activeListings.Values
                    .Where(l => l.status == ListingStatus.Active &&
                                l.itemId == order.itemId &&
                                l.price <= order.maxPrice)
                    .OrderBy(l => l.price)
                    .ToList();

                if (matchingListings.Count == 0) continue;

                // Try to fulfill order
                int remaining = order.quantity;

                foreach (var listing in matchingListings)
                {
                    if (remaining <= 0) break;

                    int quantityToBuy = Mathf.Min(remaining, listing.quantity);

                    // Execute partial/full purchase
                    // (This would normally split listings, but for simplicity we'll just match full quantities)
                    if (quantityToBuy == listing.quantity)
                    {
                        // Use existing BuyFromMarket logic
                        BuyFromMarket(order.buyerId, listing.listingId);
                        remaining -= quantityToBuy;
                    }
                }

                // Check if order fulfilled
                if (remaining == 0)
                {
                    order.status = BuyOrderStatus.Fulfilled;
                    activeBuyOrders.Remove(order.orderId);
                    playerData[order.buyerId].activeBuyOrders.Remove(order.orderId);
                }
                else if (remaining < order.quantity)
                {
                    order.quantity = remaining;
                }
            }
        }

        public void CancelBuyOrder(ulong buyerId, string orderId)
        {
            if (!activeBuyOrders.ContainsKey(orderId)) return;

            var order = activeBuyOrders[orderId];

            if (order.buyerId != buyerId) return;

            // Return reserved currency
            Economy.EconomyManager.Instance?.AddSoftCurrency(buyerId, order.reservedCurrency);

            // Update status
            order.status = BuyOrderStatus.Cancelled;

            // Remove from active
            activeBuyOrders.Remove(orderId);
            playerData[buyerId].activeBuyOrders.Remove(orderId);

            SavePlayerData(buyerId);

            Debug.Log($"[TradingSystem] Buy order cancelled: {orderId}");
        }

        #endregion

        #region Market Analytics

        private void UpdateMarketData(string itemId, int price)
        {
            if (!marketPrices.ContainsKey(itemId))
            {
                marketPrices[itemId] = new MarketData
                {
                    itemId = itemId,
                    recentPrices = new List<int>(),
                    averagePrice = price,
                    lowestPrice = price,
                    highestPrice = price,
                    totalVolume = 0
                };
            }

            var data = marketPrices[itemId];

            data.recentPrices.Add(price);
            data.totalVolume++;

            // Keep last 100 prices
            if (data.recentPrices.Count > 100)
            {
                data.recentPrices.RemoveAt(0);
            }

            // Update stats
            data.averagePrice = Mathf.RoundToInt(data.recentPrices.Average());
            data.lowestPrice = data.recentPrices.Min();
            data.highestPrice = data.recentPrices.Max();
            data.lastUpdated = DateTime.UtcNow;
        }

        public MarketData GetMarketData(string itemId)
        {
            return marketPrices.ContainsKey(itemId) ? marketPrices[itemId] : null;
        }

        public List<MarketListing> SearchMarket(string itemId = null, int maxPrice = int.MaxValue)
        {
            var results = activeListings.Values.Where(l => l.status == ListingStatus.Active);

            if (!string.IsNullOrEmpty(itemId))
            {
                results = results.Where(l => l.itemId == itemId);
            }

            if (maxPrice < int.MaxValue)
            {
                results = results.Where(l => l.price <= maxPrice);
            }

            return results.OrderBy(l => l.price).ToList();
        }

        #endregion

        #region Validation & Helpers

        private bool CanTrade(ulong playerId)
        {
            // Check player level requirement
            int playerLevel = Progression.ProgressionManager.Instance?.GetPlayerLevel(playerId) ?? 0;

            if (playerLevel < minPlayerLevel)
            {
                return false;
            }

            return true;
        }

        private bool IsOnCooldown(ulong playerId)
        {
            if (!playerData.ContainsKey(playerId)) return false;

            TimeSpan timeSinceLastTrade = DateTime.UtcNow - playerData[playerId].lastTradeTime;

            return timeSinceLastTrade.TotalSeconds < tradeCooldownSeconds;
        }

        private bool VerifyPlayerHasItems(ulong playerId, List<TradeItem> items, int currency)
        {
            // Check currency
            if (currency > 0)
            {
                int playerCurrency = Economy.EconomyManager.Instance?.GetSoftCurrency(playerId) ?? 0;
                if (playerCurrency < currency) return false;
            }

            // Check items
            foreach (var item in items)
            {
                int playerAmount = Inventory.InventoryManager.Instance?.GetItemCount(playerId, item.itemId) ?? 0;
                if (playerAmount < item.quantity) return false;
            }

            return true;
        }

        private void CheckTradeTimeouts()
        {
            var expiredOffers = activeOffers.Values
                .Where(o => o.status == TradeStatus.Pending &&
                           (DateTime.UtcNow - o.proposedTime).TotalSeconds > tradeTimeoutSeconds)
                .ToList();

            foreach (var offer in expiredOffers)
            {
                CancelTrade(offer.tradeId);
            }
        }

        private void CheckListingExpirations()
        {
            var expiredListings = activeListings.Values
                .Where(l => l.status == ListingStatus.Active && DateTime.UtcNow >= l.expirationTime)
                .ToList();

            foreach (var listing in expiredListings)
            {
                // Return item to seller
                Inventory.InventoryManager.Instance?.AddItem(listing.sellerId, listing.itemId, listing.quantity);

                listing.status = ListingStatus.Expired;
                activeListings.Remove(listing.listingId);
                playerData[listing.sellerId].activeListings.Remove(listing.listingId);

                Debug.Log($"[TradingSystem] Listing expired: {listing.listingId}");
            }
        }

        private void RecordTradeCompletion(ulong playerId, string tradeId)
        {
            if (!playerData.ContainsKey(playerId)) return;

            var data = playerData[playerId];

            data.tradeHistory.Add(new TradeRecord
            {
                tradeId = tradeId,
                timestamp = DateTime.UtcNow,
                type = TradeType.Direct
            });

            data.totalTradesCompleted++;

            SavePlayerData(playerId);
        }

        private void RecordMarketTransaction(ulong sellerId, ulong buyerId, MarketListing listing)
        {
            if (playerData.ContainsKey(sellerId))
            {
                playerData[sellerId].tradeHistory.Add(new TradeRecord
                {
                    tradeId = listing.listingId,
                    timestamp = DateTime.UtcNow,
                    type = TradeType.MarketSell
                });
            }

            if (playerData.ContainsKey(buyerId))
            {
                playerData[buyerId].tradeHistory.Add(new TradeRecord
                {
                    tradeId = listing.listingId,
                    timestamp = DateTime.UtcNow,
                    type = TradeType.MarketBuy
                });
            }
        }

        #endregion

        #region Data Persistence

        private void SavePlayerData(ulong playerId)
        {
            if (!playerData.ContainsKey(playerId)) return;

            var data = playerData[playerId];
            string json = JsonUtility.ToJson(data);

            SaveSystem.SaveManager.Instance?.SaveData($"trading_{playerId}", json);
        }

        public void LoadPlayerData(ulong playerId)
        {
            string json = SaveSystem.SaveManager.Instance?.LoadData($"trading_{playerId}");

            if (!string.IsNullOrEmpty(json))
            {
                var data = JsonUtility.FromJson<PlayerTradingData>(json);
                playerData[playerId] = data;

                Debug.Log($"[TradingSystem] Loaded trading data for player {playerId}");
            }
        }

        #endregion
    }

    #region Data Classes

    [Serializable]
    public class TradeOffer
    {
        public string tradeId;
        public ulong senderId;
        public ulong receiverId;
        public List<TradeItem> offeredItems = new List<TradeItem>();
        public List<TradeItem> requestedItems = new List<TradeItem>();
        public int offeredCurrency;
        public int requestedCurrency;
        public DateTime proposedTime;
        public DateTime completedTime;
        public bool senderAccepted;
        public bool receiverAccepted;
        public TradeStatus status;
    }

    [Serializable]
    public class TradeItem
    {
        public string itemId;
        public int quantity;
    }

    [Serializable]
    public class MarketListing
    {
        public string listingId;
        public ulong sellerId;
        public ulong buyerId;
        public string itemId;
        public int quantity;
        public int price;
        public DateTime listedTime;
        public DateTime expirationTime;
        public DateTime soldTime;
        public ListingStatus status;
    }

    [Serializable]
    public class BuyOrder
    {
        public string orderId;
        public ulong buyerId;
        public string itemId;
        public int quantity;
        public int maxPrice;
        public int reservedCurrency;
        public DateTime createdTime;
        public BuyOrderStatus status;
    }

    [Serializable]
    public class PlayerTradingData
    {
        public ulong playerId;
        public List<TradeRecord> tradeHistory = new List<TradeRecord>();
        public List<string> activeListings = new List<string>();
        public List<string> activeBuyOrders = new List<string>();
        public DateTime lastTradeTime;
        public int totalTradesCompleted;
        public int reputationScore;
    }

    [Serializable]
    public class TradeRecord
    {
        public string tradeId;
        public DateTime timestamp;
        public TradeType type;
    }

    [Serializable]
    public class MarketData
    {
        public string itemId;
        public List<int> recentPrices = new List<int>();
        public int averagePrice;
        public int lowestPrice;
        public int highestPrice;
        public int totalVolume;
        public DateTime lastUpdated;
    }

    public enum TradeStatus
    {
        Pending,
        Completed,
        Cancelled,
        Expired
    }

    public enum ListingStatus
    {
        Active,
        Sold,
        Cancelled,
        Expired
    }

    public enum BuyOrderStatus
    {
        Active,
        Fulfilled,
        Cancelled
    }

    public enum TradeType
    {
        Direct,
        MarketBuy,
        MarketSell
    }

    #endregion
}
