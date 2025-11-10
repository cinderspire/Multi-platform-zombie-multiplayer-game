using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Quests
{
    public class QuestSystem : NetworkBehaviour
    {
        public static QuestSystem Instance { get; private set; }

        [Header("Quest Configuration")]
        [SerializeField] private int maxActiveQuests = 10;
        [SerializeField] private int maxDailyQuests = 5;
        [SerializeField] private float questRefreshInterval = 86400f; // 24 hours

        private Dictionary<ulong, PlayerQuestData> playerQuests = new Dictionary<ulong, PlayerQuestData>();
        private Dictionary<string, Quest> questDatabase = new Dictionary<string, Quest>();
        private Dictionary<string, QuestChain> questChains = new Dictionary<string, QuestChain>();

        public event Action<ulong, string> OnQuestAccepted;
        public event Action<ulong, string> OnQuestCompleted;
        public event Action<ulong, string, float> OnQuestProgress;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer) { InitializeQuestDatabase(); InitializeQuestChains(); }
        }

        private void InitializeQuestDatabase()
        {
            // Story Quests
            questDatabase["story_001"] = new Quest
            {
                questId = "story_001",
                questName = "First Steps",
                description = "Survive your first encounter and find shelter",
                questType = QuestType.Story,
                difficulty = QuestDifficulty.Easy,
                objectives = new List<QuestObjective>
                {
                    new QuestObjective { objectiveId = "obj_1", description = "Kill 5 zombies", type = ObjectiveType.Kill, targetId = "zombie_walker", requiredAmount = 5 },
                    new QuestObjective { objectiveId = "obj_2", description = "Find a safe house", type = ObjectiveType.Discover, targetId = "safehouse_01", requiredAmount = 1 }
                },
                rewards = new QuestRewards { xp = 500, softCurrency = 1000, items = new List<string> { "weapon_pistol", "ammo_9mm" } },
                requiredLevel = 1,
                isRepeatable = false
            };

            questDatabase["story_002"] = new Quest
            {
                questId = "story_002",
                questName = "Scavenger's Dilemma",
                description = "Gather supplies to survive",
                questType = QuestType.Story,
                difficulty = QuestDifficulty.Normal,
                objectives = new List<QuestObjective>
                {
                    new QuestObjective { objectiveId = "obj_1", description = "Collect 20 wood", type = ObjectiveType.Collect, targetId = "wood", requiredAmount = 20 },
                    new QuestObjective { objectiveId = "obj_2", description = "Collect 10 metal", type = ObjectiveType.Collect, targetId = "metal", requiredAmount = 10 },
                    new QuestObjective { objectiveId = "obj_3", description = "Find 5 canned food", type = ObjectiveType.Collect, targetId = "canned_food", requiredAmount = 5 }
                },
                rewards = new QuestRewards { xp = 1000, softCurrency = 2000, items = new List<string> { "workbench" } },
                prerequisiteQuests = new List<string> { "story_001" },
                requiredLevel = 3,
                isRepeatable = false
            };

            // Side Quests
            questDatabase["side_hunt_001"] = new Quest
            {
                questId = "side_hunt_001",
                questName = "The Hunt",
                description = "Prove your combat prowess by eliminating threats",
                questType = QuestType.Side,
                difficulty = QuestDifficulty.Hard,
                objectives = new List<QuestObjective>
                {
                    new QuestObjective { objectiveId = "obj_1", description = "Kill 50 zombies", type = ObjectiveType.Kill, targetId = "zombie_any", requiredAmount = 50 },
                    new QuestObjective { objectiveId = "obj_2", description = "Get 20 headshot kills", type = ObjectiveType.Headshot, targetId = "zombie_any", requiredAmount = 20 }
                },
                rewards = new QuestRewards { xp = 3000, softCurrency = 5000, hardCurrency = 50, items = new List<string> { "weapon_rifle" } },
                requiredLevel = 10,
                isRepeatable = false
            };

            questDatabase["side_craft_001"] = new Quest
            {
                questId = "side_craft_001",
                questName = "Master Craftsman",
                description = "Demonstrate your crafting expertise",
                questType = QuestType.Side,
                difficulty = QuestDifficulty.Normal,
                objectives = new List<QuestObjective>
                {
                    new QuestObjective { objectiveId = "obj_1", description = "Craft 10 weapons", type = ObjectiveType.Craft, targetId = "weapon_any", requiredAmount = 10 },
                    new QuestObjective { objectiveId = "obj_2", description = "Upgrade 5 items", type = ObjectiveType.Upgrade, targetId = "item_any", requiredAmount = 5 }
                },
                rewards = new QuestRewards { xp = 2000, softCurrency = 3000, items = new List<string> { "workbench_advanced" } },
                requiredLevel = 8,
                isRepeatable = false
            };

            // Daily Quests
            questDatabase["daily_kills"] = new Quest
            {
                questId = "daily_kills",
                questName = "Daily Extermination",
                description = "Eliminate zombies for daily rewards",
                questType = QuestType.Daily,
                difficulty = QuestDifficulty.Easy,
                objectives = new List<QuestObjective>
                {
                    new QuestObjective { objectiveId = "obj_1", description = "Kill 30 zombies", type = ObjectiveType.Kill, targetId = "zombie_any", requiredAmount = 30 }
                },
                rewards = new QuestRewards { xp = 800, softCurrency = 1500 },
                requiredLevel = 1,
                isRepeatable = true,
                resetInterval = QuestResetInterval.Daily
            };

            questDatabase["daily_resources"] = new Quest
            {
                questId = "daily_resources",
                questName = "Daily Gathering",
                description = "Collect resources",
                questType = QuestType.Daily,
                difficulty = QuestDifficulty.Easy,
                objectives = new List<QuestObjective>
                {
                    new QuestObjective { objectiveId = "obj_1", description = "Collect 50 resources", type = ObjectiveType.Collect, targetId = "resource_any", requiredAmount = 50 }
                },
                rewards = new QuestRewards { xp = 600, softCurrency = 1000 },
                requiredLevel = 1,
                isRepeatable = true,
                resetInterval = QuestResetInterval.Daily
            };

            // Weekly Quests
            questDatabase["weekly_survival"] = new Quest
            {
                questId = "weekly_survival",
                questName = "Weekly Survivor",
                description = "Survive and thrive for a week",
                questType = QuestType.Weekly,
                difficulty = QuestDifficulty.Hard,
                objectives = new List<QuestObjective>
                {
                    new QuestObjective { objectiveId = "obj_1", description = "Survive 180 minutes", type = ObjectiveType.Survive, targetId = "", requiredAmount = 10800 },
                    new QuestObjective { objectiveId = "obj_2", description = "Kill 200 zombies", type = ObjectiveType.Kill, targetId = "zombie_any", requiredAmount = 200 },
                    new QuestObjective { objectiveId = "obj_3", description = "Loot 50 containers", type = ObjectiveType.Loot, targetId = "container_any", requiredAmount = 50 }
                },
                rewards = new QuestRewards { xp = 5000, softCurrency = 10000, hardCurrency = 100 },
                requiredLevel = 5,
                isRepeatable = true,
                resetInterval = QuestResetInterval.Weekly
            };

            // Boss Quests
            questDatabase["boss_001"] = new Quest
            {
                questId = "boss_001",
                questName = "Titan's Fall",
                description = "Defeat the Zombie Titan",
                questType = QuestType.Boss,
                difficulty = QuestDifficulty.Expert,
                objectives = new List<QuestObjective>
                {
                    new QuestObjective { objectiveId = "obj_1", description = "Defeat Zombie Titan", type = ObjectiveType.KillBoss, targetId = "zombie_titan", requiredAmount = 1 }
                },
                rewards = new QuestRewards { xp = 10000, softCurrency = 20000, hardCurrency = 200, items = new List<string> { "weapon_legendary", "armor_epic" } },
                requiredLevel = 25,
                isRepeatable = true
            };

            // Event Quests
            questDatabase["event_halloween"] = new Quest
            {
                questId = "event_halloween",
                questName = "Halloween Horror",
                description = "Special Halloween event quest",
                questType = QuestType.Event,
                difficulty = QuestDifficulty.Hard,
                objectives = new List<QuestObjective>
                {
                    new QuestObjective { objectiveId = "obj_1", description = "Collect 20 pumpkins", type = ObjectiveType.Collect, targetId = "pumpkin", requiredAmount = 20 },
                    new QuestObjective { objectiveId = "obj_2", description = "Defeat special zombies", type = ObjectiveType.Kill, targetId = "zombie_halloween", requiredAmount = 30 }
                },
                rewards = new QuestRewards { xp = 4000, softCurrency = 8000, items = new List<string> { "cosmetic_halloween" } },
                requiredLevel = 10,
                isRepeatable = false,
                expirationDate = DateTime.UtcNow.AddDays(14)
            };

            // PvP Quests
            questDatabase["pvp_001"] = new Quest
            {
                questId = "pvp_001",
                questName = "Hunter's Mark",
                description = "Prove yourself in PvP combat",
                questType = QuestType.PvP,
                difficulty = QuestDifficulty.Hard,
                objectives = new List<QuestObjective>
                {
                    new QuestObjective { objectiveId = "obj_1", description = "Kill 10 players", type = ObjectiveType.KillPlayer, targetId = "", requiredAmount = 10 },
                    new QuestObjective { objectiveId = "obj_2", description = "Win 5 matches", type = ObjectiveType.WinMatch, targetId = "", requiredAmount = 5 }
                },
                rewards = new QuestRewards { xp = 6000, softCurrency = 12000, hardCurrency = 150 },
                requiredLevel = 15,
                isRepeatable = true
            };
        }

        private void InitializeQuestChains()
        {
            questChains["survivor_chain"] = new QuestChain
            {
                chainId = "survivor_chain",
                chainName = "Survivor's Path",
                description = "The journey from novice to expert survivor",
                quests = new List<string> { "story_001", "story_002", "side_craft_001" },
                completionRewards = new QuestRewards { xp = 5000, softCurrency = 10000, hardCurrency = 100, items = new List<string> { "title_survivor" } }
            };

            questChains["combat_chain"] = new QuestChain
            {
                chainId = "combat_chain",
                chainName = "Warrior's Trial",
                description = "Prove your combat prowess",
                quests = new List<string> { "side_hunt_001", "boss_001" },
                completionRewards = new QuestRewards { xp = 8000, softCurrency = 15000, hardCurrency = 200, items = new List<string> { "title_warrior" } }
            };
        }

        [ServerRpc(RequireOwnership = false)]
        public void AcceptQuestServerRpc(ulong playerId, string questId, ServerRpcParams rpcParams = default)
        {
            if (!playerQuests.ContainsKey(playerId))
            {
                playerQuests[playerId] = new PlayerQuestData
                {
                    playerId = playerId,
                    activeQuests = new Dictionary<string, ActiveQuest>(),
                    completedQuests = new List<string>(),
                    dailyQuests = new List<string>(),
                    weeklyQuests = new List<string>()
                };
            }

            var playerData = playerQuests[playerId];
            if (playerData.activeQuests.Count >= maxActiveQuests) return;

            if (questDatabase.TryGetValue(questId, out var quest))
            {
                if (!CanAcceptQuest(playerId, quest)) return;

                var activeQuest = new ActiveQuest
                {
                    questId = questId,
                    quest = quest,
                    acceptedDate = DateTime.UtcNow,
                    objectiveProgress = new Dictionary<string, float>()
                };

                foreach (var objective in quest.objectives)
                {
                    activeQuest.objectiveProgress[objective.objectiveId] = 0;
                }

                playerData.activeQuests[questId] = activeQuest;
                OnQuestAccepted?.Invoke(playerId, questId);

                Debug.Log($"Player {playerId} accepted quest: {quest.questName}");
            }
        }

        private bool CanAcceptQuest(ulong playerId, Quest quest)
        {
            var playerData = playerQuests[playerId];

            // Check prerequisites
            if (quest.prerequisiteQuests != null)
            {
                foreach (var prereq in quest.prerequisiteQuests)
                {
                    if (!playerData.completedQuests.Contains(prereq)) return false;
                }
            }

            // Check if already completed and not repeatable
            if (playerData.completedQuests.Contains(quest.questId) && !quest.isRepeatable) return false;

            // Check level requirement (would integrate with progression system)
            // if (playerLevel < quest.requiredLevel) return false;

            return true;
        }

        [ServerRpc(RequireOwnership = false)]
        public void UpdateQuestProgressServerRpc(ulong playerId, string questId, string objectiveId, float progress, ServerRpcParams rpcParams = default)
        {
            if (!playerQuests.TryGetValue(playerId, out var playerData)) return;
            if (!playerData.activeQuests.TryGetValue(questId, out var activeQuest)) return;

            if (activeQuest.objectiveProgress.ContainsKey(objectiveId))
            {
                activeQuest.objectiveProgress[objectiveId] = progress;
                OnQuestProgress?.Invoke(playerId, questId, progress);

                // Check if quest is complete
                if (IsQuestComplete(activeQuest))
                {
                    CompleteQuest(playerId, questId);
                }
            }
        }

        private bool IsQuestComplete(ActiveQuest activeQuest)
        {
            foreach (var objective in activeQuest.quest.objectives)
            {
                if (activeQuest.objectiveProgress[objective.objectiveId] < objective.requiredAmount)
                    return false;
            }
            return true;
        }

        private void CompleteQuest(ulong playerId, string questId)
        {
            var playerData = playerQuests[playerId];
            var activeQuest = playerData.activeQuests[questId];

            // Award rewards
            AwardQuestRewards(playerId, activeQuest.quest.rewards);

            // Mark as completed
            if (!playerData.completedQuests.Contains(questId))
            {
                playerData.completedQuests.Add(questId);
            }

            // Remove from active
            playerData.activeQuests.Remove(questId);

            OnQuestCompleted?.Invoke(playerId, questId);

            // Check quest chains
            CheckQuestChainCompletion(playerId);

            Debug.Log($"Player {playerId} completed quest: {activeQuest.quest.questName}");
        }

        private void AwardQuestRewards(ulong playerId, QuestRewards rewards)
        {
            // Award XP (would integrate with progression system)
            // ProgressionSystem.Instance?.AddXP(playerId, rewards.xp);

            // Award currency
            Economy.EconomyManager.Instance?.AddSoftCurrency(playerId, rewards.softCurrency);
            if (rewards.hardCurrency > 0)
            {
                Economy.EconomyManager.Instance?.AddHardCurrency(playerId, rewards.hardCurrency);
            }

            // Award items (would integrate with inventory system)
            foreach (var itemId in rewards.items)
            {
                // Inventory.InventoryManager.Instance?.AddItem(playerId, itemId, 1);
            }
        }

        private void CheckQuestChainCompletion(ulong playerId)
        {
            var playerData = playerQuests[playerId];

            foreach (var chain in questChains.Values)
            {
                bool chainComplete = chain.quests.All(q => playerData.completedQuests.Contains(q));
                if (chainComplete && !playerData.completedQuests.Contains($"chain_{chain.chainId}"))
                {
                    playerData.completedQuests.Add($"chain_{chain.chainId}");
                    AwardQuestRewards(playerId, chain.completionRewards);
                    Debug.Log($"Player {playerId} completed quest chain: {chain.chainName}");
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void AbandonQuestServerRpc(ulong playerId, string questId, ServerRpcParams rpcParams = default)
        {
            if (!playerQuests.TryGetValue(playerId, out var playerData)) return;
            playerData.activeQuests.Remove(questId);
        }

        public PlayerQuestData GetPlayerQuests(ulong playerId) => playerQuests.GetValueOrDefault(playerId);
        public Quest GetQuest(string questId) => questDatabase.GetValueOrDefault(questId);
        public List<Quest> GetAvailableQuests(ulong playerId) => questDatabase.Values.Where(q => CanAcceptQuest(playerId, q)).ToList();
    }

    [Serializable]
    public class PlayerQuestData
    {
        public ulong playerId;
        public Dictionary<string, ActiveQuest> activeQuests;
        public List<string> completedQuests;
        public List<string> dailyQuests;
        public List<string> weeklyQuests;
    }

    [Serializable]
    public class ActiveQuest
    {
        public string questId;
        public Quest quest;
        public DateTime acceptedDate;
        public Dictionary<string, float> objectiveProgress;
    }

    [Serializable]
    public class Quest
    {
        public string questId;
        public string questName;
        public string description;
        public QuestType questType;
        public QuestDifficulty difficulty;
        public List<QuestObjective> objectives;
        public QuestRewards rewards;
        public List<string> prerequisiteQuests;
        public int requiredLevel;
        public bool isRepeatable;
        public QuestResetInterval resetInterval;
        public DateTime? expirationDate;
    }

    [Serializable]
    public class QuestObjective
    {
        public string objectiveId;
        public string description;
        public ObjectiveType type;
        public string targetId;
        public int requiredAmount;
    }

    [Serializable]
    public class QuestRewards
    {
        public int xp;
        public int softCurrency;
        public int hardCurrency;
        public List<string> items = new List<string>();
    }

    [Serializable]
    public class QuestChain
    {
        public string chainId;
        public string chainName;
        public string description;
        public List<string> quests;
        public QuestRewards completionRewards;
    }

    public enum QuestType { Story, Side, Daily, Weekly, Boss, Event, PvP, Exploration }
    public enum QuestDifficulty { Easy, Normal, Hard, Expert, Legendary }
    public enum ObjectiveType { Kill, Collect, Craft, Discover, Escort, Defend, Survive, KillBoss, KillPlayer, WinMatch, Loot, Upgrade, Headshot }
    public enum QuestResetInterval { None, Daily, Weekly, Monthly }
}
