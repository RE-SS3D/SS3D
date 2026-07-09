using System.Collections.Generic;

namespace SS3D.UI.MachineInterface
{
    public class ApcInterfaceViewModel : IMachineInterfaceViewModel
    {
        public string Title { get; set; } = "APC · ENGINEERING BAY";

        public ApcPowerState State { get; set; } = ApcPowerState.Nominal;

        public string StatusHeadline { get; set; } = "POWER NOMINAL";

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

        public static ApcInterfaceViewModel CreateNominal()
        {
            return new ApcInterfaceViewModel
            {
                State = ApcPowerState.Nominal,
                StatusHeadline = "POWER NOMINAL",
                StatusExplanation = "Grid supply meets connected load.",
                GridInputKw = 12.4f,
                LoadOutputKw = 9.8f,
                BatteryCharge = 0.92f,
                BatteryStateText = "CHARGED",
                BatteryEtaText = "FULL",
                LightingLoadKw = 2.1f,
                EquipmentLoadKw = 5.4f,
                EnvironmentLoadKw = 2.3f,
                Diagnostics = new List<DiagnosticLine>
                {
                    new(">", "External power available.", StatusTone.Success),
                    new(">", "Cell charge above threshold.", StatusTone.Success),
                    new(">", "All channels enabled.", StatusTone.Info),
                },
            };
        }

        public static ApcInterfaceViewModel CreateOverload()
        {
            return new ApcInterfaceViewModel
            {
                State = ApcPowerState.Overload,
                StatusHeadline = "GRID OVERLOAD",
                StatusExplanation = "Connected load exceeds grid supply. Battery discharging.",
                GridInputKw = 6.2f,
                LoadOutputKw = 11.5f,
                BatteryCharge = 0.48f,
                BatteryStateText = "DISCHARGING",
                BatteryEtaText = "~4 MIN",
                LightingLoadKw = 2.1f,
                EquipmentLoadKw = 6.8f,
                EnvironmentLoadKw = 2.6f,
                Diagnostics = new List<DiagnosticLine>
                {
                    new("!", "Grid supply below connected load.", StatusTone.Warning),
                    new(">", "Cell compensating deficit.", StatusTone.Warning),
                    new(">", "Consider shedding non-critical channels.", StatusTone.Info),
                },
            };
        }

        public static ApcInterfaceViewModel CreateCritical()
        {
            return new ApcInterfaceViewModel
            {
                State = ApcPowerState.Critical,
                StatusHeadline = "POWER CRITICAL",
                StatusExplanation = "Cell depleted. Load shedding imminent.",
                GridInputKw = 0f,
                LoadOutputKw = 8.2f,
                BatteryCharge = 0.06f,
                BatteryStateText = "CRITICAL",
                BatteryEtaText = "< 1 MIN",
                LightingOn = false,
                EquipmentOn = true,
                EnvironmentOn = false,
                LightingLoadKw = 0f,
                EquipmentLoadKw = 8.2f,
                EnvironmentLoadKw = 0f,
                Diagnostics = new List<DiagnosticLine>
                {
                    new("X", "No external power detected.", StatusTone.Danger),
                    new("!", "Cell charge critical.", StatusTone.Danger),
                    new(">", "Auto-shed active on Lighting, Environment.", StatusTone.Warning),
                },
            };
        }
    }
}
