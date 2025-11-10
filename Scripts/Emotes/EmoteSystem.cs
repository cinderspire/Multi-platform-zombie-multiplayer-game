using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Emotes
{
    public class EmoteSystem : NetworkBehaviour
    {
        public static EmoteSystem Instance { get; private set; }

        [SerializeField] private List<EmoteData> emotes = new List<EmoteData>();

        public event Action<ulong, string> OnEmotePerformed;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        [ServerRpc(RequireOwnership = false)]
        public void PerformEmoteServerRpc(ulong playerId, string emoteId, ServerRpcParams rpcParams = default)
        {
            OnEmotePerformed?.Invoke(playerId, emoteId);
            PerformEmoteClientRpc(playerId, emoteId);
        }

        [ClientRpc]
        private void PerformEmoteClientRpc(ulong playerId, string emoteId) { }
    }

    [Serializable]
    public class EmoteData
    {
        public string emoteId;
        public string emoteName;
        public EmoteType type;
        public float duration;
    }

    public enum EmoteType { Wave, Dance, Taunt, Salute, Laugh, Cry }
}
