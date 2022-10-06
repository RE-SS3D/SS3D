using System;

namespace SS3D.UI.Buttons
{
    [Serializable]
    public struct ButtonStyle
    {
        public ButtonTextColorPair NormalColor;
        public ButtonTextColorPair HighlightedColor;
        public ButtonTextColorPair PressedColor;
        public ButtonTextColorPair DisabledColor;
    }
}