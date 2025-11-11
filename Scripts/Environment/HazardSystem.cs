using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Environment
{
    /// <summary>
    /// Environmental hazard system managing persistent dangers like fire zones,
    /// toxic areas, radiation, electricity, and natural hazards.
    /// </summary>
    public class HazardSystem : NetworkBehaviour
    {
        public static HazardSystem Instance { get; private set; }

        [Header("Hazard Configuration")]
        [SerializeField] private float hazardCheckInterval = 0.5f;
        [SerializeField] private bool enableDynamicHazards = true;

        private Dictionary<string, Hazard> activeHazards = new Dictionary<string, Hazard>();
        private Dictionary<ulong, List<string>> entityInHazards = new Dictionary<ulong, List<string>>();

        public event Action<string, HazardType, Vector3> OnHazardCreated;
        public event Action<string> OnHazardRemoved;
        public event Action<ulong, HazardType, float> OnEntityEnteredHazard;
        public event Action<ulong, HazardType> OnEntityExitedHazard;

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
                StartCoroutine(HazardUpdateLoop());
            }
        }

        private IEnumerator HazardUpdateLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(hazardCheckInterval);
                UpdateAllHazards();
            }
        }

        /// <summary>
        /// Create a new environmental hazard
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void CreateHazardServerRpc(Vector3 position, float radius, HazardType hazardType,
            float duration, float damagePerSecond, ServerRpcParams rpcParams = default)
        {
            string hazardId = Guid.NewGuid().ToString();

            Hazard hazard = new Hazard
            {
                hazardId = hazardId,
                position = position,
                radius = radius,
                hazardType = hazardType,
                creationTime = Time.time,
                duration = duration,
                damagePerSecond = damagePerSecond,
                isActive = true
            };

            activeHazards[hazardId] = hazard;
            OnHazardCreated?.Invoke(hazardId, hazardType, position);

            SpawnHazardClientRpc(hazardId, position, radius, hazardType, duration);
        }

        [ClientRpc]
        private void SpawnHazardClientRpc(string hazardId, Vector3 position, float radius,
            HazardType hazardType, float duration)
        {
            // Create visual representation
            GameObject hazardObj = CreateHazardVisual(position, radius, hazardType);
            if (hazardObj != null)
            {
                hazardObj.name = hazardId;
                Destroy(hazardObj, duration);
            }
        }

        private GameObject CreateHazardVisual(Vector3 position, float radius, HazardType hazardType)
        {
            // Create a visual indicator for the hazard
            GameObject hazard = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hazard.transform.position = position;
            hazard.transform.localScale = new Vector3(radius * 2, 0.1f, radius * 2);

            // Remove collider (we handle collision separately)
            Destroy(hazard.GetComponent<Collider>());

            // Apply material/color based on type
            Renderer renderer = hazard.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = GetHazardColor(hazardType);
                mat.SetFloat("_Mode", 3); // Transparent mode
                Color color = mat.color;
                color.a = 0.5f;
                mat.color = color;
                renderer.material = mat;
            }

            // Add particle effects
            AddHazardParticles(hazard, hazardType);

            return hazard;
        }

        private void AddHazardParticles(GameObject hazardObj, HazardType type)
        {
            ParticleSystem particles = hazardObj.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startColor = GetHazardColor(type);
            main.startSize = 0.2f;
            main.startLifetime = 2f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = particles.emission;
            emission.rateOverTime = type switch
            {
                HazardType.Fire => 50f,
                HazardType.Toxic => 30f,
                HazardType.Radiation => 20f,
                HazardType.Electricity => 40f,
                _ => 10f
            };
        }

        private void UpdateAllHazards()
        {
            List<string> hazardsToRemove = new List<string>();

            foreach (var kvp in activeHazards)
            {
                Hazard hazard = kvp.Value;

                // Check if hazard expired
                if (hazard.duration > 0 && Time.time - hazard.creationTime > hazard.duration)
                {
                    hazardsToRemove.Add(kvp.Key);
                    continue;
                }

                // Check for entities in hazard radius
                CheckEntitiesInHazard(hazard);
            }

            // Remove expired hazards
            foreach (string hazardId in hazardsToRemove)
            {
                RemoveHazard(hazardId);
            }
        }

        private void CheckEntitiesInHazard(Hazard hazard)
        {
            Collider[] hits = Physics.OverlapSphere(hazard.position, hazard.radius);

            HashSet<ulong> entitiesInRange = new HashSet<ulong>();

            foreach (Collider hit in hits)
            {
                NetworkObject networkObject = hit.GetComponent<NetworkObject>();
                if (networkObject != null)
                {
                    ulong entityId = networkObject.OwnerClientId;
                    entitiesInRange.Add(entityId);

                    // Track if newly entered
                    if (!entityInHazards.ContainsKey(entityId))
                    {
                        entityInHazards[entityId] = new List<string>();
                    }

                    if (!entityInHazards[entityId].Contains(hazard.hazardId))
                    {
                        entityInHazards[entityId].Add(hazard.hazardId);
                        OnEntityEnteredHazard?.Invoke(entityId, hazard.hazardType, hazard.damagePerSecond);
                    }

                    // Apply damage
                    ApplyHazardDamage(entityId, hazard);
                }
            }

            // Check for entities that left hazard
            List<ulong> toRemove = new List<ulong>();
            foreach (var kvp in entityInHazards)
            {
                if (kvp.Value.Contains(hazard.hazardId) && !entitiesInRange.Contains(kvp.Key))
                {
                    kvp.Value.Remove(hazard.hazardId);
                    OnEntityExitedHazard?.Invoke(kvp.Key, hazard.hazardType);

                    if (kvp.Value.Count == 0)
                    {
                        toRemove.Add(kvp.Key);
                    }
                }
            }

            foreach (ulong entityId in toRemove)
            {
                entityInHazards.Remove(entityId);
            }
        }

        private void ApplyHazardDamage(ulong entityId, Hazard hazard)
        {
            if (Health.HealthSystem.Instance == null) return;

            float damage = hazard.damagePerSecond * hazardCheckInterval;

            // Apply damage based on hazard type
            switch (hazard.hazardType)
            {
                case HazardType.Fire:
                    Health.HealthSystem.Instance.DamageEntityServerRpc(entityId, damage, 0, "Fire");
                    // Could apply burning DOT
                    break;

                case HazardType.Toxic:
                    Health.HealthSystem.Instance.DamageEntityServerRpc(entityId, damage, 0, "Toxic");
                    // Could reduce max health temporarily
                    break;

                case HazardType.Radiation:
                    Health.HealthSystem.Instance.DamageEntityServerRpc(entityId, damage, 0, "Radiation");
                    // Could apply lingering radiation sickness
                    break;

                case HazardType.Electricity:
                    Health.HealthSystem.Instance.DamageEntityServerRpc(entityId, damage, 0, "Electricity");
                    // Could stun/slow
                    break;

                case HazardType.Cold:
                    // Slow movement speed
                    Health.HealthSystem.Instance.DamageEntityServerRpc(entityId, damage * 0.5f, 0, "Cold");
                    break;

                case HazardType.Lava:
                    Health.HealthSystem.Instance.DamageEntityServerRpc(entityId, damage * 2f, 0, "Lava");
                    break;

                case HazardType.Acid:
                    Health.HealthSystem.Instance.DamageEntityServerRpc(entityId, damage, 0, "Acid");
                    // Could reduce armor
                    break;

                case HazardType.Spikes:
                    Health.HealthSystem.Instance.DamageEntityServerRpc(entityId, damage, 0, "Spikes");
                    break;
            }
        }

        private void RemoveHazard(string hazardId)
        {
            if (!activeHazards.ContainsKey(hazardId)) return;

            activeHazards.Remove(hazardId);
            OnHazardRemoved?.Invoke(hazardId);

            RemoveHazardClientRpc(hazardId);
        }

        [ClientRpc]
        private void RemoveHazardClientRpc(string hazardId)
        {
            GameObject hazardObj = GameObject.Find(hazardId);
            if (hazardObj != null)
            {
                Destroy(hazardObj);
            }
        }

        private Color GetHazardColor(HazardType type)
        {
            return type switch
            {
                HazardType.Fire => new Color(1f, 0.3f, 0f),
                HazardType.Toxic => new Color(0f, 1f, 0f),
                HazardType.Radiation => new Color(0f, 1f, 0f),
                HazardType.Electricity => new Color(0f, 0.5f, 1f),
                HazardType.Cold => new Color(0.5f, 0.8f, 1f),
                HazardType.Lava => new Color(1f, 0.2f, 0f),
                HazardType.Acid => new Color(1f, 1f, 0f),
                HazardType.Spikes => new Color(0.5f, 0.5f, 0.5f),
                _ => Color.red
            };
        }

        public bool IsEntityInHazard(ulong entityId)
        {
            return entityInHazards.ContainsKey(entityId) && entityInHazards[entityId].Count > 0;
        }

        public List<HazardType> GetEntityHazards(ulong entityId)
        {
            List<HazardType> types = new List<HazardType>();
            if (!entityInHazards.ContainsKey(entityId)) return types;

            foreach (string hazardId in entityInHazards[entityId])
            {
                if (activeHazards.TryGetValue(hazardId, out Hazard hazard))
                {
                    types.Add(hazard.hazardType);
                }
            }

            return types;
        }

        [Serializable]
        private class Hazard
        {
            public string hazardId;
            public Vector3 position;
            public float radius;
            public HazardType hazardType;
            public float creationTime;
            public float duration; // 0 = permanent
            public float damagePerSecond;
            public bool isActive;
        }

        public enum HazardType
        {
            Fire,
            Toxic,
            Radiation,
            Electricity,
            Cold,
            Lava,
            Acid,
            Spikes
        }
    }
}
