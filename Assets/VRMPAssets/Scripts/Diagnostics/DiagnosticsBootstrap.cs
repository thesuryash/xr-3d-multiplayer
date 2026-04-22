using UnityEngine;

namespace XRMultiplayer
{
    /// <summary>
    /// Creates diagnostics runtime services automatically in editor/dev builds.
    /// </summary>
    public static class DiagnosticsBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Initialize()
        {
            if (!Application.isEditor && !Debug.isDebugBuild)
                return;

            if (Object.FindFirstObjectByType<DiagnosticsConfig>() == null)
            {
                var root = new GameObject("DiagnosticsRuntime");
                root.AddComponent<DiagnosticsConfig>();
                root.AddComponent<NetworkDiagnosticsService>();
                root.AddComponent<InteractionTimelineRecorder>();
                root.AddComponent<SelectedObjectDebugOverlay>();
                root.AddComponent<LatencySimulationController>();
                root.AddComponent<SmokeScenarioChecklist>();
                root.AddComponent<NetworkDiagnosticsHud>();
                Object.DontDestroyOnLoad(root);
            }
        }
    }
}
