using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Player
{
    public class LoadoutSystem : NetworkBehaviour
    {
        public static LoadoutSystem Instance { get; private set; }

        [SerializeField] private int maxLoadouts = 5;

        private Dictionary<ulong, List<Loadout>> playerLoadouts = new Dictionary<ulong, List<Loadout>>();

        public event Action<ulong, int> OnLoadoutSaved;
        public event Action<ulong, int> OnLoadoutLoaded;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        [ServerRpc(RequireOwnership = false)]
        public void SaveLoadoutServerRpc(ulong playerId, int slotIndex, Loadout loadout, ServerRpcParams rpcParams = default)
        {
            if (!playerLoadouts.ContainsKey(playerId))
            {
                playerLoadouts[playerId] = new List<Loadout>();
            }

            while (playerLoadouts[playerId].Count <= slotIndex)
            {
                playerLoadouts[playerId].Add(new Loadout());
            }

            playerLoadouts[playerId][slotIndex] = loadout;
            OnLoadoutSaved?.Invoke(playerId, slotIndex);
        }

        [ServerRpc(RequireOwnership = false)]
        public void LoadLoadoutServerRpc(ulong playerId, int slotIndex, ServerRpcParams rpcParams = default)
        {
            if (!playerLoadouts.TryGetValue(playerId, out var loadouts)) return;
            if (slotIndex >= loadouts.Count) return;

            var loadout = loadouts[slotIndex];
            ApplyLoadout(playerId, loadout);
            OnLoadoutLoaded?.Invoke(playerId, slotIndex);
        }

        private void ApplyLoadout(ulong playerId, Loadout loadout)
        {
            // Apply weapons, perks, etc.
        }

        public List<Loadout> GetPlayerLoadouts(ulong playerId) => playerLoadouts.GetValueOrDefault(playerId, new List<Loadout>());
    }

    [Serializable]
    public class Loadout
    {
        public string loadoutName;
        public List<string> weapons = new List<string>();
        public List<string> perks = new List<string>();
        public List<string> equipment = new List<string>();
    }
}
