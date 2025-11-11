using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace ZombieGame
{
    /// <summary>
    /// Dynamic Difficulty Adjustment (DDA) - Adapts game difficulty to player skill
    /// Tracks performance metrics and adjusts damage, spawn rates, aim assist
    /// Ensures balanced challenge for all skill levels (casual to hardcore)
    /// </summary>
    public class DynamicDifficultySystem : NetworkBehaviour
    {
        public static DynamicDifficultySystem Instance { get; private set; }

        [Header("DDA Settings")]
        [SerializeField] private bool enableDDA = true;
        [SerializeField] private float adjustmentSpeed = 0.1f;
        [SerializeField] private float evaluationInterval = 30f; // Evaluate every 30 seconds

        [Header("Difficulty Range")]
        [SerializeField] private float minDifficultyMultiplier = 0.5f; // Easy
        [SerializeField] private float maxDifficultyMultiplier = 2.0f; // Hard

        [Header("Performance Targets")]
        [SerializeField] private float targetKDR = 2.0f; // Target 2:1 KD ratio
        [SerializeField] private float targetAccuracy = 0.35f; // 35% hit rate
        [SerializeField] private float targetSurvivalTime = 300f; // 5 min average

        // Player Performance Tracking
        private Dictionary<ulong, PlayerPerformanceData> playerPerformance = new Dictionary<ulong, PlayerPerformanceData>();
        private Dictionary<ulong, DifficultyProfile> playerDifficulty = new Dictionary<ulong, DifficultyProfile>();

        private float evaluationTimer = 0f;

        // Events
        public event System.Action<ulong, float> OnDifficultyAdjusted;

        [System.Serializable]
        public class PlayerPerformanceData
        {
            public int kills = 0;
            public int deaths = 0;
            public int shotsFired = 0;
            public int shotsHit = 0;
            public float totalSurvivalTime = 0f;
            public float currentLifeStartTime = 0f;
            public int damageDealt = 0;
            public int damageTaken = 0;
            public int headshotKills = 0;
            public float avgKillDistance = 0f;
            public float timeInCombat = 0f;
            public float lastEvaluationTime = 0f;
        }

        [System.Serializable]
        public class DifficultyProfile
        {
            public float damageMultiplier = 1.0f;      // Damage dealt by player
            public float resistanceMultiplier = 1.0f;  // Damage taken by player
            public float enemyHealthMultiplier = 1.0f; // Enemy health
            public float enemyDamageMultiplier = 1.0f; // Enemy damage
            public float aimAssistStrength = 0.3f;     // Aim assist (0-1)
            public float lootQualityBonus = 0f;        // Better loot
            public float xpMultiplier = 1.0f;          // XP gain
            public float currentDifficultyRating = 1.0f; // Overall rating
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
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
                InitializeDDA();
            }
        }

        private void InitializeDDA()
        {
            evaluationTimer = evaluationInterval;
        }

        private void Update()
        {
            if (!IsServer || !enableDDA) return;

            evaluationTimer -= Time.deltaTime;

            if (evaluationTimer <= 0f)
            {
                EvaluateAndAdjustDifficulty();
                evaluationTimer = evaluationInterval;
            }
        }

        private void EvaluateAndAdjustDifficulty()
        {
            foreach (var kvp in playerPerformance)
            {
                ulong playerId = kvp.Key;
                PlayerPerformanceData perf = kvp.Value;

                if (!playerDifficulty.ContainsKey(playerId))
                {
                    playerDifficulty[playerId] = new DifficultyProfile();
                }

                DifficultyProfile profile = playerDifficulty[playerId];

                // Calculate performance scores
                float kdScore = CalculateKDScore(perf);
                float accuracyScore = CalculateAccuracyScore(perf);
                float survivalScore = CalculateSurvivalScore(perf);

                // Overall performance (0-1 scale, 0.5 = target)
                float overallPerformance = (kdScore + accuracyScore + survivalScore) / 3f;

                // Adjust difficulty
                AdjustDifficulty(playerId, profile, overallPerformance);

                perf.lastEvaluationTime = Time.time;
            }
        }

        private float CalculateKDScore(PlayerPerformanceData perf)
        {
            if (perf.deaths == 0) return 1.0f; // Perfect performance

            float kdr = (float)perf.kills / perf.deaths;
            float kdScore = kdr / targetKDR;

            return Mathf.Clamp01(kdScore);
        }

        private float CalculateAccuracyScore(PlayerPerformanceData perf)
        {
            if (perf.shotsFired == 0) return 0.5f; // Neutral

            float accuracy = (float)perf.shotsHit / perf.shotsFired;
            float accScore = accuracy / targetAccuracy;

            return Mathf.Clamp01(accScore);
        }

        private float CalculateSurvivalScore(PlayerPerformanceData perf)
        {
            if (perf.totalSurvivalTime == 0f) return 0.5f;

            float avgSurvival = perf.totalSurvivalTime / Mathf.Max(1, perf.deaths + 1);
            float survivalScore = avgSurvival / targetSurvivalTime;

            return Mathf.Clamp01(survivalScore);
        }

        private void AdjustDifficulty(ulong playerId, DifficultyProfile profile, float performance)
        {
            float oldRating = profile.currentDifficultyRating;

            // Performing above target = increase difficulty
            // Performing below target = decrease difficulty
            float targetAdjustment = 0f;

            if (performance > 0.6f) // Performing well
            {
                targetAdjustment = adjustmentSpeed;
            }
            else if (performance < 0.4f) // Struggling
            {
                targetAdjustment = -adjustmentSpeed;
            }

            profile.currentDifficultyRating = Mathf.Clamp(
                profile.currentDifficultyRating + targetAdjustment,
                minDifficultyMultiplier,
                maxDifficultyMultiplier
            );

            // Apply difficulty rating to multipliers
            UpdateMultipliers(profile);

            if (Mathf.Abs(oldRating - profile.currentDifficultyRating) > 0.01f)
            {
                OnDifficultyAdjusted?.Invoke(playerId, profile.currentDifficultyRating);
                SyncDifficultyProfileClientRpc(playerId, profile.currentDifficultyRating);
            }
        }

        private void UpdateMultipliers(DifficultyProfile profile)
        {
            float rating = profile.currentDifficultyRating;

            // Player dealing more damage = harder
            profile.damageMultiplier = Mathf.Lerp(1.2f, 0.8f, (rating - 0.5f) * 2f);

            // Player taking less damage = harder
            profile.resistanceMultiplier = Mathf.Lerp(1.2f, 0.8f, (rating - 0.5f) * 2f);

            // Enemies have more health = harder
            profile.enemyHealthMultiplier = Mathf.Lerp(0.7f, 1.5f, (rating - 0.5f) * 2f);

            // Enemies deal more damage = harder
            profile.enemyDamageMultiplier = Mathf.Lerp(0.7f, 1.3f, (rating - 0.5f) * 2f);

            // Aim assist (more for struggling players)
            profile.aimAssistStrength = Mathf.Lerp(0.5f, 0.1f, (rating - 0.5f) * 2f);

            // Loot quality bonus (better for struggling players)
            profile.lootQualityBonus = Mathf.Lerp(0.3f, -0.1f, (rating - 0.5f) * 2f);

            // XP multiplier (encourage struggling players)
            profile.xpMultiplier = Mathf.Lerp(1.2f, 0.9f, (rating - 0.5f) * 2f);
        }

        [ClientRpc]
        private void SyncDifficultyProfileClientRpc(ulong playerId, float difficultyRating)
        {
            Debug.Log($"[DDA] Player {playerId} difficulty adjusted to: {difficultyRating:F2}");
        }

        // Public API - Called by other systems

        [ServerRpc(RequireOwnership = false)]
        public void RegisterKillServerRpc(ulong playerId, bool isHeadshot, float distance, ServerRpcParams rpcParams = default)
        {
            if (!playerPerformance.ContainsKey(playerId))
            {
                InitializePlayer(playerId);
            }

            var perf = playerPerformance[playerId];
            perf.kills++;

            if (isHeadshot)
            {
                perf.headshotKills++;
            }

            // Update average kill distance
            perf.avgKillDistance = (perf.avgKillDistance * (perf.kills - 1) + distance) / perf.kills;
        }

        [ServerRpc(RequireOwnership = false)]
        public void RegisterDeathServerRpc(ulong playerId, ServerRpcParams rpcParams = default)
        {
            if (!playerPerformance.ContainsKey(playerId))
            {
                InitializePlayer(playerId);
            }

            var perf = playerPerformance[playerId];
            perf.deaths++;

            // Calculate survival time for this life
            if (perf.currentLifeStartTime > 0f)
            {
                float survivalTime = Time.time - perf.currentLifeStartTime;
                perf.totalSurvivalTime += survivalTime;
            }

            // Reset life timer
            perf.currentLifeStartTime = Time.time;
        }

        [ServerRpc(RequireOwnership = false)]
        public void RegisterShotServerRpc(ulong playerId, bool hit, ServerRpcParams rpcParams = default)
        {
            if (!playerPerformance.ContainsKey(playerId))
            {
                InitializePlayer(playerId);
            }

            var perf = playerPerformance[playerId];
            perf.shotsFired++;

            if (hit)
            {
                perf.shotsHit++;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void RegisterDamageDealtServerRpc(ulong playerId, int damage, ServerRpcParams rpcParams = default)
        {
            if (!playerPerformance.ContainsKey(playerId))
            {
                InitializePlayer(playerId);
            }

            playerPerformance[playerId].damageDealt += damage;
        }

        [ServerRpc(RequireOwnership = false)]
        public void RegisterDamageTakenServerRpc(ulong playerId, int damage, ServerRpcParams rpcParams = default)
        {
            if (!playerPerformance.ContainsKey(playerId))
            {
                InitializePlayer(playerId);
            }

            playerPerformance[playerId].damageTaken += damage;
        }

        private void InitializePlayer(ulong playerId)
        {
            playerPerformance[playerId] = new PlayerPerformanceData
            {
                currentLifeStartTime = Time.time
            };
            playerDifficulty[playerId] = new DifficultyProfile();
        }

        // Getters for other systems

        public DifficultyProfile GetPlayerDifficulty(ulong playerId)
        {
            return playerDifficulty.ContainsKey(playerId) ? playerDifficulty[playerId] : new DifficultyProfile();
        }

        public float GetDamageMultiplier(ulong playerId)
        {
            var profile = GetPlayerDifficulty(playerId);
            return profile.damageMultiplier;
        }

        public float GetResistanceMultiplier(ulong playerId)
        {
            var profile = GetPlayerDifficulty(playerId);
            return profile.resistanceMultiplier;
        }

        public float GetEnemyHealthMultiplier(ulong playerId)
        {
            var profile = GetPlayerDifficulty(playerId);
            return profile.enemyHealthMultiplier;
        }

        public float GetEnemyDamageMultiplier(ulong playerId)
        {
            var profile = GetPlayerDifficulty(playerId);
            return profile.enemyDamageMultiplier;
        }

        public float GetAimAssistStrength(ulong playerId)
        {
            var profile = GetPlayerDifficulty(playerId);
            return profile.aimAssistStrength;
        }

        public float GetLootQualityBonus(ulong playerId)
        {
            var profile = GetPlayerDifficulty(playerId);
            return profile.lootQualityBonus;
        }

        public float GetXPMultiplier(ulong playerId)
        {
            var profile = GetPlayerDifficulty(playerId);
            return profile.xpMultiplier;
        }

        public PlayerPerformanceData GetPlayerPerformance(ulong playerId)
        {
            return playerPerformance.ContainsKey(playerId) ? playerPerformance[playerId] : null;
        }

        // Enable/Disable DDA
        public void SetDDAEnabled(bool enabled)
        {
            enableDDA = enabled;
        }

        public bool IsDDAEnabled()
        {
            return enableDDA;
        }
    }
}
