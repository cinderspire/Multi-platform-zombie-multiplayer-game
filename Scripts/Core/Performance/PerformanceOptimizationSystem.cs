using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Core.Performance
{
    /// <summary>
    /// Advanced performance optimization system managing LOD (Level of Detail),
    /// object pooling optimization, culling, and dynamic quality adjustments.
    /// </summary>
    public class PerformanceOptimizationSystem : MonoBehaviour
    {
        public static PerformanceOptimizationSystem Instance { get; private set; }

        [Header("LOD Configuration")]
        [SerializeField] private bool enableDynamicLOD = true;
        [SerializeField] private float lodUpdateInterval = 0.5f;
        [SerializeField] private float[] lodDistances = new float[] { 25f, 50f, 100f, 200f };

        [Header("Culling Configuration")]
        [SerializeField] private bool enableOcclusionCulling = true;
        [SerializeField] private bool enableFrustumCulling = true;
        [SerializeField] private float cullingDistance = 500f;

        [Header("Quality Scaling")]
        [SerializeField] private bool enableDynamicQuality = true;
        [SerializeField] private int targetFPS = 60;
        [SerializeField] private float qualityAdjustThreshold = 0.8f;

        private Dictionary<GameObject, LODGroup> managedLODs = new Dictionary<GameObject, LODGroup>();
        private List<Renderer> culledObjects = new List<Renderer>();
        private float lastLODUpdate;
        private float lastCullingUpdate;
        private int currentQualityLevel = 3; // 0-5, 5 being highest

        private PerformanceMetrics metrics = new PerformanceMetrics();

        public event Action<int> OnQualityLevelChanged;
        public event Action<PerformanceMetrics> OnMetricsUpdated;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Update()
        {
            UpdatePerformanceMetrics();

            if (enableDynamicLOD && Time.time - lastLODUpdate > lodUpdateInterval)
            {
                UpdateLODSystem();
                lastLODUpdate = Time.time;
            }

            if (enableFrustumCulling && Time.time - lastCullingUpdate > 0.1f)
            {
                UpdateCullingSystem();
                lastCullingUpdate = Time.time;
            }

            if (enableDynamicQuality)
            {
                AdjustQualityBasedOnPerformance();
            }
        }

        /// <summary>
        /// Register an object for LOD management
        /// </summary>
        public void RegisterLODObject(GameObject obj, Renderer[] lodRenderers)
        {
            if (managedLODs.ContainsKey(obj)) return;

            LODGroup lodGroup = obj.GetComponent<LODGroup>();
            if (lodGroup == null)
            {
                lodGroup = obj.AddComponent<LODGroup>();
            }

            // Setup LOD levels
            LOD[] lods = new LOD[lodRenderers.Length];
            for (int i = 0; i < lodRenderers.Length; i++)
            {
                float distance = i < lodDistances.Length ? lodDistances[i] : lodDistances[lodDistances.Length - 1];
                lods[i] = new LOD(1f - (distance / cullingDistance), new Renderer[] { lodRenderers[i] });
            }

            lodGroup.SetLODs(lods);
            lodGroup.RecalculateBounds();

            managedLODs[obj] = lodGroup;
        }

        private void UpdateLODSystem()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null) return;

            Vector3 cameraPos = mainCamera.transform.position;

            foreach (var kvp in managedLODs)
            {
                if (kvp.Key == null) continue;

                float distance = Vector3.Distance(cameraPos, kvp.Key.transform.position);

                // Determine LOD level based on distance
                int lodLevel = 0;
                for (int i = 0; i < lodDistances.Length; i++)
                {
                    if (distance > lodDistances[i])
                    {
                        lodLevel = i + 1;
                    }
                }

                // LODGroup handles this automatically, but we can optimize further
                if (distance > cullingDistance)
                {
                    // Disable completely if beyond culling distance
                    kvp.Key.SetActive(false);
                }
                else if (!kvp.Key.activeSelf)
                {
                    kvp.Key.SetActive(true);
                }
            }
        }

        private void UpdateCullingSystem()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null) return;

            Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(mainCamera);

            foreach (var renderer in culledObjects)
            {
                if (renderer == null) continue;

                bool isVisible = GeometryUtility.TestPlanesAABB(frustumPlanes, renderer.bounds);
                renderer.enabled = isVisible;
            }
        }

        private void UpdatePerformanceMetrics()
        {
            metrics.currentFPS = 1f / Time.deltaTime;
            metrics.frameTime = Time.deltaTime * 1000f;
            metrics.drawCalls = UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(null);
            metrics.triangleCount = GetTotalTriangles();
            metrics.memoryUsage = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);

            OnMetricsUpdated?.Invoke(metrics);
        }

        private void AdjustQualityBasedOnPerformance()
        {
            float performanceRatio = metrics.currentFPS / targetFPS;

            if (performanceRatio < qualityAdjustThreshold && currentQualityLevel > 0)
            {
                // Decrease quality
                DecreaseQuality();
            }
            else if (performanceRatio > 1.2f && currentQualityLevel < 5)
            {
                // Increase quality
                IncreaseQuality();
            }
        }

        private void DecreaseQuality()
        {
            currentQualityLevel = Mathf.Max(0, currentQualityLevel - 1);
            ApplyQualitySettings(currentQualityLevel);
            OnQualityLevelChanged?.Invoke(currentQualityLevel);
            Debug.Log($"Performance: Quality decreased to level {currentQualityLevel}");
        }

        private void IncreaseQuality()
        {
            currentQualityLevel = Mathf.Min(5, currentQualityLevel + 1);
            ApplyQualitySettings(currentQualityLevel);
            OnQualityLevelChanged?.Invoke(currentQualityLevel);
            Debug.Log($"Performance: Quality increased to level {currentQualityLevel}");
        }

        private void ApplyQualitySettings(int qualityLevel)
        {
            QualitySettings.SetQualityLevel(qualityLevel, true);

            // Custom adjustments based on level
            switch (qualityLevel)
            {
                case 0: // Low
                    QualitySettings.shadows = ShadowQuality.Disable;
                    QualitySettings.shadowDistance = 20f;
                    QualitySettings.particleRaycastBudget = 64;
                    break;

                case 1: // Medium-Low
                    QualitySettings.shadows = ShadowQuality.HardOnly;
                    QualitySettings.shadowDistance = 50f;
                    QualitySettings.particleRaycastBudget = 128;
                    break;

                case 2: // Medium
                    QualitySettings.shadows = ShadowQuality.HardOnly;
                    QualitySettings.shadowDistance = 100f;
                    QualitySettings.particleRaycastBudget = 256;
                    break;

                case 3: // Medium-High
                    QualitySettings.shadows = ShadowQuality.All;
                    QualitySettings.shadowDistance = 150f;
                    QualitySettings.particleRaycastBudget = 512;
                    break;

                case 4: // High
                    QualitySettings.shadows = ShadowQuality.All;
                    QualitySettings.shadowDistance = 250f;
                    QualitySettings.particleRaycastBudget = 1024;
                    break;

                case 5: // Ultra
                    QualitySettings.shadows = ShadowQuality.All;
                    QualitySettings.shadowDistance = 500f;
                    QualitySettings.particleRaycastBudget = 2048;
                    QualitySettings.antiAliasing = 8;
                    break;
            }
        }

        private int GetTotalTriangles()
        {
            int total = 0;
            foreach (var renderer in FindObjectsOfType<MeshRenderer>())
            {
                if (!renderer.enabled) continue;

                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                if (filter != null && filter.sharedMesh != null)
                {
                    total += filter.sharedMesh.triangles.Length / 3;
                }
            }
            return total;
        }

        public void RegisterForCulling(Renderer renderer)
        {
            if (!culledObjects.Contains(renderer))
            {
                culledObjects.Add(renderer);
            }
        }

        public void UnregisterFromCulling(Renderer renderer)
        {
            culledObjects.Remove(renderer);
        }

        public PerformanceMetrics GetMetrics()
        {
            return metrics;
        }

        public void SetQualityLevel(int level)
        {
            currentQualityLevel = Mathf.Clamp(level, 0, 5);
            ApplyQualitySettings(currentQualityLevel);
            OnQualityLevelChanged?.Invoke(currentQualityLevel);
        }

        public void SetLODDistances(float[] distances)
        {
            lodDistances = distances;
        }

        public void EnableDynamicQuality(bool enable)
        {
            enableDynamicQuality = enable;
        }

        [Serializable]
        public class PerformanceMetrics
        {
            public float currentFPS;
            public float frameTime;
            public long drawCalls;
            public int triangleCount;
            public float memoryUsage;
            public float gpuMemory;
        }
    }
}
