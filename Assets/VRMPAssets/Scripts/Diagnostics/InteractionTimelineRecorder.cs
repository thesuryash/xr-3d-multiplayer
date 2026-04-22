using System;
using System.Collections.Generic;
using UnityEngine;

namespace XRMultiplayer
{
    [Serializable]
    public struct InteractionTimelineEvent
    {
        public float time;
        public string eventType;
        public string objectName;
        public ulong objectId;
        public ulong clientId;
        public Vector3 position;
        public Vector3 velocity;
        public string metadata;
    }

    /// <summary>
    /// Record/replay hooks for difficult multiplayer interaction sequences.
    /// </summary>
    public class InteractionTimelineRecorder : MonoBehaviour
    {
        public static InteractionTimelineRecorder Instance { get; private set; }

        [SerializeField, Tooltip("Max number of events kept in memory for quick QA replays.")]
        int m_MaxEvents = 1024;

        readonly List<InteractionTimelineEvent> m_Events = new();
        bool m_IsRecording;

        public IReadOnlyList<InteractionTimelineEvent> Events => m_Events;
        public bool IsRecording => m_IsRecording;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            m_IsRecording = DiagnosticsConfig.Instance != null && DiagnosticsConfig.Instance.EnableInteractionTimelineRecording;
        }

        public void SetRecording(bool enabled) => m_IsRecording = enabled;

        public void Clear() => m_Events.Clear();

        public void RecordEvent(string eventType, GameObject target, ulong objectId, ulong clientId, Vector3 velocity, string metadata = "")
        {
            if (!m_IsRecording)
                return;

            if (m_Events.Count >= m_MaxEvents)
                m_Events.RemoveAt(0);

            m_Events.Add(new InteractionTimelineEvent
            {
                time = Time.realtimeSinceStartup,
                eventType = eventType,
                objectName = target != null ? target.name : "<none>",
                objectId = objectId,
                clientId = clientId,
                position = target != null ? target.transform.position : Vector3.zero,
                velocity = velocity,
                metadata = metadata,
            });
        }

        public IEnumerable<InteractionTimelineEvent> Replay(float fromTime = 0f)
        {
            foreach (var timelineEvent in m_Events)
            {
                if (timelineEvent.time >= fromTime)
                    yield return timelineEvent;
            }
        }
    }
}
