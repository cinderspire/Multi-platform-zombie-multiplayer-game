using System;
using System.Collections;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Core
{
    /// <summary>
    /// Central game manager that coordinates all systems and manages game flow.
    /// This is the MAIN entry point for the entire game.
    /// </summary>
    public class GameManager : NetworkBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game State")]
        [SerializeField] private GameState startingState = GameState.MainMenu;
        
        private NetworkVariable<GameState> currentState = new NetworkVariable<GameState>();
        private NetworkVariable<float> gameTime = new NetworkVariable<float>();

        public event Action<GameState, GameState> OnGameStateChanged;
        public event Action OnGameStarted;
        public event Action OnGameEnded;
        public event Action<string> OnVictory;
        public event Action<string> OnDefeat;

        private void Awake()
        {
            if (Instance != null && Instance != this) 
            { 
                Destroy(gameObject); 
                return; 
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            InitializeGame();
        }

        private void Update()
        {
            if (IsServer && currentState.Value == GameState.Playing)
            {
                gameTime.Value += Time.deltaTime;
            }
        }

        private void InitializeGame()
        {
            Debug.Log("=== GAME MANAGER INITIALIZING ===");
            
            // Set initial state
            if (IsServer)
            {
                ChangeGameState(startingState);
            }

            // Subscribe to important events
            SubscribeToEvents();

            Debug.Log("=== GAME MANAGER READY ===");
        }

        private void SubscribeToEvents()
        {
            // Subscribe to system events
            if (Health.HealthSystem.Instance != null)
            {
                Health.HealthSystem.Instance.OnEntityDied += OnEntityDied;
            }

            if (Map.ObjectiveSystem.Instance != null)
            {
                Map.ObjectiveSystem.Instance.OnAllObjectivesCompleted += OnAllObjectivesComplete;
            }

            if (AI.SpawnerSystem.Instance != null)
            {
                AI.SpawnerSystem.Instance.OnWaveStarted += OnWaveStarted;
                AI.SpawnerSystem.Instance.OnWaveCompleted += OnWaveCompleted;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void ChangeGameStateServerRpc(GameState newState, ServerRpcParams rpcParams = default)
        {
            ChangeGameState(newState);
        }

        private void ChangeGameState(GameState newState)
        {
            if (!IsServer) return;

            GameState oldState = currentState.Value;
            currentState.Value = newState;

            OnGameStateChanged?.Invoke(oldState, newState);
            ChangeGameStateClientRpc(oldState, newState);

            Debug.Log($"Game State Changed: {oldState} -> {newState}");

            // Handle state transitions
            HandleStateTransition(newState);
        }

        private void HandleStateTransition(GameState newState)
        {
            switch (newState)
            {
                case GameState.MainMenu:
                    Time.timeScale = 1f;
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                    break;

                case GameState.Lobby:
                    PrepareForGame();
                    break;

                case GameState.Loading:
                    // Loading screen would be shown
                    break;

                case GameState.Playing:
                    StartGame();
                    break;

                case GameState.Paused:
                    Time.timeScale = 0f;
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                    break;

                case GameState.GameOver:
                    EndGame();
                    break;
            }
        }

        private void PrepareForGame()
        {
            Debug.Log("Preparing game...");
            // Initialize all systems
            InitializeAllSystems();
        }

        private void StartGame()
        {
            Debug.Log("=== GAME STARTED ===");
            
            gameTime.Value = 0f;
            Time.timeScale = 1f;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            OnGameStarted?.Invoke();
            GameStartedClientRpc();

            // Start spawning zombies
            if (AI.SpawnerSystem.Instance != null)
            {
                // Spawner will start automatically
            }
        }

        private void EndGame()
        {
            Debug.Log("=== GAME ENDED ===");
            
            Time.timeScale = 0f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            OnGameEnded?.Invoke();
            GameEndedClientRpc();
        }

        private void InitializeAllSystems()
        {
            Debug.Log("Initializing all game systems...");

            // Initialize each system for all connected players
            foreach (var client in NetworkManager.Singleton.ConnectedClients)
            {
                ulong playerId = client.Key;
                
                Health.HealthSystem.Instance?.InitializeEntityHealthServerRpc(playerId, 100f, 50f, 0f, true);
                Player.StaminaSystem.Instance?.InitializePlayerStaminaServerRpc(playerId);
                Save.SaveSystem.Instance?.InitializePlayerSaveServerRpc(playerId);
                Settings.SettingsSystem.Instance?.InitializePlayerSettingsServerRpc(playerId);
                Analytics.AnalyticsSystem.Instance?.InitializePlayerAnalyticsServerRpc(playerId, "PC", "Desktop");
                
                Debug.Log($"Initialized systems for player {playerId}");
            }

            Debug.Log("All systems initialized!");
        }

        private void OnEntityDied(ulong victimId, ulong killerId)
        {
            Debug.Log($"Entity {victimId} killed by {killerId}");

            // Check if all players are dead
            if (AreAllPlayersDead())
            {
                TriggerDefeat("All players have died");
            }
        }

        private void OnAllObjectivesComplete()
        {
            Debug.Log("All objectives completed!");
            TriggerVictory("All objectives completed!");
        }

        private void OnWaveStarted(int wave)
        {
            Debug.Log($"Wave {wave} started!");
            Notifications.NotificationSystem.Instance?.SendSystemNotificationServerRpc(
                0, // Broadcast to all
                "Wave Started",
                $"Wave {wave} has begun!",
                5f
            );
        }

        private void OnWaveCompleted(int wave)
        {
            Debug.Log($"Wave {wave} completed!");
        }

        private bool AreAllPlayersDead()
        {
            foreach (var client in NetworkManager.Singleton.ConnectedClients)
            {
                if (Health.HealthSystem.Instance?.IsEntityAlive(client.Key) == true)
                {
                    return false;
                }
            }
            return true;
        }

        public void TriggerVictory(string reason)
        {
            if (!IsServer) return;

            Debug.Log($"VICTORY: {reason}");
            OnVictory?.Invoke(reason);
            VictoryClientRpc(reason);
            
            StartCoroutine(EndGameAfterDelay(10f));
        }

        public void TriggerDefeat(string reason)
        {
            if (!IsServer) return;

            Debug.Log($"DEFEAT: {reason}");
            OnDefeat?.Invoke(reason);
            DefeatClientRpc(reason);
            
            StartCoroutine(EndGameAfterDelay(10f));
        }

        private IEnumerator EndGameAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            ChangeGameState(GameState.GameOver);
        }

        [ClientRpc]
        private void ChangeGameStateClientRpc(GameState oldState, GameState newState)
        {
            OnGameStateChanged?.Invoke(oldState, newState);
        }

        [ClientRpc]
        private void GameStartedClientRpc()
        {
            OnGameStarted?.Invoke();
        }

        [ClientRpc]
        private void GameEndedClientRpc()
        {
            OnGameEnded?.Invoke();
        }

        [ClientRpc]
        private void VictoryClientRpc(string reason)
        {
            OnVictory?.Invoke(reason);
        }

        [ClientRpc]
        private void DefeatClientRpc(string reason)
        {
            OnDefeat?.Invoke(reason);
        }

        // Public getters
        public GameState CurrentState => currentState.Value;
        public float GameTime => gameTime.Value;
        public bool IsGamePlaying => currentState.Value == GameState.Playing;
    }

    public enum GameState
    {
        MainMenu,
        Lobby,
        Loading,
        Playing,
        Paused,
        GameOver
    }
}
