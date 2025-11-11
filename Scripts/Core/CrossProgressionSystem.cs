using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Core
{
    /// <summary>
    /// Cross-progression and cross-platform system enabling players to sync
    /// their progress, purchases, and unlocks across different platforms.
    /// </summary>
    public class CrossProgressionSystem : MonoBehaviour
    {
        public static CrossProgressionSystem Instance { get; private set; }

        [Header("Cross-Progression Configuration")]
        [SerializeField] private bool enableCrossProgression = true;
        [SerializeField] private bool enableCrossPlatformPlay = true;
        [SerializeField] private float autoSyncInterval = 300f; // 5 minutes

        private PlayerProgressionData currentProgressionData;
        private float lastSyncTime;
        private bool isSyncing = false;

        public event Action OnSyncStarted;
        public event Action OnSyncCompleted;
        public event Action<string> OnSyncFailed;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (enableCrossProgression && Time.time - lastSyncTime > autoSyncInterval)
            {
                AutoSync();
            }
        }

        /// <summary>
        /// Initialize cross-progression for player
        /// </summary>
        public void InitializeCrossProgression(ulong playerId)
        {
            if (!enableCrossProgression) return;

            currentProgressionData = new PlayerProgressionData
            {
                playerId = playerId,
                platform = GetCurrentPlatform(),
                lastSyncTime = DateTime.UtcNow
            };

            LoadProgressionFromCloud(playerId);
        }

        /// <summary>
        /// Sync all player data to cloud
        /// </summary>
        public void SyncToCloud()
        {
            if (!enableCrossProgression || isSyncing) return;

            isSyncing = true;
            OnSyncStarted?.Invoke();

            try
            {
                // Collect all player data
                CollectAllProgressionData();

                // Upload to cloud (would integrate with actual cloud service)
                UploadToCloudService(currentProgressionData);

                currentProgressionData.lastSyncTime = DateTime.UtcNow;
                lastSyncTime = Time.time;

                OnSyncCompleted?.Invoke();
                Debug.Log("<color=lime>Cross-progression synced successfully!</color>");
            }
            catch (Exception e)
            {
                OnSyncFailed?.Invoke(e.Message);
                Debug.LogError($"Cross-progression sync failed: {e.Message}");
            }
            finally
            {
                isSyncing = false;
            }
        }

        /// <summary>
        /// Load player data from cloud
        /// </summary>
        private void LoadProgressionFromCloud(ulong playerId)
        {
            try
            {
                // Download from cloud service
                PlayerProgressionData cloudData = DownloadFromCloudService(playerId);

                if (cloudData != null)
                {
                    // Merge with local data (keep most recent)
                    MergeProgressionData(cloudData);

                    // Apply to local game
                    ApplyProgressionData(cloudData);

                    Debug.Log("<color=cyan>Cross-progression loaded from cloud!</color>");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to load from cloud: {e.Message}. Using local data.");
            }
        }

        private void CollectAllProgressionData()
        {
            if (currentProgressionData == null) return;

            // Collect progression data
            if (Progression.ProgressionSystem.Instance != null)
            {
                // Would collect XP, level, etc.
            }

            // Collect inventory
            if (Inventory.InventorySystem.Instance != null)
            {
                currentProgressionData.inventory = new List<string>(); // Would populate
            }

            // Collect unlocks
            currentProgressionData.unlockedWeapons = new List<string>();
            currentProgressionData.unlockedSkins = new List<string>();
            currentProgressionData.unlockedTitles = new List<string>();

            // Collect achievements
            if (Achievements.AchievementSystem.Instance != null)
            {
                currentProgressionData.completedAchievements = new List<string>();
            }

            // Collect statistics
            if (Statistics.PlayerStatisticsSystem.Instance != null)
            {
                currentProgressionData.totalKills = 0; // Would get actual
                currentProgressionData.totalDeaths = 0;
                currentProgressionData.totalMatches = 0;
            }

            // Collect purchases (important for cross-platform)
            currentProgressionData.purchases = new List<Purchase>();

            // Collect settings
            if (Settings.SettingsSystem.Instance != null)
            {
                // Would save settings
            }
        }

        private void MergeProgressionData(PlayerProgressionData cloudData)
        {
            if (currentProgressionData == null) return;

            // Keep the most recent data for conflicting items
            if (cloudData.lastSyncTime > currentProgressionData.lastSyncTime)
            {
                currentProgressionData = cloudData;
            }
            else
            {
                // Merge unique items from cloud
                MergeUnlocksAndPurchases(cloudData);
            }
        }

        private void MergeUnlocksAndPurchases(PlayerProgressionData cloudData)
        {
            // Merge unlocks (union of both)
            if (cloudData.unlockedWeapons != null)
            {
                foreach (string weapon in cloudData.unlockedWeapons)
                {
                    if (!currentProgressionData.unlockedWeapons.Contains(weapon))
                    {
                        currentProgressionData.unlockedWeapons.Add(weapon);
                    }
                }
            }

            // Merge purchases
            if (cloudData.purchases != null)
            {
                foreach (var purchase in cloudData.purchases)
                {
                    if (!currentProgressionData.purchases.Exists(p => p.itemId == purchase.itemId))
                    {
                        currentProgressionData.purchases.Add(purchase);
                    }
                }
            }
        }

        private void ApplyProgressionData(PlayerProgressionData data)
        {
            // Apply progression
            // Apply inventory
            // Apply unlocks
            // Apply settings
            // etc.
        }

        private void AutoSync()
        {
            if (enableCrossProgression && !isSyncing)
            {
                SyncToCloud();
            }
        }

        /// <summary>
        /// Check if platform supports cross-play
        /// </summary>
        public bool CanCrossPlayWith(Platform otherPlatform)
        {
            if (!enableCrossPlatformPlay) return false;

            Platform currentPlatform = GetCurrentPlatform();

            // All platforms can play together
            return true;
        }

        private Platform GetCurrentPlatform()
        {
            #if UNITY_STANDALONE_WIN
                return Platform.PC;
            #elif UNITY_PS4 || UNITY_PS5
                return Platform.PlayStation;
            #elif UNITY_XBOXONE || UNITY_GAMECORE_XBOXONE || UNITY_GAMECORE_SCARLETT
                return Platform.Xbox;
            #elif UNITY_SWITCH
                return Platform.Switch;
            #elif UNITY_IOS
                return Platform.iOS;
            #elif UNITY_ANDROID
                return Platform.Android;
            #else
                return Platform.PC;
            #endif
        }

        // Placeholder methods for cloud service integration
        private void UploadToCloudService(PlayerProgressionData data)
        {
            // Would integrate with actual cloud service (AWS, Azure, etc.)
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString($"CloudData_{data.playerId}", json);
            PlayerPrefs.Save();
        }

        private PlayerProgressionData DownloadFromCloudService(ulong playerId)
        {
            // Would download from actual cloud service
            string json = PlayerPrefs.GetString($"CloudData_{playerId}", "");
            if (string.IsNullOrEmpty(json)) return null;

            return JsonUtility.FromJson<PlayerProgressionData>(json);
        }

        public bool IsProgressionSynced()
        {
            return currentProgressionData != null &&
                   (DateTime.UtcNow - currentProgressionData.lastSyncTime).TotalMinutes < 10;
        }

        public DateTime GetLastSyncTime()
        {
            return currentProgressionData?.lastSyncTime ?? DateTime.MinValue;
        }

        [Serializable]
        public class PlayerProgressionData
        {
            public ulong playerId;
            public Platform platform;
            public DateTime lastSyncTime;

            // Progression
            public int level;
            public int totalXP;
            public int prestigeRank;

            // Unlocks
            public List<string> unlockedWeapons;
            public List<string> unlockedSkins;
            public List<string> unlockedTitles;
            public List<string> completedAchievements;

            // Inventory
            public List<string> inventory;
            public int softCurrency;
            public int hardCurrency;

            // Statistics
            public int totalKills;
            public int totalDeaths;
            public int totalMatches;
            public int totalWins;

            // Purchases (important for cross-platform)
            public List<Purchase> purchases;

            // Settings
            public Dictionary<string, string> settings;
        }

        [Serializable]
        public class Purchase
        {
            public string itemId;
            public DateTime purchaseDate;
            public string platform;
            public bool isPermanent;
        }

        public enum Platform
        {
            PC,
            PlayStation,
            Xbox,
            Switch,
            iOS,
            Android,
            Unknown
        }
    }
}
