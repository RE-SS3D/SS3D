using UnityEngine;

namespace SS3D.UI.MachineInterface
{
    public static class MeterInterfaceSnapshotMapper
    {
        /// <summary>Full scale of the pressure bar, a filter's outlet limit (kPa).</summary>
        public const float ScaleMaxKpa = 4500f;

        /// <summary>Where the bar's safe band starts and ends (kPa).</summary>
        public const float SafeMinKpa = 80f;

        public const float SafeMaxKpa = 450f;

        /// <summary>At or above this a line reads as overpressure (kPa).</summary>
        public const float OverpressureKpa = 4050f;

        /// <summary>Below this a line reads as empty (kPa).</summary>
        public const float VacuumKpa = 1f;

        /// <summary>A toxic gas above this share marks a line contaminated (percent).</summary>
        public const float ContaminatedPercent = 1f;

        private const float SteadyShare = 0.005f;
        private const float MinimumBarShare = 0.04f;
        private const float MinimumPressureBarPercent = 0.5f;
        private const float ColdCelsius = -100f;
        private const float HotCelsius = 60f;
        private const float KelvinOffset = 273.15f;

        public static MeterScenario Classify(float pressureKpa, GasPanelRow[] gases, float[] fractions, out int contaminantIndex)
        {
            contaminantIndex = -1;
            if (pressureKpa >= OverpressureKpa)
            {
                return MeterScenario.Overpressure;
            }

            if (pressureKpa < VacuumKpa)
            {
                return MeterScenario.Vacuum;
            }

            for (int i = 0; gases != null && fractions != null && i < gases.Length && i < fractions.Length; i++)
            {
                if (gases[i].Toxic && fractions[i] * 100f > ContaminatedPercent)
                {
                    contaminantIndex = i;
                    return MeterScenario.Contaminated;
                }
            }

            return MeterScenario.Nominal;
        }

        public static MeterInterfaceViewModel ToViewModel(MeterInterfaceSnapshot snapshot)
        {
            float pressure = snapshot.Line.PressureKpa;
            MeterScenario scenario = Classify(pressure, snapshot.Gases, snapshot.Line.Fractions, out int contaminant);
            var model = new MeterInterfaceViewModel
            {
                Scenario = scenario,
                ModelLabel = snapshot.ModelLabel,
                DeviceTitle = snapshot.DeviceTitle,
                LineLabel = snapshot.Connected ? snapshot.LineLabel : "NO LINE",
                FooterText = $"SS3D Atmospherics — Gas Meter Model {snapshot.DeviceTitle}",
                ConnectionTone = scenario == MeterScenario.Vacuum ? StatusTone.Neutral : StatusTone.Success,
                PressureValue = FormatPressure(pressure),
                PressurePercent = Mathf.Clamp(pressure / ScaleMaxKpa * 100f, MinimumPressureBarPercent, 100f),
                ScaleMidText = FormatPressure(ScaleMaxKpa / 2f),
                ScaleMaxText = $"{FormatPressure(ScaleMaxKpa)} kPa",
                SafeStartPercent = SafeMinKpa / ScaleMaxKpa * 100f,
                SafeWidthPercent = (SafeMaxKpa - SafeMinKpa) / ScaleMaxKpa * 100f,
            };

            ApplyStatus(model, snapshot, scenario, contaminant);
            MapHistory(model, snapshot.History, pressure);
            MapStats(model, snapshot);
            MapGases(model, snapshot);
            return model;
        }

        private static void ApplyStatus(
            MeterInterfaceViewModel model,
            in MeterInterfaceSnapshot snapshot,
            MeterScenario scenario,
            int contaminant)
        {
            switch (scenario)
            {
                case MeterScenario.Overpressure:
                    model.StatusTone = StatusTone.Danger;
                    model.PressureTone = StatusTone.Danger;
                    model.StatusBadgeText = "OVER";
                    model.StatusHeadline = "Overpressure";
                    model.StatusSubline = "Line near rupture rating — vent or bleed downstream.";
                    break;
                case MeterScenario.Contaminated:
                    model.StatusTone = StatusTone.Warning;
                    model.PressureTone = StatusTone.Warning;
                    model.StatusBadgeText = "TOXIC";
                    model.StatusHeadline = "Contaminated";
                    model.StatusSubline = $"{snapshot.Gases[contaminant].Symbol} above {ContaminatedPercent:0}% — check upstream filters.";
                    break;
                case MeterScenario.Vacuum:
                    model.StatusTone = StatusTone.Neutral;
                    model.PressureTone = StatusTone.Neutral;
                    model.StatusBadgeText = "VACUUM";
                    model.StatusHeadline = "Line Empty";
                    model.StatusSubline = snapshot.Connected
                        ? "Negligible gas in pipe segment."
                        : "No pipe under the meter.";
                    break;
                default:
                    model.StatusTone = StatusTone.Success;
                    model.PressureTone = StatusTone.Success;
                    model.StatusBadgeText = "NOMINAL";
                    model.StatusHeadline = "Line Nominal";
                    model.StatusSubline = "Pressure and mix within station spec.";
                    break;
            }
        }

        private static void MapHistory(MeterInterfaceViewModel model, float[] history, float pressure)
        {
            if (history == null || history.Length == 0)
            {
                return;
            }

            float max = 1f;
            foreach (float sample in history)
            {
                max = Mathf.Max(max, sample);
            }

            model.HistoryBars = new float[history.Length];
            for (int i = 0; i < history.Length; i++)
            {
                model.HistoryBars[i] = Mathf.Max(MinimumBarShare, history[i] / max);
            }

            float change = pressure - history[0];
            model.TrendText = Mathf.Abs(change) < pressure * SteadyShare
                ? "steady"
                : $"{(change > 0f ? "▲" : "▼")} {FormatPressure(Mathf.Abs(change))} kPa";
        }

        private static void MapStats(MeterInterfaceViewModel model, in MeterInterfaceSnapshot snapshot)
        {
            if (!snapshot.Connected)
            {
                return;
            }

            float celsius = snapshot.TemperatureKelvin - KelvinOffset;
            model.TemperatureText = $"{celsius:F1} °C";
            model.TemperatureTone = celsius < ColdCelsius ? StatusTone.Info
                : celsius > HotCelsius ? StatusTone.Warning
                : StatusTone.Neutral;
            model.MolesText = snapshot.Moles >= 100f ? $"{snapshot.Moles:F0} mol" : $"{snapshot.Moles:F2} mol";
            model.VolumeText = $"{snapshot.VolumeLitres:F0} L";
        }

        private static void MapGases(MeterInterfaceViewModel model, in MeterInterfaceSnapshot snapshot)
        {
            GasMixDescriber.Describe(snapshot.Gases, snapshot.Line.Fractions, model.Segments);
            if (snapshot.Gases == null)
            {
                return;
            }

            int present = 0;
            for (int i = 0; i < snapshot.Gases.Length; i++)
            {
                GasPanelRow gas = snapshot.Gases[i];
                float fraction = snapshot.Line.Fractions != null && i < snapshot.Line.Fractions.Length
                    ? snapshot.Line.Fractions[i]
                    : 0f;
                float percent = fraction * 100f;
                bool isPresent = fraction > 0f;
                present += isPresent ? 1 : 0;
                model.Gases.Add(new GasCompositionRowView
                {
                    Symbol = gas.Symbol,
                    Name = gas.Name,
                    Color = gas.Color,
                    Percent = percent,
                    PercentText = $"{percent:F1}%",
                    PressureText = isPresent ? $"{FormatPressure(snapshot.Line.PressureKpa * fraction)} kPa" : "—",
                    Present = isPresent,
                    Hazard = gas.Toxic && percent > ContaminatedPercent,
                });
            }

            model.Gases.Sort((a, b) => b.Percent.CompareTo(a.Percent));
            model.RegistryText = $"{present} of {snapshot.Gases.Length} types present";
        }

        private static string FormatPressure(float kpa)
        {
            if (kpa >= 1000f)
            {
                return kpa.ToString("F0");
            }

            return kpa >= 10f ? kpa.ToString("F1") : kpa.ToString("F2");
        }
    }
}
