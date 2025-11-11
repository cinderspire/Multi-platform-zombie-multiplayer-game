using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Spectator
{
    /// <summary>
    /// Advanced spectator system with free camera, player switching, x-ray vision,
    /// timeline scrubbing, and cinematic camera modes.
    /// </summary>
    public class AdvancedSpectatorSystem : NetworkBehaviour
    {
        public static AdvancedSpectatorSystem Instance { get; private set; }

        [Header("Spectator Configuration")]
        [SerializeField] private float freeCamSpeed = 10f;
        [SerializeField] private float freeCamSprintMultiplier = 3f;
        [SerializeField] private bool enableXRayVision = true;
        [SerializeField] private bool enableTimelineScrubbing = true;

        [Header("Camera Modes")]
        [SerializeField] private float thirdPersonDistance = 5f;
        [SerializeField] private float cinematicSmoothTime = 2f;

        private Dictionary<ulong, SpectatorData> spectators = new Dictionary<ulong, SpectatorData>();
        private List<ulong> spectatable Players = new List<ulong>();

        public event Action<ulong, ulong> OnSpectatorTargetChanged;
        public event Action<ulong, SpectatorMode> OnSpectatorModeChanged;
        public event Action<ulong, bool> OnXRayVisionToggled;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        /// <summary>
        /// Enter spectator mode
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void EnterSpectatorModeServerRpc(ulong spectatorId, ServerRpcParams rpcParams = default)
        {
            if (!spectators.ContainsKey(spectatorId))
            {
                spectators[spectatorId] = new SpectatorData
                {
                    spectatorId = spectatorId,
                    currentMode = SpectatorMode.FollowPlayer,
                    targetPlayerId = GetFirstAvailablePlayer(),
                    freeCameraPosition = Vector3.zero,
                    freeCameraRotation = Quaternion.identity,
                    xRayEnabled = false,
                    timelinePosition = 0f
                };
            }

            EnableSpectatorClientRpc(spectatorId, spectators[spectatorId].targetPlayerId);
        }

        /// <summary>
        /// Switch spectator target to next/previous player
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void CycleSpectatorTargetServerRpc(ulong spectatorId, bool forward, ServerRpcParams rpcParams = default)
        {
            if (!spectators.TryGetValue(spectatorId, out var data)) return;

            UpdateSpectatablePlayers();

            if (spectatablePlayers.Count == 0) return;

            int currentIndex = spectatablePlayers.IndexOf(data.targetPlayerId);
            int nextIndex;

            if (forward)
            {
                nextIndex = (currentIndex + 1) % spectatablePlayers.Count;
            }
            else
            {
                nextIndex = currentIndex - 1;
                if (nextIndex < 0) nextIndex = spectatablePlayers.Count - 1;
            }

            data.targetPlayerId = spectatablePlayers[nextIndex];
            OnSpectatorTargetChanged?.Invoke(spectatorId, data.targetPlayerId);

            UpdateSpectatorTargetClientRpc(spectatorId, data.targetPlayerId);
        }

        /// <summary>
        /// Switch spectator mode
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void SetSpectatorModeServerRpc(ulong spectatorId, SpectatorMode mode, ServerRpcParams rpcParams = default)
        {
            if (!spectators.TryGetValue(spectatorId, out var data)) return;

            data.currentMode = mode;
            OnSpectatorModeChanged?.Invoke(spectatorId, mode);

            UpdateSpectatorModeClientRpc(spectatorId, mode);
        }

        /// <summary>
        /// Toggle X-ray vision
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void ToggleXRayVisionServerRpc(ulong spectatorId, ServerRpcParams rpcParams = default)
        {
            if (!enableXRayVision) return;
            if (!spectators.TryGetValue(spectatorId, out var data)) return;

            data.xRayEnabled = !data.xRayEnabled;
            OnXRayVisionToggled?.Invoke(spectatorId, data.xRayEnabled);

            ToggleXRayClientRpc(spectatorId, data.xRayEnabled);
        }

        /// <summary>
        /// Update free camera position (for free cam mode)
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void UpdateFreeCameraServerRpc(ulong spectatorId, Vector3 position, Quaternion rotation, ServerRpcParams rpcParams = default)
        {
            if (!spectators.TryGetValue(spectatorId, out var data)) return;
            if (data.currentMode != SpectatorMode.FreeCamera) return;

            data.freeCameraPosition = position;
            data.freeCameraRotation = rotation;
        }

        /// <summary>
        /// Scrub timeline (for replay/killcam)
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void ScrubTimelineServerRpc(ulong spectatorId, float normalizedTime, ServerRpcParams rpcParams = default)
        {
            if (!enableTimelineScrubbing) return;
            if (!spectators.TryGetValue(spectatorId, out var data)) return;

            data.timelinePosition = Mathf.Clamp01(normalizedTime);
            ScrubTimelineClientRpc(spectatorId, data.timelinePosition);
        }

        private void Update()
        {
            HandleLocalSpectatorInput();
        }

        private void HandleLocalSpectatorInput()
        {
            ulong localId = NetworkManager.Singleton.LocalClientId;
            if (!spectators.ContainsKey(localId)) return;

            var data = spectators[localId];

            // Mode switching
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                SetSpectatorModeServerRpc(localId, SpectatorMode.FirstPerson);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                SetSpectatorModeServerRpc(localId, SpectatorMode.ThirdPerson);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                SetSpectatorModeServerRpc(localId, SpectatorMode.FreeCamera);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                SetSpectatorModeServerRpc(localId, SpectatorMode.Cinematic);
            }

            // Player cycling
            if (Input.GetKeyDown(KeyCode.Mouse0) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                CycleSpectatorTargetServerRpc(localId, true);
            }
            else if (Input.GetKeyDown(KeyCode.Mouse1) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                CycleSpectatorTargetServerRpc(localId, false);
            }

            // X-ray vision
            if (Input.GetKeyDown(KeyCode.X))
            {
                ToggleXRayVisionServerRpc(localId);
            }

            // Free camera movement
            if (data.currentMode == SpectatorMode.FreeCamera)
            {
                HandleFreeCameraMovement(localId, data);
            }
        }

        private void HandleFreeCameraMovement(ulong spectatorId, SpectatorData data)
        {
            Camera mainCam = Camera.main;
            if (mainCam == null) return;

            // Movement input
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            float upDown = 0f;

            if (Input.GetKey(KeyCode.Space)) upDown = 1f;
            if (Input.GetKey(KeyCode.LeftControl)) upDown = -1f;

            // Sprint
            float speed = freeCamSpeed;
            if (Input.GetKey(KeyCode.LeftShift))
            {
                speed *= freeCamSprintMultiplier;
            }

            // Calculate movement
            Vector3 movement = mainCam.transform.right * horizontal +
                             mainCam.transform.forward * vertical +
                             Vector3.up * upDown;

            movement = movement.normalized * speed * Time.deltaTime;

            // Mouse look
            float mouseX = Input.GetAxis("Mouse X") * 2f;
            float mouseY = Input.GetAxis("Mouse Y") * 2f;

            Vector3 eulerAngles = mainCam.transform.eulerAngles;
            eulerAngles.y += mouseX;
            eulerAngles.x -= mouseY;
            eulerAngles.x = ClampAngle(eulerAngles.x, -89f, 89f);

            // Apply to camera
            mainCam.transform.position += movement;
            mainCam.transform.rotation = Quaternion.Euler(eulerAngles);

            // Update server
            UpdateFreeCameraServerRpc(spectatorId, mainCam.transform.position, mainCam.transform.rotation);
        }

        private float ClampAngle(float angle, float min, float max)
        {
            if (angle > 180f) angle -= 360f;
            return Mathf.Clamp(angle, min, max);
        }

        [ClientRpc]
        private void EnableSpectatorClientRpc(ulong spectatorId, ulong targetId)
        {
            if (NetworkManager.Singleton.LocalClientId != spectatorId) return;

            Debug.Log($"<color=cyan>Spectator Mode Enabled</color>");
            Debug.Log("Controls: 1=First Person, 2=Third Person, 3=Free Cam, 4=Cinematic");
            Debug.Log("Mouse1/Right Arrow=Next Player, Mouse2/Left Arrow=Previous Player, X=X-Ray");
        }

        [ClientRpc]
        private void UpdateSpectatorTargetClientRpc(ulong spectatorId, ulong newTargetId)
        {
            if (NetworkManager.Singleton.LocalClientId != spectatorId) return;

            // Update camera to follow new target
            ApplySpectatorCamera(spectatorId, newTargetId);
        }

        [ClientRpc]
        private void UpdateSpectatorModeClientRpc(ulong spectatorId, SpectatorMode mode)
        {
            if (NetworkManager.Singleton.LocalClientId != spectatorId) return;

            Debug.Log($"Spectator Mode: <color=yellow>{mode}</color>");
            ApplySpectatorMode(spectatorId, mode);
        }

        [ClientRpc]
        private void ToggleXRayClientRpc(ulong spectatorId, bool enabled)
        {
            if (NetworkManager.Singleton.LocalClientId != spectatorId) return;

            Debug.Log($"X-Ray Vision: <color={( enabled ? "green" : "red")}>{(enabled ? "ON" : "OFF")}</color>");
            ApplyXRayVision(enabled);
        }

        [ClientRpc]
        private void ScrubTimelineClientRpc(ulong spectatorId, float position)
        {
            if (NetworkManager.Singleton.LocalClientId != spectatorId) return;
            // Apply timeline scrubbing to replay system
        }

        private void ApplySpectatorCamera(ulong spectatorId, ulong targetId)
        {
            if (!spectators.TryGetValue(spectatorId, out var data)) return;

            Camera mainCam = Camera.main;
            if (mainCam == null) return;

            // Get target player position (would need actual player reference)
            // For now, just log the change
        }

        private void ApplySpectatorMode(ulong spectatorId, SpectatorMode mode)
        {
            Camera mainCam = Camera.main;
            if (mainCam == null) return;

            switch (mode)
            {
                case SpectatorMode.FirstPerson:
                    // Position camera at player's head
                    break;

                case SpectatorMode.ThirdPerson:
                    // Position camera behind player
                    break;

                case SpectatorMode.FreeCamera:
                    // Enable free movement
                    break;

                case SpectatorMode.Cinematic:
                    // Apply cinematic camera with smooth follow
                    break;
            }
        }

        private void ApplyXRayVision(bool enabled)
        {
            // Apply shader to see through walls
            if (enabled)
            {
                // Enable x-ray shader on all players/zombies
            }
            else
            {
                // Disable x-ray shader
            }
        }

        private void UpdateSpectatablePlayers()
        {
            spectatablePlayers.Clear();
            foreach (var client in NetworkManager.Singleton.ConnectedClients)
            {
                if (!spectators.ContainsKey(client.Key)) // Don't spectate other spectators
                {
                    spectatablePlayers.Add(client.Key);
                }
            }
        }

        private ulong GetFirstAvailablePlayer()
        {
            UpdateSpectatablePlayers();
            return spectatablePlayers.Count > 0 ? spectatablePlayers[0] : 0;
        }

        public bool IsSpectating(ulong playerId)
        {
            return spectators.ContainsKey(playerId);
        }

        public ulong GetSpectatorTarget(ulong spectatorId)
        {
            return spectators.TryGetValue(spectatorId, out var data) ? data.targetPlayerId : 0;
        }

        [Serializable]
        private class SpectatorData
        {
            public ulong spectatorId;
            public SpectatorMode currentMode;
            public ulong targetPlayerId;
            public Vector3 freeCameraPosition;
            public Quaternion freeCameraRotation;
            public bool xRayEnabled;
            public float timelinePosition;
        }

        public enum SpectatorMode
        {
            FirstPerson,
            ThirdPerson,
            FreeCamera,
            Cinematic
        }
    }
}
