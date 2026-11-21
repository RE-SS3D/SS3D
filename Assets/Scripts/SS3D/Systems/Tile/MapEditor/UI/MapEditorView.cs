using SS3D.Data.AssetDatabases;
using SS3D.Systems.Inputs;
using SS3D.Systems.Tile.FloorVisuals;
using SS3D.Systems.Tile.MapEditor.Persistence;
using SS3D.Systems.Tile.GasMarkers;
using SS3D.Systems.Tile.SpawnPoints;
using SS3D.Systems.Tile.TileMapCreator;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace SS3D.Systems.Tile.MapEditor.UI
{
    /// <summary>
    /// UI Toolkit view for the full-screen map editor.
    /// </summary>
    public sealed partial class MapEditorView
    {
        /// <summary>
        /// Object library heights (px) for the three resize states: the header row alone, one row of slots, three. Each is
        /// the panel's 2 px border and 2 px padding top and bottom, the 22 px header and its 3 px gap, then the rows.
        /// </summary>
        private static readonly Dictionary<PaletteMode, float> PaletteHeights = new()
        {
            [PaletteMode.Min] = 33f,
            [PaletteMode.Normal] = 144f,
            [PaletteMode.Max] = 366f,
        };

        /// <summary>Object library grid row count per resize state (extra rows scroll).</summary>
        private const int NormalGridRows = 1;
        private const int MaxGridRows = 3;
        /// <summary>Palette cell (78) + horizontal margins (3 + 3).</summary>
        private const float GridSlotOuterWidthPx = 84f;
        /// <summary>Palette cell (78) + gap (3) + two-line name (27) + bottom margin (3).</summary>
        private const float GridSlotOuterHeightPx = 111f;
        /// <summary>Width of a one-row side heading (<c>.map-editor-grid-side-heading</c>: 24 + right margin 3).</summary>
        private const float GridSideHeadingOuterWidthPx = 27f;
        /// <summary>Map Check's popover: it opens from the warning cell under the view tools.</summary>
        private const string CheckPopover = "check";

        private readonly StyleSheet _styleSheet;
        private readonly MapEditorIconsSo _icons;
        private readonly MapEditorViewModel _vm;
        private readonly MapEditorCatalog _catalog;
        private readonly TileResourceLoader _loader;

        private VisualElement _root;
        private VisualElement _hudLayer;
        private VisualElement _toolBar;
        // Hidden when the server turns map persistence off (sandbox).
        private VisualElement _persistenceStrip;
        private VisualElement _viewTools;
        private MapEditorCameraDial _cameraDial;
        private VisualElement _modeRail;
        private VisualElement _subcatRow;
        private VisualElement _paletteBody;
        private VisualElement _paletteRegion;
        private ScrollView _gridScroll;
        private VisualElement _grid;
        private TextField _searchField;
        private TextField _saveNameField;
        private readonly InputTextEntryScope _textEntry = new();
        private Label _toast;
        private VisualElement _hint;
        private string _renderedHint;
        /// <summary>The button each popover opens from; its caret points back at it.</summary>
        private readonly Dictionary<string, VisualElement> _popoverAnchors = new();
        private VisualElement _activePopover;
        private VisualElement _popoverLayer;
        /// <summary>Where the player dragged each popover; reopening it keeps that spot for the session.</summary>
        private readonly Dictionary<string, Vector2> _popoverOffsets = new();

        private float _lastGridViewportWidth;
        private PaletteMode _paletteMode = PaletteMode.Normal;

        private readonly Dictionary<MapEditorTool, Button> _toolButtons = new();
        private readonly Dictionary<MapEditorMode, Button> _modeButtons = new();
        private readonly Dictionary<MapEditorSubcategory, Button> _subcatButtons = new();

        /// <summary>The colour-coding name of a tool (MapEditor.uss, "Colour coding"): Move draws as Select.</summary>
        private static string ToolColourName(MapEditorTool tool) => tool switch
        {
            MapEditorTool.Edit => "construct",
            MapEditorTool.Dropper => "dropper",
            MapEditorTool.Delete => "delete",
            MapEditorTool.Room => "room",
            _ => "select",
        };

        /// <summary>The colour-coding name of a category (MapEditor.uss, "Colour coding").</summary>
        private static string ModeColourName(MapEditorMode mode) => mode switch
        {
            MapEditorMode.Lower => "lower",
            MapEditorMode.Items => "items",
            MapEditorMode.Scripting => "scripts",
            _ => "upper",
        };

        private enum PaletteMode
        {
            Min,
            Normal,
            Max,
        }

        public MapEditorView(
            StyleSheet styleSheet,
            MapEditorIconsSo icons,
            MapEditorViewModel viewModel,
            MapEditorCatalog catalog,
            TileResourceLoader loader)
        {
            _styleSheet = styleSheet;
            _icons = icons;
            _vm = viewModel;
            _catalog = catalog;
            _loader = loader;
            _vm.StateChanged += Refresh;
        }

        public event Action<MapEditorTool> ToolSelected;
        public event Action UndoRequested;
        public event Action RedoRequested;
        public event Action<string> SaveAsRequested;
        public event Action<string> ExportRequested;
        public event Action OpenMapsFolderRequested;
        public event Action<string> LoadMapRequested;
        public event Action<string> DeleteMapRequested;
        public event Action ResetViewRequested;
        public event Action ShowUIRequested;
        public event Action<bool> DebugOverlayChanged;
        public event Action<bool> AlwaysPowerLightsChanged;
        public event Action<bool> InvincibleChanged;
        public event Action<float, float, float> CameraSettingsChanged;
        public event Action<int> CameraRotateRequested;
        public event Action<int> CameraZoomRequested;
        public event Action NewMapRequested;
        public event Action MapListRefreshRequested;
        public event Action<TileLayerCategory, bool> LayerCategoryVisibilityChanged;
        public event Action<MapEditorMode> ModeSelected;
        public event Action<MapEditorSubcategory> SubcategorySelected;
        public event Action<MapEditorCatalogEntry, GenericObjectSo> AssetSelected;
        public event Action<string> SearchChanged;

        public VisualElement Root => _root;

        public void Build(VisualElement parent)
        {
            _root = new VisualElement { name = "map-editor-root", pickingMode = PickingMode.Ignore };
            _root.AddToClassList("map-editor-root");
            _root.style.flexGrow = 1;

            if (_styleSheet != null)
                _root.styleSheets.Add(_styleSheet);

            _hudLayer = new VisualElement { name = "hud-layer", pickingMode = PickingMode.Ignore };
            _hudLayer.style.position = Position.Absolute;
            _hudLayer.style.left = 0;
            _hudLayer.style.top = 0;
            _hudLayer.style.right = 0;
            _hudLayer.style.bottom = 0;

            // The hint bar goes first: UITK paints in hierarchy order, so a tool's hint dropping over it paints on top.
            _hint = new VisualElement { name = "placement-hint", pickingMode = PickingMode.Ignore };
            _hint.AddToClassList("map-editor-hint");
            _hudLayer.Add(_hint);

            BuildViewTools();
            BuildToolBar();
            BuildCameraColumn();
            BuildBottomDock();
            BuildRevealButton();

            _toast = new Label { name = "toast", pickingMode = PickingMode.Ignore };
            _toast.AddToClassList("map-editor-toast");
            _toast.style.display = DisplayStyle.None;
            _root.Add(_toast);

            // Popovers live in their own last-child layer: UITK paints in hierarchy order and ignores z-index, so a
            // popover parented under its toolbar was painted beneath later siblings such as Map Check.
            _popoverLayer = new VisualElement { name = "popover-layer", pickingMode = PickingMode.Ignore };
            _popoverLayer.style.position = Position.Absolute;
            _popoverLayer.style.left = 0;
            _popoverLayer.style.top = 0;
            _popoverLayer.style.right = 0;
            _popoverLayer.style.bottom = 0;
            _hudLayer.Add(_popoverLayer);
            BuildMapCheck(_popoverLayer);

            _root.Add(_hudLayer);
            parent.Add(_root);
            Refresh();
        }

        public void Destroy()
        {
            _textEntry.Exit();
            _vm.StateChanged -= Refresh;
            _gridScroll?.UnregisterCallback<GeometryChangedEvent>(OnGridScrollGeometryChanged);
            _root?.RemoveFromHierarchy();
        }

        public void SetMouseOverUI(bool over) => _root?.EnableInClassList("map-editor-mouse-over", over);

        /// <summary>The tool bar, top centre: the tools, undo and redo, and the map's save and load.</summary>
        private void BuildToolBar()
        {
            _toolBar = CreateRegion("map-editor-region--top");

            VisualElement tools = CreateToolbarStrip(horizontal: true);
            AddToolButton(tools, MapEditorTool.Edit, "Construct", "1");
            AddToolButton(tools, MapEditorTool.Select, "Select", "2");
            AddToolButton(tools, MapEditorTool.Dropper, "Dropper", "3");
            AddToolButton(tools, MapEditorTool.Delete, "Delete", "4");
            AddToolButton(tools, MapEditorTool.Room, "Room", "5");
            _toolBar.Add(tools);

            VisualElement history = CreateToolbarStrip(horizontal: true);
            Button undo = CreateIconButton(_icons?.Undo, null, () => UndoRequested?.Invoke());
            undo.name = "undo-btn";
            history.Add(MapEditorChrome.WithHint(undo, "Undo", MapEditorPopoverSide.Down, "Ctrl", "Z"));
            Button redo = CreateIconButton(_icons?.Redo, null, () => RedoRequested?.Invoke());
            redo.name = "redo-btn";
            history.Add(MapEditorChrome.WithHint(redo, "Redo", MapEditorPopoverSide.Down, "Ctrl", "Y"));
            _toolBar.Add(history);

            _persistenceStrip = CreateToolbarStrip(horizontal: true);
            AddPopoverButton(_persistenceStrip, _icons?.SaveMap, "saveMenu", "Save map", MapEditorPopoverSide.Down, "Ctrl", "S");
            AddPopoverButton(_persistenceStrip, _icons?.OpenMap, "maps", "Load map", MapEditorPopoverSide.Down, "Ctrl", "Shift", "O");
            _toolBar.Add(_persistenceStrip);

            _hudLayer.Add(_toolBar);
        }

        /// <summary>The view tools, top left: layers and settings, the systems views, and Map Check's cell.</summary>
        private void BuildViewTools()
        {
            _viewTools = CreateRegion("map-editor-region--top-left");

            VisualElement view = CreateToolbarStrip();
            AddPopoverButton(view, _icons?.Layers, "layers", "Layers", MapEditorPopoverSide.Right);
            AddPopoverButton(view, _icons?.Settings, "settings", "Settings", MapEditorPopoverSide.Right);
            _viewTools.Add(view);

            AddSystemsViewToggle(_viewTools);
            BuildMapCheckButton(_viewTools);
            _systemsTrace.Build(_viewTools);
            _hudLayer.Add(_viewTools);
        }

        /// <summary>The camera column, top right: the dial and reset view, then the systems sim bar and legend.</summary>
        private void BuildCameraColumn()
        {
            VisualElement region = CreateRegion("map-editor-region--top-right");
            region.AddToClassList("map-editor-right-column");

            VisualElement camera = new() { pickingMode = PickingMode.Ignore };
            camera.AddToClassList("map-editor-camera-column");
            _cameraDial = new MapEditorCameraDial(_icons, () => TogglePopover("camera"));
            _cameraDial.RotateRequested += step => CameraRotateRequested?.Invoke(step);
            _cameraDial.ZoomRequested += step => CameraZoomRequested?.Invoke(step);
            _popoverAnchors["camera"] = _cameraDial;
            camera.Add(_cameraDial);

            VisualElement reset = CreateToolbarStrip();
            reset.Add(MapEditorChrome.WithHint(CreateIconButton(_icons?.ResetView, null, () => ResetViewRequested?.Invoke()),
                "Reset view", MapEditorPopoverSide.Left));
            camera.Add(reset);
            region.Add(camera);

            BuildSystemsLegend(region);
            _hudLayer.Add(region);
        }

        /// <summary>A cell that opens a popover; the popover's caret points back at it.</summary>
        private void AddPopoverButton(VisualElement strip, VectorImage icon, string popover, string label,
            MapEditorPopoverSide side, params string[] keys)
        {
            Button button = CreateIconButton(icon, null, () => TogglePopover(popover));
            button.name = popover + "-btn";
            VisualElement wrap = MapEditorChrome.WithHint(button, label, side, keys);
            _popoverAnchors[popover] = wrap;
            strip.Add(wrap);
        }

        /// <summary>
        /// The library, bottom: the mode rail (character setup's tabs) beside a panel holding the sub-tabs, search and
        /// resize buttons over the slot grid. No title: the tabs say where it is.
        /// </summary>
        private void BuildBottomDock()
        {
            VisualElement region = CreateRegion("map-editor-region--bottom");
            region.AddToClassList("map-editor-bottom-row");
            _paletteRegion = region;

            _modeRail = new VisualElement { pickingMode = PickingMode.Position };
            _modeRail.AddToClassList("map-editor-mode-rail");
            foreach (MapEditorMode mode in Enum.GetValues(typeof(MapEditorMode)))
            {
                Button tab = CreateModeTab(mode);
                _modeButtons[mode] = tab;
                _modeRail.Add(tab);
            }

            VisualElement library = new() { pickingMode = PickingMode.Position, name = "object-library" };
            library.AddToClassList("map-editor-library");

            VisualElement header = new() { pickingMode = PickingMode.Position };
            header.AddToClassList("map-editor-library-header");
            _subcatRow = new VisualElement { pickingMode = PickingMode.Position };
            _subcatRow.AddToClassList("map-editor-subcat-row");
            header.Add(_subcatRow);

            VisualElement search = new() { pickingMode = PickingMode.Position };
            search.AddToClassList("map-editor-search");
            VisualElement searchIcon = CreateIconElement(_icons?.Search);
            searchIcon.AddToClassList("map-editor-search-icon");
            search.Add(searchIcon);
            _searchField = new TextField { value = string.Empty };
            _searchField.AddToClassList("map-editor-search-field");
            _searchField.textEdition.placeholder = "Search every tab";
            _searchField.textEdition.hidePlaceholderOnFocus = true;
            _searchField.RegisterValueChangedCallback(evt => SearchChanged?.Invoke(evt.newValue));
            WireTextEntryField(_searchField);
            search.Add(_searchField);
            header.Add(search);

            VisualElement headerButtons = new() { pickingMode = PickingMode.Position };
            headerButtons.AddToClassList("map-editor-library-header__buttons");
            Button collapseBtn = CreateHeaderIconButton(_icons?.PanelCollapse, "Minimize panel",
                () => TogglePaletteMode(PaletteMode.Min));
            collapseBtn.name = "palette-collapse-btn";
            Button expandBtn = CreateHeaderIconButton(_icons?.PanelExpand, "Expand panel",
                () => TogglePaletteMode(PaletteMode.Max));
            expandBtn.name = "palette-expand-btn";
            headerButtons.Add(collapseBtn);
            headerButtons.Add(expandBtn);
            header.Add(headerButtons);

            _gridScroll = new ScrollView(ScrollViewMode.Vertical);
            _gridScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _gridScroll.verticalScrollerVisibility = ScrollerVisibility.Auto;
            _gridScroll.AddToClassList("map-editor-grid-scroll");
            _grid = new VisualElement();
            _grid.AddToClassList("map-editor-grid");
            _gridScroll.Add(_grid);
            _gridScroll.RegisterCallback<GeometryChangedEvent>(OnGridScrollGeometryChanged);

            _paletteBody = new VisualElement { pickingMode = PickingMode.Position };
            _paletteBody.AddToClassList("map-editor-library-body");
            _paletteBody.Add(_gridScroll);

            library.Add(header);
            library.Add(_paletteBody);
            region.Add(_modeRail);
            region.Add(library);
            _hudLayer.Add(region);
        }

        private void BuildRevealButton()
        {
            Button reveal = CreateIconButton(_icons?.ShowUi, "Show UI", () => ShowUIRequested?.Invoke());
            reveal.AddToClassList("map-editor-reveal-btn");
            reveal.name = "reveal-ui-btn";
            reveal.style.display = DisplayStyle.None;
            _root.Add(reveal);
        }

        private void WireTextEntryField(TextField field)
        {
            if (field == null)
                return;

            // The search plate outlines while its field has focus (USS has no :focus-within).
            field.RegisterCallback<FocusInEvent>(_ =>
            {
                _textEntry.Enter();
                field.parent?.EnableInClassList("map-editor-search--focus", true);
            });
            field.RegisterCallback<FocusOutEvent>(_ =>
            {
                _textEntry.Exit();
                field.parent?.EnableInClassList("map-editor-search--focus", false);
            });
        }

        private void TogglePaletteMode(PaletteMode extreme)
        {
            _paletteMode = _paletteMode == extreme ? PaletteMode.Normal : extreme;
            ApplyPaletteMode();
        }

        private void ApplyPaletteMode()
        {
            if (_paletteRegion == null)
                return;

            _paletteRegion.style.height = PaletteHeights[_paletteMode];
            // The toast sits 12 px above the library, whatever its size.
            if (_toast != null)
                _toast.style.bottom = 16 + PaletteHeights[_paletteMode] + 12;
            bool showBody = _paletteMode != PaletteMode.Min;
            _paletteBody.style.display = showBody ? DisplayStyle.Flex : DisplayStyle.None;
            _modeRail.style.display = showBody ? DisplayStyle.Flex : DisplayStyle.None;

            Button collapseBtn = _paletteRegion.Q<Button>("palette-collapse-btn");
            collapseBtn?.EnableInClassList("map-editor-header-icon-btn--active", _paletteMode == PaletteMode.Min);
            Button expandBtn = _paletteRegion.Q<Button>("palette-expand-btn");
            expandBtn?.EnableInClassList("map-editor-header-icon-btn--active", _paletteMode == PaletteMode.Max);
            if (collapseBtn != null)
                collapseBtn.tooltip = _paletteMode == PaletteMode.Min ? "Restore panel" : "Minimize panel";
            if (expandBtn != null)
                expandBtn.tooltip = _paletteMode == PaletteMode.Max ? "Restore panel" : "Expand panel";

            RebuildGrid();
            ApplyGridScrollHeight();
        }

        private void ApplyGridScrollHeight()
        {
            if (_gridScroll == null || _paletteMode == PaletteMode.Min)
                return;

            int visibleRows = _paletteMode == PaletteMode.Max ? MaxGridRows : NormalGridRows;
            _gridScroll.style.maxHeight = visibleRows * GridSlotOuterHeightPx;
        }

        private void Refresh()
        {
            if (_root == null)
                return;

            _hudLayer.style.display = _vm.HideUI ? DisplayStyle.None : DisplayStyle.Flex;
            Button reveal = _root.Q<Button>("reveal-ui-btn");
            if (reveal != null)
                reveal.style.display = _vm.HideUI ? DisplayStyle.Flex : DisplayStyle.None;

            foreach (KeyValuePair<MapEditorTool, Button> pair in _toolButtons)
            {
                pair.Value.EnableInClassList("map-editor-icon-btn--active", pair.Key == _vm.CurrentTool);
            }

            // Colour coding: the tool colours the hint bar's edge; the category colours the library's lines.
            foreach (MapEditorTool tool in Enum.GetValues(typeof(MapEditorTool)))
                _root.EnableInClassList("map-editor-root--tool-" + ToolColourName(tool), tool == _vm.CurrentTool);

            foreach (KeyValuePair<MapEditorMode, Button> pair in _modeButtons)
            {
                pair.Value.EnableInClassList("map-editor-mode-tab--active", pair.Key == _vm.CurrentMode);
                _paletteRegion?.EnableInClassList("map-editor-bottom-row--" + ModeColourName(pair.Key), pair.Key == _vm.CurrentMode);
            }

            if (_searchField != null && _searchField.value != _vm.SearchText)
                _searchField.SetValueWithoutNotify(_vm.SearchText ?? string.Empty);

            RebuildSubcategories();
            ApplyPaletteMode();

            if (_persistenceStrip != null)
                _persistenceStrip.style.display = _vm.Capabilities.Persistence ? DisplayStyle.Flex : DisplayStyle.None;

            Button undoBtn = _toolBar?.Q<Button>("undo-btn");
            Button redoBtn = _toolBar?.Q<Button>("redo-btn");
            undoBtn?.EnableInClassList("map-editor-icon-btn--disabled", _vm.UndoDepth <= 0);
            redoBtn?.EnableInClassList("map-editor-icon-btn--disabled", _vm.RedoDepth <= 0);

            RefreshMapCheck();
            RefreshSystemsView();
            RefreshHint();

            if (!string.IsNullOrEmpty(_vm.ToastMessage))
            {
                _toast.text = _vm.ToastMessage;
                _toast.style.display = DisplayStyle.Flex;
            }
            else
            {
                _toast.style.display = DisplayStyle.None;
            }

            // Keep unsaved room fields intact when toasts/undo state refresh the chrome.
            if (_vm.OpenPopover is not ("area" or GasMarkerPopover) || _activePopover == null)
                RefreshPopover();
        }

        /// <summary>Update only the tool hint bar (cheap; called on hover feedback).</summary>
        public void RefreshHint()
        {
            if (_hint == null)
                return;

            string text = _vm.SelectedObjectHint ?? string.Empty;
            if (text != _renderedHint)
            {
                _renderedHint = text;
                MapEditorChrome.FillHint(_hint, text);
            }

            _hint.style.display = text.Length == 0 ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private void RebuildSubcategories()
        {
            _subcatRow.Clear();
            _subcatButtons.Clear();

            // Sandbox-restricted and empty sub-tabs are not drawn (MapEditorCatalog.IsShown).
            foreach (MapEditorSubTab row in _vm.ShownSubTabs(_vm.CurrentMode))
            {
                Button tab = CreateSubcategoryTab(row.Id);
                _subcatButtons[row.Id] = tab;
                _subcatRow.Add(tab);
            }
        }

        private void RebuildGrid()
        {
            if (_grid == null || _paletteMode == PaletteMode.Min)
                return;

            _grid.Clear();

            if (_vm.OpenPopover == GasMarkerPopover && _vm.CurrentSubcategory != MapEditorSubcategory.StartingAir)
                ClosePopover();

            if (_vm.IsSearching)
            {
                BuildSearchResults();
                return;
            }

            if (_vm.CurrentSubcategory == MapEditorSubcategory.Rooms)
            {
                BuildAreaPalette(_grid);
                return;
            }

            List<MapEditorCatalogEntry> entryList = _catalog
                .Query(_vm.CurrentMode, _vm.CurrentSubcategory, null)
                .ToList();
            if (entryList.Count == 0)
            {
                AddGridMessage("Nothing here yet.");
                return;
            }

            IReadOnlyList<MapEditorHeadingGroup> groups =
                MapEditorCatalog.GroupByHeading(MapEditorSubTabs.Get(_vm.CurrentSubcategory), entryList);
            // Headings count entries, not slots; families fold within each heading.
            foreach (MapEditorHeadingGroup group in groups)
                AddHeadedGroup(group.Heading, "grid-heading-" + group.Heading, MapEditorCatalog.GroupFamilies(group.Entries));
        }

        /// <summary>Palette-wide search: every tab's matches, each group headed with where it lives.</summary>
        private void BuildSearchResults()
        {
            IReadOnlyList<MapEditorSearchGroup> groups = _catalog.Search(_vm.SearchText, _vm.Capabilities.RestrictedPalette);
            if (groups.Count == 0)
            {
                AddGridMessage($"No matches for \"{_vm.SearchText.Trim()}\" in any tab.");
                return;
            }

            foreach (MapEditorSearchGroup group in groups)
                AddHeadedGroup(group.Heading, "search-group-" + group.SubTab.Id, _catalog.SlotsFor(group.Entries));
        }

        /// <summary>
        /// A group's slots under its heading. The compact view shows one slot row, so a heading above it would
        /// overflow; there the heading runs up the left of the group's first row instead.
        /// </summary>
        private void AddHeadedGroup(string heading, string name, IReadOnlyList<MapEditorPaletteSlot> slots)
        {
            if (heading == null)
            {
                AddSlotRows(slots);
                return;
            }

            if (_paletteMode == PaletteMode.Normal)
            {
                AddSlotRows(slots, heading, name);
                return;
            }

            Label label = new(heading.ToUpperInvariant()) { name = name };
            label.AddToClassList("map-editor-grid-heading");
            _grid.Add(label);
            AddSlotRows(slots);
        }

        private static VisualElement SideHeading(string heading, string name)
        {
            VisualElement column = new() { pickingMode = PickingMode.Ignore };
            column.AddToClassList("map-editor-grid-side-heading");
            if (heading == null)
                return column;

            column.name = name;
            column.tooltip = heading;
            Label label = new(heading.ToUpperInvariant()) { pickingMode = PickingMode.Ignore };
            label.AddToClassList("map-editor-grid-side-heading__label");
            column.Add(label);
            return column;
        }

        private void AddGridMessage(string text)
        {
            Label message = new(text);
            message.AddToClassList("map-editor-stub-message");
            _grid.Add(message);
        }

        /// <param name="sideHeading">Compact view: the heading beside the first row; later rows get an empty column so slots line up.</param>
        private void AddSlotRows(IReadOnlyList<MapEditorPaletteSlot> slots, string sideHeading = null, string headingName = null)
        {
            int columnCount = GetMaxGridColumns(sideHeading != null ? GridSideHeadingOuterWidthPx : 0f);
            if (columnCount <= 0)
                columnCount = Math.Max(1, slots.Count);

            VisualElement row = null;
            for (int index = 0; index < slots.Count; index++)
            {
                if (index % columnCount == 0)
                {
                    row = new VisualElement { pickingMode = PickingMode.Position };
                    row.AddToClassList("map-editor-grid-row");
                    if (sideHeading != null)
                        row.Add(SideHeading(index == 0 ? sideHeading : null, headingName));
                    _grid.Add(row);
                }

                row.Add(BuildSlot(slots[index]));
            }
        }

        private void OnGridScrollGeometryChanged(GeometryChangedEvent evt)
        {
            if (_grid == null || _paletteMode == PaletteMode.Min)
                return;

            float width = _gridScroll.contentViewport.layout.width;
            if (width <= 1f)
                return;

            if (Math.Abs(width - _lastGridViewportWidth) < 1f)
                return;

            _lastGridViewportWidth = width;
            RebuildGrid();
            ApplyGridScrollHeight();
        }

        private int GetMaxGridColumns(float reservedWidth = 0f)
        {
            float width = _gridScroll?.contentViewport?.layout.width ?? 0f;
            if (width <= 1f)
                width = _gridScroll?.layout.width ?? 0f;
            if (width <= 1f)
                return 0;

            return Math.Max(1, Mathf.FloorToInt((width - reservedWidth) / GridSlotOuterWidthPx));
        }

        private Button BuildSlot(MapEditorPaletteSlot paletteSlot)
        {
            MapEditorCatalogEntry entry = _vm.ShownVariant(paletteSlot);
            ushort decalId = 0;
            bool isFloorDecal = MapEditorFloorDecalCatalog.TryDecode(entry.AssetName, out decalId);
            bool isSpawn = MapEditorSpawnCatalog.IsSpawnKey(entry.AssetName);
            bool isGasMarker = MapEditorGasMarkerCatalog.TryDecode(entry.AssetName, out GasMarkerKind gasKind);
            bool isFloorPaint = MapEditorFloorPaintCatalog.TryDecode(entry.AssetName, out int paintIndex);

            // Virtual catalog keys (floor decals, markers, paint swatches) are not TileResourceLoader assets.
            GenericObjectSo asset = isFloorDecal || isSpawn || isGasMarker || isFloorPaint
                ? null
                : _loader.GetAsset(entry.AssetName);

            FloorDecalDefinition floorDecal = null;
            if (isFloorDecal)
                FloorDecalCatalog.Get()?.TryGet(decalId, out floorDecal);

            Button slot = new(() => AssetSelected?.Invoke(entry, asset));
            slot.AddToClassList("map-editor-slot");
            if (floorDecal != null)
                slot.tooltip = floorDecal.DisplayName;
            else if (isSpawn)
                slot.tooltip = MapEditorSpawnCatalog.DisplayName(entry.AssetName);
            else if (isFloorPaint)
                slot.tooltip = FloorPaint.NameOf(paintIndex);
            else
                slot.tooltip = entry.AssetName;
            bool active = _vm.SelectedEntry != null &&
                          string.Equals(_vm.SelectedEntry.AssetName, entry.AssetName, StringComparison.OrdinalIgnoreCase);
            slot.EnableInClassList("map-editor-slot--active", active);

            VisualElement iconFrame = new();
            iconFrame.AddToClassList("map-editor-slot__icon-frame");

            VisualElement icon = new();
            icon.AddToClassList("map-editor-slot__icon");
            if (floorDecal?.Texture != null)
            {
                icon.style.backgroundImage = new StyleBackground(floorDecal.Texture);
            }
            else if (isSpawn && _icons?.GetSpawnIcon(entry.AssetName) is VectorImage spawnIcon)
            {
                icon.style.backgroundImage = new StyleBackground(spawnIcon);
                bool antagonist = MapEditorSpawnCatalog.TryDecode(entry.AssetName, out SpawnPointKind kind, out _, out _)
                                  && kind == SpawnPointKind.Antagonist;
                icon.AddToClassList(antagonist ? "map-editor-slot__icon--spawn-antag" : "map-editor-slot__icon--spawn-job");
            }
            else if (isGasMarker && _icons?.GetSubcategoryIcon(MapEditorSubcategory.StartingAir) is VectorImage gasIcon)
            {
                icon.style.backgroundImage = new StyleBackground(gasIcon);
                icon.style.unityBackgroundImageTintColor = MapEditorGasMarkerCatalog.PinColor(gasKind);
                icon.AddToClassList("map-editor-slot__icon--marker");
            }
            else if (isFloorPaint)
            {
                icon.style.backgroundColor = (Color)FloorPaint.ColorOf(paintIndex);
                icon.AddToClassList("map-editor-slot__icon--paint");
            }
            else if (asset?.icon != null)
            {
                icon.style.backgroundImage = new StyleBackground(asset.icon);
            }
            else if (_icons?.GetSubcategoryIcon(entry.Subcategory) is VectorImage glyph)
            {
                // No render (most tiles have no icon sprite): the sub-tab's glyph at the empty-slot alpha.
                icon.style.backgroundImage = new StyleBackground(glyph);
                icon.AddToClassList("map-editor-slot__icon--glyph");
            }

            iconFrame.Add(icon);

            string labelText = MapEditorCatalog.DisplayLabel(entry);
            slot.Add(iconFrame);
            if (paletteSlot.IsFamily)
            {
                AddFamilyChrome(slot, iconFrame, paletteSlot, entry);
                return slot;
            }

            Label label = new(labelText)
            {
                pickingMode = PickingMode.Ignore,
            };
            label.AddToClassList("map-editor-slot__label");
            slot.Add(label);
            return slot;
        }

        /// <summary>
        /// A family slot: the family's name takes the label's two lines, the variant names itself on a strip along the
        /// cell's bottom edge, an "n/N" badge sits in the corner, and ‹ › arrows (shown on hover or while held) pick the
        /// neighbouring variant.
        /// </summary>
        private void AddFamilyChrome(Button slot, VisualElement iconFrame, MapEditorPaletteSlot paletteSlot, MapEditorCatalogEntry shown)
        {
            slot.AddToClassList("map-editor-slot--family");
            slot.tooltip = $"{paletteSlot.Family}: {shown.VariantLabel} ({shown.AssetName}) — V / Shift+V for the next variant";

            Label family = new(paletteSlot.Family) { pickingMode = PickingMode.Ignore };
            family.AddToClassList("map-editor-slot__label");
            slot.Add(family);

            Label variant = new(shown.VariantLabel) { pickingMode = PickingMode.Ignore };
            variant.AddToClassList("map-editor-slot__variant");
            iconFrame.Add(variant);

            Label badge = new($"{paletteSlot.IndexOf(shown) + 1}/{paletteSlot.Variants.Count}") { pickingMode = PickingMode.Ignore };
            badge.AddToClassList("map-editor-slot__badge");
            iconFrame.Add(badge);

            iconFrame.Add(VariantArrow(paletteSlot, shown, -1, "\u2039", "map-editor-slot__cycle--previous"));
            iconFrame.Add(VariantArrow(paletteSlot, shown, 1, "\u203A", "map-editor-slot__cycle--next"));
        }

        private Button VariantArrow(MapEditorPaletteSlot paletteSlot, MapEditorCatalogEntry shown, int step, string glyph, string className)
        {
            Button arrow = new(() =>
            {
                MapEditorCatalogEntry next = paletteSlot.Step(shown, step);
                AssetSelected?.Invoke(next, _loader.GetAsset(next.AssetName));
            })
            {
                text = glyph,
                tooltip = step < 0 ? "Previous variant (Shift+V)" : "Next variant (V)",
            };
            arrow.AddToClassList("map-editor-slot__cycle");
            arrow.AddToClassList(className);
            // The slot is a Button too: keep this press from also picking the shown variant.
            arrow.RegisterCallback<PointerDownEvent>(evt => evt.StopPropagation());
            arrow.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());
            return arrow;
        }

        private void TogglePopover(string key)
        {
            _vm.OpenPopover = _vm.OpenPopover == key ? null : key;
            RefreshPopover();
        }

        /// <summary>Opens a popover (does not toggle closed if already that key).</summary>
        public void OpenPopover(string key)
        {
            if (string.IsNullOrEmpty(key))
                return;

            _vm.OpenPopover = key;
            RefreshPopover();
            _vm.NotifyChanged();
        }

        private void RefreshPopover()
        {
            _activePopover?.RemoveFromHierarchy();
            _activePopover = null;
            HideToolbarHints();

            string key = _vm.OpenPopover;
            ShowMapCheck(key == CheckPopover);
            _cameraDial?.SetOptionsOpen(key == "camera");
            // A popover's button stays outlined while it is open, as the active tool's does.
            foreach (KeyValuePair<string, VisualElement> pair in _popoverAnchors)
            {
                if (pair.Key != "camera")
                    pair.Value.Q<Button>()?.EnableInClassList("map-editor-icon-btn--active", pair.Key == key);
            }

            if (string.IsNullOrEmpty(key) || key == CheckPopover)
                return;

            VisualElement popover = new() { pickingMode = PickingMode.Position, name = "popover-" + key };
            popover.AddToClassList("map-editor-popover");
            if (key is "maps" or "saveMenu" or "area" or GasMarkerPopover)
                popover.AddToClassList("map-editor-popover--wide");

            VisualElement window = CreatePopoverWindow(GetPopoverTitle(key), ClosePopover);
            VisualElement contentRoot = window.Q("popover-body");
            BuildPopoverContent(key, contentRoot);
            popover.Add(window);

            _popoverOffsets.TryGetValue(key, out Vector2 dragOffset);
            popover.AddManipulator(new PanelDragManipulator(popover,
                pressed => pressed.ClassListContains("map-editor-popover-window__header"),
                dragOffset, offset =>
                {
                    _popoverOffsets[key] = offset;
                    MapEditorPopoverPlacement.ShowCaret(popover, offset == Vector2.zero && _popoverAnchors.ContainsKey(key));
                }));

            _popoverLayer.Add(popover);
            _activePopover = popover;
            PlacePopover(popover, key);

            if (_vm.OpenPopover == "maps" || _vm.OpenPopover == "saveMenu")
                RequestMapListRefresh();
        }

        /// <summary>
        /// Beside the button that opened it, with a caret pointing back at the button: the view tools' popovers open to
        /// their right, the tool bar's below it, the camera dial's to its left. Room and gas marker properties open from
        /// the world, so they sit beside the view tools without a caret, as does a popover the player dragged.
        /// </summary>
        private void PlacePopover(VisualElement popover, string key)
        {
            bool fromButton = _popoverAnchors.TryGetValue(key, out VisualElement anchor);
            MapEditorPopoverSide side = key switch
            {
                "maps" or "saveMenu" => MapEditorPopoverSide.Down,
                "camera" => MapEditorPopoverSide.Left,
                _ => MapEditorPopoverSide.Right,
            };
            bool dragged = _popoverOffsets.TryGetValue(key, out Vector2 offset) && offset != Vector2.zero;
            MapEditorPopoverPlacement.Place(_popoverLayer, popover, fromButton ? anchor : _viewTools, side, fromButton && !dragged);
        }

        private void HideToolbarHints()
        {
            if (_root == null)
                return;

            _root.Query(className: "map-editor-toolbar-hint").ForEach(hint =>
            {
                hint.style.display = DisplayStyle.None;
            });
        }

        private void ClosePopover()
        {
            _vm.OpenPopover = null;
            RefreshPopover();
        }

        public void RequestMapListRefresh() => MapListRefreshRequested?.Invoke();

        private static string GetPopoverTitle(string key) =>
            key switch
            {
                "area" => "Room properties",
                GasMarkerPopover => "Gas marker",
                "maps" => "Map Selection",
                "saveMenu" => "Save Map",
                "layers" => "Layer View Mode",
                "camera" => "Camera Options",
                "settings" => "Map Editor Settings",
                _ => "Panel",
            };

        private void BuildPopoverContent(string key, VisualElement container)
        {
            switch (key)
            {
                case "area":
                    BuildAreaPopover(container);
                    break;
                case GasMarkerPopover:
                    BuildGasMarkerPopover(container);
                    break;
                case "maps":
                    BuildMapsPopover(container);
                    break;
                case "saveMenu":
                    BuildSavePopover(container);
                    break;
                case "layers":
                    _vm.EnsureLayerDefaults();
                    foreach (TileLayerCategory category in TileLayerCategoryMapping.AllCategories)
                    {
                        AddSwitchRow(container, TileLayerCategoryMapping.GetDisplayName(category),
                            _vm.IsLayerCategoryVisible(category),
                            v => LayerCategoryVisibilityChanged?.Invoke(category, v));
                    }

                    break;
                case "camera":
                    AddSliderRow(container, "Field of View", _vm.CameraFov, 50f, 110f, "°", v =>
                    {
                        _vm.CameraFov = v;
                        RaiseCameraSettingsChanged();
                    });
                    AddSliderRow(container, "Zoom Speed", _vm.CameraZoomSpeed, 1f, 10f, string.Empty, v =>
                    {
                        _vm.CameraZoomSpeed = v;
                        RaiseCameraSettingsChanged();
                    });
                    AddSliderRow(container, "Rotation Speed", _vm.CameraRotationSpeed, 1f, 10f, string.Empty, v =>
                    {
                        _vm.CameraRotationSpeed = v;
                        RaiseCameraSettingsChanged();
                    });
                    break;
                case "settings":
                    // Client-side: each place command carries it, and the ghosts pick it up on the next hover.
                    AddSwitchRow(container, "Auto Subfloor", _vm.AutoSubfloor, v => _vm.AutoSubfloor = v);
                    AddSwitchRow(container, "Always Power Lights", _vm.AlwaysPowerLights, v =>
                    {
                        _vm.AlwaysPowerLights = v;
                        AlwaysPowerLightsChanged?.Invoke(v);
                    });
                    if (_vm.Capabilities.Invincibility)
                    {
                        AddSwitchRow(container, "Player Invincible", _vm.Invincible, v =>
                        {
                            _vm.Invincible = v;
                            InvincibleChanged?.Invoke(v);
                        });
                    }
                    AddSwitchRow(container, "Debug Overlay", _vm.DebugOverlay, v =>
                    {
                        _vm.DebugOverlay = v;
                        DebugOverlayChanged?.Invoke(v);
                    });
                    break;
            }
        }

        private void BuildMapsPopover(VisualElement container)
        {
            Button newMapBtn = new(() => NewMapRequested?.Invoke()) { text = "New Map" };
            newMapBtn.AddToClassList("map-editor-action-btn");
            newMapBtn.style.marginBottom = 4;
            container.Add(newMapBtn);

            Button openFolderBtn = new(() => OpenMapsFolderRequested?.Invoke())
            { text = "Open Maps Folder", tooltip = "Drop a shared .zip or .json here, then reopen this list" };
            openFolderBtn.AddToClassList("map-editor-action-btn");
            openFolderBtn.style.marginBottom = 8;
            container.Add(openFolderBtn);

            VisualElement listRoot = new() { pickingMode = PickingMode.Position, name = "map-load-list" };
            container.Add(listRoot);
        }

        private void BuildSavePopover(VisualElement container)
        {
            VisualElement nameLabel = new Label("Save Name") { pickingMode = PickingMode.Ignore };
            nameLabel.AddToClassList("map-editor-popover-label");
            container.Add(nameLabel);

            VisualElement nameField = new() { pickingMode = PickingMode.Position };
            nameField.AddToClassList("map-editor-search");
            _saveNameField = new TextField { value = string.IsNullOrEmpty(_vm.SaveMapName) ? "Untitled Map" : _vm.SaveMapName };
            _saveNameField.AddToClassList("map-editor-search-field");
            _saveNameField.RegisterValueChangedCallback(evt =>
            {
                _vm.SaveMapName = evt.newValue;
                // Keep the "Overwriting" badge on the row the typed name will actually replace.
                if (_lastMaps != null)
                    PopulateOverwriteList(_lastMaps);
            });
            WireTextEntryField(_saveNameField);
            nameField.Add(_saveNameField);
            container.Add(nameField);

            Label overwriteLabel = new("Overwrite Existing") { pickingMode = PickingMode.Ignore };
            overwriteLabel.AddToClassList("map-editor-popover-label");
            overwriteLabel.style.marginTop = 8;
            container.Add(overwriteLabel);

            VisualElement listRoot = new() { pickingMode = PickingMode.Position };
            listRoot.name = "save-overwrite-list";
            container.Add(listRoot);

            Button saveBtn = new(() =>
            {
                string name = _saveNameField.value?.Trim();
                if (!string.IsNullOrWhiteSpace(name))
                    SaveAsRequested?.Invoke(name);
            })
            { text = "Save Map" };
            saveBtn.AddToClassList("map-editor-action-btn");
            saveBtn.AddToClassList("map-editor-action-btn--primary");
            saveBtn.style.marginTop = 10;
            container.Add(saveBtn);

            Button exportBtn = new(() =>
            {
                string name = _saveNameField.value?.Trim();
                if (!string.IsNullOrWhiteSpace(name))
                    ExportRequested?.Invoke(name);
            })
            { text = "Save & Export", tooltip = "Saves, then opens a folder with a .zip of the map to share" };
            exportBtn.AddToClassList("map-editor-action-btn");
            exportBtn.style.marginTop = 6;
            exportBtn.style.marginLeft = 0;
            exportBtn.style.marginRight = 0;
            exportBtn.style.paddingTop = 8;
            exportBtn.style.paddingBottom = 8;
            container.Add(exportBtn);

            Label exportHint = new("Export makes a .zip you can drag into Discord. To add someone's map, use Open Maps Folder under Load and drop their .zip in.")
            { pickingMode = PickingMode.Ignore };
            exportHint.AddToClassList("map-editor-popover-hint");
            container.Add(exportHint);
        }

        private IReadOnlyList<MapEditorMapEntry> _lastMaps;

        public void PopulateMapList(IReadOnlyList<MapEditorMapEntry> maps)
        {
            _lastMaps = maps;
            if (_activePopover == null)
                return;

            if (_vm.OpenPopover == "maps")
                PopulateLoadList(maps);
            else if (_vm.OpenPopover == "saveMenu")
                PopulateOverwriteList(maps);
        }

        private void PopulateLoadList(IReadOnlyList<MapEditorMapEntry> maps)
        {
            VisualElement listRoot = _activePopover.Q(name: "map-load-list");
            if (listRoot == null)
                return;

            listRoot.Clear();

            foreach (MapEditorMapEntry map in maps)
            {
                string mapName = map.Name;

                VisualElement row = new() { pickingMode = PickingMode.Position };
                row.AddToClassList("map-editor-map-row");

                Label name = new(map.DisplayLabel) { pickingMode = PickingMode.Ignore };
                name.AddToClassList("map-editor-map-name");
                name.style.flexGrow = 1f;
                name.style.flexShrink = 1f;

                // Same constructor pattern as New Map — do not use --primary (width:100%).
                Button load = new(() => RequestLoadMap(mapName)) { text = "Load" };
                load.AddToClassList("map-editor-action-btn");

                Button delete = new(() => DeleteMapRequested?.Invoke(mapName)) { text = "Del" };
                delete.AddToClassList("map-editor-action-btn");

                row.Add(name);
                row.Add(load);
                row.Add(delete);
                listRoot.Add(row);
            }
        }

        private void RequestLoadMap(string mapName)
        {
            if (string.IsNullOrWhiteSpace(mapName))
                return;

            LoadMapRequested?.Invoke(mapName);
            ClosePopover();
        }

        private void PopulateOverwriteList(IReadOnlyList<MapEditorMapEntry> maps)
        {
            VisualElement listRoot = _activePopover.Q(name: "save-overwrite-list");
            if (listRoot == null)
                return;

            listRoot.Clear();
            string current = _saveNameField?.value?.Trim();

            foreach (MapEditorMapEntry map in maps)
            {
                bool isTarget = string.Equals(map.Name, current, StringComparison.OrdinalIgnoreCase);
                Button row = new(() =>
                {
                    if (_saveNameField != null)
                        _saveNameField.value = map.Name;
                    _vm.SaveMapName = map.Name;
                });
                row.AddToClassList("map-editor-map-row");
                Label name = new(map.DisplayLabel) { pickingMode = PickingMode.Ignore };
                name.AddToClassList("map-editor-map-name");
                name.EnableInClassList("map-editor-map-name--overwrite-target", isTarget);
                Label badge = new(isTarget ? "Overwriting" : "Overwrite") { pickingMode = PickingMode.Ignore };
                badge.AddToClassList("map-editor-map-badge");
                badge.EnableInClassList("map-editor-map-badge--warning", isTarget);
                row.Add(name);
                row.Add(badge);
                listRoot.Add(row);
            }
        }

        /// <summary>A tool's cell: its hotkey in the corner, its name and key in the hint under it.</summary>
        private void AddToolButton(VisualElement parent, MapEditorTool tool, string label, string key)
        {
            Button btn = CreateIconButton(_icons?.GetToolIcon(tool), null, () => ToolSelected?.Invoke(tool));
            MapEditorChrome.AddHotkeyMark(btn, key);
            btn.AddToClassList("map-editor-icon-btn--tool-" + ToolColourName(tool));
            _toolButtons[tool] = btn;
            parent.Add(MapEditorChrome.WithHint(btn, label, MapEditorPopoverSide.Down, key));
        }

        /// <summary>A mode tab: character setup's tab, small, a white plate when selected. No icon.</summary>
        private Button CreateModeTab(MapEditorMode mode)
        {
            Button tab = new(() => ModeSelected?.Invoke(mode)) { text = MapEditorSubTabs.ModeLabel(mode), userData = mode };
            tab.AddToClassList("map-editor-mode-tab");
            tab.AddToClassList("map-editor-mode-tab--" + ModeColourName(mode));
            return tab;
        }

        /// <summary>A sub-tab: its label alone; the open one gets a white line under it (none while searching every tab).</summary>
        private Button CreateSubcategoryTab(MapEditorSubcategory subcategory)
        {
            Button tab = new(() => SubcategorySelected?.Invoke(subcategory)) { text = MapEditorSubTabs.LabelOf(subcategory) };
            tab.AddToClassList("map-editor-subcat-tab");
            tab.EnableInClassList("map-editor-subcat-tab--active", subcategory == _vm.CurrentSubcategory && !_vm.IsSearching);
            return tab;
        }

        private static VisualElement CreateRegion(string className)
        {
            // Ignore — a Position region (esp. the full-width bottom dock) stole world picks for
            // the entire panel footprint, so vertical mouse motion / camera framing over that band
            // froze drag placement. Interactive children (buttons, windows, slots) still pick.
            VisualElement region = new() { pickingMode = PickingMode.Ignore };
            region.AddToClassList("map-editor-region");
            region.AddToClassList(className);
            return region;
        }

        /// <summary>A storage panel of cells: a column, or a row in the tool bar.</summary>
        private static VisualElement CreateToolbarStrip(bool horizontal = false)
        {
            // Ignore the strip chrome for world picks; only buttons/fields should block placement.
            VisualElement strip = new() { pickingMode = PickingMode.Ignore };
            strip.AddToClassList("map-editor-toolbar-strip");
            strip.EnableInClassList("map-editor-toolbar-strip--horizontal", horizontal);
            return strip;
        }

        private static Button CreateIconButton(VectorImage icon, string tooltip, Action onClick)
        {
            Button btn = new(onClick);
            btn.AddToClassList("map-editor-icon-btn");
            if (!string.IsNullOrEmpty(tooltip))
                btn.tooltip = tooltip;

            VisualElement iconElement = CreateIconElement(icon);
            iconElement.AddToClassList("map-editor-icon-btn__icon");
            btn.Add(iconElement);
            return btn;
        }

        private static Button CreateHeaderIconButton(VectorImage icon, string tooltip, Action onClick)
        {
            Button btn = new(onClick);
            btn.AddToClassList("map-editor-header-icon-btn");
            btn.tooltip = tooltip;

            VisualElement iconElement = CreateIconElement(icon);
            iconElement.AddToClassList("map-editor-header-icon-btn__icon");
            btn.Add(iconElement);
            return btn;
        }

        private static VisualElement CreateIconElement(VectorImage icon)
        {
            VisualElement element = new() { pickingMode = PickingMode.Ignore };
            element.AddToClassList("map-editor-vector-icon");
            if (icon != null)
                element.style.backgroundImage = new StyleBackground(icon);
            return element;
        }

        private VisualElement CreatePopoverWindow(string title, Action onClose)
        {
            VisualElement window = new() { pickingMode = PickingMode.Position };
            window.AddToClassList("map-editor-popover-window");

            // Clip layer only — never combine border-radius + overflow on the same element (UITK white block).
            VisualElement clip = new() { pickingMode = PickingMode.Ignore };
            clip.AddToClassList("map-editor-popover-window__clip");

            VisualElement header = new() { pickingMode = PickingMode.Position };
            header.AddToClassList("map-editor-popover-window__header");

            Label titleLabel = new(title) { pickingMode = PickingMode.Ignore };
            titleLabel.AddToClassList("map-editor-window__title");
            header.Add(titleLabel);

            header.Add(MapEditorChrome.CloseButton(onClose));

            VisualElement body = new() { pickingMode = PickingMode.Position, name = "popover-body" };
            body.AddToClassList("map-editor-popover-window__body");

            clip.Add(header);
            clip.Add(body);
            window.Add(clip);
            return window;
        }

        public sealed class SwitchRow : VisualElement
        {
            private readonly VisualElement _track;
            private bool _value;
            public event Action<bool> ValueChanged;
            public bool Value
            {
                get => _value;
                set { if (_value == value) return; SetValueWithoutNotify(value); ValueChanged?.Invoke(value); }
            }

            public SwitchRow(string label, bool value)
            {
                pickingMode = PickingMode.Position;
                AddToClassList("map-editor-switch-row");
                Label caption = new(label) { pickingMode = PickingMode.Ignore };
                caption.AddToClassList("map-editor-switch-row__label");
                _track = new VisualElement { pickingMode = PickingMode.Position, focusable = true };
                _track.AddToClassList("map-editor-switch");
                VisualElement thumb = new() { pickingMode = PickingMode.Ignore };
                thumb.AddToClassList("map-editor-switch__thumb");
                _track.Add(thumb);
                _track.RegisterCallback<ClickEvent>(_ => { if (enabledInHierarchy) Value = !Value; });
                _track.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (enabledInHierarchy && (evt.keyCode == KeyCode.Space || evt.keyCode == KeyCode.Return))
                    { Value = !Value; evt.StopPropagation(); }
                });
                Add(caption);
                Add(_track);
                SetValueWithoutNotify(value);
            }

            public void SetValueWithoutNotify(bool value)
            {
                _value = value;
                _track.EnableInClassList("map-editor-switch--on", value);
            }
        }

        private static SwitchRow AddSwitchRow(VisualElement parent, string label, bool value, Action<bool> onChanged)
        {
            SwitchRow row = new(label, value);
            row.ValueChanged += onChanged;
            parent.Add(row);
            return row;
        }

        private void RaiseCameraSettingsChanged() =>
            CameraSettingsChanged?.Invoke(_vm.CameraFov, _vm.CameraZoomSpeed, _vm.CameraRotationSpeed);

        private static void AddSliderRow(VisualElement parent, string label, float value, float min, float max,
            string unit, Action<float> onChanged)
        {
            VisualElement row = new();
            row.AddToClassList("map-editor-slider-row");

            VisualElement header = new();
            header.style.flexDirection = FlexDirection.Row;
            header.style.justifyContent = Justify.SpaceBetween;

            Label labelElement = new(label);
            labelElement.AddToClassList("map-editor-slider-row__label");
            Label valueElement = new(FormatSliderValue(value, unit));
            valueElement.AddToClassList("map-editor-slider-row__value");

            header.Add(labelElement);
            header.Add(valueElement);

            Slider slider = new(min, max) { value = value };
            slider.AddToClassList("map-editor-slider-row__slider");
            slider.RegisterValueChangedCallback(evt =>
            {
                valueElement.text = FormatSliderValue(evt.newValue, unit);
                onChanged(evt.newValue);
            });

            row.Add(header);
            row.Add(slider);
            parent.Add(row);
        }

        private static string FormatSliderValue(float value, string unit) => $"{Mathf.RoundToInt(value)}{unit}";
    }
}
