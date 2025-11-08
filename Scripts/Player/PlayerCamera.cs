using UnityEngine;

namespace DeadFrontier.Player
{
    /// <summary>
    /// Handles player camera movement including mouse look and recoil
    /// </summary>
    public class PlayerCamera : MonoBehaviour
    {
        [Header("Mouse Look")]
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private float gamepadSensitivity = 120f;
        [SerializeField] private bool invertY = false;

        [Header("Camera Limits")]
        [SerializeField] private float maxLookAngle = 90f;
        [SerializeField] private float minLookAngle = -90f;

        [Header("Recoil")]
        [SerializeField] private float recoilRecoverySpeed = 5f;
        [SerializeField] private float maxRecoilX = 5f;
        [SerializeField] private float maxRecoilY = 10f;

        [Header("Camera Bob (Optional)")]
        [SerializeField] private bool enableCameraBob = false;
        [SerializeField] private float bobFrequency = 1.5f;
        [SerializeField] private float bobHorizontalAmplitude = 0.1f;
        [SerializeField] private float bobVerticalAmplitude = 0.1f;

        // Camera references
        private Transform playerBody;
        private Transform cameraTransform;

        // Rotation state
        private float xRotation = 0f;
        private float yRotation = 0f;

        // Recoil state
        private Vector2 currentRecoil;
        private Vector2 targetRecoil;

        // Camera bob state
        private float bobTimer;
        private Vector3 originalCameraPosition;

        // Properties
        public float MouseSensitivity
        {
            get => mouseSensitivity;
            set => mouseSensitivity = Mathf.Max(0.1f, value);
        }

        private void Awake()
        {
            playerBody = transform;
            cameraTransform = Camera.main.transform;

            if (cameraTransform == null)
            {
                Debug.LogError("[PlayerCamera] No main camera found!");
            }

            originalCameraPosition = cameraTransform.localPosition;
        }

        private void Start()
        {
            // Lock cursor on start
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        /// <summary>
        /// Updates camera rotation based on mouse input
        /// </summary>
        public void Look(Vector2 lookInput, bool isGamepad = false)
        {
            // Apply sensitivity
            float sensitivity = isGamepad ? gamepadSensitivity : mouseSensitivity;
            float mouseX = lookInput.x * sensitivity * (isGamepad ? Time.deltaTime : 1f);
            float mouseY = lookInput.y * sensitivity * (isGamepad ? Time.deltaTime : 1f);

            if (invertY)
            {
                mouseY = -mouseY;
            }

            // Calculate rotations
            yRotation += mouseX;
            xRotation -= mouseY;

            // Apply recoil
            xRotation -= currentRecoil.y;
            yRotation += currentRecoil.x;

            // Clamp vertical rotation
            xRotation = Mathf.Clamp(xRotation, minLookAngle, maxLookAngle);

            // Apply rotations
            cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            playerBody.localRotation = Quaternion.Euler(0f, yRotation, 0f);

            // Recover from recoil
            RecoverFromRecoil();
        }

        /// <summary>
        /// Adds recoil to the camera
        /// </summary>
        public void AddRecoil(Vector2 recoil)
        {
            targetRecoil += recoil;
            targetRecoil.x = Mathf.Clamp(targetRecoil.x, -maxRecoilX, maxRecoilX);
            targetRecoil.y = Mathf.Clamp(targetRecoil.y, -maxRecoilY, maxRecoilY);
        }

        /// <summary>
        /// Recovers camera from recoil over time
        /// </summary>
        private void RecoverFromRecoil()
        {
            currentRecoil = Vector2.Lerp(currentRecoil, targetRecoil, Time.deltaTime * 10f);
            targetRecoil = Vector2.Lerp(targetRecoil, Vector2.zero, Time.deltaTime * recoilRecoverySpeed);
        }

        /// <summary>
        /// Updates camera bob based on player movement
        /// </summary>
        public void UpdateCameraBob(bool isMoving, float currentSpeed)
        {
            if (!enableCameraBob || !isMoving)
            {
                // Return to original position
                cameraTransform.localPosition = Vector3.Lerp(
                    cameraTransform.localPosition,
                    originalCameraPosition,
                    Time.deltaTime * 5f
                );
                bobTimer = 0f;
                return;
            }

            // Update bob timer based on speed
            bobTimer += Time.deltaTime * bobFrequency * currentSpeed;

            // Calculate bob offset
            float horizontalBob = Mathf.Sin(bobTimer) * bobHorizontalAmplitude;
            float verticalBob = Mathf.Sin(bobTimer * 2f) * bobVerticalAmplitude;

            Vector3 bobOffset = new Vector3(horizontalBob, verticalBob, 0f);
            cameraTransform.localPosition = originalCameraPosition + bobOffset;
        }

        /// <summary>
        /// Applies camera shake effect
        /// </summary>
        public void Shake(float intensity, float duration)
        {
            StartCoroutine(ShakeCoroutine(intensity, duration));
        }

        private System.Collections.IEnumerator ShakeCoroutine(float intensity, float duration)
        {
            float elapsed = 0f;
            Vector3 originalPos = cameraTransform.localPosition;

            while (elapsed < duration)
            {
                float x = Random.Range(-1f, 1f) * intensity;
                float y = Random.Range(-1f, 1f) * intensity;

                cameraTransform.localPosition = originalPos + new Vector3(x, y, 0f);

                elapsed += Time.deltaTime;
                yield return null;
            }

            cameraTransform.localPosition = originalPos;
        }

        /// <summary>
        /// Sets the camera's field of view
        /// </summary>
        public void SetFOV(float fov, float duration = 0.2f)
        {
            if (duration <= 0f)
            {
                Camera.main.fieldOfView = fov;
            }
            else
            {
                StartCoroutine(ChangeFOVCoroutine(fov, duration));
            }
        }

        private System.Collections.IEnumerator ChangeFOVCoroutine(float targetFOV, float duration)
        {
            float startFOV = Camera.main.fieldOfView;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                Camera.main.fieldOfView = Mathf.Lerp(startFOV, targetFOV, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            Camera.main.fieldOfView = targetFOV;
        }

        /// <summary>
        /// Resets camera rotation
        /// </summary>
        public void ResetRotation()
        {
            xRotation = 0f;
            yRotation = 0f;
            currentRecoil = Vector2.zero;
            targetRecoil = Vector2.zero;

            cameraTransform.localRotation = Quaternion.identity;
            playerBody.localRotation = Quaternion.identity;
        }

        /// <summary>
        /// Gets the forward direction the camera is looking
        /// </summary>
        public Vector3 GetLookDirection()
        {
            return cameraTransform.forward;
        }

        /// <summary>
        /// Gets the camera's position
        /// </summary>
        public Vector3 GetCameraPosition()
        {
            return cameraTransform.position;
        }

        private void OnDisable()
        {
            // Unlock cursor when disabled
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
