using FishNet.Serializing;

namespace SS3D.UI.MachineInterface
{
    public static class VentInterfaceSnapshotSerializer
    {
        public static void WriteVentInterfaceSnapshot(this Writer writer, VentInterfaceSnapshot snapshot)
        {
            writer.WriteInt32(snapshot.MachineObjectId);
            writer.WriteString(snapshot.InterfaceId);
            writer.WriteString(snapshot.Title);
            writer.WriteString(snapshot.ModelLabel);
            writer.WriteString(snapshot.DeviceTitle);
            writer.WriteString(snapshot.Subtitle);
            writer.WriteBoolean(snapshot.PowerOk);
            writer.WriteBoolean(snapshot.Powered);
            writer.WriteBoolean(snapshot.Connected);
            writer.WriteByte(snapshot.Scenario);
            writer.WriteBoolean(snapshot.AccessGranted);
            writer.WriteBoolean(snapshot.AccessScanning);
            writer.WriteBoolean(snapshot.AccessDenied);
            writer.WriteInt32(snapshot.TargetPressureKpa);
        }

        public static VentInterfaceSnapshot ReadVentInterfaceSnapshot(this Reader reader)
        {
            return new VentInterfaceSnapshot
            {
                MachineObjectId = reader.ReadInt32(),
                InterfaceId = reader.ReadString(),
                Title = reader.ReadString(),
                ModelLabel = reader.ReadString(),
                DeviceTitle = reader.ReadString(),
                Subtitle = reader.ReadString(),
                PowerOk = reader.ReadBoolean(),
                Powered = reader.ReadBoolean(),
                Connected = reader.ReadBoolean(),
                Scenario = reader.ReadByte(),
                AccessGranted = reader.ReadBoolean(),
                AccessScanning = reader.ReadBoolean(),
                AccessDenied = reader.ReadBoolean(),
                TargetPressureKpa = reader.ReadInt32(),
            };
        }
    }
}
