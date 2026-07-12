using System;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    [UxmlElement]
    public partial class SteelButton : VisualElement
    {
        public event Action Clicked;

        private readonly Button _button;

        public SteelButton()
        {
            AddToClassList("steel-button");

            _button = new Button(OnClicked);
            _button.AddToClassList("steel-button__control");
            _button.AddToClassList("font-titling");
            Add(_button);
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

        private void OnClicked()
        {
            Clicked?.Invoke();
        }
    }
}
