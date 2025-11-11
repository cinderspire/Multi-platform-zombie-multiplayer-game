using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Progression
{
    /// <summary>
    /// Player title system managing unlockable titles and badges that players
    /// can earn through achievements and display as prestige markers.
    /// </summary>
    public class TitleSystem : NetworkBehaviour
    {
        public static TitleSystem Instance { get; private set; }

        [Header("Title Configuration")]
        [SerializeField] private List<TitleDefinition> allTitles = new List<TitleDefinition>();

        private Dictionary<ulong, PlayerTitles> playerTitles = new Dictionary<ulong, PlayerTitles>();
        private Dictionary<string, TitleDefinition> titleDatabase = new Dictionary<string, TitleDefinition>();

        public event Action<ulong, string> OnTitleUnlocked;
        public event Action<ulong, string> OnTitleEquipped;
        public event Action<ulong, string> OnBadgeUnlocked;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            InitializeTitleDatabase();
        }

        private void InitializeTitleDatabase()
        {
            // Initialize default titles
            allTitles = new List<TitleDefinition>
            {
                // Starter Titles
                new TitleDefinition { titleId = "rookie", displayName = "Rookie", rarity = TitleRarity.Common,
                    description = "Just getting started", unlockRequirement = "Play first game" },
                new TitleDefinition { titleId = "survivor", displayName = "Survivor", rarity = TitleRarity.Common,
                    description = "Survive 10 rounds", unlockRequirement = "Survive 10 rounds" },

                // Combat Titles
                new TitleDefinition { titleId = "headhunter", displayName = "Headhunter", rarity = TitleRarity.Rare,
                    description = "Master of headshots", unlockRequirement = "Get 100 headshots" },
                new TitleDefinition { titleId = "slayer", displayName = "Slayer", rarity = TitleRarity.Rare,
                    description = "Eliminate 1000 zombies", unlockRequirement = "Kill 1000 zombies" },
                new TitleDefinition { titleId = "assassin", displayName = "Assassin", rarity = TitleRarity.Epic,
                    description = "Silent but deadly", unlockRequirement = "Get 50 stealth kills" },

                // Prestige Titles
                new TitleDefinition { titleId = "veteran", displayName = "Veteran", rarity = TitleRarity.Epic,
                    description = "Seasoned warrior", unlockRequirement = "Reach level 50" },
                new TitleDefinition { titleId = "elite", displayName = "Elite", rarity = TitleRarity.Legendary,
                    description = "Among the best", unlockRequirement = "Reach level 100" },
                new TitleDefinition { titleId = "master", displayName = "Master", rarity = TitleRarity.Legendary,
                    description = "True mastery achieved", unlockRequirement = "Prestige rank 5" },
                new TitleDefinition { titleId = "legend", displayName = "Legend", rarity = TitleRarity.Mythic,
                    description = "Legendary status", unlockRequirement = "Prestige rank 10" },

                // Special Achievement Titles
                new TitleDefinition { titleId = "lone_wolf", displayName = "Lone Wolf", rarity = TitleRarity.Epic,
                    description = "Solo victory", unlockRequirement = "Win a match solo" },
                new TitleDefinition { titleId = "commander", displayName = "Commander", rarity = TitleRarity.Epic,
                    description = "Natural leader", unlockRequirement = "Lead team to 10 victories" },
                new TitleDefinition { titleId = "perfectionist", displayName = "Perfectionist", rarity = TitleRarity.Legendary,
                    description = "Flawless execution", unlockRequirement = "Complete round without damage" },

                // Event Titles
                new TitleDefinition { titleId = "event_champion", displayName = "Event Champion", rarity = TitleRarity.Legendary,
                    description = "Event master", unlockRequirement = "Win seasonal event" },
                new TitleDefinition { titleId = "tournament_winner", displayName = "Tournament Winner", rarity = TitleRarity.Mythic,
                    description = "Tournament victor", unlockRequirement = "Win official tournament" },

                // Clan Titles
                new TitleDefinition { titleId = "clan_leader", displayName = "Clan Leader", rarity = TitleRarity.Rare,
                    description = "Leads a clan", unlockRequirement = "Be a clan leader" },
                new TitleDefinition { titleId = "warlord", displayName = "Warlord", rarity = TitleRarity.Legendary,
                    description = "Clan war champion", unlockRequirement = "Win 10 clan wars" },

                // Unique Titles
                new TitleDefinition { titleId = "immortal", displayName = "The Immortal", rarity = TitleRarity.Mythic,
                    description = "Defied death itself", unlockRequirement = "Survive 100 rounds" },
                new TitleDefinition { titleId = "apocalypse", displayName = "Apocalypse", rarity = TitleRarity.Mythic,
                    description = "Harbinger of doom", unlockRequirement = "Get 10000 kills" }
            };

            foreach (var title in allTitles)
            {
                titleDatabase[title.titleId] = title;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void InitializePlayerTitlesServerRpc(ulong playerId, ServerRpcParams rpcParams = default)
        {
            if (!playerTitles.ContainsKey(playerId))
            {
                playerTitles[playerId] = new PlayerTitles
                {
                    playerId = playerId,
                    unlockedTitles = new List<string> { "rookie" }, // Everyone starts with rookie
                    equippedTitle = "rookie",
                    unlockedBadges = new List<string>()
                };
            }
        }

        /// <summary>
        /// Unlock a title for a player
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void UnlockTitleServerRpc(ulong playerId, string titleId, ServerRpcParams rpcParams = default)
        {
            if (!titleDatabase.ContainsKey(titleId)) return;
            if (!playerTitles.ContainsKey(playerId))
            {
                InitializePlayerTitlesServerRpc(playerId);
            }

            var titles = playerTitles[playerId];
            if (!titles.unlockedTitles.Contains(titleId))
            {
                titles.unlockedTitles.Add(titleId);
                titles.totalTitlePoints += GetTitlePoints(titleId);

                OnTitleUnlocked?.Invoke(playerId, titleId);
                NotifyTitleUnlockedClientRpc(playerId, titleId);
            }
        }

        /// <summary>
        /// Equip a title for display
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void EquipTitleServerRpc(ulong playerId, string titleId, ServerRpcParams rpcParams = default)
        {
            if (!playerTitles.TryGetValue(playerId, out var titles)) return;
            if (!titles.unlockedTitles.Contains(titleId)) return;

            titles.equippedTitle = titleId;
            OnTitleEquipped?.Invoke(playerId, titleId);
            UpdatePlayerTitleClientRpc(playerId, titleId);
        }

        /// <summary>
        /// Unlock a badge
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void UnlockBadgeServerRpc(ulong playerId, string badgeId, ServerRpcParams rpcParams = default)
        {
            if (!playerTitles.ContainsKey(playerId))
            {
                InitializePlayerTitlesServerRpc(playerId);
            }

            var titles = playerTitles[playerId];
            if (!titles.unlockedBadges.Contains(badgeId))
            {
                titles.unlockedBadges.Add(badgeId);
                OnBadgeUnlocked?.Invoke(playerId, badgeId);
                NotifyBadgeUnlockedClientRpc(playerId, badgeId);
            }
        }

        [ClientRpc]
        private void NotifyTitleUnlockedClientRpc(ulong playerId, string titleId)
        {
            if (NetworkManager.Singleton.LocalClientId == playerId)
            {
                TitleDefinition title = titleDatabase[titleId];
                Debug.Log($"<color=gold>TITLE UNLOCKED: {title.displayName}</color>");
            }
        }

        [ClientRpc]
        private void UpdatePlayerTitleClientRpc(ulong playerId, string titleId)
        {
            // Update UI to show equipped title
        }

        [ClientRpc]
        private void NotifyBadgeUnlockedClientRpc(ulong playerId, string badgeId)
        {
            if (NetworkManager.Singleton.LocalClientId == playerId)
            {
                Debug.Log($"<color=silver>BADGE UNLOCKED: {badgeId}</color>");
            }
        }

        private int GetTitlePoints(string titleId)
        {
            if (!titleDatabase.TryGetValue(titleId, out var title)) return 0;

            return title.rarity switch
            {
                TitleRarity.Common => 10,
                TitleRarity.Rare => 25,
                TitleRarity.Epic => 50,
                TitleRarity.Legendary => 100,
                TitleRarity.Mythic => 250,
                _ => 0
            };
        }

        public string GetPlayerTitle(ulong playerId)
        {
            if (!playerTitles.TryGetValue(playerId, out var titles)) return "rookie";
            return titles.equippedTitle;
        }

        public string GetTitleDisplayName(string titleId)
        {
            return titleDatabase.TryGetValue(titleId, out var title) ? title.displayName : titleId;
        }

        public List<string> GetUnlockedTitles(ulong playerId)
        {
            return playerTitles.TryGetValue(playerId, out var titles) ?
                new List<string>(titles.unlockedTitles) : new List<string>();
        }

        public int GetTitleCount(ulong playerId)
        {
            return playerTitles.TryGetValue(playerId, out var titles) ? titles.unlockedTitles.Count : 0;
        }

        [Serializable]
        private class PlayerTitles
        {
            public ulong playerId;
            public List<string> unlockedTitles = new List<string>();
            public string equippedTitle;
            public List<string> unlockedBadges = new List<string>();
            public int totalTitlePoints;
        }

        [Serializable]
        public class TitleDefinition
        {
            public string titleId;
            public string displayName;
            public string description;
            public TitleRarity rarity;
            public string unlockRequirement;
            public string iconPath;
        }

        public enum TitleRarity
        {
            Common,
            Rare,
            Epic,
            Legendary,
            Mythic
        }
    }
}
