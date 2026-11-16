> Implements: Documents/design/creative-mode.md (map authoring foundation)
> Touches systems: tile/construction, area debug overlay, persistence (local templates)
> Status: shipped

# Map Editor Replacement

## Goal

Replace the legacy TileMap Creator (DynamicPanels docked tab) with a full-screen in-game Map Editor matching the UI mockup in `Documents/plans/assets/construction-map-editor-mockup/`. Foundation for creative-mode gamemode integration in a follow-on pass.

## Scope (v1)

- Full-screen UI Toolkit editor: tools (Select / Edit / Move), undo/redo, object library, save/load popovers, layer visibility, hide UI, settings
- Server-authoritative placement via existing `ConstructionService`; command layer for undo
- Admin-only permissions (`IMapEditorAuthorizer` extensible for creative-mode Builder role)
- Scripting & Placements mode rail: UI stub only in v1 (spawn authoring later shipped separately)

## Out of scope (v1)

- Camera options popover
- Creative-mode instant placement, area merge/split tool, round-config map pool save
- Random spawners, triggers, atmos turf preset painting
- Spawn placements — **shipped later** in [2026-07_spawn-point-authoring](2026-07_spawn-point-authoring.md) (authoring + save; runtime resolution still deferred)

## Key files

- `Assets/Scripts/SS3D/Systems/Tile/MapEditor/MapEditorSubSystem.cs`
- `Assets/Scripts/SS3D/Systems/Tile/MapEditor/UI/MapEditorView.cs`
- `Assets/Scripts/SS3D/Systems/Tile/MapEditor/MapEditorCatalog.cs`
- `Assets/Content/Systems/UI/MapEditor/MapEditor.uss`

## Deferred

- Round-config map pool integration (blocked on round-config + persistence Phase 1b)
- Creative-mode gamemode pool entry and Builder role authorizer
- Runtime role→spawn-point resolution (`EntitySubSystem`)
