using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace ZombieGame
{
    /// <summary>
    /// Loading Screen Tips System - Engaging loading experiences
    /// Features: Rotating tips, gameplay hints, lore snippets, progress tracking
    /// Turns waiting time into learning opportunity
    /// </summary>
    public class LoadingScreenTipsSystem : MonoBehaviour
    {
        public static LoadingScreenTipsSystem Instance { get; private set; }

        [Header("Tip Settings")]
        [SerializeField] private float tipRotationInterval = 5f;
        [SerializeField] private bool randomizeTips = true;
        [SerializeField] private bool showProgressBar = true;
        [SerializeField] private bool showLoadingFacts = true;

        private List<LoadingTip> allTips = new List<LoadingTip>();
        private List<LoadingTip> displayedTips = new List<LoadingTip>();
        private LoadingTip currentTip;
        private float lastTipChange = 0f;

        // Events
        public event System.Action<LoadingTip> OnTipChanged;
        public event System.Action<float> OnLoadingProgress;

        [System.Serializable]
        public class LoadingTip
        {
            public string tipId;
            public TipCategory category;
            public string title;
            public string content;
            public int minPlayerLevel;
            public bool isUnlocked = true;
        }

        public enum TipCategory
        {
            Gameplay,
            Combat,
            Survival,
            Multiplayer,
            Strategy,
            Lore,
            Controls,
            Advanced,
            Fun
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeTips();
            }
            else { Destroy(gameObject); }
        }

        private void Update()
        {
            if (Time.time - lastTipChange >= tipRotationInterval)
            {
                ShowNextTip();
                lastTipChange = Time.time;
            }
        }

        private void InitializeTips()
        {
            // Gameplay Tips
            AddTip("gameplay_01", TipCategory.Gameplay, "Resource Management", 
                "Always keep spare ammo and medkits. You never know when you'll need them!");
            AddTip("gameplay_02", TipCategory.Gameplay, "Explore Everything", 
                "Hidden loot can be found in unexpected places. Check every corner!");
            AddTip("gameplay_03", TipCategory.Gameplay, "Save Often", 
                "Your progress auto-saves every 5 minutes, but manual saves are recommended before risky situations.");

            // Combat Tips
            AddTip("combat_01", TipCategory.Combat, "Aim for the Head", 
                "Headshots deal 2x damage to zombies. Make every bullet count!");
            AddTip("combat_02", TipCategory.Combat, "Cover is Your Friend", 
                "Use cover to avoid damage and regenerate health safely.");
            AddTip("combat_03", TipCategory.Combat, "Weapon Variety", 
                "Different zombies have different weaknesses. Experiment with weapons!");
            AddTip("combat_04", TipCategory.Combat, "Reload Timing", 
                "Reload during safe moments, not in the middle of combat.");
            AddTip("combat_05", TipCategory.Combat, "Melee Backup", 
                "Your melee weapon never runs out of ammo. Use it when conserving bullets!");

            // Survival Tips
            AddTip("survival_01", TipCategory.Survival, "Health Management", 
                "Don't wait until you're critically injured to heal. Keep your health above 50%.");
            AddTip("survival_02", TipCategory.Survival, "Stamina Conservation", 
                "Sprinting depletes stamina. Walk when safe to conserve energy.");
            AddTip("survival_03", TipCategory.Survival, "Night Survival", 
                "Zombies are more aggressive at night. Find shelter or stay in safe zones.");
            AddTip("survival_04", TipCategory.Survival, "Weather Effects", 
                "Rain reduces visibility but also muffles your footsteps. Use it tactically!");

            // Multiplayer Tips
            AddTip("multiplayer_01", TipCategory.Multiplayer, "Teamwork Wins", 
                "Stick with your team. Solo players rarely survive long.");
            AddTip("multiplayer_02", TipCategory.Multiplayer, "Communication", 
                "Use voice chat or pings to coordinate with teammates.");
            AddTip("multiplayer_03", TipCategory.Multiplayer, "Share Resources", 
                "Drop ammo or medkits for teammates who need them more.");
            AddTip("multiplayer_04", TipCategory.Multiplayer, "Revive Priority", 
                "Reviving downed teammates should be your top priority in safe situations.");

            // Strategy Tips
            AddTip("strategy_01", TipCategory.Strategy, "High Ground Advantage", 
                "Elevated positions give you better sightlines and make you harder to reach.");
            AddTip("strategy_02", TipCategory.Strategy, "Choke Points", 
                "Funnel zombies through narrow passages for easier crowd control.");
            AddTip("strategy_03", TipCategory.Strategy, "Noise Management", 
                "Gunfire attracts zombies. Use silenced weapons or melee in stealth situations.");
            AddTip("strategy_04", TipCategory.Strategy, "Resource Routes", 
                "Learn the locations of loot spawns for efficient looting runs.");

            // Controls Tips
            AddTip("controls_01", TipCategory.Controls, "Quick Weapon Switch", 
                "Use number keys (1-3) to instantly switch weapons instead of scrolling.");
            AddTip("controls_02", TipCategory.Controls, "Quick Heal", 
                "Press H to instantly use a medkit without opening your inventory.");
            AddTip("controls_03", TipCategory.Controls, "Sprint Toggle", 
                "Enable sprint toggle in settings for continuous running without holding Shift.");
            AddTip("controls_04", TipCategory.Controls, "Custom Keybinds", 
                "Customize your controls in Settings > Input for maximum comfort.");

            // Advanced Tips
            AddTip("advanced_01", TipCategory.Advanced, "Animation Canceling", 
                "Switch weapons to cancel long reload animations in emergencies.", 5);
            AddTip("advanced_02", TipCategory.Advanced, "Sound Cues", 
                "Listen for audio cues. Different zombie types make different sounds.", 5);
            AddTip("advanced_03", TipCategory.Advanced, "Movement Tech", 
                "Jump while sprinting to maintain momentum over small obstacles.", 10);
            AddTip("advanced_04", TipCategory.Advanced, "Loadout Synergy", 
                "Build your loadout around a specific playstyle for maximum effectiveness.", 10);

            // Lore Tips
            AddTip("lore_01", TipCategory.Lore, "The Outbreak", 
                "The zombie outbreak started in a secret military facility. Documents reveal the truth...");
            AddTip("lore_02", TipCategory.Lore, "Safe Zones", 
                "Safe zones are remnants of early evacuation efforts. Not all survivors made it.");
            AddTip("lore_03", TipCategory.Lore, "Boss Origins", 
                "Boss zombies were exposed to concentrated infection. They retain some intelligence.");

            // Fun Facts
            AddTip("fun_01", TipCategory.Fun, "Development Stat", 
                "This game has 160+ complete systems - a world record in gaming!");
            AddTip("fun_02", TipCategory.Fun, "Easter Eggs", 
                "There are 25 hidden easter eggs scattered across all maps. Can you find them?");
            AddTip("fun_03", TipCategory.Fun, "Community Record", 
                "The longest survival run by a player is 8 hours, 23 minutes!");

            Debug.Log($"[LoadingTips] Initialized {allTips.Count} loading tips");
        }

        private void AddTip(string tipId, TipCategory category, string title, string content, int minLevel = 0)
        {
            var tip = new LoadingTip
            {
                tipId = tipId,
                category = category,
                title = title,
                content = content,
                minPlayerLevel = minLevel,
                isUnlocked = true
            };

            allTips.Add(tip);
        }

        public void ShowNextTip()
        {
            var availableTips = allTips.Where(t => t.isUnlocked && !displayedTips.Contains(t)).ToList();

            if (availableTips.Count == 0)
            {
                displayedTips.Clear();
                availableTips = allTips.Where(t => t.isUnlocked).ToList();
            }

            if (availableTips.Count == 0) return;

            currentTip = randomizeTips 
                ? availableTips[Random.Range(0, availableTips.Count)]
                : availableTips[0];

            displayedTips.Add(currentTip);
            OnTipChanged?.Invoke(currentTip);

            Debug.Log($"[LoadingTips] [{currentTip.category}] {currentTip.title}: {currentTip.content}");
        }

        public void ShowRandomTip()
        {
            var availableTips = allTips.Where(t => t.isUnlocked).ToList();
            if (availableTips.Count == 0) return;

            currentTip = availableTips[Random.Range(0, availableTips.Count)];
            OnTipChanged?.Invoke(currentTip);
        }

        public void ShowTipByCategory(TipCategory category)
        {
            var categoryTips = allTips.Where(t => t.category == category && t.isUnlocked).ToList();
            if (categoryTips.Count == 0) return;

            currentTip = categoryTips[Random.Range(0, categoryTips.Count)];
            OnTipChanged?.Invoke(currentTip);
        }

        public void UpdateLoadingProgress(float progress)
        {
            if (!showProgressBar) return;

            OnLoadingProgress?.Invoke(Mathf.Clamp01(progress));
        }

        public LoadingTip GetCurrentTip()
        {
            return currentTip;
        }

        public List<LoadingTip> GetTipsByCategory(TipCategory category)
        {
            return allTips.Where(t => t.category == category).ToList();
        }

        public List<LoadingTip> GetAllTips()
        {
            return new List<LoadingTip>(allTips);
        }

        public void UnlockTip(string tipId)
        {
            var tip = allTips.Find(t => t.tipId == tipId);
            if (tip != null)
            {
                tip.isUnlocked = true;
                Debug.Log($"[LoadingTips] Unlocked tip: {tip.title}");
            }
        }

        public void SetTipRotationInterval(float interval)
        {
            tipRotationInterval = Mathf.Clamp(interval, 2f, 30f);
            PlayerPrefs.SetFloat("LoadingTips_RotationInterval", tipRotationInterval);
            PlayerPrefs.Save();
        }

        public void SetRandomizeTips(bool randomize)
        {
            randomizeTips = randomize;
            PlayerPrefs.SetInt("LoadingTips_Randomize", randomize ? 1 : 0);
            PlayerPrefs.Save();
        }

        public int GetTipCount()
        {
            return allTips.Count;
        }

        public int GetUnlockedTipCount()
        {
            return allTips.Count(t => t.isUnlocked);
        }

        public void ResetDisplayedTips()
        {
            displayedTips.Clear();
        }
    }
}
