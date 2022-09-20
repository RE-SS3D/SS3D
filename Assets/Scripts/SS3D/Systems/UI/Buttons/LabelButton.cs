using System;
using SS3D.Systems.UI.Buttons.Data;
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
    public class LabelButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public event Action<bool> OnPressed;

        [Header("States")]
        public bool Pressed;
        public bool Disabled;

        [SerializeField] private ButtonStyleAsset _buttonStyle;

        private ButtonTextColorPair NormalColor => _buttonStyle.NormalColor;
        private ButtonTextColorPair HighlightedColor => _buttonStyle.HighlightedColor;
        private ButtonTextColorPair PressedColor => _buttonStyle.PressedColor;
        private ButtonTextColorPair DisabledColor => _buttonStyle.DisabledColor;

        [Header("UI Elements")]
        [SerializeField] private TMP_Text _label;
        [SerializeField] private Image _image;

        [Header("Debug Info")]
        private bool _hovered;

        protected void OnValidate()
        {
            UpdateVisuals();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;

            if (Disabled)
            {
                return;
            }

            Highlight();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;

            if (Disabled)
            {
                return;
            }

            Unhighlight();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (Disabled)
            {
                return;
            }

            Press();
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

        private void Press()
        {
            Pressed = !Pressed;

            if (Pressed)
            {
                Select(); 
            }
            else
            {
                Deselect();
            }

            OnPressed?.Invoke(Pressed);
        }

        private void UpdateVisuals()
        {
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
                _image.color = DisabledColor.Button;
                _label.color = DisabledColor.Text;
                return;
            }

            if (!Pressed)
            {
                _image.color = NormalColor.Button;
                _label.color = NormalColor.Text;
            }

            if (_hovered && !Pressed)
            {
                _image.color = HighlightedColor.Button;
                _label.color = HighlightedColor.Text;
                return;
            }

            if (Pressed)
            {
                _image.color = PressedColor.Button;
                _label.color = PressedColor.Text;
            }
        }
    }
}
