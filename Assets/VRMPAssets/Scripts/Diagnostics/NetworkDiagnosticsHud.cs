using System.Reflection;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace XRMultiplayer
{
    /// <summary>
    /// Runtime network HUD for RTT, packet loss, ownership churn and object count.
    /// </summary>
    public class NetworkDiagnosticsHud : MonoBehaviour
    {
        [SerializeField] KeyCode m_ToggleKey = KeyCode.F8;
        [SerializeField] float m_RefreshInterval = 0.5f;

        bool m_Visible;
        float m_NextRefreshTime;
        UnityTransport m_Transport;

        void Start()
        {
            m_Visible = DiagnosticsConfig.Instance != null && DiagnosticsConfig.Instance.ShowNetworkHud;
            m_Transport = FindFirstObjectByType<UnityTransport>();
        }

        void Update()
        {
            if (Input.GetKeyDown(m_ToggleKey))
                m_Visible = !m_Visible;

            if (!m_Visible || DiagnosticsConfig.Instance == null || !DiagnosticsConfig.Instance.ShowNetworkHud)
                return;

            if (Time.unscaledTime >= m_NextRefreshTime)
            {
                RefreshRtt();
                m_NextRefreshTime = Time.unscaledTime + m_RefreshInterval;
            }
        }

        void OnGUI()
        {
            if (!m_Visible || DiagnosticsConfig.Instance == null || !DiagnosticsConfig.Instance.ShowNetworkHud)
                return;

            var service = NetworkDiagnosticsService.Instance;
            if (service == null)
                return;

            GUILayout.BeginArea(new Rect(12, 12, 360, 180), "Network Diagnostics", GUI.skin.window);
            GUILayout.Label($"RTT: {service.LastRttMs:0.0} ms");
            GUILayout.Label($"Packet loss: {service.PacketLossPercent:0.0}%");
            GUILayout.Label($"Ownership changes/sec: {service.GetOwnershipChangesPerSecond():0.00}");
            GUILayout.Label($"Spawned object count: {service.GetSpawnedObjectCount()}");
            GUILayout.EndArea();
        }

        void RefreshRtt()
        {
            if (m_Transport == null || NetworkManager.Singleton == null)
                return;

            var method = typeof(UnityTransport).GetMethod("GetCurrentRtt", BindingFlags.Instance | BindingFlags.Public);
            if (method == null)
                return;

            object result = method.Invoke(m_Transport, new object[] { NetworkManager.Singleton.LocalClientId });
            if (result is ulong rtt)
                NetworkDiagnosticsService.Instance?.ReportProbeReceived(rtt);
            else if (result is int rttInt)
                NetworkDiagnosticsService.Instance?.ReportProbeReceived(rttInt);
            else if (result is float rttFloat)
                NetworkDiagnosticsService.Instance?.ReportProbeReceived(rttFloat);
        }
    }
}
