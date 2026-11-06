> Parent map: [audio.md](audio.md)
> Code paths: Assets/Content/Data/Databases/Sounds.asset, Assets/Content/Data/ObjectAssetReferences/, `*AudioTrackIds.cs` per domain
> Status: partial
> Verified: 7785df8dc — 2026-11-06

# Audio content (clips and track ids)

## Overview

How sound clips are found, registered and named in code, plus which registered clips are wired to anything. Playback mechanics (pool, occlusion, ambience, personal audio) live in the parent [audio](audio.md) map.

## Start here

- `Assets/Content/Data/Databases/Sounds.asset` — the sound database; clips resolve via `Assets.Get<AudioClip>(AssetDatabases.Sounds, id)`, the single clip-lookup path
- Per-domain id classes name registered clips instead of inlining GUIDs:
  - `Systems/Audio/AudioTrackIds.cs` — personal audio (`Heartbeat`, `HeavyBreathing`, `AlertCue`)
  - `Systems/Audio/FootstepAudioTrackIds.cs` — Socks / Shoes / Boots under `Assets/Art/Sound/Entities/Humanoid/Footsteps/`
  - `Systems/Audio/AmbientLoopTrackIds.cs` — machine loops, with each `AmbientLoopKind`'s level and reach
  - `Systems/Area/LightSwitchAudioTrackIds.cs` — wall switch click (`LightSwitchController.Toggle`)
  - `Systems/Audio/VendorAudioTrackIds.cs` — vending dispense (`VendingMachineController.PlayVendSound`)
  - `Systems/Furniture/AirlockAudioTrackIds.cs` — airlock deny (`AirLockOpener.ServerPlayAccessDenied`); open/close use generated `Sounds.AirlockOpen` / `AirlockClose`
  - `Systems/Comms/CommsAudioTrackIds.cs` — `Announce.ogg` chime, optional `Welcome.ogg` follow-up
  - `CombatAudioTrackIds` (gunfire / surface / `FleshHit` arrays, empty click, reload), `HealthAudioTrackIds` (gasp / choke / scream / blood)
  - `Systems/Audio/ItemCollisionAudioTrackIds.cs` — clip sets per `ItemCollisionMaterial` (metal, heavy metal, can, wood, plastic, glass) for `NoisyCollision`
  - `Systems/StructuralDamage/StructuralAudioTrackIds.cs` — hit, strain and break sets per material, wreckage thud, blast near and far, under `Assets/Art/Sound/World/Destruction/`
- `Systems/Combat/CombatAudioTrackIds.cs` — ballistic clips plus `FireFor(VisualStyle)` for energy weapons. SS14 `ship_svalinn` (laser), `plasma_cutter` (ion), `taser2`, `energy_metal1–2`, `energy_meat1–2`, and machine `beep` are under `Assets/Art/Sound/Items/Weapons/Energy/SS14/`, registered by `EnergyWeaponPrefabSetup` on the combat recipe aggregator. Credits and source paths are in ATTRIBUTIONS.
- `Assets/Scripts/SS3D/Data/Generated/Sounds.cs` — generated constants for the older clips (airlock open/close, bike horn, cans, generator, title stings)
- `Assets/Resources/AmbienceProfileCatalog.asset` lists the `AmbienceProfile` assets in `Assets/Content/Data/Ambience/`. Profiles reference their `AudioClip`s directly, so room tone needs no `Sounds.asset` entry. The catalog also holds the three wind layers and the space bed. Clips live under `Assets/Art/Sound/World/Ambience/`, and long ones import as Compressed In Memory, because the catalog holds them all.
- Third-party credits: [Sound ATTRIBUTIONS](../../../Assets/Art/Sound/ATTRIBUTIONS.md). Sound effects may be CC0, CC-BY, CC-BY-SA or CC-BY-NC; a commercial release would have to replace the NC clips (metal breaks), and custom-licensed music stays out. An SS14 clip with no `attributions.yml` entry stays out.

- `Systems/StructuralDamage/Editor/ExplosivePrefabSetup.cs` registers the five-second minibomb and fifteen-second charge countdowns, defused cue and small explosion under `Assets/Art/Sound/Items/Weapons/Explosive/SS14/`; `TimedCharge` seeks a single non-looping countdown recording from the synchronized fuse; C4/X4 reuse the registered `Explosion1` clip.

## Extension points

- **Find a clip first** in [art-asset-index.md](../../art-asset-index.md) → `art-available-for-import.json`. SS3D-Art has hundreds of unimported sounds, and some imported ones aren't registered yet.
- **Register a clip:** create an `ObjectAssetReference` under `Assets/Content/Data/ObjectAssetReferences/` with `Id = <clip asset GUID>` and `Database = AssetDatabases.Sounds`'s `DatabaseID`. Then add a matching `_key`/`_value` entry to `Sounds.asset`'s `Assets._list` (`_value` is `{fileID: 8300000, guid: <clip guid>, type: 3}`). Name it in the owning domain's `*AudioTrackIds` class. For a third-party import, add a row to ATTRIBUTIONS.
- **Registered but not wired (content decides):** `SpaceAmbience1` and `3`. `GrilleHit`, `Rod1` and `TrayHit1-2` aren't in any `NoisyCollision` set.
- **Ad-hoc `AudioSource` users outside the pool:** `StructuralIntegrityPresenter` (hiss) and `FootstepAudio`, both routed to the SFX group and occluded through `AudioSourceOcclusion`.

## Depends on / Used by

- **Depends on:** [data-codegen](data-codegen.md) (asset databases)
- **Used by:** every domain with an `*AudioTrackIds` class above

## Related docs

- Parent: [audio](audio.md)
- Design (read-only): [audio.md](../../design/audio.md) §2, §3, §10
