using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Player
{
    public class RespawnSystem : NetworkBehaviour
    {
        public static RespawnSystem Instance { get; private set; }

        [SerializeField] private float respawnDelay = 10f;
        [SerializeField] private int maxRespawns = 3;
        [SerializeField] private bool respawnInWave = true;

        private Dictionary<ulong, RespawnData> playerRespawnData = new Dictionary<ulong, RespawnData>();

        public event Action<ulong> OnPlayerDied;
        public event Action<ulong, float> OnRespawnStarted;
        public event Action<ulong> OnPlayerRespawned;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestRespawnServerRpc(ulong playerId, ServerRpcParams rpcParams = default)
        {
            if (!CanRespawn(playerId)) return;

            StartCoroutine(RespawnPlayerAfterDelay(playerId));
        }

        private IEnumerator RespawnPlayerAfterDelay(ulong playerId)
        {
            OnRespawnStarted?.Invoke(playerId, respawnDelay);
            RespawnStartedClientRpc(playerId, respawnDelay);

            yield return new WaitForSeconds(respawnDelay);

            Vector3 spawnPos = GetRespawnPosition(playerId);
            Health.HealthSystem.Instance?.RespawnEntityServerRpc(playerId, spawnPos);

            if (playerRespawnData.TryGetValue(playerId, out var data))
            {
                data.respawnsUsed++;
                data.lastRespawnTime = Time.time;
            }

            OnPlayerRespawned?.Invoke(playerId);
            PlayerRespawnedClientRpc(playerId);
        }

        private bool CanRespawn(ulong playerId)
        {
            if (!playerRespawnData.TryGetValue(playerId, out var data))
            {
                playerRespawnData[playerId] = new RespawnData { playerId = playerId };
                return true;
            }

            return data.respawnsUsed < maxRespawns;
        }

        private Vector3 GetRespawnPosition(ulong playerId)
        {
            // Get safe spawn point
            return Vector3.zero; // Would return actual spawn point
        }

        [ClientRpc]
        private void RespawnStartedClientRpc(ulong playerId, float delay) { }

        [ClientRpc]
        private void PlayerRespawnedClientRpc(ulong playerId) { }

        public int GetRemainingRespawns(ulong playerId)
        {
            if (playerRespawnData.TryGetValue(playerId, out var data))
            {
                return maxRespawns - data.respawnsUsed;
            }
            return maxRespawns;
        }
    }

    [Serializable]
    public class RespawnData
    {
        public ulong playerId;
        public int respawnsUsed;
        public float lastRespawnTime;
    }
}
