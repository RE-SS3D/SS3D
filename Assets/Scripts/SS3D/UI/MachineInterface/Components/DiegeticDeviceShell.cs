using System;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    [UxmlElement]
    public partial class DiegeticDeviceShell : VisualElement
    {
        public const float DefaultWidth = 664f;

        public event Action CloseClicked;

        private readonly Label _modelLabel;
        private readonly StatusDot _powerDot;
        private readonly Label _powerLabel;
        private readonly VisualElement _screen;
        private readonly Button _closeButton;

        public DiegeticDeviceShell()
        {
            AddToClassList("diegetic-device-shell");

            VisualElement chassisHeader = new();
            chassisHeader.AddToClassList("diegetic-device-shell__chassis-header");

            VisualElement modelGroup = new();
            modelGroup.AddToClassList("diegetic-device-shell__model-group");

            _modelLabel = new Label("DEVICE · FIELD UNIT");
            _modelLabel.AddToClassList("diegetic-device-shell__model-label");
            _modelLabel.AddToClassList("font-arcade");

            modelGroup.Add(_modelLabel);

            VisualElement powerGroup = new();
            powerGroup.AddToClassList("diegetic-device-shell__power-group");

            _powerDot = new StatusDot { Tone = StatusTone.Success };
            _powerLabel = new Label("PWR OK");
            _powerLabel.AddToClassList("diegetic-device-shell__power-label");
            _powerLabel.AddToClassList("font-arcade");

            powerGroup.Add(_powerDot);
            powerGroup.Add(_powerLabel);

            _closeButton = new Button(OnCloseClicked) { text = "×" };
            _closeButton.AddToClassList("diegetic-device-shell__close");

            chassisHeader.Add(modelGroup);
            chassisHeader.Add(powerGroup);
            chassisHeader.Add(_closeButton);

            VisualElement bezel = new();
            bezel.AddToClassList("diegetic-device-shell__bezel");

            _screen = new VisualElement();
            _screen.AddToClassList("diegetic-device-shell__screen");
            _screen.name = "screen-content";

            bezel.Add(_screen);

            Add(chassisHeader);
            Add(bezel);
        }

        public override VisualElement contentContainer => _screen;

        public VisualElement Screen => _screen;

        [UxmlAttribute]
        public string ModelLabel
        {
            get => _modelLabel.text;
            set => _modelLabel.text = value;
        }

        [UxmlAttribute]
        public bool PowerOk
        {
            get => _powerDot.Tone == StatusTone.Success;
            set
            {
                _powerDot.Tone = value ? StatusTone.Success : StatusTone.Danger;
                _powerLabel.text = value ? "PWR OK" : "NO PWR";
            }
        }

        private void OnCloseClicked()
        {
            CloseClicked?.Invoke();
        }
    }
}
