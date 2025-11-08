using UnityEngine;
using System;

namespace DeadFrontier.Core
{
    /// <summary>
    /// Main game manager that controls game state and flow
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {
        [Header("Game State")]
        [SerializeField] private GameState currentState = GameState.MainMenu;

        [Header("Match Settings")]
        [SerializeField] private float matchDuration = Constants.MATCH_DURATION;
        [SerializeField] private float extractionDuration = Constants.EXTRACTION_DURATION;

        // Events
        public event Action<GameState> OnGameStateChanged;
        public event Action OnMatchStarted;
        public event Action OnMatchEnded;
        public event Action OnExtractionStarted;

        // Properties
        public GameState CurrentState => currentState;
        public float MatchTime { get; private set; }
        public float ExtractionTime { get; private set; }
        public bool IsMatchActive => currentState == GameState.InMatch;
        public bool IsExtractionActive => currentState == GameState.Extraction;

        protected override void Awake()
        {
            base.Awake();
            Debug.Log("[GameManager] Initialized");
        }

        private void Update()
        {
            UpdateGameState();
        }

        /// <summary>
        /// Changes the game state
        /// </summary>
        public void ChangeState(GameState newState)
        {
            if (currentState == newState)
                return;

            GameState previousState = currentState;
            currentState = newState;

            Debug.Log($"[GameManager] State changed: {previousState} → {newState}");
            OnGameStateChanged?.Invoke(newState);

            HandleStateChange(newState);
        }

        /// <summary>
        /// Starts a new match
        /// </summary>
        public void StartMatch()
        {
            MatchTime = 0f;
            ChangeState(GameState.InMatch);
            OnMatchStarted?.Invoke();
            Debug.Log("[GameManager] Match started");
        }

        /// <summary>
        /// Ends the current match
        /// </summary>
        public void EndMatch()
        {
            ChangeState(GameState.MatchEnd);
            OnMatchEnded?.Invoke();
            Debug.Log("[GameManager] Match ended");
        }

        /// <summary>
        /// Starts the extraction phase
        /// </summary>
        public void StartExtraction()
        {
            ExtractionTime = 0f;
            ChangeState(GameState.Extraction);
            OnExtractionStarted?.Invoke();
            Debug.Log("[GameManager] Extraction started");
        }

        /// <summary>
        /// Player successfully extracted
        /// </summary>
        public void PlayerExtracted(ulong playerId)
        {
            Debug.Log($"[GameManager] Player {playerId} extracted successfully");
            // TODO: Handle player extraction (save loot, update stats)
        }

        /// <summary>
        /// Player died
        /// </summary>
        public void PlayerDied(ulong playerId)
        {
            Debug.Log($"[GameManager] Player {playerId} died");
            // TODO: Handle player death (lose loot, respawn logic)
        }

        private void UpdateGameState()
        {
            switch (currentState)
            {
                case GameState.InMatch:
                    MatchTime += Time.deltaTime;

                    // Check if extraction should start
                    if (MatchTime >= matchDuration - extractionDuration)
                    {
                        StartExtraction();
                    }
                    break;

                case GameState.Extraction:
                    ExtractionTime += Time.deltaTime;

                    // Check if extraction time is up
                    if (ExtractionTime >= extractionDuration)
                    {
                        EndMatch();
                    }
                    break;
            }
        }

        private void HandleStateChange(GameState newState)
        {
            switch (newState)
            {
                case GameState.MainMenu:
                    Time.timeScale = 1f;
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    break;

                case GameState.Lobby:
                    Time.timeScale = 1f;
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    break;

                case GameState.Loading:
                    Time.timeScale = 1f;
                    break;

                case GameState.InMatch:
                    Time.timeScale = 1f;
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                    break;

                case GameState.Extraction:
                    // Keep gameplay active during extraction
                    break;

                case GameState.MatchEnd:
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    break;

                case GameState.Paused:
                    Time.timeScale = 0f;
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    break;
            }
        }

        /// <summary>
        /// Pauses the game
        /// </summary>
        public void PauseGame()
        {
            if (currentState == GameState.InMatch || currentState == GameState.Extraction)
            {
                ChangeState(GameState.Paused);
            }
        }

        /// <summary>
        /// Resumes the game from pause
        /// </summary>
        public void ResumeGame()
        {
            if (currentState == GameState.Paused)
            {
                ChangeState(GameState.InMatch);
            }
        }

        /// <summary>
        /// Quits the application
        /// </summary>
        public void QuitGame()
        {
            Debug.Log("[GameManager] Quitting game");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    /// <summary>
    /// Possible game states
    /// </summary>
    public enum GameState
    {
        MainMenu,
        Lobby,
        Loading,
        InMatch,
        Extraction,
        MatchEnd,
        Paused
    }
}
