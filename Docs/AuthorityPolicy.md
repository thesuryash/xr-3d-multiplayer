# Authority Policy

This project now enforces a centralized multiplayer authority policy through `AuthorityPolicy`.

## Core rules

1. **Server authoritative spawn/despawn + lifetime**
   - Spawns are validated server-side with per-player spawn budgets.
   - Dynamically spawned objects are given `NetworkObjectLifetime` and are despawned by the server when lifetime expires.
   - Scene cleanup/moderation APIs only despawn objects on the server.

2. **Ownership requests are state-gated**
   - Ownership transfer is only granted when the target interactable is spawned and not in blocked interaction states.
   - Per-player held-object limits are checked during ownership acquisition and select events.

3. **Server-side throw + interaction rate validation**
   - Throw impulses are validated on the server.
   - Impulses are clamped to configured max magnitude.
   - Throw requests are rate-limited per player to prevent spam.

4. **Rollback/reset for invalid physics state**
   - Server checks physics states for non-finite transforms/velocities.
   - Extreme velocities are clamped.
   - Escape bounds and NaN/Infinity states trigger object reset.

5. **Per-player limits**
   - Simultaneous held-object cap.
   - Spawn budget over a rolling time window.
   - Throw-rate budget over a rolling time window.

6. **Host moderation controls**
   - Kick a client.
   - Freeze/unfreeze an object by network id.
   - Clear spawned scene objects.

## Integration points

- Ownership transfer path: `NetworkBaseInteractable.OnSelectServerRpc` and `NetworkPhysicsInteractable.RequestOwnershipRpc`.
- Launch/throw path: `NetworkProjectileLauncher.RequestFireServerRpc`.
- Spawn path: `NetworkObjectDispenser.SpawnRandomRpc`.
- Rollback path: `NetworkPhysicsInteractable.FixedUpdate`.
- Moderation path: `XRINetworkGameManager` host moderation RPCs.
