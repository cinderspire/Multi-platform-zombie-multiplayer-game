using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;
using System;

namespace DeadFrontier.Crafting
{
    /// <summary>
    /// Comprehensive crafting and upgrade system.
    /// Supports recipes, crafting stations, item modifications, repair, and salvage.
    /// Includes skill progression, quality tiers, and blueprint unlocking.
    /// </summary>
    public class CraftingSystem : NetworkBehaviour
    {
        public static CraftingSystem Instance { get; private set; }

        [Header("Crafting Settings")]
        [SerializeField] private float baseCraftingTime = 5f;
        [SerializeField] private int maxCraftingQueueSize = 10;
        [SerializeField] private bool enableCraftingSkill = true;

        [Header("Quality Settings")]
        [SerializeField] private float baseSuccessRate = 0.8f;
        [SerializeField] private float criticalSuccessChance = 0.1f;
        [SerializeField] private float failureChance = 0.05f;

        [Header("Repair Settings")]
        [SerializeField] private float repairCostMultiplier = 0.5f;
        [SerializeField] private float maxDurabilityLossPerRepair = 5f;

        // Recipe registry
        private Dictionary<string, CraftingRecipe> recipes = new Dictionary<string, CraftingRecipe>();

        // Crafting stations
        private Dictionary<string, CraftingStation> craftingStations = new Dictionary<string, CraftingStation>();

        // Player crafting data
        private Dictionary<ulong, PlayerCraftingData> playerData = new Dictionary<ulong, PlayerCraftingData>();

        // Active crafting queues
        private Dictionary<ulong, List<CraftingJob>> craftingQueues = new Dictionary<ulong, List<CraftingJob>>();

        // Events
        public event Action<ulong, string> OnItemCrafted;
        public event Action<ulong, string, ItemQuality> OnQualityCraft;
        public event Action<ulong, string> OnRecipeUnlocked;
        public event Action<ulong, int> OnCraftingLevelUp;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeRecipes();
                InitializeCraftingStations();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (!IsServer) return;

            ProcessCraftingQueues();
        }

        #region Initialization

        private void InitializeRecipes()
        {
            // Weapon Crafting
            RegisterRecipe(new CraftingRecipe
            {
                recipeId = "weapon_pistol_basic",
                recipeName = "Basic Pistol",
                category = CraftingCategory.Weapons,
                requiredStation = "workbench_weapons",
                craftingTime = 10f,
                requiredLevel = 1,
                ingredients = new List<CraftingIngredient>
                {
                    new CraftingIngredient { itemId = "material_metal", quantity = 5 },
                    new CraftingIngredient { itemId = "material_wood", quantity = 2 }
                },
                result = new CraftingResult { itemId = "weapon_pistol", quantity = 1, quality = ItemQuality.Common }
            });

            RegisterRecipe(new CraftingRecipe
            {
                recipeId = "weapon_rifle_advanced",
                recipeName = "Advanced Rifle",
                category = CraftingCategory.Weapons,
                requiredStation = "workbench_weapons",
                craftingTime = 30f,
                requiredLevel = 5,
                ingredients = new List<CraftingIngredient>
                {
                    new CraftingIngredient { itemId = "material_metal", quantity = 15 },
                    new CraftingIngredient { itemId = "material_polymer", quantity = 5 },
                    new CraftingIngredient { itemId = "component_trigger", quantity = 1 }
                },
                result = new CraftingResult { itemId = "weapon_rifle", quantity = 1, quality = ItemQuality.Rare }
            });

            // Armor Crafting
            RegisterRecipe(new CraftingRecipe
            {
                recipeId = "armor_vest_basic",
                recipeName = "Basic Vest",
                category = CraftingCategory.Armor,
                requiredStation = "workbench_armor",
                craftingTime = 15f,
                requiredLevel = 2,
                ingredients = new List<CraftingIngredient>
                {
                    new CraftingIngredient { itemId = "material_cloth", quantity = 10 },
                    new CraftingIngredient { itemId = "material_kevlar", quantity = 3 }
                },
                result = new CraftingResult { itemId = "armor_vest", quantity = 1, quality = ItemQuality.Common }
            });

            // Consumables
            RegisterRecipe(new CraftingRecipe
            {
                recipeId = "consumable_medkit",
                recipeName = "First Aid Kit",
                category = CraftingCategory.Consumables,
                requiredStation = "workbench_medical",
                craftingTime = 5f,
                requiredLevel = 1,
                ingredients = new List<CraftingIngredient>
                {
                    new CraftingIngredient { itemId = "material_bandage", quantity = 3 },
                    new CraftingIngredient { itemId = "material_antiseptic", quantity = 1 }
                },
                result = new CraftingResult { itemId = "consumable_medkit", quantity = 1, quality = ItemQuality.Common }
            });

            // Ammunition
            RegisterRecipe(new CraftingRecipe
            {
                recipeId = "ammo_9mm",
                recipeName = "9mm Ammunition",
                category = CraftingCategory.Ammunition,
                requiredStation = "workbench_ammo",
                craftingTime = 3f,
                requiredLevel = 1,
                ingredients = new List<CraftingIngredient>
                {
                    new CraftingIngredient { itemId = "material_metal", quantity = 1 },
                    new CraftingIngredient { itemId = "material_gunpowder", quantity = 1 }
                },
                result = new CraftingResult { itemId = "ammo_9mm", quantity = 10, quality = ItemQuality.Common }
            });

            // Attachments
            RegisterRecipe(new CraftingRecipe
            {
                recipeId = "attachment_scope",
                recipeName = "Weapon Scope",
                category = CraftingCategory.Attachments,
                requiredStation = "workbench_weapons",
                craftingTime = 12f,
                requiredLevel = 3,
                ingredients = new List<CraftingIngredient>
                {
                    new CraftingIngredient { itemId = "material_glass", quantity = 2 },
                    new CraftingIngredient { itemId = "material_metal", quantity = 3 },
                    new CraftingIngredient { itemId = "component_lens", quantity = 1 }
                },
                result = new CraftingResult { itemId = "attachment_scope", quantity = 1, quality = ItemQuality.Uncommon }
            });

            // Upgrades
            RegisterRecipe(new CraftingRecipe
            {
                recipeId = "upgrade_weapon_damage",
                recipeName = "Damage Upgrade Kit",
                category = CraftingCategory.Upgrades,
                requiredStation = "workbench_weapons",
                craftingTime = 20f,
                requiredLevel = 4,
                ingredients = new List<CraftingIngredient>
                {
                    new CraftingIngredient { itemId = "material_rare_metal", quantity = 5 },
                    new CraftingIngredient { itemId = "component_upgrade_chip", quantity = 1 }
                },
                result = new CraftingResult { itemId = "upgrade_damage", quantity = 1, quality = ItemQuality.Rare }
            });

            Debug.Log($"[CraftingSystem] Initialized {recipes.Count} recipes");
        }

        private void InitializeCraftingStations()
        {
            craftingStations["workbench_weapons"] = new CraftingStation
            {
                stationId = "workbench_weapons",
                stationName = "Weapons Workbench",
                allowedCategories = new List<CraftingCategory> { CraftingCategory.Weapons, CraftingCategory.Attachments },
                speedMultiplier = 1.0f,
                successBonus = 0.05f
            };

            craftingStations["workbench_armor"] = new CraftingStation
            {
                stationId = "workbench_armor",
                stationName = "Armor Workbench",
                allowedCategories = new List<CraftingCategory> { CraftingCategory.Armor },
                speedMultiplier = 1.0f,
                successBonus = 0.05f
            };

            craftingStations["workbench_medical"] = new CraftingStation
            {
                stationId = "workbench_medical",
                stationName = "Medical Station",
                allowedCategories = new List<CraftingCategory> { CraftingCategory.Consumables },
                speedMultiplier = 1.2f,
                successBonus = 0.1f
            };

            craftingStations["workbench_ammo"] = new CraftingStation
            {
                stationId = "workbench_ammo",
                stationName = "Ammunition Press",
                allowedCategories = new List<CraftingCategory> { CraftingCategory.Ammunition },
                speedMultiplier = 1.5f,
                successBonus = 0f
            };

            Debug.Log($"[CraftingSystem] Initialized {craftingStations.Count} crafting stations");
        }

        private void RegisterRecipe(CraftingRecipe recipe)
        {
            recipes[recipe.recipeId] = recipe;
        }

        #endregion

        #region Player Data

        public void InitializePlayerData(ulong playerId)
        {
            if (playerData.ContainsKey(playerId)) return;

            playerData[playerId] = new PlayerCraftingData
            {
                playerId = playerId,
                craftingLevel = 1,
                craftingXP = 0,
                unlockedRecipes = new List<string>(),
                craftingHistory = new List<string>()
            };

            craftingQueues[playerId] = new List<CraftingJob>();

            // Unlock basic recipes
            UnlockBasicRecipes(playerId);

            LoadPlayerData(playerId);
        }

        private void UnlockBasicRecipes(ulong playerId)
        {
            var basicRecipes = recipes.Values.Where(r => r.requiredLevel == 1);

            foreach (var recipe in basicRecipes)
            {
                UnlockRecipe(playerId, recipe.recipeId);
            }
        }

        #endregion

        #region Recipe Management

        public void UnlockRecipe(ulong playerId, string recipeId)
        {
            if (!recipes.ContainsKey(recipeId))
            {
                Debug.LogWarning($"[CraftingSystem] Recipe {recipeId} not found");
                return;
            }

            if (!playerData.ContainsKey(playerId))
            {
                InitializePlayerData(playerId);
            }

            var data = playerData[playerId];

            if (data.unlockedRecipes.Contains(recipeId))
            {
                Debug.LogWarning($"[CraftingSystem] Recipe {recipeId} already unlocked");
                return;
            }

            data.unlockedRecipes.Add(recipeId);
            SavePlayerData(playerId);

            OnRecipeUnlocked?.Invoke(playerId, recipeId);

            Debug.Log($"[CraftingSystem] Player {playerId} unlocked recipe: {recipeId}");
        }

        public bool IsRecipeUnlocked(ulong playerId, string recipeId)
        {
            if (!playerData.ContainsKey(playerId)) return false;
            return playerData[playerId].unlockedRecipes.Contains(recipeId);
        }

        public List<CraftingRecipe> GetUnlockedRecipes(ulong playerId)
        {
            if (!playerData.ContainsKey(playerId)) return new List<CraftingRecipe>();

            return playerData[playerId].unlockedRecipes
                .Select(id => recipes[id])
                .ToList();
        }

        public List<CraftingRecipe> GetRecipesByCategory(CraftingCategory category)
        {
            return recipes.Values.Where(r => r.category == category).ToList();
        }

        #endregion

        #region Crafting

        public bool CanCraft(ulong playerId, string recipeId)
        {
            if (!recipes.ContainsKey(recipeId)) return false;
            if (!IsRecipeUnlocked(playerId, recipeId)) return false;

            var recipe = recipes[recipeId];
            var data = playerData[playerId];

            // Check level requirement
            if (data.craftingLevel < recipe.requiredLevel) return false;

            // Check ingredients
            foreach (var ingredient in recipe.ingredients)
            {
                int playerAmount = Inventory.InventoryManager.Instance?.GetItemCount(playerId, ingredient.itemId) ?? 0;
                if (playerAmount < ingredient.quantity)
                {
                    return false;
                }
            }

            return true;
        }

        public bool StartCrafting(ulong playerId, string recipeId, int quantity = 1)
        {
            if (!CanCraft(playerId, recipeId))
            {
                Debug.LogWarning($"[CraftingSystem] Player {playerId} cannot craft {recipeId}");
                return false;
            }

            if (craftingQueues[playerId].Count >= maxCraftingQueueSize)
            {
                Debug.LogWarning($"[CraftingSystem] Crafting queue full for player {playerId}");
                return false;
            }

            var recipe = recipes[recipeId];

            // Consume ingredients
            foreach (var ingredient in recipe.ingredients)
            {
                Inventory.InventoryManager.Instance?.RemoveItem(playerId, ingredient.itemId, ingredient.quantity * quantity);
            }

            // Create crafting job
            var job = new CraftingJob
            {
                jobId = $"craft_{playerId}_{DateTime.UtcNow.Ticks}",
                recipeId = recipeId,
                playerId = playerId,
                quantity = quantity,
                startTime = DateTime.UtcNow,
                completionTime = DateTime.UtcNow.AddSeconds(recipe.craftingTime * quantity),
                stationId = recipe.requiredStation
            };

            craftingQueues[playerId].Add(job);

            Debug.Log($"[CraftingSystem] Player {playerId} started crafting {recipeId} x{quantity}");

            return true;
        }

        private void ProcessCraftingQueues()
        {
            foreach (var kvp in craftingQueues)
            {
                ulong playerId = kvp.Key;
                var queue = kvp.Value;

                for (int i = queue.Count - 1; i >= 0; i--)
                {
                    var job = queue[i];

                    if (DateTime.UtcNow >= job.completionTime)
                    {
                        CompleteCraftingJob(playerId, job);
                        queue.RemoveAt(i);
                    }
                }
            }
        }

        private void CompleteCraftingJob(ulong playerId, CraftingJob job)
        {
            var recipe = recipes[job.recipeId];

            // Calculate success
            bool success = CalculateCraftingSuccess(playerId, recipe);

            if (success)
            {
                // Determine quality
                ItemQuality quality = DetermineQuality(playerId, recipe);

                // Give item
                for (int i = 0; i < job.quantity; i++)
                {
                    Inventory.InventoryManager.Instance?.AddItem(playerId, recipe.result.itemId, recipe.result.quantity);
                }

                // Award XP
                int xp = Mathf.RoundToInt(recipe.craftingTime * 10 * job.quantity);
                AddCraftingXP(playerId, xp);

                // Update history
                playerData[playerId].craftingHistory.Add(recipe.recipeId);

                OnItemCrafted?.Invoke(playerId, recipe.result.itemId);

                if (quality > ItemQuality.Common)
                {
                    OnQualityCraft?.Invoke(playerId, recipe.result.itemId, quality);
                }

                Debug.Log($"[CraftingSystem] Player {playerId} crafted {recipe.recipeName} x{job.quantity} ({quality})");

                // Notify client
                CompleteCraftingClientRpc(playerId, recipe.recipeName, job.quantity, quality);
            }
            else
            {
                // Crafting failed - return partial ingredients
                foreach (var ingredient in recipe.ingredients)
                {
                    int returnAmount = Mathf.FloorToInt(ingredient.quantity * 0.5f * job.quantity);
                    if (returnAmount > 0)
                    {
                        Inventory.InventoryManager.Instance?.AddItem(playerId, ingredient.itemId, returnAmount);
                    }
                }

                Debug.LogWarning($"[CraftingSystem] Crafting failed for player {playerId}");
            }

            SavePlayerData(playerId);
        }

        [ClientRpc]
        private void CompleteCraftingClientRpc(ulong playerId, string itemName, int quantity, ItemQuality quality)
        {
            Debug.Log($"[CraftingSystem] Crafted: {itemName} x{quantity} ({quality})");
        }

        private bool CalculateCraftingSuccess(ulong playerId, CraftingRecipe recipe)
        {
            float successRate = baseSuccessRate;

            // Skill bonus
            if (enableCraftingSkill)
            {
                int level = playerData[playerId].craftingLevel;
                successRate += (level * 0.01f); // 1% per level
            }

            // Station bonus
            if (!string.IsNullOrEmpty(recipe.requiredStation) && craftingStations.ContainsKey(recipe.requiredStation))
            {
                successRate += craftingStations[recipe.requiredStation].successBonus;
            }

            // Cap at 99%
            successRate = Mathf.Min(successRate, 0.99f);

            return UnityEngine.Random.value <= successRate;
        }

        private ItemQuality DetermineQuality(ulong playerId, CraftingRecipe recipe)
        {
            float roll = UnityEngine.Random.value;

            // Critical success chance increases with level
            float critChance = criticalSuccessChance;
            if (enableCraftingSkill)
            {
                critChance += (playerData[playerId].craftingLevel * 0.005f);
            }

            if (roll <= critChance * 0.1f) return ItemQuality.Legendary;
            if (roll <= critChance * 0.3f) return ItemQuality.Epic;
            if (roll <= critChance) return ItemQuality.Rare;
            if (roll <= critChance * 2f) return ItemQuality.Uncommon;

            return recipe.result.quality;
        }

        #endregion

        #region Item Upgrades

        public bool UpgradeItem(ulong playerId, string itemId, string upgradeKitId)
        {
            // Check if player has the item
            if (Inventory.InventoryManager.Instance?.GetItemCount(playerId, itemId) < 1)
            {
                return false;
            }

            // Check if player has upgrade kit
            if (Inventory.InventoryManager.Instance?.GetItemCount(playerId, upgradeKitId) < 1)
            {
                return false;
            }

            // Consume upgrade kit
            Inventory.InventoryManager.Instance?.RemoveItem(playerId, upgradeKitId, 1);

            // Apply upgrade (this would integrate with item stats system)
            // For now, just log
            Debug.Log($"[CraftingSystem] Player {playerId} upgraded {itemId} with {upgradeKitId}");

            return true;
        }

        #endregion

        #region Repair

        public bool RepairItem(ulong playerId, string itemId)
        {
            // Calculate repair cost
            var repairCost = CalculateRepairCost(itemId);

            // Check if player can afford
            if (Economy.EconomyManager.Instance?.GetSoftCurrency(playerId) < repairCost)
            {
                return false;
            }

            // Charge player
            Economy.EconomyManager.Instance?.SpendSoftCurrency(playerId, repairCost);

            // Repair item (this would integrate with durability system)
            Debug.Log($"[CraftingSystem] Player {playerId} repaired {itemId} for {repairCost} credits");

            return true;
        }

        private int CalculateRepairCost(string itemId)
        {
            // Base cost calculation
            return Mathf.RoundToInt(100 * repairCostMultiplier);
        }

        #endregion

        #region Salvage

        public bool SalvageItem(ulong playerId, string itemId)
        {
            // Check if player has the item
            if (Inventory.InventoryManager.Instance?.GetItemCount(playerId, itemId) < 1)
            {
                return false;
            }

            // Remove item
            Inventory.InventoryManager.Instance?.RemoveItem(playerId, itemId, 1);

            // Give salvage materials
            var materials = GetSalvageMaterials(itemId);

            foreach (var material in materials)
            {
                Inventory.InventoryManager.Instance?.AddItem(playerId, material.Key, material.Value);
            }

            Debug.Log($"[CraftingSystem] Player {playerId} salvaged {itemId}");

            return true;
        }

        private Dictionary<string, int> GetSalvageMaterials(string itemId)
        {
            // Return materials based on item type
            var materials = new Dictionary<string, int>();
            materials["material_metal"] = UnityEngine.Random.Range(1, 5);
            materials["material_scrap"] = UnityEngine.Random.Range(2, 8);

            return materials;
        }

        #endregion

        #region Skill Progression

        private void AddCraftingXP(ulong playerId, int xp)
        {
            if (!enableCraftingSkill) return;

            var data = playerData[playerId];
            data.craftingXP += xp;

            int xpForNextLevel = GetXPForLevel(data.craftingLevel + 1);

            while (data.craftingXP >= xpForNextLevel)
            {
                data.craftingXP -= xpForNextLevel;
                data.craftingLevel++;

                OnCraftingLevelUp?.Invoke(playerId, data.craftingLevel);

                Debug.Log($"[CraftingSystem] Player {playerId} reached crafting level {data.craftingLevel}");

                // Unlock new recipes at this level
                UnlockRecipesAtLevel(playerId, data.craftingLevel);

                xpForNextLevel = GetXPForLevel(data.craftingLevel + 1);
            }

            SavePlayerData(playerId);
        }

        private int GetXPForLevel(int level)
        {
            return 100 * level * level;
        }

        private void UnlockRecipesAtLevel(ulong playerId, int level)
        {
            var recipesToUnlock = recipes.Values.Where(r => r.requiredLevel == level);

            foreach (var recipe in recipesToUnlock)
            {
                UnlockRecipe(playerId, recipe.recipeId);
            }
        }

        public int GetCraftingLevel(ulong playerId)
        {
            return playerData.ContainsKey(playerId) ? playerData[playerId].craftingLevel : 0;
        }

        #endregion

        #region Queue Management

        public List<CraftingJob> GetCraftingQueue(ulong playerId)
        {
            if (!craftingQueues.ContainsKey(playerId)) return new List<CraftingJob>();
            return new List<CraftingJob>(craftingQueues[playerId]);
        }

        public bool CancelCraftingJob(ulong playerId, string jobId)
        {
            if (!craftingQueues.ContainsKey(playerId)) return false;

            var job = craftingQueues[playerId].FirstOrDefault(j => j.jobId == jobId);

            if (job == null) return false;

            // Remove from queue
            craftingQueues[playerId].Remove(job);

            // Return ingredients
            var recipe = recipes[job.recipeId];
            foreach (var ingredient in recipe.ingredients)
            {
                Inventory.InventoryManager.Instance?.AddItem(playerId, ingredient.itemId, ingredient.quantity * job.quantity);
            }

            Debug.Log($"[CraftingSystem] Cancelled crafting job {jobId} for player {playerId}");

            return true;
        }

        #endregion

        #region Data Persistence

        private void SavePlayerData(ulong playerId)
        {
            if (!playerData.ContainsKey(playerId)) return;

            var data = playerData[playerId];
            string json = JsonUtility.ToJson(data);

            SaveSystem.SaveManager.Instance?.SaveData($"crafting_{playerId}", json);
        }

        public void LoadPlayerData(ulong playerId)
        {
            string json = SaveSystem.SaveManager.Instance?.LoadData($"crafting_{playerId}");

            if (!string.IsNullOrEmpty(json))
            {
                var data = JsonUtility.FromJson<PlayerCraftingData>(json);
                playerData[playerId] = data;

                Debug.Log($"[CraftingSystem] Loaded crafting data for player {playerId}");
            }
        }

        #endregion
    }

    #region Data Classes

    [Serializable]
    public class CraftingRecipe
    {
        public string recipeId;
        public string recipeName;
        public CraftingCategory category;
        public string requiredStation;
        public float craftingTime;
        public int requiredLevel;
        public List<CraftingIngredient> ingredients = new List<CraftingIngredient>();
        public CraftingResult result;
    }

    [Serializable]
    public class CraftingIngredient
    {
        public string itemId;
        public int quantity;
    }

    [Serializable]
    public class CraftingResult
    {
        public string itemId;
        public int quantity;
        public ItemQuality quality;
    }

    [Serializable]
    public class CraftingStation
    {
        public string stationId;
        public string stationName;
        public List<CraftingCategory> allowedCategories = new List<CraftingCategory>();
        public float speedMultiplier = 1f;
        public float successBonus = 0f;
    }

    [Serializable]
    public class PlayerCraftingData
    {
        public ulong playerId;
        public int craftingLevel;
        public int craftingXP;
        public List<string> unlockedRecipes = new List<string>();
        public List<string> craftingHistory = new List<string>();
    }

    [Serializable]
    public class CraftingJob
    {
        public string jobId;
        public string recipeId;
        public ulong playerId;
        public int quantity;
        public DateTime startTime;
        public DateTime completionTime;
        public string stationId;
    }

    public enum CraftingCategory
    {
        Weapons,
        Armor,
        Consumables,
        Ammunition,
        Attachments,
        Upgrades,
        Materials
    }

    public enum ItemQuality
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    #endregion
}
