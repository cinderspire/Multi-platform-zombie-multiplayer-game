using UnityEngine;

namespace DeadFrontier.Zombies.States
{
    /// <summary>
    /// Attack state - zombie attacks the target
    /// </summary>
    public class AttackState : Core.IState
    {
        private ZombieAI zombie;
        private Transform target;
        private float attackCooldownTimer;

        public AttackState(ZombieAI zombie, Transform target)
        {
            this.zombie = zombie;
            this.target = target;
        }

        public void Enter()
        {
            // Stop movement
            zombie.Agent.isStopped = true;
            zombie.Agent.velocity = Vector3.zero;

            // Play idle animation (will transition to attack)
            zombie.Animator.SetFloat("Speed", 0f);

            attackCooldownTimer = 0f;

            Debug.Log($"[ZombieAI] {zombie.Config.zombieName} entered Attack state");
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

            // Check if target moved out of range
            if (distanceToTarget > zombie.Config.attackRange * 1.2f)
            {
                zombie.StateMachine.ChangeState(new ChaseState(zombie, target));
                return;
            }

            // Face the target
            Vector3 direction = (target.position - zombie.transform.position).normalized;
            direction.y = 0f; // Keep on horizontal plane

            if (direction != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                zombie.transform.rotation = Quaternion.Slerp(
                    zombie.transform.rotation,
                    lookRotation,
                    Time.deltaTime * 5f
                );
            }

            // Attack cooldown
            attackCooldownTimer -= Time.deltaTime;

            if (attackCooldownTimer <= 0f)
            {
                PerformAttack();
                attackCooldownTimer = zombie.Config.attackCooldown;
            }
        }

        public void Exit()
        {
            zombie.Agent.isStopped = false;
        }

        private void PerformAttack()
        {
            // Play attack animation
            zombie.Animator.SetTrigger("Attack");

            // Play attack sound
            if (zombie.Config.attackSounds != null && zombie.Config.attackSounds.Length > 0)
            {
                AudioClip sound = zombie.Config.attackSounds[Random.Range(0, zombie.Config.attackSounds.Length)];
                Core.AudioManager.Instance.Play(sound, zombie.transform.position);
            }

            // Deal damage to target
            DealDamage();

            Debug.Log($"[ZombieAI] {zombie.Config.zombieName} attacked!");
        }

        private void DealDamage()
        {
            // Check if target is still in range (might have moved during animation)
            float distanceToTarget = Vector3.Distance(zombie.transform.position, target.position);

            if (distanceToTarget <= zombie.Config.attackRange)
            {
                var damageable = target.GetComponent<Core.IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(zombie.Config.damage);
                    Debug.Log($"[ZombieAI] Dealt {zombie.Config.damage} damage to {target.name}");
                }
            }
        }
    }

    /// <summary>
    /// Death state - zombie is dead
    /// </summary>
    public class DeathState : Core.IState
    {
        private ZombieAI zombie;

        public DeathState(ZombieAI zombie)
        {
            this.zombie = zombie;
        }

        public void Enter()
        {
            // Disable NavMesh agent
            zombie.Agent.enabled = false;

            // Play death animation
            zombie.Animator.SetTrigger("Die");

            // Stop all movement
            zombie.enabled = false;

            Debug.Log($"[ZombieAI] {zombie.Config.zombieName} died");
        }

        public void Update()
        {
            // Dead zombies don't update
        }

        public void Exit()
        {
            // Nothing to clean up
        }
    }
}
