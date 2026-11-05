using UnityEngine;

namespace SS3D.UI.Shell.MainMenu
{
    /// <summary>Small cached Gaussian backdrop for operation dialogs; never modifies the source artwork.</summary>
    internal static class MainMenuWallpaper
    {
        public static Texture2D Blur(Texture source)
        {
            const int width = 120;
            const int height = 68;
            RenderTexture previous = RenderTexture.active;
            RenderTexture target = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Texture2D texture = new(width, height, TextureFormat.RGBA32, false)
            { name = "Main menu blurred wallpaper", hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            try
            {
                Graphics.Blit(source, target, Vector2.one / 1.08f, Vector2.one * (0.5f - 0.5f / 1.08f));
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                Color[] pixels = texture.GetPixels();
                Color[] horizontal = new Color[pixels.Length];
                Color[] result = new Color[pixels.Length];
                // Sigma 1.75 at 1/16 resolution reproduces the reference's 28px wallpaper blur.
                float[] kernel = new float[13];
                float total = 0;
                for (int k = -6; k <= 6; k++) { kernel[k + 6] = Mathf.Exp(-k * k / (2f * 1.75f * 1.75f)); total += kernel[k + 6]; }
                for (int k = 0; k < kernel.Length; k++) kernel[k] /= total;
                for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                        for (int k = -6; k <= 6; k++)
                            horizontal[y * width + x] += pixels[y * width + Mathf.Clamp(x + k, 0, width - 1)] * kernel[k + 6];
                for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                        for (int k = -6; k <= 6; k++)
                            result[y * width + x] += horizontal[Mathf.Clamp(y + k, 0, height - 1) * width + x] * kernel[k + 6];
                Color32[] output = new Color32[result.Length];
                for (int i = 0; i < result.Length; i++) output[i] = result[i];
                texture.SetPixels32(output);
                texture.Apply(false, true);
                return texture;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
            }
        }
    }
}
