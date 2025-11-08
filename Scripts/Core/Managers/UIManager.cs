using UnityEngine;

namespace DeadFrontier.Core
{
    /// <summary>
    /// Manages all UI canvases and transitions
    /// </summary>
    public class UIManager : Singleton<UIManager>
    {
        [Header("Canvases")]
        [SerializeField] private Canvas mainMenuCanvas;
        [SerializeField] private Canvas lobbyCanvas;
        [SerializeField] private Canvas hudCanvas;
        [SerializeField] private Canvas pauseMenuCanvas;
        [SerializeField] private Canvas deathScreenCanvas;
        [SerializeField] private Canvas victoryScreenCanvas;
        [SerializeField] private Canvas loadingScreenCanvas;

        [Header("Fade Settings")]
        [SerializeField] private float fadeDuration = 0.3f;

        // Current active canvas
        private Canvas currentCanvas;

        protected override void Awake()
        {
            base.Awake();

            // Hide all canvases initially
            HideAllCanvases();
        }

        private void Start()
        {
            // Subscribe to game state changes
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;

            // Show initial canvas
            ShowCanvas(mainMenuCanvas);
        }

        private void HandleGameStateChanged(GameState newState)
        {
            switch (newState)
            {
                case GameState.MainMenu:
                    ShowCanvas(mainMenuCanvas);
                    break;

                case GameState.Lobby:
                    ShowCanvas(lobbyCanvas);
                    break;

                case GameState.Loading:
                    ShowCanvas(loadingScreenCanvas);
                    break;

                case GameState.InMatch:
                    ShowCanvas(hudCanvas);
                    break;

                case GameState.Paused:
                    ShowCanvas(pauseMenuCanvas, false); // Don't hide HUD
                    break;

                case GameState.MatchEnd:
                    // Check if player extracted or died
                    // For now, show victory screen
                    ShowCanvas(victoryScreenCanvas);
                    break;
            }
        }

        /// <summary>
        /// Shows a specific canvas and hides others
        /// </summary>
        public void ShowCanvas(Canvas canvas, bool hideOthers = true)
        {
            if (canvas == null)
            {
                Debug.LogWarning("[UIManager] Attempted to show null canvas");
                return;
            }

            if (hideOthers)
            {
                HideAllCanvases();
            }

            canvas.gameObject.SetActive(true);
            currentCanvas = canvas;

            // Fade in
            var canvasGroup = canvas.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                StartCoroutine(FadeCanvasGroup(canvasGroup, 0f, 1f, fadeDuration));
            }

            Debug.Log($"[UIManager] Showing canvas: {canvas.name}");
        }

        /// <summary>
        /// Hides a specific canvas
        /// </summary>
        public void HideCanvas(Canvas canvas)
        {
            if (canvas == null)
                return;

            var canvasGroup = canvas.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                StartCoroutine(FadeCanvasGroup(canvasGroup, 1f, 0f, fadeDuration, () =>
                {
                    canvas.gameObject.SetActive(false);
                }));
            }
            else
            {
                canvas.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Hides all canvases
        /// </summary>
        public void HideAllCanvases()
        {
            Canvas[] allCanvases = new Canvas[]
            {
                mainMenuCanvas,
                lobbyCanvas,
                hudCanvas,
                pauseMenuCanvas,
                deathScreenCanvas,
                victoryScreenCanvas,
                loadingScreenCanvas
            };

            foreach (var canvas in allCanvases)
            {
                if (canvas != null)
                {
                    canvas.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Shows death screen
        /// </summary>
        public void ShowDeathScreen()
        {
            ShowCanvas(deathScreenCanvas);
        }

        /// <summary>
        /// Shows victory screen
        /// </summary>
        public void ShowVictoryScreen()
        {
            ShowCanvas(victoryScreenCanvas);
        }

        /// <summary>
        /// Shows loading screen
        /// </summary>
        public void ShowLoadingScreen()
        {
            ShowCanvas(loadingScreenCanvas);
        }

        /// <summary>
        /// Fades a canvas group
        /// </summary>
        private System.Collections.IEnumerator FadeCanvasGroup(
            CanvasGroup canvasGroup,
            float startAlpha,
            float endAlpha,
            float duration,
            System.Action onComplete = null)
        {
            float elapsed = 0f;
            canvasGroup.alpha = startAlpha;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime; // Use unscaled for pause menu
                canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
                yield return null;
            }

            canvasGroup.alpha = endAlpha;
            onComplete?.Invoke();
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            }
        }
    }
}
