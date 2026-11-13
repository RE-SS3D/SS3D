using SS3D.Systems.Health;

namespace SS3D.Systems.Combat
{
    /// <summary>
    /// Firing from the floor (health.md §4 Downed): a one-handed weapon still works with a very wide cone; a weapon that
    /// needs both hands does not fire at all.
    /// </summary>
    public static class DownedFireRules
    {
        public static bool MayFire(HealthSnapshot shooter, bool requiresBothHands) => !shooter.IsDowned || !requiresBothHands;

        public static float SpreadMultiplier(HealthSnapshot shooter) =>
            shooter.IsDowned ? HealthConstants.DownedSpreadMultiplier : 1f;
    }
}
