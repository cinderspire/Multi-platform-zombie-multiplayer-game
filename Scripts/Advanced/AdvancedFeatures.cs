using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Advanced
{
    // PHOTO MODE SYSTEM
    public class PhotoModeSystem : MonoBehaviour
    {
        public static PhotoModeSystem Instance { get; private set; }
        
        [SerializeField] private Camera photoCamera;
        [SerializeField] private bool isActive;
        
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F6))
            {
                TogglePhotoMode();
            }
        }

        public void TogglePhotoMode()
        {
            isActive = !isActive;
            Time.timeScale = isActive ? 0f : 1f;
            
            if (photoCamera != null)
            {
                photoCamera.enabled = isActive;
            }
        }

        public void TakeScreenshot()
        {
            string filename = $"Screenshot_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
            ScreenCapture.CaptureScreenshot(filename);
            UnityEngine.Debug.Log($"Screenshot saved: {filename}");
        }
    }

    // LOCALIZATION SYSTEM
    public class LocalizationSystem : MonoBehaviour
    {
        public static LocalizationSystem Instance { get; private set; }
        
        private Dictionary<string, Dictionary<string, string>> translations = new Dictionary<string, Dictionary<string, string>>();
        private string currentLanguage = "en";
        
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            LoadTranslations();
        }

        private void LoadTranslations()
        {
            translations["en"] = new Dictionary<string, string>
            {
                {"start_game", "Start Game"},
                {"settings", "Settings"},
                {"quit", "Quit"}
            };
            
            translations["tr"] = new Dictionary<string, string>
            {
                {"start_game", "Oyuna Başla"},
                {"settings", "Ayarlar"},
                {"quit", "Çıkış"}
            };
        }

        public string GetText(string key)
        {
            if (translations.TryGetValue(currentLanguage, out var lang))
            {
                return lang.GetValueOrDefault(key, key);
            }
            return key;
        }

        public void SetLanguage(string lang)
        {
            currentLanguage = lang;
        }
    }

    // ANTI-CHEAT SYSTEM
    public class AntiCheatSystem : NetworkBehaviour
    {
        public static AntiCheatSystem Instance { get; private set; }
        
        private Dictionary<ulong, PlayerValidation> playerValidations = new Dictionary<ulong, PlayerValidation>();
        
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        [ServerRpc(RequireOwnership = false)]
        public void ValidatePlayerActionServerRpc(ulong playerId, Vector3 position, float speed, ServerRpcParams rpcParams = default)
        {
            if (!playerValidations.ContainsKey(playerId))
            {
                playerValidations[playerId] = new PlayerValidation { playerId = playerId };
            }

            var validation = playerValidations[playerId];
            
            // Check for speed hacks
            if (speed > 20f)
            {
                UnityEngine.Debug.LogWarning($"Player {playerId} suspected of speed hacking!");
                // Take action
            }

            validation.lastPosition = position;
            validation.lastValidationTime = Time.time;
        }
    }

    [Serializable]
    public class PlayerValidation
    {
        public ulong playerId;
        public Vector3 lastPosition;
        public float lastValidationTime;
        public int violationCount;
    }

    // DYNAMIC EVENTS SYSTEM
    public class DynamicEventsSystem : NetworkBehaviour
    {
        public static DynamicEventsSystem Instance { get; private set; }
        
        [SerializeField] private float eventCheckInterval = 60f;
        [SerializeField] private float eventChance = 0.3f;
        
        private float lastEventCheck;
        
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Update()
        {
            if (!IsServer) return;
            
            if (Time.time - lastEventCheck >= eventCheckInterval)
            {
                CheckForEvent();
                lastEventCheck = Time.time;
            }
        }

        private void CheckForEvent()
        {
            if (UnityEngine.Random.value <= eventChance)
            {
                TriggerRandomEvent();
            }
        }

        private void TriggerRandomEvent()
        {
            int eventType = UnityEngine.Random.Range(0, 5);
            
            switch (eventType)
            {
                case 0: // Supply Drop
                    UnityEngine.Debug.Log("EVENT: Supply Drop incoming!");
                    break;
                case 1: // Zombie Horde
                    UnityEngine.Debug.Log("EVENT: Zombie Horde spawning!");
                    break;
                case 2: // Merchant Visit
                    UnityEngine.Debug.Log("EVENT: Traveling Merchant arrived!");
                    break;
                case 3: // Blood Moon
                    UnityEngine.Debug.Log("EVENT: Blood Moon rising!");
                    break;
                case 4: // Rescue Mission
                    UnityEngine.Debug.Log("EVENT: Survivors need rescue!");
                    break;
            }
        }
    }

    // REPLAY SYSTEM
    public class ReplaySystem : MonoBehaviour
    {
        public static ReplaySystem Instance { get; private set; }
        
        private List<ReplayFrame> recordedFrames = new List<ReplayFrame>();
        private bool isRecording;
        private bool isReplaying;
        
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void StartRecording()
        {
            isRecording = true;
            recordedFrames.Clear();
        }

        public void StopRecording()
        {
            isRecording = false;
        }

        public void PlayReplay()
        {
            isReplaying = true;
            // Play back recorded frames
        }
    }

    [Serializable]
    public class ReplayFrame
    {
        public float timestamp;
        public Dictionary<ulong, Vector3> playerPositions;
    }

    // TOURNAMENT SYSTEM
    public class TournamentSystem : NetworkBehaviour
    {
        public static TournamentSystem Instance { get; private set; }
        
        private List<Tournament> activeTournaments = new List<Tournament>();
        
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        [ServerRpc(RequireOwnership = false)]
        public void CreateTournamentServerRpc(string name, int maxTeams, ServerRpcParams rpcParams = default)
        {
            Tournament tournament = new Tournament
            {
                tournamentId = Guid.NewGuid().ToString(),
                tournamentName = name,
                maxTeams = maxTeams,
                status = TournamentStatus.Registration
            };

            activeTournaments.Add(tournament);
        }
    }

    [Serializable]
    public class Tournament
    {
        public string tournamentId;
        public string tournamentName;
        public int maxTeams;
        public TournamentStatus status;
        public List<ulong> participants = new List<ulong>();
    }

    public enum TournamentStatus { Registration, InProgress, Completed }

    // RAID SYSTEM
    public class RaidSystem : NetworkBehaviour
    {
        public static RaidSystem Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        [ServerRpc(RequireOwnership = false)]
        public void StartRaidServerRpc(string attackingClan, string defendingClan, ServerRpcParams rpcParams = default)
        {
            UnityEngine.Debug.Log($"Raid started: {attackingClan} vs {defendingClan}");
            // Implement raid mechanics
        }
    }

    // SEASON SYSTEM
    public class SeasonSystem : NetworkBehaviour
    {
        public static SeasonSystem Instance { get; private set; }
        
        private NetworkVariable<int> currentSeason = new NetworkVariable<int>(1);
        
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        [ServerRpc(RequireOwnership = false)]
        public void AdvanceSeasonServerRpc(ServerRpcParams rpcParams = default)
        {
            currentSeason.Value++;
            UnityEngine.Debug.Log($"Advanced to Season {currentSeason.Value}");
        }

        public int CurrentSeason => currentSeason.Value;
    }
}
