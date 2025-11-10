using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Vehicles
{
    public class VehicleSystem : NetworkBehaviour
    {
        public static VehicleSystem Instance { get; private set; }

        [SerializeField] private float maxFuel = 100f;

        private Dictionary<string, Vehicle> vehicles = new Dictionary<string, Vehicle>();

        public event Action<ulong, string> OnPlayerEnteredVehicle;
        public event Action<ulong, string> OnPlayerExitedVehicle;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        [ServerRpc(RequireOwnership = false)]
        public void EnterVehicleServerRpc(ulong playerId, string vehicleId, ServerRpcParams rpcParams = default)
        {
            if (!vehicles.TryGetValue(vehicleId, out var vehicle)) return;
            if (vehicle.driverId != 0) return;

            vehicle.driverId = playerId;
            OnPlayerEnteredVehicle?.Invoke(playerId, vehicleId);
            EnterVehicleClientRpc(playerId, vehicleId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void ExitVehicleServerRpc(ulong playerId, string vehicleId, ServerRpcParams rpcParams = default)
        {
            if (!vehicles.TryGetValue(vehicleId, out var vehicle)) return;
            if (vehicle.driverId != playerId) return;

            vehicle.driverId = 0;
            OnPlayerExitedVehicle?.Invoke(playerId, vehicleId);
            ExitVehicleClientRpc(playerId, vehicleId);
        }

        [ClientRpc]
        private void EnterVehicleClientRpc(ulong playerId, string vehicleId) { }

        [ClientRpc]
        private void ExitVehicleClientRpc(ulong playerId, string vehicleId) { }
    }

    [Serializable]
    public class Vehicle
    {
        public string vehicleId;
        public VehicleType type;
        public float speed;
        public float fuel;
        public float health;
        public ulong driverId;
        public List<ulong> passengers = new List<ulong>();
    }

    public enum VehicleType { Car, Truck, Motorcycle, APC }
}
