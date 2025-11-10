using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Map
{
    public class SafeZoneSystem : NetworkBehaviour
    {
        public static SafeZoneSystem Instance { get; private set; }

        [SerializeField] private List<SafeZone> safeZones = new List<SafeZone>();

        private Dictionary<ulong, string> playerInZone = new Dictionary<ulong, string>();

        public event Action<ulong, string> OnPlayerEnteredSafeZone;
        public event Action<ulong, string> OnPlayerExitedSafeZone;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        [ServerRpc(RequireOwnership = false)]
        public void EnterSafeZoneServerRpc(ulong playerId, string zoneId, ServerRpcParams rpcParams = default)
        {
            playerInZone[playerId] = zoneId;
            Health.HealthSystem.Instance?.SetInvulnerableServerRpc(playerId, true, 0f);
            OnPlayerEnteredSafeZone?.Invoke(playerId, zoneId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void ExitSafeZoneServerRpc(ulong playerId, ServerRpcParams rpcParams = default)
        {
            if (!playerInZone.TryGetValue(playerId, out string zoneId)) return;
            playerInZone.Remove(playerId);
            Health.HealthSystem.Instance?.SetInvulnerableServerRpc(playerId, false, 0f);
            OnPlayerExitedSafeZone?.Invoke(playerId, zoneId);
        }

        public bool IsPlayerInSafeZone(ulong playerId) => playerInZone.ContainsKey(playerId);
    }

    [Serializable]
    public class SafeZone
    {
        public string zoneId;
        public string zoneName;
        public Vector3 center;
        public float radius;
        public bool hasShop;
        public bool hasRest;
    }
}
