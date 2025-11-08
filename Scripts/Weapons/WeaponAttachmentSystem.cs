using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace DeadFrontier.Weapons
{
    /// <summary>
    /// Comprehensive weapon attachment system supporting optics, barrels, grips, magazines, and more.
    /// Attachments modify weapon stats and behavior for deep customization.
    /// </summary>
    public class WeaponAttachmentSystem : MonoBehaviour
    {
        [Header("Attachment Slots")]
        [SerializeField] private AttachmentSlot opticSlot;
        [SerializeField] private AttachmentSlot barrelSlot;
        [SerializeField] private AttachmentSlot gripSlot;
        [SerializeField] private AttachmentSlot magazineSlot;
        [SerializeField] private AttachmentSlot stockSlot;
        [SerializeField] private AttachmentSlot laserSlot;

        [Header("Visual Attachment Points")]
        [SerializeField] private Transform opticAttachPoint;
        [SerializeField] private Transform barrelAttachPoint;
        [SerializeField] private Transform gripAttachPoint;
        [SerializeField] private Transform magazineAttachPoint;
        [SerializeField] private Transform stockAttachPoint;
        [SerializeField] private Transform laserAttachPoint;

        private Dictionary<AttachmentSlotType, AttachmentSlot> slotsByType = new Dictionary<AttachmentSlotType, AttachmentSlot>();
        private Dictionary<AttachmentSlotType, GameObject> visualAttachments = new Dictionary<AttachmentSlotType, GameObject>();

        private WeaponData baseWeaponData;
        private WeaponStats currentStats;

        // Events
        public event System.Action<AttachmentData, AttachmentSlotType> OnAttachmentEquipped;
        public event System.Action<AttachmentData, AttachmentSlotType> OnAttachmentRemoved;
        public event System.Action<WeaponStats> OnStatsChanged;

        private void Awake()
        {
            InitializeSlots();
        }

        private void InitializeSlots()
        {
            slotsByType[AttachmentSlotType.Optic] = opticSlot;
            slotsByType[AttachmentSlotType.Barrel] = barrelSlot;
            slotsByType[AttachmentSlotType.Grip] = gripSlot;
            slotsByType[AttachmentSlotType.Magazine] = magazineSlot;
            slotsByType[AttachmentSlotType.Stock] = stockSlot;
            slotsByType[AttachmentSlotType.Laser] = laserSlot;
        }

        public void Initialize(WeaponData weaponData)
        {
            baseWeaponData = weaponData;
            currentStats = new WeaponStats(weaponData);
            RecalculateStats();
        }

        #region Attachment Management

        public bool EquipAttachment(AttachmentData attachment)
        {
            if (attachment == null) return false;

            var slotType = attachment.slotType;

            if (!slotsByType.ContainsKey(slotType)) return false;
            if (!slotsByType[slotType].isUnlocked) return false;

            // Check compatibility
            if (!IsAttachmentCompatible(attachment)) return false;

            // Remove existing attachment in slot
            if (slotsByType[slotType].equippedAttachment != null)
            {
                RemoveAttachment(slotType);
            }

            // Equip new attachment
            slotsByType[slotType].equippedAttachment = attachment;

            // Update visuals
            UpdateAttachmentVisual(slotType, attachment);

            // Recalculate stats
            RecalculateStats();

            OnAttachmentEquipped?.Invoke(attachment, slotType);

            Debug.Log($"[WeaponAttachment] Equipped {attachment.attachmentName} to {slotType} slot");
            return true;
        }

        public bool RemoveAttachment(AttachmentSlotType slotType)
        {
            if (!slotsByType.ContainsKey(slotType)) return false;

            var attachment = slotsByType[slotType].equippedAttachment;
            if (attachment == null) return false;

            slotsByType[slotType].equippedAttachment = null;

            // Remove visual
            RemoveAttachmentVisual(slotType);

            // Recalculate stats
            RecalculateStats();

            OnAttachmentRemoved?.Invoke(attachment, slotType);

            Debug.Log($"[WeaponAttachment] Removed {attachment.attachmentName} from {slotType} slot");
            return true;
        }

        private bool IsAttachmentCompatible(AttachmentData attachment)
        {
            // Check weapon type compatibility
            if (attachment.compatibleWeaponTypes != null && attachment.compatibleWeaponTypes.Length > 0)
            {
                if (!attachment.compatibleWeaponTypes.Contains(baseWeaponData.weaponType))
                    return false;
            }

            // Check specific weapon compatibility
            if (attachment.compatibleWeaponIds != null && attachment.compatibleWeaponIds.Length > 0)
            {
                if (!attachment.compatibleWeaponIds.Contains(baseWeaponData.weaponId))
                    return false;
            }

            return true;
        }

        #endregion

        #region Stats Calculation

        private void RecalculateStats()
        {
            // Start with base stats
            currentStats = new WeaponStats(baseWeaponData);

            // Apply all attachment modifiers
            foreach (var kvp in slotsByType)
            {
                if (kvp.Value.equippedAttachment != null)
                {
                    ApplyAttachmentModifiers(kvp.Value.equippedAttachment);
                }
            }

            OnStatsChanged?.Invoke(currentStats);
        }

        private void ApplyAttachmentModifiers(AttachmentData attachment)
        {
            foreach (var modifier in attachment.statModifiers)
            {
                switch (modifier.statType)
                {
                    case WeaponStatType.Damage:
                        currentStats.damage = ApplyModifier(currentStats.damage, modifier);
                        break;

                    case WeaponStatType.FireRate:
                        currentStats.fireRate = ApplyModifier(currentStats.fireRate, modifier);
                        break;

                    case WeaponStatType.Accuracy:
                        currentStats.accuracy = ApplyModifier(currentStats.accuracy, modifier);
                        break;

                    case WeaponStatType.Range:
                        currentStats.range = ApplyModifier(currentStats.range, modifier);
                        break;

                    case WeaponStatType.ReloadSpeed:
                        currentStats.reloadSpeed = ApplyModifier(currentStats.reloadSpeed, modifier);
                        break;

                    case WeaponStatType.Stability:
                        currentStats.stability = ApplyModifier(currentStats.stability, modifier);
                        break;

                    case WeaponStatType.MagazineSize:
                        currentStats.magazineSize = (int)ApplyModifier(currentStats.magazineSize, modifier);
                        break;

                    case WeaponStatType.ADSSpeed:
                        currentStats.aimDownSightSpeed = ApplyModifier(currentStats.aimDownSightSpeed, modifier);
                        break;

                    case WeaponStatType.MovementSpeed:
                        currentStats.movementSpeedMultiplier = ApplyModifier(currentStats.movementSpeedMultiplier, modifier);
                        break;

                    case WeaponStatType.RecoilControl:
                        currentStats.recoilControl = ApplyModifier(currentStats.recoilControl, modifier);
                        break;
                }
            }
        }

        private float ApplyModifier(float baseValue, StatModifier modifier)
        {
            switch (modifier.modifierType)
            {
                case ModifierType.Additive:
                    return baseValue + modifier.value;

                case ModifierType.Multiplicative:
                    return baseValue * (1f + modifier.value);

                case ModifierType.Override:
                    return modifier.value;

                default:
                    return baseValue;
            }
        }

        #endregion

        #region Visual Management

        private void UpdateAttachmentVisual(AttachmentSlotType slotType, AttachmentData attachment)
        {
            // Remove existing visual
            RemoveAttachmentVisual(slotType);

            if (attachment.visualPrefab == null) return;

            // Get attach point for slot
            Transform attachPoint = GetAttachPoint(slotType);
            if (attachPoint == null) return;

            // Instantiate visual
            GameObject visual = Instantiate(attachment.visualPrefab, attachPoint);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;

            visualAttachments[slotType] = visual;
        }

        private void RemoveAttachmentVisual(AttachmentSlotType slotType)
        {
            if (visualAttachments.ContainsKey(slotType))
            {
                if (visualAttachments[slotType] != null)
                    Destroy(visualAttachments[slotType]);

                visualAttachments.Remove(slotType);
            }
        }

        private Transform GetAttachPoint(AttachmentSlotType slotType)
        {
            switch (slotType)
            {
                case AttachmentSlotType.Optic: return opticAttachPoint;
                case AttachmentSlotType.Barrel: return barrelAttachPoint;
                case AttachmentSlotType.Grip: return gripAttachPoint;
                case AttachmentSlotType.Magazine: return magazineAttachPoint;
                case AttachmentSlotType.Stock: return stockAttachPoint;
                case AttachmentSlotType.Laser: return laserAttachPoint;
                default: return null;
            }
        }

        #endregion

        #region Special Features

        public bool HasOptic()
        {
            return opticSlot?.equippedAttachment != null;
        }

        public float GetOpticZoomLevel()
        {
            if (opticSlot?.equippedAttachment != null)
            {
                return opticSlot.equippedAttachment.zoomLevel;
            }
            return 1f;
        }

        public bool HasLaser()
        {
            return laserSlot?.equippedAttachment != null;
        }

        public void SetLaserActive(bool active)
        {
            if (visualAttachments.ContainsKey(AttachmentSlotType.Laser))
            {
                var laserObject = visualAttachments[AttachmentSlotType.Laser];
                if (laserObject != null)
                {
                    var laser = laserObject.GetComponent<LaserSight>();
                    if (laser != null)
                        laser.SetActive(active);
                }
            }
        }

        public bool HasExtendedMag()
        {
            return magazineSlot?.equippedAttachment != null &&
                   magazineSlot.equippedAttachment.attachmentName.Contains("Extended");
        }

        #endregion

        #region Public Getters

        public WeaponStats GetCurrentStats() => currentStats;

        public AttachmentData GetEquippedAttachment(AttachmentSlotType slotType)
        {
            if (slotsByType.ContainsKey(slotType))
                return slotsByType[slotType].equippedAttachment;
            return null;
        }

        public List<AttachmentData> GetAllEquippedAttachments()
        {
            var attachments = new List<AttachmentData>();
            foreach (var slot in slotsByType.Values)
            {
                if (slot.equippedAttachment != null)
                    attachments.Add(slot.equippedAttachment);
            }
            return attachments;
        }

        public bool IsSlotUnlocked(AttachmentSlotType slotType)
        {
            return slotsByType.ContainsKey(slotType) && slotsByType[slotType].isUnlocked;
        }

        public void UnlockSlot(AttachmentSlotType slotType)
        {
            if (slotsByType.ContainsKey(slotType))
                slotsByType[slotType].isUnlocked = true;
        }

        #endregion

        #region Serialization

        public AttachmentLoadout SaveLoadout()
        {
            var loadout = new AttachmentLoadout();
            loadout.attachments = new List<AttachmentData>();

            foreach (var attachment in GetAllEquippedAttachments())
            {
                loadout.attachments.Add(attachment);
            }

            return loadout;
        }

        public void LoadLoadout(AttachmentLoadout loadout)
        {
            // Clear all attachments
            foreach (var slotType in slotsByType.Keys.ToList())
            {
                RemoveAttachment(slotType);
            }

            // Equip saved attachments
            foreach (var attachment in loadout.attachments)
            {
                EquipAttachment(attachment);
            }
        }

        #endregion
    }

    #region Data Classes

    [System.Serializable]
    public class AttachmentSlot
    {
        public bool isUnlocked = true;
        public AttachmentData equippedAttachment;
    }

    [System.Serializable]
    public class WeaponStats
    {
        public float damage;
        public float fireRate;
        public float accuracy;
        public float range;
        public float reloadSpeed;
        public float stability;
        public int magazineSize;
        public float aimDownSightSpeed;
        public float movementSpeedMultiplier;
        public float recoilControl;

        public WeaponStats(WeaponData weaponData)
        {
            damage = weaponData.damage;
            fireRate = weaponData.fireRate;
            accuracy = 1f;
            range = weaponData.range;
            reloadSpeed = weaponData.reloadTime;
            stability = 1f;
            magazineSize = weaponData.magazineSize;
            aimDownSightSpeed = 1f;
            movementSpeedMultiplier = 1f;
            recoilControl = 1f;
        }
    }

    [System.Serializable]
    public class AttachmentLoadout
    {
        public List<AttachmentData> attachments;
    }

    #endregion
}
