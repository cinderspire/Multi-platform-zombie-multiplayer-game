using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

namespace ZombieGame.UI
{
    public class MiniMapSystem : MonoBehaviour
    {
        public static MiniMapSystem Instance { get; private set; }

        [Header("MiniMap Settings")]
        [SerializeField] private RawImage miniMapImage;
        [SerializeField] private Camera miniMapCamera;
        [SerializeField] private float zoomLevel = 50f;
        [SerializeField] private bool rotateWithPlayer = true;

        [Header("Icons")]
        [SerializeField] private GameObject playerIconPrefab;
        [SerializeField] private GameObject enemyIconPrefab;
        [SerializeField] private GameObject objectiveIconPrefab;

        private Transform localPlayerTransform;
        private Dictionary<ulong, GameObject> playerIcons = new Dictionary<ulong, GameObject>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (miniMapCamera != null)
            {
                miniMapCamera.orthographicSize = zoomLevel;
            }

            var localPlayer = NetworkManager.Singleton.LocalClient?.PlayerObject;
            if (localPlayer != null)
            {
                localPlayerTransform = localPlayer.transform;
            }
        }

        private void LateUpdate()
        {
            if (localPlayerTransform == null || miniMapCamera == null) return;

            // Update camera position
            Vector3 newPos = localPlayerTransform.position;
            newPos.y = miniMapCamera.transform.position.y;
            miniMapCamera.transform.position = newPos;

            // Rotate with player
            if (rotateWithPlayer)
            {
                miniMapCamera.transform.rotation = Quaternion.Euler(90f, localPlayerTransform.eulerAngles.y, 0f);
            }
        }

        public void SetZoom(float zoom)
        {
            zoomLevel = Mathf.Clamp(zoom, 20f, 100f);
            if (miniMapCamera != null)
            {
                miniMapCamera.orthographicSize = zoomLevel;
            }
        }

        public void ToggleRotation()
        {
            rotateWithPlayer = !rotateWithPlayer;
        }
    }
}
