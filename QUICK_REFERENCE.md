# 🎮 Quick Reference Guide

## Fast reference for common tasks and controls.

---

## 🎯 PLAYER CONTROLS

### **Movement:**
- `W A S D` - Move forward/left/back/right
- `Shift` - Sprint (consumes stamina)
- `Ctrl` - Crouch
- `Space` - Jump

### **Camera:**
- `Mouse Move` - Look around
- `V` - Switch 1st/3rd person
- `Right Click` - Aim (zoom)

### **Combat:**
- `Left Click` - Fire weapon
- `R` - Reload
- `1-5` - Switch weapons
- `F` - Melee attack

### **Interaction:**
- `E` - Interact with objects
- `Tab` - Open inventory
- `M` - Open map
- `Q` - Open quest log

### **Social:**
- `Enter` - Open chat
- `T` - Team chat
- `Y` - Global chat
- `V` - Push-to-talk (voice)

### **UI:**
- `Esc` - Pause menu
- `F1` - Debug menu (dev build)
- `F5` - Performance overlay

---

## 🛠️ DEBUG COMMANDS

**Press F1 to open debug menu, then:**

- `F2` - Toggle god mode
- `F3` - Spawn test zombie
- `F4` - Kill all zombies
- `F5` - Toggle performance monitor

**Console Commands (via debug menu):**
- Heal Player
- Give Currency
- Complete Objective
- Trigger Victory/Defeat

---

## 📊 SYSTEM STATUS INDICATORS

### **HUD Elements:**
- **Top Left:** Health bar (green), Armor bar (blue)
- **Top Right:** Mini-map
- **Bottom Left:** Stamina bar (yellow)
- **Bottom Right:** Ammo counter
- **Center:** Crosshair (changes color when aiming at enemy)
- **Right Side:** Kill feed

### **Status Icons:**
- 🟢 Green health: >75%
- 🟡 Yellow health: 25-75%
- 🔴 Red health: <25%
- ⚡ Stamina depleted: Can't sprint/jump

---

## 🎮 GAME FLOW

### **Main Menu:**
1. Host Game (starts as server)
2. Join Game (connects as client)
3. Settings
4. Quit

### **Lobby:**
1. Wait for players
2. Click "Ready"
3. Game starts when all ready (10s countdown)

### **In-Game:**
1. Survive waves of zombies
2. Complete objectives
3. Work with team
4. Reach extraction or complete all waves

### **Game Over:**
- Victory: All objectives complete
- Defeat: All players dead
- Return to lobby or main menu

---

## 🧟 ZOMBIE TYPES

| Type | Speed | Health | Damage | Special |
|------|-------|--------|--------|---------|
| Walker | Slow | 100 | 10 | Basic zombie |
| Runner | Fast | 75 | 8 | Chases quickly |
| Tank | Very Slow | 300 | 25 | High health |
| Spitter | Medium | 100 | 15 | Ranged acid |
| Exploder | Fast | 50 | 50 | Explodes on death |

---

## 💰 CURRENCY TYPES

- **Gold** - Main currency (shops, trading)
- **Gems** - Premium currency (cosmetics)
- **Scrap** - Crafting material
- **Tokens** - Special events

---

## 🎯 OBJECTIVES

### **Objective Types:**
1. **Kill** - Eliminate X zombies
2. **Survive** - Last X minutes
3. **Rescue** - Save NPCs
4. **Defend** - Protect location
5. **Collect** - Gather items
6. **Reach** - Get to extraction
7. **Activate** - Turn on devices

---

## 🏆 PROGRESSION

### **Leveling:**
- Level 1-100
- XP from kills, objectives, time survived
- Prestige at level 100

### **Ranks:**
1. Rookie (1-10)
2. Soldier (11-25)
3. Veteran (26-50)
4. Elite (51-75)
5. Master (76-90)
6. Legend (91-99)
7. Prestige (100)

---

## 🛡️ EQUIPMENT SLOTS

1. Primary Weapon
2. Secondary Weapon
3. Melee Weapon
4. Helmet
5. Chest Armor
6. Gloves
7. Boots
8. Accessory 1
9. Accessory 2

---

## 🔧 CRAFTING

### **Stations:**
- Workbench (weapons/tools)
- Armor Stand (armor/clothing)
- Alchemy Lab (potions/consumables)
- Forge (metal items)
- Engineering (electronics)

### **Materials:**
- Wood, Metal, Cloth, Electronics, Chemicals

---

## 🎖️ ABILITIES

### **Active Abilities (Q, E, F, C):**
1. **Heal** - Restore 50 HP (60s cooldown)
2. **Speed Boost** - 2x speed for 5s (30s cooldown)
3. **Damage Boost** - 1.5x damage for 10s (45s cooldown)
4. **Shield** - +50 shield (90s cooldown)
5. **Teleport** - Dash forward 10m (15s cooldown)
6. **Invisibility** - Hide from zombies 8s (120s cooldown)

---

## 👥 MULTIPLAYER

### **Team Roles:**
- Leader (damage +10%)
- Tank (health +20%)
- Support (healing +20%)
- DPS (damage +15%)
- Scout (speed +10%)

### **Communication:**
- Text Chat (Enter)
- Voice Chat (V to talk)
- Ping System (Middle Mouse)

---

## 📈 PERFORMANCE TIPS

### **For Better FPS:**
1. Lower graphics quality (Settings menu)
2. Reduce shadow quality
3. Disable motion blur/bloom
4. Lower resolution
5. Close other applications

### **For Better Network:**
1. Use wired connection
2. Close bandwidth-heavy apps
3. Select nearest server region
4. Check firewall settings (port 7777)

---

## 🐛 COMMON ISSUES

### **Can't connect:**
- Check network settings
- Verify port 7777 is open
- Try direct IP connection
- Restart NetworkManager

### **Low FPS:**
- Press F5 to check performance
- Lower graphics settings
- Check CPU/GPU usage
- Update drivers

### **Zombies not spawning:**
- Check SpawnerSystem is active
- Verify NavMesh is baked
- Check spawn points exist

### **Player not moving:**
- Check CharacterController is attached
- Verify ground layer is correct
- Check PlayerController settings

---

## 🚀 QUICK START

**Solo Testing:**
1. Open GameScene
2. Press Play
3. Click "Host Game"
4. Press F3 to spawn zombies

**Multiplayer Testing:**
1. Build game (Ctrl+Shift+B)
2. Run built game → Host
3. Run Unity Editor → Join
4. Test together

---

## 📞 SUPPORT

### **Documentation:**
- GAME_COMPLETE.md - Full system list
- UNITY_SETUP_GUIDE.md - Setup instructions
- PREFAB_CONFIGURATION.md - Prefab setup

### **Debugging:**
- F1: Debug menu
- F5: Performance monitor
- Console: View error logs
- Profiler: Check performance

---

## ✅ CHECKLIST

**Before Playing:**
- [ ] Scene properly set up
- [ ] All systems initialized
- [ ] Network connected
- [ ] Controls configured

**Before Releasing:**
- [ ] All features tested
- [ ] Performance optimized
- [ ] Multiplayer works
- [ ] No console errors
- [ ] Build tested

---

## 🎉 ENJOY THE GAME!

You now have a complete, production-ready zombie multiplayer game. Have fun and happy gaming! 🧟‍♂️🎮
