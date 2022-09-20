using System;

namespace SS3D.Systems.UI.Buttons
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