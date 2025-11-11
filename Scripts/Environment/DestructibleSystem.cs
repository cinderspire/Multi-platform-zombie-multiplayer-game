using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Environment
{
    /// <summary>
    /// Destructible environment system allowing objects to be damaged and destroyed,
    /// spawning debris and potentially dropping loot.
    /// </summary>
    public class DestructibleSystem : NetworkBehaviour
    {
        public static DestructibleSystem Instance { get; private set; }

        [Header("Destructible Configuration")]
        [SerializeField] private bool enablePhysicsDebris = true;
        [SerializeField] private float debrisLifetime = 10f;
        [SerializeField] private int maxActiveDebris = 100;

        private Dictionary<string, DestructibleObject> destructibles = new Dictionary<string, DestructibleObject>();
        private List<GameObject> activeDebris = new List<GameObject>();

        public event Action<string, float> OnDestructibleDamaged;
        public event Action<string, Vector3> OnDestructibleDestroyed;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        /// <summary>
        /// Register a destructible object in the world
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void RegisterDestructibleServerRpc(string objectId, Vector3 position, DestructibleType type,
            float health, ServerRpcParams rpcParams = default)
        {
            if (destructibles.ContainsKey(objectId)) return;

            destructibles[objectId] = new DestructibleObject
            {
                objectId = objectId,
                position = position,
                type = type,
                maxHealth = health,
                currentHealth = health,
                isDestroyed = false
            };
        }

        /// <summary>
        /// Damage a destructible object
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void DamageDestructibleServerRpc(string objectId, float damage, Vector3 hitPoint,
            Vector3 hitNormal, ServerRpcParams rpcParams = default)
        {
            if (!destructibles.TryGetValue(objectId, out var destructible)) return;
            if (destructible.isDestroyed) return;

            destructible.currentHealth -= damage;
            OnDestructibleDamaged?.Invoke(objectId, destructible.currentHealth);

            // Visual feedback
            SpawnHitEffectClientRpc(hitPoint, hitNormal, destructible.type);

            if (destructible.currentHealth <= 0)
            {
                DestroyDestructible(objectId);
            }
        }

        private void DestroyDestructible(string objectId)
        {
            if (!destructibles.TryGetValue(objectId, out var destructible)) return;

            destructible.isDestroyed = true;
            OnDestructibleDestroyed?.Invoke(objectId, destructible.position);

            // Spawn debris
            SpawnDebrisClientRpc(destructible.position, destructible.type);

            // Drop loot if applicable
            if (ShouldDropLoot(destructible.type))
            {
                SpawnLoot(destructible.position, destructible.type);
            }

            // Destroy on clients
            DestroyObjectClientRpc(objectId);
        }

        [ClientRpc]
        private void DestroyObjectClientRpc(string objectId)
        {
            // Find and destroy the game object
            GameObject obj = GameObject.Find(objectId);
            if (obj != null)
            {
                Destroy(obj);
            }
        }

        [ClientRpc]
        private void SpawnHitEffectClientRpc(Vector3 position, Vector3 normal, DestructibleType type)
        {
            // Spawn particle effect based on material type
            GameObject effectPrefab = GetHitEffectPrefab(type);
            if (effectPrefab != null)
            {
                GameObject effect = Instantiate(effectPrefab, position, Quaternion.LookRotation(normal));
                Destroy(effect, 2f);
            }
        }

        [ClientRpc]
        private void SpawnDebrisClientRpc(Vector3 position, DestructibleType type)
        {
            if (!enablePhysicsDebris) return;

            // Spawn debris pieces
            GameObject debrisPrefab = GetDebrisPrefab(type);
            if (debrisPrefab == null) return;

            int pieceCount = UnityEngine.Random.Range(3, 8);
            for (int i = 0; i < pieceCount; i++)
            {
                Vector3 randomOffset = UnityEngine.Random.insideUnitSphere * 0.5f;
                GameObject debris = Instantiate(debrisPrefab, position + randomOffset, UnityEngine.Random.rotation);

                // Add physics
                Rigidbody rb = debris.GetComponent<Rigidbody>();
                if (rb == null) rb = debris.AddComponent<Rigidbody>();

                Vector3 explosionForce = (UnityEngine.Random.insideUnitSphere + Vector3.up * 0.5f) * 5f;
                rb.AddForce(explosionForce, ForceMode.Impulse);
                rb.AddTorque(UnityEngine.Random.insideUnitSphere * 10f, ForceMode.Impulse);

                activeDebris.Add(debris);
                Destroy(debris, debrisLifetime);

                // Clean up old debris if too many
                if (activeDebris.Count > maxActiveDebris)
                {
                    GameObject oldest = activeDebris[0];
                    activeDebris.RemoveAt(0);
                    if (oldest != null) Destroy(oldest);
                }
            }
        }

        private void SpawnLoot(Vector3 position, DestructibleType type)
        {
            // Integrate with loot system
            if (Loot.LootDropSystem.Instance != null)
            {
                string lootTableId = GetLootTableForType(type);
                Loot.LootDropSystem.Instance.SpawnLootDropServerRpc(position, lootTableId);
            }
        }

        private bool ShouldDropLoot(DestructibleType type)
        {
            return type switch
            {
                DestructibleType.Crate => UnityEngine.Random.value > 0.5f,
                DestructibleType.Barrel => UnityEngine.Random.value > 0.7f,
                DestructibleType.Container => true,
                _ => false
            };
        }

        private string GetLootTableForType(DestructibleType type)
        {
            return type switch
            {
                DestructibleType.Crate => "crate_loot",
                DestructibleType.Barrel => "barrel_loot",
                DestructibleType.Container => "container_loot",
                _ => "common_loot"
            };
        }

        private GameObject GetHitEffectPrefab(DestructibleType type)
        {
            // Would load from resources or prefab references
            return null;
        }

        private GameObject GetDebrisPrefab(DestructibleType type)
        {
            // Would load from resources or prefab references
            return null;
        }

        public bool IsDestroyed(string objectId)
        {
            return destructibles.TryGetValue(objectId, out var destructible) && destructible.isDestroyed;
        }

        public float GetHealthPercentage(string objectId)
        {
            if (!destructibles.TryGetValue(objectId, out var destructible)) return 0f;
            return destructible.currentHealth / destructible.maxHealth;
        }

        [Serializable]
        private class DestructibleObject
        {
            public string objectId;
            public Vector3 position;
            public DestructibleType type;
            public float maxHealth;
            public float currentHealth;
            public bool isDestroyed;
        }

        public enum DestructibleType
        {
            Wood,
            Metal,
            Glass,
            Concrete,
            Crate,
            Barrel,
            Container,
            Door,
            Window,
            Fence,
            Wall
        }
    }
}
