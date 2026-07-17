> Code paths: Assets/Scripts/SS3D/UI/ (target); interim: MachineInterface, MainHud, Interactions UITK hosts
> Entry points: (none yet — UiShell deferred)
> Status: stub
> Verified: 98207be44 — 2026-07-17

# UI shell

## Overview

Target composition root for all player-facing UI Toolkit surfaces. Layers: **HUD** (persistent), **overlay** (radial, armed, examine, reticle), **diegetic/modal** (machine panels), **debug** (console). Surfaces register via a path-based catalog (UXML/USS + binder factory), not scene SerializeField hosts. Policy: [2026-07_agent-first-composition.md](../2026-07_agent-first-composition.md).

**Shipped path-catalog wedges (duplicated pattern):**

| Surface | Paths + SO | Rebuild menu | Map |
|---|---|---|---|
| Machine UI | `MachineUiAssetPaths` / `MachineUiAssetCatalog` | **SS3D → Machine Interface → Rebuild Asset Catalog** | [machine-interface](machine-interface.md), [mi-path-catalog](../2026-07_mi-path-catalog.md) |
| Main HUD | `MainHudAssetPaths` / `MainHudAssetCatalog` | **SS3D → Main HUD → Rebuild Asset Catalog** | [inventory](inventory.md) |

Full UiShell (document ownership / layers / one shared catalog helper) is still deferred. Until then, copy the MI checklist for new self-bootstrapped UITK hosts — do not invent a third ad-hoc loader. Do not add new uGUI.

## Start here

- Policy: [2026-07_agent-first-composition.md](../2026-07_agent-first-composition.md)
- First wedge: [2026-07_mi-path-catalog.md](../2026-07_mi-path-catalog.md)
- Interim MI: `Assets/Content/Systems/UI/MachineInterface/Resources/MachineUiAssetCatalog.asset`
- Interim Main HUD: `Assets/Content/Systems/UI/MainHud/Resources/MainHudAssetCatalog.asset`
- Interim: `Assets/Scripts/SS3D/Systems/Interactions/RadialInteractionSubSystem.cs`
- Tokens: `Assets/Content/Systems/UI/Tokens/`, `Assets/Content/Systems/UI/MachineInterface/Tokens/`

## Extension points

- New UI: UITK only. Prefer extending an existing `*AssetPaths` / `*AssetCatalog` pair; if a third surface needs one, schedule the shared catalog helper under follow-on (b) instead of forking a third copy.
- Do not place new `UIDocument` hosts in Boot/Game scenes; self-bootstrap + Resources catalog (MI / Main HUD) until UiShell owns documents.

## Future work (path catalogs)

Two near-identical `Paths` + committed SO + Editor rebuild-menu stacks exist (MI, Main HUD). A later effort should extract shared infrastructure (load helper, rebuild menu skeleton, asmdef wiring convention, Addressables migration path) under follow-on **(b) UiShell + path catalog**, then migrate both surfaces onto it — not keep growing N one-off catalogs.

## Depends on / Used by

- **Depends on:** [inputs](inputs.md) (`InputInterface` document registration)
- **Will own:** [machine-interface](machine-interface.md), main HUD ([inventory](inventory.md)), lobby UI, comms UI, examine overlays, console (as redesigns land)

## Related docs

- [2026-07_agent-first-composition.md](../2026-07_agent-first-composition.md)
- [2026-07_mi-path-catalog.md](../2026-07_mi-path-catalog.md)
- Design (read-only): [Documents/design/main-hud.md](../../design/main-hud.md)
