> Code paths: Assets/Scripts/SS3D/Localization/
> Entry points: LocalizedTextService
> Status: partial
> Verified: e2f86cf20 — 2026-11-05

# Localization

## Overview

Static `LocalizedTextService` (own `SS3D.Localization` asmdef): per-table, per-locale cache, `LocaleChanged` event that clears the cache, `PreloadTableAsync` for warm-up, and `GetFormattedString` for Smart-format arguments. A missing key returns `[MISSING: key]` plus a warning in Editor/development builds and the English fallback in release. Only consumer is the [examine](examine.md) unified string table (resolver, ID-card and structural-integrity examinables, character health builder, examine overlay). Editor export/import lives under `SS3D/Localization/Examine/`.

## Start here

- `Assets/Scripts/SS3D/Localization/LocalizedTextService.cs` — shared localization service
- `Assets/Scripts/SS3D/Systems/Examine/Editor/ExamineLocalizationEditorMenu.cs` — `SS3D/Localization/Examine/` menu (export, import English migration)
- `Assets/Scripts/SS3D/Systems/Examine/Editor/ExamineLocalizationExporter.cs` / `ExamineLocalizationImporter.cs` — JSON round-trip for translation
- `Assets/Scripts/SS3D/Systems/Examine/Editor/ExamineIdentificationKeySetup.cs` — Tier C (no menu) seeding of ID-card and integrity template keys

## Extension points

- Code-driven lookups: resolve via `LocalizedTextService` rather than direct table access.
- New string tables: follow Examine migration pattern in [examine](examine.md).

## Pitfalls

- **`AssetTypeMetadata` missing `[Serializable]`:** Unity Localization package warning on SerializeReference. Upstream type inherits a `[Serializable]` base but lacks the attribute on the concrete class — cannot fix without forking the package; safe to ignore until a package update.
- **Flaky `[MISSING: key]` EditMode asserts:** asserting the missing-key formatter against a *real* Examine table key (e.g. wrench.name) fails whenever another test has already loaded the string table (`GetTable()` then returns `"Wrench"`). Use synthetic keys that are never authored (`test.…`) when testing the missing path.

## Tests

- EditMode: `LocalizedTextServiceTests`, `ExamineContentResolverTests` (`Assets/Scripts/Tests/EditMode/`).

## Depends on / Used by

- **Depends on:** Unity Localization package, [logging](logging.md)
- **Used by:** [examine](examine.md)

## Related docs

- Plan: [examine_localization_design_5ca361a6.plan.md](../../archive/plans/examine_localization_design_5ca361a6.plan.md)
