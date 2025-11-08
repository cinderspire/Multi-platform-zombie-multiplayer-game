using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;
using System;

namespace DeadFrontier.Gameplay
{
    /// <summary>
    /// Manages player inventory, loot collection, and item storage for extraction shooter gameplay.
    /// Items collected in-raid are transferred to permanent stash upon successful extraction.
    /// </summary>
    public class InventoryManager : NetworkBehaviour
    {
        public static InventoryManager Instance { get; private set; }

        [Header("Inventory Settings")]
        [SerializeField] private int defaultInventorySlots = 20;
        [SerializeField] private float maxCarryWeight = 50f;
        [SerializeField] private float weightSpeedPenalty = 0.02f; // Speed reduction per kg

        [Header("Stash Settings")]
        [SerializeField] private int defaultStashSize = 100;
        [SerializeField] private int maxStashSize = 500;

        [Header("Rarity Colors")]
        [SerializeField] private Color commonColor = Color.white;
        [SerializeField] private Color uncommonColor = Color.green;
        [SerializeField] private Color rareColor = Color.blue;
        [SerializeField] private Color epicColor = new Color(0.6f, 0f, 1f);
        [SerializeField] private Color legendaryColor = Color.yellow;

        // Player inventories (in-raid)
        private Dictionary<ulong, PlayerInventory> playerInventories = new Dictionary<ulong, PlayerInventory>();

        // Persistent stashes (between raids)
        private Dictionary<string, PlayerStash> playerStashes = new Dictionary<string, PlayerStash>();

        // Active loot in world
        private List<LootItem> worldLoot = new List<LootItem>();

        // Events
        public event Action<ulong, LootItemData> OnItemPickedUp;
        public event Action<ulong, LootItemData> OnItemDropped;
        public event Action<ulong, LootItemData> OnItemUsed;
        public event Action<ulong, float> OnInventoryWeightChanged;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                InitializeInventories();
            }
        }

        private void InitializeInventories()
        {
            // Initialize inventories for all connected players
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                CreatePlayerInventory(client.ClientId);
            }
        }

        #region Inventory Management

        public void CreatePlayerInventory(ulong playerId)
        {
            if (playerInventories.ContainsKey(playerId)) return;

            var inventory = new PlayerInventory
            {
                playerId = playerId,
                maxSlots = defaultInventorySlots,
                maxWeight = maxCarryWeight,
                items = new List<InventoryItemInstance>()
            };

            playerInventories[playerId] = inventory;

            Debug.Log($"[InventoryManager] Created inventory for player {playerId}");
        }

        public bool AddItem(ulong playerId, LootItemData itemData, int quantity = 1)
        {
            if (!IsServer) return false;
            if (!playerInventories.ContainsKey(playerId)) return false;

            var inventory = playerInventories[playerId];

            // Check if can carry more items
            if (!CanCarryItem(inventory, itemData, quantity))
            {
                NotifyInventoryFullClientRpc(playerId);
                return false;
            }

            // Check for existing stackable item
            if (itemData.isStackable)
            {
                var existingStack = inventory.items.FirstOrDefault(i =>
                    i.itemData.itemId == itemData.itemId && i.quantity < itemData.maxStackSize);

                if (existingStack != null)
                {
                    int spaceInStack = itemData.maxStackSize - existingStack.quantity;
                    int toAdd = Mathf.Min(quantity, spaceInStack);
                    existingStack.quantity += toAdd;
                    quantity -= toAdd;

                    if (quantity <= 0)
                    {
                        UpdateInventoryClientRpc(playerId, SerializeInventory(inventory));
                        OnItemPickedUp?.Invoke(playerId, itemData);
                        return true;
                    }
                }
            }

            // Add new stack(s)
            while (quantity > 0)
            {
                if (inventory.items.Count >= inventory.maxSlots)
                {
                    NotifyInventoryFullClientRpc(playerId);
                    return false;
                }

                int stackSize = itemData.isStackable ? Mathf.Min(quantity, itemData.maxStackSize) : 1;

                var newItem = new InventoryItemInstance
                {
                    instanceId = Guid.NewGuid().ToString(),
                    itemData = itemData,
                    quantity = stackSize,
                    durability = itemData.maxDurability,
                    acquiredTime = DateTime.UtcNow
                };

                inventory.items.Add(newItem);
                quantity -= stackSize;
            }

            UpdateInventoryWeight(inventory);
            UpdateInventoryClientRpc(playerId, SerializeInventory(inventory));
            OnItemPickedUp?.Invoke(playerId, itemData);

            return true;
        }

        public bool RemoveItem(ulong playerId, string itemId, int quantity = 1)
        {
            if (!IsServer) return false;
            if (!playerInventories.ContainsKey(playerId)) return false;

            var inventory = playerInventories[playerId];
            int remaining = quantity;

            for (int i = inventory.items.Count - 1; i >= 0 && remaining > 0; i--)
            {
                var item = inventory.items[i];
                if (item.itemData.itemId == itemId)
                {
                    if (item.quantity <= remaining)
                    {
                        remaining -= item.quantity;
                        inventory.items.RemoveAt(i);
                    }
                    else
                    {
                        item.quantity -= remaining;
                        remaining = 0;
                    }
                }
            }

            UpdateInventoryWeight(inventory);
            UpdateInventoryClientRpc(playerId, SerializeInventory(inventory));

            return remaining == 0;
        }

        public void DropItem(ulong playerId, string instanceId, Vector3 dropPosition)
        {
            if (!IsServer) return;
            if (!playerInventories.ContainsKey(playerId)) return;

            var inventory = playerInventories[playerId];
            var item = inventory.items.FirstOrDefault(i => i.instanceId == instanceId);

            if (item != null)
            {
                // Create world loot
                SpawnWorldLoot(item.itemData, dropPosition, item.quantity);

                // Remove from inventory
                inventory.items.Remove(item);
                UpdateInventoryWeight(inventory);
                UpdateInventoryClientRpc(playerId, SerializeInventory(inventory));

                OnItemDropped?.Invoke(playerId, item.itemData);
            }
        }

        public bool UseItem(ulong playerId, string instanceId)
        {
            if (!IsServer) return false;
            if (!playerInventories.ContainsKey(playerId)) return false;

            var inventory = playerInventories[playerId];
            var item = inventory.items.FirstOrDefault(i => i.instanceId == instanceId);

            if (item == null || !item.itemData.isConsumable) return false;

            // Apply item effects
            ApplyItemEffects(playerId, item.itemData);

            // Reduce quantity or remove
            item.quantity--;
            if (item.quantity <= 0)
            {
                inventory.items.Remove(item);
            }

            UpdateInventoryWeight(inventory);
            UpdateInventoryClientRpc(playerId, SerializeInventory(inventory));

            OnItemUsed?.Invoke(playerId, item.itemData);

            return true;
        }

        private bool CanCarryItem(PlayerInventory inventory, LootItemData itemData, int quantity)
        {
            // Check weight
            float itemWeight = itemData.weight * quantity;
            if (inventory.currentWeight + itemWeight > inventory.maxWeight)
                return false;

            // Check slots
            if (!itemData.isStackable)
            {
                return inventory.items.Count + quantity <= inventory.maxSlots;
            }

            return true;
        }

        private void UpdateInventoryWeight(PlayerInventory inventory)
        {
            float totalWeight = 0f;

            foreach (var item in inventory.items)
            {
                totalWeight += item.itemData.weight * item.quantity;
            }

            inventory.currentWeight = totalWeight;
            OnInventoryWeightChanged?.Invoke(inventory.playerId, totalWeight);

            // Apply movement penalty
            ApplyWeightPenalty(inventory.playerId, totalWeight);
        }

        private void ApplyWeightPenalty(ulong playerId, float weight)
        {
            float overweight = Mathf.Max(0f, weight - (maxCarryWeight * 0.8f));
            float penalty = overweight * weightSpeedPenalty;

            // Apply to player movement
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerId, out NetworkObject netObj))
            {
                var movement = netObj.GetComponent<Player.PlayerMovement>();
                if (movement != null)
                {
                    movement.SetSpeedMultiplier(1f - Mathf.Clamp01(penalty));
                }
            }
        }

        private void ApplyItemEffects(ulong playerId, LootItemData itemData)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerId, out NetworkObject netObj))
            {
                var health = netObj.GetComponent<Player.PlayerHealth>();

                foreach (var effect in itemData.effects)
                {
                    switch (effect.effectType)
                    {
                        case ItemEffectType.RestoreHealth:
                            health?.Heal(effect.value);
                            break;

                        case ItemEffectType.RestoreStamina:
                            var movement = netObj.GetComponent<Player.PlayerMovement>();
                            movement?.RestoreStamina(effect.value);
                            break;

                        case ItemEffectType.TempSpeedBoost:
                            var movement2 = netObj.GetComponent<Player.PlayerMovement>();
                            movement2?.ApplySpeedBoost(effect.value, effect.duration);
                            break;

                        case ItemEffectType.TempDamageBoost:
                            // Apply damage boost via buff system
                            break;
                    }
                }
            }
        }

        #endregion

        #region World Loot

        public void SpawnWorldLoot(LootItemData itemData, Vector3 position, int quantity = 1)
        {
            if (!IsServer) return;

            // Create loot GameObject
            GameObject lootObj = new GameObject($"Loot_{itemData.itemName}");
            lootObj.transform.position = position;

            var lootItem = lootObj.AddComponent<LootItem>();
            lootItem.Initialize(itemData, quantity);

            worldLoot.Add(lootItem);

            // Network spawn
            var netObj = lootObj.AddComponent<NetworkObject>();
            netObj.Spawn();

            Debug.Log($"[InventoryManager] Spawned {quantity}x {itemData.itemName} at {position}");
        }

        public void PickupWorldLoot(ulong playerId, LootItem lootItem)
        {
            if (!IsServer) return;

            if (AddItem(playerId, lootItem.ItemData, lootItem.Quantity))
            {
                worldLoot.Remove(lootItem);
                lootItem.GetComponent<NetworkObject>()?.Despawn();
                Destroy(lootItem.gameObject);
            }
        }

        #endregion

        #region Stash Management

        public void TransferInventoryToStash(ulong playerId)
        {
            if (!IsServer) return;
            if (!playerInventories.ContainsKey(playerId)) return;

            var inventory = playerInventories[playerId];
            string playerGuid = GetPlayerGuid(playerId);

            if (!playerStashes.ContainsKey(playerGuid))
            {
                playerStashes[playerGuid] = new PlayerStash
                {
                    playerGuid = playerGuid,
                    maxSize = defaultStashSize,
                    items = new List<InventoryItemInstance>()
                };
            }

            var stash = playerStashes[playerGuid];

            // Transfer all items to stash
            foreach (var item in inventory.items)
            {
                if (stash.items.Count < stash.maxSize)
                {
                    stash.items.Add(item);
                }
            }

            // Clear inventory
            inventory.items.Clear();
            UpdateInventoryWeight(inventory);

            // Save stash
            SavePlayerStash(playerGuid);

            Debug.Log($"[InventoryManager] Transferred inventory to stash for player {playerId}");
        }

        public int GetInventoryValue(ulong playerId)
        {
            if (!playerInventories.ContainsKey(playerId)) return 0;

            var inventory = playerInventories[playerId];
            int totalValue = 0;

            foreach (var item in inventory.items)
            {
                totalValue += item.itemData.baseValue * item.quantity;
            }

            return totalValue;
        }

        private string GetPlayerGuid(ulong playerId)
        {
            // In production, this would map network ID to persistent player ID
            return playerId.ToString();
        }

        private void SavePlayerStash(string playerGuid)
        {
            if (!playerStashes.ContainsKey(playerGuid)) return;

            // Save to persistent storage
            if (Core.SaveSystem.Instance != null)
            {
                string stashData = JsonUtility.ToJson(playerStashes[playerGuid]);
                Core.SaveSystem.Instance.SaveData($"stash_{playerGuid}", stashData);
            }
        }

        #endregion

        #region Client RPCs

        [ClientRpc]
        private void UpdateInventoryClientRpc(ulong playerId, string inventoryJson)
        {
            // Update local inventory UI
            // Parse inventoryJson and update display
        }

        [ClientRpc]
        private void NotifyInventoryFullClientRpc(ulong playerId)
        {
            Core.NotificationManager.Instance?.ShowNotification(
                "Inventory full!",
                NotificationType.Warning,
                2f
            );
        }

        #endregion

        #region Serialization

        private string SerializeInventory(PlayerInventory inventory)
        {
            return JsonUtility.ToJson(inventory);
        }

        #endregion

        #region Public Getters

        public PlayerInventory GetPlayerInventory(ulong playerId)
        {
            return playerInventories.ContainsKey(playerId) ? playerInventories[playerId] : null;
        }

        public List<InventoryItemInstance> GetInventoryItems(ulong playerId)
        {
            if (playerInventories.ContainsKey(playerId))
                return new List<InventoryItemInstance>(playerInventories[playerId].items);
            return new List<InventoryItemInstance>();
        }

        public float GetCurrentWeight(ulong playerId)
        {
            return playerInventories.ContainsKey(playerId) ? playerInventories[playerId].currentWeight : 0f;
        }

        public float GetMaxWeight(ulong playerId)
        {
            return playerInventories.ContainsKey(playerId) ? playerInventories[playerId].maxWeight : 0f;
        }

        #endregion
    }

    #region Data Classes

    [System.Serializable]
    public class PlayerInventory
    {
        public ulong playerId;
        public int maxSlots;
        public float maxWeight;
        public float currentWeight;
        public List<InventoryItemInstance> items;
    }

    [System.Serializable]
    public class PlayerStash
    {
        public string playerGuid;
        public int maxSize;
        public List<InventoryItemInstance> items;
    }

    [System.Serializable]
    public class InventoryItemInstance
    {
        public string instanceId;
        public LootItemData itemData;
        public int quantity;
        public float durability;
        public DateTime acquiredTime;
    }

    [System.Serializable]
    public class LootItemData
    {
        public string itemId;
        public string itemName;
        public string description;
        public ItemRarity rarity;
        public ItemCategory category;

        public float weight;
        public int baseValue;

        public bool isStackable;
        public int maxStackSize;

        public bool isConsumable;
        public float maxDurability;

        public Sprite icon;
        public GameObject worldModel;

        public ItemEffect[] effects;
    }

    [System.Serializable]
    public class ItemEffect
    {
        public ItemEffectType effectType;
        public float value;
        public float duration;
    }

    public enum ItemRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    public enum ItemCategory
    {
        Weapon,
        Ammo,
        Medical,
        Food,
        Attachment,
        Material,
        KeyItem,
        Valuable,
        Quest
    }

    public enum ItemEffectType
    {
        RestoreHealth,
        RestoreStamina,
        TempSpeedBoost,
        TempDamageBoost,
        TempDefenseBoost,
        RemoveStatus
    }

    #endregion
}
