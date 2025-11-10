using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Cosmetics
{
    public class CosmeticSystem : NetworkBehaviour
    {
        public static CosmeticSystem Instance { get; private set; }

        private Dictionary<string, CosmeticItem> cosmeticDatabase = new Dictionary<string, CosmeticItem>();
        private Dictionary<ulong, PlayerCosmetics> playerCosmetics = new Dictionary<ulong, PlayerCosmetics>();

        public event Action<ulong, string> OnCosmeticUnlocked;
        public event Action<ulong, string> OnCosmeticEquipped;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer) InitializeCosmetics();
        }

        private void InitializeCosmetics()
        {
            // WEAPON SKINS
            cosmeticDatabase["skin_rifle_desert"] = new CosmeticItem { cosmeticId = "skin_rifle_desert", name = "Desert Camo", description = "Tan desert camouflage", type = CosmeticType.WeaponSkin, rarity = CosmeticRarity.Common, targetItem = "weapon_rifle", price = 500, currencyType = Economy.CurrencyType.Soft };
            cosmeticDatabase["skin_rifle_urban"] = new CosmeticItem { cosmeticId = "skin_rifle_urban", name = "Urban Ops", description = "Urban digital camo", type = CosmeticType.WeaponSkin, rarity = CosmeticRarity.Rare, targetItem = "weapon_rifle", price = 100, currencyType = Economy.CurrencyType.Hard };
            cosmeticDatabase["skin_rifle_legendary"] = new CosmeticItem { cosmeticId = "skin_rifle_legendary", name = "Dragon's Breath", description = "Fiery legendary skin", type = CosmeticType.WeaponSkin, rarity = CosmeticRarity.Legendary, targetItem = "weapon_rifle", price = 500, currencyType = Economy.CurrencyType.Hard, animated = true };

            // CHARACTER SKINS
            cosmeticDatabase["skin_char_military"] = new CosmeticItem { cosmeticId = "skin_char_military", name = "Military Spec", description = "Special forces outfit", type = CosmeticType.CharacterSkin, rarity = CosmeticRarity.Epic, price = 200, currencyType = Economy.CurrencyType.Hard };
            cosmeticDatabase["skin_char_survivor"] = new CosmeticItem { cosmeticId = "skin_char_survivor", name = "Wasteland Survivor", description = "Post-apocalyptic gear", type = CosmeticType.CharacterSkin, rarity = CosmeticRarity.Rare, price = 1000, currencyType = Economy.CurrencyType.Soft };
            cosmeticDatabase["skin_char_legendary"] = new CosmeticItem { cosmeticId = "skin_char_legendary", name = "Apocalypse Lord", description = "Ultimate survivor outfit", type = CosmeticType.CharacterSkin, rarity = CosmeticRarity.Legendary, price = 1000, currencyType = Economy.CurrencyType.Hard, seasonExclusive = 1 };

            // EMOTES
            cosmeticDatabase["emote_wave"] = new CosmeticItem { cosmeticId = "emote_wave", name = "Wave", description = "Friendly wave", type = CosmeticType.Emote, rarity = CosmeticRarity.Common, price = 50, currencyType = Economy.CurrencyType.Soft };
            cosmeticDatabase["emote_dance"] = new CosmeticItem { cosmeticId = "emote_dance", name = "Victory Dance", description = "Celebrate your victory", type = CosmeticType.Emote, rarity = CosmeticRarity.Rare, price = 50, currencyType = Economy.CurrencyType.Hard };
            cosmeticDatabase["emote_taunt"] = new CosmeticItem { cosmeticId = "emote_taunt", name = "Taunt", description = "Taunt your enemies", type = CosmeticType.Emote, rarity = CosmeticRarity.Epic, price = 100, currencyType = Economy.CurrencyType.Hard };

            // EFFECTS
            cosmeticDatabase["effect_blood_aura"] = new CosmeticItem { cosmeticId = "effect_blood_aura", name = "Blood Aura", description = "Red combat aura", type = CosmeticType.Effect, rarity = CosmeticRarity.Epic, price = 200, currencyType = Economy.CurrencyType.Hard, animated = true };
            cosmeticDatabase["effect_immortal_glow"] = new CosmeticItem { cosmeticId = "effect_immortal_glow", name = "Immortal Glow", description = "Golden immortal effect", type = CosmeticType.Effect, rarity = CosmeticRarity.Legendary, price = 500, currencyType = Economy.CurrencyType.Hard, animated = true };

            // CHARMS
            cosmeticDatabase["charm_bullet"] = new CosmeticItem { cosmeticId = "charm_bullet", name = "Lucky Bullet", description = "Weapon charm", type = CosmeticType.Charm, rarity = CosmeticRarity.Common, price = 100, currencyType = Economy.CurrencyType.Soft };
            cosmeticDatabase["charm_skull"] = new CosmeticItem { cosmeticId = "charm_skull", name = "Skull Trophy", description = "Menacing charm", type = CosmeticType.Charm, rarity = CosmeticRarity.Rare, price = 50, currencyType = Economy.CurrencyType.Hard };

            Debug.Log($"Initialized {cosmeticDatabase.Count} cosmetic items");
        }

        [ServerRpc(RequireOwnership = false)]
        public void UnlockCosmeticServerRpc(ulong playerId, string cosmeticId, ServerRpcParams rpcParams = default)
        {
            if (!playerCosmetics.ContainsKey(playerId))
            {
                playerCosmetics[playerId] = new PlayerCosmetics { playerId = playerId, unlockedCosmetics = new List<string>(), equippedCosmetics = new Dictionary<CosmeticType, string>() };
            }

            var data = playerCosmetics[playerId];
            if (!data.unlockedCosmetics.Contains(cosmeticId))
            {
                data.unlockedCosmetics.Add(cosmeticId);
                OnCosmeticUnlocked?.Invoke(playerId, cosmeticId);
                Debug.Log($"Player {playerId} unlocked cosmetic {cosmeticId}");
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void EquipCosmeticServerRpc(ulong playerId, string cosmeticId, ServerRpcParams rpcParams = default)
        {
            if (!playerCosmetics.TryGetValue(playerId, out var data)) return;
            if (!data.unlockedCosmetics.Contains(cosmeticId)) return;
            if (!cosmeticDatabase.TryGetValue(cosmeticId, out var cosmetic)) return;

            data.equippedCosmetics[cosmetic.type] = cosmeticId;
            OnCosmeticEquipped?.Invoke(playerId, cosmeticId);
        }

        public CosmeticItem GetCosmetic(string cosmeticId) => cosmeticDatabase.GetValueOrDefault(cosmeticId);
        public PlayerCosmetics GetPlayerCosmetics(ulong playerId) => playerCosmetics.GetValueOrDefault(playerId);
    }

    [Serializable]
    public class CosmeticItem
    {
        public string cosmeticId;
        public string name;
        public string description;
        public CosmeticType type;
        public CosmeticRarity rarity;
        public string targetItem;
        public int price;
        public Economy.CurrencyType currencyType;
        public int seasonExclusive;
        public bool animated;
    }

    [Serializable]
    public class PlayerCosmetics
    {
        public ulong playerId;
        public List<string> unlockedCosmetics;
        public Dictionary<CosmeticType, string> equippedCosmetics;
    }

    public enum CosmeticType { WeaponSkin, CharacterSkin, Emote, Effect, Charm, Spray, Banner }
    public enum CosmeticRarity { Common, Uncommon, Rare, Epic, Legendary, Mythic }
}
