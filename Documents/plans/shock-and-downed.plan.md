---
name: Shock and Downed
overview: Trauma puts a body down before it bleeds out. Shock builds from hits, drives a conscious Downed stage before Critical, and flinch stops cancelling actions. Serves MVP1 M1s
todos:
  - id: shock-core
    content: "PR 1: ShockSimulation (hit shock, tier spikes, armor bleed-through, incoming-pain build-up, decay to a wound/blood floor, Downed/Critical/Stand thresholds), HealthState.Downed, snapshot fields, HighShock critical flag, stamina cap, protocol bump, M4 tuning tests"
    status: in_progress
  - id: downed-body
    content: "PR 2: BodyPresentationState.Downed through BodyPresentationIntent, crawl wired to HumanCrawl.anim, Downed capabilities (one-handed use with wide reticle, no two-handed fire, draggable), auto stand-up with hysteresis, limb rules (both legs, arm drop, head spike)"
    status: pending
  - id: help-up
    content: "PR 3: Help up interaction, unsteady window after standing, splint gate for two broken legs"
    status: pending
  - id: feedback
    content: "PR 4: Shock alert, staged screen effects and build-up warning, examine lines, onlooker stumble/hunch, groan and gasp audio by state"
    status: pending
  - id: flinch-trip
    content: "PR 5: flinch from pre-armor damage that never cancels actions, Staggered kept for heavy hits only, victim view kick, wounded-leg sprint trip"
    status: pending
  - id: acceptance
    content: "PR 6: PlayMode scenarios (M4 burst to Downed, bandage and stand, hardsuit operative, Downed operative is not a crew win), remote-client check, update-system-docs and M1s row"
    status: pending
isProject: false
---

# Shock and Downed

Implements `Documents/design/health.md` §1, §4 (Shock, Downed, Critical), §5 (limbs and staying upright), §6 (helping up, splint, transfusion), §7 (Shock alert, onlooker table), and `Documents/design/combat.md` §5 (flinch, sprint trip), `Documents/design/stamina.md` §2 (shock lowers the cap). Serves [mvp1-nuke-ops](../milestones/mvp1-nuke-ops.md) M1s.

Painkillers and stimulants (`health.md` §6) are out of this plan. They need drug items and a dose model, and come after Playtest 1.

## Decisions

**Where shock lives:** a pure static `ShockSimulation` beside `HealthSimulation` and `OrganSimulation`, called from `HumanHealthController`. It holds two values on the controller: `Shock` (0–100+) and `IncomingPain` (the not-yet-converted part). No new component on `Human`. EditMode tests cover all of it ([agent-first composition](../architecture/2026-07_agent-first-composition.md)).

**Hit shock:** `ApplyDamage` already resolves armor first. `ApplyArmorAbsorption` also returns what it absorbed. Each hit adds to `IncomingPain`: `(through × zoneWeight × typeWeight) + (absorbed × ArmorShockFraction × zoneWeight) + tierSpike`, where `tierSpike` comes from the zone's severity before and after `RefreshZoneDerivedState`. Severing adds the Severed spike.

**Build-up:** the 1 Hz health tick moves `IncomingPain` into `Shock` at a rate that empties it in about `ShockBuildSeconds`. At 1 Hz that means 3 tick steps, which is acceptable for "return fire, then crumple". Do not add a faster tick for this. The snapshot carries `IncomingPain`, so the warning cue starts as soon as the hit lands, not on the next tick.

**Decay and floor:** each tick with no pending pain, shock drains by `ShockDecayPerSecond` towards a floor. The floor is the sum of per-zone values for open, untreated wounds (bandaged zones contribute nothing), plus `(1 − BloodVolumeRatio) × LowBloodShockScale`.

**States:** append `HealthState.Downed = 4` (values stay stable for saves and the wire). Evaluation order: Dead, CardiacArrest, Critical (shock ≥ `ShockCriticalThreshold` joins the existing systemic and brain checks as `HealthCriticalFlags.HighShock`), Downed (shock ≥ `ShockDownedThreshold` or both legs Disabled/Severed, and the body was not already standing back up), Healthy. Leaving Downed needs shock < `ShockStandThreshold` (the hysteresis) and at least one usable leg. Critical's existing heart drain runs unchanged. Shock never drains the brain or heart directly.

**Presentation:** append `BodyPresentationState.Downed`. `BodyPresentationIntent.FromSnapshot` maps `HealthState.Downed` to it. That stays the only collapse path (health map: do not add a parallel one). `Ragdoll.ServerSetPresentation(Downed)` does not ragdoll. It drives `HumanoidBodyStateMachine` into `BodyState.Crawling` and sets the `IsCrawling` animator bool, which exists but nothing sets yet, using `HumanCrawl.anim`. Critical keeps `Collapsed` (ragdoll). Standing up from Downed reuses the get-up path from the [humanoid animation pass](../architecture/2026-11_humanoid-animation-pass.md).

**Downed capabilities:** `BodyCapabilities.ForState(Crawling)` already allows hands and interaction. On top of it: movement at `DownedCrawlSpeedMultiplier`; ranged fire refuses any weapon where `TwoHandedWeaponRules.RequiresBothHands` is true; one-handed weapons get `DownedSpreadMultiplier` on the accuracy cone. Dragging, radio and talking are unchanged. Held items stay held.

**Limb rules:** Downed from both legs at Disabled or Severed is a state rule in `HealthSimulation`. A splinted leg counts as usable. An arm crossing into Disabled drops what that hand holds on the server and refuses new pickups. A head crossing into Severe adds the head spike plus a blur intensity in the snapshot.

**Flinch:** today `TryApplyHitFlinch` runs on post-armor brute and calls `OnHitReceived`, which puts the body into `Staggered` (`CanUseHands = false`). That breaks combat.md §5's "a flinch never cancels". Ordinary hits play only the additive `Flinch` trigger, scaled by pre-armor damage, so a hardsuit still flinches. `Staggered` is kept for hits at or above `HeavyHitStaggerDamage` (point-blank or big melee). The victim's view kick is an owner `TargetRpc` next to `RpcHitFlash`.

**Sprint trip:** in `ApplyDamage`, a hit on a leg zone already at Wound or worse while the body is sprinting (from `SprintExertion` / `StaminaController`) calls `Ragdoll.ServerKnockdownAtLeast(SprintTripSeconds)`.

**Stamina cap:** `StaminaController` already calls `ApplyModifiers(maxScale, regenScale)`. Multiply `maxScale` by `Lerp(1, ShockStaminaCapFloor, shock / ShockDownedThreshold)`.

**Help up:** a target interaction on a Downed `Human` that is offered when the target's shock < `ShockDownedThreshold`, it has at least one usable leg, and it is not Critical. It is a short timed action with the existing progress ring. It sets shock to the stand threshold, and the body stands and gets `UnsteadySeconds` of reduced speed and no sprint.

**Feedback:**
- `HealthAlertStackMapper` gets a Shock alert: warning from `ShockWarningThreshold`, critical at Downed.
- `HealthScreenEffectMapper` gets staged vignette, desaturation and muffle intensities, plus a pulse while `IncomingPain` would carry shock past Downed.
- `HealthPersonalAudioMapper` drives the heartbeat for the same cue.
- `CharacterExamineHealthBuilder` gets the four lines from health.md §7.
- Onlookers see a stumble and hunch above `ShockWarningThreshold`, made from the existing limp blend and a slower gait in the first pass. A dedicated hunch layer only if the playtest asks for it.
- Groans while Downed and gasps while Critical are positional audio on a slow random timer. There are no groan clips today (`HealthAudioTrackIds` has gasp, scream, choke); source them from the [art index](../art-asset-index.md) or SS14 audio.

**Nuke Ops:** no rule change. The crew-win check already needs operatives dead (`antagonist-content.md` §6). PR 6 adds a scenario that proves a Downed or Critical operative does not end the round.

**Protocol:** PR 1 and PR 2 change `HealthSnapshot` and the presentation enum. Bump `ProtocolVersion.Current` in each (46 at the time of writing).

## Tuning

Starting values. The M4 test in PR 1 pins the outcome, not the constants.

| Constant | Start | Why |
|---|---|---|
| `ShockDownedThreshold` / `ShockCriticalThreshold` / `ShockStandThreshold` | 60 / 100 / 40 | Hysteresis gap of 20 |
| `ShockWarningThreshold` | 35 | Stumble, alert warning |
| Zone weights: chest, groin / head / arms, legs | 0.35 / 0.5 / 0.15 | Center mass drops people |
| Burn type weight | 1.3 | Burns hurt more |
| Tier spikes: Bruised / Wound / Severe / Disabled / Severed | 2 / 5 / 8 / 10 / 20 | |
| `ShockLimbSpikeScale` | 0.5 | Arms and legs take half the spikes (see Deviations) |
| `ArmorShockFraction` | 0.15 | Hardsuit operative goes down after a focused response |
| `ShockBuildSeconds` | 3 | |
| `ShockDecayPerSecond` | 3 | Downed at 60 with wounds bandaged stands in about 7 s |
| Floor per open zone: Wound / Severe / Disabled / Severed | 4 / 8 / 12 / 20 | |
| `LowBloodShockScale` | 60 | At the 40 % critical blood line the floor alone reaches 36 |
| `DownedCrawlSpeedMultiplier` / `DownedSpreadMultiplier` | 0.25 / 3 | |
| `ShockStaminaCapFloor` | 0.5 | |
| `HeavyHitStaggerDamage` | 30 pre-armor | M4 (18) flinches, point-blank shotgun staggers |
| `SprintTripSeconds` / `UnsteadySeconds` | 2.5 / 3 | combat.md §5 knockdown window |

**Targets (design Q1: about half a magazine):** M4 is 18 brute per round, 30 rounds. Unarmored, spread over chest and groin: Downed after 6–9 landed hits, Critical after 10–14. Limbs only: limping, not Downed, before about 12 hits unless both legs go. The operative hardsuit: Downed after roughly twice the unarmored count. Write these as EditMode tests in `ShockSimulationTests` and tune the table until they pass.

## Tests

- **EditMode, `ShockSimulationTests`:** zone and type weights; tier spikes fire once per tier crossed; armor bleed-through; build-up converts over about 3 ticks; decay stops at the wound floor; a bandage drops the floor; low blood raises it; Downed/Stand hysteresis; both legs force Downed whatever the shock; shock ≥ Critical sets `HighShock`; shock alone never drains the brain; M4 targets above.
- **EditMode:** `BodyPresentationIntent` maps Downed; stamina cap scale; two-handed weapons refused while Downed.
- **PlayMode scenarios (PR 6):** combat dummy takes an M4 burst → Downed → bandage → stands. A further burst → Critical. A hardsuit dummy needs more. A Nuke Ops round with a Downed operative does not end.
- **Validation per PR:** `Testing/ui/validate.sh --steps compile,editmode` plus the health and combat scenario steps; `visual` for PR 2 and PR 4.

## Deviations

- **Limb tier spikes are halved** (`ShockLimbSpikeScale = 0.5`). With flat spikes, M4 hits spread over both arms reached Downed at 9 hits (the Disabled crossings alone add 25 per arm), short of the "not Downed before about 12" target. Halved, it is 13. The torso and head keep the full spikes.
- **The hardsuit target is about 1.6×, not 2×.** Through-damage still crosses wound tiers, so the spikes land on the armored body too, and the damage term itself only falls from 6.3 to 3.9 per chest hit (10 through plus 15 % of the 8 absorbed). Unarmored center mass downs at 6 hits, the hardsuit at 10. `ShockSimulationTests` asserts 1.5×–3× and within one magazine. Getting to 2× needs either a lower torso spike while armored or a lower `ArmorShockFraction`, and the second barely moves it.
- **A third shock value.** `ShockState` holds `BuildPerTick` beside `Shock` and `IncomingPain`: each hit re-spreads all pending pain over the next `ShockBuildSeconds` ticks, so the conversion takes exactly 3 ticks after the last hit instead of tailing off. It is server-only and not in the snapshot.

## Implementation notes

- PR 1 (shock-core): `ShockSimulation.cs` (pure) + `ShockState`; constants in `HealthConstants` under "Shock". `HealthSimulation.EvaluateHealthState`/`BuildSnapshot` gain overloads taking zones, shock and the previous state; the old overloads pass no shock. `HealthSimulation.ApplyZoneHit` is the shared zone-damage step for the controller and the tests.
- The both-legs rule landed in PR 1, since it lives in the same state evaluation. Leaving Critical counts as "was down", so a body whose shock falls from 100 keeps Downed's lower stand line.
- Until PR 2, `HealthState.Downed` has no presentation: `BodyPresentationIntent` maps it to Locomotion, so the body stays standing. `LocalSpeechEmitter.CanSpeak` allows Downed.
- Shock-only Critical has no heart drain of its own (the plan keeps shock off the organs). In practice the hits that reach it leave Severe/Disabled torso wounds whose bleeding supplies the countdown. Environmental burn (fire, cold) adds no shock: it is not a hit.
- `RestoreSystemicPools` (the `heal` command, sandbox resets) clears shock.
