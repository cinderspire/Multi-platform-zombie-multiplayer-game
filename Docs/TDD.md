# Technical Design Document (TDD)
## Dead Frontier: Outbreak

**Version:** 1.0
**Last Updated:** November 2025
**Document Owner:** Technical Lead

---

## Table of Contents
1. [Technical Overview](#technical-overview)
2. [Architecture Patterns](#architecture-patterns)
3. [Core Systems](#core-systems)
4. [Networking Architecture](#networking-architecture)
5. [Performance Optimization](#performance-optimization)
6. [Data Structures](#data-structures)
7. [Code Organization](#code-organization)
8. [Third-Party Integrations](#third-party-integrations)
9. [Build Pipeline](#build-pipeline)
10. [Testing Strategy](#testing-strategy)

---

## 1. Technical Overview

### 1.1 Technology Stack

```
Engine: Unity 2022.3.x LTS
  ├── Render Pipeline: Universal Render Pipeline (URP) 14.x
  ├── Scripting Backend: IL2CPP
  ├── API Compatibility: .NET Standard 2.1
  └── C# Version: 9.0

Networking:
  ├── Netcode for GameObjects 1.7.x
  ├── Unity Transport 2.0.x
  └── Unity Gaming Services (Lobby, Matchmaking, Relay)

Platform SDKs:
  ├── Steam SDK (PC)
  ├── Google Play Games Services (Android)
  ├── Apple Game Center (iOS)
  └── WebGL (Browser)

Analytics:
  ├── Unity Analytics
  ├── GameAnalytics (backup)
  └── Custom event tracking

Version Control:
  ├── Git + Git LFS
  ├── GitHub (repository)
  └── GitHub Actions (CI/CD)
```

### 1.2 Target Performance Specifications

| Platform | Min FPS | Target FPS | Resolution | Max Players | Build Size |
|----------|---------|------------|------------|-------------|------------|
| PC (Low) | 45 | 60 | 1080p | 8 | 800 MB |
| PC (High) | 60 | 120+ | 4K | 8 | 800 MB |
| Mobile (Low) | 30 | 45 | 720p | 8 | 400 MB |
| Mobile (High) | 60 | 60 | 1080p | 8 | 500 MB |
| WebGL | 30 | 45 | 1080p | 8 | 100 MB initial |

**Minimum Specifications:**
- **PC:** GTX 960 / RX 560, 8GB RAM, i5-4460
- **Mobile:** Snapdragon 665 / A12 Bionic, 3GB RAM
- **WebGL:** Chrome 90+, 4GB RAM

---

## 2. Architecture Patterns

### 2.1 Overall Architecture: Hybrid ECS + OOP

**Why Hybrid?**
- Pure ECS (DOTS) not mature enough for Netcode integration (2025)
- OOP easier for rapid prototyping
- MonoBehaviour for gameplay, ScriptableObjects for data

**Pattern:**
```
GameObject (MonoBehaviour)
    ↓ (References)
ScriptableObject (Data)
    ↓ (Events)
Event System (Decoupling)
    ↓ (Listeners)
Managers (Singleton-lite)
```

### 2.2 Design Patterns Used

#### Singleton Pattern (for Managers)
```csharp
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
```

**Used For:**
- GameManager
- NetworkManager
- AudioManager
- UIManager
- PoolManager

#### Object Pool Pattern
```csharp
public class ObjectPool<T> where T : MonoBehaviour
{
    private readonly T prefab;
    private readonly Queue<T> pool = new Queue<T>();
    private readonly Transform parent;

    public T Get()
    {
        if (pool.Count > 0)
        {
            var obj = pool.Dequeue();
            obj.gameObject.SetActive(true);
            return obj;
        }
        return Object.Instantiate(prefab, parent);
    }

    public void Return(T obj)
    {
        obj.gameObject.SetActive(false);
        pool.Enqueue(obj);
    }
}
```

**Used For:**
- Zombies
- Bullets/Projectiles
- VFX particles
- Audio sources
- UI elements

#### State Machine Pattern
```csharp
public interface IState
{
    void Enter();
    void Update();
    void Exit();
}

public class StateMachine
{
    private IState currentState;

    public void ChangeState(IState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState?.Enter();
    }

    public void Update()
    {
        currentState?.Update();
    }
}
```

**Used For:**
- Zombie AI (Idle, Chase, Attack, Flee, Death)
- Player states (Idle, Moving, Sprinting, Crouching)
- Game states (Lobby, Match, Extraction, EndGame)

#### Command Pattern
```csharp
public interface ICommand
{
    void Execute();
    void Undo();
}

public class MoveCommand : ICommand
{
    private Transform transform;
    private Vector3 direction;

    public void Execute() => transform.Translate(direction);
    public void Undo() => transform.Translate(-direction);
}
```

**Used For:**
- Input handling
- Network commands
- Replay system (future)

#### Observer Pattern (Event System)
```csharp
public class GameEvent : ScriptableObject
{
    private readonly List<GameEventListener> listeners = new List<GameEventListener>();

    public void Raise()
    {
        for (int i = listeners.Count - 1; i >= 0; i--)
            listeners[i].OnEventRaised();
    }

    public void RegisterListener(GameEventListener listener) => listeners.Add(listener);
    public void UnregisterListener(GameEventListener listener) => listeners.Remove(listener);
}
```

**Used For:**
- Player death
- Zombie kill
- Extraction started
- Match end
- UI updates

#### Strategy Pattern
```csharp
public interface IWeaponStrategy
{
    void Fire(Vector3 origin, Vector3 direction);
}

public class HitscanStrategy : IWeaponStrategy
{
    public void Fire(Vector3 origin, Vector3 direction)
    {
        if (Physics.Raycast(origin, direction, out RaycastHit hit))
        {
            // Apply damage
        }
    }
}

public class ProjectileStrategy : IWeaponStrategy
{
    public void Fire(Vector3 origin, Vector3 direction)
    {
        // Spawn projectile
    }
}
```

**Used For:**
- Weapon firing modes
- Zombie AI strategies (aggressive, defensive)
- Pathfinding strategies

---

## 3. Core Systems

### 3.1 Player Controller System

**Architecture:**
```
PlayerController (MonoBehaviour)
├── PlayerInput (handles input)
├── PlayerMovement (movement logic)
├── PlayerHealth (health/damage)
├── PlayerInventory (items)
├── PlayerWeapon (shooting)
└── PlayerAnimator (animation)
```

**PlayerController.cs:**
```csharp
public class PlayerController : NetworkBehaviour
{
    // Components
    private PlayerInput input;
    private PlayerMovement movement;
    private PlayerHealth health;
    private PlayerInventory inventory;
    private PlayerWeapon weapon;

    // Network Variables
    private NetworkVariable<Vector3> networkPosition = new NetworkVariable<Vector3>();
    private NetworkVariable<float> networkHealth = new NetworkVariable<float>(100f);

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // Enable camera, input
        }
        else
        {
            // Disable camera, interpolate position
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        input.HandleInput();
        movement.Move(input.Movement);

        if (input.FirePressed)
            weapon.Fire();
    }
}
```

**Performance Optimization:**
- Input buffering to prevent missed inputs
- Client-side prediction for movement (feel responsive)
- Server reconciliation (prevent cheating)

### 3.2 Zombie AI System

**Architecture:**
```
ZombieAI (MonoBehaviour)
├── ZombieSensors (vision, hearing)
├── ZombieStateMachine (behavior)
├── ZombieMovement (NavMeshAgent)
├── ZombieHealth (HP, damage)
├── ZombieAnimator (animations)
└── ZombieNetworking (sync state)
```

**ZombieAI.cs:**
```csharp
public class ZombieAI : NetworkBehaviour
{
    // Configuration
    [SerializeField] private ZombieConfig config; // ScriptableObject

    // Components
    private NavMeshAgent agent;
    private ZombieSensors sensors;
    private ZombieStateMachine stateMachine;

    // Network Variables
    private NetworkVariable<ZombieState> networkState = new NetworkVariable<ZombieState>();
    private NetworkVariable<Vector3> targetPosition = new NetworkVariable<Vector3>();

    private void Start()
    {
        // Initialize state machine
        stateMachine = new ZombieStateMachine(this);
        stateMachine.ChangeState(new IdleState());

        // Setup sensors
        sensors.OnPlayerDetected += HandlePlayerDetected;
        sensors.OnNoiseHeard += HandleNoiseHeard;
    }

    private void Update()
    {
        if (!IsServer) return; // AI only runs on server

        sensors.Sense();
        stateMachine.Update();
    }

    private void HandlePlayerDetected(Transform player)
    {
        stateMachine.ChangeState(new ChaseState(player));
    }
}
```

**ZombieSensors.cs:**
```csharp
public class ZombieSensors : MonoBehaviour
{
    [SerializeField] private float visionRange = 30f;
    [SerializeField] private float visionAngle = 180f;
    [SerializeField] private float hearingRange = 50f;
    [SerializeField] private LayerMask playerLayer;

    public event Action<Transform> OnPlayerDetected;
    public event Action<Vector3> OnNoiseHeard;

    // Memory system
    private Vector3 lastKnownPosition;
    private float lastSeenTime;

    public void Sense()
    {
        // Vision check (expensive, run less frequently)
        if (Time.frameCount % 10 == 0) // Every 10 frames
        {
            CheckVision();
        }

        // Hearing check (cheaper, run every frame)
        CheckHearing();
    }

    private void CheckVision()
    {
        Collider[] colliders = Physics.OverlapSphere(
            transform.position,
            visionRange,
            playerLayer
        );

        foreach (var col in colliders)
        {
            Vector3 dirToTarget = (col.transform.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, dirToTarget);

            if (angle < visionAngle / 2)
            {
                // Raycast to check line of sight
                if (Physics.Raycast(
                    transform.position + Vector3.up,
                    dirToTarget,
                    out RaycastHit hit,
                    visionRange
                ))
                {
                    if (hit.collider.CompareTag("Player"))
                    {
                        lastKnownPosition = hit.transform.position;
                        lastSeenTime = Time.time;
                        OnPlayerDetected?.Invoke(hit.transform);
                        return;
                    }
                }
            }
        }
    }

    private void CheckHearing()
    {
        // Listen for noise events (registered by AudioManager)
        var noises = AudioManager.Instance.GetNoisesInRange(transform.position, hearingRange);

        if (noises.Count > 0)
        {
            // Investigate closest noise
            var closestNoise = noises.OrderBy(n =>
                Vector3.Distance(transform.position, n.position)
            ).First();

            OnNoiseHeard?.Invoke(closestNoise.position);
        }
    }

    public Vector3 GetLastKnownPosition()
    {
        // Return last known position if recent (within 10 seconds)
        if (Time.time - lastSeenTime < 10f)
            return lastKnownPosition;

        return Vector3.zero;
    }
}
```

**State Machine States:**

```csharp
// Idle State
public class IdleState : IState
{
    private ZombieAI zombie;
    private float idleTimer;

    public void Enter()
    {
        zombie.Agent.isStopped = true;
        zombie.Animator.SetTrigger("Idle");
        idleTimer = Random.Range(2f, 5f);
    }

    public void Update()
    {
        idleTimer -= Time.deltaTime;

        if (idleTimer <= 0)
        {
            // Wander to random point
            zombie.StateMachine.ChangeState(new PatrolState());
        }
    }

    public void Exit() { }
}

// Chase State
public class ChaseState : IState
{
    private ZombieAI zombie;
    private Transform target;
    private float lostTargetTimer;

    public ChaseState(Transform target)
    {
        this.target = target;
    }

    public void Enter()
    {
        zombie.Agent.isStopped = false;
        zombie.Agent.speed = zombie.Config.chaseSpeed;
        zombie.Animator.SetTrigger("Run");

        // Call nearby zombies
        CallHorde();
    }

    public void Update()
    {
        if (target == null || !target.gameObject.activeInHierarchy)
        {
            zombie.StateMachine.ChangeState(new IdleState());
            return;
        }

        float distToTarget = Vector3.Distance(zombie.transform.position, target.position);

        // Attack range
        if (distToTarget < zombie.Config.attackRange)
        {
            zombie.StateMachine.ChangeState(new AttackState(target));
            return;
        }

        // Update destination
        zombie.Agent.SetDestination(target.position);

        // Lost sight
        if (!zombie.Sensors.CanSeeTarget(target))
        {
            lostTargetTimer += Time.deltaTime;

            if (lostTargetTimer > 3f)
            {
                // Go to last known position
                var lastPos = zombie.Sensors.GetLastKnownPosition();
                if (lastPos != Vector3.zero)
                {
                    zombie.StateMachine.ChangeState(new InvestigateState(lastPos));
                }
                else
                {
                    zombie.StateMachine.ChangeState(new IdleState());
                }
            }
        }
        else
        {
            lostTargetTimer = 0f;
        }
    }

    public void Exit() { }

    private void CallHorde()
    {
        // Find nearby zombies and alert them
        Collider[] zombies = Physics.OverlapSphere(
            zombie.transform.position,
            20f,
            LayerMask.GetMask("Zombie")
        );

        foreach (var z in zombies)
        {
            var ai = z.GetComponent<ZombieAI>();
            if (ai != null && ai != zombie)
            {
                ai.AlertToTarget(target);
            }
        }
    }
}

// Attack State
public class AttackState : IState
{
    private ZombieAI zombie;
    private Transform target;
    private float attackCooldown;

    public AttackState(Transform target)
    {
        this.target = target;
    }

    public void Enter()
    {
        zombie.Agent.isStopped = true;
        attackCooldown = 0f;
    }

    public void Update()
    {
        if (target == null)
        {
            zombie.StateMachine.ChangeState(new IdleState());
            return;
        }

        float distToTarget = Vector3.Distance(zombie.transform.position, target.position);

        // Out of range, chase again
        if (distToTarget > zombie.Config.attackRange * 1.2f)
        {
            zombie.StateMachine.ChangeState(new ChaseState(target));
            return;
        }

        // Face target
        Vector3 direction = (target.position - zombie.transform.position).normalized;
        zombie.transform.rotation = Quaternion.Slerp(
            zombie.transform.rotation,
            Quaternion.LookRotation(direction),
            Time.deltaTime * 5f
        );

        // Attack
        attackCooldown -= Time.deltaTime;
        if (attackCooldown <= 0f)
        {
            zombie.Animator.SetTrigger("Attack");
            zombie.DealDamage(target);
            attackCooldown = zombie.Config.attackCooldown;
        }
    }

    public void Exit()
    {
        zombie.Agent.isStopped = false;
    }
}
```

**Performance Optimization:**
- **Spatial Partitioning:** Divide world into grid cells, only check zombies in nearby cells
- **Update Throttling:** Stagger AI updates (zombie 1 updates frame 0, zombie 2 frame 1, etc.)
- **LOD System:** Distant zombies update less frequently
- **Pooling:** Reuse zombie instances instead of Instantiate/Destroy

```csharp
public class ZombieManager : MonoBehaviour
{
    private Dictionary<Vector2Int, List<ZombieAI>> spatialGrid = new Dictionary<Vector2Int, List<ZombieAI>>();
    private const float CELL_SIZE = 10f;

    public void RegisterZombie(ZombieAI zombie)
    {
        Vector2Int cell = GetCell(zombie.transform.position);
        if (!spatialGrid.ContainsKey(cell))
            spatialGrid[cell] = new List<ZombieAI>();

        spatialGrid[cell].Add(zombie);
    }

    public List<ZombieAI> GetZombiesInRange(Vector3 position, float range)
    {
        Vector2Int centerCell = GetCell(position);
        int cellRange = Mathf.CeilToInt(range / CELL_SIZE);

        List<ZombieAI> nearbyZombies = new List<ZombieAI>();

        for (int x = -cellRange; x <= cellRange; x++)
        {
            for (int z = -cellRange; z <= cellRange; z++)
            {
                Vector2Int cell = centerCell + new Vector2Int(x, z);
                if (spatialGrid.TryGetValue(cell, out List<ZombieAI> zombies))
                {
                    nearbyZombies.AddRange(zombies);
                }
            }
        }

        return nearbyZombies;
    }

    private Vector2Int GetCell(Vector3 position)
    {
        return new Vector2Int(
            Mathf.FloorToInt(position.x / CELL_SIZE),
            Mathf.FloorToInt(position.z / CELL_SIZE)
        );
    }
}
```

### 3.3 Weapon System

**Architecture:**
```
WeaponManager
├── WeaponData (ScriptableObject)
├── WeaponFire (shooting logic)
├── WeaponRecoil (recoil pattern)
├── WeaponAmmo (ammo management)
└── WeaponNetworking (sync shots)
```

**WeaponData.cs (ScriptableObject):**
```csharp
[CreateAssetMenu(fileName = "NewWeapon", menuName = "Game/Weapon")]
public class WeaponData : ScriptableObject
{
    [Header("Basic Info")]
    public string weaponName;
    public WeaponType weaponType;
    public GameObject worldModel;
    public Sprite icon;

    [Header("Combat Stats")]
    public int damage = 25;
    public float fireRate = 0.1f; // Time between shots
    public float range = 100f;
    public int magazineSize = 30;
    public float reloadTime = 2f;

    [Header("Recoil")]
    public Vector2 recoilPattern = new Vector2(0.5f, 2f); // (horizontal, vertical)
    public float recoilRecoverySpeed = 5f;

    [Header("Audio")]
    public AudioClip fireSound;
    public AudioClip reloadSound;
    public AudioClip emptySound;
    public float noiseLevel = 50f; // Attracts zombies within this range

    [Header("VFX")]
    public GameObject muzzleFlashPrefab;
    public GameObject impactEffectPrefab;
    public GameObject bulletTracerPrefab;
}

public enum WeaponType
{
    Pistol,
    SMG,
    Rifle,
    Shotgun,
    Sniper
}
```

**WeaponController.cs:**
```csharp
public class WeaponController : NetworkBehaviour
{
    [SerializeField] private WeaponData currentWeapon;
    [SerializeField] private Transform firePoint;
    [SerializeField] private Camera playerCamera;

    // State
    private int currentAmmo;
    private float nextFireTime;
    private bool isReloading;

    // Network
    private NetworkVariable<int> networkAmmo = new NetworkVariable<int>();

    private void Start()
    {
        currentAmmo = currentWeapon.magazineSize;
        networkAmmo.Value = currentAmmo;
    }

    public void Fire()
    {
        if (!IsOwner) return;
        if (Time.time < nextFireTime) return;
        if (isReloading) return;
        if (currentAmmo <= 0)
        {
            // Play empty sound
            AudioManager.Instance.Play(currentWeapon.emptySound);
            return;
        }

        // Client-side immediate feedback
        FireWeaponClientRpc();

        // Server authoritative hit detection
        FireWeaponServerRpc(playerCamera.transform.position, playerCamera.transform.forward);

        currentAmmo--;
        networkAmmo.Value = currentAmmo;
        nextFireTime = Time.time + currentWeapon.fireRate;
    }

    [ServerRpc]
    private void FireWeaponServerRpc(Vector3 origin, Vector3 direction)
    {
        // Server validates and performs raycast
        if (Physics.Raycast(origin, direction, out RaycastHit hit, currentWeapon.range))
        {
            // Check what was hit
            if (hit.collider.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage(currentWeapon.damage);
            }

            // Notify clients of impact
            ShowImpactClientRpc(hit.point, hit.normal);
        }

        // Register noise for zombie AI
        AudioManager.Instance.RegisterNoise(origin, currentWeapon.noiseLevel);
    }

    [ClientRpc]
    private void FireWeaponClientRpc()
    {
        // Visual effects
        if (currentWeapon.muzzleFlashPrefab != null)
        {
            var flash = PoolManager.Instance.Get(currentWeapon.muzzleFlashPrefab, firePoint.position, firePoint.rotation);
            PoolManager.Instance.Return(flash, 0.1f);
        }

        // Audio
        AudioManager.Instance.Play(currentWeapon.fireSound, firePoint.position);

        // Recoil
        ApplyRecoil();
    }

    [ClientRpc]
    private void ShowImpactClientRpc(Vector3 position, Vector3 normal)
    {
        if (currentWeapon.impactEffectPrefab != null)
        {
            var impact = PoolManager.Instance.Get(
                currentWeapon.impactEffectPrefab,
                position,
                Quaternion.LookRotation(normal)
            );
            PoolManager.Instance.Return(impact, 1f);
        }
    }

    private void ApplyRecoil()
    {
        // Apply camera recoil (client-side only)
        float recoilX = Random.Range(-currentWeapon.recoilPattern.x, currentWeapon.recoilPattern.x);
        float recoilY = currentWeapon.recoilPattern.y;

        // Apply to camera controller
        GetComponent<PlayerCamera>().AddRecoil(new Vector2(recoilX, recoilY));
    }

    public void Reload()
    {
        if (isReloading) return;
        if (currentAmmo == currentWeapon.magazineSize) return;

        StartCoroutine(ReloadCoroutine());
    }

    private IEnumerator ReloadCoroutine()
    {
        isReloading = true;

        // Play animation and sound
        AudioManager.Instance.Play(currentWeapon.reloadSound);

        yield return new WaitForSeconds(currentWeapon.reloadTime);

        currentAmmo = currentWeapon.magazineSize;
        networkAmmo.Value = currentAmmo;
        isReloading = false;
    }
}
```

### 3.4 Inventory System

**InventoryData.cs:**
```csharp
[System.Serializable]
public class InventorySlot
{
    public ItemData item;
    public int quantity;

    public bool IsEmpty => item == null;
}

public class InventorySystem : NetworkBehaviour
{
    [SerializeField] private int maxSlots = 12;
    private List<InventorySlot> slots = new List<InventorySlot>();

    // Network
    private NetworkList<InventorySlot> networkSlots;

    private void Awake()
    {
        networkSlots = new NetworkList<InventorySlot>();

        for (int i = 0; i < maxSlots; i++)
        {
            slots.Add(new InventorySlot());
        }
    }

    public bool AddItem(ItemData item, int quantity = 1)
    {
        // Try to stack with existing
        for (int i = 0; i < slots.Count; i++)
        {
            if (!slots[i].IsEmpty && slots[i].item == item && item.isStackable)
            {
                slots[i].quantity += quantity;
                SyncInventoryServerRpc();
                return true;
            }
        }

        // Find empty slot
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].IsEmpty)
            {
                slots[i].item = item;
                slots[i].quantity = quantity;
                SyncInventoryServerRpc();
                return true;
            }
        }

        // Inventory full
        return false;
    }

    public void RemoveItem(int slotIndex, int quantity = 1)
    {
        if (slotIndex < 0 || slotIndex >= slots.Count) return;
        if (slots[slotIndex].IsEmpty) return;

        slots[slotIndex].quantity -= quantity;

        if (slots[slotIndex].quantity <= 0)
        {
            slots[slotIndex].item = null;
            slots[slotIndex].quantity = 0;
        }

        SyncInventoryServerRpc();
    }

    [ServerRpc]
    private void SyncInventoryServerRpc()
    {
        // Sync to all clients
        networkSlots.Clear();
        foreach (var slot in slots)
        {
            networkSlots.Add(slot);
        }
    }
}
```

---

## 4. Networking Architecture

### 4.1 Network Topology

```
Client 1 ──┐
Client 2 ──┼──→ Unity Relay Server ←──→ Host/Server
Client 3 ──┘         ↓
           Unity Matchmaking
                ↓
           Player Lobbies
```

**Architecture:** Client-Server (not P2P)
- Server authoritative for security
- Unity Relay for NAT traversal
- Dedicated server for ranked (future)

### 4.2 Network Synchronization Strategy

**What Syncs:**
- ✅ Player position (via NetworkTransform)
- ✅ Player health (via NetworkVariable)
- ✅ Weapon shots (via ServerRPC)
- ✅ Zombie state (via NetworkVariable)
- ✅ Loot spawns (server-authoritative)
- ❌ Local animations (each client handles own)
- ❌ VFX (client-side only)
- ❌ UI (local only)

**NetworkTransform Settings:**
```csharp
public class PlayerNetworkTransform : NetworkTransform
{
    protected override void Update()
    {
        base.Update();

        // Client-side prediction
        if (IsOwner)
        {
            // Apply movement immediately (feels responsive)
            transform.position += movement * Time.deltaTime;
        }
    }

    // Interpolation for other players
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsOwner)
        {
            // Smooth position updates from server
            Interpolate = true;
        }
    }
}
```

### 4.3 Lag Compensation

**Client-Side Prediction:**
```csharp
public class PredictedMovement : NetworkBehaviour
{
    private Queue<InputCommand> commandHistory = new Queue<InputCommand>();
    private Vector3 serverPosition;

    private void Update()
    {
        if (IsOwner)
        {
            // Get input
            var input = new InputCommand
            {
                tick = NetworkManager.Singleton.LocalTime.Tick,
                movement = GetMovementInput()
            };

            // Apply locally (feels instant)
            ApplyMovement(input.movement);

            // Send to server
            SendInputServerRpc(input);

            // Store for reconciliation
            commandHistory.Enqueue(input);
            if (commandHistory.Count > 60) // 1 second of history
                commandHistory.Dequeue();
        }
    }

    [ServerRpc]
    private void SendInputServerRpc(InputCommand input)
    {
        // Server validates and applies movement
        ApplyMovement(input.movement);

        // Send authoritative position back
        UpdatePositionClientRpc(transform.position, input.tick);
    }

    [ClientRpc]
    private void UpdatePositionClientRpc(Vector3 position, int tick)
    {
        if (!IsOwner) return;

        serverPosition = position;

        // Reconciliation: Check if prediction was wrong
        if (Vector3.Distance(transform.position, serverPosition) > 0.5f)
        {
            // Server says we're wrong, teleport to correct position
            transform.position = serverPosition;

            // Replay inputs since that tick
            ReplayInputs(tick);
        }
    }
}
```

### 4.4 Matchmaking Flow

```
Player Clicks "Play"
    ↓
Create/Join Lobby (Unity Lobby Service)
    ↓
Wait for Players (8/8)
    ↓
Start Match → Allocate Relay (Unity Relay)
    ↓
Load Game Scene
    ↓
Spawn Players
    ↓
Match Start (10 min timer)
```

**LobbyManager.cs:**
```csharp
public class LobbyManager : MonoBehaviour
{
    private Lobby currentLobby;

    public async Task<Lobby> CreateLobby(string lobbyName, int maxPlayers = 8)
    {
        var options = new CreateLobbyOptions
        {
            IsPrivate = false,
            Player = GetPlayer(),
            Data = new Dictionary<string, DataObject>
            {
                { "GameMode", new DataObject(DataObject.VisibilityOptions.Public, "Extraction") },
                { "Map", new DataObject(DataObject.VisibilityOptions.Public, "DowntownRuins") }
            }
        };

        currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);

        StartCoroutine(HeartbeatLobbyCoroutine(currentLobby.Id, 15f));

        return currentLobby;
    }

    public async Task<List<Lobby>> QueryLobbies()
    {
        var queryOptions = new QueryLobbiesOptions
        {
            Count = 25,
            Filters = new List<QueryFilter>
            {
                new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
            },
            Order = new List<QueryOrder>
            {
                new QueryOrder(false, QueryOrder.FieldOptions.Created)
            }
        };

        var response = await Lobbies.Instance.QueryLobbiesAsync(queryOptions);
        return response.Results;
    }

    private IEnumerator HeartbeatLobbyCoroutine(string lobbyId, float interval)
    {
        while (currentLobby != null)
        {
            LobbyService.Instance.SendHeartbeatPingAsync(lobbyId);
            yield return new WaitForSeconds(interval);
        }
    }
}
```

---

## 5. Performance Optimization

### 5.1 Graphics Optimization

**Level of Detail (LOD) System:**
```csharp
public class DynamicLOD : MonoBehaviour
{
    [SerializeField] private LODGroup lodGroup;
    [SerializeField] private float[] lodDistances = { 10f, 30f, 60f };

    private void Start()
    {
        var lods = new LOD[lodDistances.Length];

        for (int i = 0; i < lodDistances.Length; i++)
        {
            var renderers = GetRenderersForLOD(i);
            lods[i] = new LOD(1.0f / lodDistances[i], renderers);
        }

        lodGroup.SetLODs(lods);
        lodGroup.RecalculateBounds();
    }
}
```

**Occlusion Culling:**
- Bake occlusion data in Unity
- Buildings block rendering of objects behind them
- Saves 30-40% GPU time in urban environments

**Texture Streaming:**
```csharp
// In URP Asset settings
Texture Quality: Mipmap Streaming = Enabled
Streaming Budget: 512 MB (PC), 256 MB (Mobile)
```

**Shader Optimization:**
- Use URP/Lit for most objects (mobile-optimized)
- Avoid transparency where possible (expensive on mobile)
- Bake lighting (no realtime lights on mobile)

### 5.2 CPU Optimization

**Object Pooling:**
```csharp
public class PoolManager : MonoBehaviour
{
    private Dictionary<string, ObjectPool<GameObject>> pools = new Dictionary<string, ObjectPool<GameObject>>();

    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        string key = prefab.name;

        if (!pools.ContainsKey(key))
        {
            pools[key] = new ObjectPool<GameObject>(
                createFunc: () => Instantiate(prefab),
                actionOnGet: (obj) => { obj.SetActive(true); obj.transform.SetPositionAndRotation(position, rotation); },
                actionOnRelease: (obj) => obj.SetActive(false),
                actionOnDestroy: (obj) => Destroy(obj),
                collectionCheck: true,
                defaultCapacity: 20,
                maxSize: 100
            );
        }

        return pools[key].Get();
    }

    public void Return(GameObject obj, float delay = 0f)
    {
        if (delay > 0)
            StartCoroutine(ReturnDelayed(obj, delay));
        else
            pools[obj.name].Release(obj);
    }

    private IEnumerator ReturnDelayed(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        pools[obj.name.Replace("(Clone)", "")].Release(obj);
    }
}
```

**Pooled Objects:**
- Zombies (100 max)
- Bullets/Tracers (50 max)
- VFX particles (30 max)
- Audio sources (20 max)
- UI damage numbers (20 max)

**Update Loop Optimization:**
```csharp
// BAD: Update every frame
void Update()
{
    CheckForEnemies(); // Expensive
}

// GOOD: Update at intervals
private float updateInterval = 0.2f;
private float nextUpdate;

void Update()
{
    if (Time.time >= nextUpdate)
    {
        nextUpdate = Time.time + updateInterval;
        CheckForEnemies();
    }
}

// BETTER: Coroutine
private IEnumerator CheckForEnemiesRoutine()
{
    while (true)
    {
        CheckForEnemies();
        yield return new WaitForSeconds(0.2f);
    }
}
```

**Spatial Hashing for Collision Checks:**
```csharp
public class SpatialHash
{
    private Dictionary<Vector2Int, List<Transform>> grid = new Dictionary<Vector2Int, List<Transform>>();
    private float cellSize;

    public SpatialHash(float cellSize = 10f)
    {
        this.cellSize = cellSize;
    }

    public void Insert(Transform obj)
    {
        Vector2Int cell = GetCell(obj.position);
        if (!grid.ContainsKey(cell))
            grid[cell] = new List<Transform>();
        grid[cell].Add(obj);
    }

    public List<Transform> Query(Vector3 position, float radius)
    {
        List<Transform> results = new List<Transform>();
        Vector2Int centerCell = GetCell(position);
        int cellRadius = Mathf.CeilToInt(radius / cellSize);

        for (int x = -cellRadius; x <= cellRadius; x++)
        {
            for (int y = -cellRadius; y <= cellRadius; y++)
            {
                Vector2Int cell = centerCell + new Vector2Int(x, y);
                if (grid.TryGetValue(cell, out var objects))
                    results.AddRange(objects);
            }
        }

        return results;
    }

    private Vector2Int GetCell(Vector3 position)
    {
        return new Vector2Int(
            Mathf.FloorToInt(position.x / cellSize),
            Mathf.FloorToInt(position.z / cellSize)
        );
    }
}
```

### 5.3 Memory Optimization

**Asset Bundles for WebGL:**
```csharp
public class AssetBundleManager : MonoBehaviour
{
    private Dictionary<string, AssetBundle> loadedBundles = new Dictionary<string, AssetBundle>();

    public async Task<T> LoadAssetAsync<T>(string bundleName, string assetName) where T : UnityEngine.Object
    {
        if (!loadedBundles.ContainsKey(bundleName))
        {
            var bundleRequest = AssetBundle.LoadFromFileAsync(
                Path.Combine(Application.streamingAssetsPath, bundleName)
            );
            await bundleRequest;
            loadedBundles[bundleName] = bundleRequest.assetBundle;
        }

        var assetRequest = loadedBundles[bundleName].LoadAssetAsync<T>(assetName);
        await assetRequest;
        return assetRequest.asset as T;
    }

    public void UnloadBundle(string bundleName, bool unloadAllLoadedObjects = false)
    {
        if (loadedBundles.TryGetValue(bundleName, out var bundle))
        {
            bundle.Unload(unloadAllLoadedObjects);
            loadedBundles.Remove(bundleName);
        }
    }
}
```

**Texture Compression:**
```
PC: DXT5 (RGBA), DXT1 (RGB)
Android: ASTC 6x6 (high quality), ETC2 (compatibility)
iOS: ASTC 6x6
WebGL: DXT5 (fallback to RGBA32 for Safari)
```

**Audio Compression:**
```
Music: Vorbis, Quality 0.7, Streaming
SFX: ADPCM (short sounds), Vorbis (long sounds)
```

### 5.4 Network Optimization

**Snapshot Interpolation:**
```csharp
public class NetworkedTransform : NetworkBehaviour
{
    private struct TransformSnapshot
    {
        public float timestamp;
        public Vector3 position;
        public Quaternion rotation;
    }

    private Queue<TransformSnapshot> snapshots = new Queue<TransformSnapshot>();
    private float interpolationDelay = 0.1f; // 100ms

    [ServerRpc]
    private void SendTransformServerRpc(Vector3 position, Quaternion rotation, float timestamp)
    {
        // Server validates and broadcasts
        SendTransformClientRpc(position, rotation, timestamp);
    }

    [ClientRpc]
    private void SendTransformClientRpc(Vector3 position, Quaternion rotation, float timestamp)
    {
        if (IsOwner) return; // Don't interpolate own character

        snapshots.Enqueue(new TransformSnapshot
        {
            timestamp = timestamp,
            position = position,
            rotation = rotation
        });

        // Keep only last 1 second
        while (snapshots.Count > 60)
            snapshots.Dequeue();
    }

    private void Update()
    {
        if (IsOwner || snapshots.Count < 2) return;

        float renderTime = Time.time - interpolationDelay;

        TransformSnapshot from = snapshots.Peek();
        TransformSnapshot to = snapshots.Peek();

        // Find snapshots to interpolate between
        foreach (var snapshot in snapshots)
        {
            if (snapshot.timestamp <= renderTime)
                from = snapshot;
            else
            {
                to = snapshot;
                break;
            }
        }

        float t = (renderTime - from.timestamp) / (to.timestamp - from.timestamp);
        transform.position = Vector3.Lerp(from.position, to.position, t);
        transform.rotation = Quaternion.Slerp(from.rotation, to.rotation, t);
    }
}
```

**Bandwidth Optimization:**
- Send position updates at 20Hz (not every frame)
- Use delta compression (only send changed values)
- Quantize floats (position precision to 0.01m)

```csharp
public static class NetworkCompression
{
    // Compress Vector3 to 12 bytes instead of 24
    public static void WriteVector3(FastBufferWriter writer, Vector3 value)
    {
        writer.WriteValueSafe((short)(value.x * 100)); // 2 bytes, 0.01m precision
        writer.WriteValueSafe((short)(value.y * 100));
        writer.WriteValueSafe((short)(value.z * 100));
    }

    public static Vector3 ReadVector3(FastBufferReader reader)
    {
        reader.ReadValueSafe(out short x);
        reader.ReadValueSafe(out short y);
        reader.ReadValueSafe(out short z);
        return new Vector3(x / 100f, y / 100f, z / 100f);
    }

    // Compress Quaternion to 4 bytes (smallest-three algorithm)
    public static void WriteQuaternion(FastBufferWriter writer, Quaternion value)
    {
        // Find largest component
        int largestIndex = 0;
        float largestValue = Mathf.Abs(value[0]);
        for (int i = 1; i < 4; i++)
        {
            if (Mathf.Abs(value[i]) > largestValue)
            {
                largestIndex = i;
                largestValue = Mathf.Abs(value[i]);
            }
        }

        // Write index (2 bits) + 3 other components (10 bits each)
        uint compressed = (uint)largestIndex << 30;

        for (int i = 0, j = 0; i < 4; i++)
        {
            if (i == largestIndex) continue;
            int quantized = Mathf.RoundToInt((value[i] / Mathf.Sqrt(2)) * 1023);
            compressed |= (uint)(quantized & 0x3FF) << (j * 10);
            j++;
        }

        writer.WriteValueSafe(compressed);
    }
}
```

---

## 6. Data Structures

### 6.1 ScriptableObject Architecture

**Purpose:** Separate data from logic for:
- Easy balancing (no code changes)
- Asset reusability
- Memory efficiency (shared instances)

**Examples:**

**ZombieConfig.cs:**
```csharp
[CreateAssetMenu(fileName = "ZombieConfig", menuName = "Game/Zombie Config")]
public class ZombieConfig : ScriptableObject
{
    [Header("Stats")]
    public int maxHealth = 100;
    public float moveSpeed = 3f;
    public float chaseSpeed = 5f;
    public int damage = 15;
    public float attackRange = 2f;
    public float attackCooldown = 1.5f;

    [Header("Senses")]
    public float visionRange = 30f;
    public float visionAngle = 180f;
    public float hearingRange = 50f;

    [Header("Loot")]
    public LootTable lootTable;
    public int xpReward = 10;

    [Header("Visuals")]
    public GameObject modelPrefab;
    public RuntimeAnimatorController animatorController;
    public AudioClip[] idleSounds;
    public AudioClip[] attackSounds;
    public AudioClip[] deathSounds;
}
```

**ItemData.cs:**
```csharp
[CreateAssetMenu(fileName = "NewItem", menuName = "Game/Item")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public string description;
    public Sprite icon;
    public GameObject worldPrefab;

    public ItemType type;
    public ItemRarity rarity;
    public bool isStackable = true;
    public int maxStackSize = 99;
    public int sellValue = 10;

    // Type-specific data
    [Header("Weapon Data (if weapon)")]
    public WeaponData weaponData;

    [Header("Consumable Data (if consumable)")]
    public int healAmount;
    public float useTime = 3f;
}

public enum ItemType
{
    Weapon,
    Ammo,
    Medical,
    Valuable,
    Key,
    Armor
}

public enum ItemRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}
```

**LootTable.cs:**
```csharp
[CreateAssetMenu(fileName = "LootTable", menuName = "Game/Loot Table")]
public class LootTable : ScriptableObject
{
    [System.Serializable]
    public class LootEntry
    {
        public ItemData item;
        [Range(0f, 100f)] public float dropChance = 50f;
        public int minQuantity = 1;
        public int maxQuantity = 1;
    }

    public List<LootEntry> loot = new List<LootEntry>();

    public List<ItemData> Roll()
    {
        List<ItemData> drops = new List<ItemData>();

        foreach (var entry in loot)
        {
            if (Random.Range(0f, 100f) <= entry.dropChance)
            {
                int quantity = Random.Range(entry.minQuantity, entry.maxQuantity + 1);
                for (int i = 0; i < quantity; i++)
                {
                    drops.Add(entry.item);
                }
            }
        }

        return drops;
    }
}
```

### 6.2 Save System

**SaveData.cs:**
```csharp
[System.Serializable]
public class PlayerSaveData
{
    public int playerLevel;
    public int xp;
    public int currency;

    public List<string> unlockedCosmetics = new List<string>();
    public List<InventorySlot> stashItems = new List<InventorySlot>();

    public int totalMatches;
    public int totalKills;
    public int totalExtractions;
    public int totalDeaths;

    public string lastLoginDate;
}

public class SaveManager : MonoBehaviour
{
    private const string SAVE_KEY = "PlayerSave";

    public static void SavePlayer(PlayerSaveData data)
    {
        string json = JsonUtility.ToJson(data, true);
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();
    }

    public static PlayerSaveData LoadPlayer()
    {
        if (PlayerPrefs.HasKey(SAVE_KEY))
        {
            string json = PlayerPrefs.GetString(SAVE_KEY);
            return JsonUtility.FromJson<PlayerSaveData>(json);
        }

        return new PlayerSaveData();
    }

    // Cloud save (future)
    public static async Task<PlayerSaveData> LoadFromCloud(string playerId)
    {
        // Use Unity Cloud Save
        var response = await CloudSaveService.Instance.Data.LoadAsync(
            new HashSet<string> { "PlayerData" }
        );

        if (response.TryGetValue("PlayerData", out var item))
        {
            return JsonUtility.FromJson<PlayerSaveData>(item.Value.GetAsString());
        }

        return new PlayerSaveData();
    }
}
```

---

## 7. Code Organization

### 7.1 Project Structure

```
Assets/
├── _Project/                    # Game-specific assets
│   ├── Scripts/
│   │   ├── Player/
│   │   │   ├── PlayerController.cs
│   │   │   ├── PlayerMovement.cs
│   │   │   ├── PlayerHealth.cs
│   │   │   ├── PlayerInventory.cs
│   │   │   └── PlayerWeapon.cs
│   │   ├── Zombie/
│   │   │   ├── ZombieAI.cs
│   │   │   ├── ZombieSensors.cs
│   │   │   ├── ZombieStateMachine.cs
│   │   │   └── States/
│   │   │       ├── IdleState.cs
│   │   │       ├── ChaseState.cs
│   │   │       ├── AttackState.cs
│   │   │       └── FleeState.cs
│   │   ├── Weapons/
│   │   │   ├── WeaponController.cs
│   │   │   ├── WeaponData.cs
│   │   │   └── WeaponFire.cs
│   │   ├── Networking/
│   │   │   ├── NetworkManager.cs
│   │   │   ├── LobbyManager.cs
│   │   │   └── MatchmakingManager.cs
│   │   ├── Managers/
│   │   │   ├── GameManager.cs
│   │   │   ├── AudioManager.cs
│   │   │   ├── PoolManager.cs
│   │   │   ├── UIManager.cs
│   │   │   └── SaveManager.cs
│   │   ├── UI/
│   │   │   ├── HUD/
│   │   │   ├── Menus/
│   │   │   └── Inventory/
│   │   ├── Utilities/
│   │   │   ├── ObjectPool.cs
│   │   │   ├── SpatialHash.cs
│   │   │   └── Extensions.cs
│   │   └── Data/
│   │       ├── ScriptableObjects/
│   │       ├── Enums.cs
│   │       └── Structs.cs
│   ├── Prefabs/
│   │   ├── Player/
│   │   ├── Zombies/
│   │   ├── Weapons/
│   │   ├── Loot/
│   │   └── VFX/
│   ├── Scenes/
│   │   ├── MainMenu.unity
│   │   ├── Lobby.unity
│   │   ├── DowntownRuins.unity
│   │   └── TestScene.unity
│   ├── Materials/
│   ├── Textures/
│   ├── Models/
│   ├── Audio/
│   │   ├── Music/
│   │   ├── SFX/
│   │   └── Mixers/
│   └── Resources/
├── Packages/                    # Unity packages
├── ProjectSettings/
└── Docs/                        # Documentation (this file!)
```

### 7.2 Naming Conventions

**Files:**
- PascalCase for all files: `PlayerController.cs`, `ZombieAI.cs`
- Prefix interfaces with `I`: `IDamageable.cs`
- Suffix ScriptableObjects with type: `WeaponData.cs`, `ZombieConfig.cs`

**Code:**
```csharp
// Classes, Structs, Enums: PascalCase
public class PlayerController { }
public struct DamageInfo { }
public enum WeaponType { }

// Methods, Properties: PascalCase
public void TakeDamage(int amount) { }
public int CurrentHealth { get; set; }

// Fields (private): camelCase
private int currentAmmo;
private float fireRate;

// Fields (serialized): camelCase
[SerializeField] private GameObject bulletPrefab;

// Constants: UPPER_SNAKE_CASE
private const float MAX_SPEED = 10f;
private const int ZOMBIE_LAYER = 8;

// Events: PascalCase with "On" prefix
public event Action OnPlayerDied;
public event Action<int> OnHealthChanged;
```

### 7.3 Code Style Guidelines

**Keep It Simple:**
```csharp
// BAD: Over-engineered
public abstract class AbstractWeaponFactoryProxyHandler : IWeaponFactory, IDisposable
{
    // 200 lines of abstraction...
}

// GOOD: Simple and clear
public class WeaponController : MonoBehaviour
{
    public void Fire() { /* logic */ }
}
```

**Early Returns:**
```csharp
// BAD: Nested ifs
public void TakeDamage(int amount)
{
    if (isAlive)
    {
        if (amount > 0)
        {
            if (armor > 0)
            {
                // logic
            }
        }
    }
}

// GOOD: Early returns
public void TakeDamage(int amount)
{
    if (!isAlive) return;
    if (amount <= 0) return;

    if (armor > 0)
    {
        // logic
    }
}
```

**Comments:**
```csharp
// Only comment WHY, not WHAT

// BAD: Obvious
// Set health to 100
health = 100;

// GOOD: Explains reasoning
// Reset health to full because respawn should give players a fresh start
health = 100;

// GOOD: Complex algorithm explanation
/// <summary>
/// Uses A* pathfinding with heuristic cost based on:
/// 1. Distance to goal (Manhattan distance)
/// 2. Zombie density (avoid crowded areas)
/// 3. Player noise level (seek loud players)
/// </summary>
public Vector3 FindOptimalPath() { }
```

---

## 8. Third-Party Integrations

### 8.1 Required Packages (Unity Package Manager)

```json
{
  "dependencies": {
    "com.unity.netcode.gameobjects": "1.7.1",
    "com.unity.transport": "2.0.2",
    "com.unity.services.lobby": "1.0.3",
    "com.unity.services.relay": "1.0.5",
    "com.unity.services.authentication": "2.7.0",
    "com.unity.render-pipelines.universal": "14.0.9",
    "com.unity.cinemachine": "2.9.7",
    "com.unity.probuilder": "5.2.2",
    "com.unity.textmeshpro": "3.0.6",
    "com.unity.inputsystem": "1.7.0"
  }
}
```

### 8.2 Optional Assets (Unity Asset Store)

**Must-Have:**
1. **Multiplayer (STP) Survival Template PRO** (€45.99)
   - Networking foundation
   - Inventory system
   - Player controller base

2. **Zombie Wave Survival COOP** (Free?)
   - Zombie AI reference
   - Wave spawning system

**Nice-to-Have:**
3. **Post-Processing Stack v2** (Free)
   - Color grading
   - Bloom, vignette
   - Horror atmosphere

4. **DOTween** (Free)
   - UI animations
   - Camera shake
   - Smooth transitions

### 8.3 Analytics Integration

**Unity Analytics:**
```csharp
public static class AnalyticsEvents
{
    public static void PlayerDied(string cause, Vector3 location)
    {
        Analytics.CustomEvent("player_died", new Dictionary<string, object>
        {
            { "cause", cause },
            { "location_x", location.x },
            { "location_z", location.z },
            { "match_time", GameManager.Instance.MatchTime }
        });
    }

    public static void ExtractedSuccessfully(int lootValue, int zombieKills)
    {
        Analytics.CustomEvent("extraction_success", new Dictionary<string, object>
        {
            { "loot_value", lootValue },
            { "zombie_kills", zombieKills },
            { "survival_time", GameManager.Instance.MatchTime }
        });
    }
}
```

---

## 9. Build Pipeline

### 9.1 Build Settings

**PC (Windows):**
```
Target: Standalone
Architecture: x86_64
Scripting Backend: IL2CPP
API Compatibility: .NET Standard 2.1
Compression: LZ4HC (faster loading)
```

**Mobile (Android):**
```
Target: Android
Min API Level: 24 (Android 7.0)
Target API Level: 34 (Android 14)
Scripting Backend: IL2CPP
Architecture: ARM64
Compression: LZ4
Split APKs by Architecture: Yes
```

**Mobile (iOS):**
```
Target: iOS
Min iOS Version: 13.0
Target SDK: Latest
Scripting Backend: IL2CPP
Architecture: ARM64
Compression: LZ4
```

**WebGL:**
```
Target: WebGL
Compression: Brotli
Memory Size: 2048 MB
Enable Exceptions: None (smallest build)
Linker Target: Wasm
```

### 9.2 Build Automation (GitHub Actions)

**.github/workflows/build.yml:**
```yaml
name: Unity Build

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  build:
    runs-on: ubuntu-latest
    strategy:
      matrix:
        targetPlatform:
          - StandaloneWindows64
          - Android
          - WebGL

    steps:
      - uses: actions/checkout@v3

      - uses: actions/cache@v3
        with:
          path: Library
          key: Library-${{ matrix.targetPlatform }}

      - uses: game-ci/unity-builder@v3
        with:
          targetPlatform: ${{ matrix.targetPlatform }}
          unityVersion: 2022.3.20f1

      - uses: actions/upload-artifact@v3
        with:
          name: Build-${{ matrix.targetPlatform }}
          path: build/${{ matrix.targetPlatform }}
```

---

## 10. Testing Strategy

### 10.1 Unit Tests

**Example Test (NUnit):**
```csharp
[TestFixture]
public class InventorySystemTests
{
    private InventorySystem inventory;

    [SetUp]
    public void Setup()
    {
        inventory = new GameObject().AddComponent<InventorySystem>();
    }

    [Test]
    public void AddItem_WithEmptySlot_ShouldSucceed()
    {
        // Arrange
        var item = ScriptableObject.CreateInstance<ItemData>();

        // Act
        bool result = inventory.AddItem(item, 1);

        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual(1, inventory.GetItemCount(item));
    }

    [Test]
    public void AddItem_WithFullInventory_ShouldFail()
    {
        // Arrange
        var item = ScriptableObject.CreateInstance<ItemData>();
        for (int i = 0; i < inventory.MaxSlots; i++)
        {
            inventory.AddItem(item, 1);
        }

        // Act
        bool result = inventory.AddItem(item, 1);

        // Assert
        Assert.IsFalse(result);
    }
}
```

### 10.2 Integration Tests

**Multiplayer Test:**
```csharp
[UnityTest]
public IEnumerator MultiplayerConnection_ShouldConnectTwoPlayers()
{
    // Arrange
    NetworkManager.Singleton.StartHost();
    yield return new WaitForSeconds(1f);

    // Act
    NetworkManager.Singleton.StartClient();
    yield return new WaitForSeconds(2f);

    // Assert
    Assert.AreEqual(2, NetworkManager.Singleton.ConnectedClients.Count);
}
```

### 10.3 Performance Tests

**FPS Benchmark:**
```csharp
[Test, Performance]
public void ZombieSpawning_With100Zombies_ShouldMaintain60FPS()
{
    using (Measure.Frames().WarmupCount(10).MeasurementCount(100).Run())
    {
        for (int i = 0; i < 100; i++)
        {
            ZombieSpawner.Instance.SpawnZombie(RandomPosition());
        }
    }
}
```

---

## 11. Performance Budgets

### 11.1 Target Budgets

| Metric | PC (Low) | PC (High) | Mobile (Low) | Mobile (High) |
|--------|----------|-----------|--------------|---------------|
| FPS | 60 | 120+ | 45 | 60 |
| Draw Calls | 500 | 1500 | 200 | 400 |
| Batches | 400 | 1200 | 150 | 300 |
| Tris | 500K | 2M | 200K | 500K |
| Verts | 300K | 1M | 100K | 300K |
| Texture Memory | 500MB | 2GB | 200MB | 500MB |
| Mesh Memory | 100MB | 300MB | 50MB | 100MB |
| Audio Memory | 50MB | 150MB | 30MB | 50MB |

### 11.2 Profiling Targets

**Frame Time Budget (60 FPS = 16.67ms):**
- Rendering: 8ms
- Scripts: 4ms
- Physics: 2ms
- Animation: 1ms
- Networking: 1ms
- Other: 0.67ms

**Memory Budget (Mobile):**
- Textures: 200MB
- Meshes: 50MB
- Audio: 30MB
- Scripts: 20MB
- Other: 50MB
- **Total:** 350MB (safe for 2GB device)

---

## 12. Security Considerations

### 12.1 Anti-Cheat Measures

**Server-Side Validation:**
```csharp
[ServerRpc]
public void DealDamageServerRpc(ulong targetId, int damage)
{
    // Validate damage is from equipped weapon
    if (damage > GetMaxDamage())
    {
        Debug.LogWarning($"Player {OwnerClientId} sent invalid damage: {damage}");
        return; // Ignore cheater
    }

    // Validate target is in range
    if (!IsInRange(targetId))
    {
        Debug.LogWarning($"Player {OwnerClientId} hit out-of-range target");
        return;
    }

    // Valid, apply damage
    ApplyDamage(targetId, damage);
}

private int GetMaxDamage()
{
    return currentWeapon?.damage ?? 0;
}

private bool IsInRange(ulong targetId)
{
    var target = NetworkManager.Singleton.ConnectedClients[targetId].PlayerObject;
    float distance = Vector3.Distance(transform.position, target.transform.position);
    return distance <= currentWeapon.range + 5f; // 5m tolerance for lag
}
```

**Movement Validation:**
```csharp
[ServerRpc]
public void SendPositionServerRpc(Vector3 position)
{
    // Check if position is possible (not teleporting)
    float distance = Vector3.Distance(lastPosition, position);
    float maxPossibleDistance = MAX_SPEED * Time.deltaTime * 1.2f; // 20% tolerance

    if (distance > maxPossibleDistance)
    {
        // Possible teleport hack, reject
        Debug.LogWarning($"Player {OwnerClientId} moved too fast: {distance}m");
        SendCorrectedPositionClientRpc(lastPosition); // Force correct position
        return;
    }

    lastPosition = position;
    transform.position = position;
}
```

**Rate Limiting:**
```csharp
public class RateLimiter
{
    private Dictionary<ulong, Queue<float>> actionTimestamps = new Dictionary<ulong, Queue<float>>();

    public bool IsAllowed(ulong clientId, string action, int maxPerSecond)
    {
        if (!actionTimestamps.ContainsKey(clientId))
            actionTimestamps[clientId] = new Queue<float>();

        var timestamps = actionTimestamps[clientId];
        float currentTime = Time.time;

        // Remove old timestamps (>1 second old)
        while (timestamps.Count > 0 && currentTime - timestamps.Peek() > 1f)
        {
            timestamps.Dequeue();
        }

        // Check rate
        if (timestamps.Count >= maxPerSecond)
        {
            Debug.LogWarning($"Client {clientId} exceeded rate limit for {action}");
            return false;
        }

        timestamps.Enqueue(currentTime);
        return true;
    }
}
```

### 12.2 Data Encryption

**Save File Encryption:**
```csharp
public static class SecureSave
{
    private const string ENCRYPTION_KEY = "YourSecretKey123"; // Move to secure location

    public static void SaveEncrypted(string key, object data)
    {
        string json = JsonUtility.ToJson(data);
        string encrypted = Encrypt(json, ENCRYPTION_KEY);
        PlayerPrefs.SetString(key, encrypted);
    }

    public static T LoadEncrypted<T>(string key)
    {
        string encrypted = PlayerPrefs.GetString(key);
        string json = Decrypt(encrypted, ENCRYPTION_KEY);
        return JsonUtility.FromJson<T>(json);
    }

    private static string Encrypt(string plainText, string key)
    {
        // Use AES encryption (example simplified)
        // In production, use System.Security.Cryptography.Aes
        byte[] encrypted = System.Text.Encoding.UTF8.GetBytes(plainText);
        return Convert.ToBase64String(encrypted);
    }

    private static string Decrypt(string cipherText, string key)
    {
        byte[] decrypted = Convert.FromBase64String(cipherText);
        return System.Text.Encoding.UTF8.GetString(decrypted);
    }
}
```

---

## 13. Conclusion

This Technical Design Document provides the architectural foundation for Dead Frontier: Outbreak. Key takeaways:

**Performance-First:**
- Object pooling for all frequently spawned objects
- Spatial partitioning for AI queries
- LOD system for graphics
- Server authoritative for security

**Scalable Architecture:**
- ScriptableObjects for data-driven design
- Modular systems (easy to add new weapons, zombies, maps)
- Event-driven communication (decoupled)

**Production-Ready:**
- Netcode for GameObjects (Unity supported)
- Automated build pipeline
- Analytics integration
- Anti-cheat measures

**Next Steps:**
1. Set up Unity project with specified packages
2. Implement core player controller
3. Create basic zombie AI
4. Add networking layer
5. Build MVP map
6. Playtest and iterate

---

**Questions? Contact technical lead.**

*Last Updated: November 2025*
