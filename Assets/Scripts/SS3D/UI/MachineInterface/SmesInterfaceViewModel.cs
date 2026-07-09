using System.Collections.Generic;

namespace SS3D.UI.MachineInterface
{
    public class SmesInterfaceViewModel : IMachineInterfaceViewModel
    {
        public string Title { get; set; } = "SMES · ENERGY STORAGE";

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

        public string DiagnosisHint { get; set; } = string.Empty;

        public List<DiagnosticLine> Warnings { get; set; } = new();

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
                ExteriorInputWord = "AVAILABLE",
                ExteriorOutputWord = "ACTIVE",
                ConnectionStateText = "Grid link nominal — both connections healthy.",
            };
        }

        public static SmesInterfaceViewModel CreateDegraded()
        {
            return new SmesInterfaceViewModel
            {
                State = SmesPowerState.Degraded,
                StatusBadgeText = "DEGRADED",
                ExteriorStatusWord = "Running on Reserve",
                ChargePct = 0.54f,
                ChargeTrend = SmesChargeTrend.Draining,
                InputCurrentKw = 0f,
                OutputCurrentKw = 3.8f,
                InputEnabled = true,
                OutputEnabled = true,
                InputActive = false,
                OutputActive = true,
                ExteriorInputWord = "NO SIGNAL",
                ExteriorOutputWord = "ACTIVE",
                ConnectionStateText = "Input link down — output still served from storage.",
                DiagnosisHint = "The SMES is not the fault — check the upstream generator or grid cable feeding this unit.",
                Warnings = new List<DiagnosticLine>
                {
                    new("!", "No grid connection detected.", StatusTone.Danger),
                    new("!", "Battery discharge increasing.", StatusTone.Warning),
                },
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
                ExteriorInputWord = "AVAILABLE",
                ExteriorOutputWord = "OVERDRAWN",
                ConnectionStateText = "Distribution grid drawing more than input supplies.",
                DiagnosisHint = "The grid is overloaded, not the SMES — reduce distribution demand or bring another generator online.",
                Warnings = new List<DiagnosticLine>
                {
                    new("!", "Output exceeds sustainable generation.", StatusTone.Warning),
                    new("!", "Battery discharge increasing.", StatusTone.Warning),
                },
            };
        }

        public static SmesInterfaceViewModel CreateFault()
        {
            return new SmesInterfaceViewModel
            {
                State = SmesPowerState.Fault,
                StatusBadgeText = "FAULT",
                ExteriorStatusWord = "SMES Offline",
                ChargePct = 0.04f,
                ChargeTrend = SmesChargeTrend.Critical,
                InputCurrentKw = 0f,
                OutputCurrentKw = 0f,
                InputEnabled = false,
                OutputEnabled = false,
                InputActive = false,
                OutputActive = false,
                ExteriorInputWord = "DISABLED",
                ExteriorOutputWord = "DISABLED (auto-cutoff)",
                ConnectionStateText = "Both links down — unit in protective shutdown.",
                DiagnosisHint = "The SMES itself has faulted — restore input power, then let it cool before re-enabling output.",
                Warnings = new List<DiagnosticLine>
                {
                    new("X", "Cell bank overheating — output disabled.", StatusTone.Danger),
                    new("X", "Charge critical — connect input immediately.", StatusTone.Danger),
                },
            };
        }
    }
}
