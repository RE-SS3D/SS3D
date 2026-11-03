> Goal: A short bare-bones SS13 station shift — jobs, doors/power/atmos that matter, medical save path, Traitors among the crew, round end via evac
> Status: planned
> Depends on: [mvp1-nuke-ops.md](mvp1-nuke-ops.md)
> Current focus: blocked on MVP1
> Roadmap: Station round — Play a short, bare-bones SS13 shift with jobs and antagonists
> Pillars: 1, 3, 4, 5 — [vision.md](../design/vision.md) §2

# MVP2 — Bare-bones station round

## Playable definition

Done when players can lobby into ranked job prefs, staff a minimal department set, do recognizable work for a short shift, and end the round without relying only on a nuke detonation:

- Jobs with access presets and spawn loadouts (Captain, Sec, Engineer, Doctor, Cargo, Assistant at minimum)
- Station loop: doors/access, power shedding, atmos as a real threat when breached
- Medical: field treatment + defib window (cloning/cryo latejoin as thin as needed)
- Comms: radio channels on top of local speech
- **Traitor** as the antagonist (pillar 3, hidden among the crew); objectives + round-end summary. Extended stays the low-population fallback, not the target
- Security can **arrest**: stun, handcuff, take to the brig
- **Thin evac** as the shared end path: call, countdown, the shuttle arrives (appears docked, no flight), people board, it departs and the round ends

The acceptance scenario (**Done when**) and full slices are written after MVP1's Playtest 1; slices below are a sketch only.

## Dependency tree

High-level only — edges mean “needs.” MVP1 combat/structural/blast **and its round-resolution
spine** (death→spectator, round-end summary/reveal) are assumed available and reused, not rebuilt.

```mermaid
flowchart TB
  mvp2[Play_station_round]
  jobs[Job_slots_loadouts_access]
  lobbyui[Lobby_job_UI]
  station[Doors_power_atmos_felt]
  atmossync[Atmos_client_viz_sync]
  arearecompute[Area_live_recompute]
  med[Medical_shift_thin]
  cloning[Cloning_pod_fabricator]
  comms[Radio_channels]
  traitor[Traitor_or_Extended]
  uplink[PDA_uplink_telecrystals]
  evac[Evac_round_end]
  shuttles[Shuttle_framework]
  mvp1[MVP1_combat_struct_nuke_spine]
  mvp2 --> jobs
  mvp2 --> station
  mvp2 --> med
  mvp2 --> comms
  mvp2 --> traitor
  mvp2 --> evac
  mvp2 --> mvp1
  jobs --> lobbyui
  station --> atmossync
  station --> arearecompute
  med -.-> cloning
  traitor --> objectives[Objectives_PDA_tab]
  traitor --> uplink
  evac --> shuttleThin[Thin_evac_shuttle_appears_departs]
  shuttleThin --> shuttles
```

Hidden blockers this sketch makes explicit (each its own future pass):

- **Jobs need a job-select UI** — now provided by the UITK lobby ([lobby.md](../design/lobby.md);
  [effort](../architecture/systems/rounds-lobby.md) Phase D, legacy uGUI purged in Phase F). What
  remains for S1 is a real roster: only Security and Assistant are authored in `StartingRoles.asset`. Roles/loadouts have a legacy home (`RoleSubSystem`/`RoleLoadout`,
  [gamemodes-roles-traits](../architecture/systems/gamemodes-roles-traits.md)); access presets come
  from [id-access.md](../design/id-access.md) §5.
- **"Atmos as a real threat when breached"** — client atmos VFX Phase 1 is already shipped
  ([2026-07_atmos-client-visualization-sync.md](../architecture/2026-07_atmos-client-visualization-sync.md));
  mechanical bite is MVP1 **M8** (env→health + alarms). Area-id merge on breach stays out (see MVP1
  deferred). MVP2 may still want richer station atmos tooling / late-join atmos Phase 2, not a
  re-wire of client VFX.
- **Cloning is a fabricator, and crafting is a stub.** The medical save path leans on defib (shipped)
  for the *thin* version; cloning/respawn ([death-cloning-respawn.md](../design/death-cloning-respawn.md)
  §5) rides `crafting.md` §3's fabricator pattern, and [crafting](../architecture/systems/crafting.md)
  is purged/awaiting redesign — so keep cloning as thin/deferred as the gate allows (dotted edge).
- **Traitor needs the PDA/uplink stack MVP1 deferred.** Objectives have legacy scaffolding
  ([gamemodes-roles-traits](../architecture/systems/gamemodes-roles-traits.md)), but the uplink tab +
  telecrystals ([antagonist-content.md](../design/antagonist-content.md) §3–§4) are net-new. Traitor is
  the target (pillar 3); Extended is only the low-population fallback.
- **Evac is thin: the shuttle appears docked and departs, with no flight.** Existing shuttle code is outdated
  and needs updating. [shuttles.md](../design/shuttles.md)
  has no architecture/system map yet; keeping evac thin makes it a docked, boardable structure that
  comes and goes rather than a flight system. The evac
  sequence itself is [round-end.md](../design/round-end.md) §3.

## Slices

| Id | Name | Status | Links | Player view |
|----|------|--------|-------|-------------|
| S1 | Job system (bare roster) | pending — job-select UI unblocked (UITK lobby H/M/L/N prefs + one-pass resolution shipped, legacy uGUI purged); roster is still two roles (Security, Assistant) | [lobby.md](../design/lobby.md); [id-access.md](../design/id-access.md) §5; roles ([gamemodes-roles-traits](../architecture/systems/gamemodes-roles-traits.md)) | A real job list to pick from (only Security and Assistant today) |
| S2 | Station loop felt | pending — builds on MVP1 M8 env exposure + alarms; Area-id merge still out | [area.md](../design/area.md); [electricity.md](../design/electricity.md); [atmospherics.md](../design/atmospherics.md); [mvp1-nuke-ops.md](mvp1-nuke-ops.md) M8; id-access doors | The station runs: power, air and doors matter during a shift |
| S3 | Medical shift thin | pending — defib (shipped) is the thin save path; cloning rides crafting rewrite | [health.md](../design/health.md); [death-cloning-respawn.md](../design/death-cloning-respawn.md) §3, §5; [crafting](../architecture/systems/crafting.md) (fabricator); reuses MVP1 death spine | Medical can save people (defibrillators work) |
| S4 | Comms radio | partial — feed + Tab/slash compose + announce SFX + Chat purge shipped; OOC/LOOC/Admin chat shipped in the lobby/Escape shell (no in-round HUD feed yet); headset gating / PDA log / radial still open | [comms.md](../design/comms.md) §6; [2026-07_comms-non-diegetic-feed.md](../archive/architecture/2026-07_comms-non-diegetic-feed.md) | Radio and chat (radio feed and out-of-character chat work; headsets don't gate it yet) |
| S5 | Traitor + objectives + arrest (handcuffs) | pending — Traitor needs the deferred uplink; Extended is the low-population fallback only | [antagonist-content.md](../design/antagonist-content.md) §2–§4; [objectives.md](../design/objectives.md); [round-config.md](../design/round-config.md) | Traitors hide among the crew, with objectives; Security can arrest |
| S6 | Thin evac end path: shuttle appears docked, boards, departs | pending — shuttle code is outdated and needs updating; no flight | [round-end.md](../design/round-end.md) §3; [shuttles.md](../design/shuttles.md) (evac only) | The round can end by evacuation shuttle |
| S7 | Eng / cargo minimum | pending | electricity repair already partial; [cargo.md](../design/cargo.md) thin or mapped supply | Minimal engineering and cargo jobs |
| S8 | MVP2 playable close | pending | depends S1–S7 + MVP1 | Finish line: all of the above, on top of a working Nuke Ops round |

## Explicitly deferred

- AI / cyborgs, virology, chemistry depth (player-facing analyzer/metabolism/machines), full crafting rewrite (S3 cloning stays thin/deferred until crafting is redesigned). Substances **foundation** (networked containers + reactions) may ship earlier as infrastructure — [2026-07_substances-foundation.md](../archive/architecture/2026-07_substances-foundation.md) — without counting as chemistry depth.
- Shuttles beyond ops polish + evac (evac itself is blocked on the unbuilt shuttle framework — S6)
- Full observer/ghost experience beyond the thin MVP1 death→spectator spine ([observer.md](../design/observer.md))
- Persistence of station damage across rounds
- Admin tooling beyond end-round; player accounts
- Lobby visual redesign remains a parallel track — but note S1's job roster **does** need *some* job-select UI, so "not a silent gate" holds only if a minimal path exists

## Maintenance

When child work ships, run `update-system-docs`: bump slice status here and refresh [INDEX.md](INDEX.md) only if MVP2 is the active gate (otherwise leave hub focus on MVP1).
