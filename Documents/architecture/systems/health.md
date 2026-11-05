> Code paths: Assets/Scripts/SS3D/Systems/Health/
> Entry points: HumanHealthController, HealthSimulation, OrganSimulation
> Status: partial
> Verified: ae1a54ed1 — 2026-11-05

# Health

## Overview

Greenfield rewrite per [health_implementation_plan.md](../../plans/health_implementation_plan.md). Phase 1 shipped bleeding, bandage, and VFX. Phase 2 wires asset-backed organs into pool math, cardiac arrest, and movement debuffs (`Snapshot.MovementSpeedMultiplier` — consumed by humanoid gait/limp presentation after the develop integration). Phase 3 adds multi-threshold critical state, cardiac arrest → defib window, and chest defibrillation. Phase 4 adds BodyParts raycast zone resolution for melee combat. Phase 5 adds field treatments (burn dressing, splint, O2, CPR, transfusion, antitoxin). Phase 5b adds limb severing (zone `IsSevered`, anatomy hide, world drops, head mind-swap). Bleeding visuals now use tuned particle streams plus URP Decal blood marks (body + floor). Bleed drain uses `BleedingBloodDrainScale = 0.010` with oxy gain / arrest brain drain synced so hypoxia tracks bleed (see plan hemorrhage tuning).

Local-owner [screen-effects](screen-effects.md) are driven from `HealthSnapshot` via `HealthScreenEffectMapper` (dying/critical, blood-loss tunnel vision, oxy debt, concussion, unconscious) plus hit flash on `ApplyDamage`, and turf temp/fire via `AtmosScreenEffectMapper` from `HealthSnapshot.Environment`. Main HUD alert icons merge `HealthAlertStackMapper` + `AtmosAlertStackMapper` (Hot/Cold/pressure/Fire + LowOxygen from debt or turf). Personal [audio](audio.md): heartbeat + labored breathing (`HealthPersonalAudioMapper`, max-merged with stamina); alert ding; positional flesh/blood/gasp/scream on damage and critical/vacuum edges. Server `TickHealthFromEnvironment` samples the occupant tile each 1 Hz tick: O₂ partial-pressure breathability, O₂→CO₂ breath exchange, plasma toxin intake, hot/cold/fire burn on **all zones**, and low/high/vacuum pressure → **lung** barotrauma (not chest burn). `atmosdamage off` / Health Debug checkbox skips turf coupling (`HealthEnvironmentSettings.AtmosphericDamageDisabled`). Examine Tier 0/1 health lines shipped via `CharacterExamineHealthBuilder` (see [examine](examine.md)); vitals cluster UITK remains Phase 6 remainder.

**Stamina Phase 7a core shipped:** see [stamina](stamina.md) — push-past-empty calls `ApplyOxyDebt`; obsolete `StaminaBar` purged from PlayerCanvas. Melee swings and ranged fire drain stamina ([combat](combat.md)); block drain is deferred.

Phase 0d strips legacy health components from `Human.prefab` and rewires a thinner root — do not dual-stack or grow the mega-prefab ([agent-first composition](../2026-07_agent-first-composition.md), [health_implementation_plan.md](../../plans/health_implementation_plan.md) Phase 0d).

**Body presentation:** Health emits collapse intent only (`BodyPresentationIntent` → `Ragdoll.ServerSetPresentation`). Death is `Human.Kill` → `ServerDeathRagdoll`, applied before the ghost hand-off so mindless bodies (combat dummies) ragdoll too ([entities](entities.md)). Do not add a parallel collapse path — see [2026-07_body-presentation-authority.md](../../archive/architecture/2026-07_body-presentation-authority.md) (shipped).

## Start here

- `Assets/Scripts/SS3D/Systems/Examine/CharacterExamineHealthBuilder.cs` — Tier 0/1 examine lines (reads `Snapshot` + `DebugDetail`; owned by examine UI, not a Human prefab component)
- `Assets/Scripts/SS3D/Systems/Health/HumanHealthController.cs` — server tick, damage/treatment, organ registration, snapshot SyncVar + `SnapshotChanged`
- `Assets/Scripts/SS3D/Systems/Health/BodyPresentationIntent.cs` — snapshot → `BodyPresentationState` (Collapsed / Locomotion / Dead mapping)
- `Assets/Scripts/SS3D/Systems/Health/HealthScreenEffectMapper.cs` — local-owner snapshot → `ScreenEffectsSubSystem` intensities
- `Assets/Scripts/SS3D/Systems/Health/HealthAlertStackMapper.cs` — snapshot → health alert signals (no MainHud types); Main HUD merges with atmos
- `Assets/Scripts/SS3D/Systems/Health/HealthEnvironmentExposure.cs` / `HealthEnvironmentState.cs` — turf breathability, env burn, breath moles
- `Assets/Scripts/SS3D/Systems/Health/EvaGearController.cs` — internals and magboot switches on Human; finds the tank (suit storage, pocket, hand), and turns a switch off when its gear goes away. `MagbootsAnchored` reads synced state, so the owner's movement and every peer's footsteps agree with the server's wind check
- `Assets/Scripts/SS3D/Systems/Health/MagbootItemExtension.cs` — marks one boot; `EvaGearController` needs one on each foot
- `Assets/Scripts/SS3D/Systems/Health/GasTankItemExtension.cs` / `GasTankMix.cs` — server-side moles per gas with a synced fill fraction; no pressure model or atmos link
- `Assets/Scripts/SS3D/Systems/Health/AtmosAlertStackMapper.cs` / `AtmosScreenEffectMapper.cs` — synced Environment → alerts / HotRoom/Cold/Fire screens
- `Assets/Scripts/SS3D/Systems/IngameConsoleSystem/Commands/AtmosDamageCommand.cs` — `atmosdamage [on|off|status]`
- `Assets/Scripts/SS3D/Systems/Health/HumanAnatomyController.cs` — limb severance visuals, world drops, head mind-swap (Phase 5b)
- `Assets/Scripts/SS3D/Systems/Health/HealthSimulation.cs` — pool math, severity/bleeding, critical/death evaluation
- `Assets/Scripts/SS3D/Systems/Health/OrganSimulation.cs` — zone→organ damage, organ tick drains, perfusion, limb multipliers
- `Assets/Scripts/SS3D/Systems/Health/OrganInstance.cs` — organ registration on Human prefab / organ item prefabs
- `Assets/Scripts/SS3D/Systems/Health/Interactions/BandageInteraction.cs` — stop bleeding (Phase 1)
- `Assets/Scripts/SS3D/Systems/Health/HealthPersonalAudioMapper.cs` — local-owner heartbeat + labored breathing (oxy/critical); max-merged with stamina in `PersonalAudioSubSystem`
- `Assets/Scripts/SS3D/Systems/Health/HealthAudioTrackIds.cs` — gasp/choke/scream/blood clip ids
- `Assets/Scripts/SS3D/Systems/Health/WoundVfx.cs` — per-zone trickle + **on-hit impact spray** (`PlayImpactBurst`); anchors prefer armature `ZoneTargetCollider` bones; trickle/decal/floor drip scale with snapshot bleed rates; spray is ObserversRpc from `ApplyDamage` / sever (not bleed-onset alone). Hit flesh/blood SFX play server-side alongside spray.
- `Assets/Scripts/SS3D/Systems/Health/HealthSnapshot.cs` — synced vitals + `BleedingRatePacked` / `TotalBleedingRate` for client VFX
- `Assets/Scripts/SS3D/Systems/Health/BloodDecalSpawner.cs` — pooled URP floor blood decals
- `Assets/Scripts/SS3D/Systems/Health/BleedingVfxCatalog.cs` — Resources catalog; tints white mask splatters for Decal `Base_Map`
- `Assets/Scripts/SS3D/Rendering/URP/DecalRenderingLayers.cs` — floor vs character DecalProjector rendering-layer masks
- `Assets/Content/WorldObjects/World/VFX/Health/BloodDecal.mat` + `BloodFloorDecal.prefab` — URP Decal assets (keep **Opaque**)
- `Assets/Scripts/SS3D/Systems/Health/Interactions/BurnDressingInteraction.cs` — zone burn heal (Phase 5)
- `Assets/Scripts/SS3D/Systems/Health/Interactions/SplintInteraction.cs` — limb splint (Phase 5)
- `Assets/Scripts/SS3D/Systems/Health/Interactions/OxygenTreatmentInteraction.cs` — oxy debt relief (Phase 5)
- `Assets/Scripts/SS3D/Systems/Health/Interactions/CprInteraction.cs` + `HandCprExtension.cs` — chest CPR (Phase 5)
- `Assets/Scripts/SS3D/Systems/Health/Interactions/TransfusionInteraction.cs` / `AntitoxinInteraction.cs` — medkit systemic treatments (Phase 5)
- `Assets/Scripts/SS3D/Systems/Health/Interactions/DefibrillatorInteraction.cs` — chest-zone defibrillation (Phase 3)
- `Assets/Scripts/SS3D/Systems/IngameConsoleSystem/Commands/DefibCommand.cs` — admin defib testing
- `Assets/Scripts/SS3D/Systems/Health/HealthDebugController.cs` — IMGUI overlay (F1 → Health) for full zone/organ/pool inspection
- `Assets/Scripts/SS3D/Systems/Health/HealthDebugDetail.cs` — per-zone/per-organ SyncVar payload for debug UI
- `Assets/Scripts/SS3D/Systems/Health/ZoneTargetResolver.cs` — point + combat/hover raycast zone resolution, groin banding, reticle labels
- `Assets/Scripts/SS3D/Systems/Health/HealthLayers.cs` — `BodyParts` layer mask for combat raycasts

## Extension points

- Map Editor invincibility (`EntityInvincibility`) gates damage, ordinary severing and turf exposure; forced severing may bypass it. This per-connection playtest setting outlives the editor overlay ([tile](tile.md)).

- `IHealthEffectModifier` — virology/chemistry/stamina deltas (Phase 7+)
- `ApplyDamage(BodyZone, MeleeDamagePacket)` — melee combat input (`MeleeDamagePacket` lives in Health)
- `ApplyDamage(BodyZone, brute, burn)` — direct damage (console `hurt`, future sources)
- `ApplyTreatment(...)` — zone treatments including splint flag (Phase 5)
- `ApplyBloodTransfusion` / `ApplyOxyRelief` / `ApplyAntitoxin` / `ApplyCpr` — systemic field treatments (Phase 5)
- `ZoneTargetResolver.TryResolveCombatZone` — zone raycast + groin banding for Harm hits (`ZoneTargetCollider` is the contract; bones may be on Characters)
- `ZoneTargetResolver.TryResolveHoverZone` / `GetReticleLabel` / `IsMeleeZoneReachInRange` — Main HUD + combat (exclude-self overload; AnatomyNode limb meshes; closest-point melee reach). Hover/hitscan accept optional `maxRayDistance` (default 8 m; ranged passes weapon max range).
- `GetZoneBruteFraction(BodyZone)` — 0..1 zone brute for gait/limp presentation (replaces legacy `FootBodyPart.RelativeDamage`)
- Screen feedback: [screen-effects](screen-effects.md) via `HealthScreenEffectMapper` + hit-flash TargetRpc — do not reimplement Volume overlays in Health.
- Personal heartbeat + labored breathing: [audio](audio.md) via `HealthPersonalAudioMapper` (`PersonalAudioSubSystem`) — driven from the same `ApplyScreenEffectsFromSnapshot`/`ClearScreenEffectsIfDriving` local-owner hooks as screen effects, not a separate subscription. Deliberately treats `IsCardiacArrest` as heartbeat silence (opposite of the screen vignette). Breathing max-merges with stamina.
- Alert stack: emit via `SnapshotChanged`; map with `HealthAlertStackMapper` only — [inventory](inventory.md) Main HUD owns `AlertStackState` / icon rendering.
- Body presentation: map vitals with `BodyPresentationIntent.FromSnapshot` and call `Ragdoll.ServerSetPresentation` — never apply ragdoll/animator visuals from Health.

## Pitfalls

- **Critical patients can still act despite collapsing:** snapshot `CanUseArms` and `MovementSpeedMultiplier` must include systemic critical, consciousness, arrest and death checks, not only limb injury. `PublishSnapshot` drops held items on loss of arm control; recovery restores capabilities.
- **Blood decals vanish after setting Surface Type Transparent:** URP `Decal` shadergraph materials must stay **Opaque**. Alpha already blends via the DBuffer/Screen Space passes (`SrcAlpha OneMinusSrcAlpha`); flipping `_Surface` / render queue 3000 / `_SURFACE_TYPE_TRANSPARENT` makes projectors stop drawing. Keep `BloodDecal.mat` Opaque like the package `Decal.mat` default.
- **White / untinted splatters:** most `Assets/Art/Textures/World/VFX/Splatters/Splatter*.png` are white masks. URP Decal has no Base Color multiply (only `Base_Map` → albedo). `BleedingVfxCatalog.CreateDecalMaterial` tints those masks at runtime; do not “fix” white by switching the material to Transparent.
- **Floor blood paints through the player / looks cut off:** Mesh Bias only affects mesh decals. Cutoffs: align with `DecalRotationOntoSurface` (never bare `LookRotation(-normal)` on flats — use world-forward as the up hint when projecting vertically). `LookRotation`'s second arg is **upwards**, not a surface tangent; a `Cross` tangent rolled wall projectors and made vertical stamps miss. Through-player: URP Decal Layers need receivers to write rendering layers in a **DepthNormals** pass — Simple Toon lacked that, so character pixels kept the floor’s layer and still matched floor projectors. `STDefault` now has DepthNormals/`_WRITE_RENDERING_LAYERS`; tiles stamp `ReceiveWorldDecals`, floor projectors use `WorldFloorProjectorMask`. Floor blood never paints walls — `SpawnAtAnchor` only raycasts down. **Do not drop DepthNormals when turning SSAO off** — DBuffer + Decal Layers still need that prepass ([rendering](rendering.md)).
- **Bleed particles float beside the limb:** do not parent VFX to `AnatomyNode` roots first — those prefab pivots do not follow the skinned mesh. Prefer `ZoneTargetCollider` bone transforms (see `WoundVfx.EnsureAnchors`).
- **Bleed particle look:** `BloodParticle.mat` uses soft `Assets/Art/Textures/World/VFX/Health/blood_droplet.png` (round falloff), not `splatter.png` (decal mask). Keep drip emission visible; weird “paper cutout” drips were the texture, not rate.
- **Drips visible spawning on the mesh:** emission is at the bone anchor. Fade alpha in over the first ~5% of lifetime (`DripSpawnInvisibleLifetimeFraction` in `WoundVfx`) so droplets clear the surface before drawing; do not only raise start speed (that still shows a spawn pop).
- **Body blood missing on worn jumpsuit:** worn cloth is `ClothesDisplayer` → `ClothedBodyPart` on Default. Simple Toon must sample DBuffer (`#ifdef _DBUFFER` + `ApplyDecalToBaseColor`). Wound projectors must aim into the mesh (`BloodDecalSpawner.RotationOntoSurface` + outward offset) — identity local rotation on the bone misses skin/clothing so only floor stamps from `BloodDecalSpawner.SpawnAtAnchor` were visible.
- **Death re-triggers every health tick:** `TickHealth` must latch death (`_deathTriggered`) and stop ticking; otherwise `Human.Kill()` re-runs every second (ghost spam / dispose races). `WoundVfx` also clears and disables on `HealthState.Dead`.
- **Pain scream on corpses:** `TryPlayPainScream` must gate on dead / unconscious / non-`Locomotion` presentation — `ApplyDamage` still runs on bodies (flesh hit SFX OK); do not scream after death.
- **Ghost spawn stack-overflows the editor:** `HumanoidGhostController.OnAwake` must call `base.OnAwake()`, never `base.Awake()` — the latter re-enters `NetworkActor.Awake` → `OnAwake` forever when `Human.Kill()` instantiates the ghost.
- **Death / collapse presentation:** `Ragdoll` owns replicated `BodyPresentationState`. Health must not call collapse visuals or reinforce RPCs. Latch health-owned collapses (`_healthCollapseActive`) so waking does not clear combat timed knockdown. Critical and cardiac arrest collapse even while `IsConscious` is still true. Do not rely on SyncVar OnChange alone — see [body-presentation-authority](../../archive/architecture/2026-07_body-presentation-authority.md).
- **Screen-effect/personal-audio Clear from other bodies:** only clear when `_drivingLocalPresentation` — other players' mind unassign must not wipe the local owner's Volume intensities or heartbeat cue.
- **Ghost still gets hit flash after death:** `SwapMinds` used to leave FishNet ownership on the corpse (`SetMind(null)` early-returned). `ApplyDamage` then `RpcHitFlash(Owner)` to the ghost. Clear ownership on null/`Mind.Empty`; skip hit flash when `_deathTriggered` / `HealthState.Dead`; `RpcHitFlash` also requires `IsLocalOwnerMind()`.
- **Host alert/screen gap:** raise HUD consumers from `PublishSnapshot` as well as SyncVar OnChange — FishNet may skip OnChange on server assigns (same reason screen effects apply in `PublishSnapshot`).
- **Internals replace the air, not the room:** `HealthEnvironmentExposure.BreatheFromTank` swaps breathability and gas fractions for the tank's mix, and leaves pressure and temperature for the seal. An empty tank therefore suffocates the wearer in good air, and `ApplyBreathExchange` skips the room, so no CO₂ wakes atmos cells.
- **Magboots are a switch, not a state:** `MagbootsOn` stays set while the wearer is down or floating, and the boots hold again once upright over a floor. Read `MagbootsAnchored` for the effect, and `ServerAnchored` on the server, which also catches a lost foot.
- **No tile/atmos yet ≠ vacuum:** `SampleEnvironmentAtBody` returns `HealthEnvironmentState.SafeDefault` (breathable) when Tile/Atmos aren't ready — do not treat missing samples as vacuum or lobby spawns suffocate. `atmosdamage off` forces SafeDefault every tick. `HumanoidSupportState.Unknown` (map missing / not ready) also → SafeDefault.
- **Off-map / past atmos chunks is vacuum:** once Tile+Atmos are ready, `HumanoidSupportState.Unsupported` (server: no plenum, or occupancy-miss **after** `TileMapLoaded`) keeps exposure as vacuum even when `TryGetCellDebugInfo` fails outside `_coordToIndex`. Do not fall back to SafeDefault there or deep-space float becomes breathable. Client `!HasPlenum` / occupancy-miss is Unknown (AOI), not Unsupported — see [entities](entities.md) and [TECH_DEBT.md](../TECH_DEBT.md) §1.17.
- **LowOxygen flicker in station air:** turf hypoxia uses O₂ **partial pressure** (mole% × kPa), not raw mole fraction or `OxyDebt > 0`. Comfortable ≥18 kPa PO₂ (station ~20); alert soft-start for systemic debt is `LowOxygenAlertSoftStart`.
- **Pressure ≠ burn:** low/high/vacuum pressure damages **lungs** (`PressureLungDamage` → `OrganSimulation.ApplyLungDamage`). Hot/cold/fire burn **all zones** at per-zone rates. Do not dump pressure into chest burn or cascade ambient dermal burn into organ damage.
- **`AudioType` ambiguous in Health:** `HumanHealthController` imports `UnityEngine` and `SS3D.Systems.Audio` — use `using AudioType = SS3D.Systems.Audio.AudioType;` (same as `InteractionController`) before `PlayAudioSource`.
- **Melee self-hit / missed limbs:** connect and reticle must pass `excludeHealth` (attacker) into `TryResolveHoverZone`; include detachable `AnatomyNode` mesh colliders and check reach with `IsMeleeZoneReachInRange` (closest point), not the ray impact alone — see [combat](combat.md).

## Tests

- `Testing/ui/validate.sh --steps compile,editmode`: the health simulation, treatment and environment exposure EditMode tests.
- `Testing/ui/validate.sh --steps compile,scenarios:Medical` (`MedicalScenarios`): bandaging a bleeding wound on yourself.
- `Testing/ui/validate.sh --steps compile,scenarios:Environment` (`EnvironmentScenarios`): plasma in the room drives a body critical through the real atmos → health tick (~17 s: toxin intake is capped at 0.08/tick against 0.03/tick liver clearance, so more gas doesn't speed it up).

## Depends on / Used by

- **Depends on:** [entities](entities.md), [interactions-framework](interactions-framework.md), [screen-effects](screen-effects.md), [audio](audio.md) (`PersonalAudioSubSystem`, hit/gasp pool), [atmospherics](atmospherics.md) (turf sample)
- **Used by:** [combat](combat.md) (melee zone hits), [inventory](inventory.md) (Main HUD alert stack), dev console `hurt`/`heal`, `HumanoidLivingController` / `HumanoidPredictedMovement` / `HumanoidBodyStateBridge` (movement multiplier / limp + `InjuredLeg` / injured-arm presentation), `Hand` (arm debuff stub)
- **Stamina:** [stamina](stamina.md) Phase 7a core — regen/encumbrance/overdraw→oxy; breathing max-merge with health

## Related docs

- Known design deltas: [REVIEW_2026-10 — Known delta](../../design/REVIEW_2026-10.md#known-delta-revised-design-vs-code) — rows: health (decapitation, kidneys)
- Design (read-only): [Documents/design/health.md](../../design/health.md), [main-hud.md](../../design/main-hud.md) §9, [stamina.md](../../design/stamina.md), [armor.md](../../design/armor.md)
- Anatomy map: [health-anatomy-map.md](health-anatomy-map.md)
- Plan: [health_implementation_plan.md](../../plans/health_implementation_plan.md)
- Stamina map: [stamina](stamina.md)
- [2026-07_body-presentation-authority](../../archive/architecture/2026-07_body-presentation-authority.md) — **shipped** single authority for collapse/death presentation
- [2026-07_health-env-feel](../../archive/architecture/2026-07_health-env-feel.md) — **shipped** turf exposure + feel polish (M7/M8 exposure)
- [2026-07_animation-polish](../../archive/architecture/2026-07_animation-polish.md) — limp/injured gait, severity idle, arm overlay, left-hand mirror
- [2026-07_agent-first-composition](../2026-07_agent-first-composition.md)
- [screen-effects](screen-effects.md)
- Milestone: [mvp1-nuke-ops.md](../../milestones/mvp1-nuke-ops.md) M7 / M8
