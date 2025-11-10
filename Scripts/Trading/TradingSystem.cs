using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Trading
{
    public class TradingSystem : NetworkBehaviour
    {
        public static TradingSystem Instance { get; private set; }

        [Header("Trading Configuration")]
        [SerializeField] private float tradeTaxRate = 0.05f;
        [SerializeField] private int maxAuctionListings = 10;
        [SerializeField] private float defaultAuctionDuration = 86400f; // 24 hours
        [SerializeField] private int maxTradeDistance = 10;

        private Dictionary<string, TradeRequest> activeP2PTrades = new Dictionary<string, TradeRequest>();
        private Dictionary<string, AuctionListing> auctionHouse = new Dictionary<string, AuctionListing>();
        private Dictionary<ulong, List<string>> playerListings = new Dictionary<ulong, List<string>>();
        private Dictionary<string, MarketPrice> marketPrices = new Dictionary<string, MarketPrice>();

        public event Action<string> OnTradeRequestSent;
        public event Action<string> OnTradeCompleted;
        public event Action<string, ulong> OnAuctionListingCreated;
        public event Action<string, ulong> OnAuctionSold;

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
                InvokeRepeating(nameof(UpdateAuctions), 60f, 60f);
                InvokeRepeating(nameof(UpdateMarketPrices), 300f, 300f);
            }
        }

        // P2P Trading
        [ServerRpc(RequireOwnership = false)]
        public void RequestTradeServerRpc(ulong senderId, ulong recipientId, ServerRpcParams rpcParams = default)
        {
            // Check distance between players
            // if (!IsWithinTradeDistance(senderId, recipientId)) return;

            var trade = new TradeRequest
            {
                tradeId = $"trade_{Guid.NewGuid()}",
                senderId = senderId,
                recipientId = recipientId,
                senderOffers = new List<TradeItem>(),
                recipientOffers = new List<TradeItem>(),
                senderAccepted = false,
                recipientAccepted = false,
                status = TradeStatus.Pending,
                creationTime = DateTime.UtcNow
            };

            activeP2PTrades[trade.tradeId] = trade;
            OnTradeRequestSent?.Invoke(trade.tradeId);
            NotifyTradeRequestClientRpc(recipientId, trade.tradeId, senderId);

            Debug.Log($"Trade request sent from {senderId} to {recipientId}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void AddTradeItemServerRpc(string tradeId, ulong playerId, string itemId, int quantity, int currency, ServerRpcParams rpcParams = default)
        {
            if (!activeP2PTrades.TryGetValue(tradeId, out var trade)) return;

            var tradeItem = new TradeItem
            {
                itemId = itemId,
                quantity = quantity,
                currency = currency
            };

            if (playerId == trade.senderId)
            {
                trade.senderOffers.Add(tradeItem);
                trade.senderAccepted = false;
            }
            else if (playerId == trade.recipientId)
            {
                trade.recipientOffers.Add(tradeItem);
                trade.recipientAccepted = false;
            }

            NotifyTradeUpdatedClientRpc(tradeId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void AcceptTradeServerRpc(string tradeId, ulong playerId, ServerRpcParams rpcParams = default)
        {
            if (!activeP2PTrades.TryGetValue(tradeId, out var trade)) return;

            if (playerId == trade.senderId)
            {
                trade.senderAccepted = true;
            }
            else if (playerId == trade.recipientId)
            {
                trade.recipientAccepted = true;
            }

            if (trade.senderAccepted && trade.recipientAccepted)
            {
                ExecuteTrade(trade);
            }
        }

        private void ExecuteTrade(TradeRequest trade)
        {
            trade.status = TradeStatus.Completed;

            // Transfer items from sender to recipient
            foreach (var item in trade.senderOffers)
            {
                if (!string.IsNullOrEmpty(item.itemId))
                {
                    // Remove from sender, add to recipient
                    // Inventory.InventoryManager.Instance?.RemoveItem(trade.senderId, item.itemId, item.quantity);
                    // Inventory.InventoryManager.Instance?.AddItem(trade.recipientId, item.itemId, item.quantity);
                }
                if (item.currency > 0)
                {
                    Economy.EconomyManager.Instance?.SpendSoftCurrency(trade.senderId, item.currency);
                    Economy.EconomyManager.Instance?.AddSoftCurrency(trade.recipientId, item.currency);
                }
            }

            // Transfer items from recipient to sender
            foreach (var item in trade.recipientOffers)
            {
                if (!string.IsNullOrEmpty(item.itemId))
                {
                    // Inventory.InventoryManager.Instance?.RemoveItem(trade.recipientId, item.itemId, item.quantity);
                    // Inventory.InventoryManager.Instance?.AddItem(trade.senderId, item.itemId, item.quantity);
                }
                if (item.currency > 0)
                {
                    Economy.EconomyManager.Instance?.SpendSoftCurrency(trade.recipientId, item.currency);
                    Economy.EconomyManager.Instance?.AddSoftCurrency(trade.senderId, item.currency);
                }
            }

            OnTradeCompleted?.Invoke(trade.tradeId);
            activeP2PTrades.Remove(trade.tradeId);

            Debug.Log($"Trade completed between {trade.senderId} and {trade.recipientId}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void CancelTradeServerRpc(string tradeId, ServerRpcParams rpcParams = default)
        {
            if (activeP2PTrades.TryGetValue(tradeId, out var trade))
            {
                trade.status = TradeStatus.Cancelled;
                activeP2PTrades.Remove(tradeId);
            }
        }

        // Auction House
        [ServerRpc(RequireOwnership = false)]
        public void CreateAuctionListingServerRpc(ulong sellerId, string itemId, int quantity, int startingBid, int buyoutPrice, float duration, ServerRpcParams rpcParams = default)
        {
            if (!playerListings.ContainsKey(sellerId))
            {
                playerListings[sellerId] = new List<string>();
            }

            if (playerListings[sellerId].Count >= maxAuctionListings)
            {
                Debug.LogWarning($"Player {sellerId} has max auction listings");
                return;
            }

            var listing = new AuctionListing
            {
                listingId = $"auction_{Guid.NewGuid()}",
                sellerId = sellerId,
                itemId = itemId,
                quantity = quantity,
                startingBid = startingBid,
                currentBid = startingBid,
                buyoutPrice = buyoutPrice,
                listingTime = DateTime.UtcNow,
                expirationTime = DateTime.UtcNow.AddSeconds(duration),
                status = AuctionStatus.Active,
                bidHistory = new List<Bid>()
            };

            auctionHouse[listing.listingId] = listing;
            playerListings[sellerId].Add(listing.listingId);

            OnAuctionListingCreated?.Invoke(listing.listingId, sellerId);

            Debug.Log($"Auction listing created: {itemId} for {startingBid} currency");
        }

        [ServerRpc(RequireOwnership = false)]
        public void PlaceBidServerRpc(ulong bidderId, string listingId, int bidAmount, ServerRpcParams rpcParams = default)
        {
            if (!auctionHouse.TryGetValue(listingId, out var listing)) return;

            if (listing.status != AuctionStatus.Active)
            {
                Debug.LogWarning("Auction is not active");
                return;
            }

            if (DateTime.UtcNow >= listing.expirationTime)
            {
                Debug.LogWarning("Auction has expired");
                return;
            }

            if (bidAmount <= listing.currentBid)
            {
                Debug.LogWarning("Bid must be higher than current bid");
                return;
            }

            // Check if player has enough currency
            // Would integrate with economy system

            // Return previous bidder's currency
            if (listing.currentBidderId.HasValue)
            {
                Economy.EconomyManager.Instance?.AddSoftCurrency(listing.currentBidderId.Value, listing.currentBid);
            }

            // Take new bidder's currency
            if (!Economy.EconomyManager.Instance.SpendSoftCurrency(bidderId, bidAmount))
            {
                Debug.LogWarning($"Player {bidderId} cannot afford bid");
                return;
            }

            listing.currentBid = bidAmount;
            listing.currentBidderId = bidderId;
            listing.bidHistory.Add(new Bid
            {
                bidderId = bidderId,
                bidAmount = bidAmount,
                bidTime = DateTime.UtcNow
            });

            Debug.Log($"Bid placed on {listingId}: {bidAmount} by player {bidderId}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void BuyoutAuctionServerRpc(ulong buyerId, string listingId, ServerRpcParams rpcParams = default)
        {
            if (!auctionHouse.TryGetValue(listingId, out var listing)) return;

            if (listing.status != AuctionStatus.Active)
            {
                Debug.LogWarning("Auction is not active");
                return;
            }

            if (listing.buyoutPrice <= 0)
            {
                Debug.LogWarning("This auction has no buyout price");
                return;
            }

            // Calculate tax
            int tax = Mathf.RoundToInt(listing.buyoutPrice * tradeTaxRate);
            int sellerProceeds = listing.buyoutPrice - tax;

            // Transfer currency
            if (!Economy.EconomyManager.Instance.SpendSoftCurrency(buyerId, listing.buyoutPrice))
            {
                Debug.LogWarning($"Player {buyerId} cannot afford buyout");
                return;
            }

            Economy.EconomyManager.Instance?.AddSoftCurrency(listing.sellerId, sellerProceeds);

            // Transfer item
            // Inventory.InventoryManager.Instance?.AddItem(buyerId, listing.itemId, listing.quantity);

            // Return previous bidder's currency if any
            if (listing.currentBidderId.HasValue)
            {
                Economy.EconomyManager.Instance?.AddSoftCurrency(listing.currentBidderId.Value, listing.currentBid);
            }

            listing.status = AuctionStatus.Sold;
            listing.buyerId = buyerId;

            OnAuctionSold?.Invoke(listingId, buyerId);

            // Remove from active listings
            if (playerListings.TryGetValue(listing.sellerId, out var listings))
            {
                listings.Remove(listingId);
            }

            // Update market prices
            UpdateMarketPrice(listing.itemId, listing.buyoutPrice);

            Debug.Log($"Auction {listingId} bought out by {buyerId} for {listing.buyoutPrice}");
        }

        private void UpdateAuctions()
        {
            var expiredListings = auctionHouse.Values.Where(a => a.status == AuctionStatus.Active && DateTime.UtcNow >= a.expirationTime).ToList();

            foreach (var listing in expiredListings)
            {
                if (listing.currentBidderId.HasValue)
                {
                    // Auction ended with a bid
                    int tax = Mathf.RoundToInt(listing.currentBid * tradeTaxRate);
                    int sellerProceeds = listing.currentBid - tax;

                    Economy.EconomyManager.Instance?.AddSoftCurrency(listing.sellerId, sellerProceeds);
                    // Inventory.InventoryManager.Instance?.AddItem(listing.currentBidderId.Value, listing.itemId, listing.quantity);

                    listing.status = AuctionStatus.Sold;
                    listing.buyerId = listing.currentBidderId.Value;

                    OnAuctionSold?.Invoke(listing.listingId, listing.currentBidderId.Value);

                    // Update market prices
                    UpdateMarketPrice(listing.itemId, listing.currentBid);
                }
                else
                {
                    // Auction expired with no bids
                    listing.status = AuctionStatus.Expired;
                    // Return item to seller
                    // Inventory.InventoryManager.Instance?.AddItem(listing.sellerId, listing.itemId, listing.quantity);
                }

                // Remove from active listings
                if (playerListings.TryGetValue(listing.sellerId, out var listings))
                {
                    listings.Remove(listing.listingId);
                }
            }
        }

        private void UpdateMarketPrice(string itemId, int salePrice)
        {
            if (!marketPrices.ContainsKey(itemId))
            {
                marketPrices[itemId] = new MarketPrice
                {
                    itemId = itemId,
                    averagePrice = salePrice,
                    minPrice = salePrice,
                    maxPrice = salePrice,
                    totalSales = 1,
                    recentSales = new List<int> { salePrice }
                };
            }
            else
            {
                var price = marketPrices[itemId];
                price.recentSales.Add(salePrice);
                if (price.recentSales.Count > 100) price.recentSales.RemoveAt(0);

                price.averagePrice = (int)price.recentSales.Average();
                price.minPrice = price.recentSales.Min();
                price.maxPrice = price.recentSales.Max();
                price.totalSales++;
            }
        }

        private void UpdateMarketPrices()
        {
            // Recalculate market trends
        }

        [ServerRpc(RequireOwnership = false)]
        public void CancelAuctionListingServerRpc(ulong playerId, string listingId, ServerRpcParams rpcParams = default)
        {
            if (!auctionHouse.TryGetValue(listingId, out var listing)) return;
            if (listing.sellerId != playerId) return;

            if (listing.currentBidderId.HasValue)
            {
                Economy.EconomyManager.Instance?.AddSoftCurrency(listing.currentBidderId.Value, listing.currentBid);
            }

            listing.status = AuctionStatus.Cancelled;

            if (playerListings.TryGetValue(playerId, out var listings))
            {
                listings.Remove(listingId);
            }
        }

        public List<AuctionListing> SearchAuctions(string itemId = null, int maxPrice = int.MaxValue, AuctionSortBy sortBy = AuctionSortBy.TimeRemaining)
        {
            var results = auctionHouse.Values.Where(a => a.status == AuctionStatus.Active);

            if (!string.IsNullOrEmpty(itemId))
            {
                results = results.Where(a => a.itemId.Contains(itemId));
            }

            if (maxPrice < int.MaxValue)
            {
                results = results.Where(a => a.currentBid <= maxPrice || (a.buyoutPrice > 0 && a.buyoutPrice <= maxPrice));
            }

            return sortBy switch
            {
                AuctionSortBy.PriceLowToHigh => results.OrderBy(a => a.currentBid).ToList(),
                AuctionSortBy.PriceHighToLow => results.OrderByDescending(a => a.currentBid).ToList(),
                AuctionSortBy.TimeRemaining => results.OrderBy(a => a.expirationTime).ToList(),
                _ => results.ToList()
            };
        }

        [ClientRpc]
        private void NotifyTradeRequestClientRpc(ulong playerId, string tradeId, ulong senderId) { }

        [ClientRpc]
        private void NotifyTradeUpdatedClientRpc(string tradeId) { }

        public TradeRequest GetTrade(string tradeId) => activeP2PTrades.GetValueOrDefault(tradeId);
        public AuctionListing GetAuctionListing(string listingId) => auctionHouse.GetValueOrDefault(listingId);
        public MarketPrice GetMarketPrice(string itemId) => marketPrices.GetValueOrDefault(itemId);
    }

    [Serializable]
    public class TradeRequest
    {
        public string tradeId;
        public ulong senderId;
        public ulong recipientId;
        public List<TradeItem> senderOffers;
        public List<TradeItem> recipientOffers;
        public bool senderAccepted;
        public bool recipientAccepted;
        public TradeStatus status;
        public DateTime creationTime;
    }

    [Serializable]
    public class TradeItem
    {
        public string itemId;
        public int quantity;
        public int currency;
    }

    [Serializable]
    public class AuctionListing
    {
        public string listingId;
        public ulong sellerId;
        public string itemId;
        public int quantity;
        public int startingBid;
        public int currentBid;
        public ulong? currentBidderId;
        public int buyoutPrice;
        public DateTime listingTime;
        public DateTime expirationTime;
        public AuctionStatus status;
        public ulong? buyerId;
        public List<Bid> bidHistory;
    }

    [Serializable]
    public class Bid
    {
        public ulong bidderId;
        public int bidAmount;
        public DateTime bidTime;
    }

    [Serializable]
    public class MarketPrice
    {
        public string itemId;
        public int averagePrice;
        public int minPrice;
        public int maxPrice;
        public int totalSales;
        public List<int> recentSales;
    }

    public enum TradeStatus { Pending, Completed, Cancelled, Expired }
    public enum AuctionStatus { Active, Sold, Expired, Cancelled }
    public enum AuctionSortBy { TimeRemaining, PriceLowToHigh, PriceHighToLow }
}
