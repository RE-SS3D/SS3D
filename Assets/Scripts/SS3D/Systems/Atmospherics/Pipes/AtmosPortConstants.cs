namespace SS3D.Systems.Atmospherics.Pipes
{
    public static class AtmosPortConstants
    {
        /// <summary>Rated vent throughput under ideal differential (moles/s total mixture).</summary>
        public const float VentRatedFlowMolesPerSecond = 100f;

        /// <summary>Gap between line and tile above which a vent runs at its rated flow (kPa).</summary>
        public const float VentFullFlowDifferentialKpa = 20f;

        /// <summary>Rated scrubber throughput for filtered gases (moles/s).</summary>
        public const float ScrubberRatedFlowMolesPerSecond = 60f;

        /// <summary>Rated throughput of a chamber intake (moles/s), matching the pump that follows it.</summary>
        public const float AirIntakeRatedFlowMolesPerSecond = 100f;

        /// <summary>Backpressure differential at which port flow tapers to zero (kPa).</summary>
        public const float PortMaxDifferentialKpa = 101.325f;

        /// <summary>Rated gas pump throughput under ideal differential (moles/s total mixture).</summary>
        public const float PumpRatedFlowMolesPerSecond = 100f;

        /// <summary>Motor stall threshold — no flow above this differential (kPa).</summary>
        public const float PumpMaxDifferentialKpa = 450f;

        /// <summary>Starting target of a pressure pump (kPa): room pressure, for an engineer to raise.</summary>
        public const int PumpDefaultTargetKpa = 101;

        /// <summary>Scrubber flow rate setting outside a panic.</summary>
        public const int ScrubberNormalFlowRate = 5;

        /// <summary>Highest scrubber flow rate setting.</summary>
        public const int ScrubberMaxFlowRate = 10;

        /// <summary>Starting set rate of a volume pump (L/s).</summary>
        public const int VolumePumpDefaultLitresPerSecond = 500;

        /// <summary>Highest set rate of a volume pump (L/s).</summary>
        public const int VolumePumpMaxLitresPerSecond = 2000;

        /// <summary>Starting flow rate of a filter (L/s).</summary>
        public const int FilterDefaultLitresPerSecond = 50;

        /// <summary>Highest flow rate of a filter (L/s).</summary>
        public const int FilterMaxLitresPerSecond = 200;

        /// <summary>Most moles a filter passes a second, whatever its flow rate.</summary>
        public const float FilterRatedFlowMolesPerSecond = 100f;

        /// <summary>Pressure at which a filter calls an outlet full and stops (kPa).</summary>
        public const float FilterOutletLimitKpa = 4500f;

        /// <summary>Most moles a mixer passes a second.</summary>
        public const float MixerRatedFlowMolesPerSecond = 100f;

        /// <summary>Highest target outlet pressure of a pump or mixer (kPa).</summary>
        public const int MaxTargetPressureKpa = 9000;

        /// <summary>A mixer's starting share from inlet 1 (percent).</summary>
        public const int MixerDefaultInlet1Percent = 50;

        /// <summary>Moles at or below which a mixer counts an inlet as dry.</summary>
        public const float MixerDryMoles = 0.001f;

        /// <summary>Rated injector throughput under ideal differential (moles/s total mixture).</summary>
        public const float InjectorRatedFlowMolesPerSecond = 50f;

        /// <summary>Share of the pressure gap an open valve closes each tick.</summary>
        public const float ValveEqualiseFraction = 0.5f;

        /// <summary>Moles a tick below which a device counts as idle, so a settled valve goes quiet.</summary>
        public const float FlowingMolesThreshold = 0.001f;
    }
}
