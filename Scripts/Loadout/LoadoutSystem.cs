using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;
using System;

namespace DeadFrontier.Loadout
{
    /// <summary>
    /// Comprehensive perk and loadout system.
    /// Supports custom loadouts, perks, skill trees, and pre-match character builds.
    /// Includes loadout presets, perk combinations, and balance constraints.
    /// </summary>
    public class LoadoutSystem : NetworkBehaviour
    {
        public static LoadoutSystem Instance { get; private set; }

        [Header("Loadout Settings")]
        [SerializeField] private int maxLoadoutSlots = 5;
        [SerializeField] private int maxPerksPerLoadout = 4;
        [SerializeField] private int maxWeaponsPerLoadout = 3; // Primary, Secondary, Melee

        [Header("Perk Settings")]
        [SerializeField] private int maxPerkPoints = 12;
        [SerializeField] private bool enablePerkSynergies = true;

        // Perk database
        private Dictionary<string, PerkDefinition> perkDefinitions = new Dictionary<string, PerkDefinition>();

        // Player loadout data
        private Dictionary<ulong, PlayerLoadoutData> playerData = new Dictionary<ulong, PlayerLoadoutData>();

        // Active loadouts (during matches)
        private Dictionary<ulong, ActiveLoadout> activeLoadouts = new Dictionary<ulong, ActiveLoadout>();

        // Events
        public event Action<ulong, int> OnLoadoutChanged;
        public event Action<ulong, string> OnPerkEquipped;
        public event Action<ulong, string> OnPerkUnequipped;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializePerks();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        #region Initialization

        private void InitializePerks()
        {
            // Combat Perks
            RegisterPerk(new PerkDefinition
            {
                perkId = "perk_fast_reload",
                perkName = "Fast Reload",
                description = "Reload weapons 25% faster",
                category = PerkCategory.Combat,
                tier = PerkTier.Common,
                cost = 2,
                maxLevel = 3,
                effects = new List<PerkEffect>
                {
                    new PerkEffect { type = EffectType.ReloadSpeed, value = 0.25f, perLevel = 0.1f }
                }
            });

            RegisterPerk(new PerkDefinition
            {
                perkId = "perk_steady_aim",
                perkName = "Steady Aim",
                description = "Reduced weapon sway and recoil",
                category = PerkCategory.Combat,
                tier = PerkTier.Common,
                cost = 2,
                maxLevel = 3,
                effects = new List<PerkEffect>
                {
                    new PerkEffect { type = EffectType.Accuracy, value = 0.15f, perLevel = 0.05f }
                }
            });

            RegisterPerk(new PerkDefinition
            {
                perkId = "perk_iron_lungs",
                perkName = "Iron Lungs",
                description = "Hold breath longer when aiming",
                category = PerkCategory.Combat,
                tier = PerkTier.Uncommon,
                cost = 3,
                maxLevel = 2,
                effects = new List<PerkEffect>
                {
                    new PerkEffect { type = EffectType.BreathDuration, value = 0.50f, perLevel = 0.25f }
                }
            });

            // Survival Perks
            RegisterPerk(new PerkDefinition
            {
                perkId = "perk_health_boost",
                perkName = "Vital",
                description = "Increased maximum health",
                category = PerkCategory.Survival,
                tier = PerkTier.Common,
                cost = 2,
                maxLevel = 5,
                effects = new List<PerkEffect>
                {
                    new PerkEffect { type = EffectType.MaxHealth, value = 10f, perLevel = 5f }
                }
            });

            RegisterPerk(new PerkDefinition
            {
                perkId = "perk_fast_healing",
                perkName = "Quick Recovery",
                description = "Heal faster over time",
                category = PerkCategory.Survival,
                tier = PerkTier.Uncommon,
                cost = 3,
                maxLevel = 3,
                effects = new List<PerkEffect>
                {
                    new PerkEffect { type = EffectType.HealingRate, value = 0.25f, perLevel = 0.15f }
                }
            });

            RegisterPerk(new PerkDefinition
            {
                perkId = "perk_second_wind",
                perkName = "Second Wind",
                description = "Survive one lethal hit per match",
                category = PerkCategory.Survival,
                tier = PerkTier.Rare,
                cost = 4,
                maxLevel = 1,
                effects = new List<PerkEffect>
                {
                    new PerkEffect { type = EffectType.SecondChance, value = 1f }
                }
            });

            // Mobility Perks
            RegisterPerk(new PerkDefinition
            {
                perkId = "perk_sprint_boost",
                perkName = "Marathon",
                description = "Sprint 20% faster",
                category = PerkCategory.Mobility,
                tier = PerkTier.Common,
                cost = 2,
                maxLevel = 3,
                effects = new List<PerkEffect>
                {
                    new PerkEffect { type = EffectType.SprintSpeed, value = 0.20f, perLevel = 0.1f }
                }
            });

            RegisterPerk(new PerkDefinition
            {
                perkId = "perk_lightweight",
                perkName = "Lightweight",
                description = "Move faster with reduced stamina drain",
                category = PerkCategory.Mobility,
                tier = PerkTier.Uncommon,
                cost = 3,
                maxLevel = 2,
                effects = new List<PerkEffect>
                {
                    new PerkEffect { type = EffectType.MovementSpeed, value = 0.10f, perLevel = 0.05f },
                    new PerkEffect { type = EffectType.StaminaDrain, value = -0.20f, perLevel = -0.1f }
                }
            });

            RegisterPerk(new PerkDefinition
            {
                perkId = "perk_parkour_pro",
                perkName = "Parkour Pro",
                description = "Climb and vault faster",
                category = PerkCategory.Mobility,
                tier = PerkTier.Rare,
                cost = 4,
                maxLevel = 2,
                effects = new List<PerkEffect>
                {
                    new PerkEffect { type = EffectType.ClimbSpeed, value = 0.40f, perLevel = 0.20f }
                }
            });

            // Utility Perks
            RegisterPerk(new PerkDefinition
            {
                perkId = "perk_scavenger",
                perkName = "Scavenger",
                description = "Find more loot and ammo",
                category = PerkCategory.Utility,
                tier = PerkTier.Common,
                cost = 2,
                maxLevel = 3,
                effects = new List<PerkEffect>
                {
                    new PerkEffect { type = EffectType.LootChance, value = 0.15f, perLevel = 0.10f }
                }
            });

            RegisterPerk(new PerkDefinition
            {
                perkId = "perk_silent_steps",
                perkName = "Silent Steps",
                description = "Make less noise when moving",
                category = PerkCategory.Utility,
                tier = PerkTier.Uncommon,
                cost = 3,
                maxLevel = 2,
                effects = new List<PerkEffect>
                {
                    new PerkEffect { type = EffectType.NoiseReduction, value = 0.40f, perLevel = 0.20f }
                }
            });

            RegisterPerk(new PerkDefinition
            {
                perkId = "perk_sixth_sense",
                perkName = "Sixth Sense",
                description = "Detect enemies at longer range",
                category = PerkCategory.Utility,
                tier = PerkTier.Rare,
                cost = 4,
                maxLevel = 1,
                effects = new List<PerkEffect>
                {
                    new PerkEffect { type = EffectType.DetectionRange, value = 0.50f }
                }
            });

            // Team Perks
            RegisterPerk(new PerkDefinition
            {
                perkId = "perk_team_medic",
                perkName = "Team Medic",
                description = "Revive allies faster",
                category = PerkCategory.Team,
                tier = PerkTier.Uncommon,
                cost = 3,
                maxLevel = 2,
                effects = new List<PerkEffect>
                {
                    new PerkEffect { type = EffectType.ReviveSpeed, value = 0.30f, perLevel = 0.15f }
                }
            });

            RegisterPerk(new PerkDefinition
            {
                perkId = "perk_leadership",
                perkName = "Leadership",
                description = "Boost nearby allies' damage",
                category = PerkCategory.Team,
                tier = PerkTier.Rare,
                cost = 4,
                maxLevel = 1,
                effects = new List<PerkEffect>
                {
                    new PerkEffect { type = EffectType.TeamDamageBoost, value = 0.10f }
                }
            });

            Debug.Log($"[LoadoutSystem] Initialized {perkDefinitions.Count} perk definitions");
        }

        private void RegisterPerk(PerkDefinition perk)
        {
            perkDefinitions[perk.perkId] = perk;
        }

        #endregion

        #region Player Initialization

        public void InitializePlayerData(ulong playerId)
        {
            if (playerData.ContainsKey(playerId)) return;

            playerData[playerId] = new PlayerLoadoutData
            {
                playerId = playerId,
                loadouts = new List<LoadoutPreset>(),
                unlockedPerks = new List<string>(),
                perkLevels = new Dictionary<string, int>(),
                currentLoadoutIndex = 0
            };

            // Create default loadouts
            for (int i = 0; i < maxLoadoutSlots; i++)
            {
                playerData[playerId].loadouts.Add(new LoadoutPreset
                {
                    loadoutName = $"Loadout {i + 1}",
                    weapons = new List<string>(),
                    equipment = new List<string>(),
                    perks = new List<string>()
                });
            }

            // Unlock starter perks
            UnlockPerk(playerId, "perk_health_boost");
            UnlockPerk(playerId, "perk_fast_reload");
            UnlockPerk(playerId, "perk_scavenger");

            LoadPlayerData(playerId);
        }

        #endregion

        #region Loadout Management

        public bool SetLoadout(ulong playerId, int loadoutIndex, LoadoutPreset preset)
        {
            if (!playerData.ContainsKey(playerId))
            {
                InitializePlayerData(playerId);
            }

            if (loadoutIndex < 0 || loadoutIndex >= maxLoadoutSlots) return false;

            // Validate loadout
            if (!ValidateLoadout(playerId, preset)) return false;

            playerData[playerId].loadouts[loadoutIndex] = preset;

            SavePlayerData(playerId);

            Debug.Log($"[LoadoutSystem] Player {playerId} updated loadout {loadoutIndex}");

            return true;
        }

        public bool SelectLoadout(ulong playerId, int loadoutIndex)
        {
            if (!playerData.ContainsKey(playerId)) return false;

            if (loadoutIndex < 0 || loadoutIndex >= maxLoadoutSlots) return false;

            playerData[playerId].currentLoadoutIndex = loadoutIndex;

            OnLoadoutChanged?.Invoke(playerId, loadoutIndex);

            SavePlayerData(playerId);

            Debug.Log($"[LoadoutSystem] Player {playerId} selected loadout {loadoutIndex}");

            return true;
        }

        private bool ValidateLoadout(ulong playerId, LoadoutPreset preset)
        {
            var data = playerData[playerId];

            // Check perk limits
            if (preset.perks.Count > maxPerksPerLoadout) return false;

            // Check perk unlock status
            foreach (var perkId in preset.perks)
            {
                if (!data.unlockedPerks.Contains(perkId)) return false;
            }

            // Check perk points
            int totalCost = 0;

            foreach (var perkId in preset.perks)
            {
                if (!perkDefinitions.ContainsKey(perkId)) continue;

                var perk = perkDefinitions[perkId];
                int level = data.perkLevels.ContainsKey(perkId) ? data.perkLevels[perkId] : 1;

                totalCost += perk.cost * level;
            }

            if (totalCost > maxPerkPoints) return false;

            // Check weapon limits
            if (preset.weapons.Count > maxWeaponsPerLoadout) return false;

            return true;
        }

        public LoadoutPreset GetLoadout(ulong playerId, int loadoutIndex)
        {
            if (!playerData.ContainsKey(playerId)) return null;

            if (loadoutIndex < 0 || loadoutIndex >= maxLoadoutSlots) return null;

            return playerData[playerId].loadouts[loadoutIndex];
        }

        public LoadoutPreset GetCurrentLoadout(ulong playerId)
        {
            if (!playerData.ContainsKey(playerId)) return null;

            int index = playerData[playerId].currentLoadoutIndex;
            return GetLoadout(playerId, index);
        }

        #endregion

        #region Perk Management

        public bool UnlockPerk(ulong playerId, string perkId)
        {
            if (!perkDefinitions.ContainsKey(perkId)) return false;

            if (!playerData.ContainsKey(playerId))
            {
                InitializePlayerData(playerId);
            }

            var data = playerData[playerId];

            if (data.unlockedPerks.Contains(perkId)) return false;

            data.unlockedPerks.Add(perkId);
            data.perkLevels[perkId] = 1;

            SavePlayerData(playerId);

            Debug.Log($"[LoadoutSystem] Player {playerId} unlocked perk: {perkId}");

            return true;
        }

        public bool UpgradePerk(ulong playerId, string perkId)
        {
            if (!perkDefinitions.ContainsKey(perkId)) return false;

            if (!playerData.ContainsKey(playerId)) return false;

            var data = playerData[playerId];
            var perk = perkDefinitions[perkId];

            if (!data.unlockedPerks.Contains(perkId)) return false;

            int currentLevel = data.perkLevels[perkId];

            if (currentLevel >= perk.maxLevel) return false;

            data.perkLevels[perkId] = currentLevel + 1;

            SavePlayerData(playerId);

            Debug.Log($"[LoadoutSystem] Player {playerId} upgraded perk {perkId} to level {currentLevel + 1}");

            return true;
        }

        public List<PerkDefinition> GetUnlockedPerks(ulong playerId)
        {
            if (!playerData.ContainsKey(playerId)) return new List<PerkDefinition>();

            return playerData[playerId].unlockedPerks
                .Select(id => perkDefinitions[id])
                .ToList();
        }

        public int GetPerkLevel(ulong playerId, string perkId)
        {
            if (!playerData.ContainsKey(playerId)) return 0;

            return playerData[playerId].perkLevels.ContainsKey(perkId) ? playerData[playerId].perkLevels[perkId] : 0;
        }

        #endregion

        #region Active Loadout (Match)

        public void ActivateLoadout(ulong playerId)
        {
            var preset = GetCurrentLoadout(playerId);

            if (preset == null) return;

            var activeLoadout = new ActiveLoadout
            {
                playerId = playerId,
                preset = preset,
                activationTime = DateTime.UtcNow,
                activeEffects = new List<PerkEffect>()
            };

            // Collect all perk effects
            foreach (var perkId in preset.perks)
            {
                if (!perkDefinitions.ContainsKey(perkId)) continue;

                var perk = perkDefinitions[perkId];
                int level = GetPerkLevel(playerId, perkId);

                foreach (var effect in perk.effects)
                {
                    var activeEffect = new PerkEffect
                    {
                        type = effect.type,
                        value = effect.value + (effect.perLevel * (level - 1)),
                        source = perkId
                    };

                    activeLoadout.activeEffects.Add(activeEffect);
                }
            }

            activeLoadouts[playerId] = activeLoadout;

            Debug.Log($"[LoadoutSystem] Activated loadout for player {playerId} with {activeLoadout.activeEffects.Count} effects");
        }

        public void DeactivateLoadout(ulong playerId)
        {
            activeLoadouts.Remove(playerId);

            Debug.Log($"[LoadoutSystem] Deactivated loadout for player {playerId}");
        }

        public float GetPerkEffectValue(ulong playerId, EffectType effectType)
        {
            if (!activeLoadouts.ContainsKey(playerId)) return 0f;

            var activeLoadout = activeLoadouts[playerId];

            return activeLoadout.activeEffects
                .Where(e => e.type == effectType)
                .Sum(e => e.value);
        }

        public bool HasPerkEffect(ulong playerId, EffectType effectType)
        {
            if (!activeLoadouts.ContainsKey(playerId)) return false;

            return activeLoadouts[playerId].activeEffects.Any(e => e.type == effectType);
        }

        #endregion

        #region Queries

        public List<PerkDefinition> GetPerksByCategory(PerkCategory category)
        {
            return perkDefinitions.Values.Where(p => p.category == category).ToList();
        }

        public List<PerkDefinition> GetPerksByTier(PerkTier tier)
        {
            return perkDefinitions.Values.Where(p => p.tier == tier).ToList();
        }

        public PerkDefinition GetPerk(string perkId)
        {
            return perkDefinitions.ContainsKey(perkId) ? perkDefinitions[perkId] : null;
        }

        #endregion

        #region Data Persistence

        private void SavePlayerData(ulong playerId)
        {
            if (!playerData.ContainsKey(playerId)) return;

            var data = playerData[playerId];
            string json = JsonUtility.ToJson(data);

            SaveSystem.SaveManager.Instance?.SaveData($"loadout_{playerId}", json);
        }

        public void LoadPlayerData(ulong playerId)
        {
            string json = SaveSystem.SaveManager.Instance?.LoadData($"loadout_{playerId}");

            if (!string.IsNullOrEmpty(json))
            {
                var data = JsonUtility.FromJson<PlayerLoadoutData>(json);
                playerData[playerId] = data;

                Debug.Log($"[LoadoutSystem] Loaded loadout data for player {playerId}");
            }
        }

        #endregion
    }

    #region Data Classes

    [Serializable]
    public class PerkDefinition
    {
        public string perkId;
        public string perkName;
        public string description;
        public PerkCategory category;
        public PerkTier tier;
        public int cost; // Perk points required
        public int maxLevel;
        public List<PerkEffect> effects = new List<PerkEffect>();
        public List<string> synergiesWithPerks; // Perk IDs that synergize
    }

    [Serializable]
    public class PerkEffect
    {
        public EffectType type;
        public float value;
        public float perLevel; // Additional value per perk level
        public string source; // Perk ID that provides this effect
    }

    [Serializable]
    public class LoadoutPreset
    {
        public string loadoutName;
        public List<string> weapons = new List<string>(); // Weapon IDs
        public List<string> equipment = new List<string>(); // Equipment IDs
        public List<string> perks = new List<string>(); // Perk IDs
    }

    [Serializable]
    public class PlayerLoadoutData
    {
        public ulong playerId;
        public List<LoadoutPreset> loadouts = new List<LoadoutPreset>();
        public List<string> unlockedPerks = new List<string>();
        public Dictionary<string, int> perkLevels = new Dictionary<string, int>();
        public int currentLoadoutIndex;
    }

    [Serializable]
    public class ActiveLoadout
    {
        public ulong playerId;
        public LoadoutPreset preset;
        public DateTime activationTime;
        public List<PerkEffect> activeEffects = new List<PerkEffect>();
    }

    public enum PerkCategory
    {
        Combat,
        Survival,
        Mobility,
        Utility,
        Team
    }

    public enum PerkTier
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    public enum EffectType
    {
        // Combat
        ReloadSpeed,
        Accuracy,
        BreathDuration,
        WeaponDamage,
        CriticalChance,

        // Survival
        MaxHealth,
        HealingRate,
        SecondChance,
        DamageResistance,

        // Mobility
        SprintSpeed,
        MovementSpeed,
        StaminaDrain,
        ClimbSpeed,
        JumpHeight,

        // Utility
        LootChance,
        NoiseReduction,
        DetectionRange,
        XPGain,
        CurrencyGain,

        // Team
        ReviveSpeed,
        TeamDamageBoost,
        TeamHealthBoost
    }

    #endregion
}
