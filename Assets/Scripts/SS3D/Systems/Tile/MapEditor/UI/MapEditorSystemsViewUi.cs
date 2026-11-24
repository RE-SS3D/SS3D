using System;
using System.Collections.Generic;
using SS3D.Systems.Tile.MapEditor.SystemsView;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.Systems.Tile.MapEditor.UI
{
    /// <summary>
    /// Systems view chrome (creative-mode §4): the Atmos / Power toggles in the view tools (top left), the sim bar and
    /// collapsible legend under the camera column (top right), and beside the view tools the trace panel with its world label (<see cref="MapEditorSystemsTracePanel"/>; Documents/architecture/systems/map-editor-systems-views.md, Drawing).
    /// </summary>
    public sealed partial class MapEditorView
    {
        private readonly Dictionary<SystemsViewKind, Button> _systemsViewButtons = new();
        private VisualElement _systemsLegend;
        private bool _systemsLegendOpen = SystemsViewLegend.LoadOpen();
        private SystemsViewKind _renderedLegendView = SystemsViewKind.Off;
        private bool _renderedLegendOpen;
        private bool _legendRendered;

        private readonly MapEditorSystemsTracePanel _systemsTrace = new();
        private readonly MapEditorSystemsSimBar _systemsSim = new();

        /// <summary>The Room air sub-toggle (Atmos view only).</summary>
        public event Action SystemsRoomAirToggled
        {
            add => _systemsSim.RoomAirToggled += value;
            remove => _systemsSim.RoomAirToggled -= value;
        }

        /// <summary>An admin's Pause, Resume or Step.</summary>
        public event Action<SystemsViewSimulationAction> SystemsSimulationRequested
        {
            add => _systemsSim.SimulationRequested += value;
            remove => _systemsSim.SimulationRequested -= value;
        }

        /// <summary>The toolbar toggle: a view to pin on, or Off.</summary>
        public event Action<SystemsViewKind> SystemsViewSelected;

        /// <summary>Locate on a traced network's device: the tile to frame.</summary>
        public event Action<Vector2Int> SystemsTraceLocateRequested
        {
            add => _systemsTrace.LocateRequested += value;
            remove => _systemsTrace.LocateRequested -= value;
        }

        /// <summary>The trace panel's Clear.</summary>
        public event Action SystemsTraceCloseRequested
        {
            add => _systemsTrace.CloseRequested += value;
            remove => _systemsTrace.CloseRequested -= value;
        }

        /// <summary>Every frame while open: the trace panel and its world label follow the client's trace.</summary>
        public void UpdateSystemsTrace(MapEditorSystemsViewClient client, Camera camera) => _systemsTrace.Update(client, _root, camera);

        /// <summary>The Atmos and Power toggles: their own panel in the view tools.</summary>
        private void AddSystemsViewToggle(VisualElement region)
        {
            VisualElement strip = CreateToolbarStrip();
            AddSystemsViewButton(strip, SystemsViewKind.Atmos, MapEditorSubcategory.GasPipes, "Atmos view");
            AddSystemsViewButton(strip, SystemsViewKind.Power, MapEditorSubcategory.Cables, "Power view");
            region.Add(strip);
        }

        private void AddSystemsViewButton(VisualElement toolbar, SystemsViewKind view, MapEditorSubcategory iconOf, string hint)
        {
            // Pressing the view that is on turns it off; the other view's button switches straight to it.
            Button button = CreateIconButton(_icons?.GetSubcategoryIcon(iconOf), null,
                () => SystemsViewSelected?.Invoke(_vm.ActiveSystemsView == view ? SystemsViewKind.Off : view));
            button.name = $"systems-view-{view.ToString().ToLowerInvariant()}";
            _systemsViewButtons[view] = button;
            toolbar.Add(MapEditorChrome.WithHint(button, hint, MapEditorPopoverSide.Right));
        }

        private void BuildSystemsLegend(VisualElement column)
        {
            _systemsLegend = new VisualElement { name = "systems-legend", pickingMode = PickingMode.Position };
            _systemsLegend.AddToClassList("map-editor-systems-legend");
            _systemsLegend.style.display = DisplayStyle.None;
            _systemsSim.Build(column);
            column.Add(_systemsLegend);
        }

        private void RefreshSystemsView()
        {
            SystemsViewKind view = _vm.ActiveSystemsView;
            foreach (KeyValuePair<SystemsViewKind, Button> pair in _systemsViewButtons)
                pair.Value.EnableInClassList("map-editor-icon-btn--active", pair.Key == view);
            _systemsSim.Refresh(_vm);

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
