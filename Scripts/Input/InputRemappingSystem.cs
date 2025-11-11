using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace ZombieGame
{
    /// <summary>
    /// Input Remapping System - Full control customization for players
    /// Features: Rebindable keys, gamepad support, multiple profiles, conflict detection
    /// Essential for user comfort and accessibility
    /// </summary>
    public class InputRemappingSystem : MonoBehaviour
    {
        public static InputRemappingSystem Instance { get; private set; }

        [Header("Input Settings")]
        [SerializeField] private bool allowRemapping = true;
        [SerializeField] private float rebindTimeout = 5f;

        private Dictionary<string, KeyBinding> keyBindings = new Dictionary<string, KeyBinding>();
        private Dictionary<string, InputProfile> profiles = new Dictionary<string, InputProfile>();
        private string currentProfileId = "default";

        // Events
        public event System.Action<string, KeyCode> OnKeyRebound;
        public event System.Action<string> OnProfileChanged;
        public event System.Action<string, string> OnBindingConflict;

        [System.Serializable]
        public class KeyBinding
        {
            public string actionId;
            public string actionName;
            public InputCategory category;
            public KeyCode primaryKey;
            public KeyCode secondaryKey;
            public string gamepadButton;
            public bool allowRebind = true;
            public KeyCode defaultPrimaryKey;
            public KeyCode defaultSecondaryKey;
        }

        [System.Serializable]
        public class InputProfile
        {
            public string profileId;
            public string profileName;
            public Dictionary<string, KeyBinding> bindings = new Dictionary<string, KeyBinding>();
        }

        public enum InputCategory
        {
            Movement,
            Combat,
            Interaction,
            UI,
            Communication,
            Camera,
            Vehicle,
            Other
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeDefaultBindings();
                LoadBindings();
            }
            else { Destroy(gameObject); }
        }

        private void InitializeDefaultBindings()
        {
            // Movement
            AddBinding("move_forward", "Move Forward", InputCategory.Movement, KeyCode.W, KeyCode.UpArrow);
            AddBinding("move_backward", "Move Backward", InputCategory.Movement, KeyCode.S, KeyCode.DownArrow);
            AddBinding("move_left", "Move Left", InputCategory.Movement, KeyCode.A, KeyCode.LeftArrow);
            AddBinding("move_right", "Move Right", InputCategory.Movement, KeyCode.D, KeyCode.RightArrow);
            AddBinding("sprint", "Sprint", InputCategory.Movement, KeyCode.LeftShift, KeyCode.None);
            AddBinding("crouch", "Crouch", InputCategory.Movement, KeyCode.LeftControl, KeyCode.C);
            AddBinding("jump", "Jump", InputCategory.Movement, KeyCode.Space, KeyCode.None);

            // Combat
            AddBinding("fire", "Fire Weapon", InputCategory.Combat, KeyCode.Mouse0, KeyCode.None);
            AddBinding("aim", "Aim Down Sights", InputCategory.Combat, KeyCode.Mouse1, KeyCode.None);
            AddBinding("reload", "Reload", InputCategory.Combat, KeyCode.R, KeyCode.None);
            AddBinding("melee", "Melee Attack", InputCategory.Combat, KeyCode.V, KeyCode.None);
            AddBinding("grenade", "Throw Grenade", InputCategory.Combat, KeyCode.G, KeyCode.None);
            AddBinding("weapon_1", "Weapon Slot 1", InputCategory.Combat, KeyCode.Alpha1, KeyCode.None);
            AddBinding("weapon_2", "Weapon Slot 2", InputCategory.Combat, KeyCode.Alpha2, KeyCode.None);
            AddBinding("weapon_3", "Weapon Slot 3", InputCategory.Combat, KeyCode.Alpha3, KeyCode.None);
            AddBinding("next_weapon", "Next Weapon", InputCategory.Combat, KeyCode.Mouse4, KeyCode.E);
            AddBinding("prev_weapon", "Previous Weapon", InputCategory.Combat, KeyCode.Mouse3, KeyCode.Q);

            // Interaction
            AddBinding("interact", "Interact", InputCategory.Interaction, KeyCode.E, KeyCode.F);
            AddBinding("use", "Use Item", InputCategory.Interaction, KeyCode.F, KeyCode.None);
            AddBinding("pickup", "Pickup Item", InputCategory.Interaction, KeyCode.E, KeyCode.None);

            // UI
            AddBinding("inventory", "Open Inventory", InputCategory.UI, KeyCode.Tab, KeyCode.I);
            AddBinding("map", "Open Map", InputCategory.UI, KeyCode.M, KeyCode.None);
            AddBinding("pause", "Pause Menu", InputCategory.UI, KeyCode.Escape, KeyCode.P);
            AddBinding("scoreboard", "Scoreboard", InputCategory.UI, KeyCode.Tab, KeyCode.None);

            // Communication
            AddBinding("chat", "Open Chat", InputCategory.Communication, KeyCode.Return, KeyCode.T);
            AddBinding("voice_push", "Push to Talk", InputCategory.Communication, KeyCode.V, KeyCode.None);
            AddBinding("ping", "Ping Location", InputCategory.Communication, KeyCode.Z, KeyCode.None);

            // Camera
            AddBinding("toggle_camera", "Toggle Camera", InputCategory.Camera, KeyCode.C, KeyCode.None);
            AddBinding("freelook", "Free Look", InputCategory.Camera, KeyCode.LeftAlt, KeyCode.None);

            Debug.Log($"[InputRemap] Initialized {keyBindings.Count} default bindings");
        }

        private void AddBinding(string actionId, string actionName, InputCategory category, 
            KeyCode primary, KeyCode secondary, string gamepadButton = "")
        {
            var binding = new KeyBinding
            {
                actionId = actionId,
                actionName = actionName,
                category = category,
                primaryKey = primary,
                secondaryKey = secondary,
                gamepadButton = gamepadButton,
                defaultPrimaryKey = primary,
                defaultSecondaryKey = secondary
            };

            keyBindings[actionId] = binding;
        }

        public void RebindKey(string actionId, KeyCode newKey, bool isPrimary = true)
        {
            if (!allowRemapping) return;
            if (!keyBindings.ContainsKey(actionId)) return;

            var binding = keyBindings[actionId];
            if (!binding.allowRebind) return;

            // Check for conflicts
            string conflict = FindConflict(newKey, actionId);
            if (conflict != null)
            {
                OnBindingConflict?.Invoke(actionId, conflict);
                Debug.LogWarning($"[InputRemap] Key conflict: {newKey} already bound to {conflict}");
                return;
            }

            // Apply rebind
            if (isPrimary)
                binding.primaryKey = newKey;
            else
                binding.secondaryKey = newKey;

            OnKeyRebound?.Invoke(actionId, newKey);
            SaveBindings();

            Debug.Log($"[InputRemap] Rebound {actionId} to {newKey}");
        }

        private string FindConflict(KeyCode key, string excludeAction)
        {
            foreach (var kvp in keyBindings)
            {
                if (kvp.Key == excludeAction) continue;
                var binding = kvp.Value;

                if (binding.primaryKey == key || binding.secondaryKey == key)
                {
                    return kvp.Key;
                }
            }
            return null;
        }

        public void ResetBinding(string actionId)
        {
            if (!keyBindings.ContainsKey(actionId)) return;

            var binding = keyBindings[actionId];
            binding.primaryKey = binding.defaultPrimaryKey;
            binding.secondaryKey = binding.defaultSecondaryKey;

            SaveBindings();
            Debug.Log($"[InputRemap] Reset {actionId} to defaults");
        }

        public void ResetAllBindings()
        {
            foreach (var binding in keyBindings.Values)
            {
                binding.primaryKey = binding.defaultPrimaryKey;
                binding.secondaryKey = binding.defaultSecondaryKey;
            }

            SaveBindings();
            Debug.Log("[InputRemap] Reset all bindings to defaults");
        }

        public bool IsActionPressed(string actionId)
        {
            if (!keyBindings.ContainsKey(actionId)) return false;

            var binding = keyBindings[actionId];
            return Input.GetKey(binding.primaryKey) || Input.GetKey(binding.secondaryKey);
        }

        public bool IsActionDown(string actionId)
        {
            if (!keyBindings.ContainsKey(actionId)) return false;

            var binding = keyBindings[actionId];
            return Input.GetKeyDown(binding.primaryKey) || Input.GetKeyDown(binding.secondaryKey);
        }

        public bool IsActionUp(string actionId)
        {
            if (!keyBindings.ContainsKey(actionId)) return false;

            var binding = keyBindings[actionId];
            return Input.GetKeyUp(binding.primaryKey) || Input.GetKeyUp(binding.secondaryKey);
        }

        public KeyBinding GetBinding(string actionId)
        {
            return keyBindings.ContainsKey(actionId) ? keyBindings[actionId] : null;
        }

        public List<KeyBinding> GetBindingsByCategory(InputCategory category)
        {
            var result = new List<KeyBinding>();
            foreach (var binding in keyBindings.Values)
            {
                if (binding.category == category)
                    result.Add(binding);
            }
            return result;
        }

        public List<KeyBinding> GetAllBindings()
        {
            return new List<KeyBinding>(keyBindings.Values);
        }

        // Profile Management

        public void SaveProfile(string profileId, string profileName)
        {
            var profile = new InputProfile
            {
                profileId = profileId,
                profileName = profileName,
                bindings = new Dictionary<string, KeyBinding>(keyBindings)
            };

            profiles[profileId] = profile;
            PlayerPrefs.SetString($"InputProfile_{profileId}", JsonUtility.ToJson(profile));
            PlayerPrefs.Save();

            Debug.Log($"[InputRemap] Saved profile: {profileName}");
        }

        public void LoadProfile(string profileId)
        {
            if (!profiles.ContainsKey(profileId))
            {
                string json = PlayerPrefs.GetString($"InputProfile_{profileId}", "");
                if (string.IsNullOrEmpty(json)) return;

                var profile = JsonUtility.FromJson<InputProfile>(json);
                profiles[profileId] = profile;
            }

            var loadedProfile = profiles[profileId];
            keyBindings = new Dictionary<string, KeyBinding>(loadedProfile.bindings);
            currentProfileId = profileId;

            OnProfileChanged?.Invoke(profileId);
            Debug.Log($"[InputRemap] Loaded profile: {loadedProfile.profileName}");
        }

        public List<InputProfile> GetAllProfiles()
        {
            return new List<InputProfile>(profiles.Values);
        }

        private void SaveBindings()
        {
            foreach (var kvp in keyBindings)
            {
                PlayerPrefs.SetInt($"Input_{kvp.Key}_Primary", (int)kvp.Value.primaryKey);
                PlayerPrefs.SetInt($"Input_{kvp.Key}_Secondary", (int)kvp.Value.secondaryKey);
            }
            PlayerPrefs.Save();
        }

        private void LoadBindings()
        {
            foreach (var kvp in keyBindings)
            {
                int primary = PlayerPrefs.GetInt($"Input_{kvp.Key}_Primary", (int)kvp.Value.defaultPrimaryKey);
                int secondary = PlayerPrefs.GetInt($"Input_{kvp.Key}_Secondary", (int)kvp.Value.defaultSecondaryKey);

                kvp.Value.primaryKey = (KeyCode)primary;
                kvp.Value.secondaryKey = (KeyCode)secondary;
            }

            Debug.Log("[InputRemap] Loaded saved bindings");
        }

        public string GetKeyName(KeyCode key)
        {
            // Friendly key names
            switch (key)
            {
                case KeyCode.Mouse0: return "Left Mouse";
                case KeyCode.Mouse1: return "Right Mouse";
                case KeyCode.Mouse2: return "Middle Mouse";
                case KeyCode.Mouse3: return "Mouse 4";
                case KeyCode.Mouse4: return "Mouse 5";
                case KeyCode.LeftShift: return "Left Shift";
                case KeyCode.RightShift: return "Right Shift";
                case KeyCode.LeftControl: return "Left Ctrl";
                case KeyCode.RightControl: return "Right Ctrl";
                case KeyCode.LeftAlt: return "Left Alt";
                case KeyCode.RightAlt: return "Right Alt";
                case KeyCode.None: return "Not Bound";
                default: return key.ToString();
            }
        }
    }
}
