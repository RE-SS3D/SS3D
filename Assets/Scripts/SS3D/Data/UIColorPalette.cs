using UnityEngine;

namespace SS3D.Data
{
    /// <summary>
    /// Centralized color palette for consistent UI colors across the project.
    /// Create an asset via Assets &gt; Create &gt; SS3D &gt; UI &gt; Color Palette,
    /// then reference it in code via UIColorPalette.Current.
    /// </summary>
    [CreateAssetMenu(fileName = "UIColorPalette", menuName = "SS3D/UI/Color Palette", order = 1)]
    public class UIColorPalette : ScriptableObject
    {
        [Header("Primary")]
        public Color Primary = new(0.314f, 0.451f, 0.847f);
        public Color PrimaryDark = new(0.137f, 0.216f, 0.514f);
        public Color PrimaryLight = new(0.537f, 0.663f, 0.925f);

        [Header("Background")]
        public Color Background = new(0.118f, 0.118f, 0.118f);
        public Color BackgroundLight = new(0.180f, 0.180f, 0.180f);

        [Header("Text")]
        public Color TextPrimary = Color.white;
        public Color TextSecondary = new(0.706f, 0.706f, 0.706f);
        public Color TextDisabled = new(0.400f, 0.400f, 0.400f);

        [Header("Status")]
        public Color Success = new(0.165f, 0.757f, 0.196f);
        public Color Warning = new(0.890f, 0.745f, 0.180f);
        public Color Error = new(0.608f, 0.122f, 0.122f);

        [Header("Button")]
        public Color ButtonNormal = new(0.220f, 0.220f, 0.220f);
        public Color ButtonHover = new(0.259f, 0.259f, 0.259f);
        public Color ButtonPressed = new(0.157f, 0.157f, 0.157f);
        public Color ButtonDisabled = new(0.118f, 0.118f, 0.118f);

        private static UIColorPalette _current;

        /// <summary>
        /// The active color palette. Loads from Resources on first access.
        /// Place the palette asset in any Resources folder.
        /// </summary>
        public static UIColorPalette Current
        {
            get
            {
                if (_current == null)
                {
                    _current = Resources.Load<UIColorPalette>("UIColorPalette");
                }

                return _current;
            }
        }
    }
}
