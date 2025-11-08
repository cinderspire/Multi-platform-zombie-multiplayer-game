using UnityEngine;
using System.Collections.Generic;
using System.Text;

namespace DeadFrontier.Core.Debug
{
    /// <summary>
    /// In-game debug console for development and QA testing
    /// Provides commands to test systems, spawn entities, modify stats, etc.
    /// </summary>
    public class DebugConsole : Singleton<DebugConsole>
    {
        [Header("Console Settings")]
        [SerializeField] private KeyCode toggleKey = KeyCode.BackQuote; // ~
        [SerializeField] private bool enableInBuild = false;
        [SerializeField] private int maxLogEntries = 100;

        [Header("UI Settings")]
        [SerializeField] private int fontSize = 14;
        [SerializeField] private Color backgroundColor = new Color(0, 0, 0, 0.8f);
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private Color errorColor = Color.red;
        [SerializeField] private Color warningColor = Color.yellow;

        // Console state
        private bool isVisible = false;
        private string inputText = "";
        private List<LogEntry> logEntries = new List<LogEntry>();
        private Vector2 scrollPosition = Vector2.zero;
        private Dictionary<string, ConsoleCommand> commands = new Dictionary<string, ConsoleCommand>();

        // UI
        private GUIStyle consoleStyle;
        private GUIStyle inputStyle;
        private GUIStyle buttonStyle;
        private Rect consoleRect;
        private bool stylesInitialized = false;

        protected override void Awake()
        {
            base.Awake();

#if !UNITY_EDITOR
            if (!enableInBuild)
            {
                enabled = false;
                return;
            }
#endif

            RegisterDefaultCommands();
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                ToggleConsole();
            }
        }

        #region GUI

        private void OnGUI()
        {
            if (!isVisible)
                return;

            InitializeStyles();

            float width = Screen.width * 0.8f;
            float height = Screen.height * 0.6f;
            consoleRect = new Rect(
                Screen.width * 0.1f,
                Screen.height * 0.1f,
                width,
                height
            );

            GUILayout.BeginArea(consoleRect, consoleStyle);

            // Title bar
            GUILayout.BeginHorizontal();
            GUILayout.Label("Debug Console", GUILayout.Height(30));
            if (GUILayout.Button("Clear", buttonStyle, GUILayout.Width(60), GUILayout.Height(25)))
            {
                ClearLog();
            }
            if (GUILayout.Button("Close", buttonStyle, GUILayout.Width(60), GUILayout.Height(25)))
            {
                ToggleConsole();
            }
            GUILayout.EndHorizontal();

            // Log area
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(height - 100));

            foreach (var entry in logEntries)
            {
                GUI.color = GetLogColor(entry.type);
                GUILayout.Label($"[{entry.timestamp}] {entry.message}");
                GUI.color = Color.white;
            }

            GUILayout.EndScrollView();

            // Input area
            GUILayout.BeginHorizontal();
            GUI.SetNextControlName("ConsoleInput");
            inputText = GUILayout.TextField(inputText, inputStyle, GUILayout.Height(25));

            if (GUILayout.Button("Execute", buttonStyle, GUILayout.Width(80), GUILayout.Height(25)))
            {
                ExecuteCommand(inputText);
                inputText = "";
                GUI.FocusControl("ConsoleInput");
            }
            GUILayout.EndHorizontal();

            GUILayout.EndArea();

            // Handle Enter key
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
            {
                ExecuteCommand(inputText);
                inputText = "";
                Event.current.Use();
            }
        }

        private void InitializeStyles()
        {
            if (stylesInitialized)
                return;

            consoleStyle = new GUIStyle(GUI.skin.box);
            consoleStyle.normal.background = MakeTexture(2, 2, backgroundColor);
            consoleStyle.padding = new RectOffset(10, 10, 10, 10);

            inputStyle = new GUIStyle(GUI.skin.textField);
            inputStyle.fontSize = fontSize;
            inputStyle.normal.textColor = textColor;

            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = fontSize - 2;

            stylesInitialized = true;
        }

        private Texture2D MakeTexture(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;

            Texture2D texture = new Texture2D(width, height);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private Color GetLogColor(LogType type)
        {
            switch (type)
            {
                case LogType.Error:
                case LogType.Exception:
                    return errorColor;
                case LogType.Warning:
                    return warningColor;
                default:
                    return textColor;
            }
        }

        #endregion

        #region Console Control

        public void ToggleConsole()
        {
            isVisible = !isVisible;

            if (isVisible)
            {
                GUI.FocusControl("ConsoleInput");
            }
        }

        public void ShowConsole()
        {
            isVisible = true;
        }

        public void HideConsole()
        {
            isVisible = false;
        }

        public void Log(string message, LogType type = LogType.Log)
        {
            var entry = new LogEntry
            {
                message = message,
                timestamp = System.DateTime.Now.ToString("HH:mm:ss"),
                type = type
            };

            logEntries.Add(entry);

            // Limit entries
            if (logEntries.Count > maxLogEntries)
            {
                logEntries.RemoveAt(0);
            }

            // Auto-scroll to bottom
            scrollPosition = new Vector2(0, float.MaxValue);
        }

        public void ClearLog()
        {
            logEntries.Clear();
        }

        #endregion

        #region Command System

        public void RegisterCommand(string name, System.Action<string[]> callback, string description = "")
        {
            commands[name.ToLower()] = new ConsoleCommand
            {
                name = name,
                callback = callback,
                description = description
            };
        }

        private void ExecuteCommand(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return;

            Log($"> {input}");

            string[] parts = input.Trim().Split(' ');
            string commandName = parts[0].ToLower();

            if (!commands.ContainsKey(commandName))
            {
                Log($"Unknown command: {commandName}", LogType.Error);
                return;
            }

            try
            {
                string[] args = new string[parts.Length - 1];
                System.Array.Copy(parts, 1, args, 0, args.Length);
                commands[commandName].callback(args);
            }
            catch (System.Exception e)
            {
                Log($"Command error: {e.Message}", LogType.Error);
            }
        }

        #endregion

        #region Default Commands

        private void RegisterDefaultCommands()
        {
            // Help command
            RegisterCommand("help", (args) =>
            {
                Log("Available commands:");
                foreach (var cmd in commands.Values)
                {
                    Log($"  {cmd.name} - {cmd.description}");
                }
            }, "Shows all available commands");

            // Clear command
            RegisterCommand("clear", (args) => ClearLog(), "Clears the console");

            // Quit command
            RegisterCommand("quit", (args) =>
            {
                Log("Quitting application...");
                Application.Quit();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
            }, "Quits the application");

            // FPS command
            RegisterCommand("fps", (args) =>
            {
                if (Performance.PerformanceMonitor.Instance != null)
                {
                    var metrics = Performance.PerformanceMonitor.Instance.GetCurrentMetrics();
                    Log($"FPS: {metrics.currentFPS:F1} (Avg: {metrics.averageFPS:F1}, Min: {metrics.minFPS:F1}, Max: {metrics.maxFPS:F1})");
                    Log($"Frame Time: {metrics.frameTime:F2}ms");
                }
                else
                {
                    Log("PerformanceMonitor not available", LogType.Warning);
                }
            }, "Shows current FPS stats");

            // Memory command
            RegisterCommand("memory", (args) =>
            {
                if (Performance.PerformanceMonitor.Instance != null)
                {
                    var metrics = Performance.PerformanceMonitor.Instance.GetCurrentMetrics();
                    Log($"Memory: {metrics.memoryMB} MB");
                }
                else
                {
                    Log($"Memory: {System.GC.GetTotalMemory(false) / 1048576} MB");
                }
            }, "Shows memory usage");

            // God mode command
            RegisterCommand("god", (args) =>
            {
                var player = GameObject.FindObjectOfType<Player.PlayerHealth>();
                if (player != null)
                {
                    player.SetHealth(float.MaxValue);
                    Log("God mode enabled!");
                }
                else
                {
                    Log("Player not found", LogType.Error);
                }
            }, "Enables god mode");

            // Give XP command
            RegisterCommand("givexp", (args) =>
            {
                if (args.Length < 1)
                {
                    Log("Usage: givexp <amount>", LogType.Warning);
                    return;
                }

                if (int.TryParse(args[0], out int amount))
                {
                    var progression = GameObject.FindObjectOfType<Player.PlayerProgression>();
                    if (progression != null)
                    {
                        progression.AwardXP(amount);
                        Log($"Awarded {amount} XP");
                    }
                    else
                    {
                        Log("PlayerProgression not found", LogType.Error);
                    }
                }
                else
                {
                    Log("Invalid amount", LogType.Error);
                }
            }, "Gives XP to player (usage: givexp 1000)");

            // Spawn zombie command
            RegisterCommand("spawnzombie", (args) =>
            {
                if (Zombies.ZombieManager.Instance != null)
                {
                    Zombies.ZombieType type = Zombies.ZombieType.Walker;

                    if (args.Length > 0)
                    {
                        if (System.Enum.TryParse(args[0], true, out Zombies.ZombieType parsedType))
                        {
                            type = parsedType;
                        }
                    }

                    var player = Camera.main?.transform;
                    Vector3 spawnPos = player != null ? player.position + player.forward * 5f : Vector3.zero;

                    Zombies.ZombieManager.Instance.SpawnZombie(type, spawnPos);
                    Log($"Spawned {type} zombie");
                }
                else
                {
                    Log("ZombieManager not found", LogType.Error);
                }
            }, "Spawns a zombie (usage: spawnzombie [type])");

            // Kill all zombies command
            RegisterCommand("killallzombies", (args) =>
            {
                var zombies = GameObject.FindObjectsOfType<Zombies.ZombieAI>();
                foreach (var zombie in zombies)
                {
                    zombie.TakeDamage(9999f, Vector3.zero, null);
                }
                Log($"Killed {zombies.Length} zombies");
            }, "Kills all zombies in the scene");

            // Teleport command
            RegisterCommand("tp", (args) =>
            {
                if (args.Length < 3)
                {
                    Log("Usage: tp <x> <y> <z>", LogType.Warning);
                    return;
                }

                if (float.TryParse(args[0], out float x) &&
                    float.TryParse(args[1], out float y) &&
                    float.TryParse(args[2], out float z))
                {
                    var player = GameObject.FindObjectOfType<Player.PlayerController>();
                    if (player != null)
                    {
                        player.transform.position = new Vector3(x, y, z);
                        Log($"Teleported to ({x}, {y}, {z})");
                    }
                    else
                    {
                        Log("Player not found", LogType.Error);
                    }
                }
                else
                {
                    Log("Invalid coordinates", LogType.Error);
                }
            }, "Teleports player to coordinates (usage: tp 0 0 0)");

            // Validate systems command
            RegisterCommand("validate", (args) =>
            {
                if (QualityAssurance.SystemValidator.Instance != null)
                {
                    QualityAssurance.SystemValidator.Instance.ValidateAllSystems();
                    Log("System validation started. Check console for results.");
                }
                else
                {
                    Log("SystemValidator not found", LogType.Error);
                }
            }, "Validates all game systems");

            // Time scale command
            RegisterCommand("timescale", (args) =>
            {
                if (args.Length < 1)
                {
                    Log($"Current time scale: {Time.timeScale}");
                    return;
                }

                if (float.TryParse(args[0], out float scale))
                {
                    Time.timeScale = Mathf.Clamp(scale, 0f, 10f);
                    Log($"Time scale set to {Time.timeScale}");
                }
                else
                {
                    Log("Invalid scale value", LogType.Error);
                }
            }, "Sets time scale (usage: timescale 0.5)");
        }

        #endregion

        #region Data Structures

        private struct LogEntry
        {
            public string message;
            public string timestamp;
            public LogType type;
        }

        private class ConsoleCommand
        {
            public string name;
            public System.Action<string[]> callback;
            public string description;
        }

        #endregion
    }
}
