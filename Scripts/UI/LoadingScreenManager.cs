using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

namespace DeadFrontier.UI
{
    /// <summary>
    /// Manages loading screens with smooth transitions and progress tracking
    /// Displays tips, progress bars, and handles async scene loading
    /// </summary>
    public class LoadingScreenManager : Singleton<LoadingScreenManager>
    {
        [Header("UI References")]
        [SerializeField] private GameObject loadingScreenPanel;
        [SerializeField] private Slider progressBar;
        [SerializeField] private TextMeshProUGUI loadingText;
        [SerializeField] private TextMeshProUGUI tipText;
        [SerializeField] private Image backgroundImage;

        [Header("Loading Settings")]
        [SerializeField] private float minimumLoadTime = 1f; // Minimum time to show loading screen
        [SerializeField] private float fadeInDuration = 0.3f;
        [SerializeField] private float fadeOutDuration = 0.5f;

        [Header("Tips")]
        [SerializeField] private string[] loadingTips = new string[]
        {
            "Stick together! Lone survivors don't last long.",
            "Headshots deal massive damage to zombies.",
            "Listen for audio cues - they can save your life.",
            "Extract before time runs out!",
            "Higher risk areas have better loot.",
            "Save your ammo for hordes and boss zombies.",
            "Use perks to create powerful loadout combinations.",
            "Complete daily challenges for bonus XP.",
            "Upgrade your Battle Pass for exclusive rewards.",
            "The extraction zone closes after 15 minutes.",
            "Screamers alert nearby zombies - take them out fast!",
            "Tank zombies are slow but incredibly tough.",
            "Exploders deal area damage - keep your distance!",
            "Runners are fast - aim carefully or use automatic weapons.",
            "Check your map frequently to avoid dead ends.",
            "Melee weapons are silent but risky.",
            "Healing takes time - find cover first.",
            "Sprint wisely - stamina is precious.",
            "Prestige to unlock exclusive perks and cosmetics.",
            "Communication is key in squad mode."
        };

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        // State
        private bool isLoading = false;
        private CanvasGroup canvasGroup;
        private AsyncOperation currentLoadOperation;

        protected override void Awake()
        {
            base.Awake();

            if (loadingScreenPanel != null)
            {
                canvasGroup = loadingScreenPanel.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = loadingScreenPanel.AddComponent<CanvasGroup>();
                }

                loadingScreenPanel.SetActive(false);
            }
        }

        #region Public API

        /// <summary>
        /// Shows loading screen and loads a scene asynchronously
        /// </summary>
        public void LoadScene(string sceneName)
        {
            if (isLoading)
            {
                Debug.LogWarning($"[LoadingScreenManager] Already loading a scene");
                return;
            }

            StartCoroutine(LoadSceneAsync(sceneName));
        }

        /// <summary>
        /// Shows loading screen and loads a scene by index
        /// </summary>
        public void LoadScene(int sceneIndex)
        {
            if (isLoading)
            {
                Debug.LogWarning($"[LoadingScreenManager] Already loading a scene");
                return;
            }

            StartCoroutine(LoadSceneAsync(sceneIndex));
        }

        /// <summary>
        /// Shows loading screen with custom operation
        /// </summary>
        public void ShowLoadingScreen(System.Func<IEnumerator> operation)
        {
            if (isLoading)
            {
                Debug.LogWarning($"[LoadingScreenManager] Already loading");
                return;
            }

            StartCoroutine(ShowLoadingScreenCoroutine(operation));
        }

        /// <summary>
        /// Manually shows the loading screen
        /// </summary>
        public void Show()
        {
            if (loadingScreenPanel != null)
            {
                loadingScreenPanel.SetActive(true);
                SetRandomTip();

                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 1f;
                }
            }
        }

        /// <summary>
        /// Manually hides the loading screen
        /// </summary>
        public void Hide()
        {
            if (loadingScreenPanel != null)
            {
                loadingScreenPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Updates the loading progress
        /// </summary>
        public void SetProgress(float progress)
        {
            if (progressBar != null)
            {
                progressBar.value = Mathf.Clamp01(progress);
            }
        }

        /// <summary>
        /// Updates the loading text
        /// </summary>
        public void SetLoadingText(string text)
        {
            if (loadingText != null)
            {
                loadingText.text = text;
            }
        }

        #endregion

        #region Scene Loading

        private IEnumerator LoadSceneAsync(string sceneName)
        {
            isLoading = true;

            // Show loading screen
            yield return StartCoroutine(FadeIn());

            float startTime = Time.realtimeSinceStartup;

            // Set initial text
            SetLoadingText("Loading...");
            SetProgress(0f);
            SetRandomTip();

            if (showDebugLogs)
                Debug.Log($"[LoadingScreenManager] Loading scene: {sceneName}");

            // Start loading scene
            currentLoadOperation = SceneManager.LoadSceneAsync(sceneName);
            currentLoadOperation.allowSceneActivation = false;

            // Update progress
            while (!currentLoadOperation.isDone)
            {
                // 0.9 is when loading is complete but scene hasn't activated
                float progress = Mathf.Clamp01(currentLoadOperation.progress / 0.9f);
                SetProgress(progress);

                // Check if loading is complete
                if (currentLoadOperation.progress >= 0.9f)
                {
                    // Ensure minimum load time
                    float loadTime = Time.realtimeSinceStartup - startTime;
                    if (loadTime < minimumLoadTime)
                    {
                        yield return new WaitForSecondsRealtime(minimumLoadTime - loadTime);
                    }

                    SetProgress(1f);
                    SetLoadingText("Press any key to continue");

                    // Wait for input or auto-continue after delay
                    float waitTime = 0f;
                    while (waitTime < 2f && !Input.anyKey)
                    {
                        waitTime += Time.unscaledDeltaTime;
                        yield return null;
                    }

                    // Activate scene
                    currentLoadOperation.allowSceneActivation = true;
                }

                yield return null;
            }

            if (showDebugLogs)
                Debug.Log($"[LoadingScreenManager] Scene loaded: {sceneName}");

            // Hide loading screen
            yield return StartCoroutine(FadeOut());

            isLoading = false;
        }

        private IEnumerator LoadSceneAsync(int sceneIndex)
        {
            isLoading = true;

            yield return StartCoroutine(FadeIn());

            float startTime = Time.realtimeSinceStartup;

            SetLoadingText("Loading...");
            SetProgress(0f);
            SetRandomTip();

            if (showDebugLogs)
                Debug.Log($"[LoadingScreenManager] Loading scene index: {sceneIndex}");

            currentLoadOperation = SceneManager.LoadSceneAsync(sceneIndex);
            currentLoadOperation.allowSceneActivation = false;

            while (!currentLoadOperation.isDone)
            {
                float progress = Mathf.Clamp01(currentLoadOperation.progress / 0.9f);
                SetProgress(progress);

                if (currentLoadOperation.progress >= 0.9f)
                {
                    float loadTime = Time.realtimeSinceStartup - startTime;
                    if (loadTime < minimumLoadTime)
                    {
                        yield return new WaitForSecondsRealtime(minimumLoadTime - loadTime);
                    }

                    SetProgress(1f);
                    currentLoadOperation.allowSceneActivation = true;
                }

                yield return null;
            }

            yield return StartCoroutine(FadeOut());

            isLoading = false;
        }

        #endregion

        #region Custom Operations

        private IEnumerator ShowLoadingScreenCoroutine(System.Func<IEnumerator> operation)
        {
            isLoading = true;

            yield return StartCoroutine(FadeIn());

            SetLoadingText("Loading...");
            SetProgress(0f);
            SetRandomTip();

            // Execute custom operation
            yield return StartCoroutine(operation());

            yield return StartCoroutine(FadeOut());

            isLoading = false;
        }

        #endregion

        #region Transitions

        private IEnumerator FadeIn()
        {
            if (loadingScreenPanel != null)
            {
                loadingScreenPanel.SetActive(true);

                if (canvasGroup != null)
                {
                    float elapsed = 0f;
                    while (elapsed < fadeInDuration)
                    {
                        elapsed += Time.unscaledDeltaTime;
                        canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
                        yield return null;
                    }

                    canvasGroup.alpha = 1f;
                }
            }
        }

        private IEnumerator FadeOut()
        {
            if (canvasGroup != null)
            {
                float elapsed = 0f;
                while (elapsed < fadeOutDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
                    yield return null;
                }

                canvasGroup.alpha = 0f;
            }

            if (loadingScreenPanel != null)
            {
                loadingScreenPanel.SetActive(false);
            }
        }

        #endregion

        #region Tips

        private void SetRandomTip()
        {
            if (tipText != null && loadingTips.Length > 0)
            {
                int randomIndex = Random.Range(0, loadingTips.Length);
                tipText.text = $"TIP: {loadingTips[randomIndex]}";
            }
        }

        #endregion
    }
}
