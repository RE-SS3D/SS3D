using FishNet.Serializing;
using System.Collections.Generic;

namespace SS3D.UI.MachineInterface
{
    public static class AirAlarmInterfaceSnapshotSerializer
    {
        public static void WriteAirAlarmInterfaceSnapshot(this Writer writer, AirAlarmInterfaceSnapshot snapshot)
        {
            List<AirAlarmDeviceSnapshot> devices = snapshot.ConnectedDevices ?? new List<AirAlarmDeviceSnapshot>();

            writer.WriteInt32(snapshot.MachineObjectId);
            writer.WriteString(snapshot.InterfaceId);
            writer.WriteString(snapshot.Title);
            writer.WriteString(snapshot.ModelLabel);
            writer.WriteString(snapshot.DeviceTitle);
            writer.WriteString(snapshot.Subtitle);
            writer.WriteBoolean(snapshot.PowerOk);
            writer.WriteByte(snapshot.Scenario);
            writer.WriteBoolean(snapshot.AccessGranted);
            writer.WriteBoolean(snapshot.AccessScanning);
            writer.WriteSingle(snapshot.PressureKpa);
            writer.WriteSingle(snapshot.OxygenFraction);
            writer.WriteSingle(snapshot.CarbonDioxideFraction);
            writer.WriteSingle(snapshot.NitrogenFraction);
            writer.WriteSingle(snapshot.PlasmaFraction);
            writer.WriteSingle(snapshot.TemperatureKelvin);
            writer.WriteBoolean(snapshot.HasSample);
            writer.WriteByte(snapshot.ActiveMode);
            writer.WriteString(snapshot.SelectedDeviceId ?? string.Empty);
            writer.WriteUInt16((ushort)devices.Count);

            foreach (AirAlarmDeviceSnapshot device in devices)
            {
                WriteDevice(writer, device);
            }
        }

        public static AirAlarmInterfaceSnapshot ReadAirAlarmInterfaceSnapshot(this Reader reader)
        {
            AirAlarmInterfaceSnapshot snapshot = new()
            {
                MachineObjectId = reader.ReadInt32(),
                InterfaceId = reader.ReadString(),
                Title = reader.ReadString(),
                ModelLabel = reader.ReadString(),
                DeviceTitle = reader.ReadString(),
                Subtitle = reader.ReadString(),
                PowerOk = reader.ReadBoolean(),
                Scenario = reader.ReadByte(),
                AccessGranted = reader.ReadBoolean(),
                AccessScanning = reader.ReadBoolean(),
                PressureKpa = reader.ReadSingle(),
                OxygenFraction = reader.ReadSingle(),
                CarbonDioxideFraction = reader.ReadSingle(),
                NitrogenFraction = reader.ReadSingle(),
                PlasmaFraction = reader.ReadSingle(),
                TemperatureKelvin = reader.ReadSingle(),
                HasSample = reader.ReadBoolean(),
                ActiveMode = reader.ReadByte(),
                SelectedDeviceId = reader.ReadString(),
            };

            ushort deviceCount = reader.ReadUInt16();
            snapshot.ConnectedDevices = new List<AirAlarmDeviceSnapshot>(deviceCount);
            for (int i = 0; i < deviceCount; i++)
            {
                snapshot.ConnectedDevices.Add(ReadDevice(reader));
            }

            return snapshot;
        }

        public static AirAlarmDeviceSnapshot ToDeviceSnapshot(AirAlarmConnectedDevice device)
        {
            device.Filters ??= new Dictionary<string, bool>();

            return new AirAlarmDeviceSnapshot
            {
                Id = device.Id,
                Name = device.Name,
                Kind = (byte)device.Kind,
                Powered = device.Powered,
                TargetKpa = device.TargetKpa,
                FilterO2 = device.Filters.TryGetValue("O2", out bool o2) && o2,
                FilterN2 = device.Filters.TryGetValue("N2", out bool n2) && n2,
                FilterCo2 = device.Filters.TryGetValue("CO2", out bool co2) && co2,
                FilterPlasma = device.Filters.TryGetValue("Plasma", out bool plasma) && plasma,
                FilterToxins = device.Filters.TryGetValue("Toxins", out bool toxins) && toxins,
            };
        }

        public static AirAlarmConnectedDevice ToConnectedDevice(AirAlarmDeviceSnapshot snapshot)
        {
            return new AirAlarmConnectedDevice
            {
                Id = snapshot.Id,
                Kind = (AirAlarmConnectedDeviceKind)snapshot.Kind,
                Name = string.IsNullOrEmpty(snapshot.Name) ? snapshot.Id : snapshot.Name,
                Powered = snapshot.Powered,
                TargetKpa = snapshot.TargetKpa,
                Filters = new Dictionary<string, bool>
                {
                    ["O2"] = snapshot.FilterO2,
                    ["N2"] = snapshot.FilterN2,
                    ["CO2"] = snapshot.FilterCo2,
                    ["Plasma"] = snapshot.FilterPlasma,
                    ["Toxins"] = snapshot.FilterToxins,
                },
            };
        }

        private static void WriteDevice(Writer writer, AirAlarmDeviceSnapshot device)
        {
            writer.WriteString(device.Id ?? string.Empty);
            writer.WriteString(device.Name ?? string.Empty);
            writer.WriteByte(device.Kind);
            writer.WriteBoolean(device.Powered);
            writer.WriteSingle(device.TargetKpa);
            writer.WriteBoolean(device.FilterO2);
            writer.WriteBoolean(device.FilterN2);
            writer.WriteBoolean(device.FilterCo2);
            writer.WriteBoolean(device.FilterPlasma);
            writer.WriteBoolean(device.FilterToxins);
        }

        private static AirAlarmDeviceSnapshot ReadDevice(Reader reader)
        {
            return new AirAlarmDeviceSnapshot
            {
                Id = reader.ReadString(),
                Name = reader.ReadString(),
                Kind = reader.ReadByte(),
                Powered = reader.ReadBoolean(),
                TargetKpa = reader.ReadSingle(),
                FilterO2 = reader.ReadBoolean(),
                FilterN2 = reader.ReadBoolean(),
                FilterCo2 = reader.ReadBoolean(),
                FilterPlasma = reader.ReadBoolean(),
                FilterToxins = reader.ReadBoolean(),
            };
        }
    }
}
