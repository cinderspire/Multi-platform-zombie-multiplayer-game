using UnityEngine;
using System;
using System.Collections.Generic;

namespace DeadFrontier.Zombies
{
    /// <summary>
    /// Handles zombie vision and hearing senses
    /// </summary>
    public class ZombieSensors : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private ZombieConfig config;

        [Header("Layers")]
        [SerializeField] private LayerMask playerLayer;
        [SerializeField] private LayerMask obstructionLayer;

        [Header("Debug")]
        [SerializeField] private bool showDebugGizmos = false;

        // Memory
        private Vector3 lastKnownPosition;
        private float lastSeenTime;
        private Transform currentTarget;

        // Events
        public event Action<Transform> OnPlayerDetected;
        public event Action<Vector3> OnNoiseHeard;
        public event Action OnTargetLost;

        // Properties
        public Transform CurrentTarget => currentTarget;
        public bool HasTarget => currentTarget != null;
        public Vector3 LastKnownPosition => lastKnownPosition;
        public float TimeSinceLastSeen => Time.time - lastSeenTime;

        private void Awake()
        {
            if (config == null)
            {
                Debug.LogWarning("[ZombieSensors] No config assigned!");
            }
        }

        /// <summary>
        /// Sets the zombie configuration
        /// </summary>
        public void SetConfig(ZombieConfig newConfig)
        {
            config = newConfig;
        }

        /// <summary>
        /// Updates all senses
        /// </summary>
        public void Sense()
        {
            // Vision check (expensive, run less frequently)
            if (Time.frameCount % 10 == 0) // Every 10 frames (~6 times per second at 60 FPS)
            {
                CheckVision();
            }

            // Hearing check (cheaper, run every frame)
            CheckHearing();

            // Update memory
            UpdateMemory();
        }

        /// <summary>
        /// Checks for players in vision range
        /// </summary>
        private void CheckVision()
        {
            // Find all players in vision range
            Collider[] colliders = Physics.OverlapSphere(
                transform.position,
                config.visionRange,
                playerLayer
            );

            Transform closestTarget = null;
            float closestDistance = float.MaxValue;

            foreach (var col in colliders)
            {
                Vector3 directionToTarget = (col.transform.position - transform.position).normalized;
                float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);

                // Check if target is within field of view
                if (angleToTarget < config.visionAngle / 2f)
                {
                    // Raycast to check line of sight
                    Vector3 rayOrigin = transform.position + Vector3.up; // Slightly above ground
                    float distanceToTarget = Vector3.Distance(transform.position, col.transform.position);

                    if (Physics.Raycast(
                        rayOrigin,
                        directionToTarget,
                        out RaycastHit hit,
                        distanceToTarget,
                        playerLayer | obstructionLayer
                    ))
                    {
                        // Check if we hit the player (not an obstruction)
                        if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Player"))
                        {
                            // Found a visible player
                            if (distanceToTarget < closestDistance)
                            {
                                closestTarget = col.transform;
                                closestDistance = distanceToTarget;
                            }
                        }
                    }
                }
            }

            // Update target
            if (closestTarget != null)
            {
                if (currentTarget != closestTarget)
                {
                    currentTarget = closestTarget;
                    OnPlayerDetected?.Invoke(currentTarget);
                }

                lastKnownPosition = currentTarget.position;
                lastSeenTime = Time.time;
            }
        }

        /// <summary>
        /// Checks for noise events in hearing range
        /// </summary>
        private void CheckHearing()
        {
            // Get all noise events from AudioManager
            List<Core.NoiseEvent> noises = Core.AudioManager.Instance.GetNoisesInRange(
                transform.position,
                config.hearingRange
            );

            if (noises.Count > 0)
            {
                // Investigate closest noise
                Core.NoiseEvent closestNoise = noises[0];
                float closestDistance = Vector3.Distance(transform.position, closestNoise.position);

                foreach (var noise in noises)
                {
                    float distance = Vector3.Distance(transform.position, noise.position);
                    if (distance < closestDistance)
                    {
                        closestNoise = noise;
                        closestDistance = distance;
                    }
                }

                OnNoiseHeard?.Invoke(closestNoise.position);
            }
        }

        /// <summary>
        /// Updates memory of last known position
        /// </summary>
        private void UpdateMemory()
        {
            // If target is no longer visible and memory has expired
            if (currentTarget != null && TimeSinceLastSeen > config.memoryDuration)
            {
                currentTarget = null;
                OnTargetLost?.Invoke();
            }
        }

        /// <summary>
        /// Manually sets target (used by horde calling)
        /// </summary>
        public void AlertToTarget(Transform target)
        {
            if (target == null)
                return;

            currentTarget = target;
            lastKnownPosition = target.position;
            lastSeenTime = Time.time;

            OnPlayerDetected?.Invoke(target);
        }

        /// <summary>
        /// Checks if zombie can currently see the target
        /// </summary>
        public bool CanSeeTarget(Transform target)
        {
            if (target == null)
                return false;

            Vector3 directionToTarget = (target.position - transform.position).normalized;
            float distance = Vector3.Distance(transform.position, target.position);
            float angle = Vector3.Angle(transform.forward, directionToTarget);

            // Check range and angle
            if (distance > config.visionRange || angle > config.visionAngle / 2f)
                return false;

            // Check line of sight
            Vector3 rayOrigin = transform.position + Vector3.up;
            if (Physics.Raycast(
                rayOrigin,
                directionToTarget,
                out RaycastHit hit,
                distance,
                playerLayer | obstructionLayer
            ))
            {
                return hit.collider.transform == target;
            }

            return false;
        }

        /// <summary>
        /// Gets last known position if still in memory
        /// </summary>
        public Vector3 GetLastKnownPosition()
        {
            if (TimeSinceLastSeen < config.memoryDuration)
            {
                return lastKnownPosition;
            }
            return Vector3.zero;
        }

        /// <summary>
        /// Clears current target and memory
        /// </summary>
        public void ClearTarget()
        {
            currentTarget = null;
            lastKnownPosition = Vector3.zero;
            lastSeenTime = 0f;
        }

        private void OnDrawGizmos()
        {
            if (!showDebugGizmos || config == null)
                return;

            // Draw vision range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, config.visionRange);

            // Draw hearing range
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, config.hearingRange);

            // Draw field of view
            Gizmos.color = Color.green;
            Vector3 leftBoundary = Quaternion.Euler(0, -config.visionAngle / 2f, 0) * transform.forward * config.visionRange;
            Vector3 rightBoundary = Quaternion.Euler(0, config.visionAngle / 2f, 0) * transform.forward * config.visionRange;

            Gizmos.DrawLine(transform.position, transform.position + leftBoundary);
            Gizmos.DrawLine(transform.position, transform.position + rightBoundary);

            // Draw line to current target
            if (currentTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, currentTarget.position);
            }

            // Draw last known position
            if (lastKnownPosition != Vector3.zero)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawSphere(lastKnownPosition, 0.5f);
            }
        }
    }
}
