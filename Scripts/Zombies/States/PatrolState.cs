using UnityEngine;

namespace DeadFrontier.Zombies.States
{
    /// <summary>
    /// Patrol state - zombie wanders around randomly
    /// </summary>
    public class PatrolState : Core.IState
    {
        private ZombieAI zombie;
        private Vector3 targetPosition;
        private float stuckTimer;
        private Vector3 lastPosition;

        public PatrolState(ZombieAI zombie)
        {
            this.zombie = zombie;
        }

        public void Enter()
        {
            // Set patrol speed
            zombie.Agent.speed = zombie.Config.moveSpeed;
            zombie.Agent.isStopped = false;

            // Play walk animation
            zombie.Animator.SetFloat("Speed", 1f);

            // Choose random wander point
            ChooseNewWanderPoint();

            Debug.Log($"[ZombieAI] {zombie.Config.zombieName} entered Patrol state");
        }

        public void Update()
        {
            // Check if reached destination
            if (!zombie.Agent.pathPending && zombie.Agent.remainingDistance <= zombie.Agent.stoppingDistance)
            {
                // Go back to idle
                zombie.StateMachine.ChangeState(new IdleState(zombie));
                return;
            }

            // Check for stuck (not moving)
            DetectStuck();
        }

        public void Exit()
        {
            // Nothing specific to clean up
        }

        private void ChooseNewWanderPoint()
        {
            // Random point within wander radius
            Vector2 randomCircle = Random.insideUnitCircle * zombie.Config.wanderRadius;
            Vector3 randomPoint = zombie.transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

            // Sample NavMesh to find valid position
            if (UnityEngine.AI.NavMesh.SamplePosition(randomPoint, out UnityEngine.AI.NavMeshHit hit, zombie.Config.wanderRadius, UnityEngine.AI.NavMesh.AllAreas))
            {
                targetPosition = hit.position;
                zombie.Agent.SetDestination(targetPosition);
            }
            else
            {
                // Couldn't find valid point, go to idle
                zombie.StateMachine.ChangeState(new IdleState(zombie));
            }
        }

        private void DetectStuck()
        {
            // Check if zombie hasn't moved much
            if (Vector3.Distance(zombie.transform.position, lastPosition) < 0.1f)
            {
                stuckTimer += Time.deltaTime;

                if (stuckTimer > 3f)
                {
                    // Stuck for too long, choose new point or go idle
                    if (Random.value < 0.5f)
                    {
                        ChooseNewWanderPoint();
                    }
                    else
                    {
                        zombie.StateMachine.ChangeState(new IdleState(zombie));
                    }
                    stuckTimer = 0f;
                }
            }
            else
            {
                stuckTimer = 0f;
            }

            lastPosition = zombie.transform.position;
        }
    }
}
