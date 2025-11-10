using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Enhancement
{
    /// <summary>
    /// Comprehensive gear enhancement and augmentation system for multi-platform zombie multiplayer game.
    /// Handles weapon/armor upgrades, enchantments, sockets, and stat modifications.
    /// </summary>
    public class GearEnhancementSystem : NetworkBehaviour
    {
        public static GearEnhancementSystem Instance { get; private set; }

        [Header("Enhancement Settings")]
        [SerializeField] private bool enableEnhancement = true;
        [SerializeField] private int maxEnhancementLevel = 25;
        [SerializeField] private int maxSocketSlots = 5;
        [SerializeField] private bool allowEnhancementFailure = true;
        [SerializeField] private float baseFailureRate = 0.1f;

        [Header("Cost Settings")]
        [SerializeField] private int baseCurrencyCost = 100;
        [SerializeField] private float costMultiplierPerLevel = 1.5f;
        [SerializeField] private bool requireMaterials = true;

        [Header("Bonus Settings")]
        [SerializeField] private float statBonusPerLevel = 0.05f; // 5% per level
        [SerializeField] private bool enableSetBonuses = true;
        [SerializeField] private int minSetPieces = 2;

        // Enums
        public enum EnhancementType
        {
            Upgrade,        // Basic stat increase
            Enchantment,    // Special effects
            Socket,         // Gem slots
            Reforge,        // Stat reroll
            Transcendence   // Max level breakthrough
        }

        public enum GemType
        {
            Ruby,           // Damage
            Sapphire,       // Defense
            Emerald,        // Health
            Diamond,        // Critical
            Topaz,          // Speed
            Amethyst,       // Magic
            Onyx            // Special
        }

        public enum EnhancementResult
        {
            Success,
            Failure,
            GreatSuccess,
            Destroyed
        }

        // Data structures
        [Serializable]
        public class EnhancedItem
        {
            public string itemId;
            public string baseItemId;
            public int enhancementLevel;
            public List<Enchantment> enchantments = new List<Enchantment>();
            public List<GemSocket> sockets = new List<GemSocket>();
            public Dictionary<string, float> bonusStats = new Dictionary<string, float>();
            public int timesEnhanced;
            public bool isMaxLevel;
            public string setId;
        }

        [Serializable]
        public class Enchantment
        {
            public string enchantmentId;
            public string enchantmentName;
            public string description;
            public EnchantmentTier tier;
            public Dictionary<string, float> statModifiers = new Dictionary<string, float>();
            public List<string> specialEffects = new List<string>();
            public int level;
        }

        public enum EnchantmentTier
        {
            Minor,
            Lesser,
            Greater,
            Superior,
            Legendary
        }

        [Serializable]
        public class GemSocket
        {
            public int socketIndex;
            public GemType? gemType;
            public int gemLevel;
            public bool isUnlocked;
            public Dictionary<string, float> gemStats = new Dictionary<string, float>();
        }

        [Serializable]
        public class EnhancementMaterial
        {
            public string materialId;
            public string materialName;
            public int quantity;
            public EnhancementTier tier;
        }

        [Serializable]
        public class SetBonus
        {
            public string setId;
            public string setName;
            public Dictionary<int, List<SetEffect>> bonusesByPieceCount = new Dictionary<int, List<SetEffect>>();
        }

        [Serializable]
        public class SetEffect
        {
            public string effectName;
            public string description;
            public Dictionary<string, float> statBonus = new Dictionary<string, float>();
        }

        [Serializable]
        public class EnhancementAttempt
        {
            public ulong playerId;
            public string itemId;
            public EnhancementType type;
            public DateTime timestamp;
            public EnhancementResult result;
            public int costPaid;
        }

        // State
        private Dictionary<string, EnhancedItem> enhancedItems = new Dictionary<string, EnhancedItem>();
        private Dictionary<string, SetBonus> setDatabase = new Dictionary<string, SetBonus>();
        private Dictionary<string, Enchantment> enchantmentDatabase = new Dictionary<string, Enchantment>();
        private List<EnhancementAttempt> enhancementHistory = new List<EnhancementAttempt>();

        // Events
        public event Action<ulong, EnhancedItem, EnhancementResult> OnEnhancementAttempt;
        public event Action<ulong, EnhancedItem> OnItemMaxLevel;
        public event Action<ulong, string, int> OnSetBonusActivated;
        public event Action<ulong, GemSocket> OnGemSocketed;

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
                InitializeEnhancementSystem();
            }
        }

        #region Initialization

        private void InitializeEnhancementSystem()
        {
            InitializeEnchantments();
            InitializeSets();
        }

        private void InitializeEnchantments()
        {
            // Damage enchantments
            RegisterEnchantment(new Enchantment
            {
                enchantmentId = "ench_damage_minor",
                enchantmentName = "Minor Sharpness",
                description = "+5% damage",
                tier = EnchantmentTier.Minor,
                statModifiers = new Dictionary<string, float> { { "Damage", 1.05f } },
                level = 1
            });

            RegisterEnchantment(new Enchantment
            {
                enchantmentId = "ench_damage_greater",
                enchantmentName = "Greater Sharpness",
                description = "+15% damage",
                tier = EnchantmentTier.Greater,
                statModifiers = new Dictionary<string, float> { { "Damage", 1.15f } },
                level = 1
            });

            // Defense enchantments
            RegisterEnchantment(new Enchantment
            {
                enchantmentId = "ench_armor_minor",
                enchantmentName = "Minor Protection",
                description = "+5% armor",
                tier = EnchantmentTier.Minor,
                statModifiers = new Dictionary<string, float> { { "Armor", 1.05f } },
                level = 1
            });

            // Special enchantments
            RegisterEnchantment(new Enchantment
            {
                enchantmentId = "ench_lifesteal",
                enchantmentName = "Life Steal",
                description = "5% lifesteal on hit",
                tier = EnchantmentTier.Superior,
                specialEffects = new List<string> { "Lifesteal_5" },
                level = 1
            });
        }

        private void RegisterEnchantment(Enchantment enchantment)
        {
            enchantmentDatabase[enchantment.enchantmentId] = enchantment;
        }

        private void InitializeSets()
        {
            // Combat Set
            var combatSet = new SetBonus
            {
                setId = "set_combat",
                setName = "Combat Mastery Set"
            };

            combatSet.bonusesByPieceCount[2] = new List<SetEffect>
            {
                new SetEffect
                {
                    effectName = "Combat Focus",
                    description = "+10% damage",
                    statBonus = new Dictionary<string, float> { { "Damage", 1.10f } }
                }
            };

            combatSet.bonusesByPieceCount[4] = new List<SetEffect>
            {
                new SetEffect
                {
                    effectName = "Combat Expertise",
                    description = "+20% damage, +10% attack speed",
                    statBonus = new Dictionary<string, float> { { "Damage", 1.20f }, { "AttackSpeed", 1.10f } }
                }
            };

            setDatabase["set_combat"] = combatSet;

            // Survival Set
            var survivalSet = new SetBonus
            {
                setId = "set_survival",
                setName = "Survivor's Endurance Set"
            };

            survivalSet.bonusesByPieceCount[2] = new List<SetEffect>
            {
                new SetEffect
                {
                    effectName = "Resilience",
                    description = "+15% health",
                    statBonus = new Dictionary<string, float> { { "Health", 1.15f } }
                }
            };

            survivalSet.bonusesByPieceCount[4] = new List<SetEffect>
            {
                new SetEffect
                {
                    effectName = "Unbreakable",
                    description = "+30% health, +10% armor",
                    statBonus = new Dictionary<string, float> { { "Health", 1.30f }, { "Armor", 1.10f } }
                }
            };

            setDatabase["set_survival"] = survivalSet;
        }

        #endregion

        #region Enhancement

        [ServerRpc(RequireOwnership = false)]
        public void EnhanceItemServerRpc(ulong playerId, string itemId, EnhancementType type)
        {
            if (!enableEnhancement)
            {
                Debug.LogWarning("Enhancement system is disabled");
                return;
            }

            // Get or create enhanced item
            if (!enhancedItems.ContainsKey(itemId))
            {
                enhancedItems[itemId] = new EnhancedItem
                {
                    itemId = itemId,
                    baseItemId = itemId,
                    enhancementLevel = 0
                };
            }

            var item = enhancedItems[itemId];

            // Check max level
            if (item.enhancementLevel >= maxEnhancementLevel && type == EnhancementType.Upgrade)
            {
                Debug.LogWarning($"Item {itemId} is already at max enhancement level");
                return;
            }

            // Calculate cost
            int cost = CalculateEnhancementCost(item.enhancementLevel, type);

            // Check if player can afford
            if (!Economy.EconomyManager.Instance.SpendSoftCurrency(playerId, cost))
            {
                Debug.LogWarning($"Player {playerId} cannot afford enhancement cost {cost}");
                return;
            }

            // Check materials if required
            if (requireMaterials && type == EnhancementType.Upgrade)
            {
                if (!HasRequiredMaterials(playerId, item.enhancementLevel))
                {
                    Economy.EconomyManager.Instance.AddSoftCurrency(playerId, cost); // Refund
                    Debug.LogWarning($"Player {playerId} doesn't have required materials");
                    return;
                }
            }

            // Attempt enhancement
            EnhancementResult result = AttemptEnhancement(item, type);

            // Record attempt
            var attempt = new EnhancementAttempt
            {
                playerId = playerId,
                itemId = itemId,
                type = type,
                timestamp = DateTime.UtcNow,
                result = result,
                costPaid = cost
            };

            enhancementHistory.Add(attempt);

            if (enhancementHistory.Count > 1000)
            {
                enhancementHistory.RemoveAt(0);
            }

            OnEnhancementAttempt?.Invoke(playerId, item, result);

            // Handle result
            switch (result)
            {
                case EnhancementResult.Success:
                    ApplyEnhancementSuccess(item, type);
                    NotifyEnhancementClientRpc(playerId, itemId, result, item.enhancementLevel);
                    break;

                case EnhancementResult.GreatSuccess:
                    ApplyEnhancementSuccess(item, type);
                    item.enhancementLevel++; // Extra level
                    NotifyEnhancementClientRpc(playerId, itemId, result, item.enhancementLevel);
                    break;

                case EnhancementResult.Failure:
                    NotifyEnhancementClientRpc(playerId, itemId, result, item.enhancementLevel);
                    break;

                case EnhancementResult.Destroyed:
                    // Item destroyed
                    enhancedItems.Remove(itemId);
                    Inventory.InventoryManager.Instance?.RemoveItem(playerId, itemId, 1);
                    NotifyEnhancementClientRpc(playerId, itemId, result, 0);
                    break;
            }

            Debug.Log($"Player {playerId} enhancement result for {itemId}: {result}");
        }

        private EnhancementResult AttemptEnhancement(EnhancedItem item, EnhancementType type)
        {
            if (!allowEnhancementFailure)
                return EnhancementResult.Success;

            float failureRate = CalculateFailureRate(item.enhancementLevel);
            float roll = UnityEngine.Random.value;

            if (roll < 0.05f) // 5% great success
                return EnhancementResult.GreatSuccess;

            if (roll < failureRate)
            {
                // Check if item destroyed (only at high levels)
                if (item.enhancementLevel >= 15 && roll < failureRate * 0.1f)
                    return EnhancementResult.Destroyed;

                return EnhancementResult.Failure;
            }

            return EnhancementResult.Success;
        }

        private void ApplyEnhancementSuccess(EnhancedItem item, EnhancementType type)
        {
            switch (type)
            {
                case EnhancementType.Upgrade:
                    item.enhancementLevel++;
                    item.timesEnhanced++;
                    UpdateItemStats(item);
                    break;

                case EnhancementType.Socket:
                    AddSocketSlot(item);
                    break;
            }

            // Check max level
            if (item.enhancementLevel >= maxEnhancementLevel)
            {
                item.isMaxLevel = true;
                // Max level reached, notify player
            }
        }

        private void UpdateItemStats(EnhancedItem item)
        {
            // Calculate bonus stats based on enhancement level
            float damageBonus = 1f + (item.enhancementLevel * statBonusPerLevel);
            float defenseBonus = 1f + (item.enhancementLevel * statBonusPerLevel * 0.8f);

            item.bonusStats["Damage"] = damageBonus;
            item.bonusStats["Defense"] = defenseBonus;
            item.bonusStats["Durability"] = 1f + (item.enhancementLevel * 0.02f);
        }

        private int CalculateEnhancementCost(int currentLevel, EnhancementType type)
        {
            float cost = baseCurrencyCost * Mathf.Pow(costMultiplierPerLevel, currentLevel);
            return Mathf.RoundToInt(cost);
        }

        private float CalculateFailureRate(int level)
        {
            if (level < 5) return 0f;
            if (level < 10) return baseFailureRate;
            if (level < 15) return baseFailureRate * 2f;
            return baseFailureRate * 4f;
        }

        [ClientRpc]
        private void NotifyEnhancementClientRpc(ulong playerId, string itemId, EnhancementResult result, int newLevel)
        {
            if (NetworkManager.Singleton.LocalClientId != playerId) return;

            string resultMessage = result switch
            {
                EnhancementResult.Success => $"Enhancement successful! Level {newLevel}",
                EnhancementResult.GreatSuccess => $"Great success! Level {newLevel}",
                EnhancementResult.Failure => "Enhancement failed",
                EnhancementResult.Destroyed => "Item destroyed!",
                _ => ""
            };

            Debug.Log($"[ENHANCEMENT] {resultMessage}");
        }

        #endregion

        #region Sockets & Gems

        private void AddSocketSlot(EnhancedItem item)
        {
            if (item.sockets.Count >= maxSocketSlots)
            {
                Debug.LogWarning($"Item {item.itemId} already has max sockets");
                return;
            }

            var socket = new GemSocket
            {
                socketIndex = item.sockets.Count,
                isUnlocked = true
            };

            item.sockets.Add(socket);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SocketGemServerRpc(ulong playerId, string itemId, int socketIndex, GemType gemType, int gemLevel)
        {
            if (!enhancedItems.ContainsKey(itemId))
            {
                Debug.LogWarning($"Enhanced item {itemId} not found");
                return;
            }

            var item = enhancedItems[itemId];

            if (socketIndex >= item.sockets.Count || !item.sockets[socketIndex].isUnlocked)
            {
                Debug.LogWarning($"Socket {socketIndex} not available");
                return;
            }

            // Check if player has gem
            string gemItemId = $"gem_{gemType.ToString().ToLower()}_{gemLevel}";
            if (!Inventory.InventoryManager.Instance.RemoveItem(playerId, gemItemId, 1))
            {
                Debug.LogWarning($"Player {playerId} doesn't have gem {gemItemId}");
                return;
            }

            // Apply gem
            var socket = item.sockets[socketIndex];
            socket.gemType = gemType;
            socket.gemLevel = gemLevel;
            socket.gemStats = CalculateGemStats(gemType, gemLevel);

            OnGemSocketed?.Invoke(playerId, socket);

            Debug.Log($"Player {playerId} socketed {gemType} level {gemLevel} into {itemId}");
        }

        private Dictionary<string, float> CalculateGemStats(GemType gemType, int gemLevel)
        {
            var stats = new Dictionary<string, float>();
            float baseBonus = 0.05f * gemLevel; // 5% per level

            switch (gemType)
            {
                case GemType.Ruby:
                    stats["Damage"] = 1f + baseBonus;
                    break;
                case GemType.Sapphire:
                    stats["Defense"] = 1f + baseBonus;
                    break;
                case GemType.Emerald:
                    stats["Health"] = 1f + baseBonus;
                    break;
                case GemType.Diamond:
                    stats["CritChance"] = baseBonus * 0.5f;
                    stats["CritDamage"] = 1f + baseBonus;
                    break;
                case GemType.Topaz:
                    stats["Speed"] = 1f + baseBonus;
                    break;
                case GemType.Amethyst:
                    stats["MagicPower"] = 1f + baseBonus;
                    break;
                case GemType.Onyx:
                    stats["AllStats"] = 1f + (baseBonus * 0.3f);
                    break;
            }

            return stats;
        }

        #endregion

        #region Set Bonuses

        public void CheckSetBonuses(ulong playerId, List<string> equippedItems)
        {
            if (!enableSetBonuses) return;

            // Count equipped items per set
            Dictionary<string, int> setItemCounts = new Dictionary<string, int>();

            foreach (var itemId in equippedItems)
            {
                if (!enhancedItems.ContainsKey(itemId)) continue;

                var item = enhancedItems[itemId];
                if (string.IsNullOrEmpty(item.setId)) continue;

                if (!setItemCounts.ContainsKey(item.setId))
                {
                    setItemCounts[item.setId] = 0;
                }

                setItemCounts[item.setId]++;
            }

            // Apply set bonuses
            foreach (var kvp in setItemCounts)
            {
                string setId = kvp.Key;
                int pieceCount = kvp.Value;

                if (pieceCount >= minSetPieces && setDatabase.ContainsKey(setId))
                {
                    OnSetBonusActivated?.Invoke(playerId, setId, pieceCount);
                }
            }
        }

        #endregion

        #region Materials

        private bool HasRequiredMaterials(ulong playerId, int level)
        {
            // Define material requirements based on level
            if (level < 10)
            {
                return Inventory.InventoryManager.Instance.HasItem(playerId, "material_basic", 1);
            }
            else if (level < 20)
            {
                return Inventory.InventoryManager.Instance.HasItem(playerId, "material_advanced", 1);
            }
            else
            {
                return Inventory.InventoryManager.Instance.HasItem(playerId, "material_legendary", 1);
            }
        }

        #endregion

        #region Public API

        public EnhancedItem GetEnhancedItem(string itemId)
        {
            return enhancedItems.ContainsKey(itemId) ? enhancedItems[itemId] : null;
        }

        public List<EnhancementAttempt> GetPlayerEnhancementHistory(ulong playerId, int limit = 50)
        {
            return enhancementHistory
                .Where(a => a.playerId == playerId)
                .OrderByDescending(a => a.timestamp)
                .Take(limit)
                .ToList();
        }

        public Dictionary<string, float> GetTotalItemStats(string itemId)
        {
            if (!enhancedItems.ContainsKey(itemId))
                return new Dictionary<string, float>();

            var item = enhancedItems[itemId];
            var totalStats = new Dictionary<string, float>(item.bonusStats);

            // Add gem stats
            foreach (var socket in item.sockets)
            {
                if (!socket.gemType.HasValue) continue;

                foreach (var stat in socket.gemStats)
                {
                    if (!totalStats.ContainsKey(stat.Key))
                    {
                        totalStats[stat.Key] = stat.Value;
                    }
                    else
                    {
                        totalStats[stat.Key] *= stat.Value;
                    }
                }
            }

            // Add enchantment stats
            foreach (var enchantment in item.enchantments)
            {
                foreach (var stat in enchantment.statModifiers)
                {
                    if (!totalStats.ContainsKey(stat.Key))
                    {
                        totalStats[stat.Key] = stat.Value;
                    }
                    else
                    {
                        totalStats[stat.Key] *= stat.Value;
                    }
                }
            }

            return totalStats;
        }

        #endregion
    }
}
