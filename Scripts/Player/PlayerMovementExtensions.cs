using UnityEngine;

namespace DeadFrontier.Player
{
    /// <summary>
    /// Extension methods for PlayerMovement to add missing functionality
    /// </summary>
    public partial class PlayerMovement
    {
        /// <summary>
        /// Restores stamina
        /// </summary>
        public void RestoreStamina(float amount)
        {
            currentStamina = Mathf.Min(currentStamina + amount, maxStamina);
        }

        /// <summary>
        /// Modifies movement speed (for perks)
        /// </summary>
        public void ModifySpeed(float amount, bool isMultiplicative)
        {
            if (isMultiplicative)
            {
                walkSpeed *= amount;
            }
            else
            {
                walkSpeed += amount;
            }

            walkSpeed = Mathf.Max(walkSpeed, 1f); // Minimum speed
        }

        /// <summary>
        /// Modifies sprint speed (for perks)
        /// </summary>
        public void ModifySprintSpeed(float amount, bool isMultiplicative)
        {
            if (isMultiplicative)
            {
                sprintSpeed *= amount;
            }
            else
            {
                sprintSpeed += amount;
            }

            sprintSpeed = Mathf.Max(sprintSpeed, walkSpeed); // Sprint must be >= walk
        }

        /// <summary>
        /// Modifies stamina regeneration (for perks)
        /// </summary>
        public void ModifyStaminaRegen(float amount, bool isMultiplicative)
        {
            if (isMultiplicative)
            {
                staminaRegenRate *= amount;
            }
            else
            {
                staminaRegenRate += amount;
            }

            staminaRegenRate = Mathf.Max(staminaRegenRate, 1f);
        }

        /// <summary>
        /// Gets current movement speed
        /// </summary>
        public float GetCurrentSpeed()
        {
            if (isSprinting && currentStamina > 0f)
                return sprintSpeed;
            else if (isCrouching)
                return crouchSpeed;
            else
                return walkSpeed;
        }

        /// <summary>
        /// Gets current stamina percentage
        /// </summary>
        public float GetStaminaPercentage()
        {
            return maxStamina > 0f ? currentStamina / maxStamina : 0f;
        }

        /// <summary>
        /// Checks if player can sprint
        /// </summary>
        public bool CanSprint()
        {
            return currentStamina > 0f && !isCrouching && isGrounded;
        }
    }
}
