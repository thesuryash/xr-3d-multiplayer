using System;
using System.Reflection;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace XRMultiplayer
{
    public enum LatencySimulationPreset
    {
        Off,
        Low,
        Medium,
        High,
        Extreme,
    }

    /// <summary>
    /// Applies local latency/jitter/drop simulation presets for QA.
    /// </summary>
    public class LatencySimulationController : MonoBehaviour
    {
        [SerializeField] LatencySimulationPreset m_DefaultPreset = LatencySimulationPreset.Off;
        [SerializeField] KeyCode m_CyclePresetKey = KeyCode.F10;

        UnityTransport m_Transport;
        LatencySimulationPreset m_CurrentPreset;

        void Start()
        {
            m_Transport = FindFirstObjectByType<UnityTransport>();
            m_CurrentPreset = m_DefaultPreset;
            ApplyPreset(m_CurrentPreset);
        }

        void Update()
        {
            if (DiagnosticsConfig.Instance == null || !DiagnosticsConfig.Instance.EnableLatencySimulation)
                return;

            if (Input.GetKeyDown(m_CyclePresetKey))
            {
                m_CurrentPreset = (LatencySimulationPreset)(((int)m_CurrentPreset + 1) % Enum.GetValues(typeof(LatencySimulationPreset)).Length);
                ApplyPreset(m_CurrentPreset);
            }
        }

        public void ApplyPreset(LatencySimulationPreset preset)
        {
            if (m_Transport == null)
                return;

            GetParamsForPreset(preset, out int delayMs, out int jitterMs, out int dropRatePercent);

            var method = typeof(UnityTransport).GetMethod("SetDebugSimulatorParameters", BindingFlags.Instance | BindingFlags.Public);
            if (method != null)
            {
                method.Invoke(m_Transport, new object[] { delayMs, jitterMs, dropRatePercent });
                NetworkDiagnosticsService.Instance?.SetSimulatedPacketLoss(dropRatePercent);
                Utils.Log($"[LatencySimulation] Preset: {preset} (delay={delayMs}ms jitter={jitterMs}ms drop={dropRatePercent}%)");
            }
        }

        static void GetParamsForPreset(LatencySimulationPreset preset, out int delayMs, out int jitterMs, out int dropRatePercent)
        {
            switch (preset)
            {
                case LatencySimulationPreset.Low:
                    delayMs = 40;
                    jitterMs = 5;
                    dropRatePercent = 0;
                    break;
                case LatencySimulationPreset.Medium:
                    delayMs = 90;
                    jitterMs = 20;
                    dropRatePercent = 1;
                    break;
                case LatencySimulationPreset.High:
                    delayMs = 150;
                    jitterMs = 35;
                    dropRatePercent = 3;
                    break;
                case LatencySimulationPreset.Extreme:
                    delayMs = 250;
                    jitterMs = 80;
                    dropRatePercent = 8;
                    break;
                default:
                    delayMs = 0;
                    jitterMs = 0;
                    dropRatePercent = 0;
                    break;
            }
        }
    }
}
