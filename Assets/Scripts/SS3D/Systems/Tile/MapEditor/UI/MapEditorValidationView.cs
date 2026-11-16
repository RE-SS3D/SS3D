using System;
using System.Collections.Generic;
using System.Linq;
using SS3D.Systems.Tile.Validation;
using UnityEngine.UIElements;

namespace SS3D.Systems.Tile.MapEditor.UI
{
    public sealed partial class MapEditorView
    {
        private VisualElement _mapCheckColumn;
        private VisualElement _mapCheck;
        private readonly HashSet<string> _ignoredWarnings = new();
        private string _issueFilter = "all";
        private string _expandedIssue;
        private bool _checkCollapsed;
        private int _renderedValidationRevision = -1;
        private int _renderedValidationContext = -1;
        private bool _renderedValidationStale;

        public event Action<MapValidationIssue> ValidationLocateRequested;
        /// <summary>Show in palette: search for the issue's <see cref="MapValidationIssue.EntryKey"/>.</summary>
        public event Action<string> ValidationShowInPaletteRequested;

        private void BuildMapCheck(VisualElement column)
        {
            _mapCheckColumn = column;
            _mapCheck = new VisualElement { name = "map-check", pickingMode = PickingMode.Position };
            _mapCheck.AddToClassList("map-check");
            // The header is rebuilt on every refresh, so the drag lives on the panel and matches the header by class.
            _mapCheck.AddManipulator(new PanelDragManipulator(_mapCheck,
                pressed => pressed.ClassListContains("map-check__header")));
            column.Add(_mapCheck);
        }

        private void RefreshMapCheck(bool force = false)
        {
            if (_mapCheck == null || (!force && _renderedValidationRevision == _vm.ValidationRevision
                && _renderedValidationStale == _vm.ValidationStale))
                return;

            if (_renderedValidationContext != _vm.ValidationContext)
            {
                _ignoredWarnings.Clear();
                _expandedIssue = null;
                _renderedValidationContext = _vm.ValidationContext;
            }
            _renderedValidationRevision = _vm.ValidationRevision;
            _renderedValidationStale = _vm.ValidationStale;
            MapValidationIssue[] live = _vm.ValidationIssues
                .Where(issue => !issue.IsWarning || !_ignoredWarnings.Contains(issue.Id)).ToArray();
            int errors = live.Count(issue => !issue.IsWarning);
            int warnings = live.Length - errors;
            // Issues start closed: a long list is read by title, and one opens on click.

            _mapCheck.Clear();
            _mapCheck.EnableInClassList("map-check--collapsed", _checkCollapsed);
            VisualElement header = CheckElement("map-check__header");
            if (!_checkCollapsed)
                header.Add(CheckLabel("MAP CHECK", "map-check__title"));
            if (errors > 0)
                header.Add(CheckLabel(_checkCollapsed ? errors.ToString() : $"{errors} {(errors == 1 ? "Error" : "Errors")}", "map-check__badge map-check__danger"));
            if (warnings > 0)
                header.Add(CheckLabel(warnings.ToString(), "map-check__badge map-check__warning"));
            if (live.Length == 0)
                header.Add(_vm.ValidationStale || !_vm.ValidationChecked
                    ? CheckLabel("Checking", "map-check__badge")
                    : CheckLabel("Clear", "map-check__badge map-check__success"));
            header.Add(CreateHeaderIconButton(_checkCollapsed ? _icons?.PanelCollapse : _icons?.PanelExpand,
                _checkCollapsed ? "Expand Map Check" : "Collapse Map Check", () =>
                {
                    _checkCollapsed = !_checkCollapsed;
                    RefreshMapCheck(true);
                }));
            _mapCheck.Add(header);
            if (_checkCollapsed)
                return;

            VisualElement filters = CheckElement("map-check__filters");
            AddIssueFilter(filters, "all", "All", live.Length);
            AddIssueFilter(filters, "error", "Errors", errors);
            AddIssueFilter(filters, "warning", "Warnings", warnings);
            _mapCheck.Add(filters);
            ScrollView scroll = new() { name = "map-check-issues" };
            scroll.AddToClassList("map-check__issues");
            scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            foreach (MapValidationIssue issue in live.Where(issue => _issueFilter == "all"
                || (_issueFilter == "warning") == issue.IsWarning))
                scroll.Add(BuildIssueRow(issue));
            if (scroll.contentContainer.childCount == 0)
                scroll.Add(CheckLabel(_vm.ValidationChecked ? "No issues." : "Checking the map...", "map-check__empty"));
            _mapCheck.Add(scroll);

            VisualElement footer = CheckElement("map-check__footer");
            string status = !_vm.ValidationChecked || _vm.ValidationStale ? "Checking the map..."
                : errors > 0 ? "Errors block round start." : "Map is playable.";
            Label statusLabel = CheckLabel(status, "map-check__status");
            statusLabel.tooltip = $"Game mode: {_vm.ValidationMode}. Saving is always allowed. Ignoring warnings only hides them in this editor.";
            statusLabel.EnableInClassList("map-check__danger", errors > 0);
            footer.Add(statusLabel);
            int ignored = _vm.ValidationIssues.Count(issue => issue.IsWarning && _ignoredWarnings.Contains(issue.Id));
            if (ignored > 0)
                footer.Add(CheckButton($"Unignore {ignored}", "map-check-unignore", () =>
                {
                    _ignoredWarnings.Clear();
                    RefreshMapCheck(true);
                }));
            _mapCheck.Add(footer);
        }

        private void AddIssueFilter(VisualElement parent, string key, string label, int count)
        {
            Button button = CheckButton($"{label}  {count}", "map-check-filter-" + key, () =>
            {
                _issueFilter = key;
                RefreshMapCheck(true);
            });
            button.AddToClassList("map-check__filter");
            button.EnableInClassList("map-check__filter--active", _issueFilter == key);
            parent.Add(button);
        }

        private VisualElement BuildIssueRow(MapValidationIssue issue)
        {
            bool open = _expandedIssue == issue.Id;
            VisualElement row = CheckElement("map-check__issue");
            row.name = "map-check-issue-" + issue.Id;
            row.EnableInClassList("map-check__issue--open", open);
            Button toggle = new(() =>
            {
                _expandedIssue = open ? null : issue.Id;
                RefreshMapCheck(true);
            }) { name = "map-check-toggle-" + issue.Id, tooltip = open ? "Collapse issue" : "Expand issue" };
            toggle.AddToClassList("map-check__issue-toggle");
            toggle.Add(CheckLabel("!", issue.IsWarning ? "map-check__warning" : "map-check__danger"));
            VisualElement heading = CheckElement("map-check__heading");
            heading.Add(CheckLabel(issue.Title, "map-check__issue-title"));
            heading.Add(CheckLabel(issue.Category.ToUpperInvariant(), "map-check__category"));
            toggle.Add(heading);
            toggle.Add(CreateIconElement(open ? _icons?.PanelExpand : _icons?.PanelCollapse));
            row.Add(toggle);
            if (open)
            {
                VisualElement detail = CheckElement("map-check__detail");
                detail.Add(CheckLabel(issue.Body, "map-check__body"));
                if (issue.Checks != null && issue.Checks.Length > 0)
                {
                    VisualElement checks = CheckElement("map-check__checks");
                    foreach (MapValidationCheck check in issue.Checks)
                    {
                        VisualElement line = CheckElement("map-check__check");
                        line.Add(CheckLabel(check.Label, "map-check__check-label"));
                        line.Add(CheckLabel(check.IsMet ? "OK" : "MISSING",
                            check.IsMet ? "map-check__success" : "map-check__danger"));
                        checks.Add(line);
                    }
                    detail.Add(checks);
                }
                VisualElement actions = CheckElement("map-check__actions");
                if (issue.HasPosition)
                {
                    Button locate = CheckButton("Locate", "map-check-locate-" + issue.Id,
                        () => ValidationLocateRequested?.Invoke(issue));
                    locate.tooltip = "Move the camera to this spot on the map";
                    actions.Add(locate);
                }
                if (!string.IsNullOrEmpty(issue.EntryKey))
                {
                    Button show = CheckButton("Show in palette", "map-check-show-" + issue.Id, () =>
                    {
                        if (_paletteMode == PaletteMode.Min)
                            TogglePaletteMode(PaletteMode.Min);
                        ValidationShowInPaletteRequested?.Invoke(issue.EntryKey);
                    });
                    show.tooltip = "Search the palette for what this needs";
                    actions.Add(show);
                }
                if (issue.IsWarning)
                    actions.Add(CheckButton("Ignore", "map-check-ignore-" + issue.Id, () =>
                    {
                        _ignoredWarnings.Add(issue.Id);
                        RefreshMapCheck(true);
                    }));
                detail.Add(actions);
                row.Add(detail);
            }
            return row;
        }

        private static VisualElement CheckElement(string style)
        {
            VisualElement element = new();
            element.AddToClassList(style);
            return element;
        }

        private static Label CheckLabel(string text, string styles)
        {
            Label label = new(text) { enableRichText = false, pickingMode = PickingMode.Ignore };
            foreach (string style in styles.Split(' ')) label.AddToClassList(style);
            return label;
        }

        private static Button CheckButton(string text, string name, Action action)
        {
            Button button = new(action) { text = text, name = name };
            button.AddToClassList("map-check__button");
            return button;
        }
    }
}
