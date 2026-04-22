#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace XRMultiplayer.ContentPipeline.Editor
{
    public class ModelIngestionDiagnosticsWindow : EditorWindow
    {
        const string k_DefaultCachePath = "Assets/VRMPAssets/ContentPipeline/ProcessedAssetCache.asset";
        Vector2 m_Scroll;

        [MenuItem("XR Multiplayer/Content Pipeline/Ingestion Diagnostics")]
        static void ShowWindow()
        {
            var window = GetWindow<ModelIngestionDiagnosticsWindow>();
            window.titleContent = new GUIContent("Model Diagnostics");
            window.Show();
        }

        void OnGUI()
        {
            var cache = AssetDatabase.LoadAssetAtPath<ProcessedAssetCache>(k_DefaultCachePath);
            if (cache == null)
            {
                EditorGUILayout.HelpBox("No ProcessedAssetCache found. Import a model first to bootstrap the cache asset.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("Rejected Assets", EditorStyles.boldLabel);
            EditorGUILayout.Space(4f);

            m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll);
            var rejectedCount = 0;

            for (var i = 0; i < cache.Entries.Count; i++)
            {
                var entry = cache.Entries[i];
                if (entry.ModerationState != ModerationState.Rejected)
                    continue;

                rejectedCount++;
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(string.IsNullOrWhiteSpace(entry.SourceAssetPath) ? entry.SourceAssetGuid : entry.SourceAssetPath, EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Reason", string.IsNullOrWhiteSpace(entry.RejectionReason) ? "No explicit reason captured." : entry.RejectionReason);
                EditorGUILayout.LabelField("Triangles", entry.TriangleCount.ToString());
                EditorGUILayout.LabelField("Max Texture", entry.MaxTextureDimension.ToString());
                EditorGUILayout.LabelField("Estimated Texture MB", entry.EstimatedTextureMemoryMb.ToString());
                EditorGUILayout.EndVertical();
            }

            if (rejectedCount == 0)
                EditorGUILayout.HelpBox("No rejected assets recorded.", MessageType.Info);

            EditorGUILayout.EndScrollView();
        }
    }
}
#endif
