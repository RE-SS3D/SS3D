using System;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    [UxmlElement]
    public partial class ConnectedDeviceRow : VisualElement
    {
        public event Action Clicked;

        private readonly StatusDot _dot;
        private readonly Label _typeLabel;
        private readonly Label _nameLabel;
        private readonly Label _stateLabel;

        public ConnectedDeviceRow()
        {
            AddToClassList("connected-device-row");
            RegisterCallback<ClickEvent>(_ => Clicked?.Invoke());

            VisualElement left = new();
            left.AddToClassList("connected-device-row__left");

            _dot = new StatusDot { Tone = StatusTone.Success };

            _typeLabel = new Label("VENT");
            _typeLabel.AddToClassList("connected-device-row__type");
            _typeLabel.AddToClassList("font-terminal");

            _nameLabel = new Label("Device");
            _nameLabel.AddToClassList("connected-device-row__name");
            _nameLabel.AddToClassList("font-body");

            left.Add(_dot);
            left.Add(_typeLabel);
            left.Add(_nameLabel);

            _stateLabel = new Label("Online");
            _stateLabel.AddToClassList("connected-device-row__state");
            _stateLabel.AddToClassList("font-terminal");

            Add(left);
            Add(_stateLabel);
        }

        [UxmlAttribute]
        public string TypeLabel
        {
            get => _typeLabel.text;
            set => _typeLabel.text = value;
        }

        [UxmlAttribute]
        public string DeviceName
        {
            get => _nameLabel.text;
            set => _nameLabel.text = value;
        }

        [UxmlAttribute]
        public string StateText
        {
            get => _stateLabel.text;
            set => _stateLabel.text = value;
        }

        [UxmlAttribute]
        public bool Powered
        {
            get => true;
            set
            {
                _dot.Tone = value ? StatusTone.Success : StatusTone.Neutral;
                StatusToneUtility.ApplyTone(_stateLabel, value ? StatusTone.Success : StatusTone.Neutral);
            }
        }

        [UxmlAttribute]
        public bool Selected
        {
            get => ClassListContains("connected-device-row--selected");
            set => EnableInClassList("connected-device-row--selected", value);
        }
    }
}
