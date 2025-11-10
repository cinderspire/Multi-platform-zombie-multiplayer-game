using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Progression
{
    /// <summary>
    /// Comprehensive progression system handling player leveling, ranks, unlocks, and multiple progression tracks.
    /// Includes XP calculation, level rewards, prestige system, and seasonal progression.
    /// </summary>
    public class ProgressionSystem : NetworkBehaviour
    {
        public static ProgressionSystem Instance { get; private set; }

        [Header("Progression Configuration")]
        [SerializeField] private int maxLevel = 100;
        [SerializeField] private int maxPrestigeLevel = 10;
        [SerializeField] private float baseXpRequirement = 1000f;
        [SerializeField] private float xpScalingFactor = 1.15f;
        [SerializeField] private bool enableSeasonalProgression = true;

        // Player progression data
        private Dictionary<ulong, PlayerProgression> playerProgressions = new Dictionary<ulong, PlayerProgression>();
        
        // Level and rank definitions
        private Dictionary<int, LevelDefinition> levelDefinitions = new Dictionary<int, LevelDefinition>();
        private Dictionary<string, RankDefinition> rankDefinitions = new Dictionary<string, RankDefinition>();
        private Dictionary<ProgressionTrack, TrackDefinition> progressionTracks = new Dictionary<ProgressionTrack, TrackDefinition>();
        private Dictionary<int, SeasonDefinition> seasonDefinitions = new Dictionary<int, SeasonDefinition>();

        // Events
        public event Action<ulong, int> OnPlayerLevelUp;
        public event Action<ulong, int> OnPlayerPrestige;
        public event Action<ulong, string> OnUnlockAchieved;
        public event Action<ulong, ProgressionTrack, int> OnTrackLevelUp;
        public event Action<ulong, string> OnRankPromoted;

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
                InitializeLevelDefinitions();
                InitializeRankDefinitions();
                InitializeProgressionTracks();
                InitializeSeasonDefinitions();
            }
        }

        private void InitializeLevelDefinitions()
        {
            // Generate level curve for levels 1-100
            for (int level = 1; level <= maxLevel; level++)
            {
                float xpRequired = CalculateXpForLevel(level);
                
                var levelDef = new LevelDefinition
                {
                    level = level,
                    xpRequired = Mathf.RoundToInt(xpRequired),
                    rewards = GenerateLevelRewards(level),
                    unlocks = GenerateLevelUnlocks(level),
                    title = GetLevelTitle(level)
                };

                levelDefinitions[level] = levelDef;
            }

            Debug.Log($"Initialized {levelDefinitions.Count} level definitions");
        }

        private float CalculateXpForLevel(int level)
        {
            // Exponential curve: BaseXP * (ScalingFactor ^ (Level - 1))
            return baseXpRequirement * Mathf.Pow(xpScalingFactor, level - 1);
        }

        private LevelRewards GenerateLevelRewards(int level)
        {
            var rewards = new LevelRewards
            {
                softCurrency = level * 500,
                hardCurrency = 0,
                skillPoints = 1,
                items = new List<string>()
            };

            // Milestone rewards
            if (level % 5 == 0) rewards.softCurrency *= 2;
            if (level % 10 == 0)
            {
                rewards.hardCurrency = 50;
                rewards.skillPoints = 5;
                rewards.items.Add($"supply_crate_level_{level}");
            }
            if (level % 25 == 0)
            {
                rewards.hardCurrency = 200;
                rewards.items.Add($"legendary_weapon_token");
            }

            return rewards;
        }

        private List<string> GenerateLevelUnlocks(int level)
        {
            var unlocks = new List<string>();

            // Weapon unlocks
            if (level == 5) unlocks.Add("weapon_smg_basic");
            if (level == 10) unlocks.Add("weapon_shotgun_pump");
            if (level == 15) unlocks.Add("weapon_ar_m4");
            if (level == 20) unlocks.Add("weapon_sniper_bolt");
            if (level == 25) unlocks.Add("weapon_lmg_pkm");
            if (level == 30) unlocks.Add("weapon_explosive_rpg");
            if (level == 35) unlocks.Add("weapon_special_crossbow");
            if (level == 40) unlocks.Add("weapon_energy_plasma");

            // Feature unlocks
            if (level == 8) unlocks.Add("feature_trading");
            if (level == 12) unlocks.Add("feature_base_building");
            if (level == 18) unlocks.Add("feature_auction_house");
            if (level == 22) unlocks.Add("feature_clan_system");
            if (level == 28) unlocks.Add("feature_vehicle_customization");
            if (level == 35) unlocks.Add("feature_prestige");
            if (level == 50) unlocks.Add("feature_legendary_quests");

            // Game mode unlocks
            if (level == 10) unlocks.Add("gamemode_pvp_deathmatch");
            if (level == 20) unlocks.Add("gamemode_battle_royale");
            if (level == 30) unlocks.Add("gamemode_horde");
            if (level == 40) unlocks.Add("gamemode_extraction");

            // Equipment unlocks
            if (level == 7) unlocks.Add("armor_vest_tier2");
            if (level == 14) unlocks.Add("armor_vest_tier3");
            if (level == 21) unlocks.Add("armor_vest_tier4");
            if (level == 28) unlocks.Add("armor_vest_tier5");

            return unlocks;
        }

        private string GetLevelTitle(int level)
        {
            if (level < 10) return "Survivor";
            if (level < 20) return "Veteran Survivor";
            if (level < 30) return "Zombie Hunter";
            if (level < 40) return "Elite Hunter";
            if (level < 50) return "Apocalypse Warrior";
            if (level < 60) return "Legendary Survivor";
            if (level < 70) return "Death Incarnate";
            if (level < 80) return "Zombie Slayer";
            if (level < 90) return "Apocalypse Legend";
            if (level < 100) return "Immortal";
            return "Apocalypse God";
        }

        private void InitializeRankDefinitions()
        {
            // Military-style ranks
            rankDefinitions["private"] = new RankDefinition
            {
                rankId = "private",
                rankName = "Private",
                rankTier = 1,
                requiredLevel = 1,
                requiredKills = 0,
                requiredMissions = 0,
                bonuses = new RankBonuses { xpBonus = 0f, damageBonus = 0f, defenseBonus = 0f }
            };

            rankDefinitions["corporal"] = new RankDefinition
            {
                rankId = "corporal",
                rankName = "Corporal",
                rankTier = 2,
                requiredLevel = 10,
                requiredKills = 500,
                requiredMissions = 10,
                bonuses = new RankBonuses { xpBonus = 0.05f, damageBonus = 0.02f, defenseBonus = 0.02f }
            };

            rankDefinitions["sergeant"] = new RankDefinition
            {
                rankId = "sergeant",
                rankName = "Sergeant",
                rankTier = 3,
                requiredLevel = 20,
                requiredKills = 1500,
                requiredMissions = 25,
                bonuses = new RankBonuses { xpBonus = 0.10f, damageBonus = 0.05f, defenseBonus = 0.05f }
            };

            rankDefinitions["lieutenant"] = new RankDefinition
            {
                rankId = "lieutenant",
                rankName = "Lieutenant",
                rankTier = 4,
                requiredLevel = 35,
                requiredKills = 3000,
                requiredMissions = 50,
                bonuses = new RankBonuses { xpBonus = 0.15f, damageBonus = 0.08f, defenseBonus = 0.08f }
            };

            rankDefinitions["captain"] = new RankDefinition
            {
                rankId = "captain",
                rankName = "Captain",
                rankTier = 5,
                requiredLevel = 50,
                requiredKills = 6000,
                requiredMissions = 100,
                bonuses = new RankBonuses { xpBonus = 0.20f, damageBonus = 0.12f, defenseBonus = 0.12f }
            };

            rankDefinitions["major"] = new RankDefinition
            {
                rankId = "major",
                rankName = "Major",
                rankTier = 6,
                requiredLevel = 65,
                requiredKills = 10000,
                requiredMissions = 150,
                bonuses = new RankBonuses { xpBonus = 0.25f, damageBonus = 0.15f, defenseBonus = 0.15f }
            };

            rankDefinitions["colonel"] = new RankDefinition
            {
                rankId = "colonel",
                rankName = "Colonel",
                rankTier = 7,
                requiredLevel = 80,
                requiredKills = 15000,
                requiredMissions = 200,
                bonuses = new RankBonuses { xpBonus = 0.30f, damageBonus = 0.20f, defenseBonus = 0.20f }
            };

            rankDefinitions["general"] = new RankDefinition
            {
                rankId = "general",
                rankName = "General",
                rankTier = 8,
                requiredLevel = 95,
                requiredKills = 25000,
                requiredMissions = 300,
                bonuses = new RankBonuses { xpBonus = 0.40f, damageBonus = 0.25f, defenseBonus = 0.25f }
            };

            Debug.Log($"Initialized {rankDefinitions.Count} rank definitions");
        }

        private void InitializeProgressionTracks()
        {
            // Combat track
            progressionTracks[ProgressionTrack.Combat] = new TrackDefinition
            {
                track = ProgressionTrack.Combat,
                trackName = "Combat Mastery",
                description = "Improve your combat effectiveness",
                maxLevel = 50,
                milestones = GenerateCombatMilestones()
            };

            // Survival track
            progressionTracks[ProgressionTrack.Survival] = new TrackDefinition
            {
                track = ProgressionTrack.Survival,
                trackName = "Survival Expert",
                description = "Master the art of survival",
                maxLevel = 50,
                milestones = GenerateSurvivalMilestones()
            };

            // Social track
            progressionTracks[ProgressionTrack.Social] = new TrackDefinition
            {
                track = ProgressionTrack.Social,
                trackName = "Community Leader",
                description = "Build relationships and lead others",
                maxLevel = 50,
                milestones = GenerateSocialMilestones()
            };

            // Exploration track
            progressionTracks[ProgressionTrack.Exploration] = new TrackDefinition
            {
                track = ProgressionTrack.Exploration,
                trackName = "Master Explorer",
                description = "Discover all secrets of the apocalypse",
                maxLevel = 50,
                milestones = GenerateExplorationMilestones()
            };

            // Crafting track
            progressionTracks[ProgressionTrack.Crafting] = new TrackDefinition
            {
                track = ProgressionTrack.Crafting,
                trackName = "Master Craftsman",
                description = "Create legendary items and structures",
                maxLevel = 50,
                milestones = GenerateCraftingMilestones()
            };

            Debug.Log($"Initialized {progressionTracks.Count} progression tracks");
        }

        private List<TrackMilestone> GenerateCombatMilestones()
        {
            return new List<TrackMilestone>
            {
                new TrackMilestone { level = 10, rewardDescription = "+5% Damage", unlocks = new List<string> { "perk_combat_basic" } },
                new TrackMilestone { level = 20, rewardDescription = "+10% Damage, Unlock Weapon Mod Slot", unlocks = new List<string> { "feature_weapon_mods" } },
                new TrackMilestone { level = 30, rewardDescription = "+15% Damage, Unlock Elite Weapons", unlocks = new List<string> { "weapon_tier_elite" } },
                new TrackMilestone { level = 40, rewardDescription = "+20% Damage, Unlock Combat Abilities", unlocks = new List<string> { "ability_combat_tier3" } },
                new TrackMilestone { level = 50, rewardDescription = "+25% Damage, Legendary Combat Title", unlocks = new List<string> { "title_combat_legend" } }
            };
        }

        private List<TrackMilestone> GenerateSurvivalMilestones()
        {
            return new List<TrackMilestone>
            {
                new TrackMilestone { level = 10, rewardDescription = "+10% Health", unlocks = new List<string> { "perk_survival_basic" } },
                new TrackMilestone { level = 20, rewardDescription = "+20% Health, Faster Healing", unlocks = new List<string> { "ability_fast_heal" } },
                new TrackMilestone { level = 30, rewardDescription = "+30% Health, Unlock Survival Skills", unlocks = new List<string> { "skill_tree_survival" } },
                new TrackMilestone { level = 40, rewardDescription = "+40% Health, Environmental Resistance", unlocks = new List<string> { "perk_environmental_resist" } },
                new TrackMilestone { level = 50, rewardDescription = "+50% Health, Legendary Survivor Title", unlocks = new List<string> { "title_survivor_legend" } }
            };
        }

        private List<TrackMilestone> GenerateSocialMilestones()
        {
            return new List<TrackMilestone>
            {
                new TrackMilestone { level = 10, rewardDescription = "Unlock Party System", unlocks = new List<string> { "feature_party" } },
                new TrackMilestone { level = 20, rewardDescription = "Unlock Clan Creation", unlocks = new List<string> { "feature_clan_create" } },
                new TrackMilestone { level = 30, rewardDescription = "Unlock Trading Bonuses", unlocks = new List<string> { "perk_trade_bonus" } },
                new TrackMilestone { level = 40, rewardDescription = "Unlock Faction Leadership", unlocks = new List<string> { "feature_faction_lead" } },
                new TrackMilestone { level = 50, rewardDescription = "Community Leader Title", unlocks = new List<string> { "title_community_leader" } }
            };
        }

        private List<TrackMilestone> GenerateExplorationMilestones()
        {
            return new List<TrackMilestone>
            {
                new TrackMilestone { level = 10, rewardDescription = "+10% Movement Speed", unlocks = new List<string> { "perk_movement_basic" } },
                new TrackMilestone { level = 20, rewardDescription = "+20% Movement Speed, Better Loot Detection", unlocks = new List<string> { "ability_loot_sense" } },
                new TrackMilestone { level = 30, rewardDescription = "+30% Movement Speed, Unlock Secret Areas", unlocks = new List<string> { "feature_secret_areas" } },
                new TrackMilestone { level = 40, rewardDescription = "+40% Movement Speed, Treasure Hunter", unlocks = new List<string> { "perk_treasure_hunter" } },
                new TrackMilestone { level = 50, rewardDescription = "+50% Movement Speed, Master Explorer Title", unlocks = new List<string> { "title_master_explorer" } }
            };
        }

        private List<TrackMilestone> GenerateCraftingMilestones()
        {
            return new List<TrackMilestone>
            {
                new TrackMilestone { level = 10, rewardDescription = "Unlock Advanced Crafting", unlocks = new List<string> { "crafting_advanced" } },
                new TrackMilestone { level = 20, rewardDescription = "Unlock Rare Blueprints", unlocks = new List<string> { "blueprints_rare" } },
                new TrackMilestone { level = 30, rewardDescription = "Unlock Epic Crafting", unlocks = new List<string> { "crafting_epic" } },
                new TrackMilestone { level = 40, rewardDescription = "Unlock Legendary Blueprints", unlocks = new List<string> { "blueprints_legendary" } },
                new TrackMilestone { level = 50, rewardDescription = "Master Craftsman Title", unlocks = new List<string> { "title_master_craftsman" } }
            };
        }

        private void InitializeSeasonDefinitions()
        {
            seasonDefinitions[1] = new SeasonDefinition
            {
                seasonNumber = 1,
                seasonName = "Season of Survival",
                startDate = new DateTime(2024, 1, 1),
                endDate = new DateTime(2024, 4, 1),
                maxSeasonLevel = 100,
                rewards = GenerateSeasonRewards(1)
            };

            seasonDefinitions[2] = new SeasonDefinition
            {
                seasonNumber = 2,
                seasonName = "Season of Chaos",
                startDate = new DateTime(2024, 4, 1),
                endDate = new DateTime(2024, 7, 1),
                maxSeasonLevel = 100,
                rewards = GenerateSeasonRewards(2)
            };

            Debug.Log($"Initialized {seasonDefinitions.Count} season definitions");
        }

        private Dictionary<int, SeasonReward> GenerateSeasonRewards(int seasonNumber)
        {
            var rewards = new Dictionary<int, SeasonReward>();
            
            for (int level = 1; level <= 100; level++)
            {
                var reward = new SeasonReward
                {
                    level = level,
                    rewardType = level % 10 == 0 ? RewardType.Legendary : (level % 5 == 0 ? RewardType.Epic : RewardType.Common),
                    items = new List<string>(),
                    currency = level * 100
                };

                if (level % 10 == 0) reward.items.Add($"season_{seasonNumber}_legendary_tier_{level / 10}");
                if (level % 25 == 0) reward.items.Add($"season_{seasonNumber}_exclusive_skin");
                if (level == 100) reward.items.Add($"season_{seasonNumber}_ultimate_reward");

                rewards[level] = reward;
            }

            return rewards;
        }

        // Main progression methods
        [ServerRpc(RequireOwnership = false)]
        public void AddExperienceServerRpc(ulong playerId, int xpAmount, string source, ServerRpcParams rpcParams = default)
        {
            if (!playerProgressions.TryGetValue(playerId, out var progression))
            {
                progression = CreateNewProgression(playerId);
                playerProgressions[playerId] = progression;
            }

            // Apply rank bonuses
            float xpMultiplier = 1f;
            if (rankDefinitions.TryGetValue(progression.currentRank, out var rank))
            {
                xpMultiplier += rank.bonuses.xpBonus;
            }

            int adjustedXp = Mathf.RoundToInt(xpAmount * xpMultiplier);
            progression.currentXp += adjustedXp;
            progression.totalXpEarned += adjustedXp;

            // Check for level up
            CheckLevelUp(playerId, progression);

            NotifyXpGainedClientRpc(playerId, adjustedXp, source);
            Debug.Log($"Player {playerId} gained {adjustedXp} XP from {source}");
        }

        private PlayerProgression CreateNewProgression(ulong playerId)
        {
            return new PlayerProgression
            {
                playerId = playerId,
                level = 1,
                currentXp = 0,
                totalXpEarned = 0,
                prestigeLevel = 0,
                currentRank = "private",
                skillPoints = 0,
                unlockedFeatures = new List<string>(),
                trackProgress = new Dictionary<ProgressionTrack, int>
                {
                    { ProgressionTrack.Combat, 0 },
                    { ProgressionTrack.Survival, 0 },
                    { ProgressionTrack.Social, 0 },
                    { ProgressionTrack.Exploration, 0 },
                    { ProgressionTrack.Crafting, 0 }
                },
                statistics = new PlayerStatistics
                {
                    totalKills = 0,
                    totalDeaths = 0,
                    missionsCompleted = 0,
                    distanceTraveled = 0f,
                    itemsCrafted = 0,
                    tradesCompleted = 0
                },
                currentSeasonLevel = 0
            };
        }

        private void CheckLevelUp(ulong playerId, PlayerProgression progression)
        {
            bool leveledUp = false;

            while (progression.level < maxLevel && levelDefinitions.TryGetValue(progression.level + 1, out var nextLevel))
            {
                if (progression.currentXp >= nextLevel.xpRequired)
                {
                    progression.currentXp -= nextLevel.xpRequired;
                    progression.level++;
                    leveledUp = true;

                    // Grant rewards
                    GrantLevelRewards(playerId, progression, nextLevel);

                    // Check for rank promotion
                    CheckRankPromotion(playerId, progression);

                    OnPlayerLevelUp?.Invoke(playerId, progression.level);
                    NotifyLevelUpClientRpc(playerId, progression.level);

                    Debug.Log($"Player {playerId} leveled up to {progression.level}");
                }
                else
                {
                    break;
                }
            }
        }

        private void GrantLevelRewards(ulong playerId, PlayerProgression progression, LevelDefinition levelDef)
        {
            // Grant currency
            if (levelDef.rewards.softCurrency > 0)
            {
                Economy.EconomyManager.Instance?.AddSoftCurrency(playerId, levelDef.rewards.softCurrency);
            }

            if (levelDef.rewards.hardCurrency > 0)
            {
                Economy.EconomyManager.Instance?.AddHardCurrency(playerId, levelDef.rewards.hardCurrency);
            }

            // Grant skill points
            progression.skillPoints += levelDef.rewards.skillPoints;

            // Grant items
            foreach (var itemId in levelDef.rewards.items)
            {
                // Would integrate with inventory system
                // Inventory.InventoryManager.Instance?.AddItem(playerId, itemId, 1);
            }

            // Unlock features
            foreach (var unlock in levelDef.unlocks)
            {
                if (!progression.unlockedFeatures.Contains(unlock))
                {
                    progression.unlockedFeatures.Add(unlock);
                    OnUnlockAchieved?.Invoke(playerId, unlock);
                    NotifyUnlockClientRpc(playerId, unlock);
                }
            }
        }

        private void CheckRankPromotion(ulong playerId, PlayerProgression progression)
        {
            var eligibleRanks = rankDefinitions.Values
                .Where(r => r.requiredLevel <= progression.level &&
                           r.requiredKills <= progression.statistics.totalKills &&
                           r.requiredMissions <= progression.statistics.missionsCompleted)
                .OrderByDescending(r => r.rankTier)
                .ToList();

            if (eligibleRanks.Count > 0)
            {
                var newRank = eligibleRanks[0];
                if (newRank.rankId != progression.currentRank)
                {
                    progression.currentRank = newRank.rankId;
                    OnRankPromoted?.Invoke(playerId, newRank.rankId);
                    NotifyRankPromotionClientRpc(playerId, newRank.rankId, newRank.rankName);
                    Debug.Log($"Player {playerId} promoted to {newRank.rankName}");
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void AddTrackExperienceServerRpc(ulong playerId, ProgressionTrack track, int xpAmount, ServerRpcParams rpcParams = default)
        {
            if (!playerProgressions.TryGetValue(playerId, out var progression)) return;
            if (!progression.trackProgress.ContainsKey(track)) return;

            int currentLevel = progression.trackProgress[track];
            if (currentLevel >= progressionTracks[track].maxLevel) return;

            progression.trackProgress[track] += xpAmount;

            // Check for track level up (simple: every 1000 XP = 1 level)
            int newLevel = progression.trackProgress[track] / 1000;
            if (newLevel > currentLevel)
            {
                OnTrackLevelUp?.Invoke(playerId, track, newLevel);
                CheckTrackMilestones(playerId, progression, track, newLevel);
                NotifyTrackLevelUpClientRpc(playerId, track, newLevel);
            }
        }

        private void CheckTrackMilestones(ulong playerId, PlayerProgression progression, ProgressionTrack track, int level)
        {
            if (!progressionTracks.TryGetValue(track, out var trackDef)) return;

            var milestone = trackDef.milestones.FirstOrDefault(m => m.level == level);
            if (milestone != null)
            {
                foreach (var unlock in milestone.unlocks)
                {
                    if (!progression.unlockedFeatures.Contains(unlock))
                    {
                        progression.unlockedFeatures.Add(unlock);
                        OnUnlockAchieved?.Invoke(playerId, unlock);
                    }
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void PrestigeServerRpc(ulong playerId, ServerRpcParams rpcParams = default)
        {
            if (!playerProgressions.TryGetValue(playerId, out var progression)) return;

            if (progression.level < maxLevel)
            {
                Debug.LogWarning($"Player {playerId} must be max level to prestige");
                return;
            }

            if (progression.prestigeLevel >= maxPrestigeLevel)
            {
                Debug.LogWarning($"Player {playerId} is already max prestige");
                return;
            }

            // Reset progression but keep some benefits
            int oldPrestigeLevel = progression.prestigeLevel;
            progression.prestigeLevel++;
            progression.level = 1;
            progression.currentXp = 0;
            progression.currentRank = "private";

            // Keep unlocks and track progress
            // Grant prestige rewards
            int prestigeReward = progression.prestigeLevel * 10000; // Soft currency
            int prestigeGems = progression.prestigeLevel * 500; // Hard currency
            Economy.EconomyManager.Instance?.AddSoftCurrency(playerId, prestigeReward);
            Economy.EconomyManager.Instance?.AddHardCurrency(playerId, prestigeGems);

            OnPlayerPrestige?.Invoke(playerId, progression.prestigeLevel);
            NotifyPrestigeClientRpc(playerId, progression.prestigeLevel);

            Debug.Log($"Player {playerId} prestiged to level {progression.prestigeLevel}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void UpdateStatisticServerRpc(ulong playerId, string statisticName, float value, ServerRpcParams rpcParams = default)
        {
            if (!playerProgressions.TryGetValue(playerId, out var progression)) return;

            switch (statisticName)
            {
                case "kills":
                    progression.statistics.totalKills += (int)value;
                    break;
                case "deaths":
                    progression.statistics.totalDeaths += (int)value;
                    break;
                case "missions":
                    progression.statistics.missionsCompleted += (int)value;
                    break;
                case "distance":
                    progression.statistics.distanceTraveled += value;
                    break;
                case "crafted":
                    progression.statistics.itemsCrafted += (int)value;
                    break;
                case "trades":
                    progression.statistics.tradesCompleted += (int)value;
                    break;
            }

            // Check rank promotion after stat update
            CheckRankPromotion(playerId, progression);
        }

        // Client RPCs
        [ClientRpc]
        private void NotifyXpGainedClientRpc(ulong playerId, int xpAmount, string source) { }

        [ClientRpc]
        private void NotifyLevelUpClientRpc(ulong playerId, int newLevel)
        {
            OnPlayerLevelUp?.Invoke(playerId, newLevel);
        }

        [ClientRpc]
        private void NotifyUnlockClientRpc(ulong playerId, string unlockId) { }

        [ClientRpc]
        private void NotifyRankPromotionClientRpc(ulong playerId, string rankId, string rankName) { }

        [ClientRpc]
        private void NotifyTrackLevelUpClientRpc(ulong playerId, ProgressionTrack track, int newLevel) { }

        [ClientRpc]
        private void NotifyPrestigeClientRpc(ulong playerId, int prestigeLevel) { }

        // Public getters
        public PlayerProgression GetPlayerProgression(ulong playerId) => playerProgressions.GetValueOrDefault(playerId);
        public LevelDefinition GetLevelDefinition(int level) => levelDefinitions.GetValueOrDefault(level);
        public RankDefinition GetRankDefinition(string rankId) => rankDefinitions.GetValueOrDefault(rankId);
        public TrackDefinition GetTrackDefinition(ProgressionTrack track) => progressionTracks.GetValueOrDefault(track);
        public bool IsFeatureUnlocked(ulong playerId, string featureId) => playerProgressions.TryGetValue(playerId, out var prog) && prog.unlockedFeatures.Contains(featureId);
    }

    // Data structures
    [Serializable]
    public class PlayerProgression
    {
        public ulong playerId;
        public int level;
        public int currentXp;
        public int totalXpEarned;
        public int prestigeLevel;
        public string currentRank;
        public int skillPoints;
        public List<string> unlockedFeatures;
        public Dictionary<ProgressionTrack, int> trackProgress;
        public PlayerStatistics statistics;
        public int currentSeasonLevel;
    }

    [Serializable]
    public class LevelDefinition
    {
        public int level;
        public int xpRequired;
        public LevelRewards rewards;
        public List<string> unlocks;
        public string title;
    }

    [Serializable]
    public class LevelRewards
    {
        public int softCurrency;
        public int hardCurrency;
        public int skillPoints;
        public List<string> items;
    }

    [Serializable]
    public class RankDefinition
    {
        public string rankId;
        public string rankName;
        public int rankTier;
        public int requiredLevel;
        public int requiredKills;
        public int requiredMissions;
        public RankBonuses bonuses;
    }

    [Serializable]
    public class RankBonuses
    {
        public float xpBonus;
        public float damageBonus;
        public float defenseBonus;
    }

    [Serializable]
    public class TrackDefinition
    {
        public ProgressionTrack track;
        public string trackName;
        public string description;
        public int maxLevel;
        public List<TrackMilestone> milestones;
    }

    [Serializable]
    public class TrackMilestone
    {
        public int level;
        public string rewardDescription;
        public List<string> unlocks;
    }

    [Serializable]
    public class PlayerStatistics
    {
        public int totalKills;
        public int totalDeaths;
        public int missionsCompleted;
        public float distanceTraveled;
        public int itemsCrafted;
        public int tradesCompleted;
    }

    [Serializable]
    public class SeasonDefinition
    {
        public int seasonNumber;
        public string seasonName;
        public DateTime startDate;
        public DateTime endDate;
        public int maxSeasonLevel;
        public Dictionary<int, SeasonReward> rewards;
    }

    [Serializable]
    public class SeasonReward
    {
        public int level;
        public RewardType rewardType;
        public List<string> items;
        public int currency;
    }

    public enum ProgressionTrack { Combat, Survival, Social, Exploration, Crafting }
    public enum RewardType { Common, Uncommon, Rare, Epic, Legendary }
}
