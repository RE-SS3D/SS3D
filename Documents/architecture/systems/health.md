> Code paths: Assets/Scripts/SS3D/Systems/Health/
> Entry points: OxygenConsumerSubSystem
> Status: partial

# Health

## Overview

Partial implementation on `develop`. `OxygenConsumerSubSystem` exists; full two-tier limb/organ damage model from design spec is not yet implemented. Clean-slate rewrite is planned (purge legacy simulation first).

Client [screen-effects](screen-effects.md) already implement dying/critical, blood-loss, concussion, and related overlays, but **nothing in Health drives them yet** — wire via `ScreenEffectsSubSystem.SetEffect` when Phases 3/6 of the health plan ship.

## Start here

- `Assets/Scripts/SS3D/Systems/Health/OxygenConsumerSubSystem.cs` — oxygen consumption subsystem

## Extension points

- Future health controller, body parts, stamina per design spec and [health_implementation_plan.md](../../plans/health_implementation_plan.md).
- Screen feedback: call [screen-effects](screen-effects.md) from critical/death and vitals HUD slices — do not reimplement Volume overlays in Health.

## Depends on / Used by

- **Depends on:** [entities](entities.md)
- **Will use:** [screen-effects](screen-effects.md) (planned)

## Related docs

- Design (read-only): [Documents/design/health.md](../../design/health.md), [stamina.md](../../design/stamina.md), [armor.md](../../design/armor.md)
- Plan: [health_implementation_plan.md](../../plans/health_implementation_plan.md)
- [screen-effects](screen-effects.md)
