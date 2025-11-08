using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace DeadFrontier.Core
{
    /// <summary>
    /// Manages object pools for frequently spawned GameObjects
    /// </summary>
    public class PoolManager : Singleton<PoolManager>
    {
        [Header("Pool Settings")]
        [SerializeField] private Transform poolParent;

        private Dictionary<string, ObjectPool<PooledObject>> pools = new Dictionary<string, ObjectPool<PooledObject>>();
        private Dictionary<GameObject, string> activeObjects = new Dictionary<GameObject, string>();

        protected override void Awake()
        {
            base.Awake();

            if (poolParent == null)
            {
                GameObject parent = new GameObject("PooledObjects");
                poolParent = parent.transform;
                poolParent.SetParent(transform);
            }

            Debug.Log("[PoolManager] Initialized");
        }

        /// <summary>
        /// Gets an object from the pool
        /// </summary>
        public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null)
            {
                Debug.LogError("[PoolManager] Attempted to get null prefab");
                return null;
            }

            string key = prefab.name;

            // Create pool if it doesn't exist
            if (!pools.ContainsKey(key))
            {
                CreatePool(prefab);
            }

            PooledObject pooledObj = pools[key].Get(position, rotation);
            GameObject obj = pooledObj.gameObject;

            activeObjects[obj] = key;

            // Call OnPoolGet if the object implements IPoolable
            IPoolable poolable = obj.GetComponent<IPoolable>();
            poolable?.OnPoolGet();

            return obj;
        }

        /// <summary>
        /// Gets an object from the pool at Vector3.zero
        /// </summary>
        public GameObject Get(GameObject prefab)
        {
            return Get(prefab, Vector3.zero, Quaternion.identity);
        }

        /// <summary>
        /// Returns an object to the pool
        /// </summary>
        public void Return(GameObject obj)
        {
            if (obj == null)
            {
                Debug.LogWarning("[PoolManager] Attempted to return null object");
                return;
            }

            if (!activeObjects.TryGetValue(obj, out string key))
            {
                Debug.LogWarning($"[PoolManager] Object {obj.name} was not pooled, destroying instead");
                Destroy(obj);
                return;
            }

            // Call OnPoolReturn if the object implements IPoolable
            IPoolable poolable = obj.GetComponent<IPoolable>();
            poolable?.OnPoolReturn();

            PooledObject pooledObj = obj.GetComponent<PooledObject>();
            if (pooledObj != null && pools.ContainsKey(key))
            {
                pools[key].Return(pooledObj);
                activeObjects.Remove(obj);
            }
            else
            {
                Debug.LogWarning($"[PoolManager] Could not return {obj.name} to pool, destroying");
                Destroy(obj);
            }
        }

        /// <summary>
        /// Returns an object to the pool after a delay
        /// </summary>
        public void Return(GameObject obj, float delay)
        {
            if (delay <= 0f)
            {
                Return(obj);
            }
            else
            {
                StartCoroutine(ReturnDelayed(obj, delay));
            }
        }

        /// <summary>
        /// Prewarms a pool for a specific prefab
        /// </summary>
        public void PrewarmPool(GameObject prefab, int count)
        {
            if (prefab == null)
            {
                Debug.LogError("[PoolManager] Attempted to prewarm null prefab");
                return;
            }

            string key = prefab.name;

            if (!pools.ContainsKey(key))
            {
                CreatePool(prefab);
            }

            pools[key].Prewarm(count);
            Debug.Log($"[PoolManager] Prewarmed pool '{key}' with {count} objects");
        }

        /// <summary>
        /// Clears all pools
        /// </summary>
        public void ClearAllPools()
        {
            foreach (var pool in pools.Values)
            {
                pool.Clear();
            }

            pools.Clear();
            activeObjects.Clear();
            Debug.Log("[PoolManager] All pools cleared");
        }

        /// <summary>
        /// Clears a specific pool
        /// </summary>
        public void ClearPool(GameObject prefab)
        {
            if (prefab == null)
                return;

            string key = prefab.name;

            if (pools.ContainsKey(key))
            {
                pools[key].Clear();
                pools.Remove(key);
                Debug.Log($"[PoolManager] Pool '{key}' cleared");
            }
        }

        /// <summary>
        /// Gets pool statistics for debugging
        /// </summary>
        public void LogPoolStats()
        {
            Debug.Log($"[PoolManager] === Pool Statistics ===");
            Debug.Log($"Total Pools: {pools.Count}");
            Debug.Log($"Total Active Objects: {activeObjects.Count}");

            foreach (var kvp in pools)
            {
                Debug.Log($"  {kvp.Key}: Active={kvp.Value.CountActive}, Inactive={kvp.Value.CountInactive}, Total={kvp.Value.CountTotal}");
            }
        }

        private void CreatePool(GameObject prefab)
        {
            string key = prefab.name;

            // Ensure prefab has PooledObject component
            PooledObject pooledObj = prefab.GetComponent<PooledObject>();
            if (pooledObj == null)
            {
                pooledObj = prefab.AddComponent<PooledObject>();
            }

            GameObject poolContainer = new GameObject($"Pool_{key}");
            poolContainer.transform.SetParent(poolParent);

            ObjectPool<PooledObject> pool = new ObjectPool<PooledObject>(
                pooledObj,
                poolContainer.transform,
                Constants.OBJECT_POOL_DEFAULT_CAPACITY,
                Constants.OBJECT_POOL_MAX_SIZE
            );

            pools[key] = pool;
            Debug.Log($"[PoolManager] Created pool for '{key}'");
        }

        private IEnumerator ReturnDelayed(GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            Return(obj);
        }
    }

    /// <summary>
    /// Component attached to all pooled objects
    /// </summary>
    public class PooledObject : MonoBehaviour
    {
        // This is just a marker component to identify pooled objects
        // The actual pooling logic is in PoolManager
    }
}
