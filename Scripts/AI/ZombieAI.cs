using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;

namespace ZombieGame.AI
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class ZombieAI : NetworkBehaviour
    {
        [Header("AI Settings")]
        [SerializeField] private ZombieType zombieType = ZombieType.Walker;
        [SerializeField] private float detectionRange = 15f;
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private float loseTargetDistance = 25f;
        [SerializeField] private LayerMask playerMask;

        [Header("Combat Settings")]
        [SerializeField] private float attackDamage = 10f;
        [SerializeField] private float attackCooldown = 1.5f;

        [Header("Wandering")]
        [SerializeField] private float wanderRadius = 20f;
        [SerializeField] private float wanderTimer = 5f;

        private NavMeshAgent navAgent;
        private NetworkVariable<ZombieState> currentState = new NetworkVariable<ZombieState>(ZombieState.Idle);
        private NetworkVariable<ulong> targetPlayerId = new NetworkVariable<ulong>();

        private float lastAttackTime;
        private float wanderTime;
        private Vector3 wanderTarget;

        public event Action<ZombieState> OnStateChanged;

        private void Awake()
        {
            navAgent = GetComponent<NavMeshAgent>();
            ConfigureZombieType();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer)
            {
                ChangeState(ZombieState.Wandering);
            }
        }

        private void Update()
        {
            if (!IsServer) return;

            switch (currentState.Value)
            {
                case ZombieState.Idle:
                    IdleBehavior();
                    break;
                case ZombieState.Wandering:
                    WanderingBehavior();
                    break;
                case ZombieState.Chasing:
                    ChasingBehavior();
                    break;
                case ZombieState.Attacking:
                    AttackingBehavior();
                    break;
            }
        }

        private void IdleBehavior()
        {
            if (FindNearestPlayer(out ulong playerId))
            {
                targetPlayerId.Value = playerId;
                ChangeState(ZombieState.Chasing);
            }
            else
            {
                wanderTime += Time.deltaTime;
                if (wanderTime >= wanderTimer)
                {
                    ChangeState(ZombieState.Wandering);
                }
            }
        }

        private void WanderingBehavior()
        {
            if (FindNearestPlayer(out ulong playerId))
            {
                targetPlayerId.Value = playerId;
                ChangeState(ZombieState.Chasing);
                return;
            }

            if (!navAgent.hasPath || navAgent.remainingDistance < 0.5f)
            {
                Vector3 newPos = RandomNavSphere(transform.position, wanderRadius);
                navAgent.SetDestination(newPos);
            }
        }

        private void ChasingBehavior()
        {
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(targetPlayerId.Value, out var client))
            {
                ChangeState(ZombieState.Wandering);
                return;
            }

            Vector3 targetPos = client.PlayerObject.transform.position;
            float distance = Vector3.Distance(transform.position, targetPos);

            if (distance > loseTargetDistance)
            {
                ChangeState(ZombieState.Wandering);
                return;
            }

            if (distance <= attackRange)
            {
                ChangeState(ZombieState.Attacking);
                return;
            }

            navAgent.SetDestination(targetPos);
        }

        private void AttackingBehavior()
        {
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(targetPlayerId.Value, out var client))
            {
                ChangeState(ZombieState.Wandering);
                return;
            }

            Vector3 targetPos = client.PlayerObject.transform.position;
            float distance = Vector3.Distance(transform.position, targetPos);

            if (distance > attackRange * 1.2f)
            {
                ChangeState(ZombieState.Chasing);
                return;
            }

            navAgent.SetDestination(transform.position);
            transform.LookAt(new Vector3(targetPos.x, transform.position.y, targetPos.z));

            if (Time.time - lastAttackTime >= attackCooldown)
            {
                PerformAttack();
                lastAttackTime = Time.time;
            }
        }

        private void PerformAttack()
        {
            Combat.DamageSystem.Instance?.ProcessDamageServerRpc(
                OwnerClientId,
                targetPlayerId.Value,
                attackDamage,
                Health.DamageType.Physical,
                false,
                $"zombie_{zombieType}"
            );
        }

        private bool FindNearestPlayer(out ulong playerId)
        {
            playerId = 0;
            float nearestDistance = detectionRange;
            bool foundPlayer = false;

            foreach (var client in NetworkManager.Singleton.ConnectedClients.Values)
            {
                if (client.PlayerObject == null) continue;

                float distance = Vector3.Distance(transform.position, client.PlayerObject.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    playerId = client.ClientId;
                    foundPlayer = true;
                }
            }

            return foundPlayer;
        }

        private void ChangeState(ZombieState newState)
        {
            if (currentState.Value == newState) return;

            currentState.Value = newState;
            OnStateChanged?.Invoke(newState);

            switch (newState)
            {
                case ZombieState.Wandering:
                    wanderTime = 0f;
                    break;
            }
        }

        private Vector3 RandomNavSphere(Vector3 origin, float dist)
        {
            Vector3 randDirection = UnityEngine.Random.insideUnitSphere * dist;
            randDirection += origin;

            if (NavMesh.SamplePosition(randDirection, out NavMeshHit navHit, dist, NavMesh.AllAreas))
            {
                return navHit.position;
            }

            return origin;
        }

        private void ConfigureZombieType()
        {
            switch (zombieType)
            {
                case ZombieType.Walker:
                    navAgent.speed = 2f;
                    attackDamage = 10f;
                    detectionRange = 15f;
                    break;
                case ZombieType.Runner:
                    navAgent.speed = 6f;
                    attackDamage = 8f;
                    detectionRange = 20f;
                    break;
                case ZombieType.Tank:
                    navAgent.speed = 1.5f;
                    attackDamage = 25f;
                    detectionRange = 12f;
                    break;
                case ZombieType.Spitter:
                    navAgent.speed = 2.5f;
                    attackDamage = 15f;
                    detectionRange = 25f;
                    attackRange = 10f;
                    break;
                case ZombieType.Exploder:
                    navAgent.speed = 3f;
                    attackDamage = 50f;
                    detectionRange = 18f;
                    break;
            }
        }

        public ZombieState CurrentState => currentState.Value;
        public ZombieType Type => zombieType;
    }

    public enum ZombieState { Idle, Wandering, Chasing, Attacking, Dead }
    public enum ZombieType { Walker, Runner, Tank, Spitter, Exploder }
}
