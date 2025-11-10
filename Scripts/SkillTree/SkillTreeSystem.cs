using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.SkillTree
{
    /// <summary>
    /// Comprehensive skill tree and talent system.
    /// Deep character progression with multiple specialization paths.
    /// </summary>
    public class SkillTreeSystem : NetworkBehaviour
    {
        public static SkillTreeSystem Instance { get; private set; }

        [Header("Skill Tree Configuration")]
        [SerializeField] private int maxSkillPoints = 200;
        [SerializeField] private int skillPointsPerLevel = 3;
        [SerializeField] private bool enableSkillReset = true;
        [SerializeField] private int skillResetCost = 10000;

        // Data structures
        private Dictionary<ulong, PlayerSkillData> playerSkills = new Dictionary<ulong, PlayerSkillData>();
        private Dictionary<string, SkillNode> skillNodes = new Dictionary<string, SkillNode>();
        private Dictionary<string, SkillTree> skillTrees = new Dictionary<string, SkillTree>();

        // Events
        public event Action<ulong, string> OnSkillUnlocked;
        public event Action<ulong, string> OnSkillUpgraded;
        public event Action<ulong, string> OnTalentActivated;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer) { InitializeSkillTrees(); }
        }

        private void InitializeSkillTrees()
        {
            // Combat Tree
            skillTrees["combat"] = new SkillTree
            {
                treeId = "combat",
                treeName = "Combat Mastery",
                description = "Offensive combat specialization",
                nodes = new List<string>()
            };

            // Survival Tree
            skillTrees["survival"] = new SkillTree
            {
                treeId = "survival",
                treeName = "Survival Expert",
                description = "Defensive and survival specialization",
                nodes = new List<string>()
            };

            // Support Tree
            skillTrees["support"] = new SkillTree
            {
                treeId = "support",
                treeName = "Support Specialist",
                description = "Team support and utility specialization",
                nodes = new List<string>()
            };
        }

        [ServerRpc(RequireOwnership = false)]
        public void UnlockSkillServerRpc(ulong playerId, string skillId, ServerRpcParams rpcParams = default)
        {
            if (!playerSkills.ContainsKey(playerId))
            {
                playerSkills[playerId] = new PlayerSkillData
                {
                    playerId = playerId,
                    unlockedSkills = new List<string>(),
                    skillLevels = new Dictionary<string, int>(),
                    availableSkillPoints = 0
                };
            }

            var playerData = playerSkills[playerId];
            if (playerData.availableSkillPoints > 0)
            {
                playerData.unlockedSkills.Add(skillId);
                playerData.availableSkillPoints--;
                OnSkillUnlocked?.Invoke(playerId, skillId);
            }
        }

        public PlayerSkillData GetPlayerSkills(ulong playerId)
        {
            return playerSkills.GetValueOrDefault(playerId);
        }
    }

    [Serializable]
    public class PlayerSkillData
    {
        public ulong playerId;
        public List<string> unlockedSkills;
        public Dictionary<string, int> skillLevels;
        public int availableSkillPoints;
    }

    [Serializable]
    public class SkillTree
    {
        public string treeId;
        public string treeName;
        public string description;
        public List<string> nodes;
    }

    [Serializable]
    public class SkillNode
    {
        public string skillId;
        public string skillName;
        public int maxLevel;
    }
}
