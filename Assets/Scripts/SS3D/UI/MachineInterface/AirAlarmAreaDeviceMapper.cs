using SS3D.Systems.Atmospherics.Pipes;

namespace SS3D.UI.MachineInterface
{
    public static class AirAlarmAreaDeviceMapper
    {
        public static AirAlarmConnectedDevice ToConnectedDevice(AtmosAreaPortRecord record)
        {
            return new AirAlarmConnectedDevice
            {
                Id = record.ObjectId.ToString(),
                Kind = record.Kind == AtmosAreaPortKind.Vent
                    ? AirAlarmConnectedDeviceKind.Vent
                    : AirAlarmConnectedDeviceKind.Scrubber,
                Name = record.Name,
                Powered = record.Powered,
                TargetKpa = record.TargetKpa,
                Filters = new System.Collections.Generic.Dictionary<string, bool>
                {
                    ["O2"] = record.FilterO2,
                    ["N2"] = record.FilterN2,
                    ["CO2"] = record.FilterCo2,
                    ["Plasma"] = record.FilterPlasma,
                    ["Toxins"] = record.FilterToxins,
                },
            };
        }
    }
}
