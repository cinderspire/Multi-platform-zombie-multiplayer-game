using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Gameplay
{
    /// <summary>
    /// Automatic loot collection system allowing players to configure which items
    /// are automatically picked up based on rarity, type, and priority.
    /// </summary>
    public class AutoLootSystem : NetworkBehaviour
    {
        public static AutoLootSystem Instance { get; private set; }

        [Header("Auto Loot Configuration")]
        [SerializeField] private float autoLootRadius = 3f;
        [SerializeField] private float autoLootCheckInterval = 0.5f;
        [SerializeField] private bool enableAutoLootByDefault = false;

        private Dictionary<ulong, AutoLootSettings> playerSettings = new Dictionary<ulong, AutoLootSettings>();
        private Dictionary<ulong, float> lastCheckTime = new Dictionary<ulong, float>();

        public event Action<ulong, string, int> OnItemAutoLooted;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Update()
        {
            if (IsServer)
            {
                CheckAutoLootForPlayers();
            }
        }

        /// <summary>
        /// Initialize auto-loot settings for player
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void InitializeAutoLootServerRpc(ulong playerId, ServerRpcParams rpcParams = default)
        {
            if (!playerSettings.ContainsKey(playerId))
            {
                playerSettings[playerId] = new AutoLootSettings
                {
                    enabled = enableAutoLootByDefault,
                    lootRadius = autoLootRadius,
                    autoLootByRarity = new Dictionary<Loot.LootDropSystem.LootRarity, bool>
                    {
                        { Loot.LootDropSystem.LootRarity.Common, false },
                        { Loot.LootDropSystem.LootRarity.Uncommon, false },
                        { Loot.LootDropSystem.LootRarity.Rare, true },
                        { Loot.LootDropSystem.LootRarity.Epic, true },
                        { Loot.LootDropSystem.LootRarity.Legendary, true },
                        { Loot.LootDropSystem.LootRarity.Mythic, true }
                    },
                    autoLootByType = new Dictionary<ItemType, bool>
                    {
                        { ItemType.Weapon, false },
                        { ItemType.Ammo, true },
                        { ItemType.Health, true },
                        { ItemType.Armor, false },
                        { ItemType.Resource, true },
                        { ItemType.Currency, true },
                        { ItemType.Consumable, true },
                        { ItemType.Quest, true },
                        { ItemType.Cosmetic, false }
                    }
                };
            }
        }

        private void CheckAutoLootForPlayers()
        {
            foreach (var client in NetworkManager.Singleton.ConnectedClients)
            {
                ulong playerId = client.Key;

                if (!lastCheckTime.ContainsKey(playerId))
                {
                    lastCheckTime[playerId] = 0f;
                }

                if (Time.time - lastCheckTime[playerId] < autoLootCheckInterval) continue;
                lastCheckTime[playerId] = Time.time;

                if (!playerSettings.TryGetValue(playerId, out var settings)) continue;
                if (!settings.enabled) continue;

                CheckAndAutoLoot(playerId, settings);
            }
        }

        private void CheckAndAutoLoot(ulong playerId, AutoLootSettings settings)
        {
            // Get player position (would need actual player reference)
            Vector3 playerPosition = GetPlayerPosition(playerId);
            if (playerPosition == Vector3.zero) return;

            // Find nearby loot items
            Collider[] nearbyItems = Physics.OverlapSphere(playerPosition, settings.lootRadius);

            foreach (Collider item in nearbyItems)
            {
                // Check if it's a loot item
                var lootItem = item.GetComponent<LootItem>();
                if (lootItem == null) continue;

                // Check if should auto-loot based on settings
                if (ShouldAutoLoot(lootItem, settings))
                {
                    PickupItem(playerId, lootItem);
                }
            }
        }

        private bool ShouldAutoLoot(LootItem item, AutoLootSettings settings)
        {
            // Check rarity filter
            if (settings.autoLootByRarity.TryGetValue(item.rarity, out bool rarityEnabled) && !rarityEnabled)
            {
                return false;
            }

            // Check type filter
            if (settings.autoLootByType.TryGetValue(item.itemType, out bool typeEnabled) && !typeEnabled)
            {
                return false;
            }

            // Check inventory space
            if (!HasInventorySpace(item.ownerPlayerId, item.itemType))
            {
                return false;
            }

            return true;
        }

        private void PickupItem(ulong playerId, LootItem item)
        {
            // Add to inventory
            if (Inventory.InventorySystem.Instance != null)
            {
                Inventory.InventorySystem.Instance.AddItemServerRpc(playerId, item.itemId, item.quantity);
            }

            OnItemAutoLooted?.Invoke(playerId, item.itemId, item.quantity);

            // Destroy loot object
            if (item.gameObject != null)
            {
                Destroy(item.gameObject);
            }
        }

        /// <summary>
        /// Toggle auto-loot on/off
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void SetAutoLootEnabledServerRpc(ulong playerId, bool enabled, ServerRpcParams rpcParams = default)
        {
            if (!playerSettings.TryGetValue(playerId, out var settings)) return;
            settings.enabled = enabled;

            NotifySettingChangedClientRpc(playerId, $"Auto-loot {(enabled ? "enabled" : "disabled")}");
        }

        /// <summary>
        /// Configure auto-loot by rarity
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void SetAutoLootByRarityServerRpc(ulong playerId, Loot.LootDropSystem.LootRarity rarity,
            bool enabled, ServerRpcParams rpcParams = default)
        {
            if (!playerSettings.TryGetValue(playerId, out var settings)) return;
            settings.autoLootByRarity[rarity] = enabled;
        }

        /// <summary>
        /// Configure auto-loot by item type
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void SetAutoLootByTypeServerRpc(ulong playerId, ItemType itemType,
            bool enabled, ServerRpcParams rpcParams = default)
        {
            if (!playerSettings.TryGetValue(playerId, out var settings)) return;
            settings.autoLootByType[itemType] = enabled;
        }

        /// <summary>
        /// Set auto-loot radius
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void SetAutoLootRadiusServerRpc(ulong playerId, float radius, ServerRpcParams rpcParams = default)
        {
            if (!playerSettings.TryGetValue(playerId, out var settings)) return;
            settings.lootRadius = Mathf.Clamp(radius, 1f, 10f);
        }

        [ClientRpc]
        private void NotifySettingChangedClientRpc(ulong playerId, string message)
        {
            if (NetworkManager.Singleton.LocalClientId == playerId)
            {
                Debug.Log(message);
            }
        }

        private Vector3 GetPlayerPosition(ulong playerId)
        {
            // Would get actual player position from player manager
            return Vector3.zero;
        }

        private bool HasInventorySpace(ulong playerId, ItemType itemType)
        {
            // Check inventory system for space
            return true; // Simplified
        }

        [Serializable]
        private class AutoLootSettings
        {
            public bool enabled;
            public float lootRadius;
            public Dictionary<Loot.LootDropSystem.LootRarity, bool> autoLootByRarity;
            public Dictionary<ItemType, bool> autoLootByType;
        }

        [Serializable]
        private class LootItem : MonoBehaviour
        {
            public ulong ownerPlayerId;
            public string itemId;
            public int quantity;
            public Loot.LootDropSystem.LootRarity rarity;
            public ItemType itemType;
        }

        public enum ItemType
        {
            Weapon,
            Ammo,
            Health,
            Armor,
            Resource,
            Currency,
            Consumable,
            Quest,
            Cosmetic
        }
    }
}
