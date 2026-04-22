using Unity.Netcode;
using UnityEngine;

namespace XRMultiplayer
{
    /// <summary>
    /// Server-side lifetime management for dynamically spawned objects.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class NetworkObjectLifetime : NetworkBehaviour
    {
        [SerializeField] float m_MaxLifetimeSeconds = 120f;
        float m_SpawnTime;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer)
                m_SpawnTime = Time.unscaledTime;
        }

        void Update()
        {
            if (!IsServer || !IsSpawned || m_MaxLifetimeSeconds <= 0f)
                return;

            if (Time.unscaledTime - m_SpawnTime >= m_MaxLifetimeSeconds)
            {
                NetworkObject.Despawn(true);
            }
        }
    }
}
