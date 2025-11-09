using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;
using System;

namespace DeadFrontier.Moderation
{
    /// <summary>
    /// Comprehensive player report and moderation system.
    /// Handles player reports, automated actions, chat filtering, and admin tools.
    /// Includes toxicity detection, automated bans, and appeal system.
    /// </summary>
    public class ReportSystem : NetworkBehaviour
    {
        public static ReportSystem Instance { get; private set; }

        [Header("Report Settings")]
        [SerializeField] private int maxReportsPerDay = 10;
        [SerializeField] private bool enableAutoBan = true;
        [SerializeField] private int autoBanThreshold = 5; // Reports needed for auto-ban

        [Header("Mute Settings")]
        [SerializeField] private float muteDurationMinutes = 60f;
        [SerializeField] private float escalatingMuteMultiplier = 2f;

        [Header("Ban Settings")]
        [SerializeField] private float tempBanDurationHours = 24f;
        [SerializeField] private float escalatingBanMultiplier = 2f;
        [SerializeField] private int maxWarningsBeforeBan = 3;

        // Reports
        private Dictionary<string, PlayerReport> activeReports = new Dictionary<string, PlayerReport>();
        private Dictionary<ulong, List<string>> playerReportHistory = new Dictionary<ulong, List<string>>();

        // Moderation actions
        private Dictionary<ulong, List<ModerationAction>> moderationHistory = new Dictionary<ulong, List<ModerationAction>>();

        // Mutes and bans
        private Dictionary<ulong, PlayerMute> activeMutes = new Dictionary<ulong, PlayerMute>();
        private Dictionary<ulong, PlayerBan> activeBans = new Dictionary<ulong, PlayerBan>();

        // Chat filtering
        private HashSet<string> bannedWords = new HashSet<string>();
        private List<string> suspiciousPatterns = new List<string>();

        // Player reputation
        private Dictionary<ulong, PlayerReputation> playerReputations = new Dictionary<ulong, PlayerReputation>();

        // Events
        public event Action<string, PlayerReport> OnReportSubmitted;
        public event Action<ulong, ModerationAction> OnModerationAction;
        public event Action<ulong> OnPlayerMuted;
        public event Action<ulong> OnPlayerBanned;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeChatFilters();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (!IsServer) return;

            CheckExpiredMutes();
            CheckExpiredBans();
        }

        #region Initialization

        private void InitializeChatFilters()
        {
            // Add banned words (censored examples)
            bannedWords.Add("****"); // Placeholder for actual profanity
            bannedWords.Add("cheat");
            bannedWords.Add("hack");

            // Suspicious patterns for cheating
            suspiciousPatterns.Add("aimbot");
            suspiciousPatterns.Add("wallhack");
            suspiciousPatterns.Add("speedhack");

            Debug.Log($"[ReportSystem] Initialized chat filters with {bannedWords.Count} banned words");
        }

        #endregion

        #region Reporting

        public bool SubmitReport(ulong reporterId, ulong reportedPlayerId, ReportReason reason, string description = "", string evidence = "")
        {
            // Validate
            if (reporterId == reportedPlayerId)
            {
                Debug.LogWarning("[ReportSystem] Cannot report yourself");
                return false;
            }

            // Check daily limit
            if (GetPlayerReportCount(reporterId, TimeSpan.FromDays(1)) >= maxReportsPerDay)
            {
                Debug.LogWarning($"[ReportSystem] Player {reporterId} exceeded daily report limit");
                return false;
            }

            // Check for duplicate reports
            if (HasReportedRecently(reporterId, reportedPlayerId, TimeSpan.FromHours(1)))
            {
                Debug.LogWarning($"[ReportSystem] Player {reporterId} recently reported {reportedPlayerId}");
                return false;
            }

            string reportId = $"report_{reporterId}_{reportedPlayerId}_{DateTime.UtcNow.Ticks}";

            var report = new PlayerReport
            {
                reportId = reportId,
                reporterId = reporterId,
                reportedPlayerId = reportedPlayerId,
                reason = reason,
                description = description,
                evidence = evidence,
                timestamp = DateTime.UtcNow,
                status = ReportStatus.Pending,
                priority = CalculateReportPriority(reportedPlayerId, reason)
            };

            activeReports[reportId] = report;

            // Track reporter history
            if (!playerReportHistory.ContainsKey(reporterId))
            {
                playerReportHistory[reporterId] = new List<string>();
            }

            playerReportHistory[reporterId].Add(reportId);

            OnReportSubmitted?.Invoke(reportId, report);

            Debug.Log($"[ReportSystem] Report submitted: {reporterId} reported {reportedPlayerId} for {reason}");

            // Check for auto-action
            ProcessAutoModeration(reportedPlayerId, reason);

            // Update reputation
            UpdatePlayerReputation(reportedPlayerId, -5); // Negative impact

            return true;
        }

        private int CalculateReportPriority(ulong playerId, ReportReason reason)
        {
            int priority = 1;

            // High priority reasons
            if (reason == ReportReason.Cheating || reason == ReportReason.Exploiting)
            {
                priority = 5;
            }
            else if (reason == ReportReason.Harassment || reason == ReportReason.HateSpeech)
            {
                priority = 4;
            }

            // Repeat offenders get higher priority
            int reportCount = GetReportsAgainstPlayer(playerId, TimeSpan.FromDays(7));

            if (reportCount >= 3)
            {
                priority += 2;
            }

            return Mathf.Clamp(priority, 1, 5);
        }

        private void ProcessAutoModeration(ulong playerId, ReportReason reason)
        {
            if (!enableAutoBan) return;

            int recentReports = GetReportsAgainstPlayer(playerId, TimeSpan.FromHours(24));

            if (recentReports >= autoBanThreshold)
            {
                // Auto-ban
                BanPlayer(playerId, 0, $"Auto-ban: {recentReports} reports in 24 hours", BanType.Temporary);

                Debug.Log($"[ReportSystem] Auto-banned player {playerId} ({recentReports} reports)");
            }
            else if (recentReports >= autoBanThreshold / 2)
            {
                // Auto-mute
                MutePlayer(playerId, 0, $"Auto-mute: {recentReports} reports");

                Debug.Log($"[ReportSystem] Auto-muted player {playerId} ({recentReports} reports)");
            }
        }

        #endregion

        #region Muting

        public bool MutePlayer(ulong playerId, ulong moderatorId, string reason)
        {
            // Check if already muted
            if (IsPlayerMuted(playerId))
            {
                Debug.LogWarning($"[ReportSystem] Player {playerId} is already muted");
                return false;
            }

            // Calculate duration (escalating)
            int muteCount = GetModerationActionCount(playerId, ModerationType.Mute);
            float duration = muteDurationMinutes * Mathf.Pow(escalatingMuteMultiplier, muteCount);

            var mute = new PlayerMute
            {
                playerId = playerId,
                moderatorId = moderatorId,
                reason = reason,
                startTime = DateTime.UtcNow,
                endTime = DateTime.UtcNow.AddMinutes(duration),
                isActive = true
            };

            activeMutes[playerId] = mute;

            // Record action
            RecordModerationAction(playerId, ModerationType.Mute, moderatorId, reason);

            OnPlayerMuted?.Invoke(playerId);

            Debug.Log($"[ReportSystem] Player {playerId} muted for {duration} minutes");

            // Notify client
            MutePlayerClientRpc(playerId, duration);

            return true;
        }

        [ClientRpc]
        private void MutePlayerClientRpc(ulong playerId, float durationMinutes)
        {
            Debug.Log($"[ReportSystem] You have been muted for {durationMinutes} minutes");
        }

        public bool UnmutePlayer(ulong playerId, ulong moderatorId)
        {
            if (!activeMutes.ContainsKey(playerId)) return false;

            activeMutes[playerId].isActive = false;
            activeMutes.Remove(playerId);

            Debug.Log($"[ReportSystem] Player {playerId} unmuted by {moderatorId}");

            return true;
        }

        public bool IsPlayerMuted(ulong playerId)
        {
            return activeMutes.ContainsKey(playerId) && activeMutes[playerId].isActive;
        }

        #endregion

        #region Banning

        public bool BanPlayer(ulong playerId, ulong moderatorId, string reason, BanType type)
        {
            // Check if already banned
            if (IsPlayerBanned(playerId))
            {
                Debug.LogWarning($"[ReportSystem] Player {playerId} is already banned");
                return false;
            }

            DateTime? endTime = null;

            if (type == BanType.Temporary)
            {
                int banCount = GetModerationActionCount(playerId, ModerationType.Ban);
                float duration = tempBanDurationHours * Mathf.Pow(escalatingBanMultiplier, banCount);

                endTime = DateTime.UtcNow.AddHours(duration);
            }

            var ban = new PlayerBan
            {
                playerId = playerId,
                moderatorId = moderatorId,
                reason = reason,
                banType = type,
                startTime = DateTime.UtcNow,
                endTime = endTime,
                isActive = true
            };

            activeBans[playerId] = ban;

            // Record action
            RecordModerationAction(playerId, ModerationType.Ban, moderatorId, reason);

            OnPlayerBanned?.Invoke(playerId);

            Debug.Log($"[ReportSystem] Player {playerId} banned ({type})");

            // Notify client and kick
            BanPlayerClientRpc(playerId, type, endTime?.ToString() ?? "Permanent");

            // Disconnect player
            DisconnectPlayer(playerId);

            return true;
        }

        [ClientRpc]
        private void BanPlayerClientRpc(ulong playerId, BanType type, string endTime)
        {
            Debug.Log($"[ReportSystem] BANNED: {type} until {endTime}");
        }

        public bool UnbanPlayer(ulong playerId, ulong moderatorId)
        {
            if (!activeBans.ContainsKey(playerId)) return false;

            activeBans[playerId].isActive = false;
            activeBans.Remove(playerId);

            Debug.Log($"[ReportSystem] Player {playerId} unbanned by {moderatorId}");

            return true;
        }

        public bool IsPlayerBanned(ulong playerId)
        {
            return activeBans.ContainsKey(playerId) && activeBans[playerId].isActive;
        }

        private void DisconnectPlayer(ulong playerId)
        {
            // Kick player from server
            // In real implementation, use NetworkManager
            Debug.Log($"[ReportSystem] Disconnecting player {playerId}");
        }

        #endregion

        #region Warnings

        public bool WarnPlayer(ulong playerId, ulong moderatorId, string reason)
        {
            // Record warning
            RecordModerationAction(playerId, ModerationType.Warning, moderatorId, reason);

            int warningCount = GetModerationActionCount(playerId, ModerationType.Warning);

            Debug.Log($"[ReportSystem] Player {playerId} warned ({warningCount} total warnings)");

            // Auto-ban after max warnings
            if (warningCount >= maxWarningsBeforeBan)
            {
                BanPlayer(playerId, moderatorId, $"Exceeded {maxWarningsBeforeBan} warnings", BanType.Temporary);
            }

            // Notify client
            WarnPlayerClientRpc(playerId, reason, warningCount);

            return true;
        }

        [ClientRpc]
        private void WarnPlayerClientRpc(ulong playerId, string reason, int warningCount)
        {
            Debug.Log($"[ReportSystem] WARNING ({warningCount}): {reason}");
        }

        #endregion

        #region Chat Filtering

        public string FilterMessage(string message, out bool containsProfanity)
        {
            containsProfanity = false;
            string filtered = message;

            foreach (var word in bannedWords)
            {
                if (filtered.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    containsProfanity = true;
                    filtered = filtered.Replace(word, new string('*', word.Length));
                }
            }

            return filtered;
        }

        public bool IsSuspiciousMessage(string message, out string matchedPattern)
        {
            matchedPattern = null;

            foreach (var pattern in suspiciousPatterns)
            {
                if (message.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    matchedPattern = pattern;
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Reputation System

        private void UpdatePlayerReputation(ulong playerId, int change)
        {
            if (!playerReputations.ContainsKey(playerId))
            {
                playerReputations[playerId] = new PlayerReputation
                {
                    playerId = playerId,
                    reputationScore = 100 // Start at 100
                };
            }

            var rep = playerReputations[playerId];
            rep.reputationScore = Mathf.Clamp(rep.reputationScore + change, 0, 200);

            // Take action based on reputation
            if (rep.reputationScore <= 20)
            {
                // Very low reputation - auto-ban
                BanPlayer(playerId, 0, "Reputation too low", BanType.Temporary);
            }
            else if (rep.reputationScore <= 50)
            {
                // Low reputation - restrict features
                rep.isRestricted = true;
            }
        }

        public int GetPlayerReputation(ulong playerId)
        {
            return playerReputations.ContainsKey(playerId) ? playerReputations[playerId].reputationScore : 100;
        }

        #endregion

        #region Moderation History

        private void RecordModerationAction(ulong playerId, ModerationType type, ulong moderatorId, string reason)
        {
            if (!moderationHistory.ContainsKey(playerId))
            {
                moderationHistory[playerId] = new List<ModerationAction>();
            }

            var action = new ModerationAction
            {
                type = type,
                moderatorId = moderatorId,
                reason = reason,
                timestamp = DateTime.UtcNow
            };

            moderationHistory[playerId].Add(action);

            OnModerationAction?.Invoke(playerId, action);
        }

        private int GetModerationActionCount(ulong playerId, ModerationType type)
        {
            if (!moderationHistory.ContainsKey(playerId)) return 0;

            return moderationHistory[playerId].Count(a => a.type == type);
        }

        public List<ModerationAction> GetModerationHistory(ulong playerId)
        {
            return moderationHistory.ContainsKey(playerId) ? moderationHistory[playerId] : new List<ModerationAction>();
        }

        #endregion

        #region Report Queries

        private int GetPlayerReportCount(ulong reporterId, TimeSpan timeframe)
        {
            if (!playerReportHistory.ContainsKey(reporterId)) return 0;

            DateTime cutoff = DateTime.UtcNow - timeframe;

            return playerReportHistory[reporterId]
                .Select(id => activeReports.ContainsKey(id) ? activeReports[id] : null)
                .Count(r => r != null && r.timestamp >= cutoff);
        }

        private int GetReportsAgainstPlayer(ulong playerId, TimeSpan timeframe)
        {
            DateTime cutoff = DateTime.UtcNow - timeframe;

            return activeReports.Values
                .Count(r => r.reportedPlayerId == playerId && r.timestamp >= cutoff);
        }

        private bool HasReportedRecently(ulong reporterId, ulong reportedPlayerId, TimeSpan timeframe)
        {
            DateTime cutoff = DateTime.UtcNow - timeframe;

            return activeReports.Values
                .Any(r => r.reporterId == reporterId &&
                         r.reportedPlayerId == reportedPlayerId &&
                         r.timestamp >= cutoff);
        }

        public List<PlayerReport> GetPendingReports()
        {
            return activeReports.Values
                .Where(r => r.status == ReportStatus.Pending)
                .OrderByDescending(r => r.priority)
                .ThenBy(r => r.timestamp)
                .ToList();
        }

        #endregion

        #region Expiration Checks

        private void CheckExpiredMutes()
        {
            var expired = activeMutes.Values
                .Where(m => m.isActive && DateTime.UtcNow >= m.endTime)
                .Select(m => m.playerId)
                .ToList();

            foreach (var playerId in expired)
            {
                UnmutePlayer(playerId, 0);
                Debug.Log($"[ReportSystem] Mute expired for player {playerId}");
            }
        }

        private void CheckExpiredBans()
        {
            var expired = activeBans.Values
                .Where(b => b.isActive &&
                           b.banType == BanType.Temporary &&
                           b.endTime.HasValue &&
                           DateTime.UtcNow >= b.endTime.Value)
                .Select(b => b.playerId)
                .ToList();

            foreach (var playerId in expired)
            {
                UnbanPlayer(playerId, 0);
                Debug.Log($"[ReportSystem] Ban expired for player {playerId}");
            }
        }

        #endregion
    }

    #region Data Classes

    [Serializable]
    public class PlayerReport
    {
        public string reportId;
        public ulong reporterId;
        public ulong reportedPlayerId;
        public ReportReason reason;
        public string description;
        public string evidence; // Screenshot URL, video, etc.
        public DateTime timestamp;
        public ReportStatus status;
        public int priority; // 1-5, 5 = highest
    }

    [Serializable]
    public class PlayerMute
    {
        public ulong playerId;
        public ulong moderatorId;
        public string reason;
        public DateTime startTime;
        public DateTime endTime;
        public bool isActive;
    }

    [Serializable]
    public class PlayerBan
    {
        public ulong playerId;
        public ulong moderatorId;
        public string reason;
        public BanType banType;
        public DateTime startTime;
        public DateTime? endTime; // Null for permanent
        public bool isActive;
    }

    [Serializable]
    public class ModerationAction
    {
        public ModerationType type;
        public ulong moderatorId;
        public string reason;
        public DateTime timestamp;
    }

    [Serializable]
    public class PlayerReputation
    {
        public ulong playerId;
        public int reputationScore; // 0-200, 100 = neutral
        public bool isRestricted;
    }

    public enum ReportReason
    {
        Cheating,
        Exploiting,
        Harassment,
        HateSpeech,
        Griefing,
        Spamming,
        InappropriateName,
        TeamKilling,
        Other
    }

    public enum ReportStatus
    {
        Pending,
        UnderReview,
        Resolved,
        Dismissed
    }

    public enum ModerationType
    {
        Warning,
        Mute,
        Kick,
        Ban
    }

    public enum BanType
    {
        Temporary,
        Permanent
    }

    #endregion
}
