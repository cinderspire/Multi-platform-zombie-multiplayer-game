using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace DeadFrontier.Gameplay
{
    /// <summary>
    /// Comprehensive crafting and upgrade system for weapons, items, and equipment.
    /// Provides progression depth through material gathering and item improvement.
    /// </summary>
    public class CraftingSystem : MonoBehaviour
    {
        public static CraftingSystem Instance { get; private set; }

        [Header("Crafting Settings")]
        [SerializeField] private bool enableCrafting = true;
        [SerializeField] private float baseCraftingTime = 5f;
        [SerializeField] private bool allowCraftingInRaid = false;
        [SerializeField] private bool consumeIngredientsOnStart = false;

        [Header("Upgrade Settings")]
        [SerializeField] private int maxUpgradeLevel = 10;
        [SerializeField] private float upgradeCostMultiplier = 1.5f;
        [SerializeField] private float upgradeStatIncreasePerLevel = 0.1f;

        [Header("Workbenches")]
        [SerializeField] private WorkbenchData[] workbenches;

        [Header("Audio")]
        [SerializeField] private AudioClip craftingStartSound;
        [SerializeField] private AudioClip craftingCompleteSound;
        [SerializeField] private AudioClip upgradeSound;

        // Recipes database
        private Dictionary<string, CraftingRecipe> recipes = new Dictionary<string, CraftingRecipe>();
        private Dictionary<string, UpgradeRecipe> upgradeRecipes = new Dictionary<string, UpgradeRecipe>();

        // Active crafting
        private Dictionary<string, CraftingProgress> activeCrafting = new Dictionary<string, CraftingProgress>();

        // Player inventories (reference)
        private InventoryManager inventoryManager;

        // Events
        public event Action<CraftingRecipe> OnCraftingStarted;
        public event Action<CraftingRecipe, float> OnCraftingProgress;
        public event Action<CraftingRecipe, string> OnCraftingCompleted; // Recipe, Result Item ID
        public event Action<string, int> OnItemUpgraded; // Item ID, New Level

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

        private void Start()
        {
            InitializeCrafting();
        }

        private void Update()
        {
            UpdateActiveCrafting();
        }

        #region Initialization

        private void InitializeCrafting()
        {
            inventoryManager = InventoryManager.Instance;

            // Load recipes from ScriptableObjects
            LoadCraftingRecipes();
            LoadUpgradeRecipes();

            Debug.Log($"[CraftingSystem] Initialized with {recipes.Count} crafting recipes and {upgradeRecipes.Count} upgrade recipes");
        }

        private void LoadCraftingRecipes()
        {
            // Load from Resources or DataPresetsManager
            // Placeholder: Create some default recipes
            var medkitRecipe = new CraftingRecipe
            {
                recipeId = "recipe_medkit",
                recipeName = "Craft Medkit",
                description = "Craft a healing item from medical supplies",
                ingredients = new CraftingIngredient[]
                {
                    new CraftingIngredient { itemId = "material_fabric", quantity = 2 },
                    new CraftingIngredient { itemId = "material_chemicals", quantity = 1 }
                },
                resultItemId = "consumable_medkit",
                resultQuantity = 1,
                craftingTime = 5f,
                requiredWorkbench = WorkbenchType.Medical,
                unlockLevel = 1
            };

            recipes[medkitRecipe.recipeId] = medkitRecipe;

            // Add more recipes...
            AddAmmoRecipe();
            AddAttachmentRecipe();
            AddExplosiveRecipe();
        }

        private void AddAmmoRecipe()
        {
            var ammoRecipe = new CraftingRecipe
            {
                recipeId = "recipe_ammo_rifle",
                recipeName = "Craft Rifle Ammo",
                ingredients = new CraftingIngredient[]
                {
                    new CraftingIngredient { itemId = "material_metal", quantity = 5 },
                    new CraftingIngredient { itemId = "material_gunpowder", quantity = 3 }
                },
                resultItemId = "ammo_rifle",
                resultQuantity = 30,
                craftingTime = 3f,
                requiredWorkbench = WorkbenchType.Weapons,
                unlockLevel = 1
            };

            recipes[ammoRecipe.recipeId] = ammoRecipe;
        }

        private void AddAttachmentRecipe()
        {
            var attachmentRecipe = new CraftingRecipe
            {
                recipeId = "recipe_reddot",
                recipeName = "Craft Red Dot Sight",
                ingredients = new CraftingIngredient[]
                {
                    new CraftingIngredient { itemId = "material_electronics", quantity = 2 },
                    new CraftingIngredient { itemId = "material_glass", quantity = 1 },
                    new CraftingIngredient { itemId = "material_metal", quantity = 3 }
                },
                resultItemId = "attachment_reddot",
                resultQuantity = 1,
                craftingTime = 10f,
                requiredWorkbench = WorkbenchType.Weapons,
                unlockLevel = 5
            };

            recipes[attachmentRecipe.recipeId] = attachmentRecipe;
        }

        private void AddExplosiveRecipe()
        {
            var explosiveRecipe = new CraftingRecipe
            {
                recipeId = "recipe_grenade",
                recipeName = "Craft Frag Grenade",
                ingredients = new CraftingIngredient[]
                {
                    new CraftingIngredient { itemId = "material_metal", quantity = 4 },
                    new CraftingIngredient { itemId = "material_explosives", quantity = 2 },
                    new CraftingIngredient { itemId = "material_electronics", quantity = 1 }
                },
                resultItemId = "weapon_grenade",
                resultQuantity = 1,
                craftingTime = 8f,
                requiredWorkbench = WorkbenchType.Explosives,
                unlockLevel = 10
            };

            recipes[explosiveRecipe.recipeId] = explosiveRecipe;
        }

        private void LoadUpgradeRecipes()
        {
            // Weapon upgrade recipe
            var weaponUpgrade = new UpgradeRecipe
            {
                upgradeId = "upgrade_weapon",
                upgradeName = "Upgrade Weapon",
                targetCategory = ItemCategory.Weapon,
                baseIngredients = new CraftingIngredient[]
                {
                    new CraftingIngredient { itemId = "material_metal", quantity = 10 },
                    new CraftingIngredient { itemId = "material_parts", quantity = 5 }
                },
                baseCost = 1000,
                upgradeTime = 15f,
                statImprovements = new StatImprovement[]
                {
                    new StatImprovement { statName = "Damage", increasePerLevel = 5f },
                    new StatImprovement { statName = "Durability", increasePerLevel = 10f }
                },
                maxLevel = maxUpgradeLevel
            };

            upgradeRecipes[weaponUpgrade.upgradeId] = weaponUpgrade;

            // Armor upgrade recipe
            var armorUpgrade = new UpgradeRecipe
            {
                upgradeId = "upgrade_armor",
                upgradeName = "Upgrade Armor",
                targetCategory = ItemCategory.Material, // Would be Armor in full game
                baseIngredients = new CraftingIngredient[]
                {
                    new CraftingIngredient { itemId = "material_fabric", quantity = 15 },
                    new CraftingIngredient { itemId = "material_kevlar", quantity = 5 }
                },
                baseCost = 1500,
                upgradeTime = 20f,
                statImprovements = new StatImprovement[]
                {
                    new StatImprovement { statName = "Protection", increasePerLevel = 5f }
                },
                maxLevel = maxUpgradeLevel
            };

            upgradeRecipes[armorUpgrade.upgradeId] = armorUpgrade;
        }

        #endregion

        #region Crafting

        public bool CanCraftRecipe(string recipeId, ulong playerId)
        {
            if (!recipes.ContainsKey(recipeId)) return false;

            var recipe = recipes[recipeId];

            // Check if player has materials
            if (!HasRequiredMaterials(recipe, playerId))
                return false;

            // Check level requirement
            if (Progression.AchievementManager.Instance != null)
            {
                int playerLevel = Progression.AchievementManager.Instance.GetPlayerLevel();
                if (playerLevel < recipe.unlockLevel)
                    return false;
            }

            // Check workbench requirement
            if (!IsNearWorkbench(playerId, recipe.requiredWorkbench))
                return false;

            return true;
        }

        public bool StartCrafting(string recipeId, ulong playerId)
        {
            if (!enableCrafting) return false;
            if (!CanCraftRecipe(recipeId, playerId)) return false;
            if (activeCrafting.ContainsKey(playerId.ToString())) return false;

            var recipe = recipes[recipeId];

            // Consume ingredients if configured
            if (consumeIngredientsOnStart)
            {
                if (!ConsumeIngredients(recipe, playerId))
                    return false;
            }

            // Start crafting
            var progress = new CraftingProgress
            {
                recipeId = recipeId,
                playerId = playerId,
                startTime = Time.time,
                totalTime = recipe.craftingTime,
                completed = false
            };

            activeCrafting[playerId.ToString()] = progress;

            OnCraftingStarted?.Invoke(recipe);

            if (craftingStartSound != null)
                Core.AudioManager.Instance?.PlaySFX(craftingStartSound);

            Debug.Log($"[CraftingSystem] Player {playerId} started crafting {recipe.recipeName}");

            return true;
        }

        public void CancelCrafting(ulong playerId)
        {
            string key = playerId.ToString();
            if (!activeCrafting.ContainsKey(key)) return;

            var progress = activeCrafting[key];
            var recipe = recipes[progress.recipeId];

            // Refund ingredients if not consumed on start
            if (!consumeIngredientsOnStart)
            {
                // Ingredients were being held, release them
            }

            activeCrafting.Remove(key);

            Debug.Log($"[CraftingSystem] Player {playerId} cancelled crafting {recipe.recipeName}");
        }

        private void UpdateActiveCrafting()
        {
            var completedCrafts = new List<string>();

            foreach (var kvp in activeCrafting)
            {
                var progress = kvp.Value;
                var recipe = recipes[progress.recipeId];

                float elapsed = Time.time - progress.startTime;
                float progressPercent = Mathf.Clamp01(elapsed / progress.totalTime);

                OnCraftingProgress?.Invoke(recipe, progressPercent);

                if (progressPercent >= 1f && !progress.completed)
                {
                    CompleteCrafting(progress);
                    completedCrafts.Add(kvp.Key);
                }
            }

            foreach (var key in completedCrafts)
            {
                activeCrafting.Remove(key);
            }
        }

        private void CompleteCrafting(CraftingProgress progress)
        {
            var recipe = recipes[progress.recipeId];

            // Consume ingredients if not consumed on start
            if (!consumeIngredientsOnStart)
            {
                if (!ConsumeIngredients(recipe, progress.playerId))
                {
                    Debug.LogWarning($"[CraftingSystem] Failed to consume ingredients for {recipe.recipeName}");
                    return;
                }
            }

            // Give player the crafted item
            if (inventoryManager != null)
            {
                // Would use actual item data
                var itemData = new LootItemData
                {
                    itemId = recipe.resultItemId,
                    itemName = recipe.recipeName,
                    isStackable = true
                };

                inventoryManager.AddItem(progress.playerId, itemData, recipe.resultQuantity);
            }

            OnCraftingCompleted?.Invoke(recipe, recipe.resultItemId);

            if (craftingCompleteSound != null)
                Core.AudioManager.Instance?.PlaySFX(craftingCompleteSound);

            Debug.Log($"[CraftingSystem] Player {progress.playerId} completed crafting {recipe.recipeName}");
        }

        private bool HasRequiredMaterials(CraftingRecipe recipe, ulong playerId)
        {
            if (inventoryManager == null) return false;

            var inventory = inventoryManager.GetInventoryItems(playerId);

            foreach (var ingredient in recipe.ingredients)
            {
                int owned = inventory.Where(i => i.itemData.itemId == ingredient.itemId)
                                    .Sum(i => i.quantity);

                if (owned < ingredient.quantity)
                    return false;
            }

            return true;
        }

        private bool ConsumeIngredients(CraftingRecipe recipe, ulong playerId)
        {
            if (inventoryManager == null) return false;

            foreach (var ingredient in recipe.ingredients)
            {
                if (!inventoryManager.RemoveItem(playerId, ingredient.itemId, ingredient.quantity))
                    return false;
            }

            return true;
        }

        #endregion

        #region Upgrading

        public bool CanUpgradeItem(string itemId, int currentLevel, ulong playerId)
        {
            // Find upgrade recipe for item category
            var upgradeRecipe = FindUpgradeRecipeForItem(itemId);
            if (upgradeRecipe == null) return false;

            if (currentLevel >= upgradeRecipe.maxLevel) return false;

            // Check if player has materials
            int requiredMultiplier = currentLevel + 1;
            if (!HasUpgradeMaterials(upgradeRecipe, requiredMultiplier, playerId))
                return false;

            // Check currency cost
            int upgradeCost = CalculateUpgradeCost(upgradeRecipe, currentLevel);
            if (Economy.EconomyManager.Instance != null)
            {
                if (!Economy.EconomyManager.Instance.CanAfford(upgradeCost))
                    return false;
            }

            return true;
        }

        public bool UpgradeItem(string itemId, int currentLevel, ulong playerId)
        {
            if (!CanUpgradeItem(itemId, currentLevel, playerId)) return false;

            var upgradeRecipe = FindUpgradeRecipeForItem(itemId);
            if (upgradeRecipe == null) return false;

            int newLevel = currentLevel + 1;
            int upgradeCost = CalculateUpgradeCost(upgradeRecipe, currentLevel);

            // Consume materials
            if (!ConsumeUpgradeMaterials(upgradeRecipe, newLevel, playerId))
                return false;

            // Consume currency
            if (Economy.EconomyManager.Instance != null)
            {
                if (!Economy.EconomyManager.Instance.SpendSoftCurrency(upgradeCost, $"Upgrade {itemId}"))
                    return false;
            }

            // Apply upgrade
            ApplyItemUpgrade(itemId, newLevel);

            OnItemUpgraded?.Invoke(itemId, newLevel);

            if (upgradeSound != null)
                Core.AudioManager.Instance?.PlaySFX(upgradeSound);

            Debug.Log($"[CraftingSystem] Upgraded {itemId} to level {newLevel}");

            return true;
        }

        private UpgradeRecipe FindUpgradeRecipeForItem(string itemId)
        {
            // In production, would lookup item category and find matching upgrade recipe
            // For now, return weapon upgrade for all items
            return upgradeRecipes.Values.FirstOrDefault();
        }

        private bool HasUpgradeMaterials(UpgradeRecipe recipe, int multiplier, ulong playerId)
        {
            if (inventoryManager == null) return false;

            var inventory = inventoryManager.GetInventoryItems(playerId);

            foreach (var ingredient in recipe.baseIngredients)
            {
                int required = ingredient.quantity * multiplier;
                int owned = inventory.Where(i => i.itemData.itemId == ingredient.itemId)
                                    .Sum(i => i.quantity);

                if (owned < required)
                    return false;
            }

            return true;
        }

        private bool ConsumeUpgradeMaterials(UpgradeRecipe recipe, int level, ulong playerId)
        {
            if (inventoryManager == null) return false;

            foreach (var ingredient in recipe.baseIngredients)
            {
                int required = ingredient.quantity * level;
                if (!inventoryManager.RemoveItem(playerId, ingredient.itemId, required))
                    return false;
            }

            return true;
        }

        private int CalculateUpgradeCost(UpgradeRecipe recipe, int currentLevel)
        {
            return Mathf.RoundToInt(recipe.baseCost * Mathf.Pow(upgradeCostMultiplier, currentLevel));
        }

        private void ApplyItemUpgrade(string itemId, int newLevel)
        {
            // In production, would update item stats in player's inventory
            // For now, just log
            Debug.Log($"[CraftingSystem] Applied upgrade to {itemId}: Level {newLevel}");
        }

        #endregion

        #region Workbench System

        private bool IsNearWorkbench(ulong playerId, WorkbenchType requiredType)
        {
            // Check if player is near required workbench
            // In production, would check distance to workbench objects
            // For now, return true (assume hideout crafting)
            return true;
        }

        #endregion

        #region Public Getters

        public List<CraftingRecipe> GetAvailableRecipes(ulong playerId)
        {
            return recipes.Values.Where(r => CanCraftRecipe(r.recipeId, playerId)).ToList();
        }

        public List<CraftingRecipe> GetAllRecipes()
        {
            return new List<CraftingRecipe>(recipes.Values);
        }

        public CraftingRecipe GetRecipe(string recipeId)
        {
            return recipes.ContainsKey(recipeId) ? recipes[recipeId] : null;
        }

        public bool IsCrafting(ulong playerId)
        {
            return activeCrafting.ContainsKey(playerId.ToString());
        }

        public float GetCraftingProgress(ulong playerId)
        {
            string key = playerId.ToString();
            if (!activeCrafting.ContainsKey(key)) return 0f;

            var progress = activeCrafting[key];
            float elapsed = Time.time - progress.startTime;
            return Mathf.Clamp01(elapsed / progress.totalTime);
        }

        #endregion
    }

    #region Supporting Classes

    [System.Serializable]
    public class CraftingRecipe
    {
        public string recipeId;
        public string recipeName;
        [TextArea(2, 3)]
        public string description;

        public CraftingIngredient[] ingredients;
        public string resultItemId;
        public int resultQuantity = 1;

        public float craftingTime = 5f;
        public WorkbenchType requiredWorkbench = WorkbenchType.None;
        public int unlockLevel = 1;

        public Sprite icon;
    }

    [System.Serializable]
    public class UpgradeRecipe
    {
        public string upgradeId;
        public string upgradeName;

        public ItemCategory targetCategory;
        public CraftingIngredient[] baseIngredients;
        public int baseCost;
        public float upgradeTime;

        public StatImprovement[] statImprovements;
        public int maxLevel = 10;
    }

    [System.Serializable]
    public class StatImprovement
    {
        public string statName;
        public float increasePerLevel;
    }

    public class CraftingProgress
    {
        public string recipeId;
        public ulong playerId;
        public float startTime;
        public float totalTime;
        public bool completed;
    }

    [System.Serializable]
    public class WorkbenchData
    {
        public WorkbenchType workbenchType;
        public string workbenchName;
        public int unlockLevel;
        public int buildCost;
        public CraftingIngredient[] buildMaterials;
    }

    public enum WorkbenchType
    {
        None,
        Weapons,
        Medical,
        Explosives,
        Electronics,
        Armor
    }

    #endregion
}
