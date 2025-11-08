using UnityEngine;
using UnityEditor;

namespace DeadFrontier.Editor
{
    /// <summary>
    /// Custom Unity Editor tools for Dead Frontier development
    /// Provides quick access to common development tasks
    /// </summary>
    public class DeadFrontierEditorTools : EditorWindow
    {
        private Vector2 scrollPosition;
        private bool showSystemValidation = true;
        private bool showQuickActions = true;
        private bool showTestingTools = true;
        private bool showSceneSetup = true;

        [MenuItem("Dead Frontier/Editor Tools")]
        public static void ShowWindow()
        {
            var window = GetWindow<DeadFrontierEditorTools>("DF Tools");
            window.minSize = new Vector2(400, 600);
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawHeader();
            EditorGUILayout.Space(10);

            if (showSystemValidation = EditorGUILayout.Foldout(showSystemValidation, "System Validation", true))
            {
                DrawSystemValidation();
            }

            EditorGUILayout.Space(10);

            if (showQuickActions = EditorGUILayout.Foldout(showQuickActions, "Quick Actions", true))
            {
                DrawQuickActions();
            }

            EditorGUILayout.Space(10);

            if (showTestingTools = EditorGUILayout.Foldout(showTestingTools, "Testing Tools", true))
            {
                DrawTestingTools();
            }

            EditorGUILayout.Space(10);

            if (showSceneSetup = EditorGUILayout.Foldout(showSceneSetup, "Scene Setup", true))
            {
                DrawSceneSetup();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            EditorGUILayout.LabelField("Dead Frontier", headerStyle, GUILayout.Height(30));
            EditorGUILayout.LabelField("Development Tools", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        }

        #region System Validation

        private void DrawSystemValidation()
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.LabelField("Validate game systems and integration", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(5);

            if (GUILayout.Button("Validate All Systems", GUILayout.Height(30)))
            {
                ValidateAllSystems();
            }

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Check Core Systems"))
            {
                CheckCoreSystems();
            }
            if (GUILayout.Button("Check Managers"))
            {
                CheckManagers();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Check Player Systems"))
            {
                CheckPlayerSystems();
            }
            if (GUILayout.Button("Check Network"))
            {
                CheckNetworkSystems();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void ValidateAllSystems()
        {
            Debug.Log("=== VALIDATING ALL SYSTEMS ===");

            var validator = FindObjectOfType<Core.QualityAssurance.SystemValidator>();
            if (validator != null)
            {
                validator.ValidateAllSystems();
            }
            else
            {
                Debug.LogWarning("SystemValidator not found in scene. Add it to validate systems.");
            }
        }

        private void CheckCoreSystems()
        {
            Debug.Log("--- Checking Core Systems ---");

            CheckSystem<Core.GameStateManager>("GameStateManager");
            CheckSystem<Core.Performance.PerformanceMonitor>("PerformanceMonitor");
            CheckSystem<Core.Analytics.AnalyticsManager>("AnalyticsManager");

            Debug.Log("Core systems check complete");
        }

        private void CheckManagers()
        {
            Debug.Log("--- Checking Managers ---");

            CheckSystem<Core.GameManager>("GameManager");
            CheckSystem<Core.UIManager>("UIManager");
            CheckSystem<Core.AudioManager>("AudioManager");
            CheckSystem<Core.PoolManager>("PoolManager");
            CheckSystem<Core.Save.SaveSystem>("SaveSystem");
            CheckSystem<Core.Settings.SettingsManager>("SettingsManager");
            CheckSystem<Core.Achievements.AchievementManager>("AchievementManager");
            CheckSystem<Core.Economy.EconomyManager>("EconomyManager");

            Debug.Log("Manager check complete");
        }

        private void CheckPlayerSystems()
        {
            Debug.Log("--- Checking Player Systems ---");

            var player = FindObjectOfType<Player.PlayerController>();
            if (player != null)
            {
                Debug.Log("✓ PlayerController found");

                CheckComponent<Player.PlayerHealth>(player.gameObject, "PlayerHealth");
                CheckComponent<Player.PlayerMovement>(player.gameObject, "PlayerMovement");
                CheckComponent<Player.PlayerCamera>(player.gameObject, "PlayerCamera");
                CheckComponent<Player.PlayerProgression>(player.gameObject, "PlayerProgression");
            }
            else
            {
                Debug.LogWarning("✗ PlayerController not found in scene");
            }

            Debug.Log("Player systems check complete");
        }

        private void CheckNetworkSystems()
        {
            Debug.Log("--- Checking Network Systems ---");

            if (Unity.Netcode.NetworkManager.Singleton != null)
            {
                Debug.Log("✓ NetworkManager found");
            }
            else
            {
                Debug.LogWarning("✗ NetworkManager not found");
            }

            CheckSystem<Networking.NetworkBootstrap>("NetworkBootstrap");
            CheckSystem<Networking.MatchmakingManager>("MatchmakingManager");
            CheckSystem<Networking.NetworkGameManager>("NetworkGameManager");

            Debug.Log("Network systems check complete");
        }

        private void CheckSystem<T>(string name) where T : Component
        {
            var system = FindObjectOfType<T>();
            if (system != null)
            {
                Debug.Log($"✓ {name} found");
            }
            else
            {
                Debug.LogWarning($"✗ {name} not found");
            }
        }

        private void CheckComponent<T>(GameObject obj, string name) where T : Component
        {
            if (obj.GetComponent<T>() != null)
            {
                Debug.Log($"  ✓ {name}");
            }
            else
            {
                Debug.LogWarning($"  ✗ {name} missing");
            }
        }

        #endregion

        #region Quick Actions

        private void DrawQuickActions()
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.LabelField("Common development actions", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear PlayerPrefs"))
            {
                if (EditorUtility.DisplayDialog("Clear PlayerPrefs", "Are you sure you want to clear all PlayerPrefs?", "Yes", "No"))
                {
                    PlayerPrefs.DeleteAll();
                    PlayerPrefs.Save();
                    Debug.Log("PlayerPrefs cleared");
                }
            }
            if (GUILayout.Button("Clear Save Data"))
            {
                ClearSaveData();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset Tutorial"))
            {
                PlayerPrefs.SetInt("TutorialCompleted", 0);
                PlayerPrefs.Save();
                Debug.Log("Tutorial progress reset");
            }
            if (GUILayout.Button("Reset Daily Rewards"))
            {
                PlayerPrefs.DeleteKey("DailyReward_Streak");
                PlayerPrefs.DeleteKey("DailyReward_TotalDays");
                PlayerPrefs.DeleteKey("DailyReward_ClaimedToday");
                PlayerPrefs.DeleteKey("DailyReward_LastClaim");
                PlayerPrefs.Save();
                Debug.Log("Daily rewards reset");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Take Screenshot"))
            {
                TakeScreenshot();
            }
            if (GUILayout.Button("Open Persistent Data"))
            {
                EditorUtility.RevealInFinder(Application.persistentDataPath);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void ClearSaveData()
        {
            if (EditorUtility.DisplayDialog("Clear Save Data", "Are you sure you want to delete all save files?", "Yes", "No"))
            {
                string savePath = System.IO.Path.Combine(Application.persistentDataPath, "Saves");
                if (System.IO.Directory.Exists(savePath))
                {
                    System.IO.Directory.Delete(savePath, true);
                    Debug.Log($"Deleted save directory: {savePath}");
                }
                else
                {
                    Debug.Log("No save directory found");
                }
            }
        }

        private void TakeScreenshot()
        {
            string filename = $"Screenshot_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
            string path = System.IO.Path.Combine(Application.dataPath, "..", "Screenshots");

            if (!System.IO.Directory.Exists(path))
            {
                System.IO.Directory.CreateDirectory(path);
            }

            string fullPath = System.IO.Path.Combine(path, filename);
            ScreenCapture.CaptureScreenshot(fullPath);
            Debug.Log($"Screenshot saved: {fullPath}");
        }

        #endregion

        #region Testing Tools

        private void DrawTestingTools()
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.LabelField("Runtime testing utilities", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(5);

            GUI.enabled = Application.isPlaying;

            if (GUILayout.Button("Spawn 10 Zombies", GUILayout.Height(25)))
            {
                SpawnTestZombies(10);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Give 1000 XP"))
            {
                GiveTestXP(1000);
            }
            if (GUILayout.Button("Give 10000 Currency"))
            {
                GiveTestCurrency(10000);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Unlock All Achievements"))
            {
                UnlockAllAchievements();
            }
            if (GUILayout.Button("Complete Battle Pass"))
            {
                CompleteBattlePass();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Set Level 50"))
            {
                SetPlayerLevel(50);
            }
            if (GUILayout.Button("God Mode"))
            {
                EnableGodMode();
            }
            EditorGUILayout.EndHorizontal();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Testing tools only available in Play Mode", MessageType.Info);
            }

            GUI.enabled = true;

            EditorGUILayout.EndVertical();
        }

        private void SpawnTestZombies(int count)
        {
            var zombieManager = FindObjectOfType<Zombies.ZombieManager>();
            if (zombieManager != null)
            {
                for (int i = 0; i < count; i++)
                {
                    Vector3 randomPos = Random.insideUnitSphere * 20f;
                    randomPos.y = 0;
                    zombieManager.SpawnZombie(Zombies.ZombieType.Walker, randomPos);
                }
                Debug.Log($"Spawned {count} test zombies");
            }
            else
            {
                Debug.LogWarning("ZombieManager not found");
            }
        }

        private void GiveTestXP(int amount)
        {
            var progression = FindObjectOfType<Player.PlayerProgression>();
            if (progression != null)
            {
                progression.AwardXP(amount);
                Debug.Log($"Awarded {amount} XP");
            }
            else
            {
                Debug.LogWarning("PlayerProgression not found");
            }
        }

        private void GiveTestCurrency(int amount)
        {
            if (Core.Economy.EconomyManager.Instance != null)
            {
                Core.Economy.EconomyManager.Instance.AddSoftCurrency(amount, "Editor test");
                Debug.Log($"Awarded {amount} currency");
            }
            else
            {
                Debug.LogWarning("EconomyManager not found");
            }
        }

        private void UnlockAllAchievements()
        {
            if (Core.Achievements.AchievementManager.Instance != null)
            {
                var achievements = Core.Achievements.AchievementManager.Instance.GetAllAchievements();
                foreach (var achievement in achievements)
                {
                    Core.Achievements.AchievementManager.Instance.UnlockAchievement(achievement.achievementID);
                }
                Debug.Log($"Unlocked {achievements.Count} achievements");
            }
            else
            {
                Debug.LogWarning("AchievementManager not found");
            }
        }

        private void CompleteBattlePass()
        {
            if (Core.Progression.BattlePass.BattlePassManager.Instance != null)
            {
                Core.Progression.BattlePass.BattlePassManager.Instance.AwardXP(1000000, "Editor test");
                Debug.Log("Completed Battle Pass");
            }
            else
            {
                Debug.LogWarning("BattlePassManager not found");
            }
        }

        private void SetPlayerLevel(int level)
        {
            var progression = FindObjectOfType<Player.PlayerProgression>();
            if (progression != null)
            {
                int xpNeeded = level * 1000; // Simplified
                progression.AwardXP(xpNeeded);
                Debug.Log($"Set player to level {level}");
            }
            else
            {
                Debug.LogWarning("PlayerProgression not found");
            }
        }

        private void EnableGodMode()
        {
            var player = FindObjectOfType<Player.PlayerHealth>();
            if (player != null)
            {
                player.SetHealth(float.MaxValue);
                Debug.Log("God mode enabled");
            }
            else
            {
                Debug.LogWarning("PlayerHealth not found");
            }
        }

        #endregion

        #region Scene Setup

        private void DrawSceneSetup()
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.LabelField("Quick scene setup helpers", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(5);

            if (GUILayout.Button("Create Essential Managers", GUILayout.Height(30)))
            {
                CreateEssentialManagers();
            }

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create Player"))
            {
                CreatePlayer();
            }
            if (GUILayout.Button("Create Zombie Spawner"))
            {
                CreateZombieSpawner();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create UI Canvas"))
            {
                CreateUICanvas();
            }
            if (GUILayout.Button("Create Lighting"))
            {
                CreateLighting();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void CreateEssentialManagers()
        {
            GameObject managers = new GameObject("=== MANAGERS ===");

            CreateManagerIfMissing<Core.GameStateManager>(managers.transform);
            CreateManagerIfMissing<Core.Performance.PerformanceMonitor>(managers.transform);
            CreateManagerIfMissing<Core.GameManager>(managers.transform);
            CreateManagerIfMissing<Core.UIManager>(managers.transform);
            CreateManagerIfMissing<Core.AudioManager>(managers.transform);
            CreateManagerIfMissing<Core.PoolManager>(managers.transform);
            CreateManagerIfMissing<Core.Save.SaveSystem>(managers.transform);
            CreateManagerIfMissing<Core.Settings.SettingsManager>(managers.transform);
            CreateManagerIfMissing<Core.Achievements.AchievementManager>(managers.transform);
            CreateManagerIfMissing<Core.Economy.EconomyManager>(managers.transform);
            CreateManagerIfMissing<Core.Input.InputManager>(managers.transform);

            Debug.Log("Created essential managers");
            Selection.activeGameObject = managers;
        }

        private void CreateManagerIfMissing<T>(Transform parent) where T : Component
        {
            if (FindObjectOfType<T>() == null)
            {
                GameObject obj = new GameObject(typeof(T).Name);
                obj.transform.SetParent(parent);
                obj.AddComponent<T>();
            }
        }

        private void CreatePlayer()
        {
            GameObject player = new GameObject("Player");
            player.AddComponent<Player.PlayerController>();
            player.AddComponent<Player.PlayerHealth>();
            player.AddComponent<Player.PlayerMovement>();
            player.AddComponent<Player.PlayerCamera>();
            player.AddComponent<Player.PlayerProgression>();

            Debug.Log("Created player with essential components");
            Selection.activeGameObject = player;
        }

        private void CreateZombieSpawner()
        {
            GameObject spawner = new GameObject("ZombieManager");
            spawner.AddComponent<Zombies.ZombieManager>();

            Debug.Log("Created zombie manager");
            Selection.activeGameObject = spawner;
        }

        private void CreateUICanvas()
        {
            GameObject canvas = new GameObject("UI Canvas");
            canvas.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            Debug.Log("Created UI canvas");
            Selection.activeGameObject = canvas;
        }

        private void CreateLighting()
        {
            GameObject light = new GameObject("Directional Light");
            Light lightComponent = light.AddComponent<Light>();
            lightComponent.type = LightType.Directional;
            lightComponent.intensity = 1f;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);

            Debug.Log("Created directional light");
            Selection.activeGameObject = light;
        }

        #endregion
    }
}
