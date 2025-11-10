using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Housing
{
    /// <summary>
    /// Comprehensive player housing and hideout customization system for multi-platform zombie multiplayer game.
    /// Handles personal spaces, furniture, decorations, utilities, and storage upgrades.
    /// </summary>
    public class PlayerHousingSystem : NetworkBehaviour
    {
        public static PlayerHousingSystem Instance { get; private set; }

        [Header("Housing Settings")]
        [SerializeField] private bool enableHousing = true;
        [SerializeField] private int maxRoomUpgrades = 10;
        [SerializeField] private int maxFurnitureItems = 200;
        [SerializeField] private int maxStorageSlots = 500;

        [Header("Customization Settings")]
        [SerializeField] private bool enableDecorations = true;
        [SerializeField] private bool enableThemes = true;
        [SerializeField] private int maxThemesOwned = 20;

        [Header("Utility Settings")]
        [SerializeField] private bool enableCrafting = true;
        [SerializeField] private bool enableStorage = true;
        [SerializeField] private bool enableTraining = true;
        [SerializeField] private bool enableWorkshop = true;

        // Enums
        public enum RoomType
        {
            MainHall,
            Armory,
            Workshop,
            Storage,
            Quarters,
            GardenArea,
            TrainingRoom,
            MedicalBay,
            Laboratory,
            TrophyRoom
        }

        public enum FurnitureCategory
        {
            Seating,
            Tables,
            Storage,
            Decoration,
            Lighting,
            Utility,
            Wall,
            Floor,
            Ceiling,
            Special
        }

        public enum FurnitureRarity
        {
            Common,
            Uncommon,
            Rare,
            Epic,
            Legendary
        }

        // Data structures
        [Serializable]
        public class PlayerHideout
        {
            public ulong ownerId;
            public string hideoutName;
            public int hideoutLevel;
            public Dictionary<RoomType, Room> rooms = new Dictionary<RoomType, Room>();
            public List<PlacedFurniture> placedItems = new List<PlacedFurniture>();
            public HideoutTheme currentTheme;
            public List<string> unlockedThemes = new List<string>();
            public int totalStorageSlots;
            public int usedStorageSlots;
            public Dictionary<string, int> storedItems = new Dictionary<string, int>();
            public DateTime creationDate;
            public int visitorCount;
            public float prestigeScore;
        }

        [Serializable]
        public class Room
        {
            public RoomType type;
            public string roomName;
            public int level;
            public int maxLevel;
            public Vector3 dimensions;
            public bool isUnlocked;
            public Dictionary<string, float> utilities = new Dictionary<string, float>();
            public int furnitureSlots;
            public int usedSlots;
        }

        [Serializable]
        public class PlacedFurniture
        {
            public string placementId;
            public string furnitureId;
            public FurnitureItem furnitureData;
            public RoomType roomType;
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 scale;
            public DateTime placedDate;
        }

        [Serializable]
        public class FurnitureItem
        {
            public string itemId;
            public string itemName;
            public string description;
            public FurnitureCategory category;
            public FurnitureRarity rarity;
            public string prefabPath;
            public Vector3 defaultSize;
            public int prestigeValue;
            public Dictionary<string, float> bonusStats = new Dictionary<string, float>();
            public bool isInteractable;
            public string interactionType;
        }

        [Serializable]
        public class HideoutTheme
        {
            public string themeId;
            public string themeName;
            public string description;
            public Color primaryColor;
            public Color secondaryColor;
            public string wallTexture;
            public string floorTexture;
            public string lightingPreset;
            public int unlockCost;
        }

        [Serializable]
        public class RoomUpgrade
        {
            public RoomType roomType;
            public int currentLevel;
            public int nextLevel;
            public int currencyCost;
            public Dictionary<string, int> materialCosts = new Dictionary<string, int>();
            public float constructionTimeHours;
            public Dictionary<string, float> bonuses = new Dictionary<string, float>();
        }

        [Serializable]
        public class HideoutVisit
        {
            public ulong visitorId;
            public string visitorName;
            public DateTime visitTime;
            public float stayDuration;
            public int prestigeAwarded;
        }

        // State
        private Dictionary<ulong, PlayerHideout> playerHideouts = new Dictionary<ulong, PlayerHideout>();
        private Dictionary<string, FurnitureItem> furnitureDatabase = new Dictionary<string, FurnitureItem>();
        private Dictionary<string, HideoutTheme> themeDatabase = new Dictionary<string, HideoutTheme>();
        private Dictionary<ulong, List<HideoutVisit>> visitHistory = new Dictionary<ulong, List<HideoutVisit>>();

        // Events
        public event Action<ulong, PlayerHideout> OnHideoutCreated;
        public event Action<ulong, RoomType, int> OnRoomUpgraded;
        public event Action<ulong, PlacedFurniture> OnFurniturePlaced;
        public event Action<ulong, string> OnFurnitureRemoved;
        public event Action<ulong, HideoutTheme> OnThemeChanged;
        public event Action<ulong, ulong> OnHideoutVisited;

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
                InitializeHousingSystem();
            }
        }

        #region Initialization

        private void InitializeHousingSystem()
        {
            InitializeFurnitureDatabase();
            InitializeThemes();
        }

        private void InitializeFurnitureDatabase()
        {
            // Seating
            RegisterFurniture(new FurnitureItem
            {
                itemId = "furniture_chair_basic",
                itemName = "Basic Chair",
                description = "A simple wooden chair",
                category = FurnitureCategory.Seating,
                rarity = FurnitureRarity.Common,
                prestigeValue = 10,
                defaultSize = new Vector3(0.5f, 1f, 0.5f)
            });

            // Storage
            RegisterFurniture(new FurnitureItem
            {
                itemId = "furniture_chest_large",
                itemName = "Large Storage Chest",
                description = "Provides additional storage space",
                category = FurnitureCategory.Storage,
                rarity = FurnitureRarity.Rare,
                prestigeValue = 50,
                bonusStats = new Dictionary<string, float> { { "StorageSlots", 50 } },
                isInteractable = true,
                interactionType = "OpenStorage"
            });

            // Utility
            RegisterFurniture(new FurnitureItem
            {
                itemId = "furniture_workbench",
                itemName = "Crafting Workbench",
                description = "Enables advanced crafting",
                category = FurnitureCategory.Utility,
                rarity = FurnitureRarity.Epic,
                prestigeValue = 100,
                bonusStats = new Dictionary<string, float> { { "CraftingSpeed", 1.2f } },
                isInteractable = true,
                interactionType = "OpenCrafting"
            });

            // Decorations
            RegisterFurniture(new FurnitureItem
            {
                itemId = "furniture_trophy_gold",
                itemName = "Golden Trophy",
                description = "A prestigious display piece",
                category = FurnitureCategory.Decoration,
                rarity = FurnitureRarity.Legendary,
                prestigeValue = 500,
                bonusStats = new Dictionary<string, float> { { "Prestige", 100 } }
            });
        }

        private void RegisterFurniture(FurnitureItem item)
        {
            furnitureDatabase[item.itemId] = item;
        }

        private void InitializeThemes()
        {
            // Default theme
            RegisterTheme(new HideoutTheme
            {
                themeId = "theme_default",
                themeName = "Survivor's Refuge",
                description = "The standard hideout theme",
                primaryColor = new Color(0.4f, 0.3f, 0.2f),
                secondaryColor = new Color(0.6f, 0.5f, 0.4f),
                unlockCost = 0
            });

            // Military theme
            RegisterTheme(new HideoutTheme
            {
                themeId = "theme_military",
                themeName = "Military Outpost",
                description = "A fortified military aesthetic",
                primaryColor = new Color(0.3f, 0.4f, 0.3f),
                secondaryColor = new Color(0.2f, 0.2f, 0.2f),
                unlockCost = 5000
            });

            // Luxury theme
            RegisterTheme(new HideoutTheme
            {
                themeId = "theme_luxury",
                themeName = "Pre-Apocalypse Luxury",
                description = "A taste of the old world",
                primaryColor = new Color(0.7f, 0.6f, 0.4f),
                secondaryColor = new Color(0.9f, 0.8f, 0.6f),
                unlockCost = 15000
            });
        }

        private void RegisterTheme(HideoutTheme theme)
        {
            themeDatabase[theme.themeId] = theme;
        }

        #endregion

        #region Hideout Creation

        [ServerRpc(RequireOwnership = false)]
        public void CreateHideoutServerRpc(ulong playerId, string hideoutName)
        {
            if (playerHideouts.ContainsKey(playerId))
            {
                Debug.LogWarning($"Player {playerId} already has a hideout");
                return;
            }

            var hideout = new PlayerHideout
            {
                ownerId = playerId,
                hideoutName = hideoutName,
                hideoutLevel = 1,
                currentTheme = themeDatabase["theme_default"],
                creationDate = DateTime.UtcNow,
                totalStorageSlots = 50,
                usedStorageSlots = 0
            };

            // Initialize starting rooms
            InitializeStartingRooms(hideout);

            playerHideouts[playerId] = hideout;

            OnHideoutCreated?.Invoke(playerId, hideout);

            Debug.Log($"Created hideout for player {playerId}: {hideoutName}");
        }

        private void InitializeStartingRooms(PlayerHideout hideout)
        {
            // Main Hall - always unlocked
            hideout.rooms[RoomType.MainHall] = new Room
            {
                type = RoomType.MainHall,
                roomName = "Main Hall",
                level = 1,
                maxLevel = maxRoomUpgrades,
                dimensions = new Vector3(10, 3, 10),
                isUnlocked = true,
                furnitureSlots = 20,
                usedSlots = 0
            };

            // Storage - always unlocked
            hideout.rooms[RoomType.Storage] = new Room
            {
                type = RoomType.Storage,
                roomName = "Storage Room",
                level = 1,
                maxLevel = maxRoomUpgrades,
                dimensions = new Vector3(8, 3, 8),
                isUnlocked = true,
                furnitureSlots = 15,
                usedSlots = 0,
                utilities = new Dictionary<string, float> { { "StorageCapacity", 50 } }
            };

            // Other rooms start locked
            hideout.rooms[RoomType.Workshop] = new Room
            {
                type = RoomType.Workshop,
                roomName = "Workshop",
                level = 0,
                maxLevel = maxRoomUpgrades,
                isUnlocked = false
            };

            hideout.rooms[RoomType.Armory] = new Room
            {
                type = RoomType.Armory,
                roomName = "Armory",
                level = 0,
                maxLevel = maxRoomUpgrades,
                isUnlocked = false
            };
        }

        #endregion

        #region Room Management

        [ServerRpc(RequireOwnership = false)]
        public void UpgradeRoomServerRpc(ulong playerId, RoomType roomType)
        {
            if (!playerHideouts.ContainsKey(playerId))
            {
                Debug.LogWarning($"Player {playerId} doesn't have a hideout");
                return;
            }

            var hideout = playerHideouts[playerId];

            if (!hideout.rooms.ContainsKey(roomType))
            {
                Debug.LogWarning($"Room type {roomType} not found");
                return;
            }

            var room = hideout.rooms[roomType];

            // Check if unlocked
            if (!room.isUnlocked)
            {
                // Unlock room
                int unlockCost = CalculateRoomUnlockCost(roomType);
                if (!Economy.EconomyManager.Instance.SpendSoftCurrency(playerId, unlockCost))
                {
                    Debug.LogWarning($"Player {playerId} can't afford to unlock room");
                    return;
                }

                room.isUnlocked = true;
                room.level = 1;
                InitializeRoom(room, roomType);
            }
            else
            {
                // Upgrade existing room
                if (room.level >= room.maxLevel)
                {
                    Debug.LogWarning($"Room {roomType} is already max level");
                    return;
                }

                int upgradeCost = CalculateRoomUpgradeCost(room.level);
                if (!Economy.EconomyManager.Instance.SpendSoftCurrency(playerId, upgradeCost))
                {
                    Debug.LogWarning($"Player {playerId} can't afford room upgrade");
                    return;
                }

                room.level++;
                ApplyRoomUpgrade(room);
            }

            OnRoomUpgraded?.Invoke(playerId, roomType, room.level);

            Debug.Log($"Player {playerId} upgraded room {roomType} to level {room.level}");
        }

        private void InitializeRoom(Room room, RoomType type)
        {
            switch (type)
            {
                case RoomType.Workshop:
                    room.dimensions = new Vector3(8, 3, 8);
                    room.furnitureSlots = 15;
                    room.utilities["CraftingSpeed"] = 1.1f;
                    break;

                case RoomType.Armory:
                    room.dimensions = new Vector3(10, 3, 8);
                    room.furnitureSlots = 20;
                    room.utilities["WeaponDisplay"] = 10;
                    break;

                case RoomType.TrainingRoom:
                    room.dimensions = new Vector3(12, 4, 12);
                    room.furnitureSlots = 10;
                    room.utilities["TrainingEfficiency"] = 1.15f;
                    break;

                case RoomType.MedicalBay:
                    room.dimensions = new Vector3(8, 3, 6);
                    room.furnitureSlots = 12;
                    room.utilities["HealingRate"] = 1.2f;
                    break;
            }
        }

        private void ApplyRoomUpgrade(Room room)
        {
            // Increase furniture slots
            room.furnitureSlots += 5;

            // Increase room bonuses
            foreach (var utility in room.utilities.Keys.ToList())
            {
                room.utilities[utility] *= 1.05f; // 5% increase per level
            }
        }

        private int CalculateRoomUnlockCost(RoomType type)
        {
            return type switch
            {
                RoomType.Workshop => 5000,
                RoomType.Armory => 7500,
                RoomType.TrainingRoom => 10000,
                RoomType.MedicalBay => 12500,
                RoomType.Laboratory => 15000,
                RoomType.TrophyRoom => 20000,
                _ => 5000
            };
        }

        private int CalculateRoomUpgradeCost(int currentLevel)
        {
            return 1000 * currentLevel * currentLevel;
        }

        #endregion

        #region Furniture Placement

        [ServerRpc(RequireOwnership = false)]
        public void PlaceFurnitureServerRpc(ulong playerId, string furnitureId, RoomType roomType, Vector3 position, Quaternion rotation)
        {
            if (!playerHideouts.ContainsKey(playerId))
            {
                Debug.LogWarning($"Player {playerId} doesn't have a hideout");
                return;
            }

            if (!furnitureDatabase.ContainsKey(furnitureId))
            {
                Debug.LogWarning($"Furniture {furnitureId} not found");
                return;
            }

            var hideout = playerHideouts[playerId];
            var room = hideout.rooms[roomType];

            // Check room is unlocked
            if (!room.isUnlocked)
            {
                Debug.LogWarning($"Room {roomType} is not unlocked");
                return;
            }

            // Check furniture slots
            if (room.usedSlots >= room.furnitureSlots)
            {
                Debug.LogWarning($"Room {roomType} has no available furniture slots");
                return;
            }

            // Check player owns furniture
            if (!Inventory.InventoryManager.Instance.RemoveItem(playerId, furnitureId, 1))
            {
                Debug.LogWarning($"Player {playerId} doesn't own furniture {furnitureId}");
                return;
            }

            var furnitureData = furnitureDatabase[furnitureId];

            var placement = new PlacedFurniture
            {
                placementId = $"place_{Guid.NewGuid()}",
                furnitureId = furnitureId,
                furnitureData = furnitureData,
                roomType = roomType,
                position = position,
                rotation = rotation,
                scale = furnitureData.defaultSize,
                placedDate = DateTime.UtcNow
            };

            hideout.placedItems.Add(placement);
            room.usedSlots++;

            // Apply bonuses
            ApplyFurnitureBonuses(hideout, furnitureData);

            OnFurniturePlaced?.Invoke(playerId, placement);

            Debug.Log($"Player {playerId} placed furniture {furnitureId} in {roomType}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void RemoveFurnitureServerRpc(ulong playerId, string placementId)
        {
            if (!playerHideouts.ContainsKey(playerId))
                return;

            var hideout = playerHideouts[playerId];
            var placement = hideout.placedItems.FirstOrDefault(p => p.placementId == placementId);

            if (placement == null)
                return;

            // Remove furniture
            hideout.placedItems.Remove(placement);

            // Return to inventory
            Inventory.InventoryManager.Instance?.AddItem(playerId, placement.furnitureId, 1);

            // Update room slots
            if (hideout.rooms.ContainsKey(placement.roomType))
            {
                hideout.rooms[placement.roomType].usedSlots--;
            }

            // Remove bonuses
            RemoveFurnitureBonuses(hideout, placement.furnitureData);

            OnFurnitureRemoved?.Invoke(playerId, placementId);

            Debug.Log($"Player {playerId} removed furniture {placement.furnitureId}");
        }

        private void ApplyFurnitureBonuses(PlayerHideout hideout, FurnitureItem furniture)
        {
            hideout.prestigeScore += furniture.prestigeValue;

            foreach (var bonus in furniture.bonusStats)
            {
                if (bonus.Key == "StorageSlots")
                {
                    hideout.totalStorageSlots += (int)bonus.Value;
                }
            }
        }

        private void RemoveFurnitureBonuses(PlayerHideout hideout, FurnitureItem furniture)
        {
            hideout.prestigeScore -= furniture.prestigeValue;

            foreach (var bonus in furniture.bonusStats)
            {
                if (bonus.Key == "StorageSlots")
                {
                    hideout.totalStorageSlots -= (int)bonus.Value;
                }
            }
        }

        #endregion

        #region Theme Management

        [ServerRpc(RequireOwnership = false)]
        public void ChangeThemeServerRpc(ulong playerId, string themeId)
        {
            if (!playerHideouts.ContainsKey(playerId))
                return;

            if (!themeDatabase.ContainsKey(themeId))
            {
                Debug.LogWarning($"Theme {themeId} not found");
                return;
            }

            var hideout = playerHideouts[playerId];
            var theme = themeDatabase[themeId];

            // Check if theme is unlocked
            if (!hideout.unlockedThemes.Contains(themeId) && theme.unlockCost > 0)
            {
                // Purchase theme
                if (!Economy.EconomyManager.Instance.SpendSoftCurrency(playerId, theme.unlockCost))
                {
                    Debug.LogWarning($"Player {playerId} can't afford theme {themeId}");
                    return;
                }

                hideout.unlockedThemes.Add(themeId);
            }

            hideout.currentTheme = theme;

            OnThemeChanged?.Invoke(playerId, theme);

            Debug.Log($"Player {playerId} changed theme to {theme.themeName}");
        }

        #endregion

        #region Visiting

        [ServerRpc(RequireOwnership = false)]
        public void VisitHideoutServerRpc(ulong visitorId, ulong ownerId)
        {
            if (!playerHideouts.ContainsKey(ownerId))
            {
                Debug.LogWarning($"Owner {ownerId} doesn't have a hideout");
                return;
            }

            var hideout = playerHideouts[ownerId];
            hideout.visitorCount++;

            var visit = new HideoutVisit
            {
                visitorId = visitorId,
                visitorName = GetPlayerName(visitorId),
                visitTime = DateTime.UtcNow,
                prestigeAwarded = Mathf.RoundToInt(hideout.prestigeScore * 0.01f)
            };

            if (!visitHistory.ContainsKey(ownerId))
            {
                visitHistory[ownerId] = new List<HideoutVisit>();
            }

            visitHistory[ownerId].Add(visit);

            OnHideoutVisited?.Invoke(visitorId, ownerId);

            Debug.Log($"Player {visitorId} visited hideout of player {ownerId}");
        }

        #endregion

        #region Public API

        public PlayerHideout GetHideout(ulong playerId)
        {
            return playerHideouts.ContainsKey(playerId) ? playerHideouts[playerId] : null;
        }

        public List<PlacedFurniture> GetRoomFurniture(ulong playerId, RoomType roomType)
        {
            if (!playerHideouts.ContainsKey(playerId))
                return new List<PlacedFurniture>();

            return playerHideouts[playerId].placedItems
                .Where(p => p.roomType == roomType)
                .ToList();
        }

        public List<FurnitureItem> GetAvailableFurniture()
        {
            return furnitureDatabase.Values.ToList();
        }

        public List<HideoutTheme> GetAvailableThemes()
        {
            return themeDatabase.Values.ToList();
        }

        private string GetPlayerName(ulong playerId)
        {
            return $"Player_{playerId}";
        }

        #endregion
    }
}
