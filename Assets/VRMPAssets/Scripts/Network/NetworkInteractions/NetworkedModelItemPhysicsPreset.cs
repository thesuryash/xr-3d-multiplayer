using UnityEngine;

namespace XRMultiplayer
{
    [CreateAssetMenu(fileName = "NetworkedModelPhysicsPreset", menuName = "XR Multiplayer/Networked Model Physics Preset")]
    public class NetworkedModelItemPhysicsPreset : ScriptableObject
    {
        [Header("Rigidbody")]
        public float mass = 1f;
        public float drag = 0f;
        public float angularDrag = 0.05f;

        [Header("Ownership Handoff")]
        public float minExchangeVelocityMagnitude = 0.025f;
        public float throwVelocityThreshold = 1.25f;

        [Header("Stacking")]
        public bool enableStackLocking = true;
        public float stackUnlockVelocityThreshold = 0.6f;
        public float stackLockDelay = 0.2f;
        public float stackSleepDuration = 0.45f;
        public float stackSleepVelocity = 0.08f;
    }
}
