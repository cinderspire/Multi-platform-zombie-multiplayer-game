using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace DeadFrontier.Player.Perks
{
    /// <summary>
    /// Manages player loadouts (weapons, perks, equipment)
    /// </summary>
    public class LoadoutSystem : MonoBehaviour
    {
        [Header("Loadout Slots")]
        [SerializeField] private int maxPrimaryWeapons = 2;
        [SerializeField] private int maxSecondaryWeapons = 1;
        [SerializeField] private int maxPerks = 3;
        [SerializeField] private int maxEquipment = 2;

        [Header("Current Loadout")]
        [SerializeField] private List<Weapons.WeaponData> primaryWeapons = new List<Weapons.WeaponData>();
        [SerializeField] private List<Weapons.WeaponData> secondaryWeapons = new List<Weapons.WeaponData>();
        [SerializeField] private List<PerkData> equippedPerks = new List<PerkData>();
        [SerializeField] private List<Items.ItemData> equipment = new List<Items.ItemData>();

        [Header("Saved Loadouts")]
        [SerializeField] private int maxSavedLoadouts = 5;
        private List<SavedLoadout> savedLoadouts = new List<SavedLoadout>();

        // References
        private PlayerController playerController;
        private PlayerProgression progression;

        // Events
        public event System.Action OnLoadoutChanged;

        private void Start()
        {
            playerController = GetComponent<PlayerController>();
            progression = GetComponent<PlayerProgression>();

            LoadLoadouts();
            ApplyCurrentLoadout();
        }

        #region Weapon Management

        /// <summary>
        /// Equips a primary weapon
        /// </summary>
        public bool EquipPrimaryWeapon(Weapons.WeaponData weapon, int slot = 0)
        {
            if (weapon == null)
                return false;

            if (slot < 0 || slot >= maxPrimaryWeapons)
            {
                Debug.LogWarning($"[LoadoutSystem] Invalid primary weapon slot: {slot}");
                return false;
            }

            // Check if unlocked
            if (progression != null && !progression.IsUnlocked(weapon.weaponID))
            {
                Debug.LogWarning($"[LoadoutSystem] Weapon {weapon.weaponName} is locked");
                return false;
            }

            // Ensure list is large enough
            while (primaryWeapons.Count <= slot)
            {
                primaryWeapons.Add(null);
            }

            primaryWeapons[slot] = weapon;
            OnLoadoutChanged?.Invoke();

            Debug.Log($"[LoadoutSystem] Equipped {weapon.weaponName} in primary slot {slot}");
            return true;
        }

        /// <summary>
        /// Equips a secondary weapon
        /// </summary>
        public bool EquipSecondaryWeapon(Weapons.WeaponData weapon, int slot = 0)
        {
            if (weapon == null)
                return false;

            if (slot < 0 || slot >= maxSecondaryWeapons)
                return false;

            // Check if unlocked
            if (progression != null && !progression.IsUnlocked(weapon.weaponID))
                return false;

            while (secondaryWeapons.Count <= slot)
            {
                secondaryWeapons.Add(null);
            }

            secondaryWeapons[slot] = weapon;
            OnLoadoutChanged?.Invoke();

            return true;
        }

        /// <summary>
        /// Gets equipped primary weapons
        /// </summary>
        public List<Weapons.WeaponData> GetPrimaryWeapons()
        {
            return new List<Weapons.WeaponData>(primaryWeapons);
        }

        /// <summary>
        /// Gets equipped secondary weapons
        /// </summary>
        public List<Weapons.WeaponData> GetSecondaryWeapons()
        {
            return new List<Weapons.WeaponData>(secondaryWeapons);
        }

        #endregion

        #region Perk Management

        /// <summary>
        /// Equips a perk
        /// </summary>
        public bool EquipPerk(PerkData perk)
        {
            if (perk == null)
                return false;

            // Check if already equipped
            if (equippedPerks.Contains(perk))
            {
                Debug.LogWarning($"[LoadoutSystem] Perk {perk.perkName} already equipped");
                return false;
            }

            // Check perk limit
            if (equippedPerks.Count >= maxPerks)
            {
                Debug.LogWarning($"[LoadoutSystem] Max perks ({maxPerks}) already equipped");
                return false;
            }

            // Check level requirement
            if (progression != null && progression.Level < perk.levelRequired)
            {
                Debug.LogWarning($"[LoadoutSystem] Level {perk.levelRequired} required for {perk.perkName}");
                return false;
            }

            // Check if unlocked
            if (progression != null && !progression.IsUnlocked(perk.perkID))
            {
                Debug.LogWarning($"[LoadoutSystem] Perk {perk.perkName} is locked");
                return false;
            }

            // Check prerequisites
            if (perk.prerequisitePerks != null && perk.prerequisitePerks.Length > 0)
            {
                foreach (var prereq in perk.prerequisitePerks)
                {
                    if (!equippedPerks.Any(p => p.perkID == prereq))
                    {
                        Debug.LogWarning($"[LoadoutSystem] Prerequisite perk required for {perk.perkName}");
                        return false;
                    }
                }
            }

            // Equip the perk
            equippedPerks.Add(perk);
            perk.Apply(playerController);
            OnLoadoutChanged?.Invoke();

            Debug.Log($"[LoadoutSystem] Equipped perk: {perk.perkName}");
            return true;
        }

        /// <summary>
        /// Unequips a perk
        /// </summary>
        public bool UnequipPerk(PerkData perk)
        {
            if (perk == null || !equippedPerks.Contains(perk))
                return false;

            equippedPerks.Remove(perk);
            perk.Remove(playerController);
            OnLoadoutChanged?.Invoke();

            Debug.Log($"[LoadoutSystem] Unequipped perk: {perk.perkName}");
            return true;
        }

        /// <summary>
        /// Gets all equipped perks
        /// </summary>
        public List<PerkData> GetEquippedPerks()
        {
            return new List<PerkData>(equippedPerks);
        }

        /// <summary>
        /// Checks if a perk is equipped
        /// </summary>
        public bool HasPerk(PerkData perk)
        {
            return equippedPerks.Contains(perk);
        }

        /// <summary>
        /// Checks if a perk is equipped by ID
        /// </summary>
        public bool HasPerk(string perkID)
        {
            return equippedPerks.Any(p => p.perkID == perkID);
        }

        /// <summary>
        /// Gets total stat modifier from all perks
        /// </summary>
        public float GetStatModifier(StatType statType)
        {
            float additiveBonus = 0f;
            float multiplicativeBonus = 1f;

            foreach (var perk in equippedPerks)
            {
                foreach (var stat in perk.statModifiers)
                {
                    if (stat.statType == statType)
                    {
                        if (stat.isMultiplicative)
                            multiplicativeBonus *= stat.value;
                        else
                            additiveBonus += stat.value;
                    }
                }
            }

            return (1f + additiveBonus) * multiplicativeBonus;
        }

        #endregion

        #region Equipment Management

        /// <summary>
        /// Equips an equipment item (grenades, med kits, etc.)
        /// </summary>
        public bool EquipEquipment(Items.ItemData item)
        {
            if (item == null)
                return false;

            if (equipment.Count >= maxEquipment)
            {
                Debug.LogWarning($"[LoadoutSystem] Max equipment ({maxEquipment}) already equipped");
                return false;
            }

            equipment.Add(item);
            OnLoadoutChanged?.Invoke();

            return true;
        }

        /// <summary>
        /// Unequips an equipment item
        /// </summary>
        public bool UnequipEquipment(Items.ItemData item)
        {
            if (item == null || !equipment.Contains(item))
                return false;

            equipment.Remove(item);
            OnLoadoutChanged?.Invoke();

            return true;
        }

        /// <summary>
        /// Gets equipped equipment
        /// </summary>
        public List<Items.ItemData> GetEquipment()
        {
            return new List<Items.ItemData>(equipment);
        }

        #endregion

        #region Loadout Presets

        /// <summary>
        /// Saves current loadout as a preset
        /// </summary>
        public bool SaveLoadout(string loadoutName)
        {
            if (savedLoadouts.Count >= maxSavedLoadouts)
            {
                Debug.LogWarning($"[LoadoutSystem] Max saved loadouts ({maxSavedLoadouts}) reached");
                return false;
            }

            SavedLoadout loadout = new SavedLoadout
            {
                name = loadoutName,
                primaryWeapons = new List<Weapons.WeaponData>(primaryWeapons),
                secondaryWeapons = new List<Weapons.WeaponData>(secondaryWeapons),
                perks = new List<PerkData>(equippedPerks),
                equipment = new List<Items.ItemData>(equipment)
            };

            savedLoadouts.Add(loadout);
            SaveLoadouts();

            Debug.Log($"[LoadoutSystem] Saved loadout: {loadoutName}");
            return true;
        }

        /// <summary>
        /// Loads a saved loadout
        /// </summary>
        public bool LoadLoadout(string loadoutName)
        {
            SavedLoadout loadout = savedLoadouts.FirstOrDefault(l => l.name == loadoutName);

            if (loadout == null)
            {
                Debug.LogWarning($"[LoadoutSystem] Loadout '{loadoutName}' not found");
                return false;
            }

            return LoadLoadout(loadout);
        }

        /// <summary>
        /// Loads a saved loadout
        /// </summary>
        public bool LoadLoadout(SavedLoadout loadout)
        {
            if (loadout == null)
                return false;

            // Clear current loadout
            ClearLoadout();

            // Load weapons
            primaryWeapons = new List<Weapons.WeaponData>(loadout.primaryWeapons);
            secondaryWeapons = new List<Weapons.WeaponData>(loadout.secondaryWeapons);

            // Load perks
            foreach (var perk in loadout.perks)
            {
                EquipPerk(perk);
            }

            // Load equipment
            equipment = new List<Items.ItemData>(loadout.equipment);

            ApplyCurrentLoadout();
            OnLoadoutChanged?.Invoke();

            Debug.Log($"[LoadoutSystem] Loaded loadout: {loadout.name}");
            return true;
        }

        /// <summary>
        /// Deletes a saved loadout
        /// </summary>
        public bool DeleteLoadout(string loadoutName)
        {
            SavedLoadout loadout = savedLoadouts.FirstOrDefault(l => l.name == loadoutName);

            if (loadout == null)
                return false;

            savedLoadouts.Remove(loadout);
            SaveLoadouts();

            Debug.Log($"[LoadoutSystem] Deleted loadout: {loadoutName}");
            return true;
        }

        /// <summary>
        /// Gets all saved loadouts
        /// </summary>
        public List<SavedLoadout> GetSavedLoadouts()
        {
            return new List<SavedLoadout>(savedLoadouts);
        }

        /// <summary>
        /// Clears the current loadout
        /// </summary>
        public void ClearLoadout()
        {
            // Remove perk effects
            foreach (var perk in equippedPerks)
            {
                perk.Remove(playerController);
            }

            primaryWeapons.Clear();
            secondaryWeapons.Clear();
            equippedPerks.Clear();
            equipment.Clear();

            OnLoadoutChanged?.Invoke();

            Debug.Log("[LoadoutSystem] Loadout cleared");
        }

        #endregion

        #region Apply Loadout

        /// <summary>
        /// Applies the current loadout to the player
        /// </summary>
        public void ApplyCurrentLoadout()
        {
            if (playerController == null)
                return;

            // Apply perks
            foreach (var perk in equippedPerks)
            {
                perk.Apply(playerController);
            }

            // TODO: Spawn weapons on player
            // TODO: Give equipment items to inventory

            Debug.Log("[LoadoutSystem] Loadout applied");
        }

        #endregion

        #region Save/Load

        private void SaveLoadouts()
        {
            // TODO: Implement proper save system
            // For now, just log
            Debug.Log($"[LoadoutSystem] Saved {savedLoadouts.Count} loadouts");
        }

        private void LoadLoadouts()
        {
            // TODO: Load from save system
            Debug.Log("[LoadoutSystem] Loaded loadouts");
        }

        #endregion
    }

    /// <summary>
    /// Represents a saved loadout preset
    /// </summary>
    [System.Serializable]
    public class SavedLoadout
    {
        public string name;
        public List<Weapons.WeaponData> primaryWeapons;
        public List<Weapons.WeaponData> secondaryWeapons;
        public List<PerkData> perks;
        public List<Items.ItemData> equipment;
    }
}
