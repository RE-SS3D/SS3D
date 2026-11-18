using FishNet.Serializing;
using SS3D.Systems.Health;

namespace SS3D.UI.MachineInterface
{
    public static class HealthScanSnapshotSerializer
    {
        public static void WriteHealthScanSnapshot(this Writer writer, HealthScanSnapshot snapshot)
        {
            writer.WriteInt32(snapshot.MachineObjectId);
            writer.WriteString(snapshot.InterfaceId);
            writer.WriteString(snapshot.ModelLabel);
            writer.WriteString(snapshot.DeviceTitle);
            writer.WriteString(snapshot.SubjectName);
            writer.WriteByte((byte)snapshot.State);
            writer.WriteBoolean(snapshot.IsConscious);
            writer.WriteBoolean(snapshot.IsCardiacArrest);
            writer.WriteByte((byte)snapshot.CriticalFlags);
            writer.WriteSingle(snapshot.BloodVolumeRatio);
            writer.WriteSingle(snapshot.OxyDebt);
            writer.WriteSingle(snapshot.Toxin);
            writer.WriteSingle(snapshot.Shock);
            writer.WriteSingle(snapshot.IncomingPain);
            writer.WriteSingle(snapshot.TotalBleedingRate);
            WriteFloats(writer, snapshot.ZoneBrute);
            WriteFloats(writer, snapshot.ZoneBurn);

            int flagCount = snapshot.ZoneFlags?.Length ?? 0;
            writer.WriteByte((byte)flagCount);
            for (int i = 0; i < flagCount; i++)
            {
                writer.WriteByte((byte)snapshot.ZoneFlags[i]);
            }

            WriteFloats(writer, snapshot.OrganFunction);

            int severityCount = snapshot.ZoneSeverity?.Length ?? 0;
            writer.WriteByte((byte)severityCount);
            for (int i = 0; i < severityCount; i++)
            {
                writer.WriteByte((byte)snapshot.ZoneSeverity[i]);
            }
        }

        public static HealthScanSnapshot ReadHealthScanSnapshot(this Reader reader)
        {
            var snapshot = new HealthScanSnapshot
            {
                MachineObjectId = reader.ReadInt32(),
                InterfaceId = reader.ReadString(),
                ModelLabel = reader.ReadString(),
                DeviceTitle = reader.ReadString(),
                SubjectName = reader.ReadString(),
                State = (HealthState)reader.ReadByte(),
                IsConscious = reader.ReadBoolean(),
                IsCardiacArrest = reader.ReadBoolean(),
                CriticalFlags = (HealthCriticalFlags)reader.ReadByte(),
                BloodVolumeRatio = reader.ReadSingle(),
                OxyDebt = reader.ReadSingle(),
                Toxin = reader.ReadSingle(),
                Shock = reader.ReadSingle(),
                IncomingPain = reader.ReadSingle(),
                TotalBleedingRate = reader.ReadSingle(),
                ZoneBrute = ReadFloats(reader),
                ZoneBurn = ReadFloats(reader),
            };

            snapshot.ZoneFlags = new HealthScanZoneFlags[reader.ReadByte()];
            for (int i = 0; i < snapshot.ZoneFlags.Length; i++)
            {
                snapshot.ZoneFlags[i] = (HealthScanZoneFlags)reader.ReadByte();
            }

            snapshot.OrganFunction = ReadFloats(reader);

            snapshot.ZoneSeverity = new WoundSeverity[reader.ReadByte()];
            for (int i = 0; i < snapshot.ZoneSeverity.Length; i++)
            {
                snapshot.ZoneSeverity[i] = (WoundSeverity)reader.ReadByte();
            }

            return snapshot;
        }

        private static void WriteFloats(Writer writer, float[] values)
        {
            int count = values?.Length ?? 0;
            writer.WriteByte((byte)count);
            for (int i = 0; i < count; i++)
            {
                writer.WriteSingle(values[i]);
            }
        }

        private static float[] ReadFloats(Reader reader)
        {
            float[] values = new float[reader.ReadByte()];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = reader.ReadSingle();
            }

            return values;
        }
    }
}
