using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ZombieGame
{
    /// <summary>
    /// Statistics Tracker - Comprehensive player stat tracking
    /// Features: All-time stats, session stats, leaderboards, achievements integration
    /// Essential for player progression and competitive features
    /// </summary>
    public class StatisticsTracker : MonoBehaviour
    {
        public static StatisticsTracker Instance { get; private set; }

        private PlayerStatistics allTimeStats;
        private PlayerStatistics sessionStats;
        private Dictionary<string, WeaponStats> weaponStats = new Dictionary<string, WeaponStats>();

        // Events
        public event Action<StatType, float> OnStatUpdated;
        public event Action<string> OnMilestoneReached;

        [Serializable]
        public class PlayerStatistics
        {
            // Combat Stats
            public int totalKills;
            public int totalDeaths;
            public float kdRatio;
            public int headshots;
            public float headshotPercent;
            public int melee Kills;
            public int grenadeKills;
            public long totalDamageDealt;
            public long totalDamageTaken;

            // Survival Stats
            public float totalPlayTime;
            public float longestSurvivalTime;
            public int gamesPlayed;
            public int gamesWon;
            public float winRate;
            public int timesDied;
            public int timesRevived;

            // Movement Stats
            public float distanceWalked;
            public float distanceRan;
            public float distanceJumped;
            public int jumpCount;

            // Economy Stats
            public int currencyEarned;
            public int currencySpent;
            public int itemsLooted;
            public int itemsCrafted;

            // Social Stats
            public int teamsJoined;
            public int friendsAdded;
            public int messagesSent;
            public int voiceChatMinutes;

            // Achievement Stats
            public int achievementsUnlocked;
            public int challengesCompleted;
            public int milestonesReached;
        }

        [Serializable]
        public class WeaponStats
        {
            public string weaponName;
            public int kills;
            public int shots;
            public int hits;
            public float accuracy;
            public int headshots;
            public long damageDealt;
            public float timesUsed;
        }

        public enum StatType
        {
            Kills, Deaths, Headshots, Damage, PlayTime, Distance, Currency, Achievements
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                LoadStatistics();
                ResetSessionStats();
            }
            else { Destroy(gameObject); }
        }

        private void Update()
        {
            // Track play time
            if (allTimeStats != null)
            {
                allTimeStats.totalPlayTime += Time.deltaTime;
                sessionStats.totalPlayTime += Time.deltaTime;
            }
        }

        // Stat Recording

        public void RecordKill(bool isHeadshot = false, string weaponUsed = "")
        {
            allTimeStats.totalKills++;
            sessionStats.totalKills++;

            if (isHeadshot)
            {
                allTimeStats.headshots++;
                sessionStats.headshots++;
            }

            if (!string.IsNullOrEmpty(weaponUsed))
            {
                RecordWeaponKill(weaponUsed, isHeadshot);
            }

            UpdateKDRatio();
            OnStatUpdated?.Invoke(StatType.Kills, allTimeStats.totalKills);
            CheckMilestones();
            SaveStatistics();
        }

        public void RecordDeath()
        {
            allTimeStats.totalDeaths++;
            allTimeStats.timesDied++;
            sessionStats.totalDeaths++;
            sessionStats.timesDied++;

            UpdateKDRatio();
            OnStatUpdated?.Invoke(StatType.Deaths, allTimeStats.totalDeaths);
            SaveStatistics();
        }

        public void RecordDamage(long damage, bool dealt = true)
        {
            if (dealt)
            {
                allTimeStats.totalDamageDealt += damage;
                sessionStats.totalDamageDealt += damage;
            }
            else
            {
                allTimeStats.totalDamageTaken += damage;
                sessionStats.totalDamageTaken += damage;
            }

            OnStatUpdated?.Invoke(StatType.Damage, allTimeStats.totalDamageDealt);
            SaveStatistics();
        }

        public void RecordDistance(float distance, bool running = false)
        {
            if (running)
            {
                allTimeStats.distanceRan += distance;
                sessionStats.distanceRan += distance;
            }
            else
            {
                allTimeStats.distanceWalked += distance;
                sessionStats.distanceWalked += distance;
            }

            OnStatUpdated?.Invoke(StatType.Distance, allTimeStats.distanceWalked + allTimeStats.distanceRan);
        }

        public void RecordGameResult(bool won, float survivalTime)
        {
            allTimeStats.gamesPlayed++;
            sessionStats.gamesPlayed++;

            if (won)
            {
                allTimeStats.gamesWon++;
                sessionStats.gamesWon++;
            }

            if (survivalTime > allTimeStats.longestSurvivalTime)
            {
                allTimeStats.longestSurvivalTime = survivalTime;
            }

            UpdateWinRate();
            SaveStatistics();
        }

        public void RecordWeaponShot(string weaponName, bool hit = false, bool headshot = false, long damage = 0)
        {
            if (!weaponStats.ContainsKey(weaponName))
            {
                weaponStats[weaponName] = new WeaponStats { weaponName = weaponName };
            }

            var stats = weaponStats[weaponName];
            stats.shots++;

            if (hit)
            {
                stats.hits++;
                stats.damageDealt += damage;
            }

            if (headshot)
            {
                stats.headshots++;
            }

            stats.accuracy = stats.shots > 0 ? (float)stats.hits / stats.shots * 100f : 0f;
        }

        private void RecordWeaponKill(string weaponName, bool headshot)
        {
            if (!weaponStats.ContainsKey(weaponName))
            {
                weaponStats[weaponName] = new WeaponStats { weaponName = weaponName };
            }

            weaponStats[weaponName].kills++;
            if (headshot) weaponStats[weaponName].headshots++;
        }

        // Calculations

        private void UpdateKDRatio()
        {
            allTimeStats.kdRatio = allTimeStats.totalDeaths > 0 
                ? (float)allTimeStats.totalKills / allTimeStats.totalDeaths 
                : allTimeStats.totalKills;

            allTimeStats.headshotPercent = allTimeStats.totalKills > 0
                ? (float)allTimeStats.headshots / allTimeStats.totalKills * 100f
                : 0f;
        }

        private void UpdateWinRate()
        {
            allTimeStats.winRate = allTimeStats.gamesPlayed > 0
                ? (float)allTimeStats.gamesWon / allTimeStats.gamesPlayed * 100f
                : 0f;
        }

        // Milestones

        private void CheckMilestones()
        {
            CheckKillMilestones();
            CheckPlayTimeMilestones();
        }

        private void CheckKillMilestones()
        {
            int[] milestones = { 10, 50, 100, 500, 1000, 5000, 10000 };
            foreach (int milestone in milestones)
            {
                if (allTimeStats.totalKills == milestone)
                {
                    allTimeStats.milestonesReached++;
                    OnMilestoneReached?.Invoke($"{milestone} Total Kills");
                }
            }
        }

        private void CheckPlayTimeMilestones()
        {
            float[] milestones = { 3600, 36000, 360000 }; // 1h, 10h, 100h
            foreach (float milestone in milestones)
            {
                if (allTimeStats.totalPlayTime >= milestone && 
                    allTimeStats.totalPlayTime - Time.deltaTime < milestone)
                {
                    allTimeStats.milestonesReached++;
                    OnMilestoneReached?.Invoke($"{milestone/3600}h Played");
                }
            }
        }

        // Queries

        public PlayerStatistics GetAllTimeStats()
        {
            return allTimeStats;
        }

        public PlayerStatistics GetSessionStats()
        {
            return sessionStats;
        }

        public WeaponStats GetWeaponStats(string weaponName)
        {
            return weaponStats.ContainsKey(weaponName) ? weaponStats[weaponName] : null;
        }

        public List<WeaponStats> GetAllWeaponStats()
        {
            return new List<WeaponStats>(weaponStats.Values);
        }

        public WeaponStats GetFavoriteWeapon()
        {
            if (weaponStats.Count == 0) return null;
            return weaponStats.Values.OrderByDescending(w => w.kills).FirstOrDefault();
        }

        public string GetStatsReport()
        {
            return $"=== PLAYER STATISTICS ===\n" +
                   $"K/D Ratio: {allTimeStats.kdRatio:F2}\n" +
                   $"Total Kills: {allTimeStats.totalKills}\n" +
                   $"Headshot %: {allTimeStats.headshotPercent:F1}%\n" +
                   $"Win Rate: {allTimeStats.winRate:F1}%\n" +
                   $"Play Time: {TimeSpan.FromSeconds(allTimeStats.totalPlayTime):hh\\:mm\\:ss}\n" +
                   $"Distance: {(allTimeStats.distanceWalked + allTimeStats.distanceRan)/1000f:F2}km";
        }

        // Save/Load

        private void SaveStatistics()
        {
            string json = JsonUtility.ToJson(allTimeStats, true);
            PlayerPrefs.SetString("Stats_AllTime", json);

            // Save weapon stats
            foreach (var kvp in weaponStats)
            {
                string weaponJson = JsonUtility.ToJson(kvp.Value);
                PlayerPrefs.SetString($"WeaponStats_{kvp.Key}", weaponJson);
            }

            PlayerPrefs.Save();
        }

        private void LoadStatistics()
        {
            string json = PlayerPrefs.GetString("Stats_AllTime", "");
            if (!string.IsNullOrEmpty(json))
            {
                allTimeStats = JsonUtility.FromJson<PlayerStatistics>(json);
            }
            else
            {
                allTimeStats = new PlayerStatistics();
            }

            Debug.Log("[Statistics] Statistics loaded");
        }

        public void ResetSessionStats()
        {
            sessionStats = new PlayerStatistics();
        }

        public void ResetAllStats()
        {
            allTimeStats = new PlayerStatistics();
            sessionStats = new PlayerStatistics();
            weaponStats.Clear();
            SaveStatistics();
            Debug.Log("[Statistics] All statistics reset");
        }
    }
}
