using SS3D.Data.AssetDatabases;
using SS3D.Systems.Inputs;
using SS3D.Systems.Tile.FloorVisuals;
using SS3D.Systems.Tile.MapEditor.Persistence;
using SS3D.Systems.Tile.GasMarkers;
using SS3D.Systems.Tile.SpawnPoints;
using SS3D.Systems.Tile.TileMapCreator;
using System;
using System.Collections.Generic;
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
        /// <summary>Object library panel heights (px) for the three resize states.</summary>
        private static readonly Dictionary<PaletteMode, float> PaletteHeights = new()
        {
            [PaletteMode.Min] = 40f,
            [PaletteMode.Normal] = 210f,
            [PaletteMode.Max] = 420f,
        };

        /// <summary>Object library grid row count per resize state (extra columns scroll horizontally).</summary>
        private const int NormalGridRows = 1;
        private const int MaxGridRows = 3;
        /// <summary>Slot width (108) + horizontal margins (8) + border (3).</summary>
        private const float GridSlotOuterWidthPx = 120f;
        /// <summary>Slot height (104) + vertical margins (8) + border (3).</summary>
        private const float GridSlotOuterHeightPx = 115f;

        private readonly StyleSheet _styleSheet;
        private readonly MapEditorIconsSo _icons;
        private readonly MapEditorViewModel _vm;
        private readonly MapEditorCatalog _catalog;
        private readonly TileResourceLoader _loader;

        private VisualElement _root;
        private VisualElement _hudLayer;
        private VisualElement _leftToolbar;
        // Hidden when the server turns map persistence off (sandbox).
        private VisualElement _saveMapButton;
        private VisualElement _loadMapButton;
        private VisualElement _persistenceSeparator;
        private VisualElement _rightToolbar;
        private VisualElement _modeRail;
        private VisualElement _subcatRow;
        private VisualElement _paletteBody;
        private VisualElement _paletteRegion;
        private ScrollView _gridScroll;
        private VisualElement _grid;
        private TextField _searchField;
        private TextField _saveNameField;
        private readonly InputTextEntryScope _textEntry = new();
        private Label _windowTitle;
        private Label _toast;
        private Label _hint;
        private VisualElement _leftPopoverAnchor;
        private VisualElement _rightPopoverAnchor;
        private VisualElement _cameraPopoverAnchor;
        private VisualElement _activePopover;
        private VisualElement _popoverLayer;
        /// <summary>Where the player dragged each popover; reopening it keeps that spot for the session.</summary>
        private readonly Dictionary<string, Vector2> _popoverOffsets = new();

        private float _lastGridViewportWidth;
        private PaletteMode _paletteMode = PaletteMode.Normal;

        private readonly Dictionary<MapEditorTool, Button> _toolButtons = new();
        private readonly Dictionary<MapEditorMode, Button> _modeButtons = new();
        private readonly Dictionary<MapEditorSubcategory, Button> _subcatButtons = new();

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

            BuildLeftToolbar();
            BuildRightSide();
            BuildBottomDock();
            BuildRevealButton();

            _toast = new Label { name = "toast", pickingMode = PickingMode.Ignore };
            _toast.AddToClassList("map-editor-toast");
            _toast.style.display = DisplayStyle.None;
            _root.Add(_toast);

            _hint = new Label { name = "placement-hint", pickingMode = PickingMode.Ignore };
            _hint.AddToClassList("map-editor-hint");
            _hudLayer.Add(_hint);

            // Popovers live in their own last-child layer: UITK paints in hierarchy order and ignores z-index, so a
            // popover parented under its toolbar was painted beneath later siblings such as Map Check.
            _popoverLayer = new VisualElement { name = "popover-layer", pickingMode = PickingMode.Ignore };
            _popoverLayer.style.position = Position.Absolute;
            _popoverLayer.style.left = 0;
            _popoverLayer.style.top = 0;
            _popoverLayer.style.right = 0;
            _popoverLayer.style.bottom = 0;
            _hudLayer.Add(_popoverLayer);

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

        private void BuildLeftToolbar()
        {
            VisualElement region = CreateRegion("map-editor-region--top-left");
            _leftPopoverAnchor = region;

            _leftToolbar = CreateToolbarStrip(vertical: true);
            AddToolButton(_leftToolbar, MapEditorTool.Edit, "Construct [1]");
            AddToolButton(_leftToolbar, MapEditorTool.Select, "Select [2]");
            AddToolButton(_leftToolbar, MapEditorTool.Dropper, "Dropper [3]");
            AddToolButton(_leftToolbar, MapEditorTool.Delete, "Delete [4]");
            AddSeparator(_leftToolbar, vertical: true);

            Button undo = CreateIconButton(_icons?.Undo, null, () => UndoRequested?.Invoke());
            undo.name = "undo-btn";
            _leftToolbar.Add(WrapWithHoverHint(undo, "Undo"));

            Button redo = CreateIconButton(_icons?.Redo, null, () => RedoRequested?.Invoke());
            redo.name = "redo-btn";
            _leftToolbar.Add(WrapWithHoverHint(redo, "Redo"));
            AddSeparator(_leftToolbar, vertical: true);

            _persistenceSeparator = _leftToolbar[_leftToolbar.childCount - 1];
            _saveMapButton = WrapWithHoverHint(
                CreateIconButton(_icons?.SaveMap, null, () => TogglePopover("saveMenu")),
                "Save map [Ctrl+Shift+S]");
            _leftToolbar.Add(_saveMapButton);
            _loadMapButton = WrapWithHoverHint(
                CreateIconButton(_icons?.OpenMap, null, () => TogglePopover("maps")),
                "Load map [Ctrl+Shift+O]");
            _leftToolbar.Add(_loadMapButton);

            region.Add(_leftToolbar);
            _hudLayer.Add(region);
        }

        private void BuildRightSide()
        {
            VisualElement region = CreateRegion("map-editor-region--top-right");
            region.AddToClassList("map-editor-right-column");

            VisualElement cameraWrap = new() { pickingMode = PickingMode.Position };
            cameraWrap.style.position = Position.Relative;
            _cameraPopoverAnchor = cameraWrap;
            cameraWrap.Add(BuildCameraDial());
            region.Add(cameraWrap);

            VisualElement toolbarWrap = new() { pickingMode = PickingMode.Position };
            toolbarWrap.style.position = Position.Relative;
            _rightPopoverAnchor = toolbarWrap;

            _rightToolbar = CreateToolbarStrip(vertical: true);
            _rightToolbar.Add(WrapWithHoverHint(
                CreateIconButton(_icons?.ResetView, null, () => ResetViewRequested?.Invoke()),
                "Reset view",
                opensLeft: true));
            _rightToolbar.Add(WrapWithHoverHint(
                CreateIconButton(_icons?.Layers, null, () => TogglePopover("layers")),
                "Layers",
                opensLeft: true));
            _rightToolbar.Add(WrapWithHoverHint(
                CreateIconButton(_icons?.Settings, null, () => TogglePopover("settings")),
                "Settings",
                opensLeft: true));
            toolbarWrap.Add(_rightToolbar);
            region.Add(toolbarWrap);

            BuildMapCheck(region);
            _hudLayer.Add(region);
        }

        private VisualElement BuildCameraDial()
        {
            VisualElement dial = new() { pickingMode = PickingMode.Position };
            dial.AddToClassList("map-editor-camera-dial");

            VisualElement ring = new() { pickingMode = PickingMode.Ignore };
            ring.AddToClassList("map-editor-camera-dial__ring");
            ring.name = "camera-dial-ring";
            dial.Add(ring);

            // Dedicated dial button — avoid map-editor-icon-btn's fixed 38px size,
            // which fights absolute centering inside the 56px dial.
            Button open = new(() => TogglePopover("camera")) { tooltip = "Camera options", name = "camera-dial-open" };
            open.AddToClassList("map-editor-camera-dial__open");
            VisualElement cameraIcon = CreateIconElement(_icons?.Camera);
            open.Add(cameraIcon);
            dial.Add(open);

            dial.Add(CreateDialButton(_icons?.RotateLeft, "Rotate left",
                () => CameraRotateRequested?.Invoke(-1), "map-editor-camera-dial__rotate-left"));
            dial.Add(CreateDialButton(_icons?.RotateRight, "Rotate right",
                () => CameraRotateRequested?.Invoke(1), "map-editor-camera-dial__rotate-right"));

            VisualElement zoomRow = new() { pickingMode = PickingMode.Position };
            zoomRow.AddToClassList("map-editor-camera-dial__zoom-row");
            zoomRow.Add(CreateDialButton(_icons?.ZoomOut, "Zoom out", () => CameraZoomRequested?.Invoke(-1)));
            zoomRow.Add(CreateDialButton(_icons?.ZoomIn, "Zoom in", () => CameraZoomRequested?.Invoke(1)));

            VisualElement wrap = new() { pickingMode = PickingMode.Position };
            wrap.AddToClassList("map-editor-camera-dial-wrap");
            wrap.Add(dial);
            wrap.Add(zoomRow);
            return wrap;
        }

        private static Button CreateDialButton(VectorImage icon, string tooltip, Action onClick, string extraClass = null)
        {
            Button btn = new(onClick) { tooltip = tooltip };
            btn.AddToClassList("map-editor-camera-dial__btn");
            if (extraClass != null)
                btn.AddToClassList(extraClass);

            VisualElement iconElement = CreateIconElement(icon);
            iconElement.AddToClassList("map-editor-camera-dial__btn-icon");
            btn.Add(iconElement);
            return btn;
        }

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

            VisualElement library = new() { pickingMode = PickingMode.Position };
            library.AddToClassList("map-editor-library");
            VisualElement libWindow = CreateWindow(string.Empty);
            libWindow.style.flexGrow = 1;
            libWindow.style.paddingTop = 0;
            libWindow.style.paddingBottom = 0;
            libWindow.style.paddingLeft = 0;
            libWindow.style.paddingRight = 0;

            VisualElement header = new() { pickingMode = PickingMode.Position };
            header.AddToClassList("map-editor-library-header");
            _windowTitle = new Label { pickingMode = PickingMode.Ignore };
            _windowTitle.AddToClassList("map-editor-library-header__title");
            header.Add(_windowTitle);

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

            _paletteBody = new VisualElement { pickingMode = PickingMode.Position };
            _paletteBody.AddToClassList("map-editor-library-body");

            VisualElement toolsRow = new() { pickingMode = PickingMode.Position };
            toolsRow.style.flexDirection = FlexDirection.Row;
            toolsRow.style.justifyContent = Justify.SpaceBetween;

            _subcatRow = new VisualElement();
            _subcatRow.style.flexDirection = FlexDirection.Row;
            _subcatRow.style.flexWrap = Wrap.Wrap;

            VisualElement search = new();
            search.AddToClassList("map-editor-search");
            _searchField = new TextField { value = string.Empty };
            _searchField.AddToClassList("map-editor-search-field");
            _searchField.textEdition.placeholder = "Search tags, names, keyword";
            _searchField.textEdition.hidePlaceholderOnFocus = true;
            _searchField.RegisterValueChangedCallback(evt => SearchChanged?.Invoke(evt.newValue));
            WireTextEntryField(_searchField);
            VisualElement searchIcon = CreateIconElement(_icons?.Search);
            searchIcon.AddToClassList("map-editor-search-icon");
            search.Add(searchIcon);
            search.Add(_searchField);

            toolsRow.Add(_subcatRow);
            toolsRow.Add(search);

            _gridScroll = new ScrollView(ScrollViewMode.Vertical);
            _gridScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _gridScroll.verticalScrollerVisibility = ScrollerVisibility.Auto;
            _gridScroll.AddToClassList("map-editor-grid-scroll");
            _grid = new VisualElement();
            _grid.AddToClassList("map-editor-grid");
            _gridScroll.Add(_grid);
            _gridScroll.RegisterCallback<GeometryChangedEvent>(OnGridScrollGeometryChanged);

            _paletteBody.Add(toolsRow);
            _paletteBody.Add(_gridScroll);

            libWindow.Add(header);
            libWindow.Add(_paletteBody);
            library.Add(libWindow);

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

            field.RegisterCallback<FocusInEvent>(_ => _textEntry.Enter());
            field.RegisterCallback<FocusOutEvent>(_ => _textEntry.Exit());
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
            if (_mapCheckColumn != null)
                _mapCheckColumn.style.bottom = 24 + PaletteHeights[_paletteMode] + 12;
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

            foreach (KeyValuePair<MapEditorMode, Button> pair in _modeButtons)
            {
                pair.Value.EnableInClassList("map-editor-mode-tab--active", pair.Key == _vm.CurrentMode);
            }

            RebuildSubcategories();
            ApplyPaletteMode();

            DisplayStyle persistence = _vm.Capabilities.Persistence ? DisplayStyle.Flex : DisplayStyle.None;
            if (_saveMapButton != null)
                _saveMapButton.style.display = persistence;
            if (_loadMapButton != null)
                _loadMapButton.style.display = persistence;
            if (_persistenceSeparator != null)
                _persistenceSeparator.style.display = persistence;

            Button undoBtn = _leftToolbar?.Q<Button>("undo-btn");
            Button redoBtn = _leftToolbar?.Q<Button>("redo-btn");
            undoBtn?.EnableInClassList("map-editor-icon-btn--disabled", _vm.UndoDepth <= 0);
            redoBtn?.EnableInClassList("map-editor-icon-btn--disabled", _vm.RedoDepth <= 0);

            RefreshMapCheck();
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

            string text = _vm.SelectedObjectHint;
            _hint.text = text ?? string.Empty;
            _hint.style.display = string.IsNullOrEmpty(text) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private void RebuildSubcategories()
        {
            _subcatRow.Clear();
            _subcatButtons.Clear();

            MapEditorSubcategory[] subs = MapEditorCatalog.GetSubcategories(_vm.CurrentMode);
            foreach (MapEditorSubcategory sub in subs)
            {
                if (sub == MapEditorSubcategory.SpawnPlacements && _vm.Capabilities.RestrictedPalette)
                    continue;

                Button tab = CreateSubcategoryTab(sub);
                _subcatButtons[sub] = tab;
                _subcatRow.Add(tab);
            }

            if (_windowTitle != null)
            {
                string mode = MapEditorCatalog.GetModeLabel(_vm.CurrentMode).ToUpperInvariant();
                string sub = MapEditorCatalog.GetSubcategoryLabel(_vm.CurrentSubcategory).ToUpperInvariant();
                _windowTitle.text = $"Object Library — {mode} / {sub}";
            }
        }

        private void RebuildGrid()
        {
            if (_grid == null || _paletteMode == PaletteMode.Min)
                return;

            _grid.Clear();

            if (_vm.OpenPopover == GasMarkerPopover && _vm.CurrentSubcategory != MapEditorSubcategory.Atmospherics)
                ClosePopover();

            if (_vm.CurrentMode == MapEditorMode.Scripting && _vm.CurrentSubcategory == MapEditorSubcategory.Rooms)
            {
                BuildAreaPalette(_grid);
                return;
            }

            if (_vm.CurrentMode == MapEditorMode.Scripting &&
                MapEditorCatalog.IsUnavailableScriptingSubcategory(_vm.CurrentSubcategory))
            {
                Label stub = new(
                    "Not available yet — random spawners and triggers are a later pass.")
                {
                    style = { whiteSpace = WhiteSpace.Normal },
                };
                stub.AddToClassList("map-editor-stub-message");
                _grid.Add(stub);
                return;
            }

            List<MapEditorCatalogEntry> entryList = new();
            foreach (MapEditorCatalogEntry entry in _catalog.Query(
                         _vm.CurrentMode, _vm.CurrentSubcategory, _vm.SearchText))
            {
                entryList.Add(entry);
            }

            if (entryList.Count == 0)
            {
                Label empty = new($"No matches for \"{_vm.SearchText}\".");
                empty.AddToClassList("map-editor-stub-message");
                _grid.Add(empty);
                return;
            }

            int maxColumns = GetMaxGridColumns();
            if (maxColumns <= 0)
                maxColumns = Math.Max(1, entryList.Count);

            int columnCount = maxColumns;
            int usedRows = (entryList.Count + columnCount - 1) / columnCount;
            VisualElement[] rows = new VisualElement[usedRows];
            for (int rowIndex = 0; rowIndex < usedRows; rowIndex++)
            {
                VisualElement row = new() { pickingMode = PickingMode.Position };
                row.AddToClassList("map-editor-grid-row");
                rows[rowIndex] = row;
                _grid.Add(row);
            }

            for (int index = 0; index < entryList.Count; index++)
            {
                rows[index / columnCount].Add(BuildSlot(entryList[index]));
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

        private int GetMaxGridColumns()
        {
            float width = _gridScroll?.contentViewport?.layout.width ?? 0f;
            if (width <= 1f)
                width = _gridScroll?.layout.width ?? 0f;
            if (width <= 1f)
                return 0;

            return Math.Max(1, Mathf.FloorToInt(width / GridSlotOuterWidthPx));
        }

        private Button BuildSlot(MapEditorCatalogEntry entry)
        {
            ushort decalId = 0;
            bool isFloorDecal = !entry.IsEraser &&
                               MapEditorFloorDecalCatalog.TryDecode(entry.AssetName, out decalId);
            bool isSpawn = !entry.IsEraser && MapEditorSpawnCatalog.IsSpawnKey(entry.AssetName);
            bool isGasMarker = MapEditorGasMarkerCatalog.TryDecode(entry.AssetName, out GasMarkerKind gasKind) && !entry.IsEraser;

            // Virtual catalog keys (floor decals / spawn markers) are not TileResourceLoader assets.
            GenericObjectSo asset = entry.IsEraser || isFloorDecal || isSpawn || isGasMarker
                ? null
                : _loader.GetAsset(entry.AssetName);

            FloorDecalDefinition floorDecal = null;
            if (isFloorDecal)
                FloorDecalCatalog.Get()?.TryGet(decalId, out floorDecal);

            Button slot = new(() => AssetSelected?.Invoke(entry, asset));
            slot.AddToClassList("map-editor-slot");
            if (!entry.IsEraser)
            {
                if (floorDecal != null)
                    slot.tooltip = floorDecal.DisplayName;
                else if (isSpawn)
                    slot.tooltip = MapEditorSpawnCatalog.DisplayName(entry.AssetName);
                else
                    slot.tooltip = entry.AssetName;
            }
            bool active = _vm.SelectedEntry != null &&
                          string.Equals(_vm.SelectedEntry.AssetName, entry.AssetName, StringComparison.OrdinalIgnoreCase);
            slot.EnableInClassList("map-editor-slot--active", active);

            VisualElement iconFrame = new();
            iconFrame.AddToClassList("map-editor-slot__icon-frame");

            VisualElement icon = new();
            icon.AddToClassList("map-editor-slot__icon");
            if (entry.IsEraser)
            {
                icon.AddToClassList("map-editor-slot__icon--eraser");
            }
            else if (floorDecal?.Texture != null)
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
            else if (isGasMarker && _icons?.GetSubcategoryIcon(MapEditorSubcategory.Atmospherics) is VectorImage gasIcon)
            {
                icon.style.backgroundImage = new StyleBackground(gasIcon);
                icon.style.unityBackgroundImageTintColor = MapEditorGasMarkerCatalog.PinColor(gasKind);
            }
            else if (asset?.icon != null)
            {
                icon.style.backgroundImage = new StyleBackground(asset.icon);
            }

            iconFrame.Add(icon);

            string labelText = entry.IsEraser
                ? "Eraser"
                : floorDecal != null
                    ? floorDecal.DisplayName
                    : isSpawn
                        ? MapEditorSpawnCatalog.DisplayName(entry.AssetName)
                        : isGasMarker
                            ? MapEditorGasMarkerCatalog.DisplayName(gasKind)
                            : entry.AssetName;
            Label label = new(labelText)
            {
                pickingMode = PickingMode.Ignore,
            };
            label.AddToClassList("map-editor-slot__label");

            slot.Add(iconFrame);
            slot.Add(label);
            return slot;
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

        private static bool IsLeftPopover(string key) => key is "maps" or "saveMenu" or "area" or GasMarkerPopover;

        private void RefreshPopover()
        {
            _activePopover?.RemoveFromHierarchy();
            _activePopover = null;
            HideToolbarHints();

            if (string.IsNullOrEmpty(_vm.OpenPopover))
                return;

            VisualElement anchor = _vm.OpenPopover switch
            {
                "maps" or "saveMenu" or "area" or GasMarkerPopover => _leftPopoverAnchor,
                "camera" => _cameraPopoverAnchor,
                _ => _rightPopoverAnchor,
            };

            string key = _vm.OpenPopover;
            VisualElement popover = new() { pickingMode = PickingMode.Position, name = "popover-" + key };
            popover.AddToClassList("map-editor-popover");
            if (IsLeftPopover(key))
                popover.AddToClassList("map-editor-popover--wide");

            VisualElement window = CreatePopoverWindow(GetPopoverTitle(key), ClosePopover);
            VisualElement contentRoot = window.Q("popover-body");
            BuildPopoverContent(key, contentRoot);
            popover.Add(window);

            _popoverOffsets.TryGetValue(key, out Vector2 dragOffset);
            popover.AddManipulator(new PanelDragManipulator(popover,
                pressed => pressed.ClassListContains("map-editor-popover-window__header"),
                dragOffset, offset => _popoverOffsets[key] = offset));

            _popoverLayer.Add(popover);
            _activePopover = popover;
            PlacePopover(popover, anchor, opensRight: IsLeftPopover(key));
            if (float.IsNaN(anchor.layout.width))
                anchor.RegisterCallback<GeometryChangedEvent>(PlaceOnceLaidOut);

            void PlaceOnceLaidOut(GeometryChangedEvent evt)
            {
                anchor.UnregisterCallback<GeometryChangedEvent>(PlaceOnceLaidOut);
                if (_activePopover == popover)
                    PlacePopover(popover, anchor, opensRight: IsLeftPopover(key));
            }

            if (_vm.OpenPopover == "maps" || _vm.OpenPopover == "saveMenu")
                RequestMapListRefresh();
        }

        /// <summary>
        /// Puts the popover beside its anchor, top-aligned: left-column popovers open to the anchor's right, the rest
        /// to its left. Dragging adds a translate on top of this.
        /// </summary>
        private void PlacePopover(VisualElement popover, VisualElement anchor, bool opensRight)
        {
            const float gap = 8f;
            Rect anchorRect = _popoverLayer.WorldToLocal(anchor.worldBound);
            if (float.IsNaN(anchorRect.x))
                return;

            popover.style.top = anchorRect.yMin;
            if (opensRight)
            {
                popover.style.left = anchorRect.xMax + gap;
                popover.style.right = StyleKeyword.Auto;
            }
            else
            {
                popover.style.right = _popoverLayer.layout.width - anchorRect.xMin + gap;
                popover.style.left = StyleKeyword.Auto;
            }
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

        private void AddToolButton(VisualElement parent, MapEditorTool tool, string hintLabel)
        {
            Button btn = CreateIconButton(_icons?.GetToolIcon(tool), null, () => ToolSelected?.Invoke(tool));
            _toolButtons[tool] = btn;
            parent.Add(WrapWithHoverHint(btn, hintLabel));
        }

        private static VisualElement WrapWithHoverHint(Button btn, string hintLabel, bool opensLeft = false)
        {
            VisualElement wrap = new() { pickingMode = PickingMode.Ignore };
            wrap.AddToClassList("map-editor-toolbar-btn-wrap");

            Label hint = new(hintLabel) { pickingMode = PickingMode.Ignore };
            hint.AddToClassList("map-editor-toolbar-hint");
            hint.AddToClassList(opensLeft
                ? "map-editor-toolbar-hint--opens-left"
                : "map-editor-toolbar-hint--opens-right");
            hint.style.display = DisplayStyle.None;

            btn.RegisterCallback<MouseEnterEvent>(_ => hint.style.display = DisplayStyle.Flex);
            btn.RegisterCallback<MouseLeaveEvent>(_ => hint.style.display = DisplayStyle.None);

            wrap.Add(btn);
            wrap.Add(hint);
            return wrap;
        }

        private Button CreateModeTab(MapEditorMode mode)
        {
            Button tab = new(() => ModeSelected?.Invoke(mode));
            tab.AddToClassList("map-editor-mode-tab");
            tab.userData = mode;

            VisualElement surface = new();
            surface.AddToClassList("map-editor-mode-tab__surface");
            surface.pickingMode = PickingMode.Ignore;

            VisualElement icon = CreateIconElement(_icons?.GetModeIcon(mode));
            icon.AddToClassList("map-editor-mode-tab__icon");
            Label label = new(MapEditorCatalog.GetModeLabel(mode).ToUpperInvariant());
            label.AddToClassList("map-editor-mode-tab__label");
            label.pickingMode = PickingMode.Ignore;
            surface.Add(icon);
            surface.Add(label);
            tab.Add(surface);
            return tab;
        }

        private Button CreateSubcategoryTab(MapEditorSubcategory subcategory)
        {
            Button tab = new(() => SubcategorySelected?.Invoke(subcategory));
            tab.AddToClassList("map-editor-subcat-tab");
            tab.EnableInClassList("map-editor-subcat-tab--active", subcategory == _vm.CurrentSubcategory);

            VisualElement surface = new();
            surface.AddToClassList("map-editor-subcat-tab__surface");
            surface.pickingMode = PickingMode.Ignore;

            VisualElement icon = CreateIconElement(_icons?.GetSubcategoryIcon(subcategory));
            icon.AddToClassList("map-editor-subcat-tab__icon");
            Label label = new(MapEditorCatalog.GetSubcategoryLabel(subcategory).ToUpperInvariant());
            label.AddToClassList("map-editor-subcat-tab__label");
            label.pickingMode = PickingMode.Ignore;
            surface.Add(icon);
            surface.Add(label);
            tab.Add(surface);
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

        private static VisualElement CreateToolbarStrip(bool vertical = false)
        {
            // Ignore the strip chrome for world picks; only buttons/fields should block placement.
            VisualElement strip = new() { pickingMode = PickingMode.Ignore };
            strip.AddToClassList("map-editor-toolbar-strip");
            strip.EnableInClassList("map-editor-toolbar-strip--vertical", vertical);
            strip.style.flexDirection = vertical ? FlexDirection.Column : FlexDirection.Row;
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

        private static void AddSeparator(VisualElement parent, bool vertical = false)
        {
            VisualElement sep = new() { pickingMode = PickingMode.Ignore };
            sep.AddToClassList("map-editor-separator");
            sep.EnableInClassList("map-editor-separator--vertical", vertical);
            parent.Add(sep);
        }

        private static VisualElement CreateWindow(string title)
        {
            VisualElement window = new() { pickingMode = PickingMode.Position };
            window.AddToClassList("map-editor-window");
            if (!string.IsNullOrEmpty(title))
            {
                Label titleLabel = new(title) { pickingMode = PickingMode.Ignore };
                titleLabel.AddToClassList("map-editor-window__title");
                window.Add(titleLabel);
            }

            return window;
        }

        private VisualElement CreatePopoverWindow(string title, Action onClose)
        {
            VisualElement window = new() { pickingMode = PickingMode.Position };
            window.AddToClassList("map-editor-window");
            window.AddToClassList("map-editor-popover-window");

            // Clip layer only — never combine border-radius + overflow on the same element (UITK white block).
            VisualElement clip = new() { pickingMode = PickingMode.Ignore };
            clip.AddToClassList("map-editor-popover-window__clip");

            VisualElement header = new() { pickingMode = PickingMode.Position };
            header.AddToClassList("map-editor-popover-window__header");

            Label titleLabel = new(title) { pickingMode = PickingMode.Ignore };
            titleLabel.AddToClassList("map-editor-window__title");
            header.Add(titleLabel);

            Button closeBtn = new(onClose);
            closeBtn.AddToClassList("map-editor-popover-close");
            Label closeGlyph = new("\u00d7") { pickingMode = PickingMode.Ignore };
            closeGlyph.AddToClassList("map-editor-popover-close__glyph");
            closeBtn.Add(closeGlyph);
            header.Add(closeBtn);

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
