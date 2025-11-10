using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Interaction
{
    public class InteractionSystem : NetworkBehaviour
    {
        public static InteractionSystem Instance { get; private set; }

        [SerializeField] private float interactionRange = 3f;
        [SerializeField] private LayerMask interactableMask;
        [SerializeField] private KeyCode interactKey = KeyCode.E;

        private Dictionary<ulong, IInteractable> currentInteractables = new Dictionary<ulong, IInteractable>();

        public event Action<ulong, string> OnInteractionStarted;
        public event Action<ulong, string> OnInteractionCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Update()
        {
            if (Input.GetKeyDown(interactKey))
            {
                TryInteract();
            }

            CheckForInteractables();
        }

        private void CheckForInteractables()
        {
            ulong localPlayerId = NetworkManager.Singleton.LocalClientId;
            var playerObj = NetworkManager.Singleton.LocalClient?.PlayerObject;
            if (playerObj == null) return;

            Vector3 origin = playerObj.transform.position + Vector3.up;
            Vector3 direction = playerObj.transform.forward;

            if (Physics.Raycast(origin, direction, out RaycastHit hit, interactionRange, interactableMask))
            {
                if (hit.collider.TryGetComponent<IInteractable>(out var interactable))
                {
                    currentInteractables[localPlayerId] = interactable;
                    // Show interaction prompt
                    return;
                }
            }

            currentInteractables.Remove(localPlayerId);
        }

        private void TryInteract()
        {
            ulong localPlayerId = NetworkManager.Singleton.LocalClientId;
            if (currentInteractables.TryGetValue(localPlayerId, out var interactable))
            {
                InteractServerRpc(localPlayerId, interactable.GetInteractableId());
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void InteractServerRpc(ulong playerId, string interactableId, ServerRpcParams rpcParams = default)
        {
            OnInteractionStarted?.Invoke(playerId, interactableId);
            
            // Process interaction based on type
            // Could open doors, pick up items, activate switches, etc.
            
            OnInteractionCompleted?.Invoke(playerId, interactableId);
            InteractClientRpc(playerId, interactableId);
        }

        [ClientRpc]
        private void InteractClientRpc(ulong playerId, string interactableId) { }
    }

    public interface IInteractable
    {
        string GetInteractableId();
        string GetInteractionPrompt();
        bool CanInteract(ulong playerId);
        void OnInteract(ulong playerId);
    }

    public class Door : NetworkBehaviour, IInteractable
    {
        [SerializeField] private string doorId;
        [SerializeField] private bool isLocked;
        private bool isOpen;

        public string GetInteractableId() => doorId;
        public string GetInteractionPrompt() => isOpen ? "Close Door" : "Open Door";
        public bool CanInteract(ulong playerId) => !isLocked;

        public void OnInteract(ulong playerId)
        {
            isOpen = !isOpen;
            // Animate door
        }
    }
}
