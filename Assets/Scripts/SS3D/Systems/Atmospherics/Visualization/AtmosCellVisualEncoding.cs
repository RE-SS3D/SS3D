using SS3D.Systems.Atmospherics.ECS;
using System;
using Unity.Collections;
using UnityEngine;

namespace SS3D.Systems.Atmospherics.Visualization
{
    /// <summary>
    /// Encodes raw sim cell state into the byte/Color32 formats the GPU atlas (and its network
    /// patches) use. Shared by <see cref="AtmosGpuUploader"/> (host atlas) and
    /// <see cref="AtmosChunkPatchBuilder"/> (per-chunk network patches) so both agree on the mask
    /// and composition encoding.
    /// </summary>
    public static class AtmosCellVisualEncoding
    {
        public const int CompositionGasChannels = 4;

        public const byte MaskEmpty = 0;
        public const byte MaskSimulated = 1;
        public const byte MaskVacuum = 2;
        public const byte MaskBlocked = 3;

        /// <summary>Wind speed (m/s) at which encoded flow reaches full strength.</summary>
        public const float FlowFullScaleSpeed = AtmosWind.KnockdownSpeed;

        /// <summary>
        /// Wind in -1..1 from a tile's air speed: it keeps direction, and length grows with the
        /// square root of speed so a gentle draught still reads.
        /// </summary>
        public static Vector2 EncodeFlow(Vector3 wind)
        {
            var planar = new Vector2(wind.x, wind.z);
            float speed = planar.magnitude;
            if (speed <= 1e-4f)
                return Vector2.zero;

            return planar / speed * Mathf.Sqrt(Mathf.Min(speed / FlowFullScaleSpeed, 1f));
        }

        /// <summary>Stores encoded flow as 0..1 RG, where 0.5 is still air.</summary>
        public static void PackFlow(Vector2 flow, float[] target, int texel)
        {
            target[texel * 2] = flow.x * 0.5f + 0.5f;
            target[texel * 2 + 1] = flow.y * 0.5f + 0.5f;
        }

        /// <summary>
        /// Bilinear CPU read of a packed flow atlas, matching what the shader sees. Tile centres sit on
        /// whole world coordinates. Returns still air outside the atlas.
        /// </summary>
        public static Vector2 SampleFlow(float[] packed, Vector4 atlasBounds, float worldX, float worldZ)
        {
            int width = (int)atlasBounds.z;
            int height = (int)atlasBounds.w;
            float localX = worldX - atlasBounds.x;
            float localZ = worldZ - atlasBounds.y;
            if (packed == null || localX < 0f || localZ < 0f || localX > width - 1 || localZ > height - 1)
                return Vector2.zero;

            int x = Mathf.Min((int)localX, width - 2);
            int z = Mathf.Min((int)localZ, height - 2);
            if (x < 0 || z < 0)
                return Vector2.zero;

            float fractionX = localX - x;
            float fractionZ = localZ - z;
            Vector2 near = Vector2.Lerp(ReadFlow(packed, x, z, width), ReadFlow(packed, x + 1, z, width), fractionX);
            Vector2 far = Vector2.Lerp(ReadFlow(packed, x, z + 1, width), ReadFlow(packed, x + 1, z + 1, width), fractionX);
            return Vector2.Lerp(near, far, fractionZ);
        }

        private static Vector2 ReadFlow(float[] packed, int x, int z, int width)
        {
            int index = (z * width + x) * 2;
            return new Vector2(packed[index] * 2f - 1f, packed[index + 1] * 2f - 1f);
        }

        public static byte EncodeMask(AtmosCellState state)
        {
            return state switch
            {
                AtmosCellState.Vacuum => MaskVacuum,
                AtmosCellState.Blocked => MaskBlocked,
                AtmosCellState.Active or AtmosCellState.Semiactive or AtmosCellState.Inactive => MaskSimulated,
                _ => MaskEmpty,
            };
        }

        public static Color32 EncodeComposition(NativeArray<float>.ReadOnly molesRead, int cellIndex)
        {
            float totalMoles = 0f;
            Span<float> moles = stackalloc float[CompositionGasChannels];

            for (int gasId = 0; gasId < CompositionGasChannels; gasId++)
            {
                moles[gasId] = molesRead[GasMixture.GetMoleIndex(cellIndex, new GasId((ushort)gasId))];
                totalMoles += moles[gasId];
            }

            if (totalMoles <= 1e-6f)
                return new Color32(0, 0, 0, 0);

            return new Color32(
                ToByte(moles[0] / totalMoles),
                ToByte(moles[1] / totalMoles),
                ToByte(moles[2] / totalMoles),
                ToByte(moles[3] / totalMoles));
        }

        public static byte ToByte(float normalized)
        {
            return (byte)Mathf.Clamp(Mathf.RoundToInt(normalized * 255f), 0, 255);
        }
    }
}
