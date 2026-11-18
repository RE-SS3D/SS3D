> Parent map: [rendering.md](rendering.md)
> Code paths: Assets/Scripts/SS3D/Rendering/URP/CortaoAmbientOcclusionFeature.cs, Assets/Settings/URP/, Assets/Content/Resources/CORTAO/
> Status: shipped
> Verified: eb8655dfa — 2026-11-18

# Rendering quality tiers

## Overview

The station look is lit only by wall fixtures (shadowed spots plus dim unshadowed point fills), so CORTAO ambient
occlusion and the fixture spot shadows are the two costs each quality tier trades. Both are set per tier:
AO in `CortaoAmbientOcclusionFeature.QualityLevels` (one renderer is shared by all tiers, so the feature picks its
entry by `QualitySettings` level), shadows in each tier's URP asset. The number of lights per object is not
traded: every tier keeps 8, because fewer blackens walls ([Per-object lights](#per-object-lights)).

| Tier | AO | Fixture shadows (atlas, per light, filter) | Lights per object | AO cost at 1440p |
|---|---|---|---|---|
| Low | off: binds white | off (pipeline) | 8 | 0 |
| Medium | half resolution, 12 samples, 6 px blur | 2048, 256 px, soft Low | 8 | 0.45 ms |
| High | full resolution, 16 samples, 6 px blur | 4096, 512 px, soft High | 8 | 1.36 ms |
| Ultra | as High | 4096, 512 px, soft High | 8 | 1.36 ms |

Fixture spots ask for the **Medium** resolution tier and the **pipeline's** soft-shadow quality, so each URP
asset decides both: `AdditionalLightsShadowResolutionTierMedium` and `SoftShadowQuality`. Each atlas holds 64
slices at that tier's size, so the 23–26 shadowed fixtures in view on classic-ministation keep their resolution.
Before 2026-11-18, High's 2048 atlas held only 16 slices at 512 px, so URP halved every fixture shadow to 256 px
whenever more than 16 were in view (logged as "Reduced additional punctual light shadows resolution by 2").

### Measured cost

RTX 3070, D3D11, Editor host on classic-ministation, camera distance 15, vision mask off, SMAA, 29 shadowed
fixture spots. Values are median GPU ms per `ProfilingSampler` marker over ~500 frames (`Recorder.gpuElapsedNanoseconds`).
The 1080p runs had the GPU at about 1.2 GHz; 1440p and 2160p had it at 1.9 GHz (see Pitfalls).

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

### Per-object lights

The renderer is Forward (`m_RenderingMode: 0`, despite its name). URP therefore shades each mesh with at most
`m_AdditionalLightsPerObjectLimit` lights. It picks them by intensity and distance at the mesh, not by which side
of the mesh they light. Every fixture is two lights (shadowed spot, 10–12 m range, plus a 12 m PointFill). On
classic-ministation 13–27 lights reach each 1 m tile, and 1046 of the 1189 renderers in view are reached by more
than 8. Station lighting is zero on back faces and ambient is 0, so a mesh whose kept lights are all behind it
renders black. Low (2) and Medium (4) lost whole wall segments and floor patches this way. Across three camera
headings, 4.6–7.4% of the frame on Low and 0.9–1.3% on Medium went from lit to near black. Raising Low to 8 in
memory removed every black wall. A debug shader that counts the kept lights facing each surface marked the same
areas. Since 2026-11-18 every tier keeps 8, so Low and Medium pick the same lights as High. Even at 8, a wall in a
fixture-dense room sometimes keeps only lights behind it and reads dimmer than its neighbours, on High as well.

GPU ms for the opaque / transparent passes. The Game view is 7680×4320, so the GPU is the bottleneck and its
clock stays at 1.74–1.92 GHz; otherwise the setup matches the measurements above, with 52 fixture lights in view:

| Tier | Forward, old limit | Forward, 8 per object (shipped) | Forward+ |
|---|---|---|---|
| Low | 1.22 / 0.07 (limit 2) | 3.63 / 0.25 | 8.65 / 0.74 |
| Medium | 3.46 / 0.27 (limit 4) | 6.56 / 0.51 | 14.46 / 1.21 |
| High | – | 12.0 / 0.89 | 16.8 / 1.42 |
| Ultra | – | 11.3 / 0.90 | 16.7 / 1.42 |

Both passes scale mostly with pixel count, so at 1440p (a ninth of the pixels) expect roughly a ninth of these
figures. Raising the limit to 8 adds about 0.3 ms on Low and on Medium on an RTX 3070. Low at limit 4 costs
1.79 / 0.13 but still left dark patches. Forward+ has no per-object limit and lights every wall. Each pixel then
loops over every light whose range reaches its cluster, which here is most of the 52. Compared with the shipped
limit of 8, that makes the opaque pass 1.4× as expensive on High, 2.2× on Medium and 2.4× on Low. Forward+ also changes High's look: mean brightness rises
by 2.4/255 and 12% of pixels move by more than 8/255, because walls in fixture-dense rooms regain the lights
that the limit had dropped. It saves 0.4–0.6 ms of CPU in the opaque pass. Switching would also need
`SteelWreckage.shader` to compile `_CLUSTER_LIGHT_LOOP`, or wreckage loses its additional lights.

## Start here

- `Assets/Scripts/SS3D/Rendering/URP/CortaoAmbientOcclusionFeature.cs` — `QualityLevelSettings`, `DefaultQualityLevels()`, `ResolveLevel()`; the half-resolution path is `inputScale` in `RecordRenderGraph`
- `Assets/Content/Resources/CORTAO/CortaoCommon.hlsl` — `_CortaoInputScale`: CORTAO pixel p reads screen pixel 2p at half resolution
- `Assets/Content/Resources/CORTAO/CortaoUpsample.compute` — joint bilateral upsample (plane-distance and normal weights, as `CortaoBlur`)
- `Assets/Settings/URP/SS3D_URPAsset_<Tier>.asset` — atlas size, tier pixel sizes, soft-shadow quality, `m_AdditionalLightsPerObjectLimit`
- `Assets/Content/WorldObjects/Structures/WallMounts/Lights/LightTubeFixture.prefab`, `LightBulbFixture.prefab` — `SpotLight` uses resolution tier Medium and the pipeline's soft-shadow quality

## Extension points

- New quality level: add the `QualitySettings` level, its URP asset, and a `_qualityLevels` entry on `SS3D_ForwardPlusRenderer` at the same index with `Level` set to the level's name.
- To re-measure: host a round, then accumulate `Recorder.Get("CORTAO Main Pass")` etc. per frame from `script eval`; compare per-pass sums at 2160p or with GPU clocks logged. To keep a light pass GPU-bound, enlarge the Game view (`PlayModeWindow.SetCustomRenderingResolution`) rather than raising render scale (Pitfalls).
- To see which lights a mesh kept: in Play mode, compile a shader with `ShaderUtil.CreateShaderAsset` that outputs `GetAdditionalLightsCount()` and how many of those lights face the surface, and swap it onto the station renderers in view. A mesh whose kept lights all face away renders black in the station shader.

## Pitfalls

- **Per-tier AO is matched by `QualitySettings` index:** the `Level` label is not used at runtime. Adding, removing or reordering a quality level silently gives another tier's AO. `CortaoQualityLevelTests` fails when the labels stop matching `QualitySettings.names`.
- **Editor GPU timings move with load:** an Editor round is CPU/submission-bound. Removing a GPU pass lengthens `DrawOpaqueObjects` by the same time instead of shortening the camera, and the driver drops the clock when the GPU idles (RTX 3070: about 1.2 GHz at 1080p, 1.9 GHz at 2160p; switching quality level alone moved it between 0.5 and 1.9 GHz). Per-pass ms scale with the clock, so compare runs at the same clock (`nvidia-smi --query-gpu=pstate,clocks.gr`) or at 2160p. Hit 2026-11-18.
- **Render scale other than 1 drops whole frames while the selection pick runs:** `SelectionPickRendererFeature` binds the render-scaled scene depth together with its screen-sized pick target. RenderGraph rejects the pass ("Mismatch in fragment dimensions") and the camera renders nothing that frame, so profiler recorders see no samples. Players who set render scale hit the same error. For profiling, enlarge the Game view instead. Hit 2026-11-18.
- **Atlas overflow halves every fixture shadow:** URP's `AdditionalLightsShadowCasterPass` scales all additional-light shadow slices down by 2 until the requests fit, so resolution follows how many shadowed lights are in view. Keep atlas ≥ 8 × the tier's pixel size per side (64 slices) on every asset that has additional-light shadows.

## Tests

- EditMode `CortaoQualityLevelTests`: renderer entries match `QualitySettings.names`; `ResolveLevel` clamps and falls back to full quality.
- EditMode `QualityTierLightLimitTests`: while the renderer is Forward, every tier's URP asset keeps 8 lights per object.
- Visual: no automated image check. Compare `capture game_view --source camera` shots at a fixed resolution within one round; a new round spawns the player a few centimetres off, so pixel diffs across rounds show every edge.

## Depends on / Used by

- **Depends on:** [player-settings](../2026-10_player-settings.md) (the player's Quality setting picks the `QualitySettings` level)

## Related docs

- Parent: [rendering](rendering.md)
- Design: [rendering-lighting §8](../../design/rendering-lighting.md#8-post-processing-stack) (AO deprioritized; this keeps it off on Low)
