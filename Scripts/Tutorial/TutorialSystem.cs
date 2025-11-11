using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

namespace ZombieGame
{
    /// <summary>
    /// Tutorial and Onboarding System - Essential for user experience
    /// Features: Interactive tutorials, tooltips, progressive learning
    /// First-time user experience (FTUE) optimization
    /// </summary>
    public class TutorialSystem : MonoBehaviour
    {
        public static TutorialSystem Instance { get; private set; }

        [Header("Tutorial Settings")]
        [SerializeField] private bool enableTutorials = true;
        [SerializeField] private bool skipForVeterans = true;
        [SerializeField] private float tooltipDuration = 5f;

        private Dictionary<string, Tutorial> tutorials = new Dictionary<string, Tutorial>();
        private Dictionary<string, bool> completedTutorials = new Dictionary<string, bool>();
        private Queue<TutorialStep> activeSteps = new Queue<TutorialStep>();
        private TutorialStep currentStep;

        // Events
        public event System.Action<string> OnTutorialStarted;
        public event System.Action<string> OnTutorialCompleted;
        public event System.Action<TutorialStep> OnStepCompleted;

        [System.Serializable]
        public class Tutorial
        {
            public string tutorialId;
            public string tutorialName;
            public string description;
            public TutorialCategory category;
            public List<TutorialStep> steps = new List<TutorialStep>();
            public bool isOptional = false;
            public int requiredLevel = 0;
        }

        [System.Serializable]
        public class TutorialStep
        {
            public string stepId;
            public string title;
            public string instruction;
            public StepType type;
            public string targetAction;
            public Vector3 highlightPosition;
            public string highlightObject;
            public bool pauseGame = false;
            public float timeLimit = 0f;
            public string completionCondition;
        }

        public enum TutorialCategory
        {
            BasicMovement, Combat, Survival, Multiplayer, Advanced, GameModes, Social, Customization
        }

        public enum StepType
        {
            Tooltip, HighlightUI, HighlightWorld, WaitForAction, Cutscene, Interactive
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeTutorials();
                LoadProgress();
            }
            else { Destroy(gameObject); }
        }

        private void InitializeTutorials()
        {
            CreateBasicMovementTutorial();
            CreateCombatTutorial();
            CreateSurvivalTutorial();
            CreateMultiplayerTutorial();
            Debug.Log($"[Tutorial] Initialized {tutorials.Count} tutorials");
        }

        private void CreateBasicMovementTutorial()
        {
            var tutorial = new Tutorial
            {
                tutorialId = "basic_movement",
                tutorialName = "Basic Movement",
                description = "Learn how to move and interact",
                category = TutorialCategory.BasicMovement,
                steps = new List<TutorialStep>
                {
                    new TutorialStep { stepId = "welcome", title = "Welcome!", instruction = "Welcome to the game!", type = StepType.Tooltip },
                    new TutorialStep { stepId = "move", title = "Movement", instruction = "Use WASD to move", type = StepType.WaitForAction, targetAction = "Move", completionCondition = "PlayerMoved" },
                    new TutorialStep { stepId = "look", title = "Camera", instruction = "Move mouse to look around", type = StepType.WaitForAction, targetAction = "Look", completionCondition = "CameraMoved" },
                    new TutorialStep { stepId = "sprint", title = "Sprint", instruction = "Hold Shift to sprint", type = StepType.WaitForAction, targetAction = "Sprint", completionCondition = "PlayerSprinted" },
                    new TutorialStep { stepId = "jump", title = "Jump", instruction = "Press Space to jump", type = StepType.WaitForAction, targetAction = "Jump", completionCondition = "PlayerJumped" }
                }
            };
            tutorials[tutorial.tutorialId] = tutorial;
        }

        private void CreateCombatTutorial()
        {
            var tutorial = new Tutorial
            {
                tutorialId = "basic_combat",
                tutorialName = "Basic Combat",
                description = "Learn how to fight zombies",
                category = TutorialCategory.Combat,
                steps = new List<TutorialStep>
                {
                    new TutorialStep { stepId = "equip", title = "Equip Weapon", instruction = "Press 1 to equip weapon", type = StepType.WaitForAction, targetAction = "EquipWeapon", completionCondition = "WeaponEquipped" },
                    new TutorialStep { stepId = "aim", title = "Aim", instruction = "Right-click to aim", type = StepType.WaitForAction, targetAction = "Aim", completionCondition = "PlayerAimed" },
                    new TutorialStep { stepId = "shoot", title = "Shoot", instruction = "Left-click to shoot", type = StepType.WaitForAction, targetAction = "Shoot", completionCondition = "PlayerShot" },
                    new TutorialStep { stepId = "reload", title = "Reload", instruction = "Press R to reload", type = StepType.WaitForAction, targetAction = "Reload", completionCondition = "WeaponReloaded" },
                    new TutorialStep { stepId = "kill", title = "Eliminate", instruction = "Defeat the zombie", type = StepType.WaitForAction, targetAction = "KillEnemy", completionCondition = "EnemyKilled" }
                }
            };
            tutorials[tutorial.tutorialId] = tutorial;
        }

        private void CreateSurvivalTutorial()
        {
            var tutorial = new Tutorial
            {
                tutorialId = "survival_basics",
                tutorialName = "Survival Basics",
                description = "Learn survival mechanics",
                category = TutorialCategory.Survival,
                steps = new List<TutorialStep>
                {
                    new TutorialStep { stepId = "health", title = "Health", instruction = "Monitor your health bar", type = StepType.HighlightUI, highlightObject = "HealthBar" },
                    new TutorialStep { stepId = "heal", title = "Healing", instruction = "Press H to use medkit", type = StepType.WaitForAction, targetAction = "UseHeal", completionCondition = "UsedMedkit" },
                    new TutorialStep { stepId = "loot", title = "Looting", instruction = "Press E to pick up items", type = StepType.WaitForAction, targetAction = "PickupItem", completionCondition = "ItemPickedUp" },
                    new TutorialStep { stepId = "inventory", title = "Inventory", instruction = "Press Tab for inventory", type = StepType.WaitForAction, targetAction = "OpenInventory", completionCondition = "InventoryOpened" }
                }
            };
            tutorials[tutorial.tutorialId] = tutorial;
        }

        private void CreateMultiplayerTutorial()
        {
            var tutorial = new Tutorial
            {
                tutorialId = "multiplayer_basics",
                tutorialName = "Multiplayer Basics",
                description = "Learn multiplayer features",
                category = TutorialCategory.Multiplayer,
                isOptional = true,
                steps = new List<TutorialStep>
                {
                    new TutorialStep { stepId = "team", title = "Teamwork", instruction = "Work with your team!", type = StepType.Tooltip },
                    new TutorialStep { stepId = "chat", title = "Chat", instruction = "Press Enter for chat", type = StepType.WaitForAction, targetAction = "OpenChat", completionCondition = "ChatOpened" },
                    new TutorialStep { stepId = "ping", title = "Ping", instruction = "Press Z to ping", type = StepType.WaitForAction, targetAction = "Ping", completionCondition = "PingSent" }
                }
            };
            tutorials[tutorial.tutorialId] = tutorial;
        }

        public void StartTutorial(string tutorialId)
        {
            if (!enableTutorials || !tutorials.ContainsKey(tutorialId)) return;
            if (IsTutorialCompleted(tutorialId) && skipForVeterans) return;

            var tutorial = tutorials[tutorialId];
            foreach (var step in tutorial.steps) activeSteps.Enqueue(step);

            OnTutorialStarted?.Invoke(tutorialId);
            Debug.Log($"[Tutorial] Started: {tutorial.tutorialName}");
            NextStep();
        }

        private void NextStep()
        {
            if (activeSteps.Count == 0) { CompleteTutorial(); return; }
            currentStep = activeSteps.Dequeue();
            if (currentStep.pauseGame) Time.timeScale = 0f;
            DisplayStep(currentStep);
        }

        private void DisplayStep(TutorialStep step)
        {
            Debug.Log($"[Tutorial] Step: {step.title} - {step.instruction}");
        }

        public void CompleteStep(string condition)
        {
            if (currentStep != null && currentStep.completionCondition == condition)
            {
                if (currentStep.pauseGame) Time.timeScale = 1f;
                OnStepCompleted?.Invoke(currentStep);
                NextStep();
            }
        }

        private void CompleteTutorial()
        {
            if (currentStep != null)
            {
                string tutorialId = FindTutorialIdByStep(currentStep);
                if (tutorialId != null)
                {
                    completedTutorials[tutorialId] = true;
                    SaveProgress();
                    OnTutorialCompleted?.Invoke(tutorialId);
                }
            }
            currentStep = null;
        }

        private string FindTutorialIdByStep(TutorialStep step)
        {
            foreach (var kvp in tutorials)
                if (kvp.Value.steps.Contains(step)) return kvp.Key;
            return null;
        }

        public bool IsTutorialCompleted(string id) => completedTutorials.ContainsKey(id) && completedTutorials[id];
        public void ResetTutorial(string id) { completedTutorials[id] = false; SaveProgress(); }
        public void ResetAllTutorials() { completedTutorials.Clear(); SaveProgress(); }

        private void SaveProgress()
        {
            foreach (var kvp in completedTutorials)
                PlayerPrefs.SetInt($"Tutorial_{kvp.Key}", kvp.Value ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void LoadProgress()
        {
            foreach (var tutorial in tutorials.Values)
                completedTutorials[tutorial.tutorialId] = PlayerPrefs.GetInt($"Tutorial_{tutorial.tutorialId}", 0) == 1;
        }

        public float GetCompletionPercentage()
        {
            if (tutorials.Count == 0) return 100f;
            int completed = 0;
            foreach (var t in tutorials.Values)
                if (IsTutorialCompleted(t.tutorialId)) completed++;
            return (float)completed / tutorials.Count * 100f;
        }
    }
}
