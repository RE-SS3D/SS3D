---
name: Atmos machinery
overview: Named pipe lines, real gas supply and inline pipe devices, so a station's air comes from a working atmos room
todos:
  - id: foundation
    content: "PR 1: keep gas across topology changes, pump curve, pipe rename with loader name table, line-based device attachment, stricter Map Check"
    status: completed
  - id: supply
    content: "PR 2: infinite gas source, starting network fill, small atmos room in classic-ministation through the importer"
    status: completed
  - id: inline-devices
    content: "PR 3: two-network device base on PipeSurface, pumps, valves, manifolds on a new PipeManifold layer, GasPump replaced"
    status: completed
  - id: filters-mixers
    content: "PR 4: filters, mixers, meter, injector, scrubber Toxins category, Claude Design UI for filter, mixer and meter"
    status: completed
  - id: gas-markers
    content: "Atmospherics rail in the Map Editor: gas source, line fill and room air as saved markers like spawn points, replacing the AtmosSource and AtmosFill tile objects"
    status: completed
  - id: optional-extras
    content: "Optional, if effort allows: Ignitor, ConnectorPort with canisters and Connector pipes, pipe rupture, pipe gas temperature"
    status: pending
  - id: docs-sync
    content: "Each PR: update-system-docs for touched maps; MVP2 S2 milestone row when the atmos room ships"
    status: completed
isProject: false
---

# Atmos machinery

Implements `Documents/design/atmospherics.md` §4, §6 and §7. Serves [mvp2-station-round](../milestones/mvp2-station-round.md) S2; not on the Playtest 1 path. Current behaviour: [atmospherics](../architecture/systems/atmospherics.md).

## Current state

Checked against develop 25802878f on 2026-11-05.

- `GasPipeNetworkRegistry.RebuildAround` discards affected networks and creates empty records, so any pipe edit deletes their gas. No test covers this.
- Pipes start empty. The only inflow is `TransferTurfToNetwork`, through scrubbers and pumps, so vents have nothing to supply.
- `AtmosPortFlow.ComputePumpFlowMoles` scales with the absolute differential, so a pump settles at its inlet pressure. A 10-segment network held 101.3 kPa after 2,000 ticks against a 4,500 kPa target.
- `AtmosDevicePipeResolver` takes the first network on the device tile, then its neighbours, across every layer in order. `classic-ministation` runs L1 and L3 over the same 94 tiles and has no L2, so all 11 vents and 12 scrubbers join L1 and L3 is unused. Scrubbers would feed the vents.
- `GasPump` is furniture on `FurnitureBase` that pulls from its tile into a pipe, floating 6 cm above the L4 line its model belongs on. The shipped templates checked place none.
- `MapValidationService` warns about vents and scrubbers with no pipe, but accepts any pipe on the tile or next to it.
- Scrubbers default to pulling O₂ and N₂. Plasma defaults off while Toxins defaults on and also scrubs plasma. Panic overwrites the filters and Filtering never restores them.

## Decisions

**Delivery:** one PR per todo, in order, each usable alone. The optional phase is decided when PR 4 is done.

**Pipe lines:** rename assets by what attaches to them.

| Layer | Today | New name | Attaches automatically |
|---|---|---|---|
| L1, `PipeLeft` | `AtmosPipesL1` | `AtmosPipesSupply` | Vents |
| L2, `PipeMiddle` | `AtmosPipesL2` | `AtmosPipesMixed` | Vents and scrubbers |
| L3, `PipeRight` | `AtmosPipesL3` | `AtmosPipesWaste` | Scrubbers |
| L4, `PipeSurface` | `AtmosPipesL4` | `AtmosPipesSurface` | Inline devices only |

The `TileLayer` enum keeps its names.

**Saved names:** `TileMap` skips names it can't resolve, so a plain rename would drop pipes from every map, tester maps on the sandbox server included. Add an old-to-new table at the load path; maps save under the new names. Also update `teststation.json`, `classic-ministation.json`, `Tools/map_import/` and the examine strings.

**Device attachment:** a vent tries Supply, then Mixed. A scrubber tries Waste, then Mixed. Both look on their own tile only, which covers all 23 devices in `classic-ministation`. The neighbour fallback and `AtmosDevicePipeResolver_FindsNetworkOnAdjacentTile` go. Map Check uses the same rule and covers inline devices.

**Gas conservation:** merges add moles and blend temperature by heat capacity. Splits divide by segment count. A removed segment releases its share into its tile.

**Pump curve:** full rated flow while inlet ≥ outlet. Above that, flow falls linearly to zero at max differential, then stalls. `stalled` and the gauge keep their meaning.

**Infinite source:** a tile object that holds its turf cell at a fixed mix every tick, with one variant per mix (O₂, N₂, air). Hidden in play, visible in the map editor. An existing `Scrubber` serves as the intake. No save-format change.

**Starting fill:** a tile object on a pipe tile fills that network once at round start, after `RebuildAll`. Hidden like the source, with a line chosen per variant. No save-format change.

**Inline devices:** a shared two-network base on `PipeSurface` with ports from rotation, sitting in a gap in the run. This keeps networks separate without topology changes. Pipes treat a device port as a connection, so they draw as a run rather than an open end.

**Layers:** `Pump`, `PumpVolume` and both valves are L4-only; underfloor lines reach them through manifolds. `FilterL2/R2` go on L4. `FilterL/R` and mixer plates sit on the floor in `PipeSurface` and connect to L1–L3 below. Mixers reuse the plate on L4 until art exists. `PipeSurface` isn't an underfloor layer, so plates stay visible and reachable.

**Plate layer choice:** a plate replaces the pipe tile on its own line. It connects to the line with pipes at its ports and a gap under it, so other lines can pass beneath. If that's ambiguous, Map Check warns and the device stays disconnected.

**Manifolds:** a new `PipeManifold` value appended to `TileLayer`. A manifold is a straight pipe on every line it spans, all one network, connecting at its front and back only: L1–L3 for `Manifold3`, L1–L4 for `Manifold4`. No pipes go on its tile.

**Valves:** two-network devices. Closed moves nothing; open equalises fast. Toggling never rebuilds topology, so players can't trigger whole-network rebuilds by flipping one. The cost is a brief equalisation instead of an instant merge.

**Filters:** toggle each gas, listed from `GasRegistry`. Selected gases leave by the side port, the rest continue straight. If either outlet is full the device stalls and shows it on its panel. APC power and a rated flow.

**Mixers:** two-inlet ratio and a target outlet pressure. They stop when either inlet is dry.

**Scrubbers:** add a toxic flag to `GasDefinition`. Scrubbers keep a Toxins category over every flagged gas and lose the Plasma toggle. Defaults are CO₂ only and Filtering restores them after Panic (both done in PR 3).

**UI:** filter and mixer get new panels, designed in Claude Design. Volume pump reuses the pump template; digital valve gets a minimal on/off panel; manual valve uses a direct interaction; meter uses examine and its shape key.

**Gas markers:** the Atmospherics rail, stubbed through `MapEditorCatalog.IsUnavailableScriptingSubcategory`, places saved records instead of tile objects, with the same shape as spawn points: a record type, a persistence contributor beside `spawn-points`, pins under the Scripts layer, and undoable place and clear commands. A record holds kind, position, a mix and a pressure. Mixes become gas fractions rather than `AtmosSupplyMix`, so plasma or a custom mix needs no prefab. Kinds are gas source (holds its tile), line fill (once, at round start) and room air (once: vacuum or a chosen gas, `creative-mode.md` §3 starting air). Map Check warns about a source in an unsealed room. `classic-ministation`'s source and fill convert to markers, and the tile objects and `AtmosSupplySetup` go.

## Art

`PipeMachinery.fbx` is re-exported from SS3D-Art `PipeMachinery.blend` at 2e2f826. It adds `PipesManifold3`, `PipesManifold4`, `FilterL2` and `FilterR2`, and takes newer `Ignitor`, `Meter`, `ConnectorPort`, `Pump` and `PumpVolume`. The other 9 meshes are unchanged.

Export the `Models` collection with Bake Space Transform on, `FBX_SCALE_ALL`, -Z forward, Y up, no animation, and mesh names matching object names. Prefabs use these meshes on identity transforms, so without baking they tip over 90°. Compare raw vertices with the old FBX afterwards.

Not yet opened in Unity. On first reimport, confirm `GasPump` keeps its mesh and materials.

Left out: `Vertical` pipes and the 8 pipe colour variants from `AtmosphericPipes.blend`. `Connector` pipes join `ConnectorPort` in the optional phase.

## Deviations

`atmospherics.md` was revised with this plan: §6 gained pipe lines, air supply and pipe devices, and §7 and example C now describe backpressure.

PR 1 adds one thing the plan didn't list: the pump caps a tick's flow at the moles left to reach its target. Rated flow at equal pressure would otherwise overshoot by up to 974 kPa per tick on a one-segment run. `GasPump` stays turf-to-pipe furniture until PR 3, and attaches to Surface on its own tile.

PR 2 differs from the plan in four ways:

- `classic-ministation` carries its distribution line on Mixed, not Supply. Supply can't be reached from a scrubber until PR 3's manifolds, and Mixed is the one line a scrubber and a vent both attach to. Move it to Supply when pumps and manifolds reach the room, and bring the intake up through them.
- The atmos room has one air chamber. Separate O₂ and N₂ chambers need PR 4's mixer, since two intakes on one line would give a 50/50 mix. Gas markers now cover that.
- A catwalk run joins the east and west halves of the line, which were separate networks.
- Scrubbers now share their budget across selected gases by mole fraction. They used to fill it from the first gas in filter order, so an intake sent pure O₂ and CO₂ never moved while oxygen was on the tile. Not in the plan, but the intake can't carry air without it.

PR 3 differs from the plan in five ways:

- `GasPump` keeps its name and prefab rather than being replaced: its tile asset moved to `PipeSurface`, with the 6 cm offset gone, and `AtmosPumpController` became inline. The asset still lives under `Furniture/`.
- The digital valve reuses the pump template and `PumpInterfaceController` with the set point hidden, instead of its own panel. It needed no new catalog entry, and a device with a richer panel can still get one.
- `classic-ministation` now runs distribution on Supply, as PR 2 left open. The intake's Mixed run reaches a manifold, the air pump and a second manifold into Supply. Waste crosses the catwalk so all scrubbers share one line, then ends at a manual valve, a Mixed stub and a vent on a catwalk outside the east wall.
- That waste valve starts closed. Scrubbers now take CO₂ only, so Waste fills slowly, and opening the outlet is an engineer's job.
- Scrubber defaults moved up from PR 4 and changed, at the owner's call: CO₂ only, not CO₂ + Toxins. Plasma and toxins stay off so that scrubbing them is atmos's work. `atmospherics.md` §4 still lists toxic gases under what scrubbers pull; the owner decides whether it changes. `ScrubberIntake` is a `Scrubber` that takes O₂ and N₂, placed at the chamber.
- Inline pumps show a pipe cap at each port with a network, and carry a `FlowArrow` child that only holograms show. Valves carry a pipe run instead.
- `classic-ministation.json` is the source of truth from this PR on, edited in the map editor. `classic_ministation.py` won't write without `--overwrite-map-edits`. The outlet vent runs off a cable, not an APC. By its rules Map Check should warn that it's outside every room; Map Check hasn't been run on this map.
- Manifolds changed from the plan after the owner placed them: merging pipes on the tile needed an extra pipe to look connected and joined on every side. They now carry a run of each line's own mesh, and allow two facings, because lines sit at different heights and a half turn would misplace the runs.
- Pumps start at 101 kPa rather than 4,500, so raising them is atmos's work. Supply settles near room pressure once its starting fill is used.
- Air alarm presets: Panic takes every gas at top speed with vents off, and Filtering restores speed and filters, moved up from PR 4. Custom filters set by hand are lost when Filtering is pressed.
- The manual valve shows `ValveManualHandle`, in line with the pipe while open and across it while closed. It snaps, with no animation.
- `IAmbientLoopState` moved from `AtmosPortControllerBase` onto vents, scrubbers and pumps, so valves need no sound.

The gas markers PR came before PR 4, at the owner's call, and differs from the plan in six ways:

- Room air floods the marker's room with `AtmosRoomFlood` rather than using areas, because a chamber with no APC has no area. Doors bound the room, open or not, so an open door can't fill the station. Map Check reads the same flood.
- Map Check also warns about a room air marker outside a sealed room, not only a source.
- Markers sync to clients. Spawn points don't, which leaves a remote editor with no pins; the same gap here would have blocked sandbox players, who may place gas markers.
- Sources follow the registry during a round. Line fills and room air apply when atmos starts.
- One marker per tile across all kinds, so a line fill can't share a tile with a source.
- `TileObjectSo.mapEditorOnly` went too, since only these objects used it. Seven tile assets still carry `mapEditorOnly: 0`, which Unity drops on their next save.

A scenario sends a marker through the real command RPC and checks its pin and its removal. Mouse placement, the hologram, and Select and Dropper on a marker have not been run, by test or by hand, and neither have the form's looks, since no `visual` step covers them.

PR 4 differs from the plan in eight ways:

- The meter has a read-only panel from Claude Design rather than examine and a shape key, at the owner's call. It needs no power and no ID. `atmospherics.md` §6 still says a meter reads on the device itself.
- Meters and injectors sit on `FurnitureBase` over a line, not in a gap in it. Both take the first of Surface, Supply, Mixed, Waste on their tile, so on a shared tile they can't pick another line.
- The injector has no panel: it starts closed and is opened by hand, needs no power, and pours at up to 50 mol/s, tapering as pressures level. No design came with it.
- The bundle's `atmos-gases.json` lists eight gases; the panels list `GasRegistry`, which has four. `GasDefinition` gained a symbol, panel colour and toxic flag, colours from that file.
- A filter's outlet mix on its panel is worked out from the inlet and selection, as the design does, not read from the outlet pipe. Flow is 0–200 L/s in 5 L/s steps; a mixer's ratio moves in 5% steps and its target in 100 kPa steps.
- Filters and mixers start with 101 kPa targets and nothing selected, not the design's demo values: a filter sends everything straight on until set.
- Mixers run as pumps: inlets below the outlet still feed it, with no backpressure stall. The plan named only the ratio, target and dry stop.
- Filters and mixers are silent, with no `MachineLoops` entry.

`FilterL/R` and `MixerL/R` are read as side port left or right looking toward +Z, from `FilterL2`'s geometry and the plates' raised shape. Which plate port the art means as outlet is not checked.

## Implementation notes

- `classic-ministation.json` holds 240 pipe entries over 189 distinct saved placements. It loads as 240 segments in 4 networks: 120 Supply and 120 Waste, one per line on each tile. Read the map by tile and line, not by saved placement.
- The SS13 importer still maps `/scrubbers` to Mixed, not Waste. Imported maps work because vents try Supply first, but a vent on a tile with only a scrubber pipe would join it.
- Keep port ticks allocation-free and cache networks against `TopologyVersion`, as `AtmosPortControllerBase` does.
- Dynamic device state doesn't survive template saves; `SavedPlacedTileObject` holds name, origin and direction only.
- Reimport changes in the main checkout wait for the editor to be free.
- `MapEditorCatalog.asset` names a script GUID (`3b7298e7…`) that matches no script, on develop too; `MapEditorCatalogSo.cs.meta` is `16071b6e…`. Rerunning the regenerator rewrites the asset with the right GUID and reshuffles unrelated entries. PR 3 left the GUID alone and added its rows by hand.
- Open: `HullBreach_BlowsLooseItemIntoSpace_WhereItComesToRest` in `scenarios:Environment` failed 3 of 10 runs after PR 1 and 0 of 6 on a2bf3a24a, timing out at "the wind carries the item out through the hole". The test room has no pipes and no causal path was found, so this is neither a confirmed regression nor a confirmed flake. Passing runs take about 10.6 s against a 10 s wait; log the wrench's exit time on both commits to compare.
- After the owner's hand test, flow rates moved: vents 100 mol/s at full flow until 20 kPa from the line, scrubbers 60 mol/s at full speed, and the chamber intake 100 mol/s, matching the pump. The plan's numbers were first-pass; the main hall of `classic-ministation` now goes 101 to 1 kPa in about 105 s and back to 97 kPa in about 90 s.
- One PR carries all of this rather than one per todo. The optional todo stays pending, for after this ships.

## Tests

- EditMode: merge, split and removal conservation; pump flow at, below and above inlet pressure, and stall; line attachment with Supply, Waste and Mixed; old names load through the table; plate layer choice including the ambiguous case; filter and mixer stalls.
- Scenario: the `classic-ministation` atmos room raises a room's pressure through vents from the chamber, and scrubbers move CO₂ to the waste line.
- Targeted validation per PR: `compile,editmode` plus the focused steps touched. Add `visual` for PRs 3 and 4.
