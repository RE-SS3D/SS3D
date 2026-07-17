> Code paths: Assets/Scripts/SS3D/UI/ (target); interim: MachineInterface, Interactions UITK hosts
> Entry points: (none yet — UiShell deferred)
> Status: stub
> Verified: e7cfcc3aa — 2026-07-17

# UI shell

## Overview

Target composition root for all player-facing UI Toolkit surfaces. Layers: **HUD** (persistent), **overlay** (radial, armed, examine, reticle), **diegetic/modal** (machine panels), **debug** (console). Surfaces register via a path-based catalog (UXML/USS + binder factory), not scene SerializeField hosts. Policy: [2026-07_agent-first-composition.md](../2026-07_agent-first-composition.md).

**Shipped wedge:** machine UI path catalog ([2026-07_mi-path-catalog.md](../2026-07_mi-path-catalog.md)) — `MachineUiAssetPaths` + `MachineUiAssetCatalog` via Resources. Full UiShell (document ownership / layers) still deferred.

Until UiShell lands, use [machine-interface](machine-interface.md) + radial/armed UITK hosts as the interim pattern. Do not add new uGUI.

## Start here

- Policy: [2026-07_agent-first-composition.md](../2026-07_agent-first-composition.md)
- Shipped wedge: [2026-07_mi-path-catalog.md](../2026-07_mi-path-catalog.md)
- Interim: `Assets/Scripts/SS3D/UI/MachineInterface/MachineUiCatalog.cs`
- Interim: `Assets/Scripts/SS3D/UI/MachineInterface/MachineUiAssetPaths.cs`
- Interim: `Assets/Content/Systems/UI/MachineInterface/Resources/MachineUiAssetCatalog.asset`
- Interim: `Assets/Scripts/SS3D/Systems/Interactions/RadialInteractionSubSystem.cs`
- Tokens: `Assets/Content/Systems/UI/Tokens/`, `Assets/Content/Systems/UI/MachineInterface/Tokens/`

## Extension points

- New UI: UITK only; register into the catalog once UiShell exists. Until then, follow the [machine-interface](machine-interface.md) path-catalog checklist (no new host SerializeFields).
- Do not place new `UIDocument` hosts in Boot/Game scenes.

## Depends on / Used by

- **Depends on:** [inputs](inputs.md) (`InputInterface` document registration)
- **Will own:** [machine-interface](machine-interface.md), main HUD, lobby UI, comms UI, examine overlays, console (as redesigns land)

## Related docs

- [2026-07_agent-first-composition.md](../2026-07_agent-first-composition.md)
- [2026-07_mi-path-catalog.md](../2026-07_mi-path-catalog.md)
- Design (read-only): [Documents/design/main-hud.md](../../design/main-hud.md)
