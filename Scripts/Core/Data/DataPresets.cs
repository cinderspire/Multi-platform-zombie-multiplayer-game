using UnityEngine;

namespace DeadFrontier.Core.Data
{
    /// <summary>
    /// Contains preset data configurations for weapons, zombies, and items
    /// Use these values when creating ScriptableObject assets in Unity Editor
    /// </summary>
    public static class DataPresets
    {
        #region Weapon Presets

        public static class Weapons
        {
            // PISTOL - M1911
            public static readonly WeaponPreset Pistol = new WeaponPreset
            {
                weaponName = "M1911 Pistol",
                weaponType = Weapons.WeaponType.Pistol,
                damage = 25,
                fireRate = 0.15f,
                range = 50f,
                magazineSize = 12,
                maxReserveAmmo = 84,
                reloadTime = 1.5f,
                firingMode = Weapons.FiringMode.SemiAuto,
                recoilPattern = new Vector2(2f, 8f),
                baseSpread = 2f,
                movementSpreadMultiplier = 1.5f,
                aimSpreadMultiplier = 0.5f,
                fireSoundNoiseLevel = 30f
            };

            // ASSAULT RIFLE - AK-47
            public static readonly WeaponPreset AssaultRifle = new WeaponPreset
            {
                weaponName = "AK-47 Rifle",
                weaponType = Weapons.WeaponType.AssaultRifle,
                damage = 30,
                fireRate = 0.1f,
                range = 100f,
                magazineSize = 30,
                maxReserveAmmo = 210,
                reloadTime = 2.5f,
                firingMode = Weapons.FiringMode.Automatic,
                recoilPattern = new Vector2(3f, 10f),
                baseSpread = 3f,
                movementSpreadMultiplier = 2f,
                aimSpreadMultiplier = 0.3f,
                fireSoundNoiseLevel = 50f
            };

            // SHOTGUN - Pump Action
            public static readonly WeaponPreset Shotgun = new WeaponPreset
            {
                weaponName = "Pump Shotgun",
                weaponType = Weapons.WeaponType.Shotgun,
                damage = 15, // Per pellet (8 pellets total = 120 damage)
                fireRate = 0.8f,
                range = 30f,
                magazineSize = 8,
                maxReserveAmmo = 32,
                reloadTime = 0.5f, // Per shell
                firingMode = Weapons.FiringMode.SemiAuto,
                recoilPattern = new Vector2(5f, 15f),
                baseSpread = 15f,
                movementSpreadMultiplier = 1.3f,
                aimSpreadMultiplier = 0.7f,
                pelletsPerShot = 8,
                fireSoundNoiseLevel = 60f
            };

            // SMG - MP5
            public static readonly WeaponPreset SMG = new WeaponPreset
            {
                weaponName = "MP5 SMG",
                weaponType = Weapons.WeaponType.SMG,
                damage = 20,
                fireRate = 0.08f,
                range = 40f,
                magazineSize = 30,
                maxReserveAmmo = 180,
                reloadTime = 2f,
                firingMode = Weapons.FiringMode.Automatic,
                recoilPattern = new Vector2(2f, 6f),
                baseSpread = 4f,
                movementSpreadMultiplier = 1.2f,
                aimSpreadMultiplier = 0.4f,
                fireSoundNoiseLevel = 40f
            };

            // SNIPER - Bolt Action
            public static readonly WeaponPreset Sniper = new WeaponPreset
            {
                weaponName = "Bolt-Action Rifle",
                weaponType = Weapons.WeaponType.SniperRifle,
                damage = 80,
                fireRate = 1.5f,
                range = 200f,
                magazineSize = 5,
                maxReserveAmmo = 30,
                reloadTime = 3f,
                firingMode = Weapons.FiringMode.SemiAuto,
                recoilPattern = new Vector2(10f, 20f),
                baseSpread = 0.5f,
                movementSpreadMultiplier = 5f,
                aimSpreadMultiplier = 0.1f,
                fireSoundNoiseLevel = 70f
            };
        }

        #endregion

        #region Zombie Presets

        public static class Zombies
        {
            // WALKER - Standard zombie
            public static readonly ZombiePreset Walker = new ZombiePreset
            {
                zombieName = "Walker",
                zombieType = DeadFrontier.Zombies.ZombieType.Walker,
                maxHealth = 100,
                moveSpeed = 2f,
                chaseSpeed = 4f,
                damage = 15,
                attackRange = 2f,
                attackCooldown = 1.5f,
                visionRange = 30f,
                visionAngle = 180f,
                hearingRange = 50f,
                minIdleTime = 2f,
                maxIdleTime = 5f,
                wanderRadius = 15f,
                canCallHorde = true,
                callHordeRadius = 30f,
                explodesOnDeath = false
            };

            // RUNNER - Fast zombie
            public static readonly ZombiePreset Runner = new ZombiePreset
            {
                zombieName = "Runner",
                zombieType = DeadFrontier.Zombies.ZombieType.Runner,
                maxHealth = 60,
                moveSpeed = 4f,
                chaseSpeed = 8f,
                damage = 10,
                attackRange = 1.5f,
                attackCooldown = 1f,
                visionRange = 40f,
                visionAngle = 200f,
                hearingRange = 60f,
                minIdleTime = 1f,
                maxIdleTime = 3f,
                wanderRadius = 25f,
                canCallHorde = true,
                callHordeRadius = 40f,
                explodesOnDeath = false
            };

            // TANK - Heavy zombie
            public static readonly ZombiePreset Tank = new ZombiePreset
            {
                zombieName = "Tank",
                zombieType = DeadFrontier.Zombies.ZombieType.Tank,
                maxHealth = 500,
                moveSpeed = 1f,
                chaseSpeed = 2f,
                damage = 40,
                attackRange = 3f,
                attackCooldown = 2f,
                visionRange = 25f,
                visionAngle = 160f,
                hearingRange = 40f,
                minIdleTime = 3f,
                maxIdleTime = 7f,
                wanderRadius = 10f,
                canCallHorde = false,
                callHordeRadius = 0f,
                explodesOnDeath = false
            };

            // EXPLODER - Suicide bomber zombie
            public static readonly ZombiePreset Exploder = new ZombiePreset
            {
                zombieName = "Exploder",
                zombieType = DeadFrontier.Zombies.ZombieType.Exploder,
                maxHealth = 50,
                moveSpeed = 3f,
                chaseSpeed = 6f,
                damage = 25,
                attackRange = 5f, // Explosion range
                attackCooldown = 0f, // Instant suicide attack
                visionRange = 35f,
                visionAngle = 180f,
                hearingRange = 50f,
                minIdleTime = 1f,
                maxIdleTime = 3f,
                wanderRadius = 15f,
                canCallHorde = false,
                callHordeRadius = 0f,
                explodesOnDeath = true,
                explosionDamage = 100,
                explosionRadius = 10f
            };

            // SCREAMER - Support zombie that calls hordes
            public static readonly ZombiePreset Screamer = new ZombiePreset
            {
                zombieName = "Screamer",
                zombieType = DeadFrontier.Zombies.ZombieType.Screamer,
                maxHealth = 80,
                moveSpeed = 2.5f,
                chaseSpeed = 5f,
                damage = 12,
                attackRange = 2f,
                attackCooldown = 3f, // Screams every 3 seconds
                visionRange = 50f,
                visionAngle = 220f,
                hearingRange = 70f,
                minIdleTime = 2f,
                maxIdleTime = 4f,
                wanderRadius = 20f,
                canCallHorde = true,
                callHordeRadius = 100f, // Massive call radius
                explodesOnDeath = false
            };
        }

        #endregion

        #region Item Presets

        public static class Items
        {
            // AMMO
            public static readonly ItemPreset PistolAmmo = new ItemPreset
            {
                itemName = "9mm Ammo",
                itemType = Items.ItemType.Ammo,
                description = "Standard 9mm pistol ammunition",
                maxStackSize = 50,
                isStackable = true,
                weight = 0.5f,
                rarity = Items.ItemRarity.Common,
                sellValue = 10
            };

            public static readonly ItemPreset RifleAmmo = new ItemPreset
            {
                itemName = "7.62mm Ammo",
                itemType = Items.ItemType.Ammo,
                description = "High-caliber rifle ammunition",
                maxStackSize = 30,
                isStackable = true,
                weight = 0.8f,
                rarity = Items.ItemRarity.Uncommon,
                sellValue = 20
            };

            public static readonly ItemPreset ShotgunShells = new ItemPreset
            {
                itemName = "12 Gauge Shells",
                itemType = Items.ItemType.Ammo,
                description = "Shotgun shells",
                maxStackSize = 20,
                isStackable = true,
                weight = 1f,
                rarity = Items.ItemRarity.Uncommon,
                sellValue = 25
            };

            // MEDICAL
            public static readonly ItemPreset Bandage = new ItemPreset
            {
                itemName = "Bandage",
                itemType = Items.ItemType.Medical,
                description = "Restores 25 HP over 5 seconds",
                maxStackSize = 5,
                isStackable = true,
                weight = 0.1f,
                rarity = Items.ItemRarity.Common,
                sellValue = 50,
                healAmount = 25,
                healOverTime = true,
                healDuration = 5f
            };

            public static readonly ItemPreset MedKit = new ItemPreset
            {
                itemName = "Medical Kit",
                itemType = Items.ItemType.Medical,
                description = "Fully restores health",
                maxStackSize = 2,
                isStackable = true,
                weight = 1f,
                rarity = Items.ItemRarity.Rare,
                sellValue = 200,
                healAmount = 100,
                healOverTime = false
            };

            public static readonly ItemPreset Adrenaline = new ItemPreset
            {
                itemName = "Adrenaline Shot",
                itemType = Items.ItemType.Medical,
                description = "Instantly restores 50 HP and increases sprint speed for 10s",
                maxStackSize = 3,
                isStackable = true,
                weight = 0.3f,
                rarity = Items.ItemRarity.Rare,
                sellValue = 150,
                healAmount = 50,
                healOverTime = false
            };

            // VALUABLES
            public static readonly ItemPreset GoldWatch = new ItemPreset
            {
                itemName = "Gold Watch",
                itemType = Items.ItemType.Valuable,
                description = "An expensive timepiece. Can be sold for cash.",
                maxStackSize = 1,
                isStackable = false,
                weight = 0.2f,
                rarity = Items.ItemRarity.Rare,
                sellValue = 500
            };

            public static readonly ItemPreset Diamond = new ItemPreset
            {
                itemName = "Diamond",
                itemType = Items.ItemType.Valuable,
                description = "A precious gem. Highly valuable.",
                maxStackSize = 1,
                isStackable = false,
                weight = 0.1f,
                rarity = Items.ItemRarity.Epic,
                sellValue = 1000
            };

            public static readonly ItemPreset CashStack = new ItemPreset
            {
                itemName = "Cash Stack",
                itemType = Items.ItemType.Valuable,
                description = "A bundle of cash",
                maxStackSize = 10,
                isStackable = true,
                weight = 0.1f,
                rarity = Items.ItemRarity.Common,
                sellValue = 100
            };

            // CONSUMABLES
            public static readonly ItemPreset EnergyDrink = new ItemPreset
            {
                itemName = "Energy Drink",
                itemType = Items.ItemType.Consumable,
                description = "Restores stamina and increases stamina regeneration for 30s",
                maxStackSize = 5,
                isStackable = true,
                weight = 0.3f,
                rarity = Items.ItemRarity.Common,
                sellValue = 30
            };

            public static readonly ItemPreset PainKillers = new ItemPreset
            {
                itemName = "Painkillers",
                itemType = Items.ItemType.Consumable,
                description = "Reduces damage taken by 20% for 60s",
                maxStackSize = 5,
                isStackable = true,
                weight = 0.1f,
                rarity = Items.ItemRarity.Uncommon,
                sellValue = 80
            };
        }

        #endregion

        #region Data Structures

        [System.Serializable]
        public struct WeaponPreset
        {
            public string weaponName;
            public Weapons.WeaponType weaponType;
            public int damage;
            public float fireRate;
            public float range;
            public int magazineSize;
            public int maxReserveAmmo;
            public float reloadTime;
            public Weapons.FiringMode firingMode;
            public Vector2 recoilPattern;
            public float baseSpread;
            public float movementSpreadMultiplier;
            public float aimSpreadMultiplier;
            public int pelletsPerShot;
            public float fireSoundNoiseLevel;
        }

        [System.Serializable]
        public struct ZombiePreset
        {
            public string zombieName;
            public DeadFrontier.Zombies.ZombieType zombieType;
            public int maxHealth;
            public float moveSpeed;
            public float chaseSpeed;
            public int damage;
            public float attackRange;
            public float attackCooldown;
            public float visionRange;
            public float visionAngle;
            public float hearingRange;
            public float minIdleTime;
            public float maxIdleTime;
            public float wanderRadius;
            public bool canCallHorde;
            public float callHordeRadius;
            public bool explodesOnDeath;
            public int explosionDamage;
            public float explosionRadius;
        }

        [System.Serializable]
        public struct ItemPreset
        {
            public string itemName;
            public Items.ItemType itemType;
            public string description;
            public int maxStackSize;
            public bool isStackable;
            public float weight;
            public Items.ItemRarity rarity;
            public int sellValue;
            public float healAmount;
            public bool healOverTime;
            public float healDuration;
        }

        #endregion
    }
}
