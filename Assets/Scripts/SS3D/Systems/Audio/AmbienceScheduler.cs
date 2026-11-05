using System;

namespace SS3D.Systems.Audio
{
    /// <summary>
    /// When a room's occasional sound plays and which one (audio.md §2): long uneven gaps, and never
    /// the same clip twice in a row, so a room doesn't settle into an obvious loop.
    /// </summary>
    public sealed class AmbienceScheduler
    {
        public const float MinGap = 25f;
        public const float MaxGap = 70f;

        private readonly Random _random;
        private float _nextTime = -1f;
        private int _lastIndex = -1;

        public AmbienceScheduler(Random random)
        {
            _random = random;
        }

        /// <summary>Whether a sound is due. The caller plays it, then calls <see cref="ScheduleAfter"/>.</summary>
        public bool TryPick(float now, int clipCount, out int index)
        {
            index = -1;
            if (clipCount <= 0)
            {
                return false;
            }

            if (_nextTime < 0f)
            {
                ScheduleAfter(now);
            }

            if (now < _nextTime)
            {
                return false;
            }

            index = _random.Next(clipCount);
            if (index == _lastIndex)
            {
                index = (index + 1) % clipCount;
            }

            _lastIndex = index;
            return true;
        }

        /// <summary>Starts the next gap at <paramref name="soundEnd"/>.</summary>
        public void ScheduleAfter(float soundEnd)
        {
            _nextTime = soundEnd + MinGap + (float)_random.NextDouble() * (MaxGap - MinGap);
        }
    }
}
