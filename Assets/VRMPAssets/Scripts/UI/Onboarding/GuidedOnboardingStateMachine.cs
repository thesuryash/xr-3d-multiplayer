using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using Unity.Netcode;
using Unity.XR.CoreUtils.Bindings.Variables;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace XRMultiplayer
{
    /// <summary>
    /// Guided onboarding flow for first-time users joining multiplayer sessions.
    /// This class owns step progression and emits snapshots for UI widgets.
    /// </summary>
    public class GuidedOnboardingStateMachine : MonoBehaviour
    {
        public enum OnboardingStep
        {
            AuthenticationAndLobby,
            AvatarSetup,
            InteractionTutorial,
            FriendJoin,
            PresenceOverview,
            Complete
        }

        public enum PingQuality
        {
            Unknown,
            Excellent,
            Good,
            Fair,
            Poor
        }

        [Serializable]
        public struct PresenceSnapshot
        {
            public ulong playerId;
            public string playerName;
            public bool avatarLoaded;
            public bool voiceActive;
            public PingQuality pingQuality;
        }

        [Serializable]
        public class ConnectionStatusCard
        {
            public TMP_Text title;
            public TMP_Text subtitle;
            public TMP_Text detail;
            public Button retryButton;
        }

        const string k_OnboardingCompletedPref = "xrmpt_onboarding_completed_v1";

        [Header("Completion")]
        [SerializeField, Tooltip("When enabled, completed users start directly at the final state and can skip tutorial UX.")]
        bool m_SkipForReturningUsers = true;

        [Header("Connection Cards")]
        [SerializeField] ConnectionStatusCard m_AuthenticationCard;
        [SerializeField] ConnectionStatusCard m_LobbyCard;

        [Header("Presence")]
        [SerializeField, Tooltip("How often to refresh presence info such as voice activity and ping quality.")]
        float m_PresencePollInterval = 0.4f;

        [Header("Events")]
        [SerializeField] UnityEvent<OnboardingStep> m_OnStepChanged;
        [SerializeField] UnityEvent<string> m_OnConnectionFailureReason;
        [SerializeField] UnityEvent<string> m_OnRoomCodeShared;
        [SerializeField] UnityEvent<IReadOnlyList<PresenceSnapshot>> m_OnPresenceUpdated;
        [SerializeField] UnityEvent m_OnOnboardingCompleted;

        public OnboardingStep CurrentStep => m_CurrentStep;

        public bool IsComplete => m_CurrentStep == OnboardingStep.Complete;

        OnboardingStep m_CurrentStep;

        bool m_AvatarProviderLoggedIn;
        bool m_AvatarCustomized;
        bool m_AvatarPreviewed;

        bool m_GrabThrowDone;
        bool m_ResetDone;
        bool m_ComfortLocomotionHintsDone;

        bool m_RoomCodeShared;
        bool m_PresenceReviewed;

        string m_LastFailureReason;
        string m_LastRoomCode;

        float m_NextPresencePollTime;

        readonly Dictionary<ulong, PresenceSnapshot> m_PresenceByPlayer = new();
        readonly List<PresenceSnapshot> m_PresenceBuffer = new();

        void Awake()
        {
            WireRetryButton(m_AuthenticationCard, RetryAuthentication);
            WireRetryButton(m_LobbyCard, RetryLobbyConnect);
        }

        void Start()
        {
            XRINetworkGameManager.Instance.connectionUpdated += OnConnectionUpdated;
            XRINetworkGameManager.Instance.connectionFailedAction += OnConnectionFailed;
            XRINetworkGameManager.Instance.playerStateChanged += OnPlayerStateChanged;
            XRINetworkGameManager.CurrentConnectionState.Subscribe(OnConnectionStateChanged);
            XRINetworkGameManager.Connected.Subscribe(OnConnectedToGameSession);

            if (m_SkipForReturningUsers && HasCompletedBefore())
            {
                SetStep(OnboardingStep.Complete);
                return;
            }

            SetStep(OnboardingStep.AuthenticationAndLobby);
            UpdateConnectionCards(XRINetworkGameManager.CurrentConnectionState.Value, string.Empty);
        }

        void OnDestroy()
        {
            if (XRINetworkGameManager.Instance == null)
            {
                return;
            }

            XRINetworkGameManager.Instance.connectionUpdated -= OnConnectionUpdated;
            XRINetworkGameManager.Instance.connectionFailedAction -= OnConnectionFailed;
            XRINetworkGameManager.Instance.playerStateChanged -= OnPlayerStateChanged;
            XRINetworkGameManager.CurrentConnectionState.Unsubscribe(OnConnectionStateChanged);
            XRINetworkGameManager.Connected.Unsubscribe(OnConnectedToGameSession);
        }

        void Update()
        {
            if (Time.time < m_NextPresencePollTime)
            {
                return;
            }

            m_NextPresencePollTime = Time.time + m_PresencePollInterval;
            RefreshPresenceSnapshots();
        }

        public void NotifyAvatarProviderLoggedIn()
        {
            m_AvatarProviderLoggedIn = true;
            TryAdvanceFromAvatarStep();
        }

        public void NotifyAvatarCustomized()
        {
            m_AvatarCustomized = true;
            TryAdvanceFromAvatarStep();
        }

        public void NotifyAvatarPreviewed()
        {
            m_AvatarPreviewed = true;
            TryAdvanceFromAvatarStep();
        }

        public void NotifyGrabThrowLearned()
        {
            m_GrabThrowDone = true;
            TryAdvanceFromTutorialStep();
        }

        public void NotifyResetLearned()
        {
            m_ResetDone = true;
            TryAdvanceFromTutorialStep();
        }

        public void NotifyComfortLocomotionHintsLearned()
        {
            m_ComfortLocomotionHintsDone = true;
            TryAdvanceFromTutorialStep();
        }

        public void CopyRoomCodeToClipboard()
        {
            string roomCode = GetCurrentRoomCode();
            if (string.IsNullOrEmpty(roomCode))
            {
                return;
            }

            GUIUtility.systemCopyBuffer = roomCode;
            m_RoomCodeShared = true;
            m_OnRoomCodeShared?.Invoke(roomCode);
            TryAdvanceFromFriendJoinStep();
        }

        public void ShareRoomCodeText()
        {
            string roomCode = GetCurrentRoomCode();
            if (string.IsNullOrEmpty(roomCode))
            {
                return;
            }

            m_RoomCodeShared = true;
            m_OnRoomCodeShared?.Invoke($"Join my room with code: {roomCode}");
            TryAdvanceFromFriendJoinStep();
        }

        public void QuickRejoinLastRoom()
        {
            if (string.IsNullOrEmpty(m_LastRoomCode) || XRINetworkGameManager.Instance == null)
            {
                return;
            }

            XRINetworkGameManager.Instance.JoinLobbyByCode(m_LastRoomCode);
        }

        public void NotifyPresenceReviewed()
        {
            m_PresenceReviewed = true;
            TryAdvanceFromPresenceStep();
        }

        public void CompleteOnboardingNow()
        {
            MarkCompleted();
            SetStep(OnboardingStep.Complete);
        }

        public void ResetCompletionForTesting()
        {
            PlayerPrefs.DeleteKey(k_OnboardingCompletedPref);
            PlayerPrefs.Save();
        }

        bool HasCompletedBefore()
        {
            return PlayerPrefs.GetInt(k_OnboardingCompletedPref, 0) == 1;
        }

        void MarkCompleted()
        {
            PlayerPrefs.SetInt(k_OnboardingCompletedPref, 1);
            PlayerPrefs.Save();
            m_OnOnboardingCompleted?.Invoke();
        }

        void RetryAuthentication()
        {
            if (XRINetworkGameManager.Instance == null)
            {
                return;
            }

            if (!XRINetworkGameManager.Instance.IsAuthenticated())
            {
                _ = XRINetworkGameManager.Instance.Authenticate();
            }
        }

        void RetryLobbyConnect()
        {
            if (XRINetworkGameManager.Instance == null)
            {
                return;
            }

            XRINetworkGameManager.Instance.QuickJoinLobby();
        }

        void OnConnectionUpdated(string message)
        {
            UpdateConnectionCards(XRINetworkGameManager.CurrentConnectionState.Value, message);
        }

        void OnConnectionFailed(string reason)
        {
            m_LastFailureReason = string.IsNullOrWhiteSpace(reason) ? "Unknown connection error." : reason;
            m_OnConnectionFailureReason?.Invoke(m_LastFailureReason);
            UpdateConnectionCards(XRINetworkGameManager.CurrentConnectionState.Value, m_LastFailureReason);
        }

        void OnConnectionStateChanged(XRINetworkGameManager.ConnectionState state)
        {
            UpdateConnectionCards(state, string.Empty);

            if (state >= XRINetworkGameManager.ConnectionState.Connected && m_CurrentStep == OnboardingStep.AuthenticationAndLobby)
            {
                SetStep(OnboardingStep.AvatarSetup);
            }
        }

        void OnConnectedToGameSession(bool connected)
        {
            if (connected)
            {
                m_LastRoomCode = XRINetworkGameManager.ConnectedRoomCode;
            }
        }

        void TryAdvanceFromAvatarStep()
        {
            if (m_CurrentStep != OnboardingStep.AvatarSetup)
            {
                return;
            }

            if (m_AvatarProviderLoggedIn && m_AvatarCustomized && m_AvatarPreviewed)
            {
                SetStep(OnboardingStep.InteractionTutorial);
            }
        }

        void TryAdvanceFromTutorialStep()
        {
            if (m_CurrentStep != OnboardingStep.InteractionTutorial)
            {
                return;
            }

            if (m_GrabThrowDone && m_ResetDone && m_ComfortLocomotionHintsDone)
            {
                SetStep(OnboardingStep.FriendJoin);
            }
        }

        void TryAdvanceFromFriendJoinStep()
        {
            if (m_CurrentStep == OnboardingStep.FriendJoin && m_RoomCodeShared)
            {
                SetStep(OnboardingStep.PresenceOverview);
            }
        }

        void TryAdvanceFromPresenceStep()
        {
            if (m_CurrentStep == OnboardingStep.PresenceOverview && m_PresenceReviewed)
            {
                MarkCompleted();
                SetStep(OnboardingStep.Complete);
            }
        }

        void SetStep(OnboardingStep step)
        {
            m_CurrentStep = step;
            m_OnStepChanged?.Invoke(step);
        }

        void WireRetryButton(ConnectionStatusCard card, UnityAction callback)
        {
            if (card?.retryButton == null)
            {
                return;
            }

            card.retryButton.onClick.RemoveListener(callback);
            card.retryButton.onClick.AddListener(callback);
        }

        void UpdateConnectionCards(XRINetworkGameManager.ConnectionState state, string message)
        {
            string readableFailure = string.IsNullOrWhiteSpace(m_LastFailureReason) ? "No errors." : m_LastFailureReason;
            string readableMessage = string.IsNullOrWhiteSpace(message) ? "Waiting for update..." : message;

            switch (state)
            {
                case XRINetworkGameManager.ConnectionState.None:
                case XRINetworkGameManager.ConnectionState.Authenticating:
                    SetCardText(m_AuthenticationCard, "Authentication", "Signing in", readableMessage);
                    SetCardText(m_LobbyCard, "Lobby", "Not started", "Authenticate first.");
                    break;
                case XRINetworkGameManager.ConnectionState.Authenticated:
                    SetCardText(m_AuthenticationCard, "Authentication", "Signed in", "Ready.");
                    SetCardText(m_LobbyCard, "Lobby", "Ready to connect", readableFailure);
                    break;
                case XRINetworkGameManager.ConnectionState.Connecting:
                    SetCardText(m_AuthenticationCard, "Authentication", "Signed in", "Ready.");
                    SetCardText(m_LobbyCard, "Lobby", "Connecting", readableMessage);
                    break;
                case XRINetworkGameManager.ConnectionState.Connected:
                    SetCardText(m_AuthenticationCard, "Authentication", "Signed in", "Ready.");
                    SetCardText(m_LobbyCard, "Lobby", "Connected", GetCurrentRoomCodeWithFallback());
                    break;
            }
        }

        static void SetCardText(ConnectionStatusCard card, string title, string subtitle, string detail)
        {
            if (card == null)
            {
                return;
            }

            if (card.title != null)
            {
                card.title.text = title;
            }

            if (card.subtitle != null)
            {
                card.subtitle.text = subtitle;
            }

            if (card.detail != null)
            {
                card.detail.text = detail;
            }
        }

        string GetCurrentRoomCode()
        {
            if (!string.IsNullOrEmpty(XRINetworkGameManager.ConnectedRoomCode))
            {
                m_LastRoomCode = XRINetworkGameManager.ConnectedRoomCode;
            }

            return m_LastRoomCode;
        }

        string GetCurrentRoomCodeWithFallback()
        {
            string roomCode = GetCurrentRoomCode();
            return string.IsNullOrEmpty(roomCode) ? "Connected" : $"Room Code: {roomCode}";
        }

        void OnPlayerStateChanged(ulong playerId, bool connected)
        {
            if (!connected)
            {
                m_PresenceByPlayer.Remove(playerId);
                FlushPresenceEvent();
                return;
            }

            if (XRINetworkGameManager.Instance.GetPlayerByID(playerId, out XRINetworkPlayer player))
            {
                m_PresenceByPlayer[playerId] = BuildSnapshot(playerId, player);
                FlushPresenceEvent();
            }
        }

        void RefreshPresenceSnapshots()
        {
            if (m_PresenceByPlayer.Count == 0)
            {
                return;
            }

            List<ulong> staleKeys = null;
            foreach (var key in m_PresenceByPlayer.Keys)
            {
                if (!XRINetworkGameManager.Instance.GetPlayerByID(key, out XRINetworkPlayer player))
                {
                    staleKeys ??= new List<ulong>();
                    staleKeys.Add(key);
                    continue;
                }

                m_PresenceByPlayer[key] = BuildSnapshot(key, player);
            }

            if (staleKeys != null)
            {
                foreach (ulong stale in staleKeys)
                {
                    m_PresenceByPlayer.Remove(stale);
                }
            }

            FlushPresenceEvent();
        }

        void FlushPresenceEvent()
        {
            m_PresenceBuffer.Clear();
            foreach (var snapshot in m_PresenceByPlayer.Values)
            {
                m_PresenceBuffer.Add(snapshot);
            }

            m_OnPresenceUpdated?.Invoke(m_PresenceBuffer);
        }

        PresenceSnapshot BuildSnapshot(ulong playerId, XRINetworkPlayer player)
        {
            return new PresenceSnapshot
            {
                playerId = playerId,
                playerName = player.playerName,
                avatarLoaded = player.head != null && player.leftHand != null && player.rightHand != null,
                voiceActive = player.playerVoiceAmp > 0.05f,
                pingQuality = ToPingQuality(GetRoundTripTimeMs(playerId))
            };
        }

        static PingQuality ToPingQuality(int rttMs)
        {
            if (rttMs < 0)
            {
                return PingQuality.Unknown;
            }

            if (rttMs <= 60)
            {
                return PingQuality.Excellent;
            }

            if (rttMs <= 110)
            {
                return PingQuality.Good;
            }

            if (rttMs <= 180)
            {
                return PingQuality.Fair;
            }

            return PingQuality.Poor;
        }

        static int GetRoundTripTimeMs(ulong clientId)
        {
            if (NetworkManager.Singleton == null)
            {
                return -1;
            }

            object networkTransport = NetworkManager.Singleton.NetworkConfig?.NetworkTransport;
            if (networkTransport == null)
            {
                return -1;
            }

            // Use reflection to avoid hard dependency on specific transport package APIs.
            MethodInfo getCurrentRtt = networkTransport.GetType().GetMethod("GetCurrentRtt", BindingFlags.Instance | BindingFlags.Public);
            if (getCurrentRtt == null)
            {
                return -1;
            }

            object result = getCurrentRtt.Invoke(networkTransport, new object[] { clientId });
            if (result is ulong rttUlong)
            {
                return (int)rttUlong;
            }

            if (result is uint rttUint)
            {
                return (int)rttUint;
            }

            if (result is int rttInt)
            {
                return rttInt;
            }

            return -1;
        }
    }
}
