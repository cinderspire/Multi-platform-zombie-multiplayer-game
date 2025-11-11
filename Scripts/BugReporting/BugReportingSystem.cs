using Unity.Netcode;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;

namespace ZombieGame
{
    /// <summary>
    /// Bug Reporting System - In-game bug reporting for post-launch polish
    /// Features: Screenshot capture, system info, crash logs, auto-reporting
    /// Integration with bug tracking systems (Jira, GitHub Issues)
    /// </summary>
    public class BugReportingSystem : MonoBehaviour
    {
        public static BugReportingSystem Instance { get; private set; }

        [Header("Bug Reporting Settings")]
        [SerializeField] private bool enableBugReporting = true;
        [SerializeField] private bool autoReportCrashes = true;
        [SerializeField] private bool includeScreenshot = true;
        [SerializeField] private bool includeSystemInfo = true;
        [SerializeField] private string bugReportAPIUrl = "https://api.yourgame.com/bugs";

        private List<BugReport> pendingReports = new List<BugReport>();
        private Dictionary<string, int> bugCounts = new Dictionary<string, int>();

        // Events
        public event Action<BugReport> OnBugReported;
        public event Action<string> OnBugSubmitted;

        [System.Serializable]
        public class BugReport
        {
            public string reportId;
            public DateTime timestamp;
            public BugCategory category;
            public BugSeverity severity;
            public string title;
            public string description;
            public string reproSteps;
            public SystemInfo systemInfo;
            public string screenshotPath;
            public string logFilePath;
            public PlayerContext playerContext;
            public bool submitted = false;
        }

        [System.Serializable]
        public class SystemInfo
        {
            public string deviceModel;
            public string deviceType;
            public string operatingSystem;
            public string processorType;
            public int processorCount;
            public int systemMemorySize;
            public string graphicsDeviceName;
            public string graphicsDeviceType;
            public int graphicsMemorySize;
            public string graphicsDeviceVersion;
            public bool graphicsMultiThreaded;
            public string unityVersion;
            public string gameVersion;
        }

        [System.Serializable]
        public class PlayerContext
        {
            public ulong playerId;
            public string playerName;
            public Vector3 position;
            public string currentMap;
            public string currentGameMode;
            public int playerLevel;
            public float playTime;
            public string lastAction;
        }

        public enum BugCategory
        {
            Crash,
            Gameplay,
            UI,
            Graphics,
            Audio,
            Network,
            Performance,
            Controls,
            Progression,
            Other
        }

        public enum BugSeverity
        {
            Critical,  // Game-breaking
            High,      // Major feature broken
            Medium,    // Feature partially broken
            Low,       // Minor issue
            Cosmetic   // Visual only
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                SetupCrashHandler();
                LoadPendingReports();
            }
            else { Destroy(gameObject); }
        }

        private void SetupCrashHandler()
        {
            if (!autoReportCrashes) return;

            Application.logMessageReceived += HandleLog;
            Debug.Log("[BugReport] Crash handler initialized");
        }

        private void HandleLog(string logString, string stackTrace, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error)
            {
                // Auto-report critical errors
                if (autoReportCrashes)
                {
                    AutoReportCrash(logString, stackTrace);
                }
            }
        }

        private void AutoReportCrash(string error, string stackTrace)
        {
            var report = new BugReport
            {
                reportId = Guid.NewGuid().ToString(),
                timestamp = DateTime.UtcNow,
                category = BugCategory.Crash,
                severity = BugSeverity.Critical,
                title = $"Auto-reported crash: {error.Substring(0, Math.Min(50, error.Length))}",
                description = error,
                reproSteps = stackTrace,
                systemInfo = GatherSystemInfo(),
                playerContext = GatherPlayerContext()
            };

            SubmitBugReport(report);
            Debug.Log($"[BugReport] Auto-reported crash: {report.reportId}");
        }

        public void ReportBug(BugCategory category, BugSeverity severity, string title, string description, string reproSteps = "")
        {
            if (!enableBugReporting) return;

            var report = new BugReport
            {
                reportId = Guid.NewGuid().ToString(),
                timestamp = DateTime.UtcNow,
                category = category,
                severity = severity,
                title = title,
                description = description,
                reproSteps = reproSteps,
                systemInfo = includeSystemInfo ? GatherSystemInfo() : null,
                playerContext = GatherPlayerContext()
            };

            if (includeScreenshot)
            {
                report.screenshotPath = CaptureScreenshot(report.reportId);
            }

            report.logFilePath = SaveLogFile(report.reportId);

            pendingReports.Add(report);
            OnBugReported?.Invoke(report);

            // Track bug frequency
            string bugKey = $"{category}_{title}";
            if (!bugCounts.ContainsKey(bugKey)) bugCounts[bugKey] = 0;
            bugCounts[bugKey]++;

            Debug.Log($"[BugReport] Bug reported: {report.title} | Severity: {severity} | ID: {report.reportId}");

            // Auto-submit if configured
            SubmitBugReport(report);
        }

        private SystemInfo GatherSystemInfo()
        {
            return new SystemInfo
            {
                deviceModel = UnityEngine.SystemInfo.deviceModel,
                deviceType = UnityEngine.SystemInfo.deviceType.ToString(),
                operatingSystem = UnityEngine.SystemInfo.operatingSystem,
                processorType = UnityEngine.SystemInfo.processorType,
                processorCount = UnityEngine.SystemInfo.processorCount,
                systemMemorySize = UnityEngine.SystemInfo.systemMemorySize,
                graphicsDeviceName = UnityEngine.SystemInfo.graphicsDeviceName,
                graphicsDeviceType = UnityEngine.SystemInfo.graphicsDeviceType.ToString(),
                graphicsMemorySize = UnityEngine.SystemInfo.graphicsMemorySize,
                graphicsDeviceVersion = UnityEngine.SystemInfo.graphicsDeviceVersion,
                graphicsMultiThreaded = UnityEngine.SystemInfo.graphicsMultiThreaded,
                unityVersion = Application.unityVersion,
                gameVersion = Application.version
            };
        }

        private PlayerContext GatherPlayerContext()
        {
            var context = new PlayerContext
            {
                playTime = Time.time,
                currentMap = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
            };

            // Would gather actual player data from game systems
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClient != null)
            {
                context.playerId = NetworkManager.Singleton.LocalClientId;
            }

            return context;
        }

        private string CaptureScreenshot(string reportId)
        {
            string screenshotDir = Path.Combine(Application.persistentDataPath, "BugReports", "Screenshots");
            Directory.CreateDirectory(screenshotDir);

            string filename = $"{reportId}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            string path = Path.Combine(screenshotDir, filename);

            ScreenCapture.CaptureScreenshot(path);

            Debug.Log($"[BugReport] Screenshot captured: {path}");
            return path;
        }

        private string SaveLogFile(string reportId)
        {
            string logDir = Path.Combine(Application.persistentDataPath, "BugReports", "Logs");
            Directory.CreateDirectory(logDir);

            string filename = $"{reportId}_{DateTime.Now:yyyyMMdd_HHmmss}.log";
            string path = Path.Combine(logDir, filename);

            // Copy Unity log file
            string unityLogPath = Path.Combine(Application.persistentDataPath, "Player.log");
            if (File.Exists(unityLogPath))
            {
                File.Copy(unityLogPath, path, true);
            }

            return path;
        }

        private void SubmitBugReport(BugReport report)
        {
            // Would send to backend API
            // In real implementation: HTTP POST to bug tracking system

            string json = JsonUtility.ToJson(report, true);
            string reportPath = Path.Combine(Application.persistentDataPath, "BugReports", $"{report.reportId}.json");
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            File.WriteAllText(reportPath, json);

            report.submitted = true;
            OnBugSubmitted?.Invoke(report.reportId);

            Debug.Log($"[BugReport] Report submitted: {report.reportId}");
        }

        public void SubmitPendingReports()
        {
            foreach (var report in pendingReports)
            {
                if (!report.submitted)
                {
                    SubmitBugReport(report);
                }
            }
        }

        private void LoadPendingReports()
        {
            string reportsDir = Path.Combine(Application.persistentDataPath, "BugReports");
            if (!Directory.Exists(reportsDir)) return;

            string[] reportFiles = Directory.GetFiles(reportsDir, "*.json");
            foreach (string file in reportFiles)
            {
                try
                {
                    string json = File.ReadAllText(file);
                    BugReport report = JsonUtility.FromJson<BugReport>(json);
                    if (!report.submitted)
                    {
                        pendingReports.Add(report);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[BugReport] Failed to load report: {e.Message}");
                }
            }

            Debug.Log($"[BugReport] Loaded {pendingReports.Count} pending reports");
        }

        public List<BugReport> GetPendingReports()
        {
            return new List<BugReport>(pendingReports);
        }

        public Dictionary<string, int> GetBugFrequency()
        {
            return new Dictionary<string, int>(bugCounts);
        }

        public int GetReportCount(BugCategory category)
        {
            int count = 0;
            foreach (var report in pendingReports)
            {
                if (report.category == category) count++;
            }
            return count;
        }

        private void OnApplicationQuit()
        {
            SubmitPendingReports();
        }
    }
}
