using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Unity.Netcode;
using UnityEngine;

namespace Spectator
{
    /// <summary>
    /// Comprehensive spectator and replay system for multi-platform zombie multiplayer game.
    /// Handles spectating, replay recording/playback, highlights, and advanced camera controls.
    /// </summary>
    public class SpectatorSystem : NetworkBehaviour
    {
        public static SpectatorSystem Instance { get; private set; }

        [Header("Spectator Settings")]
        [SerializeField] private bool enableSpectatorMode = true;
        [SerializeField] private bool allowEnemySpectate = false;
        [SerializeField] private bool allowDeadPlayerSpectate = true;
        [SerializeField] private float spectatorSwitchCooldown = 1f;

        [Header("Camera Settings")]
        [SerializeField] private float freeCameraSpeed = 10f;
        [SerializeField] private float freeCameraSprintSpeed = 25f;
        [SerializeField] private float cameraMouseSensitivity = 2f;
        [SerializeField] private float smoothFollowSpeed = 5f;
        [SerializeField] private Vector3 thirdPersonOffset = new Vector3(0, 2, -5);

        [Header("Replay Settings")]
        [SerializeField] private bool enableReplayRecording = true;
        [SerializeField] private int maxReplayDuration = 600; // 10 minutes
        [SerializeField] private float replayTickRate = 30f;
        [SerializeField] private int maxStoredReplays = 50;
        [SerializeField] private string replayDirectory = "Replays";

        [Header("Playback Settings")]
        [SerializeField] private float[] playbackSpeeds = { 0.25f, 0.5f, 1f, 2f, 4f, 8f };
        [SerializeField] private bool enablePauseReplay = true;
        [SerializeField] private bool enableRewind = true;
        [SerializeField] private int rewindFrames = 300;

        [Header("Highlight Settings")]
        [SerializeField] private bool enableAutoHighlights = true;
        [SerializeField] private int highlightBufferSeconds = 10;
        [SerializeField] private int maxHighlightsPerMatch = 10;

        // Enums
        public enum SpectatorMode
        {
            None,
            FirstPerson,
            ThirdPerson,
            FreeCamera,
            Orbital,
            Cinematic
        }

        public enum ReplayState
        {
            Idle,
            Recording,
            Playing,
            Paused,
            Rewinding,
            FastForward
        }

        public enum HighlightType
        {
            Kill,
            MultiKill,
            HeadShot,
            LastKill,
            FirstBlood,
            LongRangeKill,
            MeleeKill,
            Extraction,
            Death,
            Custom
        }

        // Data structures
        [Serializable]
        public class SpectatorData
        {
            public ulong spectatorId;
            public ulong targetPlayerId;
            public SpectatorMode mode = SpectatorMode.FreeCamera;
            public bool isActive;
            public float lastSwitchTime;
            public List<ulong> availableTargets = new List<ulong>();
            public int currentTargetIndex = 0;
        }

        [Serializable]
        public class ReplayFrame
        {
            public float timestamp;
            public Dictionary<ulong, PlayerSnapshot> playerSnapshots = new Dictionary<ulong, PlayerSnapshot>();
            public List<ProjectileSnapshot> projectiles = new List<ProjectileSnapshot>();
            public List<EventSnapshot> events = new List<EventSnapshot>();
            public CameraSnapshot cameraData;
        }

        [Serializable]
        public class PlayerSnapshot
        {
            public ulong playerId;
            public Vector3 position;
            public Quaternion rotation;
            public int health;
            public int ammo;
            public string currentWeapon;
            public bool isAlive;
            public bool isShooting;
            public bool isReloading;
            public Vector3 aimDirection;
        }

        [Serializable]
        public class ProjectileSnapshot
        {
            public string projectileId;
            public Vector3 position;
            public Vector3 velocity;
            public string projectileType;
        }

        [Serializable]
        public class EventSnapshot
        {
            public string eventId;
            public string eventType;
            public ulong sourcePlayerId;
            public ulong targetPlayerId;
            public Vector3 position;
            public string data;
        }

        [Serializable]
        public class CameraSnapshot
        {
            public Vector3 position;
            public Quaternion rotation;
            public float fieldOfView;
        }

        [Serializable]
        public class ReplayData
        {
            public string replayId;
            public string matchId;
            public DateTime recordingDate;
            public float duration;
            public List<ReplayFrame> frames = new List<ReplayFrame>();
            public List<ulong> participants = new List<ulong>();
            public string mapName;
            public string gameMode;
            public Dictionary<string, object> metadata = new Dictionary<string, object>();
        }

        [Serializable]
        public class Highlight
        {
            public string highlightId;
            public HighlightType type;
            public float startTime;
            public float endTime;
            public ulong playerId;
            public string description;
            public int frameStart;
            public int frameEnd;
            public bool isSaved;
        }

        [Serializable]
        public class CameraBookmark
        {
            public string bookmarkId;
            public float timestamp;
            public Vector3 position;
            public Quaternion rotation;
            public string description;
        }

        [Serializable]
        public class SpectatorCamera
        {
            public Camera camera;
            public Vector3 position;
            public Quaternion rotation;
            public float currentSpeed;
            public bool isControlled;
        }

        // State
        private Dictionary<ulong, SpectatorData> spectators = new Dictionary<ulong, SpectatorData>();
        private Dictionary<string, ReplayData> replays = new Dictionary<string, ReplayData>();
        private ReplayData currentReplay;
        private ReplayState replayState = ReplayState.Idle;
        private float currentPlaybackTime = 0f;
        private int currentFrameIndex = 0;
        private float playbackSpeed = 1f;

        private List<Highlight> highlights = new List<Highlight>();
        private List<CameraBookmark> bookmarks = new List<CameraBookmark>();
        private SpectatorCamera spectatorCamera;

        // Recording state
        private bool isRecording = false;
        private float recordingStartTime;
        private float lastRecordTime;
        private Queue<ReplayFrame> replayBuffer = new Queue<ReplayFrame>();

        // Events
        public event Action<ulong, ulong> OnSpectatorTargetChanged;
        public event Action<ulong, SpectatorMode> OnSpectatorModeChanged;
        public event Action<ReplayState> OnReplayStateChanged;
        public event Action<float> OnPlaybackTimeChanged;
        public event Action<Highlight> OnHighlightCreated;
        public event Action<string> OnReplaySaved;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            InitializeSpectatorCamera();

            if (IsServer && enableReplayRecording)
            {
                StartReplayRecording();
            }
        }

        private void Update()
        {
            if (IsServer && isRecording)
            {
                RecordFrame();
            }

            if (replayState == ReplayState.Playing || replayState == ReplayState.FastForward)
            {
                UpdateReplayPlayback();
            }

            UpdateSpectatorCameras();
        }

        #region Initialization

        private void InitializeSpectatorCamera()
        {
            GameObject cameraObj = new GameObject("SpectatorCamera");
            Camera cam = cameraObj.AddComponent<Camera>();
            cam.enabled = false;

            spectatorCamera = new SpectatorCamera
            {
                camera = cam,
                position = Vector3.zero,
                rotation = Quaternion.identity,
                currentSpeed = freeCameraSpeed,
                isControlled = false
            };
        }

        #endregion

        #region Spectator Management

        public void EnableSpectatorMode(ulong playerId)
        {
            if (!enableSpectatorMode) return;

            if (!spectators.ContainsKey(playerId))
            {
                spectators[playerId] = new SpectatorData
                {
                    spectatorId = playerId,
                    mode = SpectatorMode.FreeCamera,
                    isActive = true
                };
            }
            else
            {
                spectators[playerId].isActive = true;
            }

            UpdateAvailableTargets(playerId);
            ActivateSpectatorCameraClientRpc(playerId);
        }

        public void DisableSpectatorMode(ulong playerId)
        {
            if (!spectators.ContainsKey(playerId)) return;

            spectators[playerId].isActive = false;
            DeactivateSpectatorCameraClientRpc(playerId);
        }

        [ClientRpc]
        private void ActivateSpectatorCameraClientRpc(ulong playerId)
        {
            if (NetworkManager.Singleton.LocalClientId == playerId)
            {
                spectatorCamera.camera.enabled = true;
                spectatorCamera.isControlled = true;
            }
        }

        [ClientRpc]
        private void DeactivateSpectatorCameraClientRpc(ulong playerId)
        {
            if (NetworkManager.Singleton.LocalClientId == playerId)
            {
                spectatorCamera.camera.enabled = false;
                spectatorCamera.isControlled = false;
            }
        }

        private void UpdateAvailableTargets(ulong spectatorId)
        {
            if (!spectators.ContainsKey(spectatorId)) return;

            var data = spectators[spectatorId];
            data.availableTargets.Clear();

            // Get all alive players
            var alivePlayers = GetAlivePlayers();

            foreach (var playerId in alivePlayers)
            {
                if (playerId == spectatorId) continue;

                // Check team restrictions if needed
                if (!allowEnemySpectate && !AreOnSameTeam(spectatorId, playerId))
                    continue;

                data.availableTargets.Add(playerId);
            }

            if (data.availableTargets.Count > 0 && data.currentTargetIndex >= data.availableTargets.Count)
            {
                data.currentTargetIndex = 0;
            }
        }

        public void SwitchSpectatorTarget(ulong spectatorId, int direction)
        {
            if (!spectators.ContainsKey(spectatorId)) return;

            var data = spectators[spectatorId];

            if (Time.time - data.lastSwitchTime < spectatorSwitchCooldown)
                return;

            if (data.availableTargets.Count == 0)
            {
                UpdateAvailableTargets(spectatorId);
                if (data.availableTargets.Count == 0) return;
            }

            data.currentTargetIndex += direction;

            if (data.currentTargetIndex < 0)
                data.currentTargetIndex = data.availableTargets.Count - 1;
            else if (data.currentTargetIndex >= data.availableTargets.Count)
                data.currentTargetIndex = 0;

            data.targetPlayerId = data.availableTargets[data.currentTargetIndex];
            data.lastSwitchTime = Time.time;

            OnSpectatorTargetChanged?.Invoke(spectatorId, data.targetPlayerId);
            UpdateSpectatorTargetClientRpc(spectatorId, data.targetPlayerId);
        }

        [ClientRpc]
        private void UpdateSpectatorTargetClientRpc(ulong spectatorId, ulong targetPlayerId)
        {
            OnSpectatorTargetChanged?.Invoke(spectatorId, targetPlayerId);
        }

        public void SetSpectatorMode(ulong spectatorId, SpectatorMode mode)
        {
            if (!spectators.ContainsKey(spectatorId)) return;

            spectators[spectatorId].mode = mode;
            OnSpectatorModeChanged?.Invoke(spectatorId, mode);
            UpdateSpectatorModeClientRpc(spectatorId, mode);
        }

        [ClientRpc]
        private void UpdateSpectatorModeClientRpc(ulong spectatorId, SpectatorMode mode)
        {
            OnSpectatorModeChanged?.Invoke(spectatorId, mode);
        }

        #endregion

        #region Camera Control

        private void UpdateSpectatorCameras()
        {
            foreach (var kvp in spectators)
            {
                var spectatorId = kvp.Key;
                var data = kvp.Value;

                if (!data.isActive) continue;

                switch (data.mode)
                {
                    case SpectatorMode.FreeCamera:
                        UpdateFreeCamera(spectatorId);
                        break;
                    case SpectatorMode.FirstPerson:
                        UpdateFirstPersonCamera(spectatorId, data.targetPlayerId);
                        break;
                    case SpectatorMode.ThirdPerson:
                        UpdateThirdPersonCamera(spectatorId, data.targetPlayerId);
                        break;
                    case SpectatorMode.Orbital:
                        UpdateOrbitalCamera(spectatorId, data.targetPlayerId);
                        break;
                }
            }
        }

        private void UpdateFreeCamera(ulong spectatorId)
        {
            if (!spectatorCamera.isControlled) return;

            // Movement
            Vector3 movement = Vector3.zero;

            if (Input.GetKey(KeyCode.W)) movement += spectatorCamera.camera.transform.forward;
            if (Input.GetKey(KeyCode.S)) movement -= spectatorCamera.camera.transform.forward;
            if (Input.GetKey(KeyCode.A)) movement -= spectatorCamera.camera.transform.right;
            if (Input.GetKey(KeyCode.D)) movement += spectatorCamera.camera.transform.right;
            if (Input.GetKey(KeyCode.E)) movement += Vector3.up;
            if (Input.GetKey(KeyCode.Q)) movement -= Vector3.up;

            float speed = Input.GetKey(KeyCode.LeftShift) ? freeCameraSprintSpeed : freeCameraSpeed;
            spectatorCamera.position += movement.normalized * speed * Time.deltaTime;

            // Rotation
            float mouseX = Input.GetAxis("Mouse X") * cameraMouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * cameraMouseSensitivity;

            Vector3 rotation = spectatorCamera.rotation.eulerAngles;
            rotation.y += mouseX;
            rotation.x -= mouseY;
            spectatorCamera.rotation = Quaternion.Euler(rotation);

            spectatorCamera.camera.transform.position = spectatorCamera.position;
            spectatorCamera.camera.transform.rotation = spectatorCamera.rotation;
        }

        private void UpdateFirstPersonCamera(ulong spectatorId, ulong targetId)
        {
            var targetTransform = GetPlayerTransform(targetId);
            if (targetTransform == null) return;

            spectatorCamera.camera.transform.position = targetTransform.position + Vector3.up * 1.6f;
            spectatorCamera.camera.transform.rotation = targetTransform.rotation;
        }

        private void UpdateThirdPersonCamera(ulong spectatorId, ulong targetId)
        {
            var targetTransform = GetPlayerTransform(targetId);
            if (targetTransform == null) return;

            Vector3 targetPosition = targetTransform.position + targetTransform.TransformDirection(thirdPersonOffset);
            spectatorCamera.camera.transform.position = Vector3.Lerp(
                spectatorCamera.camera.transform.position,
                targetPosition,
                smoothFollowSpeed * Time.deltaTime
            );

            spectatorCamera.camera.transform.LookAt(targetTransform.position + Vector3.up * 1.6f);
        }

        private void UpdateOrbitalCamera(ulong spectatorId, ulong targetId)
        {
            var targetTransform = GetPlayerTransform(targetId);
            if (targetTransform == null) return;

            float angle = Time.time * 30f;
            float radius = 8f;
            float height = 3f;

            Vector3 orbitPosition = targetTransform.position + new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                height,
                Mathf.Sin(angle * Mathf.Deg2Rad) * radius
            );

            spectatorCamera.camera.transform.position = orbitPosition;
            spectatorCamera.camera.transform.LookAt(targetTransform.position + Vector3.up * 1.6f);
        }

        #endregion

        #region Replay Recording

        public void StartReplayRecording()
        {
            if (!enableReplayRecording || isRecording) return;

            currentReplay = new ReplayData
            {
                replayId = $"replay_{Guid.NewGuid()}",
                matchId = GetCurrentMatchId(),
                recordingDate = DateTime.UtcNow,
                mapName = GetCurrentMapName(),
                gameMode = GetCurrentGameMode()
            };

            isRecording = true;
            recordingStartTime = Time.time;
            lastRecordTime = Time.time;

            Debug.Log($"Started replay recording: {currentReplay.replayId}");
        }

        public void StopReplayRecording()
        {
            if (!isRecording) return;

            isRecording = false;
            currentReplay.duration = Time.time - recordingStartTime;

            // Store replay
            replays[currentReplay.replayId] = currentReplay;

            Debug.Log($"Stopped replay recording. Duration: {currentReplay.duration}s, Frames: {currentReplay.frames.Count}");

            OnReplaySaved?.Invoke(currentReplay.replayId);
        }

        private void RecordFrame()
        {
            float timeSinceLastRecord = Time.time - lastRecordTime;
            if (timeSinceLastRecord < (1f / replayTickRate))
                return;

            var frame = new ReplayFrame
            {
                timestamp = Time.time - recordingStartTime
            };

            // Record player snapshots
            foreach (var playerId in GetAllPlayers())
            {
                var snapshot = CreatePlayerSnapshot(playerId);
                if (snapshot != null)
                {
                    frame.playerSnapshots[playerId] = snapshot;
                }
            }

            // Record projectiles
            frame.projectiles = GetProjectileSnapshots();

            // Record camera
            if (spectatorCamera != null && spectatorCamera.camera != null)
            {
                frame.cameraData = new CameraSnapshot
                {
                    position = spectatorCamera.camera.transform.position,
                    rotation = spectatorCamera.camera.transform.rotation,
                    fieldOfView = spectatorCamera.camera.fieldOfView
                };
            }

            currentReplay.frames.Add(frame);
            lastRecordTime = Time.time;

            // Limit replay duration
            if (currentReplay.duration > maxReplayDuration)
            {
                currentReplay.frames.RemoveAt(0);
            }
        }

        private PlayerSnapshot CreatePlayerSnapshot(ulong playerId)
        {
            var transform = GetPlayerTransform(playerId);
            if (transform == null) return null;

            return new PlayerSnapshot
            {
                playerId = playerId,
                position = transform.position,
                rotation = transform.rotation,
                health = GetPlayerHealth(playerId),
                ammo = GetPlayerAmmo(playerId),
                currentWeapon = GetPlayerWeapon(playerId),
                isAlive = IsPlayerAlive(playerId),
                aimDirection = transform.forward
            };
        }

        #endregion

        #region Replay Playback

        public void PlayReplay(string replayId)
        {
            if (!replays.ContainsKey(replayId))
            {
                Debug.LogWarning($"Replay {replayId} not found!");
                return;
            }

            currentReplay = replays[replayId];
            currentPlaybackTime = 0f;
            currentFrameIndex = 0;
            replayState = ReplayState.Playing;

            OnReplayStateChanged?.Invoke(replayState);
            Debug.Log($"Playing replay: {replayId}");
        }

        public void PauseReplay()
        {
            if (!enablePauseReplay) return;

            replayState = ReplayState.Paused;
            OnReplayStateChanged?.Invoke(replayState);
        }

        public void ResumeReplay()
        {
            replayState = ReplayState.Playing;
            OnReplayStateChanged?.Invoke(replayState);
        }

        public void StopReplay()
        {
            replayState = ReplayState.Idle;
            currentPlaybackTime = 0f;
            currentFrameIndex = 0;
            OnReplayStateChanged?.Invoke(replayState);
        }

        public void SetPlaybackSpeed(float speed)
        {
            playbackSpeed = speed;
        }

        public void SeekToTime(float time)
        {
            if (currentReplay == null) return;

            currentPlaybackTime = Mathf.Clamp(time, 0f, currentReplay.duration);

            // Find closest frame
            currentFrameIndex = 0;
            for (int i = 0; i < currentReplay.frames.Count; i++)
            {
                if (currentReplay.frames[i].timestamp <= currentPlaybackTime)
                {
                    currentFrameIndex = i;
                }
                else
                {
                    break;
                }
            }

            OnPlaybackTimeChanged?.Invoke(currentPlaybackTime);
        }

        private void UpdateReplayPlayback()
        {
            if (currentReplay == null || currentFrameIndex >= currentReplay.frames.Count)
            {
                StopReplay();
                return;
            }

            currentPlaybackTime += Time.deltaTime * playbackSpeed;

            // Find and apply current frame
            while (currentFrameIndex < currentReplay.frames.Count &&
                   currentReplay.frames[currentFrameIndex].timestamp <= currentPlaybackTime)
            {
                ApplyReplayFrame(currentReplay.frames[currentFrameIndex]);
                currentFrameIndex++;
            }

            OnPlaybackTimeChanged?.Invoke(currentPlaybackTime);

            // Check if replay finished
            if (currentPlaybackTime >= currentReplay.duration)
            {
                StopReplay();
            }
        }

        private void ApplyReplayFrame(ReplayFrame frame)
        {
            // Apply player positions and states
            foreach (var kvp in frame.playerSnapshots)
            {
                ApplyPlayerSnapshot(kvp.Value);
            }

            // Apply camera if available
            if (frame.cameraData != null && spectatorCamera != null)
            {
                spectatorCamera.camera.transform.position = frame.cameraData.position;
                spectatorCamera.camera.transform.rotation = frame.cameraData.rotation;
                spectatorCamera.camera.fieldOfView = frame.cameraData.fieldOfView;
            }
        }

        private void ApplyPlayerSnapshot(PlayerSnapshot snapshot)
        {
            // This would update visual representations of players during replay
            // Implementation depends on your player system
        }

        #endregion

        #region Highlights

        public void CreateHighlight(HighlightType type, ulong playerId, float duration = 10f)
        {
            if (!enableAutoHighlights && type != HighlightType.Custom) return;
            if (highlights.Count >= maxHighlightsPerMatch) return;

            float currentTime = Time.time - recordingStartTime;
            float startTime = Mathf.Max(0, currentTime - duration);

            var highlight = new Highlight
            {
                highlightId = $"highlight_{Guid.NewGuid()}",
                type = type,
                startTime = startTime,
                endTime = currentTime,
                playerId = playerId,
                description = GetHighlightDescription(type),
                frameStart = FindFrameAtTime(startTime),
                frameEnd = FindFrameAtTime(currentTime)
            };

            highlights.Add(highlight);
            OnHighlightCreated?.Invoke(highlight);

            Debug.Log($"Created highlight: {type} for player {playerId}");
        }

        private int FindFrameAtTime(float time)
        {
            if (currentReplay == null) return 0;

            for (int i = 0; i < currentReplay.frames.Count; i++)
            {
                if (currentReplay.frames[i].timestamp >= time)
                {
                    return i;
                }
            }
            return currentReplay.frames.Count - 1;
        }

        private string GetHighlightDescription(HighlightType type)
        {
            return type switch
            {
                HighlightType.Kill => "Kill",
                HighlightType.MultiKill => "Multi Kill",
                HighlightType.HeadShot => "Headshot",
                HighlightType.LastKill => "Last Kill",
                HighlightType.FirstBlood => "First Blood",
                HighlightType.LongRangeKill => "Long Range Kill",
                HighlightType.MeleeKill => "Melee Kill",
                HighlightType.Extraction => "Extraction",
                HighlightType.Death => "Death",
                _ => "Highlight"
            };
        }

        public void SaveHighlight(string highlightId)
        {
            var highlight = highlights.FirstOrDefault(h => h.highlightId == highlightId);
            if (highlight == null) return;

            highlight.isSaved = true;
            // Save to persistent storage
            SaveHighlightToFile(highlight);
        }

        #endregion

        #region Bookmarks

        public void CreateBookmark(float timestamp, string description = "")
        {
            var bookmark = new CameraBookmark
            {
                bookmarkId = $"bookmark_{Guid.NewGuid()}",
                timestamp = timestamp,
                position = spectatorCamera.camera.transform.position,
                rotation = spectatorCamera.camera.transform.rotation,
                description = description
            };

            bookmarks.Add(bookmark);
        }

        public void JumpToBookmark(string bookmarkId)
        {
            var bookmark = bookmarks.FirstOrDefault(b => b.bookmarkId == bookmarkId);
            if (bookmark == null) return;

            SeekToTime(bookmark.timestamp);
            spectatorCamera.camera.transform.position = bookmark.position;
            spectatorCamera.camera.transform.rotation = bookmark.rotation;
        }

        #endregion

        #region File Operations

        public void SaveReplay(string replayId)
        {
            if (!replays.ContainsKey(replayId)) return;

            string path = Path.Combine(Application.persistentDataPath, replayDirectory);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            string fileName = Path.Combine(path, $"{replayId}.json");
            string json = JsonUtility.ToJson(replays[replayId]);
            File.WriteAllText(fileName, json);

            Debug.Log($"Saved replay to {fileName}");
        }

        public void LoadReplay(string fileName)
        {
            string path = Path.Combine(Application.persistentDataPath, replayDirectory, fileName);
            if (!File.Exists(path))
            {
                Debug.LogWarning($"Replay file not found: {path}");
                return;
            }

            string json = File.ReadAllText(path);
            ReplayData replay = JsonUtility.FromJson<ReplayData>(json);
            replays[replay.replayId] = replay;

            Debug.Log($"Loaded replay: {replay.replayId}");
        }

        private void SaveHighlightToFile(Highlight highlight)
        {
            string path = Path.Combine(Application.persistentDataPath, replayDirectory, "Highlights");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            string fileName = Path.Combine(path, $"{highlight.highlightId}.json");
            string json = JsonUtility.ToJson(highlight);
            File.WriteAllText(fileName, json);
        }

        #endregion

        #region Helper Methods

        private List<ulong> GetAlivePlayers()
        {
            // Implementation depends on your player management system
            return new List<ulong>();
        }

        private List<ulong> GetAllPlayers()
        {
            // Implementation depends on your player management system
            return new List<ulong>();
        }

        private bool AreOnSameTeam(ulong player1, ulong player2)
        {
            // Implementation depends on your team system
            return true;
        }

        private Transform GetPlayerTransform(ulong playerId)
        {
            // Implementation depends on your player system
            return null;
        }

        private int GetPlayerHealth(ulong playerId) => 100;
        private int GetPlayerAmmo(ulong playerId) => 30;
        private string GetPlayerWeapon(ulong playerId) => "Rifle";
        private bool IsPlayerAlive(ulong playerId) => true;

        private List<ProjectileSnapshot> GetProjectileSnapshots()
        {
            return new List<ProjectileSnapshot>();
        }

        private string GetCurrentMatchId() => $"match_{DateTime.UtcNow.Ticks}";
        private string GetCurrentMapName() => "Map_01";
        private string GetCurrentGameMode() => "Extraction";

        public List<Highlight> GetHighlights() => highlights;
        public List<CameraBookmark> GetBookmarks() => bookmarks;
        public ReplayState GetReplayState() => replayState;
        public float GetPlaybackTime() => currentPlaybackTime;
        public float GetReplayDuration() => currentReplay?.duration ?? 0f;

        #endregion
    }
}
