using JetBrains.Annotations;
using SS3D.Core;
using SS3D.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Screens
{
    public static class GameScreens
    {
        /// <summary>
        /// A dictionary containing all the objects that registered themselves.
        /// </summary>
        private static readonly Dictionary<ScreenType, GameScreen> Screens = new();

        public static ScreenType ActiveScreen { get; set; }

        public static ScreenType LastScreen { get; set; }

        /// <summary>
        /// Registers a screen into a screen list.
        /// </summary>
        [ServerOrClient]
        public static void Register([NotNull] GameScreen screen)
        {
            if (!Screens.TryGetValue(screen.ScreenType, out GameScreen _))
            {
                Screens.Add(screen.ScreenType, screen);
            }
        }

        /// <summary>
        /// Removes a screen from the screen list.
        /// </summary>
        [ServerOrClient]
        public static void Unregister(ScreenType type)
        {
            if (Screens.TryGetValue(type, out GameScreen _))
            {
                Screens.Remove(type);
            }
        }

        /// <summary>
        /// Tries to get a screen of ScreenType.
        /// </summary>
        [ServerOrClient]
        public static bool TryGet<T>(ScreenType screenType, [CanBeNull] out T screen)
            where T : GameScreen
        {
            if (Screens.TryGetValue(screenType, out GameScreen match))
            {
                screen = match as T;
                return true;
            }

            string message = $"No screen of type {screenType} found.";

            // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
            Log.Error(typeof(GameScreens), message, Logs.Important, typeof(T).Name);

            screen = null;
            return false;
        }

        [ServerOrClient]
        public static void SwitchTo(ScreenType screenToSwitchTo)
        {
            LastScreen = ActiveScreen;
            ActiveScreen = screenToSwitchTo;

            Log.Information(typeof(GameScreens), $"Switching game screen to {screenToSwitchTo}");

            foreach (KeyValuePair<ScreenType, GameScreen> screenEntry in Screens)
            {
                GameScreen screen = screenEntry.Value;

                screen.SetScreenState(screenToSwitchTo);
            }
        }
    }
}