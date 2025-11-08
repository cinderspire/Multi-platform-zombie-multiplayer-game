using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace DeadFrontier.Core.Social
{
    /// <summary>
    /// Manages clan/guild system for deeper social engagement
    /// Enables persistent teams, clan wars, and group progression
    /// </summary>
    public class ClanSystem : Singleton<ClanSystem>
    {
        [Header("Clan Settings")]
        [SerializeField] private int maxClanMembers = 50;
        [SerializeField] private int minClanNameLength = 3;
        [SerializeField] private int maxClanNameLength = 20;
        [SerializeField] private int clanCreationCost = 10000; // Soft currency

        [Header("Ranks")]
        [SerializeField] private List<string> clanRanks = new List<string> { "Recruit", "Member", "Elite", "Officer", "Leader" };

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;
        [SerializeField] private bool useLocalStorage = true;

        // Player's clan
        private ClanData playerClan;
        private string playerClanId;
        private ClanRole playerClanRole;

        // Clan cache
        private Dictionary<string, ClanData> clanCache = new Dictionary<string, ClanData>();

        // Events
        public event System.Action<ClanData> OnClanJoined;
        public event System.Action OnClanLeft;
        public event System.Action<ClanMember> OnMemberJoined;
        public event System.Action<string> OnMemberLeft;
        public event System.Action<ClanData> OnClanUpdated;

        protected override void Awake()
        {
            base.Awake();
            LoadClanData();
        }

        #region Clan Creation

        /// <summary>
        /// Creates a new clan
        /// </summary>
        public bool CreateClan(string clanName, string clanTag, string description = "")
        {
            // Validate name
            if (!ValidateClanName(clanName))
            {
                if (showDebugLogs)
                    Debug.LogWarning("[ClanSystem] Invalid clan name");
                return false;
            }

            // Validate tag
            if (!ValidateClanTag(clanTag))
            {
                if (showDebugLogs)
                    Debug.LogWarning("[ClanSystem] Invalid clan tag");
                return false;
            }

            // Check if already in clan
            if (!string.IsNullOrEmpty(playerClanId))
            {
                if (showDebugLogs)
                    Debug.LogWarning("[ClanSystem] Already in a clan");
                return false;
            }

            // Check cost
            if (Economy.EconomyManager.Instance != null)
            {
                if (!Economy.EconomyManager.Instance.CanAfford(Economy.CurrencyType.Soft, clanCreationCost))
                {
                    if (showDebugLogs)
                        Debug.LogWarning($"[ClanSystem] Cannot afford clan creation cost: {clanCreationCost}");
                    return false;
                }

                Economy.EconomyManager.Instance.SpendSoftCurrency(clanCreationCost, "Clan creation");
            }

            // Create clan
            ClanData clan = new ClanData
            {
                clanId = System.Guid.NewGuid().ToString(),
                clanName = clanName,
                clanTag = clanTag,
                description = description,
                createdAt = System.DateTime.Now,
                level = 1,
                xp = 0,
                members = new List<ClanMember>()
            };

            // Add creator as leader
            string playerId = GetLocalPlayerId();
            string playerName = GetLocalPlayerName();

            clan.members.Add(new ClanMember
            {
                playerId = playerId,
                playerName = playerName,
                role = ClanRole.Leader,
                joinedAt = System.DateTime.Now,
                contributionPoints = 0
            });

            clan.leaderId = playerId;

            // Set as player's clan
            playerClan = clan;
            playerClanId = clan.clanId;
            playerClanRole = ClanRole.Leader;

            // Cache clan
            clanCache[clan.clanId] = clan;

            // Save
            SaveClanData();

            if (showDebugLogs)
                Debug.Log($"[ClanSystem] Created clan: {clanName} [{clanTag}]");

            OnClanJoined?.Invoke(clan);

            // Submit to backend
            if (!useLocalStorage)
            {
                CreateClanOnBackend(clan);
            }

            // Track analytics
            Core.Analytics.AnalyticsManager.Instance?.TrackEvent("clan_created", new Dictionary<string, object>
            {
                { "clan_id", clan.clanId },
                { "clan_name", clanName },
                { "clan_tag", clanTag }
            });

            return true;
        }

        private bool ValidateClanName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            if (name.Length < minClanNameLength || name.Length > maxClanNameLength)
                return false;

            // Check for profanity, special characters, etc.
            // TODO: Implement profanity filter

            return true;
        }

        private bool ValidateClanTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return false;

            if (tag.Length < 2 || tag.Length > 5)
                return false;

            // Only allow alphanumeric
            return tag.All(char.IsLetterOrDigit);
        }

        #endregion

        #region Clan Membership

        /// <summary>
        /// Sends a join request to a clan
        /// </summary>
        public bool RequestJoinClan(string clanId)
        {
            if (!string.IsNullOrEmpty(playerClanId))
            {
                if (showDebugLogs)
                    Debug.LogWarning("[ClanSystem] Already in a clan");
                return false;
            }

            // Submit request to backend
            if (!useLocalStorage)
            {
                SubmitJoinRequestToBackend(clanId);
            }

            if (showDebugLogs)
                Debug.Log($"[ClanSystem] Join request sent to clan: {clanId}");

            return true;
        }

        /// <summary>
        /// Invites a player to the clan
        /// </summary>
        public bool InvitePlayer(string playerId, string playerName)
        {
            if (playerClan == null)
            {
                if (showDebugLogs)
                    Debug.LogWarning("[ClanSystem] Not in a clan");
                return false;
            }

            // Check permissions
            if (!CanInviteMembers())
            {
                if (showDebugLogs)
                    Debug.LogWarning("[ClanSystem] Insufficient permissions to invite");
                return false;
            }

            // Check member limit
            if (playerClan.members.Count >= maxClanMembers)
            {
                if (showDebugLogs)
                    Debug.LogWarning($"[ClanSystem] Clan is full ({maxClanMembers} members)");
                return false;
            }

            // Submit invite to backend
            if (!useLocalStorage)
            {
                SubmitInviteToBackend(playerId, playerName);
            }

            if (showDebugLogs)
                Debug.Log($"[ClanSystem] Invited {playerName} to clan");

            return true;
        }

        /// <summary>
        /// Leaves the current clan
        /// </summary>
        public bool LeaveClan()
        {
            if (playerClan == null)
            {
                if (showDebugLogs)
                    Debug.LogWarning("[ClanSystem] Not in a clan");
                return false;
            }

            // Leaders cannot leave unless they transfer leadership
            if (playerClanRole == ClanRole.Leader)
            {
                if (showDebugLogs)
                    Debug.LogWarning("[ClanSystem] Leaders must transfer leadership before leaving");
                return false;
            }

            string clanId = playerClanId;
            string playerId = GetLocalPlayerId();

            // Remove from clan
            playerClan.members.RemoveAll(m => m.playerId == playerId);

            // Clear player's clan
            playerClan = null;
            playerClanId = "";
            playerClanRole = ClanRole.None;

            SaveClanData();

            if (showDebugLogs)
                Debug.Log("[ClanSystem] Left clan");

            OnClanLeft?.Invoke();

            // Submit to backend
            if (!useLocalStorage)
            {
                SubmitLeaveClanToBackend(clanId);
            }

            // Track analytics
            Core.Analytics.AnalyticsManager.Instance?.TrackEvent("clan_left", new Dictionary<string, object>
            {
                { "clan_id", clanId }
            });

            return true;
        }

        /// <summary>
        /// Kicks a member from the clan
        /// </summary>
        public bool KickMember(string memberId)
        {
            if (playerClan == null)
                return false;

            if (!CanKickMembers())
            {
                if (showDebugLogs)
                    Debug.LogWarning("[ClanSystem] Insufficient permissions to kick");
                return false;
            }

            var member = playerClan.members.Find(m => m.playerId == memberId);
            if (member == null)
                return false;

            // Cannot kick leader
            if (member.role == ClanRole.Leader)
                return false;

            // Officers can only kick members/recruits
            if (playerClanRole == ClanRole.Officer && (int)member.role >= (int)ClanRole.Officer)
                return false;

            playerClan.members.Remove(member);
            SaveClanData();

            if (showDebugLogs)
                Debug.Log($"[ClanSystem] Kicked member: {member.playerName}");

            OnMemberLeft?.Invoke(memberId);

            return true;
        }

        #endregion

        #region Clan Management

        /// <summary>
        /// Promotes a member to a higher rank
        /// </summary>
        public bool PromoteMember(string memberId)
        {
            if (playerClan == null || !CanPromoteMembers())
                return false;

            var member = playerClan.members.Find(m => m.playerId == memberId);
            if (member == null)
                return false;

            // Cannot promote beyond Officer (or Leader if you're leader)
            if ((int)member.role >= (int)ClanRole.Officer)
                return false;

            member.role = (ClanRole)((int)member.role + 1);
            SaveClanData();

            if (showDebugLogs)
                Debug.Log($"[ClanSystem] Promoted {member.playerName} to {member.role}");

            OnClanUpdated?.Invoke(playerClan);

            return true;
        }

        /// <summary>
        /// Demotes a member to a lower rank
        /// </summary>
        public bool DemoteMember(string memberId)
        {
            if (playerClan == null || !CanPromoteMembers())
                return false;

            var member = playerClan.members.Find(m => m.playerId == memberId);
            if (member == null || member.role == ClanRole.Recruit)
                return false;

            member.role = (ClanRole)((int)member.role - 1);
            SaveClanData();

            if (showDebugLogs)
                Debug.Log($"[ClanSystem] Demoted {member.playerName} to {member.role}");

            OnClanUpdated?.Invoke(playerClan);

            return true;
        }

        /// <summary>
        /// Updates clan information
        /// </summary>
        public bool UpdateClanInfo(string description)
        {
            if (playerClan == null || playerClanRole < ClanRole.Officer)
                return false;

            playerClan.description = description;
            SaveClanData();

            OnClanUpdated?.Invoke(playerClan);

            return true;
        }

        #endregion

        #region Clan Progression

        /// <summary>
        /// Awards XP to the clan
        /// </summary>
        public void AwardClanXP(int amount)
        {
            if (playerClan == null)
                return;

            playerClan.xp += amount;

            // Check for level up
            int requiredXP = GetRequiredXPForLevel(playerClan.level + 1);
            if (playerClan.xp >= requiredXP)
            {
                playerClan.level++;
                playerClan.xp -= requiredXP;

                if (showDebugLogs)
                    Debug.Log($"[ClanSystem] Clan leveled up to {playerClan.level}!");

                // Show notification
                if (UI.NotificationManager.Instance != null)
                {
                    UI.NotificationManager.Instance.ShowNotification(
                        "Clan Level Up!",
                        $"Your clan reached level {playerClan.level}",
                        new Color(1f, 0.84f, 0f),
                        UI.NotificationType.LevelUp
                    );
                }
            }

            SaveClanData();
        }

        private int GetRequiredXPForLevel(int level)
        {
            return level * 1000; // Simple formula
        }

        #endregion

        #region Queries

        public ClanData GetPlayerClan()
        {
            return playerClan;
        }

        public bool IsInClan()
        {
            return playerClan != null;
        }

        public ClanRole GetPlayerRole()
        {
            return playerClanRole;
        }

        public List<ClanMember> GetClanMembers()
        {
            return playerClan?.members ?? new List<ClanMember>();
        }

        public int GetOnlineMemberCount()
        {
            // TODO: Check online status
            return 0;
        }

        #endregion

        #region Permissions

        private bool CanInviteMembers()
        {
            return playerClanRole >= ClanRole.Officer;
        }

        private bool CanKickMembers()
        {
            return playerClanRole >= ClanRole.Officer;
        }

        private bool CanPromoteMembers()
        {
            return playerClanRole >= ClanRole.Officer;
        }

        #endregion

        #region Save/Load

        private void LoadClanData()
        {
            if (useLocalStorage)
            {
                string clanJson = PlayerPrefs.GetString("PlayerClan", "");
                // TODO: Deserialize

                if (showDebugLogs)
                    Debug.Log("[ClanSystem] Loaded clan data");
            }
            else
            {
                LoadClanFromBackend();
            }
        }

        private void SaveClanData()
        {
            if (useLocalStorage)
            {
                // TODO: Serialize
                PlayerPrefs.Save();
            }
            else
            {
                SaveClanToBackend();
            }
        }

        #endregion

        #region Backend Integration (Placeholder)

        private void CreateClanOnBackend(ClanData clan) { }
        private void SubmitJoinRequestToBackend(string clanId) { }
        private void SubmitInviteToBackend(string playerId, string playerName) { }
        private void SubmitLeaveClanToBackend(string clanId) { }
        private void LoadClanFromBackend() { }
        private void SaveClanToBackend() { }

        #endregion

        #region Helpers

        private string GetLocalPlayerId()
        {
            if (LeaderboardManager.Instance != null)
                return LeaderboardManager.Instance.LocalPlayerId;

            return PlayerPrefs.GetString("PlayerId", System.Guid.NewGuid().ToString());
        }

        private string GetLocalPlayerName()
        {
            if (LeaderboardManager.Instance != null)
                return LeaderboardManager.Instance.LocalPlayerName;

            return "Player";
        }

        #endregion
    }

    #region Data Structures

    public enum ClanRole
    {
        None = 0,
        Recruit = 1,
        Member = 2,
        Elite = 3,
        Officer = 4,
        Leader = 5
    }

    [System.Serializable]
    public class ClanData
    {
        public string clanId;
        public string clanName;
        public string clanTag;
        public string description;
        public string leaderId;
        public int level;
        public int xp;
        public System.DateTime createdAt;
        public List<ClanMember> members;
        public Sprite emblem;
    }

    [System.Serializable]
    public class ClanMember
    {
        public string playerId;
        public string playerName;
        public ClanRole role;
        public System.DateTime joinedAt;
        public int contributionPoints;
        public bool isOnline;
    }

    #endregion
}
