using SS3D.Utils;
using UnityEngine;

namespace SS3D.Systems.Audio
{
    /// <summary>
    /// Client-local occlusion for a pooled positional <see cref="AudioSource"/> — audio.md §3,
    /// the seventh consumer of the shared <see cref="LineOfSight"/> raycast (a wall blocks a
    /// sound exactly the same way it already blocks a shot, a subtitle bubble, or a wireless
    /// scan). Runs entirely client-side: occlusion depends on the local listener's position, so
    /// it cannot be baked into the server's play RPC. Sound needs air (audio.md §3), so the same
    /// component also thins every source by the air where the listener stands.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AudioSourceOcclusion : MonoBehaviour
    {
        // Must not call LayerMask.GetMask from a field initializer / static ctor — Unity forbids
        // NameToLayer during MonoBehaviour construction (same pitfall as VisionSubSystem).
        private static LayerMask s_occlusionMask;
        private static bool s_occlusionMaskReady;

        private const float SampleInterval = 0.15f;
        private const float LerpSpeed = 6f;

        /// <summary>
        /// Occlusion rays are sampled at this height above the listener/source, not at the feet.
        /// <c>ListenerPosition</c> parks the <see cref="AudioListener"/> on the body root (y≈0), and
        /// plenums/airlock tiles put Default-layer box colliders whose tops sit on that same plane —
        /// a feet-height ray therefore false-positives on the floor for almost every non-zero-length
        /// cast and leaves every pooled SFX permanently muffled (0.5× + 800Hz).
        /// </summary>
        private const float OcclusionSampleHeight = 1.2f;

        private static Transform s_listener;

        /// <summary>How well the listener's air carries sound, 0..1. Driven by <see cref="AmbienceSubSystem"/>.</summary>
        public static float ListenerAir { get; set; } = 1f;

        /// <summary>
        /// Takes the air's muffle but not a wall's. A jukebox reads as permanently muddled under the
        /// occlusion lowpass, because furniture counts as a hit.
        /// </summary>
        public bool IgnoresWalls { get; set; }

        private AudioSource _source;
        private AudioLowPassFilter _lowPass;
        private float _baseVolume = 1f;
        private float _cutoffCurrent = AudioOcclusionState.ClearCutoffHz;
        private float _volumeScaleCurrent = 1f;
        private float _nextSampleTime;
        private bool _isOccluded;

        private static LayerMask OcclusionMask
        {
            get
            {
                if (!s_occlusionMaskReady)
                {
                    s_occlusionMask = LayerMask.GetMask("Default");
                    s_occlusionMaskReady = true;
                }

                return s_occlusionMask;
            }
        }

        /// <summary>Where this client hears from, which is not always where its camera is.</summary>
        public static bool TryGetListenerPosition(out Vector3 position)
        {
            if (s_listener == null)
            {
                AudioListener listener = FindAnyObjectByType<AudioListener>();
                if (listener == null)
                {
                    position = default;
                    return false;
                }

                s_listener = listener.transform;
            }

            position = s_listener.position;
            return true;
        }

        private void Awake()
        {
            CacheComponents();
        }

        /// <summary>Also called from play, because a presenter can start a sound where Awake never ran.</summary>
        private void CacheComponents()
        {
            if (_source != null)
            {
                return;
            }

            _source = GetComponent<AudioSource>();
            _lowPass = GetComponent<AudioLowPassFilter>();
            if (_lowPass == null)
            {
                _lowPass = gameObject.AddComponent<AudioLowPassFilter>();
            }

            _lowPass.cutoffFrequency = AudioOcclusionState.ClearCutoffHz;
            _ = OcclusionMask; // Resolve once Awake is legal, not during AddComponent type init.
        }

        /// <summary>
        /// Called by <see cref="AudioSubSystem"/> right before <see cref="AudioSource.Play"/> so a
        /// reused pooled source doesn't inherit the previous clip's muffle state.
        /// </summary>
        public void PrepareForPlayback(float volume)
        {
            CacheComponents();
            _baseVolume = volume;
            _isOccluded = false;
            _cutoffCurrent = AudioOcclusionState.ClearCutoffHz;
            _volumeScaleCurrent = 1f;
            _nextSampleTime = 0f;
            _lowPass.cutoffFrequency = AudioOcclusionState.ClearCutoffHz;
            _source.volume = volume * HearingNow();
            _lowPass.enabled = false;
        }

        /// <summary>Changes the unoccluded volume of a source that is already playing, as in a fade.</summary>
        public void SetBaseVolume(float volume)
        {
            _baseVolume = volume;
        }

        private float HearingNow()
        {
            return TryGetListenerPosition(out Vector3 listenerPosition)
                ? AudioAirState.Hearing(ListenerAir, Vector3.Distance(listenerPosition, transform.position))
                : ListenerAir;
        }

        private void Update()
        {
            if (!_source.isPlaying)
            {
                return;
            }

            if (!TryGetListenerPosition(out Vector3 listenerPosition))
            {
                return;
            }

            // Unity's own rolloff already silences sources past maxDistance — skip the raycast
            // and filter update entirely rather than sampling occlusion for inaudible sources.
            float distance = Vector3.Distance(listenerPosition, transform.position);
            if (distance > _source.maxDistance)
            {
                return;
            }

            if (Time.time >= _nextSampleTime)
            {
                _nextSampleTime = Time.time + SampleInterval;

                // Own footsteps / point-blank sources: never occlude (ray length ~0 also false-hits
                // nearby Default colliders on the body/tile).
                if (IgnoresWalls || distance < 1.5f)
                {
                    _isOccluded = false;
                }
                else
                {
                    // Planar sample at chest height — walls still intersect; floors/plenums do not.
                    Vector3 origin = listenerPosition;
                    origin.y += OcclusionSampleHeight;
                    Vector3 end = transform.position;
                    end.y = origin.y;

                    _isOccluded = !LineOfSight.HasLineOfSight(origin, end, OcclusionMask, out _);
                }
            }

            float lerpFactor = Time.deltaTime * LerpSpeed;
            _cutoffCurrent = AudioOcclusionState.LerpCutoffHz(_cutoffCurrent, _isOccluded, lerpFactor);
            _volumeScaleCurrent = AudioOcclusionState.LerpVolumeScale(_volumeScaleCurrent, _isOccluded, lerpFactor);

            float hearing = AudioAirState.Hearing(ListenerAir, distance);
            float cutoff = Mathf.Min(_cutoffCurrent, AudioAirState.CutoffHz(hearing));

            // Disable the filter when fully clear — a 22kHz lowpass still colors some clips.
            bool filterNeeded = _isOccluded || cutoff < AudioOcclusionState.ClearCutoffHz - 500f;
            _lowPass.enabled = filterNeeded;
            if (filterNeeded)
            {
                _lowPass.cutoffFrequency = cutoff;
            }

            _source.volume = _baseVolume * _volumeScaleCurrent * hearing;
        }
    }
}
