using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Social
{
    /// <summary>
    /// Looking For Group (LFG) system allowing players to find teammates
    /// based on game mode, skill level, and playstyle preferences.
    /// </summary>
    public class LFGSystem : NetworkBehaviour
    {
        public static LFGSystem Instance { get; private set; }

        [Header("LFG Configuration")]
        [SerializeField] private float listingExpiration = 600f; // 10 minutes

        private Dictionary<string, LFGListing> activeListings = new Dictionary<string, LFGListing>();
        private Dictionary<ulong, string> playerListings = new Dictionary<ulong, string>();

        public event Action<string, LFGListing> OnListingCreated;
        public event Action<string> OnListingExpired;
        public event Action<ulong, string> OnPlayerJoinedGroup;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Update()
        {
            if (IsServer)
            {
                CleanupExpiredListings();
            }
        }

        /// <summary>
        /// Create LFG listing
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void CreateListingServerRpc(ulong leaderId, LFGPreferences preferences, ServerRpcParams rpcParams = default)
        {
            // Remove existing listing if any
            if (playerListings.ContainsKey(leaderId))
            {
                RemoveListingServerRpc(leaderId);
            }

            string listingId = Guid.NewGuid().ToString();

            LFGListing listing = new LFGListing
            {
                listingId = listingId,
                leaderId = leaderId,
                preferences = preferences,
                members = new List<ulong> { leaderId },
                createdAt = Time.time,
                status = ListingStatus.Open
            };

            activeListings[listingId] = listing;
            playerListings[leaderId] = listingId;

            OnListingCreated?.Invoke(listingId, listing);
            NotifyListingCreatedClientRpc(leaderId, listingId);
        }

        /// <summary>
        /// Search for matching groups
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void SearchListingsServerRpc(ulong playerId, LFGPreferences searchPreferences, ServerRpcParams rpcParams = default)
        {
            List<LFGListing> matchingListings = new List<LFGListing>();

            foreach (var listing in activeListings.Values)
            {
                if (listing.status != ListingStatus.Open) continue;
                if (listing.members.Count >= listing.preferences.groupSize) continue;
                if (listing.members.Contains(playerId)) continue;

                // Check matching criteria
                if (IsMatchingListing(listing, searchPreferences))
                {
                    matchingListings.Add(listing);
                }
            }

            // Sort by match quality
            matchingListings = matchingListings
                .OrderByDescending(l => CalculateMatchScore(l, searchPreferences))
                .Take(10)
                .ToList();

            SendSearchResultsClientRpc(playerId, matchingListings.ToArray());
        }

        private bool IsMatchingListing(LFGListing listing, LFGPreferences searchPreferences)
        {
            // Must match game mode
            if (listing.preferences.gameMode != searchPreferences.gameMode) return false;

            // Check skill range if specified
            if (searchPreferences.minSkillLevel > 0 && listing.preferences.minSkillLevel > 0)
            {
                if (searchPreferences.minSkillLevel > listing.preferences.maxSkillLevel) return false;
                if (searchPreferences.maxSkillLevel < listing.preferences.minSkillLevel) return false;
            }

            // Check region if specified
            if (searchPreferences.preferredRegion != Region.Any &&
                listing.preferences.preferredRegion != Region.Any &&
                searchPreferences.preferredRegion != listing.preferences.preferredRegion)
            {
                return false;
            }

            return true;
        }

        private float CalculateMatchScore(LFGListing listing, LFGPreferences searchPreferences)
        {
            float score = 0f;

            // Game mode match (always true if we got here)
            score += 100f;

            // Playstyle match
            if (listing.preferences.playstyle == searchPreferences.playstyle)
            {
                score += 50f;
            }

            // Communication match
            if (listing.preferences.voiceChatRequired == searchPreferences.voiceChatRequired)
            {
                score += 30f;
            }

            // Region match
            if (listing.preferences.preferredRegion == searchPreferences.preferredRegion)
            {
                score += 40f;
            }

            // Experience level match
            if (listing.preferences.experienceLevel == searchPreferences.experienceLevel)
            {
                score += 20f;
            }

            return score;
        }

        /// <summary>
        /// Join a group
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void JoinListingServerRpc(ulong playerId, string listingId, ServerRpcParams rpcParams = default)
        {
            if (!activeListings.TryGetValue(listingId, out var listing)) return;
            if (listing.status != ListingStatus.Open) return;
            if (listing.members.Count >= listing.preferences.groupSize) return;
            if (listing.members.Contains(playerId)) return;

            // Add player to group
            listing.members.Add(playerId);

            OnPlayerJoinedGroup?.Invoke(playerId, listingId);

            // Check if group is full
            if (listing.members.Count >= listing.preferences.groupSize)
            {
                listing.status = ListingStatus.Full;
            }

            NotifyPlayerJoinedClientRpc(listing.leaderId, playerId, listingId);
            NotifyJoinSuccessClientRpc(playerId, listingId);

            // Create party if doesn't exist
            if (Party.PartySystem.Instance != null)
            {
                // Would integrate with party system
            }
        }

        /// <summary>
        /// Leave a group
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void LeaveListingServerRpc(ulong playerId, ServerRpcParams rpcParams = default)
        {
            string listingId = null;

            // Find listing player is in
            foreach (var kvp in activeListings)
            {
                if (kvp.Value.members.Contains(playerId))
                {
                    listingId = kvp.Key;
                    break;
                }
            }

            if (listingId == null) return;

            var listing = activeListings[listingId];

            // If leader leaves, disband group
            if (listing.leaderId == playerId)
            {
                RemoveListingServerRpc(playerId);
            }
            else
            {
                listing.members.Remove(playerId);

                // Reopen if was full
                if (listing.status == ListingStatus.Full)
                {
                    listing.status = ListingStatus.Open;
                }

                NotifyPlayerLeftClientRpc(listing.leaderId, playerId, listingId);
            }
        }

        /// <summary>
        /// Remove listing
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void RemoveListingServerRpc(ulong leaderId, ServerRpcParams rpcParams = default)
        {
            if (!playerListings.TryGetValue(leaderId, out string listingId)) return;

            if (activeListings.ContainsKey(listingId))
            {
                // Notify all members
                var listing = activeListings[listingId];
                NotifyListingClosedClientRpc(listing.members.ToArray(), listingId);

                activeListings.Remove(listingId);
            }

            playerListings.Remove(leaderId);
        }

        private void CleanupExpiredListings()
        {
            List<string> toRemove = new List<string>();

            foreach (var kvp in activeListings)
            {
                if (Time.time - kvp.Value.createdAt > listingExpiration)
                {
                    toRemove.Add(kvp.Key);
                    OnListingExpired?.Invoke(kvp.Key);
                }
            }

            foreach (string listingId in toRemove)
            {
                var listing = activeListings[listingId];
                playerListings.Remove(listing.leaderId);
                activeListings.Remove(listingId);

                NotifyListingExpiredClientRpc(listing.leaderId, listingId);
            }
        }

        [ClientRpc]
        private void NotifyListingCreatedClientRpc(ulong leaderId, string listingId)
        {
            if (NetworkManager.Singleton.LocalClientId != leaderId) return;
            Debug.Log($"<color=cyan>LFG Listing Created: {listingId}</color>");
        }

        [ClientRpc]
        private void SendSearchResultsClientRpc(ulong playerId, LFGListing[] listings)
        {
            if (NetworkManager.Singleton.LocalClientId != playerId) return;

            Debug.Log($"<color=cyan>Found {listings.Length} matching groups:</color>");
            for (int i = 0; i < listings.Length; i++)
            {
                var listing = listings[i];
                Debug.Log($"{i + 1}. {listing.preferences.gameMode} - {listing.members.Count}/{listing.preferences.groupSize} players");
            }
        }

        [ClientRpc]
        private void NotifyPlayerJoinedClientRpc(ulong leaderId, ulong playerId, string listingId)
        {
            if (NetworkManager.Singleton.LocalClientId != leaderId) return;
            Debug.Log($"<color=lime>Player {playerId} joined your group!</color>");
        }

        [ClientRpc]
        private void NotifyJoinSuccessClientRpc(ulong playerId, string listingId)
        {
            if (NetworkManager.Singleton.LocalClientId != playerId) return;
            Debug.Log($"<color=lime>Successfully joined group!</color>");
        }

        [ClientRpc]
        private void NotifyPlayerLeftClientRpc(ulong leaderId, ulong playerId, string listingId)
        {
            if (NetworkManager.Singleton.LocalClientId != leaderId) return;
            Debug.Log($"<color=yellow>Player {playerId} left the group.</color>");
        }

        [ClientRpc]
        private void NotifyListingClosedClientRpc(ulong[] members, string listingId)
        {
            ulong localId = NetworkManager.Singleton.LocalClientId;
            if (!members.Contains(localId)) return;
            Debug.Log($"<color=orange>Group disbanded.</color>");
        }

        [ClientRpc]
        private void NotifyListingExpiredClientRpc(ulong leaderId, string listingId)
        {
            if (NetworkManager.Singleton.LocalClientId != leaderId) return;
            Debug.Log($"<color=orange>Your LFG listing has expired.</color>");
        }

        public LFGListing GetPlayerListing(ulong playerId)
        {
            if (!playerListings.TryGetValue(playerId, out string listingId)) return null;
            return activeListings.TryGetValue(listingId, out var listing) ? listing : null;
        }

        [Serializable]
        public class LFGListing
        {
            public string listingId;
            public ulong leaderId;
            public LFGPreferences preferences;
            public List<ulong> members;
            public float createdAt;
            public ListingStatus status;
        }

        [Serializable]
        public class LFGPreferences
        {
            public string gameMode = "Survival";
            public int groupSize = 4;
            public PlayStyle playstyle = PlayStyle.Balanced;
            public bool voiceChatRequired = false;
            public Region preferredRegion = Region.Any;
            public int minSkillLevel = 0;
            public int maxSkillLevel = 100;
            public ExperienceLevel experienceLevel = ExperienceLevel.Any;
            public string description = "";
        }

        public enum PlayStyle
        {
            Casual,
            Balanced,
            Competitive,
            Hardcore
        }

        public enum Region
        {
            Any,
            NorthAmerica,
            Europe,
            Asia,
            SouthAmerica,
            Oceania
        }

        public enum ExperienceLevel
        {
            Any,
            Beginner,
            Intermediate,
            Advanced,
            Expert
        }

        public enum ListingStatus
        {
            Open,
            Full,
            InProgress,
            Closed
        }
    }
}
