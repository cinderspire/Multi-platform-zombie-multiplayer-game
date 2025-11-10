#!/bin/bash

echo "=========================================="
echo "System Verification Script"
echo "=========================================="
echo ""

# List of expected systems from this session
SYSTEMS=(
    "Scripts/Quests/QuestSystem.cs"
    "Scripts/Party/PartySystem.cs"
    "Scripts/Trading/TradingSystem.cs"
    "Scripts/Progression/ProgressionSystem.cs"
    "Scripts/Perks/PerkSystem.cs"
    "Scripts/Inventory/InventorySystem.cs"
    "Scripts/Crafting/CraftingSystem.cs"
    "Scripts/Clans/ClanSystem.cs"
    "Scripts/BattlePass/BattlePassSystem.cs"
    "Scripts/Factions/FactionSystem.cs"
    "Scripts/Economy/EconomyManager.cs"
    "Scripts/Achievements/AchievementSystem.cs"
    "Scripts/Cosmetics/CosmeticSystem.cs"
    "Scripts/Arena/ArenaSystem.cs"
    "Scripts/Events/EventSystem.cs"
    "Scripts/Weapons/WeaponSystem.cs"
    "Scripts/BaseBuilding/BaseBuildingSystem.cs"
    "Scripts/AIDirector/AIDirectorSystem.cs"
    "Scripts/Loot/LootSystem.cs"
    "Scripts/Matchmaking/MatchmakingSystem.cs"
    "Scripts/DailyRewards/DailyRewardSystem.cs"
    "Scripts/Leaderboards/LeaderboardSystem.cs"
    "Scripts/Mail/MailSystem.cs"
    "Scripts/Tutorial/TutorialSystem.cs"
    "Scripts/Weather/WeatherSystem.cs"
)

TOTAL=${#SYSTEMS[@]}
FOUND=0
MISSING=0

echo "Checking for ${TOTAL} core systems..."
echo ""

for system in "${SYSTEMS[@]}"; do
    if [ -f "$system" ]; then
        echo "✓ Found: $system"
        FOUND=$((FOUND + 1))
        
        # Check for singleton pattern
        if grep -q "public static.*Instance" "$system"; then
            echo "  └─ Has Singleton pattern"
        fi
        
        # Check for NetworkBehaviour
        if grep -q "NetworkBehaviour" "$system"; then
            echo "  └─ Extends NetworkBehaviour"
        fi
        
    else
        echo "✗ Missing: $system"
        MISSING=$((MISSING + 1))
    fi
done

echo ""
echo "=========================================="
echo "Summary:"
echo "  Found: ${FOUND}/${TOTAL}"
echo "  Missing: ${MISSING}/${TOTAL}"
echo "=========================================="

if [ $MISSING -eq 0 ]; then
    echo "✅ All systems are present!"
    exit 0
else
    echo "⚠️  Some systems are missing!"
    exit 1
fi
