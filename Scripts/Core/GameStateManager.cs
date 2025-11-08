using UnityEngine;
using Unity.Netcode;

namespace DeadFrontier.Core
{
    /// <summary>
    /// Central game state manager that orchestrates all game systems
    /// Ensures all systems are compatible and work together seamlessly
    /// </summary>
    public class GameStateManager : NetworkBehaviour
    {
        [Header("System References")]
        [SerializeField] private bool autoInitializeSystems = true;
        [SerializeField] private bool showDebugLogs = true;

        // System status
        private bool systemsInitialized = false;
        private bool isTransitioning = false;

        // Game state
        private NetworkVariable<GameState> currentState = new NetworkVariable<GameState>(GameState.Initialization);
        private GameState previousState = GameState.Initialization;

        // Events
        public event System.Action<GameState, GameState> OnStateChanged;
        public event System.Action OnSystemsInitialized;

        // Singleton
        private static GameStateManager instance;
        public static GameStateManager Instance => instance;

        // Properties
        public GameState CurrentState => currentState.Value;
        public bool SystemsInitialized => systemsInitialized;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            if (autoInitializeSystems)
            {
                InitializeAllSystems();
            }
        }

        private void Start()
        {
            // Subscribe to network events
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            }

            // Transition to main menu
            ChangeState(GameState.MainMenu);
        }

        #region System Initialization

        /// <summary>
        /// Initializes all game systems in the correct order
        /// </summary>
        public void InitializeAllSystems()
        {
            if (systemsInitialized)
            {
                Debug.LogWarning("[GameStateManager] Systems already initialized");
                return;
            }

            if (showDebugLogs)
                Debug.Log("[GameStateManager] Initializing all systems...");

            // 1. Core Systems
            InitializeSaveSystem();
            InitializeSettingsSystem();
            InitializeAnalyticsSystem();

            // 2. Data Systems
            InitializeAchievementSystem();
            InitializeBattlePassSystem();
            InitializeChallengeSystem();

            // 3. Gameplay Systems
            InitializeProgressionSystem();
            InitializeLoadoutSystem();

            // 4. Network Systems (if multiplayer)
            if (NetworkManager.Singleton != null)
            {
                InitializeNetworkSystems();
            }

            systemsInitialized = true;
            OnSystemsInitialized?.Invoke();

            if (showDebugLogs)
                Debug.Log("[GameStateManager] ✓ All systems initialized successfully");
        }

        private void InitializeSaveSystem()
        {
            if (Save.SaveSystem.Instance == null)
            {
                Debug.LogError("[GameStateManager] SaveSystem not found!");
                return;
            }

            // Save system auto-loads in its Start()
            if (showDebugLogs)
                Debug.Log("  ✓ SaveSystem initialized");
        }

        private void InitializeSettingsSystem()
        {
            if (Settings.SettingsManager.Instance == null)
            {
                Debug.LogError("[GameStateManager] SettingsManager not found!");
                return;
            }

            // Settings auto-load in Awake()
            if (showDebugLogs)
                Debug.Log("  ✓ SettingsManager initialized");
        }

        private void InitializeAnalyticsSystem()
        {
            if (Analytics.AnalyticsManager.Instance == null)
            {
                Debug.LogError("[GameStateManager] AnalyticsManager not found!");
                return;
            }

            // Analytics auto-start session in Awake()
            Analytics.AnalyticsManager.Instance.TrackEvent("game_started", new System.Collections.Generic.Dictionary<string, object>
            {
                { "version", Application.version },
                { "platform", Application.platform.ToString() }
            });

            if (showDebugLogs)
                Debug.Log("  ✓ AnalyticsManager initialized");
        }

        private void InitializeAchievementSystem()
        {
            if (Achievements.AchievementManager.Instance == null)
            {
                Debug.LogError("[GameStateManager] AchievementManager not found!");
                return;
            }

            // Achievement system auto-loads in Awake()
            if (showDebugLogs)
                Debug.Log("  ✓ AchievementManager initialized");
        }

        private void InitializeBattlePassSystem()
        {
            if (Progression.BattlePass.BattlePassManager.Instance == null)
            {
                Debug.LogWarning("[GameStateManager] BattlePassManager not found");
                return;
            }

            // Battle pass auto-loads in Awake()
            if (showDebugLogs)
                Debug.Log("  ✓ BattlePassManager initialized");
        }

        private void InitializeChallengeSystem()
        {
            if (Gameplay.Challenges.ChallengeManager.Instance == null)
            {
                Debug.LogWarning("[GameStateManager] ChallengeManager not found");
                return;
            }

            // Challenge system auto-loads in Awake()
            if (showDebugLogs)
                Debug.Log("  ✓ ChallengeManager initialized");
        }

        private void InitializeProgressionSystem()
        {
            // Player progression is per-player, initialized on spawn
            if (showDebugLogs)
                Debug.Log("  ✓ Progression system ready");
        }

        private void InitializeLoadoutSystem()
        {
            // Loadout system is per-player, initialized on spawn
            if (showDebugLogs)
                Debug.Log("  ✓ Loadout system ready");
        }

        private void InitializeNetworkSystems()
        {
            if (Networking.NetworkBootstrap.Instance != null)
            {
                // Network bootstrap handles Unity Gaming Services
                if (showDebugLogs)
                    Debug.Log("  ✓ Network systems ready");
            }
        }

        #endregion

        #region State Management

        /// <summary>
        /// Changes the game state (server-authoritative in multiplayer)
        /// </summary>
        public void ChangeState(GameState newState)
        {
            if (isTransitioning)
            {
                Debug.LogWarning($"[GameStateManager] Already transitioning, ignoring state change to {newState}");
                return;
            }

            if (IsServer)
            {
                ChangeStateServerRpc(newState);
            }
            else if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsConnectedClient)
            {
                // Single-player or not connected - change directly
                PerformStateChange(newState);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void ChangeStateServerRpc(GameState newState)
        {
            currentState.Value = newState;
            PerformStateChange(newState);
        }

        private void PerformStateChange(GameState newState)
        {
            if (currentState.Value == newState)
                return;

            isTransitioning = true;
            previousState = currentState.Value;

            if (showDebugLogs)
                Debug.Log($"[GameStateManager] State: {previousState} → {newState}");

            // Exit previous state
            OnStateExit(previousState);

            // Change state
            currentState.Value = newState;

            // Enter new state
            OnStateEnter(newState);

            // Notify listeners
            OnStateChanged?.Invoke(previousState, newState);

            // Track analytics
            Analytics.AnalyticsManager.Instance?.TrackEvent("state_changed", new System.Collections.Generic.Dictionary<string, object>
            {
                { "from", previousState.ToString() },
                { "to", newState.ToString() }
            });

            isTransitioning = false;
        }

        private void OnStateEnter(GameState state)
        {
            switch (state)
            {
                case GameState.Initialization:
                    // Systems initializing
                    break;

                case GameState.MainMenu:
                    HandleMainMenuEnter();
                    break;

                case GameState.Lobby:
                    HandleLobbyEnter();
                    break;

                case GameState.Loading:
                    HandleLoadingEnter();
                    break;

                case GameState.InMatch:
                    HandleInMatchEnter();
                    break;

                case GameState.Paused:
                    HandlePausedEnter();
                    break;

                case GameState.MatchEnd:
                    HandleMatchEndEnter();
                    break;
            }
        }

        private void OnStateExit(GameState state)
        {
            switch (state)
            {
                case GameState.MainMenu:
                    // Cleanup menu
                    break;

                case GameState.InMatch:
                    HandleInMatchExit();
                    break;

                case GameState.Paused:
                    HandlePausedExit();
                    break;
            }
        }

        #endregion

        #region State Handlers

        private void HandleMainMenuEnter()
        {
            Time.timeScale = 1f;

            // Show main menu UI
            if (UIManager.Instance != null)
            {
                // UIManager.Instance.ShowMainMenu();
            }

            // Stop any match music
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopMusic();
            }

            if (showDebugLogs)
                Debug.Log("  → Main Menu active");
        }

        private void HandleLobbyEnter()
        {
            // Show lobby UI
            if (UIManager.Instance != null)
            {
                // UIManager.Instance.ShowLobby();
            }

            if (showDebugLogs)
                Debug.Log("  → Lobby active");
        }

        private void HandleLoadingEnter()
        {
            // Show loading screen
            if (UIManager.Instance != null)
            {
                // UIManager.Instance.ShowLoadingScreen();
            }

            if (showDebugLogs)
                Debug.Log("  → Loading...");
        }

        private void HandleInMatchEnter()
        {
            Time.timeScale = 1f;

            // Show HUD
            if (UIManager.Instance != null)
            {
                // UIManager.Instance.ShowHUD();
            }

            // Start match music
            if (AudioManager.Instance != null)
            {
                // AudioManager.Instance.PlayMatchMusic();
            }

            // Track match start
            Analytics.AnalyticsManager.Instance?.TrackMatchStart("Extraction", "Downtown", NetworkManager.Singleton?.ConnectedClients.Count ?? 1);

            if (showDebugLogs)
                Debug.Log("  → Match started");
        }

        private void HandleInMatchExit()
        {
            // Save progress
            Save.SaveSystem.Instance?.SaveGame(Save.SaveSystem.Instance.CurrentSlot);

            if (showDebugLogs)
                Debug.Log("  ← Match ended");
        }

        private void HandlePausedEnter()
        {
            Time.timeScale = 0f;

            // Show pause menu
            if (UIManager.Instance != null)
            {
                // UIManager.Instance.ShowPauseMenu();
            }

            if (showDebugLogs)
                Debug.Log("  → Game paused");
        }

        private void HandlePausedExit()
        {
            Time.timeScale = 1f;

            // Hide pause menu
            if (UIManager.Instance != null)
            {
                // UIManager.Instance.HidePauseMenu();
            }

            if (showDebugLogs)
                Debug.Log("  ← Game resumed");
        }

        private void HandleMatchEndEnter()
        {
            // Show match results
            if (UIManager.Instance != null)
            {
                // UIManager.Instance.ShowMatchResults();
            }

            // Save progress
            Save.SaveSystem.Instance?.SaveGame(Save.SaveSystem.Instance.CurrentSlot);

            if (showDebugLogs)
                Debug.Log("  → Match results");
        }

        #endregion

        #region Network Callbacks

        private void OnClientConnected(ulong clientId)
        {
            if (showDebugLogs)
                Debug.Log($"[GameStateManager] Client connected: {clientId}");
        }

        private void OnClientDisconnected(ulong clientId)
        {
            if (showDebugLogs)
                Debug.Log($"[GameStateManager] Client disconnected: {clientId}");

            // Handle player leaving
            if (currentState.Value == GameState.InMatch)
            {
                // Update player count, check if match should end, etc.
            }
        }

        #endregion

        #region Pause/Resume

        /// <summary>
        /// Pauses the game
        /// </summary>
        public void PauseGame()
        {
            if (currentState.Value == GameState.InMatch)
            {
                ChangeState(GameState.Paused);
            }
        }

        /// <summary>
        /// Resumes the game
        /// </summary>
        public void ResumeGame()
        {
            if (currentState.Value == GameState.Paused)
            {
                ChangeState(previousState);
            }
        }

        #endregion

        #region Utility

        /// <summary>
        /// Checks if game is currently in an active match
        /// </summary>
        public bool IsInMatch()
        {
            return currentState.Value == GameState.InMatch || currentState.Value == GameState.Paused;
        }

        /// <summary>
        /// Checks if game is in a menu state
        /// </summary>
        public bool IsInMenu()
        {
            return currentState.Value == GameState.MainMenu || currentState.Value == GameState.Lobby;
        }

        /// <summary>
        /// Gets all active systems status
        /// </summary>
        public System.Collections.Generic.Dictionary<string, bool> GetSystemsStatus()
        {
            return new System.Collections.Generic.Dictionary<string, bool>
            {
                { "SaveSystem", Save.SaveSystem.Instance != null },
                { "SettingsManager", Settings.SettingsManager.Instance != null },
                { "AnalyticsManager", Analytics.AnalyticsManager.Instance != null },
                { "AchievementManager", Achievements.AchievementManager.Instance != null },
                { "BattlePassManager", Progression.BattlePass.BattlePassManager.Instance != null },
                { "ChallengeManager", Gameplay.Challenges.ChallengeManager.Instance != null },
                { "GameManager", GameManager.Instance != null },
                { "UIManager", UIManager.Instance != null },
                { "AudioManager", AudioManager.Instance != null },
                { "PoolManager", PoolManager.Instance != null },
                { "ZombieManager", Zombies.ZombieManager.Instance != null }
            };
        }

        #endregion

        private void OnDestroy()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            }
        }

        private void OnApplicationQuit()
        {
            // Ensure everything is saved
            Save.SaveSystem.Instance?.SaveGame(Save.SaveSystem.Instance.CurrentSlot);

            // Track session end
            Analytics.AnalyticsManager.Instance?.TrackEvent("game_quit");
        }
    }

    /// <summary>
    /// Game state enum
    /// </summary>
    public enum GameState
    {
        Initialization,
        MainMenu,
        Lobby,
        Loading,
        InMatch,
        Paused,
        MatchEnd
    }
}
