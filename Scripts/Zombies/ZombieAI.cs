using UnityEngine;
using UnityEngine.AI;

namespace DeadFrontier.Zombies
{
    /// <summary>
    /// Main zombie AI controller that orchestrates all zombie components
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(ZombieHealth))]
    [RequireComponent(typeof(ZombieSensors))]
    public class ZombieAI : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private ZombieConfig config;

        [Header("Components")]
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private ZombieHealth health;
        [SerializeField] private ZombieSensors sensors;
        [SerializeField] private Animator animator;

        // State machine
        private Core.StateMachine stateMachine;

        // Properties
        public ZombieConfig Config => config;
        public NavMeshAgent Agent => agent;
        public ZombieHealth Health => health;
        public ZombieSensors Sensors => sensors;
        public Animator Animator => animator;
        public Core.StateMachine StateMachine => stateMachine;

        private void Awake()
        {
            // Get components
            agent = GetComponent<NavMeshAgent>();
            health = GetComponent<ZombieHealth>();
            sensors = GetComponent<ZombieSensors>();
            animator = GetComponent<Animator>();

            // Validate components
            if (agent == null || health == null || sensors == null)
            {
                Debug.LogError("[ZombieAI] Missing required components!");
                enabled = false;
                return;
            }

            // Initialize state machine
            stateMachine = new Core.StateMachine();
        }

        private void Start()
        {
            // Apply configuration to components
            if (config != null)
            {
                health.SetConfig(config);
                sensors.SetConfig(config);

                if (animator != null && config.animatorController != null)
                {
                    animator.runtimeAnimatorController = config.animatorController;
                }
            }
            else
            {
                Debug.LogWarning("[ZombieAI] No configuration assigned!");
            }

            // Subscribe to events
            sensors.OnPlayerDetected += HandlePlayerDetected;
            sensors.OnNoiseHeard += HandleNoiseHeard;
            sensors.OnTargetLost += HandleTargetLost;
            health.OnDeath += HandleDeath;

            // Start in idle state
            stateMachine.ChangeState(new States.IdleState(this));
        }

        private void Update()
        {
            // Only run AI on server (in multiplayer)
            // For now, run locally
            // TODO: Add IsServer check for networking

            // Update sensors
            sensors.Sense();

            // Update state machine
            stateMachine.Update();
        }

        #region Event Handlers

        private void HandlePlayerDetected(Transform player)
        {
            // Only react if not already chasing or attacking
            if (!stateMachine.IsInState<States.ChaseState>() && !stateMachine.IsInState<States.AttackState>())
            {
                stateMachine.ChangeState(new States.ChaseState(this, player));
            }
        }

        private void HandleNoiseHeard(Vector3 noisePosition)
        {
            // Only investigate if idle or patrolling
            if (stateMachine.IsInState<States.IdleState>() || stateMachine.IsInState<States.PatrolState>())
            {
                stateMachine.ChangeState(new States.InvestigateState(this, noisePosition));
            }
        }

        private void HandleTargetLost()
        {
            // Target lost, investigate last known position or return to idle
            Vector3 lastKnownPos = sensors.GetLastKnownPosition();

            if (lastKnownPos != Vector3.zero)
            {
                stateMachine.ChangeState(new States.InvestigateState(this, lastKnownPos));
            }
            else
            {
                stateMachine.ChangeState(new States.IdleState(this));
            }
        }

        private void HandleDeath()
        {
            // Enter death state
            stateMachine.ChangeState(new States.DeathState(this));

            // TODO: Award XP to killer
            // TODO: Spawn loot
        }

        #endregion

        /// <summary>
        /// Sets the zombie configuration
        /// </summary>
        public void SetConfig(ZombieConfig newConfig)
        {
            config = newConfig;

            if (health != null)
                health.SetConfig(newConfig);

            if (sensors != null)
                sensors.SetConfig(newConfig);

            if (animator != null && newConfig.animatorController != null)
                animator.runtimeAnimatorController = newConfig.animatorController;
        }

        /// <summary>
        /// Manually alerts zombie to a target
        /// </summary>
        public void AlertToTarget(Transform target)
        {
            sensors.AlertToTarget(target);
            stateMachine.ChangeState(new States.ChaseState(this, target));
        }

        /// <summary>
        /// Resets zombie to initial state
        /// </summary>
        public void ResetZombie()
        {
            health.Reset();
            sensors.ClearTarget();
            stateMachine.ChangeState(new States.IdleState(this));
            agent.enabled = true;
            enabled = true;
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (sensors != null)
            {
                sensors.OnPlayerDetected -= HandlePlayerDetected;
                sensors.OnNoiseHeard -= HandleNoiseHeard;
                sensors.OnTargetLost -= HandleTargetLost;
            }

            if (health != null)
            {
                health.OnDeath -= HandleDeath;
            }
        }

        private void OnValidate()
        {
            // Auto-assign components in editor
            if (agent == null)
                agent = GetComponent<NavMeshAgent>();
            if (health == null)
                health = GetComponent<ZombieHealth>();
            if (sensors == null)
                sensors = GetComponent<ZombieSensors>();
            if (animator == null)
                animator = GetComponent<Animator>();
        }
    }
}
