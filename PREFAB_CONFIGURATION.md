# 🎨 Prefab Configuration Guide

## Complete prefab setup templates for all game entities.

---

## 👤 PLAYER PREFAB

### **Hierarchy:**
```
Player
├── Model (or Capsule for testing)
├── Camera
│   └── (Camera component + Audio Listener)
├── WeaponHolder
│   └── (Position for weapon models)
└── UI
    └── PlayerCanvas (World Space Canvas for player-specific UI)
```

### **Components on Player:**
```
✓ NetworkObject
  - Ownership: Client
  - Synchronize Transform: true
  - Destroy With Scene: true

✓ CharacterController
  - Height: 2
  - Radius: 0.5
  - Step Offset: 0.3
  - Slope Limit: 45

✓ PlayerController (Script)
  - Walk Speed: 5
  - Sprint Speed: 8
  - Crouch Speed: 2.5
  - Jump Height: 2
  - Gravity: -20

✓ AnimationController (Script)
  - Animator: (Assign if using animated model)

✓ CapsuleCollider
  - Height: 2
  - Radius: 0.5
  - Is Trigger: false

✓ Rigidbody
  - Use Gravity: false (CharacterController handles gravity)
  - Is Kinematic: true
  - Constraints: Freeze Rotation X, Y, Z
```

### **Components on Camera (child):**
```
✓ Camera
  - Field of View: 90
  - Near Clipping: 0.1
  - Far Clipping: 1000

✓ Audio Listener

✓ CameraController (Script)
  - Default Mode: Third Person
  - Mouse Sensitivity: 2
  - First Person Offset: (0, 1.6, 0)
  - Third Person Offset: (0, 1.8, -3)
  - Third Person Distance: 3
  - Collision Mask: Everything except Player
```

### **Tags & Layers:**
- Tag: `Player`
- Layer: `Player`

---

## 🧟 ZOMBIE PREFABS

### **Zombie_Walker Prefab:**

```
Zombie_Walker
├── Model (3D model or capsule)
├── Head (for headshot detection)
└── Hitboxes (for damage detection)
```

### **Components:**
```
✓ NetworkObject
  - Ownership: Server
  - Synchronize Transform: true
  - Destroy With Scene: true

✓ NavMeshAgent
  - Speed: 2 (Walker)
  - Angular Speed: 120
  - Acceleration: 8
  - Stopping Distance: 1.5
  - Auto Braking: true
  - Height: 2
  - Base Offset: 0

✓ ZombieAI (Script)
  - Zombie Type: Walker
  - Detection Range: 15
  - Attack Range: 2
  - Attack Damage: 10
  - Attack Cooldown: 1.5
  - Wander Radius: 20

✓ CapsuleCollider
  - Height: 2
  - Radius: 0.5
  - Is Trigger: false

✓ Animator (if using animations)
```

### **Head Child Object:**
```
✓ SphereCollider
  - Radius: 0.2
  - Is Trigger: false
  - Tag: "Head" (for headshot detection)
```

### **Other Zombie Types:**

**Zombie_Runner:**
- Speed: 6
- Detection Range: 20
- Attack Damage: 8

**Zombie_Tank:**
- Speed: 1.5
- Detection Range: 12
- Attack Damage: 25
- Health: 300 (modify in HealthSystem init)

**Zombie_Spitter:**
- Speed: 2.5
- Detection Range: 25
- Attack Range: 10 (ranged)
- Attack Damage: 15

**Zombie_Exploder:**
- Speed: 3
- Detection Range: 18
- Attack Damage: 50 (AoE on death)

### **Tags & Layers:**
- Tag: `Enemy`
- Layer: `Enemy`

---

## 🎯 INTERACTABLE OBJECTS

### **Door Prefab:**

```
Door
├── DoorFrame
├── DoorModel
└── InteractionPrompt (UI)
```

### **Components:**
```
✓ NetworkObject
  - Ownership: Server

✓ Door (Script from InteractionSystem)
  - Door ID: "door_01"
  - Is Locked: false

✓ BoxCollider
  - Size: (1, 2, 0.1)
  - Is Trigger: false
  - Layer: Interactable

✓ Animator (for door open/close animation)
```

---

## 🎁 PICKUP ITEMS

### **Item Prefab Template:**

```
Item_Medkit
├── Model
└── RotationPivot (for spinning effect)
```

### **Components:**
```
✓ NetworkObject
  - Ownership: Server
  - Destroy With Scene: false

✓ SphereCollider
  - Radius: 1
  - Is Trigger: true

✓ ItemPickup (Script)
  - Item ID: "medkit"
  - Item Type: Health
  - Value: 50

✓ Rigidbody
  - Use Gravity: false
  - Is Kinematic: true
```

---

## 🔫 WEAPON PREFABS

### **Weapon Model Setup:**

```
Weapon_Rifle
├── Model
├── MuzzleFlash (Particle System)
├── MuzzlePoint (for bullet spawn)
└── ImpactEffects (Particle System)
```

### **Components:**
```
✓ WeaponController (Script)
  - Weapon ID: "rifle_m4"
  - Damage: 25
  - Fire Rate: 0.1
  - Magazine Size: 30
  - Reserve Ammo: 120
  - Range: 100
  - Spread: 0.02
  - Reload Time: 2.5
```

---

## 🏗️ BUILDING PREFABS

### **Buildable Structure:**

```
Structure_Wall
├── Model
├── SnapPoints (for building placement)
└── HealthDisplay (UI)
```

### **Components:**
```
✓ NetworkObject
  - Ownership: Server

✓ BuildingStructure (Script from BaseBuildingSystem)
  - Structure Type: Wall
  - Max Health: 1000
  - Build Cost: 50 Wood

✓ BoxCollider
  - Size: depends on structure

✓ MeshRenderer
```

---

## 🎨 EFFECT PREFABS

### **Particle Effects:**

**Muzzle Flash:**
- Duration: 0.1s
- Start Lifetime: 0.1
- Emission: 50 particles burst

**Blood Splatter:**
- Duration: 0.5s
- Start Lifetime: 1
- Emission: 20 particles

**Impact Effect:**
- Duration: 0.2s
- Emission: 10 particles burst

---

## 📦 NETWORK PREFAB LIST

Add these to NetworkManager → Network Prefabs List:

1. ✓ Player
2. ✓ Zombie_Walker
3. ✓ Zombie_Runner
4. ✓ Zombie_Tank
5. ✓ Zombie_Spitter
6. ✓ Zombie_Exploder
7. ✓ All Item Pickups
8. ✓ All Weapon Models
9. ✓ All Building Structures
10. ✓ Projectiles (if using)
11. ✓ Effect Prefabs (if networked)

---

## 🎯 LAYERMASK SETUP

**Project Settings → Tags and Layers:**

### **Layers:**
```
0:  Default
1:  TransparentFX
2:  Ignore Raycast
3:  (unused)
4:  Water
5:  UI
6:  (unused)
7:  (unused)
8:  Player
9:  Enemy
10: Ground
11: Interactable
12: Projectile
13: Building
14: ItemPickup
```

### **Tags:**
```
- Player
- Enemy
- Head (for headshots)
- Interactable
- Pickup
- SpawnPoint
- Building
```

---

## ⚙️ COLLISION MATRIX

**Edit → Project Settings → Physics → Layer Collision Matrix**

Configure which layers can collide:

```
           Player  Enemy  Ground  Projectile  Building
Player       ✗      ✓      ✓         ✗          ✓
Enemy        ✓      ✗      ✓         ✓          ✓
Ground       ✓      ✓      ✓         ✓          ✓
Projectile   ✗      ✓      ✓         ✗          ✓
Building     ✓      ✓      ✓         ✓          ✓
```

---

## ✅ PREFAB CHECKLIST

Before marking prefab as complete:

- [ ] NetworkObject component added
- [ ] Ownership set correctly (Client for players, Server for AI)
- [ ] All required scripts attached
- [ ] Colliders configured
- [ ] Layers and tags assigned
- [ ] Added to NetworkManager's Network Prefabs list
- [ ] Tested spawn/despawn
- [ ] Tested network synchronization

---

## 🚀 QUICK SETUP

**For rapid prototyping, use simple capsules:**

1. Create Capsule → Add NetworkObject → Add scripts
2. Test gameplay mechanics
3. Replace with actual 3D models later

This keeps development fast and flexible!
