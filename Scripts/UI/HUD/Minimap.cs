using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace DeadFrontier.UI.HUD
{
    /// <summary>
    /// Minimap system with icons for players, zombies, and objectives
    /// </summary>
    public class Minimap : MonoBehaviour
    {
        [Header("Camera")]
        [SerializeField] private Camera minimapCamera;
        [SerializeField] private RawImage minimapImage;
        [SerializeField] private float cameraHeight = 50f;
        [SerializeField] private float zoomLevel = 30f;
        [SerializeField] private bool rotateWithPlayer = true;

        [Header("Icons")]
        [SerializeField] private RectTransform iconContainer;
        [SerializeField] private GameObject playerIconPrefab;
        [SerializeField] private GameObject zombieIconPrefab;
        [SerializeField] private GameObject extractionIconPrefab;
        [SerializeField] private GameObject lootIconPrefab;

        [Header("Settings")]
        [SerializeField] private float updateInterval = 0.1f; // Update icons every 0.1s
        [SerializeField] private float iconScale = 1f;
        [SerializeField] private float maxIconDistance = 100f;
        [SerializeField] private bool showZombies = true;
        [SerializeField] private bool showPlayers = true;
        [SerializeField] private bool showExtractions = true;
        [SerializeField] private bool showLoot = false;

        [Header("Colors")]
        [SerializeField] private Color playerColor = Color.blue;
        [SerializeField] private Color teammateColor = Color.green;
        [SerializeField] private Color zombieColor = Color.red;
        [SerializeField] private Color extractionColor = Color.yellow;

        // State
        private Transform playerTransform;
        private Dictionary<Transform, GameObject> activeIcons = new Dictionary<Transform, GameObject>();
        private float updateTimer;

        // Icon pools
        private Queue<GameObject> playerIconPool = new Queue<GameObject>();
        private Queue<GameObject> zombieIconPool = new Queue<GameObject>();
        private Queue<GameObject> extractionIconPool = new Queue<GameObject>();
        private Queue<GameObject> lootIconPool = new Queue<GameObject>();

        private void Start()
        {
            // Find local player
            FindLocalPlayer();

            // Setup minimap camera
            if (minimapCamera != null)
            {
                minimapCamera.orthographic = true;
                minimapCamera.orthographicSize = zoomLevel;
                minimapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            }

            // Prewarm icon pools
            PrewarmIconPools();
        }

        private void Update()
        {
            if (playerTransform == null)
            {
                FindLocalPlayer();
                return;
            }

            // Update minimap camera position
            UpdateCameraPosition();

            // Update icons periodically
            updateTimer += Time.deltaTime;
            if (updateTimer >= updateInterval)
            {
                UpdateIcons();
                updateTimer = 0f;
            }
        }

        private void LateUpdate()
        {
            // Update icon positions based on world positions
            UpdateIconPositions();
        }

        private void FindLocalPlayer()
        {
            // Find the local player's transform
            var localPlayer = FindObjectOfType<Player.PlayerController>();
            if (localPlayer != null)
            {
                playerTransform = localPlayer.transform;
            }
        }

        private void UpdateCameraPosition()
        {
            if (minimapCamera == null || playerTransform == null)
                return;

            // Position camera above player
            Vector3 cameraPos = playerTransform.position;
            cameraPos.y += cameraHeight;
            minimapCamera.transform.position = cameraPos;

            // Rotate with player if enabled
            if (rotateWithPlayer)
            {
                float playerRotation = playerTransform.eulerAngles.y;
                minimapCamera.transform.rotation = Quaternion.Euler(90f, playerRotation, 0f);
            }
        }

        private void UpdateIcons()
        {
            // Clear old icons
            ClearInactiveIcons();

            // Update player icons
            if (showPlayers)
                UpdatePlayerIcons();

            // Update zombie icons
            if (showZombies)
                UpdateZombieIcons();

            // Update extraction icons
            if (showExtractions)
                UpdateExtractionIcons();

            // Update loot icons
            if (showLoot)
                UpdateLootIcons();
        }

        private void UpdatePlayerIcons()
        {
            // Find all networked players
            var players = FindObjectsOfType<Networking.NetworkedPlayer>();

            foreach (var player in players)
            {
                if (player.transform == playerTransform)
                    continue; // Skip local player

                if (!IsWithinRange(player.transform.position))
                    continue;

                // Create or update icon
                CreateOrUpdateIcon(player.transform, playerIconPrefab, playerIconPool, playerColor);
            }
        }

        private void UpdateZombieIcons()
        {
            // Get zombies from ZombieManager
            if (Zombies.ZombieManager.Instance == null)
                return;

            var zombies = Zombies.ZombieManager.Instance.GetZombiesInRange(
                playerTransform.position,
                maxIconDistance
            );

            foreach (var zombie in zombies)
            {
                if (zombie == null)
                    continue;

                // Create or update icon
                CreateOrUpdateIcon(zombie.transform, zombieIconPrefab, zombieIconPool, zombieColor);
            }
        }

        private void UpdateExtractionIcons()
        {
            // Find all extraction points
            var extractions = FindObjectsOfType<Gameplay.ExtractionPoint>();

            foreach (var extraction in extractions)
            {
                if (!extraction.IsActive)
                    continue;

                // Create or update icon
                CreateOrUpdateIcon(extraction.transform, extractionIconPrefab, extractionIconPool, extractionColor);
            }
        }

        private void UpdateLootIcons()
        {
            // Find nearby loot items
            var lootItems = FindObjectsOfType<Items.WorldItem>();

            foreach (var loot in lootItems)
            {
                if (!IsWithinRange(loot.transform.position))
                    continue;

                // Only show rare+ loot on minimap
                if (loot.ItemData != null && (int)loot.ItemData.rarity >= (int)Items.ItemRarity.Rare)
                {
                    CreateOrUpdateIcon(loot.transform, lootIconPrefab, lootIconPool, loot.ItemData.GetRarityColor());
                }
            }
        }

        private void CreateOrUpdateIcon(Transform target, GameObject prefab, Queue<GameObject> pool, Color color)
        {
            if (!activeIcons.ContainsKey(target))
            {
                // Create new icon
                GameObject icon = GetIconFromPool(prefab, pool);
                icon.transform.SetParent(iconContainer);
                icon.transform.localScale = Vector3.one * iconScale;

                // Set color
                Image iconImage = icon.GetComponent<Image>();
                if (iconImage != null)
                {
                    iconImage.color = color;
                }

                activeIcons[target] = icon;
            }

            // Icon position will be updated in UpdateIconPositions()
        }

        private void UpdateIconPositions()
        {
            if (playerTransform == null)
                return;

            foreach (var kvp in activeIcons)
            {
                Transform target = kvp.Key;
                GameObject icon = kvp.Value;

                if (target == null || icon == null)
                    continue;

                // Convert world position to minimap position
                Vector3 localPos = target.position - playerTransform.position;

                // Rotate based on player rotation if minimap rotates
                if (rotateWithPlayer)
                {
                    float angle = -playerTransform.eulerAngles.y * Mathf.Deg2Rad;
                    float rotatedX = localPos.x * Mathf.Cos(angle) - localPos.z * Mathf.Sin(angle);
                    float rotatedZ = localPos.x * Mathf.Sin(angle) + localPos.z * Mathf.Cos(angle);
                    localPos = new Vector3(rotatedX, 0f, rotatedZ);
                }

                // Scale to minimap size
                float minimapSize = minimapImage.rectTransform.rect.width;
                float scale = minimapSize / (zoomLevel * 2f);

                Vector2 iconPos = new Vector2(localPos.x * scale, localPos.z * scale);

                // Clamp to minimap bounds
                float maxDist = minimapSize * 0.45f;
                if (iconPos.magnitude > maxDist)
                {
                    iconPos = iconPos.normalized * maxDist;
                }

                icon.GetComponent<RectTransform>().anchoredPosition = iconPos;
            }
        }

        private void ClearInactiveIcons()
        {
            List<Transform> toRemove = new List<Transform>();

            foreach (var kvp in activeIcons)
            {
                Transform target = kvp.Key;
                GameObject icon = kvp.Value;

                // Check if target still exists and is in range
                if (target == null || !IsWithinRange(target.position))
                {
                    toRemove.Add(target);
                    ReturnIconToPool(icon);
                }
            }

            foreach (var target in toRemove)
            {
                activeIcons.Remove(target);
            }
        }

        private bool IsWithinRange(Vector3 position)
        {
            if (playerTransform == null)
                return false;

            return Vector3.Distance(playerTransform.position, position) <= maxIconDistance;
        }

        #region Icon Pooling

        private void PrewarmIconPools()
        {
            PrewarmPool(playerIconPrefab, playerIconPool, 10);
            PrewarmPool(zombieIconPrefab, zombieIconPool, 50);
            PrewarmPool(extractionIconPrefab, extractionIconPool, 5);
            PrewarmPool(lootIconPrefab, lootIconPool, 20);
        }

        private void PrewarmPool(GameObject prefab, Queue<GameObject> pool, int count)
        {
            if (prefab == null)
                return;

            for (int i = 0; i < count; i++)
            {
                GameObject obj = Instantiate(prefab);
                obj.SetActive(false);
                pool.Enqueue(obj);
            }
        }

        private GameObject GetIconFromPool(GameObject prefab, Queue<GameObject> pool)
        {
            if (prefab == null)
                return null;

            GameObject icon;

            if (pool.Count > 0)
            {
                icon = pool.Dequeue();
            }
            else
            {
                icon = Instantiate(prefab);
            }

            icon.SetActive(true);
            return icon;
        }

        private void ReturnIconToPool(GameObject icon)
        {
            if (icon == null)
                return;

            icon.SetActive(false);
            icon.transform.SetParent(null);

            // Return to appropriate pool based on prefab
            // For simplicity, just disable it
            Destroy(icon);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the zoom level of the minimap
        /// </summary>
        public void SetZoom(float zoom)
        {
            zoomLevel = Mathf.Clamp(zoom, 10f, 100f);

            if (minimapCamera != null)
            {
                minimapCamera.orthographicSize = zoomLevel;
            }
        }

        /// <summary>
        /// Toggles minimap rotation
        /// </summary>
        public void SetRotation(bool rotate)
        {
            rotateWithPlayer = rotate;
        }

        /// <summary>
        /// Toggles showing zombie icons
        /// </summary>
        public void SetShowZombies(bool show)
        {
            showZombies = show;
        }

        /// <summary>
        /// Pings a location on the minimap
        /// </summary>
        public void PingLocation(Vector3 worldPosition, float duration = 3f)
        {
            // TODO: Create ping effect on minimap
            Debug.Log($"[Minimap] Pinged location: {worldPosition}");
        }

        #endregion
    }
}
