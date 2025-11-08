using UnityEngine;

namespace DeadFrontier.Items
{
    /// <summary>
    /// Represents an item in the world that can be picked up
    /// </summary>
    public class WorldItem : MonoBehaviour
    {
        [Header("Item Data")]
        [SerializeField] private ItemData itemData;
        [SerializeField] private int quantity = 1;

        [Header("Visual")]
        [SerializeField] private GameObject visualModel;
        [SerializeField] private bool rotateItem = true;
        [SerializeField] private float rotationSpeed = 50f;
        [SerializeField] private bool bobItem = true;
        [SerializeField] private float bobSpeed = 2f;
        [SerializeField] private float bobHeight = 0.3f;

        [Header("Interaction")]
        [SerializeField] private float pickupRadius = 2f;
        [SerializeField] private LayerMask playerLayer;
        [SerializeField] private bool autoPickup = false;

        [Header("Highlight")]
        [SerializeField] private GameObject highlightEffect;

        private Vector3 startPosition;
        private float bobOffset;
        private bool isHighlighted = false;

        // Properties
        public ItemData ItemData => itemData;
        public int Quantity => quantity;

        private void Start()
        {
            startPosition = transform.position;
            bobOffset = Random.Range(0f, Mathf.PI * 2f); // Random start offset for bob

            // Disable highlight initially
            if (highlightEffect != null)
                highlightEffect.SetActive(false);

            // Set rarity-based color/glow if available
            ApplyRarityVisuals();
        }

        private void Update()
        {
            // Visual effects
            if (rotateItem && visualModel != null)
            {
                visualModel.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
            }

            if (bobItem)
            {
                float newY = startPosition.y + Mathf.Sin((Time.time + bobOffset) * bobSpeed) * bobHeight;
                transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            }

            // Check for nearby players
            CheckForNearbyPlayers();
        }

        private void CheckForNearbyPlayers()
        {
            Collider[] players = Physics.OverlapSphere(transform.position, pickupRadius, playerLayer);

            if (players.Length > 0)
            {
                // Highlight the item
                if (!isHighlighted)
                {
                    ShowHighlight(true);
                }

                // Auto pickup if enabled
                if (autoPickup)
                {
                    TryPickup(players[0].GetComponent<InventorySystem>());
                }
            }
            else
            {
                // Remove highlight
                if (isHighlighted)
                {
                    ShowHighlight(false);
                }
            }
        }

        /// <summary>
        /// Attempts to pick up the item
        /// </summary>
        public bool TryPickup(InventorySystem inventory)
        {
            if (inventory == null || itemData == null)
                return false;

            bool success = inventory.AddItem(itemData, quantity);

            if (success)
            {
                OnPickedUp(inventory);
                return true;
            }
            else
            {
                Debug.Log("[WorldItem] Inventory full!");
                // Show "Inventory Full" message
                return false;
            }
        }

        private void OnPickedUp(InventorySystem inventory)
        {
            Debug.Log($"[WorldItem] Picked up {quantity}x {itemData.itemName}");

            // Play pickup sound
            if (itemData.useSound != null)
            {
                Core.AudioManager.Instance.Play(itemData.useSound, transform.position);
            }

            // Spawn pickup effect
            SpawnPickupEffect();

            // Destroy the world item
            Destroy(gameObject);
        }

        private void ShowHighlight(bool show)
        {
            isHighlighted = show;

            if (highlightEffect != null)
            {
                highlightEffect.SetActive(show);
            }

            // Could also enable outline shader here
        }

        private void ApplyRarityVisuals()
        {
            if (itemData == null)
                return;

            // Apply glow/particles based on rarity
            Color rarityColor = itemData.GetRarityColor();

            // If there's a light component, set its color
            Light itemLight = GetComponentInChildren<Light>();
            if (itemLight != null)
            {
                itemLight.color = rarityColor;
            }

            // If there's a particle system, set its color
            ParticleSystem particles = GetComponentInChildren<ParticleSystem>();
            if (particles != null)
            {
                var main = particles.main;
                main.startColor = rarityColor;
            }
        }

        private void SpawnPickupEffect()
        {
            // TODO: Spawn particle effect
            // GameObject effect = PoolManager.Instance.Get(pickupEffectPrefab, transform.position, Quaternion.identity);
            // PoolManager.Instance.Return(effect, 2f);
        }

        /// <summary>
        /// Initializes the world item with data
        /// </summary>
        public void Initialize(ItemData data, int amount, Vector3 position)
        {
            itemData = data;
            quantity = amount;
            transform.position = position;
            startPosition = position;

            // Set visual model if needed
            if (visualModel == null && data.worldModelPrefab != null)
            {
                visualModel = Instantiate(data.worldModelPrefab, transform);
            }

            ApplyRarityVisuals();
        }

        /// <summary>
        /// Spawns a world item at a position
        /// </summary>
        public static WorldItem Spawn(ItemData itemData, int quantity, Vector3 position, GameObject worldItemPrefab = null)
        {
            GameObject obj;

            if (worldItemPrefab != null)
            {
                obj = Instantiate(worldItemPrefab, position, Quaternion.identity);
            }
            else
            {
                // Create basic world item
                obj = new GameObject($"WorldItem_{itemData.itemName}");
                obj.transform.position = position;
                obj.AddComponent<WorldItem>();
                obj.AddComponent<SphereCollider>().isTrigger = true;
            }

            WorldItem worldItem = obj.GetComponent<WorldItem>();
            if (worldItem != null)
            {
                worldItem.Initialize(itemData, quantity, position);
            }

            return worldItem;
        }

        private void OnDrawGizmosSelected()
        {
            // Draw pickup radius
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, pickupRadius);
        }
    }
}
