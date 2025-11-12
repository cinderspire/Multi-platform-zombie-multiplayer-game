using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

namespace ZombieGame
{
    /// <summary>
    /// Screenshot Manager - High-quality screenshot capture and management
    /// Features: Multiple resolutions, burst mode, metadata, gallery integration
    /// Essential for content creators and social sharing
    /// </summary>
    public class ScreenshotManager : MonoBehaviour
    {
        public static ScreenshotManager Instance { get; private set; }

        [Header("Screenshot Settings")]
        [SerializeField] private int superSampleMultiplier = 2;
        [SerializeField] private KeyCode screenshotHotkey = KeyCode.F12;
        [SerializeField] private bool includeUI = true;
        [SerializeField] private bool playShutterSound = true;

        private string screenshotDirectory;
        private List<ScreenshotInfo> screenshots = new List<ScreenshotInfo>();

        // Events
        public event Action<ScreenshotInfo> OnScreenshotCaptured;

        [Serializable]
        public class ScreenshotInfo
        {
            public string filename;
            public string fullPath;
            public DateTime captureTime;
            public int width;
            public int height;
            public long fileSize;
            public string location;
            public string gameMode;
        }

        public enum ScreenshotResolution
        {
            Native,      // Current screen resolution
            HD_720p,     // 1280x720
            FullHD_1080p,// 1920x1080
            QHD_1440p,   // 2560x1440
            UHD_4K,      // 3840x2160
            UHD_8K       // 7680x4320
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeScreenshotManager();
            }
            else { Destroy(gameObject); }
        }

        private void Update()
        {
            if (Input.GetKeyDown(screenshotHotkey))
            {
                CaptureScreenshot();
            }
        }

        private void InitializeScreenshotManager()
        {
            screenshotDirectory = Path.Combine(Application.persistentDataPath, "Screenshots");
            Directory.CreateDirectory(screenshotDirectory);

            Debug.Log($"[Screenshot] Screenshot directory: {screenshotDirectory}");
        }

        // Screenshot Capture

        public void CaptureScreenshot(ScreenshotResolution resolution = ScreenshotResolution.Native)
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string filename = $"screenshot_{timestamp}.png";
            string fullPath = Path.Combine(screenshotDirectory, filename);

            int width, height;
            GetResolution(resolution, out width, out height);

            if (superSampleMultiplier > 1)
            {
                ScreenCapture.CaptureScreenshot(fullPath, superSampleMultiplier);
            }
            else
            {
                ScreenCapture.CaptureScreenshot(fullPath, 1);
            }

            var info = new ScreenshotInfo
            {
                filename = filename,
                fullPath = fullPath,
                captureTime = DateTime.Now,
                width = width,
                height = height
            };

            screenshots.Add(info);
            OnScreenshotCaptured?.Invoke(info);

            if (playShutterSound)
            {
                PlayShutterSound();
            }

            Debug.Log($"[Screenshot] Captured: {filename}");
        }

        public void CaptureCustomResolution(int width, int height)
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string filename = $"screenshot_{width}x{height}_{timestamp}.png";
            string fullPath = Path.Combine(screenshotDirectory, filename);

            // Create render texture
            RenderTexture rt = new RenderTexture(width, height, 24);
            Camera.main.targetTexture = rt;
            Camera.main.Render();

            // Read pixels
            RenderTexture.active = rt;
            Texture2D screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
            screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            screenshot.Apply();

            // Save to file
            byte[] bytes = screenshot.EncodeToPNG();
            File.WriteAllBytes(fullPath, bytes);

            // Cleanup
            Camera.main.targetTexture = null;
            RenderTexture.active = null;
            Destroy(rt);
            Destroy(screenshot);

            var info = new ScreenshotInfo
            {
                filename = filename,
                fullPath = fullPath,
                captureTime = DateTime.Now,
                width = width,
                height = height,
                fileSize = bytes.Length
            };

            screenshots.Add(info);
            OnScreenshotCaptured?.Invoke(info);

            Debug.Log($"[Screenshot] Custom resolution captured: {width}x{height}");
        }

        public void CaptureBurst(int count = 5, float interval = 0.2f)
        {
            StartCoroutine(CaptureBurstCoroutine(count, interval));
        }

        private System.Collections.IEnumerator CaptureBurstCoroutine(int count, float interval)
        {
            for (int i = 0; i < count; i++)
            {
                CaptureScreenshot();
                yield return new WaitForSeconds(interval);
            }

            Debug.Log($"[Screenshot] Burst capture complete: {count} screenshots");
        }

        // Utility

        private void GetResolution(ScreenshotResolution resolution, out int width, out int height)
        {
            switch (resolution)
            {
                case ScreenshotResolution.HD_720p:
                    width = 1280;
                    height = 720;
                    break;
                case ScreenshotResolution.FullHD_1080p:
                    width = 1920;
                    height = 1080;
                    break;
                case ScreenshotResolution.QHD_1440p:
                    width = 2560;
                    height = 1440;
                    break;
                case ScreenshotResolution.UHD_4K:
                    width = 3840;
                    height = 2160;
                    break;
                case ScreenshotResolution.UHD_8K:
                    width = 7680;
                    height = 4320;
                    break;
                default: // Native
                    width = Screen.width;
                    height = Screen.height;
                    break;
            }
        }

        private void PlayShutterSound()
        {
            // Would play camera shutter sound effect
            Debug.Log("[Screenshot] *Click*");
        }

        // Gallery Management

        public List<ScreenshotInfo> GetAllScreenshots()
        {
            return new List<ScreenshotInfo>(screenshots);
        }

        public ScreenshotInfo GetLatestScreenshot()
        {
            return screenshots.Count > 0 ? screenshots[screenshots.Count - 1] : null;
        }

        public void DeleteScreenshot(string filename)
        {
            var info = screenshots.Find(s => s.filename == filename);
            if (info != null)
            {
                if (File.Exists(info.fullPath))
                {
                    File.Delete(info.fullPath);
                }
                screenshots.Remove(info);
                Debug.Log($"[Screenshot] Deleted: {filename}");
            }
        }

        public void DeleteAllScreenshots()
        {
            foreach (var info in screenshots)
            {
                if (File.Exists(info.fullPath))
                {
                    File.Delete(info.fullPath);
                }
            }
            screenshots.Clear();
            Debug.Log("[Screenshot] All screenshots deleted");
        }

        public void OpenScreenshotFolder()
        {
            Application.OpenURL(screenshotDirectory);
        }

        // Settings

        public void SetSuperSample(int multiplier)
        {
            superSampleMultiplier = Mathf.Clamp(multiplier, 1, 4);
            PlayerPrefs.SetInt("Screenshot_SuperSample", superSampleMultiplier);
            PlayerPrefs.Save();
        }

        public void SetIncludeUI(bool include)
        {
            includeUI = include;
            PlayerPrefs.SetInt("Screenshot_IncludeUI", include ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void SetHotkey(KeyCode key)
        {
            screenshotHotkey = key;
            PlayerPrefs.SetInt("Screenshot_Hotkey", (int)key);
            PlayerPrefs.Save();
        }

        public string GetScreenshotDirectory()
        {
            return screenshotDirectory;
        }

        public int GetScreenshotCount()
        {
            return screenshots.Count;
        }

        public long GetTotalScreenshotSize()
        {
            long total = 0;
            foreach (var info in screenshots)
            {
                if (File.Exists(info.fullPath))
                {
                    total += new FileInfo(info.fullPath).Length;
                }
            }
            return total;
        }
    }
}
