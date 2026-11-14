using System;
using System.Collections.Generic;
using System.Linq;
using SS3D.Systems.Inputs;
using SS3D.Systems.Settings;
using UnityEngine;
using UnityEngine.UIElements;
using static SS3D.UI.SettingsPanel.SettingsUi;

namespace SS3D.UI.SettingsPanel
{
    /// <summary>
    /// The Controls tab (Decisions 9–11): a legend, then one column per group. A rebindable row is a button that
    /// starts key capture in <see cref="PlayerSettingsSubSystem"/>; a fixed row is shown for reference only.
    /// Built fresh each time the tab opens; rows update in place after that.
    /// </summary>
    internal sealed class SettingsControlsTab
    {
        private const string MapEditorGroup = "Map Editor";
        private const int FlashMilliseconds = 1600;

        private sealed class BindView
        {
            public Button Button;
            public VisualElement Dot;
            public Label Hint;
            public Label Key;
            public Label Prefix;
        }

        private readonly PlayerSettingsSubSystem _settings;
        private readonly Dictionary<string, BindView> _binds = new();
        private readonly Dictionary<string, string> _shownPaths = new();
        private readonly VisualElement _bindGrid;
        private string _rowSignature;
        private bool _pulseDim;

        public SettingsControlsTab(PlayerSettingsSubSystem settings, VisualElement body)
        {
            _settings = settings;
            VisualElement legend = Element(body, "settings-panel__legend");
            DashedBorder(legend);
            VisualElement rebind = Element(legend, "settings-panel__legend-item");
            Text(rebind, "Q", "settings-panel__mono settings-panel__keycap settings-panel__keycap--small");
            Text(rebind, "Rebindable: click the row", "settings-panel__legend-text");
            VisualElement chord = Element(legend, "settings-panel__legend-item");
            Text(chord, "Ctrl +", "settings-panel__mono settings-panel__prefix");
            Text(chord, "Q", "settings-panel__mono settings-panel__keycap settings-panel__keycap--small");
            Text(chord, "Ctrl is fixed, the key is yours", "settings-panel__legend-text");
            VisualElement fixedKey = Element(legend, "settings-panel__legend-item");
            fixedKey.Add(LockIcon());
            Text(fixedKey, "Fixed: shown for reference, can’t be changed", "settings-panel__legend-text");

            _bindGrid = Element(body, "settings-panel__bind-grid");
        }

        /// <summary>Set while the panel resets the bindings, so the reset doesn't flash every moved row.</summary>
        public bool SuppressFlash { get; set; }

        public void Refresh()
        {
            IReadOnlyList<KeybindRow> rows = _settings.KeybindRows.Where(r => r.Group != null).ToList();
            string signature = string.Join("|", rows.Select(r => r.Id));
            if (signature != _rowSignature) RebuildBinds(rows, signature);

            string capturing = _settings.CapturingRowId;
            List<string> moved = new();
            foreach (KeybindRow row in rows)
            {
                if (!_binds.TryGetValue(row.Id, out BindView view)) continue;
                bool listening = row.Id == capturing;
                if (_shownPaths.TryGetValue(row.Id, out string shown) && !SamePath(shown, row.Path)) moved.Add(row.Id);
                _shownPaths[row.Id] = row.Path;

                view.Key.text = listening ? "Press a key" : KeybindRules.DisplayName(row.Path, null);
                view.Button.EnableInClassList("settings-panel__bind--listening", listening);
                view.Key.EnableInClassList("settings-panel__keycap--listening", listening);
                if (!listening) view.Key.RemoveFromClassList("settings-panel__keycap--dim");
                string hint = listening ? "Esc or click again to cancel" : row.Hint;
                view.Hint.text = hint ?? "";
                view.Hint.style.display = string.IsNullOrEmpty(hint) ? DisplayStyle.None : DisplayStyle.Flex;
                view.Hint.EnableInClassList("settings-panel__bind-hint--listening", listening);
                view.Dot.style.display = SamePath(row.Path, row.DefaultPath) ? DisplayStyle.None : DisplayStyle.Flex;
            }

            if (moved.Count == 0 || SuppressFlash) return;
            foreach (string id in moved) _binds[id].Button.AddToClassList("settings-panel__bind--flash");
            _bindGrid.schedule.Execute(() =>
            {
                foreach (string id in moved)
                    if (_binds.TryGetValue(id, out BindView view)) view.Button.RemoveFromClassList("settings-panel__bind--flash");
            }).StartingIn(FlashMilliseconds);
        }

        private void RebuildBinds(IReadOnlyList<KeybindRow> rows, string signature)
        {
            _rowSignature = signature;
            _binds.Clear();
            _shownPaths.Clear();
            _bindGrid.Clear();
            foreach (IGrouping<string, KeybindRow> group in rows.GroupBy(r => r.Group))
            {
                VisualElement column = Element(_bindGrid, "settings-panel__bind-group");
                VisualElement heading = Element(column, "settings-panel__group-heading");
                Text(heading, group.Key.ToUpperInvariant(), "settings-panel__group-title");
                if (group.Key == MapEditorGroup) Text(heading, "Only while the editor is open", "settings-panel__group-sub");
                foreach (KeybindRow row in group)
                {
                    if (row.ReadOnly) FixedRow(column, row);
                    else _binds[row.Id] = BindRow(column, row);
                }
            }
        }

        private BindView BindRow(VisualElement column, KeybindRow row)
        {
            string id = row.Id;
            Button button = new(() =>
            {
                if (_settings.CapturingRowId == id) _settings.CancelKeyCapture();
                else _settings.BeginKeyCapture(id);
            }) { name = "bind-" + id };
            button.AddToClassList("settings-panel__bind");
            button.userData = id;
            column.Add(button);

            BindView view = new() { Button = button };
            VisualElement copy = Element(button, "settings-panel__bind-copy");
            VisualElement line = Element(copy, "settings-panel__row-line");
            Text(line, row.Label, "settings-panel__bind-label");
            view.Dot = Element(line, "settings-panel__changed");
            view.Dot.tooltip = "Changed from default";
            view.Hint = Text(copy, "", "settings-panel__bind-hint");
            VisualElement keys = Element(button, "settings-panel__bind-keys");
            if (!string.IsNullOrEmpty(row.ModifierPath))
                view.Prefix = Text(keys, KeybindRules.DisplayName(row.ModifierPath, null) + " +", "settings-panel__mono settings-panel__prefix");
            view.Key = Text(keys, "", "settings-panel__mono settings-panel__keycap");
            foreach (VisualElement child in button.Children()) SetPickingIgnore(child);
            return view;
        }

        private static void FixedRow(VisualElement column, KeybindRow row)
        {
            VisualElement line = Element(column, "settings-panel__fixed");
            line.name = "bind-" + row.Id;
            VisualElement left = Element(line, "settings-panel__fixed-label");
            left.Add(LockIcon());
            Text(left, row.Label, "settings-panel__fixed-text");
            Text(line, row.FixedText ?? KeybindRules.DisplayName(row.Path, row.ModifierPath), "settings-panel__mono settings-panel__fixed-key");
        }

        /// <summary>Left-clicking anywhere but the listening row cancels capture (the design's "click elsewhere").</summary>
        public void HandlePointerDown(PointerDownEvent evt)
        {
            string capturing = _settings.CapturingRowId;
            if (capturing == null || evt.button != 0) return;
            for (VisualElement e = evt.target as VisualElement; e != null; e = e.parent)
                if (e.userData is string id && id == capturing) return;
            _settings.CancelKeyCapture();
        }

        public void PulseListening()
        {
            string capturing = _settings.CapturingRowId;
            if (capturing == null || !_binds.TryGetValue(capturing, out BindView view)) return;
            _pulseDim = !_pulseDim;
            view.Key.EnableInClassList("settings-panel__keycap--dim", _pulseDim);
        }

        private static void SetPickingIgnore(VisualElement element)
        {
            element.pickingMode = PickingMode.Ignore;
            foreach (VisualElement child in element.Children()) SetPickingIgnore(child);
        }

        private static bool SamePath(string a, string b) =>
            string.Equals(a ?? string.Empty, b ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }
}
