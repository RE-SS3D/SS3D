using System;
using SS3D.UI.Shell.MainMenu;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SS3D.Launcher
{
    /// <summary>
    /// The menu's wallpaper: a random still or looping video on each visit, a new one every
    /// <see cref="MainMenuWallpaperRotation.HoldSeconds"/> while browsing (a dip to black, the swap, the lift), and the video
    /// paused behind the operation dialogs. It writes the wallpaper fields of the presentation; the menu presents them.
    /// This is the wallpaper half of the tech-debt plan's <c>MainMenuAmbience</c> (P7.5); menu music still lives in
    /// <see cref="MainMenuSubSystem"/>.
    /// </summary>
    public sealed class MainMenuAmbience
    {
        private readonly MainMenuAssetCatalog _catalog;
        private readonly MainMenuVideoWallpaper _video;
        private MainMenuWallpaperRotation _rotation;
        private int _lastWallpaper = -1;

        public MainMenuAmbience(GameObject host, MainMenuAssetCatalog catalog)
        {
            _catalog = catalog;
            _video = new MainMenuVideoWallpaper(host);
        }

        /// <summary>A visit to the menu starts on a fresh pick. A video opens on black: its fallback still would flash up before the first frame.</summary>
        public void Show(MainMenuPresentation model, float now)
        {
            ChangeWallpaper(model);
            _rotation = new MainMenuWallpaperRotation(now);
            model.WallpaperVeiled = _catalog.WallpaperVideo(model.Wallpaper) != null;
            if (model.WallpaperVeiled)
            {
                _rotation.Reveal(now);
            }
        }

        /// <summary>The menu is gone (a session loaded or it was hidden): stop the video and free its render texture.</summary>
        public void Stop() => _video.Stop();

        /// <summary>Video plays behind the menu; the operation dialogs hold its frame for their blurred backdrop.</summary>
        public void UpdateVideo(MainMenuPresentation model)
        {
            _video.SetPlaying(IsBrowsing(model.Screen));
            model.VideoWallpaper = _video.Frame;
        }

        /// <summary>
        /// Advances the rotation (the owner kept the variety, 2026-11-23). True when the wallpaper fields changed and need
        /// presenting with <see cref="MainMenuView.PresentWallpaper"/>.
        /// </summary>
        public bool Tick(MainMenuPresentation model, float now)
        {
            bool frameChanged = _video.Tick();
            bool ready = _catalog.WallpaperVideo(model.Wallpaper) == null || _video.Frame != null;
            switch (_rotation?.Tick(now, IsBrowsing(model.Screen), ready) ?? MainMenuWallpaperStep.None)
            {
                case MainMenuWallpaperStep.FadeOut:
                    model.WallpaperVeiled = true;
                    break;
                case MainMenuWallpaperStep.Swap:
                    ChangeWallpaper(model);
                    break;
                case MainMenuWallpaperStep.FadeIn:
                    model.WallpaperVeiled = false;
                    break;
                default:
                    if (!frameChanged) return false;
                    break;
            }

            UpdateVideo(model);
            return true;
        }

        /// <summary>A random wallpaper other than <paramref name="previous"/>; -1 allows any.</summary>
        public static int PickOtherWallpaper(MainMenuAssetCatalog catalog, int previous, Func<float> roll)
        {
            int pick = catalog.PickWallpaper(roll(), roll());
            for (int attempt = 0; attempt < 16 && pick == previous; attempt++)
            {
                pick = catalog.PickWallpaper(roll(), roll());
            }

            return pick != previous ? pick : (previous + 1) % catalog.WallpaperCount;
        }

        private static bool IsBrowsing(MainMenuScreen screen) =>
            screen is MainMenuScreen.Menu or MainMenuScreen.Servers or MainMenuScreen.Settings;

        private void ChangeWallpaper(MainMenuPresentation model)
        {
            _lastWallpaper = model.Wallpaper = PickOtherWallpaper(_catalog, _lastWallpaper, () => Random.value);
            _video.Load(_catalog.WallpaperVideo(model.Wallpaper));
        }
    }
}
