using System;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    [UxmlElement]
    public partial class TogglePill : VisualElement
    {
        public event Action<bool> ValueChanged;

        private readonly Button _button;
        private bool _isOn = true;

        public TogglePill()
        {
            AddToClassList("toggle-pill");

            _button = new Button(OnClicked);
            _button.AddToClassList("toggle-pill__button");
            _button.AddToClassList("font-arcade");
            Add(_button);
            SetOn(true, notify: false);
        }

        [UxmlAttribute]
        public bool IsOn
        {
            get => _isOn;
            set => SetOn(value, notify: false);
        }

        [UxmlAttribute]
        public string OnLabel { get; set; } = "ENABLED";

        [UxmlAttribute]
        public string OffLabel { get; set; } = "DISABLED";

        private void OnClicked()
        {
            SetOn(!_isOn, notify: true);
        }

        private void SetOn(bool value, bool notify)
        {
            _isOn = value;
            _button.text = value ? OnLabel : OffLabel;
            EnableInClassList("toggle-pill--on", value);
            _button.EnableInClassList("toggle-pill__button--on", value);

            if (notify)
            {
                ValueChanged?.Invoke(value);
            }
        }
    }
}
