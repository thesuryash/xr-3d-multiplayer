using UnityEngine;

namespace XRMultiplayer
{
    /// <summary>
    /// Coordinates opener scene join/create flow while relying on XRINetworkGameManager + LobbyManager.
    /// </summary>
    [RequireComponent(typeof(XRINetworkGameManager), typeof(LobbyManager))]
    public class OpenerFlowCoordinator : MonoBehaviour
    {
        [SerializeField, Tooltip("If true, attempts quick join once authentication is complete.")]
        bool m_AutoQuickJoinOnAuthenticated = true;

        [SerializeField, Tooltip("Optional custom room name when creating a room from UI.")]
        string m_DefaultRoomName = "Opener Room";

        XRINetworkGameManager m_NetworkGameManager;
        LobbyManager m_LobbyManager;

        public string LastStatusMessage { get; private set; } = "Initializing";
        public string LastFailureMessage { get; private set; } = string.Empty;

        void Awake()
        {
            m_NetworkGameManager = GetComponent<XRINetworkGameManager>();
            m_LobbyManager = GetComponent<LobbyManager>();

            m_LobbyManager.OnLobbyFailed += HandleLobbyFailed;
            m_NetworkGameManager.connectionUpdated += HandleConnectionUpdated;
            m_NetworkGameManager.connectionFailedAction += HandleConnectionFailed;

            XRINetworkGameManager.CurrentConnectionState.Subscribe(OnConnectionStateChanged);
        }

        void OnDestroy()
        {
            if (m_LobbyManager != null)
                m_LobbyManager.OnLobbyFailed -= HandleLobbyFailed;

            if (m_NetworkGameManager != null)
            {
                m_NetworkGameManager.connectionUpdated -= HandleConnectionUpdated;
                m_NetworkGameManager.connectionFailedAction -= HandleConnectionFailed;
            }

            XRINetworkGameManager.CurrentConnectionState.Unsubscribe(OnConnectionStateChanged);
        }

        void OnConnectionStateChanged(XRINetworkGameManager.ConnectionState state)
        {
            LastStatusMessage = $"Connection: {state}";

            if (m_AutoQuickJoinOnAuthenticated && state == XRINetworkGameManager.ConnectionState.Authenticated)
            {
                QuickJoin();
            }
        }

        void HandleConnectionUpdated(string status)
        {
            LastStatusMessage = status;
        }

        void HandleLobbyFailed(string error)
        {
            LastFailureMessage = error;
            LastStatusMessage = $"Lobby failure: {error}";
        }

        void HandleConnectionFailed(string error)
        {
            LastFailureMessage = error;
            LastStatusMessage = $"Connection failure: {error}";
        }

        public void QuickJoin()
        {
            if (XRINetworkGameManager.CurrentConnectionState.Value < XRINetworkGameManager.ConnectionState.Authenticated)
                return;

            LastStatusMessage = "Quick join started";
            m_NetworkGameManager.QuickJoinLobby();
        }

        public void CreateLobby(bool isPrivate = false)
        {
            if (XRINetworkGameManager.CurrentConnectionState.Value < XRINetworkGameManager.ConnectionState.Authenticated)
                return;

            LastStatusMessage = "Create room started";
            m_NetworkGameManager.CreateNewLobby(m_DefaultRoomName, isPrivate);
        }

        public void JoinByCode(string roomCode)
        {
            if (XRINetworkGameManager.CurrentConnectionState.Value < XRINetworkGameManager.ConnectionState.Authenticated)
                return;

            if (string.IsNullOrWhiteSpace(roomCode))
                return;

            LastStatusMessage = $"Joining room {roomCode.ToUpperInvariant()}";
            m_NetworkGameManager.JoinLobbyByCode(roomCode);
        }
    }
}
