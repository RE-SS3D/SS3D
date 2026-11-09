using UnityEngine;

namespace SS3D.Systems.Electricity
{
    /// <summary>Pure rules for breaking, removing and fitting fixture bulbs. EditMode tests cover them.</summary>
    public static class LightBulbRules
    {
        /// <summary>Only a whole bulb of the fixture's own kind goes in, and only when the socket isn't already whole.</summary>
        public static bool CanInsert(LightBulbState socket, LightBulbKind fixtureKind, LightBulbKind bulbKind, bool bulbBroken)
        {
            return socket != LightBulbState.Intact && fixtureKind == bulbKind && !bulbBroken;
        }

        /// <summary>Anything in the socket can come out into an empty hand.</summary>
        public static bool CanRemove(LightBulbState socket, bool handEmpty)
        {
            return socket != LightBulbState.Empty && handEmpty;
        }

        /// <summary>A bulb only emits while it's whole.</summary>
        public static bool CanEmit(LightBulbState socket) => socket == LightBulbState.Intact;

        /// <summary>
        /// True when <paramref name="shot"/> crosses <paramref name="glass"/> closer than <paramref name="nearest"/>
        /// (where the round lands, or the closest bulb found so far), which then becomes that distance.
        /// </summary>
        public static bool CrossesBefore(Ray shot, Bounds glass, ref float nearest)
        {
            if (!glass.IntersectRay(shot, out float hit) || hit < 0f || hit >= nearest)
            {
                return false;
            }

            nearest = hit;
            return true;
        }
    }
}
