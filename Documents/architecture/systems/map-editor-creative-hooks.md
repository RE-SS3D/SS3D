> Parent map: [tile.md](tile.md)
> Code paths: Assets/Scripts/SS3D/Systems/Tile/MapEditor/, Assets/Scripts/SS3D/Systems/Tile/SpawnPoints/
> Status: partial
> Verified: e2f86cf20 — 2026-11-05

# Map Editor — creative mode extension hooks

## Overview

Reserved extension points shipped with map editor v1 for [creative-mode.md](../../design/creative-mode.md) Phase 2. Spawn Placements, gas markers and Area metadata now have authoring UI; the remaining reserved hooks are unselected.

Opening the editor suspends per-tick world sims and calls `ApplyEditorVisualSuppression`: health/atmos/hit/blast screen feedback is hidden and the atmos fullscreen passes plus wind streaks stop drawing. Closing restores both; the static atmos flag is also cleared in `OnDestroyed` so a scene unload cannot latch it.

Unsaved-change tracking (A4): `MapEditorUndoStack` keeps revision-based save points through undo/redo,
branches and the bounded history. Successful persistence saves/loads set the template identity
(`EditingTemplateName`) and save point; a failed save keeps the edits unsaved, and a new empty map
starts unsaved. Round stop uses this to prompt or autosave before unloading; see [rounds-lobby](rounds-lobby.md).

## Start here

Spawn Placements authoring:

- Catalog: `spawn:job:{RoleName}` / `spawn:antag:{Category}` under Scripting → Spawn Placements
- Registry: `TileSubSystem.SpawnPoints` (`SpawnPointRegistry`); one marker per tile
- Persist: `SpawnPointPersistenceContributor` (`spawn-points` station-template chunk)
- Effort: [2026-07_spawn-point-authoring](../../archive/architecture/2026-07_spawn-point-authoring.md)
- Still stubbed: RandomSpawners, Triggers Scripting rails

Gas markers authoring (Scripts → Atmospherics, [atmos-machinery](../../plans/atmos-machinery.plan.md)):

- Catalog: `gas-marker:{Kind}` through `MapEditorGasMarkerCatalog`: gas source, line fill, room air
- Registry: `TileSubSystem.GasMarkers` (`GasMarkerRegistry`); one marker per tile, whatever its kind. `GasMarkerRecord` holds kind, position, line, pressure and gas fractions, and is the saved, wire and command shape alike. Unlike spawn points it syncs to clients (`TileSubSystem.GasMarkers.cs`), so remote editors see pins and can delete
- Persist: `GasMarkerPersistenceContributor` (`gas-markers` station-template chunk)
- Form: `UI/MapEditorGasMarkerView.cs`. Picking a kind opens it on `MapEditorViewModel.GasMarkerDraft`, which placement reads, so placing needs no Apply. Select opens a placed marker with **Apply to marker**; Dropper copies one into the draft. The Move tool doesn't move markers
- Pins: `MapMarkerEditorView` draws spawn and gas markers under the Scripts layer
- Runtime: [atmospherics](atmospherics.md) `AtmosGasMarkers`

Area metadata: `MapEditorArea.cs` adds `MapEditorPermissions.TryAuthorize`-gated inspect/apply RPCs and server undo/redo; `UI/MapEditorAreaView.cs` edits the selected room. Names, department tags and explicit access masks persist through the existing area contributor. Room editing is selected through **Scripts → Rooms**. Its palette uses the standard Construct/Select/Dropper/Delete toolbar; New room is a distinct + palette entry, with numbered room names and creation/first stroke grouped in undo. Scripts markers default off and follow entry/exit of the Scripts category. `MapEditorAreaOverlay` renders the synced area membership with the gizmo color palette (one mesh per chunk; Resources shader). Areas is off by default in Layer View Mode, enabled by Scripts → Rooms across its existing toolbar tools and hidden on other tabs/editor close. Room and layer controls reuse `MapEditorView.SwitchRow`. Department mode previews read-only access bits and retains the custom override. Areas also shows UI Toolkit room-name labels anchored on a tile near each room center; names sync through a buffered snapshot and refresh on rename/undo. Painted boundaries, APC-less room creation and reversible line/rectangle strokes are implemented and manually accepted by the owner in the editor ([area-authoring](area-authoring.md)); release dogfood remains A2.5.

Sharing (A5): the Save popover's **Save & Export** saves, then zips the template into `Data/Exports/` on the server's disk (`RpcExportMap` → `MapFileSharing.TryExport`). On a host it opens the file manager with the zip selected and copies its path to the clipboard; a remote client only gets a toast, because the file is on the server. The Load popover's **Open Maps Folder** opens `StationTemplates/` (host only); zips dropped there import on the next list. A Load reports objects this build doesn't have (`TileMap.LastLoadSkipped`). Testers' guide: `Builds/TESTERS.md`, shipped in the release zips.

Open build: a server launched with `-openbuild` uses `OpenBuildMapEditorAuthorizer` (anyone not Restricted may edit, without becoming admin). For those non-admin "sandbox players" (`MapEditorPermissions.IsSandboxPlayer`), `MapEditorRateLimiter` throttles edits (4000-operation burst, 400/s refill). They may only undo or redo when the newest history entry is their own (history entries record their owner's client id), and they can't delete maps or overwrite maps saved by someone else this session (`MayReplaceMapFile`).

Sandbox lockdown ([2026-11_sandbox-server](../2026-11_sandbox-server.md)): `-sandbox` implies open build. With `MapEditorPermissions.PersistenceAllowed` off (`ServerSettings.MapPersistence`), Save, Export, Load, Delete, New Map and the round-end autosave are refused for everyone. `MapEditorSandboxRules` refuses spawn-point commands and the nuke/disk assets to sandbox players (also on move), but not gas markers, and `RpcSetInvincible` refuses them. On open the server sends a `MapEditorCapabilities` TargetRpc; the view hides Save/Load, the Player Invincible switch, the Spawn Placements tab and denied palette entries (`MapEditorCatalog.HideSandboxDenied`). Ghosts may edit (the open check only asks for a spawned entity).

Save validation uses `Tile/Validation/MapValidationService` for the server's selected mode after saving.
Incomplete maps remain saveable; errors and soft warnings open an author-only scrollable Map validation popover
(with no timed dismissal). Unknown job markers, markers without plenum flooring, and devices that cannot do their job are soft warnings: vents or scrubbers with no air alarm (a room with none, or a device outside every room), vents, scrubbers or gas pumps with no pipe on a line they attach to on their own tile, an air alarm facing no room, a room with vents but no scrubbers or the reverse, a painted room whose devices have no APC, a line fill with no pipe on its line, and a gas source or room air marker outside a sealed room (`AtmosRoomFlood`). "Room" means an [area](area.md): one APC's flood fill bounded by walls and doors, or a painted room on an authored map. It is not always one physical room, and APC default names repeat, so these warnings carry a position.
The same policy blocks round Start; see [rounds-lobby](rounds-lobby.md). Validation does not implement job-to-spawn assignment.

## Extension points

| Hook | Location | v1 default | Phase 2 use |
|------|----------|------------|-------------|
| `IMapEditorAuthorizer` | `MapEditor/Authorizers/` | `AdminMapEditorAuthorizer`; `OpenBuildMapEditorAuthorizer` with `-openbuild` or `-sandbox` | Builder role in creative-mode session |
| `MapEditorPlacementMode` | `MapEditorPlacementMode.cs` | `Normal` | `CreativeInstant` skips construction ladder |
| `MapEditorSaveTarget` | `MapEditorSaveTarget.cs` | `LocalTemplate` | `RoundConfigPool` when round-config lands |
| Scripts → Rooms | `MapEditorAreaPainting.cs`, `UI/MapEditorAreaView.cs` | Standard Construct / Select / Dropper / Delete for rooms | Rich region selection / stamps |
| `MapEditorCatalogSo` filter profiles | not yet in code | full catalog | `CreativeStructural` palette subset |
| Spawn Placements | `MapEditor/MapEditorSpawnCatalog.cs`, `Tile/SpawnPoints/` | **shipped** (authoring + save) | Runtime job→point resolution; round-config coverage |

Data contracts for Phase 2 vertical slice (per creative-mode §10):

1. Gamemode pool entry — empty antag categories, Builder job-list override
2. `MapEditorCommandService` with `PlacementMode.CreativeInstant`
3. Save via `IMapEditorPersistence` (`MapEditor/Persistence/MapEditorPersistence.cs`) targeting round-config map pool (blocked on persistence Phase 1b)

## Pitfalls

- **A new item is missing from the library with no error:** the palette and saved maps only see assets under `Content/Data/TileMap/Resources`, so a prefab with an `ObjectAssetReference` but no `ItemObjectSo` works in play and cannot be placed. `MapEditorCatalog.Build` passes no asset path, so name keywords in `MapEditorCatalogHeuristics.ClassifyItem` decide the subcategory, not the `Items/Weapons/` folder. `AssetAudit.TileMapTests` checks this for `Items/Weapons/` only. Hit 2026-11-06 (C4, X4, energy weapons).
- **A form test sees no edits outside a live panel:** UI Toolkit fields raise `ChangeEvent` only when attached to a panel, so `view.Build(new VisualElement())` passes the build and silently drops every edit. Host the view in an `EditorWindow`, as `GasMarkerAuthoringTests` and `MapEditorAreaViewTests` do. Hit 2026-11-05.
- **The gas marker form closes itself off its rail:** `RebuildGrid` closes it whenever the subcategory isn't Atmospherics, and any view model change can rebuild. A test must select the rail first or the form is gone by the next query. Hit 2026-11-05.
- **Room form shows blank labels / default white controls:** runtime panels do not inherit the Editor theme; the room form needs an explicit font, text colors and field/input styles in `MapEditor.uss`. Use stacked labels and bounded scrolling; toolbar tools need baked icon references.

## Tests

- `MapValidationTests`: shared requirements, nonblocking warnings, room device checks, repair and scrollable literal-text save report.
- `MapEditorCommandRevertTests`: save points across undo/redo, branches, the history limit and new-map/load resets; undo ownership for sandbox players.
- `GasMarkerAuthoringTests`: registry, refused records, save round trip, undo, placement gate, Map Check wording and the form. `AtmosGasSupplyTests`: mixes, holding, room air and the flood.
- `SandboxServerTests`: sandbox profile, persistence gate, capabilities, denylist and catalog filter.
- `MapFileSharingTests`, `MapLoadSkippedSummaryTests`, `MapEditorRateLimiterTests`, `AdminTokensTests`: export/import round trip, load-loss summary text, edit throttling, admin-name tokens.
- `MapEditorAreaViewTests`: room drafts survive toast/undo-state refresh; department defaults and explicit access submit distinct masks; read-only department previews preserve custom overrides; Areas follows the Rooms category across its standard tools; overlay meshes preserve tile positions and gizmo colors. Run `Testing/ui/validate.sh --steps compile,editmode`; release dogfood remains A2.5.

## Related docs

- Known design deltas: [REVIEW_2026-10 — Known delta](../../design/REVIEW_2026-10.md#known-delta-revised-design-vs-code) — rows: area, creative-mode (area fields, per-gamemode fit check, item placement)
- Parent: [tile](tile.md)
- Design (read-only): [creative-mode.md](../../design/creative-mode.md)
