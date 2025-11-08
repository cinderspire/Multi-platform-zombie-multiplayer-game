using UnityEngine;

namespace DeadFrontier.Zombies.States
{
    /// <summary>
    /// Idle state - zombie is stationary
    /// </summary>
    public class IdleState : Core.IState
    {
        private ZombieAI zombie;
        private float idleTimer;
        private float idleDuration;

        public IdleState(ZombieAI zombie)
        {
            this.zombie = zombie;
        }

        public void Enter()
        {
            // Stop movement
            zombie.Agent.isStopped = true;
            zombie.Agent.velocity = Vector3.zero;

            // Play idle animation
            zombie.Animator.SetFloat("Speed", 0f);

            // Random idle duration
            idleDuration = Random.Range(zombie.Config.minIdleTime, zombie.Config.maxIdleTime);
            idleTimer = 0f;

            // Occasionally play idle sound
            if (Random.value < 0.3f && zombie.Config.idleSounds != null && zombie.Config.idleSounds.Length > 0)
            {
                AudioClip sound = zombie.Config.idleSounds[Random.Range(0, zombie.Config.idleSounds.Length)];
                Core.AudioManager.Instance.Play(sound, zombie.transform.position);
            }

            Debug.Log($"[ZombieAI] {zombie.Config.zombieName} entered Idle state");
        }

        public void Update()
        {
            idleTimer += Time.deltaTime;

            // After idle time, start wandering
            if (idleTimer >= idleDuration)
            {
                zombie.StateMachine.ChangeState(new PatrolState(zombie));
            }
        }

        public void Exit()
        {
            zombie.Agent.isStopped = false;
        }
    }
}
