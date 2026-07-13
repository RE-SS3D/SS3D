using System;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    [UxmlElement]
    public partial class AirAlarmDeviceDetailPanel : VisualElement
    {
        public event Action CloseRequested;

        public event Action<bool> PowerChanged;

        public event Action<float> TargetDeltaRequested;

        private readonly Label _deviceName;
        private readonly Label _deviceType;
        private readonly Button _closeButton;
        private readonly ToggleSwitch _powerToggle;
        private readonly VisualElement _ventControls;
        private readonly NumericStepper _targetStepper;
        private readonly VisualElement _scrubberControls;
        private readonly CompactFilterToggle _o2Filter;
        private readonly CompactFilterToggle _n2Filter;
        private readonly CompactFilterToggle _co2Filter;
        private readonly CompactFilterToggle _plasmaFilter;
        private readonly CompactFilterToggle _toxinsFilter;
        private bool _controlsLocked;

        public AirAlarmDeviceDetailPanel()
        {
            AddToClassList("air-alarm-device-detail");

            VisualElement header = new();
            header.AddToClassList("air-alarm-device-detail__header");

            VisualElement titles = new();
            titles.AddToClassList("air-alarm-device-detail__titles");

            _deviceName = new Label("Device");
            _deviceName.AddToClassList("air-alarm-device-detail__name");
            _deviceName.AddToClassList("font-titling");

            _deviceType = new Label("Vent unit");
            _deviceType.AddToClassList("air-alarm-device-detail__type");
            _deviceType.AddToClassList("font-terminal");

            titles.Add(_deviceName);
            titles.Add(_deviceType);

            _closeButton = new Button(() => CloseRequested?.Invoke()) { text = "✕" };
            _closeButton.AddToClassList("air-alarm-device-detail__close");
            _closeButton.AddToClassList("font-terminal");

            header.Add(titles);
            header.Add(_closeButton);

            VisualElement body = new();
            body.AddToClassList("air-alarm-device-detail__body");

            VisualElement powerColumn = new();
            powerColumn.AddToClassList("air-alarm-device-detail__power-column");

            _powerToggle = new ToggleSwitch { OnLabel = "ON", OffLabel = "OFF" };
            _powerToggle.ValueChanged += value => PowerChanged?.Invoke(value);

            Label powerCaption = new Label("Pwr");
            powerCaption.AddToClassList("air-alarm-device-detail__power-caption");
            powerCaption.AddToClassList("font-arcade");

            powerColumn.Add(_powerToggle);
            powerColumn.Add(powerCaption);

            _ventControls = new VisualElement();
            _ventControls.AddToClassList("air-alarm-device-detail__vent-controls");

            Label ventHint = new Label("External pressure target");
            ventHint.AddToClassList("air-alarm-device-detail__vent-hint");
            ventHint.AddToClassList("font-body");

            _targetStepper = new NumericStepper();
            _targetStepper.DeltaRequested += delta => TargetDeltaRequested?.Invoke(delta);

            _ventControls.Add(ventHint);
            _ventControls.Add(_targetStepper);

            _scrubberControls = new VisualElement();
            _scrubberControls.AddToClassList("air-alarm-device-detail__scrubber-controls");

            Label scrubberHint = new Label("Filtering for");
            scrubberHint.AddToClassList("air-alarm-device-detail__scrubber-hint");
            scrubberHint.AddToClassList("font-body");

            VisualElement filterRow = new();
            filterRow.AddToClassList("air-alarm-device-detail__filters");

            _o2Filter = CreateFilter("O2");
            _n2Filter = CreateFilter("N2");
            _co2Filter = CreateFilter("CO2");
            _plasmaFilter = CreateFilter("Plasma");
            _toxinsFilter = CreateFilter("Toxins");

            filterRow.Add(_o2Filter);
            filterRow.Add(_n2Filter);
            filterRow.Add(_co2Filter);
            filterRow.Add(_plasmaFilter);
            filterRow.Add(_toxinsFilter);

            _scrubberControls.Add(scrubberHint);
            _scrubberControls.Add(filterRow);

            body.Add(powerColumn);
            body.Add(_ventControls);
            body.Add(_scrubberControls);

            Add(header);
            Add(body);

            SetDeviceKind(AirAlarmDeviceKind.Vent);
        }

        [UxmlAttribute]
        public string DeviceName
        {
            get => _deviceName.text;
            set => _deviceName.text = value;
        }

        [UxmlAttribute]
        public string DeviceTypeLabel
        {
            get => _deviceType.text;
            set => _deviceType.text = value;
        }

        [UxmlAttribute]
        public AirAlarmDeviceKind DeviceKind
        {
            get => AirAlarmDeviceKind.Vent;
            set => SetDeviceKind(value);
        }

        [UxmlAttribute]
        public bool Powered
        {
            get => _powerToggle.IsOn;
            set => _powerToggle.IsOn = value;
        }

        [UxmlAttribute]
        public string TargetText
        {
            get => _targetStepper.ValueText;
            set => _targetStepper.ValueText = value;
        }

        [UxmlAttribute]
        public bool ControlsLocked
        {
            get => _controlsLocked;
            set
            {
                _controlsLocked = value;
                EnableInClassList("air-alarm-device-detail--locked", value);
                _targetStepper.Locked = value;
                _o2Filter.Locked = value;
                _n2Filter.Locked = value;
                _co2Filter.Locked = value;
                _plasmaFilter.Locked = value;
                _toxinsFilter.Locked = value;
            }
        }

        public void SetFilterState(string key, bool enabled)
        {
            CompactFilterToggle toggle = key switch
            {
                "O2" => _o2Filter,
                "N2" => _n2Filter,
                "CO2" => _co2Filter,
                "Plasma" => _plasmaFilter,
                "Toxins" => _toxinsFilter,
                _ => null,
            };

            if (toggle != null)
            {
                toggle.IsOn = enabled;
            }
        }

        private void SetDeviceKind(AirAlarmDeviceKind kind)
        {
            _ventControls.style.display = kind == AirAlarmDeviceKind.Vent ? DisplayStyle.Flex : DisplayStyle.None;
            _scrubberControls.style.display = kind == AirAlarmDeviceKind.Scrubber ? DisplayStyle.Flex : DisplayStyle.None;
            DeviceTypeLabel = kind == AirAlarmDeviceKind.Vent ? "Vent unit" : "Scrubber unit";
        }

        private static CompactFilterToggle CreateFilter(string label)
        {
            CompactFilterToggle toggle = new() { FilterLabel = label };
            toggle.style.marginRight = 14;
            return toggle;
        }
    }

    public enum AirAlarmDeviceKind
    {
        Vent = 0,
        Scrubber = 1,
    }
}
