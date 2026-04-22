using System;
using System.Collections.Generic;
using UnityEngine;

namespace XRMultiplayer.ContentPipeline
{
    [CreateAssetMenu(fileName = "ProcessedAssetCache", menuName = "XR Multiplayer/Content Pipeline/Processed Asset Cache")]
    public class ProcessedAssetCache : ScriptableObject
    {
        [SerializeField] List<ProcessedAssetEntry> m_Entries = new List<ProcessedAssetEntry>();

        public IReadOnlyList<ProcessedAssetEntry> Entries => m_Entries;

        public ProcessedAssetEntry FindBySourceGuid(string sourceGuid)
        {
            for (var i = 0; i < m_Entries.Count; i++)
            {
                if (m_Entries[i].SourceAssetGuid == sourceGuid)
                    return m_Entries[i];
            }

            return null;
        }

        public void Upsert(ProcessedAssetEntry entry)
        {
            for (var i = 0; i < m_Entries.Count; i++)
            {
                if (m_Entries[i].SourceAssetGuid == entry.SourceAssetGuid)
                {
                    m_Entries[i] = entry;
                    return;
                }
            }

            m_Entries.Add(entry);
        }
    }

    [Serializable]
    public class ProcessedAssetEntry
    {
        public string SourceAssetGuid;
        public string SourceAssetPath;
        public string SourceHash;
        public string ProcessedPrefabPath;
        public string ThumbnailPath;
        public ModerationState ModerationState;
        [TextArea] public string RejectionReason;
        public int TriangleCount;
        public int MaxTextureDimension;
        public int EstimatedTextureMemoryMb;
        public string LastProcessedUtc;
    }
}
