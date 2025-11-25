using UnityEngine;
using UnityEngine.Audio;
using System;
using System.Collections.Generic;

namespace DeadFrontier.Audio
{
    /// <summary>
    /// Comprehensive audio configuration for all game sounds.
    /// </summary>
    [CreateAssetMenu(fileName = "Audio Configuration", menuName = "Dead Frontier/Audio/Configuration")]
    public class AudioConfiguration : ScriptableObject
    {
        [Header("Audio Mixer")]
        public AudioMixer masterMixer;
        public AudioMixerGroup masterGroup;
        public AudioMixerGroup musicGroup;
        public AudioMixerGroup sfxGroup;
        public AudioMixerGroup voiceGroup;
        public AudioMixerGroup ambientGroup;
        public AudioMixerGroup uiGroup;

        [Header("Mixer Parameters")]
        public string masterVolumeParam = "MasterVolume";
        public string musicVolumeParam = "MusicVolume";
        public string sfxVolumeParam = "SFXVolume";
        public string voiceVolumeParam = "VoiceVolume";
        public string ambientVolumeParam = "AmbientVolume";

        [Header("Snapshots")]
        public AudioMixerSnapshot defaultSnapshot;
        public AudioMixerSnapshot pausedSnapshot;
        public AudioMixerSnapshot underwaterSnapshot;
        public AudioMixerSnapshot lowHealthSnapshot;
        public AudioMixerSnapshot indoorSnapshot;

        [Header("Sound Banks")]
        public WeaponSoundBank[] weaponSounds;
        public ZombieSoundBank[] zombieSounds;
        public PlayerSoundBank playerSounds;
        public AmbientSoundBank ambientSounds;
        public UISoundBank uiSounds;
        public MusicTrackBank musicTracks;

        [Header("3D Audio Settings")]
        public float defaultMaxDistance = 50f;
        public float defaultMinDistance = 1f;
        public AnimationCurve distanceAttenuation;
        public AnimationCurve spatialBlendCurve;
        public float dopplerLevel = 0.5f;
        public AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;

        [Header("Pooling")]
        public int audioSourcePoolSize = 50;
        public int musicSourcePoolSize = 3;
        public int voiceSourcePoolSize = 10;

        [Header("Optimization")]
        public int maxConcurrentSounds = 32;
        public float soundCullingDistance = 100f;
        public bool enableOcclusion = true;
        public LayerMask occlusionLayers;
        public float occlusionDampening = 0.5f;
    }

    #region Sound Banks

    [Serializable]
    public class WeaponSoundBank
    {
        public string weaponId;
        public string weaponName;

        [Header("Fire Sounds")]
        public AudioClip[] fireSounds;
        public AudioClip[] fireSoundsSuppressed;
        public AudioClip[] fireDistantSounds;
        public float fireVolume = 1f;
        public float firePitch = 1f;
        public float firePitchVariation = 0.05f;

        [Header("Mechanical Sounds")]
        public AudioClip[] reloadSounds;
        public AudioClip magazineOutSound;
        public AudioClip magazineInSound;
        public AudioClip chamberSound;
        public AudioClip boltSound;
        public AudioClip emptyClickSound;

        [Header("Handling Sounds")]
        public AudioClip equipSound;
        public AudioClip holsterSound;
        public AudioClip aimInSound;
        public AudioClip aimOutSound;
        public AudioClip inspectSound;

        [Header("Shell Sounds")]
        public AudioClip[] shellDropSounds;
        public float shellDropDelay = 0.3f;

        [Header("Attachment Sounds")]
        public AudioClip attachmentEquipSound;
        public AudioClip attachmentRemoveSound;
        public AudioClip flashlightToggleSound;
        public AudioClip laserToggleSound;
    }

    [Serializable]
    public class ZombieSoundBank
    {
        public string zombieTypeId;
        public string zombieTypeName;

        [Header("Idle Sounds")]
        public AudioClip[] idleSounds;
        public float idleMinInterval = 3f;
        public float idleMaxInterval = 8f;
        public float idleVolume = 0.7f;

        [Header("Alert Sounds")]
        public AudioClip[] alertSounds;
        public AudioClip[] investigateSounds;
        public float alertVolume = 0.9f;

        [Header("Chase Sounds")]
        public AudioClip[] chaseSounds;
        public AudioClip[] aggroSounds;
        public float chaseVolume = 1f;

        [Header("Attack Sounds")]
        public AudioClip[] attackSounds;
        public AudioClip[] attackHitSounds;
        public AudioClip[] attackMissSounds;
        public float attackVolume = 1f;

        [Header("Pain Sounds")]
        public AudioClip[] hurtSounds;
        public AudioClip[] staggerSounds;
        public float hurtVolume = 0.8f;

        [Header("Death Sounds")]
        public AudioClip[] deathSounds;
        public AudioClip[] deathHeadshotSounds;
        public float deathVolume = 1f;

        [Header("Special Sounds")]
        public AudioClip[] specialAbilitySounds;
        public AudioClip[] screamSounds;
        public AudioClip explosionSound;
        public float specialVolume = 1f;

        [Header("Footstep Sounds")]
        public AudioClip[] footstepSounds;
        public float footstepVolume = 0.5f;
    }

    [Serializable]
    public class PlayerSoundBank
    {
        [Header("Movement")]
        public FootstepSoundSet[] footstepSets;
        public AudioClip[] jumpSounds;
        public AudioClip[] landSounds;
        public AudioClip[] landHardSounds;
        public AudioClip[] mantleSounds;
        public AudioClip[] slideSounds;

        [Header("Voice")]
        public AudioClip[] hurtSounds;
        public AudioClip[] hurtHeavySounds;
        public AudioClip[] deathSounds;
        public AudioClip[] reviveSounds;
        public AudioClip[] effortSounds;
        public AudioClip[] calloutSounds;

        [Header("Breathing")]
        public AudioClip normalBreathing;
        public AudioClip heavyBreathing;
        public AudioClip exhaustedBreathing;
        public AudioClip lowHealthBreathing;
        public AudioClip underwaterBreathing;

        [Header("Interaction")]
        public AudioClip pickupSound;
        public AudioClip dropSound;
        public AudioClip healSound;
        public AudioClip useItemSound;
        public AudioClip reviveStartSound;
        public AudioClip reviveCompleteSound;

        [Header("Status Effects")]
        public AudioClip bleedingLoop;
        public AudioClip burningLoop;
        public AudioClip poisonedLoop;
        public AudioClip shieldBreakSound;
        public AudioClip armorHitSound;
    }

    [Serializable]
    public class FootstepSoundSet
    {
        public string surfaceType;
        public PhysicMaterial physicMaterial;
        public AudioClip[] walkSounds;
        public AudioClip[] runSounds;
        public AudioClip[] sprintSounds;
        public AudioClip[] crouchSounds;
        public AudioClip[] landSounds;
        public float volumeMultiplier = 1f;
    }

    [Serializable]
    public class AmbientSoundBank
    {
        [Header("Environment")]
        public AmbientLoop[] environmentLoops;
        public AudioClip[] windSounds;
        public AudioClip[] rainSounds;
        public AudioClip[] thunderSounds;
        public AudioClip[] birdSounds;
        public AudioClip[] insectSounds;

        [Header("Urban")]
        public AudioClip[] sirenSounds;
        public AudioClip[] carAlarmSounds;
        public AudioClip[] distantExplosions;
        public AudioClip[] helicopterSounds;
        public AudioClip[] debrisSounds;

        [Header("Indoor")]
        public AudioClip[] hvacSounds;
        public AudioClip[] electricalBuzzSounds;
        public AudioClip[] pipeSounds;
        public AudioClip[] creakingSounds;

        [Header("Horror")]
        public AudioClip[] distantScreamSounds;
        public AudioClip[] ominousSounds;
        public AudioClip[] tensionSounds;
        public AudioClip[] heartbeatSound;

        [Header("Stingers")]
        public AudioClip zombieNearbyStinger;
        public AudioClip dangerStinger;
        public AudioClip extractionAvailableStinger;
        public AudioClip lowHealthStinger;
    }

    [Serializable]
    public class AmbientLoop
    {
        public string environmentId;
        public string environmentName;
        public AudioClip daytimeLoop;
        public AudioClip nighttimeLoop;
        public float crossfadeDuration = 5f;
        public float volume = 0.5f;
    }

    [Serializable]
    public class UISoundBank
    {
        [Header("Navigation")]
        public AudioClip buttonHover;
        public AudioClip buttonClick;
        public AudioClip buttonBack;
        public AudioClip tabSwitch;
        public AudioClip menuOpen;
        public AudioClip menuClose;

        [Header("Inventory")]
        public AudioClip itemPickup;
        public AudioClip itemDrop;
        public AudioClip itemEquip;
        public AudioClip itemUnequip;
        public AudioClip itemMove;
        public AudioClip inventoryFull;

        [Header("Notifications")]
        public AudioClip notificationInfo;
        public AudioClip notificationWarning;
        public AudioClip notificationError;
        public AudioClip notificationSuccess;
        public AudioClip questComplete;
        public AudioClip achievementUnlock;
        public AudioClip levelUp;

        [Header("Match")]
        public AudioClip matchFound;
        public AudioClip countdownTick;
        public AudioClip matchStart;
        public AudioClip matchEnd;
        public AudioClip victoryJingle;
        public AudioClip defeatJingle;
        public AudioClip extractionSuccess;

        [Header("Social")]
        public AudioClip friendOnline;
        public AudioClip messageReceived;
        public AudioClip partyInvite;

        [Header("Shop")]
        public AudioClip purchaseSuccess;
        public AudioClip purchaseFail;
        public AudioClip rewardClaim;
    }

    [Serializable]
    public class MusicTrackBank
    {
        [Header("Menu Music")]
        public MusicTrack mainMenuTheme;
        public MusicTrack lobbyTheme;
        public MusicTrack loadingTheme;

        [Header("Gameplay Music")]
        public MusicTrack[] explorationTracks;
        public MusicTrack[] combatTracks;
        public MusicTrack[] intenseCombatTracks;
        public MusicTrack[] hordeEventTracks;
        public MusicTrack[] bossEncounterTracks;
        public MusicTrack extractionTrack;

        [Header("Ambient Music")]
        public MusicTrack[] ambientDayTracks;
        public MusicTrack[] ambientNightTracks;
        public MusicTrack[] ambientTensionTracks;

        [Header("Event Music")]
        public MusicTrack victoryTheme;
        public MusicTrack defeatTheme;
        public MusicTrack eventTheme;

        [Header("Transition Settings")]
        public float crossfadeDuration = 2f;
        public float combatMusicDelay = 1f;
        public float combatMusicFadeOut = 5f;
    }

    [Serializable]
    public class MusicTrack
    {
        public string trackId;
        public string trackName;
        public AudioClip introClip;
        public AudioClip loopClip;
        public float volume = 1f;
        public float bpm = 120f;
        public MusicIntensity intensity;
    }

    public enum MusicIntensity
    {
        Calm,
        Ambient,
        Exploration,
        Tension,
        Combat,
        IntenseCombat,
        Boss,
        Victory,
        Defeat
    }

    #endregion

    #region Sound Effect Definitions

    [CreateAssetMenu(fileName = "New Sound Effect", menuName = "Dead Frontier/Audio/Sound Effect")]
    public class SoundEffectData : ScriptableObject
    {
        [Header("Identification")]
        public string soundId;
        public string soundName;
        public SoundCategory category;

        [Header("Audio Clips")]
        public AudioClip[] clips;
        public bool randomizeClip = true;
        public bool avoidRepeat = true;

        [Header("Volume")]
        [Range(0f, 1f)]
        public float volume = 1f;
        [Range(0f, 0.5f)]
        public float volumeVariation = 0.1f;

        [Header("Pitch")]
        [Range(0.5f, 2f)]
        public float pitch = 1f;
        [Range(0f, 0.5f)]
        public float pitchVariation = 0.05f;

        [Header("Spatial")]
        public bool is3D = true;
        [Range(0f, 1f)]
        public float spatialBlend = 1f;
        public float minDistance = 1f;
        public float maxDistance = 50f;
        public AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;

        [Header("Playback")]
        public bool loop;
        public int maxInstances = 5;
        public float cooldown = 0f;
        public int priority = 128;

        [Header("Effects")]
        public bool useReverb = true;
        public float reverbZoneMix = 1f;
        public bool useOcclusion = true;
        public bool useDoppler;
        public float dopplerLevel = 0.5f;

        private int _lastClipIndex = -1;

        public AudioClip GetClip()
        {
            if (clips == null || clips.Length == 0) return null;
            if (clips.Length == 1) return clips[0];

            if (randomizeClip)
            {
                int index;
                if (avoidRepeat)
                {
                    do { index = UnityEngine.Random.Range(0, clips.Length); }
                    while (index == _lastClipIndex && clips.Length > 1);
                }
                else
                {
                    index = UnityEngine.Random.Range(0, clips.Length);
                }
                _lastClipIndex = index;
                return clips[index];
            }

            return clips[0];
        }

        public float GetVolume() => volume + UnityEngine.Random.Range(-volumeVariation, volumeVariation);
        public float GetPitch() => pitch + UnityEngine.Random.Range(-pitchVariation, pitchVariation);
    }

    public enum SoundCategory
    {
        Weapon,
        Player,
        Zombie,
        Environment,
        UI,
        Music,
        Voice,
        Impact,
        Explosion,
        Ambient
    }

    #endregion
}
