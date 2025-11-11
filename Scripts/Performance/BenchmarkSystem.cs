using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace ZombieGame
{
    /// <summary>
    /// Benchmark System - Performance testing and optimization
    /// Automated tests for FPS, frame time, memory usage, GPU performance
    /// Generates reports and recommended settings
    /// </summary>
    public class BenchmarkSystem : MonoBehaviour
    {
        public static BenchmarkSystem Instance { get; private set; }

        [Header("Benchmark Settings")]
        [SerializeField] private float benchmarkDuration = 60f; // 1 minute
        [SerializeField] private bool runOnStartup = false;

        private bool isBenchmarking = false;
        private float benchmarkTimer = 0f;

        // Performance Metrics
        private List<float> fpssamples = new List<float>();
        private List<float> frameTimeSamples = new List<float>();
        private List<long> memorySamples = new List<long>();

        private BenchmarkResults currentResults = null;

        // Events
        public event System.Action OnBenchmarkStarted;
        public event System.Action<BenchmarkResults> OnBenchmarkCompleted;

        [System.Serializable]
        public class BenchmarkResults
        {
            public System.DateTime benchmarkDate;
            public float duration;

            // FPS Stats
            public float averageFPS;
            public float minFPS;
            public float maxFPS;
            public float fps1Percentile; // 1% low
            public float fps01Percentile; // 0.1% low

            // Frame Time Stats
            public float averageFrameTime; // ms
            public float maxFrameTime; // ms

            // Memory Stats
            public long averageMemoryUsage; // MB
            public long peakMemoryUsage; // MB

            // System Info
            public string gpuName;
            public string cpuName;
            public int systemRAM;
            public string operatingSystem;

            // Quality Settings
            public int qualityLevel;
            public int textureQuality;
            public int shadowQuality;
            public int antiAliasing;
            public bool vsyncEnabled;

            // Score
            public int overallScore; // 0-1000

            // Recommended Settings
            public QualityPreset recommendedPreset;
        }

        public enum QualityPreset
        {
            Low,
            Medium,
            High,
            Ultra
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;

                if (runOnStartup)
                {
                    StartBenchmark();
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (isBenchmarking)
            {
                UpdateBenchmark();
            }
        }

        public void StartBenchmark()
        {
            if (isBenchmarking) return;

            isBenchmarking = true;
            benchmarkTimer = 0f;

            // Reset samples
            fpsSamples.Clear();
            frameTimeSamples.Clear();
            memorySamples.Clear();

            OnBenchmarkStarted?.Invoke();
            Debug.Log("[Benchmark] Benchmark started");
        }

        private void UpdateBenchmark()
        {
            benchmarkTimer += Time.unscaledDeltaTime;

            // Collect samples
            CollectPerformanceSamples();

            // Check if benchmark complete
            if (benchmarkTimer >= benchmarkDuration)
            {
                CompleteBenchmark();
            }
        }

        private void CollectPerformanceSamples()
        {
            // FPS
            float fps = 1f / Time.unscaledDeltaTime;
            fpsSamples.Add(fps);

            // Frame Time
            float frameTime = Time.unscaledDeltaTime * 1000f; // Convert to ms
            frameTimeSamples.Add(frameTime);

            // Memory
            long memory = System.GC.GetTotalMemory(false) / (1024 * 1024); // Convert to MB
            memorySamples.Add(memory);
        }

        private void CompleteBenchmark()
        {
            isBenchmarking = false;

            currentResults = new BenchmarkResults
            {
                benchmarkDate = System.DateTime.Now,
                duration = benchmarkTimer
            };

            // Calculate FPS stats
            var sortedFPS = fpsSamples.OrderBy(f => f).ToList();
            currentResults.averageFPS = fpsSamples.Average();
            currentResults.minFPS = sortedFPS.First();
            currentResults.maxFPS = sortedFPS.Last();

            // Calculate percentiles
            int index1Percent = Mathf.FloorToInt(sortedFPS.Count * 0.01f);
            int index01Percent = Mathf.FloorToInt(sortedFPS.Count * 0.001f);
            currentResults.fps1Percentile = sortedFPS[index1Percent];
            currentResults.fps01Percentile = sortedFPS[index01Percent];

            // Calculate frame time stats
            currentResults.averageFrameTime = frameTimeSamples.Average();
            currentResults.maxFrameTime = frameTimeSamples.Max();

            // Calculate memory stats
            currentResults.averageMemoryUsage = (long)memorySamples.Average();
            currentResults.peakMemoryUsage = memorySamples.Max();

            // Get system info
            currentResults.gpuName = SystemInfo.graphicsDeviceName;
            currentResults.cpuName = SystemInfo.processorType;
            currentResults.systemRAM = SystemInfo.systemMemorySize;
            currentResults.operatingSystem = SystemInfo.operatingSystem;

            // Get current quality settings
            currentResults.qualityLevel = QualitySettings.GetQualityLevel();
            currentResults.textureQuality = (int)QualitySettings.masterTextureLimit;
            currentResults.shadowQuality = (int)QualitySettings.shadows;
            currentResults.antiAliasing = QualitySettings.antiAliasing;
            currentResults.vsyncEnabled = QualitySettings.vSyncCount > 0;

            // Calculate overall score
            currentResults.overallScore = CalculateScore();

            // Determine recommended settings
            currentResults.recommendedPreset = DetermineRecommendedPreset();

            // Save results
            SaveResults();

            OnBenchmarkCompleted?.Invoke(currentResults);
            Debug.Log($"[Benchmark] Benchmark complete! Score: {currentResults.overallScore}/1000");
            PrintResults();
        }

        private int CalculateScore()
        {
            int score = 0;

            // FPS Score (0-400 points)
            if (currentResults.averageFPS >= 144)
                score += 400;
            else if (currentResults.averageFPS >= 120)
                score += 350;
            else if (currentResults.averageFPS >= 60)
                score += 250;
            else if (currentResults.averageFPS >= 30)
                score += 100;

            // Frame Consistency (0-300 points)
            float fpsVariance = currentResults.maxFPS - currentResults.minFPS;
            if (fpsVariance < 10)
                score += 300;
            else if (fpsVariance < 30)
                score += 200;
            else if (fpsVariance < 60)
                score += 100;

            // 1% Low FPS (0-200 points)
            if (currentResults.fps1Percentile >= 60)
                score += 200;
            else if (currentResults.fps1Percentile >= 30)
                score += 100;

            // Frame Time (0-100 points)
            if (currentResults.averageFrameTime < 8.33f) // 120 FPS
                score += 100;
            else if (currentResults.averageFrameTime < 16.67f) // 60 FPS
                score += 75;
            else if (currentResults.averageFrameTime < 33.33f) // 30 FPS
                score += 50;

            return Mathf.Clamp(score, 0, 1000);
        }

        private QualityPreset DetermineRecommendedPreset()
        {
            if (currentResults.averageFPS >= 120 && currentResults.fps1Percentile >= 60)
            {
                return QualityPreset.Ultra;
            }
            else if (currentResults.averageFPS >= 60 && currentResults.fps1Percentile >= 30)
            {
                return QualityPreset.High;
            }
            else if (currentResults.averageFPS >= 30)
            {
                return QualityPreset.Medium;
            }
            else
            {
                return QualityPreset.Low;
            }
        }

        private void PrintResults()
        {
            Debug.Log("=== BENCHMARK RESULTS ===");
            Debug.Log($"Average FPS: {currentResults.averageFPS:F1}");
            Debug.Log($"Min FPS: {currentResults.minFPS:F1}");
            Debug.Log($"Max FPS: {currentResults.maxFPS:F1}");
            Debug.Log($"1% Low FPS: {currentResults.fps1Percentile:F1}");
            Debug.Log($"0.1% Low FPS: {currentResults.fps01Percentile:F1}");
            Debug.Log($"Average Frame Time: {currentResults.averageFrameTime:F2}ms");
            Debug.Log($"Max Frame Time: {currentResults.maxFrameTime:F2}ms");
            Debug.Log($"Average Memory: {currentResults.averageMemoryUsage}MB");
            Debug.Log($"Peak Memory: {currentResults.peakMemoryUsage}MB");
            Debug.Log($"Overall Score: {currentResults.overallScore}/1000");
            Debug.Log($"Recommended Preset: {currentResults.recommendedPreset}");
            Debug.Log("========================");
        }

        private void SaveResults()
        {
            string json = JsonUtility.ToJson(currentResults, true);
            string filePath = Path.Combine(Application.persistentDataPath, "benchmark_results.json");

            File.WriteAllText(filePath, json);

            Debug.Log($"[Benchmark] Results saved to: {filePath}");
        }

        public void ApplyRecommendedSettings()
        {
            if (currentResults == null) return;

            QualityPreset preset = currentResults.recommendedPreset;

            switch (preset)
            {
                case QualityPreset.Low:
                    ApplyLowSettings();
                    break;
                case QualityPreset.Medium:
                    ApplyMediumSettings();
                    break;
                case QualityPreset.High:
                    ApplyHighSettings();
                    break;
                case QualityPreset.Ultra:
                    ApplyUltraSettings();
                    break;
            }

            Debug.Log($"[Benchmark] Applied {preset} settings");
        }

        private void ApplyLowSettings()
        {
            QualitySettings.SetQualityLevel(0);
            QualitySettings.shadows = ShadowQuality.Disable;
            QualitySettings.shadowResolution = ShadowResolution.Low;
            QualitySettings.antiAliasing = 0;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
        }

        private void ApplyMediumSettings()
        {
            QualitySettings.SetQualityLevel(2);
            QualitySettings.shadows = ShadowQuality.HardOnly;
            QualitySettings.shadowResolution = ShadowResolution.Medium;
            QualitySettings.antiAliasing = 2;
            QualitySettings.vSyncCount = 0;
        }

        private void ApplyHighSettings()
        {
            QualitySettings.SetQualityLevel(4);
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.High;
            QualitySettings.antiAliasing = 4;
            QualitySettings.vSyncCount = 0;
        }

        private void ApplyUltraSettings()
        {
            QualitySettings.SetQualityLevel(5);
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
            QualitySettings.antiAliasing = 8;
            QualitySettings.vSyncCount = 0;
        }

        // Getters

        public bool IsBenchmarking()
        {
            return isBenchmarking;
        }

        public float GetBenchmarkProgress()
        {
            return benchmarkTimer / benchmarkDuration;
        }

        public BenchmarkResults GetCurrentResults()
        {
            return currentResults;
        }
    }
}
