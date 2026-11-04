> Implements: Documents/design/rendering-lighting.md
> Touches systems: rendering, electricity
> Status: in-progress

**Progress:** Half-toon fixture color/intensity response and shadow attenuation corrected. Finite palette normal frames and a first fixture/grade candidate applied. Skin/hair/cut and eight floor materials now use the same half-toon response. Softer spots, lower fixture fills and direct-light contact occlusion are checked in the running ministation; owner visual acceptance remains open.

# URP lighting look polish (Jul 2026)

Handoff for whoever owns the **visual plate** after Phase 1 systems land on
`feature/urp-lighting-phase1-v2`. Systems work is largely in; matching the MKII /
PR #857 security-bay reference is **not** finished and should not be chased by
blindly copying Built-in numbers.

## Goal

Restore a moodier fixture-driven station look on URP 17 Forward+ that reads like
upstream [PR #857](https://github.com/RE-SS3D/SS3D/pull/857) / [PR #283](https://github.com/RE-SS3D/SS3D/pull/283)
reference screenshots — without breaking dark rooms, emergency cones, or inventory icons.

## What already shipped (keep)

| Piece | Where |
|-------|--------|
| Zero ambient / no main light | `Game.unity`, `SS3D_URPAsset` |
| Half-toon + Forward+ additional lights | `STCore` / `STLighting` / `STFragment` |
| Floor normals (derivative TBN) | `STFragment.hlsl`, floor mats |
| Dual fixture lights (Spot + PointFill) | `LightTubeFixture` / `LightBulbFixture`, `LightPower` |
| Emergency: dim short-range spot, fill **off** | `LightPower` |
| Gameplay volume + contact occlusion | `SS3D_GameplayVolumeProfile`, Forward+ renderer — SSAO enabled; opaque ST passes consume direct-light AO |
| Icon preview lights | `RuntimePreviewGenerator` |

## Critical constraint — do not copy Built-in intensities

Half-toon now multiplies albedo by fixture radiance instead of using the legacy
`_AmbientCol` blend, which discarded light color and intensity on opaque palette
materials. Reassess existing intensities before tuning falloff: the old values
were authored against an intensity-insensitive diffuse response. The near-field
limiter applies to distance attenuation before shadow visibility.

PR #857 PointLight `2 / 10` at `z ≈ 0.36` only looked good because
`UnityDeferredLibrary.cginc` applied a **custom near-field falloff** that capped
wall blowout. URP uses stock `distanceAttenuation`.

**Do not** paste 857 prefab intensities/positions onto URP fixtures and expect the
same plate. Either:

1. Eye-tune for URP (preferred short-term), or
2. Port a soft-near / carry-far atten into `STLighting.hlsl` (partial soft-near already
   present to reduce head hotspots — extend toward 857’s curve if chasing wall-adjacent fills).

## Last eye-tuned snapshot worth revisiting

Before the failed “copy 857 literally” pass, a more acceptable mid-room fill was
roughly:

| | Tube Spot | Tube PointFill |
|---|---|---|
| Intensity | ~1.3 | ~1.4–1.8 |
| Range | ~10–12 | ~12 |
| Position | wall, pitched ~30° | `y ≈ 1.9–2.3`, `z ≈ 1.25–2.0` (into room) |

Commit landmarks on this branch:

- `e6a5fc50e` — fill lights + floor normals
- `acb7c731f` / `8928399c4` — grade + deeper fill experiments
- `4c9ba60cd` — emergency short-range spot-only
- `f18e7ef39` — icon preview lighting

Prefer those over the brief 857-literal prefab numbers (wall-hug point + intensity 2).

## Open tuning checklist

- [x] Constant palette UVs retain finite normals (carpet/vendor lighting streaks)
- [x] Half-toon preserves fixture radiance (color and intensity); no double light-color multiplication in specular
- [x] Near-field limiter preserves partial shadow visibility
- [ ] Visually accept the fixture candidate: tube spot/fill 3.4/0.6, bulb 2.8/0.6, warm ivory, 130° outer / 70° inner spot, high-quality soft filtering at medium resolution, shadow strength 0.85; unshadowed fills at local y=1.9/z=1.5
- [ ] PointFill: room center vs wall hotspot vs player-head plastic shine
- [ ] Spot angle/range for contact shadows without washing walls
- [ ] Visually accept the grade candidate: Neutral tonemapping, bloom 1.3 / threshold 0.8, contrast/saturation 5, vignette 0.06; exposure remains 0.51
- [ ] Visually accept contact occlusion: intensity 1 / radius 0.32 / direct strength 0.8 / downsampled; opaque ST now samples AO instead of bypassing it
- [ ] Floor specular vs prop `Palette` specular (keep characters `_SpecIntensity: 0`)
- [ ] Confirm `NormalOnly` fixtures extinguish in Emergency; only `EmergencyCapable` stay on
- [ ] Optional: full 857-style atten curve in half-toon additional lights

## Divergence from design

[Documents/design/rendering-lighting.md](../design/rendering-lighting.md) §8 prefers Neutral
tonemap and treats AO as mostly moot. Earlier plate experiments chased the **reference screenshot**
(SSAO on, filmic grade). **Current candidate:** SSAO enabled for contact grounding, with opaque ST explicitly consuming direct-light AO. The previous all-one AO factors bypassed that contribution; the earlier conclusion that AO barely changed ST lighting is no longer a sound tuning baseline. This is a deliberate deviation from §8, evaluated in the running ministation. Keep DepthNormals for Decal Layers regardless of AO state.

## Out of scope for polish pass

- SSRT / blob drop shadows (PR #283)
- Light-budget manager
- Full material migration of every leftover URP Lit surface
