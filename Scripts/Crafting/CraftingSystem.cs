using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Crafting
{
    /// <summary>
    /// Comprehensive crafting system with recipes, blueprints, quality tiers, crafting stations,
    /// and skill-based bonuses. Includes weapon crafting, armor crafting, consumables, and base building.
    /// </summary>
    public class CraftingSystem : NetworkBehaviour
    {
        public static CraftingSystem Instance { get; private set; }

        [Header("Crafting Configuration")]
        [SerializeField] private bool enableQualitySystem = true;
        [SerializeField] private bool enableCraftingSkills = true;
        [SerializeField] private float baseFailureChance = 0.05f;
        [SerializeField] private float criticalSuccessChance = 0.10f;

        // Crafting data
        private Dictionary<string, Recipe> recipeDatabase = new Dictionary<string, Recipe>();
        private Dictionary<string, CraftingStation> stationDatabase = new Dictionary<string, CraftingStation>();
        private Dictionary<ulong, PlayerCraftingData> playerCraftingData = new Dictionary<ulong, PlayerCraftingData>();

        // Events
        public event Action<ulong, string> OnItemCrafted;
        public event Action<ulong, string> OnRecipeUnlocked;
        public event Action<ulong, CraftingSkillType, int> OnSkillLevelUp;
        public event Action<ulong, string> OnBlueprintAcquired;

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
                InitializeRecipes();
                InitializeCraftingStations();
            }
        }

        private void InitializeRecipes()
        {
            // ===== WEAPONS CRAFTING =====

            recipeDatabase["craft_weapon_makeshift_pistol"] = new Recipe
            {
                recipeId = "craft_weapon_makeshift_pistol",
                recipeName = "Makeshift Pistol",
                description = "Craft a basic improvised pistol",
                resultItemId = "weapon_pistol_makeshift",
                resultQuantity = 1,
                recipeType = RecipeType.Weapon,
                requiredStation = CraftingStationType.WeaponBench,
                craftingTime = 30f,
                skillType = CraftingSkillType.Weaponsmithing,
                requiredSkillLevel = 1,
                materials = new List<MaterialRequirement>
                {
                    new MaterialRequirement { itemId = "material_metal", quantity = 20 },
                    new MaterialRequirement { itemId = "material_wood", quantity = 10 },
                    new MaterialRequirement { itemId = "material_electronics", quantity = 5 }
                },
                rarity = RecipeRarity.Common,
                requiredLevel = 1,
                unlockCost = 0
            };

            recipeDatabase["craft_weapon_rifle"] = new Recipe
            {
                recipeId = "craft_weapon_rifle",
                recipeName = "Basic Rifle",
                description = "Craft a reliable rifle",
                resultItemId = "weapon_rifle_basic",
                resultQuantity = 1,
                recipeType = RecipeType.Weapon,
                requiredStation = CraftingStationType.WeaponBench,
                craftingTime = 60f,
                skillType = CraftingSkillType.Weaponsmithing,
                requiredSkillLevel = 5,
                materials = new List<MaterialRequirement>
                {
                    new MaterialRequirement { itemId = "material_metal", quantity = 50 },
                    new MaterialRequirement { itemId = "material_wood", quantity = 20 },
                    new MaterialRequirement { itemId = "material_electronics", quantity = 10 },
                    new MaterialRequirement { itemId = "material_steel", quantity = 15 }
                },
                rarity = RecipeRarity.Uncommon,
                requiredLevel = 10,
                unlockCost = 1000
            };

            recipeDatabase["craft_weapon_sniper"] = new Recipe
            {
                recipeId = "craft_weapon_sniper",
                recipeName = "Precision Sniper Rifle",
                description = "Craft a high-quality sniper rifle",
                resultItemId = "weapon_sniper_precision",
                resultQuantity = 1,
                recipeType = RecipeType.Weapon,
                requiredStation = CraftingStationType.AdvancedWeaponBench,
                craftingTime = 120f,
                skillType = CraftingSkillType.Weaponsmithing,
                requiredSkillLevel = 10,
                materials = new List<MaterialRequirement>
                {
                    new MaterialRequirement { itemId = "material_steel", quantity = 40 },
                    new MaterialRequirement { itemId = "material_electronics", quantity = 25 },
                    new MaterialRequirement { itemId = "material_polymer", quantity = 20 },
                    new MaterialRequirement { itemId = "material_optics", quantity = 5 }
                },
                rarity = RecipeRarity.Rare,
                requiredLevel = 25,
                unlockCost = 5000
            };

            // ===== ARMOR CRAFTING =====

            recipeDatabase["craft_armor_leather_vest"] = new Recipe
            {
                recipeId = "craft_armor_leather_vest",
                recipeName = "Leather Vest",
                description = "Basic leather protection",
                resultItemId = "armor_vest_leather",
                resultQuantity = 1,
                recipeType = RecipeType.Armor,
                requiredStation = CraftingStationType.ArmorBench,
                craftingTime = 40f,
                skillType = CraftingSkillType.Armorsmithing,
                requiredSkillLevel = 1,
                materials = new List<MaterialRequirement>
                {
                    new MaterialRequirement { itemId = "material_cloth", quantity = 30 },
                    new MaterialRequirement { itemId = "material_leather", quantity = 20 }
                },
                rarity = RecipeRarity.Common,
                requiredLevel = 1,
                unlockCost = 0
            };

            recipeDatabase["craft_armor_tactical_vest"] = new Recipe
            {
                recipeId = "craft_armor_tactical_vest",
                recipeName = "Tactical Vest",
                description = "Military-grade body armor",
                resultItemId = "armor_vest_tactical",
                resultQuantity = 1,
                recipeType = RecipeType.Armor,
                requiredStation = CraftingStationType.ArmorBench,
                craftingTime = 80f,
                skillType = CraftingSkillType.Armorsmithing,
                requiredSkillLevel = 7,
                materials = new List<MaterialRequirement>
                {
                    new MaterialRequirement { itemId = "material_kevlar", quantity = 25 },
                    new MaterialRequirement { itemId = "material_metal", quantity = 30 },
                    new MaterialRequirement { itemId = "material_cloth", quantity = 20 }
                },
                rarity = RecipeRarity.Rare,
                requiredLevel = 20,
                unlockCost = 3000
            };

            recipeDatabase["craft_armor_helmet"] = new Recipe
            {
                recipeId = "craft_armor_helmet",
                recipeName = "Combat Helmet",
                description = "Reinforced head protection",
                resultItemId = "armor_helmet_combat",
                resultQuantity = 1,
                recipeType = RecipeType.Armor,
                requiredStation = CraftingStationType.ArmorBench,
                craftingTime = 50f,
                skillType = CraftingSkillType.Armorsmithing,
                requiredSkillLevel = 5,
                materials = new List<MaterialRequirement>
                {
                    new MaterialRequirement { itemId = "material_metal", quantity = 25 },
                    new MaterialRequirement { itemId = "material_kevlar", quantity = 15 },
                    new MaterialRequirement { itemId = "material_cloth", quantity = 10 }
                },
                rarity = RecipeRarity.Uncommon,
                requiredLevel = 15,
                unlockCost = 2000
            };

            // ===== CONSUMABLES =====

            recipeDatabase["craft_medkit"] = new Recipe
            {
                recipeId = "craft_medkit",
                recipeName = "Medical Kit",
                description = "Advanced healing item",
                resultItemId = "consumable_medkit",
                resultQuantity = 1,
                recipeType = RecipeType.Consumable,
                requiredStation = CraftingStationType.ChemistryStation,
                craftingTime = 20f,
                skillType = CraftingSkillType.Chemistry,
                requiredSkillLevel = 3,
                materials = new List<MaterialRequirement>
                {
                    new MaterialRequirement { itemId = "material_cloth", quantity = 10 },
                    new MaterialRequirement { itemId = "material_chemicals", quantity = 5 },
                    new MaterialRequirement { itemId = "material_herbs", quantity = 8 }
                },
                rarity = RecipeRarity.Common,
                requiredLevel = 5,
                unlockCost = 200
            };

            recipeDatabase["craft_stim_pack"] = new Recipe
            {
                recipeId = "craft_stim_pack",
                recipeName = "Combat Stimulant",
                description = "Instant health and stamina boost",
                resultItemId = "consumable_stim_pack",
                resultQuantity = 1,
                recipeType = RecipeType.Consumable,
                requiredStation = CraftingStationType.ChemistryStation,
                craftingTime = 30f,
                skillType = CraftingSkillType.Chemistry,
                requiredSkillLevel = 8,
                materials = new List<MaterialRequirement>
                {
                    new MaterialRequirement { itemId = "material_chemicals", quantity = 15 },
                    new MaterialRequirement { itemId = "material_rare_herbs", quantity = 5 },
                    new MaterialRequirement { itemId = "material_electronics", quantity = 3 }
                },
                rarity = RecipeRarity.Rare,
                requiredLevel = 22,
                unlockCost = 4000
            };

            // ===== AMMO CRAFTING =====

            recipeDatabase["craft_ammo_9mm"] = new Recipe
            {
                recipeId = "craft_ammo_9mm",
                recipeName = "9mm Ammunition",
                description = "Craft pistol rounds",
                resultItemId = "ammo_9mm",
                resultQuantity = 50,
                recipeType = RecipeType.Ammo,
                requiredStation = CraftingStationType.AmmoBench,
                craftingTime = 15f,
                skillType = CraftingSkillType.Weaponsmithing,
                requiredSkillLevel = 1,
                materials = new List<MaterialRequirement>
                {
                    new MaterialRequirement { itemId = "material_metal", quantity = 10 },
                    new MaterialRequirement { itemId = "material_gunpowder", quantity = 5 }
                },
                rarity = RecipeRarity.Common,
                requiredLevel = 1,
                unlockCost = 0
            };

            recipeDatabase["craft_ammo_556"] = new Recipe
            {
                recipeId = "craft_ammo_556",
                recipeName = "5.56mm Ammunition",
                description = "Craft rifle rounds",
                resultItemId = "ammo_556",
                resultQuantity = 50,
                recipeType = RecipeType.Ammo,
                requiredStation = CraftingStationType.AmmoBench,
                craftingTime = 20f,
                skillType = CraftingSkillType.Weaponsmithing,
                requiredSkillLevel = 3,
                materials = new List<MaterialRequirement>
                {
                    new MaterialRequirement { itemId = "material_metal", quantity = 15 },
                    new MaterialRequirement { itemId = "material_gunpowder", quantity = 8 }
                },
                rarity = RecipeRarity.Common,
                requiredLevel = 5,
                unlockCost = 100
            };

            recipeDatabase["craft_ammo_explosive"] = new Recipe
            {
                recipeId = "craft_ammo_explosive",
                recipeName = "Explosive Rounds",
                description = "High-damage explosive ammunition",
                resultItemId = "ammo_explosive",
                resultQuantity = 10,
                recipeType = RecipeType.Ammo,
                requiredStation = CraftingStationType.AdvancedAmmoBench,
                craftingTime = 45f,
                skillType = CraftingSkillType.Weaponsmithing,
                requiredSkillLevel = 10,
                materials = new List<MaterialRequirement>
                {
                    new MaterialRequirement { itemId = "material_metal", quantity = 20 },
                    new MaterialRequirement { itemId = "material_gunpowder", quantity = 25 },
                    new MaterialRequirement { itemId = "material_explosives", quantity = 10 }
                },
                rarity = RecipeRarity.Epic,
                requiredLevel = 30,
                unlockCost = 6000
            };

            // ===== BASE BUILDING =====

            recipeDatabase["craft_structure_wall_wood"] = new Recipe
            {
                recipeId = "craft_structure_wall_wood",
                recipeName = "Wooden Wall",
                description = "Basic defensive structure",
                resultItemId = "structure_wall_wood",
                resultQuantity = 1,
                recipeType = RecipeType.Structure,
                requiredStation = CraftingStationType.ConstructionTable,
                craftingTime = 10f,
                skillType = CraftingSkillType.Construction,
                requiredSkillLevel = 1,
                materials = new List<MaterialRequirement>
                {
                    new MaterialRequirement { itemId = "material_wood", quantity = 50 }
                },
                rarity = RecipeRarity.Common,
                requiredLevel = 1,
                unlockCost = 0
            };

            recipeDatabase["craft_structure_wall_reinforced"] = new Recipe
            {
                recipeId = "craft_structure_wall_reinforced",
                recipeName = "Reinforced Wall",
                description = "Sturdy metal-reinforced wall",
                resultItemId = "structure_wall_reinforced",
                resultQuantity = 1,
                recipeType = RecipeType.Structure,
                requiredStation = CraftingStationType.ConstructionTable,
                craftingTime = 30f,
                skillType = CraftingSkillType.Construction,
                requiredSkillLevel = 6,
                materials = new List<MaterialRequirement>
                {
                    new MaterialRequirement { itemId = "material_wood", quantity = 30 },
                    new MaterialRequirement { itemId = "material_metal", quantity = 40 },
                    new MaterialRequirement { itemId = "material_concrete", quantity = 20 }
                },
                rarity = RecipeRarity.Uncommon,
                requiredLevel = 15,
                unlockCost = 1500
            };

            recipeDatabase["craft_structure_turret"] = new Recipe
            {
                recipeId = "craft_structure_turret",
                recipeName = "Auto Turret",
                description = "Automated defense system",
                resultItemId = "structure_turret_auto",
                resultQuantity = 1,
                recipeType = RecipeType.Structure,
                requiredStation = CraftingStationType.AdvancedConstructionTable,
                craftingTime = 90f,
                skillType = CraftingSkillType.Construction,
                requiredSkillLevel = 10,
                materials = new List<MaterialRequirement>
                {
                    new MaterialRequirement { itemId = "material_metal", quantity = 60 },
                    new MaterialRequirement { itemId = "material_electronics", quantity = 40 },
                    new MaterialRequirement { itemId = "material_steel", quantity = 30 },
                    new MaterialRequirement { itemId = "material_circuits", quantity = 15 }
                },
                rarity = RecipeRarity.Epic,
                requiredLevel = 35,
                unlockCost = 8000
            };

            // ===== TOOLS =====

            recipeDatabase["craft_tool_lockpick"] = new Recipe
            {
                recipeId = "craft_tool_lockpick",
                recipeName = "Lockpick Set",
                description = "Tools for opening locked containers",
                resultItemId = "misc_lockpick",
                resultQuantity = 5,
                recipeType = RecipeType.Tool,
                requiredStation = CraftingStationType.WorkBench,
                craftingTime = 10f,
                skillType = CraftingSkillType.General,
                requiredSkillLevel = 1,
                materials = new List<MaterialRequirement>
                {
                    new MaterialRequirement { itemId = "material_metal", quantity = 5 }
                },
                rarity = RecipeRarity.Common,
                requiredLevel = 1,
                unlockCost = 0
            };

            recipeDatabase["craft_tool_repair_kit"] = new Recipe
            {
                recipeId = "craft_tool_repair_kit",
                recipeName = "Repair Kit",
                description = "Universal item repair kit",
                resultItemId = "misc_repair_kit",
                resultQuantity = 1,
                recipeType = RecipeType.Tool,
                requiredStation = CraftingStationType.WorkBench,
                craftingTime = 25f,
                skillType = CraftingSkillType.General,
                requiredSkillLevel = 3,
                materials = new List<MaterialRequirement>
                {
                    new MaterialRequirement { itemId = "material_metal", quantity = 15 },
                    new MaterialRequirement { itemId = "material_cloth", quantity = 10 },
                    new MaterialRequirement { itemId = "material_oil", quantity = 5 }
                },
                rarity = RecipeRarity.Uncommon,
                requiredLevel = 8,
                unlockCost = 500
            };

            // ===== MODIFICATIONS =====

            recipeDatabase["craft_mod_scope"] = new Recipe
            {
                recipeId = "craft_mod_scope",
                recipeName = "Weapon Scope",
                description = "Improves weapon accuracy and range",
                resultItemId = "mod_scope_basic",
                resultQuantity = 1,
                recipeType = RecipeType.Modification,
                requiredStation = CraftingStationType.WeaponBench,
                craftingTime = 35f,
                skillType = CraftingSkillType.Weaponsmithing,
                requiredSkillLevel = 4,
                materials = new List<MaterialRequirement>
                {
                    new MaterialRequirement { itemId = "material_optics", quantity = 5 },
                    new MaterialRequirement { itemId = "material_metal", quantity = 10 },
                    new MaterialRequirement { itemId = "material_electronics", quantity = 8 }
                },
                rarity = RecipeRarity.Uncommon,
                requiredLevel = 12,
                unlockCost = 1200
            };

            recipeDatabase["craft_mod_suppressor"] = new Recipe
            {
                recipeId = "craft_mod_suppressor",
                recipeName = "Suppressor",
                description = "Reduces weapon noise",
                resultItemId = "mod_suppressor",
                resultQuantity = 1,
                recipeType = RecipeType.Modification,
                requiredStation = CraftingStationType.WeaponBench,
                craftingTime = 40f,
                skillType = CraftingSkillType.Weaponsmithing,
                requiredSkillLevel = 6,
                materials = new List<MaterialRequirement>
                {
                    new MaterialRequirement { itemId = "material_metal", quantity = 20 },
                    new MaterialRequirement { itemId = "material_steel", quantity = 15 },
                    new MaterialRequirement { itemId = "material_rubber", quantity = 10 }
                },
                rarity = RecipeRarity.Rare,
                requiredLevel = 18,
                unlockCost = 2500
            };

            Debug.Log($"Initialized {recipeDatabase.Count} recipes");
        }

        private void InitializeCraftingStations()
        {
            stationDatabase["workbench"] = new CraftingStation
            {
                stationId = "workbench",
                stationName = "Workbench",
                stationType = CraftingStationType.WorkBench,
                description = "Basic crafting station for general items",
                requiredLevel = 1,
                buildCost = new List<MaterialRequirement>
                {
                    new MaterialRequirement { itemId = "material_wood", quantity = 50 },
                    new MaterialRequirement { itemId = "material_metal", quantity = 20 }
                }
            };

            stationDatabase["weapon_bench"] = new CraftingStation
            {
                stationId = "weapon_bench",
                stationName = "Weapon Bench",
                stationType = CraftingStationType.WeaponBench,
                description = "Craft and modify weapons",
                requiredLevel = 5,
                buildCost = new List<MaterialRequirement>
                {
                    new MaterialRequirement { itemId = "material_metal", quantity = 100 },
                    new MaterialRequirement { itemId = "material_wood", quantity = 50 },
                    new MaterialRequirement { itemId = "material_electronics", quantity = 25 }
                }
            };

            stationDatabase["advanced_weapon_bench"] = new CraftingStation
            {
                stationId = "advanced_weapon_bench",
                stationName = "Advanced Weapon Bench",
                stationType = CraftingStationType.AdvancedWeaponBench,
                description = "Craft high-tier weapons and modifications",
                requiredLevel = 25,
                buildCost = new List<MaterialRequirement>
                {
                    new MaterialRequirement { itemId = "material_steel", quantity = 150 },
                    new MaterialRequirement { itemId = "material_electronics", quantity = 100 },
                    new MaterialRequirement { itemId = "material_circuits", quantity = 50 }
                }
            };

            stationDatabase["armor_bench"] = new CraftingStation
            {
                stationId = "armor_bench",
                stationName = "Armor Bench",
                stationType = CraftingStationType.ArmorBench,
                description = "Craft protective gear",
                requiredLevel = 5,
                buildCost = new List<MaterialRequirement>
                {
                    new MaterialRequirement { itemId = "material_metal", quantity = 80 },
                    new MaterialRequirement { itemId = "material_cloth", quantity = 100 }
                }
            };

            stationDatabase["chemistry_station"] = new CraftingStation
            {
                stationId = "chemistry_station",
                stationName = "Chemistry Station",
                stationType = CraftingStationType.ChemistryStation,
                description = "Create consumables and medical items",
                requiredLevel = 8,
                buildCost = new List<MaterialRequirement>
                {
                    new MaterialRequirement { itemId = "material_metal", quantity = 60 },
                    new MaterialRequirement { itemId = "material_electronics", quantity = 40 },
                    new MaterialRequirement { itemId = "material_glass", quantity = 50 }
                }
            };

            stationDatabase["ammo_bench"] = new CraftingStation
            {
                stationId = "ammo_bench",
                stationName = "Ammunition Bench",
                stationType = CraftingStationType.AmmoBench,
                description = "Craft ammunition",
                requiredLevel = 3,
                buildCost = new List<MaterialRequirement>
                {
                    new MaterialRequirement { itemId = "material_metal", quantity = 70 },
                    new MaterialRequirement { itemId = "material_wood", quantity = 30 }
                }
            };

            stationDatabase["construction_table"] = new CraftingStation
            {
                stationId = "construction_table",
                stationName = "Construction Table",
                stationType = CraftingStationType.ConstructionTable,
                description = "Plan and craft base structures",
                requiredLevel = 1,
                buildCost = new List<MaterialRequirement>
                {
                    new MaterialRequirement { itemId = "material_wood", quantity = 80 }
                }
            };

            Debug.Log($"Initialized {stationDatabase.Count} crafting stations");
        }

        // Main crafting operations
        [ServerRpc(RequireOwnership = false)]
        public void StartCraftingServerRpc(ulong playerId, string recipeId, CraftingQuality targetQuality, ServerRpcParams rpcParams = default)
        {
            if (!recipeDatabase.TryGetValue(recipeId, out var recipe)) return;

            if (!playerCraftingData.ContainsKey(playerId))
            {
                InitializePlayerCraftingData(playerId);
            }

            var craftingData = playerCraftingData[playerId];

            // Check requirements
            if (!ValidateCraftingRequirements(playerId, recipe)) return;

            // Check if player has unlocked recipe
            if (!craftingData.unlockedRecipes.Contains(recipeId))
            {
                Debug.LogWarning($"Player {playerId} hasn't unlocked recipe {recipeId}");
                return;
            }

            // Remove materials from inventory
            foreach (var material in recipe.materials)
            {
                Inventory.InventorySystem.Instance?.RemoveItemServerRpc(playerId, material.itemId, material.quantity, Inventory.ContainerType.Backpack);
            }

            // Start crafting
            var craftingJob = new CraftingJob
            {
                jobId = Guid.NewGuid().ToString(),
                recipeId = recipeId,
                startTime = Time.time,
                craftingTime = CalculateCraftingTime(recipe, craftingData),
                targetQuality = targetQuality,
                completed = false
            };

            craftingData.activeCraftingJobs.Add(craftingJob);

            Debug.Log($"Player {playerId} started crafting {recipe.recipeName}");
        }

        private void Update()
        {
            if (!IsServer) return;

            // Check for completed crafting jobs
            foreach (var kvp in playerCraftingData)
            {
                var playerId = kvp.Key;
                var craftingData = kvp.Value;

                for (int i = craftingData.activeCraftingJobs.Count - 1; i >= 0; i--)
                {
                    var job = craftingData.activeCraftingJobs[i];
                    if (!job.completed && Time.time >= job.startTime + job.craftingTime)
                    {
                        CompleteCraftingJob(playerId, job);
                        craftingData.activeCraftingJobs.RemoveAt(i);
                    }
                }
            }
        }

        private void CompleteCraftingJob(ulong playerId, CraftingJob job)
        {
            if (!recipeDatabase.TryGetValue(job.recipeId, out var recipe)) return;
            if (!playerCraftingData.TryGetValue(playerId, out var craftingData)) return;

            // Determine crafting result
            CraftingResult result = DetermineCraftingResult(craftingData, recipe, job.targetQuality);

            if (result == CraftingResult.Success || result == CraftingResult.CriticalSuccess)
            {
                int quantity = recipe.resultQuantity;
                if (result == CraftingResult.CriticalSuccess)
                {
                    quantity = Mathf.RoundToInt(quantity * 1.5f); // 50% more on crit
                }

                // Add item to inventory
                Inventory.InventorySystem.Instance?.AddItemServerRpc(playerId, recipe.resultItemId, quantity, Inventory.ContainerType.Backpack);

                // Grant skill XP
                GainCraftingSkillXP(playerId, recipe.skillType, recipe.rarity);

                OnItemCrafted?.Invoke(playerId, recipe.resultItemId);
                Debug.Log($"Player {playerId} successfully crafted {recipe.recipeName} ({result})");
            }
            else
            {
                // Failure - return some materials
                foreach (var material in recipe.materials)
                {
                    int returnedAmount = Mathf.RoundToInt(material.quantity * 0.5f);
                    if (returnedAmount > 0)
                    {
                        Inventory.InventorySystem.Instance?.AddItemServerRpc(playerId, material.itemId, returnedAmount, Inventory.ContainerType.Backpack);
                    }
                }

                Debug.Log($"Player {playerId} failed to craft {recipe.recipeName}");
            }
        }

        private CraftingResult DetermineCraftingResult(PlayerCraftingData craftingData, Recipe recipe, CraftingQuality targetQuality)
        {
            int skillLevel = craftingData.craftingSkills.GetValueOrDefault(recipe.skillType, 1);
            
            // Base success chance
            float successChance = 0.95f - baseFailureChance;
            
            // Skill bonus
            float skillBonus = (skillLevel - recipe.requiredSkillLevel) * 0.02f;
            successChance = Mathf.Clamp(successChance + skillBonus, 0.5f, 0.99f);

            float roll = UnityEngine.Random.value;

            if (roll > successChance)
            {
                return CraftingResult.Failure;
            }
            else if (roll < criticalSuccessChance * (1f + skillLevel * 0.01f))
            {
                return CraftingResult.CriticalSuccess;
            }
            else
            {
                return CraftingResult.Success;
            }
        }

        private float CalculateCraftingTime(Recipe recipe, PlayerCraftingData craftingData)
        {
            int skillLevel = craftingData.craftingSkills.GetValueOrDefault(recipe.skillType, 1);
            float timeReduction = 1f - (skillLevel * 0.01f); // 1% faster per skill level
            return recipe.craftingTime * Mathf.Max(timeReduction, 0.5f); // Max 50% reduction
        }

        private bool ValidateCraftingRequirements(ulong playerId, Recipe recipe)
        {
            // Check materials
            foreach (var material in recipe.materials)
            {
                if (!Inventory.InventorySystem.Instance.HasItem(playerId, material.itemId, material.quantity, Inventory.ContainerType.Backpack))
                {
                    Debug.LogWarning($"Player {playerId} missing material {material.itemId}");
                    return false;
                }
            }

            // Check skill level
            if (enableCraftingSkills)
            {
                var craftingData = playerCraftingData[playerId];
                int skillLevel = craftingData.craftingSkills.GetValueOrDefault(recipe.skillType, 1);
                if (skillLevel < recipe.requiredSkillLevel)
                {
                    Debug.LogWarning($"Player {playerId} skill level too low");
                    return false;
                }
            }

            return true;
        }

        private void GainCraftingSkillXP(ulong playerId, CraftingSkillType skillType, RecipeRarity rarity)
        {
            if (!playerCraftingData.TryGetValue(playerId, out var craftingData)) return;

            int xpGain = rarity switch
            {
                RecipeRarity.Common => 10,
                RecipeRarity.Uncommon => 25,
                RecipeRarity.Rare => 50,
                RecipeRarity.Epic => 100,
                RecipeRarity.Legendary => 200,
                _ => 10
            };

            if (!craftingData.craftingSkillXP.ContainsKey(skillType))
            {
                craftingData.craftingSkillXP[skillType] = 0;
            }

            craftingData.craftingSkillXP[skillType] += xpGain;

            // Check for level up
            int currentLevel = craftingData.craftingSkills.GetValueOrDefault(skillType, 1);
            int requiredXP = currentLevel * 100;

            if (craftingData.craftingSkillXP[skillType] >= requiredXP)
            {
                craftingData.craftingSkillXP[skillType] -= requiredXP;
                craftingData.craftingSkills[skillType] = currentLevel + 1;

                OnSkillLevelUp?.Invoke(playerId, skillType, currentLevel + 1);
                Debug.Log($"Player {playerId} leveled up {skillType} to {currentLevel + 1}");
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void UnlockRecipeServerRpc(ulong playerId, string recipeId, ServerRpcParams rpcParams = default)
        {
            if (!recipeDatabase.ContainsKey(recipeId)) return;
            if (!playerCraftingData.ContainsKey(playerId))
            {
                InitializePlayerCraftingData(playerId);
            }

            var craftingData = playerCraftingData[playerId];
            if (!craftingData.unlockedRecipes.Contains(recipeId))
            {
                craftingData.unlockedRecipes.Add(recipeId);
                OnRecipeUnlocked?.Invoke(playerId, recipeId);
                Debug.Log($"Player {playerId} unlocked recipe {recipeId}");
            }
        }

        private void InitializePlayerCraftingData(ulong playerId)
        {
            var craftingData = new PlayerCraftingData
            {
                playerId = playerId,
                unlockedRecipes = new List<string>(),
                craftingSkills = new Dictionary<CraftingSkillType, int>(),
                craftingSkillXP = new Dictionary<CraftingSkillType, int>(),
                activeCraftingJobs = new List<CraftingJob>(),
                acquiredBlueprints = new List<string>()
            };

            // Initialize all skills at level 1
            foreach (CraftingSkillType skillType in Enum.GetValues(typeof(CraftingSkillType)))
            {
                craftingData.craftingSkills[skillType] = 1;
                craftingData.craftingSkillXP[skillType] = 0;
            }

            // Unlock basic recipes
            foreach (var recipe in recipeDatabase.Values.Where(r => r.requiredLevel == 1 && r.unlockCost == 0))
            {
                craftingData.unlockedRecipes.Add(recipe.recipeId);
            }

            playerCraftingData[playerId] = craftingData;
        }

        // Public getters
        public Recipe GetRecipe(string recipeId) => recipeDatabase.GetValueOrDefault(recipeId);
        public CraftingStation GetStation(string stationId) => stationDatabase.GetValueOrDefault(stationId);
        public PlayerCraftingData GetPlayerCraftingData(ulong playerId) => playerCraftingData.GetValueOrDefault(playerId);
        public List<Recipe> GetRecipesByType(RecipeType type) => recipeDatabase.Values.Where(r => r.recipeType == type).ToList();
    }

    // Data structures
    [Serializable]
    public class Recipe
    {
        public string recipeId;
        public string recipeName;
        public string description;
        public string resultItemId;
        public int resultQuantity;
        public RecipeType recipeType;
        public CraftingStationType requiredStation;
        public float craftingTime;
        public CraftingSkillType skillType;
        public int requiredSkillLevel;
        public List<MaterialRequirement> materials;
        public RecipeRarity rarity;
        public int requiredLevel;
        public int unlockCost;
    }

    [Serializable]
    public class MaterialRequirement
    {
        public string itemId;
        public int quantity;
    }

    [Serializable]
    public class CraftingStation
    {
        public string stationId;
        public string stationName;
        public CraftingStationType stationType;
        public string description;
        public int requiredLevel;
        public List<MaterialRequirement> buildCost;
    }

    [Serializable]
    public class PlayerCraftingData
    {
        public ulong playerId;
        public List<string> unlockedRecipes;
        public Dictionary<CraftingSkillType, int> craftingSkills;
        public Dictionary<CraftingSkillType, int> craftingSkillXP;
        public List<CraftingJob> activeCraftingJobs;
        public List<string> acquiredBlueprints;
    }

    [Serializable]
    public class CraftingJob
    {
        public string jobId;
        public string recipeId;
        public float startTime;
        public float craftingTime;
        public CraftingQuality targetQuality;
        public bool completed;
    }

    public enum RecipeType { Weapon, Armor, Consumable, Ammo, Structure, Tool, Modification }
    public enum CraftingStationType { WorkBench, WeaponBench, AdvancedWeaponBench, ArmorBench, ChemistryStation, AmmoBench, AdvancedAmmoBench, ConstructionTable, AdvancedConstructionTable }
    public enum CraftingSkillType { General, Weaponsmithing, Armorsmithing, Chemistry, Construction }
    public enum RecipeRarity { Common, Uncommon, Rare, Epic, Legendary }
    public enum CraftingQuality { Normal, Fine, Superior, Masterwork }
    public enum CraftingResult { Failure, Success, CriticalSuccess }
}
