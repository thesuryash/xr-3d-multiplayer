using System.Text;
using TMPro;
using UnityEngine;

namespace XRMultiplayer.ContentPipeline
{
    public class ModelIngestionDiagnosticsUI : MonoBehaviour
    {
        [SerializeField] ProcessedAssetCache m_Cache;
        [SerializeField] TMP_Text m_OutputText;

        [ContextMenu("Refresh Diagnostics")]
        public void RefreshDiagnostics()
        {
            if (m_OutputText == null)
                return;

            if (m_Cache == null || m_Cache.Entries.Count == 0)
            {
                m_OutputText.text = "No ingested assets yet.";
                return;
            }

            var builder = new StringBuilder();
            var rejectedCount = 0;

            for (var i = 0; i < m_Cache.Entries.Count; i++)
            {
                var entry = m_Cache.Entries[i];
                if (entry.ModerationState != ModerationState.Rejected)
                    continue;

                rejectedCount++;
                builder.Append("• ");
                builder.Append(string.IsNullOrWhiteSpace(entry.SourceAssetPath) ? entry.SourceAssetGuid : entry.SourceAssetPath);
                builder.Append("\n  Reason: ");
                builder.Append(string.IsNullOrWhiteSpace(entry.RejectionReason) ? "No reason provided." : entry.RejectionReason);
                builder.Append("\n");
            }

            if (rejectedCount == 0)
            {
                m_OutputText.text = "No rejected assets. All processed models are currently eligible or pending host approval.";
                return;
            }

            m_OutputText.text = $"Rejected assets: {rejectedCount}\n{builder}";
        }
    }
}
