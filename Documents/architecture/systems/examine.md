> Code paths: Assets/Scripts/SS3D/Systems/Examine/, Assets/Scripts/SS3D/Localization/
> Entry points: ExamineSubSystem, ExamineUI, ExamineContentResolver
> Status: shipped
> Verified: a906e2278 — 2026-07-18

# Examine

## Overview

Hover tooltips and shift-hold detailed examine panels, range-gated off the [selection](selection.md) system's current `IExaminable`. Supports text and image panel variants. Localization uses a unified Examine string table and `LocalizedTextService`; dynamic content via `IExamineContentProvider` (e.g. identification cards).

Examine is **not** an `IInteraction` — `ExaminableBase` is read by `ExamineSubSystem` from the current selection (hover + Shift). There is no `ExamineInteraction` petal class on develop.

**Condemned UI:** examine hover/detailed uGUI views — do not extend; rebuild on UITK when HUD/examine redesign lands ([agent-first composition](../2026-07_agent-first-composition.md)). Domain `IExaminable` / content resolution is **not** condemned.

## Start here

- `Assets/Scripts/SS3D/Systems/Examine/ExamineSubSystem.cs` — subsystem entry point; raises hover/detailed events
- `Assets/Scripts/SS3D/Systems/Examine/ExamineUI.cs` — hover and detailed panel UI (Shift-hold)
- `Assets/Scripts/SS3D/Systems/Examine/ExamineContentResolver.cs` — static table + dynamic section resolution
- `Assets/Scripts/SS3D/Systems/Examine/IExaminable.cs` — interface for examinable objects
- `Assets/Scripts/SS3D/Systems/Examine/ExaminableBase.cs` — base component; not an interaction target
- `Assets/Scripts/SS3D/Systems/Examine/ExamineData.cs` — ScriptableObject examine content asset
- `Assets/Scripts/SS3D/Localization/LocalizedTextService.cs` — shared localization accessor with caching

## Extension points

- Add `SimpleExaminable`, `ImageExaminable`, or subclass `ExaminableBase` on world objects.
- Dynamic lines: implement `IExamineContentProvider` (see `IdentificationCardExaminable`).
- Editor: `SS3D/Localization/Examine/` menus for JSON export/import (`Editor/ExamineLocalizationExporter.cs`).

## Depends on / Used by

- **Depends on:** [selection](selection.md), [localization](localization.md), [inputs](inputs.md) (via selection pointer-over-UI clear)
- **Used by:** Most world objects with examine content

## Related docs

- Plan: [examine_localization_design_5ca361a6.plan.md](../../plans/examine_localization_design_5ca361a6.plan.md)
- Plan: [radial_menu_implementation_5a83bdf9.plan.md](../../plans/radial_menu_implementation_5a83bdf9.plan.md) § Examine tier (planned petal; not shipped as `IInteraction`)
- Design (read-only): [Documents/design/examine.md](../../design/examine.md)
- [2026-07_agent-first-composition](../2026-07_agent-first-composition.md)
- [ui-shell](ui-shell.md)
- Tests: `ExamineContentResolverTests` (missing-key path must use synthetic keys — see [localization](localization.md) Pitfalls)
