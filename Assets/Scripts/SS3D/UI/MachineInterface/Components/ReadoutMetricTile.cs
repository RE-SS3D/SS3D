using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    [UxmlElement]
    public partial class ReadoutMetricTile : VisualElement
    {
        private readonly Label _label;
        private readonly Label _value;

        public ReadoutMetricTile()
        {
            AddToClassList("readout-metric-tile");

            _label = new Label("METRIC");
            _label.AddToClassList("readout-metric-tile__label");
            _label.AddToClassList("font-arcade");

            _value = new Label("0.0");
            _value.AddToClassList("readout-metric-tile__value");
            _value.AddToClassList("font-terminal");

            Add(_label);
            Add(_value);
        }

        [UxmlAttribute]
        public string Label
        {
            get => _label.text;
            set => _label.text = value;
        }

        [UxmlAttribute]
        public string Value
        {
            get => _value.text;
            set => _value.text = value;
        }

        [UxmlAttribute]
        public StatusTone Tone
        {
            get => StatusTone.Info;
            set => StatusToneUtility.ApplyTone(_value, value);
        }
    }
}
