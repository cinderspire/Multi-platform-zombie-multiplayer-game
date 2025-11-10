using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Tutorial
{
    public class TutorialSystem : NetworkBehaviour
    {
        public static TutorialSystem Instance { get; private set; }

        private Dictionary<string, TutorialStep> tutorialSteps = new Dictionary<string, TutorialStep>();
        private Dictionary<ulong, PlayerTutorialData> playerTutorialData = new Dictionary<ulong, PlayerTutorialData>();

        public event Action<ulong, string> OnTutorialStepCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer) InitializeTutorial();
        }

        private void InitializeTutorial()
        {
            // BASIC TUTORIAL CHAIN
            tutorialSteps["welcome"] = new TutorialStep { stepId = "welcome", stepName = "Welcome", description = "Welcome to the apocalypse", category = TutorialCategory.Basics, completionType = CompletionType.Manual, rewards = new TutorialRewards { xp = 100, currency = 50 } };
            tutorialSteps["movement"] = new TutorialStep { stepId = "movement", stepName = "Movement", description = "Learn to move around", category = TutorialCategory.Basics, completionType = CompletionType.Distance, requiredAmount = 100, prerequisites = new List<string> { "welcome" }, rewards = new TutorialRewards { xp = 100, currency = 50 } };
            tutorialSteps["combat"] = new TutorialStep { stepId = "combat", stepName = "Combat Basics", description = "Kill your first zombie", category = TutorialCategory.Combat, completionType = CompletionType.Kill, requiredAmount = 1, prerequisites = new List<string> { "movement" }, rewards = new TutorialRewards { xp = 200, currency = 100 } };
            tutorialSteps["inventory"] = new TutorialStep { stepId = "inventory", stepName = "Inventory", description = "Open your inventory", category = TutorialCategory.Systems, completionType = CompletionType.Manual, prerequisites = new List<string> { "combat" }, rewards = new TutorialRewards { xp = 100, currency = 50 } };
            tutorialSteps["looting"] = new TutorialStep { stepId = "looting", stepName = "Looting", description = "Loot your first container", category = TutorialCategory.Systems, completionType = CompletionType.Loot, requiredAmount = 1, prerequisites = new List<string> { "inventory" }, rewards = new TutorialRewards { xp = 150, currency = 75, items = new List<string> { "consumable_medkit" } } };

            // ADVANCED TUTORIAL
            tutorialSteps["crafting"] = new TutorialStep { stepId = "crafting", stepName = "Crafting", description = "Craft your first item", category = TutorialCategory.Systems, completionType = CompletionType.Craft, requiredAmount = 1, prerequisites = new List<string> { "looting" }, rewards = new TutorialRewards { xp = 200, currency = 100 } };
            tutorialSteps["base_building"] = new TutorialStep { stepId = "base_building", stepName = "Base Building", description = "Build a structure", category = TutorialCategory.Advanced, completionType = CompletionType.Build, requiredAmount = 1, prerequisites = new List<string> { "crafting" }, rewards = new TutorialRewards { xp = 300, currency = 150 } };
            tutorialSteps["party"] = new TutorialStep { stepId = "party", stepName = "Party Up", description = "Join or create a party", category = TutorialCategory.Social, completionType = CompletionType.Manual, prerequisites = new List<string> { "combat" }, rewards = new TutorialRewards { xp = 200, currency = 100 } };
            tutorialSteps["clan"] = new TutorialStep { stepId = "clan", stepName = "Clan System", description = "Join a clan", category = TutorialCategory.Social, completionType = CompletionType.Manual, prerequisites = new List<string> { "party" }, rewards = new TutorialRewards { xp = 300, currency = 200 } };

            // COMPLETION REWARDS
            tutorialSteps["tutorial_complete"] = new TutorialStep { stepId = "tutorial_complete", stepName = "Tutorial Complete", description = "Complete all tutorial steps", category = TutorialCategory.Completion, completionType = CompletionType.Manual, rewards = new TutorialRewards { xp = 1000, currency = 500, hardCurrency = 50, items = new List<string> { "crate_rare", "weapon_rifle_m4" }, cosmetics = new List<string> { "title_graduate" } } };

            Debug.Log($"Initialized {tutorialSteps.Count} tutorial steps");
        }

        [ServerRpc(RequireOwnership = false)]
        public void InitializePlayerTutorialServerRpc(ulong playerId, ServerRpcParams rpcParams = default)
        {
            if (playerTutorialData.ContainsKey(playerId)) return;

            playerTutorialData[playerId] = new PlayerTutorialData
            {
                playerId = playerId,
                completedSteps = new List<string>(),
                currentStep = "welcome",
                tutorialEnabled = true,
                progress = new Dictionary<string, int>()
            };
        }

        [ServerRpc(RequireOwnership = false)]
        public void UpdateTutorialProgressServerRpc(ulong playerId, CompletionType type, int amount, ServerRpcParams rpcParams = default)
        {
            if (!playerTutorialData.TryGetValue(playerId, out var data)) return;
            if (!data.tutorialEnabled) return;

            var currentStepData = tutorialSteps.GetValueOrDefault(data.currentStep);
            if (currentStepData == null) return;

            if (currentStepData.completionType == type)
            {
                if (!data.progress.ContainsKey(data.currentStep))
                {
                    data.progress[data.currentStep] = 0;
                }

                data.progress[data.currentStep] += amount;

                if (data.progress[data.currentStep] >= currentStepData.requiredAmount)
                {
                    CompleteTutorialStepServerRpc(playerId, data.currentStep);
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void CompleteTutorialStepServerRpc(ulong playerId, string stepId, ServerRpcParams rpcParams = default)
        {
            if (!playerTutorialData.TryGetValue(playerId, out var data)) return;
            if (data.completedSteps.Contains(stepId)) return;

            var step = tutorialSteps.GetValueOrDefault(stepId);
            if (step == null) return;

            data.completedSteps.Add(stepId);

            // Grant rewards
            if (step.rewards != null)
            {
                if (step.rewards.xp > 0)
                    Progression.ProgressionSystem.Instance?.AddExperienceServerRpc(playerId, step.rewards.xp, "tutorial");
                if (step.rewards.currency > 0)
                    Economy.EconomyManager.Instance?.AddCurrencyServerRpc(playerId, Economy.CurrencyType.Soft, step.rewards.currency);
                if (step.rewards.hardCurrency > 0)
                    Economy.EconomyManager.Instance?.AddCurrencyServerRpc(playerId, Economy.CurrencyType.Hard, step.rewards.hardCurrency);
                
                foreach (var itemId in step.rewards.items)
                {
                    Inventory.InventorySystem.Instance?.AddItemServerRpc(playerId, itemId, 1, Inventory.ContainerType.Backpack);
                }
            }

            // Find next step
            string nextStep = FindNextTutorialStep(data);
            data.currentStep = nextStep;

            OnTutorialStepCompleted?.Invoke(playerId, stepId);
            NotifyTutorialStepCompletedClientRpc(playerId, stepId, nextStep);

            Debug.Log($"Player {playerId} completed tutorial step: {step.stepName}");
        }

        private string FindNextTutorialStep(PlayerTutorialData data)
        {
            foreach (var step in tutorialSteps.Values)
            {
                if (data.completedSteps.Contains(step.stepId)) continue;

                bool hasPrereqs = step.prerequisites == null || step.prerequisites.All(p => data.completedSteps.Contains(p));
                if (hasPrereqs)
                {
                    return step.stepId;
                }
            }

            return null; // Tutorial complete
        }

        [ClientRpc]
        private void NotifyTutorialStepCompletedClientRpc(ulong playerId, string completedStep, string nextStep) { }

        public PlayerTutorialData GetPlayerTutorialData(ulong playerId) => playerTutorialData.GetValueOrDefault(playerId);
        public TutorialStep GetTutorialStep(string stepId) => tutorialSteps.GetValueOrDefault(stepId);
    }

    [Serializable]
    public class TutorialStep
    {
        public string stepId;
        public string stepName;
        public string description;
        public TutorialCategory category;
        public CompletionType completionType;
        public int requiredAmount;
        public List<string> prerequisites;
        public TutorialRewards rewards;
    }

    [Serializable]
    public class TutorialRewards
    {
        public int xp;
        public int currency;
        public int hardCurrency;
        public List<string> items = new List<string>();
        public List<string> cosmetics = new List<string>();
    }

    [Serializable]
    public class PlayerTutorialData
    {
        public ulong playerId;
        public List<string> completedSteps;
        public string currentStep;
        public bool tutorialEnabled;
        public Dictionary<string, int> progress;
    }

    public enum TutorialCategory { Basics, Combat, Systems, Advanced, Social, Completion }
    public enum CompletionType { Manual, Kill, Loot, Craft, Build, Distance }
}
