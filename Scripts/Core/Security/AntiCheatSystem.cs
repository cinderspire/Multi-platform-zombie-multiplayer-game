using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace DeadFrontier.Core.Security
{
    /// <summary>
    /// Anti-cheat system with server-side validation and client monitoring
    /// Detects speed hacks, teleport hacks, stat manipulation, and suspicious behavior
    /// </summary>
    public class AntiCheatSystem : NetworkBehaviour
    {
        [Header("Detection Settings")]
        [SerializeField] private bool enableSpeedCheck = true;
        [SerializeField] private float maxAllowedSpeed = 15f; // m/s
        [SerializeField] private bool enableTeleportCheck = true;
        [SerializeField] private float maxAllowedTeleportDistance = 10f;
        [SerializeField] private bool enableStatValidation = true;
        [SerializeField] private bool enableRateLimit = true;

        [Header("Thresholds")]
        [SerializeField] private int maxViolationsBeforeKick = 3;
        [SerializeField] private float violationResetTime = 60f;
        [SerializeField] private float positionCheckInterval = 0.1f;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;
        [SerializeField] private bool logViolations = true;

        // Client tracking (Server-side)
        private Dictionary<ulong, ClientMonitorData> clientMonitors = new Dictionary<ulong, ClientMonitorData>();

        // Position tracking
        private float positionCheckTimer = 0f;

        // Events
        public event System.Action<ulong, ViolationType> OnViolationDetected;
        public event System.Action<ulong> OnPlayerKicked;

        private void Update()
        {
            if (!IsServer) return;

            positionCheckTimer += Time.deltaTime;
            if (positionCheckTimer >= positionCheckInterval)
            {
                positionCheckTimer = 0f;
                CheckAllClients();
            }
        }

        #region Server - Monitoring

        /// <summary>
        /// Registers a client for monitoring (Server only)
        /// </summary>
        public void RegisterClient(ulong clientId)
        {
            if (!IsServer) return;

            if (!clientMonitors.ContainsKey(clientId))
            {
                clientMonitors[clientId] = new ClientMonitorData
                {
                    clientId = clientId,
                    lastPosition = Vector3.zero,
                    lastCheckTime = Time.time,
                    violations = 0,
                    lastViolationTime = 0f
                };

                if (showDebugLogs)
                    Debug.Log($"[AntiCheatSystem] Registered client {clientId} for monitoring");
            }
        }

        /// <summary>
        /// Unregisters a client from monitoring
        /// </summary>
        public void UnregisterClient(ulong clientId)
        {
            if (!IsServer) return;

            if (clientMonitors.Remove(clientId))
            {
                if (showDebugLogs)
                    Debug.Log($"[AntiCheatSystem] Unregistered client {clientId}");
            }
        }

        private void CheckAllClients()
        {
            if (!IsServer) return;

            foreach (var kvp in clientMonitors)
            {
                CheckClient(kvp.Key, kvp.Value);
            }

            // Reset old violations
            ResetOldViolations();
        }

        private void CheckClient(ulong clientId, ClientMonitorData data)
        {
            // Get player object
            if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId))
                return;

            var networkObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
            if (networkObject == null)
                return;

            Vector3 currentPosition = networkObject.transform.position;
            float currentTime = Time.time;
            float deltaTime = currentTime - data.lastCheckTime;

            if (deltaTime <= 0f) return;

            // Speed check
            if (enableSpeedCheck)
            {
                CheckSpeed(clientId, data, currentPosition, deltaTime);
            }

            // Teleport check
            if (enableTeleportCheck)
            {
                CheckTeleport(clientId, data, currentPosition);
            }

            // Update data
            data.lastPosition = currentPosition;
            data.lastCheckTime = currentTime;
        }

        private void CheckSpeed(ulong clientId, ClientMonitorData data, Vector3 currentPosition, float deltaTime)
        {
            if (data.lastPosition == Vector3.zero)
                return;

            float distance = Vector3.Distance(currentPosition, data.lastPosition);
            float speed = distance / deltaTime;

            if (speed > maxAllowedSpeed)
            {
                RecordViolation(clientId, ViolationType.SpeedHack, $"Speed: {speed:F2} m/s (max: {maxAllowedSpeed})");
            }
        }

        private void CheckTeleport(ulong clientId, ClientMonitorData data, Vector3 currentPosition)
        {
            if (data.lastPosition == Vector3.zero)
                return;

            float distance = Vector3.Distance(currentPosition, data.lastPosition);

            if (distance > maxAllowedTeleportDistance)
            {
                RecordViolation(clientId, ViolationType.TeleportHack, $"Teleport distance: {distance:F2}m");
            }
        }

        private void ResetOldViolations()
        {
            float currentTime = Time.time;

            foreach (var data in clientMonitors.Values)
            {
                if (data.violations > 0 && currentTime - data.lastViolationTime > violationResetTime)
                {
                    data.violations = 0;

                    if (showDebugLogs)
                        Debug.Log($"[AntiCheatSystem] Reset violations for client {data.clientId}");
                }
            }
        }

        #endregion

        #region Violation Handling

        private void RecordViolation(ulong clientId, ViolationType violationType, string details)
        {
            if (!clientMonitors.ContainsKey(clientId))
                return;

            var data = clientMonitors[clientId];
            data.violations++;
            data.lastViolationTime = Time.time;

            if (logViolations)
            {
                Debug.LogWarning($"[AntiCheatSystem] Violation detected! Client: {clientId}, Type: {violationType}, Details: {details}, Total: {data.violations}/{maxViolationsBeforeKick}");
            }

            OnViolationDetected?.Invoke(clientId, violationType);

            // Track analytics
            Core.Analytics.AnalyticsManager.Instance?.TrackEvent("anticheat_violation", new Dictionary<string, object>
            {
                { "client_id", clientId.ToString() },
                { "violation_type", violationType.ToString() },
                { "details", details },
                { "total_violations", data.violations }
            });

            // Kick if threshold reached
            if (data.violations >= maxViolationsBeforeKick)
            {
                KickPlayer(clientId, violationType);
            }
        }

        private void KickPlayer(ulong clientId, ViolationType reason)
        {
            if (!IsServer) return;

            Debug.LogError($"[AntiCheatSystem] Kicking client {clientId} for {reason}");

            OnPlayerKicked?.Invoke(clientId);

            // Track analytics
            Core.Analytics.AnalyticsManager.Instance?.TrackEvent("player_kicked_anticheat", new Dictionary<string, object>
            {
                { "client_id", clientId.ToString() },
                { "reason", reason.ToString() }
            });

            // Disconnect player
            if (NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId))
            {
                NetworkManager.Singleton.DisconnectClient(clientId);
            }

            // Remove from monitoring
            UnregisterClient(clientId);
        }

        #endregion

        #region Stat Validation

        /// <summary>
        /// Validates player stats (Server only)
        /// </summary>
        public bool ValidatePlayerStats(ulong clientId, float health, float maxHealth, float speed)
        {
            if (!IsServer) return true;

            if (!enableStatValidation) return true;

            bool isValid = true;

            // Health validation
            if (health > maxHealth || health < 0f)
            {
                RecordViolation(clientId, ViolationType.StatManipulation, $"Invalid health: {health}/{maxHealth}");
                isValid = false;
            }

            // Max health validation (should be within reasonable bounds)
            if (maxHealth > 1000f || maxHealth < 1f)
            {
                RecordViolation(clientId, ViolationType.StatManipulation, $"Invalid max health: {maxHealth}");
                isValid = false;
            }

            // Speed validation
            if (speed > maxAllowedSpeed * 2f) // Allow some leeway for perks
            {
                RecordViolation(clientId, ViolationType.StatManipulation, $"Invalid speed: {speed}");
                isValid = false;
            }

            return isValid;
        }

        /// <summary>
        /// Validates damage dealt (Server only)
        /// </summary>
        public bool ValidateDamage(ulong clientId, float damage, float weaponMaxDamage)
        {
            if (!IsServer) return true;

            if (!enableStatValidation) return true;

            // Damage should not exceed weapon's max damage * 2 (allowing for headshots/perks)
            if (damage > weaponMaxDamage * 3f)
            {
                RecordViolation(clientId, ViolationType.DamageHack, $"Suspicious damage: {damage} (max: {weaponMaxDamage})");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates item pickup (Server only)
        /// </summary>
        public bool ValidateItemPickup(ulong clientId, string itemId)
        {
            if (!IsServer) return true;

            if (!enableRateLimit) return true;

            if (!clientMonitors.ContainsKey(clientId))
                return true;

            var data = clientMonitors[clientId];

            // Rate limit item pickups
            float timeSinceLastPickup = Time.time - data.lastItemPickupTime;
            if (timeSinceLastPickup < 0.1f) // Max 10 pickups per second
            {
                RecordViolation(clientId, ViolationType.RateLimit, "Item pickup rate limit exceeded");
                return false;
            }

            data.lastItemPickupTime = Time.time;
            return true;
        }

        #endregion

        #region Hash Validation

        /// <summary>
        /// Validates client game files (basic implementation)
        /// </summary>
        public bool ValidateClientHash(ulong clientId, string fileHash)
        {
            if (!IsServer) return true;

            // TODO: Compare with server's file hash
            // In production, this would validate that client hasn't modified game files

            string expectedHash = GetExpectedFileHash();

            if (fileHash != expectedHash)
            {
                RecordViolation(clientId, ViolationType.FileModification, "File hash mismatch");
                return false;
            }

            return true;
        }

        private string GetExpectedFileHash()
        {
            // TODO: Calculate or retrieve expected hash
            return "expected_hash_placeholder";
        }

        #endregion

        #region Public API

        /// <summary>
        /// Gets violation count for a client
        /// </summary>
        public int GetViolationCount(ulong clientId)
        {
            if (clientMonitors.ContainsKey(clientId))
                return clientMonitors[clientId].violations;
            return 0;
        }

        /// <summary>
        /// Manually report suspicious activity
        /// </summary>
        public void ReportSuspiciousActivity(ulong clientId, string reason)
        {
            if (!IsServer) return;

            RecordViolation(clientId, ViolationType.Suspicious, reason);
        }

        /// <summary>
        /// Clears violations for a client (admin command)
        /// </summary>
        public void ClearViolations(ulong clientId)
        {
            if (!IsServer) return;

            if (clientMonitors.ContainsKey(clientId))
            {
                clientMonitors[clientId].violations = 0;

                if (showDebugLogs)
                    Debug.Log($"[AntiCheatSystem] Cleared violations for client {clientId}");
            }
        }

        #endregion
    }

    #region Data Structures

    public enum ViolationType
    {
        SpeedHack,
        TeleportHack,
        StatManipulation,
        DamageHack,
        RateLimit,
        FileModification,
        Suspicious
    }

    public class ClientMonitorData
    {
        public ulong clientId;
        public Vector3 lastPosition;
        public float lastCheckTime;
        public int violations;
        public float lastViolationTime;
        public float lastItemPickupTime;
        public Dictionary<string, float> actionTimestamps = new Dictionary<string, float>();
    }

    #endregion
}
