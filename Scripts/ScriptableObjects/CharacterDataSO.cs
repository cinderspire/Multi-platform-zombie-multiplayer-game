using UnityEngine;

namespace DeadFrontier.Characters
{
    /// <summary>
    /// ScriptableObject defining player character archetypes and classes.
    /// </summary>
    [CreateAssetMenu(fileName = "New Character", menuName = "Dead Frontier/Characters/Character Data")]
    public class CharacterDataSO : ScriptableObject
    {
        [Header("Identification")]
        public string characterId;
        public string characterName;
        [TextArea(3, 5)]
        public string description;
        [TextArea(2, 4)]
        public string backstory;
        public CharacterClass characterClass;

        [Header("Visuals")]
        public GameObject characterPrefab;
        public Sprite characterPortrait;
        public Sprite characterIcon;
        public RuntimeAnimatorController characterAnimator;
        public Avatar characterAvatar;

        [Header("Base Stats")]
        [Range(50f, 200f)]
        public float maxHealth = 100f;
        [Range(50f, 200f)]
        public float maxStamina = 100f;
        [Range(50f, 200f)]
        public float maxArmor = 0f;
        [Range(1f, 10f)]
        public float moveSpeed = 5f;
        [Range(1f, 15f)]
        public float sprintSpeed = 8f;
        [Range(0.5f, 2f)]
        public float jumpHeight = 1.2f;

        [Header("Stat Modifiers")]
        public StatModifiers statModifiers;

        [Header("Class Abilities")]
        public AbilityDataSO[] classAbilities;
        public AbilityDataSO ultimateAbility;
        [Range(60f, 300f)]
        public float ultimateCooldown = 180f;

        [Header("Passive Bonuses")]
        public PassiveBonus[] passiveBonuses;

        [Header("Starting Equipment")]
        public WeaponLoadout defaultLoadout;
        public string[] startingItems;

        [Header("Customization")]
        public CharacterCustomization defaultCustomization;
        public SkinVariant[] availableSkins;

        [Header("Audio")]
        public AudioClip[] hurtSounds;
        public AudioClip[] deathSounds;
        public AudioClip[] effortSounds;
        public AudioClip[] voiceLines;
        public AudioClip[] calloutSounds;

        [Header("Progression")]
        public int unlockLevel = 1;
        public int purchaseCost = 0;
        public bool isStarterCharacter = true;
        public Gameplay.ItemRarity rarity = Gameplay.ItemRarity.Common;
    }

    [System.Serializable]
    public class StatModifiers
    {
        [Range(-50f, 50f)]
        public float healthModifier = 0f;
        [Range(-50f, 50f)]
        public float staminaModifier = 0f;
        [Range(-2f, 2f)]
        public float speedModifier = 0f;
        [Range(-30f, 30f)]
        public float damageModifier = 0f;
        [Range(-30f, 30f)]
        public float reloadSpeedModifier = 0f;
        [Range(-30f, 30f)]
        public float healingReceivedModifier = 0f;
        [Range(-30f, 30f)]
        public float xpGainModifier = 0f;
        [Range(-30f, 30f)]
        public float lootFindModifier = 0f;
    }

    [System.Serializable]
    public class PassiveBonus
    {
        public string bonusName;
        public PassiveBonusType bonusType;
        public float bonusValue;
        [TextArea(2, 3)]
        public string description;
    }

    [System.Serializable]
    public class WeaponLoadout
    {
        public string primaryWeaponId;
        public string secondaryWeaponId;
        public string meleeWeaponId;
        public string[] grenadeIds;
    }

    [System.Serializable]
    public class CharacterCustomization
    {
        public int headIndex;
        public int bodyIndex;
        public int legsIndex;
        public Color primaryColor;
        public Color secondaryColor;
        public int hairStyleIndex;
        public Color hairColor;
        public int facialHairIndex;
    }

    [System.Serializable]
    public class SkinVariant
    {
        public string skinId;
        public string skinName;
        public Sprite skinPreview;
        public Material[] skinMaterials;
        public Gameplay.ItemRarity rarity;
        public int purchaseCost;
        public bool isLimitedTime;
    }

    public enum CharacterClass
    {
        Assault,      // Balanced combat specialist
        Medic,        // Healing and support
        Engineer,     // Building and traps
        Recon,        // Stealth and scouting
        Heavy,        // Tank with high health
        Demolition,   // Explosives expert
        Survivalist   // Resource gathering specialist
    }

    public enum PassiveBonusType
    {
        HealthRegen,
        StaminaRegen,
        DamageResistance,
        MovementSpeed,
        ReloadSpeed,
        WeaponSwapSpeed,
        MeleeDamage,
        ExplosiveDamage,
        HealingEfficiency,
        LootRadius,
        NoiseReduction,
        CriticalChance,
        XPBonus,
        CurrencyBonus
    }
}
