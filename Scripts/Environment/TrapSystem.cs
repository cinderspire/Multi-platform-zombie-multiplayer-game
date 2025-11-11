using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Environment
{
    /// <summary>
    /// Environmental trap system for placeable and world traps that damage
    /// or hinder zombies and players.
    /// </summary>
    public class TrapSystem : NetworkBehaviour
    {
        public static TrapSystem Instance { get; private set; }

        [Header("Trap Configuration")]
        [SerializeField] private float trapCheckInterval = 0.5f;
        [SerializeField] private float trapTriggerRadius = 2f;

        private Dictionary<string, Trap> activeTraps = new Dictionary<string, Trap>();
        private Dictionary<ulong, List<string>> playerTraps = new Dictionary<ulong, List<string>>();

        public event Action<string, TrapType, Vector3> OnTrapPlaced;
        public event Action<string, ulong> OnTrapTriggered;
        public event Action<string> OnTrapDestroyed;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer)
            {
                StartCoroutine(TrapUpdateLoop());
            }
        }

        private IEnumerator TrapUpdateLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(trapCheckInterval);
                CheckAllTraps();
            }
        }

        /// <summary>
        /// Place a new trap in the world
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void PlaceTrapServerRpc(ulong playerId, Vector3 position, TrapType trapType, ServerRpcParams rpcParams = default)
        {
            string trapId = Guid.NewGuid().ToString();

            Trap trap = new Trap
            {
                trapId = trapId,
                ownerId = playerId,
                position = position,
                trapType = trapType,
                placedTime = Time.time,
                isTriggered = false,
                isActive = true
            };

            activeTraps[trapId] = trap;

            if (!playerTraps.ContainsKey(playerId))
            {
                playerTraps[playerId] = new List<string>();
            }
            playerTraps[playerId].Add(trapId);

            OnTrapPlaced?.Invoke(trapId, trapType, position);
            SpawnTrapClientRpc(trapId, position, trapType);
        }

        [ClientRpc]
        private void SpawnTrapClientRpc(string trapId, Vector3 position, TrapType trapType)
        {
            // Instantiate trap visual
            GameObject trapPrefab = GetTrapPrefab(trapType);
            if (trapPrefab != null)
            {
                GameObject trapObj = Instantiate(trapPrefab, position, Quaternion.identity);
                trapObj.name = trapId;
            }
        }

        private void CheckAllTraps()
        {
            foreach (var kvp in activeTraps)
            {
                Trap trap = kvp.Value;
                if (!trap.isActive || trap.isTriggered) continue;

                CheckTrapTriggers(trap);
                CheckTrapExpiry(trap);
            }
        }

        private void CheckTrapTriggers(Trap trap)
        {
            // Check for entities in trigger radius
            Collider[] hits = Physics.OverlapSphere(trap.position, trapTriggerRadius);

            foreach (Collider hit in hits)
            {
                // Check if zombie or player
                var networkObject = hit.GetComponent<NetworkObject>();
                if (networkObject != null)
                {
                    TriggerTrap(trap.trapId, networkObject.OwnerClientId);
                    break;
                }
            }
        }

        private void CheckTrapExpiry(Trap trap)
        {
            float lifetime = GetTrapLifetime(trap.trapType);
            if (lifetime > 0 && Time.time - trap.placedTime > lifetime)
            {
                DestroyTrap(trap.trapId, false);
            }
        }

        private void TriggerTrap(string trapId, ulong triggeredBy)
        {
            if (!activeTraps.TryGetValue(trapId, out Trap trap)) return;
            if (trap.isTriggered) return;

            trap.isTriggered = true;
            OnTrapTriggered?.Invoke(trapId, triggeredBy);

            ApplyTrapEffect(trap, triggeredBy);

            // Visual/audio effects
            TriggerTrapEffectsClientRpc(trapId, trap.position, trap.trapType);

            // Destroy trap after effect
            float destroyDelay = GetTrapDestroyDelay(trap.trapType);
            StartCoroutine(DestroyTrapAfterDelay(trapId, destroyDelay));
        }

        private void ApplyTrapEffect(Trap trap, ulong targetId)
        {
            switch (trap.trapType)
            {
                case TrapType.Spike:
                    // Deal damage
                    if (Health.HealthSystem.Instance != null)
                    {
                        Health.HealthSystem.Instance.DamageEntityServerRpc(targetId, 30f, trap.ownerId, "Spike Trap");
                    }
                    break;

                case TrapType.Bear:
                    // Damage + slow
                    if (Health.HealthSystem.Instance != null)
                    {
                        Health.HealthSystem.Instance.DamageEntityServerRpc(targetId, 50f, trap.ownerId, "Bear Trap");
                    }
                    // Apply slow effect (would integrate with status effect system)
                    break;

                case TrapType.Explosive:
                    // AOE damage
                    Collider[] hits = Physics.OverlapSphere(trap.position, 5f);
                    foreach (Collider hit in hits)
                    {
                        var networkObject = hit.GetComponent<NetworkObject>();
                        if (networkObject != null && Health.HealthSystem.Instance != null)
                        {
                            float distance = Vector3.Distance(trap.position, hit.transform.position);
                            float damage = Mathf.Lerp(100f, 30f, distance / 5f);
                            Health.HealthSystem.Instance.DamageEntityServerRpc(
                                networkObject.OwnerClientId, damage, trap.ownerId, "Explosive Trap");
                        }
                    }
                    break;

                case TrapType.Electric:
                    // Damage + stun
                    if (Health.HealthSystem.Instance != null)
                    {
                        Health.HealthSystem.Instance.DamageEntityServerRpc(targetId, 40f, trap.ownerId, "Electric Trap");
                    }
                    break;

                case TrapType.Gas:
                    // DOT damage
                    StartCoroutine(ApplyDOT(targetId, trap.ownerId, 5f, 10, 0.5f));
                    break;

                case TrapType.Freeze:
                    // Freeze/slow effect
                    // Would integrate with movement system
                    break;

                case TrapType.Fire:
                    // Fire damage over time
                    StartCoroutine(ApplyDOT(targetId, trap.ownerId, 15f, 5, 1f));
                    break;
            }
        }

        private IEnumerator ApplyDOT(ulong targetId, ulong sourceId, float damagePerTick, int ticks, float interval)
        {
            for (int i = 0; i < ticks; i++)
            {
                if (Health.HealthSystem.Instance != null)
                {
                    Health.HealthSystem.Instance.DamageEntityServerRpc(targetId, damagePerTick, sourceId, "DOT");
                }
                yield return new WaitForSeconds(interval);
            }
        }

        [ClientRpc]
        private void TriggerTrapEffectsClientRpc(string trapId, Vector3 position, TrapType trapType)
        {
            // Play visual effects
            // Play sound effects
            Debug.Log($"Trap {trapType} triggered at {position}");
        }

        private IEnumerator DestroyTrapAfterDelay(string trapId, float delay)
        {
            yield return new WaitForSeconds(delay);
            DestroyTrap(trapId, true);
        }

        private void DestroyTrap(string trapId, bool triggered)
        {
            if (!activeTraps.TryGetValue(trapId, out Trap trap)) return;

            activeTraps.Remove(trapId);

            if (playerTraps.ContainsKey(trap.ownerId))
            {
                playerTraps[trap.ownerId].Remove(trapId);
            }

            OnTrapDestroyed?.Invoke(trapId);
            DestroyTrapClientRpc(trapId);
        }

        [ClientRpc]
        private void DestroyTrapClientRpc(string trapId)
        {
            GameObject trapObj = GameObject.Find(trapId);
            if (trapObj != null)
            {
                Destroy(trapObj);
            }
        }

        private float GetTrapLifetime(TrapType type)
        {
            return type switch
            {
                TrapType.Spike => 60f,
                TrapType.Bear => 120f,
                TrapType.Explosive => 0f, // No expiry
                TrapType.Electric => 90f,
                TrapType.Gas => 45f,
                TrapType.Freeze => 60f,
                TrapType.Fire => 30f,
                _ => 60f
            };
        }

        private float GetTrapDestroyDelay(TrapType type)
        {
            return type switch
            {
                TrapType.Explosive => 0.5f,
                TrapType.Gas => 10f,
                TrapType.Fire => 5f,
                _ => 0.1f
            };
        }

        private GameObject GetTrapPrefab(TrapType type)
        {
            // Would load from resources
            return null;
        }

        public int GetPlayerTrapCount(ulong playerId)
        {
            return playerTraps.ContainsKey(playerId) ? playerTraps[playerId].Count : 0;
        }

        [Serializable]
        private class Trap
        {
            public string trapId;
            public ulong ownerId;
            public Vector3 position;
            public TrapType trapType;
            public float placedTime;
            public bool isTriggered;
            public bool isActive;
        }

        public enum TrapType
        {
            Spike,
            Bear,
            Explosive,
            Electric,
            Gas,
            Freeze,
            Fire
        }
    }
}
