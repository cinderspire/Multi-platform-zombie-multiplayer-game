using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.AI
{
    public class BossZombieSystem : NetworkBehaviour
    {
        public static BossZombieSystem Instance { get; private set; }

        [SerializeField] private List<BossData> bossTypes = new List<BossData>();
        [SerializeField] private float bossSpawnInterval = 300f;

        private Dictionary<string, BossInstance> activeBosses = new Dictionary<string, BossInstance>();

        public event Action<string, BossData> OnBossSpawned;
        public event Action<string> OnBossDefeated;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        [ServerRpc(RequireOwnership = false)]
        public void SpawnBossServerRpc(string bossTypeId, Vector3 position, ServerRpcParams rpcParams = default)
        {
            var bossData = bossTypes.Find(b => b.bossId == bossTypeId);
            if (bossData == null) return;

            string instanceId = Guid.NewGuid().ToString();
            BossInstance instance = new BossInstance
            {
                instanceId = instanceId,
                bossData = bossData,
                spawnTime = Time.time,
                currentPhase = 1
            };

            activeBosses[instanceId] = instance;
            OnBossSpawned?.Invoke(instanceId, bossData);
            SpawnBossClientRpc(instanceId, bossTypeId, position);
        }

        [ServerRpc(RequireOwnership = false)]
        public void BossDefeatedServerRpc(string instanceId, ServerRpcParams rpcParams = default)
        {
            if (!activeBosses.ContainsKey(instanceId)) return;

            var boss = activeBosses[instanceId];
            DropBossLoot(boss);

            activeBosses.Remove(instanceId);
            OnBossDefeated?.Invoke(instanceId);
            BossDefeatedClientRpc(instanceId);
        }

        private void DropBossLoot(BossInstance boss)
        {
            // Drop special boss loot
        }

        [ClientRpc]
        private void SpawnBossClientRpc(string instanceId, string bossTypeId, Vector3 position) { }

        [ClientRpc]
        private void BossDefeatedClientRpc(string instanceId) { }
    }

    [Serializable]
    public class BossData
    {
        public string bossId;
        public string bossName;
        public float health;
        public float damage;
        public int phases;
        public List<BossAbility> abilities = new List<BossAbility>();
    }

    [Serializable]
    public class BossAbility
    {
        public string abilityId;
        public string abilityName;
        public float cooldown;
        public AbilityType type;
    }

    [Serializable]
    public class BossInstance
    {
        public string instanceId;
        public BossData bossData;
        public float spawnTime;
        public int currentPhase;
    }

    public enum AbilityType { AOEAttack, Summon, Buff, Heal, Rage }
}
