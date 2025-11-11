using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Challenges
{
    /// <summary>
    /// Weekly challenges and bounty system providing rotating objectives
    /// with special rewards to keep players engaged long-term.
    /// </summary>
    public class WeeklyChallengesSystem : NetworkBehaviour
    {
        public static WeeklyChallengesSystem Instance { get; private set; }

        [Header("Challenge Configuration")]
        [SerializeField] private int weeklyChallengesCount = 10;
        [SerializeField] private int bountyCount = 5;
        [SerializeField] private float challengeResetDay = 1; // Monday

        private Dictionary<ulong, PlayerWeeklyChallenges> playerChallenges = new Dictionary<ulong, PlayerWeeklyChallenges>();
        private List<WeeklyChallenge> currentWeeklyChallenges = new List<WeeklyChallenge>();
        private List<Bounty> currentBounties = new List<Bounty>();
        private DateTime weekStartDate;

        public event Action<ulong, string> OnChallengeProgressUpdated;
        public event Action<ulong, string> OnChallengeCompleted;
        public event Action<ulong, string> OnBountyCompleted;
        public event Action OnWeekReset;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            InitializeWeek();
        }

        private void InitializeWeek()
        {
            weekStartDate = GetWeekStartDate();
            GenerateWeeklyChallenges();
            GenerateBounties();
        }

        private DateTime GetWeekStartDate()
        {
            DateTime now = DateTime.UtcNow;
            int daysUntilMonday = ((int)DayOfWeek.Monday - (int)now.DayOfWeek + 7) % 7;
            return now.Date.AddDays(-daysUntilMonday);
        }

        private void GenerateWeeklyChallenges()
        {
            currentWeeklyChallenges.Clear();

            // Kill Challenges
            currentWeeklyChallenges.Add(new WeeklyChallenge
            {
                challengeId = "weekly_kills_100",
                challengeName = "Slayer",
                description = "Eliminate 100 zombies",
                objective = "Kill zombies",
                targetValue = 100,
                difficulty = ChallengeDifficulty.Easy,
                rewards = new ChallengeRewards { xp = 500, currency = 100, itemReward = "common_crate" }
            });

            currentWeeklyChallenges.Add(new WeeklyChallenge
            {
                challengeId = "weekly_headshots_50",
                challengeName = "Headhunter",
                description = "Get 50 headshot kills",
                objective = "Headshot kills",
                targetValue = 50,
                difficulty = ChallengeDifficulty.Medium,
                rewards = new ChallengeRewards { xp = 750, currency = 150, itemReward = "rare_crate" }
            });

            // Survival Challenges
            currentWeeklyChallenges.Add(new WeeklyChallenge
            {
                challengeId = "weekly_survive_50",
                challengeName = "Survivor",
                description = "Survive 50 rounds total",
                objective = "Survive rounds",
                targetValue = 50,
                difficulty = ChallengeDifficulty.Medium,
                rewards = new ChallengeRewards { xp = 1000, currency = 200, itemReward = "rare_crate" }
            });

            // Win Challenges
            currentWeeklyChallenges.Add(new WeeklyChallenge
            {
                challengeId = "weekly_wins_10",
                challengeName = "Victor",
                description = "Win 10 matches",
                objective = "Win matches",
                targetValue = 10,
                difficulty = ChallengeDifficulty.Hard,
                rewards = new ChallengeRewards { xp = 1500, currency = 300, itemReward = "epic_crate" }
            });

            // Team Challenges
            currentWeeklyChallenges.Add(new WeeklyChallenge
            {
                challengeId = "weekly_revives_25",
                challengeName = "Lifesaver",
                description = "Revive 25 teammates",
                objective = "Revive teammates",
                targetValue = 25,
                difficulty = ChallengeDifficulty.Medium,
                rewards = new ChallengeRewards { xp = 800, currency = 150 }
            });

            // Weapon Challenges
            currentWeeklyChallenges.Add(new WeeklyChallenge
            {
                challengeId = "weekly_shotgun_kills_30",
                challengeName = "Shotgun Master",
                description = "Get 30 kills with shotguns",
                objective = "Shotgun kills",
                targetValue = 30,
                difficulty = ChallengeDifficulty.Medium,
                rewards = new ChallengeRewards { xp = 700, currency = 125, itemReward = "shotgun_skin" }
            });

            // Special Challenges
            currentWeeklyChallenges.Add(new WeeklyChallenge
            {
                challengeId = "weekly_boss_kills_5",
                challengeName = "Boss Hunter",
                description = "Defeat 5 boss zombies",
                objective = "Boss kills",
                targetValue = 5,
                difficulty = ChallengeDifficulty.Hard,
                rewards = new ChallengeRewards { xp = 2000, currency = 400, itemReward = "legendary_crate" }
            });

            // Combo Challenges
            currentWeeklyChallenges.Add(new WeeklyChallenge
            {
                challengeId = "weekly_combos_20",
                challengeName = "Combo Master",
                description = "Achieve 20 kill combos",
                objective = "Kill combos",
                targetValue = 20,
                difficulty = ChallengeDifficulty.Hard,
                rewards = new ChallengeRewards { xp = 1200, currency = 250 }
            });

            // Exploration Challenges
            currentWeeklyChallenges.Add(new WeeklyChallenge
            {
                challengeId = "weekly_resource_gather_100",
                challengeName = "Gatherer",
                description = "Collect 100 resources",
                objective = "Gather resources",
                targetValue = 100,
                difficulty = ChallengeDifficulty.Easy,
                rewards = new ChallengeRewards { xp = 600, currency = 100 }
            });

            // Ultimate Challenge
            currentWeeklyChallenges.Add(new WeeklyChallenge
            {
                challengeId = "weekly_ultimate",
                challengeName = "Weekly Ultimate",
                description = "Complete all weekly challenges",
                objective = "Complete challenges",
                targetValue = weeklyChallengesCount - 1, // All except itself
                difficulty = ChallengeDifficulty.Ultimate,
                rewards = new ChallengeRewards { xp = 5000, currency = 1000, itemReward = "mythic_crate" }
            });
        }

        private void GenerateBounties()
        {
            currentBounties.Clear();

            // High-value targets
            currentBounties.Add(new Bounty
            {
                bountyId = "bounty_kill_streak_10",
                bountyName = "Unstoppable Force",
                description = "Achieve a 10 kill streak without dying",
                objective = "Kill streak",
                targetValue = 10,
                rewards = new ChallengeRewards { xp = 2500, currency = 500, itemReward = "legendary_weapon_skin" }
            });

            currentBounties.Add(new Bounty
            {
                bountyId = "bounty_perfect_round",
                bountyName = "Flawless Victory",
                description = "Complete a round without taking damage",
                objective = "Perfect round",
                targetValue = 1,
                rewards = new ChallengeRewards { xp = 3000, currency = 600, itemReward = "perfect_title" }
            });

            currentBounties.Add(new Bounty
            {
                bountyId = "bounty_solo_boss",
                bountyName = "Solo Boss Kill",
                description = "Defeat a boss zombie solo",
                objective = "Solo boss kill",
                targetValue = 1,
                rewards = new ChallengeRewards { xp = 4000, currency = 800, itemReward = "boss_slayer_title" }
            });

            currentBounties.Add(new Bounty
            {
                bountyId = "bounty_100_headshots_match",
                bountyName = "Precision Perfection",
                description = "Get 100 headshots in a single match",
                objective = "Headshots in match",
                targetValue = 100,
                rewards = new ChallengeRewards { xp = 3500, currency = 700, itemReward = "sniper_legendary_skin" }
            });

            currentBounties.Add(new Bounty
            {
                bountyId = "bounty_mvp_5",
                bountyName = "MVP Streak",
                description = "Be MVP in 5 consecutive matches",
                objective = "MVP streak",
                targetValue = 5,
                rewards = new ChallengeRewards { xp = 5000, currency = 1000, itemReward = "mvp_title" }
            });
        }

        /// <summary>
        /// Update challenge progress
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void UpdateChallengeProgressServerRpc(ulong playerId, string objective, float value, ServerRpcParams rpcParams = default)
        {
            if (!playerChallenges.ContainsKey(playerId))
            {
                InitializePlayerChallenges(playerId);
            }

            var playerData = playerChallenges[playerId];

            // Update matching challenges
            foreach (var challenge in currentWeeklyChallenges)
            {
                if (challenge.objective == objective)
                {
                    if (!playerData.challengeProgress.ContainsKey(challenge.challengeId))
                    {
                        playerData.challengeProgress[challenge.challengeId] = 0f;
                    }

                    playerData.challengeProgress[challenge.challengeId] += value;

                    OnChallengeProgressUpdated?.Invoke(playerId, challenge.challengeId);

                    // Check completion
                    if (playerData.challengeProgress[challenge.challengeId] >= challenge.targetValue &&
                        !playerData.completedChallenges.Contains(challenge.challengeId))
                    {
                        CompleteChallenge(playerId, challenge);
                    }
                }
            }

            // Update matching bounties
            foreach (var bounty in currentBounties)
            {
                if (bounty.objective == objective)
                {
                    if (!playerData.bountyProgress.ContainsKey(bounty.bountyId))
                    {
                        playerData.bountyProgress[bounty.bountyId] = 0f;
                    }

                    playerData.bountyProgress[bounty.bountyId] += value;

                    // Check completion
                    if (playerData.bountyProgress[bounty.bountyId] >= bounty.targetValue &&
                        !playerData.completedBounties.Contains(bounty.bountyId))
                    {
                        CompleteBounty(playerId, bounty);
                    }
                }
            }
        }

        private void InitializePlayerChallenges(ulong playerId)
        {
            playerChallenges[playerId] = new PlayerWeeklyChallenges
            {
                playerId = playerId,
                weekStartDate = weekStartDate,
                challengeProgress = new Dictionary<string, float>(),
                bountyProgress = new Dictionary<string, float>(),
                completedChallenges = new List<string>(),
                completedBounties = new List<string>()
            };
        }

        private void CompleteChallenge(ulong playerId, WeeklyChallenge challenge)
        {
            var playerData = playerChallenges[playerId];
            playerData.completedChallenges.Add(challenge.challengeId);

            // Award rewards
            AwardRewards(playerId, challenge.rewards);

            OnChallengeCompleted?.Invoke(playerId, challenge.challengeId);
            NotifyChallengeCompleteClientRpc(playerId, challenge.challengeName, challenge.rewards);

            // Check ultimate challenge
            CheckUltimateChallenge(playerId);
        }

        private void CompleteBounty(ulong playerId, Bounty bounty)
        {
            var playerData = playerChallenges[playerId];
            playerData.completedBounties.Add(bounty.bountyId);

            // Award rewards
            AwardRewards(playerId, bounty.rewards);

            OnBountyCompleted?.Invoke(playerId, bounty.bountyId);
            NotifyBountyCompleteClientRpc(playerId, bounty.bountyName, bounty.rewards);
        }

        private void CheckUltimateChallenge(ulong playerId)
        {
            var playerData = playerChallenges[playerId];

            // Count non-ultimate challenges completed
            int completedCount = playerData.completedChallenges.Count(id => !id.Contains("ultimate"));

            if (completedCount >= weeklyChallengesCount - 1)
            {
                var ultimate = currentWeeklyChallenges.Find(c => c.challengeId.Contains("ultimate"));
                if (ultimate != null && !playerData.completedChallenges.Contains(ultimate.challengeId))
                {
                    CompleteChallenge(playerId, ultimate);
                }
            }
        }

        private void AwardRewards(ulong playerId, ChallengeRewards rewards)
        {
            // Award XP
            if (rewards.xp > 0 && Progression.ProgressionSystem.Instance != null)
            {
                Progression.ProgressionSystem.Instance.AddExperienceServerRpc(playerId, rewards.xp);
            }

            // Award currency
            if (rewards.currency > 0)
            {
                // Economy system integration
            }

            // Award item
            if (!string.IsNullOrEmpty(rewards.itemReward) && Inventory.InventorySystem.Instance != null)
            {
                Inventory.InventorySystem.Instance.AddItemServerRpc(playerId, rewards.itemReward, 1);
            }
        }

        [ClientRpc]
        private void NotifyChallengeCompleteClientRpc(ulong playerId, string challengeName, ChallengeRewards rewards)
        {
            if (NetworkManager.Singleton.LocalClientId != playerId) return;

            Debug.Log($"<color=gold>★ WEEKLY CHALLENGE COMPLETE! ★</color>");
            Debug.Log($"<color=yellow>{challengeName}</color>");
            Debug.Log($"Rewards: {rewards.xp} XP, {rewards.currency} Currency");
            if (!string.IsNullOrEmpty(rewards.itemReward))
                Debug.Log($"Item: {rewards.itemReward}");
        }

        [ClientRpc]
        private void NotifyBountyCompleteClientRpc(ulong playerId, string bountyName, ChallengeRewards rewards)
        {
            if (NetworkManager.Singleton.LocalClientId != playerId) return;

            Debug.Log($"<color=red>★★ BOUNTY CLAIMED! ★★</color>");
            Debug.Log($"<color=orange>{bountyName}</color>");
            Debug.Log($"Rewards: {rewards.xp} XP, {rewards.currency} Currency");
            if (!string.IsNullOrEmpty(rewards.itemReward))
                Debug.Log($"Item: {rewards.itemReward}");
        }

        public List<WeeklyChallenge> GetWeeklyChallenges()
        {
            return new List<WeeklyChallenge>(currentWeeklyChallenges);
        }

        public List<Bounty> GetBounties()
        {
            return new List<Bounty>(currentBounties);
        }

        public float GetChallengeProgress(ulong playerId, string challengeId)
        {
            if (!playerChallenges.TryGetValue(playerId, out var data)) return 0f;
            return data.challengeProgress.TryGetValue(challengeId, out float progress) ? progress : 0f;
        }

        [Serializable]
        private class PlayerWeeklyChallenges
        {
            public ulong playerId;
            public DateTime weekStartDate;
            public Dictionary<string, float> challengeProgress;
            public Dictionary<string, float> bountyProgress;
            public List<string> completedChallenges;
            public List<string> completedBounties;
        }

        [Serializable]
        public class WeeklyChallenge
        {
            public string challengeId;
            public string challengeName;
            public string description;
            public string objective;
            public float targetValue;
            public ChallengeDifficulty difficulty;
            public ChallengeRewards rewards;
        }

        [Serializable]
        public class Bounty
        {
            public string bountyId;
            public string bountyName;
            public string description;
            public string objective;
            public float targetValue;
            public ChallengeRewards rewards;
        }

        [Serializable]
        public class ChallengeRewards
        {
            public int xp;
            public int currency;
            public string itemReward;
        }

        public enum ChallengeDifficulty
        {
            Easy,
            Medium,
            Hard,
            Ultimate
        }
    }
}
