using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Network
{
    public class NetworkSpawnManager : NetworkBehaviour
    {
        public static NetworkSpawnManager Instance { get; private set; }

        [Header("Player Prefabs")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private Transform[] playerSpawnPoints;

        [Header("Object Pools")]
        [SerializeField] private GameObject[] pooledObjectPrefabs;
        [SerializeField] private int poolSize = 50;

        private Dictionary<string, Queue<NetworkObject>> objectPools = new Dictionary<string, Queue<NetworkObject>>();
        private Dictionary<ulong, NetworkObject> spawnedPlayers = new Dictionary<ulong, NetworkObject>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
                InitializeObjectPools();
            }
        }

        private void OnClientConnected(ulong clientId)
        {
            SpawnPlayerServerRpc(clientId);
        }

        private void OnClientDisconnected(ulong clientId)
        {
            if (spawnedPlayers.TryGetValue(clientId, out var playerObj))
            {
                playerObj.Despawn();
                spawnedPlayers.Remove(clientId);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void SpawnPlayerServerRpc(ulong clientId, ServerRpcParams rpcParams = default)
        {
            if (playerPrefab == null || spawnedPlayers.ContainsKey(clientId)) return;

            Vector3 spawnPos = GetRandomSpawnPoint();
            GameObject player = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
            NetworkObject netObj = player.GetComponent<NetworkObject>();
            
            if (netObj != null)
            {
                netObj.SpawnAsPlayerObject(clientId);
                spawnedPlayers[clientId] = netObj;
            }
        }

        private void InitializeObjectPools()
        {
            foreach (var prefab in pooledObjectPrefabs)
            {
                if (prefab == null) continue;
                
                string poolKey = prefab.name;
                objectPools[poolKey] = new Queue<NetworkObject>();

                for (int i = 0; i < poolSize; i++)
                {
                    GameObject obj = Instantiate(prefab);
                    NetworkObject netObj = obj.GetComponent<NetworkObject>();
                    obj.SetActive(false);
                    objectPools[poolKey].Enqueue(netObj);
                }
            }
        }

        public NetworkObject SpawnFromPool(string poolKey, Vector3 position, Quaternion rotation)
        {
            if (!objectPools.ContainsKey(poolKey) || objectPools[poolKey].Count == 0) return null;

            NetworkObject obj = objectPools[poolKey].Dequeue();
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.gameObject.SetActive(true);
            obj.Spawn();

            return obj;
        }

        public void ReturnToPool(string poolKey, NetworkObject obj)
        {
            if (!objectPools.ContainsKey(poolKey)) return;

            obj.Despawn(false);
            obj.gameObject.SetActive(false);
            objectPools[poolKey].Enqueue(obj);
        }

        private Vector3 GetRandomSpawnPoint()
        {
            if (playerSpawnPoints.Length == 0) return Vector3.zero;
            return playerSpawnPoints[UnityEngine.Random.Range(0, playerSpawnPoints.Length)].position;
        }

        public NetworkObject GetPlayerObject(ulong clientId) => spawnedPlayers.GetValueOrDefault(clientId);
    }
}
