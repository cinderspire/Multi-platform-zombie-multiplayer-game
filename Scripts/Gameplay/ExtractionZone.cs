using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace DeadFrontier.Gameplay
{
    /// <summary>
    /// Individual extraction zone component. Place in scene to create extraction points.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ExtractionZone : NetworkBehaviour
    {
        [Header("Zone Settings")]
        [SerializeField] private string zoneName = "Extraction Point";
        [SerializeField] private float baseExtractionTime = 10f;
        [SerializeField] private float rewardMultiplier = 1f;
        [SerializeField] private int maxSimultaneousExtractions = 4;

        [Header("Zone Type")]
        [SerializeField] private ExtractionZoneVariant zoneVariant = ExtractionZoneVariant.Standard;

        [Header("Visuals")]
        [SerializeField] private GameObject activeVisuals;
        [SerializeField] private GameObject inactiveVisuals;
        [SerializeField] private ParticleSystem extractionVFX;
        [SerializeField] private Light zoneLight;
        [SerializeField] private Color activeColor = Color.green;
        [SerializeField] private Color inactiveColor = Color.gray;

        [Header("Audio")]
        [SerializeField] private AudioSource ambientAudioSource;
        [SerializeField] private AudioClip ambientActiveSound;

        // Network state
        private NetworkVariable<bool> isActive = new NetworkVariable<bool>(false);
        private NetworkVariable<int> playersExtracting = new NetworkVariable<int>(0);

        // Players in zone
        private HashSet<ulong> playersInZone = new HashSet<ulong>();
        private Dictionary<ulong, float> playerEnterTimes = new Dictionary<ulong, float>();

        private ExtractionZoneManager manager;
        private Collider triggerCollider;

        // Properties
        public string ZoneName => zoneName;
        public float BaseExtractionTime => baseExtractionTime;
        public float RewardMultiplier => rewardMultiplier;
        public bool IsActive => isActive.Value;
        public int PlayersExtracting => playersExtracting.Value;

        private void Awake()
        {
            triggerCollider = GetComponent<Collider>();
            triggerCollider.isTrigger = true;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            isActive.OnValueChanged += OnActiveStateChanged;
            playersExtracting.OnValueChanged += OnPlayerCountChanged;

            UpdateVisuals();
        }

        public override void OnNetworkDespawn()
        {
            isActive.OnValueChanged -= OnActiveStateChanged;
            playersExtracting.OnValueChanged -= OnPlayerCountChanged;
            base.OnNetworkDespawn();
        }

        private void Update()
        {
            if (!IsServer || !isActive.Value) return;

            // Check if players are still in zone
            CheckPlayersInZone();
        }

        public void Initialize(ExtractionZoneManager zoneManager)
        {
            manager = zoneManager;
        }

        #region Zone Activation

        public void SetActive(bool active)
        {
            if (!IsServer) return;

            isActive.Value = active;
            UpdateVisuals();

            if (!active)
            {
                // Cancel all active extractions
                CancelAllExtractions();
            }
        }

        private void OnActiveStateChanged(bool previousValue, bool newValue)
        {
            UpdateVisuals();

            if (!newValue)
            {
                CancelAllExtractions();
            }
        }

        private void UpdateVisuals()
        {
            bool active = isActive.Value;

            if (activeVisuals != null)
                activeVisuals.SetActive(active);

            if (inactiveVisuals != null)
                inactiveVisuals.SetActive(!active);

            if (zoneLight != null)
                zoneLight.color = active ? activeColor : inactiveColor;

            if (extractionVFX != null)
            {
                if (active && !extractionVFX.isPlaying)
                    extractionVFX.Play();
                else if (!active && extractionVFX.isPlaying)
                    extractionVFX.Stop();
            }

            if (ambientAudioSource != null && ambientActiveSound != null)
            {
                if (active && !ambientAudioSource.isPlaying)
                {
                    ambientAudioSource.clip = ambientActiveSound;
                    ambientAudioSource.Play();
                }
                else if (!active && ambientAudioSource.isPlaying)
                {
                    ambientAudioSource.Stop();
                }
            }
        }

        #endregion

        #region Player Detection

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer) return;
            if (!isActive.Value) return;

            if (other.CompareTag("Player"))
            {
                var networkObject = other.GetComponent<NetworkObject>();
                if (networkObject != null)
                {
                    ulong playerId = networkObject.OwnerClientId;

                    if (!playersInZone.Contains(playerId))
                    {
                        playersInZone.Add(playerId);
                        playerEnterTimes[playerId] = Time.time;

                        // Check if can start extraction
                        if (CanStartExtraction(playerId))
                        {
                            StartPlayerExtraction(playerId);
                        }
                    }
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsServer) return;

            if (other.CompareTag("Player"))
            {
                var networkObject = other.GetComponent<NetworkObject>();
                if (networkObject != null)
                {
                    ulong playerId = networkObject.OwnerClientId;

                    if (playersInZone.Contains(playerId))
                    {
                        playersInZone.Remove(playerId);
                        playerEnterTimes.Remove(playerId);

                        // Cancel extraction if in progress
                        if (manager.IsPlayerExtracting(playerId))
                        {
                            manager.CancelExtraction(playerId, "Left extraction zone");
                            playersExtracting.Value--;
                        }
                    }
                }
            }
        }

        private void CheckPlayersInZone()
        {
            var playersToRemove = new List<ulong>();

            foreach (ulong playerId in playersInZone)
            {
                // Verify player is still in zone bounds
                if (!IsPlayerInZone(playerId))
                {
                    playersToRemove.Add(playerId);

                    if (manager.IsPlayerExtracting(playerId))
                    {
                        manager.CancelExtraction(playerId, "Left extraction zone");
                        playersExtracting.Value--;
                    }
                }
            }

            foreach (ulong playerId in playersToRemove)
            {
                playersInZone.Remove(playerId);
                playerEnterTimes.Remove(playerId);
            }
        }

        private bool IsPlayerInZone(ulong playerId)
        {
            // Find player GameObject
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerId, out NetworkObject netObj))
            {
                return triggerCollider.bounds.Contains(netObj.transform.position);
            }
            return false;
        }

        #endregion

        #region Extraction

        private bool CanStartExtraction(ulong playerId)
        {
            // Check if zone is at capacity
            if (playersExtracting.Value >= maxSimultaneousExtractions)
                return false;

            // Check if player is already extracting
            if (manager.IsPlayerExtracting(playerId))
                return false;

            // Check if player is alive
            var playerHealth = GetPlayerHealth(playerId);
            if (playerHealth == null || !playerHealth.IsAlive)
                return false;

            // Variant-specific checks
            switch (zoneVariant)
            {
                case ExtractionZoneVariant.HighRisk:
                    // High risk zones require no zombies nearby
                    if (AreZombiesNearby(20f))
                        return false;
                    break;

                case ExtractionZoneVariant.SecureOnly:
                    // Secure zones require area to be cleared
                    if (AreZombiesNearby(50f))
                        return false;
                    break;
            }

            return true;
        }

        private void StartPlayerExtraction(ulong playerId)
        {
            if (manager == null) return;

            manager.StartExtraction(playerId, this);
            playersExtracting.Value++;
        }

        private void CancelAllExtractions()
        {
            if (!IsServer) return;

            foreach (ulong playerId in new List<ulong>(playersInZone))
            {
                if (manager.IsPlayerExtracting(playerId))
                {
                    manager.CancelExtraction(playerId, "Zone deactivated");
                }
            }

            playersExtracting.Value = 0;
        }

        private void OnPlayerCountChanged(int previousValue, int newValue)
        {
            // Update visuals based on player count
            // Could add more intense effects when more players extracting
        }

        #endregion

        #region Helpers

        private Player.PlayerHealth GetPlayerHealth(ulong playerId)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerId, out NetworkObject netObj))
            {
                return netObj.GetComponent<Player.PlayerHealth>();
            }
            return null;
        }

        private bool AreZombiesNearby(float radius)
        {
            if (AI.ZombieManager.Instance == null) return false;

            var nearbyZombies = AI.ZombieManager.Instance.GetZombiesInRange(transform.position, radius);
            return nearbyZombies != null && nearbyZombies.Count > 0;
        }

        #endregion

        #region Debug

        private void OnDrawGizmos()
        {
            Gizmos.color = isActive.Value ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position, 5f);

            // Draw zone name in editor
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 3f, zoneName);
            #endif
        }

        private void OnDrawGizmosSelected()
        {
            // Draw extraction radius
            Gizmos.color = Color.yellow;
            if (triggerCollider != null && triggerCollider is SphereCollider sphere)
            {
                Gizmos.DrawWireSphere(transform.position, sphere.radius);
            }
        }

        #endregion
    }

    public enum ExtractionZoneVariant
    {
        Standard,      // Normal extraction
        HighRisk,      // Faster but requires no zombies nearby
        SecureOnly,    // Slow but safe, requires large area clear
        VIPOnly,       // For special game modes
        Emergency      // Always active, longer extraction time
    }
}
