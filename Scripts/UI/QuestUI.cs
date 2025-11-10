using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Netcode;

namespace ZombieGame.UI
{
    public class QuestUI : MonoBehaviour
    {
        public static QuestUI Instance { get; private set; }

        [SerializeField] private Transform questListParent;
        [SerializeField] private GameObject questEntryPrefab;

        private List<GameObject> questEntries = new List<GameObject>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            RefreshQuestList();
        }

        public void RefreshQuestList()
        {
            ClearQuestList();

            var questSystem = Quests.QuestSystem.Instance;
            if (questSystem != null)
            {
                ulong playerId = NetworkManager.Singleton.LocalClientId;
                // Would populate active quests from QuestSystem
            }
        }

        private void ClearQuestList()
        {
            foreach (var entry in questEntries)
            {
                Destroy(entry);
            }
            questEntries.Clear();
        }
    }
}
