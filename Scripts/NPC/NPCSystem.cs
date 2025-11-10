using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.NPC
{
    public class NPCSystem : NetworkBehaviour
    {
        public static NPCSystem Instance { get; private set; }

        [SerializeField] private List<NPCData> npcs = new List<NPCData>();

        public event Action<ulong, string> OnDialogueStarted;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        [ServerRpc(RequireOwnership = false)]
        public void InteractWithNPCServerRpc(ulong playerId, string npcId, ServerRpcParams rpcParams = default)
        {
            var npc = npcs.Find(n => n.npcId == npcId);
            if (npc == null) return;

            OnDialogueStarted?.Invoke(playerId, npcId);
            StartDialogueClientRpc(playerId, npcId);
        }

        [ClientRpc]
        private void StartDialogueClientRpc(ulong playerId, string npcId) { }
    }

    [Serializable]
    public class NPCData
    {
        public string npcId;
        public string npcName;
        public NPCType npcType;
        public List<string> dialogues = new List<string>();
    }

    public enum NPCType { Merchant, QuestGiver, Survivor, Guard }
}
