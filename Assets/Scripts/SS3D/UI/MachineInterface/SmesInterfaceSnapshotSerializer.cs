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
            writer.WriteString(snapshot.DiagnosisHint);

            int warningCount = snapshot.WarningCount;
            if (warningCount > SmesInterfaceSnapshot.MaxWarnings)
            {
                warningCount = SmesInterfaceSnapshot.MaxWarnings;
            }

            writer.WriteInt32(warningCount);
            for (int i = 0; i < warningCount; i++)
            {
                WriteWarning(writer, snapshot, i);
            }
        }

        public static SmesInterfaceSnapshot ReadSmesInterfaceSnapshot(this Reader reader)
        {
            SmesInterfaceSnapshot snapshot = new()
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
                DiagnosisHint = reader.ReadString(),
                WarningCount = reader.ReadInt32(),
            };

            if (snapshot.WarningCount > SmesInterfaceSnapshot.MaxWarnings)
            {
                snapshot.WarningCount = SmesInterfaceSnapshot.MaxWarnings;
            }

            for (int i = 0; i < snapshot.WarningCount; i++)
            {
                ApcDiagnosticSnapshot warning = ReadWarning(reader);
                SetWarning(ref snapshot, i, warning);
            }

            return snapshot;
        }

        private static void WriteWarning(Writer writer, SmesInterfaceSnapshot snapshot, int index)
        {
            ApcDiagnosticSnapshot warning = GetWarning(snapshot, index);
            writer.WriteString(warning.Glyph);
            writer.WriteString(warning.Text);
            writer.WriteByte(warning.Tone);
        }

        private static ApcDiagnosticSnapshot ReadWarning(Reader reader)
        {
            return new ApcDiagnosticSnapshot
            {
                Glyph = reader.ReadString(),
                Text = reader.ReadString(),
                Tone = reader.ReadByte(),
            };
        }

        private static ApcDiagnosticSnapshot GetWarning(SmesInterfaceSnapshot snapshot, int index)
        {
            return index switch
            {
                0 => snapshot.Warning0,
                1 => snapshot.Warning1,
                2 => snapshot.Warning2,
                3 => snapshot.Warning3,
                _ => default,
            };
        }

        private static void SetWarning(ref SmesInterfaceSnapshot snapshot, int index, ApcDiagnosticSnapshot warning)
        {
            switch (index)
            {
                case 0:
                {
                    snapshot.Warning0 = warning;
                    break;
                }

                case 1:
                {
                    snapshot.Warning1 = warning;
                    break;
                }

                case 2:
                {
                    snapshot.Warning2 = warning;
                    break;
                }

                case 3:
                {
                    snapshot.Warning3 = warning;
                    break;
                }
            }
        }
    }
}
