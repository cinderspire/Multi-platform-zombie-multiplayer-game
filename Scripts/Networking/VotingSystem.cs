using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

namespace DeadFrontier.Networking
{
    /// <summary>
    /// Manages map and game mode voting in multiplayer lobbies
    /// Server-authoritative voting with client UI integration
    /// </summary>
    public class VotingSystem : NetworkBehaviour
    {
        [Header("Voting Settings")]
        [SerializeField] private float votingDuration = 15f;
        [SerializeField] private int maxMapOptions = 3;
        [SerializeField] private bool allowRandomOption = true;

        [Header("Maps")]
        [SerializeField] private List<MapData> availableMaps = new List<MapData>();

        [Header("Game Modes")]
        [SerializeField] private List<GameModeData> availableGameModes = new List<GameModeData>();

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        // Voting state
        private NetworkVariable<bool> isVotingActive = new NetworkVariable<bool>(false);
        private NetworkVariable<float> votingTimeRemaining = new NetworkVariable<float>(0f);

        private List<string> mapOptions = new List<string>();
        private List<string> gameModeOptions = new List<string>();

        private Dictionary<ulong, string> mapVotes = new Dictionary<ulong, string>();
        private Dictionary<ulong, string> gameModeVotes = new Dictionary<ulong, string>();

        // Results
        private string selectedMap = "";
        private string selectedGameMode = "";

        // Events
        public event System.Action OnVotingStarted;
        public event System.Action<string, string> OnVotingEnded; // map, gameMode
        public event System.Action<float> OnVotingTimeUpdated;

        private void Update()
        {
            if (!IsServer) return;

            if (isVotingActive.Value)
            {
                votingTimeRemaining.Value -= Time.deltaTime;

                if (votingTimeRemaining.Value <= 0f)
                {
                    EndVoting();
                }
            }
        }

        #region Server - Voting Control

        /// <summary>
        /// Starts the voting process (Server only)
        /// </summary>
        public void StartVoting()
        {
            if (!IsServer) return;

            if (isVotingActive.Value)
            {
                if (showDebugLogs)
                    Debug.LogWarning("[VotingSystem] Voting already active");
                return;
            }

            // Select random map options
            SelectMapOptions();
            SelectGameModeOptions();

            // Reset votes
            mapVotes.Clear();
            gameModeVotes.Clear();

            // Set voting active
            isVotingActive.Value = true;
            votingTimeRemaining.Value = votingDuration;

            if (showDebugLogs)
                Debug.Log($"[VotingSystem] Voting started. Maps: {string.Join(", ", mapOptions)}, Modes: {string.Join(", ", gameModeOptions)}");

            // Notify clients
            NotifyVotingStartedClientRpc(mapOptions.ToArray(), gameModeOptions.ToArray());

            OnVotingStarted?.Invoke();
        }

        private void SelectMapOptions()
        {
            mapOptions.Clear();

            if (availableMaps.Count <= maxMapOptions)
            {
                // Add all maps
                foreach (var map in availableMaps)
                {
                    mapOptions.Add(map.mapId);
                }
            }
            else
            {
                // Select random maps
                var shuffled = availableMaps.OrderBy(x => Random.value).ToList();
                for (int i = 0; i < maxMapOptions; i++)
                {
                    mapOptions.Add(shuffled[i].mapId);
                }
            }

            if (allowRandomOption)
            {
                mapOptions.Add("random");
            }
        }

        private void SelectGameModeOptions()
        {
            gameModeOptions.Clear();

            foreach (var mode in availableGameModes)
            {
                gameModeOptions.Add(mode.modeId);
            }
        }

        private void EndVoting()
        {
            if (!IsServer) return;

            isVotingActive.Value = false;

            // Count votes and determine winners
            selectedMap = CountVotes(mapVotes, mapOptions);
            selectedGameMode = CountVotes(gameModeVotes, gameModeOptions);

            // Handle random selection
            if (selectedMap == "random")
            {
                selectedMap = mapOptions[Random.Range(0, mapOptions.Count - 1)]; // -1 to exclude "random"
            }

            if (showDebugLogs)
                Debug.Log($"[VotingSystem] Voting ended. Selected Map: {selectedMap}, Mode: {selectedGameMode}");

            // Notify clients
            NotifyVotingEndedClientRpc(selectedMap, selectedGameMode);

            OnVotingEnded?.Invoke(selectedMap, selectedGameMode);

            // Track analytics
            Core.Analytics.AnalyticsManager.Instance?.TrackEvent("voting_completed", new Dictionary<string, object>
            {
                { "selected_map", selectedMap },
                { "selected_mode", selectedGameMode },
                { "total_votes", mapVotes.Count }
            });
        }

        private string CountVotes(Dictionary<ulong, string> votes, List<string> options)
        {
            if (votes.Count == 0)
            {
                // No votes - random selection
                return options[Random.Range(0, options.Count)];
            }

            // Count votes for each option
            Dictionary<string, int> voteCounts = new Dictionary<string, int>();
            foreach (var option in options)
            {
                voteCounts[option] = 0;
            }

            foreach (var vote in votes.Values)
            {
                if (voteCounts.ContainsKey(vote))
                {
                    voteCounts[vote]++;
                }
            }

            // Find option with most votes
            string winner = voteCounts.OrderByDescending(kvp => kvp.Value).First().Key;

            // Handle ties - random between tied options
            int maxVotes = voteCounts[winner];
            var tiedOptions = voteCounts.Where(kvp => kvp.Value == maxVotes).Select(kvp => kvp.Key).ToList();

            if (tiedOptions.Count > 1)
            {
                winner = tiedOptions[Random.Range(0, tiedOptions.Count)];
            }

            return winner;
        }

        #endregion

        #region Client - Voting

        /// <summary>
        /// Submits a vote for a map (Client to Server)
        /// </summary>
        public void VoteForMap(string mapId)
        {
            if (!isVotingActive.Value)
            {
                if (showDebugLogs)
                    Debug.LogWarning("[VotingSystem] Voting not active");
                return;
            }

            if (!mapOptions.Contains(mapId))
            {
                if (showDebugLogs)
                    Debug.LogWarning($"[VotingSystem] Invalid map option: {mapId}");
                return;
            }

            SubmitMapVoteServerRpc(mapId);

            if (showDebugLogs)
                Debug.Log($"[VotingSystem] Voted for map: {mapId}");
        }

        /// <summary>
        /// Submits a vote for a game mode (Client to Server)
        /// </summary>
        public void VoteForGameMode(string modeId)
        {
            if (!isVotingActive.Value)
            {
                if (showDebugLogs)
                    Debug.LogWarning("[VotingSystem] Voting not active");
                return;
            }

            if (!gameModeOptions.Contains(modeId))
            {
                if (showDebugLogs)
                    Debug.LogWarning($"[VotingSystem] Invalid game mode option: {modeId}");
                return;
            }

            SubmitGameModeVoteServerRpc(modeId);

            if (showDebugLogs)
                Debug.Log($"[VotingSystem] Voted for game mode: {modeId}");
        }

        #endregion

        #region Network RPCs

        [ServerRpc(RequireOwnership = false)]
        private void SubmitMapVoteServerRpc(string mapId, ServerRpcParams serverRpcParams = default)
        {
            ulong clientId = serverRpcParams.Receive.SenderClientId;
            mapVotes[clientId] = mapId;

            if (showDebugLogs)
                Debug.Log($"[VotingSystem] Client {clientId} voted for map: {mapId}");

            // Broadcast vote update
            BroadcastVoteUpdateClientRpc(GetMapVoteCounts());
        }

        [ServerRpc(RequireOwnership = false)]
        private void SubmitGameModeVoteServerRpc(string modeId, ServerRpcParams serverRpcParams = default)
        {
            ulong clientId = serverRpcParams.Receive.SenderClientId;
            gameModeVotes[clientId] = modeId;

            if (showDebugLogs)
                Debug.Log($"[VotingSystem] Client {clientId} voted for mode: {modeId}");
        }

        [ClientRpc]
        private void NotifyVotingStartedClientRpc(string[] maps, string[] modes)
        {
            mapOptions = new List<string>(maps);
            gameModeOptions = new List<string>(modes);

            OnVotingStarted?.Invoke();

            if (showDebugLogs)
                Debug.Log("[VotingSystem] Voting started (Client)");
        }

        [ClientRpc]
        private void NotifyVotingEndedClientRpc(string map, string mode)
        {
            selectedMap = map;
            selectedGameMode = mode;

            OnVotingEnded?.Invoke(map, mode);

            if (showDebugLogs)
                Debug.Log($"[VotingSystem] Voting ended (Client). Map: {map}, Mode: {mode}");
        }

        [ClientRpc]
        private void BroadcastVoteUpdateClientRpc(int[] voteCounts)
        {
            // Update UI with current vote counts
            OnVotingTimeUpdated?.Invoke(votingTimeRemaining.Value);
        }

        #endregion

        #region Helpers

        private int[] GetMapVoteCounts()
        {
            int[] counts = new int[mapOptions.Count];

            for (int i = 0; i < mapOptions.Count; i++)
            {
                counts[i] = mapVotes.Values.Count(v => v == mapOptions[i]);
            }

            return counts;
        }

        private int[] GetGameModeVoteCounts()
        {
            int[] counts = new int[gameModeOptions.Count];

            for (int i = 0; i < gameModeOptions.Count; i++)
            {
                counts[i] = gameModeVotes.Values.Count(v => v == gameModeOptions[i]);
            }

            return counts;
        }

        /// <summary>
        /// Gets map data by ID
        /// </summary>
        public MapData GetMapData(string mapId)
        {
            return availableMaps.FirstOrDefault(m => m.mapId == mapId);
        }

        /// <summary>
        /// Gets game mode data by ID
        /// </summary>
        public GameModeData GetGameModeData(string modeId)
        {
            return availableGameModes.FirstOrDefault(m => m.modeId == modeId);
        }

        #endregion

        #region Properties

        public bool IsVotingActive => isVotingActive.Value;
        public float VotingTimeRemaining => votingTimeRemaining.Value;
        public List<string> MapOptions => new List<string>(mapOptions);
        public List<string> GameModeOptions => new List<string>(gameModeOptions);
        public string SelectedMap => selectedMap;
        public string SelectedGameMode => selectedGameMode;

        #endregion
    }

    #region Data Structures

    [System.Serializable]
    public class MapData
    {
        public string mapId;
        public string mapName;
        public string description;
        public Sprite previewImage;
        public string sceneName;
        public int minPlayers = 1;
        public int maxPlayers = 16;
        public MapSize size;
        public List<string> supportedGameModes;
    }

    [System.Serializable]
    public class GameModeData
    {
        public string modeId;
        public string modeName;
        public string description;
        public Sprite icon;
        public int requiredPlayers = 1;
        public float matchDuration = 900f; // 15 minutes
        public bool isRanked;
    }

    public enum MapSize
    {
        Small,
        Medium,
        Large,
        ExtraLarge
    }

    #endregion
}
