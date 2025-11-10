using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Combat
{
    public class CombatSystem : NetworkBehaviour
    {
        public static CombatSystem Instance { get; private set; }

        [Header("Melee Settings")]
        [SerializeField] private float meleeRange = 2f;
        [SerializeField] private float meleeAngle = 90f;
        [SerializeField] private LayerMask meleeTargetMask;

        [Header("Ranged Settings")]
        [SerializeField] private LayerMask rangedTargetMask;
        [SerializeField] private float maxRaycastDistance = 500f;

        private Dictionary<ulong, CombatState> playerCombatStates = new Dictionary<ulong, CombatState>();

        public event Action<ulong, ulong, float> OnMeleeHit;
        public event Action<ulong, ulong, float, bool> OnRangedHit;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        [ServerRpc(RequireOwnership = false)]
        public void PerformMeleeAttackServerRpc(ulong attackerId, Vector3 attackOrigin, Vector3 attackDirection, float damage, ServerRpcParams rpcParams = default)
        {
            Collider[] hits = Physics.OverlapSphere(attackOrigin, meleeRange, meleeTargetMask);
            
            foreach (var hit in hits)
            {
                if (hit.TryGetComponent<NetworkObject>(out var netObj))
                {
                    ulong targetId = netObj.OwnerClientId;
                    if (targetId == attackerId) continue;

                    Vector3 directionToTarget = (hit.transform.position - attackOrigin).normalized;
                    float angle = Vector3.Angle(attackDirection, directionToTarget);
                    
                    if (angle <= meleeAngle / 2f)
                    {
                        DamageSystem.Instance?.ProcessDamageServerRpc(attackerId, targetId, damage, Health.DamageType.Physical, false, "melee");
                        OnMeleeHit?.Invoke(attackerId, targetId, damage);
                    }
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void PerformRangedAttackServerRpc(ulong attackerId, Vector3 origin, Vector3 direction, float damage, string weaponId, ServerRpcParams rpcParams = default)
        {
            if (Physics.Raycast(origin, direction, out RaycastHit hit, maxRaycastDistance, rangedTargetMask))
            {
                if (hit.collider.TryGetComponent<NetworkObject>(out var netObj))
                {
                    ulong targetId = netObj.OwnerClientId;
                    if (targetId == attackerId) return;

                    bool isHeadshot = hit.collider.CompareTag("Head");
                    DamageSystem.Instance?.ProcessDamageServerRpc(attackerId, targetId, damage, Health.DamageType.Physical, isHeadshot, weaponId);
                    OnRangedHit?.Invoke(attackerId, targetId, damage, isHeadshot);
                }
            }
        }
    }

    [Serializable]
    public class CombatState
    {
        public ulong playerId;
        public bool isAttacking;
        public float lastAttackTime;
    }
}
