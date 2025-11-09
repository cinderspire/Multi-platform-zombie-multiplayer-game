using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace DeadFrontier.Social
{
    /// <summary>
    /// Comprehensive clan/guild management system.
    /// Supports clan creation, hierarchy, leveling, perks, clan wars, and shared resources.
    /// Provides social structure and cooperative gameplay incentives.
    /// </summary>
    public class ClanSystem : MonoBehaviour
    {
        public static ClanSystem Instance { get; private set; }

        [Header("Clan Settings")]
        [SerializeField] private int minClanNameLength = 3;
        [SerializeField] private int maxClanNameLength = 20;
        [SerializeField] private int maxClanTagLength = 5;
        [SerializeField] private int maxClanMembers = 50;
        [SerializeField] private int clanCreationCost = 10000;

        [Header("Rank Settings")]
        [SerializeField] private ClanRankData[] defaultRanks;

        [Header("Leveling")]
        [SerializeField] private int maxClanLevel = 50;
        [SerializeField] private AnimationCurve clanXPCurve;
        [SerializeField] private ClanPerkData[] clanPerks;

        [Header("Clan Wars")]
        [SerializeField] private bool enableClanWars = true;
        [SerializeField] private int minMembersForWar = 5;
        [SerializeField] private float warPreparationTime = 3600f; // 1 hour
        [SerializeField] private float warDuration = 7200f; // 2 hours

        // Clans
        private Dictionary<string, Clan> clans = new Dictionary<string, Clan>();
        private Dictionary<ulong, string> playerClanMembership = new Dictionary<ulong, string>();

        // Clan invites
        private Dictionary<ulong, List<ClanInvite>> pendingInvites = new Dictionary<ulong, List<ClanInvite>>();

        // Clan wars
        private Dictionary<string, ClanWar> activeClanWars = new Dictionary<string, ClanWar>();

        // Events
        public event Action<Clan> OnClanCreated;
        public event Action<Clan> OnClanDisbanded;
        public event Action<string, ulong> OnMemberJoined;
        public event Action<string, ulong> OnMemberLeft;
        public event Action<string, ulong> OnMemberPromoted;
        public event Action<string, ulong> OnMemberDemoted;
        public event Action<string, int> OnClanLevelUp;
        public event Action<ClanWar> OnClanWarStarted;
        public event Action<ClanWar, string> OnClanWarEnded;

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
            LoadAllClans();
        }

        private void Update()
        {
            UpdateClanWars();
        }

        #region Clan Creation

        public bool CanCreateClan(ulong playerId, string clanName, string clanTag)
        {
            // Check if player is already in a clan
            if (playerClanMembership.ContainsKey(playerId)) return false;

            // Check name length
            if (clanName.Length < minClanNameLength || clanName.Length > maxClanNameLength) return false;

            // Check tag length
            if (clanTag.Length > maxClanTagLength) return false;

            // Check if name/tag already exists
            if (clans.Values.Any(c => c.clanName == clanName || c.clanTag == clanTag)) return false;

            // Check currency
            if (Economy.EconomyManager.Instance != null)
            {
                if (!Economy.EconomyManager.Instance.CanAfford(clanCreationCost))
                    return false;
            }

            return true;
        }

        public Clan CreateClan(ulong creatorId, string clanName, string clanTag, string clanDescription = "")
        {
            if (!CanCreateClan(creatorId, clanName, clanTag)) return null;

            // Charge creation fee
            if (Economy.EconomyManager.Instance != null)
            {
                if (!Economy.EconomyManager.Instance.SpendSoftCurrency(clanCreationCost, "Clan Creation"))
                    return null;
            }

            string clanId = $"clan_{DateTime.UtcNow.Ticks}";

            var clan = new Clan
            {
                clanId = clanId,
                clanName = clanName,
                clanTag = clanTag,
                clanDescription = clanDescription,
                leaderId = creatorId,
                creationTime = DateTime.UtcNow,
                level = 1,
                xp = 0,
                members = new List<ClanMember>(),
                ranks = new List<ClanRank>(defaultRanks.Select(r => new ClanRank
                {
                    rankId = r.rankId,
                    rankName = r.rankName,
                    permissions = new List<ClanPermission>(r.permissions)
                })),
                treasury = 0,
                perks = new List<string>()
            };

            // Add creator as leader
            var leaderMember = new ClanMember
            {
                playerId = creatorId,
                rankId = "leader",
                joinDate = DateTime.UtcNow,
                contributedXP = 0,
                contributedCurrency = clanCreationCost
            };

            clan.members.Add(leaderMember);

            clans[clanId] = clan;
            playerClanMembership[creatorId] = clanId;

            SaveClan(clanId);

            OnClanCreated?.Invoke(clan);

            Debug.Log($"[ClanSystem] Clan created: {clanName} [{clanTag}] by player {creatorId}");

            return clan;
        }

        public bool DisbandClan(string clanId, ulong requesterId)
        {
            if (!clans.ContainsKey(clanId)) return false;

            var clan = clans[clanId];

            // Only leader can disband
            if (clan.leaderId != requesterId) return false;

            // Remove all members
            foreach (var member in clan.members)
            {
                playerClanMembership.Remove(member.playerId);
            }

            clans.Remove(clanId);

            OnClanDisbanded?.Invoke(clan);

            Debug.Log($"[ClanSystem] Clan disbanded: {clan.clanName}");

            return true;
        }

        #endregion

        #region Member Management

        public bool InvitePlayer(string clanId, ulong inviterId, ulong inviteeId)
        {
            if (!clans.ContainsKey(clanId)) return false;

            var clan = clans[clanId];

            // Check if inviter has permission
            if (!HasPermission(clanId, inviterId, ClanPermission.InviteMembers)) return false;

            // Check if invitee is already in a clan
            if (playerClanMembership.ContainsKey(inviteeId)) return false;

            // Check clan size
            if (clan.members.Count >= maxClanMembers) return false;

            // Create invite
            var invite = new ClanInvite
            {
                clanId = clanId,
                inviterId = inviterId,
                inviteeId = inviteeId,
                inviteTime = DateTime.UtcNow,
                expirationTime = DateTime.UtcNow.AddDays(7)
            };

            if (!pendingInvites.ContainsKey(inviteeId))
            {
                pendingInvites[inviteeId] = new List<ClanInvite>();
            }

            pendingInvites[inviteeId].Add(invite);

            Debug.Log($"[ClanSystem] Player {inviteeId} invited to clan {clan.clanName}");

            return true;
        }

        public bool AcceptInvite(ulong playerId, string clanId)
        {
            if (!pendingInvites.ContainsKey(playerId)) return false;

            var invite = pendingInvites[playerId].FirstOrDefault(i => i.clanId == clanId);
            if (invite == null) return false;

            // Check expiration
            if (DateTime.UtcNow > invite.expirationTime)
            {
                pendingInvites[playerId].Remove(invite);
                return false;
            }

            // Join clan
            if (JoinClan(playerId, clanId))
            {
                pendingInvites[playerId].Remove(invite);
                return true;
            }

            return false;
        }

        private bool JoinClan(ulong playerId, string clanId)
        {
            if (!clans.ContainsKey(clanId)) return false;
            if (playerClanMembership.ContainsKey(playerId)) return false;

            var clan = clans[clanId];

            if (clan.members.Count >= maxClanMembers) return false;

            var member = new ClanMember
            {
                playerId = playerId,
                rankId = "member", // Default rank
                joinDate = DateTime.UtcNow,
                contributedXP = 0,
                contributedCurrency = 0
            };

            clan.members.Add(member);
            playerClanMembership[playerId] = clanId;

            SaveClan(clanId);

            OnMemberJoined?.Invoke(clanId, playerId);

            Debug.Log($"[ClanSystem] Player {playerId} joined clan {clan.clanName}");

            return true;
        }

        public bool LeaveClan(ulong playerId)
        {
            if (!playerClanMembership.ContainsKey(playerId)) return false;

            string clanId = playerClanMembership[playerId];
            var clan = clans[clanId];

            // Leader cannot leave (must transfer or disband)
            if (clan.leaderId == playerId) return false;

            var member = clan.members.FirstOrDefault(m => m.playerId == playerId);
            if (member == null) return false;

            clan.members.Remove(member);
            playerClanMembership.Remove(playerId);

            SaveClan(clanId);

            OnMemberLeft?.Invoke(clanId, playerId);

            Debug.Log($"[ClanSystem] Player {playerId} left clan {clan.clanName}");

            return true;
        }

        public bool KickMember(string clanId, ulong kickerId, ulong targetId)
        {
            if (!clans.ContainsKey(clanId)) return false;

            // Check permission
            if (!HasPermission(clanId, kickerId, ClanPermission.KickMembers)) return false;

            // Cannot kick leader
            var clan = clans[clanId];
            if (clan.leaderId == targetId) return false;

            var member = clan.members.FirstOrDefault(m => m.playerId == targetId);
            if (member == null) return false;

            clan.members.Remove(member);
            playerClanMembership.Remove(targetId);

            SaveClan(clanId);

            Debug.Log($"[ClanSystem] Player {targetId} kicked from clan {clan.clanName}");

            return true;
        }

        #endregion

        #region Rank Management

        public bool PromoteMember(string clanId, ulong promoterId, ulong targetId, string newRankId)
        {
            if (!clans.ContainsKey(clanId)) return false;

            var clan = clans[clanId];

            // Check permission
            if (!HasPermission(clanId, promoterId, ClanPermission.ManageRanks)) return false;

            var member = clan.members.FirstOrDefault(m => m.playerId == targetId);
            if (member == null) return false;

            // Check if rank exists
            if (!clan.ranks.Any(r => r.rankId == newRankId)) return false;

            member.rankId = newRankId;

            SaveClan(clanId);

            OnMemberPromoted?.Invoke(clanId, targetId);

            Debug.Log($"[ClanSystem] Player {targetId} promoted to {newRankId}");

            return true;
        }

        public bool TransferLeadership(string clanId, ulong currentLeader, ulong newLeader)
        {
            if (!clans.ContainsKey(clanId)) return false;

            var clan = clans[clanId];

            // Only current leader can transfer
            if (clan.leaderId != currentLeader) return false;

            // New leader must be a member
            var member = clan.members.FirstOrDefault(m => m.playerId == newLeader);
            if (member == null) return false;

            // Transfer leadership
            clan.leaderId = newLeader;
            member.rankId = "leader";

            // Demote old leader
            var oldLeaderMember = clan.members.FirstOrDefault(m => m.playerId == currentLeader);
            if (oldLeaderMember != null)
            {
                oldLeaderMember.rankId = "officer";
            }

            SaveClan(clanId);

            Debug.Log($"[ClanSystem] Leadership transferred from {currentLeader} to {newLeader}");

            return true;
        }

        #endregion

        #region Permissions

        private bool HasPermission(string clanId, ulong playerId, ClanPermission permission)
        {
            if (!clans.ContainsKey(clanId)) return false;

            var clan = clans[clanId];
            var member = clan.members.FirstOrDefault(m => m.playerId == playerId);

            if (member == null) return false;

            // Leader has all permissions
            if (clan.leaderId == playerId) return true;

            var rank = clan.ranks.FirstOrDefault(r => r.rankId == member.rankId);
            if (rank == null) return false;

            return rank.permissions.Contains(permission);
        }

        #endregion

        #region Clan Leveling

        public void AddClanXP(string clanId, int xp, ulong contributorId)
        {
            if (!clans.ContainsKey(clanId)) return;

            var clan = clans[clanId];

            clan.xp += xp;

            // Track contribution
            var member = clan.members.FirstOrDefault(m => m.playerId == contributorId);
            if (member != null)
            {
                member.contributedXP += xp;
            }

            // Check for level up
            int requiredXP = GetRequiredXP(clan.level);

            while (clan.xp >= requiredXP && clan.level < maxClanLevel)
            {
                clan.xp -= requiredXP;
                clan.level++;

                OnClanLevelUp?.Invoke(clanId, clan.level);

                Debug.Log($"[ClanSystem] Clan {clan.clanName} leveled up to {clan.level}");

                requiredXP = GetRequiredXP(clan.level);
            }

            SaveClan(clanId);
        }

        private int GetRequiredXP(int level)
        {
            return Mathf.RoundToInt(1000f * Mathf.Pow(1.15f, level - 1));
        }

        #endregion

        #region Clan Perks

        public bool UnlockPerk(string clanId, string perkId)
        {
            if (!clans.ContainsKey(clanId)) return false;

            var clan = clans[clanId];
            var perkData = clanPerks.FirstOrDefault(p => p.perkId == perkId);

            if (perkData == null) return false;

            // Check level requirement
            if (clan.level < perkData.requiredLevel) return false;

            // Check if already unlocked
            if (clan.perks.Contains(perkId)) return false;

            clan.perks.Add(perkId);

            SaveClan(clanId);

            Debug.Log($"[ClanSystem] Clan {clan.clanName} unlocked perk: {perkData.perkName}");

            return true;
        }

        public bool HasPerk(string clanId, string perkId)
        {
            if (!clans.ContainsKey(clanId)) return false;

            return clans[clanId].perks.Contains(perkId);
        }

        #endregion

        #region Clan Wars

        public bool DeclareClanWar(string attackerClanId, string defenderClanId, ulong declarerId)
        {
            if (!enableClanWars) return false;
            if (!clans.ContainsKey(attackerClanId) || !clans.ContainsKey(defenderClanId)) return false;

            var attackerClan = clans[attackerClanId];
            var defenderClan = clans[defenderClanId];

            // Only leader can declare war
            if (attackerClan.leaderId != declarerId) return false;

            // Check minimum members
            if (attackerClan.members.Count < minMembersForWar ||
                defenderClan.members.Count < minMembersForWar)
            {
                return false;
            }

            string warId = $"war_{DateTime.UtcNow.Ticks}";

            var war = new ClanWar
            {
                warId = warId,
                attackerClanId = attackerClanId,
                defenderClanId = defenderClanId,
                declarationTime = DateTime.UtcNow,
                startTime = DateTime.UtcNow.AddSeconds(warPreparationTime),
                endTime = DateTime.UtcNow.AddSeconds(warPreparationTime + warDuration),
                attackerKills = 0,
                defenderKills = 0,
                isActive = false
            };

            activeClanWars[warId] = war;

            Debug.Log($"[ClanSystem] Clan war declared: {attackerClan.clanName} vs {defenderClan.clanName}");

            return true;
        }

        private void UpdateClanWars()
        {
            var warsToEnd = new List<string>();

            foreach (var kvp in activeClanWars)
            {
                var warId = kvp.Key;
                var war = kvp.Value;

                // Start war if preparation time ended
                if (!war.isActive && DateTime.UtcNow >= war.startTime)
                {
                    war.isActive = true;
                    OnClanWarStarted?.Invoke(war);

                    Debug.Log($"[ClanSystem] Clan war started: {warId}");
                }

                // End war if duration expired
                if (war.isActive && DateTime.UtcNow >= war.endTime)
                {
                    warsToEnd.Add(warId);
                }
            }

            foreach (var warId in warsToEnd)
            {
                EndClanWar(warId);
            }
        }

        private void EndClanWar(string warId)
        {
            if (!activeClanWars.ContainsKey(warId)) return;

            var war = activeClanWars[warId];

            // Determine winner
            string winnerClanId = war.attackerKills > war.defenderKills
                ? war.attackerClanId
                : war.defenderClanId;

            OnClanWarEnded?.Invoke(war, winnerClanId);

            activeClanWars.Remove(warId);

            Debug.Log($"[ClanSystem] Clan war ended. Winner: {winnerClanId}");
        }

        public void RecordClanWarKill(ulong killerId, ulong victimId)
        {
            // Find active war involving these players
            if (!playerClanMembership.ContainsKey(killerId) ||
                !playerClanMembership.ContainsKey(victimId))
                return;

            string killerClanId = playerClanMembership[killerId];
            string victimClanId = playerClanMembership[victimId];

            var war = activeClanWars.Values.FirstOrDefault(w =>
                w.isActive &&
                ((w.attackerClanId == killerClanId && w.defenderClanId == victimClanId) ||
                 (w.defenderClanId == killerClanId && w.attackerClanId == victimClanId)));

            if (war == null) return;

            if (war.attackerClanId == killerClanId)
            {
                war.attackerKills++;
            }
            else
            {
                war.defenderKills++;
            }

            Debug.Log($"[ClanSystem] Clan war kill recorded. Attacker: {war.attackerKills}, Defender: {war.defenderKills}");
        }

        #endregion

        #region Persistence

        private void LoadAllClans()
        {
            // In production, load from database
            Debug.Log("[ClanSystem] Clan system initialized");
        }

        private void SaveClan(string clanId)
        {
            if (!clans.ContainsKey(clanId)) return;

            // In production, save to database
        }

        #endregion

        #region Public Getters

        public Clan GetClan(string clanId)
        {
            return clans.ContainsKey(clanId) ? clans[clanId] : null;
        }

        public Clan GetPlayerClan(ulong playerId)
        {
            if (!playerClanMembership.ContainsKey(playerId)) return null;

            string clanId = playerClanMembership[playerId];
            return GetClan(clanId);
        }

        public List<Clan> GetAllClans() => new List<Clan>(clans.Values);

        public List<ClanInvite> GetPlayerInvites(ulong playerId)
        {
            return pendingInvites.ContainsKey(playerId)
                ? new List<ClanInvite>(pendingInvites[playerId])
                : new List<ClanInvite>();
        }

        public bool IsInClan(ulong playerId) => playerClanMembership.ContainsKey(playerId);

        public List<ClanWar> GetActiveClanWars() => new List<ClanWar>(activeClanWars.Values);

        public ClanMember GetClanMember(string clanId, ulong playerId)
        {
            if (!clans.ContainsKey(clanId)) return null;

            return clans[clanId].members.FirstOrDefault(m => m.playerId == playerId);
        }

        #endregion
    }

    #region Data Classes

    public class Clan
    {
        public string clanId;
        public string clanName;
        public string clanTag;
        public string clanDescription;
        public ulong leaderId;
        public DateTime creationTime;
        public int level;
        public int xp;
        public List<ClanMember> members;
        public List<ClanRank> ranks;
        public int treasury;
        public List<string> perks;
    }

    public class ClanMember
    {
        public ulong playerId;
        public string rankId;
        public DateTime joinDate;
        public int contributedXP;
        public int contributedCurrency;
    }

    public class ClanRank
    {
        public string rankId;
        public string rankName;
        public List<ClanPermission> permissions;
    }

    [System.Serializable]
    public class ClanRankData
    {
        public string rankId;
        public string rankName;
        public List<ClanPermission> permissions;
    }

    public class ClanInvite
    {
        public string clanId;
        public ulong inviterId;
        public ulong inviteeId;
        public DateTime inviteTime;
        public DateTime expirationTime;
    }

    public class ClanWar
    {
        public string warId;
        public string attackerClanId;
        public string defenderClanId;
        public DateTime declarationTime;
        public DateTime startTime;
        public DateTime endTime;
        public int attackerKills;
        public int defenderKills;
        public bool isActive;
    }

    [System.Serializable]
    public class ClanPerkData
    {
        public string perkId;
        public string perkName;
        [TextArea(2, 3)]
        public string description;
        public int requiredLevel;
        public Sprite perkIcon;
    }

    public enum ClanPermission
    {
        InviteMembers,
        KickMembers,
        ManageRanks,
        EditClanInfo,
        ManageTreasury,
        DeclareWar,
        ManageAlliances
    }

    #endregion
}
