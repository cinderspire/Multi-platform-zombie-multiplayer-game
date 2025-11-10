using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Player
{
    public class StaminaSystem : NetworkBehaviour
    {
        public static StaminaSystem Instance { get; private set; }

        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float staminaRegenRate = 10f;
        [SerializeField] private float regenDelay = 2f;

        private Dictionary<ulong, PlayerStamina> playerStamina = new Dictionary<ulong, PlayerStamina>();

        public event Action<ulong, float, float> OnStaminaChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Update()
        {
            if (IsServer) ProcessStaminaRegeneration();
        }

        [ServerRpc(RequireOwnership = false)]
        public void InitializePlayerStaminaServerRpc(ulong playerId, ServerRpcParams rpcParams = default)
        {
            if (!playerStamina.ContainsKey(playerId))
            {
                playerStamina[playerId] = new PlayerStamina
                {
                    playerId = playerId,
                    maxStamina = maxStamina,
                    currentStamina = maxStamina,
                    lastConsumeTime = Time.time
                };
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void ConsumeStaminaServerRpc(ulong playerId, float amount, ServerRpcParams rpcParams = default)
        {
            if (!playerStamina.TryGetValue(playerId, out var stamina)) return;

            stamina.currentStamina = Mathf.Max(0f, stamina.currentStamina - amount);
            stamina.lastConsumeTime = Time.time;
            OnStaminaChanged?.Invoke(playerId, stamina.currentStamina, stamina.maxStamina);
        }

        private void ProcessStaminaRegeneration()
        {
            foreach (var kvp in playerStamina)
            {
                var stamina = kvp.Value;
                if (stamina.currentStamina < stamina.maxStamina && Time.time - stamina.lastConsumeTime >= regenDelay)
                {
                    stamina.currentStamina = Mathf.Min(stamina.maxStamina, stamina.currentStamina + staminaRegenRate * Time.deltaTime);
                    OnStaminaChanged?.Invoke(stamina.playerId, stamina.currentStamina, stamina.maxStamina);
                }
            }
        }

        public bool HasStamina(ulong playerId, float amount)
        {
            return playerStamina.TryGetValue(playerId, out var stamina) && stamina.currentStamina >= amount;
        }
    }

    [Serializable]
    public class PlayerStamina
    {
        public ulong playerId;
        public float maxStamina;
        public float currentStamina;
        public float lastConsumeTime;
    }
}
