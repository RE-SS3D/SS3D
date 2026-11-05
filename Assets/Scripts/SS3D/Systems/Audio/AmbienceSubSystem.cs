using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Area;
using SS3D.Systems.Entities.Events;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Entities.Humanoid.Body;
using UnityEngine;

namespace SS3D.Systems.Audio
{
    /// <summary>
    /// Client-local per-Area ambience crossfade (audio.md §2). Tracks the local player's current
    /// Area — the same one-tile lookup every Area-driven system uses
    /// (<see cref="AreaSubSystem.TryResolveAreaIdForWorldPosition"/>) — and crossfades between two
    /// non-positional 2D sources whenever the area's <see cref="AmbienceProfile"/> changes. Inside an
    /// area with no profile of its own that's the generic one; outside any area it's silence.
    /// Everything here follows the air where the listener stands (<see cref="ListenerAirTracker"/>):
    /// wind rises with the airflow, ambience thins with the pressure, and that same level reaches
    /// positional sounds through <see cref="AudioSourceOcclusion.ListenerAir"/>. Self-bootstrapped by <c>SystemsBootstrap</c> (process-wide DDOL), same as
    /// <c>ScreenEffectsSubSystem</c>.
    /// </summary>
    public sealed class AmbienceSubSystem : SubSystem
    {
        private const float PollInterval = 0.5f;
        private const float CrossfadeVolumePerSecond = 0.4f;
        private const float ClearFilterHz = AudioOcclusionState.ClearCutoffHz - 500f;

        /// <summary>Air thin enough that breathing is what's left to hear.</summary>
        private const float BreathingAir = 0.2f;

        /// <summary>Air thin enough to count as space, for the space bed.</summary>
        private const float SpaceBedAir = 0.05f;

        private readonly AmbienceScheduler _scheduler = new(new System.Random());
        private readonly ListenerAirTracker _air = new();
        private readonly AudioSource[] _windSources = new AudioSource[AudioAirState.WindLayerCount];

        private AudioSource _activeSource;
        private AudioSource _inactiveSource;
        private AudioSource _airSource;
        private AudioSource _occasionalSource;
        private AudioSource _spaceSource;
        private AudioLowPassFilter[] _airFilters;

        private GameObject _listenerTarget;
        private HumanoidBodyStateMachine _livingBody;
        private AmbienceProfile _currentProfile;
        private bool _inArea;
        private bool _equipmentPowered = true;
        private bool _nearStation = true;
        private float _occasionalVolume;
        private float _nextPollTime;

        /// <summary>Wind where the listener stands, in m/s, from this client's own atlas.</summary>
        public float ListenerWindSpeed => _air.WindSpeed;

        protected override void OnAwake()
        {
            base.OnAwake();

            _activeSource = BuildAmbienceSource("AmbienceSourceA");
            _inactiveSource = BuildAmbienceSource("AmbienceSourceB");
            _airSource = BuildAmbienceSource("AmbienceAirBed");
            _occasionalSource = BuildAmbienceSource("AmbienceOccasional");
            _occasionalSource.loop = false;
            _spaceSource = BuildAmbienceSource("AmbienceSpaceBed");
            for (int i = 0; i < _windSources.Length; i++)
            {
                _windSources[i] = BuildAmbienceSource($"AmbienceWind{i}");
            }

            _airFilters = Transform.GetComponentsInChildren<AudioLowPassFilter>(true);

            AddHandle(LocalPlayerObjectChanged.AddListener(HandlePlayerObjectChanged));
            AddHandle(UpdateEvent.AddListener(HandleUpdate));
        }

        private AudioSource BuildAmbienceSource(string name)
        {
            GameObject host = new(name);
            host.transform.SetParent(Transform, false);

            AudioSource source = host.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f; // Non-positional — audio.md §2, unlike diegetic SFX (§3).
            source.volume = 0f;
            source.outputAudioMixerGroup = MainAudioMixer.FindGroup(MainAudioMixer.AmbienceGroup);
            host.AddComponent<AudioLowPassFilter>().enabled = false;
            return source;
        }

        private void HandlePlayerObjectChanged(ref EventContext context, in LocalPlayerObjectChanged e)
        {
            _listenerTarget = e.PlayerHasObject ? e.PlayerObject : null;

            // A ghost floats everywhere and breathes nothing, so it hears whatever air its tile holds.
            _livingBody = _listenerTarget != null && _listenerTarget.TryGetComponent(out HumanoidLivingController _)
                ? _listenerTarget.GetComponent<HumanoidBodyStateMachine>()
                : null;

            if (_listenerTarget == null)
            {
                // No body to be "in an area" — fade to silence. Mirrors HealthScreenEffectMapper.Clear
                // on HumanHealthController's own destroy/ownership loss: the driving consumer clears
                // the shared presentation state it was pushing.
                _inArea = false;
                BeginCrossfade(null);
                _air.Reset();
            }

            DriveBreathing();
        }

        private void HandleUpdate(ref EventContext context, in UpdateEvent updateEvent)
        {
            if (_listenerTarget != null)
            {
                _air.Follow(_listenerTarget.transform.position, InOpenSpace, Time.deltaTime);
            }

            if (_listenerTarget != null && Time.time >= _nextPollTime)
            {
                _nextPollTime = Time.time + PollInterval;
                PollCurrentArea();
                _nearStation = !InOpenSpace || ListenerAirTracker.IsNearStation(_listenerTarget.transform.position);
                DriveBreathing();
            }

            AudioSourceOcclusion.ListenerAir = _air.Air;

            DriveCrossfade();
            DriveAirBed();
            DriveWind();
            DriveSpaceBed();
            DriveOccasionalSounds();
            DriveAirFilters();
        }

        /// <summary>The server's word that this body has no floor under it, which pure clients can't tell.</summary>
        private bool InOpenSpace => _livingBody != null && _livingBody.Snapshot.IsFloating;

        /// <summary>
        /// Resolves every poll rather than on area change, so a track authored while the player
        /// stands in the room applies without them leaving it.
        /// </summary>
        private void PollCurrentArea()
        {
            if (!SubSystems.TryGet(out AreaSubSystem areaSubSystem))
            {
                return;
            }

            if (!areaSubSystem.TryResolveAreaIdForWorldPosition(_listenerTarget.transform.position, out AreaId areaId))
            {
                _inArea = false;
                BeginCrossfade(null);
                return;
            }

            _inArea = true;
            _equipmentPowered = areaSubSystem.IsAreaEquipmentPowered(areaId);
            areaSubSystem.TryGetAmbienceProfileId(areaId, out string profileId);
            BeginCrossfade(AmbienceProfileCatalog.Instance.Resolve(profileId));
        }

        private void BeginCrossfade(AmbienceProfile profile)
        {
            if (profile == _currentProfile)
            {
                return;
            }

            _currentProfile = profile;

            // The previously active source fades out into silence and becomes the next
            // inactive slot; the previously inactive source takes the new clip and fades in.
            AudioSource incoming = _inactiveSource;
            AudioSource outgoing = _activeSource;

            if (profile != null && profile.Bed != null)
            {
                incoming.clip = profile.Bed;
                incoming.time = 0f;
                incoming.Play();
            }
            else
            {
                incoming.Stop();
                incoming.clip = null;
            }

            _activeSource = incoming;
            _inactiveSource = outgoing;
        }

        private void DriveCrossfade()
        {
            float step = CrossfadeVolumePerSecond * Time.deltaTime;
            bool bedRunning = _currentProfile != null && (_equipmentPowered || !_currentProfile.PowerGated);
            float activeTarget = bedRunning ? _currentProfile.BedVolume * _air.Air : 0f;

            _activeSource.volume = Mathf.MoveTowards(_activeSource.volume, activeTarget, step);
            _inactiveSource.volume = Mathf.MoveTowards(_inactiveSource.volume, 0f, step);

            if (_inactiveSource.volume <= 0f && _inactiveSource.isPlaying)
            {
                _inactiveSource.Stop();
            }
        }

        /// <summary>Plays wherever there's air (audio.md §2), so it needs no area.</summary>
        private void DriveAirBed()
        {
            AmbienceProfileCatalog catalog = AmbienceProfileCatalog.Instance;
            float target = _listenerTarget != null ? catalog.AirBedVolume * _air.Air : 0f;
            _airSource.volume = Mathf.MoveTowards(_airSource.volume, target, CrossfadeVolumePerSecond * Time.deltaTime);
            SyncLoop(_airSource, catalog.AirBed);
        }

        /// <summary>Wind plays at the listener, as strong as the airflow where they stand (audio.md §1).</summary>
        private void DriveWind()
        {
            AmbienceProfileCatalog catalog = AmbienceProfileCatalog.Instance;
            int layers = Mathf.Min(_windSources.Length, catalog.WindLayers.Length);
            for (int i = 0; i < layers; i++)
            {
                _windSources[i].volume = AudioAirState.WindGain(i, _air.WindSpeed) * catalog.WindVolume * _air.Air;
                SyncLoop(_windSources[i], catalog.WindLayers[i]);
            }
        }

        /// <summary>Near-silence by the hull; a very faint bed only well away from it (audio.md §2).</summary>
        private void DriveSpaceBed()
        {
            AmbienceProfileCatalog catalog = AmbienceProfileCatalog.Instance;
            bool adrift = InOpenSpace && !_inArea && !_nearStation && _air.Air <= SpaceBedAir;
            _spaceSource.volume = Mathf.MoveTowards(_spaceSource.volume, adrift ? catalog.SpaceBedVolume : 0f,
                CrossfadeVolumePerSecond * Time.deltaTime);
            SyncLoop(_spaceSource, catalog.SpaceBed);
        }

        /// <summary>Your own breathing is what's left when the air has gone (audio.md §8 E).</summary>
        private void DriveBreathing()
        {
            if (SubSystems.TryGet(out PersonalAudioSubSystem personalAudio))
            {
                bool breathing = _livingBody != null && _air.Air < BreathingAir;
                personalAudio.SetCalmBreathingIntensity(breathing ? 1f - _air.Air / BreathingAir : 0f);
            }
        }

        private void DriveAirFilters()
        {
            float cutoff = AudioAirState.CutoffHz(_air.Air);
            bool filtering = cutoff < ClearFilterHz;
            foreach (AudioLowPassFilter filter in _airFilters)
            {
                filter.enabled = filtering;
                filter.cutoffFrequency = cutoff;
            }
        }

        private static void SyncLoop(AudioSource source, AudioClip clip)
        {
            if (source.volume > 0f && !source.isPlaying)
            {
                source.clip = clip;
                source.Play();
            }
            else if (source.volume <= 0f && source.isPlaying)
            {
                source.Stop();
            }
        }

        /// <summary>One clock across rooms, so a walk through short rooms can't keep putting a sound off.</summary>
        private void DriveOccasionalSounds()
        {
            _occasionalSource.volume = _occasionalVolume * _air.Air;

            if (_currentProfile == null || _occasionalSource.isPlaying
                || !_scheduler.TryPick(Time.time, _currentProfile.Occasional.Length, out int index))
            {
                return;
            }

            AudioClip clip = _currentProfile.Occasional[index];
            _occasionalSource.clip = clip;
            _occasionalVolume = _currentProfile.OccasionalVolume;
            _occasionalSource.volume = _occasionalVolume * _air.Air;
            _occasionalSource.Play();
            _scheduler.ScheduleAfter(Time.time + clip.length);
        }
    }
}
