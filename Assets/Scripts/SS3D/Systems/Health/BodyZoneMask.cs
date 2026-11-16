using System;

namespace SS3D.Systems.Health
{
    /// <summary>
    /// Bitmask over <see cref="BodyZone"/> — lets a single armor piece (or future effect) cover
    /// more than one zone at once.
    /// </summary>
    [Flags]
    public enum BodyZoneMask
    {
        None = 0,
        Head = 1 << (int)BodyZone.Head,
        Chest = 1 << (int)BodyZone.Chest,
        LeftArm = 1 << (int)BodyZone.LeftArm,
        RightArm = 1 << (int)BodyZone.RightArm,
        LeftLeg = 1 << (int)BodyZone.LeftLeg,
        RightLeg = 1 << (int)BodyZone.RightLeg,
        Groin = 1 << (int)BodyZone.Groin,
    }

    public static class BodyZoneMaskExtensions
    {
        public static bool Contains(this BodyZoneMask mask, BodyZone zone)
        {
            return (mask & (BodyZoneMask)(1 << (int)zone)) != 0;
        }
    }
}
