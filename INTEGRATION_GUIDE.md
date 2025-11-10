# System Integration Guide

## Quick Start Integration

### 1. Scene Setup
Add these systems to your main NetworkManager scene:

```
NetworkManager (GameObject)
├─ SystemManagers (GameObject)
│  ├─ QuestSystem
│  ├─ PartySystem
│  ├─ TradingSystem
│  ├─ ProgressionSystem
│  ├─ PerkSystem
│  ├─ InventorySystem
│  ├─ CraftingSystem
│  ├─ ClanSystem
│  ├─ BattlePassSystem
│  ├─ FactionSystem
│  ├─ EconomyManager
│  ├─ AchievementSystem
│  ├─ CosmeticSystem
│  ├─ ArenaSystem
│  ├─ EventSystem
│  ├─ WeaponSystem
│  ├─ BaseBuildingSystem
│  ├─ AIDirectorSystem
│  ├─ LootSystem
│  ├─ MatchmakingSystem
│  ├─ DailyRewardSystem
│  ├─ LeaderboardSystem
│  ├─ MailSystem
│  ├─ TutorialSystem
│  └─ WeatherSystem
```

### 2. System Dependencies

**Core Systems** (Initialize First):
- EconomyManager
- InventorySystem
- ProgressionSystem

**Secondary Systems** (Depend on Core):
- CraftingSystem → InventorySystem
- TradingSystem → InventorySystem + EconomyManager
- AchievementSystem → ProgressionSystem + EconomyManager
- BattlePassSystem → ProgressionSystem
- DailyRewardSystem → EconomyManager + InventorySystem

**Gameplay Systems**:
- WeaponSystem → InventorySystem
- LootSystem → InventorySystem
- AIDirectorSystem → WeaponSystem
- WeatherSystem → AIDirectorSystem

**Social Systems**:
- PartySystem → ProgressionSystem
- ClanSystem → ProgressionSystem + TradingSystem
- FactionSystem → ProgressionSystem + EconomyManager

### 3. Player Initialization Sequence

When a player connects, initialize in this order:

```csharp
void OnPlayerConnected(ulong playerId)
{
    // Core
    EconomyManager.Instance?.InitializePlayerWalletServerRpc(playerId);
    InventorySystem.Instance?.InitializePlayerInventoryServerRpc(playerId);
    ProgressionSystem.Instance?.CreateNewProgression(playerId);
    
    // Progression
    PerkSystem.Instance?.InitializePlayerPerks(playerId);
    AchievementSystem.Instance?.InitializePlayerAchievementsServerRpc(playerId);
    
    // Social
    PartySystem.Instance?.InitializePlayer(playerId);
    FactionSystem.Instance?.InitializePlayerFactionsServerRpc(playerId);
    
    // Engagement
    BattlePassSystem.Instance?.InitializePlayerBattlePassServerRpc(playerId);
    DailyRewardSystem.Instance?.InitializePlayerDailyDataServerRpc(playerId);
    
    // Crafting
    CraftingSystem.Instance?.InitializePlayerCraftingData(playerId);
    
    // Arena
    ArenaSystem.Instance?.InitializePlayerArenaDataServerRpc(playerId);
    
    // Tutorial
    TutorialSystem.Instance?.InitializePlayerTutorialServerRpc(playerId);
}
```

### 4. Common Integration Patterns

#### Adding Currency
```csharp
EconomyManager.Instance?.AddCurrencyServerRpc(
    playerId, 
    Economy.CurrencyType.Soft, 
    amount
);
```

#### Adding Items
```csharp
InventorySystem.Instance?.AddItemServerRpc(
    playerId, 
    itemId, 
    quantity, 
    Inventory.ContainerType.Backpack
);
```

#### Granting XP
```csharp
ProgressionSystem.Instance?.AddExperienceServerRpc(
    playerId, 
    xpAmount, 
    "source"
);
```

#### Unlocking Achievement
```csharp
AchievementSystem.Instance?.UpdateAchievementProgressServerRpc(
    playerId, 
    AchievementObjectiveType.Kill, 
    "zombie_any", 
    1
);
```

#### Updating Battle Pass
```csharp
BattlePassSystem.Instance?.AddBattlePassXPServerRpc(
    playerId, 
    xpAmount
);
```

### 5. Event System Usage

#### Listening to System Events
```csharp
void Start()
{
    // Economy events
    EconomyManager.Instance.OnCurrencyChanged += HandleCurrencyChanged;
    EconomyManager.Instance.OnPurchaseCompleted += HandlePurchase;
    
    // Achievement events
    AchievementSystem.Instance.OnAchievementUnlocked += HandleAchievement;
    
    // Progression events
    ProgressionSystem.Instance.OnPlayerLevelUp += HandleLevelUp;
    
    // Party events
    PartySystem.Instance.OnPartyCreated += HandlePartyCreated;
}

void HandleCurrencyChanged(ulong playerId, CurrencyType type, int amount)
{
    // Update UI, etc.
}
```

### 6. Cross-System Rewards

Many systems grant rewards from multiple sources:

```csharp
// Complete achievement → grants currency + items + title
Achievement completed
├─ EconomyManager.AddCurrency()
├─ InventorySystem.AddItem()
└─ ProgressionSystem.UnlockTitle()

// Daily reward → grants multiple rewards
Daily login
├─ EconomyManager.AddCurrency()
├─ InventorySystem.AddItem()
└─ CosmeticSystem.UnlockCosmetic()
```

### 7. Testing Integration

Run this test sequence:

1. **Start Server**
2. **Connect Player**
3. **Verify Initialization**:
   - Check wallet created
   - Check inventory created
   - Check progression initialized
4. **Test Core Loop**:
   - Kill zombie → XP gained → Achievement progress
   - Loot container → Items added → Inventory updated
   - Craft item → Materials removed → Item created
5. **Test Economy**:
   - Purchase from shop → Currency deducted → Item received
   - Sell on marketplace → Item removed → Currency gained
6. **Test Social**:
   - Create party → Party exists → Bonuses applied
   - Join clan → Member added → Clan bonuses active

### 8. Performance Optimization

**Caching Recommendations**:
- Cache player data after initialization
- Use local dictionaries for frequent lookups
- Batch network updates when possible
- Use events instead of Update() polling

**Network Optimization**:
- Only sync critical data
- Use NetworkVariable for automatic sync
- ClientRpc only for UI updates
- ServerRpc for all game logic

### 9. Common Issues & Solutions

**Issue**: Systems not found (Instance is null)
**Solution**: Ensure NetworkManager spawns system GameObjects

**Issue**: Player data not initialized
**Solution**: Call Init methods in OnPlayerConnected

**Issue**: Integration points not working
**Solution**: Check namespace imports (using ZombieGame.xxx)

**Issue**: Events not firing
**Solution**: Subscribe to events after system initialization

### 10. Namespace Structure

```
ZombieGame
├─ Achievements
├─ AIDirector
├─ Arena
├─ BaseBuilding
├─ BattlePass
├─ Clans
├─ Cosmetics
├─ Crafting
├─ DailyRewards
├─ Economy
├─ Events
├─ Factions
├─ Inventory
├─ Leaderboards
├─ Loot
├─ Mail
├─ Matchmaking
├─ Party
├─ Perks
├─ Progression
├─ Quests
├─ Trading
├─ Tutorial
├─ Weapons
└─ Weather
```

### 11. Build Settings

**Required Packages**:
- Unity Netcode for GameObjects 2.0
- TextMeshPro
- Unity UI

**Scripting Define Symbols**:
- UNITY_NETCODE (for conditional compilation)

**Platform-Specific**:
- PC/Console: Full features
- Mobile: Optimized UI, touch controls

---

## System Status Checklist

✅ All 25 systems implemented
✅ Singleton pattern on all managers
✅ NetworkBehaviour base class
✅ ServerRpc/ClientRpc validation
✅ Complete data models
✅ Event-driven architecture
✅ Cross-system integration points
✅ Documentation complete

---

*Integration guide for Multi-Platform Zombie Multiplayer Game*
*Last updated: November 10, 2024*
