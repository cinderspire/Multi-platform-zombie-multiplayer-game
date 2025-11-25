using UnityEngine;
using System;
using System.Collections.Generic;

namespace DeadFrontier.VFX
{
    /// <summary>
    /// Visual effects configuration and definitions.
    /// </summary>
    [CreateAssetMenu(fileName = "VFX Configuration", menuName = "Dead Frontier/VFX/Configuration")]
    public class VisualEffectsConfiguration : ScriptableObject
    {
        [Header("Weapon VFX")]
        public WeaponVFXSet[] weaponVFX;

        [Header("Impact VFX")]
        public ImpactVFXSet[] impactVFX;

        [Header("Blood VFX")]
        public BloodVFXSet bloodVFX;

        [Header("Environment VFX")]
        public EnvironmentVFXSet environmentVFX;

        [Header("Character VFX")]
        public CharacterVFXSet characterVFX;

        [Header("UI VFX")]
        public UIVFXSet uiVFX;

        [Header("Weather VFX")]
        public WeatherVFXSet weatherVFX;

        [Header("Ability VFX")]
        public AbilityVFXSet[] abilityVFX;

        [Header("Pooling Settings")]
        public int vfxPoolSize = 100;
        public float vfxCullDistance = 100f;
        public bool enableVFXCulling = true;
    }

    #region VFX Sets

    [Serializable]
    public class WeaponVFXSet
    {
        public string weaponId;
        public string weaponName;

        [Header("Muzzle Flash")]
        public GameObject muzzleFlashPrefab;
        public float muzzleFlashDuration = 0.05f;
        public Color muzzleFlashColor = Color.yellow;
        public float muzzleFlashIntensity = 5f;
        public bool useMuzzleLight = true;

        [Header("Bullet Tracer")]
        public GameObject tracerPrefab;
        public float tracerSpeed = 500f;
        public float tracerLength = 2f;
        public Color tracerColor = Color.yellow;
        public float tracerChance = 0.3f;

        [Header("Shell Ejection")]
        public GameObject shellPrefab;
        public float shellEjectForce = 2f;
        public float shellLifetime = 3f;
        public bool shellPhysics = true;

        [Header("Reload")]
        public GameObject magazineDropPrefab;
        public GameObject smokeVentPrefab;

        [Header("Special")]
        public GameObject suppressorSmokeVFX;
        public GameObject overheatSteamVFX;
    }

    [Serializable]
    public class ImpactVFXSet
    {
        public string surfaceType;
        public PhysicMaterial[] associatedMaterials;
        public string[] associatedTags;

        [Header("Impact Effects")]
        public GameObject impactPrefab;
        public GameObject impactDecalPrefab;
        public float impactScale = 1f;
        public Color impactColor = Color.white;
        public float decalLifetime = 60f;
        public float decalSize = 0.1f;

        [Header("Debris")]
        public GameObject[] debrisPrefabs;
        public int debrisCount = 3;
        public float debrisForce = 2f;
        public float debrisLifetime = 5f;

        [Header("Dust")]
        public GameObject dustPrefab;
        public Color dustColor = Color.gray;
        public float dustScale = 1f;
    }

    [Serializable]
    public class BloodVFXSet
    {
        [Header("Blood Splatter")]
        public GameObject bloodSplatterPrefab;
        public GameObject bloodMistPrefab;
        public Color bloodColor = new Color(0.5f, 0f, 0f, 1f);

        [Header("Blood Decals")]
        public GameObject[] bloodDecalPrefabs;
        public float decalLifetime = 120f;
        public float decalMinSize = 0.2f;
        public float decalMaxSize = 0.8f;

        [Header("Blood Pools")]
        public GameObject bloodPoolPrefab;
        public float poolGrowthTime = 5f;
        public float poolMaxSize = 2f;

        [Header("Headshot")]
        public GameObject headshotVFXPrefab;
        public float headshotScale = 1.5f;

        [Header("Gore")]
        public bool enableGore = true;
        public GameObject[] gibPrefabs;
        public GameObject bloodTrailPrefab;
    }

    [Serializable]
    public class EnvironmentVFXSet
    {
        [Header("Explosions")]
        public GameObject explosionSmallPrefab;
        public GameObject explosionMediumPrefab;
        public GameObject explosionLargePrefab;
        public GameObject explosionFirePrefab;

        [Header("Fire")]
        public GameObject firePrefab;
        public GameObject fireSpreadPrefab;
        public GameObject smokePrefab;
        public GameObject embersParticles;

        [Header("Electric")]
        public GameObject sparksPrefab;
        public GameObject electricArcPrefab;
        public GameObject electricFieldPrefab;

        [Header("Hazards")]
        public GameObject toxicCloudPrefab;
        public GameObject radiationFieldPrefab;
        public GameObject acidPoolPrefab;

        [Header("Destruction")]
        public GameObject glassShatderPrefab;
        public GameObject woodSplintersVFX;
        public GameObject metalSparkVFX;
        public GameObject concreteDebrisVFX;

        [Header("Environmental")]
        public GameObject dustKickupPrefab;
        public GameObject waterSplashPrefab;
        public GameObject leavesFallingPrefab;
        public GameObject fogPrefab;
    }

    [Serializable]
    public class CharacterVFXSet
    {
        [Header("Movement")]
        public GameObject footstepDustPrefab;
        public GameObject sprintTrailPrefab;
        public GameObject slideTrailPrefab;
        public GameObject waterWadePrefab;

        [Header("Actions")]
        public GameObject healVFXPrefab;
        public GameObject reviveVFXPrefab;
        public GameObject levelUpVFXPrefab;
        public GameObject respawnVFXPrefab;

        [Header("Status Effects")]
        public GameObject bleedingVFXPrefab;
        public GameObject burningVFXPrefab;
        public GameObject poisonedVFXPrefab;
        public GameObject shieldVFXPrefab;
        public GameObject stunVFXPrefab;

        [Header("Hit Effects")]
        public GameObject hitIndicatorVFXPrefab;
        public GameObject damageNumberPrefab;
        public GameObject criticalHitVFXPrefab;
        public GameObject screenBloodOverlay;

        [Header("Death")]
        public GameObject deathVFXPrefab;
        public bool useRagdoll = true;
        public float ragdollDuration = 10f;
    }

    [Serializable]
    public class UIVFXSet
    {
        [Header("HUD Effects")]
        public GameObject damageVignetteEffect;
        public GameObject lowHealthPulseEffect;
        public GameObject hitMarkerEffect;
        public GameObject killConfirmEffect;
        public GameObject headshotIndicator;

        [Header("Screen Effects")]
        public Material screenDamageEffect;
        public Material screenBlurEffect;
        public Material screenFlashEffect;
        public Material screenVignetteEffect;

        [Header("Notification Effects")]
        public GameObject achievementPopupVFX;
        public GameObject levelUpPopupVFX;
        public GameObject rewardClaimVFX;

        [Header("Crosshair")]
        public GameObject crosshairHitVFX;
        public GameObject crosshairKillVFX;
    }

    [Serializable]
    public class WeatherVFXSet
    {
        [Header("Rain")]
        public GameObject rainParticlesPrefab;
        public GameObject rainSplashesPrefab;
        public GameObject rainOnSurfacePrefab;
        public Material wetSurfaceMaterial;

        [Header("Snow")]
        public GameObject snowParticlesPrefab;
        public GameObject snowAccumulationPrefab;
        public Material snowCoveredMaterial;

        [Header("Fog")]
        public GameObject fogVolumePrefab;
        public GameObject groundMistPrefab;
        public float fogDensity = 0.05f;
        public Color fogColor = Color.gray;

        [Header("Storm")]
        public GameObject lightningPrefab;
        public GameObject windDebrisPrefab;
        public float lightningInterval = 15f;

        [Header("Dust Storm")]
        public GameObject dustStormPrefab;
        public GameObject sandParticlesPrefab;
        public Color dustColor = new Color(0.8f, 0.6f, 0.4f);

        [Header("Day/Night")]
        public GameObject sunShaftsPrefab;
        public GameObject moonGlowPrefab;
        public AnimationCurve dayNightLightCurve;
    }

    [Serializable]
    public class AbilityVFXSet
    {
        public string abilityId;
        public string abilityName;

        [Header("Cast Effects")]
        public GameObject castStartVFX;
        public GameObject castLoopVFX;
        public GameObject castEndVFX;
        public float castScale = 1f;

        [Header("Projectile")]
        public GameObject projectilePrefab;
        public GameObject projectileTrailPrefab;
        public float projectileSpeed = 20f;

        [Header("Impact")]
        public GameObject impactVFXPrefab;
        public GameObject areaEffectPrefab;
        public float impactScale = 1f;

        [Header("Character")]
        public GameObject casterAuraPrefab;
        public GameObject targetEffectPrefab;
    }

    #endregion

    #region Shader Configuration

    [CreateAssetMenu(fileName = "Shader Configuration", menuName = "Dead Frontier/VFX/Shader Configuration")]
    public class ShaderConfiguration : ScriptableObject
    {
        [Header("Character Shaders")]
        public Shader characterShader;
        public Shader characterOutlineShader;
        public Shader characterDamageShader;
        public Shader characterGhostShader;

        [Header("Weapon Shaders")]
        public Shader weaponShader;
        public Shader weaponHeatShader;
        public Shader tracerShader;

        [Header("Environment Shaders")]
        public Shader terrainShader;
        public Shader waterShader;
        public Shader glassShader;
        public Shader foliageShader;

        [Header("VFX Shaders")]
        public Shader particleShader;
        public Shader decalShader;
        public Shader hologramShader;
        public Shader distortionShader;

        [Header("Post Processing")]
        public Shader bloomShader;
        public Shader colorGradingShader;
        public Shader dofShader;
        public Shader motionBlurShader;
        public Shader vignetteShader;
        public Shader filmGrainShader;

        [Header("Outline Settings")]
        public float outlineWidth = 2f;
        public Color friendlyOutlineColor = Color.green;
        public Color enemyOutlineColor = Color.red;
        public Color itemOutlineColor = Color.yellow;
        public Color objectiveOutlineColor = Color.blue;

        [Header("Material Properties")]
        public MaterialPropertyBlock defaultPropertyBlock;
        public float defaultSmoothness = 0.5f;
        public float defaultMetallic = 0f;
    }

    #endregion

    #region Post Processing

    [CreateAssetMenu(fileName = "Post Processing Profile", menuName = "Dead Frontier/VFX/Post Processing Profile")]
    public class PostProcessingProfile : ScriptableObject
    {
        public string profileName;

        [Header("Bloom")]
        public bool enableBloom = true;
        public float bloomIntensity = 1f;
        public float bloomThreshold = 0.9f;
        public float bloomSoftKnee = 0.5f;
        public Color bloomColor = Color.white;

        [Header("Color Grading")]
        public bool enableColorGrading = true;
        public float temperature = 0f;
        public float tint = 0f;
        public float saturation = 0f;
        public float contrast = 0f;
        public AnimationCurve toneCurve;
        public Color colorFilter = Color.white;

        [Header("Vignette")]
        public bool enableVignette = true;
        public float vignetteIntensity = 0.3f;
        public float vignetteSmoothness = 0.5f;
        public Color vignetteColor = Color.black;

        [Header("Depth of Field")]
        public bool enableDOF = false;
        public float focusDistance = 10f;
        public float aperture = 5.6f;
        public float focalLength = 50f;

        [Header("Motion Blur")]
        public bool enableMotionBlur = false;
        public float motionBlurIntensity = 0.5f;
        public int motionBlurSamples = 10;

        [Header("Chromatic Aberration")]
        public bool enableChromaticAberration = false;
        public float chromaticAberrationIntensity = 0.1f;

        [Header("Film Grain")]
        public bool enableFilmGrain = false;
        public float filmGrainIntensity = 0.3f;
        public float filmGrainResponse = 0.8f;

        [Header("Ambient Occlusion")]
        public bool enableAO = true;
        public float aoIntensity = 0.5f;
        public float aoRadius = 0.3f;
    }

    #endregion

    #region Decal System

    [CreateAssetMenu(fileName = "Decal Configuration", menuName = "Dead Frontier/VFX/Decal Configuration")]
    public class DecalConfiguration : ScriptableObject
    {
        [Header("Pool Settings")]
        public int maxDecals = 500;
        public float decalCullDistance = 50f;
        public bool fadeDecalsOverTime = true;
        public float decalFadeTime = 60f;

        [Header("Bullet Holes")]
        public DecalData[] bulletHoleDecals;
        public float bulletHoleSize = 0.05f;
        public float bulletHoleSizeVariation = 0.02f;

        [Header("Blood")]
        public DecalData[] bloodSplatterDecals;
        public DecalData[] bloodPoolDecals;
        public float bloodDecalSize = 0.3f;

        [Header("Scorch Marks")]
        public DecalData[] scorchDecals;
        public float scorchDecalSize = 1f;

        [Header("Footprints")]
        public DecalData[] mudFootprintDecals;
        public DecalData[] bloodFootprintDecals;
        public DecalData[] wetFootprintDecals;
        public float footprintSize = 0.3f;
        public float footprintFadeTime = 30f;
    }

    [Serializable]
    public class DecalData
    {
        public string decalId;
        public Material decalMaterial;
        public Texture2D[] decalTextures;
        public float minSize = 0.1f;
        public float maxSize = 0.5f;
        public float lifetime = 60f;
        public bool randomRotation = true;
        public Color tintColor = Color.white;
        public DecalProjection projection = DecalProjection.Box;
    }

    public enum DecalProjection
    {
        Box,
        Sphere,
        Cylinder
    }

    #endregion
}
