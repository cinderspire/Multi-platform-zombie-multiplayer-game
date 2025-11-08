using UnityEngine;

namespace DeadFrontier.Player.Perks
{
    /// <summary>
    /// ScriptableObject that defines a perk's properties and effects
    /// </summary>
    [CreateAssetMenu(fileName = "New Perk", menuName = "DeadFrontier/Perk Data")]
    public class PerkData : ScriptableObject
    {
        [Header("Basic Info")]
        public string perkName = "New Perk";
        public string perkID; // Unique identifier

        [TextArea(3, 5)]
        public string description = "Perk description";

        public Sprite icon;
        public PerkCategory category = PerkCategory.Combat;
        public PerkRarity rarity = PerkRarity.Common;

        [Header("Requirements")]
        public int levelRequired = 1;
        public string[] prerequisitePerks; // Must have these perks first

        [Header("Stats")]
        public PerkStat[] statModifiers;

        [Header("Special Effects")]
        public bool hasSpecialEffect = false;
        public PerkEffectType specialEffect;
        public float effectValue;
        public float effectDuration;

        /// <summary>
        /// Applies this perk's effects to a player
        /// </summary>
        public void Apply(PlayerController player)
        {
            if (player == null)
                return;

            // Apply stat modifiers
            foreach (var stat in statModifiers)
            {
                ApplyStatModifier(player, stat);
            }

            // Apply special effects
            if (hasSpecialEffect)
            {
                ApplySpecialEffect(player);
            }

            Debug.Log($"[PerkData] Applied perk: {perkName}");
        }

        /// <summary>
        /// Removes this perk's effects from a player
        /// </summary>
        public void Remove(PlayerController player)
        {
            if (player == null)
                return;

            // Remove stat modifiers
            foreach (var stat in statModifiers)
            {
                RemoveStatModifier(player, stat);
            }

            Debug.Log($"[PerkData] Removed perk: {perkName}");
        }

        private void ApplyStatModifier(PlayerController player, PerkStat stat)
        {
            switch (stat.statType)
            {
                case StatType.MaxHealth:
                    player.GetComponent<PlayerHealth>()?.ModifyMaxHealth(stat.value, stat.isMultiplicative);
                    break;

                case StatType.MovementSpeed:
                    player.GetComponent<PlayerMovement>()?.ModifySpeed(stat.value, stat.isMultiplicative);
                    break;

                case StatType.SprintSpeed:
                    player.GetComponent<PlayerMovement>()?.ModifySprintSpeed(stat.value, stat.isMultiplicative);
                    break;

                case StatType.StaminaRegen:
                    player.GetComponent<PlayerMovement>()?.ModifyStaminaRegen(stat.value, stat.isMultiplicative);
                    break;

                case StatType.WeaponDamage:
                    // Apply to all weapons
                    break;

                case StatType.ReloadSpeed:
                    // Reduce reload time
                    break;

                case StatType.AmmoCapacity:
                    // Increase magazine size
                    break;

                case StatType.RecoilReduction:
                    // Reduce weapon recoil
                    break;

                // ... more stat types
            }
        }

        private void RemoveStatModifier(PlayerController player, PerkStat stat)
        {
            // Reverse the stat modifier
            float reverseValue = stat.isMultiplicative ? 1f / stat.value : -stat.value;

            PerkStat reverseStat = new PerkStat
            {
                statType = stat.statType,
                value = reverseValue,
                isMultiplicative = stat.isMultiplicative
            };

            ApplyStatModifier(player, reverseStat);
        }

        private void ApplySpecialEffect(PlayerController player)
        {
            switch (specialEffect)
            {
                case PerkEffectType.DoubleJump:
                    // Enable double jump
                    break;

                case PerkEffectType.QuickRevive:
                    // Faster revive speed
                    break;

                case PerkEffectType.Scavenger:
                    // Increased ammo pickup
                    break;

                case PerkEffectType.SilentMovement:
                    // Reduced noise generation
                    break;

                case PerkEffectType.LastStand:
                    // Survive fatal damage once per match
                    break;

                case PerkEffectType.Lightweight:
                    // Faster movement when not carrying heavy items
                    break;

                case PerkEffectType.Stopping Power:
                    // Increased bullet damage
                    break;

                case PerkEffectType.SteadyAim:
                    // Reduced hipfire spread
                    break;

                case PerkEffectType.Sleight of Hand:
                    // Faster reload
                    break;

                case PerkEffectType.Commando:
                    // Increased melee range
                    break;
            }
        }

        /// <summary>
        /// Gets the display name with rarity color
        /// </summary>
        public string GetColoredName()
        {
            Color color = GetRarityColor();
            string hexColor = ColorUtility.ToHtmlStringRGB(color);
            return $"<color=#{hexColor}>{perkName}</color>";
        }

        /// <summary>
        /// Gets the rarity color
        /// </summary>
        public Color GetRarityColor()
        {
            return rarity switch
            {
                PerkRarity.Common => new Color(0.7f, 0.7f, 0.7f),
                PerkRarity.Uncommon => new Color(0.2f, 0.8f, 0.2f),
                PerkRarity.Rare => new Color(0.3f, 0.5f, 1f),
                PerkRarity.Epic => new Color(0.7f, 0.3f, 1f),
                PerkRarity.Legendary => new Color(1f, 0.6f, 0f),
                _ => Color.white
            };
        }

        private void OnValidate()
        {
            // Auto-generate perk ID
            if (string.IsNullOrEmpty(perkID))
            {
                perkID = System.Guid.NewGuid().ToString();
            }
        }
    }

    #region Enums and Structs

    public enum PerkCategory
    {
        Combat,
        Survival,
        Utility,
        Movement,
        Support
    }

    public enum PerkRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    public enum StatType
    {
        MaxHealth,
        HealthRegen,
        MovementSpeed,
        SprintSpeed,
        StaminaMax,
        StaminaRegen,
        WeaponDamage,
        HeadshotDamage,
        ReloadSpeed,
        AmmoCapacity,
        RecoilReduction,
        AimSpeed,
        SwapSpeed,
        MeleeRange,
        MeleeDamage,
        NoiseReduction,
        LootingSpeed,
        ReviveSpeed,
        ExtractionSpeed
    }

    public enum PerkEffectType
    {
        None,
        DoubleJump,
        QuickRevive,
        Scavenger,
        SilentMovement,
        LastStand,
        Lightweight,
        StoppingPower,
        SteadyAim,
        SleightOfHand,
        Commando,
        Hardline,
        ColdBlooded,
        Ninja,
        Jugger naut,
        Marathon
    }

    [System.Serializable]
    public struct PerkStat
    {
        public StatType statType;

        [Tooltip("Additive: +X, Multiplicative: *X")]
        public float value;

        [Tooltip("If true, multiplies the stat. If false, adds to it.")]
        public bool isMultiplicative;
    }

    #endregion
}
