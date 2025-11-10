using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Arena
{
    public class ArenaSystem : NetworkBehaviour
    {
        public static ArenaSystem Instance { get; private set; }

        private Dictionary<string, ArenaMode> arenaModes = new Dictionary<string, ArenaMode>();
        private Dictionary<ulong, PlayerArenaData> playerArenaData = new Dictionary<ulong, PlayerArenaData>();
        private Dictionary<string, ArenaMatch> activeMatches = new Dictionary<string, ArenaMatch>();

        public event Action<ulong, int> OnRankChanged;
        public event Action<string> OnMatchStarted;
        public event Action<string, ulong> OnMatchEnded;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer) InitializeArenaModes();
        }

        private void InitializeArenaModes()
        {
            arenaModes["deathmatch"] = new ArenaMode { modeId = "deathmatch", modeName = "Deathmatch", description = "Free-for-all combat", playerCount = 8, duration = 600, scoreToWin = 30, ranked = true };
            arenaModes["team_deathmatch"] = new ArenaMode { modeId = "team_deathmatch", modeName = "Team Deathmatch", description = "4v4 team combat", playerCount = 8, duration = 600, scoreToWin = 50, ranked = true, teamBased = true };
            arenaModes["elimination"] = new ArenaMode { modeId = "elimination", modeName = "Elimination", description = "Last player standing", playerCount = 16, duration = 900, ranked = true };
            arenaModes["king_of_hill"] = new ArenaMode { modeId = "king_of_hill", modeName = "King of the Hill", description = "Control the zone", playerCount = 12, duration = 600, scoreToWin = 200, ranked = true };
            arenaModes["capture_flag"] = new ArenaMode { modeId = "capture_flag", modeName = "Capture the Flag", description = "Capture enemy flags", playerCount = 10, duration = 720, scoreToWin = 5, ranked = true, teamBased = true };

            Debug.Log($"Initialized {arenaModes.Count} arena modes");
        }

        [ServerRpc(RequireOwnership = false)]
        public void InitializePlayerArenaDataServerRpc(ulong playerId, ServerRpcParams rpcParams = default)
        {
            if (playerArenaData.ContainsKey(playerId)) return;

            playerArenaData[playerId] = new PlayerArenaData
            {
                playerId = playerId,
                rank = 1000,
                wins = 0,
                losses = 0,
                kills = 0,
                deaths = 0,
                currentSeason = 1,
                seasonRank = RankTier.Bronze,
                seasonRewards = new List<string>()
            };
        }

        [ServerRpc(RequireOwnership = false)]
        public void QueueForMatchServerRpc(ulong playerId, string modeId, ServerRpcParams rpcParams = default)
        {
            // Matchmaking logic would go here
            Debug.Log($"Player {playerId} queued for {modeId}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void RecordMatchResultServerRpc(ulong playerId, bool won, int kills, int deaths, ServerRpcParams rpcParams = default)
        {
            if (!playerArenaData.TryGetValue(playerId, out var data)) return;

            if (won) data.wins++; else data.losses++;
            data.kills += kills;
            data.deaths += deaths;

            // ELO calculation
            int rankChange = won ? 25 : -20;
            data.rank = Mathf.Clamp(data.rank + rankChange, 0, 3000);

            // Update season rank
            data.seasonRank = GetRankTier(data.rank);

            OnRankChanged?.Invoke(playerId, data.rank);
            Debug.Log($"Player {playerId} new rank: {data.rank} ({data.seasonRank})");
        }

        private RankTier GetRankTier(int rank)
        {
            if (rank >= 2500) return RankTier.Grandmaster;
            if (rank >= 2000) return RankTier.Master;
            if (rank >= 1700) return RankTier.Diamond;
            if (rank >= 1400) return RankTier.Platinum;
            if (rank >= 1100) return RankTier.Gold;
            if (rank >= 800) return RankTier.Silver;
            return RankTier.Bronze;
        }

        public PlayerArenaData GetPlayerArenaData(ulong playerId) => playerArenaData.GetValueOrDefault(playerId);
        public ArenaMode GetArenaMode(string modeId) => arenaModes.GetValueOrDefault(modeId);
    }

    [Serializable]
    public class ArenaMode
    {
        public string modeId;
        public string modeName;
        public string description;
        public int playerCount;
        public int duration;
        public int scoreToWin;
        public bool ranked;
        public bool teamBased;
    }

    [Serializable]
    public class PlayerArenaData
    {
        public ulong playerId;
        public int rank;
        public int wins;
        public int losses;
        public int kills;
        public int deaths;
        public int currentSeason;
        public RankTier seasonRank;
        public List<string> seasonRewards;
    }

    [Serializable]
    public class ArenaMatch
    {
        public string matchId;
        public string modeId;
        public List<ulong> players;
        public DateTime startTime;
        public int duration;
    }

    public enum RankTier { Bronze, Silver, Gold, Platinum, Diamond, Master, Grandmaster }
}
