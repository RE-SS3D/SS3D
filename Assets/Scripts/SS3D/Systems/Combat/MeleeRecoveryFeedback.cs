using System;

namespace SS3D.Systems.Combat
{
    /// <summary>
    /// Client-local signal when the owning player's melee recovery window starts
    /// (connect frame, hit or miss). Main HUD drives bracket recharge from the hand tracker;
    /// this event is available for other listeners that do not hold a Hand reference.
    /// </summary>
    public static class MeleeRecoveryFeedback
    {
        public static event Action<float> LocalRecoveryStarted;

        public static void NotifyLocalRecoveryStarted(float recoverySeconds)
        {
            LocalRecoveryStarted?.Invoke(recoverySeconds);
        }
    }
}
