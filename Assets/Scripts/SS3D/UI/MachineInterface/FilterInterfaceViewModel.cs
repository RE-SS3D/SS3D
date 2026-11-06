using System.Collections.Generic;
using UnityEngine;

namespace SS3D.UI.MachineInterface
{
    public enum FilterScenario
    {
        Idle = 0,
        Filtering = 1,
        Stalled = 2,
    }

    public sealed class FilterInterfaceViewModel : IMachineInterfaceViewModel, IAccessGatedInterfaceViewModel
    {
        public string Title { get; set; } = "FILTER · ATMOSPHERICS";

        public string ModelLabel { get; set; } = "FLT-04 · gas filter unit";

        public string DeviceTitle { get; set; } = "FLT-04";

        public string Subtitle { get; set; } = "Gas Filter";

        public string ConnectionStatus { get; set; } = "WIRED · PIPE NET · PORT F1";

        public string ThroughputText { get; set; } = "0 L/s";

        public string FooterText { get; set; } = "SS3D Atmospherics — Filter Unit Model FLT-04";

        public bool ChassisPowerOk { get; set; } = true;

        public FilterScenario Scenario { get; set; } = FilterScenario.Idle;

        public string StatusBadgeText { get; set; } = "IDLE";

        public string StatusHeadline { get; set; } = "Filter Idle";

        public string StatusSubline { get; set; } = "Powered off — no throughput.";

        public GasPortView Inlet { get; set; } = new() { Label = "Inlet" };

        public GasPortView Outlet { get; set; } = new() { Label = "Outlet" };

        public GasPortView FilterOutlet { get; set; } = new() { Label = "Filter outlet", HasCapacity = true };

        public string FlowGlyph { get; set; } = "·";

        public StatusTone FlowTone { get; set; } = StatusTone.Neutral;

        public bool AccessGranted { get; set; }

        public bool AccessScanning { get; set; }

        public bool AccessDenied { get; set; }

        public bool Powered { get; set; } = true;

        public int FlowLitresPerSecond { get; set; } = 50;

        public List<GasFilterRowView> Gases { get; set; } = new();

        public string RegistryText { get; set; } = "0 types · none filtered";

        /// <summary>Sample gases for the preview window.</summary>
        public static GasPanelRow[] PreviewGases() => new[]
        {
            new GasPanelRow { Id = 0, Symbol = "O2", Name = "Oxygen", Color = new Color32(111, 159, 216, 255) },
            new GasPanelRow { Id = 1, Symbol = "N2", Name = "Nitrogen", Color = new Color32(185, 188, 194, 255) },
            new GasPanelRow { Id = 2, Symbol = "CO2", Name = "Carbon Dioxide", Color = new Color32(125, 131, 139, 255) },
            new GasPanelRow { Id = 3, Symbol = "Plasma", Name = "Plasma", Color = new Color32(196, 111, 214, 255), Toxic = true },
        };

        public static FilterInterfaceViewModel CreatePreview(FilterScenario scenario)
        {
            GasPanelRow[] gases = PreviewGases();
            return FilterInterfaceSnapshotMapper.ToViewModel(new FilterInterfaceSnapshot
            {
                ModelLabel = "FLT-04 · gas filter unit",
                DeviceTitle = "FLT-04",
                Subtitle = "Atmospherics Distro — Gas Filter",
                PowerOk = true,
                Powered = scenario != FilterScenario.Idle,
                Connected = true,
                Scenario = (byte)scenario,
                FlowLitresPerSecond = 50,
                ThroughputLitresPerSecond = scenario == FilterScenario.Filtering ? 50f : 0f,
                SelectedGases = 0b1100,
                Gases = gases,
                Inlet = new GasLineReading { PressureKpa = 312.4f, Fractions = new[] { 0.208f, 0.761f, 0.016f, 0.012f } },
                OutletPressureKpa = 298.1f,
                FilterOutletPressureKpa = scenario == FilterScenario.Stalled ? 4500f : 1840f,
            });
        }
    }
}
