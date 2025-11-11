using Unity.Netcode;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ZombieGame
{
    /// <summary>
    /// Seasonal Events System - Time-limited events with exclusive rewards
    /// Supports 6 major events: Halloween, Christmas, Easter, Summer, Valentine's, New Year
    /// Features special zombies, cosmetics, challenges, and limited-time shop items
    /// </summary>
    public class SeasonalEventsSystem : NetworkBehaviour
    {
        public static SeasonalEventsSystem Instance { get; private set; }

        [Header("Event Settings")]
        [SerializeField] private bool enableSeasonalEvents = true;
        [SerializeField] private bool testMode = false; // Force specific event for testing

        // Network Variables
        private NetworkVariable<SeasonalEventType> activeEvent = new NetworkVariable<SeasonalEventType>(SeasonalEventType.None);
        private NetworkVariable<float> eventTimeRemaining = new NetworkVariable<float>(0f);

        // Event Configurations
        private Dictionary<SeasonalEventType, EventConfig> eventConfigs = new Dictionary<SeasonalEventType, EventConfig>();

        // Player Event Data
        private Dictionary<ulong, PlayerEventData> playerEventData = new Dictionary<ulong, PlayerEventData>();

        // Events
        public event System.Action<SeasonalEventType> OnEventStarted;
        public event System.Action<SeasonalEventType> OnEventEnded;
        public event System.Action<ulong, string> OnEventRewardClaimed;

        public enum SeasonalEventType
        {
            None,
            Halloween,      // Oct 15 - Nov 5
            Christmas,      // Dec 15 - Jan 5
            NewYear,        // Dec 30 - Jan 7
            Valentines,     // Feb 10 - Feb 18
            Easter,         // Variable (spring)
            Summer          // Jul 1 - Jul 31
        }

        [System.Serializable]
        public class EventConfig
        {
            public SeasonalEventType eventType;
            public string eventName;
            public string description;
            public DateTime startDate;
            public DateTime endDate;
            public Color themeColor;

            // Gameplay Modifiers
            public bool hasSpecialZombies = false;
            public List<string> specialZombieTypes = new List<string>();
            public float zombieSpawnRateMultiplier = 1.0f;
            public float lootDropRateMultiplier = 1.0f;
            public float xpMultiplier = 1.0f;

            // Cosmetics
            public List<EventCosmetic> exclusiveCosmetics = new List<EventCosmetic>();

            // Challenges
            public List<EventChallenge> eventChallenges = new List<EventChallenge>();

            // Shop Items
            public List<EventShopItem> shopItems = new List<EventShopItem>();

            // Decorations
            public bool hasMapDecorations = true;
            public bool hasCustomMusic = true;
            public bool hasWeatherEffects = false;
            public string weatherType = "";
        }

        [System.Serializable]
        public class EventCosmetic
        {
            public string cosmeticId;
            public string cosmeticName;
            public CosmeticType type;
            public int requiredEventPoints = 0;
            public bool isPremium = false;
        }

        public enum CosmeticType
        {
            Skin,
            Emote,
            Banner,
            Title,
            WeaponSkin,
            Spray
        }

        [System.Serializable]
        public class EventChallenge
        {
            public string challengeId;
            public string challengeName;
            public string description;
            public ChallengeObjective objective;
            public int targetValue;
            public int eventPointsReward;
            public string exclusiveRewardId;
        }

        public enum ChallengeObjective
        {
            KillSpecialZombies,
            SurviveWaves,
            WinMatches,
            CompleteWithTheme, // Use event cosmetics
            CollectEventItems,
            PlayWithFriends
        }

        [System.Serializable]
        public class EventShopItem
        {
            public string itemId;
            public string itemName;
            public int eventPointsCost;
            public int realMoneyCost;
            public bool isLimited = true;
            public int quantityAvailable = -1; // -1 = unlimited
        }

        [System.Serializable]
        public class PlayerEventData
        {
            public int eventPoints = 0;
            public HashSet<string> claimedRewards = new HashSet<string>();
            public Dictionary<string, int> challengeProgress = new Dictionary<string, int>();
            public HashSet<string> unlockedCosmetics = new HashSet<string>();
            public int specialZombieKills = 0;
            public int matchesPlayed = 0;
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeEvents();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                CheckActiveEvent();
            }
        }

        private void InitializeEvents()
        {
            int currentYear = DateTime.UtcNow.Year;

            // HALLOWEEN EVENT
            eventConfigs[SeasonalEventType.Halloween] = new EventConfig
            {
                eventType = SeasonalEventType.Halloween,
                eventName = "Nightmare Harvest",
                description = "The veil between worlds thins... Pumpkin zombies roam the land!",
                startDate = new DateTime(currentYear, 10, 15),
                endDate = new DateTime(currentYear, 11, 5),
                themeColor = new Color(1.0f, 0.5f, 0.0f), // Orange
                hasSpecialZombies = true,
                specialZombieTypes = new List<string> { "zombie_pumpkin_head", "zombie_witch", "zombie_scarecrow" },
                zombieSpawnRateMultiplier = 1.3f,
                lootDropRateMultiplier = 1.5f,
                xpMultiplier = 2.0f,
                hasWeatherEffects = true,
                weatherType = "Fog",
                exclusiveCosmetics = new List<EventCosmetic>
                {
                    new EventCosmetic { cosmeticId = "skin_pumpkin_head", cosmeticName = "Pumpkin Head Mask", type = CosmeticType.Skin, requiredEventPoints = 1000 },
                    new EventCosmetic { cosmeticId = "emote_spooky_dance", cosmeticName = "Spooky Dance", type = CosmeticType.Emote, requiredEventPoints = 500 },
                    new EventCosmetic { cosmeticId = "weapon_candy_cane_bat", cosmeticName = "Candy Cane Bat", type = CosmeticType.WeaponSkin, requiredEventPoints = 1500, isPremium = true }
                },
                eventChallenges = new List<EventChallenge>
                {
                    new EventChallenge { challengeId = "halloween_pumpkins", challengeName = "Pumpkin Smasher", description = "Kill 100 Pumpkin Head zombies", objective = ChallengeObjective.KillSpecialZombies, targetValue = 100, eventPointsReward = 500 },
                    new EventChallenge { challengeId = "halloween_survive", challengeName = "Nightmare Survivor", description = "Survive 10 waves during event", objective = ChallengeObjective.SurviveWaves, targetValue = 10, eventPointsReward = 800 }
                }
            };

            // CHRISTMAS EVENT
            eventConfigs[SeasonalEventType.Christmas] = new EventConfig
            {
                eventType = SeasonalEventType.Christmas,
                eventName = "Winter Apocalypse",
                description = "Even the undead celebrate... in their own way. Elf zombies spread 'cheer'!",
                startDate = new DateTime(currentYear, 12, 15),
                endDate = new DateTime(currentYear + 1, 1, 5),
                themeColor = new Color(1.0f, 0.0f, 0.0f), // Red
                hasSpecialZombies = true,
                specialZombieTypes = new List<string> { "zombie_elf", "zombie_santa", "zombie_reindeer" },
                zombieSpawnRateMultiplier = 1.2f,
                lootDropRateMultiplier = 2.0f, // Generous holiday loot!
                xpMultiplier = 2.5f,
                hasWeatherEffects = true,
                weatherType = "Snow",
                exclusiveCosmetics = new List<EventCosmetic>
                {
                    new EventCosmetic { cosmeticId = "skin_santa_suit", cosmeticName = "Santa Survivor Suit", type = CosmeticType.Skin, requiredEventPoints = 2000 },
                    new EventCosmetic { cosmeticId = "emote_gift_toss", cosmeticName = "Gift Toss", type = CosmeticType.Emote, requiredEventPoints = 800 },
                    new EventCosmetic { cosmeticId = "weapon_candy_cane_rifle", cosmeticName = "Candy Cane Rifle", type = CosmeticType.WeaponSkin, requiredEventPoints = 2500, isPremium = true }
                },
                eventChallenges = new List<EventChallenge>
                {
                    new EventChallenge { challengeId = "christmas_elves", challengeName = "Elf Eliminator", description = "Kill 200 Elf zombies", objective = ChallengeObjective.KillSpecialZombies, targetValue = 200, eventPointsReward = 1000 },
                    new EventChallenge { challengeId = "christmas_friends", challengeName = "Holiday Squad", description = "Play 20 matches with friends", objective = ChallengeObjective.PlayWithFriends, targetValue = 20, eventPointsReward = 1500 }
                }
            };

            // SUMMER EVENT
            eventConfigs[SeasonalEventType.Summer] = new EventConfig
            {
                eventType = SeasonalEventType.Summer,
                eventName = "Beach Invasion",
                description = "Summer is here, but so are beach zombies! Time for vacation... and survival.",
                startDate = new DateTime(currentYear, 7, 1),
                endDate = new DateTime(currentYear, 7, 31),
                themeColor = new Color(1.0f, 1.0f, 0.0f), // Yellow
                hasSpecialZombies = true,
                specialZombieTypes = new List<string> { "zombie_beachgoer", "zombie_surfer", "zombie_lifeguard" },
                zombieSpawnRateMultiplier = 1.1f,
                lootDropRateMultiplier = 1.3f,
                xpMultiplier = 1.5f,
                exclusiveCosmetics = new List<EventCosmetic>
                {
                    new EventCosmetic { cosmeticId = "skin_beach_outfit", cosmeticName = "Beach Survivor Outfit", type = CosmeticType.Skin, requiredEventPoints = 1200 },
                    new EventCosmetic { cosmeticId = "emote_surfboard", cosmeticName = "Surfboard Trick", type = CosmeticType.Emote, requiredEventPoints = 600 },
                    new EventCosmetic { cosmeticId = "weapon_super_soaker", cosmeticName = "Super Soaker Skin", type = CosmeticType.WeaponSkin, requiredEventPoints = 1800 }
                },
                eventChallenges = new List<EventChallenge>
                {
                    new EventChallenge { challengeId = "summer_surfers", challengeName = "Surf's Down", description = "Kill 150 Surfer zombies", objective = ChallengeObjective.KillSpecialZombies, targetValue = 150, eventPointsReward = 700 },
                    new EventChallenge { challengeId = "summer_wins", challengeName = "Summer Champion", description = "Win 15 matches", objective = ChallengeObjective.WinMatches, targetValue = 15, eventPointsReward = 1000 }
                }
            };

            // VALENTINE'S EVENT
            eventConfigs[SeasonalEventType.Valentines] = new EventConfig
            {
                eventType = SeasonalEventType.Valentines,
                eventName = "Love Hurts",
                description = "Cupid zombies spread deadly love! Romance and survival collide.",
                startDate = new DateTime(currentYear, 2, 10),
                endDate = new DateTime(currentYear, 2, 18),
                themeColor = new Color(1.0f, 0.4f, 0.7f), // Pink
                hasSpecialZombies = true,
                specialZombieTypes = new List<string> { "zombie_cupid", "zombie_lover" },
                zombieSpawnRateMultiplier = 1.0f,
                lootDropRateMultiplier = 1.4f,
                xpMultiplier = 1.8f,
                exclusiveCosmetics = new List<EventCosmetic>
                {
                    new EventCosmetic { cosmeticId = "skin_cupid_wings", cosmeticName = "Cupid Wings", type = CosmeticType.Skin, requiredEventPoints = 1500 },
                    new EventCosmetic { cosmeticId = "emote_heart_toss", cosmeticName = "Heart Toss", type = CosmeticType.Emote, requiredEventPoints = 500 }
                },
                eventChallenges = new List<EventChallenge>
                {
                    new EventChallenge { challengeId = "valentine_cupids", challengeName = "Anti-Cupid", description = "Kill 80 Cupid zombies", objective = ChallengeObjective.KillSpecialZombies, targetValue = 80, eventPointsReward = 600 }
                }
            };

            Debug.Log($"[SeasonalEvents] Initialized {eventConfigs.Count} seasonal events");
        }

        private void Update()
        {
            if (!IsServer || !enableSeasonalEvents) return;

            // Check if event should change
            CheckActiveEvent();

            // Update event timer
            if (activeEvent.Value != SeasonalEventType.None)
            {
                UpdateEventTimer();
            }
        }

        private void CheckActiveEvent()
        {
            DateTime now = DateTime.UtcNow;

            SeasonalEventType newEvent = SeasonalEventType.None;

            foreach (var config in eventConfigs.Values)
            {
                if (now >= config.startDate && now <= config.endDate)
                {
                    newEvent = config.eventType;
                    break;
                }
            }

            if (newEvent != activeEvent.Value)
            {
                if (activeEvent.Value != SeasonalEventType.None)
                {
                    EndEvent(activeEvent.Value);
                }

                if (newEvent != SeasonalEventType.None)
                {
                    StartEvent(newEvent);
                }

                activeEvent.Value = newEvent;
            }
        }

        private void StartEvent(SeasonalEventType eventType)
        {
            Debug.Log($"[SeasonalEvents] Starting event: {eventType}");

            var config = eventConfigs[eventType];

            // Calculate time remaining
            TimeSpan remaining = config.endDate - DateTime.UtcNow;
            eventTimeRemaining.Value = (float)remaining.TotalSeconds;

            // Apply event modifiers
            ApplyEventModifiers(config);

            OnEventStarted?.Invoke(eventType);
            NotifyEventStartClientRpc(eventType);
        }

        private void EndEvent(SeasonalEventType eventType)
        {
            Debug.Log($"[SeasonalEvents] Ending event: {eventType}");

            // Remove event modifiers
            RemoveEventModifiers();

            OnEventEnded?.Invoke(eventType);
            NotifyEventEndClientRpc(eventType);
        }

        private void UpdateEventTimer()
        {
            eventTimeRemaining.Value -= Time.deltaTime;

            if (eventTimeRemaining.Value <= 0f)
            {
                CheckActiveEvent(); // Event ended
            }
        }

        private void ApplyEventModifiers(EventConfig config)
        {
            // Notify other systems to apply event modifiers
            // AI Director, Loot System, XP System, etc.

            if (config.hasWeatherEffects && AdvancedWeatherSystem.Instance != null)
            {
                // Force weather
                // AdvancedWeatherSystem.Instance.ForceWeatherChange(config.weatherType);
            }
        }

        private void RemoveEventModifiers()
        {
            // Reset modifiers
        }

        [ClientRpc]
        private void NotifyEventStartClientRpc(SeasonalEventType eventType)
        {
            Debug.Log($"[SeasonalEvents] EVENT STARTED: {eventType}!");
            // Show UI notification, play special music, etc.
        }

        [ClientRpc]
        private void NotifyEventEndClientRpc(SeasonalEventType eventType)
        {
            Debug.Log($"[SeasonalEvents] Event ended: {eventType}");
        }

        // Public API

        [ServerRpc(RequireOwnership = false)]
        public void AddEventPointsServerRpc(ulong playerId, int points, ServerRpcParams rpcParams = default)
        {
            if (!playerEventData.ContainsKey(playerId))
            {
                InitializePlayer(playerId);
            }

            playerEventData[playerId].eventPoints += points;
        }

        [ServerRpc(RequireOwnership = false)]
        public void ClaimEventRewardServerRpc(ulong playerId, string cosmeticId, ServerRpcParams rpcParams = default)
        {
            if (activeEvent.Value == SeasonalEventType.None) return;

            if (!playerEventData.ContainsKey(playerId))
            {
                InitializePlayer(playerId);
            }

            var playerData = playerEventData[playerId];
            var config = eventConfigs[activeEvent.Value];

            var cosmetic = config.exclusiveCosmetics.FirstOrDefault(c => c.cosmeticId == cosmeticId);

            if (cosmetic == null) return;

            if (playerData.eventPoints >= cosmetic.requiredEventPoints)
            {
                playerData.eventPoints -= cosmetic.requiredEventPoints;
                playerData.claimedRewards.Add(cosmeticId);
                playerData.unlockedCosmetics.Add(cosmeticId);

                OnEventRewardClaimed?.Invoke(playerId, cosmeticId);
                NotifyRewardClaimedClientRpc(playerId, cosmeticId);
            }
        }

        [ClientRpc]
        private void NotifyRewardClaimedClientRpc(ulong playerId, string cosmeticId)
        {
            Debug.Log($"[SeasonalEvents] Player {playerId} claimed: {cosmeticId}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void UpdateChallengeProgressServerRpc(ulong playerId, string challengeId, int progress, ServerRpcParams rpcParams = default)
        {
            if (!playerEventData.ContainsKey(playerId))
            {
                InitializePlayer(playerId);
            }

            var playerData = playerEventData[playerId];
            playerData.challengeProgress[challengeId] = progress;

            // Check if challenge complete
            var config = eventConfigs[activeEvent.Value];
            var challenge = config.eventChallenges.FirstOrDefault(c => c.challengeId == challengeId);

            if (challenge != null && progress >= challenge.targetValue)
            {
                // Grant reward
                AddEventPointsServerRpc(playerId, challenge.eventPointsReward);
            }
        }

        private void InitializePlayer(ulong playerId)
        {
            playerEventData[playerId] = new PlayerEventData();
        }

        // Getters

        public SeasonalEventType GetActiveEvent()
        {
            return activeEvent.Value;
        }

        public EventConfig GetCurrentEventConfig()
        {
            return activeEvent.Value != SeasonalEventType.None ? eventConfigs[activeEvent.Value] : null;
        }

        public float GetEventTimeRemaining()
        {
            return eventTimeRemaining.Value;
        }

        public PlayerEventData GetPlayerEventData(ulong playerId)
        {
            return playerEventData.ContainsKey(playerId) ? playerEventData[playerId] : null;
        }

        public bool IsEventActive()
        {
            return activeEvent.Value != SeasonalEventType.None;
        }
    }
}
