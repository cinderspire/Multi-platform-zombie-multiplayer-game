using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

namespace ZombieGame
{
    public class TelemetrySystem : NetworkBehaviour
    {
        public static TelemetrySystem Instance { get; private set; }
        
        private Dictionary<string, List<TelemetryEvent>> events = new Dictionary<string, List<TelemetryEvent>>();
        
        [System.Serializable]
        public class TelemetryEvent
        {
            public string eventName;
            public Dictionary<string, object> properties;
            public System.DateTime timestamp;
        }
        
        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }
        
        public void TrackEvent(string eventName, Dictionary<string, object> properties = null)
        {
            var telemetryEvent = new TelemetryEvent
            {
                eventName = eventName,
                properties = properties ?? new Dictionary<string, object>(),
                timestamp = System.DateTime.UtcNow
            };
            
            if (!events.ContainsKey(eventName))
            {
                events[eventName] = new List<TelemetryEvent>();
            }
            events[eventName].Add(telemetryEvent);
        }
        
        public Dictionary<string, int> GetEventCounts()
        {
            var counts = new Dictionary<string, int>();
            foreach (var kvp in events)
            {
                counts[kvp.Key] = kvp.Value.Count;
            }
            return counts;
        }
    }
}
