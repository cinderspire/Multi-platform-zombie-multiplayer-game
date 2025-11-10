using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Network
{
    public class TeamSystem : NetworkBehaviour
    {
        public static TeamSystem Instance { get; private set; }

        [SerializeField] private bool friendlyFireEnabled = false;
        [SerializeField] private int maxTeamSize = 4;

        private Dictionary<int, Team> teams = new Dictionary<int, Team>();
        private Dictionary<ulong, int> playerTeams = new Dictionary<ulong, int>();

        public event Action<ulong, int> OnPlayerJoinedTeam;
        public event Action<ulong, int> OnPlayerLeftTeam;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        [ServerRpc(RequireOwnership = false)]
        public void AssignPlayerToTeamServerRpc(ulong playerId, int teamId, ServerRpcParams rpcParams = default)
        {
            if (!teams.ContainsKey(teamId))
            {
                teams[teamId] = new Team { teamId = teamId, teamName = $"Team {teamId}" };
            }

            if (teams[teamId].memberIds.Count >= maxTeamSize) return;

            if (playerTeams.ContainsKey(playerId))
            {
                RemovePlayerFromTeam(playerId);
            }

            teams[teamId].memberIds.Add(playerId);
            playerTeams[playerId] = teamId;
            OnPlayerJoinedTeam?.Invoke(playerId, teamId);
        }

        private void RemovePlayerFromTeam(ulong playerId)
        {
            if (!playerTeams.TryGetValue(playerId, out int teamId)) return;

            if (teams.TryGetValue(teamId, out var team))
            {
                team.memberIds.Remove(playerId);
                OnPlayerLeftTeam?.Invoke(playerId, teamId);
            }

            playerTeams.Remove(playerId);
        }

        public bool ArePlayersOnSameTeam(ulong player1, ulong player2)
        {
            if (!playerTeams.TryGetValue(player1, out int team1)) return false;
            if (!playerTeams.TryGetValue(player2, out int team2)) return false;
            return team1 == team2;
        }

        public bool CanDamage(ulong attacker, ulong target)
        {
            if (attacker == target) return false;
            if (!friendlyFireEnabled && ArePlayersOnSameTeam(attacker, target)) return false;
            return true;
        }

        public int GetPlayerTeam(ulong playerId) => playerTeams.GetValueOrDefault(playerId, -1);
    }

    [Serializable]
    public class Team
    {
        public int teamId;
        public string teamName;
        public List<ulong> memberIds = new List<ulong>();
    }
}
