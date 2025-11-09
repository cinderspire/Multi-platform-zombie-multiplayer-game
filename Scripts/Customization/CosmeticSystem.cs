using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace DeadFrontier.Customization
{
    /// <summary>
    /// Comprehensive cosmetic customization system.
    /// Supports character skins, weapon skins, emotes, sprays, banners, and accessories.
    /// Provides unlocking, equipping, and preview functionality for player expression.
    /// </summary>
    public class CosmeticSystem : MonoBehaviour
    {
        public static CosmeticSystem Instance { get; private set; }

        [Header("Cosmetic Database")]
        [SerializeField] private CosmeticItem[] availableCosmetics;

        [Header("Customization Slots")]
        [SerializeField] private int maxCharacterSlots = 5;
        [SerializeField] private int maxWeaponSkinSlots = 10;
        [SerializeField] private int maxAccessorySlots = 8;

        [Header("Unlock Settings")]
        [SerializeField] private bool enableRaritySystem = true;

        // Player cosmetics
        private Dictionary<ulong, PlayerCosmetics> playerCosmetics = new Dictionary<ulong, PlayerCosmetics>();
        private Dictionary<ulong, EquippedCosmetics> equippedCosmetics = new Dictionary<ulong, EquippedCosmetics>();

        // Events
        public event Action<ulong, string> OnCosmeticUnlocked;
        public event Action<ulong, string, CosmeticSlot> OnCosmeticEquipped;
        public event Action<ulong, CosmeticSlot> OnCosmeticUnequipped;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            LoadAllPlayerCosmetics();
        }

        #region Initialization

        public void InitializePlayerCosmetics(ulong playerId)
        {
            if (playerCosmetics.ContainsKey(playerId)) return;

            playerCosmetics[playerId] = new PlayerCosmetics
            {
                playerId = playerId,
                unlockedCosmetics = new List<string>(),
                favoriteCosmetics = new List<string>()
            };

            equippedCosmetics[playerId] = new EquippedCosmetics
            {
                playerId = playerId,
                equippedSlots = new Dictionary<CosmeticSlot, string>()
            };

            // Unlock default cosmetics
            UnlockDefaultCosmetics(playerId);

            Debug.Log($"[CosmeticSystem] Initialized cosmetics for player {playerId}");
        }

        private void UnlockDefaultCosmetics(ulong playerId)
        {
            var defaultCosmetics = availableCosmetics.Where(c => c.isDefaultUnlocked);

            foreach (var cosmetic in defaultCosmetics)
            {
                UnlockCosmetic(playerId, cosmetic.cosmeticId, skipCost: true);
            }
        }

        #endregion

        #region Unlocking

        public bool CanUnlockCosmetic(ulong playerId, string cosmeticId)
        {
            if (!playerCosmetics.ContainsKey(playerId))
            {
                InitializePlayerCosmetics(playerId);
            }

            var cosmetic = GetCosmetic(cosmeticId);
            if (cosmetic == null) return false;

            // Check if already unlocked
            if (IsCosmeticUnlocked(playerId, cosmeticId)) return false;

            // Check level requirement
            if (cosmetic.requiredLevel > 0)
            {
                if (Progression.AchievementManager.Instance != null)
                {
                    int playerLevel = Progression.AchievementManager.Instance.GetPlayerLevel();
                    if (playerLevel < cosmetic.requiredLevel) return false;
                }
            }

            // Check currency requirement
            if (cosmetic.unlockCost > 0)
            {
                if (Economy.EconomyManager.Instance != null)
                {
                    if (cosmetic.isPremium)
                    {
                        if (!Economy.EconomyManager.Instance.CanAffordHardCurrency(cosmetic.unlockCost))
                            return false;
                    }
                    else
                    {
                        if (!Economy.EconomyManager.Instance.CanAfford(cosmetic.unlockCost))
                            return false;
                    }
                }
            }

            return true;
        }

        public bool UnlockCosmetic(ulong playerId, string cosmeticId, bool skipCost = false)
        {
            if (!skipCost && !CanUnlockCosmetic(playerId, cosmeticId)) return false;

            var cosmetic = GetCosmetic(cosmeticId);
            if (cosmetic == null) return false;

            // Charge cost
            if (!skipCost && cosmetic.unlockCost > 0)
            {
                if (Economy.EconomyManager.Instance != null)
                {
                    if (cosmetic.isPremium)
                    {
                        if (!Economy.EconomyManager.Instance.SpendHardCurrency(cosmetic.unlockCost, $"Unlock Cosmetic: {cosmetic.cosmeticName}"))
                            return false;
                    }
                    else
                    {
                        if (!Economy.EconomyManager.Instance.SpendSoftCurrency(cosmetic.unlockCost, $"Unlock Cosmetic: {cosmetic.cosmeticName}"))
                            return false;
                    }
                }
            }

            playerCosmetics[playerId].unlockedCosmetics.Add(cosmeticId);

            SavePlayerCosmetics(playerId);

            OnCosmeticUnlocked?.Invoke(playerId, cosmeticId);

            Debug.Log($"[CosmeticSystem] Player {playerId} unlocked cosmetic: {cosmetic.cosmeticName}");

            return true;
        }

        public bool IsCosmeticUnlocked(ulong playerId, string cosmeticId)
        {
            if (!playerCosmetics.ContainsKey(playerId)) return false;

            return playerCosmetics[playerId].unlockedCosmetics.Contains(cosmeticId);
        }

        #endregion

        #region Equipping

        public bool CanEquipCosmetic(ulong playerId, string cosmeticId, CosmeticSlot slot)
        {
            // Check if unlocked
            if (!IsCosmeticUnlocked(playerId, cosmeticId)) return false;

            var cosmetic = GetCosmetic(cosmeticId);
            if (cosmetic == null) return false;

            // Check if cosmetic type matches slot
            if (!IsCompatibleSlot(cosmetic.cosmeticType, slot)) return false;

            return true;
        }

        public bool EquipCosmetic(ulong playerId, string cosmeticId, CosmeticSlot slot)
        {
            if (!CanEquipCosmetic(playerId, cosmeticId, slot)) return false;

            if (!equippedCosmetics.ContainsKey(playerId))
            {
                InitializePlayerCosmetics(playerId);
            }

            equippedCosmetics[playerId].equippedSlots[slot] = cosmeticId;

            SavePlayerCosmetics(playerId);

            OnCosmeticEquipped?.Invoke(playerId, cosmeticId, slot);

            Debug.Log($"[CosmeticSystem] Player {playerId} equipped {cosmeticId} to slot {slot}");

            return true;
        }

        public bool UnequipCosmetic(ulong playerId, CosmeticSlot slot)
        {
            if (!equippedCosmetics.ContainsKey(playerId)) return false;

            if (!equippedCosmetics[playerId].equippedSlots.ContainsKey(slot)) return false;

            equippedCosmetics[playerId].equippedSlots.Remove(slot);

            SavePlayerCosmetics(playerId);

            OnCosmeticUnequipped?.Invoke(playerId, slot);

            Debug.Log($"[CosmeticSystem] Player {playerId} unequipped slot {slot}");

            return true;
        }

        private bool IsCompatibleSlot(CosmeticType type, CosmeticSlot slot)
        {
            switch (type)
            {
                case CosmeticType.CharacterSkin:
                    return slot == CosmeticSlot.CharacterBody;

                case CosmeticType.Head:
                    return slot == CosmeticSlot.CharacterHead;

                case CosmeticType.Outfit:
                    return slot == CosmeticSlot.CharacterOutfit;

                case CosmeticType.WeaponSkin:
                    return slot >= CosmeticSlot.WeaponPrimary && slot <= CosmeticSlot.WeaponMelee;

                case CosmeticType.WeaponCharm:
                    return slot == CosmeticSlot.WeaponCharm;

                case CosmeticType.Backpack:
                    return slot == CosmeticSlot.Backpack;

                case CosmeticType.Gloves:
                    return slot == CosmeticSlot.Gloves;

                case CosmeticType.Banner:
                    return slot == CosmeticSlot.Banner;

                case CosmeticType.ProfileIcon:
                    return slot == CosmeticSlot.ProfileIcon;

                case CosmeticType.Title:
                    return slot == CosmeticSlot.Title;

                default:
                    return false;
            }
        }

        #endregion

        #region Favorites

        public bool ToggleFavorite(ulong playerId, string cosmeticId)
        {
            if (!playerCosmetics.ContainsKey(playerId)) return false;

            var favorites = playerCosmetics[playerId].favoriteCosmetics;

            if (favorites.Contains(cosmeticId))
            {
                favorites.Remove(cosmeticId);
            }
            else
            {
                favorites.Add(cosmeticId);
            }

            SavePlayerCosmetics(playerId);

            return favorites.Contains(cosmeticId);
        }

        public bool IsFavorite(ulong playerId, string cosmeticId)
        {
            if (!playerCosmetics.ContainsKey(playerId)) return false;

            return playerCosmetics[playerId].favoriteCosmetics.Contains(cosmeticId);
        }

        #endregion

        #region Loadout Presets

        public bool SaveLoadout(ulong playerId, string loadoutName)
        {
            if (!equippedCosmetics.ContainsKey(playerId)) return false;
            if (!playerCosmetics.ContainsKey(playerId))
            {
                InitializePlayerCosmetics(playerId);
            }

            var loadout = new CosmeticLoadout
            {
                loadoutName = loadoutName,
                equippedSlots = new Dictionary<CosmeticSlot, string>(equippedCosmetics[playerId].equippedSlots)
            };

            if (playerCosmetics[playerId].savedLoadouts == null)
            {
                playerCosmetics[playerId].savedLoadouts = new List<CosmeticLoadout>();
            }

            // Remove existing loadout with same name
            playerCosmetics[playerId].savedLoadouts.RemoveAll(l => l.loadoutName == loadoutName);

            // Add new loadout
            playerCosmetics[playerId].savedLoadouts.Add(loadout);

            SavePlayerCosmetics(playerId);

            Debug.Log($"[CosmeticSystem] Saved loadout '{loadoutName}' for player {playerId}");

            return true;
        }

        public bool LoadLoadout(ulong playerId, string loadoutName)
        {
            if (!playerCosmetics.ContainsKey(playerId)) return false;

            var loadout = playerCosmetics[playerId].savedLoadouts?.FirstOrDefault(l => l.loadoutName == loadoutName);
            if (loadout == null) return false;

            // Apply loadout
            if (!equippedCosmetics.ContainsKey(playerId))
            {
                equippedCosmetics[playerId] = new EquippedCosmetics
                {
                    playerId = playerId,
                    equippedSlots = new Dictionary<CosmeticSlot, string>()
                };
            }

            equippedCosmetics[playerId].equippedSlots = new Dictionary<CosmeticSlot, string>(loadout.equippedSlots);

            SavePlayerCosmetics(playerId);

            Debug.Log($"[CosmeticSystem] Loaded loadout '{loadoutName}' for player {playerId}");

            return true;
        }

        public bool DeleteLoadout(ulong playerId, string loadoutName)
        {
            if (!playerCosmetics.ContainsKey(playerId)) return false;
            if (playerCosmetics[playerId].savedLoadouts == null) return false;

            int removed = playerCosmetics[playerId].savedLoadouts.RemoveAll(l => l.loadoutName == loadoutName);

            if (removed > 0)
            {
                SavePlayerCosmetics(playerId);
                return true;
            }

            return false;
        }

        #endregion

        #region Collections & Filtering

        public List<CosmeticItem> GetCosmeticsByType(CosmeticType type)
        {
            return availableCosmetics.Where(c => c.cosmeticType == type).ToList();
        }

        public List<CosmeticItem> GetCosmeticsByRarity(CosmeticRarity rarity)
        {
            return availableCosmetics.Where(c => c.rarity == rarity).ToList();
        }

        public List<CosmeticItem> GetUnlockedCosmetics(ulong playerId)
        {
            if (!playerCosmetics.ContainsKey(playerId)) return new List<CosmeticItem>();

            return availableCosmetics
                .Where(c => playerCosmetics[playerId].unlockedCosmetics.Contains(c.cosmeticId))
                .ToList();
        }

        public List<CosmeticItem> GetLockedCosmetics(ulong playerId)
        {
            if (!playerCosmetics.ContainsKey(playerId)) return new List<CosmeticItem>();

            return availableCosmetics
                .Where(c => !playerCosmetics[playerId].unlockedCosmetics.Contains(c.cosmeticId))
                .ToList();
        }

        public List<CosmeticItem> GetFavoriteCosmetics(ulong playerId)
        {
            if (!playerCosmetics.ContainsKey(playerId)) return new List<CosmeticItem>();

            return availableCosmetics
                .Where(c => playerCosmetics[playerId].favoriteCosmetics.Contains(c.cosmeticId))
                .ToList();
        }

        public int GetUnlockedCount(ulong playerId, CosmeticType? type = null)
        {
            if (!playerCosmetics.ContainsKey(playerId)) return 0;

            var unlocked = playerCosmetics[playerId].unlockedCosmetics;

            if (type.HasValue)
            {
                return availableCosmetics
                    .Where(c => c.cosmeticType == type.Value && unlocked.Contains(c.cosmeticId))
                    .Count();
            }

            return unlocked.Count;
        }

        public float GetCollectionCompletion(ulong playerId)
        {
            if (availableCosmetics.Length == 0) return 0f;

            int unlocked = GetUnlockedCount(playerId);
            return (float)unlocked / availableCosmetics.Length;
        }

        #endregion

        #region Persistence

        private void LoadAllPlayerCosmetics()
        {
            // In production, load from database
            Debug.Log("[CosmeticSystem] Cosmetic system initialized");
        }

        private void SavePlayerCosmetics(ulong playerId)
        {
            // In production, save to database
        }

        #endregion

        #region Public Getters

        public CosmeticItem GetCosmetic(string cosmeticId)
        {
            return availableCosmetics.FirstOrDefault(c => c.cosmeticId == cosmeticId);
        }

        public string GetEquippedCosmetic(ulong playerId, CosmeticSlot slot)
        {
            if (!equippedCosmetics.ContainsKey(playerId)) return null;

            return equippedCosmetics[playerId].equippedSlots.ContainsKey(slot)
                ? equippedCosmetics[playerId].equippedSlots[slot]
                : null;
        }

        public Dictionary<CosmeticSlot, string> GetEquippedCosmetics(ulong playerId)
        {
            if (!equippedCosmetics.ContainsKey(playerId)) return new Dictionary<CosmeticSlot, string>();

            return new Dictionary<CosmeticSlot, string>(equippedCosmetics[playerId].equippedSlots);
        }

        public List<CosmeticLoadout> GetSavedLoadouts(ulong playerId)
        {
            if (!playerCosmetics.ContainsKey(playerId)) return new List<CosmeticLoadout>();

            return playerCosmetics[playerId].savedLoadouts != null
                ? new List<CosmeticLoadout>(playerCosmetics[playerId].savedLoadouts)
                : new List<CosmeticLoadout>();
        }

        public CosmeticItem[] GetAllCosmetics() => availableCosmetics;

        #endregion
    }

    #region Data Classes

    public class PlayerCosmetics
    {
        public ulong playerId;
        public List<string> unlockedCosmetics;
        public List<string> favoriteCosmetics;
        public List<CosmeticLoadout> savedLoadouts;
    }

    public class EquippedCosmetics
    {
        public ulong playerId;
        public Dictionary<CosmeticSlot, string> equippedSlots;
    }

    public class CosmeticLoadout
    {
        public string loadoutName;
        public Dictionary<CosmeticSlot, string> equippedSlots;
    }

    [System.Serializable]
    public class CosmeticItem
    {
        public string cosmeticId;
        public string cosmeticName;
        [TextArea(2, 3)]
        public string description;

        public CosmeticType cosmeticType;
        public CosmeticRarity rarity;

        public bool isDefaultUnlocked;
        public bool isPremium;
        public int unlockCost;
        public int requiredLevel;

        public GameObject cosmeticPrefab;
        public Sprite cosmeticIcon;
        public Material cosmeticMaterial;
    }

    public enum CosmeticType
    {
        CharacterSkin,      // Full character model
        Head,               // Helmet/Hat
        Outfit,             // Clothing
        WeaponSkin,         // Weapon paint/skin
        WeaponCharm,        // Dangling charm on weapon
        Backpack,           // Backpack/Bag
        Gloves,             // Gloves/Hands
        Banner,             // Player card banner
        ProfileIcon,        // Avatar icon
        Title,              // Player title/nameplate
        Spray,              // In-game spray decal
        VictoryPose         // End-of-match pose
    }

    public enum CosmeticRarity
    {
        Common,             // White
        Uncommon,           // Green
        Rare,               // Blue
        Epic,               // Purple
        Legendary,          // Gold
        Mythic              // Red/Special
    }

    public enum CosmeticSlot
    {
        CharacterBody,
        CharacterHead,
        CharacterOutfit,
        WeaponPrimary,
        WeaponSecondary,
        WeaponMelee,
        WeaponCharm,
        Backpack,
        Gloves,
        Banner,
        ProfileIcon,
        Title
    }

    #endregion
}
