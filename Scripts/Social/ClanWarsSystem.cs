using Unity.Netcode;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ZombieGame
{
    /// <summary>
    /// Clan Wars & Territory System - Faction-based PvP/PvE warfare
    /// Clans compete for territory control, resources, and global domination
    /// Features war declarations, territory battles, clan rankings, and rewards
    /// </summary>
    public class ClanWarsSystem : NetworkBehaviour
    {
        public static ClanWarsSystem Instance { get; private set; }

        [Header("War Settings")]
        [SerializeField] private bool enableClanWars = true;
        [SerializeField] private float warDuration = 7200f; // 2 hours
        [SerializeField] private float warCooldown = 86400f; // 24 hours
        [SerializeField] private int minClanMembersForWar = 5;

        [Header("Territory Settings")]
        [SerializeField] private int totalTerritories = 20;
        [SerializeField] private float captureTime = 120f; // 2 minutes to capture
        [SerializeField] private int territoryDefenseBonus = 20; // %

        // Network Variables
        private NetworkVariable<int> activeWarsCount = new NetworkVariable<int>(0);

        // Clans
        private Dictionary<string, ClanData> clans = new Dictionary<string, ClanData>();

        // Territories
        private Dictionary<string, Territory> territories = new Dictionary<string, Territory>();

        // Active Wars
        private Dictionary<string, ActiveWar> activeWars = new Dictionary<string, ActiveWar>();

        // War History
        private List<WarHistory> warHistoryList = new List<WarHistory>();

        // Events
        public event System.Action<string, string> OnWarDeclared; // attackerClanId, defenderClanId
        public event System.Action<string> OnWarEnded;
        public event System.Action<string, string> OnTerritoryCapture; // territoryId, clanId

        [System.Serializable]
        public class ClanData
        {
            public string clanId;
            public string clanName;
            public string clanTag; // [TAG]
            public ulong leaderId;
            public List<ulong> members = new List<ulong>();
            public List<ulong> officers = new List<ulong>();
            public int clanLevel = 1;
            public int clanXP = 0;
            public int warPoints = 0;
            public List<string> ownedTerritories = new List<string>();
            public int resourcesHeld = 0;
            public int totalWins = 0;
            public int totalLosses = 0;
            public DateTime creationDate;
            public string clanDescription = "";
            public Dictionary<string, int> clanPerks = new Dictionary<string, int>();
        }

        [System.Serializable]
        public class Territory
        {
            public string territoryId;
            public string territoryName;
            public TerritoryType type;
            public Vector3 centerPosition;
            public float radius = 50f;
            public string ownerClanId;
            public int defensePower = 100;
            public int resourceGeneration = 10; // Per hour
            public Dictionary<string, int> resources = new Dictionary<string, int>();
            public bool isContested = false;
            public DateTime lastCaptureTime;
        }

        public enum TerritoryType
        {
            Military, // High defensive bonus
            Resource,  // High resource generation
            Strategic, // Near objectives
            Urban      // Mixed benefits
        }

        [System.Serializable]
        public class ActiveWar
        {
            public string warId;
            public string attackerClanId;
            public string defenderClanId;
            public DateTime startTime;
            public DateTime endTime;
            public WarStatus status;
            public Dictionary<string, int> attackerScore = new Dictionary<string, int>(); // playerId -> score
            public Dictionary<string, int> defenderScore = new Dictionary<string, int>();
            public int totalAttackerScore = 0;
            public int totalDefenderScore = 0;
            public string targetTerritoryId;
            public WarObjective objective;
        }

        public enum WarStatus
        {
            Declared,
            Active,
            Ended
        }

        public enum WarObjective
        {
            TerritoryCapture,  // Capture specific territory
            Elimination,       // Most kills wins
            ResourceControl,   // Hold resources longest
            Survival          // Last clan standing
        }

        [System.Serializable]
        public class WarHistory
        {
            public string warId;
            public string attackerClanId;
            public string defenderClanId;
            public string winnerClanId;
            public int attackerScore;
            public int defenderScore;
            public DateTime warDate;
            public WarObjective objective;
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeSystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                GenerateTerritories();
            }
        }

        private void InitializeSystem()
        {
            Debug.Log("[ClanWars] System initialized");
        }

        private void GenerateTerritories()
        {
            for (int i = 0; i < totalTerritories; i++)
            {
                string territoryId = $"territory_{i}";

                territories[territoryId] = new Territory
                {
                    territoryId = territoryId,
                    territoryName = GetTerritoryName(i),
                    type = (TerritoryType)(i % 4),
                    centerPosition = new Vector3(
                        UnityEngine.Random.Range(-1000f, 1000f),
                        0,
                        UnityEngine.Random.Range(-1000f, 1000f)
                    ),
                    radius = UnityEngine.Random.Range(40f, 80f),
                    ownerClanId = "", // Unclaimed
                    defensePower = 100,
                    resourceGeneration = UnityEngine.Random.Range(5, 20),
                    resources = new Dictionary<string, int>(),
                    lastCaptureTime = DateTime.MinValue
                };
            }

            Debug.Log($"[ClanWars] Generated {territories.Count} territories");
        }

        private string GetTerritoryName(int index)
        {
            string[] names = new string[]
            {
                "Deadwood Outpost", "Zombie Valley", "Safe Haven", "The Fortress",
                "Abandoned City", "Military Base", "Research Facility", "Industrial Zone",
                "Harbor District", "Downtown Square", "Farmlands", "Mountain Pass",
                "Forest Refuge", "Desert Stronghold", "Coastal Defense", "Quarantine Zone",
                "Power Plant", "Shopping Mall", "Hospital", "Police Station"
            };

            return index < names.Length ? names[index] : $"Territory {index + 1}";
        }

        private void Update()
        {
            if (!IsServer || !enableClanWars) return;

            UpdateActiveWars();
            UpdateTerritoryResources();
        }

        private void UpdateActiveWars()
        {
            var warsToEnd = new List<string>();

            foreach (var war in activeWars.Values)
            {
                if (DateTime.UtcNow >= war.endTime && war.status == WarStatus.Active)
                {
                    warsToEnd.Add(war.warId);
                }
            }

            foreach (var warId in warsToEnd)
            {
                EndWar(warId);
            }
        }

        private void UpdateTerritoryResources()
        {
            // Generate resources for owned territories
            foreach (var territory in territories.Values)
            {
                if (!string.IsNullOrEmpty(territory.ownerClanId))
                {
                    // Generate resources (simplified - would be time-based in real implementation)
                }
            }
        }

        // Public API

        [ServerRpc(RequireOwnership = false)]
        public void CreateClanServerRpc(ulong leaderId, string clanName, string clanTag, ServerRpcParams rpcParams = default)
        {
            string clanId = $"clan_{Guid.NewGuid()}";

            clans[clanId] = new ClanData
            {
                clanId = clanId,
                clanName = clanName,
                clanTag = clanTag,
                leaderId = leaderId,
                members = new List<ulong> { leaderId },
                officers = new List<ulong> { leaderId },
                creationDate = DateTime.UtcNow
            };

            NotifyClanCreatedClientRpc(clanId, clanName);
        }

        [ClientRpc]
        private void NotifyClanCreatedClientRpc(string clanId, string clanName)
        {
            Debug.Log($"[ClanWars] Clan created: {clanName} ({clanId})");
        }

        [ServerRpc(RequireOwnership = false)]
        public void DeclareWarServerRpc(string attackerClanId, string defenderClanId, string targetTerritoryId, ServerRpcParams rpcParams = default)
        {
            if (!enableClanWars) return;

            if (!clans.ContainsKey(attackerClanId) || !clans.ContainsKey(defenderClanId))
            {
                return;
            }

            var attackerClan = clans[attackerClanId];
            var defenderClan = clans[defenderClanId];

            // Check requirements
            if (attackerClan.members.Count < minClanMembersForWar ||
                defenderClan.members.Count < minClanMembersForWar)
            {
                return;
            }

            // Create war
            string warId = $"war_{Guid.NewGuid()}";

            activeWars[warId] = new ActiveWar
            {
                warId = warId,
                attackerClanId = attackerClanId,
                defenderClanId = defenderClanId,
                startTime = DateTime.UtcNow,
                endTime = DateTime.UtcNow.AddSeconds(warDuration),
                status = WarStatus.Active,
                targetTerritoryId = targetTerritoryId,
                objective = WarObjective.TerritoryCapture
            };

            activeWarsCount.Value++;

            OnWarDeclared?.Invoke(attackerClanId, defenderClanId);
            NotifyWarDeclaredClientRpc(attackerClanId, defenderClanId, warId);
        }

        [ClientRpc]
        private void NotifyWarDeclaredClientRpc(string attackerClanId, string defenderClanId, string warId)
        {
            Debug.Log($"[ClanWars] WAR DECLARED: {attackerClanId} vs {defenderClanId} (War ID: {warId})");
        }

        private void EndWar(string warId)
        {
            if (!activeWars.ContainsKey(warId)) return;

            var war = activeWars[warId];
            war.status = WarStatus.Ended;

            // Determine winner
            string winnerClanId = war.totalAttackerScore > war.totalDefenderScore
                ? war.attackerClanId
                : war.defenderClanId;

            var winnerClan = clans[winnerClanId];
            var loserClanId = winnerClanId == war.attackerClanId ? war.defenderClanId : war.attackerClanId;
            var loserClan = clans[loserClanId];

            // Update clan stats
            winnerClan.totalWins++;
            winnerClan.warPoints += 100;
            loserClan.totalLosses++;
            loserClan.warPoints -= 50;

            // Territory transfer
            if (war.objective == WarObjective.TerritoryCapture &&
                !string.IsNullOrEmpty(war.targetTerritoryId) &&
                territories.ContainsKey(war.targetTerritoryId))
            {
                var territory = territories[war.targetTerritoryId];

                if (winnerClanId == war.attackerClanId) // Attacker won
                {
                    // Transfer territory
                    if (!string.IsNullOrEmpty(territory.ownerClanId))
                    {
                        loserClan.ownedTerritories.Remove(war.targetTerritoryId);
                    }

                    territory.ownerClanId = winnerClanId;
                    territory.lastCaptureTime = DateTime.UtcNow;
                    winnerClan.ownedTerritories.Add(war.targetTerritoryId);

                    OnTerritoryCapture?.Invoke(war.targetTerritoryId, winnerClanId);
                }
            }

            // Record history
            warHistoryList.Add(new WarHistory
            {
                warId = warId,
                attackerClanId = war.attackerClanId,
                defenderClanId = war.defenderClanId,
                winnerClanId = winnerClanId,
                attackerScore = war.totalAttackerScore,
                defenderScore = war.totalDefenderScore,
                warDate = DateTime.UtcNow,
                objective = war.objective
            });

            activeWars.Remove(warId);
            activeWarsCount.Value--;

            OnWarEnded?.Invoke(warId);
            NotifyWarEndedClientRpc(warId, winnerClanId, war.totalAttackerScore, war.totalDefenderScore);
        }

        [ClientRpc]
        private void NotifyWarEndedClientRpc(string warId, string winnerClanId, int attackerScore, int defenderScore)
        {
            Debug.Log($"[ClanWars] WAR ENDED: {warId} | Winner: {winnerClanId} | Score: {attackerScore} vs {defenderScore}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void AddWarScoreServerRpc(ulong playerId, string warId, int score, ServerRpcParams rpcParams = default)
        {
            if (!activeWars.ContainsKey(warId)) return;

            var war = activeWars[warId];

            // Determine which side player is on
            var attackerClan = clans[war.attackerClanId];
            var defenderClan = clans[war.defenderClanId];

            if (attackerClan.members.Contains(playerId))
            {
                war.attackerScore[playerId.ToString()] = war.attackerScore.GetValueOrDefault(playerId.ToString(), 0) + score;
                war.totalAttackerScore += score;
            }
            else if (defenderClan.members.Contains(playerId))
            {
                war.defenderScore[playerId.ToString()] = war.defenderScore.GetValueOrDefault(playerId.ToString(), 0) + score;
                war.totalDefenderScore += score;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void CaptureTerritoryServerRpc(string clanId, string territoryId, ServerRpcParams rpcParams = default)
        {
            if (!clans.ContainsKey(clanId) || !territories.ContainsKey(territoryId))
            {
                return;
            }

            var territory = territories[territoryId];
            var clan = clans[clanId];

            // Check if can capture
            if (territory.isContested) return;

            // Transfer ownership
            if (!string.IsNullOrEmpty(territory.ownerClanId))
            {
                var previousOwner = clans[territory.ownerClanId];
                previousOwner.ownedTerritories.Remove(territoryId);
            }

            territory.ownerClanId = clanId;
            territory.lastCaptureTime = DateTime.UtcNow;
            clan.ownedTerritories.Add(territoryId);

            OnTerritoryCapture?.Invoke(territoryId, clanId);
            NotifyTerritoryCapture ClientRpc(territoryId, clanId);
        }

        [ClientRpc]
        private void NotifyTerritoryCaptureClientRpc(string territoryId, string clanId)
        {
            Debug.Log($"[ClanWars] Territory {territoryId} captured by {clanId}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void JoinClanServerRpc(ulong playerId, string clanId, ServerRpcParams rpcParams = default)
        {
            if (!clans.ContainsKey(clanId)) return;

            var clan = clans[clanId];

            if (!clan.members.Contains(playerId))
            {
                clan.members.Add(playerId);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void LeaveClanServerRpc(ulong playerId, string clanId, ServerRpcParams rpcParams = default)
        {
            if (!clans.ContainsKey(clanId)) return;

            var clan = clans[clanId];
            clan.members.Remove(playerId);
            clan.officers.Remove(playerId);

            if (clan.members.Count == 0)
            {
                // Disband clan
                clans.Remove(clanId);
            }
        }

        // Getters

        public ClanData GetClan(string clanId)
        {
            return clans.ContainsKey(clanId) ? clans[clanId] : null;
        }

        public List<ClanData> GetAllClans()
        {
            return clans.Values.ToList();
        }

        public List<ClanData> GetTopClans(int count)
        {
            return clans.Values.OrderByDescending(c => c.warPoints).Take(count).ToList();
        }

        public List<Territory> GetClanTerritories(string clanId)
        {
            return territories.Values.Where(t => t.ownerClanId == clanId).ToList();
        }

        public List<ActiveWar> GetActiveWars()
        {
            return activeWars.Values.ToList();
        }

        public int GetActiveWarsCount()
        {
            return activeWarsCount.Value;
        }
    }
}
