using UnityEngine;

namespace XRMultiplayer
{
    public interface IAvatarProvider
    {
        bool Initialize(Transform avatarRoot);
        bool LoadAvatar(string avatarId);
        void ApplyCustomization(string customizationData);
        void Dispose();
    }
}
