using System;

namespace SS3D.Rendering.URP
{
    /// <summary>
    /// Builds CORTAO's occluder lookup table: for each sphere-map encoded occluder direction and occluder
    /// radius, the bit mask of hemisphere rays the occluder blocks.
    /// </summary>
    /// <remarks>
    /// Port of <c>utils/generators/OccluderLookupTableGenerator.py</c> from
    /// https://github.com/theagentd/CORTAO (MIT, Daniel Isheden). Generating it at runtime avoids shipping a
    /// binary 3D texture; DX11/SM5 cannot sample integer textures anyway, so the shader indexes a buffer.
    /// Layout matches the reference <c>.bin</c> files: x (direction u) fastest, then y, then z (radius).
    /// </remarks>
    public static class CortaoOccluderTable
    {
        /// <summary>Direction resolution CORTAO recommends for up to 32 samples.</summary>
        public const int DirectionResolution = 24;

        /// <summary>Radius resolution CORTAO recommends for up to 32 samples.</summary>
        public const int RadiusResolution = 16;

        /// <summary>Ray count; must match <c>CORTAO_RAY_COUNT</c> in CortaoMainPass.compute.</summary>
        public const int RayCount = 16;

        public static uint[] Generate()
        {
            return Generate(DirectionResolution, RadiusResolution, RayCount);
        }

        public static uint[] Generate(int directionResolution, int radiusResolution, int rayCount)
        {
            if (directionResolution <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(directionResolution));
            }

            if (radiusResolution <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(radiusResolution));
            }

            if (rayCount < 1 || rayCount > 32)
            {
                throw new ArgumentOutOfRangeException(nameof(rayCount), "One uint holds at most 32 rays.");
            }

            double maxOccluderRadius = Math.Sqrt(2.0);
            double goldenAngle = Math.PI * (3.0 - Math.Sqrt(5.0));

            double[] rayX = new double[rayCount];
            double[] rayY = new double[rayCount];
            double[] rayZ = new double[rayCount];
            for (int i = 0; i < rayCount; i++)
            {
                double angle = i * goldenAngle;
                double radius = Math.Sqrt((i + 0.5) / rayCount);
                double x = Math.Cos(angle) * radius;
                double y = Math.Sin(angle) * radius;
                rayX[i] = x;
                rayY[i] = y;
                rayZ[i] = Math.Sqrt(1.0 - x * x - y * y);
            }

            int sliceSize = directionResolution * directionResolution;
            uint[] output = new uint[sliceSize * radiusResolution];
            int[] closestIndex = new int[rayCount];
            double[] closestCos = new double[rayCount];

            for (int z = 0; z < radiusResolution; z++)
            {
                double occluderRadius = maxOccluderRadius * (z + 0.5) / radiusResolution;
                double occluderCosAngle = 1.0 - occluderRadius * occluderRadius * 0.5;
                int offset = z * sliceSize;

                for (int i = 0; i < rayCount; i++)
                {
                    closestIndex[i] = -1;
                    closestCos[i] = -1.0;
                }

                for (int y = 0; y < directionResolution; y++)
                {
                    for (int x = 0; x < directionResolution; x++)
                    {
                        DecodeSphereMap(
                            (x + 0.5) / directionResolution,
                            (y + 0.5) / directionResolution,
                            out double dirX,
                            out double dirY,
                            out double dirZ);

                        uint rayBits = 0u;
                        if (dirZ != -1.0)
                        {
                            for (int i = 0; i < rayCount; i++)
                            {
                                double cosAngle = dirX * rayX[i] + dirY * rayY[i] + dirZ * rayZ[i];
                                if (cosAngle > closestCos[i])
                                {
                                    closestCos[i] = cosAngle;
                                    closestIndex[i] = y * directionResolution + x;
                                }

                                if (cosAngle > occluderCosAngle)
                                {
                                    rayBits |= 1u << i;
                                }
                            }
                        }

                        output[offset + y * directionResolution + x] = rayBits;
                    }
                }

                // Even a tiny occluder blocks a ray it sits exactly on: mark each ray in its nearest texel.
                for (int i = 0; i < rayCount; i++)
                {
                    if (closestIndex[i] >= 0)
                    {
                        output[offset + closestIndex[i]] |= 1u << i;
                    }
                }
            }

            return output;
        }

        private static void DecodeSphereMap(double u, double v, out double x, out double y, out double z)
        {
            double ex = u * 2.0 - 1.0;
            double ey = v * 2.0 - 1.0;
            double zz = Math.Max(1.0 - ex * ex - ey * ey, 0.0);
            double scale = 2.0 * Math.Sqrt(zz);
            x = ex * scale;
            y = ey * scale;
            z = zz * 2.0 - 1.0;
        }
    }
}
