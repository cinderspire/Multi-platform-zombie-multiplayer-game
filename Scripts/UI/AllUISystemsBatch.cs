using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace ZombieGame.UI
{
    // SHOP UI SYSTEM
    public class ShopUISystem : MonoBehaviour
    {
        public static ShopUISystem Instance { get; private set; }
        
        [SerializeField] private GameObject shopPanel;
        [SerializeField] private Transform itemGrid;
        
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void OpenShop(string shopId)
        {
            if (shopPanel != null) shopPanel.SetActive(true);
        }

        public void CloseShop()
        {
            if (shopPanel != null) shopPanel.SetActive(false);
        }
    }

    // CRAFTING UI SYSTEM
    public class CraftingUISystem : MonoBehaviour
    {
        public static CraftingUISystem Instance { get; private set; }
        
        [SerializeField] private GameObject craftingPanel;
        [SerializeField] private Transform recipeList;
        
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void OpenCrafting()
        {
            if (craftingPanel != null) craftingPanel.SetActive(true);
        }

        public void CloseCrafting()
        {
            if (craftingPanel != null) craftingPanel.SetActive(false);
        }
    }

    // CHARACTER CUSTOMIZATION SYSTEM
    public class CharacterCustomizationUI : MonoBehaviour
    {
        public static CharacterCustomizationUI Instance { get; private set; }
        
        [SerializeField] private GameObject customizationPanel;
        
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void OpenCustomization()
        {
            if (customizationPanel != null) customizationPanel.SetActive(true);
        }
    }

    // PLAYER STATS UI SYSTEM
    public class StatsUISystem : MonoBehaviour
    {
        public static StatsUISystem Instance { get; private set; }
        
        [SerializeField] private GameObject statsPanel;
        [SerializeField] private TextMeshProUGUI killsText;
        [SerializeField] private TextMeshProUGUI deathsText;
        [SerializeField] private TextMeshProUGUI winsText;
        
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void UpdateStats(int kills, int deaths, int wins)
        {
            if (killsText != null) killsText.text = $"Kills: {kills}";
            if (deathsText != null) deathsText.text = $"Deaths: {deaths}";
            if (winsText != null) winsText.text = $"Wins: {wins}";
        }
    }

    // CLAN UI SYSTEM
    public class ClanUISystem : MonoBehaviour
    {
        public static ClanUISystem Instance { get; private set; }
        
        [SerializeField] private GameObject clanPanel;
        
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void OpenClanPanel()
        {
            if (clanPanel != null) clanPanel.SetActive(true);
        }
    }

    // PARTY UI SYSTEM
    public class PartyUISystem : MonoBehaviour
    {
        public static PartyUISystem Instance { get; private set; }
        
        [SerializeField] private GameObject partyPanel;
        [SerializeField] private Transform memberList;
        
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void OpenPartyPanel()
        {
            if (partyPanel != null) partyPanel.SetActive(true);
        }
    }

    // ACHIEVEMENTS UI SYSTEM
    public class AchievementsUISystem : MonoBehaviour
    {
        public static AchievementsUISystem Instance { get; private set; }
        
        [SerializeField] private GameObject achievementsPanel;
        [SerializeField] private Transform achievementGrid;
        
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void OpenAchievements()
        {
            if (achievementsPanel != null) achievementsPanel.SetActive(true);
        }
    }
}
