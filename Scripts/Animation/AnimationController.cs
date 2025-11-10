using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Animation
{
    [RequireComponent(typeof(Animator))]
    public class AnimationController : NetworkBehaviour
    {
        private Animator animator;
        private Player.PlayerController playerController;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            playerController = GetComponentInParent<Player.PlayerController>();
        }

        private void Update()
        {
            if (!IsOwner) return;
            UpdateAnimationStates();
        }

        private void UpdateAnimationStates()
        {
            if (playerController == null || animator == null) return;

            // Movement
            float speed = playerController.CurrentSpeed;
            animator.SetFloat("Speed", speed);

            // States
            animator.SetBool("IsGrounded", playerController.IsGrounded);
            animator.SetBool("IsSprinting", playerController.IsSprinting);
            animator.SetBool("IsCrouching", playerController.IsCrouching);

            // Vertical velocity
            animator.SetFloat("VerticalVelocity", playerController.Velocity.y);
        }

        public void PlayAnimation(string animationName)
        {
            animator?.Play(animationName);
        }

        public void SetAnimatorBool(string paramName, bool value)
        {
            animator?.SetBool(paramName, value);
        }

        public void SetAnimatorFloat(string paramName, float value)
        {
            animator?.SetFloat(paramName, value);
        }

        public void TriggerAnimation(string triggerName)
        {
            animator?.SetTrigger(triggerName);
        }
    }
}
