using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System;

namespace DeadFrontier.Movement
{
    /// <summary>
    /// Comprehensive parkour and advanced movement system.
    /// Supports vaulting, climbing, wall running, sliding, mantling, and more.
    /// Provides fluid traversal mechanics with stamina costs and skill progression.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class ParkourSystem : NetworkBehaviour
    {
        [Header("Components")]
        [SerializeField] private CharacterController characterController;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Animator animator;

        [Header("Vault Settings")]
        [SerializeField] private bool enableVaulting = true;
        [SerializeField] private float vaultHeight = 1.2f;
        [SerializeField] private float vaultDistance = 2f;
        [SerializeField] private float vaultDuration = 0.4f;
        [SerializeField] private float vaultStaminaCost = 10f;
        [SerializeField] private LayerMask vaultableMask;

        [Header("Climb Settings")]
        [SerializeField] private bool enableClimbing = true;
        [SerializeField] private float climbHeight = 2.5f;
        [SerializeField] private float climbSpeed = 3f;
        [SerializeField] private float climbStaminaCost = 20f;
        [SerializeField] private float climbStaminaDrainRate = 5f; // Per second

        [Header("Wall Run Settings")]
        [SerializeField] private bool enableWallRunning = true;
        [SerializeField] private float wallRunSpeed = 8f;
        [SerializeField] private float wallRunDuration = 2f;
        [SerializeField] private float wallRunStaminaDrainRate = 15f;
        [SerializeField] private float wallRunJumpForce = 10f;
        [SerializeField] private LayerMask wallRunMask;

        [Header("Slide Settings")]
        [SerializeField] private bool enableSliding = true;
        [SerializeField] private float slideSpeed = 10f;
        [SerializeField] private float slideDuration = 1.5f;
        [SerializeField] private float slideStaminaCost = 8f;
        [SerializeField] private float slideControllerHeight = 0.6f;
        [SerializeField] private float minSlideSpeed = 5f; // Minimum speed to initiate slide

        [Header("Mantle Settings")]
        [SerializeField] private bool enableMantling = true;
        [SerializeField] private float mantleHeight = 1.5f;
        [SerializeField] private float mantleReachDistance = 1f;
        [SerializeField] private float mantleDuration = 0.6f;
        [SerializeField] private float mantleStaminaCost = 12f;

        [Header("Drop Roll Settings")]
        [SerializeField] private bool enableDropRoll = true;
        [SerializeField] private float dropRollHeight = 3f; // Height to trigger roll
        [SerializeField] private float rollDuration = 0.5f;
        [SerializeField] private float rollDamageReduction = 0.6f; // 60% damage reduction

        [Header("Sprint Jump Settings")]
        [SerializeField] private bool enableSprintJump = true;
        [SerializeField] private float sprintJumpMultiplier = 1.3f;
        [SerializeField] private float sprintJumpStaminaCost = 15f;

        [Header("Detection Settings")]
        [SerializeField] private float ledgeDetectionRadius = 0.5f;
        [SerializeField] private float obstacleCheckDistance = 1.5f;
        [SerializeField] private float groundCheckDistance = 0.3f;

        [Header("Physics")]
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float terminalVelocity = -50f;

        // State
        private ParkourState currentState = ParkourState.Normal;
        private float stateTimer;
        private Vector3 parkourTargetPosition;
        private Vector3 movementVelocity;
        private float verticalVelocity;

        // Wall run state
        private Vector3 wallRunNormal;
        private float wallRunTimer;
        private WallRunSide wallRunSide = WallRunSide.None;

        // Slide state
        private float normalControllerHeight;
        private Vector3 slideDirection;

        // Drop data
        private float lastGroundedHeight;
        private bool wasAirborne;

        // Cooldowns
        private Dictionary<ParkourAction, float> actionCooldowns = new Dictionary<ParkourAction, float>();

        // Network state
        private NetworkVariable<ParkourState> networkState = new NetworkVariable<ParkourState>(ParkourState.Normal);

        // Events
        public event Action<ParkourAction> OnParkourActionStarted;
        public event Action<ParkourAction> OnParkourActionCompleted;
        public event Action<float> OnStaminaConsumed;

        private void Awake()
        {
            if (characterController == null)
            {
                characterController = GetComponent<CharacterController>();
            }

            normalControllerHeight = characterController.height;
        }

        private void Update()
        {
            if (!IsOwner) return;

            UpdateParkourState();
            CheckParkourInputs();
        }

        #region State Management

        private void UpdateParkourState()
        {
            switch (currentState)
            {
                case ParkourState.Normal:
                    UpdateNormalMovement();
                    break;

                case ParkourState.Vaulting:
                    UpdateVault();
                    break;

                case ParkourState.Climbing:
                    UpdateClimb();
                    break;

                case ParkourState.WallRunning:
                    UpdateWallRun();
                    break;

                case ParkourState.Sliding:
                    UpdateSlide();
                    break;

                case ParkourState.Mantling:
                    UpdateMantle();
                    break;

                case ParkourState.Rolling:
                    UpdateRoll();
                    break;
            }

            // Track airborne state for drop roll
            bool isGrounded = characterController.isGrounded;

            if (isGrounded)
            {
                if (wasAirborne)
                {
                    // Just landed
                    float fallDistance = lastGroundedHeight - transform.position.y;
                    HandleLanding(fallDistance);
                }

                lastGroundedHeight = transform.position.y;
                wasAirborne = false;
            }
            else
            {
                wasAirborne = true;
            }
        }

        private void SetParkourState(ParkourState newState)
        {
            currentState = newState;
            stateTimer = 0f;

            if (IsServer)
            {
                networkState.Value = newState;
            }
            else
            {
                UpdateParkourStateServerRpc(newState);
            }
        }

        [ServerRpc]
        private void UpdateParkourStateServerRpc(ParkourState state)
        {
            networkState.Value = state;
        }

        #endregion

        #region Input Detection

        private void CheckParkourInputs()
        {
            if (currentState != ParkourState.Normal) return;

            // Vault
            if (enableVaulting && Input.GetKeyDown(KeyCode.Q))
            {
                TryVault();
            }

            // Climb
            if (enableClimbing && Input.GetKeyDown(KeyCode.E))
            {
                TryClimb();
            }

            // Slide
            if (enableSliding && Input.GetKeyDown(KeyCode.C))
            {
                TrySlide();
            }

            // Check wall run automatically
            if (enableWallRunning && !characterController.isGrounded)
            {
                TryWallRun();
            }
        }

        #endregion

        #region Vaulting

        private void TryVault()
        {
            if (!CanPerformAction(ParkourAction.Vault, vaultStaminaCost)) return;

            // Check for vaultable obstacle
            Vector3 forward = transform.forward;
            Vector3 rayStart = transform.position + Vector3.up * 0.5f;

            RaycastHit hit;
            if (Physics.Raycast(rayStart, forward, out hit, vaultDistance, vaultableMask))
            {
                // Check height
                if (hit.point.y - transform.position.y > vaultHeight) return;

                // Check if there's space on the other side
                Vector3 landingPosition = hit.point + forward * 1f;
                if (!IsPositionValid(landingPosition)) return;

                // Start vault
                StartVault(hit.point, landingPosition);
            }
        }

        private void StartVault(Vector3 obstaclePoint, Vector3 landingPosition)
        {
            ConsumeStamina(vaultStaminaCost, ParkourAction.Vault);

            parkourTargetPosition = landingPosition;
            SetParkourState(ParkourState.Vaulting);

            OnParkourActionStarted?.Invoke(ParkourAction.Vault);

            // Play animation
            if (animator != null)
            {
                animator.SetTrigger("Vault");
            }

            Debug.Log("[ParkourSystem] Started vault");
        }

        private void UpdateVault()
        {
            stateTimer += Time.deltaTime;

            float progress = stateTimer / vaultDuration;

            if (progress >= 1f)
            {
                // Complete vault
                SetParkourState(ParkourState.Normal);
                OnParkourActionCompleted?.Invoke(ParkourAction.Vault);
                return;
            }

            // Interpolate to target position
            Vector3 newPosition = Vector3.Lerp(transform.position, parkourTargetPosition, progress);
            characterController.Move(newPosition - transform.position);
        }

        #endregion

        #region Climbing

        private void TryClimb()
        {
            if (!CanPerformAction(ParkourAction.Climb, climbStaminaCost)) return;

            // Check for climbable ledge
            Vector3 forward = transform.forward;
            Vector3 rayStart = transform.position + Vector3.up * climbHeight;

            RaycastHit hit;
            if (Physics.Raycast(rayStart, forward, out hit, obstacleCheckDistance))
            {
                // Check if there's a ledge to grab
                Vector3 ledgePosition = hit.point + Vector3.up * 0.5f;

                if (IsLedgeGrabbable(ledgePosition))
                {
                    StartClimb(ledgePosition);
                }
            }
        }

        private bool IsLedgeGrabbable(Vector3 ledgePosition)
        {
            // Check if there's space to climb up
            return IsPositionValid(ledgePosition);
        }

        private void StartClimb(Vector3 ledgePosition)
        {
            ConsumeStamina(climbStaminaCost, ParkourAction.Climb);

            parkourTargetPosition = ledgePosition;
            SetParkourState(ParkourState.Climbing);

            OnParkourActionStarted?.Invoke(ParkourAction.Climb);

            if (animator != null)
            {
                animator.SetTrigger("Climb");
            }

            Debug.Log("[ParkourSystem] Started climb");
        }

        private void UpdateClimb()
        {
            // Drain stamina while climbing
            if (!ConsumeStamina(climbStaminaDrainRate * Time.deltaTime, ParkourAction.Climb))
            {
                // Out of stamina, cancel climb
                SetParkourState(ParkourState.Normal);
                return;
            }

            // Move towards ledge
            Vector3 direction = (parkourTargetPosition - transform.position).normalized;
            characterController.Move(direction * climbSpeed * Time.deltaTime);

            // Check if reached ledge
            if (Vector3.Distance(transform.position, parkourTargetPosition) < 0.5f)
            {
                SetParkourState(ParkourState.Normal);
                OnParkourActionCompleted?.Invoke(ParkourAction.Climb);
            }
        }

        #endregion

        #region Wall Running

        private void TryWallRun()
        {
            if (!CanPerformAction(ParkourAction.WallRun, 0f)) return;
            if (wallRunTimer > 0f) return; // Cooldown

            // Check for walls on left and right
            RaycastHit leftHit, rightHit;
            bool leftWall = Physics.Raycast(transform.position, -transform.right, out leftHit, 1f, wallRunMask);
            bool rightWall = Physics.Raycast(transform.position, transform.right, out rightHit, 1f, wallRunMask);

            if (leftWall || rightWall)
            {
                WallRunSide side = leftWall ? WallRunSide.Left : WallRunSide.Right;
                Vector3 wallNormal = leftWall ? leftHit.normal : rightHit.normal;

                StartWallRun(side, wallNormal);
            }
        }

        private void StartWallRun(WallRunSide side, Vector3 wallNormal)
        {
            wallRunSide = side;
            wallRunNormal = wallNormal;
            wallRunTimer = wallRunDuration;

            SetParkourState(ParkourState.WallRunning);

            OnParkourActionStarted?.Invoke(ParkourAction.WallRun);

            if (animator != null)
            {
                animator.SetTrigger("WallRun");
                animator.SetFloat("WallRunSide", side == WallRunSide.Left ? -1f : 1f);
            }

            Debug.Log($"[ParkourSystem] Started wall run on {side} side");
        }

        private void UpdateWallRun()
        {
            wallRunTimer -= Time.deltaTime;

            // Drain stamina
            if (!ConsumeStamina(wallRunStaminaDrainRate * Time.deltaTime, ParkourAction.WallRun))
            {
                EndWallRun();
                return;
            }

            // Check if still on wall
            Vector3 wallCheckDirection = wallRunSide == WallRunSide.Left ? -transform.right : transform.right;
            if (!Physics.Raycast(transform.position, wallCheckDirection, 1f, wallRunMask))
            {
                EndWallRun();
                return;
            }

            // Time expired
            if (wallRunTimer <= 0f)
            {
                EndWallRun();
                return;
            }

            // Move along wall
            Vector3 wallForward = Vector3.Cross(wallRunNormal, Vector3.up);
            if (wallRunSide == WallRunSide.Right)
            {
                wallForward = -wallForward;
            }

            characterController.Move(wallForward * wallRunSpeed * Time.deltaTime);

            // Wall run jump
            if (Input.GetKeyDown(KeyCode.Space))
            {
                PerformWallRunJump();
            }
        }

        private void PerformWallRunJump()
        {
            // Jump off wall
            Vector3 jumpDirection = (wallRunNormal + Vector3.up).normalized;
            movementVelocity = jumpDirection * wallRunJumpForce;

            EndWallRun();
        }

        private void EndWallRun()
        {
            SetParkourState(ParkourState.Normal);
            wallRunSide = WallRunSide.None;

            SetActionCooldown(ParkourAction.WallRun, 2f);

            OnParkourActionCompleted?.Invoke(ParkourAction.WallRun);
        }

        #endregion

        #region Sliding

        private void TrySlide()
        {
            if (!CanPerformAction(ParkourAction.Slide, slideStaminaCost)) return;

            // Check if moving fast enough
            float currentSpeed = movementVelocity.magnitude;
            if (currentSpeed < minSlideSpeed) return;

            StartSlide();
        }

        private void StartSlide()
        {
            ConsumeStamina(slideStaminaCost, ParkourAction.Slide);

            slideDirection = movementVelocity.normalized;

            // Reduce controller height
            characterController.height = slideControllerHeight;
            characterController.center = new Vector3(0, slideControllerHeight / 2f, 0);

            SetParkourState(ParkourState.Sliding);

            OnParkourActionStarted?.Invoke(ParkourAction.Slide);

            if (animator != null)
            {
                animator.SetTrigger("Slide");
            }

            Debug.Log("[ParkourSystem] Started slide");
        }

        private void UpdateSlide()
        {
            stateTimer += Time.deltaTime;

            if (stateTimer >= slideDuration)
            {
                EndSlide();
                return;
            }

            // Move in slide direction with deceleration
            float slideProgress = stateTimer / slideDuration;
            float currentSlideSpeed = Mathf.Lerp(slideSpeed, minSlideSpeed, slideProgress);

            characterController.Move(slideDirection * currentSlideSpeed * Time.deltaTime);

            // Allow early exit
            if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.Space))
            {
                EndSlide();
            }
        }

        private void EndSlide()
        {
            // Restore controller height
            characterController.height = normalControllerHeight;
            characterController.center = new Vector3(0, normalControllerHeight / 2f, 0);

            SetParkourState(ParkourState.Normal);

            OnParkourActionCompleted?.Invoke(ParkourAction.Slide);
        }

        #endregion

        #region Mantling

        private void TryMantle()
        {
            if (!CanPerformAction(ParkourAction.Mantle, mantleStaminaCost)) return;

            // Detect ledge at chest height
            Vector3 forward = transform.forward;
            Vector3 rayStart = transform.position + Vector3.up * mantleHeight;

            RaycastHit hit;
            if (Physics.Raycast(rayStart, forward, out hit, mantleReachDistance))
            {
                Vector3 mantlePosition = hit.point + Vector3.up * 0.5f;

                if (IsPositionValid(mantlePosition))
                {
                    StartMantle(mantlePosition);
                }
            }
        }

        private void StartMantle(Vector3 mantlePosition)
        {
            ConsumeStamina(mantleStaminaCost, ParkourAction.Mantle);

            parkourTargetPosition = mantlePosition;
            SetParkourState(ParkourState.Mantling);

            OnParkourActionStarted?.Invoke(ParkourAction.Mantle);

            if (animator != null)
            {
                animator.SetTrigger("Mantle");
            }

            Debug.Log("[ParkourSystem] Started mantle");
        }

        private void UpdateMantle()
        {
            stateTimer += Time.deltaTime;
            float progress = stateTimer / mantleDuration;

            if (progress >= 1f)
            {
                SetParkourState(ParkourState.Normal);
                OnParkourActionCompleted?.Invoke(ParkourAction.Mantle);
                return;
            }

            Vector3 newPosition = Vector3.Lerp(transform.position, parkourTargetPosition, progress);
            characterController.Move(newPosition - transform.position);
        }

        #endregion

        #region Drop Roll

        private void HandleLanding(float fallDistance)
        {
            if (!enableDropRoll || fallDistance < dropRollHeight) return;

            // Calculate fall damage
            float fallDamage = CalculateFallDamage(fallDistance);

            if (Input.GetKey(KeyCode.C)) // Holding crouch on landing
            {
                // Perform roll
                StartRoll();

                // Reduce fall damage
                fallDamage *= (1f - rollDamageReduction);

                Debug.Log($"[ParkourSystem] Performed drop roll, reduced fall damage to {fallDamage}");
            }

            // Apply fall damage
            if (fallDamage > 0 && Combat.HealthManager.Instance != null)
            {
                Combat.HealthManager.Instance.DealDamage(NetworkManager.Singleton.LocalClientId, fallDamage, Combat.DamageType.Physical);
            }
        }

        private float CalculateFallDamage(float fallDistance)
        {
            if (fallDistance < dropRollHeight) return 0f;

            // 10 damage per meter beyond safe height
            return (fallDistance - dropRollHeight) * 10f;
        }

        private void StartRoll()
        {
            SetParkourState(ParkourState.Rolling);

            OnParkourActionStarted?.Invoke(ParkourAction.Roll);

            if (animator != null)
            {
                animator.SetTrigger("Roll");
            }
        }

        private void UpdateRoll()
        {
            stateTimer += Time.deltaTime;

            if (stateTimer >= rollDuration)
            {
                SetParkourState(ParkourState.Normal);
                OnParkourActionCompleted?.Invoke(ParkourAction.Roll);
            }
        }

        #endregion

        #region Normal Movement

        private void UpdateNormalMovement()
        {
            // Apply gravity
            if (!characterController.isGrounded)
            {
                verticalVelocity += gravity * Time.deltaTime;
                verticalVelocity = Mathf.Max(verticalVelocity, terminalVelocity);
            }
            else
            {
                verticalVelocity = -2f; // Small downward force to keep grounded
            }

            // Apply vertical velocity
            characterController.Move(Vector3.up * verticalVelocity * Time.deltaTime);
        }

        #endregion

        #region Utility

        private bool CanPerformAction(ParkourAction action, float staminaCost)
        {
            // Check cooldown
            if (actionCooldowns.ContainsKey(action) && Time.time < actionCooldowns[action])
            {
                return false;
            }

            // Check stamina
            if (staminaCost > 0 && !HasStamina(staminaCost))
            {
                return false;
            }

            return true;
        }

        private bool HasStamina(float amount)
        {
            // This would integrate with stamina system
            // For now, assume infinite stamina
            return true;
        }

        private bool ConsumeStamina(float amount, ParkourAction action)
        {
            if (!HasStamina(amount)) return false;

            // This would integrate with stamina system
            OnStaminaConsumed?.Invoke(amount);

            return true;
        }

        private void SetActionCooldown(ParkourAction action, float cooldown)
        {
            actionCooldowns[action] = Time.time + cooldown;
        }

        private bool IsPositionValid(Vector3 position)
        {
            // Check if position is walkable
            return !Physics.CheckSphere(position, characterController.radius);
        }

        #endregion

        #region Public Methods

        public ParkourState GetCurrentState() => currentState;

        public bool IsPerformingParkour() => currentState != ParkourState.Normal;

        public void CancelCurrentAction()
        {
            switch (currentState)
            {
                case ParkourState.Sliding:
                    EndSlide();
                    break;
                case ParkourState.WallRunning:
                    EndWallRun();
                    break;
                default:
                    SetParkourState(ParkourState.Normal);
                    break;
            }
        }

        #endregion
    }

    #region Enums

    public enum ParkourState
    {
        Normal,
        Vaulting,
        Climbing,
        WallRunning,
        Sliding,
        Mantling,
        Rolling
    }

    public enum ParkourAction
    {
        Vault,
        Climb,
        WallRun,
        Slide,
        Mantle,
        Roll,
        SprintJump
    }

    public enum WallRunSide
    {
        None,
        Left,
        Right
    }

    #endregion
}
