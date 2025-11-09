using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Social
{
    /// <summary>
    /// Comprehensive player mentorship and coaching system for multi-platform zombie multiplayer game.
    /// Pairs experienced players with newcomers for mutual rewards and community building.
    /// </summary>
    public class MentorshipSystem : NetworkBehaviour
    {
        public static MentorshipSystem Instance { get; private set; }

        [Header("Mentorship Settings")]
        [SerializeField] private bool enableMentorship = true;
        [SerializeField] private int minMentorLevel = 20;
        [SerializeField] private int maxStudentLevel = 10;
        [SerializeField] private int maxStudentsPerMentor = 5;
        [SerializeField] private int maxActiveMentorships = 1; // For students

        [Header("Reward Settings")]
        [SerializeField] private bool enableRewards = true;
        [SerializeField] private float xpBonusMultiplier = 1.2f; // 20% bonus
        [SerializeField] private int mentorRewardPerMilestone = 1000;
        [SerializeField] private int studentBonusPerSession = 500;

        [Header("Progression Settings")]
        [SerializeField] private int[] graduationLevels = { 5, 10, 15 };
        [SerializeField] private bool autoGraduate = true;
        [SerializeField] private int minSessionsForGraduation = 10;

        [Header("Matching Settings")]
        [SerializeField] private bool enableAutoMatching = true;
        [SerializeField] private int maxMatchQueueSize = 100;
        [SerializeField] private float matchRefreshInterval = 60f;

        // Enums
        public enum MentorshipStatus
        {
            Pending,
            Active,
            Paused,
            Completed,
            Cancelled
        }

        public enum MilestoneType
        {
            FirstKill,
            FirstExtraction,
            Level5Reached,
            Level10Reached,
            FirstCraft,
            First100Kills,
            FirstBossKill,
            FullyTrained
        }

        public enum SessionRating
        {
            Poor = 1,
            Fair = 2,
            Good = 3,
            Great = 4,
            Excellent = 5
        }

        // Data structures
        [Serializable]
        public class MentorProfile
        {
            public ulong mentorId;
            public string mentorName;
            public int level;
            public int totalStudents;
            public int activeStudents;
            public int graduatedStudents;
            public float averageRating;
            public int totalSessions;
            public List<string> specializations = new List<string>();
            public List<ulong> currentStudents = new List<ulong>();
            public List<MilestoneAchievement> milestones = new List<MilestoneAchievement>();
            public DateTime registrationDate;
            public bool acceptingStudents = true;
            public string bio;
        }

        [Serializable]
        public class StudentProfile
        {
            public ulong studentId;
            public string studentName;
            public int level;
            public ulong currentMentorId;
            public int completedMilestones;
            public int sessionsAttended;
            public DateTime enrollmentDate;
            public List<MilestoneAchievement> achievedMilestones = new List<MilestoneAchievement>();
            public bool graduated;
            public DateTime graduationDate;
        }

        [Serializable]
        public class Mentorship
        {
            public string mentorshipId;
            public ulong mentorId;
            public ulong studentId;
            public MentorshipStatus status;
            public DateTime startDate;
            public DateTime? endDate;
            public int completedSessions;
            public List<Session> sessions = new List<Session>();
            public Dictionary<MilestoneType, bool> milestones = new Dictionary<MilestoneType, bool>();
            public float studentProgress;
        }

        [Serializable]
        public class Session
        {
            public string sessionId;
            public DateTime startTime;
            public DateTime endTime;
            public float durationMinutes;
            public int studentXPGained;
            public int mentorRewardGained;
            public SessionRating? rating;
            public string notes;
            public List<string> topicsCovered = new List<string>();
        }

        [Serializable]
        public class MilestoneAchievement
        {
            public MilestoneType milestone;
            public DateTime achievedDate;
            public int rewardAmount;
            public bool claimed;
        }

        [Serializable]
        public class MentorRequest
        {
            public string requestId;
            public ulong studentId;
            public ulong mentorId;
            public DateTime requestTime;
            public bool accepted;
            public bool processed;
            public string message;
        }

        // State
        private Dictionary<ulong, MentorProfile> mentors = new Dictionary<ulong, MentorProfile>();
        private Dictionary<ulong, StudentProfile> students = new Dictionary<ulong, StudentProfile>();
        private Dictionary<string, Mentorship> activeMentorships = new Dictionary<string, Mentorship>();
        private Queue<ulong> mentorQueue = new Queue<ulong>();
        private Queue<ulong> studentQueue = new Queue<ulong>();
        private List<MentorRequest> pendingRequests = new List<MentorRequest>();
        private float lastMatchTime;

        // Events
        public event Action<Mentorship> OnMentorshipStarted;
        public event Action<Mentorship> OnMentorshipCompleted;
        public event Action<ulong, MilestoneType> OnMilestoneAchieved;
        public event Action<Session> OnSessionCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
        }

        private void Update()
        {
            if (!IsServer || !enableMentorship) return;

            if (enableAutoMatching && Time.time - lastMatchTime >= matchRefreshInterval)
            {
                ProcessAutoMatching();
                lastMatchTime = Time.time;
            }
        }

        #region Mentor Registration

        [ServerRpc(RequireOwnership = false)]
        public void RegisterAsMentorServerRpc(ulong playerId, string bio, string[] specializations)
        {
            int playerLevel = Progression.ProgressionManager.Instance?.GetPlayerLevel(playerId) ?? 0;

            if (playerLevel < minMentorLevel)
            {
                Debug.LogWarning($"Player {playerId} level {playerLevel} is too low to be a mentor (min: {minMentorLevel})");
                return;
            }

            if (mentors.ContainsKey(playerId))
            {
                Debug.LogWarning($"Player {playerId} is already registered as a mentor");
                return;
            }

            var mentor = new MentorProfile
            {
                mentorId = playerId,
                mentorName = GetPlayerName(playerId),
                level = playerLevel,
                registrationDate = DateTime.UtcNow,
                bio = bio,
                specializations = specializations.ToList(),
                acceptingStudents = true
            };

            mentors[playerId] = mentor;
            mentorQueue.Enqueue(playerId);

            Debug.Log($"Player {playerId} registered as mentor");
        }

        [ServerRpc(RequireOwnership = false)]
        public void UnregisterAsMentorServerRpc(ulong playerId)
        {
            if (!mentors.ContainsKey(playerId)) return;

            // Complete all active mentorships
            var activeMentorshipsForMentor = activeMentorships.Values
                .Where(m => m.mentorId == playerId && m.status == MentorshipStatus.Active)
                .ToList();

            foreach (var mentorship in activeMentorshipsForMentor)
            {
                CompleteMentorship(mentorship.mentorshipId, false);
            }

            mentors.Remove(playerId);

            Debug.Log($"Player {playerId} unregistered as mentor");
        }

        #endregion

        #region Student Enrollment

        [ServerRpc(RequireOwnership = false)]
        public void EnrollAsStudentServerRpc(ulong playerId)
        {
            int playerLevel = Progression.ProgressionManager.Instance?.GetPlayerLevel(playerId) ?? 0;

            if (playerLevel > maxStudentLevel)
            {
                Debug.LogWarning($"Player {playerId} level {playerLevel} is too high to enroll as student (max: {maxStudentLevel})");
                return;
            }

            if (students.ContainsKey(playerId) && students[playerId].currentMentorId != 0)
            {
                Debug.LogWarning($"Player {playerId} already has an active mentorship");
                return;
            }

            if (!students.ContainsKey(playerId))
            {
                students[playerId] = new StudentProfile
                {
                    studentId = playerId,
                    studentName = GetPlayerName(playerId),
                    level = playerLevel,
                    enrollmentDate = DateTime.UtcNow
                };
            }

            studentQueue.Enqueue(playerId);

            Debug.Log($"Player {playerId} enrolled as student");
        }

        #endregion

        #region Mentorship Management

        [ServerRpc(RequireOwnership = false)]
        public void RequestMentorServerRpc(ulong studentId, ulong mentorId, string message)
        {
            if (!mentors.ContainsKey(mentorId))
            {
                Debug.LogWarning($"Mentor {mentorId} not found");
                return;
            }

            var mentor = mentors[mentorId];

            if (!mentor.acceptingStudents || mentor.activeStudents >= maxStudentsPerMentor)
            {
                Debug.LogWarning($"Mentor {mentorId} is not accepting students");
                return;
            }

            var request = new MentorRequest
            {
                requestId = Guid.NewGuid().ToString(),
                studentId = studentId,
                mentorId = mentorId,
                requestTime = DateTime.UtcNow,
                message = message
            };

            pendingRequests.Add(request);

            Debug.Log($"Student {studentId} requested mentor {mentorId}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void AcceptStudentServerRpc(ulong mentorId, string requestId)
        {
            var request = pendingRequests.FirstOrDefault(r => r.requestId == requestId && r.mentorId == mentorId);
            if (request == null) return;

            request.accepted = true;
            request.processed = true;

            CreateMentorship(mentorId, request.studentId);

            pendingRequests.Remove(request);
        }

        private void CreateMentorship(ulong mentorId, ulong studentId)
        {
            var mentorship = new Mentorship
            {
                mentorshipId = Guid.NewGuid().ToString(),
                mentorId = mentorId,
                studentId = studentId,
                status = MentorshipStatus.Active,
                startDate = DateTime.UtcNow
            };

            // Initialize milestones
            foreach (MilestoneType milestone in Enum.GetValues(typeof(MilestoneType)))
            {
                mentorship.milestones[milestone] = false;
            }

            activeMentorships[mentorship.mentorshipId] = mentorship;

            // Update profiles
            if (mentors.ContainsKey(mentorId))
            {
                mentors[mentorId].activeStudents++;
                mentors[mentorId].totalStudents++;
                mentors[mentorId].currentStudents.Add(studentId);
            }

            if (students.ContainsKey(studentId))
            {
                students[studentId].currentMentorId = mentorId;
            }

            OnMentorshipStarted?.Invoke(mentorship);

            Debug.Log($"Mentorship created between mentor {mentorId} and student {studentId}");
        }

        #endregion

        #region Session Management

        [ServerRpc(RequireOwnership = false)]
        public void StartSessionServerRpc(ulong mentorId, ulong studentId)
        {
            var mentorship = activeMentorships.Values.FirstOrDefault(m =>
                m.mentorId == mentorId &&
                m.studentId == studentId &&
                m.status == MentorshipStatus.Active);

            if (mentorship == null) return;

            var session = new Session
            {
                sessionId = Guid.NewGuid().ToString(),
                startTime = DateTime.UtcNow
            };

            mentorship.sessions.Add(session);

            Debug.Log($"Session started for mentorship {mentorship.mentorshipId}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void EndSessionServerRpc(ulong mentorId, ulong studentId, int xpGained, string[] topicsCovered)
        {
            var mentorship = activeMentorships.Values.FirstOrDefault(m =>
                m.mentorId == mentorId &&
                m.studentId == studentId &&
                m.status == MentorshipStatus.Active);

            if (mentorship == null) return;

            var session = mentorship.sessions.LastOrDefault();
            if (session == null) return;

            session.endTime = DateTime.UtcNow;
            session.durationMinutes = (float)(session.endTime - session.startTime).TotalMinutes;
            session.studentXPGained = xpGained;
            session.topicsCovered = topicsCovered.ToList();

            // Calculate rewards
            int bonusXP = Mathf.RoundToInt(xpGained * (xpBonusMultiplier - 1));
            session.studentXPGained += bonusXP;

            int mentorReward = Mathf.RoundToInt(session.durationMinutes * 10);
            session.mentorRewardGained = mentorReward;

            // Grant rewards
            if (enableRewards)
            {
                Progression.ProgressionManager.Instance?.AwardXP(studentId, bonusXP);
                Economy.EconomyManager.Instance?.AddSoftCurrency(mentorId, mentorReward);
            }

            mentorship.completedSessions++;

            if (mentors.ContainsKey(mentorId))
            {
                mentors[mentorId].totalSessions++;
            }

            if (students.ContainsKey(studentId))
            {
                students[studentId].sessionsAttended++;
            }

            OnSessionCompleted?.Invoke(session);

            // Check graduation
            CheckGraduation(mentorship);

            Debug.Log($"Session ended for mentorship {mentorship.mentorshipId}. Duration: {session.durationMinutes:F1}min");
        }

        [ServerRpc(RequireOwnership = false)]
        public void RateSessionServerRpc(ulong studentId, string mentorshipId, SessionRating rating)
        {
            if (!activeMentorships.ContainsKey(mentorshipId)) return;

            var mentorship = activeMentorships[mentorshipId];
            var session = mentorship.sessions.LastOrDefault();

            if (session != null)
            {
                session.rating = rating;

                // Update mentor average rating
                if (mentors.ContainsKey(mentorship.mentorId))
                {
                    var mentor = mentors[mentorship.mentorId];
                    var allRatings = activeMentorships.Values
                        .Where(m => m.mentorId == mentor.mentorId)
                        .SelectMany(m => m.sessions)
                        .Where(s => s.rating.HasValue)
                        .Select(s => (int)s.rating.Value)
                        .ToList();

                    if (allRatings.Any())
                    {
                        mentor.averageRating = allRatings.Average();
                    }
                }
            }
        }

        #endregion

        #region Milestones

        public void RecordMilestone(ulong studentId, MilestoneType milestone)
        {
            var mentorship = activeMentorships.Values.FirstOrDefault(m =>
                m.studentId == studentId &&
                m.status == MentorshipStatus.Active);

            if (mentorship == null) return;

            if (mentorship.milestones[milestone]) return; // Already achieved

            mentorship.milestones[milestone] = true;
            mentorship.studentProgress = mentorship.milestones.Count(m => m.Value) / (float)mentorship.milestones.Count;

            var achievement = new MilestoneAchievement
            {
                milestone = milestone,
                achievedDate = DateTime.UtcNow,
                rewardAmount = mentorRewardPerMilestone
            };

            // Add to both profiles
            if (mentors.ContainsKey(mentorship.mentorId))
            {
                mentors[mentorship.mentorId].milestones.Add(achievement);
            }

            if (students.ContainsKey(studentId))
            {
                students[studentId].achievedMilestones.Add(achievement);
                students[studentId].completedMilestones++;
            }

            // Grant reward to mentor
            if (enableRewards)
            {
                Economy.EconomyManager.Instance?.AddSoftCurrency(mentorship.mentorId, mentorRewardPerMilestone);
            }

            OnMilestoneAchieved?.Invoke(studentId, milestone);

            Debug.Log($"Student {studentId} achieved milestone: {milestone}");
        }

        #endregion

        #region Graduation

        private void CheckGraduation(Mentorship mentorship)
        {
            if (!autoGraduate) return;

            var student = students.ContainsKey(mentorship.studentId) ? students[mentorship.studentId] : null;
            if (student == null) return;

            bool shouldGraduate = false;

            // Check level
            int currentLevel = Progression.ProgressionManager.Instance?.GetPlayerLevel(mentorship.studentId) ?? 0;
            if (graduationLevels.Contains(currentLevel))
            {
                shouldGraduate = true;
            }

            // Check sessions
            if (mentorship.completedSessions >= minSessionsForGraduation)
            {
                shouldGraduate = true;
            }

            // Check milestones
            int completedMilestones = mentorship.milestones.Count(m => m.Value);
            if (completedMilestones >= mentorship.milestones.Count * 0.8f) // 80% completion
            {
                shouldGraduate = true;
            }

            if (shouldGraduate)
            {
                GraduateStudent(mentorship.mentorshipId);
            }
        }

        private void GraduateStudent(string mentorshipId)
        {
            if (!activeMentorships.ContainsKey(mentorshipId)) return;

            var mentorship = activeMentorships[mentorshipId];

            student.graduated = true;
            student.graduationDate = DateTime.UtcNow;

            CompleteMentorship(mentorshipId, true);

            Debug.Log($"Student {mentorship.studentId} graduated from mentorship");
        }

        #endregion

        #region Completion

        private void CompleteMentorship(string mentorshipId, bool successful)
        {
            if (!activeMentorships.ContainsKey(mentorshipId)) return;

            var mentorship = activeMentorships[mentorshipId];
            mentorship.status = successful ? MentorshipStatus.Completed : MentorshipStatus.Cancelled;
            mentorship.endDate = DateTime.UtcNow;

            // Update profiles
            if (mentors.ContainsKey(mentorship.mentorId))
            {
                var mentor = mentors[mentorship.mentorId];
                mentor.activeStudents--;
                mentor.currentStudents.Remove(mentorship.studentId);

                if (successful)
                {
                    mentor.graduatedStudents++;
                }
            }

            if (students.ContainsKey(mentorship.studentId))
            {
                students[mentorship.studentId].currentMentorId = 0;
            }

            activeMentorships.Remove(mentorshipId);

            OnMentorshipCompleted?.Invoke(mentorship);
        }

        #endregion

        #region Auto-Matching

        private void ProcessAutoMatching()
        {
            while (mentorQueue.Count > 0 && studentQueue.Count > 0)
            {
                var mentorId = mentorQueue.Dequeue();
                var studentId = studentQueue.Dequeue();

                if (!mentors.ContainsKey(mentorId) || !students.ContainsKey(studentId))
                    continue;

                var mentor = mentors[mentorId];

                if (!mentor.acceptingStudents || mentor.activeStudents >= maxStudentsPerMentor)
                    continue;

                CreateMentorship(mentorId, studentId);
            }
        }

        #endregion

        #region Public API

        public MentorProfile GetMentorProfile(ulong mentorId)
        {
            return mentors.ContainsKey(mentorId) ? mentors[mentorId] : null;
        }

        public StudentProfile GetStudentProfile(ulong studentId)
        {
            return students.ContainsKey(studentId) ? students[studentId] : null;
        }

        public List<MentorProfile> GetAvailableMentors()
        {
            return mentors.Values
                .Where(m => m.acceptingStudents && m.activeStudents < maxStudentsPerMentor)
                .OrderByDescending(m => m.averageRating)
                .ThenByDescending(m => m.graduatedStudents)
                .ToList();
        }

        public List<MentorRequest> GetPendingRequests(ulong mentorId)
        {
            return pendingRequests
                .Where(r => r.mentorId == mentorId && !r.processed)
                .ToList();
        }

        private string GetPlayerName(ulong playerId)
        {
            return $"Player_{playerId}";
        }

        #endregion
    }
}
