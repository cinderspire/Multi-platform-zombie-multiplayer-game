using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace DeadFrontier.Core.Input
{
    /// <summary>
    /// Manages mobile touch controls with virtual joystick and buttons
    /// Automatically enables/disables based on platform
    /// </summary>
    public class MobileControlsManager : Singleton<MobileControlsManager>
    {
        [Header("Control References")]
        [SerializeField] private GameObject mobileControlsRoot;
        [SerializeField] private VirtualJoystick movementJoystick;
        [SerializeField] private VirtualJoystick lookJoystick;
        [SerializeField] private Button fireButton;
        [SerializeField] private Button aimButton;
        [SerializeField] private Button reloadButton;
        [SerializeField] private Button jumpButton;
        [SerializeField] private Button crouchButton;
        [SerializeField] private Button interactButton;
        [SerializeField] private Button sprintButton;

        [Header("Settings")]
        [SerializeField] private bool autoDetectPlatform = true;
        [SerializeField] private bool enableOnMobile = true;
        [SerializeField] private bool enableOnTablet = true;
        [SerializeField] private bool enableOnDesktop = false;

        [Header("Joystick Settings")]
        [SerializeField] private float joystickSensitivity = 1.5f;
        [SerializeField] private float lookSensitivity = 2.0f;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = false;
        [SerializeField] private bool forceEnableInEditor = true;

        // Input state
        private Vector2 movementInput;
        private Vector2 lookInput;
        private bool isFiring;
        private bool isAiming;
        private bool jumpPressed;
        private bool crouchPressed;
        private bool interactPressed;
        private bool reloadPressed;
        private bool sprintPressed;

        // Platform detection
        private bool isMobilePlatform;

        protected override void Awake()
        {
            base.Awake();

            DetectPlatform();
            InitializeControls();
        }

        private void Start()
        {
            SetupButtonListeners();
        }

        private void Update()
        {
            // Update joystick inputs
            if (mobileControlsRoot != null && mobileControlsRoot.activeSelf)
            {
                UpdateJoystickInputs();
            }
        }

        #region Platform Detection

        private void DetectPlatform()
        {
#if UNITY_EDITOR
            if (forceEnableInEditor)
            {
                isMobilePlatform = true;
                if (showDebugLogs)
                    Debug.Log("[MobileControlsManager] Force enabled in editor");
                return;
            }
#endif

            if (!autoDetectPlatform)
            {
                isMobilePlatform = enableOnMobile || enableOnTablet;
                return;
            }

            // Detect platform
#if UNITY_ANDROID || UNITY_IOS
            isMobilePlatform = true;
#elif UNITY_STANDALONE || UNITY_WEBGL
            isMobilePlatform = false;
#else
            isMobilePlatform = UnityEngine.Application.isMobilePlatform;
#endif

            // Check for tablet
            if (isMobilePlatform && !enableOnTablet)
            {
                // Simple tablet detection based on screen size
                float diagonalInches = GetScreenDiagonalInches();
                if (diagonalInches >= 7f)
                {
                    isMobilePlatform = enableOnTablet;
                }
            }

            if (showDebugLogs)
                Debug.Log($"[MobileControlsManager] Platform: {UnityEngine.Application.platform}, Mobile controls: {isMobilePlatform}");
        }

        private float GetScreenDiagonalInches()
        {
            float dpi = Screen.dpi > 0 ? Screen.dpi : 160f; // Default to 160 if not available
            float width = Screen.width / dpi;
            float height = Screen.height / dpi;
            return Mathf.Sqrt(width * width + height * height);
        }

        #endregion

        #region Initialization

        private void InitializeControls()
        {
            if (mobileControlsRoot != null)
            {
                mobileControlsRoot.SetActive(isMobilePlatform);

                if (showDebugLogs)
                    Debug.Log($"[MobileControlsManager] Mobile controls {(isMobilePlatform ? "enabled" : "disabled")}");
            }

            // Lock cursor for desktop
            if (!isMobilePlatform)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void SetupButtonListeners()
        {
            if (!isMobilePlatform)
                return;

            // Fire button (hold to fire)
            if (fireButton != null)
            {
                var eventTrigger = fireButton.gameObject.AddComponent<EventTrigger>();

                var pointerDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
                pointerDown.callback.AddListener((data) => { isFiring = true; });
                eventTrigger.triggers.Add(pointerDown);

                var pointerUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
                pointerUp.callback.AddListener((data) => { isFiring = false; });
                eventTrigger.triggers.Add(pointerUp);
            }

            // Aim button (hold to aim)
            if (aimButton != null)
            {
                var eventTrigger = aimButton.gameObject.AddComponent<EventTrigger>();

                var pointerDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
                pointerDown.callback.AddListener((data) => { isAiming = true; });
                eventTrigger.triggers.Add(pointerDown);

                var pointerUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
                pointerUp.callback.AddListener((data) => { isAiming = false; });
                eventTrigger.triggers.Add(pointerUp);
            }

            // Reload button (tap)
            if (reloadButton != null)
            {
                reloadButton.onClick.AddListener(() =>
                {
                    reloadPressed = true;
                    Invoke(nameof(ResetReload), 0.1f);
                });
            }

            // Jump button (tap)
            if (jumpButton != null)
            {
                jumpButton.onClick.AddListener(() =>
                {
                    jumpPressed = true;
                    Invoke(nameof(ResetJump), 0.1f);
                });
            }

            // Crouch button (toggle)
            if (crouchButton != null)
            {
                crouchButton.onClick.AddListener(() =>
                {
                    crouchPressed = !crouchPressed;
                });
            }

            // Interact button (tap)
            if (interactButton != null)
            {
                interactButton.onClick.AddListener(() =>
                {
                    interactPressed = true;
                    Invoke(nameof(ResetInteract), 0.1f);
                });
            }

            // Sprint button (toggle)
            if (sprintButton != null)
            {
                sprintButton.onClick.AddListener(() =>
                {
                    sprintPressed = !sprintPressed;
                });
            }
        }

        private void ResetReload() { reloadPressed = false; }
        private void ResetJump() { jumpPressed = false; }
        private void ResetInteract() { interactPressed = false; }

        #endregion

        #region Input Updates

        private void UpdateJoystickInputs()
        {
            if (movementJoystick != null)
            {
                movementInput = movementJoystick.GetInput() * joystickSensitivity;
            }

            if (lookJoystick != null)
            {
                lookInput = lookJoystick.GetInput() * lookSensitivity;
            }
        }

        #endregion

        #region Public API

        public Vector2 GetMovementInput()
        {
            if (!isMobilePlatform)
                return Vector2.zero;

            return movementInput;
        }

        public Vector2 GetLookInput()
        {
            if (!isMobilePlatform)
                return Vector2.zero;

            return lookInput;
        }

        public bool IsFiring => isMobilePlatform && isFiring;
        public bool IsAiming => isMobilePlatform && isAiming;
        public bool GetJumpPressed()
        {
            if (!isMobilePlatform) return false;
            bool pressed = jumpPressed;
            jumpPressed = false;
            return pressed;
        }

        public bool IsCrouching => isMobilePlatform && crouchPressed;
        public bool IsSprinting => isMobilePlatform && sprintPressed;

        public bool GetInteractPressed()
        {
            if (!isMobilePlatform) return false;
            bool pressed = interactPressed;
            interactPressed = false;
            return pressed;
        }

        public bool GetReloadPressed()
        {
            if (!isMobilePlatform) return false;
            bool pressed = reloadPressed;
            reloadPressed = false;
            return pressed;
        }

        public bool IsMobilePlatform => isMobilePlatform;

        public void SetJoystickSensitivity(float sensitivity)
        {
            joystickSensitivity = Mathf.Clamp(sensitivity, 0.1f, 5f);
        }

        public void SetLookSensitivity(float sensitivity)
        {
            lookSensitivity = Mathf.Clamp(sensitivity, 0.1f, 5f);
        }

        public void ShowControls()
        {
            if (mobileControlsRoot != null)
                mobileControlsRoot.SetActive(true);
        }

        public void HideControls()
        {
            if (mobileControlsRoot != null)
                mobileControlsRoot.SetActive(false);
        }

        #endregion
    }

    /// <summary>
    /// Virtual joystick component for mobile controls
    /// Attach to UI joystick elements
    /// </summary>
    public class VirtualJoystick : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Joystick Settings")]
        [SerializeField] private RectTransform joystickBackground;
        [SerializeField] private RectTransform joystickHandle;
        [SerializeField] private float handleRange = 50f;
        [SerializeField] private bool returnToCenter = true;

        private Vector2 inputVector;
        private Vector2 joystickCenter;

        private void Start()
        {
            if (joystickBackground != null)
            {
                joystickCenter = joystickBackground.position;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            Vector2 direction = eventData.position - joystickCenter;
            inputVector = (direction.magnitude > handleRange) ? direction.normalized : direction / handleRange;

            // Move handle
            if (joystickHandle != null)
            {
                joystickHandle.anchoredPosition = inputVector * handleRange;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            inputVector = Vector2.zero;

            // Return handle to center
            if (joystickHandle != null && returnToCenter)
            {
                joystickHandle.anchoredPosition = Vector2.zero;
            }
        }

        public Vector2 GetInput()
        {
            return inputVector;
        }
    }
}
