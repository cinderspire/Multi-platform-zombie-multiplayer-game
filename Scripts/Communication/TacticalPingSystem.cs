using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;
using System;

namespace DeadFrontier.Communication
{
    /// <summary>
    /// Comprehensive tactical ping communication system.
    /// Supports contextual pings, 3D world markers, team communication without voice.
    /// Enables strategic coordination through visual and audio cues.
    /// </summary>
    public class TacticalPingSystem : NetworkBehaviour
    {
        public static TacticalPingSystem Instance { get; private set; }

        [Header("Ping Settings")]
        [SerializeField] private float pingDuration = 5f;
        [SerializeField] private float pingCooldown = 0.5f;
        [SerializeField] private int maxActivePings = 20;
        [SerializeField] private float maxPingDistance = 500f;

        [Header("Ping Visuals")]
        [SerializeField] private GameObject[] pingPrefabs; // One for each ping type
        [SerializeField] private float pingScaleMin = 0.5f;
        [SerializeField] private float pingScaleMax = 1.5f;
        [SerializeField] private AnimationCurve pingPulseCurve;

        [Header("Audio")]
        [SerializeField] private AudioClip[] pingAudioClips;
        [SerializeField] private AudioSource pingAudioSource;

        [Header("UI")]
        [SerializeField] private GameObject minimapPingPrefab;
        [SerializeField] private float minimapPingDuration = 3f;

        [Header("Contextual Detection")]
        [SerializeField] private LayerMask contextDetectionMask;
        [SerializeField] private float contextDetectionRadius = 2f;

        // Active pings
        private Dictionary<string, ActivePing> activePings = new Dictionary<string, ActivePing>();
        private Dictionary<ulong, float> playerLastPingTime = new Dictionary<ulong, float>();

        // Ping history for each player
        private Dictionary<ulong, List<PingHistoryEntry>> pingHistory = new Dictionary<ulong, List<PingHistoryEntry>>();

        // Events
        public event Action<ActivePing> OnPingCreated;
        public event Action<string> OnPingExpired;
        public event Action<ulong, PingType> OnPlayerPinged;

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

        private void Update()
        {
            if (IsServer)
            {
                UpdateActivePings();
            }

            if (IsOwner)
            {
                HandlePingInput();
            }
        }

        #region Ping Creation

        private void HandlePingInput()
        {
            // Primary ping (middle mouse or Alt)
            if (Input.GetKeyDown(KeyCode.Mouse2) || Input.GetKeyDown(KeyCode.LeftAlt))
            {
                CreatePingFromCrosshair();
            }

            // Quick pings (number keys)
            if (Input.GetKeyDown(KeyCode.Alpha1))
                CreateQuickPing(PingType.Enemy);
            else if (Input.GetKeyDown(KeyCode.Alpha2))
                CreateQuickPing(PingType.Loot);
            else if (Input.GetKeyDown(KeyCode.Alpha3))
                CreateQuickPing(PingType.Danger);
            else if (Input.GetKeyDown(KeyCode.Alpha4))
                CreateQuickPing(PingType.Regroup);
        }

        private void CreatePingFromCrosshair()
        {
            ulong playerId = NetworkManager.Singleton.LocalClientId;

            // Check cooldown
            if (!CanPing(playerId)) return;

            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, maxPingDistance))
            {
                // Contextual ping type detection
                PingType pingType = DetectContextualPingType(hit);

                CreatePing(playerId, hit.point, pingType, hit.collider.gameObject);
            }
        }

        private void CreateQuickPing(PingType pingType)
        {
            ulong playerId = NetworkManager.Singleton.LocalClientId;

            // Check cooldown
            if (!CanPing(playerId)) return;

            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, maxPingDistance))
            {
                CreatePing(playerId, hit.point, pingType, null);
            }
        }

        private PingType DetectContextualPingType(RaycastHit hit)
        {
            // Check what was hit to determine ping type
            if (hit.collider.CompareTag("Enemy"))
                return PingType.Enemy;
            else if (hit.collider.CompareTag("Loot"))
                return PingType.Loot;
            else if (hit.collider.CompareTag("Extraction"))
                return PingType.Extraction;
            else if (hit.collider.CompareTag("Danger"))
                return PingType.Danger;

            // Check nearby objects
            Collider[] nearbyObjects = Physics.OverlapSphere(hit.point, contextDetectionRadius, contextDetectionMask);

            foreach (var obj in nearbyObjects)
            {
                if (obj.CompareTag("Enemy"))
                    return PingType.Enemy;
                else if (obj.CompareTag("Loot"))
                    return PingType.Loot;
            }

            // Default to location ping
            return PingType.Location;
        }

        private bool CanPing(ulong playerId)
        {
            if (playerLastPingTime.ContainsKey(playerId))
            {
                return Time.time - playerLastPingTime[playerId] >= pingCooldown;
            }

            return true;
        }

        public void CreatePing(ulong playerId, Vector3 position, PingType pingType, GameObject target = null)
        {
            if (IsServer)
            {
                CreatePingServerSide(playerId, position, pingType, target);
            }
            else
            {
                RequestCreatePingServerRpc(playerId, position, pingType);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestCreatePingServerRpc(ulong playerId, Vector3 position, PingType pingType)
        {
            CreatePingServerSide(playerId, position, pingType, null);
        }

        private void CreatePingServerSide(ulong playerId, Vector3 position, PingType pingType, GameObject target)
        {
            // Check active ping limit
            if (activePings.Count >= maxActivePings)
            {
                // Remove oldest ping
                var oldest = activePings.Values.OrderBy(p => p.creationTime).FirstOrDefault();
                if (oldest != null)
                {
                    RemovePing(oldest.pingId);
                }
            }

            string pingId = $"ping_{playerId}_{DateTime.UtcNow.Ticks}";

            var ping = new ActivePing
            {
                pingId = pingId,
                playerId = playerId,
                position = position,
                pingType = pingType,
                targetObject = target,
                creationTime = Time.time,
                expirationTime = Time.time + pingDuration
            };

            activePings[pingId] = ping;
            playerLastPingTime[playerId] = Time.time;

            // Add to history
            AddToPingHistory(playerId, pingType, position);

            // Notify clients
            CreatePingClientRpc(pingId, playerId, position, pingType);

            OnPingCreated?.Invoke(ping);
            OnPlayerPinged?.Invoke(playerId, pingType);

            Debug.Log($"[TacticalPingSystem] Ping created: {pingType} at {position} by player {playerId}");
        }

        [ClientRpc]
        private void CreatePingClientRpc(string pingId, ulong playerId, Vector3 position, PingType pingType)
        {
            // Spawn visual ping
            SpawnPingVisual(pingId, position, pingType);

            // Play audio
            PlayPingAudio(pingType, position);

            // Create minimap marker
            CreateMinimapPing(position, pingType);

            // Show UI notification
            ShowPingNotification(playerId, pingType);
        }

        #endregion

        #region Ping Management

        private void UpdateActivePings()
        {
            var pingsToRemove = new List<string>();

            foreach (var kvp in activePings)
            {
                var pingId = kvp.Key;
                var ping = kvp.Value;

                // Check expiration
                if (Time.time >= ping.expirationTime)
                {
                    pingsToRemove.Add(pingId);
                }
                // Check if target object was destroyed
                else if (ping.targetObject != null && ping.targetObject == null)
                {
                    pingsToRemove.Add(pingId);
                }
            }

            foreach (var pingId in pingsToRemove)
            {
                RemovePing(pingId);
            }
        }

        private void RemovePing(string pingId)
        {
            if (!activePings.ContainsKey(pingId)) return;

            activePings.Remove(pingId);

            RemovePingClientRpc(pingId);

            OnPingExpired?.Invoke(pingId);
        }

        [ClientRpc]
        private void RemovePingClientRpc(string pingId)
        {
            // Remove visual ping
            // This would find and destroy the ping GameObject
        }

        public void CancelPlayerPings(ulong playerId)
        {
            var playerPings = activePings.Values.Where(p => p.playerId == playerId).ToList();

            foreach (var ping in playerPings)
            {
                RemovePing(ping.pingId);
            }

            Debug.Log($"[TacticalPingSystem] Cancelled all pings for player {playerId}");
        }

        #endregion

        #region Visual & Audio

        private void SpawnPingVisual(string pingId, Vector3 position, PingType pingType)
        {
            int prefabIndex = (int)pingType;

            if (pingPrefabs == null || prefabIndex >= pingPrefabs.Length || pingPrefabs[prefabIndex] == null)
            {
                Debug.LogWarning($"[TacticalPingSystem] No prefab for ping type: {pingType}");
                return;
            }

            GameObject pingObj = Instantiate(pingPrefabs[prefabIndex], position, Quaternion.identity);
            pingObj.name = pingId;

            // Add animation component
            var animator = pingObj.AddComponent<PingAnimator>();
            animator.Initialize(pingDuration, pingScaleMin, pingScaleMax, pingPulseCurve);
        }

        private void PlayPingAudio(PingType pingType, Vector3 position)
        {
            if (pingAudioClips == null || pingAudioClips.Length == 0) return;

            int clipIndex = Mathf.Min((int)pingType, pingAudioClips.Length - 1);

            if (pingAudioSource != null && pingAudioClips[clipIndex] != null)
            {
                pingAudioSource.PlayOneShot(pingAudioClips[clipIndex]);
            }
            else
            {
                // Play at position if no audio source
                AudioSource.PlayClipAtPoint(pingAudioClips[clipIndex], position);
            }
        }

        private void CreateMinimapPing(Vector3 position, PingType pingType)
        {
            if (minimapPingPrefab == null) return;

            // This would integrate with minimap system
            // For now, just log
            Debug.Log($"[TacticalPingSystem] Minimap ping created at {position}");
        }

        private void ShowPingNotification(ulong playerId, PingType pingType)
        {
            // Get player name
            string playerName = $"Player {playerId}"; // In production, get from player manager

            string message = $"{playerName} pinged {GetPingTypeDisplayName(pingType)}";

            // Show UI notification
            // This would integrate with UI notification system
            Debug.Log($"[TacticalPingSystem] {message}");
        }

        private string GetPingTypeDisplayName(PingType pingType)
        {
            switch (pingType)
            {
                case PingType.Enemy: return "Enemy";
                case PingType.Loot: return "Loot";
                case PingType.Location: return "Location";
                case PingType.Danger: return "Danger";
                case PingType.Defending: return "Defending Here";
                case PingType.Attacking: return "Attacking";
                case PingType.Regroup: return "Regroup";
                case PingType.Extraction: return "Extraction Point";
                case PingType.Help: return "Need Help";
                default: return "Location";
            }
        }

        #endregion

        #region Ping History

        private void AddToPingHistory(ulong playerId, PingType pingType, Vector3 position)
        {
            if (!pingHistory.ContainsKey(playerId))
            {
                pingHistory[playerId] = new List<PingHistoryEntry>();
            }

            var entry = new PingHistoryEntry
            {
                pingType = pingType,
                position = position,
                timestamp = DateTime.UtcNow
            };

            pingHistory[playerId].Add(entry);

            // Limit history size
            if (pingHistory[playerId].Count > 50)
            {
                pingHistory[playerId].RemoveAt(0);
            }
        }

        public List<PingHistoryEntry> GetPingHistory(ulong playerId)
        {
            return pingHistory.ContainsKey(playerId)
                ? new List<PingHistoryEntry>(pingHistory[playerId])
                : new List<PingHistoryEntry>();
        }

        #endregion

        #region Contextual Ping Suggestions

        public PingType SuggestPingType(Vector3 position)
        {
            // Raycast down from position to check ground type
            RaycastHit hit;
            if (Physics.Raycast(position + Vector3.up, Vector3.down, out hit, 2f))
            {
                return DetectContextualPingType(hit);
            }

            return PingType.Location;
        }

        #endregion

        #region Public Getters

        public List<ActivePing> GetActivePings()
        {
            return new List<ActivePing>(activePings.Values);
        }

        public List<ActivePing> GetPlayerPings(ulong playerId)
        {
            return activePings.Values.Where(p => p.playerId == playerId).ToList();
        }

        public List<ActivePing> GetNearbyPings(Vector3 position, float radius)
        {
            return activePings.Values
                .Where(p => Vector3.Distance(p.position, position) <= radius)
                .ToList();
        }

        public ActivePing GetClosestPing(Vector3 position)
        {
            return activePings.Values
                .OrderBy(p => Vector3.Distance(p.position, position))
                .FirstOrDefault();
        }

        #endregion
    }

    #region Data Classes

    public class ActivePing
    {
        public string pingId;
        public ulong playerId;
        public Vector3 position;
        public PingType pingType;
        public GameObject targetObject;
        public float creationTime;
        public float expirationTime;
    }

    public class PingHistoryEntry
    {
        public PingType pingType;
        public Vector3 position;
        public DateTime timestamp;
    }

    public enum PingType
    {
        Location,       // Generic location marker
        Enemy,          // Enemy spotted
        Loot,           // Valuable loot
        Danger,         // Hazard warning
        Defending,      // Holding position
        Attacking,      // Moving to attack
        Regroup,        // Rally point
        Extraction,     // Extraction point
        Help            // Request assistance
    }

    #endregion

    #region Helper Components

    /// <summary>
    /// Animates ping visual effects
    /// </summary>
    public class PingAnimator : MonoBehaviour
    {
        private float duration;
        private float scaleMin;
        private float scaleMax;
        private AnimationCurve pulseCurve;
        private float startTime;
        private Vector3 baseScale;

        public void Initialize(float dur, float min, float max, AnimationCurve curve)
        {
            duration = dur;
            scaleMin = min;
            scaleMax = max;
            pulseCurve = curve;
            startTime = Time.time;
            baseScale = transform.localScale;
        }

        private void Update()
        {
            float elapsed = Time.time - startTime;
            float progress = elapsed / duration;

            if (progress >= 1f)
            {
                Destroy(gameObject);
                return;
            }

            // Pulse animation
            float pulseValue = pulseCurve != null ? pulseCurve.Evaluate(progress) : Mathf.PingPong(elapsed * 2f, 1f);
            float scale = Mathf.Lerp(scaleMin, scaleMax, pulseValue);

            transform.localScale = baseScale * scale;

            // Fade out towards end
            if (progress > 0.7f)
            {
                float fadeProgress = (progress - 0.7f) / 0.3f;
                Color color = Color.white;
                color.a = 1f - fadeProgress;

                // Apply to all renderers
                foreach (var renderer in GetComponentsInChildren<Renderer>())
                {
                    renderer.material.color = color;
                }
            }
        }
    }

    #endregion
}
