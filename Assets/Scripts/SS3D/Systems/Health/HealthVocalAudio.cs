using SS3D.Core;
using SS3D.Systems.Audio;
using UnityEngine;
using AudioType = SS3D.Systems.Audio.AudioType;

namespace SS3D.Systems.Health
{
    /// <summary>
    /// Server-side positional body sounds for one <see cref="HumanHealthController"/> (audio.md §3): flesh hits, the pain
    /// scream, the gasp/choke on entering Critical or vacuum, and the slow groans while Downed and gasps while Critical
    /// (health.md §7). Played through <see cref="AudioSubSystem"/>, which fans out to observers.
    /// </summary>
    public sealed class HealthVocalAudio
    {
        /// <summary>Downed groans and Critical gasps repeat at a random interval in this range.</summary>
        public const float AmbientVocalMinSeconds = 6f;
        public const float AmbientVocalMaxSeconds = 12f;

        /// <summary>Stand-in groan: the gasp clip pitched down until real groan clips are sourced.</summary>
        public const float GroanPitch = 0.72f;

        private HealthState _previousState = HealthState.Healthy;
        private bool _previousVacuum;
        private float _nextScreamTime;
        private float _nextAmbientVocalTime;

        /// <summary>Which ambient vocal a body in this state makes, if any.</summary>
        public static AmbientVocal AmbientVocalFor(HealthSnapshot snapshot)
        {
            if (!snapshot.IsConscious || snapshot.IsCardiacArrest)
            {
                return AmbientVocal.None;
            }

            return snapshot.State switch
            {
                HealthState.Downed => AmbientVocal.Groan,
                HealthState.Critical => AmbientVocal.Gasp,
                _ => AmbientVocal.None,
            };
        }

        public void PlayHitImpact(Vector3 position, float intensity)
        {
            AudioSubSystem audio = SubSystems.GetOrNull<AudioSubSystem>();
            if (audio == null)
            {
                return;
            }

            string[] flesh = Combat.CombatAudioTrackIds.FleshHit;
            string fleshId = flesh[Random.Range(0, flesh.Length)];
            float pitch = Random.Range(0.92f, 1.08f);
            audio.PlayAudioSource(AudioType.Sfx, fleshId, position, null, false, 0.75f, pitch);

            string bloodId = intensity >= 0.65f ? HealthAudioTrackIds.Splat : HealthAudioTrackIds.Blood1;
            audio.PlayAudioSource(AudioType.Sfx, bloodId, position, null, false, 0.55f + (0.35f * intensity), pitch);
        }

        /// <summary>A hard hit makes a conscious, animated body scream; the cooldown keeps it from repeating.</summary>
        public void TryPainScream(Vector3 position, float brute, bool mayVocalize)
        {
            if (!mayVocalize || brute < HealthConstants.ScreamMinBrute || Time.time < _nextScreamTime)
            {
                return;
            }

            _nextScreamTime = Time.time + HealthConstants.ScreamCooldownSeconds;
            // A scream also resets the ambient timer, so a groan doesn't follow straight after.
            _nextAmbientVocalTime = Time.time + AmbientVocalMinSeconds;
            Play(position, HealthAudioTrackIds.MaleScream, 0.85f, Random.Range(0.95f, 1.05f));
        }

        /// <summary>Called on every published snapshot: the gasp/choke edges into Critical and vacuum.</summary>
        public void OnSnapshot(Vector3 position, HealthSnapshot snapshot)
        {
            bool vacuum = snapshot.Environment.IsVacuum && !snapshot.Environment.OnInternals;
            bool enteredCritical = snapshot.State == HealthState.Critical
                && _previousState != HealthState.Critical
                && snapshot.IsConscious
                && !snapshot.IsCardiacArrest;
            bool enteredVacuum = vacuum && !_previousVacuum;

            if (enteredVacuum)
            {
                string[] choke = HealthAudioTrackIds.Choke;
                Play(position, choke[Random.Range(0, choke.Length)], 0.8f, Random.Range(0.95f, 1.05f));
            }
            else if (enteredCritical)
            {
                Play(position, HealthAudioTrackIds.MaleGasp, 0.8f, Random.Range(0.95f, 1.05f));
            }

            if (snapshot.State != _previousState)
            {
                _nextAmbientVocalTime = Time.time + Random.Range(AmbientVocalMinSeconds * 0.5f, AmbientVocalMinSeconds);
            }

            _previousState = snapshot.State;
            _previousVacuum = vacuum;
        }

        /// <summary>1 Hz: a Downed body groans and a Critical one gasps on a slow random timer.</summary>
        public void Tick(Vector3 position, HealthSnapshot snapshot)
        {
            AmbientVocal vocal = AmbientVocalFor(snapshot);
            if (vocal == AmbientVocal.None || Time.time < _nextAmbientVocalTime)
            {
                return;
            }

            _nextAmbientVocalTime = Time.time + Random.Range(AmbientVocalMinSeconds, AmbientVocalMaxSeconds);
            if (vocal == AmbientVocal.Groan)
            {
                Play(position, HealthAudioTrackIds.MaleGasp, 0.55f, GroanPitch * Random.Range(0.95f, 1.05f));
            }
            else
            {
                string[] choke = HealthAudioTrackIds.Choke;
                Play(position, choke[Random.Range(0, choke.Length)], 0.6f, Random.Range(0.9f, 1.0f));
            }
        }

        private static void Play(Vector3 position, string clipId, float volume, float pitch)
        {
            SubSystems.GetOrNull<AudioSubSystem>()?.PlayAudioSource(AudioType.Sfx, clipId, position, null, false, volume, pitch);
        }

        public enum AmbientVocal
        {
            None,
            Groan,
            Gasp,
        }
    }
}
