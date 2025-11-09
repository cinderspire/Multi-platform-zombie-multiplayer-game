using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;
using System;

namespace DeadFrontier.Tutorial
{
    /// <summary>
    /// Comprehensive tutorial and onboarding system.
    /// Guides new players through game mechanics with interactive tutorials and hints.
    /// Includes contextual help, progressive unlocking, and reward system.
    /// </summary>
    public class TutorialSystem : NetworkBehaviour
    {
        public static TutorialSystem Instance { get; private set; }

        [Header("Tutorial Settings")]
        [SerializeField] private bool enableTutorials = true;
        [SerializeField] private bool allowSkipping = true;
        [SerializeField] private float hintDisplayTime = 5f;

        [Header("New Player Settings")]
        [SerializeField] private int maxPlayerLevelForTutorials = 10;
        [SerializeField] private bool replayableTutorials = true;

        // Tutorial definitions
        private Dictionary<string, TutorialDefinition> tutorialDefinitions = new Dictionary<string, TutorialDefinition>();

        // Player tutorial data
        private Dictionary<ulong, PlayerTutorialData> playerData = new Dictionary<ulong, PlayerTutorialData>();

        // Active tutorials
        private Dictionary<ulong, ActiveTutorial> activeTutorials = new Dictionary<ulong, ActiveTutorial>();

        // Hint system
        private Dictionary<string, HintDefinition> hintDefinitions = new Dictionary<string, HintDefinition>();

        // Events
        public event Action<ulong, string> OnTutorialStarted;
        public event Action<ulong, string> OnTutorialCompleted;
        public event Action<ulong, string, int> OnTutorialStepCompleted;
        public event Action<ulong, string> OnHintShown;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeTutorials();
                InitializeHints();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        #region Initialization

        private void InitializeTutorials()
        {
            // Basic Movement Tutorial
            RegisterTutorial(new TutorialDefinition
            {
                tutorialId = "tutorial_movement",
                tutorialName = "Basic Movement",
                description = "Learn how to move and navigate",
                category = TutorialCategory.Basic,
                isRequired = true,
                steps = new List<TutorialStep>
                {
                    new TutorialStep
                    {
                        stepId = "move_forward",
                        instruction = "Use WASD keys to move forward",
                        requirement = TutorialRequirement.Movement,
                        targetValue = 10f // Move 10 units
                    },
                    new TutorialStep
                    {
                        stepId = "sprint",
                        instruction = "Hold Shift to sprint",
                        requirement = TutorialRequirement.Sprint,
                        targetValue = 5f // Sprint for 5 seconds
                    },
                    new TutorialStep
                    {
                        stepId = "jump",
                        instruction = "Press Space to jump",
                        requirement = TutorialRequirement.Jump,
                        targetValue = 3 // Jump 3 times
                    }
                },
                rewards = new TutorialReward { xp = 100, softCurrency = 200 }
            });

            // Combat Tutorial
            RegisterTutorial(new TutorialDefinition
            {
                tutorialId = "tutorial_combat",
                tutorialName = "Combat Basics",
                description = "Learn how to fight zombies",
                category = TutorialCategory.Combat,
                isRequired = true,
                prerequisites = new List<string> { "tutorial_movement" },
                steps = new List<TutorialStep>
                {
                    new TutorialStep
                    {
                        stepId = "equip_weapon",
                        instruction = "Press 1 to equip your primary weapon",
                        requirement = TutorialRequirement.EquipWeapon,
                        targetValue = 1
                    },
                    new TutorialStep
                    {
                        stepId = "aim",
                        instruction = "Right-click to aim down sights",
                        requirement = TutorialRequirement.Aim,
                        targetValue = 2f // Aim for 2 seconds
                    },
                    new TutorialStep
                    {
                        stepId = "shoot",
                        instruction = "Left-click to shoot. Kill 3 zombies",
                        requirement = TutorialRequirement.KillEnemies,
                        targetValue = 3
                    },
                    new TutorialStep
                    {
                        stepId = "reload",
                        instruction = "Press R to reload your weapon",
                        requirement = TutorialRequirement.Reload,
                        targetValue = 1
                    }
                },
                rewards = new TutorialReward { xp = 200, softCurrency = 500, items = new List<string> { "weapon_pistol" } }
            });

            // Inventory Tutorial
            RegisterTutorial(new TutorialDefinition
            {
                tutorialId = "tutorial_inventory",
                tutorialName = "Inventory Management",
                description = "Learn how to manage your inventory",
                category = TutorialCategory.Systems,
                isRequired = true,
                steps = new List<TutorialStep>
                {
                    new TutorialStep
                    {
                        stepId = "open_inventory",
                        instruction = "Press I to open your inventory",
                        requirement = TutorialRequirement.OpenInventory,
                        targetValue = 1
                    },
                    new TutorialStep
                    {
                        stepId = "use_item",
                        instruction = "Click on a healing item to use it",
                        requirement = TutorialRequirement.UseItem,
                        targetValue = 1
                    },
                    new TutorialStep
                    {
                        stepId = "drop_item",
                        instruction = "Right-click an item and select 'Drop'",
                        requirement = TutorialRequirement.DropItem,
                        targetValue = 1
                    }
                },
                rewards = new TutorialReward { xp = 150, softCurrency = 300 }
            });

            // Extraction Tutorial
            RegisterTutorial(new TutorialDefinition
            {
                tutorialId = "tutorial_extraction",
                tutorialName = "Extraction Points",
                description = "Learn how to extract safely",
                category = TutorialCategory.Gameplay,
                isRequired = true,
                prerequisites = new List<string> { "tutorial_combat" },
                steps = new List<TutorialStep>
                {
                    new TutorialStep
                    {
                        stepId = "find_extraction",
                        instruction = "Find an extraction point marked on your map",
                        requirement = TutorialRequirement.ReachLocation,
                        targetValue = 1
                    },
                    new TutorialStep
                    {
                        stepId = "call_extraction",
                        instruction = "Interact with the extraction point",
                        requirement = TutorialRequirement.Interact,
                        targetValue = 1
                    },
                    new TutorialStep
                    {
                        stepId = "defend",
                        instruction = "Defend yourself while waiting for extraction",
                        requirement = TutorialRequirement.SurviveTime,
                        targetValue = 30f // 30 seconds
                    },
                    new TutorialStep
                    {
                        stepId = "extract",
                        instruction = "Board the extraction vehicle",
                        requirement = TutorialRequirement.Extract,
                        targetValue = 1
                    }
                },
                rewards = new TutorialReward { xp = 500, softCurrency = 1000, items = new List<string> { "cosmetic_survivor_badge" } }
            });

            // Crafting Tutorial
            RegisterTutorial(new TutorialDefinition
            {
                tutorialId = "tutorial_crafting",
                tutorialName = "Crafting System",
                description = "Learn how to craft items",
                category = TutorialCategory.Systems,
                isRequired = false,
                steps = new List<TutorialStep>
                {
                    new TutorialStep
                    {
                        stepId = "open_crafting",
                        instruction = "Open the crafting menu",
                        requirement = TutorialRequirement.OpenCrafting,
                        targetValue = 1
                    },
                    new TutorialStep
                    {
                        stepId = "craft_item",
                        instruction = "Craft a health pack",
                        requirement = TutorialRequirement.CraftItem,
                        targetValue = 1
                    }
                },
                rewards = new TutorialReward { xp = 200, softCurrency = 400 }
            });

            // Trading Tutorial
            RegisterTutorial(new TutorialDefinition
            {
                tutorialId = "tutorial_trading",
                tutorialName = "Player Trading",
                description = "Learn how to trade with other players",
                category = TutorialCategory.Social,
                isRequired = false,
                steps = new List<TutorialStep>
                {
                    new TutorialStep
                    {
                        stepId = "open_market",
                        instruction = "Open the marketplace",
                        requirement = TutorialRequirement.OpenMarket,
                        targetValue = 1
                    },
                    new TutorialStep
                    {
                        stepId = "buy_item",
                        instruction = "Purchase an item from the market",
                        requirement = TutorialRequirement.BuyItem,
                        targetValue = 1
                    }
                },
                rewards = new TutorialReward { xp = 150, softCurrency = 300 }
            });

            Debug.Log($"[TutorialSystem] Initialized {tutorialDefinitions.Count} tutorials");
        }

        private void InitializeHints()
        {
            RegisterHint(new HintDefinition
            {
                hintId = "hint_low_health",
                message = "Your health is low! Use a healing item or find medical supplies.",
                trigger = HintTrigger.LowHealth,
                priority = 5
            });

            RegisterHint(new HintDefinition
            {
                hintId = "hint_low_ammo",
                message = "Running low on ammo! Search for ammunition or switch weapons.",
                trigger = HintTrigger.LowAmmo,
                priority = 4
            });

            RegisterHint(new HintDefinition
            {
                hintId = "hint_enemy_nearby",
                message = "Enemies detected nearby! Stay alert.",
                trigger = HintTrigger.EnemyNearby,
                priority = 3
            });

            RegisterHint(new HintDefinition
            {
                hintId = "hint_extraction_available",
                message = "Extraction point available! Check your map.",
                trigger = HintTrigger.ExtractionAvailable,
                priority = 4
            });

            RegisterHint(new HintDefinition
            {
                hintId = "hint_inventory_full",
                message = "Your inventory is full! Drop or use items to make space.",
                trigger = HintTrigger.InventoryFull,
                priority = 3
            });

            Debug.Log($"[TutorialSystem] Initialized {hintDefinitions.Count} hints");
        }

        private void RegisterTutorial(TutorialDefinition tutorial)
        {
            tutorialDefinitions[tutorial.tutorialId] = tutorial;
        }

        private void RegisterHint(HintDefinition hint)
        {
            hintDefinitions[hint.hintId] = hint;
        }

        #endregion

        #region Player Data

        public void InitializePlayerData(ulong playerId)
        {
            if (playerData.ContainsKey(playerId)) return;

            playerData[playerId] = new PlayerTutorialData
            {
                playerId = playerId,
                completedTutorials = new List<string>(),
                shownHints = new List<string>(),
                tutorialProgress = new Dictionary<string, int>(),
                skipTutorials = false
            };

            LoadPlayerData(playerId);

            // Auto-start required tutorials for new players
            int playerLevel = Progression.ProgressionManager.Instance?.GetPlayerLevel(playerId) ?? 1;

            if (playerLevel <= maxPlayerLevelForTutorials)
            {
                StartNextRequiredTutorial(playerId);
            }
        }

        #endregion

        #region Tutorial Management

        public bool CanStartTutorial(ulong playerId, string tutorialId)
        {
            if (!enableTutorials) return false;

            if (!tutorialDefinitions.ContainsKey(tutorialId)) return false;

            if (!playerData.ContainsKey(playerId))
            {
                InitializePlayerData(playerId);
            }

            var data = playerData[playerId];
            var tutorial = tutorialDefinitions[tutorialId];

            // Check if already completed
            if (!replayableTutorials && data.completedTutorials.Contains(tutorialId)) return false;

            // Check if player opted out
            if (data.skipTutorials) return false;

            // Check prerequisites
            if (tutorial.prerequisites != null)
            {
                foreach (var prereq in tutorial.prerequisites)
                {
                    if (!data.completedTutorials.Contains(prereq)) return false;
                }
            }

            // Check if already active
            if (activeTutorials.ContainsKey(playerId)) return false;

            return true;
        }

        public bool StartTutorial(ulong playerId, string tutorialId)
        {
            if (!CanStartTutorial(playerId, tutorialId))
            {
                Debug.LogWarning($"[TutorialSystem] Cannot start tutorial {tutorialId} for player {playerId}");
                return false;
            }

            var tutorial = tutorialDefinitions[tutorialId];

            var activeTutorial = new ActiveTutorial
            {
                tutorialId = tutorialId,
                playerId = playerId,
                currentStep = 0,
                startTime = DateTime.UtcNow,
                stepProgress = new Dictionary<int, float>()
            };

            activeTutorials[playerId] = activeTutorial;

            OnTutorialStarted?.Invoke(playerId, tutorialId);

            Debug.Log($"[TutorialSystem] Player {playerId} started tutorial: {tutorial.tutorialName}");

            // Notify client
            StartTutorialClientRpc(playerId, tutorialId, tutorial.tutorialName, tutorial.steps[0].instruction);

            return true;
        }

        [ClientRpc]
        private void StartTutorialClientRpc(ulong playerId, string tutorialId, string tutorialName, string firstInstruction)
        {
            Debug.Log($"[TutorialSystem] TUTORIAL: {tutorialName}\n{firstInstruction}");
        }

        private void StartNextRequiredTutorial(ulong playerId)
        {
            var data = playerData[playerId];

            // Find next required tutorial
            var nextTutorial = tutorialDefinitions.Values
                .Where(t => t.isRequired && !data.completedTutorials.Contains(t.tutorialId))
                .OrderBy(t => t.tutorialId)
                .FirstOrDefault();

            if (nextTutorial != null && CanStartTutorial(playerId, nextTutorial.tutorialId))
            {
                StartTutorial(playerId, nextTutorial.tutorialId);
            }
        }

        public void SkipTutorial(ulong playerId)
        {
            if (!allowSkipping) return;

            if (!activeTutorials.ContainsKey(playerId)) return;

            var activeTutorial = activeTutorials[playerId];
            var tutorial = tutorialDefinitions[activeTutorial.tutorialId];

            // Mark as completed without rewards
            CompleteTutorial(playerId, false);

            Debug.Log($"[TutorialSystem] Player {playerId} skipped tutorial {activeTutorial.tutorialId}");
        }

        public void SkipAllTutorials(ulong playerId)
        {
            if (!playerData.ContainsKey(playerId)) return;

            playerData[playerId].skipTutorials = true;

            SavePlayerData(playerId);

            Debug.Log($"[TutorialSystem] Player {playerId} opted out of tutorials");
        }

        #endregion

        #region Tutorial Progress

        public void UpdateTutorialProgress(ulong playerId, TutorialRequirement requirement, float value)
        {
            if (!activeTutorials.ContainsKey(playerId)) return;

            var activeTutorial = activeTutorials[playerId];
            var tutorial = tutorialDefinitions[activeTutorial.tutorialId];

            var currentStep = tutorial.steps[activeTutorial.currentStep];

            if (currentStep.requirement != requirement) return;

            // Update progress
            if (!activeTutorial.stepProgress.ContainsKey(activeTutorial.currentStep))
            {
                activeTutorial.stepProgress[activeTutorial.currentStep] = 0f;
            }

            float newProgress = Mathf.Min(
                activeTutorial.stepProgress[activeTutorial.currentStep] + value,
                currentStep.targetValue
            );

            activeTutorial.stepProgress[activeTutorial.currentStep] = newProgress;

            // Check step completion
            if (newProgress >= currentStep.targetValue)
            {
                CompleteStep(playerId);
            }
        }

        private void CompleteStep(ulong playerId)
        {
            var activeTutorial = activeTutorials[playerId];
            var tutorial = tutorialDefinitions[activeTutorial.tutorialId];

            int completedStep = activeTutorial.currentStep;

            OnTutorialStepCompleted?.Invoke(playerId, activeTutorial.tutorialId, completedStep);

            Debug.Log($"[TutorialSystem] Player {playerId} completed step {completedStep}");

            // Move to next step
            activeTutorial.currentStep++;

            if (activeTutorial.currentStep >= tutorial.steps.Count)
            {
                // Tutorial complete
                CompleteTutorial(playerId, true);
            }
            else
            {
                // Show next step
                var nextStep = tutorial.steps[activeTutorial.currentStep];
                ShowTutorialStepClientRpc(playerId, nextStep.instruction);
            }
        }

        [ClientRpc]
        private void ShowTutorialStepClientRpc(ulong playerId, string instruction)
        {
            Debug.Log($"[TutorialSystem] NEXT STEP: {instruction}");
        }

        private void CompleteTutorial(ulong playerId, bool awardRewards)
        {
            if (!activeTutorials.ContainsKey(playerId)) return;

            var activeTutorial = activeTutorials[playerId];
            var tutorial = tutorialDefinitions[activeTutorial.tutorialId];
            var data = playerData[playerId];

            // Mark as completed
            if (!data.completedTutorials.Contains(activeTutorial.tutorialId))
            {
                data.completedTutorials.Add(activeTutorial.tutorialId);
            }

            // Award rewards
            if (awardRewards)
            {
                AwardTutorialRewards(playerId, tutorial.rewards);
            }

            activeTutorials.Remove(playerId);

            OnTutorialCompleted?.Invoke(playerId, activeTutorial.tutorialId);

            SavePlayerData(playerId);

            Debug.Log($"[TutorialSystem] Player {playerId} completed tutorial: {tutorial.tutorialName}");

            // Notify client
            CompleteTutorialClientRpc(playerId, tutorial.tutorialName);

            // Start next required tutorial
            StartNextRequiredTutorial(playerId);
        }

        [ClientRpc]
        private void CompleteTutorialClientRpc(ulong playerId, string tutorialName)
        {
            Debug.Log($"[TutorialSystem] TUTORIAL COMPLETED: {tutorialName}");
        }

        #endregion

        #region Rewards

        private void AwardTutorialRewards(ulong playerId, TutorialReward rewards)
        {
            if (rewards.xp > 0)
            {
                Progression.ProgressionManager.Instance?.AddExperience(playerId, rewards.xp);
            }

            if (rewards.softCurrency > 0)
            {
                Economy.EconomyManager.Instance?.AddSoftCurrency(playerId, rewards.softCurrency);
            }

            if (rewards.hardCurrency > 0)
            {
                Economy.EconomyManager.Instance?.AddHardCurrency(playerId, rewards.hardCurrency);
            }

            foreach (var itemId in rewards.items)
            {
                Inventory.InventoryManager.Instance?.AddItem(playerId, itemId, 1);
            }
        }

        #endregion

        #region Hint System

        public void ShowHint(ulong playerId, string hintId)
        {
            if (!hintDefinitions.ContainsKey(hintId)) return;

            if (!playerData.ContainsKey(playerId))
            {
                InitializePlayerData(playerId);
            }

            var data = playerData[playerId];
            var hint = hintDefinitions[hintId];

            // Check if already shown recently
            if (data.shownHints.Contains(hintId))
            {
                // Cooldown check (simplified - would use timestamp in real implementation)
                return;
            }

            data.shownHints.Add(hintId);

            OnHintShown?.Invoke(playerId, hintId);

            Debug.Log($"[TutorialSystem] Showing hint to player {playerId}: {hint.message}");

            // Notify client
            ShowHintClientRpc(playerId, hint.message, hintDisplayTime);
        }

        [ClientRpc]
        private void ShowHintClientRpc(ulong playerId, string message, float duration)
        {
            Debug.Log($"[TutorialSystem] HINT: {message}");
        }

        public void TriggerHint(ulong playerId, HintTrigger trigger)
        {
            var hint = hintDefinitions.Values.FirstOrDefault(h => h.trigger == trigger);

            if (hint != null)
            {
                ShowHint(playerId, hint.hintId);
            }
        }

        #endregion

        #region Public Getters

        public List<TutorialDefinition> GetAvailableTutorials(ulong playerId)
        {
            return tutorialDefinitions.Values
                .Where(t => CanStartTutorial(playerId, t.tutorialId))
                .ToList();
        }

        public List<TutorialDefinition> GetCompletedTutorials(ulong playerId)
        {
            if (!playerData.ContainsKey(playerId)) return new List<TutorialDefinition>();

            return playerData[playerId].completedTutorials
                .Select(id => tutorialDefinitions[id])
                .ToList();
        }

        public bool IsTutorialActive(ulong playerId)
        {
            return activeTutorials.ContainsKey(playerId);
        }

        public ActiveTutorial GetActiveTutorial(ulong playerId)
        {
            return activeTutorials.ContainsKey(playerId) ? activeTutorials[playerId] : null;
        }

        #endregion

        #region Data Persistence

        private void SavePlayerData(ulong playerId)
        {
            if (!playerData.ContainsKey(playerId)) return;

            var data = playerData[playerId];
            string json = JsonUtility.ToJson(data);

            SaveSystem.SaveManager.Instance?.SaveData($"tutorial_{playerId}", json);
        }

        public void LoadPlayerData(ulong playerId)
        {
            string json = SaveSystem.SaveManager.Instance?.LoadData($"tutorial_{playerId}");

            if (!string.IsNullOrEmpty(json))
            {
                var data = JsonUtility.FromJson<PlayerTutorialData>(json);
                playerData[playerId] = data;

                Debug.Log($"[TutorialSystem] Loaded tutorial data for player {playerId}");
            }
        }

        #endregion
    }

    #region Data Classes

    [Serializable]
    public class TutorialDefinition
    {
        public string tutorialId;
        public string tutorialName;
        public string description;
        public TutorialCategory category;
        public bool isRequired;
        public List<string> prerequisites;
        public List<TutorialStep> steps = new List<TutorialStep>();
        public TutorialReward rewards;
    }

    [Serializable]
    public class TutorialStep
    {
        public string stepId;
        public string instruction;
        public TutorialRequirement requirement;
        public float targetValue;
    }

    [Serializable]
    public class TutorialReward
    {
        public int xp;
        public int softCurrency;
        public int hardCurrency;
        public List<string> items = new List<string>();
    }

    [Serializable]
    public class ActiveTutorial
    {
        public string tutorialId;
        public ulong playerId;
        public int currentStep;
        public DateTime startTime;
        public Dictionary<int, float> stepProgress = new Dictionary<int, float>();
    }

    [Serializable]
    public class PlayerTutorialData
    {
        public ulong playerId;
        public List<string> completedTutorials = new List<string>();
        public List<string> shownHints = new List<string>();
        public Dictionary<string, int> tutorialProgress = new Dictionary<string, int>();
        public bool skipTutorials;
    }

    [Serializable]
    public class HintDefinition
    {
        public string hintId;
        public string message;
        public HintTrigger trigger;
        public int priority;
    }

    public enum TutorialCategory
    {
        Basic,
        Combat,
        Gameplay,
        Systems,
        Social,
        Advanced
    }

    public enum TutorialRequirement
    {
        Movement,
        Sprint,
        Jump,
        EquipWeapon,
        Aim,
        KillEnemies,
        Reload,
        OpenInventory,
        UseItem,
        DropItem,
        ReachLocation,
        Interact,
        SurviveTime,
        Extract,
        OpenCrafting,
        CraftItem,
        OpenMarket,
        BuyItem
    }

    public enum HintTrigger
    {
        LowHealth,
        LowAmmo,
        EnemyNearby,
        ExtractionAvailable,
        InventoryFull
    }

    #endregion
}
