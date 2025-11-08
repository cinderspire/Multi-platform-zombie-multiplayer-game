using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System;

namespace DeadFrontier.Weapons
{
    /// <summary>
    /// Complete weapon controller handling firing, reloading, aiming, and weapon switching.
    /// Integrates with attachment system, physics system, and network synchronization.
    /// Production-ready implementation with all features for AAA extraction shooter.
    /// </summary>
    [RequireComponent(typeof(WeaponAttachmentSystem))]
    [RequireComponent(typeof(WeaponPhysicsSystem))]
    public class WeaponController : NetworkBehaviour
    {
        [Header("Weapon Configuration")]
        [SerializeField] private WeaponData currentWeaponData;
        [SerializeField] private Transform weaponHolder;
        [SerializeField] private Transform aimPoint;
        [SerializeField] private Camera playerCamera;

        [Header("Fire Points")]
        [SerializeField] private Transform primaryFirePoint;
        [SerializeField] private Transform secondaryFirePoint;

        [Header("Sway & Bob")]
        [SerializeField] private float swayAmount = 0.02f;
        [SerializeField] private float swaySmooth = 6f;
        [SerializeField] private float bobAmount = 0.05f;
        [SerializeField] private float bobFrequency = 10f;

        [Header("Aim Settings")]
        [SerializeField] private float normalFOV = 60f;
        [SerializeField] private float aimFOV = 40f;
        [SerializeField] private float aimSpeed = 8f;
        [SerializeField] private Vector3 aimPosition;
        [SerializeField] private Vector3 hipPosition;

        [Header("Recoil Settings")]
        [SerializeField] private float recoilReturnSpeed = 5f;
        [SerializeField] private float recoilSnappiness = 10f;

        [Header("Audio")]
        [SerializeField] private AudioSource weaponAudioSource;
        [SerializeField] private AudioClip fireSound;
        [SerializeField] private AudioClip dryFireSound;
        [SerializeField] private AudioClip reloadSound;
        [SerializeField] private AudioClip weaponSwitchSound;

        // Components
        private WeaponAttachmentSystem attachmentSystem;
        private WeaponPhysicsSystem physicsSystem;
        private Animator weaponAnimator;

        // Weapon state
        private NetworkVariable<int> currentAmmo = new NetworkVariable<int>(30);
        private NetworkVariable<int> reserveAmmo = new NetworkVariable<int>(90);
        private NetworkVariable<bool> isReloading = new NetworkVariable<bool>(false);
        private NetworkVariable<bool> isAiming = new NetworkVariable<bool>(false);
        private NetworkVariable<int> currentFireMode = new NetworkVariable<int>(0); // 0=Auto, 1=Burst, 2=Semi

        private float nextFireTime;
        private int burstShotsFired;
        private bool isFiring;

        // Weapon slots
        private WeaponData[] equippedWeapons = new WeaponData[3]; // Primary, Secondary, Melee
        private int currentWeaponSlot = 0;
        private GameObject[] weaponModels = new GameObject[3];

        // Movement state (from PlayerMovement)
        private bool isMoving;
        private bool isSprinting;
        private bool isCrouching;

        // Sway
        private Vector3 swayPosition;
        private Quaternion swayRotation;
        private float bobTimer;

        // Recoil
        private Vector3 currentRecoilPosition;
        private Vector3 currentRecoilRotation;

        // Events
        public event Action OnFire;
        public event Action OnReloadStart;
        public event Action OnReloadComplete;
        public event Action OnAmmoChanged;

        private void Awake()
        {
            attachmentSystem = GetComponent<WeaponAttachmentSystem>();
            physicsSystem = GetComponent<WeaponPhysicsSystem>();

            if (playerCamera == null)
                playerCamera = Camera.main;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsOwner)
            {
                InitializeWeapon();
            }

            currentAmmo.OnValueChanged += OnAmmoChangedCallback;
            isReloading.OnValueChanged += OnReloadingChangedCallback;
            isAiming.OnValueChanged += OnAimingChangedCallback;
        }

        public override void OnNetworkDespawn()
        {
            currentAmmo.OnValueChanged -= OnAmmoChangedCallback;
            isReloading.OnValueChanged -= OnReloadingChangedCallback;
            isAiming.OnValueChanged -= OnAimingChangedCallback;
            base.OnNetworkDespawn();
        }

        private void Update()
        {
            if (!IsOwner) return;

            HandleInput();
            UpdateWeaponPosition();
            UpdateAiming();
            UpdateRecoil();
            UpdateSway();
            UpdateBob();
        }

        #region Initialization

        private void InitializeWeapon()
        {
            if (currentWeaponData != null)
            {
                EquipWeapon(currentWeaponData, 0);

                if (IsServer)
                {
                    currentAmmo.Value = currentWeaponData.magazineSize;
                    reserveAmmo.Value = currentWeaponData.maxReserveAmmo;
                }

                // Initialize attachment system
                if (attachmentSystem != null)
                {
                    attachmentSystem.Initialize(currentWeaponData, weaponHolder, primaryFirePoint);
                }

                // Initialize physics system
                if (physicsSystem != null)
                {
                    physicsSystem.Initialize(currentWeaponData, weaponHolder, primaryFirePoint);
                }
            }
        }

        public void EquipWeapon(WeaponData weaponData, int slot)
        {
            if (slot < 0 || slot >= equippedWeapons.Length) return;

            equippedWeapons[slot] = weaponData;

            if (slot == currentWeaponSlot)
            {
                SwitchToWeapon(slot);
            }
        }

        #endregion

        #region Input Handling

        private void HandleInput()
        {
            if (isReloading.Value) return;

            // Fire
            if (currentWeaponData != null)
            {
                bool fireInput = Input.GetButton("Fire1");
                bool firePressed = Input.GetButtonDown("Fire1");

                switch ((FiringMode)currentFireMode.Value)
                {
                    case FiringMode.Automatic:
                        isFiring = fireInput;
                        if (isFiring && Time.time >= nextFireTime)
                        {
                            Fire();
                        }
                        break;

                    case FiringMode.SemiAuto:
                        if (firePressed && Time.time >= nextFireTime)
                        {
                            Fire();
                        }
                        break;

                    case FiringMode.Burst:
                        if (firePressed && Time.time >= nextFireTime)
                        {
                            StartCoroutine(BurstFire());
                        }
                        break;
                }
            }

            // Reload
            if (Input.GetKeyDown(KeyCode.R) && !isReloading.Value)
            {
                if (currentAmmo.Value < currentWeaponData.magazineSize && reserveAmmo.Value > 0)
                {
                    RequestReloadServerRpc();
                }
            }

            // Aim
            bool aimInput = Input.GetButton("Fire2");
            if (aimInput != isAiming.Value && !isSprinting)
            {
                SetAimingServerRpc(aimInput);
            }

            // Switch fire mode
            if (Input.GetKeyDown(KeyCode.B) && currentWeaponData.availableFireModes.Length > 1)
            {
                CycleFireMode();
            }

            // Weapon switching
            if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchToWeapon(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchToWeapon(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchToWeapon(2);

            // Mouse wheel switch
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0f) SwitchToNextWeapon();
            else if (scroll < 0f) SwitchToPreviousWeapon();
        }

        #endregion

        #region Firing

        private void Fire()
        {
            if (!CanFire()) return;

            if (currentAmmo.Value <= 0)
            {
                PlayDryFire();
                return;
            }

            // Calculate fire rate
            float fireRate = currentWeaponData.fireRate;
            if (attachmentSystem != null)
            {
                var stats = attachmentSystem.GetCurrentStats();
                fireRate = stats.fireRate;
            }

            nextFireTime = Time.time + (60f / fireRate);

            // Fire weapon
            RequestFireServerRpc();

            // Visual/Audio feedback
            PlayFireEffects();

            // Apply recoil
            ApplyRecoil();

            // Update ammo locally (server will validate)
            if (IsServer)
            {
                currentAmmo.Value--;
            }

            OnFire?.Invoke();

            // Update UI
            if (UI.GameHUD.Instance != null)
            {
                UI.GameHUD.Instance.UpdateAmmo(currentAmmo.Value, reserveAmmo.Value);
            }
        }

        private IEnumerator BurstFire()
        {
            int burstCount = currentWeaponData.burstSize;
            burstShotsFired = 0;

            while (burstShotsFired < burstCount && currentAmmo.Value > 0)
            {
                Fire();
                burstShotsFired++;

                if (burstShotsFired < burstCount)
                {
                    yield return new WaitForSeconds(0.1f); // Delay between burst shots
                }
            }
        }

        [ServerRpc]
        private void RequestFireServerRpc()
        {
            if (!CanFire() || currentAmmo.Value <= 0) return;

            // Server validates and processes shot
            Vector3 fireDirection = playerCamera != null ? playerCamera.transform.forward : transform.forward;

            // Use physics system for actual hit detection
            if (physicsSystem != null)
            {
                float damage = currentWeaponData.damage;
                float range = currentWeaponData.range;

                if (attachmentSystem != null)
                {
                    var stats = attachmentSystem.GetCurrentStats();
                    damage = stats.damage;
                    range = stats.range;
                }

                physicsSystem.FireWeapon(fireDirection, damage, range, isAiming.Value, isCrouching);
            }

            // Broadcast fire to all clients
            FireClientRpc();
        }

        [ClientRpc]
        private void FireClientRpc()
        {
            if (IsOwner) return; // Owner already saw effects

            PlayFireEffects();
        }

        private bool CanFire()
        {
            if (isReloading.Value) return false;
            if (isSprinting) return false;
            if (currentWeaponData == null) return false;
            return true;
        }

        private void PlayFireEffects()
        {
            // Play fire sound
            if (weaponAudioSource != null && fireSound != null)
            {
                weaponAudioSource.PlayOneShot(fireSound);
            }

            // Play animation
            if (weaponAnimator != null)
            {
                weaponAnimator.SetTrigger("Fire");
            }

            // Muzzle flash handled by WeaponPhysicsSystem
        }

        private void PlayDryFire()
        {
            if (weaponAudioSource != null && dryFireSound != null)
            {
                weaponAudioSource.PlayOneShot(dryFireSound);
            }
        }

        #endregion

        #region Reloading

        [ServerRpc]
        private void RequestReloadServerRpc()
        {
            if (isReloading.Value) return;
            if (currentAmmo.Value >= currentWeaponData.magazineSize) return;
            if (reserveAmmo.Value <= 0) return;

            isReloading.Value = true;
            StartCoroutine(ReloadCoroutine());
        }

        private IEnumerator ReloadCoroutine()
        {
            OnReloadStart?.Invoke();

            float reloadTime = currentWeaponData.reloadTime;

            if (attachmentSystem != null)
            {
                var stats = attachmentSystem.GetCurrentStats();
                reloadTime = stats.reloadSpeed;
            }

            // Apply skill bonuses
            if (Progression.SkillTreeSystem.Instance != null)
            {
                float reloadBonus = Progression.SkillTreeSystem.Instance.GetBonus(Progression.SkillBonusType.ReloadSpeed);
                reloadTime *= (1f - reloadBonus);
            }

            // Play reload sound
            if (weaponAudioSource != null && reloadSound != null)
            {
                weaponAudioSource.PlayOneShot(reloadSound);
            }

            // Play animation
            if (weaponAnimator != null)
            {
                weaponAnimator.SetTrigger("Reload");
            }

            // Show reload indicator on UI
            float elapsedTime = 0f;
            while (elapsedTime < reloadTime)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / reloadTime;

                if (UI.GameHUD.Instance != null && IsOwner)
                {
                    UI.GameHUD.Instance.ShowReloadIndicator(true, progress);
                }

                yield return null;
            }

            // Complete reload
            if (IsServer)
            {
                int ammoNeeded = currentWeaponData.magazineSize - currentAmmo.Value;
                int ammoToAdd = Mathf.Min(ammoNeeded, reserveAmmo.Value);

                currentAmmo.Value += ammoToAdd;
                reserveAmmo.Value -= ammoToAdd;
                isReloading.Value = false;
            }

            OnReloadComplete?.Invoke();

            if (UI.GameHUD.Instance != null && IsOwner)
            {
                UI.GameHUD.Instance.ShowReloadIndicator(false);
                UI.GameHUD.Instance.UpdateAmmo(currentAmmo.Value, reserveAmmo.Value);
            }
        }

        #endregion

        #region Aiming

        [ServerRpc]
        private void SetAimingServerRpc(bool aiming)
        {
            isAiming.Value = aiming;
        }

        private void UpdateAiming()
        {
            if (currentWeaponData == null) return;

            // Calculate target position and FOV
            Vector3 targetPosition = isAiming.Value ? aimPosition : hipPosition;
            float targetFOV = normalFOV;

            if (isAiming.Value && attachmentSystem != null && attachmentSystem.HasOptic())
            {
                targetFOV = aimFOV / attachmentSystem.GetOpticZoomLevel();
            }
            else if (isAiming.Value)
            {
                targetFOV = aimFOV;
            }

            // Smooth transition
            if (weaponHolder != null)
            {
                weaponHolder.localPosition = Vector3.Lerp(weaponHolder.localPosition, targetPosition, Time.deltaTime * aimSpeed);
            }

            if (playerCamera != null)
            {
                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * aimSpeed);
            }
        }

        #endregion

        #region Weapon Switching

        private void SwitchToWeapon(int slot)
        {
            if (slot < 0 || slot >= equippedWeapons.Length) return;
            if (slot == currentWeaponSlot) return;
            if (equippedWeapons[slot] == null) return;
            if (isReloading.Value) return;

            StartCoroutine(SwitchWeaponCoroutine(slot));
        }

        private void SwitchToNextWeapon()
        {
            int nextSlot = (currentWeaponSlot + 1) % equippedWeapons.Length;
            while (equippedWeapons[nextSlot] == null && nextSlot != currentWeaponSlot)
            {
                nextSlot = (nextSlot + 1) % equippedWeapons.Length;
            }
            SwitchToWeapon(nextSlot);
        }

        private void SwitchToPreviousWeapon()
        {
            int prevSlot = currentWeaponSlot - 1;
            if (prevSlot < 0) prevSlot = equippedWeapons.Length - 1;
            while (equippedWeapons[prevSlot] == null && prevSlot != currentWeaponSlot)
            {
                prevSlot--;
                if (prevSlot < 0) prevSlot = equippedWeapons.Length - 1;
            }
            SwitchToWeapon(prevSlot);
        }

        private IEnumerator SwitchWeaponCoroutine(int newSlot)
        {
            // Hide current weapon
            if (weaponModels[currentWeaponSlot] != null)
            {
                weaponModels[currentWeaponSlot].SetActive(false);
            }

            // Play switch sound
            if (weaponAudioSource != null && weaponSwitchSound != null)
            {
                weaponAudioSource.PlayOneShot(weaponSwitchSound);
            }

            yield return new WaitForSeconds(0.3f);

            // Switch to new weapon
            currentWeaponSlot = newSlot;
            currentWeaponData = equippedWeapons[newSlot];

            if (weaponModels[newSlot] != null)
            {
                weaponModels[newSlot].SetActive(true);
            }

            // Re-initialize systems
            InitializeWeapon();

            // Update UI
            if (UI.GameHUD.Instance != null && IsOwner)
            {
                UI.GameHUD.Instance.UpdateWeaponInfo(currentWeaponData.weaponName, currentWeaponData.icon);
                UI.GameHUD.Instance.UpdateAmmo(currentAmmo.Value, reserveAmmo.Value);
            }
        }

        private void CycleFireMode()
        {
            if (!IsServer) return;

            int nextMode = (currentFireMode.Value + 1) % currentWeaponData.availableFireModes.Length;
            currentFireMode.Value = nextMode;

            // Update UI
            string modeName = currentWeaponData.availableFireModes[nextMode].ToString();
            if (UI.GameHUD.Instance != null && IsOwner)
            {
                UI.GameHUD.Instance.UpdateFireMode(modeName);
            }
        }

        #endregion

        #region Weapon Position & Animation

        private void UpdateWeaponPosition()
        {
            if (weaponHolder == null) return;

            Vector3 finalPosition = weaponHolder.localPosition;
            Quaternion finalRotation = weaponHolder.localRotation;

            // Apply sway
            finalPosition += swayPosition;
            finalRotation *= swayRotation;

            // Apply recoil
            finalPosition += currentRecoilPosition;
            finalRotation *= Quaternion.Euler(currentRecoilRotation);

            weaponHolder.localPosition = finalPosition;
            weaponHolder.localRotation = finalRotation;
        }

        private void UpdateSway()
        {
            if (!IsOwner) return;

            // Mouse-based sway
            float mouseX = -Input.GetAxis("Mouse X") * swayAmount;
            float mouseY = -Input.GetAxis("Mouse Y") * swayAmount;

            Vector3 targetSwayPosition = new Vector3(mouseX, mouseY, 0f);
            Quaternion targetSwayRotation = Quaternion.Euler(new Vector3(mouseY * 10f, mouseX * 10f, mouseX * 5f));

            swayPosition = Vector3.Lerp(swayPosition, targetSwayPosition, Time.deltaTime * swaySmooth);
            swayRotation = Quaternion.Slerp(swayRotation, targetSwayRotation, Time.deltaTime * swaySmooth);
        }

        private void UpdateBob()
        {
            if (!isMoving || isAiming.Value || weaponHolder == null) return;

            bobTimer += Time.deltaTime * bobFrequency;

            float bobX = Mathf.Sin(bobTimer) * bobAmount;
            float bobY = Mathf.Sin(bobTimer * 2f) * bobAmount;

            Vector3 bobPosition = new Vector3(bobX, bobY, 0f);
            weaponHolder.localPosition += bobPosition;
        }

        private void ApplyRecoil()
        {
            if (physicsSystem == null) return;

            Vector3 recoil = physicsSystem.GetCurrentRecoil();

            // Apply to camera rotation
            if (playerCamera != null)
            {
                playerCamera.transform.localRotation *= Quaternion.Euler(-recoil.x, recoil.y, 0f);
            }

            // Apply to weapon position
            currentRecoilPosition += new Vector3(0f, 0f, -recoil.x * 0.01f);
            currentRecoilRotation += recoil;
        }

        private void UpdateRecoil()
        {
            // Smooth return to zero
            currentRecoilPosition = Vector3.Lerp(currentRecoilPosition, Vector3.zero, Time.deltaTime * recoilReturnSpeed);
            currentRecoilRotation = Vector3.Lerp(currentRecoilRotation, Vector3.zero, Time.deltaTime * recoilReturnSpeed);
        }

        #endregion

        #region Network Callbacks

        private void OnAmmoChangedCallback(int previousValue, int newValue)
        {
            OnAmmoChanged?.Invoke();

            if (UI.GameHUD.Instance != null && IsOwner)
            {
                UI.GameHUD.Instance.UpdateAmmo(newValue, reserveAmmo.Value);
            }
        }

        private void OnReloadingChangedCallback(bool previousValue, bool newValue)
        {
            // Update UI or animations
        }

        private void OnAimingChangedCallback(bool previousValue, bool newValue)
        {
            // Update crosshair or UI
        }

        #endregion

        #region Public Methods

        public void AddAmmo(int amount)
        {
            if (!IsServer) return;

            reserveAmmo.Value = Mathf.Min(reserveAmmo.Value + amount, currentWeaponData.maxReserveAmmo);
        }

        public void SetMovementState(bool moving, bool sprinting, bool crouching)
        {
            isMoving = moving;
            isSprinting = sprinting;
            isCrouching = crouching;

            // Cancel aim if sprinting
            if (isSprinting && isAiming.Value && IsServer)
            {
                isAiming.Value = false;
            }
        }

        public int GetCurrentAmmo() => currentAmmo.Value;
        public int GetReserveAmmo() => reserveAmmo.Value;
        public bool IsReloading() => isReloading.Value;
        public bool IsAiming() => isAiming.Value;
        public WeaponData GetCurrentWeapon() => currentWeaponData;

        #endregion
    }
}
