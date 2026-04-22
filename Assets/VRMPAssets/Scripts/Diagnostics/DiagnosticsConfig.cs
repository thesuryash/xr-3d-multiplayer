using UnityEngine;

namespace XRMultiplayer
{
    /// <summary>
    /// Centralized feature toggles for multiplayer diagnostics.
    /// </summary>
    public class DiagnosticsConfig : MonoBehaviour
    {
        public static DiagnosticsConfig Instance { get; private set; }

        [Header("Global")]
        [SerializeField, Tooltip("Only allow diagnostics in editor and development builds.")]
        bool m_DevBuildOnly = true;

        [SerializeField, Tooltip("Master toggle for diagnostics features.")]
        bool m_EnableDiagnostics = true;

        [Header("Overlays")]
        [SerializeField] bool m_ShowNetworkHud = true;
        [SerializeField] bool m_ShowObjectDebugOverlay = true;

        [Header("Logging Categories")]
        [SerializeField] bool m_LogAuth = true;
        [SerializeField] bool m_LogLobby = true;
        [SerializeField] bool m_LogOwnershipTransfer = true;
        [SerializeField] bool m_LogResetEvents = true;

        [Header("Simulation + Timeline")]
        [SerializeField] bool m_EnableLatencySimulation = true;
        [SerializeField] bool m_EnableInteractionTimelineRecording = true;

        public bool DiagnosticsEnabled => m_EnableDiagnostics && (!m_DevBuildOnly || Application.isEditor || Debug.isDebugBuild);
        public bool ShowNetworkHud => DiagnosticsEnabled && m_ShowNetworkHud;
        public bool ShowObjectDebugOverlay => DiagnosticsEnabled && m_ShowObjectDebugOverlay;
        public bool EnableLatencySimulation => DiagnosticsEnabled && m_EnableLatencySimulation;
        public bool EnableInteractionTimelineRecording => DiagnosticsEnabled && m_EnableInteractionTimelineRecording;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public bool IsCategoryEnabled(DiagnosticLogCategory category)
        {
            if (!DiagnosticsEnabled)
                return false;

            return category switch
            {
                DiagnosticLogCategory.Authentication => m_LogAuth,
                DiagnosticLogCategory.Lobby => m_LogLobby,
                DiagnosticLogCategory.OwnershipTransfer => m_LogOwnershipTransfer,
                DiagnosticLogCategory.Reset => m_LogResetEvents,
                _ => false,
            };
        }
    }
}
