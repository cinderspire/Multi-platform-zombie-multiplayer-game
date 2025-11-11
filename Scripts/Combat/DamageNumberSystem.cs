using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

namespace ZombieGame.Combat
{
    /// <summary>
    /// Floating damage number system showing damage dealt with color coding
    /// and animations for different damage types.
    /// </summary>
    public class DamageNumberSystem : NetworkBehaviour
    {
        public static DamageNumberSystem Instance { get; private set; }

        [Header("Damage Number Configuration")]
        [SerializeField] private GameObject damageNumberPrefab;
        [SerializeField] private float numberLifetime = 1.5f;
        [SerializeField] private float floatSpeed = 1.5f;
        [SerializeField] private float randomSpread = 0.3f;
        [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

        [Header("Damage Colors")]
        [SerializeField] private Color normalDamageColor = Color.white;
        [SerializeField] private Color criticalDamageColor = Color.yellow;
        [SerializeField] private Color headshotDamageColor = Color.red;
        [SerializeField] private Color fireDamageColor = new Color(1f, 0.5f, 0f);
        [SerializeField] private Color poisonDamageColor = Color.green;
        [SerializeField] private Color healingColor = new Color(0f, 1f, 0.5f);

        [Header("Text Settings")]
        [SerializeField] private float normalFontSize = 36f;
        [SerializeField] private float criticalFontSize = 48f;
        [SerializeField] private bool showCommas = true;

        private Dictionary<string, DamageNumberInstance> activeDamageNumbers = new Dictionary<string, DamageNumberInstance>();

        public event Action<Vector3, float, DamageNumberType> OnDamageNumberSpawned;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Update()
        {
            UpdateDamageNumbers();
        }

        /// <summary>
        /// Spawn a damage number at a world position
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void SpawnDamageNumberServerRpc(Vector3 worldPosition, float damage, DamageNumberType numberType, ServerRpcParams rpcParams = default)
        {
            SpawnDamageNumberClientRpc(worldPosition, damage, numberType);
            OnDamageNumberSpawned?.Invoke(worldPosition, damage, numberType);
        }

        [ClientRpc]
        private void SpawnDamageNumberClientRpc(Vector3 worldPosition, float damage, DamageNumberType numberType)
        {
            CreateDamageNumber(worldPosition, damage, numberType);
        }

        private void CreateDamageNumber(Vector3 worldPosition, float damage, DamageNumberType numberType)
        {
            if (damageNumberPrefab == null) return;

            // Create the damage number object
            GameObject numberObj = Instantiate(damageNumberPrefab);
            numberObj.transform.position = worldPosition;

            // Add random spread
            Vector3 randomOffset = new Vector3(
                UnityEngine.Random.Range(-randomSpread, randomSpread),
                UnityEngine.Random.Range(0, randomSpread),
                UnityEngine.Random.Range(-randomSpread, randomSpread)
            );
            numberObj.transform.position += randomOffset;

            // Setup text
            TextMeshPro textMesh = numberObj.GetComponent<TextMeshPro>();
            if (textMesh == null)
            {
                textMesh = numberObj.AddComponent<TextMeshPro>();
            }

            // Format damage text
            string damageText = showCommas ? damage.ToString("N0") : Mathf.RoundToInt(damage).ToString();

            // Add prefix/suffix based on type
            switch (numberType)
            {
                case DamageNumberType.CriticalHit:
                case DamageNumberType.Headshot:
                    damageText = $"<b>{damageText}!</b>";
                    break;
                case DamageNumberType.Healing:
                    damageText = $"+{damageText}";
                    break;
            }

            textMesh.text = damageText;
            textMesh.fontSize = (numberType == DamageNumberType.CriticalHit || numberType == DamageNumberType.Headshot)
                ? criticalFontSize : normalFontSize;
            textMesh.color = GetDamageColor(numberType);
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.sortingOrder = 100;

            // Make it face camera
            numberObj.transform.rotation = Camera.main != null ?
                Quaternion.LookRotation(numberObj.transform.position - Camera.main.transform.position) :
                Quaternion.identity;

            // Store instance
            string id = Guid.NewGuid().ToString();
            activeDamageNumbers[id] = new DamageNumberInstance
            {
                id = id,
                gameObject = numberObj,
                textMesh = textMesh,
                spawnTime = Time.time,
                startPosition = numberObj.transform.position,
                numberType = numberType
            };

            // Auto-destroy
            Destroy(numberObj, numberLifetime);
        }

        private void UpdateDamageNumbers()
        {
            List<string> toRemove = new List<string>();

            foreach (var kvp in activeDamageNumbers)
            {
                var instance = kvp.Value;
                if (instance.gameObject == null)
                {
                    toRemove.Add(kvp.Key);
                    continue;
                }

                float age = Time.time - instance.spawnTime;
                float normalizedAge = age / numberLifetime;

                // Float upward
                instance.gameObject.transform.position = instance.startPosition + Vector3.up * (floatSpeed * age);

                // Face camera
                if (Camera.main != null)
                {
                    instance.gameObject.transform.rotation =
                        Quaternion.LookRotation(instance.gameObject.transform.position - Camera.main.transform.position);
                }

                // Animate scale
                float scale = scaleCurve.Evaluate(normalizedAge);
                instance.gameObject.transform.localScale = Vector3.one * scale;

                // Fade out
                if (instance.textMesh != null)
                {
                    Color color = instance.textMesh.color;
                    color.a = 1f - normalizedAge;
                    instance.textMesh.color = color;
                }

                // Mark for removal if expired
                if (normalizedAge >= 1f)
                {
                    toRemove.Add(kvp.Key);
                }
            }

            // Clean up expired numbers
            foreach (string id in toRemove)
            {
                activeDamageNumbers.Remove(id);
            }
        }

        private Color GetDamageColor(DamageNumberType numberType)
        {
            return numberType switch
            {
                DamageNumberType.Normal => normalDamageColor,
                DamageNumberType.CriticalHit => criticalDamageColor,
                DamageNumberType.Headshot => headshotDamageColor,
                DamageNumberType.Fire => fireDamageColor,
                DamageNumberType.Poison => poisonDamageColor,
                DamageNumberType.Healing => healingColor,
                _ => normalDamageColor
            };
        }

        [Serializable]
        private class DamageNumberInstance
        {
            public string id;
            public GameObject gameObject;
            public TextMeshPro textMesh;
            public float spawnTime;
            public Vector3 startPosition;
            public DamageNumberType numberType;
        }

        public enum DamageNumberType
        {
            Normal,
            CriticalHit,
            Headshot,
            Fire,
            Poison,
            Healing
        }
    }
}
