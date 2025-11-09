using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Social
{
    /// <summary>
    /// Comprehensive guild/clan treasury and shared resources system.
    /// Handles shared currency, items, donations, withdrawals, and financial management.
    /// </summary>
    public class GuildTreasurySystem : NetworkBehaviour
    {
        public static GuildTreasurySystem Instance { get; private set; }

        [Header("Treasury Settings")]
        [SerializeField] private bool enableTreasury = true;
        [SerializeField] private int maxItemStacks = 200;
        [SerializeField] private bool enableTaxSystem = true;
        [SerializeField] private float defaultTaxRate = 0.05f; // 5%

        [Header("Permission Settings")]
        [SerializeField] private bool requireWithdrawPermission = true;
        [SerializeField] private int dailyWithdrawLimit = 50000;
        [SerializeField] private bool enableWithdrawCooldown = true;
        [SerializeField] private float withdrawCooldownHours = 24f;

        [Header("Upgrade Settings")]
        [SerializeField] private bool enableTreasuryUpgrades = true;
        [SerializeField] private int maxTreasuryLevel = 10;
        [SerializeField] private int baseStorageCapacity = 1000000;

        // Enums
        public enum TransactionType
        {
            Deposit,
            Withdrawal,
            Tax,
            Maintenance,
            Upgrade,
            Reward,
            Penalty,
            Transfer
        }

        public enum ResourceType
        {
            SoftCurrency,
            HardCurrency,
            Item,
            Material,
            Equipment
        }

        // Data structures
        [Serializable]
        public class GuildTreasury
        {
            public string guildId;
            public int softCurrency;
            public int hardCurrency;
            public Dictionary<string, int> items = new Dictionary<string, int>();
            public Dictionary<string, int> materials = new Dictionary<string, int>();
            public int treasuryLevel = 1;
            public int storageCapacity;
            public float taxRate = 0.05f;
            public List<Transaction> transactionHistory = new List<Transaction>();
            public DateTime lastMaintenance;
            public int totalDeposits;
            public int totalWithdrawals;
        }

        [Serializable]
        public class Transaction
        {
            public string transactionId;
            public TransactionType type;
            public ResourceType resourceType;
            public string resourceId;
            public int amount;
            public ulong playerId;
            public string playerName;
            public DateTime timestamp;
            public string notes;
        }

        [Serializable]
        public class MemberContribution
        {
            public ulong memberId;
            public string memberName;
            public int totalSoftCurrency;
            public int totalHardCurrency;
            public int totalItems;
            public DateTime lastContribution;
            public int contributionRank;
            public Dictionary<string, int> itemContributions = new Dictionary<string, int>();
        }

        [Serializable]
        public class WithdrawRequest
        {
            public string requestId;
            public string guildId;
            public ulong requesterId;
            public ResourceType resourceType;
            public string resourceId;
            public int amount;
            public DateTime requestTime;
            public bool approved;
            public bool processed;
            public ulong approvedBy;
            public string reason;
        }

        [Serializable]
        public class TreasuryUpgrade
        {
            public int level;
            public int softCurrencyCost;
            public int hardCurrencyCost;
            public int capacityIncrease;
            public float taxRateReduction;
            public Dictionary<string, object> benefits = new Dictionary<string, object>();
        }

        // State
        private Dictionary<string, GuildTreasury> guildTreasuries = new Dictionary<string, GuildTreasury>();
        private Dictionary<string, Dictionary<ulong, MemberContribution>> memberContributions = new Dictionary<string, Dictionary<ulong, MemberContribution>>();
        private Dictionary<string, List<WithdrawRequest>> pendingRequests = new Dictionary<string, List<WithdrawRequest>>();
        private Dictionary<ulong, DateTime> lastWithdrawTime = new Dictionary<ulong, DateTime>();
        private Dictionary<int, TreasuryUpgrade> treasuryUpgrades = new Dictionary<int, TreasuryUpgrade>();

        // Events
        public event Action<string, Transaction> OnTransaction;
        public event Action<string, int> OnTreasuryLevelUp;
        public event Action<ulong, int> OnContributionMade;
        public event Action<WithdrawRequest> OnWithdrawRequested;
        public event Action<WithdrawRequest> OnWithdrawApproved;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                InitializeTreasurySystem();
            }
        }

        #region Initialization

        private void InitializeTreasurySystem()
        {
            InitializeUpgrades();
        }

        private void InitializeUpgrades()
        {
            for (int i = 1; i <= maxTreasuryLevel; i++)
            {
                treasuryUpgrades[i] = new TreasuryUpgrade
                {
                    level = i,
                    softCurrencyCost = 10000 * i * i,
                    hardCurrencyCost = 100 * i,
                    capacityIncrease = 100000 * i,
                    taxRateReduction = 0.005f * i
                };
            }
        }

        public void InitializeGuildTreasury(string guildId)
        {
            if (guildTreasuries.ContainsKey(guildId)) return;

            var treasury = new GuildTreasury
            {
                guildId = guildId,
                treasuryLevel = 1,
                storageCapacity = baseStorageCapacity,
                taxRate = defaultTaxRate,
                lastMaintenance = DateTime.UtcNow
            };

            guildTreasuries[guildId] = treasury;
            memberContributions[guildId] = new Dictionary<ulong, MemberContribution>();

            Debug.Log($"Guild treasury initialized for {guildId}");
        }

        #endregion

        #region Deposits

        [ServerRpc(RequireOwnership = false)]
        public void DepositCurrencyServerRpc(ulong playerId, string guildId, int softCurrency, int hardCurrency)
        {
            if (!guildTreasuries.ContainsKey(guildId))
            {
                InitializeGuildTreasury(guildId);
            }

            var treasury = guildTreasuries[guildId];

            // Verify player has currency
            bool canAfford = true;

            if (softCurrency > 0)
            {
                if (!Economy.EconomyManager.Instance.SpendSoftCurrency(playerId, softCurrency))
                {
                    canAfford = false;
                }
            }

            if (hardCurrency > 0 && canAfford)
            {
                if (!Economy.EconomyManager.Instance.SpendHardCurrency(playerId, hardCurrency))
                {
                    // Refund soft currency if hard currency fails
                    if (softCurrency > 0)
                    {
                        Economy.EconomyManager.Instance.AddSoftCurrency(playerId, softCurrency);
                    }
                    canAfford = false;
                }
            }

            if (!canAfford)
            {
                Debug.LogWarning($"Player {playerId} cannot afford deposit");
                return;
            }

            // Add to treasury
            treasury.softCurrency += softCurrency;
            treasury.hardCurrency += hardCurrency;
            treasury.totalDeposits += softCurrency + hardCurrency;

            // Record transaction
            if (softCurrency > 0)
            {
                RecordTransaction(guildId, new Transaction
                {
                    transactionId = Guid.NewGuid().ToString(),
                    type = TransactionType.Deposit,
                    resourceType = ResourceType.SoftCurrency,
                    amount = softCurrency,
                    playerId = playerId,
                    playerName = GetPlayerName(playerId),
                    timestamp = DateTime.UtcNow,
                    notes = "Currency deposit"
                });
            }

            if (hardCurrency > 0)
            {
                RecordTransaction(guildId, new Transaction
                {
                    transactionId = Guid.NewGuid().ToString(),
                    type = TransactionType.Deposit,
                    resourceType = ResourceType.HardCurrency,
                    amount = hardCurrency,
                    playerId = playerId,
                    playerName = GetPlayerName(playerId),
                    timestamp = DateTime.UtcNow,
                    notes = "Premium currency deposit"
                });
            }

            // Update contributions
            UpdateMemberContribution(guildId, playerId, softCurrency, hardCurrency, 0);

            OnContributionMade?.Invoke(playerId, softCurrency + hardCurrency);

            Debug.Log($"Player {playerId} deposited {softCurrency} soft + {hardCurrency} hard currency to guild {guildId}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void DepositItemServerRpc(ulong playerId, string guildId, string itemId, int quantity)
        {
            if (!guildTreasuries.ContainsKey(guildId))
            {
                InitializeGuildTreasury(guildId);
            }

            var treasury = guildTreasuries[guildId];

            // Verify player has item
            if (!Inventory.InventoryManager.Instance.RemoveItem(playerId, itemId, quantity))
            {
                Debug.LogWarning($"Player {playerId} doesn't have item {itemId}");
                return;
            }

            // Add to treasury
            if (!treasury.items.ContainsKey(itemId))
            {
                treasury.items[itemId] = 0;
            }

            treasury.items[itemId] += quantity;

            // Record transaction
            RecordTransaction(guildId, new Transaction
            {
                transactionId = Guid.NewGuid().ToString(),
                type = TransactionType.Deposit,
                resourceType = ResourceType.Item,
                resourceId = itemId,
                amount = quantity,
                playerId = playerId,
                playerName = GetPlayerName(playerId),
                timestamp = DateTime.UtcNow,
                notes = $"Item deposit: {itemId}"
            });

            // Update contributions
            UpdateMemberContribution(guildId, playerId, 0, 0, quantity);

            Debug.Log($"Player {playerId} deposited {quantity}x {itemId} to guild {guildId}");
        }

        #endregion

        #region Withdrawals

        [ServerRpc(RequireOwnership = false)]
        public void RequestWithdrawalServerRpc(ulong playerId, string guildId, ResourceType resourceType, string resourceId, int amount, string reason)
        {
            if (!requireWithdrawPermission || HasWithdrawPermission(playerId, guildId))
            {
                // Auto-approve if player has permission
                ProcessWithdrawal(playerId, guildId, resourceType, resourceId, amount);
            }
            else
            {
                // Create withdrawal request
                var request = new WithdrawRequest
                {
                    requestId = Guid.NewGuid().ToString(),
                    guildId = guildId,
                    requesterId = playerId,
                    resourceType = resourceType,
                    resourceId = resourceId,
                    amount = amount,
                    requestTime = DateTime.UtcNow,
                    reason = reason
                };

                if (!pendingRequests.ContainsKey(guildId))
                {
                    pendingRequests[guildId] = new List<WithdrawRequest>();
                }

                pendingRequests[guildId].Add(request);

                OnWithdrawRequested?.Invoke(request);

                Debug.Log($"Withdrawal request created for player {playerId} in guild {guildId}");
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void ApproveWithdrawalServerRpc(ulong approverId, string requestId, string guildId)
        {
            if (!HasWithdrawPermission(approverId, guildId))
            {
                Debug.LogWarning($"Player {approverId} doesn't have permission to approve withdrawals");
                return;
            }

            if (!pendingRequests.ContainsKey(guildId)) return;

            var request = pendingRequests[guildId].FirstOrDefault(r => r.requestId == requestId);
            if (request == null) return;

            request.approved = true;
            request.approvedBy = approverId;

            ProcessWithdrawal(request.requesterId, guildId, request.resourceType, request.resourceId, request.amount);

            pendingRequests[guildId].Remove(request);

            OnWithdrawApproved?.Invoke(request);
        }

        private void ProcessWithdrawal(ulong playerId, string guildId, ResourceType resourceType, string resourceId, int amount)
        {
            if (!guildTreasuries.ContainsKey(guildId)) return;

            var treasury = guildTreasuries[guildId];

            // Check withdraw cooldown
            if (enableWithdrawCooldown && lastWithdrawTime.ContainsKey(playerId))
            {
                if ((DateTime.UtcNow - lastWithdrawTime[playerId]).TotalHours < withdrawCooldownHours)
                {
                    Debug.LogWarning($"Player {playerId} is on withdraw cooldown");
                    return;
                }
            }

            bool success = false;

            switch (resourceType)
            {
                case ResourceType.SoftCurrency:
                    if (treasury.softCurrency >= amount)
                    {
                        treasury.softCurrency -= amount;
                        Economy.EconomyManager.Instance.AddSoftCurrency(playerId, amount);
                        success = true;
                    }
                    break;

                case ResourceType.HardCurrency:
                    if (treasury.hardCurrency >= amount)
                    {
                        treasury.hardCurrency -= amount;
                        Economy.EconomyManager.Instance.AddHardCurrency(playerId, amount);
                        success = true;
                    }
                    break;

                case ResourceType.Item:
                    if (treasury.items.ContainsKey(resourceId) && treasury.items[resourceId] >= amount)
                    {
                        treasury.items[resourceId] -= amount;
                        Inventory.InventoryManager.Instance.AddItem(playerId, resourceId, amount);
                        success = true;
                    }
                    break;
            }

            if (success)
            {
                treasury.totalWithdrawals += amount;

                RecordTransaction(guildId, new Transaction
                {
                    transactionId = Guid.NewGuid().ToString(),
                    type = TransactionType.Withdrawal,
                    resourceType = resourceType,
                    resourceId = resourceId,
                    amount = amount,
                    playerId = playerId,
                    playerName = GetPlayerName(playerId),
                    timestamp = DateTime.UtcNow,
                    notes = "Withdrawal"
                });

                lastWithdrawTime[playerId] = DateTime.UtcNow;

                Debug.Log($"Player {playerId} withdrew {amount} {resourceType} from guild {guildId}");
            }
        }

        #endregion

        #region Treasury Upgrades

        [ServerRpc(RequireOwnership = false)]
        public void UpgradeTreasuryServerRpc(ulong requesterId, string guildId)
        {
            if (!guildTreasuries.ContainsKey(guildId)) return;

            var treasury = guildTreasuries[guildId];

            if (treasury.treasuryLevel >= maxTreasuryLevel)
            {
                Debug.LogWarning("Treasury already at max level");
                return;
            }

            int nextLevel = treasury.treasuryLevel + 1;
            var upgrade = treasuryUpgrades[nextLevel];

            // Check if treasury has enough currency
            if (treasury.softCurrency < upgrade.softCurrencyCost || treasury.hardCurrency < upgrade.hardCurrencyCost)
            {
                Debug.LogWarning("Insufficient currency for upgrade");
                return;
            }

            // Deduct costs
            treasury.softCurrency -= upgrade.softCurrencyCost;
            treasury.hardCurrency -= upgrade.hardCurrencyCost;

            // Apply upgrade
            treasury.treasuryLevel = nextLevel;
            treasury.storageCapacity += upgrade.capacityIncrease;
            treasury.taxRate = Mathf.Max(0, treasury.taxRate - upgrade.taxRateReduction);

            RecordTransaction(guildId, new Transaction
            {
                transactionId = Guid.NewGuid().ToString(),
                type = TransactionType.Upgrade,
                resourceType = ResourceType.SoftCurrency,
                amount = upgrade.softCurrencyCost + upgrade.hardCurrencyCost,
                playerId = requesterId,
                playerName = GetPlayerName(requesterId),
                timestamp = DateTime.UtcNow,
                notes = $"Treasury upgraded to level {nextLevel}"
            });

            OnTreasuryLevelUp?.Invoke(guildId, nextLevel);

            Debug.Log($"Guild {guildId} treasury upgraded to level {nextLevel}");
        }

        #endregion

        #region Tax Collection

        public void CollectTax(string guildId, ulong playerId, int earnings)
        {
            if (!enableTaxSystem || !guildTreasuries.ContainsKey(guildId)) return;

            var treasury = guildTreasuries[guildId];
            int taxAmount = Mathf.RoundToInt(earnings * treasury.taxRate);

            if (taxAmount > 0)
            {
                treasury.softCurrency += taxAmount;

                RecordTransaction(guildId, new Transaction
                {
                    transactionId = Guid.NewGuid().ToString(),
                    type = TransactionType.Tax,
                    resourceType = ResourceType.SoftCurrency,
                    amount = taxAmount,
                    playerId = playerId,
                    playerName = GetPlayerName(playerId),
                    timestamp = DateTime.UtcNow,
                    notes = $"Tax from earnings ({treasury.taxRate * 100}%)"
                });
            }
        }

        #endregion

        #region Contributions Tracking

        private void UpdateMemberContribution(string guildId, ulong memberId, int softCurrency, int hardCurrency, int items)
        {
            if (!memberContributions.ContainsKey(guildId))
            {
                memberContributions[guildId] = new Dictionary<ulong, MemberContribution>();
            }

            if (!memberContributions[guildId].ContainsKey(memberId))
            {
                memberContributions[guildId][memberId] = new MemberContribution
                {
                    memberId = memberId,
                    memberName = GetPlayerName(memberId)
                };
            }

            var contribution = memberContributions[guildId][memberId];
            contribution.totalSoftCurrency += softCurrency;
            contribution.totalHardCurrency += hardCurrency;
            contribution.totalItems += items;
            contribution.lastContribution = DateTime.UtcNow;

            // Update rankings
            UpdateContributionRankings(guildId);
        }

        private void UpdateContributionRankings(string guildId)
        {
            if (!memberContributions.ContainsKey(guildId)) return;

            var ranked = memberContributions[guildId].Values
                .OrderByDescending(c => c.totalSoftCurrency + c.totalHardCurrency * 10 + c.totalItems)
                .ToList();

            for (int i = 0; i < ranked.Count; i++)
            {
                ranked[i].contributionRank = i + 1;
            }
        }

        public List<MemberContribution> GetTopContributors(string guildId, int count = 10)
        {
            if (!memberContributions.ContainsKey(guildId)) return new List<MemberContribution>();

            return memberContributions[guildId].Values
                .OrderBy(c => c.contributionRank)
                .Take(count)
                .ToList();
        }

        #endregion

        #region Transaction History

        private void RecordTransaction(string guildId, Transaction transaction)
        {
            if (!guildTreasuries.ContainsKey(guildId)) return;

            var treasury = guildTreasuries[guildId];
            treasury.transactionHistory.Add(transaction);

            // Keep last 500 transactions
            if (treasury.transactionHistory.Count > 500)
            {
                treasury.transactionHistory.RemoveAt(0);
            }

            OnTransaction?.Invoke(guildId, transaction);
        }

        public List<Transaction> GetTransactionHistory(string guildId, int limit = 50)
        {
            if (!guildTreasuries.ContainsKey(guildId)) return new List<Transaction>();

            return guildTreasuries[guildId].transactionHistory
                .OrderByDescending(t => t.timestamp)
                .Take(limit)
                .ToList();
        }

        public List<Transaction> GetPlayerTransactions(string guildId, ulong playerId)
        {
            if (!guildTreasuries.ContainsKey(guildId)) return new List<Transaction>();

            return guildTreasuries[guildId].transactionHistory
                .Where(t => t.playerId == playerId)
                .OrderByDescending(t => t.timestamp)
                .ToList();
        }

        #endregion

        #region Permissions

        private bool HasWithdrawPermission(ulong playerId, string guildId)
        {
            // Integration with clan system
            if (ClanSystem.Instance != null)
            {
                return ClanSystem.Instance.HasPermission(playerId, guildId, ClanSystem.ClanPermission.ManageTreasury);
            }

            return false;
        }

        #endregion

        #region Utility

        private string GetPlayerName(ulong playerId)
        {
            return $"Player_{playerId}";
        }

        public GuildTreasury GetTreasury(string guildId)
        {
            return guildTreasuries.ContainsKey(guildId) ? guildTreasuries[guildId] : null;
        }

        public MemberContribution GetMemberContribution(string guildId, ulong memberId)
        {
            if (!memberContributions.ContainsKey(guildId)) return null;
            return memberContributions[guildId].ContainsKey(memberId) ? memberContributions[guildId][memberId] : null;
        }

        #endregion
    }
}
