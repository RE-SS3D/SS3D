using System;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    [UxmlElement]
    public partial class DiegeticDeviceShell : VisualElement
    {
        public event Action CloseClicked;

        public const float DefaultWidth = 664f;

        private readonly Label _modelLabel;
        private readonly StatusDot _powerDot;
        private readonly Label _powerLabel;
        private readonly VisualElement _screen;
        private readonly ScrollView _scrollView;
        private readonly VisualElement _screenContent;
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
            _screen.name = "screen-surface";
            _screen.style.position = Position.Relative;
            _screen.style.flexGrow = 1;
            _screen.style.flexShrink = 1;
            _screen.style.minHeight = 0;

            _scrollView = new ScrollView(ScrollViewMode.Vertical);
            _scrollView.AddToClassList("diegetic-device-shell__scroll");
            _scrollView.name = "screen-scroll";
            _scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _scrollView.verticalScrollerVisibility = ScrollerVisibility.Auto;
            _scrollView.style.flexGrow = 1;
            _scrollView.style.flexShrink = 1;
            _scrollView.style.minHeight = 0;

            _screenContent = _scrollView.contentContainer;
            _screenContent.AddToClassList("diegetic-device-shell__screen-content");
            _screenContent.name = "screen-content";
            _screenContent.style.flexDirection = FlexDirection.Column;
            _screenContent.style.flexGrow = 0;
            _screenContent.style.flexShrink = 0;

            _screen.Add(_scrollView);

            bezel.Add(_screen);

            hierarchy.Add(chassisHeader);
            hierarchy.Add(bezel);
        }

        public override VisualElement contentContainer => _screenContent;

        public VisualElement Screen => _screen;

        public ScrollView ScreenScroll => _scrollView;

        public VisualElement ScreenContent => _screenContent;

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
