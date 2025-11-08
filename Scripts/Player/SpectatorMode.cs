using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

namespace DeadFrontier.Player
{
    /// <summary>
    /// Spectator mode for eliminated players
    /// Allows spectating teammates with smooth camera transitions
    /// </summary>
    public class SpectatorMode : NetworkBehaviour
    {
        [Header("Camera Settings")]
        [SerializeField] private float cameraSmoothSpeed = 5f;
        [SerializeField] private Vector3 cameraOffset = new Vector3(0, 2f, -5f);
        [SerializeField] private float cameraHeight = 1.6f;

        [Header("Controls")]
        [SerializeField] private KeyCode nextPlayerKey = KeyCode.E;
        [SerializeField] private KeyCode previousPlayerKey = KeyCode.Q;
        [SerializeField] private KeyCode freeCamKey = KeyCode.F;

        [Header("Free Camera")]
        [SerializeField] private float freeCamMoveSpeed = 10f;
        [SerializeField] private float freeCamFastSpeed = 20f;
        [SerializeField] private float freeCamLookSpeed = 3f;

        [Header("UI")]
        [SerializeField] private GameObject spectatorUI;
        [SerializeField] private TMPro.TextMeshProUGUI spectatingText;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        // Spectator state
        private bool isSpectating = false;
        private SpectatorMode spectatorMode = SpectatorMode.FollowPlayer;
        private int currentSpectatingIndex = 0;
        private List<PlayerController> alivePlayers = new List<PlayerController>();

        // Camera
        private Camera spectatorCamera;
        private Transform cameraTransform;
        private Vector3 freeCamVelocity;

        // Input
        private Vector2 lookInput;

        private void Update()
        {
            if (!isSpectating)
                return;

            HandleInput();
            UpdateCamera();
            UpdateUI();
        }

        #region Spectator Control

        /// <summary>
        /// Enters spectator mode
        /// </summary>
        public void EnterSpectatorMode()
        {
            if (isSpectating)
                return;

            isSpectating = true;

            // Setup camera
            SetupSpectatorCamera();

            // Find alive players
            RefreshAlivePlayers();

            // Start spectating first player
            if (alivePlayers.Count > 0)
            {
                currentSpectatingIndex = 0;
                spectatorMode = SpectatorMode.FollowPlayer;
            }
            else
            {
                // No alive players - use free cam
                spectatorMode = SpectatorMode.FreeCam;
            }

            // Show UI
            if (spectatorUI != null)
                spectatorUI.SetActive(true);

            if (showDebugLogs)
                Debug.Log("[SpectatorMode] Entered spectator mode");

            // Track analytics
            Core.Analytics.AnalyticsManager.Instance?.TrackEvent("spectator_mode_entered", null);
        }

        /// <summary>
        /// Exits spectator mode
        /// </summary>
        public void ExitSpectatorMode()
        {
            if (!isSpectating)
                return;

            isSpectating = false;

            // Cleanup camera
            if (spectatorCamera != null && spectatorCamera != Camera.main)
            {
                Destroy(spectatorCamera.gameObject);
            }

            // Hide UI
            if (spectatorUI != null)
                spectatorUI.SetActive(false);

            if (showDebugLogs)
                Debug.Log("[SpectatorMode] Exited spectator mode");
        }

        private void SetupSpectatorCamera()
        {
            // Use existing camera or create new one
            spectatorCamera = Camera.main;
            if (spectatorCamera == null)
            {
                GameObject camObj = new GameObject("SpectatorCamera");
                spectatorCamera = camObj.AddComponent<Camera>();
                spectatorCamera.tag = "MainCamera";
            }

            cameraTransform = spectatorCamera.transform;
        }

        #endregion

        #region Player Switching

        private void RefreshAlivePlayers()
        {
            alivePlayers.Clear();

            // Find all alive players
            PlayerController[] allPlayers = FindObjectsOfType<PlayerController>();

            foreach (var player in allPlayers)
            {
                // Skip self and dead players
                if (player == GetComponent<PlayerController>())
                    continue;

                var health = player.GetComponent<PlayerHealth>();
                if (health != null && !health.IsDead)
                {
                    alivePlayers.Add(player);
                }
            }

            if (showDebugLogs)
                Debug.Log($"[SpectatorMode] Found {alivePlayers.Count} alive players to spectate");
        }

        private void SpectateNextPlayer()
        {
            if (alivePlayers.Count == 0)
            {
                RefreshAlivePlayers();
                if (alivePlayers.Count == 0)
                {
                    SwitchToFreeCam();
                    return;
                }
            }

            currentSpectatingIndex = (currentSpectatingIndex + 1) % alivePlayers.Count;
            spectatorMode = SpectatorMode.FollowPlayer;

            if (showDebugLogs)
                Debug.Log($"[SpectatorMode] Spectating player {currentSpectatingIndex + 1}/{alivePlayers.Count}");
        }

        private void SpectatePreviousPlayer()
        {
            if (alivePlayers.Count == 0)
            {
                RefreshAlivePlayers();
                if (alivePlayers.Count == 0)
                {
                    SwitchToFreeCam();
                    return;
                }
            }

            currentSpectatingIndex--;
            if (currentSpectatingIndex < 0)
                currentSpectatingIndex = alivePlayers.Count - 1;

            spectatorMode = SpectatorMode.FollowPlayer;

            if (showDebugLogs)
                Debug.Log($"[SpectatorMode] Spectating player {currentSpectatingIndex + 1}/{alivePlayers.Count}");
        }

        private void SwitchToFreeCam()
        {
            spectatorMode = SpectatorMode.FreeCam;

            if (showDebugLogs)
                Debug.Log("[SpectatorMode] Switched to free camera");
        }

        #endregion

        #region Input Handling

        private void HandleInput()
        {
            // Switch players
            if (Input.GetKeyDown(nextPlayerKey))
            {
                SpectateNextPlayer();
            }

            if (Input.GetKeyDown(previousPlayerKey))
            {
                SpectatePreviousPlayer();
            }

            // Toggle free cam
            if (Input.GetKeyDown(freeCamKey))
            {
                if (spectatorMode == SpectatorMode.FreeCam)
                {
                    if (alivePlayers.Count > 0)
                    {
                        spectatorMode = SpectatorMode.FollowPlayer;
                    }
                }
                else
                {
                    SwitchToFreeCam();
                }
            }

            // Get look input
            if (Core.Input.InputManager.Instance != null)
            {
                lookInput = Core.Input.InputManager.Instance.GetMouseDelta(false);
            }
            else
            {
                lookInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            }
        }

        #endregion

        #region Camera Updates

        private void UpdateCamera()
        {
            if (cameraTransform == null)
                return;

            switch (spectatorMode)
            {
                case SpectatorMode.FollowPlayer:
                    UpdateFollowCamera();
                    break;

                case SpectatorMode.FreeCam:
                    UpdateFreeCamera();
                    break;
            }
        }

        private void UpdateFollowCamera()
        {
            if (currentSpectatingIndex < 0 || currentSpectatingIndex >= alivePlayers.Count)
            {
                RefreshAlivePlayers();
                return;
            }

            var targetPlayer = alivePlayers[currentSpectatingIndex];
            if (targetPlayer == null)
            {
                RefreshAlivePlayers();
                return;
            }

            // Follow target player
            Vector3 targetPosition = targetPlayer.transform.position + Vector3.up * cameraHeight;
            Vector3 smoothedPosition = Vector3.Lerp(cameraTransform.position, targetPosition, cameraSmoothSpeed * Time.deltaTime);
            cameraTransform.position = smoothedPosition;

            // Match target rotation
            Quaternion targetRotation = targetPlayer.transform.rotation;
            cameraTransform.rotation = Quaternion.Slerp(cameraTransform.rotation, targetRotation, cameraSmoothSpeed * Time.deltaTime);
        }

        private void UpdateFreeCamera()
        {
            // Movement
            Vector3 moveDirection = Vector3.zero;

            if (Input.GetKey(KeyCode.W))
                moveDirection += cameraTransform.forward;
            if (Input.GetKey(KeyCode.S))
                moveDirection -= cameraTransform.forward;
            if (Input.GetKey(KeyCode.A))
                moveDirection -= cameraTransform.right;
            if (Input.GetKey(KeyCode.D))
                moveDirection += cameraTransform.right;
            if (Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.Space))
                moveDirection += Vector3.up;
            if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.LeftControl))
                moveDirection -= Vector3.up;

            moveDirection.Normalize();

            // Speed modifier
            float speed = Input.GetKey(KeyCode.LeftShift) ? freeCamFastSpeed : freeCamMoveSpeed;

            // Apply movement
            cameraTransform.position += moveDirection * speed * Time.deltaTime;

            // Look rotation
            if (lookInput != Vector2.zero)
            {
                Vector3 rotation = cameraTransform.eulerAngles;
                rotation.y += lookInput.x * freeCamLookSpeed;
                rotation.x -= lookInput.y * freeCamLookSpeed;
                rotation.x = ClampAngle(rotation.x, -89f, 89f);
                cameraTransform.eulerAngles = rotation;
            }
        }

        private float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360f) angle += 360f;
            if (angle > 360f) angle -= 360f;
            return Mathf.Clamp(angle, min, max);
        }

        #endregion

        #region UI Updates

        private void UpdateUI()
        {
            if (spectatingText == null)
                return;

            string text = "";

            switch (spectatorMode)
            {
                case SpectatorMode.FollowPlayer:
                    if (currentSpectatingIndex >= 0 && currentSpectatingIndex < alivePlayers.Count)
                    {
                        var player = alivePlayers[currentSpectatingIndex];
                        string playerName = player != null ? player.name : "Unknown";
                        text = $"Spectating: {playerName} ({currentSpectatingIndex + 1}/{alivePlayers.Count})";
                    }
                    else
                    {
                        text = "Spectating...";
                    }
                    break;

                case SpectatorMode.FreeCam:
                    text = "Free Camera";
                    break;
            }

            text += $"\n\n[{nextPlayerKey}] Next | [{previousPlayerKey}] Previous | [{freeCamKey}] Free Cam";

            spectatingText.text = text;
        }

        #endregion

        #region Public API

        public bool IsSpectating => isSpectating;
        public SpectatorMode CurrentMode => spectatorMode;
        public int AlivePlayersCount => alivePlayers.Count;

        /// <summary>
        /// Forces spectating a specific player
        /// </summary>
        public void SpectatePlayer(PlayerController player)
        {
            if (!isSpectating)
                return;

            int index = alivePlayers.IndexOf(player);
            if (index >= 0)
            {
                currentSpectatingIndex = index;
                spectatorMode = SpectatorMode.FollowPlayer;
            }
        }

        #endregion
    }

    #region Enums

    public enum SpectatorMode
    {
        FollowPlayer,
        FreeCam
    }

    #endregion
}
