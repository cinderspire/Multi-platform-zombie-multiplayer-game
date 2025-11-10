using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Debug
{
    /// <summary>
    /// Debug and developer tools for testing and debugging the game.
    /// Press F1 to toggle debug UI.
    /// </summary>
    public class DebugManager : MonoBehaviour
    {
        public static DebugManager Instance { get; private set; }

        [Header("Debug Settings")]
        [SerializeField] private bool debugEnabled = true;
        [SerializeField] private KeyCode toggleKey = KeyCode.F1;
        [SerializeField] private KeyCode godModeKey = KeyCode.F2;
        [SerializeField] private KeyCode spawnZombieKey = KeyCode.F3;
        [SerializeField] private KeyCode killAllZombiesKey = KeyCode.F4;

        private bool showDebugUI = false;
        private bool godModeEnabled = false;
        private Vector2 scrollPosition;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (!debugEnabled) return;

            if (Input.GetKeyDown(toggleKey))
            {
                showDebugUI = !showDebugUI;
            }

            if (Input.GetKeyDown(godModeKey))
            {
                ToggleGodMode();
            }

            if (Input.GetKeyDown(spawnZombieKey))
            {
                SpawnTestZombie();
            }

            if (Input.GetKeyDown(killAllZombiesKey))
            {
                KillAllZombies();
            }
        }

        private void OnGUI()
        {
            if (!debugEnabled || !showDebugUI) return;

            GUILayout.BeginArea(new Rect(10, 10, 400, Screen.height - 20));
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(400), GUILayout.Height(Screen.height - 20));

            GUILayout.Label("=== DEBUG MENU ===", GUI.skin.box);
            GUILayout.Space(10);

            DrawGameInfo();
            GUILayout.Space(10);

            DrawPlayerInfo();
            GUILayout.Space(10);

            DrawNetworkInfo();
            GUILayout.Space(10);

            DrawSystemInfo();
            GUILayout.Space(10);

            DrawDebugActions();

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawGameInfo()
        {
            GUILayout.Label("--- GAME INFO ---", GUI.skin.box);
            
            if (Core.GameManager.Instance != null)
            {
                GUILayout.Label($"State: {Core.GameManager.Instance.CurrentState}");
                GUILayout.Label($"Game Time: {Core.GameManager.Instance.GameTime:F1}s");
            }

            GUILayout.Label($"FPS: {(int)(1f / Time.unscaledDeltaTime)}");
            GUILayout.Label($"Time Scale: {Time.timeScale}");
        }

        private void DrawPlayerInfo()
        {
            GUILayout.Label("--- PLAYER INFO ---", GUI.skin.box);

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
            {
                ulong localId = NetworkManager.Singleton.LocalClientId;
                GUILayout.Label($"Client ID: {localId}");

                var health = Health.HealthSystem.Instance?.GetEntityHealth(localId);
                if (health != null)
                {
                    GUILayout.Label($"Health: {health.currentHealth:F0}/{health.maxHealth:F0}");
                    GUILayout.Label($"Armor: {health.currentArmor:F0}/{health.maxArmor:F0}");
                    GUILayout.Label($"Alive: {health.isAlive}");
                }

                var playerObj = NetworkManager.Singleton.LocalClient?.PlayerObject;
                if (playerObj != null)
                {
                    var controller = playerObj.GetComponent<Player.PlayerController>();
                    if (controller != null)
                    {
                        GUILayout.Label($"State: {controller.CurrentMovementState}");
                        GUILayout.Label($"Speed: {controller.CurrentSpeed:F1}");
                        GUILayout.Label($"Grounded: {controller.IsGrounded}");
                    }
                }

                GUILayout.Label($"God Mode: {(godModeEnabled ? "ON" : "OFF")}");
            }
            else
            {
                GUILayout.Label("Not connected to network");
            }
        }

        private void DrawNetworkInfo()
        {
            GUILayout.Label("--- NETWORK INFO ---", GUI.skin.box);

            if (NetworkManager.Singleton != null)
            {
                GUILayout.Label($"Is Server: {NetworkManager.Singleton.IsServer}");
                GUILayout.Label($"Is Client: {NetworkManager.Singleton.IsClient}");
                GUILayout.Label($"Is Host: {NetworkManager.Singleton.IsHost}");
                GUILayout.Label($"Connected Clients: {NetworkManager.Singleton.ConnectedClients.Count}");
            }
            else
            {
                GUILayout.Label("NetworkManager not found");
            }
        }

        private void DrawSystemInfo()
        {
            GUILayout.Label("--- SYSTEM STATUS ---", GUI.skin.box);

            GUILayout.Label($"HealthSystem: {(Health.HealthSystem.Instance != null ? "✓" : "✗")}");
            GUILayout.Label($"SpawnerSystem: {(AI.SpawnerSystem.Instance != null ? "✓" : "✗")}");
            GUILayout.Label($"GameManager: {(Core.GameManager.Instance != null ? "✓" : "✗")}");

            if (AI.SpawnerSystem.Instance != null)
            {
                GUILayout.Label($"Current Wave: {AI.SpawnerSystem.Instance.CurrentWave}");
                GUILayout.Label($"Zombies Alive: {AI.SpawnerSystem.Instance.AliveZombieCount}");
            }
        }

        private void DrawDebugActions()
        {
            GUILayout.Label("--- DEBUG ACTIONS ---", GUI.skin.box);

            if (GUILayout.Button($"God Mode (F2): {(godModeEnabled ? "ON" : "OFF")}"))
            {
                ToggleGodMode();
            }

            if (GUILayout.Button("Heal to Full"))
            {
                HealPlayer();
            }

            if (GUILayout.Button("Spawn Zombie (F3)"))
            {
                SpawnTestZombie();
            }

            if (GUILayout.Button("Kill All Zombies (F4)"))
            {
                KillAllZombies();
            }

            if (GUILayout.Button("Complete Current Objective"))
            {
                CompleteObjective();
            }

            if (GUILayout.Button("Trigger Victory"))
            {
                Core.GameManager.Instance?.TriggerVictory("Debug victory");
            }

            if (GUILayout.Button("Trigger Defeat"))
            {
                Core.GameManager.Instance?.TriggerDefeat("Debug defeat");
            }

            if (GUILayout.Button("Give 1000 Currency"))
            {
                GiveDebugCurrency();
            }
        }

        private void ToggleGodMode()
        {
            godModeEnabled = !godModeEnabled;
            
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
            {
                ulong localId = NetworkManager.Singleton.LocalClientId;
                Health.HealthSystem.Instance?.SetInvulnerableServerRpc(localId, godModeEnabled, 0f);
            }

            UnityEngine.Debug.Log($"God Mode: {(godModeEnabled ? "ON" : "OFF")}");
        }

        private void HealPlayer()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
            {
                ulong localId = NetworkManager.Singleton.LocalClientId;
                Health.HealthSystem.Instance?.HealEntityServerRpc(localId, 9999f, Health.HealType.Instant);
                Health.HealthSystem.Instance?.RestoreArmorServerRpc(localId, 9999f);
            }
        }

        private void SpawnTestZombie()
        {
            // Would spawn a test zombie near player
            UnityEngine.Debug.Log("Spawn zombie functionality would be implemented here");
        }

        private void KillAllZombies()
        {
            // Would kill all active zombies
            UnityEngine.Debug.Log("Kill all zombies functionality would be implemented here");
        }

        private void CompleteObjective()
        {
            if (Map.ObjectiveSystem.Instance != null)
            {
                var currentObj = Map.ObjectiveSystem.Instance.GetCurrentObjective();
                if (currentObj != null)
                {
                    Map.ObjectiveSystem.Instance.UpdateObjectiveProgressServerRpc(0, 1f);
                }
            }
        }

        private void GiveDebugCurrency()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
            {
                ulong localId = NetworkManager.Singleton.LocalClientId;
                Economy.EconomyManager.Instance?.AddCurrencyServerRpc(localId, "Gold", 1000);
            }
        }

        public void LogDebug(string message)
        {
            if (debugEnabled)
            {
                UnityEngine.Debug.Log($"[DEBUG] {message}");
            }
        }

        public void LogWarning(string message)
        {
            if (debugEnabled)
            {
                UnityEngine.Debug.LogWarning($"[DEBUG] {message}");
            }
        }

        public void LogError(string message)
        {
            if (debugEnabled)
            {
                UnityEngine.Debug.LogError($"[DEBUG] {message}");
            }
        }
    }
}
