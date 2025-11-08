using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DeadFrontier.Data
{
    /// <summary>
    /// Manages ScriptableObject data presets for all game content
    /// Provides centralized access to weapons, zombies, achievements, etc.
    /// </summary>
    public class DataPresetsManager : Singleton<DataPresetsManager>
    {
        [Header("Weapon Presets")]
        [SerializeField] private List<Weapons.WeaponData> weaponPresets = new List<Weapons.WeaponData>();

        [Header("Zombie Presets")]
        [SerializeField] private List<Zombies.ZombieConfiguration> zombiePresets = new List<Zombies.ZombieConfiguration>();

        [Header("Achievement Presets")]
        [SerializeField] private List<Core.Achievements.AchievementData> achievementPresets = new List<Core.Achievements.AchievementData>();

        [Header("Perk Presets")]
        [SerializeField] private List<Core.Progression.Perks.PerkData> perkPresets = new List<Core.Progression.Perks.PerkData>();

        [Header("Challenge Presets")]
        [SerializeField] private List<Core.Progression.Challenges.ChallengeData> challengePresets = new List<Core.Progression.Challenges.ChallengeData>();

        [Header("Battle Pass Presets")]
        [SerializeField] private List<Core.Progression.BattlePass.BattlePassTier> battlePassPresets = new List<Core.Progression.BattlePass.BattlePassTier>();

        [Header("Map Presets")]
        [SerializeField] private List<Networking.MapData> mapPresets = new List<Networking.MapData>();

        [Header("Game Mode Presets")]
        [SerializeField] private List<Networking.GameModeData> gameModePresets = new List<Networking.GameModeData>();

        [Header("Cosmetic Presets")]
        [SerializeField] private List<Player.CosmeticItemData> cosmeticPresets = new List<Player.CosmeticItemData>();

        [Header("Shop Item Presets")]
        [SerializeField] private List<Core.Economy.ShopItemData> shopItemPresets = new List<Core.Economy.ShopItemData>();

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;
        [SerializeField] private bool autoLoadPresetsOnStart = true;

        // Cached dictionaries for fast lookup
        private Dictionary<string, Weapons.WeaponData> weaponCache = new Dictionary<string, Weapons.WeaponData>();
        private Dictionary<string, Zombies.ZombieConfiguration> zombieCache = new Dictionary<string, Zombies.ZombieConfiguration>();
        private Dictionary<string, Core.Achievements.AchievementData> achievementCache = new Dictionary<string, Core.Achievements.AchievementData>();
        private Dictionary<string, Core.Progression.Perks.PerkData> perkCache = new Dictionary<string, Core.Progression.Perks.PerkData>();

        protected override void Awake()
        {
            base.Awake();

            if (autoLoadPresetsOnStart)
            {
                LoadAllPresets();
            }
        }

        #region Loading

        /// <summary>
        /// Loads all data presets into cache
        /// </summary>
        public void LoadAllPresets()
        {
            LoadWeaponPresets();
            LoadZombiePresets();
            LoadAchievementPresets();
            LoadPerkPresets();

            if (showDebugLogs)
            {
                Debug.Log($"[DataPresetsManager] Loaded all presets:\n" +
                    $"  Weapons: {weaponPresets.Count}\n" +
                    $"  Zombies: {zombiePresets.Count}\n" +
                    $"  Achievements: {achievementPresets.Count}\n" +
                    $"  Perks: {perkPresets.Count}\n" +
                    $"  Challenges: {challengePresets.Count}\n" +
                    $"  Battle Pass: {battlePassPresets.Count}\n" +
                    $"  Maps: {mapPresets.Count}\n" +
                    $"  Game Modes: {gameModePresets.Count}\n" +
                    $"  Cosmetics: {cosmeticPresets.Count}\n" +
                    $"  Shop Items: {shopItemPresets.Count}");
            }
        }

        private void LoadWeaponPresets()
        {
            weaponCache.Clear();
            foreach (var weapon in weaponPresets)
            {
                if (weapon != null && !string.IsNullOrEmpty(weapon.weaponName))
                {
                    weaponCache[weapon.weaponName] = weapon;
                }
            }
        }

        private void LoadZombiePresets()
        {
            zombieCache.Clear();
            foreach (var zombie in zombiePresets)
            {
                if (zombie != null)
                {
                    zombieCache[zombie.zombieType.ToString()] = zombie;
                }
            }
        }

        private void LoadAchievementPresets()
        {
            achievementCache.Clear();
            foreach (var achievement in achievementPresets)
            {
                if (achievement != null && !string.IsNullOrEmpty(achievement.achievementID))
                {
                    achievementCache[achievement.achievementID] = achievement;
                }
            }
        }

        private void LoadPerkPresets()
        {
            perkCache.Clear();
            foreach (var perk in perkPresets)
            {
                if (perk != null && !string.IsNullOrEmpty(perk.perkId))
                {
                    perkCache[perk.perkId] = perk;
                }
            }
        }

        #endregion

        #region Weapon Queries

        public Weapons.WeaponData GetWeapon(string weaponName)
        {
            if (weaponCache.ContainsKey(weaponName))
                return weaponCache[weaponName];

            if (showDebugLogs)
                Debug.LogWarning($"[DataPresetsManager] Weapon not found: {weaponName}");
            return null;
        }

        public List<Weapons.WeaponData> GetAllWeapons()
        {
            return new List<Weapons.WeaponData>(weaponPresets);
        }

        public List<Weapons.WeaponData> GetWeaponsByType(Weapons.WeaponType type)
        {
            return weaponPresets.FindAll(w => w != null && w.weaponType == type);
        }

        #endregion

        #region Zombie Queries

        public Zombies.ZombieConfiguration GetZombieConfig(Zombies.ZombieType type)
        {
            string key = type.ToString();
            if (zombieCache.ContainsKey(key))
                return zombieCache[key];

            if (showDebugLogs)
                Debug.LogWarning($"[DataPresetsManager] Zombie config not found: {type}");
            return null;
        }

        public List<Zombies.ZombieConfiguration> GetAllZombieConfigs()
        {
            return new List<Zombies.ZombieConfiguration>(zombiePresets);
        }

        #endregion

        #region Achievement Queries

        public Core.Achievements.AchievementData GetAchievement(string achievementId)
        {
            if (achievementCache.ContainsKey(achievementId))
                return achievementCache[achievementId];

            if (showDebugLogs)
                Debug.LogWarning($"[DataPresetsManager] Achievement not found: {achievementId}");
            return null;
        }

        public List<Core.Achievements.AchievementData> GetAllAchievements()
        {
            return new List<Core.Achievements.AchievementData>(achievementPresets);
        }

        public List<Core.Achievements.AchievementData> GetAchievementsByCategory(Core.Achievements.AchievementCategory category)
        {
            return achievementPresets.FindAll(a => a != null && a.category == category);
        }

        #endregion

        #region Perk Queries

        public Core.Progression.Perks.PerkData GetPerk(string perkId)
        {
            if (perkCache.ContainsKey(perkId))
                return perkCache[perkId];

            if (showDebugLogs)
                Debug.LogWarning($"[DataPresetsManager] Perk not found: {perkId}");
            return null;
        }

        public List<Core.Progression.Perks.PerkData> GetAllPerks()
        {
            return new List<Core.Progression.Perks.PerkData>(perkPresets);
        }

        #endregion

        #region Challenge Queries

        public List<Core.Progression.Challenges.ChallengeData> GetAllChallenges()
        {
            return new List<Core.Progression.Challenges.ChallengeData>(challengePresets);
        }

        public List<Core.Progression.Challenges.ChallengeData> GetChallengesByType(Core.Progression.Challenges.ChallengeType type)
        {
            return challengePresets.FindAll(c => c != null && c.challengeType == type);
        }

        #endregion

        #region Battle Pass Queries

        public List<Core.Progression.BattlePass.BattlePassTier> GetAllBattlePassTiers()
        {
            return new List<Core.Progression.BattlePass.BattlePassTier>(battlePassPresets);
        }

        public Core.Progression.BattlePass.BattlePassTier GetBattlePassTier(int tier)
        {
            return battlePassPresets.Find(t => t != null && t.tier == tier);
        }

        #endregion

        #region Map Queries

        public List<Networking.MapData> GetAllMaps()
        {
            return new List<Networking.MapData>(mapPresets);
        }

        public Networking.MapData GetMap(string mapId)
        {
            return mapPresets.Find(m => m != null && m.mapId == mapId);
        }

        #endregion

        #region Game Mode Queries

        public List<Networking.GameModeData> GetAllGameModes()
        {
            return new List<Networking.GameModeData>(gameModePresets);
        }

        public Networking.GameModeData GetGameMode(string modeId)
        {
            return gameModePresets.Find(m => m != null && m.modeId == modeId);
        }

        #endregion

        #region Cosmetic Queries

        public List<Player.CosmeticItemData> GetAllCosmetics()
        {
            return new List<Player.CosmeticItemData>(cosmeticPresets);
        }

        public Player.CosmeticItemData GetCosmetic(string cosmeticId)
        {
            return cosmeticPresets.Find(c => c != null && c.itemId == cosmeticId);
        }

        public List<Player.CosmeticItemData> GetCosmeticsByType(Player.CosmeticType type)
        {
            return cosmeticPresets.FindAll(c => c != null && c.type == type);
        }

        #endregion

        #region Shop Item Queries

        public List<Core.Economy.ShopItemData> GetAllShopItems()
        {
            return new List<Core.Economy.ShopItemData>(shopItemPresets);
        }

        public Core.Economy.ShopItemData GetShopItem(string itemId)
        {
            return shopItemPresets.Find(i => i != null && i.itemId == itemId);
        }

        public List<Core.Economy.ShopItemData> GetShopItemsByCategory(Core.Economy.ShopCategory category)
        {
            return shopItemPresets.FindAll(i => i != null && i.category == category);
        }

        #endregion

        #region Editor Tools

#if UNITY_EDITOR
        [ContextMenu("Auto-Load All Presets from Resources")]
        public void AutoLoadPresetsFromResources()
        {
            weaponPresets.Clear();
            zombiePresets.Clear();
            achievementPresets.Clear();
            perkPresets.Clear();
            challengePresets.Clear();
            battlePassPresets.Clear();
            mapPresets.Clear();
            gameModePresets.Clear();
            cosmeticPresets.Clear();
            shopItemPresets.Clear();

            // Load weapons
            var weapons = Resources.LoadAll<Weapons.WeaponData>("Data/Weapons");
            weaponPresets.AddRange(weapons);

            // Load zombies
            var zombies = Resources.LoadAll<Zombies.ZombieConfiguration>("Data/Zombies");
            zombiePresets.AddRange(zombies);

            // Load achievements
            var achievements = Resources.LoadAll<Core.Achievements.AchievementData>("Data/Achievements");
            achievementPresets.AddRange(achievements);

            // Load perks
            var perks = Resources.LoadAll<Core.Progression.Perks.PerkData>("Data/Perks");
            perkPresets.AddRange(perks);

            // Load challenges
            var challenges = Resources.LoadAll<Core.Progression.Challenges.ChallengeData>("Data/Challenges");
            challengePresets.AddRange(challenges);

            // Load battle pass
            var battlePass = Resources.LoadAll<Core.Progression.BattlePass.BattlePassTier>("Data/BattlePass");
            battlePassPresets.AddRange(battlePass);

            // Load maps
            var maps = Resources.LoadAll<Networking.MapData>("Data/Maps");
            mapPresets.AddRange(maps);

            // Load game modes
            var modes = Resources.LoadAll<Networking.GameModeData>("Data/GameModes");
            gameModePresets.AddRange(modes);

            // Load cosmetics
            var cosmetics = Resources.LoadAll<Player.CosmeticItemData>("Data/Cosmetics");
            cosmeticPresets.AddRange(cosmetics);

            // Load shop items
            var shopItems = Resources.LoadAll<Core.Economy.ShopItemData>("Data/Shop");
            shopItemPresets.AddRange(shopItems);

            EditorUtility.SetDirty(this);
            Debug.Log("[DataPresetsManager] Auto-loaded all presets from Resources");
        }

        [ContextMenu("Validate All Presets")]
        public void ValidateAllPresets()
        {
            int errors = 0;
            int warnings = 0;

            // Validate weapons
            foreach (var weapon in weaponPresets)
            {
                if (weapon == null)
                {
                    Debug.LogError("[DataPresetsManager] Null weapon preset found");
                    errors++;
                    continue;
                }

                if (string.IsNullOrEmpty(weapon.weaponName))
                {
                    Debug.LogWarning($"[DataPresetsManager] Weapon has no name: {weapon.name}");
                    warnings++;
                }

                if (weapon.damage <= 0)
                {
                    Debug.LogWarning($"[DataPresetsManager] Weapon has invalid damage: {weapon.weaponName}");
                    warnings++;
                }
            }

            // Validate achievements
            foreach (var achievement in achievementPresets)
            {
                if (achievement == null)
                {
                    Debug.LogError("[DataPresetsManager] Null achievement preset found");
                    errors++;
                    continue;
                }

                if (string.IsNullOrEmpty(achievement.achievementID))
                {
                    Debug.LogWarning($"[DataPresetsManager] Achievement has no ID: {achievement.name}");
                    warnings++;
                }
            }

            Debug.Log($"[DataPresetsManager] Validation complete. Errors: {errors}, Warnings: {warnings}");
        }

        [ContextMenu("Generate Missing Presets")]
        public void GenerateMissingPresets()
        {
            // Create example presets if none exist
            if (weaponPresets.Count == 0)
            {
                Debug.Log("[DataPresetsManager] No weapon presets found. Create them in Resources/Data/Weapons/");
            }

            if (zombiePresets.Count == 0)
            {
                Debug.Log("[DataPresetsManager] No zombie presets found. Create them in Resources/Data/Zombies/");
            }

            // Could auto-generate default presets here
        }
#endif

        #endregion
    }
}
