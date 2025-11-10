using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Resources
{
    public class ResourceGatheringSystem : NetworkBehaviour
    {
        public static ResourceGatheringSystem Instance { get; private set; }

        [SerializeField] private float gatherTime = 3f;

        private Dictionary<ulong, GatheringState> activeGathering = new Dictionary<ulong, GatheringState>();

        public event Action<ulong, string, int> OnResourceGathered;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        [ServerRpc(RequireOwnership = false)]
        public void StartGatheringServerRpc(ulong playerId, string resourceNodeId, ServerRpcParams rpcParams = default)
        {
            if (activeGathering.ContainsKey(playerId)) return;

            activeGathering[playerId] = new GatheringState 
            { 
                playerId = playerId, 
                resourceNodeId = resourceNodeId, 
                startTime = Time.time 
            };

            StartCoroutine(CompleteGatheringAfterDelay(playerId, resourceNodeId));
        }

        private System.Collections.IEnumerator CompleteGatheringAfterDelay(ulong playerId, string resourceNodeId)
        {
            yield return new WaitForSeconds(gatherTime);

            if (activeGathering.ContainsKey(playerId))
            {
                int amount = UnityEngine.Random.Range(1, 5);
                OnResourceGathered?.Invoke(playerId, "wood", amount);
                activeGathering.Remove(playerId);
            }
        }
    }

    [Serializable]
    public class GatheringState
    {
        public ulong playerId;
        public string resourceNodeId;
        public float startTime;
    }
}
