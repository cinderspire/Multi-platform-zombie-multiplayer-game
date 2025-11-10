using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;

namespace ZombieGame.AI
{
    public class PathfindingSystem : NetworkBehaviour
    {
        public static PathfindingSystem Instance { get; private set; }

        [SerializeField] private float pathUpdateInterval = 0.5f;

        private Dictionary<ulong, NavMeshPath> entityPaths = new Dictionary<ulong, NavMeshPath>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public bool CalculatePath(Vector3 start, Vector3 end, out NavMeshPath path)
        {
            path = new NavMeshPath();
            return NavMesh.CalculatePath(start, end, NavMesh.AllAreas, path);
        }

        public Vector3 GetRandomPointInRadius(Vector3 center, float radius)
        {
            Vector3 randomPoint = center + Random.insideUnitSphere * radius;
            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, radius, NavMesh.AllAreas))
            {
                return hit.position;
            }
            return center;
        }

        public bool IsReachable(Vector3 start, Vector3 end)
        {
            NavMeshPath path = new NavMeshPath();
            if (NavMesh.CalculatePath(start, end, NavMesh.AllAreas, path))
            {
                return path.status == NavMeshPathStatus.PathComplete;
            }
            return false;
        }
    }
}
