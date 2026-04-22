using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace XRMultiplayer
{
    /// <summary>
    /// Collects runtime diagnostics data used by overlays and QA tooling.
    /// </summary>
    public class NetworkDiagnosticsService : MonoBehaviour
    {
        public static NetworkDiagnosticsService Instance { get; private set; }

        readonly Queue<float> m_OwnershipChangeTimestamps = new();
        readonly Dictionary<ulong, string> m_LastOwnershipReasons = new();

        public float LastRttMs { get; private set; }
        public float PacketLossPercent { get; private set; }
        public float SimulatedPacketLossPercent { get; private set; }
        public int SentProbeCount { get; private set; }
        public int ReceivedProbeCount { get; private set; }

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

        public void ReportProbeSent()
        {
            SentProbeCount++;
            UpdatePacketLoss();
        }

        public void ReportProbeReceived(float rttMs)
        {
            ReceivedProbeCount++;
            LastRttMs = rttMs;
            UpdatePacketLoss();
        }

        public void ReportOwnershipChange(NetworkObject networkObject, ulong oldOwner, ulong newOwner, string reason)
        {
            m_OwnershipChangeTimestamps.Enqueue(Time.unscaledTime);
            m_LastOwnershipReasons[networkObject.NetworkObjectId] = reason;

            DiagnosticLogger.Log(DiagnosticLogCategory.OwnershipTransfer,
                $"{networkObject.name} ({networkObject.NetworkObjectId}) owner {oldOwner} -> {newOwner}. Reason: {reason}");

            if (InteractionTimelineRecorder.Instance != null)
            {
                Vector3 velocity = networkObject.TryGetComponent(out Rigidbody rb) ? rb.velocity : Vector3.zero;
                InteractionTimelineRecorder.Instance.RecordEvent("ownership_transfer", networkObject.gameObject,
                    networkObject.NetworkObjectId, newOwner, velocity, reason);
            }
        }

        public float GetOwnershipChangesPerSecond(float windowSeconds = 10f)
        {
            float threshold = Time.unscaledTime - windowSeconds;
            while (m_OwnershipChangeTimestamps.Count > 0 && m_OwnershipChangeTimestamps.Peek() < threshold)
                m_OwnershipChangeTimestamps.Dequeue();

            return m_OwnershipChangeTimestamps.Count / Mathf.Max(windowSeconds, 0.01f);
        }

        public string GetLastOwnershipReason(ulong networkObjectId)
        {
            if (m_LastOwnershipReasons.TryGetValue(networkObjectId, out var reason))
                return reason;

            return "Unknown";
        }

        public int GetSpawnedObjectCount()
        {
            var manager = NetworkManager.Singleton;
            if (manager == null || manager.SpawnManager == null)
                return 0;

            return manager.SpawnManager.SpawnedObjects.Count;
        }

        void UpdatePacketLoss()
        {
            if (SentProbeCount <= 0)
            {
                PacketLossPercent = SimulatedPacketLossPercent;
                return;
            }

            PacketLossPercent = Mathf.Max(SimulatedPacketLossPercent, Mathf.Clamp01((SentProbeCount - ReceivedProbeCount) / (float)SentProbeCount) * 100f);
        }

        public void SetSimulatedPacketLoss(float packetLossPercent)
        {
            SimulatedPacketLossPercent = Mathf.Clamp(packetLossPercent, 0f, 100f);
            UpdatePacketLoss();
        }
    }
}
