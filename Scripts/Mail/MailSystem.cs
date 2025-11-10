using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Mail
{
    /// <summary>
    /// Comprehensive in-game mail and messaging system for multi-platform zombie multiplayer game.
    /// Handles player mail, system notifications, item/currency attachments, and message management.
    /// </summary>
    public class MailSystem : NetworkBehaviour
    {
        public static MailSystem Instance { get; private set; }

        [Header("Mail Settings")]
        [SerializeField] private bool enableMailSystem = true;
        [SerializeField] private int maxMailboxSize = 100;
        [SerializeField] private int maxAttachments = 5;
        [SerializeField] private int mailRetentionDays = 30;

        [Header("Messaging Settings")]
        [SerializeField] private bool enablePlayerToPlayer = true;
        [SerializeField] private int maxMessageLength = 500;
        [SerializeField] private bool requireFriendship = false;
        [SerializeField] private float messageCooldown = 5f;

        [Header("Attachment Settings")]
        [SerializeField] private bool enableItemAttachments = true;
        [SerializeField] private bool enableCurrencyAttachments = true;
        [SerializeField] private int maxCurrencyPerMail = 100000;

        // Enums
        public enum MailType
        {
            PlayerMessage,
            SystemNotification,
            RewardMail,
            AdminMessage,
            GuildMail,
            EventReward,
            CompensationMail
        }

        public enum MailPriority
        {
            Low,
            Normal,
            High,
            Urgent
        }

        public enum MailStatus
        {
            Unread,
            Read,
            Claimed,
            Archived,
            Deleted
        }

        // Data structures
        [Serializable]
        public class MailMessage
        {
            public string mailId;
            public MailType type;
            public MailPriority priority;
            public MailStatus status;
            public ulong senderId;
            public string senderName;
            public ulong recipientId;
            public string subject;
            public string body;
            public DateTime sentDate;
            public DateTime? readDate;
            public DateTime? expirationDate;
            public List<MailAttachment> attachments = new List<MailAttachment>();
            public bool hasAttachments;
            public bool attachmentsClaimed;
        }

        [Serializable]
        public class MailAttachment
        {
            public string attachmentId;
            public AttachmentType type;
            public string itemId;
            public int quantity;
            public int softCurrency;
            public int hardCurrency;
            public bool claimed;
        }

        public enum AttachmentType
        {
            Item,
            SoftCurrency,
            HardCurrency,
            Mixed
        }

        [Serializable]
        public class PlayerMailbox
        {
            public ulong playerId;
            public List<MailMessage> inbox = new List<MailMessage>();
            public List<MailMessage> sent = new List<MailMessage>();
            public int unreadCount;
            public int unclaimedCount;
            public DateTime lastChecked;
        }

        [Serializable]
        public class MailTemplate
        {
            public string templateId;
            public string subject;
            public string body;
            public MailType type;
            public MailPriority priority;
            public Dictionary<string, string> placeholders = new Dictionary<string, string>();
        }

        // State
        private Dictionary<ulong, PlayerMailbox> playerMailboxes = new Dictionary<ulong, PlayerMailbox>();
        private Dictionary<string, MailTemplate> mailTemplates = new Dictionary<string, MailTemplate>();
        private Dictionary<ulong, DateTime> lastMessageTime = new Dictionary<ulong, DateTime>();

        // Events
        public event Action<ulong, MailMessage> OnMailReceived;
        public event Action<ulong, MailMessage> OnMailRead;
        public event Action<ulong, string> OnAttachmentClaimed;
        public event Action<ulong, int> OnUnreadCountChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                InitializeMailSystem();
            }
        }

        private void Update()
        {
            if (!IsServer || !enableMailSystem) return;

            CleanupExpiredMail();
        }

        #region Initialization

        private void InitializeMailSystem()
        {
            InitializeMailTemplates();
        }

        private void InitializeMailTemplates()
        {
            // Welcome mail template
            RegisterTemplate(new MailTemplate
            {
                templateId = "welcome",
                subject = "Welcome to the Apocalypse!",
                body = "Welcome {playerName}! Here's a starter pack to help you survive.",
                type = MailType.SystemNotification,
                priority = MailPriority.High
            });

            // Reward template
            RegisterTemplate(new MailTemplate
            {
                templateId = "daily_reward",
                subject = "Daily Login Reward",
                body = "Thank you for playing! Here's your daily reward.",
                type = MailType.RewardMail,
                priority = MailPriority.Normal
            });

            // Compensation template
            RegisterTemplate(new MailTemplate
            {
                templateId = "compensation",
                subject = "Compensation Rewards",
                body = "We apologize for the inconvenience. Please accept these items as compensation.",
                type = MailType.CompensationMail,
                priority = MailPriority.Urgent
            });
        }

        private void RegisterTemplate(MailTemplate template)
        {
            mailTemplates[template.templateId] = template;
        }

        #endregion

        #region Send Mail

        [ServerRpc(RequireOwnership = false)]
        public void SendMailServerRpc(ulong senderId, ulong recipientId, string subject, string body, MailAttachment[] attachments)
        {
            if (!enablePlayerToPlayer && senderId != 0)
            {
                Debug.LogWarning("Player-to-player mail is disabled");
                return;
            }

            // Check cooldown
            if (lastMessageTime.ContainsKey(senderId))
            {
                if ((DateTime.UtcNow - lastMessageTime[senderId]).TotalSeconds < messageCooldown)
                {
                    Debug.LogWarning($"Player {senderId} is on message cooldown");
                    return;
                }
            }

            // Validate message
            if (string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(body))
            {
                Debug.LogWarning("Subject and body are required");
                return;
            }

            if (body.Length > maxMessageLength)
            {
                Debug.LogWarning($"Message exceeds max length of {maxMessageLength}");
                return;
            }

            // Check friendship requirement
            if (requireFriendship && senderId != 0)
            {
                // Would integrate with friend system
                // if (!FriendSystem.Instance.AreFriends(senderId, recipientId)) return;
            }

            var mail = new MailMessage
            {
                mailId = $"mail_{Guid.NewGuid()}",
                type = senderId == 0 ? MailType.SystemNotification : MailType.PlayerMessage,
                priority = MailPriority.Normal,
                status = MailStatus.Unread,
                senderId = senderId,
                senderName = GetPlayerName(senderId),
                recipientId = recipientId,
                subject = subject,
                body = body,
                sentDate = DateTime.UtcNow,
                expirationDate = DateTime.UtcNow.AddDays(mailRetentionDays),
                attachments = attachments != null ? attachments.ToList() : new List<MailAttachment>(),
                hasAttachments = attachments != null && attachments.Length > 0
            };

            DeliverMail(mail);

            // Update cooldown
            lastMessageTime[senderId] = DateTime.UtcNow;

            Debug.Log($"Mail sent from {senderId} to {recipientId}");
        }

        public void SendSystemMail(ulong recipientId, string templateId, Dictionary<string, string> placeholders = null, MailAttachment[] attachments = null)
        {
            if (!mailTemplates.ContainsKey(templateId))
            {
                Debug.LogWarning($"Mail template {templateId} not found");
                return;
            }

            var template = mailTemplates[templateId];
            string subject = template.subject;
            string body = template.body;

            // Replace placeholders
            if (placeholders != null)
            {
                foreach (var kvp in placeholders)
                {
                    subject = subject.Replace($"{{{kvp.Key}}}", kvp.Value);
                    body = body.Replace($"{{{kvp.Key}}}", kvp.Value);
                }
            }

            var mail = new MailMessage
            {
                mailId = $"mail_{Guid.NewGuid()}",
                type = template.type,
                priority = template.priority,
                status = MailStatus.Unread,
                senderId = 0, // System
                senderName = "System",
                recipientId = recipientId,
                subject = subject,
                body = body,
                sentDate = DateTime.UtcNow,
                expirationDate = DateTime.UtcNow.AddDays(mailRetentionDays),
                attachments = attachments != null ? attachments.ToList() : new List<MailAttachment>(),
                hasAttachments = attachments != null && attachments.Length > 0
            };

            DeliverMail(mail);
        }

        private void DeliverMail(MailMessage mail)
        {
            // Get or create mailbox
            if (!playerMailboxes.ContainsKey(mail.recipientId))
            {
                playerMailboxes[mail.recipientId] = new PlayerMailbox
                {
                    playerId = mail.recipientId
                };
            }

            var mailbox = playerMailboxes[mail.recipientId];

            // Check mailbox size
            if (mailbox.inbox.Count >= maxMailboxSize)
            {
                // Remove oldest mail
                var oldest = mailbox.inbox.OrderBy(m => m.sentDate).First();
                mailbox.inbox.Remove(oldest);
            }

            mailbox.inbox.Add(mail);
            mailbox.unreadCount++;

            if (mail.hasAttachments)
            {
                mailbox.unclaimedCount++;
            }

            OnMailReceived?.Invoke(mail.recipientId, mail);
            NotifyMailReceivedClientRpc(mail.recipientId, mail);

            Debug.Log($"Mail delivered to {mail.recipientId}");
        }

        [ClientRpc]
        private void NotifyMailReceivedClientRpc(ulong recipientId, MailMessage mail)
        {
            if (NetworkManager.Singleton.LocalClientId != recipientId) return;

            OnMailReceived?.Invoke(recipientId, mail);
        }

        #endregion

        #region Read Mail

        [ServerRpc(RequireOwnership = false)]
        public void ReadMailServerRpc(ulong playerId, string mailId)
        {
            if (!playerMailboxes.ContainsKey(playerId))
            {
                Debug.LogWarning($"Player {playerId} mailbox not found");
                return;
            }

            var mailbox = playerMailboxes[playerId];
            var mail = mailbox.inbox.FirstOrDefault(m => m.mailId == mailId);

            if (mail == null)
            {
                Debug.LogWarning($"Mail {mailId} not found in player {playerId} mailbox");
                return;
            }

            if (mail.status == MailStatus.Unread)
            {
                mail.status = MailStatus.Read;
                mail.readDate = DateTime.UtcNow;
                mailbox.unreadCount--;
                mailbox.lastChecked = DateTime.UtcNow;

                OnMailRead?.Invoke(playerId, mail);
                OnUnreadCountChanged?.Invoke(playerId, mailbox.unreadCount);

                Debug.Log($"Player {playerId} read mail {mailId}");
            }
        }

        #endregion

        #region Claim Attachments

        [ServerRpc(RequireOwnership = false)]
        public void ClaimAttachmentsServerRpc(ulong playerId, string mailId)
        {
            if (!playerMailboxes.ContainsKey(playerId))
            {
                Debug.LogWarning($"Player {playerId} mailbox not found");
                return;
            }

            var mailbox = playerMailboxes[playerId];
            var mail = mailbox.inbox.FirstOrDefault(m => m.mailId == mailId);

            if (mail == null || !mail.hasAttachments || mail.attachmentsClaimed)
            {
                Debug.LogWarning($"No claimable attachments in mail {mailId}");
                return;
            }

            // Grant attachments
            foreach (var attachment in mail.attachments)
            {
                if (attachment.claimed) continue;

                switch (attachment.type)
                {
                    case AttachmentType.Item:
                        Inventory.InventoryManager.Instance?.AddItem(playerId, attachment.itemId, attachment.quantity);
                        break;

                    case AttachmentType.SoftCurrency:
                        Economy.EconomyManager.Instance?.AddSoftCurrency(playerId, attachment.softCurrency);
                        break;

                    case AttachmentType.HardCurrency:
                        Economy.EconomyManager.Instance?.AddHardCurrency(playerId, attachment.hardCurrency);
                        break;

                    case AttachmentType.Mixed:
                        if (attachment.softCurrency > 0)
                            Economy.EconomyManager.Instance?.AddSoftCurrency(playerId, attachment.softCurrency);
                        if (attachment.hardCurrency > 0)
                            Economy.EconomyManager.Instance?.AddHardCurrency(playerId, attachment.hardCurrency);
                        if (!string.IsNullOrEmpty(attachment.itemId))
                            Inventory.InventoryManager.Instance?.AddItem(playerId, attachment.itemId, attachment.quantity);
                        break;
                }

                attachment.claimed = true;
            }

            mail.attachmentsClaimed = true;
            mail.status = MailStatus.Claimed;
            mailbox.unclaimedCount--;

            OnAttachmentClaimed?.Invoke(playerId, mailId);

            Debug.Log($"Player {playerId} claimed attachments from mail {mailId}");
        }

        #endregion

        #region Delete Mail

        [ServerRpc(RequireOwnership = false)]
        public void DeleteMailServerRpc(ulong playerId, string mailId)
        {
            if (!playerMailboxes.ContainsKey(playerId))
            {
                Debug.LogWarning($"Player {playerId} mailbox not found");
                return;
            }

            var mailbox = playerMailboxes[playerId];
            var mail = mailbox.inbox.FirstOrDefault(m => m.mailId == mailId);

            if (mail == null)
            {
                Debug.LogWarning($"Mail {mailId} not found");
                return;
            }

            // Can't delete if has unclaimed attachments
            if (mail.hasAttachments && !mail.attachmentsClaimed)
            {
                Debug.LogWarning($"Cannot delete mail with unclaimed attachments");
                return;
            }

            mailbox.inbox.Remove(mail);

            Debug.Log($"Player {playerId} deleted mail {mailId}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void DeleteAllReadMailServerRpc(ulong playerId)
        {
            if (!playerMailboxes.ContainsKey(playerId))
                return;

            var mailbox = playerMailboxes[playerId];

            var toDelete = mailbox.inbox
                .Where(m => m.status == MailStatus.Read && (!m.hasAttachments || m.attachmentsClaimed))
                .ToList();

            foreach (var mail in toDelete)
            {
                mailbox.inbox.Remove(mail);
            }

            Debug.Log($"Player {playerId} deleted {toDelete.Count} read mail");
        }

        #endregion

        #region Cleanup

        private void CleanupExpiredMail()
        {
            DateTime now = DateTime.UtcNow;

            foreach (var mailbox in playerMailboxes.Values)
            {
                var expired = mailbox.inbox
                    .Where(m => m.expirationDate.HasValue && now > m.expirationDate.Value && (!m.hasAttachments || m.attachmentsClaimed))
                    .ToList();

                foreach (var mail in expired)
                {
                    mailbox.inbox.Remove(mail);
                }
            }
        }

        #endregion

        #region Public API

        public PlayerMailbox GetMailbox(ulong playerId)
        {
            if (!playerMailboxes.ContainsKey(playerId))
            {
                playerMailboxes[playerId] = new PlayerMailbox
                {
                    playerId = playerId
                };
            }

            return playerMailboxes[playerId];
        }

        public int GetUnreadCount(ulong playerId)
        {
            if (!playerMailboxes.ContainsKey(playerId))
                return 0;

            return playerMailboxes[playerId].unreadCount;
        }

        public int GetUnclaimedCount(ulong playerId)
        {
            if (!playerMailboxes.ContainsKey(playerId))
                return 0;

            return playerMailboxes[playerId].unclaimedCount;
        }

        public List<MailMessage> GetInbox(ulong playerId)
        {
            if (!playerMailboxes.ContainsKey(playerId))
                return new List<MailMessage>();

            return playerMailboxes[playerId].inbox
                .OrderByDescending(m => m.priority)
                .ThenByDescending(m => m.sentDate)
                .ToList();
        }

        public List<MailMessage> GetUnreadMail(ulong playerId)
        {
            if (!playerMailboxes.ContainsKey(playerId))
                return new List<MailMessage>();

            return playerMailboxes[playerId].inbox
                .Where(m => m.status == MailStatus.Unread)
                .OrderByDescending(m => m.priority)
                .ThenByDescending(m => m.sentDate)
                .ToList();
        }

        private string GetPlayerName(ulong playerId)
        {
            if (playerId == 0) return "System";
            return $"Player_{playerId}";
        }

        #endregion
    }
}
