using UnityEngine;

namespace DeadFrontier.Abilities
{
    /// <summary>
    /// ScriptableObject defining player abilities and skills.
    /// </summary>
    [CreateAssetMenu(fileName = "New Ability", menuName = "Dead Frontier/Abilities/Ability Data")]
    public class AbilityDataSO : ScriptableObject
    {
        [Header("Identification")]
        public string abilityId;
        public string abilityName;
        [TextArea(3, 5)]
        public string description;
        public AbilityType abilityType;
        public AbilityTargetType targetType;

        [Header("Visuals")]
        public Sprite abilityIcon;
        public Color abilityColor = Color.white;
        public GameObject abilityEffectPrefab;
        public GameObject impactEffectPrefab;

        [Header("Timing")]
        [Range(0f, 300f)]
        public float cooldown = 30f;
        [Range(0f, 10f)]
        public float castTime = 0f;
        [Range(0f, 60f)]
        public float duration = 0f;
        public bool canCancelCast = true;

        [Header("Cost")]
        public AbilityCostType costType = AbilityCostType.Cooldown;
        [Range(0f, 100f)]
        public float staminaCost = 0f;
        [Range(0f, 100f)]
        public float healthCost = 0f;
        public int ammoCost = 0;
        public int chargeCost = 1;
        public int maxCharges = 1;
        [Range(0f, 60f)]
        public float chargeRegenTime = 30f;

        [Header("Range & Area")]
        [Range(0f, 100f)]
        public float range = 10f;
        [Range(0f, 50f)]
        public float areaRadius = 0f;
        [Range(0f, 360f)]
        public float coneAngle = 0f;

        [Header("Effects")]
        public AbilityEffect[] effects;

        [Header("Audio")]
        public AudioClip castSound;
        public AudioClip impactSound;
        public AudioClip loopSound;

        [Header("Animation")]
        public string animationTrigger;
        public float animationSpeed = 1f;

        [Header("Networking")]
        public bool requiresServerValidation = true;
        public bool syncToAllClients = true;

        [Header("Progression")]
        public int unlockLevel = 1;
        public AbilityUpgrade[] upgrades;
    }

    [System.Serializable]
    public class AbilityEffect
    {
        public AbilityEffectType effectType;
        public float value;
        public float duration;
        public bool affectsSelf;
        public bool affectsAllies;
        public bool affectsEnemies;
        public bool stacks;
        public int maxStacks = 1;
        public GameObject effectVisual;
    }

    [System.Serializable]
    public class AbilityUpgrade
    {
        public string upgradeName;
        [TextArea(2, 3)]
        public string description;
        public AbilityUpgradeType upgradeType;
        public float upgradeValue;
        public int requiredLevel;
        public int upgradeCost;
    }

    public enum AbilityType
    {
        Damage,
        Heal,
        Buff,
        Debuff,
        Mobility,
        Utility,
        Summon,
        Transform,
        Ultimate
    }

    public enum AbilityTargetType
    {
        Self,
        SingleTarget,
        AreaOfEffect,
        Cone,
        Line,
        Global,
        Projectile
    }

    public enum AbilityCostType
    {
        Cooldown,
        Stamina,
        Health,
        Ammo,
        Charges,
        Resource
    }

    public enum AbilityEffectType
    {
        DirectDamage,
        DamageOverTime,
        DirectHeal,
        HealOverTime,
        Shield,
        SpeedBoost,
        SpeedSlow,
        DamageBoost,
        DamageReduction,
        Stun,
        Root,
        Silence,
        Blind,
        Fear,
        Taunt,
        Stealth,
        Reveal,
        Teleport,
        Push,
        Pull,
        Resurrect,
        CooldownReset,
        AmmoRestore,
        StaminaRestore
    }

    public enum AbilityUpgradeType
    {
        CooldownReduction,
        DurationIncrease,
        RangeIncrease,
        AreaIncrease,
        EffectIncrease,
        CostReduction,
        ChargeIncrease,
        AdditionalEffect
    }
}
