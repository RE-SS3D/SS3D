---
name: Combat Implementation Plan
overview: Clean-slate combat build-out per combat.md. Phase 0–1 melee, Phase 3 ranged hitscan, Phase 4 combat stamina drains (swing/fire; block deferred), and Phase 5 combat armor absorption shipped. Later phases: disarm/grab, optional blocking, hardening.
todos:
  - id: phase0-purge
    content: "Phase 0: Purge Assets/Scripts/SS3D/Systems/Combat/, weapon MeleeWeaponItemExtension prefab wiring, InteractionController melee-stance LMB intercept, orphaned IntentController; leave HumanoidCombatController + stance packs; compile-clean"
    status: completed
  - id: phase1-unified-melee
    content: "Phase 1: Unified melee MVP — Harm click → windup/recovery + RequestAttack + zone ApplyDamage; fists + improvised any-held-item + dedicated tool profiles; Help does not swing"
    status: completed
  - id: phase2-disarm-grab
    content: "Phase 2: Ctrl-disarm (force-strip hands) and Alt-grab (positioning) per main-hud.md §7"
    status: pending
  - id: phase3-ranged
    content: "Phase 3: Ranged vertical slice — weapon accuracy cone, hitscan default, reload/cooldown, shared LOS occlusion helper (no parallel raycast)"
    status: completed
  - id: phase4-stamina
    content: "Phase 4: Combat stamina drains (swing/fire/block) via StaminaController; push-past-empty already → ApplyOxyDebt"
    status: completed
  - id: phase5-armor
    content: "Phase 5: Per-zone armor absorption before limb damage + seal breach per armor.md; retune combat damage numbers"
    status: completed
  - id: phase6-blocking
    content: "Phase 6 (optional for MVP): Equipment-tied timed blocking (riot shield)"
    status: pending
  - id: phase7-hardening
    content: "Phase 7: EditMode/PlayMode tests; update-system-docs (combat map condemned → partial/shipped); INDEX sync"
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
| [armor.md](../design/armor.md) | Per-zone absorption before limb damage (Phase 5 shipped; environmental seal deferred) |
| [death-cloning-respawn.md](../design/death-cloning-respawn.md) | What a lethal hit resolves into (round-end/observer) |

## Strategic shift: clean-slate (condemned prototype)

The Phase 4 melee slice under `Assets/Scripts/SS3D/Systems/Combat/` is **condemned /
obsolete**. It is a disconnected prototype, not a foundation to extend:

- Melee-stance LMB in `InteractionController` plays swing animation via
  `HumanoidCombatController.TryHandlePrimaryAttack` and **returns without running hit
  resolution** — anim and damage are two parallel half-systems.
- `HandHit` (fists) was never prefab-wired.
- `MeleeWeaponProfile` windup/recovery is not married to the swing telegraph.
- Improvised-any-item, ranged, disarm/grab, armor, and combat stamina drains were never
  built.

**No dual-stack rule:** Phase 0 deletes the Combat folder and related wiring. New melee is
written fresh against the keep-list below. Do not slim, bridge, or run old
`MeleeHitInteraction` alongside a replacement.

See system map: [systems/combat.md](../architecture/systems/combat.md) (`Status: condemned`).

### Keep (do not purge)

| Piece | Why |
|-------|-----|
| `HumanHealthController.ApplyDamage` / damage packet into health | Health owns damage intake |
| `ZoneTargetResolver` / `BodyParts` / `ZoneTargetCollider` | Health zone targeting (health plan Phase 4) |
| `HumanoidCombatController`, stance packs, `AnimationOrchestrator.PlayAttackTrigger` | Presentation hooks from [player-body-animation](../archive/architecture/2026-07_player-body-animation.md) |
| Help/Harm via Main HUD + `IIntentRestrictedInteraction` | Intent gating |
| `StaminaController.ServerDepleteStamina` / `ApplyOxyDebt` | Ready; combat drains are Phase 4 of *this* plan |
| Screen hit-flash via health → `ScreenEffectsSubSystem` | Already wired |

### Redesign (wiring only)

Keep `HumanoidCombatController` + stance/swing APIs. **Redesign click ownership:** new combat
owns the primary-attack path and calls `RequestAttack` as feedback. Presentation must not
consume LMB alone (that was the condemned preview intercept).

## Foundations ready (build on these)

- **Health** (Phases 0–5b) — zone model, `ApplyDamage`, severing, screen-effects from snapshot.
  See [systems/health.md](../architecture/systems/health.md).
- **Body-state animation** — Peaceful/Melee/Ranged stance, aim IK, swing triggers. See
  [systems/entities.md](../architecture/systems/entities.md).
- **Stamina Phase 7a core** — regen, encumbrance, sprint, overdraw→oxy; combat drains deferred.
  See [systems/stamina.md](../architecture/systems/stamina.md).
- **Intent Help/Harm** — Main HUD + pipeline `IIntentRestrictedInteraction`.

## Phases

### Phase 0 — Purge obsolete combat (first code PR)

Delete condemned code and restore a clean interaction primary-click path:

1. Delete `Assets/Scripts/SS3D/Systems/Combat/` (all types: `MeleeHitInteraction`,
   `MeleeWeaponItemExtension`, `HandHit`, `MeleeWeaponProfile`, `MeleeDamagePacket`,
   `MeleeRecoveryTracker`, etc.).
2. Strip `MeleeWeaponItemExtension` from Crowbar / Hatchet / KitchenKnife prefabs (prefer
   Editor/`PrefabUtility`; do not hand-grow `Human.prefab`).
3. Remove melee-stance LMB intercept in `InteractionController` that calls
   `TryHandlePrimaryAttack` and returns — primary click returns to the normal interaction
   pipeline until Phase 1 owns it.
4. Delete orphaned uGUI `IntentController` if still unreferenced (Main HUD owns intent).
5. Leave `HumanoidCombatController` + stance packs + `RequestAttack` / swing triggers intact.
6. Compile-clean; smoke humanoid movement + Help/Harm toggle.

### Phase 1 — Unified melee MVP (first shippable combat)

Single primary path per [combat.md](../design/combat.md) §2:

- **Harm click always swings** (windup → connect → recovery + stamina), even with no
  collider under the reticle. Help-intent click does not swing.
- At **connect**, resolve zone from synced aim → `ApplyDamage` if in range; misses still
  pay recovery. Call `HumanoidCombatController.RequestAttack` so the telegraph matches
  `WindupSeconds` / `RecoverySeconds`.
- **Fists** + **improvised fallback** for any held item (low base profile) + dedicated
  profiles a step above for crowbar (and 1–2 tools).
- Interim lethality toward combat.md §4 (“a handful of solid hits”) — **final numbers wait
  on armor (Phase 5)**.
- HUD zone-label chip ([main-hud.md](../design/main-hud.md) §6) shipped on Main HUD
  (`ZoneTargetReticle` + `TryResolveHoverZone`).

### Phase 2 — Disarm and grab

- `Ctrl`-disarm and `Alt`-grab as modifier+click interactions ([main-hud.md](../design/main-hud.md)
  §7).
- Disarm: server force-strip from target's hands (inventory primitives exist; no combat verb
  yet). Grab: positioning control. Both do real tactical work ([combat.md](../design/combat.md)
  §4).

### Phase 3 — Ranged

Current energy content: ion rifle, laser rifle and taser share the hitscan/refill path with their own models, effects and SS14 audio. The taser drains available stamina; forced knockdown, EMP and removable cells remain unimplemented. Content recipes and current tuning: [combat map](../architecture/systems/combat.md).

- Weapon-intrinsic **accuracy cone** ([combat.md](../design/combat.md) §3): base spread,
  recoil climb, movement bloom, range falloff; narrow when still/braced. Readable via recoil
  + reticle bloom — no hidden roll.
- **Hitscan** default for small arms (confirm against FishNet prediction at start of this
  phase); projectile travel for thrown/heavy later.
- Reload / cooldown pacing.
- **Line-of-sight:** extract a **shared** occlusion helper (comms / drop / combat consumers) —
  one raycast system, no parallel implementation.

### Phase 4 — Combat stamina drains

- Per [stamina.md](../design/stamina.md): swing, block, and sustained fire drain via
  `StaminaController.ServerDepleteStamina`; push-past-empty already draws oxy debt. Gate or
  soft-penalize combat verbs as design requires (core Phase 7a does not hard-lock at zero).
- **Shipped 2026-07-23 for swing + fire** (block deferred to Phase 6 — see Implementation
  notes below).

### Phase 5 — Armor

- Per-zone flat absorption before limb damage, shipped ([armor.md](../design/armor.md) §2).
- Binary seal breach and environmental protective gear ([armor.md](../design/armor.md) §3) —
  **deferred**: no environment→health exposure pipeline exists yet to punch a hole in
  (atmospherics client sync and the remaining health systemic-pool wiring are both still open
  per `INDEX.md`); building the seal now means inventing that pipeline too, outside combat's
  scope. Revisit once atmos exposure lands.
- Retune combat damage numbers once mitigation exists ([armor.md](../design/armor.md) §2) —
  only new armor absorption defaults were picked (interim, same spirit as existing weapon
  numbers); no rebalance of shipped weapon damage this pass.

### Phase 6 — Blocking (optional for MVP)

- Equipment-tied timed block (riot shield) reducing/negating a hit within a window
  ([combat.md](../design/combat.md) §2).

### Phase 7 — Hardening

- EditMode tests for damage-into-health and (later) accuracy cone; PlayMode for melee
  skirmish and disarm when those phases land ([combat.md](../design/combat.md) §9).
- Run `update-system-docs`: combat map condemned → partial/shipped; INDEX coverage sync.

## Explicitly out of Phase 0–1

- Armor, ranged, disarm/grab, combat stamina drains
- Atmos → `HealthSimulation.LungIntake` (health/atmos follow-up)
- Body-presentation-authority rewrite; vitals HUD
- Skill/training accuracy modifiers ([combat.md](../design/combat.md) §8)

## Open questions (surfaced, not decided here)

- Hitscan vs. projectile for Phase 3 — **resolved 2026-07-23:** hitscan for small arms; projectile later for thrown/heavy.
- Armor must land before final damage tuning.
- No skill/training accuracy modifier assumed.

## Implementation notes

- **2026-07-19:** Plan rewritten from “finish Phase 4 melee” to clean-slate. Prior plan
  assumed `MeleeHitInteraction` / fists / crowbar were keepable foundation; code audit found
  that slice condemned (stance LMB ≠ damage, unwired fists, windup ≠ anim). Presentation
  (`HumanoidCombatController` + stance packs) deliberately **kept**. Doc pass only — no
  gameplay C# until Phase 0 executes.
- **2026-07-19 (Phase 0–1):** Purged obsolete Combat prototype + orphaned `IntentController`;
  moved `MeleeDamagePacket` into Health; rebuilt unified Hit path with
  `RequestAttack` telegraph on Run Primary; fists on hand prefabs; improvised fallback on
  `Item`; dedicated profiles on crowbar/hatchet/knife. Editor menu
  `SS3D/Combat/Setup Melee Prefabs` for PrefabUtility re-wiring. Lethality interim until armor.
- **2026-07-19 (test dummy):** Admin console `spawndummy` → `EntitySubSystem.ServerSpawnCombatDummy`
  (mindless Human + `CombatDummyBootstrap`). See [systems/combat.md](../architecture/systems/combat.md) Testing.
- **2026-07-20 (connect damage fix):** Swings ran but never damaged — (1) cancel-on-move used the
  hand bone so swing anim aborted windup before `StartDelayed`; (2) connect resolved from
  stance `AimYaw`/`AimPitch`, which stay stale outside combat stance. Fix: melee move-check
  uses entity root; owner syncs mouse aim via `CmdSyncMeleeAim` for connect resolve.
- **2026-07-20 (targeting + intent):** Connect uses camera ray + exclude-self + closest-point
  reach + AnatomyNode limb meshes. `C`/HUD chip toggle Help/Harm; Harm forces combat stance.
  Reticle: lock-on recharge from `MeleeRecoveryTracker` + cross flash on land. HUD intent
  polls `CurrentIntent` (interim; prefer `IntentChanged` later). `C` still also Cancel
  Interaction in `Controls.inputed` — input conflict open.
- **2026-07-20 (Harm exclusivity):** Unrestricted interactions are Help-default in
  `MatchesIntent`; Harm primary always returns after the melee attempt (no Drop/Open
  fall-through); Drop hotkey requires Help.
- **2026-07-23 (Phase 3 ranged):** Hitscan small-arms slice. Shared `SS3D.Utils.LineOfSight`
  (Drop + LocalSpeech migrated). `RangedWeaponProfile` / `RangedWeaponItemExtension` on M4;
  Harm primary branches to `CmdRunRangedFire` (accuracy cone, LOS, zone/structural); mag +
  cooldown + timed reload (E / empty fire). Reticle bloom via `ZoneReticleDriver`. Armor,
  disarm, projectile, loose ammo still deferred.
- **2026-07-23 (Phase 4, swing + fire only):** Gave `RangedWeaponProfile.M4` a nonzero
  `StaminaCost` (3, vs. melee's 8-12 — much higher fire rate) and drained it per shot in
  `CmdRunRangedFire`, mirroring the melee `TryConsumeSwingStamina` pattern. Also wired the
  winded-performance feedback from `stamina.md` §3 that neither swing nor fire had before:
  `StaminaController.ExertionPenalty` widens the ranged accuracy cone
  (`RangedWeaponProfile.ExhaustionSpreadDegrees`, new field, read in
  `AccuracyCone.ComputeSpreadDegrees`) and scales melee windup/recovery up to 1.6x
  (`MeleeHitInteraction.ComputeExertionTimeMultiplier`). `MeleeHitInteraction.ServerBeginSwing`
  now returns the scaled windup so `InteractionController.CmdRunMeleeSwing` can schedule the
  actual connect timer (`ServerScheduleMeleeConnect`) at the same lengthened duration as the
  recovery lock — the client-side optimistic lock stays unscaled since the server's
  `TargetNotifyMeleeRecovery` RPC corrects it moments later. Main HUD's ranged reticle bloom
  (`MainHudSubSystem.GetSelectedRangedBloom01`) now reads the same `ExertionPenalty` so the
  preview matches the server-fired spread. **Block drain deliberately not done** — no block
  interaction exists in code yet (Phase 6 is still pending/optional-for-MVP and owns building
  the block mechanic itself); wiring a drain-over-time onto a nonexistent interaction would be
  scope creep ahead of its own phase. Numeric costs/multipliers are a balancing placeholder,
  consistent with `stamina.md` §7 leaving exact rates unspecified.
- **2026-07-23 (Phase 5 armor):** Combat-armor absorption slice. `ArmorProfile` (per-zone flat
  brute/burn absorption + integrity, `BodyZoneMask` coverage) + `ArmorItemExtension`
  (`NetworkBehaviour`, `SyncVar` integrity, `ServerAbsorb`) follow the weapon-profile pattern.
  `HumanHealthController.ApplyDamage(BodyZone, float, float)` — the single chokepoint both melee
  and ranged already funneled through — now runs incoming damage through every worn armor piece
  covering the hit zone first (`ApplyArmorAbsorption`, layered across overlapping pieces), found
  via existing clothing containers (`ContainerType.IsWornSlot()`, `ContainerType >= ShoeLeft`) —
  no new equip/slot plumbing needed. Armor weight already flowed into `CarriedWeight` → stamina
  for free (`Item.Weight` + worn container, pre-existing). Interim test piece:
  `JumpsuitSecurity.prefab` via `SS3D/Combat/Setup Armor Prefabs`
  (`Editor/ArmorPrefabSetup.cs`). Pure-math `ArmorSimulation.ResolveAbsorption` unit-tested in
  `ArmorSimulationTests`. Environmental seal/breach, armor wear visuals, and examine-self
  integration deferred — see Phase 5 notes above.
