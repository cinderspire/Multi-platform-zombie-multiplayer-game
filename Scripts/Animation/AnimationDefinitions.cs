using UnityEngine;
using System;
using System.Collections.Generic;

namespace DeadFrontier.Animation
{
    /// <summary>
    /// Animation parameter and state definitions for all characters.
    /// </summary>
    public static class AnimationDefinitions
    {
        #region Parameter Names

        public static class Parameters
        {
            // Movement
            public const string Speed = "Speed";
            public const string VelocityX = "VelocityX";
            public const string VelocityZ = "VelocityZ";
            public const string MoveSpeed = "MoveSpeed";
            public const string TurnSpeed = "TurnSpeed";
            public const string IsMoving = "IsMoving";
            public const string IsSprinting = "IsSprinting";
            public const string IsCrouching = "IsCrouching";
            public const string IsProne = "IsProne";
            public const string IsGrounded = "IsGrounded";
            public const string IsJumping = "IsJumping";
            public const string IsFalling = "IsFalling";
            public const string IsClimbing = "IsClimbing";
            public const string IsSwimming = "IsSwimming";

            // Combat
            public const string IsAiming = "IsAiming";
            public const string IsFiring = "IsFiring";
            public const string IsReloading = "IsReloading";
            public const string WeaponType = "WeaponType";
            public const string FireMode = "FireMode";
            public const string MeleeAttack = "MeleeAttack";
            public const string MeleeCombo = "MeleeCombo";
            public const string ThrowGrenade = "ThrowGrenade";

            // State
            public const string Health = "Health";
            public const string IsDead = "IsDead";
            public const string IsDown = "IsDown";
            public const string IsReviving = "IsReviving";
            public const string IsBeingRevived = "IsBeingRevived";
            public const string IsInteracting = "IsInteracting";
            public const string IsUsingAbility = "IsUsingAbility";
            public const string AbilityIndex = "AbilityIndex";

            // Look
            public const string LookAngle = "LookAngle";
            public const string AimAngle = "AimAngle";
            public const string HeadLookX = "HeadLookX";
            public const string HeadLookY = "HeadLookY";

            // Zombie Specific
            public const string IsAlerted = "IsAlerted";
            public const string IsChasing = "IsChasing";
            public const string IsAttacking = "IsAttacking";
            public const string AttackType = "AttackType";
            public const string IsStunned = "IsStunned";
            public const string RageMode = "RageMode";

            // Triggers
            public const string TriggerJump = "Jump";
            public const string TriggerLand = "Land";
            public const string TriggerDeath = "Death";
            public const string TriggerHit = "Hit";
            public const string TriggerReload = "Reload";
            public const string TriggerFire = "Fire";
            public const string TriggerMelee = "Melee";
            public const string TriggerInteract = "Interact";
            public const string TriggerAbility = "Ability";
            public const string TriggerEmote = "Emote";
            public const string TriggerRevive = "Revive";
            public const string TriggerAlert = "Alert";
            public const string TriggerAttack = "Attack";
            public const string TriggerScream = "Scream";
        }

        #endregion

        #region State Names

        public static class States
        {
            // Locomotion
            public const string Idle = "Idle";
            public const string Walk = "Walk";
            public const string Run = "Run";
            public const string Sprint = "Sprint";
            public const string CrouchIdle = "CrouchIdle";
            public const string CrouchWalk = "CrouchWalk";
            public const string ProneIdle = "ProneIdle";
            public const string ProneCrawl = "ProneCrawl";

            // Jumping/Falling
            public const string JumpStart = "JumpStart";
            public const string JumpAir = "JumpAir";
            public const string JumpLand = "JumpLand";
            public const string FallStart = "FallStart";
            public const string FallLoop = "FallLoop";
            public const string FallLand = "FallLand";
            public const string FallHardLand = "FallHardLand";

            // Combat
            public const string AimIdle = "AimIdle";
            public const string AimWalk = "AimWalk";
            public const string Fire = "Fire";
            public const string FireAuto = "FireAuto";
            public const string Reload = "Reload";
            public const string ReloadTactical = "ReloadTactical";
            public const string MeleeSlash = "MeleeSlash";
            public const string MeleeStab = "MeleeStab";
            public const string MeleeHeavy = "MeleeHeavy";
            public const string ThrowPrepare = "ThrowPrepare";
            public const string ThrowRelease = "ThrowRelease";

            // Weapon Handling
            public const string WeaponDraw = "WeaponDraw";
            public const string WeaponHolster = "WeaponHolster";
            public const string WeaponSwitch = "WeaponSwitch";
            public const string WeaponInspect = "WeaponInspect";

            // Interaction
            public const string InteractStart = "InteractStart";
            public const string InteractLoop = "InteractLoop";
            public const string InteractEnd = "InteractEnd";
            public const string PickupItem = "PickupItem";
            public const string UseMedkit = "UseMedkit";
            public const string ReviveAlly = "ReviveAlly";

            // Death/Down
            public const string DeathFront = "DeathFront";
            public const string DeathBack = "DeathBack";
            public const string DeathLeft = "DeathLeft";
            public const string DeathRight = "DeathRight";
            public const string DeathHeadshot = "DeathHeadshot";
            public const string DeathExplosion = "DeathExplosion";
            public const string DownedEnter = "DownedEnter";
            public const string DownedIdle = "DownedIdle";
            public const string DownedCrawl = "DownedCrawl";
            public const string ReviveReceive = "ReviveReceive";

            // Hit Reactions
            public const string HitFront = "HitFront";
            public const string HitBack = "HitBack";
            public const string HitLeft = "HitLeft";
            public const string HitRight = "HitRight";
            public const string Stagger = "Stagger";
            public const string Knockdown = "Knockdown";

            // Zombie States
            public const string ZombieIdle = "ZombieIdle";
            public const string ZombieWander = "ZombieWander";
            public const string ZombieAlert = "ZombieAlert";
            public const string ZombieChase = "ZombieChase";
            public const string ZombieAttackSwipe = "ZombieAttackSwipe";
            public const string ZombieAttackBite = "ZombieAttackBite";
            public const string ZombieAttackGrab = "ZombieAttackGrab";
            public const string ZombieScream = "ZombieScream";
            public const string ZombieFeed = "ZombieFeed";
            public const string ZombieStunned = "ZombieStunned";
            public const string ZombieDeath = "ZombieDeath";
        }

        #endregion

        #region Layer Names

        public static class Layers
        {
            public const string BaseLayer = "Base Layer";
            public const string UpperBody = "Upper Body";
            public const string Arms = "Arms";
            public const string Head = "Head";
            public const string Additive = "Additive";
            public const string Override = "Override";
        }

        #endregion

        #region Animation Events

        public static class Events
        {
            public const string FootstepLeft = "FootstepLeft";
            public const string FootstepRight = "FootstepRight";
            public const string WeaponFire = "WeaponFire";
            public const string WeaponEjectShell = "WeaponEjectShell";
            public const string WeaponMagOut = "WeaponMagOut";
            public const string WeaponMagIn = "WeaponMagIn";
            public const string WeaponChamber = "WeaponChamber";
            public const string WeaponReloadComplete = "WeaponReloadComplete";
            public const string MeleeHitWindow = "MeleeHitWindow";
            public const string MeleeImpact = "MeleeImpact";
            public const string ThrowRelease = "ThrowRelease";
            public const string AbilityTrigger = "AbilityTrigger";
            public const string VoiceLine = "VoiceLine";
            public const string ImpactSound = "ImpactSound";
        }

        #endregion

        #region Weapon Type IDs

        public static class WeaponTypeIDs
        {
            public const int Unarmed = 0;
            public const int Pistol = 1;
            public const int Revolver = 2;
            public const int SMG = 3;
            public const int AssaultRifle = 4;
            public const int Shotgun = 5;
            public const int SniperRifle = 6;
            public const int LMG = 7;
            public const int Launcher = 8;
            public const int MeleeOneHand = 9;
            public const int MeleeTwoHand = 10;
            public const int Throwable = 11;
        }

        #endregion
    }

    /// <summary>
    /// Animation configuration for player characters.
    /// </summary>
    [CreateAssetMenu(fileName = "New Player Animation Config", menuName = "Dead Frontier/Animation/Player Config")]
    public class PlayerAnimationConfig : ScriptableObject
    {
        [Header("Blend Trees")]
        public float locomotionBlendTime = 0.2f;
        public float aimTransitionTime = 0.15f;
        public float crouchTransitionTime = 0.25f;
        public float proneTransitionTime = 0.4f;

        [Header("IK Settings")]
        public bool useFootIK = true;
        public bool useHandIK = true;
        public bool useLookIK = true;
        public float footIKWeight = 1f;
        public float handIKWeight = 1f;
        public float lookIKWeight = 0.7f;
        public float lookIKClampWeight = 0.5f;

        [Header("Aim Settings")]
        public float aimLayerWeight = 1f;
        public float upperBodyWeight = 0.7f;
        public float spineRotationWeight = 0.5f;
        public float maxAimAngle = 80f;

        [Header("Hit Reactions")]
        public float hitReactionDuration = 0.3f;
        public float hitReactionWeight = 0.5f;
        public bool useAdditiveHitReaction = true;

        [Header("Physics")]
        public bool useRagdollOnDeath = true;
        public float ragdollBlendTime = 0.3f;
        public float ragdollForceMultiplier = 10f;
    }

    /// <summary>
    /// Animation configuration for zombie characters.
    /// </summary>
    [CreateAssetMenu(fileName = "New Zombie Animation Config", menuName = "Dead Frontier/Animation/Zombie Config")]
    public class ZombieAnimationConfig : ScriptableObject
    {
        [Header("Movement")]
        public float walkSpeed = 1f;
        public float runSpeed = 1.5f;
        public float sprintSpeed = 2f;
        public float rotationSpeed = 5f;

        [Header("Attack")]
        public AnimationClip[] attackAnimations;
        public float attackTransitionTime = 0.1f;
        public float attackAnimationSpeed = 1f;

        [Header("Variations")]
        public float idleVariationChance = 0.3f;
        public float moveAnimationVariation = 0.2f;

        [Header("Ragdoll")]
        public bool useRagdollOnDeath = true;
        public float ragdollDuration = 5f;
        public float ragdollFadeTime = 1f;

        [Header("Special")]
        public bool hasSpecialAnimations;
        public AnimationClip specialAbilityAnimation;
        public AnimationClip rageAnimation;
    }

    /// <summary>
    /// Animation clip reference with metadata.
    /// </summary>
    [Serializable]
    public class AnimationClipData
    {
        public string clipId;
        public AnimationClip clip;
        public float speed = 1f;
        public bool mirror;
        public bool loop;
        public float transitionDuration = 0.25f;
        public AvatarMask avatarMask;
        public AnimationEvent[] customEvents;
    }

    /// <summary>
    /// Animation state machine configuration.
    /// </summary>
    [CreateAssetMenu(fileName = "New State Machine Config", menuName = "Dead Frontier/Animation/State Machine Config")]
    public class StateMachineConfig : ScriptableObject
    {
        public string stateMachineName;
        public AnimationStateData[] states;
        public AnimationTransitionData[] transitions;
        public string defaultState;
    }

    [Serializable]
    public class AnimationStateData
    {
        public string stateName;
        public AnimationClip clip;
        public BlendTree blendTree;
        public bool isLooping = true;
        public float speed = 1f;
        public StateType stateType;
    }

    [Serializable]
    public class AnimationTransitionData
    {
        public string fromState;
        public string toState;
        public string conditionParameter;
        public ConditionMode conditionMode;
        public float threshold;
        public float duration = 0.25f;
        public float exitTime = 0.9f;
        public bool hasExitTime = true;
    }

    public enum StateType
    {
        Normal,
        Entry,
        Exit,
        AnyState
    }

    public enum ConditionMode
    {
        If,
        IfNot,
        Greater,
        Less,
        Equals,
        NotEquals
    }

    [Serializable]
    public class BlendTree
    {
        public string blendTreeName;
        public BlendTreeType blendType;
        public string blendParameterX;
        public string blendParameterY;
        public BlendTreeChild[] children;
    }

    [Serializable]
    public class BlendTreeChild
    {
        public AnimationClip clip;
        public Vector2 position;
        public float threshold;
        public float speed = 1f;
    }

    public enum BlendTreeType
    {
        Simple1D,
        SimpleDirectional2D,
        FreeformDirectional2D,
        FreeformCartesian2D
    }
}
