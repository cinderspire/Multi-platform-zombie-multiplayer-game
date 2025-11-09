using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;
using System;

namespace DeadFrontier.Progression
{
    /// <summary>
    /// Comprehensive challenge system with daily, weekly, and special challenges.
    /// Tracks progress, awards rewards, supports challenge chains and rotations.
    /// Encourages varied gameplay through diverse challenge types.
    /// </summary>
    public class ChallengeSystem : NetworkBehaviour
    {
        public static ChallengeSystem Instance { get; private set; }

        [Header("Challenge Settings")]
        [SerializeField] private int maxDailyChallenges = 3;
        [SerializeField] private int maxWeeklyChallenges = 5;
        [SerializeField] private int maxSpecialChallenges = 2;

        [Header("Rotation Settings")]
        [SerializeField] private float dailyResetHour = 0f; // UTC midnight
        [SerializeField] private DayOfWeek weeklyResetDay = DayOfWeek.Monday;

        [Header("Reward Multipliers")]
        [SerializeField] private float weeklyRewardMultiplier = 3f;
        [SerializeField] private float specialRewardMultiplier = 5f;

        // Challenge templates
        private Dictionary<string, ChallengeTemplate> challengeTemplates = new Dictionary<string, ChallengeTemplate>();

        // Active challenges
        private Dictionary<string, ActiveChallenge> activeDailyChallenges = new Dictionary<string, ActiveChallenge>();
        private Dictionary<string, ActiveChallenge> activeWeeklyChallenges = new Dictionary<string, ActiveChallenge>();
        private Dictionary<string, ActiveChallenge> activeSpecialChallenges = new Dictionary<string, ActiveChallenge>();

        // Player challenge data
        private Dictionary<ulong, PlayerChallengeData> playerData = new Dictionary<ulong, PlayerChallengeData>();

        // Reset tracking
        private DateTime lastDailyReset;
        private DateTime lastWeeklyReset;

        // Events
        public event Action<ulong, string> OnChallengeCompleted;
        public event Action<ulong, string, float> OnChallengeProgress;
        public event Action OnDailyChallengesReset;
        public event Action OnWeeklyChallengesReset;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeChallengeTemplates();
                InitializeResetTimes();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (!IsServer) return;

            CheckResets();
        }

        #region Initialization

        private void InitializeChallengeTemplates()
        {
            // Combat Challenges
            RegisterTemplate(new ChallengeTemplate
            {
                templateId = "kill_zombies_10",
                title = "Zombie Hunter",
                description = "Kill 10 zombies",
                challengeType = ChallengeType.Combat,
                tier = ChallengeTier.Easy,
                requirement = new ChallengeRequirement { type = RequirementType.ZombieKills, targetValue = 10 },
                rewards = new ChallengeReward { softCurrency = 200, xp = 100 }
            });

            RegisterTemplate(new ChallengeTemplate
            {
                templateId = "kill_zombies_50",
                title = "Zombie Slayer",
                description = "Kill 50 zombies",
                challengeType = ChallengeType.Combat,
                tier = ChallengeTier.Medium,
                requirement = new ChallengeRequirement { type = RequirementType.ZombieKills, targetValue = 50 },
                rewards = new ChallengeReward { softCurrency = 500, xp = 250 }
            });

            RegisterTemplate(new ChallengeTemplate
            {
                templateId = "headshots_20",
                title = "Headshot Expert",
                description = "Get 20 headshot kills",
                challengeType = ChallengeType.Combat,
                tier = ChallengeTier.Hard,
                requirement = new ChallengeRequirement { type = RequirementType.HeadshotKills, targetValue = 20 },
                rewards = new ChallengeReward { softCurrency = 800, hardCurrency = 10, xp = 400 }
            });

            RegisterTemplate(new ChallengeTemplate
            {
                templateId = "killstreak_5",
                title = "Killstreak",
                description = "Get a 5 kill streak",
                challengeType = ChallengeType.Combat,
                tier = ChallengeTier.Medium,
                requirement = new ChallengeRequirement { type = RequirementType.KillStreak, targetValue = 5 },
                rewards = new ChallengeReward { softCurrency = 600, xp = 300 }
            });

            // Survival Challenges
            RegisterTemplate(new ChallengeTemplate
            {
                templateId = "survive_10min",
                title = "Survivor",
                description = "Survive for 10 minutes in a raid",
                challengeType = ChallengeType.Survival,
                tier = ChallengeTier.Easy,
                requirement = new ChallengeRequirement { type = RequirementType.SurvivalTime, targetValue = 600 }, // seconds
                rewards = new ChallengeReward { softCurrency = 300, xp = 150 }
            });

            RegisterTemplate(new ChallengeTemplate
            {
                templateId = "extract_3",
                title = "Extraction Pro",
                description = "Successfully extract 3 times",
                challengeType = ChallengeType.Survival,
                tier = ChallengeTier.Medium,
                requirement = new ChallengeRequirement { type = RequirementType.Extractions, targetValue = 3 },
                rewards = new ChallengeReward { softCurrency = 700, xp = 350 }
            });

            RegisterTemplate(new ChallengeTemplate
            {
                templateId = "no_death",
                title = "Flawless Victory",
                description = "Win a match without dying",
                challengeType = ChallengeType.Survival,
                tier = ChallengeTier.Hard,
                requirement = new ChallengeRequirement { type = RequirementType.NoDeath, targetValue = 1 },
                rewards = new ChallengeReward { softCurrency = 1000, hardCurrency = 20, xp = 500 }
            });

            // Wealth Challenges
            RegisterTemplate(new ChallengeTemplate
            {
                templateId = "earn_5000",
                title = "Money Maker",
                description = "Earn 5,000 credits",
                challengeType = ChallengeType.Wealth,
                tier = ChallengeTier.Easy,
                requirement = new ChallengeRequirement { type = RequirementType.CurrencyEarned, targetValue = 5000 },
                rewards = new ChallengeReward { softCurrency = 500, xp = 200 }
            });

            RegisterTemplate(new ChallengeTemplate
            {
                templateId = "loot_rare_5",
                title = "Treasure Hunter",
                description = "Loot 5 rare items",
                challengeType = ChallengeType.Wealth,
                tier = ChallengeTier.Medium,
                requirement = new ChallengeRequirement { type = RequirementType.RareLoot, targetValue = 5 },
                rewards = new ChallengeReward { softCurrency = 800, xp = 400 }
            });

            // Social Challenges
            RegisterTemplate(new ChallengeTemplate
            {
                templateId = "party_wins_3",
                title = "Team Player",
                description = "Win 3 matches with a party",
                challengeType = ChallengeType.Social,
                tier = ChallengeTier.Medium,
                requirement = new ChallengeRequirement { type = RequirementType.PartyWins, targetValue = 3 },
                rewards = new ChallengeReward { softCurrency = 600, xp = 300 }
            });

            RegisterTemplate(new ChallengeTemplate
            {
                templateId = "revive_allies_5",
                title = "Medic",
                description = "Revive 5 allies",
                challengeType = ChallengeType.Social,
                tier = ChallengeTier.Easy,
                requirement = new ChallengeRequirement { type = RequirementType.Revives, targetValue = 5 },
                rewards = new ChallengeReward { softCurrency = 400, xp = 200 }
            });

            // Skill Challenges
            RegisterTemplate(new ChallengeTemplate
            {
                templateId = "accuracy_80",
                title = "Sharpshooter",
                description = "Achieve 80% accuracy in a match",
                challengeType = ChallengeType.Skill,
                tier = ChallengeTier.Hard,
                requirement = new ChallengeRequirement { type = RequirementType.Accuracy, targetValue = 80 },
                rewards = new ChallengeReward { softCurrency = 1000, hardCurrency = 15, xp = 500 }
            });

            RegisterTemplate(new ChallengeTemplate
            {
                templateId = "melee_kills_10",
                title = "Close Combat",
                description = "Get 10 melee kills",
                challengeType = ChallengeType.Skill,
                tier = ChallengeTier.Medium,
                requirement = new ChallengeRequirement { type = RequirementType.MeleeKills, targetValue = 10 },
                rewards = new ChallengeReward { softCurrency = 600, xp = 300 }
            });

            // Exploration Challenges
            RegisterTemplate(new ChallengeTemplate
            {
                templateId = "discover_locations_5",
                title = "Explorer",
                description = "Discover 5 new locations",
                challengeType = ChallengeType.Exploration,
                tier = ChallengeTier.Easy,
                requirement = new ChallengeRequirement { type = RequirementType.LocationsDiscovered, targetValue = 5 },
                rewards = new ChallengeReward { softCurrency = 400, xp = 200 }
            });

            // Special Challenges (chains)
            RegisterTemplate(new ChallengeTemplate
            {
                templateId = "special_master_hunter",
                title = "Master Hunter",
                description = "Complete a series of hunting challenges",
                challengeType = ChallengeType.Special,
                tier = ChallengeTier.Extreme,
                isChain = true,
                chainRequirements = new List<ChallengeRequirement>
                {
                    new ChallengeRequirement { type = RequirementType.ZombieKills, targetValue = 100 },
                    new ChallengeRequirement { type = RequirementType.HeadshotKills, targetValue = 50 },
                    new ChallengeRequirement { type = RequirementType.KillStreak, targetValue = 10 }
                },
                rewards = new ChallengeReward
                {
                    softCurrency = 5000,
                    hardCurrency = 100,
                    xp = 2500,
                    items = new List<string> { "title_master_hunter", "weapon_skin_legendary" }
                }
            });

            Debug.Log($"[ChallengeSystem] Initialized {challengeTemplates.Count} challenge templates");
        }

        private void InitializeResetTimes()
        {
            lastDailyReset = GetLastDailyResetTime();
            lastWeeklyReset = GetLastWeeklyResetTime();

            // Generate initial challenges
            GenerateDailyChallenges();
            GenerateWeeklyChallenges();
        }

        private void RegisterTemplate(ChallengeTemplate template)
        {
            challengeTemplates[template.templateId] = template;
        }

        #endregion

        #region Challenge Generation

        private void GenerateDailyChallenges()
        {
            activeDailyChallenges.Clear();

            // Select random challenges of different types
            var easyTemplates = challengeTemplates.Values.Where(t => t.tier == ChallengeTier.Easy && !t.isChain).ToList();
            var mediumTemplates = challengeTemplates.Values.Where(t => t.tier == ChallengeTier.Medium && !t.isChain).ToList();

            var selected = new List<ChallengeTemplate>();

            // 2 easy, 1 medium
            if (easyTemplates.Count >= 2)
            {
                selected.Add(easyTemplates[UnityEngine.Random.Range(0, easyTemplates.Count)]);
                easyTemplates.Remove(selected.Last());
                selected.Add(easyTemplates[UnityEngine.Random.Range(0, easyTemplates.Count)]);
            }

            if (mediumTemplates.Count >= 1)
            {
                selected.Add(mediumTemplates[UnityEngine.Random.Range(0, mediumTemplates.Count)]);
            }

            // Create active challenges
            foreach (var template in selected)
            {
                string challengeId = $"daily_{template.templateId}_{DateTime.UtcNow.Ticks}";
                activeDailyChallenges[challengeId] = new ActiveChallenge
                {
                    challengeId = challengeId,
                    templateId = template.templateId,
                    expirationTime = GetNextDailyResetTime()
                };
            }

            Debug.Log($"[ChallengeSystem] Generated {activeDailyChallenges.Count} daily challenges");

            OnDailyChallengesReset?.Invoke();
        }

        private void GenerateWeeklyChallenges()
        {
            activeWeeklyChallenges.Clear();

            // Select harder challenges
            var mediumTemplates = challengeTemplates.Values.Where(t => t.tier == ChallengeTier.Medium && !t.isChain).ToList();
            var hardTemplates = challengeTemplates.Values.Where(t => t.tier == ChallengeTier.Hard && !t.isChain).ToList();

            var selected = new List<ChallengeTemplate>();

            // 3 medium, 2 hard
            for (int i = 0; i < 3 && mediumTemplates.Count > 0; i++)
            {
                var template = mediumTemplates[UnityEngine.Random.Range(0, mediumTemplates.Count)];
                selected.Add(template);
                mediumTemplates.Remove(template);
            }

            for (int i = 0; i < 2 && hardTemplates.Count > 0; i++)
            {
                var template = hardTemplates[UnityEngine.Random.Range(0, hardTemplates.Count)];
                selected.Add(template);
                hardTemplates.Remove(template);
            }

            // Create active challenges with multiplied rewards
            foreach (var template in selected)
            {
                string challengeId = $"weekly_{template.templateId}_{DateTime.UtcNow.Ticks}";

                var modifiedTemplate = new ChallengeTemplate(template);
                modifiedTemplate.rewards.softCurrency = Mathf.RoundToInt(modifiedTemplate.rewards.softCurrency * weeklyRewardMultiplier);
                modifiedTemplate.rewards.hardCurrency = Mathf.RoundToInt(modifiedTemplate.rewards.hardCurrency * weeklyRewardMultiplier);
                modifiedTemplate.rewards.xp = Mathf.RoundToInt(modifiedTemplate.rewards.xp * weeklyRewardMultiplier);

                activeWeeklyChallenges[challengeId] = new ActiveChallenge
                {
                    challengeId = challengeId,
                    templateId = template.templateId,
                    expirationTime = GetNextWeeklyResetTime()
                };
            }

            Debug.Log($"[ChallengeSystem] Generated {activeWeeklyChallenges.Count} weekly challenges");

            OnWeeklyChallengesReset?.Invoke();
        }

        #endregion

        #region Player Data

        public void InitializePlayerData(ulong playerId)
        {
            if (playerData.ContainsKey(playerId)) return;

            playerData[playerId] = new PlayerChallengeData
            {
                playerId = playerId,
                activeChallenges = new Dictionary<string, ChallengeProgress>(),
                completedChallenges = new List<string>(),
                totalChallengesCompleted = 0
            };

            // Add current active challenges to player
            foreach (var kvp in activeDailyChallenges)
            {
                AddChallengeToPlayer(playerId, kvp.Key);
            }

            foreach (var kvp in activeWeeklyChallenges)
            {
                AddChallengeToPlayer(playerId, kvp.Key);
            }

            LoadPlayerData(playerId);
        }

        private void AddChallengeToPlayer(ulong playerId, string challengeId)
        {
            if (!playerData.ContainsKey(playerId)) return;

            var data = playerData[playerId];

            if (!data.activeChallenges.ContainsKey(challengeId))
            {
                data.activeChallenges[challengeId] = new ChallengeProgress
                {
                    challengeId = challengeId,
                    currentProgress = 0f,
                    isCompleted = false,
                    rewardClaimed = false
                };
            }
        }

        #endregion

        #region Progress Tracking

        public void TrackProgress(ulong playerId, RequirementType type, float value)
        {
            if (!playerData.ContainsKey(playerId))
            {
                InitializePlayerData(playerId);
            }

            var data = playerData[playerId];

            // Find all active challenges that match this requirement
            foreach (var kvp in data.activeChallenges)
            {
                string challengeId = kvp.Key;
                var progress = kvp.Value;

                if (progress.isCompleted) continue;

                // Get template
                var activeChallenge = GetActiveChallenge(challengeId);
                if (activeChallenge == null) continue;

                var template = challengeTemplates[activeChallenge.templateId];

                // Check if this challenge tracks this requirement type
                if (template.requirement.type == type)
                {
                    UpdateChallengeProgress(playerId, challengeId, value);
                }
                // Check chain requirements
                else if (template.isChain && template.chainRequirements != null)
                {
                    foreach (var chainReq in template.chainRequirements)
                    {
                        if (chainReq.type == type)
                        {
                            UpdateChallengeProgress(playerId, challengeId, value);
                            break;
                        }
                    }
                }
            }
        }

        private void UpdateChallengeProgress(ulong playerId, string challengeId, float currentValue)
        {
            var data = playerData[playerId];
            var progress = data.activeChallenges[challengeId];

            if (progress.isCompleted) return;

            var activeChallenge = GetActiveChallenge(challengeId);
            var template = challengeTemplates[activeChallenge.templateId];

            // Update progress
            progress.currentProgress = currentValue;

            // Calculate progress percentage
            float progressPercent = 0f;

            if (!template.isChain)
            {
                progressPercent = Mathf.Clamp01(currentValue / template.requirement.targetValue);

                OnChallengeProgress?.Invoke(playerId, challengeId, progressPercent);

                // Check completion
                if (currentValue >= template.requirement.targetValue)
                {
                    CompleteChallenge(playerId, challengeId);
                }
            }
            else
            {
                // Check all chain requirements
                bool allComplete = CheckChainCompletion(playerId, template);
                if (allComplete)
                {
                    CompleteChallenge(playerId, challengeId);
                }
            }
        }

        private bool CheckChainCompletion(ulong playerId, ChallengeTemplate template)
        {
            // This would need to track individual chain step progress
            // For simplicity, just check if all requirements are met
            return true; // Placeholder
        }

        #endregion

        #region Challenge Completion

        private void CompleteChallenge(ulong playerId, string challengeId)
        {
            var data = playerData[playerId];
            var progress = data.activeChallenges[challengeId];

            progress.isCompleted = true;
            progress.completionTime = DateTime.UtcNow;

            data.completedChallenges.Add(challengeId);
            data.totalChallengesCompleted++;

            OnChallengeCompleted?.Invoke(playerId, challengeId);

            var activeChallenge = GetActiveChallenge(challengeId);
            var template = challengeTemplates[activeChallenge.templateId];

            Debug.Log($"[ChallengeSystem] Player {playerId} completed challenge: {template.title}");

            SavePlayerData(playerId);

            // Notify client
            CompleteChallengeClientRpc(playerId, template.title);
        }

        [ClientRpc]
        private void CompleteChallengeClientRpc(ulong playerId, string challengeTitle)
        {
            Debug.Log($"[ChallengeSystem] CHALLENGE COMPLETED: {challengeTitle}");
        }

        public bool ClaimReward(ulong playerId, string challengeId)
        {
            if (!playerData.ContainsKey(playerId)) return false;

            var data = playerData[playerId];

            if (!data.activeChallenges.ContainsKey(challengeId)) return false;

            var progress = data.activeChallenges[challengeId];

            if (!progress.isCompleted || progress.rewardClaimed) return false;

            var activeChallenge = GetActiveChallenge(challengeId);
            var template = challengeTemplates[activeChallenge.templateId];

            // Grant rewards
            GrantRewards(playerId, template.rewards);

            progress.rewardClaimed = true;

            SavePlayerData(playerId);

            Debug.Log($"[ChallengeSystem] Player {playerId} claimed reward for: {template.title}");

            return true;
        }

        private void GrantRewards(ulong playerId, ChallengeReward reward)
        {
            if (reward.softCurrency > 0)
            {
                Economy.EconomyManager.Instance?.AddSoftCurrency(playerId, reward.softCurrency);
            }

            if (reward.hardCurrency > 0)
            {
                Economy.EconomyManager.Instance?.AddHardCurrency(playerId, reward.hardCurrency);
            }

            if (reward.xp > 0)
            {
                Progression.ProgressionManager.Instance?.AddExperience(playerId, reward.xp);
            }

            foreach (var itemId in reward.items)
            {
                Inventory.InventoryManager.Instance?.AddItem(playerId, itemId, 1);
            }
        }

        #endregion

        #region Reset System

        private void CheckResets()
        {
            DateTime now = DateTime.UtcNow;

            // Check daily reset
            if (now >= GetNextDailyResetTime())
            {
                ResetDailyChallenges();
                lastDailyReset = now;
            }

            // Check weekly reset
            if (now >= GetNextWeeklyResetTime())
            {
                ResetWeeklyChallenges();
                lastWeeklyReset = now;
            }
        }

        private void ResetDailyChallenges()
        {
            // Clear old challenges from all players
            foreach (var kvp in playerData)
            {
                var data = kvp.Value;

                var dailyChallengeIds = data.activeChallenges.Keys
                    .Where(id => id.StartsWith("daily_"))
                    .ToList();

                foreach (var id in dailyChallengeIds)
                {
                    data.activeChallenges.Remove(id);
                }
            }

            // Generate new challenges
            GenerateDailyChallenges();

            // Add new challenges to all players
            foreach (var kvp in playerData)
            {
                ulong playerId = kvp.Key;

                foreach (var challengeId in activeDailyChallenges.Keys)
                {
                    AddChallengeToPlayer(playerId, challengeId);
                }

                SavePlayerData(playerId);
            }

            Debug.Log("[ChallengeSystem] Daily challenges reset");
        }

        private void ResetWeeklyChallenges()
        {
            // Clear old challenges from all players
            foreach (var kvp in playerData)
            {
                var data = kvp.Value;

                var weeklyChallengeIds = data.activeChallenges.Keys
                    .Where(id => id.StartsWith("weekly_"))
                    .ToList();

                foreach (var id in weeklyChallengeIds)
                {
                    data.activeChallenges.Remove(id);
                }
            }

            // Generate new challenges
            GenerateWeeklyChallenges();

            // Add new challenges to all players
            foreach (var kvp in playerData)
            {
                ulong playerId = kvp.Key;

                foreach (var challengeId in activeWeeklyChallenges.Keys)
                {
                    AddChallengeToPlayer(playerId, challengeId);
                }

                SavePlayerData(playerId);
            }

            Debug.Log("[ChallengeSystem] Weekly challenges reset");
        }

        private DateTime GetLastDailyResetTime()
        {
            DateTime now = DateTime.UtcNow;
            DateTime reset = new DateTime(now.Year, now.Month, now.Day, (int)dailyResetHour, 0, 0, DateTimeKind.Utc);

            if (now.Hour < dailyResetHour)
            {
                reset = reset.AddDays(-1);
            }

            return reset;
        }

        private DateTime GetNextDailyResetTime()
        {
            return GetLastDailyResetTime().AddDays(1);
        }

        private DateTime GetLastWeeklyResetTime()
        {
            DateTime now = DateTime.UtcNow;
            int daysUntilReset = ((int)weeklyResetDay - (int)now.DayOfWeek + 7) % 7;

            DateTime reset = now.AddDays(-daysUntilReset);
            reset = new DateTime(reset.Year, reset.Month, reset.Day, (int)dailyResetHour, 0, 0, DateTimeKind.Utc);

            return reset;
        }

        private DateTime GetNextWeeklyResetTime()
        {
            return GetLastWeeklyResetTime().AddDays(7);
        }

        #endregion

        #region Queries

        public List<ActiveChallenge> GetActiveDailyChallenges()
        {
            return activeDailyChallenges.Values.ToList();
        }

        public List<ActiveChallenge> GetActiveWeeklyChallenges()
        {
            return activeWeeklyChallenges.Values.ToList();
        }

        public List<ChallengeProgress> GetPlayerChallenges(ulong playerId)
        {
            if (!playerData.ContainsKey(playerId)) return new List<ChallengeProgress>();

            return playerData[playerId].activeChallenges.Values.ToList();
        }

        private ActiveChallenge GetActiveChallenge(string challengeId)
        {
            if (activeDailyChallenges.ContainsKey(challengeId))
                return activeDailyChallenges[challengeId];

            if (activeWeeklyChallenges.ContainsKey(challengeId))
                return activeWeeklyChallenges[challengeId];

            if (activeSpecialChallenges.ContainsKey(challengeId))
                return activeSpecialChallenges[challengeId];

            return null;
        }

        #endregion

        #region Data Persistence

        private void SavePlayerData(ulong playerId)
        {
            if (!playerData.ContainsKey(playerId)) return;

            var data = playerData[playerId];
            string json = JsonUtility.ToJson(data);

            SaveSystem.SaveManager.Instance?.SaveData($"challenges_{playerId}", json);
        }

        public void LoadPlayerData(ulong playerId)
        {
            string json = SaveSystem.SaveManager.Instance?.LoadData($"challenges_{playerId}");

            if (!string.IsNullOrEmpty(json))
            {
                var data = JsonUtility.FromJson<PlayerChallengeData>(json);
                playerData[playerId] = data;

                Debug.Log($"[ChallengeSystem] Loaded challenge data for player {playerId}");
            }
        }

        #endregion
    }

    #region Data Classes

    [Serializable]
    public class ChallengeTemplate
    {
        public string templateId;
        public string title;
        public string description;
        public ChallengeType challengeType;
        public ChallengeTier tier;
        public ChallengeRequirement requirement;
        public List<ChallengeRequirement> chainRequirements;
        public ChallengeReward rewards;
        public bool isChain;

        public ChallengeTemplate() { }

        public ChallengeTemplate(ChallengeTemplate other)
        {
            templateId = other.templateId;
            title = other.title;
            description = other.description;
            challengeType = other.challengeType;
            tier = other.tier;
            requirement = other.requirement;
            chainRequirements = other.chainRequirements;
            rewards = new ChallengeReward
            {
                softCurrency = other.rewards.softCurrency,
                hardCurrency = other.rewards.hardCurrency,
                xp = other.rewards.xp,
                items = new List<string>(other.rewards.items)
            };
            isChain = other.isChain;
        }
    }

    [Serializable]
    public class ChallengeRequirement
    {
        public RequirementType type;
        public float targetValue;
    }

    [Serializable]
    public class ChallengeReward
    {
        public int softCurrency;
        public int hardCurrency;
        public int xp;
        public List<string> items = new List<string>();
    }

    [Serializable]
    public class ActiveChallenge
    {
        public string challengeId;
        public string templateId;
        public DateTime expirationTime;
    }

    [Serializable]
    public class PlayerChallengeData
    {
        public ulong playerId;
        public Dictionary<string, ChallengeProgress> activeChallenges = new Dictionary<string, ChallengeProgress>();
        public List<string> completedChallenges = new List<string>();
        public int totalChallengesCompleted;
    }

    [Serializable]
    public class ChallengeProgress
    {
        public string challengeId;
        public float currentProgress;
        public bool isCompleted;
        public bool rewardClaimed;
        public DateTime completionTime;
    }

    public enum ChallengeType
    {
        Combat,
        Survival,
        Wealth,
        Social,
        Skill,
        Exploration,
        Special
    }

    public enum ChallengeTier
    {
        Easy,
        Medium,
        Hard,
        Extreme
    }

    public enum RequirementType
    {
        ZombieKills,
        PlayerKills,
        HeadshotKills,
        MeleeKills,
        KillStreak,
        SurvivalTime,
        Extractions,
        NoDeath,
        CurrencyEarned,
        RareLoot,
        PartyWins,
        Revives,
        Accuracy,
        LocationsDiscovered
    }

    #endregion
}
