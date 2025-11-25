using UnityEngine;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DeadFrontier.UI.ViewModels
{
    /// <summary>
    /// Base class for all UI view models implementing MVVM pattern.
    /// </summary>
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        public virtual void Initialize() { }
        public virtual void Cleanup() { }
    }

    /// <summary>
    /// Main menu view model
    /// </summary>
    public class MainMenuViewModel : BaseViewModel
    {
        private string _playerName;
        private int _playerLevel;
        private int _softCurrency;
        private int _hardCurrency;
        private bool _isOnline;
        private string _currentVersion;
        private bool _hasNewContent;
        private List<NewsItem> _newsItems;
        private List<FeaturedContent> _featuredContent;

        public string PlayerName { get => _playerName; set => SetProperty(ref _playerName, value); }
        public int PlayerLevel { get => _playerLevel; set => SetProperty(ref _playerLevel, value); }
        public int SoftCurrency { get => _softCurrency; set => SetProperty(ref _softCurrency, value); }
        public int HardCurrency { get => _hardCurrency; set => SetProperty(ref _hardCurrency, value); }
        public bool IsOnline { get => _isOnline; set => SetProperty(ref _isOnline, value); }
        public string CurrentVersion { get => _currentVersion; set => SetProperty(ref _currentVersion, value); }
        public bool HasNewContent { get => _hasNewContent; set => SetProperty(ref _hasNewContent, value); }
        public List<NewsItem> NewsItems { get => _newsItems; set => SetProperty(ref _newsItems, value); }
        public List<FeaturedContent> FeaturedContent { get => _featuredContent; set => SetProperty(ref _featuredContent, value); }

        public Action OnPlayClicked { get; set; }
        public Action OnLoadoutClicked { get; set; }
        public Action OnShopClicked { get; set; }
        public Action OnSocialClicked { get; set; }
        public Action OnSettingsClicked { get; set; }
        public Action OnBattlePassClicked { get; set; }
    }

    /// <summary>
    /// HUD view model for in-game UI
    /// </summary>
    public class HUDViewModel : BaseViewModel
    {
        private float _health;
        private float _maxHealth;
        private float _stamina;
        private float _maxStamina;
        private float _armor;
        private int _ammoInMag;
        private int _ammoReserve;
        private string _weaponName;
        private Sprite _weaponIcon;
        private bool _isReloading;
        private float _reloadProgress;
        private List<MinimapMarker> _minimapMarkers;
        private List<KillfeedEntry> _killfeed;
        private List<ObjectiveDisplay> _objectives;
        private float _extractionProgress;
        private bool _isExtracting;
        private string _extractionTimer;
        private List<TeammateDisplay> _teammates;
        private string _currentZone;
        private float _matchTimeRemaining;
        private int _kills;
        private int _lootValue;

        public float Health { get => _health; set => SetProperty(ref _health, value); }
        public float MaxHealth { get => _maxHealth; set => SetProperty(ref _maxHealth, value); }
        public float HealthPercent => _maxHealth > 0 ? _health / _maxHealth : 0;
        public float Stamina { get => _stamina; set => SetProperty(ref _stamina, value); }
        public float MaxStamina { get => _maxStamina; set => SetProperty(ref _maxStamina, value); }
        public float StaminaPercent => _maxStamina > 0 ? _stamina / _maxStamina : 0;
        public float Armor { get => _armor; set => SetProperty(ref _armor, value); }
        public int AmmoInMag { get => _ammoInMag; set => SetProperty(ref _ammoInMag, value); }
        public int AmmoReserve { get => _ammoReserve; set => SetProperty(ref _ammoReserve, value); }
        public string WeaponName { get => _weaponName; set => SetProperty(ref _weaponName, value); }
        public Sprite WeaponIcon { get => _weaponIcon; set => SetProperty(ref _weaponIcon, value); }
        public bool IsReloading { get => _isReloading; set => SetProperty(ref _isReloading, value); }
        public float ReloadProgress { get => _reloadProgress; set => SetProperty(ref _reloadProgress, value); }
        public List<MinimapMarker> MinimapMarkers { get => _minimapMarkers; set => SetProperty(ref _minimapMarkers, value); }
        public List<KillfeedEntry> Killfeed { get => _killfeed; set => SetProperty(ref _killfeed, value); }
        public List<ObjectiveDisplay> Objectives { get => _objectives; set => SetProperty(ref _objectives, value); }
        public float ExtractionProgress { get => _extractionProgress; set => SetProperty(ref _extractionProgress, value); }
        public bool IsExtracting { get => _isExtracting; set => SetProperty(ref _isExtracting, value); }
        public string ExtractionTimer { get => _extractionTimer; set => SetProperty(ref _extractionTimer, value); }
        public List<TeammateDisplay> Teammates { get => _teammates; set => SetProperty(ref _teammates, value); }
        public string CurrentZone { get => _currentZone; set => SetProperty(ref _currentZone, value); }
        public float MatchTimeRemaining { get => _matchTimeRemaining; set => SetProperty(ref _matchTimeRemaining, value); }
        public int Kills { get => _kills; set => SetProperty(ref _kills, value); }
        public int LootValue { get => _lootValue; set => SetProperty(ref _lootValue, value); }
    }

    /// <summary>
    /// Inventory view model
    /// </summary>
    public class InventoryViewModel : BaseViewModel
    {
        private List<InventorySlot> _inventorySlots;
        private List<InventorySlot> _stashSlots;
        private InventorySlot _selectedSlot;
        private ItemDetails _selectedItemDetails;
        private float _currentWeight;
        private float _maxWeight;
        private int _availableSlots;
        private bool _isStashOpen;
        private List<EquipmentSlot> _equipmentSlots;
        private string _sortMode;
        private string _filterCategory;

        public List<InventorySlot> InventorySlots { get => _inventorySlots; set => SetProperty(ref _inventorySlots, value); }
        public List<InventorySlot> StashSlots { get => _stashSlots; set => SetProperty(ref _stashSlots, value); }
        public InventorySlot SelectedSlot { get => _selectedSlot; set => SetProperty(ref _selectedSlot, value); }
        public ItemDetails SelectedItemDetails { get => _selectedItemDetails; set => SetProperty(ref _selectedItemDetails, value); }
        public float CurrentWeight { get => _currentWeight; set => SetProperty(ref _currentWeight, value); }
        public float MaxWeight { get => _maxWeight; set => SetProperty(ref _maxWeight, value); }
        public int AvailableSlots { get => _availableSlots; set => SetProperty(ref _availableSlots, value); }
        public bool IsStashOpen { get => _isStashOpen; set => SetProperty(ref _isStashOpen, value); }
        public List<EquipmentSlot> EquipmentSlots { get => _equipmentSlots; set => SetProperty(ref _equipmentSlots, value); }
        public string SortMode { get => _sortMode; set => SetProperty(ref _sortMode, value); }
        public string FilterCategory { get => _filterCategory; set => SetProperty(ref _filterCategory, value); }

        public Action<InventorySlot> OnSlotClicked { get; set; }
        public Action<InventorySlot> OnSlotDoubleClicked { get; set; }
        public Action<InventorySlot, InventorySlot> OnItemDropped { get; set; }
        public Action OnSortClicked { get; set; }
        public Action OnFilterChanged { get; set; }
    }

    /// <summary>
    /// Loadout view model
    /// </summary>
    public class LoadoutViewModel : BaseViewModel
    {
        private List<LoadoutPreset> _loadoutPresets;
        private int _selectedLoadoutIndex;
        private LoadoutPreset _currentLoadout;
        private List<WeaponOption> _primaryWeapons;
        private List<WeaponOption> _secondaryWeapons;
        private List<WeaponOption> _meleeWeapons;
        private List<PerkOption> _availablePerks;
        private List<PerkOption> _equippedPerks;
        private int _usedPerkSlots;
        private int _maxPerkSlots;
        private List<CharacterOption> _characters;
        private CharacterOption _selectedCharacter;
        private List<AttachmentOption> _availableAttachments;

        public List<LoadoutPreset> LoadoutPresets { get => _loadoutPresets; set => SetProperty(ref _loadoutPresets, value); }
        public int SelectedLoadoutIndex { get => _selectedLoadoutIndex; set => SetProperty(ref _selectedLoadoutIndex, value); }
        public LoadoutPreset CurrentLoadout { get => _currentLoadout; set => SetProperty(ref _currentLoadout, value); }
        public List<WeaponOption> PrimaryWeapons { get => _primaryWeapons; set => SetProperty(ref _primaryWeapons, value); }
        public List<WeaponOption> SecondaryWeapons { get => _secondaryWeapons; set => SetProperty(ref _secondaryWeapons, value); }
        public List<WeaponOption> MeleeWeapons { get => _meleeWeapons; set => SetProperty(ref _meleeWeapons, value); }
        public List<PerkOption> AvailablePerks { get => _availablePerks; set => SetProperty(ref _availablePerks, value); }
        public List<PerkOption> EquippedPerks { get => _equippedPerks; set => SetProperty(ref _equippedPerks, value); }
        public int UsedPerkSlots { get => _usedPerkSlots; set => SetProperty(ref _usedPerkSlots, value); }
        public int MaxPerkSlots { get => _maxPerkSlots; set => SetProperty(ref _maxPerkSlots, value); }
        public List<CharacterOption> Characters { get => _characters; set => SetProperty(ref _characters, value); }
        public CharacterOption SelectedCharacter { get => _selectedCharacter; set => SetProperty(ref _selectedCharacter, value); }
        public List<AttachmentOption> AvailableAttachments { get => _availableAttachments; set => SetProperty(ref _availableAttachments, value); }

        public Action<int> OnLoadoutSelected { get; set; }
        public Action OnSaveLoadout { get; set; }
        public Action<string> OnWeaponSelected { get; set; }
        public Action<string> OnPerkToggled { get; set; }
        public Action<string> OnCharacterSelected { get; set; }
    }

    /// <summary>
    /// Shop view model
    /// </summary>
    public class ShopViewModel : BaseViewModel
    {
        private List<ShopCategoryVM> _categories;
        private string _selectedCategory;
        private List<ShopItemVM> _items;
        private ShopItemVM _selectedItem;
        private int _softCurrency;
        private int _hardCurrency;
        private List<ShopItemVM> _dailyDeals;
        private List<ShopItemVM> _featured;
        private string _searchQuery;
        private bool _showOwned;
        private string _sortBy;

        public List<ShopCategoryVM> Categories { get => _categories; set => SetProperty(ref _categories, value); }
        public string SelectedCategory { get => _selectedCategory; set => SetProperty(ref _selectedCategory, value); }
        public List<ShopItemVM> Items { get => _items; set => SetProperty(ref _items, value); }
        public ShopItemVM SelectedItem { get => _selectedItem; set => SetProperty(ref _selectedItem, value); }
        public int SoftCurrency { get => _softCurrency; set => SetProperty(ref _softCurrency, value); }
        public int HardCurrency { get => _hardCurrency; set => SetProperty(ref _hardCurrency, value); }
        public List<ShopItemVM> DailyDeals { get => _dailyDeals; set => SetProperty(ref _dailyDeals, value); }
        public List<ShopItemVM> Featured { get => _featured; set => SetProperty(ref _featured, value); }
        public string SearchQuery { get => _searchQuery; set => SetProperty(ref _searchQuery, value); }
        public bool ShowOwned { get => _showOwned; set => SetProperty(ref _showOwned, value); }
        public string SortBy { get => _sortBy; set => SetProperty(ref _sortBy, value); }

        public Action<string> OnCategorySelected { get; set; }
        public Action<ShopItemVM> OnItemSelected { get; set; }
        public Action<ShopItemVM> OnPurchase { get; set; }
        public Action OnSearch { get; set; }
    }

    /// <summary>
    /// Battle Pass view model
    /// </summary>
    public class BattlePassViewModel : BaseViewModel
    {
        private string _seasonName;
        private int _currentTier;
        private int _maxTiers;
        private int _currentXP;
        private int _xpToNextTier;
        private float _tierProgress;
        private bool _hasPremium;
        private List<BattlePassTierVM> _tiers;
        private BattlePassTierVM _selectedTier;
        private int _daysRemaining;
        private List<ChallengeVM> _dailyChallenges;
        private List<ChallengeVM> _weeklyChallenges;

        public string SeasonName { get => _seasonName; set => SetProperty(ref _seasonName, value); }
        public int CurrentTier { get => _currentTier; set => SetProperty(ref _currentTier, value); }
        public int MaxTiers { get => _maxTiers; set => SetProperty(ref _maxTiers, value); }
        public int CurrentXP { get => _currentXP; set => SetProperty(ref _currentXP, value); }
        public int XPToNextTier { get => _xpToNextTier; set => SetProperty(ref _xpToNextTier, value); }
        public float TierProgress { get => _tierProgress; set => SetProperty(ref _tierProgress, value); }
        public bool HasPremium { get => _hasPremium; set => SetProperty(ref _hasPremium, value); }
        public List<BattlePassTierVM> Tiers { get => _tiers; set => SetProperty(ref _tiers, value); }
        public BattlePassTierVM SelectedTier { get => _selectedTier; set => SetProperty(ref _selectedTier, value); }
        public int DaysRemaining { get => _daysRemaining; set => SetProperty(ref _daysRemaining, value); }
        public List<ChallengeVM> DailyChallenges { get => _dailyChallenges; set => SetProperty(ref _dailyChallenges, value); }
        public List<ChallengeVM> WeeklyChallenges { get => _weeklyChallenges; set => SetProperty(ref _weeklyChallenges, value); }

        public Action OnUpgradeToPremium { get; set; }
        public Action<int> OnClaimReward { get; set; }
        public Action OnBuyTiers { get; set; }
    }

    #region Supporting Data Structures

    [Serializable]
    public class NewsItem
    {
        public string id;
        public string title;
        public string summary;
        public Sprite image;
        public DateTime date;
        public string url;
    }

    [Serializable]
    public class FeaturedContent
    {
        public string id;
        public string title;
        public Sprite banner;
        public string actionType;
        public string actionData;
    }

    [Serializable]
    public class MinimapMarker
    {
        public Vector3 worldPosition;
        public MarkerType markerType;
        public string label;
        public Color color;
        public bool isPulsing;
    }

    public enum MarkerType
    {
        Player,
        Teammate,
        Enemy,
        Objective,
        Extraction,
        Loot,
        Danger,
        Ping
    }

    [Serializable]
    public class KillfeedEntry
    {
        public string killerName;
        public string victimName;
        public string weaponId;
        public Sprite weaponIcon;
        public bool isHeadshot;
        public float timestamp;
    }

    [Serializable]
    public class ObjectiveDisplay
    {
        public string objectiveId;
        public string description;
        public int currentProgress;
        public int targetProgress;
        public bool isComplete;
        public bool isOptional;
        public float distance;
    }

    [Serializable]
    public class TeammateDisplay
    {
        public string playerId;
        public string playerName;
        public float health;
        public float maxHealth;
        public bool isAlive;
        public bool isDown;
        public float distance;
        public Sprite classIcon;
    }

    [Serializable]
    public class InventorySlot
    {
        public int slotIndex;
        public string itemId;
        public string itemName;
        public Sprite icon;
        public int quantity;
        public float durability;
        public string rarity;
        public bool isEmpty;
        public bool isLocked;
    }

    [Serializable]
    public class ItemDetails
    {
        public string itemId;
        public string itemName;
        public string description;
        public Sprite icon;
        public string rarity;
        public float weight;
        public int value;
        public List<ItemStat> stats;
        public List<string> actions;
    }

    [Serializable]
    public class ItemStat
    {
        public string statName;
        public string statValue;
        public bool isPositive;
    }

    [Serializable]
    public class EquipmentSlot
    {
        public string slotType;
        public string equippedItemId;
        public Sprite icon;
        public bool isEmpty;
    }

    [Serializable]
    public class LoadoutPreset
    {
        public string presetName;
        public string primaryWeaponId;
        public string secondaryWeaponId;
        public string meleeWeaponId;
        public List<string> perkIds;
        public string characterId;
    }

    [Serializable]
    public class WeaponOption
    {
        public string weaponId;
        public string weaponName;
        public Sprite icon;
        public string weaponType;
        public bool isUnlocked;
        public int unlockLevel;
        public List<WeaponStatVM> stats;
    }

    [Serializable]
    public class WeaponStatVM
    {
        public string statName;
        public float value;
        public float maxValue;
    }

    [Serializable]
    public class PerkOption
    {
        public string perkId;
        public string perkName;
        public string description;
        public Sprite icon;
        public int slotCost;
        public bool isUnlocked;
        public bool isEquipped;
        public string category;
    }

    [Serializable]
    public class CharacterOption
    {
        public string characterId;
        public string characterName;
        public string className;
        public Sprite portrait;
        public bool isUnlocked;
        public List<string> abilities;
    }

    [Serializable]
    public class AttachmentOption
    {
        public string attachmentId;
        public string attachmentName;
        public string slotType;
        public Sprite icon;
        public bool isUnlocked;
        public bool isEquipped;
        public List<ItemStat> stats;
    }

    [Serializable]
    public class ShopCategoryVM
    {
        public string categoryId;
        public string categoryName;
        public Sprite icon;
        public int itemCount;
    }

    [Serializable]
    public class ShopItemVM
    {
        public string itemId;
        public string itemName;
        public string description;
        public Sprite icon;
        public string rarity;
        public int softPrice;
        public int hardPrice;
        public bool isOnSale;
        public float discount;
        public bool isOwned;
        public bool isNew;
        public bool isFeatured;
        public string timeRemaining;
    }

    [Serializable]
    public class BattlePassTierVM
    {
        public int tier;
        public RewardVM freeReward;
        public RewardVM premiumReward;
        public bool freeClaimable;
        public bool premiumClaimable;
        public bool freeClaimed;
        public bool premiumClaimed;
        public bool isCurrentTier;
    }

    [Serializable]
    public class RewardVM
    {
        public string rewardId;
        public string rewardName;
        public Sprite icon;
        public string rarity;
        public int quantity;
    }

    [Serializable]
    public class ChallengeVM
    {
        public string challengeId;
        public string challengeName;
        public string description;
        public int currentProgress;
        public int targetProgress;
        public bool isComplete;
        public int xpReward;
        public string timeRemaining;
    }

    #endregion
}
