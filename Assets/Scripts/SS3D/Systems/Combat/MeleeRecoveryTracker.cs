using UnityEngine;

namespace SS3D.Systems.Combat
{
    /// <summary>
    /// Locks follow-up melee swings for the full windup + recovery cycle from swing start.
    /// ReadyProgress01 drives the Main HUD bracket lock-on recharge (design 2A).
    /// </summary>
    public sealed class MeleeRecoveryTracker : MonoBehaviour
    {
        private float _busyUntil;
        private float _busyDuration;

        /// <summary>True from swing Start until windup+recovery elapses.</summary>
        public bool IsBusy => Time.time < _busyUntil;

        /// <summary>Alias for HUD / gates — busy covers windup and post-connect recovery.</summary>
        public bool IsRecovering => IsBusy;

        /// <summary>
        /// 0 at swing-cycle start (brackets fully receded), 1 when ready / idle (full lock-on).
        /// </summary>
        public float ReadyProgress01
        {
            get
            {
                if (_busyDuration <= 0f || Time.time >= _busyUntil)
                {
                    return 1f;
                }

                float remaining = _busyUntil - Time.time;
                float elapsed = _busyDuration - remaining;
                return Mathf.Clamp01(elapsed / _busyDuration);
            }
        }

        /// <summary>
        /// Call from swing <c>Start</c> so rapid clicks cannot cancel/restart windup and skip the lockout.
        /// </summary>
        public void BeginSwingCycle(float windupSeconds, float recoverySeconds)
        {
            float total = Mathf.Max(0f, windupSeconds) + Mathf.Max(0f, recoverySeconds);
            if (total <= 0f)
            {
                return;
            }

            _busyDuration = total;
            _busyUntil = Time.time + total;
        }
    }
}
