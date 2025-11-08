using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;
using System;

namespace DeadFrontier.Social
{
    /// <summary>
    /// Comprehensive gesture and emote system for player expression.
    /// Supports categorized emotes, unlock progression, premium emotes, and network synchronization.
    /// Includes emote wheel UI, quick slots, and voice line integration.
    /// </summary>
    public class EmoteSystem : NetworkBehaviour
    {
        public static EmoteSystem Instance { get; private set; }

        [Header("Emote Settings")]
        [SerializeField] private EmoteData[] availableEmotes;
        [SerializeField] private int maxQuickSlots = 4;
        [SerializeField] private float emoteGlobalCooldown = 1f;
        [SerializeField] private float maxEmoteDuration = 10f;

        [Header("Unlock Settings")]
        [SerializeField] private bool enableEmoteUnlocks = true;
        [SerializeField] private EmoteUnlockData[] unlockableEmotes;

        [Header("UI References")]
        [SerializeField] private GameObject emoteWheelPrefab;
        [SerializeField] private KeyCode emoteWheelKey = KeyCode.B;

        [Header("Audio")]
        [SerializeField] private AudioSource voiceLineSource;

        // Player emote data
        private Dictionary<ulong, PlayerEmoteData> playerEmoteData = new Dictionary<ulong, PlayerEmoteData>();
        private Dictionary<ulong, ActiveEmote> activeEmotes = new Dictionary<ulong, ActiveEmote>();

        // Cooldowns
        private Dictionary<ulong, float> globalCooldowns = new Dictionary<ulong, float>();
        private Dictionary<ulong, Dictionary<string, float>> emoteCooldowns = new Dictionary<ulong, Dictionary<string, float>>();

        // UI
        private GameObject emoteWheelInstance;
        private bool isEmoteWheelOpen;

        // Events
        public event Action<ulong, EmoteData> OnEmoteStarted;
        public event Action<ulong, EmoteData> OnEmoteCompleted;
        public event Action<ulong, EmoteData> OnEmoteCancelled;
        public event Action<ulong, string> OnEmoteUnlocked;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            LoadAllPlayerData();
        }

        private void Update()
        {
            if (IsOwner)
            {
                HandleEmoteInput();
                UpdateActiveEmotes();
            }
        }

        #region Input Handling

        private void HandleEmoteInput()
        {
            ulong localPlayerId = NetworkManager.Singleton.LocalClientId;

            // Emote wheel
            if (Input.GetKeyDown(emoteWheelKey))
            {
                OpenEmoteWheel();
            }
            else if (Input.GetKeyUp(emoteWheelKey))
            {
                CloseEmoteWheel();
            }

            // Quick slots (F1-F4)
            if (Input.GetKeyDown(KeyCode.F1))
                PlayQuickSlotEmote(localPlayerId, 0);
            else if (Input.GetKeyDown(KeyCode.F2))
                PlayQuickSlotEmote(localPlayerId, 1);
            else if (Input.GetKeyDown(KeyCode.F3))
                PlayQuickSlotEmote(localPlayerId, 2);
            else if (Input.GetKeyDown(KeyCode.F4))
                PlayQuickSlotEmote(localPlayerId, 3);

            // Cancel emote
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space))
            {
                if (IsPlayingEmote(localPlayerId))
                {
                    CancelEmote(localPlayerId);
                }
            }
        }

        private void PlayQuickSlotEmote(ulong playerId, int slotIndex)
        {
            if (!playerEmoteData.ContainsKey(playerId))
            {
                InitializePlayerEmoteData(playerId);
            }

            var data = playerEmoteData[playerId];

            if (slotIndex < 0 || slotIndex >= data.quickSlots.Length) return;

            string emoteId = data.quickSlots[slotIndex];
            if (string.IsNullOrEmpty(emoteId)) return;

            PlayEmote(playerId, emoteId);
        }

        #endregion

        #region Emote Playback

        public bool CanPlayEmote(ulong playerId, string emoteId)
        {
            // Check global cooldown
            if (globalCooldowns.ContainsKey(playerId) && Time.time < globalCooldowns[playerId])
            {
                return false;
            }

            // Check emote-specific cooldown
            if (emoteCooldowns.ContainsKey(playerId) &&
                emoteCooldowns[playerId].ContainsKey(emoteId) &&
                Time.time < emoteCooldowns[playerId][emoteId])
            {
                return false;
            }

            // Check if emote is unlocked
            if (enableEmoteUnlocks && !IsEmoteUnlocked(playerId, emoteId))
            {
                return false;
            }

            // Check if already playing an emote
            if (activeEmotes.ContainsKey(playerId))
            {
                return false;
            }

            return true;
        }

        public void PlayEmote(ulong playerId, string emoteId)
        {
            if (!CanPlayEmote(playerId, emoteId))
            {
                Debug.LogWarning($"[EmoteSystem] Cannot play emote {emoteId} for player {playerId}");
                return;
            }

            var emoteData = GetEmoteData(emoteId);
            if (emoteData == null)
            {
                Debug.LogError($"[EmoteSystem] Emote {emoteId} not found");
                return;
            }

            // Request server to play emote
            if (IsServer)
            {
                StartEmoteServerSide(playerId, emoteData);
            }
            else
            {
                RequestPlayEmoteServerRpc(playerId, emoteId);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestPlayEmoteServerRpc(ulong playerId, string emoteId)
        {
            var emoteData = GetEmoteData(emoteId);
            if (emoteData != null && CanPlayEmote(playerId, emoteId))
            {
                StartEmoteServerSide(playerId, emoteData);
            }
        }

        private void StartEmoteServerSide(ulong playerId, EmoteData emoteData)
        {
            var activeEmote = new ActiveEmote
            {
                playerId = playerId,
                emoteData = emoteData,
                startTime = Time.time,
                duration = emoteData.duration
            };

            activeEmotes[playerId] = activeEmote;

            // Set cooldowns
            globalCooldowns[playerId] = Time.time + emoteGlobalCooldown;

            if (emoteData.cooldown > 0f)
            {
                if (!emoteCooldowns.ContainsKey(playerId))
                {
                    emoteCooldowns[playerId] = new Dictionary<string, float>();
                }

                emoteCooldowns[playerId][emoteData.emoteId] = Time.time + emoteData.cooldown;
            }

            // Notify clients
            PlayEmoteClientRpc(playerId, emoteData.emoteId);

            OnEmoteStarted?.Invoke(playerId, emoteData);

            Debug.Log($"[EmoteSystem] Player {playerId} started emote: {emoteData.emoteName}");
        }

        [ClientRpc]
        private void PlayEmoteClientRpc(ulong playerId, string emoteId)
        {
            var emoteData = GetEmoteData(emoteId);
            if (emoteData == null) return;

            // Play animation
            PlayEmoteAnimation(playerId, emoteData);

            // Play voice line
            if (emoteData.voiceLineClip != null)
            {
                PlayVoiceLine(playerId, emoteData.voiceLineClip);
            }

            // Play sound effect
            if (emoteData.soundEffect != null)
            {
                PlaySoundEffect(playerId, emoteData.soundEffect);
            }

            // Spawn visual effect
            if (emoteData.visualEffect != null)
            {
                SpawnVisualEffect(playerId, emoteData.visualEffect);
            }
        }

        private void PlayEmoteAnimation(ulong playerId, EmoteData emoteData)
        {
            // Get player animator
            // This would find the player object and trigger the animation
            // For now, just a placeholder

            Debug.Log($"[EmoteSystem] Playing animation: {emoteData.animationTrigger}");
        }

        private void PlayVoiceLine(ulong playerId, AudioClip voiceClip)
        {
            if (voiceLineSource != null)
            {
                voiceLineSource.PlayOneShot(voiceClip);
            }
        }

        private void PlaySoundEffect(ulong playerId, AudioClip soundEffect)
        {
            // Play sound at player position
            // This would use audio system
        }

        private void SpawnVisualEffect(ulong playerId, GameObject effectPrefab)
        {
            // Spawn particle effect at player position
            // This would integrate with VFX system
        }

        public void CancelEmote(ulong playerId)
        {
            if (!activeEmotes.ContainsKey(playerId)) return;

            var activeEmote = activeEmotes[playerId];

            // Check if emote is cancellable
            if (!activeEmote.emoteData.isCancellable)
            {
                Debug.LogWarning($"[EmoteSystem] Emote {activeEmote.emoteData.emoteName} is not cancellable");
                return;
            }

            if (IsServer)
            {
                CancelEmoteServerSide(playerId);
            }
            else
            {
                RequestCancelEmoteServerRpc(playerId);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestCancelEmoteServerRpc(ulong playerId)
        {
            CancelEmoteServerSide(playerId);
        }

        private void CancelEmoteServerSide(ulong playerId)
        {
            if (!activeEmotes.ContainsKey(playerId)) return;

            var activeEmote = activeEmotes[playerId];
            activeEmotes.Remove(playerId);

            CancelEmoteClientRpc(playerId);

            OnEmoteCancelled?.Invoke(playerId, activeEmote.emoteData);

            Debug.Log($"[EmoteSystem] Cancelled emote for player {playerId}");
        }

        [ClientRpc]
        private void CancelEmoteClientRpc(ulong playerId)
        {
            // Stop animation
            // Reset player state
        }

        private void UpdateActiveEmotes()
        {
            if (!IsServer) return;

            var emotesToComplete = new List<ulong>();

            foreach (var kvp in activeEmotes)
            {
                var playerId = kvp.Key;
                var activeEmote = kvp.Value;

                float elapsed = Time.time - activeEmote.startTime;

                if (elapsed >= activeEmote.duration)
                {
                    emotesToComplete.Add(playerId);
                }
            }

            foreach (var playerId in emotesToComplete)
            {
                CompleteEmote(playerId);
            }
        }

        private void CompleteEmote(ulong playerId)
        {
            if (!activeEmotes.ContainsKey(playerId)) return;

            var activeEmote = activeEmotes[playerId];
            activeEmotes.Remove(playerId);

            OnEmoteCompleted?.Invoke(playerId, activeEmote.emoteData);

            Debug.Log($"[EmoteSystem] Completed emote for player {playerId}");
        }

        #endregion

        #region Emote Unlocking

        public bool IsEmoteUnlocked(ulong playerId, string emoteId)
        {
            if (!enableEmoteUnlocks) return true;

            if (!playerEmoteData.ContainsKey(playerId))
            {
                InitializePlayerEmoteData(playerId);
            }

            var data = playerEmoteData[playerId];

            // Check if in unlocked list
            if (data.unlockedEmotes.Contains(emoteId)) return true;

            // Check if it's a default emote
            var emoteData = GetEmoteData(emoteId);
            if (emoteData != null && emoteData.isDefaultUnlocked) return true;

            return false;
        }

        public bool UnlockEmote(ulong playerId, string emoteId, bool skipCost = false)
        {
            if (!enableEmoteUnlocks) return false;

            if (IsEmoteUnlocked(playerId, emoteId)) return false;

            var unlockData = GetUnlockData(emoteId);
            if (unlockData == null) return false;

            // Check requirements
            if (!skipCost)
            {
                // Check level requirement
                if (Progression.AchievementManager.Instance != null)
                {
                    int playerLevel = Progression.AchievementManager.Instance.GetPlayerLevel();
                    if (playerLevel < unlockData.requiredLevel) return false;
                }

                // Check currency cost
                if (unlockData.currencyCost > 0 && Economy.EconomyManager.Instance != null)
                {
                    if (unlockData.isPremium)
                    {
                        if (!Economy.EconomyManager.Instance.SpendHardCurrency(unlockData.currencyCost, $"Unlock Emote: {emoteId}"))
                            return false;
                    }
                    else
                    {
                        if (!Economy.EconomyManager.Instance.SpendSoftCurrency(unlockData.currencyCost, $"Unlock Emote: {emoteId}"))
                            return false;
                    }
                }
            }

            // Unlock emote
            if (!playerEmoteData.ContainsKey(playerId))
            {
                InitializePlayerEmoteData(playerId);
            }

            var data = playerEmoteData[playerId];
            data.unlockedEmotes.Add(emoteId);

            SavePlayerEmoteData(playerId);

            OnEmoteUnlocked?.Invoke(playerId, emoteId);

            Debug.Log($"[EmoteSystem] Unlocked emote {emoteId} for player {playerId}");

            return true;
        }

        private EmoteUnlockData GetUnlockData(string emoteId)
        {
            return unlockableEmotes.FirstOrDefault(u => u.emoteId == emoteId);
        }

        #endregion

        #region Quick Slots

        public void SetQuickSlot(ulong playerId, int slotIndex, string emoteId)
        {
            if (!playerEmoteData.ContainsKey(playerId))
            {
                InitializePlayerEmoteData(playerId);
            }

            var data = playerEmoteData[playerId];

            if (slotIndex < 0 || slotIndex >= maxQuickSlots) return;

            // Check if emote is unlocked
            if (!string.IsNullOrEmpty(emoteId) && !IsEmoteUnlocked(playerId, emoteId))
            {
                Debug.LogWarning($"[EmoteSystem] Cannot set locked emote {emoteId} to quick slot");
                return;
            }

            data.quickSlots[slotIndex] = emoteId;

            SavePlayerEmoteData(playerId);

            Debug.Log($"[EmoteSystem] Set quick slot {slotIndex} to {emoteId} for player {playerId}");
        }

        public string GetQuickSlot(ulong playerId, int slotIndex)
        {
            if (!playerEmoteData.ContainsKey(playerId)) return null;

            var data = playerEmoteData[playerId];

            if (slotIndex < 0 || slotIndex >= maxQuickSlots) return null;

            return data.quickSlots[slotIndex];
        }

        #endregion

        #region Emote Wheel UI

        private void OpenEmoteWheel()
        {
            if (isEmoteWheelOpen) return;

            if (emoteWheelPrefab != null && emoteWheelInstance == null)
            {
                emoteWheelInstance = Instantiate(emoteWheelPrefab);
                // Configure emote wheel with unlocked emotes
            }

            isEmoteWheelOpen = true;

            // Pause player controls
            Time.timeScale = 0.5f; // Slow time for selection

            Debug.Log("[EmoteSystem] Opened emote wheel");
        }

        private void CloseEmoteWheel()
        {
            if (!isEmoteWheelOpen) return;

            isEmoteWheelOpen = false;

            // Resume normal time
            Time.timeScale = 1f;

            // Get selected emote from wheel and play it
            // This would integrate with UI system

            Debug.Log("[EmoteSystem] Closed emote wheel");
        }

        #endregion

        #region Data Management

        private void InitializePlayerEmoteData(ulong playerId)
        {
            if (playerEmoteData.ContainsKey(playerId)) return;

            var data = new PlayerEmoteData
            {
                playerId = playerId,
                unlockedEmotes = new List<string>(),
                quickSlots = new string[maxQuickSlots]
            };

            // Unlock default emotes
            foreach (var emote in availableEmotes)
            {
                if (emote.isDefaultUnlocked)
                {
                    data.unlockedEmotes.Add(emote.emoteId);
                }
            }

            playerEmoteData[playerId] = data;
        }

        private EmoteData GetEmoteData(string emoteId)
        {
            return availableEmotes.FirstOrDefault(e => e.emoteId == emoteId);
        }

        public List<EmoteData> GetUnlockedEmotes(ulong playerId)
        {
            if (!playerEmoteData.ContainsKey(playerId))
            {
                InitializePlayerEmoteData(playerId);
            }

            var data = playerEmoteData[playerId];

            return availableEmotes.Where(e => IsEmoteUnlocked(playerId, e.emoteId)).ToList();
        }

        public List<EmoteData> GetEmotesByCategory(EmoteCategory category)
        {
            return availableEmotes.Where(e => e.category == category).ToList();
        }

        public bool IsPlayingEmote(ulong playerId)
        {
            return activeEmotes.ContainsKey(playerId);
        }

        public ActiveEmote GetActiveEmote(ulong playerId)
        {
            return activeEmotes.ContainsKey(playerId) ? activeEmotes[playerId] : null;
        }

        #endregion

        #region Persistence

        private void LoadAllPlayerData()
        {
            Debug.Log("[EmoteSystem] Ready to load player emote data");
        }

        public void LoadPlayerEmoteData(ulong playerId)
        {
            if (Core.SaveSystem.Instance == null) return;

            string savedData = Core.SaveSystem.Instance.LoadData($"emotes_{playerId}");
            if (!string.IsNullOrEmpty(savedData))
            {
                try
                {
                    var emoteSave = JsonUtility.FromJson<EmoteSaveData>(savedData);

                    var data = new PlayerEmoteData
                    {
                        playerId = playerId,
                        unlockedEmotes = new List<string>(emoteSave.unlockedEmotes),
                        quickSlots = emoteSave.quickSlots
                    };

                    playerEmoteData[playerId] = data;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[EmoteSystem] Error loading emote data for player {playerId}: {e.Message}");
                    InitializePlayerEmoteData(playerId);
                }
            }
            else
            {
                InitializePlayerEmoteData(playerId);
            }
        }

        private void SavePlayerEmoteData(ulong playerId)
        {
            if (Core.SaveSystem.Instance == null) return;
            if (!playerEmoteData.ContainsKey(playerId)) return;

            var data = playerEmoteData[playerId];

            var emoteSave = new EmoteSaveData
            {
                unlockedEmotes = new List<string>(data.unlockedEmotes),
                quickSlots = data.quickSlots
            };

            string json = JsonUtility.ToJson(emoteSave);
            Core.SaveSystem.Instance.SaveData($"emotes_{playerId}", json);
        }

        #endregion
    }

    #region Data Classes

    public class PlayerEmoteData
    {
        public ulong playerId;
        public List<string> unlockedEmotes;
        public string[] quickSlots;
    }

    public class ActiveEmote
    {
        public ulong playerId;
        public EmoteData emoteData;
        public float startTime;
        public float duration;
    }

    [System.Serializable]
    public class EmoteData
    {
        public string emoteId;
        public string emoteName;
        [TextArea(2, 3)]
        public string description;

        public EmoteCategory category;
        public float duration = 3f;
        public float cooldown = 0f;
        public bool isCancellable = true;
        public bool isDefaultUnlocked = false;

        [Header("Animation")]
        public string animationTrigger;
        public AnimationClip animationClip;

        [Header("Audio")]
        public AudioClip voiceLineClip;
        public AudioClip soundEffect;

        [Header("Visual")]
        public GameObject visualEffect;
        public Sprite emoteIcon;
    }

    [System.Serializable]
    public class EmoteUnlockData
    {
        public string emoteId;
        public int requiredLevel = 1;
        public int currencyCost = 0;
        public bool isPremium = false;
        public string unlockDescription;
    }

    [System.Serializable]
    public class EmoteSaveData
    {
        public List<string> unlockedEmotes;
        public string[] quickSlots;
    }

    public enum EmoteCategory
    {
        Friendly,       // Wave, thumbs up, etc.
        Tactical,       // Point, hold position, etc.
        Taunt,          // Mocking gestures
        Dance,          // Dance moves
        Celebration,    // Victory poses
        Reaction,       // Laugh, cry, scared
        Team,           // Team-specific emotes
        Seasonal,       // Event/season emotes
        Premium         // Premium purchasable emotes
    }

    #endregion
}
