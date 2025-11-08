using UnityEngine;
using UnityEngine.InputSystem;

namespace DeadFrontier.Player
{
    /// <summary>
    /// Main player controller that orchestrates all player components
    /// </summary>
    [RequireComponent(typeof(PlayerMovement))]
    [RequireComponent(typeof(PlayerCamera))]
    [RequireComponent(typeof(PlayerHealth))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private PlayerCamera playerCamera;
        [SerializeField] private PlayerHealth health;

        [Header("Input Actions")]
        [SerializeField] private InputActionAsset inputActions;

        // Input action references
        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction sprintAction;
        private InputAction crouchAction;
        private InputAction jumpAction;
        private InputAction fireAction;
        private InputAction reloadAction;
        private InputAction interactAction;

        // Input state
        private Vector2 moveInput;
        private Vector2 lookInput;
        private bool sprintInput;
        private bool crouchInput;
        private bool jumpInput;
        private bool fireInput;

        // Properties
        public PlayerMovement Movement => movement;
        public PlayerCamera Camera => playerCamera;
        public PlayerHealth Health => health;
        public bool IsLocalPlayer { get; set; } = true; // Set by network system

        private void Awake()
        {
            // Get components
            movement = GetComponent<PlayerMovement>();
            playerCamera = GetComponent<PlayerCamera>();
            health = GetComponent<PlayerHealth>();

            // Validate components
            if (movement == null || playerCamera == null || health == null)
            {
                Debug.LogError("[PlayerController] Missing required components!");
            }
        }

        private void OnEnable()
        {
            SetupInputActions();
        }

        private void OnDisable()
        {
            DisableInputActions();
        }

        private void Update()
        {
            if (!IsLocalPlayer)
                return;

            // Read inputs
            ReadInputs();

            // Update camera
            playerCamera.Look(lookInput);

            // Update camera bob
            playerCamera.UpdateCameraBob(moveInput.magnitude > 0.1f, movement.CurrentSpeed);
        }

        private void FixedUpdate()
        {
            if (!IsLocalPlayer)
                return;

            // Update movement
            movement.Move(moveInput, sprintInput, crouchInput, jumpInput);

            // Reset jump input after processing
            jumpInput = false;
        }

        /// <summary>
        /// Sets up input action references and callbacks
        /// </summary>
        private void SetupInputActions()
        {
            if (inputActions == null)
            {
                Debug.LogWarning("[PlayerController] No InputActionAsset assigned. Input will not work.");
                return;
            }

            // Get action map
            var gameplayMap = inputActions.FindActionMap("Gameplay");
            if (gameplayMap == null)
            {
                Debug.LogError("[PlayerController] 'Gameplay' action map not found!");
                return;
            }

            // Get actions
            moveAction = gameplayMap.FindAction("Move");
            lookAction = gameplayMap.FindAction("Look");
            sprintAction = gameplayMap.FindAction("Sprint");
            crouchAction = gameplayMap.FindAction("Crouch");
            jumpAction = gameplayMap.FindAction("Jump");
            fireAction = gameplayMap.FindAction("Fire");
            reloadAction = gameplayMap.FindAction("Reload");
            interactAction = gameplayMap.FindAction("Interact");

            // Subscribe to jump action (performed once per press)
            if (jumpAction != null)
            {
                jumpAction.performed += OnJumpPerformed;
            }

            if (reloadAction != null)
            {
                reloadAction.performed += OnReloadPerformed;
            }

            if (interactAction != null)
            {
                interactAction.performed += OnInteractPerformed;
            }

            // Enable actions
            gameplayMap.Enable();
        }

        /// <summary>
        /// Disables input actions
        /// </summary>
        private void DisableInputActions()
        {
            if (inputActions == null)
                return;

            // Unsubscribe from events
            if (jumpAction != null)
                jumpAction.performed -= OnJumpPerformed;

            if (reloadAction != null)
                reloadAction.performed -= OnReloadPerformed;

            if (interactAction != null)
                interactAction.performed -= OnInteractPerformed;

            // Disable actions
            inputActions.Disable();
        }

        /// <summary>
        /// Reads input values from actions
        /// </summary>
        private void ReadInputs()
        {
            moveInput = moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
            lookInput = lookAction?.ReadValue<Vector2>() ?? Vector2.zero;
            sprintInput = sprintAction?.IsPressed() ?? false;
            crouchInput = crouchAction?.IsPressed() ?? false;
            fireInput = fireAction?.IsPressed() ?? false;
        }

        // Input callbacks
        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            if (!IsLocalPlayer)
                return;

            jumpInput = true;
        }

        private void OnReloadPerformed(InputAction.CallbackContext context)
        {
            if (!IsLocalPlayer)
                return;

            Debug.Log("[PlayerController] Reload input received");
            // TODO: Call weapon reload
        }

        private void OnInteractPerformed(InputAction.CallbackContext context)
        {
            if (!IsLocalPlayer)
                return;

            Debug.Log("[PlayerController] Interact input received");
            // TODO: Handle interaction (pickup loot, open doors, etc.)
            TryInteract();
        }

        /// <summary>
        /// Attempts to interact with objects in front of the player
        /// </summary>
        private void TryInteract()
        {
            Ray ray = new Ray(playerCamera.GetCameraPosition(), playerCamera.GetLookDirection());
            float interactDistance = 3f;

            if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
            {
                // TODO: Check if hit object is interactable
                Debug.Log($"[PlayerController] Raycast hit: {hit.collider.name}");
            }
        }

        /// <summary>
        /// Teleports the player to a position
        /// </summary>
        public void Teleport(Vector3 position)
        {
            movement.Teleport(position);
        }

        /// <summary>
        /// Resets the player to initial state
        /// </summary>
        public void ResetPlayer()
        {
            health.ResetHealth();
            movement.ResetStamina();
            playerCamera.ResetRotation();
        }

        private void OnValidate()
        {
            // Auto-assign components in editor
            if (movement == null)
                movement = GetComponent<PlayerMovement>();
            if (playerCamera == null)
                playerCamera = GetComponent<PlayerCamera>();
            if (health == null)
                health = GetComponent<PlayerHealth>();
        }
    }
}
