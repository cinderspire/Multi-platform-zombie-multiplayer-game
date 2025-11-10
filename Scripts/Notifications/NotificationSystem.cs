using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Notifications
{
    /// <summary>
    /// Comprehensive notification system for in-game alerts, pop-ups, toasts, and notifications
    /// across all game systems with priority queuing and persistence.
    /// </summary>
    public class NotificationSystem : NetworkBehaviour
    {
        public static NotificationSystem Instance { get; private set; }

        [Header("Notification Configuration")]
        [SerializeField] private int maxActiveNotifications = 5;
        [SerializeField] private float defaultDisplayDuration = 5f;
        [SerializeField] private int maxStoredNotifications = 50;

        private Dictionary<ulong, List<Notification>> playerNotifications = new Dictionary<ulong, List<Notification>>();
        private Dictionary<ulong, Queue<Notification>> activeNotificationQueues = new Dictionary<ulong, Queue<Notification>>();

        public event Action<ulong, string, NotificationType> OnNotificationReceived;
        public event Action<ulong, string> OnNotificationDismissed;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        [ServerRpc(RequireOwnership = false)]
        public void SendNotificationServerRpc(ulong playerId, string title, string message, NotificationType type, NotificationPriority priority, float duration, List<NotificationAction> actions, ServerRpcParams rpcParams = default)
        {
            if (!playerNotifications.ContainsKey(playerId))
            {
                playerNotifications[playerId] = new List<Notification>();
                activeNotificationQueues[playerId] = new Queue<Notification>();
            }

            var notification = new Notification
            {
                notificationId = Guid.NewGuid().ToString(),
                title = title,
                message = message,
                type = type,
                priority = priority,
                displayDuration = duration > 0 ? duration : defaultDisplayDuration,
                actions = actions ?? new List<NotificationAction>(),
                timestamp = DateTime.UtcNow,
                isRead = false,
                isPersistent = priority >= NotificationPriority.High
            };

            // Add to storage
            playerNotifications[playerId].Add(notification);

            // Limit stored notifications
            if (playerNotifications[playerId].Count > maxStoredNotifications)
            {
                playerNotifications[playerId].RemoveAt(0);
            }

            // Queue for display
            activeNotificationQueues[playerId].Enqueue(notification);

            OnNotificationReceived?.Invoke(playerId, notification.notificationId, type);
            NotifyPlayerClientRpc(playerId, notification.notificationId, title, message, type, priority, duration);

            Debug.Log($"Notification sent to player {playerId}: {title}");
        }

        // Convenience methods for common notifications
        [ServerRpc(RequireOwnership = false)]
        public void SendAchievementNotificationServerRpc(ulong playerId, string achievementName, ServerRpcParams rpcParams = default)
        {
            SendNotificationServerRpc(
                playerId,
                "Achievement Unlocked!",
                $"You unlocked: {achievementName}",
                NotificationType.Achievement,
                NotificationPriority.High,
                7f,
                new List<NotificationAction>
                {
                    new NotificationAction { actionId = "view", actionText = "View", actionType = ActionType.OpenUI, actionData = "achievements" }
                }
            );
        }

        [ServerRpc(RequireOwnership = false)]
        public void SendLevelUpNotificationServerRpc(ulong playerId, int newLevel, ServerRpcParams rpcParams = default)
        {
            SendNotificationServerRpc(
                playerId,
                "Level Up!",
                $"You reached level {newLevel}!",
                NotificationType.LevelUp,
                NotificationPriority.High,
                5f,
                new List<NotificationAction>
                {
                    new NotificationAction { actionId = "claim", actionText = "Claim Rewards", actionType = ActionType.ClaimReward, actionData = $"level_{newLevel}" }
                }
            );
        }

        [ServerRpc(RequireOwnership = false)]
        public void SendRewardNotificationServerRpc(ulong playerId, string rewardName, int amount, ServerRpcParams rpcParams = default)
        {
            SendNotificationServerRpc(
                playerId,
                "Reward Received!",
                $"You received {amount}x {rewardName}",
                NotificationType.Reward,
                NotificationPriority.Medium,
                4f,
                null
            );
        }

        [ServerRpc(RequireOwnership = false)]
        public void SendPartyInviteNotificationServerRpc(ulong playerId, string inviterName, string partyId, ServerRpcParams rpcParams = default)
        {
            SendNotificationServerRpc(
                playerId,
                "Party Invite",
                $"{inviterName} invited you to join their party",
                NotificationType.PartyInvite,
                NotificationPriority.High,
                30f,
                new List<NotificationAction>
                {
                    new NotificationAction { actionId = "accept", actionText = "Accept", actionType = ActionType.AcceptInvite, actionData = partyId },
                    new NotificationAction { actionId = "decline", actionText = "Decline", actionType = ActionType.DeclineInvite, actionData = partyId }
                }
            );
        }

        [ServerRpc(RequireOwnership = false)]
        public void SendClanInviteNotificationServerRpc(ulong playerId, string clanName, string clanId, ServerRpcParams rpcParams = default)
        {
            SendNotificationServerRpc(
                playerId,
                "Clan Invite",
                $"You've been invited to join {clanName}",
                NotificationType.ClanInvite,
                NotificationPriority.High,
                60f,
                new List<NotificationAction>
                {
                    new NotificationAction { actionId = "accept", actionText = "Accept", actionType = ActionType.AcceptInvite, actionData = clanId },
                    new NotificationAction { actionId = "decline", actionText = "Decline", actionType = ActionType.DeclineInvite, actionData = clanId }
                }
            );
        }

        [ServerRpc(RequireOwnership = false)]
        public void SendEventStartNotificationServerRpc(ulong playerId, string eventName, ServerRpcParams rpcParams = default)
        {
            SendNotificationServerRpc(
                playerId,
                "Event Started!",
                $"{eventName} is now active!",
                NotificationType.EventStart,
                NotificationPriority.High,
                10f,
                new List<NotificationAction>
                {
                    new NotificationAction { actionId = "view", actionText = "View Event", actionType = ActionType.OpenUI, actionData = "events" }
                }
            );
        }

        [ServerRpc(RequireOwnership = false)]
        public void SendDailyRewardNotificationServerRpc(ulong playerId, ServerRpcParams rpcParams = default)
        {
            SendNotificationServerRpc(
                playerId,
                "Daily Reward Available!",
                "Claim your daily login reward",
                NotificationType.DailyReward,
                NotificationPriority.High,
                15f,
                new List<NotificationAction>
                {
                    new NotificationAction { actionId = "claim", actionText = "Claim Now", actionType = ActionType.OpenUI, actionData = "daily_rewards" }
                }
            );
        }

        [ServerRpc(RequireOwnership = false)]
        public void SendMailNotificationServerRpc(ulong playerId, string senderName, ServerRpcParams rpcParams = default)
        {
            SendNotificationServerRpc(
                playerId,
                "New Mail",
                $"You have new mail from {senderName}",
                NotificationType.Mail,
                NotificationPriority.Medium,
                5f,
                new List<NotificationAction>
                {
                    new NotificationAction { actionId = "view", actionText = "Read", actionType = ActionType.OpenUI, actionData = "mailbox" }
                }
            );
        }

        [ServerRpc(RequireOwnership = false)]
        public void SendWarningNotificationServerRpc(ulong playerId, string warningMessage, ServerRpcParams rpcParams = default)
        {
            SendNotificationServerRpc(
                playerId,
                "Warning",
                warningMessage,
                NotificationType.Warning,
                NotificationPriority.Critical,
                10f,
                null
            );
        }

        [ServerRpc(RequireOwnership = false)]
        public void SendSystemNotificationServerRpc(ulong playerId, string message, ServerRpcParams rpcParams = default)
        {
            SendNotificationServerRpc(
                playerId,
                "System Message",
                message,
                NotificationType.System,
                NotificationPriority.Medium,
                5f,
                null
            );
        }

        [ServerRpc(RequireOwnership = false)]
        public void MarkNotificationAsReadServerRpc(ulong playerId, string notificationId, ServerRpcParams rpcParams = default)
        {
            if (!playerNotifications.TryGetValue(playerId, out var notifications)) return;

            var notification = notifications.FirstOrDefault(n => n.notificationId == notificationId);
            if (notification != null)
            {
                notification.isRead = true;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void DismissNotificationServerRpc(ulong playerId, string notificationId, ServerRpcParams rpcParams = default)
        {
            if (!playerNotifications.TryGetValue(playerId, out var notifications)) return;

            var notification = notifications.FirstOrDefault(n => n.notificationId == notificationId);
            if (notification != null)
            {
                notifications.Remove(notification);
                OnNotificationDismissed?.Invoke(playerId, notificationId);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void ClearAllNotificationsServerRpc(ulong playerId, ServerRpcParams rpcParams = default)
        {
            if (playerNotifications.ContainsKey(playerId))
            {
                playerNotifications[playerId].Clear();
            }

            if (activeNotificationQueues.ContainsKey(playerId))
            {
                activeNotificationQueues[playerId].Clear();
            }
        }

        [ClientRpc]
        private void NotifyPlayerClientRpc(ulong playerId, string notificationId, string title, string message, NotificationType type, NotificationPriority priority, float duration)
        {
            // Client-side notification display
            // Would trigger UI notification popup/toast
        }

        public List<Notification> GetPlayerNotifications(ulong playerId) => playerNotifications.GetValueOrDefault(playerId, new List<Notification>());
        public int GetUnreadCount(ulong playerId)
        {
            if (!playerNotifications.TryGetValue(playerId, out var notifications)) return 0;
            return notifications.Count(n => !n.isRead);
        }
        public List<Notification> GetUnreadNotifications(ulong playerId)
        {
            if (!playerNotifications.TryGetValue(playerId, out var notifications)) return new List<Notification>();
            return notifications.Where(n => !n.isRead).ToList();
        }
    }

    [Serializable]
    public class Notification
    {
        public string notificationId;
        public string title;
        public string message;
        public NotificationType type;
        public NotificationPriority priority;
        public float displayDuration;
        public List<NotificationAction> actions;
        public DateTime timestamp;
        public bool isRead;
        public bool isPersistent;
    }

    [Serializable]
    public class NotificationAction
    {
        public string actionId;
        public string actionText;
        public ActionType actionType;
        public string actionData;
    }

    public enum NotificationType
    {
        Achievement, LevelUp, Reward, QuestComplete,
        PartyInvite, ClanInvite, FriendRequest,
        EventStart, EventEnd, DailyReward,
        Mail, Trade, System, Warning, Error
    }

    public enum NotificationPriority { Low, Medium, High, Critical }
    public enum ActionType { Dismiss, OpenUI, ClaimReward, AcceptInvite, DeclineInvite, Custom }
}
