using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;
using System;

namespace DeadFrontier.UI
{
    /// <summary>
    /// Comprehensive notification system for player alerts and messages.
    /// Supports multiple notification types, priorities, queuing, and history.
    /// Includes toast notifications, persistent alerts, and notification preferences.
    /// </summary>
    public class NotificationSystem : NetworkBehaviour
    {
        public static NotificationSystem Instance { get; private set; }

        [Header("Display Settings")]
        [SerializeField] private float defaultToastDuration = 5f;
        [SerializeField] private float importantToastDuration = 10f;
        [SerializeField] private int maxActiveToasts = 3;
        [SerializeField] private int maxQueuedNotifications = 50;

        [Header("History Settings")]
        [SerializeField] private int maxHistorySize = 100;
        [SerializeField] private float historyRetentionDays = 7f;

        // Notification storage
        private Dictionary<ulong, List<Notification>> playerNotifications = new Dictionary<ulong, List<Notification>>();
        private Dictionary<ulong, List<Notification>> notificationHistory = new Dictionary<ulong, List<Notification>>();
        private Dictionary<ulong, NotificationPreferences> playerPreferences = new Dictionary<ulong, NotificationPreferences>();

        // Active toast queue
        private Dictionary<ulong, Queue<Notification>> toastQueues = new Dictionary<ulong, Queue<Notification>>();
        private Dictionary<ulong, List<Notification>> activeToasts = new Dictionary<ulong, List<Notification>>();

        // Events
        public event Action<ulong, Notification> OnNotificationReceived;
        public event Action<ulong, Notification> OnNotificationRead;
        public event Action<ulong, Notification> OnNotificationDismissed;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (!IsServer) return;

            ProcessToastQueues();
            CleanupExpiredNotifications();
        }

        #region Player Initialization

        public void InitializePlayerNotifications(ulong playerId)
        {
            if (playerNotifications.ContainsKey(playerId)) return;

            playerNotifications[playerId] = new List<Notification>();
            notificationHistory[playerId] = new List<Notification>();
            toastQueues[playerId] = new Queue<Notification>();
            activeToasts[playerId] = new List<Notification>();

            playerPreferences[playerId] = new NotificationPreferences
            {
                enabledTypes = new List<NotificationType>
                {
                    NotificationType.System,
                    NotificationType.Achievement,
                    NotificationType.Friend,
                    NotificationType.Reward,
                    NotificationType.Challenge,
                    NotificationType.Clan,
                    NotificationType.Trade,
                    NotificationType.Match,
                    NotificationType.Event
                },
                enableSound = true,
                enableToasts = true
            };

            LoadPlayerData(playerId);
        }

        #endregion

        #region Send Notifications

        public void SendNotification(ulong playerId, NotificationType type, string title, string message, NotificationPriority priority = NotificationPriority.Normal, Dictionary<string, string> data = null)
        {
            if (!playerNotifications.ContainsKey(playerId))
            {
                InitializePlayerNotifications(playerId);
            }

            // Check if player has this type enabled
            if (!IsNotificationTypeEnabled(playerId, type))
            {
                return;
            }

            var notification = new Notification
            {
                notificationId = $"notif_{playerId}_{DateTime.UtcNow.Ticks}",
                type = type,
                priority = priority,
                title = title,
                message = message,
                timestamp = DateTime.UtcNow,
                isRead = false,
                isPersistent = priority >= NotificationPriority.Important,
                data = data ?? new Dictionary<string, string>()
            };

            // Add to player notifications
            playerNotifications[playerId].Add(notification);

            // Add to toast queue if enabled
            if (playerPreferences[playerId].enableToasts)
            {
                toastQueues[playerId].Enqueue(notification);
            }

            // Trim if too many
            if (playerNotifications[playerId].Count > maxQueuedNotifications)
            {
                var oldest = playerNotifications[playerId].OrderBy(n => n.timestamp).First();
                playerNotifications[playerId].Remove(oldest);
            }

            OnNotificationReceived?.Invoke(playerId, notification);

            SavePlayerData(playerId);

            // Notify client
            SendNotificationClientRpc(playerId, notification.notificationId, type, title, message, priority);

            Debug.Log($"[NotificationSystem] Sent {type} notification to player {playerId}: {title}");
        }

        [ClientRpc]
        private void SendNotificationClientRpc(ulong playerId, string notificationId, NotificationType type, string title, string message, NotificationPriority priority)
        {
            // Client-side notification display
            Debug.Log($"[NotificationSystem] {type} ({priority}): {title} - {message}");
        }

        public void SendSystemNotification(ulong playerId, string title, string message)
        {
            SendNotification(playerId, NotificationType.System, title, message, NotificationPriority.Important);
        }

        public void SendAchievementNotification(ulong playerId, string achievementName)
        {
            SendNotification(playerId, NotificationType.Achievement, "Achievement Unlocked!", achievementName, NotificationPriority.High);
        }

        public void SendFriendNotification(ulong playerId, string friendName, string action)
        {
            SendNotification(playerId, NotificationType.Friend, "Friend Activity", $"{friendName} {action}", NotificationPriority.Normal);
        }

        public void SendRewardNotification(ulong playerId, string rewardDescription)
        {
            SendNotification(playerId, NotificationType.Reward, "Reward Received!", rewardDescription, NotificationPriority.High);
        }

        public void SendChallengeNotification(ulong playerId, string challengeName, bool completed)
        {
            string title = completed ? "Challenge Completed!" : "Challenge Progress";
            SendNotification(playerId, NotificationType.Challenge, title, challengeName, NotificationPriority.Normal);
        }

        public void SendClanNotification(ulong playerId, string clanMessage)
        {
            SendNotification(playerId, NotificationType.Clan, "Clan Update", clanMessage, NotificationPriority.Normal);
        }

        public void SendTradeNotification(ulong playerId, string traderName, string action)
        {
            SendNotification(playerId, NotificationType.Trade, "Trade Activity", $"{traderName} {action}", NotificationPriority.High);
        }

        public void SendMatchNotification(ulong playerId, string matchInfo)
        {
            SendNotification(playerId, NotificationType.Match, "Match Update", matchInfo, NotificationPriority.Normal);
        }

        public void SendEventNotification(ulong playerId, string eventName, string eventInfo)
        {
            SendNotification(playerId, NotificationType.Event, eventName, eventInfo, NotificationPriority.Important);
        }

        #endregion

        #region Broadcast Notifications

        public void BroadcastNotification(NotificationType type, string title, string message, NotificationPriority priority = NotificationPriority.Normal)
        {
            // Send to all players
            foreach (var playerId in playerNotifications.Keys)
            {
                SendNotification(playerId, type, title, message, priority);
            }

            Debug.Log($"[NotificationSystem] Broadcast {type} notification: {title}");
        }

        public void BroadcastSystemMessage(string title, string message)
        {
            BroadcastNotification(NotificationType.System, title, message, NotificationPriority.Important);
        }

        public void BroadcastEvent(string eventName, string eventInfo)
        {
            BroadcastNotification(NotificationType.Event, eventName, eventInfo, NotificationPriority.Critical);
        }

        #endregion

        #region Toast Management

        private void ProcessToastQueues()
        {
            foreach (var kvp in toastQueues)
            {
                ulong playerId = kvp.Key;
                var queue = kvp.Value;
                var active = activeToasts[playerId];

                // Remove expired toasts
                active.RemoveAll(n => DateTime.UtcNow >= n.expirationTime);

                // Add new toasts from queue
                while (active.Count < maxActiveToasts && queue.Count > 0)
                {
                    var notification = queue.Dequeue();

                    // Set expiration
                    float duration = notification.priority >= NotificationPriority.Important
                        ? importantToastDuration
                        : defaultToastDuration;

                    notification.expirationTime = DateTime.UtcNow.AddSeconds(duration);

                    active.Add(notification);

                    // Notify client to show toast
                    ShowToastClientRpc(playerId, notification.notificationId, notification.type, notification.title, notification.message, duration);
                }
            }
        }

        [ClientRpc]
        private void ShowToastClientRpc(ulong playerId, string notificationId, NotificationType type, string title, string message, float duration)
        {
            // Client-side toast UI display
            Debug.Log($"[NotificationSystem] TOAST ({duration}s): [{type}] {title} - {message}");
        }

        #endregion

        #region Notification Management

        public void MarkAsRead(ulong playerId, string notificationId)
        {
            if (!playerNotifications.ContainsKey(playerId)) return;

            var notification = playerNotifications[playerId].FirstOrDefault(n => n.notificationId == notificationId);

            if (notification != null && !notification.isRead)
            {
                notification.isRead = true;
                notification.readTime = DateTime.UtcNow;

                OnNotificationRead?.Invoke(playerId, notification);

                SavePlayerData(playerId);
            }
        }

        public void MarkAllAsRead(ulong playerId)
        {
            if (!playerNotifications.ContainsKey(playerId)) return;

            foreach (var notification in playerNotifications[playerId])
            {
                if (!notification.isRead)
                {
                    notification.isRead = true;
                    notification.readTime = DateTime.UtcNow;
                }
            }

            SavePlayerData(playerId);

            Debug.Log($"[NotificationSystem] Marked all notifications as read for player {playerId}");
        }

        public void DismissNotification(ulong playerId, string notificationId)
        {
            if (!playerNotifications.ContainsKey(playerId)) return;

            var notification = playerNotifications[playerId].FirstOrDefault(n => n.notificationId == notificationId);

            if (notification != null)
            {
                // Move to history
                if (!notificationHistory[playerId].Contains(notification))
                {
                    notificationHistory[playerId].Add(notification);
                }

                // Remove from active
                playerNotifications[playerId].Remove(notification);

                OnNotificationDismissed?.Invoke(playerId, notification);

                SavePlayerData(playerId);

                Debug.Log($"[NotificationSystem] Dismissed notification {notificationId} for player {playerId}");
            }
        }

        public void DismissAllNotifications(ulong playerId)
        {
            if (!playerNotifications.ContainsKey(playerId)) return;

            // Move all to history
            foreach (var notification in playerNotifications[playerId])
            {
                if (!notificationHistory[playerId].Contains(notification))
                {
                    notificationHistory[playerId].Add(notification);
                }
            }

            playerNotifications[playerId].Clear();

            SavePlayerData(playerId);

            Debug.Log($"[NotificationSystem] Dismissed all notifications for player {playerId}");
        }

        private void CleanupExpiredNotifications()
        {
            DateTime cutoffDate = DateTime.UtcNow.AddDays(-historyRetentionDays);

            foreach (var kvp in notificationHistory)
            {
                var history = kvp.Value;

                // Remove old notifications
                history.RemoveAll(n => n.timestamp < cutoffDate);

                // Trim if too many
                if (history.Count > maxHistorySize)
                {
                    var toRemove = history.OrderBy(n => n.timestamp).Take(history.Count - maxHistorySize).ToList();
                    foreach (var notification in toRemove)
                    {
                        history.Remove(notification);
                    }
                }
            }
        }

        #endregion

        #region Queries

        public List<Notification> GetNotifications(ulong playerId, bool unreadOnly = false)
        {
            if (!playerNotifications.ContainsKey(playerId)) return new List<Notification>();

            var notifications = playerNotifications[playerId];

            if (unreadOnly)
            {
                notifications = notifications.Where(n => !n.isRead).ToList();
            }

            return notifications.OrderByDescending(n => n.timestamp).ToList();
        }

        public List<Notification> GetNotificationsByType(ulong playerId, NotificationType type, bool unreadOnly = false)
        {
            if (!playerNotifications.ContainsKey(playerId)) return new List<Notification>();

            var notifications = playerNotifications[playerId].Where(n => n.type == type);

            if (unreadOnly)
            {
                notifications = notifications.Where(n => !n.isRead);
            }

            return notifications.OrderByDescending(n => n.timestamp).ToList();
        }

        public int GetUnreadCount(ulong playerId)
        {
            if (!playerNotifications.ContainsKey(playerId)) return 0;

            return playerNotifications[playerId].Count(n => !n.isRead);
        }

        public int GetUnreadCountByType(ulong playerId, NotificationType type)
        {
            if (!playerNotifications.ContainsKey(playerId)) return 0;

            return playerNotifications[playerId].Count(n => !n.isRead && n.type == type);
        }

        public List<Notification> GetNotificationHistory(ulong playerId, int maxCount = 50)
        {
            if (!notificationHistory.ContainsKey(playerId)) return new List<Notification>();

            return notificationHistory[playerId]
                .OrderByDescending(n => n.timestamp)
                .Take(maxCount)
                .ToList();
        }

        #endregion

        #region Preferences

        public void SetNotificationTypeEnabled(ulong playerId, NotificationType type, bool enabled)
        {
            if (!playerPreferences.ContainsKey(playerId))
            {
                InitializePlayerNotifications(playerId);
            }

            var prefs = playerPreferences[playerId];

            if (enabled && !prefs.enabledTypes.Contains(type))
            {
                prefs.enabledTypes.Add(type);
            }
            else if (!enabled && prefs.enabledTypes.Contains(type))
            {
                prefs.enabledTypes.Remove(type);
            }

            SavePlayerData(playerId);
        }

        public bool IsNotificationTypeEnabled(ulong playerId, NotificationType type)
        {
            if (!playerPreferences.ContainsKey(playerId)) return true;

            return playerPreferences[playerId].enabledTypes.Contains(type);
        }

        public void SetSoundEnabled(ulong playerId, bool enabled)
        {
            if (!playerPreferences.ContainsKey(playerId))
            {
                InitializePlayerNotifications(playerId);
            }

            playerPreferences[playerId].enableSound = enabled;
            SavePlayerData(playerId);
        }

        public void SetToastsEnabled(ulong playerId, bool enabled)
        {
            if (!playerPreferences.ContainsKey(playerId))
            {
                InitializePlayerNotifications(playerId);
            }

            playerPreferences[playerId].enableToasts = enabled;
            SavePlayerData(playerId);
        }

        public NotificationPreferences GetPreferences(ulong playerId)
        {
            if (!playerPreferences.ContainsKey(playerId))
            {
                InitializePlayerNotifications(playerId);
            }

            return playerPreferences[playerId];
        }

        #endregion

        #region Integration Helpers

        // Called by other systems
        public void NotifyAchievementUnlocked(ulong playerId, string achievementName)
        {
            SendAchievementNotification(playerId, achievementName);
        }

        public void NotifyFriendOnline(ulong playerId, string friendName)
        {
            SendFriendNotification(playerId, friendName, "is now online");
        }

        public void NotifyFriendRequest(ulong playerId, string friendName)
        {
            SendFriendNotification(playerId, friendName, "sent you a friend request");
        }

        public void NotifyClanInvite(ulong playerId, string clanName)
        {
            SendClanNotification(playerId, $"You've been invited to join {clanName}");
        }

        public void NotifyRewardClaimed(ulong playerId, string rewardName, int amount)
        {
            SendRewardNotification(playerId, $"{rewardName} x{amount}");
        }

        public void NotifyLevelUp(ulong playerId, int newLevel)
        {
            SendNotification(playerId, NotificationType.System, "Level Up!", $"You reached level {newLevel}!", NotificationPriority.High);
        }

        public void NotifyMatchFound(ulong playerId)
        {
            SendMatchNotification(playerId, "Match found! Preparing to join...");
        }

        public void NotifySeasonEnded(ulong playerId, int rank, string reward)
        {
            SendNotification(playerId, NotificationType.Event, "Season Ended!", $"Final Rank: {rank}\nReward: {reward}", NotificationPriority.Critical);
        }

        #endregion

        #region Data Persistence

        private void SavePlayerData(ulong playerId)
        {
            if (!playerNotifications.ContainsKey(playerId)) return;

            var data = new NotificationData
            {
                notifications = playerNotifications[playerId],
                history = notificationHistory[playerId],
                preferences = playerPreferences[playerId]
            };

            string json = JsonUtility.ToJson(data);

            SaveSystem.SaveManager.Instance?.SaveData($"notifications_{playerId}", json);
        }

        public void LoadPlayerData(ulong playerId)
        {
            string json = SaveSystem.SaveManager.Instance?.LoadData($"notifications_{playerId}");

            if (!string.IsNullOrEmpty(json))
            {
                var data = JsonUtility.FromJson<NotificationData>(json);

                playerNotifications[playerId] = data.notifications;
                notificationHistory[playerId] = data.history;
                playerPreferences[playerId] = data.preferences;

                Debug.Log($"[NotificationSystem] Loaded notification data for player {playerId}");
            }
        }

        #endregion
    }

    #region Data Classes

    [Serializable]
    public class Notification
    {
        public string notificationId;
        public NotificationType type;
        public NotificationPriority priority;
        public string title;
        public string message;
        public DateTime timestamp;
        public DateTime readTime;
        public DateTime expirationTime;
        public bool isRead;
        public bool isPersistent;
        public Dictionary<string, string> data = new Dictionary<string, string>();
    }

    [Serializable]
    public class NotificationPreferences
    {
        public List<NotificationType> enabledTypes = new List<NotificationType>();
        public bool enableSound = true;
        public bool enableToasts = true;
    }

    [Serializable]
    public class NotificationData
    {
        public List<Notification> notifications = new List<Notification>();
        public List<Notification> history = new List<Notification>();
        public NotificationPreferences preferences;
    }

    public enum NotificationType
    {
        System,
        Achievement,
        Friend,
        Reward,
        Challenge,
        Clan,
        Trade,
        Match,
        Event
    }

    public enum NotificationPriority
    {
        Low,
        Normal,
        High,
        Important,
        Critical
    }

    #endregion
}
