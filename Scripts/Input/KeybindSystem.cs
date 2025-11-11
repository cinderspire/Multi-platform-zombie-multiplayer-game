using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Input
{
    /// <summary>
    /// Customizable keybind system allowing players to remap all controls
    /// with conflict detection and preset configurations.
    /// </summary>
    public class KeybindSystem : MonoBehaviour
    {
        public static KeybindSystem Instance { get; private set; }

        [Header("Keybind Configuration")]
        [SerializeField] private bool allowConflicts = false;
        [SerializeField] private string keybindFileName = "Keybinds.json";

        private Dictionary<string, KeyMapping> defaultKeybinds = new Dictionary<string, KeyMapping>();
        private Dictionary<string, KeyMapping> currentKeybinds = new Dictionary<string, KeyMapping>();
        private bool isRebinding = false;
        private string rebindingAction = "";

        public event Action<string, KeyCode> OnKeybindChanged;
        public event Action OnKeybindsReset;
        public event Action<string> OnRebindStarted;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            InitializeDefaultKeybinds();
            LoadKeybinds();
        }

        private void InitializeDefaultKeybinds()
        {
            // Movement
            AddKeybind("MoveForward", KeyCode.W, "Movement", "Move Forward");
            AddKeybind("MoveBackward", KeyCode.S, "Movement", "Move Backward");
            AddKeybind("MoveLeft", KeyCode.A, "Movement", "Move Left");
            AddKeybind("MoveRight", KeyCode.D, "Movement", "Move Right");
            AddKeybind("Sprint", KeyCode.LeftShift, "Movement", "Sprint");
            AddKeybind("Crouch", KeyCode.LeftControl, "Movement", "Crouch/Slide");
            AddKeybind("Jump", KeyCode.Space, "Movement", "Jump");

            // Combat
            AddKeybind("Fire", KeyCode.Mouse0, "Combat", "Fire Weapon");
            AddKeybind("AimDownSights", KeyCode.Mouse1, "Combat", "Aim Down Sights");
            AddKeybind("Reload", KeyCode.R, "Combat", "Reload");
            AddKeybind("Melee", KeyCode.V, "Combat", "Melee Attack");
            AddKeybind("ThrowGrenade", KeyCode.G, "Combat", "Throw Grenade");

            // Weapons
            AddKeybind("WeaponSlot1", KeyCode.Alpha1, "Weapons", "Primary Weapon");
            AddKeybind("WeaponSlot2", KeyCode.Alpha2, "Weapons", "Secondary Weapon");
            AddKeybind("WeaponSlot3", KeyCode.Alpha3, "Weapons", "Melee Weapon");
            AddKeybind("NextWeapon", KeyCode.E, "Weapons", "Next Weapon");
            AddKeybind("PreviousWeapon", KeyCode.Q, "Weapons", "Previous Weapon");

            // Abilities
            AddKeybind("Ability1", KeyCode.Alpha4, "Abilities", "Ability 1");
            AddKeybind("Ability2", KeyCode.Alpha5, "Abilities", "Ability 2");
            AddKeybind("Ability3", KeyCode.Alpha6, "Abilities", "Ability 3");
            AddKeybind("Ultimate", KeyCode.F, "Abilities", "Ultimate Ability");

            // Interface
            AddKeybind("Inventory", KeyCode.Tab, "Interface", "Open Inventory");
            AddKeybind("Map", KeyCode.M, "Interface", "Open Map");
            AddKeybind("Scoreboard", KeyCode.Tab, "Interface", "Scoreboard");
            AddKeybind("Menu", KeyCode.Escape, "Interface", "Game Menu");
            AddKeybind("Chat", KeyCode.Return, "Interface", "Open Chat");
            AddKeybind("TeamChat", KeyCode.T, "Interface", "Team Chat");
            AddKeybind("VoiceChat", KeyCode.C, "Interface", "Push to Talk");

            // Interaction
            AddKeybind("Interact", KeyCode.E, "Interaction", "Interact");
            AddKeybind("Use", KeyCode.F, "Interaction", "Use Item");
            AddKeybind("Ping", KeyCode.Z, "Interaction", "Ping Location");
            AddKeybind("Mark", KeyCode.X, "Interaction", "Mark Enemy");

            // Camera
            AddKeybind("ToggleCamera", KeyCode.V, "Camera", "Toggle Camera");
            AddKeybind("FreeLook", KeyCode.LeftAlt, "Camera", "Free Look");
            AddKeybind("Zoom", KeyCode.B, "Camera", "Zoom");

            // Emotes & Social
            AddKeybind("EmoteWheel", KeyCode.B, "Social", "Emote Wheel");
            AddKeybind("QuickEmote1", KeyCode.Keypad1, "Social", "Quick Emote 1");
            AddKeybind("QuickEmote2", KeyCode.Keypad2, "Social", "Quick Emote 2");
        }

        private void AddKeybind(string action, KeyCode defaultKey, string category, string displayName)
        {
            KeyMapping mapping = new KeyMapping
            {
                action = action,
                keyCode = defaultKey,
                category = category,
                displayName = displayName
            };

            defaultKeybinds[action] = mapping;
            currentKeybinds[action] = new KeyMapping
            {
                action = action,
                keyCode = defaultKey,
                category = category,
                displayName = displayName
            };
        }

        private void Update()
        {
            if (isRebinding)
            {
                CheckForRebindInput();
            }
        }

        /// <summary>
        /// Start rebinding an action
        /// </summary>
        public void StartRebind(string action)
        {
            if (!currentKeybinds.ContainsKey(action)) return;

            isRebinding = true;
            rebindingAction = action;
            OnRebindStarted?.Invoke(action);
        }

        private void CheckForRebindInput()
        {
            // Check all keycodes
            foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (UnityEngine.Input.GetKeyDown(key))
                {
                    // Allow Escape to cancel
                    if (key == KeyCode.Escape)
                    {
                        CancelRebind();
                        return;
                    }

                    // Check for conflicts
                    if (!allowConflicts && HasConflict(key, rebindingAction))
                    {
                        Debug.LogWarning($"Key {key} is already bound to another action!");
                        continue;
                    }

                    // Apply new binding
                    SetKeybind(rebindingAction, key);
                    isRebinding = false;
                    rebindingAction = "";
                    return;
                }
            }
        }

        private void CancelRebind()
        {
            isRebinding = false;
            rebindingAction = "";
        }

        private bool HasConflict(KeyCode newKey, string actionToRebind)
        {
            foreach (var kvp in currentKeybinds)
            {
                if (kvp.Key == actionToRebind) continue;
                if (kvp.Value.keyCode == newKey) return true;
            }
            return false;
        }

        /// <summary>
        /// Set keybind for an action
        /// </summary>
        public void SetKeybind(string action, KeyCode newKey)
        {
            if (!currentKeybinds.ContainsKey(action)) return;

            currentKeybinds[action].keyCode = newKey;
            OnKeybindChanged?.Invoke(action, newKey);

            SaveKeybinds();
        }

        /// <summary>
        /// Get current keybind for action
        /// </summary>
        public KeyCode GetKeybind(string action)
        {
            return currentKeybinds.TryGetValue(action, out var mapping) ?
                mapping.keyCode : KeyCode.None;
        }

        /// <summary>
        /// Check if action is pressed
        /// </summary>
        public bool GetAction(string action)
        {
            KeyCode key = GetKeybind(action);
            return key != KeyCode.None && UnityEngine.Input.GetKey(key);
        }

        /// <summary>
        /// Check if action was pressed this frame
        /// </summary>
        public bool GetActionDown(string action)
        {
            KeyCode key = GetKeybind(action);
            return key != KeyCode.None && UnityEngine.Input.GetKeyDown(key);
        }

        /// <summary>
        /// Check if action was released this frame
        /// </summary>
        public bool GetActionUp(string action)
        {
            KeyCode key = GetKeybind(action);
            return key != KeyCode.None && UnityEngine.Input.GetKeyUp(key);
        }

        /// <summary>
        /// Reset all keybinds to default
        /// </summary>
        public void ResetToDefaults()
        {
            currentKeybinds.Clear();
            foreach (var kvp in defaultKeybinds)
            {
                currentKeybinds[kvp.Key] = new KeyMapping
                {
                    action = kvp.Value.action,
                    keyCode = kvp.Value.keyCode,
                    category = kvp.Value.category,
                    displayName = kvp.Value.displayName
                };
            }

            OnKeybindsReset?.Invoke();
            SaveKeybinds();
        }

        /// <summary>
        /// Load preset keybind configuration
        /// </summary>
        public void LoadPreset(KeybindPreset preset)
        {
            switch (preset)
            {
                case KeybindPreset.Default:
                    ResetToDefaults();
                    break;

                case KeybindPreset.WASD:
                    // Already default
                    break;

                case KeybindPreset.ESDF:
                    SetKeybind("MoveForward", KeyCode.E);
                    SetKeybind("MoveBackward", KeyCode.D);
                    SetKeybind("MoveLeft", KeyCode.S);
                    SetKeybind("MoveRight", KeyCode.F);
                    break;

                case KeybindPreset.ArrowKeys:
                    SetKeybind("MoveForward", KeyCode.UpArrow);
                    SetKeybind("MoveBackward", KeyCode.DownArrow);
                    SetKeybind("MoveLeft", KeyCode.LeftArrow);
                    SetKeybind("MoveRight", KeyCode.RightArrow);
                    break;

                case KeybindPreset.LeftHanded:
                    // Mirror controls for left-handed players
                    SetKeybind("MoveForward", KeyCode.I);
                    SetKeybind("MoveBackward", KeyCode.K);
                    SetKeybind("MoveLeft", KeyCode.J);
                    SetKeybind("MoveRight", KeyCode.L);
                    break;
            }

            SaveKeybinds();
        }

        public Dictionary<string, KeyMapping> GetAllKeybinds()
        {
            return new Dictionary<string, KeyMapping>(currentKeybinds);
        }

        public List<string> GetCategories()
        {
            HashSet<string> categories = new HashSet<string>();
            foreach (var mapping in currentKeybinds.Values)
            {
                categories.Add(mapping.category);
            }
            return new List<string>(categories);
        }

        private void SaveKeybinds()
        {
            // Save to PlayerPrefs or file
            string json = JsonUtility.ToJson(new KeybindData { bindings = new List<KeyMapping>(currentKeybinds.Values) });
            PlayerPrefs.SetString(keybindFileName, json);
            PlayerPrefs.Save();
        }

        private void LoadKeybinds()
        {
            if (PlayerPrefs.HasKey(keybindFileName))
            {
                string json = PlayerPrefs.GetString(keybindFileName);
                KeybindData data = JsonUtility.FromJson<KeybindData>(json);

                foreach (var mapping in data.bindings)
                {
                    if (currentKeybinds.ContainsKey(mapping.action))
                    {
                        currentKeybinds[mapping.action].keyCode = mapping.keyCode;
                    }
                }
            }
        }

        [Serializable]
        public class KeyMapping
        {
            public string action;
            public KeyCode keyCode;
            public string category;
            public string displayName;
        }

        [Serializable]
        private class KeybindData
        {
            public List<KeyMapping> bindings;
        }

        public enum KeybindPreset
        {
            Default,
            WASD,
            ESDF,
            ArrowKeys,
            LeftHanded
        }
    }
}
