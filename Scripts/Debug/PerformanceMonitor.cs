using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZombieGame.Debug
{
    /// <summary>
    /// Performance monitoring tool for tracking FPS, memory, and network stats.
    /// Press F5 to toggle performance overlay.
    /// </summary>
    public class PerformanceMonitor : MonoBehaviour
    {
        public static PerformanceMonitor Instance { get; private set; }

        [SerializeField] private bool showPerformanceOverlay = false;
        [SerializeField] private KeyCode toggleKey = KeyCode.F5;
        [SerializeField] private int fpsHistorySize = 60;

        private Queue<float> fpsHistory = new Queue<float>();
        private float deltaTime = 0f;
        private float updateInterval = 0.5f;
        private float timeSinceUpdate = 0f;
        
        private int currentFPS = 0;
        private int avgFPS = 0;
        private int minFPS = int.MaxValue;
        private int maxFPS = 0;
        
        private float usedMemoryMB = 0f;
        private float allocatedMemoryMB = 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                showPerformanceOverlay = !showPerformanceOverlay;
            }

            UpdatePerformanceStats();
        }

        private void UpdatePerformanceStats()
        {
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
            timeSinceUpdate += Time.unscaledDeltaTime;

            if (timeSinceUpdate >= updateInterval)
            {
                currentFPS = (int)(1f / deltaTime);
                
                fpsHistory.Enqueue(currentFPS);
                if (fpsHistory.Count > fpsHistorySize)
                {
                    fpsHistory.Dequeue();
                }

                CalculateFPSStats();
                UpdateMemoryStats();

                timeSinceUpdate = 0f;
            }
        }

        private void CalculateFPSStats()
        {
            int sum = 0;
            minFPS = int.MaxValue;
            maxFPS = 0;

            foreach (int fps in fpsHistory)
            {
                sum += fps;
                if (fps < minFPS) minFPS = fps;
                if (fps > maxFPS) maxFPS = fps;
            }

            avgFPS = fpsHistory.Count > 0 ? sum / fpsHistory.Count : 0;
        }

        private void UpdateMemoryStats()
        {
            usedMemoryMB = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / 1048576f;
            allocatedMemoryMB = UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong() / 1048576f;
        }

        private void OnGUI()
        {
            if (!showPerformanceOverlay) return;

            int w = Screen.width, h = Screen.height;
            GUIStyle style = new GUIStyle();
            style.alignment = TextAnchor.UpperRight;
            style.fontSize = h * 2 / 50;
            style.normal.textColor = GetFPSColor();

            Rect rect = new Rect(w - 250, 10, 240, h * 2 / 50);

            string text = $"FPS: {currentFPS}\n";
            text += $"Avg: {avgFPS} | Min: {minFPS} | Max: {maxFPS}\n";
            text += $"Frame: {(deltaTime * 1000f):F1}ms\n";
            text += $"Memory: {usedMemoryMB:F1}MB / {allocatedMemoryMB:F1}MB\n";
            
            if (Unity.Netcode.NetworkManager.Singleton != null)
            {
                text += $"Network: {(Unity.Netcode.NetworkManager.Singleton.IsConnectedClient ? "Connected" : "Disconnected")}\n";
                text += $"Clients: {Unity.Netcode.NetworkManager.Singleton.ConnectedClients.Count}";
            }

            GUI.Label(rect, text, style);
        }

        private Color GetFPSColor()
        {
            if (currentFPS >= 60) return Color.green;
            if (currentFPS >= 30) return Color.yellow;
            return Color.red;
        }

        public void LogPerformanceMetric(string metricName, float value)
        {
            UnityEngine.Debug.Log($"[PERF] {metricName}: {value:F2}");
        }
    }
}
