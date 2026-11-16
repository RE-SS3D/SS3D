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
        private readonly ToggleSwitch _toggle;
        private bool _isOn = true;
        private float _loadKw;

        public ChannelRow()
        {
            AddToClassList("channel-row");

            _dot = new VisualElement();
            _dot.AddToClassList("channel-row__dot");

            VisualElement textColumn = new();
            textColumn.AddToClassList("channel-row__text");

            _nameLabel = new Label("Channel");
            _nameLabel.AddToClassList("channel-row__name");
            _nameLabel.AddToClassList("font-body");

            _loadLabel = new Label("0.0 kW");
            _loadLabel.AddToClassList("channel-row__load");
            _loadLabel.AddToClassList("font-terminal");

            textColumn.Add(_nameLabel);
            textColumn.Add(_loadLabel);

            _toggle = new ToggleSwitch { OnLabel = "ON", OffLabel = "OFF" };
            _toggle.ValueChanged += value => SetOn(value, notify: true);

            Add(_dot);
            Add(textColumn);
            Add(_toggle);
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

        private void SetOn(bool value, bool notify)
        {
            _isOn = value;
            _toggle.IsOn = value;
            EnableInClassList("channel-row--off", !value);
            _dot.EnableInClassList("channel-row__dot--off", !value);

            if (notify)
            {
                ValueChanged?.Invoke(value);
            }
        }
    }
}
