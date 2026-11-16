using UnityEngine;

namespace SS3D.Core
{
    /// <summary>
    /// The vertical band everything on the station lives in: the deck is world y = 0 and the walls stop at about 2.5 m.
    /// Every floor tile carries a ceiling collider on <see cref="CeilingLayerName"/> that only items and bodies collide
    /// with, and the server moves anything it finds outside the band back onto the floor.
    /// </summary>
    public static class StationBounds
    {
        public const string CeilingLayerName = "StationCeiling";

        /// <summary>Underside of the ceiling on Plenum.prefab, just under the wall tops so nothing can rest on a wall.</summary>
        public const float CeilingHeight = 2.5f;

        /// <summary>
        /// A root higher than this is above the ceiling. Something that hits the ceiling fast sinks a little way into it
        /// for a physics step before it is pushed back out, which is not an escape.
        /// </summary>
        public const float EscapedAbove = CeilingHeight + 0.5f;

        /// <summary>A root lower than this has fallen through the deck.</summary>
        public const float FloorLimit = -0.5f;

        private static int _ceilingMask = -1;

        /// <summary>Layer mask of the ceiling. Must not be read from a field initializer or static constructor.</summary>
        public static int CeilingMask
        {
            get
            {
                if (_ceilingMask < 0)
                {
                    _ceilingMask = LayerMask.GetMask(CeilingLayerName);
                }

                return _ceilingMask;
            }
        }

        /// <summary>
        /// <paramref name="mask"/> without the ceiling. Gameplay queries (picking, aim, reach, floor probes) must never
        /// see it: from the top-down camera it covers everything on the station.
        /// </summary>
        public static int WithoutCeiling(int mask) => mask & ~CeilingMask;

        public static bool IsCeiling(Collider collider) =>
            collider != null && ((1 << collider.gameObject.layer) & CeilingMask) != 0;
    }
}
