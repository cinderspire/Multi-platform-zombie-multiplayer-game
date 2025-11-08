using UnityEngine;
using System.Collections;
using System;

namespace DeadFrontier.Weapons
{
    /// <summary>
    /// Controls weapon firing, reloading, and ammo management
    /// </summary>
    public class WeaponController : MonoBehaviour
    {
        [Header("Weapon Setup")]
        [SerializeField] private WeaponData weaponData;
        [SerializeField] private Transform firePoint;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private LayerMask hitLayers;

        [Header("Ammo")]
        [SerializeField] private int currentAmmo;
        [SerializeField] private int reserveAmmo = 120;
        [SerializeField] private bool infiniteAmmo = false;

        // State
        private float nextFireTime;
        private bool isReloading;
        private int burstShotsFired;

        // Events
        public event Action OnFire;
        public event Action OnReloadStart;
        public event Action OnReloadComplete;
        public event Action OnAmmoChanged;

        // Properties
        public WeaponData Data => weaponData;
        public int CurrentAmmo => currentAmmo;
        public int ReserveAmmo => reserveAmmo;
        public bool IsReloading => isReloading;
        public bool CanFire => !isReloading && currentAmmo > 0 && Time.time >= nextFireTime;
        public bool NeedsReload => currentAmmo < weaponData.magazineSize && (reserveAmmo > 0 || infiniteAmmo);

        private void Awake()
        {
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }

            if (firePoint == null)
            {
                firePoint = transform;
            }

            currentAmmo = weaponData.magazineSize;
        }

        /// <summary>
        /// Attempts to fire the weapon
        /// </summary>
        public void Fire()
        {
            if (!CanFire)
            {
                // Play empty sound if trying to fire with no ammo
                if (currentAmmo <= 0 && weaponData.emptySound != null)
                {
                    Core.AudioManager.Instance.Play(weaponData.emptySound);
                }
                return;
            }

            switch (weaponData.firingMode)
            {
                case FiringMode.SemiAuto:
                    FireSingleShot();
                    break;

                case FiringMode.Automatic:
                    FireSingleShot();
                    break;

                case FiringMode.Burst:
                    if (burstShotsFired == 0)
                    {
                        StartCoroutine(FireBurst());
                    }
                    break;
            }
        }

        /// <summary>
        /// Fires a single shot
        /// </summary>
        private void FireSingleShot()
        {
            // Consume ammo
            currentAmmo--;
            OnAmmoChanged?.Invoke();

            // Set next fire time
            nextFireTime = Time.time + weaponData.TimeBetweenShots;

            // Perform raycast
            PerformRaycast();

            // Visual and audio feedback
            PlayFireEffects();

            // Notify event
            OnFire?.Invoke();

            Debug.Log($"[WeaponController] Fired {weaponData.weaponName}. Ammo: {currentAmmo}/{weaponData.magazineSize}");
        }

        /// <summary>
        /// Fires a burst of shots
        /// </summary>
        private IEnumerator FireBurst()
        {
            int burstCount = 3;
            burstShotsFired = burstCount;

            for (int i = 0; i < burstCount; i++)
            {
                if (currentAmmo <= 0)
                    break;

                FireSingleShot();
                yield return new WaitForSeconds(0.1f); // 100ms between burst shots
            }

            burstShotsFired = 0;
        }

        /// <summary>
        /// Performs raycast for hit detection
        /// </summary>
        private void PerformRaycast()
        {
            Vector3 rayOrigin = playerCamera.transform.position;
            Vector3 rayDirection = playerCamera.transform.forward;

            // Apply spread
            float spread = weaponData.GetSpread(IsPlayerMoving(), IsPlayerJumping());
            rayDirection = ApplySpread(rayDirection, spread);

            // Perform raycast
            if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, weaponData.range, hitLayers))
            {
                ProcessHit(hit);

                // Handle penetration
                if (weaponData.canPenetrate)
                {
                    HandlePenetration(rayOrigin, rayDirection, hit);
                }
            }
        }

        /// <summary>
        /// Processes a raycast hit
        /// </summary>
        private void ProcessHit(RaycastHit hit)
        {
            // Check if hit object can take damage
            var damageable = hit.collider.GetComponent<Core.IDamageable>();
            if (damageable != null)
            {
                // Calculate damage (check for headshot)
                float damage = weaponData.damage;
                if (IsHeadshot(hit))
                {
                    damage *= Core.Constants.HEADSHOT_MULTIPLIER;
                    Debug.Log($"[WeaponController] HEADSHOT! Damage: {damage}");
                }

                damageable.TakeDamage(damage);
            }

            // Show impact effect
            ShowImpactEffect(hit.point, hit.normal);
        }

        /// <summary>
        /// Handles bullet penetration through multiple targets
        /// </summary>
        private void HandlePenetration(Vector3 origin, Vector3 direction, RaycastHit firstHit)
        {
            int penetrationCount = 0;
            float remainingDamage = weaponData.damage;
            Vector3 currentOrigin = firstHit.point + direction * 0.1f; // Start slightly past first hit

            while (penetrationCount < weaponData.maxPenetrationTargets)
            {
                if (Physics.Raycast(currentOrigin, direction, out RaycastHit hit, weaponData.range, hitLayers))
                {
                    penetrationCount++;
                    remainingDamage *= (1f - weaponData.penetrationDamageReduction);

                    var damageable = hit.collider.GetComponent<Core.IDamageable>();
                    damageable?.TakeDamage(remainingDamage);

                    currentOrigin = hit.point + direction * 0.1f;
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Applies spread to the ray direction
        /// </summary>
        private Vector3 ApplySpread(Vector3 direction, float spreadAngle)
        {
            if (spreadAngle <= 0f)
                return direction;

            // Random spread within cone
            float spreadX = UnityEngine.Random.Range(-spreadAngle, spreadAngle);
            float spreadY = UnityEngine.Random.Range(-spreadAngle, spreadAngle);

            Quaternion spread = Quaternion.Euler(spreadX, spreadY, 0f);
            return spread * direction;
        }

        /// <summary>
        /// Checks if hit is a headshot
        /// </summary>
        private bool IsHeadshot(RaycastHit hit)
        {
            // Simple check: if hit point is in upper 20% of collider
            // More sophisticated version would check for "Head" tag or specific collider
            if (hit.collider.bounds.max.y - hit.point.y < hit.collider.bounds.size.y * 0.2f)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Plays fire effects (audio, VFX, recoil)
        /// </summary>
        private void PlayFireEffects()
        {
            // Audio
            if (weaponData.fireSound != null)
            {
                Core.AudioManager.Instance.Play(
                    weaponData.fireSound,
                    firePoint.position,
                    1f,
                    1f,
                    weaponData.noiseLevel
                );
            }

            // Muzzle flash
            if (weaponData.muzzleFlashPrefab != null)
            {
                GameObject flash = Core.PoolManager.Instance.Get(
                    weaponData.muzzleFlashPrefab,
                    firePoint.position,
                    firePoint.rotation
                );
                Core.PoolManager.Instance.Return(flash, 0.1f);
            }

            // Recoil
            ApplyRecoil();

            // TODO: Bullet tracer
        }

        /// <summary>
        /// Shows impact effect at hit point
        /// </summary>
        private void ShowImpactEffect(Vector3 position, Vector3 normal)
        {
            if (weaponData.impactEffectPrefab != null)
            {
                GameObject impact = Core.PoolManager.Instance.Get(
                    weaponData.impactEffectPrefab,
                    position,
                    Quaternion.LookRotation(normal)
                );
                Core.PoolManager.Instance.Return(impact, 1f);
            }
        }

        /// <summary>
        /// Applies recoil to camera
        /// </summary>
        private void ApplyRecoil()
        {
            Vector2 recoil = weaponData.GetRecoilAmount();
            var playerCamera = GetComponentInParent<Player.PlayerCamera>();
            playerCamera?.AddRecoil(recoil);
        }

        /// <summary>
        /// Reloads the weapon
        /// </summary>
        public void Reload()
        {
            if (isReloading || currentAmmo >= weaponData.magazineSize || (reserveAmmo <= 0 && !infiniteAmmo))
            {
                return;
            }

            StartCoroutine(ReloadCoroutine());
        }

        private IEnumerator ReloadCoroutine()
        {
            isReloading = true;
            OnReloadStart?.Invoke();

            Debug.Log($"[WeaponController] Reloading {weaponData.weaponName}...");

            // Play reload sound
            if (weaponData.reloadSound != null)
            {
                Core.AudioManager.Instance.Play(weaponData.reloadSound);
            }

            // Wait for reload time
            yield return new WaitForSeconds(weaponData.reloadTime);

            // Calculate ammo to reload
            int ammoNeeded = weaponData.magazineSize - currentAmmo;

            if (infiniteAmmo)
            {
                currentAmmo = weaponData.magazineSize;
            }
            else
            {
                int ammoToReload = Mathf.Min(ammoNeeded, reserveAmmo);
                currentAmmo += ammoToReload;
                reserveAmmo -= ammoToReload;
            }

            isReloading = false;
            OnReloadComplete?.Invoke();
            OnAmmoChanged?.Invoke();

            Debug.Log($"[WeaponController] Reload complete. Ammo: {currentAmmo}/{weaponData.magazineSize}");
        }

        /// <summary>
        /// Adds ammo to reserve
        /// </summary>
        public void AddAmmo(int amount)
        {
            reserveAmmo += amount;
            OnAmmoChanged?.Invoke();
        }

        /// <summary>
        /// Sets weapon data
        /// </summary>
        public void SetWeaponData(WeaponData data)
        {
            weaponData = data;
            currentAmmo = weaponData.magazineSize;
            OnAmmoChanged?.Invoke();
        }

        // Helper methods to check player state (would integrate with PlayerMovement)
        private bool IsPlayerMoving()
        {
            var movement = GetComponentInParent<Player.PlayerMovement>();
            return movement != null && movement.CurrentSpeed > 0.1f;
        }

        private bool IsPlayerJumping()
        {
            var movement = GetComponentInParent<Player.PlayerMovement>();
            return movement != null && !movement.IsGrounded;
        }

        private void OnValidate()
        {
            if (weaponData != null && currentAmmo > weaponData.magazineSize)
            {
                currentAmmo = weaponData.magazineSize;
            }
        }
    }
}
