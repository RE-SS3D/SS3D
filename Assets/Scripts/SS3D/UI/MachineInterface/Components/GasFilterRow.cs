using System;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    /// <summary>
    /// One gas on a filter panel: a toggle, its swatch and names, and its share of the inlet.
    /// </summary>
    public sealed class GasFilterRow : VisualElement
    {
        public event Action<byte> Toggled;

        private readonly VisualElement _swatch;
        private readonly Label _symbol;
        private readonly Label _name;
        private readonly Label _percent;
        private byte _gasId;
        private bool _locked;

        public GasFilterRow()
        {
            AddToClassList("gas-filter-row");

            VisualElement track = new();
            track.AddToClassList("gas-filter-row__track");
            track.pickingMode = PickingMode.Ignore;

            VisualElement thumb = new();
            thumb.AddToClassList("gas-filter-row__thumb");
            thumb.pickingMode = PickingMode.Ignore;
            track.Add(thumb);

            _swatch = new VisualElement();
            _swatch.AddToClassList("gas-filter-row__swatch");
            _swatch.pickingMode = PickingMode.Ignore;

            VisualElement names = new();
            names.AddToClassList("gas-filter-row__names");
            names.pickingMode = PickingMode.Ignore;

            _symbol = new Label();
            _symbol.AddToClassList("gas-filter-row__symbol");
            _symbol.AddToClassList("font-terminal");
            _symbol.pickingMode = PickingMode.Ignore;

            _name = new Label();
            _name.AddToClassList("gas-filter-row__name");
            _name.AddToClassList("font-body");
            _name.pickingMode = PickingMode.Ignore;

            names.Add(_symbol);
            names.Add(_name);

            _percent = new Label();
            _percent.AddToClassList("gas-filter-row__percent");
            _percent.AddToClassList("font-terminal");
            _percent.pickingMode = PickingMode.Ignore;

            Add(track);
            Add(_swatch);
            Add(names);
            Add(_percent);

            RegisterCallback<ClickEvent>(_ =>
            {
                if (!_locked)
                {
                    Toggled?.Invoke(_gasId);
                }
            });
        }

        public void Bind(GasFilterRowView row, bool locked)
        {
            _gasId = row.Id;
            _locked = locked;
            _swatch.style.backgroundColor = row.Color;
            _symbol.text = row.Symbol;
            _name.text = row.Name;
            _percent.text = row.PercentText;
            EnableInClassList("gas-filter-row--on", row.Selected);
            EnableInClassList("gas-filter-row--present", row.Present);
            EnableInClassList("gas-filter-row--locked", locked);
        }
    }
}
