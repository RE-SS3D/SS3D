using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    [UxmlElement]
    public partial class PowerFlowRow : VisualElement
    {
        private readonly Label _gridValue;
        private readonly Label _loadValue;
        private readonly Label _arrow;
        private float _gridKw;
        private float _loadKw;

        public PowerFlowRow()
        {
            AddToClassList("power-flow-row");

            VisualElement gridMetric = CreateMetric("GRID IN", out _gridValue, alignEnd: false);
            gridMetric.AddToClassList("power-flow-row__metric--grid-in");
            _arrow = new Label("→");
            _arrow.AddToClassList("power-flow-row__arrow");
            _arrow.AddToClassList("font-arcade");
            VisualElement loadMetric = CreateMetric("LOAD OUT", out _loadValue, alignEnd: true);
            loadMetric.AddToClassList("power-flow-row__metric--load-out");

            Add(gridMetric);
            Add(_arrow);
            Add(loadMetric);
        }

        [UxmlAttribute]
        public float GridInputKw
        {
            get => _gridKw;
            set
            {
                _gridKw = value;
                _gridValue.text = $"{value:0.0} kW";
            }
        }

        [UxmlAttribute]
        public float LoadOutputKw
        {
            get => _loadKw;
            set
            {
                _loadKw = value;
                _loadValue.text = $"{value:0.0} kW";
            }
        }

        private static VisualElement CreateMetric(string labelText, out Label valueLabel, bool alignEnd)
        {
            VisualElement metric = new();
            metric.AddToClassList("power-flow-row__metric");
            if (alignEnd)
            {
                metric.AddToClassList("power-flow-row__metric--output");
            }

            Label label = new(labelText);
            label.AddToClassList("power-flow-row__label");
            label.AddToClassList("font-arcade");

            valueLabel = new Label("0.0 kW");
            valueLabel.AddToClassList("power-flow-row__value");
            valueLabel.AddToClassList("font-terminal");

            metric.Add(label);
            metric.Add(valueLabel);
            return metric;
        }

        public void SetFlowDisplay(string arrowGlyph, StatusTone arrowTone, StatusTone loadTone)
        {
            _arrow.text = arrowGlyph;
            StatusToneUtility.ApplyTone(_arrow, arrowTone);

            _loadValue.RemoveFromClassList("tone-info");
            _loadValue.RemoveFromClassList("tone-success");
            _loadValue.RemoveFromClassList("tone-warning");
            _loadValue.RemoveFromClassList("tone-danger");
            _loadValue.RemoveFromClassList("tone-neutral");

            if (loadTone == StatusTone.Neutral)
            {
                _loadValue.style.color = StyleKeyword.Null;
            }
            else
            {
                StatusToneUtility.ApplyTone(_loadValue, loadTone);
                _loadValue.style.color = StyleKeyword.Null;
            }
        }
    }
}
