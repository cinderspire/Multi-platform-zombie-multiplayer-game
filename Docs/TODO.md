# Master TODO List
## Dead Frontier: Outbreak

**Status:** Planning Complete, Ready for Implementation
**Last Updated:** November 2025
**Total Estimated Time:** 12 months (see ROADMAP.md for detailed breakdown)

---

## 📋 How to Use This TODO

### Priority Levels:
- **🔴 CRITICAL:** Must have for MVP
- **🟡 HIGH:** Important for MVP
- **🟢 MEDIUM:** Nice to have for MVP
- **🔵 LOW:** Post-MVP / Polish

### Status Icons:
- ⬜ Not Started
- 🟦 In Progress
- ✅ Complete
- ❌ Blocked
- 🚫 Cancelled/Deferred

### Time Estimates:
- Quick: <2 hours
- Short: 2-4 hours
- Medium: 4-8 hours (1 day)
- Long: 1-3 days
- XL: 1+ weeks

---

## 🚀 PHASE 1: MVP Development (Months 1-3)

### Week 1: Project Setup & Foundation

#### Day 1-2: Unity Project Setup 🔴 CRITICAL
- ⬜ Create new Unity 6.2 URP project (Quick)
- ⬜ Set up Git repository + Git LFS (Quick)
- ⬜ Add .gitignore and .gitattributes (Quick)
- ⬜ Initial commit and push to GitHub (Quick)
- ⬜ Install required packages via Package Manager: (Medium)
  - ⬜ Netcode for GameObjects 2.0+
  - ⬜ Unity Transport 2.3+
  - ⬜ Unity Lobby Service 1.2+
  - ⬜ Unity Relay 1.1+
  - ⬜ Unity Authentication 3.3+
  - ⬜ Input System 1.8+
  - ⬜ Cinemachine 3.1+
  - ⬜ TextMeshPro 4.0+
  - ⬜ ProBuilder 6.0+
- ⬜ Configure project settings: (Short)
  - ⬜ Physics (timestep 0.02, solver iterations)
  - ⬜ Quality (create Low/Medium/High presets)
  - ⬜ Player (input system, scripting backend IL2CPP)
  - ⬜ Graphics (URP asset, GPU Resident Drawer enabled)
- ⬜ Create folder structure (use PROJECT_STRUCTURE.md) (Medium)
- ⬜ Set up layers and tags (Quick)
- ⬜ Configure physics collision matrix (Quick)

#### Day 3-4: Core Architecture 🔴 CRITICAL
- ⬜ Create singleton base class `Singleton<T>.cs` (Quick)
- ⬜ Create `GameManager.cs` (singleton, DontDestroyOnLoad) (Short)
- ⬜ Create `NetworkManager.cs` (extends NetworkBehaviour) (Medium)
- ⬜ Create `AudioManager.cs` (sound management, pooling) (Medium)
- ⬜ Create `PoolManager.cs` (object pooling system) (Medium)
- ⬜ Create `UIManager.cs` (canvas management) (Short)
- ⬜ Create event system: (Medium)
  - ⬜ `GameEvent.cs` (ScriptableObject)
  - ⬜ `GameEventListener.cs` (MonoBehaviour)
  - ⬜ Test with simple event (e.g., OnPlayerSpawned)
- ⬜ Create state machine base: (Medium)
  - ⬜ `IState.cs` interface
  - ⬜ `StateMachine.cs` class
  - ⬜ Test with simple state (Idle, Active)
- ⬜ Create `Bootstrap.unity` scene with managers (Short)
- ⬜ Create `Constants.cs` for magic numbers (Quick)
- ⬜ Create `Extensions.cs` for helper methods (Quick)

#### Day 5-7: Player Controller (Local) 🔴 CRITICAL
- ⬜ Create `PlayerInputActions.inputactions` asset (Medium)
  - ⬜ Add action maps: Gameplay, UI
  - ⬜ Add actions: Move, Look, Fire, Reload, Jump, Sprint, Crouch
  - ⬜ Bind keyboard/mouse controls
  - ⬜ Bind gamepad controls (optional)
- ⬜ Create `PlayerInput.cs` (Input System integration) (Medium)
- ⬜ Create `PlayerMovement.cs` (WASD, sprint, crouch, jump) (Long)
  - ⬜ Basic WASD movement
  - ⬜ Sprint (Shift, drains stamina)
  - ⬜ Crouch (Ctrl, reduces speed)
  - ⬜ Jump (Space, uses CharacterController)
  - ⬜ Gravity and ground detection
- ⬜ Create `PlayerCamera.cs` (mouse look, recoil) (Medium)
  - ⬜ Mouse look (rotate camera with mouse)
  - ⬜ Sensitivity settings
  - ⬜ Recoil system (add/recover)
  - ⬜ Camera bob (optional, when walking)
- ⬜ Create `PlayerController.cs` (orchestrates all components) (Short)
- ⬜ Create `PlayerAnimator.cs` (trigger animations) (Medium)
  - ⬜ Download Mixamo animations (idle, walk, run, jump)
  - ⬜ Create Animator Controller
  - ⬜ Set up blend trees (idle/walk/run)
  - ⬜ Hook up to PlayerMovement
- ⬜ Create Player prefab (Short)
- ⬜ Create `TestPlayer.unity` scene for testing (Short)
- ⬜ Test all movement (walk, sprint, crouch, jump) (Medium)

---

### Week 2: Combat System

#### Day 1-3: Weapon System 🔴 CRITICAL
- ⬜ Create `WeaponData.cs` ScriptableObject (Short)
  - ⬜ Fields: damage, fireRate, range, magazineSize, reloadTime, recoilPattern, audio clips
- ⬜ Create 2 weapon data assets: (Quick)
  - ⬜ Pistol_Data (low damage, fast fire, low recoil)
  - ⬜ Rifle_Data (high damage, medium fire, high recoil)
- ⬜ Create `WeaponController.cs` (Medium)
  - ⬜ Reference to WeaponData
  - ⬜ Fire() method (raycast shooting)
  - ⬜ Reload() coroutine
  - ⬜ Ammo tracking
  - ⬜ Fire rate limiting
- ⬜ Create `WeaponRecoil.cs` (Short)
  - ⬜ Apply recoil to PlayerCamera
  - ⬜ Recoil recovery over time
- ⬜ Create weapon prefabs: (Medium)
  - ⬜ Download free pistol and rifle models
  - ⬜ Import to Unity
  - ⬜ Create prefabs with WeaponController
  - ⬜ Position in player's hand (child of camera)
- ⬜ Create `WeaponSwitcher.cs` (Short)
  - ⬜ Switch weapons with number keys (1, 2, 3)
  - ⬜ Enable/disable weapon GameObjects
- ⬜ Create `TestWeapons.unity` scene (Short)
- ⬜ Test shooting, reloading, switching (Medium)

#### Day 4-5: Damage System 🔴 CRITICAL
- ⬜ Create `IDamageable.cs` interface (Quick)
  - ⬜ Method: TakeDamage(int amount)
- ⬜ Create `PlayerHealth.cs` (implements IDamageable) (Medium)
  - ⬜ Health tracking (current, max)
  - ⬜ TakeDamage implementation
  - ⬜ Death logic (disable player, trigger event)
  - ⬜ Respawn logic (for testing)
- ⬜ Create `DamageFlash.cs` (UI feedback) (Short)
  - ⬜ Red screen flash on damage
  - ⬜ Fade out over time
- ⬜ Update `WeaponController.cs`: (Short)
  - ⬜ Raycast hit detection
  - ⬜ Call TakeDamage on IDamageable targets
  - ⬜ Show hit marker UI
- ⬜ Create test target dummy (cube with health) (Quick)
- ⬜ Test damage, death, respawn (Short)

#### Day 6-7: Polish 🟡 HIGH
- ⬜ Download free muzzle flash VFX (Quick)
- ⬜ Create `MuzzleFlash.prefab` (Quick)
- ⬜ Integrate muzzle flash on fire (Short)
- ⬜ Download/create bullet tracer VFX (Short)
- ⬜ Create impact effects: (Medium)
  - ⬜ Sparks (metal hit)
  - ⬜ Dust (concrete hit)
  - ⬜ Blood (flesh hit)
- ⬜ Download weapon SFX (Freesound): (Short)
  - ⬜ Pistol fire, rifle fire
  - ⬜ Reload sounds
  - ⬜ Empty click
  - ⬜ Weapon switch
- ⬜ Integrate audio into WeaponController (Short)
- ⬜ Set up object pooling for VFX (Medium)
  - ⬜ Pool muzzle flash
  - ⬜ Pool bullet tracers
  - ⬜ Pool impact effects
- ⬜ Test pooling performance (spawn 100 VFX rapidly) (Short)

---

### Week 3: Zombie AI (Basic)

#### Day 1-2: Zombie Foundation 🔴 CRITICAL
- ⬜ Download free zombie model (Sketchfab/Quaternius) (Quick)
- ⬜ Import zombie model to Unity (Quick)
- ⬜ Download Mixamo zombie animations: (Short)
  - ⬜ Idle, Walk, Run, Attack, Death
- ⬜ Create Zombie Animator Controller (Short)
  - ⬜ Set up states (Idle, Walk, Run, Attack, Death)
  - ⬜ Transitions with parameters (Speed, Attack, Die)
- ⬜ Create `ZombieConfig.cs` ScriptableObject (Short)
  - ⬜ Fields: health, moveSpeed, chaseSpeed, damage, attackRange, visionRange, hearingRange
- ⬜ Create `Walker_Config.asset` (baseline zombie) (Quick)
- ⬜ Create `ZombieHealth.cs` (implements IDamageable) (Short)
  - ⬜ Health tracking
  - ⬜ TakeDamage implementation
  - ⬜ Death (ragdoll, destroy after delay)
- ⬜ Create `ZombieMovement.cs` (Short)
  - ⬜ NavMeshAgent integration
  - ⬜ SetDestination method
  - ⬜ Speed control
- ⬜ Create `ZombieAnimator.cs` (Quick)
  - ⬜ Update Animator parameters based on state

#### Day 3-4: AI State Machine 🔴 CRITICAL
- ⬜ Create `ZombieStateMachine.cs` (uses StateMachine base) (Short)
- ⬜ Create `IdleState.cs` (Short)
  - ⬜ Enter: Stop movement, play idle animation
  - ⬜ Update: Random timer (2-5s), then wander
  - ⬜ Exit: None
- ⬜ Create `PatrolState.cs` (Medium)
  - ⬜ Enter: Choose random nearby point, navigate
  - ⬜ Update: Check if reached destination, return to idle
  - ⬜ Exit: None
- ⬜ Create `ChaseState.cs` (Medium)
  - ⬜ Enter: Set speed to chaseSpeed, play run animation
  - ⬜ Update: Navigate to player position, check attack range
  - ⬜ Exit: None
  - ⬜ Transition to Attack if in range
- ⬜ Create `AttackState.cs` (Medium)
  - ⬜ Enter: Stop movement, face player
  - ⬜ Update: Play attack animation, deal damage, cooldown
  - ⬜ Exit: Resume movement
  - ⬜ Transition to Chase if out of range
- ⬜ Create `DeathState.cs` (Short)
  - ⬜ Enter: Disable NavMeshAgent, play death animation
  - ⬜ Update: None
  - ⬜ Exit: Destroy GameObject after 5 seconds
- ⬜ Create `ZombieAI.cs` (orchestrates state machine) (Medium)
- ⬜ Test state transitions manually (toggle states in Inspector) (Medium)

#### Day 5-6: Sensor System 🔴 CRITICAL
- ⬜ Create `ZombieSensors.cs` (Long)
  - ⬜ Vision sensor: (Medium)
    - ⬜ OverlapSphere to find players in range
    - ⬜ Check angle (FOV cone)
    - ⬜ Raycast to check line of sight
  - ⬜ Hearing sensor: (Short)
    - ⬜ Listen for noise events (from AudioManager)
    - ⬜ Check distance
  - ⬜ Memory system: (Short)
    - ⬜ Store last known player position
    - ⬜ Timeout after 10 seconds
- ⬜ Update AudioManager: (Short)
  - ⬜ Register noise events (gunshots, footsteps)
  - ⬜ GetNoisesInRange() method for sensors
- ⬜ Integrate sensors with state machine: (Medium)
  - ⬜ IdleState → ChaseState when player detected
  - ⬜ ChaseState uses last known position if lost sight
- ⬜ Create `TestZombieAI.unity` scene (Short)
- ⬜ Test vision, hearing, memory (Medium)

#### Day 7: Testing & Tuning 🟡 HIGH
- ⬜ Create zombie prefab (Quick)
- ⬜ Spawn 10-20 zombies in test scene (Quick)
- ⬜ Playtest: (Long)
  - ⬜ Test detection (walk into view, make noise)
  - ⬜ Test chasing (run away, hide)
  - ⬜ Test attacking (let zombie catch you)
  - ⬜ Test death (shoot zombies)
- ⬜ Balance tuning: (Medium)
  - ⬜ Adjust zombie speed (should be catchable but threatening)
  - ⬜ Adjust damage (2-3 hits to kill player)
  - ⬜ Adjust HP (1 headshot or 3-5 body shots)
  - ⬜ Adjust vision range (not too far, not too close)
- ⬜ Fix bugs: (Variable)
  - ⬜ Zombies getting stuck on geometry
  - ⬜ Zombies attacking through walls
  - ⬜ Weird animations

---

### Week 4: Networking Foundation

#### Day 1-2: Netcode Setup 🔴 CRITICAL
- ⬜ Set up Unity Gaming Services: (Medium)
  - ⬜ Create Unity project in dashboard
  - ⬜ Link Unity Editor to project
  - ⬜ Enable Authentication, Lobby, Relay
- ⬜ Create `NetworkBootstrap.cs` (Medium)
  - ⬜ Initialize Unity services (Authentication)
  - ⬜ Sign in anonymously
  - ⬜ Load networking scene
- ⬜ Configure NetworkManager: (Short)
  - ⬜ Add NetworkManager to scene
  - ⬜ Set up transport (Unity Transport)
  - ⬜ Configure connection settings
- ⬜ Create simple host/client UI: (Short)
  - ⬜ "Host" button
  - ⬜ "Join" button
  - ⬜ IP input field (for local testing)
- ⬜ Test local connection: (Short)
  - ⬜ Host on one instance
  - ⬜ Join from another (ParrelSync or build)
  - ⬜ Verify connection in Network Manager

#### Day 3-4: Player Networking 🔴 CRITICAL
- ⬜ Update `PlayerController.cs`: (Medium)
  - ⬜ Extend NetworkBehaviour
  - ⬜ Add NetworkObject component
  - ⬜ Override OnNetworkSpawn
  - ⬜ Disable input/camera if not owner
- ⬜ Add `NetworkTransform` component to Player (Quick)
  - ⬜ Configure interpolation
  - ⬜ Test position sync
- ⬜ Create `PlayerNetworking.cs` (Medium)
  - ⬜ NetworkVariable for health
  - ⬜ NetworkVariable for ammo
  - ⬜ Sync player name (future)
- ⬜ Implement client-side prediction: (Long)
  - ⬜ Store input commands locally
  - ⬜ Send commands to server via ServerRpc
  - ⬜ Server validates and applies
  - ⬜ Server sends authoritative position via ClientRpc
  - ⬜ Client reconciles if mismatch
- ⬜ Create player spawner: (Short)
  - ⬜ Spawn players at random spawn points
  - ⬜ Assign ownership correctly
- ⬜ Test with 2 clients: (Medium)
  - ⬜ Both players can move
  - ⬜ Positions sync smoothly
  - ⬜ No jitter or teleporting

#### Day 5-6: Weapon Networking 🔴 CRITICAL
- ⬜ Create `WeaponNetworking.cs` (Long)
  - ⬜ [ServerRpc] FireWeaponServerRpc (client requests fire)
  - ⬜ Server validates (has ammo, fire rate OK)
  - ⬜ Server performs raycast (authoritative)
  - ⬜ [ClientRpc] FireWeaponClientRpc (visual/audio feedback)
  - ⬜ [ClientRpc] ShowImpactClientRpc (show hit VFX)
- ⬜ Update `WeaponController.cs`: (Medium)
  - ⬜ Call FireWeaponServerRpc instead of local fire
  - ⬜ Client shows immediate muzzle flash (prediction)
  - ⬜ Server result confirms hit
- ⬜ Sync damage: (Short)
  - ⬜ Server calls TakeDamage on target
  - ⬜ Target's NetworkVariable health updates
  - ⬜ All clients see health change
- ⬜ Test with 2 clients: (Medium)
  - ⬜ Player 1 shoots Player 2
  - ⬜ Player 2 takes damage
  - ⬜ Both see correct health
  - ⬜ VFX appears on both clients

#### Day 7: Zombie Networking 🔴 CRITICAL
- ⬜ Update `ZombieAI.cs`: (Medium)
  - ⬜ Add NetworkObject component
  - ⬜ Only run AI on server (IsServer check)
  - ⬜ NetworkVariable for current state
  - ⬜ NetworkVariable for target position
- ⬜ Sync animations: (Short)
  - ⬜ Add NetworkAnimator component
  - ⬜ Configure synced parameters
- ⬜ Create `ZombieSpawner.cs` (server-only) (Medium)
  - ⬜ Spawn zombies on server
  - ⬜ NetworkObject.Spawn() to sync to clients
- ⬜ Test with 2 clients + zombies: (Medium)
  - ⬜ Zombies spawn on both clients
  - ⬜ Zombies chase nearest player
  - ⬜ Zombies take damage from either client
  - ⬜ Zombie death syncs
- ⬜ Create `TestNetworking.unity` scene (Short)
- ⬜ Full multiplayer test (Medium)

---

### Week 5-12: [Continue with remaining weeks from ROADMAP.md]

*[For brevity, I'll summarize remaining phases. Full tasks in ROADMAP.md]*

---

## ⚡ Quick Win Tasks (Do Anytime)

### 🟢 MEDIUM Priority
- ⬜ Add player footstep sounds (Quick)
- ⬜ Add crosshair UI (Quick)
- ⬜ Add FPS counter (dev only) (Quick)
- ⬜ Add console log viewer (Quick)
- ⬜ Create credits file for free assets (Quick)
- ⬜ Write basic README.md for GitHub (Short)

### 🔵 LOW Priority
- ⬜ Add settings menu (graphics, audio, controls) (Long)
- ⬜ Add pause menu (Medium)
- ⬜ Add main menu background music (Quick)
- ⬜ Add button hover sounds (Quick)
- ⬜ Create logo/icon (or use placeholder) (Medium)

---

## 🐛 Known Issues / Tech Debt

### Critical Bugs (Fix ASAP)
- ⬜ [None yet - will populate during development]

### High Priority Bugs
- ⬜ [TBD]

### Medium Priority
- ⬜ [TBD]

### Low Priority / Won't Fix
- ⬜ [TBD]

---

## 📚 Learning Tasks (As Needed)

### Netcode for GameObjects
- ⬜ Watch Code Monkey Netcode tutorial (2 hours)
- ⬜ Study Boss Room sample project (4 hours)
- ⬜ Read official Netcode docs (2 hours)

### AI & NavMesh
- ⬜ Watch Sebastian Lague A* pathfinding (1 hour)
- ⬜ Watch Brackeys State Machine tutorial (30 min)
- ⬜ Study NavMesh Components (1 hour)

### Optimization
- ⬜ Read Unity Mobile Optimization guide (2 hours)
- ⬜ Watch Code Monkey Object Pooling tutorial (30 min)
- ⬜ Profile game with Unity Profiler (ongoing)

### Unity 6 Specific
- ⬜ Read Unity 6 release notes (1 hour)
- ⬜ Study GPU Resident Drawer docs (30 min)
- ⬜ Explore Entities Graphics (optional, 4 hours)

---

## 🎨 Art Asset Tasks

### 3D Models to Download
- ⬜ 3-5 zombie models (Sketchfab, Quaternius) (30 min)
- ⬜ 5-10 weapon models (Free3D, TurboSquid) (1 hour)
- ⬜ Building pack (Kenney, Unity Asset Store) (30 min)
- ⬜ Props (cars, debris, furniture) (1 hour)
- ⬜ Vegetation (trees, grass, rocks) (30 min)

### Animations (Mixamo)
- ⬜ Player: Idle, Walk, Run, Sprint, Jump, Crouch, Shoot, Reload, Death (5-10 variations) (1 hour)
- ⬜ Zombie: Idle, Walk, Run, Attack, Death (5-10 variations) (1 hour)

### Audio
- ⬜ Weapon SFX (Freesound) (30 min)
- ⬜ Zombie SFX (Freesound) (30 min)
- ⬜ Player SFX (footsteps, hurt, death) (30 min)
- ⬜ Environment SFX (wind, sirens, helicopters) (30 min)
- ⬜ UI SFX (clicks, notifications) (15 min)
- ⬜ Music tracks (Incompetech) (30 min)

### Textures
- ⬜ Concrete, metal, brick, asphalt (Poly Haven) (30 min)
- ⬜ Skyboxes (day, night) (Unity Asset Store) (15 min)

---

## 🔄 Recurring Tasks

### Daily
- ⬜ Git commit at end of day (describe what was done)
- ⬜ Update TODO.md status
- ⬜ Test new features in play mode
- ⬜ Profile performance if adding heavy features

### Weekly
- ⬜ Sprint review (What worked? What didn't?)
- ⬜ Sprint planning (Pick tasks for next week)
- ⬜ Push to GitHub
- ⬜ Create build and test on target platforms
- ⬜ Update ROADMAP.md if timeline changes

### Monthly
- ⬜ Milestone review (On track for phase goals?)
- ⬜ Playtest with others (if possible)
- ⬜ Review KPIs (performance metrics)
- ⬜ Update documentation (GDD, TDD if major changes)

---

## 🎯 Milestone Checklists

### Milestone 1: MVP Core Loop (Month 3)
- ⬜ Player can move, shoot, take damage, die
- ⬜ Zombies can detect, chase, attack player
- ⬜ Multiplayer works (8 players, stable)
- ⬜ Inventory system functional
- ⬜ Extraction mechanics working
- ⬜ One complete map playable
- ⬜ 45+ FPS on target devices
- ⬜ < 1% crash rate in testing
- **Decision:** Proceed to content expansion or iterate?

### Milestone 2: Content Complete (Month 6)
- ⬜ 4+ zombie types
- ⬜ 5+ weapons
- ⬜ 2+ maps
- ⬜ Battle Pass system
- ⬜ Cosmetics store
- ⬜ IAP integration
- ⬜ Mobile builds stable
- **Decision:** Ready for beta or need more polish?

### Milestone 3: Beta Ready (Month 9)
- ⬜ All critical bugs fixed
- ⬜ Performance optimized
- ⬜ Balance pass complete
- ⬜ UI/UX polished
- ⬜ Marketing assets ready (trailer, screenshots)
- ⬜ Store pages submitted
- **Decision:** Soft launch or full launch?

### Milestone 4: Launch (Month 11)
- ⬜ Global release on all platforms
- ⬜ Marketing campaign executed
- ⬜ Community Discord active
- ⬜ Analytics tracking
- ⬜ Live ops team ready (even if solo)
- **Decision:** Season 1 content or more bug fixes?

---

## 📊 Progress Tracking

### Overall Progress: 0% Complete

#### Phase 1 (MVP): 0/100 tasks ⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜
#### Phase 2 (Content): 0/50 tasks ⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜
#### Phase 3 (Polish): 0/30 tasks ⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜
#### Phase 4 (Launch): 0/20 tasks ⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜

---

## 🎯 Current Sprint (Update Weekly)

**Sprint #:** 0 (Pre-Development)
**Sprint Goal:** Complete planning, start Week 1 tasks
**Sprint Duration:** Nov 2025

### This Sprint Tasks:
- ✅ Research 2025 zombie multiplayer trends
- ✅ Create comprehensive GDD
- ✅ Create comprehensive TDD
- ✅ Create detailed ROADMAP
- ✅ Create RESOURCES list
- ✅ Create PROJECT_STRUCTURE guide
- ✅ Create TODO list (this file!)
- ⬜ Set up Unity project (Week 1, Day 1)

### Blockers:
- [None]

### Notes:
- Planning phase complete! 🎉
- Ready to start development Week 1, Day 1
- All documentation in place

---

## 💡 Ideas for Future Phases (Post-MVP)

### Gameplay Ideas
- ⬜ New game mode: Battle Royale (zombie + players shrinking circle)
- ⬜ New game mode: Horde Defense (co-op wave survival)
- ⬜ New game mode: Ranked Extraction (leaderboards, skill-based matchmaking)
- ⬜ Boss zombies (giant, special mechanics)
- ⬜ Vehicle system (drive cars to extract faster)
- ⬜ Base building (build defenses during match)
- ⬜ Crafting system (combine items for better gear)
- ⬜ Weather system (rain, fog affects visibility)

### Content Ideas
- ⬜ Map 4: Cruise Ship
- ⬜ Map 5: Prison
- ⬜ Map 6: Airport
- ⬜ New weapons: Grenade launcher, flamethrower
- ⬜ New zombie: Spitter (ranged acid attack)
- ⬜ New zombie: Stalker (invisible until close)

### Technical Ideas
- ⬜ Upgrade to ECS for 10K+ zombies (Horde Mode)
- ⬜ Add DLSS/FSR for PC (better performance)
- ⬜ Add ray tracing (high-end PC only)
- ⬜ Console ports (PS5, Xbox Series)
- ⬜ VR mode (experimental)

### Monetization Ideas
- ⬜ Limited-time skins (seasonal)
- ⬜ Battle Pass bundles (discount multiple seasons)
- ⬜ Streamer mode (custom cosmetics for content creators)
- ⬜ Esports skins (team-branded)

---

## 🏆 Achievements (Personal Dev Milestones)

### Week 1
- ⬜ First commit
- ⬜ Player moves for first time
- ⬜ First shot fired

### Month 1
- ⬜ First zombie kill
- ⬜ First death
- ⬜ First multiplayer connection

### Month 3
- ⬜ MVP playable
- ⬜ First full match completed
- ⬜ First playtester feedback

### Month 6
- ⬜ Content complete
- ⬜ First purchase (IAP working!)

### Month 12
- ⬜ Game launched
- ⬜ 1000 players reached
- ⬜ First positive review

---

## 📞 Next Steps (Right Now!)

**If you're reading this and ready to start:**

1. ✅ Read all documentation (GDD, TDD, ROADMAP, RESOURCES)
2. ⬜ Set up Unity project (Week 1, Day 1 tasks)
3. ⬜ Download essential free assets (RESOURCES.md)
4. ⬜ Start coding PlayerController!

**Remember:**
- ✅ Start small, iterate fast
- ✅ Commit often
- ✅ Test continuously
- ✅ Ask for help when stuck (Unity Forums, Discord)
- ✅ Enjoy the process! 🚀

---

**Let's build this game!** 🧟‍♂️🔫

*Last Updated: November 2025*
*Total Tasks: ~500+ (across all phases)*
*Estimated Completion: 12 months*

---

## 📝 Task Update Log

### [Date] - [Your Name]
- ✅ Task completed
- 🟦 Task in progress
- ⬜ Task not started
- Notes: [Any notes]

---

**End of TODO.md**
