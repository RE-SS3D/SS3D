> Implements: Documents/design/audio.md §2 (ambience), §3 (diegetic SFX + occlusion), §4 (personal audio), §5 (music), §6 (alerts), §7 (volume categories)
> Touches systems: audio, area, electricity, health, stamina, main-hud, rounds-lobby, structural-destruction, furniture
> Status: in-progress

**Progress:** Phase 1–4 shipped; Phase 5 (lobby music bed) pending. Ambience content, power gating and object and destruction sounds moved to [2026-11_ambience-and-impact-audio](../archive/architecture/2026-11_ambience-and-impact-audio.md). Phase 0 and Phase 5's volume sliders moved to [2026-10_player-settings](2026-10_player-settings.md) Phase 1, which shipped them 2026-10-31.

# Audio foundation (Jul 2026)

Builds the audio domain [audio.md](../design/audio.md) formalizes — the system four other
design docs were already assuming (`area.md` §5 ambience, `main-hud.md` §5 heartbeat,
`stamina.md` §4 breathing, `virology.md` §5 symptom cue) without anyone specifying it.
Navigation map: [systems/audio.md](systems/audio.md) — split out of
[systems/chat-audio-screens.md](systems/chat-audio-screens.md) now that Phase 1 has shipped
(cold-start step 3, [SKILL.md](../SKILL.md)).

## What already exists (upstream legacy)

`Assets/Scripts/SS3D/Systems/Audio/` carries inherited, unwired audio code. This effort keeps the
good bones and replaces the parts that predate the fork's Area/electricity/health/occlusion systems:

| File | Verdict | Why |
|---|---|---|
| `AudioSubSystem.cs` | **Keep + extend** | Working networked SFX/music pool: `[Server] PlayAudioSource` → `[ObserversRpc] RpcPlayAudioSource` → pooled `AudioSource` with Unity 3D falloff (`minDistance`/`maxDistance`), clips via `Assets.Get<AudioClip>(AssetDatabases.Sounds, id)`. Server runs no pool (`#if !UNITY_SERVER`). This is the §3/§5 playback path — do not reinvent it. It has **no wall occlusion** — that is the Phase 1 seam. |
| `Boombox.cs` | **Keep** | Already the §5 diegetic music source: `MachinePowerConsumer`-gated jukebox playing Music through the pool at short range. |
| `NoisyCollision.cs` | **Keep** | A real §3 example (positional collision SFX, server-triggered). Gains occlusion for free once Phase 1 lands. |
| `ChangeMusicInteraction.cs`, `ListenerPosition.cs`, `AudioType.cs` | **Keep** | Music-swap interaction, listener transform holder, `Sfx`/`Music` enum. |
| `AmbienceHandler.cs` | **Replace (Phase 2)** | Legacy per-scene ambience with manual `_air`/`_windiness`/`_power` knobs (all `//TODO`), a global AudioMixer lowpass "muffle", and an animator. Not per-Area, not wired to `AreaSubSystem` or electricity. §2 replaces it with an Area-driven, crossfading, per-track power-gated controller. |

Ad-hoc `AudioSource` users outside the pool — `AirlockStateMachine`, `StructuralIntegrityPresenter`,
`BlastExplosionEffect`, `BikeHorn`, `VendingMachineController`, `FuelPowerGenerator` — are §3 sounds
that today bypass occlusion. Consolidate them onto `AudioSubSystem` as their phases come up (roster
is content, §10 / this doc's Deferred).

## Three governing decisions

1. **Occlusion and ambience are client-local, never server-authoritative.** A wall muffles a sound
   *for a given listener* — it depends on the local `AudioListener` position, so it cannot be baked
   into the server RPC (which only says "play clip X at position P"). The server stays the trigger
   authority; the muffle/attenuate and the ambience crossfade are per-client presentation, exactly
   like [screen-space-effects](../archive/architecture/2026-07_screen-space-effects.md) drives overlays from a synced
   `HealthSnapshot` client-side. The server pool guard (`#if !UNITY_SERVER`) already reflects this.
2. **Occlusion is the seventh consumer of `SS3D.Utils.LineOfSight`** (`audio.md` §1). Reuse
   `LineOfSight.HasLineOfSight(listener, source, occluderMask)` — the same helper Drop, combat cover,
   and comms occlusion use. Do **not** fork a private `Physics.Raycast` in audio (the
   [chat-audio-screens](systems/chat-audio-screens.md) pitfall already records this for comms).
3. **Personal audio (§4) and alerts (§6) mirror the snapshot-mapper pattern.** A client-local mapper
   on the local owner subscribes to a synced state (`HealthSnapshot`, stamina, alert-stack add) and
   drives a non-positional, occlusion-free 2D source. `HealthScreenEffectMapper` is the template.

## Phases

### Phase 0 — Foundation & mixer — **moved to [2026-10_player-settings](2026-10_player-settings.md) Phase 1**

- Establish four exposed mixer groups on `Assets/Content/Systems/Audio/MainMixer.mixer` matching §7's
  categories: **Ambience**, **SFX**, **Personal**, **Music** — each independently volume-adjustable.
  These become the settings surface in Phase 5; nothing here decides levels (§10 balancing).
- Route the `AudioSubSystem` SFX/Music pool prefabs (`SfxAudioSourcePrefab`, `MusicAudioSourcePrefab`)
  to the SFX / Music groups. Ambience and Personal controllers route to their own groups.
- Confirm the sound content path and `AssetDatabases.Sounds` clip lookup is the single addition point
  (aligns with the Addressables migration — do not add a second clip loader).

### Phase 1 — Diegetic SFX occlusion (§3) — highest leverage, do first — **shipped**

Delivers the design's core claim (real geometry occludes sound) and upgrades every existing pooled
SFX at once.

- ✅ `AudioSourceOcclusion` — a client-local component added at runtime to every pooled Sfx/Music
  `AudioSource` (`AudioSubSystem.CreateNewAudioSource`). While a positional source is playing, it
  throttle-samples (every 0.15s) `LineOfSight.HasLineOfSight(listener, source.transform.position,
  LayerMask.GetMask("Default"))` — the same occluder mask/layer convention Drop, combat LOS, and comms
  occlusion already use — and lerps an `AudioLowPassFilter` cutoff (22kHz ↔ 800Hz) plus a volume
  scale (1.0 ↔ 0.5) toward the occluded/clear target, so toggling reads as a muffle, not a pop.
  `AudioOcclusionState` holds the pure lerp math (unit-tested in `AudioOcclusionStateTests`,
  independent of any live scene). `PrepareForPlayback` resets the muffle state before every `Play()`
  so a reused pooled source doesn't inherit its previous clip's occlusion.
- ✅ Bounded raycast cost: sampling is throttled per-source, and sources beyond their own `maxDistance`
  skip the raycast/filter update entirely (Unity's own rolloff already silences them).
- ✅ `systems/audio.md` split out of `chat-audio-screens.md`, stamping the occlusion pitfalls.
- **Not yet done:** consolidating ad-hoc emitters (`AirlockStateMachine` door cycle,
  `BlastExplosionEffect`) onto `PlayAudioSource` so they gain occlusion — `NoisyCollision` already
  routes through the pool and got occlusion for free. Content-roster follow-up, not blocking.

### Phase 2 — Per-area ambience (§2) — **shipped (mechanism; power-gating deferred)**

- ✅ `AmbienceSubSystem` — a new **client-local**, self-bootstrapped controller (same
  `SystemsBootstrap.EnsureSubSystem<T>()` pattern as `ScreenEffectsSubSystem`, not a scene/prefab
  placement). Polls the local player's world position every 0.5s and resolves the current Area via a
  new `AreaSubSystem.TryResolveAreaIdForWorldPosition` (live registry on host, `FloorVisualCache`
  fallback on pure clients — the same two-tier pattern `TryResolveAreaIdForDevice` already uses for
  devices, generalized to a moving world position). On an Area change it crossfades two non-positional
  (`spatialBlend = 0`) `AudioSource`s from the old `AmbienceTrackId` clip to the new one.
- ✅ **Client sync for `AmbienceTrackId`** — this field existed on `AreaRecord` but had no
  broadcast channel; pure clients have no flood-fill registry (per [area](systems/area.md)) so they
  could never read it. Added a `RpcSyncAreaAmbience` (BufferLast) snapshot mirroring the existing
  departmental-tint sync pattern, landing in a new `AreaFloorVisualCache` ambience-id cache
  (`SetAmbienceTrackId`/`TryGetAmbienceTrackId`/`ReplaceAmbienceTrackIds`, unit-tested). A thin
  `AreaSubSystem.SetAreaAmbienceTrackId` server setter authors it (no Map Editor UI yet).
- ✅ `AmbienceHandler` marked superseded in `systems/audio.md` (not deleted — still referenced from
  content until any remaining scene placements are removed, tracked as a follow-up, not blocking).
- **Deferred, not built:** per-track power gating (§2 "honestly power-dependent, not all of them")
  needs a new per-track opt-in data field (mirroring `AreaRecord.HasDepartmentalLightTint`) before
  `AreaLightingState`/`OnAreaLightingStateChanged` can silence a hum on `Dark`. Not added this pass —
  inventing the flag with no authoring surface or content decision on which tracks use it would be
  dead schema; see `systems/audio.md` Pitfalls.
- **Known gap:** `AmbienceTrackId` is **not persisted** (`SavedAreaRecord` doesn't carry it) — resets
  on map reload/restore until a persistence contributor is added. Pre-existing gap in the field the
  design doc calls "already exists," not introduced by this phase, but now more visible since it's
  actually wired end-to-end.
- **Follow-up (content pass):** registered 31 imported-but-previously-unregistered SS3D-Art clips
  into `Sounds.asset` — `StationAmbience1-15`, `SpaceAmbience1-3`, `AirStagnant`,
  `WindHeavy/Light/Violent`, `PowerHum` (candidate `areaambience` track ids), plus 8 more
  `NoisyCollision`-shaped collision variants (registered for availability; no prefab uses
  `NoisyCollision` yet so they're unwired). Added `AreaAmbienceCommand` (`areaambience <trackId>`)
  as the actual dev-console authoring surface `SetAreaAmbienceTrackId` needed — always targets the
  calling player's own current area, same reasoning as `AtmosDebugCommand`.
- ~~**Known gap:** ambience `AudioSource`s output to Master~~ — closed by
  [2026-10_player-settings](2026-10_player-settings.md) Phase 1: sources route to the `Ambience` group
  via `MainAudioMixer` (mixer moved under `Resources/`).

### Phase 3 — Personal audio (§4) — **shipped**

- ✅ `PersonalAudioSubSystem` — a third self-bootstrapped controller (same pattern as
  `AmbienceSubSystem`/`ScreenEffectsSubSystem`), owning two non-positional, occlusion-free
  `AudioSource`s (heartbeat, breathing) driven purely by intensity (`SetHeartbeatIntensity`/
  `SetBreathingIntensity`, 0..1) — it never polls or subscribes to anything itself.
  `AudioTrackIds` names the two fixed clip ids.
- ✅ `HealthPersonalAudioMapper` — heartbeat intensity from `HealthSnapshot`, wired into
  `HumanHealthController`'s existing local-owner hooks (`ApplyScreenEffectsFromSnapshot`/
  `ClearScreenEffectsIfDriving`, alongside `HealthScreenEffectMapper` — same event, second consumer,
  not a new subscription). **Deliberately diverges from the screen-effect mapper**: `IsCardiacArrest`
  silences the heartbeat (heart stopped, nothing to beat) rather than forcing it to full intensity —
  worked example C's "resuming on a successful defib" only makes sense if cardiac arrest silenced it
  first. Unit-tested (`HealthPersonalAudioMapperTests`), including this exact divergence case.
- ✅ `StaminaPersonalAudioMapper` — breathing intensity from stamina ratio (already 0..1 per
  `IStamina.Current`), wired into `StaminaController.SyncCurrentStamina`'s existing `IsOwner` gate
  (a no-op placeholder before this). Unit-tested (`StaminaPersonalAudioMapperTests`).
- ✅ Lifecycle: `PersonalAudioSubSystem` never watches for disconnect itself — the driving consumer
  zeroes intensity on ownership loss (`ClearScreenEffectsIfDriving` already ran on `OnDestroyed`), so
  no separate reset logic was needed the way `AmbienceSubSystem` needed one.
- This is the reusable category virology's symptom cue (§4) later plugs into — the seam is
  `PersonalAudioSubSystem.SetHeartbeatIntensity`/`SetBreathingIntensity`, callable directly by any
  future domain mapper; virology content itself is out of scope here.
- ~~**Known gap (shared with Phase 2):** outputs to Master~~ — closed by
  [2026-10_player-settings](2026-10_player-settings.md) Phase 1: routes to the new `Personal` group.

### Phase 4 — Alert cues (§6) — **shipped (alert stack; PDA deferred)**

- ✅ `PersonalAudioSubSystem.PlayAlertCue()` — a third playback method on the same subsystem
  (alongside heartbeat/breathing), but **one-shot** (`AudioSource.PlayOneShot`, not a loop) on its own
  dedicated non-looping source, cooldown-debounced (1s) so a burst of chips reads as one restrained
  "ding," not a machine-gun.
- ✅ `AlertStackAudioMapper.HasNewAlert(previous, current)` — pure diff over `AlertStackState`
  (`Assets/Scripts/SS3D/UI/MainHud/Components/`), true only when a hazard goes None → any severity.
  An escalation already showing (Warning → Critical) does **not** re-trigger the cue — that's not a
  new alert, it's the same chip getting worse. Unit-tested.
- ✅ `MainHudSubSystem.PushAlertState` — the single funnel every `AlertStackState` push already went
  through (health-driven via `HandleHealthSnapshotChanged`/`RefreshAlerts`, debug override via
  `SetDebugAlertOverride`) now diffs through the mapper before forwarding to the view, so all three
  existing call sites got the cue for free rather than needing three separate hooks.
- **Deferred:** PDA notification chips — no PDA notification chip system exists in code yet to hook
  (same gap `chat-audio-screens.md` already records for the non-diegetic feed / PDA log). The alert
  stack was the only side of §6 actually built to wire into this pass.

### Phase 5 — Music exception & volume settings (§5, §7)

- **Lobby/menu non-diegetic bed** — the one place a 2D score is allowed (`lobby.md` §1's existing meta
  carve-out): plays on lobby/menu scenes only, stops at round start. Enforce "no global in-round score" —
  in-round music is Boombox-sourced only (§5, already true).
- ~~Bind four settings sliders to the Phase 0 mixer groups~~ — moved to
  [2026-10_player-settings](2026-10_player-settings.md) Phase 1. Default values and mix balance stay a
  balancing pass (§10).

## Deferred (content / balancing, per audio.md §10)

- Full sound-effect roster per object/weapon/action (§10) — the per-emitter content pass.
- Exact volume levels, category mix, default slider values (§10).
- Music track roster / licensing (§10).
- Which specific ambience tracks are power-dependent (§2 — authored per-track).
- Any stealth/footstep mechanic — footsteps are ordinary §3 SFX; no stealth system exists to consume them.
- **Voice chat — explicitly out** (`comms.md` §10 territory, `audio.md` §10). Not extended here.

## Companion design edits (owner-only — do not make in this effort)

`audio.md` §9 lists design-doc cross-citations the **owner** should add (`area.md` §5, `main-hud.md` §5,
`stamina.md` §4, `death-cloning-respawn.md` §7, `virology.md` §5 each citing `audio.md`). Agents never edit
`Documents/design/`; noted here so the dependency is tracked, not actioned.
