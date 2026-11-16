namespace SS3D.Systems.Combat
{
    /// <summary>
    /// Key moments of the Upper Body "Reload Rifle" clip (Mix_Reloading, Basic Shooter Pack), played at
    /// speed 1. Gameplay follows the animation: the reload completes, and the gun can fire, when the
    /// hands are back in the aim pose. Measured by sampling the hands against the M4 in the editor;
    /// a new reload clip means re-measuring every value here.
    /// </summary>
    public static class RifleReloadClipTiming
    {
        /// <summary>Clip length in seconds.</summary>
        public const float ClipSeconds = 3.3f;

        /// <summary>Left hand reaches the magazine well; the magazine starts following the hand.</summary>
        public const float GrabStartSeconds = 0.36f;

        /// <summary>Magazine fully in the left hand.</summary>
        public const float GrabEndSeconds = 0.63f;

        /// <summary>Left hand brings the new magazine up to the well.</summary>
        public const float InsertStartSeconds = 1.32f;

        /// <summary>Magazine seated (mag-in sound).</summary>
        public const float MagazineInSeconds = 1.42f;

        /// <summary>Left hand slaps the bolt catch behind the magwell (bolt-forward sound).</summary>
        public const float BoltReleaseSeconds = 2.15f;

        /// <summary>Both hands back within ~2 cm of the aim pose: reload completes, the clip exits to aim idle.</summary>
        public const float ReadySeconds = 2.7f;

        /// <summary><paramref name="seconds"/> as normalized clip time.</summary>
        public static float Normalized(float seconds) => seconds / ClipSeconds;
    }
}
