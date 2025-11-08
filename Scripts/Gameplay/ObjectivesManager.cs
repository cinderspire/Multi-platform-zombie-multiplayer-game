using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;
using System;

namespace DeadFrontier.Gameplay
{
    /// <summary>
    /// Manages map objectives, mission goals, and optional challenges.
    /// Provides additional gameplay depth and rewards beyond pure extraction.
    /// </summary>
    public class ObjectivesManager : NetworkBehaviour
    {
        public static ObjectivesManager Instance { get; private set; }

        [Header("Objective Settings")]
        [SerializeField] private bool enableObjectives = true;
        [SerializeField] private bool objectivesRequired = false; // Must complete to extract
        [SerializeField] private int maxActiveObjectives = 3;

        [Header("Rewards")]
        [SerializeField] private float objectiveXPMultiplier = 1.5f;
        [SerializeField] private float objectiveCurrencyMultiplier = 1.3f;
        [SerializeField] private float completionBonusMultiplier = 2f;

        [Header("UI")]
        [SerializeField] private Color primaryColor = Color.yellow;
        [SerializeField] private Color secondaryColor = Color.cyan;
        [SerializeField] private Color completeColor = Color.green;

        [Header("Audio")]
        [SerializeField] private AudioClip objectiveStartSound;
        [SerializeField] private AudioClip objectiveProgressSound;
        [SerializeField] private AudioClip objectiveCompleteSound;

        // Objectives
        private List<MapObjective> availableObjectives = new List<MapObjective>();
        private Dictionary<string, ObjectiveState> objectiveStates = new Dictionary<string, ObjectiveState>();

        // Network state
        private NetworkList<ObjectiveUpdate> activeObjectives;

        // Events
        public event Action<MapObjective> OnObjectiveStarted;
        public event Action<MapObjective, float> OnObjectiveProgress;
        public event Action<MapObjective> OnObjectiveCompleted;
        public event Action<MapObjective> OnObjectiveFailed;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                activeObjectives = new NetworkList<ObjectiveUpdate>();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                InitializeObjectives();
                ActivateObjectives();
            }

            activeObjectives.OnListChanged += OnActiveObjectivesChanged;
        }

        public override void OnNetworkDespawn()
        {
            activeObjectives.OnListChanged -= OnActiveObjectivesChanged;
            base.OnNetworkDespawn();
        }

        private void Update()
        {
            if (!IsServer || !enableObjectives) return;

            UpdateActiveObjectives();
        }

        #region Initialization

        private void InitializeObjectives()
        {
            // Find all objectives in scene
            var objectiveMarkers = FindObjectsOfType<ObjectiveMarker>();

            foreach (var marker in objectiveMarkers)
            {
                var objective = new MapObjective
                {
                    objectiveId = marker.objectiveId,
                    objectiveName = marker.objectiveName,
                    objectiveDescription = marker.objectiveDescription,
                    objectiveType = marker.objectiveType,
                    objectiveLocation = marker.transform.position,
                    objectiveRadius = marker.radius,
                    rewardSoftCurrency = marker.rewardCurrency,
                    rewardXP = marker.rewardXP
                };

                availableObjectives.Add(objective);
                marker.Initialize(this);
            }

            Debug.Log($"[ObjectivesManager] Initialized {availableObjectives.Count} objectives");
        }

        private void ActivateObjectives()
        {
            if (!IsServer) return;

            // Randomly select objectives to activate
            int objectivesToActivate = Mathf.Min(maxActiveObjectives, availableObjectives.Count);
            var shuffled = availableObjectives.OrderBy(x => UnityEngine.Random.value).Take(objectivesToActivate);

            foreach (var objective in shuffled)
            {
                ActivateObjective(objective);
            }
        }

        #endregion

        #region Objective Management

        private void ActivateObjective(MapObjective objective)
        {
            if (!IsServer) return;

            var state = new ObjectiveState
            {
                objective = objective,
                isActive = true,
                progress = 0f,
                startTime = Time.time
            };

            objectiveStates[objective.objectiveId] = state;

            // Add to network list
            var update = new ObjectiveUpdate
            {
                objectiveId = objective.objectiveId,
                isActive = true,
                progress = 0f
            };

            activeObjectives.Add(update);

            OnObjectiveStarted?.Invoke(objective);

            Debug.Log($"[ObjectivesManager] Activated objective: {objective.objectiveName}");
        }

        private void UpdateActiveObjectives()
        {
            foreach (var kvp in objectiveStates.ToList())
            {
                if (!kvp.Value.isActive || kvp.Value.isCompleted) continue;

                var state = kvp.Value;
                var objective = state.objective;

                // Update objective based on type
                switch (objective.objectiveType)
                {
                    case Core.ObjectiveType.Eliminate:
                        UpdateEliminateObjective(state);
                        break;

                    case Core.ObjectiveType.Collect:
                        UpdateCollectObjective(state);
                        break;

                    case Core.ObjectiveType.Secure:
                        UpdateSecureObjective(state);
                        break;

                    case Core.ObjectiveType.Activate:
                        UpdateActivateObjective(state);
                        break;

                    case Core.ObjectiveType.Survive:
                        UpdateSurviveObjective(state);
                        break;
                }
            }
        }

        private void UpdateEliminateObjective(ObjectiveState state)
        {
            // Check if required zombies are killed
            // This would be tracked via ZombieManager kill events
            // Placeholder: auto-complete after 30 seconds
            if (Time.time - state.startTime > 30f && state.progress < 1f)
            {
                SetObjectiveProgress(state.objective.objectiveId, 1f);
            }
        }

        private void UpdateCollectObjective(ObjectiveState state)
        {
            // Check if required items are collected
            // This would be tracked via InventoryManager pickup events
        }

        private void UpdateSecureObjective(ObjectiveState state)
        {
            // Check if players are in objective area
            bool anyPlayerInZone = false;
            var players = NetworkManager.Singleton.ConnectedClientsList;

            foreach (var client in players)
            {
                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
                    client.ClientId, out NetworkObject playerObj))
                {
                    float distance = Vector3.Distance(playerObj.transform.position, state.objective.objectiveLocation);
                    if (distance <= state.objective.objectiveRadius)
                    {
                        anyPlayerInZone = true;
                        break;
                    }
                }
            }

            if (anyPlayerInZone)
            {
                state.secureTimer += Time.deltaTime;
                float requiredTime = 60f; // 1 minute to secure
                float progress = Mathf.Clamp01(state.secureTimer / requiredTime);

                if (progress != state.progress)
                {
                    SetObjectiveProgress(state.objective.objectiveId, progress);
                }
            }
            else
            {
                // Lose progress if no one in zone
                state.secureTimer = Mathf.Max(0f, state.secureTimer - Time.deltaTime * 0.5f);
                float progress = Mathf.Clamp01(state.secureTimer / 60f);

                if (progress != state.progress)
                {
                    SetObjectiveProgress(state.objective.objectiveId, progress);
                }
            }
        }

        private void UpdateActivateObjective(ObjectiveState state)
        {
            // Check if objective device is activated
            // This would be triggered via ObjectiveMarker interaction
        }

        private void UpdateSurviveObjective(ObjectiveState state)
        {
            // Check survival time
            float elapsed = Time.time - state.startTime;
            float requiredTime = 300f; // 5 minutes
            float progress = Mathf.Clamp01(elapsed / requiredTime);

            if (progress != state.progress)
            {
                SetObjectiveProgress(state.objective.objectiveId, progress);
            }
        }

        #endregion

        #region Progress Tracking

        public void SetObjectiveProgress(string objectiveId, float progress)
        {
            if (!IsServer) return;
            if (!objectiveStates.ContainsKey(objectiveId)) return;

            var state = objectiveStates[objectiveId];
            float oldProgress = state.progress;
            state.progress = Mathf.Clamp01(progress);

            // Update network list
            for (int i = 0; i < activeObjectives.Count; i++)
            {
                if (activeObjectives[i].objectiveId == objectiveId)
                {
                    var update = activeObjectives[i];
                    update.progress = state.progress;
                    activeObjectives[i] = update;
                    break;
                }
            }

            OnObjectiveProgress?.Invoke(state.objective, state.progress);

            // Check for completion
            if (state.progress >= 1f && !state.isCompleted)
            {
                CompleteObjective(objectiveId);
            }
        }

        public void IncrementObjectiveProgress(string objectiveId, float amount)
        {
            if (!objectiveStates.ContainsKey(objectiveId)) return;

            var state = objectiveStates[objectiveId];
            SetObjectiveProgress(objectiveId, state.progress + amount);
        }

        private void CompleteObjective(string objectiveId)
        {
            if (!IsServer) return;
            if (!objectiveStates.ContainsKey(objectiveId)) return;

            var state = objectiveStates[objectiveId];
            state.isCompleted = true;
            state.completionTime = Time.time;

            // Award rewards to all players
            AwardObjectiveRewards(state.objective);

            OnObjectiveCompleted?.Invoke(state.objective);

            // Notify clients
            NotifyObjectiveCompleteClientRpc(objectiveId, state.objective.objectiveName);

            Debug.Log($"[ObjectivesManager] Objective completed: {state.objective.objectiveName}");
        }

        private void AwardObjectiveRewards(MapObjective objective)
        {
            int softCurrency = Mathf.RoundToInt(objective.rewardSoftCurrency * objectiveCurrencyMultiplier);
            int xp = Mathf.RoundToInt(objective.rewardXP * objectiveXPMultiplier);

            // Award to all alive players
            var players = NetworkManager.Singleton.ConnectedClientsList;

            foreach (var client in players)
            {
                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
                    client.ClientId, out NetworkObject playerObj))
                {
                    var health = playerObj.GetComponent<Player.PlayerHealth>();
                    if (health != null && health.IsAlive)
                    {
                        // Award currency
                        if (Economy.EconomyManager.Instance != null)
                        {
                            Economy.EconomyManager.Instance.EarnSoftCurrency(softCurrency, $"Objective: {objective.objectiveName}");
                        }

                        // Award XP
                        if (Progression.AchievementManager.Instance != null)
                        {
                            // XP would be awarded via progression system
                        }
                    }
                }
            }
        }

        #endregion

        #region Client RPCs

        [ClientRpc]
        private void NotifyObjectiveCompleteClientRpc(string objectiveId, string objectiveName)
        {
            if (objectiveCompleteSound != null)
                Core.AudioManager.Instance?.PlaySFX(objectiveCompleteSound);

            Core.NotificationManager.Instance?.ShowNotification(
                $"Objective Complete: {objectiveName}",
                NotificationType.Success,
                4f
            );
        }

        private void OnActiveObjectivesChanged(NetworkListEvent<ObjectiveUpdate> changeEvent)
        {
            // Update UI when objectives change
            // Clients can react to new objectives here
        }

        #endregion

        #region Public Getters

        public List<MapObjective> GetActiveObjectives()
        {
            return objectiveStates.Values
                .Where(s => s.isActive && !s.isCompleted)
                .Select(s => s.objective)
                .ToList();
        }

        public float GetObjectiveProgress(string objectiveId)
        {
            if (objectiveStates.ContainsKey(objectiveId))
                return objectiveStates[objectiveId].progress;
            return 0f;
        }

        public bool IsObjectiveComplete(string objectiveId)
        {
            if (objectiveStates.ContainsKey(objectiveId))
                return objectiveStates[objectiveId].isCompleted;
            return false;
        }

        public int GetCompletedObjectivesCount()
        {
            return objectiveStates.Values.Count(s => s.isCompleted);
        }

        public bool CanExtract()
        {
            if (!objectivesRequired) return true;

            // Must complete all primary objectives to extract
            return GetActiveObjectives().Count == 0;
        }

        #endregion
    }

    #region Supporting Classes

    public class ObjectiveState
    {
        public MapObjective objective;
        public bool isActive;
        public bool isCompleted;
        public float progress;
        public float startTime;
        public float completionTime;
        public float secureTimer; // For secure objectives
    }

    public struct ObjectiveUpdate : INetworkSerializable
    {
        public string objectiveId;
        public bool isActive;
        public float progress;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref objectiveId);
            serializer.SerializeValue(ref isActive);
            serializer.SerializeValue(ref progress);
        }
    }

    public class ObjectiveMarker : MonoBehaviour
    {
        [Header("Objective Configuration")]
        public string objectiveId;
        public string objectiveName;
        [TextArea(2, 3)]
        public string objectiveDescription;
        public Core.ObjectiveType objectiveType;
        public float radius = 10f;
        public int rewardCurrency = 500;
        public int rewardXP = 100;

        [Header("Visuals")]
        public GameObject markerVisual;
        public ParticleSystem activeVFX;

        private ObjectivesManager manager;

        public void Initialize(ObjectivesManager objectivesManager)
        {
            manager = objectivesManager;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, radius);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(transform.position, radius);

            #if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 3f, objectiveName);
            #endif
        }
    }

    #endregion
}
