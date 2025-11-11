using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

namespace ZombieGame.UI
{
    /// <summary>
    /// Real-time kill feed system displaying kills, deaths, and notable events
    /// with icons, colors, and animations in the game UI.
    /// </summary>
    public class KillFeedSystem : NetworkBehaviour
    {
        public static KillFeedSystem Instance { get; private set; }

        [Header("Kill Feed Configuration")]
        [SerializeField] private GameObject killFeedEntryPrefab;
        [SerializeField] private Transform killFeedContainer;
        [SerializeField] private int maxEntries = 5;
        [SerializeField] private float entryLifetime = 5f;
        [SerializeField] private float entryFadeTime = 1f;

        private Queue<GameObject> activeEntries = new Queue<GameObject>();

        public event Action<KillFeedEntry> OnKillFeedEntryAdded;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        /// <summary>
        /// Add kill to feed
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void AddKillServerRpc(ulong killerId, ulong victimId, string weaponUsed,
            bool wasHeadshot, KillFeedEventType eventType, ServerRpcParams rpcParams = default)
        {
            KillFeedEntry entry = new KillFeedEntry
            {
                killerId = killerId,
                victimId = victimId,
                weaponUsed = weaponUsed,
                wasHeadshot = wasHeadshot,
                eventType = eventType,
                timestamp = DateTime.UtcNow
            };

            AddKillFeedEntryClientRpc(entry);
            OnKillFeedEntryAdded?.Invoke(entry);
        }

        [ClientRpc]
        private void AddKillFeedEntryClientRpc(KillFeedEntry entry)
        {
            CreateKillFeedEntry(entry);
        }

        private void CreateKillFeedEntry(KillFeedEntry entry)
        {
            if (killFeedContainer == null) return;

            // Create entry UI
            GameObject entryObj = Instantiate(killFeedEntryPrefab, killFeedContainer);

            // Get player names (would integrate with player system)
            string killerName = GetPlayerName(entry.killerId);
            string victimName = GetPlayerName(entry.victimId);

            // Format entry text
            string entryText = FormatKillFeedText(entry, killerName, victimName);
            string colorCode = GetEventColor(entry.eventType);

            // Set text
            TextMeshProUGUI textComponent = entryObj.GetComponent<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = $"<color={colorCode}>{entryText}</color>";

                // Add headshot icon
                if (entry.wasHeadshot)
                {
                    textComponent.text += " <color=red>☠</color>";
                }

                // Add special event icons
                textComponent.text += GetEventIcon(entry.eventType);
            }

            // Add to queue
            activeEntries.Enqueue(entryObj);

            // Remove old entries
            while (activeEntries.Count > maxEntries)
            {
                GameObject oldEntry = activeEntries.Dequeue();
                if (oldEntry != null)
                {
                    Destroy(oldEntry);
                }
            }

            // Auto-fade and destroy
            StartCoroutine(FadeAndDestroyEntry(entryObj));
        }

        private string FormatKillFeedText(KillFeedEntry entry, string killerName, string victimName)
        {
            return entry.eventType switch
            {
                KillFeedEventType.Kill => $"{killerName} [{entry.weaponUsed}] {victimName}",
                KillFeedEventType.Suicide => $"{victimName} took their own life",
                KillFeedEventType.TeamKill => $"{killerName} [TEAM KILL] {victimName}",
                KillFeedEventType.MeleeKill => $"{killerName} [MELEE] {victimName}",
                KillFeedEventType.ExplosiveKill => $"{killerName} [EXPLOSIVE] {victimName}",
                KillFeedEventType.EnvironmentalKill => $"{victimName} died to environment",
                KillFeedEventType.FirstBlood => $"{killerName} [FIRST BLOOD] {victimName}",
                KillFeedEventType.Revenge => $"{killerName} [REVENGE] {victimName}",
                KillFeedEventType.DominatingKill => $"{killerName} is DOMINATING {victimName}",
                KillFeedEventType.ShutdownKill => $"{killerName} SHUTDOWN {victimName}'s spree!",
                _ => $"{killerName} eliminated {victimName}"
            };
        }

        private string GetEventColor(KillFeedEventType eventType)
        {
            return eventType switch
            {
                KillFeedEventType.Kill => "white",
                KillFeedEventType.Suicide => "gray",
                KillFeedEventType.TeamKill => "orange",
                KillFeedEventType.MeleeKill => "yellow",
                KillFeedEventType.ExplosiveKill => "red",
                KillFeedEventType.EnvironmentalKill => "green",
                KillFeedEventType.FirstBlood => "gold",
                KillFeedEventType.Revenge => "purple",
                KillFeedEventType.DominatingKill => "red",
                KillFeedEventType.ShutdownKill => "cyan",
                _ => "white"
            };
        }

        private string GetEventIcon(KillFeedEventType eventType)
        {
            return eventType switch
            {
                KillFeedEventType.FirstBlood => " ⭐",
                KillFeedEventType.DominatingKill => " 🔥",
                KillFeedEventType.ShutdownKill => " ⚡",
                KillFeedEventType.Revenge => " ⚔",
                _ => ""
            };
        }

        private IEnumerator FadeAndDestroyEntry(GameObject entryObj)
        {
            yield return new WaitForSeconds(entryLifetime - entryFadeTime);

            // Fade out
            TextMeshProUGUI textComponent = entryObj.GetComponent<TextMeshProUGUI>();
            if (textComponent != null)
            {
                float elapsed = 0f;
                Color startColor = textComponent.color;

                while (elapsed < entryFadeTime)
                {
                    elapsed += Time.deltaTime;
                    float alpha = 1f - (elapsed / entryFadeTime);
                    textComponent.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                    yield return null;
                }
            }

            Destroy(entryObj);
        }

        private string GetPlayerName(ulong playerId)
        {
            // Would integrate with player profile system
            return $"Player{playerId}";
        }

        /// <summary>
        /// Add special event notification
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void AddEventNotificationServerRpc(string message, Color color, ServerRpcParams rpcParams = default)
        {
            AddEventNotificationClientRpc(message, color);
        }

        [ClientRpc]
        private void AddEventNotificationClientRpc(string message, Color color)
        {
            if (killFeedContainer == null) return;

            GameObject entryObj = Instantiate(killFeedEntryPrefab, killFeedContainer);
            TextMeshProUGUI textComponent = entryObj.GetComponent<TextMeshProUGUI>();

            if (textComponent != null)
            {
                textComponent.text = $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{message}</color>";
                textComponent.fontSize *= 1.2f; // Larger for events
            }

            activeEntries.Enqueue(entryObj);

            while (activeEntries.Count > maxEntries)
            {
                GameObject oldEntry = activeEntries.Dequeue();
                if (oldEntry != null) Destroy(oldEntry);
            }

            StartCoroutine(FadeAndDestroyEntry(entryObj));
        }

        public void ClearKillFeed()
        {
            while (activeEntries.Count > 0)
            {
                GameObject entry = activeEntries.Dequeue();
                if (entry != null) Destroy(entry);
            }
        }

        [Serializable]
        public class KillFeedEntry
        {
            public ulong killerId;
            public ulong victimId;
            public string weaponUsed;
            public bool wasHeadshot;
            public KillFeedEventType eventType;
            public DateTime timestamp;
        }

        public enum KillFeedEventType
        {
            Kill,
            Suicide,
            TeamKill,
            MeleeKill,
            ExplosiveKill,
            EnvironmentalKill,
            FirstBlood,
            Revenge,
            DominatingKill,
            ShutdownKill
        }
    }
}
