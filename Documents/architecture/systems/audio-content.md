> Parent map: [audio.md](audio.md)
> Code paths: Assets/Content/Data/Databases/Sounds.asset, Assets/Content/Data/ObjectAssetReferences/, `*AudioTrackIds.cs` per domain
> Status: partial
> Verified: e2f86cf20 — 2026-11-05

# Audio content (clips and track ids)

## Overview

How sound clips are found, registered and named in code, plus which registered clips are wired to anything. Playback mechanics (pool, occlusion, ambience, personal audio) live in the parent [audio](audio.md) map.

## Start here

- `Assets/Content/Data/Databases/Sounds.asset` — the sound database; clips resolve via `Assets.Get<AudioClip>(AssetDatabases.Sounds, id)`, the single clip-lookup path
- Per-domain id classes name registered clips instead of inlining GUIDs:
  - `Systems/Audio/AudioTrackIds.cs` — personal audio (`Heartbeat`, `HeavyBreathing`, `AlertCue`)
  - `Systems/Audio/FootstepAudioTrackIds.cs` — Socks / Shoes / Boots under `Assets/Art/Sound/Entities/Humanoid/Footsteps/`
  - `Systems/Audio/VendorAudioTrackIds.cs` — vending dispense (`VendingMachineController.PlayVendSound`)
  - `Systems/Furniture/AirlockAudioTrackIds.cs` — airlock deny (`AirLockOpener.ServerPlayAccessDenied`); open/close use generated `Sounds.AirlockOpen` / `AirlockClose`
  - `Systems/Comms/CommsAudioTrackIds.cs` — `Announce.ogg` chime, optional `Welcome.ogg` follow-up
  - `CombatAudioTrackIds` (gunfire / surface / `FleshHit` arrays, empty click, reload), `HealthAudioTrackIds` (gasp / choke / scream / blood)
- `Assets/Scripts/SS3D/Data/Generated/Sounds.cs` — generated constants for the older clips (airlock open/close, bike horn, cans, generator, title stings)
- Third-party credits: [Sound ATTRIBUTIONS](../../../Assets/Art/Sound/ATTRIBUTIONS.md)

## Extension points

- **Find a clip first** in [art-asset-index.md](../../art-asset-index.md) → `art-available-for-import.json`. SS3D-Art has hundreds of unimported sounds, and some imported ones aren't registered yet.
- **Register a clip:** create an `ObjectAssetReference` under `Assets/Content/Data/ObjectAssetReferences/` with `Id = <clip asset GUID>` and `Database = AssetDatabases.Sounds`'s `DatabaseID`. Then add a matching `_key`/`_value` entry to `Sounds.asset`'s `Assets._list` (`_value` is `{fileID: 8300000, guid: <clip guid>, type: 3}`). Name it in the owning domain's `*AudioTrackIds` class. For a third-party import, add a row to ATTRIBUTIONS.
- **Registered but not wired (content decides):** `StationAmbience1-15`, `SpaceAmbience1-3`, `AirStagnant`, `WindHeavy/Light/Violent` and `PowerHum` are candidate `areaambience` track ids. `MetalHit1-2`, `GrilleHit`, `Rod1`, `Tap`, `TrayHit1-2` and `WoodHit1` are for `NoisyCollision`, which only `SodaCanGeneric.prefab` uses so far (with `Can1-4`).
- **Ad-hoc `AudioSource` users that skip occlusion:** `StructuralIntegrityPresenter` (hiss) and `BlastExplosionEffect`, both routed to the SFX group. Moving them onto `PlayAudioSource` is deferred content work ([audio-foundation](../2026-07_audio-foundation.md)). Airlocks, bike horn, vending and the fuel generator already use the pool. UI chimes (`CommsFeedController` announce/welcome) and main-menu music stay client-local and non-spatial on purpose.

## Depends on / Used by

- **Depends on:** [data-codegen](data-codegen.md) (asset databases)
- **Used by:** every domain with an `*AudioTrackIds` class above

## Related docs

- Parent: [audio](audio.md)
- Design (read-only): [audio.md](../../design/audio.md) §2, §3, §10
