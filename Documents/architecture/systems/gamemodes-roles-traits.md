> Code paths: Assets/Scripts/SS3D/Systems/Gamemodes/, Assets/Scripts/SS3D/Systems/Roles/, Assets/Scripts/SS3D/Systems/Traits/
> Entry points: GamemodeSubSystem, RoleSubSystem
> Status: partial
> Verified: ae1a54ed1 — 2026-11-05

# Gamemodes / roles / traits

## Overview

Round objectives, job roles/loadouts, and character traits (e.g. ID permissions). Coordinates with [rounds-lobby](rounds-lobby.md) embark flow.

## Start here

- `Assets/Scripts/SS3D/Systems/Gamemodes/GamemodeSubSystem.cs` — gamemode subsystem
- `Assets/Scripts/SS3D/Systems/Roles/RoleSubSystem.cs` — replicated job slots (`Jobs`), lobby preferences (`JobPreferencesMessage`), assignment, ID/PDA + loadout spawn
- `Assets/Scripts/SS3D/Systems/Roles/JobResolver.cs` — pure round-start (High → Medium → Low, fallback Assistant) and latejoin resolution; `JobResolverTests`
- `Assets/Scripts/SS3D/Systems/Roles/AntagonistCategories.cs` — lobby antagonist opt-in keys (Nuclear Operative only). `IsOptedIn`: on by default, so a player who never sent preferences is in the draw (round-end loop Decision 22); the lobby list starts ticked
- `Assets/Scripts/SS3D/Systems/Roles/SquadDraw.cs` — pure Nuke Ops squad draw; `RoleSubSystem.DrawSquad` runs it before `JobResolver` and calls `RoundSubSystem.FallBackToExtended` on too few opt-ins, or when Secret has fewer than `MinimumReadyForNukeOps` ready players (`HasEnoughReady`, Decision 23, default 4; Nuclear Emergency is not gated)
- `Assets/Scripts/SS3D/Systems/Gamemodes/Modes/NukeGamemode.cs` — one shared team objective for the squad, resolved at the round-end trigger; `OperativeBriefing` is the spawn briefing, sent by `GamemodeSubSystem` to each operative alone through `CommsSubSystem.SendPrivateFeed` ([chat](chat-audio-screens.md))
- `Assets/Scripts/SS3D/Systems/Gamemodes/NukeOpsRules.cs` — pure §6 win/lose table ([round-end loop](../../archive/architecture/2026-11_round-end-loop.md) P3): crew win when the squad is out, timer/admin end resolved by who holds the disk, container-tree walk; `NukeOpsRulesTests`
- `Assets/Scripts/SS3D/Systems/Gamemodes/NukeOpsWorld.cs` — server adapter: reads each operative's entity (dead, ghost, gone, SSD) and the disk's carrier into a `NukeOpsSnapshot`
- `Assets/Content/Data/Roles/StartingRoles.asset` — available roles and slot caps (0 = unlimited)
- `GamemodeObjectivePanelView` — legacy uGUI panel hidden (no Tab hotkey; PDA tab later)

Server Settings selects Secret / Extended / Nuclear Emergency. Extended skips objective
initialization; Secret uses the sole existing antagonist mode. The runtime gamemode reference
is server-side (no SyncVar); authorized settings use the rounds subsystem's TargetRpc.
Map validation uses the same Secret → nuclear mapping and requires all offered job markers before countdown ([rounds-lobby](rounds-lobby.md)).
Weighted mode pools remain deferred; see [rounds-lobby](rounds-lobby.md).
The `-nukeops` server profile (`Rounds/NukeOpsProfile.cs`, [rounds-lobby](rounds-lobby.md)) runs 30-minute Secret rounds and sets the ready threshold in memory at boot.

The operative loadout includes the Syndicate hardsuit (ExoSuit) and helmet (Head), authored by the [inventory content recipe](inventory.md#overview). The suit and helmet seal against pressure and cold, and a full oxygen tank starts in suit storage with internals off. Magboots fill both shoe slots, switched off. `RoleSubSystem` spawns suit storage after the suit. Crew roles carry an emergency tank and a gas mask through `RoleLoadout.PocketAssets`.

## Extension points

- Active gamemode name: `GamemodeSubSystem.CurrentGamemodeName` (used by round-history persistence).
- Mode win rules: override `Gamemode.ResolveEnd` (gives a timer or admin end its winner, called from `RoundSubSystem.BuildOutcome`) and `Gamemode.TryGetEndCondition` (checked on every `EntitySubSystem.ServerSpawnedPlayersChanged` while Ongoing). Keep the rules pure, as `NukeOpsRules` is.
- New job: add a `RoleData` to `StartingRoles.asset` and map its department in `IdAccessSubSystem.DepartmentForRole`.
- New antagonist role: a `RoleData` with `_antagonistCategory` set, listed in `StartingRoles.asset`; it never appears in the job list. `RoleSubSystem.Operatives` / `IsOperative` expose the drawn squad (server).

## Pitfalls

- Operatives spawn on Nuclear Operatives markers (round-robin); everyone else still uses the legacy transform or first marker, so job markers are not routed.
- Objectives must resolve inside `RoundSubSystem.BuildOutcome`: `Nuke.Detonate` (a `[Server]` entry; a ServerRpc called from the server's interaction tick is dropped on a dedicated server, pinned by `NukeDetonateContractTests`) ends the round before `NukeDetonateEvent` fires, so an event-driven objective would still be pending when the outcome is fixed.
- `ServerSpawnedPlayersChanged` fires while a death is half done: the ghost is in `SpawnedPlayers` before `MindSubSystem.SwapMinds` gives it the mind. Find an operative's entity null-safely by mind (`NukeOpsWorld`); `EntitySubSystem.GetSpawnedEntity` dereferences `Mind` and throws on that ghost.
- There is no voluntary ghost-out: a ghost always means the body died, so Decision 16's "ghosted-out bodies count as alive" has no case yet. An SSD body keeps its mind and counts as up.
- **Design deviations** ([antagonist-content.md](../../design/antagonist-content.md) §6): an operative is out when dead or destroyed; restraint is ignored until restraints exist (handcuffs are MVP2). An admin end in a Nuke Ops round resolves as a timeout, so it still gets a winner. The ready threshold gates Secret only and has no admin form field; `-nukeops-min-ready=N` sets it.
- The operative kit (M4, engineering jumpsuit) is a placeholder: MVP1 M1c gives operatives the C20R and Stechkin. Swap `OperativeLoadout.asset` when those land.
- Assignment happens at round start (`ResolveRoundStartRoles`, called by `EntitySubSystem.SpawnReadyPlayers`) or at latejoin spawn, not on connect. `TryGetRole` is empty in the lobby.

## Tests

- `validate.sh --steps compile,editmode`: `GamemodeTests` (team objective assignment and resolution), `SquadDrawTests` (squad size, opt-ins only, Extended fallback, latejoiners never drawn), `NukeOpsProfileTests` (ready threshold, default opt-in), `RoundEndLoopTests` (objective results on the outcome).
- `NukeOpsRulesTests` (EditMode): every §6 row plus SSD, ghosted, dead carrier, crew carrier, nested-bag and missing-disk cases, and the admin end resolved as a timeout. `TestRoomFixtureTests` keeps the test room passing Map Check for Nuclear Emergency.
- `validate.sh --steps scenarios:NukeOps` (tier 2, `NukeOpsScenarios`): a Nuclear Emergency round in the test room with the host opted in. Checks the operative spawn (marker, kit, briefing naming the objective), detonation → operatives win with the station destroyed, and the squad killed → crew win at once. The fixture keeps the round-start body (`FreshBodyPerScenario`), and both outcome scenarios reboot the room.

## Depends on / Used by

- **Depends on:** [rounds-lobby](rounds-lobby.md), [entities](entities.md), [id-access](id-access.md) (crew records at spawn), [chat](chat-audio-screens.md) (briefing feed line)
- **Used by:** [persistence](persistence.md) (round-end history entry)

## Related docs

- Known design deltas: [REVIEW_2026-10 — Known delta](../../design/REVIEW_2026-10.md#known-delta-revised-design-vs-code) — rows: antagonist-content, objectives (crew win, legacy objectives, briefing), round-config (opt-in minimum)
- [INDEX.md](../INDEX.md)
