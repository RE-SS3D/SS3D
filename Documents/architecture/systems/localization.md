> Code paths: Assets/Scripts/SS3D/Localization/
> Entry points: LocalizedTextService
> Status: partial
> Verified: a906e2278 — 2026-07-18

# Localization

## Overview

Shared localization accessor with caching, locale-change invalidation, and dev/release fallback. Primary consumer is [examine](examine.md) unified string table; editor export/import under `SS3D/Localization/Examine/`.

## Start here

- `Assets/Scripts/SS3D/Localization/LocalizedTextService.cs` — shared localization service
- `Assets/Scripts/SS3D/Systems/Examine/Editor/ExamineLocalizationExporter.cs` — JSON export for translation

## Extension points

- Code-driven lookups: resolve via `LocalizedTextService` rather than direct table access.
- New string tables: follow Examine migration pattern in [examine](examine.md).

## Pitfalls

- **`AssetTypeMetadata` missing `[Serializable]`:** Unity Localization package warning on SerializeReference. Upstream type inherits a `[Serializable]` base but lacks the attribute on the concrete class — cannot fix without forking the package; safe to ignore until a package update.
- **Flaky `[MISSING: key]` EditMode asserts:** asserting the missing-key formatter against a *real* Examine table key (e.g. wrench.name) fails whenever another test has already loaded the string table (`GetTable()` then returns `"Wrench"`). Use synthetic keys that are never authored (`test.…`) when testing the missing path.

## Depends on / Used by

- **Used by:** [examine](examine.md)

## Related docs

- Plan: [examine_localization_design_5ca361a6.plan.md](../../plans/examine_localization_design_5ca361a6.plan.md)
