using System;

namespace SS3D.Systems.Combat
{
    /// <summary>
    /// Client-local signal when a melee swing's connect frame applied damage.
    /// Used by Main HUD for the design-2A cross flash (whiffs stay silent).
    /// </summary>
    public static class MeleeConnectFeedback
    {
        public static event Action LocalConnectHitLanded;

        public static void NotifyLocalConnectHitLanded()
        {
            LocalConnectHitLanded?.Invoke();
        }
    }
}
