using Unity.Netcode;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace ZombieGame
{
    public class LiveEventsSystem : NetworkBehaviour
    {
        public static LiveEventsSystem Instance { get; private set; }
        
        private Dictionary<string, LiveEvent> activeEvents = new Dictionary<string, LiveEvent>();
        
        [System.Serializable]
        public class LiveEvent
        {
            public string eventId;
            public string eventName;
            public EventType type;
            public DateTime startTime;
            public DateTime endTime;
            public Vector3 location;
            public Dictionary<string, object> rewards = new Dictionary<string, object>();
            public int participantCount = 0;
        }
        
        public enum EventType
        {
            BossInvasion,    // World boss appears
            Meteor,          // Meteor shower with loot
            AirDrop,         // Supply drop
            HordeWave,       // Massive zombie wave
            DoubleLoot,      // 2x loot for duration
            DoubleXP         // 2x XP for duration
        }
        
        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }
        
        public void StartLiveEvent(string eventName, EventType type, int durationMinutes)
        {
            string eventId = Guid.NewGuid().ToString();
            
            var liveEvent = new LiveEvent
            {
                eventId = eventId,
                eventName = eventName,
                type = type,
                startTime = DateTime.UtcNow,
                endTime = DateTime.UtcNow.AddMinutes(durationMinutes),
                location = Vector3.zero
            };
            
            activeEvents[eventId] = liveEvent;
            Debug.Log($"[LiveEvents] Started: {eventName} ({type}) for {durationMinutes} minutes");
        }
        
        public List<LiveEvent> GetActiveEvents()
        {
            return new List<LiveEvent>(activeEvents.Values);
        }
    }
}
