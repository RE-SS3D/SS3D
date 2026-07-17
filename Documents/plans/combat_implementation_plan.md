---
name: Combat Implementation Plan
overview: Build combat.md on top of the now-integrated foundations — the health rewrite (Phases 0–5b, zone damage + ApplyDamage), body-state animation (stance packs, swing triggers, aim IK), screen-space effects, and the atmos turf sim. Phase 4 melee already lands hits; this plan finishes melee, wires the foundation hookups, adds ranged, intent/disarm/grab, and the stamina/armor cross-system layer.
todos:
  - id: phase0-foundation-wiring
    content: "Phase 0: Verify merge + smoke-test; screen-effects←health shipped; remaining: turf O2 into LungIntake; marry melee swing animation to windup/recovery"
    status: pending
  - id: phase1-melee-complete
    content: "Phase 1: Melee to MVP — intent (help/harm) gating on the hit path, lethality tuning to 'a handful of solid hits', improvised-weapon fallback for any held item"
    status: pending
  - id: phase2-disarm-grab
    content: "Phase 2: Ctrl-disarm (strip weapon from hands) and Alt-grab (positioning control) as modifier+click interactions on the existing IntentController"
    status: pending
  - id: phase3-ranged
    content: "Phase 3: Ranged weapon vertical slice — weapon-intrinsic accuracy cone (recoil climb, movement bloom, range falloff, narrow-when-still), hitscan default, reload/cooldown, LOS via shared occlusion raycast"
    status: pending
  - id: phase4-stamina-bridge
    content: "Phase 4: Stamina<->oxy bridge (sprint/swing/sustained-fire drain; push-past-empty -> real oxy debt) per stamina.md; gate combat verbs on stamina"
    status: pending
  - id: phase5-armor
    content: "Phase 5: Per-zone armor absorption before limb damage + seal breach, per armor.md; finalize combat damage numbers once mitigation exists"
    status: pending
  - id: phase6-blocking
    content: "Phase 6 (optional for MVP): Equipment-tied timed blocking (riot shield) — reduce/negate a hit inside a timing window"
    status: pending
  - id: phase7-hardening
    content: "Phase 7: EditMode/PlayMode tests for accuracy cone, disarm, damage-into-health; update system maps + INDEX"
    status: pending
isProject: false
---

# Combat — Implementation Plan

## Design authority

Primary spec: [Documents/design/combat.md](../design/combat.md). Adjacent specs cited, not
redesigned:

| Spec | Role |
|------|------|
| [main-hud.md](../design/main-hud.md) §6 | Zone targeting — raycast aim, seven zones, reticle confirm |
| [main-hud.md](../design/main-hud.md) §7 | Intent / combat-verb chording — help/harm, Ctrl disarm, Alt grab |
| [health.md](../design/health.md) | Per-limb brute/burn/oxy model damage feeds into |
| [stamina.md](../design/stamina.md) | Stamina drain on combat actions; push-past-empty → oxy debt |
| [armor.md](../design/armor.md) | Per-zone absorption before limb damage |
| [death-cloning-respawn.md](../design/death-cloning-respawn.md) | What a lethal hit resolves into (round-end/observer) |

## Why now — the foundations are in place

This plan exists because the four blocks combat builds on all landed:

- **Health rewrite** (Phases 0–5b) — `HumanHealthController.ApplyDamage(BodyZone, MeleeDamagePacket)`,
  the zone/organ/systemic model, critical/death, and the `ZoneTargetResolver` raycast. See
  [systems/health.md](../architecture/systems/health.md).
- **Body-state animation** — Peaceful/Melee/Ranged stance packs, aim look-at IK, and melee swing
  triggers via `HumanoidCombatController` / `AnimationOrchestrator`. See
  [systems/entities.md](../architecture/systems/entities.md) and
  [2026-07_player-body-animation.md](../architecture/2026-07_player-body-animation.md).
- **Screen-space effects** — Volume overlays + hit flash, ready to drive from health. See
  [systems/screen-effects.md](../architecture/systems/screen-effects.md).
- **Atmospherics** — turf O2 available to feed oxygen debt. See
  [systems/atmospherics.md](../architecture/systems/atmospherics.md).

**Phase 4 already ships zone-targeted melee** (`SS3D.Systems.Combat`: `MeleeHitInteraction`,
`MeleeWeaponProfile` with windup/recovery, `HandHit`, fists + crowbar). This plan finishes the
model rather than starting it.

## What is already built (do not rebuild)

- Melee hit resolution: aim ray → `ZoneTargetResolver.TryResolveCombatZone` → `ApplyDamage`.
- Per-weapon `MeleeWeaponProfile` (brute/burn, `WindupSeconds`, `RecoverySeconds`, `CanSever`) and
  `MeleeRecoveryTracker` (blocks follow-up swings).
- Limb severing on sharp weapons (health Phase 5b).
- Intent framework: `SS3D.Interactions.IntentController` / `IntentType` / `IIntentProvider` /
  `IIntentRestrictedInteraction` — present, needs to gate the hit path and drive the HUD.
- Combat stance switching from held-item traits (`HumanoidBodyStateBridge.ResolveCombatStance`).

## Phases

### Phase 0 — Foundation wiring (do first)

1. **Verify the merge.** Open the project on `health-rewrite`, confirm a clean compile, and
   smoke-test humanoid movement + limp (the merge rerouted gait/limp off the deleted
   `FeetController` onto `HumanHealthController.Snapshot.MovementSpeedMultiplier` /
   `GetZoneBruteFraction`). See merge commit for the exact resolution.
2. ~~**Screen-effects ← health**~~ — **shipped** (`HealthScreenEffectMapper` + hit-flash TargetRpc).
3. **Atmos → oxygen.** Feed the occupant's turf O2 ratio into `HealthSimulation.LungIntake(atmosphereO2)`
   (currently defaults to `1f`). This makes suffocation/low-pressure real and gives combat stakes in
   breached areas.
4. **Marry swing animation to windup/recovery.** The swing telegraph (animation) and
   `MeleeWeaponProfile.WindupSeconds`/`RecoverySeconds` (data) exist separately — align them so the
   visible wind-back matches the mechanical window (`combat.md` §2).

### Phase 1 — Melee to MVP-complete

- Gate the hit path on **intent**: harm-intent targeted hits only; help-intent click does not swing.
- Tune lethality to `combat.md` §4 ("a handful of solid hits, not a DPS race").
- **Improvised weapons** (`combat.md` §2): any held item swings at a low base `MeleeWeaponProfile`;
  dedicated tools a step above; purpose-built weapons above that.

### Phase 2 — Disarm and grab

- `Ctrl`-disarm and `Alt`-grab as modifier+click interactions on `IntentController` (`main-hud.md` §7).
- Disarm strips the weapon from the target's hands; grab controls positioning. Both do real tactical
  work against ranged/melee respectively (`combat.md` §4).

### Phase 3 — Ranged (the net-new system)

- Weapon-intrinsic **accuracy cone** (`combat.md` §3): base spread widening with recoil climb,
  movement bloom, and range falloff; narrowing when still/braced. Readable via recoil kick + reticle
  bloom — no hidden roll.
- **Hitscan** default for small arms; reserve projectile travel for thrown/heavy ordnance.
- Reload / cooldown pacing.
- **Line-of-sight** reuses the shared occlusion raycast (same system as comms occlusion / hacking
  discovery) — one raycast system, no parallel implementation (`combat.md` §3).

### Phase 4 — Stamina ↔ oxy bridge

- Per `stamina.md`: sprint, swing, block, and sustained fire drain stamina; pushing past empty draws
  real oxy debt into the health model (health plan Phase 7a). Gate combat verbs on stamina.

### Phase 5 — Armor

- Per-zone flat absorption before limb damage + binary seal breach (`armor.md` §2). Reuse the same
  per-zone coverage check for defib pad contact (`death-cloning-respawn.md` §3). Finalize combat
  damage numbers once mitigation exists (`combat.md` §6).

### Phase 6 — Blocking (optional for MVP)

- Equipment-tied timed block (riot shield) reducing/negating a hit within a window (`combat.md` §2).

### Phase 7 — Hardening

- EditMode tests for the accuracy cone and damage-into-health; PlayMode for disarm + a full melee/ranged
  skirmish (`combat.md` §7 worked example). Run `update-system-docs` to sync
  [systems/combat.md](../architecture/systems/combat.md) and INDEX.

## Open questions (from combat.md §6, surfaced not decided)

- Hitscan vs. projectile split — recommended hitscan-by-default, confirm against FishNet prediction.
- Armor mitigation must land before final damage tuning.
- No skill/training accuracy modifier is assumed to exist.
