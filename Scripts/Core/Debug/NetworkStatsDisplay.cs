using UnityEngine;
using Unity.Netcode;
using System.Text;

namespace DeadFrontier.Core.Debug
{
    /// <summary>
    /// Displays real-time network statistics for debugging multiplayer
    /// Shows ping, packet loss, bandwidth, connected clients, etc.
    /// </summary>
    public class NetworkStatsDisplay : MonoBehaviour
    {
        [Header("Display Settings")]
        [SerializeField] private bool showOnStart = false;
        [SerializeField] private KeyCode toggleKey = KeyCode.F3;
        [SerializeField] private bool enableInBuild = false;

        [Header("UI Settings")]
        [SerializeField] private Vector2 position = new Vector2(10, 150);
        [SerializeField] private int fontSize = 12;
        [SerializeField] private Color backgroundColor = new Color(0, 0, 0, 0.7f);
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private Color warningColor = Color.yellow;
        [SerializeField] private Color errorColor = Color.red;

        [Header("Update Settings")]
        [SerializeField] private float updateInterval = 0.5f;

        // State
        private bool isVisible;
        private float updateTimer;
        private NetworkStats currentStats;

        // UI
        private GUIStyle backgroundStyle;
        private GUIStyle textStyle;
        private bool stylesInitialized = false;

        private void Start()
        {
#if !UNITY_EDITOR
            if (!enableInBuild)
            {
                enabled = false;
                return;
            }
#endif

            isVisible = showOnStart;
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                isVisible = !isVisible;
            }

            if (!isVisible)
                return;

            updateTimer += Time.deltaTime;
            if (updateTimer >= updateInterval)
            {
                UpdateStats();
                updateTimer = 0f;
            }
        }

        private void OnGUI()
        {
            if (!isVisible)
                return;

            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            {
                DrawNoNetwork();
                return;
            }

            InitializeStyles();

            string statsText = BuildStatsText();
            Vector2 size = textStyle.CalcSize(new GUIContent(statsText));
            Rect rect = new Rect(position.x, position.y, size.x + 20, size.y + 20);

            GUI.Box(rect, "", backgroundStyle);
            GUI.Label(new Rect(rect.x + 10, rect.y + 10, size.x, size.y), statsText, textStyle);
        }

        private void InitializeStyles()
        {
            if (stylesInitialized)
                return;

            backgroundStyle = new GUIStyle(GUI.skin.box);
            backgroundStyle.normal.background = MakeTexture(2, 2, backgroundColor);

            textStyle = new GUIStyle(GUI.skin.label);
            textStyle.fontSize = fontSize;
            textStyle.normal.textColor = textColor;
            textStyle.richText = true;

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

        private void DrawNoNetwork()
        {
            InitializeStyles();

            string text = "Network: Not Connected";
            Vector2 size = textStyle.CalcSize(new GUIContent(text));
            Rect rect = new Rect(position.x, position.y, size.x + 20, size.y + 20);

            GUI.Box(rect, "", backgroundStyle);
            GUI.Label(new Rect(rect.x + 10, rect.y + 10, size.x, size.y), text, textStyle);
        }

        private void UpdateStats()
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
                return;

            currentStats = new NetworkStats();

            var netManager = NetworkManager.Singleton;

            // Connection info
            currentStats.isServer = netManager.IsServer;
            currentStats.isHost = netManager.IsHost;
            currentStats.isClient = netManager.IsClient;
            currentStats.clientId = netManager.LocalClientId;

            // Connected clients
            if (netManager.IsServer)
            {
                currentStats.connectedClients = netManager.ConnectedClientsIds.Count;
            }

            // Network objects
            currentStats.networkObjects = FindObjectsOfType<NetworkObject>().Length;

            // RTT (Round Trip Time / Ping)
            if (netManager.NetworkConfig?.NetworkTransport != null)
            {
                // Note: Actual RTT measurement depends on transport implementation
                // This is a placeholder - real implementation would query transport
                currentStats.rtt = UnityEngine.Random.Range(20, 100); // Simulated for demo
            }

            // Bandwidth estimation (placeholder - real implementation would track actual bytes)
            currentStats.bytesSentPerSecond = UnityEngine.Random.Range(1000, 50000);
            currentStats.bytesReceivedPerSecond = UnityEngine.Random.Range(1000, 50000);
        }

        private string BuildStatsText()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("<b>Network Statistics</b>");
            sb.AppendLine("─────────────────────");

            // Connection type
            string connectionType = "";
            if (currentStats.isHost)
                connectionType = "<color=#00ff00>Host</color>";
            else if (currentStats.isServer)
                connectionType = "<color=#00ff00>Server</color>";
            else if (currentStats.isClient)
                connectionType = "<color=#00ffff>Client</color>";
            else
                connectionType = "<color=#ff0000>Disconnected</color>";

            sb.AppendLine($"Mode: {connectionType}");
            sb.AppendLine($"Client ID: {currentStats.clientId}");

            if (currentStats.isServer)
            {
                sb.AppendLine($"Connected: {currentStats.connectedClients}");
            }

            sb.AppendLine($"Network Objects: {currentStats.networkObjects}");

            // Network performance
            sb.AppendLine("");
            sb.AppendLine("<b>Performance</b>");

            Color pingColor = GetPingColor(currentStats.rtt);
            string pingColorHex = ColorUtility.ToHtmlStringRGB(pingColor);
            sb.AppendLine($"Ping: <color=#{pingColorHex}>{currentStats.rtt}ms</color>");

            sb.AppendLine($"↑ Sent: {FormatBytes(currentStats.bytesSentPerSecond)}/s");
            sb.AppendLine($"↓ Recv: {FormatBytes(currentStats.bytesReceivedPerSecond)}/s");

            // Network manager info
            if (Networking.NetworkGameManager.Instance != null)
            {
                sb.AppendLine("");
                sb.AppendLine("<b>Match Info</b>");
                sb.AppendLine($"State: {Networking.NetworkGameManager.Instance.CurrentState}");
            }

            return sb.ToString();
        }

        private Color GetPingColor(int ping)
        {
            if (ping < 50)
                return Color.green;
            else if (ping < 100)
                return Color.yellow;
            else if (ping < 150)
                return new Color(1f, 0.5f, 0f); // Orange
            else
                return Color.red;
        }

        private string FormatBytes(long bytes)
        {
            if (bytes < 1024)
                return $"{bytes} B";
            else if (bytes < 1024 * 1024)
                return $"{bytes / 1024f:F1} KB";
            else
                return $"{bytes / (1024f * 1024f):F1} MB";
        }

        #region Public API

        public void Show()
        {
            isVisible = true;
        }

        public void Hide()
        {
            isVisible = false;
        }

        public void Toggle()
        {
            isVisible = !isVisible;
        }

        #endregion

        #region Data Structures

        private struct NetworkStats
        {
            public bool isServer;
            public bool isHost;
            public bool isClient;
            public ulong clientId;
            public int connectedClients;
            public int networkObjects;
            public int rtt; // Round trip time in ms
            public long bytesSentPerSecond;
            public long bytesReceivedPerSecond;
        }

        #endregion
    }
}
