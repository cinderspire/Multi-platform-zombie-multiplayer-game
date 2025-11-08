using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
using System.IO;
using System;

namespace DeadFrontier.PhotoMode
{
    /// <summary>
    /// Comprehensive photo mode system for capturing cinematic screenshots.
    /// Supports camera freeflight, filters, DOF, time control, and high-resolution capture.
    /// Includes photo gallery, metadata, and social sharing integration.
    /// </summary>
    public class PhotoModeSystem : MonoBehaviour
    {
        public static PhotoModeSystem Instance { get; private set; }

        [Header("Controls")]
        [SerializeField] private KeyCode photoModeKey = KeyCode.F6;
        [SerializeField] private KeyCode captureKey = KeyCode.F12;
        [SerializeField] private KeyCode hideUIKey = KeyCode.H;

        [Header("Camera Settings")]
        [SerializeField] private Camera photoCamera;
        [SerializeField] private float cameraMoveSpeed = 10f;
        [SerializeField] private float cameraRotationSpeed = 100f;
        [SerializeField] private float cameraZoomSpeed = 50f;
        [SerializeField] private float minFOV = 10f;
        [SerializeField] private float maxFOV = 120f;

        [Header("Screenshot Settings")]
        [SerializeField] private int screenshotWidth = 3840;
        [SerializeField] private int screenshotHeight = 2160;
        [SerializeField] private int superSampleMultiplier = 2;
        [SerializeField] private string screenshotFolder = "Screenshots";

        [Header("Filters")]
        [SerializeField] private PhotoFilter[] availableFilters;
        [SerializeField] private Volume postProcessVolume;

        [Header("UI")]
        [SerializeField] private GameObject photoModeUI;
        [SerializeField] private GameObject gameUI;

        // State
        private bool isPhotoModeActive;
        private bool isUIHidden;
        private float originalTimeScale;
        private Vector3 originalCameraPosition;
        private Quaternion originalCameraRotation;

        // Camera control
        private float currentFOV = 60f;
        private float currentRoll;

        // Photo settings
        private int currentFilterIndex;
        private float currentDOFIntensity;
        private float currentDOFFocalLength = 10f;
        private float currentExposure = 1f;
        private float currentSaturation = 1f;
        private float currentContrast = 1f;
        private float currentVignette;

        // Post processing
        private DepthOfField depthOfField;
        private ColorAdjustments colorAdjustments;
        private Vignette vignette;
        private Bloom bloom;

        // Photo gallery
        private List<PhotoMetadata> photoGallery = new List<PhotoMetadata>();

        // Events
        public event Action OnPhotoModeEntered;
        public event Action OnPhotoModeExited;
        public event Action<string> OnScreenshotCaptured;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            InitializePhotoMode();
            LoadPhotoGallery();
        }

        private void Update()
        {
            HandlePhotoModeInput();

            if (isPhotoModeActive)
            {
                UpdateCameraControls();
            }
        }

        #region Initialization

        private void InitializePhotoMode()
        {
            if (photoCamera == null)
            {
                // Create dedicated photo camera
                GameObject camObj = new GameObject("PhotoModeCamera");
                photoCamera = camObj.AddComponent<Camera>();
                photoCamera.enabled = false;
            }

            // Get post-processing components
            if (postProcessVolume != null)
            {
                postProcessVolume.profile.TryGet(out depthOfField);
                postProcessVolume.profile.TryGet(out colorAdjustments);
                postProcessVolume.profile.TryGet(out vignette);
                postProcessVolume.profile.TryGet(out bloom);
            }

            // Ensure screenshot folder exists
            string screenshotPath = Path.Combine(Application.persistentDataPath, screenshotFolder);
            if (!Directory.Exists(screenshotPath))
            {
                Directory.CreateDirectory(screenshotPath);
            }

            Debug.Log($"[PhotoModeSystem] Initialized. Screenshots will be saved to: {screenshotPath}");
        }

        #endregion

        #region Photo Mode Toggle

        private void HandlePhotoModeInput()
        {
            // Toggle photo mode
            if (Input.GetKeyDown(photoModeKey))
            {
                if (isPhotoModeActive)
                {
                    ExitPhotoMode();
                }
                else
                {
                    EnterPhotoMode();
                }
            }

            // Capture screenshot
            if (isPhotoModeActive && Input.GetKeyDown(captureKey))
            {
                CaptureScreenshot();
            }

            // Toggle UI
            if (isPhotoModeActive && Input.GetKeyDown(hideUIKey))
            {
                ToggleUI();
            }
        }

        public void EnterPhotoMode()
        {
            if (isPhotoModeActive) return;

            isPhotoModeActive = true;

            // Store original settings
            originalTimeScale = Time.timeScale;
            originalCameraPosition = Camera.main.transform.position;
            originalCameraRotation = Camera.main.transform.rotation;

            // Freeze time
            Time.timeScale = 0f;

            // Setup photo camera
            photoCamera.transform.position = Camera.main.transform.position;
            photoCamera.transform.rotation = Camera.main.transform.rotation;
            photoCamera.fieldOfView = Camera.main.fieldOfView;
            currentFOV = photoCamera.fieldOfView;

            photoCamera.enabled = true;
            Camera.main.enabled = false;

            // Show photo mode UI
            if (photoModeUI != null)
            {
                photoModeUI.SetActive(true);
            }

            // Hide game UI
            if (gameUI != null)
            {
                gameUI.SetActive(false);
            }

            OnPhotoModeEntered?.Invoke();

            Debug.Log("[PhotoModeSystem] Entered photo mode");
        }

        public void ExitPhotoMode()
        {
            if (!isPhotoModeActive) return;

            isPhotoModeActive = false;

            // Restore time
            Time.timeScale = originalTimeScale;

            // Restore camera
            photoCamera.enabled = false;
            Camera.main.enabled = true;

            // Reset post-processing
            ResetPostProcessing();

            // Hide photo mode UI
            if (photoModeUI != null)
            {
                photoModeUI.SetActive(false);
            }

            // Show game UI
            if (gameUI != null)
            {
                gameUI.SetActive(true);
            }

            OnPhotoModeExited?.Invoke();

            Debug.Log("[PhotoModeSystem] Exited photo mode");
        }

        #endregion

        #region Camera Controls

        private void UpdateCameraControls()
        {
            // Movement (WASD + QE for up/down)
            Vector3 movement = Vector3.zero;

            if (Input.GetKey(KeyCode.W))
                movement += photoCamera.transform.forward;
            if (Input.GetKey(KeyCode.S))
                movement -= photoCamera.transform.forward;
            if (Input.GetKey(KeyCode.A))
                movement -= photoCamera.transform.right;
            if (Input.GetKey(KeyCode.D))
                movement += photoCamera.transform.right;
            if (Input.GetKey(KeyCode.Q))
                movement -= photoCamera.transform.up;
            if (Input.GetKey(KeyCode.E))
                movement += photoCamera.transform.up;

            float speed = Input.GetKey(KeyCode.LeftShift) ? cameraMoveSpeed * 2f : cameraMoveSpeed;
            photoCamera.transform.position += movement.normalized * speed * Time.unscaledDeltaTime;

            // Rotation (Mouse)
            if (Input.GetMouseButton(1)) // Right mouse button
            {
                float rotX = Input.GetAxis("Mouse X") * cameraRotationSpeed * Time.unscaledDeltaTime;
                float rotY = -Input.GetAxis("Mouse Y") * cameraRotationSpeed * Time.unscaledDeltaTime;

                photoCamera.transform.Rotate(Vector3.up, rotX, Space.World);
                photoCamera.transform.Rotate(Vector3.right, rotY, Space.Self);
            }

            // Roll (Z/X keys)
            if (Input.GetKey(KeyCode.Z))
                currentRoll += cameraRotationSpeed * Time.unscaledDeltaTime;
            if (Input.GetKey(KeyCode.X))
                currentRoll -= cameraRotationSpeed * Time.unscaledDeltaTime;

            Vector3 currentEuler = photoCamera.transform.eulerAngles;
            photoCamera.transform.eulerAngles = new Vector3(currentEuler.x, currentEuler.y, currentRoll);

            // FOV (Mouse scroll)
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0)
            {
                currentFOV -= scroll * cameraZoomSpeed;
                currentFOV = Mathf.Clamp(currentFOV, minFOV, maxFOV);
                photoCamera.fieldOfView = currentFOV;
            }

            // Reset camera (R key)
            if (Input.GetKeyDown(KeyCode.R))
            {
                ResetCamera();
            }
        }

        private void ResetCamera()
        {
            photoCamera.transform.position = originalCameraPosition;
            photoCamera.transform.rotation = originalCameraRotation;
            currentRoll = 0f;
            currentFOV = 60f;
            photoCamera.fieldOfView = currentFOV;

            Debug.Log("[PhotoModeSystem] Reset camera to original position");
        }

        #endregion

        #region Screenshot Capture

        public void CaptureScreenshot()
        {
            if (!isPhotoModeActive)
            {
                Debug.LogWarning("[PhotoModeSystem] Cannot capture screenshot outside of photo mode");
                return;
            }

            StartCoroutine(CaptureScreenshotCoroutine());
        }

        private System.Collections.IEnumerator CaptureScreenshotCoroutine()
        {
            // Hide UI if needed
            bool wasUIHidden = isUIHidden;
            if (!isUIHidden)
            {
                HideUI();
            }

            yield return new WaitForEndOfFrame();

            // Create render texture
            int width = screenshotWidth * superSampleMultiplier;
            int height = screenshotHeight * superSampleMultiplier;

            RenderTexture rt = new RenderTexture(width, height, 24);
            photoCamera.targetTexture = rt;

            // Render
            photoCamera.Render();

            // Read pixels
            RenderTexture.active = rt;
            Texture2D screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
            screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            screenshot.Apply();

            // Cleanup
            photoCamera.targetTexture = null;
            RenderTexture.active = null;
            Destroy(rt);

            // Downscale if supersampled
            if (superSampleMultiplier > 1)
            {
                screenshot = DownscaleTexture(screenshot, screenshotWidth, screenshotHeight);
            }

            // Save screenshot
            string filename = SaveScreenshot(screenshot);

            // Restore UI
            if (!wasUIHidden)
            {
                ShowUI();
            }

            OnScreenshotCaptured?.Invoke(filename);

            Debug.Log($"[PhotoModeSystem] Screenshot captured: {filename}");
        }

        private Texture2D DownscaleTexture(Texture2D source, int targetWidth, int targetHeight)
        {
            RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight);
            rt.filterMode = FilterMode.Bilinear;

            RenderTexture.active = rt;
            Graphics.Blit(source, rt);

            Texture2D result = new Texture2D(targetWidth, targetHeight, TextureFormat.RGB24, false);
            result.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
            result.Apply();

            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);

            Destroy(source);

            return result;
        }

        private string SaveScreenshot(Texture2D screenshot)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string filename = $"Screenshot_{timestamp}.png";
            string fullPath = Path.Combine(Application.persistentDataPath, screenshotFolder, filename);

            byte[] bytes = screenshot.EncodeToPNG();
            File.WriteAllBytes(fullPath, bytes);

            Destroy(screenshot);

            // Save metadata
            SavePhotoMetadata(filename);

            return filename;
        }

        #endregion

        #region Filters and Effects

        public void ApplyFilter(int filterIndex)
        {
            if (filterIndex < 0 || filterIndex >= availableFilters.Length) return;

            currentFilterIndex = filterIndex;
            var filter = availableFilters[filterIndex];

            // Apply filter settings
            if (colorAdjustments != null)
            {
                colorAdjustments.saturation.value = filter.saturation;
                colorAdjustments.contrast.value = filter.contrast;
            }

            if (vignette != null)
            {
                vignette.intensity.value = filter.vignetteIntensity;
            }

            Debug.Log($"[PhotoModeSystem] Applied filter: {filter.filterName}");
        }

        public void SetDepthOfField(float intensity, float focalLength)
        {
            if (depthOfField == null) return;

            currentDOFIntensity = intensity;
            currentDOFFocalLength = focalLength;

            depthOfField.active = intensity > 0f;
            depthOfField.focusDistance.value = focalLength;

            Debug.Log($"[PhotoModeSystem] Set DOF: intensity={intensity}, focal={focalLength}");
        }

        public void SetExposure(float exposure)
        {
            currentExposure = exposure;

            if (colorAdjustments != null)
            {
                colorAdjustments.postExposure.value = Mathf.Log(exposure, 2f);
            }
        }

        public void SetSaturation(float saturation)
        {
            currentSaturation = saturation;

            if (colorAdjustments != null)
            {
                colorAdjustments.saturation.value = (saturation - 1f) * 100f;
            }
        }

        public void SetContrast(float contrast)
        {
            currentContrast = contrast;

            if (colorAdjustments != null)
            {
                colorAdjustments.contrast.value = (contrast - 1f) * 100f;
            }
        }

        public void SetVignette(float intensity)
        {
            currentVignette = intensity;

            if (vignette != null)
            {
                vignette.intensity.value = intensity;
            }
        }

        private void ResetPostProcessing()
        {
            if (depthOfField != null)
            {
                depthOfField.active = false;
            }

            if (colorAdjustments != null)
            {
                colorAdjustments.saturation.value = 0f;
                colorAdjustments.contrast.value = 0f;
                colorAdjustments.postExposure.value = 0f;
            }

            if (vignette != null)
            {
                vignette.intensity.value = 0f;
            }
        }

        #endregion

        #region UI Management

        private void ToggleUI()
        {
            if (isUIHidden)
            {
                ShowUI();
            }
            else
            {
                HideUI();
            }
        }

        private void HideUI()
        {
            isUIHidden = true;

            if (photoModeUI != null)
            {
                photoModeUI.SetActive(false);
            }
        }

        private void ShowUI()
        {
            isUIHidden = false;

            if (photoModeUI != null)
            {
                photoModeUI.SetActive(true);
            }
        }

        #endregion

        #region Time Control

        public void SetTimeScale(float timeScale)
        {
            if (!isPhotoModeActive) return;

            Time.timeScale = timeScale;

            Debug.Log($"[PhotoModeSystem] Set time scale: {timeScale}");
        }

        public void FreezeTime()
        {
            SetTimeScale(0f);
        }

        public void SlowMotion(float scale = 0.1f)
        {
            SetTimeScale(scale);
        }

        #endregion

        #region Photo Gallery

        private void SavePhotoMetadata(string filename)
        {
            var metadata = new PhotoMetadata
            {
                filename = filename,
                timestamp = DateTime.Now.ToString(),
                cameraPosition = photoCamera.transform.position,
                cameraRotation = photoCamera.transform.rotation.eulerAngles,
                fov = currentFOV,
                filterIndex = currentFilterIndex,
                dofIntensity = currentDOFIntensity,
                exposure = currentExposure
            };

            photoGallery.Add(metadata);

            SavePhotoGallery();
        }

        private void LoadPhotoGallery()
        {
            if (Core.SaveSystem.Instance == null) return;

            string savedData = Core.SaveSystem.Instance.LoadData("photo_gallery");
            if (!string.IsNullOrEmpty(savedData))
            {
                try
                {
                    var gallerySave = JsonUtility.FromJson<PhotoGallerySaveData>(savedData);
                    photoGallery = gallerySave.photos;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[PhotoModeSystem] Error loading photo gallery: {e.Message}");
                }
            }
        }

        private void SavePhotoGallery()
        {
            if (Core.SaveSystem.Instance == null) return;

            var gallerySave = new PhotoGallerySaveData
            {
                photos = photoGallery
            };

            string json = JsonUtility.ToJson(gallerySave);
            Core.SaveSystem.Instance.SaveData("photo_gallery", json);
        }

        public List<PhotoMetadata> GetPhotoGallery() => new List<PhotoMetadata>(photoGallery);

        public string GetScreenshotPath(string filename)
        {
            return Path.Combine(Application.persistentDataPath, screenshotFolder, filename);
        }

        #endregion

        #region Public Getters

        public bool IsPhotoModeActive() => isPhotoModeActive;

        public float GetCurrentFOV() => currentFOV;

        public int GetCurrentFilterIndex() => currentFilterIndex;

        public PhotoFilter GetCurrentFilter()
        {
            if (currentFilterIndex < 0 || currentFilterIndex >= availableFilters.Length) return null;
            return availableFilters[currentFilterIndex];
        }

        public PhotoFilter[] GetAvailableFilters() => availableFilters;

        #endregion
    }

    #region Data Classes

    [System.Serializable]
    public class PhotoFilter
    {
        public string filterName;
        public float saturation = 0f;
        public float contrast = 0f;
        public float vignetteIntensity = 0f;
        public Sprite filterIcon;
    }

    [System.Serializable]
    public class PhotoMetadata
    {
        public string filename;
        public string timestamp;
        public Vector3 cameraPosition;
        public Vector3 cameraRotation;
        public float fov;
        public int filterIndex;
        public float dofIntensity;
        public float exposure;
    }

    [System.Serializable]
    public class PhotoGallerySaveData
    {
        public List<PhotoMetadata> photos;
    }

    #endregion
}
