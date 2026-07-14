using System.Collections.Generic;

namespace SS3D.UI.MachineInterface
{
    public class ApcInterfaceViewModel : IMachineInterfaceViewModel, IAccessGatedInterfaceViewModel
    {
        public string Title { get; set; } = "APC · ENGINEERING BAY";

        public string ModelLabel { get; set; } = "APC-7 · area power controller";

        public string DeviceTitle { get; set; } = "APC-ENG-03";

        public string Subtitle { get; set; } = "Engineering Bay 3";

        public string ConnectionStatus { get; set; } = "WIRED · POWER NET · NODE E-12";

        public string HeaderReadout { get; set; } = "GRID FEED NOMINAL";

        public string FooterText { get; set; } = "SS3D Area Power Controller — Model APC-7";

        public bool ChassisPowerOk { get; set; } = true;

        public ApcPowerState State { get; set; } = ApcPowerState.Nominal;

        public string StatusHeadline { get; set; } = "ROOM POWERED";

        public string StatusExplanation { get; set; } = "Grid supply meets connected load.";

        public float GridInputKw { get; set; }

        public float LoadOutputKw { get; set; }

        public float BatteryCharge { get; set; } = 1f;

        public string BatteryStateText { get; set; } = "CHARGED";

        public string BatteryEtaText { get; set; } = "—";

        public bool LightingOn { get; set; } = true;

        public bool EquipmentOn { get; set; } = true;

        public bool EnvironmentOn { get; set; } = true;

        public float LightingLoadKw { get; set; }

        public float EquipmentLoadKw { get; set; }

        public float EnvironmentLoadKw { get; set; }

        public List<DiagnosticLine> Diagnostics { get; set; } = new();

        public bool AccessGranted { get; set; }

        public bool AccessScanning { get; set; }

        public bool AccessDenied { get; set; }

        public static ApcInterfaceViewModel CreateNominal()
        {
            return new ApcInterfaceViewModel
            {
                State = ApcPowerState.Nominal,
                StatusHeadline = "ROOM POWERED",
                StatusExplanation = "Grid supply meets connected load.",
                HeaderReadout = "GRID FEED NOMINAL",
                GridInputKw = 6.2f,
                LoadOutputKw = 4.8f,
                BatteryCharge = 0.98f,
                BatteryStateText = "CHARGED",
                LightingLoadKw = 0.6f,
                EquipmentLoadKw = 2.9f,
                EnvironmentLoadKw = 1.3f,
                Diagnostics = new List<DiagnosticLine>
                {
                    new("✓", "NO FAULTS DETECTED.", StatusTone.Neutral),
                },
            };
        }

        public static ApcInterfaceViewModel CreateOverload()
        {
            return new ApcInterfaceViewModel
            {
                State = ApcPowerState.Overload,
                StatusHeadline = "Running on Battery",
                StatusExplanation = "Grid supply below connected load.",
                HeaderReadout = "LOAD EXCEEDS SUPPLY",
                GridInputKw = 3.0f,
                LoadOutputKw = 5.4f,
                BatteryCharge = 0.41f,
                BatteryStateText = "DISCHARGING",
                LightingLoadKw = 0.6f,
                EquipmentLoadKw = 2.9f,
                EnvironmentLoadKw = 1.3f,
                Diagnostics = new List<DiagnosticLine>
                {
                    new("!", "Grid supply below connected load.", StatusTone.Warning),
                    new("→", "Shut off a system to reduce draw.", StatusTone.Info),
                },
            };
        }

        public static ApcInterfaceViewModel CreateCritical()
        {
            return new ApcInterfaceViewModel
            {
                State = ApcPowerState.Critical,
                StatusHeadline = "POWER FAILURE",
                StatusExplanation = "No grid input detected.",
                HeaderReadout = "NO GRID INPUT",
                ChassisPowerOk = false,
                GridInputKw = 0f,
                LoadOutputKw = 2.1f,
                BatteryCharge = 0.06f,
                BatteryStateText = "CRITICAL",
                LightingOn = false,
                EquipmentOn = false,
                EnvironmentOn = true,
                LightingLoadKw = 0f,
                EquipmentLoadKw = 0f,
                EnvironmentLoadKw = 2.1f,
                Diagnostics = new List<DiagnosticLine>
                {
                    new("✕", "No grid input detected. Check upstream cable / SMES.", StatusTone.Danger),
                    new("!", "Battery critical — equipment auto-disabled.", StatusTone.Danger),
                },
            };
        }
    }
}
