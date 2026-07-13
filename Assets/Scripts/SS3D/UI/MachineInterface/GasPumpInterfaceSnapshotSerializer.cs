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
            writer.WriteBoolean(snapshot.PowerOk);
            writer.WriteBoolean(snapshot.Enabled);
            writer.WriteBoolean(snapshot.Connected);
            writer.WriteSingle(snapshot.RatedMaxFlowMolesPerSecond);
            writer.WriteSingle(snapshot.CurrentFlowMolesPerSecond);
            writer.WriteSingle(snapshot.DifferentialKpa);
            writer.WriteSingle(snapshot.MaxDifferentialKpa);
            writer.WriteBoolean(snapshot.Stalled);
            writer.WriteByte(snapshot.HealthState);
        }

        public static GasPumpInterfaceSnapshot ReadGasPumpInterfaceSnapshot(this Reader reader)
        {
            return new GasPumpInterfaceSnapshot
            {
                MachineObjectId = reader.ReadInt32(),
                InterfaceId = reader.ReadString(),
                Title = reader.ReadString(),
                ModelLabel = reader.ReadString(),
                PowerOk = reader.ReadBoolean(),
                Enabled = reader.ReadBoolean(),
                Connected = reader.ReadBoolean(),
                RatedMaxFlowMolesPerSecond = reader.ReadSingle(),
                CurrentFlowMolesPerSecond = reader.ReadSingle(),
                DifferentialKpa = reader.ReadSingle(),
                MaxDifferentialKpa = reader.ReadSingle(),
                Stalled = reader.ReadBoolean(),
                HealthState = reader.ReadByte(),
            };
        }
    }
}
