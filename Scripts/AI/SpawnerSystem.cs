using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.AI
{
    public class SpawnerSystem : NetworkBehaviour
    {
        public static SpawnerSystem Instance { get; private set; }

        [Header("Spawn Settings")]
        [SerializeField] private GameObject[] zombiePrefabs;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private float spawnInterval = 5f;
        [SerializeField] private int maxZombies = 50;

        [Header("Wave Settings")]
        [SerializeField] private bool useWaveSystem = true;
        [SerializeField] private int zombiesPerWave = 10;
        [SerializeField] private float waveCooldown = 30f;

        private List<NetworkObject> spawnedZombies = new List<NetworkObject>();
        private int currentWave = 0;
        private float lastSpawnTime;
        private int zombiesSpawnedThisWave = 0;

        public event Action<int> OnWaveStarted;
        public event Action<int> OnWaveCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Update()
        {
            if (!IsServer) return;

            if (useWaveSystem)
            {
                UpdateWaveSystem();
            }
            else
            {
                UpdateContinuousSpawning();
            }
        }

        private void UpdateWaveSystem()
        {
            if (zombiesSpawnedThisWave < zombiesPerWave)
            {
                if (Time.time - lastSpawnTime >= spawnInterval)
                {
                    SpawnZombie();
                    zombiesSpawnedThisWave++;
                    lastSpawnTime = Time.time;
                }
            }
            else
            {
                CheckWaveCompletion();
            }
        }

        private void UpdateContinuousSpawning()
        {
            if (spawnedZombies.Count < maxZombies && Time.time - lastSpawnTime >= spawnInterval)
            {
                SpawnZombie();
                lastSpawnTime = Time.time;
            }
        }

        private void CheckWaveCompletion()
        {
            spawnedZombies.RemoveAll(z => z == null);

            if (spawnedZombies.Count == 0)
            {
                OnWaveCompleted?.Invoke(currentWave);
                StartCoroutine(StartNextWaveAfterDelay());
            }
        }

        private System.Collections.IEnumerator StartNextWaveAfterDelay()
        {
            yield return new WaitForSeconds(waveCooldown);
            StartNextWave();
        }

        private void StartNextWave()
        {
            currentWave++;
            zombiesSpawnedThisWave = 0;
            zombiesPerWave = Mathf.RoundToInt(zombiesPerWave * 1.2f);
            OnWaveStarted?.Invoke(currentWave);
        }

        private void SpawnZombie()
        {
            if (zombiePrefabs.Length == 0 || spawnPoints.Length == 0) return;

            GameObject prefab = zombiePrefabs[UnityEngine.Random.Range(0, zombiePrefabs.Length)];
            Transform spawnPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];

            GameObject zombie = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
            NetworkObject netObj = zombie.GetComponent<NetworkObject>();
            
            if (netObj != null)
            {
                netObj.Spawn();
                spawnedZombies.Add(netObj);
            }
        }

        public int CurrentWave => currentWave;
        public int AliveZombieCount => spawnedZombies.Count;
    }
}
