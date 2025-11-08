using UnityEngine;

namespace DeadFrontier.Weapons
{
    /// <summary>
    /// ScriptableObject defining weapon attachment data, stats, and compatibility.
    /// </summary>
    [CreateAssetMenu(fileName = "New Attachment", menuName = "Dead Frontier/Weapons/Attachment")]
    public class AttachmentData : ScriptableObject
    {
        [Header("Basic Info")]
        public string attachmentId;
        public string attachmentName;
        [TextArea(3, 5)]
        public string description;
        public AttachmentSlotType slotType;

        [Header("Visuals")]
        public Sprite icon;
        public GameObject visualPrefab;

        [Header("Stats")]
        public StatModifier[] statModifiers;

        [Header("Special Properties")]
        public float zoomLevel = 1f; // For optics
        public bool providesLaser; // For laser sights
        public bool providesFlashlight; // For tactical lights
        public bool reducesMuzzleFlash; // For suppressors
        public bool providesRecoilPattern; // For grips

        [Header("Compatibility")]
        public WeaponType[] compatibleWeaponTypes;
        public string[] compatibleWeaponIds;

        [Header("Unlock Requirements")]
        public int unlockLevel;
        public int unlockCost;
        public bool isDefaultAttachment;

        [Header("Rarity & Value")]
        public Gameplay.ItemRarity rarity = Gameplay.ItemRarity.Common;
        public int sellValue = 100;
    }

    [System.Serializable]
    public class StatModifier
    {
        public WeaponStatType statType;
        public ModifierType modifierType;
        public float value;

        [TextArea(2, 3)]
        public string displayText; // UI text like "+20% Range"
    }

    public enum AttachmentSlotType
    {
        Optic,      // Red dots, scopes, holographic sights
        Barrel,     // Suppressors, compensators, extended barrels
        Grip,       // Foregrips, angled grips
        Magazine,   // Extended mags, fast mags
        Stock,      // Tactical stocks, no stock
        Laser,      // Laser sights, flashlights
        Underbarrel // Grenade launchers (future)
    }

    public enum WeaponStatType
    {
        Damage,
        FireRate,
        Accuracy,
        Range,
        ReloadSpeed,
        Stability,
        MagazineSize,
        ADSSpeed,
        MovementSpeed,
        RecoilControl
    }

    public enum ModifierType
    {
        Additive,       // Add flat value
        Multiplicative, // Multiply by percentage
        Override        // Replace value entirely
    }
}
