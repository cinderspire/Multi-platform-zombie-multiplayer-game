using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Combat
{
    /// <summary>
    /// Visual hit marker system providing instant feedback for hits with different markers
    /// for headshots, critical hits, kills, and armor hits.
    /// </summary>
    public class HitMarkerSystem : NetworkBehaviour
    {
        public static HitMarkerSystem Instance { get; private set; }

        [Header("Hit Marker Configuration")]
        [SerializeField] private GameObject hitMarkerPrefab;
        [SerializeField] private float hitMarkerDuration = 0.2f;
        [SerializeField] private float hitMarkerScale = 1f;

        [Header("Hit Marker Colors")]
        [SerializeField] private Color normalHitColor = Color.white;
        [SerializeField] private Color headshotColor = Color.red;
        [SerializeField] private Color criticalHitColor = Color.yellow;
        [SerializeField] private Color killShotColor = new Color(1f, 0.5f, 0f);
        [SerializeField] private Color armorHitColor = Color.cyan;

        [Header("Audio")]
        [SerializeField] private AudioClip normalHitSound;
        [SerializeField] private AudioClip headshotSound;
        [SerializeField] private AudioClip criticalHitSound;
        [SerializeField] private AudioClip killSound;

        private Dictionary<ulong, HitMarkerUI> playerHitMarkers = new Dictionary<ulong, HitMarkerUI>();

        public event Action<ulong, HitMarkerType, Vector3> OnHitMarkerTriggered;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        /// <summary>
        /// Show hit marker for the attacking player
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void ShowHitMarkerServerRpc(ulong attackerId, HitMarkerType markerType, Vector3 hitPosition, ServerRpcParams rpcParams = default)
        {
            if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(attackerId)) return;

            ShowHitMarkerClientRpc(attackerId, markerType, hitPosition);
            OnHitMarkerTriggered?.Invoke(attackerId, markerType, hitPosition);
        }

        [ClientRpc]
        private void ShowHitMarkerClientRpc(ulong attackerId, HitMarkerType markerType, Vector3 hitPosition)
        {
            // Only show for the local player who attacked
            if (NetworkManager.Singleton.LocalClientId != attackerId) return;

            DisplayHitMarker(markerType, hitPosition);
            PlayHitSound(markerType);
        }

        private void DisplayHitMarker(HitMarkerType markerType, Vector3 worldPosition)
        {
            // Create UI hit marker on screen
            GameObject marker = Instantiate(hitMarkerPrefab);
            marker.transform.SetParent(GameObject.Find("Canvas")?.transform ?? transform);
            marker.transform.localScale = Vector3.one * hitMarkerScale;

            // Position at center of screen (hit markers typically appear at crosshair)
            RectTransform rectTransform = marker.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = Vector2.zero;
            }

            // Apply color based on hit type
            UnityEngine.UI.Image markerImage = marker.GetComponent<UnityEngine.UI.Image>();
            if (markerImage != null)
            {
                markerImage.color = GetMarkerColor(markerType);

                // Apply special effects based on type
                switch (markerType)
                {
                    case HitMarkerType.Headshot:
                    case HitMarkerType.CriticalHit:
                        markerImage.transform.localScale *= 1.3f; // Bigger for crits
                        break;
                    case HitMarkerType.Kill:
                        markerImage.transform.localScale *= 1.5f; // Biggest for kills
                        break;
                }
            }

            // Auto-destroy after duration
            Destroy(marker, hitMarkerDuration);
        }

        private Color GetMarkerColor(HitMarkerType markerType)
        {
            return markerType switch
            {
                HitMarkerType.Normal => normalHitColor,
                HitMarkerType.Headshot => headshotColor,
                HitMarkerType.CriticalHit => criticalHitColor,
                HitMarkerType.Kill => killShotColor,
                HitMarkerType.ArmorHit => armorHitColor,
                _ => normalHitColor
            };
        }

        private void PlayHitSound(HitMarkerType markerType)
        {
            AudioClip clip = markerType switch
            {
                HitMarkerType.Headshot => headshotSound,
                HitMarkerType.CriticalHit => criticalHitSound,
                HitMarkerType.Kill => killSound,
                _ => normalHitSound
            };

            if (clip != null && Audio.AudioSystem.Instance != null)
            {
                Audio.AudioSystem.Instance.PlaySFX(clip, Vector3.zero, 1f);
            }
        }

        /// <summary>
        /// Automatically determine hit marker type from damage info
        /// </summary>
        public static HitMarkerType DetermineHitMarkerType(bool isHeadshot, bool isCritical, bool isKill, bool hitArmor)
        {
            if (isKill) return HitMarkerType.Kill;
            if (isHeadshot) return HitMarkerType.Headshot;
            if (isCritical) return HitMarkerType.CriticalHit;
            if (hitArmor) return HitMarkerType.ArmorHit;
            return HitMarkerType.Normal;
        }

        [Serializable]
        public class HitMarkerUI
        {
            public GameObject markerObject;
            public float remainingDuration;
        }

        public enum HitMarkerType
        {
            Normal,
            Headshot,
            CriticalHit,
            Kill,
            ArmorHit
        }
    }
}
