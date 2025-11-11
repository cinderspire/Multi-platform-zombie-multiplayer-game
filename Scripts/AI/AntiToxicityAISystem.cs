using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

namespace ZombieGame
{
    public class AntiToxicityAISystem : NetworkBehaviour
    {
        public static AntiToxicityAISystem Instance { get; private set; }
        
        private Dictionary<ulong, ToxicityScore> playerScores = new Dictionary<ulong, ToxicityScore>();
        private List<string> toxicKeywords = new List<string> { "hate", "offensive", "slur" }; // Simplified
        
        [System.Serializable]
        public class ToxicityScore
        {
            public float score = 0f; // 0-100, higher = more toxic
            public int warningsIssued = 0;
            public bool isMuted = false;
            public List<string> flaggedMessages = new List<string>();
        }
        
        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }
        
        public float AnalyzeMessage(string message)
        {
            float toxicity = 0f;
            
            string lowerMessage = message.ToLower();
            foreach (var keyword in toxicKeywords)
            {
                if (lowerMessage.Contains(keyword))
                {
                    toxicity += 25f;
                }
            }
            
            return Mathf.Clamp(toxicity, 0f, 100f);
        }
        
        [ServerRpc(RequireOwnership = false)]
        public void ReportMessageServerRpc(ulong playerId, string message, ServerRpcParams rpcParams = default)
        {
            if (!playerScores.ContainsKey(playerId))
            {
                playerScores[playerId] = new ToxicityScore();
            }
            
            float toxicity = AnalyzeMessage(message);
            var score = playerScores[playerId];
            
            if (toxicity > 50f)
            {
                score.score += toxicity;
                score.flaggedMessages.Add(message);
                
                if (score.score > 200f)
                {
                    score.isMuted = true;
                    score.warningsIssued++;
                    Debug.Log($"[AntiToxicity] Player {playerId} muted (toxicity: {score.score})");
                }
            }
        }
        
        public ToxicityScore GetPlayerScore(ulong playerId)
        {
            return playerScores.ContainsKey(playerId) ? playerScores[playerId] : null;
        }
    }
}
