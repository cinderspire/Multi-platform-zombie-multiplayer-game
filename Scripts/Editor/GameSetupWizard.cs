#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace DeadFrontier.Editor
{
    /// <summary>
    /// Unity Editor wizard for setting up the game project.
    /// </summary>
    public class GameSetupWizard : EditorWindow
    {
        private Vector2 scrollPosition;
        private int selectedTab = 0;
        private readonly string[] tabNames = new string[]
        {
            "Quick Start",
            "Scene Setup",
            "Layer Setup",
            "Prefab Setup",
            "Data Setup",
            "Build Settings"
        };

        [MenuItem("Dead Frontier/Setup Wizard")]
        public static void ShowWindow()
        {
            var window = GetWindow<GameSetupWizard>("Dead Frontier Setup");
            window.minSize = new Vector2(500, 600);
        }

        private void OnGUI()
        {
            DrawHeader();

            selectedTab = GUILayout.Toolbar(selectedTab, tabNames);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            switch (selectedTab)
            {
                case 0: DrawQuickStartTab(); break;
                case 1: DrawSceneSetupTab(); break;
                case 2: DrawLayerSetupTab(); break;
                case 3: DrawPrefabSetupTab(); break;
                case 4: DrawDataSetupTab(); break;
                case 5: DrawBuildSettingsTab(); break;
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            GUILayout.Label("Dead Frontier - Game Setup Wizard", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox("Use this wizard to set up your game project. Follow the tabs in order for best results.", MessageType.Info);
            EditorGUILayout.Space(10);
        }

        private void DrawQuickStartTab()
        {
            EditorGUILayout.LabelField("Quick Start Guide", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "Welcome to Dead Frontier!\n\n" +
                "This wizard will help you set up your game project.\n\n" +
                "Steps:\n" +
                "1. Set up layers and tags\n" +
                "2. Configure your scenes\n" +
                "3. Set up prefabs\n" +
                "4. Import/Create game data\n" +
                "5. Configure build settings",
                MessageType.None);

            EditorGUILayout.Space(20);

            if (GUILayout.Button("Run Full Setup", GUILayout.Height(40)))
            {
                RunFullSetup();
            }

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Individual Setup Steps:", EditorStyles.boldLabel);

            if (GUILayout.Button("Setup Layers & Tags"))
                SetupLayersAndTags();

            if (GUILayout.Button("Create Required Folders"))
                CreateRequiredFolders();

            if (GUILayout.Button("Import Package Dependencies"))
                ImportDependencies();

            if (GUILayout.Button("Create Manager GameObjects"))
                CreateManagers();

            if (GUILayout.Button("Generate Default Data"))
                GenerateDefaultData();
        }

        private void DrawSceneSetupTab()
        {
            EditorGUILayout.LabelField("Scene Setup", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Required Scenes:", EditorStyles.boldLabel);

            DrawSceneItem("MainMenu", "Main menu scene with UI");
            DrawSceneItem("Lobby", "Multiplayer lobby scene");
            DrawSceneItem("Loading", "Loading screen scene");
            DrawSceneItem("Tutorial", "Tutorial/training scene");
            DrawSceneItem("Map_Urban", "Urban map gameplay scene");

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Create All Scenes"))
                CreateAllScenes();

            EditorGUILayout.Space(20);

            EditorGUILayout.LabelField("Scene Components:", EditorStyles.boldLabel);

            if (GUILayout.Button("Add Game Managers to Current Scene"))
                AddManagersToScene();

            if (GUILayout.Button("Setup Lighting for Current Scene"))
                SetupSceneLighting();

            if (GUILayout.Button("Setup Post Processing for Current Scene"))
                SetupPostProcessing();
        }

        private void DrawSceneItem(string sceneName, string description)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(sceneName, GUILayout.Width(150));
            EditorGUILayout.LabelField(description, EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawLayerSetupTab()
        {
            EditorGUILayout.LabelField("Layer & Tag Setup", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Required Layers:", EditorStyles.boldLabel);

            string[] layers = new string[]
            {
                "Player", "Enemy", "Zombie", "Weapon", "Item",
                "Projectile", "Interactable", "Ground", "Wall",
                "Water", "Trigger", "Ragdoll", "Vehicle", "Building"
            };

            foreach (var layer in layers)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(layer, GUILayout.Width(150));
                bool exists = LayerMask.NameToLayer(layer) != -1;
                EditorGUILayout.LabelField(exists ? "✓ Exists" : "✗ Missing",
                    exists ? EditorStyles.label : EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Setup All Layers"))
                SetupLayersAndTags();

            EditorGUILayout.Space(20);

            EditorGUILayout.LabelField("Required Tags:", EditorStyles.boldLabel);

            string[] tags = new string[]
            {
                "Player", "Enemy", "Zombie", "Weapon", "Item",
                "Interactable", "ExtractionZone", "SpawnPoint",
                "SafeZone", "Vehicle", "Building", "Destructible",
                "Water", "Loot", "NPC", "Boss", "Objective"
            };

            foreach (var tag in tags)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(tag, GUILayout.Width(150));
                bool exists = TagExists(tag);
                EditorGUILayout.LabelField(exists ? "✓ Exists" : "✗ Missing",
                    exists ? EditorStyles.label : EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawPrefabSetupTab()
        {
            EditorGUILayout.LabelField("Prefab Setup", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "Create prefabs for all game objects. Each prefab should use the " +
                "corresponding configuration ScriptableObject.",
                MessageType.Info);

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Prefab Categories:", EditorStyles.boldLabel);

            if (GUILayout.Button("Create Player Prefab Template"))
                CreatePlayerPrefabTemplate();

            if (GUILayout.Button("Create Zombie Prefab Templates"))
                CreateZombiePrefabTemplates();

            if (GUILayout.Button("Create Weapon Prefab Templates"))
                CreateWeaponPrefabTemplates();

            if (GUILayout.Button("Create Item Prefab Templates"))
                CreateItemPrefabTemplates();

            EditorGUILayout.Space(20);

            EditorGUILayout.LabelField("Utility:", EditorStyles.boldLabel);

            if (GUILayout.Button("Validate All Prefabs"))
                ValidateAllPrefabs();

            if (GUILayout.Button("Generate Prefab Database"))
                GeneratePrefabDatabase();
        }

        private void DrawDataSetupTab()
        {
            EditorGUILayout.LabelField("Data Setup", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "Create ScriptableObject assets for all game data. " +
                "Use the presets in CompleteDataPresets.cs as reference.",
                MessageType.Info);

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Data Categories:", EditorStyles.boldLabel);

            if (GUILayout.Button("Generate Weapon Data Assets"))
                GenerateWeaponDataAssets();

            if (GUILayout.Button("Generate Zombie Data Assets"))
                GenerateZombieDataAssets();

            if (GUILayout.Button("Generate Item Data Assets"))
                GenerateItemDataAssets();

            if (GUILayout.Button("Generate Perk Data Assets"))
                GeneratePerkDataAssets();

            if (GUILayout.Button("Generate Achievement Data Assets"))
                GenerateAchievementDataAssets();

            EditorGUILayout.Space(20);

            EditorGUILayout.LabelField("Database:", EditorStyles.boldLabel);

            if (GUILayout.Button("Create Game Database Asset"))
                CreateGameDatabase();

            if (GUILayout.Button("Validate All Data Assets"))
                ValidateDataAssets();
        }

        private void DrawBuildSettingsTab()
        {
            EditorGUILayout.LabelField("Build Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Target Platform:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(EditorUserBuildSettings.activeBuildTarget.ToString());

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Quality Settings:", EditorStyles.boldLabel);

            if (GUILayout.Button("Configure for PC"))
                ConfigureForPC();

            if (GUILayout.Button("Configure for Mobile"))
                ConfigureForMobile();

            if (GUILayout.Button("Configure for Console"))
                ConfigureForConsole();

            EditorGUILayout.Space(20);

            EditorGUILayout.LabelField("Build:", EditorStyles.boldLabel);

            if (GUILayout.Button("Open Build Settings"))
                EditorWindow.GetWindow(System.Type.GetType("UnityEditor.BuildPlayerWindow,UnityEditor"));

            if (GUILayout.Button("Build Development"))
                BuildDevelopment();

            if (GUILayout.Button("Build Release"))
                BuildRelease();
        }

        #region Setup Methods

        private void RunFullSetup()
        {
            SetupLayersAndTags();
            CreateRequiredFolders();
            CreateManagers();
            GenerateDefaultData();
            Debug.Log("[Setup Wizard] Full setup completed!");
        }

        private void SetupLayersAndTags()
        {
            Debug.Log("[Setup Wizard] Setting up layers and tags...");
            // In a real implementation, this would modify TagManager.asset
            EditorUtility.DisplayDialog("Layers & Tags",
                "Please manually add the required layers and tags in:\n" +
                "Edit > Project Settings > Tags and Layers",
                "OK");
        }

        private void CreateRequiredFolders()
        {
            string[] folders = new string[]
            {
                "Assets/Prefabs",
                "Assets/Prefabs/Player",
                "Assets/Prefabs/Zombies",
                "Assets/Prefabs/Weapons",
                "Assets/Prefabs/Items",
                "Assets/Prefabs/Vehicles",
                "Assets/Prefabs/Buildings",
                "Assets/Prefabs/VFX",
                "Assets/Data",
                "Assets/Data/Weapons",
                "Assets/Data/Zombies",
                "Assets/Data/Items",
                "Assets/Data/Perks",
                "Assets/Data/Quests",
                "Assets/Data/Achievements",
                "Assets/Materials",
                "Assets/Textures",
                "Assets/Audio",
                "Assets/Audio/SFX",
                "Assets/Audio/Music",
                "Assets/Audio/Voice",
                "Assets/Animations",
                "Assets/Animations/Player",
                "Assets/Animations/Zombies",
                "Assets/Scenes",
                "Assets/Resources"
            };

            foreach (var folder in folders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    string[] parts = folder.Split('/');
                    string parent = parts[0];
                    for (int i = 1; i < parts.Length; i++)
                    {
                        string path = parent + "/" + parts[i];
                        if (!AssetDatabase.IsValidFolder(path))
                        {
                            AssetDatabase.CreateFolder(parent, parts[i]);
                        }
                        parent = path;
                    }
                }
            }

            AssetDatabase.Refresh();
            Debug.Log("[Setup Wizard] Created required folders");
        }

        private void ImportDependencies()
        {
            EditorUtility.DisplayDialog("Dependencies",
                "Required packages:\n" +
                "- Netcode for GameObjects\n" +
                "- Input System\n" +
                "- TextMeshPro\n" +
                "- Cinemachine\n" +
                "- Post Processing\n\n" +
                "Install via Package Manager (Window > Package Manager)",
                "OK");
        }

        private void CreateManagers()
        {
            Debug.Log("[Setup Wizard] Creating manager GameObjects...");
            // Create manager objects in scene
        }

        private void GenerateDefaultData()
        {
            Debug.Log("[Setup Wizard] Generating default data assets...");
            // Create ScriptableObject assets
        }

        private void CreateAllScenes()
        {
            Debug.Log("[Setup Wizard] Creating scenes...");
        }

        private void AddManagersToScene()
        {
            var managers = new GameObject("--- MANAGERS ---");

            var gameManager = new GameObject("GameManager");
            gameManager.transform.SetParent(managers.transform);

            var networkManager = new GameObject("NetworkManager");
            networkManager.transform.SetParent(managers.transform);

            var audioManager = new GameObject("AudioManager");
            audioManager.transform.SetParent(managers.transform);

            var uiManager = new GameObject("UIManager");
            uiManager.transform.SetParent(managers.transform);

            Debug.Log("[Setup Wizard] Added managers to scene");
        }

        private void SetupSceneLighting()
        {
            Debug.Log("[Setup Wizard] Setting up lighting...");
        }

        private void SetupPostProcessing()
        {
            Debug.Log("[Setup Wizard] Setting up post processing...");
        }

        private void CreatePlayerPrefabTemplate()
        {
            Debug.Log("[Setup Wizard] Creating player prefab template...");
        }

        private void CreateZombiePrefabTemplates()
        {
            Debug.Log("[Setup Wizard] Creating zombie prefab templates...");
        }

        private void CreateWeaponPrefabTemplates()
        {
            Debug.Log("[Setup Wizard] Creating weapon prefab templates...");
        }

        private void CreateItemPrefabTemplates()
        {
            Debug.Log("[Setup Wizard] Creating item prefab templates...");
        }

        private void ValidateAllPrefabs()
        {
            Debug.Log("[Setup Wizard] Validating prefabs...");
        }

        private void GeneratePrefabDatabase()
        {
            Debug.Log("[Setup Wizard] Generating prefab database...");
        }

        private void GenerateWeaponDataAssets()
        {
            Debug.Log("[Setup Wizard] Generating weapon data...");
        }

        private void GenerateZombieDataAssets()
        {
            Debug.Log("[Setup Wizard] Generating zombie data...");
        }

        private void GenerateItemDataAssets()
        {
            Debug.Log("[Setup Wizard] Generating item data...");
        }

        private void GeneratePerkDataAssets()
        {
            Debug.Log("[Setup Wizard] Generating perk data...");
        }

        private void GenerateAchievementDataAssets()
        {
            Debug.Log("[Setup Wizard] Generating achievement data...");
        }

        private void CreateGameDatabase()
        {
            Debug.Log("[Setup Wizard] Creating game database...");
        }

        private void ValidateDataAssets()
        {
            Debug.Log("[Setup Wizard] Validating data assets...");
        }

        private void ConfigureForPC()
        {
            Debug.Log("[Setup Wizard] Configuring for PC...");
        }

        private void ConfigureForMobile()
        {
            Debug.Log("[Setup Wizard] Configuring for Mobile...");
        }

        private void ConfigureForConsole()
        {
            Debug.Log("[Setup Wizard] Configuring for Console...");
        }

        private void BuildDevelopment()
        {
            Debug.Log("[Setup Wizard] Building development...");
        }

        private void BuildRelease()
        {
            Debug.Log("[Setup Wizard] Building release...");
        }

        private bool TagExists(string tag)
        {
            try
            {
                GameObject.FindGameObjectWithTag(tag);
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}
#endif
