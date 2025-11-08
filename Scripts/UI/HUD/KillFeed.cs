using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace DeadFrontier.UI.HUD
{
    /// <summary>
    /// Kill feed that shows kills, deaths, and game events
    /// </summary>
    public class KillFeed : Core.Singleton<KillFeed>
    {
        [Header("References")]
        [SerializeField] private RectTransform feedContainer;
        [SerializeField] private GameObject killFeedEntryPrefab;

        [Header("Settings")]
        [SerializeField] private int maxEntries = 5;
        [SerializeField] private float entryLifetime = 5f;
        [SerializeField] private float entrySpacing = 5f;
        [SerializeField] private bool fadeOut = true;
        [SerializeField] private float fadeOutDuration = 1f;

        [Header("Colors")]
        [SerializeField] private Color killColor = Color.white;
        [SerializeField] private Color headshotColor = new Color(1f, 0.5f, 0f); // Orange
        [SerializeField] private Color deathColor = Color.red;
        [SerializeField] private Color extractionColor = Color.green;
        [SerializeField] private Color eventColor = Color.yellow;

        [Header("Icons")]
        [SerializeField] private Sprite headshotIcon;
        [SerializeField] private Sprite explosionIcon;
        [SerializeField] private Sprite extractionIcon;

        // Entry pool
        private Queue<KillFeedEntry> entryPool = new Queue<KillFeedEntry>();
        private List<KillFeedEntry> activeEntries = new List<KillFeedEntry>();

        protected override void Awake()
        {
            base.Awake();

            // Prewarm pool
            PrewarmPool(maxEntries * 2);
        }

        #region Public Methods

        /// <summary>
        /// Shows a kill message
        /// </summary>
        public void ShowKill(string killerName, string victimName, string weaponName = null, bool isHeadshot = false)
        {
            string message = FormatKillMessage(killerName, victimName, weaponName, isHeadshot);
            Color color = isHeadshot ? headshotColor : killColor;
            Sprite icon = isHeadshot ? headshotIcon : null;

            AddEntry(message, color, icon);

            Debug.Log($"[KillFeed] {message}");
        }

        /// <summary>
        /// Shows a player death (to zombie)
        /// </summary>
        public void ShowDeath(string playerName, string causeOfDeath = "zombies")
        {
            string message = $"{playerName} was killed by {causeOfDeath}";
            AddEntry(message, deathColor);

            Debug.Log($"[KillFeed] {message}");
        }

        /// <summary>
        /// Shows an extraction message
        /// </summary>
        public void ShowExtraction(string playerName)
        {
            string message = $"{playerName} extracted successfully!";
            AddEntry(message, extractionColor, extractionIcon);

            Debug.Log($"[KillFeed] {message}");
        }

        /// <summary>
        /// Shows a general game event
        /// </summary>
        public void ShowEvent(string eventMessage, Color? customColor = null)
        {
            Color color = customColor ?? eventColor;
            AddEntry(eventMessage, color);

            Debug.Log($"[KillFeed] {eventMessage}");
        }

        /// <summary>
        /// Shows a horde spawn event
        /// </summary>
        public void ShowHordeSpawn(Vector3 location)
        {
            string message = "⚠ HORDE INCOMING!";
            AddEntry(message, Color.red);
        }

        #endregion

        #region Entry Management

        private void AddEntry(string message, Color color, Sprite icon = null)
        {
            // Remove oldest entry if at max
            if (activeEntries.Count >= maxEntries)
            {
                RemoveEntry(activeEntries[0]);
            }

            // Get entry from pool
            KillFeedEntry entry = GetEntryFromPool();

            // Setup entry
            entry.Setup(message, color, icon, entryLifetime);
            entry.transform.SetParent(feedContainer);
            entry.transform.SetAsFirstSibling(); // New entries at top
            entry.gameObject.SetActive(true);

            // Add to active list
            activeEntries.Add(entry);

            // Animate entry in
            AnimateEntryIn(entry);

            // Schedule removal
            Invoke(nameof(UpdateEntries), entryLifetime);
        }

        private void RemoveEntry(KillFeedEntry entry)
        {
            if (entry == null)
                return;

            activeEntries.Remove(entry);

            // Animate out
            if (fadeOut)
            {
                AnimateEntryOut(entry, () => ReturnEntryToPool(entry));
            }
            else
            {
                ReturnEntryToPool(entry);
            }
        }

        private void UpdateEntries()
        {
            // Remove expired entries
            for (int i = activeEntries.Count - 1; i >= 0; i--)
            {
                KillFeedEntry entry = activeEntries[i];

                if (entry != null && entry.IsExpired())
                {
                    RemoveEntry(entry);
                }
            }
        }

        #endregion

        #region Animations

        private void AnimateEntryIn(KillFeedEntry entry)
        {
            RectTransform rect = entry.GetComponent<RectTransform>();

            // Slide in from right
            Vector2 startPos = rect.anchoredPosition;
            startPos.x += 300f;
            rect.anchoredPosition = startPos;

            LeanTween.moveX(rect, startPos.x - 300f, 0.3f)
                .setEase(LeanTweenType.easeOutCubic);

            // Fade in
            CanvasGroup canvasGroup = entry.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = entry.gameObject.AddComponent<CanvasGroup>();

            canvasGroup.alpha = 0f;
            LeanTween.alphaCanvas(canvasGroup, 1f, 0.2f);
        }

        private void AnimateEntryOut(KillFeedEntry entry, System.Action onComplete)
        {
            CanvasGroup canvasGroup = entry.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = entry.gameObject.AddComponent<CanvasGroup>();

            LeanTween.alphaCanvas(canvasGroup, 0f, fadeOutDuration)
                .setOnComplete(() => onComplete?.Invoke());
        }

        #endregion

        #region Entry Pooling

        private void PrewarmPool(int count)
        {
            if (killFeedEntryPrefab == null)
            {
                Debug.LogError("[KillFeed] Kill feed entry prefab is not assigned!");
                return;
            }

            for (int i = 0; i < count; i++)
            {
                GameObject obj = Instantiate(killFeedEntryPrefab);
                KillFeedEntry entry = obj.GetComponent<KillFeedEntry>();

                if (entry == null)
                    entry = obj.AddComponent<KillFeedEntry>();

                obj.SetActive(false);
                entryPool.Enqueue(entry);
            }
        }

        private KillFeedEntry GetEntryFromPool()
        {
            if (entryPool.Count > 0)
            {
                return entryPool.Dequeue();
            }
            else
            {
                // Create new entry
                GameObject obj = Instantiate(killFeedEntryPrefab);
                KillFeedEntry entry = obj.GetComponent<KillFeedEntry>();

                if (entry == null)
                    entry = obj.AddComponent<KillFeedEntry>();

                return entry;
            }
        }

        private void ReturnEntryToPool(KillFeedEntry entry)
        {
            if (entry == null)
                return;

            entry.gameObject.SetActive(false);
            entry.transform.SetParent(null);
            entryPool.Enqueue(entry);
        }

        #endregion

        #region Formatting

        private string FormatKillMessage(string killerName, string victimName, string weaponName, bool isHeadshot)
        {
            string headshot = isHeadshot ? " [HEADSHOT]" : "";
            string weapon = !string.IsNullOrEmpty(weaponName) ? $" with {weaponName}" : "";

            return $"{killerName} killed {victimName}{weapon}{headshot}";
        }

        #endregion
    }

    /// <summary>
    /// Individual kill feed entry component
    /// </summary>
    public class KillFeedEntry : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Image iconImage;

        private float expiryTime;

        public void Setup(string message, Color color, Sprite icon, float lifetime)
        {
            // Set message
            if (messageText != null)
            {
                messageText.text = message;
                messageText.color = color;
            }

            // Set icon
            if (iconImage != null)
            {
                if (icon != null)
                {
                    iconImage.sprite = icon;
                    iconImage.gameObject.SetActive(true);
                }
                else
                {
                    iconImage.gameObject.SetActive(false);
                }
            }

            // Set expiry time
            expiryTime = Time.time + lifetime;
        }

        public bool IsExpired()
        {
            return Time.time >= expiryTime;
        }
    }
}
