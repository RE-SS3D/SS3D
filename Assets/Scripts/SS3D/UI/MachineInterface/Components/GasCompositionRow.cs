using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    /// <summary>
    /// One gas on a meter panel: swatch, names, share and partial pressure.
    /// </summary>
    public sealed class GasCompositionRow : VisualElement
    {
        private readonly VisualElement _swatch;
        private readonly Label _symbol;
        private readonly Label _name;
        private readonly Label _percent;
        private readonly Label _pressure;

        public GasCompositionRow()
        {
            AddToClassList("gas-composition-row");

            _swatch = new VisualElement();
            _swatch.AddToClassList("gas-composition-row__swatch");

            _symbol = new Label();
            _symbol.AddToClassList("gas-composition-row__symbol");
            _symbol.AddToClassList("font-terminal");

            _name = new Label();
            _name.AddToClassList("gas-composition-row__name");
            _name.AddToClassList("font-body");

            _percent = new Label();
            _percent.AddToClassList("gas-composition-row__percent");
            _percent.AddToClassList("font-terminal");

            _pressure = new Label();
            _pressure.AddToClassList("gas-composition-row__pressure");
            _pressure.AddToClassList("font-terminal");

            Add(_swatch);
            Add(_symbol);
            Add(_name);
            Add(_percent);
            Add(_pressure);
        }

        public void Bind(GasCompositionRowView row)
        {
            _swatch.style.backgroundColor = row.Color;
            _symbol.text = row.Symbol;
            _name.text = row.Name;
            _percent.text = row.PercentText;
            _pressure.text = row.PressureText;
            EnableInClassList("gas-composition-row--absent", !row.Present);
            EnableInClassList("gas-composition-row--hazard", row.Hazard);
        }
    }
}
