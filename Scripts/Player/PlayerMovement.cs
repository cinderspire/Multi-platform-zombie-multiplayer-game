using UnityEngine;

namespace DeadFrontier.Player
{
    /// <summary>
    /// Handles player movement including walking, sprinting, crouching, and jumping
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement Speeds")]
        [SerializeField] private float walkSpeed = DeadFrontier.Core.Constants.PLAYER_WALK_SPEED;
        [SerializeField] private float sprintSpeed = DeadFrontier.Core.Constants.PLAYER_SPRINT_SPEED;
        [SerializeField] private float crouchSpeed = DeadFrontier.Core.Constants.PLAYER_CROUCH_SPEED;

        [Header("Jump")]
        [SerializeField] private float jumpHeight = DeadFrontier.Core.Constants.PLAYER_JUMP_HEIGHT;
        [SerializeField] private float gravity = DeadFrontier.Core.Constants.PLAYER_GRAVITY;

        [Header("Stamina")]
        [SerializeField] private float maxStamina = DeadFrontier.Core.Constants.PLAYER_MAX_STAMINA;
        [SerializeField] private float staminaRegenRate = DeadFrontier.Core.Constants.PLAYER_STAMINA_REGEN_RATE;
        [SerializeField] private float staminaDrainRate = DeadFrontier.Core.Constants.PLAYER_STAMINA_DRAIN_RATE;
        [SerializeField] private float minStaminaToSprint = 10f;

        [Header("Ground Check")]
        [SerializeField] private float groundCheckDistance = 0.2f;
        [SerializeField] private LayerMask groundMask;

        // Components
        private CharacterController controller;

        // Movement state
        private Vector3 velocity;
        private bool isGrounded;
        private float currentStamina;
        private MovementState currentMovementState = MovementState.Walking;

        // Properties
        public bool IsGrounded => isGrounded;
        public bool IsSprinting => currentMovementState == MovementState.Sprinting;
        public bool IsCrouching => currentMovementState == MovementState.Crouching;
        public float CurrentStamina => currentStamina;
        public float MaxStamina => maxStamina;
        public Vector3 Velocity => controller.velocity;
        public float CurrentSpeed => controller.velocity.magnitude;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            currentStamina = maxStamina;
        }

        private void Update()
        {
            CheckGround();
            UpdateStamina();
        }

        /// <summary>
        /// Moves the player based on input
        /// </summary>
        public void Move(Vector2 input, bool sprintInput, bool crouchInput, bool jumpInput)
        {
            // Determine movement state
            UpdateMovementState(sprintInput, crouchInput);

            // Calculate movement direction
            Vector3 moveDirection = transform.right * input.x + transform.forward * input.y;
            moveDirection.Normalize();

            // Apply movement speed
            float targetSpeed = GetCurrentSpeed();
            Vector3 move = moveDirection * targetSpeed;

            // Move the character
            controller.Move(move * Time.deltaTime);

            // Handle jumping
            if (jumpInput && isGrounded)
            {
                Jump();
            }

            // Apply gravity
            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f; // Small downward force to keep grounded
            }

            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);
        }

        /// <summary>
        /// Makes the player jump
        /// </summary>
        private void Jump()
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        /// <summary>
        /// Checks if player is on the ground
        /// </summary>
        private void CheckGround()
        {
            Vector3 spherePosition = transform.position - new Vector3(0, controller.height / 2, 0);
            isGrounded = Physics.CheckSphere(spherePosition, groundCheckDistance, groundMask);
        }

        /// <summary>
        /// Updates the movement state based on input
        /// </summary>
        private void UpdateMovementState(bool sprintInput, bool crouchInput)
        {
            if (crouchInput)
            {
                currentMovementState = MovementState.Crouching;
            }
            else if (sprintInput && currentStamina >= minStaminaToSprint)
            {
                currentMovementState = MovementState.Sprinting;
            }
            else
            {
                currentMovementState = MovementState.Walking;
            }
        }

        /// <summary>
        /// Gets the current movement speed based on state
        /// </summary>
        private float GetCurrentSpeed()
        {
            switch (currentMovementState)
            {
                case MovementState.Sprinting:
                    return sprintSpeed;
                case MovementState.Crouching:
                    return crouchSpeed;
                case MovementState.Walking:
                default:
                    return walkSpeed;
            }
        }

        /// <summary>
        /// Updates stamina regeneration and drain
        /// </summary>
        private void UpdateStamina()
        {
            if (currentMovementState == MovementState.Sprinting)
            {
                // Drain stamina while sprinting
                currentStamina -= staminaDrainRate * Time.deltaTime;
                currentStamina = Mathf.Max(0f, currentStamina);
            }
            else
            {
                // Regenerate stamina when not sprinting
                currentStamina += staminaRegenRate * Time.deltaTime;
                currentStamina = Mathf.Min(maxStamina, currentStamina);
            }
        }

        /// <summary>
        /// Teleports the player to a position
        /// </summary>
        public void Teleport(Vector3 position)
        {
            controller.enabled = false;
            transform.position = position;
            controller.enabled = true;
            velocity = Vector3.zero;
        }

        /// <summary>
        /// Resets stamina to maximum
        /// </summary>
        public void ResetStamina()
        {
            currentStamina = maxStamina;
        }

        private void OnDrawGizmosSelected()
        {
            // Draw ground check sphere
            Gizmos.color = Color.yellow;
            Vector3 spherePosition = transform.position - new Vector3(0, GetComponent<CharacterController>()?.height / 2 ?? 1f, 0);
            Gizmos.DrawWireSphere(spherePosition, groundCheckDistance);
        }
    }

    public enum MovementState
    {
        Walking,
        Sprinting,
        Crouching
    }
}
