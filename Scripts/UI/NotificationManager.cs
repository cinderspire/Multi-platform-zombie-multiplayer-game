using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

namespace DeadFrontier.UI
{
    /// <summary>
    /// Manages in-game notifications (achievements, level ups, challenges, etc.)
    /// Displays toast-style notifications with icons and animations
    /// </summary>
    public class NotificationManager : Singleton<NotificationManager>
    {
        [Header("UI References")]
        [SerializeField] private GameObject notificationPrefab;
        [SerializeField] private Transform notificationContainer;
        [SerializeField] private float notificationSpacing = 10f;

        [Header("Animation Settings")]
        [SerializeField] private float slideInDuration = 0.3f;
        [SerializeField] private float displayDuration = 3f;
        [SerializeField] private float slideOutDuration = 0.3f;
        [SerializeField] private AnimationCurve slideInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Queue Settings")]
        [SerializeField] private int maxSimultaneousNotifications = 3;
        [SerializeField] private float queueCheckInterval = 0.1f;

        [Header("Sound")]
        [SerializeField] private AudioClip notificationSound;
        [SerializeField] private float soundVolume = 0.7f;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = false;

        // State
        private Queue<NotificationData> notificationQueue = new Queue<NotificationData>();
        private List<GameObject> activeNotifications = new List<GameObject>();
        private bool isProcessingQueue = false;

        protected override void Awake()
        {
            base.Awake();
            StartCoroutine(ProcessQueueCoroutine());
        }

        private void Start()
        {
            // Subscribe to events
            SubscribeToGameEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromGameEvents();
        }

        #region Event Subscriptions

        private void SubscribeToGameEvents()
        {
            // Achievement events
            if (Core.Achievements.AchievementManager.Instance != null)
            {
                Core.Achievements.AchievementManager.Instance.OnAchievementUnlocked += OnAchievementUnlocked;
            }

            // Progression events
            // Note: Would subscribe to PlayerProgression level up events here

            // Challenge events
            if (Core.Progression.Challenges.ChallengeManager.Instance != null)
            {
                Core.Progression.Challenges.ChallengeManager.Instance.OnChallengeCompleted += OnChallengeCompleted;
            }

            // Battle Pass events
            if (Core.Progression.BattlePass.BattlePassManager.Instance != null)
            {
                Core.Progression.BattlePass.BattlePassManager.Instance.OnTierUnlocked += OnBattlePassTierUnlocked;
            }
        }

        private void UnsubscribeFromGameEvents()
        {
            if (Core.Achievements.AchievementManager.Instance != null)
            {
                Core.Achievements.AchievementManager.Instance.OnAchievementUnlocked -= OnAchievementUnlocked;
            }

            if (Core.Progression.Challenges.ChallengeManager.Instance != null)
            {
                Core.Progression.Challenges.ChallengeManager.Instance.OnChallengeCompleted -= OnChallengeCompleted;
            }

            if (Core.Progression.BattlePass.BattlePassManager.Instance != null)
            {
                Core.Progression.BattlePass.BattlePassManager.Instance.OnTierUnlocked -= OnBattlePassTierUnlocked;
            }
        }

        #endregion

        #region Event Handlers

        private void OnAchievementUnlocked(Core.Achievements.AchievementData achievement)
        {
            ShowNotification(
                "Achievement Unlocked!",
                achievement.achievementName,
                GetAchievementColor(achievement.rarity),
                NotificationType.Achievement
            );
        }

        private void OnChallengeCompleted(Core.Progression.Challenges.ChallengeData challenge)
        {
            ShowNotification(
                "Challenge Complete!",
                challenge.challengeName,
                new Color(0.2f, 0.8f, 1f),
                NotificationType.Challenge
            );
        }

        private void OnBattlePassTierUnlocked(int tier)
        {
            ShowNotification(
                "Battle Pass",
                $"Tier {tier} Unlocked!",
                new Color(1f, 0.84f, 0f),
                NotificationType.BattlePass
            );
        }

        private Color GetAchievementColor(Core.Achievements.AchievementRarity rarity)
        {
            switch (rarity)
            {
                case Core.Achievements.AchievementRarity.Common:
                    return new Color(0.7f, 0.7f, 0.7f);
                case Core.Achievements.AchievementRarity.Uncommon:
                    return new Color(0.3f, 1f, 0.3f);
                case Core.Achievements.AchievementRarity.Rare:
                    return new Color(0.3f, 0.6f, 1f);
                case Core.Achievements.AchievementRarity.Epic:
                    return new Color(0.8f, 0.3f, 1f);
                case Core.Achievements.AchievementRarity.Legendary:
                    return new Color(1f, 0.5f, 0f);
                default:
                    return Color.white;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Shows a notification
        /// </summary>
        public void ShowNotification(string title, string message, Color color, NotificationType type = NotificationType.Info)
        {
            var notification = new NotificationData
            {
                title = title,
                message = message,
                color = color,
                type = type
            };

            notificationQueue.Enqueue(notification);

            if (showDebugLogs)
                Debug.Log($"[NotificationManager] Queued notification: {title} - {message}");
        }

        /// <summary>
        /// Shows a simple notification with default color
        /// </summary>
        public void ShowNotification(string title, string message)
        {
            ShowNotification(title, message, Color.white, NotificationType.Info);
        }

        /// <summary>
        /// Shows a level up notification
        /// </summary>
        public void ShowLevelUp(int level)
        {
            ShowNotification(
                "Level Up!",
                $"You reached level {level}",
                new Color(1f, 0.84f, 0f),
                NotificationType.LevelUp
            );
        }

        /// <summary>
        /// Shows a prestige notification
        /// </summary>
        public void ShowPrestige(int prestigeLevel)
        {
            ShowNotification(
                "Prestige!",
                $"Prestige Level {prestigeLevel}",
                new Color(1f, 0.5f, 0f),
                NotificationType.Prestige
            );
        }

        /// <summary>
        /// Shows a reward notification
        /// </summary>
        public void ShowReward(string rewardName, int amount = 1)
        {
            string message = amount > 1 ? $"{rewardName} x{amount}" : rewardName;
            ShowNotification(
                "Reward Earned!",
                message,
                new Color(0.3f, 1f, 0.3f),
                NotificationType.Reward
            );
        }

        #endregion

        #region Queue Processing

        private IEnumerator ProcessQueueCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(queueCheckInterval);

                if (notificationQueue.Count > 0 && activeNotifications.Count < maxSimultaneousNotifications)
                {
                    NotificationData data = notificationQueue.Dequeue();
                    StartCoroutine(ShowNotificationCoroutine(data));
                }
            }
        }

        private IEnumerator ShowNotificationCoroutine(NotificationData data)
        {
            if (notificationPrefab == null || notificationContainer == null)
            {
                Debug.LogError("[NotificationManager] Missing prefab or container!");
                yield break;
            }

            // Create notification
            GameObject notificationObj = Instantiate(notificationPrefab, notificationContainer);
            activeNotifications.Add(notificationObj);

            // Setup notification
            var notification = notificationObj.GetComponent<NotificationUI>();
            if (notification != null)
            {
                notification.Setup(data);
            }

            // Play sound
            if (notificationSound != null && Core.AudioManager.Instance != null)
            {
                Core.AudioManager.Instance.PlaySFX(notificationSound, soundVolume);
            }

            // Slide in animation
            yield return StartCoroutine(SlideIn(notificationObj));

            // Display duration
            yield return new WaitForSeconds(displayDuration);

            // Slide out animation
            yield return StartCoroutine(SlideOut(notificationObj));

            // Cleanup
            activeNotifications.Remove(notificationObj);
            Destroy(notificationObj);

            // Reposition remaining notifications
            RepositionNotifications();
        }

        private IEnumerator SlideIn(GameObject notification)
        {
            RectTransform rect = notification.GetComponent<RectTransform>();
            if (rect == null)
                yield break;

            Vector2 startPos = rect.anchoredPosition;
            Vector2 targetPos = startPos;
            startPos.x += 500f; // Start off-screen to the right

            rect.anchoredPosition = startPos;

            float elapsed = 0f;
            while (elapsed < slideInDuration)
            {
                elapsed += Time.deltaTime;
                float t = slideInCurve.Evaluate(elapsed / slideInDuration);
                rect.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
                yield return null;
            }

            rect.anchoredPosition = targetPos;
        }

        private IEnumerator SlideOut(GameObject notification)
        {
            RectTransform rect = notification.GetComponent<RectTransform>();
            if (rect == null)
                yield break;

            Vector2 startPos = rect.anchoredPosition;
            Vector2 targetPos = startPos;
            targetPos.x += 500f; // Slide off-screen to the right

            float elapsed = 0f;
            while (elapsed < slideOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / slideOutDuration;
                rect.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
                yield return null;
            }
        }

        private void RepositionNotifications()
        {
            for (int i = 0; i < activeNotifications.Count; i++)
            {
                RectTransform rect = activeNotifications[i].GetComponent<RectTransform>();
                if (rect != null)
                {
                    Vector2 targetPos = new Vector2(0, -i * (rect.sizeDelta.y + notificationSpacing));
                    StartCoroutine(SmoothMove(rect, targetPos, 0.3f));
                }
            }
        }

        private IEnumerator SmoothMove(RectTransform rect, Vector2 targetPos, float duration)
        {
            Vector2 startPos = rect.anchoredPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                rect.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
                yield return null;
            }

            rect.anchoredPosition = targetPos;
        }

        #endregion
    }

    #region Data Structures

    public enum NotificationType
    {
        Info,
        Achievement,
        LevelUp,
        Prestige,
        Challenge,
        BattlePass,
        Reward,
        Warning,
        Error
    }

    [System.Serializable]
    public struct NotificationData
    {
        public string title;
        public string message;
        public Color color;
        public NotificationType type;
    }

    #endregion

    #region Notification UI Component

    /// <summary>
    /// Component for individual notification UI elements
    /// Attach this to the notification prefab
    /// </summary>
    public class NotificationUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;

        public void Setup(NotificationData data)
        {
            if (titleText != null)
                titleText.text = data.title;

            if (messageText != null)
                messageText.text = data.message;

            if (backgroundImage != null)
            {
                Color bgColor = data.color;
                bgColor.a = 0.8f;
                backgroundImage.color = bgColor;
            }

            // Set icon based on type
            if (iconImage != null)
            {
                iconImage.color = data.color;
                // Could set different sprites based on type
            }
        }
    }

    #endregion
}
