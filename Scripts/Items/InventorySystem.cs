using UnityEngine;
using System;
using System.Collections.Generic;

namespace DeadFrontier.Items
{
    /// <summary>
    /// Manages player inventory including items, weapons, and equipment
    /// </summary>
    public class InventorySystem : MonoBehaviour
    {
        [Header("Inventory Settings")]
        [SerializeField] private int maxSlots = Core.Constants.INVENTORY_DEFAULT_SLOTS;

        // Inventory slots
        private List<InventorySlot> slots = new List<InventorySlot>();

        // Events
        public event Action OnInventoryChanged;
        public event Action<int, InventorySlot> OnSlotChanged;

        // Properties
        public int MaxSlots => maxSlots;
        public int UsedSlots => GetUsedSlotCount();
        public int FreeSlots => maxSlots - UsedSlots;
        public bool IsFull => UsedSlots >= maxSlots;

        private void Awake()
        {
            // Initialize slots
            for (int i = 0; i < maxSlots; i++)
            {
                slots.Add(new InventorySlot());
            }
        }

        /// <summary>
        /// Adds an item to the inventory
        /// </summary>
        public bool AddItem(ItemData item, int quantity = 1)
        {
            if (item == null || quantity <= 0)
                return false;

            // Try to stack with existing item first
            if (item.isStackable)
            {
                for (int i = 0; i < slots.Count; i++)
                {
                    if (!slots[i].IsEmpty && slots[i].item == item)
                    {
                        int spaceLeft = item.maxStackSize - slots[i].quantity;
                        if (spaceLeft > 0)
                        {
                            int amountToAdd = Mathf.Min(quantity, spaceLeft);
                            slots[i].quantity += amountToAdd;
                            quantity -= amountToAdd;

                            OnSlotChanged?.Invoke(i, slots[i]);

                            if (quantity <= 0)
                            {
                                OnInventoryChanged?.Invoke();
                                return true;
                            }
                        }
                    }
                }
            }

            // Add to new slot(s)
            while (quantity > 0)
            {
                int emptySlotIndex = FindEmptySlot();
                if (emptySlotIndex == -1)
                {
                    Debug.LogWarning("[InventorySystem] Inventory is full!");
                    OnInventoryChanged?.Invoke();
                    return false; // Inventory full
                }

                int amountToAdd = item.isStackable ? Mathf.Min(quantity, item.maxStackSize) : 1;
                slots[emptySlotIndex].item = item;
                slots[emptySlotIndex].quantity = amountToAdd;
                quantity -= amountToAdd;

                OnSlotChanged?.Invoke(emptySlotIndex, slots[emptySlotIndex]);
            }

            Debug.Log($"[InventorySystem] Added {item.itemName} to inventory");
            OnInventoryChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Removes an item from the inventory
        /// </summary>
        public bool RemoveItem(ItemData item, int quantity = 1)
        {
            if (item == null || quantity <= 0)
                return false;

            int remainingToRemove = quantity;

            for (int i = 0; i < slots.Count; i++)
            {
                if (!slots[i].IsEmpty && slots[i].item == item)
                {
                    if (slots[i].quantity >= remainingToRemove)
                    {
                        slots[i].quantity -= remainingToRemove;
                        if (slots[i].quantity <= 0)
                        {
                            slots[i].Clear();
                        }
                        OnSlotChanged?.Invoke(i, slots[i]);
                        OnInventoryChanged?.Invoke();
                        return true;
                    }
                    else
                    {
                        remainingToRemove -= slots[i].quantity;
                        slots[i].Clear();
                        OnSlotChanged?.Invoke(i, slots[i]);
                    }
                }
            }

            OnInventoryChanged?.Invoke();
            return remainingToRemove == 0;
        }

        /// <summary>
        /// Removes item from specific slot
        /// </summary>
        public void RemoveFromSlot(int slotIndex, int quantity = 1)
        {
            if (slotIndex < 0 || slotIndex >= slots.Count)
                return;

            if (slots[slotIndex].IsEmpty)
                return;

            slots[slotIndex].quantity -= quantity;

            if (slots[slotIndex].quantity <= 0)
            {
                slots[slotIndex].Clear();
            }

            OnSlotChanged?.Invoke(slotIndex, slots[slotIndex]);
            OnInventoryChanged?.Invoke();
        }

        /// <summary>
        /// Swaps items between two slots
        /// </summary>
        public void SwapSlots(int slotA, int slotB)
        {
            if (slotA < 0 || slotA >= slots.Count || slotB < 0 || slotB >= slots.Count)
                return;

            if (slotA == slotB)
                return;

            InventorySlot temp = slots[slotA].Clone();
            slots[slotA] = slots[slotB].Clone();
            slots[slotB] = temp;

            OnSlotChanged?.Invoke(slotA, slots[slotA]);
            OnSlotChanged?.Invoke(slotB, slots[slotB]);
            OnInventoryChanged?.Invoke();
        }

        /// <summary>
        /// Gets item at specific slot
        /// </summary>
        public InventorySlot GetSlot(int index)
        {
            if (index < 0 || index >= slots.Count)
                return null;

            return slots[index];
        }

        /// <summary>
        /// Gets all slots
        /// </summary>
        public List<InventorySlot> GetAllSlots()
        {
            return new List<InventorySlot>(slots);
        }

        /// <summary>
        /// Checks if inventory contains item
        /// </summary>
        public bool HasItem(ItemData item, int quantity = 1)
        {
            return GetItemCount(item) >= quantity;
        }

        /// <summary>
        /// Gets total count of specific item
        /// </summary>
        public int GetItemCount(ItemData item)
        {
            if (item == null)
                return 0;

            int count = 0;
            foreach (var slot in slots)
            {
                if (!slot.IsEmpty && slot.item == item)
                {
                    count += slot.quantity;
                }
            }
            return count;
        }

        /// <summary>
        /// Finds first empty slot index
        /// </summary>
        private int FindEmptySlot()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].IsEmpty)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Gets count of used slots
        /// </summary>
        private int GetUsedSlotCount()
        {
            int count = 0;
            foreach (var slot in slots)
            {
                if (!slot.IsEmpty)
                    count++;
            }
            return count;
        }

        /// <summary>
        /// Clears entire inventory
        /// </summary>
        public void Clear()
        {
            foreach (var slot in slots)
            {
                slot.Clear();
            }
            OnInventoryChanged?.Invoke();
        }

        /// <summary>
        /// Expands inventory size
        /// </summary>
        public void ExpandInventory(int additionalSlots)
        {
            for (int i = 0; i < additionalSlots; i++)
            {
                slots.Add(new InventorySlot());
            }
            maxSlots += additionalSlots;
            OnInventoryChanged?.Invoke();
        }
    }

    /// <summary>
    /// Represents a single inventory slot
    /// </summary>
    [System.Serializable]
    public class InventorySlot
    {
        public ItemData item;
        public int quantity;

        public bool IsEmpty => item == null || quantity <= 0;

        public void Clear()
        {
            item = null;
            quantity = 0;
        }

        public InventorySlot Clone()
        {
            return new InventorySlot
            {
                item = this.item,
                quantity = this.quantity
            };
        }
    }
}
