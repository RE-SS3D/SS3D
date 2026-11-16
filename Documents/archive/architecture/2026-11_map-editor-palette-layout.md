> Implements: Documents/design/creative-mode.md §2 (The palette's layout)
> Touches systems: tile (Map Editor catalog, delete targeting, hologram manager), map-editor-creative-hooks (view, view model, sandbox capabilities, area and gas-marker forms), map validator, tile content folders under `Resources`
> Status: shipped

**Progress:** S1 done (reorganisation, eraser and stubs removed, per-row delete, drift test); S2 done (palette-wide search, Show in palette, last tab, two-line slot labels, compact Map Check rows); S3 done (folder headings in Furniture and Machines); S4 done (Layers popover groups match Lower); S5 done (saved maps by prefab id); S6a done (variant families baked into the catalog); S6b done (one slot per family, V/Shift+V); S6c done (one selection route, family-label search). Advances [map-authoring.md](../../milestones/map-authoring.md) A8.

# Map Editor palette layout (Nov 2026)

Replaces the palette's sub-tab taxonomy with the one in `creative-mode.md` §2: four tabs (Upper, Lower, Items, Scripts), sub-tabs in build order, objects filed by the layer they sit on, palette-wide search, headings before new tabs, eraser as a tool.

## Starting state (2026-11-07, `db359c98`)

- `MapEditorSubcategory` (`MapEditor/MapEditorMode.cs`) has 21 values, the first 19 implicitly numbered. `MapEditorCatalog.asset` (169 entries, `Assets/Content/Systems/Tile/`) stores them as ints.
- The asset is generated: `Editor/MapEditor/MapEditorCatalogRegenerator.cs` (menu *SS3D/Map Editor/Regenerate Catalog*) rewrites it from `MapEditorCatalogHeuristics`, the only place asset paths are known (runtime has none).
- Classification mixes layer rules (tile objects) with name keywords (items). Furniture holds 47 entries (vendors, SMES, vents, lockers, chairs); Piping holds pipes, inline atmos parts **and** cables; Flooring/Turfs overlap; Catwalk/Lattice fall to Uncategorized; Items › Tools holds materials, parts, the nuke disk and sunglasses.
- Sub-tab facts live in ~8 switches across 15 files: `MapEditorCatalog.GetSubcategories` / `GetSubcategoryLabel` / `IsScriptingSubcategory` / `IsUnavailableScriptingSubcategory`, `MapEditorIconsSo.GetSubcategoryIcon`, `MapEditorDeleteTargeting.Resolve` / `IsItemSubcategory` / `RequiresSubcategorySelection`, `MapEditorViewModel.PlacementHint` / `IsRoomCategory`, `MapEditorView` (sandbox hides Spawn Placements; gas-marker form closes off Atmospherics; Rooms), `ConstructionHologramManager` (delete toast, item delete), `MapEditorSubSystem.TryGetSubcategoryPrototypeAssetName`.
- Delete is scoped to the open sub-tab (`MapEditorDeleteTargeting.Resolve`); Items delete the item under the cursor in any sub-tab.
- `MapValidator` hard-codes tab paths in text ("Add it under Scripts → Spawn Placements").
- `MapEditorCatalog.Query` searches only the open sub-tab, and skips search in Scripts outside Spawn Placements.
- An `IsEraser` catalog entry under Upper › Walls threads through view model, view and subsystem; a Delete tool already exists.
- Nothing remembers the last tab.
- `TileResourceLoader` uses `Resources.LoadAll<GenericObjectSo>("")`: asset paths inside `Resources` don't matter at runtime, so moving assets (with `.meta`) is safe.

## Decisions

Settled with the owner in an implementation grill (2026-11-07). Design premises are `creative-mode.md` §2; nothing here deviates.

1. **Layer-first classification.** Tile objects classify by `TileLayer` + `genericType`; items classify by `Items/` folder, and the folder *is* the sub-tab. Name keywords are deleted.
2. **Furniture-layer split.** A `FurnitureBase`/`FurnitureTop` object is **Machines** unless it sits under `Furniture/Generic`, `Furniture/Flora` or `Furniture/Storage` (→ Furniture). New devices land in Machines without listing.
3. **One static C# sub-tab table** (not a ScriptableObject) replaces the switches. One row per sub-tab: mode, build order, label, icon, delete scope, heading threshold, flags (`IsMarker`, `IsItem`, `SandboxRestricted`). Every former switch reads it.
4. **Fresh enum with explicit values**, old values deleted, never reused. The asset is regenerated in the same commit. An EditMode **drift test** asserts the committed asset equals regeneration output (tier-B pattern, [editor-tooling-tiers](../../architecture/2026-07_editor-tooling-tiers.md)).
5. **Eraser removed completely**: the catalog entry, `MapEditorCatalogEntry.IsEraser`, `CreateEraserEntry`, `IsEraserSelected` and every Edit+eraser path. The Delete tool is the eraser.
6. **Delete scope declared per row, by layer** (table below). Furniture and Machines both clear the furniture layers. Cables getting their own sub-tab retires the `tile.md` pitfall "Piping delete takes pipes before wires".
7. **Couplings by ID or flag, never label:** sandbox hides rows with `SandboxRestricted`; Rooms and Starting Air compare sub-tab IDs; `MapValidator` and the delete toast build text from `PathOf(subTab)` ("Scripts → Spawn Points").
8. **Stubs deleted:** Random Spawners and Triggers lose enum values, icons and the stub message. General rule: a sub-tab whose query is empty is not drawn.
9. **Palette-wide search:** a non-empty search queries all tabs (Scripts included), groups results under "Tab › Sub-tab", and picking a result switches mode and sub-tab before selecting it (delete scope, forms and hints key off the open sub-tab).
10. **Show in palette:** a Map Check issue may carry an entry key; its row gets a button that runs search for that entry.
11. **Last tab remembered** per machine in `PlayerPrefs`, stored by mode and sub-tab **name**; fallback Upper › Floors.
12. **Headings from folders,** baked by the regenerator as a `Heading` string on each entry; drawn only when a sub-tab has ≥ 20 entries (table constant). Machine folders are renamed to the heading words.
13. **Layers popover groups** (`TileLayerCategory`) realign later: Plenums → Subfloor, and "Wires and pipes" splits into Cables / Gas pipes / Disposals.

## Shape

| Tab | Sub-tab (build order) | Classifies | Delete clears |
|---|---|---|---|
| Upper | Floors | Turf, `genericType` Floor | that turf |
| Upper | Walls & Windows | Turf, Wall (incl. windows, girders) | that turf |
| Upper | Doors | Turf, Door | that turf |
| Upper | Wall Mounts | `WallMountLow` / `WallMountHigh` | wall face under cursor |
| Upper | Furniture | furniture layers under `Furniture/Generic`, `/Flora`, `/Storage` | furniture layers |
| Upper | Machines | all other furniture-layer objects | furniture layers |
| Upper | Decals | floor decal catalog | decal |
| Lower | Subfloor | `Plenum` layer (plenum, lattice, catwalk) | plenum |
| Lower | Cables | `Wire` | wire |
| Lower | Gas Pipes | pipe layers incl. `PipeSurface`, `PipeManifold` (inline pumps, valves, filters, mixers, meter, injector, `GasPump`) | pipe layers |
| Lower | Disposal Pipes | `Disposal` | disposal |
| Items | Tools · Materials & Parts · Clothing · Medical · Food & Drink · Devices · Weapons · Misc | `Items/` folder map below | item under cursor |
| Scripts | Spawn Points (`SandboxRestricted`) · Rooms · Starting Air | spawn / area / gas-marker keys | their markers; Rooms unchanged |

**Item folder → sub-tab:** `Clothing` → Clothing; `Consumables/Chemical`, `Functional/Medical` → Medical; `Consumables/Food` → Food & Drink; `Functional/Tools` → Tools; `Functional/Materials`, `Functional/Parts` → Materials & Parts; `Functional/Devices` → Devices; `Weapons` → Weapons; anything else → Misc.

**Asset moves** (within `Assets/Content/Data/TileMap/Resources/`, always with `.meta`):

| Asset | From | To |
|---|---|---|
| NukeCart | `Furniture/Generic` | `Furniture/Machines/General` |
| MachineFrame | `Furniture/Machines` | `Furniture/Machines/General` |
| ScrubberIntake | `Structures/Pipes/Atmospherics` | `Furniture/Machines/Atmos` |
| folder rename | `Furniture/Machines/Engineering`, `/Supply`, `/Atmospherics`, `/Generic` | `Power`, `Disposal`, `Atmos`, `General` |
| Medkit | `Items/Functional/Storage/Objects` | `Items/Functional/Medical` |
| HealthScanner | `Items/Functional/Tools/Medical` | `Items/Functional/Medical` |
| Mug | `Items/Functional/Storage/Substances` | `Items/Consumables/Food/Drinks` |
| ToolboxBlue | `Items/Functional/Storage/Objects` | `Items/Functional/Tools` |
| FloorTile | `Items/Functional/Generic` | `Items/Functional/Materials` |
| Boombox, IDCard, PDA, SecurityIDCard, SecurityPDA | `Items/Functional/Generic/…` | `Items/Functional/Devices` |
| FlashlightBlue | `Items/Functional/Tools/Generic` | `Items/Functional/Devices` |
| NuclearAuthenticationDisk | `Items/Functional/Tools/Command` | `Items/Functional/Devices` |
| Headset, HeadsetSecurity | `Items/Clothing` | `Items/Functional/Devices` |

Confirm each source path in the Editor before moving; any other folder whose contents land in the wrong sub-tab after regeneration is fixed by moving the asset, never by an exception list.

## Phases

### S1 — Reorganisation — **done**

Atomic: the enum, the asset and the delete switch must change together.

- Sub-tab table + fresh enum; port every switch listed in Starting state to it.
- Rewrite `MapEditorCatalogHeuristics` to decisions 1–2 and the folder map; delete keyword rules.
- Asset and folder moves above; regenerate `MapEditorCatalog.asset`.
- Eraser removal (decision 5); stub removal (decision 8); empty sub-tabs not drawn.
- Delete scope per row (decision 6); couplings (decision 7), including `MapValidator` text and `ConstructionHologramManager` toast.
- Tests: update `MapEditorCatalogMappingTests`, `MapEditorDeleteTargetingTests`, `GasMarkerAuthoringTests`, `MapEditorAreaViewTests`; add the drift test, a per-folder/per-layer classification test, a per-row delete-scope test, and a test that no row's label appears hard-coded in `MapValidator`.
- Docs: `Builds/TESTERS.md`; `tile.md` (remove Piping pitfall, update catalog lines); `map-editor-creative-hooks.md` (Scripts → Rooms, Spawn Points, Starting Air, gas-form pitfall); `area.md`, `area-authoring.md`; `FORK_STATUS.md` where it names tabs.
- Validate: `Testing/ui/validate.sh --steps compile,editmode,scenarios:MapEditor`, plus a manual pass in the editor (every sub-tab non-empty and correct, Delete per sub-tab, sandbox hides Spawn Points).

**As built, and where it differs from the plan above** (owner-approved where noted):

- **AtmosMeter and AtmosInjector are Machines, not Gas Pipes** (owner, 2026-11-07). Both sit on `FurnitureBase`, so layer-first filing makes them Machines and the furniture-layer delete clears them. The Shape table's "meter, injector" under Gas Pipes was wrong about their layer. They stay in `Structures/Pipes/Atmospherics/`, where `AtmosInlineDeviceSetup` creates them; S3 needs a heading for them (folder below the Machines root doesn't exist).
- **SteelGirderUnbolted moved** to `Items/Functional/Materials` (owner, 2026-11-07): an `ItemObjectSo` stored under `Structures/Walls`, which the folder map otherwise files as Misc.
- **Map Check grouped bodies name the path.** `MapValidator` folds per-job and operative issues into the `job-spawns` and `nuclear` checklists, so the per-issue "Add it under …" text never reached Map Check. Both grouped bodies now end with `PathOf(SpawnPoints)`.
- **The committed catalog had never loaded:** its `m_Script` GUID matched no script, so the runtime filed every asset by name-keyword fallback. Regeneration fixed the GUID and also dropped six deleted TileOverlay rows and added the missing M4. The baked asset now drives the palette.
- **Enum values start at 100** per tab (Upper 100s … Scripts 400s), so no old 0–20 value can be read as a new sub-tab.
- **Rooms is always drawn**: it lists areas, not catalog entries, so the "empty sub-tab is not drawn" rule (`MapEditorCatalog.IsShown`) skips it. Sandbox players opening Scripts land on Rooms.
- **Icons** are one baked list keyed by each row's icon path (`MapEditorIconsSo._subTabIcons`), not a field per sub-tab.
- **Tier-B recipes repointed**: `AtmosInlineDeviceSetup` writes ScrubberIntake's tile under `Furniture/Machines/Atmos`; `StructuralIntegrityPrefabSetup` maps renamed tile folders back to the unrenamed `WorldObjects` prefab folders.
- **For S3:** Furniture has 21 entries after S1, so it also passes the 20-entry heading threshold, not only Machines.
- Not machine-checked: Items delete (a raycast under the real cursor). The manual pass covered every other sub-tab's delete in a live session.

### S2 — Finding things — **done**

Palette-wide search with switch-on-pick (decision 9); Map Check **Show in palette** (decision 10); last tab in `PlayerPrefs` (decision 11). Tests: search across tabs, pick switches mode/sub-tab, Scripts searchable, preference round-trip and fallback for an unknown name.

**As built:**

- **Added after the owner's hand check (2026-11-07):** slot labels split asset names into words (`MapEditorCatalog.PaletteLabel`) on two lines, because about 47 names were cut off at one line. Map Check issues no longer open on their own, and a closed issue is one line (title, with the category on the right).
- `MapEditorCatalog.Search` returns `MapEditorSearchGroup`s in tab and build order; the grid heads each with "Upper › Machines" (`map-editor-grid-heading`, which S3 reuses). Marker rails now honour a search term; without one they keep kind order. Rooms lists areas, not entries, so search doesn't reach room names.
- A pick goes through `MapEditorSubSystem.OnPaletteEntryPicked`: select, then `MapEditorViewModel.OpenSubTab` (mode and sub-tab in one step), then the ghost. Only palette clicks switch tabs; the Dropper doesn't.
- **Clicking a mode or sub-tab clears the search** (not in the plan): with search showing every tab, a tab click would otherwise look dead.
- `MapValidationIssue.EntryKey` carries the entry. Set on missing job spawns and the nuclear checklist (first unmet requirement), and on the room warnings for a missing air alarm, scrubber, vent or APC. The button searches `MapEditorCatalog.SearchTermFor(key)` (a job, antagonist or marker name, else the asset name), opens the entry's sub-tab and un-minimises the palette. A job with no palette entry (only Assistant and Security have one) gets a toast. The new field changes the `TargetMapValidation` payload: protocol 31 → 32.
- `MapEditorLastTab` stores `"Mode/SubTab"` enum names under `SS3D.MapEditor.LastTab`. It restores on the session's first open; a stored tab under another mode, an unknown name, a number or a hidden sub-tab falls back to Upper › Floors. `SetCapabilities` also moves a sandbox player off a hidden open sub-tab.
- Tests: `MapEditorPaletteSearchTests`. The pick path itself (a `NetworkBehaviour` handler) is covered through `OpenSubTab`, not end to end.

### S3 — Headings — **done**

`Heading` string on `MapEditorCatalogEntry`, baked by the regenerator from the folder segment below the sub-tab's root; grid renders headings when the sub-tab has ≥ 20 entries.

**As built:**

- **Roots:** `MapEditorCatalogHeuristics.HeadingRoots` names them per sub-tab: Machines → `Furniture/Machines/`; Furniture → its three classifying folders. The folder below a root is the heading (split into words like slot labels); a file directly in a root, or outside every root, is **General**. Other sub-tabs bake an empty heading.
- **Furniture gets headings too** (21 entries). Owner chose "by use" (2026-11-07): Seats, Tables, Plants, Lockers, General. Moves: `Furniture/Generic/Surfaces/Tables` → `Furniture/Generic/Tables`, `Furniture/Flora/PottedPlants` → `Furniture/Flora/Plants` (tile assets only; prefabs and examine strings stay put).
- **AtmosMeter and AtmosInjector tiles moved** to `Furniture/Machines/Atmos`, so they head under Atmos. `AtmosInlineDeviceSetup` now writes furniture-layer device tiles to `MachineTileFolder`.
- `MapEditorCatalog.GroupByHeading` sorts headings by name with General last; Machines reads Atmos, Disposal, Kitchen, Medical, Power, Vendors, General. Search results stay grouped by sub-tab only, without folder headings.
- **Compact view puts headings beside the row** (owner, after the hand check, 2026-11-07): it shows one 115 px slot row, and a heading above would overflow it. There the heading (folder or search group) runs bottom-to-top in a 24 px column left of the group's first row; later rows get an empty column so slots line up. The expanded view keeps headings above.
- **Spawn Points lists Nuclear Operatives only** (owner, 2026-11-07): Traitor and Malfunctioning AI are assigned to someone already aboard, so their markers did nothing. `MapEditorSpawnCatalog.MarkerAntagonists` picks the palette's antagonists; the enum values stay so saved records still read.
- Tests: `HeadingOf` cases, the Machines General set is pinned (`EveryMachine_SitsInAHeadingFolder`), heading order, below-threshold and no-heading sub-tabs; the drift test now compares `Heading`.

### S4 — Layers groups — **done**

`TileLayerCategory`: rename Plenums → Subfloor; split Wires and pipes into Cables, Gas pipes, Disposals. Update `MapEditorLayerVisibility` and its tests.

**As built:**

- The popover reads Subfloor, Turfs, Furniture, Wall mounts, Cables, Gas pipes, Disposals, Floor decals, Items, Scripts, Areas. The three new groups sit where Wires and pipes was; the order is otherwise unchanged.
- Gas pipes holds every pipe layer (`PipeLeft`, `PipeRight`, `PipeSurface`, `PipeMiddle`, `PipeManifold`), matching the Gas Pipes sub-tab's delete scope.
- No save or wire change: layer visibility lives only in `MapEditorViewModel` and `TileLayerVisibilityService`, never persisted, so renaming enum values breaks nothing. `MapEditorLayerVisibility` and the cursor pick iterate `AllCategories` and needed no edit.
- Tests: `TileLayerCategoryMappingTests` checks each Lower layer's group, that every `TileLayer` sits in exactly one group, and the plain-word labels.

### S5 — Saved maps reference objects by ID — **done**

Added by the owner on 2026-11-07. Nobody but the owner has saved maps yet, so a save-format change costs nothing now and more with every outside map.

**Why:** saved maps store objects by name: `SavedPlacedTileObject.tileObjectSOName` and `SavedPlacedItemObject.itemName`, resolved through `RenamedTileObjects`. A rename therefore needs an alias entry and breaks the map-import tools. Stable IDs already exist: each prefab's `ObjectAssetReference.Id` is its Unity asset GUID (the generated `Data/Generated/Items.cs` lists them), and a rename keeps the GUID.

- Save the ID as the key and keep the name beside it, for readable JSON and for loading old saves. Load by ID, then by name through `RenamedTileObjects`.
- Bump `PersistenceEnvelope.CurrentSchemaVersion` (it is 1), and write the shipped maps under `Builds/Game/Data/` in the new format.
- Update `Tools/map_import/` (it writes names) and `MapImportPipeResolver` / `Ss13TypeMapper` if they emit names into saves.
- Check the wire path: whether placement commands and RPCs send names. Moving them to IDs is a protocol bump.
- Test: the old format loads; a saved and renamed asset still loads. An audit test checks that IDs are unique and that no two assets share a GUID (a deleted and recreated asset gets a new one).
- Not in scope, but to record: examine localization keys are built from the asset's folder path (`ExamineCanonicalKeyGenerator.GetNameKey`), so moving or renaming an asset changes its translation key. Decide whether S5 moves those keys to IDs too, or leaves that to a localization effort.
- After S5, renames are cheap. The owner may then shorten names such as `AtmosPipesSupply` (see S2's labels).

**As built:**

- `SavedPlacedTileObject.id` and `SavedPlacedItemObject.id` hold `GenericObjectSo.PrefabAsset.Id`, written first, with the name kept beside it. All 173 tile assets have their own prefab, so the id is unique; `TileResourceLoader` logs an error if two ever share one.
- `SavedAssetLookup.Resolve` tries the id, then the name through `RenamedTileObjects`. The name fallback also covers a deleted and recreated asset (new GUID, same name). Items now go through `RenamedTileObjects` too; before, only tiles did.
- Schema 1 → 2. Nothing reads `schemaVersion` on load: a version 1 file has no `id` and loads by name, and an older build reading a version 2 file ignores `id`. The bump marks the format and triggers a MINOR release.
- **Shipped maps stamped** by the new `Tools/map_import/asset_ids.py` (name → id from the `ObjectAssetReferences` and tile asset files, no Unity): both `StationTemplates`, the legacy `Tilemaps`, and the `testroom` and `atmostest` fixtures. With the ids removed, every file compares equal to the original. `classic_ministation.py` writes ids through the same table. The DMM importer places through the game, so its saves get ids for free.
- **Wire path unchanged.** Placement, clear and move RPCs send names, and `TileAssetCatalog` indices follow sorted names. Both sides run the same build, so a rename only needs the `ProtocolVersion` bump that `atmospherics.md` already calls for. Moving RPCs to ids would buy nothing until mismatched builds can connect.
- **Examine localization keys stay path-based** (owner, 2026-11-07: left for a later localization effort). `ExamineCanonicalKeyGenerator.GetNameKey` builds keys from the asset's folder path, so a move or rename still changes the key.
- Tests: `SavedAssetLookupTests`: id finds a renamed asset; a version 1 save without id loads by name; old pipe names go through `RenamedTileObjects`; an unknown id falls back to the name; every tile asset has a unique prefab id; every shipped map stores an id matching each name. `scenarios:NukeOps` loads the stamped test room (disk, nuke cart) through the real loader.

### S6 — Variants — **done**

Added by the owner on 2026-11-07. Several entries are variants of one object; the palette shows one slot per family and cycles its variants. This is not in `creative-mode.md` §2, so it is recorded here as a decision, not as a change to the design doc. Settled with the owner in an implementation grill (2026-11-07).

**Starting facts:** `Plant0` and `Plant7` are the only plants and `TileDirt1` is a single asset. Filters vary on two axes: `L`/`R` is the side port's side (`sideOnLeft`), `L2`/`R2` sit on the inline pipe layers instead of the plate layers (`AtmosInlineDeviceSetup.BuildFiltersAndMixers`). Manifold 3/4 differ in port count.

**Decisions:**

1. **A family is a folder named `<Family>Variants/`.** The regenerator bakes `Family` and `VariantLabel` onto each entry, beside S3's `Heading`. A family folder with one member draws as a plain entry.
2. **Each variant stays its own asset and prefab id.** Saves (S5), undo and Map Check don't change; only the palette groups them.
3. **Mirroring (L/R) is a variant,** not a transform. Mirror-as-transform is parked.
4. **Families:** Office chair (3), Plastic chair (3), Plant (2), Soda can (7), Mixer (L/R), Filter (L/R), Inline filter (L2/R2). Filters split by layer, so cycling never changes the layers a placement occupies. Manifold 3/4 stay separate entries (different objects). `TileDirt` is not a family.
5. **Input:** V next variant, Shift+V previous, beside R rotate; ‹ › arrows on the slot for the mouse.
6. **Default and memory:** the base asset first (no suffix, else first by name); the last variant is remembered per family for the session, not in `PlayerPrefs`.
7. **Labels baked from names:** family = folder name without `Variants`, split into words; variant = asset name minus the family's shared prefix, `L`/`R` → Left/Right, empty → Standard. The two plant assets are renamed to descriptive names.
8. **Slot:** current variant's thumbnail, family on line 1, variant on line 2 (S2's two lines), an "n/N" badge, ‹ › on hover or selection. The hint bar shows "V: variant" while a family is held.
9. **Heading threshold counts entries, not slots,** so Furniture (21 entries, 16 slots after S6) keeps its S3 headings.
10. **One `SelectAsset` route** for Dropper, search and Show in palette: select the asset's family slot, set it as the family's current variant, update the memory. Search matches the family label and every variant name; results show one slot per family.
11. **V mid-gesture does what R does;** confirm R's behaviour in code and record it. With no family held, V does nothing.
12. **Mixed families fail regeneration:** members must share sub-tab, heading and layer set. An EditMode test asserts every family is uniform.

**Steps:**

- **S6a — data:** move assets into `…Variants/` folders (with `.meta`), rename the plants, update tier-B path constants (`AtmosInlineDeviceSetup`, `StructuralIntegrityPrefabSetup`); add `Family`/`VariantLabel` to `MapEditorCatalogEntry` and the regenerator, the uniformity check, regenerate the catalog; tests: label baking, uniformity, drift. Check examine keys (path-based, S5) for the renames.
- **S6b — palette:** one slot per family, labels, badge, arrows; V/Shift+V in `Controls.inputactions`; per-family session memory; hint bar.
- **S6c — routing:** `SelectAsset` for Dropper, search and Show in palette; `scenarios:MapEditor`; sync `tile.md` and milestone A8.

**As built (S6a):**

- **Folders:** chair folders `Office`/`Plastic` → `OfficeChairVariants`/`PlasticChairVariants`; `Drinks/Soda` → `Drinks/SodaCanVariants`; plants into `Plants/PlantVariants/`; atmos tiles into `Atmospherics/FilterVariants/` (L, R), `InlineFilterVariants/` (L2, R2) and `MixerVariants/`. Tile assets only, moved through `AssetDatabase.MoveAsset` (GUIDs kept); prefabs, examine strings and their keys stay put. Headings are unchanged: the family folder sits below the heading folder.
- `MapEditorCatalogHeuristics.FamilyFolderOf` / `FamilyLabelOf` / `VariantLabels`; `MapEditorCatalogGeneration.FamilyProblems` / `AssignFamilies`. A mixed family makes `BuildEntries` throw, so regeneration and the drift test both fail and name the folder. Two family folders with one label in one sub-tab are also an error, since the palette groups by label.
- `AtmosInlineDeviceSetup` writes filter and mixer tiles into the family folders (and creates a missing folder); not re-run, as the assets were moved, not rebuilt.
- **Plants renamed** (owner, 2026-11-07): `Plant0` → `PlantEarth`, `Plant7` → `PlantExotic` (from their examine text, "an Earth species" / "some exotic species"), so they read Earth / Exotic. Prefab, `ObjectAssetReference`, tile asset, addressable address, the `Items.cs` constants and the shipped ministation names changed; GUIDs, and so saved ids, did not. `RenamedTileObjects` maps the old names for pre-id saves. The examine assets and their keys keep the old name (path-based keys, S5).
- **Don't run `AssetDatabasesCodeGenerator.GenerateAssetDatabasesCode` for a rename:** it writes invalid C# into `Sounds.cs` and fills `_asset` on about 90 tile prefabs (committed prefabs keep it empty) and renumbers PrefabIds. Edit the `Items.cs` constants by hand.
- Tests in `MapEditorCatalogMappingTests`: folder detection, labels, problems per kind, one-member folder stays plain, and the committed family set is pinned (`CommittedFamilies_AreTheS6Set`).

**As built (S6b):**

- `MapEditorPaletteSlot` (in `MapEditorCatalog.cs`): a family's variants, base first, with `IndexOf` / `Step` (wraps). `MapEditorCatalog.GroupFamilies` folds a family where its first member stood; `SlotOf(entry)` is the entry's whole family in its sub-tab (sandbox-hidden members left out); `SlotsFor(matches)` gives search one slot per family holding every variant, even when only some matched. Search matching the family label came in S6c.
- The grid groups by heading first (entries counted, decision 9), then folds families within each heading.
- `MapEditorViewModel.ShownVariant(slot)`: the held variant, else the family's last pick this session, else the base. `SelectEntry` records the pick, keyed by sub-tab + family. `HoldsFamily` drives the hint suffix " — V: variant" (Edit tool, family of 2+).
- Slot: `map-editor-slot--family`, two one-line labels (`__family`, `__variant`), an `n/N` badge (`__badge`), and ‹ › `__cycle` buttons shown on hover or while held. An arrow press stops propagation so the slot doesn't also pick, then goes through `AssetSelected` like a slot click.
- Input: `Tile Creator / Cycle Variant` on V in `Controls.inputactions` (`Controls.cs` regenerated by the Editor); Shift held means previous, read in `MapEditorSubSystem.HandleVariantHotkey` beside the tool hotkeys (skipped while typing). Keybinds lists it as "Next Variant (Shift: previous)".
- **R mid-gesture (decision 11):** `HandleRotate` → `SetNextRotation` turns every ghost of the drag in place and updates `_lastRegisteredDirection`; the drag goes on. V matches it: `ConstructionHologramManager.SwapSelectedObject` swaps `_selectedObject`, drops the ghosts and pool, and the next update rebuilds the drag's ghosts from the new prefab with the same direction. Outside a drag (and for items) it is `SetSelectedObject`.
- Tests: `MapEditorPaletteVariantTests` (folding and order, wrap, session memory, hint, search slots, committed Furniture keeps headings with fewer slots).
- **Cursor badge names what you place** (owner, after the first S6b look: the ghost alone is hard to read, and the examine badge named the object under the pointer instead). `MapEditorSubSystem.SyncCursorLabel` sets `ExamineSubSystem.SetCursorLabelOverride` each frame: Edit holding an entry shows `MapEditorCatalog.PlacingLabel` ("Office Chair (Blue)", the bare family for its base), Rooms shows the room being painted; other tools, the pointer over editor UI, and F7 show no badge. Examine is off inside the editor, which also stops Shift (rectangle drag) from opening detailed examine. Closing the editor clears the override. `MapEditorCatalog.DisplayLabel` is now the slot label for single entries too.

**As built (S6c):**

- `MapEditorViewModel.SelectAsset(entry, asset)` is the one route (decision 10): it swaps in the catalog's own entry by asset name (the Dropper only knows the placed object's name, so before this it held a bare entry with no family), selects it so the family remembers the variant, and opens its sub-tab when the player sees it; it returns whether the sub-tab changed. An asset the catalog lacks keeps the open sub-tab. `MapEditorSubSystem.SelectAsset` adds the tab side effects and the ghost.
- Callers: palette and search clicks, the Dropper (switches to Edit first, as before), and Map Check's Show in palette, which now also selects the entry instead of only searching for it. The Select tool still holds a bare entry: it inspects, it doesn't pick from the palette.
- `MapEditorCatalog.IsVirtualKey` (decal, spawn or gas marker key) keeps Show in palette from asking the loader for a marker and logging a missing asset.
- Search matches the family label as well as each variant's asset name ("office chair" finds the family).
- Tests in `MapEditorPaletteVariantTests`: family-label search; `SelectAsset` from a bare name opens the family's sub-tab and remembers the variant; an asset outside the palette keeps the sub-tab.

## Parked

- Gas Pipes split into runs and inline parts: only under §2's growth rule (≈ 12 entries and a clear boundary).
- Storage and Atmos Machines as their own sub-tabs: same rule.
- Mirroring as its own transform beside rotation (S6 decision 3): only once handed forms exist beyond the atmos parts; it needs a per-placement save field.
- Clothing headings: Clothing has 12 entries after headsets leave, below the threshold; add subfolders when it grows.
