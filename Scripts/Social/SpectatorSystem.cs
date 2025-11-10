using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Social
{
    public class SpectatorSystem : NetworkBehaviour
    {
        public static SpectatorSystem Instance { get; private set; }

        [SerializeField] private float switchCooldown = 1f;

        private Dictionary<ulong, SpectatorState> spectators = new Dictionary<ulong, SpectatorState>();
        private Dictionary<ulong, float> lastSwitchTime = new Dictionary<ulong, float>();

        public event Action<ulong, ulong> OnSpectatorTargetChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        [ServerRpc(RequireOwnership = false)]
        public void EnterSpectatorModeServerRpc(ulong playerId, ServerRpcParams rpcParams = default)
        {
            if (!spectators.ContainsKey(playerId))
            {
                spectators[playerId] = new SpectatorState
                {
                    spectatorId = playerId,
                    isSpectating = true,
                    targetPlayerId = GetNextValidTarget(playerId)
                };

                EnterSpectatorModeClientRpc(playerId);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void ExitSpectatorModeServerRpc(ulong playerId, ServerRpcParams rpcParams = default)
        {
            if (spectators.ContainsKey(playerId))
            {
                spectators.Remove(playerId);
                ExitSpectatorModeClientRpc(playerId);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void SwitchSpectatorTargetServerRpc(ulong spectatorId, bool next, ServerRpcParams rpcParams = default)
        {
            if (!spectators.ContainsKey(spectatorId)) return;

            // Check cooldown
            if (lastSwitchTime.TryGetValue(spectatorId, out float lastTime))
            {
                if (Time.time - lastTime < switchCooldown) return;
            }

            ulong newTarget = next ? GetNextValidTarget(spectatorId) : GetPreviousValidTarget(spectatorId);
            spectators[spectatorId].targetPlayerId = newTarget;
            lastSwitchTime[spectatorId] = Time.time;

            OnSpectatorTargetChanged?.Invoke(spectatorId, newTarget);
            SwitchTargetClientRpc(spectatorId, newTarget);
        }

        private ulong GetNextValidTarget(ulong spectatorId)
        {
            var alivePlayers = GetAlivePlayers(spectatorId);
            if (alivePlayers.Count == 0) return 0;

            if (!spectators.TryGetValue(spectatorId, out var state))
            {
                return alivePlayers[0];
            }

            int currentIndex = alivePlayers.IndexOf(state.targetPlayerId);
            int nextIndex = (currentIndex + 1) % alivePlayers.Count;
            return alivePlayers[nextIndex];
        }

        private ulong GetPreviousValidTarget(ulong spectatorId)
        {
            var alivePlayers = GetAlivePlayers(spectatorId);
            if (alivePlayers.Count == 0) return 0;

            if (!spectators.TryGetValue(spectatorId, out var state))
            {
                return alivePlayers[alivePlayers.Count - 1];
            }

            int currentIndex = alivePlayers.IndexOf(state.targetPlayerId);
            int prevIndex = currentIndex - 1;
            if (prevIndex < 0) prevIndex = alivePlayers.Count - 1;
            return alivePlayers[prevIndex];
        }

        private List<ulong> GetAlivePlayers(ulong excluding)
        {
            return NetworkManager.Singleton.ConnectedClients.Keys
                .Where(id => id != excluding && Health.HealthSystem.Instance?.IsEntityAlive(id) == true)
                .ToList();
        }

        [ClientRpc]
        private void EnterSpectatorModeClientRpc(ulong playerId) { }

        [ClientRpc]
        private void ExitSpectatorModeClientRpc(ulong playerId) { }

        [ClientRpc]
        private void SwitchTargetClientRpc(ulong spectatorId, ulong targetId) { }

        public bool IsSpectating(ulong playerId) => spectators.ContainsKey(playerId);
        public ulong GetSpectatorTarget(ulong spectatorId)
        {
            return spectators.TryGetValue(spectatorId, out var state) ? state.targetPlayerId : 0;
        }
    }

    [Serializable]
    public class SpectatorState
    {
        public ulong spectatorId;
        public bool isSpectating;
        public ulong targetPlayerId;
    }
}
