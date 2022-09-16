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

        private readonly ButtonTextColorPair _normalColor = UIStyles.LabelButton.NormalColor;
        private readonly ButtonTextColorPair _highlightedColor = UIStyles.LabelButton.HighlightedColor;
        private readonly ButtonTextColorPair _pressedColor = UIStyles.LabelButton.PressedColor;
        private readonly ButtonTextColorPair _disabledColor = UIStyles.LabelButton.DisabledColor;

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
                _image.color = _disabledColor.Button;
                _label.color = _disabledColor.Text;
                return;
            }

            if (!Pressed)
            {
                _image.color = _normalColor.Button;
                _label.color = _normalColor.Text;
            }

            if (_hovered && !Pressed)
            {
                _image.color = _highlightedColor.Button;
                _label.color = _highlightedColor.Text;
                return;
            }

            if (Pressed)
            {
                _image.color = _pressedColor.Button;
                _label.color = _pressedColor.Text;
            }
        }
    }
}
