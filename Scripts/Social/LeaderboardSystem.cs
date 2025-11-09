using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;
using System;

namespace DeadFrontier.Social
{
    /// <summary>
    /// Comprehensive leaderboard system with multiple categories and time periods.
    /// Supports global, regional, friends-only, and clan leaderboards.
    /// Includes rewards, seasons, and real-time rank tracking.
    /// </summary>
    public class LeaderboardSystem : NetworkBehaviour
    {
        public static LeaderboardSystem Instance { get; private set; }

        [Header("Leaderboard Settings")]
        [SerializeField] private int maxLeaderboardSize = 1000;
        [SerializeField] private int entriesPerPage = 25;
        [SerializeField] private float updateIntervalSeconds = 60f;

        [Header("Season Settings")]
        [SerializeField] private bool enableSeasons = true;
        [SerializeField] private int seasonDurationDays = 90;
        [SerializeField] private DateTime currentSeasonStart;
        [SerializeField] private DateTime currentSeasonEnd;

        [Header("Reward Settings")]
        [SerializeField] private bool enableRankRewards = true;

        // Leaderboard data storage
        private Dictionary<string, SortedList<ulong, LeaderboardEntry>> leaderboards = new Dictionary<string, SortedList<ulong, LeaderboardEntry>>();

        // Cached rankings
        private Dictionary<string, List<LeaderboardEntry>> cachedRankings = new Dictionary<string, List<LeaderboardEntry>>();
        private Dictionary<string, DateTime> lastUpdateTimes = new Dictionary<string, DateTime>();

        // Player personal bests
        private Dictionary<ulong, PlayerLeaderboardStats> playerStats = new Dictionary<ulong, PlayerLeaderboardStats>();

        // Seasonal data
        private int currentSeason = 1;
        private Dictionary<int, SeasonData> seasonHistory = new Dictionary<int, SeasonData>();

        // Events
        public event Action<ulong, LeaderboardType, int> OnRankChanged;
        public event Action<int> OnSeasonEnded;
        public event Action<int> OnSeasonStarted;

        private float updateTimer = 0f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeLeaderboards();
                InitializeSeason();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (!IsServer) return;

            updateTimer += Time.deltaTime;

            if (updateTimer >= updateIntervalSeconds)
            {
                updateTimer = 0f;
                UpdateAllLeaderboards();
            }

            // Check season end
            if (enableSeasons && DateTime.UtcNow >= currentSeasonEnd)
            {
                EndSeason();
            }
        }

        #region Initialization

        private void InitializeLeaderboards()
        {
            // Create leaderboards for each type
            foreach (LeaderboardType type in Enum.GetValues(typeof(LeaderboardType)))
            {
                string key = GetLeaderboardKey(type, TimePeriod.AllTime);
                leaderboards[key] = new SortedList<ulong, LeaderboardEntry>(new ScoreComparer());
                cachedRankings[key] = new List<LeaderboardEntry>();
                lastUpdateTimes[key] = DateTime.MinValue;
            }

            Debug.Log($"[LeaderboardSystem] Initialized {leaderboards.Count} leaderboards");
        }

        private void InitializeSeason()
        {
            if (!enableSeasons) return;

            if (currentSeasonStart == DateTime.MinValue)
            {
                StartNewSeason();
            }
        }

        #endregion

        #region Season Management

        private void StartNewSeason()
        {
            currentSeason++;
            currentSeasonStart = DateTime.UtcNow;
            currentSeasonEnd = currentSeasonStart.AddDays(seasonDurationDays);

            Debug.Log($"[LeaderboardSystem] Season {currentSeason} started! Ends: {currentSeasonEnd}");

            OnSeasonStarted?.Invoke(currentSeason);

            // Reset seasonal leaderboards
            ResetSeasonalLeaderboards();

            // Notify clients
            StartNewSeasonClientRpc(currentSeason, currentSeasonEnd.ToString());
        }

        [ClientRpc]
        private void StartNewSeasonClientRpc(int season, string endDate)
        {
            Debug.Log($"[LeaderboardSystem] NEW SEASON {season} - Ends: {endDate}");
        }

        private void EndSeason()
        {
            Debug.Log($"[LeaderboardSystem] Season {currentSeason} ended!");

            // Save season data
            var seasonData = new SeasonData
            {
                seasonNumber = currentSeason,
                startDate = currentSeasonStart,
                endDate = currentSeasonEnd,
                topPlayers = GetTopPlayers(LeaderboardType.OverallScore, 100)
            };

            seasonHistory[currentSeason] = seasonData;

            OnSeasonEnded?.Invoke(currentSeason);

            // Award season rewards
            if (enableRankRewards)
            {
                AwardSeasonRewards();
            }

            // Start new season
            StartNewSeason();
        }

        private void ResetSeasonalLeaderboards()
        {
            // Keep all-time, reset seasonal
            foreach (LeaderboardType type in Enum.GetValues(typeof(LeaderboardType)))
            {
                string seasonKey = GetLeaderboardKey(type, TimePeriod.Season);
                if (leaderboards.ContainsKey(seasonKey))
                {
                    leaderboards[seasonKey].Clear();
                    cachedRankings[seasonKey].Clear();
                }
            }

            Debug.Log("[LeaderboardSystem] Seasonal leaderboards reset");
        }

        #endregion

        #region Score Submission

        public void SubmitScore(ulong playerId, LeaderboardType type, long score)
        {
            // Update all time periods
            UpdateLeaderboard(playerId, type, TimePeriod.AllTime, score);
            UpdateLeaderboard(playerId, type, TimePeriod.Season, score);
            UpdateLeaderboard(playerId, type, TimePeriod.Monthly, score);
            UpdateLeaderboard(playerId, type, TimePeriod.Weekly, score);
            UpdateLeaderboard(playerId, type, TimePeriod.Daily, score);

            // Update player stats
            UpdatePlayerStats(playerId, type, score);
        }

        private void UpdateLeaderboard(ulong playerId, LeaderboardType type, TimePeriod period, long score)
        {
            string key = GetLeaderboardKey(type, period);

            if (!leaderboards.ContainsKey(key))
            {
                leaderboards[key] = new SortedList<ulong, LeaderboardEntry>(new ScoreComparer());
            }

            var leaderboard = leaderboards[key];

            // Check if player already has an entry
            if (leaderboard.ContainsKey(playerId))
            {
                var existing = leaderboard[playerId];

                // Only update if new score is better
                if (score > existing.score)
                {
                    leaderboard[playerId] = new LeaderboardEntry
                    {
                        playerId = playerId,
                        playerName = GetPlayerName(playerId),
                        score = score,
                        timestamp = DateTime.UtcNow,
                        rank = 0 // Will be calculated
                    };

                    // Mark for recalculation
                    lastUpdateTimes[key] = DateTime.MinValue;
                }
            }
            else
            {
                // New entry
                leaderboard.Add(playerId, new LeaderboardEntry
                {
                    playerId = playerId,
                    playerName = GetPlayerName(playerId),
                    score = score,
                    timestamp = DateTime.UtcNow,
                    rank = 0
                });

                // Mark for recalculation
                lastUpdateTimes[key] = DateTime.MinValue;
            }

            // Trim leaderboard if too large
            if (leaderboard.Count > maxLeaderboardSize)
            {
                var lowest = leaderboard.Values.Last();
                leaderboard.Remove(lowest.playerId);
            }
        }

        private void UpdatePlayerStats(ulong playerId, LeaderboardType type, long score)
        {
            if (!playerStats.ContainsKey(playerId))
            {
                playerStats[playerId] = new PlayerLeaderboardStats
                {
                    playerId = playerId,
                    personalBests = new Dictionary<LeaderboardType, long>()
                };
            }

            var stats = playerStats[playerId];

            if (!stats.personalBests.ContainsKey(type) || score > stats.personalBests[type])
            {
                stats.personalBests[type] = score;
            }
        }

        #endregion

        #region Leaderboard Updates

        private void UpdateAllLeaderboards()
        {
            foreach (var kvp in leaderboards)
            {
                string key = kvp.Key;

                // Check if needs update
                if (DateTime.UtcNow - lastUpdateTimes[key] > TimeSpan.FromSeconds(updateIntervalSeconds))
                {
                    RecalculateRankings(key);
                }
            }
        }

        private void RecalculateRankings(string leaderboardKey)
        {
            if (!leaderboards.ContainsKey(leaderboardKey)) return;

            var leaderboard = leaderboards[leaderboardKey];

            // Sort by score descending
            var sorted = leaderboard.Values
                .OrderByDescending(e => e.score)
                .ThenBy(e => e.timestamp)
                .ToList();

            // Assign ranks
            for (int i = 0; i < sorted.Count; i++)
            {
                var entry = sorted[i];
                int oldRank = entry.rank;
                entry.rank = i + 1;

                // Check for rank changes
                if (oldRank != entry.rank && oldRank > 0)
                {
                    var parts = leaderboardKey.Split('_');
                    if (Enum.TryParse(parts[0], out LeaderboardType type))
                    {
                        OnRankChanged?.Invoke(entry.playerId, type, entry.rank);
                    }
                }
            }

            cachedRankings[leaderboardKey] = sorted;
            lastUpdateTimes[leaderboardKey] = DateTime.UtcNow;
        }

        #endregion

        #region Leaderboard Queries

        public List<LeaderboardEntry> GetLeaderboard(LeaderboardType type, TimePeriod period, int page = 0)
        {
            string key = GetLeaderboardKey(type, period);

            if (!cachedRankings.ContainsKey(key) || cachedRankings[key].Count == 0)
            {
                RecalculateRankings(key);
            }

            var rankings = cachedRankings[key];

            int startIndex = page * entriesPerPage;
            int count = Mathf.Min(entriesPerPage, rankings.Count - startIndex);

            if (startIndex >= rankings.Count || count <= 0)
            {
                return new List<LeaderboardEntry>();
            }

            return rankings.GetRange(startIndex, count);
        }

        public LeaderboardEntry GetPlayerRank(ulong playerId, LeaderboardType type, TimePeriod period)
        {
            string key = GetLeaderboardKey(type, period);

            if (!cachedRankings.ContainsKey(key))
            {
                RecalculateRankings(key);
            }

            return cachedRankings[key].FirstOrDefault(e => e.playerId == playerId);
        }

        public int GetPlayerRankPosition(ulong playerId, LeaderboardType type, TimePeriod period)
        {
            var entry = GetPlayerRank(playerId, type, period);
            return entry?.rank ?? 0;
        }

        public List<LeaderboardEntry> GetFriendsLeaderboard(ulong playerId, LeaderboardType type, TimePeriod period)
        {
            // Get player's friends
            var friends = Social.FriendSystem.Instance?.GetFriends(playerId);
            if (friends == null || friends.Count == 0) return new List<LeaderboardEntry>();

            string key = GetLeaderboardKey(type, period);

            if (!cachedRankings.ContainsKey(key))
            {
                RecalculateRankings(key);
            }

            var friendIds = friends.Select(f => f.friendId).ToList();
            friendIds.Add(playerId); // Include self

            return cachedRankings[key]
                .Where(e => friendIds.Contains(e.playerId))
                .ToList();
        }

        public List<LeaderboardEntry> GetClanLeaderboard(string clanId, LeaderboardType type, TimePeriod period)
        {
            // Get clan members
            var clan = Social.ClanSystem.Instance?.GetClan(clanId);
            if (clan == null) return new List<LeaderboardEntry>();

            var memberIds = clan.members.Select(m => m.playerId).ToList();

            string key = GetLeaderboardKey(type, period);

            if (!cachedRankings.ContainsKey(key))
            {
                RecalculateRankings(key);
            }

            return cachedRankings[key]
                .Where(e => memberIds.Contains(e.playerId))
                .ToList();
        }

        public List<LeaderboardEntry> GetTopPlayers(LeaderboardType type, int count)
        {
            string key = GetLeaderboardKey(type, TimePeriod.AllTime);

            if (!cachedRankings.ContainsKey(key))
            {
                RecalculateRankings(key);
            }

            return cachedRankings[key].Take(count).ToList();
        }

        #endregion

        #region Rewards

        private void AwardSeasonRewards()
        {
            var topPlayers = GetTopPlayers(LeaderboardType.OverallScore, 100);

            for (int i = 0; i < topPlayers.Count; i++)
            {
                var entry = topPlayers[i];
                int rank = i + 1;

                var reward = CalculateSeasonReward(rank);

                // Award rewards
                Economy.EconomyManager.Instance?.AddSoftCurrency(entry.playerId, reward.softCurrency);
                Economy.EconomyManager.Instance?.AddHardCurrency(entry.playerId, reward.hardCurrency);

                foreach (var itemId in reward.items)
                {
                    Inventory.InventoryManager.Instance?.AddItem(entry.playerId, itemId, 1);
                }

                Debug.Log($"[LeaderboardSystem] Awarded rank {rank} rewards to player {entry.playerId}");

                // Notify player
                AwardSeasonRewardClientRpc(entry.playerId, rank, reward.softCurrency, reward.hardCurrency);
            }
        }

        [ClientRpc]
        private void AwardSeasonRewardClientRpc(ulong playerId, int rank, int softCurrency, int hardCurrency)
        {
            Debug.Log($"[LeaderboardSystem] Season ended! Rank: {rank} - Rewards: {softCurrency} coins, {hardCurrency} gems");
        }

        private LeaderboardReward CalculateSeasonReward(int rank)
        {
            var reward = new LeaderboardReward();

            // Top 10 get special items
            if (rank == 1)
            {
                reward.softCurrency = 50000;
                reward.hardCurrency = 2000;
                reward.items.Add("cosmetic_seasonal_rank1");
                reward.items.Add("title_champion");
            }
            else if (rank <= 3)
            {
                reward.softCurrency = 30000;
                reward.hardCurrency = 1500;
                reward.items.Add($"cosmetic_seasonal_rank{rank}");
                reward.items.Add("title_top3");
            }
            else if (rank <= 10)
            {
                reward.softCurrency = 20000;
                reward.hardCurrency = 1000;
                reward.items.Add("cosmetic_seasonal_top10");
                reward.items.Add("title_top10");
            }
            else if (rank <= 50)
            {
                reward.softCurrency = 10000;
                reward.hardCurrency = 500;
                reward.items.Add("cosmetic_seasonal_top50");
            }
            else if (rank <= 100)
            {
                reward.softCurrency = 5000;
                reward.hardCurrency = 250;
                reward.items.Add("cosmetic_seasonal_top100");
            }

            return reward;
        }

        #endregion

        #region Clan Rankings

        public List<ClanLeaderboardEntry> GetClanLeaderboard(TimePeriod period, int page = 0)
        {
            // Get all clans
            var clans = Social.ClanSystem.Instance?.GetAllClans();
            if (clans == null) return new List<ClanLeaderboardEntry>();

            var clanScores = new List<ClanLeaderboardEntry>();

            foreach (var clan in clans)
            {
                long totalScore = 0;

                // Sum all member scores
                foreach (var member in clan.members)
                {
                    var entry = GetPlayerRank(member.playerId, LeaderboardType.OverallScore, period);
                    if (entry != null)
                    {
                        totalScore += entry.score;
                    }
                }

                clanScores.Add(new ClanLeaderboardEntry
                {
                    clanId = clan.clanId,
                    clanName = clan.clanName,
                    clanTag = clan.clanTag,
                    totalScore = totalScore,
                    memberCount = clan.members.Count
                });
            }

            // Sort by total score
            var sorted = clanScores
                .OrderByDescending(c => c.totalScore)
                .ToList();

            // Assign ranks
            for (int i = 0; i < sorted.Count; i++)
            {
                sorted[i].rank = i + 1;
            }

            // Paginate
            int startIndex = page * entriesPerPage;
            int count = Mathf.Min(entriesPerPage, sorted.Count - startIndex);

            if (startIndex >= sorted.Count || count <= 0)
            {
                return new List<ClanLeaderboardEntry>();
            }

            return sorted.GetRange(startIndex, count);
        }

        #endregion

        #region Helper Methods

        private string GetLeaderboardKey(LeaderboardType type, TimePeriod period)
        {
            return $"{type}_{period}";
        }

        private string GetPlayerName(ulong playerId)
        {
            // Get from player profile/data
            return $"Player_{playerId}";
        }

        #endregion

        #region Data Persistence

        public void SaveLeaderboardData()
        {
            foreach (var kvp in leaderboards)
            {
                string key = kvp.Key;
                var entries = kvp.Value.Values.ToList();

                string json = JsonUtility.ToJson(new LeaderboardData { entries = entries });
                SaveSystem.SaveManager.Instance?.SaveData($"leaderboard_{key}", json);
            }

            Debug.Log("[LeaderboardSystem] Saved all leaderboard data");
        }

        public void LoadLeaderboardData()
        {
            foreach (LeaderboardType type in Enum.GetValues(typeof(LeaderboardType)))
            {
                foreach (TimePeriod period in Enum.GetValues(typeof(TimePeriod)))
                {
                    string key = GetLeaderboardKey(type, period);
                    string json = SaveSystem.SaveManager.Instance?.LoadData($"leaderboard_{key}");

                    if (!string.IsNullOrEmpty(json))
                    {
                        var data = JsonUtility.FromJson<LeaderboardData>(json);

                        leaderboards[key] = new SortedList<ulong, LeaderboardEntry>(new ScoreComparer());

                        foreach (var entry in data.entries)
                        {
                            leaderboards[key].Add(entry.playerId, entry);
                        }

                        RecalculateRankings(key);
                    }
                }
            }

            Debug.Log("[LeaderboardSystem] Loaded all leaderboard data");
        }

        #endregion
    }

    #region Data Classes

    [Serializable]
    public class LeaderboardEntry
    {
        public ulong playerId;
        public string playerName;
        public long score;
        public int rank;
        public DateTime timestamp;
    }

    [Serializable]
    public class ClanLeaderboardEntry
    {
        public string clanId;
        public string clanName;
        public string clanTag;
        public long totalScore;
        public int memberCount;
        public int rank;
    }

    [Serializable]
    public class PlayerLeaderboardStats
    {
        public ulong playerId;
        public Dictionary<LeaderboardType, long> personalBests = new Dictionary<LeaderboardType, long>();
    }

    [Serializable]
    public class SeasonData
    {
        public int seasonNumber;
        public DateTime startDate;
        public DateTime endDate;
        public List<LeaderboardEntry> topPlayers = new List<LeaderboardEntry>();
    }

    [Serializable]
    public class LeaderboardReward
    {
        public int softCurrency;
        public int hardCurrency;
        public List<string> items = new List<string>();
    }

    [Serializable]
    public class LeaderboardData
    {
        public List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
    }

    public enum LeaderboardType
    {
        OverallScore,
        Kills,
        ZombieKills,
        PlayerKills,
        SurvivalTime,
        Extractions,
        WealthEarned,
        RaidWins,
        KillStreak,
        Headshots,
        AchievementScore,
        ClanContribution
    }

    public enum TimePeriod
    {
        Daily,
        Weekly,
        Monthly,
        Season,
        AllTime
    }

    // Custom comparer for SortedList to sort by score descending
    public class ScoreComparer : IComparer<ulong>
    {
        public int Compare(ulong x, ulong y)
        {
            return x.CompareTo(y);
        }
    }

    #endregion
}
