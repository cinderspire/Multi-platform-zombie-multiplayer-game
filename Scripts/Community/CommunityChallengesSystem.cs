using Unity.Netcode;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ZombieGame
{
    /// <summary>
    /// Community Challenges System - Global cooperative goals for all players
    /// Features: Worldwide challenges, community milestones, tier rewards
    /// Creates sense of unity - all players working toward common goals
    /// </summary>
    public class CommunityChallengesSystem : NetworkBehaviour
    {
        public static CommunityChallengesSystem Instance { get; private set; }

        [Header("Challenge Settings")]
        [SerializeField] private float challengeUpdateInterval = 60f; // Update progress every minute

        // Active Challenges
        private Dictionary<string, CommunityChallenge> activeChallenges = new Dictionary<string, CommunityChallenge>();

        // Challenge History
        private List<CompletedChallenge> challengeHistory = new List<CompletedChallenge>();

        // Network Variables
        private NetworkVariable<int> totalActivePlayers = new NetworkVariable<int>(0);

        // Player Contributions
        private Dictionary<ulong, PlayerContribution> playerContributions = new Dictionary<ulong, PlayerContribution>();

        // Events
        public event System.Action<string> OnChallengeStarted;
        public event System.Action<string, int> OnChallengeProgress; // challengeId, tier completed
        public event System.Action<string> OnChallengeCompleted;

        [System.Serializable]
        public class CommunityChallenge
        {
            public string challengeId;
            public string challengeName;
            public string description;
            public ChallengeObjective objective;
            public DateTime startDate;
            public DateTime endDate;
            public long currentProgress = 0;
            public List<ChallengeTier> tiers = new List<ChallengeTier>();
            public int currentTier = 0;
            public bool isCompleted = false;
            public ChallengeType type;
        }

        public enum ChallengeObjective
        {
            KillZombies,        // Kill X million zombies worldwide
            SurviveMinutes,     // Survive X total hours
            WinMatches,         // Win X matches
            CraftItems,         // Craft X items
            ReviveTeammates,    // Revive X players
            CompleteObjectives  // Complete X objectives
        }

        public enum ChallengeType
        {
            Daily,      // 24 hours
            Weekly,     // 7 days
            Monthly,    // 30 days
            Special     // Event-specific
        }

        [System.Serializable]
        public class ChallengeTier
        {
            public int tierNumber;
            public long targetValue;
            public List<TierReward> rewards = new List<TierReward>();
            public bool isUnlocked = false;
        }

        [System.Serializable]
        public class TierReward
        {
            public RewardType rewardType;
            public string rewardId;
            public int rewardQuantity;
        }

        public enum RewardType
        {
            Currency,
            XP,
            Weapon,
            Skin,
            Emote,
            Title,
            Badge
        }

        [System.Serializable]
        public class CompletedChallenge
        {
            public string challengeId;
            public string challengeName;
            public DateTime completionDate;
            public long finalProgress;
            public int participatingPlayers;
        }

        [System.Serializable]
        public class PlayerContribution
        {
            public Dictionary<string, long> challengeContributions = new Dictionary<string, long>();
            public int totalPoints = 0;
            public int challengesCompleted = 0;
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeChallenges();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                StartActiveChallenges();
            }
        }

        private void InitializeChallenges()
        {
            // DAILY CHALLENGE: Kill Zombies
            CreateChallenge(new CommunityChallenge
            {
                challengeId = "daily_kill_1m",
                challengeName = "Global Elimination Day",
                description = "Kill 1,000,000 zombies as a community in 24 hours!",
                objective = ChallengeObjective.KillZombies,
                startDate = DateTime.UtcNow,
                endDate = DateTime.UtcNow.AddHours(24),
                type = ChallengeType.Daily,
                tiers = new List<ChallengeTier>
                {
                    new ChallengeTier
                    {
                        tierNumber = 1,
                        targetValue = 250000,
                        rewards = new List<TierReward>
                        {
                            new TierReward { rewardType = RewardType.Currency, rewardId = "soft_currency", rewardQuantity = 500 },
                            new TierReward { rewardType = RewardType.XP, rewardId = "xp", rewardQuantity = 1000 }
                        }
                    },
                    new ChallengeTier
                    {
                        tierNumber = 2,
                        targetValue = 500000,
                        rewards = new List<TierReward>
                        {
                            new TierReward { rewardType = RewardType.Currency, rewardId = "soft_currency", rewardQuantity = 1000 },
                            new TierReward { rewardType = RewardType.Weapon, rewardId = "weapon_community_rifle", rewardQuantity = 1 }
                        }
                    },
                    new ChallengeTier
                    {
                        tierNumber = 3,
                        targetValue = 1000000,
                        rewards = new List<TierReward>
                        {
                            new TierReward { rewardType = RewardType.Currency, rewardId = "hard_currency", rewardQuantity = 100 },
                            new TierReward { rewardType = RewardType.Skin, rewardId = "skin_community_hero", rewardQuantity = 1 },
                            new TierReward { rewardType = RewardType.Title, rewardId = "title_zombie_slayer", rewardQuantity = 1 }
                        }
                    }
                }
            });

            // WEEKLY CHALLENGE: Survival Time
            CreateChallenge(new CommunityChallenge
            {
                challengeId = "weekly_survive_1000h",
                challengeName = "Week of Survival",
                description = "Survive 1,000 total hours as a community!",
                objective = ChallengeObjective.SurviveMinutes,
                startDate = DateTime.UtcNow,
                endDate = DateTime.UtcNow.AddDays(7),
                type = ChallengeType.Weekly,
                tiers = new List<ChallengeTier>
                {
                    new ChallengeTier
                    {
                        tierNumber = 1,
                        targetValue = 30000, // 500 hours (in minutes)
                        rewards = new List<TierReward>
                        {
                            new TierReward { rewardType = RewardType.XP, rewardId = "xp", rewardQuantity = 2000 }
                        }
                    },
                    new ChallengeTier
                    {
                        tierNumber = 2,
                        targetValue = 60000, // 1000 hours
                        rewards = new List<TierReward>
                        {
                            new TierReward { rewardType = RewardType.Badge, rewardId = "badge_survivor_week", rewardQuantity = 1 },
                            new TierReward { rewardType = RewardType.Currency, rewardId = "soft_currency", rewardQuantity = 2000 }
                        }
                    }
                }
            });

            // MONTHLY CHALLENGE: Win Matches
            CreateChallenge(new CommunityChallenge
            {
                challengeId = "monthly_wins_50k",
                challengeName = "Victory Month",
                description = "Win 50,000 matches together this month!",
                objective = ChallengeObjective.WinMatches,
                startDate = DateTime.UtcNow,
                endDate = DateTime.UtcNow.AddDays(30),
                type = ChallengeType.Monthly,
                tiers = new List<ChallengeTier>
                {
                    new ChallengeTier
                    {
                        tierNumber = 1,
                        targetValue = 10000,
                        rewards = new List<TierReward>
                        {
                            new TierReward { rewardType = RewardType.Currency, rewardId = "soft_currency", rewardQuantity = 1500 }
                        }
                    },
                    new ChallengeTier
                    {
                        tierNumber = 2,
                        targetValue = 25000,
                        rewards = new List<TierReward>
                        {
                            new TierReward { rewardType = RewardType.Emote, rewardId = "emote_victory_dance", rewardQuantity = 1 }
                        }
                    },
                    new ChallengeTier
                    {
                        tierNumber = 3,
                        targetValue = 50000,
                        rewards = new List<TierReward>
                        {
                            new TierReward { rewardType = RewardType.Weapon, rewardId = "weapon_legendary_community", rewardQuantity = 1 },
                            new TierReward { rewardType = RewardType.Title, rewardId = "title_champion_of_the_people", rewardQuantity = 1 },
                            new TierReward { rewardType = RewardType.Currency, rewardId = "hard_currency", rewardQuantity = 250 }
                        }
                    }
                }
            });

            // SPECIAL CHALLENGE: Crafting Event
            CreateChallenge(new CommunityChallenge
            {
                challengeId = "special_craft_100k",
                challengeName = "Global Crafting Event",
                description = "Craft 100,000 items together in 3 days!",
                objective = ChallengeObjective.CraftItems,
                startDate = DateTime.UtcNow,
                endDate = DateTime.UtcNow.AddDays(3),
                type = ChallengeType.Special,
                tiers = new List<ChallengeTier>
                {
                    new ChallengeTier
                    {
                        tierNumber = 1,
                        targetValue = 50000,
                        rewards = new List<TierReward>
                        {
                            new TierReward { rewardType = RewardType.Currency, rewardId = "soft_currency", rewardQuantity = 1000 }
                        }
                    },
                    new ChallengeTier
                    {
                        tierNumber = 2,
                        targetValue = 100000,
                        rewards = new List<TierReward>
                        {
                            new TierReward { rewardType = RewardType.Skin, rewardId = "skin_master_crafter", rewardQuantity = 1 },
                            new TierReward { rewardType = RewardType.Title, rewardId = "title_master_craftsman", rewardQuantity = 1 }
                        }
                    }
                }
            });

            Debug.Log($"[CommunityChallenges] Initialized {activeChallenges.Count} challenges");
        }

        private void CreateChallenge(CommunityChallenge challenge)
        {
            activeChallenges[challenge.challengeId] = challenge;
        }

        private void StartActiveChallenges()
        {
            foreach (var challenge in activeChallenges.Values)
            {
                OnChallengeStarted?.Invoke(challenge.challengeId);
                NotifyChallengeStartClientRpc(challenge.challengeId, challenge.challengeName);
            }
        }

        [ClientRpc]
        private void NotifyChallengeStartClientRpc(string challengeId, string challengeName)
        {
            Debug.Log($"[CommunityChallenges] 🌍 NEW COMMUNITY CHALLENGE: {challengeName}");
        }

        private void Update()
        {
            if (!IsServer) return;

            UpdateChallenges();
        }

        private void UpdateChallenges()
        {
            var expiredChallenges = new List<string>();

            foreach (var challenge in activeChallenges.Values)
            {
                // Check if expired
                if (DateTime.UtcNow >= challenge.endDate && !challenge.isCompleted)
                {
                    expiredChallenges.Add(challenge.challengeId);
                }

                // Check tier progress
                CheckTierProgress(challenge);
            }

            // End expired challenges
            foreach (var challengeId in expiredChallenges)
            {
                EndChallenge(challengeId);
            }
        }

        private void CheckTierProgress(CommunityChallenge challenge)
        {
            for (int i = challenge.currentTier; i < challenge.tiers.Count; i++)
            {
                var tier = challenge.tiers[i];

                if (challenge.currentProgress >= tier.targetValue && !tier.isUnlocked)
                {
                    UnlockTier(challenge, i);
                }
            }
        }

        private void UnlockTier(CommunityChallenge challenge, int tierIndex)
        {
            var tier = challenge.tiers[tierIndex];
            tier.isUnlocked = true;
            challenge.currentTier = tierIndex + 1;

            OnChallengeProgress?.Invoke(challenge.challengeId, tier.tierNumber);
            NotifyTierUnlockedClientRpc(challenge.challengeId, tier.tierNumber, challenge.currentProgress, tier.targetValue);

            // Distribute rewards to all participating players
            DistributeTierRewards(challenge.challengeId, tier);

            Debug.Log($"[CommunityChallenges] 🎉 Tier {tier.tierNumber} unlocked for {challenge.challengeName}!");

            // Check if all tiers complete
            if (tierIndex == challenge.tiers.Count - 1)
            {
                CompleteChallenge(challenge.challengeId);
            }
        }

        [ClientRpc]
        private void NotifyTierUnlockedClientRpc(string challengeId, int tierNumber, long currentProgress, long targetValue)
        {
            Debug.Log($"[CommunityChallenges] 🎉 TIER {tierNumber} UNLOCKED! Progress: {currentProgress:N0}/{targetValue:N0}");
        }

        private void DistributeTierRewards(string challengeId, ChallengeTier tier)
        {
            // Give rewards to all players who contributed
            foreach (var kvp in playerContributions)
            {
                ulong playerId = kvp.Key;
                var contribution = kvp.Value;

                if (contribution.challengeContributions.ContainsKey(challengeId) &&
                    contribution.challengeContributions[challengeId] > 0)
                {
                    GiveRewardsToPlayer(playerId, tier.rewards);
                }
            }
        }

        private void GiveRewardsToPlayer(ulong playerId, List<TierReward> rewards)
        {
            foreach (var reward in rewards)
            {
                switch (reward.rewardType)
                {
                    case RewardType.Currency:
                        // CurrencySystem.Instance.AddCurrency(playerId, reward.rewardId, reward.rewardQuantity);
                        break;

                    case RewardType.XP:
                        // ProgressionSystem.Instance.AddXP(playerId, reward.rewardQuantity);
                        break;

                    case RewardType.Weapon:
                    case RewardType.Skin:
                    case RewardType.Emote:
                        // InventorySystem.Instance.AddItem(playerId, reward.rewardId);
                        break;

                    case RewardType.Title:
                        // TitleSystem.Instance.UnlockTitle(playerId, reward.rewardId);
                        break;

                    case RewardType.Badge:
                        // BadgeSystem.Instance.AwardBadge(playerId, reward.rewardId);
                        break;
                }
            }
        }

        private void CompleteChallenge(string challengeId)
        {
            if (!activeChallenges.ContainsKey(challengeId)) return;

            var challenge = activeChallenges[challengeId];
            challenge.isCompleted = true;

            // Record history
            challengeHistory.Add(new CompletedChallenge
            {
                challengeId = challenge.challengeId,
                challengeName = challenge.challengeName,
                completionDate = DateTime.UtcNow,
                finalProgress = challenge.currentProgress,
                participatingPlayers = playerContributions.Count
            });

            OnChallengeCompleted?.Invoke(challengeId);
            NotifyChallengeCompleteClientRpc(challenge.challengeName, challenge.currentProgress);

            Debug.Log($"[CommunityChallenges] ✅ CHALLENGE COMPLETE: {challenge.challengeName}");
        }

        [ClientRpc]
        private void NotifyChallengeCompleteClientRpc(string challengeName, long finalProgress)
        {
            Debug.Log($"[CommunityChallenges] ✅🌍 COMMUNITY CHALLENGE COMPLETE: {challengeName}!");
            Debug.Log($"   Final Progress: {finalProgress:N0}");
            Debug.Log($"   Thank you to all players who participated!");
        }

        private void EndChallenge(string challengeId)
        {
            if (!activeChallenges.ContainsKey(challengeId)) return;

            var challenge = activeChallenges[challengeId];

            Debug.Log($"[CommunityChallenges] Challenge expired: {challenge.challengeName}");

            // Could still give partial rewards for effort
            activeChallenges.Remove(challengeId);
        }

        // Public API

        [ServerRpc(RequireOwnership = false)]
        public void ContributeToChallengeServerRpc(ulong playerId, string challengeId, long amount, ServerRpcParams rpcParams = default)
        {
            if (!activeChallenges.ContainsKey(challengeId)) return;

            var challenge = activeChallenges[challengeId];

            if (challenge.isCompleted) return;

            // Update challenge progress
            challenge.currentProgress += amount;

            // Track player contribution
            if (!playerContributions.ContainsKey(playerId))
            {
                playerContributions[playerId] = new PlayerContribution();
            }

            var contribution = playerContributions[playerId];

            if (!contribution.challengeContributions.ContainsKey(challengeId))
            {
                contribution.challengeContributions[challengeId] = 0;
            }

            contribution.challengeContributions[challengeId] += amount;
        }

        // Getters

        public List<CommunityChallenge> GetActiveChallenges()
        {
            return activeChallenges.Values.ToList();
        }

        public CommunityChallenge GetChallenge(string challengeId)
        {
            return activeChallenges.ContainsKey(challengeId) ? activeChallenges[challengeId] : null;
        }

        public PlayerContribution GetPlayerContribution(ulong playerId)
        {
            return playerContributions.ContainsKey(playerId) ? playerContributions[playerId] : null;
        }

        public List<CompletedChallenge> GetChallengeHistory()
        {
            return challengeHistory;
        }

        public float GetChallengeProgress(string challengeId)
        {
            if (!activeChallenges.ContainsKey(challengeId)) return 0f;

            var challenge = activeChallenges[challengeId];

            if (challenge.tiers.Count == 0) return 0f;

            long maxTarget = challenge.tiers.Last().targetValue;

            return (float)challenge.currentProgress / maxTarget;
        }
    }
}
