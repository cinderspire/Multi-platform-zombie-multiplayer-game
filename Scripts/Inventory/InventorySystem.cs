using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Inventory
{
    /// <summary>
    /// Comprehensive inventory system with equipment slots, weight management, item durability,
    /// multiple inventory types, and complete item database with all stats and properties.
    /// </summary>
    public class InventorySystem : NetworkBehaviour
    {
        public static InventorySystem Instance { get; private set; }

        [Header("Inventory Configuration")]
        [SerializeField] private int defaultBackpackSlots = 30;
        [SerializeField] private int defaultStashSlots = 100;
        [SerializeField] private float defaultWeightCapacity = 100f;
        [SerializeField] private int quickSlotCount = 4;
        [SerializeField] private bool enableDurability = true;
        [SerializeField] private bool enableWeight = true;

        // Player inventories
        private Dictionary<ulong, PlayerInventory> playerInventories = new Dictionary<ulong, PlayerInventory>();
        
        // Item database
        private Dictionary<string, ItemDefinition> itemDatabase = new Dictionary<string, ItemDefinition>();

        // Events
        public event Action<ulong, string, int> OnItemAdded;
        public event Action<ulong, string, int> OnItemRemoved;
        public event Action<ulong, string, EquipmentSlot> OnItemEquipped;
        public event Action<ulong, EquipmentSlot> OnItemUnequipped;
        public event Action<ulong> OnInventoryUpdated;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer)
            {
                InitializeItemDatabase();
            }
        }

        private void InitializeItemDatabase()
        {
            // ===== WEAPONS =====
            
            itemDatabase["weapon_pistol_glock"] = new ItemDefinition
            {
                itemId = "weapon_pistol_glock",
                itemName = "Glock 17",
                description = "Reliable 9mm pistol with high capacity",
                itemType = ItemType.Weapon,
                weaponType = WeaponType.Pistol,
                rarity = ItemRarity.Common,
                maxStack = 1,
                weight = 2.5f,
                value = 500,
                maxDurability = 100,
                equipSlot = EquipmentSlot.Primary,
                stats = new ItemStats
                {
                    damage = 25f,
                    fireRate = 0.15f,
                    magazineSize = 17,
                    reloadTime = 2.0f,
                    range = 50f
                },
                requiredLevel = 1
            };

            itemDatabase["weapon_rifle_m4"] = new ItemDefinition
            {
                itemId = "weapon_rifle_m4",
                itemName = "M4 Carbine",
                description = "Versatile assault rifle with excellent accuracy",
                itemType = ItemType.Weapon,
                weaponType = WeaponType.AssaultRifle,
                rarity = ItemRarity.Rare,
                maxStack = 1,
                weight = 7.5f,
                value = 5000,
                maxDurability = 150,
                equipSlot = EquipmentSlot.Primary,
                stats = new ItemStats
                {
                    damage = 35f,
                    fireRate = 0.1f,
                    magazineSize = 30,
                    reloadTime = 2.5f,
                    range = 100f,
                    accuracy = 0.88f
                },
                requiredLevel = 15
            };

            itemDatabase["weapon_shotgun_remington"] = new ItemDefinition
            {
                itemId = "weapon_shotgun_remington",
                itemName = "Remington 870",
                description = "Pump-action shotgun devastating at close range",
                itemType = ItemType.Weapon,
                weaponType = WeaponType.Shotgun,
                rarity = ItemRarity.Uncommon,
                maxStack = 1,
                weight = 8.0f,
                value = 2500,
                maxDurability = 120,
                equipSlot = EquipmentSlot.Primary,
                stats = new ItemStats
                {
                    damage = 80f,
                    fireRate = 0.8f,
                    magazineSize = 8,
                    reloadTime = 4.0f,
                    range = 30f
                },
                requiredLevel = 10
            };

            itemDatabase["weapon_sniper_awp"] = new ItemDefinition
            {
                itemId = "weapon_sniper_awp",
                itemName = "AWP Sniper Rifle",
                description = "High-powered bolt-action sniper rifle",
                itemType = ItemType.Weapon,
                weaponType = WeaponType.Sniper,
                rarity = ItemRarity.Epic,
                maxStack = 1,
                weight = 12.0f,
                value = 8000,
                maxDurability = 150,
                equipSlot = EquipmentSlot.Primary,
                stats = new ItemStats
                {
                    damage = 120f,
                    fireRate = 1.5f,
                    magazineSize = 5,
                    reloadTime = 3.5f,
                    range = 300f,
                    accuracy = 0.95f
                },
                requiredLevel = 25
            };

            itemDatabase["weapon_melee_katana"] = new ItemDefinition
            {
                itemId = "weapon_melee_katana",
                itemName = "Katana",
                description = "Sharp blade for silent takedowns",
                itemType = ItemType.Weapon,
                weaponType = WeaponType.Melee,
                rarity = ItemRarity.Rare,
                maxStack = 1,
                weight = 3.0f,
                value = 3000,
                maxDurability = 200,
                equipSlot = EquipmentSlot.Melee,
                stats = new ItemStats
                {
                    damage = 60f,
                    fireRate = 0.5f,
                    range = 3f
                },
                requiredLevel = 12
            };

            // ===== ARMOR =====

            itemDatabase["armor_helmet_basic"] = new ItemDefinition
            {
                itemId = "armor_helmet_basic",
                itemName = "Basic Helmet",
                description = "Provides basic head protection",
                itemType = ItemType.Armor,
                armorType = ArmorType.Helmet,
                rarity = ItemRarity.Common,
                maxStack = 1,
                weight = 2.0f,
                value = 300,
                maxDurability = 80,
                equipSlot = EquipmentSlot.Head,
                stats = new ItemStats
                {
                    armor = 15f,
                    damageReduction = 0.10f
                },
                requiredLevel = 1
            };

            itemDatabase["armor_helmet_tactical"] = new ItemDefinition
            {
                itemId = "armor_helmet_tactical",
                itemName = "Tactical Helmet",
                description = "Military-grade head protection with night vision mount",
                itemType = ItemType.Armor,
                armorType = ArmorType.Helmet,
                rarity = ItemRarity.Rare,
                maxStack = 1,
                weight = 3.5f,
                value = 2000,
                maxDurability = 150,
                equipSlot = EquipmentSlot.Head,
                stats = new ItemStats
                {
                    armor = 35f,
                    damageReduction = 0.25f
                },
                requiredLevel = 20
            };

            itemDatabase["armor_vest_light"] = new ItemDefinition
            {
                itemId = "armor_vest_light",
                itemName = "Light Vest",
                description = "Lightweight protection for mobility",
                itemType = ItemType.Armor,
                armorType = ArmorType.Vest,
                rarity = ItemRarity.Common,
                maxStack = 1,
                weight = 5.0f,
                value = 500,
                maxDurability = 100,
                equipSlot = EquipmentSlot.Chest,
                stats = new ItemStats
                {
                    armor = 25f,
                    damageReduction = 0.15f,
                    movementSpeed = 0.05f
                },
                requiredLevel = 1
            };

            itemDatabase["armor_vest_heavy"] = new ItemDefinition
            {
                itemId = "armor_vest_heavy",
                itemName = "Heavy Ballistic Vest",
                description = "Maximum protection with reduced mobility",
                itemType = ItemType.Armor,
                armorType = ArmorType.Vest,
                rarity = ItemRarity.Epic,
                maxStack = 1,
                weight = 15.0f,
                value = 6000,
                maxDurability = 200,
                equipSlot = EquipmentSlot.Chest,
                stats = new ItemStats
                {
                    armor = 75f,
                    damageReduction = 0.40f,
                    movementSpeed = -0.10f
                },
                requiredLevel = 30
            };

            itemDatabase["armor_gloves_tactical"] = new ItemDefinition
            {
                itemId = "armor_gloves_tactical",
                itemName = "Tactical Gloves",
                description = "Improves weapon handling and reload speed",
                itemType = ItemType.Armor,
                armorType = ArmorType.Gloves,
                rarity = ItemRarity.Uncommon,
                maxStack = 1,
                weight = 0.5f,
                value = 800,
                maxDurability = 100,
                equipSlot = EquipmentSlot.Hands,
                stats = new ItemStats
                {
                    armor = 5f,
                    reloadSpeed = 0.10f,
                    accuracy = 0.05f
                },
                requiredLevel = 8
            };

            itemDatabase["armor_boots_combat"] = new ItemDefinition
            {
                itemId = "armor_boots_combat",
                itemName = "Combat Boots",
                description = "Durable boots for all terrain",
                itemType = ItemType.Armor,
                armorType = ArmorType.Boots,
                rarity = ItemRarity.Common,
                maxStack = 1,
                weight = 2.0f,
                value = 400,
                maxDurability = 120,
                equipSlot = EquipmentSlot.Feet,
                stats = new ItemStats
                {
                    armor = 10f,
                    movementSpeed = 0.05f,
                    staminaRegen = 0.10f
                },
                requiredLevel = 1
            };

            itemDatabase["armor_backpack_medium"] = new ItemDefinition
            {
                itemId = "armor_backpack_medium",
                itemName = "Medium Backpack",
                description = "Increases carrying capacity",
                itemType = ItemType.Armor,
                armorType = ArmorType.Backpack,
                rarity = ItemRarity.Uncommon,
                maxStack = 1,
                weight = 2.0f,
                value = 1000,
                maxDurability = 100,
                equipSlot = EquipmentSlot.Back,
                stats = new ItemStats
                {
                    inventorySlots = 10,
                    weightCapacity = 20f
                },
                requiredLevel = 5
            };

            itemDatabase["armor_backpack_large"] = new ItemDefinition
            {
                itemId = "armor_backpack_large",
                itemName = "Large Tactical Backpack",
                description = "Maximum storage capacity",
                itemType = ItemType.Armor,
                armorType = ArmorType.Backpack,
                rarity = ItemRarity.Rare,
                maxStack = 1,
                weight = 3.5f,
                value = 3000,
                maxDurability = 150,
                equipSlot = EquipmentSlot.Back,
                stats = new ItemStats
                {
                    inventorySlots = 20,
                    weightCapacity = 40f
                },
                requiredLevel = 15
            };

            // ===== CONSUMABLES =====

            itemDatabase["consumable_medkit"] = new ItemDefinition
            {
                itemId = "consumable_medkit",
                itemName = "Medical Kit",
                description = "Restores 75 health over 5 seconds",
                itemType = ItemType.Consumable,
                consumableType = ConsumableType.Health,
                rarity = ItemRarity.Common,
                maxStack = 10,
                weight = 0.5f,
                value = 100,
                stats = new ItemStats
                {
                    healAmount = 75f,
                    useTime = 5f
                },
                requiredLevel = 1
            };

            itemDatabase["consumable_bandage"] = new ItemDefinition
            {
                itemId = "consumable_bandage",
                itemName = "Bandage",
                description = "Restores 25 health quickly",
                itemType = ItemType.Consumable,
                consumableType = ConsumableType.Health,
                rarity = ItemRarity.Common,
                maxStack = 20,
                weight = 0.1f,
                value = 25,
                stats = new ItemStats
                {
                    healAmount = 25f,
                    useTime = 2f
                },
                requiredLevel = 1
            };

            itemDatabase["consumable_energy_drink"] = new ItemDefinition
            {
                itemId = "consumable_energy_drink",
                itemName = "Energy Drink",
                description = "Restores stamina and increases speed temporarily",
                itemType = ItemType.Consumable,
                consumableType = ConsumableType.Stamina,
                rarity = ItemRarity.Uncommon,
                maxStack = 10,
                weight = 0.3f,
                value = 50,
                stats = new ItemStats
                {
                    staminaAmount = 100f,
                    movementSpeed = 0.15f,
                    useTime = 3f,
                    duration = 30f
                },
                requiredLevel = 1
            };

            itemDatabase["consumable_antidote"] = new ItemDefinition
            {
                itemId = "consumable_antidote",
                itemName = "Antidote",
                description = "Cures poison and infection",
                itemType = ItemType.Consumable,
                consumableType = ConsumableType.Buff,
                rarity = ItemRarity.Rare,
                maxStack = 5,
                weight = 0.2f,
                value = 200,
                stats = new ItemStats
                {
                    useTime = 2f
                },
                requiredLevel = 1
            };

            // ===== AMMO =====

            itemDatabase["ammo_9mm"] = new ItemDefinition
            {
                itemId = "ammo_9mm",
                itemName = "9mm Rounds",
                description = "Standard pistol ammunition",
                itemType = ItemType.Ammo,
                ammoType = AmmoType.Pistol9mm,
                rarity = ItemRarity.Common,
                maxStack = 200,
                weight = 0.01f,
                value = 1,
                requiredLevel = 1
            };

            itemDatabase["ammo_556"] = new ItemDefinition
            {
                itemId = "ammo_556",
                itemName = "5.56mm Rounds",
                description = "Rifle ammunition",
                itemType = ItemType.Ammo,
                ammoType = AmmoType.Rifle556,
                rarity = ItemRarity.Common,
                maxStack = 200,
                weight = 0.012f,
                value = 2,
                requiredLevel = 1
            };

            itemDatabase["ammo_762"] = new ItemDefinition
            {
                itemId = "ammo_762",
                itemName = "7.62mm Rounds",
                description = "Heavy rifle and sniper ammunition",
                itemType = ItemType.Ammo,
                ammoType = AmmoType.Rifle762,
                rarity = ItemRarity.Uncommon,
                maxStack = 200,
                weight = 0.015f,
                value = 3,
                requiredLevel = 1
            };

            itemDatabase["ammo_shotgun"] = new ItemDefinition
            {
                itemId = "ammo_shotgun",
                itemName = "12 Gauge Shells",
                description = "Shotgun shells",
                itemType = ItemType.Ammo,
                ammoType = AmmoType.Shotgun12Gauge,
                rarity = ItemRarity.Common,
                maxStack = 100,
                weight = 0.05f,
                value = 5,
                requiredLevel = 1
            };

            // ===== MATERIALS =====

            itemDatabase["material_wood"] = new ItemDefinition
            {
                itemId = "material_wood",
                itemName = "Wood",
                description = "Basic building material",
                itemType = ItemType.Material,
                materialType = MaterialType.Wood,
                rarity = ItemRarity.Common,
                maxStack = 500,
                weight = 0.2f,
                value = 1,
                requiredLevel = 1
            };

            itemDatabase["material_stone"] = new ItemDefinition
            {
                itemId = "material_stone",
                itemName = "Stone",
                description = "Durable building material",
                itemType = ItemType.Material,
                materialType = MaterialType.Stone,
                rarity = ItemRarity.Common,
                maxStack = 500,
                weight = 0.5f,
                value = 2,
                requiredLevel = 1
            };

            itemDatabase["material_metal"] = new ItemDefinition
            {
                itemId = "material_metal",
                itemName = "Metal Scrap",
                description = "Used for advanced crafting",
                itemType = ItemType.Material,
                materialType = MaterialType.Metal,
                rarity = ItemRarity.Uncommon,
                maxStack = 200,
                weight = 1.0f,
                value = 5,
                requiredLevel = 1
            };

            itemDatabase["material_cloth"] = new ItemDefinition
            {
                itemId = "material_cloth",
                itemName = "Cloth",
                description = "Textile material for crafting",
                itemType = ItemType.Material,
                materialType = MaterialType.Cloth,
                rarity = ItemRarity.Common,
                maxStack = 200,
                weight = 0.1f,
                value = 1,
                requiredLevel = 1
            };

            itemDatabase["material_electronics"] = new ItemDefinition
            {
                itemId = "material_electronics",
                itemName = "Electronics",
                description = "Advanced components for high-tech items",
                itemType = ItemType.Material,
                materialType = MaterialType.Electronics,
                rarity = ItemRarity.Rare,
                maxStack = 100,
                weight = 0.3f,
                value = 10,
                requiredLevel = 1
            };

            // ===== QUEST ITEMS =====

            itemDatabase["quest_keycard_red"] = new ItemDefinition
            {
                itemId = "quest_keycard_red",
                itemName = "Red Keycard",
                description = "Grants access to high-security areas",
                itemType = ItemType.QuestItem,
                rarity = ItemRarity.Epic,
                maxStack = 1,
                weight = 0.05f,
                value = 0,
                requiredLevel = 1
            };

            itemDatabase["quest_data_drive"] = new ItemDefinition
            {
                itemId = "quest_data_drive",
                itemName = "Encrypted Data Drive",
                description = "Contains valuable information",
                itemType = ItemType.QuestItem,
                rarity = ItemRarity.Rare,
                maxStack = 1,
                weight = 0.1f,
                value = 0,
                requiredLevel = 1
            };

            // ===== MISC =====

            itemDatabase["misc_repair_kit"] = new ItemDefinition
            {
                itemId = "misc_repair_kit",
                itemName = "Repair Kit",
                description = "Restores 50 durability to weapons and armor",
                itemType = ItemType.Misc,
                rarity = ItemRarity.Uncommon,
                maxStack = 10,
                weight = 1.0f,
                value = 100,
                stats = new ItemStats
                {
                    repairAmount = 50f
                },
                requiredLevel = 1
            };

            itemDatabase["misc_lockpick"] = new ItemDefinition
            {
                itemId = "misc_lockpick",
                itemName = "Lockpick",
                description = "Used to open locked containers",
                itemType = ItemType.Misc,
                rarity = ItemRarity.Common,
                maxStack = 20,
                weight = 0.05f,
                value = 25,
                requiredLevel = 1
            };

            Debug.Log($"Initialized {itemDatabase.Count} item definitions");
        }

        // Main inventory operations
        [ServerRpc(RequireOwnership = false)]
        public void InitializePlayerInventoryServerRpc(ulong playerId, ServerRpcParams rpcParams = default)
        {
            if (playerInventories.ContainsKey(playerId)) return;

            var inventory = new PlayerInventory
            {
                playerId = playerId,
                backpack = new InventoryContainer
                {
                    containerType = ContainerType.Backpack,
                    maxSlots = defaultBackpackSlots,
                    items = new List<InventoryItem>()
                },
                stash = new InventoryContainer
                {
                    containerType = ContainerType.Stash,
                    maxSlots = defaultStashSlots,
                    items = new List<InventoryItem>()
                },
                equipment = new Dictionary<EquipmentSlot, InventoryItem>(),
                quickSlots = new Dictionary<int, string>(),
                currentWeight = 0f,
                maxWeight = defaultWeightCapacity,
                currency = 0
            };

            // Initialize equipment slots
            foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
            {
                inventory.equipment[slot] = null;
            }

            // Initialize quick slots
            for (int i = 0; i < quickSlotCount; i++)
            {
                inventory.quickSlots[i] = null;
            }

            playerInventories[playerId] = inventory;

            // Give starter items
            AddItemServerRpc(playerId, "weapon_pistol_glock", 1, ContainerType.Backpack);
            AddItemServerRpc(playerId, "ammo_9mm", 50, ContainerType.Backpack);
            AddItemServerRpc(playerId, "consumable_bandage", 5, ContainerType.Backpack);
            AddItemServerRpc(playerId, "armor_vest_light", 1, ContainerType.Backpack);

            Debug.Log($"Initialized inventory for player {playerId}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void AddItemServerRpc(ulong playerId, string itemId, int quantity, ContainerType containerType, ServerRpcParams rpcParams = default)
        {
            if (!playerInventories.TryGetValue(playerId, out var inventory)) return;
            if (!itemDatabase.TryGetValue(itemId, out var itemDef)) return;

            var container = GetContainer(inventory, containerType);
            if (container == null) return;

            // Check weight
            if (enableWeight)
            {
                float addedWeight = itemDef.weight * quantity;
                if (inventory.currentWeight + addedWeight > inventory.maxWeight)
                {
                    Debug.LogWarning($"Cannot add item: exceeds weight limit");
                    return;
                }
            }

            // Try to stack with existing items
            if (itemDef.maxStack > 1)
            {
                var existingItem = container.items.FirstOrDefault(i => i.itemId == itemId && i.quantity < itemDef.maxStack);
                if (existingItem != null)
                {
                    int spaceInStack = itemDef.maxStack - existingItem.quantity;
                    int amountToAdd = Mathf.Min(quantity, spaceInStack);
                    existingItem.quantity += amountToAdd;
                    quantity -= amountToAdd;

                    inventory.currentWeight += itemDef.weight * amountToAdd;
                }
            }

            // Add remaining quantity as new items
            while (quantity > 0)
            {
                if (container.items.Count >= container.maxSlots)
                {
                    Debug.LogWarning($"Container full, cannot add more items");
                    break;
                }

                int stackSize = Mathf.Min(quantity, itemDef.maxStack);
                var newItem = new InventoryItem
                {
                    itemId = itemId,
                    quantity = stackSize,
                    durability = itemDef.maxDurability,
                    slotIndex = GetNextAvailableSlot(container)
                };

                container.items.Add(newItem);
                inventory.currentWeight += itemDef.weight * stackSize;
                quantity -= stackSize;
            }

            OnItemAdded?.Invoke(playerId, itemId, quantity);
            NotifyInventoryUpdatedClientRpc(playerId);

            Debug.Log($"Added {quantity}x {itemDef.itemName} to player {playerId}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void RemoveItemServerRpc(ulong playerId, string itemId, int quantity, ContainerType containerType, ServerRpcParams rpcParams = default)
        {
            if (!playerInventories.TryGetValue(playerId, out var inventory)) return;
            if (!itemDatabase.TryGetValue(itemId, out var itemDef)) return;

            var container = GetContainer(inventory, containerType);
            if (container == null) return;

            int remaining = quantity;

            for (int i = container.items.Count - 1; i >= 0 && remaining > 0; i--)
            {
                var item = container.items[i];
                if (item.itemId != itemId) continue;

                int toRemove = Mathf.Min(remaining, item.quantity);
                item.quantity -= toRemove;
                remaining -= toRemove;

                inventory.currentWeight -= itemDef.weight * toRemove;

                if (item.quantity <= 0)
                {
                    container.items.RemoveAt(i);
                }
            }

            OnItemRemoved?.Invoke(playerId, itemId, quantity - remaining);
            NotifyInventoryUpdatedClientRpc(playerId);

            Debug.Log($"Removed {quantity - remaining}x {itemDef.itemName} from player {playerId}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void EquipItemServerRpc(ulong playerId, string itemId, int slotIndex, ContainerType containerType, ServerRpcParams rpcParams = default)
        {
            if (!playerInventories.TryGetValue(playerId, out var inventory)) return;
            if (!itemDatabase.TryGetValue(itemId, out var itemDef)) return;

            var container = GetContainer(inventory, containerType);
            if (container == null) return;

            var item = container.items.FirstOrDefault(i => i.itemId == itemId && i.slotIndex == slotIndex);
            if (item == null) return;

            if (itemDef.equipSlot == EquipmentSlot.None)
            {
                Debug.LogWarning($"Item {itemId} cannot be equipped");
                return;
            }

            // Unequip current item in slot if any
            if (inventory.equipment[itemDef.equipSlot] != null)
            {
                UnequipItemServerRpc(playerId, itemDef.equipSlot);
            }

            // Move item to equipment
            container.items.Remove(item);
            inventory.equipment[itemDef.equipSlot] = item;

            // Apply stat bonuses from backpack
            if (itemDef.equipSlot == EquipmentSlot.Back)
            {
                inventory.maxWeight += itemDef.stats.weightCapacity;
                container.maxSlots += itemDef.stats.inventorySlots;
            }

            OnItemEquipped?.Invoke(playerId, itemId, itemDef.equipSlot);
            NotifyInventoryUpdatedClientRpc(playerId);

            Debug.Log($"Player {playerId} equipped {itemDef.itemName} to {itemDef.equipSlot}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void UnequipItemServerRpc(ulong playerId, EquipmentSlot slot, ServerRpcParams rpcParams = default)
        {
            if (!playerInventories.TryGetValue(playerId, out var inventory)) return;
            if (inventory.equipment[slot] == null) return;

            var item = inventory.equipment[slot];
            if (!itemDatabase.TryGetValue(item.itemId, out var itemDef)) return;

            // Check if there's space in backpack
            if (inventory.backpack.items.Count >= inventory.backpack.maxSlots)
            {
                Debug.LogWarning($"Backpack full, cannot unequip");
                return;
            }

            // Remove stat bonuses from backpack
            if (slot == EquipmentSlot.Back)
            {
                inventory.maxWeight -= itemDef.stats.weightCapacity;
                inventory.backpack.maxSlots -= itemDef.stats.inventorySlots;
            }

            // Move to backpack
            item.slotIndex = GetNextAvailableSlot(inventory.backpack);
            inventory.backpack.items.Add(item);
            inventory.equipment[slot] = null;

            OnItemUnequipped?.Invoke(playerId, slot);
            NotifyInventoryUpdatedClientRpc(playerId);

            Debug.Log($"Player {playerId} unequipped {itemDef.itemName} from {slot}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void UseConsumableServerRpc(ulong playerId, string itemId, ContainerType containerType, ServerRpcParams rpcParams = default)
        {
            if (!playerInventories.TryGetValue(playerId, out var inventory)) return;
            if (!itemDatabase.TryGetValue(itemId, out var itemDef)) return;
            if (itemDef.itemType != ItemType.Consumable) return;

            // Apply consumable effects based on type
            switch (itemDef.consumableType)
            {
                case ConsumableType.Health:
                    // Would integrate with health system
                    Debug.Log($"Healing player {playerId} for {itemDef.stats.healAmount} HP");
                    break;
                case ConsumableType.Stamina:
                    Debug.Log($"Restoring {itemDef.stats.staminaAmount} stamina to player {playerId}");
                    break;
                case ConsumableType.Buff:
                    Debug.Log($"Applying buff to player {playerId}");
                    break;
            }

            // Remove one from inventory
            RemoveItemServerRpc(playerId, itemId, 1, containerType);
        }

        [ServerRpc(RequireOwnership = false)]
        public void RepairItemServerRpc(ulong playerId, EquipmentSlot slot, ServerRpcParams rpcParams = default)
        {
            if (!playerInventories.TryGetValue(playerId, out var inventory)) return;
            if (inventory.equipment[slot] == null) return;

            var item = inventory.equipment[slot];
            if (!itemDatabase.TryGetValue(item.itemId, out var itemDef)) return;

            // Check for repair kit
            if (!HasItem(playerId, "misc_repair_kit", 1, ContainerType.Backpack)) return;

            // Restore durability
            item.durability = Mathf.Min(item.durability + 50f, itemDef.maxDurability);
            RemoveItemServerRpc(playerId, "misc_repair_kit", 1, ContainerType.Backpack);

            NotifyInventoryUpdatedClientRpc(playerId);
            Debug.Log($"Repaired {itemDef.itemName} for player {playerId}");
        }

        // Helper methods
        private InventoryContainer GetContainer(PlayerInventory inventory, ContainerType type)
        {
            return type switch
            {
                ContainerType.Backpack => inventory.backpack,
                ContainerType.Stash => inventory.stash,
                _ => null
            };
        }

        private int GetNextAvailableSlot(InventoryContainer container)
        {
            for (int i = 0; i < container.maxSlots; i++)
            {
                if (!container.items.Any(item => item.slotIndex == i))
                    return i;
            }
            return container.items.Count;
        }

        public bool HasItem(ulong playerId, string itemId, int quantity, ContainerType containerType)
        {
            if (!playerInventories.TryGetValue(playerId, out var inventory)) return false;
            
            var container = GetContainer(inventory, containerType);
            if (container == null) return false;

            int totalQuantity = container.items.Where(i => i.itemId == itemId).Sum(i => i.quantity);
            return totalQuantity >= quantity;
        }

        public int GetItemCount(ulong playerId, string itemId, ContainerType containerType)
        {
            if (!playerInventories.TryGetValue(playerId, out var inventory)) return 0;
            
            var container = GetContainer(inventory, containerType);
            if (container == null) return 0;

            return container.items.Where(i => i.itemId == itemId).Sum(i => i.quantity);
        }

        // Client RPCs
        [ClientRpc]
        private void NotifyInventoryUpdatedClientRpc(ulong playerId)
        {
            OnInventoryUpdated?.Invoke(playerId);
        }

        // Public getters
        public PlayerInventory GetPlayerInventory(ulong playerId) => playerInventories.GetValueOrDefault(playerId);
        public ItemDefinition GetItemDefinition(string itemId) => itemDatabase.GetValueOrDefault(itemId);
        public Dictionary<string, ItemDefinition> GetAllItems() => new Dictionary<string, ItemDefinition>(itemDatabase);
    }

    // Data structures
    [Serializable]
    public class PlayerInventory
    {
        public ulong playerId;
        public InventoryContainer backpack;
        public InventoryContainer stash;
        public Dictionary<EquipmentSlot, InventoryItem> equipment;
        public Dictionary<int, string> quickSlots;
        public float currentWeight;
        public float maxWeight;
        public int currency;
    }

    [Serializable]
    public class InventoryContainer
    {
        public ContainerType containerType;
        public int maxSlots;
        public List<InventoryItem> items;
    }

    [Serializable]
    public class InventoryItem
    {
        public string itemId;
        public int quantity;
        public float durability;
        public int slotIndex;
    }

    [Serializable]
    public class ItemDefinition
    {
        public string itemId;
        public string itemName;
        public string description;
        public ItemType itemType;
        public WeaponType weaponType;
        public ArmorType armorType;
        public ConsumableType consumableType;
        public AmmoType ammoType;
        public MaterialType materialType;
        public ItemRarity rarity;
        public int maxStack;
        public float weight;
        public int value;
        public float maxDurability;
        public EquipmentSlot equipSlot;
        public ItemStats stats;
        public int requiredLevel;
    }

    [Serializable]
    public class ItemStats
    {
        // Weapon stats
        public float damage;
        public float fireRate;
        public int magazineSize;
        public float reloadTime;
        public float range;
        public float accuracy;
        
        // Armor stats
        public float armor;
        public float damageReduction;
        
        // Bonus stats
        public float movementSpeed;
        public float reloadSpeed;
        public float staminaRegen;
        public int inventorySlots;
        public float weightCapacity;
        
        // Consumable stats
        public float healAmount;
        public float staminaAmount;
        public float useTime;
        public float duration;
        
        // Misc stats
        public float repairAmount;
    }

    public enum ItemType { Weapon, Armor, Consumable, Ammo, Material, QuestItem, Misc }
    public enum WeaponType { Pistol, AssaultRifle, Shotgun, Sniper, SMG, LMG, Melee, Explosive }
    public enum ArmorType { Helmet, Vest, Gloves, Boots, Backpack }
    public enum ConsumableType { Health, Stamina, Buff, Debuff }
    public enum AmmoType { Pistol9mm, Rifle556, Rifle762, Shotgun12Gauge }
    public enum MaterialType { Wood, Stone, Metal, Cloth, Electronics }
    public enum ItemRarity { Common, Uncommon, Rare, Epic, Legendary }
    public enum ContainerType { Backpack, Stash }
    public enum EquipmentSlot { None, Head, Chest, Hands, Feet, Back, Primary, Secondary, Melee }
}
