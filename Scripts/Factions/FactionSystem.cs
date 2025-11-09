using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Factions
{
    /// <summary>
    /// Comprehensive faction reputation and allegiance system for multi-platform zombie multiplayer game.
    /// Manages multiple factions with reputation, rewards, and storyline progression.
    /// </summary>
    public class FactionSystem : NetworkBehaviour
    {
        public static FactionSystem Instance { get; private set; }

        [Header("Faction Settings")]
        [SerializeField] private bool enableFactionSystem = true;
        [SerializeField] private int maxFactions = 20;
        [SerializeField] private bool enableReputationDecay = true;
        [SerializeField] private float decayRate = 1f; // Points per day

        [Header("Reputation Settings")]
        [SerializeField] private int minReputation = -10000;
        [SerializeField] private int maxReputation = 10000;
        [SerializeField] private int neutralThreshold = 0;
        [SerializeField] private bool enableOpposingFactions = true;

        [Header("Reward Settings")]
        [SerializeField] private bool enableReputationRewards = true;
        [SerializeField] private int rewardInterval = 1000; // Every 1000 rep

        // Enums
        public enum FactionStanding
        {
            Hated = -4,
            Hostile = -3,
            Unfriendly = -2,
            Neutral = 0,
            Friendly = 1,
            Honored = 2,
            Revered = 3,
            Exalted = 4
        }

        public enum FactionType
        {
            Military,
            Survivors,
            Scientists,
            Traders,
            Raiders,
            Cultists,
            Government,
            Rebels
        }

        // Data structures
        [Serializable]
        public class Faction
        {
            public string factionId;
            public string factionName;
            public string description;
            public FactionType type;
            public string leaderName;
            public string homeBase;
            public List<string> opposingFactions = new List<string>();
            public List<string> alliedFactions = new List<string>();
            public Dictionary<FactionStanding, FactionReward> standingRewards = new Dictionary<FactionStanding, FactionReward>();
            public List<FactionQuest> availableQuests = new List<FactionQuest>();
            public string iconPath;
            public Color factionColor;
        }

        [Serializable]
        public class PlayerFactionData
        {
            public ulong playerId;
            public Dictionary<string, int> reputation = new Dictionary<string, int>();
            public Dictionary<string, FactionStanding> standings = new Dictionary<string, FactionStanding>();
            public Dictionary<string, List<FactionReward>> claimedRewards = new Dictionary<string, List<FactionReward>>();
            public Dictionary<string, DateTime> lastReputationChange = new Dictionary<string, DateTime>();
            public string primaryFaction;
        }

        [Serializable]
        public class FactionReward
        {
            public string rewardId;
            public FactionStanding requiredStanding;
            public int requiredReputation;
            public string itemId;
            public int currency;
            public string title;
            public string perkId;
            public bool claimed;
        }

        [Serializable]
        public class FactionQuest
        {
            public string questId;
            public string questName;
            public string description;
            public FactionStanding requiredStanding;
            public int reputationReward;
            public int currencyReward;
            public string itemReward;
            public bool isRepeatable;
        }

        [Serializable]
        public class ReputationChange
        {
            public string factionId;
            public int amount;
            public string reason;
            public DateTime timestamp;
            public ulong playerId;
        }

        // State
        private Dictionary<string, Faction> factions = new Dictionary<string, Faction>();
        private Dictionary<ulong, PlayerFactionData> playerFactionData = new Dictionary<ulong, PlayerFactionData>();
        private List<ReputationChange> recentChanges = new List<ReputationChange>();

        // Events
        public event Action<ulong, string, int> OnReputationChanged;
        public event Action<ulong, string, FactionStanding> OnStandingChanged;
        public event Action<ulong, string> OnPrimaryFactionChanged;
        public event Action<ulong, FactionReward> OnRewardClaimed;

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
                InitializeFactionSystem();
            }
        }

        private void Update()
        {
            if (!IsServer || !enableFactionSystem) return;

            if (enableReputationDecay)
            {
                ProcessReputationDecay();
            }
        }

        #region Initialization

        private void InitializeFactionSystem()
        {
            InitializeFactions();
        }

        private void InitializeFactions()
        {
            // Military Faction
            RegisterFaction(new Faction
            {
                factionId = "faction_military",
                factionName = "United Military Forces",
                description = "Remnants of the world's armed forces fighting to restore order",
                type = FactionType.Military,
                leaderName = "General Marcus Hayes",
                homeBase = "Fort Resilience",
                opposingFactions = new List<string> { "faction_raiders", "faction_cultists" },
                alliedFactions = new List<string> { "faction_government" },
                factionColor = new Color(0.2f, 0.4f, 0.2f)
            });

            // Survivors Faction
            RegisterFaction(new Faction
            {
                factionId = "faction_survivors",
                factionName = "The Survivors Coalition",
                description = "Civilian groups banded together for mutual protection",
                type = FactionType.Survivors,
                leaderName = "Maria Chen",
                homeBase = "Haven Settlement",
                alliedFactions = new List<string> { "faction_traders" },
                factionColor = new Color(0.6f, 0.6f, 0.3f)
            });

            // Scientists Faction
            RegisterFaction(new Faction
            {
                factionId = "faction_scientists",
                factionName = "Research Initiative",
                description = "Scientists seeking a cure for the infection",
                type = FactionType.Scientists,
                leaderName = "Dr. Elena Volkov",
                homeBase = "Apex Laboratory",
                alliedFactions = new List<string> { "faction_military", "faction_government" },
                factionColor = new Color(0.3f, 0.5f, 0.8f)
            });

            // Traders Faction
            RegisterFaction(new Faction
            {
                factionId = "faction_traders",
                factionName = "Free Traders Guild",
                description = "Merchants and scavengers trading in the wasteland",
                type = FactionType.Traders,
                leaderName = "Victor Kane",
                homeBase = "Market Square",
                alliedFactions = new List<string> { "faction_survivors" },
                factionColor = new Color(0.8f, 0.6f, 0.2f)
            });

            // Raiders Faction
            RegisterFaction(new Faction
            {
                factionId = "faction_raiders",
                factionName = "The Marauders",
                description = "Hostile groups preying on other survivors",
                type = FactionType.Raiders,
                leaderName = "Reaver",
                homeBase = "Unknown",
                opposingFactions = new List<string> { "faction_military", "faction_survivors", "faction_government" },
                factionColor = new Color(0.7f, 0.2f, 0.2f)
            });

            // Setup rewards for each faction
            foreach (var faction in factions.Values)
            {
                SetupFactionRewards(faction);
            }
        }

        private void RegisterFaction(Faction faction)
        {
            if (factions.Count >= maxFactions)
            {
                Debug.LogWarning("Maximum factions reached");
                return;
            }

            factions[faction.factionId] = faction;
        }

        private void SetupFactionRewards(Faction faction)
        {
            faction.standingRewards[FactionStanding.Friendly] = new FactionReward
            {
                rewardId = $"{faction.factionId}_friendly",
                requiredStanding = FactionStanding.Friendly,
                requiredReputation = 1000,
                currency = 500,
                title = $"{faction.factionName} Friend"
            };

            faction.standingRewards[FactionStanding.Honored] = new FactionReward
            {
                rewardId = $"{faction.factionId}_honored",
                requiredStanding = FactionStanding.Honored,
                requiredReputation = 3000,
                currency = 1500,
                itemId = $"{faction.factionId}_armor",
                title = $"{faction.factionName} Champion"
            };

            faction.standingRewards[FactionStanding.Revered] = new FactionReward
            {
                rewardId = $"{faction.factionId}_revered",
                requiredStanding = FactionStanding.Revered,
                requiredReputation = 6000,
                currency = 3000,
                itemId = $"{faction.factionId}_weapon",
                title = $"{faction.factionName} Hero"
            };

            faction.standingRewards[FactionStanding.Exalted] = new FactionReward
            {
                rewardId = $"{faction.factionId}_exalted",
                requiredStanding = FactionStanding.Exalted,
                requiredReputation = 10000,
                currency = 10000,
                itemId = $"{faction.factionId}_legendary",
                title = $"{faction.factionName} Legend"
            };
        }

        #endregion

        #region Reputation Management

        [ServerRpc(RequireOwnership = false)]
        public void ModifyReputationServerRpc(ulong playerId, string factionId, int amount, string reason)
        {
            if (!factions.ContainsKey(factionId))
            {
                Debug.LogWarning($"Faction {factionId} not found");
                return;
            }

            if (!playerFactionData.ContainsKey(playerId))
            {
                playerFactionData[playerId] = new PlayerFactionData { playerId = playerId };
            }

            var data = playerFactionData[playerId];
            var faction = factions[factionId];

            // Initialize reputation if needed
            if (!data.reputation.ContainsKey(factionId))
            {
                data.reputation[factionId] = 0;
                data.standings[factionId] = FactionStanding.Neutral;
            }

            int oldReputation = data.reputation[factionId];
            FactionStanding oldStanding = data.standings[factionId];

            // Apply reputation change
            data.reputation[factionId] = Mathf.Clamp(
                data.reputation[factionId] + amount,
                minReputation,
                maxReputation
            );

            data.lastReputationChange[factionId] = DateTime.UtcNow;

            // Update standing
            FactionStanding newStanding = CalculateStanding(data.reputation[factionId]);
            data.standings[factionId] = newStanding;

            // Record change
            var change = new ReputationChange
            {
                factionId = factionId,
                amount = amount,
                reason = reason,
                timestamp = DateTime.UtcNow,
                playerId = playerId
            };

            recentChanges.Add(change);

            if (recentChanges.Count > 100)
            {
                recentChanges.RemoveAt(0);
            }

            OnReputationChanged?.Invoke(playerId, factionId, amount);

            // Check standing change
            if (newStanding != oldStanding)
            {
                OnStandingChanged?.Invoke(playerId, factionId, newStanding);
                CheckStandingRewards(playerId, factionId, newStanding);
            }

            // Handle opposing factions
            if (enableOpposingFactions && amount > 0)
            {
                foreach (var opposingFactionId in faction.opposingFactions)
                {
                    int opposingAmount = -Mathf.RoundToInt(amount * 0.25f);
                    ModifyReputationServerRpc(playerId, opposingFactionId, opposingAmount, $"Opposed by {faction.factionName}");
                }
            }

            // Handle allied factions
            if (amount > 0)
            {
                foreach (var alliedFactionId in faction.alliedFactions)
                {
                    int alliedAmount = Mathf.RoundToInt(amount * 0.1f);
                    if (alliedAmount > 0)
                    {
                        ModifyReputationServerRpc(playerId, alliedFactionId, alliedAmount, $"Allied with {faction.factionName}");
                    }
                }
            }

            Debug.Log($"Player {playerId} reputation with {faction.factionName}: {oldReputation} -> {data.reputation[factionId]} ({newStanding})");
        }

        private FactionStanding CalculateStanding(int reputation)
        {
            if (reputation >= 10000) return FactionStanding.Exalted;
            if (reputation >= 6000) return FactionStanding.Revered;
            if (reputation >= 3000) return FactionStanding.Honored;
            if (reputation >= 1000) return FactionStanding.Friendly;
            if (reputation >= 0) return FactionStanding.Neutral;
            if (reputation >= -1000) return FactionStanding.Unfriendly;
            if (reputation >= -3000) return FactionStanding.Hostile;
            return FactionStanding.Hated;
        }

        #endregion

        #region Rewards

        private void CheckStandingRewards(ulong playerId, string factionId, FactionStanding newStanding)
        {
            if (!factions.ContainsKey(factionId)) return;

            var faction = factions[factionId];

            if (!faction.standingRewards.ContainsKey(newStanding)) return;

            var reward = faction.standingRewards[newStanding];

            if (!playerFactionData[playerId].claimedRewards.ContainsKey(factionId))
            {
                playerFactionData[playerId].claimedRewards[factionId] = new List<FactionReward>();
            }

            var claimedRewards = playerFactionData[playerId].claimedRewards[factionId];

            if (claimedRewards.Any(r => r.rewardId == reward.rewardId)) return;

            GrantFactionReward(playerId, factionId, reward);
        }

        private void GrantFactionReward(ulong playerId, string factionId, FactionReward reward)
        {
            // Grant currency
            if (reward.currency > 0)
            {
                Economy.EconomyManager.Instance?.AddSoftCurrency(playerId, reward.currency);
            }

            // Grant item
            if (!string.IsNullOrEmpty(reward.itemId))
            {
                Inventory.InventoryManager.Instance?.AddItem(playerId, reward.itemId, 1);
            }

            // Grant title (would integrate with a title system)

            // Mark as claimed
            reward.claimed = true;
            playerFactionData[playerId].claimedRewards[factionId].Add(reward);

            OnRewardClaimed?.Invoke(playerId, reward);

            Debug.Log($"Player {playerId} received reward from {factionId}: {reward.title}");
        }

        #endregion

        #region Primary Faction

        [ServerRpc(RequireOwnership = false)]
        public void SetPrimaryFactionServerRpc(ulong playerId, string factionId)
        {
            if (!factions.ContainsKey(factionId))
            {
                Debug.LogWarning($"Faction {factionId} not found");
                return;
            }

            if (!playerFactionData.ContainsKey(playerId))
            {
                playerFactionData[playerId] = new PlayerFactionData { playerId = playerId };
            }

            // Check minimum standing
            if (GetStanding(playerId, factionId) < FactionStanding.Friendly)
            {
                Debug.LogWarning($"Player {playerId} standing too low with {factionId}");
                return;
            }

            playerFactionData[playerId].primaryFaction = factionId;

            OnPrimaryFactionChanged?.Invoke(playerId, factionId);

            Debug.Log($"Player {playerId} set primary faction to {factionId}");
        }

        #endregion

        #region Reputation Decay

        private void ProcessReputationDecay()
        {
            DateTime now = DateTime.UtcNow;

            foreach (var playerData in playerFactionData.Values)
            {
                foreach (var factionId in playerData.reputation.Keys.ToList())
                {
                    if (!playerData.lastReputationChange.ContainsKey(factionId))
                        continue;

                    DateTime lastChange = playerData.lastReputationChange[factionId];
                    float daysSinceChange = (float)(now - lastChange).TotalDays;

                    if (daysSinceChange >= 1f)
                    {
                        int currentRep = playerData.reputation[factionId];

                        // Decay toward neutral
                        if (currentRep > 0)
                        {
                            int decay = Mathf.RoundToInt(decayRate * daysSinceChange);
                            playerData.reputation[factionId] = Mathf.Max(0, currentRep - decay);
                        }
                        else if (currentRep < 0)
                        {
                            int decay = Mathf.RoundToInt(decayRate * daysSinceChange);
                            playerData.reputation[factionId] = Mathf.Min(0, currentRep + decay);
                        }

                        playerData.lastReputationChange[factionId] = now;
                    }
                }
            }
        }

        #endregion

        #region Public API

        public PlayerFactionData GetPlayerFactionData(ulong playerId)
        {
            if (!playerFactionData.ContainsKey(playerId))
            {
                playerFactionData[playerId] = new PlayerFactionData { playerId = playerId };
            }

            return playerFactionData[playerId];
        }

        public int GetReputation(ulong playerId, string factionId)
        {
            if (!playerFactionData.ContainsKey(playerId)) return 0;

            var data = playerFactionData[playerId];
            return data.reputation.ContainsKey(factionId) ? data.reputation[factionId] : 0;
        }

        public FactionStanding GetStanding(ulong playerId, string factionId)
        {
            if (!playerFactionData.ContainsKey(playerId)) return FactionStanding.Neutral;

            var data = playerFactionData[playerId];
            return data.standings.ContainsKey(factionId) ? data.standings[factionId] : FactionStanding.Neutral;
        }

        public Faction GetFaction(string factionId)
        {
            return factions.ContainsKey(factionId) ? factions[factionId] : null;
        }

        public List<Faction> GetAllFactions()
        {
            return factions.Values.ToList();
        }

        public Dictionary<string, int> GetAllReputations(ulong playerId)
        {
            if (!playerFactionData.ContainsKey(playerId))
                return new Dictionary<string, int>();

            return playerFactionData[playerId].reputation;
        }

        public List<Faction> GetAlliedFactions(ulong playerId, int minReputation = 1000)
        {
            if (!playerFactionData.ContainsKey(playerId))
                return new List<Faction>();

            return factions.Values
                .Where(f => GetReputation(playerId, f.factionId) >= minReputation)
                .ToList();
        }

        public List<Faction> GetHostileFactions(ulong playerId)
        {
            if (!playerFactionData.ContainsKey(playerId))
                return new List<Faction>();

            return factions.Values
                .Where(f => GetStanding(playerId, f.factionId) <= FactionStanding.Hostile)
                .ToList();
        }

        #endregion
    }
}
