using UnityEngine;

namespace DeadFrontier.Gameplay
{
    /// <summary>
    /// ScriptableObject defining loot item properties for the extraction shooter.
    /// </summary>
    [CreateAssetMenu(fileName = "New Loot Item", menuName = "Dead Frontier/Gameplay/Loot Item")]
    public class LootItemDataSO : ScriptableObject
    {
        [Header("Basic Info")]
        public string itemId;
        public string itemName;
        [TextArea(3, 5)]
        public string description;

        [Header("Classification")]
        public ItemRarity rarity = ItemRarity.Common;
        public ItemCategory category = ItemCategory.Material;

        [Header("Physical Properties")]
        [Range(0.1f, 50f)]
        public float weight = 1f;
        public int baseValue = 10;
        public Vector2Int inventorySize = new Vector2Int(1, 1); // Grid size

        [Header("Stack Properties")]
        public bool isStackable = true;
        public int maxStackSize = 99;

        [Header("Durability")]
        public bool hasDurability;
        [Range(1f, 1000f)]
        public float maxDurability = 100f;

        [Header("Consumable Properties")]
        public bool isConsumable;
        public bool canUseInCombat = true;
        public float useTime = 1f;
        public ItemEffect[] effects;

        [Header("Crafting")]
        public bool isCraftingMaterial;
        public CraftingRecipe[] usedInRecipes;

        [Header("Quest")]
        public bool isQuestItem;
        public string relatedQuestId;

        [Header("Visuals")]
        public Sprite icon;
        public GameObject worldModel;
        public GameObject inventoryModel;

        [Header("Audio")]
        public AudioClip pickupSound;
        public AudioClip useSound;
        public AudioClip dropSound;

        [Header("Sell/Trade")]
        public bool canBeSold = true;
        public bool canBeTraded = true;
        public bool canBeDropped = true;
        public bool canBeDestroyed = true;

        [Header("Special Properties")]
        public bool isKeycard; // For accessing areas
        public bool providesIntel; // For map objectives
        public bool isValuable; // High-value extraction target
        public float detectRange; // How far players can detect it

        // Convert to runtime LootItemData
        public LootItemData ToRuntimeData()
        {
            return new LootItemData
            {
                itemId = itemId,
                itemName = itemName,
                description = description,
                rarity = rarity,
                category = category,
                weight = weight,
                baseValue = baseValue,
                isStackable = isStackable,
                maxStackSize = maxStackSize,
                isConsumable = isConsumable,
                maxDurability = maxDurability,
                icon = icon,
                worldModel = worldModel,
                effects = effects
            };
        }
    }

    [System.Serializable]
    public class CraftingRecipe
    {
        public string recipeId;
        public string recipeName;
        public CraftingIngredient[] ingredients;
        public string resultItemId;
        public int resultQuantity = 1;
        public float craftingTime = 5f;
    }

    [System.Serializable]
    public class CraftingIngredient
    {
        public string itemId;
        public int quantity = 1;
    }
}
