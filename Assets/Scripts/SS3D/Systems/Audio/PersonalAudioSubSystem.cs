using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using SS3D.Core.Behaviours;
using SS3D.Data;
using SS3D.Data.Generated;
using SS3D.Systems.Entities.Events;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SS3D.Systems.Audio
{
    /// <summary>
    /// Personal, internal audio cues (audio.md §4) — heartbeat, breathing, and future systemic cues
    /// (virology's symptom sound is the same category, per §4). Non-positional, no occlusion, heard
    /// only by whichever local owner is driving it — the diegetic-but-personal exception §1 carves
    /// out from ordinary positional SFX (§3). Self-bootstrapped by <c>SystemsBootstrap</c>
    /// (process-wide DDOL), same pattern as <c>ScreenEffectsSubSystem</c> / <c>AmbienceSubSystem</c>.
    /// Domain mappers (<c>HealthPersonalAudioMapper</c>, <c>StaminaPersonalAudioMapper</c>) push
    /// intensities in; this subsystem only owns playback. Breathing takes the max of stamina and
    /// health contributors so neither overwrites the other.
    /// </summary>
    public sealed class PersonalAudioSubSystem : SubSystem
    {
        private const float IntensityLerpPerSecond = 4f;
        private const float AlertCueVolumeScale = 0.8f;
        private const float CalmBreathingVolume = 0.35f;

        /// <summary>Minimum gap between alert cues — a burst of chips appearing together (or in
        /// quick succession) still reads as one restrained "ding," not a machine-gun (audio.md §6).</summary>
        private const float AlertCueCooldownSeconds = 1f;

        private AudioSource _heartbeatSource;
        private AudioSource _breathingSource;
        private AudioSource _internalsSource;
        private AudioSource _calmBreathingSource;
        private AudioSource _alertCueSource;

        private float _heartbeatTarget;
        private float _staminaBreathingTarget;
        private float _healthBreathingTarget;
        private float _breathingTarget;
        private float _internalsTarget;
        private float _calmBreathingTarget;
        private float _nextAlertCueTime;
        private GameObject _localPlayerObject;

        protected override void OnAwake()
        {
            base.OnAwake();

            _heartbeatSource = BuildPersonalSource("HeartbeatSource");
            _breathingSource = BuildPersonalSource("BreathingSource");
            _internalsSource = BuildPersonalSource("InternalsSource");
            _calmBreathingSource = BuildPersonalSource("CalmBreathingSource");
            _alertCueSource = BuildPersonalSource("AlertCueSource", loop: false);

            AddHandle(UpdateEvent.AddListener(HandleUpdate));
            AddHandle(LocalPlayerObjectChanged.AddListener(HandleLocalPlayerObjectChanged));
            SceneManager.sceneUnloaded += HandleSceneUnloaded;
        }

        protected override void OnDestroyed()
        {
            SceneManager.sceneUnloaded -= HandleSceneUnloaded;
            base.OnDestroyed();
        }

        /// <summary>
        /// The domain mappers only push on change, so a body that goes away (despawn, ghosting,
        /// leaving the round) never pushes its last zero. Whoever drives the next body re-pushes
        /// its own intensities, so dropping the old ones here is safe.
        /// </summary>
        private void HandleLocalPlayerObjectChanged(ref EventContext context, in LocalPlayerObjectChanged e)
        {
            GameObject next = e.PlayerHasObject ? e.PlayerObject : null;
            if (next == _localPlayerObject)
            {
                return;
            }

            _localPlayerObject = next;
            ClearTargets();
        }

        /// <summary>
        /// Leaving to the main menu unloads Game. This subsystem lives across scenes, so cut every
        /// loop dead there rather than letting it fade (or never fade) under the launcher.
        /// </summary>
        private void HandleSceneUnloaded(Scene scene)
        {
            if (scene.name != Scenes.Game)
            {
                return;
            }

            _localPlayerObject = null;
            SilenceAll();
        }

        /// <summary>Zeroes every loop target; the sources fade out and stop on their own.</summary>
        public void ClearTargets()
        {
            _heartbeatTarget = 0f;
            _staminaBreathingTarget = 0f;
            _healthBreathingTarget = 0f;
            _breathingTarget = 0f;
            _internalsTarget = 0f;
            _calmBreathingTarget = 0f;
        }

        /// <summary>Zeroes every loop target and stops every source immediately.</summary>
        public void SilenceAll()
        {
            ClearTargets();
            AudioSource[] sources = { _heartbeatSource, _breathingSource, _internalsSource, _calmBreathingSource, _alertCueSource };
            foreach (AudioSource source in sources)
            {
                if (source == null)
                {
                    continue;
                }

                source.Stop();
                if (source.loop)
                {
                    source.volume = 0f;
                }
            }
        }

        private AudioSource BuildPersonalSource(string name, bool loop = true)
        {
            GameObject host = new(name);
            host.transform.SetParent(Transform, false);

            AudioSource source = host.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f; // Non-positional, no occlusion — audio.md §4.
            source.volume = loop ? 0f : 1f; // One-shot cue source keeps a fixed base volume.
            source.outputAudioMixerGroup = MainAudioMixer.FindGroup(MainAudioMixer.PersonalGroup);
            return source;
        }

        /// <summary>
        /// Sets the target heartbeat intensity (0 silent .. 1 full volume). The clip already loops a
        /// single beat/pair-of-beats cycle; intensity only scales volume, not tempo.
        /// </summary>
        public void SetHeartbeatIntensity(float intensity)
        {
            _heartbeatTarget = Mathf.Clamp01(intensity);
            EnsurePlayingIfAudible(_heartbeatSource, AudioTrackIds.Heartbeat, _heartbeatTarget);
        }

        /// <summary>
        /// Stamina-driven breathing contribution (0 silent/resting .. 1 fully exhausted).
        /// Combined with health via max — see <see cref="SetHealthBreathingIntensity"/>.
        /// </summary>
        public void SetStaminaBreathingIntensity(float intensity)
        {
            _staminaBreathingTarget = Mathf.Clamp01(intensity);
            RefreshBreathingTarget();
        }

        /// <summary>
        /// Health-driven labored breathing (oxy debt / critical). Combined with stamina via max.
        /// </summary>
        public void SetHealthBreathingIntensity(float intensity)
        {
            _healthBreathingTarget = Mathf.Clamp01(intensity);
            RefreshBreathingTarget();
        }

        /// <summary>
        /// Clears health breathing on ownership loss without touching stamina contribution.
        /// </summary>
        public void ClearHealthBreathing()
        {
            SetHealthBreathingIntensity(0f);
        }

        /// <summary>
        /// Calm breathing through the mask while on internals, alongside any labored breathing.
        /// </summary>
        public void SetInternalsBreathingIntensity(float intensity)
        {
            _internalsTarget = Mathf.Clamp01(intensity);
            EnsurePlayingIfAudible(_internalsSource, AudioTrackIds.Breathing, _internalsTarget);
        }

        /// <summary>
        /// Calm breathing where no air carries anything else (audio.md §2, "Outside the station").
        /// Heavy breathing takes over from it as stamina or health drive that one up, and so do internals.
        /// </summary>
        public void SetCalmBreathingIntensity(float intensity)
        {
            _calmBreathingTarget = Mathf.Clamp01(intensity);
            EnsurePlayingIfAudible(_calmBreathingSource, AudioTrackIds.Breathing, _calmBreathingTarget);
        }

        private void RefreshBreathingTarget()
        {
            _breathingTarget = Mathf.Max(_staminaBreathingTarget, _healthBreathingTarget);
            EnsurePlayingIfAudible(_breathingSource, AudioTrackIds.HeavyBreathing, _breathingTarget);
        }

        /// <summary>
        /// One-shot cue for a newly-appearing alert-stack chip or PDA notification (audio.md §6) —
        /// not a loop, cooldown-debounced so a burst of chips reads as one restrained "ding."
        /// </summary>
        public void PlayAlertCue()
        {
            if (Time.time < _nextAlertCueTime)
            {
                return;
            }

            if (!Assets.TryGet(AssetDatabases.Sounds, AudioTrackIds.AlertCue, out AudioClip clip))
            {
                return;
            }

            _nextAlertCueTime = Time.time + AlertCueCooldownSeconds;
            _alertCueSource.PlayOneShot(clip, AlertCueVolumeScale);
        }

        private static void EnsurePlayingIfAudible(AudioSource source, string trackId, float targetIntensity)
        {
            if (targetIntensity <= 0f || source.isPlaying)
            {
                return;
            }

            if (Assets.TryGet(AssetDatabases.Sounds, trackId, out AudioClip clip))
            {
                source.clip = clip;
                source.time = 0f;
                source.Play();
            }
        }

        private void HandleUpdate(ref EventContext context, in UpdateEvent updateEvent)
        {
            float step = IntensityLerpPerSecond * Time.deltaTime;

            _heartbeatSource.volume = Mathf.MoveTowards(_heartbeatSource.volume, _heartbeatTarget, step);
            _breathingSource.volume = Mathf.MoveTowards(_breathingSource.volume, _breathingTarget, step);
            _internalsSource.volume = Mathf.MoveTowards(_internalsSource.volume, _internalsTarget, step);

            float calmTarget = CalmBreathingVolume * Mathf.Max(_calmBreathingTarget - Mathf.Max(_breathingTarget, _internalsTarget), 0f);
            _calmBreathingSource.volume = Mathf.MoveTowards(_calmBreathingSource.volume, calmTarget, step);

            StopIfSilent(_heartbeatSource, _heartbeatTarget);
            StopIfSilent(_breathingSource, _breathingTarget);
            StopIfSilent(_internalsSource, _internalsTarget);
            StopIfSilent(_calmBreathingSource, _calmBreathingTarget);
        }

        private static void StopIfSilent(AudioSource source, float target)
        {
            if (target <= 0f && source.volume <= 0f && source.isPlaying)
            {
                source.Stop();
            }
        }
    }
}
