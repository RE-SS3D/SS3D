using System.Collections.Generic;
using SS3D.Systems.Tile;
using UnityEngine;

namespace SS3D.Systems.StructuralDamage
{
    public enum StructuralSoundKind
    {
        Break,
        Thud,
    }

    public readonly struct StructuralSound
    {
        public readonly StructuralSoundKind Kind;
        public readonly Vector3 Position;
        public readonly StructuralMaterial Material;
        public readonly float Volume;

        public StructuralSound(StructuralSoundKind kind, Vector3 position, StructuralMaterial material, float volume)
        {
            Kind = kind;
            Position = position;
            Material = material;
            Volume = volume;
        }
    }

    /// <summary>
    /// Decides when each break is heard. A lone break plays in full, and steel wreckage thuds when
    /// it lands. A blast keeps only the debris nearest the listener, so it stays one event.
    /// Holds no Unity state, so time and the listener are passed in.
    /// </summary>
    public sealed class StructuralSoundScheduler
    {
        /// <summary>
        /// How long a break waits to see whether a blast claims it. The server sends a blast's
        /// breakups before the blast message, so they land just ahead of it. Sending the blast
        /// first would run its scorch raycast against a table that's about to be destroyed.
        /// </summary>
        public const float BreakHoldSeconds = 0.05f;

        /// <summary>How long after a blast message a break can still fall due and join its debris tail.</summary>
        public const float BlastWindowSeconds = 0.1f;

        public const int MaxDebrisVoices = 3;

        /// <summary>Matches the ground-impact dust delay in <see cref="SteelWreckageView"/>.</summary>
        public const float WreckageLandSeconds = 0.32f;

        private const float BreakVolume = 1f;
        private const float ThudVolume = 0.8f;
        private const float DebrisVolume = 0.55f;
        private const float DebrisVolumeStep = 0.15f;
        private const float DebrisFirstDelaySeconds = 0.15f;
        private const float DebrisStaggerSeconds = 0.16f;

        private readonly List<(StructuralSound Sound, float PlayAt)> _queued = new();
        private readonly List<(Vector3 Position, StructuralMaterial Material, bool HasWreckage, float DueAt)> _held = new();
        private readonly List<(Vector3 Position, StructuralMaterial Material)> _debris = new();
        private float _blastWindowStart = float.NegativeInfinity;
        private float _blastWindowEnd = float.NegativeInfinity;

        public void OnBlast(float now)
        {
            if (now > _blastWindowEnd)
                _blastWindowStart = now;

            _blastWindowEnd = now + BlastWindowSeconds;
        }

        public void OnBreak(Vector3 position, StructuralMaterial material, bool hasWreckage, float now)
        {
            _held.Add((position, material, hasWreckage, now + BreakHoldSeconds));
        }

        /// <summary>Appends every sound that's due at <paramref name="now"/> to <paramref name="due"/>.</summary>
        public void Tick(float now, Vector3 listener, List<StructuralSound> due)
        {
            for (int i = _held.Count - 1; i >= 0; i--)
            {
                var held = _held[i];
                if (held.DueAt > now)
                    continue;

                _held.RemoveAt(i);
                // Judged on the break's own time, so a hitch after a blast can't turn its debris into full breaks.
                if (held.DueAt >= _blastWindowStart && held.DueAt <= _blastWindowEnd)
                {
                    _debris.Add((held.Position, held.Material));
                    continue;
                }

                _queued.Add((new StructuralSound(StructuralSoundKind.Break, held.Position, held.Material, BreakVolume), now));
                if (held.HasWreckage)
                {
                    _queued.Add((new StructuralSound(StructuralSoundKind.Thud, held.Position, held.Material, ThudVolume),
                        now + WreckageLandSeconds));
                }
            }

            if (_debris.Count > 0 && now > _blastWindowEnd)
                QueueDebrisTail(now, listener);

            for (int i = _queued.Count - 1; i >= 0; i--)
            {
                if (_queued[i].PlayAt > now)
                    continue;

                due.Add(_queued[i].Sound);
                _queued.RemoveAt(i);
            }
        }

        private void QueueDebrisTail(float now, Vector3 listener)
        {
            _debris.Sort((a, b) => (a.Position - listener).sqrMagnitude.CompareTo((b.Position - listener).sqrMagnitude));

            int voices = Mathf.Min(_debris.Count, MaxDebrisVoices);
            for (int i = 0; i < voices; i++)
            {
                var sound = new StructuralSound(StructuralSoundKind.Break, _debris[i].Position, _debris[i].Material,
                    DebrisVolume - DebrisVolumeStep * i);
                _queued.Add((sound, now + DebrisFirstDelaySeconds + DebrisStaggerSeconds * i));
            }

            _debris.Clear();
        }
    }
}
