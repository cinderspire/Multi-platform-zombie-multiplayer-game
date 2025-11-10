using System;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Combat
{
    public class DamageSystem : NetworkBehaviour
    {
        public static DamageSystem Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        [ServerRpc(RequireOwnership = false)]
        public void ProcessDamageServerRpc(ulong attackerId, ulong targetId, float damage, Health.DamageType damageType, bool isHeadshot, string weaponId, ServerRpcParams rpcParams = default)
        {
            var damageInfo = new Health.DamageInfo
            {
                attackerId = attackerId,
                baseDamage = damage,
                damageType = damageType,
                isHeadshot = isHeadshot,
                weaponId = weaponId,
                headshotMultiplier = 2f
            };

            Health.HealthSystem.Instance?.ApplyDamageServerRpc(targetId, damageInfo);
        }
    }
}
