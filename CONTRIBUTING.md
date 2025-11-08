# Contributing to Dead Frontier: Outbreak

Thank you for your interest in contributing! This document provides guidelines for contributing to the project.

---

## 🚀 Getting Started

### Prerequisites
- Unity 6.2 (6000.0.x) or newer
- Git + Git LFS
- C# knowledge (intermediate level)
- Familiarity with Unity Editor

### Setup Development Environment

1. **Clone the repository:**
```bash
git clone https://github.com/cinderspire/Multi-platform-zombie-multiplayer-game.git
cd Multi-platform-zombie-multiplayer-game
```

2. **Install Git LFS:**
```bash
git lfs install
git lfs pull
```

3. **Open in Unity:**
- Open Unity Hub
- Click "Add" > Select project folder
- Open project (Unity 6.2 required)

4. **Install packages:**
- Unity will auto-install packages from `Packages/manifest.json`
- If missing, manually install via Package Manager (see `Docs/TDD.md`)

5. **Download free assets:**
- See `Docs/RESOURCES.md` for list
- Download zombies, weapons, audio
- Place in appropriate folders (see `Docs/PROJECT_STRUCTURE.md`)

---

## 📋 Development Workflow

### Branch Strategy

- **main:** Production-ready code only
- **develop:** Main development branch
- **feature/[name]:** New features (e.g., `feature/zombie-ai`)
- **bugfix/[name]:** Bug fixes (e.g., `bugfix/player-movement`)
- **hotfix/[name]:** Urgent production fixes

### Creating a Feature

1. **Create branch from develop:**
```bash
git checkout develop
git pull origin develop
git checkout -b feature/your-feature-name
```

2. **Develop your feature:**
- Follow coding standards (see below)
- Test thoroughly
- Update documentation if needed

3. **Commit your changes:**
```bash
git add .
git commit -m "feat: add zombie AI state machine"
```

4. **Push and create PR:**
```bash
git push origin feature/your-feature-name
# Create Pull Request on GitHub to `develop` branch
```

---

## 📝 Commit Message Convention

We follow **Conventional Commits**:

### Format:
```
<type>(<scope>): <subject>

<body>

<footer>
```

### Types:
- **feat:** New feature
- **fix:** Bug fix
- **docs:** Documentation changes
- **style:** Code style (formatting, no logic change)
- **refactor:** Code refactoring
- **perf:** Performance improvement
- **test:** Add/update tests
- **chore:** Maintenance (dependencies, build, etc.)

### Examples:
```bash
# Good commits
git commit -m "feat(player): add sprint functionality"
git commit -m "fix(zombie): resolve pathfinding stuck issue"
git commit -m "docs: update README with setup instructions"
git commit -m "perf(networking): optimize NetworkTransform sync rate"

# Bad commits
git commit -m "fixed stuff"
git commit -m "WIP"
git commit -m "update"
```

---

## 🎨 Coding Standards

### C# Style Guide

Follow **Microsoft C# Coding Conventions** with these additions:

#### Naming:
```csharp
// Classes, Structs, Enums: PascalCase
public class PlayerController { }
public struct DamageInfo { }
public enum WeaponType { Pistol, Rifle }

// Methods, Properties: PascalCase
public void TakeDamage(int amount) { }
public int CurrentHealth { get; set; }

// Private fields: camelCase
private int currentAmmo;
private float fireRate;

// Serialized fields: camelCase with [SerializeField]
[SerializeField] private GameObject bulletPrefab;

// Constants: UPPER_SNAKE_CASE
private const float MAX_SPEED = 10f;
```

#### File Organization:
```csharp
// 1. Usings
using UnityEngine;
using Unity.Netcode;

// 2. Namespace (optional, for larger projects)
namespace DeadFrontier.Player
{
    // 3. Class
    public class PlayerController : NetworkBehaviour
    {
        // 4. Serialized Fields
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;

        // 5. Private Fields
        private CharacterController controller;
        private Vector3 velocity;

        // 6. Properties
        public bool IsMoving { get; private set; }

        // 7. Unity Methods
        private void Awake() { }
        private void Start() { }
        private void Update() { }

        // 8. Public Methods
        public void Move(Vector3 direction) { }

        // 9. Private Methods
        private void ApplyGravity() { }
    }
}
```

#### Comments:
```csharp
// Use XML comments for public APIs
/// <summary>
/// Applies damage to the player.
/// </summary>
/// <param name="amount">Damage amount (0-100)</param>
public void TakeDamage(int amount)
{
    // Only comment WHY, not WHAT
    // Clamp damage to prevent overflow
    amount = Mathf.Clamp(amount, 0, 100);
}
```

### Unity Best Practices

#### Performance:
- Use object pooling for frequently spawned objects
- Cache component references in `Awake()`/`Start()`
- Avoid `GetComponent()` in `Update()`
- Use `LayerMask` to filter raycasts

```csharp
// BAD
void Update()
{
    GetComponent<Rigidbody>().AddForce(Vector3.up);
}

// GOOD
private Rigidbody rb;

void Awake()
{
    rb = GetComponent<Rigidbody>();
}

void Update()
{
    rb.AddForce(Vector3.up);
}
```

#### Networking:
- Always validate on server (never trust client)
- Use `IsServer`, `IsClient`, `IsOwner` checks
- Minimize data sent over network (compress when possible)

```csharp
[ServerRpc]
private void DealDamageServerRpc(ulong targetId, int damage)
{
    // Server validation
    if (damage > GetMaxDamage())
    {
        Debug.LogWarning("Client sent invalid damage!");
        return;
    }

    // Apply damage
    ApplyDamage(targetId, damage);
}
```

---

## 🧪 Testing

### Before Submitting PR:

- [ ] Code compiles without errors
- [ ] No new warnings introduced
- [ ] Feature works as intended (manual testing)
- [ ] No performance regressions (profile if heavy feature)
- [ ] Works on all target platforms (PC, Android, WebGL)
- [ ] Networking tested (if multiplayer feature)
- [ ] Documentation updated (if public API changed)

### Manual Testing Checklist:
```
1. Enter Play Mode
2. Test feature in isolation
3. Test feature in context (full game loop)
4. Test edge cases (null values, extreme inputs)
5. Test on low-end device (if performance-critical)
6. Test in multiplayer (if networked)
```

---

## 📚 Documentation

### When to Update Docs:

- **GDD.md:** Game design changes (new mechanics, balance)
- **TDD.md:** Architecture changes (new systems, patterns)
- **ROADMAP.md:** Timeline changes (delays, re-prioritization)
- **TODO.md:** Task progress (mark completed, add new tasks)
- **README.md:** Setup instructions, project overview

### Code Documentation:
- Public classes/methods need XML comments
- Complex algorithms need explanation comments
- Magic numbers need constants with comments

---

## 🐛 Reporting Bugs

### Bug Report Template:

```markdown
**Bug Description:**
[Clear, concise description]

**Steps to Reproduce:**
1. Start game
2. Click X button
3. See error

**Expected Behavior:**
[What should happen]

**Actual Behavior:**
[What actually happens]

**Screenshots/Logs:**
[If applicable]

**Environment:**
- Unity Version: 6.2.x
- Platform: PC/Android/WebGL
- Build: Debug/Release

**Additional Context:**
[Any other relevant info]
```

---

## 💡 Feature Requests

### Feature Request Template:

```markdown
**Feature Description:**
[Clear, concise description]

**Problem it Solves:**
[Why is this needed?]

**Proposed Solution:**
[How would it work?]

**Alternatives Considered:**
[Other ways to solve this?]

**Priority:**
[Low/Medium/High/Critical]

**Additional Context:**
[Mockups, references, etc.]
```

---

## 🔍 Code Review Guidelines

### For Reviewers:

- **Be constructive:** Focus on improvement, not criticism
- **Be specific:** Point to exact lines, suggest alternatives
- **Be timely:** Review within 24-48 hours if possible
- **Check:**
  - Code quality (readable, maintainable)
  - Performance (no obvious bottlenecks)
  - Security (no exploits, validated inputs)
  - Networking (server authoritative, synced correctly)

### For Authors:

- **Be responsive:** Address feedback quickly
- **Be open:** Consider suggestions, don't take personally
- **Explain:** If disagreement, provide reasoning
- **Test:** Verify suggested changes work

---

## 🎯 Priority Labels

When creating issues/PRs, use these labels:

- **Priority: Critical** - Blocks development, must fix immediately
- **Priority: High** - Important for milestone, fix soon
- **Priority: Medium** - Nice to have, can wait
- **Priority: Low** - Future consideration

- **Type: Bug** - Something broken
- **Type: Feature** - New functionality
- **Type: Enhancement** - Improve existing feature
- **Type: Documentation** - Docs only

---

## 🚀 Release Process

### Version Numbering:
- **MAJOR.MINOR.PATCH** (e.g., 1.0.0)
- **MAJOR:** Breaking changes, major features
- **MINOR:** New features, backward compatible
- **PATCH:** Bug fixes, small improvements

### Release Checklist:
- [ ] All tests pass
- [ ] No critical bugs
- [ ] Performance benchmarks met
- [ ] Changelog updated
- [ ] Version number bumped
- [ ] Tagged release in Git
- [ ] Build for all platforms
- [ ] Upload to distribution (Steam, Google Play, etc.)

---

## 📞 Communication

### Where to Ask Questions:

- **Technical questions:** GitHub Discussions
- **Bug reports:** GitHub Issues
- **Feature requests:** GitHub Issues
- **General chat:** Discord (if available)

### Response Times:

- **Critical bugs:** < 24 hours
- **High priority:** < 48 hours
- **Medium/Low priority:** < 1 week

---

## 🙏 Thank You!

Your contributions make this project better! Whether it's:
- Reporting bugs
- Suggesting features
- Writing code
- Improving documentation
- Testing and feedback

**Every contribution matters!** ❤️

---

## 📜 License

By contributing, you agree that your contributions will be licensed under the same license as the project (MIT for code, CC-BY-4.0 for assets).

---

**Questions?** Open a GitHub Discussion or contact the maintainers!

*Last Updated: November 2025*
