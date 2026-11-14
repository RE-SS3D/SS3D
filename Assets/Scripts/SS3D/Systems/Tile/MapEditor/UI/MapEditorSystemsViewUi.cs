using System;
using System.Collections.Generic;
using SS3D.Systems.Tile.MapEditor.SystemsView;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.Systems.Tile.MapEditor.UI
{
    /// <summary>
    /// Systems view chrome (creative-mode §4): the Atmos / Power toolbar toggle and the collapsible legend. The trace
    /// panel and network labels join here in later phases (Documents/architecture/2026-11_map-editor-systems-views.md Q4).
    /// </summary>
    public sealed partial class MapEditorView
    {
        private readonly Dictionary<SystemsViewKind, Button> _systemsViewButtons = new();
        private VisualElement _systemsLegend;
        private bool _systemsLegendOpen = SystemsViewLegend.LoadOpen();
        private SystemsViewKind _renderedLegendView = SystemsViewKind.Off;
        private bool _renderedLegendOpen;
        private bool _legendRendered;

        /// <summary>The toolbar toggle: a view to pin on, or Off.</summary>
        public event Action<SystemsViewKind> SystemsViewSelected;

        private void AddSystemsViewToggle(VisualElement toolbar)
        {
            AddSeparator(toolbar, vertical: true);
            AddSystemsViewButton(toolbar, SystemsViewKind.Atmos, MapEditorSubcategory.GasPipes, "Atmos view");
            AddSystemsViewButton(toolbar, SystemsViewKind.Power, MapEditorSubcategory.Cables, "Power view");
        }

        private void AddSystemsViewButton(VisualElement toolbar, SystemsViewKind view, MapEditorSubcategory iconOf, string hint)
        {
            // Pressing the view that is on turns it off; the other view's button switches straight to it.
            Button button = CreateIconButton(_icons?.GetSubcategoryIcon(iconOf), null,
                () => SystemsViewSelected?.Invoke(_vm.ActiveSystemsView == view ? SystemsViewKind.Off : view));
            button.name = $"systems-view-{view.ToString().ToLowerInvariant()}";
            _systemsViewButtons[view] = button;
            toolbar.Add(WrapWithHoverHint(button, hint, opensLeft: true));
        }

        private void BuildSystemsLegend(VisualElement column)
        {
            _systemsLegend = new VisualElement { name = "systems-legend", pickingMode = PickingMode.Position };
            _systemsLegend.AddToClassList("map-editor-systems-legend");
            _systemsLegend.style.display = DisplayStyle.None;
            column.Add(_systemsLegend);
        }

        private void RefreshSystemsView()
        {
            SystemsViewKind view = _vm.ActiveSystemsView;
            foreach (KeyValuePair<SystemsViewKind, Button> pair in _systemsViewButtons)
                pair.Value.EnableInClassList("map-editor-icon-btn--active", pair.Key == view);

            if (_systemsLegend == null
                || (_legendRendered && _renderedLegendView == view && _renderedLegendOpen == _systemsLegendOpen))
                return;

            _legendRendered = true;
            _renderedLegendView = view;
            _renderedLegendOpen = _systemsLegendOpen;
            _systemsLegend.Clear();
            _systemsLegend.style.display = view == SystemsViewKind.Off ? DisplayStyle.None : DisplayStyle.Flex;
            if (view == SystemsViewKind.Off)
                return;

            _systemsLegend.EnableInClassList("map-editor-systems-legend--collapsed", !_systemsLegendOpen);
            VisualElement header = new() { pickingMode = PickingMode.Position };
            header.AddToClassList("map-editor-systems-legend__header");
            Label title = new(SystemsViewLegend.TitleFor(view).ToUpperInvariant()) { pickingMode = PickingMode.Ignore };
            title.AddToClassList("map-editor-systems-legend__title");
            header.Add(title);
            header.Add(CreateHeaderIconButton(_systemsLegendOpen ? _icons?.PanelExpand : _icons?.PanelCollapse,
                _systemsLegendOpen ? "Hide legend" : "Show legend", () => SetSystemsLegendOpen(!_systemsLegendOpen)));
            _systemsLegend.Add(header);
            if (!_systemsLegendOpen)
                return;

            // Scrolls when the screen is too short for every line, rather than running under the library.
            ScrollView body = new(ScrollViewMode.Vertical) { horizontalScrollerVisibility = ScrollerVisibility.Hidden };
            body.AddToClassList("map-editor-systems-legend__body");
            foreach (SystemsViewLegendEntry entry in SystemsViewLegend.EntriesFor(view))
                body.Add(LegendRow(entry));
            _systemsLegend.Add(body);
        }

        /// <summary>Collapses or opens the legend and remembers it on this machine.</summary>
        public void SetSystemsLegendOpen(bool open)
        {
            _systemsLegendOpen = open;
            SystemsViewLegend.SaveOpen(open);
            RefreshSystemsView();
        }

        private static VisualElement LegendRow(SystemsViewLegendEntry entry)
        {
            VisualElement row = new() { pickingMode = PickingMode.Ignore };
            row.AddToClassList("map-editor-systems-legend__row");
            VisualElement swatches = new() { pickingMode = PickingMode.Ignore };
            swatches.AddToClassList("map-editor-systems-legend__swatches");
            foreach (Color color in entry.Swatches)
            {
                VisualElement swatch = new() { pickingMode = PickingMode.Ignore };
                swatch.AddToClassList("map-editor-systems-legend__swatch");
                swatch.style.backgroundColor = color;
                swatches.Add(swatch);
            }

            row.Add(swatches);
            Label text = new(entry.Text) { pickingMode = PickingMode.Ignore };
            text.AddToClassList("map-editor-systems-legend__text");
            row.Add(text);
            return row;
        }
    }
}
