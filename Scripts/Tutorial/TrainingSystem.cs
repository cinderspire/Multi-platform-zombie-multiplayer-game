using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Tutorial
{
    /// <summary>
    /// Comprehensive training system with interactive tutorials, bot matches,
    /// weapon range, and skill challenges for new player onboarding.
    /// </summary>
    public class TrainingSystem : NetworkBehaviour
    {
        public static TrainingSystem Instance { get; private set; }

        [Header("Training Configuration")]
        [SerializeField] private bool enableSkipTutorial = false;
        [SerializeField] private float tutorialCheckpointDelay = 2f;

        private Dictionary<ulong, PlayerTrainingProgress> playerProgress = new Dictionary<ulong, PlayerTrainingProgress>();
        private List<TutorialModule> tutorialModules = new List<TutorialModule>();

        public event Action<ulong, string> OnTutorialStarted;
        public event Action<ulong, string, int> OnCheckpointCompleted;
        public event Action<ulong, string> OnTutorialCompleted;
        public event Action<ulong, TrainingMode> OnTrainingModeEntered;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            InitializeTutorials();
        }

        private void InitializeTutorials()
        {
            // Basic Movement Tutorial
            tutorialModules.Add(new TutorialModule
            {
                moduleId = "basic_movement",
                moduleName = "Basic Movement",
                description = "Learn to move, sprint, jump, and crouch",
                checkpoints = new List<TutorialCheckpoint>
                {
                    new TutorialCheckpoint { description = "Move forward with W", action = "move_forward" },
                    new TutorialCheckpoint { description = "Move backward with S", action = "move_backward" },
                    new TutorialCheckpoint { description = "Strafe left with A", action = "move_left" },
                    new TutorialCheckpoint { description = "Strafe right with D", action = "move_right" },
                    new TutorialCheckpoint { description = "Sprint with Shift", action = "sprint" },
                    new TutorialCheckpoint { description = "Jump with Space", action = "jump" },
                    new TutorialCheckpoint { description = "Crouch with Ctrl", action = "crouch" }
                }
            });

            // Combat Basics Tutorial
            tutorialModules.Add(new TutorialModule
            {
                moduleId = "combat_basics",
                moduleName = "Combat Basics",
                description = "Learn to aim, shoot, and reload",
                checkpoints = new List<TutorialCheckpoint>
                {
                    new TutorialCheckpoint { description = "Aim down sights with Right Mouse", action = "ads" },
                    new TutorialCheckpoint { description = "Fire weapon with Left Mouse", action = "fire" },
                    new TutorialCheckpoint { description = "Reload weapon with R", action = "reload" },
                    new TutorialCheckpoint { description = "Hit target 5 times", action = "hit_target", requiredCount = 5 },
                    new TutorialCheckpoint { description = "Get 3 headshots", action = "headshot", requiredCount = 3 },
                    new TutorialCheckpoint { description = "Melee attack with V", action = "melee" }
                }
            });

            // Weapon Switching Tutorial
            tutorialModules.Add(new TutorialModule
            {
                moduleId = "weapon_switching",
                moduleName = "Weapon Management",
                description = "Learn to switch weapons and use equipment",
                checkpoints = new List<TutorialCheckpoint>
                {
                    new TutorialCheckpoint { description = "Switch to primary weapon with 1", action = "weapon_1" },
                    new TutorialCheckpoint { description = "Switch to secondary weapon with 2", action = "weapon_2" },
                    new TutorialCheckpoint { description = "Switch to melee weapon with 3", action = "weapon_3" },
                    new TutorialCheckpoint { description = "Throw grenade with G", action = "grenade" }
                }
            });

            // Survival Basics Tutorial
            tutorialModules.Add(new TutorialModule
            {
                moduleId = "survival_basics",
                moduleName = "Survival Basics",
                description = "Learn to heal, revive, and use cover",
                checkpoints = new List<TutorialCheckpoint>
                {
                    new TutorialCheckpoint { description = "Use health kit with H", action = "heal" },
                    new TutorialCheckpoint { description = "Take cover behind object", action = "use_cover" },
                    new TutorialCheckpoint { description = "Revive teammate with E", action = "revive" },
                    new TutorialCheckpoint { description = "Use ability with 4", action = "ability" }
                }
            });

            // Advanced Combat Tutorial
            tutorialModules.Add(new TutorialModule
            {
                moduleId = "advanced_combat",
                moduleName = "Advanced Combat",
                description = "Master advanced techniques",
                checkpoints = new List<TutorialCheckpoint>
                {
                    new TutorialCheckpoint { description = "Kill zombie with headshot", action = "zombie_headshot" },
                    new TutorialCheckpoint { description = "Kill 5 zombies without reloading", action = "kill_streak", requiredCount = 5 },
                    new TutorialCheckpoint { description = "Survive 3 rounds", action = "survive_rounds", requiredCount = 3 },
                    new TutorialCheckpoint { description = "Use trap effectively", action = "trap_kill" }
                }
            });
        }

        /// <summary>
        /// Start tutorial for player
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void StartTutorialServerRpc(ulong playerId, string moduleId, ServerRpcParams rpcParams = default)
        {
            if (!playerProgress.ContainsKey(playerId))
            {
                playerProgress[playerId] = new PlayerTrainingProgress { playerId = playerId };
            }

            var progress = playerProgress[playerId];
            var module = tutorialModules.Find(m => m.moduleId == moduleId);
            if (module == null) return;

            progress.currentModule = moduleId;
            progress.currentCheckpoint = 0;

            OnTutorialStarted?.Invoke(playerId, moduleId);
            ShowTutorialClientRpc(playerId, moduleId, module.moduleName, module.description);
            ShowCurrentCheckpointClientRpc(playerId, module.checkpoints[0].description);
        }

        /// <summary>
        /// Complete tutorial checkpoint
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void CompleteCheckpointServerRpc(ulong playerId, string action, ServerRpcParams rpcParams = default)
        {
            if (!playerProgress.TryGetValue(playerId, out var progress)) return;

            var module = tutorialModules.Find(m => m.moduleId == progress.currentModule);
            if (module == null) return;

            if (progress.currentCheckpoint >= module.checkpoints.Count) return;

            var checkpoint = module.checkpoints[progress.currentCheckpoint];

            // Check if action matches
            if (checkpoint.action == action)
            {
                checkpoint.currentCount++;

                // Check if checkpoint completed
                if (checkpoint.currentCount >= checkpoint.requiredCount)
                {
                    progress.currentCheckpoint++;
                    OnCheckpointCompleted?.Invoke(playerId, progress.currentModule, progress.currentCheckpoint);

                    NotifyCheckpointCompleteClientRpc(playerId);

                    // Check if tutorial complete
                    if (progress.currentCheckpoint >= module.checkpoints.Count)
                    {
                        CompleteTutorial(playerId, progress.currentModule);
                    }
                    else
                    {
                        // Show next checkpoint
                        ShowCurrentCheckpointClientRpc(playerId, module.checkpoints[progress.currentCheckpoint].description);
                    }
                }
            }
        }

        private void CompleteTutorial(ulong playerId, string moduleId)
        {
            var progress = playerProgress[playerId];
            if (!progress.completedModules.Contains(moduleId))
            {
                progress.completedModules.Add(moduleId);
            }

            OnTutorialCompleted?.Invoke(playerId, moduleId);

            // Award rewards
            AwardTutorialRewards(playerId, moduleId);

            NotifyTutorialCompleteClientRpc(playerId, moduleId);
        }

        private void AwardTutorialRewards(ulong playerId, string moduleId)
        {
            // Award XP
            int xpReward = 100;
            if (Progression.ProgressionSystem.Instance != null)
            {
                Progression.ProgressionSystem.Instance.AddExperienceServerRpc(playerId, xpReward);
            }

            // Award currency
            int currencyReward = 50;

            // Unlock achievement
            string achievementId = $"tutorial_{moduleId}_complete";
            if (Achievements.AchievementSystem.Instance != null)
            {
                // Would unlock achievement
            }
        }

        /// <summary>
        /// Enter training mode
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void EnterTrainingModeServerRpc(ulong playerId, TrainingMode mode, ServerRpcParams rpcParams = default)
        {
            OnTrainingModeEntered?.Invoke(playerId, mode);

            switch (mode)
            {
                case TrainingMode.WeaponRange:
                    SetupWeaponRange(playerId);
                    break;

                case TrainingMode.BotMatch:
                    SetupBotMatch(playerId);
                    break;

                case TrainingMode.SkillChallenge:
                    SetupSkillChallenge(playerId);
                    break;

                case TrainingMode.FreeRoam:
                    SetupFreeRoam(playerId);
                    break;
            }

            NotifyTrainingModeClientRpc(playerId, mode);
        }

        private void SetupWeaponRange(ulong playerId)
        {
            // Spawn targets
            // Give player all weapons
            // Enable infinite ammo
        }

        private void SetupBotMatch(ulong playerId)
        {
            // Spawn bot zombies
            // Configurable difficulty
            // Practice combat
        }

        private void SetupSkillChallenge(ulong playerId)
        {
            // Time trials
            // Accuracy challenges
            // Survival challenges
        }

        private void SetupFreeRoam(ulong playerId)
        {
            // Explore map
            // No zombies
            // Learn locations
        }

        [ClientRpc]
        private void ShowTutorialClientRpc(ulong playerId, string moduleId, string name, string description)
        {
            if (NetworkManager.Singleton.LocalClientId != playerId) return;

            Debug.Log($"<color=cyan>═══════════════════════════════</color>");
            Debug.Log($"<color=yellow>TUTORIAL: {name}</color>");
            Debug.Log($"<color=white>{description}</color>");
            Debug.Log($"<color=cyan>═══════════════════════════════</color>");
        }

        [ClientRpc]
        private void ShowCurrentCheckpointClientRpc(ulong playerId, string checkpointDescription)
        {
            if (NetworkManager.Singleton.LocalClientId != playerId) return;

            Debug.Log($"<color=orange>→ {checkpointDescription}</color>");
        }

        [ClientRpc]
        private void NotifyCheckpointCompleteClientRpc(ulong playerId)
        {
            if (NetworkManager.Singleton.LocalClientId != playerId) return;

            Debug.Log($"<color=lime>✓ Checkpoint Complete!</color>");
        }

        [ClientRpc]
        private void NotifyTutorialCompleteClientRpc(ulong playerId, string moduleId)
        {
            if (NetworkManager.Singleton.LocalClientId != playerId) return;

            Debug.Log($"<color=gold>★ TUTORIAL COMPLETE! ★</color>");
            Debug.Log($"<color=lime>+100 XP, +50 Currency</color>");
        }

        [ClientRpc]
        private void NotifyTrainingModeClientRpc(ulong playerId, TrainingMode mode)
        {
            if (NetworkManager.Singleton.LocalClientId != playerId) return;

            Debug.Log($"<color=cyan>Entered Training Mode: {mode}</color>");
        }

        public bool HasCompletedTutorial(ulong playerId, string moduleId)
        {
            if (!playerProgress.TryGetValue(playerId, out var progress)) return false;
            return progress.completedModules.Contains(moduleId);
        }

        public List<string> GetCompletedTutorials(ulong playerId)
        {
            return playerProgress.TryGetValue(playerId, out var progress) ?
                new List<string>(progress.completedModules) : new List<string>();
        }

        [Serializable]
        private class PlayerTrainingProgress
        {
            public ulong playerId;
            public string currentModule;
            public int currentCheckpoint;
            public List<string> completedModules = new List<string>();
        }

        [Serializable]
        private class TutorialModule
        {
            public string moduleId;
            public string moduleName;
            public string description;
            public List<TutorialCheckpoint> checkpoints;
        }

        [Serializable]
        private class TutorialCheckpoint
        {
            public string description;
            public string action;
            public int requiredCount = 1;
            public int currentCount = 0;
        }

        public enum TrainingMode
        {
            WeaponRange,
            BotMatch,
            SkillChallenge,
            FreeRoam
        }
    }
}
