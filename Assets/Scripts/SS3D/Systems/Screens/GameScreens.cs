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

        private static ScreenType LastScreen { get; set; }

        /// <summary>
        /// Registers a screen into a screen list.
        /// </summary>
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
        public static bool TryGet<T>(ScreenType screenType, [CanBeNull] out T screen) where T : GameScreen
        {
            if (Screens.TryGetValue(screenType, out GameScreen match))
            {
                screen = match as T;
                return true;
            }                                                                   

            string message = $"No screen of type {screenType} found.";

            // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
            Punpun.Error(typeof(Subsystems), message, Logs.Important, typeof(T).Name);

            screen = null;
            return false;
        }

        public static void SwitchTo(ScreenType screenToSwitchTo)
        {
            LastScreen = screenToSwitchTo;

            foreach (KeyValuePair<ScreenType,GameScreen> screenEntry in Screens)
            {
                GameScreen screen = screenEntry.Value;

                screen.SetScreenState(screenToSwitchTo);
            }
        }
    }
}