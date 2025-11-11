using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Social
{
    /// <summary>
    /// Comprehensive player profile system aggregating all player data including
    /// stats, titles, achievements, match history, and customization.
    /// </summary>
    public class PlayerProfileSystem : NetworkBehaviour
    {
        public static PlayerProfileSystem Instance { get; private set; }

        [Header("Profile Configuration")]
        [SerializeField] private bool enableProfilePrivacy = true;
        [SerializeField] private int profileCacheTime = 300; // 5 minutes

        private Dictionary<ulong, PlayerProfile> playerProfiles = new Dictionary<ulong, PlayerProfile>();
        private Dictionary<ulong, float> profileCacheTimestamps = new Dictionary<ulong, float>();

        public event Action<ulong> OnProfileCreated;
        public event Action<ulong> OnProfileUpdated;
        public event Action<ulong, string> OnProfilePictureChanged;
        public event Action<ulong, string> OnBioUpdated;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        /// <summary>
        /// Create or load player profile
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void InitializeProfileServerRpc(ulong playerId, string playerName, ServerRpcParams rpcParams = default)
        {
            if (playerProfiles.ContainsKey(playerId))
            {
                // Update timestamp
                playerProfiles[playerId].lastLoginTime = DateTime.UtcNow;
                return;
            }

            PlayerProfile profile = new PlayerProfile
            {
                playerId = playerId,
                playerName = playerName,
                createdAt = DateTime.UtcNow,
                lastLoginTime = DateTime.UtcNow,
                privacySettings = new ProfilePrivacySettings()
            };

            playerProfiles[playerId] = profile;
            OnProfileCreated?.Invoke(playerId);

            // Initialize profile data from other systems
            LoadProfileData(playerId);
        }

        private void LoadProfileData(ulong playerId)
        {
            var profile = playerProfiles[playerId];

            // Get progression data
            if (Progression.ProgressionSystem.Instance != null)
            {
                profile.level = GetPlayerLevel(playerId);
                profile.totalXP = GetPlayerXP(playerId);
                profile.prestigeRank = GetPrestigeRank(playerId);
            }

            // Get title
            if (Progression.TitleSystem.Instance != null)
            {
                profile.equippedTitle = Progression.TitleSystem.Instance.GetPlayerTitle(playerId);
                profile.totalTitlesUnlocked = Progression.TitleSystem.Instance.GetTitleCount(playerId);
            }

            // Get stats
            if (Statistics.PlayerStatisticsSystem.Instance != null)
            {
                profile.totalKills = GetTotalKills(playerId);
                profile.totalDeaths = GetTotalDeaths(playerId);
                profile.totalMatches = GetTotalMatches(playerId);
            }

            // Get match record
            if (Statistics.MatchHistorySystem.Instance != null)
            {
                var record = Statistics.MatchHistorySystem.Instance.GetPlayerRecord(playerId);
                profile.wins = record.wins;
                profile.losses = record.losses;
                profile.winRate = profile.totalMatches > 0 ?
                    (profile.wins / (float)profile.totalMatches) * 100f : 0f;
            }

            // Get achievements
            if (Achievements.AchievementSystem.Instance != null)
            {
                profile.totalAchievements = GetAchievementCount(playerId);
            }

            // Get clan info
            if (Clans.ClanSystem.Instance != null)
            {
                profile.clanId = GetPlayerClan(playerId);
                profile.clanRank = GetClanRank(playerId);
            }

            profile.lastUpdated = DateTime.UtcNow;
        }

        /// <summary>
        /// Get player profile (with privacy check)
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void RequestProfileServerRpc(ulong requesterId, ulong targetPlayerId, ServerRpcParams rpcParams = default)
        {
            if (!playerProfiles.TryGetValue(targetPlayerId, out var profile)) return;

            // Check privacy settings
            if (!CanViewProfile(requesterId, targetPlayerId))
            {
                SendProfilePrivateClientRpc(requesterId);
                return;
            }

            // Refresh profile data if cache expired
            if (!profileCacheTimestamps.ContainsKey(targetPlayerId) ||
                Time.time - profileCacheTimestamps[targetPlayerId] > profileCacheTime)
            {
                LoadProfileData(targetPlayerId);
                profileCacheTimestamps[targetPlayerId] = Time.time;
            }

            SendProfileDataClientRpc(requesterId, profile);
        }

        private bool CanViewProfile(ulong viewerId, ulong targetId)
        {
            if (!enableProfilePrivacy) return true;
            if (viewerId == targetId) return true; // Can always view own profile

            if (!playerProfiles.TryGetValue(targetId, out var profile)) return false;

            switch (profile.privacySettings.profileVisibility)
            {
                case ProfileVisibility.Public:
                    return true;

                case ProfileVisibility.FriendsOnly:
                    // Check if friends (would integrate with friend system)
                    return Social.FriendSystem.Instance?.AreFriends(viewerId, targetId) ?? false;

                case ProfileVisibility.Private:
                    return false;

                default:
                    return true;
            }
        }

        [ClientRpc]
        private void SendProfileDataClientRpc(ulong requesterId, PlayerProfile profile)
        {
            if (NetworkManager.Singleton.LocalClientId != requesterId) return;
            // Display profile UI
            Debug.Log($"Profile for {profile.playerName}: Level {profile.level}, {profile.totalKills} kills");
        }

        [ClientRpc]
        private void SendProfilePrivateClientRpc(ulong requesterId)
        {
            if (NetworkManager.Singleton.LocalClientId != requesterId) return;
            Debug.Log("This profile is private.");
        }

        /// <summary>
        /// Update player bio
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void UpdateBioServerRpc(ulong playerId, string bio, ServerRpcParams rpcParams = default)
        {
            if (!playerProfiles.TryGetValue(playerId, out var profile)) return;

            // Sanitize bio (max length, profanity filter, etc.)
            bio = SanitizeBio(bio);

            profile.bio = bio;
            profile.lastUpdated = DateTime.UtcNow;

            OnBioUpdated?.Invoke(playerId, bio);
        }

        /// <summary>
        /// Update privacy settings
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void UpdatePrivacySettingsServerRpc(ulong playerId, ProfileVisibility visibility,
            bool showStats, bool showMatchHistory, ServerRpcParams rpcParams = default)
        {
            if (!playerProfiles.TryGetValue(playerId, out var profile)) return;

            profile.privacySettings.profileVisibility = visibility;
            profile.privacySettings.showDetailedStats = showStats;
            profile.privacySettings.showMatchHistory = showMatchHistory;

            OnProfileUpdated?.Invoke(playerId);
        }

        /// <summary>
        /// Update profile picture
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void UpdateProfilePictureServerRpc(ulong playerId, string pictureId, ServerRpcParams rpcParams = default)
        {
            if (!playerProfiles.TryGetValue(playerId, out var profile)) return;

            profile.profilePictureId = pictureId;
            profile.lastUpdated = DateTime.UtcNow;

            OnProfilePictureChanged?.Invoke(playerId, pictureId);
        }

        private string SanitizeBio(string bio)
        {
            if (string.IsNullOrEmpty(bio)) return "";

            // Max length
            if (bio.Length > 500) bio = bio.Substring(0, 500);

            // Remove profanity (basic example)
            // In production, use proper profanity filter

            return bio.Trim();
        }

        // Helper methods to get data from other systems
        private int GetPlayerLevel(ulong playerId) => 1; // Would get from ProgressionSystem
        private int GetPlayerXP(ulong playerId) => 0;
        private int GetPrestigeRank(ulong playerId) => 0;
        private int GetTotalKills(ulong playerId) => 0;
        private int GetTotalDeaths(ulong playerId) => 0;
        private int GetTotalMatches(ulong playerId) => 0;
        private int GetAchievementCount(ulong playerId) => 0;
        private string GetPlayerClan(ulong playerId) => "";
        private string GetClanRank(ulong playerId) => "";

        public PlayerProfile GetProfile(ulong playerId)
        {
            return playerProfiles.TryGetValue(playerId, out var profile) ? profile : null;
        }

        [Serializable]
        public class PlayerProfile
        {
            public ulong playerId;
            public string playerName;
            public string bio = "";
            public string profilePictureId = "default";

            // Progression
            public int level;
            public int totalXP;
            public int prestigeRank;
            public string equippedTitle;
            public int totalTitlesUnlocked;

            // Combat Stats
            public int totalKills;
            public int totalDeaths;
            public float kdRatio => totalDeaths > 0 ? totalKills / (float)totalDeaths : totalKills;

            // Match Stats
            public int totalMatches;
            public int wins;
            public int losses;
            public float winRate;

            // Other
            public int totalAchievements;
            public string clanId;
            public string clanRank;

            // Timestamps
            public DateTime createdAt;
            public DateTime lastLoginTime;
            public DateTime lastUpdated;

            // Privacy
            public ProfilePrivacySettings privacySettings;
        }

        [Serializable]
        public class ProfilePrivacySettings
        {
            public ProfileVisibility profileVisibility = ProfileVisibility.Public;
            public bool showDetailedStats = true;
            public bool showMatchHistory = true;
            public bool allowFriendRequests = true;
            public bool showOnlineStatus = true;
        }

        public enum ProfileVisibility
        {
            Public,
            FriendsOnly,
            Private
        }
    }
}
