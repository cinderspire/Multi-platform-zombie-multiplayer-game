using UnityEngine;

namespace DeadFrontier.Items
{
    /// <summary>
    /// ScriptableObject that defines an item's properties
    /// </summary>
    [CreateAssetMenu(fileName = "New Item", menuName = "DeadFrontier/Item Data")]
    public class ItemData : ScriptableObject
    {
        [Header("Basic Info")]
        public string itemName = "New Item";
        public string itemID; // Unique identifier
        public ItemType itemType = ItemType.Consumable;

        [TextArea(3, 6)]
        public string description = "Item description";

        [Header("Inventory")]
        public Sprite icon;
        public GameObject worldModelPrefab;
        public int maxStackSize = 1;
        public bool isStackable = false;
        public float weight = 1f;

        [Header("Rarity & Value")]
        public ItemRarity rarity = ItemRarity.Common;
        public int sellValue = 100;
        public int buyValue = 150;

        [Header("Usage")]
        public bool isConsumable = false;
        public float useTime = 1f; // Time to use the item
        public AudioClip useSound;

        [Header("Effects - Medical")]
        public float healAmount = 0f;
        public bool healOverTime = false;
        public float healDuration = 0f;
        public float staminaRestore = 0f;

        [Header("Effects - Buffs")]
        public bool grantsBuff = false;
        public BuffType buffType;
        public float buffDuration = 0f;
        public float buffStrength = 0f;

        [Header("Ammo (if ItemType is Ammo)")]
        public Weapons.WeaponType compatibleWeaponType;
        public int ammoAmount = 30;

        /// <summary>
        /// Uses the item and applies its effects
        /// </summary>
        public void Use(Player.PlayerHealth playerHealth = null, Player.PlayerMovement playerMovement = null)
        {
            if (!isConsumable)
                return;

            // Apply healing
            if (healAmount > 0f && playerHealth != null)
            {
                if (healOverTime)
                {
                    // Start heal over time coroutine
                    // This would be handled by a HealOverTime component
                    Debug.Log($"[ItemData] Healing {healAmount} HP over {healDuration} seconds");
                }
                else
                {
                    playerHealth.Heal(healAmount);
                    Debug.Log($"[ItemData] Healed {healAmount} HP instantly");
                }
            }

            // Restore stamina
            if (staminaRestore > 0f && playerMovement != null)
            {
                playerMovement.RestoreStamina(staminaRestore);
                Debug.Log($"[ItemData] Restored {staminaRestore} stamina");
            }

            // Apply buff
            if (grantsBuff)
            {
                ApplyBuff(playerHealth, playerMovement);
            }

            // Play use sound
            if (useSound != null)
            {
                Core.AudioManager.Instance.Play(useSound, Vector3.zero);
            }
        }

        private void ApplyBuff(Player.PlayerHealth playerHealth, Player.PlayerMovement playerMovement)
        {
            // TODO: Implement buff system
            Debug.Log($"[ItemData] Applied {buffType} buff for {buffDuration} seconds");
        }

        /// <summary>
        /// Gets the display name with rarity color
        /// </summary>
        public string GetColoredName()
        {
            Color color = GetRarityColor();
            string hexColor = ColorUtility.ToHtmlStringRGB(color);
            return $"<color=#{hexColor}>{itemName}</color>";
        }

        /// <summary>
        /// Gets the rarity color
        /// </summary>
        public Color GetRarityColor()
        {
            return rarity switch
            {
                ItemRarity.Common => new Color(0.7f, 0.7f, 0.7f), // Gray
                ItemRarity.Uncommon => new Color(0.2f, 0.8f, 0.2f), // Green
                ItemRarity.Rare => new Color(0.3f, 0.5f, 1f), // Blue
                ItemRarity.Epic => new Color(0.7f, 0.3f, 1f), // Purple
                ItemRarity.Legendary => new Color(1f, 0.6f, 0f), // Orange
                _ => Color.white
            };
        }

        private void OnValidate()
        {
            // Auto-generate item ID if empty
            if (string.IsNullOrEmpty(itemID))
            {
                itemID = System.Guid.NewGuid().ToString();
            }

            // Ensure stackable items have max stack > 1
            if (isStackable && maxStackSize <= 1)
            {
                maxStackSize = 10;
            }
        }
    }

    public enum ItemType
    {
        Weapon,
        Ammo,
        Medical,
        Consumable,
        Valuable,
        KeyItem,
        Attachment
    }

    public enum ItemRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    public enum BuffType
    {
        None,
        SpeedBoost,
        DamageReduction,
        DamageBoost,
        StaminaRegen,
        HealthRegen
    }
}
