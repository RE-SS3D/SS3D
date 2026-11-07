> Code paths: Assets/Scripts/SS3D/Systems/Audio/
> Entry points: AudioSubSystem, AmbienceSubSystem, PersonalAudioSubSystem
> Status: partial
> Verified: 7785df8dc — 2026-11-06

# Audio

## Overview

Networked positional SFX/music playback, split out of [chat-audio-screens](chat-audio-screens.md)
once Phase 1 of [audio-foundation](../2026-07_audio-foundation.md) shipped client-local occlusion.
`AudioSubSystem` owns a per-client pool of `AudioSource`s; the server calls `[Server] PlayAudioSource`,
which fans out via `[ObserversRpc]` so each client plays its own pooled source at the given world
position with Unity's own distance falloff (`minDistance`/`maxDistance`). The server itself runs no
pool (`#if !UNITY_SERVER` — playback is observer-only). Per [audio.md](../../design/audio.md) §1,
occlusion and (later) ambience are **client-local presentation**, not server state — a wall muffles a
sound *for a given listener*, which the server's "play clip X at position P" RPC has no notion of.

## Start here

- `Assets/Scripts/SS3D/Systems/Audio/AudioSubSystem.cs` — pool: `PlayAudioSource` / `StopAudioSource` / `SetTimeAudioSource` (server) → `RpcPlayAudioSource` (observers); `PlayAudioSourceLocal` (returns a voice handle, accepts `startTime` for a synchronized recording) / `StopAudioSourceLocal` play on this machine's pool only (no-op on the server), for a domain's own buffered state RPC; pool sizing and periodic purge
- `Assets/Scripts/SS3D/Systems/Audio/AudioSourceOcclusion.cs` — client-local occlusion (§3, a `SS3D.Utils.LineOfSight` consumer), added to every pooled source and primed by `PrepareForPlayback` before each `Play()`; pure math in `AudioOcclusionState.cs` (`AudioOcclusionStateTests`)
- `Assets/Scripts/SS3D/Systems/Audio/AmbienceSubSystem.cs` — Phase 2 per-Area ambience crossfade (§2); self-bootstrapped, polls the local player's area every 0.5 s, two non-positional sources, no occlusion. Plays the `AmbienceProfile` for the area's resolved id (`AmbienceProfileCatalog`, loaded from Resources), or the generic `station` profile: a bed, occasional clips timed by `AmbienceScheduler`, and the catalog's thin air bed underneath. A `PowerGated` bed fades with `AreaSubSystem.IsAreaEquipmentPowered`. Outside any area, silence. Replaces legacy `AmbienceHandler.cs`.
- `Assets/Scripts/SS3D/Systems/Audio/ListenerAirTracker.cs` / `AudioAirState.cs` — air and wind at the listener from this client's atlas, no messages; pure muffle curve and wind crossfade in `AudioAirState`. `AmbienceSubSystem` publishes the level as `AudioSourceOcclusion.ListenerAir`
- `Assets/Scripts/SS3D/Systems/Audio/AmbientLoopSubSystem.cs` — machine hums (§3): 12 client-local voices lent to the nearest running `AmbientLoopSource`s, each occluded through `AudioSourceOcclusion`; choice is pure in `AmbientLoopSelection.cs`, and each kind's clip, level and reach in `AmbientLoopTrackIds.cs`
- `Assets/Scripts/SS3D/Systems/Audio/PersonalAudioSubSystem.cs` — Phase 3 owner-only heartbeat, breathing and alert ding (§4), non-positional. Driven by `HealthPersonalAudioMapper` / `StaminaPersonalAudioMapper` from each domain's existing local-owner hook.
- `Assets/Scripts/SS3D/UI/MainHud/Components/AlertStackAudioMapper.cs` — Phase 4 (§6): `HasNewAlert` diff; `MainHudSubSystem.PushAlertState` is the single funnel that calls `PlayAlertCue()`
- `Assets/Scripts/SS3D/Systems/Audio/FootstepAudio.cs` — footwear footsteps on a **client-local** loop, not the pool (host `Move(default)` clears server locomotion every tick); skips while floating, and pitches down with the gait while magboots hold
- `Assets/Scripts/SS3D/Systems/Audio/NoisyCollision.cs` — server-side item collision sounds; `CollisionSoundLimiter.cs` caps them per item and overall, through `AudioSubSystem.TryPlayCollisionSound`
- `Boombox.cs`, `ChangeMusicInteraction.cs`, `ListenerPosition.cs`, `AudioType.cs` — jukebox, music swap, listener follow, `Sfx`/`Music`/`Ambient` enum
- `Assets/Content/Systems/Audio/Resources/MainMixer.mixer` — `Master` → `Music`/`SFX`/`Ambience`/`Personal` groups, each with an exposed volume (`MasterVolume`, `MusicVolume`, `SFXVolume`, `AmbienceVolume`, `PersonalVolume`) driven by `PlayerSettingsSubSystem` ([2026-10_player-settings](../2026-10_player-settings.md)). Pool prefabs (`SFXAudioSource.prefab`, `MusicAudioSource.prefab`) target their groups by reference.
- `Assets/Scripts/SS3D/Systems/Audio/MainAudioMixer.cs` — runtime handle on the mixer (`Resources.Load`) plus group/parameter names; code-built sources (ambience, personal, footsteps) set `outputAudioMixerGroup` through `FindGroup`
- Clips, track ids and registration: child map [audio-content](audio-content.md)

## Extension points

- Positional sound: `SubSystems.Get<AudioSubSystem>().PlayAudioSource(AudioType, clipId, position, parent, ...)` (server-only). Route anything that should occlude through the pool.
- Positional sound a client derives from a message it already has: `PlayAudioSourceLocal` plays on the same pool with no message, as `StructuralAudioPresenter` does. Listener position: `AudioSourceOcclusion.TryGetListenerPosition`.
- Energy weapon reports/impacts: `CombatAudioTrackIds.FireFor(VisualStyle)` selects the registered SS14 energy set; `EnergyWeaponPrefabSetup` registers clips alongside the combat content. Ion rifle, laser rifle and taser are metal collision entries in `AudioContentPrefabRecipes`.
- A new item's collision sound: add it to `Materials` or `SilentItems` in `Audio/Editor/AudioContentPrefabRecipes.cs`, then call `SetupItemCollisions` (or **SS3D → Audio → Run Content Prefab Recipes**). `ItemPrefabs_MatchTheRecipeLists` fails until you do. A prefab variant inherits its base's entry.
- Area ambience: tag the room's department, or set an override in the Map Editor room form or with `AreaSubSystem.SetAreaAmbienceProfileId(areaId, profileId)` (server). In game, the `areaambience (profileId|clear)` console command (`AreaAmbienceCommand`) targets the caller's own area. Syncs via `RpcSyncAreaAmbience` (BufferLast).
- Machine hum: implement `IAmbientLoopState` from synced state, add an `AmbientLoopKind`, and list the prefab in `AudioContentPrefabRecipes.MachineLoops`. `MachinePrefabs_MatchTheRecipeList` fails if a prefab with that state isn't listed.
- Personal cues (e.g. virology symptoms, audio.md §4): `SetHeartbeatIntensity`, `SetStaminaBreathingIntensity` / `SetHealthBreathingIntensity` (max-merge), `SetCalmBreathingIntensity` and `SetInternalsBreathingIntensity`, a separate calm loop heard on internals alongside any labored breathing. Build no parallel non-positional path.
- Code-built `AudioSource`: set `outputAudioMixerGroup = MainAudioMixer.FindGroup(...)` so the player's category slider reaches it (ambience, personal, footsteps, blast, structural hiss, the comms chime and main-menu music all do). Unrouted sources play under Master only.
- Looping machine sound (e.g. `FuelPowerGenerator`): sync the running state with an `[ObserversRpc(BufferLast = true)]` that calls `PlayAudioSourceLocal` / `StopAudioSourceLocal`.
- New positional source: add `AudioSourceOcclusion` and call `PrepareForPlayback` before `Play()`, or it ignores walls and keeps playing in vacuum. Set `IgnoresWalls` for one that should only take the air.
- Volume settings: `SubSystems.Get<PlayerSettingsSubSystem>().SetVolume(PlayerVolume, linear)`; never `SetFloat` the exposed volumes from elsewhere, the next settings apply overwrites them.
- Main-menu music is `SS3D.Launcher.MainMenuMusic` (non-positional, Music group, tracks from the main-menu catalog), outside this folder.
- Not built yet: PDA notification cues, in-game lobby music bed (Phase 5); see [audio-foundation](../2026-07_audio-foundation.md).

## Pitfalls

- **A prefab recipe save bakes `[RequireComponent]` into the prefab.** `SaveAsPrefabAsset` added `PlacedTileObject` and `AuthLogDeviceBehaviour`, both `NetworkBehaviour`s, to GasPump, AirAlarm and APC when `SetupMachineLoops` first ran, and refreshed stale fields. The machine loops went in by text to keep those out. Read the prefab diff after any recipe run.
- **Missing atmos data is not vacuum.** A pure client's atlas holds only chunks that changed since it joined, so `MaskEmpty` or a tile outside the atlas means unknown. Reading it as 0 kPa silences every late joiner with no error. `AudioAirState.ResolveAir` calls it air unless the server's `IsFloating` says open space, so a late joiner who missed a settled breach hears air there until [atmos client sync](../2026-07_atmos-client-visualization-sync.md) Phase 2 sends a late-join bootstrap. Designed around 2026-11-05.
- **`AudioSourceOcclusion` can be asked to play where `Awake` never ran.** EditMode tests drive `StructuralIntegrityPresenter`, so `AddComponent` then `PrepareForPlayback` must work with no `Awake`; it caches its components itself. Hit 2026-11-05.
- **Occlusion is client-local — do not gate it behind `[Server]`/RPC.** The listener position only
  exists on each client; baking a muffled/clear decision into the server RPC would require per-observer
  variants of every play call. `AudioSourceOcclusion.Update()` runs unconditionally on whichever machine
  owns the `AudioSource` instance (clients only, since the server never builds a pool).
- **Reuse `SS3D.Utils.LineOfSight`, do not fork a raycast.** Same shared occluder mask
  (`LayerMask.GetMask("Default")`) as Drop, combat LOS, and comms occlusion — solid geometry that
  should occlude sits on the `Default` layer in this project, not a dedicated `Walls` layer.
- **Occlusion rays must not cast at feet height.** `ListenerPosition` follows the body root, and
  plenum/airlock-tile colliders on `Default` top out near y=0 — a root-height ray false-positives on
  the floor and muffles every pooled SFX. Sample planar LOS at chest height
  (`AudioSourceOcclusion.OcclusionSampleHeight`) so walls still block and floors don't.
- **Item collision sounds can't be client-side.** `Item.OnStart` makes rigidbodies kinematic on client-only peers, so `OnCollisionEnter` only fires on the server. Each sound is a message, which is why `CollisionSoundLimiter` runs there; don't call `PlayAudioSource` from a new collision path without it.
- **`AudioMixer.SetFloat` is ignored when called from `Awake`.** `PlayerSettingsSubSystem` loads in
  `OnAwake` but first applies volumes in `OnStart`; an exposed parameter set any earlier silently keeps
  its snapshot value.
- **Exposed volumes replace the snapshot value.** The `Ambience` group's snapshot level (≈−10 dB) is
  overwritten by the Ambience slider at boot. Balance levels in the settings defaults, not the snapshot.
- **`MainMixer` Master defaults to 0 dB.** The inherited snapshot had Master at ≈−20 dB (~10% linear);
  pooled SFX/Music route through it, so that alone made every diegetic sound inaudibly quiet even with
  clear LOS. Per-category slider balancing stays Phase 5 — don't re-bury Master as a stand-in for it.
- **Do not call `LayerMask.GetMask` / `NameToLayer` from a MonoBehaviour field initializer or static
  ctor.** Unity throws `UnityException` during `AddComponent` type init (pool create path hit this on
  `AudioSourceOcclusion`). Resolve lazily or in `Awake` — same rule `VisionSubSystem` already documents.
- **Pooled sources are never destroyed between plays** — the same `GameObject` is reused, so
  `AudioSourceOcclusion` state (cutoff, volume scale, occlusion flag) persists across unrelated clips
  unless reset. `AudioSubSystem.RpcPlayAudioSource` must call `PrepareForPlayback(volume)` before every
  `Play()` — skipping this leaves a newly played sound inheriting the previous clip's muffle.
- **`AmbienceHandler` is legacy — do not wire new features onto it.** No `AreaSubSystem` awareness, no
  power gating; `AmbienceSubSystem` (Phase 2) replaces it wholesale rather than extending its
  `_air`/`_windiness` fields.
- **Breathing contributors must max-merge:** stamina and health both drive heavy breathing. Use
  `SetStaminaBreathingIntensity` / `SetHealthBreathingIntensity` — a single setter lets the last
  writer silence the other domain's cue.
- **"New alert" means None → any severity, not any change.** `AlertStackAudioMapper.HasNewAlert`
  deliberately does not fire on Warning → Critical escalation — the visual chip is already showing,
  so re-cueing it would contradict §6's "not a continuous loop, only draws attention when something's
  actually wrong" restraint. Route any future alert-audio change through this diff, not a raw
  field-by-field equality check.
- **`StopAudioSource(parent)` stops the first child `AudioSource` under that NetworkObject.** Footsteps
  parent their pooled loop to the player body — don't parent unrelated one-shots to the same
  NetworkObject or a stop (idle / footwear swap) can kill the wrong clip. Prefer `parent: null` for
  transient SFX (gunfire already does).
- **A process-wide DDOL subsystem outlives any one player body.** `AmbienceSubSystem` never gets
  destroyed/recreated across disconnect/respawn/map-reload the way a `NetworkSubSystem` on the hub
  does, so it clears its own `_lastResolvedAreaId`/`_currentTrackId` when `LocalPlayerObjectChanged`
  reports no body — otherwise ambience loops the last live area's track forever with nothing left to
  poll, and a fresh map's `AreaId`s can numerically collide with the old ones. `PersonalAudioSubSystem`
  used to trust its driving consumers alone (`HumanHealthController.ClearScreenEffectsIfDriving`), but
  stamina never pushes a final zero and the health clear skipped internals, so breathing kept looping
  under the launcher after Leave. It now zeroes every target when `LocalPlayerObjectChanged` names a
  different (or no) body, and stops every source outright on `SceneManager.sceneUnloaded` for Game.
  Do not rely on `LocalPlayerObjectChanged(false)` alone at session end: `Entity.OnDestroyed` checks
  `IsOwner`, which is already false by then.
- **`PlayAudioSource` reaches only the observers present at that moment.** A loop started that way
  (machine hum, generator) is silent for late joiners and clients re-entering AOI. Persist the state in
  a `BufferLast` ObserversRpc that calls `PlayAudioSourceLocal`; and call `PlayAudioSource` /
  `StopAudioSource` from server code only (from a client they log "server is not active" and return).

## Tests

- EditMode: `AudioAirStateTests`, `AudioOcclusionStateTests`, `HealthPersonalAudioMapperTests`, `StaminaPersonalAudioMapperTests`, `AlertStackAudioMapperTests`, `PlayerAudioMixTests`, `PlayerSettingsStoreTests`. `Testing/ui/validate.sh --steps mppm:breach` proves a remote client hears wind and thinning air from its own patches. Pool playback, occlusion and the mix itself have no automated coverage; check them by ear.

## Depends on / Used by

- **Depends on:** [area](area.md) (`AreaRecord.EffectiveAmbienceProfileId`, `TryResolveAreaIdForWorldPosition`, `TryGetAmbienceProfileId`), [electricity](electricity.md) (`MachinePowerConsumer` gates Boombox; `FuelPowerGenerator` loop), [entities](entities.md) (`LocalPlayerObjectChanged` — `ListenerPosition` / `AmbienceSubSystem`)
- **Used by:** [chat-audio-screens](chat-audio-screens.md); [health](health.md) (`HealthPersonalAudioMapper`, hit/gasp/blood via pool); [stamina](stamina.md) (`StaminaPersonalAudioMapper`); [inventory](inventory.md) (`AlertStackAudioMapper` / `MainHudSubSystem.PushAlertState`); [combat](combat.md) (gunfire/reload + `FleshHit`); [furniture](furniture.md) (airlock open/close/deny + boombox; the cracked-wall hiss is a consolidation candidate); [structural-destruction](structural-destruction.md) (impacts, breaks, blasts via `PlayAudioSourceLocal`)

## Related docs

- Design (read-only): [Documents/design/audio.md](../../design/audio.md)
- Architecture effort: [2026-07_audio-foundation](../2026-07_audio-foundation.md); [ambience-and-impact-audio](../../archive/architecture/2026-11_ambience-and-impact-audio.md); health feel clip registration also in [2026-07_health-env-feel](../../archive/architecture/2026-07_health-env-feel.md)
- Precedent: [2026-07_screen-space-effects](../../archive/architecture/2026-07_screen-space-effects.md) (client-local snapshot-mapper pattern)
