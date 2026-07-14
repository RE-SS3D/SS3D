using System;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    [UxmlElement]
    public partial class ToggleSwitch : VisualElement
    {
        public event Action<bool> ValueChanged;

        private readonly Label _stateLabel;
        private readonly VisualElement _track;
        private readonly VisualElement _thumb;
        private bool _isOn = true;
        private bool _horizontal;

        public ToggleSwitch()
        {
            AddToClassList("toggle-switch");

            _stateLabel = new Label("ON");
            _stateLabel.AddToClassList("toggle-switch__label");
            _stateLabel.AddToClassList("font-arcade");

            _track = new VisualElement();
            _track.AddToClassList("toggle-switch__track");
            _track.RegisterCallback<ClickEvent>(_ => Toggle());

            _thumb = new VisualElement();
            _thumb.AddToClassList("toggle-switch__thumb");
            _thumb.pickingMode = PickingMode.Ignore;
            _track.Add(_thumb);

            Add(_stateLabel);
            Add(_track);

            SetOn(true, notify: false);
        }

        [UxmlAttribute]
        public bool Horizontal
        {
            get => _horizontal;
            set
            {
                _horizontal = value;
                EnableInClassList("toggle-switch--horizontal", value);
            }
        }

        [UxmlAttribute]
        public bool IsOn
        {
            get => _isOn;
            set => SetOn(value, notify: false);
        }

        [UxmlAttribute]
        public string OnLabel { get; set; } = "ON";

        [UxmlAttribute]
        public string OffLabel { get; set; } = "OFF";

        [UxmlAttribute]
        public bool Locked
        {
            get => ClassListContains("toggle-switch--locked");
            set
            {
                EnableInClassList("toggle-switch--locked", value);
                _track.pickingMode = value ? PickingMode.Ignore : PickingMode.Position;
            }
        }

        private void Toggle()
        {
            SetOn(!_isOn, notify: true);
        }

        private void SetOn(bool value, bool notify)
        {
            _isOn = value;
            _stateLabel.text = value ? OnLabel : OffLabel;
            EnableInClassList("toggle-switch--on", value);
            _track.EnableInClassList("toggle-switch__track--on", value);

            if (notify)
            {
                ValueChanged?.Invoke(value);
            }
        }
    }
}
