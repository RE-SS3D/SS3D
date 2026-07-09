> Code paths: Assets/Scripts/SS3D/Systems/Examine/, Assets/Scripts/SS3D/Localization/
> Entry points: ExamineSubSystem, ExamineUI, ExamineContentResolver
> Status: shipped

# Examine

## Overview

Hover tooltips and shift-hold detailed examine panels, range-gated off the [selection](selection.md) system's current `IExaminable`. Supports text and image panel variants. Localization uses a unified Examine string table and `LocalizedTextService`; dynamic content via `IExamineContentProvider` (e.g. identification cards).

## Start here

- `Assets/Scripts/SS3D/Systems/Examine/ExamineSubSystem.cs` — subsystem entry point
- `Assets/Scripts/SS3D/Systems/Examine/ExamineUI.cs` — hover and detailed panel UI
- `Assets/Scripts/SS3D/Systems/Examine/ExamineContentResolver.cs` — static table + dynamic section resolution
- `Assets/Scripts/SS3D/Systems/Examine/IExaminable.cs` — interface for examinable objects
- `Assets/Scripts/SS3D/Systems/Examine/ExamineData.cs` — ScriptableObject examine content asset
- `Assets/Scripts/SS3D/Systems/Examine/ExamineInteraction.cs` — Tier 1 radial petal; shift-hold detailed examine
- `Assets/Scripts/SS3D/Localization/LocalizedTextService.cs` — shared localization accessor with caching

## Extension points

- Add `SimpleExaminable`, `ImageExaminable`, or subclass `ExaminableBase` on world objects.
- Dynamic lines: implement `IExamineContentProvider` (see `IdentificationCardExaminable`).
- Editor: `SS3D/Localization/Examine/` menus for JSON export/import (`Editor/ExamineLocalizationExporter.cs`).

## Depends on / Used by

- **Depends on:** [selection](selection.md), [localization](localization.md), [interactions-framework](interactions-framework.md)
- **Used by:** Most world objects with examine content

## Related docs

- Plan: [examine_localization_design_5ca361a6.plan.md](../../plans/examine_localization_design_5ca361a6.plan.md)
- Plan: [radial_menu_implementation_5a83bdf9.plan.md](../../plans/radial_menu_implementation_5a83bdf9.plan.md) § Examine tier
