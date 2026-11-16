> Implements: Documents/plans/animation_system_design_250de599.plan.md (polish); Documents/architecture/2026-07_player-body-animation.md
> Touches systems: entities, health, combat
> Status: shipped

# Animation polish — melee torso, limp, blend-tree ownership (Jul 2026)

Follow-on to [2026-07_player-body-animation.md](2026-07_player-body-animation.md). Navigation: [systems/entities.md](../../architecture/systems/entities.md).

## Decisions

| Topic | Choice |
|-------|--------|
| Melee swing torso | Upper Body mask includes spine/chest (head excluded); Mixamo swing owns torso; look-at head-only during Attack Swing |
| Limp in combat | Yes — Injured Locomotion from Male Injured Pack enters from Peaceful/Melee/Ranged when `LimpSide != 0` |

## What shipped

1. **Melee torso authority** — `HumanoidUpperBodyMask` spine+chest on; Melee stance keeps Upper Body layer weight at 1; `AttackSwing` is Animator trigger + exit-time (no C# swing duration / `Animator.Play`).
2. **Look-at** — `HumanoidIkController` reads Upper Body state `Attack Swing` and zeros body look-at weight for that window.
3. **Injured gait** — `Injured Locomotion 2D` blend from Male Injured Pack; rebuild menu wires limp entry/exit for all stances.
4. **Injured arms** — `HumanoidBodyStateBridge` feeds `SetInjuredArms` from arm zone brute; Additive weight scales with arm damage (and yields when legs dominate stumble).
5. **Rebuild tooling** — `HumanoidLocomotionBlendSetup` builds Injured tree; optional `artifacts/force-rebuild-animator.flag` one-shot when interactive Editor holds the project.
6. **Left-hand mirror** — `Hand.Side` + snapshot `MirrorUpperBody` bool; Upper Body Hold*/Attack Swing* states use Animator mirror parameter (not Base Layer locomotion).
7. **Injured oneshots + severity** — limp `Jump`/`Turn90` use Male Injured Pack; idle blends to stumble via `InjuredLeg`; rare `Emote` → Injured Wave while severely limping.
8. **Swing variants** — `AttackVariant` 0–2 cycles horizontal / downward / backhand.
9. **Combat gait sync** — predicted movement eases world speed with anim `VelZ` (`GetAnimSpeedForScale`).
10. **Shelved art** — `Assets/Art/Animations/Misc/` and `Probably Not/` hold future Mixamo/human clips (not wired).

## Who tunes what (after polish)

| Owner | Owns |
|-------|------|
| Animator | Swing exit time, limp transitions, blend samples, upper-body mask, AttackSwing Any State, injured jump/turn/wave |
| Code | `CombatStance`, `LimpSide`, `InjuredArm*`, `InjuredLeg`, `VelX`/`VelZ`, `MirrorUpperBody`, fire `AttackSwing` trigger, Melee layer weight on/off, look-at aim smoothing |

Intentional leftovers in code: Upper Body weight lerp for stance enter/leave; locomotion velocity snap into the blend tree; look-at lerp. Do not reintroduce swing wall-clock timers.

## Out of scope / follow-ups

- Bind Jump / Turn90 in `Controls.inputed` and call `PlayLocomotionTrigger` (clips + limp gates are ready; no input yet)
- Combat windup telegraph length vs Mixamo swing (shorten Attack Swing state speed in controller, or add windup pose later)
- Side-specific limp L/R clips (pack is not side-split; `LimpSide` still set for future bias)
- Mirror Base Layer feet stance for left hand (illogical without `VelX` invert — deferred)
- Body-presentation-authority collapse path (consume Misc / Probably Not laying/stand-up clips there)
- Injured pack path-curve turn clips (walk/run left-right turns) — not FreeformCartesian samples

## Related

- Plan: [animation_system_design_250de599.plan.md](../plans/animation_system_design_250de599.plan.md)
- Design (read-only): [health.md](../../design/health.md) (visible limp), [combat.md](../../design/combat.md)
