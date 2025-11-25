using UnityEngine;

namespace DeadFrontier.Cosmetics
{
    /// <summary>
    /// ScriptableObject defining cosmetic items for player customization.
    /// </summary>
    [CreateAssetMenu(fileName = "New Cosmetic", menuName = "Dead Frontier/Cosmetics/Cosmetic Data")]
    public class CosmeticDataSO : ScriptableObject
    {
        [Header("Identification")]
        public string cosmeticId;
        public string cosmeticName;
        [TextArea(3, 5)]
        public string description;
        public CosmeticType cosmeticType;
        public CosmeticSlot slot;
        public Gameplay.ItemRarity rarity;

        [Header("Visuals")]
        public Sprite cosmeticIcon;
        public Sprite previewImage;
        public GameObject cosmeticPrefab;
        public Material[] cosmeticMaterials;
        public Texture2D[] cosmeticTextures;

        [Header("Animation")]
        public RuntimeAnimatorController animatorOverride;
        public AnimationClip[] customAnimations;
        public bool hasIdleAnimation;
        public bool hasEquipAnimation;

        [Header("Audio")]
        public AudioClip equipSound;
        public AudioClip[] useSounds;

        [Header("Particle Effects")]
        public GameObject trailEffectPrefab;
        public GameObject auraEffectPrefab;
        public GameObject footstepEffectPrefab;

        [Header("Compatibility")]
        public string[] compatibleCharacterIds;
        public string[] compatibleWeaponIds;
        public bool isUniversal = true;
        public Gender genderRestriction = Gender.Any;

        [Header("Set Bonus")]
        public string setId;
        public string setName;
        public int piecesInSet;
        public CosmeticSetBonus[] setBonuses;

        [Header("Unlock")]
        public CosmeticUnlockType unlockType;
        public int purchaseCost;
        public Economy.CurrencyType currencyType;
        public string unlockAchievementId;
        public int unlockLevel;
        public string unlockEventId;
        public bool isBattlePassExclusive;
        public int battlePassTier;

        [Header("Availability")]
        public bool isAvailable = true;
        public bool isLimitedTime;
        public System.DateTime availableFrom;
        public System.DateTime availableUntil;
        public bool isLegacy;

        [Header("Trading")]
        public bool isTradeable = true;
        public bool isMarketable = true;
        public int tradeValue;

        [Header("Display")]
        public int sortOrder;
        public string[] tags;
        public bool isNew;
        public bool isFeatured;
    }

    [System.Serializable]
    public class CosmeticSetBonus
    {
        public int piecesRequired;
        public SetBonusType bonusType;
        public float bonusValue;
        [TextArea(2, 3)]
        public string bonusDescription;
        public GameObject setBonusEffect;
    }

    public enum CosmeticType
    {
        CharacterSkin,
        WeaponSkin,
        WeaponCharm,
        Headwear,
        Eyewear,
        Facewear,
        Gloves,
        Backpack,
        Emote,
        Spray,
        Banner,
        Avatar,
        Title,
        LoadingScreen,
        MusicTrack,
        Finisher,
        VoicePack,
        DeathEffect,
        Parachute
    }

    public enum CosmeticSlot
    {
        None,
        Head,
        Face,
        Eyes,
        Torso,
        Hands,
        Legs,
        Feet,
        Back,
        WeaponPrimary,
        WeaponSecondary,
        WeaponMelee,
        Profile,
        Misc
    }

    public enum Gender
    {
        Any,
        Male,
        Female
    }

    public enum CosmeticUnlockType
    {
        Default,
        Purchase,
        Achievement,
        Level,
        Event,
        BattlePass,
        Quest,
        Collection,
        Promotional,
        Referral,
        Twitch
    }

    public enum SetBonusType
    {
        MovementSpeed,
        XPBonus,
        CurrencyBonus,
        LootBonus,
        VisualEffect,
        UniqueAnimation,
        SpecialInteraction
    }
}
