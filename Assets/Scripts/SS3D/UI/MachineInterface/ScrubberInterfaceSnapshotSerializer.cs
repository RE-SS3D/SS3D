using FishNet.Serializing;

namespace SS3D.UI.MachineInterface
{
    public static class ScrubberInterfaceSnapshotSerializer
    {
        public static void WriteScrubberInterfaceSnapshot(this Writer writer, ScrubberInterfaceSnapshot snapshot)
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
            writer.WriteInt32(snapshot.FlowRate);
            writer.WriteBoolean(snapshot.FilterO2);
            writer.WriteBoolean(snapshot.FilterN2);
            writer.WriteBoolean(snapshot.FilterCo2);
            writer.WriteBoolean(snapshot.FilterPlasma);
            writer.WriteBoolean(snapshot.FilterToxins);
        }

        public static ScrubberInterfaceSnapshot ReadScrubberInterfaceSnapshot(this Reader reader)
        {
            return new ScrubberInterfaceSnapshot
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
                FlowRate = reader.ReadInt32(),
                FilterO2 = reader.ReadBoolean(),
                FilterN2 = reader.ReadBoolean(),
                FilterCo2 = reader.ReadBoolean(),
                FilterPlasma = reader.ReadBoolean(),
                FilterToxins = reader.ReadBoolean(),
            };
        }
    }
}
