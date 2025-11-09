using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace News
{
    /// <summary>
    /// Comprehensive news and message of the day (MOTD) system for multi-platform zombie multiplayer game.
    /// Handles game news, announcements, patch notes, events, and server messages.
    /// </summary>
    public class NewsSystem : NetworkBehaviour
    {
        public static NewsSystem Instance { get; private set; }

        [Header("News Settings")]
        [SerializeField] private bool enableNewsSystem = true;
        [SerializeField] private int maxNewsArticles = 100;
        [SerializeField] private int newsDisplayDuration = 7; // days
        [SerializeField] private bool autoShowNews = true;

        [Header("MOTD Settings")]
        [SerializeField] private bool enableMOTD = true;
        [SerializeField] private bool showMOTDOnLogin = true;
        [SerializeField] private float motdRefreshInterval = 3600f; // 1 hour

        [Header("Notification Settings")]
        [SerializeField] private bool enablePopupNews = true;
        [SerializeField] private int maxPopupDuration = 30; // seconds
        [SerializeField] private bool enableNewsNotifications = true;

        // Enums
        public enum NewsCategory
        {
            General,
            PatchNotes,
            Event,
            Maintenance,
            Community,
            Competition,
            Store,
            Emergency
        }

        public enum NewsPriority
        {
            Low,
            Normal,
            High,
            Critical
        }

        public enum NewsStatus
        {
            Draft,
            Published,
            Archived,
            Deleted
        }

        // Data structures
        [Serializable]
        public class NewsArticle
        {
            public string articleId;
            public string title;
            public string content;
            public string summary;
            public NewsCategory category;
            public NewsPriority priority;
            public NewsStatus status;
            public DateTime publishDate;
            public DateTime expirationDate;
            public string authorId;
            public string authorName;
            public string imageUrl;
            public string linkUrl;
            public List<string> tags = new List<string>();
            public Dictionary<string, object> metadata = new Dictionary<string, object>();
            public int viewCount;
            public bool requiresAcknowledgment;
            public bool showAsPopup;
        }

        [Serializable]
        public class MOTDMessage
        {
            public string motdId;
            public string message;
            public string title;
            public NewsPriority priority;
            public DateTime startDate;
            public DateTime endDate;
            public bool isActive;
            public string backgroundColor;
            public string textColor;
            public bool allowDismiss;
        }

        [Serializable]
        public class PlayerNewsData
        {
            public ulong playerId;
            public HashSet<string> viewedArticles = new HashSet<string>();
            public HashSet<string> acknowledgedArticles = new HashSet<string>();
            public DateTime lastNewsCheck;
            public Dictionary<NewsCategory, bool> categoryPreferences = new Dictionary<NewsCategory, bool>();
        }

        [Serializable]
        public class Announcement
        {
            public string announcementId;
            public string message;
            public NewsPriority priority;
            public DateTime timestamp;
            public float displayDuration;
            public bool serverWide;
            public List<ulong> targetPlayers = new List<ulong>();
        }

        // State
        private Dictionary<string, NewsArticle> newsArticles = new Dictionary<string, NewsArticle>();
        private List<MOTDMessage> activeMOTDs = new List<MOTDMessage>();
        private Dictionary<ulong, PlayerNewsData> playerNewsData = new Dictionary<ulong, PlayerNewsData>();
        private Queue<Announcement> pendingAnnouncements = new Queue<Announcement>();
        private float lastMOTDRefresh;

        // Events
        public event Action<NewsArticle> OnNewsPublished;
        public event Action<NewsArticle> OnNewsViewed;
        public event Action<MOTDMessage> OnMOTDUpdated;
        public event Action<Announcement> OnAnnouncementReceived;

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
                InitializeNewsSystem();
            }
        }

        private void Update()
        {
            if (!IsServer || !enableNewsSystem) return;

            if (Time.time - lastMOTDRefresh >= motdRefreshInterval)
            {
                RefreshMOTD();
                lastMOTDRefresh = Time.time;
            }

            ProcessPendingAnnouncements();
        }

        #region Initialization

        private void InitializeNewsSystem()
        {
            LoadNewsArticles();
            LoadMOTDMessages();
            CreateSampleNews();
        }

        private void LoadNewsArticles()
        {
            // Load from persistent storage or API
            // For now, we'll create sample data
        }

        private void LoadMOTDMessages()
        {
            // Load active MOTD messages
        }

        private void CreateSampleNews()
        {
            // Welcome message
            PublishNewsArticle(new NewsArticle
            {
                articleId = "welcome_001",
                title = "Welcome to the Apocalypse!",
                summary = "Your guide to surviving the zombie outbreak",
                content = "Welcome to our multiplayer zombie survival game! This guide will help you get started...",
                category = NewsCategory.General,
                priority = NewsPriority.Normal,
                status = NewsStatus.Published,
                publishDate = DateTime.UtcNow,
                expirationDate = DateTime.UtcNow.AddDays(newsDisplayDuration),
                authorName = "Game Team",
                showAsPopup = false
            });

            // Latest patch
            PublishNewsArticle(new NewsArticle
            {
                articleId = "patch_001",
                title = "Patch 1.0 - Launch Update",
                summary = "New features, balance changes, and bug fixes",
                content = "Patch 1.0 brings exciting new content including new weapons, improved AI, and various gameplay improvements...",
                category = NewsCategory.PatchNotes,
                priority = NewsPriority.High,
                status = NewsStatus.Published,
                publishDate = DateTime.UtcNow,
                expirationDate = DateTime.UtcNow.AddDays(newsDisplayDuration),
                authorName = "Development Team",
                requiresAcknowledgment = true
            });
        }

        #endregion

        #region News Management

        [ServerRpc(RequireOwnership = false)]
        public void PublishNewsArticleServerRpc(NewsArticle article)
        {
            PublishNewsArticle(article);
        }

        private void PublishNewsArticle(NewsArticle article)
        {
            if (newsArticles.Count >= maxNewsArticles)
            {
                // Remove oldest article
                var oldest = newsArticles.Values
                    .OrderBy(a => a.publishDate)
                    .First();
                newsArticles.Remove(oldest.articleId);
            }

            article.articleId = string.IsNullOrEmpty(article.articleId)
                ? $"news_{Guid.NewGuid()}"
                : article.articleId;

            article.status = NewsStatus.Published;
            article.publishDate = DateTime.UtcNow;

            if (article.expirationDate == default)
            {
                article.expirationDate = DateTime.UtcNow.AddDays(newsDisplayDuration);
            }

            newsArticles[article.articleId] = article;

            OnNewsPublished?.Invoke(article);
            BroadcastNewsArticleClientRpc(article);

            Debug.Log($"News published: {article.title}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void ArchiveNewsArticleServerRpc(string articleId)
        {
            if (!newsArticles.ContainsKey(articleId)) return;

            newsArticles[articleId].status = NewsStatus.Archived;

            Debug.Log($"News archived: {articleId}");
        }

        [ClientRpc]
        private void BroadcastNewsArticleClientRpc(NewsArticle article)
        {
            OnNewsPublished?.Invoke(article);

            if (article.showAsPopup && enablePopupNews)
            {
                // Show popup notification
                ShowNewsPopup(article);
            }
        }

        private void ShowNewsPopup(NewsArticle article)
        {
            Debug.Log($"[NEWS POPUP] {article.title}: {article.summary}");
            // Implement UI popup logic
        }

        #endregion

        #region Player News Interaction

        [ServerRpc(RequireOwnership = false)]
        public void MarkNewsAsViewedServerRpc(ulong playerId, string articleId)
        {
            if (!playerNewsData.ContainsKey(playerId))
            {
                playerNewsData[playerId] = new PlayerNewsData { playerId = playerId };
            }

            var data = playerNewsData[playerId];
            data.viewedArticles.Add(articleId);
            data.lastNewsCheck = DateTime.UtcNow;

            if (newsArticles.ContainsKey(articleId))
            {
                newsArticles[articleId].viewCount++;
                OnNewsViewed?.Invoke(newsArticles[articleId]);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void AcknowledgeNewsServerRpc(ulong playerId, string articleId)
        {
            if (!playerNewsData.ContainsKey(playerId))
            {
                playerNewsData[playerId] = new PlayerNewsData { playerId = playerId };
            }

            var data = playerNewsData[playerId];
            data.acknowledgedArticles.Add(articleId);

            Debug.Log($"Player {playerId} acknowledged news {articleId}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestUnreadNewsServerRpc(ulong playerId)
        {
            if (!playerNewsData.ContainsKey(playerId))
            {
                playerNewsData[playerId] = new PlayerNewsData { playerId = playerId };
            }

            var data = playerNewsData[playerId];
            var unreadNews = newsArticles.Values
                .Where(a => a.status == NewsStatus.Published &&
                           !data.viewedArticles.Contains(a.articleId) &&
                           DateTime.UtcNow < a.expirationDate)
                .OrderByDescending(a => a.priority)
                .ThenByDescending(a => a.publishDate)
                .ToList();

            SendUnreadNewsClientRpc(playerId, unreadNews.ToArray());
        }

        [ClientRpc]
        private void SendUnreadNewsClientRpc(ulong targetPlayerId, NewsArticle[] articles)
        {
            if (NetworkManager.Singleton.LocalClientId != targetPlayerId) return;

            foreach (var article in articles)
            {
                OnNewsPublished?.Invoke(article);
            }
        }

        #endregion

        #region MOTD Management

        [ServerRpc(RequireOwnership = false)]
        public void SetMOTDServerRpc(MOTDMessage motd)
        {
            motd.motdId = string.IsNullOrEmpty(motd.motdId)
                ? $"motd_{Guid.NewGuid()}"
                : motd.motdId;

            motd.isActive = true;
            motd.startDate = DateTime.UtcNow;

            if (motd.endDate == default)
            {
                motd.endDate = DateTime.UtcNow.AddDays(7);
            }

            activeMOTDs.Add(motd);

            OnMOTDUpdated?.Invoke(motd);
            BroadcastMOTDClientRpc(motd);

            Debug.Log($"MOTD set: {motd.title}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void ClearMOTDServerRpc(string motdId)
        {
            var motd = activeMOTDs.FirstOrDefault(m => m.motdId == motdId);
            if (motd != null)
            {
                motd.isActive = false;
                activeMOTDs.Remove(motd);
            }
        }

        [ClientRpc]
        private void BroadcastMOTDClientRpc(MOTDMessage motd)
        {
            OnMOTDUpdated?.Invoke(motd);

            if (showMOTDOnLogin)
            {
                ShowMOTDPopup(motd);
            }
        }

        private void ShowMOTDPopup(MOTDMessage motd)
        {
            Debug.Log($"[MOTD] {motd.title}: {motd.message}");
            // Implement UI popup logic
        }

        private void RefreshMOTD()
        {
            // Remove expired MOTDs
            activeMOTDs.RemoveAll(m => DateTime.UtcNow > m.endDate);

            // Update active MOTDs
            foreach (var motd in activeMOTDs)
            {
                if (DateTime.UtcNow >= motd.startDate && DateTime.UtcNow < motd.endDate)
                {
                    motd.isActive = true;
                }
                else
                {
                    motd.isActive = false;
                }
            }
        }

        public List<MOTDMessage> GetActiveMOTDs()
        {
            return activeMOTDs
                .Where(m => m.isActive && DateTime.UtcNow >= m.startDate && DateTime.UtcNow < m.endDate)
                .OrderByDescending(m => m.priority)
                .ToList();
        }

        #endregion

        #region Announcements

        [ServerRpc(RequireOwnership = false)]
        public void SendAnnouncementServerRpc(string message, NewsPriority priority, float duration, bool serverWide)
        {
            var announcement = new Announcement
            {
                announcementId = Guid.NewGuid().ToString(),
                message = message,
                priority = priority,
                timestamp = DateTime.UtcNow,
                displayDuration = duration,
                serverWide = serverWide
            };

            if (serverWide)
            {
                BroadcastAnnouncementClientRpc(announcement);
            }
            else
            {
                pendingAnnouncements.Enqueue(announcement);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void SendTargetedAnnouncementServerRpc(string message, NewsPriority priority, float duration, ulong[] targetPlayers)
        {
            var announcement = new Announcement
            {
                announcementId = Guid.NewGuid().ToString(),
                message = message,
                priority = priority,
                timestamp = DateTime.UtcNow,
                displayDuration = duration,
                serverWide = false,
                targetPlayers = targetPlayers.ToList()
            };

            foreach (var playerId in targetPlayers)
            {
                SendAnnouncementToPlayerClientRpc(playerId, announcement);
            }
        }

        [ClientRpc]
        private void BroadcastAnnouncementClientRpc(Announcement announcement)
        {
            OnAnnouncementReceived?.Invoke(announcement);
            DisplayAnnouncement(announcement);
        }

        [ClientRpc]
        private void SendAnnouncementToPlayerClientRpc(ulong targetPlayerId, Announcement announcement)
        {
            if (NetworkManager.Singleton.LocalClientId != targetPlayerId) return;

            OnAnnouncementReceived?.Invoke(announcement);
            DisplayAnnouncement(announcement);
        }

        private void DisplayAnnouncement(Announcement announcement)
        {
            string priorityTag = announcement.priority switch
            {
                NewsPriority.Critical => "[CRITICAL]",
                NewsPriority.High => "[IMPORTANT]",
                _ => "[INFO]"
            };

            Debug.Log($"{priorityTag} {announcement.message}");
            // Implement UI display logic
        }

        private void ProcessPendingAnnouncements()
        {
            while (pendingAnnouncements.Count > 0)
            {
                var announcement = pendingAnnouncements.Dequeue();
                BroadcastAnnouncementClientRpc(announcement);
            }
        }

        #endregion

        #region News Filtering

        public List<NewsArticle> GetNewsByCategory(NewsCategory category)
        {
            return newsArticles.Values
                .Where(a => a.category == category &&
                           a.status == NewsStatus.Published &&
                           DateTime.UtcNow < a.expirationDate)
                .OrderByDescending(a => a.publishDate)
                .ToList();
        }

        public List<NewsArticle> GetNewsByPriority(NewsPriority priority)
        {
            return newsArticles.Values
                .Where(a => a.priority == priority &&
                           a.status == NewsStatus.Published &&
                           DateTime.UtcNow < a.expirationDate)
                .OrderByDescending(a => a.publishDate)
                .ToList();
        }

        public List<NewsArticle> GetRecentNews(int count = 10)
        {
            return newsArticles.Values
                .Where(a => a.status == NewsStatus.Published &&
                           DateTime.UtcNow < a.expirationDate)
                .OrderByDescending(a => a.publishDate)
                .Take(count)
                .ToList();
        }

        public List<NewsArticle> SearchNews(string searchTerm)
        {
            return newsArticles.Values
                .Where(a => a.status == NewsStatus.Published &&
                           DateTime.UtcNow < a.expirationDate &&
                           (a.title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                            a.content.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                            a.tags.Any(t => t.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))))
                .OrderByDescending(a => a.publishDate)
                .ToList();
        }

        #endregion

        #region Public API

        public NewsArticle GetNewsArticle(string articleId)
        {
            return newsArticles.ContainsKey(articleId) ? newsArticles[articleId] : null;
        }

        public List<NewsArticle> GetAllActiveNews()
        {
            return newsArticles.Values
                .Where(a => a.status == NewsStatus.Published &&
                           DateTime.UtcNow < a.expirationDate)
                .OrderByDescending(a => a.priority)
                .ThenByDescending(a => a.publishDate)
                .ToList();
        }

        public int GetUnreadNewsCount(ulong playerId)
        {
            if (!playerNewsData.ContainsKey(playerId))
                return newsArticles.Count(a => a.status == NewsStatus.Published);

            var data = playerNewsData[playerId];
            return newsArticles.Values
                .Count(a => a.status == NewsStatus.Published &&
                           !data.viewedArticles.Contains(a.articleId) &&
                           DateTime.UtcNow < a.expirationDate);
        }

        public bool HasUnacknowledgedNews(ulong playerId)
        {
            if (!playerNewsData.ContainsKey(playerId))
                return newsArticles.Any(a => a.Value.requiresAcknowledgment);

            var data = playerNewsData[playerId];
            return newsArticles.Values
                .Any(a => a.status == NewsStatus.Published &&
                         a.requiresAcknowledgment &&
                         !data.acknowledgedArticles.Contains(a.articleId) &&
                         DateTime.UtcNow < a.expirationDate);
        }

        #endregion

        #region Player Preferences

        [ServerRpc(RequireOwnership = false)]
        public void SetCategoryPreferenceServerRpc(ulong playerId, NewsCategory category, bool enabled)
        {
            if (!playerNewsData.ContainsKey(playerId))
            {
                playerNewsData[playerId] = new PlayerNewsData { playerId = playerId };
            }

            playerNewsData[playerId].categoryPreferences[category] = enabled;
        }

        public bool IsCategoryEnabled(ulong playerId, NewsCategory category)
        {
            if (!playerNewsData.ContainsKey(playerId))
                return true; // Default all enabled

            var data = playerNewsData[playerId];
            return !data.categoryPreferences.ContainsKey(category) || data.categoryPreferences[category];
        }

        #endregion
    }
}
