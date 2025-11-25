using UnityEngine;

namespace DeadFrontier.Weapons
{
    /// <summary>
    /// ScriptableObject defining complete weapon data for all weapon types.
    /// </summary>
    [CreateAssetMenu(fileName = "New Weapon", menuName = "Dead Frontier/Weapons/Weapon Data")]
    public class WeaponDataSO : ScriptableObject
    {
        [Header("Identification")]
        public string weaponId;
        public string weaponName;
        [TextArea(3, 5)]
        public string description;
        public WeaponType weaponType;
        public WeaponCategory category;

        [Header("Prefabs & Visuals")]
        public GameObject weaponPrefab;
        public GameObject worldDropPrefab;
        public Sprite weaponIcon;
        public Sprite crosshairSprite;
        public RuntimeAnimatorController weaponAnimator;

        [Header("Combat Stats")]
        [Range(1f, 500f)]
        public float damage = 25f;
        [Range(0.01f, 3f)]
        public float fireRate = 0.15f;
        [Range(5f, 500f)]
        public float range = 50f;
        [Range(0f, 1f)]
        public float criticalChance = 0.1f;
        [Range(1f, 5f)]
        public float criticalMultiplier = 2f;
        [Range(0f, 100f)]
        public float armorPenetration = 0f;
        public DamageType damageType = DamageType.Ballistic;

        [Header("Ammo & Magazine")]
        public int magazineSize = 30;
        public int maxReserveAmmo = 210;
        public AmmoType ammoType = AmmoType.Rifle;
        [Range(0.5f, 5f)]
        public float reloadTime = 2.5f;
        public ReloadType reloadType = ReloadType.Magazine;

        [Header("Firing Mode")]
        public FiringMode primaryFireMode = FiringMode.Automatic;
        public FiringMode[] availableFireModes;
        public int burstCount = 3;
        public float burstDelay = 0.05f;

        [Header("Accuracy & Recoil")]
        [Range(0f, 20f)]
        public float baseSpread = 3f;
        [Range(1f, 5f)]
        public float movementSpreadMultiplier = 2f;
        [Range(0.1f, 1f)]
        public float aimSpreadMultiplier = 0.3f;
        [Range(0f, 1f)]
        public float spreadRecoveryRate = 0.8f;
        public Vector2 recoilPattern = new Vector2(3f, 10f);
        [Range(0.1f, 1f)]
        public float recoilRecoveryRate = 0.7f;

        [Header("ADS (Aim Down Sights)")]
        [Range(0.1f, 1f)]
        public float adsSpeed = 0.25f;
        [Range(1f, 12f)]
        public float adsZoom = 1.5f;
        [Range(0f, 1f)]
        public float adsMovementPenalty = 0.3f;

        [Header("Shotgun Specific")]
        public int pelletsPerShot = 1;
        public float pelletSpread = 5f;

        [Header("Melee Specific")]
        public float meleeSwingTime = 0.5f;
        public float meleeArc = 90f;
        public bool canBlock;
        [Range(0f, 1f)]
        public float blockDamageReduction = 0.5f;

        [Header("Physics")]
        public float bulletVelocity = 900f;
        public float bulletDrop = 0.1f;
        public bool hasBulletPenetration;
        public int maxPenetrationTargets = 2;
        [Range(0f, 1f)]
        public float penetrationDamageFalloff = 0.3f;

        [Header("Audio")]
        public AudioClip[] fireSounds;
        public AudioClip[] reloadSounds;
        public AudioClip emptyClickSound;
        public AudioClip equipSound;
        public AudioClip adsInSound;
        public AudioClip adsOutSound;
        [Range(0f, 100f)]
        public float fireSoundNoiseLevel = 50f;

        [Header("Visual Effects")]
        public GameObject muzzleFlashPrefab;
        public GameObject bulletTracerPrefab;
        public GameObject impactEffectPrefab;
        public GameObject shellEjectPrefab;
        public Vector3 muzzleFlashOffset;
        public Vector3 shellEjectOffset;

        [Header("Attachments")]
        public AttachmentSlotType[] availableSlots;
        public AttachmentData[] defaultAttachments;

        [Header("Progression & Economy")]
        public Gameplay.ItemRarity rarity = Gameplay.ItemRarity.Common;
        public int unlockLevel = 1;
        public int purchaseCost = 1000;
        public int sellValue = 250;
        public bool isStarterWeapon;

        [Header("Balance Tags")]
        public string[] balanceTags; // "anti-armor", "silenced", "high-rof", etc.

        public WeaponRuntimeData ToRuntimeData()
        {
            return new WeaponRuntimeData
            {
                weaponId = weaponId,
                weaponName = weaponName,
                weaponType = weaponType,
                damage = damage,
                fireRate = fireRate,
                range = range,
                magazineSize = magazineSize,
                maxReserveAmmo = maxReserveAmmo,
                reloadTime = reloadTime,
                firingMode = primaryFireMode,
                recoilPattern = recoilPattern,
                baseSpread = baseSpread,
                adsZoom = adsZoom
            };
        }
    }

    [System.Serializable]
    public class WeaponRuntimeData
    {
        public string weaponId;
        public string weaponName;
        public WeaponType weaponType;
        public float damage;
        public float fireRate;
        public float range;
        public int magazineSize;
        public int maxReserveAmmo;
        public float reloadTime;
        public FiringMode firingMode;
        public Vector2 recoilPattern;
        public float baseSpread;
        public float adsZoom;
    }

    public enum WeaponType
    {
        Pistol,
        Revolver,
        SMG,
        AssaultRifle,
        SniperRifle,
        Shotgun,
        LMG,
        DMR,
        Launcher,
        Melee,
        Throwable
    }

    public enum WeaponCategory
    {
        Primary,
        Secondary,
        Melee,
        Special
    }

    public enum FiringMode
    {
        SemiAuto,
        Automatic,
        Burst,
        BoltAction,
        PumpAction
    }

    public enum AmmoType
    {
        Pistol,
        Rifle,
        Shotgun,
        Sniper,
        Heavy,
        Special,
        Explosive
    }

    public enum ReloadType
    {
        Magazine,
        SingleRound,
        Clip,
        Belt
    }

    public enum DamageType
    {
        Ballistic,
        Explosive,
        Fire,
        Electric,
        Toxic,
        Melee,
        Bleed
    }
}
