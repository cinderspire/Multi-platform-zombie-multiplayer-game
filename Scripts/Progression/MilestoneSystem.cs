using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Progression
{
    /// <summary>
    /// Milestone tracking system monitoring player progress across various metrics
    /// and rewarding achievements with escalating bonuses.
    /// </summary>
    public class MilestoneSystem : NetworkBehaviour
    {
        public static MilestoneSystem Instance { get; private set; }

        [Header("Milestone Configuration")]
        [SerializeField] private bool enableAutoTracking = true;

        private Dictionary<ulong, PlayerMilestones> playerMilestones = new Dictionary<ulong, PlayerMilestones>();
        private Dictionary<string, MilestoneDefinition> milestoneDefinitions = new Dictionary<string, MilestoneDefinition>();

        public event Action<ulong, string, int> OnMilestoneProgressUpdated;
        public event Action<ulong, string, int> OnMilestoneCompleted;
        public event Action<ulong, MilestoneCategory, int> OnCategoryMastered;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            InitializeMilestones();
        }

        private void InitializeMilestones()
        {
            // Combat Milestones
            AddMilestone("kills", "Total Kills", MilestoneCategory.Combat,
                new int[] { 10, 50, 100, 500, 1000, 5000, 10000, 25000, 50000, 100000 });
            AddMilestone("headshots", "Headshot Kills", MilestoneCategory.Combat,
                new int[] { 5, 25, 50, 250, 500, 2500, 5000, 10000 });
            AddMilestone("melee_kills", "Melee Kills", MilestoneCategory.Combat,
                new int[] { 5, 25, 50, 100, 250, 500, 1000 });
            AddMilestone("explosives", "Explosive Kills", MilestoneCategory.Combat,
                new int[] { 10, 50, 100, 250, 500, 1000 });

            // Survival Milestones
            AddMilestone("rounds_survived", "Rounds Survived", MilestoneCategory.Survival,
                new int[] { 10, 25, 50, 100, 250, 500, 1000, 2500 });
            AddMilestone("time_survived", "Hours Survived", MilestoneCategory.Survival,
                new int[] { 1, 5, 10, 25, 50, 100, 250, 500 });
            AddMilestone("revives", "Teammates Revived", MilestoneCategory.Survival,
                new int[] { 5, 25, 50, 100, 250, 500 });

            // Progression Milestones
            AddMilestone("level", "Player Level", MilestoneCategory.Progression,
                new int[] { 10, 25, 50, 75, 100, 150, 200, 250, 500, 1000 });
            AddMilestone("prestige", "Prestige Ranks", MilestoneCategory.Progression,
                new int[] { 1, 3, 5, 10, 15, 20, 25, 30 });
            AddMilestone("xp_earned", "Total XP Earned", MilestoneCategory.Progression,
                new int[] { 10000, 50000, 100000, 500000, 1000000, 5000000 });

            // Social Milestones
            AddMilestone("matches_played", "Matches Played", MilestoneCategory.Social,
                new int[] { 10, 50, 100, 500, 1000, 5000, 10000 });
            AddMilestone("wins", "Victories", MilestoneCategory.Social,
                new int[] { 5, 25, 50, 100, 250, 500, 1000, 2500 });
            AddMilestone("friends", "Friends Added", MilestoneCategory.Social,
                new int[] { 5, 10, 25, 50, 100, 250 });
            AddMilestone("clan_level", "Clan Level", MilestoneCategory.Social,
                new int[] { 5, 10, 25, 50, 100 });

            // Collection Milestones
            AddMilestone("weapons_unlocked", "Weapons Unlocked", MilestoneCategory.Collection,
                new int[] { 5, 10, 25, 50, 100 });
            AddMilestone("skins_collected", "Skins Collected", MilestoneCategory.Collection,
                new int[] { 10, 25, 50, 100, 250, 500 });
            AddMilestone("achievements", "Achievements", MilestoneCategory.Collection,
                new int[] { 10, 25, 50, 75, 100 });

            // Economy Milestones
            AddMilestone("currency_earned", "Currency Earned", MilestoneCategory.Economy,
                new int[] { 10000, 50000, 100000, 500000, 1000000, 5000000 });
            AddMilestone("items_crafted", "Items Crafted", MilestoneCategory.Economy,
                new int[] { 10, 50, 100, 500, 1000 });
            AddMilestone("trades_completed", "Trades Completed", MilestoneCategory.Economy,
                new int[] { 5, 25, 50, 100, 250 });

            // Special Milestones
            AddMilestone("boss_kills", "Boss Kills", MilestoneCategory.Special,
                new int[] { 1, 5, 10, 25, 50, 100 });
            AddMilestone("events_won", "Events Won", MilestoneCategory.Special,
                new int[] { 1, 5, 10, 25, 50 });
            AddMilestone("perfect_rounds", "Perfect Rounds", MilestoneCategory.Special,
                new int[] { 1, 5, 10, 25, 50 });
        }

        private void AddMilestone(string id, string name, MilestoneCategory category, int[] tiers)
        {
            milestoneDefinitions[id] = new MilestoneDefinition
            {
                milestoneId = id,
                milestoneName = name,
                category = category,
                tiers = tiers.ToList()
            };
        }

        /// <summary>
        /// Update milestone progress
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void UpdateMilestoneProgressServerRpc(ulong playerId, string milestoneId, int value, ServerRpcParams rpcParams = default)
        {
            if (!milestoneDefinitions.ContainsKey(milestoneId)) return;

            if (!playerMilestones.ContainsKey(playerId))
            {
                InitializePlayerMilestones(playerId);
            }

            var milestones = playerMilestones[playerId];
            if (!milestones.progress.ContainsKey(milestoneId))
            {
                milestones.progress[milestoneId] = new MilestoneProgress
                {
                    milestoneId = milestoneId,
                    currentValue = 0,
                    currentTier = 0
                };
            }

            var progress = milestones.progress[milestoneId];
            int oldValue = progress.currentValue;
            progress.currentValue = value;

            OnMilestoneProgressUpdated?.Invoke(playerId, milestoneId, value);

            // Check if any new tiers reached
            CheckTierCompletion(playerId, milestoneId, oldValue, value);
        }

        /// <summary>
        /// Increment milestone progress
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void IncrementMilestoneServerRpc(ulong playerId, string milestoneId, int amount, ServerRpcParams rpcParams = default)
        {
            if (!playerMilestones.ContainsKey(playerId))
            {
                InitializePlayerMilestones(playerId);
            }

            var milestones = playerMilestones[playerId];
            if (!milestones.progress.ContainsKey(milestoneId))
            {
                milestones.progress[milestoneId] = new MilestoneProgress
                {
                    milestoneId = milestoneId,
                    currentValue = 0,
                    currentTier = 0
                };
            }

            int newValue = milestones.progress[milestoneId].currentValue + amount;
            UpdateMilestoneProgressServerRpc(playerId, milestoneId, newValue);
        }

        private void CheckTierCompletion(ulong playerId, string milestoneId, int oldValue, int newValue)
        {
            var definition = milestoneDefinitions[milestoneId];
            var progress = playerMilestones[playerId].progress[milestoneId];

            foreach (int tier in definition.tiers)
            {
                if (newValue >= tier && oldValue < tier)
                {
                    // New tier reached!
                    progress.currentTier = definition.tiers.IndexOf(tier) + 1;
                    CompleteMilestoneTier(playerId, milestoneId, tier, progress.currentTier);
                }
            }

            // Check if category mastered
            if (progress.currentTier == definition.tiers.Count)
            {
                CheckCategoryMastery(playerId, definition.category);
            }
        }

        private void CompleteMilestoneTier(ulong playerId, string milestoneId, int tierValue, int tierNumber)
        {
            OnMilestoneCompleted?.Invoke(playerId, milestoneId, tierNumber);

            // Award rewards
            MilestoneReward reward = CalculateReward(tierNumber, tierValue);
            AwardMilestoneReward(playerId, reward);

            NotifyMilestoneCompletedClientRpc(playerId, milestoneId, tierNumber, tierValue, reward);
        }

        private void CheckCategoryMastery(ulong playerId, MilestoneCategory category)
        {
            var milestones = playerMilestones[playerId];
            int completedInCategory = 0;
            int totalInCategory = 0;

            foreach (var kvp in milestoneDefinitions)
            {
                if (kvp.Value.category == category)
                {
                    totalInCategory++;
                    if (milestones.progress.ContainsKey(kvp.Key))
                    {
                        var progress = milestones.progress[kvp.Key];
                        if (progress.currentTier == kvp.Value.tiers.Count)
                        {
                            completedInCategory++;
                        }
                    }
                }
            }

            if (completedInCategory == totalInCategory && totalInCategory > 0)
            {
                OnCategoryMastered?.Invoke(playerId, category, totalInCategory);
                NotifyCategoryMasteredClientRpc(playerId, category);

                // Award mastery title
                if (TitleSystem.Instance != null)
                {
                    string masteryTitle = $"{category.ToString().ToLower()}_master";
                    TitleSystem.Instance.UnlockTitleServerRpc(playerId, masteryTitle);
                }
            }
        }

        private MilestoneReward CalculateReward(int tierNumber, int tierValue)
        {
            return new MilestoneReward
            {
                xp = 100 * tierNumber * tierNumber,
                currency = 50 * tierNumber * tierNumber,
                titleUnlock = tierNumber >= 5 ? "milestone_tier_" + tierNumber : null
            };
        }

        private void AwardMilestoneReward(ulong playerId, MilestoneReward reward)
        {
            // Award XP
            if (reward.xp > 0 && ProgressionSystem.Instance != null)
            {
                ProgressionSystem.Instance.AddExperienceServerRpc(playerId, reward.xp);
            }

            // Award currency
            // Economy system integration

            // Award title
            if (!string.IsNullOrEmpty(reward.titleUnlock) && TitleSystem.Instance != null)
            {
                TitleSystem.Instance.UnlockTitleServerRpc(playerId, reward.titleUnlock);
            }
        }

        [ClientRpc]
        private void NotifyMilestoneCompletedClientRpc(ulong playerId, string milestoneId, int tier, int value, MilestoneReward reward)
        {
            if (NetworkManager.Singleton.LocalClientId != playerId) return;

            var definition = milestoneDefinitions[milestoneId];
            Debug.Log($"<color=purple>MILESTONE COMPLETED!</color>");
            Debug.Log($"{definition.milestoneName} - Tier {tier}: {value}");
            Debug.Log($"Rewards: {reward.xp} XP, {reward.currency} currency");
        }

        [ClientRpc]
        private void NotifyCategoryMasteredClientRpc(ulong playerId, MilestoneCategory category)
        {
            if (NetworkManager.Singleton.LocalClientId != playerId) return;
            Debug.Log($"<color=gold>CATEGORY MASTERED: {category}!</color>");
        }

        private void InitializePlayerMilestones(ulong playerId)
        {
            playerMilestones[playerId] = new PlayerMilestones
            {
                playerId = playerId,
                progress = new Dictionary<string, MilestoneProgress>()
            };
        }

        public MilestoneProgress GetMilestoneProgress(ulong playerId, string milestoneId)
        {
            if (!playerMilestones.TryGetValue(playerId, out var milestones)) return null;
            return milestones.progress.TryGetValue(milestoneId, out var progress) ? progress : null;
        }

        public int GetCategoryCompletion(ulong playerId, MilestoneCategory category)
        {
            if (!playerMilestones.TryGetValue(playerId, out var milestones)) return 0;

            int completed = 0;
            foreach (var kvp in milestoneDefinitions)
            {
                if (kvp.Value.category == category && milestones.progress.ContainsKey(kvp.Key))
                {
                    if (milestones.progress[kvp.Key].currentTier == kvp.Value.tiers.Count)
                    {
                        completed++;
                    }
                }
            }

            return completed;
        }

        [Serializable]
        private class PlayerMilestones
        {
            public ulong playerId;
            public Dictionary<string, MilestoneProgress> progress;
        }

        [Serializable]
        public class MilestoneProgress
        {
            public string milestoneId;
            public int currentValue;
            public int currentTier;
        }

        [Serializable]
        private class MilestoneDefinition
        {
            public string milestoneId;
            public string milestoneName;
            public MilestoneCategory category;
            public List<int> tiers;
        }

        [Serializable]
        public class MilestoneReward
        {
            public int xp;
            public int currency;
            public string titleUnlock;
        }

        public enum MilestoneCategory
        {
            Combat,
            Survival,
            Progression,
            Social,
            Collection,
            Economy,
            Special
        }
    }
}
