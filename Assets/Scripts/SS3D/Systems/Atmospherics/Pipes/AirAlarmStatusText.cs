using System.Collections.Generic;

namespace SS3D.Systems.Atmospherics.Pipes
{
    /// <summary>How bad the alarm's status is, for its panel badge and examine colour.</summary>
    public enum AirAlarmSeverity : byte
    {
        Normal = 0,
        Warning = 1,
        Danger = 2,
    }

    /// <summary>One status line: a short headline and the advice under it.</summary>
    public readonly struct AirAlarmStatus
    {
        public readonly AirAlarmSeverity Severity;
        public readonly string Badge;
        public readonly string Headline;
        public readonly string Subline;

        public AirAlarmStatus(AirAlarmSeverity severity, string badge, string headline, string subline)
        {
            Severity = severity;
            Badge = badge;
            Headline = headline;
            Subline = subline;
        }
    }

    /// <summary>
    /// Words for what an air alarm is reacting to, built from its real flags and pressure trend. The panel used to
    /// show a fixed "CO2 RISING" for every warning, so a pressure drop read as a CO₂ alarm.
    /// </summary>
    public static class AirAlarmStatusText
    {
        private readonly struct Condition
        {
            public readonly AirAlarmState Flag;
            public readonly string Headline;
            public readonly string Short;
            public readonly string Advice;

            public Condition(AirAlarmState flag, string headline, string shortName, string advice)
            {
                Flag = flag;
                Headline = headline;
                Short = shortName;
                Advice = advice;
            }
        }

        // Worst first: the first condition that is set names the headline.
        private static readonly Condition[] Conditions =
        {
            new(AirAlarmState.HighPlasma, "PLASMA DETECTED", "plasma",
                "Toxic gas in the room. Wear internals and set scrubbers to toxins."),
            new(AirAlarmState.HighTemperature, "TEMPERATURE HIGH", "heat",
                "Room above 50°C. Check for fire."),
            new(AirAlarmState.LowPressure, "LOW PRESSURE", "low pressure",
                "Pressure below 80 kPa. Internals advised."),
            new(AirAlarmState.LowOxygen, "LOW OXYGEN", "low oxygen",
                "Oxygen below 18%. Internals advised."),
            new(AirAlarmState.HighPressure, "HIGH PRESSURE", "high pressure",
                "Pressure above 150 kPa. Check vents and pumps."),
            new(AirAlarmState.HighCarbonDioxide, "CO2 HIGH", "CO2",
                "CO2 above 1%. Check the scrubbers."),
        };

        public static AirAlarmStatus Describe(bool powered, bool hasSample, AirAlarmState state, AirAlarmIndicator indicator)
        {
            if (!powered)
                return new AirAlarmStatus(AirAlarmSeverity.Danger, "NO POWER", "NO POWER", "Alarm unpowered. No readings.");

            if (!hasSample)
                return new AirAlarmStatus(AirAlarmSeverity.Warning, "NO READING", "NO READING", "No room assigned to sample.");

            if (indicator == AirAlarmIndicator.Repressurizing)
            {
                return new AirAlarmStatus(AirAlarmSeverity.Warning, "REFILLING", "REPRESSURIZING",
                    WithOthers("Pressure low and rising. Vents are refilling the room.", state, AirAlarmIndicatorRules.RecoverableFlags));
            }

            if (indicator == AirAlarmIndicator.Depressurizing)
            {
                return new AirAlarmStatus(AirAlarmSeverity.Danger, "LEAK", "DEPRESSURIZING",
                    WithOthers("Pressure falling. Check for a breach.", state, AirAlarmState.LowPressure));
            }

            if (!TryGetWorst(state, out Condition worst))
                return new AirAlarmStatus(AirAlarmSeverity.Normal, "NORMAL", "ATMOSPHERE NOMINAL", "All readings within safe tolerance.");

            // The badge matches the lights: a fast red flash is danger, anything milder a warning.
            AirAlarmSeverity severity = indicator == AirAlarmIndicator.Danger ? AirAlarmSeverity.Danger : AirAlarmSeverity.Warning;
            return new AirAlarmStatus(severity, severity == AirAlarmSeverity.Danger ? "DANGER" : "WARNING", worst.Headline,
                WithOthers(worst.Advice, state, worst.Flag));
        }

        /// <summary>Every condition <paramref name="state"/> sets, in short words, worst first: "low pressure, low oxygen".</summary>
        public static string ListConditions(AirAlarmState state)
        {
            List<string> names = new();
            foreach (Condition condition in Conditions)
            {
                if ((state & condition.Flag) != 0)
                    names.Add(condition.Short);
            }

            return string.Join(", ", names);
        }

        private static string WithOthers(string advice, AirAlarmState state, AirAlarmState covered)
        {
            string others = ListConditions(state & ~covered);
            return others.Length == 0 ? advice : $"{advice} Also: {others}.";
        }

        private static bool TryGetWorst(AirAlarmState state, out Condition worst)
        {
            foreach (Condition condition in Conditions)
            {
                if ((state & condition.Flag) != 0)
                {
                    worst = condition;
                    return true;
                }
            }

            worst = default;
            return false;
        }
    }
}
