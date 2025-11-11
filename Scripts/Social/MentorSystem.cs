using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Social
{
    /// <summary>
    /// Mentor/Recruit system connecting veteran players with new players,
    /// providing rewards for both parties and tracking mentorship progress.
    /// </summary>
    public class MentorSystem : NetworkBehaviour
    {
        public static MentorSystem Instance { get; private set; }

        [Header("Mentor Configuration")]
        [SerializeField] private int minLevelToMentor = 25;
        [SerializeField] private int maxRecruitsPerMentor = 5;
        [SerializeField] private int recruitLevelCap = 10;

        private Dictionary<ulong, MentorProfile> mentorProfiles = new Dictionary<ulong, MentorProfile>();
        private Dictionary<ulong, RecruitProfile> recruitProfiles = new Dictionary<ulong, RecruitProfile>();

        public event Action<ulong, ulong> OnMentorshipEstablished;
        public event Action<ulong, ulong, MentorMilestone> OnMilestoneReached;
        public event Action<ulong, MentorReward> OnRewardEarned;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        /// <summary>
        /// Register as mentor
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void RegisterAsMentorServerRpc(ulong playerId, ServerRpcParams rpcParams = default)
        {
            // Check level requirement
            int playerLevel = GetPlayerLevel(playerId);
            if (playerLevel < minLevelToMentor)
            {
                NotifyInsufficientLevelClientRpc(playerId, minLevelToMentor);
                return;
            }

            if (mentorProfiles.ContainsKey(playerId)) return;

            mentorProfiles[playerId] = new MentorProfile
            {
                mentorId = playerId,
                recruits = new List<ulong>(),
                totalRecruitsGraduated = 0,
                mentorshipStartDate = DateTime.UtcNow,
                mentorRating = 5f,
                milestonesCompleted = new List<MentorMilestone>()
            };

            NotifyMentorRegisteredClientRpc(playerId);
        }

        /// <summary>
        /// Request mentorship
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void RequestMentorServerRpc(ulong recruitId, ServerRpcParams rpcParams = default)
        {
            // Check if eligible for mentorship
            int playerLevel = GetPlayerLevel(recruitId);
            if (playerLevel > recruitLevelCap)
            {
                NotifyTooHighLevelClientRpc(recruitId);
                return;
            }

            if (recruitProfiles.ContainsKey(recruitId)) return;

            // Find available mentor
            ulong mentorId = FindAvailableMentor();

            if (mentorId == 0)
            {
                NotifyNoMentorAvailableClientRpc(recruitId);
                return;
            }

            EstablishMentorship(mentorId, recruitId);
        }

        /// <summary>
        /// Accept recruit
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void AcceptRecruitServerRpc(ulong mentorId, ulong recruitId, ServerRpcParams rpcParams = default)
        {
            if (!mentorProfiles.TryGetValue(mentorId, out var mentor)) return;

            if (mentor.recruits.Count >= maxRecruitsPerMentor)
            {
                NotifyMaxRecruitsClientRpc(mentorId);
                return;
            }

            EstablishMentorship(mentorId, recruitId);
        }

        private void EstablishMentorship(ulong mentorId, ulong recruitId)
        {
            // Create/update profiles
            if (!mentorProfiles.ContainsKey(mentorId))
            {
                RegisterAsMentorServerRpc(mentorId);
            }

            var mentor = mentorProfiles[mentorId];
            mentor.recruits.Add(recruitId);

            recruitProfiles[recruitId] = new RecruitProfile
            {
                recruitId = recruitId,
                mentorId = mentorId,
                mentorshipStartDate = DateTime.UtcNow,
                startLevel = GetPlayerLevel(recruitId),
                currentLevel = GetPlayerLevel(recruitId),
                milestonesCompleted = new List<RecruitMilestone>()
            };

            OnMentorshipEstablished?.Invoke(mentorId, recruitId);
            NotifyMentorshipEstablishedClientRpc(mentorId, recruitId);

            // Give initial rewards
            AwardMentorshipBonuses(mentorId, recruitId);
        }

        /// <summary>
        /// Update recruit progress
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void UpdateRecruitProgressServerRpc(ulong recruitId, ServerRpcParams rpcParams = default)
        {
            if (!recruitProfiles.TryGetValue(recruitId, out var recruit)) return;

            int currentLevel = GetPlayerLevel(recruitId);
            int oldLevel = recruit.currentLevel;
            recruit.currentLevel = currentLevel;

            // Check milestones
            CheckRecruitMilestones(recruit, oldLevel, currentLevel);

            // Check graduation
            if (currentLevel > recruitLevelCap)
            {
                GraduateRecruit(recruitId);
            }
        }

        private void CheckRecruitMilestones(RecruitProfile recruit, int oldLevel, int newLevel)
        {
            int[] milestoneLevels = { 5, 10, 15, 20, 25 };

            foreach (int level in milestoneLevels)
            {
                if (newLevel >= level && oldLevel < level)
                {
                    RecruitMilestone milestone = (RecruitMilestone)level;

                    if (!recruit.milestonesCompleted.Contains(milestone))
                    {
                        recruit.milestonesCompleted.Add(milestone);

                        // Reward both mentor and recruit
                        RewardMilestone(recruit.mentorId, recruit.recruitId, milestone);
                    }
                }
            }
        }

        private void RewardMilestone(ulong mentorId, ulong recruitId, RecruitMilestone milestone)
        {
            // Mentor rewards
            MentorReward mentorReward = new MentorReward
            {
                xp = 500,
                currency = 100,
                itemReward = milestone == RecruitMilestone.Level25 ? "mentor_exclusive_skin" : null
            };

            AwardRewards(mentorId, mentorReward);

            // Recruit rewards
            MentorReward recruitReward = new MentorReward
            {
                xp = 200,
                currency = 50
            };

            AwardRewards(recruitId, recruitReward);

            OnMilestoneReached?.Invoke(mentorId, recruitId, (MentorMilestone)milestone);
            NotifyMilestoneClientRpc(mentorId, recruitId, (int)milestone);
        }

        private void GraduateRecruit(ulong recruitId)
        {
            if (!recruitProfiles.TryGetValue(recruitId, out var recruit)) return;

            ulong mentorId = recruit.mentorId;
            var mentor = mentorProfiles[mentorId];

            // Remove from mentor's active recruits
            mentor.recruits.Remove(recruitId);
            mentor.totalRecruitsGraduated++;

            // Graduation rewards
            MentorReward graduationReward = new MentorReward
            {
                xp = 2000,
                currency = 500,
                itemReward = "graduation_title",
                exclusive Badge = "mentor_badge_" + mentor.totalRecruitsGraduated
            };

            AwardRewards(mentorId, graduationReward);
            NotifyGraduationClientRpc(mentorId, recruitId);

            // Remove recruit profile
            recruitProfiles.Remove(recruitId);

            // Check mentor milestones
            CheckMentorMilestones(mentorId);
        }

        private void CheckMentorMilestones(ulong mentorId)
        {
            var mentor = mentorProfiles[mentorId];

            int[] mentorMilestones = { 5, 10, 25, 50, 100 };

            foreach (int count in mentorMilestones)
            {
                if (mentor.totalRecruitsGraduated >= count)
                {
                    MentorMilestone milestone = (MentorMilestone)(count);

                    if (!mentor.milestonesCompleted.Contains(milestone))
                    {
                        mentor.milestonesCompleted.Add(milestone);

                        MentorReward reward = new MentorReward
                        {
                            xp = count * 100,
                            currency = count * 50,
                            itemReward = $"mentor_title_{count}",
                            exclusiveBadge = $"mentor_legend_{count}"
                        };

                        AwardRewards(mentorId, reward);
                        NotifyMentorMilestoneClientRpc(mentorId, count);
                    }
                }
            }
        }

        private void AwardRewards(ulong playerId, MentorReward reward)
        {
            // Award XP
            if (reward.xp > 0 && Progression.ProgressionSystem.Instance != null)
            {
                Progression.ProgressionSystem.Instance.AddExperienceServerRpc(playerId, reward.xp);
            }

            // Award currency (integrate with economy)

            // Award items
            if (!string.IsNullOrEmpty(reward.itemReward) && Inventory.InventorySystem.Instance != null)
            {
                Inventory.InventorySystem.Instance.AddItemServerRpc(playerId, reward.itemReward, 1);
            }

            OnRewardEarned?.Invoke(playerId, reward);
        }

        private void AwardMentorshipBonuses(ulong mentorId, ulong recruitId)
        {
            // Initial bonuses for joining
            MentorReward mentorBonus = new MentorReward { xp = 100, currency = 50 };
            MentorReward recruitBonus = new MentorReward { xp = 100, currency = 50, itemReward = "welcome_pack" };

            AwardRewards(mentorId, mentorBonus);
            AwardRewards(recruitId, recruitBonus);
        }

        private ulong FindAvailableMentor()
        {
            foreach (var kvp in mentorProfiles)
            {
                if (kvp.Value.recruits.Count < maxRecruitsPerMentor)
                {
                    return kvp.Key;
                }
            }
            return 0;
        }

        private int GetPlayerLevel(ulong playerId)
        {
            // Would integrate with progression system
            return 1;
        }

        [ClientRpc]
        private void NotifyMentorRegisteredClientRpc(ulong mentorId)
        {
            if (NetworkManager.Singleton.LocalClientId != mentorId) return;
            Debug.Log("<color=cyan>You are now registered as a Mentor!</color>");
        }

        [ClientRpc]
        private void NotifyMentorshipEstablishedClientRpc(ulong mentorId, ulong recruitId)
        {
            ulong localId = NetworkManager.Singleton.LocalClientId;

            if (localId == mentorId)
            {
                Debug.Log($"<color=lime>You are now mentoring Player {recruitId}!</color>");
            }
            else if (localId == recruitId)
            {
                Debug.Log($"<color=lime>Player {mentorId} is now your Mentor!</color>");
            }
        }

        [ClientRpc]
        private void NotifyMilestoneClientRpc(ulong mentorId, ulong recruitId, int level)
        {
            ulong localId = NetworkManager.Singleton.LocalClientId;

            if (localId == mentorId || localId == recruitId)
            {
                Debug.Log($"<color=gold>★ MILESTONE REACHED: Level {level} ★</color>");
                Debug.Log("<color=yellow>Rewards earned!</color>");
            }
        }

        [ClientRpc]
        private void NotifyGraduationClientRpc(ulong mentorId, ulong recruitId)
        {
            ulong localId = NetworkManager.Singleton.LocalClientId;

            if (localId == mentorId)
            {
                Debug.Log($"<color=gold>★★ RECRUIT GRADUATED! ★★</color>");
                Debug.Log($"<color=lime>Player {recruitId} has completed mentorship!</color>");
            }
            else if (localId == recruitId)
            {
                Debug.Log($"<color=gold>★★ MENTORSHIP COMPLETE! ★★</color>");
                Debug.Log("<color=lime>You have graduated!</color>");
            }
        }

        [ClientRpc]
        private void NotifyMentorMilestoneClientRpc(ulong mentorId, int graduateCount)
        {
            if (NetworkManager.Singleton.LocalClientId != mentorId) return;
            Debug.Log($"<color=gold>★★★ MENTOR MILESTONE: {graduateCount} Graduates! ★★★</color>");
        }

        [ClientRpc]
        private void NotifyInsufficientLevelClientRpc(ulong playerId, int requiredLevel)
        {
            if (NetworkManager.Singleton.LocalClientId != playerId) return;
            Debug.Log($"<color=red>You need to be level {requiredLevel} to become a Mentor.</color>");
        }

        [ClientRpc]
        private void NotifyTooHighLevelClientRpc(ulong playerId)
        {
            if (NetworkManager.Singleton.LocalClientId != playerId) return;
            Debug.Log("<color=yellow>You are too experienced for mentorship.</color>");
        }

        [ClientRpc]
        private void NotifyNoMentorAvailableClientRpc(ulong playerId)
        {
            if (NetworkManager.Singleton.LocalClientId != playerId) return;
            Debug.Log("<color=yellow>No mentors available right now. Try again later.</color>");
        }

        [ClientRpc]
        private void NotifyMaxRecruitsClientRpc(ulong playerId)
        {
            if (NetworkManager.Singleton.LocalClientId != playerId) return;
            Debug.Log("<color=yellow>You have reached the maximum number of recruits.</color>");
        }

        [Serializable]
        private class MentorProfile
        {
            public ulong mentorId;
            public List<ulong> recruits;
            public int totalRecruitsGraduated;
            public DateTime mentorshipStartDate;
            public float mentorRating;
            public List<MentorMilestone> milestonesCompleted;
        }

        [Serializable]
        private class RecruitProfile
        {
            public ulong recruitId;
            public ulong mentorId;
            public DateTime mentorshipStartDate;
            public int startLevel;
            public int currentLevel;
            public List<RecruitMilestone> milestonesCompleted;
        }

        [Serializable]
        public class MentorReward
        {
            public int xp;
            public int currency;
            public string itemReward;
            public string exclusiveBadge;
        }

        public enum MentorMilestone
        {
            Graduates5 = 5,
            Graduates10 = 10,
            Graduates25 = 25,
            Graduates50 = 50,
            Graduates100 = 100
        }

        public enum RecruitMilestone
        {
            Level5 = 5,
            Level10 = 10,
            Level15 = 15,
            Level20 = 20,
            Level25 = 25
        }
    }
}
