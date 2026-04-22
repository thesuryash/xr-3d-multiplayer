using Unity.Netcode.Components;
using UnityEngine;

namespace XRMultiplayer
{
    [DisallowMultipleComponent]
    public class NetworkModelResetMonitor : MonoBehaviour
    {
        [SerializeField] NetworkedModelItem m_ModelItem;
        [SerializeField] float m_CheckInterval = 0.3f;

        float m_LastCheck;
        NetworkPhysicsInteractable m_Interactable;
        NetworkTransform m_NetworkTransform;

        void Awake()
        {
            if (m_ModelItem == null)
                m_ModelItem = GetComponent<NetworkedModelItem>();

            if (m_ModelItem != null)
                m_Interactable = m_ModelItem.interactionComponent;

            m_NetworkTransform = GetComponent<NetworkTransform>();
        }

        void Update()
        {
            if (m_ModelItem == null || m_Interactable == null || !m_Interactable.IsOwner)
                return;

            if (Time.time < m_LastCheck + m_CheckInterval)
                return;

            m_LastCheck = Time.time;
            if (m_Interactable.isInteracting)
                return;

            Pose resetPose = m_ModelItem.GetResetPose();
            bool outOfBounds = transform.position.y <= m_ModelItem.outOfBoundsY;
            bool tooFar = Vector3.Distance(transform.position, resetPose.position) >= m_ModelItem.outOfBoundsDistance;

            if (outOfBounds || tooFar)
            {
                if (m_NetworkTransform != null)
                    m_NetworkTransform.Teleport(resetPose.position, resetPose.rotation, transform.localScale);
                else
                    transform.SetPositionAndRotation(resetPose.position, resetPose.rotation);

                m_Interactable.ResetObjectPhysics();
            }
        }
    }
}
