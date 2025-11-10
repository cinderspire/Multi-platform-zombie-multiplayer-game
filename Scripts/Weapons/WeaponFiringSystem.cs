using System;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Weapons
{
    public class WeaponFiringSystem : NetworkBehaviour
    {
        public static WeaponFiringSystem Instance { get; private set; }

        [Header("Firing Settings")]
        [SerializeField] private float maxRecoil = 2f;
        [SerializeField] private float recoilRecoverySpeed = 5f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        [ServerRpc(RequireOwnership = false)]
        public void FireWeaponServerRpc(ulong playerId, WeaponData weapon, Vector3 origin, Vector3 direction, ServerRpcParams rpcParams = default)
        {
            if (weapon == null) return;

            // Check if weapon can fire
            if (!CanFire(playerId, weapon)) return;

            // Process firing based on weapon type
            switch (weapon.weaponType)
            {
                case WeaponType.Pistol:
                case WeaponType.Rifle:
                case WeaponType.Shotgun:
                case WeaponType.SMG:
                case WeaponType.Sniper:
                    ProcessHitscanFire(playerId, weapon, origin, direction);
                    break;

                case WeaponType.Launcher:
                    ProcessProjectileFire(playerId, weapon, origin, direction);
                    break;
            }

            // Consume ammo
            ConsumeAmmo(playerId, weapon);

            // Apply recoil
            ApplyRecoil(playerId, weapon);

            // Notify clients
            FireWeaponClientRpc(playerId, weapon.weaponId, origin, direction);
        }

        private void ProcessHitscanFire(ulong playerId, WeaponData weapon, Vector3 origin, Vector3 direction)
        {
            // Shotgun fires multiple pellets
            int pelletCount = weapon.weaponType == WeaponType.Shotgun ? 8 : 1;
            
            for (int i = 0; i < pelletCount; i++)
            {
                Vector3 spread = ApplySpread(direction, weapon.spread);
                
                if (Physics.Raycast(origin, spread, out RaycastHit hit, weapon.range))
                {
                    // Check if hit player or zombie
                    if (hit.collider.TryGetComponent<NetworkObject>(out var netObj))
                    {
                        bool isHeadshot = hit.collider.CompareTag("Head");
                        float damage = weapon.damage / pelletCount; // Spread damage for shotgun

                        Combat.CombatSystem.Instance?.PerformRangedAttackServerRpc(
                            playerId,
                            origin,
                            spread,
                            damage,
                            weapon.weaponId
                        );
                    }
                }
            }
        }

        private void ProcessProjectileFire(ulong playerId, WeaponData weapon, Vector3 origin, Vector3 direction)
        {
            // Would spawn projectile NetworkObject here
            Debug.Log($"Firing projectile: {weapon.weaponId}");
        }

        private Vector3 ApplySpread(Vector3 direction, float spread)
        {
            Vector3 spreadOffset = new Vector3(
                UnityEngine.Random.Range(-spread, spread),
                UnityEngine.Random.Range(-spread, spread),
                0f
            );

            return (direction + spreadOffset).normalized;
        }

        private bool CanFire(ulong playerId, WeaponData weapon)
        {
            // Check ammo, fire rate, etc.
            return true; // Simplified
        }

        private void ConsumeAmmo(ulong playerId, WeaponData weapon)
        {
            // Would integrate with inventory system
        }

        private void ApplyRecoil(ulong playerId, WeaponData weapon)
        {
            // Recoil applied on client side
        }

        [ClientRpc]
        private void FireWeaponClientRpc(ulong playerId, string weaponId, Vector3 origin, Vector3 direction)
        {
            // Play muzzle flash
            // Play firing sound
            // Apply camera shake
            // Show bullet tracers
        }
    }

    [Serializable]
    public class WeaponData
    {
        public string weaponId;
        public WeaponType weaponType;
        public float damage;
        public float fireRate;
        public float range;
        public float spread;
        public int magazineSize;
        public int reserveAmmo;
        public float reloadTime;
    }

    public enum WeaponType { Pistol, Rifle, Shotgun, SMG, Sniper, Launcher }
}
