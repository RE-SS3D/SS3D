using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.Shell
{
    /// <summary>
    /// The UI's motion, played through USS transitions (Tokens/ui-controls.uss, "Motion"). USS has no keyframes, so an
    /// entrance is two styles: the element snaps to its <see cref="EnterClass"/> look (faded, a little off) and
    /// eases into place when <see cref="InClass"/> is added a frame later; only the "in" look carries the transition,
    /// so replaying an entrance on a shown element resets it instead of fading it out. Loops (a pulse, a breathing
    /// glow) toggle a class on a timer and let its transition do the easing.
    /// </summary>
    public static class UiMotion
    {
        /// <summary>The hidden look: faded and 10 px low. <c>ui-enter--left</c>, <c>--right</c>, <c>--scale</c> vary it.</summary>
        public const string EnterClass = "ui-enter";

        /// <summary>In place. Added a frame after the element shows, so the transition runs.</summary>
        public const string InClass = "ui-enter--in";

        /// <summary>Between a pulse's two looks (<see cref="Pulse"/>).</summary>
        public const string PulseClass = "ui-pulse--on";

        /// <summary>
        /// Eases <paramref name="element"/> in from its <see cref="EnterClass"/> look; again each time it is called
        /// (a panel shown again, a tab switched to). <paramref name="delayMs"/> holds it back, for a stagger: the
        /// scheduler waits it out, so no transition delay stays on the element to slow its later hovers.
        /// </summary>
        public static void Enter(VisualElement element, long delayMs = 0)
        {
            if (element == null) return;
            element.AddToClassList(EnterClass);
            element.RemoveFromClassList(InClass);
            // Without a panel (EditMode tests, a detached tree) nothing would add it: show the element as it is.
            if (element.panel == null)
            {
                element.AddToClassList(InClass);
                return;
            }

            element.schedule.Execute(() => element.AddToClassList(InClass)).ExecuteLater(16 + Math.Max(0, delayMs));
        }

        /// <summary>Eases each child of <paramref name="parent"/> in, <paramref name="stepMs"/> apart.</summary>
        public static void Stagger(VisualElement parent, long stepMs = 35, long firstMs = 0, int maxStaggered = 12)
        {
            if (parent == null) return;
            int i = 0;
            foreach (VisualElement child in parent.Children())
            {
                Enter(child, firstMs + (Math.Min(i, maxStaggered) * stepMs));
                i++;
            }
        }

        /// <summary>
        /// Toggles <see cref="PulseClass"/> on <paramref name="element"/> every <paramref name="halfPeriodMs"/> until
        /// the returned item is paused, or the element leaves its panel. Style the pulse's far look on
        /// <c>.your-class.ui-pulse--on</c> with a transition as long as the half period.
        /// </summary>
        public static IVisualElementScheduledItem Pulse(VisualElement element, long halfPeriodMs = 700)
        {
            if (element == null) return null;
            return element.schedule.Execute(() => element.ToggleInClassList(PulseClass)).Every(halfPeriodMs);
        }

        /// <summary>Stops a <see cref="Pulse"/> and leaves the element at rest.</summary>
        public static void StopPulse(VisualElement element, IVisualElementScheduledItem pulse)
        {
            pulse?.Pause();
            element?.RemoveFromClassList(PulseClass);
        }

        /// <summary>A short "something happened here" flash: <c>ui-flash</c> on, off after <paramref name="ms"/>.</summary>
        public static void Flash(VisualElement element, long ms = 260)
        {
            if (element == null) return;
            element.AddToClassList("ui-flash");
            element.schedule.Execute(() => element.RemoveFromClassList("ui-flash")).ExecuteLater(ms);
        }

        /// <summary>Real seconds since startup, for animation drawn by hand (a spinner, a glint).</summary>
        public static float Time => UnityEngine.Time.realtimeSinceStartup;
    }
}
