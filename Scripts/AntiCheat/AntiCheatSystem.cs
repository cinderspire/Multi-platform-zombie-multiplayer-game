using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace AntiCheat
{
    /// <summary>
    /// Comprehensive anti-cheat detection system for multi-platform zombie multiplayer game.
    /// Detects speed hacks, aimbots, wallhacks, teleportation, and other exploits through server-side validation.
    /// </summary>
    public class AntiCheatSystem : NetworkBehaviour
    {
        public static AntiCheatSystem Instance { get; private set; }

        [Header("Detection Settings")]
        [SerializeField] private bool enableAntiCheat = true;
        [SerializeField] private float detectionInterval = 0.5f;
        [SerializeField] private bool autoKickOnDetection = true;
        [SerializeField] private int violationThreshold = 3;

        [Header("Speed Hack Detection")]
        [SerializeField] private bool detectSpeedHacks = true;
        [SerializeField] private float maxAllowedSpeed = 15f;
        [SerializeField] private float speedCheckInterval = 0.1f;
        [SerializeField] private int consecutiveSpeedViolations = 5;

        [Header("Teleport Detection")]
        [SerializeField] private bool detectTeleports = true;
        [SerializeField] private float maxAllowedDistance = 50f;
        [SerializeField] private float teleportCheckInterval = 0.2f;

        [Header("Aimbot Detection")]
        [SerializeField] private bool detectAimbots = true;
        [SerializeField] private float suspiciousAimSpeed = 720f; // degrees per second
        [SerializeField] private float perfectAimThreshold = 0.99f; // Accuracy threshold
        [SerializeField] private int consecutiveHeadshotThreshold = 10;

        [Header("Wallhack Detection")]
        [SerializeField] private bool detectWallhacks = true;
        [SerializeField] private float wallhackSuspicionThreshold = 0.8f;
        [SerializeField] private int impossibleShotsThreshold = 5;

        [Header("Rate of Fire Detection")]
        [SerializeField] private bool detectRoFManipulation = true;
        [SerializeField] private float maxRoFDeviation = 1.5f; // 150% of normal RoF

        [Header("Movement Validation")]
        [SerializeField] private bool validateMovement = true;
        [SerializeField] private float maxVerticalSpeed = 20f;
        [SerializeField] private bool validatePhysics = true;

        [Header("Client Integrity")]
        [SerializeField] private bool validateClientTimestamps = true;
        [SerializeField] private float maxTimestampDeviation = 2f;
        [SerializeField] private bool detectModifiedClients = true;

        [Header("Behavior Analysis")]
        [SerializeField] private bool enableBehaviorAnalysis = true;
        [SerializeField] private int statisticalSampleSize = 100;
        [SerializeField] private float behaviorAnomalyThreshold = 3.5f; // Standard deviations

        // Enums
        public enum ViolationType
        {
            SpeedHack,
            Teleport,
            Aimbot,
            Wallhack,
            RoFManipulation,
            InvalidMovement,
            TimestampManipulation,
            ModifiedClient,
            InventoryManipulation,
            HealthManipulation,
            NoClip,
            FlyHack,
            StatisticalAnomaly
        }

        public enum DetectionSeverity
        {
            Low,
            Medium,
            High,
            Critical
        }

        // Data structures
        [Serializable]
        public class PlayerMonitoringData
        {
            public ulong playerId;
            public Vector3 lastPosition;
            public Vector3 lastVelocity;
            public Quaternion lastRotation;
            public float lastUpdateTime;
            public int totalViolations;
            public Dictionary<ViolationType, int> violationCounts = new Dictionary<ViolationType, int>();
            public Queue<Vector3> positionHistory = new Queue<Vector3>();
            public Queue<float> velocityHistory = new Queue<float>();
            public Queue<ShotData> shotHistory = new Queue<ShotData>();
            public Queue<float> timestampHistory = new Queue<float>();
            public bool isFlagged;
            public float flaggedTime;
        }

        [Serializable]
        public class ShotData
        {
            public float timestamp;
            public Vector3 origin;
            public Vector3 direction;
            public bool isHeadshot;
            public bool throughWall;
            public float distance;
            public float aimSpeed;
            public ulong targetId;
        }

        [Serializable]
        public class Violation
        {
            public string violationId;
            public ulong playerId;
            public ViolationType type;
            public DetectionSeverity severity;
            public DateTime timestamp;
            public string description;
            public Dictionary<string, object> evidence = new Dictionary<string, object>();
            public bool processed;
            public bool resultedInKick;
        }

        [Serializable]
        public class BehaviorStatistics
        {
            public float averageAccuracy;
            public float averageHeadshotRate;
            public float averageSpeed;
            public float averageReactionTime;
            public int totalShots;
            public int totalHits;
            public int totalHeadshots;
            public float averageAimSpeed;
            public Dictionary<string, float> customStats = new Dictionary<string, float>();
        }

        [Serializable]
        public class ValidationResult
        {
            public bool isValid;
            public ViolationType? violationType;
            public DetectionSeverity severity;
            public string reason;
            public Dictionary<string, object> data = new Dictionary<string, object>();
        }

        // State
        private Dictionary<ulong, PlayerMonitoringData> monitoredPlayers = new Dictionary<ulong, PlayerMonitoringData>();
        private Dictionary<ulong, BehaviorStatistics> playerStatistics = new Dictionary<ulong, BehaviorStatistics>();
        private List<Violation> violations = new List<Violation>();
        private Dictionary<ulong, int> consecutiveSpeedViolations = new Dictionary<ulong, int>();
        private Dictionary<ulong, int> consecutiveHeadshots = new Dictionary<ulong, int>();
        private Dictionary<ulong, float> lastShotTimes = new Dictionary<ulong, float>();

        private float lastDetectionTime;

        // Events
        public event Action<Violation> OnViolationDetected;
        public event Action<ulong, ViolationType> OnPlayerFlagged;
        public event Action<ulong> OnPlayerKicked;
        public event Action<ulong> OnPlayerBanned;

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

            if (!IsServer)
            {
                enabled = false;
                return;
            }
        }

        private void Update()
        {
            if (!IsServer || !enableAntiCheat) return;

            if (Time.time - lastDetectionTime >= detectionInterval)
            {
                RunDetectionCycle();
                lastDetectionTime = Time.time;
            }
        }

        #region Detection Cycle

        private void RunDetectionCycle()
        {
            foreach (var kvp in monitoredPlayers)
            {
                var playerId = kvp.Key;
                var data = kvp.Value;

                if (detectSpeedHacks) CheckSpeedHack(playerId, data);
                if (detectTeleports) CheckTeleport(playerId, data);
                if (validateMovement) ValidatePlayerMovement(playerId, data);
                if (validateClientTimestamps) ValidateTimestamps(playerId, data);
                if (enableBehaviorAnalysis) AnalyzeBehavior(playerId);
            }
        }

        #endregion

        #region Player Monitoring

        public void RegisterPlayer(ulong playerId)
        {
            if (!monitoredPlayers.ContainsKey(playerId))
            {
                monitoredPlayers[playerId] = new PlayerMonitoringData
                {
                    playerId = playerId,
                    lastUpdateTime = Time.time,
                    lastPosition = Vector3.zero
                };

                playerStatistics[playerId] = new BehaviorStatistics();
                consecutiveSpeedViolations[playerId] = 0;
                consecutiveHeadshots[playerId] = 0;

                Debug.Log($"AntiCheat: Registered player {playerId}");
            }
        }

        public void UnregisterPlayer(ulong playerId)
        {
            monitoredPlayers.Remove(playerId);
            playerStatistics.Remove(playerId);
            consecutiveSpeedViolations.Remove(playerId);
            consecutiveHeadshots.Remove(playerId);
            lastShotTimes.Remove(playerId);
        }

        public void UpdatePlayerPosition(ulong playerId, Vector3 position, Vector3 velocity, Quaternion rotation)
        {
            if (!monitoredPlayers.ContainsKey(playerId))
            {
                RegisterPlayer(playerId);
            }

            var data = monitoredPlayers[playerId];
            data.lastPosition = position;
            data.lastVelocity = velocity;
            data.lastRotation = rotation;
            data.lastUpdateTime = Time.time;

            // Update position history
            data.positionHistory.Enqueue(position);
            if (data.positionHistory.Count > 60) // Keep last 60 positions
            {
                data.positionHistory.Dequeue();
            }

            // Update velocity history
            data.velocityHistory.Enqueue(velocity.magnitude);
            if (data.velocityHistory.Count > 30)
            {
                data.velocityHistory.Dequeue();
            }
        }

        #endregion

        #region Speed Hack Detection

        private void CheckSpeedHack(ulong playerId, PlayerMonitoringData data)
        {
            if (data.velocityHistory.Count < 5) return;

            float avgSpeed = data.velocityHistory.Average();

            if (avgSpeed > maxAllowedSpeed)
            {
                consecutiveSpeedViolations[playerId]++;

                if (consecutiveSpeedViolations[playerId] >= consecutiveSpeedViolations)
                {
                    ReportViolation(playerId, ViolationType.SpeedHack, DetectionSeverity.High,
                        $"Player moving at {avgSpeed:F2} m/s (max: {maxAllowedSpeed} m/s)",
                        new Dictionary<string, object>
                        {
                            { "averageSpeed", avgSpeed },
                            { "maxAllowed", maxAllowedSpeed },
                            { "consecutiveViolations", consecutiveSpeedViolations[playerId] }
                        });

                    consecutiveSpeedViolations[playerId] = 0;
                }
            }
            else
            {
                consecutiveSpeedViolations[playerId] = Mathf.Max(0, consecutiveSpeedViolations[playerId] - 1);
            }
        }

        #endregion

        #region Teleport Detection

        private void CheckTeleport(ulong playerId, PlayerMonitoringData data)
        {
            if (data.positionHistory.Count < 2) return;

            var positions = data.positionHistory.ToArray();
            Vector3 currentPos = positions[positions.Length - 1];
            Vector3 previousPos = positions[positions.Length - 2];

            float distance = Vector3.Distance(currentPos, previousPos);
            float timeElapsed = teleportCheckInterval;
            float maxPossibleDistance = maxAllowedSpeed * timeElapsed;

            if (distance > maxAllowedDistance && distance > maxPossibleDistance * 2)
            {
                ReportViolation(playerId, ViolationType.Teleport, DetectionSeverity.Critical,
                    $"Instant movement of {distance:F2}m detected",
                    new Dictionary<string, object>
                    {
                        { "distance", distance },
                        { "maxAllowed", maxAllowedDistance },
                        { "previousPosition", previousPos },
                        { "currentPosition", currentPos }
                    });
            }
        }

        #endregion

        #region Aimbot Detection

        public void RecordShot(ulong playerId, Vector3 origin, Vector3 direction, ulong targetId, bool isHeadshot, float distance)
        {
            if (!detectAimbots) return;

            if (!monitoredPlayers.ContainsKey(playerId))
            {
                RegisterPlayer(playerId);
            }

            var data = monitoredPlayers[playerId];
            var stats = playerStatistics[playerId];

            // Calculate aim speed
            float aimSpeed = 0f;
            if (data.shotHistory.Count > 0)
            {
                var lastShot = data.shotHistory.Last();
                float angle = Vector3.Angle(lastShot.direction, direction);
                float timeDelta = Time.time - lastShot.timestamp;
                aimSpeed = angle / timeDelta;
            }

            var shotData = new ShotData
            {
                timestamp = Time.time,
                origin = origin,
                direction = direction,
                isHeadshot = isHeadshot,
                distance = distance,
                aimSpeed = aimSpeed,
                targetId = targetId
            };

            data.shotHistory.Enqueue(shotData);
            if (data.shotHistory.Count > 50)
            {
                data.shotHistory.Dequeue();
            }

            // Update statistics
            stats.totalShots++;
            if (isHeadshot)
            {
                stats.totalHeadshots++;
                consecutiveHeadshots[playerId]++;
            }
            else
            {
                consecutiveHeadshots[playerId] = 0;
            }

            // Check for suspicious aim speed
            if (aimSpeed > suspiciousAimSpeed && data.shotHistory.Count > 3)
            {
                ReportViolation(playerId, ViolationType.Aimbot, DetectionSeverity.High,
                    $"Suspicious aim speed: {aimSpeed:F2} deg/s",
                    new Dictionary<string, object>
                    {
                        { "aimSpeed", aimSpeed },
                        { "threshold", suspiciousAimSpeed }
                    });
            }

            // Check for consecutive headshots
            if (consecutiveHeadshots[playerId] >= consecutiveHeadshotThreshold)
            {
                ReportViolation(playerId, ViolationType.Aimbot, DetectionSeverity.High,
                    $"{consecutiveHeadshots[playerId]} consecutive headshots",
                    new Dictionary<string, object>
                    {
                        { "consecutiveHeadshots", consecutiveHeadshots[playerId] },
                        { "threshold", consecutiveHeadshotThreshold }
                    });
            }

            // Check headshot rate
            if (stats.totalShots > 20)
            {
                float headshotRate = (float)stats.totalHeadshots / stats.totalShots;
                if (headshotRate > perfectAimThreshold)
                {
                    ReportViolation(playerId, ViolationType.Aimbot, DetectionSeverity.Medium,
                        $"Abnormally high headshot rate: {headshotRate * 100:F1}%",
                        new Dictionary<string, object>
                        {
                            { "headshotRate", headshotRate },
                            { "totalShots", stats.totalShots },
                            { "totalHeadshots", stats.totalHeadshots }
                        });
                }
            }
        }

        #endregion

        #region Wallhack Detection

        public void ValidateShot(ulong playerId, Vector3 origin, Vector3 hitPoint, bool hadLineOfSight)
        {
            if (!detectWallhacks) return;

            if (!monitoredPlayers.ContainsKey(playerId))
            {
                RegisterPlayer(playerId);
            }

            var data = monitoredPlayers[playerId];

            if (!hadLineOfSight)
            {
                // Player shot through a wall without proper line of sight
                int impossibleShots = data.violationCounts.ContainsKey(ViolationType.Wallhack)
                    ? data.violationCounts[ViolationType.Wallhack]
                    : 0;

                impossibleShots++;

                if (impossibleShots >= impossibleShotsThreshold)
                {
                    ReportViolation(playerId, ViolationType.Wallhack, DetectionSeverity.High,
                        $"{impossibleShots} shots without line of sight",
                        new Dictionary<string, object>
                        {
                            { "impossibleShots", impossibleShots },
                            { "origin", origin },
                            { "hitPoint", hitPoint }
                        });
                }
            }
        }

        #endregion

        #region Rate of Fire Detection

        public void ValidateFireRate(ulong playerId, float actualRoF, float expectedRoF)
        {
            if (!detectRoFManipulation) return;

            float deviation = actualRoF / expectedRoF;

            if (deviation > maxRoFDeviation)
            {
                ReportViolation(playerId, ViolationType.RoFManipulation, DetectionSeverity.Medium,
                    $"Rate of fire {deviation * 100:F1}% of expected",
                    new Dictionary<string, object>
                    {
                        { "actualRoF", actualRoF },
                        { "expectedRoF", expectedRoF },
                        { "deviation", deviation }
                    });
            }
        }

        #endregion

        #region Movement Validation

        private void ValidatePlayerMovement(ulong playerId, PlayerMonitoringData data)
        {
            if (data.positionHistory.Count < 3) return;

            var positions = data.positionHistory.ToArray();
            var current = positions[positions.Length - 1];
            var previous = positions[positions.Length - 2];

            // Check vertical speed (flying/noclip)
            float verticalSpeed = Mathf.Abs(current.y - previous.y) / detectionInterval;

            if (verticalSpeed > maxVerticalSpeed)
            {
                ReportViolation(playerId, ViolationType.FlyHack, DetectionSeverity.High,
                    $"Abnormal vertical speed: {verticalSpeed:F2} m/s",
                    new Dictionary<string, object>
                    {
                        { "verticalSpeed", verticalSpeed },
                        { "maxAllowed", maxVerticalSpeed }
                    });
            }

            // Check for physics violations
            if (validatePhysics)
            {
                ValidatePhysics(playerId, data);
            }
        }

        private void ValidatePhysics(ulong playerId, PlayerMonitoringData data)
        {
            // Check if player is passing through solid objects
            if (data.positionHistory.Count < 2) return;

            var positions = data.positionHistory.ToArray();
            var current = positions[positions.Length - 1];
            var previous = positions[positions.Length - 2];

            // Raycast between positions to check for obstacles
            RaycastHit hit;
            if (Physics.Linecast(previous, current, out hit))
            {
                // Player moved through a solid object
                ReportViolation(playerId, ViolationType.NoClip, DetectionSeverity.Critical,
                    "Player moving through solid objects",
                    new Dictionary<string, object>
                    {
                        { "previousPosition", previous },
                        { "currentPosition", current },
                        { "collisionPoint", hit.point }
                    });
            }
        }

        #endregion

        #region Timestamp Validation

        private void ValidateTimestamps(ulong playerId, PlayerMonitoringData data)
        {
            if (data.timestampHistory.Count < 5) return;

            var timestamps = data.timestampHistory.ToArray();
            float serverTime = Time.time;

            // Check for timestamp manipulation
            for (int i = 0; i < timestamps.Length - 1; i++)
            {
                float delta = timestamps[i + 1] - timestamps[i];
                if (delta < 0 || delta > maxTimestampDeviation)
                {
                    ReportViolation(playerId, ViolationType.TimestampManipulation, DetectionSeverity.Medium,
                        $"Invalid timestamp delta: {delta:F3}s",
                        new Dictionary<string, object>
                        {
                            { "delta", delta },
                            { "maxDeviation", maxTimestampDeviation }
                        });
                    break;
                }
            }
        }

        public void RecordClientTimestamp(ulong playerId, float clientTime)
        {
            if (!monitoredPlayers.ContainsKey(playerId))
            {
                RegisterPlayer(playerId);
            }

            var data = monitoredPlayers[playerId];
            data.timestampHistory.Enqueue(clientTime);

            if (data.timestampHistory.Count > 10)
            {
                data.timestampHistory.Dequeue();
            }
        }

        #endregion

        #region Behavior Analysis

        private void AnalyzeBehavior(ulong playerId)
        {
            if (!playerStatistics.ContainsKey(playerId)) return;

            var stats = playerStatistics[playerId];

            if (stats.totalShots < statisticalSampleSize) return;

            // Calculate statistical anomalies
            float headshotRate = (float)stats.totalHeadshots / stats.totalShots;
            float avgHeadshotRate = CalculateAverageHeadshotRate();
            float stdDev = CalculateHeadshotRateStdDev();

            float zScore = (headshotRate - avgHeadshotRate) / stdDev;

            if (Mathf.Abs(zScore) > behaviorAnomalyThreshold)
            {
                ReportViolation(playerId, ViolationType.StatisticalAnomaly, DetectionSeverity.Medium,
                    $"Statistical anomaly detected (z-score: {zScore:F2})",
                    new Dictionary<string, object>
                    {
                        { "zScore", zScore },
                        { "headshotRate", headshotRate },
                        { "avgHeadshotRate", avgHeadshotRate },
                        { "threshold", behaviorAnomalyThreshold }
                    });
            }
        }

        private float CalculateAverageHeadshotRate()
        {
            if (playerStatistics.Count == 0) return 0f;

            float totalRate = 0f;
            int validPlayers = 0;

            foreach (var stats in playerStatistics.Values)
            {
                if (stats.totalShots >= 20)
                {
                    totalRate += (float)stats.totalHeadshots / stats.totalShots;
                    validPlayers++;
                }
            }

            return validPlayers > 0 ? totalRate / validPlayers : 0.25f;
        }

        private float CalculateHeadshotRateStdDev()
        {
            float avg = CalculateAverageHeadshotRate();
            float sumSquaredDiff = 0f;
            int validPlayers = 0;

            foreach (var stats in playerStatistics.Values)
            {
                if (stats.totalShots >= 20)
                {
                    float rate = (float)stats.totalHeadshots / stats.totalShots;
                    sumSquaredDiff += Mathf.Pow(rate - avg, 2);
                    validPlayers++;
                }
            }

            return validPlayers > 1 ? Mathf.Sqrt(sumSquaredDiff / validPlayers) : 0.1f;
        }

        #endregion

        #region Violation Handling

        private void ReportViolation(ulong playerId, ViolationType type, DetectionSeverity severity, string description, Dictionary<string, object> evidence)
        {
            var violation = new Violation
            {
                violationId = $"violation_{Guid.NewGuid()}",
                playerId = playerId,
                type = type,
                severity = severity,
                timestamp = DateTime.UtcNow,
                description = description,
                evidence = evidence
            };

            violations.Add(violation);

            if (!monitoredPlayers.ContainsKey(playerId)) return;

            var data = monitoredPlayers[playerId];
            data.totalViolations++;

            if (!data.violationCounts.ContainsKey(type))
            {
                data.violationCounts[type] = 0;
            }
            data.violationCounts[type]++;

            OnViolationDetected?.Invoke(violation);

            Debug.LogWarning($"AntiCheat: Violation detected - Player {playerId}, Type: {type}, Severity: {severity}, Description: {description}");

            // Flag player if not already flagged
            if (!data.isFlagged)
            {
                data.isFlagged = true;
                data.flaggedTime = Time.time;
                OnPlayerFlagged?.Invoke(playerId, type);
            }

            // Auto-kick if threshold reached
            if (autoKickOnDetection && data.totalViolations >= violationThreshold)
            {
                KickPlayer(playerId, $"Anti-cheat violation: {type}");
                violation.resultedInKick = true;
            }

            // Auto-ban for critical violations
            if (severity == DetectionSeverity.Critical)
            {
                BanPlayer(playerId, $"Critical anti-cheat violation: {type}");
            }

            violation.processed = true;
        }

        private void KickPlayer(ulong playerId, string reason)
        {
            Debug.LogWarning($"AntiCheat: Kicking player {playerId} - {reason}");
            OnPlayerKicked?.Invoke(playerId);

            // Integration with NetworkManager to disconnect player
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.DisconnectClient(playerId);
            }
        }

        private void BanPlayer(ulong playerId, string reason)
        {
            Debug.LogWarning($"AntiCheat: Banning player {playerId} - {reason}");
            OnPlayerBanned?.Invoke(playerId);

            // Integration with ban system (ReportSystem)
            if (Moderation.ReportSystem.Instance != null)
            {
                Moderation.ReportSystem.Instance.BanPlayer(playerId, 0, reason,
                    Moderation.ReportSystem.BanType.Permanent);
            }

            KickPlayer(playerId, reason);
        }

        #endregion

        #region Public API

        public bool IsPlayerFlagged(ulong playerId)
        {
            return monitoredPlayers.ContainsKey(playerId) && monitoredPlayers[playerId].isFlagged;
        }

        public int GetViolationCount(ulong playerId, ViolationType? type = null)
        {
            if (!monitoredPlayers.ContainsKey(playerId)) return 0;

            if (type.HasValue)
            {
                return monitoredPlayers[playerId].violationCounts.ContainsKey(type.Value)
                    ? monitoredPlayers[playerId].violationCounts[type.Value]
                    : 0;
            }

            return monitoredPlayers[playerId].totalViolations;
        }

        public List<Violation> GetViolations(ulong playerId)
        {
            return violations.Where(v => v.playerId == playerId).ToList();
        }

        public List<Violation> GetAllViolations()
        {
            return new List<Violation>(violations);
        }

        public BehaviorStatistics GetPlayerStatistics(ulong playerId)
        {
            return playerStatistics.ContainsKey(playerId) ? playerStatistics[playerId] : null;
        }

        public void ClearViolations(ulong playerId)
        {
            if (!monitoredPlayers.ContainsKey(playerId)) return;

            var data = monitoredPlayers[playerId];
            data.totalViolations = 0;
            data.violationCounts.Clear();
            data.isFlagged = false;

            violations.RemoveAll(v => v.playerId == playerId);
        }

        public void WhitelistPlayer(ulong playerId)
        {
            // Remove from monitoring temporarily
            if (monitoredPlayers.ContainsKey(playerId))
            {
                ClearViolations(playerId);
            }
        }

        #endregion

        #region Cleanup

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            monitoredPlayers.Clear();
            playerStatistics.Clear();
            violations.Clear();
        }

        #endregion
    }
}
