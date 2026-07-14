using System.Collections.Generic;

namespace SS3D.UI.MachineInterface
{
    public enum ScrubberScenario
    {
        Filtering = 0,
        Idle = 1,
        Overloaded = 2,
        Fault = 3,
    }

    public class ScrubberInterfaceViewModel : IMachineInterfaceViewModel
    {
        public string Title { get; set; } = "SCRUBBER · ATMOSPHERICS";

        public string ModelLabel { get; set; } = "SCB-09 · atmospheric scrubber unit";

        public string DeviceTitle { get; set; } = "SCB-09";

        public string Subtitle { get; set; } = "Engineering Bay 3 — Atmospheric Scrubber";

        public string ConnectionStatus { get; set; } = "WIRED · PIPE NET · PORT S2";

        public string ThroughputText { get; set; } = "48 L/s";

        public string FooterText { get; set; } = "SS3D Atmospherics — Scrubber Unit Model SCB-09";

        public bool ChassisPowerOk { get; set; } = true;

        public ScrubberScenario Scenario { get; set; } = ScrubberScenario.Filtering;

        public string StatusBadgeText { get; set; } = "FILTERING";

        public string StatusHeadline { get; set; } = "SCRUBBING NORMALLY";

        public string StatusSubline { get; set; } = "Room mix within safe tolerance.";

        public List<AirAlarmGasReadout> GasReadouts { get; set; } = new();

        public bool AccessGranted { get; set; }

        public bool AccessScanning { get; set; }

        public string IdReaderSubline { get; set; } =
            "Read an ID to unlock gas filter selection and flow rate";

        public bool Powered { get; set; } = true;

        public Dictionary<string, bool> Filters { get; set; } = new()
        {
            ["O2"] = true,
            ["N2"] = true,
            ["CO2"] = true,
            ["Plasma"] = false,
            ["Toxins"] = true,
        };

        public int FlowRate { get; set; } = 5;

        public static ScrubberInterfaceViewModel CreateFiltering()
        {
            return new ScrubberInterfaceViewModel
            {
                Scenario = ScrubberScenario.Filtering,
                StatusBadgeText = "FILTERING",
                StatusHeadline = "SCRUBBING NORMALLY",
                StatusSubline = "Room mix within safe tolerance.",
                ThroughputText = "48 L/s",
                GasReadouts = new List<AirAlarmGasReadout>
                {
                    new() { Label = "O2", Percent = 21f, BarTone = StatusTone.Success },
                    new() { Label = "N2", Percent = 78f, BarTone = StatusTone.Info },
                    new() { Label = "CO2", Percent = 0.5f, BarTone = StatusTone.Info },
                    new() { Label = "Plasma", Percent = 0f, BarTone = StatusTone.Info },
                    new() { Label = "Toxins", Percent = 0f, BarTone = StatusTone.Info },
                },
            };
        }

        public static ScrubberInterfaceViewModel CreateIdle()
        {
            ScrubberInterfaceViewModel model = CreateFiltering();
            model.Scenario = ScrubberScenario.Idle;
            model.StatusBadgeText = "IDLE";
            model.StatusHeadline = "SCRUBBER IDLE";
            model.StatusSubline = "No filtering active — room mix stable.";
            model.ThroughputText = "0 L/s";
            model.Powered = false;
            return model;
        }

        public static ScrubberInterfaceViewModel CreateOverloaded()
        {
            ScrubberInterfaceViewModel model = CreateFiltering();
            model.Scenario = ScrubberScenario.Overloaded;
            model.StatusBadgeText = "OVERLOADED";
            model.StatusHeadline = "CONTAMINANTS RISING";
            model.StatusSubline = "Intake exceeds scrub capacity.";
            model.ThroughputText = "61 L/s";
            model.GasReadouts = new List<AirAlarmGasReadout>
            {
                new() { Label = "O2", Percent = 18.4f, BarTone = StatusTone.Warning },
                new() { Label = "N2", Percent = 73.2f, BarTone = StatusTone.Info },
                new() { Label = "CO2", Percent = 3.9f, ValueTone = StatusTone.Danger, BarTone = StatusTone.Warning },
                new() { Label = "Plasma", Percent = 0.6f, BarTone = StatusTone.Info },
                new() { Label = "Toxins", Percent = 1.2f, BarTone = StatusTone.Info },
            };
            return model;
        }

        public static ScrubberInterfaceViewModel CreateFault()
        {
            ScrubberInterfaceViewModel model = CreateFiltering();
            model.Scenario = ScrubberScenario.Fault;
            model.ChassisPowerOk = false;
            model.StatusBadgeText = "FAULT";
            model.StatusHeadline = "SCRUBBER FAULT";
            model.StatusSubline = "Filter jammed — no throughput.";
            model.ThroughputText = "0 L/s";
            model.Powered = false;
            return model;
        }
    }
}
