> Implements: Documents/design/creative-mode.md §4 ("Seeing what's connected"), §12
> Touches systems: map-editor-creative-hooks, tile, atmospherics, electricity, area, networking-session
> Status: in-progress

**Progress:** Phase 0 (shared base) built 2026-11-14: tracker, DTO and delta, subscriptions, view mode and x-ray, overlay and legend shells. Phase 1 (Atmos topology and badges) built 2026-11-14: the Atmos source and domain registration, per-chunk run and badge meshes, hover readout, legend lines and a `visual` pilot; see [Phase 1 deviations](#phase-1-deviations). Advances [map-authoring.md](../milestones/map-authoring.md) A10.

# Map Editor systems views (Nov 2026)

The Map Editor gets an **Atmos** view and a **Power** view. Each colours every network, shows device state, traces a clicked network with live numbers, and marks clear problems. The same problems also appear in Map Check. Scope was settled in a grill on 2026-11-14: rounds 1–3 at the gameplay altitude, which produced [creative-mode.md](../design/creative-mode.md) §4, and rounds 4–5 at the implementation altitude (Q1–Q12 below; Q13 was settled while building Phase 0).

## Starting state (2026-11-14, `c82326f5`)

- **Network state is server-only.** Pipe networks live in `GasPipeNetworkRegistry` and power circuits in `ElectricitySubSystem._circuits`. Both are plain classes on the server. Clients have pipes and cables as tiles in their tilemap, but no networks, pressures or circuit numbers.
- **Network records are replaced on every pipe edit** (`RebuildAround`; [atmospherics](systems/atmospherics.md) Pitfalls). Ports cache network ids against `TopologyVersion`. Nothing gives a network an identity that survives an edit.
- **Debug queries exist server-side:** `AtmosCellDebugInfo` and `AtmosPipeDebugInfo` (used by `AtmosDebugController`), and `ElectricityDeviceDebugInfo` (`ElectricitySubSystem.Debug.cs`).
- **Debug views today:** `AtmosDebugController` is an IMGUI panel opened from F1, host-only, with turf and pipe colouring, inspection, gas spawning and region waking. `ElectricityDebugGizmoDrawer` draws circuit links in the Scene view only. Neither reaches a remote client or a release build's editor.
- **Overlay precedent:** `MapEditorAreaOverlay` draws synced room membership as client-local per-chunk meshes with a Resources shader. It switches on with Scripts → Rooms and off on other tabs. Room names show as UI Toolkit labels anchored near each room centre. Room membership and names reach clients through buffered `ObserversRpc`s on `AreaSubSystem`.
- **Layer visibility:** `MapEditorLayerVisibility` → `TileLayerVisibilityService` (client-only). The Cables, Gas Pipes and Disposal Pipes tabs hide Turfs and restore them on leaving, unless the user toggled Turfs meanwhile.
- **Pause:** each sim has a `SimulationChannel` with `Paused`. `AtmosSubSystem.SimulationPaused` wraps it, and `AtmosSubSystem.StepOnce()` runs one server tick. Electricity has no step. Every peer runs its own electricity channel (tech-debt P6.1). Opening the editor hides atmos *visuals* (`AtmosRenderContext.SetRenderingSuppressed`) but keeps the sim running.
- **Map Check:** `MapValidationReport.Issues` carries typed server-authored issues with an optional camera position (Locate). The mockup's atmosphere and power checks were never built ([tile](systems/tile.md)).
- **Select tool** opens an object's setup through its own interface ([object-setup-persistence](2026-11_object-setup-persistence.md)).

## Decisions

### Gameplay (premises)

[creative-mode.md](../design/creative-mode.md) §4 "Seeing what's connected" is the spec. In short:

- Two views, Atmos and Power. Each switches on with its Lower tab, and a toolbar toggle keeps it on from any tab. While a view is on, floors and upper objects dim like an x-ray.
- Colour per network. Device badges show state and flow direction. Rooms are tinted by their APC's circuit, and consumers get status dots.
- Select traces a network: everything else dims, the network gets a name label, and a panel shows live numbers and Locate.
- Room air is a sub-toggle. Pause and step are admin-only.
- Colours stay put through edits. A plain-language legend explains the markings.
- Problem markers use a fixed v1 list and also appear in Map Check (§8).

### Implementation

| # | Decision |
|---|---|
| Q1 | **The server computes everything**: network identity, colour ids, device states and problems. It sends them to clients that have the editor open. Rejected: clients rebuilding topology from their own tilemap. Problems must match the server-authored Map Check, and client reconstruction is the class of bug that host play hides. |
| Q2 | **Two data streams.** *Structure* (segment → view id, view id → colour and label, device badges, problem markers) is pushed to subscribed editor clients **on change**. A change is a topology version bump, a circuit rebuild, a device state change or a problem change, compared with what was last sent to *that* client (the atmos dirty-tracking pitfall). *Live numbers* are sent **only for the traced network**, to the client tracing it, at about 2 Hz. Room air reuses the existing atmos chunk-patch path, and only while its sub-toggle is on. Clients without the editor open get nothing. |
| Q3 | **One shared identity tracker** (pure C#), used by pipes and circuits alike. After a rebuild, each new network takes the view id of the old network it shares the most segments with. Ties go to the lower id. Others get fresh ids. The view id indexes a fixed colour palette and drives the label ("Supply 3", "Circuit 2"). |
| Q4 | **Rendering:** client-local per-chunk line meshes for runs, in the `MapEditorAreaOverlay` style. Room tint reuses that overlay with circuit colours. Instanced world-space quads draw device badges and problem markers. UI Toolkit draws the view toggle, trace panel, legend and trace labels, anchored like room-name labels. No Gizmos, no IMGUI ([agent-first-composition](2026-07_agent-first-composition.md) UI policy). |
| Q5 | **Problem analyzers are pure and server-side, one per domain.** Each returns typed issues with tile positions. The view snapshot and `MapValidationReport.Issues` both consume them, inside this effort, as their own phase. |
| Q6 | **Pause and step** go through an admin-gated ServerRpc (`MapEditorPermissions` with admin required, refused for sandbox players). It sets `Paused` on the atmos and electricity server channels and steps them, which means adding `ElectricitySubSystem.StepOnce`. Clients keep their local electricity tick, since visuals come from server SyncVars. The server **auto-resumes** when the pausing admin closes the editor or disconnects, and when the round ends. |
| Q7 | **Retire `ElectricityDebugGizmoDrawer`** once the Power view ships. **Keep the F1 `AtmosDebugController`** for its dev-only turf inspection, gas spawning and region waking, which are out of scope for an authoring tool. |
| Q8 | **Phase order:** shared base → atmos topology and badges → atmos tracing → power → problems and Map Check → pause, step and Room air. Each phase ships on its own. |
| Q9 | **Validation:** EditMode covers the tracker, analyzers, snapshot builders and change detection. One `mppm` step checks that a pure client in the editor receives structure and a traced network's numbers. A `visual` step runs only for overlay phases. |
| Q10 | **Code split.** Analyzers, snapshot builders and the network → segment mapping live in each domain (`Atmospherics/…/SystemsView/`, `Electricity/…/SystemsView/`) behind one small interface, `ISystemsViewSource`. Subscription, transport and rendering are a `MapEditorSystemsView` piece beside `MapEditorArea`, on the existing networked hub, with no scene or prefab edits. New RPCs bump `ProtocolVersion`. |
| Q11 | **Select on a device while a view is on** both traces its network(s) *and* opens its setup as usual. A bare pipe or cable has no setup, so a click only traces. Turning a view on never changes what a click does to an object. |
| Q12 | **Milestone:** a new slice A10 in [map-authoring.md](../milestones/map-authoring.md), landing before A6 if possible. |
| Q13 | **Turning a view Off on Gas Pipes or Cables** also turns that tab's own view off, and it stays off until another sub-tab opens; a view pinned from the toolbar wins over the tab's own. Settled in Phase 0 (2026-11-14). |

## Shape

```
Server                                              Client (editor open)
──────                                              ────────────────────
GasPipeNetworkRegistry ─┐                           MapEditorSystemsView
ElectricitySubSystem ───┤                             ├─ subscribe(view) / unsubscribe
                        ▼                             ├─ MapEditorSystemsOverlay (chunk meshes, quads)
ISystemsViewSource (per domain)                       ├─ room tint            (area overlay, circuit colours)
  ├─ BuildStructure() → segments, devices             └─ UITK: toggle, legend, trace panel, labels
  ├─ Analyze()        → problems  ──────────► MapValidationReport.Issues
  └─ ReadLive(viewId) → numbers
NetworkIdentityTracker (shared, pure)
MapEditorSystemsView (server half)
  ├─ per-connection last-sent structure  ── TargetRpc on change ──►
  ├─ traced network per connection       ── TargetRpc ~2 Hz ──────►
  └─ RpcPause / RpcStep (admin only)
```

## Phases

### Phase 0 — Shared base

1. `NetworkIdentityTracker` (Q3) with EditMode tests: merge, split, a full re-create with the same segments, and a tie.
2. `ISystemsViewSource`, plus the structure DTO and a delta against the last state sent.
3. `MapEditorSystemsView`: subscribe and unsubscribe per connection (editor open and current view), and per-connection last-sent tracking. Unsubscribe when the editor closes or the client disconnects.
4. View mode on `MapEditorViewModel`: Off, Atmos or Power; on by itself with Lower › Gas Pipes and Lower › Cables, and a toolbar toggle. X-ray layer handling goes through `MapEditorLayerVisibility` with the same restore rule the Lower tabs use.
5. Overlay renderer skeleton and the legend shell (collapsible, open the first time, remembered in `PlayerPrefs` like `MapEditorLastTab`).

`ISystemsViewSource` ships in Phase 0 with `View`, `StructureVersion` and `BuildStructure()` only; it gains `ReadLive(viewId)` in Phase 2 and `Analyze()` in Phase 4.

### Phase 1 — Atmos topology and badges

1. An atmos source: segment → network per line, manifolds included (`TryGetManifoldPort`), and devices from `AtmosPortRegistry`. Badge state covers vents and scrubbers (on, off, mode), valves (open, closed), and pumps, filters and mixers (direction, from their front and back). Two-network devices carry both view ids.
2. The overlay draws runs in their network colour, with real pipe colours dimmed. Hovering names the line.

#### Phase 1 deviations

- **Deviates from Q4 ("instanced world-space quads draw device badges")** because the area overlay shader has no instancing path. Badges are world-space quads in a per-chunk mesh, one render queue above the runs, rebuilt with their chunk. A few hundred badges cost one draw per chunk, so instancing would buy nothing yet. Phase 4's markers can follow the same path, or move both to instancing then.
- **Real pipe colours are dimmed by the x-ray, not covered.** The Atmos view dims the Gas Pipes layer to the x-ray alpha (its domain's own layer, `SystemsViewDomain.OwnLayer`), because the overlay's line offsets cannot match the real pipe meshes. That layer stays first to pick (`MapEditorViewModel.IsLayerPickable`), so Select and hover still hit pipes.

### Phase 2 — Atmos tracing

1. With a view on, Select traces the clicked pipe or device (Q11). The trace dims the rest, sets a label, and shows a UITK panel with pressure, temperature, mix, volume and the attached devices with Locate. A two-network device shows both sides, labelled.
2. The live stream (Q2): the traced view id per connection; values are re-read every ~0.5 s and sent only when they change.

### Phase 3 — Power

1. An electricity source: cable circuits, producers, SMES, APC → circuit, room → APC (`TryGetEffectiveApcForDevice`), and consumer status (powered, channel off, unpowered).
2. Room tint through the area overlay in circuit colours, with a hatched pattern for rooms that have no APC. Consumer dots.
3. A power trace panel: supply against demand, producer output, SMES charge and rates, APC charge, channels and breaker; clicking a room shows its APC and consumers.
4. Delete `ElectricityDebugGizmoDrawer` (Q7). Check that nothing else uses `ElectricityDeviceDebugInfo` before trimming it.

### Phase 4 — Problems and Map Check

1. An atmos analyzer: open pipe end; a device on no pipe; a vent or scrubber whose network has no source or no sink; a powered atmos device in a room with no power.
2. A power analyzer: an APC on no cable; a circuit with no producer or storage; demand above supply; a room with consumers but no APC; an SMES on no circuit.
3. Markers in the view. Typed `MapValidationReport.Issues` with Locate; severity is a warning, never a round-start block. Plain-language strings shared with the legend.

### Phase 5 — Pause, step and Room air

1. Pause and step (Q6), including `ElectricitySubSystem.StepOnce` and auto-resume.
2. The Room air sub-toggle: subscribe that client to atmos chunk patches and lift `AtmosRenderContext.SetRenderingSuppressed` *only* for the pressure tint, never the fog or wind visuals the editor hides.

## Tests

- **EditMode:** `NetworkIdentityTrackerTests`; atmos and power analyzer tests, one fixture per problem plus a clean map; structure delta tests (no change → nothing sent; a badge change → only that badge); `MapValidationTests` gains the new issue ids.
- **Remote client:** an `mppm` step where a pure client opens the editor, turns on Atmos, receives structure, traces a network and sees its pressure change after a valve opens.
- **Visual:** `--steps visual` for Phases 1, 3 and 5 only.
- Run `Testing/ui/validate.sh --steps compile,editmode` plus the focused steps for each phase.

## Risks to check while building

- **MetaStation traffic and mesh cost.** A full structure snapshot on first open could be large. Measure it, and fall back to chunked sending, oldest first, as the atmos patches do, if it passes the existing patch budget. *Phase 1:* MetaStation's first open is 45,878 B (5,759 runs, 54 networks, 513 badges), one send under the 48 KB `MaxBytesPerSend`; larger deltas go out over several polls, removals and labels first.
- **Ports resolve their network lazily** against `TopologyVersion`. The source must read the current registry, not port caches, or a just-edited device shows its old network for a tick.
- **The electricity graph rebuild** after a world suspension (`Resumed`) must also bump the structure, or the Power view goes stale after a template load.
- **A device-state change has no single event.** Each badge-carrying device needs a cheap server-side "changed" signal, or the source polls on the atmos tick and compares. *Phase 1:* the Atmos source compares each device with its last poll (4 Hz, only while watched). On MetaStation, in the editor: a cold build walking all 54 networks takes 90 ms and a warm one 2 ms; a device added or removed re-links only the pipes beside it, 5.5 ms (4 pipes, no network walked), where it first re-walked every network (92 ms).
- **Pausing while an `mppm` client is mid-trace:** live numbers stop changing; the panel should say "Paused" rather than look frozen.

## Follow-ups

- A disposals view ([creative-mode.md](../design/creative-mode.md) §15).
- An admin toggle in normal rounds, and in-world scanners: design work in `atmospherics.md` and `electricity.md` first.
- Gas meter line choice ([#280](https://github.com/henkhooft/SS3D/issues/280)) touches the same "which line is this device on" question as the hover readout.
