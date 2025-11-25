using UnityEngine;

namespace DeadFrontier.Economy
{
    /// <summary>
    /// ScriptableObject defining shop items and store configurations.
    /// </summary>
    [CreateAssetMenu(fileName = "New Shop Item", menuName = "Dead Frontier/Economy/Shop Item")]
    public class ShopItemDataSO : ScriptableObject
    {
        [Header("Identification")]
        public string shopItemId;
        public string displayName;
        [TextArea(3, 5)]
        public string description;
        public ShopCategory category;
        public ShopItemType itemType;

        [Header("Visuals")]
        public Sprite itemIcon;
        public Sprite featuredImage;
        public GameObject previewPrefab;
        public Color highlightColor = Color.white;

        [Header("Item Reference")]
        public string linkedItemId;
        public int quantity = 1;
        public Gameplay.ItemRarity rarity;

        [Header("Pricing")]
        public PricingInfo[] prices;
        public PricingInfo originalPrice;
        public bool isOnSale;
        [Range(0f, 1f)]
        public float discountPercent = 0f;
        public System.DateTime saleEndDate;

        [Header("Availability")]
        public bool isAvailable = true;
        public bool isLimitedTime;
        public System.DateTime availableFrom;
        public System.DateTime availableUntil;
        public bool isLimitedStock;
        public int stockLimit = -1;
        public int purchaseLimitPerPlayer = -1;
        public RefreshType refreshType = RefreshType.Never;

        [Header("Requirements")]
        public int requiredLevel = 0;
        public int requiredReputation = 0;
        public string requiredFactionId;
        public string[] requiredAchievementIds;
        public bool requiresBattlePass;

        [Header("Bundle Contents")]
        public bool isBundle;
        public BundleItem[] bundleContents;
        public float bundleSavings;

        [Header("Display")]
        public int sortOrder = 0;
        public bool isFeatured;
        public bool isNew;
        public bool isPopular;
        public string[] tags;
    }

    [System.Serializable]
    public class PricingInfo
    {
        public CurrencyType currencyType;
        public int amount;
        public bool isPrimary;
    }

    [System.Serializable]
    public class BundleItem
    {
        public string itemId;
        public int quantity;
        public Sprite itemIcon;
        public string itemName;
    }

    public enum ShopCategory
    {
        Featured,
        Weapons,
        Equipment,
        Cosmetics,
        Consumables,
        Currency,
        BattlePass,
        Bundles,
        Special,
        Daily,
        Weekly
    }

    public enum ShopItemType
    {
        Weapon,
        WeaponSkin,
        CharacterSkin,
        Emote,
        Spray,
        Banner,
        Avatar,
        Title,
        Currency,
        XPBoost,
        LootBox,
        BattlePassTier,
        Consumable,
        Material,
        Recipe,
        StashSpace
    }

    public enum CurrencyType
    {
        SoftCurrency,
        HardCurrency,
        EventCurrency,
        SeasonalCurrency,
        PremiumCurrency,
        RealMoney
    }

    public enum RefreshType
    {
        Never,
        Daily,
        Weekly,
        Monthly,
        Seasonal
    }
}
