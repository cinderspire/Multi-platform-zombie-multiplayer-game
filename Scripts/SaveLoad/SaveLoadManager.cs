using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

namespace ZombieGame
{
    /// <summary>
    /// Save/Load Manager - Local save game management
    /// Features: Multiple save slots, auto-save, quick save/load, backup system
    /// Complements cloud save system with local reliability
    /// </summary>
    public class SaveLoadManager : MonoBehaviour
    {
        public static SaveLoadManager Instance { get; private set; }

        [Header("Save Settings")]
        [SerializeField] private int maxSaveSlots = 10;
        [SerializeField] private bool enableAutoSave = true;
        [SerializeField] private float autoSaveInterval = 300f; // 5 minutes
        [SerializeField] private bool createBackups = true;
        [SerializeField] private int maxBackups = 3;

        private string saveDirectory;
        private float lastAutoSaveTime = 0f;
        private Dictionary<int, SaveSlot> saveSlots = new Dictionary<int, SaveSlot>();

        // Events
        public event Action<int> OnGameSaved;
        public event Action<int> OnGameLoaded;
        public event Action<string> OnSaveError;

        [Serializable]
        public class SaveSlot
        {
            public int slotNumber;
            public string saveName;
            public DateTime saveTime;
            public SaveData data;
            public string thumbnail;
            public int playTimeSeconds;
            public int playerLevel;
            public string currentMap;
            public bool isAutoSave;
        }

        [Serializable]
        public class SaveData
        {
            public string version = "1.0";
            public DateTime timestamp;
            
            // Player Data
            public PlayerData player;
            public InventoryData inventory;
            public ProgressData progress;
            public SettingsData settings;
            public StatisticsData statistics;
        }

        [Serializable]
        public class PlayerData
        {
            public string playerName;
            public int level;
            public float experience;
            public Vector3 position;
            public Quaternion rotation;
            public float health;
            public float stamina;
            public string currentWeapon;
            public List<string> unlockedAbilities = new List<string>();
        }

        [Serializable]
        public class InventoryData
        {
            public Dictionary<string, int> items = new Dictionary<string, int>();
            public Dictionary<string, int> ammo = new Dictionary<string, int>();
            public List<string> weapons = new List<string>();
            public Dictionary<string, bool> keycards = new Dictionary<string, bool>();
        }

        [Serializable]
        public class ProgressData
        {
            public List<string> completedQuests = new List<string>();
            public List<string> activeQuests = new List<string>();
            public Dictionary<string, bool> unlockedAreas = new Dictionary<string, bool>();
            public Dictionary<string, bool> discoveredLocations = new Dictionary<string, bool>();
            public List<string> collectedItems = new List<string>();
        }

        [Serializable]
        public class SettingsData
        {
            public Dictionary<string, object> customSettings = new Dictionary<string, object>();
        }

        [Serializable]
        public class StatisticsData
        {
            public int totalKills;
            public int totalDeaths;
            public float totalPlayTime;
            public int zombiesKilled;
            public int headshots;
            public float distanceTraveled;
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeSaveSystem();
                LoadSaveSlotInfo();
            }
            else { Destroy(gameObject); }
        }

        private void Update()
        {
            if (enableAutoSave && Time.time - lastAutoSaveTime >= autoSaveInterval)
            {
                AutoSave();
                lastAutoSaveTime = Time.time;
            }
        }

        private void InitializeSaveSystem()
        {
            saveDirectory = Path.Combine(Application.persistentDataPath, "SaveGames");
            Directory.CreateDirectory(saveDirectory);

            Debug.Log($"[SaveLoad] Save directory: {saveDirectory}");
        }

        // Save Operations

        public void SaveGame(int slotNumber, string saveName = "")
        {
            if (slotNumber < 0 || slotNumber >= maxSaveSlots)
            {
                OnSaveError?.Invoke($"Invalid slot number: {slotNumber}");
                return;
            }

            try
            {
                var saveData = GatherSaveData();
                var saveSlot = new SaveSlot
                {
                    slotNumber = slotNumber,
                    saveName = string.IsNullOrEmpty(saveName) ? $"Save {slotNumber + 1}" : saveName,
                    saveTime = DateTime.Now,
                    data = saveData,
                    playTimeSeconds = (int)Time.time,
                    isAutoSave = false
                };

                // Create backup if enabled
                if (createBackups)
                {
                    CreateBackup(slotNumber);
                }

                // Save to file
                string json = JsonUtility.ToJson(saveSlot, true);
                string filePath = GetSaveFilePath(slotNumber);
                File.WriteAllText(filePath, json);

                saveSlots[slotNumber] = saveSlot;
                OnGameSaved?.Invoke(slotNumber);

                Debug.Log($"[SaveLoad] Game saved to slot {slotNumber}: {saveName}");
            }
            catch (Exception e)
            {
                OnSaveError?.Invoke($"Save failed: {e.Message}");
                Debug.LogError($"[SaveLoad] Save error: {e.Message}");
            }
        }

        public void QuickSave()
        {
            SaveGame(0, "Quick Save");
        }

        private void AutoSave()
        {
            try
            {
                var saveData = GatherSaveData();
                var saveSlot = new SaveSlot
                {
                    slotNumber = -1, // Special auto-save slot
                    saveName = "Auto Save",
                    saveTime = DateTime.Now,
                    data = saveData,
                    isAutoSave = true
                };

                string json = JsonUtility.ToJson(saveSlot, true);
                string filePath = Path.Combine(saveDirectory, "autosave.json");
                File.WriteAllText(filePath, json);

                Debug.Log("[SaveLoad] Auto-save completed");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveLoad] Auto-save failed: {e.Message}");
            }
        }

        // Load Operations

        public bool LoadGame(int slotNumber)
        {
            if (slotNumber < 0 || slotNumber >= maxSaveSlots)
            {
                OnSaveError?.Invoke($"Invalid slot number: {slotNumber}");
                return false;
            }

            try
            {
                string filePath = GetSaveFilePath(slotNumber);
                if (!File.Exists(filePath))
                {
                    OnSaveError?.Invoke($"Save file not found for slot {slotNumber}");
                    return false;
                }

                string json = File.ReadAllText(filePath);
                var saveSlot = JsonUtility.FromJson<SaveSlot>(json);

                ApplySaveData(saveSlot.data);

                OnGameLoaded?.Invoke(slotNumber);
                Debug.Log($"[SaveLoad] Game loaded from slot {slotNumber}");
                return true;
            }
            catch (Exception e)
            {
                OnSaveError?.Invoke($"Load failed: {e.Message}");
                Debug.LogError($"[SaveLoad] Load error: {e.Message}");
                return false;
            }
        }

        public bool QuickLoad()
        {
            return LoadGame(0);
        }

        public bool LoadAutoSave()
        {
            try
            {
                string filePath = Path.Combine(saveDirectory, "autosave.json");
                if (!File.Exists(filePath))
                {
                    return false;
                }

                string json = File.ReadAllText(filePath);
                var saveSlot = JsonUtility.FromJson<SaveSlot>(json);

                ApplySaveData(saveSlot.data);

                Debug.Log("[SaveLoad] Auto-save loaded");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveLoad] Auto-save load error: {e.Message}");
                return false;
            }
        }

        // Delete Operations

        public void DeleteSave(int slotNumber)
        {
            string filePath = GetSaveFilePath(slotNumber);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                saveSlots.Remove(slotNumber);
                Debug.Log($"[SaveLoad] Deleted save slot {slotNumber}");
            }
        }

        public void DeleteAllSaves()
        {
            for (int i = 0; i < maxSaveSlots; i++)
            {
                DeleteSave(i);
            }
            Debug.Log("[SaveLoad] All saves deleted");
        }

        // Backup System

        private void CreateBackup(int slotNumber)
        {
            string filePath = GetSaveFilePath(slotNumber);
            if (!File.Exists(filePath)) return;

            string backupDir = Path.Combine(saveDirectory, "Backups");
            Directory.CreateDirectory(backupDir);

            // Create timestamped backup
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string backupPath = Path.Combine(backupDir, $"save_{slotNumber}_{timestamp}.bak");

            File.Copy(filePath, backupPath);

            // Clean old backups
            CleanOldBackups(slotNumber);
        }

        private void CleanOldBackups(int slotNumber)
        {
            string backupDir = Path.Combine(saveDirectory, "Backups");
            if (!Directory.Exists(backupDir)) return;

            var backups = new List<FileInfo>();
            var dirInfo = new DirectoryInfo(backupDir);

            foreach (var file in dirInfo.GetFiles($"save_{slotNumber}_*.bak"))
            {
                backups.Add(file);
            }

            backups.Sort((a, b) => b.CreationTime.CompareTo(a.CreationTime));

            // Keep only maxBackups
            for (int i = maxBackups; i < backups.Count; i++)
            {
                backups[i].Delete();
            }
        }

        // Data Gathering

        private SaveData GatherSaveData()
        {
            return new SaveData
            {
                timestamp = DateTime.Now,
                player = GatherPlayerData(),
                inventory = GatherInventoryData(),
                progress = GatherProgressData(),
                settings = new SettingsData(),
                statistics = GatherStatisticsData()
            };
        }

        private PlayerData GatherPlayerData()
        {
            // Would gather from actual player system
            return new PlayerData
            {
                playerName = "Player",
                level = 1,
                experience = 0f,
                position = Vector3.zero,
                rotation = Quaternion.identity,
                health = 100f,
                stamina = 100f
            };
        }

        private InventoryData GatherInventoryData()
        {
            return new InventoryData();
        }

        private ProgressData GatherProgressData()
        {
            return new ProgressData();
        }

        private StatisticsData GatherStatisticsData()
        {
            return new StatisticsData
            {
                totalPlayTime = Time.time
            };
        }

        // Data Application

        private void ApplySaveData(SaveData data)
        {
            // Would apply to actual game systems
            Debug.Log($"[SaveLoad] Applying save data from {data.timestamp}");
        }

        // Utility

        private string GetSaveFilePath(int slotNumber)
        {
            return Path.Combine(saveDirectory, $"save_{slotNumber}.json");
        }

        public SaveSlot GetSaveSlotInfo(int slotNumber)
        {
            if (saveSlots.ContainsKey(slotNumber))
            {
                return saveSlots[slotNumber];
            }

            string filePath = GetSaveFilePath(slotNumber);
            if (!File.Exists(filePath)) return null;

            try
            {
                string json = File.ReadAllText(filePath);
                return JsonUtility.FromJson<SaveSlot>(json);
            }
            catch
            {
                return null;
            }
        }

        public List<SaveSlot> GetAllSaveSlots()
        {
            var slots = new List<SaveSlot>();
            for (int i = 0; i < maxSaveSlots; i++)
            {
                var slot = GetSaveSlotInfo(i);
                if (slot != null) slots.Add(slot);
            }
            return slots;
        }

        private void LoadSaveSlotInfo()
        {
            for (int i = 0; i < maxSaveSlots; i++)
            {
                var slot = GetSaveSlotInfo(i);
                if (slot != null)
                {
                    saveSlots[i] = slot;
                }
            }

            Debug.Log($"[SaveLoad] Loaded {saveSlots.Count} save slots");
        }

        public bool HasSaveInSlot(int slotNumber)
        {
            return File.Exists(GetSaveFilePath(slotNumber));
        }

        public void SetAutoSave(bool enabled)
        {
            enableAutoSave = enabled;
            PlayerPrefs.SetInt("SaveLoad_AutoSave", enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void SetAutoSaveInterval(float interval)
        {
            autoSaveInterval = Mathf.Clamp(interval, 60f, 600f);
            PlayerPrefs.SetFloat("SaveLoad_AutoSaveInterval", autoSaveInterval);
            PlayerPrefs.Save();
        }
    }
}
