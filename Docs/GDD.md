# Game Design Document (GDD)
## Dead Frontier: Outbreak

**Version:** 1.0
**Last Updated:** November 2025
**Document Owner:** Lead Designer

---

## Table of Contents
1. [High Concept](#high-concept)
2. [Game Pillars](#game-pillars)
3. [Core Gameplay](#core-gameplay)
4. [Player Experience](#player-experience)
5. [Game Mechanics](#game-mechanics)
6. [Zombie AI Design](#zombie-ai-design)
7. [Map Design](#map-design)
8. [Progression Systems](#progression-systems)
9. [Monetization](#monetization)
10. [Platform Considerations](#platform-considerations)

---

## 1. High Concept

**Elevator Pitch:**
"Escape from Tarkov meets Left 4 Dead in a mobile-friendly package. Drop into zombie-infested zones, loot valuable gear, survive against intelligent hordes AND other players, then extract before time runs out. Death means losing everything."

**Target Audience:**
- Primary: 18-35 year old hardcore gamers
- Secondary: Mobile gamers seeking console-quality experience
- Crossover: Battle royale players, extraction shooter fans, zombie game veterans

**Unique Selling Points:**
1. First mobile-optimized zombie extraction shooter
2. Full cross-platform play (PC ↔ Mobile ↔ Web)
3. Smart zombie AI with dynamic horde behavior
4. Fair F2P model (no pay-to-win)
5. Quick matches (10-15 minutes)

---

## 2. Game Pillars

### Pillar 1: High-Stakes Tension
Every decision matters. Do you push deeper for better loot or extract safely with what you have? Every gunshot attracts zombies. Every fight with players risks losing everything.

**Design Mandate:**
- Permanent loot loss on death
- Limited ammo/resources
- Audio cues for danger (horde approaching, player nearby)
- Extraction timer creates urgency

### Pillar 2: Emergent Combat
No two encounters feel the same. Zombie hordes force players into compromises. Player vs player battles attract zombie attention. Environment is a weapon.

**Design Mandate:**
- Dynamic zombie spawning based on noise
- Environmental hazards (alarms, explosions)
- Physics-based interactions
- Verticality in combat spaces

### Pillar 3: Accessibility Without Compromise
Console-quality gameplay on mobile. One-handed play option. Skill-based, not device-based.

**Design Mandate:**
- Aim assist for mobile (tunable, fair)
- Simplified controls without removing depth
- 60 FPS on mid-range devices
- Crossplay with input-based matchmaking option

### Pillar 4: Rewarding Mastery
Easy to learn, hard to master. Depth through systems mastery, not grinding.

**Design Mandate:**
- Map knowledge matters (loot spawns, extraction routes)
- Zombie behavior understanding (sound, sight, hordes)
- Movement mechanics (parkour, sliding)
- Weapon mastery (recoil patterns, ammo types)

---

## 3. Core Gameplay

### Match Flow (10-15 minutes)

#### Phase 1: Drop (0:00 - 1:00)
- Players select drop zone on map
- 8 players total per match
- Random loot distribution
- Zombies in idle/patrol state

**Tension Point:** Do you drop hot (good loot, more players) or safe (worse loot, isolated)?

#### Phase 2: Scavenge (1:00 - 8:00)
- Search buildings for loot
- Manage inventory (limited slots)
- Avoid/fight zombies
- Optional: Engage other players

**Loot Categories:**
1. **Weapons** - Pistols, SMGs, Rifles, Shotguns
2. **Ammo** - Limited, valuable
3. **Medical** - Bandages, medkits, adrenaline
4. **Valuables** - Gold bars, diamonds (sell for currency)
5. **Keys** - Unlock high-tier loot rooms

**Tension Point:** Noise attracts zombies AND players. Gunfire = dinner bell.

#### Phase 3: Extraction (8:00 - 10:00)
- Extraction zones marked on map (3 per match)
- Must reach extraction and survive 30 seconds
- Helicopter arrives, limited seats
- Final desperate battles

**Tension Point:** Extraction zones are obvious choke points. Other players WILL camp them.

#### Win Conditions
- **Victory:** Extract with loot
- **Defeat:** Die (lose all equipped/looted gear)
- **Survival:** Outlast timer without extracting (keep nothing)

---

## 4. Player Experience

### Moment-to-Moment Gameplay

**The Tension Cycle:**
```
Quiet Looting → Hear Zombie Growl → Adrenaline Spike →
Kill Zombie OR Run → Hear Gunshots Nearby →
Hide OR Investigate → Loot Decision → Repeat
```

### Emotional Beats

**0-2 Minutes: Cautious Optimism**
- "Nice, I found a rifle!"
- Exploring cautiously
- Building confidence

**3-5 Minutes: Rising Tension**
- "Is that a player or zombie?"
- Inventory getting full
- Risk vs reward calculations

**6-8 Minutes: Paranoia**
- "I have too much to lose"
- Every sound is a threat
- Extract or push for more?

**9-10 Minutes: Desperation**
- "Just let me extract!"
- Sprint to extraction
- Final desperate fight

### Victory Emotions
- **Extracted with good loot:** Euphoria, accomplishment
- **Barely survived:** Relief, heart pounding
- **Lost everything:** Frustration but "one more round" feeling

---

## 5. Game Mechanics

### 5.1 Movement System

**PC Controls:**
- WASD: Movement
- Shift: Sprint
- Ctrl: Crouch
- Space: Jump
- C: Slide
- V: Vault

**Mobile Controls:**
- Left Joystick: Movement
- Right Joystick: Aim (auto-fire option)
- Sprint: Double-tap movement
- Crouch/Slide: Swipe down
- Jump/Vault: Context button

**Advanced Movement:**
- **Parkour System:** Auto-vault over low objects (<1.5m)
- **Slide:** Maintain momentum downhill
- **Climbing:** Reach elevated positions
- **Swim:** Slow, vulnerable (avoid water)

**Stamina System:**
- Depletes during sprint, vault, melee
- Regenerates when standing still
- Empty stamina = slow movement, heavy breathing (attracts zombies)

### 5.2 Combat System

**Weapon Categories:**

| Type | Example | Damage | Fire Rate | Noise Level | Ammo Scarcity |
|------|---------|--------|-----------|-------------|---------------|
| Melee | Bat | Medium | N/A | Silent | Infinite |
| Pistol | Glock 19 | Low | Medium | Low | Common |
| SMG | MP5 | Low | High | Medium | Uncommon |
| Rifle | AK-47 | High | Medium | High | Rare |
| Shotgun | SPAS-12 | Very High | Low | Very High | Rare |
| Sniper | AWP | Extreme | Very Low | Extreme | Very Rare |

**Ballistics:**
- Hitscan for performance (not realistic ballistics)
- Recoil patterns (learnable)
- Damage falloff at range
- Headshot multiplier (2.5x)

**Zombie Combat:**
- Headshots = instant kill
- Body shots = 3-5 hits (depends on type)
- Dismemberment (legs = slow, arms = less damage)
- Ragdoll physics on death

**Player vs Player:**
- Time-to-kill (TTK): ~1 second with good aim
- Armor system (3 tiers, degrades)
- Healing takes 3 seconds (vulnerable)

### 5.3 Inventory System

**Slots:**
- Primary Weapon (1)
- Secondary Weapon (1)
- Melee (1)
- Backpack (6-12 slots, depends on backpack tier)
- Equipped Armor (1)
- Medical Pouch (3 slots, quick access)

**Loot Rarity:**
- **Common** (White): Basic weapons, ammo
- **Uncommon** (Green): Attachments, better armor
- **Rare** (Blue): High-tier weapons, medkits
- **Epic** (Purple): Valuables, rare ammo types
- **Legendary** (Gold): Keys, special items

**Weight System:** None (keeps it simple for mobile)

### 5.4 Health & Damage

**Player Health:**
- Base: 100 HP
- Armor: Absorbs % of damage (Tier 1: 25%, Tier 2: 50%, Tier 3: 75%)
- Helmet: Prevents 1 headshot (breaks after)

**Healing Items:**
- Bandage (3s use): +25 HP
- Medkit (5s use): +75 HP
- Adrenaline (instant): +25 HP + 10s speed boost

**Damage Types:**
- Bullet: Standard
- Explosive: AoE, damages armor heavily
- Infection: Zombie bites slowly drain HP (use antibiotics)

### 5.5 Sound Design (Gameplay Critical)

**Noise Levels:**
1. **Silent:** Melee, crouch-walking
2. **Quiet:** Pistol with suppressor, footsteps
3. **Medium:** Pistol, breaking windows
4. **Loud:** Rifle, shotgun, explosions
5. **Extreme:** Car alarms, grenades, screamer zombie

**Zombie Aggro Radius:**
- Silent: 0m
- Quiet: 20m
- Medium: 50m
- Loud: 100m
- Extreme: 200m + calls horde

**Player Audio Cues:**
- **Heartbeat:** Increases near players/extraction
- **Heavy Breathing:** Low stamina
- **Footsteps:** Directional audio (enemy position)
- **Zombie Growls:** Distance-based volume

---

## 6. Zombie AI Design

### 6.1 Core Intelligence

**Sensory System:**

**Vision:**
- 180° cone, 30m range
- Raycasts to detect players
- Memory: Remembers last seen position for 10 seconds

**Hearing:**
- Omnidirectional, range-based
- Gunshots, footsteps, door opens
- Investigates noise source

**Smell (Future):**
- Detects player within 5m through walls
- Adds unpredictability

### 6.2 Behavior State Machine

```
IDLE
  ↓ (See/Hear Player)
CHASE
  ↓ (In Range)
ATTACK
  ↓ (Take Heavy Damage)
FLEE (Optional, rare)
  ↓ (HP = 0)
DEATH
```

**Idle State:**
- Wander patrol points
- Occasional idle animations (look around)
- React to loud noises

**Chase State:**
- NavMesh pathfinding to last known position
- Speed: 3-7 m/s (depends on type)
- Call nearby zombies (within 20m)

**Attack State:**
- Melee range (<2m)
- Lunge attack (animated)
- 10-25 damage per hit
- 1-2 second attack cooldown

**Flee State (Rare):**
- Tank zombie at <10% HP
- Runs to cover
- Allows tactical retreat

### 6.3 Zombie Types

**Runner (Common)**
- Speed: 7 m/s
- HP: 50
- Damage: 10/hit
- Special: Fast, swarms in groups

**Walker (Common)**
- Speed: 3 m/s
- HP: 100
- Damage: 15/hit
- Special: Baseline zombie

**Tank (Uncommon)**
- Speed: 2 m/s
- HP: 300
- Damage: 25/hit
- Special: High HP, mini-boss

**Exploder (Rare)**
- Speed: 5 m/s
- HP: 75
- Damage: 100 (AoE explosion on death)
- Special: Must headshot or keep distance

**Screamer (Rare)**
- Speed: 4 m/s
- HP: 60
- Damage: 5/hit
- Special: Screams call horde (20+ zombies)

### 6.4 Horde Behavior

**Horde Triggers:**
1. Screamer zombie scream
2. Car alarm activated
3. Timed event (every 3 minutes)
4. Extraction phase starts

**Horde Mechanics:**
- 20-50 zombies spawn
- Boids/flocking algorithm (move as group)
- Converge on player last known position
- Despawn after 2 minutes if no target

**Player Strategy:**
- Hide and wait it out
- Fight (risky, lots of loot)
- Use chokepoints (doorways)
- Escape and reposition

---

## 7. Map Design

### 7.1 MVP Map: "Downtown Ruins"

**Theme:** Abandoned city downtown (post-apocalypse, 6 months after outbreak)

**Size:** 500m x 500m (medium)

**Key Locations:**
1. **Police Station** - High-tier weapons, locked armory (needs key)
2. **Hospital** - Medical supplies, vertical gameplay (3 floors)
3. **Shopping Mall** - Wide open, risky, lots of loot
4. **Subway Tunnels** - Dark, CQC, zombie spawn point
5. **Parking Garage** - Verticality, extraction point
6. **Apartments** - Tight corridors, ambush spots
7. **Bank Vault** - Best loot, requires key + explosives

**Extraction Points (3):**
1. Rooftop Helipad (Police Station)
2. Parking Garage (Street level)
3. Park Clearing (Open, dangerous)

**Environmental Hazards:**
- Car alarms (red cars, can activate)
- Broken glass (makes noise)
- Fire (damage over time)
- Collapsed floors (fall damage)

**Loot Distribution:**
- **Hot Zones** (High risk, high reward): Bank, Police Station
- **Medium Zones**: Hospital, Mall
- **Safe Zones** (Low loot): Apartments, side streets

### 7.2 Future Maps

**Map 2: "Military Base"** - Structured, lots of cover, sniper-friendly
**Map 3: "Forest Outpost"** - Open areas, stealth-focused, night setting
**Map 4: "Cruise Ship"** - Tight corridors, water hazards, unique setting

---

## 8. Progression Systems

### 8.1 Player Leveling (Account Level)

**XP Sources:**
- Zombie kills: 10 XP
- Player kills: 100 XP
- Successful extraction: 200 XP
- Loot extracted: 1 XP per $1 value
- Survival time: 10 XP per minute

**Level Rewards:**
1-10: Cosmetics (skins, emotes)
11-20: Loadout slots, inventory space
21-30: Prestige skins
31+: Battle Pass tier skips (seasonal)

**Max Level:** 100 (seasonal reset with prestige option)

### 8.2 Economy System

**Currency: "Outbreak Credits" (OC)**

**Earning OC:**
- Extract valuables (gold bars = 1000 OC)
- Sell weapons/armor you don't need
- Daily/weekly challenges
- Battle Pass rewards

**Spending OC:**
- Buy starting loadout gear (insurance)
- Unlock cosmetics
- Battle Pass purchase
- Stash expansion

**Stash System:**
- Permanent storage for extracted gear
- Use in future matches as starting loadout
- Risk vs reward: Use best gear or save it?

### 8.3 Battle Pass (Seasonal)

**Free Track:**
- Levels 1-50
- Basic cosmetics
- Small OC rewards
- Character XP boosts

**Premium Track ($9.99):**
- Exclusive weapon skins
- Legendary character outfits
- Emotes, sprays
- 1000 OC (worth $10)
- Early access to new zombie types

**Season Length:** 90 days (4 seasons per year)

---

## 9. Monetization

### 9.1 Revenue Streams

**1. Battle Pass ($9.99/season)**
- Expected conversion: 5%
- Revenue: $0.50 ARPU per season

**2. Cosmetics Store**
- Weapon skins: $2-5
- Character outfits: $5-10
- Bundles: $15-20
- Expected: $1 ARPU per month

**3. Convenience Items**
- Extra stash space: $5 (one-time)
- Loadout slots: $3 each
- XP boosts: $2 (1 hour, 2x XP)
- Expected: $0.25 ARPU per month

**4. Rewarded Ads (Mobile Only)**
- Watch ad for bonus loot roll after extraction
- Optional, not intrusive
- Expected: $0.10 ARPU per month

**Total Expected ARPU:** $5-7/month (industry standard for F2P)

### 9.2 Anti-Pay-to-Win Safeguards

**NEVER Sell:**
- ❌ Weapons
- ❌ Ammo
- ❌ Armor
- ❌ Health items
- ❌ Gameplay advantages

**Always Cosmetic Only:**
- ✅ Skins (doesn't affect gameplay)
- ✅ Emotes (fun, not advantage)
- ✅ Sprays
- ✅ Victory poses

**Convenience Is OK:**
- ✅ Extra storage (doesn't affect match)
- ✅ XP boosts (cosmetic unlocks faster)
- ✅ Loadout slots (doesn't give better gear)

---

## 10. Platform Considerations

### 10.1 PC (Steam)

**Advantages:**
- Best graphics settings
- Mouse & keyboard precision
- Large screen awareness

**Optimizations:**
- DLSS/FSR support for lower-end PCs
- Unlocked framerate (60-144 FPS)
- Ultra-wide monitor support

**Steam Features:**
- Achievements
- Trading cards
- Steam Workshop (future mod support)
- Cloud saves

### 10.2 Mobile (Android/iOS)

**Advantages:**
- Largest player base
- Touchscreen controls optimized
- Gyroscope aiming

**Challenges:**
- Performance on lower-end devices
- Battery drain
- Touchscreen occlusion (fingers block view)

**Solutions:**
- Simplified graphics presets (Low/Medium/High)
- Transparent UI elements
- Customizable HUD layout
- One-handed mode option
- Auto-fire for accessibility

**Target Devices:**
- Minimum: 2019 mid-range (Snapdragon 665, iPhone XR)
- Recommended: 2021+ (Snapdragon 778G, iPhone 12)

### 10.3 WebGL (Browser)

**Advantages:**
- Instant play, no download
- Great for virality (share link)
- Low barrier to entry

**Challenges:**
- Limited performance
- File size restrictions

**Solutions:**
- Asset streaming
- Compressed textures
- Simplified shaders
- Max 100MB initial load

### 10.4 Cross-Platform Balancing

**Input-Based Matchmaking (Optional Toggle):**
- Controller vs Controller
- Touch vs Touch
- M&KB vs M&KB
- Mixed (default)

**Aim Assist Settings:**
- PC: None
- Console: Medium
- Mobile: High (tunable in settings)

**Performance Parity:**
- All platforms target 60 FPS minimum
- Graphics don't affect hitboxes/gameplay
- Server-authoritative prevents client-side cheats

---

## 11. Success Metrics (KPIs)

### Engagement Metrics
- **DAU/MAU Ratio:** Target >20% (sticky game)
- **Session Length:** Target 30-45 minutes (2-3 matches)
- **Retention:**
  - Day 1: >60%
  - Day 7: >30%
  - Day 30: >10%

### Monetization Metrics
- **Conversion Rate:** 5% (F2P to paying)
- **ARPU:** $5-7/month
- **LTV (Lifetime Value):** $50-70/player

### Gameplay Metrics
- **Extraction Rate:** 20-30% (most players should lose)
- **Average Kills per Match:** 3-5 zombies, 0.5 players
- **Match Completion:** >80% (players don't quit early)

### Technical Metrics
- **Average FPS:** >55 on mid-range
- **Crash Rate:** <1%
- **Average Latency:** <100ms same region

---

## 12. Competitive Landscape

| Game | Zombies | Extraction | Cross-Platform | F2P | Mobile | Our Advantage |
|------|---------|------------|----------------|-----|--------|---------------|
| **Escape from Tarkov** | ❌ | ✅ | ❌ | ❌ | ❌ | We have zombies + mobile |
| **The Cycle** | ❌ | ✅ | ❌ | ✅ | ❌ | We have zombies + mobile |
| **Left 4 Dead 2** | ✅ | ❌ | ❌ | ❌ | ❌ | We have extraction + modern |
| **World War Z** | ✅ | ❌ | ✅ | ❌ | ❌ | We have extraction + F2P |
| **The Midnight Walkers** | ✅ | ✅ | ❌ | ❓ | ❌ | We have cross-platform + mobile |
| **Dead Frontier: Outbreak** | ✅ | ✅ | ✅ | ✅ | ✅ | **FIRST IN CATEGORY** |

---

## 13. Risk Mitigation

### Technical Risks
- **Risk:** Mobile performance issues
- **Mitigation:** Aggressive LOD, object pooling, texture streaming

- **Risk:** Netcode lag/desync
- **Mitigation:** Server authoritative, lag compensation, Unity Relay

- **Risk:** Cheaters (aimbot, wallhack)
- **Mitigation:** Server validation, anti-cheat (Easy Anti-Cheat integration)

### Design Risks
- **Risk:** Too difficult for casual players
- **Mitigation:** Co-op Horde mode, tutorial, matchmaking by skill

- **Risk:** Toxic community
- **Mitigation:** Report system, chat moderation, positive reinforcement

- **Risk:** Stale meta (same strategies every game)
- **Mitigation:** Dynamic events, map rotations, balance patches

### Business Risks
- **Risk:** Low player retention
- **Mitigation:** Daily challenges, Battle Pass, seasonal content

- **Risk:** Low monetization
- **Mitigation:** Attractive cosmetics, fair pricing, influencer marketing

---

## 14. Post-Launch Content Roadmap

### Season 1 (Months 1-3)
- New map: "Military Base"
- 2 new zombie types
- 5 new weapons
- Battle Pass #1

### Season 2 (Months 4-6)
- Ranked mode
- New map: "Forest Outpost"
- Clan system
- Battle Pass #2

### Season 3 (Months 7-9)
- Limited-time events (Halloween)
- New extraction mechanic (armored truck)
- Boss zombie (mega horde)
- Battle Pass #3

### Season 4 (Months 10-12)
- Map reworks based on data
- Community-voted features
- Esports tournament
- Battle Pass #4

---

## 15. Conclusion

Dead Frontier: Outbreak fills a unique gap in the market: a cross-platform, mobile-optimized zombie extraction shooter with fair F2P monetization. By leveraging Unity's ecosystem, open-source assets, and smart scoping (MVP → Expansion), we can ship a competitive product in Q4 2025.

**Core Differentiators:**
1. ✅ First mobile zombie extraction shooter
2. ✅ True cross-platform (PC/Mobile/Web)
3. ✅ Smart zombie AI (not just bullet sponges)
4. ✅ Fair F2P (cosmetic-only)
5. ✅ Quick matches (10-15 min, perfect for mobile)

**Next Steps:**
1. Finalize Technical Design Document (TDD)
2. Create vertical slice (1 weapon, 1 zombie type, 1 small map)
3. Playtest and iterate
4. Build MVP
5. Soft launch and gather data
6. Full launch with marketing push

---

**Approved By:** [Pending]
**Next Review:** After Vertical Slice Completion

---

*This is a living document. Update regularly based on playtesting and market research.*
