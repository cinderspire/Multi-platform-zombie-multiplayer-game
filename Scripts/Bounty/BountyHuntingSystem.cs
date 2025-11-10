using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Bounty
{
    /// <summary>
    /// Comprehensive bounty hunting and contract system for player-vs-player and PvE bounties.
    /// Includes contracts, hunter rankings, target tracking, and reward distribution.
    /// </summary>
    public class BountyHuntingSystem : NetworkBehaviour
    {
        public static BountyHuntingSystem Instance { get; private set; }

        [Header("Bounty Configuration")]
        [SerializeField] private int maxActiveBounties = 100;
        [SerializeField] private int maxPlayerBounties = 10;
        [SerializeField] private int maxContractsPerPlayer = 5;
        [SerializeField] private float bountyExpirationHours = 48f;
        [SerializeField] private float contractExpirationHours = 24f;
        [SerializeField] private int minBountyAmount = 1000;
        [SerializeField] private int maxBountyAmount = 1000000;

        [Header("Hunter Ranking")]
        [SerializeField] private int pointsPerKill = 100;
        [SerializeField] private int pointsPerContract = 250;
        [SerializeField] private int pointsPerBossBounty = 500;
        [SerializeField] private int maxHunterRank = 10;

        [Header("Protection Mechanics")]
        [SerializeField] private float newPlayerProtectionHours = 24f;
        [SerializeField] private int minLevelForBounties = 10;
        [SerializeField] private float bountyCooldownMinutes = 30f;

        [Header("Contract Types")]
        [SerializeField] private int maxSimultaneousContracts = 20;
        [SerializeField] private float contractRefreshHours = 6f;

        // Data structures
        private Dictionary<string, Bounty> activeBounties = new Dictionary<string, Bounty>();
        private Dictionary<ulong, List<string>> playerBounties = new Dictionary<ulong, List<string>>();
        private Dictionary<string, Contract> activeContracts = new Dictionary<string, Contract>();
        private Dictionary<ulong, PlayerHunterData> hunterData = new Dictionary<ulong, PlayerHunterData>();
        private Dictionary<ulong, List<string>> activePlayerContracts = new Dictionary<ulong, List<string>>();
        private Dictionary<string, BountyBoard> bountyBoards = new Dictionary<string, BountyBoard>();
        private Dictionary<ulong, DateTime> lastBountyPlaced = new Dictionary<ulong, DateTime>();
        private Dictionary<ulong, DateTime> playerCreationTime = new Dictionary<ulong, DateTime>();

        // Events
        public event Action<Bounty> OnBountyPosted;
        public event Action<string, ulong> OnBountyClaimed;
        public event Action<Contract> OnContractAvailable;
        public event Action<string, ulong> OnContractCompleted;
        public event Action<ulong, int> OnHunterRankChanged;
        public event Action<ulong, string> OnTargetKilled;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                InitializeBountyBoards();
                StartContractGeneration();
            }
        }

        private void InitializeBountyBoards()
        {
            // Initialize bounty boards in major locations
            var boards = new List<(string id, string location, BoardType type)>
            {
                ("board_safezone_1", "Safe Zone Alpha", BoardType.General),
                ("board_safezone_2", "Safe Zone Bravo", BoardType.General),
                ("board_pvp_1", "PvP Arena", BoardType.PlayerBounties),
                ("board_pve_1", "Hunter's Lodge", BoardType.CreatureBounties),
                ("board_elite_1", "Elite Hunter HQ", BoardType.EliteContracts)
            };

            foreach (var (id, location, type) in boards)
            {
                bountyBoards[id] = new BountyBoard
                {
                    boardId = id,
                    location = location,
                    boardType = type,
                    postedBounties = new List<string>(),
                    availableContracts = new List<string>()
                };
            }
        }

        private void StartContractGeneration()
        {
            InvokeRepeating(nameof(GenerateNewContracts), 0f, contractRefreshHours * 3600f);
        }

        private void GenerateNewContracts()
        {
            // Generate new contracts for each board
            foreach (var board in bountyBoards.Values)
            {
                if (board.boardType == BoardType.EliteContracts)
                {
                    GenerateEliteContracts(board);
                }
                else
                {
                    GenerateStandardContracts(board);
                }
            }
        }

        private void GenerateStandardContracts(BountyBoard board)
        {
            int contractsToGenerate = UnityEngine.Random.Range(3, 8);

            for (int i = 0; i < contractsToGenerate; i++)
            {
                var contract = CreateRandomContract(board.boardType);
                activeContracts[contract.contractId] = contract;
                board.availableContracts.Add(contract.contractId);
                OnContractAvailable?.Invoke(contract);
            }
        }

        private void GenerateEliteContracts(BountyBoard board)
        {
            int contractsToGenerate = UnityEngine.Random.Range(1, 4);

            for (int i = 0; i < contractsToGenerate; i++)
            {
                var contract = CreateRandomContract(BoardType.EliteContracts);
                contract.requiredHunterRank = UnityEngine.Random.Range(5, maxHunterRank);
                contract.rewardMultiplier = UnityEngine.Random.Range(2f, 5f);

                activeContracts[contract.contractId] = contract;
                board.availableContracts.Add(contract.contractId);
                OnContractAvailable?.Invoke(contract);
            }
        }

        private Contract CreateRandomContract(BoardType boardType)
        {
            var contractType = boardType switch
            {
                BoardType.PlayerBounties => ContractType.EliminatePlayer,
                BoardType.CreatureBounties => (ContractType)UnityEngine.Random.Range(1, 4),
                BoardType.EliteContracts => (ContractType)UnityEngine.Random.Range(0, 8),
                _ => (ContractType)UnityEngine.Random.Range(0, 8)
            };

            var contract = new Contract
            {
                contractId = $"contract_{Guid.NewGuid()}",
                contractType = contractType,
                title = GenerateContractTitle(contractType),
                description = GenerateContractDescription(contractType),
                issuer = "Bounty Board",
                issueDate = DateTime.UtcNow,
                expirationDate = DateTime.UtcNow.AddHours(contractExpirationHours),
                status = ContractStatus.Available,
                difficulty = (ContractDifficulty)UnityEngine.Random.Range(0, 5),
                objectives = GenerateContractObjectives(contractType),
                currentProgress = new Dictionary<string, float>(),
                rewardSoftCurrency = CalculateContractReward(contractType),
                rewardHardCurrency = 0,
                rewardItems = new List<string>(),
                rewardMultiplier = 1f,
                requiredHunterRank = 0,
                maxAcceptances = UnityEngine.Random.Range(1, 10),
                currentAcceptances = 0
            };

            // Initialize progress tracking
            foreach (var objective in contract.objectives)
            {
                contract.currentProgress[objective.objectiveId] = 0f;
            }

            // Add hard currency rewards for higher difficulties
            if (contract.difficulty >= ContractDifficulty.Hard)
            {
                contract.rewardHardCurrency = UnityEngine.Random.Range(10, 100);
            }

            return contract;
        }

        private string GenerateContractTitle(ContractType type)
        {
            return type switch
            {
                ContractType.EliminatePlayer => "Eliminate Hostile Player",
                ContractType.HuntCreatures => "Creature Extermination",
                ContractType.CollectItems => "Resource Collection",
                ContractType.EliminateBoss => "Boss Elimination",
                ContractType.ClearArea => "Area Cleansing",
                ContractType.ProtectTarget => "Target Protection",
                ContractType.InvestigateLocation => "Location Investigation",
                ContractType.RetrieveItem => "Item Retrieval",
                _ => "Special Contract"
            };
        }

        private string GenerateContractDescription(ContractType type)
        {
            return type switch
            {
                ContractType.EliminatePlayer => "A dangerous player has been terrorizing the area. Eliminate them for a reward.",
                ContractType.HuntCreatures => "The zombie population in this sector has grown too large. Thin their numbers.",
                ContractType.CollectItems => "We need specific resources for our operations. Collect and deliver them.",
                ContractType.EliminateBoss => "A powerful boss creature has been spotted. Take it down.",
                ContractType.ClearArea => "Clear all hostile entities from the designated area.",
                ContractType.ProtectTarget => "Keep the designated target alive for the specified duration.",
                ContractType.InvestigateLocation => "Investigate the marked location and report your findings.",
                ContractType.RetrieveItem => "Retrieve a specific item from a dangerous location.",
                _ => "Complete the specified objectives for rewards."
            };
        }

        private List<ContractObjective> GenerateContractObjectives(ContractType type)
        {
            var objectives = new List<ContractObjective>();

            switch (type)
            {
                case ContractType.EliminatePlayer:
                    objectives.Add(new ContractObjective
                    {
                        objectiveId = "obj_1",
                        description = "Eliminate 1 hostile player",
                        objectiveType = ObjectiveType.KillPlayers,
                        targetCount = 1,
                        currentCount = 0,
                        isOptional = false
                    });
                    break;

                case ContractType.HuntCreatures:
                    int creatureCount = UnityEngine.Random.Range(10, 50);
                    objectives.Add(new ContractObjective
                    {
                        objectiveId = "obj_1",
                        description = $"Eliminate {creatureCount} zombies",
                        objectiveType = ObjectiveType.KillCreatures,
                        targetCount = creatureCount,
                        currentCount = 0,
                        isOptional = false
                    });
                    break;

                case ContractType.CollectItems:
                    int itemCount = UnityEngine.Random.Range(5, 20);
                    objectives.Add(new ContractObjective
                    {
                        objectiveId = "obj_1",
                        description = $"Collect {itemCount} items",
                        objectiveType = ObjectiveType.CollectItems,
                        targetCount = itemCount,
                        currentCount = 0,
                        targetItemId = "contract_item_generic",
                        isOptional = false
                    });
                    break;

                case ContractType.EliminateBoss:
                    objectives.Add(new ContractObjective
                    {
                        objectiveId = "obj_1",
                        description = "Defeat the boss creature",
                        objectiveType = ObjectiveType.KillBoss,
                        targetCount = 1,
                        currentCount = 0,
                        targetEntityId = "boss_zombie_alpha",
                        isOptional = false
                    });
                    break;

                case ContractType.ClearArea:
                    objectives.Add(new ContractObjective
                    {
                        objectiveId = "obj_1",
                        description = "Clear all enemies from area",
                        objectiveType = ObjectiveType.ClearArea,
                        targetCount = 100,
                        currentCount = 0,
                        targetLocation = new Vector3(UnityEngine.Random.Range(-1000, 1000), 0, UnityEngine.Random.Range(-1000, 1000)),
                        isOptional = false
                    });
                    break;

                case ContractType.ProtectTarget:
                    objectives.Add(new ContractObjective
                    {
                        objectiveId = "obj_1",
                        description = "Protect target for 10 minutes",
                        objectiveType = ObjectiveType.ProtectTarget,
                        targetCount = 600,
                        currentCount = 0,
                        isOptional = false
                    });
                    break;

                case ContractType.InvestigateLocation:
                    objectives.Add(new ContractObjective
                    {
                        objectiveId = "obj_1",
                        description = "Investigate marked location",
                        objectiveType = ObjectiveType.ReachLocation,
                        targetCount = 1,
                        currentCount = 0,
                        targetLocation = new Vector3(UnityEngine.Random.Range(-1000, 1000), 0, UnityEngine.Random.Range(-1000, 1000)),
                        isOptional = false
                    });
                    break;

                case ContractType.RetrieveItem:
                    objectives.Add(new ContractObjective
                    {
                        objectiveId = "obj_1",
                        description = "Retrieve the specified item",
                        objectiveType = ObjectiveType.RetrieveItem,
                        targetCount = 1,
                        currentCount = 0,
                        targetItemId = "special_contract_item",
                        isOptional = false
                    });
                    break;
            }

            return objectives;
        }

        private int CalculateContractReward(ContractType type)
        {
            int baseReward = type switch
            {
                ContractType.EliminatePlayer => 5000,
                ContractType.HuntCreatures => 2000,
                ContractType.CollectItems => 1500,
                ContractType.EliminateBoss => 10000,
                ContractType.ClearArea => 7500,
                ContractType.ProtectTarget => 6000,
                ContractType.InvestigateLocation => 3000,
                ContractType.RetrieveItem => 4000,
                _ => 1000
            };

            return baseReward + UnityEngine.Random.Range(-500, 1000);
        }

        #region Server RPCs

        [ServerRpc(RequireOwnership = false)]
        public void PostBountyServerRpc(ulong posterId, ulong targetPlayerId, int amount, string reason, ServerRpcParams rpcParams = default)
        {
            if (!IsValidBountyPost(posterId, targetPlayerId, amount, out string error))
            {
                Debug.LogWarning($"Invalid bounty post: {error}");
                return;
            }

            var bounty = new Bounty
            {
                bountyId = $"bounty_{Guid.NewGuid()}",
                bountyType = BountyType.PlayerBounty,
                posterId = posterId,
                targetPlayerId = targetPlayerId,
                targetEntityId = null,
                amount = amount,
                reason = reason,
                postDate = DateTime.UtcNow,
                expirationDate = DateTime.UtcNow.AddHours(bountyExpirationHours),
                status = BountyStatus.Active,
                claimedBy = null,
                completedBy = null,
                completionDate = null
            };

            activeBounties[bounty.bountyId] = bounty;

            if (!playerBounties.ContainsKey(targetPlayerId))
            {
                playerBounties[targetPlayerId] = new List<string>();
            }
            playerBounties[targetPlayerId].Add(bounty.bountyId);

            lastBountyPlaced[posterId] = DateTime.UtcNow;

            // Deduct bounty amount from poster
            Economy.EconomyManager.Instance?.SpendSoftCurrency(posterId, amount);

            OnBountyPosted?.Invoke(bounty);
            BroadcastBountyPostedClientRpc(bounty);

            Debug.Log($"Bounty posted: {bounty.bountyId} on player {targetPlayerId} for {amount}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void PostCreatureBountyServerRpc(ulong posterId, string creatureType, int killCount, int amount, ServerRpcParams rpcParams = default)
        {
            var bounty = new Bounty
            {
                bountyId = $"bounty_{Guid.NewGuid()}",
                bountyType = BountyType.CreatureBounty,
                posterId = posterId,
                targetEntityId = creatureType,
                requiredKills = killCount,
                currentKills = 0,
                amount = amount,
                reason = $"Eliminate {killCount} {creatureType}",
                postDate = DateTime.UtcNow,
                expirationDate = DateTime.UtcNow.AddHours(bountyExpirationHours),
                status = BountyStatus.Active
            };

            activeBounties[bounty.bountyId] = bounty;

            OnBountyPosted?.Invoke(bounty);
            BroadcastBountyPostedClientRpc(bounty);
        }

        [ServerRpc(RequireOwnership = false)]
        public void ClaimBountyServerRpc(ulong hunterId, string bountyId, ServerRpcParams rpcParams = default)
        {
            if (!activeBounties.TryGetValue(bountyId, out var bounty))
            {
                Debug.LogWarning($"Bounty not found: {bountyId}");
                return;
            }

            if (bounty.status != BountyStatus.Active)
            {
                Debug.LogWarning($"Bounty not active: {bountyId}");
                return;
            }

            bounty.claimedBy = hunterId;
            bounty.status = BountyStatus.Claimed;

            OnBountyClaimed?.Invoke(bountyId, hunterId);
            NotifyBountyClaimedClientRpc(bountyId, hunterId);

            Debug.Log($"Bounty {bountyId} claimed by hunter {hunterId}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void CompleteBountyServerRpc(ulong hunterId, string bountyId, ulong killedPlayerId, ServerRpcParams rpcParams = default)
        {
            if (!activeBounties.TryGetValue(bountyId, out var bounty))
            {
                Debug.LogWarning($"Bounty not found: {bountyId}");
                return;
            }

            if (bounty.status != BountyStatus.Claimed || bounty.claimedBy != hunterId)
            {
                Debug.LogWarning($"Invalid bounty completion attempt");
                return;
            }

            if (bounty.bountyType == BountyType.PlayerBounty && bounty.targetPlayerId != killedPlayerId)
            {
                Debug.LogWarning($"Killed player does not match bounty target");
                return;
            }

            CompleteBounty(bounty, hunterId);
        }

        private void CompleteBounty(Bounty bounty, ulong hunterId)
        {
            bounty.completedBy = hunterId;
            bounty.completionDate = DateTime.UtcNow;
            bounty.status = BountyStatus.Completed;

            // Award bounty amount to hunter
            Economy.EconomyManager.Instance?.AddSoftCurrency(hunterId, bounty.amount);

            // Update hunter stats
            UpdateHunterStats(hunterId, bounty);

            // Remove from active player bounties
            if (bounty.targetPlayerId.HasValue && playerBounties.ContainsKey(bounty.targetPlayerId.Value))
            {
                playerBounties[bounty.targetPlayerId.Value].Remove(bounty.bountyId);
            }

            OnBountyClaimed?.Invoke(bounty.bountyId, hunterId);
            NotifyBountyCompletedClientRpc(bounty.bountyId, hunterId);

            Debug.Log($"Bounty {bounty.bountyId} completed by hunter {hunterId}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void AcceptContractServerRpc(ulong playerId, string contractId, ServerRpcParams rpcParams = default)
        {
            if (!activeContracts.TryGetValue(contractId, out var contract))
            {
                Debug.LogWarning($"Contract not found: {contractId}");
                return;
            }

            if (!CanAcceptContract(playerId, contract, out string error))
            {
                Debug.LogWarning($"Cannot accept contract: {error}");
                return;
            }

            if (!activePlayerContracts.ContainsKey(playerId))
            {
                activePlayerContracts[playerId] = new List<string>();
            }

            activePlayerContracts[playerId].Add(contractId);
            contract.currentAcceptances++;
            contract.acceptedBy.Add(playerId);

            NotifyContractAcceptedClientRpc(playerId, contractId);

            Debug.Log($"Player {playerId} accepted contract {contractId}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void UpdateContractProgressServerRpc(ulong playerId, string contractId, string objectiveId, float progress, ServerRpcParams rpcParams = default)
        {
            if (!activeContracts.TryGetValue(contractId, out var contract))
            {
                return;
            }

            if (!contract.acceptedBy.Contains(playerId))
            {
                return;
            }

            if (contract.currentProgress.ContainsKey(objectiveId))
            {
                contract.currentProgress[objectiveId] = progress;
            }

            // Check if contract is complete
            if (IsContractComplete(contract))
            {
                CompleteContract(contract, playerId);
            }
        }

        private void CompleteContract(Contract contract, ulong playerId)
        {
            contract.status = ContractStatus.Completed;
            contract.completionDate = DateTime.UtcNow;

            // Award rewards
            int softCurrencyReward = Mathf.RoundToInt(contract.rewardSoftCurrency * contract.rewardMultiplier);
            int hardCurrencyReward = Mathf.RoundToInt(contract.rewardHardCurrency * contract.rewardMultiplier);

            Economy.EconomyManager.Instance?.AddSoftCurrency(playerId, softCurrencyReward);
            if (hardCurrencyReward > 0)
            {
                Economy.EconomyManager.Instance?.AddHardCurrency(playerId, hardCurrencyReward);
            }

            // Update hunter stats
            if (!hunterData.ContainsKey(playerId))
            {
                hunterData[playerId] = CreateNewHunterData(playerId);
            }

            var data = hunterData[playerId];
            data.contractsCompleted++;
            data.totalEarnings += softCurrencyReward;
            data.hunterPoints += pointsPerContract;
            UpdateHunterRank(data);

            // Remove from active contracts
            if (activePlayerContracts.ContainsKey(playerId))
            {
                activePlayerContracts[playerId].Remove(contract.contractId);
            }

            OnContractCompleted?.Invoke(contract.contractId, playerId);
            NotifyContractCompletedClientRpc(contract.contractId, playerId, softCurrencyReward, hardCurrencyReward);

            Debug.Log($"Contract {contract.contractId} completed by player {playerId}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void AbandonContractServerRpc(ulong playerId, string contractId, ServerRpcParams rpcParams = default)
        {
            if (!activeContracts.TryGetValue(contractId, out var contract))
            {
                return;
            }

            if (activePlayerContracts.ContainsKey(playerId))
            {
                activePlayerContracts[playerId].Remove(contractId);
            }

            contract.acceptedBy.Remove(playerId);
            contract.currentAcceptances--;

            NotifyContractAbandonedClientRpc(playerId, contractId);
        }

        #endregion

        #region Validation

        private bool IsValidBountyPost(ulong posterId, ulong targetPlayerId, int amount, out string error)
        {
            error = null;

            // Check amount
            if (amount < minBountyAmount || amount > maxBountyAmount)
            {
                error = $"Bounty amount must be between {minBountyAmount} and {maxBountyAmount}";
                return false;
            }

            // Check cooldown
            if (lastBountyPlaced.ContainsKey(posterId))
            {
                var timeSinceLastBounty = DateTime.UtcNow - lastBountyPlaced[posterId];
                if (timeSinceLastBounty.TotalMinutes < bountyCooldownMinutes)
                {
                    error = $"Bounty cooldown active. Wait {bountyCooldownMinutes - timeSinceLastBounty.TotalMinutes:F1} more minutes";
                    return false;
                }
            }

            // Check new player protection
            if (playerCreationTime.ContainsKey(targetPlayerId))
            {
                var playerAge = DateTime.UtcNow - playerCreationTime[targetPlayerId];
                if (playerAge.TotalHours < newPlayerProtectionHours)
                {
                    error = "Target player is under new player protection";
                    return false;
                }
            }

            // Check max bounties on target
            if (playerBounties.ContainsKey(targetPlayerId) && playerBounties[targetPlayerId].Count >= maxPlayerBounties)
            {
                error = "Target player has maximum bounties";
                return false;
            }

            // Check poster has enough currency
            // This would integrate with your economy system
            // For now, assume it's valid

            return true;
        }

        private bool CanAcceptContract(ulong playerId, Contract contract, out string error)
        {
            error = null;

            // Check if player already has max contracts
            if (activePlayerContracts.ContainsKey(playerId) && activePlayerContracts[playerId].Count >= maxContractsPerPlayer)
            {
                error = "Maximum active contracts reached";
                return false;
            }

            // Check hunter rank requirement
            if (hunterData.TryGetValue(playerId, out var data))
            {
                if (data.hunterRank < contract.requiredHunterRank)
                {
                    error = $"Requires hunter rank {contract.requiredHunterRank}";
                    return false;
                }
            }
            else if (contract.requiredHunterRank > 0)
            {
                error = $"Requires hunter rank {contract.requiredHunterRank}";
                return false;
            }

            // Check if contract is expired
            if (DateTime.UtcNow > contract.expirationDate)
            {
                error = "Contract expired";
                return false;
            }

            // Check if contract has available slots
            if (contract.currentAcceptances >= contract.maxAcceptances)
            {
                error = "Contract fully accepted";
                return false;
            }

            return true;
        }

        private bool IsContractComplete(Contract contract)
        {
            foreach (var objective in contract.objectives)
            {
                if (objective.isOptional)
                    continue;

                if (!contract.currentProgress.TryGetValue(objective.objectiveId, out float progress))
                    return false;

                if (progress < objective.targetCount)
                    return false;
            }

            return true;
        }

        #endregion

        #region Hunter Stats

        private void UpdateHunterStats(ulong hunterId, Bounty bounty)
        {
            if (!hunterData.ContainsKey(hunterId))
            {
                hunterData[hunterId] = CreateNewHunterData(hunterId);
            }

            var data = hunterData[hunterId];
            data.bountiesCompleted++;
            data.totalEarnings += bounty.amount;

            if (bounty.bountyType == BountyType.PlayerBounty)
            {
                data.playerKills++;
                data.hunterPoints += pointsPerKill;
            }
            else if (bounty.bountyType == BountyType.BossBounty)
            {
                data.bossKills++;
                data.hunterPoints += pointsPerBossBounty;
            }
            else
            {
                data.creatureKills += bounty.currentKills;
                data.hunterPoints += bounty.currentKills;
            }

            UpdateHunterRank(data);
        }

        private PlayerHunterData CreateNewHunterData(ulong playerId)
        {
            return new PlayerHunterData
            {
                playerId = playerId,
                hunterRank = 0,
                hunterPoints = 0,
                bountiesCompleted = 0,
                contractsCompleted = 0,
                playerKills = 0,
                creatureKills = 0,
                bossKills = 0,
                totalEarnings = 0,
                successRate = 100f,
                registrationDate = DateTime.UtcNow,
                activeBounties = new List<string>(),
                activeContracts = new List<string>(),
                completedBountyHistory = new List<string>()
            };
        }

        private void UpdateHunterRank(PlayerHunterData data)
        {
            int oldRank = data.hunterRank;
            int newRank = CalculateHunterRank(data.hunterPoints);

            if (newRank != oldRank)
            {
                data.hunterRank = newRank;
                OnHunterRankChanged?.Invoke(data.playerId, newRank);
                NotifyHunterRankChangedClientRpc(data.playerId, newRank);
            }
        }

        private int CalculateHunterRank(int points)
        {
            // Exponential rank progression
            for (int rank = maxHunterRank; rank >= 0; rank--)
            {
                int requiredPoints = GetPointsForRank(rank);
                if (points >= requiredPoints)
                {
                    return rank;
                }
            }
            return 0;
        }

        private int GetPointsForRank(int rank)
        {
            if (rank == 0) return 0;
            return Mathf.RoundToInt(1000 * Mathf.Pow(1.5f, rank - 1));
        }

        #endregion

        #region Client RPCs

        [ClientRpc]
        private void BroadcastBountyPostedClientRpc(Bounty bounty)
        {
            OnBountyPosted?.Invoke(bounty);
        }

        [ClientRpc]
        private void NotifyBountyClaimedClientRpc(string bountyId, ulong hunterId)
        {
            OnBountyClaimed?.Invoke(bountyId, hunterId);
        }

        [ClientRpc]
        private void NotifyBountyCompletedClientRpc(string bountyId, ulong hunterId)
        {
            // Client-side notification
        }

        [ClientRpc]
        private void NotifyContractAcceptedClientRpc(ulong playerId, string contractId)
        {
            // Client-side notification
        }

        [ClientRpc]
        private void NotifyContractCompletedClientRpc(string contractId, ulong playerId, int softCurrency, int hardCurrency)
        {
            OnContractCompleted?.Invoke(contractId, playerId);
        }

        [ClientRpc]
        private void NotifyContractAbandonedClientRpc(ulong playerId, string contractId)
        {
            // Client-side notification
        }

        [ClientRpc]
        private void NotifyHunterRankChangedClientRpc(ulong playerId, int newRank)
        {
            OnHunterRankChanged?.Invoke(playerId, newRank);
        }

        #endregion

        #region Public API

        public List<Bounty> GetActiveBounties()
        {
            return activeBounties.Values.Where(b => b.status == BountyStatus.Active).ToList();
        }

        public List<Bounty> GetPlayerBounties(ulong playerId)
        {
            if (!playerBounties.ContainsKey(playerId))
                return new List<Bounty>();

            return playerBounties[playerId]
                .Select(id => activeBounties.GetValueOrDefault(id))
                .Where(b => b != null && b.status == BountyStatus.Active)
                .ToList();
        }

        public List<Contract> GetAvailableContracts(string boardId = null)
        {
            if (boardId != null && bountyBoards.TryGetValue(boardId, out var board))
            {
                return board.availableContracts
                    .Select(id => activeContracts.GetValueOrDefault(id))
                    .Where(c => c != null && c.status == ContractStatus.Available)
                    .ToList();
            }

            return activeContracts.Values.Where(c => c.status == ContractStatus.Available).ToList();
        }

        public PlayerHunterData GetHunterData(ulong playerId)
        {
            if (!hunterData.ContainsKey(playerId))
            {
                hunterData[playerId] = CreateNewHunterData(playerId);
            }
            return hunterData[playerId];
        }

        public int GetTotalBountyAmount(ulong targetPlayerId)
        {
            if (!playerBounties.ContainsKey(targetPlayerId))
                return 0;

            return playerBounties[targetPlayerId]
                .Select(id => activeBounties.GetValueOrDefault(id))
                .Where(b => b != null && b.status == BountyStatus.Active)
                .Sum(b => b.amount);
        }

        #endregion
    }

    #region Data Structures

    [Serializable]
    public class Bounty
    {
        public string bountyId;
        public BountyType bountyType;
        public ulong posterId;
        public ulong? targetPlayerId;
        public string targetEntityId;
        public int amount;
        public string reason;
        public DateTime postDate;
        public DateTime expirationDate;
        public BountyStatus status;
        public ulong? claimedBy;
        public ulong? completedBy;
        public DateTime? completionDate;
        public int requiredKills;
        public int currentKills;
    }

    [Serializable]
    public class Contract
    {
        public string contractId;
        public ContractType contractType;
        public string title;
        public string description;
        public string issuer;
        public DateTime issueDate;
        public DateTime expirationDate;
        public ContractStatus status;
        public ContractDifficulty difficulty;
        public List<ContractObjective> objectives;
        public Dictionary<string, float> currentProgress;
        public int rewardSoftCurrency;
        public int rewardHardCurrency;
        public List<string> rewardItems;
        public float rewardMultiplier;
        public int requiredHunterRank;
        public int maxAcceptances;
        public int currentAcceptances;
        public List<ulong> acceptedBy = new List<ulong>();
        public DateTime? completionDate;
    }

    [Serializable]
    public class ContractObjective
    {
        public string objectiveId;
        public string description;
        public ObjectiveType objectiveType;
        public int targetCount;
        public int currentCount;
        public string targetItemId;
        public string targetEntityId;
        public Vector3 targetLocation;
        public bool isOptional;
    }

    [Serializable]
    public class PlayerHunterData
    {
        public ulong playerId;
        public int hunterRank;
        public int hunterPoints;
        public int bountiesCompleted;
        public int contractsCompleted;
        public int playerKills;
        public int creatureKills;
        public int bossKills;
        public int totalEarnings;
        public float successRate;
        public DateTime registrationDate;
        public List<string> activeBounties;
        public List<string> activeContracts;
        public List<string> completedBountyHistory;
    }

    [Serializable]
    public class BountyBoard
    {
        public string boardId;
        public string location;
        public BoardType boardType;
        public List<string> postedBounties;
        public List<string> availableContracts;
    }

    public enum BountyType
    {
        PlayerBounty,
        CreatureBounty,
        BossBounty,
        LocationBounty
    }

    public enum BountyStatus
    {
        Active,
        Claimed,
        Completed,
        Expired,
        Cancelled
    }

    public enum ContractType
    {
        EliminatePlayer,
        HuntCreatures,
        CollectItems,
        EliminateBoss,
        ClearArea,
        ProtectTarget,
        InvestigateLocation,
        RetrieveItem
    }

    public enum ContractStatus
    {
        Available,
        InProgress,
        Completed,
        Failed,
        Expired
    }

    public enum ContractDifficulty
    {
        Easy,
        Normal,
        Hard,
        Expert,
        Legendary
    }

    public enum ObjectiveType
    {
        KillPlayers,
        KillCreatures,
        KillBoss,
        CollectItems,
        ReachLocation,
        ClearArea,
        ProtectTarget,
        RetrieveItem,
        SurviveTime
    }

    public enum BoardType
    {
        General,
        PlayerBounties,
        CreatureBounties,
        EliteContracts
    }

    #endregion
}
