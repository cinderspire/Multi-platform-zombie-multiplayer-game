using UnityEngine;
using System.Collections.Generic;
using TMPro;

namespace DeadFrontier.Core.Localization
{
    /// <summary>
    /// Manages multi-language localization for global release
    /// Supports hot-swapping languages and automatic text updates
    /// </summary>
    public class LocalizationManager : Singleton<LocalizationManager>
    {
        [Header("Supported Languages")]
        [SerializeField] private List<LanguageData> supportedLanguages = new List<LanguageData>();
        [SerializeField] private SystemLanguage defaultLanguage = SystemLanguage.English;
        [SerializeField] private bool autoDetectSystemLanguage = true;

        [Header("Localization Data")]
        [SerializeField] private TextAsset localizationCSV;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;
        [SerializeField] private bool showMissingKeys = true;

        // Current language
        private SystemLanguage currentLanguage;
        private Dictionary<string, string> localizedStrings = new Dictionary<string, string>();

        // Registered components
        private List<LocalizedText> registeredTexts = new List<LocalizedText>();

        // Events
        public event System.Action<SystemLanguage> OnLanguageChanged;

        protected override void Awake()
        {
            base.Awake();
            LoadDefaultLanguages();
            LoadSavedLanguage();
            LoadLocalizationData();
        }

        #region Initialization

        private void LoadDefaultLanguages()
        {
            if (supportedLanguages.Count == 0)
            {
                // Add common languages
                supportedLanguages.Add(new LanguageData { language = SystemLanguage.English, languageName = "English", languageCode = "en" });
                supportedLanguages.Add(new LanguageData { language = SystemLanguage.Spanish, languageName = "Español", languageCode = "es" });
                supportedLanguages.Add(new LanguageData { language = SystemLanguage.French, languageName = "Français", languageCode = "fr" });
                supportedLanguages.Add(new LanguageData { language = SystemLanguage.German, languageName = "Deutsch", languageCode = "de" });
                supportedLanguages.Add(new LanguageData { language = SystemLanguage.Italian, languageName = "Italiano", languageCode = "it" });
                supportedLanguages.Add(new LanguageData { language = SystemLanguage.Portuguese, languageName = "Português", languageCode = "pt" });
                supportedLanguages.Add(new LanguageData { language = SystemLanguage.Russian, languageName = "Русский", languageCode = "ru" });
                supportedLanguages.Add(new LanguageData { language = SystemLanguage.Chinese, languageName = "中文", languageCode = "zh" });
                supportedLanguages.Add(new LanguageData { language = SystemLanguage.Japanese, languageName = "日本語", languageCode = "ja" });
                supportedLanguages.Add(new LanguageData { language = SystemLanguage.Korean, languageName = "한국어", languageCode = "ko" });
                supportedLanguages.Add(new LanguageData { language = SystemLanguage.Turkish, languageName = "Türkçe", languageCode = "tr" });
                supportedLanguages.Add(new LanguageData { language = SystemLanguage.Arabic, languageName = "العربية", languageCode = "ar" });
            }
        }

        private void LoadSavedLanguage()
        {
            if (PlayerPrefs.HasKey("Language"))
            {
                string savedLang = PlayerPrefs.GetString("Language");
                if (System.Enum.TryParse(savedLang, out SystemLanguage lang))
                {
                    currentLanguage = lang;
                }
                else
                {
                    currentLanguage = defaultLanguage;
                }
            }
            else if (autoDetectSystemLanguage)
            {
                currentLanguage = Application.systemLanguage;

                // Check if system language is supported
                if (!IsLanguageSupported(currentLanguage))
                {
                    currentLanguage = defaultLanguage;
                }
            }
            else
            {
                currentLanguage = defaultLanguage;
            }

            if (showDebugLogs)
                Debug.Log($"[LocalizationManager] Current language: {currentLanguage}");
        }

        #endregion

        #region Localization Data

        private void LoadLocalizationData()
        {
            localizedStrings.Clear();

            if (localizationCSV != null)
            {
                ParseCSVData(localizationCSV.text);
            }
            else
            {
                LoadDefaultStrings();
            }

            if (showDebugLogs)
                Debug.Log($"[LocalizationManager] Loaded {localizedStrings.Count} localized strings");
        }

        private void ParseCSVData(string csvText)
        {
            string[] lines = csvText.Split('\n');

            if (lines.Length == 0)
                return;

            // First line is header: Key,English,Spanish,French,etc.
            string[] header = lines[0].Split(',');

            // Find language column index
            int languageColumnIndex = -1;
            string langCode = GetLanguageCode(currentLanguage);

            for (int i = 0; i < header.Length; i++)
            {
                if (header[i].Trim().ToLower() == langCode.ToLower() ||
                    header[i].Trim().ToLower() == currentLanguage.ToString().ToLower())
                {
                    languageColumnIndex = i;
                    break;
                }
            }

            // Fallback to English if language not found
            if (languageColumnIndex == -1)
            {
                languageColumnIndex = 1; // Assuming English is second column
            }

            // Parse data lines
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line))
                    continue;

                string[] values = line.Split(',');
                if (values.Length > languageColumnIndex)
                {
                    string key = values[0].Trim();
                    string value = values[languageColumnIndex].Trim();

                    // Handle escaped characters
                    value = value.Replace("\\n", "\n");
                    value = value.Replace("\\t", "\t");

                    localizedStrings[key] = value;
                }
            }
        }

        private void LoadDefaultStrings()
        {
            // Default English strings
            localizedStrings["menu.play"] = "Play";
            localizedStrings["menu.settings"] = "Settings";
            localizedStrings["menu.quit"] = "Quit";
            localizedStrings["menu.shop"] = "Shop";
            localizedStrings["menu.leaderboard"] = "Leaderboard";
            localizedStrings["menu.achievements"] = "Achievements";

            localizedStrings["game.health"] = "Health";
            localizedStrings["game.ammo"] = "Ammo";
            localizedStrings["game.reload"] = "Reload";
            localizedStrings["game.extract"] = "Extract";
            localizedStrings["game.killed"] = "Killed";
            localizedStrings["game.died"] = "Died";

            localizedStrings["settings.graphics"] = "Graphics";
            localizedStrings["settings.audio"] = "Audio";
            localizedStrings["settings.controls"] = "Controls";
            localizedStrings["settings.language"] = "Language";

            localizedStrings["notification.level_up"] = "Level Up!";
            localizedStrings["notification.achievement"] = "Achievement Unlocked!";
            localizedStrings["notification.daily_reward"] = "Daily Reward!";

            if (showDebugLogs)
                Debug.Log("[LocalizationManager] Loaded default strings");
        }

        #endregion

        #region Language Management

        /// <summary>
        /// Changes the current language
        /// </summary>
        public void SetLanguage(SystemLanguage language)
        {
            if (!IsLanguageSupported(language))
            {
                if (showDebugLogs)
                    Debug.LogWarning($"[LocalizationManager] Language not supported: {language}");
                return;
            }

            currentLanguage = language;
            PlayerPrefs.SetString("Language", language.ToString());
            PlayerPrefs.Save();

            // Reload localization data
            LoadLocalizationData();

            // Update all registered texts
            UpdateAllTexts();

            OnLanguageChanged?.Invoke(language);

            if (showDebugLogs)
                Debug.Log($"[LocalizationManager] Language changed to: {language}");

            // Track analytics
            Analytics.AnalyticsManager.Instance?.TrackEvent("language_changed", new Dictionary<string, object>
            {
                { "language", language.ToString() },
                { "language_code", GetLanguageCode(language) }
            });
        }

        /// <summary>
        /// Checks if a language is supported
        /// </summary>
        public bool IsLanguageSupported(SystemLanguage language)
        {
            return supportedLanguages.Exists(l => l.language == language);
        }

        /// <summary>
        /// Gets language code (e.g., "en" for English)
        /// </summary>
        public string GetLanguageCode(SystemLanguage language)
        {
            var langData = supportedLanguages.Find(l => l.language == language);
            return langData != null ? langData.languageCode : "en";
        }

        #endregion

        #region String Retrieval

        /// <summary>
        /// Gets a localized string by key
        /// </summary>
        public string GetString(string key)
        {
            if (localizedStrings.ContainsKey(key))
            {
                return localizedStrings[key];
            }

            if (showMissingKeys)
            {
                Debug.LogWarning($"[LocalizationManager] Missing localization key: {key}");
            }

            return $"[{key}]"; // Return key in brackets if not found
        }

        /// <summary>
        /// Gets a localized string with parameters
        /// </summary>
        public string GetString(string key, params object[] args)
        {
            string text = GetString(key);
            return string.Format(text, args);
        }

        #endregion

        #region Text Registration

        /// <summary>
        /// Registers a LocalizedText component for auto-updates
        /// </summary>
        public void RegisterText(LocalizedText localizedText)
        {
            if (!registeredTexts.Contains(localizedText))
            {
                registeredTexts.Add(localizedText);
            }
        }

        /// <summary>
        /// Unregisters a LocalizedText component
        /// </summary>
        public void UnregisterText(LocalizedText localizedText)
        {
            registeredTexts.Remove(localizedText);
        }

        /// <summary>
        /// Updates all registered text components
        /// </summary>
        private void UpdateAllTexts()
        {
            // Remove null references
            registeredTexts.RemoveAll(t => t == null);

            foreach (var text in registeredTexts)
            {
                text.UpdateText();
            }

            if (showDebugLogs)
                Debug.Log($"[LocalizationManager] Updated {registeredTexts.Count} text components");
        }

        #endregion

        #region Properties

        public SystemLanguage CurrentLanguage => currentLanguage;
        public List<LanguageData> SupportedLanguages => new List<LanguageData>(supportedLanguages);
        public int LocalizedStringCount => localizedStrings.Count;

        #endregion
    }

    #region Data Structures

    [System.Serializable]
    public class LanguageData
    {
        public SystemLanguage language;
        public string languageName;
        public string languageCode;
        public Sprite flag;
    }

    #endregion

    #region Localized Text Component

    /// <summary>
    /// Component that automatically updates text when language changes
    /// Attach to TextMeshProUGUI components
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string localizationKey;
        [SerializeField] private bool updateOnEnable = true;

        private TextMeshProUGUI textComponent;

        private void Awake()
        {
            textComponent = GetComponent<TextMeshProUGUI>();
        }

        private void OnEnable()
        {
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.RegisterText(this);

                if (updateOnEnable)
                {
                    UpdateText();
                }
            }
        }

        private void OnDisable()
        {
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.UnregisterText(this);
            }
        }

        public void UpdateText()
        {
            if (textComponent != null && !string.IsNullOrEmpty(localizationKey))
            {
                textComponent.text = LocalizationManager.Instance.GetString(localizationKey);
            }
        }

        public void SetKey(string key)
        {
            localizationKey = key;
            UpdateText();
        }

        public string LocalizationKey
        {
            get => localizationKey;
            set
            {
                localizationKey = value;
                UpdateText();
            }
        }
    }

    #endregion
}
