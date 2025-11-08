using UnityEngine;
using UnityEngine.Profiling;
using System.Collections.Generic;

namespace DeadFrontier.Core.Performance
{
    /// <summary>
    /// Monitors game performance and provides optimization recommendations
    /// Ensures game runs smoothly on all platforms
    /// </summary>
    public class PerformanceMonitor : Singleton<PerformanceMonitor>
    {
        [Header("Monitoring Settings")]
        [SerializeField] private bool enableMonitoring = true;
        [SerializeField] private float updateInterval = 1f;
        [SerializeField] private bool showDebugOverlay = false;

        [Header("Performance Targets")]
        [SerializeField] private int targetFPS = 60;
        [SerializeField] private float targetFrameTime = 16.67f; // ms
        [SerializeField] private long maxMemoryMB = 512;

        [Header("Warning Thresholds")]
        [SerializeField] private int lowFPSThreshold = 30;
        [SerializeField] private long highMemoryThresholdMB = 400;

        // Performance metrics
        private float currentFPS;
        private float averageFPS;
        private float minFPS;
        private float maxFPS;
        private float currentFrameTime;
        private long currentMemoryMB;
        private long peakMemoryMB;

        // Tracking
        private float fpsUpdateTimer;
        private List<float> fpsHistory = new List<float>();
        private const int FPS_HISTORY_SIZE = 60;

        // Entity tracking
        private int zombieCount;
        private int playerCount;
        private int networkObjectCount;

        // Optimization state
        private PerformanceLevel currentPerformanceLevel = PerformanceLevel.High;
        private bool isOptimizing = false;

        // Events
        public event System.Action<PerformanceMetrics> OnPerformanceUpdated;
        public event System.Action<PerformanceWarning> OnPerformanceWarning;

        // Properties
        public float CurrentFPS => currentFPS;
        public float AverageFPS => averageFPS;
        public long CurrentMemoryMB => currentMemoryMB;
        public PerformanceLevel CurrentLevel => currentPerformanceLevel;

        protected override void Awake()
        {
            base.Awake();

            // Initialize FPS history
            for (int i = 0; i < FPS_HISTORY_SIZE; i++)
            {
                fpsHistory.Add(60f);
            }
        }

        private void Update()
        {
            if (!enableMonitoring)
                return;

            // Update FPS
            UpdateFPSMetrics();

            // Update periodically
            fpsUpdateTimer += Time.unscaledDeltaTime;
            if (fpsUpdateTimer >= updateInterval)
            {
                UpdateAllMetrics();
                fpsUpdateTimer = 0f;
            }
        }

        #region Metrics Update

        private void UpdateFPSMetrics()
        {
            // Calculate current FPS
            currentFPS = 1f / Time.unscaledDeltaTime;
            currentFrameTime = Time.unscaledDeltaTime * 1000f;

            // Update FPS history
            fpsHistory.Add(currentFPS);
            if (fpsHistory.Count > FPS_HISTORY_SIZE)
            {
                fpsHistory.RemoveAt(0);
            }

            // Calculate average FPS
            float sum = 0f;
            foreach (var fps in fpsHistory)
            {
                sum += fps;
            }
            averageFPS = sum / fpsHistory.Count;

            // Track min/max
            minFPS = Mathf.Min(minFPS == 0 ? currentFPS : minFPS, currentFPS);
            maxFPS = Mathf.Max(maxFPS, currentFPS);
        }

        private void UpdateAllMetrics()
        {
            // Memory metrics
            currentMemoryMB = Profiler.GetTotalAllocatedMemoryLong() / 1048576; // Bytes to MB
            peakMemoryMB = Mathf.Max(peakMemoryMB, currentMemoryMB);

            // Entity counts
            zombieCount = Zombies.ZombieManager.Instance?.GetActiveZombieCount() ?? 0;
            playerCount = FindObjectsOfType<Player.PlayerController>().Length;

            if (Unity.Netcode.NetworkManager.Singleton != null)
            {
                networkObjectCount = Unity.Netcode.NetworkManager.Singleton.SpawnManager.SpawnedObjects.Count;
            }

            // Check for performance issues
            CheckPerformanceWarnings();

            // Auto-optimize if needed
            if (averageFPS < lowFPSThreshold && !isOptimizing)
            {
                AutoOptimize();
            }

            // Notify listeners
            OnPerformanceUpdated?.Invoke(GetCurrentMetrics());

            // Track analytics (periodically)
            if (UnityEngine.Random.value < 0.1f) // 10% chance to avoid spam
            {
                Analytics.AnalyticsManager.Instance?.TrackPerformance(
                    averageFPS,
                    GetPing(),
                    zombieCount,
                    playerCount
                );
            }
        }

        private void CheckPerformanceWarnings()
        {
            // Low FPS warning
            if (averageFPS < lowFPSThreshold)
            {
                OnPerformanceWarning?.Invoke(new PerformanceWarning
                {
                    type = WarningType.LowFPS,
                    severity = Severity.High,
                    message = $"Low FPS detected: {averageFPS:F1} FPS (target: {targetFPS})",
                    recommendation = "Consider lowering graphics settings or reducing entity count"
                });
            }

            // High memory warning
            if (currentMemoryMB > highMemoryThresholdMB)
            {
                OnPerformanceWarning?.Invoke(new PerformanceWarning
                {
                    type = WarningType.HighMemory,
                    severity = Severity.Medium,
                    message = $"High memory usage: {currentMemoryMB} MB (threshold: {highMemoryThresholdMB} MB)",
                    recommendation = "Memory cleanup recommended"
                });
            }

            // High entity count
            if (zombieCount > Constants.MAX_ZOMBIES_PER_MATCH * 0.8f)
            {
                OnPerformanceWarning?.Invoke(new PerformanceWarning
                {
                    type = WarningType.HighEntityCount,
                    severity = Severity.Low,
                    message = $"High zombie count: {zombieCount}",
                    recommendation = "Consider culling distant zombies"
                });
            }
        }

        #endregion

        #region Auto-Optimization

        /// <summary>
        /// Automatically optimizes performance based on current metrics
        /// </summary>
        private void AutoOptimize()
        {
            isOptimizing = true;

            Debug.LogWarning($"[PerformanceMonitor] Auto-optimization triggered (FPS: {averageFPS:F1})");

            // Determine target performance level
            PerformanceLevel targetLevel = DetermineOptimalPerformanceLevel();

            if (targetLevel != currentPerformanceLevel)
            {
                ApplyPerformanceLevel(targetLevel);
            }

            // Specific optimizations
            OptimizeMemory();
            OptimizeRendering();
            OptimizePhysics();

            isOptimizing = false;
        }

        private PerformanceLevel DetermineOptimalPerformanceLevel()
        {
            if (averageFPS >= 50)
                return PerformanceLevel.High;
            else if (averageFPS >= 35)
                return PerformanceLevel.Medium;
            else
                return PerformanceLevel.Low;
        }

        /// <summary>
        /// Applies a performance level preset
        /// </summary>
        public void ApplyPerformanceLevel(PerformanceLevel level)
        {
            currentPerformanceLevel = level;

            Debug.Log($"[PerformanceMonitor] Applying performance level: {level}");

            switch (level)
            {
                case PerformanceLevel.Low:
                    QualitySettings.SetQualityLevel(0); // Low
                    Application.targetFrameRate = 30;
                    break;

                case PerformanceLevel.Medium:
                    QualitySettings.SetQualityLevel(1); // Medium
                    Application.targetFrameRate = 45;
                    break;

                case PerformanceLevel.High:
                    QualitySettings.SetQualityLevel(2); // High
                    Application.targetFrameRate = 60;
                    break;

                case PerformanceLevel.Ultra:
                    QualitySettings.SetQualityLevel(3); // Ultra
                    Application.targetFrameRate = -1; // Unlimited
                    break;
            }

            // Notify settings manager
            Settings.SettingsManager.Instance?.SetQualityLevel((int)level);
        }

        private void OptimizeMemory()
        {
            // Force garbage collection if memory is high
            if (currentMemoryMB > highMemoryThresholdMB)
            {
                Debug.Log("[PerformanceMonitor] Forcing garbage collection...");
                System.GC.Collect();
                Resources.UnloadUnusedAssets();
            }
        }

        private void OptimizeRendering()
        {
            // Adjust shadow distance based on performance
            if (averageFPS < 40)
            {
                QualitySettings.shadowDistance = 30f;
            }
            else if (averageFPS < 50)
            {
                QualitySettings.shadowDistance = 50f;
            }
        }

        private void OptimizePhysics()
        {
            // Adjust physics update rate based on performance
            if (averageFPS < 35)
            {
                Time.fixedDeltaTime = 0.03f; // 33 Hz
            }
            else
            {
                Time.fixedDeltaTime = 0.02f; // 50 Hz
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Gets current performance metrics
        /// </summary>
        public PerformanceMetrics GetCurrentMetrics()
        {
            return new PerformanceMetrics
            {
                currentFPS = currentFPS,
                averageFPS = averageFPS,
                minFPS = minFPS,
                maxFPS = maxFPS,
                frameTimeMS = currentFrameTime,
                memoryMB = currentMemoryMB,
                peakMemoryMB = peakMemoryMB,
                zombieCount = zombieCount,
                playerCount = playerCount,
                networkObjectCount = networkObjectCount,
                performanceLevel = currentPerformanceLevel
            };
        }

        /// <summary>
        /// Gets network ping (if available)
        /// </summary>
        private float GetPing()
        {
            // TODO: Get actual network ping
            return 0f;
        }

        /// <summary>
        /// Resets performance statistics
        /// </summary>
        public void ResetStatistics()
        {
            minFPS = 0;
            maxFPS = 0;
            peakMemoryMB = 0;
            fpsHistory.Clear();

            for (int i = 0; i < FPS_HISTORY_SIZE; i++)
            {
                fpsHistory.Add(60f);
            }

            Debug.Log("[PerformanceMonitor] Statistics reset");
        }

        /// <summary>
        /// Gets performance report for debugging
        /// </summary>
        public string GetPerformanceReport()
        {
            var metrics = GetCurrentMetrics();

            return $@"=== PERFORMANCE REPORT ===
FPS: {metrics.currentFPS:F1} (Avg: {metrics.averageFPS:F1}, Min: {metrics.minFPS:F1}, Max: {metrics.maxFPS:F1})
Frame Time: {metrics.frameTimeMS:F2} ms
Memory: {metrics.memoryMB} MB (Peak: {metrics.peakMemoryMB} MB)
Entities: {metrics.zombieCount} zombies, {metrics.playerCount} players
Network Objects: {metrics.networkObjectCount}
Performance Level: {metrics.performanceLevel}
========================";
        }

        #endregion

        #region Debug Overlay

        private void OnGUI()
        {
            if (!showDebugOverlay)
                return;

            // Simple debug overlay
            GUIStyle style = new GUIStyle();
            style.fontSize = 14;
            style.normal.textColor = Color.white;
            style.alignment = TextAnchor.UpperLeft;

            GUI.Label(new Rect(10, 10, 300, 200), GetPerformanceReport(), style);

            // FPS color indicator
            Color fpsColor = averageFPS >= 50 ? Color.green : averageFPS >= 30 ? Color.yellow : Color.red;
            GUI.color = fpsColor;
            GUI.Label(new Rect(10, 10, 100, 30), $"FPS: {currentFPS:F1}", style);
            GUI.color = Color.white;
        }

        #endregion
    }

    #region Data Structures

    public struct PerformanceMetrics
    {
        public float currentFPS;
        public float averageFPS;
        public float minFPS;
        public float maxFPS;
        public float frameTimeMS;
        public long memoryMB;
        public long peakMemoryMB;
        public int zombieCount;
        public int playerCount;
        public int networkObjectCount;
        public PerformanceLevel performanceLevel;
    }

    public struct PerformanceWarning
    {
        public WarningType type;
        public Severity severity;
        public string message;
        public string recommendation;
    }

    public enum PerformanceLevel
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Ultra = 3
    }

    public enum WarningType
    {
        LowFPS,
        HighMemory,
        HighEntityCount,
        NetworkLatency,
        MemoryLeak
    }

    public enum Severity
    {
        Low,
        Medium,
        High,
        Critical
    }

    #endregion
}
