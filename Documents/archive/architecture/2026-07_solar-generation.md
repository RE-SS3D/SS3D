> Implements: Documents/design/electricity.md §2 (solar arrays)
> Touches systems: electricity, interactions-runtime, tile
> Status: shipped

# Solar generation (panels + tracker)

## Goal

Ship design §2 solar as an HV-backbone `IPowerProducer`, with diegetic manual aim and an automated tracking beacon — no machine-interface UI.

## Shipped

1. **`SolarCycle`** — stub celestial sampler (azimuth sweep, day/night intensity, eclipse windows). No world sun / station-rotation sim yet.
2. **`SolarPanel`** — `BasicElectricDevice` + `IPowerProducer`; `PowerProduction = peak × intensity × aimFactor`. SyncVar aim + **Rotate** interaction (45° steps).
3. **`SolarTrackingBeacon`** — world Toggle on/off; while on, aims nearby panels at current sun azimuth. Not a producer; no APC/MI.
4. **Prefab recipes** — `SolarPrefabSetup` + `ElectricityContentPrefabRecipes` (`SS3D/Electricity/Run Content Prefab Recipes`). Wired `SolarPanel.prefab` and `SolarTrackingBeacon.prefab`.
5. **EditMode** — `SolarGenerationTests` (cycle math, production, rotate, tracker radius).

## Defaults

| Knob | Value |
|------|-------|
| Panel peak | 2 kW |
| Cycle period | 600 s |
| Eclipse fraction | 0.2 (centered on peak day) |
| Tracker radius | 8 Unity units |

## Explicitly deferred

- Real station rotation / eclipse from world physics (replace `SolarCycle` stub)
- Machine-interface panels for tracker/panels (design: diegetic only)
- Map-authored solar farms
- Reactor (design §2 baseline)

## Related

- System map: [electricity](../../architecture/systems/electricity.md)
- Design (read-only): [Documents/design/electricity.md](../../design/electricity.md) §2
- Prior follow-up flag: [electricity_kwh_foundation](../plans/electricity_kwh_foundation_917ccdbc.plan.md) “Out of scope — Solar generation”
