> Implements: (infra tooling — no design-doc section)
> Touches systems: data-codegen, ui-shell, entities, core-subsystems, and every Content/Systems/* UI surface
> Status: in-progress

# Asset & file structure taxonomy (Jul 2026)

## Why this exists

The project's intended split — `Assets/Art/` for raw art assets organized by type then domain, `Assets/Content/`
for game data and composition (Addressables, ScriptableObject data, scenes, per-domain system prefabs/UI) — is
sound as a concept. At ~12k files in, adherence has drifted: icons alone now live in at least eight separate
locations, "Misc" has been used as a folder name five separate times, and there's no written rule for when a
ScriptableObject belongs in `Content/Data/` versus living next to the system that owns it. This doc is the
first full audit pass: catalog every current inconsistency, define the ruleset going forward, and phase in
fixes ordered by risk (docs first, safe renames next, reference-sensitive moves last).

**This doc is the plan and progress tracker.** Phase 0 and Phase 1 icon consolidation have shipped;
remaining Phase 1 leftovers and Phase 2+ are separate, reviewable changes. Phase 2+ moves touch
GUID/path references in a Unity project with no CLI build/test flow (`CLAUDE.md`), so each needs an
Editor Play Mode smoke check before merging — don't batch phases together to save review passes.

## Current inventory (audit @ `b329ad1a`, 2026-07-22)

| Folder | File count |
|---|---:|
| `Assets/Art/` | 10,682 |
| `Assets/Scripts/` | 3,579 (incl. vendored `Scripts/External/*`) |
| `Assets/Content/` | 2,763 |
| `Assets/FishNet/` | 1,521 (vendored) |
| `Assets/Settings/`, `Assets/Editor/` | 30 each |
| `Assets/UI Toolkit/`, `Assets/Resources/`, `Assets/AddressableAssetsData/` | 5 each |

### 1. Icon scatter

**Audit snapshot @ `b329ad1a` (pre–Phase 1).** Phase 1 icon consolidation (`3fe823a15`) moved the starred
items into `Assets/Art/Icons/`; strike-through paths below are historical. Still open: InteractionIcons
Art/Content name collision (Phase 2) and TMP sprite sheets.

All 4,219 `.svg` files already lived under `Assets/Art/Icons/` (now `External/` after rename). Icon-like
image assets also existed in:

- ~~`Assets/Art/Graphics/UI/Misc/Heroicons/`~~ → `Assets/Art/Icons/Heroicons/` ★
- `Assets/Art/Graphics/UI/Interactions/InteractionIcons/` — 51 PNGs, plus `.../RadialMenu/CloseIcon.png`
  (Phase 2).
- ~~`Assets/Art/Graphics/UI/Containers/InventoryIcons/`~~ → `Assets/Art/Icons/Inventory/` ★
- ~~`Assets/Art/Graphics/Misc/RenderedIcons/`~~ → `Assets/Art/Icons/Rendered/` ★
- ~~`Assets/Content/Systems/UI/MainHud/Icons/AlertStack/`~~ → `Assets/Art/Icons/Alerts/` ★
- `Assets/Content/Systems/UI/Systems/Interactions/InteractionIcons/` — 51 `.asset` ScriptableObject wrappers
  that **share the exact folder name** with the Art-side PNG folder above, in a different tree, wrapping a
  different asset kind. This is the sharpest collision in the audit (Phase 2).
- `Assets/Content/Systems/UI/MapEditor/MapEditorIcons.asset`, `.../Font/SpriteAssets/TMPSpriteAssetIcons.asset` —
  more icon-catalog ScriptableObjects mirroring raw PNGs elsewhere.
- Vendored editor-toolbar icons in `Scripts/External/DOTween`, `Scripts/External/Hierarchy 2`,
  `Scripts/External/IngameDebugConsole` (incl. the repo's only `.spriteatlas`), `Scripts/External/RuntimeInspector`,
  `Assets/FishNet/Runtime/Editor/Textures/`.

### 2. Prefab placement

88 of 301 `.prefab` files live outside `Content/WorldObjects/`. Most (~36) are UI/system-composition prefabs
under `Content/Systems/<Domain>/` — that split (WorldObjects = spawnable entities/props, Systems = UI/system
internals) is actually **correct and should be kept**, it just isn't written down anywhere, so it reads as
scatter on a first pass. The rest are vendored (FishNet demos, `Scripts/External/*` packages) and out of scope.

### 3. ScriptableObject (`.asset`) placement

563 live under `Content/Data/`; 254 live elsewhere. Some of that is legitimate (Addressables config,
`Settings/` project singletons, `Content/Localization/` string tables). But domain data like
`Content/Systems/Substances/**` (Drinks/Elements/Fuel/Nutrients/Organic/Recipes — 11 files) sits next to UI
code instead of alongside the rest of the game's Recipes/Roles/Traits definitions in `Content/Data/`, with no
documented rule for which bucket a new ScriptableObject should land in.

### 4. Duplicate/confusing folders

- Two unrelated `Resources` roots: `Assets/Resources/` (a VFX catalog + floor-visual PNGs) and
  `Assets/Content/Resources/` (DOTween settings, `BillingMode.json`, shader subfolders) — plus several more
  `Resources/` subfolders nested under `Content/Data/TileMap`, `Content/Systems/UI/*`. Some of this is Unity's
  magic-loading-folder mechanic (`Resources.Load`) and can't be fully eliminated, but the two disconnected
  roots at different depths is avoidable confusion.
- `Assets/UI Toolkit/UnityThemes/UnityDefaultRuntimeTheme.tss` duplicated by name in `Assets/Editor/`.
- The InteractionIcons name collision (§1).

### 5. Five "Misc" dumping grounds

`Art/Animations/Misc`, `Art/Animations/Probably Not/Misc`, ~~`Art/Graphics/Misc`~~, ~~`Art/Graphics/UI/Misc`~~,
`Content/Localization/Table Collections/Misc` — the classic signal of a taxonomy with no place for edge cases.
Graphics Misc folders were emptied/removed with Phase 1 icon moves (`Windows/` → `Graphics/UI/Windows/`,
overlays → `Graphics/UI/Chrome/`).

### 6. Structural mistake

`Content/Systems/UI/Systems/` — a folder literally named "Systems" nested inside `Systems/UI`, holding
`Crafting/` and `Interactions/` as siblings that belong at the same level as `Comms/`, `Examine/`, `Lobby/`, etc.

### 7. Naming inconsistency

Spaces vs. PascalCase: `Assets/UI Toolkit`, ~~`Art/Icons/external icons`~~ (renamed `External/` in Phase 1),
`Content/Localization/Table Collections`. Most other spaced folders
(`Basic Shooter Pack`, `Pro Melee Axe Pack`, `TextMesh Pro`, `Hierarchy 2`) are asset-store/vendor pack names
and should be left alone, same as we don't rename `Assets/FishNet/`.

### 8. Root-level loose files and a stray asmdef

`AssemblyInfo.cs`, `Assembly-CSharp-Assets.asmdef`, `ExternalRules.ruleset` sit directly at `Assets/` root
alongside Unity/package-required singletons (`DefaultPrefabObjects.asset`, `DefaultVolumeProfile.asset`,
`UniversalRenderPipelineGlobalSettings.asset` — these are created at root by FishNet/URP by convention and
should **not** be moved). Separately, `Assets/Editor/URPMigration/SS3D.Editor.URPMigration.asmdef` is a
first-party asmdef living outside `Assets/Scripts/`, contradicting `CLAUDE.md`'s "gameplay code under
`Assets/Scripts/SS3D/`" premise for first-party C#.

### 9. Legacy stub

`Assets/Scripts/External/FishNet/` — 7 orphaned `.meta`-only entries left over from before FishNet moved to
its current home at `Assets/FishNet/`. Candidate for deletion after confirming no real files remain under it.

## Target taxonomy

| Kind of file | Home | Rule |
|---|---|---|
| Raw art (models/textures/sound/anim), by type then domain | `Assets/Art/<Type>/<Domain>/` | Unchanged — already sound. |
| Any icon image (SVG/PNG) used as a UI icon, regardless of which system consumes it | `Assets/Art/Icons/<Source>/` | **Single source of truth.** Sub-namespace by origin/purpose: `Icons/External/<pack>/` (was "external icons"), `Icons/Interactions/`, `Icons/Inventory/`, `Icons/Alerts/`, `Icons/MapEditor/`, `Icons/Rendered/`. Never add a new icon folder under `Graphics/` or `Content/Systems/*` again. |
| Icon-reference ScriptableObjects/catalogs (wrapping the raw icon asset for code lookup) | Owning system's `Content/Systems/<Domain>/` catalog | Fine to colocate with the code that consumes it, but must **never share a folder name** with the Art-side icon folder it wraps — rename one side when this collides (see Phase 2). |
| Cross-system definitional game data (recipes, traits, roles, databases, tilemap defs, examine strings) | `Assets/Content/Data/<Category>/` | Unchanged. If a new ScriptableObject is consumed by more than one system, it belongs here, not under `Systems/`. |
| Private implementation config of one system/UI surface (asset-path catalogs, per-surface resource lookups) | `Assets/Content/Systems/<Domain>/Resources|Config/` | Keep near the owning code — this is not scatter, it's correct scoping, as long as it isn't raw icon image data (see above). |
| Prefabs: spawnable entities/furniture/items/structures/world objects | `Assets/Content/WorldObjects/<Category>/` | Unchanged. |
| Prefabs: UI panels / system-internal composition | `Assets/Content/Systems/<Domain>/` | Unchanged — explicitly not a violation, document it so future audits don't re-flag it. |
| Scenes | `Assets/Content/Scenes/` | Already 100% consistent, no action needed. |
| Vendored third-party packages | `Assets/FishNet/`, `Assets/Scripts/External/<Package>/`, or the package's own asset-store root | Never reorganize to match our conventions — the upstream package is the source of truth, including its spaced folder names. |
| First-party C# code | `Assets/Scripts/SS3D/<Area>/` under an `SS3D.*` asmdef | Unchanged. First-party Editor tooling currently living under `Assets/Editor/` should migrate here over time (Phase 2). |
| Root-level Unity/package-required singletons | `Assets/` root | Leave in place — moving breaks FishNet/URP defaults. |
| "Misc" as a folder name | Banned going forward | Every existing Misc folder gets dispositioned (real category name, or its contents get individually recategorized) during Phase 1/2; no new one gets created. |

## Phased implementation plan

### Phase 0 — Docs & guardrails (no file moves)

- [x] This doc + the `asset-organization` system map (this change).
- [x] Update `AGENTS.md` § Finding UI icons to state the single-source-of-truth rule and the icon-image vs.
      icon-catalog-ScriptableObject distinction.
- [x] Add a `TECH_DEBT.md` entry pointing here.
- [x] Extend `Tools/generate_icon_index.py` to also index the current stray icon locations (§1) so the index
      is accurate immediately, independent of when the physical consolidation lands.
- [x] **CI enforcement:** `Assets/Scripts/Tests/AssetAudit/AssetTaxonomyTests.cs` (EditMode, runs on every PR
      via the existing `editmodetestrunner.yml` — no workflow change needed) fails on any *new*:
      - raw art file type (image/audio/model/font extension) added under `Assets/Content/`
      - icon image (SVG, or PNG with "icon" in the path) added outside `Assets/Art/Icons/`
      - new folder literally named "Misc"
      - first-party `.asmdef` outside `Assets/Scripts/`
      Every violation catalogued in §1–§8 above is grandfathered by exact path/prefix in
      `AssetAuditUtilities.cs` so the test suite is green today; **shrink a grandfather list entry when its
      matching Phase 1/2 checklist item below ships — never add to one to make a new violation pass.**

### Phase 1 — Low-risk consolidation (asset+`.meta` pairs moved together, no code changes expected)

Unity resolves references by the GUID inside each asset's `.meta` file, not by path — moving a file and its
`.meta` together (e.g. `git mv foo.png foo.png.meta <dest>/`) preserves the GUID and leaves scene/prefab
references intact. Still verify in the Editor after each batch: some code paths use string
`Resources.Load(path)` or Addressables address keys, which **are** path-sensitive and need a source-level fix,
not just a file move — enumerate call sites before each move in this phase.

- [x] `Art/Icons/external icons/` → `Art/Icons/External/` (rename + fix `generate_icon_index.py`'s `ICONS_ROOT`).
- [x] `Heroicons`, `InventoryIcons`, `RenderedIcons` → `Art/Icons/<Source>/`
      (`Heroicons/`, `Inventory/`, `Rendered/`). Coupled: emptied `Graphics/Misc` and
      `Graphics/UI/Misc` (Windows → `Graphics/UI/Windows/`, loose overlays → `Graphics/UI/Chrome/`).
- [x] `Content/Systems/UI/MainHud/Icons/AlertStack/` → `Art/Icons/Alerts/`, fix the handful of UXML/code refs.
- [ ] Flatten `Content/Systems/UI/Systems/` → `Content/Systems/UI/Crafting/` + `Content/Systems/UI/Interactions/`
      (removes the redundant nesting from §6).
- [ ] Delete the orphaned `Scripts/External/FishNet/` meta-only stubs after confirming no real files remain.
- [ ] Disposition remaining Misc folders: `Art/Animations/Misc`, `Art/Animations/Probably Not/Misc`,
      `Content/Localization/Table Collections/Misc` (`Graphics/Misc` and `Graphics/UI/Misc` done with icon moves).
- [ ] Move `Content/WorldObjects/World/VFX/Health/splatter.png` → `Art/Textures/World/` (or the relevant
      VFX-texture convention), fixing the material reference that consumes it.

### Phase 2 — Reference-sensitive moves (code + Addressables/catalog updates required)

- [ ] Rename the Art-side `InteractionIcons/` folder or the ScriptableObject-wrapper folder so they no longer
      share a name (§1); update codegen/catalog references.
- [ ] Move `Content/Systems/Substances/**` domain data (Drinks/Elements/Fuel/Nutrients/Organic/Recipes) into
      `Content/Data/`, alongside the game's other Recipes/Roles/Traits definitions — this is genuinely
      cross-system data, not UI-private config.
- [ ] Move first-party `Assets/Editor/*` code (`UIElements`, `StyleSheetImporter`, `URPMigration`) under
      `Assets/Scripts/SS3D/Editor/`, consolidating onto one `SS3D.*` asmdef (§8).

### Phase 3 — Cosmetic (optional, lowest value/risk ratio, do last)

- [ ] `Art/Animations/Probably Not/` — the one spaced in-house folder name that isn't a vendor pack name and
      reads as an unresolved placeholder; disposition its contents (promote or delete) rather than just renaming.
- [ ] Leave every other spaced folder alone — they're asset-store/vendor pack names (`Basic Shooter Pack`,
      `Pro Melee Axe Pack`, `TextMesh Pro`, `Hierarchy 2`), same category as `Assets/FishNet/`.

## Out of scope for this pass

- Completing remaining Phase 1 leftovers (Systems flatten, FishNet stubs, animation/localization Misc,
  splatter) and Phase 2+ — separate, reviewable PRs; each needs an Editor Play Mode smoke check before
  merge since there's no CLI build/test flow for this project.
- Re-litigating the Art/Content split itself — the concept is sound; the problem audited here is inconsistent
  adherence, not the taxonomy's design.
- The upstream `RE-SS3D/SS3D-Art` repo's own internal organization.

## Related docs

- [AGENTS.md](../../AGENTS.md) § Finding art assets / Finding UI icons
- [art-asset-index.md](../art-asset-index.md), [icon-index.md](../icon-index.md)
- [TECH_DEBT.md](TECH_DEBT.md) §1.4–1.5, §1.15 — same root cause (one-off Editor
  rebuild menus) as the icon-catalog scatter documented here
- [systems/asset-organization.md](systems/asset-organization.md) — this effort's navigation counterpart
- [TECH_DEBT.md](TECH_DEBT.md)
