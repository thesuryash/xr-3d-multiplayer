using UnityEngine;

namespace XRMultiplayer
{
    /// <summary>
    /// Lightweight always-on overlay that shows connection and room status.
    /// </summary>
    public class OpenerStatusHUD : MonoBehaviour
    {
        [SerializeField] Rect m_Rect = new Rect(12, 12, 500, 120);

        OpenerFlowCoordinator m_FlowCoordinator;
        VoiceChatManager m_VoiceChatManager;

        void Awake()
        {
            m_FlowCoordinator = FindFirstObjectByType<OpenerFlowCoordinator>();
            m_VoiceChatManager = FindFirstObjectByType<VoiceChatManager>();
        }

        void OnGUI()
        {
            GUI.Box(m_Rect, "Opener Network Status");

            GUILayout.BeginArea(new Rect(m_Rect.x + 12, m_Rect.y + 24, m_Rect.width - 24, m_Rect.height - 30));
            GUILayout.Label($"Connection: {XRINetworkGameManager.CurrentConnectionState.Value}");
            GUILayout.Label($"Room Code: {(string.IsNullOrEmpty(XRINetworkGameManager.ConnectedRoomCode) ? "--" : XRINetworkGameManager.ConnectedRoomCode)}");
            GUILayout.Label($"Voice: {GetVoiceState()}");
            GUILayout.Label($"Region: {(string.IsNullOrEmpty(XRINetworkGameManager.ConnectedRoomRegion) ? "--" : XRINetworkGameManager.ConnectedRoomRegion)}");

            if (m_FlowCoordinator != null && !string.IsNullOrEmpty(m_FlowCoordinator.LastFailureMessage))
                GUILayout.Label($"Last Error: {m_FlowCoordinator.LastFailureMessage}");

            GUILayout.EndArea();
        }

        string GetVoiceState()
        {
            if (m_VoiceChatManager == null)
                return "Unavailable";

            return m_VoiceChatManager.connectionStatus.Value;
        }
    }
}
