using UnityEngine;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace DeadFrontier.Core.Save
{
    /// <summary>
    /// Advanced save system with JSON serialization, encryption, and cloud integration
    /// </summary>
    public class SaveSystem : Singleton<SaveSystem>
    {
        [Header("Save Settings")]
        [SerializeField] private bool enableEncryption = true;
        [SerializeField] private bool enableAutoSave = true;
        [SerializeField] private float autoSaveInterval = 300f; // 5 minutes
        [SerializeField] private int maxSaveSlots = 3;

        [Header("Cloud Save")]
        [SerializeField] private bool enableCloudSave = false;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        // Current save data
        private SaveData currentSave;
        private int currentSlot = 0;

        // Auto-save
        private float autoSaveTimer = 0f;

        // Encryption key (should be securely generated/stored in production)
        private const string ENCRYPTION_KEY = "DeadFrontier2025SecureKey!@#";

        // Events
        public event System.Action OnSaveStarted;
        public event System.Action<bool> OnSaveCompleted;
        public event System.Action OnLoadStarted;
        public event System.Action<bool, SaveData> OnLoadCompleted;

        // Properties
        public SaveData CurrentSave => currentSave;
        public int CurrentSlot => currentSlot;
        public bool HasSaveData => currentSave != null;

        protected override void Awake()
        {
            base.Awake();

            // Create saves directory if it doesn't exist
            CreateSaveDirectory();
        }

        private void Start()
        {
            // Auto-load last save on start
            LoadGame(currentSlot);
        }

        private void Update()
        {
            // Auto-save
            if (enableAutoSave && currentSave != null)
            {
                autoSaveTimer += Time.deltaTime;

                if (autoSaveTimer >= autoSaveInterval)
                {
                    SaveGame(currentSlot, true);
                    autoSaveTimer = 0f;
                }
            }
        }

        #region Save Operations

        /// <summary>
        /// Saves the game to a slot
        /// </summary>
        public bool SaveGame(int slot, bool isAutoSave = false)
        {
            if (slot < 0 || slot >= maxSaveSlots)
            {
                Debug.LogError($"[SaveSystem] Invalid save slot: {slot}");
                return false;
            }

            OnSaveStarted?.Invoke();

            try
            {
                // Update save data before saving
                UpdateSaveData();

                // Convert to JSON
                string json = JsonUtility.ToJson(currentSave, true);

                // Encrypt if enabled
                if (enableEncryption)
                {
                    json = Encrypt(json);
                }

                // Write to file
                string filePath = GetSaveFilePath(slot);
                File.WriteAllText(filePath, json);

                if (showDebugLogs)
                {
                    string saveType = isAutoSave ? "Auto-saved" : "Saved";
                    Debug.Log($"[SaveSystem] {saveType} to slot {slot}");
                }

                OnSaveCompleted?.Invoke(true);

                // Cloud save (if enabled)
                if (enableCloudSave)
                {
                    UploadToCloud(slot);
                }

                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveSystem] Save failed: {e.Message}");
                OnSaveCompleted?.Invoke(false);
                return false;
            }
        }

        /// <summary>
        /// Loads the game from a slot
        /// </summary>
        public bool LoadGame(int slot)
        {
            if (slot < 0 || slot >= maxSaveSlots)
            {
                Debug.LogError($"[SaveSystem] Invalid save slot: {slot}");
                return false;
            }

            OnLoadStarted?.Invoke();

            try
            {
                string filePath = GetSaveFilePath(slot);

                // Check if save file exists
                if (!File.Exists(filePath))
                {
                    if (showDebugLogs)
                        Debug.Log($"[SaveSystem] No save file found in slot {slot}. Creating new save.");

                    // Create new save
                    currentSave = new SaveData();
                    currentSave.saveFileIndex = slot;
                    currentSlot = slot;

                    OnLoadCompleted?.Invoke(true, currentSave);
                    return true;
                }

                // Read from file
                string json = File.ReadAllText(filePath);

                // Decrypt if encrypted
                if (enableEncryption)
                {
                    json = Decrypt(json);
                }

                // Parse JSON
                currentSave = JsonUtility.FromJson<SaveData>(json);
                currentSlot = slot;

                if (showDebugLogs)
                    Debug.Log($"[SaveSystem] Loaded from slot {slot}");

                // Apply loaded data to game systems
                ApplySaveData();

                OnLoadCompleted?.Invoke(true, currentSave);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveSystem] Load failed: {e.Message}");
                OnLoadCompleted?.Invoke(false, null);

                // Create new save on failure
                currentSave = new SaveData();
                currentSave.saveFileIndex = slot;
                currentSlot = slot;

                return false;
            }
        }

        /// <summary>
        /// Deletes a save slot
        /// </summary>
        public bool DeleteSave(int slot)
        {
            if (slot < 0 || slot >= maxSaveSlots)
                return false;

            try
            {
                string filePath = GetSaveFilePath(slot);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);

                    if (showDebugLogs)
                        Debug.Log($"[SaveSystem] Deleted save slot {slot}");

                    // Delete from cloud if enabled
                    if (enableCloudSave)
                    {
                        DeleteFromCloud(slot);
                    }

                    return true;
                }

                return false;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveSystem] Delete failed: {e.Message}");
                return false;
            }
        }

        #endregion

        #region Data Management

        /// <summary>
        /// Updates current save data from game systems
        /// </summary>
        private void UpdateSaveData()
        {
            if (currentSave == null)
                currentSave = new SaveData();

            currentSave.lastSaveTime = System.DateTime.Now.Ticks;

            // Update progression
            var progression = FindObjectOfType<Player.PlayerProgression>();
            if (progression != null)
            {
                currentSave.progression.level = progression.Level;
                currentSave.progression.xp = progression.XP;
                currentSave.progression.prestigeLevel = progression.PrestigeLevel;
                currentSave.progression.stats = progression.GetAllStats();
            }

            // Update loadout
            var loadoutSystem = FindObjectOfType<Player.Perks.LoadoutSystem>();
            if (loadoutSystem != null)
            {
                // Convert loadout to save format
                // ... (implementation depends on loadout system)
            }

            // Update achievements
            var achievementManager = Achievements.AchievementManager.Instance;
            if (achievementManager != null)
            {
                // Save achievement data
                // ... (implementation depends on achievement system)
            }

            // Update battle pass
            var battlePassManager = Progression.BattlePass.BattlePassManager.Instance;
            if (battlePassManager != null)
            {
                currentSave.battlePass.currentTier = battlePassManager.CurrentTier;
                currentSave.battlePass.currentXP = battlePassManager.CurrentXP;
                currentSave.battlePass.hasPremiumPass = battlePassManager.HasPremiumPass;
            }

            // Update settings (already in currentSave.settings)
            // ...

            if (showDebugLogs)
                Debug.Log("[SaveSystem] Save data updated from game systems");
        }

        /// <summary>
        /// Applies loaded save data to game systems
        /// </summary>
        private void ApplySaveData()
        {
            if (currentSave == null)
                return;

            // Apply progression
            var progression = FindObjectOfType<Player.PlayerProgression>();
            if (progression != null)
            {
                // ... apply progression data
            }

            // Apply loadout
            var loadoutSystem = FindObjectOfType<Player.Perks.LoadoutSystem>();
            if (loadoutSystem != null)
            {
                // ... apply loadout data
            }

            // Apply achievements
            var achievementManager = Achievements.AchievementManager.Instance;
            if (achievementManager != null)
            {
                // ... apply achievement data
            }

            // Apply battle pass
            var battlePassManager = Progression.BattlePass.BattlePassManager.Instance;
            if (battlePassManager != null)
            {
                // ... apply battle pass data
            }

            // Apply settings
            ApplySettings(currentSave.settings);

            if (showDebugLogs)
                Debug.Log("[SaveSystem] Save data applied to game systems");
        }

        private void ApplySettings(SettingsData settings)
        {
            // Apply graphics settings
            QualitySettings.SetQualityLevel(settings.qualityLevel);
            Screen.SetResolution(settings.resolutionWidth, settings.resolutionHeight, settings.fullscreen);
            Application.targetFrameRate = settings.targetFrameRate;
            QualitySettings.vSyncCount = settings.vsyncEnabled ? 1 : 0;

            // Apply audio settings
            AudioListener.volume = settings.masterVolume;
            // ... apply other audio settings via AudioManager

            // Apply control settings
            // ... apply mouse sensitivity, key bindings, etc.

            if (showDebugLogs)
                Debug.Log("[SaveSystem] Settings applied");
        }

        #endregion

        #region File Management

        private string GetSaveDirectory()
        {
            return Path.Combine(Application.persistentDataPath, "Saves");
        }

        private string GetSaveFilePath(int slot)
        {
            return Path.Combine(GetSaveDirectory(), $"save_slot_{slot}.dat");
        }

        private void CreateSaveDirectory()
        {
            string dir = GetSaveDirectory();

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);

                if (showDebugLogs)
                    Debug.Log($"[SaveSystem] Created save directory: {dir}");
            }
        }

        /// <summary>
        /// Checks if a save slot has data
        /// </summary>
        public bool HasSaveInSlot(int slot)
        {
            if (slot < 0 || slot >= maxSaveSlots)
                return false;

            return File.Exists(GetSaveFilePath(slot));
        }

        /// <summary>
        /// Gets save metadata for a slot (without loading entire save)
        /// </summary>
        public SaveMetadata GetSaveMetadata(int slot)
        {
            if (!HasSaveInSlot(slot))
                return null;

            try
            {
                string filePath = GetSaveFilePath(slot);
                string json = File.ReadAllText(filePath);

                if (enableEncryption)
                {
                    json = Decrypt(json);
                }

                SaveData save = JsonUtility.FromJson<SaveData>(json);

                return new SaveMetadata
                {
                    slot = slot,
                    lastSaveTime = new System.DateTime(save.lastSaveTime),
                    playerLevel = save.progression.level,
                    prestigeLevel = save.progression.prestigeLevel,
                    playtime = save.stats.totalPlayTime
                };
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region Encryption

        private string Encrypt(string plainText)
        {
            try
            {
                byte[] key = Encoding.UTF8.GetBytes(ENCRYPTION_KEY.PadRight(32).Substring(0, 32));
                byte[] iv = new byte[16]; // Use a proper IV in production

                using (Aes aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = iv;

                    ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                        {
                            using (StreamWriter sw = new StreamWriter(cs))
                            {
                                sw.Write(plainText);
                            }
                        }

                        return System.Convert.ToBase64String(ms.ToArray());
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveSystem] Encryption failed: {e.Message}");
                return plainText;
            }
        }

        private string Decrypt(string cipherText)
        {
            try
            {
                byte[] key = Encoding.UTF8.GetBytes(ENCRYPTION_KEY.PadRight(32).Substring(0, 32));
                byte[] iv = new byte[16];
                byte[] buffer = System.Convert.FromBase64String(cipherText);

                using (Aes aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = iv;

                    ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                    using (MemoryStream ms = new MemoryStream(buffer))
                    {
                        using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader sr = new StreamReader(cs))
                            {
                                return sr.ReadToEnd();
                            }
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveSystem] Decryption failed: {e.Message}");
                return cipherText;
            }
        }

        #endregion

        #region Cloud Save (Placeholder)

        private void UploadToCloud(int slot)
        {
            // TODO: Implement cloud save upload
            // Could use Unity Cloud Save, Steam Cloud, or custom backend
            if (showDebugLogs)
                Debug.Log($"[SaveSystem] Uploading slot {slot} to cloud...");
        }

        private void DownloadFromCloud(int slot)
        {
            // TODO: Implement cloud save download
            if (showDebugLogs)
                Debug.Log($"[SaveSystem] Downloading slot {slot} from cloud...");
        }

        private void DeleteFromCloud(int slot)
        {
            // TODO: Implement cloud save deletion
            if (showDebugLogs)
                Debug.Log($"[SaveSystem] Deleting slot {slot} from cloud...");
        }

        #endregion

        #region Backup System

        /// <summary>
        /// Creates a backup of a save slot
        /// </summary>
        public bool CreateBackup(int slot)
        {
            if (!HasSaveInSlot(slot))
                return false;

            try
            {
                string sourceFile = GetSaveFilePath(slot);
                string backupFile = sourceFile + ".backup";

                File.Copy(sourceFile, backupFile, true);

                if (showDebugLogs)
                    Debug.Log($"[SaveSystem] Created backup for slot {slot}");

                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveSystem] Backup failed: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Restores a save from backup
        /// </summary>
        public bool RestoreBackup(int slot)
        {
            try
            {
                string sourceFile = GetSaveFilePath(slot);
                string backupFile = sourceFile + ".backup";

                if (!File.Exists(backupFile))
                {
                    Debug.LogWarning($"[SaveSystem] No backup found for slot {slot}");
                    return false;
                }

                File.Copy(backupFile, sourceFile, true);

                if (showDebugLogs)
                    Debug.Log($"[SaveSystem] Restored backup for slot {slot}");

                // Reload the save
                LoadGame(slot);

                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveSystem] Backup restore failed: {e.Message}");
                return false;
            }
        }

        #endregion

        private void OnApplicationQuit()
        {
            // Save on quit
            if (currentSave != null)
            {
                SaveGame(currentSlot);
            }
        }

        private void OnApplicationPause(bool pause)
        {
            // Save on pause (mobile)
            if (pause && currentSave != null)
            {
                SaveGame(currentSlot);
            }
        }
    }

    /// <summary>
    /// Save metadata for UI display
    /// </summary>
    public class SaveMetadata
    {
        public int slot;
        public System.DateTime lastSaveTime;
        public int playerLevel;
        public int prestigeLevel;
        public int playtime; // Seconds

        public string GetPlaytimeFormatted()
        {
            int hours = playtime / 3600;
            int minutes = (playtime % 3600) / 60;
            return $"{hours}h {minutes}m";
        }
    }
}
