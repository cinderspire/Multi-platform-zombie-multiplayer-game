using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace DeadFrontier.Items
{
    /// <summary>
    /// Defines loot drop probabilities for containers, zombies, etc.
    /// </summary>
    [CreateAssetMenu(fileName = "New Loot Table", menuName = "DeadFrontier/Loot Table")]
    public class LootTable : ScriptableObject
    {
        [Header("Loot Settings")]
        [SerializeField] private string tableName = "Loot Table";
        [SerializeField, Range(0f, 1f)] private float dropChance = 1f; // Chance that ANY loot drops
        [SerializeField, Range(1, 10)] private int minItems = 1;
        [SerializeField, Range(1, 10)] private int maxItems = 3;

        [Header("Loot Entries")]
        [SerializeField] private List<LootEntry> lootEntries = new List<LootEntry>();

        /// <summary>
        /// Generates random loot from this table
        /// </summary>
        public List<LootDrop> GenerateLoot()
        {
            List<LootDrop> loot = new List<LootDrop>();

            // Check if any loot drops at all
            if (Random.value > dropChance)
            {
                return loot; // No loot
            }

            // Determine number of items to drop
            int itemCount = Random.Range(minItems, maxItems + 1);

            // Generate items
            for (int i = 0; i < itemCount; i++)
            {
                LootEntry entry = SelectRandomEntry();

                if (entry != null && entry.itemData != null)
                {
                    int quantity = Random.Range(entry.minQuantity, entry.maxQuantity + 1);

                    loot.Add(new LootDrop
                    {
                        itemData = entry.itemData,
                        quantity = quantity
                    });
                }
            }

            return loot;
        }

        /// <summary>
        /// Selects a random loot entry based on weight
        /// </summary>
        private LootEntry SelectRandomEntry()
        {
            if (lootEntries.Count == 0)
                return null;

            // Calculate total weight
            float totalWeight = lootEntries.Sum(entry => entry.weight);

            // Random value within total weight
            float randomValue = Random.Range(0f, totalWeight);

            // Find the entry
            float currentWeight = 0f;
            foreach (var entry in lootEntries)
            {
                currentWeight += entry.weight;

                if (randomValue <= currentWeight)
                {
                    // Check rarity chance
                    if (Random.value <= entry.rarityMultiplier)
                    {
                        return entry;
                    }
                }
            }

            // Fallback to first entry
            return lootEntries[0];
        }

        /// <summary>
        /// Gets all possible items in this loot table
        /// </summary>
        public List<ItemData> GetAllPossibleItems()
        {
            return lootEntries.Select(entry => entry.itemData).ToList();
        }

        [System.Serializable]
        public class LootEntry
        {
            public ItemData itemData;

            [Tooltip("Higher weight = more likely to drop")]
            [Range(0.1f, 100f)]
            public float weight = 10f;

            [Tooltip("Additional rarity check (1.0 = always, 0.5 = 50% chance)")]
            [Range(0f, 1f)]
            public float rarityMultiplier = 1f;

            [Tooltip("Minimum quantity to drop")]
            public int minQuantity = 1;

            [Tooltip("Maximum quantity to drop")]
            public int maxQuantity = 1;
        }

        public struct LootDrop
        {
            public ItemData itemData;
            public int quantity;
        }

        private void OnValidate()
        {
            // Ensure min/max items are valid
            if (minItems > maxItems)
                minItems = maxItems;
        }
    }
}
