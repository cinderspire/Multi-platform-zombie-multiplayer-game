# Development Roadmap
## Dead Frontier: Outbreak

**Project Start:** November 2025
**Target MVP:** February 2026 (3 months)
**Target Release:** Q4 2026 (12 months)

---

## 📊 Overview

This roadmap breaks down development into **4 major phases** over 12 months:

1. **Phase 1: MVP (Months 1-3)** - Core gameplay loop
2. **Phase 2: Content Expansion (Months 4-6)** - More content, polish
3. **Phase 3: Beta & Polish (Months 7-9)** - Testing, optimization, marketing
4. **Phase 4: Launch & Post-Launch (Months 10-12)** - Release, live ops

---

## 🎯 Phase 1: MVP Development (Months 1-3)

**Goal:** Playable extraction shooter with basic zombies, multiplayer, and one map.

### Month 1: Foundation

#### Week 1: Project Setup
**Priority: Critical**

- [ ] **Day 1-2: Unity Project Setup**
  - Create new Unity 6.2 URP project
  - Install required packages (Netcode, URP, Input System)
  - Set up Git repository + Git LFS
  - Configure project settings (physics, quality, player)
  - Create folder structure

- [ ] **Day 3-4: Core Architecture**
  - Create singleton managers (GameManager, NetworkManager, AudioManager, PoolManager)
  - Set up ScriptableObject architecture
  - Create base classes (NetworkBehaviour, State Machine)
  - Set up event system (GameEvent ScriptableObjects)

- [ ] **Day 5-7: Player Controller (Local)**
  - FPS movement (WASD, sprint, crouch, jump)
  - Camera controller (mouse look, recoil)
  - Input System setup (PC + Mobile)
  - Animation controller (idle, walk, run, jump)
  - Testing in test scene

**Deliverable:** Player can move around a test scene smoothly.

---

#### Week 2: Combat System
**Priority: Critical**

- [ ] **Day 1-3: Weapon System**
  - WeaponData ScriptableObject (stats, sounds, VFX)
  - Weapon controller (fire, reload, ammo)
  - Hitscan raycast shooting
  - Recoil system (camera shake, pattern)
  - Weapon switching (1-3 keys)
  - Create 2 test weapons (Pistol, Rifle)

- [ ] **Day 4-5: Damage System**
  - IDamageable interface
  - Health component
  - Damage feedback (hit markers, blood effects)
  - Death/respawn logic

- [ ] **Day 6-7: Polish**
  - Muzzle flash VFX
  - Bullet tracers
  - Impact effects (sparks, dust)
  - Audio (gunshots, reload, empty click)
  - Object pooling for VFX/bullets

**Deliverable:** Player can shoot targets with visual/audio feedback.

---

#### Week 3: Zombie AI (Basic)
**Priority: Critical**

- [ ] **Day 1-2: Zombie Foundation**
  - ZombieAI MonoBehaviour
  - ZombieConfig ScriptableObject
  - Zombie health system
  - NavMesh agent movement
  - Import free zombie model (Sketchfab)
  - Mixamo animations (idle, walk, run, attack, death)

- [ ] **Day 3-4: AI State Machine**
  - Idle state (wander, look around)
  - Chase state (follow player)
  - Attack state (melee damage)
  - Death state (ragdoll, loot drop)
  - State transition logic

- [ ] **Day 5-6: Sensor System**
  - Vision sensor (raycast, FOV check)
  - Hearing sensor (noise detection)
  - Memory system (last known position)
  - Integration with state machine

- [ ] **Day 7: Testing & Tuning**
  - Spawn 10-20 zombies
  - Balance speed, damage, HP
  - Test pathfinding on simple terrain
  - Fix edge cases (stuck zombies, etc.)

**Deliverable:** Zombies can detect player, chase, and attack.

---

#### Week 4: Networking Foundation
**Priority: Critical**

- [ ] **Day 1-2: Netcode Setup**
  - NetworkManager configuration
  - Unity Gaming Services setup (create project)
  - Authentication integration
  - Test client-server connection locally

- [ ] **Day 3-4: Player Networking**
  - NetworkBehaviour for PlayerController
  - NetworkTransform (position sync)
  - NetworkVariable (health, ammo)
  - Client-side prediction (movement)
  - Server reconciliation

- [ ] **Day 5-6: Weapon Networking**
  - ServerRpc for shooting
  - ClientRpc for VFX/audio
  - Hit detection (server authoritative)
  - Damage synchronization

- [ ] **Day 7: Zombie Networking**
  - Zombies spawn on server only
  - Sync zombie state (NetworkVariable)
  - Sync animations (NetworkAnimator)
  - Test with 2 clients + zombies

**Deliverable:** 2 players can join, shoot each other, and fight zombies together.

---

### Month 2: Core Systems

#### Week 5: Inventory & Loot
**Priority: High**

- [ ] **Day 1-2: Inventory System**
  - InventorySlot class
  - Inventory UI (grid layout)
  - Add/remove items
  - Item tooltips
  - Equipment slots (primary, secondary, melee)

- [ ] **Day 3-4: Loot System**
  - ItemData ScriptableObject
  - LootTable ScriptableObject (drop chances)
  - Loot spawning in world
  - Pickup interaction (E key)
  - Drop item functionality

- [ ] **Day 5-6: Loot Networking**
  - Server authoritative loot spawns
  - Pickup synchronization
  - Inventory sync across clients
  - Stash system (persistent storage)

- [ ] **Day 7: Polish**
  - Loot rarity colors (white, green, blue, purple, gold)
  - Pickup animations
  - Audio feedback (pickup sound)
  - Inventory management (drag & drop)

**Deliverable:** Players can find, pickup, store, and use loot.

---

#### Week 6: Extraction Mechanics
**Priority: High**

- [ ] **Day 1-2: Extraction Points**
  - Extraction zone triggers
  - Extraction timer (30 seconds)
  - Visual feedback (helicopter model, spotlight)
  - Audio cues (helicopter sounds, countdown)

- [ ] **Day 3-4: Match Flow**
  - Game state manager (Lobby → Match → Extraction → Results)
  - Match timer (10 minutes)
  - Extraction activation (last 2 minutes)
  - Victory/defeat conditions

- [ ] **Day 5-6: Extraction Networking**
  - Sync extraction state
  - Handle multiple players extracting
  - Loot persistence on extraction
  - Death = lose everything logic

- [ ] **Day 7: Testing**
  - Full match playthrough
  - Balance extraction timer
  - Test edge cases (die during extraction, etc.)

**Deliverable:** Full extraction shooter loop functional.

---

#### Week 7: MVP Map Design
**Priority: High**

- [ ] **Day 1-3: Map Layout (ProBuilder)**
  - "Downtown Ruins" concept
  - Blockout with ProBuilder (500m x 500m)
  - Key locations: Police Station, Hospital, Mall, Subway
  - Spawn points (player, zombie, loot)
  - Extraction points (3 locations)

- [ ] **Day 4-5: NavMesh Baking**
  - Mark walkable areas
  - Add off-mesh links (jumps, climbs)
  - Test zombie pathfinding
  - Fix stuck points

- [ ] **Day 6-7: Visual Pass (Basic)**
  - Import free building assets (CGTrader, Unity Asset Store)
  - Apply textures
  - Add props (cars, debris, furniture)
  - Lighting setup (baked + realtime)
  - Fog for atmosphere

**Deliverable:** Playable urban map with atmosphere.

---

#### Week 8: UI & UX
**Priority: Medium**

- [ ] **Day 1-2: HUD**
  - Health bar
  - Ammo counter
  - Minimap
  - Crosshair
  - Kill feed
  - Extraction timer UI

- [ ] **Day 3-4: Menus**
  - Main menu (Play, Settings, Quit)
  - Lobby UI (player list, ready status)
  - Death screen (stats, respawn/exit)
  - Victory screen (loot summary, XP)

- [ ] **Day 5-6: Settings Menu**
  - Graphics quality (Low/Medium/High)
  - Audio sliders (Master, Music, SFX)
  - Controls rebinding
  - Sensitivity slider

- [ ] **Day 7: Polish**
  - UI animations (DOTween)
  - Button hover effects
  - Consistent style (dark, horror theme)
  - Mobile UI adjustments

**Deliverable:** Complete UI for all game states.

---

### Month 3: MVP Polish & Testing

#### Week 9: Audio Implementation
**Priority: Medium**

- [ ] **Day 1-2: SFX Integration**
  - Weapon sounds (fire, reload, empty, switch)
  - Zombie sounds (idle, chase, attack, death)
  - Player sounds (footsteps, hurt, death)
  - Environment sounds (wind, distant sirens)

- [ ] **Day 3-4: Music System**
  - Main menu music
  - Lobby music
  - In-game dynamic music (calm vs intense)
  - Victory/defeat stingers

- [ ] **Day 5-6: Audio Manager**
  - 3D spatial audio for zombies/players
  - Audio pooling (20 AudioSources)
  - Volume mixing (music ducks during action)
  - Noise registration for zombie AI

- [ ] **Day 7: Testing**
  - Audio balancing
  - Fix audio bugs (missing sounds, cutoff)
  - Test 3D audio positioning

**Deliverable:** Immersive audio experience.

---

#### Week 10: Optimization (Mobile Focus)
**Priority: High**

- [ ] **Day 1-2: Graphics Optimization**
  - LOD groups for zombies, buildings
  - Occlusion culling baking
  - Texture compression (ASTC for Android)
  - Shader optimization (use URP/Lit only)

- [ ] **Day 3-4: CPU Optimization**
  - Object pooling (zombies, bullets, VFX)
  - Spatial partitioning for zombie AI
  - Update throttling (stagger AI updates)
  - Profiler analysis (remove bottlenecks)

- [ ] **Day 5-6: Memory Optimization**
  - Texture streaming settings
  - Mesh memory reduction
  - Asset bundle preparation (WebGL)
  - Memory profiler analysis

- [ ] **Day 7: Platform Testing**
  - Build for Android (test on mid-range device)
  - Build for iOS (if available)
  - Build for WebGL (test in Chrome)
  - Fix platform-specific bugs

**Deliverable:** 45+ FPS on 2019 mid-range devices.

---

#### Week 11: Matchmaking & Lobbies
**Priority: High**

- [ ] **Day 1-3: Lobby System**
  - Unity Lobby Service integration
  - Create lobby UI
  - Join lobby UI (browse available lobbies)
  - Lobby chat (optional, text only)
  - Ready system (8/8 players ready to start)

- [ ] **Day 4-5: Matchmaking**
  - Quick match (auto-join)
  - Private lobby (invite code)
  - Region selection (NA, EU, ASIA)
  - Party system (invite friends)

- [ ] **Day 6-7: Relay Integration**
  - Unity Relay for NAT traversal
  - Allocate relay on match start
  - Connect all clients via relay
  - Test with players on different networks

**Deliverable:** Smooth matchmaking and lobby experience.

---

#### Week 12: MVP Testing & Bug Fixes
**Priority: Critical**

- [ ] **Day 1-2: Internal Playtesting**
  - Full match playthroughs (at least 10)
  - Document all bugs
  - Prioritize critical bugs (crashes, game-breaking)

- [ ] **Day 3-5: Bug Fixing**
  - Fix critical bugs (crashes, stuck states)
  - Fix high-priority bugs (UI issues, balance)
  - Fix medium bugs (minor visual glitches)
  - Low-priority bugs → backlog

- [ ] **Day 6: Performance Testing**
  - FPS benchmarks on target devices
  - Memory leak testing (long sessions)
  - Network stress testing (8 players + 50 zombies)

- [ ] **Day 7: MVP Build**
  - Build for all platforms (PC, Android, WebGL)
  - Upload to itch.io for closed alpha
  - Prepare feedback survey

**Deliverable:** Stable MVP ready for closed alpha.

---

## 🚀 Phase 2: Content Expansion (Months 4-6)

**Goal:** Add variety, depth, and monetization systems.

### Month 4: Content Creation

#### Week 13-14: New Zombie Types
- [ ] Runner zombie (fast, low HP)
- [ ] Tank zombie (slow, high HP)
- [ ] Exploder zombie (proximity bomb)
- [ ] Screamer zombie (calls horde)
- AI behavior for each type
- Balance testing

#### Week 15-16: More Weapons
- [ ] 5 new weapons (SMG, Shotgun, Sniper, Melee, Grenade)
- [ ] Weapon attachments (scope, suppressor)
- [ ] Weapon upgrade system
- [ ] Balance testing

### Month 5: Systems Expansion

#### Week 17-18: Progression System
- [ ] Player leveling (XP, levels 1-100)
- [ ] Unlock system (cosmetics, loadout slots)
- [ ] Stats tracking (kills, deaths, extractions)
- [ ] Leaderboards (global, friends)

#### Week 19-20: Battle Pass
- [ ] Battle Pass UI
- [ ] Tier progression (50 tiers)
- [ ] Reward unlocking
- [ ] Free vs Premium tracks

### Month 6: Monetization & New Map

#### Week 21-22: Cosmetics Store
- [ ] Store UI (featured, categories)
- [ ] In-app purchase integration (Unity IAP)
- [ ] Weapon skins (5-10 variants)
- [ ] Character outfits (3-5 sets)
- [ ] Purchase flow testing

#### Week 23-24: Map 2 - "Military Base"
- [ ] Map design & blockout
- [ ] Asset placement
- [ ] NavMesh baking
- [ ] Loot distribution
- [ ] Testing & balancing

**Deliverable:** Feature-rich game with monetization.

---

## 🧪 Phase 3: Beta & Polish (Months 7-9)

**Goal:** Public beta, community feedback, optimization, marketing.

### Month 7: Beta Testing

#### Week 25-26: Closed Beta
- [ ] Recruit 100 beta testers
- [ ] Set up Discord community
- [ ] Beta feedback survey
- [ ] Bug reporting system
- [ ] Weekly beta builds

#### Week 27-28: Open Beta
- [ ] Public release on itch.io
- [ ] Marketing push (Reddit, Twitter, TikTok)
- [ ] Streamer outreach (send keys)
- [ ] Community events (tournaments)
- [ ] Analytics tracking (retention, monetization)

### Month 8: Polish & Optimization

#### Week 29-30: Visual Polish
- [ ] VFX improvements (blood, explosions, muzzle flash)
- [ ] Animation polish (blend trees, transitions)
- [ ] Post-processing (color grading, bloom)
- [ ] UI/UX improvements based on feedback

#### Week 31-32: Balance Pass
- [ ] Weapon balance (damage, recoil, fire rate)
- [ ] Zombie balance (HP, speed, spawn rates)
- [ ] Loot economy (drop rates, sell values)
- [ ] Extraction balance (timer, locations)

### Month 9: Final Prep

#### Week 33-34: Marketing Assets
- [ ] Trailer (60 seconds, gameplay highlights)
- [ ] Screenshots (Steam, mobile stores)
- [ ] Website (landing page, FAQ, roadmap)
- [ ] Social media presence (Twitter, Discord)

#### Week 35-36: Store Submissions
- [ ] Steam page (PC)
- [ ] Google Play Store (Android)
- [ ] Apple App Store (iOS)
- [ ] Press kit (media, fact sheet)
- [ ] Influencer kit (keys, talking points)

**Deliverable:** Polished beta, ready for launch.

---

## 🎉 Phase 4: Launch & Post-Launch (Months 10-12)

**Goal:** Successful launch, live operations, seasonal content.

### Month 10: Soft Launch

#### Week 37-38: Regional Soft Launch
- [ ] Release in 1-2 regions (e.g., Canada, New Zealand)
- [ ] Monitor crash reports, reviews
- [ ] Fix critical bugs rapidly
- [ ] Gather data on retention, monetization

#### Week 39-40: Iterate Based on Data
- [ ] Adjust balance based on data
- [ ] Fix top player-reported issues
- [ ] Optimize bottlenecks
- [ ] A/B test monetization pricing

### Month 11: Global Launch

#### Week 41-42: Launch Week
- [ ] Global release (all platforms)
- [ ] Marketing blitz (ads, influencers, PR)
- [ ] Launch event (double XP weekend)
- [ ] Community engagement (Discord events)

#### Week 43-44: Post-Launch Support
- [ ] Hotfixes for critical bugs
- [ ] Daily player support (Discord, email)
- [ ] Monitor server stability
- [ ] First balance patch based on data

### Month 12: Live Operations

#### Week 45-48: Season 1 Content
- [ ] **New Map:** "Forest Outpost"
- [ ] **New Zombies:** 2 types
- [ ] **New Weapons:** 3 weapons
- [ ] **Battle Pass Season 1:** 50 tiers, new cosmetics
- [ ] **Limited-Time Event:** Holiday theme
- [ ] **Ranked Mode:** Leaderboards, skill-based matchmaking

**Deliverable:** Thriving live game with engaged community.

---

## 📈 Key Milestones & Decision Points

### Milestone 1: MVP Complete (Month 3)
**Decision:** Proceed to content expansion or iterate on core loop?
- If retention > 30% D7: Proceed
- If retention < 30% D7: Iterate MVP

### Milestone 2: Beta Launch (Month 7)
**Decision:** Soft launch or delay for more polish?
- If crash rate < 1%: Soft launch
- If crash rate > 1%: Delay, fix crashes

### Milestone 3: Monetization Validation (Month 9)
**Decision:** Launch pricing confirmed or adjust?
- If conversion > 3%: Launch pricing
- If conversion < 3%: A/B test lower prices

### Milestone 4: Global Launch (Month 11)
**Decision:** Scale servers or maintain current capacity?
- If player growth > 50K/week: Scale aggressively
- If player growth < 10K/week: Marketing push

---

## 🎯 Success Metrics (KPIs to Track)

### Development KPIs
- **Velocity:** Story points per sprint (aim for consistent)
- **Bug Density:** Bugs per 1000 lines of code (target < 5)
- **Build Success Rate:** CI/CD success rate (target > 95%)

### Product KPIs
- **Retention:**
  - Day 1: > 60%
  - Day 7: > 30%
  - Day 30: > 10%

- **Engagement:**
  - Session length: 30-45 minutes
  - Sessions per day: 2-3
  - DAU/MAU ratio: > 20%

- **Monetization:**
  - Conversion rate: > 5%
  - ARPU: $5-7/month
  - LTV: $50-70

### Technical KPIs
- **Performance:**
  - FPS: 60 on PC, 45 on mobile
  - Crash rate: < 1%
  - Load time: < 10 seconds

- **Network:**
  - Latency: < 100ms same region
  - Packet loss: < 1%
  - Concurrent users: 10K+ at launch

---

## 🔄 Agile Workflow

### Sprint Structure
- **Sprint Length:** 2 weeks
- **Sprint Planning:** Monday (2 hours)
- **Daily Standup:** 15 minutes (async if solo)
- **Sprint Review:** Friday (1 hour)
- **Sprint Retro:** Friday (30 minutes)

### Task Prioritization (MoSCoW)
- **Must Have:** Critical for MVP
- **Should Have:** Important but not critical
- **Could Have:** Nice to have
- **Won't Have:** Future phases

### Example Sprint Backlog (Week 1)
```
MUST HAVE:
☐ Set up Unity project [8 pts]
☐ Install required packages [3 pts]
☐ Create folder structure [2 pts]

SHOULD HAVE:
☐ Set up Git LFS [2 pts]
☐ Create base managers [5 pts]

COULD HAVE:
☐ Write technical docs [3 pts]
```

---

## 🚨 Risk Management

### Technical Risks
1. **Netcode Complexity**
   - **Mitigation:** Start with Netcode early, use Boss Room sample
   - **Contingency:** Hire networking expert (freelance)

2. **Mobile Performance**
   - **Mitigation:** Profile early and often, target mid-range devices
   - **Contingency:** Reduce scope (fewer zombies, smaller map)

3. **Cross-Platform Input**
   - **Mitigation:** Use Unity Input System from day 1
   - **Contingency:** Separate mobile/PC builds if needed

### Business Risks
1. **Low Retention**
   - **Mitigation:** Playtest early, iterate on core loop
   - **Contingency:** Pivot to co-op PvE (remove PvP)

2. **Poor Monetization**
   - **Mitigation:** Study successful F2P games, fair pricing
   - **Contingency:** Add rewarded ads, Battle Pass discounts

3. **Competition**
   - **Mitigation:** Unique cross-platform + mobile focus
   - **Contingency:** Niche down (mobile-first zombie extraction)

---

## 📅 Timeline Summary

```
Month 1: Foundation (Player, Weapons, Basic AI)
Month 2: Core Systems (Inventory, Extraction, Map)
Month 3: MVP Polish (UI, Audio, Testing)
         └── MILESTONE: MVP Complete

Month 4: Content (New zombies, weapons)
Month 5: Progression (Leveling, Battle Pass)
Month 6: Monetization (Store, IAP, New Map)
         └── MILESTONE: Content Complete

Month 7: Beta Testing (Closed → Open)
Month 8: Polish (Visual, Balance, Optimization)
Month 9: Marketing (Trailer, Store Pages, Influencers)
         └── MILESTONE: Beta Complete

Month 10: Soft Launch (Regional)
Month 11: Global Launch
Month 12: Live Ops (Season 1, Events)
          └── MILESTONE: Launch Success
```

---

## ✅ Definition of Done (DoD)

### For Each Feature:
- [ ] Code written and peer reviewed
- [ ] Unit tests pass (if applicable)
- [ ] Integration tested in-game
- [ ] Works on all target platforms
- [ ] No new critical bugs introduced
- [ ] Performance benchmarks met
- [ ] Documentation updated
- [ ] Merged to main branch

### For Each Sprint:
- [ ] All "Must Have" tasks complete
- [ ] Sprint review completed
- [ ] Demo to stakeholders (if any)
- [ ] Retro notes documented
- [ ] Backlog groomed for next sprint

### For Each Phase:
- [ ] Milestone deliverable met
- [ ] KPIs tracked and reviewed
- [ ] Post-mortem completed
- [ ] Lessons learned documented
- [ ] Marketing assets updated

---

## 🎓 Learning & Iteration

### Weekly Learning Goals
- **Week 1-4:** Netcode mastery
- **Week 5-8:** AI and pathfinding
- **Week 9-12:** UI/UX design
- **Month 4-6:** Monetization and analytics
- **Month 7-9:** Marketing and community management
- **Month 10-12:** Live operations

### Knowledge Resources
- Unity Learn (Netcode tutorials)
- Code Monkey (YouTube)
- GameDev.tv (Udemy courses)
- GDC Vault (postmortems)
- Gamasutra articles

---

## 🏁 Conclusion

This roadmap is **ambitious but achievable** with focus and discipline. Key success factors:

1. **Start simple:** MVP first, features later
2. **Iterate quickly:** 2-week sprints, rapid prototyping
3. **Test early:** Playtesting from Week 4 onwards
4. **Stay lean:** Use free assets, open-source tools
5. **Community first:** Build Discord, engage players
6. **Data-driven:** Track KPIs, make informed decisions

**Remember:** This is a living document. Adjust based on reality, feedback, and data.

---

**Good luck, developer! You've got this!** 🚀

*Next Step: Start Week 1, Day 1 - Unity Project Setup*
