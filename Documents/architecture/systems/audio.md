> Code paths: Assets/Scripts/SS3D/Systems/Audio/
> Entry points: AudioSubSystem, AmbienceSubSystem, PersonalAudioSubSystem
> Status: partial
> Verified: e2f86cf20 — 2026-11-05

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

- `Assets/Scripts/SS3D/Systems/Audio/AudioSubSystem.cs` — pool: `PlayAudioSource` / `StopAudioSource` / `SetTimeAudioSource` (server) → `RpcPlayAudioSource` (observers); `PlayAudioSourceLocal` / `StopAudioSourceLocal` play on this machine's pool only (no-op on the server), for a domain's own buffered state RPC; pool sizing and periodic purge
- `Assets/Scripts/SS3D/Systems/Audio/AudioSourceOcclusion.cs` — client-local occlusion (§3, a `SS3D.Utils.LineOfSight` consumer), added to every pooled source and primed by `PrepareForPlayback` before each `Play()`; pure math in `AudioOcclusionState.cs` (`AudioOcclusionStateTests`)
- `Assets/Scripts/SS3D/Systems/Audio/AmbienceSubSystem.cs` — Phase 2 per-Area ambience crossfade (§2); self-bootstrapped, polls the local player's area every 0.5 s, two non-positional sources, no occlusion. Replaces legacy `AmbienceHandler.cs`.
- `Assets/Scripts/SS3D/Systems/Audio/PersonalAudioSubSystem.cs` — Phase 3 owner-only heartbeat, breathing and alert ding (§4), non-positional. Driven by `HealthPersonalAudioMapper` / `StaminaPersonalAudioMapper` from each domain's existing local-owner hook.
- `Assets/Scripts/SS3D/UI/MainHud/Components/AlertStackAudioMapper.cs` — Phase 4 (§6): `HasNewAlert` diff; `MainHudSubSystem.PushAlertState` is the single funnel that calls `PlayAlertCue()`
- `Assets/Scripts/SS3D/Systems/Audio/FootstepAudio.cs` — footwear footsteps on a **client-local** loop, not the pool (host `Move(default)` clears server locomotion every tick); skips while floating
- `Boombox.cs`, `NoisyCollision.cs`, `ChangeMusicInteraction.cs`, `ListenerPosition.cs`, `AudioType.cs` — jukebox, collision SFX, music swap, listener follow, `Sfx`/`Music`/`Ambient` enum
- `Assets/Content/Systems/Audio/Resources/MainMixer.mixer` — `Master` → `Music`/`SFX`/`Ambience`/`Personal` groups, each with an exposed volume (`MasterVolume`, `MusicVolume`, `SFXVolume`, `AmbienceVolume`, `PersonalVolume`) driven by `PlayerSettingsSubSystem` ([2026-10_player-settings](../2026-10_player-settings.md)). Pool prefabs (`SFXAudioSource.prefab`, `MusicAudioSource.prefab`) target their groups by reference.
- `Assets/Scripts/SS3D/Systems/Audio/MainAudioMixer.cs` — runtime handle on the mixer (`Resources.Load`) plus group/parameter names; code-built sources (ambience, personal, footsteps) set `outputAudioMixerGroup` through `FindGroup`
- Clips, track ids and registration: child map [audio-content](audio-content.md)

## Extension points

- Positional sound: `SubSystems.Get<AudioSubSystem>().PlayAudioSource(AudioType, clipId, position, parent, ...)` (server-only). Route anything that should occlude through the pool.
- Area ambience: `AreaSubSystem.SetAreaAmbienceTrackId(areaId, trackId)` (server). In game, the `areaambience (trackId|clear)` console command (`AreaAmbienceCommand`) targets the caller's own area. Syncs via `RpcSyncAreaAmbience` (BufferLast).
- Personal cues (e.g. virology symptoms, audio.md §4): `SetHeartbeatIntensity`, `SetStaminaBreathingIntensity` / `SetHealthBreathingIntensity` (max-merge). Build no parallel non-positional path.
- Code-built `AudioSource`: set `outputAudioMixerGroup = MainAudioMixer.FindGroup(...)` so the player's category slider reaches it (ambience, personal, footsteps, blast, structural hiss, the comms chime and main-menu music all do). Unrouted sources play under Master only.
- Looping machine sound (e.g. `FuelPowerGenerator`): sync the running state with an `[ObserversRpc(BufferLast = true)]` that calls `PlayAudioSourceLocal` / `StopAudioSourceLocal`.
- Volume settings: `SubSystems.Get<PlayerSettingsSubSystem>().SetVolume(PlayerVolume, linear)`; never `SetFloat` the exposed volumes from elsewhere, the next settings apply overwrites them.
- Main-menu music is `SS3D.Launcher.MainMenuMusic` (non-positional, Music group, tracks from the main-menu catalog), outside this folder.
- Not built yet: power-gated ambience, PDA notification cues, in-game lobby music bed (Phase 5); see [audio-foundation](../2026-07_audio-foundation.md).

## Pitfalls

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
- **`AreaRecord.AmbienceTrackId` is not persisted.** Absent from `SavedAreaRecord` /
  `BuildSavedAreaRecords` / `RestoreFromSave` — authoring via `SetAreaAmbienceTrackId` resets on the
  next map load/restore until a persistence contributor is added ([persistence](persistence.md)).
- **No per-track power-gating data field yet.** `audio.md` §2 says "some tracks are honestly
  power-dependent, not all of them" — that needs a per-track opt-in (mirroring
  `AreaRecord.HasDepartmentalLightTint`) before `AmbienceSubSystem` can silence a hum on `Dark`. Not
  built here on purpose: inventing the flag without an authoring surface or content decision on which
  tracks use it would be dead schema. `AreaSubSystem.OnAreaLightingStateChanged` /
  `TryGetLightingStateForTile` are the signal to consume once the flag exists.
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
  sidesteps this the way `ScreenEffectsSubSystem` does: it never watches for disconnect itself — the
  driving consumer (`HumanHealthController.ClearScreenEffectsIfDriving`, same call site as the
  screen-effect clear) zeroes the intensity on ownership loss instead.
- **`PlayAudioSource` reaches only the observers present at that moment.** A loop started that way
  (machine hum, generator) is silent for late joiners and clients re-entering AOI. Persist the state in
  a `BufferLast` ObserversRpc that calls `PlayAudioSourceLocal`; and call `PlayAudioSource` /
  `StopAudioSource` from server code only (from a client they log "server is not active" and return).

## Tests

- EditMode: `AudioOcclusionStateTests`, `HealthPersonalAudioMapperTests`, `StaminaPersonalAudioMapperTests`, `AlertStackAudioMapperTests`, `PlayerAudioMixTests`, `PlayerSettingsStoreTests`. Pool playback, occlusion and ambience have no automated coverage; check them in Play Mode.

## Depends on / Used by

- **Depends on:** [area](area.md) (`AreaRecord.AmbienceTrackId`, `TryResolveAreaIdForWorldPosition`, `TryGetAmbienceTrackId`), [electricity](electricity.md) (`MachinePowerConsumer` gates Boombox; `FuelPowerGenerator` loop), [entities](entities.md) (`LocalPlayerObjectChanged` — `ListenerPosition` / `AmbienceSubSystem`)
- **Used by:** [chat-audio-screens](chat-audio-screens.md); [health](health.md) (`HealthPersonalAudioMapper`, hit/gasp/blood via pool); [stamina](stamina.md) (`StaminaPersonalAudioMapper`); [inventory](inventory.md) (`AlertStackAudioMapper` / `MainHudSubSystem.PushAlertState`); [combat](combat.md) (gunfire/reload + `FleshHit`); [furniture](furniture.md) (airlock open/close/deny + boombox; structural-destruction ad-hoc emitters are consolidation candidates)

## Related docs

- Design (read-only): [Documents/design/audio.md](../../design/audio.md)
- Architecture effort: [2026-07_audio-foundation](../2026-07_audio-foundation.md); health feel clip registration also in [2026-07_health-env-feel](../../archive/architecture/2026-07_health-env-feel.md)
- Precedent: [2026-07_screen-space-effects](../../archive/architecture/2026-07_screen-space-effects.md) (client-local snapshot-mapper pattern)
