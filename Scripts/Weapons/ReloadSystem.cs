using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Weapons
{
    public class ReloadSystem : NetworkBehaviour
    {
        public static ReloadSystem Instance { get; private set; }

        private Dictionary<ulong, WeaponState> playerWeaponStates = new Dictionary<ulong, WeaponState>();
        private Dictionary<ulong, Coroutine> reloadCoroutines = new Dictionary<ulong, Coroutine>();

        public event Action<ulong, string> OnReloadStarted;
        public event Action<ulong, string> OnReloadCompleted;
        public event Action<ulong, int, int> OnAmmoChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        [ServerRpc(RequireOwnership = false)]
        public void StartReloadServerRpc(ulong playerId, string weaponId, float reloadTime, ServerRpcParams rpcParams = default)
        {
            if (IsReloading(playerId)) return;

            if (!playerWeaponStates.TryGetValue(playerId, out var state))
            {
                state = new WeaponState { playerId = playerId, weaponId = weaponId };
                playerWeaponStates[playerId] = state;
            }

            if (state.reserveAmmo <= 0) return;

            OnReloadStarted?.Invoke(playerId, weaponId);
            StartReloadClientRpc(playerId, weaponId, reloadTime);

            if (reloadCoroutines.ContainsKey(playerId))
            {
                StopCoroutine(reloadCoroutines[playerId]);
            }

            reloadCoroutines[playerId] = StartCoroutine(ReloadCoroutine(playerId, weaponId, reloadTime));
        }

        private IEnumerator ReloadCoroutine(ulong playerId, string weaponId, float reloadTime)
        {
            yield return new WaitForSeconds(reloadTime);

            if (playerWeaponStates.TryGetValue(playerId, out var state))
            {
                int ammoNeeded = state.magazineSize - state.currentAmmo;
                int ammoToReload = Mathf.Min(ammoNeeded, state.reserveAmmo);

                state.currentAmmo += ammoToReload;
                state.reserveAmmo -= ammoToReload;
                state.isReloading = false;

                OnReloadCompleted?.Invoke(playerId, weaponId);
                OnAmmoChanged?.Invoke(playerId, state.currentAmmo, state.reserveAmmo);
                CompleteReloadClientRpc(playerId, state.currentAmmo, state.reserveAmmo);
            }

            reloadCoroutines.Remove(playerId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void CancelReloadServerRpc(ulong playerId, ServerRpcParams rpcParams = default)
        {
            if (reloadCoroutines.TryGetValue(playerId, out var coroutine))
            {
                StopCoroutine(coroutine);
                reloadCoroutines.Remove(playerId);

                if (playerWeaponStates.TryGetValue(playerId, out var state))
                {
                    state.isReloading = false;
                }

                CancelReloadClientRpc(playerId);
            }
        }

        [ClientRpc]
        private void StartReloadClientRpc(ulong playerId, string weaponId, float reloadTime) { }

        [ClientRpc]
        private void CompleteReloadClientRpc(ulong playerId, int currentAmmo, int reserveAmmo) { }

        [ClientRpc]
        private void CancelReloadClientRpc(ulong playerId) { }

        public bool IsReloading(ulong playerId)
        {
            return playerWeaponStates.TryGetValue(playerId, out var state) && state.isReloading;
        }

        public WeaponState GetWeaponState(ulong playerId) => playerWeaponStates.GetValueOrDefault(playerId);
    }

    [Serializable]
    public class WeaponState
    {
        public ulong playerId;
        public string weaponId;
        public int currentAmmo;
        public int reserveAmmo;
        public int magazineSize;
        public bool isReloading;
    }
}
