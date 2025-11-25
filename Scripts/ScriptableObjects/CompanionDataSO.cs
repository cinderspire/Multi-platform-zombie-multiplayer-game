using UnityEngine;

namespace DeadFrontier.Companions
{
    /// <summary>
    /// ScriptableObject defining companion/pet data and abilities.
    /// </summary>
    [CreateAssetMenu(fileName = "New Companion", menuName = "Dead Frontier/Companions/Companion Data")]
    public class CompanionDataSO : ScriptableObject
    {
        [Header("Identification")]
        public string companionId;
        public string companionName;
        [TextArea(3, 5)]
        public string description;
        [TextArea(2, 4)]
        public string backstory;
        public CompanionType companionType;
        public CompanionRole role;
        public Gameplay.ItemRarity rarity;

        [Header("Visuals")]
        public GameObject companionPrefab;
        public Sprite companionIcon;
        public Sprite companionPortrait;
        public RuntimeAnimatorController animator;
        public float scale = 1f;

        [Header("Base Stats")]
        [Range(10f, 500f)]
        public float maxHealth = 100f;
        [Range(1f, 15f)]
        public float moveSpeed = 5f;
        [Range(1f, 50f)]
        public float damage = 10f;
        [Range(1f, 30f)]
        public float attackRange = 5f;
        [Range(0.5f, 5f)]
        public float attackCooldown = 1.5f;

        [Header("Abilities")]
        public CompanionAbility[] abilities;
        public CompanionAbility ultimateAbility;

        [Header("Behavior")]
        public CompanionBehavior defaultBehavior;
        public float followDistance = 3f;
        public float aggroRange = 15f;
        public bool canRevive;
        public float reviveTime = 10f;

        [Header("Bonding")]
        public int maxBondLevel = 10;
        public int[] bondXPRequirements;
        public BondLevelBonus[] bondBonuses;

        [Header("Feeding")]
        public string[] acceptedFoodIds;
        public float hungerDecayRate = 0.01f;
        public float happinessDecayRate = 0.005f;

        [Header("Audio")]
        public AudioClip[] idleSounds;
        public AudioClip[] happySounds;
        public AudioClip[] attackSounds;
        public AudioClip[] hurtSounds;
        public AudioClip summonSound;
        public AudioClip dismissSound;

        [Header("Unlock")]
        public CompanionUnlockType unlockType;
        public int purchaseCost;
        public string unlockQuestId;
        public string unlockAchievementId;
        public bool isEventExclusive;

        [Header("Customization")]
        public CompanionSkin[] availableSkins;
        public CompanionAccessory[] availableAccessories;
    }

    [System.Serializable]
    public class CompanionAbility
    {
        public string abilityId;
        public string abilityName;
        [TextArea(2, 3)]
        public string description;
        public CompanionAbilityType abilityType;
        public float cooldown = 30f;
        public float range = 10f;
        public float value;
        public float duration;
        public int requiredBondLevel = 1;
        public Sprite abilityIcon;
        public GameObject effectPrefab;
        public AudioClip abilitySound;
    }

    [System.Serializable]
    public class BondLevelBonus
    {
        public int bondLevel;
        public BondBonusType bonusType;
        public float bonusValue;
        public string unlockedAbilityId;
        public string unlockedSkinId;
        [TextArea(2, 3)]
        public string description;
    }

    [System.Serializable]
    public class CompanionSkin
    {
        public string skinId;
        public string skinName;
        public Sprite skinPreview;
        public Material[] skinMaterials;
        public Gameplay.ItemRarity rarity;
        public int cost;
        public int requiredBondLevel;
    }

    [System.Serializable]
    public class CompanionAccessory
    {
        public string accessoryId;
        public string accessoryName;
        public AccessorySlot slot;
        public GameObject accessoryPrefab;
        public Sprite accessoryIcon;
        public int cost;
    }

    public enum CompanionType
    {
        Dog,
        Cat,
        Bird,
        Robot,
        Mutant,
        Spirit,
        Drone,
        Alien
    }

    public enum CompanionRole
    {
        Combat,     // Attacks enemies
        Support,    // Heals/buffs player
        Scout,      // Detects enemies/loot
        Carrier,    // Extra inventory space
        Tank,       // Draws enemy attention
        Utility     // Mixed abilities
    }

    public enum CompanionBehavior
    {
        Aggressive, // Attacks on sight
        Defensive,  // Only attacks when player is attacked
        Passive,    // Never attacks
        Guard,      // Protects player
        Scout       // Explores ahead
    }

    public enum CompanionAbilityType
    {
        Attack,
        Heal,
        Buff,
        Debuff,
        Scout,
        Taunt,
        Shield,
        Revive,
        Loot,
        Stealth
    }

    public enum BondBonusType
    {
        StatBoost,
        AbilityUnlock,
        SkinUnlock,
        CooldownReduction,
        DurationIncrease,
        PassiveBonus
    }

    public enum CompanionUnlockType
    {
        Default,
        Purchase,
        Quest,
        Achievement,
        Event,
        Drop,
        BattlePass
    }

    public enum AccessorySlot
    {
        Head,
        Neck,
        Back,
        Body
    }
}
