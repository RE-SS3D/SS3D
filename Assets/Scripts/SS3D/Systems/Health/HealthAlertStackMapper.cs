namespace SS3D.Systems.Health
{
    /// <summary>
    /// Maps a local player's <see cref="HealthSnapshot"/> onto health-driven alert-stack signals.
    /// Returns Health-local severities so this assembly never references Main HUD types.
    /// Atmos / hunger / restraint hazards are left alone (those systems own them).
    /// </summary>
    public static class HealthAlertStackMapper
    {
        /// <summary>
        /// How urgently a health hazard reads. Mirrors Main HUD's None/Warning/Critical without
        /// taking a dependency on that assembly.
        /// </summary>
        public enum Severity
        {
            None,
            Warning,
            Critical,
        }

        public struct HealthAlertSignals
        {
            public Severity Bleeding;
            public Severity Dying;
            public Severity CardiacArrest;
            public Severity LowOxygen;
            public Severity Shock;
        }

        /// <summary>
        /// Bleed rate at or above this is Critical (matches Severe+ wound rates); below is Warning.
        /// </summary>
        public const float BleedingCriticalRate = 1f;

        public static HealthAlertSignals Compute(HealthSnapshot snapshot)
        {
            if (snapshot.State == HealthState.Dead)
            {
                return default;
            }

            return new HealthAlertSignals
            {
                Bleeding = ComputeBleeding(snapshot),
                Dying = snapshot.State == HealthState.Critical ? Severity.Critical : Severity.None,
                CardiacArrest = snapshot.IsCardiacArrest ? Severity.Critical : Severity.None,
                LowOxygen = ComputeLowOxygen(snapshot),
                Shock = ComputeShock(snapshot),
            };
        }

        private static Severity ComputeBleeding(HealthSnapshot snapshot)
        {
            if (snapshot.TotalBleedingRate >= BleedingCriticalRate)
            {
                return Severity.Critical;
            }

            if (snapshot.IsBleeding && snapshot.TotalBleedingRate < BleedingCriticalRate)
            {
                return Severity.Warning;
            }

            return Severity.None;
        }

        /// <summary>Warning from the stumble line; critical once Downed (or worse, from shock).</summary>
        private static Severity ComputeShock(HealthSnapshot snapshot)
        {
            if (snapshot.State == HealthState.Downed || (snapshot.CriticalFlags & HealthCriticalFlags.HighShock) != 0)
            {
                return Severity.Critical;
            }

            return snapshot.Shock >= HealthConstants.ShockWarningThreshold ? Severity.Warning : Severity.None;
        }

        private static Severity ComputeLowOxygen(HealthSnapshot snapshot)
        {
            if (snapshot.Pools.OxyDebt >= HealthConstants.CriticalOxyDebt
                || (snapshot.CriticalFlags & HealthCriticalFlags.HighOxyDebt) != 0)
            {
                return Severity.Critical;
            }

            // Ignore micro-debt from float/local-mix noise — only warn once hypoxia is meaningful.
            if (snapshot.Pools.OxyDebt >= HealthConstants.LowOxygenAlertSoftStart)
            {
                return Severity.Warning;
            }

            return Severity.None;
        }
    }
}
