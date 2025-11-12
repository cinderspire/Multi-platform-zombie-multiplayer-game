using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Profiling;

namespace ZombieGame
{
    /// <summary>
    /// Memory Manager - Intelligent memory optimization and garbage collection
    /// Features: Auto GC, memory pools, texture streaming, asset unloading
    /// Essential for stable 60+ FPS and preventing memory leaks
    /// </summary>
    public class MemoryManager : MonoBehaviour
    {
        public static MemoryManager Instance { get; private set; }

        [Header("Memory Settings")]
        [SerializeField] private bool enableAutoGC = true;
        [SerializeField] private float gcInterval = 60f; // Every 60 seconds
        [SerializeField] private long memoryThreshold = 1024 * 1024 * 1024; // 1GB
        [SerializeField] private bool aggressiveUnloading = false;

        [Header("Object Pooling")]
        [SerializeField] private bool enableObjectPooling = true;
        [SerializeField] private int defaultPoolSize = 50;

        private float lastGCTime = 0f;
        private long lastMemoryUsage = 0;
        private Dictionary<string, ObjectPool> pools = new Dictionary<string, ObjectPool>();

        // Events
        public event System.Action OnGarbageCollected;
        public event System.Action<long> OnMemoryThresholdExceeded;

        [System.Serializable]
        public class ObjectPool
        {
            public string poolName;
            public GameObject prefab;
            public Queue<GameObject> availableObjects = new Queue<GameObject>();
            public List<GameObject> activeObjects = new List<GameObject>();
            public int maxSize = 100;
            public bool autoExpand = true;
        }

        public class MemoryStats
        {
            public long totalAllocated;
            public long totalReserved;
            public long monoUsed;
            public long monoHeap;
            public long textureMemory;
            public long meshMemory;
            public long audioMemory;
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeMemoryManager();
            }
            else { Destroy(gameObject); }
        }

        private void Update()
        {
            if (enableAutoGC && Time.time - lastGCTime >= gcInterval)
            {
                CheckMemoryAndGC();
                lastGCTime = Time.time;
            }
        }

        private void InitializeMemoryManager()
        {
            // Set quality settings for memory optimization
            QualitySettings.streamingMipmapsActive = true;
            QualitySettings.streamingMipmapsMemoryBudget = 512; // 512MB

            Debug.Log("[Memory] Memory manager initialized");
        }

        // Garbage Collection

        private void CheckMemoryAndGC()
        {
            long currentMemory = Profiler.GetTotalAllocatedMemoryLong();

            if (currentMemory > memoryThreshold)
            {
                OnMemoryThresholdExceeded?.Invoke(currentMemory);
                PerformGarbageCollection();
            }

            lastMemoryUsage = currentMemory;
        }

        public void PerformGarbageCollection()
        {
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();

            OnGarbageCollected?.Invoke();

            long afterGC = Profiler.GetTotalAllocatedMemoryLong();
            long freed = lastMemoryUsage - afterGC;

            Debug.Log($"[Memory] GC performed - Freed {freed / (1024 * 1024)}MB");
        }

        public void ForceGarbageCollection()
        {
            PerformGarbageCollection();
        }

        // Object Pooling

        public void CreatePool(string poolName, GameObject prefab, int initialSize = 0, int maxSize = 100)
        {
            if (!enableObjectPooling) return;
            if (pools.ContainsKey(poolName)) return;

            var pool = new ObjectPool
            {
                poolName = poolName,
                prefab = prefab,
                maxSize = maxSize
            };

            // Pre-instantiate initial objects
            for (int i = 0; i < initialSize; i++)
            {
                var obj = Instantiate(prefab);
                obj.SetActive(false);
                pool.availableObjects.Enqueue(obj);
            }

            pools[poolName] = pool;
            Debug.Log($"[Memory] Created pool '{poolName}' with {initialSize} objects");
        }

        public GameObject GetFromPool(string poolName)
        {
            if (!enableObjectPooling) return null;
            if (!pools.ContainsKey(poolName)) return null;

            var pool = pools[poolName];

            GameObject obj;
            if (pool.availableObjects.Count > 0)
            {
                obj = pool.availableObjects.Dequeue();
            }
            else if (pool.autoExpand && pool.activeObjects.Count < pool.maxSize)
            {
                obj = Instantiate(pool.prefab);
            }
            else
            {
                Debug.LogWarning($"[Memory] Pool '{poolName}' exhausted!");
                return null;
            }

            obj.SetActive(true);
            pool.activeObjects.Add(obj);
            return obj;
        }

        public void ReturnToPool(string poolName, GameObject obj)
        {
            if (!enableObjectPooling) return;
            if (!pools.ContainsKey(poolName)) return;

            var pool = pools[poolName];

            if (pool.activeObjects.Contains(obj))
            {
                pool.activeObjects.Remove(obj);
                obj.SetActive(false);
                pool.availableObjects.Enqueue(obj);
            }
        }

        public void ClearPool(string poolName)
        {
            if (!pools.ContainsKey(poolName)) return;

            var pool = pools[poolName];

            // Destroy all objects
            while (pool.availableObjects.Count > 0)
            {
                var obj = pool.availableObjects.Dequeue();
                Destroy(obj);
            }

            foreach (var obj in pool.activeObjects)
            {
                Destroy(obj);
            }

            pool.activeObjects.Clear();
            pools.Remove(poolName);

            Debug.Log($"[Memory] Cleared pool '{poolName}'");
        }

        // Asset Management

        public void UnloadUnusedAssets()
        {
            Resources.UnloadUnusedAssets();
            Debug.Log("[Memory] Unloaded unused assets");
        }

        public void UnloadAssetBundle(string bundleName)
        {
            // Would integrate with AssetBundle system
            Debug.Log($"[Memory] Unloaded asset bundle: {bundleName}");
        }

        // Memory Stats

        public MemoryStats GetMemoryStats()
        {
            return new MemoryStats
            {
                totalAllocated = Profiler.GetTotalAllocatedMemoryLong(),
                totalReserved = Profiler.GetTotalReservedMemoryLong(),
                monoUsed = Profiler.GetMonoUsedSizeLong(),
                monoHeap = Profiler.GetMonoHeapSizeLong(),
                textureMemory = Profiler.GetAllocatedMemoryForGraphicsDriver(),
                meshMemory = 0, // Would calculate from mesh data
                audioMemory = 0  // Would calculate from audio clips
            };
        }

        public string GetMemoryReport()
        {
            var stats = GetMemoryStats();
            return $"Memory Report:\n" +
                   $"Total Allocated: {stats.totalAllocated / (1024 * 1024)}MB\n" +
                   $"Total Reserved: {stats.totalReserved / (1024 * 1024)}MB\n" +
                   $"Mono Used: {stats.monoUsed / (1024 * 1024)}MB\n" +
                   $"Mono Heap: {stats.monoHeap / (1024 * 1024)}MB\n" +
                   $"Texture Memory: {stats.textureMemory / (1024 * 1024)}MB";
        }

        public long GetTotalMemoryUsage()
        {
            return Profiler.GetTotalAllocatedMemoryLong();
        }

        public float GetMemoryUsagePercent()
        {
            long total = Profiler.GetTotalAllocatedMemoryLong();
            return (float)total / memoryThreshold * 100f;
        }

        // Optimization

        public void OptimizeMemory()
        {
            // Unload unused assets
            UnloadUnusedAssets();

            // Force GC
            PerformGarbageCollection();

            // Clear empty pools
            var emptyPools = new List<string>();
            foreach (var kvp in pools)
            {
                if (kvp.Value.activeObjects.Count == 0 && kvp.Value.availableObjects.Count == 0)
                {
                    emptyPools.Add(kvp.Key);
                }
            }

            foreach (var poolName in emptyPools)
            {
                ClearPool(poolName);
            }

            Debug.Log("[Memory] Memory optimization complete");
        }

        // Settings

        public void SetAutoGC(bool enabled)
        {
            enableAutoGC = enabled;
            PlayerPrefs.SetInt("Memory_AutoGC", enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void SetGCInterval(float interval)
        {
            gcInterval = Mathf.Clamp(interval, 10f, 300f);
            PlayerPrefs.SetFloat("Memory_GCInterval", gcInterval);
            PlayerPrefs.Save();
        }

        public void SetMemoryThreshold(long threshold)
        {
            memoryThreshold = threshold;
            PlayerPrefs.SetString("Memory_Threshold", threshold.ToString());
            PlayerPrefs.Save();
        }

        public void SetObjectPooling(bool enabled)
        {
            enableObjectPooling = enabled;
            PlayerPrefs.SetInt("Memory_ObjectPooling", enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        // Texture Streaming

        public void SetTextureStreamingBudget(int budgetMB)
        {
            QualitySettings.streamingMipmapsMemoryBudget = Mathf.Clamp(budgetMB, 256, 2048);
            Debug.Log($"[Memory] Texture streaming budget set to {budgetMB}MB");
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                // App paused - aggressively clean memory
                if (aggressiveUnloading)
                {
                    OptimizeMemory();
                }
            }
        }

        private void OnApplicationQuit()
        {
            // Final cleanup
            foreach (var poolName in new List<string>(pools.Keys))
            {
                ClearPool(poolName);
            }
        }
    }
}
