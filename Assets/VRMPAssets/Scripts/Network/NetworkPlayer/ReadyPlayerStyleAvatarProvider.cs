using UnityEngine;

namespace XRMultiplayer
{
#if READY_PLAYER_ME_AVATARS || META_AVATARS_SDK
    /// <summary>
    /// Provider entry point kept behind compile symbols so gameplay code stays decoupled from vendor SDKs.
    /// Hook this class to your SDK-specific implementation details.
    /// </summary>
    public class ReadyPlayerStyleAvatarProvider : IAvatarProvider
    {
        Transform m_AvatarRoot;

        public bool Initialize(Transform avatarRoot)
        {
            m_AvatarRoot = avatarRoot;
            return m_AvatarRoot != null;
        }

        public bool LoadAvatar(string avatarId)
        {
            // Placeholder path for SDK integration.
            // Return false if no linked account/avatar id is available.
            return !string.IsNullOrWhiteSpace(avatarId) && m_AvatarRoot != null;
        }

        public void ApplyCustomization(string customizationData)
        {
            // SDK-specific customization payload application can be implemented here.
        }

        public void Dispose()
        {
            m_AvatarRoot = null;
        }
    }
#else
    public class ReadyPlayerStyleAvatarProvider : IAvatarProvider
    {
        public bool Initialize(Transform avatarRoot) => false;
        public bool LoadAvatar(string avatarId) => false;
        public void ApplyCustomization(string customizationData) { }
        public void Dispose() { }
    }
#endif
}
