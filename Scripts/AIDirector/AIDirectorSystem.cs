using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.AIDirector
{
    public class AIDirectorSystem : NetworkBehaviour
    {
        public static AIDirectorSystem Instance { get; private set; }

        [Header("AI Director Configuration")]
        [SerializeField] private float difficultyUpdateInterval = 30f;
        [SerializeField] private float spawnCheckInterval = 5f;
        [SerializeField] private int maxZombiesPerPlayer = 10;
        [SerializeField] private int maxTotalZombies = 100;

        private Dictionary<ulong, PlayerDifficultyProfile> playerProfiles = new Dictionary<ulong, PlayerDifficultyProfile>();
        private Dictionary<string, ZombieSpawn> activeZombies = new Dictionary<string, ZombieSpawn>();
        private Dictionary<string, ZombieType> zombieTypes = new Dictionary<string, ZombieType>();
        private float globalDifficulty = 1f;

        public event Action<float> OnDifficultyChanged;
        public event Action<string, Vector3> OnZombieSpawned;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer)
            {
                InitializeZombieTypes();
                InvokeRepeating(nameof(UpdateDifficulty), 0f, difficultyUpdateInterval);
                InvokeRepeating(nameof(UpdateSpawns), 0f, spawnCheckInterval);
            }
        }

        private void InitializeZombieTypes()
        {
            zombieTypes["walker"] = new ZombieType
            {
                typeId = "walker",
                typeName = "Walker",
                tier = 1,
                health = 100f,
                damage = 10f,
                speed = 2f,
                spawnWeight = 50,
                threatLevel = 1
            };

            zombieTypes["runner"] = new ZombieType
            {
                typeId = "runner",
                typeName = "Runner",
                tier = 2,
                health = 80f,
                damage = 15f,
                speed = 6f,
                spawnWeight = 30,
                threatLevel = 2
            };

            zombieTypes["tank"] = new ZombieType
            {
                typeId = "tank",
                typeName = "Tank",
                tier = 3,
                health = 500f,
                damage = 25f,
                speed = 1.5f,
                spawnWeight = 10,
                threatLevel = 5
            };

            zombieTypes["screamer"] = new ZombieType
            {
                typeId = "screamer",
                typeName = "Screamer",
                tier = 2,
                health = 60f,
                damage = 5f,
                speed = 3f,
                spawnWeight = 15,
                threatLevel = 3,
                specialAbility = "attract_horde"
            };

            zombieTypes["explosive"] = new ZombieType
            {
                typeId = "explosive",
                typeName = "Explosive Zombie",
                tier = 3,
                health = 120f,
                damage = 100f,
                speed = 3f,
                spawnWeight = 5,
                threatLevel = 4,
                specialAbility = "explode_on_death"
            };
        }

        private void UpdateDifficulty()
        {
            float avgPlayerPerformance = 0f;
            int playerCount = 0;

            foreach (var profile in playerProfiles.Values)
            {
                float performance = CalculatePlayerPerformance(profile);
                avgPlayerPerformance += performance;
                playerCount++;
            }

            if (playerCount > 0)
            {
                avgPlayerPerformance /= playerCount;
                globalDifficulty = Mathf.Clamp(avgPlayerPerformance, 0.5f, 3f);
                OnDifficultyChanged?.Invoke(globalDifficulty);
            }
        }

        private float CalculatePlayerPerformance(PlayerDifficultyProfile profile)
        {
            float killDeathRatio = profile.deaths > 0 ? (float)profile.kills / profile.deaths : profile.kills;
            float survivalFactor = Mathf.Clamp01(profile.survivalTime / 3600f);
            float damageFactor = Mathf.Clamp01(profile.damageDealt / 10000f);

            return (killDeathRatio * 0.4f + survivalFactor * 0.3f + damageFactor * 0.3f);
        }

        private void UpdateSpawns()
        {
            if (activeZombies.Count >= maxTotalZombies) return;

            foreach (var playerId in playerProfiles.Keys)
            {
                int playerZombieCount = activeZombies.Values.Count(z => z.targetPlayerId == playerId);
                if (playerZombieCount < maxZombiesPerPlayer)
                {
                    SpawnZombieNearPlayer(playerId);
                }
            }
        }

        private void SpawnZombieNearPlayer(ulong playerId)
        {
            var zombieType = SelectZombieType();
            Vector3 spawnPos = GetSpawnPositionNearPlayer(playerId);

            var zombie = new ZombieSpawn
            {
                zombieId = $"zombie_{Guid.NewGuid()}",
                zombieType = zombieType,
                targetPlayerId = playerId,
                spawnPosition = spawnPos,
                currentHealth = zombieType.health * globalDifficulty,
                spawnTime = DateTime.UtcNow
            };

            activeZombies[zombie.zombieId] = zombie;
            OnZombieSpawned?.Invoke(zombie.zombieId, spawnPos);
        }

        private ZombieType SelectZombieType()
        {
            int totalWeight = zombieTypes.Values.Sum(z => z.spawnWeight);
            int random = UnityEngine.Random.Range(0, totalWeight);
            int current = 0;

            foreach (var type in zombieTypes.Values)
            {
                current += type.spawnWeight;
                if (random < current) return type;
            }

            return zombieTypes["walker"];
        }

        private Vector3 GetSpawnPositionNearPlayer(ulong playerId)
        {
            float angle = UnityEngine.Random.Range(0f, 360f);
            float distance = UnityEngine.Random.Range(30f, 50f);
            return new Vector3(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance);
        }

        [ServerRpc(RequireOwnership = false)]
        public void UpdatePlayerStatsServerRpc(ulong playerId, int kills, int deaths, float survivalTime, float damageDealt, ServerRpcParams rpcParams = default)
        {
            if (!playerProfiles.ContainsKey(playerId))
            {
                playerProfiles[playerId] = new PlayerDifficultyProfile
                {
                    playerId = playerId,
                    kills = 0,
                    deaths = 0,
                    survivalTime = 0f,
                    damageDealt = 0f
                };
            }

            var profile = playerProfiles[playerId];
            profile.kills = kills;
            profile.deaths = deaths;
            profile.survivalTime = survivalTime;
            profile.damageDealt = damageDealt;
        }

        [ServerRpc(RequireOwnership = false)]
        public void RemoveZombieServerRpc(string zombieId, ServerRpcParams rpcParams = default)
        {
            activeZombies.Remove(zombieId);
        }

        public float GetCurrentDifficulty() => globalDifficulty;
    }

    [Serializable]
    public class PlayerDifficultyProfile
    {
        public ulong playerId;
        public int kills;
        public int deaths;
        public float survivalTime;
        public float damageDealt;
    }

    [Serializable]
    public class ZombieSpawn
    {
        public string zombieId;
        public ZombieType zombieType;
        public ulong targetPlayerId;
        public Vector3 spawnPosition;
        public float currentHealth;
        public DateTime spawnTime;
    }

    [Serializable]
    public class ZombieType
    {
        public string typeId;
        public string typeName;
        public int tier;
        public float health;
        public float damage;
        public float speed;
        public int spawnWeight;
        public int threatLevel;
        public string specialAbility;
    }
}
