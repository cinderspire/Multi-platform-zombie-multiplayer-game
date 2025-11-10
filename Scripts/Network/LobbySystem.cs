using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Network
{
    public class LobbySystem : NetworkBehaviour
    {
        public static LobbySystem Instance { get; private set; }

        [Header("Lobby Settings")]
        [SerializeField] private int maxPlayers = 4;
        [SerializeField] private float countdownDuration = 10f;

        private NetworkVariable<LobbyState> currentState = new NetworkVariable<LobbyState>(LobbyState.Waiting);
        private NetworkVariable<float> countdownTimer = new NetworkVariable<float>();
        private Dictionary<ulong, PlayerLobbyInfo> lobbyPlayers = new Dictionary<ulong, PlayerLobbyInfo>();

        public event Action<LobbyState> OnLobbyStateChanged;
        public event Action<ulong, bool> OnPlayerReadyChanged;
        public event Action OnGameStarting;

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
                NetworkManager.Singleton.OnClientConnectedCallback += OnPlayerJoinedLobby;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnPlayerLeftLobby;
            }
        }

        private void Update()
        {
            if (!IsServer) return;

            if (currentState.Value == LobbyState.Countdown)
            {
                countdownTimer.Value -= Time.deltaTime;
                if (countdownTimer.Value <= 0f)
                {
                    StartGame();
                }
            }
        }

        private void OnPlayerJoinedLobby(ulong clientId)
        {
            lobbyPlayers[clientId] = new PlayerLobbyInfo
            {
                clientId = clientId,
                isReady = false,
                playerName = $"Player{clientId}"
            };

            NotifyPlayerJoinedClientRpc(clientId);
        }

        private void OnPlayerLeftLobby(ulong clientId)
        {
            if (lobbyPlayers.ContainsKey(clientId))
            {
                lobbyPlayers.Remove(clientId);
                NotifyPlayerLeftClientRpc(clientId);

                if (currentState.Value == LobbyState.Countdown)
                {
                    CheckAllPlayersReady();
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void SetPlayerReadyServerRpc(ulong clientId, bool ready, ServerRpcParams rpcParams = default)
        {
            if (!lobbyPlayers.ContainsKey(clientId)) return;

            lobbyPlayers[clientId].isReady = ready;
            OnPlayerReadyChanged?.Invoke(clientId, ready);
            NotifyPlayerReadyClientRpc(clientId, ready);

            CheckAllPlayersReady();
        }

        private void CheckAllPlayersReady()
        {
            if (lobbyPlayers.Count < 1) return;

            bool allReady = true;
            foreach (var player in lobbyPlayers.Values)
            {
                if (!player.isReady)
                {
                    allReady = false;
                    break;
                }
            }

            if (allReady && currentState.Value == LobbyState.Waiting)
            {
                StartCountdown();
            }
            else if (!allReady && currentState.Value == LobbyState.Countdown)
            {
                CancelCountdown();
            }
        }

        private void StartCountdown()
        {
            currentState.Value = LobbyState.Countdown;
            countdownTimer.Value = countdownDuration;
            OnLobbyStateChanged?.Invoke(LobbyState.Countdown);
            NotifyCountdownStartClientRpc();
        }

        private void CancelCountdown()
        {
            currentState.Value = LobbyState.Waiting;
            OnLobbyStateChanged?.Invoke(LobbyState.Waiting);
            NotifyCountdownCancelledClientRpc();
        }

        private void StartGame()
        {
            currentState.Value = LobbyState.InGame;
            OnLobbyStateChanged?.Invoke(LobbyState.InGame);
            OnGameStarting?.Invoke();
            NotifyGameStartingClientRpc();
        }

        [ClientRpc]
        private void NotifyPlayerJoinedClientRpc(ulong clientId) { }

        [ClientRpc]
        private void NotifyPlayerLeftClientRpc(ulong clientId) { }

        [ClientRpc]
        private void NotifyPlayerReadyClientRpc(ulong clientId, bool ready) { }

        [ClientRpc]
        private void NotifyCountdownStartClientRpc() { }

        [ClientRpc]
        private void NotifyCountdownCancelledClientRpc() { }

        [ClientRpc]
        private void NotifyGameStartingClientRpc() { }

        public LobbyState CurrentState => currentState.Value;
        public float CountdownTime => countdownTimer.Value;
        public int PlayerCount => lobbyPlayers.Count;
    }

    [Serializable]
    public class PlayerLobbyInfo
    {
        public ulong clientId;
        public string playerName;
        public bool isReady;
    }

    public enum LobbyState { Waiting, Countdown, InGame }
}
