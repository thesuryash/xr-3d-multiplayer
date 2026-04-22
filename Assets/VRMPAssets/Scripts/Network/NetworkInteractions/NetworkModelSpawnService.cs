using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace XRMultiplayer
{
    public class NetworkModelSpawnService : NetworkBehaviour
    {
        [SerializeField] int m_MaxActiveObjects = 30;
        [SerializeField] int m_MaxPerPrefab = 8;

        readonly List<NetworkObject> m_Spawned = new List<NetworkObject>();
        readonly Dictionary<int, int> m_SpawnCountByPrefab = new Dictionary<int, int>();

        public bool TrySpawn(NetworkedModelItem modelPrefab, Vector3 position, Quaternion rotation, out NetworkedModelItem item)
        {
            item = null;
            if (!IsServer || modelPrefab == null)
                return false;

            if (m_Spawned.Count >= m_MaxActiveObjects)
                return false;

            int prefabId = modelPrefab.gameObject.GetInstanceID();
            int count = m_SpawnCountByPrefab.TryGetValue(prefabId, out var current) ? current : 0;
            if (count >= m_MaxPerPrefab)
                return false;

            item = Instantiate(modelPrefab, position, rotation);
            if (!item.ValidateContract())
            {
                Destroy(item.gameObject);
                item = null;
                return false;
            }

            item.networkObject.Spawn(true);
            TrackSpawn(item.networkObject, prefabId);
            return true;
        }

        void TrackSpawn(NetworkObject networkObject, int prefabId)
        {
            m_Spawned.Add(networkObject);
            if (!m_SpawnCountByPrefab.ContainsKey(prefabId))
                m_SpawnCountByPrefab[prefabId] = 0;
            m_SpawnCountByPrefab[prefabId]++;

            var cleanup = networkObject.gameObject.AddComponent<NetworkModelSpawnCleanup>();
            cleanup.Initialize(this, prefabId);
        }

        internal void NotifyDespawn(NetworkObject networkObject, int prefabId)
        {
            m_Spawned.Remove(networkObject);
            if (!m_SpawnCountByPrefab.ContainsKey(prefabId))
                return;

            m_SpawnCountByPrefab[prefabId] = Mathf.Max(0, m_SpawnCountByPrefab[prefabId] - 1);
        }

        public void DespawnAllUnheld()
        {
            if (!IsServer)
                return;

            for (int i = m_Spawned.Count - 1; i >= 0; i--)
            {
                NetworkObject netObj = m_Spawned[i];
                if (netObj == null || !netObj.IsSpawned)
                    continue;

                var interactable = netObj.GetComponent<NetworkBaseInteractable>();
                if (interactable != null && interactable.isInteracting)
                    continue;

                netObj.Despawn(true);
            }
        }
    }

    public class NetworkModelSpawnCleanup : MonoBehaviour
    {
        NetworkModelSpawnService m_Service;
        int m_PrefabId;
        NetworkObject m_NetworkObject;

        public void Initialize(NetworkModelSpawnService service, int prefabId)
        {
            m_Service = service;
            m_PrefabId = prefabId;
            m_NetworkObject = GetComponent<NetworkObject>();
        }

        void OnDestroy()
        {
            if (m_Service != null && m_NetworkObject != null)
                m_Service.NotifyDespawn(m_NetworkObject, m_PrefabId);
        }
    }
}
