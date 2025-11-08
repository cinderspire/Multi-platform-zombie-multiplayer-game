using UnityEngine;

namespace DeadFrontier.Weapons
{
    /// <summary>
    /// ScriptableObject that defines weapon statistics and properties
    /// </summary>
    [CreateAssetMenu(fileName = "New Weapon", menuName = "DeadFrontier/Weapon Data")]
    public class WeaponData : ScriptableObject
    {
        [Header("Basic Info")]
        public string weaponName = "New Weapon";
        public WeaponType weaponType = WeaponType.Pistol;
        public Sprite weaponIcon;
        public GameObject worldModel;

        [Header("Combat Stats")]
        [Tooltip("Damage per shot")]
        public int damage = 25;

        [Tooltip("Time between shots (seconds)")]
        public float fireRate = 0.1f;

        [Tooltip("Maximum effective range (meters)")]
        public float range = 100f;

        [Tooltip("Number of bullets in magazine")]
        public int magazineSize = 30;

        [Tooltip("Time to reload (seconds)")]
        public float reloadTime = 2f;

        [Tooltip("Automatic, semi-auto, or burst")]
        public FiringMode firingMode = FiringMode.Automatic;

        [Header("Recoil")]
        [Tooltip("Horizontal recoil (random spread)")]
        public Vector2 recoilHorizontal = new Vector2(-0.5f, 0.5f);

        [Tooltip("Vertical recoil (kick)")]
        public Vector2 recoilVertical = new Vector2(1f, 2f);

        [Tooltip("How fast recoil recovers")]
        public float recoilRecoverySpeed = 5f;

        [Header("Accuracy")]
        [Tooltip("Base spread in degrees")]
        public float baseSpread = 1f;

        [Tooltip("Additional spread while moving")]
        public float movementSpreadMultiplier = 1.5f;

        [Tooltip("Additional spread while jumping")]
        public float jumpSpreadMultiplier = 3f;

        [Header("Audio")]
        public AudioClip fireSound;
        public AudioClip reloadSound;
        public AudioClip emptySound;

        [Range(0f, 200f)]
        [Tooltip("How far zombies can hear this weapon (meters)")]
        public float noiseLevel = 50f;

        [Header("Visual Effects")]
        public GameObject muzzleFlashPrefab;
        public GameObject impactEffectPrefab;
        public GameObject bulletTracerPrefab;

        [Header("Animation")]
        public string fireAnimationTrigger = "Fire";
        public string reloadAnimationTrigger = "Reload";

        [Header("Special Features")]
        [Tooltip("Can this weapon penetrate targets?")]
        public bool canPenetrate = false;

        [Tooltip("Maximum targets to penetrate")]
        public int maxPenetrationTargets = 1;

        [Tooltip("Damage reduction per penetration (percentage)")]
        [Range(0f, 1f)]
        public float penetrationDamageReduction = 0.5f;

        /// <summary>
        /// Gets the time between shots in seconds
        /// </summary>
        public float TimeBetweenShots => 1f / fireRate;

        /// <summary>
        /// Gets the random recoil for this shot
        /// </summary>
        public Vector2 GetRecoilAmount()
        {
            float horizontal = Random.Range(recoilHorizontal.x, recoilHorizontal.y);
            float vertical = Random.Range(recoilVertical.x, recoilVertical.y);
            return new Vector2(horizontal, vertical);
        }

        /// <summary>
        /// Gets the spread angle for current conditions
        /// </summary>
        public float GetSpread(bool isMoving, bool isJumping)
        {
            float spread = baseSpread;

            if (isMoving)
                spread *= movementSpreadMultiplier;

            if (isJumping)
                spread *= jumpSpreadMultiplier;

            return spread;
        }

        /// <summary>
        /// Validates weapon data
        /// </summary>
        private void OnValidate()
        {
            // Clamp values to reasonable ranges
            damage = Mathf.Max(1, damage);
            fireRate = Mathf.Max(0.1f, fireRate);
            range = Mathf.Max(1f, range);
            magazineSize = Mathf.Max(1, magazineSize);
            reloadTime = Mathf.Max(0.1f, reloadTime);
            baseSpread = Mathf.Max(0f, baseSpread);
            recoilRecoverySpeed = Mathf.Max(0.1f, recoilRecoverySpeed);
        }
    }

    /// <summary>
    /// Types of weapons in the game
    /// </summary>
    public enum WeaponType
    {
        Pistol,
        SMG,
        Rifle,
        Shotgun,
        Sniper,
        Melee
    }

    /// <summary>
    /// Firing modes for weapons
    /// </summary>
    public enum FiringMode
    {
        Automatic,  // Holds down fire button
        SemiAuto,   // One shot per click
        Burst       // 3 round burst
    }
}
