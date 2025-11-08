using UnityEngine;
using UnityEngine.UI;

namespace DeadFrontier.UI.HUD
{
    /// <summary>
    /// Dynamic crosshair that reacts to weapon spread and hit feedback
    /// </summary>
    public class Crosshair : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform crosshairContainer;
        [SerializeField] private Image topLine;
        [SerializeField] private Image bottomLine;
        [SerializeField] private Image leftLine;
        [SerializeField] private Image rightLine;
        [SerializeField] private Image centerDot;

        [Header("Settings")]
        [SerializeField] private float baseGap = 10f;
        [SerializeField] private float maxGap = 50f;
        [SerializeField] private float spreadMultiplier = 2f;
        [SerializeField] private float smoothSpeed = 10f;

        [Header("Hit Marker")]
        [SerializeField] private Image hitMarker;
        [SerializeField] private Color normalHitColor = Color.white;
        [SerializeField] private Color headshotColor = Color.red;
        [SerializeField] private float hitMarkerDuration = 0.2f;

        [Header("Colors")]
        [SerializeField] private Color defaultColor = Color.white;
        [SerializeField] private Color enemyColor = Color.red;
        [SerializeField] private Color friendlyColor = Color.green;

        // State
        private float currentGap;
        private float targetGap;
        private float hitMarkerTimer;
        private Weapons.WeaponController currentWeapon;

        private void Start()
        {
            currentGap = baseGap;
            targetGap = baseGap;

            // Hide hit marker initially
            if (hitMarker != null)
                hitMarker.gameObject.SetActive(false);

            // Set default colors
            SetCrosshairColor(defaultColor);
        }

        private void Update()
        {
            // Update gap based on weapon spread
            UpdateGap();

            // Update hit marker
            UpdateHitMarker();

            // Apply positions
            ApplyCrosshairGap();
        }

        private void UpdateGap()
        {
            if (currentWeapon != null)
            {
                // Calculate gap based on weapon spread
                float spread = currentWeapon.CurrentSpread;
                targetGap = baseGap + (spread * spreadMultiplier);
                targetGap = Mathf.Clamp(targetGap, baseGap, maxGap);
            }
            else
            {
                targetGap = baseGap;
            }

            // Smooth interpolation
            currentGap = Mathf.Lerp(currentGap, targetGap, Time.deltaTime * smoothSpeed);
        }

        private void ApplyCrosshairGap()
        {
            if (topLine != null)
            {
                topLine.rectTransform.anchoredPosition = new Vector2(0f, currentGap);
            }

            if (bottomLine != null)
            {
                bottomLine.rectTransform.anchoredPosition = new Vector2(0f, -currentGap);
            }

            if (leftLine != null)
            {
                leftLine.rectTransform.anchoredPosition = new Vector2(-currentGap, 0f);
            }

            if (rightLine != null)
            {
                rightLine.rectTransform.anchoredPosition = new Vector2(currentGap, 0f);
            }
        }

        private void UpdateHitMarker()
        {
            if (hitMarkerTimer > 0f)
            {
                hitMarkerTimer -= Time.deltaTime;

                if (hitMarkerTimer <= 0f && hitMarker != null)
                {
                    hitMarker.gameObject.SetActive(false);
                }
            }
        }

        #region Public Methods

        /// <summary>
        /// Shows hit marker when hitting a target
        /// </summary>
        public void ShowHitMarker(bool isHeadshot = false)
        {
            if (hitMarker == null)
                return;

            hitMarker.gameObject.SetActive(true);
            hitMarker.color = isHeadshot ? headshotColor : normalHitColor;
            hitMarkerTimer = hitMarkerDuration;

            // Animate hit marker (scale punch)
            LeanTween.cancel(hitMarker.gameObject);
            hitMarker.transform.localScale = Vector3.one * 1.5f;
            LeanTween.scale(hitMarker.gameObject, Vector3.one, hitMarkerDuration * 0.5f)
                .setEase(LeanTweenType.easeOutBack);
        }

        /// <summary>
        /// Sets the current weapon to track spread
        /// </summary>
        public void SetWeapon(Weapons.WeaponController weapon)
        {
            currentWeapon = weapon;

            if (currentWeapon != null)
            {
                // Show crosshair
                SetCrosshairVisible(true);
            }
        }

        /// <summary>
        /// Sets crosshair color based on what's being aimed at
        /// </summary>
        public void SetCrosshairColor(Color color)
        {
            if (topLine != null) topLine.color = color;
            if (bottomLine != null) bottomLine.color = color;
            if (leftLine != null) leftLine.color = color;
            if (rightLine != null) rightLine.color = color;
            if (centerDot != null) centerDot.color = color;
        }

        /// <summary>
        /// Sets crosshair visibility
        /// </summary>
        public void SetCrosshairVisible(bool visible)
        {
            if (crosshairContainer != null)
            {
                crosshairContainer.gameObject.SetActive(visible);
            }
        }

        /// <summary>
        /// Sets crosshair to enemy color (aiming at enemy)
        /// </summary>
        public void HighlightEnemy()
        {
            SetCrosshairColor(enemyColor);
        }

        /// <summary>
        /// Sets crosshair to friendly color (aiming at teammate)
        /// </summary>
        public void HighlightFriendly()
        {
            SetCrosshairColor(friendlyColor);
        }

        /// <summary>
        /// Resets crosshair to default color
        /// </summary>
        public void ResetHighlight()
        {
            SetCrosshairColor(defaultColor);
        }

        /// <summary>
        /// Expands crosshair momentarily (when firing)
        /// </summary>
        public void FireExpansion(float amount = 5f)
        {
            targetGap += amount;
        }

        #endregion

        #region Aim Detection

        private void LateUpdate()
        {
            // Raycast to detect what we're aiming at
            DetectAimTarget();
        }

        private void DetectAimTarget()
        {
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 100f))
            {
                // Check what we hit
                if (hit.collider.CompareTag("Player"))
                {
                    // TODO: Check if friendly or enemy based on team
                    // For now, assume enemy
                    HighlightEnemy();
                }
                else if (hit.collider.CompareTag("Zombie"))
                {
                    HighlightEnemy();
                }
                else
                {
                    ResetHighlight();
                }
            }
            else
            {
                ResetHighlight();
            }
        }

        #endregion
    }
}
