using FishNet.Serializing;
using UnityEngine;

namespace SS3D.UI.MachineInterface
{
    /// <summary>
    /// Gas table and line readings inside machine snapshots. Fractions go as one byte each: a panel
    /// shows a tenth of a percent at best.
    /// </summary>
    public static class GasPanelSnapshotSerializer
    {
        private const float FractionScale = 250f;

        public static void WriteGasRows(Writer writer, GasPanelRow[] rows)
        {
            int count = rows?.Length ?? 0;
            writer.WriteByte((byte)count);
            for (int i = 0; i < count; i++)
            {
                GasPanelRow row = rows[i];
                writer.WriteByte(row.Id);
                writer.WriteString(row.Symbol);
                writer.WriteString(row.Name);
                writer.WriteByte(row.Color.r);
                writer.WriteByte(row.Color.g);
                writer.WriteByte(row.Color.b);
                writer.WriteBoolean(row.Toxic);
            }
        }

        public static GasPanelRow[] ReadGasRows(Reader reader)
        {
            var rows = new GasPanelRow[reader.ReadByte()];
            for (int i = 0; i < rows.Length; i++)
            {
                rows[i] = new GasPanelRow
                {
                    Id = reader.ReadByte(),
                    Symbol = reader.ReadString(),
                    Name = reader.ReadString(),
                    Color = new Color32(reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), 255),
                    Toxic = reader.ReadBoolean(),
                };
            }

            return rows;
        }

        public static void WriteLine(Writer writer, GasLineReading reading)
        {
            writer.WriteSingle(reading.PressureKpa);
            int count = reading.Fractions?.Length ?? 0;
            writer.WriteByte((byte)count);
            for (int i = 0; i < count; i++)
            {
                writer.WriteByte((byte)Mathf.RoundToInt(Mathf.Clamp01(reading.Fractions[i]) * FractionScale));
            }
        }

        public static GasLineReading ReadLine(Reader reader)
        {
            var reading = new GasLineReading { PressureKpa = reader.ReadSingle() };
            reading.Fractions = new float[reader.ReadByte()];
            for (int i = 0; i < reading.Fractions.Length; i++)
            {
                reading.Fractions[i] = reader.ReadByte() / FractionScale;
            }

            return reading;
        }
    }
}
