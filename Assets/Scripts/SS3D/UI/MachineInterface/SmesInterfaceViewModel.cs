namespace SS3D.UI.MachineInterface
{
    public class SmesInterfaceViewModel : IMachineInterfaceViewModel, IAccessGatedInterfaceViewModel
    {
        public string Title { get; set; } = "SMES · ENERGY STORAGE";

        public string ModelLabel { get; set; } = "SMES-02 · magnetic energy storage bank";

        public string DeviceTitle { get; set; } = "SMES-02";

        public string Subtitle { get; set; } = "Engineering — Substation Storage Bank";

        public string ConnectionStatus { get; set; } = "WIRED · POWER NET · PORT J1";

        public string FooterText { get; set; } = "SS3D Superconducting Magnetic Energy Storage — Model SMES-02";

        public bool ChassisPowerOk { get; set; } = true;

        public SmesPowerState State { get; set; } = SmesPowerState.Nominal;

        public string StatusBadgeText { get; set; } = "ONLINE";

        public string ExteriorStatusWord { get; set; } = "SMES Online";

        public float ChargePct { get; set; } = 1f;

        public SmesChargeTrend ChargeTrend { get; set; } = SmesChargeTrend.Steady;

        public string ExteriorInputWord { get; set; } = "AVAILABLE";

        public string ExteriorOutputWord { get; set; } = "ACTIVE";

        public string ConnectionStateText { get; set; } = "Grid link nominal — both connections healthy.";

        public float InputCurrentKw { get; set; }

        public float OutputCurrentKw { get; set; }

        public float InputMaxKw { get; set; } = 10f;

        public float OutputMaxKw { get; set; } = 10f;

        public bool InputEnabled { get; set; } = true;

        public bool OutputEnabled { get; set; } = true;

        public bool InputActive { get; set; }

        public bool OutputActive { get; set; }

        public bool AccessGranted { get; set; }

        public bool AccessScanning { get; set; }

        public static SmesInterfaceViewModel CreateNominal()
        {
            return new SmesInterfaceViewModel
            {
                State = SmesPowerState.Nominal,
                StatusBadgeText = "ONLINE",
                ExteriorStatusWord = "SMES Online",
                ChargePct = 0.72f,
                ChargeTrend = SmesChargeTrend.Steady,
                InputCurrentKw = 3.2f,
                OutputCurrentKw = 4.1f,
                InputMaxKw = 10f,
                OutputMaxKw = 10f,
                InputEnabled = true,
                OutputEnabled = true,
                InputActive = true,
                OutputActive = true,
                ConnectionStateText = "Grid link nominal — both connections healthy.",
            };
        }

        public static SmesInterfaceViewModel CreateDegraded()
        {
            return new SmesInterfaceViewModel
            {
                State = SmesPowerState.Degraded,
                StatusBadgeText = "DEGRADED",
                ExteriorStatusWord = "On Reserve",
                ChargePct = 0.54f,
                ChargeTrend = SmesChargeTrend.Draining,
                InputCurrentKw = 0f,
                OutputCurrentKw = 3.8f,
                InputEnabled = true,
                OutputEnabled = true,
                InputActive = false,
                OutputActive = true,
                ConnectionStateText = "Input down — serving from storage.",
            };
        }

        public static SmesInterfaceViewModel CreateOverload()
        {
            return new SmesInterfaceViewModel
            {
                State = SmesPowerState.Overload,
                StatusBadgeText = "OVERLOAD",
                ExteriorStatusWord = "Overload Risk",
                ChargePct = 0.38f,
                ChargeTrend = SmesChargeTrend.DrainingFast,
                InputCurrentKw = 2.4f,
                OutputCurrentKw = 5.8f,
                InputEnabled = true,
                OutputEnabled = true,
                InputActive = true,
                OutputActive = true,
                ConnectionStateText = "Distribution grid drawing more than input supplies.",
            };
        }

        public static SmesInterfaceViewModel CreateFault()
        {
            return new SmesInterfaceViewModel
            {
                State = SmesPowerState.Fault,
                StatusBadgeText = "FAULT",
                ExteriorStatusWord = "SMES Offline",
                ChassisPowerOk = false,
                ChargePct = 0.04f,
                ChargeTrend = SmesChargeTrend.Critical,
                InputCurrentKw = 0f,
                OutputCurrentKw = 0f,
                InputEnabled = false,
                OutputEnabled = false,
                InputActive = false,
                OutputActive = false,
                ConnectionStateText = "Both links down — unit in protective shutdown.",
            };
        }
    }
}
