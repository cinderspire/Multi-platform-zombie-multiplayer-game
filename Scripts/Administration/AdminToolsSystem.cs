using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Administration
{
    /// <summary>
    /// Comprehensive admin and moderation tools system for multi-platform zombie multiplayer game.
    /// Provides server administration, player management, and live game monitoring capabilities.
    /// </summary>
    public class AdminToolsSystem : NetworkBehaviour
    {
        public static AdminToolsSystem Instance { get; private set; }

        [Header("Admin Settings")]
        [SerializeField] private bool enableAdminTools = true;
        [SerializeField] private int maxAdmins = 50;
        [SerializeField] private bool logAdminActions = true;
        [SerializeField] private bool requireAuthToken = true;

        [Header("Permission Levels")]
        [SerializeField] private bool enableRoleBased Permissions = true;

        // Enums
        public enum AdminRole
        {
            SuperAdmin,     // Full control
            Admin,          // Most permissions
            Moderator,      // Player moderation
            Helper,         // Limited assistance
            TrialMod        // Trial period
        }

        public enum AdminPermission
        {
            KickPlayers,
            BanPlayers,
            MutePlayers,
            TeleportPlayers,
            GiveItems,
            ModifyCurrency,
            ModifyStats,
            ManageServer,
            ViewLogs,
            ManageAdmins,
            BroadcastMessages,
            ModifyGameSettings,
            SpawnEntities,
            ViewPlayerData,
            ExecuteCommands
        }

        public enum AdminAction
        {
            PlayerKicked,
            PlayerBanned,
            PlayerMuted,
            PlayerTeleported,
            ItemGiven,
            CurrencyModified,
            StatsModified,
            ServerShutdown,
            ServerRestart,
            MessageBroadcast,
            SettingChanged,
            EntitySpawned,
            CommandExecuted
        }

        // Data structures
        [Serializable]
        public class AdminAccount
        {
            public ulong adminId;
            public string adminName;
            public AdminRole role;
            public HashSet<AdminPermission> permissions = new HashSet<AdminPermission>();
            public DateTime grantedDate;
            public ulong grantedBy;
            public bool isActive;
            public string authToken;
            public List<AdminActionLog> actionHistory = new List<AdminActionLog>();
            public int totalActions;
        }

        [Serializable]
        public class AdminActionLog
        {
            public string actionId;
            public ulong adminId;
            public AdminAction actionType;
            public DateTime timestamp;
            public string description;
            public ulong targetPlayerId;
            public Dictionary<string, object> actionData = new Dictionary<string, object>();
        }

        [Serializable]
        public class PlayerMonitorData
        {
            public ulong playerId;
            public Vector3 position;
            public int health;
            public int ammo;
            public float ping;
            public bool isAlive;
            public int kills;
            public int deaths;
            public float playtime;
            public DateTime lastAction;
            public List<string> recentActions = new List<string>();
        }

        [Serializable]
        public class ServerCommand
        {
            public string commandName;
            public string description;
            public AdminPermission requiredPermission;
            public List<string> parameters;
            public Action<ulong, string[]> executeAction;
        }

        // State
        private Dictionary<ulong, AdminAccount> admins = new Dictionary<ulong, AdminAccount>();
        private List<AdminActionLog> globalActionLog = new List<AdminActionLog>();
        private Dictionary<ulong, PlayerMonitorData> monitoredPlayers = new Dictionary<ulong, PlayerMonitorData>();
        private Dictionary<string, ServerCommand> commands = new Dictionary<string, ServerCommand>();

        // Events
        public event Action<AdminActionLog> OnAdminActionPerformed;
        public event Action<ulong, AdminRole> OnAdminRoleChanged;
        public event Action<string> OnServerCommandExecuted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                InitializeAdminSystem();
                RegisterCommands();
            }
        }

        #region Initialization

        private void InitializeAdminSystem()
        {
            LoadAdminAccounts();
            InitializeDefaultPermissions();
        }

        private void LoadAdminAccounts()
        {
            // Load from persistent storage or configuration
            // For now, create a default super admin if none exist
            if (admins.Count == 0)
            {
                Debug.LogWarning("No admin accounts found. Admin system initialized without accounts.");
            }
        }

        private void InitializeDefaultPermissions()
        {
            // Define default permissions for each role
            var allPermissions = Enum.GetValues(typeof(AdminPermission)).Cast<AdminPermission>().ToHashSet();
            
            // SuperAdmin gets all permissions
            // Admin gets most permissions except ManageAdmins
            // Moderator gets player moderation permissions
            // Helper gets view and broadcast permissions
            // TrialMod gets limited moderation permissions
        }

        #endregion

        #region Admin Management

        [ServerRpc(RequireOwnership = false)]
        public void GrantAdminServerRpc(ulong requesterId, ulong targetId, AdminRole role, string authToken = "")
        {
            if (!HasPermission(requesterId, AdminPermission.ManageAdmins))
            {
                Debug.LogWarning($"Player {requesterId} doesn't have permission to grant admin");
                return;
            }

            if (admins.Count >= maxAdmins)
            {
                Debug.LogWarning("Maximum admin accounts reached");
                return;
            }

            var admin = new AdminAccount
            {
                adminId = targetId,
                adminName = GetPlayerName(targetId),
                role = role,
                grantedDate = DateTime.UtcNow,
                grantedBy = requesterId,
                isActive = true,
                authToken = authToken
            };

            AssignPermissionsForRole(admin);
            admins[targetId] = admin;

            LogAdminAction(new AdminActionLog
            {
                actionId = Guid.NewGuid().ToString(),
                adminId = requesterId,
                actionType = AdminAction.SettingChanged,
                timestamp = DateTime.UtcNow,
                description = $"Granted {role} to player {targetId}",
                targetPlayerId = targetId
            });

            OnAdminRoleChanged?.Invoke(targetId, role);
            NotifyAdminGrantedClientRpc(targetId, role);

            Debug.Log($"Admin role {role} granted to player {targetId} by {requesterId}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void RevokeAdminServerRpc(ulong requesterId, ulong targetId)
        {
            if (!HasPermission(requesterId, AdminPermission.ManageAdmins))
                return;

            if (!admins.ContainsKey(targetId))
                return;

            admins[targetId].isActive = false;

            LogAdminAction(new AdminActionLog
            {
                actionId = Guid.NewGuid().ToString(),
                adminId = requesterId,
                actionType = AdminAction.SettingChanged,
                timestamp = DateTime.UtcNow,
                description = $"Revoked admin from player {targetId}",
                targetPlayerId = targetId
            });

            admins.Remove(targetId);

            Debug.Log($"Admin revoked from player {targetId}");
        }

        private void AssignPermissionsForRole(AdminAccount admin)
        {
            admin.permissions.Clear();

            switch (admin.role)
            {
                case AdminRole.SuperAdmin:
                    admin.permissions = Enum.GetValues(typeof(AdminPermission)).Cast<AdminPermission>().ToHashSet();
                    break;

                case AdminRole.Admin:
                    admin.permissions.Add(AdminPermission.KickPlayers);
                    admin.permissions.Add(AdminPermission.BanPlayers);
                    admin.permissions.Add(AdminPermission.MutePlayers);
                    admin.permissions.Add(AdminPermission.TeleportPlayers);
                    admin.permissions.Add(AdminPermission.GiveItems);
                    admin.permissions.Add(AdminPermission.ViewLogs);
                    admin.permissions.Add(AdminPermission.BroadcastMessages);
                    admin.permissions.Add(AdminPermission.SpawnEntities);
                    admin.permissions.Add(AdminPermission.ViewPlayerData);
                    break;

                case AdminRole.Moderator:
                    admin.permissions.Add(AdminPermission.KickPlayers);
                    admin.permissions.Add(AdminPermission.BanPlayers);
                    admin.permissions.Add(AdminPermission.MutePlayers);
                    admin.permissions.Add(AdminPermission.ViewLogs);
                    admin.permissions.Add(AdminPermission.ViewPlayerData);
                    break;

                case AdminRole.Helper:
                    admin.permissions.Add(AdminPermission.ViewLogs);
                    admin.permissions.Add(AdminPermission.BroadcastMessages);
                    admin.permissions.Add(AdminPermission.ViewPlayerData);
                    break;

                case AdminRole.TrialMod:
                    admin.permissions.Add(AdminPermission.MutePlayers);
                    admin.permissions.Add(AdminPermission.ViewLogs);
                    break;
            }
        }

        #endregion

        #region Player Management

        [ServerRpc(RequireOwnership = false)]
        public void KickPlayerServerRpc(ulong adminId, ulong targetId, string reason)
        {
            if (!HasPermission(adminId, AdminPermission.KickPlayers))
                return;

            LogAdminAction(new AdminActionLog
            {
                actionId = Guid.NewGuid().ToString(),
                adminId = adminId,
                actionType = AdminAction.PlayerKicked,
                timestamp = DateTime.UtcNow,
                description = $"Kicked player {targetId}: {reason}",
                targetPlayerId = targetId,
                actionData = new Dictionary<string, object> { { "reason", reason } }
            });

            NetworkManager.Singleton?.DisconnectClient(targetId);

            Debug.Log($"Admin {adminId} kicked player {targetId}: {reason}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void BanPlayerServerRpc(ulong adminId, ulong targetId, int durationHours, string reason)
        {
            if (!HasPermission(adminId, AdminPermission.BanPlayers))
                return;

            LogAdminAction(new AdminActionLog
            {
                actionId = Guid.NewGuid().ToString(),
                adminId = adminId,
                actionType = AdminAction.PlayerBanned,
                timestamp = DateTime.UtcNow,
                description = $"Banned player {targetId} for {durationHours}h: {reason}",
                targetPlayerId = targetId,
                actionData = new Dictionary<string, object>
                {
                    { "durationHours", durationHours },
                    { "reason", reason }
                }
            });

            // Integration with moderation system
            if (Moderation.ReportSystem.Instance != null)
            {
                var banType = durationHours == 0
                    ? Moderation.ReportSystem.BanType.Permanent
                    : Moderation.ReportSystem.BanType.Temporary;

                Moderation.ReportSystem.Instance.BanPlayer(targetId, adminId, reason, banType);
            }

            NetworkManager.Singleton?.DisconnectClient(targetId);

            Debug.Log($"Admin {adminId} banned player {targetId} for {durationHours} hours: {reason}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void TeleportPlayerServerRpc(ulong adminId, ulong targetId, Vector3 position)
        {
            if (!HasPermission(adminId, AdminPermission.TeleportPlayers))
                return;

            LogAdminAction(new AdminActionLog
            {
                actionId = Guid.NewGuid().ToString(),
                adminId = adminId,
                actionType = AdminAction.PlayerTeleported,
                timestamp = DateTime.UtcNow,
                description = $"Teleported player {targetId} to {position}",
                targetPlayerId = targetId,
                actionData = new Dictionary<string, object> { { "position", position } }
            });

            // Implement teleport logic based on your player controller
            TeleportPlayerClientRpc(targetId, position);
        }

        [ServerRpc(RequireOwnership = false)]
        public void GiveItemServerRpc(ulong adminId, ulong targetId, string itemId, int quantity)
        {
            if (!HasPermission(adminId, AdminPermission.GiveItems))
                return;

            LogAdminAction(new AdminActionLog
            {
                actionId = Guid.NewGuid().ToString(),
                adminId = adminId,
                actionType = AdminAction.ItemGiven,
                timestamp = DateTime.UtcNow,
                description = $"Gave {quantity}x {itemId} to player {targetId}",
                targetPlayerId = targetId,
                actionData = new Dictionary<string, object>
                {
                    { "itemId", itemId },
                    { "quantity", quantity }
                }
            });

            Inventory.InventoryManager.Instance?.AddItem(targetId, itemId, quantity);

            Debug.Log($"Admin {adminId} gave {quantity}x {itemId} to player {targetId}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void ModifyCurrencyServerRpc(ulong adminId, ulong targetId, int softCurrency, int hardCurrency)
        {
            if (!HasPermission(adminId, AdminPermission.ModifyCurrency))
                return;

            LogAdminAction(new AdminActionLog
            {
                actionId = Guid.NewGuid().ToString(),
                adminId = adminId,
                actionType = AdminAction.CurrencyModified,
                timestamp = DateTime.UtcNow,
                description = $"Modified currency for player {targetId}",
                targetPlayerId = targetId,
                actionData = new Dictionary<string, object>
                {
                    { "softCurrency", softCurrency },
                    { "hardCurrency", hardCurrency }
                }
            });

            if (softCurrency != 0)
            {
                if (softCurrency > 0)
                    Economy.EconomyManager.Instance?.AddSoftCurrency(targetId, softCurrency);
                else
                    Economy.EconomyManager.Instance?.SpendSoftCurrency(targetId, -softCurrency);
            }

            if (hardCurrency != 0)
            {
                if (hardCurrency > 0)
                    Economy.EconomyManager.Instance?.AddHardCurrency(targetId, hardCurrency);
                else
                    Economy.EconomyManager.Instance?.SpendHardCurrency(targetId, -hardCurrency);
            }
        }

        #endregion

        #region Server Management

        [ServerRpc(RequireOwnership = false)]
        public void BroadcastMessageServerRpc(ulong adminId, string message, float displayDuration = 10f)
        {
            if (!HasPermission(adminId, AdminPermission.BroadcastMessages))
                return;

            LogAdminAction(new AdminActionLog
            {
                actionId = Guid.NewGuid().ToString(),
                adminId = adminId,
                actionType = AdminAction.MessageBroadcast,
                timestamp = DateTime.UtcNow,
                description = $"Broadcast message: {message}",
                actionData = new Dictionary<string, object> { { "message", message } }
            });

            BroadcastMessageClientRpc(message, displayDuration);
        }

        [ServerRpc(RequireOwnership = false)]
        public void ExecuteCommandServerRpc(ulong adminId, string commandName, string[] parameters)
        {
            if (!HasPermission(adminId, AdminPermission.ExecuteCommands))
                return;

            if (!commands.ContainsKey(commandName))
            {
                Debug.LogWarning($"Unknown command: {commandName}");
                return;
            }

            var command = commands[commandName];

            if (!HasPermission(adminId, command.requiredPermission))
            {
                Debug.LogWarning($"Admin {adminId} doesn't have permission for command {commandName}");
                return;
            }

            LogAdminAction(new AdminActionLog
            {
                actionId = Guid.NewGuid().ToString(),
                adminId = adminId,
                actionType = AdminAction.CommandExecuted,
                timestamp = DateTime.UtcNow,
                description = $"Executed command: {commandName}",
                actionData = new Dictionary<string, object>
                {
                    { "command", commandName },
                    { "parameters", parameters }
                }
            });

            command.executeAction?.Invoke(adminId, parameters);

            OnServerCommandExecuted?.Invoke(commandName);
        }

        #endregion

        #region Player Monitoring

        public void UpdatePlayerMonitorData(ulong playerId, Vector3 position, int health, float ping, bool isAlive)
        {
            if (!monitoredPlayers.ContainsKey(playerId))
            {
                monitoredPlayers[playerId] = new PlayerMonitorData { playerId = playerId };
            }

            var data = monitoredPlayers[playerId];
            data.position = position;
            data.health = health;
            data.ping = ping;
            data.isAlive = isAlive;
            data.lastAction = DateTime.UtcNow;
        }

        public PlayerMonitorData GetPlayerMonitorData(ulong playerId)
        {
            return monitoredPlayers.ContainsKey(playerId) ? monitoredPlayers[playerId] : null;
        }

        public List<PlayerMonitorData> GetAllMonitoredPlayers()
        {
            return monitoredPlayers.Values.ToList();
        }

        #endregion

        #region Commands

        private void RegisterCommands()
        {
            RegisterCommand("heal", "Heal a player to full health", AdminPermission.ModifyStats,
                new List<string> { "playerId" }, (adminId, args) =>
                {
                    if (ulong.TryParse(args[0], out ulong targetId))
                    {
                        // Implement heal logic
                        Debug.Log($"Healed player {targetId}");
                    }
                });

            RegisterCommand("godmode", "Toggle god mode for a player", AdminPermission.ModifyStats,
                new List<string> { "playerId", "enabled" }, (adminId, args) =>
                {
                    // Implement god mode logic
                });

            RegisterCommand("noclip", "Toggle noclip for a player", AdminPermission.TeleportPlayers,
                new List<string> { "playerId", "enabled" }, (adminId, args) =>
                {
                    // Implement noclip logic
                });

            RegisterCommand("spawn", "Spawn an entity", AdminPermission.SpawnEntities,
                new List<string> { "entityId", "quantity" }, (adminId, args) =>
                {
                    // Implement spawn logic
                });

            RegisterCommand("weather", "Change weather", AdminPermission.ModifyGameSettings,
                new List<string> { "weatherType" }, (adminId, args) =>
                {
                    // Implement weather change logic
                });

            RegisterCommand("time", "Set time of day", AdminPermission.ModifyGameSettings,
                new List<string> { "timeOfDay" }, (adminId, args) =>
                {
                    // Implement time change logic
                });
        }

        private void RegisterCommand(string name, string description, AdminPermission permission,
            List<string> parameters, Action<ulong, string[]> action)
        {
            commands[name] = new ServerCommand
            {
                commandName = name,
                description = description,
                requiredPermission = permission,
                parameters = parameters,
                executeAction = action
            };
        }

        #endregion

        #region Permissions

        public bool HasPermission(ulong adminId, AdminPermission permission)
        {
            if (!admins.ContainsKey(adminId)) return false;

            var admin = admins[adminId];
            if (!admin.isActive) return false;

            return admin.permissions.Contains(permission);
        }

        public bool IsAdmin(ulong playerId)
        {
            return admins.ContainsKey(playerId) && admins[playerId].isActive;
        }

        #endregion

        #region Logging

        private void LogAdminAction(AdminActionLog log)
        {
            if (!logAdminActions) return;

            globalActionLog.Add(log);

            if (admins.ContainsKey(log.adminId))
            {
                var admin = admins[log.adminId];
                admin.actionHistory.Add(log);
                admin.totalActions++;
            }

            OnAdminActionPerformed?.Invoke(log);

            // Keep last 1000 global actions
            if (globalActionLog.Count > 1000)
            {
                globalActionLog.RemoveAt(0);
            }
        }

        public List<AdminActionLog> GetAdminActionHistory(ulong adminId)
        {
            return admins.ContainsKey(adminId) ? admins[adminId].actionHistory : new List<AdminActionLog>();
        }

        public List<AdminActionLog> GetGlobalActionLog(int limit = 100)
        {
            return globalActionLog.TakeLast(limit).ToList();
        }

        #endregion

        #region Utility

        private string GetPlayerName(ulong playerId)
        {
            return $"Player_{playerId}";
        }

        public AdminAccount GetAdminAccount(ulong adminId)
        {
            return admins.ContainsKey(adminId) ? admins[adminId] : null;
        }

        public List<AdminAccount> GetAllAdmins()
        {
            return admins.Values.Where(a => a.isActive).ToList();
        }

        #endregion

        #region ClientRpc

        [ClientRpc]
        private void NotifyAdminGrantedClientRpc(ulong adminId, AdminRole role)
        {
            OnAdminRoleChanged?.Invoke(adminId, role);
        }

        [ClientRpc]
        private void TeleportPlayerClientRpc(ulong playerId, Vector3 position)
        {
            if (NetworkManager.Singleton.LocalClientId != playerId) return;
            // Implement teleport on client
        }

        [ClientRpc]
        private void BroadcastMessageClientRpc(string message, float duration)
        {
            // Display message to all clients
            Debug.Log($"[SERVER] {message}");
        }

        #endregion
    }
}
