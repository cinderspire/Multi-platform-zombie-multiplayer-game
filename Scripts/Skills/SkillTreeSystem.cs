using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Skills
{
    public class SkillTreeSystem : NetworkBehaviour
    {
        public static SkillTreeSystem Instance { get; private set; }

        [SerializeField] private List<SkillNode> allSkills = new List<SkillNode>();

        private Dictionary<ulong, PlayerSkillTree> playerSkillTrees = new Dictionary<ulong, PlayerSkillTree>();

        public event Action<ulong, string> OnSkillUnlocked;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        [ServerRpc(RequireOwnership = false)]
        public void UnlockSkillServerRpc(ulong playerId, string skillId, ServerRpcParams rpcParams = default)
        {
            if (!playerSkillTrees.TryGetValue(playerId, out var tree))
            {
                tree = new PlayerSkillTree { playerId = playerId };
                playerSkillTrees[playerId] = tree;
            }

            var skill = allSkills.FirstOrDefault(s => s.skillId == skillId);
            if (skill == null || tree.unlockedSkills.Contains(skillId)) return;

            // Check prerequisites
            if (!ArePrerequisitesMet(tree, skill)) return;

            // Check skill points
            if (tree.availableSkillPoints < skill.cost) return;

            tree.unlockedSkills.Add(skillId);
            tree.availableSkillPoints -= skill.cost;

            OnSkillUnlocked?.Invoke(playerId, skillId);
            UnlockSkillClientRpc(playerId, skillId);

            ApplySkillEffects(playerId, skill);
        }

        private bool ArePrerequisitesMet(PlayerSkillTree tree, SkillNode skill)
        {
            return skill.prerequisites.All(prereq => tree.unlockedSkills.Contains(prereq));
        }

        private void ApplySkillEffects(ulong playerId, SkillNode skill)
        {
            // Apply skill bonuses to player
        }

        [ClientRpc]
        private void UnlockSkillClientRpc(ulong playerId, string skillId) { }

        public PlayerSkillTree GetPlayerSkillTree(ulong playerId) => playerSkillTrees.GetValueOrDefault(playerId);
    }

    [Serializable]
    public class PlayerSkillTree
    {
        public ulong playerId;
        public List<string> unlockedSkills = new List<string>();
        public int availableSkillPoints;
    }

    [Serializable]
    public class SkillNode
    {
        public string skillId;
        public string skillName;
        public string description;
        public SkillTreeBranch branch;
        public int tier;
        public int cost;
        public List<string> prerequisites = new List<string>();
        public SkillEffect effect;
    }

    [Serializable]
    public class SkillEffect
    {
        public SkillEffectType type;
        public float value;
    }

    public enum SkillTreeBranch { Combat, Survival, Support, Crafting, Movement }
    public enum SkillEffectType { DamageBonus, HealthBonus, SpeedBonus, XPBonus, ResourceBonus }
}
