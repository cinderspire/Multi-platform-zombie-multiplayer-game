using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Party
{
    public class PartySystem : NetworkBehaviour
    {
        public static PartySystem Instance { get; private set; }

        [Header("Party Configuration")]
        [SerializeField] private int maxPartySize = 4;
        [SerializeField] private float xpShareRadius = 50f;
        [SerializeField] private float lootShareRadius = 30f;
        [SerializeField] private bool enableRoleSystem = true;

        private Dictionary<string, PartyGroup> activeParties = new Dictionary<string, PartyGroup>();
        private Dictionary<ulong, string> playerPartyAssignments = new Dictionary<ulong, string>();
        private Dictionary<PartyRole, RoleBonuses> roleBonuses = new Dictionary<PartyRole, RoleBonuses>();

        public event Action<string> OnPartyCreated;
        public event Action<ulong, string> OnPlayerJoinedParty;
        public event Action<ulong, string> OnPlayerLeftParty;
        public event Action<ulong, PartyRole> OnRoleAssigned;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer) { InitializeRoleBonuses(); }
        }

        private void InitializeRoleBonuses()
        {
            roleBonuses[PartyRole.Leader] = new RoleBonuses
            {
                role = PartyRole.Leader,
                roleName = "Party Leader",
                description = "Provides tactical advantages to the party",
                xpBonus = 0.15f,
                damageBonus = 0.10f,
                defenseBonus = 0.05f,
                movementBonus = 0.05f,
                lootBonus = 0.10f,
                specialAbilities = new List<string> { "mark_target", "rally", "tactical_retreat" }
            };

            roleBonuses[PartyRole.Tank] = new RoleBonuses
            {
                role = PartyRole.Tank,
                roleName = "Tank",
                description = "Absorbs damage and protects allies",
                xpBonus = 0.05f,
                damageBonus = 0.05f,
                defenseBonus = 0.30f,
                healthBonus = 0.25f,
                movementBonus = -0.05f,
                specialAbilities = new List<string> { "taunt", "shield_wall", "last_stand" }
            };

            roleBonuses[PartyRole.DPS] = new RoleBonuses
            {
                role = PartyRole.DPS,
                roleName = "Damage Dealer",
                description = "Maximizes damage output",
                xpBonus = 0.10f,
                damageBonus = 0.25f,
                defenseBonus = -0.05f,
                critChanceBonus = 0.15f,
                reloadSpeedBonus = 0.20f,
                specialAbilities = new List<string> { "berserker", "focus_fire", "death_mark" }
            };

            roleBonuses[PartyRole.Support] = new RoleBonuses
            {
                role = PartyRole.Support,
                roleName = "Support",
                description = "Heals and buffs allies",
                xpBonus = 0.12f,
                defenseBonus = 0.10f,
                healingBonus = 0.40f,
                reviveSpeedBonus = 0.50f,
                specialAbilities = new List<string> { "heal_burst", "buff_allies", "revive_nearby" }
            };

            roleBonuses[PartyRole.Scout] = new RoleBonuses
            {
                role = PartyRole.Scout,
                roleName = "Scout",
                description = "Provides vision and mobility",
                xpBonus = 0.08f,
                damageBonus = 0.08f,
                movementBonus = 0.25f,
                stealthBonus = 0.30f,
                lootBonus = 0.20f,
                specialAbilities = new List<string> { "reveal_area", "mark_loot", "quick_escape" }
            };
        }

        [ServerRpc(RequireOwnership = false)]
        public void CreatePartyServerRpc(ulong leaderId, string partyName, ServerRpcParams rpcParams = default)
        {
            if (playerPartyAssignments.ContainsKey(leaderId))
            {
                Debug.LogWarning($"Player {leaderId} is already in a party");
                return;
            }

            var party = new PartyGroup
            {
                partyId = $"party_{Guid.NewGuid()}",
                partyName = partyName,
                leaderId = leaderId,
                members = new List<PartyMember>
                {
                    new PartyMember
                    {
                        playerId = leaderId,
                        role = PartyRole.Leader,
                        joinDate = DateTime.UtcNow,
                        contributionScore = 0
                    }
                },
                creationDate = DateTime.UtcNow,
                settings = new PartySettings
                {
                    isPublic = false,
                    autoAccept = false,
                    xpShare = true,
                    lootShare = false,
                    friendlyFire = false
                },
                bonuses = CalculatePartyBonuses(new List<PartyMember> { new PartyMember { playerId = leaderId, role = PartyRole.Leader } })
            };

            activeParties[party.partyId] = party;
            playerPartyAssignments[leaderId] = party.partyId;

            OnPartyCreated?.Invoke(party.partyId);
            Debug.Log($"Party created: {partyName} by player {leaderId}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void InvitePlayerServerRpc(string partyId, ulong targetPlayerId, ServerRpcParams rpcParams = default)
        {
            if (!activeParties.TryGetValue(partyId, out var party)) return;
            if (party.members.Count >= maxPartySize)
            {
                Debug.LogWarning("Party is full");
                return;
            }

            // Send invite notification to target player
            NotifyInviteClientRpc(targetPlayerId, partyId, party.partyName);
        }

        [ServerRpc(RequireOwnership = false)]
        public void JoinPartyServerRpc(ulong playerId, string partyId, ServerRpcParams rpcParams = default)
        {
            if (playerPartyAssignments.ContainsKey(playerId))
            {
                Debug.LogWarning($"Player {playerId} is already in a party");
                return;
            }

            if (!activeParties.TryGetValue(partyId, out var party)) return;
            if (party.members.Count >= maxPartySize)
            {
                Debug.LogWarning("Party is full");
                return;
            }

            var member = new PartyMember
            {
                playerId = playerId,
                role = PartyRole.None,
                joinDate = DateTime.UtcNow,
                contributionScore = 0
            };

            party.members.Add(member);
            playerPartyAssignments[playerId] = partyId;

            // Recalculate bonuses
            party.bonuses = CalculatePartyBonuses(party.members);

            OnPlayerJoinedParty?.Invoke(playerId, partyId);
            NotifyPartyMembersClientRpc(partyId);

            Debug.Log($"Player {playerId} joined party {party.partyName}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void LeavePartyServerRpc(ulong playerId, ServerRpcParams rpcParams = default)
        {
            if (!playerPartyAssignments.TryGetValue(playerId, out var partyId)) return;
            if (!activeParties.TryGetValue(partyId, out var party)) return;

            party.members.RemoveAll(m => m.playerId == playerId);
            playerPartyAssignments.Remove(playerId);

            // If leader left, assign new leader or disband
            if (party.leaderId == playerId)
            {
                if (party.members.Count > 0)
                {
                    var newLeader = party.members[0];
                    party.leaderId = newLeader.playerId;
                    newLeader.role = PartyRole.Leader;
                }
                else
                {
                    DisbandParty(partyId);
                    return;
                }
            }

            // Recalculate bonuses
            party.bonuses = CalculatePartyBonuses(party.members);

            OnPlayerLeftParty?.Invoke(playerId, partyId);
            NotifyPartyMembersClientRpc(partyId);

            Debug.Log($"Player {playerId} left party");
        }

        [ServerRpc(RequireOwnership = false)]
        public void AssignRoleServerRpc(ulong requesterId, string partyId, ulong targetPlayerId, PartyRole role, ServerRpcParams rpcParams = default)
        {
            if (!activeParties.TryGetValue(partyId, out var party)) return;
            if (party.leaderId != requesterId)
            {
                Debug.LogWarning("Only party leader can assign roles");
                return;
            }

            var member = party.members.FirstOrDefault(m => m.playerId == targetPlayerId);
            if (member == null) return;

            // Check if role is already taken (except Leader and None)
            if (role != PartyRole.Leader && role != PartyRole.None)
            {
                if (party.members.Any(m => m.role == role && m.playerId != targetPlayerId))
                {
                    Debug.LogWarning($"Role {role} is already assigned");
                    return;
                }
            }

            member.role = role;

            // Recalculate bonuses
            party.bonuses = CalculatePartyBonuses(party.members);

            OnRoleAssigned?.Invoke(targetPlayerId, role);
            NotifyRoleAssignedClientRpc(targetPlayerId, role);

            Debug.Log($"Assigned role {role} to player {targetPlayerId}");
        }

        private PartyBonuses CalculatePartyBonuses(List<PartyMember> members)
        {
            var bonuses = new PartyBonuses();

            foreach (var member in members)
            {
                if (member.role != PartyRole.None && roleBonuses.TryGetValue(member.role, out var roleBonus))
                {
                    bonuses.totalXpBonus += roleBonus.xpBonus;
                    bonuses.totalDamageBonus += roleBonus.damageBonus;
                    bonuses.totalDefenseBonus += roleBonus.defenseBonus;
                    bonuses.totalHealthBonus += roleBonus.healthBonus;
                    bonuses.totalMovementBonus += roleBonus.movementBonus;
                    bonuses.totalLootBonus += roleBonus.lootBonus;
                }
            }

            // Party size bonus
            int partySize = members.Count;
            bonuses.partySizeBonus = (partySize - 1) * 0.05f; // 5% per additional member

            return bonuses;
        }

        [ServerRpc(RequireOwnership = false)]
        public void UpdateContributionServerRpc(ulong playerId, int contributionAmount, ServerRpcParams rpcParams = default)
        {
            if (!playerPartyAssignments.TryGetValue(playerId, out var partyId)) return;
            if (!activeParties.TryGetValue(partyId, out var party)) return;

            var member = party.members.FirstOrDefault(m => m.playerId == playerId);
            if (member != null)
            {
                member.contributionScore += contributionAmount;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void UpdatePartySettingsServerRpc(ulong requesterId, string partyId, PartySettings settings, ServerRpcParams rpcParams = default)
        {
            if (!activeParties.TryGetValue(partyId, out var party)) return;
            if (party.leaderId != requesterId)
            {
                Debug.LogWarning("Only party leader can change settings");
                return;
            }

            party.settings = settings;
            NotifyPartyMembersClientRpc(partyId);
        }

        private void DisbandParty(string partyId)
        {
            if (!activeParties.TryGetValue(partyId, out var party)) return;

            foreach (var member in party.members)
            {
                playerPartyAssignments.Remove(member.playerId);
            }

            activeParties.Remove(partyId);
            Debug.Log($"Party {party.partyName} disbanded");
        }

        [ClientRpc]
        private void NotifyInviteClientRpc(ulong playerId, string partyId, string partyName)
        {
            // Client-side invitation notification
        }

        [ClientRpc]
        private void NotifyPartyMembersClientRpc(string partyId)
        {
            // Notify all party members of changes
        }

        [ClientRpc]
        private void NotifyRoleAssignedClientRpc(ulong playerId, PartyRole role)
        {
            OnRoleAssigned?.Invoke(playerId, role);
        }

        public PartyGroup GetParty(string partyId) => activeParties.GetValueOrDefault(partyId);
        public string GetPlayerParty(ulong playerId) => playerPartyAssignments.GetValueOrDefault(playerId);
        public List<PartyGroup> GetPublicParties() => activeParties.Values.Where(p => p.settings.isPublic).ToList();
    }

    [Serializable]
    public class PartyGroup
    {
        public string partyId;
        public string partyName;
        public ulong leaderId;
        public List<PartyMember> members;
        public DateTime creationDate;
        public PartySettings settings;
        public PartyBonuses bonuses;
    }

    [Serializable]
    public class PartyMember
    {
        public ulong playerId;
        public PartyRole role;
        public DateTime joinDate;
        public int contributionScore;
    }

    [Serializable]
    public class PartySettings
    {
        public bool isPublic;
        public bool autoAccept;
        public bool xpShare;
        public bool lootShare;
        public bool friendlyFire;
    }

    [Serializable]
    public class PartyBonuses
    {
        public float totalXpBonus;
        public float totalDamageBonus;
        public float totalDefenseBonus;
        public float totalHealthBonus;
        public float totalMovementBonus;
        public float totalLootBonus;
        public float partySizeBonus;
    }

    [Serializable]
    public class RoleBonuses
    {
        public PartyRole role;
        public string roleName;
        public string description;
        public float xpBonus;
        public float damageBonus;
        public float defenseBonus;
        public float healthBonus;
        public float movementBonus;
        public float critChanceBonus;
        public float reloadSpeedBonus;
        public float healingBonus;
        public float reviveSpeedBonus;
        public float stealthBonus;
        public float lootBonus;
        public List<string> specialAbilities;
    }

    public enum PartyRole { None, Leader, Tank, DPS, Support, Scout }
}
