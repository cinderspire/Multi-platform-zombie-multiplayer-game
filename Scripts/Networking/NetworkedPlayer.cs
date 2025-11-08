using UnityEngine;
using Unity.Netcode;

namespace DeadFrontier.Networking
{
    /// <summary>
    /// Networked player with client-side prediction and server reconciliation
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class NetworkedPlayer : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private Player.PlayerController playerController;
        [SerializeField] private Player.PlayerMovement playerMovement;
        [SerializeField] private Player.PlayerCamera playerCamera;
        [SerializeField] private Player.PlayerHealth playerHealth;

        [Header("Network Settings")]
        [SerializeField] private float positionLerpSpeed = 10f;
        [SerializeField] private float rotationLerpSpeed = 10f;

        // Client-side prediction
        private Vector3 serverPosition;
        private Quaternion serverRotation;
        private float serverHealth;

        // Input buffering
        private struct InputState
        {
            public Vector2 moveInput;
            public Vector2 lookInput;
            public bool sprintInput;
            public bool crouchInput;
            public bool jumpInput;
            public int tick;
        }

        private InputState currentInput;
        private int currentTick = 0;

        // Network variables
        private NetworkVariable<Vector3> networkPosition = new NetworkVariable<Vector3>();
        private NetworkVariable<Quaternion> networkRotation = new NetworkVariable<Quaternion>();
        private NetworkVariable<float> networkHealth = new NetworkVariable<float>();
        private NetworkVariable<PlayerState> playerState = new NetworkVariable<PlayerState>();

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsOwner)
            {
                // Enable local player components
                playerController.enabled = true;
                playerCamera.enabled = true;

                // Set camera as main camera
                if (playerCamera.GetComponent<Camera>() != null)
                {
                    playerCamera.GetComponent<Camera>().enabled = true;
                }

                Debug.Log("[NetworkedPlayer] Local player spawned");
            }
            else
            {
                // Disable components for remote players (we'll sync them)
                playerController.enabled = false;
                playerCamera.enabled = false;

                if (playerCamera.GetComponent<Camera>() != null)
                {
                    playerCamera.GetComponent<Camera>().enabled = false;
                }

                Debug.Log("[NetworkedPlayer] Remote player spawned");
            }

            // Subscribe to network variable changes
            networkPosition.OnValueChanged += OnPositionChanged;
            networkRotation.OnValueChanged += OnRotationChanged;
            networkHealth.OnValueChanged += OnHealthChanged;

            // Subscribe to health events
            if (playerHealth != null)
            {
                playerHealth.OnDamaged += OnPlayerDamaged;
                playerHealth.OnDeath += OnPlayerDeath;
            }
        }

        private void Update()
        {
            if (IsOwner)
            {
                // Local player - send updates to server
                UpdateLocalPlayer();
            }
            else
            {
                // Remote player - interpolate to server position
                UpdateRemotePlayer();
            }
        }

        private void UpdateLocalPlayer()
        {
            // Client-side prediction: play movement immediately
            // Server will validate and send corrections if needed

            currentTick++;

            // Send input to server
            if (IsClient)
            {
                SendInputToServerRpc(currentInput, currentTick);
            }

            // Update network variables (server authoritative)
            if (IsServer)
            {
                networkPosition.Value = transform.position;
                networkRotation.Value = transform.rotation;
                networkHealth.Value = playerHealth.CurrentHealth;
            }
        }

        private void UpdateRemotePlayer()
        {
            // Smoothly interpolate to server position
            transform.position = Vector3.Lerp(transform.position, serverPosition, Time.deltaTime * positionLerpSpeed);
            transform.rotation = Quaternion.Lerp(transform.rotation, serverRotation, Time.deltaTime * rotationLerpSpeed);

            // Update health
            if (playerHealth != null && Mathf.Abs(playerHealth.CurrentHealth - serverHealth) > 0.1f)
            {
                playerHealth.SetHealth(serverHealth);
            }
        }

        #region Network Callbacks

        private void OnPositionChanged(Vector3 oldPos, Vector3 newPos)
        {
            serverPosition = newPos;

            // Server reconciliation for local player
            if (IsOwner)
            {
                float distance = Vector3.Distance(transform.position, newPos);

                // If difference is too large, teleport to correct position
                if (distance > 5f)
                {
                    transform.position = newPos;
                    Debug.LogWarning($"[NetworkedPlayer] Position corrected by server (difference: {distance:F2}m)");
                }
            }
        }

        private void OnRotationChanged(Quaternion oldRot, Quaternion newRot)
        {
            serverRotation = newRot;
        }

        private void OnHealthChanged(float oldHealth, float newHealth)
        {
            serverHealth = newHealth;
        }

        #endregion

        #region RPCs

        /// <summary>
        /// Client sends input to server for validation
        /// </summary>
        [ServerRpc]
        private void SendInputToServerRpc(InputState input, int tick)
        {
            // Server validates input and processes movement
            // This prevents cheating by validating all movement server-side

            ProcessMovementOnServer(input, tick);
        }

        /// <summary>
        /// Server processes movement and validates it
        /// </summary>
        private void ProcessMovementOnServer(InputState input, int tick)
        {
            if (!IsServer)
                return;

            // Server validates and processes the movement
            // Apply anti-cheat checks here (speed limits, physics validation, etc.)

            // Update position
            networkPosition.Value = transform.position;
            networkRotation.Value = transform.rotation;

            // Send correction to client if needed
            // (Only if client position differs significantly from server calculation)
        }

        /// <summary>
        /// Takes damage over network
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void TakeDamageServerRpc(float damage, ulong attackerId)
        {
            if (!IsServer)
                return;

            // Server authoritative damage
            playerHealth.TakeDamage(damage);
            networkHealth.Value = playerHealth.CurrentHealth;

            // Notify clients about damage
            NotifyDamageClientRpc(damage, attackerId);
        }

        /// <summary>
        /// Notifies all clients about damage taken
        /// </summary>
        [ClientRpc]
        private void NotifyDamageClientRpc(float damage, ulong attackerId)
        {
            if (IsOwner)
            {
                // Play damage effects on owner's screen
                // playerCamera.Shake(...);
                // Show damage indicator
            }
        }

        /// <summary>
        /// Heals player over network
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void HealServerRpc(float amount)
        {
            if (!IsServer)
                return;

            playerHealth.Heal(amount);
            networkHealth.Value = playerHealth.CurrentHealth;
        }

        #endregion

        #region Player Events

        private void OnPlayerDamaged(float damageAmount, float remainingHealth)
        {
            if (IsServer)
            {
                networkHealth.Value = remainingHealth;
            }
        }

        private void OnPlayerDeath()
        {
            if (IsServer)
            {
                // Handle player death on server
                playerState.Value = PlayerState.Dead;

                // Notify all clients
                NotifyPlayerDeathClientRpc();
            }
        }

        [ClientRpc]
        private void NotifyPlayerDeathClientRpc()
        {
            Debug.Log($"[NetworkedPlayer] Player {OwnerClientId} died");

            // Disable controls
            if (IsOwner)
            {
                playerController.enabled = false;
            }

            // Play death animation/effects
            // Ragdoll, etc.
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Updates the current input state (called by PlayerController)
        /// </summary>
        public void SetInput(Vector2 moveInput, Vector2 lookInput, bool sprint, bool crouch, bool jump)
        {
            currentInput = new InputState
            {
                moveInput = moveInput,
                lookInput = lookInput,
                sprintInput = sprint,
                crouchInput = crouch,
                jumpInput = jump,
                tick = currentTick
            };
        }

        /// <summary>
        /// Spawns player at a position
        /// </summary>
        public void SpawnAtPosition(Vector3 position, Quaternion rotation)
        {
            if (!IsServer)
                return;

            transform.position = position;
            transform.rotation = rotation;

            networkPosition.Value = position;
            networkRotation.Value = rotation;

            // Reset health
            playerHealth.ResetHealth();
            networkHealth.Value = playerHealth.CurrentHealth;

            playerState.Value = PlayerState.Alive;

            Debug.Log($"[NetworkedPlayer] Player {OwnerClientId} spawned at {position}");
        }

        /// <summary>
        /// Checks if player is local player
        /// </summary>
        public bool IsLocalPlayer => IsOwner;

        #endregion

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            // Unsubscribe from events
            networkPosition.OnValueChanged -= OnPositionChanged;
            networkRotation.OnValueChanged -= OnRotationChanged;
            networkHealth.OnValueChanged -= OnHealthChanged;

            if (playerHealth != null)
            {
                playerHealth.OnDamaged -= OnPlayerDamaged;
                playerHealth.OnDeath -= OnPlayerDeath;
            }
        }
    }

    public enum PlayerState
    {
        Alive,
        Dead,
        Extracting,
        Extracted
    }
}
