using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Admin
{
    /// <summary>
    /// Comprehensive admin and moderation system with player management, chat moderation,
    /// reporting, banning, server commands, and audit logging.
    /// </summary>
    public class AdminSystem : NetworkBehaviour
    {
        public static AdminSystem Instance { get; private set; }

        [Header("Admin Configuration")]
        [SerializeField] private int maxReportsPerPlayer = 10;
        [SerializeField] private float reportCooldown = 300f; // 5 minutes
        [SerializeField] private bool enableAuditLog = true;

        private Dictionary<ulong, AdminPermissions> adminPermissions = new Dictionary<ulong, AdminPermissions>();
        private Dictionary<ulong, PlayerModeration> playerModerations = new Dictionary<ulong, PlayerModeration>();
        private Dictionary<ulong, List<Report>> playerReports = new Dictionary<ulong, List<Report>>();
        private List<AuditLogEntry> auditLog = new List<AuditLogEntry>();
        private Dictionary<ulong, float> lastReportTime = new Dictionary<ulong, float>();

        public event Action<ulong, ModerationAction, string> OnModerationActionTaken;
        public event Action<Report> OnReportSubmitted;
        public event Action<ulong, AdminCommand> OnAdminCommandExecuted;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // Admin Permission Management
        [ServerRpc(RequireOwnership = false)]
        public void GrantAdminPermissionsServerRpc(ulong adminId, ulong targetPlayerId, AdminRole role, ServerRpcParams rpcParams = default)
        {
            if (!HasPermission(adminId, AdminPermission.ManageAdmins))
            {
                Debug.LogWarning($"Player {adminId} lacks permission to grant admin rights");
                return;
            }

            adminPermissions[targetPlayerId] = new AdminPermissions
            {
                playerId = targetPlayerId,
                role = role,
                permissions = GetPermissionsForRole(role),
                grantedBy = adminId,
                grantedAt = DateTime.UtcNow
            };

            LogAdminAction(adminId, AdminCommand.GrantPermissions, targetPlayerId, $"Granted {role} role");
            Debug.Log($"Player {targetPlayerId} granted {role} permissions by admin {adminId}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void RevokeAdminPermissionsServerRpc(ulong adminId, ulong targetPlayerId, ServerRpcParams rpcParams = default)
        {
            if (!HasPermission(adminId, AdminPermission.ManageAdmins))
            {
                Debug.LogWarning($"Player {adminId} lacks permission to revoke admin rights");
                return;
            }

            if (adminPermissions.ContainsKey(targetPlayerId))
            {
                adminPermissions.Remove(targetPlayerId);
                LogAdminAction(adminId, AdminCommand.RevokePermissions, targetPlayerId, "Revoked admin permissions");
                Debug.Log($"Player {targetPlayerId} admin permissions revoked by {adminId}");
            }
        }

        // Player Moderation
        [ServerRpc(RequireOwnership = false)]
        public void KickPlayerServerRpc(ulong adminId, ulong targetPlayerId, string reason, ServerRpcParams rpcParams = default)
        {
            if (!HasPermission(adminId, AdminPermission.KickPlayers))
            {
                Debug.LogWarning($"Player {adminId} lacks permission to kick players");
                return;
            }

            RecordModeration(targetPlayerId, ModerationAction.Kick, reason, adminId);
            LogAdminAction(adminId, AdminCommand.Kick, targetPlayerId, reason);
            OnModerationActionTaken?.Invoke(targetPlayerId, ModerationAction.Kick, reason);

            // Disconnect player
            NetworkManager.Singleton.DisconnectClient(targetPlayerId);
            Debug.Log($"Player {targetPlayerId} kicked by admin {adminId}: {reason}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void BanPlayerServerRpc(ulong adminId, ulong targetPlayerId, string reason, int durationHours, bool isPermanent, ServerRpcParams rpcParams = default)
        {
            if (!HasPermission(adminId, AdminPermission.BanPlayers))
            {
                Debug.LogWarning($"Player {adminId} lacks permission to ban players");
                return;
            }

            var ban = new Ban
            {
                banId = Guid.NewGuid().ToString(),
                playerId = targetPlayerId,
                reason = reason,
                bannedBy = adminId,
                bannedAt = DateTime.UtcNow,
                expiresAt = isPermanent ? DateTime.MaxValue : DateTime.UtcNow.AddHours(durationHours),
                isPermanent = isPermanent,
                isActive = true
            };

            RecordModeration(targetPlayerId, ModerationAction.Ban, reason, adminId, ban);
            LogAdminAction(adminId, AdminCommand.Ban, targetPlayerId, $"{reason} ({(isPermanent ? "Permanent" : $"{durationHours}h")})");
            OnModerationActionTaken?.Invoke(targetPlayerId, ModerationAction.Ban, reason);

            // Disconnect player
            NetworkManager.Singleton.DisconnectClient(targetPlayerId);
            Debug.Log($"Player {targetPlayerId} banned by admin {adminId}: {reason}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void UnbanPlayerServerRpc(ulong adminId, ulong targetPlayerId, ServerRpcParams rpcParams = default)
        {
            if (!HasPermission(adminId, AdminPermission.BanPlayers))
            {
                Debug.LogWarning($"Player {adminId} lacks permission to unban players");
                return;
            }

            if (playerModerations.TryGetValue(targetPlayerId, out var moderation))
            {
                foreach (var ban in moderation.bans.Where(b => b.isActive))
                {
                    ban.isActive = false;
                }
            }

            LogAdminAction(adminId, AdminCommand.Unban, targetPlayerId, "Ban lifted");
            Debug.Log($"Player {targetPlayerId} unbanned by admin {adminId}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void MutePlayerServerRpc(ulong adminId, ulong targetPlayerId, string reason, int durationMinutes, ServerRpcParams rpcParams = default)
        {
            if (!HasPermission(adminId, AdminPermission.MutePlayers))
            {
                Debug.LogWarning($"Player {adminId} lacks permission to mute players");
                return;
            }

            var mute = new Mute
            {
                playerId = targetPlayerId,
                reason = reason,
                mutedBy = adminId,
                mutedAt = DateTime.UtcNow,
                expiresAt = DateTime.UtcNow.AddMinutes(durationMinutes),
                isActive = true
            };

            RecordModeration(targetPlayerId, ModerationAction.Mute, reason, adminId, null, mute);
            LogAdminAction(adminId, AdminCommand.Mute, targetPlayerId, $"{reason} ({durationMinutes}m)");
            OnModerationActionTaken?.Invoke(targetPlayerId, ModerationAction.Mute, reason);

            NotifyPlayerMutedClientRpc(targetPlayerId, durationMinutes);
            Debug.Log($"Player {targetPlayerId} muted by admin {adminId}: {reason}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void UnmutePlayerServerRpc(ulong adminId, ulong targetPlayerId, ServerRpcParams rpcParams = default)
        {
            if (!HasPermission(adminId, AdminPermission.MutePlayers))
            {
                Debug.LogWarning($"Player {adminId} lacks permission to unmute players");
                return;
            }

            if (playerModerations.TryGetValue(targetPlayerId, out var moderation))
            {
                foreach (var mute in moderation.mutes.Where(m => m.isActive))
                {
                    mute.isActive = false;
                }
            }

            LogAdminAction(adminId, AdminCommand.Unmute, targetPlayerId, "Mute lifted");
            NotifyPlayerUnmutedClientRpc(targetPlayerId);
            Debug.Log($"Player {targetPlayerId} unmuted by admin {adminId}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void WarnPlayerServerRpc(ulong adminId, ulong targetPlayerId, string reason, ServerRpcParams rpcParams = default)
        {
            if (!HasPermission(adminId, AdminPermission.WarnPlayers))
            {
                Debug.LogWarning($"Player {adminId} lacks permission to warn players");
                return;
            }

            var warning = new Warning
            {
                reason = reason,
                warnedBy = adminId,
                warnedAt = DateTime.UtcNow
            };

            RecordModeration(targetPlayerId, ModerationAction.Warn, reason, adminId, null, null, warning);
            LogAdminAction(adminId, AdminCommand.Warn, targetPlayerId, reason);
            OnModerationActionTaken?.Invoke(targetPlayerId, ModerationAction.Warn, reason);

            NotifyPlayerWarningClientRpc(targetPlayerId, reason);
            Debug.Log($"Player {targetPlayerId} warned by admin {adminId}: {reason}");
        }

        // Reporting System
        [ServerRpc(RequireOwnership = false)]
        public void SubmitReportServerRpc(ulong reporterId, ulong reportedPlayerId, ReportReason reason, string description, ServerRpcParams rpcParams = default)
        {
            // Check cooldown
            if (lastReportTime.TryGetValue(reporterId, out float lastTime))
            {
                if (Time.time - lastTime < reportCooldown)
                {
                    Debug.LogWarning($"Player {reporterId} is on report cooldown");
                    return;
                }
            }

            // Check max reports
            if (playerReports.TryGetValue(reporterId, out var reports))
            {
                if (reports.Count >= maxReportsPerPlayer)
                {
                    Debug.LogWarning($"Player {reporterId} has reached max reports");
                    return;
                }
            }

            var report = new Report
            {
                reportId = Guid.NewGuid().ToString(),
                reporterId = reporterId,
                reportedPlayerId = reportedPlayerId,
                reason = reason,
                description = description,
                timestamp = DateTime.UtcNow,
                status = ReportStatus.Pending
            };

            if (!playerReports.ContainsKey(reportedPlayerId))
            {
                playerReports[reportedPlayerId] = new List<Report>();
            }

            playerReports[reportedPlayerId].Add(report);
            lastReportTime[reporterId] = Time.time;

            OnReportSubmitted?.Invoke(report);
            Debug.Log($"Report submitted by {reporterId} against {reportedPlayerId}: {reason}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void ReviewReportServerRpc(ulong adminId, string reportId, ReportStatus status, string notes, ServerRpcParams rpcParams = default)
        {
            if (!HasPermission(adminId, AdminPermission.ReviewReports))
            {
                Debug.LogWarning($"Player {adminId} lacks permission to review reports");
                return;
            }

            foreach (var reports in playerReports.Values)
            {
                var report = reports.FirstOrDefault(r => r.reportId == reportId);
                if (report != null)
                {
                    report.status = status;
                    report.reviewedBy = adminId;
                    report.reviewedAt = DateTime.UtcNow;
                    report.reviewNotes = notes;

                    LogAdminAction(adminId, AdminCommand.ReviewReport, 0, $"Report {reportId}: {status}");
                    Debug.Log($"Report {reportId} reviewed by admin {adminId}: {status}");
                    return;
                }
            }
        }

        // Server Commands
        [ServerRpc(RequireOwnership = false)]
        public void ExecuteServerCommandServerRpc(ulong adminId, string command, List<string> args, ServerRpcParams rpcParams = default)
        {
            if (!HasPermission(adminId, AdminPermission.ExecuteCommands))
            {
                Debug.LogWarning($"Player {adminId} lacks permission to execute server commands");
                return;
            }

            LogAdminAction(adminId, AdminCommand.ExecuteCommand, 0, $"{command} {string.Join(" ", args)}");
            ProcessServerCommand(adminId, command, args);
        }

        private void ProcessServerCommand(ulong adminId, string command, List<string> args)
        {
            switch (command.ToLower())
            {
                case "teleport":
                    if (args.Count >= 4)
                    {
                        ulong playerId = ulong.Parse(args[0]);
                        float x = float.Parse(args[1]);
                        float y = float.Parse(args[2]);
                        float z = float.Parse(args[3]);
                        // Teleport player logic
                        Debug.Log($"Teleporting player {playerId} to ({x}, {y}, {z})");
                    }
                    break;

                case "heal":
                    if (args.Count >= 1)
                    {
                        ulong playerId = ulong.Parse(args[0]);
                        // Heal player logic
                        Debug.Log($"Healing player {playerId}");
                    }
                    break;

                case "god":
                    if (args.Count >= 1)
                    {
                        ulong playerId = ulong.Parse(args[0]);
                        // Toggle god mode
                        Debug.Log($"Toggling god mode for player {playerId}");
                    }
                    break;

                case "giveitem":
                    if (args.Count >= 3)
                    {
                        ulong playerId = ulong.Parse(args[0]);
                        string itemId = args[1];
                        int quantity = int.Parse(args[2]);
                        // Give item logic
                        Debug.Log($"Giving {quantity}x {itemId} to player {playerId}");
                    }
                    break;

                case "setlevel":
                    if (args.Count >= 2)
                    {
                        ulong playerId = ulong.Parse(args[0]);
                        int level = int.Parse(args[1]);
                        // Set level logic
                        Debug.Log($"Setting player {playerId} level to {level}");
                    }
                    break;

                case "broadcast":
                    if (args.Count >= 1)
                    {
                        string message = string.Join(" ", args);
                        BroadcastMessageClientRpc(message);
                        Debug.Log($"Broadcasting: {message}");
                    }
                    break;

                default:
                    Debug.LogWarning($"Unknown command: {command}");
                    break;
            }

            OnAdminCommandExecuted?.Invoke(adminId, ParseCommand(command));
        }

        // Permission Checks
        private bool HasPermission(ulong adminId, AdminPermission permission)
        {
            if (!adminPermissions.TryGetValue(adminId, out var permissions))
            {
                return false;
            }

            return permissions.permissions.Contains(permission);
        }

        private List<AdminPermission> GetPermissionsForRole(AdminRole role)
        {
            switch (role)
            {
                case AdminRole.Moderator:
                    return new List<AdminPermission>
                    {
                        AdminPermission.KickPlayers,
                        AdminPermission.MutePlayers,
                        AdminPermission.WarnPlayers,
                        AdminPermission.ReviewReports,
                        AdminPermission.ViewLogs
                    };

                case AdminRole.Admin:
                    return new List<AdminPermission>
                    {
                        AdminPermission.KickPlayers,
                        AdminPermission.MutePlayers,
                        AdminPermission.WarnPlayers,
                        AdminPermission.BanPlayers,
                        AdminPermission.ReviewReports,
                        AdminPermission.ViewLogs,
                        AdminPermission.ExecuteCommands
                    };

                case AdminRole.SuperAdmin:
                    return Enum.GetValues(typeof(AdminPermission)).Cast<AdminPermission>().ToList();

                default:
                    return new List<AdminPermission>();
            }
        }

        // Audit Logging
        private void LogAdminAction(ulong adminId, AdminCommand command, ulong targetId, string details)
        {
            if (!enableAuditLog) return;

            var logEntry = new AuditLogEntry
            {
                entryId = Guid.NewGuid().ToString(),
                adminId = adminId,
                command = command,
                targetPlayerId = targetId,
                details = details,
                timestamp = DateTime.UtcNow
            };

            auditLog.Add(logEntry);
            Debug.Log($"[Audit] {adminId} executed {command} on {targetId}: {details}");
        }

        private void RecordModeration(ulong playerId, ModerationAction action, string reason, ulong adminId, Ban ban = null, Mute mute = null, Warning warning = null)
        {
            if (!playerModerations.ContainsKey(playerId))
            {
                playerModerations[playerId] = new PlayerModeration { playerId = playerId };
            }

            var moderation = playerModerations[playerId];

            if (ban != null) moderation.bans.Add(ban);
            if (mute != null) moderation.mutes.Add(mute);
            if (warning != null) moderation.warnings.Add(warning);
        }

        // Client Notifications
        [ClientRpc]
        private void NotifyPlayerMutedClientRpc(ulong playerId, int durationMinutes)
        {
            // Show mute notification to player
        }

        [ClientRpc]
        private void NotifyPlayerUnmutedClientRpc(ulong playerId)
        {
            // Show unmute notification to player
        }

        [ClientRpc]
        private void NotifyPlayerWarningClientRpc(ulong playerId, string reason)
        {
            // Show warning to player
        }

        [ClientRpc]
        private void BroadcastMessageClientRpc(string message)
        {
            // Show broadcast message to all players
        }

        // Query Methods
        public bool IsPlayerBanned(ulong playerId)
        {
            if (!playerModerations.TryGetValue(playerId, out var moderation)) return false;

            var activeBan = moderation.bans.FirstOrDefault(b => b.isActive && DateTime.UtcNow < b.expiresAt);
            return activeBan != null;
        }

        public bool IsPlayerMuted(ulong playerId)
        {
            if (!playerModerations.TryGetValue(playerId, out var moderation)) return false;

            var activeMute = moderation.mutes.FirstOrDefault(m => m.isActive && DateTime.UtcNow < m.expiresAt);
            return activeMute != null;
        }

        public List<Report> GetPendingReports() => playerReports.Values.SelectMany(r => r).Where(r => r.status == ReportStatus.Pending).ToList();
        public List<AuditLogEntry> GetAuditLog(int limit = 100) => auditLog.TakeLast(limit).ToList();
        public PlayerModeration GetPlayerModeration(ulong playerId) => playerModerations.GetValueOrDefault(playerId);

        private AdminCommand ParseCommand(string command)
        {
            return command.ToLower() switch
            {
                "kick" => AdminCommand.Kick,
                "ban" => AdminCommand.Ban,
                "mute" => AdminCommand.Mute,
                "warn" => AdminCommand.Warn,
                "teleport" => AdminCommand.Teleport,
                "heal" => AdminCommand.Heal,
                _ => AdminCommand.ExecuteCommand
            };
        }
    }

    [Serializable]
    public class AdminPermissions
    {
        public ulong playerId;
        public AdminRole role;
        public List<AdminPermission> permissions;
        public ulong grantedBy;
        public DateTime grantedAt;
    }

    [Serializable]
    public class PlayerModeration
    {
        public ulong playerId;
        public List<Ban> bans = new List<Ban>();
        public List<Mute> mutes = new List<Mute>();
        public List<Warning> warnings = new List<Warning>();
    }

    [Serializable]
    public class Ban
    {
        public string banId;
        public ulong playerId;
        public string reason;
        public ulong bannedBy;
        public DateTime bannedAt;
        public DateTime expiresAt;
        public bool isPermanent;
        public bool isActive;
    }

    [Serializable]
    public class Mute
    {
        public ulong playerId;
        public string reason;
        public ulong mutedBy;
        public DateTime mutedAt;
        public DateTime expiresAt;
        public bool isActive;
    }

    [Serializable]
    public class Warning
    {
        public string reason;
        public ulong warnedBy;
        public DateTime warnedAt;
    }

    [Serializable]
    public class Report
    {
        public string reportId;
        public ulong reporterId;
        public ulong reportedPlayerId;
        public ReportReason reason;
        public string description;
        public DateTime timestamp;
        public ReportStatus status;
        public ulong reviewedBy;
        public DateTime reviewedAt;
        public string reviewNotes;
    }

    [Serializable]
    public class AuditLogEntry
    {
        public string entryId;
        public ulong adminId;
        public AdminCommand command;
        public ulong targetPlayerId;
        public string details;
        public DateTime timestamp;
    }

    public enum AdminRole { None, Moderator, Admin, SuperAdmin }
    
    public enum AdminPermission
    {
        KickPlayers, BanPlayers, MutePlayers, WarnPlayers,
        ReviewReports, ViewLogs, ExecuteCommands, ManageAdmins,
        ModifyPlayer, TeleportPlayers
    }

    public enum ModerationAction { Kick, Ban, Mute, Warn }

    public enum AdminCommand
    {
        Kick, Ban, Unban, Mute, Unmute, Warn,
        GrantPermissions, RevokePermissions, ReviewReport,
        ExecuteCommand, Teleport, Heal, GiveItem, SetLevel
    }

    public enum ReportReason
    {
        Cheating, Harassment, Exploiting, Griefing,
        InappropriateName, InappropriateChat, Teaming, Other
    }

    public enum ReportStatus { Pending, UnderReview, Resolved, Dismissed }
}
