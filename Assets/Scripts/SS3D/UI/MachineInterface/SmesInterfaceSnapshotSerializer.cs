using FishNet.Serializing;

namespace SS3D.UI.MachineInterface
{
    public static class SmesInterfaceSnapshotSerializer
    {
        public static void WriteSmesInterfaceSnapshot(this Writer writer, SmesInterfaceSnapshot snapshot)
        {
            writer.WriteInt32(snapshot.MachineObjectId);
            writer.WriteString(snapshot.InterfaceId);
            writer.WriteString(snapshot.Title);
            writer.WriteByte(snapshot.PowerState);
            writer.WriteSingle(snapshot.ChargePct);
            writer.WriteByte(snapshot.ChargeTrend);
            writer.WriteSingle(snapshot.InputCurrentKw);
            writer.WriteSingle(snapshot.OutputCurrentKw);
            writer.WriteSingle(snapshot.InputMaxKw);
            writer.WriteSingle(snapshot.OutputMaxKw);
            writer.WriteBoolean(snapshot.InputEnabled);
            writer.WriteBoolean(snapshot.OutputEnabled);
            writer.WriteBoolean(snapshot.InputActive);
            writer.WriteBoolean(snapshot.OutputActive);
            writer.WriteString(snapshot.ConnectionStateText);
            writer.WriteBoolean(snapshot.AccessGranted);
            writer.WriteBoolean(snapshot.AccessScanning);
        }

        public static SmesInterfaceSnapshot ReadSmesInterfaceSnapshot(this Reader reader)
        {
            return new SmesInterfaceSnapshot
            {
                MachineObjectId = reader.ReadInt32(),
                InterfaceId = reader.ReadString(),
                Title = reader.ReadString(),
                PowerState = reader.ReadByte(),
                ChargePct = reader.ReadSingle(),
                ChargeTrend = reader.ReadByte(),
                InputCurrentKw = reader.ReadSingle(),
                OutputCurrentKw = reader.ReadSingle(),
                InputMaxKw = reader.ReadSingle(),
                OutputMaxKw = reader.ReadSingle(),
                InputEnabled = reader.ReadBoolean(),
                OutputEnabled = reader.ReadBoolean(),
                InputActive = reader.ReadBoolean(),
                OutputActive = reader.ReadBoolean(),
                ConnectionStateText = reader.ReadString(),
                AccessGranted = reader.ReadBoolean(),
                AccessScanning = reader.ReadBoolean(),
            };
        }
    }
}
