using UnityEngine;
using Unity.Netcode;

namespace DeadFrontier.Networking
{
    /// <summary>
    /// Networked weapon with server-authoritative hit detection
    /// </summary>
    public class NetworkedWeapon : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private Weapons.WeaponController weaponController;
        [SerializeField] private Transform firePoint;
        [SerializeField] private Camera playerCamera;

        [Header("Network Settings")]
        [SerializeField] private bool clientSideHitDetection = false; // For lag compensation

        // Network variables
        private NetworkVariable<int> networkAmmo = new NetworkVariable<int>();
        private NetworkVariable<int> networkReserveAmmo = new NetworkVariable<int>();
        private NetworkVariable<bool> networkIsReloading = new NetworkVariable<bool>();

        // Events
        public event System.Action<int, int> OnAmmoChanged;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsOwner)
            {
                // Enable weapon controller for local player
                weaponController.enabled = true;

                // Subscribe to weapon events
                weaponController.OnFire += HandleLocalFire;
                weaponController.OnReloadStarted += HandleLocalReloadStarted;
                weaponController.OnReloadCompleted += HandleLocalReloadCompleted;
            }
            else
            {
                // Disable for remote players
                weaponController.enabled = false;
            }

            // Subscribe to network variable changes
            networkAmmo.OnValueChanged += OnAmmoNetworkChanged;
            networkReserveAmmo.OnValueChanged += OnReserveAmmoNetworkChanged;
            networkIsReloading.OnValueChanged += OnReloadingNetworkChanged;

            Debug.Log($"[NetworkedWeapon] Spawned for client {OwnerClientId}");
        }

        #region Local Player Events

        private void HandleLocalFire()
        {
            if (!IsOwner)
                return;

            // Get fire direction
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            Vector3 fireDirection = ray.direction;
            Vector3 fireOrigin = ray.origin;

            if (clientSideHitDetection)
            {
                // Client-side hit detection (with lag compensation)
                PerformHitscan(fireOrigin, fireDirection);

                // Notify server
                FireWeaponServerRpc(fireOrigin, fireDirection);
            }
            else
            {
                // Server-authoritative hit detection (more secure, but higher latency)
                FireWeaponServerRpc(fireOrigin, fireDirection);
            }

            // Play local fire effects immediately (client-side prediction)
            PlayFireEffectsClientRpc(fireOrigin, fireDirection);
        }

        private void HandleLocalReloadStarted()
        {
            if (!IsOwner)
                return;

            ReloadWeaponServerRpc();
        }

        private void HandleLocalReloadCompleted()
        {
            // Handled by server
        }

        #endregion

        #region Server RPCs

        /// <summary>
        /// Client requests to fire weapon
        /// </summary>
        [ServerRpc]
        private void FireWeaponServerRpc(Vector3 fireOrigin, Vector3 fireDirection)
        {
            if (!IsServer)
                return;

            // Validate ammo
            if (weaponController.CurrentAmmo <= 0 || weaponController.IsReloading)
            {
                Debug.LogWarning($"[NetworkedWeapon] Client {OwnerClientId} tried to fire without ammo");
                return;
            }

            // Validate fire rate (anti-cheat)
            // ... fire rate validation logic

            // Consume ammo
            weaponController.ConsumeAmmo();
            networkAmmo.Value = weaponController.CurrentAmmo;

            // Perform server-side hitscan
            PerformHitscan(fireOrigin, fireDirection);

            // Notify all clients to play effects
            PlayFireEffectsClientRpc(fireOrigin, fireDirection);

            Debug.Log($"[NetworkedWeapon] Player {OwnerClientId} fired weapon");
        }

        /// <summary>
        /// Client requests to reload weapon
        /// </summary>
        [ServerRpc]
        private void ReloadWeaponServerRpc()
        {
            if (!IsServer)
                return;

            // Validate reload
            if (weaponController.IsReloading || weaponController.CurrentAmmo == weaponController.MagazineSize)
            {
                return;
            }

            if (weaponController.ReserveAmmo <= 0)
            {
                Debug.LogWarning($"[NetworkedWeapon] Client {OwnerClientId} tried to reload without reserve ammo");
                return;
            }

            // Start reload
            networkIsReloading.Value = true;

            // Notify all clients
            PlayReloadEffectsClientRpc();

            // Complete reload after delay
            Invoke(nameof(CompleteReload), weaponController.WeaponData.reloadTime);
        }

        /// <summary>
        /// Server completes reload
        /// </summary>
        private void CompleteReload()
        {
            if (!IsServer)
                return;

            int ammoNeeded = weaponController.MagazineSize - weaponController.CurrentAmmo;
            int ammoToReload = Mathf.Min(ammoNeeded, weaponController.ReserveAmmo);

            weaponController.SetAmmo(weaponController.CurrentAmmo + ammoToReload);
            weaponController.SetReserveAmmo(weaponController.ReserveAmmo - ammoToReload);

            networkAmmo.Value = weaponController.CurrentAmmo;
            networkReserveAmmo.Value = weaponController.ReserveAmmo;
            networkIsReloading.Value = false;

            Debug.Log($"[NetworkedWeapon] Player {OwnerClientId} reload complete");
        }

        #endregion

        #region Client RPCs

        /// <summary>
        /// Plays fire effects on all clients
        /// </summary>
        [ClientRpc]
        private void PlayFireEffectsClientRpc(Vector3 fireOrigin, Vector3 fireDirection)
        {
            // Skip if this is the owner (already played locally)
            if (IsOwner && clientSideHitDetection)
                return;

            PlayLocalFireEffects(fireOrigin, fireDirection);
        }

        /// <summary>
        /// Plays reload effects on all clients
        /// </summary>
        [ClientRpc]
        private void PlayReloadEffectsClientRpc()
        {
            if (!IsOwner)
            {
                // Play reload animation/sounds for remote players
                weaponController.PlayReloadAnimation();
            }
        }

        /// <summary>
        /// Shows hit effect at a position
        /// </summary>
        [ClientRpc]
        private void ShowHitEffectClientRpc(Vector3 hitPosition, Vector3 hitNormal)
        {
            // Show bullet impact effect
            ShowImpactEffect(hitPosition, hitNormal);
        }

        #endregion

        #region Hit Detection

        /// <summary>
        /// Performs hitscan raycast
        /// </summary>
        private void PerformHitscan(Vector3 fireOrigin, Vector3 fireDirection)
        {
            // Apply weapon spread
            Vector3 spreadDirection = ApplySpread(fireDirection, weaponController.CurrentSpread);

            RaycastHit hit;
            bool didHit = Physics.Raycast(
                fireOrigin,
                spreadDirection,
                out hit,
                weaponController.WeaponData.range,
                LayerMask.GetMask("Player", "Zombie", "Default")
            );

            if (didHit)
            {
                ProcessHit(hit);

                // Show hit effect on all clients
                if (IsServer)
                {
                    ShowHitEffectClientRpc(hit.point, hit.normal);
                }
            }
        }

        /// <summary>
        /// Processes a successful hit
        /// </summary>
        private void ProcessHit(RaycastHit hit)
        {
            float damage = weaponController.WeaponData.damage;

            // Check for headshot
            if (IsHeadshot(hit))
            {
                damage *= Core.Constants.HEADSHOT_MULTIPLIER;
                Debug.Log($"[NetworkedWeapon] HEADSHOT! Damage: {damage}");
            }

            // Apply damage based on what was hit
            if (hit.collider.CompareTag("Player"))
            {
                // Hit another player
                NetworkedPlayer targetPlayer = hit.collider.GetComponentInParent<NetworkedPlayer>();
                if (targetPlayer != null)
                {
                    if (IsServer)
                    {
                        targetPlayer.TakeDamageServerRpc(damage, OwnerClientId);
                    }
                    else
                    {
                        // Client hit detection - notify server
                        RequestDamageServerRpc(targetPlayer.OwnerClientId, damage);
                    }
                }
            }
            else if (hit.collider.CompareTag("Zombie"))
            {
                // Hit a zombie
                var zombieHealth = hit.collider.GetComponent<Zombies.ZombieHealth>();
                if (zombieHealth != null && IsServer)
                {
                    zombieHealth.TakeDamage(damage);
                }
            }

            Debug.Log($"[NetworkedWeapon] Hit {hit.collider.name} for {damage} damage");
        }

        /// <summary>
        /// Client requests server to apply damage (for client-side hit detection)
        /// </summary>
        [ServerRpc]
        private void RequestDamageServerRpc(ulong targetClientId, float damage)
        {
            if (!IsServer)
                return;

            // Server validates hit with lag compensation
            // For now, trust the client (in production, add validation)

            NetworkManager.Singleton.ConnectedClients.TryGetValue(targetClientId, out var client);
            if (client != null)
            {
                var targetPlayer = client.PlayerObject.GetComponent<NetworkedPlayer>();
                if (targetPlayer != null)
                {
                    targetPlayer.TakeDamageServerRpc(damage, OwnerClientId);
                }
            }
        }

        /// <summary>
        /// Checks if hit was a headshot
        /// </summary>
        private bool IsHeadshot(RaycastHit hit)
        {
            // Simple geometric check: upper 20% of the collider
            Collider col = hit.collider;
            float relativeHeight = (hit.point.y - col.bounds.min.y) / col.bounds.size.y;

            return relativeHeight > 0.8f;
        }

        /// <summary>
        /// Applies weapon spread to fire direction
        /// </summary>
        private Vector3 ApplySpread(Vector3 direction, float spread)
        {
            float spreadAmount = spread * 0.01f; // Convert to radians-ish
            Vector3 randomSpread = new Vector3(
                Random.Range(-spreadAmount, spreadAmount),
                Random.Range(-spreadAmount, spreadAmount),
                0f
            );

            return (direction + randomSpread).normalized;
        }

        #endregion

        #region Visual Effects

        private void PlayLocalFireEffects(Vector3 fireOrigin, Vector3 fireDirection)
        {
            // Muzzle flash
            if (weaponController.WeaponData.muzzleFlashPrefab != null && firePoint != null)
            {
                GameObject muzzleFlash = Core.PoolManager.Instance.Get(
                    weaponController.WeaponData.muzzleFlashPrefab,
                    firePoint.position,
                    firePoint.rotation
                );

                Core.PoolManager.Instance.Return(muzzleFlash, 0.1f);
            }

            // Fire sound
            if (weaponController.WeaponData.fireSound != null)
            {
                Core.AudioManager.Instance.Play(
                    weaponController.WeaponData.fireSound,
                    firePoint != null ? firePoint.position : transform.position,
                    1f,
                    Random.Range(0.95f, 1.05f),
                    weaponController.WeaponData.fireSoundNoiseLevel
                );
            }

            // Tracer (optional)
            // ... tracer line renderer logic
        }

        private void ShowImpactEffect(Vector3 position, Vector3 normal)
        {
            // Bullet impact particles
            // GameObject impact = PoolManager.Instance.Get(impactEffectPrefab, position, Quaternion.LookRotation(normal));
            // PoolManager.Instance.Return(impact, 1f);
        }

        #endregion

        #region Network Variable Callbacks

        private void OnAmmoNetworkChanged(int oldValue, int newValue)
        {
            if (!IsOwner)
            {
                weaponController.SetAmmo(newValue);
            }

            OnAmmoChanged?.Invoke(newValue, networkReserveAmmo.Value);
        }

        private void OnReserveAmmoNetworkChanged(int oldValue, int newValue)
        {
            if (!IsOwner)
            {
                weaponController.SetReserveAmmo(newValue);
            }

            OnAmmoChanged?.Invoke(networkAmmo.Value, newValue);
        }

        private void OnReloadingNetworkChanged(bool oldValue, bool newValue)
        {
            if (!IsOwner)
            {
                weaponController.SetReloading(newValue);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds ammo to the weapon (server-only)
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void AddAmmoServerRpc(int amount)
        {
            if (!IsServer)
                return;

            weaponController.SetReserveAmmo(weaponController.ReserveAmmo + amount);
            networkReserveAmmo.Value = weaponController.ReserveAmmo;

            Debug.Log($"[NetworkedWeapon] Added {amount} ammo to player {OwnerClientId}");
        }

        #endregion

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            if (weaponController != null)
            {
                weaponController.OnFire -= HandleLocalFire;
                weaponController.OnReloadStarted -= HandleLocalReloadStarted;
                weaponController.OnReloadCompleted -= HandleLocalReloadCompleted;
            }

            networkAmmo.OnValueChanged -= OnAmmoNetworkChanged;
            networkReserveAmmo.OnValueChanged -= OnReserveAmmoNetworkChanged;
            networkIsReloading.OnValueChanged -= OnReloadingNetworkChanged;
        }
    }
}
