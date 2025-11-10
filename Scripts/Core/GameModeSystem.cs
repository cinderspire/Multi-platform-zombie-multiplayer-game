using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Core
{
    public class GameModeSystem : NetworkBehaviour
    {
        public static GameModeSystem Instance { get; private set; }

        [SerializeField] private List<GameModeData> gameModes = new List<GameModeData>();
        private NetworkVariable<GameMode> currentMode = new NetworkVariable<GameMode>();

        public event Action<GameMode> OnGameModeChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        [ServerRpc(RequireOwnership = false)]
        public void SetGameModeServerRpc(GameMode mode, ServerRpcParams rpcParams = default)
        {
            currentMode.Value = mode;
            OnGameModeChanged?.Invoke(mode);
            ConfigureGameMode(mode);
        }

        private void ConfigureGameMode(GameMode mode)
        {
            switch (mode)
            {
                case GameMode.Survival:
                    // Endless waves, no objectives
                    break;
                case GameMode.Horde:
                    // Timer-based survival
                    break;
                case GameMode.Extraction:
                    // Reach extraction point
                    break;
                case GameMode.PvPvE:
                    // Player vs Player vs Zombies
                    break;
            }
        }

        public GameMode CurrentMode => currentMode.Value;
    }

    [Serializable]
    public class GameModeData
    {
        public GameMode mode;
        public string modeName;
        public string description;
        public int minPlayers;
        public int maxPlayers;
    }

    public enum GameMode { Survival, Horde, Extraction, PvPvE, Campaign, Arena }
}
