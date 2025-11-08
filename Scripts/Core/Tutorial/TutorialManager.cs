using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

namespace DeadFrontier.Core.Tutorial
{
    /// <summary>
    /// Manages interactive tutorial system for onboarding new players
    /// Provides step-by-step guidance with highlighting and objectives
    /// </summary>
    public class TutorialManager : Singleton<TutorialManager>
    {
        [Header("Tutorial Settings")]
        [SerializeField] private bool autoStartOnFirstLaunch = true;
        [SerializeField] private bool skipTutorialInEditor = false;
        [SerializeField] private List<TutorialStep> tutorialSteps = new List<TutorialStep>();

        [Header("UI References")]
        [SerializeField] private GameObject tutorialPanel;
        [SerializeField] private TextMeshProUGUI tutorialTitleText;
        [SerializeField] private TextMeshProUGUI tutorialDescriptionText;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button skipButton;
        [SerializeField] private Image highlightOverlay;
        [SerializeField] private GameObject pointerArrow;

        [Header("Animation Settings")]
        [SerializeField] private float fadeInDuration = 0.3f;
        [SerializeField] private float fadeOutDuration = 0.3f;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        // State
        private bool isTutorialActive = false;
        private bool isTutorialCompleted = false;
        private int currentStepIndex = 0;
        private TutorialStep currentStep;
        private CanvasGroup panelCanvasGroup;

        // Events
        public event System.Action OnTutorialStarted;
        public event System.Action OnTutorialCompleted;
        public event System.Action OnTutorialSkipped;
        public event System.Action<int> OnStepCompleted;

        protected override void Awake()
        {
            base.Awake();

            if (tutorialPanel != null)
            {
                panelCanvasGroup = tutorialPanel.GetComponent<CanvasGroup>();
                if (panelCanvasGroup == null)
                {
                    panelCanvasGroup = tutorialPanel.AddComponent<CanvasGroup>();
                }
                tutorialPanel.SetActive(false);
            }

            SetupButtons();
            LoadTutorialProgress();
        }

        private void Start()
        {
#if UNITY_EDITOR
            if (skipTutorialInEditor)
            {
                if (showDebugLogs)
                    Debug.Log("[TutorialManager] Skipping tutorial in editor");
                return;
            }
#endif

            if (autoStartOnFirstLaunch && !isTutorialCompleted)
            {
                // Small delay to let other systems initialize
                Invoke(nameof(StartTutorial), 1f);
            }
        }

        #region Tutorial Flow

        /// <summary>
        /// Starts the tutorial from the beginning
        /// </summary>
        public void StartTutorial()
        {
            if (isTutorialActive)
            {
                if (showDebugLogs)
                    Debug.LogWarning("[TutorialManager] Tutorial already active");
                return;
            }

            if (tutorialSteps.Count == 0)
            {
                if (showDebugLogs)
                    Debug.LogWarning("[TutorialManager] No tutorial steps defined");
                return;
            }

            isTutorialActive = true;
            currentStepIndex = 0;

            if (showDebugLogs)
                Debug.Log("[TutorialManager] Starting tutorial");

            OnTutorialStarted?.Invoke();

            // Track analytics
            Analytics.AnalyticsManager.Instance?.TrackEvent("tutorial_started", null);

            StartCoroutine(ShowTutorialCoroutine());
        }

        private IEnumerator ShowTutorialCoroutine()
        {
            // Show tutorial panel
            if (tutorialPanel != null)
            {
                tutorialPanel.SetActive(true);
                yield return StartCoroutine(FadeIn());
            }

            // Show first step
            ShowStep(currentStepIndex);
        }

        private void ShowStep(int stepIndex)
        {
            if (stepIndex < 0 || stepIndex >= tutorialSteps.Count)
            {
                CompleteTutorial();
                return;
            }

            currentStep = tutorialSteps[stepIndex];

            if (showDebugLogs)
                Debug.Log($"[TutorialManager] Showing step {stepIndex + 1}/{tutorialSteps.Count}: {currentStep.title}");

            // Update UI
            if (tutorialTitleText != null)
                tutorialTitleText.text = currentStep.title;

            if (tutorialDescriptionText != null)
                tutorialDescriptionText.text = currentStep.description;

            // Handle highlighting
            if (highlightOverlay != null)
            {
                highlightOverlay.gameObject.SetActive(currentStep.highlightUIElement != null);
            }

            // Position pointer arrow
            if (pointerArrow != null && currentStep.highlightUIElement != null)
            {
                pointerArrow.SetActive(true);
                PositionPointerAtUI(currentStep.highlightUIElement);
            }
            else if (pointerArrow != null)
            {
                pointerArrow.SetActive(false);
            }

            // Handle step type
            switch (currentStep.stepType)
            {
                case TutorialStepType.Message:
                    // Just show message, wait for next button
                    if (nextButton != null)
                        nextButton.gameObject.SetActive(true);
                    break;

                case TutorialStepType.WaitForAction:
                    // Hide next button, wait for action
                    if (nextButton != null)
                        nextButton.gameObject.SetActive(false);
                    StartCoroutine(WaitForActionCoroutine());
                    break;

                case TutorialStepType.Automatic:
                    // Auto-proceed after delay
                    if (nextButton != null)
                        nextButton.gameObject.SetActive(false);
                    StartCoroutine(AutoProceedCoroutine());
                    break;
            }

            // Trigger step event
            currentStep.onStepStarted?.Invoke();
        }

        private IEnumerator WaitForActionCoroutine()
        {
            bool actionCompleted = false;

            while (!actionCompleted)
            {
                // Check custom condition
                if (currentStep.actionCondition != null && currentStep.actionCondition.Invoke())
                {
                    actionCompleted = true;
                }

                yield return null;
            }

            if (showDebugLogs)
                Debug.Log($"[TutorialManager] Action completed for step {currentStepIndex + 1}");

            yield return new WaitForSeconds(0.5f); // Small delay before next step
            NextStep();
        }

        private IEnumerator AutoProceedCoroutine()
        {
            yield return new WaitForSeconds(currentStep.autoProceedDelay);
            NextStep();
        }

        /// <summary>
        /// Proceeds to next tutorial step
        /// </summary>
        public void NextStep()
        {
            if (!isTutorialActive)
                return;

            // Trigger step completed event
            currentStep.onStepCompleted?.Invoke();
            OnStepCompleted?.Invoke(currentStepIndex);

            // Track analytics
            Analytics.AnalyticsManager.Instance?.TrackEvent("tutorial_step_completed", new Dictionary<string, object>
            {
                { "step_index", currentStepIndex },
                { "step_title", currentStep.title }
            });

            currentStepIndex++;

            if (currentStepIndex < tutorialSteps.Count)
            {
                ShowStep(currentStepIndex);
            }
            else
            {
                CompleteTutorial();
            }
        }

        /// <summary>
        /// Skips the tutorial
        /// </summary>
        public void SkipTutorial()
        {
            if (!isTutorialActive)
                return;

            if (showDebugLogs)
                Debug.Log("[TutorialManager] Tutorial skipped");

            StopAllCoroutines();

            OnTutorialSkipped?.Invoke();

            // Track analytics
            Analytics.AnalyticsManager.Instance?.TrackEvent("tutorial_skipped", new Dictionary<string, object>
            {
                { "step_reached", currentStepIndex },
                { "total_steps", tutorialSteps.Count }
            });

            StartCoroutine(HideTutorialCoroutine());
        }

        private void CompleteTutorial()
        {
            if (showDebugLogs)
                Debug.Log("[TutorialManager] Tutorial completed");

            isTutorialCompleted = true;
            SaveTutorialProgress();

            OnTutorialCompleted?.Invoke();

            // Track analytics
            Analytics.AnalyticsManager.Instance?.TrackEvent("tutorial_completed", null);

            StartCoroutine(HideTutorialCoroutine());
        }

        private IEnumerator HideTutorialCoroutine()
        {
            if (tutorialPanel != null)
            {
                yield return StartCoroutine(FadeOut());
                tutorialPanel.SetActive(false);
            }

            isTutorialActive = false;
        }

        #endregion

        #region UI Helpers

        private void SetupButtons()
        {
            if (nextButton != null)
            {
                nextButton.onClick.AddListener(NextStep);
            }

            if (skipButton != null)
            {
                skipButton.onClick.AddListener(SkipTutorial);
            }
        }

        private void PositionPointerAtUI(RectTransform target)
        {
            if (pointerArrow == null || target == null)
                return;

            // Position pointer near the target UI element
            RectTransform pointerRect = pointerArrow.GetComponent<RectTransform>();
            if (pointerRect != null)
            {
                pointerRect.position = target.position;
                // Could add offset or rotation based on target position
            }
        }

        private IEnumerator FadeIn()
        {
            if (panelCanvasGroup == null)
                yield break;

            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                panelCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
                yield return null;
            }

            panelCanvasGroup.alpha = 1f;
        }

        private IEnumerator FadeOut()
        {
            if (panelCanvasGroup == null)
                yield break;

            float elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                panelCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
                yield return null;
            }

            panelCanvasGroup.alpha = 0f;
        }

        #endregion

        #region Save/Load

        private void LoadTutorialProgress()
        {
            // Check PlayerPrefs for tutorial completion
            isTutorialCompleted = PlayerPrefs.GetInt("TutorialCompleted", 0) == 1;

            if (showDebugLogs)
                Debug.Log($"[TutorialManager] Tutorial completed: {isTutorialCompleted}");
        }

        private void SaveTutorialProgress()
        {
            PlayerPrefs.SetInt("TutorialCompleted", 1);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Resets tutorial progress (for testing)
        /// </summary>
        public void ResetTutorialProgress()
        {
            isTutorialCompleted = false;
            PlayerPrefs.SetInt("TutorialCompleted", 0);
            PlayerPrefs.Save();

            if (showDebugLogs)
                Debug.Log("[TutorialManager] Tutorial progress reset");
        }

        #endregion

        #region Public API

        public bool IsTutorialActive => isTutorialActive;
        public bool IsTutorialCompleted => isTutorialCompleted;
        public int CurrentStepIndex => currentStepIndex;
        public int TotalSteps => tutorialSteps.Count;

        /// <summary>
        /// Marks a specific tutorial objective as completed
        /// </summary>
        public void CompleteObjective(string objectiveId)
        {
            if (currentStep != null && currentStep.objectiveId == objectiveId)
            {
                NextStep();
            }
        }

        #endregion
    }

    #region Data Structures

    public enum TutorialStepType
    {
        Message,        // Show message, wait for next button
        WaitForAction,  // Wait for player to complete action
        Automatic       // Auto-proceed after delay
    }

    [System.Serializable]
    public class TutorialStep
    {
        [Header("Content")]
        public string title;
        [TextArea(3, 5)]
        public string description;

        [Header("Step Type")]
        public TutorialStepType stepType = TutorialStepType.Message;
        public string objectiveId; // For WaitForAction type
        public float autoProceedDelay = 3f; // For Automatic type

        [Header("UI Highlighting")]
        public RectTransform highlightUIElement;

        [Header("Events")]
        public System.Action onStepStarted;
        public System.Action onStepCompleted;
        public System.Func<bool> actionCondition; // For WaitForAction type
    }

    #endregion
}
