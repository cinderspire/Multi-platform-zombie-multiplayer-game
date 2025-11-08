using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DeadFrontier.Core.Replay
{
    /// <summary>
    /// Records and plays back match replays
    /// Essential for competitive integrity, content creation, and cheat investigation
    /// </summary>
    public class ReplaySystem : Singleton<ReplaySystem>
    {
        [Header("Recording Settings")]
        [SerializeField] private bool autoRecordMatches = true;
        [SerializeField] private float recordingInterval = 0.1f; // 10 snapshots per second
        [SerializeField] private int maxReplayDuration = 1800; // 30 minutes

        [Header("Storage Settings")]
        [SerializeField] private string replayFolder = "Replays";
        [SerializeField] private int maxStoredReplays = 10;
        [SerializeField] private bool compressReplays = true;

        [Header("Playback Settings")]
        [SerializeField] private float playbackSpeed = 1f;
        [SerializeField] private bool allowSpeedControl = true;
        [SerializeField] private float[] playbackSpeeds = { 0.25f, 0.5f, 1f, 2f, 4f };

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        // Recording state
        private bool isRecording = false;
        private ReplayData currentReplay;
        private float recordTimer = 0f;
        private List<Player.PlayerController> trackedPlayers = new List<Player.PlayerController>();
        private List<Zombies.ZombieAI> trackedZombies = new List<Zombies.ZombieAI>();

        // Playback state
        private bool isPlayingBack = false;
        private ReplayData loadedReplay;
        private int currentFrame = 0;
        private float playbackTimer = 0f;

        // Events
        public event System.Action OnRecordingStarted;
        public event System.Action<string> OnRecordingSaved;
        public event System.Action OnPlaybackStarted;
        public event System.Action OnPlaybackEnded;

        private void Update()
        {
            if (isRecording)
            {
                UpdateRecording();
            }
            else if (isPlayingBack)
            {
                UpdatePlayback();
            }
        }

        #region Recording

        /// <summary>
        /// Starts recording a match replay
        /// </summary>
        public void StartRecording()
        {
            if (isRecording)
            {
                if (showDebugLogs)
                    Debug.LogWarning("[ReplaySystem] Already recording");
                return;
            }

            if (isPlayingBack)
            {
                if (showDebugLogs)
                    Debug.LogWarning("[ReplaySystem] Cannot record during playback");
                return;
            }

            // Initialize replay data
            currentReplay = new ReplayData
            {
                replayId = System.Guid.NewGuid().ToString(),
                recordedAt = System.DateTime.Now,
                mapName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                gameModeId = "extraction", // TODO: Get from game manager
                frames = new List<ReplayFrame>()
            };

            // Find all entities to track
            trackedPlayers = FindObjectsOfType<Player.PlayerController>().ToList();
            trackedZombies = FindObjectsOfType<Zombies.ZombieAI>().ToList();

            isRecording = true;
            recordTimer = 0f;

            if (showDebugLogs)
                Debug.Log("[ReplaySystem] Recording started");

            OnRecordingStarted?.Invoke();

            // Track analytics
            Core.Analytics.AnalyticsManager.Instance?.TrackEvent("replay_recording_started", null);
        }

        /// <summary>
        /// Stops recording and saves the replay
        /// </summary>
        public void StopRecording()
        {
            if (!isRecording)
                return;

            isRecording = false;

            // Finalize replay data
            currentReplay.duration = currentReplay.frames.Count * recordingInterval;

            // Save replay
            string filePath = SaveReplay(currentReplay);

            if (showDebugLogs)
                Debug.Log($"[ReplaySystem] Recording stopped. Saved to: {filePath}");

            OnRecordingSaved?.Invoke(filePath);

            // Track analytics
            Core.Analytics.AnalyticsManager.Instance?.TrackEvent("replay_recording_stopped", new Dictionary<string, object>
            {
                { "duration", currentReplay.duration },
                { "frames", currentReplay.frames.Count }
            });

            // Clear temporary data
            currentReplay = null;
        }

        private void UpdateRecording()
        {
            recordTimer += Time.deltaTime;

            if (recordTimer >= recordingInterval)
            {
                recordTimer = 0f;
                RecordFrame();
            }

            // Check max duration
            if (currentReplay.frames.Count * recordingInterval >= maxReplayDuration)
            {
                if (showDebugLogs)
                    Debug.LogWarning("[ReplaySystem] Max replay duration reached, stopping recording");
                StopRecording();
            }
        }

        private void RecordFrame()
        {
            ReplayFrame frame = new ReplayFrame
            {
                frameIndex = currentReplay.frames.Count,
                timestamp = Time.time,
                playerStates = new List<PlayerState>(),
                zombieStates = new List<ZombieState>(),
                events = new List<ReplayEvent>()
            };

            // Record player states
            foreach (var player in trackedPlayers)
            {
                if (player == null) continue;

                var health = player.GetComponent<Player.PlayerHealth>();

                frame.playerStates.Add(new PlayerState
                {
                    playerId = player.GetInstanceID().ToString(),
                    position = player.transform.position,
                    rotation = player.transform.rotation.eulerAngles,
                    health = health != null ? health.CurrentHealth : 100f,
                    isAlive = health == null || !health.IsDead
                });
            }

            // Record zombie states (sample subset for performance)
            int zombieSampleRate = Mathf.Max(1, trackedZombies.Count / 50); // Max 50 zombies per frame
            for (int i = 0; i < trackedZombies.Count; i += zombieSampleRate)
            {
                var zombie = trackedZombies[i];
                if (zombie == null) continue;

                frame.zombieStates.Add(new ZombieState
                {
                    zombieId = zombie.GetInstanceID().ToString(),
                    position = zombie.transform.position,
                    rotation = zombie.transform.rotation.eulerAngles,
                    zombieType = zombie.Config.zombieType,
                    health = zombie.CurrentHealth,
                    isAlive = !zombie.IsDead
                });
            }

            currentReplay.frames.Add(frame);
        }

        #endregion

        #region Playback

        /// <summary>
        /// Loads and plays a replay file
        /// </summary>
        public bool PlayReplay(string replayId)
        {
            ReplayData replay = LoadReplay(replayId);
            if (replay == null)
            {
                if (showDebugLogs)
                    Debug.LogError($"[ReplaySystem] Failed to load replay: {replayId}");
                return false;
            }

            return PlayReplay(replay);
        }

        /// <summary>
        /// Plays a loaded replay
        /// </summary>
        public bool PlayReplay(ReplayData replay)
        {
            if (isRecording)
            {
                if (showDebugLogs)
                    Debug.LogWarning("[ReplaySystem] Cannot playback during recording");
                return false;
            }

            if (isPlayingBack)
            {
                StopPlayback();
            }

            loadedReplay = replay;
            currentFrame = 0;
            playbackTimer = 0f;
            isPlayingBack = true;

            if (showDebugLogs)
                Debug.Log($"[ReplaySystem] Playing replay: {replay.replayId}");

            OnPlaybackStarted?.Invoke();

            // Track analytics
            Core.Analytics.AnalyticsManager.Instance?.TrackEvent("replay_playback_started", new Dictionary<string, object>
            {
                { "replay_id", replay.replayId }
            });

            return true;
        }

        /// <summary>
        /// Stops replay playback
        /// </summary>
        public void StopPlayback()
        {
            if (!isPlayingBack)
                return;

            isPlayingBack = false;
            loadedReplay = null;
            currentFrame = 0;

            if (showDebugLogs)
                Debug.Log("[ReplaySystem] Playback stopped");

            OnPlaybackEnded?.Invoke();
        }

        private void UpdatePlayback()
        {
            playbackTimer += Time.deltaTime * playbackSpeed;

            if (playbackTimer >= recordingInterval)
            {
                playbackTimer = 0f;
                PlayFrame();
                currentFrame++;

                // Check if replay finished
                if (currentFrame >= loadedReplay.frames.Count)
                {
                    if (showDebugLogs)
                        Debug.Log("[ReplaySystem] Replay finished");
                    StopPlayback();
                }
            }
        }

        private void PlayFrame()
        {
            if (currentFrame >= loadedReplay.frames.Count)
                return;

            ReplayFrame frame = loadedReplay.frames[currentFrame];

            // Apply player states
            foreach (var playerState in frame.playerStates)
            {
                // TODO: Update player visual representation
                // In a full implementation, this would update player models/ghosts
            }

            // Apply zombie states
            foreach (var zombieState in frame.zombieStates)
            {
                // TODO: Update zombie visual representation
            }

            // Process events
            foreach (var replayEvent in frame.events)
            {
                // TODO: Display events (kills, objectives, etc.)
            }
        }

        #endregion

        #region Playback Controls

        /// <summary>
        /// Seeks to a specific frame
        /// </summary>
        public void SeekToFrame(int frameIndex)
        {
            if (!isPlayingBack || loadedReplay == null)
                return;

            currentFrame = Mathf.Clamp(frameIndex, 0, loadedReplay.frames.Count - 1);
            playbackTimer = 0f;

            if (showDebugLogs)
                Debug.Log($"[ReplaySystem] Seeked to frame {currentFrame}");
        }

        /// <summary>
        /// Sets playback speed
        /// </summary>
        public void SetPlaybackSpeed(float speed)
        {
            if (!allowSpeedControl)
                return;

            playbackSpeed = Mathf.Clamp(speed, 0.1f, 10f);

            if (showDebugLogs)
                Debug.Log($"[ReplaySystem] Playback speed: {playbackSpeed}x");
        }

        /// <summary>
        /// Pauses playback
        /// </summary>
        public void PausePlayback()
        {
            if (isPlayingBack)
            {
                playbackSpeed = 0f;
            }
        }

        /// <summary>
        /// Resumes playback
        /// </summary>
        public void ResumePlayback()
        {
            if (isPlayingBack)
            {
                playbackSpeed = 1f;
            }
        }

        #endregion

        #region Save/Load

        private string SaveReplay(ReplayData replay)
        {
            string folderPath = Path.Combine(Application.persistentDataPath, replayFolder);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string filename = $"{replay.recordedAt:yyyy-MM-dd_HH-mm-ss}_{replay.replayId}.replay";
            string filePath = Path.Combine(folderPath, filename);

            // Serialize replay data
            string json = JsonUtility.ToJson(replay, true);

            // Optionally compress
            if (compressReplays)
            {
                // TODO: Implement compression
                // Could use GZipStream or other compression
            }

            File.WriteAllText(filePath, json);

            // Manage storage limit
            ManageReplayStorage(folderPath);

            return filePath;
        }

        private ReplayData LoadReplay(string replayId)
        {
            string folderPath = Path.Combine(Application.persistentDataPath, replayFolder);
            if (!Directory.Exists(folderPath))
                return null;

            string[] files = Directory.GetFiles(folderPath, "*.replay");
            foreach (var file in files)
            {
                if (file.Contains(replayId))
                {
                    string json = File.ReadAllText(file);
                    return JsonUtility.FromJson<ReplayData>(json);
                }
            }

            return null;
        }

        private void ManageReplayStorage(string folderPath)
        {
            string[] files = Directory.GetFiles(folderPath, "*.replay");

            if (files.Length > maxStoredReplays)
            {
                // Sort by creation time
                var sortedFiles = files.OrderBy(f => File.GetCreationTime(f)).ToArray();

                // Delete oldest replays
                int toDelete = files.Length - maxStoredReplays;
                for (int i = 0; i < toDelete; i++)
                {
                    File.Delete(sortedFiles[i]);

                    if (showDebugLogs)
                        Debug.Log($"[ReplaySystem] Deleted old replay: {Path.GetFileName(sortedFiles[i])}");
                }
            }
        }

        /// <summary>
        /// Gets list of all saved replays
        /// </summary>
        public List<ReplayInfo> GetSavedReplays()
        {
            List<ReplayInfo> replays = new List<ReplayInfo>();

            string folderPath = Path.Combine(Application.persistentDataPath, replayFolder);
            if (!Directory.Exists(folderPath))
                return replays;

            string[] files = Directory.GetFiles(folderPath, "*.replay");
            foreach (var file in files)
            {
                FileInfo fileInfo = new FileInfo(file);
                replays.Add(new ReplayInfo
                {
                    filename = Path.GetFileName(file),
                    filePath = file,
                    createdAt = fileInfo.CreationTime,
                    fileSize = fileInfo.Length
                });
            }

            return replays.OrderByDescending(r => r.createdAt).ToList();
        }

        /// <summary>
        /// Deletes a replay file
        /// </summary>
        public bool DeleteReplay(string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);

                if (showDebugLogs)
                    Debug.Log($"[ReplaySystem] Deleted replay: {filePath}");

                return true;
            }

            return false;
        }

        #endregion

        #region Properties

        public bool IsRecording => isRecording;
        public bool IsPlayingBack => isPlayingBack;
        public float CurrentPlaybackSpeed => playbackSpeed;
        public int CurrentFrame => currentFrame;
        public int TotalFrames => loadedReplay?.frames.Count ?? 0;
        public float PlaybackProgress => loadedReplay != null ? (float)currentFrame / loadedReplay.frames.Count : 0f;

        #endregion
    }

    #region Data Structures

    [System.Serializable]
    public class ReplayData
    {
        public string replayId;
        public System.DateTime recordedAt;
        public string mapName;
        public string gameModeId;
        public float duration;
        public List<ReplayFrame> frames;
    }

    [System.Serializable]
    public class ReplayFrame
    {
        public int frameIndex;
        public float timestamp;
        public List<PlayerState> playerStates;
        public List<ZombieState> zombieStates;
        public List<ReplayEvent> events;
    }

    [System.Serializable]
    public struct PlayerState
    {
        public string playerId;
        public Vector3 position;
        public Vector3 rotation;
        public float health;
        public bool isAlive;
    }

    [System.Serializable]
    public struct ZombieState
    {
        public string zombieId;
        public Vector3 position;
        public Vector3 rotation;
        public Zombies.ZombieType zombieType;
        public float health;
        public bool isAlive;
    }

    [System.Serializable]
    public struct ReplayEvent
    {
        public string eventType;
        public string description;
        public Vector3 position;
    }

    public struct ReplayInfo
    {
        public string filename;
        public string filePath;
        public System.DateTime createdAt;
        public long fileSize;
    }

    #endregion
}
