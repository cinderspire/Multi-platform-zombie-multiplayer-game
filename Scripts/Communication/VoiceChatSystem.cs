using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Communication
{
    /// <summary>
    /// Comprehensive voice chat and communication system for multi-platform zombie multiplayer game.
    /// Handles voice channels, proximity chat, push-to-talk, spatial audio, and moderation integration.
    /// </summary>
    public class VoiceChatSystem : NetworkBehaviour
    {
        public static VoiceChatSystem Instance { get; private set; }

        [Header("Voice Chat Settings")]
        [SerializeField] private bool enableVoiceChat = true;
        [SerializeField] private bool enableProximityChat = true;
        [SerializeField] private float proximityRange = 20f;
        [SerializeField] private float maxVoiceRange = 100f;
        [SerializeField] private bool enableSpatialAudio = true;

        [Header("Channel Settings")]
        [SerializeField] private bool enableTeamChannel = true;
        [SerializeField] private bool enableGlobalChannel = false;
        [SerializeField] private bool enablePartyChannel = true;
        [SerializeField] private bool enableRadioChannel = true;

        [Header("Quality Settings")]
        [SerializeField] private VoiceQuality defaultQuality = VoiceQuality.Medium;
        [SerializeField] private int sampleRate = 48000;
        [SerializeField] private int bitrate = 64000;
        [SerializeField] private bool enableNoiseSupression = true;
        [SerializeField] private bool enableEchoCancellation = true;

        [Header("Push-to-Talk Settings")]
        [SerializeField] private bool enablePushToTalk = true;
        [SerializeField] private KeyCode pushToTalkKey = KeyCode.V;
        [SerializeField] private float voiceActivationThreshold = 0.02f;
        [SerializeField] private bool enableVoiceActivationDetection = false;

        [Header("Moderation")]
        [SerializeField] private bool enableVoiceModeration = true;
        [SerializeField] private int maxMutedPlayers = 100;
        [SerializeField] private float muteExpiration = 86400f; // 24 hours

        [Header("Network Optimization")]
        [SerializeField] private bool enableAdaptiveBitrate = true;
        [SerializeField] private int maxBandwidthKbps = 128;
        [SerializeField] private float packetSendInterval = 0.02f; // 50Hz
        [SerializeField] private bool enableJitterBuffer = true;

        // Enums
        public enum VoiceChannel
        {
            None,
            Proximity,
            Team,
            Global,
            Party,
            Radio,
            Spectator
        }

        public enum VoiceQuality
        {
            Low,      // 16kbps, 16kHz
            Medium,   // 64kbps, 48kHz
            High,     // 128kbps, 48kHz
            Ultra     // 256kbps, 96kHz
        }

        public enum MicrophoneState
        {
            Inactive,
            Active,
            Muted,
            Disabled
        }

        // Data structures
        [Serializable]
        public class VoiceSettings
        {
            public float inputVolume = 1f;
            public float outputVolume = 1f;
            public VoiceQuality quality = VoiceQuality.Medium;
            public bool pushToTalkEnabled = true;
            public bool voiceActivationEnabled = false;
            public float voiceActivationThreshold = 0.02f;
            public bool spatialAudioEnabled = true;
            public VoiceChannel activeChannel = VoiceChannel.Team;
            public HashSet<VoiceChannel> enabledChannels = new HashSet<VoiceChannel>();
        }

        [Serializable]
        public class VoiceStream
        {
            public ulong playerId;
            public VoiceChannel channel;
            public byte[] audioData;
            public int frequency;
            public int channels;
            public float timestamp;
            public Vector3 position; // For spatial audio
            public float volume;
            public bool isSpatial;
        }

        [Serializable]
        public class PlayerVoiceData
        {
            public ulong playerId;
            public MicrophoneState micState = MicrophoneState.Inactive;
            public VoiceChannel currentChannel = VoiceChannel.Team;
            public HashSet<VoiceChannel> activeChannels = new HashSet<VoiceChannel>();
            public float lastTransmitTime;
            public float totalTransmitTime;
            public bool isMuted;
            public DateTime muteExpiration;
            public HashSet<ulong> mutedPlayers = new HashSet<ulong>(); // Players this user has muted
            public float avgVolume;
            public int packetsReceived;
            public int packetsLost;
        }

        [Serializable]
        public class VoiceChannel_Data
        {
            public VoiceChannel channelType;
            public string channelName;
            public HashSet<ulong> activeParticipants = new HashSet<ulong>();
            public int maxParticipants = 100;
            public bool isActive = true;
            public float volume = 1f;
            public bool requiresPermission = false;
        }

        [Serializable]
        public class ProximityVoiceData
        {
            public ulong playerId;
            public Vector3 position;
            public float range;
            public float volume;
            public DateTime lastUpdate;
        }

        // State
        private Dictionary<ulong, PlayerVoiceData> playerVoiceData = new Dictionary<ulong, PlayerVoiceData>();
        private Dictionary<VoiceChannel, VoiceChannel_Data> voiceChannels = new Dictionary<VoiceChannel, VoiceChannel_Data>();
        private Dictionary<ulong, VoiceSettings> playerSettings = new Dictionary<ulong, VoiceSettings>();
        private Dictionary<ulong, ProximityVoiceData> proximityData = new Dictionary<ulong, ProximityVoiceData>();
        private Queue<VoiceStream> voiceStreamQueue = new Queue<VoiceStream>();
        private Dictionary<ulong, Queue<byte[]>> jitterBuffers = new Dictionary<ulong, Queue<byte[]>>();

        // Audio sources
        private Dictionary<ulong, AudioSource> playerAudioSources = new Dictionary<ulong, AudioSource>();
        private AudioClip microphoneClip;
        private string currentMicrophone;
        private bool isRecording = false;
        private float[] microphoneBuffer;
        private int microphonePosition = 0;

        // Events
        public event Action<ulong, VoiceChannel> OnPlayerJoinedChannel;
        public event Action<ulong, VoiceChannel> OnPlayerLeftChannel;
        public event Action<ulong, bool> OnPlayerStartedTalking;
        public event Action<ulong, bool> OnPlayerMuted;
        public event Action<ulong, MicrophoneState> OnMicrophoneStateChanged;
        public event Action<VoiceChannel, bool> OnChannelStateChanged;

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

            if (IsServer)
            {
                InitializeChannels();
            }

            InitializeMicrophone();
        }

        private void Update()
        {
            if (!enableVoiceChat) return;

            if (IsServer)
            {
                ProcessVoiceStreams();
                UpdateProximityChat();
            }

            if (IsClient)
            {
                HandlePushToTalk();
                ProcessJitterBuffers();
            }
        }

        #region Initialization

        private void InitializeChannels()
        {
            RegisterChannel(new VoiceChannel_Data
            {
                channelType = VoiceChannel.Proximity,
                channelName = "Proximity Chat",
                maxParticipants = 1000,
                isActive = enableProximityChat
            });

            RegisterChannel(new VoiceChannel_Data
            {
                channelType = VoiceChannel.Team,
                channelName = "Team Voice",
                maxParticipants = 50,
                isActive = enableTeamChannel
            });

            RegisterChannel(new VoiceChannel_Data
            {
                channelType = VoiceChannel.Global,
                channelName = "Global Voice",
                maxParticipants = 100,
                isActive = enableGlobalChannel,
                requiresPermission = true
            });

            RegisterChannel(new VoiceChannel_Data
            {
                channelType = VoiceChannel.Party,
                channelName = "Party Voice",
                maxParticipants = 20,
                isActive = enablePartyChannel
            });

            RegisterChannel(new VoiceChannel_Data
            {
                channelType = VoiceChannel.Radio,
                channelName = "Radio",
                maxParticipants = 100,
                isActive = enableRadioChannel
            });

            RegisterChannel(new VoiceChannel_Data
            {
                channelType = VoiceChannel.Spectator,
                channelName = "Spectator",
                maxParticipants = 100,
                isActive = true
            });
        }

        private void RegisterChannel(VoiceChannel_Data channel)
        {
            voiceChannels[channel.channelType] = channel;
        }

        private void InitializeMicrophone()
        {
            if (Microphone.devices.Length == 0)
            {
                Debug.LogWarning("No microphone detected!");
                return;
            }

            currentMicrophone = Microphone.devices[0];
            microphoneBuffer = new float[sampleRate * 2]; // 2 second buffer
        }

        #endregion

        #region Voice Channel Management

        [ServerRpc(RequireOwnership = false)]
        public void JoinVoiceChannelServerRpc(ulong playerId, VoiceChannel channel)
        {
            if (!voiceChannels.ContainsKey(channel) || !voiceChannels[channel].isActive)
            {
                Debug.LogWarning($"Voice channel {channel} is not available");
                return;
            }

            var channelData = voiceChannels[channel];

            if (channelData.activeParticipants.Count >= channelData.maxParticipants)
            {
                Debug.LogWarning($"Voice channel {channel} is full");
                return;
            }

            if (!playerVoiceData.ContainsKey(playerId))
            {
                playerVoiceData[playerId] = new PlayerVoiceData { playerId = playerId };
            }

            channelData.activeParticipants.Add(playerId);
            playerVoiceData[playerId].activeChannels.Add(channel);
            playerVoiceData[playerId].currentChannel = channel;

            OnPlayerJoinedChannel?.Invoke(playerId, channel);
            NotifyChannelJoinClientRpc(playerId, channel);
        }

        [ServerRpc(RequireOwnership = false)]
        public void LeaveVoiceChannelServerRpc(ulong playerId, VoiceChannel channel)
        {
            if (!voiceChannels.ContainsKey(channel)) return;

            var channelData = voiceChannels[channel];
            channelData.activeParticipants.Remove(playerId);

            if (playerVoiceData.ContainsKey(playerId))
            {
                playerVoiceData[playerId].activeChannels.Remove(channel);
            }

            OnPlayerLeftChannel?.Invoke(playerId, channel);
            NotifyChannelLeaveClientRpc(playerId, channel);
        }

        [ClientRpc]
        private void NotifyChannelJoinClientRpc(ulong playerId, VoiceChannel channel)
        {
            OnPlayerJoinedChannel?.Invoke(playerId, channel);
        }

        [ClientRpc]
        private void NotifyChannelLeaveClientRpc(ulong playerId, VoiceChannel channel)
        {
            OnPlayerLeftChannel?.Invoke(playerId, channel);
        }

        public void SwitchChannel(ulong playerId, VoiceChannel newChannel)
        {
            if (IsServer)
            {
                if (playerVoiceData.ContainsKey(playerId))
                {
                    var currentChannel = playerVoiceData[playerId].currentChannel;
                    if (currentChannel != VoiceChannel.None)
                    {
                        LeaveVoiceChannelServerRpc(playerId, currentChannel);
                    }
                }
                JoinVoiceChannelServerRpc(playerId, newChannel);
            }
            else
            {
                SwitchChannelServerRpc(playerId, newChannel);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void SwitchChannelServerRpc(ulong playerId, VoiceChannel newChannel)
        {
            SwitchChannel(playerId, newChannel);
        }

        #endregion

        #region Voice Transmission

        private void HandlePushToTalk()
        {
            if (!enablePushToTalk) return;

            bool isPressed = Input.GetKey(pushToTalkKey);

            if (isPressed && !isRecording)
            {
                StartRecording();
            }
            else if (!isPressed && isRecording)
            {
                StopRecording();
            }

            if (isRecording)
            {
                CaptureAudio();
            }
        }

        private void StartRecording()
        {
            if (currentMicrophone == null) return;

            isRecording = true;
            microphoneClip = Microphone.Start(currentMicrophone, true, 2, sampleRate);
            microphonePosition = 0;

            var localPlayerId = NetworkManager.Singleton.LocalClientId;
            UpdateMicrophoneStateServerRpc(localPlayerId, MicrophoneState.Active);
        }

        private void StopRecording()
        {
            isRecording = false;
            Microphone.End(currentMicrophone);

            var localPlayerId = NetworkManager.Singleton.LocalClientId;
            UpdateMicrophoneStateServerRpc(localPlayerId, MicrophoneState.Inactive);
        }

        private void CaptureAudio()
        {
            if (microphoneClip == null) return;

            int currentPosition = Microphone.GetPosition(currentMicrophone);
            if (currentPosition < 0 || currentPosition == microphonePosition) return;

            int sampleCount = currentPosition - microphonePosition;
            if (sampleCount < 0)
            {
                sampleCount = microphoneClip.samples - microphonePosition + currentPosition;
            }

            float[] samples = new float[sampleCount];
            microphoneClip.GetData(samples, microphonePosition);
            microphonePosition = currentPosition;

            // Check if audio is above threshold
            float avgVolume = CalculateAverageVolume(samples);

            if (enableVoiceActivationDetection && avgVolume < voiceActivationThreshold)
            {
                return; // Too quiet, don't transmit
            }

            // Convert to bytes and transmit
            byte[] audioData = ConvertFloatsToBytes(samples);
            TransmitVoiceDataServerRpc(NetworkManager.Singleton.LocalClientId, audioData, sampleRate, 1);
        }

        [ServerRpc(RequireOwnership = false)]
        private void TransmitVoiceDataServerRpc(ulong senderId, byte[] audioData, int frequency, int channels)
        {
            if (!playerVoiceData.ContainsKey(senderId)) return;

            var playerData = playerVoiceData[senderId];

            if (playerData.isMuted)
            {
                CheckMuteExpiration(senderId);
                if (playerData.isMuted) return;
            }

            playerData.lastTransmitTime = Time.time;
            playerData.totalTransmitTime += Time.deltaTime;

            var stream = new VoiceStream
            {
                playerId = senderId,
                channel = playerData.currentChannel,
                audioData = audioData,
                frequency = frequency,
                channels = channels,
                timestamp = Time.time,
                volume = 1f
            };

            // Get player position for spatial audio
            if (proximityData.ContainsKey(senderId))
            {
                stream.position = proximityData[senderId].position;
                stream.isSpatial = enableSpatialAudio;
            }

            voiceStreamQueue.Enqueue(stream);
        }

        [ServerRpc(RequireOwnership = false)]
        private void UpdateMicrophoneStateServerRpc(ulong playerId, MicrophoneState state)
        {
            if (!playerVoiceData.ContainsKey(playerId))
            {
                playerVoiceData[playerId] = new PlayerVoiceData { playerId = playerId };
            }

            playerVoiceData[playerId].micState = state;
            OnMicrophoneStateChanged?.Invoke(playerId, state);
            NotifyMicrophoneStateClientRpc(playerId, state);
        }

        [ClientRpc]
        private void NotifyMicrophoneStateClientRpc(ulong playerId, MicrophoneState state)
        {
            OnMicrophoneStateChanged?.Invoke(playerId, state);
        }

        #endregion

        #region Voice Processing

        private void ProcessVoiceStreams()
        {
            while (voiceStreamQueue.Count > 0)
            {
                var stream = voiceStreamQueue.Dequeue();
                DistributeVoiceStream(stream);
            }
        }

        private void DistributeVoiceStream(VoiceStream stream)
        {
            var channel = voiceChannels[stream.channel];

            foreach (var participantId in channel.activeParticipants)
            {
                if (participantId == stream.playerId) continue; // Don't send to self

                // Check if recipient has muted the sender
                if (playerVoiceData.ContainsKey(participantId))
                {
                    if (playerVoiceData[participantId].mutedPlayers.Contains(stream.playerId))
                        continue;
                }

                // For proximity chat, check distance
                if (stream.channel == VoiceChannel.Proximity && enableProximityChat)
                {
                    if (!IsInProximityRange(stream.playerId, participantId))
                        continue;

                    // Adjust volume based on distance
                    stream.volume = CalculateProximityVolume(stream.playerId, participantId);
                }

                SendVoiceToClientRpc(participantId, stream);
            }
        }

        [ClientRpc]
        private void SendVoiceToClientRpc(ulong recipientId, VoiceStream stream)
        {
            if (NetworkManager.Singleton.LocalClientId != recipientId) return;

            // Add to jitter buffer if enabled
            if (enableJitterBuffer)
            {
                if (!jitterBuffers.ContainsKey(stream.playerId))
                {
                    jitterBuffers[stream.playerId] = new Queue<byte[]>();
                }
                jitterBuffers[stream.playerId].Enqueue(stream.audioData);
            }
            else
            {
                PlayAudioData(stream);
            }
        }

        private void ProcessJitterBuffers()
        {
            foreach (var kvp in jitterBuffers)
            {
                var playerId = kvp.Key;
                var buffer = kvp.Value;

                if (buffer.Count > 3) // Maintain small buffer
                {
                    var audioData = buffer.Dequeue();

                    var stream = new VoiceStream
                    {
                        playerId = playerId,
                        audioData = audioData,
                        frequency = sampleRate,
                        channels = 1,
                        volume = 1f
                    };

                    PlayAudioData(stream);
                }
            }
        }

        private void PlayAudioData(VoiceStream stream)
        {
            if (!playerAudioSources.ContainsKey(stream.playerId))
            {
                CreateAudioSourceForPlayer(stream.playerId);
            }

            var audioSource = playerAudioSources[stream.playerId];

            // Convert bytes back to floats
            float[] samples = ConvertBytesToFloats(stream.audioData);

            // Create audio clip
            AudioClip clip = AudioClip.Create($"Voice_{stream.playerId}", samples.Length, stream.channels, stream.frequency, false);
            clip.SetData(samples, 0);

            // Apply settings
            audioSource.volume = stream.volume;

            if (stream.isSpatial && enableSpatialAudio)
            {
                audioSource.spatialBlend = 1f;
                audioSource.transform.position = stream.position;
                audioSource.maxDistance = maxVoiceRange;
                audioSource.rolloffMode = AudioRolloffMode.Linear;
            }
            else
            {
                audioSource.spatialBlend = 0f;
            }

            audioSource.PlayOneShot(clip);
        }

        private void CreateAudioSourceForPlayer(ulong playerId)
        {
            GameObject audioObject = new GameObject($"VoiceAudio_{playerId}");
            audioObject.transform.SetParent(transform);

            AudioSource audioSource = audioObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = enableSpatialAudio ? 1f : 0f;

            playerAudioSources[playerId] = audioSource;
        }

        #endregion

        #region Proximity Chat

        public void UpdatePlayerPosition(ulong playerId, Vector3 position)
        {
            if (!enableProximityChat) return;

            if (!proximityData.ContainsKey(playerId))
            {
                proximityData[playerId] = new ProximityVoiceData { playerId = playerId };
            }

            proximityData[playerId].position = position;
            proximityData[playerId].lastUpdate = DateTime.UtcNow;
            proximityData[playerId].range = proximityRange;
        }

        private void UpdateProximityChat()
        {
            if (!enableProximityChat) return;

            // Clean up stale position data
            var staleEntries = proximityData.Where(kvp =>
                (DateTime.UtcNow - kvp.Value.lastUpdate).TotalSeconds > 5).Select(kvp => kvp.Key).ToList();

            foreach (var playerId in staleEntries)
            {
                proximityData.Remove(playerId);
            }
        }

        private bool IsInProximityRange(ulong speaker, ulong listener)
        {
            if (!proximityData.ContainsKey(speaker) || !proximityData.ContainsKey(listener))
                return false;

            float distance = Vector3.Distance(
                proximityData[speaker].position,
                proximityData[listener].position
            );

            return distance <= proximityRange;
        }

        private float CalculateProximityVolume(ulong speaker, ulong listener)
        {
            if (!proximityData.ContainsKey(speaker) || !proximityData.ContainsKey(listener))
                return 0f;

            float distance = Vector3.Distance(
                proximityData[speaker].position,
                proximityData[listener].position
            );

            if (distance >= proximityRange)
                return 0f;

            // Linear falloff
            return 1f - (distance / proximityRange);
        }

        #endregion

        #region Muting & Moderation

        public void MutePlayer(ulong playerId, float duration = 0)
        {
            MutePlayerServerRpc(playerId, duration);
        }

        [ServerRpc(RequireOwnership = false)]
        private void MutePlayerServerRpc(ulong playerId, float duration)
        {
            if (!playerVoiceData.ContainsKey(playerId))
            {
                playerVoiceData[playerId] = new PlayerVoiceData { playerId = playerId };
            }

            var data = playerVoiceData[playerId];
            data.isMuted = true;

            if (duration > 0)
            {
                data.muteExpiration = DateTime.UtcNow.AddSeconds(duration);
            }
            else
            {
                data.muteExpiration = DateTime.MaxValue;
            }

            OnPlayerMuted?.Invoke(playerId, true);
            NotifyPlayerMutedClientRpc(playerId, true);
        }

        public void UnmutePlayer(ulong playerId)
        {
            UnmutePlayerServerRpc(playerId);
        }

        [ServerRpc(RequireOwnership = false)]
        private void UnmutePlayerServerRpc(ulong playerId)
        {
            if (!playerVoiceData.ContainsKey(playerId)) return;

            var data = playerVoiceData[playerId];
            data.isMuted = false;
            data.muteExpiration = DateTime.MinValue;

            OnPlayerMuted?.Invoke(playerId, false);
            NotifyPlayerMutedClientRpc(playerId, false);
        }

        [ClientRpc]
        private void NotifyPlayerMutedClientRpc(ulong playerId, bool isMuted)
        {
            OnPlayerMuted?.Invoke(playerId, isMuted);
        }

        private void CheckMuteExpiration(ulong playerId)
        {
            if (!playerVoiceData.ContainsKey(playerId)) return;

            var data = playerVoiceData[playerId];

            if (data.isMuted && DateTime.UtcNow >= data.muteExpiration)
            {
                UnmutePlayer(playerId);
            }
        }

        public void LocalMutePlayer(ulong localPlayerId, ulong targetPlayerId)
        {
            if (!playerVoiceData.ContainsKey(localPlayerId))
            {
                playerVoiceData[localPlayerId] = new PlayerVoiceData { playerId = localPlayerId };
            }

            playerVoiceData[localPlayerId].mutedPlayers.Add(targetPlayerId);
        }

        public void LocalUnmutePlayer(ulong localPlayerId, ulong targetPlayerId)
        {
            if (!playerVoiceData.ContainsKey(localPlayerId)) return;
            playerVoiceData[localPlayerId].mutedPlayers.Remove(targetPlayerId);
        }

        #endregion

        #region Settings

        public void UpdateVoiceSettings(ulong playerId, VoiceSettings settings)
        {
            playerSettings[playerId] = settings;
        }

        public VoiceSettings GetVoiceSettings(ulong playerId)
        {
            if (!playerSettings.ContainsKey(playerId))
            {
                playerSettings[playerId] = new VoiceSettings();
            }
            return playerSettings[playerId];
        }

        public void SetInputVolume(ulong playerId, float volume)
        {
            var settings = GetVoiceSettings(playerId);
            settings.inputVolume = Mathf.Clamp01(volume);
        }

        public void SetOutputVolume(ulong playerId, float volume)
        {
            var settings = GetVoiceSettings(playerId);
            settings.outputVolume = Mathf.Clamp01(volume);
        }

        public void SetVoiceQuality(ulong playerId, VoiceQuality quality)
        {
            var settings = GetVoiceSettings(playerId);
            settings.quality = quality;
            ApplyQualitySettings(quality);
        }

        private void ApplyQualitySettings(VoiceQuality quality)
        {
            switch (quality)
            {
                case VoiceQuality.Low:
                    sampleRate = 16000;
                    bitrate = 16000;
                    break;
                case VoiceQuality.Medium:
                    sampleRate = 48000;
                    bitrate = 64000;
                    break;
                case VoiceQuality.High:
                    sampleRate = 48000;
                    bitrate = 128000;
                    break;
                case VoiceQuality.Ultra:
                    sampleRate = 96000;
                    bitrate = 256000;
                    break;
            }
        }

        #endregion

        #region Utility

        private float CalculateAverageVolume(float[] samples)
        {
            float sum = 0f;
            foreach (float sample in samples)
            {
                sum += Mathf.Abs(sample);
            }
            return sum / samples.Length;
        }

        private byte[] ConvertFloatsToBytes(float[] floats)
        {
            byte[] bytes = new byte[floats.Length * 2];

            for (int i = 0; i < floats.Length; i++)
            {
                short value = (short)(floats[i] * short.MaxValue);
                bytes[i * 2] = (byte)(value & 0xFF);
                bytes[i * 2 + 1] = (byte)((value >> 8) & 0xFF);
            }

            return bytes;
        }

        private float[] ConvertBytesToFloats(byte[] bytes)
        {
            float[] floats = new float[bytes.Length / 2];

            for (int i = 0; i < floats.Length; i++)
            {
                short value = (short)(bytes[i * 2] | (bytes[i * 2 + 1] << 8));
                floats[i] = value / (float)short.MaxValue;
            }

            return floats;
        }

        public bool IsPlayerMuted(ulong playerId)
        {
            if (!playerVoiceData.ContainsKey(playerId)) return false;
            return playerVoiceData[playerId].isMuted;
        }

        public bool IsPlayerTalking(ulong playerId)
        {
            if (!playerVoiceData.ContainsKey(playerId)) return false;
            return playerVoiceData[playerId].micState == MicrophoneState.Active;
        }

        public List<ulong> GetChannelParticipants(VoiceChannel channel)
        {
            if (!voiceChannels.ContainsKey(channel)) return new List<ulong>();
            return voiceChannels[channel].activeParticipants.ToList();
        }

        #endregion

        #region Cleanup

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            if (isRecording)
            {
                StopRecording();
            }

            foreach (var audioSource in playerAudioSources.Values)
            {
                if (audioSource != null)
                {
                    Destroy(audioSource.gameObject);
                }
            }
            playerAudioSources.Clear();
        }

        #endregion
    }
}
