using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Lore
{
    /// <summary>
    /// Comprehensive lore discovery and codex system for multi-platform zombie multiplayer game.
    /// Handles collectible lore items, story progression, and encyclopedic knowledge tracking.
    /// </summary>
    public class LoreSystem : NetworkBehaviour
    {
        public static LoreSystem Instance { get; private set; }

        [Header("Lore Settings")]
        [SerializeField] private bool enableLoreSystem = true;
        [SerializeField] private int maxLoreEntries = 500;
        [SerializeField] private bool enableLoreRewards = true;
        [SerializeField] private int rewardPerEntry = 100;

        [Header("Discovery Settings")]
        [SerializeField] private bool enableAutoDiscovery = true;
        [SerializeField] private float discoveryRadius = 5f;
        [SerializeField] private bool showDiscoveryNotifications = true;

        [Header("Codex Settings")]
        [SerializeField] private bool enableBestiaryTracking = true;
        [SerializeField] private bool enableLocationTracking = true;
        [SerializeField] private int entriesForCompletionBonus = 10;

        // Enums
        public enum LoreCategory
        {
            WorldHistory,
            Characters,
            Factions,
            Locations,
            Items,
            Creatures,
            Events,
            Mythology,
            Science,
            Mystery
        }

        public enum LoreRarity
        {
            Common,
            Uncommon,
            Rare,
            Epic,
            Legendary
        }

        public enum DiscoveryMethod
        {
            Found,
            Killed,
            Visited,
            Completed,
            Purchased,
            Gifted,
            Achievement
        }

        // Data structures
        [Serializable]
        public class LoreEntry
        {
            public string entryId;
            public string title;
            public string description;
            public string fullText;
            public LoreCategory category;
            public LoreRarity rarity;
            public int sortOrder;
            public List<string> relatedEntries = new List<string>();
            public string imageUrl;
            public string audioClipId;
            public bool isHidden;
            public Dictionary<string, object> metadata = new Dictionary<string, object>();
        }

        [Serializable]
        public class PlayerLoreData
        {
            public ulong playerId;
            public HashSet<string> discoveredEntries = new HashSet<string>();
            public Dictionary<string, LoreDiscovery> discoveries = new Dictionary<string, LoreDiscovery>();
            public DateTime firstDiscoveryDate;
            public DateTime lastDiscoveryDate;
            public int totalDiscoveries;
            public Dictionary<LoreCategory, int> categoryProgress = new Dictionary<LoreCategory, int>();
        }

        [Serializable]
        public class LoreDiscovery
        {
            public string entryId;
            public DateTime discoveryDate;
            public DiscoveryMethod method;
            public Vector3 discoveryLocation;
            public string discoveryContext;
        }

        [Serializable]
        public class BestiaryEntry
        {
            public string creatureId;
            public string creatureName;
            public string description;
            public int timesEncountered;
            public int timesKilled;
            public int deaths;
            public float highestDamageDealt;
            public List<string> weaknesses = new List<string>();
            public List<string> resistances = new List<string>();
            public List<string> dropTable = new List<string>();
            public bool fullyDocumented;
        }

        [Serializable]
        public class LocationEntry
        {
            public string locationId;
            public string locationName;
            public string description;
            public DateTime firstVisited;
            public int timesVisited;
            public List<string> notableFeatures = new List<string>();
            public List<string> inhabitedBy = new List<string>();
            public bool fullyExplored;
        }

        [Serializable]
        public class CodexCollection
        {
            public string collectionId;
            public string collectionName;
            public List<string> requiredEntries = new List<string>();
            public int rewardAmount;
            public string rewardItemId;
            public bool completed;
        }

        // State
        private Dictionary<string, LoreEntry> loreEntries = new Dictionary<string, LoreEntry>();
        private Dictionary<ulong, PlayerLoreData> playerLoreData = new Dictionary<ulong, PlayerLoreData>();
        private Dictionary<string, BestiaryEntry> bestiary = new Dictionary<string, BestiaryEntry>();
        private Dictionary<string, LocationEntry> locations = new Dictionary<string, LocationEntry>();
        private Dictionary<string, CodexCollection> collections = new Dictionary<string, CodexCollection>();

        // Events
        public event Action<ulong, LoreEntry> OnLoreDiscovered;
        public event Action<ulong, LoreCategory> OnCategoryCompleted;
        public event Action<ulong, CodexCollection> OnCollectionCompleted;
        public event Action<string, BestiaryEntry> OnBestiaryUpdated;

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
                InitializeLoreSystem();
            }
        }

        #region Initialization

        private void InitializeLoreSystem()
        {
            InitializeLoreEntries();
            InitializeCollections();
            InitializeBestiary();
        }

        private void InitializeLoreEntries()
        {
            // World History
            RegisterLoreEntry(new LoreEntry
            {
                entryId = "lore_outbreak_001",
                title = "The First Outbreak",
                description = "The initial zombie outbreak that changed everything",
                fullText = "On the day known as Day Zero, the first cases of the infection were reported in major cities across the globe. What started as isolated incidents quickly spiraled into a worldwide pandemic...",
                category = LoreCategory.WorldHistory,
                rarity = LoreRarity.Common,
                sortOrder = 1
            });

            RegisterLoreEntry(new LoreEntry
            {
                entryId = "lore_safe_zones_001",
                title = "Safe Zone Establishment",
                description = "How humanity's last bastions were created",
                fullText = "In the weeks following the outbreak, military forces and civilian survivors banded together to establish fortified safe zones. These became the last strongholds of civilization...",
                category = LoreCategory.WorldHistory,
                rarity = LoreRarity.Uncommon,
                sortOrder = 2
            });

            // Creatures
            RegisterLoreEntry(new LoreEntry
            {
                entryId = "lore_zombie_types_001",
                title = "Classification of the Infected",
                description = "Scientific documentation of zombie variants",
                fullText = "Research has identified several distinct types of infected individuals, each with unique characteristics and threat levels...",
                category = LoreCategory.Creatures,
                rarity = LoreRarity.Common,
                sortOrder = 1
            });

            // Mysterious entries
            RegisterLoreEntry(new LoreEntry
            {
                entryId = "lore_patient_zero_001",
                title = "Patient Zero",
                description = "The mystery of the infection's origin",
                fullText = "Despite extensive investigation, the true origin of the infection remains unknown. Some documents reference a 'Patient Zero', but their identity has been redacted...",
                category = LoreCategory.Mystery,
                rarity = LoreRarity.Legendary,
                sortOrder = 1,
                isHidden = true
            });
        }

        private void RegisterLoreEntry(LoreEntry entry)
        {
            if (loreEntries.Count >= maxLoreEntries)
            {
                Debug.LogWarning("Maximum lore entries reached");
                return;
            }

            loreEntries[entry.entryId] = entry;
        }

        private void InitializeCollections()
        {
            collections["collection_outbreak"] = new CodexCollection
            {
                collectionId = "collection_outbreak",
                collectionName = "Outbreak Chronicles",
                requiredEntries = new List<string> { "lore_outbreak_001", "lore_safe_zones_001" },
                rewardAmount = 1000
            };

            collections["collection_bestiary"] = new CodexCollection
            {
                collectionId = "collection_bestiary",
                collectionName = "Complete Bestiary",
                rewardAmount = 5000
            };
        }

        private void InitializeBestiary()
        {
            // Initialize common zombie types
            bestiary["zombie_basic"] = new BestiaryEntry
            {
                creatureId = "zombie_basic",
                creatureName = "Common Infected",
                description = "The most common type of infected human",
                weaknesses = new List<string> { "Headshots", "Fire" },
                resistances = new List<string> { "Pain" }
            };

            bestiary["zombie_runner"] = new BestiaryEntry
            {
                creatureId = "zombie_runner",
                creatureName = "Runner",
                description = "Fast-moving infected with heightened aggression",
                weaknesses = new List<string> { "Headshots", "Explosives" },
                resistances = new List<string> { "Pain", "Fatigue" }
            };
        }

        #endregion

        #region Lore Discovery

        [ServerRpc(RequireOwnership = false)]
        public void DiscoverLoreServerRpc(ulong playerId, string entryId, DiscoveryMethod method, Vector3 location, string context = "")
        {
            if (!loreEntries.ContainsKey(entryId))
            {
                Debug.LogWarning($"Lore entry {entryId} not found");
                return;
            }

            if (!playerLoreData.ContainsKey(playerId))
            {
                playerLoreData[playerId] = new PlayerLoreData
                {
                    playerId = playerId,
                    firstDiscoveryDate = DateTime.UtcNow
                };
            }

            var data = playerLoreData[playerId];

            // Check if already discovered
            if (data.discoveredEntries.Contains(entryId))
            {
                Debug.Log($"Player {playerId} already discovered lore {entryId}");
                return;
            }

            var entry = loreEntries[entryId];

            // Record discovery
            var discovery = new LoreDiscovery
            {
                entryId = entryId,
                discoveryDate = DateTime.UtcNow,
                method = method,
                discoveryLocation = location,
                discoveryContext = context
            };

            data.discoveredEntries.Add(entryId);
            data.discoveries[entryId] = discovery;
            data.totalDiscoveries++;
            data.lastDiscoveryDate = DateTime.UtcNow;

            // Update category progress
            if (!data.categoryProgress.ContainsKey(entry.category))
            {
                data.categoryProgress[entry.category] = 0;
            }
            data.categoryProgress[entry.category]++;

            // Grant rewards
            if (enableLoreRewards)
            {
                int reward = rewardPerEntry * (int)(entry.rarity + 1);
                Economy.EconomyManager.Instance?.AddSoftCurrency(playerId, reward);
            }

            OnLoreDiscovered?.Invoke(playerId, entry);
            NotifyLoreDiscoveredClientRpc(playerId, entryId);

            // Check category completion
            CheckCategoryCompletion(playerId, entry.category);

            // Check collections
            CheckCollectionCompletion(playerId);

            Debug.Log($"Player {playerId} discovered lore: {entry.title}");
        }

        [ClientRpc]
        private void NotifyLoreDiscoveredClientRpc(ulong playerId, string entryId)
        {
            if (NetworkManager.Singleton.LocalClientId != playerId) return;

            if (loreEntries.ContainsKey(entryId))
            {
                OnLoreDiscovered?.Invoke(playerId, loreEntries[entryId]);
            }
        }

        private void CheckCategoryCompletion(ulong playerId, LoreCategory category)
        {
            int totalInCategory = loreEntries.Values.Count(e => e.category == category && !e.isHidden);
            int discovered = playerLoreData[playerId].categoryProgress[category];

            if (discovered >= totalInCategory)
            {
                OnCategoryCompleted?.Invoke(playerId, category);

                // Grant completion bonus
                if (enableLoreRewards)
                {
                    Economy.EconomyManager.Instance?.AddSoftCurrency(playerId, 5000);
                }

                Debug.Log($"Player {playerId} completed category: {category}");
            }
        }

        #endregion

        #region Bestiary

        [ServerRpc(RequireOwnership = false)]
        public void RecordCreatureEncounterServerRpc(ulong playerId, string creatureId)
        {
            if (!bestiary.ContainsKey(creatureId)) return;

            var entry = bestiary[creatureId];
            entry.timesEncountered++;

            OnBestiaryUpdated?.Invoke(creatureId, entry);
        }

        [ServerRpc(RequireOwnership = false)]
        public void RecordCreatureKillServerRpc(ulong playerId, string creatureId, float damageDealt)
        {
            if (!bestiary.ContainsKey(creatureId)) return;

            var entry = bestiary[creatureId];
            entry.timesKilled++;

            if (damageDealt > entry.highestDamageDealt)
            {
                entry.highestDamageDealt = damageDealt;
            }

            // Check if fully documented
            if (entry.timesKilled >= 10 && entry.timesEncountered >= 20)
            {
                entry.fullyDocumented = true;

                // Auto-discover related lore
                string loreId = $"lore_creature_{creatureId}";
                if (loreEntries.ContainsKey(loreId))
                {
                    DiscoverLoreServerRpc(playerId, loreId, DiscoveryMethod.Killed, Vector3.zero, $"Documented {entry.creatureName}");
                }
            }

            OnBestiaryUpdated?.Invoke(creatureId, entry);
        }

        [ServerRpc]
        public void RecordPlayerDeathServerRpc(ulong playerId, string creatureId)
        {
            if (!bestiary.ContainsKey(creatureId)) return;

            var entry = bestiary[creatureId];
            entry.deaths++;

            OnBestiaryUpdated?.Invoke(creatureId, entry);
        }

        public BestiaryEntry GetBestiaryEntry(string creatureId)
        {
            return bestiary.ContainsKey(creatureId) ? bestiary[creatureId] : null;
        }

        public List<BestiaryEntry> GetAllBestiaryEntries()
        {
            return bestiary.Values.ToList();
        }

        #endregion

        #region Locations

        [ServerRpc(RequireOwnership = false)]
        public void RecordLocationVisitServerRpc(ulong playerId, string locationId)
        {
            if (!locations.ContainsKey(locationId))
            {
                Debug.LogWarning($"Location {locationId} not registered");
                return;
            }

            var location = locations[locationId];

            if (location.firstVisited == default)
            {
                location.firstVisited = DateTime.UtcNow;

                // Auto-discover location lore
                string loreId = $"lore_location_{locationId}";
                if (loreEntries.ContainsKey(loreId))
                {
                    DiscoverLoreServerRpc(playerId, loreId, DiscoveryMethod.Visited, Vector3.zero, $"Discovered {location.locationName}");
                }
            }

            location.timesVisited++;
        }

        public void RegisterLocation(LocationEntry location)
        {
            locations[location.locationId] = location;
        }

        #endregion

        #region Collections

        private void CheckCollectionCompletion(ulong playerId)
        {
            if (!playerLoreData.ContainsKey(playerId)) return;

            var data = playerLoreData[playerId];

            foreach (var collection in collections.Values)
            {
                if (collection.completed) continue;

                bool allDiscovered = collection.requiredEntries.All(e => data.discoveredEntries.Contains(e));

                if (allDiscovered)
                {
                    collection.completed = true;

                    // Grant rewards
                    if (collection.rewardAmount > 0)
                    {
                        Economy.EconomyManager.Instance?.AddSoftCurrency(playerId, collection.rewardAmount);
                    }

                    if (!string.IsNullOrEmpty(collection.rewardItemId))
                    {
                        Inventory.InventoryManager.Instance?.AddItem(playerId, collection.rewardItemId, 1);
                    }

                    OnCollectionCompleted?.Invoke(playerId, collection);

                    Debug.Log($"Player {playerId} completed collection: {collection.collectionName}");
                }
            }
        }

        #endregion

        #region Public API

        public PlayerLoreData GetPlayerLoreData(ulong playerId)
        {
            if (!playerLoreData.ContainsKey(playerId))
            {
                playerLoreData[playerId] = new PlayerLoreData
                {
                    playerId = playerId,
                    firstDiscoveryDate = DateTime.UtcNow
                };
            }

            return playerLoreData[playerId];
        }

        public LoreEntry GetLoreEntry(string entryId)
        {
            return loreEntries.ContainsKey(entryId) ? loreEntries[entryId] : null;
        }

        public List<LoreEntry> GetLoreByCategory(LoreCategory category)
        {
            return loreEntries.Values
                .Where(e => e.category == category && !e.isHidden)
                .OrderBy(e => e.sortOrder)
                .ToList();
        }

        public List<LoreEntry> GetDiscoveredLore(ulong playerId)
        {
            if (!playerLoreData.ContainsKey(playerId)) return new List<LoreEntry>();

            var data = playerLoreData[playerId];

            return loreEntries.Values
                .Where(e => data.discoveredEntries.Contains(e.entryId))
                .OrderBy(e => e.category)
                .ThenBy(e => e.sortOrder)
                .ToList();
        }

        public float GetCompletionPercentage(ulong playerId)
        {
            if (!playerLoreData.ContainsKey(playerId)) return 0f;

            int total = loreEntries.Count(e => !e.Value.isHidden);
            int discovered = playerLoreData[playerId].discoveredEntries.Count;

            return total > 0 ? (discovered / (float)total) * 100f : 0f;
        }

        public Dictionary<LoreCategory, float> GetCategoryProgress(ulong playerId)
        {
            var progress = new Dictionary<LoreCategory, float>();

            foreach (LoreCategory category in Enum.GetValues(typeof(LoreCategory)))
            {
                int total = loreEntries.Values.Count(e => e.category == category && !e.isHidden);
                int discovered = 0;

                if (playerLoreData.ContainsKey(playerId))
                {
                    discovered = playerLoreData[playerId].categoryProgress.ContainsKey(category)
                        ? playerLoreData[playerId].categoryProgress[category]
                        : 0;
                }

                progress[category] = total > 0 ? (discovered / (float)total) * 100f : 0f;
            }

            return progress;
        }

        #endregion
    }
}
