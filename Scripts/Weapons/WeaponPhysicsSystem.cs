using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace DeadFrontier.Weapons
{
    /// <summary>
    /// Advanced weapon physics system with realistic ballistics, recoil patterns, and penetration.
    /// Provides AAA-quality shooting mechanics for competitive gameplay.
    /// </summary>
    public class WeaponPhysicsSystem : NetworkBehaviour
    {
        [Header("Ballistics")]
        [SerializeField] private bool useBallisticPhysics = true;
        [SerializeField] private float bulletGravity = 9.81f;
        [SerializeField] private float airResistance = 0.01f;
        [SerializeField] private int ballisticSimulationSteps = 100;
        [SerializeField] private float ballisticTimeStep = 0.01f;

        [Header("Recoil")]
        [SerializeField] private bool useRecoilPatterns = true;
        [SerializeField] private AnimationCurve recoilRecoverySpeed;
        [SerializeField] private float recoilRecoveryDelay = 0.2f;
        [SerializeField] private float aimDownSightRecoilMultiplier = 0.5f;
        [SerializeField] private float crouchRecoilMultiplier = 0.7f;

        [Header("Penetration")]
        [SerializeField] private bool enablePenetration = true;
        [SerializeField] private int maxPenetrations = 3;
        [SerializeField] private float penetrationDamageReduction = 0.3f;
        [SerializeField] private LayerMask penetrableLayers;

        [Header("Bullet Impact")]
        [SerializeField] private GameObject defaultImpactVFX;
        [SerializeField] private GameObject fleshImpactVFX;
        [SerializeField] private GameObject metalImpactVFX;
        [SerializeField] private GameObject woodImpactVFX;
        [SerializeField] private GameObject concreteImpactVFX;
        [SerializeField] private float impactForce = 10f;

        [Header("Tracer Effects")]
        [SerializeField] private GameObject tracerPrefab;
        [SerializeField] private float tracerSpeed = 500f;
        [SerializeField] private float tracerFrequency = 5f; // Every 5th bullet

        [Header("Muzzle Flash")]
        [SerializeField] private GameObject muzzleFlashPrefab;
        [SerializeField] private Light muzzleFlashLight;
        [SerializeField] private float muzzleFlashDuration = 0.05f;

        // Recoil state
        private Vector3 currentRecoil;
        private Vector3 targetRecoil;
        private float recoilRecoveryTimer;
        private int shotsFired;
        private RecoilPattern currentRecoilPattern;

        // Weapon reference
        private WeaponData weaponData;
        private Transform firePoint;
        private Transform weaponTransform;
        private Camera playerCamera;

        // Bullet count for tracers
        private int bulletCount;

        private void Awake()
        {
            playerCamera = Camera.main;
        }

        private void Update()
        {
            UpdateRecoilRecovery();
        }

        public void Initialize(WeaponData weapon, Transform weaponRoot, Transform fireTransform)
        {
            weaponData = weapon;
            weaponTransform = weaponRoot;
            firePoint = fireTransform;
            bulletCount = 0;
        }

        #region Shooting

        public void FireWeapon(Vector3 direction, float damage, float range, bool isADS, bool isCrouching)
        {
            if (!IsOwner) return;

            bulletCount++;

            // Apply recoil
            ApplyRecoil(isADS, isCrouching);

            // Calculate bullet spawn
            Vector3 origin = firePoint.position;
            Vector3 shootDirection = ApplySpread(direction);

            // Play muzzle flash
            PlayMuzzleFlash();

            // Spawn tracer
            if (bulletCount % Mathf.RoundToInt(tracerFrequency) == 0)
            {
                SpawnTracer(origin, shootDirection);
            }

            // Perform raycast with physics
            if (useBallisticPhysics)
            {
                FireBallisticShot(origin, shootDirection, damage, range);
            }
            else
            {
                FireHitscanShot(origin, shootDirection, damage, range);
            }

            shotsFired++;
        }

        private void FireHitscanShot(Vector3 origin, Vector3 direction, float damage, float range)
        {
            RaycastHit hit;
            List<RaycastHit> hits = new List<RaycastHit>();

            Vector3 currentOrigin = origin;
            Vector3 currentDirection = direction;
            float remainingDamage = damage;
            int penetrations = 0;

            while (penetrations <= maxPenetrations)
            {
                if (Physics.Raycast(currentOrigin, currentDirection, out hit, range))
                {
                    hits.Add(hit);

                    // Apply damage
                    ProcessHit(hit, remainingDamage);

                    // Check for penetration
                    if (enablePenetration && CanPenetrate(hit.collider.gameObject))
                    {
                        remainingDamage *= (1f - penetrationDamageReduction);

                        // Continue ray from penetration point
                        currentOrigin = hit.point + currentDirection * 0.1f;
                        penetrations++;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            // Request server to validate hits
            if (IsClient)
            {
                RequestHitValidationServerRpc(hits.ToArray());
            }
        }

        private void FireBallisticShot(Vector3 origin, Vector3 direction, float damage, float range)
        {
            // Simulate bullet trajectory with physics
            Vector3 position = origin;
            Vector3 velocity = direction * weaponData.bulletVelocity;

            List<RaycastHit> hits = new List<RaycastHit>();
            float traveledDistance = 0f;

            for (int step = 0; step < ballisticSimulationSteps; step++)
            {
                // Calculate forces
                Vector3 gravity = Vector3.down * bulletGravity * ballisticTimeStep;
                Vector3 drag = -velocity * airResistance * ballisticTimeStep;

                velocity += gravity + drag;

                // Calculate movement this step
                Vector3 nextPosition = position + velocity * ballisticTimeStep;
                Vector3 stepDirection = (nextPosition - position).normalized;
                float stepDistance = Vector3.Distance(position, nextPosition);

                // Check for collision
                RaycastHit hit;
                if (Physics.Raycast(position, stepDirection, out hit, stepDistance))
                {
                    hits.Add(hit);

                    // Calculate damage falloff
                    float damageFalloff = CalculateDamageFalloff(traveledDistance, range);
                    float finalDamage = damage * damageFalloff;

                    ProcessHit(hit, finalDamage);

                    // Check penetration
                    if (enablePenetration && CanPenetrate(hit.collider.gameObject))
                    {
                        damage *= (1f - penetrationDamageReduction);
                        position = hit.point + stepDirection * 0.1f;
                    }
                    else
                    {
                        break;
                    }
                }

                position = nextPosition;
                traveledDistance += stepDistance;

                if (traveledDistance >= range)
                    break;
            }

            // Request server validation
            if (IsClient)
            {
                RequestHitValidationServerRpc(hits.ToArray());
            }
        }

        private Vector3 ApplySpread(Vector3 baseDirection)
        {
            if (weaponData == null) return baseDirection;

            float spread = weaponData.spread;

            // Reduce spread if aiming down sights
            // This would be checked via weapon state

            float spreadX = Random.Range(-spread, spread);
            float spreadY = Random.Range(-spread, spread);

            Vector3 spreadDirection = baseDirection;
            spreadDirection += playerCamera.transform.right * spreadX;
            spreadDirection += playerCamera.transform.up * spreadY;

            return spreadDirection.normalized;
        }

        private float CalculateDamageFalloff(float distance, float maxRange)
        {
            if (distance < maxRange * 0.5f)
                return 1f; // Full damage at close range

            float falloffStart = maxRange * 0.5f;
            float falloffEnd = maxRange;
            float falloffPercent = (distance - falloffStart) / (falloffEnd - falloffStart);

            return Mathf.Lerp(1f, 0.3f, falloffPercent); // 30% damage at max range
        }

        #endregion

        #region Hit Processing

        private void ProcessHit(RaycastHit hit, float damage)
        {
            // Damage targets
            var health = hit.collider.GetComponent<Player.PlayerHealth>();
            if (health != null)
            {
                // Check for headshot
                bool isHeadshot = hit.collider.CompareTag("Head");
                float finalDamage = isHeadshot ? damage * 2f : damage;

                health.TakeDamage(finalDamage, hit.point);

                // Show hit marker
                if (UI.GameHUD.Instance != null)
                {
                    UI.GameHUD.Instance.ShowHitMarker(isHeadshot);
                }
            }

            var zombieAI = hit.collider.GetComponent<AI.ZombieAI>();
            if (zombieAI != null)
            {
                // Check for weak point
                bool isWeakPoint = hit.collider.CompareTag("WeakPoint");
                float finalDamage = isWeakPoint ? damage * 1.5f : damage;

                zombieAI.TakeDamage(finalDamage, hit.point);

                if (UI.GameHUD.Instance != null)
                {
                    UI.GameHUD.Instance.ShowHitMarker(isWeakPoint);
                }
            }

            // Spawn impact effects
            SpawnImpactEffect(hit);

            // Apply physics force
            Rigidbody hitRigidbody = hit.collider.GetComponent<Rigidbody>();
            if (hitRigidbody != null)
            {
                hitRigidbody.AddForceAtPosition(hit.normal * -impactForce, hit.point, ForceMode.Impulse);
            }
        }

        private bool CanPenetrate(GameObject obj)
        {
            // Check if object can be penetrated
            if (((1 << obj.layer) & penetrableLayers) == 0)
                return false;

            // Thin objects can be penetrated
            return obj.CompareTag("Wood") || obj.CompareTag("Thin") || obj.CompareTag("Glass");
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestHitValidationServerRpc(RaycastHit[] hits)
        {
            // Server validates hits to prevent cheating
            // In production, server would re-simulate and validate
            foreach (var hit in hits)
            {
                // Server processes hit
            }
        }

        #endregion

        #region Recoil System

        private void ApplyRecoil(bool isADS, bool isCrouching)
        {
            if (weaponData == null) return;

            float recoilMultiplier = 1f;
            if (isADS) recoilMultiplier *= aimDownSightRecoilMultiplier;
            if (isCrouching) recoilMultiplier *= crouchRecoilMultiplier;

            // Get recoil from pattern or procedural
            Vector3 recoil;
            if (useRecoilPatterns && currentRecoilPattern != null)
            {
                recoil = currentRecoilPattern.GetRecoilAtShot(shotsFired);
            }
            else
            {
                // Procedural recoil
                float verticalRecoil = weaponData.recoilVertical * recoilMultiplier;
                float horizontalRecoil = Random.Range(-weaponData.recoilHorizontal, weaponData.recoilHorizontal) * recoilMultiplier;
                recoil = new Vector3(verticalRecoil, horizontalRecoil, 0f);
            }

            targetRecoil += recoil;
            recoilRecoveryTimer = recoilRecoveryDelay;
        }

        private void UpdateRecoilRecovery()
        {
            // Smoothly recover from recoil
            if (recoilRecoveryTimer > 0f)
            {
                recoilRecoveryTimer -= Time.deltaTime;
                return;
            }

            float recoverySpeed = recoilRecoverySpeed.Evaluate(Time.time);
            currentRecoil = Vector3.Lerp(currentRecoil, Vector3.zero, recoverySpeed * Time.deltaTime);
            targetRecoil = Vector3.Lerp(targetRecoil, Vector3.zero, recoverySpeed * Time.deltaTime);

            // Reset shots fired when recoil is recovered
            if (currentRecoil.magnitude < 0.01f)
            {
                shotsFired = 0;
            }
        }

        public Vector3 GetCurrentRecoil()
        {
            currentRecoil = Vector3.Lerp(currentRecoil, targetRecoil, Time.deltaTime * 10f);
            return currentRecoil;
        }

        public void ResetRecoil()
        {
            currentRecoil = Vector3.zero;
            targetRecoil = Vector3.zero;
            shotsFired = 0;
        }

        #endregion

        #region Visual Effects

        private void PlayMuzzleFlash()
        {
            if (muzzleFlashPrefab != null && firePoint != null)
            {
                GameObject flash = Instantiate(muzzleFlashPrefab, firePoint.position, firePoint.rotation);
                flash.transform.SetParent(firePoint);
                Destroy(flash, muzzleFlashDuration);
            }

            if (muzzleFlashLight != null)
            {
                muzzleFlashLight.enabled = true;
                Invoke(nameof(DisableMuzzleFlashLight), muzzleFlashDuration);
            }
        }

        private void DisableMuzzleFlashLight()
        {
            if (muzzleFlashLight != null)
                muzzleFlashLight.enabled = false;
        }

        private void SpawnTracer(Vector3 origin, Vector3 direction)
        {
            if (tracerPrefab == null) return;

            GameObject tracer = Instantiate(tracerPrefab, origin, Quaternion.LookRotation(direction));

            // Animate tracer
            var tracerScript = tracer.GetComponent<BulletTracer>();
            if (tracerScript != null)
            {
                tracerScript.Initialize(origin, direction, tracerSpeed);
            }
            else
            {
                Destroy(tracer, 2f);
            }
        }

        private void SpawnImpactEffect(RaycastHit hit)
        {
            GameObject impactPrefab = GetImpactPrefabForSurface(hit.collider.tag);
            if (impactPrefab == null)
                impactPrefab = defaultImpactVFX;

            if (impactPrefab != null)
            {
                GameObject impact = Instantiate(impactPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(impact, 2f);
            }

            // Play impact sound
            PlayImpactSound(hit.collider.tag, hit.point);
        }

        private GameObject GetImpactPrefabForSurface(string surfaceTag)
        {
            switch (surfaceTag)
            {
                case "Flesh":
                case "Player":
                case "Zombie":
                    return fleshImpactVFX;

                case "Metal":
                    return metalImpactVFX;

                case "Wood":
                    return woodImpactVFX;

                case "Concrete":
                case "Stone":
                    return concreteImpactVFX;

                default:
                    return defaultImpactVFX;
            }
        }

        private void PlayImpactSound(string surfaceTag, Vector3 position)
        {
            // Play impact sound based on surface type
            // Would use AudioManager with appropriate clip
            string soundName = $"Impact_{surfaceTag}";
            Core.AudioManager.Instance?.PlaySFX(soundName, position);
        }

        #endregion

        #region Public Getters

        public Vector3 GetCurrentRecoilOffset() => currentRecoil;

        public int GetShotsFired() => shotsFired;

        #endregion
    }

    #region Supporting Classes

    [System.Serializable]
    public class RecoilPattern
    {
        public string patternName;
        public Vector3[] recoilPoints;
        public bool loopPattern = true;

        public Vector3 GetRecoilAtShot(int shotNumber)
        {
            if (recoilPoints == null || recoilPoints.Length == 0)
                return Vector3.zero;

            int index = shotNumber;

            if (loopPattern)
            {
                index = shotNumber % recoilPoints.Length;
            }
            else
            {
                index = Mathf.Min(shotNumber, recoilPoints.Length - 1);
            }

            return recoilPoints[index];
        }
    }

    public class BulletTracer : MonoBehaviour
    {
        private Vector3 startPosition;
        private Vector3 direction;
        private float speed;
        private float traveledDistance;
        private float maxDistance = 500f;

        public void Initialize(Vector3 origin, Vector3 dir, float tracerSpeed)
        {
            startPosition = origin;
            direction = dir;
            speed = tracerSpeed;
        }

        private void Update()
        {
            float distance = speed * Time.deltaTime;
            transform.position += direction * distance;
            traveledDistance += distance;

            if (traveledDistance >= maxDistance)
            {
                Destroy(gameObject);
            }
        }
    }

    #endregion
}
