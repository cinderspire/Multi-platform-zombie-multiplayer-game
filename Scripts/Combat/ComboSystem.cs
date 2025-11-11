using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Combat
{
    /// <summary>
    /// Combo system tracking consecutive kills and actions, providing multipliers
    /// and special rewards for maintaining combat flow.
    /// </summary>
    public class ComboSystem : NetworkBehaviour
    {
        public static ComboSystem Instance { get; private set; }

        [Header("Combo Configuration")]
        [SerializeField] private float comboTimeWindow = 4f;
        [SerializeField] private int maxComboCount = 99;
        [SerializeField] private bool enableComboMultipliers = true;

        [Header("Combo Tiers")]
        [SerializeField] private ComboTier[] comboTiers = new ComboTier[]
        {
            new ComboTier { killCount = 3, tierName = "Double Kill", xpMultiplier = 1.2f, scoreMultiplier = 1.2f },
            new ComboTier { killCount = 5, tierName = "Triple Kill", xpMultiplier = 1.5f, scoreMultiplier = 1.5f },
            new ComboTier { killCount = 7, tierName = "Mega Kill", xpMultiplier = 2f, scoreMultiplier = 2f },
            new ComboTier { killCount = 10, tierName = "Ultra Kill", xpMultiplier = 2.5f, scoreMultiplier = 2.5f },
            new ComboTier { killCount = 15, tierName = "Monster Kill", xpMultiplier = 3f, scoreMultiplier = 3f },
            new ComboTier { killCount = 20, tierName = "Godlike", xpMultiplier = 4f, scoreMultiplier = 4f },
            new ComboTier { killCount = 30, tierName = "Unstoppable", xpMultiplier = 5f, scoreMultiplier = 5f }
        };

        [Header("Bonuses")]
        [SerializeField] private int bonusXPPerKill = 10;
        [SerializeField] private int bonusScorePerKill = 50;

        private Dictionary<ulong, PlayerCombo> playerCombos = new Dictionary<ulong, PlayerCombo>();

        public event Action<ulong, int, string> OnComboTierReached; // playerId, killCount, tierName
        public event Action<ulong, int> OnComboIncreased;
        public event Action<ulong, int> OnComboEnded;
        public event Action<ulong, int, int, int> OnComboBonusAwarded; // playerId, comboCount, bonusXP, bonusScore

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Update()
        {
            if (IsServer)
            {
                CheckComboTimeouts();
            }
        }

        /// <summary>
        /// Register a kill for combo tracking
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void RegisterKillServerRpc(ulong playerId, ulong victimId, ServerRpcParams rpcParams = default)
        {
            if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(playerId)) return;

            if (!playerCombos.ContainsKey(playerId))
            {
                playerCombos[playerId] = new PlayerCombo { playerId = playerId };
            }

            var combo = playerCombos[playerId];
            combo.killCount = Mathf.Min(combo.killCount + 1, maxComboCount);
            combo.lastKillTime = Time.time;

            // Check for tier achievement
            ComboTier tier = GetCurrentTier(combo.killCount);
            if (tier != null && combo.killCount == tier.killCount)
            {
                OnComboTierReached?.Invoke(playerId, combo.killCount, tier.tierName);
                NotifyComboTierClientRpc(playerId, combo.killCount, tier.tierName);

                // Award bonus
                int bonusXP = Mathf.RoundToInt(bonusXPPerKill * tier.xpMultiplier * combo.killCount);
                int bonusScore = Mathf.RoundToInt(bonusScorePerKill * tier.scoreMultiplier * combo.killCount);

                AwardComboBonus(playerId, combo.killCount, bonusXP, bonusScore);
            }

            OnComboIncreased?.Invoke(playerId, combo.killCount);
            UpdateComboClientRpc(playerId, combo.killCount);
        }

        /// <summary>
        /// Register an action (headshot, melee kill, etc.) for combo
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void RegisterActionServerRpc(ulong playerId, ComboActionType actionType, ServerRpcParams rpcParams = default)
        {
            if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(playerId)) return;

            if (!playerCombos.ContainsKey(playerId))
            {
                playerCombos[playerId] = new PlayerCombo { playerId = playerId };
            }

            var combo = playerCombos[playerId];
            combo.lastKillTime = Time.time; // Extend combo window

            // Increment action-specific counters
            switch (actionType)
            {
                case ComboActionType.Headshot:
                    combo.consecutiveHeadshots++;
                    break;
                case ComboActionType.MeleeKill:
                    combo.consecutiveMeleeKills++;
                    break;
                case ComboActionType.NoScopeKill:
                    combo.consecutiveNoScopes++;
                    break;
                case ComboActionType.AirborneKill:
                    combo.consecutiveAirborneKills++;
                    break;
            }
        }

        private void CheckComboTimeouts()
        {
            List<ulong> playersToReset = new List<ulong>();

            foreach (var kvp in playerCombos)
            {
                var combo = kvp.Value;
                if (Time.time - combo.lastKillTime > comboTimeWindow)
                {
                    if (combo.killCount > 0)
                    {
                        playersToReset.Add(kvp.Key);
                    }
                }
            }

            foreach (ulong playerId in playersToReset)
            {
                EndCombo(playerId);
            }
        }

        private void EndCombo(ulong playerId)
        {
            if (!playerCombos.ContainsKey(playerId)) return;

            var combo = playerCombos[playerId];
            int finalCount = combo.killCount;

            OnComboEnded?.Invoke(playerId, finalCount);
            NotifyComboEndedClientRpc(playerId, finalCount);

            // Reset combo
            combo.killCount = 0;
            combo.consecutiveHeadshots = 0;
            combo.consecutiveMeleeKills = 0;
            combo.consecutiveNoScopes = 0;
            combo.consecutiveAirborneKills = 0;
        }

        private void AwardComboBonus(ulong playerId, int comboCount, int bonusXP, int bonusScore)
        {
            // Award XP
            if (Progression.ProgressionSystem.Instance != null)
            {
                Progression.ProgressionSystem.Instance.AddExperienceServerRpc(playerId, bonusXP);
            }

            // Award Score (if score system exists)
            // Economy.EconomyManager.Instance?.AddCurrency(playerId, ...)

            OnComboBonusAwarded?.Invoke(playerId, comboCount, bonusXP, bonusScore);
        }

        private ComboTier GetCurrentTier(int killCount)
        {
            ComboTier currentTier = null;
            foreach (var tier in comboTiers)
            {
                if (killCount >= tier.killCount)
                {
                    currentTier = tier;
                }
            }
            return currentTier;
        }

        public float GetComboMultiplier(ulong playerId)
        {
            if (!enableComboMultipliers) return 1f;
            if (!playerCombos.ContainsKey(playerId)) return 1f;

            var combo = playerCombos[playerId];
            ComboTier tier = GetCurrentTier(combo.killCount);
            return tier?.xpMultiplier ?? 1f;
        }

        public int GetCurrentCombo(ulong playerId)
        {
            return playerCombos.ContainsKey(playerId) ? playerCombos[playerId].killCount : 0;
        }

        [ClientRpc]
        private void UpdateComboClientRpc(ulong playerId, int killCount)
        {
            // UI can subscribe to events to update combo display
        }

        [ClientRpc]
        private void NotifyComboTierClientRpc(ulong playerId, int killCount, string tierName)
        {
            // Show combo tier achievement UI
            if (NetworkManager.Singleton.LocalClientId == playerId)
            {
                Debug.Log($"<color=orange>COMBO: {tierName}! ({killCount} kills)</color>");
            }
        }

        [ClientRpc]
        private void NotifyComboEndedClientRpc(ulong playerId, int finalCount)
        {
            if (NetworkManager.Singleton.LocalClientId == playerId && finalCount > 0)
            {
                Debug.Log($"<color=yellow>Combo Ended: {finalCount} kills</color>");
            }
        }

        [Serializable]
        private class PlayerCombo
        {
            public ulong playerId;
            public int killCount;
            public float lastKillTime;
            public int consecutiveHeadshots;
            public int consecutiveMeleeKills;
            public int consecutiveNoScopes;
            public int consecutiveAirborneKills;
        }

        [Serializable]
        public class ComboTier
        {
            public int killCount;
            public string tierName;
            public float xpMultiplier;
            public float scoreMultiplier;
        }

        public enum ComboActionType
        {
            Headshot,
            MeleeKill,
            NoScopeKill,
            AirborneKill,
            QuickScope,
            Longshot
        }
    }
}
