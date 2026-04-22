using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace XRMultiplayer
{
    /// <summary>
    /// Centralized authority policy used by server-side interaction, spawn, and throw validation paths.
    /// </summary>
    public static class AuthorityPolicy
    {
        const int k_MaxHeldObjectsPerPlayer = 2;
        const int k_MaxSpawnsPerWindow = 8;
        const float k_SpawnWindowSeconds = 10f;
        const int k_MaxThrowsPerWindow = 6;
        const float k_ThrowWindowSeconds = 2f;
        const float k_MaxThrowImpulseMagnitude = 1500f;
        const float k_MaxLinearVelocity = 50f;
        const float k_MaxAngularVelocity = 80f;
        const float k_EscapeBounds = 250f;

        class RateWindow
        {
            public float StartTime;
            public int Count;
        }

        static readonly Dictionary<ulong, int> s_HeldObjectCounts = new();
        static readonly Dictionary<ulong, RateWindow> s_SpawnWindows = new();
        static readonly Dictionary<ulong, RateWindow> s_ThrowWindows = new();

        public static bool CanAcquireOwnership(NetworkBaseInteractable interactable, ulong requesterClientId, out string reason)
        {
            reason = string.Empty;
            if (interactable == null || !interactable.IsSpawned)
            {
                reason = "Invalid interactable or not spawned.";
                return false;
            }

            if (interactable.isInteracting && !interactable.allowOverrideOwnership)
            {
                reason = "Interactable is currently held and override is disabled.";
                return false;
            }

            if (GetHeldCount(requesterClientId) >= k_MaxHeldObjectsPerPlayer)
            {
                reason = $"Player {requesterClientId} exceeded held object limit ({k_MaxHeldObjectsPerPlayer}).";
                return false;
            }

            return true;
        }

        public static void RegisterSelect(ulong clientId, bool selected)
        {
            int current = GetHeldCount(clientId);
            current += selected ? 1 : -1;
            s_HeldObjectCounts[clientId] = Mathf.Clamp(current, 0, k_MaxHeldObjectsPerPlayer);
        }

        public static bool CanSpawn(ulong clientId, float now, out string reason)
        {
            reason = string.Empty;
            if (!TryConsume(s_SpawnWindows, clientId, k_MaxSpawnsPerWindow, k_SpawnWindowSeconds, now))
            {
                reason = $"Spawn budget exceeded ({k_MaxSpawnsPerWindow} per {k_SpawnWindowSeconds:0.#}s).";
                return false;
            }

            return true;
        }

        public static bool ValidateThrowImpulse(ulong clientId, Vector3 requestedImpulse, float now, out Vector3 clampedImpulse, out string reason)
        {
            reason = string.Empty;
            clampedImpulse = Vector3.ClampMagnitude(requestedImpulse, k_MaxThrowImpulseMagnitude);

            if (!TryConsume(s_ThrowWindows, clientId, k_MaxThrowsPerWindow, k_ThrowWindowSeconds, now))
            {
                reason = $"Throw rate exceeded ({k_MaxThrowsPerWindow} per {k_ThrowWindowSeconds:0.#}s).";
                return false;
            }

            if (!IsFinite(requestedImpulse))
            {
                reason = "Throw impulse contained non-finite values.";
                return false;
            }

            return true;
        }

        public static bool ShouldRollbackState(Transform objectTransform, Rigidbody rigidbody)
        {
            if (objectTransform == null || rigidbody == null)
                return true;

            if (!IsFinite(objectTransform.position) || !IsFinite(objectTransform.rotation) ||
                !IsFinite(rigidbody.velocity) || !IsFinite(rigidbody.angularVelocity))
            {
                return true;
            }

            if (objectTransform.position.magnitude > k_EscapeBounds)
            {
                return true;
            }

            if (rigidbody.velocity.magnitude > k_MaxLinearVelocity)
            {
                rigidbody.velocity = Vector3.ClampMagnitude(rigidbody.velocity, k_MaxLinearVelocity);
            }

            if (rigidbody.angularVelocity.magnitude > k_MaxAngularVelocity)
            {
                rigidbody.angularVelocity = Vector3.ClampMagnitude(rigidbody.angularVelocity, k_MaxAngularVelocity);
            }

            return false;
        }

        static bool TryConsume(Dictionary<ulong, RateWindow> windows, ulong clientId, int maxCount, float durationSeconds, float now)
        {
            if (!windows.TryGetValue(clientId, out var window))
            {
                windows[clientId] = new RateWindow { StartTime = now, Count = 1 };
                return true;
            }

            if (now - window.StartTime > durationSeconds)
            {
                window.StartTime = now;
                window.Count = 1;
                return true;
            }

            if (window.Count >= maxCount)
                return false;

            window.Count++;
            return true;
        }

        static int GetHeldCount(ulong clientId)
        {
            return s_HeldObjectCounts.TryGetValue(clientId, out int count) ? count : 0;
        }

        static bool IsFinite(Vector3 value)
        {
            return float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
        }

        static bool IsFinite(Quaternion value)
        {
            return float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z) && float.IsFinite(value.w);
        }
    }
}
