using UnityEngine;

namespace DeadFrontier.Quests
{
    /// <summary>
    /// ScriptableObject defining quest data and objectives.
    /// </summary>
    [CreateAssetMenu(fileName = "New Quest", menuName = "Dead Frontier/Quests/Quest Data")]
    public class QuestDataSO : ScriptableObject
    {
        [Header("Identification")]
        public string questId;
        public string questName;
        [TextArea(3, 5)]
        public string description;
        [TextArea(3, 5)]
        public string completionText;
        public QuestType questType;
        public QuestCategory category;

        [Header("Visuals")]
        public Sprite questIcon;
        public string questGiverNpcId;
        public string turnInNpcId;

        [Header("Objectives")]
        public QuestObjective[] objectives;

        [Header("Requirements")]
        public int requiredLevel = 1;
        public string[] prerequisiteQuestIds;
        public int requiredReputation = 0;
        public string requiredFactionId;

        [Header("Rewards")]
        public QuestRewards rewards;

        [Header("Timing")]
        public bool isTimeLimited;
        [Range(0f, 86400f)]
        public float timeLimit = 0f;
        public bool isRepeatable;
        [Range(0f, 86400f)]
        public float repeatCooldown = 86400f; // 24 hours default

        [Header("Difficulty")]
        public QuestDifficulty difficulty;
        public int recommendedPlayers = 1;

        [Header("Location")]
        public string mapId;
        public Vector3[] objectiveMarkers;
        public float searchRadius = 50f;

        [Header("Dialogue")]
        public DialogueNode[] startDialogue;
        public DialogueNode[] progressDialogue;
        public DialogueNode[] completionDialogue;

        [Header("Audio")]
        public AudioClip questAcceptSound;
        public AudioClip questCompleteSound;
        public AudioClip objectiveCompleteSound;
    }

    [System.Serializable]
    public class QuestObjective
    {
        public string objectiveId;
        public string description;
        public ObjectiveType objectiveType;
        public string targetId; // Enemy ID, item ID, location ID, etc.
        public int targetAmount = 1;
        public bool isOptional;
        public bool isHidden;
        public Vector3 targetLocation;
        public float locationRadius = 10f;
        public QuestRewards bonusRewards;
    }

    [System.Serializable]
    public class QuestRewards
    {
        public int xpReward;
        public int softCurrencyReward;
        public int hardCurrencyReward;
        public int reputationReward;
        public ItemReward[] itemRewards;
        public string unlockedQuestId;
        public string unlockedRecipeId;
        public int battlePassXP;
    }

    [System.Serializable]
    public class ItemReward
    {
        public string itemId;
        public int quantity = 1;
        public Gameplay.ItemRarity guaranteedRarity;
        public bool isChoice; // Player can choose this OR another reward
    }

    [System.Serializable]
    public class DialogueNode
    {
        public string speakerId;
        [TextArea(2, 4)]
        public string dialogueText;
        public AudioClip voiceLine;
        public DialogueChoice[] choices;
    }

    [System.Serializable]
    public class DialogueChoice
    {
        public string choiceText;
        public int nextNodeIndex = -1;
        public QuestDialogueEffect effect;
        public int effectValue;
    }

    public enum QuestType
    {
        MainStory,
        SideQuest,
        Daily,
        Weekly,
        Event,
        Bounty,
        Contract,
        Tutorial
    }

    public enum QuestCategory
    {
        Combat,
        Exploration,
        Collection,
        Delivery,
        Escort,
        Defense,
        Stealth,
        Investigation,
        Boss
    }

    public enum ObjectiveType
    {
        KillEnemies,
        KillSpecificEnemy,
        CollectItems,
        DeliverItems,
        ReachLocation,
        DefendLocation,
        EscortNPC,
        SurviveTime,
        InteractWithObject,
        CraftItem,
        EquipItem,
        UseAbility,
        CompleteWithoutDamage,
        CompleteInTime,
        TalkToNPC,
        DiscoverArea,
        PhotoObjective,
        HackTerminal
    }

    public enum QuestDifficulty
    {
        Easy,
        Normal,
        Hard,
        Nightmare,
        Impossible
    }

    public enum QuestDialogueEffect
    {
        None,
        ReputationGain,
        ReputationLoss,
        ItemGain,
        ItemLoss,
        UnlockDialogue,
        StartQuest,
        FailQuest
    }
}
