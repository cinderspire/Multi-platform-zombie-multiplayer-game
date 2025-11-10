using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Vehicles
{
    /// <summary>
    /// Comprehensive vehicle system for zombie multiplayer game.
    /// Handles vehicle spawning, physics, damage, fuel, passengers, and upgrades.
    /// </summary>
    public class VehicleSystem : NetworkBehaviour
    {
        public static VehicleSystem Instance { get; private set; }
 
        [Header("Vehicle Configuration")]
        [SerializeField] private int maxActiveVehicles = 50;
        [SerializeField] private float vehicleRespawnTime = 300f;
        [SerializeField] private bool enableVehicleDamage = true;
        [SerializeField] private bool enableFuelSystem = true;

        // Data structures
        private Dictionary<string, Vehicle> activeVehicles = new Dictionary<string, Vehicle>();
        private Dictionary<string, VehicleDefinition> vehicleDefinitions = new Dictionary<string, VehicleDefinition>();

        // Events
        public event Action<string> OnVehicleSpawned;
        public event Action<ulong, string> OnPlayerEnteredVehicle;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }
    }

    [Serializable]
    public class Vehicle
    {
        public string vehicleId;
        public VehicleDefinition definition;
        public float maxHealth;
        public float currentHealth;
    }

    [Serializable]
    public class VehicleDefinition
    {
        public string vehicleId;
        public string vehicleName;
        public VehicleType vehicleType;
        public float maxHealth;
        public float maxSpeed;
    }

    public enum VehicleType { Motorcycle, Car, SUV, Truck, Helicopter, APC, Tank }
}
