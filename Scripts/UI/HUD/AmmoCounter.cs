using UnityEngine;
using TMPro;

namespace DeadFrontier.UI
{
    /// <summary>
    /// Displays current weapon ammo count
    /// </summary>
    public class AmmoCounter : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI ammoText;
        [SerializeField] private TextMeshProUGUI weaponNameText;

        [Header("Settings")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color lowAmmoColor = Color.yellow;
        [SerializeField] private Color emptyColor = Color.red;
        [SerializeField] private int lowAmmoThreshold = 5;

        private Weapons.WeaponController currentWeapon;

        private void Start()
        {
            FindCurrentWeapon();
        }

        private void FindCurrentWeapon()
        {
            // Find local player's weapon
            var player = FindObjectOfType<Player.PlayerController>();
            if (player != null)
            {
                currentWeapon = player.GetComponentInChildren<Weapons.WeaponController>();
                if (currentWeapon != null)
                {
                    currentWeapon.OnAmmoChanged += UpdateAmmoDisplay;
                    UpdateAmmoDisplay();

                    // Update weapon name
                    if (weaponNameText != null && currentWeapon.Data != null)
                    {
                        weaponNameText.text = currentWeapon.Data.weaponName;
                    }
                }
            }
        }

        private void UpdateAmmoDisplay()
        {
            if (currentWeapon == null || ammoText == null)
                return;

            int currentAmmo = currentWeapon.CurrentAmmo;
            int reserveAmmo = currentWeapon.ReserveAmmo;

            // Format: "30 / 120"
            ammoText.text = $"{currentAmmo} / {reserveAmmo}";

            // Change color based on ammo count
            if (currentAmmo == 0)
            {
                ammoText.color = emptyColor;
            }
            else if (currentAmmo <= lowAmmoThreshold)
            {
                ammoText.color = lowAmmoColor;
            }
            else
            {
                ammoText.color = normalColor;
            }
        }

        /// <summary>
        /// Updates when weapon changes
        /// </summary>
        public void OnWeaponChanged(Weapons.WeaponController newWeapon)
        {
            if (currentWeapon != null)
            {
                currentWeapon.OnAmmoChanged -= UpdateAmmoDisplay;
            }

            currentWeapon = newWeapon;

            if (currentWeapon != null)
            {
                currentWeapon.OnAmmoChanged += UpdateAmmoDisplay;
                UpdateAmmoDisplay();

                if (weaponNameText != null && currentWeapon.Data != null)
                {
                    weaponNameText.text = currentWeapon.Data.weaponName;
                }
            }
        }

        private void OnDestroy()
        {
            if (currentWeapon != null)
            {
                currentWeapon.OnAmmoChanged -= UpdateAmmoDisplay;
            }
        }
    }
}
