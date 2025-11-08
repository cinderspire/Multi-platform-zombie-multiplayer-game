using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;
using System;

namespace DeadFrontier.Vehicles
{
    /// <summary>
    /// Comprehensive vehicle system for extraction shooters.
    /// Handles vehicle spawning, controls, damage, fuel, extraction, and multiplayer synchronization.
    /// Supports cars, trucks, helicopters, and armored vehicles with storage capacity.
    /// </summary>
    public class VehicleSystem : NetworkBehaviour
    {
        public static VehicleSystem Instance { get; private set; }

        [Header("Vehicle Spawn Settings")]
        [SerializeField] private VehicleSpawnPoint[] vehicleSpawnPoints;
        [SerializeField] private VehicleData[] availableVehicles;
        [SerializeField] private int maxSimultaneousVehicles = 20;
        [SerializeField] private float vehicleRespawnTime = 300f; // 5 minutes

        [Header("Extraction Settings")]
        [SerializeField] private ExtractionVehiclePoint[] extractionPoints;
        [SerializeField] private float extractionCountdownTime = 30f; // Time to extract
        [SerializeField] private float extractionArrivalTime = 120f; // Time for vehicle to arrive

        [Header("Fuel Settings")]
        [SerializeField] private bool enableFuelSystem = true;
        [SerializeField] private float fuelConsumptionRate = 1f; // Fuel per second when driving
        [SerializeField] private float idleFuelConsumption = 0.1f; // Fuel per second when idle

        // Active vehicles
        private Dictionary<ulong, Vehicle> activeVehicles = new Dictionary<ulong, Vehicle>();
        private Dictionary<ulong, VehicleState> vehicleStates = new Dictionary<ulong, VehicleState>();
        private Dictionary<Vector3, float> spawnPointCooldowns = new Dictionary<Vector3, float>();

        // Extraction requests
        private Dictionary<string, ExtractionRequest> activeExtractions = new Dictionary<string, ExtractionRequest>();

        // Events
        public event Action<Vehicle, ulong> OnPlayerEnteredVehicle;
        public event Action<Vehicle, ulong> OnPlayerExitedVehicle;
        public event Action<Vehicle> OnVehicleDestroyed;
        public event Action<Vehicle, float> OnVehicleDamaged;
        public event Action<string, float> OnExtractionRequested;
        public event Action<string> OnExtractionArrived;
        public event Action<ulong, string> OnPlayerExtracted;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                InitializeVehicleSystem();
            }
        }

        private void Update()
        {
            if (IsServer)
            {
                UpdateVehicles();
                UpdateExtractions();
                UpdateSpawnPointCooldowns();
            }
        }

        #region Initialization

        private void InitializeVehicleSystem()
        {
            // Spawn initial vehicles at spawn points
            SpawnInitialVehicles();

            Debug.Log($"[VehicleSystem] Initialized with {vehicleSpawnPoints.Length} spawn points and {availableVehicles.Length} vehicle types");
        }

        private void SpawnInitialVehicles()
        {
            foreach (var spawnPoint in vehicleSpawnPoints)
            {
                if (spawnPoint.spawnOnStart && activeVehicles.Count < maxSimultaneousVehicles)
                {
                    SpawnVehicle(spawnPoint.position, spawnPoint.rotation, spawnPoint.vehicleType);
                }
            }
        }

        #endregion

        #region Vehicle Spawning

        public Vehicle SpawnVehicle(Vector3 position, Quaternion rotation, VehicleType vehicleType)
        {
            if (!IsServer) return null;
            if (activeVehicles.Count >= maxSimultaneousVehicles) return null;

            // Get vehicle data
            var vehicleData = availableVehicles.FirstOrDefault(v => v.vehicleType == vehicleType);
            if (vehicleData == null)
            {
                Debug.LogError($"[VehicleSystem] Vehicle type {vehicleType} not found in database");
                return null;
            }

            // Instantiate vehicle
            GameObject vehicleObj = Instantiate(vehicleData.vehiclePrefab, position, rotation);
            Vehicle vehicle = vehicleObj.GetComponent<Vehicle>();

            if (vehicle == null)
            {
                Debug.LogError($"[VehicleSystem] Vehicle prefab missing Vehicle component");
                Destroy(vehicleObj);
                return null;
            }

            // Initialize vehicle
            vehicle.Initialize(vehicleData, GenerateVehicleId());
            vehicle.GetComponent<NetworkObject>().Spawn();

            // Create vehicle state
            var state = new VehicleState
            {
                vehicleId = vehicle.VehicleId,
                health = vehicleData.maxHealth,
                fuel = vehicleData.maxFuel,
                isEngineRunning = false,
                driver = null,
                passengers = new List<ulong>()
            };

            activeVehicles[vehicle.VehicleId] = vehicle;
            vehicleStates[vehicle.VehicleId] = state;

            // Set spawn point cooldown
            spawnPointCooldowns[position] = Time.time + vehicleRespawnTime;

            Debug.Log($"[VehicleSystem] Spawned {vehicleType} at {position}");

            return vehicle;
        }

        public void DespawnVehicle(ulong vehicleId, bool respawn = true)
        {
            if (!IsServer) return;
            if (!activeVehicles.ContainsKey(vehicleId)) return;

            var vehicle = activeVehicles[vehicleId];
            Vector3 spawnPosition = vehicle.transform.position;

            // Eject all occupants
            EjectAllOccupants(vehicleId);

            // Destroy vehicle
            activeVehicles.Remove(vehicleId);
            vehicleStates.Remove(vehicleId);

            vehicle.GetComponent<NetworkObject>().Despawn();
            Destroy(vehicle.gameObject);

            OnVehicleDestroyed?.Invoke(vehicle);

            // Schedule respawn if needed
            if (respawn)
            {
                spawnPointCooldowns[spawnPosition] = Time.time + vehicleRespawnTime;
            }

            Debug.Log($"[VehicleSystem] Despawned vehicle {vehicleId}");
        }

        private ulong GenerateVehicleId()
        {
            return (ulong)UnityEngine.Random.Range(1000000, 9999999);
        }

        #endregion

        #region Vehicle Updates

        private void UpdateVehicles()
        {
            var vehiclesToDestroy = new List<ulong>();

            foreach (var kvp in vehicleStates)
            {
                var vehicleId = kvp.Key;
                var state = kvp.Value;

                if (!activeVehicles.ContainsKey(vehicleId)) continue;

                var vehicle = activeVehicles[vehicleId];

                // Update fuel consumption
                if (enableFuelSystem && state.isEngineRunning)
                {
                    float consumption = state.driver.HasValue ? fuelConsumptionRate : idleFuelConsumption;
                    state.fuel -= consumption * Time.deltaTime;

                    if (state.fuel <= 0f)
                    {
                        state.fuel = 0f;
                        state.isEngineRunning = false;
                        vehicle.StopEngine();
                    }
                }

                // Check if vehicle should be destroyed
                if (state.health <= 0f)
                {
                    vehiclesToDestroy.Add(vehicleId);
                }
            }

            // Destroy vehicles
            foreach (var vehicleId in vehiclesToDestroy)
            {
                DespawnVehicle(vehicleId, true);
            }
        }

        private void UpdateSpawnPointCooldowns()
        {
            var pointsToRespawn = new List<Vector3>();

            foreach (var kvp in spawnPointCooldowns.ToList())
            {
                if (Time.time >= kvp.Value)
                {
                    pointsToRespawn.Add(kvp.Key);
                }
            }

            foreach (var position in pointsToRespawn)
            {
                // Find spawn point data
                var spawnPoint = vehicleSpawnPoints.FirstOrDefault(sp => Vector3.Distance(sp.position, position) < 1f);
                if (spawnPoint != null && spawnPoint.respawnOnDestroy)
                {
                    SpawnVehicle(position, spawnPoint.rotation, spawnPoint.vehicleType);
                }

                spawnPointCooldowns.Remove(position);
            }
        }

        #endregion

        #region Vehicle Entry/Exit

        [ServerRpc(RequireOwnership = false)]
        public void RequestEnterVehicleServerRpc(ulong vehicleId, ulong playerId, VehicleSeat seat)
        {
            if (!activeVehicles.ContainsKey(vehicleId)) return;
            if (!vehicleStates.ContainsKey(vehicleId)) return;

            var vehicle = activeVehicles[vehicleId];
            var state = vehicleStates[vehicleId];
            var vehicleData = vehicle.VehicleData;

            // Check if seat is available
            if (seat == VehicleSeat.Driver && state.driver.HasValue)
            {
                Debug.LogWarning($"[VehicleSystem] Driver seat already occupied in vehicle {vehicleId}");
                return;
            }

            if (seat == VehicleSeat.Passenger && state.passengers.Count >= vehicleData.maxPassengers)
            {
                Debug.LogWarning($"[VehicleSystem] All passenger seats occupied in vehicle {vehicleId}");
                return;
            }

            // Assign player to seat
            if (seat == VehicleSeat.Driver)
            {
                state.driver = playerId;
            }
            else
            {
                state.passengers.Add(playerId);
            }

            // Notify clients
            EnterVehicleClientRpc(vehicleId, playerId, seat);

            OnPlayerEnteredVehicle?.Invoke(vehicle, playerId);

            Debug.Log($"[VehicleSystem] Player {playerId} entered vehicle {vehicleId} as {seat}");
        }

        [ClientRpc]
        private void EnterVehicleClientRpc(ulong vehicleId, ulong playerId, VehicleSeat seat)
        {
            if (!activeVehicles.ContainsKey(vehicleId)) return;

            var vehicle = activeVehicles[vehicleId];
            vehicle.OnPlayerEntered(playerId, seat);
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestExitVehicleServerRpc(ulong vehicleId, ulong playerId)
        {
            if (!activeVehicles.ContainsKey(vehicleId)) return;
            if (!vehicleStates.ContainsKey(vehicleId)) return;

            var vehicle = activeVehicles[vehicleId];
            var state = vehicleStates[vehicleId];

            // Remove player from vehicle
            if (state.driver == playerId)
            {
                state.driver = null;
                state.isEngineRunning = false; // Stop engine when driver exits
            }
            else
            {
                state.passengers.Remove(playerId);
            }

            // Notify clients
            ExitVehicleClientRpc(vehicleId, playerId);

            OnPlayerExitedVehicle?.Invoke(vehicle, playerId);

            Debug.Log($"[VehicleSystem] Player {playerId} exited vehicle {vehicleId}");
        }

        [ClientRpc]
        private void ExitVehicleClientRpc(ulong vehicleId, ulong playerId)
        {
            if (!activeVehicles.ContainsKey(vehicleId)) return;

            var vehicle = activeVehicles[vehicleId];
            vehicle.OnPlayerExited(playerId);
        }

        private void EjectAllOccupants(ulong vehicleId)
        {
            if (!vehicleStates.ContainsKey(vehicleId)) return;

            var state = vehicleStates[vehicleId];

            if (state.driver.HasValue)
            {
                RequestExitVehicleServerRpc(vehicleId, state.driver.Value);
            }

            foreach (var passengerId in state.passengers.ToList())
            {
                RequestExitVehicleServerRpc(vehicleId, passengerId);
            }
        }

        #endregion

        #region Vehicle Controls

        [ServerRpc(RequireOwnership = false)]
        public void RequestStartEngineServerRpc(ulong vehicleId, ulong playerId)
        {
            if (!vehicleStates.ContainsKey(vehicleId)) return;

            var state = vehicleStates[vehicleId];

            // Only driver can start engine
            if (state.driver != playerId) return;

            // Check fuel
            if (enableFuelSystem && state.fuel <= 0f)
            {
                Debug.LogWarning($"[VehicleSystem] Cannot start engine - no fuel");
                return;
            }

            state.isEngineRunning = true;

            StartEngineClientRpc(vehicleId);

            Debug.Log($"[VehicleSystem] Engine started for vehicle {vehicleId}");
        }

        [ClientRpc]
        private void StartEngineClientRpc(ulong vehicleId)
        {
            if (!activeVehicles.ContainsKey(vehicleId)) return;

            var vehicle = activeVehicles[vehicleId];
            vehicle.StartEngine();
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestStopEngineServerRpc(ulong vehicleId, ulong playerId)
        {
            if (!vehicleStates.ContainsKey(vehicleId)) return;

            var state = vehicleStates[vehicleId];

            // Only driver can stop engine
            if (state.driver != playerId) return;

            state.isEngineRunning = false;

            StopEngineClientRpc(vehicleId);

            Debug.Log($"[VehicleSystem] Engine stopped for vehicle {vehicleId}");
        }

        [ClientRpc]
        private void StopEngineClientRpc(ulong vehicleId)
        {
            if (!activeVehicles.ContainsKey(vehicleId)) return;

            var vehicle = activeVehicles[vehicleId];
            vehicle.StopEngine();
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestHornServerRpc(ulong vehicleId)
        {
            HornClientRpc(vehicleId);
        }

        [ClientRpc]
        private void HornClientRpc(ulong vehicleId)
        {
            if (!activeVehicles.ContainsKey(vehicleId)) return;

            var vehicle = activeVehicles[vehicleId];
            vehicle.PlayHorn();
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestToggleLightsServerRpc(ulong vehicleId)
        {
            if (!activeVehicles.ContainsKey(vehicleId)) return;

            var vehicle = activeVehicles[vehicleId];
            ToggleLightsClientRpc(vehicleId);
        }

        [ClientRpc]
        private void ToggleLightsClientRpc(ulong vehicleId)
        {
            if (!activeVehicles.ContainsKey(vehicleId)) return;

            var vehicle = activeVehicles[vehicleId];
            vehicle.ToggleLights();
        }

        #endregion

        #region Vehicle Damage

        public void DamageVehicle(ulong vehicleId, float damage, Vector3 hitPoint, DamageSource source)
        {
            if (!IsServer) return;
            if (!vehicleStates.ContainsKey(vehicleId)) return;

            var state = vehicleStates[vehicleId];
            var vehicle = activeVehicles[vehicleId];

            state.health -= damage;
            state.health = Mathf.Max(0f, state.health);

            OnVehicleDamaged?.Invoke(vehicle, damage);

            // Apply damage to occupants if significant damage
            if (damage > 50f)
            {
                DamageOccupants(vehicleId, damage * 0.3f);
            }

            DamageVehicleClientRpc(vehicleId, damage, hitPoint);

            Debug.Log($"[VehicleSystem] Vehicle {vehicleId} took {damage} damage, health: {state.health}");
        }

        [ClientRpc]
        private void DamageVehicleClientRpc(ulong vehicleId, float damage, Vector3 hitPoint)
        {
            if (!activeVehicles.ContainsKey(vehicleId)) return;

            var vehicle = activeVehicles[vehicleId];
            vehicle.OnDamaged(damage, hitPoint);
        }

        private void DamageOccupants(ulong vehicleId, float damage)
        {
            if (!vehicleStates.ContainsKey(vehicleId)) return;

            var state = vehicleStates[vehicleId];

            // Damage driver
            if (state.driver.HasValue && Combat.HealthManager.Instance != null)
            {
                Combat.HealthManager.Instance.DealDamage(state.driver.Value, damage, Combat.DamageType.Physical);
            }

            // Damage passengers
            foreach (var passengerId in state.passengers)
            {
                if (Combat.HealthManager.Instance != null)
                {
                    Combat.HealthManager.Instance.DealDamage(passengerId, damage, Combat.DamageType.Physical);
                }
            }
        }

        #endregion

        #region Fuel System

        public void RefuelVehicle(ulong vehicleId, float amount)
        {
            if (!IsServer) return;
            if (!vehicleStates.ContainsKey(vehicleId)) return;
            if (!activeVehicles.ContainsKey(vehicleId)) return;

            var state = vehicleStates[vehicleId];
            var vehicle = activeVehicles[vehicleId];
            var maxFuel = vehicle.VehicleData.maxFuel;

            state.fuel = Mathf.Min(state.fuel + amount, maxFuel);

            RefuelVehicleClientRpc(vehicleId, state.fuel);

            Debug.Log($"[VehicleSystem] Refueled vehicle {vehicleId} by {amount}, fuel: {state.fuel}/{maxFuel}");
        }

        [ClientRpc]
        private void RefuelVehicleClientRpc(ulong vehicleId, float newFuel)
        {
            if (!activeVehicles.ContainsKey(vehicleId)) return;

            var vehicle = activeVehicles[vehicleId];
            vehicle.UpdateFuel(newFuel);
        }

        #endregion

        #region Extraction System

        public void RequestExtraction(string extractionPointId, ulong playerId)
        {
            if (!IsServer) return;

            var extractionPoint = extractionPoints.FirstOrDefault(ep => ep.extractionId == extractionPointId);
            if (extractionPoint == null)
            {
                Debug.LogError($"[VehicleSystem] Extraction point {extractionPointId} not found");
                return;
            }

            // Check if extraction already active
            if (activeExtractions.ContainsKey(extractionPointId))
            {
                var existing = activeExtractions[extractionPointId];

                // Add player to extraction if not already in
                if (!existing.playersWaiting.Contains(playerId))
                {
                    existing.playersWaiting.Add(playerId);
                    Debug.Log($"[VehicleSystem] Player {playerId} joined extraction at {extractionPointId}");
                }
                return;
            }

            // Create new extraction request
            var extraction = new ExtractionRequest
            {
                extractionPointId = extractionPointId,
                requestTime = Time.time,
                arrivalTime = Time.time + extractionArrivalTime,
                extractionTime = Time.time + extractionArrivalTime + extractionCountdownTime,
                playersWaiting = new List<ulong> { playerId },
                vehicleArrived = false
            };

            activeExtractions[extractionPointId] = extraction;

            OnExtractionRequested?.Invoke(extractionPointId, extractionArrivalTime);

            Debug.Log($"[VehicleSystem] Extraction requested at {extractionPointId}, arrival in {extractionArrivalTime}s");
        }

        private void UpdateExtractions()
        {
            var extractionsToRemove = new List<string>();

            foreach (var kvp in activeExtractions)
            {
                var extractionId = kvp.Key;
                var extraction = kvp.Value;

                // Check if vehicle should arrive
                if (!extraction.vehicleArrived && Time.time >= extraction.arrivalTime)
                {
                    SpawnExtractionVehicle(extractionId);
                    extraction.vehicleArrived = true;

                    OnExtractionArrived?.Invoke(extractionId);
                }

                // Check if extraction time reached
                if (extraction.vehicleArrived && Time.time >= extraction.extractionTime)
                {
                    // Extract all waiting players
                    foreach (var playerId in extraction.playersWaiting)
                    {
                        ExtractPlayer(playerId, extractionId);
                    }

                    // Despawn extraction vehicle
                    if (extraction.vehicleId.HasValue)
                    {
                        DespawnVehicle(extraction.vehicleId.Value, false);
                    }

                    extractionsToRemove.Add(extractionId);
                }
            }

            // Remove completed extractions
            foreach (var extractionId in extractionsToRemove)
            {
                activeExtractions.Remove(extractionId);
            }
        }

        private void SpawnExtractionVehicle(string extractionPointId)
        {
            var extractionPoint = extractionPoints.FirstOrDefault(ep => ep.extractionId == extractionPointId);
            if (extractionPoint == null) return;

            var vehicle = SpawnVehicle(extractionPoint.position, extractionPoint.rotation, extractionPoint.vehicleType);

            if (vehicle != null && activeExtractions.ContainsKey(extractionPointId))
            {
                activeExtractions[extractionPointId].vehicleId = vehicle.VehicleId;
            }

            Debug.Log($"[VehicleSystem] Extraction vehicle arrived at {extractionPointId}");
        }

        private void ExtractPlayer(ulong playerId, string extractionPointId)
        {
            OnPlayerExtracted?.Invoke(playerId, extractionPointId);

            // Handle player extraction (remove from raid, save loot, etc.)
            if (Gameplay.RaidManager.Instance != null)
            {
                Gameplay.RaidManager.Instance.ExtractPlayer(playerId, extractionPointId, true);
            }

            Debug.Log($"[VehicleSystem] Player {playerId} extracted via {extractionPointId}");
        }

        public void CancelExtraction(string extractionPointId, ulong playerId)
        {
            if (!IsServer) return;
            if (!activeExtractions.ContainsKey(extractionPointId)) return;

            var extraction = activeExtractions[extractionPointId];
            extraction.playersWaiting.Remove(playerId);

            // If no players waiting, cancel extraction
            if (extraction.playersWaiting.Count == 0)
            {
                if (extraction.vehicleId.HasValue)
                {
                    DespawnVehicle(extraction.vehicleId.Value, false);
                }

                activeExtractions.Remove(extractionPointId);

                Debug.Log($"[VehicleSystem] Extraction cancelled at {extractionPointId}");
            }
        }

        #endregion

        #region Vehicle Repair

        public void RepairVehicle(ulong vehicleId, float amount, ulong repairerId)
        {
            if (!IsServer) return;
            if (!vehicleStates.ContainsKey(vehicleId)) return;
            if (!activeVehicles.ContainsKey(vehicleId)) return;

            var state = vehicleStates[vehicleId];
            var vehicle = activeVehicles[vehicleId];
            var maxHealth = vehicle.VehicleData.maxHealth;

            float oldHealth = state.health;
            state.health = Mathf.Min(state.health + amount, maxHealth);

            RepairVehicleClientRpc(vehicleId, state.health);

            Debug.Log($"[VehicleSystem] Vehicle {vehicleId} repaired by {amount}, health: {state.health}/{maxHealth}");
        }

        [ClientRpc]
        private void RepairVehicleClientRpc(ulong vehicleId, float newHealth)
        {
            if (!activeVehicles.ContainsKey(vehicleId)) return;

            var vehicle = activeVehicles[vehicleId];
            vehicle.UpdateHealth(newHealth);
        }

        #endregion

        #region Public Getters

        public Vehicle GetVehicle(ulong vehicleId)
        {
            return activeVehicles.ContainsKey(vehicleId) ? activeVehicles[vehicleId] : null;
        }

        public VehicleState GetVehicleState(ulong vehicleId)
        {
            return vehicleStates.ContainsKey(vehicleId) ? vehicleStates[vehicleId] : null;
        }

        public List<Vehicle> GetAllVehicles()
        {
            return new List<Vehicle>(activeVehicles.Values);
        }

        public Vehicle GetNearestVehicle(Vector3 position, float maxDistance = 50f)
        {
            Vehicle nearest = null;
            float nearestDistance = maxDistance;

            foreach (var vehicle in activeVehicles.Values)
            {
                float distance = Vector3.Distance(position, vehicle.transform.position);
                if (distance < nearestDistance)
                {
                    nearest = vehicle;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }

        public bool IsPlayerInVehicle(ulong playerId, out ulong vehicleId)
        {
            foreach (var kvp in vehicleStates)
            {
                var state = kvp.Value;

                if (state.driver == playerId || state.passengers.Contains(playerId))
                {
                    vehicleId = kvp.Key;
                    return true;
                }
            }

            vehicleId = 0;
            return false;
        }

        public ExtractionRequest GetExtractionRequest(string extractionPointId)
        {
            return activeExtractions.ContainsKey(extractionPointId) ? activeExtractions[extractionPointId] : null;
        }

        public float GetFuelPercentage(ulong vehicleId)
        {
            if (!vehicleStates.ContainsKey(vehicleId)) return 0f;
            if (!activeVehicles.ContainsKey(vehicleId)) return 0f;

            var state = vehicleStates[vehicleId];
            var vehicle = activeVehicles[vehicleId];

            return state.fuel / vehicle.VehicleData.maxFuel;
        }

        public float GetHealthPercentage(ulong vehicleId)
        {
            if (!vehicleStates.ContainsKey(vehicleId)) return 0f;
            if (!activeVehicles.ContainsKey(vehicleId)) return 0f;

            var state = vehicleStates[vehicleId];
            var vehicle = activeVehicles[vehicleId];

            return state.health / vehicle.VehicleData.maxHealth;
        }

        #endregion
    }

    #region Data Classes

    [System.Serializable]
    public class VehicleSpawnPoint
    {
        public Vector3 position;
        public Quaternion rotation;
        public VehicleType vehicleType;
        public bool spawnOnStart = true;
        public bool respawnOnDestroy = true;
    }

    [System.Serializable]
    public class ExtractionVehiclePoint
    {
        public string extractionId;
        public Vector3 position;
        public Quaternion rotation;
        public VehicleType vehicleType = VehicleType.Helicopter;
        public bool requiresPayment;
        public int extractionCost;
    }

    [System.Serializable]
    public class VehicleData
    {
        public string vehicleId;
        public string vehicleName;
        public VehicleType vehicleType;
        public GameObject vehiclePrefab;

        [Header("Stats")]
        public float maxHealth = 1000f;
        public float maxSpeed = 100f; // km/h
        public float acceleration = 5f;
        public float handling = 5f;

        [Header("Fuel")]
        public float maxFuel = 100f;
        public float fuelEfficiency = 1f; // Lower is better

        [Header("Capacity")]
        public int maxPassengers = 3;
        public int storageCapacity = 20; // Inventory slots

        [Header("Features")]
        public bool hasLights = true;
        public bool hasHorn = true;
        public bool hasArmor = false;
        public float armorRating = 0f;

        public Sprite vehicleIcon;
    }

    public class VehicleState
    {
        public ulong vehicleId;
        public float health;
        public float fuel;
        public bool isEngineRunning;
        public ulong? driver;
        public List<ulong> passengers;
    }

    public class ExtractionRequest
    {
        public string extractionPointId;
        public float requestTime;
        public float arrivalTime;
        public float extractionTime;
        public List<ulong> playersWaiting;
        public bool vehicleArrived;
        public ulong? vehicleId;
    }

    public enum VehicleType
    {
        Car,            // Fast, low capacity
        Truck,          // Slow, high capacity
        SUV,            // Balanced
        APC,            // Armored, slow
        Motorcycle,     // Very fast, 1-2 seats
        Helicopter,     // Air extraction
        Boat            // Water extraction
    }

    public enum VehicleSeat
    {
        Driver,
        Passenger
    }

    public enum DamageSource
    {
        Bullet,
        Explosion,
        Collision,
        Environmental
    }

    #endregion
}

/// <summary>
/// Vehicle component - attach to vehicle prefabs.
/// Handles individual vehicle behavior, physics, and visual updates.
/// </summary>
public class Vehicle : NetworkBehaviour
{
    [Header("Vehicle Components")]
    [SerializeField] private Rigidbody vehicleRigidbody;
    [SerializeField] private WheelCollider[] wheelColliders;
    [SerializeField] private Transform[] wheelMeshes;

    [Header("Audio")]
    [SerializeField] private AudioSource engineAudio;
    [SerializeField] private AudioSource hornAudio;
    [SerializeField] private AudioClip engineStartClip;
    [SerializeField] private AudioClip engineStopClip;
    [SerializeField] private AudioClip hornClip;

    [Header("Visual Effects")]
    [SerializeField] private Light[] headLights;
    [SerializeField] private Light[] brakeLights;
    [SerializeField] private ParticleSystem exhaustParticles;
    [SerializeField] private ParticleSystem damageSmoke;

    [Header("Seat Positions")]
    [SerializeField] private Transform driverSeat;
    [SerializeField] private Transform[] passengerSeats;

    // Vehicle data
    private DeadFrontier.Vehicles.VehicleData vehicleData;
    private ulong vehicleId;
    private bool lightsOn;

    // Network state
    private NetworkVariable<bool> networkEngineRunning = new NetworkVariable<bool>(false);
    private NetworkVariable<float> networkSpeed = new NetworkVariable<float>(0f);

    public ulong VehicleId => vehicleId;
    public DeadFrontier.Vehicles.VehicleData VehicleData => vehicleData;

    public void Initialize(DeadFrontier.Vehicles.VehicleData data, ulong id)
    {
        vehicleData = data;
        vehicleId = id;

        if (vehicleRigidbody == null)
        {
            vehicleRigidbody = GetComponent<Rigidbody>();
        }

        Debug.Log($"[Vehicle] Initialized {data.vehicleName} with ID {id}");
    }

    private void Update()
    {
        if (IsServer)
        {
            UpdatePhysics();
        }

        UpdateVisuals();
    }

    #region Physics

    private void UpdatePhysics()
    {
        if (vehicleRigidbody == null) return;

        // Update speed
        float speed = vehicleRigidbody.velocity.magnitude * 3.6f; // m/s to km/h
        networkSpeed.Value = speed;

        // Update wheel meshes
        UpdateWheelMeshes();
    }

    private void UpdateWheelMeshes()
    {
        if (wheelColliders == null || wheelMeshes == null) return;
        if (wheelColliders.Length != wheelMeshes.Length) return;

        for (int i = 0; i < wheelColliders.Length; i++)
        {
            if (wheelColliders[i] == null || wheelMeshes[i] == null) continue;

            wheelColliders[i].GetWorldPose(out Vector3 position, out Quaternion rotation);
            wheelMeshes[i].position = position;
            wheelMeshes[i].rotation = rotation;
        }
    }

    #endregion

    #region Engine

    public void StartEngine()
    {
        networkEngineRunning.Value = true;

        if (engineAudio != null && engineStartClip != null)
        {
            engineAudio.PlayOneShot(engineStartClip);
            engineAudio.loop = true;
            engineAudio.Play();
        }

        if (exhaustParticles != null)
        {
            exhaustParticles.Play();
        }
    }

    public void StopEngine()
    {
        networkEngineRunning.Value = false;

        if (engineAudio != null && engineStopClip != null)
        {
            engineAudio.PlayOneShot(engineStopClip);
            engineAudio.loop = false;
        }

        if (exhaustParticles != null)
        {
            exhaustParticles.Stop();
        }
    }

    #endregion

    #region Controls

    public void PlayHorn()
    {
        if (hornAudio != null && hornClip != null)
        {
            hornAudio.PlayOneShot(hornClip);
        }
    }

    public void ToggleLights()
    {
        lightsOn = !lightsOn;

        if (headLights != null)
        {
            foreach (var light in headLights)
            {
                if (light != null)
                {
                    light.enabled = lightsOn;
                }
            }
        }
    }

    #endregion

    #region Occupants

    public void OnPlayerEntered(ulong playerId, DeadFrontier.Vehicles.VehicleSeat seat)
    {
        // Position player at seat
        // This would be handled by player controller

        Debug.Log($"[Vehicle] Player {playerId} entered as {seat}");
    }

    public void OnPlayerExited(ulong playerId)
    {
        // Remove player from seat
        // This would be handled by player controller

        Debug.Log($"[Vehicle] Player {playerId} exited");
    }

    #endregion

    #region Damage

    public void OnDamaged(float damage, Vector3 hitPoint)
    {
        // Visual damage effects
        if (damageSmoke != null && !damageSmoke.isPlaying)
        {
            float healthPercent = DeadFrontier.Vehicles.VehicleSystem.Instance.GetHealthPercentage(vehicleId);

            if (healthPercent < 0.5f)
            {
                damageSmoke.Play();
            }
        }

        // Spawn impact effects at hit point
        // This would spawn particle effects, decals, etc.
    }

    public void UpdateHealth(float newHealth)
    {
        // Update visual damage based on health
        float healthPercent = newHealth / vehicleData.maxHealth;

        if (damageSmoke != null)
        {
            if (healthPercent < 0.5f && !damageSmoke.isPlaying)
            {
                damageSmoke.Play();
            }
            else if (healthPercent >= 0.5f && damageSmoke.isPlaying)
            {
                damageSmoke.Stop();
            }
        }
    }

    #endregion

    #region Fuel

    public void UpdateFuel(float newFuel)
    {
        // Update fuel UI
        // This would be handled by vehicle HUD
    }

    #endregion

    #region Visuals

    private void UpdateVisuals()
    {
        // Update engine audio pitch based on speed
        if (engineAudio != null && networkEngineRunning.Value)
        {
            float speedPercent = networkSpeed.Value / vehicleData.maxSpeed;
            engineAudio.pitch = Mathf.Lerp(0.8f, 1.5f, speedPercent);
        }
    }

    #endregion
}
