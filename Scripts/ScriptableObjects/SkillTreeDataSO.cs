using UnityEngine;
using System.Collections.Generic;

namespace DeadFrontier.Skills
{
    /// <summary>
    /// ScriptableObject defining skill tree structure and progression.
    /// </summary>
    [CreateAssetMenu(fileName = "New Skill Tree", menuName = "Dead Frontier/Skills/Skill Tree Data")]
    public class SkillTreeDataSO : ScriptableObject
    {
        [Header("Identification")]
        public string skillTreeId;
        public string skillTreeName;
        [TextArea(3, 5)]
        public string description;
        public SkillTreeCategory category;

        [Header("Visuals")]
        public Sprite skillTreeIcon;
        public Color treeColor;
        public Texture2D backgroundTexture;

        [Header("Structure")]
        public SkillNode[] nodes;
        public SkillConnection[] connections;
        public int maxTier = 5;

        [Header("Progression")]
        public int skillPointsPerLevel = 1;
        public int bonusPointsPerPrestige = 5;
        public bool allowRespec = true;
        public int respecCost = 1000;

        [Header("Requirements")]
        public int unlockLevel = 1;
        public string prerequisiteTreeId;
    }

    [System.Serializable]
    public class SkillNode
    {
        public string nodeId;
        public string nodeName;
        [TextArea(2, 4)]
        public string description;
        public SkillNodeType nodeType;
        public int tier;
        public Vector2 position;

        [Header("Visuals")]
        public Sprite nodeIcon;
        public Color nodeColor;

        [Header("Levels")]
        public int maxLevel = 5;
        public int pointsPerLevel = 1;
        public SkillNodeEffect[] effectsPerLevel;

        [Header("Requirements")]
        public int requiredTier = 0;
        public int requiredPointsInTree = 0;
        public string[] prerequisiteNodeIds;

        [Header("Networking")]
        public bool syncToServer = true;
    }

    [System.Serializable]
    public class SkillNodeEffect
    {
        public int level;
        public SkillEffectType effectType;
        public float value;
        public bool isPercentage;
        [TextArea(1, 2)]
        public string displayText;
    }

    [System.Serializable]
    public class SkillConnection
    {
        public string fromNodeId;
        public string toNodeId;
        public ConnectionType connectionType;
    }

    public enum SkillTreeCategory
    {
        Combat,
        Survival,
        Support,
        Stealth,
        Technical,
        Leadership
    }

    public enum SkillNodeType
    {
        Passive,
        Active,
        Keystone,
        Mastery
    }

    public enum SkillEffectType
    {
        // Combat
        WeaponDamage,
        CriticalChance,
        CriticalDamage,
        HeadshotDamage,
        FireRate,
        ReloadSpeed,
        Accuracy,
        RecoilReduction,
        MagazineSize,
        PenetrationDamage,

        // Survival
        MaxHealth,
        HealthRegen,
        DamageResistance,
        Armor,
        MaxStamina,
        StaminaRegen,
        HealingReceived,
        StatusResistance,

        // Movement
        MovementSpeed,
        SprintSpeed,
        JumpHeight,
        FallDamageReduction,
        ClimbSpeed,
        SwimSpeed,

        // Stealth
        NoiseReduction,
        DetectionRange,
        BackstabDamage,
        CrouchSpeed,
        TakedownSpeed,

        // Support
        TeamHealingBonus,
        ReviveSpeed,
        SharedXP,
        PingRange,
        ItemSharing,

        // Economy
        LootChance,
        LootQuality,
        XPGain,
        CurrencyGain,
        CraftingSpeed,
        CraftingQuality,

        // Special
        AbilityCooldown,
        AbilityDuration,
        AbilityRange,
        CompanionBonus,
        VehicleBonus
    }

    public enum ConnectionType
    {
        Required,   // Must unlock parent first
        Optional,   // Can unlock either path
        Exclusive   // Choosing this locks the other
    }

    /// <summary>
    /// Individual skill data for active skills.
    /// </summary>
    [CreateAssetMenu(fileName = "New Skill", menuName = "Dead Frontier/Skills/Skill Data")]
    public class SkillDataSO : ScriptableObject
    {
        [Header("Identification")]
        public string skillId;
        public string skillName;
        [TextArea(3, 5)]
        public string description;
        public SkillCategory skillCategory;

        [Header("Visuals")]
        public Sprite skillIcon;
        public Color skillColor;
        public GameObject skillEffectPrefab;

        [Header("Activation")]
        public SkillActivationType activationType;
        public float cooldown = 30f;
        public float duration = 10f;
        public float castTime = 0f;

        [Header("Cost")]
        public float staminaCost = 0f;
        public float healthCost = 0f;
        public int charges = 1;
        public float chargeRegenTime = 60f;

        [Header("Effects")]
        public SkillEffect[] effects;

        [Header("Targeting")]
        public SkillTargetType targetType;
        public float range = 10f;
        public float radius = 0f;

        [Header("Audio")]
        public AudioClip activationSound;
        public AudioClip loopSound;
        public AudioClip endSound;

        [Header("Progression")]
        public int unlockLevel = 1;
        public string skillTreeId;
        public string nodeId;
        public SkillUpgrade[] upgrades;
    }

    [System.Serializable]
    public class SkillEffect
    {
        public SkillEffectType effectType;
        public float baseValue;
        public float scalingPerLevel;
        public float duration;
        public bool affectsSelf = true;
        public bool affectsAllies;
        public bool affectsEnemies;
    }

    [System.Serializable]
    public class SkillUpgrade
    {
        public string upgradeId;
        public string upgradeName;
        public string description;
        public SkillUpgradeType upgradeType;
        public float value;
        public int cost;
        public int requiredLevel;
    }

    public enum SkillCategory
    {
        Offensive,
        Defensive,
        Utility,
        Movement,
        Support,
        Ultimate
    }

    public enum SkillActivationType
    {
        Instant,
        Toggle,
        Channeled,
        Charged,
        Passive
    }

    public enum SkillTargetType
    {
        Self,
        SingleEnemy,
        SingleAlly,
        AreaEnemy,
        AreaAlly,
        AreaAll,
        Cone,
        Line,
        Global
    }

    public enum SkillUpgradeType
    {
        CooldownReduction,
        DurationIncrease,
        RangeIncrease,
        RadiusIncrease,
        EffectIncrease,
        CostReduction,
        ChargeIncrease,
        NewEffect
    }
}
