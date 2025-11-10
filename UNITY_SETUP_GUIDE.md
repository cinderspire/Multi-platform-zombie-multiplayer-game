# 🎮 Unity Setup Guide - Complete Installation Instructions

## 📋 Prerequisites

- Unity 2022.3 LTS or newer
- Unity Netcode for GameObjects 2.0+
- TextMeshPro (install via Package Manager)
- Input System (optional, using legacy input currently)

---

## 🚀 STEP-BY-STEP SETUP

### **STEP 1: Create Unity Project**

1. Open Unity Hub
2. Create new project with **3D (URP)** template
3. Name it: `ZombieMultiplayerGame`
4. Unity version: **2022.3 LTS** or newer

### **STEP 2: Install Required Packages**

Open **Window > Package Manager** and install:

1. **Netcode for GameObjects**
   - Unity Registry → Search "Netcode for GameObjects"
   - Install latest version (2.0+)

2. **TextMeshPro**
   - Unity Registry → Search "TextMeshPro"
   - Import TMP Essentials when prompted

3. **ProBuilder** (Optional, for level design)

### **STEP 3: Import Project Scripts**

1. Copy all `Scripts/` folder to `Assets/Scripts/`
2. Wait for Unity to compile (may take 2-3 minutes)
3. Fix any namespace references if needed

---

## 🏗️ SCENE SETUP

### **STEP 4: Create Main Game Scene**

1. Create new scene: `GameScene`
2. Save to `Assets/Scenes/GameScene.unity`

### **STEP 5: Setup Network Manager**

**Create NetworkManager GameObject:**

```
Hierarchy:
└── NetworkManager
    ├── SystemManagers (Empty GameObject)
    └── UI (Canvas)
```

**On NetworkManager:**
1. Add Component: `NetworkManager`
2. Select Transport: `Unity Transport`
3. Configure:
   - Connection Data: `UnityTransport`
   - Address: `127.0.0.1` (for local testing)
   - Port: `7777`

**Add Network Prefabs List** (will add later)

### **STEP 6: Create System Managers**

Under `NetworkManager/SystemManagers`, create these GameObjects:

```
SystemManagers/
├── Core
│   ├── GameManager (add GameManager.cs)
│   └── DebugManager (add DebugManager.cs)
├── Health
│   ├── HealthSystem (add HealthSystem.cs)
│   └── StaminaSystem (add StaminaSystem.cs)
├── Combat
│   ├── CombatSystem (add CombatSystem.cs)
│   ├── DamageSystem (add DamageSystem.cs)
│   ├── WeaponFiringSystem (add WeaponFiringSystem.cs)
│   └── ReloadSystem (add ReloadSystem.cs)
├── AI
│   ├── SpawnerSystem (add SpawnerSystem.cs)
│   ├── PathfindingSystem (add PathfindingSystem.cs)
│   └── AIDirectorSystem (add AIDirectorSystem.cs)
├── Network
│   ├── NetworkSpawnManager (add NetworkSpawnManager.cs)
│   ├── LobbySystem (add LobbySystem.cs)
│   ├── ChatSystem (add ChatSystem.cs)
│   └── TeamSystem (add TeamSystem.cs)
├── UI
│   └── HUDSystem (add HUDSystem.cs)
├── Audio
│   └── AudioSystem (add AudioSystem.cs)
├── Map
│   ├── MapSystem (add MapSystem.cs)
│   ├── SceneManagement (add SceneManagement.cs)
│   └── ObjectiveSystem (add ObjectiveSystem.cs)
└── All Other Systems...
```

**IMPORTANT**: Add `NetworkObject` component to each system GameObject!

### **STEP 7: Create Player Prefab**

1. Create new GameObject: `Player`
2. Add components:
   ```
   - NetworkObject (Owner: Client)
   - CharacterController
   - PlayerController.cs
   - AnimationController.cs
   ```

3. Create child GameObject: `Camera`
   - Add: `Camera` component
   - Add: `CameraController.cs`
   - Position: (0, 1.6, 0)

4. Save as Prefab: `Assets/Prefabs/Player.prefab`
5. Add to NetworkManager's **Network Prefabs List**

### **STEP 8: Create Zombie Prefabs**

For each zombie type (Walker, Runner, Tank, Spitter, Exploder):

1. Create GameObject: `Zombie_Walker`
2. Add components:
   ```
   - NetworkObject (Owner: Server)
   - NavMeshAgent
   - ZombieAI.cs
   - CapsuleCollider
   ```
3. Configure ZombieAI:
   - Zombie Type: Walker
   - Detection Range: 15
   - Attack Range: 2
   - Attack Damage: 10

4. Save as Prefab: `Assets/Prefabs/Zombies/Zombie_Walker.prefab`
5. Repeat for other zombie types
6. Add all to SpawnerSystem's **Zombie Prefabs** array

### **STEP 9: Setup Spawn Points**

```
Create Empty GameObjects:
└── SpawnPoints
    ├── PlayerSpawn1 (position: 0, 0, 0)
    ├── PlayerSpawn2 (position: 5, 0, 0)
    ├── PlayerSpawn3 (position: -5, 0, 0)
    └── PlayerSpawn4 (position: 0, 0, 5)

└── ZombieSpawnPoints
    ├── ZombieSpawn1 (position: 20, 0, 0)
    ├── ZombieSpawn2 (position: -20, 0, 0)
    ├── ZombieSpawn3 (position: 0, 0, 20)
    └── ZombieSpawn4 (position: 0, 0, -20)
```

Assign to:
- **NetworkSpawnManager**: Player spawn points
- **SpawnerSystem**: Zombie spawn points

### **STEP 10: Bake NavMesh**

1. Select all ground objects
2. Mark as **Navigation Static**
3. **Window > AI > Navigation**
4. **Bake** tab → Click **Bake**

### **STEP 11: Setup UI**

Create Canvas:
```
UI (Canvas)
├── HUD
│   ├── HealthBar (Slider)
│   ├── ArmorBar (Slider)
│   ├── StaminaBar (Slider)
│   ├── AmmoText (TextMeshPro)
│   ├── Crosshair (Image)
│   └── KillFeed (Vertical Layout Group)
├── MainMenu
│   ├── HostButton (Button)
│   ├── JoinButton (Button)
│   └── QuitButton (Button)
└── PauseMenu
    ├── ResumeButton (Button)
    ├── SettingsButton (Button)
    └── QuitButton (Button)
```

Link to HUDSystem and MenuSystem components.

### **STEP 12: Setup Audio**

1. Create GameObject: `AudioManager`
2. Add `AudioSystem.cs`
3. Add 4 Audio Sources:
   - Music Source
   - SFX Source
   - Ambience Source
   - Voice Source

---

## ⚙️ CONFIGURATION

### **Network Transport Settings**

In NetworkManager:
- Address: `127.0.0.1` (localhost for testing)
- Port: `7777`
- Max Packet Size: `1300`

### **Player Controller Settings**

- Walk Speed: `5`
- Sprint Speed: `8`
- Jump Height: `2`
- Gravity: `-20`

### **Spawner Settings**

- Max Zombies: `50`
- Spawn Interval: `5` seconds
- Wave Mode: `Enabled`
- Zombies Per Wave: `10`

---

## 🎮 TESTING

### **Local Multiplayer Test:**

1. **Build Settings:**
   - Add GameScene to build
   - Platform: PC, Mac & Linux Standalone

2. **Build the game** (Ctrl+Shift+B)

3. **Start Host:**
   - Run built game
   - Click "Host Game"

4. **Start Client:**
   - Run in Unity Editor (Play button)
   - Click "Join Game"

5. **Test gameplay:**
   - Move with WASD
   - Sprint with Shift
   - Jump with Space
   - Crouch with Ctrl
   - Camera with Mouse
   - Shoot with Left Click

### **Debug Console:**

Press **F1** in Play mode to open debug menu:
- F2: God Mode
- F3: Spawn Zombie
- F4: Kill All Zombies

---

## 🐛 TROUBLESHOOTING

### **"NetworkObject not found" error:**
- Ensure all system GameObjects have NetworkObject component
- Check Network Prefabs list in NetworkManager

### **Players not spawning:**
- Check NetworkSpawnManager has spawn points assigned
- Verify Player prefab is in Network Prefabs list
- Ensure Player prefab has NetworkObject set to "Owner: Client"

### **Zombies not moving:**
- Bake NavMesh (Window > AI > Navigation > Bake)
- Check NavMeshAgent component on zombie prefabs
- Verify ground is marked as Navigation Static

### **UI not showing:**
- Check Canvas Render Mode is "Screen Space - Overlay"
- Verify HUDSystem has UI elements assigned
- Check Canvas Scaler settings

### **Netcode errors:**
- Update to latest Netcode for GameObjects package
- Ensure Unity version is 2022.3 LTS or newer
- Check all NetworkBehaviour scripts have [ServerRpc] or [ClientRpc] attributes

---

## 📦 OPTIONAL ENHANCEMENTS

### **Add Models & Animations:**
1. Import character models
2. Setup Animator Controller
3. Link to AnimationController.cs

### **Add Audio Clips:**
1. Import audio files
2. Add to AudioSystem's clip list
3. Configure audio types

### **Create Multiple Maps:**
1. Duplicate GameScene
2. Change layout
3. Add to MapSystem's available maps

---

## ✅ VERIFICATION CHECKLIST

Before final testing:

- [ ] NetworkManager configured
- [ ] All systems have NetworkObject
- [ ] Player prefab created and added to Network Prefabs
- [ ] Zombie prefabs created and assigned to SpawnerSystem
- [ ] Spawn points created and assigned
- [ ] NavMesh baked
- [ ] UI canvas setup
- [ ] Audio sources configured
- [ ] Can host/join game
- [ ] Player can move and look around
- [ ] Zombies spawn and chase players
- [ ] Combat works (damage/death)
- [ ] HUD displays correctly

---

## 🚀 YOU'RE READY!

Your game is now fully set up and playable!

Press Play and enjoy your AAA-quality zombie multiplayer game!
