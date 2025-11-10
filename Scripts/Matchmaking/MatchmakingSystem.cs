using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Matchmaking
{
    public class MatchmakingSystem : NetworkBehaviour
    {
        public static MatchmakingSystem Instance { get; private set; }

        [Header("Matchmaking Configuration")]
        [SerializeField] private int minPlayersPerMatch = 4;
        [SerializeField] private int maxPlayersPerMatch = 16;
        [SerializeField] private float matchmakingTimeout = 60f;
        [SerializeField] private int maxSkillDifference = 500;

        private Dictionary<string, MatchLobby> activeLobbies = new Dictionary<string, MatchLobby>();
        private Dictionary<ulong, PlayerMatchmakingData> playerQueue = new Dictionary<ulong, PlayerMatchmakingData>();
        private Dictionary<string, GameMode> gameModes = new Dictionary<string, GameMode>();

        public event Action<string> OnLobbyCreated;
        public event Action<ulong, string> OnPlayerJoinedLobby;
        public event Action<string> OnMatchStarted;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer) { InitializeGameModes(); InvokeRepeating(nameof(ProcessMatchmaking), 1f, 2f); }
        }

        private void InitializeGameModes()
        {
            gameModes["survival"] = new GameMode
            {
                modeId = "survival",
                modeName = "Survival",
                description = "Survive waves of zombies",
                minPlayers = 1,
                maxPlayers = 4,
                modeType = GameModeType.PvE,
                duration = 1800f,
                skillBased = false
            };

            gameModes["battle_royale"] = new GameMode
            {
                modeId = "battle_royale",
                modeName = "Battle Royale",
                description = "Last player/team standing wins",
                minPlayers = 16,
                maxPlayers = 100,
                modeType = GameModeType.PvP,
                duration = 1200f,
                skillBased = true
            };

            gameModes["team_deathmatch"] = new GameMode
            {
                modeId = "team_deathmatch",
                modeName = "Team Deathmatch",
                description = "Team vs Team combat",
                minPlayers = 8,
                maxPlayers = 16,
                modeType = GameModeType.PvP,
                duration = 600f,
                skillBased = true
            };

            gameModes["extraction"] = new GameMode
            {
                modeId = "extraction",
                modeName = "Extraction",
                description = "Extract with loot while fighting zombies and players",
                minPlayers = 4,
                maxPlayers = 12,
                modeType = GameModeType.PvPvE,
                duration = 1200f,
                skillBased = true
            };

            gameModes["horde"] = new GameMode
            {
                modeId = "horde",
                modeName = "Horde Mode",
                description = "Survive endless zombie waves",
                minPlayers = 1,
                maxPlayers = 8,
                modeType = GameModeType.PvE,
                duration = -1f,
                skillBased = false
            };
        }

        [ServerRpc(RequireOwnership = false)]
        public void JoinMatchmakingServerRpc(ulong playerId, string gameModeId, int skillRating, ServerRpcParams rpcParams = default)
        {
            if (!gameModes.TryGetValue(gameModeId, out var gameMode)) return;

            var playerData = new PlayerMatchmakingData
            {
                playerId = playerId,
                gameModeId = gameModeId,
                skillRating = skillRating,
                queueTime = DateTime.UtcNow,
                partyMembers = new List<ulong> { playerId }
            };

            playerQueue[playerId] = playerData;
            Debug.Log($"Player {playerId} joined matchmaking for {gameMode.modeName}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void LeaveMatchmakingServerRpc(ulong playerId, ServerRpcParams rpcParams = default)
        {
            playerQueue.Remove(playerId);
        }

        private void ProcessMatchmaking()
        {
            var groupedByMode = playerQueue.Values.GroupBy(p => p.gameModeId);

            foreach (var group in groupedByMode)
            {
                if (!gameModes.TryGetValue(group.Key, out var gameMode)) continue;

                var players = group.ToList();
                if (players.Count < gameMode.minPlayers) continue;

                if (gameMode.skillBased)
                {
                    CreateSkillBasedMatch(gameMode, players);
                }
                else
                {
                    CreateQuickMatch(gameMode, players);
                }
            }
        }

        private void CreateSkillBasedMatch(GameMode gameMode, List<PlayerMatchmakingData> players)
        {
            players = players.OrderBy(p => p.skillRating).ToList();

            for (int i = 0; i < players.Count; i++)
            {
                if (playerQueue.ContainsKey(players[i].playerId))
                {
                    var matchPlayers = new List<PlayerMatchmakingData> { players[i] };

                    for (int j = i + 1; j < players.Count && matchPlayers.Count < gameMode.maxPlayers; j++)
                    {
                        if (Math.Abs(players[i].skillRating - players[j].skillRating) <= maxSkillDifference)
                        {
                            matchPlayers.Add(players[j]);
                        }
                    }

                    if (matchPlayers.Count >= gameMode.minPlayers)
                    {
                        CreateLobby(gameMode, matchPlayers);
                        foreach (var player in matchPlayers)
                        {
                            playerQueue.Remove(player.playerId);
                        }
                    }
                }
            }
        }

        private void CreateQuickMatch(GameMode gameMode, List<PlayerMatchmakingData> players)
        {
            while (players.Count >= gameMode.minPlayers)
            {
                var matchPlayers = players.Take(Math.Min(gameMode.maxPlayers, players.Count)).ToList();
                CreateLobby(gameMode, matchPlayers);

                foreach (var player in matchPlayers)
                {
                    playerQueue.Remove(player.playerId);
                    players.Remove(player);
                }
            }
        }

        private void CreateLobby(GameMode gameMode, List<PlayerMatchmakingData> players)
        {
            var lobby = new MatchLobby
            {
                lobbyId = $"lobby_{Guid.NewGuid()}",
                gameMode = gameMode,
                players = players.Select(p => p.playerId).ToList(),
                creationTime = DateTime.UtcNow,
                status = LobbyStatus.Forming,
                maxPlayers = gameMode.maxPlayers,
                mapName = SelectRandomMap(gameMode)
            };

            activeLobbies[lobby.lobbyId] = lobby;
            OnLobbyCreated?.Invoke(lobby.lobbyId);

            foreach (var playerId in lobby.players)
            {
                OnPlayerJoinedLobby?.Invoke(playerId, lobby.lobbyId);
            }

            if (lobby.players.Count >= gameMode.minPlayers)
            {
                StartMatch(lobby);
            }
        }

        private string SelectRandomMap(GameMode gameMode)
        {
            var maps = new List<string> { "downtown", "suburbs", "industrial", "military_base", "forest" };
            return maps[UnityEngine.Random.Range(0, maps.Count)];
        }

        private void StartMatch(MatchLobby lobby)
        {
            lobby.status = LobbyStatus.InProgress;
            lobby.matchStartTime = DateTime.UtcNow;
            OnMatchStarted?.Invoke(lobby.lobbyId);
            Debug.Log($"Match started: {lobby.gameMode.modeName} with {lobby.players.Count} players");
        }

        [ServerRpc(RequireOwnership = false)]
        public void EndMatchServerRpc(string lobbyId, List<PlayerMatchResult> results, ServerRpcParams rpcParams = default)
        {
            if (!activeLobbies.TryGetValue(lobbyId, out var lobby)) return;

            lobby.status = LobbyStatus.Completed;
            lobby.matchEndTime = DateTime.UtcNow;
            lobby.results = results;

            foreach (var result in results)
            {
                UpdatePlayerSkillRating(result);
            }
        }

        private void UpdatePlayerSkillRating(PlayerMatchResult result)
        {
            int ratingChange = result.placement <= 3 ? 25 : -15;
            result.newSkillRating = result.oldSkillRating + ratingChange;
        }

        public MatchLobby GetLobby(string lobbyId) => activeLobbies.GetValueOrDefault(lobbyId);
        public List<MatchLobby> GetActiveLobbies() => activeLobbies.Values.Where(l => l.status != LobbyStatus.Completed).ToList();
    }

    [Serializable]
    public class MatchLobby
    {
        public string lobbyId;
        public GameMode gameMode;
        public List<ulong> players;
        public DateTime creationTime;
        public DateTime? matchStartTime;
        public DateTime? matchEndTime;
        public LobbyStatus status;
        public int maxPlayers;
        public string mapName;
        public List<PlayerMatchResult> results;
    }

    [Serializable]
    public class GameMode
    {
        public string modeId;
        public string modeName;
        public string description;
        public int minPlayers;
        public int maxPlayers;
        public GameModeType modeType;
        public float duration;
        public bool skillBased;
    }

    [Serializable]
    public class PlayerMatchmakingData
    {
        public ulong playerId;
        public string gameModeId;
        public int skillRating;
        public DateTime queueTime;
        public List<ulong> partyMembers;
    }

    [Serializable]
    public class PlayerMatchResult
    {
        public ulong playerId;
        public int placement;
        public int kills;
        public int deaths;
        public int oldSkillRating;
        public int newSkillRating;
    }

    public enum LobbyStatus { Forming, InProgress, Completed, Cancelled }
    public enum GameModeType { PvE, PvP, PvPvE }
}
