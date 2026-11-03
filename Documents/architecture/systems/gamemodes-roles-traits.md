> Code paths: Assets/Scripts/SS3D/Systems/Gamemodes/, Assets/Scripts/SS3D/Systems/Roles/, Assets/Scripts/SS3D/Systems/Traits/
> Entry points: GamemodeSubSystem, RoleSubSystem
> Status: partial
> Verified: 5fe7dbb57 — 2026-10-31

# Gamemodes / roles / traits

## Overview

Round objectives, job roles/loadouts, and character traits (e.g. ID permissions). Coordinates with [rounds-lobby](rounds-lobby.md) embark flow.

## Start here

- `Assets/Scripts/SS3D/Systems/Gamemodes/GamemodeSubSystem.cs` — gamemode subsystem
- `Assets/Scripts/SS3D/Systems/Roles/RoleSubSystem.cs` — replicated job slots (`Jobs`), lobby preferences (`JobPreferencesMessage`), assignment, ID/PDA + loadout spawn
- `Assets/Scripts/SS3D/Systems/Roles/JobResolver.cs` — pure round-start (High → Medium → Low, fallback Assistant) and latejoin resolution; `JobResolverTests`
- `Assets/Scripts/SS3D/Systems/Roles/AntagonistCategories.cs` — lobby antagonist opt-in keys (Nuclear Operative only)
- `Assets/Scripts/SS3D/Systems/Roles/SquadDraw.cs` — pure Nuke Ops squad draw; `RoleSubSystem.DrawSquad` runs it before `JobResolver` and calls `RoundSubSystem.FallBackToExtended` on too few opt-ins
- `Assets/Scripts/SS3D/Systems/Gamemodes/Modes/NukeGamemode.cs` — one shared team objective for the squad, resolved at the round-end trigger; `OperativeBriefing` is the spawn briefing
- `Assets/Content/Data/Roles/StartingRoles.asset` — available roles and slot caps (0 = unlimited)
- `GamemodeObjectivePanelView` — legacy uGUI panel hidden (no Tab hotkey; PDA tab later)

Server Settings selects Secret / Extended / Nuclear Emergency. Extended skips objective
initialization; Secret uses the sole existing antagonist mode. The runtime gamemode reference
is server-side (no SyncVar); authorized settings use the rounds subsystem's TargetRpc.
Map validation uses the same Secret → nuclear mapping and requires all offered job markers before countdown ([rounds-lobby](rounds-lobby.md)).
Weighted mode pools remain deferred; see [rounds-lobby](rounds-lobby.md).

## Extension points

- Active gamemode name: `GamemodeSubSystem.CurrentGamemodeName` (used by round-history persistence).
- New job: add a `RoleData` to `StartingRoles.asset` and map its department in `IdAccessSubSystem.DepartmentForRole`.
- New antagonist role: a `RoleData` with `_antagonistCategory` set, listed in `StartingRoles.asset`; it never appears in the job list. `RoleSubSystem.Operatives` / `IsOperative` expose the drawn squad (server).

## Pitfalls

- Operatives spawn on Nuclear Operatives markers (round-robin); everyone else still uses the legacy transform or first marker, so job markers are not routed.
- Objectives must resolve inside `RoundSubSystem.BuildOutcome`: `Nuke.Detonate` ends the round before `NukeDetonateEvent` fires, so an event-driven objective would still be pending when the outcome is fixed.
- Assignment happens at round start (`ResolveRoundStartRoles`, called by `EntitySubSystem.SpawnReadyPlayers`) or at latejoin spawn, not on connect. `TryGetRole` is empty in the lobby.

## Tests

- `validate.sh --steps compile,editmode`: `GamemodeTests` (team objective assignment and resolution), `SquadDrawTests` (squad size, opt-ins only, Extended fallback, latejoiners never drawn), `RoundEndLoopTests` (objective results on the outcome).
- No scenario category yet. The operative spawn (marker, kit, briefing, team objective) and both Nuke Ops outcomes become one tier-2 scenario fixture with a nuke, disk and operative marker in [2026-11_round-end-loop](../2026-11_round-end-loop.md) P3.

## Depends on / Used by

- **Depends on:** [rounds-lobby](rounds-lobby.md), [entities](entities.md)
- **Used by:** [persistence](persistence.md) (round-end history entry)

## Related docs

- Known design deltas: [REVIEW_2026-10 — Known delta](../../design/REVIEW_2026-10.md#known-delta-revised-design-vs-code) — rows: antagonist-content, objectives (crew win, legacy objectives, briefing), round-config (opt-in minimum)
- [INDEX.md](../INDEX.md)
