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
        public static class Buttons
        {
            public static readonly ButtonStyle LabelButton = new()
            {
                NormalColor = new ButtonTextColorPair
                {
                    Button = new Color(0, 0, 0, .35f),
                    Text = new Color(255, 255, 255, 1)
                },
                HighlightedColor = new ButtonTextColorPair
                {
                    Button = new Color(0, 0, 0, .75f),
                    Text = new Color(255, 255, 255, 1)
                },
                PressedColor = new ButtonTextColorPair
                {
                    Button = new Color(255, 255, 255, 1),
                    Text = new Color(0, 0, 0, 1)
                },
                DisabledColor = new ButtonTextColorPair
                {
                    Button = new Color(0, 0, 0, .6f),
                    Text = new Color(255, 255, 255, .23f)
                },
            };
        }
        #endregion
    }
}