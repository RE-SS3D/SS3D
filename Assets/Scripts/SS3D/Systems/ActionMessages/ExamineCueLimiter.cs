using System.Collections.Generic;

namespace SS3D.Systems.ActionMessages
{
    /// <summary>Server: at most one examine cue per examiner and target per cooldown (Decision 12).</summary>
    public sealed class ExamineCueLimiter
    {
        private readonly Dictionary<(int Examiner, int Target), float> _last = new();
        private readonly float _cooldownSeconds;

        public ExamineCueLimiter(float cooldownSeconds = ActionMessageConfig.CueCooldownSeconds)
        {
            _cooldownSeconds = cooldownSeconds;
        }

        public bool TryAllow(int examiner, int target, float now)
        {
            (int, int) pair = (examiner, target);
            if (_last.TryGetValue(pair, out float last) && now - last < _cooldownSeconds)
            {
                return false;
            }

            _last[pair] = now;
            return true;
        }
    }
}
