> Code paths: Assets/Scripts/SS3D/Systems/ScreenEffects/
> Entry points: ScreenEffectsSubSystem
> Status: partial

# Screen-space effects

## Overview

Client-only URP Volume overlays for diegetic feedback from [main-hud](../../design/main-hud.md) §5: temperature, fire/freezing, low oxygen, dying/critical, blood-loss tunnel vision, concussion, unconsciousness, plus a momentary melee hit flash. Driven by intensity (0..1) via `SetEffect` / `TriggerHitFlash`. **Not wired to health or atmospherics yet** — only the F2 debug menu and console commands call it today.

Bootstraps itself with `RuntimeInitializeOnLoadMethod` (not in Boot.unity) so it can land without scene YAML edits.

**Condemned UI:** F2 debug Canvas (`ScreenEffectsDebugMenuView`) — do not port to UITK; delete when health rewrite absorbs debug. Volume/effect path stays until health wires it ([agent-first composition](../2026-07_agent-first-composition.md)).

## Start here

- `Assets/Scripts/SS3D/Systems/ScreenEffects/ScreenEffectsSubSystem.cs` — Volume + blackout + ember/frost particles; `SetEffect` / `TriggerHitFlash`
- `Assets/Scripts/SS3D/Systems/ScreenEffects/ScreenEffectType.cs` — sustained effect enum
- `Assets/Scripts/SS3D/Systems/ScreenEffects/ScreenEffectsDebugMenuView.cs` — F2 debug menu (lazy UI build)
- `Assets/Scripts/SS3D/Systems/IngameConsoleSystem/Commands/ScreenEffectCommand.cs` — `screeneffect <type> <0-1>`
- `Assets/Scripts/SS3D/Systems/IngameConsoleSystem/Commands/ScreenEffectHitFlashCommand.cs` — hit-flash trigger

## Extension points

- Gameplay integration: call `SubSystems.Get<ScreenEffectsSubSystem>().SetEffect(...)` from health/atmos when those systems ship the corresponding states (see [health](health.md) plan Phase 3/6).
- New sustained effect: add to `ScreenEffectType`, handle in `ScreenEffectsSubSystem` update/composite, expose in debug menu + command usage string.

## Depends on / Used by

- **Depends on:** URP Volume stack on the player camera
- **Used by:** [ingame-console](ingame-console.md) debug commands; future [health](health.md) / atmospherics

## Related docs

- Design (read-only): [Documents/design/main-hud.md](../../design/main-hud.md) §5
- Plan: [health_implementation_plan.md](../../plans/health_implementation_plan.md) (Phases 3/6 wire screen feedback)
- Effort: [2026-07_screen-space-effects.md](../2026-07_screen-space-effects.md)
- [2026-07_agent-first-composition](../2026-07_agent-first-composition.md)
