using System;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    [UxmlElement]
    public partial class ChannelRow : VisualElement
    {
        public event Action<bool> ValueChanged;

        private readonly VisualElement _dot;
        private readonly Label _nameLabel;
        private readonly Label _loadLabel;
        private readonly Button _toggleButton;
        private bool _isOn = true;
        private float _loadKw;

        public ChannelRow()
        {
            AddToClassList("channel-row");

            _dot = new VisualElement();
            _dot.AddToClassList("channel-row__dot");

            _nameLabel = new Label("CHANNEL");
            _nameLabel.AddToClassList("channel-row__name");
            _nameLabel.AddToClassList("font-arcade");

            _loadLabel = new Label("0.0 kW");
            _loadLabel.AddToClassList("channel-row__load");
            _loadLabel.AddToClassList("font-terminal");

            _toggleButton = new Button(OnToggleClicked) { text = "ON" };
            _toggleButton.AddToClassList("channel-row__toggle");
            _toggleButton.AddToClassList("channel-row__toggle--on");
            _toggleButton.AddToClassList("font-arcade");

            Add(_dot);
            Add(_nameLabel);
            Add(_loadLabel);
            Add(_toggleButton);
        }

        [UxmlAttribute]
        public string ChannelName
        {
            get => _nameLabel.text;
            set => _nameLabel.text = value;
        }

        [UxmlAttribute]
        public float LoadKw
        {
            get => _loadKw;
            set
            {
                _loadKw = value;
                _loadLabel.text = $"{value:0.0} kW";
            }
        }

        [UxmlAttribute]
        public bool IsOn
        {
            get => _isOn;
            set => SetOn(value, notify: false);
        }

        private void OnToggleClicked()
        {
            SetOn(!_isOn, notify: true);
        }

        private void SetOn(bool value, bool notify)
        {
            _isOn = value;
            _toggleButton.text = value ? "ON" : "OFF";
            _toggleButton.EnableInClassList("channel-row__toggle--on", value);
            EnableInClassList("channel-row--off", !value);
            _dot.EnableInClassList("channel-row__dot--off", !value);

            if (notify)
            {
                ValueChanged?.Invoke(value);
            }
        }
    }
}
