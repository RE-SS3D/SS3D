using UnityEngine;

namespace SS3D.Data
{
    /// <summary>
    /// Shared UI colors used by SS3D views and style assets.
    /// Keep subsystem-specific colors close to their subsystem instead.
    /// </summary>
    public static class PaletteColors
    {
        public static Color White = Color.white;
        public static Color TextPrimary = White;
        public static Color TextInverted = Color.black;
        public static Color TextDisabled = new Color(1f, 1f, 1f, 0.23137255f);

        public static Color ButtonBackground = new Color(0f, 0f, 0f, 0.34901962f);
        public static Color ButtonBackgroundHighlighted = new Color(0f, 0f, 0f, 0.7490196f);
        public static Color ButtonBackgroundPressed = White;
        public static Color ButtonBackgroundDisabled = new Color(0f, 0f, 0f, 0.6f);

        public static Color LightGreen = new Color32(42, 193, 50, 255);
        public static Color NormalGreen = new Color32(35, 155, 42, 255);
        public static Color LightBlue = new Color32(80, 115, 216, 255);
        public static Color LightRed = new Color32(155, 31, 31, 255);
    }
}
