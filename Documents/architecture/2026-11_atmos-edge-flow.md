> Implements: Documents/design/atmospherics.md §2 (open-tile simulation), §5 (fire), §9 (what you see); Documents/design/explosives-destruction.md §4 (breach into atmosphere)
> Touches systems: atmospherics, entities, rendering, networking-session
> Status: planned

**Progress:** Plan grilled and settled 2026-11-04, not started; scheduled in M8's slot. The consumer smoothing it builds on shipped on `codex/violent-decompression`. Serves [mvp1-nuke-ops.md](../milestones/mvp1-nuke-ops.md) M8 and must hold [test-server.md](../milestones/test-server.md) T3's 8-client breach line. Tests follow the [testing philosophy](2026-11_testing-philosophy.md).

# Atmos edge flow: pressure-driven wind on the tile grid (Nov 2026)

A hull breach should pull air, bodies and loose objects out of a room within seconds, on the existing tile grid. Today a room leaks slowly because gas only diffuses. This effort gives each open tile edge a flow with momentum, so wind becomes something the simulation computes rather than something inferred from a pressure slope. Zones are rejected: one mixture per room removes the gradient that gives wind a direction, and fire needs per-tile heat (design §5).

## Starting state (2026-11-04, `ab8e396ec` plus the working tree)

- **Transport:** `ShareGasJob` moves `Δn × speed × dt` per edge, about 1 tile²/s of diffusion. Substeps divide `dt`, so they add stability and no speed. Drain time grows with the square of room size.
- **Measured on the real `AtmosSimulation`,** one-tile hole in a sealed square room:

| Room | Time to 50% | Time to 10% |
|---|---|---|
| 6×6 | 45 s | 148 s |
| 10×10 | 134 s | 472 s |
| 13×13 | 239 s | over 600 s |

- **Prototype:** a Python edge-flow model (wave speed 12 tiles/s, friction 2/s, hole limited to 30 tiles/s) reached 50% in 3 s for 10×10 and 13 s for 20×20. These are a direction, not targets. The prototype has no temperature, composition, doors or sleeping.
- **Consumers are already continuous.** `AtmosWind.SampleAt` blends through open edges, bodies follow the field per frame by drag, and `AtmosWindView` streaks and the gas shaders read the shared flow atlas. Only the source of the field changes here.
- **Chunk borders were one-way valves until this branch** (see the atmospherics map Pitfalls). Connected air spaces now really connect, so a breach can reach further than any earlier measurement implied.

## Model

Each open edge stores a signed flow `Q` in moles per second. The edge between a cell and its east or north neighbour belongs to that cell, so storage is two floats per cell, kept beside the existing buffers.

Per substep, semi-implicit:

1. `Q += dt × (k × (Pa − Pb) − γ × Q)`: pressure accelerates the flow, friction `γ` damps it.
2. Clamp `Q` to the edge's speed limit. Vacuum edges use a lower limit, which stands in for choked flow.
3. Scale each cell's outflows so it cannot lose more than it holds.
4. Move `Q × dt` moles from the upwind cell, carrying that cell's composition and enthalpy. Bulk flow never separates gases.
5. Keep the existing equal-pressure partial-pressure diffusion as a separate path. Breath pockets still need it (design §2).

Explicit stability needs `c × dt < 0.7`, where `c = √k` in tiles per second. At 5 Hz with 4 substeps, `dt` is 0.05 s and `c` must stay under 14 tiles/s.

**Wind** at a cell comes from its four edge flows divided by local gas density, replacing the slope in `AtmosWind.SampleCell`. The flow atlas encodes the same value.

## Decisions

Settled in an implementation grill on 2026-11-04.

| # | Decision |
|---|---|
| 1 | Edge flow replaces bulk transport everywhere, not only during breaches, so opening a door between rooms at different pressures gusts too. A temporary constant keeps the old path until phase 2 passes, then it is deleted. |
| 2 | Edges are processed once each, by their owning cell, so one job writes both ends. This fixes conservation by construction. |
| 3 | The active set stays. A cell sleeps only when its pressure is settled and all its edges are below a flow threshold. A closed door or wall zeroes its edge. |
| 4 | Substeps stay fixed at 4 while any edge exceeds a threshold, and 1 otherwise. A calm station keeps today's cost. |
| 5 | Target feel: a 10×10 room with a one-tile hole reaches half pressure in about 15–20 s. Bigger holes go faster. Design §1's telegraphed catastrophe drove this. |
| 6 | One hole may drain every connected space, with closed doors as the only barrier. No firelocks in this effort (design §12). Reviewed after the playtest. |
| 7 | Knockdown is a wind speed threshold of about 5 m/s, run speed. Below it, wind adds to movement and can be fought. |
| 8 | Wind stays server-authoritative. Owners feel it by `RpcWind`; clients see it through the atlas. No new per-frame traffic. |
| 9 | Fire changes from moving air are accepted. Tests pin design §5's rules, not today's burn numbers. |
| 10 | Budgets during a Metastation breach: at most 4 ms per atmos tick, and at most 300 KB/s per client with 8 clients. Phase 0 measures both. Over on network puts [client sync](2026-07_atmos-client-visualization-sync.md) Phase 2 AOI ahead of phase 1; over on sim reviews the substep rule first. |
| 11 | Loose items are phase 4: anything with a free server `Rigidbody`, drag scaled by mass, with a cap on how many respond at once. Placed furniture stays put. Item physics is already server-only, so no prefab or ownership changes. |

Coasting velocity and fire rendering stay separate follow-ups. They read the same field and do not block this effort.

## Phases

| Phase | Deliverable | Checks |
|---|---|---|
| 0 | Reference fixtures: drain time, far-field onset and wind at distance for 10×10, a 40×8 corridor and a two-room door case. Records the table above as the baseline. Measures Decision 10's budgets on today's code. | EditMode, targeted perf run |
| 1 | Edge flow in `ShareGasJob` behind the constant. Conservation, stability and sleeping. | EditMode: mass and energy conserved in a sealed room, no sign oscillation after a step change, a sealed room settles and sleeps, a door closing zeroes flow, border edges work in both chunk orders |
| 2 | `AtmosWind` and flow encoding read edges. Old path deleted. Fire and breath regressions. | EditMode plus `scenarios:Environment` and `scenarios:Atmos` |
| 3 | Tuning and budget: a 10×10 room drains in Decision 5's time; knockdown moves to Decision 7's rule; `SS3D.Atmos.Sim` and patch bandwidth meet Decision 10. | `mppm:breach`, targeted perf run, screenshot review of a live breach |
| 4 | Loose items ride the wind (Decision 11). | EditMode for drag and the cap, `scenarios:Environment`, patch and `NetworkTransform` bandwidth re-measured |

## Risks

- **Sloshing:** too little friction makes pressure ring between rooms and keeps cells awake. Phase 1's oscillation test catches it.
- **Near-empty cells:** `Q / density` blows up as density approaches zero. Wind must clamp and fade below a floor density.
- **Network:** a chunk patch is about 6 KB, so 20 changing chunks at 5 Hz is roughly 600 KB/s per client while AOI ([client sync](2026-07_atmos-client-visualization-sync.md) Phase 2) is unshipped. Faster breaches dirty more chunks at once, which puts T3 at risk.
- **Reach:** one hole can empty every connected space (Decision 6). If a single C4 charge ruins whole rounds, firelocks need a design pass.
- **Prediction:** if `HumanoidPredictedMovement` is ever enabled on `Human.prefab`, wind must move into its replicate step, or reconcile undoes it.

## Design changes

The owner revised `atmospherics.md` §1, §2 and §6 on 2026-11-04 to allow bulk flow in open rooms, so this effort no longer deviates from the design. Until phase 2 ships, the code lags the revised sections: rooms still drain by diffusion.
