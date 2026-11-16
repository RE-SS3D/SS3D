> Implements: [Documents/design/health.md](../../design/health.md) (critical / exposure presentation), [Documents/design/atmospherics.md](../../design/atmospherics.md) (turf sample → body), [Documents/design/audio.md](../../design/audio.md) §4/§6 (personal + alert cues), [Documents/design/main-hud.md](../../design/main-hud.md) §5 (screen overlays)
> Touches systems: health, atmospherics, screen-effects, audio, entities, stamina
> Status: shipped (MVP1 M8 exposure + M7 feel polish; armor seal / internals / air-alarm audible alarms deferred)

# Health env + feel polish (Jul 2026)

Phased health-feel pass that closed the silent gaps between vitals, presentation, turf atmosphere, and audio. Advances [mvp1-nuke-ops.md](../../milestones/mvp1-nuke-ops.md) **M7** (crit feel) and **M8 exposure** without seal/internals. Cursor plan: `health_feel_integrations` (`.cursor/plans/`). Navigation: [systems/health.md](../../architecture/systems/health.md), [systems/audio.md](../../architecture/systems/audio.md), [systems/screen-effects.md](../../architecture/systems/screen-effects.md).

## Shipped

| Phase | What |
|-------|------|
| 1 Critical ragdoll | `BodyPresentationIntent` collapses on `HealthState.Critical` (and cardiac), not only `!IsConscious` |
| 2 Critical screen FX | Dying vignette preferred over LowOxy washout; F2 solos not wiped by health mapper |
| 3 Turf → health | Oxy math fix; O₂→CO₂ breath exchange; PO₂ breathability; hot/cold/fire all-zone burn; pressure → lungs; alert/screen merge; `atmosdamage` toggle |
| 4 Blood VFX | On-hit spray ObserversRpc + tuned trickle |
| 5 SFX | FreeSound heartbeat/heavy breathing + SS14 beep/blood/flesh + SS3D-Art gasp/choke/scream; health breathing max-merged with stamina; positional hit/gasp/scream |

Also on this branch (adjacent feel): stance-aware Base `Flinch` + soft Additive gut overlay ([entities](../../architecture/systems/entities.md)).

## Deferred (still M8 / later)

- Armor environmental seal / breach ([armor.md](../../design/armor.md) §3)
- Internals tanks
- Air alarms that **audibly** alarm (controllers exist; cue wiring open)
- Thin fire extinguisher response
- Vitals UITK cluster / examine-self (health Phase 6)
- `Mix_ShoulderHitAndFall` knockdown presentation

## Key files

- `BodyPresentationIntent`, `HealthScreenEffectMapper`, `HealthEnvironmentExposure`, `AtmosAlertStackMapper` / `AtmosScreenEffectMapper`
- `WoundVfx.PlayImpactBurst`, `HealthPersonalAudioMapper`, `HealthAudioTrackIds`, `CombatAudioTrackIds.FleshHit`
- `PersonalAudioSubSystem` stamina/health breathing max-merge
- `AtmosDamageCommand` / `HealthEnvironmentSettings.AtmosphericDamageDisabled`
