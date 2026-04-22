using TMPro;
using UnityEngine;

namespace XRMultiplayer
{
    /// <summary>
    /// Simple local UI to edit and persist avatar profile values.
    /// </summary>
    public class LocalAvatarSetupUI : MonoBehaviour
    {
        [SerializeField] TMP_InputField m_AvatarIdInput;
        [SerializeField] TMP_InputField m_CustomizationInput;
        [SerializeField] TMP_Text m_StatusText;

        void OnEnable()
        {
            LoadProfileToFields();
        }

        public void SaveProfileFromFields()
        {
            string avatarId = m_AvatarIdInput != null ? m_AvatarIdInput.text : string.Empty;
            string customization = m_CustomizationInput != null ? m_CustomizationInput.text : string.Empty;
            AvatarProfilePreferences.Save(avatarId, customization);
            SetStatus("Saved local avatar profile.");
        }

        public void LoadProfileToFields()
        {
            AvatarProfile profile = AvatarProfilePreferences.Load();
            if (m_AvatarIdInput != null)
                m_AvatarIdInput.text = profile.avatarId;

            if (m_CustomizationInput != null)
                m_CustomizationInput.text = profile.customization;

            SetStatus("Loaded local avatar profile.");
        }

        public void ApplyProfileToLocalPlayer()
        {
            SaveProfileFromFields();

            if (XRINetworkPlayer.LocalPlayer == null)
            {
                SetStatus("Profile saved. Connect to a room to apply.");
                return;
            }

            XRINetworkPlayer.LocalPlayer.SetLocalAvatarProfile(
                m_AvatarIdInput != null ? m_AvatarIdInput.text : string.Empty,
                m_CustomizationInput != null ? m_CustomizationInput.text : string.Empty);

            SetStatus("Applied avatar profile to network player.");
        }

        void SetStatus(string message)
        {
            if (m_StatusText != null)
                m_StatusText.text = message;
        }
    }
}
