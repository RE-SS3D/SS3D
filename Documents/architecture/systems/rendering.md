> Code paths: Assets/Scripts/SS3D/Rendering/, Assets/Content/Resources/Simple Toon/, Assets/Scripts/SS3D/Systems/Vision/, Assets/Content/Resources/Vision/
> Entry points: SelectionPickRendererFeature, AtmosRendererFeature, VisionRendererFeature
> Status: partial
> Verified: 5834bcdac — 2026-11-04 (atmos render suppression in the map editor)

# Rendering

## Overview

URP rendering extensions for this fork. The selection pick pass ([selection](selection.md)) is gameplay-critical for interaction targeting. The atmospherics pass ([atmospherics](atmospherics.md)) composites gas scatter, plasma glow, and heat distortion from sim GPU textures via `AtmosRenderContext`. **Decal Renderer** is enabled on the forward renderer (Use Rendering Layers on) for health blood decals and future surface marks — see `DecalRenderingLayers`. **Atmos snapshots are server-built** — hosts render directly; pure clients consume Phase 1 dirty-chunk patches through `AtmosClientVisualizationBridge`. Late-join bootstrap / AOI remain Phase 2; see [atmospherics](atmospherics.md).

Station materials use the **Simple Toon** shader stack (`STDefault` / `STTransparent`). ST already has `UnityPerMaterial` + `#pragma multi_compile_instancing` — Metastation wins come from enabling material instancing and keeping MeshRenderers free of permanent MaterialPropertyBlocks (see [srp-batcher-gpu-instancing](../2026-07_srp-batcher-gpu-instancing.md)). Palette emission must sample `_EmissionMap` (same UV swatch pattern as albedo) — a flat `_EmissionColor` alone washes shared `PaletteEmission` materials white. `STDefault` includes DepthOnly + DepthNormals (with `_WRITE_RENDERING_LAYERS`) so URP Decal Layers can distinguish characters from tiles, and ForwardLit samples DBuffer (`ApplyDecalToBaseColor`) so health blood / surface `DecalProjector`s tint skin and worn clothing. **SSAO is enabled** on `SS3D_ForwardPlusRenderer` for contact grounding (downsampled, intensity 1, radius 0.32, direct strength 0.8). Opaque ST variants sample URP AO and pass it into additional-light evaluation; emission stays unaffected. This deliberately revisits [design §8](../../design/rendering-lighting.md#8-post-processing-stack), which deprioritizes AO. DepthNormals also serves DBuffer + Decal Layers. Skin, hair, cut surfaces and eight floor materials use half-toon; PaletteEmission retains its separate emissive behavior.

Client FOV / fog-of-war is a hard black mask driven by batched physics raycasts from `Entity.ViewPoint` (`VisionSubSystem` → `_VisionMap`) and composited by `VisionRendererFeature`. Unseen areas are fully opaque black, not soft fog. Rays advance in `RaycastCommand` waves, skipping furniture/props until the nearest wall/door (non-window); a capped multi-hit buffer previously filled with props and leaked vision through walls. Triggers and inventory preview cameras are ignored.

## Start here

- `Assets/Scripts/SS3D/Rendering/URP/SelectionPickRendererFeature.cs` — URP feature for shader-ID picking
- `Assets/Scripts/SS3D/Rendering/URP/SelectionPickContext.cs` — pick pass render context
- `Assets/Scripts/SS3D/Rendering/URP/SelectionRenderingLayers.cs` — layer bit to exclude outline shells from the pick pass
- `Assets/Scripts/SS3D/Rendering/URP/DecalRenderingLayers.cs` — floor vs character DecalProjector masks (`ReceiveWorldDecals`)
- `Assets/Scripts/SS3D/Rendering/URP/AtmosRendererFeature.cs` — gas scatter, glow, distortion passes
- `Assets/Scripts/SS3D/Rendering/URP/AtmosRenderContext.cs` — shared GPU snapshot for atmos shaders
- `Assets/Scripts/SS3D/Rendering/URP/VisionRendererFeature.cs` — FOV mask + hard black composite
- `Assets/Scripts/SS3D/Rendering/URP/UiBackdropBlurRendererFeature.cs` — Dual Kawase world blur behind diegetic machine UI
- `Assets/Scripts/SS3D/Systems/Vision/VisionSubSystem.cs` — client `RaycastCommand` waves → R16 `_VisionMap` (occluder cache; float dilate + `SetPixelData`)
- `Assets/Content/Resources/Simple Toon/Shaders/STLighting.hlsl` — half-toon lighting + palette emission sample
- `Assets/Content/Resources/Simple Toon/Shaders/STDefault.shader` — opaque toon (+ DepthNormals for Decal Layers)
- `Assets/Content/Resources/Simple Toon/Shaders/ObjectIcon.shader` — unlit bright full-toon for UI icon previews
- `Assets/Scripts/SS3D/Utils/IconPreviewGenerator.cs` — ObjectIcon material swap for UI icon previews
- `Assets/Editor/URPMigration/SelectionPickRendererFeatureSetup.cs` — idempotent selection-feature registration and recovery-map synchronization
- `Assets/Settings/URP/` — pipeline asset and Forward+ renderer (includes Decal Renderer feature)
- **SS3D/Rendering/Run Content Prefab Recipes** — `RendererProbeUsageSetup` (light/reflection probes Off on Content prefabs)

## Extension points

- New render features: add URP `ScriptableRendererFeature` under `Rendering/URP/`.
- Outline / auxiliary meshes that must not participate in pick: set rendering layer `SelectionRenderingLayers.ExcludeFromSelectionPick`.
- World surface marks: stamp `DecalRenderingLayers.ReceiveWorldDecals` on receiver renderers; point floor `DecalProjector`s at `WorldFloorProjectorMask`. Custom opaque shaders must implement DepthNormals with `_WRITE_RENDERING_LAYERS` or Decal Layers will not exclude them, and must sample DBuffer (`ApplyDecalToBaseColor` / `_DBUFFER_MRT*`) or DBuffer projectors never tint albedo (worn jumpsuit / ST floors).

## Pitfalls

- **Carpet/vendor surfaces break into streaks:** constant palette-swatch UVs produce zero derivatives in `ST_CotangentFrame`; unguarded `rsqrt(0)` creates NaN normals. Clamp the squared tangent length before reciprocal square root so flat normal maps retain geometric normals.

- **Half-toon fixtures lose color and intensity:** legacy `_AmbientCol: 1` blends light radiance away in opaque materials. Half-toon must multiply albedo by `light.color`; keep the legacy color blend only for full toon. Specular uses the already-lit texture color once, avoiding squared light intensity.
- **Near-light shadows wash out:** apply the soft-near distance limiter before multiplying shadow visibility; limiting their product brightens partial shadows. Fixture spots cast soft shadows at the medium resolution tier; PointFill stays unshadowed. Low quality disables additional-light shadows at the pipeline level.
- **Ghost FOV:** `VisionSubSystem` clears its target and disables the composite on a `HumanoidGhostController` body. Map-editor suppression must not reactivate vision for a ghost; respawning into a living body restores targeting.
- **Atmos visuals outlive a paused sim:** `AtmosSubSystem.SimulationPaused` stops the tick, not the render path — the retained GPU snapshot keeps scatter/glow/distortion and `AtmosWindView` animating (and a remote client's map editor cannot pause the server). `AtmosRenderContext.SetRenderingSuppressed` is checked by both `AtmosRendererFeature.AddRenderPasses` and `AtmosWindView.Update`; `MapEditorSubSystem` owns the toggle and must clear the statically-held flag on destroy too. Hit 2026-11-04 (#147).
- **Decal technique is pinned to DBuffer, not Automatic:** `SS3D_ForwardPlusRenderer` Decal feature `technique: 1`. Automatic resolves through URP's cached `SHADER_API_MOBILE` check, which differs between the headless CI editor and a desktop player: v0.3.3's CI build kept only `DecalScreenSpaceProjector`, the desktop player chose DBuffer, found no `DBufferProjector` pass, and URP's `DecalDrawErrorSystem` drew every bullet hole and blood decal with the magenta error material (#111). Local builds kept DBuffer, so they never showed it. `Shader Graphs/Decal` and `Hidden/SS3D/MultiplyTint` also stay in Always Included Shaders. To check a player build, run the `assert_bullet_decal` automation opcode (fails if the DBuffer pass is missing) or `stamp_bullet_holes` + `screenshot`. Hit 2026-11-02.
- **Borderless below native resolution offsets the mouse:** `Screen.SetResolution` in `FullScreenWindow` at a smaller size stretches the swapchain to the desktop, but Linux/X11 still reports pointer coordinates in desktop pixels, so map editor and diegetic UI clicks land off-target. `PlayerDisplayApplier.ApplyWindow` always uses native size in Borderless; the saved resolution only applies to Fullscreen/Windowed. Hit 2026-10-31.
- **One URP asset per quality tier.** `QualitySettings` has four levels (Low/Medium/High/Ultra, default High), each pointing at its own `Assets/Settings/URP/SS3D_URPAsset_<Tier>.asset`; `GraphicsSettings` keeps Ultra as the fallback. A pipeline-wide change (renderer features, GPU Resident Drawer, HDR) must go into all four. Render scale and upscaler are written at runtime by `PlayerSettingsSubSystem` onto whichever asset is active ([2026-10_player-settings](../2026-10_player-settings.md)); in the editor it restores them when play stops, so don't treat play-mode values in the inspector as saved.
- **Player camera AA is overridden at runtime.** `PlayerCamera.prefab` serializes TAA, but the player's Anti-Aliasing setting is applied to every game camera without a target texture on `beginCameraRendering`. Render-texture cameras (preview booth, icon generator) keep their own AA.

- **Selection feature changed IDs on every script reload:** `SelectionPickRendererFeatureSetup.RemoveBrokenFeatures` destroyed healthy selection features as well as broken stubs. Keep valid instances, synchronize `m_RendererFeatureMap` to the actual sub-asset IDs and save only when wiring changes. `SelectionRendererSetupTests` asserts repeated setup preserves object identity and the recovery map.

- **Permanent MaterialPropertyBlocks break SRP Batcher:** any MPB on a MeshRenderer (e.g. old Selectable `_SelectionColor`, Intact white integrity tint) drops that draw out of the SRP Batcher path. Selection pick uses transient MPB on `DrawMesh` via `SelectionPickContext`; intact integrity clears MPBs. Hit 2026-07-28 (Metastation).
- **Light/reflection probes block GPU instancing:** BlendProbes (and reflection probes) inject per-renderer non-instanced data — Frame Debugger shows "Non-instanced properties set for instanced shader." SS3D does not use probe lighting; keep `LightProbeUsage` / `ReflectionProbeUsage` **Off** on Content renderers via **SS3D/Rendering/Run Content Prefab Recipes** (`RendererProbeUsageSetup`). Hit 2026-07-29.
- **Selection `DrawMesh` needs explicit frustum cull:** HashGrid AOI ≠ camera frustum. Pick collect must `TestPlanesAABB` against the request camera (`IsInPickFrustum`) or Metastation pays full-AOI pick draws. See [selection](selection.md). Hit 2026-07-28.
- **GPU Instancing needs the material flag:** ST shaders compile instancing variants, but assets need `enableInstancing` / `m_EnableInstancingVariants: 1`. Floor ST mats were flipped in [srp-batcher-gpu-instancing](../2026-07_srp-batcher-gpu-instancing.md).
- **GPU Resident Drawer on Linux/OpenGL:** `m_GPUResidentDrawerMode` must stay **Disabled** (`0`) on every quality-tier asset (`SS3D_URPAsset_Low/Medium/High/Ultra`). Instanced Drawing requires `BatchBufferTarget.RawBuffer`; unsupported APIs spam the warning every rebuild. Do not re-enable in `URPFoundationSetup` without checking the active graphics API. Do not confuse with classic GPU Instancing / SRP Batcher (still on).
- **ST meshes ignore blood/floor DecalProjectors:** the Decal technique is DBuffer. `STDefault` ForwardLit must keep `#pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3` and `ApplyDecalToBaseColor` under `#ifdef _DBUFFER` — calling it without the keyword samples an unbound buffer (weight 0 → black mesh). DepthNormals alone only fixes Decal Layers filtering, not albedo tint.
- **Decal Layers must stay enabled:** `SS3D_ForwardPlusRenderer` Decal feature `decalLayers: 1`. Floor/bullet projectors target `ReceiveWorldDecals` only; characters stay Default. Turning layers off makes Lit tiles look fine while masking breaks for the intended filter path — do not disable during lighting retunes.
- **Do not drop DepthNormals when disabling SSAO:** DBuffer decals + Decal Layers require the DepthNormals prepass (`_WRITE_RENDERING_LAYERS`). SSAO is another consumer; even with it off the ~opaque-sized DepthNormals pass remains. Removing it re-breaks floor blood painting through the player ([health](health.md)). Hit 2026-07-29 (Frame Debugger).
- **SSAO can appear ineffective on ST:** enabling the feature alone is insufficient. Opaque forward passes need `_SCREEN_SPACE_OCCLUSION` variants and `GetScreenSpaceAmbientOcclusion`; passing all-one AO factors bypasses it. With no ambient illumination, use direct-light strength for contact grounding.
- **Skin/floors remain flat despite fixture tuning:** `_HalfToon: 0` retains legacy radiance blending and max-light stacking. Keep skin/hair and floor materials on half-toon with zero minimum light; inspect actual material values rather than assuming the shader name implies the shading mode.
- **GPU instancing still limited after probes Off:** Frame Debugger on Metastation — after Content probe clear + GenericShadeless instancing, remaining breaks are mostly different adjacency/door meshes and intentional emissive MPBs; do not expect floor-like instance counts on airlocks. Hit 2026-07-29.
- **Item/tile icons dark or black under half-toon / fixture-only lighting:** do not render icons with live ST half-toon mats or bare scene lights. Use `IconPreviewGenerator` (`Unlit/ObjectIcon` overrides). `RuntimePreviewGenerator` still spawns temporary preview lights as a fallback path — do not rely on scene lighting. Keep `ObjectIcon` in Always Included Shaders (same pattern as InteractionOutline).
- **Shiny player head under PointFill:** close URP point lights create a bright N·L hotspot on bald/curved meshes (bloom amplifies it). Soft-near atten in `STLighting.hlsl` + keep character `_SpecIntensity: 0`; raise/dim fill rather than copying Built-in intensities.
- **Vision FOV must not use fixed multi-hit RaycastAll buffers:** a dense prop pile can exhaust the hit slots and report a clear line through walls. Keep iterative/wave single-hit casts that skip non-occluders (`VisionSubSystem` `RaycastCommand` waves + collider occluder cache).
- **Vision map upload:** do not `new Color[]` / `SetPixels` on the LateUpdate path — dilate with persistent float scratch and upload R16 via `SetPixelData` (`Vision.VisionMap` was a multi-MB/frame GC hotspot).

## Depends on / Used by

- **Used by:** [selection](selection.md), [atmospherics](atmospherics.md), [screen-effects](screen-effects.md) / [machine-interface](machine-interface.md) (UI backdrop blur)
- **Vision FOV depends on:** `PlacedTileObject` Wall/Door (or `Walls` layer) colliders; cast origin from [entities](entities.md) `Entity.ViewPoint` when present

## Related docs

- [FORK_STATUS.md](../../FORK_STATUS.md) § URP migration
- Plan: [urp_lighting_look_plan_d42c32f5.plan.md](../../plans/urp_lighting_look_plan_d42c32f5.plan.md)
- Polish handoff: [2026-07_urp-lighting-look-polish.md](../2026-07_urp-lighting-look-polish.md)
- Effort (shipped): [2026-07_srp-batcher-gpu-instancing.md](../2026-07_srp-batcher-gpu-instancing.md)
- Effort (shipped): [2026-07_unity-framedebug-ai-tooling.md](../2026-07_unity-framedebug-ai-tooling.md) — Frame Debugger → `Logs/framedebug/` markdown
- Content recipe: **SS3D/Rendering/Run Content Prefab Recipes** (`RendererProbeUsageSetup` — probes Off for GPU instancing)
- Effort (planned): [2026-07_atmos-client-visualization-sync.md](../2026-07_atmos-client-visualization-sync.md)
