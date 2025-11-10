using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.BattlePass
{
    /// <summary>
    /// Comprehensive battle pass/season pass system with free and premium tracks, daily challenges,
    /// tier rewards, cosmetics, and complete progression mechanics for player engagement.
    /// </summary>
    public class BattlePassSystem : NetworkBehaviour
    {
        public static BattlePassSystem Instance { get; private set; }

        [Header("Battle Pass Configuration")]
        [SerializeField] private int maxTier = 100;
        [SerializeField] private int xpPerTier = 1000;
        [SerializeField] private int premiumPassCost = 1000; // Hard currency
        [SerializeField] private int tierSkipCost = 100; // Hard currency

        // Battle pass data
        private Dictionary<int, SeasonPass> seasonPassDatabase = new Dictionary<int, SeasonPass>();
        private Dictionary<ulong, PlayerBattlePassData> playerBattlePassData = new Dictionary<ulong, PlayerBattlePassData>();
        private int currentSeasonNumber = 1;

        // Events
        public event Action<ulong, int> OnTierComplete;
        public event Action<ulong, int, string> OnRewardClaimed;
        public event Action<ulong> OnPremiumPurchased;
        public event Action<ulong, string> OnChallengeCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer)
            {
                InitializeSeasonPasses();
            }
        }

        private void InitializeSeasonPasses()
        {
            // SEASON 1: Survival Origins
            seasonPassDatabase[1] = new SeasonPass
            {
                seasonNumber = 1,
                seasonName = "Survival Origins",
                description = "The beginning of the apocalypse",
                startDate = new DateTime(2024, 1, 1),
                endDate = new DateTime(2024, 4, 1),
                maxTier = maxTier,
                xpPerTier = xpPerTier,
                freeTierRewards = GenerateSeason1FreeTierRewards(),
                premiumTierRewards = GenerateSeason1PremiumTierRewards(),
                dailyChallenges = GenerateSeason1DailyChallenges(),
                weeklyChallenges = GenerateSeason1WeeklyChallenges()
            };

            // SEASON 2: Outbreak
            seasonPassDatabase[2] = new SeasonPass
            {
                seasonNumber = 2,
                seasonName = "Outbreak",
                description = "The infection spreads",
                startDate = new DateTime(2024, 4, 1),
                endDate = new DateTime(2024, 7, 1),
                maxTier = maxTier,
                xpPerTier = xpPerTier,
                freeTierRewards = GenerateSeason2FreeTierRewards(),
                premiumTierRewards = GenerateSeason2PremiumTierRewards(),
                dailyChallenges = GenerateSeason2DailyChallenges(),
                weeklyChallenges = GenerateSeason2WeeklyChallenges()
            };

            Debug.Log($"Initialized {seasonPassDatabase.Count} season passes");
        }

        private Dictionary<int, TierReward> GenerateSeason1FreeTierRewards()
        {
            var rewards = new Dictionary<int, TierReward>();

            // Every tier has a reward in free track
            for (int tier = 1; tier <= maxTier; tier++)
            {
                var reward = new TierReward { tier = tier, rewards = new List<RewardItem>() };

                if (tier % 10 == 0) // Major milestones
                {
                    reward.rewards.Add(new RewardItem { rewardType = RewardType.SoftCurrency, itemId = "currency_soft", quantity = 1000 });
                    reward.rewards.Add(new RewardItem { rewardType = RewardType.Cosmetic, itemId = $"skin_free_tier_{tier}", quantity = 1 });
                }
                else if (tier % 5 == 0) // Mid milestones
                {
                    reward.rewards.Add(new RewardItem { rewardType = RewardType.SoftCurrency, itemId = "currency_soft", quantity = 500 });
                    reward.rewards.Add(new RewardItem { rewardType = RewardType.Item, itemId = "consumable_medkit", quantity = 5 });
                }
                else // Regular tiers
                {
                    reward.rewards.Add(new RewardItem { rewardType = RewardType.SoftCurrency, itemId = "currency_soft", quantity = 100 });
                }

                rewards[tier] = reward;
            }

            return rewards;
        }

        private Dictionary<int, TierReward> GenerateSeason1PremiumTierRewards()
        {
            var rewards = new Dictionary<int, TierReward>();

            for (int tier = 1; tier <= maxTier; tier++)
            {
                var reward = new TierReward { tier = tier, rewards = new List<RewardItem>() };

                if (tier == 100) // Max tier ultimate reward
                {
                    reward.rewards.Add(new RewardItem { rewardType = RewardType.Cosmetic, itemId = "skin_legendary_s1_ultimate", quantity = 1 });
                    reward.rewards.Add(new RewardItem { rewardType = RewardType.HardCurrency, itemId = "currency_hard", quantity = 500 });
                    reward.rewards.Add(new RewardItem { rewardType = RewardType.Title, itemId = "title_s1_legend", quantity = 1 });
                }
                else if (tier % 25 == 0) // Legendary milestones
                {
                    reward.rewards.Add(new RewardItem { rewardType = RewardType.Cosmetic, itemId = $"skin_legendary_s1_tier_{tier}", quantity = 1 });
                    reward.rewards.Add(new RewardItem { rewardType = RewardType.HardCurrency, itemId = "currency_hard", quantity = 200 });
                }
                else if (tier % 10 == 0) // Epic milestones
                {
                    reward.rewards.Add(new RewardItem { rewardType = RewardType.Cosmetic, itemId = $"skin_epic_s1_tier_{tier}", quantity = 1 });
                    reward.rewards.Add(new RewardItem { rewardType = RewardType.HardCurrency, itemId = "currency_hard", quantity = 100 });
                    reward.rewards.Add(new RewardItem { rewardType = RewardType.Item, itemId = "crate_premium", quantity = 1 });
                }
                else if (tier % 5 == 0) // Rare milestones
                {
                    reward.rewards.Add(new RewardItem { rewardType = RewardType.Cosmetic, itemId = $"skin_rare_s1_tier_{tier}", quantity = 1 });
                    reward.rewards.Add(new RewardItem { rewardType = RewardType.SoftCurrency, itemId = "currency_soft", quantity = 2000 });
                }
                else // Regular premium tiers
                {
                    reward.rewards.Add(new RewardItem { rewardType = RewardType.SoftCurrency, itemId = "currency_soft", quantity = 500 });
                    reward.rewards.Add(new RewardItem { rewardType = RewardType.XPBoost, itemId = "boost_xp", quantity = 1 });
                }

                rewards[tier] = reward;
            }

            return rewards;
        }

        private Dictionary<int, TierReward> GenerateSeason2FreeTierRewards()
        {
            var rewards = new Dictionary<int, TierReward>();

            for (int tier = 1; tier <= maxTier; tier++)
            {
                var reward = new TierReward { tier = tier, rewards = new List<RewardItem>() };

                if (tier % 10 == 0)
                {
                    reward.rewards.Add(new RewardItem { rewardType = RewardType.SoftCurrency, itemId = "currency_soft", quantity = 1200 });
                    reward.rewards.Add(new RewardItem { rewardType = RewardType.Cosmetic, itemId = $"skin_free_s2_tier_{tier}", quantity = 1 });
                }
                else if (tier % 5 == 0)
                {
                    reward.rewards.Add(new RewardItem { rewardType = RewardType.SoftCurrency, itemId = "currency_soft", quantity = 600 });
                }
                else
                {
                    reward.rewards.Add(new RewardItem { rewardType = RewardType.SoftCurrency, itemId = "currency_soft", quantity = 150 });
                }

                rewards[tier] = reward;
            }

            return rewards;
        }

        private Dictionary<int, TierReward> GenerateSeason2PremiumTierRewards()
        {
            var rewards = new Dictionary<int, TierReward>();

            for (int tier = 1; tier <= maxTier; tier++)
            {
                var reward = new TierReward { tier = tier, rewards = new List<RewardItem>() };

                if (tier == 100)
                {
                    reward.rewards.Add(new RewardItem { rewardType = RewardType.Cosmetic, itemId = "skin_legendary_s2_outbreak", quantity = 1 });
                    reward.rewards.Add(new RewardItem { rewardType = RewardType.HardCurrency, itemId = "currency_hard", quantity = 600 });
                    reward.rewards.Add(new RewardItem { rewardType = RewardType.Title, itemId = "title_outbreak_survivor", quantity = 1 });
                }
                else if (tier % 25 == 0)
                {
                    reward.rewards.Add(new RewardItem { rewardType = RewardType.Cosmetic, itemId = $"skin_legendary_s2_tier_{tier}", quantity = 1 });
                    reward.rewards.Add(new RewardItem { rewardType = RewardType.HardCurrency, itemId = "currency_hard", quantity = 250 });
                }
                else if (tier % 10 == 0)
                {
                    reward.rewards.Add(new RewardItem { rewardType = RewardType.Cosmetic, itemId = $"skin_epic_s2_tier_{tier}", quantity = 1 });
                    reward.rewards.Add(new RewardItem { rewardType = RewardType.HardCurrency, itemId = "currency_hard", quantity = 120 });
                }
                else
                {
                    reward.rewards.Add(new RewardItem { rewardType = RewardType.SoftCurrency, itemId = "currency_soft", quantity = 600 });
                }

                rewards[tier] = reward;
            }

            return rewards;
        }

        private List<BattlePassChallenge> GenerateSeason1DailyChallenges()
        {
            return new List<BattlePassChallenge>
            {
                new BattlePassChallenge
                {
                    challengeId = "daily_s1_kills",
                    challengeName = "Zombie Slayer",
                    description = "Kill 50 zombies",
                    challengeType = ChallengeType.Daily,
                    objectiveType = ChallengeObjectiveType.Kill,
                    targetId = "zombie_any",
                    requiredAmount = 50,
                    xpReward = 500
                },
                new BattlePassChallenge
                {
                    challengeId = "daily_s1_headshots",
                    challengeName = "Precision",
                    description = "Get 20 headshot kills",
                    challengeType = ChallengeType.Daily,
                    objectiveType = ChallengeObjectiveType.HeadshotKill,
                    targetId = "zombie_any",
                    requiredAmount = 20,
                    xpReward = 750
                },
                new BattlePassChallenge
                {
                    challengeId = "daily_s1_loot",
                    challengeName = "Scavenger",
                    description = "Loot 10 containers",
                    challengeType = ChallengeType.Daily,
                    objectiveType = ChallengeObjectiveType.Loot,
                    targetId = "container_any",
                    requiredAmount = 10,
                    xpReward = 400
                },
                new BattlePassChallenge
                {
                    challengeId = "daily_s1_distance",
                    challengeName = "Explorer",
                    description = "Travel 5000 meters",
                    challengeType = ChallengeType.Daily,
                    objectiveType = ChallengeObjectiveType.Travel,
                    requiredAmount = 5000,
                    xpReward = 600
                }
            };
        }

        private List<BattlePassChallenge> GenerateSeason1WeeklyChallenges()
        {
            return new List<BattlePassChallenge>
            {
                new BattlePassChallenge
                {
                    challengeId = "weekly_s1_survival",
                    challengeName = "Survivor",
                    description = "Survive 10 nights",
                    challengeType = ChallengeType.Weekly,
                    objectiveType = ChallengeObjectiveType.Survive,
                    requiredAmount = 10,
                    xpReward = 3000
                },
                new BattlePassChallenge
                {
                    challengeId = "weekly_s1_boss",
                    challengeName = "Boss Hunter",
                    description = "Defeat 3 world bosses",
                    challengeType = ChallengeType.Weekly,
                    objectiveType = ChallengeObjectiveType.Kill,
                    targetId = "boss_any",
                    requiredAmount = 3,
                    xpReward = 5000
                },
                new BattlePassChallenge
                {
                    challengeId = "weekly_s1_crafting",
                    challengeName = "Master Crafter",
                    description = "Craft 50 items",
                    challengeType = ChallengeType.Weekly,
                    objectiveType = ChallengeObjectiveType.Craft,
                    requiredAmount = 50,
                    xpReward = 2500
                },
                new BattlePassChallenge
                {
                    challengeId = "weekly_s1_party",
                    challengeName = "Team Player",
                    description = "Complete 20 missions in a party",
                    challengeType = ChallengeType.Weekly,
                    objectiveType = ChallengeObjectiveType.PartyMission,
                    requiredAmount = 20,
                    xpReward = 4000
                }
            };
        }

        private List<BattlePassChallenge> GenerateSeason2DailyChallenges()
        {
            return new List<BattlePassChallenge>
            {
                new BattlePassChallenge
                {
                    challengeId = "daily_s2_kills",
                    challengeName = "Outbreak Control",
                    description = "Kill 75 zombies",
                    challengeType = ChallengeType.Daily,
                    objectiveType = ChallengeObjectiveType.Kill,
                    targetId = "zombie_any",
                    requiredAmount = 75,
                    xpReward = 600
                },
                new BattlePassChallenge
                {
                    challengeId = "daily_s2_special",
                    challengeName = "Special Ops",
                    description = "Kill 10 special zombies",
                    challengeType = ChallengeType.Daily,
                    objectiveType = ChallengeObjectiveType.Kill,
                    targetId = "zombie_special",
                    requiredAmount = 10,
                    xpReward = 800
                }
            };
        }

        private List<BattlePassChallenge> GenerateSeason2WeeklyChallenges()
        {
            return new List<BattlePassChallenge>
            {
                new BattlePassChallenge
                {
                    challengeId = "weekly_s2_horde",
                    challengeName = "Horde Breaker",
                    description = "Complete 5 horde mode matches",
                    challengeType = ChallengeType.Weekly,
                    objectiveType = ChallengeObjectiveType.GameMode,
                    targetId = "gamemode_horde",
                    requiredAmount = 5,
                    xpReward = 4000
                },
                new BattlePassChallenge
                {
                    challengeId = "weekly_s2_territory",
                    challengeName = "Territory Control",
                    description = "Capture 3 territories",
                    challengeType = ChallengeType.Weekly,
                    objectiveType = ChallengeObjectiveType.Territory,
                    requiredAmount = 3,
                    xpReward = 6000
                }
            };
        }

        // Main battle pass operations
        [ServerRpc(RequireOwnership = false)]
        public void InitializePlayerBattlePassServerRpc(ulong playerId, ServerRpcParams rpcParams = default)
        {
            if (playerBattlePassData.ContainsKey(playerId)) return;

            var data = new PlayerBattlePassData
            {
                playerId = playerId,
                currentSeason = currentSeasonNumber,
                currentTier = 0,
                currentXP = 0,
                hasPremium = false,
                claimedFreeTiers = new List<int>(),
                claimedPremiumTiers = new List<int>(),
                activeChallenges = new Dictionary<string, ChallengeProgress>(),
                completedChallenges = new List<string>()
            };

            // Initialize daily challenges
            if (seasonPassDatabase.TryGetValue(currentSeasonNumber, out var season))
            {
                foreach (var challenge in season.dailyChallenges)
                {
                    data.activeChallenges[challenge.challengeId] = new ChallengeProgress
                    {
                        challengeId = challenge.challengeId,
                        currentProgress = 0,
                        completed = false
                    };
                }
            }

            playerBattlePassData[playerId] = data;

            Debug.Log($"Initialized battle pass for player {playerId}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void AddBattlePassXPServerRpc(ulong playerId, int xpAmount, ServerRpcParams rpcParams = default)
        {
            if (!playerBattlePassData.TryGetValue(playerId, out var data)) return;

            data.currentXP += xpAmount;

            // Check for tier ups
            while (data.currentXP >= xpPerTier && data.currentTier < maxTier)
            {
                data.currentXP -= xpPerTier;
                data.currentTier++;

                OnTierComplete?.Invoke(playerId, data.currentTier);
                NotifyTierCompleteClientRpc(playerId, data.currentTier);

                Debug.Log($"Player {playerId} reached battle pass tier {data.currentTier}");
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void ClaimTierRewardServerRpc(ulong playerId, int tier, bool isPremium, ServerRpcParams rpcParams = default)
        {
            if (!playerBattlePassData.TryGetValue(playerId, out var data)) return;
            if (!seasonPassDatabase.TryGetValue(data.currentSeason, out var season)) return;

            // Check if tier is unlocked
            if (tier > data.currentTier)
            {
                Debug.LogWarning($"Player {playerId} hasn't unlocked tier {tier}");
                return;
            }

            // Check if already claimed
            if (isPremium)
            {
                if (!data.hasPremium)
                {
                    Debug.LogWarning($"Player {playerId} doesn't have premium pass");
                    return;
                }

                if (data.claimedPremiumTiers.Contains(tier)) return;

                // Grant premium rewards
                if (season.premiumTierRewards.TryGetValue(tier, out var premiumReward))
                {
                    GrantRewards(playerId, premiumReward.rewards);
                    data.claimedPremiumTiers.Add(tier);
                }
            }
            else
            {
                if (data.claimedFreeTiers.Contains(tier)) return;

                // Grant free rewards
                if (season.freeTierRewards.TryGetValue(tier, out var freeReward))
                {
                    GrantRewards(playerId, freeReward.rewards);
                    data.claimedFreeTiers.Add(tier);
                }
            }

            OnRewardClaimed?.Invoke(playerId, tier, isPremium ? "premium" : "free");

            Debug.Log($"Player {playerId} claimed tier {tier} {(isPremium ? "premium" : "free")} reward");
        }

        [ServerRpc(RequireOwnership = false)]
        public void PurchasePremiumPassServerRpc(ulong playerId, ServerRpcParams rpcParams = default)
        {
            if (!playerBattlePassData.TryGetValue(playerId, out var data)) return;

            if (data.hasPremium)
            {
                Debug.LogWarning($"Player {playerId} already has premium pass");
                return;
            }

            // Would integrate with economy system
            // if (!Economy.EconomyManager.Instance.RemoveHardCurrency(playerId, premiumPassCost)) return;

            data.hasPremium = true;

            OnPremiumPurchased?.Invoke(playerId);
            NotifyPremiumPurchasedClientRpc(playerId);

            Debug.Log($"Player {playerId} purchased premium battle pass");
        }

        [ServerRpc(RequireOwnership = false)]
        public void SkipTiersServerRpc(ulong playerId, int tierCount, ServerRpcParams rpcParams = default)
        {
            if (!playerBattlePassData.TryGetValue(playerId, out var data)) return;

            int cost = tierCount * tierSkipCost;

            // Would integrate with economy system
            // if (!Economy.EconomyManager.Instance.RemoveHardCurrency(playerId, cost)) return;

            data.currentTier = Mathf.Min(data.currentTier + tierCount, maxTier);

            NotifyTiersSkippedClientRpc(playerId, data.currentTier);

            Debug.Log($"Player {playerId} skipped to tier {data.currentTier}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void UpdateChallengeProgressServerRpc(ulong playerId, string challengeId, int progress, ServerRpcParams rpcParams = default)
        {
            if (!playerBattlePassData.TryGetValue(playerId, out var data)) return;
            if (!data.activeChallenges.TryGetValue(challengeId, out var challengeProgress)) return;
            if (challengeProgress.completed) return;

            if (!seasonPassDatabase.TryGetValue(data.currentSeason, out var season)) return;

            var challengeDef = season.dailyChallenges.FirstOrDefault(c => c.challengeId == challengeId);
            if (challengeDef == null)
            {
                challengeDef = season.weeklyChallenges.FirstOrDefault(c => c.challengeId == challengeId);
            }
            if (challengeDef == null) return;

            challengeProgress.currentProgress += progress;

            // Check completion
            if (challengeProgress.currentProgress >= challengeDef.requiredAmount)
            {
                challengeProgress.completed = true;
                data.completedChallenges.Add(challengeId);

                // Grant XP
                AddBattlePassXPServerRpc(playerId, challengeDef.xpReward);

                OnChallengeCompleted?.Invoke(playerId, challengeId);
                NotifyChallengeCompletedClientRpc(playerId, challengeId);

                Debug.Log($"Player {playerId} completed challenge {challengeDef.challengeName}");
            }
        }

        private void GrantRewards(ulong playerId, List<RewardItem> rewards)
        {
            foreach (var reward in rewards)
            {
                switch (reward.rewardType)
                {
                    case RewardType.SoftCurrency:
                        // Economy.EconomyManager.Instance.AddSoftCurrency(playerId, reward.quantity);
                        break;
                    case RewardType.HardCurrency:
                        // Economy.EconomyManager.Instance.AddHardCurrency(playerId, reward.quantity);
                        break;
                    case RewardType.Item:
                        // Inventory.InventorySystem.Instance.AddItemServerRpc(playerId, reward.itemId, reward.quantity, Inventory.ContainerType.Backpack);
                        break;
                    case RewardType.Cosmetic:
                        // Would integrate with cosmetics system
                        break;
                    case RewardType.XPBoost:
                        // Would integrate with buff system
                        break;
                    case RewardType.Title:
                        // Would integrate with title system
                        break;
                }
            }
        }

        // Client RPCs
        [ClientRpc]
        private void NotifyTierCompleteClientRpc(ulong playerId, int tier) { }

        [ClientRpc]
        private void NotifyPremiumPurchasedClientRpc(ulong playerId) { }

        [ClientRpc]
        private void NotifyTiersSkippedClientRpc(ulong playerId, int newTier) { }

        [ClientRpc]
        private void NotifyChallengeCompletedClientRpc(ulong playerId, string challengeId) { }

        // Public getters
        public SeasonPass GetCurrentSeason() => seasonPassDatabase.GetValueOrDefault(currentSeasonNumber);
        public PlayerBattlePassData GetPlayerData(ulong playerId) => playerBattlePassData.GetValueOrDefault(playerId);
    }

    // Data structures
    [Serializable]
    public class SeasonPass
    {
        public int seasonNumber;
        public string seasonName;
        public string description;
        public DateTime startDate;
        public DateTime endDate;
        public int maxTier;
        public int xpPerTier;
        public Dictionary<int, TierReward> freeTierRewards;
        public Dictionary<int, TierReward> premiumTierRewards;
        public List<BattlePassChallenge> dailyChallenges;
        public List<BattlePassChallenge> weeklyChallenges;
    }

    [Serializable]
    public class TierReward
    {
        public int tier;
        public List<RewardItem> rewards;
    }

    [Serializable]
    public class RewardItem
    {
        public RewardType rewardType;
        public string itemId;
        public int quantity;
    }

    [Serializable]
    public class BattlePassChallenge
    {
        public string challengeId;
        public string challengeName;
        public string description;
        public ChallengeType challengeType;
        public ChallengeObjectiveType objectiveType;
        public string targetId;
        public int requiredAmount;
        public int xpReward;
    }

    [Serializable]
    public class PlayerBattlePassData
    {
        public ulong playerId;
        public int currentSeason;
        public int currentTier;
        public int currentXP;
        public bool hasPremium;
        public List<int> claimedFreeTiers;
        public List<int> claimedPremiumTiers;
        public Dictionary<string, ChallengeProgress> activeChallenges;
        public List<string> completedChallenges;
    }

    [Serializable]
    public class ChallengeProgress
    {
        public string challengeId;
        public int currentProgress;
        public bool completed;
    }

    public enum RewardType { SoftCurrency, HardCurrency, Item, Cosmetic, XPBoost, Title }
    public enum ChallengeType { Daily, Weekly, Seasonal }
    public enum ChallengeObjectiveType { Kill, HeadshotKill, Loot, Travel, Survive, Craft, GameMode, PartyMission, Territory }
}
