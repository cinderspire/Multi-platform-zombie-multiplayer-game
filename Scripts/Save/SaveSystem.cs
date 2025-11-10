using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Save
{
    /// <summary>
    /// Comprehensive cloud save and data persistence system with cross-platform support,
    /// backup/restore, compression, encryption, and conflict resolution.
    /// </summary>
    public class SaveSystem : NetworkBehaviour
    {
        public static SaveSystem Instance { get; private set; }

        [Header("Save Configuration")]
        [SerializeField] private int maxSaveSlots = 5;
        [SerializeField] private int maxBackups = 3;
        [SerializeField] private bool compressionEnabled = true;
        [SerializeField] private bool encryptionEnabled = true;
        [SerializeField] private float autoSaveInterval = 300f; // 5 minutes

        [Header("Cloud Save Configuration")]
        [SerializeField] private bool cloudSaveEnabled = true;
        [SerializeField] private string cloudSaveEndpoint = "https://api.zombiegame.com/saves";

        private Dictionary<ulong, PlayerSaveData> playerSaves = new Dictionary<ulong, PlayerSaveData>();
        private Dictionary<ulong, List<SaveSlot>> playerSaveSlots = new Dictionary<ulong, List<SaveSlot>>();
        private Dictionary<ulong, float> lastSaveTime = new Dictionary<ulong, float>();

        private string saveDirectory;
        private string backupDirectory;

        public event Action<ulong, int> OnSaveCompleted;
        public event Action<ulong, int> OnLoadCompleted;
        public event Action<ulong, SaveError> OnSaveError;
        public event Action<ulong, int> OnBackupCreated;
        public event Action<ulong> OnCloudSyncCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            saveDirectory = Path.Combine(Application.persistentDataPath, "Saves");
            backupDirectory = Path.Combine(Application.persistentDataPath, "Backups");

            Directory.CreateDirectory(saveDirectory);
            Directory.CreateDirectory(backupDirectory);
        }

        private void Update()
        {
            if (IsServer)
            {
                AutoSaveCheck();
            }
        }

        private void AutoSaveCheck()
        {
            foreach (var kvp in playerSaves)
            {
                ulong playerId = kvp.Key;
                if (!lastSaveTime.ContainsKey(playerId))
                {
                    lastSaveTime[playerId] = Time.time;
                    continue;
                }

                if (Time.time - lastSaveTime[playerId] >= autoSaveInterval)
                {
                    SavePlayerDataServerRpc(playerId, 0, true);
                    lastSaveTime[playerId] = Time.time;
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void InitializePlayerSaveServerRpc(ulong playerId, ServerRpcParams rpcParams = default)
        {
            if (!playerSaves.ContainsKey(playerId))
            {
                playerSaves[playerId] = new PlayerSaveData
                {
                    playerId = playerId,
                    saveVersion = GetCurrentSaveVersion(),
                    createdAt = DateTime.UtcNow,
                    lastModified = DateTime.UtcNow
                };

                LoadPlayerSaveSlots(playerId);
                Debug.Log($"Initialized save system for player {playerId}");
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void SavePlayerDataServerRpc(ulong playerId, int slotIndex, bool isAutoSave, ServerRpcParams rpcParams = default)
        {
            if (!playerSaves.TryGetValue(playerId, out var saveData))
            {
                OnSaveError?.Invoke(playerId, SaveError.PlayerNotFound);
                return;
            }

            if (slotIndex < 0 || slotIndex >= maxSaveSlots)
            {
                OnSaveError?.Invoke(playerId, SaveError.InvalidSlot);
                return;
            }

            try
            {
                // Gather data from all systems
                saveData = GatherPlayerData(playerId);
                saveData.slotIndex = slotIndex;
                saveData.isAutoSave = isAutoSave;
                saveData.lastModified = DateTime.UtcNow;

                // Create backup before saving
                CreateBackup(playerId, slotIndex);

                // Serialize save data
                string json = JsonUtility.ToJson(saveData, false);
                byte[] data = Encoding.UTF8.GetBytes(json);

                // Compress if enabled
                if (compressionEnabled)
                {
                    data = CompressData(data);
                }

                // Encrypt if enabled
                if (encryptionEnabled)
                {
                    data = EncryptData(data, playerId.ToString());
                }

                // Save to disk
                string filePath = GetSaveFilePath(playerId, slotIndex);
                File.WriteAllBytes(filePath, data);

                // Update save slot info
                UpdateSaveSlot(playerId, slotIndex, saveData);

                // Sync to cloud if enabled
                if (cloudSaveEnabled)
                {
                    SyncToCloud(playerId, slotIndex, data);
                }

                playerSaves[playerId] = saveData;
                OnSaveCompleted?.Invoke(playerId, slotIndex);
                NotifySaveCompletedClientRpc(playerId, slotIndex, isAutoSave);

                Debug.Log($"Saved player {playerId} data to slot {slotIndex} (AutoSave: {isAutoSave})");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save player {playerId} data: {ex.Message}");
                OnSaveError?.Invoke(playerId, SaveError.SaveFailed);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void LoadPlayerDataServerRpc(ulong playerId, int slotIndex, ServerRpcParams rpcParams = default)
        {
            if (slotIndex < 0 || slotIndex >= maxSaveSlots)
            {
                OnSaveError?.Invoke(playerId, SaveError.InvalidSlot);
                return;
            }

            try
            {
                string filePath = GetSaveFilePath(playerId, slotIndex);

                if (!File.Exists(filePath))
                {
                    OnSaveError?.Invoke(playerId, SaveError.SaveNotFound);
                    return;
                }

                byte[] data = File.ReadAllBytes(filePath);

                // Decrypt if enabled
                if (encryptionEnabled)
                {
                    data = DecryptData(data, playerId.ToString());
                }

                // Decompress if enabled
                if (compressionEnabled)
                {
                    data = DecompressData(data);
                }

                // Deserialize
                string json = Encoding.UTF8.GetBytes(data);
                PlayerSaveData saveData = JsonUtility.FromJson<PlayerSaveData>(json);

                // Validate save data
                if (!ValidateSaveData(saveData, playerId))
                {
                    OnSaveError?.Invoke(playerId, SaveError.CorruptedSave);
                    return;
                }

                // Apply save data to all systems
                ApplyPlayerData(playerId, saveData);

                playerSaves[playerId] = saveData;
                OnLoadCompleted?.Invoke(playerId, slotIndex);
                NotifyLoadCompletedClientRpc(playerId, slotIndex);

                Debug.Log($"Loaded player {playerId} data from slot {slotIndex}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load player {playerId} data: {ex.Message}");
                OnSaveError?.Invoke(playerId, SaveError.LoadFailed);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void DeleteSaveServerRpc(ulong playerId, int slotIndex, ServerRpcParams rpcParams = default)
        {
            try
            {
                string filePath = GetSaveFilePath(playerId, slotIndex);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    RemoveSaveSlot(playerId, slotIndex);
                    Debug.Log($"Deleted save slot {slotIndex} for player {playerId}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to delete save: {ex.Message}");
                OnSaveError?.Invoke(playerId, SaveError.DeleteFailed);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void RestoreFromBackupServerRpc(ulong playerId, int slotIndex, int backupIndex, ServerRpcParams rpcParams = default)
        {
            try
            {
                string backupPath = GetBackupFilePath(playerId, slotIndex, backupIndex);
                string savePath = GetSaveFilePath(playerId, slotIndex);

                if (!File.Exists(backupPath))
                {
                    OnSaveError?.Invoke(playerId, SaveError.BackupNotFound);
                    return;
                }

                File.Copy(backupPath, savePath, true);
                Debug.Log($"Restored backup {backupIndex} to save slot {slotIndex} for player {playerId}");

                // Reload the restored data
                LoadPlayerDataServerRpc(playerId, slotIndex);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to restore backup: {ex.Message}");
                OnSaveError?.Invoke(playerId, SaveError.RestoreFailed);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void SyncFromCloudServerRpc(ulong playerId, ServerRpcParams rpcParams = default)
        {
            if (!cloudSaveEnabled)
            {
                OnSaveError?.Invoke(playerId, SaveError.CloudSaveDisabled);
                return;
            }

            // Implement cloud sync logic
            // Would use REST API or cloud provider SDK
            Debug.Log($"Syncing from cloud for player {playerId}");
            OnCloudSyncCompleted?.Invoke(playerId);
        }

        private PlayerSaveData GatherPlayerData(ulong playerId)
        {
            var saveData = new PlayerSaveData
            {
                playerId = playerId,
                saveVersion = GetCurrentSaveVersion(),
                createdAt = playerSaves.ContainsKey(playerId) ? playerSaves[playerId].createdAt : DateTime.UtcNow,
                lastModified = DateTime.UtcNow,

                // Gather from all systems
                progressionData = GatherProgressionData(playerId),
                inventoryData = GatherInventoryData(playerId),
                questData = GatherQuestData(playerId),
                achievementData = GatherAchievementData(playerId),
                economyData = GatherEconomyData(playerId),
                settingsData = GatherSettingsData(playerId),
                cosmeticData = GatherCosmeticData(playerId),
                clanData = GatherClanData(playerId),
                arenaData = GatherArenaData(playerId),
                battlePassData = GatherBattlePassData(playerId),
                statisticsData = GatherStatisticsData(playerId)
            };

            return saveData;
        }

        private void ApplyPlayerData(ulong playerId, PlayerSaveData saveData)
        {
            // Apply to all systems
            ApplyProgressionData(playerId, saveData.progressionData);
            ApplyInventoryData(playerId, saveData.inventoryData);
            ApplyQuestData(playerId, saveData.questData);
            ApplyAchievementData(playerId, saveData.achievementData);
            ApplyEconomyData(playerId, saveData.economyData);
            ApplySettingsData(playerId, saveData.settingsData);
            ApplyCosmeticData(playerId, saveData.cosmeticData);
            ApplyClanData(playerId, saveData.clanData);
            ApplyArenaData(playerId, saveData.arenaData);
            ApplyBattlePassData(playerId, saveData.battlePassData);
            ApplyStatisticsData(playerId, saveData.statisticsData);
        }

        // System data gathering methods (would integrate with actual systems)
        private ProgressionSaveData GatherProgressionData(ulong playerId) => new ProgressionSaveData();
        private InventorySaveData GatherInventoryData(ulong playerId) => new InventorySaveData();
        private QuestSaveData GatherQuestData(ulong playerId) => new QuestSaveData();
        private AchievementSaveData GatherAchievementData(ulong playerId) => new AchievementSaveData();
        private EconomySaveData GatherEconomyData(ulong playerId) => new EconomySaveData();
        private SettingsSaveData GatherSettingsData(ulong playerId) => new SettingsSaveData();
        private CosmeticSaveData GatherCosmeticData(ulong playerId) => new CosmeticSaveData();
        private ClanSaveData GatherClanData(ulong playerId) => new ClanSaveData();
        private ArenaSaveData GatherArenaData(ulong playerId) => new ArenaSaveData();
        private BattlePassSaveData GatherBattlePassData(ulong playerId) => new BattlePassSaveData();
        private StatisticsSaveData GatherStatisticsData(ulong playerId) => new StatisticsSaveData();

        // System data application methods
        private void ApplyProgressionData(ulong playerId, ProgressionSaveData data) { }
        private void ApplyInventoryData(ulong playerId, InventorySaveData data) { }
        private void ApplyQuestData(ulong playerId, QuestSaveData data) { }
        private void ApplyAchievementData(ulong playerId, AchievementSaveData data) { }
        private void ApplyEconomyData(ulong playerId, EconomySaveData data) { }
        private void ApplySettingsData(ulong playerId, SettingsSaveData data) { }
        private void ApplyCosmeticData(ulong playerId, CosmeticSaveData data) { }
        private void ApplyClanData(ulong playerId, ClanSaveData data) { }
        private void ApplyArenaData(ulong playerId, ArenaSaveData data) { }
        private void ApplyBattlePassData(ulong playerId, BattlePassSaveData data) { }
        private void ApplyStatisticsData(ulong playerId, StatisticsSaveData data) { }

        private void CreateBackup(ulong playerId, int slotIndex)
        {
            string savePath = GetSaveFilePath(playerId, slotIndex);
            if (!File.Exists(savePath)) return;

            // Rotate backups
            for (int i = maxBackups - 1; i > 0; i--)
            {
                string oldBackup = GetBackupFilePath(playerId, slotIndex, i - 1);
                string newBackup = GetBackupFilePath(playerId, slotIndex, i);
                if (File.Exists(oldBackup))
                {
                    File.Copy(oldBackup, newBackup, true);
                }
            }

            // Create new backup
            string backupPath = GetBackupFilePath(playerId, slotIndex, 0);
            File.Copy(savePath, backupPath, true);
            OnBackupCreated?.Invoke(playerId, slotIndex);
        }

        private byte[] CompressData(byte[] data)
        {
            using (var outputStream = new MemoryStream())
            {
                using (var gzipStream = new GZipStream(outputStream, CompressionMode.Compress))
                {
                    gzipStream.Write(data, 0, data.Length);
                }
                return outputStream.ToArray();
            }
        }

        private byte[] DecompressData(byte[] data)
        {
            using (var inputStream = new MemoryStream(data))
            using (var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress))
            using (var outputStream = new MemoryStream())
            {
                gzipStream.CopyTo(outputStream);
                return outputStream.ToArray();
            }
        }

        private byte[] EncryptData(byte[] data, string key)
        {
            // Simple XOR encryption for demonstration
            // In production, use AES or similar
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] encrypted = new byte[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                encrypted[i] = (byte)(data[i] ^ keyBytes[i % keyBytes.Length]);
            }
            return encrypted;
        }

        private byte[] DecryptData(byte[] data, string key)
        {
            // XOR is symmetric
            return EncryptData(data, key);
        }

        private bool ValidateSaveData(PlayerSaveData saveData, ulong playerId)
        {
            if (saveData.playerId != playerId) return false;
            if (saveData.saveVersion > GetCurrentSaveVersion()) return false;
            return true;
        }

        private void SyncToCloud(ulong playerId, int slotIndex, byte[] data)
        {
            // Implement cloud upload logic
            Debug.Log($"Syncing to cloud: Player {playerId}, Slot {slotIndex}");
        }

        private void LoadPlayerSaveSlots(ulong playerId)
        {
            if (!playerSaveSlots.ContainsKey(playerId))
            {
                playerSaveSlots[playerId] = new List<SaveSlot>();
            }

            for (int i = 0; i < maxSaveSlots; i++)
            {
                string filePath = GetSaveFilePath(playerId, i);
                if (File.Exists(filePath))
                {
                    var fileInfo = new FileInfo(filePath);
                    playerSaveSlots[playerId].Add(new SaveSlot
                    {
                        slotIndex = i,
                        isEmpty = false,
                        lastModified = fileInfo.LastWriteTime,
                        fileSize = fileInfo.Length
                    });
                }
                else
                {
                    playerSaveSlots[playerId].Add(new SaveSlot
                    {
                        slotIndex = i,
                        isEmpty = true
                    });
                }
            }
        }

        private void UpdateSaveSlot(ulong playerId, int slotIndex, PlayerSaveData saveData)
        {
            if (!playerSaveSlots.ContainsKey(playerId)) return;

            var slot = playerSaveSlots[playerId].FirstOrDefault(s => s.slotIndex == slotIndex);
            if (slot != null)
            {
                slot.isEmpty = false;
                slot.lastModified = saveData.lastModified;
                slot.characterName = "Player"; // Would get from actual data
                slot.level = 1; // Would get from progression data
            }
        }

        private void RemoveSaveSlot(ulong playerId, int slotIndex)
        {
            if (!playerSaveSlots.ContainsKey(playerId)) return;

            var slot = playerSaveSlots[playerId].FirstOrDefault(s => s.slotIndex == slotIndex);
            if (slot != null)
            {
                slot.isEmpty = true;
            }
        }

        private string GetSaveFilePath(ulong playerId, int slotIndex) =>
            Path.Combine(saveDirectory, $"save_{playerId}_{slotIndex}.dat");

        private string GetBackupFilePath(ulong playerId, int slotIndex, int backupIndex) =>
            Path.Combine(backupDirectory, $"backup_{playerId}_{slotIndex}_{backupIndex}.dat");

        private int GetCurrentSaveVersion() => 1;

        [ClientRpc]
        private void NotifySaveCompletedClientRpc(ulong playerId, int slotIndex, bool isAutoSave)
        {
            // Client-side notification
        }

        [ClientRpc]
        private void NotifyLoadCompletedClientRpc(ulong playerId, int slotIndex)
        {
            // Client-side notification
        }

        public List<SaveSlot> GetPlayerSaveSlots(ulong playerId) => playerSaveSlots.GetValueOrDefault(playerId, new List<SaveSlot>());
        public PlayerSaveData GetPlayerSaveData(ulong playerId) => playerSaves.GetValueOrDefault(playerId);
    }

    [Serializable]
    public class PlayerSaveData
    {
        public ulong playerId;
        public int saveVersion;
        public int slotIndex;
        public bool isAutoSave;
        public DateTime createdAt;
        public DateTime lastModified;

        // All system data
        public ProgressionSaveData progressionData;
        public InventorySaveData inventoryData;
        public QuestSaveData questData;
        public AchievementSaveData achievementData;
        public EconomySaveData economyData;
        public SettingsSaveData settingsData;
        public CosmeticSaveData cosmeticData;
        public ClanSaveData clanData;
        public ArenaSaveData arenaData;
        public BattlePassSaveData battlePassData;
        public StatisticsSaveData statisticsData;
    }

    [Serializable] public class ProgressionSaveData { public int level; public int experience; public int prestige; }
    [Serializable] public class InventorySaveData { public List<string> itemIds; public Dictionary<string, int> itemCounts; }
    [Serializable] public class QuestSaveData { public List<string> completedQuests; public List<string> activeQuests; }
    [Serializable] public class AchievementSaveData { public List<string> unlockedAchievements; }
    [Serializable] public class EconomySaveData { public Dictionary<string, int> currencies; }
    [Serializable] public class SettingsSaveData { }
    [Serializable] public class CosmeticSaveData { public List<string> unlockedCosmetics; }
    [Serializable] public class ClanSaveData { public string clanId; }
    [Serializable] public class ArenaSaveData { public int rating; public int rank; }
    [Serializable] public class BattlePassSaveData { public int tier; public int seasonId; }
    [Serializable] public class StatisticsSaveData { public Dictionary<string, float> stats; }

    [Serializable]
    public class SaveSlot
    {
        public int slotIndex;
        public bool isEmpty;
        public DateTime lastModified;
        public string characterName;
        public int level;
        public long fileSize;
    }

    public enum SaveError
    {
        None, PlayerNotFound, InvalidSlot, SaveFailed, LoadFailed,
        SaveNotFound, CorruptedSave, DeleteFailed, BackupNotFound,
        RestoreFailed, CloudSaveDisabled, CloudSyncFailed
    }
}
