using UnityEngine;

namespace DeadFrontier.Vehicles
{
    /// <summary>
    /// ScriptableObject defining vehicle data and properties.
    /// </summary>
    [CreateAssetMenu(fileName = "New Vehicle", menuName = "Dead Frontier/Vehicles/Vehicle Data")]
    public class VehicleDataSO : ScriptableObject
    {
        [Header("Identification")]
        public string vehicleId;
        public string vehicleName;
        [TextArea(3, 5)]
        public string description;
        public VehicleType vehicleType;
        public VehicleClass vehicleClass;

        [Header("Prefab")]
        public GameObject vehiclePrefab;
        public Sprite vehicleIcon;
        public Sprite vehiclePreview;

        [Header("Performance")]
        [Range(10f, 200f)]
        public float maxSpeed = 80f;
        [Range(1f, 50f)]
        public float acceleration = 15f;
        [Range(1f, 20f)]
        public float braking = 10f;
        [Range(0.5f, 5f)]
        public float handling = 2f;
        [Range(100f, 10000f)]
        public float mass = 1500f;

        [Header("Durability")]
        [Range(100f, 5000f)]
        public float maxHealth = 1000f;
        [Range(0f, 100f)]
        public float armor = 0f;
        public bool isDestructible = true;
        public bool explodesOnDestroy = true;
        public float explosionDamage = 200f;
        public float explosionRadius = 10f;

        [Header("Fuel")]
        public bool requiresFuel = true;
        [Range(10f, 500f)]
        public float fuelCapacity = 100f;
        [Range(0.01f, 1f)]
        public float fuelConsumption = 0.1f;
        public FuelType fuelType = FuelType.Gasoline;

        [Header("Passengers")]
        public int maxPassengers = 4;
        public VehicleSeat[] seats;

        [Header("Storage")]
        public int storageSlots = 10;
        public float maxStorageWeight = 100f;

        [Header("Weapons")]
        public bool hasMountedWeapon;
        public VehicleWeapon[] mountedWeapons;

        [Header("Special Features")]
        public VehicleFeature[] features;
        public bool hasNightVision;
        public bool hasRadio;
        public bool hasSiren;
        public bool hasWinch;

        [Header("Terrain")]
        public TerrainCapability terrainCapability;
        [Range(0f, 1f)]
        public float offroadPenalty = 0.3f;
        public bool canSwim;
        public float waterSpeed = 20f;

        [Header("Audio")]
        public AudioClip engineStartSound;
        public AudioClip engineIdleSound;
        public AudioClip engineRunningSound;
        public AudioClip engineStopSound;
        public AudioClip hornSound;
        public AudioClip crashSound;
        public AudioClip explosionSound;

        [Header("Visual Effects")]
        public GameObject exhaustPrefab;
        public GameObject damageSmokeEffect;
        public GameObject fireEffect;
        public GameObject explosionEffect;
        public TrailRenderer[] tireTracks;

        [Header("Economy")]
        public Gameplay.ItemRarity rarity;
        public int purchaseCost;
        public int repairCostPerPercent = 10;
        public int fuelCostPerUnit = 5;

        [Header("Unlock")]
        public int unlockLevel = 1;
        public string unlockQuestId;

        [Header("Customization")]
        public VehicleSkin[] availableSkins;
        public VehicleUpgrade[] availableUpgrades;
    }

    [System.Serializable]
    public class VehicleSeat
    {
        public string seatId;
        public SeatType seatType;
        public Vector3 seatPosition;
        public Quaternion seatRotation;
        public bool canShoot;
        public float shootingArc = 180f;
        public string mountedWeaponId;
    }

    [System.Serializable]
    public class VehicleWeapon
    {
        public string weaponId;
        public string weaponName;
        public VehicleWeaponType weaponType;
        public float damage;
        public float fireRate;
        public int ammoCapacity;
        public float range;
        public Vector3 mountPosition;
        public float rotationSpeed = 90f;
        public float minElevation = -15f;
        public float maxElevation = 45f;
    }

    [System.Serializable]
    public class VehicleFeature
    {
        public VehicleFeatureType featureType;
        public float value;
        public bool isActive;
        public float energyCost;
    }

    [System.Serializable]
    public class TerrainCapability
    {
        public bool road = true;
        public bool dirt = true;
        public bool grass = true;
        public bool sand = false;
        public bool mud = false;
        public bool snow = false;
        public bool water = false;
    }

    [System.Serializable]
    public class VehicleSkin
    {
        public string skinId;
        public string skinName;
        public Material[] skinMaterials;
        public Sprite skinPreview;
        public Gameplay.ItemRarity rarity;
        public int cost;
    }

    [System.Serializable]
    public class VehicleUpgrade
    {
        public string upgradeId;
        public string upgradeName;
        public VehicleUpgradeType upgradeType;
        public float upgradeValue;
        public int cost;
        public int requiredLevel;
        public string[] requiredUpgrades;
    }

    public enum VehicleType
    {
        Car,
        Truck,
        SUV,
        Motorcycle,
        ATV,
        Tank,
        Helicopter,
        Boat,
        Bus
    }

    public enum VehicleClass
    {
        Civilian,
        Military,
        Emergency,
        Industrial,
        Luxury,
        Offroad,
        Armored
    }

    public enum SeatType
    {
        Driver,
        Passenger,
        Gunner,
        Commander
    }

    public enum VehicleWeaponType
    {
        MachineGun,
        Cannon,
        RocketLauncher,
        Flamethrower,
        ElectricTurret
    }

    public enum VehicleFeatureType
    {
        Turbo,
        Shield,
        Repair,
        Stealth,
        EMP,
        Smoke,
        Horn,
        Spotlight,
        GPS
    }

    public enum FuelType
    {
        Gasoline,
        Diesel,
        Electric,
        Hybrid
    }

    public enum VehicleUpgradeType
    {
        Engine,
        Armor,
        Suspension,
        Tires,
        Brakes,
        FuelTank,
        Storage,
        Weapon,
        Electronics
    }
}
