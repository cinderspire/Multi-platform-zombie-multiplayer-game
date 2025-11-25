using UnityEngine;

namespace DeadFrontier.Crafting
{
    /// <summary>
    /// ScriptableObject defining crafting recipes for items, weapons, and equipment.
    /// </summary>
    [CreateAssetMenu(fileName = "New Recipe", menuName = "Dead Frontier/Crafting/Recipe Data")]
    public class CraftingRecipeDataSO : ScriptableObject
    {
        [Header("Identification")]
        public string recipeId;
        public string recipeName;
        [TextArea(3, 5)]
        public string description;
        public RecipeCategory category;

        [Header("Visuals")]
        public Sprite recipeIcon;

        [Header("Ingredients")]
        public CraftingIngredient[] requiredIngredients;
        public CraftingIngredient[] optionalIngredients;

        [Header("Output")]
        public CraftingOutput[] outputs;
        public float successChance = 1f;
        public CraftingOutput[] failureOutputs;

        [Header("Requirements")]
        public CraftingStationType requiredStation;
        public int requiredCraftingLevel = 1;
        public string[] requiredUnlocks;
        public int requiredReputation = 0;

        [Header("Process")]
        [Range(0f, 3600f)]
        public float craftingTime = 5f;
        public bool canQueue = true;
        public int maxQueueSize = 10;
        public bool requiresElectricity;
        public bool requiresFuel;
        public float fuelCost = 0f;

        [Header("Quality")]
        public bool hasQualityVariation;
        [Range(0f, 1f)]
        public float baseQualityChance = 0.1f;
        public QualityModifier[] qualityModifiers;

        [Header("Audio & Effects")]
        public AudioClip craftingStartSound;
        public AudioClip craftingLoopSound;
        public AudioClip craftingCompleteSound;
        public AudioClip craftingFailSound;
        public GameObject craftingEffectPrefab;

        [Header("Progression")]
        public int xpReward = 10;
        public bool isUnlockedByDefault;
        public int unlockCost;
    }

    [System.Serializable]
    public class CraftingIngredient
    {
        public string itemId;
        public int quantity = 1;
        public bool isConsumed = true;
        public Gameplay.ItemRarity minimumRarity;
        public string[] alternativeItemIds;
    }

    [System.Serializable]
    public class CraftingOutput
    {
        public string itemId;
        public int quantity = 1;
        public int minQuantity = 1;
        public int maxQuantity = 1;
        [Range(0f, 1f)]
        public float dropChance = 1f;
        public Gameplay.ItemRarity outputRarity;
    }

    [System.Serializable]
    public class QualityModifier
    {
        public QualityModifierType modifierType;
        public float bonusChance;
        public string description;
    }

    public enum RecipeCategory
    {
        Weapons,
        Ammunition,
        Medical,
        Consumables,
        Equipment,
        Building,
        Traps,
        Electronics,
        Furniture,
        Cosmetic,
        Special
    }

    public enum CraftingStationType
    {
        None,
        Workbench,
        Forge,
        ChemistryLab,
        Kitchen,
        Electronics,
        MedicalStation,
        GunsmithBench,
        TailorStation,
        AdvancedWorkshop
    }

    public enum QualityModifierType
    {
        HighCraftingLevel,
        PremiumIngredients,
        SpecialTool,
        RecipeMastery,
        LuckyCharm
    }
}
