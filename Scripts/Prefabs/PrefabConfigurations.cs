using UnityEngine;
using System;
using System.Collections.Generic;

namespace DeadFrontier.Prefabs
{
    /// <summary>
    /// Configuration scripts for setting up game prefabs in Unity Editor.
    /// </summary>

    #region Player Prefab Configuration

    [CreateAssetMenu(fileName = "Player Prefab Config", menuName = "Dead Frontier/Prefabs/Player Configuration")]
    public class PlayerPrefabConfig : ScriptableObject
    {
        [Header("Character Model")]
        public GameObject characterModelPrefab;
        public Avatar characterAvatar;
        public RuntimeAnimatorController animatorController;

        [Header("Collision")]
        public float capsuleRadius = 0.4f;
        public float capsuleHeight = 1.8f;
        public Vector3 capsuleCenter = new Vector3(0, 0.9f, 0);
        public PhysicMaterial physicMaterial;

        [Header("Camera")]
        public Vector3 cameraOffset = new Vector3(0, 1.6f, 0);
        public float cameraDistance = 3f;
        public float minPitch = -80f;
        public float maxPitch = 80f;

        [Header("Audio")]
        public Transform headPosition;
        public float footstepVolume = 0.5f;
        public float voiceVolume = 1f;

        [Header("Attachment Points")]
        public Transform rightHandSocket;
        public Transform leftHandSocket;
        public Transform holsterSocket;
        public Transform backSocket;
        public Transform headSocket;

        [Header("IK")]
        public bool useIK = true;
        public Transform leftFootTarget;
        public Transform rightFootTarget;
        public Transform leftHandIKTarget;
        public Transform rightHandIKTarget;
        public Transform lookAtTarget;

        [Header("Network")]
        public float networkSendRate = 20f;
        public float interpolationDelay = 0.1f;

        [Header("Components To Add")]
        public string[] requiredComponents = new string[]
        {
            "PlayerController",
            "PlayerHealth",
            "PlayerMovement",
            "PlayerCamera",
            "WeaponController",
            "InventoryManager",
            "NetworkTransform",
            "AudioSource"
        };
    }

    #endregion

    #region Zombie Prefab Configuration

    [CreateAssetMenu(fileName = "Zombie Prefab Config", menuName = "Dead Frontier/Prefabs/Zombie Configuration")]
    public class ZombiePrefabConfig : ScriptableObject
    {
        [Header("Model")]
        public GameObject zombieModelPrefab;
        public Avatar zombieAvatar;
        public RuntimeAnimatorController animatorController;
        public AI.ZombieTypeData zombieTypeData;

        [Header("Collision")]
        public float capsuleRadius = 0.5f;
        public float capsuleHeight = 1.8f;
        public Vector3 capsuleCenter = new Vector3(0, 0.9f, 0);
        public float navMeshRadius = 0.5f;
        public float navMeshHeight = 2f;

        [Header("AI")]
        public float agentSpeed = 3.5f;
        public float agentAngularSpeed = 120f;
        public float agentAcceleration = 8f;
        public float stoppingDistance = 1.5f;
        public int agentAreaMask = -1;

        [Header("Senses")]
        public float visionRange = 30f;
        public float visionAngle = 180f;
        public float hearingRange = 50f;
        public float memoryDuration = 10f;

        [Header("Combat")]
        public Transform attackOrigin;
        public float attackRange = 2f;
        public float attackArc = 90f;
        public LayerMask attackLayerMask;

        [Header("Audio")]
        public float audioSourceMinDistance = 1f;
        public float audioSourceMaxDistance = 30f;

        [Header("LOD")]
        public float[] lodDistances = new float[] { 15f, 30f, 60f };
        public bool useLOD = true;

        [Header("Ragdoll")]
        public bool useRagdoll = true;
        public float ragdollMass = 70f;
        public float ragdollDrag = 0.5f;

        [Header("Components To Add")]
        public string[] requiredComponents = new string[]
        {
            "ZombieAI",
            "ZombieHealth",
            "ZombieSensors",
            "NavMeshAgent",
            "NetworkObject",
            "AudioSource"
        };
    }

    #endregion

    #region Weapon Prefab Configuration

    [CreateAssetMenu(fileName = "Weapon Prefab Config", menuName = "Dead Frontier/Prefabs/Weapon Configuration")]
    public class WeaponPrefabConfig : ScriptableObject
    {
        [Header("Model")]
        public GameObject firstPersonModelPrefab;
        public GameObject thirdPersonModelPrefab;
        public GameObject worldDropModelPrefab;
        public Weapons.WeaponDataSO weaponData;

        [Header("Transforms")]
        public Vector3 firstPersonPosition;
        public Vector3 firstPersonRotation;
        public Vector3 thirdPersonPosition;
        public Vector3 thirdPersonRotation;
        public Vector3 adsPositionOffset;
        public Vector3 adsRotationOffset;

        [Header("Muzzle")]
        public Transform muzzlePoint;
        public Vector3 muzzleOffset;

        [Header("Shell Ejection")]
        public Transform shellEjectPoint;
        public Vector3 shellEjectDirection = new Vector3(1, 1, 0);
        public float shellEjectForce = 3f;

        [Header("Magazine")]
        public Transform magazineSocket;
        public GameObject magazinePrefab;

        [Header("Attachment Sockets")]
        public Transform opticSocket;
        public Transform barrelSocket;
        public Transform gripSocket;
        public Transform magazineModSocket;
        public Transform stockSocket;
        public Transform laserSocket;

        [Header("Animation")]
        public RuntimeAnimatorController weaponAnimator;
        public float drawTime = 0.5f;
        public float holsterTime = 0.4f;

        [Header("Audio")]
        public Vector3 audioSourcePosition;

        [Header("VFX")]
        public Vector3 muzzleFlashOffset;
        public float muzzleFlashScale = 1f;

        [Header("Components To Add")]
        public string[] requiredComponents = new string[]
        {
            "WeaponController",
            "RecoilController",
            "AudioSource"
        };
    }

    #endregion

    #region Item Prefab Configuration

    [CreateAssetMenu(fileName = "Item Prefab Config", menuName = "Dead Frontier/Prefabs/Item Configuration")]
    public class ItemPrefabConfig : ScriptableObject
    {
        [Header("Model")]
        public GameObject worldModelPrefab;
        public GameObject inventoryModelPrefab;
        public Gameplay.LootItemDataSO itemData;

        [Header("Physics")]
        public bool usePhysics = true;
        public float mass = 1f;
        public float drag = 0.5f;
        public float angularDrag = 0.5f;
        public bool useGravity = true;
        public CollisionDetectionMode collisionMode = CollisionDetectionMode.Discrete;

        [Header("Collider")]
        public ColliderType colliderType = ColliderType.Box;
        public Vector3 colliderSize = Vector3.one;
        public Vector3 colliderCenter = Vector3.zero;
        public bool isTrigger = false;

        [Header("Interaction")]
        public float interactionRadius = 1.5f;
        public Vector3 interactionCenter = Vector3.zero;
        public float pickupTime = 0f;
        public bool requireHold = false;

        [Header("Visual")]
        public bool useOutline = true;
        public Color outlineColor = Color.yellow;
        public float outlineWidth = 2f;
        public bool useHighlight = true;
        public float highlightPulseSpeed = 2f;

        [Header("Audio")]
        public AudioClip pickupSound;
        public AudioClip dropSound;
        public AudioClip impactSound;

        [Header("VFX")]
        public GameObject pickupVFXPrefab;
        public GameObject idleVFXPrefab;
        public bool useGlow = false;
        public Color glowColor = Color.white;

        [Header("LOD")]
        public bool useLOD = false;
        public float[] lodDistances = new float[] { 10f, 25f };

        [Header("Network")]
        public bool syncPhysics = true;
        public float despawnTime = 300f; // 5 minutes

        [Header("Components To Add")]
        public string[] requiredComponents = new string[]
        {
            "WorldItem",
            "Rigidbody",
            "Collider",
            "NetworkObject"
        };
    }

    public enum ColliderType
    {
        Box,
        Sphere,
        Capsule,
        Mesh
    }

    #endregion

    #region Vehicle Prefab Configuration

    [CreateAssetMenu(fileName = "Vehicle Prefab Config", menuName = "Dead Frontier/Prefabs/Vehicle Configuration")]
    public class VehiclePrefabConfig : ScriptableObject
    {
        [Header("Model")]
        public GameObject vehicleModelPrefab;
        public Vehicles.VehicleDataSO vehicleData;

        [Header("Physics")]
        public float mass = 1500f;
        public float drag = 0.1f;
        public Vector3 centerOfMass = new Vector3(0, 0.3f, 0);

        [Header("Wheels")]
        public WheelConfig[] wheels;

        [Header("Seats")]
        public SeatConfig[] seats;

        [Header("Collision")]
        public Collider[] bodyColliders;
        public LayerMask collisionLayers;

        [Header("Damage")]
        public Transform[] damageParts;
        public GameObject[] damageEffects;
        public float maxHealth = 1000f;

        [Header("Audio")]
        public Transform engineAudioPosition;
        public Transform exhaustAudioPosition;

        [Header("VFX")]
        public Transform[] exhaustPoints;
        public Transform[] wheelDustPoints;
        public Transform[] lightTransforms;

        [Header("Entry/Exit")]
        public Transform[] entryPoints;
        public float entryRadius = 2f;

        [Header("Components To Add")]
        public string[] requiredComponents = new string[]
        {
            "VehicleController",
            "VehicleHealth",
            "VehicleAudio",
            "Rigidbody",
            "NetworkObject"
        };
    }

    [Serializable]
    public class WheelConfig
    {
        public string wheelName;
        public Transform wheelTransform;
        public WheelCollider wheelCollider;
        public bool isPowered;
        public bool isSteering;
        public bool hasBrake;
        public float suspensionDistance = 0.3f;
        public float suspensionSpring = 35000f;
        public float suspensionDamper = 4500f;
    }

    [Serializable]
    public class SeatConfig
    {
        public string seatName;
        public Vehicles.SeatType seatType;
        public Transform seatTransform;
        public Transform exitPoint;
        public RuntimeAnimatorController seatAnimator;
        public bool canShoot;
        public float shootingArc = 180f;
    }

    #endregion

    #region Building Prefab Configuration

    [CreateAssetMenu(fileName = "Building Prefab Config", menuName = "Dead Frontier/Prefabs/Building Configuration")]
    public class BuildingPrefabConfig : ScriptableObject
    {
        [Header("Model")]
        public GameObject buildingModelPrefab;
        public GameObject blueprintPrefab;
        public GameObject constructionPrefab;

        [Header("Placement")]
        public Vector3 placementSize = new Vector3(2, 2, 2);
        public Vector3 placementOffset = Vector3.zero;
        public bool requiresFoundation;
        public bool canStackVertically;
        public bool canAttachToWall;
        public LayerMask placementBlockingLayers;

        [Header("Building")]
        public float buildTime = 5f;
        public int requiredMaterials = 10;
        public string[] requiredResourceIds;
        public int[] requiredResourceAmounts;

        [Header("Durability")]
        public float maxHealth = 500f;
        public float repairRate = 10f;
        public bool isDestructible = true;

        [Header("Snap Points")]
        public SnapPoint[] snapPoints;

        [Header("Upgrades")]
        public BuildingUpgrade[] upgrades;

        [Header("Interaction")]
        public bool isInteractable;
        public string interactionType;
        public Transform interactionPoint;

        [Header("Network")]
        public bool syncState = true;
        public float stateUpdateRate = 1f;

        [Header("Components To Add")]
        public string[] requiredComponents = new string[]
        {
            "BuildingController",
            "BuildingHealth",
            "NetworkObject"
        };
    }

    [Serializable]
    public class SnapPoint
    {
        public string snapPointId;
        public Transform snapTransform;
        public SnapPointType snapType;
        public string[] compatibleSnapTypes;
        public bool isOccupied;
    }

    public enum SnapPointType
    {
        Foundation,
        Wall,
        Floor,
        Ceiling,
        Door,
        Window,
        Stairs,
        Roof,
        Generic
    }

    [Serializable]
    public class BuildingUpgrade
    {
        public string upgradeId;
        public string upgradeName;
        public GameObject upgradedModelPrefab;
        public int upgradeCost;
        public string[] requiredResources;
        public float upgradeTime;
        public float healthBonus;
    }

    #endregion

    #region Companion Prefab Configuration

    [CreateAssetMenu(fileName = "Companion Prefab Config", menuName = "Dead Frontier/Prefabs/Companion Configuration")]
    public class CompanionPrefabConfig : ScriptableObject
    {
        [Header("Model")]
        public GameObject companionModelPrefab;
        public Avatar companionAvatar;
        public RuntimeAnimatorController animatorController;
        public Companions.CompanionDataSO companionData;

        [Header("Collision")]
        public float capsuleRadius = 0.3f;
        public float capsuleHeight = 0.8f;
        public Vector3 capsuleCenter = new Vector3(0, 0.4f, 0);

        [Header("AI")]
        public float followDistance = 3f;
        public float maxFollowDistance = 20f;
        public float teleportDistance = 30f;
        public float agentSpeed = 5f;
        public float agentAngularSpeed = 360f;

        [Header("Combat")]
        public Transform attackOrigin;
        public float attackRange = 3f;
        public float aggroRange = 15f;

        [Header("Interaction")]
        public float petRadius = 2f;
        public Transform petInteractionPoint;

        [Header("Accessories")]
        public Transform headAccessorySocket;
        public Transform neckAccessorySocket;
        public Transform backAccessorySocket;
        public Transform bodyAccessorySocket;

        [Header("Audio")]
        public float audioMinDistance = 1f;
        public float audioMaxDistance = 15f;

        [Header("Components To Add")]
        public string[] requiredComponents = new string[]
        {
            "CompanionAI",
            "CompanionAbilities",
            "NavMeshAgent",
            "NetworkObject",
            "AudioSource"
        };
    }

    #endregion

    #region Prefab Database

    [CreateAssetMenu(fileName = "Prefab Database", menuName = "Dead Frontier/Prefabs/Database")]
    public class PrefabDatabase : ScriptableObject
    {
        [Header("Players")]
        public PlayerPrefabConfig[] playerConfigs;

        [Header("Zombies")]
        public ZombiePrefabConfig[] zombieConfigs;

        [Header("Weapons")]
        public WeaponPrefabConfig[] weaponConfigs;

        [Header("Items")]
        public ItemPrefabConfig[] itemConfigs;

        [Header("Vehicles")]
        public VehiclePrefabConfig[] vehicleConfigs;

        [Header("Buildings")]
        public BuildingPrefabConfig[] buildingConfigs;

        [Header("Companions")]
        public CompanionPrefabConfig[] companionConfigs;

        private Dictionary<string, PlayerPrefabConfig> _playerLookup;
        private Dictionary<string, ZombiePrefabConfig> _zombieLookup;
        private Dictionary<string, WeaponPrefabConfig> _weaponLookup;
        private Dictionary<string, ItemPrefabConfig> _itemLookup;
        private Dictionary<string, VehiclePrefabConfig> _vehicleLookup;
        private Dictionary<string, BuildingPrefabConfig> _buildingLookup;
        private Dictionary<string, CompanionPrefabConfig> _companionLookup;

        public void Initialize()
        {
            _playerLookup = new Dictionary<string, PlayerPrefabConfig>();
            _zombieLookup = new Dictionary<string, ZombiePrefabConfig>();
            _weaponLookup = new Dictionary<string, WeaponPrefabConfig>();
            _itemLookup = new Dictionary<string, ItemPrefabConfig>();
            _vehicleLookup = new Dictionary<string, VehiclePrefabConfig>();
            _buildingLookup = new Dictionary<string, BuildingPrefabConfig>();
            _companionLookup = new Dictionary<string, CompanionPrefabConfig>();

            // Index all prefabs by their data IDs
            Debug.Log("[PrefabDatabase] Initialized with all prefab configurations");
        }

        public PlayerPrefabConfig GetPlayerConfig(string characterId) => _playerLookup.TryGetValue(characterId, out var c) ? c : null;
        public ZombiePrefabConfig GetZombieConfig(string zombieId) => _zombieLookup.TryGetValue(zombieId, out var z) ? z : null;
        public WeaponPrefabConfig GetWeaponConfig(string weaponId) => _weaponLookup.TryGetValue(weaponId, out var w) ? w : null;
        public ItemPrefabConfig GetItemConfig(string itemId) => _itemLookup.TryGetValue(itemId, out var i) ? i : null;
        public VehiclePrefabConfig GetVehicleConfig(string vehicleId) => _vehicleLookup.TryGetValue(vehicleId, out var v) ? v : null;
        public BuildingPrefabConfig GetBuildingConfig(string buildingId) => _buildingLookup.TryGetValue(buildingId, out var b) ? b : null;
        public CompanionPrefabConfig GetCompanionConfig(string companionId) => _companionLookup.TryGetValue(companionId, out var c) ? c : null;
    }

    #endregion
}
