using System.Collections.Generic;
using UnityEngine;

namespace DeadFrontier.Core
{
    /// <summary>
    /// Generic object pool for reusing GameObjects
    /// </summary>
    /// <typeparam name="T">Type of component on the pooled objects</typeparam>
    public class ObjectPool<T> where T : Component
    {
        private readonly T prefab;
        private readonly Queue<T> pool;
        private readonly Transform parent;
        private readonly int defaultCapacity;
        private readonly int maxSize;

        public int CountActive { get; private set; }
        public int CountInactive => pool.Count;
        public int CountTotal => CountActive + CountInactive;

        public ObjectPool(T prefab, Transform parent = null, int defaultCapacity = 20, int maxSize = 100)
        {
            this.prefab = prefab;
            this.parent = parent;
            this.defaultCapacity = defaultCapacity;
            this.maxSize = maxSize;
            this.pool = new Queue<T>(defaultCapacity);

            // Pre-warm the pool
            for (int i = 0; i < defaultCapacity; i++)
            {
                T obj = CreateNewObject();
                obj.gameObject.SetActive(false);
                pool.Enqueue(obj);
            }
        }

        /// <summary>
        /// Gets an object from the pool
        /// </summary>
        public T Get()
        {
            T obj;

            if (pool.Count > 0)
            {
                obj = pool.Dequeue();
            }
            else
            {
                obj = CreateNewObject();
            }

            obj.gameObject.SetActive(true);
            CountActive++;
            return obj;
        }

        /// <summary>
        /// Gets an object from the pool at a specific position and rotation
        /// </summary>
        public T Get(Vector3 position, Quaternion rotation)
        {
            T obj = Get();
            obj.transform.SetPositionAndRotation(position, rotation);
            return obj;
        }

        /// <summary>
        /// Returns an object to the pool
        /// </summary>
        public void Return(T obj)
        {
            if (obj == null)
            {
                Debug.LogWarning("[ObjectPool] Attempted to return null object");
                return;
            }

            obj.gameObject.SetActive(false);
            obj.transform.SetParent(parent);

            if (pool.Count < maxSize)
            {
                pool.Enqueue(obj);
                CountActive--;
            }
            else
            {
                // Pool is full, destroy the object
                Object.Destroy(obj.gameObject);
                CountActive--;
            }
        }

        /// <summary>
        /// Clears the pool and destroys all inactive objects
        /// </summary>
        public void Clear()
        {
            while (pool.Count > 0)
            {
                T obj = pool.Dequeue();
                if (obj != null)
                {
                    Object.Destroy(obj.gameObject);
                }
            }
            CountActive = 0;
        }

        /// <summary>
        /// Prewarms the pool by creating additional objects
        /// </summary>
        public void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (pool.Count >= maxSize)
                    break;

                T obj = CreateNewObject();
                obj.gameObject.SetActive(false);
                pool.Enqueue(obj);
            }
        }

        private T CreateNewObject()
        {
            T obj = Object.Instantiate(prefab, parent);
            obj.name = $"{prefab.name} (Pooled)";
            return obj;
        }
    }

    /// <summary>
    /// Interface for pooled objects that need initialization/cleanup
    /// </summary>
    public interface IPoolable
    {
        void OnPoolGet();
        void OnPoolReturn();
    }
}
