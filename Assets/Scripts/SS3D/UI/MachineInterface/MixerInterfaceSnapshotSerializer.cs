using FishNet.Serializing;

namespace SS3D.UI.MachineInterface
{
    public static class MixerInterfaceSnapshotSerializer
    {
        public static void WriteMixerInterfaceSnapshot(this Writer writer, MixerInterfaceSnapshot snapshot)
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
            writer.WriteInt32(snapshot.Inlet1Percent);
            writer.WriteInt32(snapshot.TargetOutletPressureKpa);
            writer.WriteBoolean(snapshot.Inlet1Dry);
            writer.WriteBoolean(snapshot.Inlet2Dry);
            GasPanelSnapshotSerializer.WriteGasRows(writer, snapshot.Gases);
            GasPanelSnapshotSerializer.WriteLine(writer, snapshot.Inlet1);
            GasPanelSnapshotSerializer.WriteLine(writer, snapshot.Inlet2);
            GasPanelSnapshotSerializer.WriteLine(writer, snapshot.Outlet);
        }

        public static MixerInterfaceSnapshot ReadMixerInterfaceSnapshot(this Reader reader)
        {
            return new MixerInterfaceSnapshot
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
                Inlet1Percent = reader.ReadInt32(),
                TargetOutletPressureKpa = reader.ReadInt32(),
                Inlet1Dry = reader.ReadBoolean(),
                Inlet2Dry = reader.ReadBoolean(),
                Gases = GasPanelSnapshotSerializer.ReadGasRows(reader),
                Inlet1 = GasPanelSnapshotSerializer.ReadLine(reader),
                Inlet2 = GasPanelSnapshotSerializer.ReadLine(reader),
                Outlet = GasPanelSnapshotSerializer.ReadLine(reader),
            };
        }
    }
}
