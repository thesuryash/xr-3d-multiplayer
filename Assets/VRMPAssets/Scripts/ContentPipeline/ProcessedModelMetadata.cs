using UnityEngine;

namespace XRMultiplayer.ContentPipeline
{
    public class ProcessedModelMetadata : MonoBehaviour
    {
        [SerializeField] string m_SourceAssetGuid;
        [SerializeField] string m_SourceAssetPath;
        [SerializeField] string m_SourceFileExtension;
        [SerializeField] string m_ContentHash;
        [SerializeField] int m_TriangleCount;
        [SerializeField] int m_MaxTextureDimension;
        [SerializeField] int m_EstimatedTextureMemoryMb;
        [SerializeField] ModerationState m_ModerationState;
        [SerializeField] string m_RejectionReason;

        public string SourceAssetGuid { get => m_SourceAssetGuid; set => m_SourceAssetGuid = value; }
        public string SourceAssetPath { get => m_SourceAssetPath; set => m_SourceAssetPath = value; }
        public string SourceFileExtension { get => m_SourceFileExtension; set => m_SourceFileExtension = value; }
        public string ContentHash { get => m_ContentHash; set => m_ContentHash = value; }
        public int TriangleCount { get => m_TriangleCount; set => m_TriangleCount = value; }
        public int MaxTextureDimension { get => m_MaxTextureDimension; set => m_MaxTextureDimension = value; }
        public int EstimatedTextureMemoryMb { get => m_EstimatedTextureMemoryMb; set => m_EstimatedTextureMemoryMb = value; }
        public ModerationState CurrentModerationState { get => m_ModerationState; set => m_ModerationState = value; }
        public string RejectionReason { get => m_RejectionReason; set => m_RejectionReason = value; }
    }

    public enum ModerationState
    {
        Approved = 0,
        PendingHostApproval = 1,
        Rejected = 2,
    }
}
