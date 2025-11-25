using UnityEngine;
using System.Collections.Generic;

namespace DeadFrontier.Dialogue
{
    /// <summary>
    /// ScriptableObject defining NPC dialogue trees and conversations.
    /// </summary>
    [CreateAssetMenu(fileName = "New Dialogue", menuName = "Dead Frontier/Dialogue/Dialogue Data")]
    public class DialogueDataSO : ScriptableObject
    {
        [Header("Identification")]
        public string dialogueId;
        public string dialogueName;
        public DialogueType dialogueType;
        public string speakerId;
        public string speakerName;

        [Header("Visuals")]
        public Sprite speakerPortrait;
        public Sprite[] emotionPortraits;

        [Header("Dialogue Content")]
        public DialogueEntry[] entries;
        public int startEntryIndex = 0;

        [Header("Conditions")]
        public DialogueCondition[] startConditions;
        public bool repeatable = true;
        public float cooldown = 0f;

        [Header("Rewards")]
        public DialogueReward[] completionRewards;

        [Header("Audio")]
        public AudioClip backgroundMusic;
        public float musicVolume = 0.5f;
    }

    [System.Serializable]
    public class DialogueEntry
    {
        public int entryIndex;
        public string speakerId;
        public SpeakerEmotion emotion;

        [TextArea(3, 6)]
        public string dialogueText;

        public AudioClip voiceLine;
        public float textSpeed = 0.05f;
        public float autoAdvanceDelay = 0f;

        [Header("Animation")]
        public string animationTrigger;
        public bool cameraFocusOnSpeaker;

        [Header("Choices")]
        public DialogueChoice[] choices;

        [Header("Events")]
        public DialogueEvent[] events;

        [Header("Conditions")]
        public DialogueCondition[] entryConditions;
        public int fallbackEntryIndex = -1;
    }

    [System.Serializable]
    public class DialogueChoice
    {
        [TextArea(1, 2)]
        public string choiceText;
        public int nextEntryIndex = -1;
        public bool endsDialogue;

        [Header("Conditions")]
        public DialogueCondition[] conditions;
        public bool hideIfConditionsFail = true;
        public string failText;

        [Header("Effects")]
        public DialogueEffect[] effects;

        [Header("Visuals")]
        public Color choiceColor = Color.white;
        public Sprite choiceIcon;
    }

    [System.Serializable]
    public class DialogueCondition
    {
        public ConditionType conditionType;
        public string targetId;
        public ComparisonType comparison;
        public float value;
        public bool invertCondition;
    }

    [System.Serializable]
    public class DialogueEffect
    {
        public EffectType effectType;
        public string targetId;
        public float value;
        public bool isTemporary;
        public float duration;
    }

    [System.Serializable]
    public class DialogueEvent
    {
        public EventTiming timing;
        public EventType eventType;
        public string eventData;
        public float delay;
    }

    [System.Serializable]
    public class DialogueReward
    {
        public RewardType rewardType;
        public string itemId;
        public int amount;
    }

    public enum DialogueType
    {
        Conversation,
        Quest,
        Shop,
        Tutorial,
        Cinematic,
        Ambient,
        Radio
    }

    public enum SpeakerEmotion
    {
        Neutral,
        Happy,
        Sad,
        Angry,
        Scared,
        Surprised,
        Thinking,
        Determined
    }

    public enum ConditionType
    {
        HasItem,
        HasCurrency,
        HasLevel,
        HasReputation,
        HasQuest,
        QuestComplete,
        QuestActive,
        HasAchievement,
        HasPerk,
        HasSkill,
        TimeOfDay,
        Weather,
        PlayerStat,
        DialogueViewed,
        ChoiceMade
    }

    public enum ComparisonType
    {
        Equal,
        NotEqual,
        GreaterThan,
        LessThan,
        GreaterOrEqual,
        LessOrEqual
    }

    public enum EffectType
    {
        AddItem,
        RemoveItem,
        AddCurrency,
        RemoveCurrency,
        AddReputation,
        RemoveReputation,
        AddXP,
        StartQuest,
        CompleteQuest,
        FailQuest,
        SetFlag,
        ClearFlag,
        UnlockDialogue,
        TeleportPlayer,
        SpawnEnemy,
        PlayAnimation,
        PlaySound,
        ChangeWeather,
        SetTime
    }

    public enum EventTiming
    {
        OnStart,
        OnEnd,
        Delayed
    }

    public enum EventType
    {
        PlayAnimation,
        PlaySound,
        PlayMusic,
        SpawnObject,
        DestroyObject,
        MoveCamera,
        ShakeCamera,
        FadeScreen,
        ShowUI,
        HideUI,
        TriggerCutscene,
        SetVariable,
        CallFunction
    }

    public enum RewardType
    {
        Item,
        Currency,
        XP,
        Reputation,
        Quest,
        Recipe,
        Skill
    }
}
