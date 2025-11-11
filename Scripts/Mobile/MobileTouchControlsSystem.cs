using UnityEngine;
using UnityEngine.EventSystems;

namespace ZombieGame
{
    /// <summary>
    /// Mobile Touch Controls System - Optimized touch input for mobile platforms
    /// Features: Virtual joysticks, touch aim, gesture controls, customizable layouts
    /// Auto-detection for iOS/Android with performance optimization
    /// </summary>
    public class MobileTouchControlsSystem : MonoBehaviour
    {
        public static MobileTouchControlsSystem Instance { get; private set; }

        [Header("Control Settings")]
        [SerializeField] private bool autoEnableOnMobile = true;
        [SerializeField] private float joystickSensitivity = 1.5f;
        [SerializeField] private float aimSensitivity = 2.0f;

        [Header("Layout Presets")]
        [SerializeField] private ControlLayout currentLayout = ControlLayout.Default;

        // Touch tracking
        private Dictionary<int, TouchData> activeTouches = new Dictionary<int, TouchData>();

        // Virtual controls
        private VirtualJoystick moveJoystick;
        private TouchAimArea aimArea;
        private Dictionary<string, VirtualButton> virtualButtons = new Dictionary<string, VirtualButton>();

        // Settings
        private bool isEnabled = false;
        private float deadZone = 0.1f;

        public enum ControlLayout
        {
            Default,        // Joystick left, aim right
            Southpaw,       // Flipped (joystick right, aim left)
            Tablet,         // Optimized for tablets
            Compact,        // Minimal UI
            Custom          // User-defined
        }

        [System.Serializable]
        public class TouchData
        {
            public int fingerId;
            public Vector2 startPosition;
            public Vector2 currentPosition;
            public float startTime;
            public TouchPhase phase;
            public string assignedControl = ""; // Which control owns this touch
        }

        [System.Serializable]
        public class VirtualJoystick
        {
            public RectTransform joystickBase;
            public RectTransform joystickHandle;
            public float radius = 50f;
            public Vector2 inputVector = Vector2.zero;
            public bool isActive = false;
            public int touchId = -1;
        }

        [System.Serializable]
        public class TouchAimArea
        {
            public RectTransform aimAreaRect;
            public Vector2 aimDelta = Vector2.zero;
            public bool isActive = false;
            public int touchId = -1;
            public Vector2 lastTouchPosition;
        }

        [System.Serializable]
        public class VirtualButton
        {
            public string buttonName;
            public RectTransform buttonRect;
            public bool isPressed = false;
            public int touchId = -1;
            public System.Action onPress;
            public System.Action onRelease;
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DetectPlatform();
                InitializeControls();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void DetectPlatform()
        {
#if UNITY_IOS || UNITY_ANDROID
            if (autoEnableOnMobile)
            {
                EnableMobileControls(true);
            }
#else
            EnableMobileControls(false);
#endif
        }

        private void InitializeControls()
        {
            // Initialize virtual joystick
            moveJoystick = new VirtualJoystick
            {
                radius = 75f
            };

            // Initialize aim area
            aimArea = new TouchAimArea();

            // Initialize virtual buttons
            RegisterButton("Fire", OnFirePress, OnFireRelease);
            RegisterButton("Reload", OnReloadPress, null);
            RegisterButton("Jump", OnJumpPress, null);
            RegisterButton("Crouch", OnCrouchPress, null);
            RegisterButton("Interact", OnInteractPress, null);
            RegisterButton("WeaponSwitch", OnWeaponSwitchPress, null);
            RegisterButton("Grenade", OnGrenadePress, null);
            RegisterButton("Ability", OnAbilityPress, null);

            ApplyLayout(currentLayout);
        }

        private void RegisterButton(string buttonName, System.Action onPress, System.Action onRelease)
        {
            virtualButtons[buttonName] = new VirtualButton
            {
                buttonName = buttonName,
                onPress = onPress,
                onRelease = onRelease
            };
        }

        private void Update()
        {
            if (!isEnabled) return;

            ProcessTouches();
            UpdateJoystick();
            UpdateAimArea();
        }

        private void ProcessTouches()
        {
            // Process all active touches
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);

                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        OnTouchBegan(touch);
                        break;

                    case TouchPhase.Moved:
                        OnTouchMoved(touch);
                        break;

                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        OnTouchEnded(touch);
                        break;
                }
            }
        }

        private void OnTouchBegan(Touch touch)
        {
            Vector2 touchPos = touch.position;

            // Check if touch is on joystick area (left side)
            if (IsInJoystickArea(touchPos) && moveJoystick.touchId == -1)
            {
                moveJoystick.isActive = true;
                moveJoystick.touchId = touch.fingerId;
                // Set joystick base to touch position for dynamic joystick
            }
            // Check if touch is on aim area (right side)
            else if (IsInAimArea(touchPos) && aimArea.touchId == -1)
            {
                aimArea.isActive = true;
                aimArea.touchId = touch.fingerId;
                aimArea.lastTouchPosition = touchPos;
            }
            // Check if touch is on a button
            else
            {
                CheckButtonTouch(touchPos, touch.fingerId, true);
            }

            activeTouches[touch.fingerId] = new TouchData
            {
                fingerId = touch.fingerId,
                startPosition = touchPos,
                currentPosition = touchPos,
                startTime = Time.time,
                phase = touch.phase
            };
        }

        private void OnTouchMoved(Touch touch)
        {
            if (!activeTouches.ContainsKey(touch.fingerId)) return;

            var touchData = activeTouches[touch.fingerId];
            touchData.currentPosition = touch.position;

            // Update joystick
            if (moveJoystick.touchId == touch.fingerId)
            {
                UpdateJoystickInput(touch.position);
            }

            // Update aim
            if (aimArea.touchId == touch.fingerId)
            {
                UpdateAimInput(touch.position);
            }
        }

        private void OnTouchEnded(Touch touch)
        {
            // Release joystick
            if (moveJoystick.touchId == touch.fingerId)
            {
                moveJoystick.isActive = false;
                moveJoystick.touchId = -1;
                moveJoystick.inputVector = Vector2.zero;
            }

            // Release aim
            if (aimArea.touchId == touch.fingerId)
            {
                aimArea.isActive = false;
                aimArea.touchId = -1;
                aimArea.aimDelta = Vector2.zero;
            }

            // Release buttons
            CheckButtonTouch(touch.position, touch.fingerId, false);

            activeTouches.Remove(touch.fingerId);
        }

        private void UpdateJoystickInput(Vector2 touchPosition)
        {
            // Calculate input vector from joystick base to touch position
            Vector2 delta = touchPosition - moveJoystick.joystickBase.position;
            float distance = delta.magnitude;

            if (distance > moveJoystick.radius)
            {
                delta = delta.normalized * moveJoystick.radius;
            }

            moveJoystick.inputVector = delta / moveJoystick.radius;

            // Apply dead zone
            if (moveJoystick.inputVector.magnitude < deadZone)
            {
                moveJoystick.inputVector = Vector2.zero;
            }

            // Update visual handle position
            if (moveJoystick.joystickHandle != null)
            {
                moveJoystick.joystickHandle.anchoredPosition = delta;
            }
        }

        private void UpdateAimInput(Vector2 touchPosition)
        {
            Vector2 delta = touchPosition - aimArea.lastTouchPosition;
            aimArea.aimDelta = delta * aimSensitivity * Time.deltaTime;
            aimArea.lastTouchPosition = touchPosition;
        }

        private void UpdateJoystick()
        {
            // Apply joystick input to player movement
            if (moveJoystick.isActive && PlayerController.Instance != null)
            {
                Vector2 input = moveJoystick.inputVector * joystickSensitivity;
                // PlayerController.Instance.SetMoveInput(input);
            }
        }

        private void UpdateAimArea()
        {
            // Apply aim input to camera
            if (aimArea.isActive && CameraController.Instance != null)
            {
                // CameraController.Instance.AddRotationInput(aimArea.aimDelta);
            }
        }

        private bool IsInJoystickArea(Vector2 position)
        {
            // Left half of screen
            return position.x < Screen.width * 0.5f && position.y < Screen.height * 0.7f;
        }

        private bool IsInAimArea(Vector2 position)
        {
            // Right half of screen
            return position.x > Screen.width * 0.5f;
        }

        private void CheckButtonTouch(Vector2 position, int touchId, bool isPress)
        {
            foreach (var button in virtualButtons.Values)
            {
                if (button.buttonRect != null && RectTransformUtility.RectangleContainsScreenPoint(button.buttonRect, position))
                {
                    if (isPress)
                    {
                        button.isPressed = true;
                        button.touchId = touchId;
                        button.onPress?.Invoke();
                    }
                    else if (button.touchId == touchId)
                    {
                        button.isPressed = false;
                        button.touchId = -1;
                        button.onRelease?.Invoke();
                    }
                }
            }
        }

        private void ApplyLayout(ControlLayout layout)
        {
            switch (layout)
            {
                case ControlLayout.Default:
                    ApplyDefaultLayout();
                    break;
                case ControlLayout.Southpaw:
                    ApplySouthpawLayout();
                    break;
                case ControlLayout.Tablet:
                    ApplyTabletLayout();
                    break;
                case ControlLayout.Compact:
                    ApplyCompactLayout();
                    break;
            }
        }

        private void ApplyDefaultLayout()
        {
            // Joystick on left, buttons on right
        }

        private void ApplySouthpawLayout()
        {
            // Joystick on right, buttons on left
        }

        private void ApplyTabletLayout()
        {
            // Larger controls, more spread out
        }

        private void ApplyCompactLayout()
        {
            // Minimal UI, essential controls only
        }

        // Button Callbacks

        private void OnFirePress()
        {
            // WeaponSystem.Instance.StartFiring();
            Debug.Log("[Mobile] Fire pressed");
        }

        private void OnFireRelease()
        {
            // WeaponSystem.Instance.StopFiring();
            Debug.Log("[Mobile] Fire released");
        }

        private void OnReloadPress()
        {
            // WeaponSystem.Instance.Reload();
            Debug.Log("[Mobile] Reload");
        }

        private void OnJumpPress()
        {
            // PlayerController.Instance.Jump();
            Debug.Log("[Mobile] Jump");
        }

        private void OnCrouchPress()
        {
            // PlayerController.Instance.ToggleCrouch();
            Debug.Log("[Mobile] Crouch");
        }

        private void OnInteractPress()
        {
            // InteractionSystem.Instance.Interact();
            Debug.Log("[Mobile] Interact");
        }

        private void OnWeaponSwitchPress()
        {
            // WeaponSystem.Instance.SwitchWeapon();
            Debug.Log("[Mobile] Weapon switch");
        }

        private void OnGrenadePress()
        {
            // WeaponSystem.Instance.ThrowGrenade();
            Debug.Log("[Mobile] Grenade");
        }

        private void OnAbilityPress()
        {
            // AbilitySystem.Instance.UseAbility();
            Debug.Log("[Mobile] Ability");
        }

        // Public API

        public void EnableMobileControls(bool enabled)
        {
            isEnabled = enabled;
            // Show/hide mobile UI
        }

        public void SetControlLayout(ControlLayout layout)
        {
            currentLayout = layout;
            ApplyLayout(layout);
        }

        public void SetJoystickSensitivity(float sensitivity)
        {
            joystickSensitivity = sensitivity;
        }

        public void SetAimSensitivity(float sensitivity)
        {
            aimSensitivity = sensitivity;
        }

        public Vector2 GetMoveInput()
        {
            return moveJoystick.inputVector;
        }

        public Vector2 GetAimInput()
        {
            return aimArea.aimDelta;
        }

        public bool IsButtonPressed(string buttonName)
        {
            return virtualButtons.ContainsKey(buttonName) && virtualButtons[buttonName].isPressed;
        }
    }
}
