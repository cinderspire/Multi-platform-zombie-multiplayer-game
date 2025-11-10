using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.BaseBuilding
{
    public class BaseBuildingSystem : NetworkBehaviour
    {
        public static BaseBuildingSystem Instance { get; private set; }

        [Header("Base Configuration")]
        [SerializeField] private int maxStructuresPerBase = 200;
        [SerializeField] private float structurePlacementRange = 5f;
        [SerializeField] private bool enableStructureHealth = true;

        private Dictionary<ulong, PlayerBase> playerBases = new Dictionary<ulong, PlayerBase>();
        private Dictionary<string, Structure> activeStructures = new Dictionary<string, Structure>();
        private Dictionary<string, StructureDefinition> structureDatabase = new Dictionary<string, StructureDefinition>();

        public event Action<ulong, string> OnStructurePlaced;
        public event Action<string> OnStructureDestroyed;
        public event Action<ulong, string> OnStructureUpgraded;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer) { InitializeStructureDatabase(); }
        }

        private void InitializeStructureDatabase()
        {
            // Walls
            structureDatabase["wall_wood"] = new StructureDefinition
            {
                structureId = "wall_wood",
                structureName = "Wooden Wall",
                structureType = StructureType.Wall,
                rarity = StructureRarity.Common,
                maxHealth = 500f,
                buildTime = 5f,
                buildCost = new Dictionary<string, int> { { "wood", 10 }, { "nails", 5 } },
                tier = 1,
                canUpgrade = true,
                upgradeTarget = "wall_metal",
                defensiveValue = 10,
                unlockLevel = 1,
                placementSize = new Vector3(4f, 3f, 0.2f)
            };

            structureDatabase["wall_metal"] = new StructureDefinition
            {
                structureId = "wall_metal",
                structureName = "Metal Wall",
                structureType = StructureType.Wall,
                rarity = StructureRarity.Uncommon,
                maxHealth = 1500f,
                buildTime = 10f,
                buildCost = new Dictionary<string, int> { { "metal", 15 }, { "screws", 10 } },
                tier = 2,
                canUpgrade = true,
                upgradeTarget = "wall_reinforced",
                defensiveValue = 25,
                unlockLevel = 10,
                placementSize = new Vector3(4f, 3f, 0.3f)
            };

            structureDatabase["wall_reinforced"] = new StructureDefinition
            {
                structureId = "wall_reinforced",
                structureName = "Reinforced Wall",
                structureType = StructureType.Wall,
                rarity = StructureRarity.Rare,
                maxHealth = 3000f,
                buildTime = 20f,
                buildCost = new Dictionary<string, int> { { "steel", 20 }, { "concrete", 15 } },
                tier = 3,
                canUpgrade = false,
                defensiveValue = 50,
                unlockLevel = 25,
                placementSize = new Vector3(4f, 3f, 0.4f)
            };

            // Doors
            structureDatabase["door_wood"] = new StructureDefinition
            {
                structureId = "door_wood",
                structureName = "Wooden Door",
                structureType = StructureType.Door,
                rarity = StructureRarity.Common,
                maxHealth = 300f,
                buildTime = 3f,
                buildCost = new Dictionary<string, int> { { "wood", 8 }, { "hinges", 2 } },
                tier = 1,
                canUpgrade = true,
                upgradeTarget = "door_metal",
                defensiveValue = 5,
                unlockLevel = 1,
                placementSize = new Vector3(2f, 3f, 0.2f)
            };

            // Traps
            structureDatabase["trap_spike"] = new StructureDefinition
            {
                structureId = "trap_spike",
                structureName = "Spike Trap",
                structureType = StructureType.Trap,
                rarity = StructureRarity.Common,
                maxHealth = 200f,
                buildTime = 2f,
                buildCost = new Dictionary<string, int> { { "metal", 5 }, { "wood", 5 } },
                tier = 1,
                canUpgrade = false,
                defensiveValue = 15,
                trapDamage = 50f,
                unlockLevel = 5,
                placementSize = new Vector3(2f, 0.5f, 2f)
            };

            structureDatabase["trap_mine"] = new StructureDefinition
            {
                structureId = "trap_mine",
                structureName = "Land Mine",
                structureType = StructureType.Trap,
                rarity = StructureRarity.Uncommon,
                maxHealth = 50f,
                buildTime = 3f,
                buildCost = new Dictionary<string, int> { { "explosives", 2 }, { "metal", 3 } },
                tier = 1,
                canUpgrade = false,
                defensiveValue = 30,
                trapDamage = 200f,
                explosionRadius = 5f,
                unlockLevel = 15,
                placementSize = new Vector3(0.5f, 0.2f, 0.5f)
            };

            // Turrets
            structureDatabase["turret_basic"] = new StructureDefinition
            {
                structureId = "turret_basic",
                structureName = "Auto Turret",
                structureType = StructureType.Turret,
                rarity = StructureRarity.Rare,
                maxHealth = 800f,
                buildTime = 15f,
                buildCost = new Dictionary<string, int> { { "metal", 25 }, { "electronics", 10 }, { "ammo", 100 } },
                tier = 1,
                canUpgrade = true,
                upgradeTarget = "turret_advanced",
                defensiveValue = 40,
                turretDamage = 25f,
                turretRange = 30f,
                turretFireRate = 0.2f,
                unlockLevel = 20,
                placementSize = new Vector3(1f, 2f, 1f)
            };

            // Storage
            structureDatabase["storage_small"] = new StructureDefinition
            {
                structureId = "storage_small",
                structureName = "Small Storage",
                structureType = StructureType.Storage,
                rarity = StructureRarity.Common,
                maxHealth = 400f,
                buildTime = 5f,
                buildCost = new Dictionary<string, int> { { "wood", 15 } },
                tier = 1,
                canUpgrade = true,
                upgradeTarget = "storage_medium",
                storageCapacity = 20,
                unlockLevel = 1,
                placementSize = new Vector3(2f, 2f, 2f)
            };

            structureDatabase["storage_medium"] = new StructureDefinition
            {
                structureId = "storage_medium",
                structureName = "Medium Storage",
                structureType = StructureType.Storage,
                rarity = StructureRarity.Uncommon,
                maxHealth = 800f,
                buildTime = 10f,
                buildCost = new Dictionary<string, int> { { "metal", 20 } },
                tier = 2,
                canUpgrade = true,
                upgradeTarget = "storage_large",
                storageCapacity = 50,
                unlockLevel = 10,
                placementSize = new Vector3(3f, 2.5f, 3f)
            };

            // Utility
            structureDatabase["workbench"] = new StructureDefinition
            {
                structureId = "workbench",
                structureName = "Workbench",
                structureType = StructureType.Utility,
                rarity = StructureRarity.Common,
                maxHealth = 300f,
                buildTime = 8f,
                buildCost = new Dictionary<string, int> { { "wood", 20 }, { "metal", 5 } },
                tier = 1,
                canUpgrade = true,
                upgradeTarget = "workbench_advanced",
                craftingSpeedBonus = 0.25f,
                unlockLevel = 3,
                placementSize = new Vector3(2f, 1f, 1f)
            };

            structureDatabase["generator"] = new StructureDefinition
            {
                structureId = "generator",
                structureName = "Generator",
                structureType = StructureType.Utility,
                rarity = StructureRarity.Rare,
                maxHealth = 600f,
                buildTime = 15f,
                buildCost = new Dictionary<string, int> { { "metal", 30 }, { "electronics", 15 }, { "fuel", 10 } },
                tier = 1,
                canUpgrade = false,
                powerOutput = 100,
                unlockLevel = 18,
                placementSize = new Vector3(1.5f, 1.5f, 1.5f)
            };

            // Foundations
            structureDatabase["foundation_wood"] = new StructureDefinition
            {
                structureId = "foundation_wood",
                structureName = "Wooden Foundation",
                structureType = StructureType.Foundation,
                rarity = StructureRarity.Common,
                maxHealth = 1000f,
                buildTime = 10f,
                buildCost = new Dictionary<string, int> { { "wood", 25 } },
                tier = 1,
                canUpgrade = true,
                upgradeTarget = "foundation_concrete",
                defensiveValue = 5,
                unlockLevel = 1,
                placementSize = new Vector3(4f, 0.5f, 4f)
            };
        }

        [ServerRpc(RequireOwnership = false)]
        public void PlaceStructureServerRpc(ulong playerId, string structureId, Vector3 position, Quaternion rotation, ServerRpcParams rpcParams = default)
        {
            if (!playerBases.ContainsKey(playerId))
            {
                playerBases[playerId] = new PlayerBase
                {
                    ownerId = playerId,
                    baseId = $"base_{playerId}",
                    structures = new List<string>(),
                    creationDate = DateTime.UtcNow,
                    defensiveRating = 0
                };
            }

            var playerBase = playerBases[playerId];
            if (playerBase.structures.Count >= maxStructuresPerBase) return;

            if (structureDatabase.TryGetValue(structureId, out var structureDef))
            {
                // Check if player has resources
                bool hasResources = true;
                foreach (var cost in structureDef.buildCost)
                {
                    // Would check inventory here
                }

                if (hasResources)
                {
                    var structure = new Structure
                    {
                        structureId = $"{structureId}_{Guid.NewGuid()}",
                        definition = structureDef,
                        ownerId = playerId,
                        position = position,
                        rotation = rotation,
                        currentHealth = structureDef.maxHealth,
                        placementTime = DateTime.UtcNow,
                        tier = structureDef.tier
                    };

                    activeStructures[structure.structureId] = structure;
                    playerBase.structures.Add(structure.structureId);
                    playerBase.defensiveRating += structureDef.defensiveValue;

                    OnStructurePlaced?.Invoke(playerId, structure.structureId);
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void DestroyStructureServerRpc(string structureId, ServerRpcParams rpcParams = default)
        {
            if (!activeStructures.TryGetValue(structureId, out var structure)) return;

            if (playerBases.TryGetValue(structure.ownerId, out var playerBase))
            {
                playerBase.structures.Remove(structureId);
                playerBase.defensiveRating -= structure.definition.defensiveValue;
            }

            activeStructures.Remove(structureId);
            OnStructureDestroyed?.Invoke(structureId);
        }

        public PlayerBase GetPlayerBase(ulong playerId)
        {
            return playerBases.GetValueOrDefault(playerId);
        }
    }

    [Serializable]
    public class PlayerBase
    {
        public ulong ownerId;
        public string baseId;
        public List<string> structures;
        public DateTime creationDate;
        public int defensiveRating;
    }

    [Serializable]
    public class Structure
    {
        public string structureId;
        public StructureDefinition definition;
        public ulong ownerId;
        public Vector3 position;
        public Quaternion rotation;
        public float currentHealth;
        public DateTime placementTime;
        public int tier;
    }

    [Serializable]
    public class StructureDefinition
    {
        public string structureId;
        public string structureName;
        public StructureType structureType;
        public StructureRarity rarity;
        public float maxHealth;
        public float buildTime;
        public Dictionary<string, int> buildCost;
        public int tier;
        public bool canUpgrade;
        public string upgradeTarget;
        public int defensiveValue;
        public float trapDamage;
        public float explosionRadius;
        public float turretDamage;
        public float turretRange;
        public float turretFireRate;
        public int storageCapacity;
        public float craftingSpeedBonus;
        public int powerOutput;
        public int unlockLevel;
        public Vector3 placementSize;
    }

    public enum StructureType { Wall, Door, Foundation, Roof, Stairs, Trap, Turret, Storage, Utility, Decoration }
    public enum StructureRarity { Common, Uncommon, Rare, Epic, Legendary }
}
