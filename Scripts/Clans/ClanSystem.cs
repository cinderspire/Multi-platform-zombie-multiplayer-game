using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Clans
{
    /// <summary>
    /// Comprehensive clan/guild system with ranks, permissions, territories, wars, clan perks,
    /// treasury management, and clan progression. Includes complete clan bonus systems.
    /// </summary>
    public class ClanSystem : NetworkBehaviour
    {
        public static ClanSystem Instance { get; private set; }

        [Header("Clan Configuration")]
        [SerializeField] private int maxClanMembers = 50;
        [SerializeField] private int clanCreationCost = 10000;
        [SerializeField] private int maxClanLevel = 20;
        [SerializeField] private bool enableClanWars = true;
        [SerializeField] private bool enableTerritories = true;

        // Clan data
        private Dictionary<string, Clan> clanDatabase = new Dictionary<string, Clan>();
        private Dictionary<ulong, string> playerClanMembership = new Dictionary<ulong, string>();
        private Dictionary<string, ClanWar> activeClanWars = new Dictionary<string, ClanWar>();
        private Dictionary<string, Territory> territoryDatabase = new Dictionary<string, Territory>();

        // Events
        public event Action<string> OnClanCreated;
        public event Action<ulong, string> OnPlayerJoinedClan;
        public event Action<ulong, string> OnPlayerLeftClan;
        public event Action<string, int> OnClanLevelUp;
        public event Action<string, string> OnClanWarStarted;
        public event Action<string, string> OnTerritoryCapture;

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
                InitializeTerritories();
            }
        }

        private void InitializeTerritories()
        {
            // Strategic locations
            territoryDatabase["territory_city_center"] = new Territory
            {
                territoryId = "territory_city_center",
                territoryName = "City Center",
                description = "Prime urban location with high loot density",
                position = new Vector3(0, 0, 0),
                radius = 500f,
                capturePoints = 10000,
                bonuses = new TerritoryBonuses
                {
                    xpBonus = 0.25f,
                    lootBonus = 0.30f,
                    resourceGeneration = 100
                },
                defenseDifficulty = TerritoryDifficulty.Hard
            };

            territoryDatabase["territory_military_base"] = new Territory
            {
                territoryId = "territory_military_base",
                territoryName = "Military Base",
                description = "Strategic military installation",
                position = new Vector3(1000, 0, 500),
                radius = 400f,
                capturePoints = 15000,
                bonuses = new TerritoryBonuses
                {
                    damageBonus = 0.20f,
                    weaponDropRate = 0.40f,
                    resourceGeneration = 150
                },
                defenseDifficulty = TerritoryDifficulty.VeryHard
            };

            territoryDatabase["territory_industrial"] = new Territory
            {
                territoryId = "territory_industrial",
                territoryName = "Industrial District",
                description = "Manufacturing zone with crafting bonuses",
                position = new Vector3(-800, 0, 200),
                radius = 450f,
                capturePoints = 8000,
                bonuses = new TerritoryBonuses
                {
                    craftingSpeedBonus = 0.30f,
                    materialDropRate = 0.50f,
                    resourceGeneration = 120
                },
                defenseDifficulty = TerritoryDifficulty.Medium
            };

            territoryDatabase["territory_research_lab"] = new Territory
            {
                territoryId = "territory_research_lab",
                territoryName = "Research Laboratory",
                description = "Advanced facility with technology bonuses",
                position = new Vector3(600, 0, -700),
                radius = 300f,
                capturePoints = 12000,
                bonuses = new TerritoryBonuses
                {
                    xpBonus = 0.35f,
                    blueprintDropRate = 0.25f,
                    resourceGeneration = 80
                },
                defenseDifficulty = TerritoryDifficulty.Hard
            };

            territoryDatabase["territory_safehouse"] = new Territory
            {
                territoryId = "territory_safehouse",
                territoryName = "Fortified Safehouse",
                description = "Secure location for clan operations",
                position = new Vector3(-400, 0, -600),
                radius = 350f,
                capturePoints = 6000,
                bonuses = new TerritoryBonuses
                {
                    defenseBonus = 0.25f,
                    healingBonus = 0.40f,
                    resourceGeneration = 60
                },
                defenseDifficulty = TerritoryDifficulty.Easy
            };

            Debug.Log($"Initialized {territoryDatabase.Count} territories");
        }

        // Main clan operations
        [ServerRpc(RequireOwnership = false)]
        public void CreateClanServerRpc(ulong leaderId, string clanName, string clanTag, string description, ServerRpcParams rpcParams = default)
        {
            // Validate clan name
            if (clanDatabase.Values.Any(c => c.clanName == clanName))
            {
                Debug.LogWarning($"Clan name {clanName} already exists");
                return;
            }

            // Check if player is already in a clan
            if (playerClanMembership.ContainsKey(leaderId))
            {
                Debug.LogWarning($"Player {leaderId} is already in a clan");
                return;
            }

            // Check currency (would integrate with economy system)
            // Economy.EconomyManager.Instance.RemoveSoftCurrency(leaderId, clanCreationCost);

            string clanId = Guid.NewGuid().ToString();

            var clan = new Clan
            {
                clanId = clanId,
                clanName = clanName,
                clanTag = clanTag,
                description = description,
                leaderId = leaderId,
                members = new Dictionary<ulong, ClanMember>(),
                ranks = InitializeClanRanks(),
                level = 1,
                experience = 0,
                treasury = 0,
                perks = new List<string>(),
                ownedTerritories = new List<string>(),
                allyClans = new List<string>(),
                enemyClans = new List<string>(),
                statistics = new ClanStatistics
                {
                    totalKills = 0,
                    totalDeaths = 0,
                    warsWon = 0,
                    warsLost = 0,
                    territoriesCapture = 0
                },
                creationDate = DateTime.UtcNow
            };

            // Add leader as member
            clan.members[leaderId] = new ClanMember
            {
                playerId = leaderId,
                rankId = "leader",
                joinDate = DateTime.UtcNow,
                contribution = 0,
                lastActive = DateTime.UtcNow
            };

            clanDatabase[clanId] = clan;
            playerClanMembership[leaderId] = clanId;

            OnClanCreated?.Invoke(clanId);
            NotifyClanCreatedClientRpc(clanId, clanName);

            Debug.Log($"Clan {clanName} [{clanTag}] created by player {leaderId}");
        }

        private Dictionary<string, ClanRank> InitializeClanRanks()
        {
            var ranks = new Dictionary<string, ClanRank>();

            ranks["leader"] = new ClanRank
            {
                rankId = "leader",
                rankName = "Leader",
                rankLevel = 10,
                permissions = new ClanPermissions
                {
                    canInvite = true,
                    canKick = true,
                    canPromote = true,
                    canDeclareWar = true,
                    canManageTreasury = true,
                    canManageTerritories = true,
                    canEditClan = true,
                    canDisbandClan = true
                }
            };

            ranks["officer"] = new ClanRank
            {
                rankId = "officer",
                rankName = "Officer",
                rankLevel = 8,
                permissions = new ClanPermissions
                {
                    canInvite = true,
                    canKick = true,
                    canPromote = false,
                    canDeclareWar = false,
                    canManageTreasury = true,
                    canManageTerritories = true,
                    canEditClan = false,
                    canDisbandClan = false
                }
            };

            ranks["elite"] = new ClanRank
            {
                rankId = "elite",
                rankName = "Elite",
                rankLevel = 6,
                permissions = new ClanPermissions
                {
                    canInvite = true,
                    canKick = false,
                    canPromote = false,
                    canDeclareWar = false,
                    canManageTreasury = false,
                    canManageTerritories = true,
                    canEditClan = false,
                    canDisbandClan = false
                }
            };

            ranks["veteran"] = new ClanRank
            {
                rankId = "veteran",
                rankName = "Veteran",
                rankLevel = 4,
                permissions = new ClanPermissions
                {
                    canInvite = true,
                    canKick = false,
                    canPromote = false,
                    canDeclareWar = false,
                    canManageTreasury = false,
                    canManageTerritories = false,
                    canEditClan = false,
                    canDisbandClan = false
                }
            };

            ranks["member"] = new ClanRank
            {
                rankId = "member",
                rankName = "Member",
                rankLevel = 2,
                permissions = new ClanPermissions
                {
                    canInvite = false,
                    canKick = false,
                    canPromote = false,
                    canDeclareWar = false,
                    canManageTreasury = false,
                    canManageTerritories = false,
                    canEditClan = false,
                    canDisbandClan = false
                }
            };

            ranks["recruit"] = new ClanRank
            {
                rankId = "recruit",
                rankName = "Recruit",
                rankLevel = 1,
                permissions = new ClanPermissions()
            };

            return ranks;
        }

        [ServerRpc(RequireOwnership = false)]
        public void InvitePlayerServerRpc(ulong inviterId, ulong inviteeId, ServerRpcParams rpcParams = default)
        {
            if (!playerClanMembership.TryGetValue(inviterId, out string clanId)) return;
            if (!clanDatabase.TryGetValue(clanId, out var clan)) return;

            // Check permissions
            if (!clan.members.TryGetValue(inviterId, out var inviterMember)) return;
            if (!clan.ranks[inviterMember.rankId].permissions.canInvite)
            {
                Debug.LogWarning($"Player {inviterId} doesn't have invite permission");
                return;
            }

            // Check if invitee is already in a clan
            if (playerClanMembership.ContainsKey(inviteeId))
            {
                Debug.LogWarning($"Player {inviteeId} is already in a clan");
                return;
            }

            // Check member limit
            if (clan.members.Count >= maxClanMembers)
            {
                Debug.LogWarning($"Clan {clan.clanName} is full");
                return;
            }

            // Send invitation (would integrate with notification system)
            NotifyClanInviteClientRpc(inviteeId, clanId, clan.clanName);

            Debug.Log($"Player {inviterId} invited player {inviteeId} to clan {clan.clanName}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void AcceptClanInviteServerRpc(ulong playerId, string clanId, ServerRpcParams rpcParams = default)
        {
            if (!clanDatabase.TryGetValue(clanId, out var clan)) return;
            if (playerClanMembership.ContainsKey(playerId)) return;
            if (clan.members.Count >= maxClanMembers) return;

            var member = new ClanMember
            {
                playerId = playerId,
                rankId = "recruit",
                joinDate = DateTime.UtcNow,
                contribution = 0,
                lastActive = DateTime.UtcNow
            };

            clan.members[playerId] = member;
            playerClanMembership[playerId] = clanId;

            OnPlayerJoinedClan?.Invoke(playerId, clanId);
            NotifyPlayerJoinedClanClientRpc(playerId, clanId);

            Debug.Log($"Player {playerId} joined clan {clan.clanName}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void LeaveClanServerRpc(ulong playerId, ServerRpcParams rpcParams = default)
        {
            if (!playerClanMembership.TryGetValue(playerId, out string clanId)) return;
            if (!clanDatabase.TryGetValue(clanId, out var clan)) return;

            // Can't leave if you're the leader and there are other members
            if (clan.leaderId == playerId && clan.members.Count > 1)
            {
                Debug.LogWarning($"Leader must transfer leadership before leaving");
                return;
            }

            clan.members.Remove(playerId);
            playerClanMembership.Remove(playerId);

            // Disband clan if leader leaves and clan is empty
            if (clan.leaderId == playerId && clan.members.Count == 0)
            {
                clanDatabase.Remove(clanId);
            }

            OnPlayerLeftClan?.Invoke(playerId, clanId);

            Debug.Log($"Player {playerId} left clan {clan.clanName}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void PromoteMemberServerRpc(ulong promoterId, ulong memberId, string newRankId, ServerRpcParams rpcParams = default)
        {
            if (!playerClanMembership.TryGetValue(promoterId, out string clanId)) return;
            if (!clanDatabase.TryGetValue(clanId, out var clan)) return;

            // Check permissions
            if (!clan.members.TryGetValue(promoterId, out var promoterMember)) return;
            if (!clan.ranks[promoterMember.rankId].permissions.canPromote)
            {
                Debug.LogWarning($"Player {promoterId} doesn't have promote permission");
                return;
            }

            if (!clan.members.TryGetValue(memberId, out var member)) return;

            member.rankId = newRankId;

            Debug.Log($"Player {memberId} promoted to {newRankId} in clan {clan.clanName}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void AddClanExperienceServerRpc(string clanId, int xpAmount, ServerRpcParams rpcParams = default)
        {
            if (!clanDatabase.TryGetValue(clanId, out var clan)) return;

            clan.experience += xpAmount;

            // Check for level up
            int xpRequired = clan.level * 10000;
            if (clan.experience >= xpRequired && clan.level < maxClanLevel)
            {
                clan.experience -= xpRequired;
                clan.level++;

                OnClanLevelUp?.Invoke(clanId, clan.level);
                NotifyClanLevelUpClientRpc(clanId, clan.level);

                Debug.Log($"Clan {clan.clanName} leveled up to {clan.level}");
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void DonateToTreasuryServerRpc(ulong playerId, int amount, ServerRpcParams rpcParams = default)
        {
            if (!playerClanMembership.TryGetValue(playerId, out string clanId)) return;
            if (!clanDatabase.TryGetValue(clanId, out var clan)) return;

            // Would integrate with economy system
            // Economy.EconomyManager.Instance.RemoveSoftCurrency(playerId, amount);

            clan.treasury += amount;

            if (clan.members.TryGetValue(playerId, out var member))
            {
                member.contribution += amount;
            }

            Debug.Log($"Player {playerId} donated {amount} to clan {clan.clanName} treasury");
        }

        // Clan Wars
        [ServerRpc(RequireOwnership = false)]
        public void DeclareWarServerRpc(ulong declarerId, string targetClanId, ServerRpcParams rpcParams = default)
        {
            if (!enableClanWars) return;
            if (!playerClanMembership.TryGetValue(declarerId, out string attackerClanId)) return;
            if (!clanDatabase.TryGetValue(attackerClanId, out var attackerClan)) return;
            if (!clanDatabase.TryGetValue(targetClanId, out var targetClan)) return;

            // Check permissions
            if (!attackerClan.members.TryGetValue(declarerId, out var member)) return;
            if (!attackerClan.ranks[member.rankId].permissions.canDeclareWar)
            {
                Debug.LogWarning($"Player {declarerId} doesn't have war declaration permission");
                return;
            }

            string warId = $"war_{attackerClanId}_{targetClanId}_{DateTime.UtcNow.Ticks}";

            var war = new ClanWar
            {
                warId = warId,
                attackerClanId = attackerClanId,
                defenderClanId = targetClanId,
                startTime = DateTime.UtcNow,
                endTime = DateTime.UtcNow.AddDays(7),
                attackerScore = 0,
                defenderScore = 0,
                status = WarStatus.Active
            };

            activeClanWars[warId] = war;

            attackerClan.enemyClans.Add(targetClanId);
            targetClan.enemyClans.Add(attackerClanId);

            OnClanWarStarted?.Invoke(attackerClanId, targetClanId);
            NotifyClanWarStartedClientRpc(attackerClanId, targetClanId);

            Debug.Log($"War declared between {attackerClan.clanName} and {targetClan.clanName}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void RecordWarKillServerRpc(ulong killerId, ulong victimId, ServerRpcParams rpcParams = default)
        {
            if (!playerClanMembership.TryGetValue(killerId, out string killerClanId)) return;
            if (!playerClanMembership.TryGetValue(victimId, out string victimClanId)) return;

            // Find active war between these clans
            var war = activeClanWars.Values.FirstOrDefault(w =>
                (w.attackerClanId == killerClanId && w.defenderClanId == victimClanId) ||
                (w.defenderClanId == killerClanId && w.attackerClanId == victimClanId));

            if (war == null) return;

            // Add score
            if (war.attackerClanId == killerClanId)
            {
                war.attackerScore += 1;
            }
            else
            {
                war.defenderScore += 1;
            }
        }

        // Territory system
        [ServerRpc(RequireOwnership = false)]
        public void CaptureTerritoryServerRpc(string clanId, string territoryId, int contributedPoints, ServerRpcParams rpcParams = default)
        {
            if (!enableTerritories) return;
            if (!clanDatabase.TryGetValue(clanId, out var clan)) return;
            if (!territoryDatabase.TryGetValue(territoryId, out var territory)) return;

            // Add capture progress
            if (!territory.captureProgress.ContainsKey(clanId))
            {
                territory.captureProgress[clanId] = 0;
            }

            territory.captureProgress[clanId] += contributedPoints;

            // Check if captured
            if (territory.captureProgress[clanId] >= territory.capturePoints)
            {
                // Remove from previous owner
                if (!string.IsNullOrEmpty(territory.ownerClanId))
                {
                    if (clanDatabase.TryGetValue(territory.ownerClanId, out var previousOwner))
                    {
                        previousOwner.ownedTerritories.Remove(territoryId);
                    }
                }

                // Assign new owner
                territory.ownerClanId = clanId;
                territory.captureTime = DateTime.UtcNow;
                clan.ownedTerritories.Add(territoryId);

                // Reset capture progress
                territory.captureProgress.Clear();

                // Update stats
                clan.statistics.territoriesCapture++;

                OnTerritoryCapture?.Invoke(clanId, territoryId);
                NotifyTerritoryCaptureClientRpc(clanId, territoryId);

                Debug.Log($"Clan {clan.clanName} captured territory {territory.territoryName}");
            }
        }

        public ClanBonuses CalculateClanBonuses(string clanId)
        {
            if (!clanDatabase.TryGetValue(clanId, out var clan)) return new ClanBonuses();

            var bonuses = new ClanBonuses();

            // Level bonuses
            bonuses.xpBonus += clan.level * 0.01f;
            bonuses.damageBonus += clan.level * 0.005f;

            // Territory bonuses
            foreach (var territoryId in clan.ownedTerritories)
            {
                if (territoryDatabase.TryGetValue(territoryId, out var territory))
                {
                    bonuses.xpBonus += territory.bonuses.xpBonus;
                    bonuses.damageBonus += territory.bonuses.damageBonus;
                    bonuses.lootBonus += territory.bonuses.lootBonus;
                    bonuses.craftingSpeedBonus += territory.bonuses.craftingSpeedBonus;
                }
            }

            return bonuses;
        }

        // Client RPCs
        [ClientRpc]
        private void NotifyClanCreatedClientRpc(string clanId, string clanName) { }

        [ClientRpc]
        private void NotifyClanInviteClientRpc(ulong playerId, string clanId, string clanName) { }

        [ClientRpc]
        private void NotifyPlayerJoinedClanClientRpc(ulong playerId, string clanId) { }

        [ClientRpc]
        private void NotifyClanLevelUpClientRpc(string clanId, int newLevel) { }

        [ClientRpc]
        private void NotifyClanWarStartedClientRpc(string attackerClanId, string defenderClanId) { }

        [ClientRpc]
        private void NotifyTerritoryCaptureClientRpc(string clanId, string territoryId) { }

        // Public getters
        public Clan GetClan(string clanId) => clanDatabase.GetValueOrDefault(clanId);
        public string GetPlayerClan(ulong playerId) => playerClanMembership.GetValueOrDefault(playerId);
        public List<Clan> GetAllClans() => clanDatabase.Values.ToList();
        public Territory GetTerritory(string territoryId) => territoryDatabase.GetValueOrDefault(territoryId);
    }

    // Data structures
    [Serializable]
    public class Clan
    {
        public string clanId;
        public string clanName;
        public string clanTag;
        public string description;
        public ulong leaderId;
        public Dictionary<ulong, ClanMember> members;
        public Dictionary<string, ClanRank> ranks;
        public int level;
        public int experience;
        public int treasury;
        public List<string> perks;
        public List<string> ownedTerritories;
        public List<string> allyClans;
        public List<string> enemyClans;
        public ClanStatistics statistics;
        public DateTime creationDate;
    }

    [Serializable]
    public class ClanMember
    {
        public ulong playerId;
        public string rankId;
        public DateTime joinDate;
        public int contribution;
        public DateTime lastActive;
    }

    [Serializable]
    public class ClanRank
    {
        public string rankId;
        public string rankName;
        public int rankLevel;
        public ClanPermissions permissions;
    }

    [Serializable]
    public class ClanPermissions
    {
        public bool canInvite;
        public bool canKick;
        public bool canPromote;
        public bool canDeclareWar;
        public bool canManageTreasury;
        public bool canManageTerritories;
        public bool canEditClan;
        public bool canDisbandClan;
    }

    [Serializable]
    public class ClanStatistics
    {
        public int totalKills;
        public int totalDeaths;
        public int warsWon;
        public int warsLost;
        public int territoriesCapture;
    }

    [Serializable]
    public class ClanWar
    {
        public string warId;
        public string attackerClanId;
        public string defenderClanId;
        public DateTime startTime;
        public DateTime endTime;
        public int attackerScore;
        public int defenderScore;
        public WarStatus status;
    }

    [Serializable]
    public class Territory
    {
        public string territoryId;
        public string territoryName;
        public string description;
        public Vector3 position;
        public float radius;
        public int capturePoints;
        public Dictionary<string, int> captureProgress = new Dictionary<string, int>();
        public string ownerClanId;
        public DateTime captureTime;
        public TerritoryBonuses bonuses;
        public TerritoryDifficulty defenseDifficulty;
    }

    [Serializable]
    public class TerritoryBonuses
    {
        public float xpBonus;
        public float damageBonus;
        public float defenseBonus;
        public float lootBonus;
        public float craftingSpeedBonus;
        public float healingBonus;
        public float weaponDropRate;
        public float materialDropRate;
        public float blueprintDropRate;
        public int resourceGeneration;
    }

    [Serializable]
    public class ClanBonuses
    {
        public float xpBonus;
        public float damageBonus;
        public float lootBonus;
        public float craftingSpeedBonus;
    }

    public enum WarStatus { Active, Ended, Cancelled }
    public enum TerritoryDifficulty { Easy, Medium, Hard, VeryHard }
}
