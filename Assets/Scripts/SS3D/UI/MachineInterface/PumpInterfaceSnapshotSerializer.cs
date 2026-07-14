using FishNet.Serializing;

namespace SS3D.UI.MachineInterface
{
    public static class GasPumpInterfaceSnapshotSerializer
    {
        public static void WriteGasPumpInterfaceSnapshot(this Writer writer, GasPumpInterfaceSnapshot snapshot)
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
            writer.WriteInt32(snapshot.TargetOutletPressureKpa);
            writer.WriteSingle(snapshot.InletPressureKpa);
            writer.WriteSingle(snapshot.OutletPressureKpa);
            writer.WriteSingle(snapshot.FlowMolesPerSecond);
        }

        public static GasPumpInterfaceSnapshot ReadGasPumpInterfaceSnapshot(this Reader reader)
        {
            return new GasPumpInterfaceSnapshot
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
                TargetOutletPressureKpa = reader.ReadInt32(),
                InletPressureKpa = reader.ReadSingle(),
                OutletPressureKpa = reader.ReadSingle(),
                FlowMolesPerSecond = reader.ReadSingle(),
            };
        }
    }
}
