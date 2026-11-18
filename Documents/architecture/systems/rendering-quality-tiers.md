> Parent map: [rendering.md](rendering.md)
> Code paths: Assets/Scripts/SS3D/Rendering/URP/CortaoAmbientOcclusionFeature.cs, Assets/Settings/URP/, Assets/Content/Resources/CORTAO/
> Status: shipped
> Verified: eb8655dfa — 2026-11-18

# Rendering quality tiers

## Overview

The station look is lit only by wall fixtures (one shadowed spot each, 6.12 m range), so CORTAO ambient
occlusion and the fixture spot shadows are the two costs each quality tier trades. Both are set per tier:
AO in `CortaoAmbientOcclusionFeature.QualityLevels` (one renderer is shared by all tiers, so the feature picks its
entry by `QualitySettings` level), shadows in each tier's URP asset. Which lights reach a pixel is not traded.
The renderer is Forward+, so on every tier each pixel is lit by every fixture in range
([Lighting path](#lighting-path-forward)).

| Tier | AO | Fixture shadows (atlas, per light, filter) | AO cost at 1440p |
|---|---|---|---|
| Low | off: binds white | off (pipeline) | 0 |
| Medium | half resolution, 12 samples, 6 px blur | 2048, 256 px, soft Low | 0.45 ms |
| High | full resolution, 16 samples, 6 px blur | 4096, 512 px, soft High | 1.36 ms |
| Ultra | as High | 4096, 512 px, soft High | 1.36 ms |

Fixture spots ask for the **Medium** resolution tier and the **pipeline's** soft-shadow quality, so each URP
asset decides both: `AdditionalLightsShadowResolutionTierMedium` and `SoftShadowQuality`. Each atlas holds 64
slices at that tier's size, so the 23–26 shadowed fixtures in view on classic-ministation keep their resolution.
Before 2026-11-18, High's 2048 atlas held only 16 slices at 512 px, so URP halved every fixture shadow to 256 px
whenever more than 16 were in view (logged as "Reduced additional punctual light shadows resolution by 2").

### Measured cost

RTX 3070, D3D11, Editor host on classic-ministation, camera distance 15, vision mask off, SMAA, 29 shadowed
fixture spots. Values are median GPU ms per `ProfilingSampler` marker over ~500 frames (`Recorder.gpuElapsedNanoseconds`).
The 1080p runs had the GPU at about 1.2 GHz; 1440p and 2160p had it at 1.9 GHz (see Pitfalls).
All numbers on this page predate the 2026-11-18 fixture retune (spot range 10–12 m → 6.12 m, PointFills removed)
and were not re-measured. Fewer lights now reach each cluster, so expect the Forward+ light loop to cost less
than shown.

CORTAO at full resolution:

| Pass | 1080p | 1440p | 2160p |
|---|---|---|---|
| Prepass | 0.09 | 0.11 | 0.25 |
| Main pass, 16 / 12 / 8 samples | 0.50 / 0.41 / 0.31 | 0.60 / 0.48 / 0.36 | 1.43 / – / 0.85 |
| CORTAO filter (off in SS3D) | 0.09 | 0.10 | – |
| Bilateral blur, two passes, radius 6 / 3 | 0.55 / 0.34 | 0.64 / 0.38 | 1.47 / 0.90 |
| **Total as shipped on High** | **1.15** | **1.36** | **3.14** |

CORTAO at half resolution (blur radius 3 CORTAO pixels = 6 screen pixels):

| Pass | 1080p | 1440p | 2160p |
|---|---|---|---|
| Prepass | 0.04 | 0.04 | 0.08 |
| Main pass, 16 / 12 / 8 samples | 0.15 / 0.12 / 0.10 | 0.17 / 0.14 / 0.11 | 0.34 / 0.27 / – |
| Bilateral blur, two passes | 0.10 | 0.12 | 0.22 |
| Upsample | 0.12 | 0.15 | 0.30 |
| **Total, 12 samples (Medium)** | **0.38** | **0.45** | **0.87** |

At 2160p the frame is GPU-bound and the sums show up whole: the camera took 13.5 ms with full-resolution AO,
11.3 ms at half resolution and 10.7 ms with AO off. At 1080p and 1440p the Editor frame is CPU-bound and the camera
total did not move.

Fixture shadows at 1440p on High:

| Configuration | Shadow casters | Opaque pass | Camera |
|---|---|---|---|
| 2048 atlas, 512 px requested, halved to 256 (old High) | 0.88 | 1.71 | 7.0 |
| 4096 atlas, 512 px (new High) | 0.79–0.94 | 1.77–1.79 | 7.0–7.35 |
| 2048 atlas, soft filter Medium / Low instead of High | 0.97 | 1.46 / 1.26 | – |
| Additional-light shadows off | 0 | 0.67 | 5.3 |

Shadow resolution barely changes the cost (the caster pass is bound by draw calls); the 4096 atlas costs 24 MB
more VRAM (16-bit depth). The soft filter is the per-pixel cost: High → Low saves about 0.45 ms. At 512 px the
character's shadow on a wall reads as head and shoulders; at 256 px it is a blob. Full and half-resolution AO
differ by more than 8/255 in 0.08% of pixels; AO's whole contribution darkens the frame by 0.4/255 on average,
mostly wall bases, vents and panel seams.

### Lighting path: Forward+

`SS3D_ForwardPlusRenderer` is Forward+ (`m_RenderingMode: 2`), as `URPFoundationSetup` writes it. On
2026-11-18 the station flat-lighting rewrite switched it to Forward (0) by accident. While it ran
Forward, two faults showed up, and Forward+ was restored the same day. Forward shades each mesh with at most
`m_AdditionalLightsPerObjectLimit` lights, picked by
intensity and distance at the mesh, not by which side of the mesh they light. At the time every fixture was two
lights (shadowed spot, 10–12 m range, plus a 12 m PointFill), so on classic-ministation 13–27 lights reached each
1 m tile and 1046 of the 1189 renderers in view were reached by more than 8. The two faults:

- **Black walls on Low and Medium.** At limits of 2 and 4, a wall often kept only the fixture behind it. Ambient
  is 0, and that fixture adds nothing to the wall's front face: its spot points away, and wrapped diffuse is zero
  for light from straight behind. Across three camera headings, 4.6–7.4% of the frame on Low and 0.9–1.3% on
  Medium was black.
- **Seams on every tier.** Neighbouring wall meshes kept different sets of 8, so lighting jumped at mesh edges. A
  character's shadow was cut off at a wall seam, or was 23/255 on one mesh and 41/255 on the next. Panels in
  fixture-dense rooms also showed brightness steps.

Forward+ has no per-object limit: each pixel loops over every light whose range reaches its screen cluster, which
in the measured setup was most of the 52. `SteelWreckage.shader` needed `_CLUSTER_LIGHT_LOOP` and `LIGHT_LOOP_BEGIN` to keep its
fixture light. Decals (DBuffer + Decal Layers), CORTAO, the vision mask and the selection pick pass behave the
same as on Forward. The per-object limits are 8 on every asset, so a switch back to Forward would not bring back
the black walls.

The cost is the light loop. Most lights in a cluster miss any given pixel: outside the spot cone, past the range
window, or behind the surface. `Station_AdditionalLightRadiance` tests those cases before calling
`GetAdditionalLight`, which samples and filters the shadow map. GPU ms for the opaque / transparent passes, with
the Game view at 7680×4320 so the GPU is the bottleneck (1.71–1.91 GHz):

| Tier | Forward, limits 2/4/8/8 (before) | Forward, 8 per object | Forward+, no early-out | Forward+ (shipped) |
|---|---|---|---|---|
| Low | 1.22 / 0.07 | 4.30 / 0.26 | 10.36 / 0.85 | 8.86 / 0.67 |
| Medium | 3.46 / 0.27 | 7.14 / 0.52 | 17.21 / 1.19 | 13.12 / 0.87 |
| High | 12.0 / 0.89 | 12.38 / 0.81 | 20.04 / 1.45 | 14.01 / 0.93 |
| Ultra | 11.3 / 0.90 | – | – | 15.34 / 0.98 |

The first column comes from an earlier round with the same setup (High agrees within 4%), and the other columns
come from one round. Both passes scale mostly with pixel count, so at 1440p (a ninth of the pixels) expect roughly
a ninth of these figures. Against the old Forward setup, that is about +0.2 ms on High, +0.45 ms on Ultra,
+1.1 ms on Medium and +0.85 ms on Low on an RTX 3070. Without the early-out, Forward+ on High cost 1.6× the old Forward
pass; with it, 1.13×. The early-out also cuts Forward's own cost on High to 8.75. Forward+ saves 0.4–0.6 ms of
CPU in the opaque pass.

The look changes on every tier, because each surface now receives every fixture that reaches it. At High the
overview frame is 2.9/255 brighter on average and 14% of pixels move by more than 8/255. Character shadows are
lighter where several spots overlap, since the spots that the cap dropped now fill them. To restore darker
shadows, retune `StationShading`.

## Start here

- `Assets/Scripts/SS3D/Rendering/URP/CortaoAmbientOcclusionFeature.cs` — `QualityLevelSettings`, `DefaultQualityLevels()`, `ResolveLevel()`; the half-resolution path is `inputScale` in `RecordRenderGraph`
- `Assets/Content/Resources/CORTAO/CortaoCommon.hlsl` — `_CortaoInputScale`: CORTAO pixel p reads screen pixel 2p at half resolution
- `Assets/Content/Resources/CORTAO/CortaoUpsample.compute` — joint bilateral upsample (plane-distance and normal weights, as `CortaoBlur`)
- `Assets/Settings/URP/SS3D_URPAsset_<Tier>.asset` — atlas size, tier pixel sizes, soft-shadow quality
- `Assets/Settings/URP/SS3D_ForwardPlusRenderer.asset` — `m_RenderingMode: 2` (Forward+), shared by every tier
- `Assets/Content/Resources/Simple Toon/Shaders/StationFlatLighting.hlsl` — `Station_AdditionalLightRadiance`: skips lights that miss the pixel before the shadow lookup
- `Assets/Content/WorldObjects/Structures/WallMounts/Lights/LightTubeFixture.prefab`, `LightBulbFixture.prefab` — `SpotLight` uses resolution tier Medium and the pipeline's soft-shadow quality

## Extension points

- New quality level: add the `QualitySettings` level, its URP asset, and a `_qualityLevels` entry on `SS3D_ForwardPlusRenderer` at the same index with `Level` set to the level's name.
- To re-measure: host a round, then accumulate `Recorder.Get("CORTAO Main Pass")` etc. per frame from `script eval`; compare per-pass sums at 2160p or with GPU clocks logged. To keep a light pass GPU-bound, enlarge the Game view (`PlayModeWindow.SetCustomRenderingResolution`) rather than raising render scale (Pitfalls).
- A custom lit shader must compile `#pragma multi_compile _ _CLUSTER_LIGHT_LOOP` and walk lights with `LIGHT_LOOP_BEGIN` / `LIGHT_LOOP_END`, with `inputData.positionWS` and `inputData.normalizedScreenSpaceUV` set (see `SteelWreckage.shader`). A shader that only loops to `GetAdditionalLightsCount()` gets no additional lights on Forward+, because that count is 0 there.

## Pitfalls

- **Per-tier AO is matched by `QualitySettings` index:** the `Level` label is not used at runtime. Adding, removing or reordering a quality level silently gives another tier's AO. `CortaoQualityLevelTests` fails when the labels stop matching `QualitySettings.names`.
- **Editor GPU timings move with load:** an Editor round is CPU/submission-bound. Removing a GPU pass lengthens `DrawOpaqueObjects` by the same time instead of shortening the camera, and the driver drops the clock when the GPU idles (RTX 3070: about 1.2 GHz at 1080p, 1.9 GHz at 2160p; switching quality level alone moved it between 0.5 and 1.9 GHz). Per-pass ms scale with the clock, so compare runs at the same clock (`nvidia-smi --query-gpu=pstate,clocks.gr`) or at 2160p. Hit 2026-11-18.
- **GPU pass timings go quiet with no error:** three setups return stale or missing GPU samples. (1) With the Game view tab behind the Scene view, the game camera renders only now and then, and the opaque samples you read are the Scene view's; focus the Game view first. (2) With the GPU at 100 % (7680×4320 on a close-up, about 25 fps), a `ProfilerRecorder` with `GpuRecorder` got 2–4 samples in 12 s, and `Recorder.gpuElapsedNanoseconds` repeated one value for hundreds of frames; 3840×2160 at about 75 % load gave one sample per frame. (3) Recorders created, reset or read inside `RenderPipelineManager` callbacks collected nothing or one frozen value. What works: create the `ProfilerRecorder`s from one `script eval`, let the round run, `CopyTo` from the next eval, and alternate A/B phases with a control pass such as `CORTAO Main Pass` (the floor tile A/B, 2026-11-18). Hit 2026-11-18.
- **Render scale other than 1 drops whole frames while the selection pick runs:** `SelectionPickRendererFeature` binds the render-scaled scene depth together with its screen-sized pick target. RenderGraph rejects the pass ("Mismatch in fragment dimensions") and the camera renders nothing that frame, so profiler recorders see no samples. Players who set render scale hit the same error. For profiling, enlarge the Game view instead. Hit 2026-11-18.
- **Forward+ cost is shadow filtering on lights that miss the pixel:** fixture ranges are long (6.12 m; 10–12 m when this was measured), so a cluster holds many of the station's lights. `GetAdditionalLight` samples and filters each one's shadow map even when the pixel is outside its cone or behind the surface. Test attenuation × wrapped diffuse first, as `Station_AdditionalLightRadiance` does. That cut High's opaque pass from 20.0 to 14.0 ms at 7680×4320. Hit 2026-11-18.
- **Atlas overflow halves every fixture shadow:** URP's `AdditionalLightsShadowCasterPass` scales all additional-light shadow slices down by 2 until the requests fit, so resolution follows how many shadowed lights are in view. Keep atlas ≥ 8 × the tier's pixel size per side (64 slices) on every asset that has additional-light shadows.

## Tests

- EditMode `CortaoQualityLevelTests`: renderer entries match `QualitySettings.names`; `ResolveLevel` clamps and falls back to full quality.
- EditMode `ForwardPlusLightingTests`: the renderer is Forward+, and every project shader that compiles `_ADDITIONAL_LIGHTS` also compiles `_CLUSTER_LIGHT_LOOP`.
- Visual: no automated image check. Compare `capture game_view --source camera` shots at a fixed resolution within one round; a new round spawns the player a few centimetres off, so pixel diffs across rounds show every edge.

## Depends on / Used by

- **Depends on:** [player-settings](../2026-10_player-settings.md) (the player's Quality setting picks the `QualitySettings` level)

## Related docs

- Parent: [rendering](rendering.md)
- Design: [rendering-lighting §8](../../design/rendering-lighting.md#8-post-processing-stack) (AO deprioritized; this keeps it off on Low)
