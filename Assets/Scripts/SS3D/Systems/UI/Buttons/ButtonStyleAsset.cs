using UnityEngine;

namespace SS3D.Systems.UI.Buttons
{
    [CreateAssetMenu(fileName = "ButtonStyleAsset", menuName = "SS3D/UI/Buttons/ButtonStyle", order = 0)]
    public class ButtonStyleAsset : ScriptableObject
    {
        public ButtonTextColorPair NormalColor;
        public ButtonTextColorPair HighlightedColor;
        public ButtonTextColorPair PressedColor;
        public ButtonTextColorPair DisabledColor;
    }
}