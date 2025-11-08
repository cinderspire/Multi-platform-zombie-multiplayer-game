using UnityEngine;
using System;
using System.Collections.Generic;

namespace DeadFrontier.Player
{
    /// <summary>
    /// Manages player progression, XP, levels, and unlocks
    /// </summary>
    public class PlayerProgression : MonoBehaviour
    {
        [Header("Progression")]
        [SerializeField] private int currentLevel = 1;
        [SerializeField] private int currentXP = 0;
        [SerializeField] private int maxLevel = 100;
        [SerializeField] private AnimationCurve xpCurve; // XP needed per level

        [Header("XP Rewards")]
        [SerializeField] private int xpPerZombieKill = 10;
        [SerializeField] private int xpPerPlayerKill = 50;
        [SerializeField] private int xpPerHeadshot = 5; // Bonus
        [SerializeField] private int xpPerExtraction = 100;
        [SerializeField] private int xpPerSurvivalMinute = 5;
        [SerializeField] private int xpPerItemLooted = 2;

        [Header("Prestige")]
        [SerializeField] private int prestigeLevel = 0;
        [SerializeField] private int maxPrestigeLevel = 10;

        // Unlocks
        private HashSet<string> unlockedItems = new HashSet<string>();
        private Dictionary<string, int> stats = new Dictionary<string, int>();

        // Events
        public event Action<int> OnXPGained;
        public event Action<int> OnLevelUp;
        public event Action<int> OnPrestige;
        public event Action<string> OnItemUnlocked;

        // Properties
        public int Level => currentLevel;
        public int XP => currentXP;
        public int PrestigeLevel => prestigeLevel;
        public int TotalLevel => prestigeLevel * maxLevel + currentLevel;

        private void Start()
        {
            // Load progression from save system
            LoadProgression();

            // Initialize stats
            InitializeStats();
        }

        #region XP and Leveling

        /// <summary>
        /// Awards XP to the player
        /// </summary>
        public void AwardXP(int amount, string reason = "")
        {
            if (currentLevel >= maxLevel && prestigeLevel >= maxPrestigeLevel)
                return; // Max level and prestige reached

            currentXP += amount;
            OnXPGained?.Invoke(amount);

            Debug.Log($"[PlayerProgression] +{amount} XP {(string.IsNullOrEmpty(reason) ? "" : $"({reason})")}");

            // Check for level up
            CheckLevelUp();

            // Update stats
            IncrementStat("totalXPEarned", amount);
        }

        /// <summary>
        /// Awards XP for killing a zombie
        /// </summary>
        public void AwardZombieKill(bool isHeadshot = false)
        {
            int xp = xpPerZombieKill;
            if (isHeadshot)
                xp += xpPerHeadshot;

            AwardXP(xp, isHeadshot ? "Zombie Kill (Headshot)" : "Zombie Kill");
            IncrementStat("zombiesKilled", 1);

            if (isHeadshot)
                IncrementStat("headshotKills", 1);
        }

        /// <summary>
        /// Awards XP for killing a player
        /// </summary>
        public void AwardPlayerKill(bool isHeadshot = false)
        {
            int xp = xpPerPlayerKill;
            if (isHeadshot)
                xp += xpPerHeadshot;

            AwardXP(xp, isHeadshot ? "Player Kill (Headshot)" : "Player Kill");
            IncrementStat("playersKilled", 1);

            if (isHeadshot)
                IncrementStat("headshotKills", 1);
        }

        /// <summary>
        /// Awards XP for successful extraction
        /// </summary>
        public void AwardExtraction(int itemsExtracted = 0)
        {
            int xp = xpPerExtraction + (itemsExtracted * xpPerItemLooted);
            AwardXP(xp, "Successful Extraction");
            IncrementStat("successfulExtractions", 1);
        }

        /// <summary>
        /// Awards XP for surviving time
        /// </summary>
        public void AwardSurvivalTime(float minutes)
        {
            int xp = Mathf.FloorToInt(minutes * xpPerSurvivalMinute);
            AwardXP(xp, "Survival Time");
        }

        /// <summary>
        /// Awards XP for looting an item
        /// </summary>
        public void AwardItemLooted(Items.ItemRarity rarity)
        {
            int rarityMultiplier = (int)rarity + 1; // Common=1, Uncommon=2, etc.
            int xp = xpPerItemLooted * rarityMultiplier;
            AwardXP(xp, "Item Looted");
            IncrementStat("itemsLooted", 1);
        }

        private void CheckLevelUp()
        {
            int xpNeeded = GetXPNeededForLevel(currentLevel);

            while (currentXP >= xpNeeded && currentLevel < maxLevel)
            {
                currentXP -= xpNeeded;
                currentLevel++;

                Debug.Log($"[PlayerProgression] LEVEL UP! Now level {currentLevel}");
                OnLevelUp?.Invoke(currentLevel);

                // Check for unlocks at this level
                CheckUnlocksForLevel(currentLevel);

                // Get XP needed for next level
                xpNeeded = GetXPNeededForLevel(currentLevel);
            }

            // If max level reached and still have XP, allow prestige
            if (currentLevel >= maxLevel && currentXP >= xpNeeded)
            {
                // Ready to prestige
            }
        }

        /// <summary>
        /// Gets XP needed to reach a specific level
        /// </summary>
        public int GetXPNeededForLevel(int level)
        {
            if (level >= maxLevel)
                return int.MaxValue;

            // Use animation curve for non-linear progression
            if (xpCurve != null && xpCurve.length > 0)
            {
                float t = (float)level / maxLevel;
                return Mathf.RoundToInt(xpCurve.Evaluate(t) * 10000f);
            }

            // Fallback: Linear progression (1000 XP per level, increasing by 100 each level)
            return 1000 + (level * 100);
        }

        /// <summary>
        /// Gets current progress to next level (0-1)
        /// </summary>
        public float GetLevelProgress()
        {
            if (currentLevel >= maxLevel)
                return 1f;

            int xpNeeded = GetXPNeededForLevel(currentLevel);
            return Mathf.Clamp01((float)currentXP / xpNeeded);
        }

        #endregion

        #region Prestige

        /// <summary>
        /// Prestiges the player (resets level but grants prestige bonuses)
        /// </summary>
        public void Prestige()
        {
            if (currentLevel < maxLevel)
            {
                Debug.LogWarning("[PlayerProgression] Must reach max level to prestige");
                return;
            }

            if (prestigeLevel >= maxPrestigeLevel)
            {
                Debug.LogWarning("[PlayerProgression] Max prestige level reached");
                return;
            }

            prestigeLevel++;
            currentLevel = 1;
            currentXP = 0;

            Debug.Log($"[PlayerProgression] PRESTIGE {prestigeLevel}!");
            OnPrestige?.Invoke(prestigeLevel);

            // Grant prestige rewards
            GrantPrestigeRewards(prestigeLevel);

            IncrementStat("timesPrestiged", 1);
        }

        private void GrantPrestigeRewards(int prestige)
        {
            // TODO: Grant special prestige rewards
            // - Exclusive skins
            // - XP boost
            // - Special titles
            // - Unique weapon skins

            Debug.Log($"[PlayerProgression] Granted prestige {prestige} rewards");
        }

        /// <summary>
        /// Gets XP boost from prestige (multiplicative)
        /// </summary>
        public float GetPrestigeXPBoost()
        {
            return 1f + (prestigeLevel * 0.1f); // 10% per prestige
        }

        #endregion

        #region Unlocks

        /// <summary>
        /// Checks for items that should unlock at this level
        /// </summary>
        private void CheckUnlocksForLevel(int level)
        {
            // Get all unlockables for this level
            var unlockables = GetUnlockablesForLevel(level);

            foreach (var unlockable in unlockables)
            {
                UnlockItem(unlockable);
            }
        }

        /// <summary>
        /// Unlocks an item/weapon/perk
        /// </summary>
        public void UnlockItem(string itemId)
        {
            if (unlockedItems.Contains(itemId))
                return;

            unlockedItems.Add(itemId);
            OnItemUnlocked?.Invoke(itemId);

            Debug.Log($"[PlayerProgression] Unlocked: {itemId}");

            // Show unlock notification
            // UIManager could handle this
        }

        /// <summary>
        /// Checks if an item is unlocked
        /// </summary>
        public bool IsUnlocked(string itemId)
        {
            return unlockedItems.Contains(itemId);
        }

        /// <summary>
        /// Gets all unlockables for a specific level
        /// </summary>
        private List<string> GetUnlockablesForLevel(int level)
        {
            // TODO: Load from configuration/database
            List<string> unlockables = new List<string>();

            // Example unlock progression
            switch (level)
            {
                case 2: unlockables.Add("weapon_pistol"); break;
                case 3: unlockables.Add("perk_quickreload"); break;
                case 5: unlockables.Add("weapon_smg"); break;
                case 7: unlockables.Add("perk_extrahealth"); break;
                case 10: unlockables.Add("weapon_shotgun"); break;
                case 12: unlockables.Add("perk_fasterheal"); break;
                case 15: unlockables.Add("weapon_assaultrifle"); break;
                case 20: unlockables.Add("perk_extramags"); break;
                case 25: unlockables.Add("weapon_sniper"); break;
                case 30: unlockables.Add("perk_lightweight"); break;
                // ... more unlocks
            }

            return unlockables;
        }

        #endregion

        #region Stats

        private void InitializeStats()
        {
            // Initialize all tracked stats
            stats["zombiesKilled"] = 0;
            stats["playersKilled"] = 0;
            stats["headshotKills"] = 0;
            stats["totalDeaths"] = 0;
            stats["successfulExtractions"] = 0;
            stats["failedExtractions"] = 0;
            stats["itemsLooted"] = 0;
            stats["totalXPEarned"] = 0;
            stats["timesPrestiged"] = 0;
            stats["totalMatchesPlayed"] = 0;
            stats["totalMatchesWon"] = 0;
            stats["totalDamageDealt"] = 0;
            stats["totalDamageTaken"] = 0;
            stats["totalDistanceTraveled"] = 0;
        }

        private void IncrementStat(string statName, int amount)
        {
            if (!stats.ContainsKey(statName))
                stats[statName] = 0;

            stats[statName] += amount;
        }

        /// <summary>
        /// Gets a stat value
        /// </summary>
        public int GetStat(string statName)
        {
            return stats.ContainsKey(statName) ? stats[statName] : 0;
        }

        /// <summary>
        /// Gets all stats
        /// </summary>
        public Dictionary<string, int> GetAllStats()
        {
            return new Dictionary<string, int>(stats);
        }

        /// <summary>
        /// Gets K/D ratio
        /// </summary>
        public float GetKDRatio()
        {
            int kills = GetStat("playersKilled");
            int deaths = GetStat("totalDeaths");

            if (deaths == 0)
                return kills;

            return (float)kills / deaths;
        }

        /// <summary>
        /// Gets headshot percentage
        /// </summary>
        public float GetHeadshotPercentage()
        {
            int headshots = GetStat("headshotKills");
            int totalKills = GetStat("zombiesKilled") + GetStat("playersKilled");

            if (totalKills == 0)
                return 0f;

            return (float)headshots / totalKills * 100f;
        }

        #endregion

        #region Save/Load

        /// <summary>
        /// Saves progression data
        /// </summary>
        public void SaveProgression()
        {
            // TODO: Integrate with save system
            PlayerPrefs.SetInt("PlayerLevel", currentLevel);
            PlayerPrefs.SetInt("PlayerXP", currentXP);
            PlayerPrefs.SetInt("PrestigeLevel", prestigeLevel);

            // Save stats
            foreach (var stat in stats)
            {
                PlayerPrefs.SetInt($"Stat_{stat.Key}", stat.Value);
            }

            // Save unlocks
            PlayerPrefs.SetString("UnlockedItems", string.Join(",", unlockedItems));

            PlayerPrefs.Save();

            Debug.Log("[PlayerProgression] Progression saved");
        }

        /// <summary>
        /// Loads progression data
        /// </summary>
        public void LoadProgression()
        {
            currentLevel = PlayerPrefs.GetInt("PlayerLevel", 1);
            currentXP = PlayerPrefs.GetInt("PlayerXP", 0);
            prestigeLevel = PlayerPrefs.GetInt("PrestigeLevel", 0);

            // Load stats
            foreach (var statKey in new List<string>(stats.Keys))
            {
                stats[statKey] = PlayerPrefs.GetInt($"Stat_{statKey}", 0);
            }

            // Load unlocks
            string unlockedItemsStr = PlayerPrefs.GetString("UnlockedItems", "");
            if (!string.IsNullOrEmpty(unlockedItemsStr))
            {
                foreach (var item in unlockedItemsStr.Split(','))
                {
                    if (!string.IsNullOrEmpty(item))
                        unlockedItems.Add(item);
                }
            }

            Debug.Log($"[PlayerProgression] Loaded - Level {currentLevel}, Prestige {prestigeLevel}");
        }

        /// <summary>
        /// Resets all progression (for testing or prestige)
        /// </summary>
        public void ResetProgression()
        {
            currentLevel = 1;
            currentXP = 0;
            prestigeLevel = 0;
            unlockedItems.Clear();
            InitializeStats();

            SaveProgression();

            Debug.Log("[PlayerProgression] Progression reset");
        }

        #endregion

        private void OnApplicationQuit()
        {
            SaveProgression();
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause)
                SaveProgression();
        }
    }
}
