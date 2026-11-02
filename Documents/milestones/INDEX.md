# Playable milestones

Hub for **what to focus on next** and the **dependency trees** that unlock a playable gate.
Conventions: [Documents/SKILL.md](../SKILL.md) (Milestones section). Domain coverage (designed
vs built) stays in [architecture/INDEX.md](../architecture/INDEX.md); structural risk in
[TECH_DEBT.md](../architecture/TECH_DEBT.md).

## Current focus

Three tracks run in parallel — an **operational** one that makes multiplayer playtests trustworthy, an **authoring** one that lets testers build maps, and the **gameplay** gate:

**Near-term operational push** — [test-server.md](test-server.md):

- **T1 shipped** — Release zips load and host (owner-verified on stable v0.3.x; no outside tester yet)
- **T2** — Zero-setup remote join (stop hardcoding `127.0.0.1`)
- **T3** — Session stability: doors under MP, remote drop/selection, remaining admin wedge tools (round controls + stop-cancels-start shipped), Nuke Ops happy-path smoke, test-station perf floor. Local `validate.sh --full` covers PlayMode/server build/smoke, but **CI smoke hasn't passed since 2026-10-01** (runner offline)
- **T4** — End-game screen + restart (**shared with M5**); latejoin without softlock shipped, including after an earlier round
- **T7** — Sandbox server hosting (loop, `/bug`/F8 and the owner's first-session fixes #111–#118 shipped)

**Authoring track** — [map-authoring.md](map-authoring.md) (expect many gaps: map authoring in a build is largely untested):

- **A1 shipped** — Loadable local build, including packaged `Data/StationTemplates/` (shares test-server T1)
- **A2 shipped** — Scripts → Rooms: painting, metadata, shared toolbar, undo/persistence and APC-facing membership; owner manually accepted in the editor
- **A2.5 shipped** — Owner dogfooded the map editor and the F8 bug report in a release build
- **A3 partial** — Map validation and save warnings implemented; Unity validation/render review pending
- **A7 partial** — Windows/Linux client stable-release updaters implemented; packaged update and backup launch checks pending before public announcement

**Active gameplay gate** — [mvp1-nuke-ops.md](mvp1-nuke-ops.md) (M1 combat + M2 structural/blast API + client atmos VFX Phase 1 + equal-P turf diffusion shipped):

- **M0** — Test station; vault/disk/nuke; loadouts (ammo, ID/access, defuse tools, internals); assignment; ops/crew identity
- **M1p** — Combat feel **partial:** muzzle/holes/SS14 audio/anims/two-hand/`rangeddebug` shipped; still blood, knockdown clarity, sprint, strip debug chrome (aim IK deferred)
- **M3** — Nuke loop + disk pinpoint + breaching + arm announce + examine state
- **M4** — Objectives + win/lose
- **M5** — End-game screen + restart; spectators still see the outcome
- **M7** — Drag body/carrier remainder (crit ragdoll, screen FX, feel SFX shipped)
- **M8** — Armor seal + internals + air alarms that alarm + thin fire response (turf→health exposure + equal-P turf mixing shipped)
- **M9** — Test-map APC lights matter + wall damage readable at range
Not focus: Area-id merge on breach, re-wiring client atmos VFX (already shipped), shuttle-as-ops-gate, full PDA/uplink/Traitor, cloning/respawn, evac shuttle, full radio, server browser / accounts.

The **lobby UITK redesign** ([lobby.md](../design/lobby.md)) shipped 2026-10-31 ([rounds-lobby](../architecture/systems/rounds-lobby.md)); its deferred items (non-Human species, antag opt-in consumer) wait on MVP1 M4/M0.

## Gates

| Gate | Status | Depends on | Doc |
|------|--------|------------|-----|
| Test server — hop-on/off playtest | in-progress | — (parallel; reuses MVP1 M5 loop) | [test-server.md](test-server.md) |
| Map authoring — tester-built maps | in-progress | test-server T1 | [map-authoring.md](map-authoring.md) |
| MVP1 — Nuclear Operatives round | in-progress | — | [mvp1-nuke-ops.md](mvp1-nuke-ops.md) |
| MVP2 — Bare-bones station round | planned | MVP1 | [mvp2-station-round.md](mvp2-station-round.md) |

## How to use

1. Open this hub for current focus.
2. Open the active gate doc for the dependency tree and slice status.
3. Follow leaf links into design §§, architecture efforts, or plans for HOW — do not restate them here.
4. On ship, run `update-system-docs` so slice status and this hub’s current focus stay accurate.
