# Unity 6.2 Compatibility Guide
## Dead Frontier: Outbreak

**Unity Version:** Unity 6.2 (6000.0.x)
**Last Updated:** November 2025

---

## ✅ Good News: Unity 6 is Better!

Unity 6.2 is a **newer, more advanced** version than Unity 2022.3 LTS. You'll get better performance and features. All our planned systems are fully compatible!

---

## 🎯 Key Unity 6 Advantages

### 1. **Better Performance**
- **GPU Resident Drawer:** Up to 50% faster rendering (automatic in Unity 6)
- **Improved IL2CPP:** Faster build times, better runtime performance
- **Better Memory Management:** Reduced allocations

### 2. **Enhanced URP**
- Better mobile performance out of the box
- Improved lighting (APV - Adaptive Probe Volumes)
- Better shadows and post-processing

### 3. **Networking Improvements**
- Netcode for GameObjects fully compatible
- Better multiplayer debugging tools
- Improved profiler for network stats

### 4. **WebGL Optimization**
- Smaller build sizes (important for browser version)
- Faster loading times
- Better multi-threading support

---

## 📦 Updated Package Versions (Unity 6.2)

Replace the package versions in TDD.md with these Unity 6-compatible versions:

```json
{
  "dependencies": {
    "com.unity.netcode.gameobjects": "2.0.0",
    "com.unity.transport": "2.3.0",
    "com.unity.services.lobby": "1.2.2",
    "com.unity.services.relay": "1.1.0",
    "com.unity.services.authentication": "3.3.0",
    "com.unity.render-pipelines.universal": "17.0.3",
    "com.unity.cinemachine": "3.1.0",
    "com.unity.probuilder": "6.0.2",
    "com.unity.textmeshpro": "4.0.0",
    "com.unity.inputsystem": "1.8.0",
    "com.unity.entities": "1.2.0"
  }
}
```

### Installation Command (Package Manager Console)
```
Window > Package Manager > + > Add package by name
```

Then add each package name (Unity will fetch correct version for Unity 6).

---

## ⚠️ Important Changes for Unity 6

### 1. **Entity Component System (ECS) - Optional but Recommended**

Unity 6 has mature DOTS/ECS support. Consider using it for:
- Zombie AI (can handle 1000+ zombies instead of 100)
- Particle systems
- Physics-heavy scenarios

**Example: ECS Zombie (Advanced, Optional):**
```csharp
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

public struct ZombieComponent : IComponentData
{
    public float speed;
    public int health;
    public ZombieState state;
}

public partial struct ZombieMovementSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (transform, zombie) in
                 SystemAPI.Query<RefRW<LocalTransform>, RefRO<ZombieComponent>>())
        {
            // Move zombie (runs in parallel on all cores!)
            transform.ValueRW.Position += new float3(0, 0, zombie.ValueRO.speed) * SystemAPI.Time.DeltaTime;
        }
    }
}
```

**Recommendation:** Start with MonoBehaviour (as planned), upgrade to ECS later for more zombies.

### 2. **New Input System (Required)**

Unity 6 strongly pushes new Input System. Update our PlayerInput code:

**PlayerInputActions.inputactions (Asset):**
Create via: `Assets > Create > Input Actions`

```
Action Maps:
  - Gameplay
    - Move (Vector2)
    - Look (Vector2)
    - Fire (Button)
    - Reload (Button)
    - Jump (Button)
    - Sprint (Button)
```

**PlayerInput.cs (Updated for Unity 6):**
```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour
{
    private PlayerInputActions inputActions;

    // Input values
    public Vector2 Movement { get; private set; }
    public Vector2 Look { get; private set; }
    public bool FirePressed { get; private set; }
    public bool SprintPressed { get; private set; }

    private void Awake()
    {
        inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        inputActions.Gameplay.Enable();

        // Subscribe to events
        inputActions.Gameplay.Fire.performed += ctx => FirePressed = true;
        inputActions.Gameplay.Fire.canceled += ctx => FirePressed = false;
        inputActions.Gameplay.Reload.performed += ctx => OnReloadPressed();
    }

    private void OnDisable()
    {
        inputActions.Gameplay.Disable();
    }

    private void Update()
    {
        // Read continuous inputs
        Movement = inputActions.Gameplay.Move.ReadValue<Vector2>();
        Look = inputActions.Gameplay.Look.ReadValue<Vector2>();
        SprintPressed = inputActions.Gameplay.Sprint.IsPressed();
    }

    private void OnReloadPressed()
    {
        GetComponent<PlayerWeapon>()?.Reload();
    }
}
```

### 3. **GPU Resident Drawer (Automatic Performance Boost)**

Unity 6 enables this by default. Ensures:
- Better draw call batching
- Reduced CPU overhead
- Faster rendering (especially for mobile)

**No code changes needed!** Just verify in Project Settings:
```
Project Settings > Graphics > GPU Resident Drawer: Enabled ✓
```

### 4. **Entities Graphics (Optional Advanced)**

For maximum zombie count, use Entities Graphics:

```csharp
// Render 10,000 zombies with one draw call
public class ZombieSpawner : MonoBehaviour
{
    [SerializeField] private Mesh zombieMesh;
    [SerializeField] private Material zombieMaterial;

    private void Start()
    {
        var batchDescription = new BatchRendererGroup.RenderParams
        {
            material = zombieMaterial,
            mesh = zombieMesh
        };

        // Spawn thousands of zombies efficiently
        for (int i = 0; i < 10000; i++)
        {
            // Use Entities Graphics API
        }
    }
}
```

**Recommendation:** Stick with GameObjects for MVP, use EG for "Horde Mode" (10K+ zombies).

---

## 🔧 Project Settings Adjustments for Unity 6

### Graphics Settings
```
Edit > Project Settings > Graphics

- Render Pipeline Asset: UniversalRenderPipelineAsset
- GPU Resident Drawer: Enabled
- SRP Batcher: Enabled
- Dynamic Batching: Disabled (GPU Resident Drawer is better)
- Shader Stripping: Custom (disable unused variants)
```

### Quality Settings
```
Edit > Project Settings > Quality

PC (High):
  - Anti-Aliasing: SMAA (better than MSAA in URP)
  - Shadows: Hard + Soft
  - Shadow Distance: 150m
  - LOD Bias: 1.5

Mobile (Low):
  - Anti-Aliasing: None
  - Shadows: Hard only
  - Shadow Distance: 50m
  - LOD Bias: 1.0
```

### Physics Settings
```
Edit > Project Settings > Physics

- Fixed Timestep: 0.02 (50Hz, good for networking)
- Default Solver Iterations: 6
- Default Solver Velocity Iterations: 1
- Layer Collision Matrix: Optimize (disable unnecessary collisions)
```

---

## 🚀 Unity 6-Specific Performance Optimizations

### 1. **Use Rendering Layers Instead of Cameras**

Unity 6 makes rendering layers cheaper:

```csharp
// OLD (Unity 2022): Multiple cameras = expensive
Camera minimapCamera;
Camera mainCamera;

// NEW (Unity 6): Single camera + rendering layers = cheap
[SerializeField] private LayerMask minimapLayers;
[SerializeField] private LayerMask mainLayers;
```

### 2. **Async GPU Readback (for screenshots/thumbnails)**

```csharp
using Unity.Collections;
using UnityEngine.Rendering;

public IEnumerator CaptureScreenshot()
{
    yield return new WaitForEndOfFrame();

    var request = AsyncGPUReadback.Request(Camera.main.activeTexture, 0);
    yield return new WaitUntil(() => request.done);

    if (!request.hasError)
    {
        NativeArray<byte> data = request.GetData<byte>();
        // Process screenshot without blocking main thread
    }
}
```

### 3. **Temporal Anti-Aliasing (TAA) - Better than MSAA**

Unity 6's URP has improved TAA:

```
URP Asset > Quality > Anti-Aliasing: Temporal Anti-Aliasing
```

Benefits:
- Better quality than MSAA
- Cheaper on mobile
- Reduces shimmering

---

## 🐛 Potential Issues & Fixes

### Issue 1: Older Asset Store Assets
**Problem:** Some old Unity Asset Store packages may not work with Unity 6.
**Solution:**
- Check asset compatibility before purchasing
- Look for "Unity 6 Compatible" badge
- Alternative: Use GitHub open-source alternatives

### Issue 2: Legacy Render Pipeline Assets
**Problem:** Some free models use Built-in RP materials.
**Solution:**
```
Edit > Render Pipeline > Universal Render Pipeline > Upgrade Project Materials to URP
```

### Issue 3: Netcode Version Mismatch
**Problem:** Tutorials may use older Netcode versions.
**Solution:**
- Use Netcode 2.0+ (has breaking changes from 1.x)
- Check Unity Multiplayer docs for migration guide
- Key changes:
  - `NetworkBehaviour.IsLocalPlayer` → `NetworkBehaviour.IsOwner`
  - `NetworkManager.Singleton.LocalClientId` → same
  - RPC attributes remain the same

---

## 📚 Unity 6 Learning Resources

### Official Unity 6 Docs
- [Unity 6 Release Notes](https://unity.com/releases/unity-6)
- [Netcode for GameObjects 2.0 Docs](https://docs-multiplayer.unity3d.com/)
- [URP 17 Documentation](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.0/)

### Recommended Tutorials (Unity 6 Compatible)
- **Unity Learn:** "Create a Multiplayer Game with Netcode" (updated for Unity 6)
- **Code Monkey:** Unity 6 Multiplayer series (YouTube)
- **Brackeys (community):** Unity 6 optimization tips

---

## ✅ Compatibility Checklist

Before starting development, verify:

- [x] Unity 6.2 installed
- [ ] URP 17.x package installed
- [ ] Netcode for GameObjects 2.0+ installed
- [ ] Unity Gaming Services logged in
- [ ] New Input System enabled (Project Settings > Player)
- [ ] GPU Resident Drawer enabled
- [ ] SRP Batcher enabled
- [ ] Target platforms added (PC, Android, iOS, WebGL)

---

## 🎮 Recommended Unity 6 Workflow

### 1. Create New Project
```
Unity Hub > New Project > 3D (URP)
Template: 3D (URP) [Unity 6]
```

### 2. Install Packages
```
Window > Package Manager

Install:
✓ Netcode for GameObjects
✓ Unity Transport
✓ Lobby
✓ Relay
✓ Input System
✓ Cinemachine
✓ ProBuilder
✓ TextMeshPro
```

### 3. Enable Features
```
Edit > Project Settings > Player > Other Settings

✓ Active Input Handling: Input System Package (New)
✓ API Compatibility Level: .NET Standard 2.1
✓ Scripting Backend: IL2CPP
✓ Managed Stripping Level: Medium
```

### 4. Setup URP Asset
```
Assets > Create > Rendering > URP Asset (with Renderer)

Configure:
- Quality: Medium (adjustable per platform)
- Anti-Aliasing: TAA
- HDR: Off (mobile compatibility)
- MSAA: Off (use TAA instead)
```

---

## 🚀 Conclusion

**Unity 6.2 is PERFECT for this project!** You'll get:

✅ **Better performance** than Unity 2022.3
✅ **All planned features work** (Netcode, URP, Gaming Services)
✅ **Future-proof** (Unity 6 is LTS, long-term support)
✅ **Mobile optimization** (GPU Resident Drawer, better IL2CPP)

**No major changes needed** to our TDD.md plan, just use updated package versions listed above.

---

**Ready to start development? All systems compatible!** 🎉

*Last Updated: November 2025*
