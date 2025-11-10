using System;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Player
{
    /// <summary>
    /// Comprehensive camera controller with 1st/3rd person switching, mouse look,
    /// FOV control, camera shake, and smooth transitions.
    /// </summary>
    public class CameraController : NetworkBehaviour
    {
        [Header("Camera Settings")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private CameraMode defaultMode = CameraMode.ThirdPerson;
        [SerializeField] private bool allowModeSwitch = true;

        [Header("Mouse Look Settings")]
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private float aimSensitivity = 1f;
        [SerializeField] private bool invertY = false;
        [SerializeField] private bool invertX = false;
        [SerializeField] private float minVerticalAngle = -80f;
        [SerializeField] private float maxVerticalAngle = 80f;

        [Header("First Person Settings")]
        [SerializeField] private Vector3 firstPersonOffset = new Vector3(0f, 1.6f, 0f);
        [SerializeField] private float firstPersonFOV = 90f;

        [Header("Third Person Settings")]
        [SerializeField] private Vector3 thirdPersonOffset = new Vector3(0f, 1.8f, -3f);
        [SerializeField] private float thirdPersonFOV = 75f;
        [SerializeField] private float thirdPersonDistance = 3f;
        [SerializeField] private LayerMask collisionMask;
        [SerializeField] private float cameraCollisionRadius = 0.3f;

        [Header("Aiming Settings")]
        [SerializeField] private Vector3 aimOffset = new Vector3(0.5f, 1.6f, -1.5f);
        [SerializeField] private float aimFOV = 60f;
        [SerializeField] private float aimTransitionSpeed = 10f;

        [Header("Camera Shake")]
        [SerializeField] private float shakeDecay = 2f;

        [Header("Smooth Transitions")]
        [SerializeField] private float positionSmoothTime = 0.1f;
        [SerializeField] private float rotationSmoothTime = 0.1f;
        [SerializeField] private float fovTransitionSpeed = 5f;

        private Transform playerTransform;
        private PlayerController playerController;

        private CameraMode currentMode;
        private float verticalRotation;
        private float horizontalRotation;

        private Vector3 currentCameraPosition;
        private Vector3 cameraVelocity;
        private Quaternion currentCameraRotation;

        private bool isAiming;
        private float currentFOV;
        private float targetFOV;

        private Vector3 shakeOffset;
        private float shakeIntensity;

        public event Action<CameraMode> OnCameraModeChanged;

        private void Awake()
        {
            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
            }

            currentMode = defaultMode;
            targetFOV = GetFOVForMode(currentMode);
            currentFOV = targetFOV;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (!IsOwner)
            {
                // Disable camera for non-owners
                if (playerCamera != null)
                {
                    playerCamera.enabled = false;
                }
                enabled = false;
                return;
            }

            playerTransform = transform.parent;
            playerController = playerTransform.GetComponent<PlayerController>();

            // Initialize rotation from player's rotation
            horizontalRotation = playerTransform.eulerAngles.y;
            verticalRotation = 0f;

            // Lock cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Load settings
            LoadCameraSettings();
        }

        private void Update()
        {
            if (!IsOwner) return;

            HandleCameraInput();
            HandleCameraModeSwitch();
            UpdateCameraPosition();
            UpdateCameraRotation();
            UpdateFOV();
            UpdateCameraShake();
        }

        private void LateUpdate()
        {
            if (!IsOwner) return;

            ApplyCameraTransform();
        }

        private void HandleCameraInput()
        {
            // Get mouse input
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            // Apply sensitivity
            float sensitivity = isAiming ? aimSensitivity : mouseSensitivity;
            mouseX *= sensitivity;
            mouseY *= sensitivity;

            // Invert if needed
            if (invertX) mouseX *= -1f;
            if (invertY) mouseY *= -1f;

            // Update rotation
            horizontalRotation += mouseX;
            verticalRotation -= mouseY;

            // Clamp vertical rotation
            verticalRotation = Mathf.Clamp(verticalRotation, minVerticalAngle, maxVerticalAngle);

            // Handle aim input
            if (Input.GetMouseButtonDown(1)) // Right click
            {
                isAiming = true;
            }
            if (Input.GetMouseButtonUp(1))
            {
                isAiming = false;
            }
        }

        private void HandleCameraModeSwitch()
        {
            if (!allowModeSwitch) return;

            if (Input.GetKeyDown(KeyCode.V))
            {
                SwitchCameraMode();
            }
        }

        private void UpdateCameraPosition()
        {
            Vector3 targetPosition;

            if (isAiming)
            {
                targetPosition = playerTransform.position + aimOffset;
            }
            else
            {
                switch (currentMode)
                {
                    case CameraMode.FirstPerson:
                        targetPosition = playerTransform.position + firstPersonOffset;
                        break;

                    case CameraMode.ThirdPerson:
                        targetPosition = CalculateThirdPersonPosition();
                        break;

                    default:
                        targetPosition = playerTransform.position + thirdPersonOffset;
                        break;
                }
            }

            // Smooth damp to target position
            currentCameraPosition = Vector3.SmoothDamp(
                currentCameraPosition,
                targetPosition,
                ref cameraVelocity,
                positionSmoothTime
            );
        }

        private Vector3 CalculateThirdPersonPosition()
        {
            // Calculate ideal third person position
            Vector3 targetOffset = thirdPersonOffset;
            Quaternion rotation = Quaternion.Euler(verticalRotation, horizontalRotation, 0f);
            Vector3 desiredPosition = playerTransform.position + playerTransform.up * thirdPersonOffset.y;
            desiredPosition += rotation * Vector3.back * thirdPersonDistance;

            // Check for camera collision
            Vector3 direction = desiredPosition - playerTransform.position;
            float distance = direction.magnitude;

            if (Physics.SphereCast(
                playerTransform.position + playerTransform.up * thirdPersonOffset.y,
                cameraCollisionRadius,
                direction.normalized,
                out RaycastHit hit,
                distance,
                collisionMask
            ))
            {
                // Collision detected, move camera closer
                desiredPosition = hit.point - direction.normalized * cameraCollisionRadius;
            }

            return desiredPosition;
        }

        private void UpdateCameraRotation()
        {
            Quaternion targetRotation = Quaternion.Euler(verticalRotation, horizontalRotation, 0f);
            currentCameraRotation = Quaternion.Slerp(
                currentCameraRotation,
                targetRotation,
                rotationSmoothTime * 60f * Time.deltaTime
            );
        }

        private void UpdateFOV()
        {
            if (isAiming)
            {
                targetFOV = aimFOV;
            }
            else
            {
                targetFOV = GetFOVForMode(currentMode);
            }

            currentFOV = Mathf.Lerp(currentFOV, targetFOV, fovTransitionSpeed * Time.deltaTime);

            if (playerCamera != null)
            {
                playerCamera.fieldOfView = currentFOV;
            }
        }

        private void UpdateCameraShake()
        {
            if (shakeIntensity > 0f)
            {
                // Generate random shake offset
                shakeOffset = UnityEngine.Random.insideUnitSphere * shakeIntensity;

                // Decay shake intensity
                shakeIntensity = Mathf.Max(0f, shakeIntensity - shakeDecay * Time.deltaTime);
            }
            else
            {
                shakeOffset = Vector3.zero;
            }
        }

        private void ApplyCameraTransform()
        {
            if (playerCamera == null) return;

            // Apply position with shake
            transform.position = currentCameraPosition + shakeOffset;

            // Apply rotation
            transform.rotation = currentCameraRotation;
        }

        private float GetFOVForMode(CameraMode mode)
        {
            switch (mode)
            {
                case CameraMode.FirstPerson:
                    return firstPersonFOV;
                case CameraMode.ThirdPerson:
                    return thirdPersonFOV;
                default:
                    return 75f;
            }
        }

        public void SwitchCameraMode()
        {
            currentMode = currentMode == CameraMode.FirstPerson ? CameraMode.ThirdPerson : CameraMode.FirstPerson;
            OnCameraModeChanged?.Invoke(currentMode);
        }

        public void SetCameraMode(CameraMode mode)
        {
            if (currentMode != mode)
            {
                currentMode = mode;
                OnCameraModeChanged?.Invoke(currentMode);
            }
        }

        public void ShakeCamera(float intensity, float duration)
        {
            shakeIntensity = Mathf.Max(shakeIntensity, intensity);
        }

        public void SetAiming(bool aiming)
        {
            isAiming = aiming;
        }

        private void LoadCameraSettings()
        {
            // Would load from SettingsSystem
            if (Settings.SettingsSystem.Instance != null)
            {
                var settings = Settings.SettingsSystem.Instance.GetPlayerSettings(OwnerClientId);
                if (settings != null && settings.controls != null)
                {
                    mouseSensitivity = settings.controls.mouseSensitivity;
                    aimSensitivity = settings.controls.aimSensitivity;
                    invertY = settings.controls.invertY;
                    invertX = settings.controls.invertX;
                }

                if (settings != null && settings.graphics != null)
                {
                    firstPersonFOV = settings.graphics.fovValue;
                    thirdPersonFOV = settings.graphics.fovValue - 15f;
                }
            }
        }

        // Public getters
        public CameraMode CurrentMode => currentMode;
        public bool IsAiming => isAiming;
        public float VerticalRotation => verticalRotation;
        public float HorizontalRotation => horizontalRotation;
        public Camera Camera => playerCamera;
    }

    public enum CameraMode
    {
        FirstPerson,
        ThirdPerson
    }
}
