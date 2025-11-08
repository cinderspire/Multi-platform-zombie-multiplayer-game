# Dead Frontier: Outbreak
## Cross-Platform Zombie Multiplayer Extraction Shooter

**Status:** 🚧 In Development (Planning Phase)
**Target Platforms:** PC (Steam/WebGL) | Mobile (Android/iOS) | Console (Future)
**Engine:** Unity 2022.3+ LTS
**Networking:** Netcode for GameObjects
**Target Release:** Q4 2025

---

## 🎮 Game Overview

Dead Frontier: Outbreak is a free-to-play, cross-platform PvPvE extraction shooter set in a zombie-infested world. Players drop into dangerous zones to scavenge loot, survive against intelligent zombie hordes, compete with other players, and extract with their rewards before time runs out.

### Core Pillars
1. **High-Stakes Extraction** - Risk vs. reward gameplay with permanent loot loss on death
2. **Smart Zombie AI** - Dynamic, behavior-driven enemies that hunt in hordes
3. **Cross-Platform First** - Seamless play across PC, mobile, and web
4. **Fair Monetization** - Cosmetic-only purchases, no pay-to-win
5. **Optimized Performance** - Runs on mid-range devices (2019+)

---

## 🎯 2025 Market Validation

### Trending Elements (Validated Nov 2025)
✅ **Extraction Shooters** - Genre exploding in 2025 (The Midnight Walkers, Marathon, Arc Raiders)
✅ **Zombie Comeback** - 2025 declared "comeback year for zombie games" (XDA Developers)
✅ **Cross-Platform Demand** - 72% of players want cross-platform support
✅ **F2P Model** - 85% of gaming revenue from free-to-play titles
✅ **Multiplayer Focus** - 60%+ of zombie games are multiplayer-focused

### Competitive Landscape
- **Back 4 Blood** - Co-op only, no extraction mechanics
- **World War Z** - PvE focused, limited PvP
- **The Midnight Walkers** - Direct competitor (Q2 2025, extraction + zombies)
- **7 Days to Die** - Survival focus, slower gameplay

**Our Edge:** First mobile-optimized zombie extraction shooter with cross-platform support

---

## 🛠️ Technology Stack

### Core Engine
- **Unity 2022.3 LTS** - Stable, cross-platform, massive asset ecosystem
- **Universal Render Pipeline (URP)** - Mobile-optimized graphics
- **IL2CPP Backend** - Better performance and security

### Networking
- **Netcode for GameObjects (NGO)** - Unity's official solution, production-ready
- **Unity Gaming Services** - Lobby, Matchmaking, Relay (built-in NAT traversal)
- **Server Authoritative Model** - Anti-cheat security

### Build Targets
1. **PC** - Windows/Mac/Linux (Steam distribution)
2. **WebGL** - Instant browser play
3. **Mobile** - Android (API 24+), iOS (13+)
4. **Console** - PS5/Xbox Series (Phase 2)

---

## 📦 Asset Strategy (Open-Source First)

### 3D Models (FREE)
- **Sketchfab** - Rigged zombies with idle/walk/attack animations (<10k verts)
- **Mixamo** - Character animations (Adobe, free)
- **CGTrader** - 2,424 free zombie models (various styles)
- **TurboSquid** - Free rigged models (royalty-free license)

### Unity Asset Store
- **Multiplayer (STP) Survival Template PRO** (€45.99) - Updated May 2025
- **Zombie Wave Survival COOP** - Wave system with AI
- **ProBuilder** (FREE) - Level design tool

### Audio
- **Freesound.org** - SFX library
- **Incompetech** - Royalty-free music
- **Unity Audio Mixer** - Dynamic sound management

### GitHub Resources
- Unity Boss Room Sample (Netcode reference)
- Open-source AI behavior trees
- Community UI frameworks

---

## 🎲 Core Gameplay Loop

```
Match Start (60 seconds)
    ↓
Drop into Map (8 players)
    ↓
Scavenge & Loot (weapons, ammo, valuables)
    ↓
Survive Zombies + Fight Players
    ↓
Reach Extraction Point (10 min timer)
    ↓
Escape with Loot OR Lose Everything
    ↓
Upgrade Loadout & Repeat
```

### Game Modes
1. **Solo Extraction** - 8 players, every man for themselves
2. **Duo Extraction** - 4 teams of 2
3. **Squad Extraction** - 2 teams of 4 (future)
4. **Horde Defense** - Co-op wave survival (casual mode)

---

## 🧟 Zombie AI System

### Intelligence Features
- **Sensory System** - Vision (raycast), Hearing (range-based), Memory
- **State Machine** - Idle → Patrol → Chase → Attack → Flee → Death
- **Horde Behavior** - Boids/flocking algorithm for group movement
- **Dynamic Spawning** - Wave-based + procedural ambush points
- **Zombie Types**:
  - Runner (fast, low HP)
  - Tank (slow, high HP)
  - Exploder (proximity bomb)
  - Screamer (calls horde)

---

## 💰 Monetization (Fair F2P)

### Revenue Streams
1. **Battle Pass** ($9.99/season, 3 months)
   - Free track: Basic rewards
   - Premium track: Exclusive skins, XP boosts

2. **Cosmetics Store**
   - Weapon skins
   - Character outfits
   - Emotes & sprays

3. **Convenience Items** (Optional)
   - Extra loadout slots
   - XP boosters (2x for 1 hour)

4. **Rewarded Ads** (Mobile only)
   - Watch ad for bonus loot roll

**STRICT RULE:** NO pay-to-win. No purchasable weapons, armor, or gameplay advantages.

---

## 🚀 Development Phases

### Phase 1: MVP (Months 1-3)
- ✅ Core movement & shooting
- ✅ Basic zombie AI (1 type)
- ✅ Multiplayer (8 players)
- ✅ 1 map (small urban area)
- ✅ Extraction mechanics
- ✅ Simple UI/HUD

### Phase 2: Content Expansion (Months 4-6)
- ➕ 3 zombie types
- ➕ 5 weapons
- ➕ 2 maps
- ➕ Battle Pass system
- ➕ Mobile optimization
- ➕ Cosmetic store

### Phase 3: Polish & Launch (Months 7-9)
- 🎨 Visual effects & animations
- 🎵 Audio implementation
- 🐛 Bug fixes & balancing
- 📊 Analytics integration
- 🚀 Soft launch (region-locked)
- 🌍 Global release

### Phase 4: Post-Launch (Ongoing)
- 🔄 Seasonal content
- 🗺️ New maps every 2 months
- 🧟 New zombie types
- ⚔️ Community events
- 🏆 Ranked mode

---

## 📊 Success Metrics

### Technical KPIs
- 60 FPS on mid-range devices (2019+)
- <100ms latency in same region
- <1% crash rate
- <500MB initial download (mobile)

### Business KPIs
- 100K players in first month
- 30% Day 7 retention
- 5% conversion to paying users
- $5 ARPU (Average Revenue Per User)

---

## 👥 Team Structure

### Solo/Small Team Roles
1. **Developer (You)** - Programming, Unity implementation
2. **AI Tools** - Asset creation, concept art (Midjourney, DALL-E)
3. **Community** - Testing, feedback, content ideas
4. **Outsource** - 3D modeling (Fiverr), audio (contractors)

---

## 📚 Documentation Structure

```
/Docs
├── GDD.md                    # Game Design Document
├── TDD.md                    # Technical Design Document
├── ART_STYLE_GUIDE.md        # Visual direction
├── AUDIO_BIBLE.md            # Sound design guide
├── MONETIZATION_STRATEGY.md  # F2P economics
├── MARKETING_PLAN.md         # Pre/post launch strategy
└── ROADMAP.md                # Development timeline
```

---

## 🔗 Resources

### Unity Learning
- [Netcode for GameObjects Docs](https://docs-multiplayer.unity3d.com/netcode/current/about/)
- [Boss Room Sample Project](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop)
- [Unity Multiplayer Best Practices](https://blog.unity.com/games/build-a-production-ready-multiplayer-game-with-netcode-for-gameobjects)

### Game Design
- [Extraction Shooter Design Patterns](https://www.thegamer.com/extraction-shooters-marathon-titanfall-helldivers-2-arc-raiders-mycopunk/)
- [Zombie AI Design](https://gamedevacademy.org/how-to-create-a-simple-zombie-ai-in-unity/)

### Community
- Discord: [Coming Soon]
- Subreddit: r/DeadFrontierOutbreak
- Twitter: @DFOutbreak

---

## 🏁 Getting Started (For Developers)

See [CONTRIBUTING.md](CONTRIBUTING.md) for development setup and guidelines.

### Quick Start
```bash
# Clone repository
git clone https://github.com/cinderspire/Multi-platform-zombie-multiplayer-game.git

# Open in Unity Hub (2022.3 LTS required)
# Install required packages:
# - Netcode for GameObjects
# - Unity Gaming Services
# - ProBuilder
```

---

**License:** MIT (Code) | CC-BY-4.0 (Assets)
**Contact:** [Your Email/Discord]

---

*Last Updated: November 2025*
