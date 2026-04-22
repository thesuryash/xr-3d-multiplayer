using UnityEngine;

namespace XRMultiplayer
{
    [RequireComponent(typeof(XRINetworkPlayer))]
    public class XRNetworkAvatarCoordinator : MonoBehaviour
    {
        [Header("Avatar Roots")]
        [SerializeField] GameObject[] m_FallbackVisuals;
        [SerializeField] GameObject[] m_ProviderVisuals;
        [SerializeField] Transform m_ProviderRoot;

        XRINetworkPlayer m_Player;
        IAvatarProvider m_Provider;
        bool m_UsingProvider;

        void Awake()
        {
            m_Player = GetComponent<XRINetworkPlayer>();
            m_Player.onSpawnedAll += HandlePlayerSpawned;
            m_Player.onAvatarStateUpdated += HandleAvatarStateUpdated;
        }

        void OnDestroy()
        {
            if (m_Player != null)
            {
                m_Player.onSpawnedAll -= HandlePlayerSpawned;
                m_Player.onAvatarStateUpdated -= HandleAvatarStateUpdated;
            }

            m_Provider?.Dispose();
        }

        void HandlePlayerSpawned()
        {
            InitializeProvider();
            HandleAvatarStateUpdated(m_Player.avatarId, m_Player.avatarCustomization);
        }

        void InitializeProvider()
        {
            m_Provider?.Dispose();
            m_Provider = new ReadyPlayerStyleAvatarProvider();
            m_UsingProvider = m_Provider.Initialize(m_ProviderRoot != null ? m_ProviderRoot : transform);
            ApplyVisualState(m_UsingProvider);
        }

        void HandleAvatarStateUpdated(string avatarId, string customization)
        {
            if (!m_UsingProvider)
            {
                ApplyVisualState(false);
                return;
            }

            bool loaded = m_Provider.LoadAvatar(avatarId);
            if (!loaded)
            {
                // No linked account / invalid identity / provider failure.
                ApplyVisualState(false);
                return;
            }

            m_Provider.ApplyCustomization(customization);
            ApplyVisualState(true);
        }

        void ApplyVisualState(bool providerActive)
        {
            for (int i = 0; i < m_FallbackVisuals.Length; i++)
            {
                if (m_FallbackVisuals[i] != null)
                {
                    m_FallbackVisuals[i].SetActive(!providerActive);
                }
            }

            for (int i = 0; i < m_ProviderVisuals.Length; i++)
            {
                if (m_ProviderVisuals[i] != null)
                {
                    m_ProviderVisuals[i].SetActive(providerActive);
                }
            }
        }
    }
}
