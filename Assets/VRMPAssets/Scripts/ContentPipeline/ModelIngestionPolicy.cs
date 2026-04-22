using System;
using System.Collections.Generic;
using UnityEngine;

namespace XRMultiplayer.ContentPipeline
{
    [CreateAssetMenu(fileName = "ModelIngestionPolicy", menuName = "XR Multiplayer/Content Pipeline/Model Ingestion Policy")]
    public class ModelIngestionPolicy : ScriptableObject
    {
        [Header("Accepted Source Formats")]
        [Tooltip("Lowercase file extensions (include dot), e.g. .fbx, .obj, .glb")]
        [SerializeField] List<string> m_AcceptedExtensions = new List<string> { ".fbx", ".obj", ".gltf", ".glb" };

        [Header("Budget Limits")]
        [SerializeField, Min(1000)] int m_MaxTriangleCount = 100000;
        [SerializeField, Min(256)] int m_MaxTextureDimension = 2048;
        [SerializeField, Min(1)] int m_MaxEstimatedTextureMemoryMb = 64;

        [Header("Normalization")]
        [SerializeField, Min(0.1f)] float m_TargetLargestDimensionMeters = 1.25f;

        [Header("Moderation")]
        [SerializeField] ModerationMode m_ModerationMode = ModerationMode.Allowlist;
        [Tooltip("Used when moderation mode is Allowlist. Matches source asset GUID.")]
        [SerializeField] List<string> m_ApprovedSourceGuids = new List<string>();

        public IReadOnlyList<string> AcceptedExtensions => m_AcceptedExtensions;
        public int MaxTriangleCount => m_MaxTriangleCount;
        public int MaxTextureDimension => m_MaxTextureDimension;
        public int MaxEstimatedTextureMemoryMb => m_MaxEstimatedTextureMemoryMb;
        public float TargetLargestDimensionMeters => m_TargetLargestDimensionMeters;
        public ModerationMode Moderation => m_ModerationMode;

        public bool IsExtensionAccepted(string extension)
        {
            if (string.IsNullOrEmpty(extension))
                return false;

            extension = extension.ToLowerInvariant();
            for (var i = 0; i < m_AcceptedExtensions.Count; i++)
            {
                if (string.Equals(m_AcceptedExtensions[i], extension, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public bool IsGuidAllowlisted(string guid)
        {
            if (string.IsNullOrEmpty(guid))
                return false;

            return m_ApprovedSourceGuids.Contains(guid);
        }

        public enum ModerationMode
        {
            Allowlist = 0,
            HostApproval = 1,
        }
    }
}
