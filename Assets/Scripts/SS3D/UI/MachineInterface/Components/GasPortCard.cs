using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    /// <summary>
    /// One port of a filter or mixer: label, pressure, mix bar, mix text, and a capacity bar for an
    /// outlet that can fill.
    /// </summary>
    [UxmlElement]
    public partial class GasPortCard : VisualElement
    {
        private readonly Label _label;
        private readonly Label _pressure;
        private readonly GasMixBar _mixBar;
        private readonly Label _mixText;
        private readonly VisualElement _capacity;
        private readonly Label _capacityText;
        private readonly VisualElement _capacityFill;

        public GasPortCard()
        {
            AddToClassList("gas-port-card");

            VisualElement header = new();
            header.AddToClassList("gas-port-card__header");

            _label = new Label("Port");
            _label.AddToClassList("gas-port-card__label");
            _label.AddToClassList("font-arcade");

            _pressure = new Label("0.0 kPa");
            _pressure.AddToClassList("gas-port-card__pressure");
            _pressure.AddToClassList("font-terminal");

            header.Add(_label);
            header.Add(_pressure);

            _mixBar = new GasMixBar();

            _mixText = new Label("No gas");
            _mixText.AddToClassList("gas-port-card__mix");
            _mixText.AddToClassList("font-terminal");

            _capacity = new VisualElement();
            _capacity.AddToClassList("gas-port-card__capacity");
            _capacity.style.display = DisplayStyle.None;

            VisualElement capacityHeader = new();
            capacityHeader.AddToClassList("gas-port-card__header");

            Label capacityLabel = new("Capacity");
            capacityLabel.AddToClassList("gas-port-card__capacity-label");
            capacityLabel.AddToClassList("font-arcade");

            _capacityText = new Label("0%");
            _capacityText.AddToClassList("gas-port-card__capacity-text");
            _capacityText.AddToClassList("font-terminal");

            capacityHeader.Add(capacityLabel);
            capacityHeader.Add(_capacityText);

            VisualElement capacityTrack = new();
            capacityTrack.AddToClassList("gas-port-card__capacity-track");

            _capacityFill = new VisualElement();
            _capacityFill.AddToClassList("gas-port-card__capacity-fill");
            capacityTrack.Add(_capacityFill);

            _capacity.Add(capacityHeader);
            _capacity.Add(capacityTrack);

            Add(header);
            Add(_mixBar);
            Add(_mixText);
            Add(_capacity);
        }

        /// <summary>Raised cards are outlets; inset ones are inlets.</summary>
        [UxmlAttribute]
        public bool Raised
        {
            get => ClassListContains("gas-port-card--raised");
            set => EnableInClassList("gas-port-card--raised", value);
        }

        public void Bind(GasPortView port)
        {
            _label.text = port.Label;
            _pressure.text = port.PressureText;
            EnableInClassList("gas-port-card--warn", port.Warn);
            _mixBar.SetSegments(port.Segments);
            _mixText.text = port.MixText;
            _mixText.EnableInClassList("gas-port-card__mix--warn", port.MixTone == StatusTone.Warning);

            _capacity.style.display = port.HasCapacity ? DisplayStyle.Flex : DisplayStyle.None;
            _capacityText.text = port.CapacityText;
            _capacityFill.style.width = new Length(port.CapacityPercent, LengthUnit.Percent);
            _capacity.EnableInClassList("gas-port-card__capacity--full", port.CapacityFull);
        }
    }
}
