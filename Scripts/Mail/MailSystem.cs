using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Mail
{
    public class MailSystem : NetworkBehaviour
    {
        public static MailSystem Instance { get; private set; }

        private Dictionary<ulong, List<MailMessage>> playerMailboxes = new Dictionary<ulong, List<MailMessage>>();
        
        public event Action<ulong, string> OnMailReceived;
        public event Action<ulong, string> OnMailRead;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        [ServerRpc(RequireOwnership = false)]
        public void SendMailServerRpc(ulong recipientId, string subject, string body, MailType type, List<MailAttachment> attachments, ServerRpcParams rpcParams = default)
        {
            if (!playerMailboxes.ContainsKey(recipientId))
            {
                playerMailboxes[recipientId] = new List<MailMessage>();
            }

            var mail = new MailMessage
            {
                mailId = Guid.NewGuid().ToString(),
                subject = subject,
                body = body,
                mailType = type,
                attachments = attachments ?? new List<MailAttachment>(),
                sentDate = DateTime.UtcNow,
                expiryDate = DateTime.UtcNow.AddDays(30),
                isRead = false,
                attachmentsClaimed = false
            };

            playerMailboxes[recipientId].Add(mail);
            OnMailReceived?.Invoke(recipientId, mail.mailId);
            NotifyNewMailClientRpc(recipientId);

            Debug.Log($"Mail sent to player {recipientId}: {subject}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void SendSystemRewardMailServerRpc(ulong playerId, string subject, string body, List<MailAttachment> rewards, ServerRpcParams rpcParams = default)
        {
            SendMailServerRpc(playerId, subject, body, MailType.SystemReward, rewards);
        }

        [ServerRpc(RequireOwnership = false)]
        public void MarkMailAsReadServerRpc(ulong playerId, string mailId, ServerRpcParams rpcParams = default)
        {
            if (!playerMailboxes.TryGetValue(playerId, out var mailbox)) return;

            var mail = mailbox.FirstOrDefault(m => m.mailId == mailId);
            if (mail != null && !mail.isRead)
            {
                mail.isRead = true;
                OnMailRead?.Invoke(playerId, mailId);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void ClaimMailAttachmentsServerRpc(ulong playerId, string mailId, ServerRpcParams rpcParams = default)
        {
            if (!playerMailboxes.TryGetValue(playerId, out var mailbox)) return;

            var mail = mailbox.FirstOrDefault(m => m.mailId == mailId);
            if (mail != null && !mail.attachmentsClaimed && mail.attachments.Count > 0)
            {
                foreach (var attachment in mail.attachments)
                {
                    switch (attachment.attachmentType)
                    {
                        case AttachmentType.Currency:
                            Economy.EconomyManager.Instance?.AddCurrencyServerRpc(playerId, Economy.CurrencyType.Soft, attachment.quantity);
                            break;
                        case AttachmentType.Item:
                            Inventory.InventorySystem.Instance?.AddItemServerRpc(playerId, attachment.itemId, attachment.quantity, Inventory.ContainerType.Backpack);
                            break;
                        case AttachmentType.Cosmetic:
                            Cosmetics.CosmeticSystem.Instance?.UnlockCosmeticServerRpc(playerId, attachment.itemId);
                            break;
                    }
                }

                mail.attachmentsClaimed = true;
                Debug.Log($"Player {playerId} claimed mail attachments from {mailId}");
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void DeleteMailServerRpc(ulong playerId, string mailId, ServerRpcParams rpcParams = default)
        {
            if (!playerMailboxes.TryGetValue(playerId, out var mailbox)) return;

            var mail = mailbox.FirstOrDefault(m => m.mailId == mailId);
            if (mail != null)
            {
                mailbox.Remove(mail);
            }
        }

        [ClientRpc]
        private void NotifyNewMailClientRpc(ulong playerId) { }

        public List<MailMessage> GetPlayerMail(ulong playerId) => playerMailboxes.GetValueOrDefault(playerId, new List<MailMessage>());
        public int GetUnreadCount(ulong playerId)
        {
            if (!playerMailboxes.TryGetValue(playerId, out var mailbox)) return 0;
            return mailbox.Count(m => !m.isRead);
        }
    }

    [Serializable]
    public class MailMessage
    {
        public string mailId;
        public string subject;
        public string body;
        public MailType mailType;
        public List<MailAttachment> attachments;
        public DateTime sentDate;
        public DateTime expiryDate;
        public bool isRead;
        public bool attachmentsClaimed;
    }

    [Serializable]
    public class MailAttachment
    {
        public AttachmentType attachmentType;
        public string itemId;
        public int quantity;
    }

    public enum MailType { PlayerMessage, SystemNotification, SystemReward, ClanMessage, EventReward }
    public enum AttachmentType { Currency, Item, Cosmetic }
}
