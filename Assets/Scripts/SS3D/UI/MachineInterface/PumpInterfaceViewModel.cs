namespace SS3D.UI.MachineInterface
{
    public enum PumpScenario
    {
        Idle = 0,
        Pumping = 1,
        Starved = 2,
        Fault = 3,
    }

    public sealed class PumpInterfaceViewModel : IMachineInterfaceViewModel
    {
        public string Title { get; set; } = "PUMP · ATMOSPHERICS";

        public string ModelLabel { get; set; } = "PMP-22 · pipe pump unit";

        public string DeviceTitle { get; set; } = "PMP-22";

        public string Subtitle { get; set; } = "Engineering Bay 3 — Pipe Pump";

        public string ConnectionStatus { get; set; } = "WIRED · PIPE NET · PORT P1";

        public string FooterText { get; set; } = "SS3D Atmospherics — Pump Unit Model PMP-22";

        public bool ChassisPowerOk { get; set; } = true;

        public PumpScenario Scenario { get; set; } = PumpScenario.Idle;

        public string StatusBadgeText { get; set; } = "IDLE";

        public string StatusHeadline { get; set; } = "Pump Idle";

        public string StatusSubline { get; set; } = "Powered off — no throughput.";

        public string InletPressureText { get; set; } = "101.3 kPa";

        public StatusTone InletTone { get; set; } = StatusTone.Neutral;

        public string OutletPressureText { get; set; } = "101.3 kPa";

        public StatusTone OutletTone { get; set; } = StatusTone.Neutral;

        public string FlowGlyph { get; set; } = "·";

        public StatusTone FlowTone { get; set; } = StatusTone.Neutral;

        public string FlowStatusText { get; set; } = "No flow";

        public StatusTone FlowStatusTone { get; set; } = StatusTone.Neutral;

        public bool AccessGranted { get; set; }

        public bool AccessScanning { get; set; }

        public string IdReaderSubline { get; set; } =
            "Read an ID to unlock power and target pressure control";

        public bool Powered { get; set; } = true;

        public int TargetOutletPressureKpa { get; set; } = 4500;

        public static PumpInterfaceViewModel CreateIdle()
        {
            return new PumpInterfaceViewModel
            {
                Scenario = PumpScenario.Idle,
                StatusBadgeText = "IDLE",
                StatusHeadline = "Pump Idle",
                StatusSubline = "Powered off — no throughput.",
                InletPressureText = "101.3 kPa",
                OutletPressureText = "101.3 kPa",
                FlowGlyph = "·",
                FlowStatusText = "No flow",
            };
        }

        public static PumpInterfaceViewModel CreatePumping()
        {
            return new PumpInterfaceViewModel
            {
                Scenario = PumpScenario.Pumping,
                StatusBadgeText = "PUMPING",
                StatusHeadline = "Pumping Normally",
                StatusSubline = "Outlet pressure within target range.",
                InletPressureText = "101.3 kPa",
                OutletPressureText = "4487.6 kPa",
                FlowGlyph = "→",
                FlowTone = StatusTone.Success,
                FlowStatusText = "Flowing — 12.4 L/s",
                FlowStatusTone = StatusTone.Success,
            };
        }

        public static PumpInterfaceViewModel CreateStarved()
        {
            return new PumpInterfaceViewModel
            {
                Scenario = PumpScenario.Starved,
                StatusBadgeText = "STARVED",
                StatusHeadline = "Inlet Starved",
                StatusSubline = "Insufficient inlet supply to reach target.",
                InletPressureText = "4.8 kPa",
                InletTone = StatusTone.Warning,
                OutletPressureText = "612.0 kPa",
                FlowGlyph = "→",
                FlowTone = StatusTone.Warning,
                FlowStatusText = "Restricted — 1.1 L/s",
                FlowStatusTone = StatusTone.Warning,
            };
        }

        public static PumpInterfaceViewModel CreateFault()
        {
            return new PumpInterfaceViewModel
            {
                Scenario = PumpScenario.Fault,
                ChassisPowerOk = false,
                StatusBadgeText = "FAULT",
                StatusHeadline = "Overpressure",
                StatusSubline = "Relief valve unresponsive — outlet exceeds target.",
                InletPressureText = "101.3 kPa",
                OutletPressureText = "6218.4 kPa",
                OutletTone = StatusTone.Danger,
                FlowGlyph = "✕",
                FlowTone = StatusTone.Danger,
                FlowStatusText = "Uncontrolled — 18.7 L/s",
                FlowStatusTone = StatusTone.Danger,
                Powered = false,
            };
        }
    }
}
