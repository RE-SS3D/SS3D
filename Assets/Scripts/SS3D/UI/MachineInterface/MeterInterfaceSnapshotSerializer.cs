using FishNet.Serializing;

namespace SS3D.UI.MachineInterface
{
    public static class MeterInterfaceSnapshotSerializer
    {
        public static void WriteMeterInterfaceSnapshot(this Writer writer, MeterInterfaceSnapshot snapshot)
        {
            writer.WriteInt32(snapshot.MachineObjectId);
            writer.WriteString(snapshot.InterfaceId);
            writer.WriteString(snapshot.ModelLabel);
            writer.WriteString(snapshot.DeviceTitle);
            writer.WriteString(snapshot.LineLabel);
            writer.WriteBoolean(snapshot.Connected);
            writer.WriteSingle(snapshot.TemperatureKelvin);
            writer.WriteSingle(snapshot.Moles);
            writer.WriteSingle(snapshot.VolumeLitres);
            GasPanelSnapshotSerializer.WriteGasRows(writer, snapshot.Gases);
            GasPanelSnapshotSerializer.WriteLine(writer, snapshot.Line);

            int historyCount = snapshot.History?.Length ?? 0;
            writer.WriteByte((byte)historyCount);
            for (int i = 0; i < historyCount; i++)
            {
                writer.WriteSingle(snapshot.History[i]);
            }
        }

        public static MeterInterfaceSnapshot ReadMeterInterfaceSnapshot(this Reader reader)
        {
            var snapshot = new MeterInterfaceSnapshot
            {
                MachineObjectId = reader.ReadInt32(),
                InterfaceId = reader.ReadString(),
                ModelLabel = reader.ReadString(),
                DeviceTitle = reader.ReadString(),
                LineLabel = reader.ReadString(),
                Connected = reader.ReadBoolean(),
                TemperatureKelvin = reader.ReadSingle(),
                Moles = reader.ReadSingle(),
                VolumeLitres = reader.ReadSingle(),
                Gases = GasPanelSnapshotSerializer.ReadGasRows(reader),
                Line = GasPanelSnapshotSerializer.ReadLine(reader),
            };

            snapshot.History = new float[reader.ReadByte()];
            for (int i = 0; i < snapshot.History.Length; i++)
            {
                snapshot.History[i] = reader.ReadSingle();
            }

            return snapshot;
        }
    }
}
