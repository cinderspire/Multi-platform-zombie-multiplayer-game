using UnityEngine;

namespace DeadFrontier.Player
{
    /// <summary>
    /// Integrates player actions with all progression systems
    /// Ensures everything is tracked correctly for achievements, analytics, and stats
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerStatsIntegrator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private PlayerProgression progression;
        [SerializeField] private Perks.LoadoutSystem loadoutSystem;

        [Header("Match Stats")]
        private int matchKills = 0;
        private int matchDeaths = 0;
        private int matchHeadshots = 0;
        private int matchZombieKills = 0;
        private float matchDamageDealt = 0f;
        private float matchDamageTaken = 0f;
        private float matchDistanceTraveled = 0f;
        private int matchItemsLooted = 0;
        private bool hasExtracted = false;

        // Tracking
        private Vector3 lastPosition;
        private float matchStartTime;

        private void Awake()
        {
            // Get components
            if (playerController == null)
                playerController = GetComponent<PlayerController>();
            if (playerHealth == null)
                playerHealth = GetComponent<PlayerHealth>();
            if (progression == null)
                progression = GetComponent<PlayerProgression>();
            if (loadoutSystem == null)
                loadoutSystem = GetComponent<Perks.LoadoutSystem>();
        }

        private void Start()
        {
            // Subscribe to events
            SubscribeToEvents();

            // Initialize tracking
            lastPosition = transform.position;
            matchStartTime = Time.time;
        }

        private void Update()
        {
            // Track distance traveled
            float distance = Vector3.Distance(transform.position, lastPosition);
            matchDistanceTraveled += distance;
            lastPosition = transform.position;
        }

        #region Event Subscriptions

        private void SubscribeToEvents()
        {
            // Health events
            if (playerHealth != null)
            {
                playerHealth.OnDamaged += HandlePlayerDamaged;
                playerHealth.OnDeath += HandlePlayerDeath;
            }

            // Subscribe to weapon events (would need to be added to WeaponController)
            // Subscribe to loot events (would need to be added to InventorySystem)
        }

        #endregion

        #region Event Handlers

        private void HandlePlayerDamaged(float damageAmount, float remainingHealth)
        {
            matchDamageTaken += damageAmount;

            // Track analytics
            Core.Analytics.AnalyticsManager.Instance?.TrackEvent("player_damaged", new System.Collections.Generic.Dictionary<string, object>
            {
                { "damage", damageAmount },
                { "health_remaining", remainingHealth }
            });
        }

        private void HandlePlayerDeath()
        {
            matchDeaths++;

            // Award XP if applicable (or penalize)
            // progression?.AwardXP(-50, "Death penalty");

            // Track achievement
            Achievements.AchievementManager.Instance?.TrackEvent(
                Achievements.AchievementEventType.MatchLost,
                1
            );

            // Track analytics
            Core.Analytics.AnalyticsManager.Instance?.TrackPlayerDeath(
                "zombie", // Would need to track actual cause
                transform.position
            );
        }

        /// <summary>
        /// Call this when player kills a zombie
        /// </summary>
        public void OnZombieKilled(Zombies.ZombieAI zombie, bool isHeadshot, Weapons.WeaponData weaponUsed)
        {
            matchKills++;
            matchZombieKills++;

            if (isHeadshot)
            {
                matchHeadshots++;
            }

            // Award XP
            if (progression != null)
            {
                progression.AwardZombieKill(isHeadshot);
            }

            // Update battle pass
            Progression.BattlePass.BattlePassManager.Instance?.AwardXP(10, "Zombie Kill");

            // Track achievements
            Achievements.AchievementManager.Instance?.TrackEvent(
                Achievements.AchievementEventType.ZombieKilled,
                1
            );

            if (isHeadshot)
            {
                Achievements.AchievementManager.Instance?.TrackEvent(
                    Achievements.AchievementEventType.HeadshotKill,
                    1
                );
            }

            // Track analytics
            Core.Analytics.AnalyticsManager.Instance?.TrackZombieKill(
                zombie.Config.zombieType.ToString(),
                weaponUsed?.weaponName ?? "Unknown",
                isHeadshot
            );

            // Update challenges
            Gameplay.Challenges.ChallengeManager.Instance?.UpdateProgress(
                Gameplay.Challenges.ChallengeObjectiveType.KillZombies,
                1
            );

            if (isHeadshot)
            {
                Gameplay.Challenges.ChallengeManager.Instance?.UpdateProgress(
                    Gameplay.Challenges.ChallengeObjectiveType.GetHeadshots,
                    1
                );
            }
        }

        /// <summary>
        /// Call this when player kills another player
        /// </summary>
        public void OnPlayerKilled(NetworkedPlayer targetPlayer, bool isHeadshot, Weapons.WeaponData weaponUsed)
        {
            matchKills++;

            if (isHeadshot)
            {
                matchHeadshots++;
            }

            // Award XP
            if (progression != null)
            {
                progression.AwardPlayerKill(isHeadshot);
            }

            // Update battle pass
            Progression.BattlePass.BattlePassManager.Instance?.AwardXP(50, "Player Kill");

            // Track achievements
            Achievements.AchievementManager.Instance?.TrackEvent(
                Achievements.AchievementEventType.PlayerKilled,
                1
            );

            // Track analytics
            Core.Analytics.AnalyticsManager.Instance?.TrackEvent("player_killed", new System.Collections.Generic.Dictionary<string, object>
            {
                { "weapon", weaponUsed?.weaponName ?? "Unknown" },
                { "headshot", isHeadshot },
                { "distance", Vector3.Distance(transform.position, targetPlayer.transform.position) }
            });

            // Update challenges
            Gameplay.Challenges.ChallengeManager.Instance?.UpdateProgress(
                Gameplay.Challenges.ChallengeObjectiveType.KillPlayers,
                1
            );
        }

        /// <summary>
        /// Call this when player loots an item
        /// </summary>
        public void OnItemLooted(Items.ItemData item, int quantity)
        {
            matchItemsLooted += quantity;

            // Award XP
            if (progression != null)
            {
                progression.AwardItemLooted(item.rarity);
            }

            // Track achievements
            Achievements.AchievementManager.Instance?.TrackEvent(
                Achievements.AchievementEventType.ItemLooted,
                quantity
            );

            if ((int)item.rarity >= (int)Items.ItemRarity.Rare)
            {
                Achievements.AchievementManager.Instance?.TrackEvent(
                    Achievements.AchievementEventType.RareItemLooted,
                    quantity
                );
            }

            // Track analytics
            Core.Analytics.AnalyticsManager.Instance?.TrackEvent("item_looted", new System.Collections.Generic.Dictionary<string, object>
            {
                { "item_id", item.itemID },
                { "rarity", item.rarity.ToString() },
                { "quantity", quantity }
            });

            // Update challenges
            Gameplay.Challenges.ChallengeManager.Instance?.UpdateProgress(
                Gameplay.Challenges.ChallengeObjectiveType.LootItems,
                quantity
            );

            if ((int)item.rarity >= (int)Items.ItemRarity.Rare)
            {
                Gameplay.Challenges.ChallengeManager.Instance?.UpdateProgress(
                    Gameplay.Challenges.ChallengeObjectiveType.LootRareItems,
                    quantity
                );
            }
        }

        /// <summary>
        /// Call this when player successfully extracts
        /// </summary>
        public void OnSuccessfulExtraction(int itemsExtracted, int totalValue)
        {
            hasExtracted = true;

            // Award XP
            if (progression != null)
            {
                progression.AwardExtraction(itemsExtracted);
            }

            // Update battle pass
            Progression.BattlePass.BattlePassManager.Instance?.AwardXP(200, "Successful Extraction");

            // Track achievements
            Achievements.AchievementManager.Instance?.TrackEvent(
                Achievements.AchievementEventType.SuccessfulExtraction,
                1
            );

            // Check for special achievements
            if (matchDamageTaken == 0)
            {
                // Untouchable achievement
                Achievements.AchievementManager.Instance?.UnlockAchievement("achievement_untouchable");
            }

            if (playerHealth.CurrentHealth < 10)
            {
                // Close Call achievement
                Achievements.AchievementManager.Instance?.UnlockAchievement("achievement_close_call");
            }

            // Track analytics
            Core.Analytics.AnalyticsManager.Instance?.TrackExtraction(
                true,
                itemsExtracted,
                totalValue
            );

            // Update challenges
            Gameplay.Challenges.ChallengeManager.Instance?.UpdateProgress(
                Gameplay.Challenges.ChallengeObjectiveType.SuccessfulExtractions,
                1
            );

            Gameplay.Challenges.ChallengeManager.Instance?.UpdateProgress(
                Gameplay.Challenges.ChallengeObjectiveType.ExtractItems,
                itemsExtracted
            );

            // Award match completion
            OnMatchCompleted(true);
        }

        /// <summary>
        /// Call this when match ends (extracted or died)
        /// </summary>
        public void OnMatchCompleted(bool victory)
        {
            float matchDuration = Time.time - matchStartTime;

            // Award survival XP
            if (progression != null)
            {
                progression.AwardSurvivalTime(matchDuration / 60f);
            }

            // Track achievements
            Achievements.AchievementManager.Instance?.TrackEvent(
                Achievements.AchievementEventType.MatchStarted,
                1
            );

            if (victory)
            {
                Achievements.AchievementManager.Instance?.TrackEvent(
                    Achievements.AchievementEventType.MatchWon,
                    1
                );
            }

            // Track analytics
            Core.Analytics.AnalyticsManager.Instance?.TrackMatchEnd(
                victory ? "extracted" : "died",
                matchDuration,
                matchKills,
                matchDeaths,
                hasExtracted
            );

            // Update challenges
            Gameplay.Challenges.ChallengeManager.Instance?.UpdateProgress(
                Gameplay.Challenges.ChallengeObjectiveType.CompleteMatches,
                1
            );

            if (victory)
            {
                Gameplay.Challenges.ChallengeManager.Instance?.UpdateProgress(
                    Gameplay.Challenges.ChallengeObjectiveType.WinMatches,
                    1
                );
            }

            // Save progress
            Core.Save.SaveSystem.Instance?.SaveGame(Core.Save.SaveSystem.Instance.CurrentSlot);

            Debug.Log($"[PlayerStatsIntegrator] Match completed - K:{matchKills} D:{matchDeaths} Items:{matchItemsLooted} Extracted:{hasExtracted}");
        }

        /// <summary>
        /// Call this when weapon is fired
        /// </summary>
        public void OnWeaponFired(Weapons.WeaponData weapon, bool hit, float distance)
        {
            // Track analytics (sampled to avoid spam)
            if (UnityEngine.Random.value < 0.1f) // 10% sampling
            {
                Core.Analytics.AnalyticsManager.Instance?.TrackWeaponFired(
                    weapon.weaponID,
                    hit,
                    distance
                );
            }

            // Update challenges
            Gameplay.Challenges.ChallengeManager.Instance?.UpdateProgress(
                Gameplay.Challenges.ChallengeObjectiveType.GetKillsWithWeapon,
                hit ? 1 : 0,
                weapon.weaponID
            );
        }

        /// <summary>
        /// Call this when player levels up
        /// </summary>
        public void OnLevelUp(int newLevel, int prestigeLevel)
        {
            // Track achievements
            Achievements.AchievementManager.Instance?.TrackEvent(
                Achievements.AchievementEventType.LevelUp,
                1
            );

            // Track analytics
            Core.Analytics.AnalyticsManager.Instance?.TrackLevelUp(newLevel, prestigeLevel);

            // Show notification
            Debug.Log($"[PlayerStatsIntegrator] LEVEL UP! Now level {newLevel}");
        }

        /// <summary>
        /// Call this when loadout is changed
        /// </summary>
        public void OnLoadoutChanged()
        {
            if (loadoutSystem == null)
                return;

            // Track achievements
            Achievements.AchievementManager.Instance?.TrackEvent(
                Achievements.AchievementEventType.LoadoutSaved,
                1
            );

            // Track analytics
            var primaryWeapons = loadoutSystem.GetPrimaryWeapons();
            var perks = loadoutSystem.GetEquippedPerks();

            var weaponNames = new System.Collections.Generic.List<string>();
            foreach (var weapon in primaryWeapons)
            {
                if (weapon != null)
                    weaponNames.Add(weapon.weaponName);
            }

            var perkNames = new System.Collections.Generic.List<string>();
            foreach (var perk in perks)
            {
                if (perk != null)
                    perkNames.Add(perk.perkName);
            }

            Core.Analytics.AnalyticsManager.Instance?.TrackLoadoutChanged(weaponNames, perkNames);
        }

        #endregion

        #region Match Stats

        /// <summary>
        /// Gets current match statistics
        /// </summary>
        public MatchStats GetMatchStats()
        {
            return new MatchStats
            {
                kills = matchKills,
                deaths = matchDeaths,
                headshots = matchHeadshots,
                zombieKills = matchZombieKills,
                damageDealt = matchDamageDealt,
                damageTaken = matchDamageTaken,
                distanceTraveled = matchDistanceTraveled,
                itemsLooted = matchItemsLooted,
                matchDuration = Time.time - matchStartTime,
                extracted = hasExtracted
            };
        }

        /// <summary>
        /// Resets match statistics
        /// </summary>
        public void ResetMatchStats()
        {
            matchKills = 0;
            matchDeaths = 0;
            matchHeadshots = 0;
            matchZombieKills = 0;
            matchDamageDealt = 0f;
            matchDamageTaken = 0f;
            matchDistanceTraveled = 0f;
            matchItemsLooted = 0;
            hasExtracted = false;
            matchStartTime = Time.time;
            lastPosition = transform.position;
        }

        #endregion

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (playerHealth != null)
            {
                playerHealth.OnDamaged -= HandlePlayerDamaged;
                playerHealth.OnDeath -= HandlePlayerDeath;
            }
        }
    }

    /// <summary>
    /// Match statistics data
    /// </summary>
    [System.Serializable]
    public struct MatchStats
    {
        public int kills;
        public int deaths;
        public int headshots;
        public int zombieKills;
        public float damageDealt;
        public float damageTaken;
        public float distanceTraveled;
        public int itemsLooted;
        public float matchDuration;
        public bool extracted;

        public float GetKDRatio()
        {
            return deaths > 0 ? (float)kills / deaths : kills;
        }

        public float GetHeadshotPercentage()
        {
            return kills > 0 ? (float)headshots / kills * 100f : 0f;
        }
    }
}
