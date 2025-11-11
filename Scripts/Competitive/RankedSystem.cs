using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Competitive
{
    /// <summary>
    /// Comprehensive ranked matchmaking system with skill-based rating (MMR),
    /// competitive tiers, placement matches, and seasonal rankings.
    /// </summary>
    public class RankedSystem : NetworkBehaviour
    {
        public static RankedSystem Instance { get; private set; }

        [Header("Ranked Configuration")]
        [SerializeField] private int placementMatchCount = 10;
        [SerializeField] private float mmrGainBase = 25f;
        [SerializeField] private float mmrLossBase = 20f;
        [SerializeField] private bool enableRankDecay = true;
        [SerializeField] private int decayDaysInactive = 7;
        [SerializeField] private float decayMMRPerDay = 10f;

        [Header("Season Configuration")]
        [SerializeField] private float seasonDurationDays = 90f;

        private Dictionary<ulong, RankedProfile> playerRankedProfiles = new Dictionary<ulong, RankedProfile>();
        private RankedSeason currentSeason;

        public event Action<ulong, RankTier, int> OnRankChanged;
        public event Action<ulong, float> OnMMRChanged;
        public event Action<ulong, int> OnPlacementMatchCompleted;
        public event Action<RankedSeason> OnSeasonStarted;
        public event Action<RankedSeason> OnSeasonEnded;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            InitializeSeason();
        }

        private void InitializeSeason()
        {
            currentSeason = new RankedSeason
            {
                seasonNumber = 1,
                startDate = DateTime.UtcNow,
                endDate = DateTime.UtcNow.AddDays(seasonDurationDays),
                isActive = true
            };
        }

        /// <summary>
        /// Initialize ranked profile for player
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void InitializeRankedProfileServerRpc(ulong playerId, ServerRpcParams rpcParams = default)
        {
            if (playerRankedProfiles.ContainsKey(playerId)) return;

            playerRankedProfiles[playerId] = new RankedProfile
            {
                playerId = playerId,
                mmr = 1000f, // Starting MMR
                currentTier = RankTier.Unranked,
                divisionNumber = 0,
                placementMatchesPlayed = 0,
                rankedWins = 0,
                rankedLosses = 0,
                winStreak = 0,
                lastMatchDate = DateTime.UtcNow,
                seasonalStats = new SeasonalStats()
            };
        }

        /// <summary>
        /// Record ranked match result
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void RecordRankedMatchServerRpc(ulong playerId, bool won, float performanceScore, ServerRpcParams rpcParams = default)
        {
            if (!playerRankedProfiles.TryGetValue(playerId, out var profile)) return;

            profile.lastMatchDate = DateTime.UtcNow;

            // Check if still in placements
            if (profile.placementMatchesPlayed < placementMatchCount)
            {
                profile.placementMatchesPlayed++;
                if (won) profile.placementMatchesWon++;

                OnPlacementMatchCompleted?.Invoke(playerId, profile.placementMatchesPlayed);

                // Complete placements
                if (profile.placementMatchesPlayed == placementMatchCount)
                {
                    CompletePlacementMatches(playerId, profile);
                }
                return;
            }

            // Calculate MMR change
            float mmrChange = CalculateMMRChange(profile, won, performanceScore);
            float oldMMR = profile.mmr;
            profile.mmr += mmrChange;
            profile.mmr = Mathf.Clamp(profile.mmr, 0f, 5000f);

            // Update stats
            if (won)
            {
                profile.rankedWins++;
                profile.winStreak++;
                profile.seasonalStats.wins++;
            }
            else
            {
                profile.rankedLosses++;
                profile.winStreak = 0;
                profile.seasonalStats.losses++;
            }

            profile.seasonalStats.totalMatches++;

            // Update rank
            RankTier oldTier = profile.currentTier;
            int oldDivision = profile.divisionNumber;
            UpdateRankFromMMR(profile);

            // Notify changes
            OnMMRChanged?.Invoke(playerId, profile.mmr);

            if (oldTier != profile.currentTier || oldDivision != profile.divisionNumber)
            {
                OnRankChanged?.Invoke(playerId, profile.currentTier, profile.divisionNumber);
                NotifyRankChangeClientRpc(playerId, profile.currentTier, profile.divisionNumber,
                    profile.currentTier > oldTier || (profile.currentTier == oldTier && profile.divisionNumber > oldDivision));
            }

            NotifyMMRChangeClientRpc(playerId, mmrChange, profile.mmr);
        }

        private void CompletePlacementMatches(ulong playerId, RankedProfile profile)
        {
            // Calculate initial MMR based on placement performance
            float winRate = profile.placementMatchesWon / (float)placementMatchCount;

            // Base MMR on performance (500-1500)
            profile.mmr = 500f + (winRate * 1000f);

            UpdateRankFromMMR(profile);

            NotifyPlacementCompleteClientRpc(playerId, profile.currentTier, profile.divisionNumber, profile.mmr);
        }

        private float CalculateMMRChange(RankedProfile profile, bool won, float performanceScore)
        {
            float baseChange = won ? mmrGainBase : -mmrLossBase;

            // Performance multiplier (0.5x - 2x)
            float performanceMultiplier = Mathf.Clamp(performanceScore / 100f, 0.5f, 2f);

            // Win streak bonus
            float streakBonus = won ? Mathf.Min(profile.winStreak * 2f, 20f) : 0f;

            float totalChange = (baseChange * performanceMultiplier) + streakBonus;

            return totalChange;
        }

        private void UpdateRankFromMMR(RankedProfile profile)
        {
            // Rank thresholds
            if (profile.mmr < 500f)
            {
                profile.currentTier = RankTier.Bronze;
                profile.divisionNumber = 1;
            }
            else if (profile.mmr < 750f)
            {
                profile.currentTier = RankTier.Bronze;
                profile.divisionNumber = 2;
            }
            else if (profile.mmr < 1000f)
            {
                profile.currentTier = RankTier.Bronze;
                profile.divisionNumber = 3;
            }
            else if (profile.mmr < 1250f)
            {
                profile.currentTier = RankTier.Silver;
                profile.divisionNumber = 1;
            }
            else if (profile.mmr < 1500f)
            {
                profile.currentTier = RankTier.Silver;
                profile.divisionNumber = 2;
            }
            else if (profile.mmr < 1750f)
            {
                profile.currentTier = RankTier.Silver;
                profile.divisionNumber = 3;
            }
            else if (profile.mmr < 2000f)
            {
                profile.currentTier = RankTier.Gold;
                profile.divisionNumber = 1;
            }
            else if (profile.mmr < 2250f)
            {
                profile.currentTier = RankTier.Gold;
                profile.divisionNumber = 2;
            }
            else if (profile.mmr < 2500f)
            {
                profile.currentTier = RankTier.Gold;
                profile.divisionNumber = 3;
            }
            else if (profile.mmr < 2750f)
            {
                profile.currentTier = RankTier.Platinum;
                profile.divisionNumber = 1;
            }
            else if (profile.mmr < 3000f)
            {
                profile.currentTier = RankTier.Platinum;
                profile.divisionNumber = 2;
            }
            else if (profile.mmr < 3250f)
            {
                profile.currentTier = RankTier.Platinum;
                profile.divisionNumber = 3;
            }
            else if (profile.mmr < 3500f)
            {
                profile.currentTier = RankTier.Diamond;
                profile.divisionNumber = 1;
            }
            else if (profile.mmr < 3750f)
            {
                profile.currentTier = RankTier.Diamond;
                profile.divisionNumber = 2;
            }
            else if (profile.mmr < 4000f)
            {
                profile.currentTier = RankTier.Diamond;
                profile.divisionNumber = 3;
            }
            else if (profile.mmr < 4500f)
            {
                profile.currentTier = RankTier.Master;
                profile.divisionNumber = 1;
            }
            else
            {
                profile.currentTier = RankTier.Grandmaster;
                profile.divisionNumber = 1;
            }

            // Track peak
            if (profile.mmr > profile.seasonalStats.peakMMR)
            {
                profile.seasonalStats.peakMMR = profile.mmr;
                profile.seasonalStats.peakRank = profile.currentTier;
            }
        }

        /// <summary>
        /// Apply rank decay for inactive players
        /// </summary>
        private void ApplyRankDecay()
        {
            if (!enableRankDecay) return;

            foreach (var kvp in playerRankedProfiles)
            {
                var profile = kvp.Value;

                // Only apply to ranked players (not in placements)
                if (profile.placementMatchesPlayed < placementMatchCount) continue;
                if (profile.currentTier < RankTier.Platinum) continue; // Only decay Platinum+

                TimeSpan inactiveDuration = DateTime.UtcNow - profile.lastMatchDate;
                int daysInactive = (int)inactiveDuration.TotalDays;

                if (daysInactive >= decayDaysInactive)
                {
                    int decayDays = daysInactive - decayDaysInactive + 1;
                    float mmrLoss = decayDays * decayMMRPerDay;

                    profile.mmr = Mathf.Max(profile.mmr - mmrLoss, 2500f); // Can't decay below Platinum 3
                    UpdateRankFromMMR(profile);
                }
            }
        }

        [ClientRpc]
        private void NotifyRankChangeClientRpc(ulong playerId, RankTier newTier, int division, bool promoted)
        {
            if (NetworkManager.Singleton.LocalClientId != playerId) return;

            string message = promoted ?
                $"<color=lime>PROMOTED to {newTier} {division}!</color>" :
                $"<color=red>DEMOTED to {newTier} {division}</color>";

            Debug.Log(message);
        }

        [ClientRpc]
        private void NotifyMMRChangeClientRpc(ulong playerId, float change, float newMMR)
        {
            if (NetworkManager.Singleton.LocalClientId != playerId) return;

            string color = change > 0 ? "green" : "red";
            string sign = change > 0 ? "+" : "";
            Debug.Log($"MMR: <color={color}>{sign}{change:F0}</color> → {newMMR:F0}");
        }

        [ClientRpc]
        private void NotifyPlacementCompleteClientRpc(ulong playerId, RankTier tier, int division, float mmr)
        {
            if (NetworkManager.Singleton.LocalClientId != playerId) return;
            Debug.Log($"<color=gold>PLACEMENT COMPLETE!</color> Ranked: {tier} {division} ({mmr:F0} MMR)");
        }

        public RankedProfile GetRankedProfile(ulong playerId)
        {
            return playerRankedProfiles.TryGetValue(playerId, out var profile) ? profile : null;
        }

        public List<RankedProfile> GetLeaderboard(int count = 100)
        {
            return playerRankedProfiles.Values
                .OrderByDescending(p => p.mmr)
                .Take(count)
                .ToList();
        }

        public float GetWinRate(ulong playerId)
        {
            if (!playerRankedProfiles.TryGetValue(playerId, out var profile)) return 0f;
            int totalGames = profile.rankedWins + profile.rankedLosses;
            return totalGames > 0 ? (profile.rankedWins / (float)totalGames) * 100f : 0f;
        }

        [Serializable]
        public class RankedProfile
        {
            public ulong playerId;
            public float mmr;
            public RankTier currentTier;
            public int divisionNumber;
            public int placementMatchesPlayed;
            public int placementMatchesWon;
            public int rankedWins;
            public int rankedLosses;
            public int winStreak;
            public DateTime lastMatchDate;
            public SeasonalStats seasonalStats;
        }

        [Serializable]
        public class SeasonalStats
        {
            public int wins;
            public int losses;
            public int totalMatches;
            public float peakMMR;
            public RankTier peakRank;
            public int topPlacement; // For tournaments
        }

        [Serializable]
        public class RankedSeason
        {
            public int seasonNumber;
            public DateTime startDate;
            public DateTime endDate;
            public bool isActive;
        }

        public enum RankTier
        {
            Unranked = 0,
            Bronze = 1,
            Silver = 2,
            Gold = 3,
            Platinum = 4,
            Diamond = 5,
            Master = 6,
            Grandmaster = 7
        }
    }
}
