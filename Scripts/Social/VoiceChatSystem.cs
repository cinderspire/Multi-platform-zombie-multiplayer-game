using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace ZombieGame.Social
{
    public class VoiceChatSystem : NetworkBehaviour
    {
        public static VoiceChatSystem Instance { get; private set; }

        [Header("Voice Chat Settings")]
        [SerializeField] private bool pushToTalkEnabled = true;
        [SerializeField] private KeyCode pushToTalkKey = KeyCode.V;
        [SerializeField] private VoiceChatChannel defaultChannel = VoiceChatChannel.Proximity;
        [SerializeField] private float proximityRadius = 20f;

        private Dictionary<ulong, VoiceChatState> voiceChatStates = new Dictionary<ulong, VoiceChatState>();
        private bool isTransmitting = false;

        public event Action<ulong, bool> OnPlayerVoiceStateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Update()
        {
            if (pushToTalkEnabled)
            {
                bool pressing = Input.GetKey(pushToTalkKey);
                if (pressing != isTransmitting)
                {
                    isTransmitting = pressing;
                    SetTransmittingServerRpc(NetworkManager.Singleton.LocalClientId, isTransmitting);
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void SetTransmittingServerRpc(ulong playerId, bool transmitting, ServerRpcParams rpcParams = default)
        {
            if (!voiceChatStates.ContainsKey(playerId))
            {
                voiceChatStates[playerId] = new VoiceChatState
                {
                    playerId = playerId,
                    channel = defaultChannel
                };
            }

            voiceChatStates[playerId].isTransmitting = transmitting;
            OnPlayerVoiceStateChanged?.Invoke(playerId, transmitting);
            SetTransmittingClientRpc(playerId, transmitting);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SetVoiceChannelServerRpc(ulong playerId, VoiceChatChannel channel, ServerRpcParams rpcParams = default)
        {
            if (!voiceChatStates.ContainsKey(playerId))
            {
                voiceChatStates[playerId] = new VoiceChatState { playerId = playerId };
            }

            voiceChatStates[playerId].channel = channel;
        }

        [ClientRpc]
        private void SetTransmittingClientRpc(ulong playerId, bool transmitting) { }

        public bool IsPlayerTransmitting(ulong playerId)
        {
            return voiceChatStates.TryGetValue(playerId, out var state) && state.isTransmitting;
        }

        public bool CanHearPlayer(ulong listener, ulong speaker)
        {
            if (!voiceChatStates.TryGetValue(speaker, out var state)) return false;
            if (!state.isTransmitting) return false;

            switch (state.channel)
            {
                case VoiceChatChannel.Global:
                    return true;

                case VoiceChatChannel.Team:
                    return Network.TeamSystem.Instance?.ArePlayersOnSameTeam(listener, speaker) ?? false;

                case VoiceChatChannel.Party:
                    return Party.PartySystem.Instance?.AreInSameParty(listener, speaker) ?? false;

                case VoiceChatChannel.Proximity:
                    return IsInProximity(listener, speaker);

                default:
                    return false;
            }
        }

        private bool IsInProximity(ulong player1, ulong player2)
        {
            var client1 = NetworkManager.Singleton.ConnectedClients.GetValueOrDefault(player1);
            var client2 = NetworkManager.Singleton.ConnectedClients.GetValueOrDefault(player2);

            if (client1?.PlayerObject == null || client2?.PlayerObject == null) return false;

            float distance = Vector3.Distance(
                client1.PlayerObject.transform.position,
                client2.PlayerObject.transform.position
            );

            return distance <= proximityRadius;
        }
    }

    [Serializable]
    public class VoiceChatState
    {
        public ulong playerId;
        public bool isTransmitting;
        public bool isMuted;
        public VoiceChatChannel channel;
    }

    public enum VoiceChatChannel { Global, Team, Party, Proximity }
}
