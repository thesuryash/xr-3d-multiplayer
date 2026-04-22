using UnityEngine;

namespace XRMultiplayer
{
    public struct AvatarProfile
    {
        public string avatarId;
        public string customization;
    }

    public static class AvatarProfilePreferences
    {
        const string k_AvatarIdKey = "vrmp.avatar.id";
        const string k_CustomizationKey = "vrmp.avatar.customization";

        public static AvatarProfile Load()
        {
            return new AvatarProfile
            {
                avatarId = PlayerPrefs.GetString(k_AvatarIdKey, string.Empty),
                customization = PlayerPrefs.GetString(k_CustomizationKey, string.Empty)
            };
        }

        public static void Save(string avatarId, string customization)
        {
            PlayerPrefs.SetString(k_AvatarIdKey, avatarId ?? string.Empty);
            PlayerPrefs.SetString(k_CustomizationKey, customization ?? string.Empty);
            PlayerPrefs.Save();
        }
    }
}
