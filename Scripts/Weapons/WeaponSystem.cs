using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Weapons
{
    public class WeaponSystem : NetworkBehaviour
    {
        public static WeaponSystem Instance { get; private set; }

        [Header("Weapon Configuration")]
        [SerializeField] private int maxWeaponSlots = 4;
        [SerializeField] private bool enableWeaponDegradation = true;
        [SerializeField] private float degradationRate = 0.001f;

        private Dictionary<ulong, PlayerWeaponData> playerWeapons = new Dictionary<ulong, PlayerWeaponData>();
        private Dictionary<string, WeaponDefinition> weaponDatabase = new Dictionary<string, WeaponDefinition>();
        private Dictionary<string, AttachmentDefinition> attachmentDatabase = new Dictionary<string, AttachmentDefinition>();

        public event Action<ulong, string> OnWeaponEquipped;
        public event Action<ulong, string, float> OnWeaponFired;
        public event Action<ulong, string> OnWeaponReloaded;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer) { InitializeWeaponDatabase(); InitializeAttachmentDatabase(); }
        }

        private void InitializeWeaponDatabase()
        {
            // Pistols
            weaponDatabase["pistol_9mm"] = new WeaponDefinition
            {
                weaponId = "pistol_9mm",
                weaponName = "9mm Pistol",
                weaponType = WeaponType.Pistol,
                rarity = WeaponRarity.Common,
                baseDamage = 25f,
                headshotMultiplier = 2.5f,
                fireRate = 0.2f,
                reloadTime = 1.5f,
                magazineSize = 15,
                maxAmmo = 120,
                range = 30f,
                accuracy = 0.85f,
                recoilVertical = 2f,
                recoilHorizontal = 1f,
                penetration = 0,
                attachmentSlots = new List<AttachmentSlot> { AttachmentSlot.Sight, AttachmentSlot.Magazine, AttachmentSlot.Grip },
                firingMode = FiringMode.SemiAuto,
                ammoType = AmmoType.Pistol9mm,
                weaponClass = WeaponClass.Light,
                unlockLevel = 1,
                price = 500
            };

            weaponDatabase["pistol_45"] = new WeaponDefinition
            {
                weaponId = "pistol_45",
                weaponName = ".45 Pistol",
                weaponType = WeaponType.Pistol,
                rarity = WeaponRarity.Uncommon,
                baseDamage = 35f,
                headshotMultiplier = 2.5f,
                fireRate = 0.25f,
                reloadTime = 1.8f,
                magazineSize = 10,
                maxAmmo = 90,
                range = 35f,
                accuracy = 0.88f,
                recoilVertical = 3f,
                recoilHorizontal = 1.5f,
                penetration = 1,
                attachmentSlots = new List<AttachmentSlot> { AttachmentSlot.Sight, AttachmentSlot.Magazine, AttachmentSlot.Suppressor },
                firingMode = FiringMode.SemiAuto,
                ammoType = AmmoType.Pistol45,
                weaponClass = WeaponClass.Light,
                unlockLevel = 5,
                price = 1200
            };

            // SMGs
            weaponDatabase["smg_mp5"] = new WeaponDefinition
            {
                weaponId = "smg_mp5",
                weaponName = "MP5 SMG",
                weaponType = WeaponType.SMG,
                rarity = WeaponRarity.Uncommon,
                baseDamage = 22f,
                headshotMultiplier = 2f,
                fireRate = 0.08f,
                reloadTime = 2f,
                magazineSize = 30,
                maxAmmo = 180,
                range = 40f,
                accuracy = 0.75f,
                recoilVertical = 1.5f,
                recoilHorizontal = 2f,
                penetration = 0,
                attachmentSlots = new List<AttachmentSlot> { AttachmentSlot.Sight, AttachmentSlot.Magazine, AttachmentSlot.Barrel, AttachmentSlot.Stock, AttachmentSlot.Grip },
                firingMode = FiringMode.FullAuto,
                ammoType = AmmoType.Pistol9mm,
                weaponClass = WeaponClass.Medium,
                unlockLevel = 8,
                price = 2500
            };

            // Assault Rifles
            weaponDatabase["ar_m4"] = new WeaponDefinition
            {
                weaponId = "ar_m4",
                weaponName = "M4 Assault Rifle",
                weaponType = WeaponType.AssaultRifle,
                rarity = WeaponRarity.Rare,
                baseDamage = 35f,
                headshotMultiplier = 2.2f,
                fireRate = 0.1f,
                reloadTime = 2.5f,
                magazineSize = 30,
                maxAmmo = 210,
                range = 100f,
                accuracy = 0.88f,
                recoilVertical = 2.5f,
                recoilHorizontal = 1.8f,
                penetration = 1,
                attachmentSlots = new List<AttachmentSlot> { AttachmentSlot.Sight, AttachmentSlot.Magazine, AttachmentSlot.Barrel, AttachmentSlot.Stock, AttachmentSlot.Grip, AttachmentSlot.Underbarrel },
                firingMode = FiringMode.Burst | FiringMode.FullAuto,
                ammoType = AmmoType.Rifle556,
                weaponClass = WeaponClass.Medium,
                unlockLevel = 15,
                price = 5000
            };

            weaponDatabase["ar_ak47"] = new WeaponDefinition
            {
                weaponId = "ar_ak47",
                weaponName = "AK-47",
                weaponType = WeaponType.AssaultRifle,
                rarity = WeaponRarity.Rare,
                baseDamage = 42f,
                headshotMultiplier = 2.2f,
                fireRate = 0.12f,
                reloadTime = 2.8f,
                magazineSize = 30,
                maxAmmo = 180,
                range = 95f,
                accuracy = 0.82f,
                recoilVertical = 3.5f,
                recoilHorizontal = 2.5f,
                penetration = 2,
                attachmentSlots = new List<AttachmentSlot> { AttachmentSlot.Sight, AttachmentSlot.Magazine, AttachmentSlot.Barrel, AttachmentSlot.Stock, AttachmentSlot.Grip },
                firingMode = FiringMode.FullAuto,
                ammoType = AmmoType.Rifle762,
                weaponClass = WeaponClass.Medium,
                unlockLevel = 18,
                price = 5500
            };

            // Sniper Rifles
            weaponDatabase["sniper_m24"] = new WeaponDefinition
            {
                weaponId = "sniper_m24",
                weaponName = "M24 Sniper Rifle",
                weaponType = WeaponType.SniperRifle,
                rarity = WeaponRarity.Epic,
                baseDamage = 120f,
                headshotMultiplier = 3.5f,
                fireRate = 1.5f,
                reloadTime = 3.5f,
                magazineSize = 5,
                maxAmmo = 40,
                range = 500f,
                accuracy = 0.98f,
                recoilVertical = 8f,
                recoilHorizontal = 2f,
                penetration = 3,
                attachmentSlots = new List<AttachmentSlot> { AttachmentSlot.Sight, AttachmentSlot.Magazine, AttachmentSlot.Barrel, AttachmentSlot.Stock, AttachmentSlot.Bipod },
                firingMode = FiringMode.BoltAction,
                ammoType = AmmoType.Rifle762,
                weaponClass = WeaponClass.Heavy,
                unlockLevel = 25,
                price = 10000
            };

            // Shotguns
            weaponDatabase["shotgun_pump"] = new WeaponDefinition
            {
                weaponId = "shotgun_pump",
                weaponName = "Pump Shotgun",
                weaponType = WeaponType.Shotgun,
                rarity = WeaponRarity.Common,
                baseDamage = 80f,
                headshotMultiplier = 1.8f,
                fireRate = 0.8f,
                reloadTime = 0.5f,
                magazineSize = 8,
                maxAmmo = 40,
                range = 15f,
                accuracy = 0.5f,
                recoilVertical = 5f,
                recoilHorizontal = 3f,
                penetration = 0,
                attachmentSlots = new List<AttachmentSlot> { AttachmentSlot.Sight, AttachmentSlot.Barrel, AttachmentSlot.Stock },
                firingMode = FiringMode.SemiAuto,
                ammoType = AmmoType.Shotgun12g,
                weaponClass = WeaponClass.Heavy,
                pelletsPerShot = 8,
                unlockLevel = 3,
                price = 1500
            };

            // LMGs
            weaponDatabase["lmg_m249"] = new WeaponDefinition
            {
                weaponId = "lmg_m249",
                weaponName = "M249 LMG",
                weaponType = WeaponType.LMG,
                rarity = WeaponRarity.Epic,
                baseDamage = 38f,
                headshotMultiplier = 2f,
                fireRate = 0.09f,
                reloadTime = 4.5f,
                magazineSize = 100,
                maxAmmo = 300,
                range = 120f,
                accuracy = 0.75f,
                recoilVertical = 2f,
                recoilHorizontal = 3f,
                penetration = 2,
                attachmentSlots = new List<AttachmentSlot> { AttachmentSlot.Sight, AttachmentSlot.Barrel, AttachmentSlot.Bipod, AttachmentSlot.Grip },
                firingMode = FiringMode.FullAuto,
                ammoType = AmmoType.Rifle556,
                weaponClass = WeaponClass.Heavy,
                unlockLevel = 30,
                price = 12000
            };

            // Melee Weapons
            weaponDatabase["melee_knife"] = new WeaponDefinition
            {
                weaponId = "melee_knife",
                weaponName = "Combat Knife",
                weaponType = WeaponType.Melee,
                rarity = WeaponRarity.Common,
                baseDamage = 50f,
                headshotMultiplier = 1f,
                fireRate = 0.5f,
                reloadTime = 0f,
                magazineSize = 0,
                maxAmmo = 0,
                range = 2f,
                accuracy = 1f,
                recoilVertical = 0f,
                recoilHorizontal = 0f,
                penetration = 0,
                attachmentSlots = new List<AttachmentSlot>(),
                firingMode = FiringMode.Melee,
                ammoType = AmmoType.None,
                weaponClass = WeaponClass.Melee,
                unlockLevel = 1,
                price = 200
            };

            weaponDatabase["melee_axe"] = new WeaponDefinition
            {
                weaponId = "melee_axe",
                weaponName = "Fire Axe",
                weaponType = WeaponType.Melee,
                rarity = WeaponRarity.Uncommon,
                baseDamage = 90f,
                headshotMultiplier = 1.5f,
                fireRate = 1f,
                reloadTime = 0f,
                magazineSize = 0,
                maxAmmo = 0,
                range = 2.5f,
                accuracy = 1f,
                recoilVertical = 0f,
                recoilHorizontal = 0f,
                penetration = 0,
                attachmentSlots = new List<AttachmentSlot>(),
                firingMode = FiringMode.Melee,
                ammoType = AmmoType.None,
                weaponClass = WeaponClass.Melee,
                unlockLevel = 5,
                price = 800
            };

            // Explosives
            weaponDatabase["explosive_grenade"] = new WeaponDefinition
            {
                weaponId = "explosive_grenade",
                weaponName = "Frag Grenade",
                weaponType = WeaponType.Explosive,
                rarity = WeaponRarity.Uncommon,
                baseDamage = 150f,
                headshotMultiplier = 1f,
                fireRate = 2f,
                reloadTime = 0f,
                magazineSize = 1,
                maxAmmo = 5,
                range = 30f,
                accuracy = 1f,
                recoilVertical = 0f,
                recoilHorizontal = 0f,
                penetration = 0,
                explosionRadius = 8f,
                attachmentSlots = new List<AttachmentSlot>(),
                firingMode = FiringMode.Throw,
                ammoType = AmmoType.Explosive,
                weaponClass = WeaponClass.Throwable,
                unlockLevel = 10,
                price = 500
            };
        }

        private void InitializeAttachmentDatabase()
        {
            // Sights
            attachmentDatabase["sight_reddot"] = new AttachmentDefinition
            {
                attachmentId = "sight_reddot",
                attachmentName = "Red Dot Sight",
                attachmentType = AttachmentSlot.Sight,
                rarity = AttachmentRarity.Common,
                accuracyBonus = 0.05f,
                recoilReduction = 0f,
                rangeBonus = 0f,
                magazineSizeBonus = 0,
                reloadSpeedBonus = 0f,
                unlockLevel = 3,
                price = 500
            };

            attachmentDatabase["sight_acog"] = new AttachmentDefinition
            {
                attachmentId = "sight_acog",
                attachmentName = "ACOG Scope (4x)",
                attachmentType = AttachmentSlot.Sight,
                rarity = AttachmentRarity.Rare,
                accuracyBonus = 0.15f,
                recoilReduction = 0f,
                rangeBonus = 20f,
                magazineSizeBonus = 0,
                reloadSpeedBonus = 0f,
                unlockLevel = 15,
                price = 2000
            };

            // Magazines
            attachmentDatabase["mag_extended"] = new AttachmentDefinition
            {
                attachmentId = "mag_extended",
                attachmentName = "Extended Magazine",
                attachmentType = AttachmentSlot.Magazine,
                rarity = AttachmentRarity.Uncommon,
                accuracyBonus = 0f,
                recoilReduction = 0f,
                rangeBonus = 0f,
                magazineSizeBonus = 10,
                reloadSpeedBonus = -0.1f,
                unlockLevel = 5,
                price = 800
            };

            attachmentDatabase["mag_quickdraw"] = new AttachmentDefinition
            {
                attachmentId = "mag_quickdraw",
                attachmentName = "Quickdraw Magazine",
                attachmentType = AttachmentSlot.Magazine,
                rarity = AttachmentRarity.Rare,
                accuracyBonus = 0f,
                recoilReduction = 0f,
                rangeBonus = 0f,
                magazineSizeBonus = 0,
                reloadSpeedBonus = 0.3f,
                unlockLevel = 12,
                price = 1500
            };

            // Barrels
            attachmentDatabase["barrel_suppressor"] = new AttachmentDefinition
            {
                attachmentId = "barrel_suppressor",
                attachmentName = "Suppressor",
                attachmentType = AttachmentSlot.Barrel,
                rarity = AttachmentRarity.Rare,
                accuracyBonus = 0.1f,
                recoilReduction = 0.15f,
                rangeBonus = -5f,
                magazineSizeBonus = 0,
                reloadSpeedBonus = 0f,
                unlockLevel = 20,
                price = 3000
            };

            attachmentDatabase["barrel_compensator"] = new AttachmentDefinition
            {
                attachmentId = "barrel_compensator",
                attachmentName = "Muzzle Compensator",
                attachmentType = AttachmentSlot.Barrel,
                rarity = AttachmentRarity.Uncommon,
                accuracyBonus = 0.05f,
                recoilReduction = 0.25f,
                rangeBonus = 0f,
                magazineSizeBonus = 0,
                reloadSpeedBonus = 0f,
                unlockLevel = 8,
                price = 1200
            };

            // Grips
            attachmentDatabase["grip_vertical"] = new AttachmentDefinition
            {
                attachmentId = "grip_vertical",
                attachmentName = "Vertical Grip",
                attachmentType = AttachmentSlot.Grip,
                rarity = AttachmentRarity.Common,
                accuracyBonus = 0.08f,
                recoilReduction = 0.2f,
                rangeBonus = 0f,
                magazineSizeBonus = 0,
                reloadSpeedBonus = 0f,
                unlockLevel = 6,
                price = 600
            };

            attachmentDatabase["grip_angled"] = new AttachmentDefinition
            {
                attachmentId = "grip_angled",
                attachmentName = "Angled Grip",
                attachmentType = AttachmentSlot.Grip,
                rarity = AttachmentRarity.Uncommon,
                accuracyBonus = 0.12f,
                recoilReduction = 0.15f,
                rangeBonus = 0f,
                magazineSizeBonus = 0,
                reloadSpeedBonus = 0f,
                unlockLevel = 10,
                price = 900
            };

            // Stocks
            attachmentDatabase["stock_tactical"] = new AttachmentDefinition
            {
                attachmentId = "stock_tactical",
                attachmentName = "Tactical Stock",
                attachmentType = AttachmentSlot.Stock,
                rarity = AttachmentRarity.Uncommon,
                accuracyBonus = 0.1f,
                recoilReduction = 0.18f,
                rangeBonus = 5f,
                magazineSizeBonus = 0,
                reloadSpeedBonus = 0f,
                unlockLevel = 7,
                price = 1000
            };
        }

        [ServerRpc(RequireOwnership = false)]
        public void EquipWeaponServerRpc(ulong playerId, string weaponId, int slot, ServerRpcParams rpcParams = default)
        {
            if (!playerWeapons.ContainsKey(playerId))
            {
                playerWeapons[playerId] = new PlayerWeaponData
                {
                    playerId = playerId,
                    equippedWeapons = new Dictionary<int, EquippedWeapon>(),
                    ownedWeapons = new List<string>(),
                    currentWeaponSlot = 0
                };
            }

            var playerData = playerWeapons[playerId];
            if (slot < 0 || slot >= maxWeaponSlots) return;

            if (weaponDatabase.TryGetValue(weaponId, out var weaponDef))
            {
                var equipped = new EquippedWeapon
                {
                    weaponId = weaponId,
                    definition = weaponDef,
                    currentAmmo = weaponDef.magazineSize,
                    reserveAmmo = weaponDef.maxAmmo,
                    durability = 100f,
                    attachments = new Dictionary<AttachmentSlot, string>()
                };

                playerData.equippedWeapons[slot] = equipped;
                playerData.currentWeaponSlot = slot;
                OnWeaponEquipped?.Invoke(playerId, weaponId);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void FireWeaponServerRpc(ulong playerId, Vector3 direction, ServerRpcParams rpcParams = default)
        {
            if (!playerWeapons.TryGetValue(playerId, out var playerData)) return;
            if (!playerData.equippedWeapons.TryGetValue(playerData.currentWeaponSlot, out var weapon)) return;

            if (weapon.currentAmmo <= 0) return;

            weapon.currentAmmo--;
            float damage = CalculateDamage(weapon);
            OnWeaponFired?.Invoke(playerId, weapon.weaponId, damage);

            if (enableWeaponDegradation)
            {
                weapon.durability -= degradationRate;
            }
        }

        private float CalculateDamage(EquippedWeapon weapon)
        {
            float damage = weapon.definition.baseDamage;
            foreach (var attachment in weapon.attachments.Values)
            {
                if (attachmentDatabase.TryGetValue(attachment, out var attachDef))
                {
                    damage *= (1f + attachDef.accuracyBonus);
                }
            }
            return damage;
        }

        public PlayerWeaponData GetPlayerWeapons(ulong playerId)
        {
            return playerWeapons.GetValueOrDefault(playerId);
        }
    }

    [Serializable]
    public class PlayerWeaponData
    {
        public ulong playerId;
        public Dictionary<int, EquippedWeapon> equippedWeapons;
        public List<string> ownedWeapons;
        public int currentWeaponSlot;
    }

    [Serializable]
    public class EquippedWeapon
    {
        public string weaponId;
        public WeaponDefinition definition;
        public int currentAmmo;
        public int reserveAmmo;
        public float durability;
        public Dictionary<AttachmentSlot, string> attachments;
    }

    [Serializable]
    public class WeaponDefinition
    {
        public string weaponId;
        public string weaponName;
        public WeaponType weaponType;
        public WeaponRarity rarity;
        public float baseDamage;
        public float headshotMultiplier;
        public float fireRate;
        public float reloadTime;
        public int magazineSize;
        public int maxAmmo;
        public float range;
        public float accuracy;
        public float recoilVertical;
        public float recoilHorizontal;
        public int penetration;
        public List<AttachmentSlot> attachmentSlots;
        public FiringMode firingMode;
        public AmmoType ammoType;
        public WeaponClass weaponClass;
        public int pelletsPerShot = 1;
        public float explosionRadius;
        public int unlockLevel;
        public int price;
    }

    [Serializable]
    public class AttachmentDefinition
    {
        public string attachmentId;
        public string attachmentName;
        public AttachmentSlot attachmentType;
        public AttachmentRarity rarity;
        public float accuracyBonus;
        public float recoilReduction;
        public float rangeBonus;
        public int magazineSizeBonus;
        public float reloadSpeedBonus;
        public int unlockLevel;
        public int price;
    }

    public enum WeaponType { Pistol, SMG, AssaultRifle, SniperRifle, Shotgun, LMG, Melee, Explosive }
    public enum WeaponRarity { Common, Uncommon, Rare, Epic, Legendary, Mythic }
    public enum FiringMode { SemiAuto = 1, Burst = 2, FullAuto = 4, BoltAction = 8, Melee = 16, Throw = 32 }
    public enum AmmoType { None, Pistol9mm, Pistol45, Rifle556, Rifle762, Shotgun12g, Explosive }
    public enum WeaponClass { Light, Medium, Heavy, Melee, Throwable }
    public enum AttachmentSlot { Sight, Magazine, Barrel, Stock, Grip, Underbarrel, Suppressor, Bipod }
    public enum AttachmentRarity { Common, Uncommon, Rare, Epic, Legendary }
}
