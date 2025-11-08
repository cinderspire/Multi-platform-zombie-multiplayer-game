using UnityEngine;
using Unity.Netcode;

namespace DeadFrontier.Gameplay
{
    /// <summary>
    /// World loot item that players can pick up during raids.
    /// </summary>
    public class LootItem : NetworkBehaviour
    {
        [Header("Loot Settings")]
        [SerializeField] private LootItemData itemData;
        [SerializeField] private int quantity = 1;

        [Header("Interaction")]
        [SerializeField] private float interactionRange = 3f;
        [SerializeField] private KeyCode pickupKey = KeyCode.E;

        [Header("Visuals")]
        [SerializeField] private GameObject visualModel;
        [SerializeField] private ParticleSystem glowEffect;
        [SerializeField] private Light itemLight;

        [Header("Physics")]
        [SerializeField] private bool usePhysics = true;
        [SerializeField] private float despawnTime = 300f; // 5 minutes

        private NetworkVariable<int> networkQuantity = new NetworkVariable<int>(1);
        private float spawnTime;
        private bool canInteract = true;

        public LootItemData ItemData => itemData;
        public int Quantity => networkQuantity.Value;

        private void Start()
        {
            spawnTime = Time.time;

            if (IsServer)
            {
                networkQuantity.Value = quantity;
            }

            SetupVisuals();
        }

        private void Update()
        {
            if (IsServer)
            {
                // Despawn after timeout
                if (Time.time - spawnTime > despawnTime)
                {
                    GetComponent<NetworkObject>()?.Despawn();
                    Destroy(gameObject);
                }
            }

            if (!IsOwner && !IsServer) return;

            CheckPlayerInteraction();
        }

        public void Initialize(LootItemData data, int qty = 1)
        {
            itemData = data;
            quantity = qty;

            if (IsServer)
            {
                networkQuantity.Value = qty;
            }

            SetupVisuals();
        }

        private void SetupVisuals()
        {
            if (itemData == null) return;

            // Set rarity color
            Color rarityColor = GetRarityColor(itemData.rarity);

            if (glowEffect != null)
            {
                var main = glowEffect.main;
                main.startColor = rarityColor;
                glowEffect.Play();
            }

            if (itemLight != null)
            {
                itemLight.color = rarityColor;
            }

            // Spawn world model if specified
            if (itemData.worldModel != null && visualModel == null)
            {
                visualModel = Instantiate(itemData.worldModel, transform);
            }
        }

        private void CheckPlayerInteraction()
        {
            if (!canInteract) return;

            // Find nearby players
            var nearbyPlayers = Physics.OverlapSphere(transform.position, interactionRange, LayerMask.GetMask("Player"));

            foreach (var col in nearbyPlayers)
            {
                var networkObject = col.GetComponent<NetworkObject>();
                if (networkObject != null && networkObject.IsLocalPlayer)
                {
                    // Show pickup prompt
                    ShowPickupPrompt(true);

                    if (Input.GetKeyDown(pickupKey))
                    {
                        RequestPickupServerRpc(networkObject.OwnerClientId);
                    }

                    return;
                }
            }

            ShowPickupPrompt(false);
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestPickupServerRpc(ulong playerId)
        {
            if (!canInteract) return;

            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.PickupWorldLoot(playerId, this);
            }
        }

        private void ShowPickupPrompt(bool show)
        {
            if (show)
            {
                // Show UI prompt: "Press E to pick up [ItemName] x[Quantity]"
                string promptText = $"Press {pickupKey} to pick up {itemData.itemName}";
                if (quantity > 1)
                    promptText += $" x{quantity}";

                // Display via UI system
                // TODO: Implement prompt UI
            }
            else
            {
                // Hide prompt
            }
        }

        private Color GetRarityColor(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Common: return Color.white;
                case ItemRarity.Uncommon: return Color.green;
                case ItemRarity.Rare: return Color.blue;
                case ItemRarity.Epic: return new Color(0.6f, 0f, 1f);
                case ItemRarity.Legendary: return Color.yellow;
                default: return Color.white;
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactionRange);
        }
    }
}
