using UnityEngine;

namespace DeadFrontier.Perks
{
    /// <summary>
    /// ScriptableObject defining perk data for the loadout system.
    /// </summary>
    [CreateAssetMenu(fileName = "New Perk", menuName = "Dead Frontier/Perks/Perk Data")]
    public class PerkDataSO : ScriptableObject
    {
        [Header("Identification")]
        public string perkId;
        public string perkName;
        [TextArea(3, 5)]
        public string description;
        public PerkCategory category;
        public PerkTier tier;

        [Header("Visuals")]
        public Sprite perkIcon;
        public Color perkColor = Color.white;
        public GameObject activationEffect;

        [Header("Effects")]
        public PerkEffect[] effects;

        [Header("Conditions")]
        public PerkCondition[] activationConditions;
        public bool isPassive = true;
        public float activeDuration = 0f;
        public float cooldown = 0f;

        [Header("Slot Requirements")]
        public int slotCost = 1;
        public int maxSlots = 4;
        public string[] incompatiblePerks;
        public string[] requiredPerks;

        [Header("Progression")]
        public int unlockLevel = 1;
        public int purchaseCost = 500;
        public bool isDefault;

        [Header("Pro/Con")]
        [TextArea(2, 3)]
        public string proText;
        [TextArea(2, 3)]
        public string conText;
    }

    [System.Serializable]
    public class PerkEffect
    {
        public PerkEffectType effectType;
        public float value;
        public float duration;
        public bool isPercentage;
        [TextArea(2, 3)]
        public string effectDescription;
    }

    [System.Serializable]
    public class PerkCondition
    {
        public PerkConditionType conditionType;
        public float threshold;
        public bool invertCondition;
    }

    public enum PerkCategory
    {
        Combat,         // Weapon and damage perks
        Survival,       // Health and defense perks
        Movement,       // Speed and mobility perks
        Stealth,        // Noise and detection perks
        Support,        // Team and healing perks
        Economy,        // Loot and money perks
        Utility         // Miscellaneous perks
    }

    public enum PerkTier
    {
        Basic,
        Advanced,
        Expert,
        Elite,
        Legendary
    }

    public enum PerkEffectType
    {
        // Combat
        DamageIncrease,
        CriticalChanceIncrease,
        CriticalDamageIncrease,
        HeadshotDamageIncrease,
        FireRateIncrease,
        ReloadSpeedIncrease,
        MagazineSizeIncrease,
        ArmorPenetration,

        // Survival
        MaxHealthIncrease,
        HealthRegeneration,
        DamageReduction,
        ArmorIncrease,
        BleedResistance,
        PoisonResistance,
        ExplosionResistance,

        // Movement
        MovementSpeedIncrease,
        SprintSpeedIncrease,
        StaminaIncrease,
        StaminaRegenIncrease,
        JumpHeightIncrease,
        FallDamageReduction,
        ADSSpeedIncrease,

        // Stealth
        NoiseReduction,
        DetectionRangeReduction,
        SilencerEfficiency,
        CrouchSpeedIncrease,

        // Support
        HealingGivenIncrease,
        HealingReceivedIncrease,
        ReviveSpeedIncrease,
        TeamAuraRange,

        // Economy
        LootChanceIncrease,
        LootQualityIncrease,
        XPGainIncrease,
        CurrencyGainIncrease,
        ItemCapacityIncrease,

        // Utility
        InteractionSpeedIncrease,
        RadarRange,
        TrapEfficiency,
        VehicleEfficiency
    }

    public enum PerkConditionType
    {
        Always,
        HealthBelow,
        HealthAbove,
        StaminaBelow,
        StaminaAbove,
        Sprinting,
        Crouching,
        Aiming,
        InCombat,
        OutOfCombat,
        NearTeammates,
        Alone,
        NearEnemy,
        Reloading,
        LastMagazine,
        NightTime,
        Injured,
        FullHealth,
        HasKillStreak,
        LowAmmo
    }
}
