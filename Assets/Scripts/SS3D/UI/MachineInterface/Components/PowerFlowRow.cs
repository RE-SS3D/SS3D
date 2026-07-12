using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    [UxmlElement]
    public partial class PowerFlowRow : VisualElement
    {
        private readonly Label _gridValue;
        private readonly Label _loadValue;
        private float _gridKw;
        private float _loadKw;

        public PowerFlowRow()
        {
            AddToClassList("power-flow-row");

            VisualElement gridMetric = CreateMetric("GRID IN", out _gridValue, alignEnd: false);
            Label arrow = new("→");
            arrow.AddToClassList("power-flow-row__arrow");
            arrow.AddToClassList("font-arcade");
            VisualElement loadMetric = CreateMetric("LOAD OUT", out _loadValue, alignEnd: true);

            Add(gridMetric);
            Add(arrow);
            Add(loadMetric);
        }

        [UxmlAttribute]
        public float GridInputKw
        {
            get => _gridKw;
            set
            {
                _gridKw = value;
                _gridValue.text = value.ToString("0.0");
            }
        }

        [UxmlAttribute]
        public float LoadOutputKw
        {
            get => _loadKw;
            set
            {
                _loadKw = value;
                _loadValue.text = value.ToString("0.0");
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

            VisualElement valueRow = new();
            valueRow.AddToClassList("power-flow-row__value-row");

            valueLabel = new Label("0.0");
            valueLabel.AddToClassList("power-flow-row__value");
            valueLabel.AddToClassList("font-terminal");

            Label unit = new("kW");
            unit.AddToClassList("power-flow-row__unit");
            unit.AddToClassList("font-body");

            valueRow.Add(valueLabel);
            valueRow.Add(unit);

            metric.Add(label);
            metric.Add(valueRow);
            return metric;
        }
    }
}
