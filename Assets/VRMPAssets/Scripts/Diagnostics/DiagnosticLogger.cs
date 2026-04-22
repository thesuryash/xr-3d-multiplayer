using UnityEngine;

namespace XRMultiplayer
{
    public enum DiagnosticLogCategory
    {
        Authentication,
        Lobby,
        OwnershipTransfer,
        Reset,
    }

    public static class DiagnosticLogger
    {
        public static void Log(DiagnosticLogCategory category, string message, int logLevel = 0)
        {
            if (DiagnosticsConfig.Instance == null || !DiagnosticsConfig.Instance.IsCategoryEnabled(category))
                return;

            Utils.Log($"<b>[Diag:{category}]</b> {message}", logLevel);
        }
    }
}
