using System;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Player
{
    /// <summary>
    /// Comprehensive player controller with movement, sprinting, jumping, crouching,
    /// network synchronization, and input handling.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : NetworkBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float walkSpeed = 5f;
        [SerializeField] private float sprintSpeed = 8f;
        [SerializeField] private float crouchSpeed = 2.5f;
        [SerializeField] private float acceleration = 10f;
        [SerializeField] private float deceleration = 10f;

        [Header("Jump Settings")]
        [SerializeField] private float jumpHeight = 2f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float jumpCooldown = 0.5f;

        [Header("Crouch Settings")]
        [SerializeField] private float standingHeight = 2f;
        [SerializeField] private float crouchHeight = 1f;
        [SerializeField] private float crouchTransitionSpeed = 10f;

        [Header("Ground Check")]
        [SerializeField] private float groundCheckDistance = 0.2f;
        [SerializeField] private LayerMask groundMask;

        [Header("Stamina Integration")]
        [SerializeField] private float sprintStaminaCost = 10f;
        [SerializeField] private float jumpStaminaCost = 15f;

        private CharacterController characterController;
        private StaminaSystem staminaSystem;
        private CameraController cameraController;

        private NetworkVariable<Vector3> networkPosition = new NetworkVariable<Vector3>();
        private NetworkVariable<Quaternion> networkRotation = new NetworkVariable<Quaternion>();
        private NetworkVariable<PlayerMovementState> movementState = new NetworkVariable<PlayerMovementState>();

        private Vector3 velocity;
        private Vector3 currentMovement;
        private bool isGrounded;
        private float lastJumpTime;
        private float targetHeight;
        private bool isSprinting;
        private bool isCrouching;

        public event Action<PlayerMovementState> OnMovementStateChanged;
        public event Action OnJump;
        public event Action OnLand;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            targetHeight = standingHeight;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsOwner)
            {
                // Get stamina system reference
                staminaSystem = StaminaSystem.Instance;
                
                // Find camera controller
                cameraController = GetComponentInChildren<CameraController>();
                if (cameraController != null)
                {
                    cameraController.enabled = true;
                }
            }
            else
            {
                // Disable camera for non-owners
                if (cameraController != null)
                {
                    cameraController.enabled = false;
                }
            }
        }

        private void Update()
        {
            if (IsOwner)
            {
                HandleOwnerUpdate();
            }
            else
            {
                HandleClientUpdate();
            }
        }

        private void HandleOwnerUpdate()
        {
            // Ground check
            CheckGrounded();

            // Handle input
            HandleMovementInput();
            HandleJumpInput();
            HandleCrouchInput();
            HandleSprintInput();

            // Apply movement
            ApplyMovement();
            ApplyGravity();
            ApplyCrouchHeight();

            // Move character
            characterController.Move(velocity * Time.deltaTime);

            // Update network state
            UpdateNetworkStateServerRpc(transform.position, transform.rotation, movementState.Value);
        }

        private void HandleClientUpdate()
        {
            // Smooth position interpolation for other clients
            transform.position = Vector3.Lerp(transform.position, networkPosition.Value, Time.deltaTime * 10f);
            transform.rotation = Quaternion.Lerp(transform.rotation, networkRotation.Value, Time.deltaTime * 10f);
        }

        private void CheckGrounded()
        {
            bool wasGrounded = isGrounded;
            
            // Raycast downward to check ground
            isGrounded = Physics.Raycast(
                transform.position,
                Vector3.down,
                characterController.height / 2f + groundCheckDistance,
                groundMask
            );

            // Landing event
            if (isGrounded && !wasGrounded)
            {
                OnLand?.Invoke();
            }
        }

        private void HandleMovementInput()
        {
            // Get input
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");

            // Calculate movement direction (relative to camera)
            Vector3 inputDirection = new Vector3(horizontal, 0f, vertical).normalized;
            
            if (inputDirection.magnitude >= 0.1f)
            {
                // Get camera forward direction (flattened)
                Vector3 cameraForward = Vector3.zero;
                if (cameraController != null)
                {
                    cameraForward = cameraController.transform.forward;
                    cameraForward.y = 0f;
                    cameraForward.Normalize();
                }
                else
                {
                    cameraForward = transform.forward;
                }

                // Calculate move direction relative to camera
                Vector3 moveDirection = Quaternion.LookRotation(cameraForward) * inputDirection;

                // Determine speed based on state
                float targetSpeed = walkSpeed;
                if (isSprinting && !isCrouching)
                {
                    targetSpeed = sprintSpeed;
                }
                else if (isCrouching)
                {
                    targetSpeed = crouchSpeed;
                }

                // Smooth acceleration
                currentMovement = Vector3.Lerp(
                    currentMovement,
                    moveDirection * targetSpeed,
                    acceleration * Time.deltaTime
                );

                // Rotate player to face movement direction
                if (moveDirection != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
                }

                // Update movement state
                UpdateMovementState(isSprinting, isCrouching, true);
            }
            else
            {
                // Decelerate when no input
                currentMovement = Vector3.Lerp(currentMovement, Vector3.zero, deceleration * Time.deltaTime);
                UpdateMovementState(false, isCrouching, false);
            }

            velocity.x = currentMovement.x;
            velocity.z = currentMovement.z;
        }

        private void HandleJumpInput()
        {
            if (Input.GetButtonDown("Jump") && CanJump())
            {
                PerformJump();
            }
        }

        private void HandleCrouchInput()
        {
            if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.C))
            {
                ToggleCrouch();
            }
        }

        private void HandleSprintInput()
        {
            bool sprintInput = Input.GetKey(KeyCode.LeftShift);
            
            if (sprintInput && !isCrouching && currentMovement.magnitude > 0.1f)
            {
                if (CanSprint())
                {
                    isSprinting = true;
                    
                    // Consume stamina
                    if (staminaSystem != null && IsServer)
                    {
                        staminaSystem.ConsumeStaminaServerRpc(OwnerClientId, sprintStaminaCost * Time.deltaTime);
                    }
                }
                else
                {
                    isSprinting = false;
                }
            }
            else
            {
                isSprinting = false;
            }
        }

        private void ApplyMovement()
        {
            // Movement is already applied in velocity.x and velocity.z
        }

        private void ApplyGravity()
        {
            if (isGrounded && velocity.y < 0f)
            {
                velocity.y = -2f; // Small downward force to keep grounded
            }
            else
            {
                velocity.y += gravity * Time.deltaTime;
            }
        }

        private void ApplyCrouchHeight()
        {
            float currentHeight = characterController.height;
            float newHeight = Mathf.Lerp(currentHeight, targetHeight, crouchTransitionSpeed * Time.deltaTime);
            
            characterController.height = newHeight;
            characterController.center = new Vector3(0f, newHeight / 2f, 0f);
        }

        private bool CanJump()
        {
            if (!isGrounded) return false;
            if (Time.time - lastJumpTime < jumpCooldown) return false;
            if (isCrouching) return false;

            // Check stamina
            if (staminaSystem != null)
            {
                if (!staminaSystem.HasStamina(OwnerClientId, jumpStaminaCost))
                {
                    return false;
                }
            }

            return true;
        }

        private void PerformJump()
        {
            // Calculate jump velocity
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            lastJumpTime = Time.time;

            // Consume stamina
            if (staminaSystem != null && IsServer)
            {
                staminaSystem.ConsumeStaminaServerRpc(OwnerClientId, jumpStaminaCost);
            }

            OnJump?.Invoke();
        }

        private bool CanSprint()
        {
            if (isCrouching) return false;

            // Check stamina
            if (staminaSystem != null)
            {
                if (!staminaSystem.HasStamina(OwnerClientId, sprintStaminaCost * Time.deltaTime))
                {
                    return false;
                }
            }

            return true;
        }

        private void ToggleCrouch()
        {
            if (isCrouching)
            {
                // Try to stand up (check if there's space)
                if (CanStandUp())
                {
                    isCrouching = false;
                    targetHeight = standingHeight;
                    isSprinting = false; // Can't sprint while standing up from crouch
                }
            }
            else
            {
                isCrouching = true;
                targetHeight = crouchHeight;
                isSprinting = false; // Can't sprint while crouching
            }

            UpdateMovementState(isSprinting, isCrouching, currentMovement.magnitude > 0.1f);
        }

        private bool CanStandUp()
        {
            // Raycast upward to check if there's space to stand
            float heightDifference = standingHeight - crouchHeight;
            return !Physics.Raycast(
                transform.position + Vector3.up * crouchHeight,
                Vector3.up,
                heightDifference,
                groundMask
            );
        }

        private void UpdateMovementState(bool sprinting, bool crouching, bool moving)
        {
            PlayerMovementState newState = PlayerMovementState.Idle;

            if (crouching)
            {
                newState = moving ? PlayerMovementState.Crouching : PlayerMovementState.CrouchIdle;
            }
            else if (sprinting && moving)
            {
                newState = PlayerMovementState.Sprinting;
            }
            else if (moving)
            {
                newState = PlayerMovementState.Walking;
            }

            if (!isGrounded)
            {
                newState = velocity.y > 0 ? PlayerMovementState.Jumping : PlayerMovementState.Falling;
            }

            if (movementState.Value != newState)
            {
                movementState.Value = newState;
                OnMovementStateChanged?.Invoke(newState);
            }
        }

        [ServerRpc]
        private void UpdateNetworkStateServerRpc(Vector3 position, Quaternion rotation, PlayerMovementState state)
        {
            networkPosition.Value = position;
            networkRotation.Value = rotation;
            movementState.Value = state;
        }

        // Public getters
        public bool IsGrounded => isGrounded;
        public bool IsSprinting => isSprinting;
        public bool IsCrouching => isCrouching;
        public PlayerMovementState CurrentMovementState => movementState.Value;
        public Vector3 Velocity => velocity;
        public float CurrentSpeed => currentMovement.magnitude;
    }

    public enum PlayerMovementState
    {
        Idle,
        Walking,
        Sprinting,
        Crouching,
        CrouchIdle,
        Jumping,
        Falling
    }
}
