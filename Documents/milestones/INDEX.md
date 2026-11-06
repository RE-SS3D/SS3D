# Playable milestones

Hub for **what to focus on next** and the **dependency trees** that unlock a playable gate.
Conventions: [Documents/SKILL.md](../SKILL.md) (Milestones section). Domain coverage (designed
vs built) stays in [architecture/INDEX.md](../architecture/INDEX.md); structural risk in
[TECH_DEBT.md](../architecture/TECH_DEBT.md).

## North star

A 3D Space Station 13. Every gate names the [vision](../design/vision.md) pillars it advances; enabler gates name the gameplay gate they serve.

## Current focus

**Target: Playtest 1** — 8 players play about 3 Nuke Ops rounds on the 24/7 server, in a session announced on Discord ([mvp1-nuke-ops.md](mvp1-nuke-ops.md) M6). Two tracks serve it:

**Gameplay** — [mvp1-nuke-ops.md](mvp1-nuke-ops.md), in this order:

1. **M1m** — responsive movement: finish the peaceful drag gait (ramps, blends, strafes and sprint shipped in [#138](https://github.com/henkhooft/SS3D/pull/138))
2. **M0** — in-house test station; Captain, Security, Engineer, Doctor, Assistant with loadouts and access; ops suit identity
3. **M1c** — combat core: C20R, Stechkin, security pistol, M4, stun baton; physical magazines; C4/X4 and minibomb (implemented; multiplayer acceptance pending); sounds; no debug chrome
4. **M3** — nuke loop (thin) with floor and furniture damage
5. **M8 + M9 (thin)** — vacuum kills, suit seal and internals protect, engineers patch hull holes; APC lights matter

M4 (win/lose) and M5 (end screen, restart) shipped: [round-end loop](../archive/architecture/2026-11_round-end-loop.md), [#131](https://github.com/henkhooft/SS3D/pull/131)–[#137](https://github.com/henkhooft/SS3D/pull/137).

**Enabler** — [test-server.md](test-server.md), serves Playtest 1:

- **T3** — exit line: doors for remote clients; admin force-end and teleport disk; Nuke Ops smoke green nightly; 8 clients through a breach without hitching
- **T8** — 24/7 Nuke Ops server, separate from the sandbox; Extended below 4 ready players. Announcing it publicly closes the gate (T5)

**Maintenance** — [map-authoring.md](map-authoring.md): no build work queued; triage tester reports. A6 (a tester-built station) is off the critical path.

After Playtest 1, a **feel pass** ordered by what testers report: [mvp1-nuke-ops.md](mvp1-nuke-ops.md) **Explicitly deferred**.

Not focus: Area-id merge on breach, shuttles, PDA/uplink/Traitor, handcuffs, ranged stuns, cloning/respawn, full radio, server browser and accounts.

## Gates

| Gate | Status | Pillars | Depends on | Doc |
|------|--------|---------|------------|-----|
| Test server — hop-on/off playtest | in-progress | enabler for MVP1 | — (parallel; reuses MVP1 M5 loop) | [test-server.md](test-server.md) |
| Map authoring — tester-built maps | in-progress | enabler for MVP1 (maintenance) | test-server T1 | [map-authoring.md](map-authoring.md) |
| MVP1 — Nuclear Operatives round | in-progress | 2, 4, 6 (1 thin) | — | [mvp1-nuke-ops.md](mvp1-nuke-ops.md) |
| MVP2 — Bare-bones station round | planned | 1, 3, 4, 5 | MVP1 | [mvp2-station-round.md](mvp2-station-round.md) |

### Beyond MVP2

A sketch, so every gate above sits on a path to a recognizable SS13 shift. No docs or slices until MVP2 is in progress.

- **MVP3 — Departments with depth**: chemistry, a cargo economy, robotics, construction
- **MVP4 — The full ensemble**: AI and cyborgs, the antagonist roster and evacuation drama on a MetaStation-scale map with 30+ players

## How to use

1. Open this hub for current focus.
2. Open the active gate doc for the dependency tree and slice status.
3. Follow leaf links into design §§, architecture efforts, or plans for HOW — do not restate them here.
4. On ship, run `update-system-docs` so slice status and this hub’s current focus stay accurate.
