using UnityEngine;
using System.Collections.Generic;

namespace ZombieGame
{
    /// <summary>
    /// Quality of Life System - Small features that make big differences
    /// Features: Auto-loot filter, quick actions, smart pings, item comparison, favorites
    /// Essential for smooth, frustration-free gameplay
    /// </summary>
    public class QualityOfLifeSystem : MonoBehaviour
    {
        public static QualityOfLifeSystem Instance { get; private set; }

        [Header("QoL Settings")]
        [SerializeField] private bool enableAutoLoot = true;
        [SerializeField] private bool enableQuickActions = true;
        [SerializeField] private bool enableSmartPings = true;
        [SerializeField] private bool enableItemComparison = true;
        [SerializeField] private bool showDamageNumbers = true;
        [SerializeField] private bool showHitMarkers = true;

        [Header("Auto-Loot Settings")]
        [SerializeField] private LootRarity minAutoLootRarity = LootRarity.Common;
        [SerializeField] private bool autoLootAmmo = true;
        [SerializeField] private bool autoLootMedkits = true;
        [SerializeField] private bool autoLootCurrency = true;

        private Dictionary<string, bool> favoriteItems = new Dictionary<string, bool>();
        private List<QuickAction> quickActions = new List<QuickAction>();
        private Queue<SmartPing> recentPings = new Queue<SmartPing>();

        // Events
        public event System.Action<string> OnItemAutoLooted;
        public event System.Action<QuickAction> OnQuickActionUsed;
        public event System.Action<SmartPing> OnSmartPingSent;

        [System.Serializable]
        public class QuickAction
        {
            public string actionId;
            public string actionName;
            public KeyCode hotkey;
            public System.Action callback;
        }

        [System.Serializable]
        public class SmartPing
        {
            public PingType type;
            public Vector3 position;
            public string itemName;
            public float timestamp;
        }

        public enum LootRarity
        {
            Common,
            Uncommon,
            Rare,
            Epic,
            Legendary
        }

        public enum PingType
        {
            Enemy,
            Item,
            Location,
            Danger,
            Help,
            MoveTo,
            Defend
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeQoLFeatures();
                LoadSettings();
            }
            else { Destroy(gameObject); }
        }

        private void InitializeQoLFeatures()
        {
            // Initialize quick actions
            RegisterQuickAction("quick_heal", "Quick Heal", KeyCode.H);
            RegisterQuickAction("quick_reload", "Quick Reload All", KeyCode.R);
            RegisterQuickAction("toggle_crouch", "Toggle Crouch", KeyCode.C);

            Debug.Log("[QoL] Quality of Life features initialized");
        }

        // Auto-Loot System

        public bool ShouldAutoLoot(string itemId, LootRarity rarity, string itemType)
        {
            if (!enableAutoLoot) return false;

            // Check minimum rarity
            if (rarity < minAutoLootRarity) return false;

            // Always loot specific types
            if (itemType == "ammo" && autoLootAmmo) return true;
            if (itemType == "medkit" && autoLootMedkits) return true;
            if (itemType == "currency" && autoLootCurrency) return true;

            // Check favorites
            if (favoriteItems.ContainsKey(itemId) && favoriteItems[itemId]) return true;

            return rarity >= minAutoLootRarity;
        }

        public void AutoLootItem(string itemId, string itemName)
        {
            if (!enableAutoLoot) return;

            OnItemAutoLooted?.Invoke(itemId);
            Debug.Log($"[QoL] Auto-looted: {itemName}");
        }

        public void SetAutoLootRarity(LootRarity minRarity)
        {
            minAutoLootRarity = minRarity;
            SaveSettings();
            Debug.Log($"[QoL] Auto-loot minimum rarity set to: {minRarity}");
        }

        public void SetAutoLootType(string type, bool enabled)
        {
            switch (type.ToLower())
            {
                case "ammo": autoLootAmmo = enabled; break;
                case "medkit": autoLootMedkits = enabled; break;
                case "currency": autoLootCurrency = enabled; break;
            }
            SaveSettings();
        }

        // Favorite Items

        public void MarkAsFavorite(string itemId, bool isFavorite)
        {
            favoriteItems[itemId] = isFavorite;
            SaveSettings();
            Debug.Log($"[QoL] Item {itemId} favorite status: {isFavorite}");
        }

        public bool IsFavorite(string itemId)
        {
            return favoriteItems.ContainsKey(itemId) && favoriteItems[itemId];
        }

        // Quick Actions

        public void RegisterQuickAction(string actionId, string actionName, KeyCode hotkey)
        {
            var action = new QuickAction
            {
                actionId = actionId,
                actionName = actionName,
                hotkey = hotkey
            };

            quickActions.Add(action);
        }

        public void ExecuteQuickAction(string actionId)
        {
            if (!enableQuickActions) return;

            var action = quickActions.Find(a => a.actionId == actionId);
            if (action != null)
            {
                action.callback?.Invoke();
                OnQuickActionUsed?.Invoke(action);
                Debug.Log($"[QoL] Quick action executed: {action.actionName}");
            }
        }

        private void Update()
        {
            if (!enableQuickActions) return;

            // Check for quick action hotkeys
            foreach (var action in quickActions)
            {
                if (Input.GetKeyDown(action.hotkey))
                {
                    ExecuteQuickAction(action.actionId);
                }
            }
        }

        // Smart Pings

        public void SendSmartPing(PingType type, Vector3 position, string context = "")
        {
            if (!enableSmartPings) return;

            var ping = new SmartPing
            {
                type = type,
                position = position,
                itemName = context,
                timestamp = Time.time
            };

            recentPings.Enqueue(ping);
            if (recentPings.Count > 10)
            {
                recentPings.Dequeue();
            }

            OnSmartPingSent?.Invoke(ping);
            Debug.Log($"[QoL] Smart ping sent: {type} at {position}");
        }

        public string GetPingMessage(PingType type, string context)
        {
            switch (type)
            {
                case PingType.Enemy:
                    return $"Enemy spotted{(string.IsNullOrEmpty(context) ? "" : $": {context}")}!";
                case PingType.Item:
                    return $"Item here{(string.IsNullOrEmpty(context) ? "" : $": {context}")}";
                case PingType.Location:
                    return "Move to this location";
                case PingType.Danger:
                    return "Danger! Be careful!";
                case PingType.Help:
                    return "I need help!";
                case PingType.MoveTo:
                    return "Let's go here";
                case PingType.Defend:
                    return "Defend this position!";
                default:
                    return "Ping";
            }
        }

        // Item Comparison

        public string CompareItems(string item1Id, string item2Id)
        {
            if (!enableItemComparison) return "Comparison disabled";

            // Would integrate with actual item system
            // Returns formatted comparison string
            return $"Comparing {item1Id} vs {item2Id}:\n" +
                   "Damage: +10\n" +
                   "Fire Rate: -5%\n" +
                   "Reload Speed: +15%";
        }

        // Damage Numbers

        public void ShowDamageNumber(Vector3 position, float damage, bool isCritical = false)
        {
            if (!showDamageNumbers) return;

            // Would create floating damage text
            Color color = isCritical ? Color.red : Color.white;
            float scale = isCritical ? 1.5f : 1.0f;

            Debug.Log($"[QoL] Damage number: {damage} (Critical: {isCritical})");
        }

        // Hit Markers

        public void ShowHitMarker(bool isHeadshot = false, bool isKill = false)
        {
            if (!showHitMarkers) return;

            // Would show hit marker UI
            string markerType = isKill ? "KILL" : (isHeadshot ? "HEADSHOT" : "HIT");
            Debug.Log($"[QoL] Hit marker: {markerType}");
        }

        // Additional QoL Features

        public void EnableFeature(string featureName, bool enabled)
        {
            switch (featureName.ToLower())
            {
                case "autoloot":
                    enableAutoLoot = enabled;
                    break;
                case "quickactions":
                    enableQuickActions = enabled;
                    break;
                case "smartpings":
                    enableSmartPings = enabled;
                    break;
                case "itemcomparison":
                    enableItemComparison = enabled;
                    break;
                case "damagenumbers":
                    showDamageNumbers = enabled;
                    break;
                case "hitmarkers":
                    showHitMarkers = enabled;
                    break;
            }

            SaveSettings();
            Debug.Log($"[QoL] Feature {featureName} set to: {enabled}");
        }

        public bool IsFeatureEnabled(string featureName)
        {
            switch (featureName.ToLower())
            {
                case "autoloot": return enableAutoLoot;
                case "quickactions": return enableQuickActions;
                case "smartpings": return enableSmartPings;
                case "itemcomparison": return enableItemComparison;
                case "damagenumbers": return showDamageNumbers;
                case "hitmarkers": return showHitMarkers;
                default: return false;
            }
        }

        // Utility Functions

        public void QuickSortInventory()
        {
            // Would sort player inventory by rarity/type
            Debug.Log("[QoL] Inventory sorted");
        }

        public void QuickSellJunk()
        {
            // Would sell all junk items
            Debug.Log("[QoL] Junk items sold");
        }

        public void QuickRepairAll()
        {
            // Would repair all damaged items
            Debug.Log("[QoL] All items repaired");
        }

        public void ToggleAutoRun(bool enabled)
        {
            // Would enable/disable auto-run
            Debug.Log($"[QoL] Auto-run: {enabled}");
        }

        // Save/Load

        private void SaveSettings()
        {
            PlayerPrefs.SetInt("QoL_AutoLoot", enableAutoLoot ? 1 : 0);
            PlayerPrefs.SetInt("QoL_AutoLootRarity", (int)minAutoLootRarity);
            PlayerPrefs.SetInt("QoL_AutoLootAmmo", autoLootAmmo ? 1 : 0);
            PlayerPrefs.SetInt("QoL_AutoLootMedkits", autoLootMedkits ? 1 : 0);
            PlayerPrefs.SetInt("QoL_AutoLootCurrency", autoLootCurrency ? 1 : 0);
            PlayerPrefs.SetInt("QoL_QuickActions", enableQuickActions ? 1 : 0);
            PlayerPrefs.SetInt("QoL_SmartPings", enableSmartPings ? 1 : 0);
            PlayerPrefs.SetInt("QoL_DamageNumbers", showDamageNumbers ? 1 : 0);
            PlayerPrefs.SetInt("QoL_HitMarkers", showHitMarkers ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void LoadSettings()
        {
            enableAutoLoot = PlayerPrefs.GetInt("QoL_AutoLoot", 1) == 1;
            minAutoLootRarity = (LootRarity)PlayerPrefs.GetInt("QoL_AutoLootRarity", 0);
            autoLootAmmo = PlayerPrefs.GetInt("QoL_AutoLootAmmo", 1) == 1;
            autoLootMedkits = PlayerPrefs.GetInt("QoL_AutoLootMedkits", 1) == 1;
            autoLootCurrency = PlayerPrefs.GetInt("QoL_AutoLootCurrency", 1) == 1;
            enableQuickActions = PlayerPrefs.GetInt("QoL_QuickActions", 1) == 1;
            enableSmartPings = PlayerPrefs.GetInt("QoL_SmartPings", 1) == 1;
            showDamageNumbers = PlayerPrefs.GetInt("QoL_DamageNumbers", 1) == 1;
            showHitMarkers = PlayerPrefs.GetInt("QoL_HitMarkers", 1) == 1;

            Debug.Log("[QoL] Settings loaded");
        }
    }
}
