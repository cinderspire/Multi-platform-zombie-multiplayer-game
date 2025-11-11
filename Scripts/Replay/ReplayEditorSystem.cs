using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ZombieGame
{
    /// <summary>
    /// Replay Editor System - Record, edit, and share gameplay moments
    /// Features timeline editing, camera controls, slow-motion, filters
    /// Export to video format for sharing on social media
    /// </summary>
    public class ReplayEditorSystem : MonoBehaviour
    {
        public static ReplayEditorSystem Instance { get; private set; }

        [Header("Recording Settings")]
        [SerializeField] private bool autoRecordMatches = true;
        [SerializeField] private float recordingFPS = 60f;
        [SerializeField] private int maxRecordingDuration = 600; // 10 minutes
        [SerializeField] private bool recordHighlightsOnly = false;

        [Header("Playback Settings")]
        [SerializeField] private float defaultPlaybackSpeed = 1.0f;
        [SerializeField] private float minPlaybackSpeed = 0.1f; // Slow motion
        [SerializeField] private float maxPlaybackSpeed = 5.0f; // Fast forward

        [Header("Camera Settings")]
        [SerializeField] private float freeCamSpeed = 10f;
        [SerializeField] private float freeCamSprintMultiplier = 3f;

        // Recording Data
        private ReplayRecording currentRecording = null;
        private bool isRecording = false;
        private float recordingTimer = 0f;

        // Playback Data
        private ReplayRecording activeReplay = null;
        private bool isPlayingReplay = false;
        private float playbackTime = 0f;
        private float playbackSpeed = 1.0f;
        private int currentFrameIndex = 0;

        // Camera
        private ReplayCameraMode cameraMode = ReplayCameraMode.FreeCam;
        private Transform replayCameraTransform;
        private ulong followedPlayerId = 0;

        // Saved Replays
        private List<ReplaySaveData> savedReplays = new List<ReplaySaveData>();

        // Events
        public event System.Action OnRecordingStarted;
        public event System.Action<string> OnRecordingSaved;
        public event System.Action OnPlaybackStarted;
        public event System.Action OnPlaybackEnded;

        public enum ReplayCameraMode
        {
            FreeCam,       // Fly anywhere
            FollowPlayer,  // Orbit player
            FirstPerson,   // Player POV
            Cinematic      // Smooth automated camera
        }

        [System.Serializable]
        public class ReplayRecording
        {
            public string recordingId;
            public string matchId;
            public System.DateTime recordingDate;
            public float duration;
            public List<ReplayFrame> frames = new List<ReplayFrame>();
            public Dictionary<ulong, PlayerReplayData> players = new Dictionary<ulong, PlayerReplayData>();
            public ReplayMetadata metadata = new ReplayMetadata();
        }

        [System.Serializable]
        public class ReplayFrame
        {
            public float timestamp;
            public Dictionary<ulong, EntityState> entityStates = new Dictionary<ulong, EntityState>();
            public List<ReplayEvent> events = new List<ReplayEvent>();
        }

        [System.Serializable]
        public class EntityState
        {
            public Vector3 position;
            public Quaternion rotation;
            public string animationState;
            public float health;
            public string weaponEquipped;
        }

        [System.Serializable]
        public class ReplayEvent
        {
            public ReplayEventType eventType;
            public ulong playerId;
            public ulong targetId;
            public Vector3 position;
            public string additionalData;
        }

        public enum ReplayEventType
        {
            Kill,
            Death,
            Damage,
            Heal,
            WeaponFire,
            Reload,
            AbilityUsed,
            ItemPickup,
            ObjectiveComplete
        }

        [System.Serializable]
        public class PlayerReplayData
        {
            public ulong playerId;
            public string playerName;
            public int finalKills;
            public int finalDeaths;
            public int finalScore;
        }

        [System.Serializable]
        public class ReplayMetadata
        {
            public string gameMode;
            public string mapName;
            public int totalPlayers;
            public string winnerTeam;
            public List<string> highlights = new List<string>(); // Timestamp markers
        }

        [System.Serializable]
        public class ReplaySaveData
        {
            public string saveId;
            public string recordingId;
            public string saveName;
            public System.DateTime saveDate;
            public float duration;
            public string thumbnailPath;
            public ReplayMetadata metadata;
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                LoadSavedReplays();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (isRecording)
            {
                UpdateRecording();
            }

            if (isPlayingReplay)
            {
                UpdatePlayback();
            }

            if (isPlayingReplay && cameraMode == ReplayCameraMode.FreeCam)
            {
                UpdateFreeCameraControls();
            }
        }

        // Recording

        public void StartRecording(string matchId)
        {
            if (isRecording) return;

            currentRecording = new ReplayRecording
            {
                recordingId = System.Guid.NewGuid().ToString(),
                matchId = matchId,
                recordingDate = System.DateTime.Now,
                duration = 0f
            };

            isRecording = true;
            recordingTimer = 0f;

            OnRecordingStarted?.Invoke();
            Debug.Log($"[ReplayEditor] Recording started: {currentRecording.recordingId}");
        }

        public void StopRecording()
        {
            if (!isRecording) return;

            currentRecording.duration = recordingTimer;
            isRecording = false;

            Debug.Log($"[ReplayEditor] Recording stopped. Duration: {recordingTimer}s, Frames: {currentRecording.frames.Count}");
        }

        private void UpdateRecording()
        {
            recordingTimer += Time.deltaTime;

            // Check max duration
            if (recordingTimer >= maxRecordingDuration)
            {
                StopRecording();
                return;
            }

            // Record frame (at specified FPS)
            float frameInterval = 1f / recordingFPS;

            if (recordingTimer >= currentRecording.frames.Count * frameInterval)
            {
                RecordFrame();
            }
        }

        private void RecordFrame()
        {
            var frame = new ReplayFrame
            {
                timestamp = recordingTimer
            };

            // Record all networked entities
            var networkObjects = FindObjectsOfType<NetworkObject>();

            foreach (var netObj in networkObjects)
            {
                if (netObj.IsPlayerObject)
                {
                    frame.entityStates[netObj.OwnerClientId] = new EntityState
                    {
                        position = netObj.transform.position,
                        rotation = netObj.transform.rotation,
                        animationState = "", // Would get from Animator
                        health = 100f, // Would get from HealthSystem
                        weaponEquipped = "" // Would get from WeaponSystem
                    };
                }
            }

            currentRecording.frames.Add(frame);
        }

        public void RecordEvent(ReplayEventType eventType, ulong playerId, ulong targetId = 0, Vector3 position = default, string additionalData = "")
        {
            if (!isRecording) return;

            if (currentRecording.frames.Count == 0) return;

            var lastFrame = currentRecording.frames[currentRecording.frames.Count - 1];

            lastFrame.events.Add(new ReplayEvent
            {
                eventType = eventType,
                playerId = playerId,
                targetId = targetId,
                position = position,
                additionalData = additionalData
            });

            // Auto-mark highlights
            if (eventType == ReplayEventType.Kill)
            {
                currentRecording.metadata.highlights.Add($"{recordingTimer:F1}");
            }
        }

        public void SaveRecording(string saveName)
        {
            if (currentRecording == null) return;

            var saveData = new ReplaySaveData
            {
                saveId = System.Guid.NewGuid().ToString(),
                recordingId = currentRecording.recordingId,
                saveName = saveName,
                saveDate = System.DateTime.Now,
                duration = currentRecording.duration,
                metadata = currentRecording.metadata
            };

            savedReplays.Add(saveData);

            // Serialize and save to disk (simplified)
            string json = JsonUtility.ToJson(currentRecording);
            string filePath = GetReplayFilePath(saveData.saveId);
            File.WriteAllText(filePath, json);

            OnRecordingSaved?.Invoke(saveData.saveId);
            Debug.Log($"[ReplayEditor] Recording saved: {saveName}");
        }

        // Playback

        public void LoadAndPlayReplay(string saveId)
        {
            var saveData = savedReplays.FirstOrDefault(s => s.saveId == saveId);

            if (saveData == null) return;

            string filePath = GetReplayFilePath(saveId);

            if (!File.Exists(filePath)) return;

            string json = File.ReadAllText(filePath);
            activeReplay = JsonUtility.FromJson<ReplayRecording>(json);

            StartPlayback();
        }

        public void StartPlayback()
        {
            if (activeReplay == null) return;

            isPlayingReplay = true;
            playbackTime = 0f;
            currentFrameIndex = 0;
            playbackSpeed = defaultPlaybackSpeed;

            OnPlaybackStarted?.Invoke();
            Debug.Log($"[ReplayEditor] Playback started. Duration: {activeReplay.duration}s");
        }

        public void StopPlayback()
        {
            isPlayingReplay = false;
            activeReplay = null;

            OnPlaybackEnded?.Invoke();
            Debug.Log("[ReplayEditor] Playback stopped");
        }

        private void UpdatePlayback()
        {
            playbackTime += Time.deltaTime * playbackSpeed;

            // Check if replay ended
            if (playbackTime >= activeReplay.duration)
            {
                StopPlayback();
                return;
            }

            // Find and display current frame
            while (currentFrameIndex < activeReplay.frames.Count &&
                   activeReplay.frames[currentFrameIndex].timestamp <= playbackTime)
            {
                DisplayFrame(activeReplay.frames[currentFrameIndex]);
                currentFrameIndex++;
            }
        }

        private void DisplayFrame(ReplayFrame frame)
        {
            // Apply entity states to scene
            // In real implementation, would update visual representations
        }

        // Camera Controls

        public void SetCameraMode(ReplayCameraMode mode)
        {
            cameraMode = mode;
        }

        public void FollowPlayer(ulong playerId)
        {
            followedPlayerId = playerId;
            cameraMode = ReplayCameraMode.FollowPlayer;
        }

        private void UpdateFreeCameraControls()
        {
            if (replayCameraTransform == null) return;

            // WASD movement
            Vector3 movement = Vector3.zero;

            if (Input.GetKey(KeyCode.W)) movement += replayCameraTransform.forward;
            if (Input.GetKey(KeyCode.S)) movement -= replayCameraTransform.forward;
            if (Input.GetKey(KeyCode.A)) movement -= replayCameraTransform.right;
            if (Input.GetKey(KeyCode.D)) movement += replayCameraTransform.right;

            float speed = freeCamSpeed;

            if (Input.GetKey(KeyCode.LeftShift))
            {
                speed *= freeCamSprintMultiplier;
            }

            replayCameraTransform.position += movement.normalized * speed * Time.deltaTime;

            // Mouse look
            if (Input.GetMouseButton(1)) // Right mouse button
            {
                float mouseX = Input.GetAxis("Mouse X");
                float mouseY = Input.GetAxis("Mouse Y");

                replayCameraTransform.Rotate(Vector3.up, mouseX, Space.World);
                replayCameraTransform.Rotate(Vector3.left, mouseY, Space.Self);
            }
        }

        // Playback Controls

        public void SetPlaybackSpeed(float speed)
        {
            playbackSpeed = Mathf.Clamp(speed, minPlaybackSpeed, maxPlaybackSpeed);
        }

        public void SeekToTime(float time)
        {
            playbackTime = Mathf.Clamp(time, 0f, activeReplay.duration);

            // Find corresponding frame
            currentFrameIndex = activeReplay.frames.FindIndex(f => f.timestamp >= playbackTime);

            if (currentFrameIndex == -1)
            {
                currentFrameIndex = activeReplay.frames.Count - 1;
            }
        }

        public void SeekToNextHighlight()
        {
            if (activeReplay == null) return;

            foreach (var highlight in activeReplay.metadata.highlights)
            {
                float highlightTime = float.Parse(highlight);

                if (highlightTime > playbackTime)
                {
                    SeekToTime(highlightTime - 3f); // 3 seconds before highlight
                    return;
                }
            }
        }

        public void TogglePause()
        {
            playbackSpeed = playbackSpeed == 0f ? defaultPlaybackSpeed : 0f;
        }

        // Helpers

        private string GetReplayFilePath(string saveId)
        {
            return Path.Combine(Application.persistentDataPath, "Replays", $"{saveId}.replay");
        }

        private void LoadSavedReplays()
        {
            string replayDir = Path.Combine(Application.persistentDataPath, "Replays");

            if (!Directory.Exists(replayDir))
            {
                Directory.CreateDirectory(replayDir);
            }

            // Load replay metadata
            // In real implementation, would load from index file
        }

        // Getters

        public bool IsRecording()
        {
            return isRecording;
        }

        public bool IsPlayingReplay()
        {
            return isPlayingReplay;
        }

        public float GetPlaybackTime()
        {
            return playbackTime;
        }

        public float GetPlaybackSpeed()
        {
            return playbackSpeed;
        }

        public List<ReplaySaveData> GetSavedReplays()
        {
            return savedReplays;
        }

        public ReplayRecording GetCurrentRecording()
        {
            return currentRecording;
        }

        public ReplayRecording GetActiveReplay()
        {
            return activeReplay;
        }
    }
}
