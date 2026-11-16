using System.Collections.Generic;

namespace SS3D.Systems.BugReports
{
    /// <summary>
    /// One report per player per minute (<c>sandbox-server.md</c> §7), shared by <c>/bug &lt;note&gt;</c> and the
    /// dialog. Keyed by ckey so reconnecting doesn't reset it. Time is passed in so tests can drive it.
    /// </summary>
    public sealed class BugReportRateLimiter
    {
        public const float IntervalSeconds = 60f;

        private readonly Dictionary<string, float> _lastAccepted = new();
        private readonly float _interval;

        public BugReportRateLimiter(float intervalSeconds = IntervalSeconds)
        {
            _interval = intervalSeconds;
        }

        /// <summary>Seconds until <paramref name="ckey"/> may file again; 0 when they may now.</summary>
        public float SecondsUntilAllowed(string ckey, float now)
        {
            if (string.IsNullOrEmpty(ckey) || !_lastAccepted.TryGetValue(ckey, out float last))
                return 0f;
            float left = last + _interval - now;
            return left > 0f ? left : 0f;
        }

        /// <summary>Records a report when allowed; otherwise returns false and leaves the clock alone.</summary>
        public bool TryConsume(string ckey, float now, out float retryAfter)
        {
            retryAfter = SecondsUntilAllowed(ckey, now);
            if (retryAfter > 0f)
                return false;
            if (!string.IsNullOrEmpty(ckey))
                _lastAccepted[ckey] = now;
            return true;
        }
    }
}
