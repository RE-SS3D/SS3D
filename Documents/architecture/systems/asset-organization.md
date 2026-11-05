> Code paths: Assets/Art/, Assets/Content/, Assets/Scripts/, Assets/Editor/, Assets/Settings/, Assets/Resources/
> Entry points: Tools/generate_art_index.py, Tools/generate_icon_index.py, Assets/Scripts/Tests/AssetAudit/AssetTaxonomyTests.cs
> Status: partial
> Verified: e2f86cf20 — 2026-11-05

# Asset organization

## Overview

Two top-level trees split raw art from game data/composition: `Assets/Art/` (`Animations`, `Font`,
`Graphics`, `Icons`, `Models`, `Sound`, `Textures`, `Videos` — organized by asset type then game domain) and
`Assets/Content/` (`Addressables`, `Data`, `Localization`, `Resources`, `Scenes`, per-domain system
prefabs/UI under `Systems/`, spawnables under `WorldObjects/`). The split is sound; adherence has drifted —
see the full audit in [2026-07_asset-file-structure-taxonomy.md](../2026-07_asset-file-structure-taxonomy.md).
Phase 1 icon consolidation moved Heroicons, Inventory, Rendered, Alerts and `External/` under
`Assets/Art/Icons/` (later joined by `Lobby/` and `map-editor/`). Still open: the remaining Phase 1 items
(`Content/Systems/UI/Systems/` flatten, `Scripts/External/FishNet/` stubs, animation/localization Misc,
`splatter.png`) and all of Phase 2 (InteractionIcons name collision, `Content/Systems/Substances/**` →
`Data/`, `Assets/Editor/*` → `Scripts/SS3D/Editor/`).

UI surfaces keep their asset catalog as `Content/Systems/UI/<Surface>/Resources/<Name>AssetCatalog.asset`,
loaded by name (`*AssetPaths.ResourcesCatalogName`). Hub prefab and VFX catalogs sit in the separate
`Assets/Resources/` root.

## Start here

Decision table for "where does a new file go" (full version + rationale in the taxonomy doc):

| New file is... | Goes in |
|---|---|
| Any icon image (SVG/PNG) | `Assets/Art/Icons/<Source>/` — always, no exceptions |
| Icon-catalog ScriptableObject (wraps icon assets for code lookup) | Owning system's `Content/Systems/<Domain>/`, named so it never collides with the Art-side icon folder it wraps |
| Cross-system game data (recipes, traits, roles, tilemap defs) | `Content/Data/<Category>/` |
| One-system-only config/catalog | `Content/Systems/<Domain>/` |
| Spawnable entity/furniture/item/structure prefab | `Content/WorldObjects/<Category>/` |
| UI panel / system-internal composition prefab | `Content/Systems/<Domain>/` |
| First-party C# (incl. Editor tooling) | `Assets/Scripts/SS3D/<Area>/` under an `SS3D.*` asmdef |

Also see [AGENTS.md](../../../AGENTS.md) § Finding art assets / Finding UI icons, and the generated indexes
[art-asset-index.md](../../art-asset-index.md) / [icon-index.md](../../icon-index.md).

## Extension points

Adding a genuinely new asset *category* (not covered by the table above): extend the table in the taxonomy
doc, then re-run `generate_art_index.py` / `generate_icon_index.py` so the generated indexes pick it up.
A new `Art/Icons/<Source>/` folder must also be added to the source list at the top of
`Tools/generate_icon_index.py`; the script scans only the folders listed there.

## Pitfalls

- **This is CI-enforced, not just documented.** `AssetTaxonomyTests.cs` (EditMode, runs via the existing
  `editmodetestrunner.yml` on every PR) fails on new raw-art files under `Content/`, new icon images outside
  `Art/Icons/`, new "Misc" folders, or new first-party asmdefs outside `Scripts/`. Pre-existing violations are
  grandfathered by exact path in `AssetAuditUtilities.cs` — when a Phase 1/2 migration item in the taxonomy
  doc ships, remove the matching grandfather entry so the test tightens; never add an entry to make a new
  violation pass.
- **Don't pre-create the destination folder before `git mv src dest`.** If `dest/` already exists, git puts
  `src` *inside* it (`dest/src/…`) instead of renaming. Move the folder itself, or `git mv` into a non-existent
  path; flatten immediately if nesting happens.
- **Phase 1 icons are under `Art/Icons/` now** — remaining scatter is InteractionIcons (+ `CloseIcon.png`)
  under `Art/Graphics/UI/Interactions/` (Phase 2 name collision) and TMP sprite sheets under
  `Art/Font/SpriteAssets/`. Don't add a new icon folder outside `Art/Icons/<Source>/`.
- **`.meta` pairing on moves:** Unity resolves references by the GUID inside a file's `.meta`, not by path —
  moving an asset and its `.meta` together (e.g. `git mv foo.png foo.png.meta <dest>/`) preserves scene/prefab
  references. String path constants (`MainHudAssetPaths.IconRoot` / `AlertIconRoot`), USS `url("project://…")`
  without a GUID, and Addressables address keys are path-sensitive — grep for the old path before any move.
- **`Content/Data/` vs. `Content/Systems/<Domain>/` line:** if more than one system consumes the
  ScriptableObject, it belongs in `Data/`; if it's private config for one UI surface/system, it stays in
  `Systems/`. Getting this wrong is how `Content/Systems/Substances/**` ended up holding domain data that
  belongs in `Data/` (see taxonomy doc Phase 2).

- **A new icon folder is missing from the icon index.** `generate_icon_index.py` scans a fixed list of
  roots; `Art/Icons/Lobby/` is not in it today, so its icons never reach `icon-index.json`. Add the root
  when you add the folder.
- **Videos under `Content/` pass the taxonomy test.** `AssetAuditUtilities.RawArtExtensions` covers
  images, audio, models and fonts, not `.webm`/`.mp4`. Main-menu wallpapers live in `Art/Videos/`; keep
  new video there by hand.

## Tests

- `Assets/Scripts/Tests/AssetAudit/` (EditMode): `AssetTaxonomyTests` (placement rules), plus `AssetDataTests`,
  `PrefabTests`, `SceneTests`, `TileMapTests`, `ExamineDataTests` for asset integrity. CI runs them through
  `editmodetestrunner.yml` on every PR and push to develop.

## Depends on / Used by

- Shares its root cause (one-off Editor rebuild menus producing per-feature asset catalogs) with
  [TECH_DEBT.md](../TECH_DEBT.md) §1.4–1.5, §1.15.
- Consumed by every domain that adds art, icons, or ScriptableObject data — i.e. all of `Content/Systems/*`.

## Related docs

- [2026-07_asset-file-structure-taxonomy.md](../2026-07_asset-file-structure-taxonomy.md) — full audit, target
  taxonomy, phased migration plan
- [2026-07_addressables-expansion-migration.md](../2026-07_addressables-expansion-migration.md) and the
  [data-codegen](data-codegen.md) map — asset databases and Addressables loading (path-sensitive address keys)
- [INDEX.md](../INDEX.md)
- [TECH_DEBT.md](../TECH_DEBT.md)
