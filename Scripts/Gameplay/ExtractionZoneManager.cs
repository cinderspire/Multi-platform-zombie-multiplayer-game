using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;
using System;

namespace DeadFrontier.Gameplay
{
    /// <summary>
    /// Manages extraction zones where players can escape the match with their loot.
    /// Core mechanic for extraction shooter gameplay.
    /// </summary>
    public class ExtractionZoneManager : NetworkBehaviour
    {
        public static ExtractionZoneManager Instance { get; private set; }

        [Header("Extraction Settings")]
        [SerializeField] private float extractionTime = 10f; // Time to extract
        [SerializeField] private int minActiveZones = 2;
        [SerializeField] private int maxActiveZones = 4;
        [SerializeField] private float zoneRotationInterval = 180f; // 3 minutes

        [Header("Zone Types")]
        [SerializeField] private ExtractionZoneType[] zoneTypes;

        [Header("Extraction Rewards")]
        [SerializeField] private float survivalBonusMultiplier = 1.5f;
        [SerializeField] private float earlyExtractionBonus = 1.2f;
        [SerializeField] private float lateExtractionPenalty = 0.8f;

        [Header("Audio")]
        [SerializeField] private AudioClip extractionStartSound;
        [SerializeField] private AudioClip extractionCompleteSound;
        [SerializeField] private AudioClip extractionCancelledSound;
        [SerializeField] private AudioClip zoneActivatedSound;

        // Active extraction zones
        private List<ExtractionZone> allZones = new List<ExtractionZone>();
        private List<ExtractionZone> activeZones = new List<ExtractionZone>();

        // Players currently extracting
        private Dictionary<ulong, ExtractionProgress> extractingPlayers = new Dictionary<ulong, ExtractionProgress>();

        // Zone rotation timer
        private float zoneRotationTimer;
        private float matchStartTime;

        // Events
        public event Action<ExtractionZone> OnZoneActivated;
        public event Action<ExtractionZone> OnZoneDeactivated;
        public event Action<ulong, ExtractionZone> OnExtractionStarted;
        public event Action<ulong, float> OnExtractionProgress;
        public event Action<ulong, bool> OnExtractionComplete; // bool = success

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
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
                matchStartTime = Time.time;
                InitializeExtractionZones();
                ActivateRandomZones();
            }
        }

        private void Update()
        {
            if (!IsServer) return;

            UpdateZoneRotation();
            UpdateExtractingPlayers();
        }

        #region Zone Management

        private void InitializeExtractionZones()
        {
            // Find all extraction zones in the scene
            allZones.Clear();
            allZones.AddRange(FindObjectsOfCreator<ExtractionZone>());

            foreach (var zone in allZones)
            {
                zone.Initialize(this);
                zone.SetActive(false);
            }

            Debug.Log($"[ExtractionZoneManager] Initialized {allZones.Count} extraction zones");
        }

        private void ActivateRandomZones()
        {
            if (allZones.Count == 0) return;

            // Deactivate current zones
            foreach (var zone in activeZones)
            {
                zone.SetActive(false);
                OnZoneDeactivated?.Invoke(zone);
            }
            activeZones.Clear();

            // Activate random zones
            int zoneCount = UnityEngine.Random.Range(minActiveZones, maxActiveZones + 1);
            var availableZones = new List<ExtractionZone>(allZones);

            for (int i = 0; i < zoneCount && availableZones.Count > 0; i++)
            {
                int randomIndex = UnityEngine.Random.Range(0, availableZones.Count);
                ExtractionZone zone = availableZones[randomIndex];
                availableZones.RemoveAt(randomIndex);

                zone.SetActive(true);
                activeZones.Add(zone);
                OnZoneActivated?.Invoke(zone);

                // Notify clients
                NotifyZoneActivatedClientRpc(zone.NetworkObjectId);
            }

            Debug.Log($"[ExtractionZoneManager] Activated {activeZones.Count} extraction zones");
        }

        private void UpdateZoneRotation()
        {
            zoneRotationTimer += Time.deltaTime;

            if (zoneRotationTimer >= zoneRotationInterval)
            {
                zoneRotationTimer = 0f;
                ActivateRandomZones();
            }
        }

        [ClientRpc]
        private void NotifyZoneActivatedClientRpc(ulong zoneNetworkId)
        {
            if (IsServer) return;

            var zone = NetworkManager.Singleton.SpawnManager.SpawnedObjects[zoneNetworkId].GetComponent<ExtractionZone>();
            if (zone != null)
            {
                zone.SetActive(true);
                OnZoneActivated?.Invoke(zone);

                if (zoneActivatedSound != null)
                    Core.AudioManager.Instance?.PlaySFX(zoneActivatedSound);
            }
        }

        #endregion

        #region Extraction Process

        public void StartExtraction(ulong playerId, ExtractionZone zone)
        {
            if (!IsServer) return;
            if (!zone.IsActive) return;
            if (extractingPlayers.ContainsKey(playerId)) return;

            var progress = new ExtractionProgress
            {
                playerId = playerId,
                zone = zone,
                startTime = Time.time,
                extractionTime = GetModifiedExtractionTime(playerId, zone),
                cancelled = false
            };

            extractingPlayers[playerId] = progress;
            OnExtractionStarted?.Invoke(playerId, zone);

            // Notify client
            NotifyExtractionStartedClientRpc(playerId, zone.NetworkObjectId, progress.extractionTime);

            Debug.Log($"[ExtractionZoneManager] Player {playerId} started extraction at {zone.ZoneName}");
        }

        public void CancelExtraction(ulong playerId, string reason = "Left zone")
        {
            if (!IsServer) return;
            if (!extractingPlayers.ContainsKey(playerId)) return;

            extractingPlayers[playerId].cancelled = true;
            extractingPlayers.Remove(playerId);

            OnExtractionComplete?.Invoke(playerId, false);

            // Notify client
            NotifyExtractionCancelledClientRpc(playerId, reason);

            Debug.Log($"[ExtractionZoneManager] Player {playerId} extraction cancelled: {reason}");
        }

        private void UpdateExtractingPlayers()
        {
            var completedPlayers = new List<ulong>();

            foreach (var kvp in extractingPlayers)
            {
                ulong playerId = kvp.Key;
                ExtractionProgress progress = kvp.Value;

                if (progress.cancelled)
                {
                    completedPlayers.Add(playerId);
                    continue;
                }

                float elapsed = Time.time - progress.startTime;
                float progressPercent = Mathf.Clamp01(elapsed / progress.extractionTime);

                OnExtractionProgress?.Invoke(playerId, progressPercent);

                // Notify client of progress
                if (Time.frameCount % 30 == 0) // Every 30 frames
                {
                    UpdateExtractionProgressClientRpc(playerId, progressPercent);
                }

                // Check if extraction complete
                if (progressPercent >= 1f)
                {
                    CompleteExtraction(playerId, progress);
                    completedPlayers.Add(playerId);
                }
            }

            // Remove completed players
            foreach (ulong playerId in completedPlayers)
            {
                extractingPlayers.Remove(playerId);
            }
        }

        private void CompleteExtraction(ulong playerId, ExtractionProgress progress)
        {
            OnExtractionComplete?.Invoke(playerId, true);

            // Calculate rewards
            int softCurrency = CalculateExtractionRewards(playerId, progress);

            // Award rewards
            if (Economy.EconomyManager.Instance != null)
            {
                Economy.EconomyManager.Instance.EarnSoftCurrency(softCurrency, "Successful extraction");
            }

            // Save player data
            if (Core.SaveSystem.Instance != null)
            {
                Core.SaveSystem.Instance.IncrementStat("totalExtractions", 1);
                Core.SaveSystem.Instance.IncrementStat("totalSoftCurrencyEarned", softCurrency);
            }

            // Notify client
            NotifyExtractionCompleteClientRpc(playerId, softCurrency);

            // Remove player from match
            RemovePlayerFromMatch(playerId);

            Debug.Log($"[ExtractionZoneManager] Player {playerId} extracted successfully with {softCurrency} rewards");
        }

        private int CalculateExtractionRewards(ulong playerId, ExtractionProgress progress)
        {
            // Base reward from inventory
            int baseReward = 0;
            if (InventoryManager.Instance != null)
            {
                baseReward = InventoryManager.Instance.GetInventoryValue(playerId);
            }

            // Survival bonus
            float survivalBonus = survivalBonusMultiplier;

            // Time-based bonus/penalty
            float timeSinceStart = Time.time - matchStartTime;
            float timeMultiplier = 1f;

            if (timeSinceStart < 300f) // Less than 5 minutes = early extraction bonus
            {
                timeMultiplier = earlyExtractionBonus;
            }
            else if (timeSinceStart > 1200f) // More than 20 minutes = late penalty
            {
                timeMultiplier = lateExtractionPenalty;
            }

            // Zone type multiplier
            float zoneMultiplier = progress.zone.RewardMultiplier;

            // Calculate final reward
            int finalReward = Mathf.RoundToInt(baseReward * survivalBonus * timeMultiplier * zoneMultiplier);

            return finalReward;
        }

        private float GetModifiedExtractionTime(ulong playerId, ExtractionZone zone)
        {
            float modifiedTime = zone.BaseExtractionTime;

            // Check for perks that reduce extraction time
            // TODO: Integrate with perk system when ready

            return modifiedTime;
        }

        private void RemovePlayerFromMatch(ulong playerId)
        {
            // Mark player as extracted
            if (Network.NetworkGameManager.Instance != null)
            {
                // Player should be moved to spectator or returned to lobby
                // Implementation depends on game state management
            }

            // Cleanup player state
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.TransferInventoryToStash(playerId);
            }
        }

        #endregion

        #region Client RPCs

        [ClientRpc]
        private void NotifyExtractionStartedClientRpc(ulong playerId, ulong zoneNetworkId, float extractionTime)
        {
            if (IsServer) return;

            var zone = NetworkManager.Singleton.SpawnManager.SpawnedObjects[zoneNetworkId].GetComponent<ExtractionZone>();
            if (zone != null)
            {
                OnExtractionStarted?.Invoke(playerId, zone);

                if (extractionStartSound != null)
                    Core.AudioManager.Instance?.PlaySFX(extractionStartSound);

                // Show UI notification
                Core.NotificationManager.Instance?.ShowNotification(
                    $"Extraction started at {zone.ZoneName}",
                    NotificationType.Info,
                    3f
                );
            }
        }

        [ClientRpc]
        private void UpdateExtractionProgressClientRpc(ulong playerId, float progress)
        {
            OnExtractionProgress?.Invoke(playerId, progress);
        }

        [ClientRpc]
        private void NotifyExtractionCompleteClientRpc(ulong playerId, int rewards)
        {
            if (IsServer) return;

            OnExtractionComplete?.Invoke(playerId, true);

            if (extractionCompleteSound != null)
                Core.AudioManager.Instance?.PlaySFX(extractionCompleteSound);

            // Show success UI
            Core.NotificationManager.Instance?.ShowNotification(
                $"Extraction successful! Earned {rewards} credits",
                NotificationType.Success,
                5f
            );
        }

        [ClientRpc]
        private void NotifyExtractionCancelledClientRpc(ulong playerId, string reason)
        {
            if (IsServer) return;

            OnExtractionComplete?.Invoke(playerId, false);

            if (extractionCancelledSound != null)
                Core.AudioManager.Instance?.PlaySFX(extractionCancelledSound);

            Core.NotificationManager.Instance?.ShowNotification(
                $"Extraction cancelled: {reason}",
                NotificationType.Warning,
                3f
            );
        }

        #endregion

        #region Public Getters

        public List<ExtractionZone> GetActiveZones() => new List<ExtractionZone>(activeZones);

        public bool IsPlayerExtracting(ulong playerId) => extractingPlayers.ContainsKey(playerId);

        public float GetExtractionProgress(ulong playerId)
        {
            if (!extractingPlayers.ContainsKey(playerId)) return 0f;

            var progress = extractingPlayers[playerId];
            float elapsed = Time.time - progress.startTime;
            return Mathf.Clamp01(elapsed / progress.extractionTime);
        }

        public float GetTimeUntilNextRotation() => zoneRotationInterval - zoneRotationTimer;

        #endregion

        private T[] FindObjectsOfCreator<T>() where T : Component
        {
            return FindObjectsOfType<T>();
        }
    }

    #region Supporting Classes

    [System.Serializable]
    public class ExtractionZoneType
    {
        public string typeName;
        public float baseExtractionTime = 10f;
        public float rewardMultiplier = 1f;
        public Color zoneColor = Color.green;
        public bool requiresInteraction = false;
    }

    public class ExtractionProgress
    {
        public ulong playerId;
        public ExtractionZone zone;
        public float startTime;
        public float extractionTime;
        public bool cancelled;
    }

    #endregion
}
