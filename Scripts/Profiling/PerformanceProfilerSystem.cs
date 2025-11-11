using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace ZombieGame
{
    /// <summary>
    /// Performance Profiler System - Real-time performance monitoring and optimization
    /// Features: FPS tracking, memory profiling, network stats, bottleneck detection
    /// Helps maintain "well working game" priority with optimization insights
    /// </summary>
    public class PerformanceProfilerSystem : MonoBehaviour
    {
        public static PerformanceProfilerSystem Instance { get; private set; }

        [Header("Profiler Settings")]
        [SerializeField] private bool enableProfiling = true;
        [SerializeField] private bool showOnScreenStats = false;
        [SerializeField] private float updateInterval = 1.0f;
        [SerializeField] private int historySize = 300; // 5 minutes at 1 update/second

        // Performance Metrics
        private PerformanceMetrics currentMetrics;
        private Queue<PerformanceMetrics> metricsHistory = new Queue<PerformanceMetrics>();

        // FPS Tracking
        private float fpsTimer = 0f;
        private int frameCount = 0;
        private List<float> fpsHistory = new List<float>();

        // Memory Tracking
        private long lastTotalMemory = 0;
        private long peakMemoryUsage = 0;

        // Network Tracking
        private long lastBytesReceived = 0;
        private long lastBytesSent = 0;

        // Events
        public event System.Action<PerformanceMetrics> OnMetricsUpdated;
        public event System.Action<PerformanceWarning> OnPerformanceWarning;

        [System.Serializable]
        public class PerformanceMetrics
        {
            public float timestamp;
            public FPSMetrics fps;
            public MemoryMetrics memory;
            public NetworkMetrics network;
            public RenderMetrics rendering;
            public CPUMetrics cpu;
        }

        [System.Serializable]
        public class FPSMetrics
        {
            public float currentFPS;
            public float averageFPS;
            public float minFPS;
            public float maxFPS;
            public float frameTime; // milliseconds
        }

        [System.Serializable]
        public class MemoryMetrics
        {
            public long totalAllocated; // bytes
            public long totalReserved;  // bytes
            public long monoUsed;       // bytes
            public long monoHeap;       // bytes
            public float gcAllocRate;   // MB/s
        }

        [System.Serializable]
        public class NetworkMetrics
        {
            public long bytesReceived;
            public long bytesSent;
            public float downloadSpeed; // KB/s
            public float uploadSpeed;   // KB/s
            public int ping;            // milliseconds
            public int packetLoss;      // percentage
        }

        [System.Serializable]
        public class RenderMetrics
        {
            public int drawCalls;
            public int batches;
            public int triangles;
            public int vertices;
            public float renderTime; // milliseconds
        }

        [System.Serializable]
        public class CPUMetrics
        {
            public float cpuUsage;      // percentage
            public float cpuTime;       // milliseconds
            public int activeThreads;
        }

        [System.Serializable]
        public class PerformanceWarning
        {
            public WarningType type;
            public string message;
            public float severity; // 0-1
            public float timestamp;
        }

        public enum WarningType
        {
            LowFPS,
            HighMemory,
            MemoryLeak,
            HighNetworkLatency,
            PacketLoss,
            HighDrawCalls,
            LongFrameTime
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeProfiler();
            }
            else { Destroy(gameObject); }
        }

        private void InitializeProfiler()
        {
            currentMetrics = new PerformanceMetrics
            {
                fps = new FPSMetrics(),
                memory = new MemoryMetrics(),
                network = new NetworkMetrics(),
                rendering = new RenderMetrics(),
                cpu = new CPUMetrics()
            };

            Debug.Log("[Profiler] Performance profiler initialized");
        }

        private void Update()
        {
            if (!enableProfiling) return;

            // Track FPS
            frameCount++;
            fpsTimer += Time.unscaledDeltaTime;

            if (fpsTimer >= updateInterval)
            {
                UpdateMetrics();
                fpsTimer = 0f;
                frameCount = 0;
            }
        }

        private void UpdateMetrics()
        {
            currentMetrics.timestamp = Time.time;

            // Update FPS
            UpdateFPSMetrics();

            // Update Memory
            UpdateMemoryMetrics();

            // Update Network
            UpdateNetworkMetrics();

            // Update Rendering
            UpdateRenderingMetrics();

            // Update CPU
            UpdateCPUMetrics();

            // Store in history
            metricsHistory.Enqueue(currentMetrics);
            if (metricsHistory.Count > historySize)
            {
                metricsHistory.Dequeue();
            }

            // Check for warnings
            CheckPerformanceWarnings();

            // Notify listeners
            OnMetricsUpdated?.Invoke(currentMetrics);
        }

        private void UpdateFPSMetrics()
        {
            float fps = frameCount / updateInterval;
            currentMetrics.fps.currentFPS = fps;
            currentMetrics.fps.frameTime = (1000f / fps);

            fpsHistory.Add(fps);
            if (fpsHistory.Count > historySize)
            {
                fpsHistory.RemoveAt(0);
            }

            if (fpsHistory.Count > 0)
            {
                currentMetrics.fps.averageFPS = fpsHistory.Average();
                currentMetrics.fps.minFPS = fpsHistory.Min();
                currentMetrics.fps.maxFPS = fpsHistory.Max();
            }
        }

        private void UpdateMemoryMetrics()
        {
            currentMetrics.memory.totalAllocated = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
            currentMetrics.memory.totalReserved = UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong();
            currentMetrics.memory.monoUsed = UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong();
            currentMetrics.memory.monoHeap = UnityEngine.Profiling.Profiler.GetMonoHeapSizeLong();

            // Calculate GC alloc rate
            long currentTotal = currentMetrics.memory.totalAllocated;
            if (lastTotalMemory > 0)
            {
                long allocated = currentTotal - lastTotalMemory;
                currentMetrics.memory.gcAllocRate = (allocated / (1024f * 1024f)) / updateInterval;
            }
            lastTotalMemory = currentTotal;

            // Track peak memory
            if (currentTotal > peakMemoryUsage)
            {
                peakMemoryUsage = currentTotal;
            }
        }

        private void UpdateNetworkMetrics()
        {
            // Would get from NetworkManager
            // Placeholder values
            currentMetrics.network.bytesReceived = 0;
            currentMetrics.network.bytesSent = 0;
            currentMetrics.network.ping = 0;
            currentMetrics.network.packetLoss = 0;

            // Calculate speeds
            if (lastBytesReceived > 0)
            {
                long received = currentMetrics.network.bytesReceived - lastBytesReceived;
                currentMetrics.network.downloadSpeed = (received / 1024f) / updateInterval;
            }

            if (lastBytesSent > 0)
            {
                long sent = currentMetrics.network.bytesSent - lastBytesSent;
                currentMetrics.network.uploadSpeed = (sent / 1024f) / updateInterval;
            }

            lastBytesReceived = currentMetrics.network.bytesReceived;
            lastBytesSent = currentMetrics.network.bytesSent;
        }

        private void UpdateRenderingMetrics()
        {
            // Would use Unity Profiler API or custom rendering stats
            currentMetrics.rendering.drawCalls = UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(null) > 0 ? 0 : 0;
            currentMetrics.rendering.batches = 0;
            currentMetrics.rendering.triangles = 0;
            currentMetrics.rendering.vertices = 0;
            currentMetrics.rendering.renderTime = 0;
        }

        private void UpdateCPUMetrics()
        {
            currentMetrics.cpu.cpuTime = currentMetrics.fps.frameTime;
            currentMetrics.cpu.activeThreads = System.Diagnostics.Process.GetCurrentProcess().Threads.Count;
            // CPU usage would require platform-specific code
            currentMetrics.cpu.cpuUsage = 0f;
        }

        private void CheckPerformanceWarnings()
        {
            // Low FPS Warning
            if (currentMetrics.fps.currentFPS < 30f)
            {
                IssueWarning(WarningType.LowFPS, 
                    $"Low FPS detected: {currentMetrics.fps.currentFPS:F1} FPS", 
                    1f - (currentMetrics.fps.currentFPS / 60f));
            }

            // High Memory Warning
            float memoryUsageGB = currentMetrics.memory.totalAllocated / (1024f * 1024f * 1024f);
            if (memoryUsageGB > 2f) // > 2GB
            {
                IssueWarning(WarningType.HighMemory,
                    $"High memory usage: {memoryUsageGB:F2} GB",
                    memoryUsageGB / 4f);
            }

            // Memory Leak Detection
            if (currentMetrics.memory.gcAllocRate > 10f) // > 10 MB/s
            {
                IssueWarning(WarningType.MemoryLeak,
                    $"Potential memory leak: {currentMetrics.memory.gcAllocRate:F2} MB/s allocation rate",
                    0.8f);
            }

            // Network Latency Warning
            if (currentMetrics.network.ping > 150)
            {
                IssueWarning(WarningType.HighNetworkLatency,
                    $"High network latency: {currentMetrics.network.ping} ms",
                    currentMetrics.network.ping / 500f);
            }

            // Long Frame Time
            if (currentMetrics.fps.frameTime > 33.3f) // > 30 FPS threshold
            {
                IssueWarning(WarningType.LongFrameTime,
                    $"Long frame time: {currentMetrics.fps.frameTime:F1} ms",
                    currentMetrics.fps.frameTime / 100f);
            }
        }

        private void IssueWarning(WarningType type, string message, float severity)
        {
            var warning = new PerformanceWarning
            {
                type = type,
                message = message,
                severity = Mathf.Clamp01(severity),
                timestamp = Time.time
            };

            OnPerformanceWarning?.Invoke(warning);
            Debug.LogWarning($"[Profiler] {message}");
        }

        // Public API

        public PerformanceMetrics GetCurrentMetrics()
        {
            return currentMetrics;
        }

        public List<PerformanceMetrics> GetMetricsHistory()
        {
            return new List<PerformanceMetrics>(metricsHistory);
        }

        public float GetAverageFPS()
        {
            return currentMetrics.fps.averageFPS;
        }

        public long GetCurrentMemoryUsage()
        {
            return currentMetrics.memory.totalAllocated;
        }

        public long GetPeakMemoryUsage()
        {
            return peakMemoryUsage;
        }

        public float GetAverageFrameTime()
        {
            if (fpsHistory.Count == 0) return 0f;
            return 1000f / fpsHistory.Average();
        }

        public void ResetPeakMemory()
        {
            peakMemoryUsage = currentMetrics.memory.totalAllocated;
        }

        public void ClearHistory()
        {
            metricsHistory.Clear();
            fpsHistory.Clear();
        }

        public string GetPerformanceSummary()
        {
            return $"FPS: {currentMetrics.fps.currentFPS:F1} | " +
                   $"Frame: {currentMetrics.fps.frameTime:F1}ms | " +
                   $"Memory: {(currentMetrics.memory.totalAllocated / (1024f * 1024f)):F0}MB | " +
                   $"Ping: {currentMetrics.network.ping}ms";
        }

        public PerformanceGrade GetPerformanceGrade()
        {
            float fps = currentMetrics.fps.currentFPS;

            if (fps >= 60f) return PerformanceGrade.Excellent;
            if (fps >= 45f) return PerformanceGrade.Good;
            if (fps >= 30f) return PerformanceGrade.Fair;
            if (fps >= 20f) return PerformanceGrade.Poor;
            return PerformanceGrade.Critical;
        }

        public enum PerformanceGrade
        {
            Excellent, // 60+ FPS
            Good,      // 45-60 FPS
            Fair,      // 30-45 FPS
            Poor,      // 20-30 FPS
            Critical   // < 20 FPS
        }

        private void OnGUI()
        {
            if (!showOnScreenStats || !enableProfiling) return;

            int w = Screen.width, h = Screen.height;
            GUIStyle style = new GUIStyle();
            style.alignment = TextAnchor.UpperLeft;
            style.fontSize = h * 2 / 50;
            style.normal.textColor = GetGradeColor();

            Rect rect = new Rect(10, 10, w, h * 2 / 50);
            string text = GetPerformanceSummary();
            GUI.Label(rect, text, style);
        }

        private Color GetGradeColor()
        {
            switch (GetPerformanceGrade())
            {
                case PerformanceGrade.Excellent: return Color.green;
                case PerformanceGrade.Good: return Color.cyan;
                case PerformanceGrade.Fair: return Color.yellow;
                case PerformanceGrade.Poor: return new Color(1f, 0.5f, 0f);
                case PerformanceGrade.Critical: return Color.red;
                default: return Color.white;
            }
        }

        public void ToggleOnScreenStats()
        {
            showOnScreenStats = !showOnScreenStats;
        }
    }
}
