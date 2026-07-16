namespace SS3D.UI.MachineInterface
{
    public enum VentScenario
    {
        Idle = 0,
        Pressurizing = 1,
        Depressurizing = 2,
        Fault = 3,
    }

    public class VentInterfaceViewModel : IMachineInterfaceViewModel, IAccessGatedInterfaceViewModel
    {
        public string Title { get; set; } = "VENT · ATMOSPHERICS";

        public string ModelLabel { get; set; } = "VT-14 · atmospheric vent unit";

        public string DeviceTitle { get; set; } = "VT-14";

        public string Subtitle { get; set; } = "Engineering Bay 3 — Atmospheric Vent";

        public string ConnectionStatus { get; set; } = "WIRED · PIPE NET · PORT V3";

        public string FooterText { get; set; } = "SS3D Atmospherics — Vent Unit Model VT-14";

        public bool ChassisPowerOk { get; set; } = true;

        public VentScenario Scenario { get; set; } = VentScenario.Idle;

        public string StatusBadgeText { get; set; } = "IDLE";

        public string StatusHeadline { get; set; } = "VENT IDLE";

        public string StatusSubline { get; set; } = "Pressure equalized.";

        public string ExternalPressureText { get; set; } = "101.3 kPa";

        public StatusTone ExternalTone { get; set; } = StatusTone.Success;

        public string InternalPressureText { get; set; } = "101.1 kPa";

        public string FlowGlyph { get; set; } = "·";

        public StatusTone FlowTone { get; set; } = StatusTone.Neutral;

        public bool AccessGranted { get; set; }

        public bool AccessScanning { get; set; }

        public bool AccessDenied { get; set; }

        public string IdReaderSubline { get; set; } =
            "Read an ID to unlock power and target pressure control";

        public bool Powered { get; set; } = true;

        public int TargetPressureKpa { get; set; } = 101;

        public static VentInterfaceViewModel CreateIdle()
        {
            return new VentInterfaceViewModel
            {
                Scenario = VentScenario.Idle,
                StatusBadgeText = "IDLE",
                StatusHeadline = "VENT IDLE",
                StatusSubline = "Pressure equalized.",
                ExternalPressureText = "101.3 kPa",
                InternalPressureText = "101.1 kPa",
                FlowGlyph = "·",
                FlowTone = StatusTone.Neutral,
            };
        }

        public static VentInterfaceViewModel CreatePressurizing()
        {
            return new VentInterfaceViewModel
            {
                Scenario = VentScenario.Pressurizing,
                StatusBadgeText = "PRESSURIZING",
                StatusHeadline = "REPRESSURIZING",
                StatusSubline = "Pulling from pipe network.",
                ExternalPressureText = "64.2 kPa",
                ExternalTone = StatusTone.Warning,
                InternalPressureText = "148.6 kPa",
                FlowGlyph = "→",
                FlowTone = StatusTone.Info,
            };
        }

        public static VentInterfaceViewModel CreateDepressurizing()
        {
            return new VentInterfaceViewModel
            {
                Scenario = VentScenario.Depressurizing,
                StatusBadgeText = "SIPHONING",
                StatusHeadline = "VENTING ROOM",
                StatusSubline = "Pushing to pipe network.",
                ExternalPressureText = "132.8 kPa",
                ExternalTone = StatusTone.Warning,
                InternalPressureText = "89.4 kPa",
                FlowGlyph = "←",
                FlowTone = StatusTone.Warning,
            };
        }

        public static VentInterfaceViewModel CreateFault()
        {
            return new VentInterfaceViewModel
            {
                Scenario = VentScenario.Fault,
                ChassisPowerOk = false,
                StatusBadgeText = "FAULT",
                StatusHeadline = "VENT FAULT",
                StatusSubline = "Valve unresponsive.",
                ExternalPressureText = "8.1 kPa",
                ExternalTone = StatusTone.Danger,
                InternalPressureText = "0.0 kPa",
                FlowGlyph = "✕",
                FlowTone = StatusTone.Danger,
                Powered = false,
            };
        }
    }
}
