using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace DeadFrontier.Quests
{
    /// <summary>
    /// Comprehensive quest and mission system with story progression, daily quests, and rewards.
    /// Provides narrative structure and additional gameplay objectives beyond extraction.
    /// </summary>
    public class QuestSystem : MonoBehaviour
    {
        public static QuestSystem Instance { get; private set; }

        [Header("Quest Settings")]
        [SerializeField] private int maxActiveQuests = 5;
        [SerializeField] private int maxDailyQuests = 3;
        [SerializeField] private float dailyQuestResetTime = 24f; // Hours

        [Header("Quest Database")]
        [SerializeField] private QuestData[] allQuests;

        // Active quests
        private Dictionary<string, ActiveQuest> activeQuests = new Dictionary<string, ActiveQuest>();
        private Dictionary<string, Quest Data> completedQuests = new Dictionary<string, QuestData>();
        private List<QuestData> availableQuests = new List<QuestData>();
        private List<QuestData> dailyQuests = new List<QuestData>();

        // Daily quest timer
        private float lastDailyQuestReset;

        // Events
        public event Action<QuestData> OnQuestAccepted;
        public event Action<QuestData, float> OnQuestProgress;
        public event Action<QuestData> OnQuestCompleted;
        public event Action<QuestData> OnQuestFailed;
        public event Action<List<QuestData>> OnDailyQuestsRefreshed;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            LoadQuestProgress();
            RefreshDailyQuests();
        }

        private void Update()
        {
            CheckDailyQuestReset();
            UpdateActiveQuests();
        }

        #region Quest Management

        public bool CanAcceptQuest(QuestData quest)
        {
            if (quest == null) return false;
            if (activeQuests.Count >= maxActiveQuests) return false;
            if (activeQuests.ContainsKey(quest.questId)) return false;
            if (completedQuests.ContainsKey(quest.questId) && !quest.isRepeatable) return false;

            // Check level requirement
            if (Progression.AchievementManager.Instance != null)
            {
                int playerLevel = Progression.AchievementManager.Instance.GetPlayerLevel();
                if (playerLevel < quest.requiredLevel) return false;
            }

            // Check prerequisites
            if (quest.prerequisiteQuestIds != null)
            {
                foreach (string prereqId in quest.prerequisiteQuestIds)
                {
                    if (!completedQuests.ContainsKey(prereqId))
                        return false;
                }
            }

            return true;
        }

        public bool AcceptQuest(string questId)
        {
            var quest = GetQuestById(questId);
            if (!CanAcceptQuest(quest)) return false;

            var activeQuest = new ActiveQuest
            {
                questData = quest,
                startTime = DateTime.UtcNow,
                currentProgress = new Dictionary<string, float>()
            };

            // Initialize objective progress
            foreach (var objective in quest.objectives)
            {
                activeQuest.currentProgress[objective.objectiveId] = 0f;
            }

            activeQuests[questId] = activeQuest;
            OnQuestAccepted?.Invoke(quest);

            SaveQuestProgress();

            Debug.Log($"[QuestSystem] Accepted quest: {quest.questName}");
            return true;
        }

        public void AbandonQuest(string questId)
        {
            if (!activeQuests.ContainsKey(questId)) return;

            var quest = activeQuests[questId].questData;
            activeQuests.Remove(questId);

            SaveQuestProgress();

            Debug.Log($"[QuestSystem] Abandoned quest: {quest.questName}");
        }

        private void UpdateActiveQuests()
        {
            var questsToComplete = new List<string>();

            foreach (var kvp in activeQuests)
            {
                var activeQuest = kvp.Value;
                var quest = activeQuest.questData;

                // Check if quest is completed
                if (IsQuestCompleted(activeQuest))
                {
                    questsToComplete.Add(kvp.Key);
                }

                // Check time-based failure
                if (quest.hasTimeLimit)
                {
                    var elapsed = DateTime.UtcNow - activeQuest.startTime;
                    if (elapsed.TotalSeconds > quest.timeLimitSeconds)
                    {
                        FailQuest(kvp.Key);
                    }
                }
            }

            // Complete quests
            foreach (var questId in questsToComplete)
            {
                CompleteQuest(questId);
            }
        }

        private bool IsQuestCompleted(ActiveQuest activeQuest)
        {
            foreach (var objective in activeQuest.questData.objectives)
            {
                if (!activeQuest.currentProgress.ContainsKey(objective.objectiveId))
                    return false;

                if (activeQuest.currentProgress[objective.objectiveId] < objective.requiredAmount)
                    return false;
            }

            return true;
        }

        private void CompleteQuest(string questId)
        {
            if (!activeQuests.ContainsKey(questId)) return;

            var activeQuest = activeQuests[questId];
            var quest = activeQuest.questData;

            // Award rewards
            AwardQuestRewards(quest);

            // Mark as completed
            completedQuests[questId] = quest;
            activeQuests.Remove(questId);

            OnQuestCompleted?.Invoke(quest);

            SaveQuestProgress();

            Debug.Log($"[QuestSystem] Completed quest: {quest.questName}");
        }

        private void FailQuest(string questId)
        {
            if (!activeQuests.ContainsKey(questId)) return;

            var quest = activeQuests[questId].questData;
            activeQuests.Remove(questId);

            OnQuestFailed?.Invoke(quest);

            SaveQuestProgress();

            Debug.Log($"[QuestSystem] Failed quest: {quest.questName}");
        }

        #endregion

        #region Quest Progress

        public void UpdateQuestProgress(QuestObjectiveType objectiveType, string targetId, float amount)
        {
            foreach (var kvp in activeQuests)
            {
                var activeQuest = kvp.Value;
                var quest = activeQuest.questData;

                foreach (var objective in quest.objectives)
                {
                    if (objective.objectiveType == objectiveType)
                    {
                        // Check if target matches
                        bool targetMatches = string.IsNullOrEmpty(objective.targetId) || objective.targetId == targetId;

                        if (targetMatches)
                        {
                            float currentProgress = activeQuest.currentProgress[objective.objectiveId];
                            float newProgress = Mathf.Min(currentProgress + amount, objective.requiredAmount);

                            activeQuest.currentProgress[objective.objectiveId] = newProgress;

                            float progressPercent = newProgress / objective.requiredAmount;
                            OnQuestProgress?.Invoke(quest, progressPercent);

                            SaveQuestProgress();
                        }
                    }
                }
            }
        }

        public void UpdateQuestProgressOverride(string questId, string objectiveId, float value)
        {
            if (!activeQuests.ContainsKey(questId)) return;

            var activeQuest = activeQuests[questId];
            if (!activeQuest.currentProgress.ContainsKey(objectiveId)) return;

            activeQuest.currentProgress[objectiveId] = value;

            float progressPercent = value / activeQuest.questData.objectives.First(o => o.objectiveId == objectiveId).requiredAmount;
            OnQuestProgress?.Invoke(activeQuest.questData, progressPercent);

            SaveQuestProgress();
        }

        #endregion

        #region Daily Quests

        private void CheckDailyQuestReset()
        {
            float timeSinceReset = Time.realtimeSinceStartup - lastDailyQuestReset;

            if (timeSinceReset >= dailyQuestResetTime * 3600f) // Convert hours to seconds
            {
                RefreshDailyQuests();
            }
        }

        private void RefreshDailyQuests()
        {
            dailyQuests.Clear();

            // Get all daily quest candidates
            var dailyCandidates = allQuests.Where(q => q.questType == QuestType.Daily).ToList();

            // Randomly select daily quests
            int questsToSelect = Mathf.Min(maxDailyQuests, dailyCandidates.Count);

            for (int i = 0; i < questsToSelect; i++)
            {
                if (dailyCandidates.Count == 0) break;

                int randomIndex = UnityEngine.Random.Range(0, dailyCandidates.Count);
                dailyQuests.Add(dailyCandidates[randomIndex]);
                dailyCandidates.RemoveAt(randomIndex);
            }

            lastDailyQuestReset = Time.realtimeSinceStartup;

            OnDailyQuestsRefreshed?.Invoke(dailyQuests);

            Debug.Log($"[QuestSystem] Refreshed {dailyQuests.Count} daily quests");
        }

        #endregion

        #region Rewards

        private void AwardQuestRewards(QuestData quest)
        {
            // Award XP
            if (quest.xpReward > 0 && Progression.AchievementManager.Instance != null)
            {
                // Award XP via achievement system
            }

            // Award currency
            if (quest.softCurrencyReward > 0 && Economy.EconomyManager.Instance != null)
            {
                Economy.EconomyManager.Instance.EarnSoftCurrency(quest.softCurrencyReward, $"Quest: {quest.questName}");
            }

            if (quest.hardCurrencyReward > 0 && Economy.EconomyManager.Instance != null)
            {
                Economy.EconomyManager.Instance.AddHardCurrency(quest.hardCurrencyReward, $"Quest: {quest.questName}");
            }

            // Award items
            if (quest.itemRewards != null && Gameplay.InventoryManager.Instance != null)
            {
                ulong playerId = 0; // Get actual player ID
                foreach (var itemReward in quest.itemRewards)
                {
                    var itemData = new Gameplay.LootItemData
                    {
                        itemId = itemReward.itemId,
                        itemName = itemReward.itemName
                    };

                    Gameplay.InventoryManager.Instance.AddItem(playerId, itemData, itemReward.quantity);
                }
            }
        }

        #endregion

        #region Persistence

        private void LoadQuestProgress()
        {
            if (Core.SaveSystem.Instance == null) return;

            string savedData = Core.SaveSystem.Instance.LoadData("quest_progress");
            if (!string.IsNullOrEmpty(savedData))
            {
                var questSave = JsonUtility.FromJson<QuestSaveData>(savedData);

                // Load active quests
                foreach (var activeQuestData in questSave.activeQuests)
                {
                    var quest = GetQuestById(activeQuestData.questId);
                    if (quest != null)
                    {
                        var activeQuest = new ActiveQuest
                        {
                            questData = quest,
                            startTime = DateTime.Parse(activeQuestData.startTime),
                            currentProgress = activeQuestData.progress.ToDictionary(p => p.objectiveId, p => p.progress)
                        };

                        activeQuests[quest.questId] = activeQuest;
                    }
                }

                // Load completed quests
                foreach (var completedId in questSave.completedQuestIds)
                {
                    var quest = GetQuestById(completedId);
                    if (quest != null)
                    {
                        completedQuests[completedId] = quest;
                    }
                }
            }
        }

        private void SaveQuestProgress()
        {
            if (Core.SaveSystem.Instance == null) return;

            var questSave = new QuestSaveData
            {
                activeQuests = new List<ActiveQuestSaveData>(),
                completedQuestIds = new List<string>(completedQuests.Keys)
            };

            foreach (var kvp in activeQuests)
            {
                var activeQuest = kvp.Value;
                var saveData = new ActiveQuestSaveData
                {
                    questId = kvp.Key,
                    startTime = activeQuest.startTime.ToString(),
                    progress = activeQuest.currentProgress.Select(p => new ObjectiveProgress
                    {
                        objectiveId = p.Key,
                        progress = p.Value
                    }).ToList()
                };

                questSave.activeQuests.Add(saveData);
            }

            string json = JsonUtility.ToJson(questSave);
            Core.SaveSystem.Instance.SaveData("quest_progress", json);
        }

        #endregion

        #region Public Getters

        public List<QuestData> GetAvailableQuests()
        {
            return allQuests.Where(q => CanAcceptQuest(q)).ToList();
        }

        public List<ActiveQuest> GetActiveQuests()
        {
            return new List<ActiveQuest>(activeQuests.Values);
        }

        public List<QuestData> GetCompletedQuests()
        {
            return new List<QuestData>(completedQuests.Values);
        }

        public List<QuestData> GetDailyQuests() => dailyQuests;

        public QuestData GetQuestById(string questId)
        {
            return allQuests.FirstOrDefault(q => q.questId == questId);
        }

        public float GetQuestProgress(string questId, string objectiveId)
        {
            if (!activeQuests.ContainsKey(questId)) return 0f;

            var activeQuest = activeQuests[questId];
            if (!activeQuest.currentProgress.ContainsKey(objectiveId)) return 0f;

            return activeQuest.currentProgress[objectiveId];
        }

        #endregion
    }

    #region Data Classes

    [System.Serializable]
    public class QuestData
    {
        public string questId;
        public string questName;
        [TextArea(3, 5)]
        public string description;
        [TextArea(2, 3)]
        public string completionText;

        public QuestType questType = QuestType.Main;
        public int requiredLevel = 1;
        public string[] prerequisiteQuestIds;

        public QuestObjective[] objectives;

        public bool isRepeatable;
        public bool hasTimeLimit;
        public float timeLimitSeconds = 3600f;

        // Rewards
        public int xpReward;
        public int softCurrencyReward;
        public int hardCurrencyReward;
        public ItemReward[] itemRewards;

        public Sprite questIcon;
    }

    [System.Serializable]
    public class QuestObjective
    {
        public string objectiveId;
        public string description;
        public QuestObjectiveType objectiveType;
        public string targetId; // Specific target (e.g., zombie type, item ID)
        public float requiredAmount = 1f;
    }

    [System.Serializable]
    public class ItemReward
    {
        public string itemId;
        public string itemName;
        public int quantity = 1;
    }

    public class ActiveQuest
    {
        public QuestData questData;
        public DateTime startTime;
        public Dictionary<string, float> currentProgress;
    }

    [System.Serializable]
    public class QuestSaveData
    {
        public List<ActiveQuestSaveData> activeQuests;
        public List<string> completedQuestIds;
    }

    [System.Serializable]
    public class ActiveQuestSaveData
    {
        public string questId;
        public string startTime;
        public List<ObjectiveProgress> progress;
    }

    [System.Serializable]
    public class ObjectiveProgress
    {
        public string objectiveId;
        public float progress;
    }

    public enum QuestType
    {
        Main,           // Story quests
        Side,           // Optional quests
        Daily,          // Daily quests
        Weekly,         // Weekly quests
        Event           // Limited-time event quests
    }

    public enum QuestObjectiveType
    {
        KillZombies,        // Kill X zombies
        KillBoss,           // Kill specific boss
        CollectItems,       // Collect X items
        ExtractSuccess,     // Successfully extract
        CompleteMissions,   // Complete X missions
        EarnCurrency,       // Earn X currency
        DealDamage,         // Deal X damage
        Survive,            // Survive X minutes
        CompleteObjectives, // Complete X map objectives
        ReachLocation,      // Reach specific location
        UseWeapon,          // Use specific weapon type
        CraftItems          // Craft X items
    }

    #endregion
}
