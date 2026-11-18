> Parent map: [rendering.md](rendering.md)
> Code paths: Assets/Scripts/SS3D/Rendering/URP/CortaoAmbientOcclusionFeature.cs, Assets/Settings/URP/, Assets/Content/Resources/CORTAO/
> Status: shipped
> Verified: eb8655dfa — 2026-11-18

# Rendering quality tiers

## Overview

The station look is lit only by wall fixtures (shadowed spots plus dim unshadowed point fills), so CORTAO ambient
occlusion and the fixture spot shadows are the two costs each quality tier trades. Both are set per tier:
AO in `CortaoAmbientOcclusionFeature.QualityLevels` (one renderer is shared by all tiers, so the feature picks its
entry by `QualitySettings` level), shadows in each tier's URP asset.

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

## Start here

- `Assets/Scripts/SS3D/Rendering/URP/CortaoAmbientOcclusionFeature.cs` — `QualityLevelSettings`, `DefaultQualityLevels()`, `ResolveLevel()`; the half-resolution path is `inputScale` in `RecordRenderGraph`
- `Assets/Content/Resources/CORTAO/CortaoCommon.hlsl` — `_CortaoInputScale`: CORTAO pixel p reads screen pixel 2p at half resolution
- `Assets/Content/Resources/CORTAO/CortaoUpsample.compute` — joint bilateral upsample (plane-distance and normal weights, as `CortaoBlur`)
- `Assets/Settings/URP/SS3D_URPAsset_<Tier>.asset` — atlas size, tier pixel sizes, soft-shadow quality
- `Assets/Content/WorldObjects/Structures/WallMounts/Lights/LightTubeFixture.prefab`, `LightBulbFixture.prefab` — `SpotLight` uses resolution tier Medium and the pipeline's soft-shadow quality

## Extension points

- New quality level: add the `QualitySettings` level, its URP asset, and a `_qualityLevels` entry on `SS3D_ForwardPlusRenderer` at the same index with `Level` set to the level's name.
- To re-measure: host a round, then accumulate `Recorder.Get("CORTAO Main Pass")` etc. per frame from `script eval`; compare per-pass sums at 2160p or with GPU clocks logged.

## Pitfalls

- **Per-tier AO is matched by `QualitySettings` index:** the `Level` label is not used at runtime. Adding, removing or reordering a quality level silently gives another tier's AO. `CortaoQualityLevelTests` fails when the labels stop matching `QualitySettings.names`.
- **Editor GPU timings move with load:** an Editor round is CPU/submission-bound. Removing a GPU pass lengthens `DrawOpaqueObjects` by the same time instead of shortening the camera, and the driver drops the clock when the GPU idles (RTX 3070: about 1.2 GHz at 1080p, 1.9 GHz at 2160p; switching quality level alone moved it between 0.5 and 1.9 GHz). Per-pass ms scale with the clock, so compare runs at the same clock (`nvidia-smi --query-gpu=pstate,clocks.gr`) or at 2160p. Hit 2026-11-18.
- **Atlas overflow halves every fixture shadow:** URP's `AdditionalLightsShadowCasterPass` scales all additional-light shadow slices down by 2 until the requests fit, so resolution follows how many shadowed lights are in view. Keep atlas ≥ 8 × the tier's pixel size per side (64 slices) on every asset that has additional-light shadows.

## Tests

- EditMode `CortaoQualityLevelTests`: renderer entries match `QualitySettings.names`; `ResolveLevel` clamps and falls back to full quality.
- Visual: no automated image check. Compare `capture game_view --source camera` shots at a fixed resolution within one round; a new round spawns the player a few centimetres off, so pixel diffs across rounds show every edge.

## Depends on / Used by

- **Depends on:** [player-settings](../2026-10_player-settings.md) (the player's Quality setting picks the `QualitySettings` level)

## Related docs

- Parent: [rendering](rendering.md)
- Design: [rendering-lighting §8](../../design/rendering-lighting.md#8-post-processing-stack) (AO deprioritized; this keeps it off on Low)
