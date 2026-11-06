using System.Collections.Generic;
using UnityEngine;

namespace SS3D.UI.MachineInterface
{
    public enum MeterScenario
    {
        Nominal = 0,
        Overpressure = 1,
        Contaminated = 2,
        Vacuum = 3,
    }

    public sealed class MeterInterfaceViewModel : IMachineInterfaceViewModel
    {
        public string Title { get; set; } = "METER · ATMOSPHERICS";

        public string ModelLabel { get; set; } = "GM-02 · pipe gas meter";

        public string DeviceTitle { get; set; } = "GM-02";

        public const string DefaultDeviceKind = "Pipe Gas Meter";

        public string Subtitle { get; set; } = DefaultDeviceKind;

        public string LineLabel { get; set; } = "NO LINE";

        public string FooterText { get; set; } = "SS3D Atmospherics — Gas Meter Model GM-02";

        public MeterScenario Scenario { get; set; } = MeterScenario.Vacuum;

        public StatusTone StatusTone { get; set; } = StatusTone.Neutral;

        public StatusTone ConnectionTone { get; set; } = StatusTone.Neutral;

        public string StatusBadgeText { get; set; } = "VACUUM";

        public string StatusHeadline { get; set; } = "Line Empty";

        public string StatusSubline { get; set; } = "Negligible gas in pipe segment.";

        public string PressureValue { get; set; } = "0.00";

        public StatusTone PressureTone { get; set; } = StatusTone.Neutral;

        public float PressurePercent { get; set; }

        public string ScaleMidText { get; set; } = string.Empty;

        public string ScaleMaxText { get; set; } = string.Empty;

        public float SafeStartPercent { get; set; }

        public float SafeWidthPercent { get; set; }

        public float[] HistoryBars { get; set; } = System.Array.Empty<float>();

        public string TrendText { get; set; } = "steady";

        public string TemperatureText { get; set; } = "—";

        public StatusTone TemperatureTone { get; set; } = StatusTone.Neutral;

        public string MolesText { get; set; } = "—";

        public string VolumeText { get; set; } = "—";

        public List<GasMixSegment> Segments { get; set; } = new();

        public List<GasCompositionRowView> Gases { get; set; } = new();

        public string RegistryText { get; set; } = "0 types present";

        public static MeterInterfaceViewModel CreatePreview(MeterScenario scenario)
        {
            float pressure = scenario switch
            {
                MeterScenario.Overpressure => 4120f,
                MeterScenario.Contaminated => 186.4f,
                MeterScenario.Vacuum => 0.4f,
                _ => 101.3f,
            };

            var history = new float[40];
            for (int i = 0; i < history.Length; i++)
            {
                history[i] = pressure * (0.92f + 0.08f * Mathf.Sin(i * 0.5f) * Mathf.Sin(i * 0.5f));
            }

            return MeterInterfaceSnapshotMapper.ToViewModel(new MeterInterfaceSnapshot
            {
                ModelLabel = "GM-02 · pipe gas meter",
                DeviceTitle = "GM-02",
                LineLabel = "Supply line · 14 segments",
                Connected = true,
                TemperatureKelvin = 293.2f,
                Moles = 28.8f,
                VolumeLitres = 7000f,
                Gases = FilterInterfaceViewModel.PreviewGases(),
                Line = new GasLineReading
                {
                    PressureKpa = pressure,
                    Fractions = scenario == MeterScenario.Contaminated
                        ? new[] { 0.142f, 0.584f, 0.063f, 0.189f }
                        : new[] { 0.209f, 0.781f, 0.01f, 0f },
                },
                History = history,
            });
        }
    }
}
