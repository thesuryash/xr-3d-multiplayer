using Unity.Netcode;
using UnityEngine;

namespace XRMultiplayer
{
    /// <summary>
    /// Provides host/client-safe entry spawn points that are distinct from mini game start zones.
    /// </summary>
    public class OpenerSpawnSystem : MonoBehaviour
    {
        [Header("Player Entry Spawns")]
        [SerializeField] Transform m_HostEntryPoint;
        [SerializeField] Transform[] m_ClientEntryPoints;

        [Header("Mini Game Start Zones (separate from entry)")]
        [SerializeField] Transform[] m_MiniGameStartZones;

        [Header("Local Rig Root")]
        [SerializeField, Tooltip("Optional player rig root; falls back to Camera.main root if not provided.")]
        Transform m_LocalRigRoot;

        bool m_HasPlacedPlayer;

        void OnEnable()
        {
            XRINetworkGameManager.Connected.Subscribe(OnConnectionStateUpdated);
        }

        void OnDisable()
        {
            XRINetworkGameManager.Connected.Unsubscribe(OnConnectionStateUpdated);
        }

        void OnConnectionStateUpdated(bool connected)
        {
            if (!connected || m_HasPlacedPlayer)
                return;

            PlaceLocalPlayerAtEntryPoint();
            m_HasPlacedPlayer = true;
        }

        public void PlaceLocalPlayerAtEntryPoint()
        {
            Transform destination = GetEntryPointForRole();
            if (destination == null)
                return;

            Transform rigRoot = GetRigRoot();
            if (rigRoot == null)
                return;

            rigRoot.SetPositionAndRotation(destination.position, destination.rotation);
        }

        Transform GetRigRoot()
        {
            if (m_LocalRigRoot != null)
                return m_LocalRigRoot;

            if (Camera.main != null)
                return Camera.main.transform.root;

            return null;
        }

        Transform GetEntryPointForRole()
        {
            if (NetworkManager.Singleton == null)
                return m_HostEntryPoint;

            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
                return m_HostEntryPoint;

            if (m_ClientEntryPoints == null || m_ClientEntryPoints.Length == 0)
                return m_HostEntryPoint;

            int index = Mathf.Abs((int)NetworkManager.Singleton.LocalClientId) % m_ClientEntryPoints.Length;
            return m_ClientEntryPoints[index];
        }

        public Transform[] GetMiniGameStartZones()
        {
            return m_MiniGameStartZones;
        }
    }
}
