using UnityEngine;

namespace DeadFrontier.Zombies.States
{
    /// <summary>
    /// Chase state - zombie pursues a target
    /// </summary>
    public class ChaseState : Core.IState
    {
        private ZombieAI zombie;
        private Transform target;
        private float lostTargetTimer;
        private float updatePathTimer;
        private const float PATH_UPDATE_INTERVAL = 0.2f; // Update path 5 times per second

        public ChaseState(ZombieAI zombie, Transform target)
        {
            this.zombie = zombie;
            this.target = target;
        }

        public void Enter()
        {
            // Set chase speed
            zombie.Agent.speed = zombie.Config.chaseSpeed;
            zombie.Agent.isStopped = false;

            // Play run animation
            zombie.Animator.SetFloat("Speed", 2f);

            // Play chase sound
            if (zombie.Config.chaseSounds != null && zombie.Config.chaseSounds.Length > 0)
            {
                AudioClip sound = zombie.Config.chaseSounds[Random.Range(0, zombie.Config.chaseSounds.Length)];
                Core.AudioManager.Instance.Play(sound, zombie.transform.position);
            }

            // Call nearby zombies to join
            CallHorde();

            Debug.Log($"[ZombieAI] {zombie.Config.zombieName} entered Chase state");
        }

        public void Update()
        {
            // Check if target is still valid
            if (target == null || !target.gameObject.activeInHierarchy)
            {
                zombie.StateMachine.ChangeState(new IdleState(zombie));
                return;
            }

            float distanceToTarget = Vector3.Distance(zombie.transform.position, target.position);

            // Check if in attack range
            if (distanceToTarget <= zombie.Config.attackRange)
            {
                zombie.StateMachine.ChangeState(new AttackState(zombie, target));
                return;
            }

            // Update path periodically (not every frame for performance)
            updatePathTimer += Time.deltaTime;
            if (updatePathTimer >= PATH_UPDATE_INTERVAL)
            {
                zombie.Agent.SetDestination(target.position);
                updatePathTimer = 0f;
            }

            // Check if can still see target
            if (!zombie.Sensors.CanSeeTarget(target))
            {
                lostTargetTimer += Time.deltaTime;

                // Lost sight for too long
                if (lostTargetTimer > 3f)
                {
                    Vector3 lastKnownPos = zombie.Sensors.GetLastKnownPosition();

                    if (lastKnownPos != Vector3.zero)
                    {
                        // Go to last known position
                        zombie.StateMachine.ChangeState(new InvestigateState(zombie, lastKnownPos));
                    }
                    else
                    {
                        // No last known position, return to idle
                        zombie.StateMachine.ChangeState(new IdleState(zombie));
                    }
                }
            }
            else
            {
                // Can see target, reset timer
                lostTargetTimer = 0f;
            }
        }

        public void Exit()
        {
            // Nothing specific to clean up
        }

        private void CallHorde()
        {
            if (!zombie.Config.canCallHorde)
                return;

            // Find nearby zombies and alert them
            Collider[] zombies = Physics.OverlapSphere(
                zombie.transform.position,
                zombie.Config.callHordeRadius,
                LayerMask.GetMask("Zombie")
            );

            foreach (var col in zombies)
            {
                ZombieAI ai = col.GetComponent<ZombieAI>();
                if (ai != null && ai != zombie && !ai.Sensors.HasTarget)
                {
                    ai.Sensors.AlertToTarget(target);
                }
            }
        }
    }

    /// <summary>
    /// Investigate state - zombie checks last known position
    /// </summary>
    public class InvestigateState : Core.IState
    {
        private ZombieAI zombie;
        private Vector3 investigatePosition;
        private float investigateTimer;
        private const float INVESTIGATE_DURATION = 5f;

        public InvestigateState(ZombieAI zombie, Vector3 position)
        {
            this.zombie = zombie;
            this.investigatePosition = position;
        }

        public void Enter()
        {
            zombie.Agent.speed = zombie.Config.moveSpeed;
            zombie.Agent.isStopped = false;
            zombie.Agent.SetDestination(investigatePosition);
            zombie.Animator.SetFloat("Speed", 1f);

            investigateTimer = 0f;

            Debug.Log($"[ZombieAI] {zombie.Config.zombieName} investigating position");
        }

        public void Update()
        {
            investigateTimer += Time.deltaTime;

            // Check if reached investigation point
            if (!zombie.Agent.pathPending && zombie.Agent.remainingDistance <= zombie.Agent.stoppingDistance)
            {
                // Reached point, look around for a bit
                if (investigateTimer > INVESTIGATE_DURATION)
                {
                    zombie.StateMachine.ChangeState(new IdleState(zombie));
                }
            }
            else if (investigateTimer > INVESTIGATE_DURATION * 2f)
            {
                // Took too long to reach, give up
                zombie.StateMachine.ChangeState(new IdleState(zombie));
            }
        }

        public void Exit()
        {
            // Nothing specific
        }
    }
}
