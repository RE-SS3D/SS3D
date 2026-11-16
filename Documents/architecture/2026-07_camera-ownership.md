> Implements: infrastructure — no dedicated design doc; supports map editor, observer/lobby cameras, and gameplay follow
> Touches systems: chat-audio-screens (camera), tile (map editor), player-control, inputs (parallel ownership model)
> Status: planned

# Camera ownership / dedicated camera manager

## Why

Player-camera pose has the same failure mode [input arbitration](../archive/architecture/2026-07_input-arbitration.md) fixed for actions: **many writers, no single owner.**

Today `CameraFollow`, `MapEditorSession`, FOV tweens on `PlayerCameraSubSystem`, and ad-hoc `Camera.main` / `CameraSubSystem.PlayerCamera` lookups all mutate or read the same transform. Correctness depends on every caller disabling the others in the right order. Coimbra `UpdateEvent` still fires after `enabled = false`, so “disable `CameraFollow`” was not enough — map-editor orbit was overwritten every frame and hologram picks stayed locked to follow-camera axes (mouse X = world east/west).

Interim patches (early-out on `CameraFollow`, disable-before-enter, `PickCamera` on the map editor) are the camera equivalent of the old input `ForceEnableActionMap` bandaids. They ship; they are not the model.

## Target model

One subsystem owns the player camera (extend or replace the thin `CameraSubSystem` / screens stack):

- **Contexts** (coarse): e.g. `GameplayFollow`, `MapEditorOrbit`, later `Observer` / `Cinematic`. Highest-priority live handle wins.
- **Single writer** of pose (and optionally FOV): only the active driver’s update runs.
- **One pick accessor**: `PickCamera` / screen-point helpers always come from that subsystem — never `Camera.main` in modal features.
- **Owned handles**: push on modal enter, dispose on exit (same shape as `IInputHandle`).

Map editor then requests `CameraContext.MapEditor` and never touches `CameraFollow.enabled`.

## Out of scope for this note

- Second physical camera GameObject (split view) unless a feature needs it.
- Rewriting every `Camera.main` call site before the manager exists — new modal camera users should wait for this effort or go through `CameraSubSystem.PlayerCamera` only.

## Related

- Symptom / interim fix: [systems/tile.md](systems/tile.md) Pitfalls (map editor hologram / orbit)
- Screens map: [systems/chat-audio-screens.md](systems/chat-audio-screens.md)
- Precedent: [2026-07_input-arbitration.md](../archive/architecture/2026-07_input-arbitration.md)
