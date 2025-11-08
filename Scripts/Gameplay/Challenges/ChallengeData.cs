using UnityEngine;

namespace DeadFrontier.Gameplay.Challenges
{
    /// <summary>
    /// ScriptableObject defining a challenge/mission
    /// </summary>
    [CreateAssetMenu(fileName = "New Challenge", menuName = "DeadFrontier/Challenge Data")]
    public class ChallengeData : ScriptableObject
    {
        [Header("Basic Info")]
        public string challengeName = "New Challenge";
        public string challengeID;

        [TextArea(3, 5)]
        public string description = "Complete this challenge to earn rewards";

        public Sprite icon;
        public ChallengeType challengeType = ChallengeType.Daily;
        public ChallengeDifficulty difficulty = ChallengeDifficulty.Easy;

        [Header("Objectives")]
        public ChallengeObjective[] objectives;

        [Header("Rewards")]
        public int xpReward = 100;
        public int currencyReward = 50;
        public string[] itemRewards; // Item IDs to award
        public int battlePassXPReward = 0; // Battle pass progression

        [Header("Time Limit")]
        public bool hasTimeLimit = false;
        public float timeLimitHours = 24f; // For daily/weekly challenges

        [Header("Requirements")]
        public int levelRequired = 1;
        public string[] prerequisiteChallenges; // Must complete these first

        /// <summary>
        /// Checks if all objectives are complete
        /// </summary>
        public bool IsComplete(ChallengeProgress progress)
        {
            if (progress == null || progress.challengeID != challengeID)
                return false;

            for (int i = 0; i < objectives.Length; i++)
            {
                if (i >= progress.objectiveProgress.Length)
                    return false;

                if (progress.objectiveProgress[i] < objectives[i].targetValue)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Gets completion percentage (0-1)
        /// </summary>
        public float GetCompletionPercentage(ChallengeProgress progress)
        {
            if (progress == null || objectives.Length == 0)
                return 0f;

            float totalProgress = 0f;

            for (int i = 0; i < objectives.Length; i++)
            {
                if (i < progress.objectiveProgress.Length)
                {
                    float objProgress = Mathf.Clamp01(progress.objectiveProgress[i] / objectives[i].targetValue);
                    totalProgress += objProgress;
                }
            }

            return totalProgress / objectives.Length;
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(challengeID))
            {
                challengeID = System.Guid.NewGuid().ToString();
            }
        }
    }

    #region Enums and Structs

    public enum ChallengeType
    {
        Daily,          // Refreshes daily
        Weekly,         // Refreshes weekly
        Seasonal,       // Lasts entire season
        Career,         // Permanent lifetime challenges
        Special,        // Limited-time events
        BattlePass      // Battle pass exclusive
    }

    public enum ChallengeDifficulty
    {
        Easy,
        Medium,
        Hard,
        Expert,
        Master
    }

    public enum ChallengeObjectiveType
    {
        // Combat
        KillZombies,
        KillPlayers,
        GetHeadshots,
        DealDamage,
        SurviveTime,
        KillZombieType, // Specific zombie type

        // Extraction
        SuccessfulExtractions,
        ExtractItems,
        ExtractWithoutDying,

        // Weapons
        GetKillsWithWeapon,
        GetKillsWithWeaponType,
        DealDamageWithWeapon,

        // Looting
        LootItems,
        LootRareItems,
        CollectCurrency,

        // Movement
        TravelDistance,
        CompleteWithoutSprinting,

        // Survival
        SurviveWithoutTakingDamage,
        HealOthers,
        ReviveTeammates,

        // Misc
        CompleteMatches,
        WinMatches,
        CallExtractionFirst,
        ExtractAlone
    }

    [System.Serializable]
    public struct ChallengeObjective
    {
        [Header("Objective")]
        public ChallengeObjectiveType objectiveType;

        [Tooltip("Description shown to player")]
        public string description;

        [Tooltip("How many needed to complete")]
        public float targetValue;

        [Header("Specific Requirements (if applicable)")]
        [Tooltip("Specific weapon ID (for weapon-specific challenges)")]
        public string weaponID;

        [Tooltip("Specific zombie type (for zombie-type challenges)")]
        public Zombies.ZombieType zombieType;

        [Tooltip("Specific item rarity (for rarity challenges)")]
        public Items.ItemRarity itemRarity;

        /// <summary>
        /// Gets formatted objective text
        /// </summary>
        public string GetFormattedText(float currentProgress)
        {
            string progress = $"({currentProgress}/{targetValue})";

            if (string.IsNullOrEmpty(description))
            {
                return $"{objectiveType} {progress}";
            }

            return $"{description} {progress}";
        }
    }

    #endregion
}
