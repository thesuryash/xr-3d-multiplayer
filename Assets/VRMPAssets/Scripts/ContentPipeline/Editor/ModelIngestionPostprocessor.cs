#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using XRMultiplayer;

namespace XRMultiplayer.ContentPipeline.Editor
{
    public class ModelIngestionPostprocessor : AssetPostprocessor
    {
        const string k_DefaultPolicyPath = "Assets/VRMPAssets/ContentPipeline/ModelIngestionPolicy.asset";
        const string k_DefaultCachePath = "Assets/VRMPAssets/ContentPipeline/ProcessedAssetCache.asset";
        const string k_ProcessedPrefabFolder = "Assets/VRMPAssets/ContentPipeline/ProcessedPrefabs";

        static readonly HashSet<string> s_PendingAssetPaths = new HashSet<string>();
        static bool s_Enqueued;

        void OnPostprocessModel(GameObject importedRoot)
        {
            if (importedRoot == null || string.IsNullOrEmpty(assetPath))
                return;

            s_PendingAssetPaths.Add(assetPath);
            if (s_Enqueued)
                return;

            s_Enqueued = true;
            EditorApplication.delayCall += ProcessPendingImports;
        }

        static void ProcessPendingImports()
        {
            s_Enqueued = false;

            var policy = EnsurePolicyAsset();
            var cache = EnsureCacheAsset();
            Directory.CreateDirectory(k_ProcessedPrefabFolder);

            foreach (var path in s_PendingAssetPaths)
            {
                ProcessAsset(path, policy, cache);
            }

            s_PendingAssetPaths.Clear();
            EditorUtility.SetDirty(cache);
            AssetDatabase.SaveAssets();
        }

        static void ProcessAsset(string path, ModelIngestionPolicy policy, ProcessedAssetCache cache)
        {
            var extension = Path.GetExtension(path).ToLowerInvariant();
            var guid = AssetDatabase.AssetPathToGUID(path);

            if (!policy.IsExtensionAccepted(extension))
            {
                SaveRejectedEntry(cache, path, guid, $"Extension '{extension}' is not allowed by ingestion policy.");
                return;
            }

            var sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (sourcePrefab == null)
            {
                SaveRejectedEntry(cache, path, guid, "Imported model is unavailable as a GameObject asset.");
                return;
            }

            var workingInstance = PrefabUtility.InstantiatePrefab(sourcePrefab) as GameObject;
            if (workingInstance == null)
            {
                SaveRejectedEntry(cache, path, guid, "Unable to instantiate source model for processing.");
                return;
            }

            try
            {
                var metrics = EvaluateModel(workingInstance);
                var rejectionReason = ValidateMetrics(policy, metrics);
                if (!string.IsNullOrEmpty(rejectionReason))
                {
                    SaveRejectedEntry(cache, path, guid, rejectionReason, metrics);
                    return;
                }

                NormalizeScale(workingInstance.transform, metrics.BoundsLargestDimension, policy.TargetLargestDimensionMeters);
                EnsurePhysicsAndInteraction(workingInstance);

                var metadata = workingInstance.GetComponent<ProcessedModelMetadata>();
                if (metadata == null)
                    metadata = workingInstance.AddComponent<ProcessedModelMetadata>();

                metadata.SourceAssetGuid = guid;
                metadata.SourceAssetPath = path;
                metadata.SourceFileExtension = extension;
                metadata.ContentHash = ComputeStableHash(path, metrics);
                metadata.TriangleCount = metrics.TriangleCount;
                metadata.MaxTextureDimension = metrics.MaxTextureDimension;
                metadata.EstimatedTextureMemoryMb = metrics.EstimatedTextureMemoryMb;
                metadata.CurrentModerationState = ModelModerationGate.ResolveInitialState(policy, guid);
                metadata.RejectionReason = metadata.CurrentModerationState == ModerationState.Rejected ? "Not approved by moderation gate." : string.Empty;

                var prefabName = Path.GetFileNameWithoutExtension(path) + "_NetVariant.prefab";
                var prefabPath = Path.Combine(k_ProcessedPrefabFolder, prefabName).Replace("\\", "/");
                var processedPrefab = PrefabUtility.SaveAsPrefabAsset(workingInstance, prefabPath);

                var entry = new ProcessedAssetEntry
                {
                    SourceAssetGuid = guid,
                    SourceAssetPath = path,
                    SourceHash = metadata.ContentHash,
                    ProcessedPrefabPath = prefabPath,
                    ThumbnailPath = string.Empty,
                    ModerationState = metadata.CurrentModerationState,
                    RejectionReason = metadata.RejectionReason,
                    TriangleCount = metrics.TriangleCount,
                    MaxTextureDimension = metrics.MaxTextureDimension,
                    EstimatedTextureMemoryMb = metrics.EstimatedTextureMemoryMb,
                    LastProcessedUtc = DateTime.UtcNow.ToString("O"),
                };

                cache.Upsert(entry);

                if (processedPrefab == null)
                {
                    SaveRejectedEntry(cache, path, guid, "Failed to write processed prefab variant.", metrics);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(workingInstance);
            }
        }

        static ModelIngestionPolicy EnsurePolicyAsset()
        {
            var policy = AssetDatabase.LoadAssetAtPath<ModelIngestionPolicy>(k_DefaultPolicyPath);
            if (policy != null)
                return policy;

            Directory.CreateDirectory(Path.GetDirectoryName(k_DefaultPolicyPath) ?? "Assets");
            policy = ScriptableObject.CreateInstance<ModelIngestionPolicy>();
            AssetDatabase.CreateAsset(policy, k_DefaultPolicyPath);
            return policy;
        }

        static ProcessedAssetCache EnsureCacheAsset()
        {
            var cache = AssetDatabase.LoadAssetAtPath<ProcessedAssetCache>(k_DefaultCachePath);
            if (cache != null)
                return cache;

            Directory.CreateDirectory(Path.GetDirectoryName(k_DefaultCachePath) ?? "Assets");
            cache = ScriptableObject.CreateInstance<ProcessedAssetCache>();
            AssetDatabase.CreateAsset(cache, k_DefaultCachePath);
            return cache;
        }

        static void SaveRejectedEntry(ProcessedAssetCache cache, string path, string guid, string reason, ModelMetrics metrics = default)
        {
            var entry = new ProcessedAssetEntry
            {
                SourceAssetGuid = guid,
                SourceAssetPath = path,
                SourceHash = string.Empty,
                ProcessedPrefabPath = string.Empty,
                ThumbnailPath = string.Empty,
                ModerationState = ModerationState.Rejected,
                RejectionReason = reason,
                TriangleCount = metrics.TriangleCount,
                MaxTextureDimension = metrics.MaxTextureDimension,
                EstimatedTextureMemoryMb = metrics.EstimatedTextureMemoryMb,
                LastProcessedUtc = DateTime.UtcNow.ToString("O"),
            };

            cache.Upsert(entry);
        }

        static ModelMetrics EvaluateModel(GameObject root)
        {
            var metrics = new ModelMetrics();
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var boundsSet = false;

            for (var i = 0; i < renderers.Length; i++)
            {
                if (!boundsSet)
                {
                    metrics.Bounds = renderers[i].bounds;
                    boundsSet = true;
                }
                else
                {
                    metrics.Bounds.Encapsulate(renderers[i].bounds);
                }

                var materials = renderers[i].sharedMaterials;
                for (var m = 0; m < materials.Length; m++)
                {
                    var material = materials[m];
                    if (material == null)
                        continue;

                    var shader = material.shader;
                    if (shader == null)
                        continue;

                    var propertyCount = shader.GetPropertyCount();
                    for (var p = 0; p < propertyCount; p++)
                    {
                        if (shader.GetPropertyType(p) != UnityEngine.Rendering.ShaderPropertyType.Texture)
                            continue;

                        var texture = material.GetTexture(shader.GetPropertyName(p));
                        if (texture == null)
                            continue;

                        metrics.MaxTextureDimension = Mathf.Max(metrics.MaxTextureDimension, texture.width, texture.height);
                        metrics.EstimatedTextureMemoryBytes += EstimateTextureBytes(texture);
                    }
                }
            }

            var filters = root.GetComponentsInChildren<MeshFilter>(true);
            for (var i = 0; i < filters.Length; i++)
            {
                var mesh = filters[i].sharedMesh;
                if (mesh == null)
                    continue;

                metrics.TriangleCount += mesh.triangles.Length / 3;
            }

            var skinnedMeshes = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (var i = 0; i < skinnedMeshes.Length; i++)
            {
                var mesh = skinnedMeshes[i].sharedMesh;
                if (mesh == null)
                    continue;

                metrics.TriangleCount += mesh.triangles.Length / 3;
            }

            metrics.BoundsLargestDimension = boundsSet ? Mathf.Max(metrics.Bounds.size.x, metrics.Bounds.size.y, metrics.Bounds.size.z) : 1f;
            metrics.EstimatedTextureMemoryMb = Mathf.CeilToInt(metrics.EstimatedTextureMemoryBytes / (1024f * 1024f));
            return metrics;
        }

        static string ValidateMetrics(ModelIngestionPolicy policy, ModelMetrics metrics)
        {
            if (metrics.TriangleCount > policy.MaxTriangleCount)
                return $"Triangle budget exceeded ({metrics.TriangleCount} > {policy.MaxTriangleCount}).";

            if (metrics.MaxTextureDimension > policy.MaxTextureDimension)
                return $"Texture dimension budget exceeded ({metrics.MaxTextureDimension} > {policy.MaxTextureDimension}).";

            if (metrics.EstimatedTextureMemoryMb > policy.MaxEstimatedTextureMemoryMb)
                return $"Texture memory budget exceeded ({metrics.EstimatedTextureMemoryMb} MB > {policy.MaxEstimatedTextureMemoryMb} MB).";

            return string.Empty;
        }

        static void NormalizeScale(Transform root, float currentLargestDimension, float targetLargestDimension)
        {
            if (currentLargestDimension <= 0.0001f)
                return;

            var scaleFactor = targetLargestDimension / currentLargestDimension;
            root.localScale *= scaleFactor;
        }

        static void EnsurePhysicsAndInteraction(GameObject root)
        {
            var colliders = root.GetComponentsInChildren<Collider>(true);
            if (colliders.Length == 0)
            {
                var meshFilter = root.GetComponentInChildren<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    var meshCollider = root.AddComponent<MeshCollider>();
                    meshCollider.sharedMesh = meshFilter.sharedMesh;
                    meshCollider.convex = true;
                }
                else
                {
                    root.AddComponent<BoxCollider>();
                }
            }

            var rigidbody = root.GetComponent<Rigidbody>();
            if (rigidbody == null)
                rigidbody = root.AddComponent<Rigidbody>();

            rigidbody.mass = 1f;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;

            if (root.GetComponent<XRGrabInteractable>() == null)
                root.AddComponent<XRGrabInteractable>();

            if (root.GetComponent<NetworkObject>() == null)
                root.AddComponent<NetworkObject>();

            if (root.GetComponent<ClientNetworkTransform>() == null)
                root.AddComponent<ClientNetworkTransform>();

            if (root.GetComponent<NetworkPhysicsInteractable>() == null)
                root.AddComponent<NetworkPhysicsInteractable>();
        }

        static int EstimateTextureBytes(Texture texture)
        {
            var mipMultiplier = 1.33f;
            var bitsPerPixel = 32f;
            return Mathf.CeilToInt(texture.width * texture.height * (bitsPerPixel / 8f) * mipMultiplier);
        }

        static string ComputeStableHash(string path, ModelMetrics metrics)
        {
            using (var sha = SHA256.Create())
            {
                var payload = $"{path}|{metrics.TriangleCount}|{metrics.MaxTextureDimension}|{metrics.EstimatedTextureMemoryMb}";
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(payload));
                var builder = new StringBuilder(bytes.Length * 2);
                for (var i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }

        struct ModelMetrics
        {
            public int TriangleCount;
            public int MaxTextureDimension;
            public int EstimatedTextureMemoryBytes;
            public int EstimatedTextureMemoryMb;
            public Bounds Bounds;
            public float BoundsLargestDimension;
        }
    }
}
#endif
