using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace DeadFrontier.Contracts
{
    /// <summary>
    /// Comprehensive contract and bounty system for mission-based gameplay.
    /// Provides daily/weekly contracts, bounty hunting, and special limited-time missions.
    /// Includes player-placed bounties and NPC contract vendors.
    /// </summary>
    public class ContractSystem : MonoBehaviour
    {
        public static ContractSystem Instance { get; private set; }

        [Header("Contract Settings")]
        [SerializeField] private ContractData[] availableContracts;
        [SerializeField] private int maxActiveContracts = 10;
        [SerializeField] private int maxDailyContracts = 3;
        [SerializeField] private int maxWeeklyContracts = 2;
        [SerializeField] private float dailyContractResetTime = 24f; // Hours
        [SerializeField] private float weeklyContractResetTime = 168f; // 7 days

        [Header("Bounty Settings")]
        [SerializeField] private bool enablePlayerBounties = true;
        [SerializeField] private int minBountyAmount = 1000;
        [SerializeField] private int maxBountyAmount = 100000;
        [SerializeField] private float bountyExpirationTime = 168f; // 7 days
        [SerializeField] private float bountyClaimFee = 0.1f; // 10% fee

        [Header("Contract Vendors")]
        [SerializeField] private ContractVendor[] contractVendors;

        [Header("Reward Scaling")]
        [SerializeField] private float easyContractMultiplier = 1f;
        [SerializeField] private float normalContractMultiplier = 1.5f;
        [SerializeField] private float hardContractMultiplier = 2.5f;
        [SerializeField] private float extremeContractMultiplier = 4f;

        // Player contracts
        private Dictionary<ulong, PlayerContracts> playerActiveContracts = new Dictionary<ulong, PlayerContracts>();
        private Dictionary<ulong, List<string>> playerCompletedContracts = new Dictionary<ulong, List<string>>();

        // Bounties
        private Dictionary<string, Bounty> activeBounties = new Dictionary<string, Bounty>();
        private Dictionary<ulong, List<string>> playerBounties = new Dictionary<ulong, List<string>>();

        // Daily/Weekly contracts
        private List<ContractData> currentDailyContracts = new List<ContractData>();
        private List<ContractData> currentWeeklyContracts = new List<ContractData>();
        private DateTime lastDailyReset;
        private DateTime lastWeeklyReset;

        // Events
        public event Action<ulong, ContractData> OnContractAccepted;
        public event Action<ulong, ContractData, float> OnContractProgress;
        public event Action<ulong, ContractData> OnContractCompleted;
        public event Action<ulong, ContractData> OnContractFailed;
        public event Action<ulong, ContractData> OnContractAbandoned;
        public event Action<Bounty> OnBountyCreated;
        public event Action<Bounty, ulong> OnBountyClaimed;
        public event Action<Bounty> OnBountyExpired;

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
            InitializeContracts();
            LoadAllPlayerData();
        }

        private void Update()
        {
            UpdateContractTimers();
            UpdateBountyExpiration();
        }

        #region Initialization

        private void InitializeContracts()
        {
            RefreshDailyContracts();
            RefreshWeeklyContracts();

            Debug.Log($"[ContractSystem] Initialized with {availableContracts.Length} available contracts");
        }

        #endregion

        #region Contract Management

        public bool CanAcceptContract(ulong playerId, string contractId)
        {
            if (!playerActiveContracts.ContainsKey(playerId))
            {
                InitializePlayerContracts(playerId);
            }

            var playerContracts = playerActiveContracts[playerId];

            // Check if at max contracts
            if (playerContracts.activeContracts.Count >= maxActiveContracts) return false;

            // Check if already has this contract
            if (playerContracts.activeContracts.Any(c => c.contractData.contractId == contractId)) return false;

            // Check if already completed (if not repeatable)
            var contractData = GetContractById(contractId);
            if (contractData == null) return false;

            if (!contractData.isRepeatable && HasCompletedContract(playerId, contractId)) return false;

            // Check level requirement
            if (Progression.AchievementManager.Instance != null)
            {
                int playerLevel = Progression.AchievementManager.Instance.GetPlayerLevel();
                if (playerLevel < contractData.requiredLevel) return false;
            }

            // Check reputation requirement
            if (!string.IsNullOrEmpty(contractData.requiredVendor))
            {
                if (NPCs.NPCTraderSystem.Instance != null)
                {
                    var reputationTier = NPCs.NPCTraderSystem.Instance.GetReputationTier(contractData.requiredVendor);
                    // Could check minimum reputation tier here
                }
            }

            return true;
        }

        public bool AcceptContract(ulong playerId, string contractId)
        {
            if (!CanAcceptContract(playerId, contractId)) return false;

            var contractData = GetContractById(contractId);
            if (contractData == null) return false;

            var playerContracts = playerActiveContracts[playerId];

            var activeContract = new ActiveContract
            {
                contractData = contractData,
                startTime = DateTime.UtcNow,
                expirationTime = contractData.hasTimeLimit
                    ? DateTime.UtcNow.AddSeconds(contractData.timeLimitSeconds)
                    : DateTime.MaxValue,
                currentProgress = new Dictionary<string, float>()
            };

            // Initialize objective progress
            foreach (var objective in contractData.objectives)
            {
                activeContract.currentProgress[objective.objectiveId] = 0f;
            }

            playerContracts.activeContracts.Add(activeContract);

            OnContractAccepted?.Invoke(playerId, contractData);

            SavePlayerContracts(playerId);

            Debug.Log($"[ContractSystem] Player {playerId} accepted contract: {contractData.contractName}");

            return true;
        }

        public void AbandonContract(ulong playerId, string contractId)
        {
            if (!playerActiveContracts.ContainsKey(playerId)) return;

            var playerContracts = playerActiveContracts[playerId];
            var contract = playerContracts.activeContracts.FirstOrDefault(c => c.contractData.contractId == contractId);

            if (contract == null) return;

            playerContracts.activeContracts.Remove(contract);

            OnContractAbandoned?.Invoke(playerId, contract.contractData);

            SavePlayerContracts(playerId);

            Debug.Log($"[ContractSystem] Player {playerId} abandoned contract: {contract.contractData.contractName}");
        }

        private void InitializePlayerContracts(ulong playerId)
        {
            if (playerActiveContracts.ContainsKey(playerId)) return;

            playerActiveContracts[playerId] = new PlayerContracts
            {
                activeContracts = new List<ActiveContract>()
            };

            if (!playerCompletedContracts.ContainsKey(playerId))
            {
                playerCompletedContracts[playerId] = new List<string>();
            }
        }

        #endregion

        #region Contract Progress

        public void UpdateContractProgress(ulong playerId, ContractObjectiveType objectiveType, string targetId, float amount)
        {
            if (!playerActiveContracts.ContainsKey(playerId)) return;

            var playerContracts = playerActiveContracts[playerId];

            foreach (var contract in playerContracts.activeContracts)
            {
                foreach (var objective in contract.contractData.objectives)
                {
                    if (objective.objectiveType == objectiveType)
                    {
                        // Check if target matches
                        bool targetMatches = string.IsNullOrEmpty(objective.targetId) || objective.targetId == targetId;

                        if (targetMatches)
                        {
                            if (!contract.currentProgress.ContainsKey(objective.objectiveId))
                            {
                                contract.currentProgress[objective.objectiveId] = 0f;
                            }

                            float currentProgress = contract.currentProgress[objective.objectiveId];
                            float newProgress = Mathf.Min(currentProgress + amount, objective.requiredAmount);

                            contract.currentProgress[objective.objectiveId] = newProgress;

                            float progressPercent = newProgress / objective.requiredAmount;
                            OnContractProgress?.Invoke(playerId, contract.contractData, progressPercent);
                        }
                    }
                }
            }

            SavePlayerContracts(playerId);
        }

        private void UpdateContractTimers()
        {
            foreach (var kvp in playerActiveContracts)
            {
                var playerId = kvp.Key;
                var playerContracts = kvp.Value;

                var contractsToComplete = new List<ActiveContract>();
                var contractsToFail = new List<ActiveContract>();

                foreach (var contract in playerContracts.activeContracts)
                {
                    // Check completion
                    if (IsContractCompleted(contract))
                    {
                        contractsToComplete.Add(contract);
                    }
                    // Check expiration
                    else if (contract.contractData.hasTimeLimit && DateTime.UtcNow >= contract.expirationTime)
                    {
                        contractsToFail.Add(contract);
                    }
                }

                // Complete contracts
                foreach (var contract in contractsToComplete)
                {
                    CompleteContract(playerId, contract);
                }

                // Fail contracts
                foreach (var contract in contractsToFail)
                {
                    FailContract(playerId, contract);
                }
            }

            // Check daily/weekly resets
            CheckDailyReset();
            CheckWeeklyReset();
        }

        private bool IsContractCompleted(ActiveContract contract)
        {
            foreach (var objective in contract.contractData.objectives)
            {
                if (!contract.currentProgress.ContainsKey(objective.objectiveId))
                    return false;

                if (contract.currentProgress[objective.objectiveId] < objective.requiredAmount)
                    return false;
            }

            return true;
        }

        private void CompleteContract(ulong playerId, ActiveContract contract)
        {
            if (!playerActiveContracts.ContainsKey(playerId)) return;

            var playerContracts = playerActiveContracts[playerId];

            // Award rewards
            AwardContractRewards(playerId, contract.contractData);

            // Mark as completed
            if (!playerCompletedContracts.ContainsKey(playerId))
            {
                playerCompletedContracts[playerId] = new List<string>();
            }

            playerCompletedContracts[playerId].Add(contract.contractData.contractId);

            // Remove from active
            playerContracts.activeContracts.Remove(contract);

            OnContractCompleted?.Invoke(playerId, contract.contractData);

            SavePlayerContracts(playerId);

            Debug.Log($"[ContractSystem] Player {playerId} completed contract: {contract.contractData.contractName}");
        }

        private void FailContract(ulong playerId, ActiveContract contract)
        {
            if (!playerActiveContracts.ContainsKey(playerId)) return;

            var playerContracts = playerActiveContracts[playerId];

            playerContracts.activeContracts.Remove(contract);

            OnContractFailed?.Invoke(playerId, contract.contractData);

            SavePlayerContracts(playerId);

            Debug.Log($"[ContractSystem] Player {playerId} failed contract: {contract.contractData.contractName}");
        }

        #endregion

        #region Contract Rewards

        private void AwardContractRewards(ulong playerId, ContractData contract)
        {
            // Get difficulty multiplier
            float difficultyMultiplier = GetDifficultyMultiplier(contract.difficulty);

            // Award XP
            if (contract.xpReward > 0 && Progression.AchievementManager.Instance != null)
            {
                int xp = Mathf.RoundToInt(contract.xpReward * difficultyMultiplier);
                Progression.AchievementManager.Instance.AddExperience(playerId, xp, $"Contract: {contract.contractName}");
            }

            // Award currency
            if (contract.softCurrencyReward > 0 && Economy.EconomyManager.Instance != null)
            {
                int currency = Mathf.RoundToInt(contract.softCurrencyReward * difficultyMultiplier);
                Economy.EconomyManager.Instance.EarnSoftCurrency(currency, $"Contract: {contract.contractName}");
            }

            if (contract.hardCurrencyReward > 0 && Economy.EconomyManager.Instance != null)
            {
                int hardCurrency = Mathf.RoundToInt(contract.hardCurrencyReward * difficultyMultiplier);
                Economy.EconomyManager.Instance.AddHardCurrency(hardCurrency, $"Contract: {contract.contractName}");
            }

            // Award items
            if (contract.itemRewards != null && Gameplay.InventoryManager.Instance != null)
            {
                foreach (var itemReward in contract.itemRewards)
                {
                    var itemData = new Gameplay.LootItemData
                    {
                        itemId = itemReward.itemId,
                        itemName = itemReward.itemName
                    };

                    Gameplay.InventoryManager.Instance.AddItem(playerId, itemData, itemReward.quantity);
                }
            }

            // Award reputation
            if (!string.IsNullOrEmpty(contract.requiredVendor) && contract.reputationReward > 0)
            {
                if (NPCs.NPCTraderSystem.Instance != null)
                {
                    NPCs.NPCTraderSystem.Instance.ModifyReputation(contract.requiredVendor, contract.reputationReward);
                }
            }
        }

        private float GetDifficultyMultiplier(ContractDifficulty difficulty)
        {
            switch (difficulty)
            {
                case ContractDifficulty.Easy: return easyContractMultiplier;
                case ContractDifficulty.Normal: return normalContractMultiplier;
                case ContractDifficulty.Hard: return hardContractMultiplier;
                case ContractDifficulty.Extreme: return extremeContractMultiplier;
                default: return 1f;
            }
        }

        #endregion

        #region Daily/Weekly Contracts

        private void RefreshDailyContracts()
        {
            currentDailyContracts.Clear();

            var dailyCandidates = availableContracts.Where(c => c.contractType == ContractType.Daily).ToList();

            int contractsToSelect = Mathf.Min(maxDailyContracts, dailyCandidates.Count);

            for (int i = 0; i < contractsToSelect; i++)
            {
                if (dailyCandidates.Count == 0) break;

                int randomIndex = UnityEngine.Random.Range(0, dailyCandidates.Count);
                currentDailyContracts.Add(dailyCandidates[randomIndex]);
                dailyCandidates.RemoveAt(randomIndex);
            }

            lastDailyReset = DateTime.UtcNow;

            Debug.Log($"[ContractSystem] Refreshed {currentDailyContracts.Count} daily contracts");
        }

        private void RefreshWeeklyContracts()
        {
            currentWeeklyContracts.Clear();

            var weeklyCandidates = availableContracts.Where(c => c.contractType == ContractType.Weekly).ToList();

            int contractsToSelect = Mathf.Min(maxWeeklyContracts, weeklyCandidates.Count);

            for (int i = 0; i < contractsToSelect; i++)
            {
                if (weeklyCandidates.Count == 0) break;

                int randomIndex = UnityEngine.Random.Range(0, weeklyCandidates.Count);
                currentWeeklyContracts.Add(weeklyCandidates[randomIndex]);
                weeklyCandidates.RemoveAt(randomIndex);
            }

            lastWeeklyReset = DateTime.UtcNow;

            Debug.Log($"[ContractSystem] Refreshed {currentWeeklyContracts.Count} weekly contracts");
        }

        private void CheckDailyReset()
        {
            TimeSpan timeSinceReset = DateTime.UtcNow - lastDailyReset;

            if (timeSinceReset.TotalHours >= dailyContractResetTime)
            {
                RefreshDailyContracts();
            }
        }

        private void CheckWeeklyReset()
        {
            TimeSpan timeSinceReset = DateTime.UtcNow - lastWeeklyReset;

            if (timeSinceReset.TotalHours >= weeklyContractResetTime)
            {
                RefreshWeeklyContracts();
            }
        }

        #endregion

        #region Bounty System

        public bool CreateBounty(ulong targetPlayerId, ulong creatorId, int amount, string reason)
        {
            if (!enablePlayerBounties) return false;

            // Validate amount
            if (amount < minBountyAmount || amount > maxBountyAmount) return false;

            // Check if creator has funds
            if (Economy.EconomyManager.Instance != null)
            {
                int totalCost = Mathf.RoundToInt(amount * (1f + bountyClaimFee));

                if (!Economy.EconomyManager.Instance.CanAfford(totalCost))
                    return false;

                if (!Economy.EconomyManager.Instance.SpendSoftCurrency(totalCost, "Place Bounty"))
                    return false;
            }

            string bountyId = $"bounty_{targetPlayerId}_{DateTime.UtcNow.Ticks}";

            var bounty = new Bounty
            {
                bountyId = bountyId,
                targetPlayerId = targetPlayerId,
                creatorId = creatorId,
                amount = amount,
                reason = reason,
                creationTime = DateTime.UtcNow,
                expirationTime = DateTime.UtcNow.AddHours(bountyExpirationTime),
                isActive = true
            };

            activeBounties[bountyId] = bounty;

            // Track player bounties
            if (!playerBounties.ContainsKey(targetPlayerId))
            {
                playerBounties[targetPlayerId] = new List<string>();
            }

            playerBounties[targetPlayerId].Add(bountyId);

            OnBountyCreated?.Invoke(bounty);

            Debug.Log($"[ContractSystem] Bounty created on player {targetPlayerId} for {amount} by {creatorId}");

            return true;
        }

        public bool ClaimBounty(string bountyId, ulong claimerId)
        {
            if (!activeBounties.ContainsKey(bountyId)) return false;

            var bounty = activeBounties[bountyId];

            if (!bounty.isActive) return false;

            // Award bounty to claimer
            if (Economy.EconomyManager.Instance != null)
            {
                Economy.EconomyManager.Instance.EarnSoftCurrency(bounty.amount, $"Bounty Claim: {bountyId}");
            }

            bounty.isActive = false;
            bounty.claimerId = claimerId;
            bounty.claimTime = DateTime.UtcNow;

            OnBountyClaimed?.Invoke(bounty, claimerId);

            // Remove from active bounties
            activeBounties.Remove(bountyId);

            if (playerBounties.ContainsKey(bounty.targetPlayerId))
            {
                playerBounties[bounty.targetPlayerId].Remove(bountyId);
            }

            Debug.Log($"[ContractSystem] Bounty {bountyId} claimed by player {claimerId}");

            return true;
        }

        private void UpdateBountyExpiration()
        {
            var expiredBounties = new List<string>();

            foreach (var kvp in activeBounties)
            {
                var bounty = kvp.Value;

                if (DateTime.UtcNow >= bounty.expirationTime)
                {
                    expiredBounties.Add(kvp.Key);
                }
            }

            foreach (var bountyId in expiredBounties)
            {
                var bounty = activeBounties[bountyId];

                // Refund creator
                if (Economy.EconomyManager.Instance != null)
                {
                    Economy.EconomyManager.Instance.EarnSoftCurrency(bounty.amount, $"Bounty Expired: {bountyId}");
                }

                OnBountyExpired?.Invoke(bounty);

                activeBounties.Remove(bountyId);

                if (playerBounties.ContainsKey(bounty.targetPlayerId))
                {
                    playerBounties[bounty.targetPlayerId].Remove(bountyId);
                }

                Debug.Log($"[ContractSystem] Bounty {bountyId} expired");
            }
        }

        public int GetTotalBountyOnPlayer(ulong playerId)
        {
            if (!playerBounties.ContainsKey(playerId)) return 0;

            int totalBounty = 0;

            foreach (var bountyId in playerBounties[playerId])
            {
                if (activeBounties.ContainsKey(bountyId))
                {
                    totalBounty += activeBounties[bountyId].amount;
                }
            }

            return totalBounty;
        }

        #endregion

        #region Persistence

        private void LoadAllPlayerData()
        {
            Debug.Log("[ContractSystem] Ready to load player contract data");
        }

        public void LoadPlayerContracts(ulong playerId)
        {
            if (Core.SaveSystem.Instance == null) return;

            string savedData = Core.SaveSystem.Instance.LoadData($"contracts_{playerId}");
            if (!string.IsNullOrEmpty(savedData))
            {
                try
                {
                    var contractSave = JsonUtility.FromJson<ContractSaveData>(savedData);

                    var playerContracts = new PlayerContracts
                    {
                        activeContracts = new List<ActiveContract>()
                    };

                    foreach (var activeContractSave in contractSave.activeContracts)
                    {
                        var contractData = GetContractById(activeContractSave.contractId);
                        if (contractData != null)
                        {
                            var activeContract = new ActiveContract
                            {
                                contractData = contractData,
                                startTime = DateTime.Parse(activeContractSave.startTime),
                                expirationTime = DateTime.Parse(activeContractSave.expirationTime),
                                currentProgress = activeContractSave.progress.ToDictionary(p => p.objectiveId, p => p.progress)
                            };

                            playerContracts.activeContracts.Add(activeContract);
                        }
                    }

                    playerActiveContracts[playerId] = playerContracts;
                    playerCompletedContracts[playerId] = new List<string>(contractSave.completedContractIds);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ContractSystem] Error loading contracts for player {playerId}: {e.Message}");
                    InitializePlayerContracts(playerId);
                }
            }
            else
            {
                InitializePlayerContracts(playerId);
            }
        }

        private void SavePlayerContracts(ulong playerId)
        {
            if (Core.SaveSystem.Instance == null) return;
            if (!playerActiveContracts.ContainsKey(playerId)) return;

            var playerContracts = playerActiveContracts[playerId];

            var contractSave = new ContractSaveData
            {
                activeContracts = new List<ActiveContractSaveData>(),
                completedContractIds = playerCompletedContracts.ContainsKey(playerId)
                    ? new List<string>(playerCompletedContracts[playerId])
                    : new List<string>()
            };

            foreach (var contract in playerContracts.activeContracts)
            {
                var saveData = new ActiveContractSaveData
                {
                    contractId = contract.contractData.contractId,
                    startTime = contract.startTime.ToString(),
                    expirationTime = contract.expirationTime.ToString(),
                    progress = contract.currentProgress.Select(p => new ContractObjectiveProgress
                    {
                        objectiveId = p.Key,
                        progress = p.Value
                    }).ToList()
                };

                contractSave.activeContracts.Add(saveData);
            }

            string json = JsonUtility.ToJson(contractSave);
            Core.SaveSystem.Instance.SaveData($"contracts_{playerId}", json);
        }

        #endregion

        #region Public Getters

        public List<ContractData> GetAvailableContracts(ulong playerId)
        {
            return availableContracts.Where(c => CanAcceptContract(playerId, c.contractId)).ToList();
        }

        public List<ActiveContract> GetActiveContracts(ulong playerId)
        {
            if (!playerActiveContracts.ContainsKey(playerId)) return new List<ActiveContract>();

            return new List<ActiveContract>(playerActiveContracts[playerId].activeContracts);
        }

        public List<ContractData> GetDailyContracts() => new List<ContractData>(currentDailyContracts);

        public List<ContractData> GetWeeklyContracts() => new List<ContractData>(currentWeeklyContracts);

        public bool HasCompletedContract(ulong playerId, string contractId)
        {
            if (!playerCompletedContracts.ContainsKey(playerId)) return false;

            return playerCompletedContracts[playerId].Contains(contractId);
        }

        public ContractData GetContractById(string contractId)
        {
            return availableContracts.FirstOrDefault(c => c.contractId == contractId);
        }

        public List<Bounty> GetActiveBounties()
        {
            return new List<Bounty>(activeBounties.Values);
        }

        public List<Bounty> GetBountiesOnPlayer(ulong playerId)
        {
            if (!playerBounties.ContainsKey(playerId)) return new List<Bounty>();

            return playerBounties[playerId]
                .Where(id => activeBounties.ContainsKey(id))
                .Select(id => activeBounties[id])
                .ToList();
        }

        public TimeSpan GetContractTimeRemaining(ActiveContract contract)
        {
            if (!contract.contractData.hasTimeLimit) return TimeSpan.MaxValue;

            TimeSpan remaining = contract.expirationTime - DateTime.UtcNow;
            return remaining.TotalSeconds > 0 ? remaining : TimeSpan.Zero;
        }

        public TimeSpan GetDailyResetTime()
        {
            TimeSpan timeSinceReset = DateTime.UtcNow - lastDailyReset;
            TimeSpan resetInterval = TimeSpan.FromHours(dailyContractResetTime);

            TimeSpan remaining = resetInterval - timeSinceReset;
            return remaining.TotalSeconds > 0 ? remaining : TimeSpan.Zero;
        }

        public TimeSpan GetWeeklyResetTime()
        {
            TimeSpan timeSinceReset = DateTime.UtcNow - lastWeeklyReset;
            TimeSpan resetInterval = TimeSpan.FromHours(weeklyContractResetTime);

            TimeSpan remaining = resetInterval - timeSinceReset;
            return remaining.TotalSeconds > 0 ? remaining : TimeSpan.Zero;
        }

        #endregion
    }

    #region Data Classes

    public class PlayerContracts
    {
        public List<ActiveContract> activeContracts;
    }

    public class ActiveContract
    {
        public ContractData contractData;
        public DateTime startTime;
        public DateTime expirationTime;
        public Dictionary<string, float> currentProgress;
    }

    [System.Serializable]
    public class ContractData
    {
        public string contractId;
        public string contractName;
        [TextArea(3, 5)]
        public string description;

        public ContractType contractType;
        public ContractDifficulty difficulty;
        public int requiredLevel = 1;
        public string requiredVendor; // Required vendor for contract

        public ContractObjective[] objectives;

        public bool isRepeatable;
        public bool hasTimeLimit;
        public float timeLimitSeconds = 3600f;

        // Rewards
        public int xpReward;
        public int softCurrencyReward;
        public int hardCurrencyReward;
        public int reputationReward;
        public ContractItemReward[] itemRewards;

        public Sprite contractIcon;
    }

    [System.Serializable]
    public class ContractObjective
    {
        public string objectiveId;
        public string description;
        public ContractObjectiveType objectiveType;
        public string targetId;
        public float requiredAmount = 1f;
    }

    [System.Serializable]
    public class ContractItemReward
    {
        public string itemId;
        public string itemName;
        public int quantity = 1;
    }

    [System.Serializable]
    public class ContractVendor
    {
        public string vendorId;
        public string vendorName;
        public ContractType[] offersContractTypes;
        public Sprite vendorIcon;
    }

    public class Bounty
    {
        public string bountyId;
        public ulong targetPlayerId;
        public ulong creatorId;
        public int amount;
        public string reason;
        public DateTime creationTime;
        public DateTime expirationTime;
        public bool isActive;
        public ulong? claimerId;
        public DateTime? claimTime;
    }

    [System.Serializable]
    public class ContractSaveData
    {
        public List<ActiveContractSaveData> activeContracts;
        public List<string> completedContractIds;
    }

    [System.Serializable]
    public class ActiveContractSaveData
    {
        public string contractId;
        public string startTime;
        public string expirationTime;
        public List<ContractObjectiveProgress> progress;
    }

    [System.Serializable]
    public class ContractObjectiveProgress
    {
        public string objectiveId;
        public float progress;
    }

    public enum ContractType
    {
        Main,       // Main story contracts
        Side,       // Side contracts
        Daily,      // Daily rotation
        Weekly,     // Weekly rotation
        Special,    // Limited-time events
        Repeatable  // Can be repeated
    }

    public enum ContractDifficulty
    {
        Easy,       // 1x rewards
        Normal,     // 1.5x rewards
        Hard,       // 2.5x rewards
        Extreme     // 4x rewards
    }

    public enum ContractObjectiveType
    {
        KillZombies,
        KillPlayers,
        KillBoss,
        CollectItems,
        ExtractWith Items,
        CompleteMissions,
        EarnCurrency,
        DealDamage,
        Survive,
        CompleteObjectives,
        ReachLocation,
        UseWeapon,
        CraftItems,
        TradewithVendor,
        EarnReputation
    }

    #endregion
}
