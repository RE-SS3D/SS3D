using System.Collections.Generic;
using SS3D.Core;
using SS3D.Systems.Audio;
using SS3D.Systems.Combat;
using SS3D.Systems.Tile;
using UnityEngine;
using AudioType = SS3D.Systems.Audio.AudioType;

namespace SS3D.Systems.StructuralDamage
{
    /// <summary>
    /// Client-side sounds for structural impacts, breaks and blasts. Plays from the messages that
    /// already drive their visuals, through the occluded pool. Added by <see cref="TileSubSystem"/>.
    /// </summary>
    public sealed class StructuralAudioPresenter : MonoBehaviour
    {
        /// <summary>Past this range a blast is the distant rumble, not a quieter boom.</summary>
        public const float BlastFarDistance = 30f;

        private const float ImpactMinRange = 6f;
        private const float ImpactMaxRange = 50f;
        private const float BreakMinRange = 8f;
        private const float BreakMaxRange = 60f;
        private const float BlastNearMinRange = 12f;
        private const float BlastNearMaxRange = 90f;
        private const float BlastFarMinRange = 40f;
        private const float BlastFarMaxRange = 200f;
        private const float BlastFarVolume = 0.7f;
        private const float WoodStrainPitch = 0.65f;

        private readonly StructuralSoundScheduler _scheduler = new();
        private readonly List<StructuralSound> _due = new();

        /// <summary>Whether a listener this far from a blast hears the distant rumble.</summary>
        public static bool IsDistantBlast(float distance) => distance > BlastFarDistance;

        /// <summary>
        /// The clips a cue draws on, or empty for a cue that has none. Living targets keep their
        /// own hit sounds, and a bullet only ricochets off metal.
        /// </summary>
        public static string[] ClipsFor(StructuralImpactCue cue, StructuralMaterial material)
        {
            switch (cue)
            {
                case StructuralImpactCue.Hit:
                    return material switch
                    {
                        StructuralMaterial.Wood => StructuralAudioTrackIds.WoodHit,
                        StructuralMaterial.Glass => StructuralAudioTrackIds.GlassHit,
                        _ => StructuralAudioTrackIds.MetalHit,
                    };
                case StructuralImpactCue.Ricochet:
                    return CombatAudioTrackIds.SurfaceHit;
                case StructuralImpactCue.Strain:
                    return material switch
                    {
                        StructuralMaterial.Wood => StructuralAudioTrackIds.WoodStrain,
                        StructuralMaterial.Glass => StructuralAudioTrackIds.GlassStrain,
                        _ => StructuralAudioTrackIds.MetalStrain,
                    };
                case StructuralImpactCue.Break:
                    return material switch
                    {
                        StructuralMaterial.Wood => StructuralAudioTrackIds.WoodBreak,
                        StructuralMaterial.Glass => StructuralAudioTrackIds.GlassBreak,
                        _ => StructuralAudioTrackIds.MetalBreak,
                    };
                default:
                    return System.Array.Empty<string>();
            }
        }

        public void OnImpact(Vector3 point, StructuralMaterial material, StructuralImpactCue cue)
        {
            switch (cue)
            {
                case StructuralImpactCue.None:
                    return;
                case StructuralImpactCue.Break:
                    _scheduler.OnBreak(point, material, false, Time.time);
                    return;
                case StructuralImpactCue.Strain:
                    Play(ClipsFor(StructuralImpactCue.Hit, material), point, 0.95f, ImpactMinRange, ImpactMaxRange);
                    Play(ClipsFor(cue, material), point, 1f, BreakMinRange, BreakMaxRange,
                        material == StructuralMaterial.Wood ? WoodStrainPitch : 1f);
                    return;
                default:
                    Play(ClipsFor(cue, material), point, 0.95f, ImpactMinRange, ImpactMaxRange);
                    return;
            }
        }

        /// <summary>A steel wall or table came down, with wreckage that lands a moment later.</summary>
        public void OnSteelBreakup(Vector3 position)
        {
            _scheduler.OnBreak(position, StructuralMaterial.Metal, true, Time.time);
        }

        public void OnBlast(Vector3 epicenter, float yield)
        {
            _scheduler.OnBlast(Time.time);

            float volume = Mathf.Clamp01(0.55f + 0.35f * Mathf.Max(0.35f, BlastVfxFalloff.YieldScale(yield)));
            if (AudioSourceOcclusion.TryGetListenerPosition(out Vector3 listener)
                && IsDistantBlast(Vector3.Distance(listener, epicenter)))
            {
                PlayClip(StructuralAudioTrackIds.BlastFar, epicenter, volume * BlastFarVolume,
                    BlastFarMinRange, BlastFarMaxRange, 1f);
                return;
            }

            Play(StructuralAudioTrackIds.BlastNear, epicenter, volume, BlastNearMinRange, BlastNearMaxRange);
        }

        private void Update()
        {
            if (!AudioSourceOcclusion.TryGetListenerPosition(out Vector3 listener))
                listener = transform.position;

            _due.Clear();
            _scheduler.Tick(Time.time, listener, _due);
            foreach (StructuralSound sound in _due)
            {
                string[] clips = sound.Kind == StructuralSoundKind.Thud
                    ? StructuralAudioTrackIds.WreckageThud
                    : ClipsFor(StructuralImpactCue.Break, sound.Material);
                Play(clips, sound.Position, sound.Volume, BreakMinRange, BreakMaxRange);
            }
        }

        private static void Play(string[] clips, Vector3 position, float volume, float minRange, float maxRange,
            float basePitch = 1f)
        {
            if (clips.Length > 0)
                PlayClip(clips[Random.Range(0, clips.Length)], position, volume, minRange, maxRange, basePitch);
        }

        private static void PlayClip(string clipId, Vector3 position, float volume, float minRange, float maxRange,
            float basePitch)
        {
            if (SubSystems.TryGet(out AudioSubSystem audio))
            {
                audio.PlayAudioSourceLocal(AudioType.Sfx, clipId, position, null, false, volume,
                    basePitch * Random.Range(0.92f, 1.08f), minRange, maxRange);
            }
        }
    }
}
