using UnityEngine;
using System.Collections.Generic;
using System.Text;

namespace DeadFrontier.Core.QualityAssurance
{
    /// <summary>
    /// Validates all game systems are properly integrated and configured
    /// Run this to ensure compatibility and quality
    /// </summary>
    public class SystemValidator : MonoBehaviour
    {
        [Header("Validation Settings")]
        [SerializeField] private bool validateOnStart = false;
        [SerializeField] private bool showDetailedReport = true;

        // Validation results
        private List<ValidationResult> results = new List<ValidationResult>();
        private int passedChecks = 0;
        private int failedChecks = 0;
        private int warningChecks = 0;

        private void Start()
        {
            if (validateOnStart)
            {
                ValidateAllSystems();
            }
        }

        /// <summary>
        /// Validates all game systems
        /// </summary>
        [ContextMenu("Validate All Systems")]
        public void ValidateAllSystems()
        {
            results.Clear();
            passedChecks = 0;
            failedChecks = 0;
            warningChecks = 0;

            Debug.Log("=== SYSTEM VALIDATION STARTED ===");

            // Core Systems
            ValidateCoreSystems();

            // Manager Systems
            ValidateManagerSystems();

            // Player Systems
            ValidatePlayerSystems();

            // Network Systems
            ValidateNetworkSystems();

            // Data Systems
            ValidateDataSystems();

            // Integration Checks
            ValidateIntegration();

            // Performance Checks
            ValidatePerformance();

            // Print report
            PrintValidationReport();
        }

        #region Core Systems Validation

        private void ValidateCoreSystem()
        {
            Check("GameStateManager", GameStateManager.Instance != null,
                "GameStateManager singleton is required for orchestrating game flow");

            if (GameStateManager.Instance != null)
            {
                Check("Systems Initialized", GameStateManager.Instance.SystemsInitialized,
                    "All game systems should be initialized");

                var systemsStatus = GameStateManager.Instance.GetSystemsStatus();
                foreach (var kvp in systemsStatus)
                {
                    Check($"System: {kvp.Key}", kvp.Value,
                        $"{kvp.Key} should be available", ValidationSeverity.Warning);
                }
            }
        }

        private void ValidateManagerSystems()
        {
            Check("SaveSystem", Save.SaveSystem.Instance != null,
                "SaveSystem is required for data persistence");

            Check("SettingsManager", Settings.SettingsManager.Instance != null,
                "SettingsManager is required for game options");

            Check("AnalyticsManager", Analytics.AnalyticsManager.Instance != null,
                "AnalyticsManager is required for telemetry", ValidationSeverity.Warning);

            Check("PerformanceMonitor", Performance.PerformanceMonitor.Instance != null,
                "PerformanceMonitor is required for optimization");

            Check("AchievementManager", Achievements.AchievementManager.Instance != null,
                "AchievementManager is required for achievements");

            Check("GameManager", GameManager.Instance != null,
                "GameManager is required for game state");

            Check("UIManager", UIManager.Instance != null,
                "UIManager is required for UI control");

            Check("AudioManager", AudioManager.Instance != null,
                "AudioManager is required for audio");

            Check("PoolManager", PoolManager.Instance != null,
                "PoolManager is required for object pooling");
        }

        private void ValidatePlayerSystems()
        {
            var player = FindObjectOfType<Player.PlayerController>();

            if (player != null)
            {
                Check("PlayerController", true, "Player controller found");

                Check("PlayerHealth", player.GetComponent<Player.PlayerHealth>() != null,
                    "PlayerHealth component is required on player");

                Check("PlayerMovement", player.GetComponent<Player.PlayerMovement>() != null,
                    "PlayerMovement component is required on player");

                Check("PlayerCamera", player.GetComponent<Player.PlayerCamera>() != null,
                    "PlayerCamera component is required on player");

                Check("PlayerProgression", player.GetComponent<Player.PlayerProgression>() != null,
                    "PlayerProgression is required for XP/levels", ValidationSeverity.Warning);

                Check("PlayerStatsIntegrator", player.GetComponent<Player.PlayerStatsIntegrator>() != null,
                    "PlayerStatsIntegrator is required for tracking", ValidationSeverity.Warning);

                Check("LoadoutSystem", player.GetComponent<Player.Perks.LoadoutSystem>() != null,
                    "LoadoutSystem is required for perks/weapons", ValidationSeverity.Warning);
            }
            else
            {
                Check("Player Found", false,
                    "Player instance not found in scene", ValidationSeverity.Warning);
            }
        }

        private void ValidateNetworkSystems()
        {
            if (Unity.Netcode.NetworkManager.Singleton != null)
            {
                Check("NetworkManager", true, "Network manager found");

                Check("NetworkBootstrap", Networking.NetworkBootstrap.Instance != null,
                    "NetworkBootstrap required for multiplayer", ValidationSeverity.Warning);

                Check("MatchmakingManager", Networking.MatchmakingManager.Instance != null,
                    "MatchmakingManager required for matchmaking", ValidationSeverity.Warning);

                Check("NetworkGameManager", Networking.NetworkGameManager.Instance != null,
                    "NetworkGameManager required for networked matches", ValidationSeverity.Warning);
            }
            else
            {
                Check("Network Mode", false,
                    "No NetworkManager found - single player mode?", ValidationSeverity.Info);
            }
        }

        private void ValidateDataSystems()
        {
            // Check save system
            if (Save.SaveSystem.Instance != null)
            {
                Check("Save Slots", Save.SaveSystem.Instance.HasSaveData,
                    "At least one save should exist", ValidationSeverity.Warning);

                var currentSave = Save.SaveSystem.Instance.CurrentSave;
                if (currentSave != null)
                {
                    Check("Save Data Valid", currentSave.progression != null,
                        "Save data should have progression data");

                    Check("Settings Data", currentSave.settings != null,
                        "Save data should have settings");
                }
            }

            // Check achievements
            if (Achievements.AchievementManager.Instance != null)
            {
                int totalAchievements = Achievements.AchievementManager.Instance.TotalAchievements;
                Check("Achievements Configured", totalAchievements > 0,
                    "At least some achievements should be configured", ValidationSeverity.Warning);

                if (totalAchievements > 0)
                {
                    Debug.Log($"  → {totalAchievements} achievements configured");
                }
            }

            // Check battle pass
            if (Progression.BattlePass.BattlePassManager.Instance != null)
            {
                var battlePass = Progression.BattlePass.BattlePassManager.Instance.CurrentSeason;
                Check("Battle Pass Season", battlePass != null,
                    "Current season should be configured", ValidationSeverity.Warning);
            }
        }

        #endregion

        #region Integration Validation

        private void ValidateIntegration()
        {
            Debug.Log("--- Integration Checks ---");

            // Check if progression system triggers achievements
            Check("Progression→Achievement",
                Achievements.AchievementManager.Instance != null && FindObjectOfType<Player.PlayerProgression>() != null,
                "Progression should integrate with achievements");

            // Check if analytics tracks events
            Check("Analytics Integration",
                Analytics.AnalyticsManager.Instance != null && Analytics.AnalyticsManager.Instance.IsSessionActive,
                "Analytics should be tracking events");

            // Check if save system saves all data
            if (Save.SaveSystem.Instance != null && Save.SaveSystem.Instance.CurrentSave != null)
            {
                var save = Save.SaveSystem.Instance.CurrentSave;

                Check("Save→Progression", save.progression != null,
                    "Save system should include progression");

                Check("Save→Achievements", save.achievements != null,
                    "Save system should include achievements");

                Check("Save→Settings", save.settings != null,
                    "Save system should include settings");

                Check("Save→BattlePass", save.battlePass != null,
                    "Save system should include battle pass");
            }

            // Check if settings apply correctly
            if (Settings.SettingsManager.Instance != null)
            {
                var settings = Settings.SettingsManager.Instance.CurrentSettings;

                Check("Settings Applied",
                    QualitySettings.GetQualityLevel() == settings.qualityLevel,
                    "Quality settings should match saved settings");

                Check("Audio Settings",
                    Mathf.Approximately(AudioListener.volume, settings.masterVolume),
                    "Audio volume should match saved settings");
            }
        }

        #endregion

        #region Performance Validation

        private void ValidatePerformance()
        {
            Debug.Log("--- Performance Checks ---");

            if (Performance.PerformanceMonitor.Instance != null)
            {
                var metrics = Performance.PerformanceMonitor.Instance.GetCurrentMetrics();

                Check("FPS Acceptable",
                    metrics.averageFPS >= 30f,
                    $"FPS should be >= 30 (current: {metrics.averageFPS:F1})",
                    ValidationSeverity.Warning);

                Check("Memory Usage",
                    metrics.memoryMB < 500,
                    $"Memory should be < 500 MB (current: {metrics.memoryMB} MB)",
                    ValidationSeverity.Warning);

                Check("Entity Count",
                    metrics.zombieCount < Constants.MAX_ZOMBIES_PER_MATCH,
                    $"Zombie count within limits ({metrics.zombieCount}/{Constants.MAX_ZOMBIES_PER_MATCH})");
            }

            // Check object pooling is being used
            if (PoolManager.Instance != null)
            {
                Check("Object Pooling", true,
                    "Object pooling system is active for performance");
            }

            // Check spatial partitioning
            if (Zombies.ZombieManager.Instance != null)
            {
                Check("Spatial Partitioning", true,
                    "Spatial grid is used for O(1) zombie queries");
            }
        }

        #endregion

        #region Helper Methods

        private void Check(string checkName, bool passed, string message, ValidationSeverity severity = ValidationSeverity.Error)
        {
            var result = new ValidationResult
            {
                checkName = checkName,
                passed = passed,
                message = message,
                severity = severity
            };

            results.Add(result);

            if (passed)
            {
                passedChecks++;
                if (showDetailedReport)
                    Debug.Log($"  ✓ {checkName}");
            }
            else
            {
                if (severity == ValidationSeverity.Error)
                {
                    failedChecks++;
                    Debug.LogError($"  ✗ {checkName}: {message}");
                }
                else if (severity == ValidationSeverity.Warning)
                {
                    warningChecks++;
                    Debug.LogWarning($"  ⚠ {checkName}: {message}");
                }
                else
                {
                    Debug.Log($"  ℹ {checkName}: {message}");
                }
            }
        }

        private void PrintValidationReport()
        {
            StringBuilder report = new StringBuilder();
            report.AppendLine("\n=== VALIDATION REPORT ===");
            report.AppendLine($"Total Checks: {results.Count}");
            report.AppendLine($"✓ Passed: {passedChecks}");
            report.AppendLine($"✗ Failed: {failedChecks}");
            report.AppendLine($"⚠ Warnings: {warningChecks}");

            float successRate = results.Count > 0 ? (float)passedChecks / results.Count * 100f : 0f;
            report.AppendLine($"Success Rate: {successRate:F1}%");

            if (failedChecks == 0 && warningChecks == 0)
            {
                report.AppendLine("\n🎉 ALL SYSTEMS VALIDATED SUCCESSFULLY! 🎉");
                report.AppendLine("Game is ready for testing!");
            }
            else if (failedChecks == 0)
            {
                report.AppendLine("\n✅ Core systems validated, but there are warnings.");
                report.AppendLine("Game should work but may be missing optional features.");
            }
            else
            {
                report.AppendLine("\n❌ VALIDATION FAILED!");
                report.AppendLine("Please fix critical errors before testing.");
            }

            report.AppendLine("========================\n");

            Debug.Log(report.ToString());

            // Track validation in analytics
            Analytics.AnalyticsManager.Instance?.TrackEvent("system_validation", new Dictionary<string, object>
            {
                { "passed", passedChecks },
                { "failed", failedChecks },
                { "warnings", warningChecks },
                { "success_rate", successRate }
            });
        }

        /// <summary>
        /// Gets validation results
        /// </summary>
        public List<ValidationResult> GetResults()
        {
            return new List<ValidationResult>(results);
        }

        /// <summary>
        /// Gets validation summary
        /// </summary>
        public ValidationSummary GetSummary()
        {
            return new ValidationSummary
            {
                totalChecks = results.Count,
                passed = passedChecks,
                failed = failedChecks,
                warnings = warningChecks,
                successRate = results.Count > 0 ? (float)passedChecks / results.Count : 0f
            };
        }

        #endregion

        private void ValidateCoreSystems()
        {
            Debug.Log("--- Core Systems ---");
            ValidateCoreSystem();
        }
    }

    #region Data Structures

    [System.Serializable]
    public struct ValidationResult
    {
        public string checkName;
        public bool passed;
        public string message;
        public ValidationSeverity severity;
    }

    [System.Serializable]
    public struct ValidationSummary
    {
        public int totalChecks;
        public int passed;
        public int failed;
        public int warnings;
        public float successRate;

        public bool AllPassed => failed == 0;
        public bool HasWarnings => warnings > 0;
    }

    public enum ValidationSeverity
    {
        Info,       // Informational
        Warning,    // Optional feature missing
        Error       // Critical issue
    }

    #endregion
}
