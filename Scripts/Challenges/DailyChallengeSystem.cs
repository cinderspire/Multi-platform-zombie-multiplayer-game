using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Challenges
{
    /// <summary>
    /// Comprehensive daily and weekly challenge system.
    /// Provides rotating challenges, rewards, and streak bonuses for player engagement.
    /// </summary>
    public class DailyChallengeSystem : NetworkBehaviour
    {
        public static DailyChallengeSystem Instance { get; private set; }

        [Header("Challenge Configuration")]
        [SerializeField] private int dailyChallengeCount = 3;
        [SerializeField] private int weeklyChallengeCount = 5;
        [SerializeField] private float challengeRefreshHour = 0f; // UTC midnight
        [SerializeField] private int maxActiveChallenges = 20;

        [Header("Streak System")]
        [SerializeField] private bool enableStreakSystem = true;
        [SerializeField] private int maxStreakBonus = 10;
        [SerializeField] private float streakBonusMultiplier = 0.1f; // 10% per day

        [Header("Reroll System")]
        [SerializeField] private bool enableReroll = true;
        [SerializeField] private int rerollCost = 50;
        [SerializeField] private int maxRerollsPerDay = 3;

        [Header("Rewards")]
        [SerializeField] private int baseDailyReward = 500;
        [SerializeField] private int baseWeeklyReward = 2500;
        [SerializeField] private int bonusHardCurrency = 10;

        // Data structures
        private Dictionary<ulong, PlayerChallengeData> playerChallenges = new Dictionary<ulong, PlayerChallengeData>();
        private Dictionary<string, Challenge> activeChallenges = new Dictionary<string, Challenge>();
        private Dictionary<ChallengeType, List<ChallengeTemplate>> challengeTemplates = new Dictionary<ChallengeType, List<ChallengeTemplate>>();
        private DateTime lastDailyRefresh;
        private DateTime lastWeeklyRefresh;

        // Events
        public event Action<ulong, string> OnChallengeAssigned;
        public event Action<ulong, string> OnChallengeCompleted;
        public event Action<ulong, string, float> OnChallengeProgress;
        public event Action<ulong, int> OnStreakUpdated;
        public event Action OnChallengesRefreshed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                InitializeChallengeTemplates();
                lastDailyRefresh = GetLastDailyReset();
                lastWeeklyRefresh = GetLastWeeklyReset();
                InvokeRepeating(nameof(CheckChallengeRefresh), 60f, 60f); // Check every minute
            }
        }

        private void InitializeChallengeTemplates()
        {
            foreach (ChallengeType type in Enum.GetValues(typeof(ChallengeType)))
            {
                challengeTemplates[type] = new List<ChallengeTemplate>();
            }

            // Combat Challenges
            challengeTemplates[ChallengeType.Combat].AddRange(new[]
            {
                new ChallengeTemplate { templateId = "kill_zombies_10", title = "Zombie Slayer I", description = "Kill 10 zombies", requirementType = RequirementType.KillZombies, requiredAmount = 10, difficulty = ChallengeDifficulty.Easy },
                new ChallengeTemplate { templateId = "kill_zombies_25", title = "Zombie Slayer II", description = "Kill 25 zombies", requirementType = RequirementType.KillZombies, requiredAmount = 25, difficulty = ChallengeDifficulty.Normal },
                new ChallengeTemplate { templateId = "kill_zombies_50", title = "Zombie Slayer III", description = "Kill 50 zombies", requirementType = RequirementType.KillZombies, requiredAmount = 50, difficulty = ChallengeDifficulty.Hard },
                new ChallengeTemplate { templateId = "headshots_5", title = "Headhunter", description = "Get 5 headshot kills", requirementType = RequirementType.HeadshotKills, requiredAmount = 5, difficulty = ChallengeDifficulty.Normal },
                new ChallengeTemplate { templateId = "headshots_15", title = "Precision Killer", description = "Get 15 headshot kills", requirementType = RequirementType.HeadshotKills, requiredAmount = 15, difficulty = ChallengeDifficulty.Hard },
                new ChallengeTemplate { templateId = "melee_kills_10", title = "Close Combat", description = "Get 10 melee kills", requirementType = RequirementType.MeleeKills, requiredAmount = 10, difficulty = ChallengeDifficulty.Normal },
                new ChallengeTemplate { templateId = "explosive_kills_5", title = "Demolition Expert", description = "Kill 5 enemies with explosives", requirementType = RequirementType.ExplosiveKills, requiredAmount = 5, difficulty = ChallengeDifficulty.Normal },
                new ChallengeTemplate { templateId = "killstreak_5", title = "Killing Spree", description = "Get a 5 kill streak", requirementType = RequirementType.KillStreak, requiredAmount = 5, difficulty = ChallengeDifficulty.Hard }
            });

            // Survival Challenges
            challengeTemplates[ChallengeType.Survival].AddRange(new[]
            {
                new ChallengeTemplate { templateId = "survive_15min", title = "Survivor", description = "Survive for 15 minutes", requirementType = RequirementType.SurvivalTime, requiredAmount = 900, difficulty = ChallengeDifficulty.Easy },
                new ChallengeTemplate { templateId = "survive_30min", title = "Endurance", description = "Survive for 30 minutes", requirementType = RequirementType.SurvivalTime, requiredAmount = 1800, difficulty = ChallengeDifficulty.Normal },
                new ChallengeTemplate { templateId = "survive_60min", title = "Marathon Runner", description = "Survive for 60 minutes", requirementType = RequirementType.SurvivalTime, requiredAmount = 3600, difficulty = ChallengeDifficulty.Hard },
                new ChallengeTemplate { templateId = "no_damage_10min", title = "Untouchable", description = "Survive 10 minutes without taking damage", requirementType = RequirementType.NoDamageSurvival, requiredAmount = 600, difficulty = ChallengeDifficulty.Expert },
                new ChallengeTemplate { templateId = "revive_teammates_3", title = "Lifesaver", description = "Revive 3 teammates", requirementType = RequirementType.ReviveTeammates, requiredAmount = 3, difficulty = ChallengeDifficulty.Normal }
            });

            // Economy Challenges
            challengeTemplates[ChallengeType.Economy].AddRange(new[]
            {
                new ChallengeTemplate { templateId = "earn_1000", title = "Money Maker", description = "Earn 1,000 currency", requirementType = RequirementType.CurrencyEarned, requiredAmount = 1000, difficulty = ChallengeDifficulty.Easy },
                new ChallengeTemplate { templateId = "earn_5000", title = "Entrepreneur", description = "Earn 5,000 currency", requirementType = RequirementType.CurrencyEarned, requiredAmount = 5000, difficulty = ChallengeDifficulty.Normal },
                new ChallengeTemplate { templateId = "earn_10000", title = "Business Tycoon", description = "Earn 10,000 currency", requirementType = RequirementType.CurrencyEarned, requiredAmount = 10000, difficulty = ChallengeDifficulty.Hard },
                new ChallengeTemplate { templateId = "loot_chests_5", title = "Treasure Hunter", description = "Loot 5 chests", requirementType = RequirementType.ChestsLooted, requiredAmount = 5, difficulty = ChallengeDifficulty.Easy },
                new ChallengeTemplate { templateId = "complete_trades_3", title = "Trader", description = "Complete 3 trades", requirementType = RequirementType.TradesCompleted, requiredAmount = 3, difficulty = ChallengeDifficulty.Normal }
            });

            // Crafting Challenges
            challengeTemplates[ChallengeType.Crafting].AddRange(new[]
            {
                new ChallengeTemplate { templateId = "craft_items_5", title = "Apprentice Crafter", description = "Craft 5 items", requirementType = RequirementType.ItemsCrafted, requiredAmount = 5, difficulty = ChallengeDifficulty.Easy },
                new ChallengeTemplate { templateId = "craft_items_15", title = "Skilled Crafter", description = "Craft 15 items", requirementType = RequirementType.ItemsCrafted, requiredAmount = 15, difficulty = ChallengeDifficulty.Normal },
                new ChallengeTemplate { templateId = "craft_weapons_3", title = "Weaponsmith", description = "Craft 3 weapons", requirementType = RequirementType.WeaponsCrafted, requiredAmount = 3, difficulty = ChallengeDifficulty.Normal },
                new ChallengeTemplate { templateId = "upgrade_items_5", title = "Enhancer", description = "Upgrade 5 items", requirementType = RequirementType.ItemsUpgraded, requiredAmount = 5, difficulty = ChallengeDifficulty.Normal }
            });

            // Exploration Challenges
            challengeTemplates[ChallengeType.Exploration].AddRange(new[]
            {
                new ChallengeTemplate { templateId = "travel_1km", title = "Walker", description = "Travel 1 km", requirementType = RequirementType.DistanceTraveled, requiredAmount = 1000, difficulty = ChallengeDifficulty.Easy },
                new ChallengeTemplate { templateId = "travel_5km", title = "Explorer", description = "Travel 5 km", requirementType = RequirementType.DistanceTraveled, requiredAmount = 5000, difficulty = ChallengeDifficulty.Normal },
                new ChallengeTemplate { templateId = "discover_locations_3", title = "Scout", description = "Discover 3 new locations", requirementType = RequirementType.LocationsDiscovered, requiredAmount = 3, difficulty = ChallengeDifficulty.Normal },
                new ChallengeTemplate { templateId = "visit_zones_5", title = "Wanderer", description = "Visit 5 different zones", requirementType = RequirementType.ZonesVisited, requiredAmount = 5, difficulty = ChallengeDifficulty.Normal }
            });

            // Social Challenges
            challengeTemplates[ChallengeType.Social].AddRange(new[]
            {
                new ChallengeTemplate { templateId = "party_missions_2", title = "Team Player", description = "Complete 2 missions in a party", requirementType = RequirementType.PartyMissions, requiredAmount = 2, difficulty = ChallengeDifficulty.Easy },
                new ChallengeTemplate { templateId = "party_missions_5", title = "Squad Leader", description = "Complete 5 missions in a party", requirementType = RequirementType.PartyMissions, requiredAmount = 5, difficulty = ChallengeDifficulty.Normal },
                new ChallengeTemplate { templateId = "help_newbies_3", title = "Mentor", description = "Help 3 lower level players", requirementType = RequirementType.HelpNewPlayers, requiredAmount = 3, difficulty = ChallengeDifficulty.Normal }
            });

            // Weekly Challenges (Harder)
            challengeTemplates[ChallengeType.Weekly].AddRange(new[]
            {
                new ChallengeTemplate { templateId = "weekly_kills_200", title = "Weekly Exterminator", description = "Kill 200 zombies this week", requirementType = RequirementType.KillZombies, requiredAmount = 200, difficulty = ChallengeDifficulty.Hard },
                new ChallengeTemplate { templateId = "weekly_earn_25000", title = "Weekly Fortune", description = "Earn 25,000 currency this week", requirementType = RequirementType.CurrencyEarned, requiredAmount = 25000, difficulty = ChallengeDifficulty.Hard },
                new ChallengeTemplate { templateId = "weekly_craft_50", title = "Weekly Producer", description = "Craft 50 items this week", requirementType = RequirementType.ItemsCrafted, requiredAmount = 50, difficulty = ChallengeDifficulty.Hard },
                new ChallengeTemplate { templateId = "weekly_travel_25km", title = "Weekly Explorer", description = "Travel 25 km this week", requirementType = RequirementType.DistanceTraveled, requiredAmount = 25000, difficulty = ChallengeDifficulty.Hard },
                new ChallengeTemplate { templateId = "weekly_headshots_50", title = "Weekly Sharpshooter", description = "Get 50 headshots this week", requirementType = RequirementType.HeadshotKills, requiredAmount = 50, difficulty = ChallengeDifficulty.Expert },
                new ChallengeTemplate { templateId = "weekly_boss_3", title = "Weekly Boss Hunter", description = "Defeat 3 bosses this week", requirementType = RequirementType.BossesDefeated, requiredAmount = 3, difficulty = ChallengeDifficulty.Expert }
            });
        }

        private void CheckChallengeRefresh()
        {
            DateTime now = DateTime.UtcNow;

            // Check daily refresh
            if (now >= lastDailyRefresh.AddDays(1))
            {
                RefreshDailyChallenges();
                lastDailyRefresh = GetLastDailyReset();
            }

            // Check weekly refresh
            if (now >= lastWeeklyRefresh.AddDays(7))
            {
                RefreshWeeklyChallenges();
                lastWeeklyRefresh = GetLastWeeklyReset();
            }
        }

        private DateTime GetLastDailyReset()
        {
            DateTime now = DateTime.UtcNow;
            DateTime reset = new DateTime(now.Year, now.Month, now.Day, (int)challengeRefreshHour, 0, 0, DateTimeKind.Utc);

            if (now < reset)
            {
                reset = reset.AddDays(-1);
            }

            return reset;
        }

        private DateTime GetLastWeeklyReset()
        {
            DateTime now = DateTime.UtcNow;
            int daysUntilMonday = ((int)DayOfWeek.Monday - (int)now.DayOfWeek + 7) % 7;
            DateTime lastMonday = now.AddDays(-daysUntilMonday).Date;

            if (daysUntilMonday == 0 && now.Hour < challengeRefreshHour)
            {
                lastMonday = lastMonday.AddDays(-7);
            }

            return new DateTime(lastMonday.Year, lastMonday.Month, lastMonday.Day, (int)challengeRefreshHour, 0, 0, DateTimeKind.Utc);
        }

        private void RefreshDailyChallenges()
        {
            // Refresh daily challenges for all players
            foreach (var playerId in playerChallenges.Keys)
            {
                GenerateDailyChallenges(playerId);
            }

            OnChallengesRefreshed?.Invoke();
            BroadcastChallengesRefreshedClientRpc();

            Debug.Log("Daily challenges refreshed");
        }

        private void RefreshWeeklyChallenges()
        {
            // Refresh weekly challenges for all players
            foreach (var playerId in playerChallenges.Keys)
            {
                GenerateWeeklyChallenges(playerId);
            }

            OnChallengesRefreshed?.Invoke();
            BroadcastChallengesRefreshedClientRpc();

            Debug.Log("Weekly challenges refreshed");
        }

        #region Server RPCs

        [ServerRpc(RequireOwnership = false)]
        public void InitializePlayerChallengesServerRpc(ulong playerId, ServerRpcParams rpcParams = default)
        {
            if (!playerChallenges.ContainsKey(playerId))
            {
                playerChallenges[playerId] = new PlayerChallengeData
                {
                    playerId = playerId,
                    dailyChallenges = new List<string>(),
                    weeklyChallenges = new List<string>(),
                    challengeProgress = new Dictionary<string, float>(),
                    completedChallenges = new List<string>(),
                    dailyStreak = 0,
                    lastCompletionDate = DateTime.MinValue,
                    rerollsUsedToday = 0,
                    lastRerollResetDate = DateTime.UtcNow.Date
                };

                GenerateDailyChallenges(playerId);
                GenerateWeeklyChallenges(playerId);
            }
        }

        private void GenerateDailyChallenges(ulong playerId)
        {
            if (!playerChallenges.TryGetValue(playerId, out var playerData))
            {
                return;
            }

            // Clear old daily challenges
            foreach (var challengeId in playerData.dailyChallenges)
            {
                activeChallenges.Remove(challengeId);
                playerData.challengeProgress.Remove(challengeId);
            }
            playerData.dailyChallenges.Clear();

            // Generate new daily challenges
            var selectedTemplates = SelectRandomChallenges(ChallengeType.Combat, ChallengeType.Survival, ChallengeType.Economy, ChallengeType.Crafting, ChallengeType.Exploration, ChallengeType.Social);

            for (int i = 0; i < dailyChallengeCount && i < selectedTemplates.Count; i++)
            {
                var challenge = CreateChallengeFromTemplate(selectedTemplates[i], ChallengeFrequency.Daily);
                activeChallenges[challenge.challengeId] = challenge;
                playerData.dailyChallenges.Add(challenge.challengeId);
                playerData.challengeProgress[challenge.challengeId] = 0f;

                OnChallengeAssigned?.Invoke(playerId, challenge.challengeId);
            }
        }

        private void GenerateWeeklyChallenges(ulong playerId)
        {
            if (!playerChallenges.TryGetValue(playerId, out var playerData))
            {
                return;
            }

            // Clear old weekly challenges
            foreach (var challengeId in playerData.weeklyChallenges)
            {
                activeChallenges.Remove(challengeId);
                playerData.challengeProgress.Remove(challengeId);
            }
            playerData.weeklyChallenges.Clear();

            // Generate new weekly challenges
            var weeklyTemplates = challengeTemplates[ChallengeType.Weekly];
            var selectedTemplates = weeklyTemplates.OrderBy(x => UnityEngine.Random.value).Take(weeklyChallengeCount).ToList();

            foreach (var template in selectedTemplates)
            {
                var challenge = CreateChallengeFromTemplate(template, ChallengeFrequency.Weekly);
                activeChallenges[challenge.challengeId] = challenge;
                playerData.weeklyChallenges.Add(challenge.challengeId);
                playerData.challengeProgress[challenge.challengeId] = 0f;

                OnChallengeAssigned?.Invoke(playerId, challenge.challengeId);
            }
        }

        private List<ChallengeTemplate> SelectRandomChallenges(params ChallengeType[] types)
        {
            var allTemplates = new List<ChallengeTemplate>();

            foreach (var type in types)
            {
                if (challengeTemplates.ContainsKey(type))
                {
                    allTemplates.AddRange(challengeTemplates[type]);
                }
            }

            return allTemplates.OrderBy(x => UnityEngine.Random.value).Take(dailyChallengeCount).ToList();
        }

        private Challenge CreateChallengeFromTemplate(ChallengeTemplate template, ChallengeFrequency frequency)
        {
            int reward = frequency == ChallengeFrequency.Daily ? baseDailyReward : baseWeeklyReward;
            reward = Mathf.RoundToInt(reward * GetDifficultyMultiplier(template.difficulty));

            return new Challenge
            {
                challengeId = $"{template.templateId}_{Guid.NewGuid()}",
                templateId = template.templateId,
                title = template.title,
                description = template.description,
                challengeType = template.requirementType,
                requiredAmount = template.requiredAmount,
                currentAmount = 0,
                difficulty = template.difficulty,
                frequency = frequency,
                rewardCurrency = reward,
                rewardHardCurrency = frequency == ChallengeFrequency.Weekly ? bonusHardCurrency : 0,
                status = ChallengeStatus.Active,
                assignedDate = DateTime.UtcNow,
                expirationDate = frequency == ChallengeFrequency.Daily ?
                    DateTime.UtcNow.AddDays(1) : DateTime.UtcNow.AddDays(7)
            };
        }

        private float GetDifficultyMultiplier(ChallengeDifficulty difficulty)
        {
            return difficulty switch
            {
                ChallengeDifficulty.Easy => 1f,
                ChallengeDifficulty.Normal => 1.5f,
                ChallengeDifficulty.Hard => 2f,
                ChallengeDifficulty.Expert => 3f,
                _ => 1f
            };
        }

        [ServerRpc(RequireOwnership = false)]
        public void UpdateChallengeProgressServerRpc(ulong playerId, RequirementType requirementType, float amount, ServerRpcParams rpcParams = default)
        {
            if (!playerChallenges.TryGetValue(playerId, out var playerData))
            {
                return;
            }

            var relevantChallenges = activeChallenges.Values
                .Where(c => c.challengeType == requirementType &&
                           c.status == ChallengeStatus.Active &&
                           (playerData.dailyChallenges.Contains(c.challengeId) || playerData.weeklyChallenges.Contains(c.challengeId)))
                .ToList();

            foreach (var challenge in relevantChallenges)
            {
                challenge.currentAmount += amount;
                playerData.challengeProgress[challenge.challengeId] = challenge.currentAmount;

                OnChallengeProgress?.Invoke(playerId, challenge.challengeId, challenge.currentAmount / challenge.requiredAmount);

                if (challenge.currentAmount >= challenge.requiredAmount)
                {
                    CompleteChallenge(playerId, challenge.challengeId);
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void CompleteChallengeServerRpc(ulong playerId, string challengeId, ServerRpcParams rpcParams = default)
        {
            CompleteChallenge(playerId, challengeId);
        }

        private void CompleteChallenge(ulong playerId, string challengeId)
        {
            if (!activeChallenges.TryGetValue(challengeId, out var challenge))
            {
                return;
            }

            if (challenge.status != ChallengeStatus.Active)
            {
                return;
            }

            if (!playerChallenges.TryGetValue(playerId, out var playerData))
            {
                return;
            }

            challenge.status = ChallengeStatus.Completed;
            challenge.completionDate = DateTime.UtcNow;
            playerData.completedChallenges.Add(challengeId);

            // Update streak
            if (enableStreakSystem && challenge.frequency == ChallengeFrequency.Daily)
            {
                UpdateDailyStreak(playerData);
            }

            // Calculate rewards with streak bonus
            int currencyReward = challenge.rewardCurrency;
            if (enableStreakSystem && playerData.dailyStreak > 0)
            {
                float bonus = 1 + (playerData.dailyStreak * streakBonusMultiplier);
                bonus = Mathf.Min(bonus, 1 + (maxStreakBonus * streakBonusMultiplier));
                currencyReward = Mathf.RoundToInt(currencyReward * bonus);
            }

            // Award rewards
            Economy.EconomyManager.Instance?.AddSoftCurrency(playerId, currencyReward);

            if (challenge.rewardHardCurrency > 0)
            {
                Economy.EconomyManager.Instance?.AddHardCurrency(playerId, challenge.rewardHardCurrency);
            }

            OnChallengeCompleted?.Invoke(playerId, challengeId);
            NotifyChallengeCompletedClientRpc(playerId, challengeId, currencyReward, challenge.rewardHardCurrency);

            Debug.Log($"Challenge completed by player {playerId}: {challenge.title} (+{currencyReward} currency)");
        }

        private void UpdateDailyStreak(PlayerChallengeData playerData)
        {
            DateTime today = DateTime.UtcNow.Date;
            DateTime lastCompletion = playerData.lastCompletionDate.Date;

            if (lastCompletion == today)
            {
                // Already completed a challenge today
                return;
            }
            else if (lastCompletion == today.AddDays(-1))
            {
                // Consecutive day
                playerData.dailyStreak++;
            }
            else
            {
                // Streak broken
                playerData.dailyStreak = 1;
            }

            playerData.dailyStreak = Mathf.Min(playerData.dailyStreak, maxStreakBonus);
            playerData.lastCompletionDate = today;

            OnStreakUpdated?.Invoke(playerData.playerId, playerData.dailyStreak);
            NotifyStreakUpdatedClientRpc(playerData.playerId, playerData.dailyStreak);
        }

        [ServerRpc(RequireOwnership = false)]
        public void RerollChallengeServerRpc(ulong playerId, string challengeId, ServerRpcParams rpcParams = default)
        {
            if (!enableReroll)
            {
                Debug.LogWarning("Reroll system is disabled");
                return;
            }

            if (!playerChallenges.TryGetValue(playerId, out var playerData))
            {
                return;
            }

            // Check reroll limit
            DateTime today = DateTime.UtcNow.Date;
            if (playerData.lastRerollResetDate.Date != today)
            {
                playerData.rerollsUsedToday = 0;
                playerData.lastRerollResetDate = today;
            }

            if (playerData.rerollsUsedToday >= maxRerollsPerDay)
            {
                Debug.LogWarning($"Player {playerId} has used all rerolls today");
                return;
            }

            // Charge reroll cost
            if (!Economy.EconomyManager.Instance.SpendSoftCurrency(playerId, rerollCost))
            {
                Debug.LogWarning($"Player {playerId} cannot afford reroll");
                return;
            }

            // Determine challenge type
            bool isDaily = playerData.dailyChallenges.Contains(challengeId);

            // Remove old challenge
            activeChallenges.Remove(challengeId);
            playerData.challengeProgress.Remove(challengeId);

            if (isDaily)
            {
                playerData.dailyChallenges.Remove(challengeId);

                // Generate new daily challenge
                var templates = SelectRandomChallenges(ChallengeType.Combat, ChallengeType.Survival, ChallengeType.Economy);
                if (templates.Count > 0)
                {
                    var newChallenge = CreateChallengeFromTemplate(templates[0], ChallengeFrequency.Daily);
                    activeChallenges[newChallenge.challengeId] = newChallenge;
                    playerData.dailyChallenges.Add(newChallenge.challengeId);
                    playerData.challengeProgress[newChallenge.challengeId] = 0f;
                }
            }
            else
            {
                playerData.weeklyChallenges.Remove(challengeId);

                // Generate new weekly challenge
                var weeklyTemplates = challengeTemplates[ChallengeType.Weekly];
                if (weeklyTemplates.Count > 0)
                {
                    var template = weeklyTemplates[UnityEngine.Random.Range(0, weeklyTemplates.Count)];
                    var newChallenge = CreateChallengeFromTemplate(template, ChallengeFrequency.Weekly);
                    activeChallenges[newChallenge.challengeId] = newChallenge;
                    playerData.weeklyChallenges.Add(newChallenge.challengeId);
                    playerData.challengeProgress[newChallenge.challengeId] = 0f;
                }
            }

            playerData.rerollsUsedToday++;
            NotifyChallengeRerolledClientRpc(playerId, challengeId);

            Debug.Log($"Challenge rerolled for player {playerId}");
        }

        #endregion

        #region Client RPCs

        [ClientRpc]
        private void BroadcastChallengesRefreshedClientRpc()
        {
            OnChallengesRefreshed?.Invoke();
        }

        [ClientRpc]
        private void NotifyChallengeCompletedClientRpc(ulong playerId, string challengeId, int currencyReward, int hardCurrencyReward)
        {
            OnChallengeCompleted?.Invoke(playerId, challengeId);
        }

        [ClientRpc]
        private void NotifyStreakUpdatedClientRpc(ulong playerId, int streak)
        {
            OnStreakUpdated?.Invoke(playerId, streak);
        }

        [ClientRpc]
        private void NotifyChallengeRerolledClientRpc(ulong playerId, string oldChallengeId)
        {
            // Client-side notification
        }

        #endregion

        #region Public API

        public PlayerChallengeData GetPlayerChallenges(ulong playerId)
        {
            return playerChallenges.GetValueOrDefault(playerId);
        }

        public List<Challenge> GetActiveChallenges(ulong playerId, ChallengeFrequency? frequency = null)
        {
            if (!playerChallenges.TryGetValue(playerId, out var playerData))
            {
                return new List<Challenge>();
            }

            var challengeIds = frequency switch
            {
                ChallengeFrequency.Daily => playerData.dailyChallenges,
                ChallengeFrequency.Weekly => playerData.weeklyChallenges,
                _ => playerData.dailyChallenges.Concat(playerData.weeklyChallenges).ToList()
            };

            return challengeIds
                .Select(id => activeChallenges.GetValueOrDefault(id))
                .Where(c => c != null)
                .ToList();
        }

        public Challenge GetChallenge(string challengeId)
        {
            return activeChallenges.GetValueOrDefault(challengeId);
        }

        public int GetDailyStreak(ulong playerId)
        {
            if (playerChallenges.TryGetValue(playerId, out var playerData))
            {
                return playerData.dailyStreak;
            }
            return 0;
        }

        public int GetRemainingRerolls(ulong playerId)
        {
            if (!playerChallenges.TryGetValue(playerId, out var playerData))
            {
                return maxRerollsPerDay;
            }

            DateTime today = DateTime.UtcNow.Date;
            if (playerData.lastRerollResetDate.Date != today)
            {
                return maxRerollsPerDay;
            }

            return maxRerollsPerDay - playerData.rerollsUsedToday;
        }

        public DateTime GetNextDailyReset()
        {
            return lastDailyRefresh.AddDays(1);
        }

        public DateTime GetNextWeeklyReset()
        {
            return lastWeeklyRefresh.AddDays(7);
        }

        #endregion
    }

    #region Data Structures

    [Serializable]
    public class PlayerChallengeData
    {
        public ulong playerId;
        public List<string> dailyChallenges;
        public List<string> weeklyChallenges;
        public Dictionary<string, float> challengeProgress;
        public List<string> completedChallenges;
        public int dailyStreak;
        public DateTime lastCompletionDate;
        public int rerollsUsedToday;
        public DateTime lastRerollResetDate;
    }

    [Serializable]
    public class Challenge
    {
        public string challengeId;
        public string templateId;
        public string title;
        public string description;
        public RequirementType challengeType;
        public float requiredAmount;
        public float currentAmount;
        public ChallengeDifficulty difficulty;
        public ChallengeFrequency frequency;
        public int rewardCurrency;
        public int rewardHardCurrency;
        public ChallengeStatus status;
        public DateTime assignedDate;
        public DateTime expirationDate;
        public DateTime? completionDate;
    }

    [Serializable]
    public class ChallengeTemplate
    {
        public string templateId;
        public string title;
        public string description;
        public RequirementType requirementType;
        public float requiredAmount;
        public ChallengeDifficulty difficulty;
    }

    public enum ChallengeType
    {
        Combat,
        Survival,
        Economy,
        Crafting,
        Exploration,
        Social,
        Weekly
    }

    public enum ChallengeFrequency
    {
        Daily,
        Weekly
    }

    public enum ChallengeStatus
    {
        Active,
        Completed,
        Expired,
        Abandoned
    }

    public enum ChallengeDifficulty
    {
        Easy,
        Normal,
        Hard,
        Expert
    }

    public enum RequirementType
    {
        KillZombies,
        HeadshotKills,
        MeleeKills,
        ExplosiveKills,
        KillStreak,
        SurvivalTime,
        NoDamageSurvival,
        ReviveTeammates,
        CurrencyEarned,
        ChestsLooted,
        TradesCompleted,
        ItemsCrafted,
        WeaponsCrafted,
        ItemsUpgraded,
        DistanceTraveled,
        LocationsDiscovered,
        ZonesVisited,
        PartyMissions,
        HelpNewPlayers,
        BossesDefeated
    }

    #endregion
}
