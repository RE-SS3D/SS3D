using System;
using System.Collections.Generic;
using System.Text;
using SS3D.Systems.Inputs;
using SS3D.Systems.Tile.MapEditor.SystemsView;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.Systems.Tile.MapEditor.UI
{
    /// <summary>
    /// The trace's UI Toolkit half (creative-mode §4 Tracing, Documents/architecture/2026-11_map-editor-systems-views.md
    /// Phase 2): a panel floating beside the top of the left column (the legend's) with each traced network's live rows
    /// and its devices with Locate, and a name label anchored in the world at the traced tile, the way room names are
    /// (<c>MapEditorAreaView.UpdateAreaLabels</c>). Domain-free: rows and devices come worded from the server, side
    /// names from the domain's style. Redraws only when <see cref="MapEditorSystemsViewClient.TraceRevision"/> moves.
    /// </summary>
    public sealed class MapEditorSystemsTracePanel
    {
        /// <summary>Lifts the label above the traced tile, so it doesn't cover the badge it names.</summary>
        private const float LabelLift = 0.6f;

        private VisualElement _panel;
        private VisualElement _labelsLayer;
        private VisualElement _label;
        private int _renderedRevision = -1;
        private string _renderedShape;
        // Every value label, in build order, for updates in place.
        private readonly List<Label> _values = new();

        public event Action<Vector2Int> LocateRequested;
        public event Action CloseRequested;

        public void Build(VisualElement column)
        {
            _panel = new VisualElement { name = "systems-trace", pickingMode = PickingMode.Position };
            _panel.AddToClassList("map-editor-systems-trace");
            _panel.style.display = DisplayStyle.None;
            column.Add(_panel);
            _renderedRevision = -1;
            _renderedShape = null;
        }

        /// <summary>Every frame while the editor is open: redraws the panel on a trace change and moves the label with the camera.</summary>
        public void Update(MapEditorSystemsViewClient client, VisualElement root, Camera camera)
        {
            if (_panel == null || client == null)
                return;

            if (_renderedRevision != client.TraceRevision)
            {
                _renderedRevision = client.TraceRevision;
                Render(client);
                RenderLabel(client, root);
            }

            PlaceLabel(client, root, camera);
        }

        /// <summary>
        /// A full rebuild when what is listed changed; otherwise only the values' text, so the numbers' 2 Hz updates
        /// don't reset the list's scroll or the pointer's hover.
        /// </summary>
        private void Render(MapEditorSystemsViewClient client)
        {
            string shape = Shape(client);
            if (shape == _renderedShape && TryUpdateValues(client))
                return;

            _renderedShape = shape;
            _values.Clear();
            _panel.Clear();
            _panel.style.display = client.IsTracing ? DisplayStyle.Flex : DisplayStyle.None;
            if (!client.IsTracing)
                return;

            SystemsViewLive? live = client.Live;
            VisualElement header = Element("map-editor-systems-trace__header");
            header.Add(Text("TRACE", "map-editor-systems-trace__title"));
            if (live is { Paused: true })
            {
                Label paused = Text("Paused", "map-editor-systems-trace__paused");
                paused.name = "systems-trace-paused";
                paused.tooltip = "The simulation is paused, so these numbers hold still.";
                header.Add(paused);
            }

            Button close = new(() => CloseRequested?.Invoke()) { name = "systems-trace-close", text = "Clear", tooltip = "Stop tracing" };
            close.AddToClassList("map-editor-systems-trace__button");
            header.Add(close);
            _panel.Add(header);

            ScrollView body = new(ScrollViewMode.Vertical) { horizontalScrollerVisibility = ScrollerVisibility.Hidden };
            body.AddToClassList("map-editor-systems-trace__body");
            int deviceIndex = 0;
            for (int i = 0; i < client.TraceSides.Count; i++)
            {
                SystemsViewTraceSide side = client.TraceSides[i];
                VisualElement section = Element("map-editor-systems-trace__section");
                section.name = "systems-trace-network-" + i;
                VisualElement heading = Element("map-editor-systems-trace__heading");
                VisualElement swatch = Element("map-editor-systems-legend__swatch");
                swatch.style.backgroundColor = client.ColorOf(side.ViewId);
                heading.Add(swatch);
                heading.Add(Text(client.TraceLabel(side), "map-editor-systems-trace__network"));
                section.Add(heading);

                SystemsViewLiveNetwork? network = Find(live, side.ViewId);
                if (network == null)
                {
                    section.Add(Text("Reading...", "map-editor-systems-trace__empty"));
                    body.Add(section);
                    continue;
                }

                foreach (SystemsViewLiveRow row in network.Value.Rows)
                {
                    VisualElement line = Element("map-editor-systems-trace__row");
                    line.Add(Text(row.Label, "map-editor-systems-trace__label"));
                    Label value = Text(row.Value, "map-editor-systems-trace__value");
                    _values.Add(value);
                    line.Add(value);
                    section.Add(line);
                }

                SystemsViewLiveDevice[] devices = network.Value.Devices;
                section.Add(Text(devices.Length == 0 ? "No devices attached." : $"Devices ({devices.Length})",
                    "map-editor-systems-trace__subtitle"));
                foreach (SystemsViewLiveDevice device in devices)
                    section.Add(DeviceRow(device, deviceIndex++));
                body.Add(section);
            }

            _panel.Add(body);
        }

        // What the panel lists, without the values: the sides and their names, each network's row labels and devices.
        private static string Shape(MapEditorSystemsViewClient client)
        {
            StringBuilder shape = new();
            shape.Append(client.IsTracing).Append(client.Live is { Paused: true }).Append('|');
            foreach (SystemsViewTraceSide side in client.TraceSides)
            {
                shape.Append(client.TraceLabel(side)).Append('#').Append(side.ViewId).Append('|');
                if (Find(client.Live, side.ViewId) is not { } network)
                {
                    shape.Append("reading|");
                    continue;
                }

                foreach (SystemsViewLiveRow row in network.Rows)
                    shape.Append(row.Label).Append(';');
                foreach (SystemsViewLiveDevice device in network.Devices)
                    shape.Append(device.Key).Append(device.Label).Append(';');
                shape.Append('|');
            }

            return shape.ToString();
        }

        // The values in the order the full build added them: each listed network's rows, then its devices' details.
        private bool TryUpdateValues(MapEditorSystemsViewClient client)
        {
            int index = 0;
            foreach (SystemsViewTraceSide side in client.TraceSides)
            {
                if (Find(client.Live, side.ViewId) is { } network)
                {
                    foreach (SystemsViewLiveRow row in network.Rows)
                    {
                        if (index >= _values.Count) return false;
                        _values[index++].text = row.Value;
                    }

                    foreach (SystemsViewLiveDevice device in network.Devices)
                    {
                        if (index >= _values.Count) return false;
                        _values[index++].text = device.Detail;
                    }
                }
            }

            return index == _values.Count;
        }

        private VisualElement DeviceRow(SystemsViewLiveDevice device, int index)
        {
            VisualElement row = Element("map-editor-systems-trace__device");
            VisualElement words = Element("map-editor-systems-trace__device-words");
            words.Add(Text(device.Label, "map-editor-systems-trace__device-name"));
            Label detail = Text(device.Detail, "map-editor-systems-trace__device-detail");
            _values.Add(detail);
            words.Add(detail);
            row.Add(words);
            Vector2Int tile = device.Key.Tile;
            Button locate = new(() => LocateRequested?.Invoke(tile))
            {
                name = "systems-trace-locate-" + index,
                text = "Locate",
                tooltip = "Move the camera to this device",
            };
            locate.AddToClassList("map-editor-systems-trace__button");
            row.Add(locate);
            return row;
        }

        // One label per trace, a line per side, each underlined in its network's colour.
        private void RenderLabel(MapEditorSystemsViewClient client, VisualElement root)
        {
            if (root == null)
                return;

            if (_labelsLayer == null)
            {
                _labelsLayer = new VisualElement { name = "systems-trace-labels", pickingMode = PickingMode.Ignore };
                _labelsLayer.style.position = Position.Absolute;
                _labelsLayer.style.left = _labelsLayer.style.top = 0;
                _labelsLayer.style.right = _labelsLayer.style.bottom = 0;
                _label = new VisualElement { name = "systems-trace-label", pickingMode = PickingMode.Ignore };
                _label.AddToClassList("map-editor-systems-trace-label");
                _labelsLayer.Add(_label);
            }

            // Under everything else on the root, like the room names; again if the view rebuilt its root.
            if (_labelsLayer.parent != root)
                root.Insert(0, _labelsLayer);

            _label.Clear();
            foreach (SystemsViewTraceSide side in client.TraceSides)
            {
                Label line = Text(client.TraceLabel(side), "map-editor-systems-trace-label__line");
                line.style.borderBottomColor = client.ColorOf(side.ViewId);
                _label.Add(line);
            }
        }

        private void PlaceLabel(MapEditorSystemsViewClient client, VisualElement root, Camera camera)
        {
            if (_labelsLayer == null)
                return;

            bool shown = client.IsTracing && camera != null && root?.panel != null;
            Vector3 screen = Vector3.zero;
            if (shown)
            {
                screen = camera.WorldToScreenPoint(new Vector3(client.TraceOrigin.x, LabelLift, client.TraceOrigin.y));
                shown = screen.z > 0 && camera.pixelRect.Contains((Vector2)screen);
            }

            _label.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
            if (!shown)
                return;

            Vector2 point = InputInterface.ScreenToPanel(root.panel, screen);
            _label.style.left = point.x;
            _label.style.top = point.y;
        }

        /// <summary>The label element, for tests and the visual pilot.</summary>
        public VisualElement Label => _label;

        private static SystemsViewLiveNetwork? Find(SystemsViewLive? live, int viewId) =>
            live is { } message && message.TryFind(viewId, out SystemsViewLiveNetwork network) ? network : null;

        private static VisualElement Element(string style)
        {
            VisualElement element = new() { pickingMode = PickingMode.Ignore };
            element.AddToClassList(style);
            return element;
        }

        private static Label Text(string text, string style)
        {
            Label label = new(text) { enableRichText = false, pickingMode = PickingMode.Ignore };
            label.AddToClassList(style);
            return label;
        }
    }
}
