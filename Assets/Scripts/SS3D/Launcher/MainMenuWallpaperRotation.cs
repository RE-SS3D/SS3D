namespace SS3D.Launcher
{
    /// <summary>What <see cref="MainMenuWallpaperRotation.Tick"/> asks the menu to do this frame.</summary>
    public enum MainMenuWallpaperStep
    {
        None = 0,
        FadeOut = 1,
        Swap = 2,
        FadeIn = 3,
    }

    /// <summary>
    /// Timing of the menu's wallpaper changes: hold, dip to black, swap behind the black, then lift it
    /// once a new video has a frame. A dip covers every still and video pairing with one code path.
    /// </summary>
    public sealed class MainMenuWallpaperRotation
    {
        public const float HoldSeconds = 60f;
        /// <summary>Must match the veil's transition in MainMenu.uss.</summary>
        public const float FadeSeconds = 0.5f;
        private const float LoadTimeoutSeconds = 1.5f;

        private enum Phase { Holding, FadingOut, Loading }

        private Phase _phase;
        private float _nextAt;

        public MainMenuWallpaperRotation(float now) => Restart(now);

        /// <summary>Begin a full hold; the current wallpaper has just been chosen.</summary>
        public void Restart(float now)
        {
            _phase = Phase.Holding;
            _nextAt = now + HoldSeconds;
        }

        /// <summary>Open on black and lift once the wallpaper is ready, as after a swap.</summary>
        public void Reveal(float now)
        {
            _phase = Phase.Loading;
            _nextAt = now + LoadTimeoutSeconds;
        }

        /// <param name="now">Unscaled time in seconds.</param>
        /// <param name="browsing">False during dialogs: no new dip starts, and a dip in progress ends early.</param>
        /// <param name="wallpaperReady">True once the wallpaper can be seen: a still, or a video with a frame.</param>
        public MainMenuWallpaperStep Tick(float now, bool browsing, bool wallpaperReady)
        {
            switch (_phase)
            {
                case Phase.Holding:
                    if (!browsing || now < _nextAt) return MainMenuWallpaperStep.None;
                    _phase = Phase.FadingOut;
                    _nextAt = now + FadeSeconds;
                    return MainMenuWallpaperStep.FadeOut;
                case Phase.FadingOut:
                    if (now < _nextAt) return MainMenuWallpaperStep.None;
                    _phase = Phase.Loading;
                    _nextAt = now + LoadTimeoutSeconds;
                    return MainMenuWallpaperStep.Swap;
                default:
                    // The video layer crossfades in over FadeSeconds; let it finish behind the black.
                    if (wallpaperReady && now + FadeSeconds < _nextAt) _nextAt = now + FadeSeconds;
                    if (browsing && now < _nextAt) return MainMenuWallpaperStep.None;
                    Restart(now);
                    return MainMenuWallpaperStep.FadeIn;
            }
        }
    }
}
