---
name: Examine Localization Design
overview: Redesign Examine localization around a single unified string table, a shared localization accessor, and a static/dynamic content split — with a phased migration that preserves existing English text and uses AI for locale translation.
todos:
  - id: foundation-service
    content: Add LocalizedTextService with caching, async resolution, locale-change invalidation, and dev/release fallback policy
    status: pending
  - id: examine-resolver
    content: Add ExamineContentResolver + ExamineContent struct; refactor ExamineUI to use it with cached resolution
    status: pending
  - id: examine-table
    content: Create unified Examine string table collection and Addressables group entry
    status: pending
  - id: examine-data-refactor
    content: Refactor ExamineData to LocalizedString fields with Phase 0 compatibility shim for legacy triple
    status: pending
  - id: editor-export-import
    content: "Build editor tooling: key generation from asset paths, English extraction from legacy assets, JSON export/import"
    status: pending
  - id: migrate-english
    content: "Phase 1: populate Examine_en for all 146 assets and rewrite ExamineData references"
    status: pending
  - id: ai-translate
    content: "Phase 2: AI-translate Examine strings to fr/pt-BR/ru-RU with glossary; import and validate"
    status: pending
  - id: audit-tests
    content: Extend ExamineDataTests for key existence and locale completeness; add resolver/service unit tests
    status: pending
  - id: cleanup-dynamic
    content: "Phase 3: remove legacy fields, deprecate old tables, add IExamineContentProvider for dynamic examine types"
    status: pending
isProject: false
---

# Future-Proof Examine Localization Design

## Current State

SS3D uses **Unity Localization 1.5.8** with no shared C# accessor. Examine is the main code-driven consumer:

```mermaid
flowchart TB
    subgraph today [Current Flow]
        ExamineData["ExamineData SO\n(table + NameKey + DescriptionKey)"]
        ExamineUI["ExamineUI\n(sync GetTable per lookup)"]
        Tables["3 sparse tables\nItems / Tiles / Misc"]
        ExamineData --> ExamineUI
        Tables --> ExamineUI
    end
```

**Key files:**
- [`Assets/Scripts/SS3D/Systems/Examine/ExamineData.cs`](Assets/Scripts/SS3D/Systems/Examine/ExamineData.cs) — stores `LocalizedStringTable` + raw string keys
- [`Assets/Scripts/SS3D/Systems/Examine/ExamineUI.cs`](Assets/Scripts/SS3D/Systems/Examine/ExamineUI.cs) — all lookup logic, no locale-change handling
- [`Assets/Content/Data/Examine/String/`](Assets/Content/Data/Examine/String/) — ~146 `ExamineData` assets
- [`Assets/Content/Localization/Table Collections/`](Assets/Content/Localization/Table Collections/) — UI, Items, Tiles, Misc, Font

**Problems to solve:**

| Issue | Impact |
|-------|--------|
| ~133/146 assets use **English prose as keys** | Keys are not stable, not translatable, leak as fallback text |
| Tables are **sparse** (Items has 2 keys; Tiles ~20) | Most examines show `*[to be localized]*` in-game |
| **3 arbitrary tables** (furniture in Tiles, wires split across Items/Tiles) | Authors must guess table; inconsistent |
| Localization logic **embedded in ExamineUI** | No reuse for `RoundStateView`, ID cards, future stats |
| **Sync `GetTable()` every frame** while Shift held | Wasteful; no caching or locale-change subscription |
| `IDENTIFICATION_CARD` enum exists, **no dynamic content path** | Future features have nowhere to plug in |
| SmartFormat configured but **unused** | No parameterized strings (counts, names, stats) |

---

## Target Architecture

```mermaid
flowchart TB
    subgraph content [Content Layer]
        ExamineData2["ExamineData\n(LocalizedString name + description)"]
        ExamineTable["Examine string table\n(unified, hierarchical keys)"]
    end

    subgraph core [Core Layer]
        LocService["LocalizedTextService\n(cache, async, fallback, locale events)"]
        ExamineResolver["ExamineContentResolver\n(static + dynamic sections)"]
    end

    subgraph ui [UI Layer]
        ExamineUI2["ExamineUI\n(display only)"]
    end

    ExamineData2 --> ExamineResolver
    ExamineTable --> LocService
    LocService --> ExamineResolver
    ExamineResolver --> ExamineUI2
```

### 1. Unified `Examine` string table

Create one collection at `Assets/Content/Localization/Table Collections/Examine/`.

**Key convention** — derived from asset path under `Examine/String/`:

```
{category}.{subpath}.{asset_name}.name
{category}.{subpath}.{asset_name}.desc
```

Examples:

| Asset path | Name key | Description key |
|------------|----------|-----------------|
| `Items/Functional/Tools/Engineering/Wrench.asset` | `items.tools.engineering.wrench.name` | `items.tools.engineering.wrench.desc` |
| `Structures/Walls/SteelWall.asset` | `structures.walls.steel_wall.name` | `structures.walls.steel_wall.desc` |
| `Entities/Humanoids/Human.asset` | `entities.humanoids.human.name` | `entities.humanoids.human.desc` |

**Shared-name variants** (e.g. four atmos pipe levels sharing `atmos_pipe` name) keep a shared name key but unique desc keys:

```
structures.pipes.atmos_pipe.name          → shared
structures.pipes.atmos_pipes_l1.desc      → per-variant
structures.pipes.atmos_pipes_l2.desc
```

Rules:
- `snake_case`, lowercase, dot-separated hierarchy
- Suffix `.name` / `.desc` always
- Keys are **never** display text
- English (`en`) is the **source locale** for AI translation

Deprecate Items/Tiles/Misc examine usage after migration (keep `UI` and `Font` tables unchanged).

### 2. Refactor `ExamineData` to use `LocalizedString`

Replace the triple `(LocalizationTable, NameKey, DescriptionKey)` with Unity-native references:

```csharp
// ExamineData.cs (target shape)
public LocalizedString Name;
public LocalizedString Description;
```

Benefits: editor picker with key validation, GUID-based references (rename-safe), built-in locale awareness.

Add optional `ExamineId` string (auto-derived from asset path in editor) for dynamic content and tests.

Remove per-asset `LocalizationTable` assignment — all examine strings live in the `Examine` table.

### 3. Add `LocalizedTextService` (thin shared accessor)

New file: [`Assets/Scripts/SS3D/Localization/LocalizedTextService.cs`](Assets/Scripts/SS3D/Localization/LocalizedTextService.cs)

Responsibilities (single place for all code-driven localization):
- Async table/string resolution with per-table cache
- Subscribe to `LocalizationSettings.SelectedLocaleChanged` and invalidate cache
- Centralized missing-key policy:
  - **Editor/dev builds**: log warning + show `[MISSING: key]`
  - **Release**: fall back to English source string (never show `*[to be localized]*` to players)
- `GetString(LocalizedString)` and `GetString(table, key, args?)` for SmartFormat args

Refactor [`RoundStateView.cs`](Assets/Scripts/SS3D/Systems/Lobby/UI/RoundStateView.cs) to use this service as the second adopter (validates the abstraction early).

### 4. Add `ExamineContentResolver` (static + dynamic split)

New file: [`Assets/Scripts/SS3D/Systems/Examine/ExamineContentResolver.cs`](Assets/Scripts/SS3D/Systems/Examine/ExamineContentResolver.cs)

```csharp
public readonly struct ExamineContent
{
    public string Name { get; }
    public string Description { get; }
    public IReadOnlyList<ExamineSection> Sections { get; }  // future: stats, ID fields
}

public interface IExamineContentProvider
{
    void AppendSections(IExaminable examinable, List<ExamineSection> sections);
}
```

- **Static text**: resolved from `ExamineData.Name` / `Description` via `LocalizedTextService`
- **Dynamic text** (future): `IExamineContentProvider` implementations on examinable objects (ID card, stack count, damage) append localized template sections with SmartFormat args

`ExamineUI` becomes display-only: calls resolver once per examine change, not per-frame string lookup.

### 5. Slim down `ExamineUI`

Changes to [`ExamineUI.cs`](Assets/Scripts/SS3D/Systems/Examine/ExamineUI.cs):
- Remove `GetLocalizedValue`, `_currentStringTable`, direct table access
- Cache last resolved `ExamineContent` + examinable reference; only re-resolve on examinable change, locale change, or Shift toggle
- Subscribe to locale-changed event via service
- Keep existing hover/detailed/image view logic unchanged

---

## AI-Assisted Translation Workflow

Migration should treat **English as source of truth** and use AI for `fr`, `pt-BR`, `ru-RU`.

```mermaid
flowchart LR
    Assets["146 ExamineData assets\n(English in keys today)"]
    Extract["Editor export script\nkeys + en text CSV/JSON"]
    AI["AI translation pass\nfr / pt-BR / ru-RU"]
    Import["Editor import script\nwrites locale assets"]
    Validate["Asset audit tests\nkey coverage"]

    Assets --> Extract --> AI --> Import --> Validate
```

**Export script** (Editor tool):
- Walk all `ExamineData` assets
- Generate canonical keys from asset path
- For legacy assets: use current `NameKey`/`DescriptionKey` **values** as English source text (not as keys)
- Output: `examine_strings_export.json` with `{ key, en, context? }`

**AI translation**:
- Batch keys through AI with SS3D context (sci-fi station game, item/tool descriptions, preserve `<br>` and TMP rich text)
- Include glossary constraints (e.g. "crowbar", "SMES", "PDA" — translate or keep per project policy)
- Human review optional but recommended for first pass

**Import script**:
- Creates/updates entries in `Examine` shared data + per-locale assets
- Rewrites `ExamineData` assets to `LocalizedString` references pointing at new keys

This avoids manually translating 290+ strings and makes adding future locales repeatable.

---

## Phased Migration Plan

### Phase 0 — Foundation (code, no content breakage)
- Add `LocalizedTextService` + tests
- Add `Examine` string table collection (empty)
- Add `ExamineContentResolver`
- Refactor `ExamineUI` to use resolver (still reads old `ExamineData` fields via compatibility shim)
- Add locale-change handling and string caching
- Fix player-visible `*[to be localized]*` fallback

### Phase 1 — Key generation + English population
- Build editor export/import tooling
- Auto-generate canonical keys for all 146 assets
- Populate `Examine_en` from existing English text (from legacy key values)
- Update `ExamineData` assets to new `LocalizedString` fields
- Extend [`ExamineDataTests.cs`](Assets/Scripts/Tests/AssetAudit/ExamineDataTests.cs):
  - Every asset has valid `LocalizedString` entries
  - Every referenced key exists in `Examine` shared data
  - English value is non-empty

### Phase 2 — AI translation
- Export `Examine_en` entries for translation
- AI-translate to `fr`, `pt-BR`, `ru-RU`
- Import into locale assets
- Add audit test: all keys present in all supported locales (can warn-only initially)

### Phase 3 — Cleanup + future features
- Remove legacy `LocalizationTable`/`NameKey`/`DescriptionKey` fields from `ExamineData`
- Retire Items/Tiles/Misc examine entries (or mark deprecated)
- Implement `IExamineContentProvider` for `IDENTIFICATION_CARD` as first dynamic consumer
- Add SmartFormat templates for parameterized examine lines (e.g. `"Charge: {charge}%"`)

---

## Testing Strategy

| Test | Purpose |
|------|---------|
| `EveryExamineDataHasValidLocalizedStrings` | Asset wiring |
| `EveryExamineKeyExistsInTable` | No missing keys at build time |
| `EveryLocaleHasAllExamineKeys` | Translation completeness |
| `ExamineContentResolverTests` (EditMode) | Resolver logic, fallback, caching |
| `LocalizedTextServiceTests` | Cache invalidation on locale change |

---

## What Stays Unchanged

- **Prefab-driven UI** (`LocalizeStringEvent` on lobby prefabs) — keep as-is; no need to route through the service
- **`LocalizeFont`** — asset-table pattern is fine
- **`UI` string table** — lobby/round state strings remain separate
- **Examine UI prefabs** — layout/positioning logic unchanged
- **Addressables delivery** — add `Examine` table to existing localization addressable groups

---

## Risk Mitigations

- **Compatibility shim** in Phase 0 reads old fields if new `LocalizedString` is empty — zero breakage during rollout
- **English fallback in release** ensures untranslated keys never show dev markers
- **AI glossary file** (`examine_glossary.json`) prevents inconsistent translation of game terms
- **Incremental audit tests** (warn → fail) so translation gaps don't block development
