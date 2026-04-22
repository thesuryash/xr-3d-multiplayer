# Multiplayer Milestone Quality Gates

This document defines promotion gates for each delivery milestone and binds them to reproducible multiplayer test scenarios.

## Common Definitions

- **Latency envelope**: end-to-end interaction latency measured between actor action and remote observer confirmation.
- **Jitter envelope**: variation (p95-p50) in one-way packet delay during active sessions.
- **Crash-free sessions**: percentage of sessions with no app crash, hard disconnect, or unrecoverable simulation fault.
- **Session stability**: percentage of sessions reaching scenario completion without forced room restart.
- **Blocker bug**: defect that prevents milestone outcomes from being safely validated by target users.

## Milestone Targets

| Milestone | Max room size target | Latency/jitter envelope | Crash-free / stability goals | Blocker bug criteria for promotion |
|---|---:|---|---|---|
| **MVP (internal)** | **8 concurrent users** (6 active interactors + 2 observers) | **p50 ≤ 90 ms**, **p95 ≤ 180 ms** action replication; jitter **p95-p50 ≤ 35 ms** | **Crash-free ≥ 97.0%**, **session stability ≥ 95.0%** over 200 internal sessions | Any P0/P1 in connection flow, avatar spawn/sync, prefab ownership conflicts, grab/throw determinism, or reset failures in >1% runs |
| **Beta (external small group)** | **16 concurrent users** (12 active + 4 observers) | **p50 ≤ 80 ms**, **p95 ≤ 150 ms**; jitter **p95-p50 ≤ 25 ms** | **Crash-free ≥ 99.0%**, **session stability ≥ 97.5%** over 1,000 sessions | Any exploit or moderation bypass, realistic avatar desync >2 sec, ingestion gate false-negative on prohibited model classes, or interaction state divergence >0.5% runs |
| **Launch** | **32 concurrent users** baseline (configurable tiers 16/24/32) | **p50 ≤ 70 ms**, **p95 ≤ 120 ms**; jitter **p95-p50 ≤ 20 ms** | **Crash-free ≥ 99.5%**, **session stability ≥ 99.0%** over 10,000 sessions | Any security/privacy breach, revenue-impacting onboarding break, crash cluster >0.25% daily sessions, room scaling regression, or unresolved Sev-1 analytics/monitoring blind spots |

## Reproducible Multiplayer Test Scenarios

Each promotion requires all scenarios to pass in two environments:
1. **Lab LAN/Wi‑Fi mixed topology** (best-case consistency)
2. **Network-shaped WAN profile** (realistic impairment)

Use deterministic bot + human hybrid playback where possible and save raw traces per run.

### Scenario S1: Cold Connection and Room Join Flow

**Purpose:** Validate login/session bootstrap, room discovery/join, and rejoin resilience.

**Setup**
- Start clean clients with no cached room state.
- Simulate target room size for milestone.
- Apply network profile: 0/40/80 ms base RTT variants.

**Steps**
1. Create room from host client.
2. Join all clients in 10-second stagger.
3. Force-drop 20% of clients for 15 seconds, then rejoin.
4. Verify identity persistence and world-state continuity.

**Pass criteria by gate**
- Join success rate ≥ 99% (MVP), 99.5% (Beta), 99.8% (Launch).
- Rejoin recovery time p95 ≤ 8 s (MVP), 5 s (Beta), 3 s (Launch).
- No orphaned avatars or duplicate authority assignment.

### Scenario S2: Avatar Spawn, Sync, and Presence Consistency

**Purpose:** Confirm basic (MVP) and realistic-provider (Beta+) avatar lifecycle and remote fidelity.

**Setup**
- Spawn all users in shared lobby with synchronized clock markers.
- For Beta/Launch, run realistic avatar provider auth/token refresh path.

**Steps**
1. Simultaneous spawn event for all participants.
2. Execute scripted locomotion path for 5 minutes.
3. Toggle mute/unmute, hand gesture states, and emote state changes.
4. Force provider token rotation mid-session (Beta/Launch).

**Pass criteria by gate**
- Spawn completion ≤ 3 s p95 (MVP), 2 s (Beta), 1.5 s (Launch).
- Remote pose drift ≤ 20 cm p95 (MVP), 12 cm (Beta), 8 cm (Launch).
- Provider fallback to default avatar ≤ 1% sessions (Beta/Launch).

### Scenario S3: Networked Prefab Ownership + Grab/Throw/Reset Reliability

**Purpose:** Validate 10 model prefabs (MVP) and continuing content updates without state corruption.

**Setup**
- Load canonical prefab pack with IDs and expected physics parameters.
- Instrument ownership transfer, impulse vectors, and reset checksum.

**Steps**
1. Concurrently spawn all prefabs.
2. Run pairwise contention: two users attempt grab within 100 ms window.
3. Execute throw trajectories at low/medium/high force.
4. Trigger global and per-object reset every 2 minutes for 20 minutes.

**Pass criteria by gate**
- Ownership conflict resolution deterministic in ≥ 99.5% (MVP), 99.8% (Beta), 99.9% (Launch).
- Throw landing divergence (remote vs authoritative) ≤ 30 cm (MVP), 20 cm (Beta), 12 cm (Launch).
- Reset checksum match across clients ≥ 99.9% runs, no stuck-interactable state.

### Scenario S4: Model Ingestion + Moderation Workflow (Beta/Launch)

**Purpose:** Ensure new content pipeline and governance controls are safe for external users.

**Setup**
- Curated ingestion corpus: clean assets, malformed assets, policy-violating assets.
- Moderator accounts with tiered permissions.

**Steps**
1. Submit mixed model batch via ingestion gate.
2. Validate schema, geometry, and policy classification outputs.
3. Publish approved assets to staging room.
4. Execute moderator actions: mute, kick, content quarantine, appeal restore.

**Pass criteria**
- Ingestion gate precision/recall each ≥ 98% for prohibited classes.
- Time to quarantine abusive content ≤ 30 s p95.
- Moderator audit log completeness 100% for privileged actions.

### Scenario S5: Soak, Scale, and Operational Readiness

**Purpose:** Validate launch-grade reliability, monitoring, and content refresh.

**Setup**
- 4-hour soak (MVP), 12-hour soak (Beta), 24-hour soak (Launch).
- Scheduled content refresh every 2 hours.
- Synthetic faults: service restart, packet burst, regional latency spike.

**Steps**
1. Maintain rooms at 80–100% target occupancy.
2. Execute periodic interaction workloads (movement, grab, throw, voice/chat).
3. Inject one fault class every hour.
4. Confirm monitoring alerts, dashboards, and crash reports are actionable.

**Pass criteria by gate**
- Memory growth trend: no unbounded leak (≤ 5% slope per hour at Launch).
- Alert detection latency ≤ 2 min and MTTR improvement trend over prior milestone.
- Content refresh success ≥ 99.5% with no forced room teardown.

## Promotion Decision Rules

A milestone is promotable only when all conditions are met:
1. All required scenarios pass at least **3 consecutive runs** in both environments.
2. Reliability metrics meet or exceed milestone targets for a full validation window.
3. No open blocker bugs under milestone-specific criteria.
4. Post-run report includes trace IDs, build hash, network profile, and known-risk sign-off.

## Suggested Exit Artifacts

- Test matrix report (scenario × environment × build).
- Defect burndown with blocker/non-blocker classification.
- Performance trend dashboard snapshots (latency, jitter, crash-free, stability).
- Signed go/no-go checklist by engineering + QA + product.
