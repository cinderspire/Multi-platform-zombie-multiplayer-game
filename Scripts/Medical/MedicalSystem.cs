using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;
using System;

namespace DeadFrontier.Medical
{
    /// <summary>
    /// Comprehensive medical and healing system with injuries, healing items, and field medicine.
    /// Provides tactical healing choices and realistic injury management.
    /// </summary>
    public class MedicalSystem : NetworkBehaviour
    {
        public static MedicalSystem Instance { get; private set; }

        [Header("Injury Settings")]
        [SerializeField] private float bleedingDamagePerSecond = 2f;
        [SerializeField] private float criticalHealthThreshold = 0.25f;
        [SerializeField] private float incapacitatedThreshold = 0.01f;

        [Header("Healing Settings")]
        [SerializeField] private float bandageHealAmount = 25f;
        [SerializeField] private float bandageUseTime = 3f;
        [SerializeField] private float medkitHealAmount = 75f;
        [SerializeField] private float medkitUseTime = 6f;
        [SerializeField] private float stimHealAmount = 50f;
        [SerializeField] private float stimUseTime = 2f;
        [SerializeField] private float stimSpeedBoost = 0.3f;
        [SerializeField] private float stimDuration = 10f;

        [Header("Revive Settings")]
        [SerializeField] private float reviveTime = 8f;
        [SerializeField] private float revivedHealthPercent = 0.3f;
        [SerializeField] private float reviveRange = 3f;

        [Header("Surgery Settings")]
        [SerializeField] private bool enableSurgerySystem = true;
        [SerializeField] private float surgeryTime = 15f;
        [SerializeField] private int surgeryKitCost = 1;

        // Player medical states
        private Dictionary<ulong, MedicalState> playerMedicalStates = new Dictionary<ulong, MedicalState>();

        // Active healing
        private Dictionary<ulong, ActiveHealing> activeHealing = new Dictionary<ulong, ActiveHealing>();

        // Events
        public event Action<ulong, InjuryType> OnInjuryReceived;
        public event Action<ulong, MedicalItemType> OnHealingStarted;
        public event Action<ulong, float> OnHealingProgress;
        public event Action<ulong, float> OnHealingCompleted;
        public event Action<ulong, ulong> OnReviveStarted; // Reviver, Target
        public event Action<ulong, ulong> OnReviveCompleted;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (!IsServer) return;

            UpdateInjuries();
            UpdateActiveHealing();
        }

        #region Injury System

        public void RegisterPlayer(ulong playerId)
        {
            if (playerMedicalStates.ContainsKey(playerId)) return;

            playerMedicalStates[playerId] = new MedicalState
            {
                playerId = playerId,
                injuries = new List<Injury>(),
                isIncapacitated = false
            };
        }

        public void ApplyInjury(ulong playerId, InjuryType injuryType, InjurySeverity severity)
        {
            if (!IsServer) return;
            if (!playerMedicalStates.ContainsKey(playerId)) return;

            var state = playerMedicalStates[playerId];

            var injury = new Injury
            {
                injuryType = injuryType,
                severity = severity,
                timeReceived = Time.time
            };

            state.injuries.Add(injury);

            OnInjuryReceived?.Invoke(playerId, injuryType);

            // Apply immediate effects
            ApplyInjuryEffects(playerId, injury);

            Debug.Log($"[MedicalSystem] Player {playerId} received {severity} {injuryType}");
        }

        private void ApplyInjuryEffects(ulong playerId, Injury injury)
        {
            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerId, out NetworkObject playerObj))
                return;

            switch (injury.injuryType)
            {
                case InjuryType.Bleeding:
                    // DOT applied in UpdateInjuries
                    break;

                case InjuryType.Fracture:
                    var movement = playerObj.GetComponent<Player.PlayerMovement>();
                    if (movement != null)
                    {
                        float speedReduction = injury.severity == InjurySeverity.Severe ? 0.5f : 0.25f;
                        movement.SetSpeedMultiplier(1f - speedReduction);
                    }
                    break;

                case InjuryType.Concussion:
                    // Vision blur, sensitivity reduction
                    // TODO: Apply post-processing effects
                    break;

                case InjuryType.Burns:
                    // Apply burn damage over time
                    Combat.DamageTypeSystem.Instance?.ApplyStatusEffect(
                        playerObj.gameObject,
                        Combat.StatusEffectType.Burning,
                        30f
                    );
                    break;

                case InjuryType.Infection:
                    // Gradually reduce max health
                    break;
            }
        }

        private void UpdateInjuries()
        {
            foreach (var kvp in playerMedicalStates)
            {
                ulong playerId = kvp.Key;
                var state = kvp.Value;

                if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerId, out NetworkObject playerObj))
                    continue;

                var health = playerObj.GetComponent<Player.PlayerHealth>();
                if (health == null || !health.IsAlive) continue;

                // Process bleeding
                var bleedingInjuries = state.injuries.Where(i => i.injuryType == InjuryType.Bleeding).ToList();
                if (bleedingInjuries.Count > 0)
                {
                    float totalBleedDamage = 0f;
                    foreach (var injury in bleedingInjuries)
                    {
                        float multiplier = injury.severity == InjurySeverity.Severe ? 2f : 1f;
                        totalBleedDamage += bleedingDamagePerSecond * multiplier;
                    }

                    health.TakeDamage(totalBleedDamage * Time.deltaTime);
                }

                // Check for incapacitation
                if (health.GetHealthPercentage() <= incapacitatedThreshold && !state.isIncapacitated)
                {
                    IncapacitatePlayer(playerId);
                }
            }
        }

        public void HealInjury(ulong playerId, InjuryType injuryType)
        {
            if (!IsServer) return;
            if (!playerMedicalStates.ContainsKey(playerId)) return;

            var state = playerMedicalStates[playerId];
            var injury = state.injuries.FirstOrDefault(i => i.injuryType == injuryType);

            if (injury != null)
            {
                state.injuries.Remove(injury);
                RemoveInjuryEffects(playerId, injury);

                Debug.Log($"[MedicalSystem] Healed {injuryType} for player {playerId}");
            }
        }

        private void RemoveInjuryEffects(ulong playerId, Injury injury)
        {
            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerId, out NetworkObject playerObj))
                return;

            switch (injury.injuryType)
            {
                case InjuryType.Fracture:
                    var movement = playerObj.GetComponent<Player.PlayerMovement>();
                    if (movement != null)
                    {
                        movement.SetSpeedMultiplier(1f);
                    }
                    break;

                case InjuryType.Burns:
                    Combat.DamageTypeSystem.Instance?.RemoveStatusEffect(
                        playerObj.gameObject,
                        Combat.StatusEffectType.Burning
                    );
                    break;
            }
        }

        #endregion

        #region Healing Items

        public bool CanUseHealingItem(ulong playerId, MedicalItemType itemType)
        {
            if (!playerMedicalStates.ContainsKey(playerId)) return false;
            if (activeHealing.ContainsKey(playerId)) return false; // Already healing

            var state = playerMedicalStates[playerId];
            if (state.isIncapacitated) return false;

            // Check if player has the item
            if (Gameplay.InventoryManager.Instance != null)
            {
                var inventory = Gameplay.InventoryManager.Instance.GetInventoryItems(playerId);
                return inventory.Any(i => i.itemData.itemName.Contains(itemType.ToString()));
            }

            return false;
        }

        [ServerRpc(RequireOwnership = false)]
        public void UseHealingItemServerRpc(ulong playerId, MedicalItemType itemType)
        {
            if (!CanUseHealingItem(playerId, itemType)) return;

            StartHealing(playerId, itemType);
        }

        private void StartHealing(ulong playerId, MedicalItemType itemType)
        {
            var healingData = GetHealingData(itemType);
            if (healingData == null) return;

            var activeHeal = new ActiveHealing
            {
                playerId = playerId,
                itemType = itemType,
                startTime = Time.time,
                healingTime = healingData.useTime,
                healAmount = healingData.healAmount
            };

            activeHealing[playerId] = activeHeal;

            OnHealingStarted?.Invoke(playerId, itemType);

            // Notify client
            NotifyHealingStartedClientRpc(playerId, itemType, healingData.useTime);

            Debug.Log($"[MedicalSystem] Player {playerId} started using {itemType}");
        }

        public void CancelHealing(ulong playerId)
        {
            if (!IsServer) return;
            if (!activeHealing.ContainsKey(playerId)) return;

            activeHealing.Remove(playerId);

            NotifyHealingCancelledClientRpc(playerId);

            Debug.Log($"[MedicalSystem] Cancelled healing for player {playerId}");
        }

        private void UpdateActiveHealing()
        {
            var completedHealing = new List<ulong>();

            foreach (var kvp in activeHealing)
            {
                ulong playerId = kvp.Key;
                var healing = kvp.Value;

                float elapsed = Time.time - healing.startTime;
                float progress = elapsed / healing.healingTime;

                OnHealingProgress?.Invoke(playerId, progress);

                // Update client progress
                if (Time.frameCount % 30 == 0)
                {
                    UpdateHealingProgressClientRpc(playerId, progress);
                }

                // Check for completion
                if (progress >= 1f)
                {
                    CompleteHealing(playerId, healing);
                    completedHealing.Add(playerId);
                }

                // Check if player moved (cancel healing)
                if (IsPlayerMoving(playerId))
                {
                    CancelHealing(playerId);
                }
            }

            foreach (var playerId in completedHealing)
            {
                activeHealing.Remove(playerId);
            }
        }

        private void CompleteHealing(ulong playerId, ActiveHealing healing)
        {
            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerId, out NetworkObject playerObj))
                return;

            var health = playerObj.GetComponent<Player.PlayerHealth>();
            if (health == null) return;

            // Apply healing
            health.Heal(healing.healAmount);

            // Apply special effects
            switch (healing.itemType)
            {
                case MedicalItemType.Bandage:
                    // Stop bleeding
                    if (playerMedicalStates.ContainsKey(playerId))
                    {
                        HealInjury(playerId, InjuryType.Bleeding);
                    }
                    break;

                case MedicalItemType.Medkit:
                    // Heal all injuries
                    if (playerMedicalStates.ContainsKey(playerId))
                    {
                        var state = playerMedicalStates[playerId];
                        state.injuries.Clear();
                    }
                    break;

                case MedicalItemType.Stimpack:
                    // Speed boost
                    var movement = playerObj.GetComponent<Player.PlayerMovement>();
                    if (movement != null)
                    {
                        movement.ApplySpeedBoost(stimSpeedBoost, stimDuration);
                    }
                    break;

                case MedicalItemType.Painkiller:
                    // Damage resistance
                    Combat.DamageTypeSystem.Instance?.ApplyStatusEffect(
                        playerObj.gameObject,
                        Combat.StatusEffectType.DamageReduction,
                        30f
                    );
                    break;
            }

            // Consume item
            if (Gameplay.InventoryManager.Instance != null)
            {
                string itemId = $"medical_{healing.itemType.ToString().ToLower()}";
                Gameplay.InventoryManager.Instance.RemoveItem(playerId, itemId, 1);
            }

            OnHealingCompleted?.Invoke(playerId, healing.healAmount);

            NotifyHealingCompleteClientRpc(playerId, healing.healAmount);

            Debug.Log($"[MedicalSystem] Player {playerId} completed healing with {healing.itemType}");
        }

        private HealingItemData GetHealingData(MedicalItemType itemType)
        {
            switch (itemType)
            {
                case MedicalItemType.Bandage:
                    return new HealingItemData { useTime = bandageUseTime, healAmount = bandageHealAmount };
                case MedicalItemType.Medkit:
                    return new HealingItemData { useTime = medkitUseTime, healAmount = medkitHealAmount };
                case MedicalItemType.Stimpack:
                    return new HealingItemData { useTime = stimUseTime, healAmount = stimHealAmount };
                case MedicalItemType.Painkiller:
                    return new HealingItemData { useTime = 2f, healAmount = 15f };
                case MedicalItemType.Adrenaline:
                    return new HealingItemData { useTime = 1f, healAmount = 30f };
                default:
                    return null;
            }
        }

        private bool IsPlayerMoving(ulong playerId)
        {
            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerId, out NetworkObject playerObj))
                return false;

            var movement = playerObj.GetComponent<Player.PlayerMovement>();
            return movement != null && movement.CurrentSpeed > 0.1f;
        }

        #endregion

        #region Incapacitation & Revive

        private void IncapacitatePlayer(ulong playerId)
        {
            if (!playerMedicalStates.ContainsKey(playerId)) return;

            var state = playerMedicalStates[playerId];
            state.isIncapacitated = true;
            state.incapacitatedTime = Time.time;

            NotifyIncapacitatedClientRpc(playerId);

            Debug.Log($"[MedicalSystem] Player {playerId} incapacitated");
        }

        public bool CanRevive(ulong reviverId, ulong targetId)
        {
            if (!playerMedicalStates.ContainsKey(targetId)) return false;

            var targetState = playerMedicalStates[targetId];
            if (!targetState.isIncapacitated) return false;

            // Check distance
            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(reviverId, out NetworkObject reviverObj))
                return false;
            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetId, out NetworkObject targetObj))
                return false;

            float distance = Vector3.Distance(reviverObj.transform.position, targetObj.transform.position);
            return distance <= reviveRange;
        }

        [ServerRpc(RequireOwnership = false)]
        public void StartReviveServerRpc(ulong reviverId, ulong targetId)
        {
            if (!CanRevive(reviverId, targetId)) return;

            OnReviveStarted?.Invoke(reviverId, targetId);

            // Start revive coroutine
            StartCoroutine(ReviveCoroutine(reviverId, targetId));
        }

        private System.Collections.IEnumerator ReviveCoroutine(ulong reviverId, ulong targetId)
        {
            float elapsed = 0f;

            // Apply skill bonuses
            float actualReviveTime = reviveTime;
            if (Progression.SkillTreeSystem.Instance != null)
            {
                float reviveBonus = Progression.SkillTreeSystem.Instance.GetBonus(Progression.SkillBonusType.ReviveSpeed);
                actualReviveTime *= (1f - reviveBonus);
            }

            while (elapsed < actualReviveTime)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / actualReviveTime;

                // Check if reviver moved
                if (IsPlayerMoving(reviverId))
                {
                    Debug.Log($"[MedicalSystem] Revive cancelled - reviver moved");
                    yield break;
                }

                // Check distance
                if (!CanRevive(reviverId, targetId))
                {
                    Debug.Log($"[MedicalSystem] Revive cancelled - out of range");
                    yield break;
                }

                yield return null;
            }

            // Complete revive
            CompleteRevive(targetId);
            OnReviveCompleted?.Invoke(reviverId, targetId);
        }

        private void CompleteRevive(ulong playerId)
        {
            if (!playerMedicalStates.ContainsKey(playerId)) return;

            var state = playerMedicalStates[playerId];
            state.isIncapacitated = false;

            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerId, out NetworkObject playerObj))
                return;

            var health = playerObj.GetComponent<Player.PlayerHealth>();
            if (health != null)
            {
                float reviveHealth = health.GetMaxHealth() * revivedHealthPercent;
                health.Heal(reviveHealth);
            }

            NotifyRevivedClientRpc(playerId);

            Debug.Log($"[MedicalSystem] Player {playerId} revived");
        }

        #endregion

        #region Client RPCs

        [ClientRpc]
        private void NotifyHealingStartedClientRpc(ulong playerId, MedicalItemType itemType, float duration)
        {
            // Show healing UI
            Core.NotificationManager.Instance?.ShowNotification(
                $"Using {itemType}...",
                NotificationType.Info,
                duration
            );
        }

        [ClientRpc]
        private void UpdateHealingProgressClientRpc(ulong playerId, float progress)
        {
            // Update healing progress bar
        }

        [ClientRpc]
        private void NotifyHealingCompleteClientRpc(ulong playerId, float healAmount)
        {
            Core.NotificationManager.Instance?.ShowNotification(
                $"Healed +{healAmount:F0}",
                NotificationType.Success,
                2f
            );
        }

        [ClientRpc]
        private void NotifyHealingCancelledClientRpc(ulong playerId)
        {
            Core.NotificationManager.Instance?.ShowNotification(
                "Healing cancelled",
                NotificationType.Warning,
                2f
            );
        }

        [ClientRpc]
        private void NotifyIncapacitatedClientRpc(ulong playerId)
        {
            Core.NotificationManager.Instance?.ShowNotification(
                "INCAPACITATED!",
                NotificationType.Danger,
                5f
            );
        }

        [ClientRpc]
        private void NotifyRevivedClientRpc(ulong playerId)
        {
            Core.NotificationManager.Instance?.ShowNotification(
                "Revived!",
                NotificationType.Success,
                3f
            );
        }

        #endregion

        #region Public Getters

        public List<Injury> GetPlayerInjuries(ulong playerId)
        {
            if (playerMedicalStates.ContainsKey(playerId))
                return new List<Injury>(playerMedicalStates[playerId].injuries);
            return new List<Injury>();
        }

        public bool IsIncapacitated(ulong playerId)
        {
            return playerMedicalStates.ContainsKey(playerId) && playerMedicalStates[playerId].isIncapacitated;
        }

        public bool IsHealing(ulong playerId)
        {
            return activeHealing.ContainsKey(playerId);
        }

        #endregion
    }

    #region Data Classes

    public class MedicalState
    {
        public ulong playerId;
        public List<Injury> injuries;
        public bool isIncapacitated;
        public float incapacitatedTime;
    }

    [System.Serializable]
    public class Injury
    {
        public InjuryType injuryType;
        public InjurySeverity severity;
        public float timeReceived;
    }

    public class ActiveHealing
    {
        public ulong playerId;
        public MedicalItemType itemType;
        public float startTime;
        public float healingTime;
        public float healAmount;
    }

    public class HealingItemData
    {
        public float useTime;
        public float healAmount;
    }

    public enum InjuryType
    {
        Bleeding,       // Damage over time
        Fracture,       // Reduced movement speed
        Concussion,     // Vision/aim penalty
        Burns,          // DOT + infection risk
        Infection,      // Reduces max health
        Poisoned        // Stat penalties
    }

    public enum InjurySeverity
    {
        Minor,
        Moderate,
        Severe
    }

    public enum MedicalItemType
    {
        Bandage,        // Stops bleeding, small heal
        Medkit,         // Large heal, removes injuries
        Stimpack,       // Medium heal + speed boost
        Painkiller,     // Small heal + damage resistance
        Adrenaline,     // Instant partial heal
        SurgeryKit,     // Removes severe injuries
        Antidote        // Removes poison/infection
    }

    #endregion
}
