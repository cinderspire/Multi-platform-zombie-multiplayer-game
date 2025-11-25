using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace DeadFrontier.Data
{
    /// <summary>
    /// Central game database containing all game data configurations.
    /// Singleton that loads and provides access to all ScriptableObject data.
    /// </summary>
    public class GameDatabase : MonoBehaviour
    {
        public static GameDatabase Instance { get; private set; }

        [Header("Weapons")]
        [SerializeField] private Weapons.WeaponDataSO[] weapons;
        [SerializeField] private Weapons.AttachmentData[] attachments;

        [Header("Characters")]
        [SerializeField] private Characters.CharacterDataSO[] characters;

        [Header("Zombies")]
        [SerializeField] private AI.ZombieTypeData[] zombieTypes;

        [Header("Items")]
        [SerializeField] private Gameplay.LootItemDataSO[] lootItems;

        [Header("Perks")]
        [SerializeField] private Perks.PerkDataSO[] perks;

        [Header("Abilities")]
        [SerializeField] private Abilities.AbilityDataSO[] abilities;

        [Header("Quests")]
        [SerializeField] private Quests.QuestDataSO[] quests;

        [Header("Crafting")]
        [SerializeField] private Crafting.CraftingRecipeDataSO[] recipes;

        [Header("Maps")]
        [SerializeField] private Maps.MapDataSO[] maps;

        [Header("Achievements")]
        [SerializeField] private Achievements.AchievementDataSO[] achievements;

        [Header("Cosmetics")]
        [SerializeField] private Cosmetics.CosmeticDataSO[] cosmetics;

        [Header("Events")]
        [SerializeField] private Events.EventDataSO[] events;

        [Header("Shop")]
        [SerializeField] private Economy.ShopItemDataSO[] shopItems;

        [Header("Battle Pass")]
        [SerializeField] private BattlePass.BattlePassDataSO[] battlePasses;

        [Header("Companions")]
        [SerializeField] private Companions.CompanionDataSO[] companions;

        [Header("Vehicles")]
        [SerializeField] private Vehicles.VehicleDataSO[] vehicles;

        [Header("Skills")]
        [SerializeField] private Skills.SkillTreeDataSO[] skillTrees;
        [SerializeField] private Skills.SkillDataSO[] skills;

        [Header("Dialogue")]
        [SerializeField] private Dialogue.DialogueDataSO[] dialogues;

        // Dictionaries for fast lookup
        private Dictionary<string, Weapons.WeaponDataSO> _weaponLookup;
        private Dictionary<string, Weapons.AttachmentData> _attachmentLookup;
        private Dictionary<string, Characters.CharacterDataSO> _characterLookup;
        private Dictionary<string, AI.ZombieTypeData> _zombieLookup;
        private Dictionary<string, Gameplay.LootItemDataSO> _itemLookup;
        private Dictionary<string, Perks.PerkDataSO> _perkLookup;
        private Dictionary<string, Abilities.AbilityDataSO> _abilityLookup;
        private Dictionary<string, Quests.QuestDataSO> _questLookup;
        private Dictionary<string, Crafting.CraftingRecipeDataSO> _recipeLookup;
        private Dictionary<string, Maps.MapDataSO> _mapLookup;
        private Dictionary<string, Achievements.AchievementDataSO> _achievementLookup;
        private Dictionary<string, Cosmetics.CosmeticDataSO> _cosmeticLookup;
        private Dictionary<string, Events.EventDataSO> _eventLookup;
        private Dictionary<string, Economy.ShopItemDataSO> _shopItemLookup;
        private Dictionary<string, Companions.CompanionDataSO> _companionLookup;
        private Dictionary<string, Vehicles.VehicleDataSO> _vehicleLookup;
        private Dictionary<string, Skills.SkillTreeDataSO> _skillTreeLookup;
        private Dictionary<string, Skills.SkillDataSO> _skillLookup;
        private Dictionary<string, Dialogue.DialogueDataSO> _dialogueLookup;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeLookups();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeLookups()
        {
            _weaponLookup = weapons?.ToDictionary(w => w.weaponId, w => w) ?? new Dictionary<string, Weapons.WeaponDataSO>();
            _attachmentLookup = attachments?.ToDictionary(a => a.attachmentId, a => a) ?? new Dictionary<string, Weapons.AttachmentData>();
            _characterLookup = characters?.ToDictionary(c => c.characterId, c => c) ?? new Dictionary<string, Characters.CharacterDataSO>();
            _zombieLookup = zombieTypes?.ToDictionary(z => z.zombieId, z => z) ?? new Dictionary<string, AI.ZombieTypeData>();
            _itemLookup = lootItems?.ToDictionary(i => i.itemId, i => i) ?? new Dictionary<string, Gameplay.LootItemDataSO>();
            _perkLookup = perks?.ToDictionary(p => p.perkId, p => p) ?? new Dictionary<string, Perks.PerkDataSO>();
            _abilityLookup = abilities?.ToDictionary(a => a.abilityId, a => a) ?? new Dictionary<string, Abilities.AbilityDataSO>();
            _questLookup = quests?.ToDictionary(q => q.questId, q => q) ?? new Dictionary<string, Quests.QuestDataSO>();
            _recipeLookup = recipes?.ToDictionary(r => r.recipeId, r => r) ?? new Dictionary<string, Crafting.CraftingRecipeDataSO>();
            _mapLookup = maps?.ToDictionary(m => m.mapId, m => m) ?? new Dictionary<string, Maps.MapDataSO>();
            _achievementLookup = achievements?.ToDictionary(a => a.achievementId, a => a) ?? new Dictionary<string, Achievements.AchievementDataSO>();
            _cosmeticLookup = cosmetics?.ToDictionary(c => c.cosmeticId, c => c) ?? new Dictionary<string, Cosmetics.CosmeticDataSO>();
            _eventLookup = events?.ToDictionary(e => e.eventId, e => e) ?? new Dictionary<string, Events.EventDataSO>();
            _shopItemLookup = shopItems?.ToDictionary(s => s.shopItemId, s => s) ?? new Dictionary<string, Economy.ShopItemDataSO>();
            _companionLookup = companions?.ToDictionary(c => c.companionId, c => c) ?? new Dictionary<string, Companions.CompanionDataSO>();
            _vehicleLookup = vehicles?.ToDictionary(v => v.vehicleId, v => v) ?? new Dictionary<string, Vehicles.VehicleDataSO>();
            _skillTreeLookup = skillTrees?.ToDictionary(s => s.skillTreeId, s => s) ?? new Dictionary<string, Skills.SkillTreeDataSO>();
            _skillLookup = skills?.ToDictionary(s => s.skillId, s => s) ?? new Dictionary<string, Skills.SkillDataSO>();
            _dialogueLookup = dialogues?.ToDictionary(d => d.dialogueId, d => d) ?? new Dictionary<string, Dialogue.DialogueDataSO>();

            Debug.Log($"[GameDatabase] Initialized with {GetTotalCount()} entries");
        }

        private int GetTotalCount()
        {
            return _weaponLookup.Count + _attachmentLookup.Count + _characterLookup.Count +
                   _zombieLookup.Count + _itemLookup.Count + _perkLookup.Count +
                   _abilityLookup.Count + _questLookup.Count + _recipeLookup.Count +
                   _mapLookup.Count + _achievementLookup.Count + _cosmeticLookup.Count +
                   _eventLookup.Count + _shopItemLookup.Count + _companionLookup.Count +
                   _vehicleLookup.Count + _skillTreeLookup.Count + _skillLookup.Count +
                   _dialogueLookup.Count;
        }

        // Weapon accessors
        public Weapons.WeaponDataSO GetWeapon(string id) => _weaponLookup.TryGetValue(id, out var w) ? w : null;
        public IEnumerable<Weapons.WeaponDataSO> GetAllWeapons() => weapons ?? System.Array.Empty<Weapons.WeaponDataSO>();
        public IEnumerable<Weapons.WeaponDataSO> GetWeaponsByType(Weapons.WeaponType type) => GetAllWeapons().Where(w => w.weaponType == type);

        public Weapons.AttachmentData GetAttachment(string id) => _attachmentLookup.TryGetValue(id, out var a) ? a : null;
        public IEnumerable<Weapons.AttachmentData> GetAllAttachments() => attachments ?? System.Array.Empty<Weapons.AttachmentData>();

        // Character accessors
        public Characters.CharacterDataSO GetCharacter(string id) => _characterLookup.TryGetValue(id, out var c) ? c : null;
        public IEnumerable<Characters.CharacterDataSO> GetAllCharacters() => characters ?? System.Array.Empty<Characters.CharacterDataSO>();

        // Zombie accessors
        public AI.ZombieTypeData GetZombieType(string id) => _zombieLookup.TryGetValue(id, out var z) ? z : null;
        public IEnumerable<AI.ZombieTypeData> GetAllZombieTypes() => zombieTypes ?? System.Array.Empty<AI.ZombieTypeData>();

        // Item accessors
        public Gameplay.LootItemDataSO GetItem(string id) => _itemLookup.TryGetValue(id, out var i) ? i : null;
        public IEnumerable<Gameplay.LootItemDataSO> GetAllItems() => lootItems ?? System.Array.Empty<Gameplay.LootItemDataSO>();
        public IEnumerable<Gameplay.LootItemDataSO> GetItemsByCategory(Gameplay.ItemCategory category) => GetAllItems().Where(i => i.category == category);

        // Perk accessors
        public Perks.PerkDataSO GetPerk(string id) => _perkLookup.TryGetValue(id, out var p) ? p : null;
        public IEnumerable<Perks.PerkDataSO> GetAllPerks() => perks ?? System.Array.Empty<Perks.PerkDataSO>();
        public IEnumerable<Perks.PerkDataSO> GetPerksByCategory(Perks.PerkCategory category) => GetAllPerks().Where(p => p.category == category);

        // Ability accessors
        public Abilities.AbilityDataSO GetAbility(string id) => _abilityLookup.TryGetValue(id, out var a) ? a : null;
        public IEnumerable<Abilities.AbilityDataSO> GetAllAbilities() => abilities ?? System.Array.Empty<Abilities.AbilityDataSO>();

        // Quest accessors
        public Quests.QuestDataSO GetQuest(string id) => _questLookup.TryGetValue(id, out var q) ? q : null;
        public IEnumerable<Quests.QuestDataSO> GetAllQuests() => quests ?? System.Array.Empty<Quests.QuestDataSO>();
        public IEnumerable<Quests.QuestDataSO> GetQuestsByType(Quests.QuestType type) => GetAllQuests().Where(q => q.questType == type);

        // Recipe accessors
        public Crafting.CraftingRecipeDataSO GetRecipe(string id) => _recipeLookup.TryGetValue(id, out var r) ? r : null;
        public IEnumerable<Crafting.CraftingRecipeDataSO> GetAllRecipes() => recipes ?? System.Array.Empty<Crafting.CraftingRecipeDataSO>();

        // Map accessors
        public Maps.MapDataSO GetMap(string id) => _mapLookup.TryGetValue(id, out var m) ? m : null;
        public IEnumerable<Maps.MapDataSO> GetAllMaps() => maps ?? System.Array.Empty<Maps.MapDataSO>();

        // Achievement accessors
        public Achievements.AchievementDataSO GetAchievement(string id) => _achievementLookup.TryGetValue(id, out var a) ? a : null;
        public IEnumerable<Achievements.AchievementDataSO> GetAllAchievements() => achievements ?? System.Array.Empty<Achievements.AchievementDataSO>();

        // Cosmetic accessors
        public Cosmetics.CosmeticDataSO GetCosmetic(string id) => _cosmeticLookup.TryGetValue(id, out var c) ? c : null;
        public IEnumerable<Cosmetics.CosmeticDataSO> GetAllCosmetics() => cosmetics ?? System.Array.Empty<Cosmetics.CosmeticDataSO>();

        // Event accessors
        public Events.EventDataSO GetEvent(string id) => _eventLookup.TryGetValue(id, out var e) ? e : null;
        public IEnumerable<Events.EventDataSO> GetAllEvents() => events ?? System.Array.Empty<Events.EventDataSO>();

        // Shop accessors
        public Economy.ShopItemDataSO GetShopItem(string id) => _shopItemLookup.TryGetValue(id, out var s) ? s : null;
        public IEnumerable<Economy.ShopItemDataSO> GetAllShopItems() => shopItems ?? System.Array.Empty<Economy.ShopItemDataSO>();

        // Companion accessors
        public Companions.CompanionDataSO GetCompanion(string id) => _companionLookup.TryGetValue(id, out var c) ? c : null;
        public IEnumerable<Companions.CompanionDataSO> GetAllCompanions() => companions ?? System.Array.Empty<Companions.CompanionDataSO>();

        // Vehicle accessors
        public Vehicles.VehicleDataSO GetVehicle(string id) => _vehicleLookup.TryGetValue(id, out var v) ? v : null;
        public IEnumerable<Vehicles.VehicleDataSO> GetAllVehicles() => vehicles ?? System.Array.Empty<Vehicles.VehicleDataSO>();

        // Skill accessors
        public Skills.SkillTreeDataSO GetSkillTree(string id) => _skillTreeLookup.TryGetValue(id, out var s) ? s : null;
        public IEnumerable<Skills.SkillTreeDataSO> GetAllSkillTrees() => skillTrees ?? System.Array.Empty<Skills.SkillTreeDataSO>();
        public Skills.SkillDataSO GetSkill(string id) => _skillLookup.TryGetValue(id, out var s) ? s : null;
        public IEnumerable<Skills.SkillDataSO> GetAllSkills() => skills ?? System.Array.Empty<Skills.SkillDataSO>();

        // Dialogue accessors
        public Dialogue.DialogueDataSO GetDialogue(string id) => _dialogueLookup.TryGetValue(id, out var d) ? d : null;
        public IEnumerable<Dialogue.DialogueDataSO> GetAllDialogues() => dialogues ?? System.Array.Empty<Dialogue.DialogueDataSO>();

        // Battle Pass accessor
        public BattlePass.BattlePassDataSO GetCurrentBattlePass() => battlePasses?.FirstOrDefault();
        public BattlePass.BattlePassDataSO GetBattlePassBySeason(int season) => battlePasses?.FirstOrDefault(b => b.seasonNumber == season);
    }
}
