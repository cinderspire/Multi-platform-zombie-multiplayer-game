using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace DeadFrontier.Player
{
    /// <summary>
    /// Manages player cosmetics and customization
    /// Handles outfits, weapon skins, emotes, and visual customization
    /// </summary>
    public class CosmeticsManager : Singleton<CosmeticsManager>
    {
        [Header("Cosmetic Database")]
        [SerializeField] private List<CosmeticItemData> cosmeticItems = new List<CosmeticItemData>();

        [Header("Default Items")]
        [SerializeField] private string defaultOutfit = "default_outfit";
        [SerializeField] private string defaultWeaponSkin = "default_skin";

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;
        [SerializeField] private bool unlockAllInEditor = false;

        // Owned cosmetics
        private HashSet<string> ownedCosmetics = new HashSet<string>();

        // Equipped cosmetics
        private Dictionary<CosmeticType, string> equippedCosmetics = new Dictionary<CosmeticType, string>();

        // Events
        public event System.Action<CosmeticItemData> OnCosmeticUnlocked;
        public event System.Action<CosmeticItemData> OnCosmeticEquipped;

        protected override void Awake()
        {
            base.Awake();
            InitializeDefaults();
            LoadCosmeticsData();

#if UNITY_EDITOR
            if (unlockAllInEditor)
            {
                UnlockAllCosmetics();
            }
#endif
        }

        #region Initialization

        private void InitializeDefaults()
        {
            if (cosmeticItems.Count == 0)
            {
                AddDefaultCosmetics();
            }

            // Initialize equipped dictionary
            foreach (CosmeticType type in System.Enum.GetValues(typeof(CosmeticType)))
            {
                equippedCosmetics[type] = "";
            }
        }

        private void AddDefaultCosmetics()
        {
            // Outfits
            cosmeticItems.Add(new CosmeticItemData
            {
                itemId = "default_outfit",
                itemName = "Default Outfit",
                description = "Standard survivor outfit",
                type = CosmeticType.Outfit,
                rarity = CosmeticRarity.Common,
                isDefault = true
            });

            cosmeticItems.Add(new CosmeticItemData
            {
                itemId = "tactical_outfit",
                itemName = "Tactical Gear",
                description = "Military-grade tactical outfit",
                type = CosmeticType.Outfit,
                rarity = CosmeticRarity.Rare,
                unlockLevel = 10
            });

            // Weapon Skins
            cosmeticItems.Add(new CosmeticItemData
            {
                itemId = "default_skin",
                itemName = "Default Skin",
                description = "Standard weapon finish",
                type = CosmeticType.WeaponSkin,
                rarity = CosmeticRarity.Common,
                isDefault = true
            });

            cosmeticItems.Add(new CosmeticItemData
            {
                itemId = "gold_skin",
                itemName = "Golden Weapon",
                description = "Prestigious golden finish",
                type = CosmeticType.WeaponSkin,
                rarity = CosmeticRarity.Legendary,
                unlockLevel = 50
            });

            // Emotes
            cosmeticItems.Add(new CosmeticItemData
            {
                itemId = "wave_emote",
                itemName = "Wave",
                description = "Friendly wave gesture",
                type = CosmeticType.Emote,
                rarity = CosmeticRarity.Common,
                isDefault = true
            });

            // Victory Poses
            cosmeticItems.Add(new CosmeticItemData
            {
                itemId = "default_pose",
                itemName = "Victory Stance",
                description = "Default victory pose",
                type = CosmeticType.VictoryPose,
                rarity = CosmeticRarity.Common,
                isDefault = true
            });

            // Death Effects
            cosmeticItems.Add(new CosmeticItemData
            {
                itemId = "sparkle_death",
                itemName = "Sparkle Effect",
                description = "Sparkles on elimination",
                type = CosmeticType.DeathEffect,
                rarity = CosmeticRarity.Epic,
                unlockLevel = 25
            });

            if (showDebugLogs)
                Debug.Log($"[CosmeticsManager] Added {cosmeticItems.Count} default cosmetics");
        }

        #endregion

        #region Cosmetic Management

        /// <summary>
        /// Unlocks a cosmetic item
        /// </summary>
        public bool UnlockCosmetic(string cosmeticId)
        {
            if (OwnsCosmetic(cosmeticId))
            {
                if (showDebugLogs)
                    Debug.LogWarning($"[CosmeticsManager] Already own cosmetic: {cosmeticId}");
                return false;
            }

            var cosmetic = GetCosmeticById(cosmeticId);
            if (cosmetic == null)
            {
                if (showDebugLogs)
                    Debug.LogError($"[CosmeticsManager] Cosmetic not found: {cosmeticId}");
                return false;
            }

            ownedCosmetics.Add(cosmeticId);
            SaveCosmeticsData();

            if (showDebugLogs)
                Debug.Log($"[CosmeticsManager] Unlocked cosmetic: {cosmetic.itemName}");

            OnCosmeticUnlocked?.Invoke(cosmetic);

            // Show notification
            if (UI.NotificationManager.Instance != null)
            {
                UI.NotificationManager.Instance.ShowNotification(
                    "Cosmetic Unlocked!",
                    cosmetic.itemName,
                    GetRarityColor(cosmetic.rarity),
                    UI.NotificationType.Reward
                );
            }

            // Track analytics
            Core.Analytics.AnalyticsManager.Instance?.TrackEvent("cosmetic_unlocked", new Dictionary<string, object>
            {
                { "cosmetic_id", cosmeticId },
                { "cosmetic_type", cosmetic.type.ToString() },
                { "rarity", cosmetic.rarity.ToString() }
            });

            return true;
        }

        /// <summary>
        /// Equips a cosmetic item
        /// </summary>
        public bool EquipCosmetic(string cosmeticId)
        {
            if (!OwnsCosmetic(cosmeticId))
            {
                if (showDebugLogs)
                    Debug.LogWarning($"[CosmeticsManager] Don't own cosmetic: {cosmeticId}");
                return false;
            }

            var cosmetic = GetCosmeticById(cosmeticId);
            if (cosmetic == null)
                return false;

            equippedCosmetics[cosmetic.type] = cosmeticId;
            SaveCosmeticsData();

            if (showDebugLogs)
                Debug.Log($"[CosmeticsManager] Equipped {cosmetic.type}: {cosmetic.itemName}");

            OnCosmeticEquipped?.Invoke(cosmetic);

            // Apply cosmetic to player
            ApplyCosmeticToPlayer(cosmetic);

            // Track analytics
            Core.Analytics.AnalyticsManager.Instance?.TrackEvent("cosmetic_equipped", new Dictionary<string, object>
            {
                { "cosmetic_id", cosmeticId },
                { "cosmetic_type", cosmetic.type.ToString() }
            });

            return true;
        }

        /// <summary>
        /// Unequips a cosmetic type
        /// </summary>
        public void UnequipCosmetic(CosmeticType type)
        {
            if (equippedCosmetics.ContainsKey(type))
            {
                equippedCosmetics[type] = "";
                SaveCosmeticsData();

                if (showDebugLogs)
                    Debug.Log($"[CosmeticsManager] Unequipped {type}");

                // Revert to default
                EquipDefaultCosmetic(type);
            }
        }

        private void EquipDefaultCosmetic(CosmeticType type)
        {
            var defaultCosmetic = cosmeticItems.FirstOrDefault(c => c.type == type && c.isDefault);
            if (defaultCosmetic != null)
            {
                EquipCosmetic(defaultCosmetic.itemId);
            }
        }

        private void ApplyCosmeticToPlayer(CosmeticItemData cosmetic)
        {
            // TODO: Apply visual changes to player model
            // This would involve:
            // - Loading outfit meshes/materials
            // - Applying weapon skins
            // - Setting up emote animations
            // - Configuring particle effects

            switch (cosmetic.type)
            {
                case CosmeticType.Outfit:
                    ApplyOutfit(cosmetic);
                    break;
                case CosmeticType.WeaponSkin:
                    ApplyWeaponSkin(cosmetic);
                    break;
                case CosmeticType.Emote:
                    // Emotes are applied when triggered
                    break;
                case CosmeticType.VictoryPose:
                    // Victory poses are applied at match end
                    break;
                case CosmeticType.DeathEffect:
                    // Death effects are applied on elimination
                    break;
            }
        }

        private void ApplyOutfit(CosmeticItemData cosmetic)
        {
            // TODO: Change player mesh/materials
            if (showDebugLogs)
                Debug.Log($"[CosmeticsManager] Applying outfit: {cosmetic.itemName}");
        }

        private void ApplyWeaponSkin(CosmeticItemData cosmetic)
        {
            // TODO: Change weapon materials
            if (showDebugLogs)
                Debug.Log($"[CosmeticsManager] Applying weapon skin: {cosmetic.itemName}");
        }

        #endregion

        #region Unlocking

        /// <summary>
        /// Unlocks cosmetics based on level
        /// </summary>
        public void CheckLevelUnlocks(int level)
        {
            var unlockedItems = cosmeticItems.Where(c =>
                !c.isDefault &&
                c.unlockLevel > 0 &&
                c.unlockLevel == level &&
                !OwnsCosmetic(c.itemId)
            ).ToList();

            foreach (var item in unlockedItems)
            {
                UnlockCosmetic(item.itemId);
            }
        }

        /// <summary>
        /// Unlocks cosmetics from achievements
        /// </summary>
        public void UnlockFromAchievement(string achievementId)
        {
            var cosmetics = cosmeticItems.Where(c => c.unlockAchievementId == achievementId).ToList();

            foreach (var cosmetic in cosmetics)
            {
                UnlockCosmetic(cosmetic.itemId);
            }
        }

        /// <summary>
        /// Unlocks cosmetics from seasonal events
        /// </summary>
        public void UnlockFromEvent(string eventId)
        {
            var cosmetics = cosmeticItems.Where(c => c.eventExclusive && c.eventId == eventId).ToList();

            foreach (var cosmetic in cosmetics)
            {
                UnlockCosmetic(cosmetic.itemId);
            }
        }

        #endregion

        #region Queries

        /// <summary>
        /// Checks if player owns a cosmetic
        /// </summary>
        public bool OwnsCosmetic(string cosmeticId)
        {
            var cosmetic = GetCosmeticById(cosmeticId);
            return cosmetic != null && (cosmetic.isDefault || ownedCosmetics.Contains(cosmeticId));
        }

        /// <summary>
        /// Gets a cosmetic by ID
        /// </summary>
        public CosmeticItemData GetCosmeticById(string cosmeticId)
        {
            return cosmeticItems.FirstOrDefault(c => c.itemId == cosmeticId);
        }

        /// <summary>
        /// Gets all cosmetics of a type
        /// </summary>
        public List<CosmeticItemData> GetCosmeticsByType(CosmeticType type)
        {
            return cosmeticItems.Where(c => c.type == type).ToList();
        }

        /// <summary>
        /// Gets owned cosmetics of a type
        /// </summary>
        public List<CosmeticItemData> GetOwnedCosmeticsByType(CosmeticType type)
        {
            return cosmeticItems.Where(c => c.type == type && OwnsCosmetic(c.itemId)).ToList();
        }

        /// <summary>
        /// Gets currently equipped cosmetic for a type
        /// </summary>
        public string GetEquippedCosmetic(CosmeticType type)
        {
            return equippedCosmetics.ContainsKey(type) ? equippedCosmetics[type] : "";
        }

        /// <summary>
        /// Gets all cosmetics
        /// </summary>
        public List<CosmeticItemData> GetAllCosmetics()
        {
            return new List<CosmeticItemData>(cosmeticItems);
        }

        /// <summary>
        /// Gets cosmetics by rarity
        /// </summary>
        public List<CosmeticItemData> GetCosmeticsByRarity(CosmeticRarity rarity)
        {
            return cosmeticItems.Where(c => c.rarity == rarity).ToList();
        }

        #endregion

        #region Save/Load

        private void LoadCosmeticsData()
        {
            if (Core.Save.SaveSystem.Instance != null && Core.Save.SaveSystem.Instance.CurrentSave != null)
            {
                var save = Core.Save.SaveSystem.Instance.CurrentSave;

                // Load owned cosmetics
                ownedCosmetics.Clear();
                if (save.cosmeticsData != null && save.cosmeticsData.ownedCosmetics != null)
                {
                    foreach (var id in save.cosmeticsData.ownedCosmetics)
                    {
                        ownedCosmetics.Add(id);
                    }
                }

                // Load equipped cosmetics
                if (save.cosmeticsData != null && save.cosmeticsData.equippedCosmetics != null)
                {
                    foreach (var kvp in save.cosmeticsData.equippedCosmetics)
                    {
                        if (System.Enum.TryParse(kvp.Key, out CosmeticType type))
                        {
                            equippedCosmetics[type] = kvp.Value;
                        }
                    }
                }

                if (showDebugLogs)
                    Debug.Log($"[CosmeticsManager] Loaded {ownedCosmetics.Count} owned cosmetics");
            }
            else
            {
                // Unlock default cosmetics
                foreach (var cosmetic in cosmeticItems.Where(c => c.isDefault))
                {
                    ownedCosmetics.Add(cosmetic.itemId);
                    if (string.IsNullOrEmpty(equippedCosmetics[cosmetic.type]))
                    {
                        equippedCosmetics[cosmetic.type] = cosmetic.itemId;
                    }
                }
            }
        }

        private void SaveCosmeticsData()
        {
            if (Core.Save.SaveSystem.Instance != null && Core.Save.SaveSystem.Instance.CurrentSave != null)
            {
                var save = Core.Save.SaveSystem.Instance.CurrentSave;

                if (save.cosmeticsData == null)
                {
                    save.cosmeticsData = new Core.Save.CosmeticsData();
                }

                save.cosmeticsData.ownedCosmetics = new List<string>(ownedCosmetics);
                save.cosmeticsData.equippedCosmetics = new Dictionary<string, string>();

                foreach (var kvp in equippedCosmetics)
                {
                    save.cosmeticsData.equippedCosmetics[kvp.Key.ToString()] = kvp.Value;
                }

                Core.Save.SaveSystem.Instance.SaveGame(Core.Save.SaveSystem.Instance.CurrentSlot);
            }
        }

        #endregion

        #region Helpers

        private Color GetRarityColor(CosmeticRarity rarity)
        {
            switch (rarity)
            {
                case CosmeticRarity.Common:
                    return new Color(0.7f, 0.7f, 0.7f);
                case CosmeticRarity.Uncommon:
                    return new Color(0.3f, 1f, 0.3f);
                case CosmeticRarity.Rare:
                    return new Color(0.3f, 0.6f, 1f);
                case CosmeticRarity.Epic:
                    return new Color(0.8f, 0.3f, 1f);
                case CosmeticRarity.Legendary:
                    return new Color(1f, 0.5f, 0f);
                default:
                    return Color.white;
            }
        }

        [ContextMenu("Unlock All Cosmetics")]
        private void UnlockAllCosmetics()
        {
            foreach (var cosmetic in cosmeticItems)
            {
                if (!OwnsCosmetic(cosmetic.itemId))
                {
                    ownedCosmetics.Add(cosmetic.itemId);
                }
            }

            SaveCosmeticsData();

            if (showDebugLogs)
                Debug.Log("[CosmeticsManager] Unlocked all cosmetics");
        }

        #endregion

        #region Properties

        public int TotalCosmeticsCount => cosmeticItems.Count;
        public int OwnedCosmeticsCount => ownedCosmetics.Count;
        public float CollectionProgress => (float)OwnedCosmeticsCount / TotalCosmeticsCount * 100f;

        #endregion
    }

    #region Data Structures

    public enum CosmeticType
    {
        Outfit,
        WeaponSkin,
        Emote,
        VictoryPose,
        DeathEffect,
        NameTag,
        PlayerIcon
    }

    public enum CosmeticRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    [System.Serializable]
    public class CosmeticItemData
    {
        [Header("Basic Info")]
        public string itemId;
        public string itemName;
        [TextArea(2, 3)]
        public string description;
        public CosmeticType type;
        public CosmeticRarity rarity;
        public Sprite icon;
        public GameObject prefab; // For 3D preview

        [Header("Unlock Requirements")]
        public bool isDefault;
        public int unlockLevel;
        public string unlockAchievementId;
        public bool eventExclusive;
        public string eventId;

        [Header("Purchase")]
        public bool isPurchasable;
        public int softCurrencyPrice;
        public int hardCurrencyPrice;

        [Header("Display")]
        public bool isFeatured;
        public bool isNew;
        public System.DateTime addedDate;
    }

    #endregion
}

namespace DeadFrontier.Core.Save
{
    [System.Serializable]
    public class CosmeticsData
    {
        public List<string> ownedCosmetics = new List<string>();
        public Dictionary<string, string> equippedCosmetics = new Dictionary<string, string>();
    }
}
