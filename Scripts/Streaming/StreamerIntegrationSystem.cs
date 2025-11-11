using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace ZombieGame
{
    /// <summary>
    /// Streamer Integration System - Twitch/YouTube integration for content creators
    /// Features: Chat commands, viewer participation, stream overlays, drops
    /// Supports Twitch Extensions, chat voting, and viewer challenges
    /// </summary>
    public class StreamerIntegrationSystem : NetworkBehaviour
    {
        public static StreamerIntegrationSystem Instance { get; private set; }

        [Header("Streamer Settings")]
        [SerializeField] private bool enableStreamerMode = false;
        [SerializeField] private bool hidePersonalInfo = true; // Hide player names, etc.
        [SerializeField] private bool enableChatCommands = true;

        [Header("Platform Integration")]
        [SerializeField] private string twitchChannelName = "";
        [SerializeField] private string youtubeChannelId = "";

        // Chat Commands
        private Dictionary<string, ChatCommand> chatCommands = new Dictionary<string, ChatCommand>();

        // Viewer Participation
        private List<ViewerParticipationEvent> activeEvents = new List<ViewerParticipationEvent>();

        // Stream Stats
        private StreamerStats currentStats = new StreamerStats();

        // Drops & Rewards
        private Dictionary<string, StreamDrop> streamDrops = new Dictionary<string, StreamDrop>();

        // Events
        public event System.Action<string, string> OnChatCommand; // command, user
        public event System.Action<ViewerVoteResult> OnViewerVoteComplete;
        public event System.Action<string> OnStreamDropClaimed;

        [System.Serializable]
        public class ChatCommand
        {
            public string command; // !help, !stats, !kill
            public string description;
            public CommandPermission permission;
            public float cooldown = 0f;
            public System.Action<string> action; // Action to perform
            public float lastUsedTime = 0f;
        }

        public enum CommandPermission
        {
            Everyone,
            Subscribers,
            Mods,
            StreamerOnly
        }

        [System.Serializable]
        public class ViewerParticipationEvent
        {
            public string eventId;
            public ViewerEventType eventType;
            public float duration;
            public float startTime;
            public Dictionary<string, int> votes = new Dictionary<string, int>(); // option -> vote count
        }

        public enum ViewerEventType
        {
            VoteNextWeapon,     // Viewers vote on streamer's weapon
            VoteChallenge,      // Choose difficulty challenge
            SpawnBoss,          // Trigger boss spawn
            HordeMode,          // Activate horde
            CarePackage,        // Drop loot
            RandomEvent         // Chaos
        }

        [System.Serializable]
        public class ViewerVoteResult
        {
            public string eventId;
            public string winningOption;
            public int totalVotes;
            public Dictionary<string, int> voteBreakdown;
        }

        [System.Serializable]
        public class StreamerStats
        {
            public int totalViewers = 0;
            public int peakViewers = 0;
            public int totalFollowers = 0;
            public int totalSubscribers = 0;
            public float sessionDuration = 0f;
            public int chatMessagesReceived = 0;
            public int commandsExecuted = 0;
            public int dropsEarned = 0;
        }

        [System.Serializable]
        public class StreamDrop
        {
            public string dropId;
            public string dropName;
            public string description;
            public DropRarity rarity;
            public int watchTimeRequired; // Minutes
            public List<string> rewardItems = new List<string>();
            public bool isClaimed = false;
        }

        public enum DropRarity
        {
            Common,     // 30 min watch
            Rare,       // 1 hour watch
            Epic,       // 2 hours watch
            Legendary   // 4 hours watch
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeChatCommands();
                InitializeStreamDrops();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeChatCommands()
        {
            // Public Commands
            RegisterCommand(new ChatCommand
            {
                command = "!help",
                description = "Show available commands",
                permission = CommandPermission.Everyone,
                cooldown = 10f,
                action = (user) => ShowHelp(user)
            });

            RegisterCommand(new ChatCommand
            {
                command = "!stats",
                description = "Show streamer's stats",
                permission = CommandPermission.Everyone,
                cooldown = 30f,
                action = (user) => ShowStats(user)
            });

            RegisterCommand(new ChatCommand
            {
                command = "!vote",
                description = "Vote in active poll",
                permission = CommandPermission.Everyone,
                cooldown = 0f,
                action = (user) => ProcessVote(user)
            });

            // Subscriber Commands
            RegisterCommand(new ChatCommand
            {
                command = "!horde",
                description = "Trigger zombie horde (Subs only)",
                permission = CommandPermission.Subscribers,
                cooldown = 300f, // 5 minutes
                action = (user) => TriggerHorde(user)
            });

            RegisterCommand(new ChatCommand
            {
                command = "!boss",
                description = "Spawn a boss (Subs only)",
                permission = CommandPermission.Subscribers,
                cooldown = 600f, // 10 minutes
                action = (user) => SpawnBoss(user)
            });

            // Mod Commands
            RegisterCommand(new ChatCommand
            {
                command = "!heal",
                description = "Heal the streamer (Mods only)",
                permission = CommandPermission.Mods,
                cooldown = 120f,
                action = (user) => HealStreamer(user)
            });

            RegisterCommand(new ChatCommand
            {
                command = "!weapon",
                description = "Give random weapon (Mods only)",
                permission = CommandPermission.Mods,
                cooldown = 180f,
                action = (user) => GiveRandomWeapon(user)
            });

            // Streamer Only
            RegisterCommand(new ChatCommand
            {
                command = "!godmode",
                description = "Toggle god mode",
                permission = CommandPermission.StreamerOnly,
                cooldown = 0f,
                action = (user) => ToggleGodMode(user)
            });

            Debug.Log($"[StreamerIntegration] Registered {chatCommands.Count} chat commands");
        }

        private void InitializeStreamDrops()
        {
            // Common Drop (30 min)
            streamDrops["drop_common_1"] = new StreamDrop
            {
                dropId = "drop_common_1",
                dropName = "Starter Pack",
                description = "Basic supplies for beginners",
                rarity = DropRarity.Common,
                watchTimeRequired = 30,
                rewardItems = new List<string> { "medkit", "ammo_100", "currency_500" }
            };

            // Rare Drop (1 hour)
            streamDrops["drop_rare_1"] = new StreamDrop
            {
                dropId = "drop_rare_1",
                dropName = "Survivor Cache",
                description = "Quality gear for survivors",
                rarity = DropRarity.Rare,
                watchTimeRequired = 60,
                rewardItems = new List<string> { "weapon_epic", "armor_rare", "currency_1000" }
            };

            // Epic Drop (2 hours)
            streamDrops["drop_epic_1"] = new StreamDrop
            {
                dropId = "drop_epic_1",
                dropName = "Elite Arsenal",
                description = "High-tier weapons and gear",
                rarity = DropRarity.Epic,
                watchTimeRequired = 120,
                rewardItems = new List<string> { "weapon_legendary", "skin_exclusive", "currency_2500" }
            };

            // Legendary Drop (4 hours)
            streamDrops["drop_legendary_1"] = new StreamDrop
            {
                dropId = "drop_legendary_1",
                dropName = "Streamer's Blessing",
                description = "Ultimate rewards for dedicated viewers",
                rarity = DropRarity.Legendary,
                watchTimeRequired = 240,
                rewardItems = new List<string> { "weapon_mythic", "emote_exclusive", "title_streamer_supporter", "currency_5000" }
            };

            Debug.Log($"[StreamerIntegration] Initialized {streamDrops.Count} stream drops");
        }

        private void RegisterCommand(ChatCommand command)
        {
            chatCommands[command.command] = command;
        }

        // Chat Command Handlers

        private void ShowHelp(string user)
        {
            Debug.Log($"[Chat] {user} requested help");
            // Send chat message with available commands
        }

        private void ShowStats(string user)
        {
            Debug.Log($"[Chat] {user} requested stats");
            // Display streamer's current stats in chat
        }

        private void ProcessVote(string user)
        {
            // Process viewer vote for active event
            if (activeEvents.Count == 0)
            {
                Debug.Log($"[Chat] No active votes");
                return;
            }

            Debug.Log($"[Chat] {user} voted");
        }

        private void TriggerHorde(string user)
        {
            if (!IsServer) return;

            Debug.Log($"[StreamerIntegration] {user} triggered HORDE!");

            // Trigger horde via AI Director
            if (AIDirectorSystem.Instance != null)
            {
                // AIDirectorSystem.Instance.TriggerHorde();
            }
        }

        private void SpawnBoss(string user)
        {
            if (!IsServer) return;

            Debug.Log($"[StreamerIntegration] {user} spawned BOSS!");

            // Spawn boss zombie
            if (SpawnerSystem.Instance != null)
            {
                // SpawnerSystem.Instance.SpawnBossZombie();
            }
        }

        private void HealStreamer(string user)
        {
            if (!IsServer) return;

            Debug.Log($"[StreamerIntegration] {user} healed streamer");

            // Heal streamer's character
            // HealthSystem.Instance.Heal(streamerPlayerId, 50f);
        }

        private void GiveRandomWeapon(string user)
        {
            if (!IsServer) return;

            Debug.Log($"[StreamerIntegration] {user} gave random weapon");

            // Give random weapon to streamer
        }

        private void ToggleGodMode(string user)
        {
            if (!IsServer) return;

            Debug.Log($"[StreamerIntegration] {user} toggled god mode");

            // Toggle invincibility for streamer
        }

        // Viewer Participation

        public void StartViewerVote(ViewerEventType eventType, string[] options, float duration)
        {
            string eventId = System.Guid.NewGuid().ToString();

            var voteEvent = new ViewerParticipationEvent
            {
                eventId = eventId,
                eventType = eventType,
                duration = duration,
                startTime = Time.time
            };

            foreach (string option in options)
            {
                voteEvent.votes[option] = 0;
            }

            activeEvents.Add(voteEvent);

            Debug.Log($"[StreamerIntegration] Started vote: {eventType} for {duration}s");
        }

        private void Update()
        {
            if (!IsServer) return;

            UpdateActiveVotes();
        }

        private void UpdateActiveVotes()
        {
            var completedEvents = new List<string>();

            foreach (var voteEvent in activeEvents)
            {
                if (Time.time >= voteEvent.startTime + voteEvent.duration)
                {
                    completedEvents.Add(voteEvent.eventId);
                    CompleteVote(voteEvent);
                }
            }

            activeEvents.RemoveAll(e => completedEvents.Contains(e.eventId));
        }

        private void CompleteVote(ViewerParticipationEvent voteEvent)
        {
            // Determine winner
            string winningOption = voteEvent.votes.OrderByDescending(v => v.Value).First().Key;
            int totalVotes = voteEvent.votes.Values.Sum();

            var result = new ViewerVoteResult
            {
                eventId = voteEvent.eventId,
                winningOption = winningOption,
                totalVotes = totalVotes,
                voteBreakdown = voteEvent.votes
            };

            OnViewerVoteComplete?.Invoke(result);

            // Execute vote result
            ExecuteVoteResult(voteEvent.eventType, winningOption);

            Debug.Log($"[StreamerIntegration] Vote complete! Winner: {winningOption} ({totalVotes} votes)");
        }

        private void ExecuteVoteResult(ViewerEventType eventType, string result)
        {
            switch (eventType)
            {
                case ViewerEventType.VoteNextWeapon:
                    // Give voted weapon to streamer
                    Debug.Log($"[StreamerIntegration] Giving weapon: {result}");
                    break;

                case ViewerEventType.SpawnBoss:
                    // Spawn the voted boss type
                    Debug.Log($"[StreamerIntegration] Spawning boss: {result}");
                    break;

                case ViewerEventType.HordeMode:
                    TriggerHorde("Viewers");
                    break;

                case ViewerEventType.CarePackage:
                    // Drop loot package
                    Debug.Log($"[StreamerIntegration] Dropping care package");
                    break;
            }
        }

        // Stream Drops

        [ServerRpc(RequireOwnership = false)]
        public void ClaimStreamDropServerRpc(ulong playerId, string dropId, ServerRpcParams rpcParams = default)
        {
            if (!streamDrops.ContainsKey(dropId)) return;

            var drop = streamDrops[dropId];

            if (drop.isClaimed)
            {
                Debug.LogWarning($"[StreamerIntegration] Drop {dropId} already claimed");
                return;
            }

            // Grant rewards
            foreach (string rewardItem in drop.rewardItems)
            {
                // InventorySystem.Instance.AddItem(playerId, rewardItem);
            }

            drop.isClaimed = true;
            currentStats.dropsEarned++;

            OnStreamDropClaimed?.Invoke(dropId);
            Debug.Log($"[StreamerIntegration] Player {playerId} claimed drop: {drop.dropName}");
        }

        // Public API

        public void EnableStreamerMode(bool enabled)
        {
            enableStreamerMode = enabled;

            if (enabled)
            {
                Debug.Log("[StreamerIntegration] Streamer mode ENABLED");
                // Hide personal info, enable overlays
            }
            else
            {
                Debug.Log("[StreamerIntegration] Streamer mode DISABLED");
            }
        }

        public void SetTwitchChannel(string channelName)
        {
            twitchChannelName = channelName;
            // Initialize Twitch API connection
        }

        public void SetYouTubeChannel(string channelId)
        {
            youtubeChannelId = channelId;
            // Initialize YouTube API connection
        }

        public void ProcessChatMessage(string user, string message, string userRole)
        {
            if (!enableChatCommands) return;

            // Check if message is a command
            if (message.StartsWith("!"))
            {
                string command = message.Split(' ')[0].ToLower();

                if (chatCommands.ContainsKey(command))
                {
                    var cmd = chatCommands[command];

                    // Check permission
                    if (!HasPermission(userRole, cmd.permission))
                    {
                        return;
                    }

                    // Check cooldown
                    if (Time.time - cmd.lastUsedTime < cmd.cooldown)
                    {
                        float remainingCooldown = cmd.cooldown - (Time.time - cmd.lastUsedTime);
                        Debug.Log($"[Chat] Command on cooldown: {remainingCooldown:F0}s remaining");
                        return;
                    }

                    // Execute command
                    cmd.action?.Invoke(user);
                    cmd.lastUsedTime = Time.time;
                    currentStats.commandsExecuted++;

                    OnChatCommand?.Invoke(command, user);
                }
            }
        }

        private bool HasPermission(string userRole, CommandPermission required)
        {
            switch (required)
            {
                case CommandPermission.Everyone:
                    return true;

                case CommandPermission.Subscribers:
                    return userRole == "subscriber" || userRole == "mod" || userRole == "broadcaster";

                case CommandPermission.Mods:
                    return userRole == "mod" || userRole == "broadcaster";

                case CommandPermission.StreamerOnly:
                    return userRole == "broadcaster";

                default:
                    return false;
            }
        }

        // Getters

        public bool IsStreamerModeEnabled()
        {
            return enableStreamerMode;
        }

        public StreamerStats GetStats()
        {
            return currentStats;
        }

        public List<StreamDrop> GetAvailableDrops()
        {
            return streamDrops.Values.Where(d => !d.isClaimed).ToList();
        }

        public List<ViewerParticipationEvent> GetActiveEvents()
        {
            return activeEvents;
        }
    }
}
