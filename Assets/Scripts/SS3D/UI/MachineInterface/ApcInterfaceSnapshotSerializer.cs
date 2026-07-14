using FishNet.Serializing;

namespace SS3D.UI.MachineInterface
{
    public static class ApcInterfaceSnapshotSerializer
    {
        public static void WriteApcInterfaceSnapshot(this Writer writer, ApcInterfaceSnapshot snapshot)
        {
            writer.WriteInt32(snapshot.MachineObjectId);
            writer.WriteString(snapshot.InterfaceId);
            writer.WriteString(snapshot.Title);
            writer.WriteByte((byte)snapshot.Channels);
            writer.WriteByte(snapshot.PowerState);
            writer.WriteSingle(snapshot.GridInputKw);
            writer.WriteSingle(snapshot.LoadOutputKw);
            writer.WriteSingle(snapshot.BatteryCharge);
            writer.WriteByte(snapshot.BatteryState);
            writer.WriteSingle(snapshot.LightingLoadKw);
            writer.WriteSingle(snapshot.EquipmentLoadKw);
            writer.WriteSingle(snapshot.EnvironmentLoadKw);
            writer.WriteBoolean(snapshot.MultipleApcsInArea);
            writer.WriteBoolean(snapshot.AccessGranted);
            writer.WriteBoolean(snapshot.AccessScanning);
            writer.WriteBoolean(snapshot.AccessDenied);

            int diagnosticCount = snapshot.DiagnosticCount;
            if (diagnosticCount > ApcInterfaceSnapshot.MaxDiagnostics)
            {
                diagnosticCount = ApcInterfaceSnapshot.MaxDiagnostics;
            }

            writer.WriteInt32(diagnosticCount);
            for (int i = 0; i < diagnosticCount; i++)
            {
                WriteDiagnostic(writer, snapshot, i);
            }
        }

        public static ApcInterfaceSnapshot ReadApcInterfaceSnapshot(this Reader reader)
        {
            ApcInterfaceSnapshot snapshot = new()
            {
                MachineObjectId = reader.ReadInt32(),
                InterfaceId = reader.ReadString(),
                Title = reader.ReadString(),
                Channels = (System.Electricity.ApcControlFlags)reader.ReadByte(),
                PowerState = reader.ReadByte(),
                GridInputKw = reader.ReadSingle(),
                LoadOutputKw = reader.ReadSingle(),
                BatteryCharge = reader.ReadSingle(),
                BatteryState = reader.ReadByte(),
                LightingLoadKw = reader.ReadSingle(),
                EquipmentLoadKw = reader.ReadSingle(),
                EnvironmentLoadKw = reader.ReadSingle(),
                MultipleApcsInArea = reader.ReadBoolean(),
                AccessGranted = reader.ReadBoolean(),
                AccessScanning = reader.ReadBoolean(),
                AccessDenied = reader.ReadBoolean(),
                DiagnosticCount = reader.ReadInt32(),
            };

            if (snapshot.DiagnosticCount > ApcInterfaceSnapshot.MaxDiagnostics)
            {
                snapshot.DiagnosticCount = ApcInterfaceSnapshot.MaxDiagnostics;
            }

            for (int i = 0; i < snapshot.DiagnosticCount; i++)
            {
                ApcDiagnosticSnapshot diagnostic = ReadDiagnostic(reader);
                SetDiagnostic(ref snapshot, i, diagnostic);
            }

            return snapshot;
        }

        private static void WriteDiagnostic(Writer writer, ApcInterfaceSnapshot snapshot, int index)
        {
            ApcDiagnosticSnapshot diagnostic = GetDiagnostic(snapshot, index);
            writer.WriteString(diagnostic.Glyph);
            writer.WriteString(diagnostic.Text);
            writer.WriteByte(diagnostic.Tone);
        }

        private static ApcDiagnosticSnapshot ReadDiagnostic(Reader reader)
        {
            return new ApcDiagnosticSnapshot
            {
                Glyph = reader.ReadString(),
                Text = reader.ReadString(),
                Tone = reader.ReadByte(),
            };
        }

        private static ApcDiagnosticSnapshot GetDiagnostic(ApcInterfaceSnapshot snapshot, int index)
        {
            return index switch
            {
                0 => snapshot.Diagnostic0,
                1 => snapshot.Diagnostic1,
                2 => snapshot.Diagnostic2,
                3 => snapshot.Diagnostic3,
                4 => snapshot.Diagnostic4,
                5 => snapshot.Diagnostic5,
                _ => default,
            };
        }

        private static void SetDiagnostic(ref ApcInterfaceSnapshot snapshot, int index, ApcDiagnosticSnapshot diagnostic)
        {
            switch (index)
            {
                case 0:
                {
                    snapshot.Diagnostic0 = diagnostic;
                    break;
                }

                case 1:
                {
                    snapshot.Diagnostic1 = diagnostic;
                    break;
                }

                case 2:
                {
                    snapshot.Diagnostic2 = diagnostic;
                    break;
                }

                case 3:
                {
                    snapshot.Diagnostic3 = diagnostic;
                    break;
                }

                case 4:
                {
                    snapshot.Diagnostic4 = diagnostic;
                    break;
                }

                case 5:
                {
                    snapshot.Diagnostic5 = diagnostic;
                    break;
                }
            }
        }
    }
}
