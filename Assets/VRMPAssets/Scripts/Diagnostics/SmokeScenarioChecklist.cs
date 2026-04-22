using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace XRMultiplayer
{
    /// <summary>
    /// Automated smoke checklist runner for join -> spawn -> interact -> disconnect.
    /// </summary>
    public class SmokeScenarioChecklist : MonoBehaviour
    {
        [SerializeField] NetworkObjectDispenser m_Dispenser;
        [SerializeField] float m_TimeoutPerStep = 15f;

        [ContextMenu("Run Smoke Checklist")]
        public void RunSmokeChecklist()
        {
            StopAllCoroutines();
            StartCoroutine(RunChecklistRoutine());
        }

        IEnumerator RunChecklistRoutine()
        {
            Utils.Log("[Smoke] Starting smoke scenario: join -> spawn -> interact -> disconnect");

            yield return WaitForCondition("join", () => XRINetworkGameManager.Connected.Value, m_TimeoutPerStep);

            int spawnedBefore = NetworkDiagnosticsService.Instance != null
                ? NetworkDiagnosticsService.Instance.GetSpawnedObjectCount()
                : 0;

            if (m_Dispenser != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                m_Dispenser.EnablePanel(0);
            }

            yield return WaitForCondition("spawn", () =>
            {
                if (NetworkDiagnosticsService.Instance == null)
                    return false;
                return NetworkDiagnosticsService.Instance.GetSpawnedObjectCount() > spawnedBefore;
            }, m_TimeoutPerStep);

            yield return WaitForCondition("interact", () =>
            {
                var interactables = FindObjectsByType<NetworkBaseInteractable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                foreach (var interactable in interactables)
                {
                    if (interactable.isInteracting)
                        return true;
                }

                return false;
            }, m_TimeoutPerStep);

            XRINetworkGameManager.Instance?.Disconnect();
            yield return WaitForCondition("disconnect", () => !XRINetworkGameManager.Connected.Value, m_TimeoutPerStep);

            Utils.Log("[Smoke] Checklist complete.");
        }

        IEnumerator WaitForCondition(string step, System.Func<bool> predicate, float timeout)
        {
            float startTime = Time.time;
            while (Time.time - startTime < timeout)
            {
                if (predicate())
                {
                    Utils.Log($"[Smoke] PASS: {step}");
                    yield break;
                }

                yield return null;
            }

            Utils.LogWarning($"[Smoke] FAIL/TIMEOUT: {step}");
        }
    }
}
