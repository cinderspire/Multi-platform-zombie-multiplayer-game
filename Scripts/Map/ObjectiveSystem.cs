using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Map
{
    public class ObjectiveSystem : NetworkBehaviour
    {
        public static ObjectiveSystem Instance { get; private set; }

        [SerializeField] private List<Objective> objectives = new List<Objective>();

        private NetworkVariable<int> currentObjectiveIndex = new NetworkVariable<int>(0);

        public event Action<Objective> OnObjectiveStarted;
        public event Action<Objective> OnObjectiveCompleted;
        public event Action OnAllObjectivesCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer && objectives.Count > 0)
            {
                StartObjective(0);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void UpdateObjectiveProgressServerRpc(int objectiveIndex, float progress, ServerRpcParams rpcParams = default)
        {
            if (objectiveIndex < 0 || objectiveIndex >= objectives.Count) return;

            Objective obj = objectives[objectiveIndex];
            obj.currentProgress = Mathf.Clamp01(progress);

            if (obj.currentProgress >= 1f && !obj.isCompleted)
            {
                CompleteObjective(objectiveIndex);
            }

            UpdateObjectiveProgressClientRpc(objectiveIndex, progress);
        }

        private void StartObjective(int index)
        {
            if (index < 0 || index >= objectives.Count) return;

            currentObjectiveIndex.Value = index;
            Objective obj = objectives[index];
            obj.isActive = true;
            OnObjectiveStarted?.Invoke(obj);
            StartObjectiveClientRpc(index);
        }

        private void CompleteObjective(int index)
        {
            if (index < 0 || index >= objectives.Count) return;

            Objective obj = objectives[index];
            obj.isCompleted = true;
            obj.isActive = false;
            OnObjectiveCompleted?.Invoke(obj);
            CompleteObjectiveClientRpc(index);

            // Start next objective
            if (index + 1 < objectives.Count)
            {
                StartObjective(index + 1);
            }
            else
            {
                OnAllObjectivesCompleted?.Invoke();
                AllObjectivesCompletedClientRpc();
            }
        }

        [ClientRpc]
        private void StartObjectiveClientRpc(int index)
        {
            if (index >= 0 && index < objectives.Count)
            {
                OnObjectiveStarted?.Invoke(objectives[index]);
            }
        }

        [ClientRpc]
        private void CompleteObjectiveClientRpc(int index)
        {
            if (index >= 0 && index < objectives.Count)
            {
                OnObjectiveCompleted?.Invoke(objectives[index]);
            }
        }

        [ClientRpc]
        private void UpdateObjectiveProgressClientRpc(int index, float progress)
        {
            if (index >= 0 && index < objectives.Count)
            {
                objectives[index].currentProgress = progress;
            }
        }

        [ClientRpc]
        private void AllObjectivesCompletedClientRpc()
        {
            OnAllObjectivesCompleted?.Invoke();
        }

        public Objective GetCurrentObjective()
        {
            int index = currentObjectiveIndex.Value;
            if (index >= 0 && index < objectives.Count)
            {
                return objectives[index];
            }
            return null;
        }

        public List<Objective> GetAllObjectives() => new List<Objective>(objectives);
    }

    [Serializable]
    public class Objective
    {
        public string objectiveName;
        public string objectiveDescription;
        public ObjectiveType objectiveType;
        public bool isActive;
        public bool isCompleted;
        public float currentProgress;
        public int targetCount;
    }

    public enum ObjectiveType
    {
        KillZombies, Survive, Rescue, Defend, Collect, Reach, Activate
    }
}
