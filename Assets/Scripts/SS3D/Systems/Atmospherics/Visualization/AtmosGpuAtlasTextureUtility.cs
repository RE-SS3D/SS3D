using UnityEngine;

namespace SS3D.Systems.Atmospherics.Visualization
{
    /// <summary>
    /// Texture allocation helpers shared by the host atlas builder (<see cref="AtmosGpuUploader"/>)
    /// and the client-side atlas built from network patches (<see cref="AtmosClientAtlas"/>).
    /// </summary>
    internal static class AtmosGpuAtlasTextureUtility
    {
        public static void EnsureTexture(
            ref Texture2D texture,
            int width,
            int height,
            TextureFormat format,
            FilterMode filterMode)
        {
            if (texture != null && texture.width == width && texture.height == height && texture.format == format)
            {
                texture.filterMode = filterMode;
                texture.wrapMode = TextureWrapMode.Clamp;
                return;
            }

            if (texture != null)
                DestroyTexture(ref texture);

            texture = new Texture2D(width, height, format, mipChain: false, linear: true)
            {
                filterMode = filterMode,
                wrapMode = TextureWrapMode.Clamp,
                name = $"Atmos{format}",
            };
        }

        public static void DestroyTexture(ref Texture2D texture)
        {
            if (texture == null)
                return;

            Object.DestroyImmediate(texture);
            texture = null;
        }

        public static int NextPowerOfTwo(int value)
        {
            int power = 1;
            while (power < value)
                power <<= 1;
            return power;
        }
    }
}
