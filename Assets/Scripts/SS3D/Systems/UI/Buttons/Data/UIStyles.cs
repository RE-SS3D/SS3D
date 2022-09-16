using UnityEngine;

namespace SS3D.Systems.UI.Buttons.Data
{
    /// <summary>
    /// Used to define the general style of the standard button.
    ///
    /// Kind of a big CSS file, just less stupid.
    /// </summary>
    public static class UIStyles
    {
        #region GENERIC
        public static class Generic
        {
            public static Color OpaqueBackgroundColor = new Color(0, 0, 0, .35f);
        }
        #endregion

        #region LABEL_BUTTON
        public static class LabelButton
        {
            public static ButtonTextColorPair NormalColor = new()
            {
                Button = new Color(0, 0, 0, .35f),
                Text = new Color(255, 255, 255, 1)
            };

            public static ButtonTextColorPair HighlightedColor = new()
            {
                Button = new Color(0, 0, 0, .75f),
                Text = new Color(255, 255, 255, 1)
            };

            public static ButtonTextColorPair PressedColor = new()
            {
                Button = new Color(255, 255, 255, 1),
                Text = new Color(0, 0, 0, 1)
            };

            public static ButtonTextColorPair DisabledColor = new()
            {
                Button = new Color(0, 0, 0, .6f),
                Text = new Color(255, 255, 255, .23f)
            };
        }
        #endregion
    }
}