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
            writer.WriteString(snapshot.MaintenanceText);
            writer.WriteByte(snapshot.MaintenanceTone);

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

            int metricCount = snapshot.AdvancedMetricCount;
            if (metricCount > SmesInterfaceSnapshot.MaxAdvancedMetrics)
            {
                metricCount = SmesInterfaceSnapshot.MaxAdvancedMetrics;
            }

            writer.WriteInt32(metricCount);
            for (int i = 0; i < metricCount; i++)
            {
                WriteMetric(writer, snapshot, i);
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
                MaintenanceText = reader.ReadString(),
                MaintenanceTone = reader.ReadByte(),
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

            snapshot.AdvancedMetricCount = reader.ReadInt32();
            if (snapshot.AdvancedMetricCount > SmesInterfaceSnapshot.MaxAdvancedMetrics)
            {
                snapshot.AdvancedMetricCount = SmesInterfaceSnapshot.MaxAdvancedMetrics;
            }

            for (int i = 0; i < snapshot.AdvancedMetricCount; i++)
            {
                SmesMetricSnapshot metric = ReadMetric(reader);
                SetMetric(ref snapshot, i, metric);
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

        private static void WriteMetric(Writer writer, SmesInterfaceSnapshot snapshot, int index)
        {
            SmesMetricSnapshot metric = GetMetric(snapshot, index);
            writer.WriteString(metric.Label);
            writer.WriteString(metric.Value);
            writer.WriteByte(metric.Tone);
        }

        private static SmesMetricSnapshot ReadMetric(Reader reader)
        {
            return new SmesMetricSnapshot
            {
                Label = reader.ReadString(),
                Value = reader.ReadString(),
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

        private static SmesMetricSnapshot GetMetric(SmesInterfaceSnapshot snapshot, int index)
        {
            return index switch
            {
                0 => snapshot.AdvancedMetric0,
                1 => snapshot.AdvancedMetric1,
                2 => snapshot.AdvancedMetric2,
                3 => snapshot.AdvancedMetric3,
                4 => snapshot.AdvancedMetric4,
                5 => snapshot.AdvancedMetric5,
                _ => default,
            };
        }

        private static void SetMetric(ref SmesInterfaceSnapshot snapshot, int index, SmesMetricSnapshot metric)
        {
            switch (index)
            {
                case 0:
                {
                    snapshot.AdvancedMetric0 = metric;
                    break;
                }

                case 1:
                {
                    snapshot.AdvancedMetric1 = metric;
                    break;
                }

                case 2:
                {
                    snapshot.AdvancedMetric2 = metric;
                    break;
                }

                case 3:
                {
                    snapshot.AdvancedMetric3 = metric;
                    break;
                }

                case 4:
                {
                    snapshot.AdvancedMetric4 = metric;
                    break;
                }

                case 5:
                {
                    snapshot.AdvancedMetric5 = metric;
                    break;
                }
            }
        }
    }
}
