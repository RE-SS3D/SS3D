using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    [UxmlElement]
    public partial class ConnectionStatusRow : VisualElement
    {
        private readonly StatusDot _statusDot;
        private readonly Label _statusLabel;
        private readonly Label _readoutLabel;

        public ConnectionStatusRow()
        {
            AddToClassList("connection-status-row");

            VisualElement left = new();
            left.AddToClassList("connection-status-row__left");

            _statusDot = new StatusDot { Tone = StatusTone.Info };
            _statusLabel = new Label("WIRED · DATA BUS · PORT J1");
            _statusLabel.AddToClassList("connection-status-row__status");
            _statusLabel.AddToClassList("font-terminal");

            left.Add(_statusDot);
            left.Add(_statusLabel);

            _readoutLabel = new Label();
            _readoutLabel.AddToClassList("connection-status-row__readout");
            _readoutLabel.AddToClassList("font-terminal");

            Add(left);
            Add(_readoutLabel);
        }

        [UxmlAttribute]
        public string StatusText
        {
            get => _statusLabel.text;
            set => _statusLabel.text = value;
        }

        [UxmlAttribute]
        public string ReadoutText
        {
            get => _readoutLabel.text;
            set => _readoutLabel.text = value;
        }

        [UxmlAttribute]
        public StatusTone DotTone
        {
            get => _statusDot.Tone;
            set => _statusDot.Tone = value;
        }
    }
}
