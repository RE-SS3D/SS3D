using SS3D.Systems.Tile.GasMarkers;
using SS3D.Data.AssetDatabases;
using SS3D.Systems.Tile.TileMapCreator;
using SS3D.Systems.Tile.Validation;
using System;
using System.Collections.Generic;

namespace SS3D.Systems.Tile.MapEditor.UI
{
    /// <summary>
    /// View-model state for the map editor UI.
    /// </summary>
    public sealed class MapEditorViewModel
    {
        public string AreaRoomName { get; set; } = "New room";
        public ushort AreaRoomId { get; set; }
        public bool IsRoomCategory => CurrentMode == MapEditorMode.Scripting && CurrentSubcategory == MapEditorSubcategory.Rooms;
        public MapEditorTool CurrentTool { get; set; } = MapEditorTool.Edit;
        public MapEditorMode CurrentMode { get; set; } = MapEditorMode.Upper;
        public MapEditorSubcategory CurrentSubcategory { get; set; } = MapEditorSubcategory.Flooring;
        public string SearchText { get; set; } = string.Empty;
        public GenericObjectSo SelectedAsset { get; set; }
        public MapEditorCatalogEntry SelectedEntry { get; set; }
        public bool IsEraserSelected { get; set; }
        public bool HideUI { get; set; }
        public bool DebugOverlay { get; set; }
        /// <summary>Light fixtures glow without grid power (server-wide; persists after closing the editor).</summary>
        public bool AlwaysPowerLights { get; set; }
        /// <summary>Builder takes no damage and walks on open space (persists after closing the editor).</summary>
        public bool Invincible { get; set; } = true;
        public float CameraFov { get; set; } = 72f;
        public float CameraZoomSpeed { get; set; } = 5f;
        public float CameraRotationSpeed { get; set; } = 4f;
        public int UndoDepth { get; set; }
        public int RedoDepth { get; set; }
        public string SelectedObjectHint { get; set; } = "Select the Edit tool to place this object";
        public MapValidationIssue[] ValidationIssues { get; private set; } = Array.Empty<MapValidationIssue>();
        public bool ValidationChecked { get; private set; }
        public string ValidationMode { get; private set; }
        public bool ValidationStale { get; set; }
        public int ValidationRevision { get; private set; }
        public int ValidationContext { get; private set; }

        public void SetValidation(MapValidationIssue[] issues, string mode, bool resetDismissed = false)
        {
            ValidationIssues = issues ?? Array.Empty<MapValidationIssue>();
            ValidationMode = mode;
            ValidationChecked = true;
            ValidationStale = false;
            ValidationRevision++;
            if (resetDismissed) ValidationContext++;
            NotifyChanged();
        }
        /// <summary>What the server lets this player use; full until the server answers on open.</summary>
        public MapEditorCapabilities Capabilities { get; private set; } = MapEditorCapabilities.Full;

        public void SetCapabilities(MapEditorCapabilities capabilities)
        {
            Capabilities = capabilities;
            if (!capabilities.Persistence && OpenPopover is "saveMenu" or "maps")
                OpenPopover = null;
            NotifyChanged();
        }

        public string ToastMessage { get; set; }
        public string OpenPopover { get; set; }

        /// <summary>What the next placed gas marker writes, or what the marker form is editing.</summary>
        public GasMarkerRecord GasMarkerDraft { get; set; } = GasMarkerRecord.Default(GasMarkerKind.Source);
        public string SaveMapName { get; set; } = "Untitled Map";
        public Direction CurrentRotation { get; set; } = Direction.North;
        public MapEditorSaveTarget SaveTarget { get; set; } = MapEditorSaveTarget.LocalTemplate;
        public MapEditorPlacementMode PlacementMode { get; set; } = MapEditorPlacementMode.Normal;

        private readonly Dictionary<TileLayerCategory, bool> _layerCategoryVisibility = new();

        // True while Piping/Disposals hid Turfs; leaving those tabs restores it. A manual Turfs toggle clears it.
        private bool _turfsAutoHidden;

        public event Action StateChanged;

        public void EnsureLayerDefaults()
        {
            foreach (TileLayerCategory category in TileLayerCategoryMapping.AllCategories)
            {
                if (!_layerCategoryVisibility.ContainsKey(category))
                    _layerCategoryVisibility[category] = category is not (TileLayerCategory.Areas or TileLayerCategory.Scripts);
            }
        }

        public bool IsLayerCategoryVisible(TileLayerCategory category) =>
            _layerCategoryVisibility.TryGetValue(category, out bool visible) ? visible : category is not (TileLayerCategory.Areas or TileLayerCategory.Scripts);

        public void SetLayerCategoryVisible(TileLayerCategory category, bool visible)
        {
            _layerCategoryVisibility[category] = visible;
            if (category == TileLayerCategory.Turfs) _turfsAutoHidden = false;
            NotifyChanged();
        }

        public void NotifyChanged() => StateChanged?.Invoke();

        public void SetTool(MapEditorTool tool)
        {
            CurrentTool = tool;
            _layerCategoryVisibility[TileLayerCategory.Areas] = IsRoomCategory;
            UpdateHint();
            NotifyChanged();
        }

        public void SetMode(MapEditorMode mode)
        {
            CurrentMode = mode;
            MapEditorSubcategory[] subs = MapEditorCatalog.GetSubcategories(mode);
            CurrentSubcategory = subs.Length > 0 ? subs[0] : MapEditorSubcategory.Uncategorized;
            _layerCategoryVisibility[TileLayerCategory.Scripts] = mode == MapEditorMode.Scripting;
            UpdateCategoryTool();
            UpdateHint();
            NotifyChanged();
        }

        public void SetSubcategory(MapEditorSubcategory subcategory)
        {
            CurrentSubcategory = subcategory;
            UpdateCategoryTool();
            UpdateHint();
            NotifyChanged();
        }

        private void UpdateCategoryTool()
        {
            bool rooms = CurrentMode == MapEditorMode.Scripting && CurrentSubcategory == MapEditorSubcategory.Rooms;
            if (CurrentTool == MapEditorTool.Area) CurrentTool = MapEditorTool.Edit;
            _layerCategoryVisibility[TileLayerCategory.Areas] = rooms;
            UpdateTurfsForLowerRuns();
        }

        /// <summary>Pipes and disposals run under the turf, so their tabs hide Turfs until you leave them.</summary>
        private void UpdateTurfsForLowerRuns()
        {
            bool underTurf = CurrentSubcategory is MapEditorSubcategory.Piping or MapEditorSubcategory.Disposals;
            if (underTurf && !_turfsAutoHidden && IsLayerCategoryVisible(TileLayerCategory.Turfs))
            {
                _layerCategoryVisibility[TileLayerCategory.Turfs] = false;
                _turfsAutoHidden = true;
            }
            else if (!underTurf && _turfsAutoHidden)
            {
                _layerCategoryVisibility[TileLayerCategory.Turfs] = true;
                _turfsAutoHidden = false;
            }
        }

        public void SelectEntry(MapEditorCatalogEntry entry, GenericObjectSo asset)
        {
            SelectedEntry = entry;
            SelectedAsset = asset;
            IsEraserSelected = entry?.IsEraser == true;
            UpdateHint();
            NotifyChanged();
        }

        public void ClearSelection()
        {
            SelectedEntry = null;
            SelectedAsset = null;
            IsEraserSelected = false;
            UpdateHint();
            NotifyChanged();
        }

        public void ShowToast(string message)
        {
            ToastMessage = message;
            NotifyChanged();
        }

        public void ClearToast()
        {
            ToastMessage = null;
            NotifyChanged();
        }

        public void SetUndoState(int undoDepth, int redoDepth)
        {
            UndoDepth = undoDepth;
            RedoDepth = redoDepth;
            NotifyChanged();
        }

        /// <summary>Recompute the default hint (after a transient hover message such as a build failure).</summary>
        public void RefreshHint() => UpdateHint();

        /// <summary>
        /// Construct-tool hint for the selected asset. Lower-layer runs (plenum, pipes, disposals) and
        /// flooring are usually laid in areas, so Shift fill leads; wall mounts lead with R to rotate.
        /// </summary>
        public string PlacementHint()
        {
            TileLayer? layer = (SelectedAsset as TileObjectSo)?.layer;

            if (CurrentMode == MapEditorMode.Items || SelectedAsset is ItemObjectSo)
                return "Click to place the item";

            if (layer is TileLayer.WallMountHigh or TileLayer.WallMountLow
                || (layer == null && CurrentSubcategory == MapEditorSubcategory.WallAttachments))
                return "Press R to rotate — click a wall to place, drag for a line";

            if (CurrentMode == MapEditorMode.Lower
                || layer is TileLayer.Plenum or TileLayer.Turf or TileLayer.Wire or TileLayer.Disposal
                    or TileLayer.PipeLeft or TileLayer.PipeMiddle or TileLayer.PipeRight or TileLayer.PipeSurface
                    or TileLayer.PipeManifold)
                return "Hold Shift and drag to fill a rectangle — plain drag places a line";

            return "Drag to place a line, Shift+drag to fill a rectangle — R to rotate";
        }

        private void UpdateHint()
        {
            if (IsRoomCategory)
            {
                SelectedObjectHint = CurrentTool switch
                {
                    MapEditorTool.Edit => $"Construct: {AreaRoomName} — drag a line; Shift-drag a rectangle",
                    MapEditorTool.Select => "Select a room to edit its properties",
                    MapEditorTool.Dropper => "Pick a room to paint with",
                    MapEditorTool.Delete => "Delete room assignments — drag a line; Shift-drag a rectangle",
                    _ => "Choose Construct, Select, Dropper or Delete for rooms",
                };
                return;
            }
            if ((IsEraserSelected && CurrentTool == MapEditorTool.Edit) || CurrentTool == MapEditorTool.Delete)
            {
                string label = MapEditorCatalog.GetSubcategoryLabel(CurrentSubcategory);
                SelectedObjectHint = CurrentSubcategory == MapEditorSubcategory.WallAttachments
                    ? $"Delete {label} — R to change face"
                    : $"Delete {label} — drag a line, Shift+drag a rectangle";
                return;
            }

            if (CurrentTool == MapEditorTool.Edit && SelectedAsset != null)
            {
                SelectedObjectHint = PlacementHint();
                return;
            }

            if (CurrentTool == MapEditorTool.Select)
            {
                SelectedObjectHint = "Select tool active — click a tile to inspect";
                return;
            }

            if (CurrentTool == MapEditorTool.Move)
            {
                SelectedObjectHint = "Move tool active — click and drag an object";
                return;
            }

            if (CurrentTool == MapEditorTool.Dropper)
            {
                SelectedObjectHint = "Dropper tool active — click a placed object to copy it";
                return;
            }

            SelectedObjectHint = SelectedAsset != null
                ? "Select the Edit tool to place this object"
                : "Choose an object from the library";
        }
    }
}
