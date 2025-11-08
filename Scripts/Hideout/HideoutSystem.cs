using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace DeadFrontier.Hideout
{
    /// <summary>
    /// Comprehensive hideout and base building system for extraction shooters.
    /// Players can upgrade facilities, expand storage, craft items, and generate passive income.
    /// Provides progression and customization between raids.
    /// </summary>
    public class HideoutSystem : MonoBehaviour
    {
        public static HideoutSystem Instance { get; private set; }

        [Header("Hideout Settings")]
        [SerializeField] private HideoutModuleData[] availableModules;
        [SerializeField] private int maxModules = 15;
        [SerializeField] private bool realTimeConstruction = true; // Continue construction while offline

        [Header("Resource Generation")]
        [SerializeField] private bool enablePassiveGeneration = true;
        [SerializeField] private float generationTickInterval = 60f; // 1 minute
        [SerializeField] private int maxStoredGenerationTicks = 1440; // 24 hours worth

        [Header("Power System")]
        [SerializeField] private bool enablePowerSystem = true;
        [SerializeField] private int basePowerGeneration = 100;

        // Player hideouts
        private Dictionary<ulong, HideoutState> playerHideouts = new Dictionary<ulong, HideoutState>();

        // Events
        public event Action<ulong, HideoutModuleType, int> OnModuleUpgraded;
        public event Action<ulong, HideoutModuleType> OnModuleConstructionStarted;
        public event Action<ulong, HideoutModuleType> OnModuleConstructionCompleted;
        public event Action<ulong, string, int> OnResourceGenerated;
        public event Action<ulong, int> OnStashExpanded;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            LoadAllHideouts();
        }

        private void Update()
        {
            UpdateConstructions();
            UpdateResourceGeneration();
        }

        #region Initialization

        public void InitializePlayerHideout(ulong playerId)
        {
            if (playerHideouts.ContainsKey(playerId)) return;

            var hideout = new HideoutState
            {
                playerId = playerId,
                modules = new Dictionary<HideoutModuleType, HideoutModule>(),
                activeConstructions = new List<Construction>(),
                lastGenerationTime = DateTime.UtcNow,
                powerGeneration = basePowerGeneration,
                powerConsumption = 0
            };

            // Initialize base modules at level 0
            foreach (HideoutModuleType moduleType in Enum.GetValues(typeof(HideoutModuleType)))
            {
                hideout.modules[moduleType] = new HideoutModule
                {
                    moduleType = moduleType,
                    level = 0,
                    isUnlocked = (moduleType == HideoutModuleType.Stash), // Only stash starts unlocked
                    lastUpgradeTime = DateTime.UtcNow
                };
            }

            playerHideouts[playerId] = hideout;

            Debug.Log($"[HideoutSystem] Initialized hideout for player {playerId}");
        }

        #endregion

        #region Module Upgrades

        public bool CanUpgradeModule(ulong playerId, HideoutModuleType moduleType)
        {
            if (!playerHideouts.ContainsKey(playerId))
            {
                InitializePlayerHideout(playerId);
            }

            var hideout = playerHideouts[playerId];
            var module = hideout.modules[moduleType];

            // Check if module is unlocked
            if (!module.isUnlocked) return false;

            // Check if already at max level
            var moduleData = GetModuleData(moduleType);
            if (moduleData == null) return false;

            var nextLevel = module.level + 1;
            var upgradeData = moduleData.levels.FirstOrDefault(l => l.level == nextLevel);
            if (upgradeData == null) return false;

            // Check if already constructing
            if (IsModuleUnderConstruction(playerId, moduleType)) return false;

            // Check prerequisites
            if (upgradeData.prerequisites != null)
            {
                foreach (var prereq in upgradeData.prerequisites)
                {
                    if (!hideout.modules.ContainsKey(prereq.moduleType)) return false;

                    var prereqModule = hideout.modules[prereq.moduleType];
                    if (prereqModule.level < prereq.requiredLevel) return false;
                }
            }

            // Check resources
            if (upgradeData.resourceCosts != null)
            {
                foreach (var cost in upgradeData.resourceCosts)
                {
                    if (Economy.EconomyManager.Instance != null)
                    {
                        int playerAmount = Economy.EconomyManager.Instance.GetSoftCurrency();
                        if (cost.resourceType == ResourceType.SoftCurrency && playerAmount < cost.amount)
                            return false;
                    }

                    // Check item resources
                    if (cost.resourceType == ResourceType.Item)
                    {
                        if (Gameplay.InventoryManager.Instance != null)
                        {
                            var inventory = Gameplay.InventoryManager.Instance.GetInventoryItems(playerId);
                            var item = inventory.FirstOrDefault(i => i.itemData.itemId == cost.itemId);

                            if (item == null || item.quantity < cost.amount)
                                return false;
                        }
                    }
                }
            }

            return true;
        }

        public bool StartModuleUpgrade(ulong playerId, HideoutModuleType moduleType)
        {
            if (!CanUpgradeModule(playerId, moduleType)) return false;

            var hideout = playerHideouts[playerId];
            var module = hideout.modules[moduleType];
            var moduleData = GetModuleData(moduleType);

            var nextLevel = module.level + 1;
            var upgradeData = moduleData.levels.FirstOrDefault(l => l.level == nextLevel);

            if (upgradeData == null) return false;

            // Consume resources
            if (!ConsumeUpgradeResources(playerId, upgradeData)) return false;

            // Start construction
            var construction = new Construction
            {
                moduleType = moduleType,
                targetLevel = nextLevel,
                startTime = DateTime.UtcNow,
                completionTime = DateTime.UtcNow.AddSeconds(upgradeData.constructionTimeSeconds)
            };

            hideout.activeConstructions.Add(construction);

            OnModuleConstructionStarted?.Invoke(playerId, moduleType);

            SavePlayerHideout(playerId);

            Debug.Log($"[HideoutSystem] Started upgrade for {moduleType} to level {nextLevel}, completion in {upgradeData.constructionTimeSeconds}s");

            return true;
        }

        private bool ConsumeUpgradeResources(ulong playerId, ModuleLevelData upgradeData)
        {
            if (upgradeData.resourceCosts == null) return true;

            foreach (var cost in upgradeData.resourceCosts)
            {
                if (cost.resourceType == ResourceType.SoftCurrency)
                {
                    if (Economy.EconomyManager.Instance != null)
                    {
                        if (!Economy.EconomyManager.Instance.SpendSoftCurrency(cost.amount, "Hideout Upgrade"))
                            return false;
                    }
                }
                else if (cost.resourceType == ResourceType.HardCurrency)
                {
                    if (Economy.EconomyManager.Instance != null)
                    {
                        if (!Economy.EconomyManager.Instance.SpendHardCurrency(cost.amount, "Hideout Upgrade"))
                            return false;
                    }
                }
                else if (cost.resourceType == ResourceType.Item)
                {
                    if (Gameplay.InventoryManager.Instance != null)
                    {
                        if (!Gameplay.InventoryManager.Instance.RemoveItem(playerId, cost.itemId, cost.amount))
                            return false;
                    }
                }
            }

            return true;
        }

        private void UpdateConstructions()
        {
            foreach (var kvp in playerHideouts)
            {
                var playerId = kvp.Key;
                var hideout = kvp.Value;

                var constructionsToComplete = new List<Construction>();

                foreach (var construction in hideout.activeConstructions)
                {
                    // Check if construction is complete
                    if (DateTime.UtcNow >= construction.completionTime)
                    {
                        constructionsToComplete.Add(construction);
                    }
                }

                // Complete constructions
                foreach (var construction in constructionsToComplete)
                {
                    CompleteConstruction(playerId, construction);
                    hideout.activeConstructions.Remove(construction);
                }

                if (constructionsToComplete.Count > 0)
                {
                    SavePlayerHideout(playerId);
                }
            }
        }

        private void CompleteConstruction(ulong playerId, Construction construction)
        {
            if (!playerHideouts.ContainsKey(playerId)) return;

            var hideout = playerHideouts[playerId];
            var module = hideout.modules[construction.moduleType];

            module.level = construction.targetLevel;
            module.lastUpgradeTime = DateTime.UtcNow;

            // Apply module benefits
            ApplyModuleBenefits(playerId, construction.moduleType, module.level);

            OnModuleConstructionCompleted?.Invoke(playerId, construction.moduleType);
            OnModuleUpgraded?.Invoke(playerId, construction.moduleType, module.level);

            Debug.Log($"[HideoutSystem] Completed upgrade for {construction.moduleType} to level {module.level}");
        }

        public bool InstantCompleteConstruction(ulong playerId, HideoutModuleType moduleType, bool usePremiumCurrency = true)
        {
            if (!playerHideouts.ContainsKey(playerId)) return false;

            var hideout = playerHideouts[playerId];
            var construction = hideout.activeConstructions.FirstOrDefault(c => c.moduleType == moduleType);

            if (construction == null) return false;

            // Calculate time remaining
            TimeSpan remaining = construction.completionTime - DateTime.UtcNow;
            if (remaining.TotalSeconds <= 0)
            {
                CompleteConstruction(playerId, construction);
                hideout.activeConstructions.Remove(construction);
                return true;
            }

            // Calculate cost to instant complete
            int cost = CalculateInstantCompleteCost(remaining);

            if (usePremiumCurrency && Economy.EconomyManager.Instance != null)
            {
                if (!Economy.EconomyManager.Instance.SpendHardCurrency(cost, "Instant Complete Construction"))
                    return false;
            }

            // Complete immediately
            CompleteConstruction(playerId, construction);
            hideout.activeConstructions.Remove(construction);

            SavePlayerHideout(playerId);

            return true;
        }

        private int CalculateInstantCompleteCost(TimeSpan remaining)
        {
            // 1 premium currency per minute remaining
            return Mathf.CeilToInt((float)remaining.TotalMinutes);
        }

        #endregion

        #region Module Benefits

        private void ApplyModuleBenefits(ulong playerId, HideoutModuleType moduleType, int level)
        {
            var hideout = playerHideouts[playerId];
            var moduleData = GetModuleData(moduleType);
            var levelData = moduleData.levels.FirstOrDefault(l => l.level == level);

            if (levelData == null) return;

            switch (moduleType)
            {
                case HideoutModuleType.Stash:
                    // Expand stash size
                    if (Gameplay.InventoryManager.Instance != null)
                    {
                        int stashSize = levelData.stashSizeBonus;
                        // Apply stash expansion
                        OnStashExpanded?.Invoke(playerId, stashSize);
                    }
                    break;

                case HideoutModuleType.Generator:
                    // Increase power generation
                    hideout.powerGeneration = basePowerGeneration + levelData.powerGenerationBonus;
                    break;

                case HideoutModuleType.MedicalStation:
                    // Faster healing/regeneration
                    if (Medical.MedicalSystem.Instance != null)
                    {
                        // Apply healing speed bonus
                    }
                    break;

                case HideoutModuleType.Workbench:
                    // Faster crafting
                    if (Gameplay.CraftingSystem.Instance != null)
                    {
                        // Apply crafting speed bonus
                    }
                    break;

                case HideoutModuleType.BitcoinFarm:
                    // Generate passive income
                    // Handled in resource generation
                    break;

                case HideoutModuleType.ScavCase:
                    // Generate passive loot
                    // Handled in resource generation
                    break;

                case HideoutModuleType.ShootingRange:
                    // Unlock weapon testing
                    // Provides skill XP bonus
                    break;

                case HideoutModuleType.Library:
                    // Faster skill leveling
                    if (Progression.SkillTreeManager.Instance != null)
                    {
                        // Apply skill XP bonus
                    }
                    break;

                case HideoutModuleType.Intelligence:
                    // Unlock better quests/contracts
                    break;

                case HideoutModuleType.Security:
                    // Reduce insurance time
                    // Protect stash on death
                    break;
            }
        }

        #endregion

        #region Resource Generation

        private void UpdateResourceGeneration()
        {
            if (!enablePassiveGeneration) return;

            foreach (var kvp in playerHideouts)
            {
                var playerId = kvp.Key;
                var hideout = kvp.Value;

                // Calculate time since last generation
                TimeSpan timeSinceGeneration = DateTime.UtcNow - hideout.lastGenerationTime;
                int ticksToGenerate = Mathf.FloorToInt((float)timeSinceGeneration.TotalSeconds / generationTickInterval);

                // Cap at max stored ticks
                ticksToGenerate = Mathf.Min(ticksToGenerate, maxStoredGenerationTicks);

                if (ticksToGenerate > 0)
                {
                    GenerateResources(playerId, ticksToGenerate);
                    hideout.lastGenerationTime = hideout.lastGenerationTime.AddSeconds(ticksToGenerate * generationTickInterval);

                    SavePlayerHideout(playerId);
                }
            }
        }

        private void GenerateResources(ulong playerId, int ticks)
        {
            var hideout = playerHideouts[playerId];

            // Bitcoin Farm generation
            if (hideout.modules[HideoutModuleType.BitcoinFarm].isUnlocked)
            {
                int bitcoinFarmLevel = hideout.modules[HideoutModuleType.BitcoinFarm].level;
                if (bitcoinFarmLevel > 0)
                {
                    var moduleData = GetModuleData(HideoutModuleType.BitcoinFarm);
                    var levelData = moduleData.levels.FirstOrDefault(l => l.level == bitcoinFarmLevel);

                    if (levelData != null && levelData.resourceGenerationRate > 0)
                    {
                        int generatedAmount = Mathf.RoundToInt(levelData.resourceGenerationRate * ticks);

                        if (Economy.EconomyManager.Instance != null)
                        {
                            Economy.EconomyManager.Instance.EarnSoftCurrency(generatedAmount, "Bitcoin Farm");
                            OnResourceGenerated?.Invoke(playerId, "Bitcoin", generatedAmount);

                            Debug.Log($"[HideoutSystem] Bitcoin Farm generated {generatedAmount} currency for player {playerId}");
                        }
                    }
                }
            }

            // Scav Case generation
            if (hideout.modules[HideoutModuleType.ScavCase].isUnlocked)
            {
                int scavCaseLevel = hideout.modules[HideoutModuleType.ScavCase].level;
                if (scavCaseLevel > 0 && ticks > 0)
                {
                    // Generate random loot
                    GenerateScavCaseLoot(playerId, scavCaseLevel, ticks);
                }
            }

            // Generator fuel consumption
            if (hideout.modules[HideoutModuleType.Generator].isUnlocked)
            {
                int generatorLevel = hideout.modules[HideoutModuleType.Generator].level;
                if (generatorLevel > 0)
                {
                    // Consume fuel (would integrate with fuel item system)
                }
            }
        }

        private void GenerateScavCaseLoot(ulong playerId, int level, int ticks)
        {
            // Every X ticks, generate a random item
            int itemsToGenerate = ticks / 10; // 1 item every 10 minutes

            if (itemsToGenerate > 0 && Gameplay.InventoryManager.Instance != null)
            {
                for (int i = 0; i < itemsToGenerate; i++)
                {
                    // Generate random loot based on level
                    var lootData = GenerateRandomLoot(level);

                    if (lootData != null)
                    {
                        Gameplay.InventoryManager.Instance.AddItem(playerId, lootData, 1);
                        OnResourceGenerated?.Invoke(playerId, lootData.itemName, 1);
                    }
                }

                Debug.Log($"[HideoutSystem] Scav Case generated {itemsToGenerate} items for player {playerId}");
            }
        }

        private Gameplay.LootItemData GenerateRandomLoot(int scavCaseLevel)
        {
            // Generate loot based on scav case level
            // Higher levels = better loot
            int baseValue = UnityEngine.Random.Range(50, 500) * scavCaseLevel;

            return new Gameplay.LootItemData
            {
                itemId = $"scav_loot_{UnityEngine.Random.Range(0, 10000)}",
                itemName = "Scav Case Loot",
                category = Gameplay.ItemCategory.Misc,
                baseValue = baseValue,
                weight = UnityEngine.Random.Range(0.1f, 2f)
            };
        }

        #endregion

        #region Module Unlocking

        public bool UnlockModule(ulong playerId, HideoutModuleType moduleType)
        {
            if (!playerHideouts.ContainsKey(playerId))
            {
                InitializePlayerHideout(playerId);
            }

            var hideout = playerHideouts[playerId];
            var module = hideout.modules[moduleType];

            if (module.isUnlocked) return false;

            var moduleData = GetModuleData(moduleType);
            if (moduleData == null) return false;

            // Check unlock requirements
            if (moduleData.unlockCost > 0 && Economy.EconomyManager.Instance != null)
            {
                if (!Economy.EconomyManager.Instance.SpendSoftCurrency(moduleData.unlockCost, $"Unlock {moduleType}"))
                    return false;
            }

            module.isUnlocked = true;

            SavePlayerHideout(playerId);

            Debug.Log($"[HideoutSystem] Unlocked module {moduleType} for player {playerId}");

            return true;
        }

        #endregion

        #region Power System

        public int GetAvailablePower(ulong playerId)
        {
            if (!playerHideouts.ContainsKey(playerId)) return 0;

            var hideout = playerHideouts[playerId];
            return hideout.powerGeneration - hideout.powerConsumption;
        }

        public void UpdatePowerConsumption(ulong playerId)
        {
            if (!playerHideouts.ContainsKey(playerId)) return;

            var hideout = playerHideouts[playerId];
            int totalConsumption = 0;

            foreach (var kvp in hideout.modules)
            {
                if (!kvp.Value.isUnlocked || kvp.Value.level == 0) continue;

                var moduleData = GetModuleData(kvp.Key);
                var levelData = moduleData?.levels.FirstOrDefault(l => l.level == kvp.Value.level);

                if (levelData != null)
                {
                    totalConsumption += levelData.powerConsumption;
                }
            }

            hideout.powerConsumption = totalConsumption;
        }

        #endregion

        #region Persistence

        private void LoadAllHideouts()
        {
            if (Core.SaveSystem.Instance == null) return;

            // In production, would load hideouts for all players
            // For now, just log
            Debug.Log("[HideoutSystem] Hideout system ready for player data");
        }

        public void LoadPlayerHideout(ulong playerId)
        {
            if (Core.SaveSystem.Instance == null) return;

            string savedData = Core.SaveSystem.Instance.LoadData($"hideout_{playerId}");
            if (!string.IsNullOrEmpty(savedData))
            {
                try
                {
                    var hideoutSave = JsonUtility.FromJson<HideoutSaveData>(savedData);

                    var hideout = new HideoutState
                    {
                        playerId = playerId,
                        modules = new Dictionary<HideoutModuleType, HideoutModule>(),
                        activeConstructions = new List<Construction>(),
                        lastGenerationTime = DateTime.Parse(hideoutSave.lastGenerationTime),
                        powerGeneration = hideoutSave.powerGeneration,
                        powerConsumption = hideoutSave.powerConsumption
                    };

                    // Load modules
                    foreach (var moduleSave in hideoutSave.modules)
                    {
                        var module = new HideoutModule
                        {
                            moduleType = moduleSave.moduleType,
                            level = moduleSave.level,
                            isUnlocked = moduleSave.isUnlocked,
                            lastUpgradeTime = DateTime.Parse(moduleSave.lastUpgradeTime)
                        };

                        hideout.modules[moduleSave.moduleType] = module;
                    }

                    // Load active constructions
                    foreach (var constructionSave in hideoutSave.activeConstructions)
                    {
                        var construction = new Construction
                        {
                            moduleType = constructionSave.moduleType,
                            targetLevel = constructionSave.targetLevel,
                            startTime = DateTime.Parse(constructionSave.startTime),
                            completionTime = DateTime.Parse(constructionSave.completionTime)
                        };

                        hideout.activeConstructions.Add(construction);
                    }

                    playerHideouts[playerId] = hideout;

                    Debug.Log($"[HideoutSystem] Loaded hideout for player {playerId}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[HideoutSystem] Error loading hideout for player {playerId}: {e.Message}");
                    InitializePlayerHideout(playerId);
                }
            }
            else
            {
                InitializePlayerHideout(playerId);
            }
        }

        private void SavePlayerHideout(ulong playerId)
        {
            if (Core.SaveSystem.Instance == null) return;
            if (!playerHideouts.ContainsKey(playerId)) return;

            var hideout = playerHideouts[playerId];

            var hideoutSave = new HideoutSaveData
            {
                playerId = playerId,
                modules = new List<HideoutModuleSaveData>(),
                activeConstructions = new List<ConstructionSaveData>(),
                lastGenerationTime = hideout.lastGenerationTime.ToString(),
                powerGeneration = hideout.powerGeneration,
                powerConsumption = hideout.powerConsumption
            };

            // Save modules
            foreach (var kvp in hideout.modules)
            {
                var moduleSave = new HideoutModuleSaveData
                {
                    moduleType = kvp.Value.moduleType,
                    level = kvp.Value.level,
                    isUnlocked = kvp.Value.isUnlocked,
                    lastUpgradeTime = kvp.Value.lastUpgradeTime.ToString()
                };

                hideoutSave.modules.Add(moduleSave);
            }

            // Save constructions
            foreach (var construction in hideout.activeConstructions)
            {
                var constructionSave = new ConstructionSaveData
                {
                    moduleType = construction.moduleType,
                    targetLevel = construction.targetLevel,
                    startTime = construction.startTime.ToString(),
                    completionTime = construction.completionTime.ToString()
                };

                hideoutSave.activeConstructions.Add(constructionSave);
            }

            string json = JsonUtility.ToJson(hideoutSave);
            Core.SaveSystem.Instance.SaveData($"hideout_{playerId}", json);
        }

        #endregion

        #region Public Getters

        public HideoutState GetPlayerHideout(ulong playerId)
        {
            if (!playerHideouts.ContainsKey(playerId))
            {
                LoadPlayerHideout(playerId);
            }

            return playerHideouts.ContainsKey(playerId) ? playerHideouts[playerId] : null;
        }

        public int GetModuleLevel(ulong playerId, HideoutModuleType moduleType)
        {
            var hideout = GetPlayerHideout(playerId);
            if (hideout == null) return 0;

            return hideout.modules.ContainsKey(moduleType) ? hideout.modules[moduleType].level : 0;
        }

        public bool IsModuleUnlocked(ulong playerId, HideoutModuleType moduleType)
        {
            var hideout = GetPlayerHideout(playerId);
            if (hideout == null) return false;

            return hideout.modules.ContainsKey(moduleType) && hideout.modules[moduleType].isUnlocked;
        }

        public bool IsModuleUnderConstruction(ulong playerId, HideoutModuleType moduleType)
        {
            var hideout = GetPlayerHideout(playerId);
            if (hideout == null) return false;

            return hideout.activeConstructions.Any(c => c.moduleType == moduleType);
        }

        public TimeSpan GetConstructionTimeRemaining(ulong playerId, HideoutModuleType moduleType)
        {
            var hideout = GetPlayerHideout(playerId);
            if (hideout == null) return TimeSpan.Zero;

            var construction = hideout.activeConstructions.FirstOrDefault(c => c.moduleType == moduleType);
            if (construction == null) return TimeSpan.Zero;

            TimeSpan remaining = construction.completionTime - DateTime.UtcNow;
            return remaining.TotalSeconds > 0 ? remaining : TimeSpan.Zero;
        }

        public List<Construction> GetActiveConstructions(ulong playerId)
        {
            var hideout = GetPlayerHideout(playerId);
            if (hideout == null) return new List<Construction>();

            return new List<Construction>(hideout.activeConstructions);
        }

        public HideoutModuleData GetModuleData(HideoutModuleType moduleType)
        {
            return availableModules.FirstOrDefault(m => m.moduleType == moduleType);
        }

        public int GetStashSize(ulong playerId)
        {
            int stashLevel = GetModuleLevel(playerId, HideoutModuleType.Stash);
            var moduleData = GetModuleData(HideoutModuleType.Stash);

            if (moduleData == null) return 10; // Default size

            var levelData = moduleData.levels.FirstOrDefault(l => l.level == stashLevel);
            return levelData != null ? levelData.stashSizeBonus : 10;
        }

        #endregion
    }

    #region Data Classes

    public class HideoutState
    {
        public ulong playerId;
        public Dictionary<HideoutModuleType, HideoutModule> modules;
        public List<Construction> activeConstructions;
        public DateTime lastGenerationTime;
        public int powerGeneration;
        public int powerConsumption;
    }

    public class HideoutModule
    {
        public HideoutModuleType moduleType;
        public int level;
        public bool isUnlocked;
        public DateTime lastUpgradeTime;
    }

    public class Construction
    {
        public HideoutModuleType moduleType;
        public int targetLevel;
        public DateTime startTime;
        public DateTime completionTime;
    }

    [System.Serializable]
    public class HideoutModuleData
    {
        public HideoutModuleType moduleType;
        public string moduleName;
        [TextArea(2, 3)]
        public string description;

        public int unlockCost;
        public ModuleLevelData[] levels;

        public Sprite moduleIcon;
        public GameObject modulePrefab; // Visual representation
    }

    [System.Serializable]
    public class ModuleLevelData
    {
        public int level;
        [TextArea(2, 3)]
        public string levelDescription;

        public float constructionTimeSeconds = 60f;

        public ResourceCost[] resourceCosts;
        public ModulePrerequisite[] prerequisites;

        // Benefits
        public int stashSizeBonus;
        public int powerGenerationBonus;
        public int powerConsumption;
        public float craftingSpeedBonus;
        public float healingSpeedBonus;
        public float resourceGenerationRate; // Currency per tick
        public float skillXPBonus;
    }

    [System.Serializable]
    public class ResourceCost
    {
        public ResourceType resourceType;
        public string itemId; // For item resources
        public int amount;
    }

    [System.Serializable]
    public class ModulePrerequisite
    {
        public HideoutModuleType moduleType;
        public int requiredLevel;
    }

    [System.Serializable]
    public class HideoutSaveData
    {
        public ulong playerId;
        public List<HideoutModuleSaveData> modules;
        public List<ConstructionSaveData> activeConstructions;
        public string lastGenerationTime;
        public int powerGeneration;
        public int powerConsumption;
    }

    [System.Serializable]
    public class HideoutModuleSaveData
    {
        public HideoutModuleType moduleType;
        public int level;
        public bool isUnlocked;
        public string lastUpgradeTime;
    }

    [System.Serializable]
    public class ConstructionSaveData
    {
        public HideoutModuleType moduleType;
        public int targetLevel;
        public string startTime;
        public string completionTime;
    }

    public enum HideoutModuleType
    {
        Stash,              // Inventory expansion
        Generator,          // Power generation
        MedicalStation,     // Faster healing
        Workbench,          // Crafting
        BitcoinFarm,        // Passive currency generation
        ScavCase,           // Passive loot generation
        ShootingRange,      // Weapon testing, skill training
        Library,            // Faster skill leveling
        Intelligence,       // Better quests/contracts
        Security,           // Stash protection, faster insurance
        WaterCollector,     // Generate water resources
        FoodStorage,        // Store food, reduce consumption
        ArmorRepair,        // Repair armor/equipment
        WeaponMaintenance,  // Repair weapons
        RestArea            // Passive health regeneration
    }

    public enum ResourceType
    {
        SoftCurrency,
        HardCurrency,
        Item,
        Power
    }

    #endregion
}
