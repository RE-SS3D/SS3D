using System;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    public enum SteelButtonVariant
    {
        Steel,
        Blue,
        Danger,
    }

    [UxmlElement]
    public partial class SteelButton : VisualElement
    {
        public event Action Clicked;

        private readonly Button _button;
        private SteelButtonVariant _variant = SteelButtonVariant.Steel;

        public SteelButton()
        {
            AddToClassList("steel-button");
            pickingMode = PickingMode.Position;

            _button = new Button(OnClicked);
            _button.AddToClassList("steel-button__control");
            _button.AddToClassList("font-titling");
            Add(_button);
            ApplyVariant();
        }

        [UxmlAttribute]
        public string Text
        {
            get => _button.text;
            set => _button.text = value;
        }

        [UxmlAttribute]
        public bool Disabled
        {
            get => !_button.enabledSelf;
            set => _button.SetEnabled(!value);
        }

        [UxmlAttribute]
        public SteelButtonVariant Variant
        {
            get => _variant;
            set
            {
                _variant = value;
                ApplyVariant();
            }
        }

        private void ApplyVariant()
        {
            RemoveFromClassList("steel-button--blue");
            RemoveFromClassList("steel-button--danger");

            switch (_variant)
            {
                case SteelButtonVariant.Blue:
                    AddToClassList("steel-button--blue");
                    break;
                case SteelButtonVariant.Danger:
                    AddToClassList("steel-button--danger");
                    break;
            }
        }

        private void OnClicked()
        {
            Clicked?.Invoke();
        }
    }
}
