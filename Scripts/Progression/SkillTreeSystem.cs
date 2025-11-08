using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace DeadFrontier.Progression
{
    /// <summary>
    /// Comprehensive skill tree system with multiple specializations and talent paths.
    /// Provides deep progression and build customization for players.
    /// </summary>
    public class SkillTreeSystem : MonoBehaviour
    {
        public static SkillTreeSystem Instance { get; private set; }

        [Header("Progression Settings")]
        [SerializeField] private int maxSkillPoints = 100;
        [SerializeField] private int skillPointsPerLevel = 1;
        [SerializeField] private bool allowRespec = true;
        [SerializeField] private int respecCost = 10000;

        [Header("Skill Trees")]
        [SerializeField] private SkillTreeData[] skillTrees;

        [Header("Audio")]
        [SerializeField] private AudioClip skillUnlockedSound;
        [SerializeField] private AudioClip respecSound;

        // Player skill state
        private Dictionary<string, PlayerSkillTree> playerSkillData = new Dictionary<string, PlayerSkillTree>();
        private Dictionary<string, int> unlockedSkills = new Dictionary<string, int>(); // Skill ID -> Level
        private int availableSkillPoints;
        private int totalSkillPoints;

        // Active bonuses
        private Dictionary<SkillBonusType, float> activeBonuses = new Dictionary<SkillBonusType, float>();

        // Events
        public event Action<SkillData, int> OnSkillUnlocked; // Skill, Level
        public event Action<int> OnSkillPointsChanged;
        public event Action OnSkillsReset;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            InitializeSkillTrees();
            LoadPlayerSkills();
        }

        #region Initialization

        private void InitializeSkillTrees()
        {
            // Initialize skill tree data
            CreateCombatTree();
            CreateSurvivalTree();
            CreateSupportTree();
            CreateScavengerTree();

            Debug.Log($"[SkillTreeSystem] Initialized {skillTrees.Length} skill trees");
        }

        private void CreateCombatTree()
        {
            // Combat specialization tree
            var combatTree = new SkillTreeData
            {
                treeId = "tree_combat",
                treeName = "Combat",
                description = "Weapon damage, accuracy, and combat effectiveness",
                icon = null,
                skills = new SkillData[]
                {
                    CreateSkill("combat_damage1", "Weapon Damage I", "Increase weapon damage by 5%", SkillBonusType.DamageMultiplier, 0.05f, 0, 5),
                    CreateSkill("combat_damage2", "Weapon Damage II", "Increase weapon damage by 10%", SkillBonusType.DamageMultiplier, 0.10f, 1, 5, "combat_damage1"),
                    CreateSkill("combat_damage3", "Weapon Damage III", "Increase weapon damage by 15%", SkillBonusType.DamageMultiplier, 0.15f, 2, 5, "combat_damage2"),

                    CreateSkill("combat_accuracy", "Steady Aim", "Reduce weapon spread by 20%", SkillBonusType.AccuracyMultiplier, 0.20f, 0, 3),
                    CreateSkill("combat_recoil", "Recoil Control", "Reduce recoil by 25%", SkillBonusType.RecoilReduction, 0.25f, 0, 3, "combat_accuracy"),

                    CreateSkill("combat_reload", "Quick Hands", "Increase reload speed by 15%", SkillBonusType.ReloadSpeed, 0.15f, 0, 3),
                    CreateSkill("combat_ads", "Snap Aim", "Increase ADS speed by 20%", SkillBonusType.ADSSpeed, 0.20f, 0, 3),

                    CreateSkill("combat_crit", "Critical Training", "5% chance for critical hits (2x damage)", SkillBonusType.CriticalChance, 0.05f, 1, 1),
                    CreateSkill("combat_headshot", "Marksman", "Increase headshot damage by 25%", SkillBonusType.HeadshotDamage, 0.25f, 1, 1, "combat_crit")
                }
            };
        }

        private void CreateSurvivalTree()
        {
            var survivalTree = new SkillTreeData
            {
                treeId = "tree_survival",
                treeName = "Survival",
                description = "Health, stamina, and survivability improvements",
                skills = new SkillData[]
                {
                    CreateSkill("survival_health1", "Tough I", "Increase max health by 10%", SkillBonusType.MaxHealth, 0.10f, 0, 5),
                    CreateSkill("survival_health2", "Tough II", "Increase max health by 20%", SkillBonusType.MaxHealth, 0.20f, 1, 5, "survival_health1"),
                    CreateSkill("survival_health3", "Tough III", "Increase max health by 30%", SkillBonusType.MaxHealth, 0.30f, 2, 5, "survival_health2"),

                    CreateSkill("survival_regen", "Regeneration", "Slowly regenerate health out of combat", SkillBonusType.HealthRegen, 1f, 1, 1),
                    CreateSkill("survival_stamina", "Endurance", "Increase max stamina by 25%", SkillBonusType.MaxStamina, 0.25f, 0, 3),
                    CreateSkill("survival_speed", "Athlete", "Increase movement speed by 10%", SkillBonusType.MovementSpeed, 0.10f, 0, 3),

                    CreateSkill("survival_carry", "Pack Mule", "Increase carry capacity by 20%", SkillBonusType.CarryWeight, 0.20f, 0, 3),
                    CreateSkill("survival_armor", "Iron Skin", "Reduce incoming damage by 10%", SkillBonusType.DamageResistance, 0.10f, 1, 1)
                }
            };
        }

        private void CreateSupportTree()
        {
            var supportTree = new SkillTreeData
            {
                treeId = "tree_support",
                treeName = "Support",
                description = "Team healing, buffs, and utility abilities",
                skills = new SkillData[]
                {
                    CreateSkill("support_heal", "Field Medic", "Increase healing effectiveness by 25%", SkillBonusType.HealingMultiplier, 0.25f, 0, 5),
                    CreateSkill("support_revive", "Combat Medic", "Revive teammates 50% faster", SkillBonusType.ReviveSpeed, 0.50f, 0, 3, "support_heal"),

                    CreateSkill("support_share", "Team Player", "Share 10% of earned XP with nearby teammates", SkillBonusType.XPShare, 0.10f, 1, 1),
                    CreateSkill("support_aura", "Inspiring Presence", "Boost nearby allies' reload speed by 10%", SkillBonusType.TeamReloadSpeed, 0.10f, 1, 1),

                    CreateSkill("support_craft", "Craftsman", "Reduce crafting time by 25%", SkillBonusType.CraftingSpeed, 0.25f, 0, 3),
                    CreateSkill("support_repair", "Repair Expert", "Repair durability more efficiently", SkillBonusType.RepairEfficiency, 0.30f, 0, 3)
                }
            };
        }

        private void CreateScavengerTree()
        {
            var scavengerTree = new SkillTreeData
            {
                treeId = "tree_scavenger",
                treeName = "Scavenger",
                description = "Loot finding, resource gathering, and rewards",
                skills = new SkillData[]
                {
                    CreateSkill("scav_loot1", "Fortune Finder", "10% chance to find bonus loot", SkillBonusType.LootChance, 0.10f, 0, 5),
                    CreateSkill("scav_loot2", "Treasure Hunter", "20% chance to find bonus loot", SkillBonusType.LootChance, 0.20f, 1, 5, "scav_loot1"),

                    CreateSkill("scav_rarity", "Lucky", "Increase loot rarity by one tier 15% of the time", SkillBonusType.LootRarity, 0.15f, 1, 3),
                    CreateSkill("scav_currency", "Entrepreneur", "Earn 15% more soft currency", SkillBonusType.CurrencyMultiplier, 0.15f, 0, 5),

                    CreateSkill("scav_extraction", "Fast Extract", "Reduce extraction time by 20%", SkillBonusType.ExtractionSpeed, 0.20f, 1, 3),
                    CreateSkill("scav_stealth", "Shadow", "Reduce zombie detection range by 25%", SkillBonusType.StealthMultiplier, 0.25f, 1, 1)
                }
            };
        }

        private SkillData CreateSkill(string id, string name, string description, SkillBonusType bonusType, float bonusValue, int tier, int maxLevel, string prerequisite = null)
        {
            return new SkillData
            {
                skillId = id,
                skillName = name,
                description = description,
                bonusType = bonusType,
                bonusValue = bonusValue,
                tier = tier,
                maxLevel = maxLevel,
                prerequisiteSkillId = prerequisite
            };
        }

        private void LoadPlayerSkills()
        {
            // Load from save system
            if (Core.SaveSystem.Instance != null)
            {
                // Load saved skills
                string savedData = Core.SaveSystem.Instance.LoadData("player_skills");
                if (!string.IsNullOrEmpty(savedData))
                {
                    var skillsData = JsonUtility.FromJson<SavedSkillData>(savedData);
                    unlockedSkills = skillsData.skills.ToDictionary(s => s.skillId, s => s.level);
                    availableSkillPoints = skillsData.availablePoints;
                    totalSkillPoints = skillsData.totalPoints;

                    RecalculateBonuses();
                }
            }

            // Grant initial skill points based on player level
            if (AchievementManager.Instance != null)
            {
                int playerLevel = AchievementManager.Instance.GetPlayerLevel();
                int expectedPoints = playerLevel * skillPointsPerLevel;

                if (totalSkillPoints < expectedPoints)
                {
                    int pointsToGrant = expectedPoints - totalSkillPoints;
                    GrantSkillPoints(pointsToGrant);
                }
            }
        }

        #endregion

        #region Skill Management

        public bool CanUnlockSkill(string skillId)
        {
            var skill = FindSkill(skillId);
            if (skill == null) return false;

            // Check if already at max level
            int currentLevel = GetSkillLevel(skillId);
            if (currentLevel >= skill.maxLevel) return false;

            // Check if have skill points
            if (availableSkillPoints <= 0) return false;

            // Check prerequisite
            if (!string.IsNullOrEmpty(skill.prerequisiteSkillId))
            {
                int prereqLevel = GetSkillLevel(skill.prerequisiteSkillId);
                var prereqSkill = FindSkill(skill.prerequisiteSkillId);

                if (prereqSkill == null || prereqLevel < prereqSkill.maxLevel)
                    return false;
            }

            // Check tier requirements (must have certain skills in previous tiers)
            if (skill.tier > 0)
            {
                var tree = FindTreeForSkill(skillId);
                if (tree != null)
                {
                    int pointsInTree = GetPointsInTree(tree.treeId);
                    int requiredPoints = skill.tier * 5; // 5 points per tier

                    if (pointsInTree < requiredPoints)
                        return false;
                }
            }

            return true;
        }

        public bool UnlockSkill(string skillId)
        {
            if (!CanUnlockSkill(skillId)) return false;

            var skill = FindSkill(skillId);
            if (skill == null) return false;

            // Unlock skill
            int currentLevel = GetSkillLevel(skillId);
            int newLevel = currentLevel + 1;

            unlockedSkills[skillId] = newLevel;
            availableSkillPoints--;

            // Recalculate active bonuses
            RecalculateBonuses();

            // Save progress
            SavePlayerSkills();

            OnSkillUnlocked?.Invoke(skill, newLevel);
            OnSkillPointsChanged?.Invoke(availableSkillPoints);

            if (skillUnlockedSound != null)
                Core.AudioManager.Instance?.PlaySFX(skillUnlockedSound);

            Debug.Log($"[SkillTreeSystem] Unlocked {skill.skillName} (Level {newLevel})");

            return true;
        }

        public bool RespecSkills()
        {
            if (!allowRespec) return false;

            // Check cost
            if (Economy.EconomyManager.Instance != null)
            {
                if (!Economy.EconomyManager.Instance.CanAfford(respecCost))
                    return false;

                if (!Economy.EconomyManager.Instance.SpendSoftCurrency(respecCost, "Skill Respec"))
                    return false;
            }

            // Reset all skills
            int pointsToRefund = unlockedSkills.Values.Sum();
            unlockedSkills.Clear();
            availableSkillPoints += pointsToRefund;

            // Recalculate bonuses
            RecalculateBonuses();

            // Save
            SavePlayerSkills();

            OnSkillsReset?.Invoke();
            OnSkillPointsChanged?.Invoke(availableSkillPoints);

            if (respecSound != null)
                Core.AudioManager.Instance?.PlaySFX(respecSound);

            Debug.Log($"[SkillTreeSystem] Reset all skills, refunded {pointsToRefund} points");

            return true;
        }

        public void GrantSkillPoints(int amount)
        {
            availableSkillPoints += amount;
            totalSkillPoints += amount;

            OnSkillPointsChanged?.Invoke(availableSkillPoints);

            SavePlayerSkills();

            Debug.Log($"[SkillTreeSystem] Granted {amount} skill points");
        }

        #endregion

        #region Bonus Calculation

        private void RecalculateBonuses()
        {
            activeBonuses.Clear();

            foreach (var kvp in unlockedSkills)
            {
                var skill = FindSkill(kvp.Key);
                if (skill == null) continue;

                int level = kvp.Value;
                float totalBonus = skill.bonusValue * level;

                if (activeBonuses.ContainsKey(skill.bonusType))
                {
                    activeBonuses[skill.bonusType] += totalBonus;
                }
                else
                {
                    activeBonuses[skill.bonusType] = totalBonus;
                }
            }

            Debug.Log($"[SkillTreeSystem] Recalculated bonuses: {activeBonuses.Count} active");
        }

        public float GetBonus(SkillBonusType bonusType)
        {
            return activeBonuses.ContainsKey(bonusType) ? activeBonuses[bonusType] : 0f;
        }

        public Dictionary<SkillBonusType, float> GetAllBonuses()
        {
            return new Dictionary<SkillBonusType, float>(activeBonuses);
        }

        #endregion

        #region Helpers

        private SkillData FindSkill(string skillId)
        {
            foreach (var tree in skillTrees)
            {
                if (tree.skills == null) continue;

                foreach (var skill in tree.skills)
                {
                    if (skill.skillId == skillId)
                        return skill;
                }
            }
            return null;
        }

        private SkillTreeData FindTreeForSkill(string skillId)
        {
            foreach (var tree in skillTrees)
            {
                if (tree.skills == null) continue;

                if (tree.skills.Any(s => s.skillId == skillId))
                    return tree;
            }
            return null;
        }

        private int GetPointsInTree(string treeId)
        {
            var tree = skillTrees.FirstOrDefault(t => t.treeId == treeId);
            if (tree == null || tree.skills == null) return 0;

            int points = 0;
            foreach (var skill in tree.skills)
            {
                if (unlockedSkills.ContainsKey(skill.skillId))
                {
                    points += unlockedSkills[skill.skillId];
                }
            }

            return points;
        }

        private void SavePlayerSkills()
        {
            if (Core.SaveSystem.Instance == null) return;

            var savedData = new SavedSkillData
            {
                skills = unlockedSkills.Select(kvp => new SkillSaveEntry { skillId = kvp.Key, level = kvp.Value }).ToList(),
                availablePoints = availableSkillPoints,
                totalPoints = totalSkillPoints
            };

            string json = JsonUtility.ToJson(savedData);
            Core.SaveSystem.Instance.SaveData("player_skills", json);
        }

        #endregion

        #region Public Getters

        public int GetSkillLevel(string skillId)
        {
            return unlockedSkills.ContainsKey(skillId) ? unlockedSkills[skillId] : 0;
        }

        public int GetAvailableSkillPoints() => availableSkillPoints;

        public int GetTotalSkillPoints() => totalSkillPoints;

        public List<SkillTreeData> GetAllSkillTrees()
        {
            return new List<SkillTreeData>(skillTrees);
        }

        public List<SkillData> GetUnlockedSkills()
        {
            return unlockedSkills.Keys.Select(id => FindSkill(id)).Where(s => s != null).ToList();
        }

        #endregion
    }

    #region Data Classes

    [System.Serializable]
    public class SkillTreeData
    {
        public string treeId;
        public string treeName;
        [TextArea(2, 3)]
        public string description;
        public Sprite icon;
        public SkillData[] skills;
    }

    [System.Serializable]
    public class SkillData
    {
        public string skillId;
        public string skillName;
        [TextArea(2, 4)]
        public string description;

        public SkillBonusType bonusType;
        public float bonusValue;

        public int tier; // Skill tier (0-3)
        public int maxLevel = 1;
        public string prerequisiteSkillId;

        public Sprite icon;
    }

    [System.Serializable]
    public class SavedSkillData
    {
        public List<SkillSaveEntry> skills;
        public int availablePoints;
        public int totalPoints;
    }

    [System.Serializable]
    public class SkillSaveEntry
    {
        public string skillId;
        public int level;
    }

    [System.Serializable]
    public class PlayerSkillTree
    {
        public string treeId;
        public Dictionary<string, int> unlockedSkills;
        public int totalPoints;
    }

    public enum SkillBonusType
    {
        // Combat
        DamageMultiplier,
        AccuracyMultiplier,
        RecoilReduction,
        ReloadSpeed,
        ADSSpeed,
        CriticalChance,
        HeadshotDamage,

        // Survival
        MaxHealth,
        HealthRegen,
        MaxStamina,
        MovementSpeed,
        CarryWeight,
        DamageResistance,

        // Support
        HealingMultiplier,
        ReviveSpeed,
        XPShare,
        TeamReloadSpeed,
        CraftingSpeed,
        RepairEfficiency,

        // Scavenger
        LootChance,
        LootRarity,
        CurrencyMultiplier,
        ExtractionSpeed,
        StealthMultiplier
    }

    #endregion
}
