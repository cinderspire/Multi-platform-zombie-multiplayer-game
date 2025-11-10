using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Map
{
    public class MapSystem : NetworkBehaviour
    {
        public static MapSystem Instance { get; private set; }

        [Header("Available Maps")]
        [SerializeField] private List<MapData> availableMaps = new List<MapData>();

        private NetworkVariable<int> currentMapIndex = new NetworkVariable<int>(-1);

        public event Action<MapData> OnMapSelected;
        public event Action<MapData> OnMapLoaded;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        [ServerRpc(RequireOwnership = false)]
        public void SelectMapServerRpc(int mapIndex, ServerRpcParams rpcParams = default)
        {
            if (mapIndex < 0 || mapIndex >= availableMaps.Count) return;

            currentMapIndex.Value = mapIndex;
            MapData selectedMap = availableMaps[mapIndex];
            OnMapSelected?.Invoke(selectedMap);
            SelectMapClientRpc(mapIndex);
        }

        [ClientRpc]
        private void SelectMapClientRpc(int mapIndex)
        {
            if (mapIndex >= 0 && mapIndex < availableMaps.Count)
            {
                OnMapSelected?.Invoke(availableMaps[mapIndex]);
            }
        }

        public MapData GetCurrentMap()
        {
            int index = currentMapIndex.Value;
            if (index >= 0 && index < availableMaps.Count)
            {
                return availableMaps[index];
            }
            return null;
        }

        public List<MapData> GetAvailableMaps() => new List<MapData>(availableMaps);
    }

    [Serializable]
    public class MapData
    {
        public string mapName;
        public string mapDescription;
        public string sceneName;
        public Sprite mapThumbnail;
        public int maxPlayers;
        public MapDifficulty difficulty;
    }

    public enum MapDifficulty { Easy, Medium, Hard, Expert }
}
