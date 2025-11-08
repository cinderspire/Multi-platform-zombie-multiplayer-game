using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace DeadFrontier.Progression
{
    /// <summary>
    /// Comprehensive prestige and rank system for competitive progression.
    /// Includes military ranks, prestige tiers, seasonal rankings, and competitive matchmaking.
    /// Provides long-term progression goals and competitive ladders.
    /// </summary>
    public class PrestigeRankSystem : MonoBehaviour
    {
        public static PrestigeRankSystem Instance { get; private set; }

        [Header("Rank Settings")]
        [SerializeField] private RankData[] rankTiers;
        [SerializeField] private int maxRank = 100;
        [SerializeField] private float rankPointDecayRate = 0.02f; // 2% per day of inactivity

        [Header("Prestige Settings")]
        [SerializeField] private int maxPrestigeLevel = 10;
        [SerializeField] private int levelRequiredForPrestige = 100;
        [SerializeField] private PrestigeReward[] prestigeRewards;

        [Header("Season Settings")]
        [SerializeField] private bool enableSeasons = true;
        [SerializeField] private float seasonDurationDays = 90f; // 3 months
        [SerializeField] private SeasonData currentSeason;

        [Header("Competitive Settings")]
        [SerializeField] private CompetitiveRankData[] competitiveRanks;
        [SerializeField] private int placementMatchesRequired = 10;
        [SerializeField] private float rankPointsPerWin = 25f;
        [SerializeField] private float rankPointsPerLoss = -15f;

        [Header("Leaderboard Settings")]
        [SerializeField] private int leaderboardSize = 100;
        [SerializeField] private float leaderboardUpdateInterval = 60f; // 1 minute

        // Player progression data
        private Dictionary<ulong, PlayerRankData> playerRanks = new Dictionary<ulong, PlayerRankData>();
        private Dictionary<ulong, PrestigeData> playerPrestige = new Dictionary<ulong, PrestigeData>();
        private Dictionary<ulong, CompetitiveRank> playerCompetitiveRanks = new Dictionary<ulong, CompetitiveRank>();

        // Leaderboards
        private List<LeaderboardEntry> globalLeaderboard = new List<LeaderboardEntry>();
        private List<LeaderboardEntry> seasonLeaderboard = new List<LeaderboardEntry>();
        private float lastLeaderboardUpdate;

        // Events
        public event Action<ulong, int, int> OnRankUp;
        public event Action<ulong, int, int> OnRankDown;
        public event Action<ulong, int> OnPrestige;
        public event Action<ulong, CompetitiveRankTier, CompetitiveRankTier> OnCompetitiveRankChanged;
        public event Action<ulong, int> OnRankPointsChanged;
        public event Action OnSeasonEnded;

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
            LoadSeasonData();
            LoadAllPlayerData();
        }

        private void Update()
        {
            UpdateSeasonTimer();
            UpdateRankDecay();
            UpdateLeaderboards();
        }

        #region Rank System

        public void InitializePlayerRank(ulong playerId)
        {
            if (playerRanks.ContainsKey(playerId)) return;

            var rankData = new PlayerRankData
            {
                playerId = playerId,
                currentRank = 1,
                currentXP = 0,
                totalXP = 0,
                lastActivity = DateTime.UtcNow
            };

            playerRanks[playerId] = rankData;

            Debug.Log($"[PrestigeRankSystem] Initialized rank for player {playerId}");
        }

        public void AddXP(ulong playerId, int amount, string source = "Unknown")
        {
            if (!playerRanks.ContainsKey(playerId))
            {
                InitializePlayerRank(playerId);
            }

            var rankData = playerRanks[playerId];

            // Apply prestige XP multiplier
            float multiplier = GetPrestigeXPMultiplier(playerId);
            int adjustedAmount = Mathf.RoundToInt(amount * multiplier);

            rankData.currentXP += adjustedAmount;
            rankData.totalXP += adjustedAmount;
            rankData.lastActivity = DateTime.UtcNow;

            // Check for rank up
            CheckRankProgress(playerId);

            SavePlayerRankData(playerId);

            Debug.Log($"[PrestigeRankSystem] Player {playerId} gained {adjustedAmount} XP from {source}");
        }

        private void CheckRankProgress(ulong playerId)
        {
            if (!playerRanks.ContainsKey(playerId)) return;

            var rankData = playerRanks[playerId];

            while (rankData.currentRank < maxRank)
            {
                int xpRequired = GetXPRequiredForRank(rankData.currentRank + 1);

                if (rankData.currentXP >= xpRequired)
                {
                    rankData.currentXP -= xpRequired;
                    int oldRank = rankData.currentRank;
                    rankData.currentRank++;

                    OnRankUp?.Invoke(playerId, oldRank, rankData.currentRank);

                    // Award rank up rewards
                    AwardRankRewards(playerId, rankData.currentRank);

                    Debug.Log($"[PrestigeRankSystem] Player {playerId} ranked up to {rankData.currentRank}");
                }
                else
                {
                    break;
                }
            }
        }

        private int GetXPRequiredForRank(int rank)
        {
            // Exponential XP curve
            return Mathf.RoundToInt(1000f * Mathf.Pow(1.1f, rank - 1));
        }

        private void AwardRankRewards(ulong playerId, int rank)
        {
            var rankTier = GetRankTier(rank);
            if (rankTier == null) return;

            // Award soft currency
            if (rankTier.currencyReward > 0 && Economy.EconomyManager.Instance != null)
            {
                Economy.EconomyManager.Instance.EarnSoftCurrency(rankTier.currencyReward, $"Rank {rank} Reward");
            }

            // Award items
            if (rankTier.itemRewards != null && Gameplay.InventoryManager.Instance != null)
            {
                foreach (var itemReward in rankTier.itemRewards)
                {
                    var itemData = new Gameplay.LootItemData
                    {
                        itemId = itemReward.itemId,
                        itemName = itemReward.itemName
                    };

                    Gameplay.InventoryManager.Instance.AddItem(playerId, itemData, itemReward.quantity);
                }
            }

            // Unlock cosmetics
            if (rankTier.cosmeticUnlocks != null)
            {
                foreach (var cosmetic in rankTier.cosmeticUnlocks)
                {
                    // Unlock cosmetic for player
                    // This would integrate with cosmetic system
                }
            }
        }

        private RankData GetRankTier(int rank)
        {
            return rankTiers.FirstOrDefault(r => rank >= r.minRank && rank <= r.maxRank);
        }

        #endregion

        #region Prestige System

        public bool CanPrestige(ulong playerId)
        {
            if (!playerRanks.ContainsKey(playerId)) return false;
            if (!playerPrestige.ContainsKey(playerId))
            {
                InitializePlayerPrestige(playerId);
            }

            var rankData = playerRanks[playerId];
            var prestigeData = playerPrestige[playerId];

            // Check rank requirement
            if (rankData.currentRank < levelRequiredForPrestige) return false;

            // Check max prestige
            if (prestigeData.prestigeLevel >= maxPrestigeLevel) return false;

            return true;
        }

        public bool Prestige(ulong playerId)
        {
            if (!CanPrestige(playerId)) return false;

            var rankData = playerRanks[playerId];
            var prestigeData = playerPrestige[playerId];

            int oldPrestige = prestigeData.prestigeLevel;

            // Reset rank and XP
            rankData.currentRank = 1;
            rankData.currentXP = 0;
            // Keep totalXP for lifetime stats

            // Increase prestige
            prestigeData.prestigeLevel++;
            prestigeData.totalPrestiges++;
            prestigeData.lastPrestigeTime = DateTime.UtcNow;

            // Award prestige rewards
            AwardPrestigeRewards(playerId, prestigeData.prestigeLevel);

            OnPrestige?.Invoke(playerId, prestigeData.prestigeLevel);

            SavePlayerRankData(playerId);
            SavePlayerPrestigeData(playerId);

            Debug.Log($"[PrestigeRankSystem] Player {playerId} prestiged to level {prestigeData.prestigeLevel}");

            return true;
        }

        private void InitializePlayerPrestige(ulong playerId)
        {
            if (playerPrestige.ContainsKey(playerId)) return;

            var prestigeData = new PrestigeData
            {
                playerId = playerId,
                prestigeLevel = 0,
                totalPrestiges = 0,
                lastPrestigeTime = DateTime.MinValue
            };

            playerPrestige[playerId] = prestigeData;
        }

        private void AwardPrestigeRewards(ulong playerId, int prestigeLevel)
        {
            var reward = prestigeRewards.FirstOrDefault(r => r.prestigeLevel == prestigeLevel);
            if (reward == null) return;

            // Award hard currency
            if (reward.hardCurrencyReward > 0 && Economy.EconomyManager.Instance != null)
            {
                Economy.EconomyManager.Instance.AddHardCurrency(reward.hardCurrencyReward, $"Prestige {prestigeLevel}");
            }

            // Award permanent bonuses
            // These would be applied globally to the player

            Debug.Log($"[PrestigeRankSystem] Awarded prestige {prestigeLevel} rewards to player {playerId}");
        }

        private float GetPrestigeXPMultiplier(ulong playerId)
        {
            if (!playerPrestige.ContainsKey(playerId)) return 1f;

            var prestigeData = playerPrestige[playerId];
            var reward = prestigeRewards.FirstOrDefault(r => r.prestigeLevel == prestigeData.prestigeLevel);

            return reward != null ? reward.xpMultiplier : 1f;
        }

        #endregion

        #region Competitive Ranking

        public void InitializeCompetitiveRank(ulong playerId)
        {
            if (playerCompetitiveRanks.ContainsKey(playerId)) return;

            var compRank = new CompetitiveRank
            {
                playerId = playerId,
                rankTier = CompetitiveRankTier.Unranked,
                rankPoints = 0,
                wins = 0,
                losses = 0,
                placementMatchesPlayed = 0,
                highestRankTier = CompetitiveRankTier.Unranked,
                seasonId = currentSeason != null ? currentSeason.seasonId : 0,
                lastMatchTime = DateTime.UtcNow
            };

            playerCompetitiveRanks[playerId] = compRank;
        }

        public void RecordMatchResult(ulong playerId, bool won, MatchPerformance performance)
        {
            if (!playerCompetitiveRanks.ContainsKey(playerId))
            {
                InitializeCompetitiveRank(playerId);
            }

            var compRank = playerCompetitiveRanks[playerId];

            // Update match count
            if (won)
            {
                compRank.wins++;
            }
            else
            {
                compRank.losses++;
            }

            compRank.lastMatchTime = DateTime.UtcNow;

            // Placement matches
            if (compRank.placementMatchesPlayed < placementMatchesRequired)
            {
                compRank.placementMatchesPlayed++;

                // After placement matches, assign initial rank
                if (compRank.placementMatchesPlayed >= placementMatchesRequired)
                {
                    AssignInitialRank(playerId, performance);
                }
            }
            else
            {
                // Calculate rank points change
                float pointsChange = CalculateRankPointsChange(won, performance);
                AddRankPoints(playerId, pointsChange);
            }

            SaveCompetitiveRankData(playerId);

            Debug.Log($"[PrestigeRankSystem] Recorded match result for player {playerId}: {(won ? "Win" : "Loss")}");
        }

        private void AssignInitialRank(ulong playerId, MatchPerformance performance)
        {
            if (!playerCompetitiveRanks.ContainsKey(playerId)) return;

            var compRank = playerCompetitiveRanks[playerId];

            // Calculate initial rank based on placement performance
            float winRate = (float)compRank.wins / placementMatchesRequired;

            CompetitiveRankTier initialRank;

            if (winRate >= 0.8f)
                initialRank = CompetitiveRankTier.Platinum;
            else if (winRate >= 0.6f)
                initialRank = CompetitiveRankTier.Gold;
            else if (winRate >= 0.4f)
                initialRank = CompetitiveRankTier.Silver;
            else
                initialRank = CompetitiveRankTier.Bronze;

            compRank.rankTier = initialRank;
            compRank.rankPoints = GetMinPointsForRank(initialRank);
            compRank.highestRankTier = initialRank;

            OnCompetitiveRankChanged?.Invoke(playerId, CompetitiveRankTier.Unranked, initialRank);

            Debug.Log($"[PrestigeRankSystem] Assigned initial rank {initialRank} to player {playerId}");
        }

        private float CalculateRankPointsChange(bool won, MatchPerformance performance)
        {
            float basePoints = won ? rankPointsPerWin : rankPointsPerLoss;

            // Performance modifiers
            float performanceMultiplier = 1f;

            if (performance != null)
            {
                // KDA contribution
                float kda = performance.assists > 0
                    ? (performance.kills + performance.assists * 0.5f) / Mathf.Max(1, performance.deaths)
                    : (float)performance.kills / Mathf.Max(1, performance.deaths);

                if (kda >= 3f)
                    performanceMultiplier += 0.3f;
                else if (kda >= 2f)
                    performanceMultiplier += 0.2f;
                else if (kda >= 1.5f)
                    performanceMultiplier += 0.1f;
                else if (kda < 0.5f)
                    performanceMultiplier -= 0.2f;

                // Objective contribution
                if (performance.objectivesCompleted > 5)
                    performanceMultiplier += 0.1f;

                // MVP bonus
                if (performance.wasMVP)
                    performanceMultiplier += 0.25f;
            }

            return basePoints * performanceMultiplier;
        }

        public void AddRankPoints(ulong playerId, float points)
        {
            if (!playerCompetitiveRanks.ContainsKey(playerId)) return;

            var compRank = playerCompetitiveRanks[playerId];
            CompetitiveRankTier oldTier = compRank.rankTier;

            compRank.rankPoints += points;

            // Check for rank changes
            CheckCompetitiveRankChange(playerId);

            OnRankPointsChanged?.Invoke(playerId, Mathf.RoundToInt(compRank.rankPoints));

            if (compRank.rankTier != oldTier)
            {
                OnCompetitiveRankChanged?.Invoke(playerId, oldTier, compRank.rankTier);
            }
        }

        private void CheckCompetitiveRankChange(ulong playerId)
        {
            if (!playerCompetitiveRanks.ContainsKey(playerId)) return;

            var compRank = playerCompetitiveRanks[playerId];

            // Find appropriate rank tier
            foreach (var rankData in competitiveRanks.OrderBy(r => r.minPoints))
            {
                if (compRank.rankPoints >= rankData.minPoints)
                {
                    compRank.rankTier = rankData.rankTier;
                }
            }

            // Update highest rank
            if ((int)compRank.rankTier > (int)compRank.highestRankTier)
            {
                compRank.highestRankTier = compRank.rankTier;
            }

            // Demotion protection
            if (compRank.rankPoints < GetMinPointsForRank(compRank.rankTier))
            {
                // Apply demotion protection (can only demote after multiple losses)
                // This would check a demotion protection counter
            }
        }

        private int GetMinPointsForRank(CompetitiveRankTier tier)
        {
            var rankData = competitiveRanks.FirstOrDefault(r => r.rankTier == tier);
            return rankData != null ? rankData.minPoints : 0;
        }

        #endregion

        #region Rank Decay

        private void UpdateRankDecay()
        {
            foreach (var kvp in playerCompetitiveRanks)
            {
                var playerId = kvp.Key;
                var compRank = kvp.Value;

                // Calculate days inactive
                TimeSpan inactivity = DateTime.UtcNow - compRank.lastMatchTime;
                int daysInactive = (int)inactivity.TotalDays;

                // Apply decay after 7 days
                if (daysInactive > 7)
                {
                    float decayAmount = rankPointDecayRate * (daysInactive - 7);
                    float pointsToDecay = compRank.rankPoints * decayAmount;

                    if (pointsToDecay > 0)
                    {
                        AddRankPoints(playerId, -pointsToDecay);
                        compRank.lastMatchTime = compRank.lastMatchTime.AddDays(1); // Prevent continuous decay

                        Debug.Log($"[PrestigeRankSystem] Applied {pointsToDecay} rank decay to player {playerId}");
                    }
                }
            }
        }

        #endregion

        #region Seasons

        private void LoadSeasonData()
        {
            if (currentSeason == null)
            {
                // Create new season
                currentSeason = new SeasonData
                {
                    seasonId = 1,
                    seasonName = "Season 1",
                    startTime = DateTime.UtcNow,
                    endTime = DateTime.UtcNow.AddDays(seasonDurationDays)
                };

                Debug.Log($"[PrestigeRankSystem] Started new season: {currentSeason.seasonName}");
            }
        }

        private void UpdateSeasonTimer()
        {
            if (!enableSeasons || currentSeason == null) return;

            if (DateTime.UtcNow >= currentSeason.endTime)
            {
                EndSeason();
            }
        }

        private void EndSeason()
        {
            Debug.Log($"[PrestigeRankSystem] Season {currentSeason.seasonId} ended");

            // Award seasonal rewards
            AwardSeasonalRewards();

            // Reset competitive ranks
            ResetCompetitiveRanks();

            // Start new season
            currentSeason = new SeasonData
            {
                seasonId = currentSeason.seasonId + 1,
                seasonName = $"Season {currentSeason.seasonId + 1}",
                startTime = DateTime.UtcNow,
                endTime = DateTime.UtcNow.AddDays(seasonDurationDays)
            };

            OnSeasonEnded?.Invoke();

            Debug.Log($"[PrestigeRankSystem] Started new season: {currentSeason.seasonName}");
        }

        private void AwardSeasonalRewards()
        {
            // Award rewards to top players on leaderboard
            for (int i = 0; i < Mathf.Min(100, seasonLeaderboard.Count); i++)
            {
                var entry = seasonLeaderboard[i];
                int reward = 0;

                if (i == 0) reward = 10000; // 1st place
                else if (i < 3) reward = 5000; // Top 3
                else if (i < 10) reward = 2500; // Top 10
                else if (i < 50) reward = 1000; // Top 50
                else reward = 500; // Top 100

                if (Economy.EconomyManager.Instance != null)
                {
                    Economy.EconomyManager.Instance.AddHardCurrency(reward, $"Season {currentSeason.seasonId} Reward (Rank {i + 1})");
                }
            }
        }

        private void ResetCompetitiveRanks()
        {
            foreach (var kvp in playerCompetitiveRanks)
            {
                var compRank = kvp.Value;

                // Soft reset - reduce rank points
                compRank.rankPoints = Mathf.RoundToInt(compRank.rankPoints * 0.5f);
                compRank.placementMatchesPlayed = 0;
                compRank.wins = 0;
                compRank.losses = 0;
                compRank.seasonId = currentSeason.seasonId;

                SaveCompetitiveRankData(kvp.Key);
            }

            seasonLeaderboard.Clear();
        }

        #endregion

        #region Leaderboards

        private void UpdateLeaderboards()
        {
            if (Time.time - lastLeaderboardUpdate < leaderboardUpdateInterval) return;

            UpdateGlobalLeaderboard();
            UpdateSeasonLeaderboard();

            lastLeaderboardUpdate = Time.time;
        }

        private void UpdateGlobalLeaderboard()
        {
            globalLeaderboard = playerRanks.Values
                .OrderByDescending(p => p.totalXP)
                .Take(leaderboardSize)
                .Select((p, index) => new LeaderboardEntry
                {
                    rank = index + 1,
                    playerId = p.playerId,
                    playerName = $"Player {p.playerId}", // Get from player name system
                    score = p.totalXP,
                    rankLevel = p.currentRank
                })
                .ToList();
        }

        private void UpdateSeasonLeaderboard()
        {
            seasonLeaderboard = playerCompetitiveRanks.Values
                .Where(p => p.seasonId == currentSeason.seasonId)
                .OrderByDescending(p => p.rankPoints)
                .Take(leaderboardSize)
                .Select((p, index) => new LeaderboardEntry
                {
                    rank = index + 1,
                    playerId = p.playerId,
                    playerName = $"Player {p.playerId}",
                    score = Mathf.RoundToInt(p.rankPoints),
                    rankTier = p.rankTier
                })
                .ToList();
        }

        #endregion

        #region Persistence

        private void LoadAllPlayerData()
        {
            // In production, would load from database
            Debug.Log("[PrestigeRankSystem] Ready to load player data");
        }

        public void LoadPlayerData(ulong playerId)
        {
            LoadPlayerRankData(playerId);
            LoadPlayerPrestigeData(playerId);
            LoadCompetitiveRankData(playerId);
        }

        private void LoadPlayerRankData(ulong playerId)
        {
            if (Core.SaveSystem.Instance == null) return;

            string savedData = Core.SaveSystem.Instance.LoadData($"rank_{playerId}");
            if (!string.IsNullOrEmpty(savedData))
            {
                try
                {
                    var rankSave = JsonUtility.FromJson<PlayerRankSaveData>(savedData);

                    var rankData = new PlayerRankData
                    {
                        playerId = playerId,
                        currentRank = rankSave.currentRank,
                        currentXP = rankSave.currentXP,
                        totalXP = rankSave.totalXP,
                        lastActivity = DateTime.Parse(rankSave.lastActivity)
                    };

                    playerRanks[playerId] = rankData;
                }
                catch
                {
                    InitializePlayerRank(playerId);
                }
            }
            else
            {
                InitializePlayerRank(playerId);
            }
        }

        private void SavePlayerRankData(ulong playerId)
        {
            if (Core.SaveSystem.Instance == null) return;
            if (!playerRanks.ContainsKey(playerId)) return;

            var rankData = playerRanks[playerId];

            var rankSave = new PlayerRankSaveData
            {
                currentRank = rankData.currentRank,
                currentXP = rankData.currentXP,
                totalXP = rankData.totalXP,
                lastActivity = rankData.lastActivity.ToString()
            };

            string json = JsonUtility.ToJson(rankSave);
            Core.SaveSystem.Instance.SaveData($"rank_{playerId}", json);
        }

        private void LoadPlayerPrestigeData(ulong playerId)
        {
            if (Core.SaveSystem.Instance == null) return;

            string savedData = Core.SaveSystem.Instance.LoadData($"prestige_{playerId}");
            if (!string.IsNullOrEmpty(savedData))
            {
                try
                {
                    var prestigeSave = JsonUtility.FromJson<PrestigeSaveData>(savedData);

                    var prestigeData = new PrestigeData
                    {
                        playerId = playerId,
                        prestigeLevel = prestigeSave.prestigeLevel,
                        totalPrestiges = prestigeSave.totalPrestiges,
                        lastPrestigeTime = DateTime.Parse(prestigeSave.lastPrestigeTime)
                    };

                    playerPrestige[playerId] = prestigeData;
                }
                catch
                {
                    InitializePlayerPrestige(playerId);
                }
            }
            else
            {
                InitializePlayerPrestige(playerId);
            }
        }

        private void SavePlayerPrestigeData(ulong playerId)
        {
            if (Core.SaveSystem.Instance == null) return;
            if (!playerPrestige.ContainsKey(playerId)) return;

            var prestigeData = playerPrestige[playerId];

            var prestigeSave = new PrestigeSaveData
            {
                prestigeLevel = prestigeData.prestigeLevel,
                totalPrestiges = prestigeData.totalPrestiges,
                lastPrestigeTime = prestigeData.lastPrestigeTime.ToString()
            };

            string json = JsonUtility.ToJson(prestigeSave);
            Core.SaveSystem.Instance.SaveData($"prestige_{playerId}", json);
        }

        private void LoadCompetitiveRankData(ulong playerId)
        {
            if (Core.SaveSystem.Instance == null) return;

            string savedData = Core.SaveSystem.Instance.LoadData($"competitive_{playerId}");
            if (!string.IsNullOrEmpty(savedData))
            {
                try
                {
                    var compSave = JsonUtility.FromJson<CompetitiveRankSaveData>(savedData);

                    var compRank = new CompetitiveRank
                    {
                        playerId = playerId,
                        rankTier = compSave.rankTier,
                        rankPoints = compSave.rankPoints,
                        wins = compSave.wins,
                        losses = compSave.losses,
                        placementMatchesPlayed = compSave.placementMatchesPlayed,
                        highestRankTier = compSave.highestRankTier,
                        seasonId = compSave.seasonId,
                        lastMatchTime = DateTime.Parse(compSave.lastMatchTime)
                    };

                    playerCompetitiveRanks[playerId] = compRank;
                }
                catch
                {
                    InitializeCompetitiveRank(playerId);
                }
            }
            else
            {
                InitializeCompetitiveRank(playerId);
            }
        }

        private void SaveCompetitiveRankData(ulong playerId)
        {
            if (Core.SaveSystem.Instance == null) return;
            if (!playerCompetitiveRanks.ContainsKey(playerId)) return;

            var compRank = playerCompetitiveRanks[playerId];

            var compSave = new CompetitiveRankSaveData
            {
                rankTier = compRank.rankTier,
                rankPoints = compRank.rankPoints,
                wins = compRank.wins,
                losses = compRank.losses,
                placementMatchesPlayed = compRank.placementMatchesPlayed,
                highestRankTier = compRank.highestRankTier,
                seasonId = compRank.seasonId,
                lastMatchTime = compRank.lastMatchTime.ToString()
            };

            string json = JsonUtility.ToJson(compSave);
            Core.SaveSystem.Instance.SaveData($"competitive_{playerId}", json);
        }

        #endregion

        #region Public Getters

        public int GetPlayerRank(ulong playerId)
        {
            return playerRanks.ContainsKey(playerId) ? playerRanks[playerId].currentRank : 1;
        }

        public int GetPlayerPrestige(ulong playerId)
        {
            return playerPrestige.ContainsKey(playerId) ? playerPrestige[playerId].prestigeLevel : 0;
        }

        public CompetitiveRankTier GetCompetitiveRank(ulong playerId)
        {
            return playerCompetitiveRanks.ContainsKey(playerId)
                ? playerCompetitiveRanks[playerId].rankTier
                : CompetitiveRankTier.Unranked;
        }

        public int GetRankPoints(ulong playerId)
        {
            return playerCompetitiveRanks.ContainsKey(playerId)
                ? Mathf.RoundToInt(playerCompetitiveRanks[playerId].rankPoints)
                : 0;
        }

        public float GetRankProgress(ulong playerId)
        {
            if (!playerRanks.ContainsKey(playerId)) return 0f;

            var rankData = playerRanks[playerId];
            int xpRequired = GetXPRequiredForRank(rankData.currentRank + 1);

            return (float)rankData.currentXP / xpRequired;
        }

        public List<LeaderboardEntry> GetGlobalLeaderboard() => new List<LeaderboardEntry>(globalLeaderboard);

        public List<LeaderboardEntry> GetSeasonLeaderboard() => new List<LeaderboardEntry>(seasonLeaderboard);

        public int GetLeaderboardPosition(ulong playerId, bool seasonal = false)
        {
            var leaderboard = seasonal ? seasonLeaderboard : globalLeaderboard;
            var entry = leaderboard.FirstOrDefault(e => e.playerId == playerId);

            return entry != null ? entry.rank : -1;
        }

        public SeasonData GetCurrentSeason() => currentSeason;

        public TimeSpan GetSeasonTimeRemaining()
        {
            if (currentSeason == null) return TimeSpan.Zero;

            TimeSpan remaining = currentSeason.endTime - DateTime.UtcNow;
            return remaining.TotalSeconds > 0 ? remaining : TimeSpan.Zero;
        }

        #endregion
    }

    #region Data Classes

    public class PlayerRankData
    {
        public ulong playerId;
        public int currentRank;
        public int currentXP;
        public int totalXP;
        public DateTime lastActivity;
    }

    public class PrestigeData
    {
        public ulong playerId;
        public int prestigeLevel;
        public int totalPrestiges;
        public DateTime lastPrestigeTime;
    }

    public class CompetitiveRank
    {
        public ulong playerId;
        public CompetitiveRankTier rankTier;
        public float rankPoints;
        public int wins;
        public int losses;
        public int placementMatchesPlayed;
        public CompetitiveRankTier highestRankTier;
        public int seasonId;
        public DateTime lastMatchTime;
    }

    [System.Serializable]
    public class RankData
    {
        public string rankName;
        public int minRank;
        public int maxRank;
        public int currencyReward;
        public ItemReward[] itemRewards;
        public string[] cosmeticUnlocks;
        public Sprite rankBadge;
    }

    [System.Serializable]
    public class PrestigeReward
    {
        public int prestigeLevel;
        public int hardCurrencyReward;
        public float xpMultiplier = 1.1f; // 10% XP bonus
        public string[] permanentUnlocks;
        public Sprite prestigeBadge;
    }

    [System.Serializable]
    public class CompetitiveRankData
    {
        public CompetitiveRankTier rankTier;
        public int minPoints;
        public int maxPoints;
        public Color rankColor;
        public Sprite rankIcon;
    }

    [System.Serializable]
    public class ItemReward
    {
        public string itemId;
        public string itemName;
        public int quantity = 1;
    }

    [System.Serializable]
    public class SeasonData
    {
        public int seasonId;
        public string seasonName;
        public DateTime startTime;
        public DateTime endTime;
    }

    public class MatchPerformance
    {
        public int kills;
        public int deaths;
        public int assists;
        public int objectivesCompleted;
        public int damageDealt;
        public bool wasMVP;
    }

    public class LeaderboardEntry
    {
        public int rank;
        public ulong playerId;
        public string playerName;
        public int score;
        public int rankLevel;
        public CompetitiveRankTier rankTier;
    }

    [System.Serializable]
    public class PlayerRankSaveData
    {
        public int currentRank;
        public int currentXP;
        public int totalXP;
        public string lastActivity;
    }

    [System.Serializable]
    public class PrestigeSaveData
    {
        public int prestigeLevel;
        public int totalPrestiges;
        public string lastPrestigeTime;
    }

    [System.Serializable]
    public class CompetitiveRankSaveData
    {
        public CompetitiveRankTier rankTier;
        public float rankPoints;
        public int wins;
        public int losses;
        public int placementMatchesPlayed;
        public CompetitiveRankTier highestRankTier;
        public int seasonId;
        public string lastMatchTime;
    }

    public enum CompetitiveRankTier
    {
        Unranked,
        Bronze,
        Silver,
        Gold,
        Platinum,
        Diamond,
        Master,
        Grandmaster,
        Champion
    }

    #endregion
}
