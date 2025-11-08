# Unity Project Structure
## Dead Frontier: Outbreak

**Unity Version:** 6.2 (6000.0.x)
**Last Updated:** November 2025

---

## 📁 Complete Folder Structure

```
Multi-platform-zombie-multiplayer-game/
├── .git/                           # Git version control
├── .gitattributes                  # Git LFS configuration
├── .gitignore                      # Unity .gitignore
├── README.md                       # Project overview
├── CONTRIBUTING.md                 # Contribution guidelines
├── LICENSE                         # Project license (MIT)
│
├── Assets/                         # Unity Assets folder
│   ├── _Project/                   # Game-specific assets (underscore = top of list)
│   │   │
│   │   ├── Art/                    # Visual assets
│   │   │   ├── Materials/          # Material files (.mat)
│   │   │   │   ├── Characters/
│   │   │   │   ├── Environment/
│   │   │   │   ├── Props/
│   │   │   │   ├── UI/
│   │   │   │   └── VFX/
│   │   │   │
│   │   │   ├── Models/             # 3D models (.fbx, .obj)
│   │   │   │   ├── Characters/
│   │   │   │   │   ├── Player/
│   │   │   │   │   └── Zombies/
│   │   │   │   │       ├── Runner/
│   │   │   │   │       ├── Walker/
│   │   │   │   │       ├── Tank/
│   │   │   │   │       ├── Exploder/
│   │   │   │   │       └── Screamer/
│   │   │   │   ├── Environment/
│   │   │   │   │   ├── Buildings/
│   │   │   │   │   ├── Props/
│   │   │   │   │   ├── Roads/
│   │   │   │   │   └── Vegetation/
│   │   │   │   └── Weapons/
│   │   │   │       ├── Pistol/
│   │   │   │       ├── Rifle/
│   │   │   │       ├── SMG/
│   │   │   │       ├── Shotgun/
│   │   │   │       ├── Sniper/
│   │   │   │       └── Melee/
│   │   │   │
│   │   │   ├── Textures/           # Texture files (.png, .jpg, .tga)
│   │   │   │   ├── Characters/
│   │   │   │   ├── Environment/
│   │   │   │   ├── Props/
│   │   │   │   ├── UI/
│   │   │   │   └── VFX/
│   │   │   │
│   │   │   ├── Animations/         # Animation files (.anim)
│   │   │   │   ├── Player/
│   │   │   │   │   ├── Idle.anim
│   │   │   │   │   ├── Walk.anim
│   │   │   │   │   ├── Run.anim
│   │   │   │   │   ├── Sprint.anim
│   │   │   │   │   ├── Jump.anim
│   │   │   │   │   ├── Shoot.anim
│   │   │   │   │   ├── Reload.anim
│   │   │   │   │   └── Death.anim
│   │   │   │   └── Zombies/
│   │   │   │       ├── Idle.anim
│   │   │   │       ├── Walk.anim
│   │   │   │       ├── Run.anim
│   │   │   │       ├── Attack.anim
│   │   │   │       └── Death.anim
│   │   │   │
│   │   │   ├── Animators/          # Animator controllers (.controller)
│   │   │   │   ├── PlayerAnimator.controller
│   │   │   │   ├── ZombieRunnerAnimator.controller
│   │   │   │   ├── ZombieWalkerAnimator.controller
│   │   │   │   └── ZombieTankAnimator.controller
│   │   │   │
│   │   │   └── Shaders/            # Custom shaders (.shader, .shadergraph)
│   │   │       ├── WaterShader.shadergraph
│   │   │       └── BloodDecalShader.shader
│   │   │
│   │   ├── Audio/                  # Audio assets
│   │   │   ├── Music/              # Background music
│   │   │   │   ├── MainMenu.mp3
│   │   │   │   ├── Lobby.mp3
│   │   │   │   ├── Gameplay_Calm.mp3
│   │   │   │   ├── Gameplay_Intense.mp3
│   │   │   │   ├── Victory.mp3
│   │   │   │   └── Defeat.mp3
│   │   │   │
│   │   │   ├── SFX/                # Sound effects
│   │   │   │   ├── Weapons/
│   │   │   │   │   ├── Pistol_Fire.wav
│   │   │   │   │   ├── Rifle_Fire.wav
│   │   │   │   │   ├── Shotgun_Fire.wav
│   │   │   │   │   ├── Reload.wav
│   │   │   │   │   ├── EmptyClick.wav
│   │   │   │   │   └── WeaponSwitch.wav
│   │   │   │   ├── Zombies/
│   │   │   │   │   ├── Idle_Growl_01.wav
│   │   │   │   │   ├── Chase_Scream.wav
│   │   │   │   │   ├── Attack_Bite.wav
│   │   │   │   │   └── Death_01.wav
│   │   │   │   ├── Player/
│   │   │   │   │   ├── Footstep_Concrete_01.wav
│   │   │   │   │   ├── Hurt_01.wav
│   │   │   │   │   ├── Death.wav
│   │   │   │   │   └── Heartbeat.wav
│   │   │   │   ├── Environment/
│   │   │   │   │   ├── Wind_Loop.wav
│   │   │   │   │   ├── Siren_Distant.wav
│   │   │   │   │   └── Helicopter.wav
│   │   │   │   └── UI/
│   │   │   │       ├── Button_Click.wav
│   │   │   │       ├── Menu_Select.wav
│   │   │   │       └── Notification.wav
│   │   │   │
│   │   │   └── Mixers/             # Audio mixers
│   │   │       ├── MasterMixer.mixer
│   │   │       ├── MusicMixer.mixer
│   │   │       ├── SFXMixer.mixer
│   │   │       └── VoiceMixer.mixer
│   │   │
│   │   ├── Prefabs/                # Prefab objects
│   │   │   ├── Characters/
│   │   │   │   ├── Player.prefab
│   │   │   │   ├── ZombieRunner.prefab
│   │   │   │   ├── ZombieWalker.prefab
│   │   │   │   ├── ZombieTank.prefab
│   │   │   │   ├── ZombieExploder.prefab
│   │   │   │   └── ZombieScreamer.prefab
│   │   │   │
│   │   │   ├── Weapons/
│   │   │   │   ├── Pistol.prefab
│   │   │   │   ├── Rifle.prefab
│   │   │   │   ├── SMG.prefab
│   │   │   │   ├── Shotgun.prefab
│   │   │   │   ├── Sniper.prefab
│   │   │   │   └── Melee_Bat.prefab
│   │   │   │
│   │   │   ├── Items/
│   │   │   │   ├── Loot/
│   │   │   │   │   ├── Ammo_Pistol.prefab
│   │   │   │   │   ├── Ammo_Rifle.prefab
│   │   │   │   │   ├── Medkit.prefab
│   │   │   │   │   ├── Bandage.prefab
│   │   │   │   │   └── GoldBar.prefab
│   │   │   │   └── Interactables/
│   │   │   │       ├── Door.prefab
│   │   │   │       ├── Chest.prefab
│   │   │   │       └── ExtractionPoint.prefab
│   │   │   │
│   │   │   ├── VFX/                # Visual effects
│   │   │   │   ├── MuzzleFlash.prefab
│   │   │   │   ├── BulletTracer.prefab
│   │   │   │   ├── BloodSplatter.prefab
│   │   │   │   ├── Explosion.prefab
│   │   │   │   └── HitMarker.prefab
│   │   │   │
│   │   │   ├── Environment/
│   │   │   │   ├── SpawnPoint.prefab
│   │   │   │   ├── LootSpawnPoint.prefab
│   │   │   │   └── ExtractionZone.prefab
│   │   │   │
│   │   │   ├── UI/
│   │   │   │   ├── HUD.prefab
│   │   │   │   ├── MainMenu.prefab
│   │   │   │   ├── Lobby.prefab
│   │   │   │   ├── DeathScreen.prefab
│   │   │   │   └── VictoryScreen.prefab
│   │   │   │
│   │   │   └── Managers/           # Manager prefabs
│   │   │       ├── GameManager.prefab
│   │   │       ├── NetworkManager.prefab
│   │   │       ├── AudioManager.prefab
│   │   │       ├── UIManager.prefab
│   │   │       └── PoolManager.prefab
│   │   │
│   │   ├── Scenes/                 # Unity scenes
│   │   │   ├── Bootstrap.unity           # Initial scene (loads managers)
│   │   │   ├── MainMenu.unity            # Main menu
│   │   │   ├── Lobby.unity               # Multiplayer lobby
│   │   │   ├── Loading.unity             # Loading screen
│   │   │   ├── Maps/
│   │   │   │   ├── DowntownRuins.unity   # MVP map
│   │   │   │   ├── MilitaryBase.unity    # Map 2
│   │   │   │   └── ForestOutpost.unity   # Map 3
│   │   │   └── Test/
│   │   │       ├── TestPlayer.unity      # Test player movement
│   │   │       ├── TestWeapons.unity     # Test weapons
│   │   │       ├── TestZombieAI.unity    # Test AI
│   │   │       └── TestNetworking.unity  # Test multiplayer
│   │   │
│   │   ├── Scripts/                # C# scripts (organized by feature)
│   │   │   ├── Core/               # Core systems
│   │   │   │   ├── Managers/
│   │   │   │   │   ├── GameManager.cs
│   │   │   │   │   ├── NetworkManager.cs
│   │   │   │   │   ├── AudioManager.cs
│   │   │   │   │   ├── UIManager.cs
│   │   │   │   │   ├── PoolManager.cs
│   │   │   │   │   ├── SceneLoader.cs
│   │   │   │   │   └── SaveManager.cs
│   │   │   │   ├── Events/
│   │   │   │   │   ├── GameEvent.cs
│   │   │   │   │   ├── GameEventListener.cs
│   │   │   │   │   └── VoidEvent.cs
│   │   │   │   ├── StateMachine/
│   │   │   │   │   ├── IState.cs
│   │   │   │   │   └── StateMachine.cs
│   │   │   │   └── Utilities/
│   │   │   │       ├── Singleton.cs
│   │   │   │       ├── ObjectPool.cs
│   │   │   │       ├── SpatialHash.cs
│   │   │   │       ├── Extensions.cs
│   │   │   │       └── Constants.cs
│   │   │   │
│   │   │   ├── Player/             # Player-related scripts
│   │   │   │   ├── PlayerController.cs
│   │   │   │   ├── PlayerInput.cs
│   │   │   │   ├── PlayerMovement.cs
│   │   │   │   ├── PlayerCamera.cs
│   │   │   │   ├── PlayerHealth.cs
│   │   │   │   ├── PlayerInventory.cs
│   │   │   │   ├── PlayerWeapon.cs
│   │   │   │   ├── PlayerAnimator.cs
│   │   │   │   ├── PlayerAudio.cs
│   │   │   │   └── PlayerNetworking.cs
│   │   │   │
│   │   │   ├── Zombies/            # Zombie AI
│   │   │   │   ├── ZombieAI.cs
│   │   │   │   ├── ZombieHealth.cs
│   │   │   │   ├── ZombieMovement.cs
│   │   │   │   ├── ZombieAnimator.cs
│   │   │   │   ├── ZombieNetworking.cs
│   │   │   │   ├── Sensors/
│   │   │   │   │   ├── ZombieSensors.cs
│   │   │   │   │   ├── VisionSensor.cs
│   │   │   │   │   └── HearingSensor.cs
│   │   │   │   ├── StateMachine/
│   │   │   │   │   ├── ZombieStateMachine.cs
│   │   │   │   │   ├── IdleState.cs
│   │   │   │   │   ├── PatrolState.cs
│   │   │   │   │   ├── ChaseState.cs
│   │   │   │   │   ├── AttackState.cs
│   │   │   │   │   ├── FleeState.cs
│   │   │   │   │   └── DeathState.cs
│   │   │   │   └── Managers/
│   │   │   │       ├── ZombieManager.cs
│   │   │   │       ├── ZombieSpawner.cs
│   │   │   │       └── HordeManager.cs
│   │   │   │
│   │   │   ├── Weapons/            # Weapon systems
│   │   │   │   ├── WeaponController.cs
│   │   │   │   ├── WeaponFire.cs
│   │   │   │   ├── WeaponRecoil.cs
│   │   │   │   ├── WeaponAmmo.cs
│   │   │   │   ├── WeaponSwitcher.cs
│   │   │   │   ├── WeaponNetworking.cs
│   │   │   │   └── Projectile.cs
│   │   │   │
│   │   │   ├── Items/              # Inventory & loot
│   │   │   │   ├── InventorySystem.cs
│   │   │   │   ├── InventorySlot.cs
│   │   │   │   ├── LootSystem.cs
│   │   │   │   ├── LootSpawner.cs
│   │   │   │   ├── LootPickup.cs
│   │   │   │   └── StashSystem.cs
│   │   │   │
│   │   │   ├── Networking/         # Multiplayer
│   │   │   │   ├── NetworkBootstrap.cs
│   │   │   │   ├── LobbyManager.cs
│   │   │   │   ├── MatchmakingManager.cs
│   │   │   │   ├── RelayManager.cs
│   │   │   │   ├── NetworkTransformExtended.cs
│   │   │   │   └── NetworkCompression.cs
│   │   │   │
│   │   │   ├── GameModes/          # Game mode logic
│   │   │   │   ├── ExtractionMode.cs
│   │   │   │   ├── HordeDefenseMode.cs
│   │   │   │   ├── MatchManager.cs
│   │   │   │   ├── RoundManager.cs
│   │   │   │   └── ExtractionPoint.cs
│   │   │   │
│   │   │   ├── UI/                 # User interface
│   │   │   │   ├── HUD/
│   │   │   │   │   ├── HealthBar.cs
│   │   │   │   │   ├── AmmoCounter.cs
│   │   │   │   │   ├── Minimap.cs
│   │   │   │   │   ├── Crosshair.cs
│   │   │   │   │   └── KillFeed.cs
│   │   │   │   ├── Menus/
│   │   │   │   │   ├── MainMenuUI.cs
│   │   │   │   │   ├── LobbyUI.cs
│   │   │   │   │   ├── SettingsUI.cs
│   │   │   │   │   ├── DeathScreenUI.cs
│   │   │   │   │   └── VictoryScreenUI.cs
│   │   │   │   ├── Inventory/
│   │   │   │   │   ├── InventoryUI.cs
│   │   │   │   │   ├── InventorySlotUI.cs
│   │   │   │   │   └── ItemTooltip.cs
│   │   │   │   └── Components/
│   │   │   │       ├── ButtonUI.cs
│   │   │   │       ├── SliderUI.cs
│   │   │   │       └── ToggleUI.cs
│   │   │   │
│   │   │   ├── Analytics/          # Tracking & metrics
│   │   │   │   ├── AnalyticsManager.cs
│   │   │   │   └── AnalyticsEvents.cs
│   │   │   │
│   │   │   ├── Monetization/       # F2P systems
│   │   │   │   ├── BattlePassManager.cs
│   │   │   │   ├── StoreManager.cs
│   │   │   │   ├── IAPManager.cs
│   │   │   │   └── RewardedAdManager.cs
│   │   │   │
│   │   │   └── Editor/             # Unity Editor scripts
│   │   │       ├── CustomInspectors/
│   │   │       ├── EditorTools/
│   │   │       └── BuildScripts/
│   │   │
│   │   ├── ScriptableObjects/      # Data assets
│   │   │   ├── Weapons/
│   │   │   │   ├── Pistol_Data.asset
│   │   │   │   ├── Rifle_Data.asset
│   │   │   │   ├── SMG_Data.asset
│   │   │   │   ├── Shotgun_Data.asset
│   │   │   │   └── Sniper_Data.asset
│   │   │   ├── Zombies/
│   │   │   │   ├── Runner_Config.asset
│   │   │   │   ├── Walker_Config.asset
│   │   │   │   ├── Tank_Config.asset
│   │   │   │   ├── Exploder_Config.asset
│   │   │   │   └── Screamer_Config.asset
│   │   │   ├── Items/
│   │   │   │   ├── Ammo_Pistol.asset
│   │   │   │   ├── Medkit.asset
│   │   │   │   ├── Bandage.asset
│   │   │   │   └── GoldBar.asset
│   │   │   ├── LootTables/
│   │   │   │   ├── ZombieWalker_Loot.asset
│   │   │   │   ├── PoliceStation_Loot.asset
│   │   │   │   └── Hospital_Loot.asset
│   │   │   └── Events/
│   │   │       ├── OnPlayerDied.asset
│   │   │       ├── OnZombieKilled.asset
│   │   │       └── OnExtractionStarted.asset
│   │   │
│   │   ├── Settings/               # Project settings
│   │   │   ├── Input/
│   │   │   │   └── PlayerInputActions.inputactions
│   │   │   ├── Rendering/
│   │   │   │   ├── UniversalRenderPipelineAsset.asset
│   │   │   │   └── UniversalRenderPipelineAsset_Renderer.asset
│   │   │   └── Quality/
│   │   │       ├── QualitySettings_PC_High.asset
│   │   │       ├── QualitySettings_PC_Low.asset
│   │   │       └── QualitySettings_Mobile.asset
│   │   │
│   │   └── Resources/              # Runtime-loaded assets
│   │       ├── Prefabs/
│   │       ├── Audio/
│   │       └── UI/
│   │
│   ├── Packages/                   # Unity packages (managed by Package Manager)
│   │   └── manifest.json
│   │
│   └── StreamingAssets/            # Asset bundles (for WebGL)
│       ├── Bundles/
│       └── Data/
│
├── Packages/                       # Unity Package Manager cache
│   ├── manifest.json
│   └── packages-lock.json
│
├── ProjectSettings/                # Unity project settings
│   ├── ProjectSettings.asset
│   ├── QualitySettings.asset
│   ├── TagManager.asset
│   ├── InputManager.asset
│   ├── Physics2DSettings.asset
│   ├── DynamicsManager.asset
│   └── NavMeshAreas.asset
│
├── UserSettings/                   # User-specific settings (gitignored)
│
├── Library/                        # Unity cache (gitignored)
│
├── Temp/                           # Temporary files (gitignored)
│
├── Builds/                         # Build outputs (gitignored)
│   ├── PC/
│   ├── Android/
│   ├── iOS/
│   └── WebGL/
│
├── Docs/                           # Documentation (you are here!)
│   ├── README.md
│   ├── GDD.md
│   ├── TDD.md
│   ├── UNITY6_COMPATIBILITY.md
│   ├── ROADMAP.md
│   ├── RESOURCES.md
│   ├── PROJECT_STRUCTURE.md
│   ├── TODO.md
│   ├── ART_STYLE_GUIDE.md
│   ├── AUDIO_BIBLE.md
│   └── MONETIZATION_STRATEGY.md
│
└── Tools/                          # External tools, scripts
    ├── Blender/                    # Blender project files
    ├── Audacity/                   # Audio project files
    └── Scripts/                    # Build automation, etc.
```

---

## 📝 Naming Conventions

### Folders
- **PascalCase:** `Models`, `Scripts`, `Prefabs`
- **Underscore prefix for project:** `_Project` (appears at top in Unity)

### Files (Assets)
- **Prefabs:** `PascalCase` - `Player.prefab`, `ZombieWalker.prefab`
- **Scenes:** `PascalCase` - `MainMenu.unity`, `DowntownRuins.unity`
- **Scripts:** `PascalCase` - `PlayerController.cs`, `ZombieAI.cs`
- **ScriptableObjects:** `PascalCase_Type` - `Pistol_Data.asset`, `Walker_Config.asset`
- **Animations:** `PascalCase` - `Walk.anim`, `Attack.anim`
- **Materials:** `PascalCase_Mat` - `Zombie_Mat`, `Concrete_Mat`
- **Textures:** `PascalCase_Descriptor` - `Zombie_Albedo`, `Concrete_Normal`

### Scene Hierarchy (In-Game Objects)
- **PascalCase:** `PlayerSpawnPoint`, `ExtractionZone`
- **Underscores for categories:** `_Managers`, `_Environment`, `_Lighting`

---

## 🔧 Git Setup

### .gitignore (Unity Specific)
```gitignore
# Unity generated
[Ll]ibrary/
[Tt]emp/
[Oo]bj/
[Bb]uild/
[Bb]uilds/
[Ll]ogs/
[Uu]serSettings/

# Unity3D generated meta files
*.pidb.meta
*.pdb.meta
*.mdb.meta

# Visual Studio cache directory
.vs/

# Rider cache directory
.idea/

# macOS
.DS_Store

# Windows
Thumbs.db

# Build results
*.apk
*.aab
*.ipa
*.exe

# Asset Store
AssetStoreTools/
```

### .gitattributes (Git LFS for Large Files)
```gitattributes
# 3D Models
*.fbx filter=lfs diff=lfs merge=lfs -text
*.obj filter=lfs diff=lfs merge=lfs -text
*.blend filter=lfs diff=lfs merge=lfs -text

# Audio
*.mp3 filter=lfs diff=lfs merge=lfs -text
*.wav filter=lfs diff=lfs merge=lfs -text
*.ogg filter=lfs diff=lfs merge=lfs -text

# Textures
*.png filter=lfs diff=lfs merge=lfs -text
*.jpg filter=lfs diff=lfs merge=lfs -text
*.tga filter=lfs diff=lfs merge=lfs -text
*.psd filter=lfs diff=lfs merge=lfs -text

# Video
*.mp4 filter=lfs diff=lfs merge=lfs -text
*.mov filter=lfs diff=lfs merge=lfs -text
```

---

## 🚀 Quick Setup Commands

### 1. Initialize Git
```bash
cd Multi-platform-zombie-multiplayer-game
git init
git lfs install
git add .
git commit -m "Initial commit: Project structure"
git remote add origin <your-repo-url>
git push -u origin main
```

### 2. Create Unity Project
```bash
# Open Unity Hub
# Click "New Project"
# Template: 3D (URP)
# Name: Multi-platform-zombie-multiplayer-game
# Location: <this-folder>
```

### 3. Create Folder Structure
**Option A: Manual (Unity Editor)**
- Right-click in Project window
- Create folders as per structure above

**Option B: Script (Faster)**
Create `Tools/Scripts/CreateFolderStructure.cs`:
```csharp
using UnityEditor;
using System.IO;

public class CreateFolderStructure : EditorWindow
{
    [MenuItem("Tools/Create Project Structure")]
    static void CreateFolders()
    {
        string[] folders = new string[]
        {
            "Assets/_Project",
            "Assets/_Project/Art",
            "Assets/_Project/Art/Materials",
            "Assets/_Project/Art/Models",
            "Assets/_Project/Art/Textures",
            "Assets/_Project/Art/Animations",
            "Assets/_Project/Audio",
            "Assets/_Project/Prefabs",
            "Assets/_Project/Scenes",
            "Assets/_Project/Scripts",
            // ... add all folders
        };

        foreach (var folder in folders)
        {
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
        }

        AssetDatabase.Refresh();
        Debug.Log("Project structure created!");
    }
}
```

---

## 📊 Scene Organization

### Bootstrap Scene (Loads First)
```
Bootstrap.unity
├── _Managers (Empty GameObject)
│   ├── GameManager (prefab)
│   ├── NetworkManager (prefab)
│   ├── AudioManager (prefab)
│   ├── UIManager (prefab)
│   └── PoolManager (prefab)
├── _EventSystem
│   └── EventSystem (Unity UI)
└── _Camera
    └── MainCamera (disabled, scene-specific cameras will be used)
```

### Game Scene (DowntownRuins.unity)
```
DowntownRuins.unity
├── _Lighting
│   ├── Directional Light
│   ├── Skybox
│   └── Light Probes
├── _Environment
│   ├── Buildings
│   ├── Roads
│   ├── Props
│   └── Vegetation
├── _NavMesh
│   └── NavMeshSurface (component on root)
├── _SpawnPoints
│   ├── PlayerSpawns (Empty)
│   │   ├── Spawn1
│   │   ├── Spawn2
│   │   └── ...
│   ├── ZombieSpawns (Empty)
│   │   ├── ZSpawn1
│   │   └── ...
│   └── LootSpawns (Empty)
│       └── ...
├── _ExtractionPoints
│   ├── Extraction1
│   ├── Extraction2
│   └── Extraction3
└── _DynamicObjects (runtime spawns here)
```

---

## 🎨 Layer Setup

**Edit > Project Settings > Tags and Layers:**

### Layers:
- 0: Default
- 3: TransparentFX
- 4: Ignore Raycast
- 5: UI
- 6: Water
- **8: Player**
- **9: Zombie**
- **10: Environment**
- **11: Projectile**
- **12: Loot**
- **13: Interactable**
- **14: ExtractionZone**

### Tags:
- Player
- Zombie
- Weapon
- Loot
- ExtractionPoint
- SpawnPoint
- MainCamera

### Sorting Layers (for 2D UI):
- Background
- Default
- Foreground
- UI
- Overlay

---

## 🔍 Physics Layer Collision Matrix

**Edit > Project Settings > Physics > Layer Collision Matrix:**

|                | Player | Zombie | Environment | Projectile | Loot |
|----------------|--------|--------|-------------|------------|------|
| **Player**     | ❌     | ✅     | ✅          | ❌         | ✅   |
| **Zombie**     | ✅     | ❌     | ✅          | ✅         | ❌   |
| **Environment**| ✅     | ✅     | ✅          | ✅         | ✅   |
| **Projectile** | ❌     | ✅     | ✅          | ❌         | ❌   |
| **Loot**       | ✅     | ❌     | ✅          | ❌         | ❌   |

**Optimization:** Disable unnecessary collisions (saves physics calculations)

---

## 📋 Prefab Variants

### Use Prefab Variants for Variations:
```
Zombie Base Prefab
├── ZombieRunner (variant)
├── ZombieWalker (variant)
├── ZombieTank (variant)
└── ZombieExploder (variant)
```

**Why:** Change base prefab, all variants update automatically.

---

## 🏗️ Build Configuration

### Platform Build Settings

#### PC (Windows)
```
Build Settings:
- Target Platform: Standalone
- Architecture: x86_64
- Compression: LZ4HC
- Scripting Backend: IL2CPP

Player Settings:
- Company Name: [Your Studio]
- Product Name: Dead Frontier Outbreak
- Version: 0.1.0
- Icon: [512x512 PNG]
```

#### Android
```
Build Settings:
- Target Platform: Android
- Texture Compression: ASTC
- Build System: Gradle
- Min API Level: 24
- Target API Level: 34

Player Settings:
- Package Name: com.yourstudio.deadfrontier
- Version: 0.1.0
- Bundle Version Code: 1
- Icon: [Adaptive Icon]
- Splash Screen: [Custom]
```

#### iOS
```
Build Settings:
- Target Platform: iOS
- Architecture: ARM64

Player Settings:
- Bundle Identifier: com.yourstudio.deadfrontier
- Version: 0.1.0
- Build Number: 1
- Requires iOS: 13.0
```

#### WebGL
```
Build Settings:
- Target Platform: WebGL
- Compression: Brotli
- Memory Size: 2048 MB
- Enable Exceptions: None

Player Settings:
- WebGL Template: Default
```

---

## ✅ Folder Structure Checklist

Before starting development, verify:

- [ ] Unity project created with URP template
- [ ] Git initialized with .gitignore and .gitattributes
- [ ] All main folders created (`Art`, `Audio`, `Prefabs`, `Scenes`, `Scripts`)
- [ ] ScriptableObjects folder ready
- [ ] Test scenes folder created
- [ ] Layers and tags configured
- [ ] Physics collision matrix optimized
- [ ] Build settings configured for all platforms

---

**Project structure is ready! Start implementing Week 1 tasks from ROADMAP.md** 🚀

*Last Updated: November 2025*
