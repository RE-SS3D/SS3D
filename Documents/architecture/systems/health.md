> Code paths: Assets/Scripts/SS3D/Systems/Health/
> Entry points: HumanHealthController, HealthSimulation, OrganSimulation
> Status: partial (Phase 5b severing shipped; screen-effects wired; vitals HUD Phase 6 remainder)
> Verified: ab8eff923 — 2026-07-17

# Health

## Overview

Greenfield rewrite per [health_implementation_plan.md](../../plans/health_implementation_plan.md). Phase 1 shipped bleeding, bandage, VFX, and alert chip. Phase 2 wires asset-backed organs into pool math, cardiac arrest, and movement debuffs (`Snapshot.MovementSpeedMultiplier` — consumed by humanoid gait/limp presentation after the develop integration). Phase 3 adds multi-threshold critical state, cardiac arrest → defib window, and chest defibrillation. Phase 4 adds BodyParts raycast zone resolution for melee combat. Phase 5 adds field treatments (burn dressing, splint, O2, CPR, transfusion, antitoxin). Phase 5b adds limb severing (zone `IsSevered`, anatomy hide, world drops, head mind-swap). Bleeding visuals now use tuned particle streams plus URP Decal blood marks (body + floor). Bleed drain uses `BleedingBloodDrainScale = 0.010` with oxy gain / arrest brain drain synced so hypoxia tracks bleed (see plan hemorrhage tuning).

Local-owner [screen-effects](screen-effects.md) are driven from `HealthSnapshot` via `HealthScreenEffectMapper` (dying/critical, blood-loss tunnel vision, oxy debt, concussion, unconscious) plus hit flash on `ApplyDamage`. The atmosphere→oxygen coupling is still open: `HealthSimulation.LungIntake(atmosphereO2)` currently defaults to full O2 and should be fed the occupant's turf O2 ratio. Vitals cluster UITK and examine-self readout remain Phase 6.

**Stamina Phase 7a core shipped:** see [stamina](stamina.md) — push-past-empty calls `ApplyOxyDebt`; obsolete `StaminaBar` purged from PlayerCanvas. Combat stamina costs still deferred.

Phase 0d strips legacy health components from `Human.prefab` and rewires a thinner root — do not dual-stack or grow the mega-prefab ([agent-first composition](../2026-07_agent-first-composition.md), [health_implementation_plan.md](../../plans/health_implementation_plan.md) Phase 0d).

**Body presentation debt:** Health owns vitals intent (conscious / cardiac arrest / dead); it must not grow a third collapse path. Ragdoll, animator, and movement still share presentation via interim RPCs — refactor per [2026-07_body-presentation-authority.md](../2026-07_body-presentation-authority.md).

## Start here

- `Assets/Scripts/SS3D/Systems/Health/HumanHealthController.cs` — server tick, damage/treatment, organ registration, snapshot SyncVar
- `Assets/Scripts/SS3D/Systems/Health/HealthScreenEffectMapper.cs` — local-owner snapshot → `ScreenEffectsSubSystem` intensities
- `Assets/Scripts/SS3D/Systems/Health/HumanAnatomyController.cs` — limb severance visuals, world drops, head mind-swap (Phase 5b)
- `Assets/Scripts/SS3D/Systems/Health/HealthSimulation.cs` — pool math, severity/bleeding, critical/death evaluation
- `Assets/Scripts/SS3D/Systems/Health/OrganSimulation.cs` — zone→organ damage, organ tick drains, perfusion, limb multipliers
- `Assets/Scripts/SS3D/Systems/Health/OrganInstance.cs` — organ registration on Human prefab / organ item prefabs
- `Assets/Scripts/SS3D/Systems/Health/Interactions/BandageInteraction.cs` — stop bleeding (Phase 1)
- `Assets/Scripts/SS3D/Systems/Health/WoundVfx.cs` — per-zone bleed particles / body decals; anchors prefer armature `ZoneTargetCollider` bones; particle/decal intensity and floor drip cadence scale with snapshot bleed rates
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
- `Assets/Scripts/SS3D/Systems/Health/HealthDebugController.cs` — IMGUI overlay (H) for full zone/organ/pool inspection
- `Assets/Scripts/SS3D/Systems/Health/HealthDebugDetail.cs` — per-zone/per-organ SyncVar payload for debug UI
- `Assets/Scripts/SS3D/Systems/Health/ZoneTargetResolver.cs` — point + combat raycast zone resolution, groin banding
- `Assets/Scripts/SS3D/Systems/Health/HealthLayers.cs` — `BodyParts` layer mask for combat raycasts

## Extension points

- `IHealthEffectModifier` — virology/chemistry/stamina deltas (Phase 7+)
- `ApplyDamage(BodyZone, MeleeDamagePacket)` — melee combat input (Phase 4)
- `ApplyDamage(BodyZone, brute, burn)` — direct damage (console `hurt`, future sources)
- `ApplyTreatment(...)` — zone treatments including splint flag (Phase 5)
- `ApplyBloodTransfusion` / `ApplyOxyRelief` / `ApplyAntitoxin` / `ApplyCpr` — systemic field treatments (Phase 5)
- `ZoneTargetResolver.TryResolveCombatZone` — BodyParts raycast + groin banding for Harm hits
- `GetZoneBruteFraction(BodyZone)` — 0..1 zone brute for gait/limp presentation (replaces legacy `FootBodyPart.RelativeDamage`)
- Screen feedback: [screen-effects](screen-effects.md) via `HealthScreenEffectMapper` + hit-flash TargetRpc — do not reimplement Volume overlays in Health.

## Pitfalls

- **Blood decals vanish after setting Surface Type Transparent:** URP `Decal` shadergraph materials must stay **Opaque**. Alpha already blends via the DBuffer/Screen Space passes (`SrcAlpha OneMinusSrcAlpha`); flipping `_Surface` / render queue 3000 / `_SURFACE_TYPE_TRANSPARENT` makes projectors stop drawing. Keep `BloodDecal.mat` Opaque like the package `Decal.mat` default.
- **White / untinted splatters:** most `Assets/Art/Textures/World/VFX/Splatters/Splatter*.png` are white masks. URP Decal has no Base Color multiply (only `Base_Map` → albedo). `BleedingVfxCatalog.CreateDecalMaterial` tints those masks at runtime; do not “fix” white by switching the material to Transparent.
- **Floor blood paints through the player / looks cut off:** Mesh Bias only affects mesh decals. Cutoffs: align with `DecalRotationOntoSurface` (never bare `LookRotation(-normal)` on flats). Through-player: URP Decal Layers need receivers to write rendering layers in a **DepthNormals** pass — Simple Toon lacked that, so character pixels kept the floor’s layer and still matched floor projectors. `STDefault` now has DepthNormals/`_WRITE_RENDERING_LAYERS`; tiles stamp `ReceiveWorldDecals`, floor projectors use `WorldFloorProjectorMask`.
- **Bleed particles float beside the limb:** do not parent VFX to `AnatomyNode` roots first — those prefab pivots do not follow the skinned mesh. Prefer `ZoneTargetCollider` bone transforms (see `WoundVfx.EnsureAnchors`).
- **Death re-triggers every health tick:** `TickHealth` must latch death (`_deathTriggered`) and stop ticking; otherwise `Human.Kill()` re-runs every second (ghost spam / dispose races). `WoundVfx` also clears and disables on `HealthState.Dead`.
- **Ghost spawn stack-overflows the editor:** `HumanoidGhostController.OnAwake` must call `base.OnAwake()`, never `base.Awake()` — the latter re-enters `NetworkActor.Awake` → `OnAwake` forever when `Human.Kill()` instantiates the ghost.
- **Death skips ragdoll / keeps walk cycle:** `OnDisable` must not `Recover()` (ownership teardown stands the corpse up). Death uses `ServerDeathRagdoll` + observer reinforce: disable Animator/`AnimationOrchestrator`, enable bone physics. Do not rely on SyncVar OnChange alone from server `Kill()`.
- **Unconscious presentation:** collapse on `!IsConscious` **or** `IsCardiacArrest`. Use `ApplyCollapseVisuals` + `RpcSetConsciousnessCollapsed` (same reinforce pattern as death). Coimbra `UpdateEvent` keeps firing after `enabled=false` — `AnimationOrchestrator.SetPosingSuppressed` must stop walk-param writes. Broader ownership: [body-presentation-authority](../2026-07_body-presentation-authority.md).
- **Screen-effect Clear from other bodies:** only clear when `_drivingLocalScreenEffects` — other players' mind unassign must not wipe the local owner's Volume intensities.

## Depends on / Used by

- **Depends on:** [entities](entities.md), [interactions-framework](interactions-framework.md), [screen-effects](screen-effects.md)
- **Used by:** [combat](combat.md) (melee zone hits), dev console `hurt`/`heal`, `HumanoidLivingController` / `HumanoidPredictedMovement` / `HumanoidBodyStateBridge` (movement/consciousness/limp), `Hand` (arm debuff stub)
- **Stamina:** [stamina](stamina.md) Phase 7a core — regen/encumbrance/overdraw→oxy; combat drains deferred

## Related docs

- Design (read-only): [Documents/design/health.md](../../design/health.md), [main-hud.md](../../design/main-hud.md) §9, [stamina.md](../../design/stamina.md), [armor.md](../../design/armor.md)
- Anatomy map: [health-anatomy-map.md](health-anatomy-map.md)
- Plan: [health_implementation_plan.md](../../plans/health_implementation_plan.md)
- Stamina map: [stamina](stamina.md)
- [2026-07_body-presentation-authority](../2026-07_body-presentation-authority.md) — **planned** single authority for collapse/death presentation
- [2026-07_agent-first-composition](../2026-07_agent-first-composition.md)
- [screen-effects](screen-effects.md)
