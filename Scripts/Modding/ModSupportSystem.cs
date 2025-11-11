using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace ZombieGame
{
    public class ModSupportSystem : MonoBehaviour
    {
        public static ModSupportSystem Instance { get; private set; }
        
        [SerializeField] private string modsDirectory = "Mods";
        
        private Dictionary<string, LoadedMod> loadedMods = new Dictionary<string, LoadedMod>();
        
        [System.Serializable]
        public class LoadedMod
        {
            public string modId;
            public string modName;
            public string version;
            public string author;
            public bool isEnabled = true;
            public ModType type;
        }
        
        public enum ModType
        {
            Gameplay,
            Visual,
            Audio,
            Map,
            Weapon
        }
        
        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
            LoadMods();
        }
        
        private void LoadMods()
        {
            string modPath = Path.Combine(Application.dataPath, modsDirectory);
            if (!Directory.Exists(modPath))
            {
                Directory.CreateDirectory(modPath);
            }
            
            Debug.Log($"[Mods] Mod support initialized. Place mods in: {modPath}");
        }
        
        public void EnableMod(string modId, bool enabled)
        {
            if (loadedMods.ContainsKey(modId))
            {
                loadedMods[modId].isEnabled = enabled;
                Debug.Log($"[Mods] Mod {modId} {(enabled ? "enabled" : "disabled")}");
            }
        }
        
        public List<LoadedMod> GetLoadedMods()
        {
            return new List<LoadedMod>(loadedMods.Values);
        }
    }
}
