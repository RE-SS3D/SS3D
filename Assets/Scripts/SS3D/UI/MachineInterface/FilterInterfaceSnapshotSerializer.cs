using FishNet.Serializing;

namespace SS3D.UI.MachineInterface
{
    public static class FilterInterfaceSnapshotSerializer
    {
        public static void WriteFilterInterfaceSnapshot(this Writer writer, FilterInterfaceSnapshot snapshot)
        {
            writer.WriteInt32(snapshot.MachineObjectId);
            writer.WriteString(snapshot.InterfaceId);
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
            writer.WriteInt32(snapshot.FlowLitresPerSecond);
            writer.WriteSingle(snapshot.ThroughputLitresPerSecond);
            writer.WriteUInt64(snapshot.SelectedGases);
            GasPanelSnapshotSerializer.WriteGasRows(writer, snapshot.Gases);
            GasPanelSnapshotSerializer.WriteLine(writer, snapshot.Inlet);
            writer.WriteSingle(snapshot.OutletPressureKpa);
            writer.WriteSingle(snapshot.FilterOutletPressureKpa);
        }

        public static FilterInterfaceSnapshot ReadFilterInterfaceSnapshot(this Reader reader)
        {
            return new FilterInterfaceSnapshot
            {
                MachineObjectId = reader.ReadInt32(),
                InterfaceId = reader.ReadString(),
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
                FlowLitresPerSecond = reader.ReadInt32(),
                ThroughputLitresPerSecond = reader.ReadSingle(),
                SelectedGases = reader.ReadUInt64(),
                Gases = GasPanelSnapshotSerializer.ReadGasRows(reader),
                Inlet = GasPanelSnapshotSerializer.ReadLine(reader),
                OutletPressureKpa = reader.ReadSingle(),
                FilterOutletPressureKpa = reader.ReadSingle(),
            };
        }
    }
}
