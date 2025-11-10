using System;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Communication
{
    public class PingSystem : NetworkBehaviour
    {
        public static PingSystem Instance { get; private set; }

        public event Action<ulong, PingType, Vector3> OnPingCreated;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        [ServerRpc(RequireOwnership = false)]
        public void CreatePingServerRpc(ulong playerId, PingType type, Vector3 position, ServerRpcParams rpcParams = default)
        {
            OnPingCreated?.Invoke(playerId, type, position);
            CreatePingClientRpc(playerId, type, position);
        }

        [ClientRpc]
        private void CreatePingClientRpc(ulong playerId, PingType type, Vector3 position) { }
    }

    public enum PingType { Attack, Defend, Help, Enemy, Item, Danger, GoHere }
}
