using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Challenges
{
    public class ChallengeSystem : NetworkBehaviour
    {
        public static ChallengeSystem Instance { get; private set; }

        [SerializeField] private List<ChallengeData> dailyChallenges = new List<ChallengeData>();
        [SerializeField] private List<ChallengeData> weeklyChallenges = new List<ChallengeData>();

        private Dictionary<ulong, PlayerChallenges> playerChallenges = new Dictionary<ulong, PlayerChallenges>();

        public event Action<ulong, string> OnChallengeCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        [ServerRpc(RequireOwnership = false)]
        public void UpdateChallengeProgressServerRpc(ulong playerId, string challengeId, int progress, ServerRpcParams rpcParams = default)
        {
            if (!playerChallenges.ContainsKey(playerId))
            {
                playerChallenges[playerId] = new PlayerChallenges { playerId = playerId };
            }

            var playerChallenge = playerChallenges[playerId].challenges.Find(c => c.challengeId == challengeId);
            if (playerChallenge == null) return;

            playerChallenge.currentProgress = progress;
            
            if (playerChallenge.currentProgress >= playerChallenge.targetProgress && !playerChallenge.isCompleted)
            {
                playerChallenge.isCompleted = true;
                OnChallengeCompleted?.Invoke(playerId, challengeId);
                RewardPlayer(playerId, playerChallenge.reward);
            }
        }

        private void RewardPlayer(ulong playerId, ChallengeReward reward)
        {
            Economy.EconomyManager.Instance?.AddCurrencyServerRpc(playerId, "Gold", reward.goldAmount);
        }
    }

    [Serializable]
    public class PlayerChallenges
    {
        public ulong playerId;
        public List<PlayerChallenge> challenges = new List<PlayerChallenge>();
    }

    [Serializable]
    public class PlayerChallenge
    {
        public string challengeId;
        public int currentProgress;
        public int targetProgress;
        public bool isCompleted;
        public ChallengeReward reward;
    }

    [Serializable]
    public class ChallengeData
    {
        public string challengeId;
        public string challengeName;
        public string description;
        public ChallengeType type;
        public int targetProgress;
        public ChallengeReward reward;
    }

    [Serializable]
    public class ChallengeReward
    {
        public int goldAmount;
        public int xpAmount;
        public List<string> itemRewards = new List<string>();
    }

    public enum ChallengeType { KillZombies, SurviveTime, CompleteQuests, EarnCurrency, WinMatches }
}
