using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace DeadFrontier.Core.Moderation
{
    /// <summary>
    /// Manages player reporting, moderation, and ban system
    /// Essential for maintaining a healthy community
    /// </summary>
    public class ModerationSystem : Singleton<ModerationSystem>
    {
        [Header("Report Settings")]
        [SerializeField] private int maxReportsPerDay = 10;
        [SerializeField] private float reportCooldown = 60f; // 1 minute

        [Header("Ban Settings")]
        [SerializeField] private int reportsForAutoReview = 5;
        [SerializeField] private int reportsForTempBan = 10;
        [SerializeField] private int reportsForPermBan = 20;

        [Header("Ban Durations")]
        [SerializeField] private int tempBan1Hours = 24;
        [SerializeField] private int tempBan2Hours = 72;
        [SerializeField] private int tempBan3Hours = 168; // 1 week

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;
        [SerializeField] private bool useLocalStorage = true; // For testing without backend

        // Report tracking
        private Dictionary<string, int> dailyReportCount = new Dictionary<string, int>();
        private Dictionary<string, float> lastReportTime = new Dictionary<string, float>();
        private List<PlayerReport> pendingReports = new List<PlayerReport>();

        // Ban tracking
        private Dictionary<string, BanData> activeBans = new Dictionary<string, BanData>();
        private List<string> permaBannedPlayers = new List<string>();

        // Events
        public event System.Action<PlayerReport> OnReportSubmitted;
        public event System.Action<string, BanData> OnPlayerBanned;
        public event System.Action<string> OnBanExpired;

        protected override void Awake()
        {
            base.Awake();
            LoadModerationData();
        }

        private void Update()
        {
            CheckExpiredBans();
        }

        #region Reporting

        /// <summary>
        /// Reports a player for misconduct
        /// </summary>
        public bool ReportPlayer(string targetPlayerId, string targetPlayerName, ReportReason reason, string description = "")
        {
            string reporterId = GetLocalPlayerId();

            // Check if can report
            if (!CanReport(reporterId))
            {
                if (showDebugLogs)
                    Debug.LogWarning("[ModerationSystem] Report limit reached or on cooldown");
                return false;
            }

            // Don't allow self-reports
            if (reporterId == targetPlayerId)
            {
                if (showDebugLogs)
                    Debug.LogWarning("[ModerationSystem] Cannot report yourself");
                return false;
            }

            // Create report
            PlayerReport report = new PlayerReport
            {
                reportId = System.Guid.NewGuid().ToString(),
                reporterId = reporterId,
                targetPlayerId = targetPlayerId,
                targetPlayerName = targetPlayerName,
                reason = reason,
                description = description,
                timestamp = System.DateTime.Now,
                status = ReportStatus.Pending
            };

            pendingReports.Add(report);

            // Update tracking
            if (!dailyReportCount.ContainsKey(reporterId))
            {
                dailyReportCount[reporterId] = 0;
            }
            dailyReportCount[reporterId]++;
            lastReportTime[reporterId] = Time.time;

            if (showDebugLogs)
                Debug.Log($"[ModerationSystem] Report submitted: {targetPlayerName} for {reason}");

            OnReportSubmitted?.Invoke(report);

            // Check if auto-action needed
            CheckAutoModeration(targetPlayerId);

            // Save
            SaveModerationData();

            // Submit to backend
            if (!useLocalStorage)
            {
                SubmitReportToBackend(report);
            }

            // Track analytics
            Core.Analytics.AnalyticsManager.Instance?.TrackEvent("player_reported", new Dictionary<string, object>
            {
                { "reason", reason.ToString() },
                { "target_player_id", targetPlayerId }
            });

            return true;
        }

        private bool CanReport(string playerId)
        {
            // Check daily limit
            if (dailyReportCount.ContainsKey(playerId))
            {
                if (dailyReportCount[playerId] >= maxReportsPerDay)
                {
                    return false;
                }
            }

            // Check cooldown
            if (lastReportTime.ContainsKey(playerId))
            {
                if (Time.time - lastReportTime[playerId] < reportCooldown)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Gets reports for a specific player
        /// </summary>
        public List<PlayerReport> GetReportsForPlayer(string playerId)
        {
            return pendingReports.Where(r => r.targetPlayerId == playerId).ToList();
        }

        /// <summary>
        /// Gets total report count for a player
        /// </summary>
        public int GetReportCount(string playerId)
        {
            return pendingReports.Count(r => r.targetPlayerId == playerId && r.status == ReportStatus.Pending);
        }

        #endregion

        #region Auto-Moderation

        private void CheckAutoModeration(string playerId)
        {
            int reportCount = GetReportCount(playerId);

            if (reportCount >= reportsForPermBan)
            {
                // Permanent ban
                BanPlayer(playerId, BanType.Permanent, "Excessive reports", 0);
            }
            else if (reportCount >= reportsForTempBan)
            {
                // Escalating temporary bans
                int tempBanCount = GetTempBanCount(playerId);

                int banDurationHours = tempBan1Hours;
                if (tempBanCount >= 2)
                    banDurationHours = tempBan3Hours;
                else if (tempBanCount >= 1)
                    banDurationHours = tempBan2Hours;

                BanPlayer(playerId, BanType.Temporary, "Multiple reports", banDurationHours);
            }
            else if (reportCount >= reportsForAutoReview)
            {
                // Flag for manual review
                if (showDebugLogs)
                    Debug.Log($"[ModerationSystem] Player {playerId} flagged for review ({reportCount} reports)");

                FlagForReview(playerId);
            }
        }

        private void FlagForReview(string playerId)
        {
            // Mark reports as under review
            var reports = GetReportsForPlayer(playerId);
            foreach (var report in reports)
            {
                report.status = ReportStatus.UnderReview;
            }

            // Submit to backend for moderator review
            if (!useLocalStorage)
            {
                SubmitForReviewToBackend(playerId, reports);
            }
        }

        private int GetTempBanCount(string playerId)
        {
            // TODO: Track ban history
            return 0;
        }

        #endregion

        #region Ban System

        /// <summary>
        /// Bans a player
        /// </summary>
        public void BanPlayer(string playerId, BanType banType, string reason, int durationHours = 0)
        {
            BanData banData = new BanData
            {
                playerId = playerId,
                banType = banType,
                reason = reason,
                bannedAt = System.DateTime.Now,
                expiresAt = banType == BanType.Permanent ? System.DateTime.MaxValue : System.DateTime.Now.AddHours(durationHours),
                durationHours = durationHours
            };

            if (banType == BanType.Permanent)
            {
                if (!permaBannedPlayers.Contains(playerId))
                {
                    permaBannedPlayers.Add(playerId);
                }
            }

            activeBans[playerId] = banData;
            SaveModerationData();

            if (showDebugLogs)
            {
                string duration = banType == BanType.Permanent ? "PERMANENT" : $"{durationHours} hours";
                Debug.Log($"[ModerationSystem] Banned player {playerId} for {duration}. Reason: {reason}");
            }

            OnPlayerBanned?.Invoke(playerId, banData);

            // Submit to backend
            if (!useLocalStorage)
            {
                SubmitBanToBackend(banData);
            }

            // Track analytics
            Core.Analytics.AnalyticsManager.Instance?.TrackEvent("player_banned", new Dictionary<string, object>
            {
                { "player_id", playerId },
                { "ban_type", banType.ToString() },
                { "duration_hours", durationHours },
                { "reason", reason }
            });

            // Kick player if currently connected
            KickBannedPlayer(playerId);
        }

        /// <summary>
        /// Checks if a player is banned
        /// </summary>
        public bool IsPlayerBanned(string playerId)
        {
            // Check permanent bans
            if (permaBannedPlayers.Contains(playerId))
                return true;

            // Check active temporary bans
            if (activeBans.ContainsKey(playerId))
            {
                var ban = activeBans[playerId];
                if (System.DateTime.Now < ban.expiresAt)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets ban data for a player
        /// </summary>
        public BanData GetBanData(string playerId)
        {
            if (activeBans.ContainsKey(playerId))
                return activeBans[playerId];

            if (permaBannedPlayers.Contains(playerId))
            {
                return new BanData
                {
                    playerId = playerId,
                    banType = BanType.Permanent,
                    reason = "Permanent ban",
                    bannedAt = System.DateTime.MinValue,
                    expiresAt = System.DateTime.MaxValue
                };
            }

            return null;
        }

        /// <summary>
        /// Unbans a player
        /// </summary>
        public void UnbanPlayer(string playerId)
        {
            if (activeBans.Remove(playerId))
            {
                SaveModerationData();

                if (showDebugLogs)
                    Debug.Log($"[ModerationSystem] Unbanned player {playerId}");

                // Submit to backend
                if (!useLocalStorage)
                {
                    SubmitUnbanToBackend(playerId);
                }
            }

            permaBannedPlayers.Remove(playerId);
        }

        private void CheckExpiredBans()
        {
            var expiredBans = activeBans.Where(kvp => System.DateTime.Now >= kvp.Value.expiresAt).ToList();

            foreach (var kvp in expiredBans)
            {
                activeBans.Remove(kvp.Key);

                if (showDebugLogs)
                    Debug.Log($"[ModerationSystem] Ban expired for player {kvp.Key}");

                OnBanExpired?.Invoke(kvp.Key);
            }

            if (expiredBans.Count > 0)
            {
                SaveModerationData();
            }
        }

        private void KickBannedPlayer(string playerId)
        {
            // If player is currently connected, disconnect them
            if (Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsServer)
            {
                if (ulong.TryParse(playerId, out ulong clientId))
                {
                    if (Unity.Netcode.NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId))
                    {
                        Unity.Netcode.NetworkManager.Singleton.DisconnectClient(clientId);

                        if (showDebugLogs)
                            Debug.Log($"[ModerationSystem] Kicked banned player {clientId}");
                    }
                }
            }
        }

        #endregion

        #region Save/Load

        private void LoadModerationData()
        {
            if (useLocalStorage)
            {
                string reportsJson = PlayerPrefs.GetString("ModerationReports", "");
                string bansJson = PlayerPrefs.GetString("ModerationBans", "");

                // TODO: Deserialize data
                // In production, this would load from backend

                if (showDebugLogs)
                    Debug.Log("[ModerationSystem] Loaded moderation data");
            }
            else
            {
                LoadModerationFromBackend();
            }
        }

        private void SaveModerationData()
        {
            if (useLocalStorage)
            {
                // TODO: Serialize data
                PlayerPrefs.Save();
            }
            else
            {
                SaveModerationToBackend();
            }
        }

        #endregion

        #region Backend Integration (Placeholder)

        private void SubmitReportToBackend(PlayerReport report)
        {
            // TODO: Submit to backend moderation system
            if (showDebugLogs)
                Debug.Log($"[ModerationSystem] Submitting report to backend: {report.reportId}");
        }

        private void SubmitForReviewToBackend(string playerId, List<PlayerReport> reports)
        {
            // TODO: Submit to backend for moderator review
            if (showDebugLogs)
                Debug.Log($"[ModerationSystem] Submitting player for review: {playerId}");
        }

        private void SubmitBanToBackend(BanData ban)
        {
            // TODO: Submit ban to backend
            if (showDebugLogs)
                Debug.Log($"[ModerationSystem] Submitting ban to backend: {ban.playerId}");
        }

        private void SubmitUnbanToBackend(string playerId)
        {
            // TODO: Submit unban to backend
            if (showDebugLogs)
                Debug.Log($"[ModerationSystem] Submitting unban to backend: {playerId}");
        }

        private void LoadModerationFromBackend()
        {
            // TODO: Load from backend
            if (showDebugLogs)
                Debug.Log("[ModerationSystem] Loading moderation data from backend...");
        }

        private void SaveModerationToBackend()
        {
            // TODO: Save to backend
        }

        #endregion

        #region Helpers

        private string GetLocalPlayerId()
        {
            if (Social.LeaderboardManager.Instance != null)
            {
                return Social.LeaderboardManager.Instance.LocalPlayerId;
            }

            return PlayerPrefs.GetString("PlayerId", System.Guid.NewGuid().ToString());
        }

        #endregion

        #region Properties

        public int PendingReportsCount => pendingReports.Count(r => r.status == ReportStatus.Pending);
        public int ActiveBansCount => activeBans.Count + permaBannedPlayers.Count;

        #endregion
    }

    #region Data Structures

    public enum ReportReason
    {
        Cheating,
        Harassment,
        Hate Speech,
        Griefing,
        Inappropriate_Name,
        AFK,
        Teaming,
        Other
    }

    public enum ReportStatus
    {
        Pending,
        UnderReview,
        Resolved,
        Dismissed
    }

    public enum BanType
    {
        Temporary,
        Permanent
    }

    [System.Serializable]
    public class PlayerReport
    {
        public string reportId;
        public string reporterId;
        public string targetPlayerId;
        public string targetPlayerName;
        public ReportReason reason;
        public string description;
        public System.DateTime timestamp;
        public ReportStatus status;
    }

    [System.Serializable]
    public class BanData
    {
        public string playerId;
        public BanType banType;
        public string reason;
        public System.DateTime bannedAt;
        public System.DateTime expiresAt;
        public int durationHours;
    }

    #endregion
}
