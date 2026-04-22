using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace XRMultiplayer
{
    /// <summary>
    /// Shows debug details for the currently selected interactable.
    /// </summary>
    public class SelectedObjectDebugOverlay : MonoBehaviour
    {
        [SerializeField] KeyCode m_ToggleKey = KeyCode.F9;

        bool m_Visible;

        void Start()
        {
            m_Visible = DiagnosticsConfig.Instance != null && DiagnosticsConfig.Instance.ShowObjectDebugOverlay;
        }

        void Update()
        {
            if (Input.GetKeyDown(m_ToggleKey))
                m_Visible = !m_Visible;
        }

        void OnGUI()
        {
            if (!m_Visible || DiagnosticsConfig.Instance == null || !DiagnosticsConfig.Instance.ShowObjectDebugOverlay)
                return;

            var target = FindSelectedInteractable();
            GUILayout.BeginArea(new Rect(12, 205, 420, 170), "Selected Object Debug", GUI.skin.window);

            if (target == null)
            {
                GUILayout.Label("No selected network interactable.");
                GUILayout.EndArea();
                return;
            }

            ulong ownerId = target.IsSpawned ? target.OwnerClientId : ulong.MaxValue;
            Vector3 velocity = target.TryGetComponent(out Rigidbody rb) ? rb.velocity : Vector3.zero;
            string reason = target.lastOwnershipTransferReason;

            GUILayout.Label($"Object: {target.name}");
            GUILayout.Label($"Owner client ID: {(ownerId == ulong.MaxValue ? "N/A" : ownerId.ToString())}");
            GUILayout.Label($"Velocity: {velocity}");
            GUILayout.Label($"Last transfer reason: {reason}");
            GUILayout.EndArea();
        }

        NetworkBaseInteractable FindSelectedInteractable()
        {
            var interactors = FindObjectsByType<XRBaseInteractor>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var interactor in interactors)
            {
                if (!interactor.hasSelection)
                    continue;

                foreach (var selected in interactor.interactablesSelected)
                {
                    if (selected.transform.TryGetComponent(out NetworkBaseInteractable networkInteractable))
                        return networkInteractable;
                }
            }

            return null;
        }
    }
}
