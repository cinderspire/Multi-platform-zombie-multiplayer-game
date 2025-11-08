using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

namespace DeadFrontier.Gameplay
{
    /// <summary>
    /// Extraction point where players can escape with their loot
    /// </summary>
    public class ExtractionPoint : NetworkBehaviour
    {
        [Header("Extraction Settings")]
        [SerializeField] private string extractionName = "Evac Zone";
        [SerializeField] private float extractionTime = 8f; // Time to extract
        [SerializeField] private int maxPlayers = 4; // Max players that can extract at once
        [SerializeField] private bool requiresActivation = true;

        [Header("Zone")]
        [SerializeField] private Transform extractionZone;
        [SerializeField] private float extractionRadius = 5f;
        [SerializeField] private LayerMask playerLayer;

        [Header("Visual Feedback")]
        [SerializeField] private GameObject inactiveVisual;
        [SerializeField] private GameObject activeVisual;
        [SerializeField] private GameObject extractingVisual;
        [SerializeField] private Light statusLight;
        [SerializeField] private ParticleSystem extractionEffect;

        [Header("Audio")]
        [SerializeField] private AudioClip activationSound;
        [SerializeField] private AudioClip extractionLoopSound;
        [SerializeField] private AudioClip extractionCompleteSound;

        // State
        private NetworkVariable<bool> isActive = new NetworkVariable<bool>(false);
        private NetworkVariable<int> currentExtractingCount = new NetworkVariable<int>(0);
        private Dictionary<ulong, float> extractingPlayers = new Dictionary<ulong, float>();

        // Events
        public event System.Action<string> OnPlayerStartedExtraction;
        public event System.Action<string> OnPlayerExtracted;

        // Properties
        public bool IsActive => isActive.Value;
        public bool CanExtract => isActive.Value && currentExtractingCount.Value < maxPlayers;
        public string ExtractionName => extractionName;

        private void Start()
        {
            // Set initial visuals
            UpdateVisuals();

            // Subscribe to network variable changes
            isActive.OnValueChanged += OnActiveChanged;
            currentExtractingCount.OnValueChanged += OnExtractingCountChanged;

            // Auto-activate if not requiring activation
            if (!requiresActivation && IsServer)
            {
                ActivateExtractionPoint();
            }
        }

        private void Update()
        {
            if (!IsServer)
                return;

            if (!isActive.Value)
                return;

            // Check for players in extraction zone
            CheckForPlayersInZone();

            // Update extracting players
            UpdateExtractingPlayers();
        }

        private void CheckForPlayersInZone()
        {
            Vector3 center = extractionZone != null ? extractionZone.position : transform.position;

            Collider[] players = Physics.OverlapSphere(center, extractionRadius, playerLayer);

            foreach (var playerCol in players)
            {
                var networkedPlayer = playerCol.GetComponentInParent<Networking.NetworkedPlayer>();
                if (networkedPlayer != null && networkedPlayer.IsSpawned)
                {
                    ulong clientId = networkedPlayer.OwnerClientId;

                    // Start extraction if not already extracting
                    if (!extractingPlayers.ContainsKey(clientId) && currentExtractingCount.Value < maxPlayers)
                    {
                        StartExtraction(clientId);
                    }
                }
            }

            // Check if players left the zone
            List<ulong> playersToRemove = new List<ulong>();
            foreach (var kvp in extractingPlayers)
            {
                bool stillInZone = false;

                foreach (var playerCol in players)
                {
                    var networkedPlayer = playerCol.GetComponentInParent<Networking.NetworkedPlayer>();
                    if (networkedPlayer != null && networkedPlayer.OwnerClientId == kvp.Key)
                    {
                        stillInZone = true;
                        break;
                    }
                }

                if (!stillInZone)
                {
                    playersToRemove.Add(kvp.Key);
                }
            }

            foreach (var clientId in playersToRemove)
            {
                CancelExtraction(clientId);
            }
        }

        private void UpdateExtractingPlayers()
        {
            List<ulong> completedPlayers = new List<ulong>();

            foreach (var kvp in extractingPlayers)
            {
                ulong clientId = kvp.Key;
                float progress = kvp.Value + Time.deltaTime;

                extractingPlayers[clientId] = progress;

                // Update progress on client
                UpdateExtractionProgressClientRpc(clientId, progress / extractionTime);

                // Check if extraction complete
                if (progress >= extractionTime)
                {
                    completedPlayers.Add(clientId);
                }
            }

            // Complete extractions
            foreach (var clientId in completedPlayers)
            {
                CompleteExtraction(clientId);
            }
        }

        #region Extraction Logic

        private void StartExtraction(ulong clientId)
        {
            extractingPlayers[clientId] = 0f;
            currentExtractingCount.Value = extractingPlayers.Count;

            Debug.Log($"[ExtractionPoint] Player {clientId} started extraction at {extractionName}");

            // Notify client
            NotifyExtractionStartClientRpc(clientId);

            OnPlayerStartedExtraction?.Invoke(extractionName);

            // Play extraction sound
            if (extractionLoopSound != null)
            {
                // Play looping sound
            }

            // Update visuals
            UpdateVisuals();
        }

        private void CancelExtraction(ulong clientId)
        {
            if (extractingPlayers.ContainsKey(clientId))
            {
                extractingPlayers.Remove(clientId);
                currentExtractingCount.Value = extractingPlayers.Count;

                Debug.Log($"[ExtractionPoint] Player {clientId} cancelled extraction");

                // Notify client
                NotifyExtractionCancelledClientRpc(clientId);

                UpdateVisuals();
            }
        }

        private void CompleteExtraction(ulong clientId)
        {
            extractingPlayers.Remove(clientId);
            currentExtractingCount.Value = extractingPlayers.Count;

            Debug.Log($"[ExtractionPoint] Player {clientId} successfully extracted!");

            // Notify game manager
            if (Networking.NetworkGameManager.Instance != null)
            {
                Networking.NetworkGameManager.Instance.CompleteExtractionServerRpc(clientId);
            }

            OnPlayerExtracted?.Invoke(extractionName);

            // Play success sound
            PlaySoundClientRpc(extractionCompleteSound != null ? extractionCompleteSound.name : "");

            // Spawn extraction effect
            SpawnExtractionEffectClientRpc();

            UpdateVisuals();
        }

        #endregion

        #region Activation

        /// <summary>
        /// Activates the extraction point (server-only)
        /// </summary>
        public void ActivateExtractionPoint()
        {
            if (!IsServer)
                return;

            if (isActive.Value)
                return;

            isActive.Value = true;

            Debug.Log($"[ExtractionPoint] {extractionName} activated!");

            // Play activation sound
            if (activationSound != null)
            {
                Core.AudioManager.Instance.Play(activationSound, transform.position);
            }

            // Notify all clients
            NotifyActivationClientRpc();

            UpdateVisuals();
        }

        /// <summary>
        /// Deactivates the extraction point
        /// </summary>
        public void DeactivateExtractionPoint()
        {
            if (!IsServer)
                return;

            isActive.Value = false;

            // Cancel all ongoing extractions
            foreach (var clientId in extractingPlayers.Keys)
            {
                NotifyExtractionCancelledClientRpc(clientId);
            }

            extractingPlayers.Clear();
            currentExtractingCount.Value = 0;

            UpdateVisuals();
        }

        #endregion

        #region RPCs

        [ClientRpc]
        private void NotifyActivationClientRpc()
        {
            Debug.Log($"[ExtractionPoint] {extractionName} is now active!");

            // Show UI notification
            // "EXTRACTION POINT AVAILABLE"
        }

        [ClientRpc]
        private void NotifyExtractionStartClientRpc(ulong clientId)
        {
            if (NetworkManager.Singleton.LocalClientId == clientId)
            {
                Debug.Log("[ExtractionPoint] You are extracting...");

                // Show extraction UI with progress bar
                // Lock player movement (optional)
            }
        }

        [ClientRpc]
        private void NotifyExtractionCancelledClientRpc(ulong clientId)
        {
            if (NetworkManager.Singleton.LocalClientId == clientId)
            {
                Debug.Log("[ExtractionPoint] Extraction cancelled!");

                // Hide extraction UI
            }
        }

        [ClientRpc]
        private void UpdateExtractionProgressClientRpc(ulong clientId, float progress)
        {
            if (NetworkManager.Singleton.LocalClientId == clientId)
            {
                // Update progress bar (0-1)
                // UIManager could handle this
            }
        }

        [ClientRpc]
        private void PlaySoundClientRpc(string soundName)
        {
            // Play sound by name
            // AudioManager could handle this
        }

        [ClientRpc]
        private void SpawnExtractionEffectClientRpc()
        {
            if (extractionEffect != null)
            {
                extractionEffect.Play();
            }
        }

        #endregion

        #region Visuals

        private void UpdateVisuals()
        {
            if (!isActive.Value)
            {
                // Inactive state
                SetVisualActive(inactiveVisual, true);
                SetVisualActive(activeVisual, false);
                SetVisualActive(extractingVisual, false);

                if (statusLight != null)
                {
                    statusLight.color = Color.red;
                }
            }
            else if (currentExtractingCount.Value > 0)
            {
                // Extracting state
                SetVisualActive(inactiveVisual, false);
                SetVisualActive(activeVisual, false);
                SetVisualActive(extractingVisual, true);

                if (statusLight != null)
                {
                    statusLight.color = Color.yellow;
                }
            }
            else
            {
                // Active but not extracting
                SetVisualActive(inactiveVisual, false);
                SetVisualActive(activeVisual, true);
                SetVisualActive(extractingVisual, false);

                if (statusLight != null)
                {
                    statusLight.color = Color.green;
                }
            }
        }

        private void SetVisualActive(GameObject visual, bool active)
        {
            if (visual != null)
            {
                visual.SetActive(active);
            }
        }

        private void OnActiveChanged(bool oldValue, bool newValue)
        {
            UpdateVisuals();
        }

        private void OnExtractingCountChanged(int oldValue, int newValue)
        {
            UpdateVisuals();
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmos()
        {
            Vector3 center = extractionZone != null ? extractionZone.position : transform.position;

            // Draw extraction radius
            Gizmos.color = isActive.Value ? Color.green : Color.red;
            Gizmos.DrawWireSphere(center, extractionRadius);

            // Draw extraction zone marker
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(center + Vector3.up * 2f, Vector3.one);

#if UNITY_EDITOR
            // Draw label
            UnityEditor.Handles.Label(center + Vector3.up * 3f, extractionName);
#endif
        }

        #endregion

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            isActive.OnValueChanged -= OnActiveChanged;
            currentExtractingCount.OnValueChanged -= OnExtractingCountChanged;
        }
    }
}
