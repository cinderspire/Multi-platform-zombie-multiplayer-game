using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Weapons
{
    public class WeaponSwitchingSystem : NetworkBehaviour
    {
        public static WeaponSwitchingSystem Instance { get; private set; }

        [SerializeField] private int weaponSlots = 3;
        [SerializeField] private float switchTime = 0.5f;

        private Dictionary<ulong, PlayerWeaponLoadout> playerLoadouts = new Dictionary<ulong, PlayerWeaponLoadout>();

        public event Action<ulong, int, string> OnWeaponSwitched;
        public event Action<ulong, string, AttachmentType, string> OnAttachmentEquipped;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        [ServerRpc(RequireOwnership = false)]
        public void SwitchWeaponServerRpc(ulong playerId, int slotIndex, ServerRpcParams rpcParams = default)
        {
            if (!playerLoadouts.TryGetValue(playerId, out var loadout)) return;
            if (slotIndex < 0 || slotIndex >= loadout.weapons.Count) return;
            if (loadout.isSwitching) return;

            loadout.isSwitching = true;
            loadout.currentWeaponIndex = slotIndex;
            
            OnWeaponSwitched?.Invoke(playerId, slotIndex, loadout.weapons[slotIndex].weaponId);
            SwitchWeaponClientRpc(playerId, slotIndex);

            StartCoroutine(CompleteSwitchAfterDelay(playerId));
        }

        private System.Collections.IEnumerator CompleteSwitchAfterDelay(ulong playerId)
        {
            yield return new WaitForSeconds(switchTime);
            if (playerLoadouts.TryGetValue(playerId, out var loadout))
            {
                loadout.isSwitching = false;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void EquipAttachmentServerRpc(ulong playerId, int weaponSlot, AttachmentType type, string attachmentId, ServerRpcParams rpcParams = default)
        {
            if (!playerLoadouts.TryGetValue(playerId, out var loadout)) return;
            if (weaponSlot >= loadout.weapons.Count) return;

            var weapon = loadout.weapons[weaponSlot];
            weapon.attachments[type] = attachmentId;

            OnAttachmentEquipped?.Invoke(playerId, weapon.weaponId, type, attachmentId);
            EquipAttachmentClientRpc(playerId, weaponSlot, type, attachmentId);
        }

        [ClientRpc]
        private void SwitchWeaponClientRpc(ulong playerId, int slotIndex) { }

        [ClientRpc]
        private void EquipAttachmentClientRpc(ulong playerId, int weaponSlot, AttachmentType type, string attachmentId) { }

        public PlayerWeaponLoadout GetPlayerLoadout(ulong playerId) => playerLoadouts.GetValueOrDefault(playerId);
    }

    [Serializable]
    public class PlayerWeaponLoadout
    {
        public ulong playerId;
        public List<WeaponSlot> weapons = new List<WeaponSlot>();
        public int currentWeaponIndex;
        public bool isSwitching;
    }

    [Serializable]
    public class WeaponSlot
    {
        public string weaponId;
        public Dictionary<AttachmentType, string> attachments = new Dictionary<AttachmentType, string>();
        public int currentAmmo;
        public int reserveAmmo;
    }

    public enum AttachmentType { Scope, Barrel, Magazine, Grip, Stock, Laser, Muzzle, Underbarrel }
}
