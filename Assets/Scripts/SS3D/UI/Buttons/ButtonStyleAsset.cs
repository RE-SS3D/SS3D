using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.UI.Buttons
{
    [CreateAssetMenu(fileName = "ButtonStyleAsset", menuName = "SS3D/UI/Buttons/ButtonStyle", order = 0)]
    public class ButtonStyleAsset : ScriptableObject
    {
        public ButtonTextColorPair NormalColor;

        [FormerlySerializedAs("HighlightedColor")]
        public ButtonTextColorPair NormalHighlightedColor;

        public ButtonTextColorPair PressedColor;

        public ButtonTextColorPair PressedHighlightedColor;

        public ButtonTextColorPair DisabledColor;
    }
}