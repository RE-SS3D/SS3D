using UnityEngine;

namespace SS3D.Data
{
    /// <summary>
    /// Shared UI colors used by SS3D views and style assets.
    /// Keep subsystem-specific colors close to their subsystem instead.
    /// </summary>
    public static class PaletteColors
    {
        public static readonly Color White = Color.white;
        public static readonly Color TextPrimary = White;
        public static readonly Color TextInverted = Color.black;
        public static readonly Color TextDisabled = new Color(1f, 1f, 1f, 0.23137255f);

        public static readonly Color ButtonBackground = new Color(0f, 0f, 0f, 0.34901962f);
        public static readonly Color ButtonBackgroundHighlighted = new Color(0f, 0f, 0f, 0.7490196f);
        public static readonly Color ButtonBackgroundPressed = White;
        public static readonly Color ButtonBackgroundDisabled = new Color(0f, 0f, 0f, 0.6f);

        public static readonly Color LightGreen = new Color32(42, 193, 50, 255);
        public static readonly Color NormalGreen = new Color32(35, 155, 42, 255);
        public static readonly Color LightBlue = new Color32(80, 115, 216, 255);
        public static readonly Color LightRed = new Color32(155, 31, 31, 255);
    }
}
