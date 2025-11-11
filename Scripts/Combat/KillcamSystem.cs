using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Combat
{
    /// <summary>
    /// Killcam system recording final moments before death and playing them back
    /// to the killed player, showing perspective of the killer.
    /// </summary>
    public class KillcamSystem : NetworkBehaviour
    {
        public static KillcamSystem Instance { get; private set; }

        [Header("Killcam Configuration")]
        [SerializeField] private float killcamDuration = 5f;
        [SerializeField] private float rewindTime = 3f;
        [SerializeField] private bool enableKillcam = true;
        [SerializeField] private bool showKillerPerspective = true;

        [Header("Recording Settings")]
        [SerializeField] private int maxRecordingFrames = 300; // 5 seconds at 60fps
        [SerializeField] private float recordingInterval = 0.016f; // ~60fps

        [Header("UI")]
        [SerializeField] private GameObject killcamUIOverlay;
        [SerializeField] private float skipDelay = 2f; // Can't skip for first 2 seconds

        private Dictionary<ulong, PlayerRecording> playerRecordings = new Dictionary<ulong, PlayerRecording>();
        private Dictionary<ulong, KillcamPlayback> activeKillcams = new Dictionary<ulong, KillcamPlayback>();

        public event Action<ulong, ulong> OnKillcamStarted; // victim, killer
        public event Action<ulong> OnKillcamEnded;
        public event Action<ulong, ulong> OnKillcamSkipped;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                StartCoroutine(RecordingLoop());
            }
        }

        private IEnumerator RecordingLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(recordingInterval);
                RecordAllPlayers();
            }
        }

        private void RecordAllPlayers()
        {
            foreach (var client in NetworkManager.Singleton.ConnectedClients)
            {
                ulong playerId = client.Key;

                if (!playerRecordings.ContainsKey(playerId))
                {
                    playerRecordings[playerId] = new PlayerRecording { playerId = playerId };
                }

                RecordPlayerFrame(playerId);
            }
        }

        private void RecordPlayerFrame(ulong playerId)
        {
            var recording = playerRecordings[playerId];

            // Get player transform (this would need to reference actual player object)
            // For now, storing conceptual data
            PlayerFrame frame = new PlayerFrame
            {
                timestamp = Time.time,
                position = Vector3.zero, // Would get from actual player
                rotation = Quaternion.identity, // Would get from actual player
                cameraPosition = Vector3.zero,
                cameraRotation = Quaternion.identity,
                weaponId = "", // Current weapon
                animationState = 0
            };

            recording.frames.Add(frame);

            // Keep only recent frames
            if (recording.frames.Count > maxRecordingFrames)
            {
                recording.frames.RemoveAt(0);
            }
        }

        /// <summary>
        /// Trigger killcam when a player is killed
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void TriggerKillcamServerRpc(ulong victimId, ulong killerId, Vector3 deathPosition, ServerRpcParams rpcParams = default)
        {
            if (!enableKillcam) return;
            if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(victimId)) return;
            if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(killerId)) return;

            // Get killer's recording
            if (!playerRecordings.ContainsKey(killerId)) return;

            var killerRecording = playerRecordings[killerId];

            // Get frames from rewindTime before death
            float deathTime = Time.time;
            float startTime = deathTime - rewindTime;

            List<PlayerFrame> killcamFrames = new List<PlayerFrame>();
            foreach (var frame in killerRecording.frames)
            {
                if (frame.timestamp >= startTime && frame.timestamp <= deathTime)
                {
                    killcamFrames.Add(frame);
                }
            }

            if (killcamFrames.Count > 0)
            {
                PlayKillcamClientRpc(victimId, killerId, killcamFrames.ToArray(), deathPosition);
            }
        }

        [ClientRpc]
        private void PlayKillcamClientRpc(ulong victimId, ulong killerId, PlayerFrame[] frames, Vector3 deathPosition)
        {
            // Only play for the victim
            if (NetworkManager.Singleton.LocalClientId != victimId) return;

            StartCoroutine(PlayKillcamSequence(victimId, killerId, frames, deathPosition));
        }

        private IEnumerator PlayKillcamSequence(ulong victimId, ulong killerId, PlayerFrame[] frames, Vector3 deathPosition)
        {
            OnKillcamStarted?.Invoke(victimId, killerId);

            // Show killcam UI
            if (killcamUIOverlay != null)
            {
                killcamUIOverlay.SetActive(true);
            }

            // Store playback data
            activeKillcams[victimId] = new KillcamPlayback
            {
                victimId = victimId,
                killerId = killerId,
                frames = frames,
                currentFrameIndex = 0,
                startTime = Time.time,
                canSkip = false
            };

            // Wait before allowing skip
            yield return new WaitForSeconds(skipDelay);
            if (activeKillcams.ContainsKey(victimId))
            {
                activeKillcams[victimId].canSkip = true;
            }

            // Play killcam for duration
            yield return new WaitForSeconds(killcamDuration - skipDelay);

            EndKillcam(victimId);
        }

        private void Update()
        {
            UpdateKillcamPlayback();
            CheckForSkipInput();
        }

        private void UpdateKillcamPlayback()
        {
            foreach (var kvp in activeKillcams)
            {
                var playback = kvp.Value;
                float elapsed = Time.time - playback.startTime;

                // Calculate which frame to show
                float frameDuration = killcamDuration / playback.frames.Length;
                int frameIndex = Mathf.FloorToInt(elapsed / frameDuration);

                if (frameIndex < playback.frames.Length)
                {
                    playback.currentFrameIndex = frameIndex;
                    ApplyFrameToCamera(playback.frames[frameIndex]);
                }
            }
        }

        private void ApplyFrameToCamera(PlayerFrame frame)
        {
            // Apply frame data to camera
            if (Camera.main != null)
            {
                Camera.main.transform.position = frame.cameraPosition;
                Camera.main.transform.rotation = frame.cameraRotation;
            }
        }

        private void CheckForSkipInput()
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                ulong localId = NetworkManager.Singleton.LocalClientId;
                if (activeKillcams.ContainsKey(localId) && activeKillcams[localId].canSkip)
                {
                    OnKillcamSkipped?.Invoke(localId, activeKillcams[localId].killerId);
                    EndKillcam(localId);
                }
            }
        }

        private void EndKillcam(ulong victimId)
        {
            if (activeKillcams.ContainsKey(victimId))
            {
                activeKillcams.Remove(victimId);

                if (killcamUIOverlay != null)
                {
                    killcamUIOverlay.SetActive(false);
                }

                OnKillcamEnded?.Invoke(victimId);
            }
        }

        public bool IsPlayingKillcam(ulong playerId)
        {
            return activeKillcams.ContainsKey(playerId);
        }

        [Serializable]
        private class PlayerRecording
        {
            public ulong playerId;
            public List<PlayerFrame> frames = new List<PlayerFrame>();
        }

        [Serializable]
        private class PlayerFrame : INetworkSerializable
        {
            public float timestamp;
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 cameraPosition;
            public Quaternion cameraRotation;
            public string weaponId;
            public int animationState;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref timestamp);
                serializer.SerializeValue(ref position);
                serializer.SerializeValue(ref rotation);
                serializer.SerializeValue(ref cameraPosition);
                serializer.SerializeValue(ref cameraRotation);
            }
        }

        [Serializable]
        private class KillcamPlayback
        {
            public ulong victimId;
            public ulong killerId;
            public PlayerFrame[] frames;
            public int currentFrameIndex;
            public float startTime;
            public bool canSkip;
        }
    }
}
