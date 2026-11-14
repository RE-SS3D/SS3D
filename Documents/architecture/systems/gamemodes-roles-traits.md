> Code paths: Assets/Scripts/SS3D/Systems/Gamemodes/, Assets/Scripts/SS3D/Systems/Roles/, Assets/Scripts/SS3D/Systems/Traits/
> Entry points: GamemodeSubSystem, RoleSubSystem
> Status: partial
> Verified: e03d3fca — 2026-11-14

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
- `Assets/Scripts/SS3D/Systems/Gamemodes/NukeOpsWorld.cs` — server adapter: reads each operative's entity (dead, ghost, gone, SSD), the disk's carrier and whether a device holds the disk into a `NukeOpsSnapshot`
- `Assets/Scripts/SS3D/Systems/Roles/RoleLoadout.cs` — per-role kit: hands, worn slots (`EquipmentAssets`), pockets, and the contents of a held or worn item's storage (`EquipmentContents` keyed by the holder's slot, `HandLeftContents`/`HandRightContents`). `RoleLoadout.StorageOf` picks the item's storage (first container with a storage panel)
- `Assets/Scripts/SS3D/Systems/Roles/Editor/JobKitSetup.cs` — puts the job weapons and medical gear into the loadouts
- `Assets/Scripts/SS3D/Systems/Furniture/NukeDeviceState.cs` — pure device rules (MVP1 M3): load the disk, arm (countdown), screwdriver opens the wiring panel, wirecutters defuse; pulling the disk after arming does not stop the clock; a defused device re-arms with a disk loaded; `NukeDeviceStateTests`
- `Assets/Scripts/SS3D/Systems/Furniture/Nuke.cs` — the device's server adapter and synced readout: five interactions (load disk, eject disk, arm, unscrew panel, cut wires), the countdown in `Update`, the arm announcement through `CommsSubSystem.SendAnnouncement` naming the device's area, examine lines (`ExamineNukeKeys`), action lines (`ActionMessageKeys.Nuke*`), sounds (`NukeAudioTrackIds`: disk insert, code-accepted beep, screwdriver, wirecutter and disarm sting at the device; a countdown beep each client derives from the synced deadline, faster under 30 s and 10 s; the arm sting after the announcement chime and the last-ten-seconds alarm station-wide through `CommsSubSystem.SendStationCue`). `Detonate` stays a `[Server]` entry, pinned by `NukeDetonateContractTests`
- `Assets/Content/Data/Roles/StartingRoles.asset` — available roles and slot caps (0 = unlimited): Captain (1 slot), Assistant, Security (1 slot), Operative (antagonist), Engineer, Doctor. List order is the lobby order; the fallback job is `RoleSubSystem._fallbackRole` by name
- `GamemodeObjectivePanelView` — legacy uGUI panel hidden (no Tab hotkey; PDA tab later)

Server Settings selects Secret / Extended / Nuclear Emergency. Extended skips objective
initialization; Secret uses the sole existing antagonist mode. The runtime gamemode reference
is server-side (no SyncVar); authorized settings use the rounds subsystem's TargetRpc.
Map validation uses the same Secret → nuclear mapping and requires all offered job markers before countdown ([rounds-lobby](rounds-lobby.md)).
Weighted mode pools remain deferred; see [rounds-lobby](rounds-lobby.md).
The `-nukeops` server profile (`Rounds/NukeOpsProfile.cs`, [rounds-lobby](rounds-lobby.md)) runs 30-minute Secret rounds and sets the ready threshold in memory at boot. The device countdown is 180 s (`NukeOpsProfile.NukeCountdownSeconds`); `-nukeops-nuke-seconds=N` (10–3600) changes it on any server, for a tiny station where the crew may be gone by the time the ops arm.

The operative loadout includes the Syndicate hardsuit (ExoSuit) and helmet (Head), authored by the [inventory content recipe](inventory.md#overview). The suit and helmet seal against pressure and cold, and a full oxygen tank starts in suit storage with internals off. Magboots fill both shoe slots, switched off. `RoleSubSystem` spawns suit storage after the suit. Crew roles carry an emergency tank and a gas mask through `RoleLoadout.PocketAssets`. `RoleLoadout.FirstHolderContents` is like `EquipmentContents` but goes only to the first player given that loadout each round (`RoleSubSystem` tracks it per round): the first operative to spawn carries the squad's one Syndicate bomb beacon in their bag ([explosive items](structural-destruction-explosives.md)).

Crew kits (worn, then what spawns inside):

| Role | Worn and held | Contents |
| --- | --- | --- |
| Assistant | grey jumpsuit, HiTops, headset, backpack; toolbox in hand | none |
| Security | security jumpsuit, jackboots, security headset, sunglasses, backpack; Taser in hand; Security PDA/ID | backpack: flashlight, brute and burn patch |
| Engineer | engineering jumpsuit, HiTops, insulated gloves, headset, toolbelt, backpack | belt: screwdriver, wrench, wirecutters, multitool, welder. Backpack: crowbar, cable, 2 steel sheets, flashlight |
| Doctor | grey jumpsuit, HiTops, headset, toolbelt, backpack; medkit in hand | belt: health scanner, one of each patch. Medkit: two of each patch. Backpack: flashlight |

The operative also gets a filled toolbelt (the Engineer's five tools) and a backpack with C4, a Syndicate minibomb and a flashlight. Engineer access is Engineering, EVA, Maintenance and Crew; Doctor access is Medical, Maintenance and Crew.

## Extension points

- Active gamemode name: `GamemodeSubSystem.CurrentGamemodeName` (used by round-history persistence).
- Device tuning: step timers are the `Nuke.*Seconds` constants; defuse tools are matched by item id (`Items.ScrewdriverBlue`, `Items.Wirecutters`), so a new screwdriver variant must be added to `Nuke.IsTool` callers. Scenarios shorten the countdown with `Nuke.SetCountdownSecondsForTesting`.
- Mode win rules: override `Gamemode.ResolveEnd` (gives a timer or admin end its winner, called from `RoundSubSystem.BuildOutcome`) and `Gamemode.TryGetEndCondition` (checked on every `EntitySubSystem.ServerSpawnedPlayersChanged` while Ongoing). Keep the rules pure, as `NukeOpsRules` is.
- New job: add a `RoleData` to `StartingRoles.asset` and map its department in `IdAccessSubSystem.DepartmentForRole`. Map Check refuses a round on a map without a marker for every offered job, so add the job's spawn marker to the shipped templates (`Builds/Game/Data/StationTemplates/`) and the test fixtures (`Testing/fixtures/stations/`), and list the job in `MapEditorSpawnCatalog.DefaultJobNames`. Add its loadout to `GasTankSetup`'s crew list, or the recipe leaves its pockets alone. The lobby job icon is `Assets/Art/Icons/Lobby/Jobs/<Name>.png` (`LobbyShellView.JobIconId`).
- Filled containers: put the holder in `EquipmentAssets` (or a hand) and its contents in `EquipmentContents` (or `HandLeftContents`/`HandRightContents`). Contents spawn after all worn items.
- New antagonist role: a `RoleData` with `_antagonistCategory` set, listed in `StartingRoles.asset`; it never appears in the job list. `RoleSubSystem.Operatives` / `IsOperative` expose the drawn squad (server).

## Pitfalls

- **Map Check required job markers that spawning never read:** `ResolveSpawnPosition` put every crew member on the map's first marker, so a Doctor on Firststation woke up in Security, with no error. Crew now spawn on their job's markers in turn (`SpawnPointRegistry.TryGetJobSpawn`), operatives on Nuclear Operatives markers; the legacy transform and first marker are only fallbacks for a job with no marker. Hit 2026-11-14 (bug report 3).
- Objectives must resolve inside `RoundSubSystem.BuildOutcome`: `Nuke.Detonate` (a `[Server]` entry; a ServerRpc called from the server's interaction tick is dropped on a dedicated server, pinned by `NukeDetonateContractTests`) ends the round before `NukeDetonateEvent` fires, so an event-driven objective would still be pending when the outcome is fixed.
- `ServerSpawnedPlayersChanged` fires while a death is half done: the ghost is in `SpawnedPlayers` before `MindSubSystem.SwapMinds` gives it the mind. Find an operative's entity null-safely by mind (`NukeOpsWorld`); `EntitySubSystem.GetSpawnedEntity` dereferences `Mind` and throws on that ghost.
- There is no voluntary ghost-out: a ghost always means the body died, so Decision 16's "ghosted-out bodies count as alive" has no case yet. An SSD body keeps its mind and counts as up.
- **Design deviations** ([antagonist-content.md](../../design/antagonist-content.md) §6): an operative is out when dead or destroyed; restraint is ignored until restraints exist (handcuffs are MVP2). An admin end in a Nuke Ops round resolves as a timeout, so it still gets a winner. The ready threshold gates Secret only and has no admin form field; `-nukeops-min-ready=N` sets it.
- **The loaded disk is not in a container.** It sits kinematic in a slot on top of the device and stays selectable, so its own Pickup also unloads it. The server clears the slot when the disk leaves it (in a container, moved more than 0.3 m, despawned), which also covers an admin `disk` teleport. Eject clears the slot at once; relying on that `Update` check left the device reading as loaded for a frame after the disk was already in hand.
- **Station-wide sound can't come from the nuke.** Its NetworkObject uses a grid observer condition, so a far client has no sync vars or RPCs from it; the alarm goes through `CommsSubSystem.SendStationCue` (crew with a body, like announcements). The countdown beep is local to observers on purpose.
- **A loaded disk counts as held by the operatives** (`NukeOpsSnapshot.DiskInDevice`, §6 "or the device itself once loaded"), so a timer or admin end with the disk in the device is a minor operative win.
- **Device design deviations** ([antagonist-content.md](../../design/antagonist-content.md) §6, §8; [explosives-destruction.md](../../design/explosives-destruction.md) §7): the countdown and disk state show on examine and in the arm announcement only. There is no readout on the model and no proximity alert yet; the countdown is audible (beeps, then the station-wide alarm). A defuse is not announced: its sting plays only at the device, unlike SS14's station-wide one. No countdown music (SS14's `NukeMusic`). Defuse is two tool steps (screwdriver 5 s, then wirecutters 10 s), owner's choice 2026-11-12; the Engineer spawns with both tools, but anyone holding them can defuse. The pinpointer is not built.
- Weapons and medical gear come from `JobKitSetup.Apply` (SS3D → Roles → Apply Job Kits, idempotent): operatives hold the C20R with a Stechkin in the backpack, Security holds the security pistol with a stun baton in the backpack (the taser is no longer in the kit), and the Doctor gets two gauze in the medkit (in place of its two medical patches; the medkit holds six) and a defibrillator in the backpack. Under the hardsuit operatives wear the Syndicate fatigues and combat gloves.
- Department clothing comes from `ClothingImportSetup.ApplyLoadouts` ([inventory](inventory.md)): Captain jumpsuit, garrison cap and command backpack; Doctor scrubs, lab coat and medical backpack; Security armor vest, helmet, security glasses and backpack; Engineer hard hat, meson goggles and work boots. Stand-ins until art lands: the Doctor wears a toolbelt as a medical belt, the Captain has no weapon, and every crew job but Security wears the generic headset, PDA and ID. Headsets are cosmetic: radio channels aren't tied to the worn headset. The welding helmet, security cap and Syndicate armor vest exist as items but are in no loadout.
- **Captain access is enumerated** (`id-access.md` §4): every department level plus Bridge, Captain's Office, Change ID and EVA, with Maintenance and Crew like other jobs; no Armory. A vault is an area painted with Command access, so the Captain opens it without a vault flag.
- **Loadout contents that don't fit vanish with only a warning:** `ItemSubSystem.SpawnItemInContainer` despawns what a container refuses (slot count, size class, nesting, storage conditions). The toolbelt holds five Small items, so the Crowbar (Normal) rides in the backpack. `PrefabTests.LoadoutContentsFitTheirStorage` checks slots, size, nesting and weight, but not storage conditions.
- **Worn slots have order rules:** the belt slot and pockets need a jumpsuit, and suit storage needs the suit. `SpawnLoadoutItems` equips the jumpsuit first and suit storage last; pockets and hands follow the worn slots.
- Kit weight counts against stamina (`StaminaController.ReferenceEncumbranceWeight`, 40 kg). At spawn the Engineer carries about 27% of the cap and the operative about 60% (51% before its belt and backpack were filled); Security's armor vest (3 kg) and helmet (1 kg) add 10%. Keep heavy materials out of spawn kits.
- Assignment happens at round start (`ResolveRoundStartRoles`, called by `EntitySubSystem.SpawnReadyPlayers`) or at latejoin spawn, not on connect. `TryGetRole` is empty in the lobby.

## Tests

- `validate.sh --steps compile,editmode`: `NukeDeviceStateTests` (device steps, time format, announcement text, countdown flag), `GamemodeTests` (team objective assignment and resolution), `SquadDrawTests` (squad size, opt-ins only, Extended fallback, latejoiners never drawn), `NukeOpsProfileTests` (ready threshold, default opt-in), `RoundEndLoopTests` (objective results on the outcome).
- `PrefabTests` (AssetAudit): `LoadoutsDoNotFillTheSecondSideOfAPair`, and `LoadoutContentsFitTheirStorage` for every content list.
- `NukeOpsRulesTests` (EditMode): every §6 row plus SSD, ghosted, dead carrier, crew carrier, nested-bag and missing-disk cases, and the admin end resolved as a timeout. `TestRoomFixtureTests` keeps the test room passing Map Check for Nuclear Emergency.
- `validate.sh --steps scenarios:NukeOps` (tier 2, `NukeOpsScenarios`): a Nuclear Emergency round in the test room with the host opted in. Checks the operative spawn (marker, kit, briefing naming the objective), the device loop through real interactions (load, arm, announcement, examine lines, eject keeps the clock, screwdriver then wirecutters defuse, the round goes on), a 10 s countdown detonation → operatives win with the station destroyed, and the squad killed → crew win at once. The fixture keeps the round-start body (`FreshBodyPerScenario`), and both outcome scenarios reboot the room.

## Depends on / Used by

- **Depends on:** [rounds-lobby](rounds-lobby.md), [entities](entities.md), [id-access](id-access.md) (crew records at spawn), [chat](chat-audio-screens.md) (briefing feed line)
- **Used by:** [persistence](persistence.md) (round-end history entry)

## Related docs

- Known design deltas: [REVIEW_2026-10 — Known delta](../../design/REVIEW_2026-10.md#known-delta-revised-design-vs-code) — rows: antagonist-content, objectives (crew win, legacy objectives, briefing), round-config (opt-in minimum)
- [INDEX.md](../INDEX.md)
