using UnityEngine;
using Unity.Netcode;

namespace XRMultiplayer
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject), typeof(Rigidbody), typeof(NetworkPhysicsInteractable))]
    public class NetworkedModelItem : MonoBehaviour
    {
        public enum WeightPreset
        {
            Light,
            Medium,
            Heavy
        }

        [Header("Prefab Contract")]
        public NetworkObject networkObject;
        public Rigidbody rigidbodyComponent;
        public Collider[] colliders;
        public NetworkPhysicsInteractable interactionComponent;

        [Header("Reset Metadata")]
        public Transform resetAnchor;
        public float outOfBoundsY = -10f;
        public float outOfBoundsDistance = 30f;

        [Header("Tuning Presets")]
        public WeightPreset weightPreset = WeightPreset.Medium;
        public NetworkedModelItemPhysicsPreset lightPreset;
        public NetworkedModelItemPhysicsPreset mediumPreset;
        public NetworkedModelItemPhysicsPreset heavyPreset;

        Pose m_DefaultResetPose;

        void Awake()
        {
            m_DefaultResetPose = new Pose(transform.position, transform.rotation);
            ValidateContract();
            ApplyWeightPreset();
        }

        public bool ValidateContract()
        {
            bool valid = true;

            if (networkObject == null)
            {
                valid &= TryGetComponent(out networkObject);
            }

            if (rigidbodyComponent == null)
            {
                valid &= TryGetComponent(out rigidbodyComponent);
            }

            if (interactionComponent == null)
            {
                valid &= TryGetComponent(out interactionComponent);
            }

            if (colliders == null || colliders.Length == 0)
            {
                colliders = GetComponentsInChildren<Collider>(true);
            }

            if (colliders == null || colliders.Length == 0)
            {
                valid = false;
            }

            return valid;
        }

        public void ApplyWeightPreset()
        {
            if (interactionComponent == null)
                return;

            interactionComponent.ApplyTuningPreset(GetPreset(weightPreset));
        }

        public NetworkedModelItemPhysicsPreset GetPreset(WeightPreset preset)
        {
            switch (preset)
            {
                case WeightPreset.Light:
                    return lightPreset;
                case WeightPreset.Heavy:
                    return heavyPreset;
                default:
                    return mediumPreset;
            }
        }

        public Pose GetResetPose()
        {
            if (resetAnchor != null)
                return new Pose(resetAnchor.position, resetAnchor.rotation);

            return m_DefaultResetPose;
        }
    }
}
