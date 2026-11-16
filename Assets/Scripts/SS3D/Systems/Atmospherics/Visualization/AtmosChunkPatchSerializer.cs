using FishNet.Serializing;
using UnityEngine;

namespace SS3D.Systems.Atmospherics.Visualization
{
    /// <summary>
    /// Custom FishNet wire format for <see cref="AtmosChunkPatch"/>. Named to match FishNet's
    /// custom-serializer convention (Write/Read + type name as Writer/Reader extensions), same
    /// pattern as the machine interface snapshot serializers. Channels are quantized to what the
    /// atlas can show: half floats for pressure and temperature, bytes for 0..1 channels. The sizes
    /// are <see cref="AtmosChunkPatch.WireBytes"/>, which Decision 10's traffic budget is counted in.
    /// </summary>
    public static class AtmosChunkPatchSerializer
    {
        public static void WriteAtmosChunkPatch(this Writer writer, AtmosChunkPatch patch)
        {
            writer.WriteInt32(patch.MapId);
            writer.WriteVector2Int(patch.ChunkKey);

            for (int i = 0; i < AtmosChunkPatch.CellCount; i++)
                writer.WriteUInt16(Mathf.FloatToHalf(patch.Pressure[i]));

            for (int i = 0; i < AtmosChunkPatch.CellCount; i++)
                writer.WriteUInt16(Mathf.FloatToHalf(patch.Temperature[i]));

            for (int i = 0; i < AtmosChunkPatch.CellCount; i++)
                writer.WriteColor32(patch.Composition[i]);

            for (int i = 0; i < AtmosChunkPatch.CellCount * 2; i++)
                writer.WriteByte(PackUnit(patch.Flow[i]));

            for (int i = 0; i < AtmosChunkPatch.CellCount; i++)
                writer.WriteByte(PackUnit(patch.FireIntensity[i]));

            for (int i = 0; i < AtmosChunkPatch.CellCount; i++)
                writer.WriteByte(patch.Mask[i]);
        }

        public static AtmosChunkPatch ReadAtmosChunkPatch(this Reader reader)
        {
            var patch = new AtmosChunkPatch
            {
                MapId = reader.ReadInt32(),
                ChunkKey = reader.ReadVector2Int(),
                Pressure = new float[AtmosChunkPatch.CellCount],
                Temperature = new float[AtmosChunkPatch.CellCount],
                Composition = new UnityEngine.Color32[AtmosChunkPatch.CellCount],
                Flow = new float[AtmosChunkPatch.CellCount * 2],
                FireIntensity = new float[AtmosChunkPatch.CellCount],
                Mask = new byte[AtmosChunkPatch.CellCount],
            };

            for (int i = 0; i < AtmosChunkPatch.CellCount; i++)
                patch.Pressure[i] = Mathf.HalfToFloat(reader.ReadUInt16());

            for (int i = 0; i < AtmosChunkPatch.CellCount; i++)
                patch.Temperature[i] = Mathf.HalfToFloat(reader.ReadUInt16());

            for (int i = 0; i < AtmosChunkPatch.CellCount; i++)
                patch.Composition[i] = reader.ReadColor32();

            for (int i = 0; i < AtmosChunkPatch.CellCount * 2; i++)
                patch.Flow[i] = UnpackUnit(reader.ReadByte());

            for (int i = 0; i < AtmosChunkPatch.CellCount; i++)
                patch.FireIntensity[i] = UnpackUnit(reader.ReadByte());

            for (int i = 0; i < AtmosChunkPatch.CellCount; i++)
                patch.Mask[i] = reader.ReadByte();

            return patch;
        }

        // 254 steps, so packed flow's 0.5 (still air) survives exactly.
        private static byte PackUnit(float value) => (byte)Mathf.RoundToInt(Mathf.Clamp01(value) * UnitSteps);

        private static float UnpackUnit(byte value) => value / UnitSteps;

        private const float UnitSteps = 254f;
    }
}
