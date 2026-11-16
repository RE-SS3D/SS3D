using UnityEngine;

namespace SS3D.UI.MachineInterface
{
    public enum MixerScenario
    {
        Idle = 0,
        Mixing = 1,
        Stopped = 2,
    }

    public sealed class MixerInterfaceViewModel : IMachineInterfaceViewModel, IAccessGatedInterfaceViewModel
    {
        public string Title { get; set; } = "MIXER · ATMOSPHERICS";

        public string ModelLabel { get; set; } = "MIX-11 · gas mixer unit";

        public string DeviceTitle { get; set; } = "MIX-11";

        public string Subtitle { get; set; } = "Gas Mixer";

        public string ConnectionStatus { get; set; } = "WIRED · PIPE NET · PORT M1";

        public string FooterText { get; set; } = "SS3D Atmospherics — Mixer Unit Model MIX-11";

        public bool ChassisPowerOk { get; set; } = true;

        public MixerScenario Scenario { get; set; } = MixerScenario.Idle;

        public string StatusBadgeText { get; set; } = "IDLE";

        public string StatusHeadline { get; set; } = "Mixer Idle";

        public string StatusSubline { get; set; } = "Powered off — no throughput.";

        public GasPortView Inlet1 { get; set; } = new() { Label = "Inlet 1" };

        public GasPortView Inlet2 { get; set; } = new() { Label = "Inlet 2" };

        public GasPortView Outlet { get; set; } = new() { Label = "Outlet" };

        public string OutletTargetText { get; set; } = "Target 101 kPa";

        public string FlowGlyph { get; set; } = "·";

        public StatusTone FlowTone { get; set; } = StatusTone.Neutral;

        public bool AccessGranted { get; set; }

        public bool AccessScanning { get; set; }

        public bool AccessDenied { get; set; }

        public bool Powered { get; set; } = true;

        public int Inlet1Percent { get; set; } = 50;

        public int TargetOutletPressureKpa { get; set; } = 101;

        public Color Inlet1Color { get; set; } = new(0.54f, 0.55f, 0.57f);

        public Color Inlet2Color { get; set; } = new(0.54f, 0.55f, 0.57f);

        public static MixerInterfaceViewModel CreatePreview(MixerScenario scenario)
        {
            GasPanelRow[] gases = FilterInterfaceViewModel.PreviewGases();
            return MixerInterfaceSnapshotMapper.ToViewModel(new MixerInterfaceSnapshot
            {
                ModelLabel = "MIX-11 · gas mixer unit",
                DeviceTitle = "MIX-11",
                Subtitle = "Atmospherics Distro — Gas Mixer",
                PowerOk = true,
                Powered = scenario != MixerScenario.Idle,
                Connected = true,
                Scenario = (byte)scenario,
                Inlet1Percent = 20,
                TargetOutletPressureKpa = 4500,
                Inlet2Dry = scenario == MixerScenario.Stopped,
                Gases = gases,
                Inlet1 = new GasLineReading { PressureKpa = 2410.6f, Fractions = new[] { 1f, 0f, 0f, 0f } },
                Inlet2 = new GasLineReading
                {
                    PressureKpa = scenario == MixerScenario.Stopped ? 0f : 3122f,
                    Fractions = new[] { 0f, 0.985f, 0.015f, 0f },
                },
                Outlet = new GasLineReading
                {
                    PressureKpa = 4487.6f,
                    Fractions = new[] { 0.2f, 0.788f, 0.012f, 0f },
                },
            });
        }
    }
}
