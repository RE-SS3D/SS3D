using FishNet.Serializing;

namespace SS3D.UI.MachineInterface
{
    public static class AirAlarmInterfaceSnapshotSerializer
    {
        public static void WriteAirAlarmInterfaceSnapshot(this Writer writer, AirAlarmInterfaceSnapshot snapshot)
        {
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
        }

        public static AirAlarmInterfaceSnapshot ReadAirAlarmInterfaceSnapshot(this Reader reader)
        {
            return new AirAlarmInterfaceSnapshot
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
            };
        }
    }
}
