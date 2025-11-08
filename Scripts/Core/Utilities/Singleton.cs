using UnityEngine;

namespace DeadFrontier.Core
{
    /// <summary>
    /// Generic singleton pattern for MonoBehaviour classes.
    /// Ensures only one instance exists and persists across scenes.
    /// </summary>
    /// <typeparam name="T">The type of the singleton class</typeparam>
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T instance;
        private static readonly object lockObject = new object();
        private static bool isQuitting = false;

        public static T Instance
        {
            get
            {
                if (isQuitting)
                {
                    Debug.LogWarning($"[Singleton] Instance of {typeof(T)} is being accessed after application quit.");
                    return null;
                }

                lock (lockObject)
                {
                    if (instance == null)
                    {
                        instance = FindObjectOfType<T>();

                        if (instance == null)
                        {
                            GameObject singletonObject = new GameObject($"{typeof(T).Name} (Singleton)");
                            instance = singletonObject.AddComponent<T>();
                            DontDestroyOnLoad(singletonObject);
                        }
                    }

                    return instance;
                }
            }
        }

        protected virtual void Awake()
        {
            if (instance == null)
            {
                instance = this as T;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Debug.LogWarning($"[Singleton] Duplicate instance of {typeof(T)} destroyed.");
                Destroy(gameObject);
            }
        }

        protected virtual void OnApplicationQuit()
        {
            isQuitting = true;
        }

        protected virtual void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}
