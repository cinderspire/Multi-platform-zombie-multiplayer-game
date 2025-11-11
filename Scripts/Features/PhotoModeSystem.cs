using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace ZombieGame
{
    /// <summary>
    /// Photo Mode System - Pause gameplay and take stunning screenshots
    /// Features: Free camera, filters, stickers, depth of field, time freeze
    /// Share directly to social media with watermark options
    /// </summary>
    public class PhotoModeSystem : MonoBehaviour
    {
        public static PhotoModeSystem Instance { get; private set; }

        [Header("Photo Mode Settings")]
        [SerializeField] private KeyCode photoModeKey = KeyCode.F6;
        [SerializeField] private KeyCode captureKey = KeyCode.Space;
        [SerializeField] private bool allowInMultiplayer = false; // Only in replays

        [Header("Camera Settings")]
        [SerializeField] private float cameraMoveSpeed = 5f;
        [SerializeField] private float cameraRotateSpeed = 2f;
        [SerializeField] private float minFOV = 10f;
        [SerializeField] private float maxFOV = 120f;

        private bool isPhotoModeActive = false;
        private Camera photoCamera;
        private Vector3 originalCameraPosition;
        private Quaternion originalCameraRotation;
        private float originalFOV;
        private float originalTimeScale;

        // Photo Mode State
        private PhotoModeSettings currentSettings = new PhotoModeSettings();

        // Saved Photos
        private List<PhotoData> savedPhotos = new List<PhotoData>();

        // Events
        public event System.Action OnPhotoModeEntered;
        public event System.Action OnPhotoModeExited;
        public event System.Action<string> OnPhotoCapture;

        [System.Serializable]
        public class PhotoModeSettings
        {
            // Camera
            public float fieldOfView = 60f;
            public float cameraRoll = 0f;

            // Post Processing
            public PhotoFilter activeFilter = PhotoFilter.None;
            public float filterIntensity = 1.0f;
            public float brightness = 0f;
            public float contrast = 0f;
            public float saturation = 0f;
            public float vignette = 0f;

            // Depth of Field
            public bool enableDOF = false;
            public float focusDistance = 10f;
            public float aperture = 5.6f;

            // Effects
            public bool hideUI = true;
            public bool hidePlayer = false;
            public bool freezeAnimation = true;
            public bool showGrid = false;
            public bool showRuleOfThirds = false;

            // Stickers/Overlays
            public List<string> activeStickers = new List<string>();

            // Pose (for character photos)
            public string characterPose = "idle";
        }

        public enum PhotoFilter
        {
            None,
            Noir,           // Black & white
            Sepia,          // Old photo
            Vibrant,        // Saturated colors
            Dramatic,       // High contrast
            Vintage,        // Faded colors
            CinematicWarm,  // Orange/teal
            CinematicCool,  // Blue tones
            Horror,         // Desaturated with red tint
            NightVision,    // Green monochrome
            Thermal         // Heat vision
        }

        [System.Serializable]
        public class PhotoData
        {
            public string photoId;
            public string filePath;
            public System.DateTime captureDate;
            public PhotoModeSettings settings;
            public int resolutionWidth;
            public int resolutionHeight;
            public string mapName;
            public string gameMode;
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                LoadSavedPhotos();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            // Toggle photo mode
            if (Input.GetKeyDown(photoModeKey))
            {
                if (!isPhotoModeActive)
                {
                    EnterPhotoMode();
                }
                else
                {
                    ExitPhotoMode();
                }
            }

            if (isPhotoModeActive)
            {
                UpdatePhotoMode();
            }
        }

        private void EnterPhotoMode()
        {
            // Check if allowed
            if (!CanEnterPhotoMode())
            {
                Debug.LogWarning("[PhotoMode] Cannot enter photo mode in multiplayer");
                return;
            }

            isPhotoModeActive = true;

            // Pause game
            originalTimeScale = Time.timeScale;
            if (currentSettings.freezeAnimation)
            {
                Time.timeScale = 0f;
            }

            // Setup camera
            SetupPhotoCamera();

            // Hide UI if needed
            if (currentSettings.hideUI)
            {
                // Canvas.SetActive(false);
            }

            OnPhotoModeEntered?.Invoke();
            Debug.Log("[PhotoMode] Photo mode activated");
        }

        private void ExitPhotoMode()
        {
            isPhotoModeActive = false;

            // Restore time
            Time.timeScale = originalTimeScale;

            // Restore camera
            if (photoCamera != null)
            {
                Camera.main.transform.position = originalCameraPosition;
                Camera.main.transform.rotation = originalCameraRotation;
                Camera.main.fieldOfView = originalFOV;
            }

            // Show UI
            // Canvas.SetActive(true);

            OnPhotoModeExited?.Invoke();
            Debug.Log("[PhotoMode] Photo mode deactivated");
        }

        private void SetupPhotoCamera()
        {
            photoCamera = Camera.main;

            if (photoCamera != null)
            {
                originalCameraPosition = photoCamera.transform.position;
                originalCameraRotation = photoCamera.transform.rotation;
                originalFOV = photoCamera.fieldOfView;
            }
        }

        private void UpdatePhotoMode()
        {
            // Camera movement
            UpdateCameraMovement();

            // Camera rotation
            UpdateCameraRotation();

            // FOV adjustment
            UpdateFOV();

            // Filters
            if (Input.GetKeyDown(KeyCode.F))
            {
                CycleFilter();
            }

            // Capture photo
            if (Input.GetKeyDown(captureKey))
            {
                CapturePhoto();
            }

            // Toggle DOF
            if (Input.GetKeyDown(KeyCode.D))
            {
                currentSettings.enableDOF = !currentSettings.enableDOF;
            }

            // Toggle UI
            if (Input.GetKeyDown(KeyCode.H))
            {
                currentSettings.hideUI = !currentSettings.hideUI;
            }

            // Toggle grid
            if (Input.GetKeyDown(KeyCode.G))
            {
                currentSettings.showGrid = !currentSettings.showGrid;
            }
        }

        private void UpdateCameraMovement()
        {
            if (photoCamera == null) return;

            Vector3 movement = Vector3.zero;

            // WASD movement
            if (Input.GetKey(KeyCode.W)) movement += photoCamera.transform.forward;
            if (Input.GetKey(KeyCode.S)) movement -= photoCamera.transform.forward;
            if (Input.GetKey(KeyCode.A)) movement -= photoCamera.transform.right;
            if (Input.GetKey(KeyCode.D)) movement += photoCamera.transform.right;

            // Q/E up/down
            if (Input.GetKey(KeyCode.Q)) movement += Vector3.down;
            if (Input.GetKey(KeyCode.E)) movement += Vector3.up;

            // Apply movement (unscaled time)
            float speed = cameraMoveSpeed;
            if (Input.GetKey(KeyCode.LeftShift))
            {
                speed *= 3f; // Sprint
            }

            photoCamera.transform.position += movement.normalized * speed * Time.unscaledDeltaTime;
        }

        private void UpdateCameraRotation()
        {
            if (photoCamera == null) return;

            if (Input.GetMouseButton(1)) // Right mouse button
            {
                float mouseX = Input.GetAxis("Mouse X") * cameraRotateSpeed;
                float mouseY = Input.GetAxis("Mouse Y") * cameraRotateSpeed;

                photoCamera.transform.Rotate(Vector3.up, mouseX, Space.World);
                photoCamera.transform.Rotate(Vector3.left, mouseY, Space.Self);
            }

            // Camera roll (Z/C keys)
            if (Input.GetKey(KeyCode.Z))
            {
                currentSettings.cameraRoll -= 30f * Time.unscaledDeltaTime;
                photoCamera.transform.Rotate(Vector3.forward, -30f * Time.unscaledDeltaTime);
            }
            if (Input.GetKey(KeyCode.C))
            {
                currentSettings.cameraRoll += 30f * Time.unscaledDeltaTime;
                photoCamera.transform.Rotate(Vector3.forward, 30f * Time.unscaledDeltaTime);
            }
        }

        private void UpdateFOV()
        {
            if (photoCamera == null) return;

            float scrollDelta = Input.mouseScrollDelta.y;

            if (scrollDelta != 0f)
            {
                currentSettings.fieldOfView -= scrollDelta * 5f;
                currentSettings.fieldOfView = Mathf.Clamp(currentSettings.fieldOfView, minFOV, maxFOV);
                photoCamera.fieldOfView = currentSettings.fieldOfView;
            }
        }

        private void CycleFilter()
        {
            int currentFilterIndex = (int)currentSettings.activeFilter;
            int nextFilterIndex = (currentFilterIndex + 1) % System.Enum.GetValues(typeof(PhotoFilter)).Length;
            currentSettings.activeFilter = (PhotoFilter)nextFilterIndex;

            ApplyFilter(currentSettings.activeFilter);
            Debug.Log($"[PhotoMode] Filter: {currentSettings.activeFilter}");
        }

        private void ApplyFilter(PhotoFilter filter)
        {
            // In real implementation, would apply post-processing effects
            switch (filter)
            {
                case PhotoFilter.Noir:
                    // Black & white
                    currentSettings.saturation = -100f;
                    currentSettings.contrast = 20f;
                    break;

                case PhotoFilter.Sepia:
                    // Warm, faded tones
                    currentSettings.saturation = -50f;
                    currentSettings.brightness = -10f;
                    break;

                case PhotoFilter.Vibrant:
                    currentSettings.saturation = 50f;
                    currentSettings.contrast = 10f;
                    break;

                case PhotoFilter.Dramatic:
                    currentSettings.contrast = 40f;
                    currentSettings.vignette = 0.5f;
                    break;

                case PhotoFilter.Horror:
                    currentSettings.saturation = -30f;
                    currentSettings.brightness = -20f;
                    break;

                default:
                    // Reset to default
                    currentSettings.saturation = 0f;
                    currentSettings.contrast = 0f;
                    currentSettings.brightness = 0f;
                    currentSettings.vignette = 0f;
                    break;
            }
        }

        private void CapturePhoto()
        {
            string photoId = System.Guid.NewGuid().ToString();
            string fileName = $"Photo_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";
            string filePath = Path.Combine(Application.persistentDataPath, "Photos", fileName);

            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            // Capture screenshot (simplified - real implementation would use RenderTexture)
            ScreenCapture.CaptureScreenshot(filePath, 2); // 2x supersampling

            // Save photo data
            var photoData = new PhotoData
            {
                photoId = photoId,
                filePath = filePath,
                captureDate = System.DateTime.Now,
                settings = currentSettings,
                resolutionWidth = Screen.width * 2,
                resolutionHeight = Screen.height * 2,
                mapName = "Current Map", // Would get from MapSystem
                gameMode = "Current Mode" // Would get from GameModeSystem
            };

            savedPhotos.Add(photoData);
            SavePhotoMetadata();

            OnPhotoCapture?.Invoke(filePath);
            Debug.Log($"[PhotoMode] Photo captured: {fileName}");
        }

        private bool CanEnterPhotoMode()
        {
            // Check if in single-player or replay mode
            if (UnityEngine.Networking.NetworkServer.active && !allowInMultiplayer)
            {
                return false;
            }

            // Check if in replay
            if (ReplayEditorSystem.Instance != null && ReplayEditorSystem.Instance.IsPlayingReplay())
            {
                return true;
            }

            return true; // Allow in single-player
        }

        private void SavePhotoMetadata()
        {
            // Save list of photos to JSON
            string metadataPath = Path.Combine(Application.persistentDataPath, "Photos", "metadata.json");

            // Simplified - real implementation would serialize properly
            // File.WriteAllText(metadataPath, JsonUtility.ToJson(savedPhotos));
        }

        private void LoadSavedPhotos()
        {
            string metadataPath = Path.Combine(Application.persistentDataPath, "Photos", "metadata.json");

            if (File.Exists(metadataPath))
            {
                // Load photo metadata
                // savedPhotos = JsonUtility.FromJson<List<PhotoData>>(File.ReadAllText(metadataPath));
            }
        }

        // Public API

        public void SetFilter(PhotoFilter filter)
        {
            currentSettings.activeFilter = filter;
            ApplyFilter(filter);
        }

        public void SetDOFEnabled(bool enabled)
        {
            currentSettings.enableDOF = enabled;
        }

        public void SetFocusDistance(float distance)
        {
            currentSettings.focusDistance = distance;
        }

        public void SetAperture(float aperture)
        {
            currentSettings.aperture = aperture;
        }

        public void AddSticker(string stickerId)
        {
            if (!currentSettings.activeStickers.Contains(stickerId))
            {
                currentSettings.activeStickers.Add(stickerId);
            }
        }

        public void RemoveSticker(string stickerId)
        {
            currentSettings.activeStickers.Remove(stickerId);
        }

        public void ShareToSocialMedia(string photoId, string platform)
        {
            // Integration with social media APIs
            Debug.Log($"[PhotoMode] Sharing photo {photoId} to {platform}");
        }

        // Getters

        public bool IsPhotoModeActive()
        {
            return isPhotoModeActive;
        }

        public List<PhotoData> GetSavedPhotos()
        {
            return savedPhotos;
        }

        public PhotoModeSettings GetCurrentSettings()
        {
            return currentSettings;
        }
    }
}
