using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

namespace DeadFrontier.Gameplay
{
    /// <summary>
    /// Manages environmental hazards like fire, radiation, toxic gas, and explosives.
    /// Adds dynamic danger to maps and tactical considerations for players.
    /// </summary>
    public class EnvironmentalHazardsManager : NetworkBehaviour
    {
        public static EnvironmentalHazardsManager Instance { get; private set; }

        [Header("Hazard Settings")]
        [SerializeField] private float hazardCheckInterval = 0.5f;
        [SerializeField] private bool enableDynamicHazards = true;

        [Header("Hazard Prefabs")]
        [SerializeField] private GameObject firePrefab;
        [SerializeField] private GameObject radiationPrefab;
        [SerializeField] private GameObject toxicGasPrefab;
        [SerializeField] private GameObject electricPrefab;
        [SerializeField] private GameObject explosivePrefab;

        [Header("Audio")]
        [SerializeField] private AudioClip fireSound;
        [SerializeField] private AudioClip explosionSound;
        [SerializeField] private AudioClip electricSound;
        [SerializeField] private AudioClip geigerSound;

        // Active hazards
        private List<HazardZone> activeHazards = new List<HazardZone>();
        private float nextHazardCheck;

        // Players in hazard zones
        private Dictionary<ulong, List<HazardZone>> playersInHazards = new Dictionary<ulong, List<HazardZone>>();

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
            base.OnNetworkSpawn();

            if (IsServer)
            {
                InitializeHazards();
            }
        }

        private void Update()
        {
            if (!IsServer) return;

            if (Time.time >= nextHazardCheck)
            {
                CheckPlayersInHazards();
                UpdateDynamicHazards();
                nextHazardCheck = Time.time + hazardCheckInterval;
            }
        }

        #region Initialization

        private void InitializeHazards()
        {
            // Find all hazard zones in scene
            var hazardZones = FindObjectsOfType<HazardZone>();
            activeHazards.AddRange(hazardZones);

            foreach (var hazard in activeHazards)
            {
                hazard.Initialize(this);
            }

            Debug.Log($"[EnvironmentalHazards] Initialized {activeHazards.Count} hazard zones");
        }

        #endregion

        #region Hazard Creation

        public HazardZone CreateHazard(Core.HazardType hazardType, Vector3 position, float radius, float damage, float duration = 0f)
        {
            if (!IsServer) return null;

            GameObject hazardPrefab = GetHazardPrefab(hazardType);
            if (hazardPrefab == null)
            {
                Debug.LogWarning($"[EnvironmentalHazards] No prefab found for hazard type: {hazardType}");
                return null;
            }

            // Instantiate hazard
            GameObject hazardObj = Instantiate(hazardPrefab, position, Quaternion.identity);
            HazardZone hazardZone = hazardObj.GetComponent<HazardZone>();

            if (hazardZone == null)
            {
                hazardZone = hazardObj.AddComponent<HazardZone>();
            }

            // Configure hazard
            hazardZone.hazardType = hazardType;
            hazardZone.position = position;
            hazardZone.radius = radius;
            hazardZone.damagePerSecond = damage;
            hazardZone.isActive = true;

            if (duration > 0f)
            {
                hazardZone.hasLifetime = true;
                hazardZone.lifetime = duration;
            }

            hazardZone.Initialize(this);

            // Network spawn
            var netObj = hazardObj.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.Spawn();
            }

            activeHazards.Add(hazardZone);

            Debug.Log($"[EnvironmentalHazards] Created {hazardType} hazard at {position}");

            return hazardZone;
        }

        public void RemoveHazard(HazardZone hazard)
        {
            if (!IsServer) return;

            if (activeHazards.Contains(hazard))
            {
                activeHazards.Remove(hazard);
            }

            // Remove from player tracking
            foreach (var playerHazards in playersInHazards.Values)
            {
                if (playerHazards.Contains(hazard))
                {
                    playerHazards.Remove(hazard);
                }
            }

            // Destroy GameObject
            if (hazard != null && hazard.gameObject != null)
            {
                var netObj = hazard.GetComponent<NetworkObject>();
                if (netObj != null)
                {
                    netObj.Despawn();
                }
                Destroy(hazard.gameObject);
            }
        }

        private GameObject GetHazardPrefab(Core.HazardType hazardType)
        {
            switch (hazardType)
            {
                case Core.HazardType.Fire: return firePrefab;
                case Core.HazardType.Radiation: return radiationPrefab;
                case Core.HazardType.Toxic: return toxicGasPrefab;
                case Core.HazardType.Electric: return electricPrefab;
                case Core.HazardType.Explosive: return explosivePrefab;
                default: return null;
            }
        }

        #endregion

        #region Player Damage

        private void CheckPlayersInHazards()
        {
            if (!IsServer) return;

            var connectedClients = NetworkManager.Singleton.ConnectedClientsList;

            foreach (var client in connectedClients)
            {
                if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
                    client.ClientId, out NetworkObject playerObj))
                    continue;

                var playerHealth = playerObj.GetComponent<Player.PlayerHealth>();
                if (playerHealth == null || !playerHealth.IsAlive)
                    continue;

                Vector3 playerPos = playerObj.transform.position;
                ulong playerId = client.ClientId;

                // Check which hazards player is in
                List<HazardZone> currentHazards = new List<HazardZone>();

                foreach (var hazard in activeHazards)
                {
                    if (!hazard.isActive) continue;

                    float distance = Vector3.Distance(playerPos, hazard.position);
                    if (distance <= hazard.radius)
                    {
                        currentHazards.Add(hazard);

                        // Apply damage
                        float damage = hazard.damagePerSecond * hazardCheckInterval;
                        playerHealth.TakeDamage(damage);

                        // Apply special effects
                        ApplyHazardEffects(playerId, hazard.hazardType, playerObj);
                    }
                }

                // Update tracking
                if (currentHazards.Count > 0)
                {
                    if (!playersInHazards.ContainsKey(playerId) || !playersInHazards[playerId].SequenceEqual(currentHazards))
                    {
                        playersInHazards[playerId] = currentHazards;

                        // Notify client of hazard entry
                        NotifyHazardStatusClientRpc(playerId, true, GetHazardTypesMask(currentHazards));
                    }
                }
                else if (playersInHazards.ContainsKey(playerId))
                {
                    playersInHazards.Remove(playerId);

                    // Notify client of hazard exit
                    NotifyHazardStatusClientRpc(playerId, false, 0);
                }
            }
        }

        private void ApplyHazardEffects(ulong playerId, Core.HazardType hazardType, NetworkObject playerObj)
        {
            var movement = playerObj.GetComponent<Player.PlayerMovement>();

            switch (hazardType)
            {
                case Core.HazardType.Fire:
                    // Apply burn DOT (already applied via damage)
                    break;

                case Core.HazardType.Radiation:
                    // Reduce max health temporarily
                    // TODO: Implement radiation sickness debuff
                    break;

                case Core.HazardType.Toxic:
                    // Blur vision, reduce stamina regen
                    if (movement != null)
                    {
                        movement.SetStaminaRegenMultiplier(0.5f);
                    }
                    break;

                case Core.HazardType.Electric:
                    // Random movement stutter
                    if (UnityEngine.Random.value < 0.1f) // 10% chance per check
                    {
                        // TODO: Apply short stun
                    }
                    break;
            }
        }

        private int GetHazardTypesMask(List<HazardZone> hazards)
        {
            int mask = 0;
            foreach (var hazard in hazards)
            {
                mask |= (1 << (int)hazard.hazardType);
            }
            return mask;
        }

        #endregion

        #region Dynamic Hazards

        private void UpdateDynamicHazards()
        {
            if (!enableDynamicHazards) return;

            // Update active hazards with lifetimes
            for (int i = activeHazards.Count - 1; i >= 0; i--)
            {
                var hazard = activeHazards[i];

                if (hazard.hasLifetime)
                {
                    hazard.currentLifetime += hazardCheckInterval;

                    if (hazard.currentLifetime >= hazard.lifetime)
                    {
                        RemoveHazard(hazard);
                    }
                }
            }
        }

        public void TriggerExplosion(Vector3 position, float radius, float damage, float fireDuration = 5f)
        {
            if (!IsServer) return;

            // Deal immediate explosion damage
            Collider[] hits = Physics.OverlapSphere(position, radius);

            foreach (var hit in hits)
            {
                // Damage players
                var playerHealth = hit.GetComponent<Player.PlayerHealth>();
                if (playerHealth != null && playerHealth.IsAlive)
                {
                    float distance = Vector3.Distance(position, hit.transform.position);
                    float falloff = 1f - (distance / radius);
                    playerHealth.TakeDamage(damage * falloff);
                }

                // Damage zombies
                var zombieAI = hit.GetComponent<AI.ZombieAI>();
                if (zombieAI != null)
                {
                    float distance = Vector3.Distance(position, hit.transform.position);
                    float falloff = 1f - (distance / radius);
                    zombieAI.TakeDamage(damage * falloff);
                }
            }

            // Create fire hazard
            if (fireDuration > 0f)
            {
                CreateHazard(Core.HazardType.Fire, position, radius * 0.5f, 10f, fireDuration);
            }

            // Play explosion effects
            if (explosionSound != null)
            {
                Core.AudioManager.Instance?.PlaySFX(explosionSound, position);
            }

            // Notify clients
            TriggerExplosionClientRpc(position, radius);

            Debug.Log($"[EnvironmentalHazards] Triggered explosion at {position}");
        }

        #endregion

        #region Client RPCs

        [ClientRpc]
        private void NotifyHazardStatusClientRpc(ulong playerId, bool inHazard, int hazardTypesMask)
        {
            // Update UI to show hazard warning
            // Play hazard sounds
            if (inHazard)
            {
                // Check which hazards are active
                if ((hazardTypesMask & (1 << (int)Core.HazardType.Radiation)) != 0)
                {
                    if (geigerSound != null)
                        Core.AudioManager.Instance?.PlaySFX(geigerSound);
                }

                // Show hazard UI
                Core.NotificationManager.Instance?.ShowNotification(
                    "WARNING: Environmental Hazard!",
                    NotificationType.Warning,
                    2f
                );
            }
        }

        [ClientRpc]
        private void TriggerExplosionClientRpc(Vector3 position, float radius)
        {
            // Play explosion VFX
            // Camera shake
            if (Camera.main != null)
            {
                // TODO: Implement camera shake
            }
        }

        #endregion

        #region Public Methods

        public bool IsPositionInHazard(Vector3 position)
        {
            foreach (var hazard in activeHazards)
            {
                if (!hazard.isActive) continue;

                float distance = Vector3.Distance(position, hazard.position);
                if (distance <= hazard.radius)
                    return true;
            }
            return false;
        }

        public List<HazardZone> GetHazardsInRange(Vector3 position, float range)
        {
            return activeHazards.Where(h =>
                h.isActive && Vector3.Distance(h.position, position) <= range
            ).ToList();
        }

        #endregion
    }

    #region Supporting Classes

    public class HazardZone : MonoBehaviour
    {
        public Core.HazardType hazardType;
        public Vector3 position;
        public float radius = 10f;
        public float damagePerSecond = 10f;
        public bool isActive = true;

        public bool hasLifetime;
        public float lifetime = 10f;
        public float currentLifetime;

        private EnvironmentalHazardsManager manager;

        public void Initialize(EnvironmentalHazardsManager hazardManager)
        {
            manager = hazardManager;
            position = transform.position;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = GetHazardColor();
            Gizmos.DrawWireSphere(transform.position, radius);
        }

        private Color GetHazardColor()
        {
            switch (hazardType)
            {
                case Core.HazardType.Fire: return Color.red;
                case Core.HazardType.Radiation: return Color.green;
                case Core.HazardType.Toxic: return Color.yellow;
                case Core.HazardType.Electric: return Color.cyan;
                case Core.HazardType.Explosive: return Color.magenta;
                default: return Color.white;
            }
        }
    }

    #endregion
}
