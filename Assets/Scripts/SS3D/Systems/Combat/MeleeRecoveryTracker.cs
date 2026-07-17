using UnityEngine;

namespace SS3D.Systems.Combat
{
    /// <summary>
    /// Blocks follow-up melee swings during the recovery window after a hit lands.
    /// </summary>
    public sealed class MeleeRecoveryTracker : MonoBehaviour
    {
        private float _recoverUntil;

        public bool IsRecovering => Time.time < _recoverUntil;

        public void BeginRecovery(float recoverySeconds)
        {
            if (recoverySeconds <= 0f)
            {
                return;
            }

            _recoverUntil = Time.time + recoverySeconds;
        }
    }
}
