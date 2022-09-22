using System;
using SS3D.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SS3D.Systems.UI.Buttons
{
    /// <summary>
    /// Custom SS3D button, works similarly as Unity's
    /// </summary>
    [AddComponentMenu("| SS3D/UI/Label Button")]
    public class LabelButton : SpessBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler, IPointerDownHandler
    {
        public event Action<bool> OnPressed;
        public event Action<bool> OnHighlightChanged;
        public event Action<bool> OnDisabledChanged;

        [Header("States")] 
        [SerializeField] private bool _pressed;
        [SerializeField] private bool _disabled;
        [Header("Debug Info")]
        [SerializeField] private bool _highlighted;

        [Header("Style")]
        [SerializeField] private ButtonStyleAsset _buttonStyle;

        [Header("Colors")]
        [SerializeField] private float _colorMultiplier = 1f;
        [SerializeField] private Color _baseImageColor = Color.white;
        [SerializeField] private Color _baseTextColor = Color.white;

        [Header("UI Elements")]
        [SerializeField] private TMP_Text _label;
        [SerializeField] private Image _image;

        public bool Pressed
        {
            get => _pressed;
            private set
            {
                _pressed = value; 
                OnPressed?.Invoke(_pressed);
            }
        }

        public bool Disabled
        {
            get => _disabled;
            private set
            {
                _disabled = value;
                OnDisabledChanged?.Invoke(_disabled);
            }
        }

        public bool Highlighted
        {
            get => _highlighted;
            private set
            {
                _highlighted = value; 
                OnHighlightChanged?.Invoke(_highlighted);
            }
        }

        private ButtonTextColorPair NormalColor => _buttonStyle.NormalColor;
        private ButtonTextColorPair HighlightedColor => _buttonStyle.HighlightedColor;
        private ButtonTextColorPair PressedColor => _buttonStyle.PressedColor;
        private ButtonTextColorPair DisabledColor => _buttonStyle.DisabledColor;

        protected override void OnValidate()
        {
            base.OnValidate();

            UpdateVisuals();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Highlighted = true;

            if (Disabled)
            {
                return;
            }

            Highlight();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Highlighted = false;

            if (Disabled)
            {
                return;
            }

            Unhighlight();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (Disabled)
            {
                return;
            }

            _pressed = true;
            ProcessPress();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (Disabled)
            {
                return;
            }

            _pressed = false;
            ProcessPress();
        }

        private void Highlight()
        {
            UpdateVisuals();
        }

        private void Unhighlight()
        {
            UpdateVisuals();
        }

        private void Select()
        {
            UpdateVisuals();
        }

        private void Deselect()
        {
            UpdateVisuals();
        }

        private void ProcessPress()
        {
            if (_pressed)
            {
                Select(); 
            }
            else
            {
                Deselect();
            }

            OnPressed?.Invoke(_pressed);
        }

        private void UpdateVisuals()
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

            if (_pressed)
            {
                _image.color = PressedColor.Button * (_baseImageColor * _colorMultiplier);
                _label.color = PressedColor.Text * (_baseTextColor * _colorMultiplier);
            }

            if (!_pressed)
            {
                _image.color = NormalColor.Button * (_baseImageColor * _colorMultiplier);
                _label.color = NormalColor.Text * (_baseTextColor * _colorMultiplier);
            }

            if (Highlighted && !_pressed)
            {
                _image.color = HighlightedColor.Button * (_baseImageColor * _colorMultiplier);
                _label.color = HighlightedColor.Text * (_baseTextColor * _colorMultiplier);
            }
        }
    }
}
