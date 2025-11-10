using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Factions
{
    /// <summary>
    /// Comprehensive faction and reputation system with multiple factions, reputation tiers,
    /// faction-specific rewards, vendors, abilities, and dynamic faction relationships.
    /// </summary>
    public class FactionSystem : NetworkBehaviour
    {
        public static FactionSystem Instance { get; private set; }

        [Header("Faction Configuration")]
        [SerializeField] private int maxReputation = 10000;
        [SerializeField] private int minReputation = -10000;
        [SerializeField] private bool enableDynamicRelations = true;

        // Faction data
        private Dictionary<string, Faction> factionDatabase = new Dictionary<string, Faction>();
        private Dictionary<ulong, PlayerFactionData> playerFactionData = new Dictionary<ulong, PlayerFactionData>();

        // Events
        public event Action<ulong, string, int> OnReputationChanged;
        public event Action<ulong, string, ReputationTier> OnTierChanged;
        public event Action<ulong, string> OnFactionAbilityUnlocked;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer)
            {
                InitializeFactions();
            }
        }

        private void InitializeFactions()
        {
            // ===== SURVIVORS FACTION =====
            factionDatabase["survivors"] = new Faction
            {
                factionId = "survivors",
                factionName = "The Survivors",
                description = "Ordinary people banding together to survive the apocalypse",
                baseLocation = new Vector3(100, 0, 100),
                factionType = FactionType.Friendly,
                reputationTiers = InitializeReputationTiers(),
                vendorItems = new List<VendorItem>
                {
                    new VendorItem { itemId = "consumable_medkit", price = 100, requiredTier = ReputationTier.Neutral },
                    new VendorItem { itemId = "weapon_pistol_glock", price = 500, requiredTier = ReputationTier.Friendly },
                    new VendorItem { itemId = "armor_vest_light", price = 800, requiredTier = ReputationTier.Honored },
                    new VendorItem { itemId = "blueprint_survivors_hideout", price = 5000, requiredTier = ReputationTier.Revered },
                    new VendorItem { itemId = "skill_survivor_instinct", price = 10000, requiredTier = ReputationTier.Exalted }
                },
                factionAbilities = new List<FactionAbility>
                {
                    new FactionAbility
                    {
                        abilityId = "survivors_scavenge",
                        abilityName = "Scavenger's Luck",
                        description = "Increased loot from containers",
                        requiredTier = ReputationTier.Honored,
                        bonuses = new AbilityBonuses { lootBonus = 0.15f }
                    },
                    new FactionAbility
                    {
                        abilityId = "survivors_community",
                        abilityName = "Community Support",
                        description = "Reduced vendor prices",
                        requiredTier = ReputationTier.Revered,
                        bonuses = new AbilityBonuses { vendorDiscount = 0.20f }
                    }
                },
                relationships = new Dictionary<string, FactionRelationship>
                {
                    { "military", FactionRelationship.Neutral },
                    { "scientists", FactionRelationship.Friendly },
                    { "raiders", FactionRelationship.Hostile },
                    { "cultists", FactionRelationship.Hostile }
                }
            };

            // ===== MILITARY FACTION =====
            factionDatabase["military"] = new Faction
            {
                factionId = "military",
                factionName = "The Remnants",
                description = "Surviving military forces trying to restore order",
                baseLocation = new Vector3(1000, 0, 500),
                factionType = FactionType.Neutral,
                reputationTiers = InitializeReputationTiers(),
                vendorItems = new List<VendorItem>
                {
                    new VendorItem { itemId = "weapon_rifle_m4", price = 5000, requiredTier = ReputationTier.Friendly },
                    new VendorItem { itemId = "armor_vest_tactical", price = 8000, requiredTier = ReputationTier.Honored },
                    new VendorItem { itemId = "ammo_556", price = 10, requiredTier = ReputationTier.Neutral },
                    new VendorItem { itemId = "weapon_sniper_awp", price = 15000, requiredTier = ReputationTier.Revered },
                    new VendorItem { itemId = "blueprint_military_turret", price = 20000, requiredTier = ReputationTier.Exalted }
                },
                factionAbilities = new List<FactionAbility>
                {
                    new FactionAbility
                    {
                        abilityId = "military_discipline",
                        abilityName = "Military Discipline",
                        description = "Increased weapon damage",
                        requiredTier = ReputationTier.Honored,
                        bonuses = new AbilityBonuses { damageBonus = 0.15f }
                    },
                    new FactionAbility
                    {
                        abilityId = "military_training",
                        abilityName = "Advanced Training",
                        description = "Improved accuracy and reload speed",
                        requiredTier = ReputationTier.Revered,
                        bonuses = new AbilityBonuses { accuracy = 0.20f, reloadSpeed = 0.25f }
                    }
                },
                relationships = new Dictionary<string, FactionRelationship>
                {
                    { "survivors", FactionRelationship.Neutral },
                    { "scientists", FactionRelationship.Friendly },
                    { "raiders", FactionRelationship.Hostile },
                    { "cultists", FactionRelationship.Hostile }
                }
            };

            // ===== SCIENTISTS FACTION =====
            factionDatabase["scientists"] = new Faction
            {
                factionId = "scientists",
                factionName = "The Institute",
                description = "Researchers seeking a cure for the infection",
                baseLocation = new Vector3(600, 0, -700),
                factionType = FactionType.Friendly,
                reputationTiers = InitializeReputationTiers(),
                vendorItems = new List<VendorItem>
                {
                    new VendorItem { itemId = "consumable_stim_pack", price = 1000, requiredTier = ReputationTier.Friendly },
                    new VendorItem { itemId = "consumable_antidote", price = 2000, requiredTier = ReputationTier.Honored },
                    new VendorItem { itemId = "blueprint_advanced_medkit", price = 5000, requiredTier = ReputationTier.Revered },
                    new VendorItem { itemId = "serum_mutation", price = 15000, requiredTier = ReputationTier.Exalted }
                },
                factionAbilities = new List<FactionAbility>
                {
                    new FactionAbility
                    {
                        abilityId = "scientists_research",
                        abilityName = "Research Insights",
                        description = "Increased XP gain",
                        requiredTier = ReputationTier.Honored,
                        bonuses = new AbilityBonuses { xpBonus = 0.20f }
                    },
                    new FactionAbility
                    {
                        abilityId = "scientists_chemistry",
                        abilityName = "Advanced Chemistry",
                        description = "Improved consumable effectiveness",
                        requiredTier = ReputationTier.Revered,
                        bonuses = new AbilityBonuses { healingBonus = 0.30f }
                    }
                },
                relationships = new Dictionary<string, FactionRelationship>
                {
                    { "survivors", FactionRelationship.Friendly },
                    { "military", FactionRelationship.Friendly },
                    { "raiders", FactionRelationship.Neutral },
                    { "cultists", FactionRelationship.Hostile }
                }
            };

            // ===== RAIDERS FACTION =====
            factionDatabase["raiders"] = new Faction
            {
                factionId = "raiders",
                factionName = "The Marauders",
                description = "Ruthless bandits taking what they want by force",
                baseLocation = new Vector3(-1200, 0, 800),
                factionType = FactionType.Hostile,
                reputationTiers = InitializeReputationTiers(),
                vendorItems = new List<VendorItem>
                {
                    new VendorItem { itemId = "weapon_melee_katana", price = 2000, requiredTier = ReputationTier.Friendly },
                    new VendorItem { itemId = "armor_raider_gear", price = 4000, requiredTier = ReputationTier.Honored },
                    new VendorItem { itemId = "explosive_c4", price = 3000, requiredTier = ReputationTier.Honored },
                    new VendorItem { itemId = "skill_raider_brutality", price = 12000, requiredTier = ReputationTier.Revered }
                },
                factionAbilities = new List<FactionAbility>
                {
                    new FactionAbility
                    {
                        abilityId = "raider_plunder",
                        abilityName = "Plunderer",
                        description = "Increased currency from kills",
                        requiredTier = ReputationTier.Honored,
                        bonuses = new AbilityBonuses { currencyBonus = 0.25f }
                    },
                    new FactionAbility
                    {
                        abilityId = "raider_intimidation",
                        abilityName = "Intimidation",
                        description = "Enemies deal less damage",
                        requiredTier = ReputationTier.Revered,
                        bonuses = new AbilityBonuses { enemyDamageReduction = 0.15f }
                    }
                },
                relationships = new Dictionary<string, FactionRelationship>
                {
                    { "survivors", FactionRelationship.Hostile },
                    { "military", FactionRelationship.Hostile },
                    { "scientists", FactionRelationship.Neutral },
                    { "cultists", FactionRelationship.Neutral }
                }
            };

            // ===== CULTISTS FACTION =====
            factionDatabase["cultists"] = new Faction
            {
                factionId = "cultists",
                factionName = "The Forsaken",
                description = "Mysterious cult worshipping the infection",
                baseLocation = new Vector3(-800, 0, -900),
                factionType = FactionType.Hostile,
                reputationTiers = InitializeReputationTiers(),
                vendorItems = new List<VendorItem>
                {
                    new VendorItem { itemId = "weapon_ritual_blade", price = 5000, requiredTier = ReputationTier.Friendly },
                    new VendorItem { itemId = "armor_cultist_robes", price = 6000, requiredTier = ReputationTier.Honored },
                    new VendorItem { itemId = "consumable_dark_elixir", price = 8000, requiredTier = ReputationTier.Revered },
                    new VendorItem { itemId = "skill_dark_pact", price = 20000, requiredTier = ReputationTier.Exalted }
                },
                factionAbilities = new List<FactionAbility>
                {
                    new FactionAbility
                    {
                        abilityId = "cultist_embrace",
                        abilityName = "Embrace the Darkness",
                        description = "Reduced damage from infected",
                        requiredTier = ReputationTier.Honored,
                        bonuses = new AbilityBonuses { zombieDamageReduction = 0.30f }
                    },
                    new FactionAbility
                    {
                        abilityId = "cultist_sacrifice",
                        abilityName = "Blood Sacrifice",
                        description = "Convert health to damage",
                        requiredTier = ReputationTier.Revered,
                        bonuses = new AbilityBonuses { damageBonus = 0.40f, healthCost = 0.20f }
                    }
                },
                relationships = new Dictionary<string, FactionRelationship>
                {
                    { "survivors", FactionRelationship.Hostile },
                    { "military", FactionRelationship.Hostile },
                    { "scientists", FactionRelationship.Hostile },
                    { "raiders", FactionRelationship.Neutral }
                }
            };

            // ===== TRADERS FACTION =====
            factionDatabase["traders"] = new Faction
            {
                factionId = "traders",
                factionName = "The Merchants Guild",
                description = "Opportunistic traders profiting from the apocalypse",
                baseLocation = new Vector3(0, 0, -500),
                factionType = FactionType.Neutral,
                reputationTiers = InitializeReputationTiers(),
                vendorItems = new List<VendorItem>
                {
                    new VendorItem { itemId = "material_rare_parts", price = 500, requiredTier = ReputationTier.Neutral },
                    new VendorItem { itemId = "blueprint_random", price = 2000, requiredTier = ReputationTier.Friendly },
                    new VendorItem { itemId = "crate_premium", price = 5000, requiredTier = ReputationTier.Honored },
                    new VendorItem { itemId = "vehicle_armored_truck", price = 50000, requiredTier = ReputationTier.Exalted }
                },
                factionAbilities = new List<FactionAbility>
                {
                    new FactionAbility
                    {
                        abilityId = "trader_haggle",
                        abilityName = "Master Haggler",
                        description = "Better vendor prices everywhere",
                        requiredTier = ReputationTier.Honored,
                        bonuses = new AbilityBonuses { vendorDiscount = 0.25f, vendorSellBonus = 0.20f }
                    },
                    new FactionAbility
                    {
                        abilityId = "trader_fortune",
                        abilityName = "Fortune's Favor",
                        description = "Increased currency from all sources",
                        requiredTier = ReputationTier.Revered,
                        bonuses = new AbilityBonuses { currencyBonus = 0.30f }
                    }
                },
                relationships = new Dictionary<string, FactionRelationship>
                {
                    { "survivors", FactionRelationship.Friendly },
                    { "military", FactionRelationship.Neutral },
                    { "scientists", FactionRelationship.Neutral },
                    { "raiders", FactionRelationship.Neutral },
                    { "cultists", FactionRelationship.Neutral }
                }
            };

            Debug.Log($"Initialized {factionDatabase.Count} factions");
        }

        private Dictionary<ReputationTier, ReputationTierDefinition> InitializeReputationTiers()
        {
            return new Dictionary<ReputationTier, ReputationTierDefinition>
            {
                {
                    ReputationTier.Hated,
                    new ReputationTierDefinition
                    {
                        tier = ReputationTier.Hated,
                        tierName = "Hated",
                        minReputation = -10000,
                        maxReputation = -6000,
                        color = new Color(0.5f, 0f, 0f)
                    }
                },
                {
                    ReputationTier.Hostile,
                    new ReputationTierDefinition
                    {
                        tier = ReputationTier.Hostile,
                        tierName = "Hostile",
                        minReputation = -6000,
                        maxReputation = -3000,
                        color = new Color(0.8f, 0f, 0f)
                    }
                },
                {
                    ReputationTier.Unfriendly,
                    new ReputationTierDefinition
                    {
                        tier = ReputationTier.Unfriendly,
                        tierName = "Unfriendly",
                        minReputation = -3000,
                        maxReputation = 0,
                        color = new Color(1f, 0.3f, 0f)
                    }
                },
                {
                    ReputationTier.Neutral,
                    new ReputationTierDefinition
                    {
                        tier = ReputationTier.Neutral,
                        tierName = "Neutral",
                        minReputation = 0,
                        maxReputation = 3000,
                        color = new Color(1f, 1f, 0f)
                    }
                },
                {
                    ReputationTier.Friendly,
                    new ReputationTierDefinition
                    {
                        tier = ReputationTier.Friendly,
                        tierName = "Friendly",
                        minReputation = 3000,
                        maxReputation = 6000,
                        color = new Color(0f, 1f, 0f)
                    }
                },
                {
                    ReputationTier.Honored,
                    new ReputationTierDefinition
                    {
                        tier = ReputationTier.Honored,
                        tierName = "Honored",
                        minReputation = 6000,
                        maxReputation = 8000,
                        color = new Color(0f, 0.8f, 1f)
                    }
                },
                {
                    ReputationTier.Revered,
                    new ReputationTierDefinition
                    {
                        tier = ReputationTier.Revered,
                        tierName = "Revered",
                        minReputation = 8000,
                        maxReputation = 9500,
                        color = new Color(0.5f, 0f, 1f)
                    }
                },
                {
                    ReputationTier.Exalted,
                    new ReputationTierDefinition
                    {
                        tier = ReputationTier.Exalted,
                        tierName = "Exalted",
                        minReputation = 9500,
                        maxReputation = 10000,
                        color = new Color(1f, 0f, 1f)
                    }
                }
            };
        }

        // Main faction operations
        [ServerRpc(RequireOwnership = false)]
        public void InitializePlayerFactionsServerRpc(ulong playerId, ServerRpcParams rpcParams = default)
        {
            if (playerFactionData.ContainsKey(playerId)) return;

            var data = new PlayerFactionData
            {
                playerId = playerId,
                factionReputations = new Dictionary<string, int>(),
                unlockedAbilities = new List<string>()
            };

            // Initialize all factions at neutral (0 reputation)
            foreach (var factionId in factionDatabase.Keys)
            {
                data.factionReputations[factionId] = 0;
            }

            playerFactionData[playerId] = data;

            Debug.Log($"Initialized faction data for player {playerId}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void AddReputationServerRpc(ulong playerId, string factionId, int amount, ServerRpcParams rpcParams = default)
        {
            if (!playerFactionData.TryGetValue(playerId, out var data)) return;
            if (!factionDatabase.ContainsKey(factionId)) return;

            int oldReputation = data.factionReputations.GetValueOrDefault(factionId, 0);
            int newReputation = Mathf.Clamp(oldReputation + amount, minReputation, maxReputation);
            
            data.factionReputations[factionId] = newReputation;

            // Check for tier change
            ReputationTier oldTier = GetReputationTier(oldReputation);
            ReputationTier newTier = GetReputationTier(newReputation);

            if (oldTier != newTier)
            {
                OnTierChanged?.Invoke(playerId, factionId, newTier);
                CheckAbilityUnlocks(playerId, factionId, newTier);
                NotifyTierChangedClientRpc(playerId, factionId, newTier);
            }

            OnReputationChanged?.Invoke(playerId, factionId, newReputation);

            // Dynamic faction relationships
            if (enableDynamicRelations)
            {
                ApplyDynamicRelationships(playerId, factionId, amount);
            }

            Debug.Log($"Player {playerId} gained {amount} reputation with {factionId}. New total: {newReputation}");
        }

        private void ApplyDynamicRelationships(ulong playerId, string gainedFactionId, int amount)
        {
            if (!factionDatabase.TryGetValue(gainedFactionId, out var gainedFaction)) return;
            if (!playerFactionData.TryGetValue(playerId, out var data)) return;

            // Affect relationships with other factions
            foreach (var kvp in gainedFaction.relationships)
            {
                string otherFactionId = kvp.Key;
                FactionRelationship relationship = kvp.Value;

                int reputationChange = 0;

                switch (relationship)
                {
                    case FactionRelationship.Allied:
                        reputationChange = Mathf.RoundToInt(amount * 0.5f); // Allies gain 50%
                        break;
                    case FactionRelationship.Friendly:
                        reputationChange = Mathf.RoundToInt(amount * 0.25f); // Friends gain 25%
                        break;
                    case FactionRelationship.Neutral:
                        reputationChange = 0; // No change
                        break;
                    case FactionRelationship.Hostile:
                        reputationChange = -Mathf.RoundToInt(amount * 0.25f); // Enemies lose 25%
                        break;
                    case FactionRelationship.Enemy:
                        reputationChange = -Mathf.RoundToInt(amount * 0.5f); // Sworn enemies lose 50%
                        break;
                }

                if (reputationChange != 0 && data.factionReputations.ContainsKey(otherFactionId))
                {
                    int oldRep = data.factionReputations[otherFactionId];
                    data.factionReputations[otherFactionId] = Mathf.Clamp(oldRep + reputationChange, minReputation, maxReputation);
                }
            }
        }

        private void CheckAbilityUnlocks(ulong playerId, string factionId, ReputationTier tier)
        {
            if (!factionDatabase.TryGetValue(factionId, out var faction)) return;
            if (!playerFactionData.TryGetValue(playerId, out var data)) return;

            foreach (var ability in faction.factionAbilities)
            {
                if (ability.requiredTier <= tier && !data.unlockedAbilities.Contains(ability.abilityId))
                {
                    data.unlockedAbilities.Add(ability.abilityId);
                    OnFactionAbilityUnlocked?.Invoke(playerId, ability.abilityId);
                    NotifyAbilityUnlockedClientRpc(playerId, ability.abilityId, ability.abilityName);

                    Debug.Log($"Player {playerId} unlocked ability {ability.abilityName}");
                }
            }
        }

        public ReputationTier GetReputationTier(int reputation)
        {
            if (reputation >= 9500) return ReputationTier.Exalted;
            if (reputation >= 8000) return ReputationTier.Revered;
            if (reputation >= 6000) return ReputationTier.Honored;
            if (reputation >= 3000) return ReputationTier.Friendly;
            if (reputation >= 0) return ReputationTier.Neutral;
            if (reputation >= -3000) return ReputationTier.Unfriendly;
            if (reputation >= -6000) return ReputationTier.Hostile;
            return ReputationTier.Hated;
        }

        public AbilityBonuses CalculatePlayerBonuses(ulong playerId)
        {
            if (!playerFactionData.TryGetValue(playerId, out var data))
                return new AbilityBonuses();

            var totalBonuses = new AbilityBonuses();

            foreach (var abilityId in data.unlockedAbilities)
            {
                // Find ability across all factions
                foreach (var faction in factionDatabase.Values)
                {
                    var ability = faction.factionAbilities.FirstOrDefault(a => a.abilityId == abilityId);
                    if (ability != null)
                    {
                        totalBonuses.damageBonus += ability.bonuses.damageBonus;
                        totalBonuses.xpBonus += ability.bonuses.xpBonus;
                        totalBonuses.lootBonus += ability.bonuses.lootBonus;
                        totalBonuses.healingBonus += ability.bonuses.healingBonus;
                        totalBonuses.currencyBonus += ability.bonuses.currencyBonus;
                        totalBonuses.vendorDiscount += ability.bonuses.vendorDiscount;
                        totalBonuses.accuracy += ability.bonuses.accuracy;
                        totalBonuses.reloadSpeed += ability.bonuses.reloadSpeed;
                        // Add other bonuses...
                    }
                }
            }

            return totalBonuses;
        }

        public bool CanAccessVendorItem(ulong playerId, string factionId, string itemId)
        {
            if (!playerFactionData.TryGetValue(playerId, out var data)) return false;
            if (!factionDatabase.TryGetValue(factionId, out var faction)) return false;

            int reputation = data.factionReputations.GetValueOrDefault(factionId, 0);
            ReputationTier tier = GetReputationTier(reputation);

            var item = faction.vendorItems.FirstOrDefault(vi => vi.itemId == itemId);
            if (item == null) return false;

            return tier >= item.requiredTier;
        }

        // Client RPCs
        [ClientRpc]
        private void NotifyTierChangedClientRpc(ulong playerId, string factionId, ReputationTier newTier) { }

        [ClientRpc]
        private void NotifyAbilityUnlockedClientRpc(ulong playerId, string abilityId, string abilityName) { }

        // Public getters
        public Faction GetFaction(string factionId) => factionDatabase.GetValueOrDefault(factionId);
        public PlayerFactionData GetPlayerFactionData(ulong playerId) => playerFactionData.GetValueOrDefault(playerId);
        public int GetPlayerReputation(ulong playerId, string factionId) =>
            playerFactionData.TryGetValue(playerId, out var data) ? data.factionReputations.GetValueOrDefault(factionId, 0) : 0;
    }

    // Data structures
    [Serializable]
    public class Faction
    {
        public string factionId;
        public string factionName;
        public string description;
        public Vector3 baseLocation;
        public FactionType factionType;
        public Dictionary<ReputationTier, ReputationTierDefinition> reputationTiers;
        public List<VendorItem> vendorItems;
        public List<FactionAbility> factionAbilities;
        public Dictionary<string, FactionRelationship> relationships;
    }

    [Serializable]
    public class ReputationTierDefinition
    {
        public ReputationTier tier;
        public string tierName;
        public int minReputation;
        public int maxReputation;
        public Color color;
    }

    [Serializable]
    public class VendorItem
    {
        public string itemId;
        public int price;
        public ReputationTier requiredTier;
    }

    [Serializable]
    public class FactionAbility
    {
        public string abilityId;
        public string abilityName;
        public string description;
        public ReputationTier requiredTier;
        public AbilityBonuses bonuses;
    }

    [Serializable]
    public class AbilityBonuses
    {
        public float damageBonus;
        public float xpBonus;
        public float lootBonus;
        public float healingBonus;
        public float currencyBonus;
        public float vendorDiscount;
        public float vendorSellBonus;
        public float accuracy;
        public float reloadSpeed;
        public float zombieDamageReduction;
        public float enemyDamageReduction;
        public float healthCost;
    }

    [Serializable]
    public class PlayerFactionData
    {
        public ulong playerId;
        public Dictionary<string, int> factionReputations;
        public List<string> unlockedAbilities;
    }

    public enum FactionType { Friendly, Neutral, Hostile }
    public enum FactionRelationship { Allied, Friendly, Neutral, Hostile, Enemy }
    public enum ReputationTier { Hated = 0, Hostile = 1, Unfriendly = 2, Neutral = 3, Friendly = 4, Honored = 5, Revered = 6, Exalted = 7 }
}
