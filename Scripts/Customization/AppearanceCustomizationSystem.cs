using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Customization
{
    /// <summary>
    /// Comprehensive appearance customization and transmogrification system.
    /// Handles character creation, equipment transmog, dyes, outfits, and cosmetics.
    /// </summary>
    public class AppearanceCustomizationSystem : NetworkBehaviour
    {
        public static AppearanceCustomizationSystem Instance { get; private set; }

        [Header("Customization Configuration")]
        [SerializeField] private int maxOutfitSlots = 10;
        [SerializeField] private int maxWardrobeItems = 500;
        [SerializeField] private bool allowCrossFactionCosmetics = false;
        [SerializeField] private int transmogCost = 100;
        [SerializeField] private int dyeCost = 50;

        [Header("Character Creation")]
        [SerializeField] private int maxBodyPresets = 20;
        [SerializeField] private int maxFacePresets = 30;
        [SerializeField] private int maxHairStyles = 40;
        [SerializeField] private int maxTattoos = 25;
        [SerializeField] private int maxScars = 15;

        [Header("Cosmetic Slots")]
        [SerializeField] private bool enableCosmeticSlots = true;
        [SerializeField] private int cosmeticSlotCount = 6;

        // Data structures
        private Dictionary<ulong, PlayerAppearance> playerAppearances = new Dictionary<ulong, PlayerAppearance>();
        private Dictionary<ulong, WardrobeCollection> playerWardrobes = new Dictionary<ulong, WardrobeCollection>();
        private Dictionary<ulong, List<PlayerOutfit>> playerOutfits = new Dictionary<ulong, List<PlayerOutfit>>();
        private Dictionary<string, CosmeticItem> cosmeticDatabase = new Dictionary<string, CosmeticItem>();
        private Dictionary<string, DyeColor> dyeDatabase = new Dictionary<string, DyeColor>();
        private Dictionary<string, TransmogTemplate> transmogTemplates = new Dictionary<string, TransmogTemplate>();

        // Events
        public event Action<ulong, PlayerAppearance> OnAppearanceChanged;
        public event Action<ulong, string> OnCosmeticUnlocked;
        public event Action<ulong, string> OnOutfitSaved;
        public event Action<ulong, EquipmentSlot, string> OnTransmogApplied;
        public event Action<ulong, EquipmentSlot, string> OnDyeApplied;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                InitializeCosmeticDatabase();
                InitializeDyeDatabase();
                InitializeTransmogTemplates();
            }
        }

        private void InitializeCosmeticDatabase()
        {
            // Initialize cosmetic items
            var cosmeticItems = new List<(string id, string name, CosmeticType type, CosmeticRarity rarity, int unlockLevel)>
            {
                // Head cosmetics
                ("helmet_skull", "Skull Helmet", CosmeticType.Head, CosmeticRarity.Epic, 20),
                ("helmet_tactical", "Tactical Helmet", CosmeticType.Head, CosmeticRarity.Rare, 10),
                ("mask_gas", "Gas Mask", CosmeticType.Head, CosmeticRarity.Common, 1),

                // Body cosmetics
                ("armor_heavy", "Heavy Armor", CosmeticType.Body, CosmeticRarity.Legendary, 50),
                ("jacket_leather", "Leather Jacket", CosmeticType.Body, CosmeticRarity.Rare, 15),
                ("vest_tactical", "Tactical Vest", CosmeticType.Body, CosmeticRarity.Common, 5),

                // Back cosmetics
                ("wings_demon", "Demon Wings", CosmeticType.Back, CosmeticRarity.Mythic, 100),
                ("cape_survivor", "Survivor's Cape", CosmeticType.Back, CosmeticRarity.Epic, 30),
                ("backpack_military", "Military Backpack", CosmeticType.Back, CosmeticRarity.Common, 1),

                // Weapon skins
                ("skin_weapon_golden", "Golden Weapon Skin", CosmeticType.WeaponSkin, CosmeticRarity.Legendary, 75),
                ("skin_weapon_camo", "Camo Weapon Skin", CosmeticType.WeaponSkin, CosmeticRarity.Rare, 25),

                // Emotes
                ("emote_dance", "Victory Dance", CosmeticType.Emote, CosmeticRarity.Rare, 10),
                ("emote_salute", "Military Salute", CosmeticType.Emote, CosmeticRarity.Common, 1)
            };

            foreach (var (id, name, type, rarity, level) in cosmeticItems)
            {
                cosmeticDatabase[id] = new CosmeticItem
                {
                    itemId = id,
                    itemName = name,
                    cosmeticType = type,
                    rarity = rarity,
                    unlockRequirement = new UnlockRequirement
                    {
                        requirementType = UnlockType.Level,
                        requiredLevel = level
                    },
                    canBeDyed = type != CosmeticType.Emote && type != CosmeticType.WeaponSkin,
                    canBeTransmogged = type == CosmeticType.Head || type == CosmeticType.Body || type == CosmeticType.Legs || type == CosmeticType.Hands || type == CosmeticType.Feet
                };
            }
        }

        private void InitializeDyeDatabase()
        {
            // Initialize dye colors
            var dyes = new List<(string id, string name, Color primary, Color secondary, DyeRarity rarity)>
            {
                ("dye_black", "Midnight Black", Color.black, Color.gray, DyeRarity.Common),
                ("dye_white", "Pure White", Color.white, new Color(0.9f, 0.9f, 0.9f), DyeRarity.Common),
                ("dye_red", "Blood Red", Color.red, new Color(0.5f, 0, 0), DyeRarity.Uncommon),
                ("dye_blue", "Ocean Blue", Color.blue, Color.cyan, DyeRarity.Uncommon),
                ("dye_green", "Forest Green", Color.green, new Color(0, 0.5f, 0), DyeRarity.Uncommon),
                ("dye_gold", "Royal Gold", new Color(1f, 0.84f, 0f), new Color(1f, 0.65f, 0f), DyeRarity.Rare),
                ("dye_rainbow", "Rainbow Shimmer", Color.magenta, Color.cyan, DyeRarity.Legendary)
            };

            foreach (var (id, name, primary, secondary, rarity) in dyes)
            {
                dyeDatabase[id] = new DyeColor
                {
                    dyeId = id,
                    dyeName = name,
                    primaryColor = primary,
                    secondaryColor = secondary,
                    rarity = rarity,
                    isAnimated = rarity == DyeRarity.Legendary
                };
            }
        }

        private void InitializeTransmogTemplates()
        {
            // Initialize transmog templates for quick application
            transmogTemplates["set_survivor"] = new TransmogTemplate
            {
                templateId = "set_survivor",
                templateName = "Survivor Set",
                transmogs = new Dictionary<EquipmentSlot, string>
                {
                    { EquipmentSlot.Head, "helmet_tactical" },
                    { EquipmentSlot.Body, "vest_tactical" },
                    { EquipmentSlot.Legs, "pants_cargo" },
                    { EquipmentSlot.Feet, "boots_combat" }
                }
            };
        }

        #region Server RPCs

        [ServerRpc(RequireOwnership = false)]
        public void CreateCharacterAppearanceServerRpc(ulong playerId, CharacterCreationData creationData, ServerRpcParams rpcParams = default)
        {
            var appearance = new PlayerAppearance
            {
                playerId = playerId,
                bodyPreset = creationData.bodyPreset,
                facePreset = creationData.facePreset,
                hairStyle = creationData.hairStyle,
                hairColor = creationData.hairColor,
                skinTone = creationData.skinTone,
                eyeColor = creationData.eyeColor,
                facialHair = creationData.facialHair,
                tattoos = creationData.tattoos,
                scars = creationData.scars,
                height = creationData.height,
                build = creationData.build,
                equipmentTransmogs = new Dictionary<EquipmentSlot, string>(),
                equipmentDyes = new Dictionary<EquipmentSlot, string>(),
                cosmeticSlots = new Dictionary<CosmeticSlot, string>(),
                activeOutfit = null
            };

            playerAppearances[playerId] = appearance;

            // Initialize wardrobe
            playerWardrobes[playerId] = new WardrobeCollection
            {
                playerId = playerId,
                unlockedCosmetics = new List<string>(),
                unlockedDyes = new List<string>(),
                favoriteCosmetics = new List<string>()
            };

            // Unlock starter cosmetics and dyes
            UnlockStarterItems(playerId);

            OnAppearanceChanged?.Invoke(playerId, appearance);
            BroadcastAppearanceChangedClientRpc(playerId, appearance);

            Debug.Log($"Created character appearance for player {playerId}");
        }

        private void UnlockStarterItems(ulong playerId)
        {
            var wardrobe = playerWardrobes[playerId];

            // Unlock basic cosmetics
            var starterCosmetics = cosmeticDatabase.Values
                .Where(c => c.unlockRequirement.requiredLevel <= 1)
                .Select(c => c.itemId);

            wardrobe.unlockedCosmetics.AddRange(starterCosmetics);

            // Unlock basic dyes
            var starterDyes = dyeDatabase.Values
                .Where(d => d.rarity == DyeRarity.Common)
                .Select(d => d.dyeId);

            wardrobe.unlockedDyes.AddRange(starterDyes);
        }

        [ServerRpc(RequireOwnership = false)]
        public void UpdateCharacterAppearanceServerRpc(ulong playerId, AppearanceUpdate update, ServerRpcParams rpcParams = default)
        {
            if (!playerAppearances.TryGetValue(playerId, out var appearance))
            {
                Debug.LogWarning($"Appearance not found for player {playerId}");
                return;
            }

            // Update appearance fields
            if (update.hairStyle.HasValue)
                appearance.hairStyle = update.hairStyle.Value;

            if (update.hairColor.HasValue)
                appearance.hairColor = update.hairColor.Value;

            if (update.facialHair.HasValue)
                appearance.facialHair = update.facialHair.Value;

            OnAppearanceChanged?.Invoke(playerId, appearance);
            BroadcastAppearanceChangedClientRpc(playerId, appearance);
        }

        [ServerRpc(RequireOwnership = false)]
        public void ApplyTransmogServerRpc(ulong playerId, EquipmentSlot slot, string cosmeticId, ServerRpcParams rpcParams = default)
        {
            if (!ValidateTransmog(playerId, cosmeticId, out string error))
            {
                Debug.LogWarning($"Invalid transmog: {error}");
                return;
            }

            if (!playerAppearances.TryGetValue(playerId, out var appearance))
            {
                Debug.LogWarning($"Appearance not found for player {playerId}");
                return;
            }

            // Charge transmog cost
            if (!Economy.EconomyManager.Instance.SpendSoftCurrency(playerId, transmogCost))
            {
                Debug.LogWarning($"Player {playerId} cannot afford transmog");
                return;
            }

            appearance.equipmentTransmogs[slot] = cosmeticId;

            OnTransmogApplied?.Invoke(playerId, slot, cosmeticId);
            NotifyTransmogAppliedClientRpc(playerId, slot, cosmeticId);

            Debug.Log($"Applied transmog to player {playerId}: {slot} -> {cosmeticId}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void RemoveTransmogServerRpc(ulong playerId, EquipmentSlot slot, ServerRpcParams rpcParams = default)
        {
            if (!playerAppearances.TryGetValue(playerId, out var appearance))
            {
                return;
            }

            appearance.equipmentTransmogs.Remove(slot);
            NotifyTransmogRemovedClientRpc(playerId, slot);
        }

        [ServerRpc(RequireOwnership = false)]
        public void ApplyDyeServerRpc(ulong playerId, EquipmentSlot slot, string dyeId, ServerRpcParams rpcParams = default)
        {
            if (!ValidateDye(playerId, dyeId, out string error))
            {
                Debug.LogWarning($"Invalid dye: {error}");
                return;
            }

            if (!playerAppearances.TryGetValue(playerId, out var appearance))
            {
                return;
            }

            // Charge dye cost
            if (!Economy.EconomyManager.Instance.SpendSoftCurrency(playerId, dyeCost))
            {
                Debug.LogWarning($"Player {playerId} cannot afford dye");
                return;
            }

            appearance.equipmentDyes[slot] = dyeId;

            OnDyeApplied?.Invoke(playerId, slot, dyeId);
            NotifyDyeAppliedClientRpc(playerId, slot, dyeId);

            Debug.Log($"Applied dye to player {playerId}: {slot} -> {dyeId}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void RemoveDyeServerRpc(ulong playerId, EquipmentSlot slot, ServerRpcParams rpcParams = default)
        {
            if (!playerAppearances.TryGetValue(playerId, out var appearance))
            {
                return;
            }

            appearance.equipmentDyes.Remove(slot);
            NotifyDyeRemovedClientRpc(playerId, slot);
        }

        [ServerRpc(RequireOwnership = false)]
        public void EquipCosmeticServerRpc(ulong playerId, CosmeticSlot slot, string cosmeticId, ServerRpcParams rpcParams = default)
        {
            if (!enableCosmeticSlots)
            {
                Debug.LogWarning("Cosmetic slots are disabled");
                return;
            }

            if (!ValidateCosmetic(playerId, cosmeticId, out string error))
            {
                Debug.LogWarning($"Invalid cosmetic: {error}");
                return;
            }

            if (!playerAppearances.TryGetValue(playerId, out var appearance))
            {
                return;
            }

            appearance.cosmeticSlots[slot] = cosmeticId;
            NotifyCosmeticEquippedClientRpc(playerId, slot, cosmeticId);

            Debug.Log($"Equipped cosmetic to player {playerId}: {slot} -> {cosmeticId}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void UnequipCosmeticServerRpc(ulong playerId, CosmeticSlot slot, ServerRpcParams rpcParams = default)
        {
            if (!playerAppearances.TryGetValue(playerId, out var appearance))
            {
                return;
            }

            appearance.cosmeticSlots.Remove(slot);
            NotifyCosmeticUnequippedClientRpc(playerId, slot);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SaveOutfitServerRpc(ulong playerId, string outfitName, ServerRpcParams rpcParams = default)
        {
            if (!playerAppearances.TryGetValue(playerId, out var appearance))
            {
                Debug.LogWarning($"Appearance not found for player {playerId}");
                return;
            }

            if (!playerOutfits.ContainsKey(playerId))
            {
                playerOutfits[playerId] = new List<PlayerOutfit>();
            }

            if (playerOutfits[playerId].Count >= maxOutfitSlots)
            {
                Debug.LogWarning($"Player {playerId} has maximum outfits");
                return;
            }

            var outfit = new PlayerOutfit
            {
                outfitId = Guid.NewGuid().ToString(),
                outfitName = outfitName,
                savedDate = DateTime.UtcNow,
                transmogs = new Dictionary<EquipmentSlot, string>(appearance.equipmentTransmogs),
                dyes = new Dictionary<EquipmentSlot, string>(appearance.equipmentDyes),
                cosmetics = new Dictionary<CosmeticSlot, string>(appearance.cosmeticSlots)
            };

            playerOutfits[playerId].Add(outfit);

            OnOutfitSaved?.Invoke(playerId, outfit.outfitId);
            NotifyOutfitSavedClientRpc(playerId, outfit.outfitId, outfitName);

            Debug.Log($"Saved outfit for player {playerId}: {outfitName}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void LoadOutfitServerRpc(ulong playerId, string outfitId, ServerRpcParams rpcParams = default)
        {
            if (!playerOutfits.TryGetValue(playerId, out var outfits))
            {
                return;
            }

            var outfit = outfits.FirstOrDefault(o => o.outfitId == outfitId);
            if (outfit == null)
            {
                Debug.LogWarning($"Outfit not found: {outfitId}");
                return;
            }

            if (!playerAppearances.TryGetValue(playerId, out var appearance))
            {
                return;
            }

            // Apply outfit
            appearance.equipmentTransmogs = new Dictionary<EquipmentSlot, string>(outfit.transmogs);
            appearance.equipmentDyes = new Dictionary<EquipmentSlot, string>(outfit.dyes);
            appearance.cosmeticSlots = new Dictionary<CosmeticSlot, string>(outfit.cosmetics);
            appearance.activeOutfit = outfitId;

            OnAppearanceChanged?.Invoke(playerId, appearance);
            BroadcastAppearanceChangedClientRpc(playerId, appearance);

            Debug.Log($"Loaded outfit for player {playerId}: {outfit.outfitName}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void DeleteOutfitServerRpc(ulong playerId, string outfitId, ServerRpcParams rpcParams = default)
        {
            if (!playerOutfits.TryGetValue(playerId, out var outfits))
            {
                return;
            }

            var outfit = outfits.FirstOrDefault(o => o.outfitId == outfitId);
            if (outfit != null)
            {
                outfits.Remove(outfit);
                NotifyOutfitDeletedClientRpc(playerId, outfitId);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void UnlockCosmeticServerRpc(ulong playerId, string cosmeticId, ServerRpcParams rpcParams = default)
        {
            if (!cosmeticDatabase.TryGetValue(cosmeticId, out var cosmetic))
            {
                Debug.LogWarning($"Cosmetic not found: {cosmeticId}");
                return;
            }

            if (!playerWardrobes.TryGetValue(playerId, out var wardrobe))
            {
                playerWardrobes[playerId] = new WardrobeCollection
                {
                    playerId = playerId,
                    unlockedCosmetics = new List<string>(),
                    unlockedDyes = new List<string>(),
                    favoriteCosmetics = new List<string>()
                };
                wardrobe = playerWardrobes[playerId];
            }

            if (wardrobe.unlockedCosmetics.Contains(cosmeticId))
            {
                Debug.LogWarning($"Cosmetic already unlocked: {cosmeticId}");
                return;
            }

            if (wardrobe.unlockedCosmetics.Count >= maxWardrobeItems)
            {
                Debug.LogWarning($"Wardrobe full for player {playerId}");
                return;
            }

            wardrobe.unlockedCosmetics.Add(cosmeticId);

            OnCosmeticUnlocked?.Invoke(playerId, cosmeticId);
            NotifyCosmeticUnlockedClientRpc(playerId, cosmeticId);

            Debug.Log($"Unlocked cosmetic for player {playerId}: {cosmeticId}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void UnlockDyeServerRpc(ulong playerId, string dyeId, ServerRpcParams rpcParams = default)
        {
            if (!dyeDatabase.ContainsKey(dyeId))
            {
                Debug.LogWarning($"Dye not found: {dyeId}");
                return;
            }

            if (!playerWardrobes.TryGetValue(playerId, out var wardrobe))
            {
                return;
            }

            if (!wardrobe.unlockedDyes.Contains(dyeId))
            {
                wardrobe.unlockedDyes.Add(dyeId);
                NotifyDyeUnlockedClientRpc(playerId, dyeId);
            }
        }

        #endregion

        #region Validation

        private bool ValidateTransmog(ulong playerId, string cosmeticId, out string error)
        {
            error = null;

            if (!cosmeticDatabase.TryGetValue(cosmeticId, out var cosmetic))
            {
                error = "Cosmetic not found";
                return false;
            }

            if (!cosmetic.canBeTransmogged)
            {
                error = "Cosmetic cannot be transmogged";
                return false;
            }

            if (!playerWardrobes.TryGetValue(playerId, out var wardrobe))
            {
                error = "Wardrobe not found";
                return false;
            }

            if (!wardrobe.unlockedCosmetics.Contains(cosmeticId))
            {
                error = "Cosmetic not unlocked";
                return false;
            }

            return true;
        }

        private bool ValidateDye(ulong playerId, string dyeId, out string error)
        {
            error = null;

            if (!dyeDatabase.ContainsKey(dyeId))
            {
                error = "Dye not found";
                return false;
            }

            if (!playerWardrobes.TryGetValue(playerId, out var wardrobe))
            {
                error = "Wardrobe not found";
                return false;
            }

            if (!wardrobe.unlockedDyes.Contains(dyeId))
            {
                error = "Dye not unlocked";
                return false;
            }

            return true;
        }

        private bool ValidateCosmetic(ulong playerId, string cosmeticId, out string error)
        {
            error = null;

            if (!cosmeticDatabase.TryGetValue(cosmeticId, out var cosmetic))
            {
                error = "Cosmetic not found";
                return false;
            }

            if (!playerWardrobes.TryGetValue(playerId, out var wardrobe))
            {
                error = "Wardrobe not found";
                return false;
            }

            if (!wardrobe.unlockedCosmetics.Contains(cosmeticId))
            {
                error = "Cosmetic not unlocked";
                return false;
            }

            return true;
        }

        #endregion

        #region Client RPCs

        [ClientRpc]
        private void BroadcastAppearanceChangedClientRpc(ulong playerId, PlayerAppearance appearance)
        {
            OnAppearanceChanged?.Invoke(playerId, appearance);
        }

        [ClientRpc]
        private void NotifyTransmogAppliedClientRpc(ulong playerId, EquipmentSlot slot, string cosmeticId)
        {
            OnTransmogApplied?.Invoke(playerId, slot, cosmeticId);
        }

        [ClientRpc]
        private void NotifyTransmogRemovedClientRpc(ulong playerId, EquipmentSlot slot)
        {
            // Client-side notification
        }

        [ClientRpc]
        private void NotifyDyeAppliedClientRpc(ulong playerId, EquipmentSlot slot, string dyeId)
        {
            OnDyeApplied?.Invoke(playerId, slot, dyeId);
        }

        [ClientRpc]
        private void NotifyDyeRemovedClientRpc(ulong playerId, EquipmentSlot slot)
        {
            // Client-side notification
        }

        [ClientRpc]
        private void NotifyCosmeticEquippedClientRpc(ulong playerId, CosmeticSlot slot, string cosmeticId)
        {
            // Client-side notification
        }

        [ClientRpc]
        private void NotifyCosmeticUnequippedClientRpc(ulong playerId, CosmeticSlot slot)
        {
            // Client-side notification
        }

        [ClientRpc]
        private void NotifyOutfitSavedClientRpc(ulong playerId, string outfitId, string outfitName)
        {
            OnOutfitSaved?.Invoke(playerId, outfitId);
        }

        [ClientRpc]
        private void NotifyOutfitDeletedClientRpc(ulong playerId, string outfitId)
        {
            // Client-side notification
        }

        [ClientRpc]
        private void NotifyCosmeticUnlockedClientRpc(ulong playerId, string cosmeticId)
        {
            OnCosmeticUnlocked?.Invoke(playerId, cosmeticId);
        }

        [ClientRpc]
        private void NotifyDyeUnlockedClientRpc(ulong playerId, string dyeId)
        {
            // Client-side notification
        }

        #endregion

        #region Public API

        public PlayerAppearance GetPlayerAppearance(ulong playerId)
        {
            return playerAppearances.GetValueOrDefault(playerId);
        }

        public WardrobeCollection GetPlayerWardrobe(ulong playerId)
        {
            return playerWardrobes.GetValueOrDefault(playerId);
        }

        public List<PlayerOutfit> GetPlayerOutfits(ulong playerId)
        {
            return playerOutfits.GetValueOrDefault(playerId, new List<PlayerOutfit>());
        }

        public List<CosmeticItem> GetAvailableCosmetics(CosmeticType? type = null)
        {
            var cosmetics = cosmeticDatabase.Values.AsEnumerable();

            if (type.HasValue)
            {
                cosmetics = cosmetics.Where(c => c.cosmeticType == type.Value);
            }

            return cosmetics.ToList();
        }

        public List<DyeColor> GetAvailableDyes()
        {
            return dyeDatabase.Values.ToList();
        }

        public CosmeticItem GetCosmetic(string cosmeticId)
        {
            return cosmeticDatabase.GetValueOrDefault(cosmeticId);
        }

        public DyeColor GetDye(string dyeId)
        {
            return dyeDatabase.GetValueOrDefault(dyeId);
        }

        #endregion
    }

    #region Data Structures

    [Serializable]
    public class PlayerAppearance
    {
        public ulong playerId;
        public int bodyPreset;
        public int facePreset;
        public int hairStyle;
        public Color hairColor;
        public Color skinTone;
        public Color eyeColor;
        public int facialHair;
        public List<int> tattoos;
        public List<int> scars;
        public float height;
        public float build;
        public Dictionary<EquipmentSlot, string> equipmentTransmogs;
        public Dictionary<EquipmentSlot, string> equipmentDyes;
        public Dictionary<CosmeticSlot, string> cosmeticSlots;
        public string activeOutfit;
    }

    [Serializable]
    public class CharacterCreationData
    {
        public int bodyPreset;
        public int facePreset;
        public int hairStyle;
        public Color hairColor;
        public Color skinTone;
        public Color eyeColor;
        public int facialHair;
        public List<int> tattoos;
        public List<int> scars;
        public float height;
        public float build;
    }

    [Serializable]
    public class AppearanceUpdate
    {
        public int? hairStyle;
        public Color? hairColor;
        public int? facialHair;
    }

    [Serializable]
    public class WardrobeCollection
    {
        public ulong playerId;
        public List<string> unlockedCosmetics;
        public List<string> unlockedDyes;
        public List<string> favoriteCosmetics;
    }

    [Serializable]
    public class PlayerOutfit
    {
        public string outfitId;
        public string outfitName;
        public DateTime savedDate;
        public Dictionary<EquipmentSlot, string> transmogs;
        public Dictionary<EquipmentSlot, string> dyes;
        public Dictionary<CosmeticSlot, string> cosmetics;
    }

    [Serializable]
    public class CosmeticItem
    {
        public string itemId;
        public string itemName;
        public CosmeticType cosmeticType;
        public CosmeticRarity rarity;
        public UnlockRequirement unlockRequirement;
        public bool canBeDyed;
        public bool canBeTransmogged;
    }

    [Serializable]
    public class DyeColor
    {
        public string dyeId;
        public string dyeName;
        public Color primaryColor;
        public Color secondaryColor;
        public DyeRarity rarity;
        public bool isAnimated;
    }

    [Serializable]
    public class TransmogTemplate
    {
        public string templateId;
        public string templateName;
        public Dictionary<EquipmentSlot, string> transmogs;
    }

    [Serializable]
    public class UnlockRequirement
    {
        public UnlockType requirementType;
        public int requiredLevel;
        public string requiredAchievement;
        public int requiredCurrency;
    }

    public enum CosmeticType
    {
        Head,
        Body,
        Legs,
        Hands,
        Feet,
        Back,
        WeaponSkin,
        Emote,
        Pose,
        VictoryAnimation
    }

    public enum CosmeticRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary,
        Mythic
    }

    public enum DyeRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    public enum EquipmentSlot
    {
        Head,
        Body,
        Legs,
        Hands,
        Feet,
        MainHand,
        OffHand,
        Back
    }

    public enum CosmeticSlot
    {
        Slot1,
        Slot2,
        Slot3,
        Slot4,
        Slot5,
        Slot6
    }

    public enum UnlockType
    {
        Level,
        Achievement,
        Purchase,
        Event,
        Quest
    }

    #endregion
}
