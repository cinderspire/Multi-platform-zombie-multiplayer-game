using UnityEngine;
using System.Collections.Generic;
using System;

namespace ZombieGame
{
    /// <summary>
    /// Notification System - Smart, non-intrusive player communication
    /// Features: Priority queuing, toast notifications, achievement popups, system messages
    /// Essential for keeping players informed without overwhelming them
    /// </summary>
    public class NotificationSystem : MonoBehaviour
    {
        public static NotificationSystem Instance { get; private set; }

        [Header("Notification Settings")]
        [SerializeField] private bool enableNotifications = true;
        [SerializeField] private int maxSimultaneousNotifications = 3;
        [SerializeField] private float defaultDuration = 5f;
        [SerializeField] private float queueCheckInterval = 0.5f;

        private Queue<Notification> notificationQueue = new Queue<Notification>();
        private List<Notification> activeNotifications = new List<Notification>();
        private float lastQueueCheck = 0f;

        // Events
        public event Action<Notification> OnNotificationShown;
        public event Action<Notification> OnNotificationDismissed;

        [Serializable]
        public class Notification
        {
            public string id;
            public NotificationType type;
            public NotificationPriority priority;
            public string title;
            public string message;
            public Sprite icon;
            public float duration;
            public DateTime timestamp;
            public bool isDismissible = true;
            public Action onClickCallback;
            public Color backgroundColor;
        }

        public enum NotificationType
        {
            Info,
            Success,
            Warning,
            Error,
            Achievement,
            LevelUp,
            ItemReceived,
            FriendOnline,
            PartyInvite,
            MatchFound,
            System
        }

        public enum NotificationPriority
        {
            Low,       // Can be queued for a while
            Normal,    // Standard priority
            High,      // Show soon
            Critical   // Show immediately, bump others
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else { Destroy(gameObject); }
        }

        private void Update()
        {
            if (!enableNotifications) return;

            // Update active notifications
            for (int i = activeNotifications.Count - 1; i >= 0; i--)
            {
                var notification = activeNotifications[i];
                if (Time.time - (float)(DateTime.UtcNow - notification.timestamp).TotalSeconds >= notification.duration)
                {
                    DismissNotification(notification);
                }
            }

            // Process queue
            if (Time.time - lastQueueCheck >= queueCheckInterval)
            {
                ProcessQueue();
                lastQueueCheck = Time.time;
            }
        }

        public void ShowNotification(NotificationType type, string title, string message, 
            NotificationPriority priority = NotificationPriority.Normal, float duration = 0f)
        {
            if (!enableNotifications) return;

            var notification = CreateNotification(type, title, message, priority, duration);
            QueueNotification(notification);
        }

        private Notification CreateNotification(NotificationType type, string title, string message,
            NotificationPriority priority, float duration)
        {
            var notification = new Notification
            {
                id = Guid.NewGuid().ToString(),
                type = type,
                priority = priority,
                title = title,
                message = message,
                duration = duration > 0 ? duration : defaultDuration,
                timestamp = DateTime.UtcNow,
                backgroundColor = GetTypeColor(type)
            };

            return notification;
        }

        private void QueueNotification(Notification notification)
        {
            // Critical notifications bypass queue
            if (notification.priority == NotificationPriority.Critical)
            {
                ShowImmediately(notification);
                return;
            }

            // Add to queue based on priority
            if (notification.priority == NotificationPriority.High)
            {
                // Insert near front of queue
                var tempQueue = new Queue<Notification>();
                bool inserted = false;

                while (notificationQueue.Count > 0)
                {
                    var existing = notificationQueue.Dequeue();
                    if (!inserted && existing.priority < NotificationPriority.High)
                    {
                        tempQueue.Enqueue(notification);
                        inserted = true;
                    }
                    tempQueue.Enqueue(existing);
                }

                if (!inserted) tempQueue.Enqueue(notification);

                notificationQueue = tempQueue;
            }
            else
            {
                notificationQueue.Enqueue(notification);
            }

            Debug.Log($"[Notification] Queued: {notification.title} (Queue size: {notificationQueue.Count})");
        }

        private void ShowImmediately(Notification notification)
        {
            if (activeNotifications.Count >= maxSimultaneousNotifications)
            {
                // Remove lowest priority active notification
                var lowestPriority = activeNotifications[0];
                foreach (var active in activeNotifications)
                {
                    if (active.priority < lowestPriority.priority)
                        lowestPriority = active;
                }
                DismissNotification(lowestPriority);
            }

            DisplayNotification(notification);
        }

        private void ProcessQueue()
        {
            if (notificationQueue.Count == 0) return;
            if (activeNotifications.Count >= maxSimultaneousNotifications) return;

            var notification = notificationQueue.Dequeue();
            DisplayNotification(notification);
        }

        private void DisplayNotification(Notification notification)
        {
            activeNotifications.Add(notification);
            OnNotificationShown?.Invoke(notification);

            Debug.Log($"[Notification] Showing: [{notification.type}] {notification.title} - {notification.message}");
        }

        public void DismissNotification(Notification notification)
        {
            if (!activeNotifications.Contains(notification)) return;

            activeNotifications.Remove(notification);
            OnNotificationDismissed?.Invoke(notification);

            Debug.Log($"[Notification] Dismissed: {notification.title}");
        }

        public void DismissAll()
        {
            foreach (var notification in new List<Notification>(activeNotifications))
            {
                DismissNotification(notification);
            }
        }

        private Color GetTypeColor(NotificationType type)
        {
            switch (type)
            {
                case NotificationType.Info: return new Color(0.2f, 0.6f, 1f);
                case NotificationType.Success: return new Color(0.2f, 0.8f, 0.2f);
                case NotificationType.Warning: return new Color(1f, 0.8f, 0.2f);
                case NotificationType.Error: return new Color(0.9f, 0.2f, 0.2f);
                case NotificationType.Achievement: return new Color(1f, 0.8f, 0.2f);
                case NotificationType.LevelUp: return new Color(0.6f, 0.3f, 1f);
                case NotificationType.ItemReceived: return new Color(0.2f, 0.8f, 0.6f);
                case NotificationType.FriendOnline: return new Color(0.3f, 0.7f, 1f);
                case NotificationType.PartyInvite: return new Color(1f, 0.5f, 0.8f);
                case NotificationType.MatchFound: return new Color(0.2f, 1f, 0.4f);
                case NotificationType.System: return new Color(0.5f, 0.5f, 0.5f);
                default: return Color.white;
            }
        }

        // Convenience Methods

        public void ShowInfoNotification(string title, string message)
        {
            ShowNotification(NotificationType.Info, title, message);
        }

        public void ShowSuccessNotification(string title, string message)
        {
            ShowNotification(NotificationType.Success, title, message);
        }

        public void ShowWarningNotification(string title, string message)
        {
            ShowNotification(NotificationType.Warning, title, message, NotificationPriority.High);
        }

        public void ShowErrorNotification(string title, string message)
        {
            ShowNotification(NotificationType.Error, title, message, NotificationPriority.High);
        }

        public void ShowAchievementNotification(string achievementName, string description)
        {
            ShowNotification(NotificationType.Achievement, $"Achievement Unlocked!", 
                $"{achievementName}\n{description}", NotificationPriority.High, 8f);
        }

        public void ShowLevelUpNotification(int newLevel)
        {
            ShowNotification(NotificationType.LevelUp, "Level Up!", 
                $"Congratulations! You reached level {newLevel}", NotificationPriority.High, 6f);
        }

        public void ShowItemReceivedNotification(string itemName, int quantity = 1)
        {
            string message = quantity > 1 ? $"Received {quantity}x {itemName}" : $"Received {itemName}";
            ShowNotification(NotificationType.ItemReceived, "Item Received", message);
        }

        public void ShowFriendOnlineNotification(string friendName)
        {
            ShowNotification(NotificationType.FriendOnline, "Friend Online", 
                $"{friendName} is now online");
        }

        public void ShowPartyInviteNotification(string playerName)
        {
            ShowNotification(NotificationType.PartyInvite, "Party Invite", 
                $"{playerName} invited you to their party", NotificationPriority.High);
        }

        public void ShowMatchFoundNotification(string gameMode)
        {
            ShowNotification(NotificationType.MatchFound, "Match Found!", 
                $"{gameMode} match is ready", NotificationPriority.Critical, 10f);
        }

        public void ShowSystemNotification(string title, string message, NotificationPriority priority = NotificationPriority.Normal)
        {
            ShowNotification(NotificationType.System, title, message, priority);
        }

        // Settings

        public void SetMaxSimultaneousNotifications(int max)
        {
            maxSimultaneousNotifications = Mathf.Clamp(max, 1, 10);
            PlayerPrefs.SetInt("Notification_MaxSimultaneous", maxSimultaneousNotifications);
            PlayerPrefs.Save();
        }

        public void SetDefaultDuration(float duration)
        {
            defaultDuration = Mathf.Clamp(duration, 1f, 30f);
            PlayerPrefs.SetFloat("Notification_DefaultDuration", defaultDuration);
            PlayerPrefs.Save();
        }

        public void SetNotificationsEnabled(bool enabled)
        {
            enableNotifications = enabled;
            PlayerPrefs.SetInt("Notification_Enabled", enabled ? 1 : 0);
            PlayerPrefs.Save();

            if (!enabled)
            {
                DismissAll();
                notificationQueue.Clear();
            }
        }

        public int GetQueueSize()
        {
            return notificationQueue.Count;
        }

        public int GetActiveCount()
        {
            return activeNotifications.Count;
        }

        public List<Notification> GetActiveNotifications()
        {
            return new List<Notification>(activeNotifications);
        }
    }
}
