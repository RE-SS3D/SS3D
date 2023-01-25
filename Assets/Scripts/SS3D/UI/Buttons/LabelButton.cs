using System;
using SS3D.Core;
using SS3D.Core.Behaviours;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SS3D.UI.Buttons
{
    /// <summary>
    /// Custom SS3D button, works similarly as Unity's
    /// </summary>
    [AddComponentMenu("| SS3D/UI/Label Button")]
    public class LabelButton : Actor, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler, IPointerDownHandler
    {
        public event Action<bool, MouseButtonType> OnPressed;
        public event Action<bool> OnPressedDown; 
        public event Action<bool> OnPressedUp; 

        public event Action<bool> OnHighlightChanged;
        public event Action<bool> OnDisabledChanged;

        [Header("States")] 
        [SerializeField]
        protected bool _pressed;
        [SerializeField] private bool _disabled;
        [Header("Debug Info")]
        [SerializeField] private bool _highlighted;

        [Header("Style")]
        [SerializeField]
        protected ButtonStyleAsset _buttonStyle;

        [Header("Colors")]
        [SerializeField]
        protected float _colorMultiplier = 1f;
        [SerializeField] protected Color _baseImageColor = Color.white;
        [SerializeField] protected Color _baseTextColor = Color.white;

        [Header("UI Elements")]
        [SerializeField]
        protected TMP_Text _label;
        [SerializeField] protected Image _image;

        public bool Pressed
        {
            get => _pressed;
            set
            {
                _pressed = value;
                UpdateVisuals();
            }
        }

        public bool Disabled
        {
            get => _disabled;
            set
            {
                _disabled = value;
                OnDisabledChanged?.Invoke(_disabled);
                UpdateVisuals();
            }
        }

        public bool Highlighted
        {
            get => _highlighted;
            set
            {
                _highlighted = value; 
                OnHighlightChanged?.Invoke(_highlighted);
            }
        }

        protected ButtonTextColorPair NormalColor => _buttonStyle.NormalColor;
        protected ButtonTextColorPair NormalHighlightedColor => _buttonStyle.NormalHighlightedColor;
        protected ButtonTextColorPair PressedColor => _buttonStyle.PressedColor;
        protected ButtonTextColorPair PressedHighlightedColor => _buttonStyle.PressedHighlightedColor;
        protected ButtonTextColorPair DisabledColor => _buttonStyle.DisabledColor;

        private void OnValidate()
        {
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

        public virtual void OnPointerDown(PointerEventData eventData)
        {
            if (Disabled)
            {
                return;
            }

            _pressed = true;
            ProcessPress(MouseButtonType.MouseDown);
        }

        public virtual void OnPointerUp(PointerEventData eventData)
        {
            if (Disabled)
            {
                return;
            }

            _pressed = false;
            ProcessPress(MouseButtonType.MouseUp);
        }

        public void Press()
        {
            OnPointerDown(null);
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

        protected void ProcessPress(MouseButtonType eventData)
        {
            if (_pressed)
            {
                Select(); 
            }
            else
            {
                Deselect();
            }

            OnPressed?.Invoke(_pressed, eventData);

            switch (eventData)
            {
                case MouseButtonType.MouseDown:
                    OnPressedDown?.Invoke(_pressed);
                    break;
                default:
                    OnPressedUp?.Invoke(_pressed);
                    break;
            }
        }

        protected virtual void UpdateVisuals()
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
            }

            if (!_pressed && !Highlighted)
            {
                _image.color = NormalColor.Button * (_baseImageColor * _colorMultiplier);
                _label.color = NormalColor.Text * (_baseTextColor * _colorMultiplier);
            }

            if (_pressed && Highlighted)
            {
                _image.color = PressedHighlightedColor.Button * (_baseImageColor * _colorMultiplier);
                _label.color = PressedHighlightedColor.Text * (_baseTextColor * _colorMultiplier);
            }

            if (!_pressed && Highlighted)
            {
                _image.color = NormalHighlightedColor.Button * (_baseImageColor * _colorMultiplier);
                _label.color = NormalHighlightedColor.Text * (_baseTextColor * _colorMultiplier);
            }
        }
    }
}
