---
name: Map editor undo redo
overview: Phase 1 — route place/delete/decals through Compound commands so solo undo/redo works. Per-builder stacks + conflict validity deferred.
todos:
  - id: cmd-model
    content: Floor-decal commands + previous-state snapshots; server FromDto factory
    status: completed
  - id: route-hologram
    content: PlaceOnHolograms / DeleteOnHolograms → SubmitCommands; compound per gesture
    status: completed
  - id: server-execute
    content: Harden RpcExecuteCommands (auth, preview gate, sync depths)
    status: completed
  - id: tests
    content: "EditMode: place/replace/compound/decal undo-redo"
    status: completed
  - id: docs
    content: tile.md pitfalls + plan notes; Phase 2 deferral explicit
    status: completed
isProject: false
---

# Map editor undo/redo (Phase 1) — shipped

## Implementation notes

- Hologram place/delete submit flat `MapEditorCommandDto[]` via `MapEditorSubSystem.SubmitCommands` → `RpcExecuteCommands` → `MapEditorCommandFactory` (server snapshots previous occupants/decal ids) → `ExecuteCompound` (one stack entry per gesture).
- `SetFloorDecalCommand` covers set and clear; `FloorDecalsChanged` on the command context syncs clients.
- Alt-replace `PlaceTileCommand` restores the previous asset on undo.
- **Deferred (design creative-mode.md §6):** per-builder private stacks and “modified since by another builder” validity checks.
