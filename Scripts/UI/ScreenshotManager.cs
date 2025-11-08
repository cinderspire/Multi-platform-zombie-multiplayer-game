using UnityEngine;
using System.Collections;
using System.IO;

namespace DeadFrontier.UI
{
    /// <summary>
    /// Manages screenshot capture and sharing functionality
    /// Supports various platforms and social sharing
    /// </summary>
    public class ScreenshotManager : Singleton<ScreenshotManager>
    {
        [Header("Screenshot Settings")]
        [SerializeField] private KeyCode screenshotKey = KeyCode.F12;
        [SerializeField] private int superSizeMultiplier = 1; // 1 = native resolution, 2 = 2x resolution, etc.
        [SerializeField] private bool includeUI = true;
        [SerializeField] private bool playSound = true;

        [Header("Save Settings")]
        [SerializeField] private string folderName = "Screenshots";
        [SerializeField] private bool saveToGallery = true; // Mobile only

        [Header("Audio")]
        [SerializeField] private AudioClip screenshotSound;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        // State
        private bool isCapturing = false;
        private string lastScreenshotPath = "";

        // Events
        public event System.Action<string> OnScreenshotCaptured;
        public event System.Action<string> OnScreenshotSaved;

        private void Update()
        {
            if (Input.GetKeyDown(screenshotKey))
            {
                CaptureScreenshot();
            }
        }

        #region Screenshot Capture

        /// <summary>
        /// Captures a screenshot
        /// </summary>
        public void CaptureScreenshot()
        {
            if (isCapturing)
            {
                if (showDebugLogs)
                    Debug.LogWarning("[ScreenshotManager] Already capturing screenshot");
                return;
            }

            StartCoroutine(CaptureScreenshotCoroutine());
        }

        private IEnumerator CaptureScreenshotCoroutine()
        {
            isCapturing = true;

            // Wait for end of frame to capture
            yield return new WaitForEndOfFrame();

            // Generate filename
            string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string filename = $"DeadFrontier_{timestamp}.png";

            // Determine save path
            string path = GetScreenshotPath(filename);

            // Capture screenshot
            if (superSizeMultiplier > 1)
            {
                ScreenCapture.CaptureScreenshot(path, superSizeMultiplier);
            }
            else
            {
                Texture2D screenshot = CaptureScreenshotTexture();
                if (screenshot != null)
                {
                    SaveScreenshot(screenshot, path);
                    Destroy(screenshot);
                }
            }

            lastScreenshotPath = path;

            if (showDebugLogs)
                Debug.Log($"[ScreenshotManager] Screenshot saved: {path}");

            // Play sound
            if (playSound && screenshotSound != null && Core.AudioManager.Instance != null)
            {
                Core.AudioManager.Instance.PlaySFX(screenshotSound);
            }

            // Show notification
            if (NotificationManager.Instance != null)
            {
                NotificationManager.Instance.ShowNotification(
                    "Screenshot Captured!",
                    "Screenshot saved successfully",
                    new Color(0.3f, 1f, 0.3f),
                    NotificationType.Info
                );
            }

            OnScreenshotCaptured?.Invoke(path);
            OnScreenshotSaved?.Invoke(path);

            // Save to gallery on mobile
#if UNITY_ANDROID || UNITY_IOS
            if (saveToGallery)
            {
                yield return new WaitForSeconds(0.5f); // Wait for file to be written
                SaveToGallery(path);
            }
#endif

            // Track analytics
            Core.Analytics.AnalyticsManager.Instance?.TrackEvent("screenshot_captured", new System.Collections.Generic.Dictionary<string, object>
            {
                { "resolution", $"{Screen.width}x{Screen.height}" },
                { "super_size", superSizeMultiplier }
            });

            isCapturing = false;
        }

        private Texture2D CaptureScreenshotTexture()
        {
            int width = Screen.width;
            int height = Screen.height;

            Texture2D screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);

            screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            screenshot.Apply();

            return screenshot;
        }

        private void SaveScreenshot(Texture2D texture, string path)
        {
            byte[] bytes = texture.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
        }

        #endregion

        #region Paths

        private string GetScreenshotPath(string filename)
        {
#if UNITY_EDITOR
            // Save to project folder in editor
            string folder = Path.Combine(Application.dataPath, "..", folderName);
#elif UNITY_ANDROID || UNITY_IOS
            // Save to persistent data path on mobile
            string folder = Path.Combine(Application.persistentDataPath, folderName);
#else
            // Save to MyDocuments on PC
            string documentsPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
            string folder = Path.Combine(documentsPath, "DeadFrontier", folderName);
#endif

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            return Path.Combine(folder, filename);
        }

        #endregion

        #region Mobile Gallery

#if UNITY_ANDROID || UNITY_IOS
        private void SaveToGallery(string path)
        {
            if (!File.Exists(path))
            {
                if (showDebugLogs)
                    Debug.LogError($"[ScreenshotManager] File not found: {path}");
                return;
            }

#if UNITY_ANDROID
            SaveToAndroidGallery(path);
#elif UNITY_IOS
            SaveToIOSGallery(path);
#endif

            if (showDebugLogs)
                Debug.Log("[ScreenshotManager] Saved to device gallery");
        }

#if UNITY_ANDROID
        private void SaveToAndroidGallery(string path)
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaClass mediaStore = new AndroidJavaClass("android.provider.MediaStore$Images$Media"))
            {
                mediaStore.CallStatic("insertImage",
                    currentActivity.Call<AndroidJavaObject>("getContentResolver"),
                    path,
                    "DeadFrontier",
                    "Screenshot from Dead Frontier"
                );
            }
        }
#endif

#if UNITY_IOS
        private void SaveToIOSGallery(string path)
        {
            // Use Unity's NativeGallery or similar plugin
            // Placeholder - requires native iOS implementation
            if (showDebugLogs)
                Debug.Log("[ScreenshotManager] iOS gallery save not implemented");
        }
#endif
#endif

        #endregion

        #region Sharing

        /// <summary>
        /// Opens native share dialog (mobile)
        /// </summary>
        public void ShareLastScreenshot()
        {
            if (string.IsNullOrEmpty(lastScreenshotPath))
            {
                if (showDebugLogs)
                    Debug.LogWarning("[ScreenshotManager] No screenshot to share");
                return;
            }

            ShareScreenshot(lastScreenshotPath);
        }

        /// <summary>
        /// Shares a screenshot using native sharing
        /// </summary>
        public void ShareScreenshot(string path)
        {
            if (!File.Exists(path))
            {
                if (showDebugLogs)
                    Debug.LogError($"[ScreenshotManager] Screenshot not found: {path}");
                return;
            }

#if UNITY_ANDROID || UNITY_IOS
            StartCoroutine(ShareScreenshotMobile(path));
#else
            // On PC, open folder location
            System.Diagnostics.Process.Start(Path.GetDirectoryName(path));
#endif

            // Track analytics
            Core.Analytics.AnalyticsManager.Instance?.TrackEvent("screenshot_shared", null);
        }

#if UNITY_ANDROID || UNITY_IOS
        private IEnumerator ShareScreenshotMobile(string path)
        {
            yield return new WaitForEndOfFrame();

            // Use NativeShare or similar plugin for mobile sharing
            // Placeholder implementation
            if (showDebugLogs)
                Debug.Log($"[ScreenshotManager] Sharing screenshot: {path}");

            // This would typically use a plugin like NativeShare
            // new NativeShare().AddFile(path).SetSubject("Dead Frontier Screenshot").Share();
        }
#endif

        #endregion

        #region Capture Modes

        /// <summary>
        /// Captures a screenshot without UI
        /// </summary>
        public void CaptureWithoutUI()
        {
            bool originalState = includeUI;
            includeUI = false;

            // Hide UI
            Canvas[] canvases = FindObjectsOfType<Canvas>();
            foreach (var canvas in canvases)
            {
                canvas.enabled = false;
            }

            StartCoroutine(CaptureAfterDelay(0.1f, () =>
            {
                // Restore UI
                foreach (var canvas in canvases)
                {
                    canvas.enabled = true;
                }
                includeUI = originalState;
            }));
        }

        /// <summary>
        /// Captures a high-resolution screenshot
        /// </summary>
        public void CaptureHighRes()
        {
            int originalMultiplier = superSizeMultiplier;
            superSizeMultiplier = 4; // 4x resolution

            CaptureScreenshot();

            superSizeMultiplier = originalMultiplier;
        }

        private IEnumerator CaptureAfterDelay(float delay, System.Action onComplete)
        {
            yield return new WaitForSeconds(delay);
            CaptureScreenshot();
            yield return new WaitForSeconds(0.5f);
            onComplete?.Invoke();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Gets the last screenshot path
        /// </summary>
        public string GetLastScreenshotPath()
        {
            return lastScreenshotPath;
        }

        /// <summary>
        /// Opens the screenshots folder
        /// </summary>
        public void OpenScreenshotsFolder()
        {
            string folder = Path.GetDirectoryName(GetScreenshotPath("dummy.png"));

#if UNITY_EDITOR || UNITY_STANDALONE
            System.Diagnostics.Process.Start(folder);
#elif UNITY_ANDROID
            // Open gallery app
            Application.OpenURL("content://media/internal/images/media");
#endif

            if (showDebugLogs)
                Debug.Log($"[ScreenshotManager] Opening folder: {folder}");
        }

        /// <summary>
        /// Deletes all screenshots
        /// </summary>
        public void DeleteAllScreenshots()
        {
            string folder = Path.GetDirectoryName(GetScreenshotPath("dummy.png"));

            if (Directory.Exists(folder))
            {
                string[] files = Directory.GetFiles(folder, "*.png");
                foreach (var file in files)
                {
                    File.Delete(file);
                }

                if (showDebugLogs)
                    Debug.Log($"[ScreenshotManager] Deleted {files.Length} screenshots");
            }
        }

        #endregion

        #region Properties

        public bool IsCapturing => isCapturing;
        public string LastScreenshotPath => lastScreenshotPath;

        #endregion
    }
}
