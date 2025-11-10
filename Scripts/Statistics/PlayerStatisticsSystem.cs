using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Statistics
{
    /// <summary>
    /// Comprehensive player statistics and analytics system.
    /// Tracks all player activities, achievements, and provides detailed analytics.
    /// </summary>
    public class PlayerStatisticsSystem : NetworkBehaviour
    {
        public static PlayerStatisticsSystem Instance { get; private set; }

        [Header("Statistics Configuration")]
        [SerializeField] private bool trackDetailedStats = true;
        [SerializeField] private float statSaveInterval = 300f; // 5 minutes
        [SerializeField] private int maxLeaderboardEntries = 100;
        [SerializeField] private int maxHistoryEntries = 1000;
        [SerializeField] private bool enableHeatmaps = true;

        [Header("Session Tracking")]
        [SerializeField] private bool trackSessionStats = true;
        [SerializeField] private int maxSessionsStored = 50;

        [Header("Comparison Features")]
        [SerializeField] private bool enablePlayerComparison = true;
        [SerializeField] private int maxComparisonPlayers = 5;

        // Data structures
        private Dictionary<ulong, PlayerStatistics> playerStats = new Dictionary<ulong, PlayerStatistics>();
        private Dictionary<ulong, SessionStatistics> currentSessions = new Dictionary<ulong, SessionStatistics>();
        private Dictionary<StatCategory, Leaderboard> leaderboards = new Dictionary<StatCategory, Leaderboard>();
        private Dictionary<ulong, List<Vector3>> playerHeatmapData = new Dictionary<ulong, List<Vector3>>();
        private Dictionary<string, GlobalStatistics> globalStats = new Dictionary<string, GlobalStatistics>();

        // Events
        public event Action<ulong, StatType, float> OnStatUpdated;
        public event Action<ulong, string> OnMilestoneReached;
        public event Action<ulong, SessionStatistics> OnSessionEnded;
        public event Action<StatCategory, List<LeaderboardEntry>> OnLeaderboardUpdated;

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
                InitializeLeaderboards();
                InitializeGlobalStats();
                InvokeRepeating(nameof(SaveAllStatistics), statSaveInterval, statSaveInterval);

                if (enableHeatmaps)
                {
                    InvokeRepeating(nameof(CollectHeatmapData), 10f, 10f);
                }
            }
        }

        private void InitializeLeaderboards()
        {
            foreach (StatCategory category in Enum.GetValues(typeof(StatCategory)))
            {
                leaderboards[category] = new Leaderboard
                {
                    category = category,
                    entries = new List<LeaderboardEntry>(),
                    lastUpdate = DateTime.UtcNow
                };
            }
        }

        private void InitializeGlobalStats()
        {
            globalStats["total_playtime"] = new GlobalStatistics { statName = "total_playtime", value = 0 };
            globalStats["total_kills"] = new GlobalStatistics { statName = "total_kills", value = 0 };
            globalStats["total_deaths"] = new GlobalStatistics { statName = "total_deaths", value = 0 };
            globalStats["total_items_crafted"] = new GlobalStatistics { statName = "total_items_crafted", value = 0 };
            globalStats["total_currency_earned"] = new GlobalStatistics { statName = "total_currency_earned", value = 0 };
        }

        private void CollectHeatmapData()
        {
            // Collect position data for heatmaps
            foreach (var playerId in playerStats.Keys)
            {
                // This would integrate with your player position system
                Vector3 position = GetPlayerPosition(playerId);

                if (!playerHeatmapData.ContainsKey(playerId))
                {
                    playerHeatmapData[playerId] = new List<Vector3>();
                }

                playerHeatmapData[playerId].Add(position);

                // Limit heatmap data size
                if (playerHeatmapData[playerId].Count > maxHistoryEntries)
                {
                    playerHeatmapData[playerId].RemoveAt(0);
                }
            }
        }

        private Vector3 GetPlayerPosition(ulong playerId)
        {
            // Placeholder - integrate with your player management system
            return Vector3.zero;
        }

        #region Server RPCs

        [ServerRpc(RequireOwnership = false)]
        public void InitializePlayerStatsServerRpc(ulong playerId, ServerRpcParams rpcParams = default)
        {
            if (!playerStats.ContainsKey(playerId))
            {
                playerStats[playerId] = CreateNewPlayerStatistics(playerId);
            }

            StartPlayerSession(playerId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void UpdateStatServerRpc(ulong playerId, StatType statType, float value, bool isIncrement = true, ServerRpcParams rpcParams = default)
        {
            if (!playerStats.ContainsKey(playerId))
            {
                InitializePlayerStatsServerRpc(playerId);
            }

            var stats = playerStats[playerId];
            UpdateStatInternal(stats, statType, value, isIncrement);

            // Update session stats
            if (currentSessions.ContainsKey(playerId))
            {
                UpdateSessionStat(currentSessions[playerId], statType, value, isIncrement);
            }

            // Update global stats
            UpdateGlobalStat(statType, value);

            OnStatUpdated?.Invoke(playerId, statType, value);
            NotifyStatUpdatedClientRpc(playerId, statType, GetStatValue(stats, statType));

            // Check for milestones
            CheckMilestones(playerId, stats, statType);
        }

        [ServerRpc(RequireOwnership = false)]
        public void RecordKillServerRpc(ulong playerId, string victimId, string weaponUsed, float distance, bool isHeadshot, ServerRpcParams rpcParams = default)
        {
            if (!playerStats.ContainsKey(playerId))
            {
                InitializePlayerStatsServerRpc(playerId);
            }

            var stats = playerStats[playerId];
            var combat = stats.combatStats;

            combat.totalKills++;
            combat.killsByWeapon[weaponUsed] = combat.killsByWeapon.GetValueOrDefault(weaponUsed) + 1;

            if (isHeadshot)
            {
                combat.headshots++;
            }

            if (distance > combat.longestKillDistance)
            {
                combat.longestKillDistance = distance;
            }

            // Update kill streak
            combat.currentKillStreak++;
            if (combat.currentKillStreak > combat.bestKillStreak)
            {
                combat.bestKillStreak = combat.currentKillStreak;
            }

            // Record kill in history
            stats.recentKills.Add(new KillRecord
            {
                timestamp = DateTime.UtcNow,
                victimId = victimId,
                weaponUsed = weaponUsed,
                distance = distance,
                wasHeadshot = isHeadshot
            });

            if (stats.recentKills.Count > 100)
            {
                stats.recentKills.RemoveAt(0);
            }

            // Update session stats
            if (currentSessions.ContainsKey(playerId))
            {
                currentSessions[playerId].kills++;
                if (isHeadshot) currentSessions[playerId].headshots++;
            }

            // Update leaderboards
            UpdateLeaderboard(StatCategory.Kills, playerId, combat.totalKills);

            Debug.Log($"Kill recorded for player {playerId}: weapon={weaponUsed}, distance={distance:F1}m, headshot={isHeadshot}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void RecordDeathServerRpc(ulong playerId, string killerId, string weaponUsed, ServerRpcParams rpcParams = default)
        {
            if (!playerStats.ContainsKey(playerId))
            {
                InitializePlayerStatsServerRpc(playerId);
            }

            var stats = playerStats[playerId];
            var combat = stats.combatStats;

            combat.totalDeaths++;
            combat.currentKillStreak = 0;

            // Record death in history
            stats.recentDeaths.Add(new DeathRecord
            {
                timestamp = DateTime.UtcNow,
                killerId = killerId,
                weaponUsed = weaponUsed
            });

            if (stats.recentDeaths.Count > 100)
            {
                stats.recentDeaths.RemoveAt(0);
            }

            // Update session stats
            if (currentSessions.ContainsKey(playerId))
            {
                currentSessions[playerId].deaths++;
            }

            // Update leaderboards
            UpdateLeaderboard(StatCategory.Deaths, playerId, combat.totalDeaths);
        }

        [ServerRpc(RequireOwnership = false)]
        public void RecordDamageServerRpc(ulong playerId, float damageDealt, float damageTaken, ServerRpcParams rpcParams = default)
        {
            if (!playerStats.ContainsKey(playerId))
            {
                InitializePlayerStatsServerRpc(playerId);
            }

            var stats = playerStats[playerId];
            var combat = stats.combatStats;

            combat.totalDamageDealt += damageDealt;
            combat.totalDamageTaken += damageTaken;

            // Update session stats
            if (currentSessions.ContainsKey(playerId))
            {
                currentSessions[playerId].damageDealt += damageDealt;
                currentSessions[playerId].damageTaken += damageTaken;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void RecordItemCraftedServerRpc(ulong playerId, string itemId, int quantity, ServerRpcParams rpcParams = default)
        {
            if (!playerStats.ContainsKey(playerId))
            {
                InitializePlayerStatsServerRpc(playerId);
            }

            var stats = playerStats[playerId];
            var economy = stats.economyStats;

            economy.totalItemsCrafted += quantity;
            economy.itemsCraftedByType[itemId] = economy.itemsCraftedByType.GetValueOrDefault(itemId) + quantity;

            // Update session stats
            if (currentSessions.ContainsKey(playerId))
            {
                currentSessions[playerId].itemsCrafted += quantity;
            }

            UpdateLeaderboard(StatCategory.Crafting, playerId, economy.totalItemsCrafted);
        }

        [ServerRpc(RequireOwnership = false)]
        public void RecordDistanceTraveledServerRpc(ulong playerId, float distance, TravelMethod method, ServerRpcParams rpcParams = default)
        {
            if (!playerStats.ContainsKey(playerId))
            {
                InitializePlayerStatsServerRpc(playerId);
            }

            var stats = playerStats[playerId];
            var exploration = stats.explorationStats;

            exploration.totalDistanceTraveled += distance;

            switch (method)
            {
                case TravelMethod.Walking:
                    exploration.distanceWalked += distance;
                    break;
                case TravelMethod.Running:
                    exploration.distanceRan += distance;
                    break;
                case TravelMethod.Vehicle:
                    exploration.distanceInVehicle += distance;
                    break;
            }

            // Update session stats
            if (currentSessions.ContainsKey(playerId))
            {
                currentSessions[playerId].distanceTraveled += distance;
            }

            UpdateLeaderboard(StatCategory.Exploration, playerId, exploration.totalDistanceTraveled);
        }

        [ServerRpc(RequireOwnership = false)]
        public void RecordLocationDiscoveredServerRpc(ulong playerId, string locationId, ServerRpcParams rpcParams = default)
        {
            if (!playerStats.ContainsKey(playerId))
            {
                InitializePlayerStatsServerRpc(playerId);
            }

            var stats = playerStats[playerId];
            var exploration = stats.explorationStats;

            if (!exploration.locationsDiscovered.Contains(locationId))
            {
                exploration.locationsDiscovered.Add(locationId);
                exploration.totalLocationsDiscovered = exploration.locationsDiscovered.Count;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void RecordCurrencyEarnedServerRpc(ulong playerId, int softCurrency, int hardCurrency, ServerRpcParams rpcParams = default)
        {
            if (!playerStats.ContainsKey(playerId))
            {
                InitializePlayerStatsServerRpc(playerId);
            }

            var stats = playerStats[playerId];
            var economy = stats.economyStats;

            economy.totalSoftCurrencyEarned += softCurrency;
            economy.totalHardCurrencyEarned += hardCurrency;

            // Update session stats
            if (currentSessions.ContainsKey(playerId))
            {
                currentSessions[playerId].currencyEarned += softCurrency;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void RecordCurrencySpentServerRpc(ulong playerId, int softCurrency, int hardCurrency, ServerRpcParams rpcParams = default)
        {
            if (!playerStats.ContainsKey(playerId))
            {
                InitializePlayerStatsServerRpc(playerId);
            }

            var stats = playerStats[playerId];
            var economy = stats.economyStats;

            economy.totalSoftCurrencySpent += softCurrency;
            economy.totalHardCurrencySpent += hardCurrency;

            // Update session stats
            if (currentSessions.ContainsKey(playerId))
            {
                currentSessions[playerId].currencySpent += softCurrency;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void RecordSocialActionServerRpc(ulong playerId, SocialAction action, ServerRpcParams rpcParams = default)
        {
            if (!playerStats.ContainsKey(playerId))
            {
                InitializePlayerStatsServerRpc(playerId);
            }

            var stats = playerStats[playerId];
            var social = stats.socialStats;

            switch (action)
            {
                case SocialAction.PartyFormed:
                    social.partiesFormed++;
                    break;
                case SocialAction.PartyJoined:
                    social.partiesJoined++;
                    break;
                case SocialAction.FriendAdded:
                    social.friendsAdded++;
                    break;
                case SocialAction.MessageSent:
                    social.messagesSent++;
                    break;
                case SocialAction.TradeCompleted:
                    social.tradesCompleted++;
                    break;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void EndPlayerSessionServerRpc(ulong playerId, ServerRpcParams rpcParams = default)
        {
            if (!currentSessions.ContainsKey(playerId))
            {
                return;
            }

            var session = currentSessions[playerId];
            session.endTime = DateTime.UtcNow;
            session.duration = (float)(session.endTime - session.startTime).TotalSeconds;

            // Update player stats with session data
            if (playerStats.ContainsKey(playerId))
            {
                var stats = playerStats[playerId];
                stats.sessionHistory.Add(session);

                if (stats.sessionHistory.Count > maxSessionsStored)
                {
                    stats.sessionHistory.RemoveAt(0);
                }

                stats.generalStats.totalPlaytime += session.duration;
                stats.generalStats.sessionsPlayed++;
            }

            OnSessionEnded?.Invoke(playerId, session);
            currentSessions.Remove(playerId);

            Debug.Log($"Session ended for player {playerId}: duration={session.duration:F1}s, kills={session.kills}, deaths={session.deaths}");
        }

        #endregion

        #region Session Management

        private void StartPlayerSession(ulong playerId)
        {
            var session = new SessionStatistics
            {
                sessionId = Guid.NewGuid().ToString(),
                playerId = playerId,
                startTime = DateTime.UtcNow,
                kills = 0,
                deaths = 0,
                headshots = 0,
                damageDealt = 0,
                damageTaken = 0,
                distanceTraveled = 0,
                itemsCrafted = 0,
                currencyEarned = 0,
                currencySpent = 0,
                xpGained = 0
            };

            currentSessions[playerId] = session;
        }

        private void UpdateSessionStat(SessionStatistics session, StatType statType, float value, bool isIncrement)
        {
            switch (statType)
            {
                case StatType.XPGained:
                    session.xpGained += (int)value;
                    break;
            }
        }

        #endregion

        #region Statistics Updates

        private void UpdateStatInternal(PlayerStatistics stats, StatType statType, float value, bool isIncrement)
        {
            switch (statType)
            {
                case StatType.XPGained:
                    if (isIncrement)
                        stats.generalStats.totalXPEarned += (int)value;
                    else
                        stats.generalStats.totalXPEarned = (int)value;
                    break;

                case StatType.LevelReached:
                    stats.generalStats.currentLevel = (int)value;
                    if (value > stats.generalStats.highestLevel)
                        stats.generalStats.highestLevel = (int)value;
                    break;

                case StatType.QuestsCompleted:
                    if (isIncrement)
                        stats.progressionStats.questsCompleted += (int)value;
                    else
                        stats.progressionStats.questsCompleted = (int)value;
                    break;

                case StatType.AchievementsUnlocked:
                    if (isIncrement)
                        stats.progressionStats.achievementsUnlocked += (int)value;
                    else
                        stats.progressionStats.achievementsUnlocked = (int)value;
                    break;
            }
        }

        private float GetStatValue(PlayerStatistics stats, StatType statType)
        {
            return statType switch
            {
                StatType.XPGained => stats.generalStats.totalXPEarned,
                StatType.LevelReached => stats.generalStats.currentLevel,
                StatType.QuestsCompleted => stats.progressionStats.questsCompleted,
                StatType.AchievementsUnlocked => stats.progressionStats.achievementsUnlocked,
                _ => 0f
            };
        }

        private void UpdateGlobalStat(StatType statType, float value)
        {
            string statName = statType switch
            {
                StatType.XPGained => "total_xp_earned",
                _ => null
            };

            if (statName != null)
            {
                if (!globalStats.ContainsKey(statName))
                {
                    globalStats[statName] = new GlobalStatistics { statName = statName, value = 0 };
                }
                globalStats[statName].value += value;
            }
        }

        #endregion

        #region Leaderboards

        private void UpdateLeaderboard(StatCategory category, ulong playerId, float value)
        {
            if (!leaderboards.ContainsKey(category))
            {
                return;
            }

            var leaderboard = leaderboards[category];
            var existingEntry = leaderboard.entries.FirstOrDefault(e => e.playerId == playerId);

            if (existingEntry != null)
            {
                existingEntry.value = value;
                existingEntry.lastUpdate = DateTime.UtcNow;
            }
            else
            {
                leaderboard.entries.Add(new LeaderboardEntry
                {
                    playerId = playerId,
                    playerName = GetPlayerName(playerId),
                    value = value,
                    rank = 0,
                    lastUpdate = DateTime.UtcNow
                });
            }

            // Sort and update ranks
            leaderboard.entries = leaderboard.entries.OrderByDescending(e => e.value).ToList();
            for (int i = 0; i < leaderboard.entries.Count; i++)
            {
                leaderboard.entries[i].rank = i + 1;
            }

            // Trim to max entries
            if (leaderboard.entries.Count > maxLeaderboardEntries)
            {
                leaderboard.entries = leaderboard.entries.Take(maxLeaderboardEntries).ToList();
            }

            leaderboard.lastUpdate = DateTime.UtcNow;
            OnLeaderboardUpdated?.Invoke(category, leaderboard.entries);
        }

        private string GetPlayerName(ulong playerId)
        {
            // Placeholder - integrate with your player management system
            return $"Player_{playerId}";
        }

        #endregion

        #region Milestones

        private void CheckMilestones(ulong playerId, PlayerStatistics stats, StatType statType)
        {
            var milestones = GetMilestonesForStat(statType);

            foreach (var milestone in milestones)
            {
                float currentValue = GetStatValue(stats, statType);
                if (currentValue >= milestone.threshold && !stats.milestonesReached.Contains(milestone.milestoneId))
                {
                    stats.milestonesReached.Add(milestone.milestoneId);
                    OnMilestoneReached?.Invoke(playerId, milestone.milestoneId);
                    NotifyMilestoneReachedClientRpc(playerId, milestone.milestoneId, milestone.name);
                }
            }
        }

        private List<Milestone> GetMilestonesForStat(StatType statType)
        {
            // Define milestones for different stat types
            var milestones = new List<Milestone>();

            switch (statType)
            {
                case StatType.XPGained:
                    milestones.Add(new Milestone { milestoneId = "xp_10k", name = "10,000 XP Earned", threshold = 10000 });
                    milestones.Add(new Milestone { milestoneId = "xp_100k", name = "100,000 XP Earned", threshold = 100000 });
                    milestones.Add(new Milestone { milestoneId = "xp_1m", name = "1,000,000 XP Earned", threshold = 1000000 });
                    break;

                case StatType.LevelReached:
                    milestones.Add(new Milestone { milestoneId = "level_10", name = "Reached Level 10", threshold = 10 });
                    milestones.Add(new Milestone { milestoneId = "level_25", name = "Reached Level 25", threshold = 25 });
                    milestones.Add(new Milestone { milestoneId = "level_50", name = "Reached Level 50", threshold = 50 });
                    milestones.Add(new Milestone { milestoneId = "level_100", name = "Reached Level 100", threshold = 100 });
                    break;
            }

            return milestones;
        }

        #endregion

        #region Data Management

        private PlayerStatistics CreateNewPlayerStatistics(ulong playerId)
        {
            return new PlayerStatistics
            {
                playerId = playerId,
                generalStats = new GeneralStats
                {
                    accountCreationDate = DateTime.UtcNow,
                    lastLoginDate = DateTime.UtcNow,
                    totalPlaytime = 0,
                    sessionsPlayed = 0,
                    currentLevel = 1,
                    highestLevel = 1,
                    totalXPEarned = 0
                },
                combatStats = new CombatStats
                {
                    totalKills = 0,
                    totalDeaths = 0,
                    headshots = 0,
                    totalDamageDealt = 0,
                    totalDamageTaken = 0,
                    currentKillStreak = 0,
                    bestKillStreak = 0,
                    longestKillDistance = 0,
                    killsByWeapon = new Dictionary<string, int>()
                },
                economyStats = new EconomyStats
                {
                    totalSoftCurrencyEarned = 0,
                    totalSoftCurrencySpent = 0,
                    totalHardCurrencyEarned = 0,
                    totalHardCurrencySpent = 0,
                    totalItemsCrafted = 0,
                    itemsCraftedByType = new Dictionary<string, int>()
                },
                explorationStats = new ExplorationStats
                {
                    totalDistanceTraveled = 0,
                    distanceWalked = 0,
                    distanceRan = 0,
                    distanceInVehicle = 0,
                    locationsDiscovered = new List<string>(),
                    totalLocationsDiscovered = 0
                },
                socialStats = new SocialStats
                {
                    partiesFormed = 0,
                    partiesJoined = 0,
                    friendsAdded = 0,
                    messagesSent = 0,
                    tradesCompleted = 0
                },
                progressionStats = new ProgressionStats
                {
                    questsCompleted = 0,
                    achievementsUnlocked = 0,
                    skillsUnlocked = 0,
                    blueprintsDiscovered = 0
                },
                sessionHistory = new List<SessionStatistics>(),
                recentKills = new List<KillRecord>(),
                recentDeaths = new List<DeathRecord>(),
                milestonesReached = new List<string>()
            };
        }

        private void SaveAllStatistics()
        {
            if (!IsServer)
                return;

            // This would integrate with your save system
            // SaveSystem.Instance?.SavePlayerStatistics(playerStats);

            Debug.Log($"Saved statistics for {playerStats.Count} players");
        }

        #endregion

        #region Client RPCs

        [ClientRpc]
        private void NotifyStatUpdatedClientRpc(ulong playerId, StatType statType, float newValue)
        {
            OnStatUpdated?.Invoke(playerId, statType, newValue);
        }

        [ClientRpc]
        private void NotifyMilestoneReachedClientRpc(ulong playerId, string milestoneId, string milestoneName)
        {
            OnMilestoneReached?.Invoke(playerId, milestoneId);
        }

        #endregion

        #region Public API

        public PlayerStatistics GetPlayerStatistics(ulong playerId)
        {
            if (!playerStats.ContainsKey(playerId))
            {
                playerStats[playerId] = CreateNewPlayerStatistics(playerId);
            }
            return playerStats[playerId];
        }

        public SessionStatistics GetCurrentSession(ulong playerId)
        {
            return currentSessions.GetValueOrDefault(playerId);
        }

        public List<LeaderboardEntry> GetLeaderboard(StatCategory category, int topN = 10)
        {
            if (!leaderboards.ContainsKey(category))
                return new List<LeaderboardEntry>();

            return leaderboards[category].entries.Take(topN).ToList();
        }

        public int GetPlayerRank(ulong playerId, StatCategory category)
        {
            if (!leaderboards.ContainsKey(category))
                return -1;

            var entry = leaderboards[category].entries.FirstOrDefault(e => e.playerId == playerId);
            return entry?.rank ?? -1;
        }

        public Dictionary<string, float> CompareWithPlayer(ulong playerId1, ulong playerId2)
        {
            if (!enablePlayerComparison)
                return null;

            var stats1 = GetPlayerStatistics(playerId1);
            var stats2 = GetPlayerStatistics(playerId2);

            return new Dictionary<string, float>
            {
                { "kills_diff", stats1.combatStats.totalKills - stats2.combatStats.totalKills },
                { "deaths_diff", stats1.combatStats.totalDeaths - stats2.combatStats.totalDeaths },
                { "kd_ratio_1", CalculateKDRatio(stats1) },
                { "kd_ratio_2", CalculateKDRatio(stats2) },
                { "playtime_diff", stats1.generalStats.totalPlaytime - stats2.generalStats.totalPlaytime },
                { "level_diff", stats1.generalStats.currentLevel - stats2.generalStats.currentLevel }
            };
        }

        public float CalculateKDRatio(PlayerStatistics stats)
        {
            if (stats.combatStats.totalDeaths == 0)
                return stats.combatStats.totalKills;

            return (float)stats.combatStats.totalKills / stats.combatStats.totalDeaths;
        }

        public List<Vector3> GetPlayerHeatmap(ulong playerId)
        {
            return playerHeatmapData.GetValueOrDefault(playerId, new List<Vector3>());
        }

        public GlobalStatistics GetGlobalStat(string statName)
        {
            return globalStats.GetValueOrDefault(statName);
        }

        public Dictionary<string, object> GeneratePlayerReport(ulong playerId)
        {
            var stats = GetPlayerStatistics(playerId);

            return new Dictionary<string, object>
            {
                { "player_id", playerId },
                { "total_playtime_hours", stats.generalStats.totalPlaytime / 3600f },
                { "sessions_played", stats.generalStats.sessionsPlayed },
                { "current_level", stats.generalStats.currentLevel },
                { "total_kills", stats.combatStats.totalKills },
                { "total_deaths", stats.combatStats.totalDeaths },
                { "kd_ratio", CalculateKDRatio(stats) },
                { "headshot_percentage", stats.combatStats.totalKills > 0 ? (float)stats.combatStats.headshots / stats.combatStats.totalKills * 100f : 0f },
                { "best_kill_streak", stats.combatStats.bestKillStreak },
                { "total_damage_dealt", stats.combatStats.totalDamageDealt },
                { "total_distance_km", stats.explorationStats.totalDistanceTraveled / 1000f },
                { "locations_discovered", stats.explorationStats.totalLocationsDiscovered },
                { "items_crafted", stats.economyStats.totalItemsCrafted },
                { "currency_earned", stats.economyStats.totalSoftCurrencyEarned },
                { "currency_spent", stats.economyStats.totalSoftCurrencySpent },
                { "quests_completed", stats.progressionStats.questsCompleted },
                { "achievements_unlocked", stats.progressionStats.achievementsUnlocked }
            };
        }

        #endregion
    }

    #region Data Structures

    [Serializable]
    public class PlayerStatistics
    {
        public ulong playerId;
        public GeneralStats generalStats;
        public CombatStats combatStats;
        public EconomyStats economyStats;
        public ExplorationStats explorationStats;
        public SocialStats socialStats;
        public ProgressionStats progressionStats;
        public List<SessionStatistics> sessionHistory;
        public List<KillRecord> recentKills;
        public List<DeathRecord> recentDeaths;
        public List<string> milestonesReached;
    }

    [Serializable]
    public class GeneralStats
    {
        public DateTime accountCreationDate;
        public DateTime lastLoginDate;
        public float totalPlaytime;
        public int sessionsPlayed;
        public int currentLevel;
        public int highestLevel;
        public int totalXPEarned;
    }

    [Serializable]
    public class CombatStats
    {
        public int totalKills;
        public int totalDeaths;
        public int headshots;
        public float totalDamageDealt;
        public float totalDamageTaken;
        public int currentKillStreak;
        public int bestKillStreak;
        public float longestKillDistance;
        public Dictionary<string, int> killsByWeapon;
    }

    [Serializable]
    public class EconomyStats
    {
        public int totalSoftCurrencyEarned;
        public int totalSoftCurrencySpent;
        public int totalHardCurrencyEarned;
        public int totalHardCurrencySpent;
        public int totalItemsCrafted;
        public Dictionary<string, int> itemsCraftedByType;
    }

    [Serializable]
    public class ExplorationStats
    {
        public float totalDistanceTraveled;
        public float distanceWalked;
        public float distanceRan;
        public float distanceInVehicle;
        public List<string> locationsDiscovered;
        public int totalLocationsDiscovered;
    }

    [Serializable]
    public class SocialStats
    {
        public int partiesFormed;
        public int partiesJoined;
        public int friendsAdded;
        public int messagesSent;
        public int tradesCompleted;
    }

    [Serializable]
    public class ProgressionStats
    {
        public int questsCompleted;
        public int achievementsUnlocked;
        public int skillsUnlocked;
        public int blueprintsDiscovered;
    }

    [Serializable]
    public class SessionStatistics
    {
        public string sessionId;
        public ulong playerId;
        public DateTime startTime;
        public DateTime endTime;
        public float duration;
        public int kills;
        public int deaths;
        public int headshots;
        public float damageDealt;
        public float damageTaken;
        public float distanceTraveled;
        public int itemsCrafted;
        public int currencyEarned;
        public int currencySpent;
        public int xpGained;
    }

    [Serializable]
    public class KillRecord
    {
        public DateTime timestamp;
        public string victimId;
        public string weaponUsed;
        public float distance;
        public bool wasHeadshot;
    }

    [Serializable]
    public class DeathRecord
    {
        public DateTime timestamp;
        public string killerId;
        public string weaponUsed;
    }

    [Serializable]
    public class Leaderboard
    {
        public StatCategory category;
        public List<LeaderboardEntry> entries;
        public DateTime lastUpdate;
    }

    [Serializable]
    public class LeaderboardEntry
    {
        public ulong playerId;
        public string playerName;
        public float value;
        public int rank;
        public DateTime lastUpdate;
    }

    [Serializable]
    public class Milestone
    {
        public string milestoneId;
        public string name;
        public float threshold;
    }

    [Serializable]
    public class GlobalStatistics
    {
        public string statName;
        public float value;
    }

    public enum StatType
    {
        XPGained,
        LevelReached,
        KillCount,
        DeathCount,
        DamageDealt,
        DamageTaken,
        DistanceTraveled,
        ItemsCrafted,
        CurrencyEarned,
        CurrencySpent,
        QuestsCompleted,
        AchievementsUnlocked
    }

    public enum StatCategory
    {
        Kills,
        Deaths,
        KDRatio,
        Playtime,
        Level,
        Crafting,
        Exploration,
        Economy,
        Social
    }

    public enum TravelMethod
    {
        Walking,
        Running,
        Vehicle,
        Teleport
    }

    public enum SocialAction
    {
        PartyFormed,
        PartyJoined,
        FriendAdded,
        MessageSent,
        TradeCompleted
    }

    #endregion
}
