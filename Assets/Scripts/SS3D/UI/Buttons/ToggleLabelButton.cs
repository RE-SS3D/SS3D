using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SS3D.UI.Buttons
{
    public class ToggleLabelButton : LabelButton
    {
        [SerializeField]
        private string _normalText;

        [SerializeField]
        private string _pressedText;

        public override void OnPointerDown(PointerEventData eventData)
        {
            if (Disabled)
            {
                return;
            }

            _pressed = !_pressed;
            ProcessPress(MouseButtonType.MouseDown);
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            if (Disabled)
            {
                return;
            }

            ProcessPress(MouseButtonType.MouseUp);
        }

        protected override void UpdateVisuals()
        {
            if (_buttonStyle == null)
            {
                return;
            }

            if (_image == null)
            {
                _image = GetComponent<Image>();
            }

            if (_label == null)
            {
                _label = GetComponentInChildren<TMP_Text>();
            }

            if (Disabled)
            {
                _image.color = DisabledColor.Button * (_baseImageColor * _colorMultiplier);
                _label.color = DisabledColor.Text * (_baseTextColor * _colorMultiplier);
                return;
            }

            if (_pressed && !Highlighted)
            {
                _image.color = PressedColor.Button * (_baseImageColor * _colorMultiplier);
                _label.color = PressedColor.Text * (_baseTextColor * _colorMultiplier);
                _label.SetText(_pressedText);
            }

            if (!_pressed && !Highlighted)
            {
                _image.color = NormalColor.Button * (_baseImageColor * _colorMultiplier);
                _label.color = NormalColor.Text * (_baseTextColor * _colorMultiplier);
                _label.SetText(_normalText);
            }

            if (_pressed && Highlighted)
            {
                _image.color = PressedHighlightedColor.Button * (_baseImageColor * _colorMultiplier);
                _label.color = PressedHighlightedColor.Text * (_baseTextColor * _colorMultiplier);
                _label.SetText(_pressedText);
            }

            if (!_pressed && Highlighted)
            {
                _image.color = NormalHighlightedColor.Button * (_baseImageColor * _colorMultiplier);
                _label.color = NormalHighlightedColor.Text * (_baseTextColor * _colorMultiplier);
                _label.SetText(_normalText);
            }
        }
    }
}