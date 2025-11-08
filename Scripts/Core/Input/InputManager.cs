using UnityEngine;
using System.Collections.Generic;

namespace DeadFrontier.Core.Input
{
    /// <summary>
    /// Centralized input management with key rebinding support
    /// Integrates with SettingsManager for persistent key bindings
    /// </summary>
    public class InputManager : Singleton<InputManager>
    {
        [Header("Default Key Bindings")]
        [SerializeField] private KeyBindings defaultBindings = new KeyBindings();

        [Header("Mouse Settings")]
        [SerializeField] private float defaultMouseSensitivity = 1.0f;
        [SerializeField] private float defaultAimSensitivity = 0.7f;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = false;

        // Current bindings
        private Dictionary<string, KeyCode> currentBindings = new Dictionary<string, KeyCode>();
        private Dictionary<string, bool> inputStates = new Dictionary<string, bool>();
        private Dictionary<string, bool> inputDownStates = new Dictionary<string, bool>();
        private Dictionary<string, bool> inputUpStates = new Dictionary<string, bool>();

        // Mouse
        private float mouseSensitivity;
        private float aimSensitivity;
        private bool invertY;

        // Rebinding
        private bool isWaitingForKey = false;
        private string keyToRebind = "";
        private System.Action<KeyCode> onKeyRebindCallback;

        protected override void Awake()
        {
            base.Awake();
            InitializeBindings();
            LoadSettings();
        }

        private void Update()
        {
            // Handle key rebinding
            if (isWaitingForKey)
            {
                CheckForKeyPress();
                return;
            }

            // Update input states
            UpdateInputStates();
        }

        #region Initialization

        private void InitializeBindings()
        {
            // Movement
            SetBinding("MoveForward", defaultBindings.moveForward);
            SetBinding("MoveBackward", defaultBindings.moveBackward);
            SetBinding("MoveLeft", defaultBindings.moveLeft);
            SetBinding("MoveRight", defaultBindings.moveRight);
            SetBinding("Jump", defaultBindings.jump);
            SetBinding("Crouch", defaultBindings.crouch);
            SetBinding("Sprint", defaultBindings.sprint);

            // Combat
            SetBinding("Fire", defaultBindings.fire);
            SetBinding("Aim", defaultBindings.aim);
            SetBinding("Reload", defaultBindings.reload);
            SetBinding("MeleeAttack", defaultBindings.meleeAttack);
            SetBinding("ThrowGrenade", defaultBindings.throwGrenade);
            SetBinding("SwitchWeapon", defaultBindings.switchWeapon);

            // Interaction
            SetBinding("Interact", defaultBindings.interact);
            SetBinding("Use", defaultBindings.use);

            // UI
            SetBinding("Inventory", defaultBindings.inventory);
            SetBinding("Map", defaultBindings.map);
            SetBinding("Scoreboard", defaultBindings.scoreboard);
            SetBinding("Pause", defaultBindings.pause);

            // Mouse
            mouseSensitivity = defaultMouseSensitivity;
            aimSensitivity = defaultAimSensitivity;
            invertY = false;

            if (showDebugLogs)
                Debug.Log("[InputManager] Initialized default key bindings");
        }

        private void LoadSettings()
        {
            if (Settings.SettingsManager.Instance != null)
            {
                var settings = Settings.SettingsManager.Instance.CurrentSettings;

                // Load mouse settings
                mouseSensitivity = settings.mouseSensitivity;
                aimSensitivity = settings.aimSensitivity;
                invertY = settings.invertY;

                // Load key bindings if they exist
                if (settings.keyBindings != null && settings.keyBindings.Count > 0)
                {
                    foreach (var kvp in settings.keyBindings)
                    {
                        if (System.Enum.TryParse(kvp.Value, out KeyCode keyCode))
                        {
                            SetBinding(kvp.Key, keyCode);
                        }
                    }
                }

                if (showDebugLogs)
                    Debug.Log("[InputManager] Loaded settings from SettingsManager");
            }
        }

        #endregion

        #region Input Queries

        /// <summary>
        /// Returns true while the key is held down
        /// </summary>
        public bool GetKey(string action)
        {
            if (inputStates.ContainsKey(action))
                return inputStates[action];
            return false;
        }

        /// <summary>
        /// Returns true on the frame the key is pressed
        /// </summary>
        public bool GetKeyDown(string action)
        {
            if (inputDownStates.ContainsKey(action))
                return inputDownStates[action];
            return false;
        }

        /// <summary>
        /// Returns true on the frame the key is released
        /// </summary>
        public bool GetKeyUp(string action)
        {
            if (inputUpStates.ContainsKey(action))
                return inputUpStates[action];
            return false;
        }

        /// <summary>
        /// Gets mouse input with sensitivity applied
        /// </summary>
        public Vector2 GetMouseDelta(bool isAiming = false)
        {
            float sensitivity = isAiming ? aimSensitivity : mouseSensitivity;
            float x = UnityEngine.Input.GetAxis("Mouse X") * sensitivity;
            float y = UnityEngine.Input.GetAxis("Mouse Y") * sensitivity;

            if (invertY)
                y = -y;

            return new Vector2(x, y);
        }

        /// <summary>
        /// Gets movement input (WASD)
        /// </summary>
        public Vector2 GetMovementInput()
        {
            float horizontal = 0f;
            float vertical = 0f;

            if (GetKey("MoveForward")) vertical += 1f;
            if (GetKey("MoveBackward")) vertical -= 1f;
            if (GetKey("MoveRight")) horizontal += 1f;
            if (GetKey("MoveLeft")) horizontal -= 1f;

            return new Vector2(horizontal, vertical).normalized;
        }

        #endregion

        #region Key Binding

        /// <summary>
        /// Sets a key binding
        /// </summary>
        public void SetBinding(string action, KeyCode key)
        {
            currentBindings[action] = key;

            // Save to settings
            if (Settings.SettingsManager.Instance != null)
            {
                Settings.SettingsManager.Instance.SetKeyBinding(action, key.ToString());
            }

            if (showDebugLogs)
                Debug.Log($"[InputManager] Bound {action} to {key}");
        }

        /// <summary>
        /// Gets the current key binding for an action
        /// </summary>
        public KeyCode GetBinding(string action)
        {
            if (currentBindings.ContainsKey(action))
                return currentBindings[action];
            return KeyCode.None;
        }

        /// <summary>
        /// Starts waiting for a key press to rebind
        /// </summary>
        public void StartRebinding(string action, System.Action<KeyCode> callback)
        {
            isWaitingForKey = true;
            keyToRebind = action;
            onKeyRebindCallback = callback;

            if (showDebugLogs)
                Debug.Log($"[InputManager] Waiting for key press to rebind {action}");
        }

        /// <summary>
        /// Cancels key rebinding
        /// </summary>
        public void CancelRebinding()
        {
            isWaitingForKey = false;
            keyToRebind = "";
            onKeyRebindCallback = null;
        }

        /// <summary>
        /// Resets all bindings to defaults
        /// </summary>
        public void ResetToDefaults()
        {
            InitializeBindings();

            if (showDebugLogs)
                Debug.Log("[InputManager] Reset to default bindings");
        }

        /// <summary>
        /// Checks if an action is bound to a key
        /// </summary>
        public bool IsActionBound(KeyCode key, out string action)
        {
            foreach (var kvp in currentBindings)
            {
                if (kvp.Value == key)
                {
                    action = kvp.Key;
                    return true;
                }
            }

            action = "";
            return false;
        }

        #endregion

        #region Mouse Settings

        public void SetMouseSensitivity(float sensitivity)
        {
            mouseSensitivity = Mathf.Clamp(sensitivity, 0.1f, 5f);

            if (Settings.SettingsManager.Instance != null)
            {
                Settings.SettingsManager.Instance.SetMouseSensitivity(mouseSensitivity);
            }
        }

        public void SetAimSensitivity(float sensitivity)
        {
            aimSensitivity = Mathf.Clamp(sensitivity, 0.1f, 5f);

            if (Settings.SettingsManager.Instance != null)
            {
                Settings.SettingsManager.Instance.SetAimSensitivity(aimSensitivity);
            }
        }

        public void SetInvertY(bool invert)
        {
            invertY = invert;

            if (Settings.SettingsManager.Instance != null)
            {
                Settings.SettingsManager.Instance.SetInvertY(invertY);
            }
        }

        public float MouseSensitivity => mouseSensitivity;
        public float AimSensitivity => aimSensitivity;
        public bool InvertY => invertY;

        #endregion

        #region Internal

        private void UpdateInputStates()
        {
            foreach (var kvp in currentBindings)
            {
                string action = kvp.Key;
                KeyCode key = kvp.Value;

                inputStates[action] = UnityEngine.Input.GetKey(key);
                inputDownStates[action] = UnityEngine.Input.GetKeyDown(key);
                inputUpStates[action] = UnityEngine.Input.GetKeyUp(key);
            }
        }

        private void CheckForKeyPress()
        {
            foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (UnityEngine.Input.GetKeyDown(key))
                {
                    // Ignore mouse buttons for rebinding
                    if (key >= KeyCode.Mouse0 && key <= KeyCode.Mouse6)
                        continue;

                    // Check if key is already bound
                    if (IsActionBound(key, out string existingAction))
                    {
                        if (showDebugLogs)
                            Debug.LogWarning($"[InputManager] Key {key} is already bound to {existingAction}");
                    }

                    // Set new binding
                    SetBinding(keyToRebind, key);

                    // Callback
                    onKeyRebindCallback?.Invoke(key);

                    // Reset rebinding state
                    isWaitingForKey = false;
                    keyToRebind = "";
                    onKeyRebindCallback = null;

                    break;
                }
            }
        }

        #endregion

        #region Properties

        public bool IsWaitingForKeyRebind => isWaitingForKey;
        public Dictionary<string, KeyCode> CurrentBindings => new Dictionary<string, KeyCode>(currentBindings);

        #endregion
    }

    #region Data Structures

    [System.Serializable]
    public class KeyBindings
    {
        [Header("Movement")]
        public KeyCode moveForward = KeyCode.W;
        public KeyCode moveBackward = KeyCode.S;
        public KeyCode moveLeft = KeyCode.A;
        public KeyCode moveRight = KeyCode.D;
        public KeyCode jump = KeyCode.Space;
        public KeyCode crouch = KeyCode.LeftControl;
        public KeyCode sprint = KeyCode.LeftShift;

        [Header("Combat")]
        public KeyCode fire = KeyCode.Mouse0;
        public KeyCode aim = KeyCode.Mouse1;
        public KeyCode reload = KeyCode.R;
        public KeyCode meleeAttack = KeyCode.F;
        public KeyCode throwGrenade = KeyCode.G;
        public KeyCode switchWeapon = KeyCode.Q;

        [Header("Interaction")]
        public KeyCode interact = KeyCode.E;
        public KeyCode use = KeyCode.F;

        [Header("UI")]
        public KeyCode inventory = KeyCode.Tab;
        public KeyCode map = KeyCode.M;
        public KeyCode scoreboard = KeyCode.Tab;
        public KeyCode pause = KeyCode.Escape;
    }

    #endregion
}
