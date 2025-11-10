using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Abilities
{
    public class AbilitySystem : NetworkBehaviour
    {
        public static AbilitySystem Instance { get; private set; }

        [SerializeField] private List<AbilityData> availableAbilities = new List<AbilityData>();

        private Dictionary<ulong, List<PlayerAbility>> playerAbilities = new Dictionary<ulong, List<PlayerAbility>>();

        public event Action<ulong, string> OnAbilityActivated;
        public event Action<ulong, string> OnAbilityCooldownComplete;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Update()
        {
            if (IsServer) UpdateCooldowns();
        }

        [ServerRpc(RequireOwnership = false)]
        public void ActivateAbilityServerRpc(ulong playerId, string abilityId, ServerRpcParams rpcParams = default)
        {
            if (!playerAbilities.ContainsKey(playerId)) return;

            var ability = playerAbilities[playerId].Find(a => a.abilityId == abilityId);
            if (ability == null || !CanUseAbility(playerId, ability)) return;

            // Execute ability effect
            ExecuteAbility(playerId, ability);

            // Start cooldown
            ability.currentCooldown = ability.cooldownTime;
            ability.lastUsedTime = Time.time;

            OnAbilityActivated?.Invoke(playerId, abilityId);
            ActivateAbilityClientRpc(playerId, abilityId);
        }

        private void ExecuteAbility(ulong playerId, PlayerAbility ability)
        {
            switch (ability.abilityType)
            {
                case AbilityType.Heal:
                    Health.HealthSystem.Instance?.HealEntityServerRpc(playerId, 50f, Health.HealType.Ability);
                    break;

                case AbilityType.Speed:
                    ApplySpeedBoost(playerId, 2f, 5f);
                    break;

                case AbilityType.Damage:
                    ApplyDamageBoost(playerId, 1.5f, 10f);
                    break;

                case AbilityType.Shield:
                    Health.HealthSystem.Instance?.RestoreShieldServerRpc(playerId, 50f);
                    break;

                case AbilityType.Teleport:
                    TeleportPlayer(playerId, 10f);
                    break;

                case AbilityType.Invisibility:
                    ApplyInvisibility(playerId, 8f);
                    break;
            }
        }

        private void ApplySpeedBoost(ulong playerId, float multiplier, float duration)
        {
            // Would integrate with PlayerController
        }

        private void ApplyDamageBoost(ulong playerId, float multiplier, float duration)
        {
            // Would integrate with CombatSystem
        }

        private void TeleportPlayer(ulong playerId, float distance)
        {
            // Would teleport player forward
        }

        private void ApplyInvisibility(ulong playerId, float duration)
        {
            // Would make player invisible to zombies
        }

        private bool CanUseAbility(ulong playerId, PlayerAbility ability)
        {
            return ability.currentCooldown <= 0f;
        }

        private void UpdateCooldowns()
        {
            foreach (var kvp in playerAbilities)
            {
                foreach (var ability in kvp.Value)
                {
                    if (ability.currentCooldown > 0f)
                    {
                        ability.currentCooldown -= Time.deltaTime;
                        if (ability.currentCooldown <= 0f)
                        {
                            ability.currentCooldown = 0f;
                            OnAbilityCooldownComplete?.Invoke(kvp.Key, ability.abilityId);
                        }
                    }
                }
            }
        }

        [ClientRpc]
        private void ActivateAbilityClientRpc(ulong playerId, string abilityId) { }

        public List<PlayerAbility> GetPlayerAbilities(ulong playerId)
        {
            return playerAbilities.GetValueOrDefault(playerId, new List<PlayerAbility>());
        }
    }

    [Serializable]
    public class AbilityData
    {
        public string abilityId;
        public string abilityName;
        public AbilityType abilityType;
        public float cooldownTime;
        public float duration;
    }

    [Serializable]
    public class PlayerAbility
    {
        public string abilityId;
        public AbilityType abilityType;
        public float cooldownTime;
        public float currentCooldown;
        public float lastUsedTime;
    }

    public enum AbilityType { Heal, Speed, Damage, Shield, Teleport, Invisibility, Stun, Revive }
}
