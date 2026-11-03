> Implements: Documents/design/round-end.md §2, §4–§6; Documents/design/antagonist-content.md §2, §6 (first pass, How the round ends); Documents/design/objectives.md §2, §4, §8 (team objective, assignment, spawn briefing); Documents/design/lobby.md §4, §7 (opt-in list, self-starting round)
> Touches systems: rounds-lobby, gamemodes-roles-traits, entities, persistence, chat-audio-screens, screen-effects, ui-shell
> Status: active

**Progress:** P1 implemented and validated on 2026-11-03 (lifecycle pilot, nightly `sandbox-reset` smoke). P2 implemented with EditMode coverage; its spawn checks move into P3's scenario fixture. Both merged to develop in [#131](https://github.com/henkhooft/SS3D/pull/131) (`360ab4d60`). P3 (Nuke Ops rules) merged in [#136](https://github.com/henkhooft/SS3D/pull/136) with EditMode coverage and the `NukeOps` scenario fixture. P4 (the `-nukeops` 24/7 profile, Decisions 22–23, the `nukeops-round` smoke) is implemented on `claude/nukeops-profile`. The effort ships once that line is green in a nightly run. Advances [mvp1-nuke-ops.md](../milestones/mvp1-nuke-ops.md) M4 and M5 and [test-server.md](../milestones/test-server.md) T4 and T8. Tests follow the [testing philosophy](2026-11_testing-philosophy.md): each phase lists its checks by tier.

# Round-end loop and thin Nuke Ops outcome (Nov 2026)

A round ends with a legible outcome, everyone sees a summary, and the server returns to the lobby and starts the next round on its own. A Nuke Ops round has a real squad and a real winner, crew win included. Scope was settled in an implementation grill on 2026-11-02; the gameplay rules are the cited design sections and the REVIEW_2026-10 decisions Q14, Q16, Q31 and Q32.

## Starting state (2026-11-02, `06c7e4045`)

- **Ending the round:** `RoundSubSystem.ProcessEndRound` sets `Ending`, posts "The round has ended." to OOC, calls `AppendRoundHistory`, waits 3 s, then `StopRound` despawns everyone and unloads the station. There is no outcome, no summary and no return to a fresh round unless `AutoStart` is on.
- **Self-starting rounds** exist as the `AutoStart` server setting (`RoundAutoStart`, 20 s countdown once at least one player is ready, admin hold/cancel), built for the sandbox ([2026-11_sandbox-server](2026-11_sandbox-server.md) Decision 2). Only `-sandbox` turns it on.
- **Round length** is enforced in `RoundSubSystem`'s tick with 5- and 1-minute warnings for every timed round.
- **Nuke:** `Nuke.Detonate()` (via `NukeDetonateInteraction`) calls `GamemodeSubSystem.EndRound()` instantly. No arming, countdown or disk check (that is M3).
- **Gamemode:** `NukeGamemode` is an empty `Gamemode` subclass. Legacy objectives (`DetonateNukeObjective`, `GetItemObjective`) go to every spawned player, complete on pickup, and `ObjectiveStatus` has four states.
- **Antagonist opt-ins** are stored per ckey (`RoleSubSystem.IsAntagonistOptedIn`) from a fixed list in `AntagonistCategories` that includes Traitor; nothing consumes them.
- **Roles:** `StartingRoles.asset` holds Assistant and Security. `RoleLoadout` spawns equipment and hand items. `EntitySubSystem.ResolveSpawnPosition` ignores job/operative markers (gamemodes map pitfall). Map validation already requires an operative marker (`AntagonistSpawnCategory.NuclearOperatives`), the nuke and the disk for Secret and Nuclear Emergency.
- **Round history:** `RoundHistoryEntry` stores timestamp, gamemode, map, player count, duration and a hardcoded `fallbackMap = true`.
- **Restraints** do not exist (only a "restrained" alert icon).

## Decisions

| # | Decision |
|---|---|
| 1 | Scope is M5 + thin M4. M3's nuke loop is out; the existing instant `Detonate()` stands in for "the device detonates" until M3 replaces it. |
| 2 | One **round outcome record**, fixed on the server at the instant a trigger fires (round-end §2): trigger (`GamemodeCondition`, `Timer`, `Admin`), winner (`None`, `Operatives`, `OperativesMinor`, `Crew`), resolved gamemode, antagonist roster with objective results, and every player who took a body (name, job, alive/dead). Nothing is computed after the trigger. |
| 3 | **Summary content, pass 1:** gamemode, winner, what ended the round, antagonist roster with objectives met/failed, everyone's name, job and alive/dead. The viewer's own row is highlighted. Cause of death is out. Detonation adds "Station destroyed"; fates still show state at the trigger instant. |
| 4 | **Everyone connected** sees the summary: living, ghost and lobby players. It is an overlay; the world keeps running and players keep control (SS13 end-of-round precedent). It can collapse to a banner. Nothing during the summary changes the fixed outcome. |
| 5 | The summary beat is a **fixed 30 s**; an admin can **hold** it (same wedge pattern as the countdown hold). After it, `StopRound` runs as today and everyone is in the lobby. |
| 6 | The lobby keeps a **"Last round" card** with the same summary until the next round ends, so latecomers can read it. |
| 7 | **`AutoStart` defaults on** for every dedicated server (round-end §6, lobby §7, Q16). Add a **minimum-ready** server setting, default 1. Readiness resets each round, as today. |
| 8 | **Admin end and Extended timeout** have winner `None`. An admin end in a Nuke Ops round resolves the §6 table **as a timeout**, so a stopped round still gets a result (owner decision; §6 only names the timer). |
| 9 | `RoundHistoryEntry` gains trigger, winner and operative ckeys; `fallbackMap` gets its real value. |
| 10 | **Squad size** = ceil(ready players / 4), capped at 5. The mode minimum is a server setting, default 1. |
| 11 | **The draw** (antagonist §2, Q31) runs once, at round-start resolution, before jobs are assigned: a plain random draw among ready players who opted into Nuclear Operative. Those players skip job resolution. Too few opt-ins, under Secret **or** an explicit Nuclear Emergency, runs the round as Extended and tells admins in Admin chat and the Server Settings status. Latejoiners are never drawn. |
| 12 | **Operative role, thin:** an `Operative` `RoleData` that never appears in the job list; spawns at the operative marker; a `RoleLoadout` from existing items (gun, spare magazines, an ID with operative access, an outfit distinct from any crew job). Pinpointer and disk loading are M3; sealed hardsuit and internals are M8. Crew spawning on job markers stays out. |
| 13 | **Objectives** follow Q32: only the squad holds one, a single shared "detonate the station's device" team objective (objectives §2); three states; resolved when the round ends. Legacy every-player objectives and pickup completion are removed. |
| 14 | **Briefing:** a personal feed message at spawn naming the role, the objective and the other squad members (objectives §8). The Escape-menu Notes panel is deferred. |
| 15 | The **lobby opt-in list** shows only antagonist roles of enabled modes (lobby §4, Q31): Nuclear Operative alone for now. |
| 16 | **Operative out** = body dead (health §4) or destroyed. SSD and ghosted-out bodies count as alive (Q35). **Deviates from antagonist-content.md §6** ("dead or restrained"): restraint is ignored until a restraint mechanic exists. |
| 17 | **Crew win** is checked whenever an operative's state changes and ends the round immediately (§6). |
| 18 | **Timeout disk check:** held = the disk is anywhere in the container tree on a *living* operative's body. Anything else, including a missing disk, is a crew win. "Loaded in the device" waits for M3. |
| 19 | **Detonation presentation:** a station-wide announcement plus a short white-out and screen shake for every client, then the summary. No `ResolveBlast` on the station. |
| 20 | Nuke Ops default round length is **30 minutes** (server setting). Gamemode-specific lengths stay deferred. |
| 21 | The gamemode-identity leak to clients (round-config §4 delta) stays parked; the summary shows the resolved mode regardless. |
| 22 | **Nuclear Operative opt-in is on by default** (owner, 2026-11-03): a player who never opened the antagonist list is in the draw; they can opt out. Without it, a public server where nobody ticks the box runs every round as Extended. |
| 23 | **Nuke Ops needs a minimum number of ready players** before the draw runs, a server setting the 24/7 profile sets to 4 ([test-server.md](../milestones/test-server.md) T8). Fewer runs the round as Extended with the same admin notice as Decision 11. Distinct from Decision 10's minimum squad size. |
| 24 | **Tests stay in the cheapest tier** ([testing philosophy](2026-11_testing-philosophy.md)). Outcome rules, the draw and the beat are EditMode; behaviour across roles, spawning, inventory, comms and the round end is one tier-2 scenario fixture; client delivery is a nightly smoke line; screenshots only for the summary's visuals. Automation runs a 3 s beat (`SetEndBeatSecondsForTesting`) except where the summary is the subject. |

## Shape

- **Outcome.** A plain serializable `RoundOutcome` built on the server by one entry point that every trigger calls (gamemode condition, timer, admin stop). Nuke Ops rules live in a **pure resolver** (`NukeOpsOutcomeRules` or similar) that takes squad states and disk location and returns the winner. `RoundSubSystem` owns the outcome's lifetime and sends it to clients when `Ending` begins and to anyone who connects while it is the last round.
- **Ending beat.** `ProcessEndRound` replaces its 3 s delay with the 30 s beat plus admin hold, then `StopRound` continues unchanged (map-editor autosave, despawn, unload). Admin stop requests during Ongoing go through the same outcome entry point with trigger `Admin`.
- **Summary UI** follows the UiShell view/subsystem split ([ui-shell](systems/ui-shell.md)): a round-end view on a layer above the HUD, collapsible to a banner, and a "Last round" card in `LobbyShellView`. Visuals come from the separate design pass.
- **Squad and roles** extend `RoleSubSystem.ResolveRoundStartRoles` with a draw step before `JobResolver`, and teach `EntitySubSystem.ResolveSpawnPosition` the operative marker. The draw itself is a pure function for EditMode tests.
- **Objectives** reshape `GamemodeSubSystem`/`Gamemode` around antagonist-only assignment and a resolve-at-end pass. Keep `GamemodeObjectivesView` on the `Networked View` prefab untouched unless it breaks.
- **Detonation** goes through `CommsSubSystem.SendAnnouncement` and a screen-effects pulse sent to every client.

## Phases

### P1 — Round-end loop (generic)

- `RoundOutcome` with triggers and winner `None`; detonation counts as `Operatives` (Decisions 2, 8).
- Summary overlay + banner, everyone connected, viewer row highlighted (Decisions 3, 4).
- 30 s beat with admin hold (Decision 5); "Last round" lobby card (Decision 6).
- `AutoStart` on by default, minimum-ready setting (Decision 7).
- Round history fields (Decision 9).
- Tests (as shipped):
  - **EditMode:** `RoundEndLoopTests`: outcome per trigger, roster, viewer row, beat and hold, countdown, minimum-ready auto-start, dedicated default.
  - **Visual + host integration:** one round-end pass in the lifecycle pilot (overlay over a living player, admin hold reaching the UI, return to the lobby, Last Round tab), with a D8 header.
  - **Multiplayer:** the nightly `sandbox-reset` smoke asserts that the outcome reaches a remote client and that the next automatic round starts.
  - Not covered: a ghost viewer and a client connecting after the round ended (the buffered RPC). Add them to P3's fixture or the smoke only if they break.
- Ships on its own: closes most of T4 without any Nuke Ops code.

### P2 — Squad

- Opt-in list from enabled modes (Decision 15).
- Draw, size, minimum and Extended fallback with admin notice (Decisions 10, 11).
- Operative role, marker spawn, loadout (Decision 12). The kit (M4 rifle, engineering jumpsuit) is a **placeholder**: [mvp1-nuke-ops.md](../milestones/mvp1-nuke-ops.md) M1c gives operatives the C20R and Stechkin and the M4 to Security, and M0 adds the ops suit and a real Engineer job. Swap the kit when those land.
- Team objective replaces legacy objectives (Decision 13); spawn briefing (Decision 14).
- Tests (as shipped): **EditMode** `SquadDrawTests` (size, opt-in only, fallback, latejoiners never drawn) and `GamemodeTests` (team objective). "A non-opted player gets a job" is the draw plus `JobResolver`, both EditMode. The spawn path (drawn host → Operative role → marker → kit → briefing → team objective) is not a pilot. It becomes the first check in P3's Nuke Ops scenario fixture, which pays the same setup.

### P3 — Nuke Ops rules

- Pure resolver for the §6 table with Decisions 8, 16 and 18; EditMode tests for every row plus SSD, ghosted, nested-bag and missing-disk cases.
- Crew-win check on operative state change (Decision 17); timeout and admin-end resolution.
- Detonation announcement, white-out and shake (Decision 19); 30 min default length (Decision 20).
- Tests:
  - **EditMode:** the resolver, every §6 row plus SSD, ghosted, nested-bag and missing-disk cases; the timeout and admin-end mapping.
  - **Tier 2:** a `NukeOps` scenario fixture. It boots a test room with a nuke, disk and operative marker in Nuclear Emergency mode, host opted in. Round flow is the subject here, so it goes through round start (rule 3). Checks: operative spawn, kit, briefing and team objective (P2); detonation → operatives win with the station destroyed; squad killed → crew wins at once. This is MVP1's "both outcomes proven reachable in the harness".
  - **Visual:** the detonation white-out and shake, one capture, only when that code changes.
  - **Multiplayer:** a `nukeops-round` line in `smoke-set.txt` (nightly) on the dedicated build: the draw on a real server, a remote operative spawning, the outcome reaching clients, and the next round auto-starting. Check the D7 smoke budget before adding it. The full happy path (breach, arm, end, restart) that closes T3 waits on M3.
- **Acceptance:** EditMode and `scenarios:NukeOps` green on the branch; the smoke line green in a nightly run.

### P4 — 24/7 Nuke Ops server profile

Serves [test-server.md](../milestones/test-server.md) T8 and Playtest 1's "every round loops with no admin rescue".

- A `-nukeops` server profile on the `-sandbox` pattern (`SandboxProfile`, [2026-11_sandbox-server](2026-11_sandbox-server.md)): mode Secret, `AutoStart`, 30 min rounds (Decision 20), no map editing or sandbox resets, persistence as a normal server.
- Minimum ready players for Nuke Ops, 4 in the profile (Decision 23); Nuclear Operative opt-in on by default (Decision 22).
- Hosting on TomNAS next to the sandbox is outside this repo; the profile and its arguments are what this effort ships.
- Tests: **EditMode** for the profile settings and the ready-player threshold. **Multiplayer:** the `nukeops-round` smoke line (moved here from P3) runs with `-nukeops` and a lowered threshold: the draw on a real server, a remote operative spawning, the outcome reaching clients, the next round auto-starting. Check the D7 smoke budget before adding it.

## Parked

- M3: disk loading, arming, countdown, defuse, pinpointer, arm announcement, indestructible disk.
- M8: sealed hardsuit and internals in the operative kit.
- Cause of death on the summary; objectives Notes panel; restraints (then lift Decision 16's deviation).
- Gamemode-identity leak; crew spawning on job markers; gamemode-specific round length.
- Evacuation trigger and escape roster (round-end §3, MVP2).

## Implementation notes

**P1 (2026-11-02).** Code: `Systems/Rounds/RoundEnd.cs`, `Systems/Rounds/Outcome/`, `UI/Lobby/RoundEnd/`; map: [rounds-lobby](systems/rounds-lobby.md). Divergences from the decisions above and the design pass:

- **Last round lives in a lobby tab, not a sidebar card** (Decision 6). The design's card sits in a wide left sidebar; the real lobby's sidebar is the 280 px character preview column. The Last Round tab (after Server Info) renders the same ledger as the overlay. The shell opens it once per ended round, but not while the player is on Server Settings or Chat.
- **Lobby players read the summary in that tab during the beat.** The overlay sits on the shell's Overlay layer, below the lobby window (Modal), so only players with a body or ghost see the overlay itself (Decision 4).
- **Dedicated means `NetworkType.DedicatedServer`.** A listen host (editor, main-menu Host) keeps `AutoStart` off. The admin form gained an Auto Start toggle and a Minimum Ready field (Decision 7).
- **Admin hold** is a Hold Summary / Resume Summary pair in Server Settings → Round Controls, posted to Admin chat (Decision 5).
- **The outcome reaches clients through a buffered `ObserversRpc`**, so a client connecting later still gets the last round. `fallbackMap` is written from a flag that P2's Extended fallback sets.
- **Tab** is a code-defined `ToggleRoundSummary` system action, live in the Global, Gameplay, TileMenu and MapEditor contexts ([inputs](systems/inputs.md)).

**P2 (2026-11-02).** Code: `Systems/Roles/SquadDraw.cs` (pure draw, `SquadDrawTests`), `RoleSubSystem.DrawSquad`, `RoundSubSystem.FallBackToExtended`, `Gamemodes/OperativeBriefing.cs`, `NukeGamemode`; data: `Roles/Operative.asset`, `Roles/Loadouts/OperativeLoadout.asset`. Divergences and choices:

- **Antagonist roles live in `StartingRoles.asset`** with `RoleData._antagonistCategory` set. `RoleSubSystem` keeps them out of the replicated job list and rejects them as lobby job preferences.
- **Squad size is at least the minimum:** max(minimum, ceil(ready / 4)), capped at 5, then limited by the opt-in count. The minimum is the **Minimum Operatives** field in Server Settings (1–5, default 1). Fewer opt-ins than the minimum gives an Extended round (`IsExtendedRound`) and a notice in Admin chat and the Server Settings status.
- **Kit:** M4 in the right hand, engineering jumpsuit, gas mask, insulated gloves, jackboots, toolbelt, backpack; a plain ID and PDA with Maintenance access (no operative access level exists; vault access is M3). **No spare magazines:** ranged weapons reload without magazine items. Loadout hand items are now placed by `Hand.Side`, not list order.
- **The operative's crew record** is created like any job's (Department None), so the crew manifest can list "Operative". Hiding it waits for a manifest pass.
- **Several operatives share markers** round-robin; with one marker they spawn on the same tile.
- **Objectives resolve at the trigger** (`GamemodeSubSystem.ResolveObjectives`, called from `BuildOutcome`). `Nuke.Detonate` ends the round before `NukeDetonateEvent` fires, so the team objective is met by `request.StationDestroyed`, not by the event. `ObjectiveStatus.Cancelled`, `GetItemObjective`, `GetNukeCard.asset`, `ItemPickedUpEvent` and late-join objectives are gone.
- **Briefing** is a private line in the HUD radio feed (header BRIEFING) sent 3 s after round start, so the client's feed exists. `CommsSubSystem.SendPrivateFeed` sends it.
- **Opt-in list** is `AntagonistCategories.All`, now Nuclear Operative only; Traitor is removed.
- **Not yet exercised in a running round:** the spawn path. It is P3's first scenario check (see P2 Tests).

**Tests (2026-11-03).** Brought in line with the testing philosophy after develop's harness rework (#129). The first pass added `RoundEndBeat.Seconds` to every PlayMode stop helper and raised pilot timeouts. That would have charged every scenario room reload 30 s against D7's nightly budget. Automation now shortens the beat instead (Decision 24). Validating the merge found two unrelated bugs, both fixed:
- `TestRoomFixtureTests` counted the Operative role as a required job spawn.
- Server Settings' once-a-second refresh wiped an admin action's result before it could be read.

**P3 (2026-11-03).** Code: `Systems/Gamemodes/NukeOpsRules.cs` (pure, `NukeOpsRulesTests`), `NukeOpsWorld.cs` (server adapter), `Gamemode.ResolveEnd` / `TryGetEndCondition`, `EntitySubSystem.ServerSpawnedPlayersChanged`, `ScreenEffects/StationDetonationPresentation.cs`; tier 2: `NukeOpsScenarios` (`scenarios:NukeOps`). Divergences and choices:

- **Timer and admin ends keep their trigger** and take the §6 winner from the disk; the reason says which ("Ended by an admin with the disk still on the station."). `BuildOutcome` calls `GamemodeSubSystem.ResolveEnd` before objectives resolve, and before `FinalizeGamemode` runs on Ending, so the squad and disk are still live.
- **Operative out = no entity, a ghost, or a Dead health state.** There is no voluntary ghost-out, so a ghost always means a dead body; Decision 16's "ghosted-out counts as alive" has no case to cover yet. SSD bodies keep their mind and count as up.
- **The crew-win check runs on `ServerSpawnedPlayersChanged`** (a death swaps the corpse for its ghost in `SpawnedPlayers`; a destroyed body leaves it). It fires before the ghost holds the mind, so the adapter's lookup is null-safe.
- **Disk held** = the disk's container chain (item → bag's item → … → body) ends at a living operative's entity. Several disks: any held one counts.
- **Detonation presentation reuses the blast pieces**: `ScreenEffectsSubSystem.TriggerWhiteout` (post-exposure burn plus a white vignette in the existing volume) and `CameraFollow.AddImpulse` at station strength. The summary overlay waits for the 2.5 s white-out.
- **30 min** is the `ServerSettings` default round length (Secret is the default mode); saved settings keep their value.
- **The fixture keeps the round-start body** (`ScenarioFixture.FreshBodyPerScenario`), unlike D3's fresh body per scenario: the spawn path is the subject. Both outcome scenarios end the round and set `ChangesRoom`, so each starts from a fresh round. `ScenarioFixture` gained `Mode` (a fixture in another mode reboots) and `BeforeRoundStart` (the host's opt-in, via `RoleSubSystem.OptIntoAntagonistForTesting`).
- **The test room** gained a NukeCart and the authentication disk so it passes Map Check for Nuclear Emergency (`TestRoomFixtureTests`).
- **The `nukeops-round` smoke moved to P4**: a dedicated build only runs Nuclear Emergency through the `-nukeops` profile, and a remote client is only drawn with Decision 22's default opt-in.

**P4 (2026-11-03).** Code: `Systems/Rounds/NukeOpsProfile.cs`, `ServerSettings.MinimumReadyForNukeOps`, `SquadDraw.HasEnoughReady`, `AntagonistCategories.IsOptedIn`; tests: `NukeOpsProfileTests` (EditMode), the `nukeops-round` smoke line. Divergences and choices:

- **The ready threshold gates Secret only** (Decision 23 names Nuke Ops in general). An admin who picks Nuclear Emergency gets Nuke Ops at any size, as with SS13's forced modes. The threshold is a server rule with **default 4 on every server**, not only in the profile. A solo or two-player Secret round, which every editor test and existing smoke runs, therefore stays Extended, as it did before default opt-in. It has no admin form field yet: `-nukeops-min-ready=N` sets it.
- **Default opt-in lives in two places** (Decision 22). The lobby list starts ticked, and the server counts a player who never sent preferences as opted in. The second case covers batchmode clients and the moment before a client's first preferences arrive. An empty list sent by the client is an opt-out. Opt-outs stay in memory, like job priorities, so a reconnect that rebuilds the lobby ticks the box again.
- **The profile keeps normal server rules**: players ready themselves (no `AllConnectedReady`), no respawn, map persistence on, no open build. It sets its own MOTD naming the threshold and the opt-out. `-sandbox` wins over `-nukeops`.
- **The smoke** runs `-nukeops -nukeops-round-minutes=1 -nukeops-min-ready=1` with one client. The timer ends the round with the disk on the station, so the check is `assert_outcome Timer Crew antagonist`. The client then readies into the next auto-started round. The set's estimate is ~15 min against D7's 20 (677 s measured plus ~210 s).
- **Hosting:** run the dedicated build with `-nukeops`, next to the sandbox on TomNAS. That deployment is outside this repo.

