using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Netcode;

namespace ZombieGame.UI
{
    public class InventoryUI : MonoBehaviour
    {
        public static InventoryUI Instance { get; private set; }

        [SerializeField] private GameObject inventoryPanel;
        [SerializeField] private Transform inventoryGrid;
        [SerializeField] private GameObject inventorySlotPrefab;

        private List<GameObject> inventorySlots = new List<GameObject>();
        private bool isOpen = false;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.I) || Input.GetKeyDown(KeyCode.Tab))
            {
                ToggleInventory();
            }
        }

        public void ToggleInventory()
        {
            isOpen = !isOpen;
            if (inventoryPanel != null)
            {
                inventoryPanel.SetActive(isOpen);
            }

            if (isOpen)
            {
                RefreshInventory();
            }

            Cursor.lockState = isOpen ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = isOpen;
        }

        private void RefreshInventory()
        {
            ClearInventory();

            var inventory = Inventory.InventorySystem.Instance;
            if (inventory != null)
            {
                ulong playerId = NetworkManager.Singleton.LocalClientId;
                // Would populate inventory slots from InventorySystem
            }
        }

        private void ClearInventory()
        {
            foreach (var slot in inventorySlots)
            {
                Destroy(slot);
            }
            inventorySlots.Clear();
        }
    }
}
