using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

namespace ZombieGame
{
    public class PlayerRetentionAISystem : NetworkBehaviour
    {
        public static PlayerRetentionAISystem Instance { get; private set; }
        
        [SerializeField] private float checkInterval = 300f; // 5 minutes
        
        private Dictionary<ulong, PlayerEngagementData> playerData = new Dictionary<ulong, PlayerEngagementData>();
        
        [System.Serializable]
        public class PlayerEngagementData
        {
            public float sessionDuration = 0f;
            public int matchesPlayed = 0;
            public float lastActivityTime = 0f;
            public float churnRisk = 0f; // 0-1, higher = more likely to leave
            public List<string> suggestedActivities = new List<string>();
        }
        
        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }
        
        private void Update()
        {
            if (!IsServer) return;
            UpdatePlayerEngagement();
        }
        
        private void UpdatePlayerEngagement()
        {
            foreach (var kvp in playerData)
            {
                var data = kvp.Value;
                data.sessionDuration += Time.deltaTime;
                
                // Calculate churn risk
                float timeSinceActivity = Time.time - data.lastActivityTime;
                data.churnRisk = Mathf.Clamp01(timeSinceActivity / 600f); // 10 min threshold
                
                // Generate suggestions if at risk
                if (data.churnRisk > 0.7f)
                {
                    GenerateSuggestions(kvp.Key, data);
                }
            }
        }
        
        private void GenerateSuggestions(ulong playerId, PlayerEngagementData data)
        {
            data.suggestedActivities.Clear();
            data.suggestedActivities.Add("Try a new game mode");
            data.suggestedActivities.Add("Complete daily challenges");
            data.suggestedActivities.Add("Join a clan");
        }
        
        public void TrackActivity(ulong playerId, string activity)
        {
            if (!playerData.ContainsKey(playerId))
            {
                playerData[playerId] = new PlayerEngagementData();
            }
            playerData[playerId].lastActivityTime = Time.time;
        }
        
        public PlayerEngagementData GetPlayerData(ulong playerId)
        {
            return playerData.ContainsKey(playerId) ? playerData[playerId] : null;
        }
    }
}
