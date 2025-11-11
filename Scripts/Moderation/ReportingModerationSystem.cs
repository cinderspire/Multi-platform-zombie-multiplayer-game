using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Moderation
{
    /// <summary>
    /// Comprehensive player reporting and moderation system with automated
    /// chat filtering, player reports, and admin review queue.
    /// </summary>
    public class ReportingModerationSystem : NetworkBehaviour
    {
        public static ReportingModerationSystem Instance { get; private set; }

        [Header("Moderation Configuration")]
        [SerializeField] private bool enableAutoChatFilter = true;
        [SerializeField] private int reportsForAutoAction = 5;
        [SerializeField] private float reportCooldown = 300f; // 5 minutes

        private Dictionary<ulong, List<PlayerReport>> playerReports = new Dictionary<ulong, List<PlayerReport>>();
        private Dictionary<ulong, PlayerModerationStatus> playerStatus = new Dictionary<ulong, PlayerModerationStatus>();
        private Dictionary<ulong, float> lastReportTime = new Dictionary<ulong, float>();
        private List<string> bannedWords = new List<string>();
        private Queue<PlayerReport> moderationQueue = new Queue<PlayerReport>();

        public event Action<ulong, ulong, ReportReason> OnPlayerReported;
        public event Action<ulong, ModerationAction> OnModerationActionTaken;
        public event Action<string> OnChatMessageFiltered;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            InitializeChatFilter();
        }

        private void InitializeChatFilter()
        {
            // Basic profanity filter (would be expanded in production)
            bannedWords = new List<string>
            {
                // Common profanity placeholders
                "badword1", "badword2", "badword3"
            };
        }

        /// <summary>
        /// Submit player report
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void ReportPlayerServerRpc(ulong reporterId, ulong reportedId, ReportReason reason,
            string description, ServerRpcParams rpcParams = default)
        {
            // Check cooldown
            if (lastReportTime.ContainsKey(reporterId))
            {
                if (Time.time - lastReportTime[reporterId] < reportCooldown)
                {
                    NotifyReportCooldownClientRpc(reporterId);
                    return;
                }
            }

            lastReportTime[reporterId] = Time.time;

            // Create report
            PlayerReport report = new PlayerReport
            {
                reportId = Guid.NewGuid().ToString(),
                reporterId = reporterId,
                reportedPlayerId = reportedId,
                reason = reason,
                description = description,
                timestamp = DateTime.UtcNow,
                status = ReportStatus.Pending
            };

            // Store report
            if (!playerReports.ContainsKey(reportedId))
            {
                playerReports[reportedId] = new List<PlayerReport>();
            }
            playerReports[reportedId].Add(report);

            // Add to moderation queue
            moderationQueue.Enqueue(report);

            OnPlayerReported?.Invoke(reporterId, reportedId, reason);

            // Check if auto-action needed
            int recentReports = GetRecentReportCount(reportedId);
            if (recentReports >= reportsForAutoAction)
            {
                ApplyAutoModeration(reportedId, reason);
            }

            NotifyReportSubmittedClientRpc(reporterId);
        }

        /// <summary>
        /// Filter chat message for profanity
        /// </summary>
        public string FilterChatMessage(string message)
        {
            if (!enableAutoChatFilter) return message;

            string filteredMessage = message;

            foreach (string bannedWord in bannedWords)
            {
                if (filteredMessage.ToLower().Contains(bannedWord.ToLower()))
                {
                    filteredMessage = filteredMessage.Replace(bannedWord, new string('*', bannedWord.Length));
                    OnChatMessageFiltered?.Invoke(message);
                }
            }

            return filteredMessage;
        }

        /// <summary>
        /// Apply moderation action
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void ApplyModerationActionServerRpc(ulong adminId, ulong targetId, ModerationAction action,
            int durationMinutes, string reason, ServerRpcParams rpcParams = default)
        {
            // Verify admin permissions (would check actual admin status)
            if (!IsAdmin(adminId)) return;

            if (!playerStatus.ContainsKey(targetId))
            {
                playerStatus[targetId] = new PlayerModerationStatus { playerId = targetId };
            }

            var status = playerStatus[targetId];

            switch (action)
            {
                case ModerationAction.Warning:
                    status.warningCount++;
                    break;

                case ModerationAction.Mute:
                    status.isMuted = true;
                    status.muteExpiration = DateTime.UtcNow.AddMinutes(durationMinutes);
                    break;

                case ModerationAction.Kick:
                    KickPlayer(targetId, reason);
                    break;

                case ModerationAction.TempBan:
                    status.isBanned = true;
                    status.banExpiration = DateTime.UtcNow.AddMinutes(durationMinutes);
                    status.banReason = reason;
                    KickPlayer(targetId, reason);
                    break;

                case ModerationAction.PermaBan:
                    status.isBanned = true;
                    status.banExpiration = DateTime.MaxValue;
                    status.banReason = reason;
                    KickPlayer(targetId, reason);
                    break;
            }

            status.moderationHistory.Add(new ModerationRecord
            {
                action = action,
                adminId = adminId,
                reason = reason,
                timestamp = DateTime.UtcNow,
                durationMinutes = durationMinutes
            });

            OnModerationActionTaken?.Invoke(targetId, action);
            NotifyModerationActionClientRpc(targetId, action, durationMinutes, reason);
        }

        private void ApplyAutoModeration(ulong playerId, ReportReason reason)
        {
            // Auto-mute for chat abuse
            if (reason == ReportReason.ChatAbuse || reason == ReportReason.Harassment)
            {
                ApplyModerationActionServerRpc(0, playerId, ModerationAction.Mute, 60, "Auto-muted: Multiple reports");
            }

            // Auto-kick for cheating
            if (reason == ReportReason.Cheating)
            {
                ApplyModerationActionServerRpc(0, playerId, ModerationAction.Kick, 0, "Auto-kicked: Multiple cheating reports");
            }
        }

        private int GetRecentReportCount(ulong playerId)
        {
            if (!playerReports.ContainsKey(playerId)) return 0;

            DateTime threshold = DateTime.UtcNow.AddHours(-1);
            return playerReports[playerId].Count(r => r.timestamp > threshold);
        }

        private void KickPlayer(ulong playerId, string reason)
        {
            // Would integrate with network manager to disconnect player
            Debug.Log($"Player {playerId} kicked: {reason}");
        }

        private bool IsAdmin(ulong playerId)
        {
            // Would check actual admin list
            return false;
        }

        /// <summary>
        /// Check if player can chat
        /// </summary>
        public bool CanPlayerChat(ulong playerId)
        {
            if (!playerStatus.ContainsKey(playerId)) return true;

            var status = playerStatus[playerId];
            if (status.isMuted)
            {
                if (DateTime.UtcNow < status.muteExpiration)
                {
                    return false;
                }
                else
                {
                    status.isMuted = false;
                }
            }

            return true;
        }

        /// <summary>
        /// Check if player is banned
        /// </summary>
        public bool IsPlayerBanned(ulong playerId)
        {
            if (!playerStatus.ContainsKey(playerId)) return false;

            var status = playerStatus[playerId];
            if (status.isBanned)
            {
                if (DateTime.UtcNow < status.banExpiration)
                {
                    return true;
                }
                else if (status.banExpiration != DateTime.MaxValue)
                {
                    status.isBanned = false;
                }
            }

            return status.isBanned;
        }

        [ClientRpc]
        private void NotifyReportSubmittedClientRpc(ulong reporterId)
        {
            if (NetworkManager.Singleton.LocalClientId != reporterId) return;
            Debug.Log("<color=green>Report submitted successfully. Thank you for helping keep the community safe.</color>");
        }

        [ClientRpc]
        private void NotifyReportCooldownClientRpc(ulong reporterId)
        {
            if (NetworkManager.Singleton.LocalClientId != reporterId) return;
            Debug.Log("<color=yellow>Please wait before submitting another report.</color>");
        }

        [ClientRpc]
        private void NotifyModerationActionClientRpc(ulong targetId, ModerationAction action, int duration, string reason)
        {
            if (NetworkManager.Singleton.LocalClientId != targetId) return;

            string message = action switch
            {
                ModerationAction.Warning => $"<color=yellow>WARNING: {reason}</color>",
                ModerationAction.Mute => $"<color=orange>You have been muted for {duration} minutes. Reason: {reason}</color>",
                ModerationAction.Kick => $"<color=red>You have been kicked. Reason: {reason}</color>",
                ModerationAction.TempBan => $"<color=red>You have been banned for {duration} minutes. Reason: {reason}</color>",
                ModerationAction.PermaBan => $"<color=red>You have been permanently banned. Reason: {reason}</color>",
                _ => ""
            };

            Debug.Log(message);
        }

        public PlayerReport GetNextReportForReview()
        {
            return moderationQueue.Count > 0 ? moderationQueue.Dequeue() : null;
        }

        public List<PlayerReport> GetPlayerReports(ulong playerId)
        {
            return playerReports.TryGetValue(playerId, out var reports) ?
                new List<PlayerReport>(reports) : new List<PlayerReport>();
        }

        [Serializable]
        public class PlayerReport
        {
            public string reportId;
            public ulong reporterId;
            public ulong reportedPlayerId;
            public ReportReason reason;
            public string description;
            public DateTime timestamp;
            public ReportStatus status;
        }

        [Serializable]
        private class PlayerModerationStatus
        {
            public ulong playerId;
            public bool isMuted;
            public DateTime muteExpiration;
            public bool isBanned;
            public DateTime banExpiration;
            public string banReason;
            public int warningCount;
            public List<ModerationRecord> moderationHistory = new List<ModerationRecord>();
        }

        [Serializable]
        private class ModerationRecord
        {
            public ModerationAction action;
            public ulong adminId;
            public string reason;
            public DateTime timestamp;
            public int durationMinutes;
        }

        public enum ReportReason
        {
            Cheating,
            ChatAbuse,
            Harassment,
            InappropriateName,
            Griefing,
            AFK,
            Other
        }

        public enum ModerationAction
        {
            Warning,
            Mute,
            Kick,
            TempBan,
            PermaBan
        }

        public enum ReportStatus
        {
            Pending,
            UnderReview,
            Resolved,
            Dismissed
        }
    }
}
