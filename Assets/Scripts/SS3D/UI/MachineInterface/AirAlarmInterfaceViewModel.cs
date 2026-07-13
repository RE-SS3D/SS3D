using System.Collections.Generic;

namespace SS3D.UI.MachineInterface
{
    public enum AirAlarmScenario
    {
        Normal = 0,
        Warning = 1,
        Danger = 2,
    }

    public enum AirAlarmPresetMode
    {
        Filtering = 0,
        Panic = 1,
        Fill = 2,
        Off = 3,
    }

    public enum AirAlarmConnectedDeviceKind
    {
        Vent = 0,
        Scrubber = 1,
    }

    public class AirAlarmGasReadout
    {
        public string Label;

        public float Percent;

        public StatusTone ValueTone;

        public StatusTone BarTone;
    }

    public class AirAlarmConnectedDevice
    {
        public string Id;

        public AirAlarmConnectedDeviceKind Kind;

        public string Name;

        public bool Powered;

        public float TargetKpa;

        public Dictionary<string, bool> Filters = new();
    }

    public class AirAlarmInterfaceViewModel : IMachineInterfaceViewModel
    {
        public string Title { get; set; } = "AIR ALARM · ATMOSPHERICS";

        public string ModelLabel { get; set; } = "AA-07 · atmospheric alarm control unit";

        public string DeviceTitle { get; set; } = "AA-07";

        public string Subtitle { get; set; } = "Engineering Bay 3 — Air Alarm";

        public string ConnectionStatus { get; set; } = "WIRED · ATMOS NET · ZONE ENGINEERING BAY 3";

        public string DeviceCountText { get; set; } = "4 devices linked";

        public string FooterText { get; set; } = "SS3D Atmospherics — Air Alarm Unit Model AA-07";

        public bool ChassisPowerOk { get; set; } = true;

        public AirAlarmScenario Scenario { get; set; } = AirAlarmScenario.Normal;

        public string StatusBadgeText { get; set; } = "NORMAL";

        public string StatusHeadline { get; set; } = "ATMOSPHERE NOMINAL";

        public string AlarmSubline { get; set; } = "All readings within safe tolerance.";

        public string PressureText { get; set; } = "101.3 kPa";

        public StatusTone PressureTone { get; set; } = StatusTone.Success;

        public string TemperatureText { get; set; } = "20.9°C";

        public StatusTone TemperatureTone { get; set; } = StatusTone.Success;

        public List<AirAlarmGasReadout> GasReadouts { get; set; } = new();

        public bool AccessGranted { get; set; }

        public bool AccessScanning { get; set; }

        public string IdReaderSubline { get; set; } =
            "Read an ID to unlock preset modes and device control";

        public AirAlarmPresetMode ActiveMode { get; set; } = AirAlarmPresetMode.Filtering;

        public List<AirAlarmConnectedDevice> ConnectedDevices { get; set; } = new();

        public string SelectedDeviceId { get; set; }

        public static AirAlarmInterfaceViewModel CreateNormal()
        {
            return new AirAlarmInterfaceViewModel
            {
                Scenario = AirAlarmScenario.Normal,
                StatusBadgeText = "NORMAL",
                StatusHeadline = "ATMOSPHERE NOMINAL",
                AlarmSubline = "All readings within safe tolerance.",
                PressureText = "101.3 kPa",
                PressureTone = StatusTone.Success,
                TemperatureText = "20.9°C",
                TemperatureTone = StatusTone.Success,
                GasReadouts = new List<AirAlarmGasReadout>
                {
                    new() { Label = "O2", Percent = 21f, ValueTone = StatusTone.Success, BarTone = StatusTone.Success },
                    new() { Label = "N2", Percent = 78f, BarTone = StatusTone.Info },
                    new() { Label = "CO2", Percent = 0.5f, BarTone = StatusTone.Info },
                    new() { Label = "Plasma", Percent = 0f, BarTone = StatusTone.Info },
                },
                ConnectedDevices = CreateDefaultDevices(),
            };
        }

        public static AirAlarmInterfaceViewModel CreateWarning()
        {
            AirAlarmInterfaceViewModel model = CreateNormal();
            model.Scenario = AirAlarmScenario.Warning;
            model.StatusBadgeText = "WARNING";
            model.StatusHeadline = "CO2 RISING";
            model.AlarmSubline = "CO2 above threshold — scrubbers advised.";
            model.PressureText = "96.4 kPa";
            model.PressureTone = StatusTone.Warning;
            model.TemperatureText = "24.1°C";
            model.TemperatureTone = StatusTone.Warning;
            model.GasReadouts = new List<AirAlarmGasReadout>
            {
                new() { Label = "O2", Percent = 19.2f, ValueTone = StatusTone.Warning, BarTone = StatusTone.Warning },
                new() { Label = "N2", Percent = 74.6f, BarTone = StatusTone.Info },
                new() { Label = "CO2", Percent = 3.8f, ValueTone = StatusTone.Danger, BarTone = StatusTone.Warning },
                new() { Label = "Plasma", Percent = 0.2f, BarTone = StatusTone.Info },
            };
            return model;
        }

        public static AirAlarmInterfaceViewModel CreateDanger()
        {
            AirAlarmInterfaceViewModel model = CreateNormal();
            model.Scenario = AirAlarmScenario.Danger;
            model.ChassisPowerOk = false;
            model.StatusBadgeText = "DANGER";
            model.StatusHeadline = "PLASMA FIRE DETECTED";
            model.AlarmSubline = "Evacuate — panic siphon advised.";
            model.PressureText = "42.0 kPa";
            model.PressureTone = StatusTone.Danger;
            model.TemperatureText = "61.7°C";
            model.TemperatureTone = StatusTone.Danger;
            model.GasReadouts = new List<AirAlarmGasReadout>
            {
                new() { Label = "O2", Percent = 11.4f, ValueTone = StatusTone.Danger, BarTone = StatusTone.Warning },
                new() { Label = "N2", Percent = 58.2f, BarTone = StatusTone.Info },
                new() { Label = "CO2", Percent = 6.1f, ValueTone = StatusTone.Danger, BarTone = StatusTone.Warning },
                new() { Label = "Plasma", Percent = 9.4f, ValueTone = StatusTone.Danger, BarTone = StatusTone.Danger },
            };
            return model;
        }

        private static List<AirAlarmConnectedDevice> CreateDefaultDevices()
        {
            return new List<AirAlarmConnectedDevice>
            {
                new()
                {
                    Id = "vent-n",
                    Kind = AirAlarmConnectedDeviceKind.Vent,
                    Name = "Vent — North Wall",
                    Powered = true,
                    TargetKpa = 101f,
                },
                new()
                {
                    Id = "vent-s",
                    Kind = AirAlarmConnectedDeviceKind.Vent,
                    Name = "Vent — South Wall",
                    Powered = true,
                    TargetKpa = 101f,
                },
                new()
                {
                    Id = "scrub-n",
                    Kind = AirAlarmConnectedDeviceKind.Scrubber,
                    Name = "Scrubber — North Wall",
                    Powered = true,
                    Filters = new Dictionary<string, bool>
                    {
                        ["O2"] = true,
                        ["N2"] = true,
                        ["CO2"] = true,
                        ["Plasma"] = false,
                        ["Toxins"] = true,
                    },
                },
                new()
                {
                    Id = "scrub-s",
                    Kind = AirAlarmConnectedDeviceKind.Scrubber,
                    Name = "Scrubber — South Wall",
                    Powered = false,
                    Filters = new Dictionary<string, bool>
                    {
                        ["O2"] = true,
                        ["N2"] = true,
                        ["CO2"] = true,
                        ["Plasma"] = false,
                        ["Toxins"] = false,
                    },
                },
            };
        }
    }
}
