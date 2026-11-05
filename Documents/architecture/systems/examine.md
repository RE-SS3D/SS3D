> Code paths: Assets/Scripts/SS3D/Systems/Examine/, Assets/Scripts/SS3D/UI/Examine/, Assets/Scripts/SS3D/Localization/
> Entry points: ExamineSubSystem, ExamineOverlaySubSystem, ExamineContentResolver, CharacterExamineHealthBuilder, SearchInteraction, TakeFromCharacterInteraction
> Status: partial
> Verified: e2f86cf20 — 2026-11-05

# Examine

## Overview

Hover tooltips and shift-hold detailed examine panels, range-gated off the [selection](selection.md) system's current `IExaminable`. Supports text and image panel variants. Localization uses a unified Examine string table and `LocalizedTextService`; dynamic content via `IExamineContentProvider` (multi-provider on one GameObject) and character health builders.

Glance examine (hover + Shift-hold) is **not** an `IInteraction` — `ExaminableBase` is read by `ExamineSubSystem` from the current selection. Opening another character's paperdoll **is** the **Search** interaction (`SearchInteraction` / `HandSearchExtension` on hand prefabs; icon `InteractionIcons.Search`).

**Rendering is UI Toolkit** (`ExamineOverlaySubSystem`, `Assets/Scripts/SS3D/UI/Examine/`) — the previous uGUI trio has been deleted, not ported (Phase 0 purge). Surfaces: `GenericExamineHoverView`; `CharacterQuickLookView` / `CharacterExamineWindowView` (character paperdoll).

**Fork deviations from** [examine.md](../../design/examine.md): design is hold-to-peek with no hover/click-lock (§1/§4); shipped code shows the name tooltip on plain hover, with Shift gating richer detail. Accepted. Design §5 says others never get internal vitals — shipped **Tier 0** public lines (bleed / severed / wound+ / conscious-state) are model-visible facts; organ bands and bruise-only lines are **Tier 1 self-only**. Medical scanner (numeric pools) stays out.

**Character examine** (design §7): no new `Human.prefab` component — `Human.asset` is `Type: CHARACTER`; overlay reads sibling `HumanInventory` / `HumanHealthController`. Do **not** reintroduce `CharacterExaminable` ([TECH_DEBT.md](../TECH_DEBT.md) §1.1).

- **Hover** → name tooltip only (identity from ID when visible). Works on self and others.
- **Shift (hold)** → name + `ExamineData` details + health lines. Tier 0 (self + others): state + one consolidated appearance sentence (`I` vs `he`). Tier 1 (self only): short feel lines (`I feel dizzy`). Quiet when healthy. Health rebuilds each frame while held. Does not open the paperdoll.
- **Shift, no target** → self detailed examine (design §2 fallback).
- **Search** (radial petal or **Shift+Click** shortcut) → persistent paperdoll on **other** characters only. Same Discover/`CmdRunInteraction` path; client opens UI via `SearchClientInteraction` → `ExamineSubSystem.RequestCharacterWindow`.
- **Click-to-take** — ~1.5s `TakeFromCharacterInteraction`; gated by `CharacterLootUtility.IsLootable` (dead/unconscious; restrained deferred).
- **Covered/obscured-slot filtering** not implemented. Slot↔`ContainerType`: `CharacterExamineSlotContainerMap`.

**Editor:** **SS3D → Examine → Rebuild Examine Asset Catalog**. Hand Search wiring: `HandsPrefabSetup.Wire` / `HumanPrefabRecipes`.

## Start here

Paths are under `Assets/Scripts/SS3D/Systems/` unless they start with `UI/` (`Assets/Scripts/SS3D/UI/`).

- `Examine/ExamineSubSystem.cs` — hover/detailed/character-window events
- `Examine/ExamineContentResolver.cs` / `ExamineData.cs` / `IExaminable.cs` / `ExaminableBase.cs` / `IExamineContentProvider.cs`
- `Examine/CharacterExamineHealthBuilder.cs` / `ExamineHealthKeys.cs` — Tier 0/1 health lines from `HealthSnapshot` + `HealthDebugDetail`
- `UI/Examine/ExamineOverlaySubSystem.cs` — surfaces + take wiring + self fallback
- `UI/Examine/GenericExamineHoverView.cs` / `CharacterExamineWindowView.cs`
- `Examine/CharacterExamineContentBuilder.cs` — slot icons + visible identity
- `Examine/CharacterExamineSlotContainerMap.cs` / `CharacterExamineTargetUtility.cs`
- `Examine/IdentificationCardExaminable.cs` (owner/role lines) / `StructuralIntegrityExaminable.cs` (damaged/cracked lines for walls, windows and tables; added by `StructuralIntegrityPrefabSetup`) — content-provider examples
- `Examine/Editor/` — localization JSON export/import menus; `ExamineIdentificationKeySetup` seeds ID-card and integrity keys (no menu)
- `Inventory/Interactions/SearchInteraction.cs` / `HandSearchExtension.cs` — paperdoll open as Help Instant
- `Inventory/Containers/CharacterLootUtility.cs` / `Inventory/Interactions/TakeFromCharacterInteraction.cs`
- UITK examine is `SS3D.UI.Examine` (avoids Systems↔MachineInterface cycle)

## Extension points

- `SimpleExaminable` / `ImageExaminable` / multiple `IExamineContentProvider`s on one GameObject.
- Character health: extend `CharacterExamineHealthBuilder` (no Human prefab component).
- Follow-ups: obscured-slot filtering; restrained loot; StoragePanel foreign-loot; medical scanner (virology).

## Pitfalls

- **Take without Discover:** start via `InteractionController.RequestTakeFromCharacter`, never `CmdRunInteraction` name resolve.
- **Search vs glance:** Shift-hold detail is examine overlay; paperdoll open is Search (Shift+Click runs Search via `InteractionController.TryRunSearchOnCharacterSelection`, not a separate overlay click path).
- **Foreign `CmdTransferItem` rejects** containers not in `ContainerViewer` display list — take uses `Hand.Pickup` instead.
- **Catalog missing in builds:** run **Rebuild Examine Asset Catalog** and commit `Resources` asset.
- **`InventorySlot` white HUD:** never put `overflow: hidden` on `.inventory-slot__well` (shared with Main HUD).
- **DDOL overlay vs Online hub / Selection enable order:** bind examine/selection events with `Update` retry.
- **Hover label off-screen:** UiShell `ConstantPhysicalSize` — use `InputInterface.ScreenToPanel` then `style.left`/`style.top`.
- **Hover paperdoll blocks Search:** paperdoll is Search/Shift+Click-only; hover is name tooltip.
- **Clothing pick hides CHARACTER:** walk ancestors via `CharacterExamineTargetUtility`.
- **Zone severity for remote examine** reads synced `HealthDebugDetail` (name is historical) — do not expand `HealthSnapshot` just for examine lines.
- **No `box-shadow` in USS:** UI Toolkit rejects it.

## Tests

- EditMode: `ExamineContentResolverTests`, `ExamineCanonicalKeyGeneratorTests`, `CharacterExamineHealthBuilderTests`, `CharacterExamineContentBuilderTests`, `CharacterExamineSlotContainerMapTests`, `CharacterLootUtilityTests`, `SearchInteractionTests`.
- AssetAudit: `ExamineDataTests`.

## Depends on / Used by

- **Depends on:** [selection](selection.md), [localization](localization.md), [inputs](inputs.md), [inventory](inventory.md), [health](health.md), [interactions-framework](interactions-framework.md), [interactions-runtime](interactions-runtime.md), [ui-shell](ui-shell.md), [core-subsystems](core-subsystems.md)
- **Used by:** Most world objects with examine content; [structural-destruction](structural-destruction.md) integrity lines, [id-access](id-access.md) card lines

## Related docs

- Known design deltas: [REVIEW_2026-10 — Known delta](../../design/REVIEW_2026-10.md#known-delta-revised-design-vs-code) — rows: id-access (examine names only a bare card)
- Plan: [examine_localization_design_5ca361a6.plan.md](../../archive/plans/examine_localization_design_5ca361a6.plan.md), [health_implementation_plan.md](../../plans/health_implementation_plan.md) Phase 6
- Design (read-only): [Documents/design/examine.md](../../design/examine.md), [health.md](../../design/health.md) §7
- Defaults: [2026-07_default-input-scheme.md](../../archive/architecture/2026-07_default-input-scheme.md)
